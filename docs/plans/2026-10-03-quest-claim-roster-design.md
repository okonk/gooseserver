# Quest Claim Roster Design

## Goal

One-time quests (`Quest.OnlyOnePlayerCanComplete`) are expected to be hard group content, but today
the first player to reach the NPC claims the quest globally and everyone who helped gets nothing.
This design makes the first turn-in record a **roster** of players who had done the work, letting
them turn in later through the normal flow despite the claim.

Builds directly on the one-time quest claim system
(`docs/plans/2026-10-02-one-time-quests-design.md`): `quest_claims` table, `QuestHandler.Claims`,
and the five gating points all stay; the gate predicate becomes roster-aware.

## Semantics

When the first turn-in of a one-time quest happens, the claim stores a roster: every loaded player
(online or offline) whose state at that instant shows the quest **active** and its **requirements
met**, excluding the completer and players who already completed it. Roster membership means
*"you did the work; you may still turn it in."*

- **Gate everywhere** (visibility, start guard, turn-in block) is one predicate:
  `claimed && !(rostered && !playerHasCompletedThisQuest) → block`. A rostered player passes until
  they complete; after that the claim blocks them like anyone else. One completion, ever, for every
  player — claimer, rostered, latecomer.
- Rostered players turn in through the normal flow: requirements re-checked and consumed, inventory/
  spellbook/script checks all apply. The roster grants the right to attempt, not a free completion.
- Everyone else behaves exactly as today: hidden from lists and yellow icons, blocked with
  "X has already completed this quest", evicted from the log on attempt.
- The roster is **write-once and never mutated**. No expiry, no consumption. It doubles as a
  permanent audit trail of who participated in the first clear. Admin reset stays
  `DELETE FROM quest_claims WHERE quest_id = N` — clears claim and roster together.
- No new quest flag. Applies to all one-time quests; a solo first-completer produces an empty
  roster and behavior is identical to today.

## Data model & persistence

One column on the existing live table, no new table:

```sql
CREATE TABLE quest_claims (
  quest_id INT PRIMARY KEY,
  player_id INT NOT NULL,
  completed_at TEXT NOT NULL,      -- ISO-8601 UTC
  player_ids TEXT NOT NULL DEFAULT ''   -- JSON array of int player ids: the roster
);
```

- Fresh databases: `Goose/sql/quest_claims.sql` gains the column.
- Existing databases: in `GameWorld.MigrateDatabaseSchema()`, extend the `CreateTableIfMissing`
  column list and add
  `AddColumnIfMissing(conn, "quest_claims", "player_ids", "TEXT NOT NULL DEFAULT ''")`. Both
  helpers exist. Pre-feature rows get `''` → empty roster → today's behavior, which is correct.
- `QuestClaim` gains `public HashSet<int> Roster { get; set; } = [];` (HashSet: the gate runs on
  every icon resolve and window open). Serialized as a JSON int array; `''` or `[]` loads as empty.
- `QuestHandler.LoadClaims()` parses the new column. Claims and rosters load at startup only;
  `/reloadsql` must not touch them (existing rule).
- The roster is built on the game thread inside `QuestHandler.Claim()` before the existing
  write-through `INSERT OR REPLACE`, as part of the same row insert: iterate every loaded player
  (`PlayerHandler.allNameToPlayer.Values`, online and offline alike) and roster those where
  `QuestStateResolver.IsActive(quest, p) && MeetsRequirements(quest, p, world) &&
  !p.QuestsCompleted.Any(q => q.Id == quest.Id) && p != completer`.
  Because it rides the existing single INSERT, the roster adds no new crash window: a failed insert
  re-opens claim and roster together (accepted failure mode). There is no UPDATE path anywhere.
- Gate helper: one method, e.g. `QuestHandler.IsClaimedFor(quest, player)`, implementing the
  predicate above, used unchanged at all five gating points so they cannot drift apart.

## Gating points

All five existing points swap `quest.OnlyOnePlayerCanComplete && IsClaimed(quest.Id)` for
`QuestHandler.IsClaimedFor(quest, player)`:

1. **Availability** — `QuestWindow.GetAvailableQuests()`: skip when `IsClaimedFor && !started`.
2. **Yellow icon** — `QuestStateResolver.IsAvailable()`: same swap. The Ready-icon branch needs no
   change; it never consulted the claim, so rostered players meeting requirements get Ready for free.
3. **Start guard** — `QuestWindow.StartQuest()`: refuse when `IsClaimedFor`. Rostered-and-not-
   completed may (re)start — covers the player who abandoned and comes back.
4. **Turn-in block** — `QuestWindow.Clicked()`: block + eviction + `QuestAlreadyClaimed` only when
   `IsClaimedFor`. Rostered players fall through into the normal completion flow, untouched code.
5. **Claim write** — `QuestWindow.CompleteQuest()`: unchanged guard (`one-time && !IsClaimed →
   Claim`); `Claim()` now also builds the roster. Rostered turn-ins find the quest already claimed:
   no re-claim, no roster mutation, no broadcast.

Back-to-back turn-ins need no locking: the single game thread serializes them; the first click
claims and rosters (including the second clicker, who met requirements), the second click then
passes the roster-aware gate and completes normally.

## Player experience & notifications

- **Rostered turn-in**: no new window state. Ready icon at the NPC, ordinary description →
  requirements → `QuestPass` flow. Nothing in the UI reveals the quest is claimed for the world.
- **Nudge at claim time**: after `Claim()`, each online roster member receives
  `P.ServerMessage`: `You helped complete {QuestName}. You can still turn it in at {NpcName}.`
  `{NpcName}` via `QuestWindow.FindGrantingNpc`; fallback "the quest giver" when not found.
- **First-claim broadcast**: inside the `!IsClaimed` guard, right after `Claim()`:
  `world.SendToAll(P.ServerMessage($"[World First] {quest.Name} has been completed by {player.Name}!"))`.
  Fires exactly once per quest ever. Rostered turn-ins never re-broadcast.
- **No login nudge.** Offline roster members discover it via the Ready icon when next at the NPC.
- Everyone else: unchanged.

## Edge cases & accepted gaps

- **Abandon = forfeit (mostly)**: a rostered player who abandons can restart, but kill progress
  resets; on a one-time boss they can no longer meet requirements. Accepted consequence of
  re-check-at-turn-in.
- **Re-earn**: a rostered player who lost their ticket (spent the gold, abandoned) can re-earn the
  requirements later and turn in — roster membership is the permanent door, requirements are the
  current lock. Accepted as generous-but-honest.
- **Requirement leaks**: a one-time quest with only farmable requirements (gold/items) would
  roster every player who happens to meet them at the instant of the claim. Data-authoring caution;
  kill/talk requirements on the one-time boss are the intended participation ticket.
- **Script requirements in the snapshot**: `MeetsRequirements` runs on every loaded player at claim
  time, so script `IsMet` must stay side-effect-free and cheap — the same contract icon resolution
  already requires. Authoring documentation, no code.
- **`Repeatable=true` + one-time**: the completion-aware gate gives every player exactly one
  completion ever, consistent with the original one-time design.
- **Deleted rostered players**: unresolvable ids in the roster are harmless; the gate is id-based.
- **Crash between claim and flush**: quest re-opens with no roster; accepted pre-existing window.
- **Roster size**: a leaky quest could store thousands of ids in one TEXT column (a few KB).
  Harmless; the caution is authoring-side.
- **No admin tooling** for viewing rosters; `SELECT player_ids FROM quest_claims` is the interface,
  matching the existing no-reset-command precedent. Deferred (YAGNI).
- **Stale Ready icon for non-rostered starters**: unchanged pre-existing accepted gap.

## Testing

Unit tests — new `Goose.Tests/QuestRosterTests.cs` using `TestWorldFixture`:

1. Snapshot correctness: rosteres online **and** offline players with quest active + requirements
   met; excludes the completer, already-completed players, non-starters, and requirement-failers.
2. Rostered player passes all gates: listed, startable, turn-in completes with rewards and
   requirement consumption; non-rostered players still blocked with the claimer-name message.
3. Completion-aware gate: after a rostered player completes, they are blocked from re-turn-in —
   including the `Repeatable=true` one-time combination.
4. Rostered turn-in does not re-claim, re-broadcast, or mutate the roster.
5. Solo first-completer → empty roster → identical to today's behavior (regression guard).
6. Existing `QuestOneTimeTests` still pass unchanged.

Integration tests — extend `Goose.IntegrationTests/QuestClaimPersistenceTests.cs`, real SQLite:

1. First completion writes the roster ids into `quest_claims.player_ids`.
2. Restart round-trip: roster loads and the rostered player is still unblocked.
3. Migration: a database with old-schema `quest_claims` gains the column via
   `AddColumnIfMissing`; old rows behave as empty rosters.
4. `/reloadsql` leaves claims and rosters untouched.
