# One-Time Quests Design

## Goal

Make `Quest.OnlyOnePlayerCanComplete` functional. Once any player completes such a quest, the
quest is claimed forever: nobody else (including the claimer) can complete it again. The claim is
server-global and survives restarts and spreadsheet re-imports.

Today the flag is loaded from the `only_one_player_can_complete` column but read nowhere. Per-player
quest state lives in the `quest_status` JSON blobs; the `quests` table itself is DROP/CREATE'd on
every data import, so claim state needs its own live table.

## Semantics (agreed)

- Claimed quests are hidden from players who have not started them: absent from the NPC quest list
  (`GetAvailableQuests`) and from the yellow "available" icon (`QuestStateResolver.IsAvailable`).
- Players who already started one can still see it (Ready icon) and attempt turn-in. The attempt is
  blocked with a message naming the completer, and the dead quest is evicted from their log then.
- Claimed blocks everyone, including the claimer re-completing a `Repeatable=true` one-time quest.
  That combination is not intended for authored content; it simply behaves as "one completion, ever".
- No admin reset command. Clearing a claim is `DELETE FROM quest_claims WHERE quest_id = N`.

## Data model & persistence

`Goose/Quests/QuestClaim.cs`:

```csharp
public class QuestClaim
{
    public int QuestId { get; set; }
    public int PlayerId { get; set; }
    public DateTime CompletedAt { get; set; }
}
```

The player name is deliberately not stored; it is resolved at display time.

Table (live, never touched by the CsvToSql import):

```sql
CREATE TABLE quest_claims (
  quest_id INT PRIMARY KEY,
  player_id INT NOT NULL,
  completed_at TEXT NOT NULL   -- ISO-8601 UTC
);
```

- Fresh databases: new `Goose/sql/quest_claims.sql`, added to the schema-file array in
  `GameWorld.CreateDatabaseSchema()`.
- Existing databases: `CreateTableIfMissing(conn, "quest_claims", ...)` in
  `GameWorld.MigrateDatabaseSchema()`, which runs on every startup. Mirrors the `quest_status`
  belt-and-braces pattern.

`QuestHandler` gains `Dictionary<int, QuestClaim> Claims`, loaded at startup by `LoadClaims()`
(claims are server state, never spreadsheet data — `/reloadsql`'s `LoadQuests()` must not touch
them, or a completion enqueued mid-reload would be wiped from the live dictionary), exposed via
`IsClaimed(int questId)` / `TryGetClaim(int questId, out QuestClaim)`. The dictionary is the
source of truth during play.

Write path: `QuestHandler.Claim(quest, player)` adds to the dictionary immediately (game thread —
first writer wins with no locking) and enqueues the `INSERT` (write-through). A failed INSERT is
logged via NLog; a crash before the queue drains at worst re-opens the quest, an accepted tradeoff
for a rarity flag. A crash after the claim commits but before the winner's next player save
permanently claims the quest while the winner's completion/rewards are lost and cannot be
retried; accepted (keeps the immediate write-through).

Name resolution: `PlayerHandler.GetPlayerName(int playerId)` scans `allNameToPlayer.Values` (all
non-deleted players are loaded at startup) and returns the name or null. Callers substitute
"Someone" when null. No SQL.

## Gating points

All keyed off `quest.OnlyOnePlayerCanComplete && QuestHandler.IsClaimed(quest.Id)`; no identity
comparison anywhere.

1. **Availability** — `QuestWindow.GetAvailableQuests()` skips claimed quests unless the player has
   already started them. `QuestStateResolver.IsAvailable()` gets the same check so claimed quests
   never show the Available icon to non-starters.
2. **Start guard** — `QuestWindow.StartQuest()` refuses to start a claimed quest for a non-starter
   (covers a stale option-list window opened before the claim landed).
3. **Turn-in block** — in `QuestWindow.Clicked`, first thing inside the meets-requirements branch,
   before any state mutation or script pre-checks (same pattern as `GetScriptCannotCompleteMessage`):
   if claimed, set new state `QuestWindowState.QuestAlreadyClaimed` with text
   `"{Name} has already completed this quest.\nIt can only be completed once."` No requirements are
   consumed and no rewards are given.
4. **Eviction on block** — when the block fires, remove the quest from `player.QuestsStarted`, drop
   its `player.QuestProgress` entries (mirroring `CompleteQuest`), and run
   `world.QuestHandler.RefreshIcons(player, world)` so the Ready icon dies immediately. The eviction
   persists with the player's normal save.
5. **Claim write** — at the top of `QuestWindow.CompleteQuest()`, after the `QuestsCompleted.Add`:
   if `OnlyOnePlayerCanComplete` and not yet claimed, call `QuestHandler.Claim(quest, player)`. The
   pre-check in `Clicked` plus the single game thread make first-completer-wins atomic.

`CompleteQuest`/`StartQuest` have no callers outside `QuestWindow`; `/quests` renders via
`GetActiveQuests`, which self-corrects after eviction. One `GameWorld` per server, so one cache.

## Edge cases & accepted gaps

- **Ready-icon staleness**: when A completes, only A's icons refresh (viewer-scoped, existing
  behavior). Other starters may see a stale Ready icon until they click (blocked + evicted) or
  relog. Accepted; no broadcast to all players in range.
- **Prerequisite chains**: a quest requiring a claimed one-time quest is permanently uncompletable
  for everyone else. Data-design caution for the spreadsheet, no code.
- **Orphan claims**: rows whose quest id no longer exists load harmlessly and never match. Cleanup
  is the same manual DELETE.
- **Deleted claimer**: `GetPlayerName` returns null; the message says "Someone".

## Testing

Unit tests — new `Goose.Tests/QuestOneTimeTests.cs` using `TestWorldFixture`, claims injected via
`QuestHandler`:

1. Claimed quest hidden from `GetAvailableQuests` for non-starters; still listed for starters.
2. `QuestStateResolver`: no Available icon when claimed; starter meeting requirements still gets Ready.
3. `StartQuest` on a claimed quest is a no-op for a non-starter.
4. Turn-in by a second player → `QuestAlreadyClaimed` state; message contains the claimer's name,
   "Someone" when unresolvable; player state untouched (no rewards, requirements kept, not added to
   `QuestsCompleted`); quest evicted from `QuestsStarted` and its `QuestProgress` dropped.
5. First completion claims: dictionary updated; second player blocked end-to-end through the window.
6. `Repeatable=true` + `OnlyOnePlayerCanComplete=true`: claimer blocked on re-turn-in.
7. Regression: non-one-time quests unchanged.

Integration tests — new `Goose.IntegrationTests/QuestClaimPersistenceTests.cs`, real SQLite:

1. Completing a one-time quest writes a `quest_claims` row with correct quest/player ids.
2. Claim survives a world restart: complete → dispose → fresh world loads claims → second player
   blocked (round-trip pattern from `GuildSaveTests`/`DoubleRoundTripTests`).
3. Fresh DB gets the table from `quest_claims.sql`; pre-existing DB without it gets it from
   `MigrateDatabaseSchema()`.

Baseline: 1611 unit + 430 integration tests pass on `feat-one-time-quests`.
