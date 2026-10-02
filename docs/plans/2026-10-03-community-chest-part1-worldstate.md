# Community chest Part 1: WorldState foundation — Implementation Plan

**Goal:** Build the generic `WorldState` key/value persistence service plus the two event
seams (`ItemContainer.SlotChanged`, `PlayerHandler.PlayerRemoved`) that the community chest
(part 2) is built on.

**Architecture:** `WorldState` holds an in-memory `Dictionary<string, object>` of JSON blobs
loaded from a new `world_state` SQLite table; dirty detection is serialize-and-diff at save
time (no tenant marks anything). A periodic `WorldSaveEvent` (the `GuildSaveEvent` pattern)
and a shutdown flush persist it. Design: `docs/plans/2026-10-03-community-chest-design.md`.

**Tech Stack:** .NET 10, C#, xUnit, System.Data.SQLite, NLog.

**Conventions:** follow `AGENTS.md` — no new comments except non-obvious invariants (the
DB-thread lock in Task 3 qualifies, one line).

## APIs verified

- `Database.Execute(Action<SQLiteConnection>)` / `Execute<T>(Func<…>)` — `Goose/Database.cs:163,196`; `Enqueue(action, onComplete)` — `:218`; `EnqueueTransaction(action, onCommit)` — `:238` (onCommit runs on the **DB thread** after COMMIT, `:249-253`); `PendingCount` — `:43`; throws `InvalidOperationException` when not started — `:171,199`.
- `JsonHelper.Serialize<T>` / `Deserialize<T>` with `DatabaseOptions` — `Goose/JsonHelper.cs:45-49`; format contract at `:8-12`.
- `GameWorld.LoadStep(name, action, countFn)` — `Goose/GameWorld.cs:471`; schema file list — `:245-251`; `MigrateDatabaseSchema` + `CreateTableIfMissing` — `:272-281,305`; `Stop()` save/flush order — `:500-539`; `Send(Player, string)` — `:753`; `EnqueueCompletion` — `:83`.
- `GuildSaveEvent` — `Goose/Events/GuildSaveEvent.cs` (whole file); re-arm idiom with `Math.Max(1, …)` clamp — `Goose/GuildHandler.cs:113-120` (clamp rationale at `:116`).
- `Event` base (`Ticks`, `Ready(GameWorld)`) — `Goose/Event.cs`; `EventHandler.AddEvent(Event)` — `Goose/EventHandler.cs:295`; `internal int Count` / `internal Event Peek()` — `:314,316`.
- `ItemContainer.SetSlot` (only container write path; logs + returns on out-of-range) — `Goose/ItemContainer.cs:25-34`.
- `PlayerHandler.RemovePlayer(Socket)` delegates to `RemovePlayer(Player)` — `Goose/PlayerHandler.cs:68-72,82-90`.
- `GooseSettings.GuildSavePeriod` — `Goose/GooseSettings.cs:96`; json common block — `Goose/GooseSettings.json:108`. Missing int settings deserialize to 0 (`GooseSettingsLoader.cs:60-71` only fixes strings) → clamp at use sites.
- `sql/*.sql` auto-copies to every referencing project's output — `Goose/Goose.csproj:29-31`; integration harness reads `AppContext.BaseDirectory/sql/<file>.sql` — `Goose.IntegrationTests/PlayerFirstSaveTests.cs:22-27`.
- Test helpers: `TestWorldFixture` (world without started DB; `AddOnlinePlayer` at `:107`, `CommandPlayerOn` at `:94`) — `TestSupport/TestWorldFixture.cs`; `PlayerFirstSaveTestBase(schemaFiles, generatedTables, withQuestStatus)` + `Count(sql)` — `Goose.IntegrationTests/PlayerFirstSaveTests.cs:7-56`.
- Event pump dequeues **before** `Ready` and drops the event on exception — `Goose/EventHandler.cs:337,355-366`; periodic events must re-arm in `finally`. `quest_status` is created by `players.sql:86`, not by the test base — a fixture listing only `world_state` with `withQuestStatus: false` would `DROP` a nonexistent table (`PlayerFirstSaveTests.cs:37-41`). `GooseSettings` int properties have no code defaults unless initialized (`GooseSettings.cs:97-99` precedents).

---

### Task 1: WorldState core (no DB)

**Files:**
- Create: `Goose/WorldState.cs`
- Test: `Goose.Tests/WorldStateTests.cs`

**Contract:**

```csharp
public class WorldState
{
    public void Load(Database db);
    internal void LoadRows(IEnumerable<KeyValuePair<string, string>> rows);
    public T? Get<T>(string key);
    public void Set(string key, object value);
    public void Remove(string key);
    public IEnumerable<string> KeysWithPrefix(string prefix);
    internal record SavePlan(List<(string Key, string Json)> Upserts, List<string> Deletes);
    internal SavePlan PlanSave();
    internal void ApplyCommit(SavePlan plan);   // on-commit; also the test seam
}
```

- **Value representation**: the dictionary holds either a `RawJson(string Json)` private
  record — the only way an unparsed database blob is represented — or a live object. A bare
  `string` value is always a live tenant value. Without the wrapper the two representations
  are indistinguishable and both round-trips break: `Set("k", "hello")` would be written as
  invalid JSON `hello`, and a persisted `"hello"` would be returned by `Get<string>` with its
  JSON quotes.
- `Load` runs `SELECT key, value FROM world_state` via `db.Execute` and feeds `LoadRows`;
  `LoadRows` stores every value as `RawJson` and seeds the baseline with the same strings.
  Splitting `LoadRows` out keeps the class unit-testable without a started `Database`
  (`Goose/Database.cs:171` throws otherwise, and `TestWorldFixture` never starts one).
- `Get<T>`: missing key → `default`. Stored `RawJson` → `JsonHelper.Deserialize<T>(json)`
  (so `Get<string>` on a persisted `"hello"` yields `hello`); on success cache the typed
  object in place of the `RawJson`, on `JsonException` log an error and return `default`
  (leave the `RawJson` in place). Stored value already `T` → return it. Stored live value of
  another type → round-trip via `JsonHelper.Deserialize<T>(JsonHelper.Serialize(value))`,
  log + `default` on failure.
- `Set` stores the live object. `Remove` drops the entry and records the key in a pending
  delete set.
- `PlanSave` is pure (no DB, no mutation of world state): for each value, serialize (`RawJson`
  passes through its text, live objects via `JsonHelper.Serialize`); emit an upsert when the
  baseline differs or is absent; emit deletes for pending-delete keys **not present in the
  live dictionary** (so remove-then-set in one cycle upserts — design "Delete-vs-upsert
  ordering"). The returned plan is treated as immutable once built.
- `ApplyCommit(plan)` runs under the private lock (from the DB-thread on-commit callback, or
  directly in tests): `baseline[key] := json` for every upsert, `baseline.Remove(key)` for
  every delete, `pendingDeletes.ExceptWith(plan.Deletes)`. Correctness comes from this
  per-plan reconciliation, not from lock ordering: a mutation landing between plan and commit
  either re-adds its key to `pendingDeletes` after the `ExceptWith`, or leaves a
  baseline/value mismatch, and the next `PlanSave` catches it. The SQL transaction must never
  read live state — only the plan.

**Step 1: Write the failing tests** (`Goose.Tests/WorldStateTests.cs`), using
`TestWorldFixture` only if a `GameWorld` is needed at all — these tests need none:

- `Get_MaterializesRawJsonOnce`: `LoadRows` with `("k", jsonOfItemSlotArray)` → first
  `Get<ItemSlot[]>` returns typed slots; second `Get` returns the same reference (cached).
- `Get_CorruptJson_LogsAndReturnsDefault`: `LoadRows` with `("k", "{ not json")` →
  `Get<ItemSlot[]>` is `null`, no throw.
- `StringValues_RoundTripAsJson`: `Set("k", "hello")` → `PlanSave` upsert json is
  `"\"hello\""`; after `LoadRows([("k", "\"hello\"")])`, `Get<string>` returns `hello`.
- `PlanSave_SkipsUnchangedValues`: `LoadRows([("k", json)])`, then
  `Set("k", JsonHelper.Deserialize<ItemSlot[]>(json))` → `Upserts` empty (live object
  serializes equal to baseline — this is the diff-on-save invariant).
- `PlanSave_EmitsChangedAndNewKeys`: set a second key → upsert contains only that key.
- `Remove_QueuesDelete`: `Remove("k")` → `Deletes` contains `"k"`.
- `RemoveThenSet_SameCycle_UpsertsNotDeletes` (adversarial: a naive save that applies deletes
  after upserts loses the row): `Remove("k"); Set("k", value)` → `Deletes` empty, `Upserts`
  contains `"k"`.
- `InterleavedRemove_AfterPlan_StillDeletedNextCycle` (adversarial for the commit race):
  value live and changed → `plan = PlanSave()` (upserts k, deletes empty) → `Remove("k")` →
  `ApplyCommit(plan)` → next `PlanSave` emits `Deletes` containing `"k"`.
- `InterleavedSet_AfterDeletePlan_RestoredNextCycle`: `Remove("k")` → `plan = PlanSave()`
  (deletes k) → `Set("k", v)` → `ApplyCommit(plan)` → next `PlanSave` upserts k.

**Step 2:** `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~WorldStateTests`
— red (class missing), then implement minimally — green.

**Step 3: Commit** — `feat(worldstate): in-memory key/value store with diff-on-save`

| Invariant | Proved by |
|-----------|-----------|
| Tenants never mark dirty; changed state is detected by diff | `PlanSave_SkipsUnchangedValues`, `PlanSave_EmitsChangedAndNewKeys` |
| Remove-then-set cannot lose the row | `RemoveThenSet_SameCycle_UpsertsNotDeletes` |
| Corrupt blob degrades to empty tenant, not crash | `Get_CorruptJson_LogsAndReturnsDefault` |
| Live strings and raw JSON blobs are distinct | `StringValues_RoundTripAsJson` |
| Post-plan mutations converge on the next cycle | both `Interleaved...` tests |

---

### Task 2: world_state table + real-DB round trip

**Files:**
- Create: `Goose/sql/world_state.sql`
- Modify: `Goose/GameWorld.cs:247` (schema list), `Goose/GameWorld.cs:272-281` (migration)
- Test: `Goose.IntegrationTests/WorldStatePersistenceTests.cs`

**Schema strategy:** automatic migration. `world_state.sql` joins the fresh-database list
(`"players", "banks", "logs", "pets", "guilds", "wordfilter"` → add `"world_state"`), and
`MigrateDatabaseSchema` gains
`CreateTableIfMissing(conn, "world_state", "key TEXT PRIMARY KEY, value TEXT NOT NULL")` so
existing databases get it on next boot (the `quest_status` precedent, `GameWorld.cs:277`).

```sql
CREATE TABLE IF NOT EXISTS world_state (
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL
);
```

`Goose.csproj:29-31` already copies `sql/*.sql` to test outputs — no csproj change.

**Tests** — `class WorldStatePersistenceTests : PlayerFirstSaveTestBase(["players",
"world_state"])` (`players.sql:86` creates `quest_status`, which the base's
`withQuestStatus: false` would then try to `DROP` unconditionally and throw —
`PlayerFirstSaveTests.cs:37-41`; leaving the default `true` keeps it. The `players` schema
costs nothing and matches `GuildSaveTests`' usage):

Every "drain" below means the synchronous fence `world.Database.Execute(conn => { });` —
`Execute` blocks on the queue (`Database.cs:163-189`), so when it returns every earlier
`EnqueueTransaction` **and its onCommit** has run. `PendingCount` excludes in-flight work
(`Database.cs:41-44`) and is racy; never use it as a barrier.

- `Set_Save_Load_RoundTripsTypedValue`: `Set("probe:key", slots)` where `slots` is a small
  `ItemSlot[]` → `Save(world)` (added in Task 3; until then call `PlanSave` +
  `EnqueueTransaction` + `ApplyCommit` inline — see note) → fence → new `WorldState`, `Load`,
  `Get<ItemSlot[]>` returns equal content.
- `Save_WritesNothing_WhenUnchanged`: save, fence, assert `PlanSave()` has empty upserts and
  deletes — an identical UPSERT would leave row count and bytes unchanged, so only the plan
  proves no SQL was emitted.
- `Remove_PersistsDelete`: set, save, fence, remove, save, fence → `Count("SELECT COUNT(*)
  FROM world_state WHERE key='probe:key'") == 0`.

Note: if Task 3 lands first, use its `Save(world)` directly; otherwise this task may add a
`Save(GameWorld)` stub that runs `PlanSave()` + `EnqueueTransaction` whose action executes
only the captured plan and whose onCommit calls `ApplyCommit(plan)`, as described in Task 3;
Task 3 then only adds the event/setting around it.

**Red/green:** tests fail (no table / no property), then pass. Run:
`dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter FullyQualifiedName~WorldStatePersistenceTests`.

**Commit** — `feat(persistence): world_state table with fresh and migrated schema paths`

---

### Task 3: Save cadence, settings, GameWorld wiring

**Files:**
- Create: `Goose/Events/WorldSaveEvent.cs`
- Modify: `Goose/WorldState.cs` (add `Save(GameWorld)`, `AddSaveEvent(GameWorld)`, baseline lock), `Goose/GameWorld.cs` (property + ctor, `Start` LoadStep after *Global Scripts* `:453` and before *Players* `:457`, `Stop` flush after the logs block `:519-527`), `Goose/GooseSettings.cs` (add `public int WorldSavePeriod { get; set; }` next to `GuildSavePeriod` `:96`), `Goose/GooseSettings.json` (common block, next to `"GuildSavePeriod": 300` `:108`)
- Test: `Goose.Tests/WorldStateSaveEventTests.cs`

**Save flow (the one tricky piece — threading):**

- `Save(GameWorld world)` runs on the game thread (event pump or `Stop`). Structure:

```csharp
public void Save(GameWorld world)
{
    try
    {
        var plan = PlanSave();
        if (plan.Upserts.Count == 0 && plan.Deletes.Count == 0) return;
        world.Database.EnqueueTransaction(
            conn => { /* DELETE plan.Deletes; UPSERT plan.Upserts — plan data only,
                        never live state */ },
            onCommit: () => ApplyCommit(plan));
    }
    finally
    {
        AddSaveEvent(world);
    }
}
```

- The transaction action executes **only the immutable captured plan** (deletes first, then
  upserts) — it must not read the live dictionary, which the DB thread cannot touch
  (`Database.cs:16-20` single-connection/thread model; `values` is game-thread state).
- `onCommit` executes **on the DB thread** (`Goose/Database.cs:249-253`) and only calls
  `ApplyCommit(plan)` (Task 1), which takes the private lock also held by `PlanSave` and
  reconciles baseline + pendingDeletes per plan. The lock gives mutual exclusion; the
  per-plan reconciliation gives correctness across interleavings — a lock alone would not.
  One-line comment justified here ("runs on the DB thread; plan-only").
- `finally` re-arm: the event pump dequeues before running `Ready` and drops the event on
  exception (`Goose/EventHandler.cs:337,355-366`), so a serialization or enqueue failure
  must not end the cadence. `AddSaveEvent` mirrors
  `GuildHandler.AddSaveEvent` (`Goose/GuildHandler.cs:113-120`) including the
  `Math.Max(1, world.Settings.WorldSavePeriod)` clamp (H6 rationale at `:116`).
- `GameWorld.Stop` calls `WorldState.Save(this)` inside try/catch like the `LogHandler`
  flush (`GameWorld.cs:519-527`) — the re-arm during shutdown is inert because no event pump
  runs after `Stop` begins.
- **Startup scheduling**: the `Start` load step calls `WorldState.Load(this.Database)` and
  then `WorldState.AddSaveEvent(this)` — mirroring `GuildHandler.LoadGuilds` +
  `AddSaveEvent` (`GameWorld.cs:375-379`). Without this the first periodic save is only
  scheduled by the first `Save`, which may never happen on an idle server.
- Setting default: `public int WorldSavePeriod { get; set; } = 300;` — property initializer,
  not just the json value, because existing `GooseSettings.json` files on disk predate the
  key and missing ints deserialize to 0 (`GooseSettingsLoader.cs:60-71` only fixes strings);
  a bare clamp would silently mean "save every second". Precedent: initializers at
  `GooseSettings.cs:97-99`. The json common block still gains `"WorldSavePeriod": 300`.

`WorldSaveEvent` is `GuildSaveEvent` verbatim with `world.WorldState.Save(world)`
(`Goose/Events/GuildSaveEvent.cs`).

**Tests:**

- `Save_NoChanges_EnqueuesNoDbWork`: fresh `WorldState` on fixture world →
  `Save(world)`; the fixture DB is not started, so any `Enqueue` would throw
  `InvalidOperationException` (`Database.cs:220`) — the test completing *is* the assertion.
  Additionally assert `PlanSave()` was empty (capture via a pre-call).
- `Save_RearmsWorldSaveEvent_EvenWhenSaveThrows`: settings `WorldSavePeriod` default 300 →
  `Save(world)` → `world.EventHandler.Count` increased and `Peek()` is a `WorldSaveEvent`
  (`EventHandler.cs:314,316`; internal access is available to Goose.Tests the same way
  `EventHandlerTests` uses it). Adversarial half: point the world at a started-but-closed
  failure path is awkward in unit scope — instead unit-test the `finally` by asserting the
  re-arm happens when `PlanSave` is empty (early `return` inside `try` must still re-arm —
  the `return` path runs `finally`), and leave the throw path to code review of the
  `finally` block itself.
- `MissingSettingDefaultsTo300`: `JsonSerializer.Deserialize<GooseSettings>("{}",
  JsonHelper.SettingsOptions).WorldSavePeriod == 300` (guards the initializer, not the
  json).
- `Baseline_Update_AfterCommit` (integration, added to `WorldStatePersistenceTests`): set →
  `Save` → fence → `PlanSave()` empty. Proves `ApplyCommit` ran on commit; would fail if the
  on-commit callback were dropped, since the fixture world never pumps completions.

**Commit** — `feat(persistence): periodic world state saves and startup/shutdown wiring`

| Invariant | Proved by |
|-----------|-----------|
| Unchanged world state writes no SQL | `Save_NoChanges_EnqueuesNoDbWork`, `Save_WritesNothing_WhenUnchanged` |
| Save cycle always re-arms, even on the early-return path | `Save_RearmsWorldSaveEvent_EvenWhenSaveThrows` |
| Existing configs get 300s, not 1s | `MissingSettingDefaultsTo300` |
| Baseline advances only after COMMIT | `Baseline_Update_AfterCommit` |

---

### Task 4: ItemContainer change event

**Files:**
- Modify: `Goose/ItemContainer.cs:25-34`
- Test: `Goose.Tests/ItemContainerSlotChangedTests.cs`

Add `public event Action<int, ItemSlot?, ItemSlot?>? SlotChanged;` (index, old, new) and
`public void NotifySlotChanged(int index)` which fires the event with the slot's current
value as both old and new — the announcement channel for **in-place** mutations
(`SwapSlots` stack merges mutate the destination `ItemSlot`; `ItemSlot.cs:75-79`).
`SetSlot` fires the event at its end **only when `!ReferenceEquals(existing, itemSlot)`** —
swaps produce no-op writes (same-slot drag sets the same reference back) and subscribers must
not see phantom replacements. The out-of-range early return (`ItemContainer.cs:27-31`) fires
nothing. No subscriber → null-delegate check only; bank and combine containers never
subscribe. Merge detection (comparing the captured pre-swap stack) is part 2's drag-path job;
this task only provides the mechanism.

**Tests:**

- `SetSlot_FiresWithOldAndNew`
- `SetSlot_SameReference_DoesNotFire` (adversarial: the naive implementation fires on every
  `SetSlot` and would make every swap broadcast two redundant packets in part 2)
- `SetSlot_OutOfRange_DoesNotFire`
- `NotifySlotChanged_FiresWithSameSlotBothSides` (the merge channel)

**Commit** — `feat(containers): SlotChanged event and NotifySlotChanged on ItemContainer`

---

### Task 5: PlayerHandler.PlayerRemoved event

**Files:**
- Modify: `Goose/PlayerHandler.cs:82-90`
- Test: `Goose.Tests/PlayerRemovedEventTests.cs`

Add `public event Action<Player>? PlayerRemoved;`; invoke at the end of
`RemovePlayer(Player)` after all maps are updated (`PlayerHandler.cs:84-88`).
`RemovePlayer(Socket)` (`:68-72`) delegates, so all three teardown paths — logout,
lost-connection logout, duplicate-login replacement — are covered by construction.
Subscribers must not mutate `players` from the handler; part 2's subscriber only touches its
own registry.

**Tests** (fixture `AddOnlinePlayer`, `TestWorldFixture.cs:107`):

- `RemovePlayer_FiresOnceWithPlayer`
- `RemovePlayerBySocket_Fires` (guards the delegation path)

**Commit** — `feat(players): PlayerRemoved event on session teardown`

---

## Design alignment check (part 1 promises)

- API list matches design §WorldState (`Load/Get/Set/Remove/Save/KeysWithPrefix`);
  `LoadRows`/`PlanSave` are internal test seams, not new design surface.
- Table DDL and both schema paths match the design bullet "Table".
- Diff-on-save with immutable plan + per-plan `ApplyCommit` reconciliation matches design
  §WorldState "Dirty tracking"; delete-vs-upsert rule matches "Delete-vs-upsert ordering";
  `RawJson` wrapper matches the design's value-representation paragraph.
- `WorldSavePeriod` default 300 via property initializer + json; startup `AddSaveEvent`;
  `finally` re-arm — all match design §WorldState "Save cadence".
- `SlotChanged` fires from `SetSlot` on reference change; `NotifySlotChanged` is the
  in-place-mutation channel — design §ItemContainer change event.
- `PlayerRemoved` fires at end of `RemovePlayer` — design §Viewer registry bullet.

Not in this part (by design): `ChestHandler`, windows, validation, right-click wiring,
`CommunityChestPages` — all part 2.
