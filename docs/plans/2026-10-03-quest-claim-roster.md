# Quest Claim Roster Implementation Plan

**Goal:** When the first turn-in of a one-time quest (`Quest.OnlyOnePlayerCanComplete`) happens, record a roster of every player who had the quest active and met its requirements, so they can still turn it in later despite the global claim.

**Architecture:** Extends the existing one-time claim system (`docs/plans/2026-10-02-one-time-quests-design.md`): a `player_ids` JSON column on `quest_claims` holds an immutable roster built on the game thread at claim time; all five claim gates switch to one roster-aware predicate `QuestHandler.IsClaimedFor(quest, player)`. No new table, no roster mutation, no new quest flag.

**Tech Stack:** C#/.NET 10, SQLite (write-through queue via `Database.Enqueue`), System.Text.Json (`JsonHelper.DatabaseOptions`), xUnit. Unit tests via `TestWorldFixture`, integration tests on real SQLite via `PlayerFirstSaveTestBase`.

**Design doc:** `docs/plans/2026-10-03-quest-claim-roster-design.md` (this worktree). Every promise there must survive into this plan — see final review pass.

---

## APIs verified (from source, this branch)

| API | Location | Note |
|-----|----------|------|
| `QuestClaim` | `Goose/Quests/QuestClaim.cs:5-9` | QuestId, PlayerId, CompletedAt |
| `QuestHandler.Claims` / `IsClaimed` / `TryGetClaim` / `Claim` | `Goose/Quests/QuestHandler.cs:13-40` | dict is source of truth; game thread only |
| `QuestHandler.LoadClaims` | `Goose/Quests/QuestHandler.cs:106-128`; called at `Goose/GameWorld.cs:400` (startup only, never from `LoadQuests`) | |
| Gate: availability | `Goose/Quests/QuestWindow.cs:107` | `IsClaimed && !started → skip` |
| Gate: start guard | `Goose/Quests/QuestWindow.cs:130` | |
| Gate: turn-in block + eviction | `Goose/Quests/QuestWindow.cs:231-237` | |
| Claim write | `Goose/Quests/QuestWindow.cs:376-377` | guarded by `!IsClaimed`; `QuestsCompleted.Add` happens first at :371-374 |
| Gate: yellow icon | `Goose/Quests/QuestStateResolver.cs:37` | Ready branch (:63) never consults the claim |
| `QuestStateResolver.IsActive` / `MeetsRequirements` | `Goose/Quests/QuestStateResolver.cs:12-19, 48-57` | |
| `QuestWindow.FindGrantingNpc` | `Goose/Quests/QuestWindow.cs:87-96` | `internal static string?` |
| All loaded players (online + offline) | `Goose/PlayerHandler.cs:245-248` `GetAllPlayerData()` | backed by private `allNameToPlayer` (:19), populated at startup load (:215) |
| `GameWorld.SendToAll` | `Goose/GameWorld.cs:814-823` | sends to `PlayerHandler.Players` with `State > LoadingGame` |
| `GameWorld.Send(Player, string)` | `Goose/GameWorld.cs:753` | `CapturingPlayer` overrides `Player.Send` to capture |
| `P.ServerMessage` | `Goose/Packets.cs:38` | `Func<string,string>` |
| `Player.States` | `Goose/Player.cs:61-64` | `NotLoggedIn = 0` — loaded offline players keep this default |
| Per-player quest state non-null for loaded players | `Goose/Player.cs:583-585` (ctor), `:909-911` (Inventory/Spellbook on load) | snapshot may call `MeetsRequirements` on any loaded player |
| Migration helpers | `Goose/GameWorld.cs:272-283` (`MigrateDatabaseSchema`), `:298-305` (`AddColumnIfMissing`), `:307` (`CreateTableIfMissing`) | schema file list at `:247` includes `quest_claims` |
| Fresh schema file | `Goose/sql/quest_claims.sql` | copied to test output; `QuestOneTimeTests.StartDatabase` executes it |
| `TestWorldFixture` | `TestSupport/TestWorldFixture.cs` | `CapturingPlayer` :93, `CommandPlayerOn` :100 (State=Ready, not registered), `RegisterOnlinePlayer` :115 (`nameToPlayer` only — invisible to SendToAll), `RegisterDatabasePlayer` :123 (`allNameToPlayer`), `AddOnlinePlayer` :131 (`PlayerHandler.AddPlayer` — the only helper reaching `Players`) |
| Existing test patterns | `Goose.Tests/QuestOneTimeTests.cs` | `SetupOneTime`, `MakePlayer`, `StartDatabase`, direct `Claims[...]` injection |
| Integration base | `Goose.IntegrationTests/QuestClaimPersistenceTests.cs:8` | `PlayerFirstSaveTestBase(["players","quest_claims"], ["quests","quest_requirements","quest_rewards"])`, `InsertQuestRow` :118 |

**Roster JSON format:** serialized `HashSet<int>` via `JsonSerializer.Serialize(roster, JsonHelper.DatabaseOptions)` → `"[]"` or `"[7,12]"`. Column default `''` exists only for pre-feature migrated rows; loader treats `''` and `[]` as empty.

---

## Task 1: Schema — `player_ids` column, fresh + migrated

**Mutation impact:**
- Source of truth changed: `quest_claims` table gains `player_ids TEXT NOT NULL DEFAULT ''`. Chosen strategy: **automatic migration in code** (fresh schema file + `AddColumnIfMissing` on every startup), mirroring the existing `players.player_properties` pattern (`Goose/GameWorld.cs:276`).
- Important readers: `QuestHandler.Claim` INSERT (Task 3), `QuestHandler.LoadClaims` (Task 2). Nothing reads the column before Task 2 — this task is inert storage.
- Derived/cached state affected: none yet. "No derived state found."
- Required propagation sequence: table exists with column → later tasks write/read it in the same row INSERT.
- Invariants: pre-feature rows load as empty roster; both fresh and migrated DBs end with identical schema.
- Observable proof: integration test creates a table with the OLD schema, runs `MigrateDatabaseSchema()`, asserts the column exists and an old row round-trips as empty roster.

**Files:**
- Modify: `Goose/sql/quest_claims.sql`
- Modify: `Goose/GameWorld.cs:279-280` (inside `MigrateDatabaseSchema`)
- Test: `Goose.IntegrationTests/QuestClaimPersistenceTests.cs`

**Step 1: Write the failing test** — add to `QuestClaimPersistenceTests`:

```csharp
[Fact]
public void Migration_adds_player_ids_to_an_old_schema_table()
{
    world.Database.Execute(conn =>
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DROP TABLE quest_claims";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "CREATE TABLE quest_claims (quest_id INT PRIMARY KEY, player_id INT NOT NULL, completed_at TEXT NOT NULL)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO quest_claims VALUES (9, 7, '2026-01-01T00:00:00.0000000Z')";
        cmd.ExecuteNonQuery();
    });

    world.MigrateDatabaseSchema(); // idempotent — every helper is guard-first

    var hasColumn = world.Database.Execute<bool>(conn =>
        GameWorld.ColumnExists(conn, "quest_claims", "player_ids"));
    Assert.True(hasColumn);

    var reloaded = new QuestHandler();
    reloaded.LoadClaims(world); // must not crash on the migrated row (loader doesn't read the column until Task 2)
    Assert.True(reloaded.IsClaimed(9));
}
```

Order matters: Task 1 lands schema-only (inert column, nothing reads it), so the existing `LoadClaims` keeps working on both old and new tables. Task 2 then adds the reader. Asserting `claim.Roster` here is impossible (property arrives in Task 2) — the "migrated row = empty roster" proof lives in Task 6 instead.

**Step 2: Red** — `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter QuestClaimPersistence` → fails (no column).

**Step 3: Implement**

Schema source of truth: both test projects execute `sql/quest_claims.sql` from their output dir (`PlayerFirstSaveTestBase` ctor, `QuestOneTimeTests.StartDatabase`), and `Goose.csproj:29-31` copies `sql/*.sql` PreserveNewest — so updating the one file covers fresh DBs and both test suites; no other DDL exists.

`Goose/sql/quest_claims.sql`:

```sql
CREATE TABLE quest_claims (
  quest_id INT PRIMARY KEY,
  player_id INT NOT NULL,
  completed_at TEXT NOT NULL,
  player_ids TEXT NOT NULL DEFAULT ''
);
```

`GameWorld.MigrateDatabaseSchema()` — extend the `CreateTableIfMissing` column string (`:279-280`) to include `player_ids TEXT NOT NULL DEFAULT ''` and add after it:

```csharp
AddColumnIfMissing(conn, "quest_claims", "player_ids", "TEXT NOT NULL DEFAULT ''");
```

**Step 4: Green** — same command passes. Full integration suite: `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --nologo -v q`.

**Step 5: Commit** — `feat(quests): player_ids column on quest_claims with startup migration`

---

## Task 2: Model, loader, and the roster-aware gate

**Mutation impact:**
- Source of truth: `QuestHandler.Claims` dictionary (`QuestHandler.cs:13`), loaded from `quest_claims` at startup only (`GameWorld.cs:400`). Adds `QuestClaim.Roster` (`HashSet<int>`).
- Important readers of the new gate: the five gating points listed in the APIs table (Task 4 switches them; until then they keep the old predicate — behavior unchanged, this task only adds the helper).
- Derived/cached state: none — `IsClaimedFor` computes from claim + player state on demand. "No derived state found."
- Invariants:
  - `IsClaimedFor == false` for non-one-time quests and unclaimed quests (never blocks normal quests).
  - Rostered + not-yet-completed → `false` (may act). Rostered + completed → `true` (one completion, ever). Not rostered + claimed → `true`.
- Observable proof: unit tests on the predicate directly, including the adversarial "rostered player who completed is blocked" (fails any plain pass-through implementation).

**Files:**
- Modify: `Goose/Quests/QuestClaim.cs`
- Modify: `Goose/Quests/QuestHandler.cs:106-128` (`LoadClaims`) and add `IsClaimedFor`
- Test: `Goose.Tests/QuestRosterTests.cs` (new)

**Step 1: Write the failing tests** in `Goose.Tests/QuestRosterTests.cs` (reuse the `SetupOneTime`/`MakePlayer` helper shapes from `QuestOneTimeTests.cs:7-31, 85-99`):

```csharp
[Theory]
[InlineData(true, false, false)]   // rostered, not completed  -> not blocked
[InlineData(true, true, true)]     // rostered, completed      -> blocked (adversarial: plain pass-through fails)
[InlineData(false, false, true)]   // not rostered             -> blocked
[InlineData(false, true, true)]    // not rostered, completed  -> blocked
public void Gate_follows_roster_and_completion(bool rostered, bool completed, bool expectBlocked)
{
    var (world, _, player, quest) = SetupOneTime();
    world.World.QuestHandler.Claims[quest.Id] = new QuestClaim
    {
        QuestId = quest.Id,
        PlayerId = 1,
        CompletedAt = DateTime.UtcNow,
        Roster = rostered ? [player.PlayerID] : [],
    };
    if (completed) player.QuestsCompleted.Add(quest);

    Assert.Equal(expectBlocked, world.World.QuestHandler.IsClaimedFor(quest, player));
}

[Fact]
public void Gate_never_blocks_non_one_time_or_unclaimed_quests()
{
    var (world, _, player, quest) = SetupOneTime();
    quest.OnlyOnePlayerCanComplete = false;
    Assert.False(world.World.QuestHandler.IsClaimedFor(quest, player));

    quest.OnlyOnePlayerCanComplete = true;
    Assert.False(world.World.QuestHandler.IsClaimedFor(quest, player));
}
```

Plus a loader round-trip test (in-memory → `Claim` INSERT → `LoadClaims` into a fresh handler → roster equal) — that one needs Task 3's write path; defer it to Task 6's integration test instead. Keep Task 2 tests to the predicate.

**Step 2: Red** — `dotnet test Goose.Tests/Goose.Tests.csproj --filter QuestRoster` → compile failure is acceptable red here (new API).

**Step 3: Implement**

`QuestClaim`: add `public HashSet<int> Roster { get; set; } = [];`

`QuestHandler`:

```csharp
public bool IsClaimedFor(Quest quest, Player player)
{
    if (!quest.OnlyOnePlayerCanComplete) return false;
    if (!Claims.TryGetValue(quest.Id, out var claim)) return false;
    return !(claim.Roster.Contains(player.PlayerID)
        && !player.QuestsCompleted.Any(q => q.Id == quest.Id));
}
```

`LoadClaims` (`QuestHandler.cs:106-128`): select `player_ids`, parse with the corrupt-blob pattern the quest_status loader uses (`Player.cs:955-962` — log and continue):

```csharp
var roster = new HashSet<int>();
var raw = reader.GetString("player_ids");
if (!string.IsNullOrEmpty(raw))
{
    try { roster = JsonSerializer.Deserialize<HashSet<int>>(raw, JsonHelper.DatabaseOptions) ?? []; }
    catch (JsonException e) { log.Error(e, "quest_claims roster for quest {0} is corrupt; starting empty", reader.GetInt32("quest_id")); }
}
```

**Step 4: Green** — filtered test passes; then full unit suite (`dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q`, expect 1637+ passing — existing one-time tests must not change behavior since no gate uses the new predicate yet).

**Step 5: Commit** — `feat(quests): roster on QuestClaim with completion-aware gate predicate`

---

## Task 3: Roster snapshot + online nudge inside `Claim()`

Builds the roster at claim time. The nudge lives inside `Claim()` rather than after the call in `CompleteQuest` (design said "after `Claim()`" — same game thread, same tick; the snapshot loop already has the `Player` objects, so no id→player re-lookup). Document this in the code only if it needs the "why"; otherwise let it be.

**Mutation impact:**
- Source of truth: `Claims[quest.Id]` gains a populated `Roster` before the write-through INSERT (`QuestHandler.cs:19-40`), so dict and row always agree (existing pattern; dict is truth during play).
- Important readers: `IsClaimedFor` at the five gates (Task 4), `LoadClaims` on restart, integration tests reading `player_ids`.
- Derived/cached state: none. Icons are viewer-scoped; the nudge is a message, not state. "No derived state found" beyond the claim dict itself.
- Required propagation sequence: game thread → `Claim()` mutates dict (incl. roster) → `Database.Enqueue` INSERT with `player_ids` → readers see it immediately via dict; DB via queue drain; restart via `LoadClaims`.
- Invariants:
  - Roster excludes the completer, already-completed players, non-starters, and requirement-failers; includes offline loaded players.
  - Roster is never mutated after this point (no UPDATE anywhere).
  - `INSERT OR REPLACE` unchanged: a crash-reclaim overwrites row and roster together (no new crash window).
- Observable proof: unit tests assert roster membership by id and captured nudge text, not that a helper "was called".

**Files:**
- Modify: `Goose/Quests/QuestHandler.cs:19-40` (`Claim`)
- Test: `Goose.Tests/QuestRosterTests.cs`

**Step 1: Write the failing tests** — scenarios (each registers players with `world.RegisterDatabasePlayer` for offline-loaded, or sets `State = Player.States.Ready` for online):

```csharp
[Fact]
public void Claim_rosters_active_requirement_meeting_players_online_and_offline_and_excludes_everyone_else()
```
Setup: one-time quest with a gold requirement (50) and a kill requirement (npc 5 ×3). Players:
- "OfflineDid" — registered via `RegisterDatabasePlayer`, quest started, progress met, gold 50 → **rostered** (State stays `NotLoggedIn`).
- "OnlineDid" — same state, `State = Player.States.Ready` → **rostered** + captured `Sent` contains `"You helped complete"` and `"turn it in at"`.
- "Alice" — the completer (passed to `Claim`) → **not rostered**.
- "BobNeverStarted" — meets gold, quest not started → not rostered (adversarial: fails a snapshot that skips `IsActive`).
- "CarolNoTicket" — started, gold 50, kill progress 2/3 → not rostered (adversarial: fails a snapshot that skips `MeetsRequirements`).
- "DanDone" — quest in `QuestsCompleted`, started list cleared → not rostered.
Assert `Claims[quest.Id].Roster` set-equals `{OfflineDid.PlayerID, OnlineDid.PlayerID}`.

```csharp
[Fact]
public void Nudge_names_the_granting_npc_and_falls_back_when_none_grants_the_quest()
```
Npc granted → message contains the npc name via `QuestWindow.FindGrantingNpc`; ungranted → contains `"the quest giver"`.

**Step 2: Red** — `dotnet test Goose.Tests/Goose.Tests.csproj --filter QuestRoster` → roster empty.

**Step 3: Implement** — in `Claim`, after populating `Claims[quest.Id]` and before the enqueue:

```csharp
var roster = new HashSet<int>();
foreach (var p in world.PlayerHandler.GetAllPlayerData())
{
    if (p == player) continue;
    if (!QuestStateResolver.IsActive(quest, p)) continue;
    if (p.QuestsCompleted.Any(q => q.Id == quest.Id)) continue;
    if (!QuestStateResolver.MeetsRequirements(quest, p, world)) continue;
    roster.Add(p.PlayerID);
}
Claims[quest.Id].Roster = roster;

var npcName = QuestWindow.FindGrantingNpc(world, quest.Id) ?? "the quest giver";
foreach (var id in roster)
{
    var p = world.PlayerHandler.GetAllPlayerData().First(x => x.PlayerID == id);
    if (p.State == Player.States.Ready)
        world.Send(p, P.ServerMessage($"You helped complete {quest.Name}. You can still turn it in at {npcName}."));
}
```

(Iterate once if cleaner: collect `List<Player>` alongside the ids, then send — implementation judgment. The second lookup must not silently skip: every roster id comes from this same iteration.)

Add `player_ids` to the INSERT (`QuestHandler.cs:34-37`): serialize `claim.Roster` with `JsonHelper.DatabaseOptions`, bind as `DbType.String`.

**Step 4: Green** — filtered tests pass; full unit suite green (existing claim tests: the INSERT gained a column — `Claiming_writes_a_row_and_reload_restores_it` must still pass once Task 1's schema is in).

**Step 5: Commit** — `feat(quests): roster snapshot and online nudge at claim time`

---

## Task 4: Switch the five gates to `IsClaimedFor`

**Mutation impact:**
- Source of truth: unchanged (`Claims`); this task changes *readers* — the four blocking gates consult the roster. The claim-write guard at `QuestWindow.cs:376` deliberately keeps raw `IsClaimed` (a rostered turn-in must not re-claim or re-broadcast).
- Important readers after change: `GetAvailableQuests` (:107), `StartQuest` (:130), `Clicked` block+eviction (:231), `QuestStateResolver.IsAvailable` (:37). The `QuestAlreadyClaimed` text path (:199-203) is unchanged — rostered players never reach it.
- Derived/cached state: icons. `QuestHandler.RefreshIcons` only *sends* icon packets (`QuestHandler.cs:148-172`); the roster change needs no new refresh beyond what each path already calls (`StartQuest`/`CompleteQuest`/eviction already refresh).
- Invariants: non-rostered behavior byte-identical to today (all existing `QuestOneTimeTests` pass without edits); rostered players complete through the normal flow with requirements consumed.
- Observable proof: end-to-end window tests (click → rewards + rostered state), plus the untouched existing suite as the regression proof.

**Files:**
- Modify: `Goose/Quests/QuestWindow.cs:107, 130, 231`
- Modify: `Goose/Quests/QuestStateResolver.cs:37`
- Test: `Goose.Tests/QuestRosterTests.cs`

**Step 1: Write the failing tests**

```csharp
[Fact]
public void Rostered_player_sees_lists_starts_and_turns_in_a_claimed_quest_normally()
```
Inject claim with roster containing the player (like `QuestOneTimeTests.Claim` helper but with `Roster = [player.PlayerID]`); quest has gold requirement 50, player has 100 gold, quest started. Assert:
- listed by `GetAvailableQuests`; `Resolve` → `Ready`;
- `window.Clicked(Next, ...)` → text equals `quest.PassText`, gold 50 (consumed — adversarial: fails a "free completion" implementation), quest in `QuestsCompleted`, `Claims[quest.Id].PlayerId` still the original claimer and `Roster` unchanged (no re-claim).

```csharp
[Fact]
public void Rostered_player_who_already_completed_is_blocked_from_re_turn_in_including_repeatable()
```
Same, but `player.QuestsCompleted.Add(quest)` and `quest.Repeatable = true`, quest re-started. Clicked → `QuestAlreadyClaimed` text, evicted. (Adversarial for the completion-aware gate; also pin the non-repeatable variant.)

```csharp
[Fact]
public void Rostered_player_can_restart_a_claimed_quest_and_a_non_rostered_one_still_cannot()
```
`StartQuest` both players.

**Step 2: Red** — rostered tests fail (old predicate blocks them).

**Step 3: Implement** — replace the predicate at the four sites:

- `QuestWindow.cs:107` → `if (world.QuestHandler.IsClaimedFor(quest, player) && !player.QuestsStarted.Any(q => q.Id == quest.Id)) continue;`
- `QuestWindow.cs:130` → `if (world.QuestHandler.IsClaimedFor(quest, player)) return;`
- `QuestWindow.cs:231` → `if (world.QuestHandler.IsClaimedFor(quest, player))`
- `QuestStateResolver.cs:37` → `if (world.QuestHandler.IsClaimedFor(quest, player)) return false;`

Do **not** touch `QuestWindow.cs:376` (claim write) or the `QuestAlreadyClaimed` message branch.

**Step 4: Green** — `dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q`; the entire existing `QuestOneTimeTests` class must pass unmodified (regression invariant for "empty roster = today's behavior").

**Step 5: Commit** — `feat(quests): roster-aware claim gates for turn-in, start, and visibility`

---

## Task 5: World-first broadcast

**Mutation impact:**
- Source of truth: none new — message only, fired inside the existing `!IsClaimed` guard (`QuestWindow.cs:376-377`), so exactly once per quest ever.
- Readers: all connected players via `GameWorld.SendToAll` (`GameWorld.cs:814`, skips `State <= LoadingGame`).
- "No derived state found."
- Invariant: rostered turn-ins never broadcast (guard already excludes them — prove it, don't assume).
- Observable proof: online `CapturingPlayer` observers' `Sent` contains the exact text on first completion, and a second (rostered) completion produces no further `[World First]` message.

**Files:**
- Modify: `Goose/Quests/QuestWindow.cs:376-377`
- Test: `Goose.Tests/QuestRosterTests.cs`

**Step 1: Write the failing test** — end-to-end through the window like `The_first_completion_claims_the_quest_and_blocks_a_second_player` (`QuestOneTimeTests.cs:160+`), with an observer registered via `world.AddOnlinePlayer(observer)` (`TestWorldFixture.cs:131` — the only fixture helper that reaches `PlayerHandler.AddPlayer` → the `players` list that `SendToAll` iterates, `PlayerHandler.cs:51-59, 160-162`; `RegisterOnlinePlayer` only touches `nameToPlayer` and would be invisible to the broadcast) and `observer.State = Player.States.Ready` (`SendToAll` skips `State <= LoadingGame`, `GameWorld.cs:818`). Assert first completion sends `"[World First] One Time has been completed by Alice!"` (names from the test setup) to the observer; then a rostered player completes and no additional `[World First]` appears in `Sent`.

**Step 2: Red** → **Step 3: Implement** — inside the `if (quest.OnlyOnePlayerCanComplete && !world.QuestHandler.IsClaimed(quest.Id))` block, after `Claim(...)`:

```csharp
world.SendToAll(P.ServerMessage($"[World First] {quest.Name} has been completed by {player.Name}!"));
```

**Step 4: Green** → **Step 5: Commit** — `feat(quests): world-first broadcast on first one-time completion`

---

## Task 6: Integration — roster persistence, restart, reload safety

**Files:**
- Test: `Goose.IntegrationTests/QuestClaimPersistenceTests.cs`

**Step 1: Write the tests** (real SQLite, existing `InsertQuestRow` + `Claim` patterns):

1. `Claiming_writes_the_roster_into_player_ids` — build a `Player` whose quest is started and requirements met, register it as a loaded player via `world.PlayerHandler.AddPlayerToData(player)` (`PlayerHandler.cs:225`, the public seam into `allNameToPlayer`; no players-table row needed), call `Claim` with a different completer, drain queue (`world.Database.Execute(conn => { })`), read `player_ids` from the row, assert the JSON contains the rostered id.
2. `Roster_survives_restart_and_unblocks_the_rostered_player` — complete → dispose → fresh `QuestHandler.LoadClaims` → `IsClaimedFor` false for rostered id, true for others. (Round-trip pattern from `Claiming_writes_a_row_and_reload_restores_it`, :16-47.)
3. Extend `Loading_quests_does_not_touch_existing_claims` (:49-62) to also assert `claim.Roster` survives `LoadQuests` (the `/reloadsql` invariant).
4. Extend Task 1's migration test: after `MigrateDatabaseSchema` + `LoadClaims`, assert `claim.Roster` is empty for the pre-feature row (the "migrated row = empty roster" proof deferred from Task 1).

**Step 2: Red** (test 1 fails until Task 3's column write; on top of Tasks 1–5 all should pass — if they're green immediately, that's fine; they still pin the invariants).

**Step 3: Implement** any gaps the tests expose — no new features in this task.

**Step 4: Green** — `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --nologo -v q`.

**Step 5: Commit** — `test(quests): roster persistence, restart round-trip, and reload safety`

---

## Task 7: Full-suite verification and design walk

1. `dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q` and `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --nologo -v q` — all green (baseline was 1637 unit).
2. Walk the design doc top to bottom; confirm each promise exists in code: gate predicate form, roster exclusions, JSON format incl. `''` legacy rows, nudge text and fallback, broadcast text incl. trailing `!`, no UPDATE path for rosters, `/reloadsql` untouched, no new quest flag, comment policy per AGENTS.md (new code carries no comments unless a non-obvious "why").
3. Commit any corrections as `docs`/`fix` follow-ups.

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| One completion ever for every player (incl. rostered, claimer, repeatable combo) | Task 2 gate theory; Task 4 re-turn-in test |
| Roster = active ∧ requirements met ∧ ¬completed ∧ ¬completer, online+offline | Task 3 snapshot test (adversarial exclusions) |
| Empty/legacy roster ⇒ today's behavior | Existing `QuestOneTimeTests` unmodified (Task 4 step 4) |
| Rostered turn-in does not re-claim/mutate roster/re-broadcast | Task 4 completion test asserts claim+roster; Task 5 second-completion assertion |
| Requirements consumed at rostered turn-in | Task 4 gold assertion (adversarial vs. freeze semantics) |
| Dict↔row agreement; roster rides the single INSERT | Task 6 tests 1–2 |
| `/reloadsql` never touches claims/rosters | Task 6 test 3 (extends existing) |
| Migrated DBs get the column; old rows = empty roster | Task 1 migration test |
| Nudge only to online (Ready) roster members, with npc name or fallback | Task 3 nudge tests |
| Broadcast fires exactly once per quest | Task 5 test; guard structure at `QuestWindow.cs:376` |

Deferred by design: admin roster inspection command (SQL query instead), login nudge, whole-group broadcast naming — see design doc.
