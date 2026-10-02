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
    internal (List<(string Key, string Json)> Upserts, List<string> Deletes) PlanSave();
}
```

- `Load` runs `SELECT key, value FROM world_state` via `db.Execute` and feeds `LoadRows`;
  `LoadRows` stores every value as the raw JSON **string** and seeds the baseline with the
  same strings. Splitting `LoadRows` out keeps the class unit-testable without a started
  `Database` (`Goose/Database.cs:171` throws otherwise, and `TestWorldFixture` never starts
  one).
- `Get<T>`: missing key → `default`. Stored value already `T` → return it. Stored string →
  `JsonHelper.Deserialize<T>`; on success cache the typed object back into the dictionary
  (materialization), on `JsonException` log an error and return `default` (leave the string
  in place). Any other type → log error, return `default`.
- `Set` stores the live object. `Remove` drops the entry and records the key in a pending
  delete set.
- `PlanSave` is pure (no DB, no mutation): for each value, serialize (strings pass through,
  objects via `JsonHelper.Serialize`); emit an upsert when the baseline differs or is absent;
  emit deletes for pending-delete keys **not present in the live dictionary** (so
  remove-then-set in one cycle upserts — design "Delete-vs-upsert ordering").

**Step 1: Write the failing tests** (`Goose.Tests/WorldStateTests.cs`), using
`TestWorldFixture` only if a `GameWorld` is needed at all — these tests need none:

- `Get_MaterializesRawJsonOnce`: `LoadRows` with `("k", jsonOfItemSlotArray)` → first
  `Get<ItemSlot[]>` returns typed slots; second `Get` returns the same reference (cached).
- `Get_CorruptJson_LogsAndReturnsDefault`: `LoadRows` with `("k", "{ not json")` →
  `Get<ItemSlot[]>` is `null`, no throw.
- `PlanSave_SkipsUnchangedValues`: `LoadRows([("k", json)])`, then
  `Set("k", JsonHelper.Deserialize<ItemSlot[]>(json))` → `Upserts` empty (live object
  serializes equal to baseline — this is the diff-on-save invariant).
- `PlanSave_EmitsChangedAndNewKeys`: set a second key → upsert contains only that key.
- `Remove_QueuesDelete`: `Remove("k")` → `Deletes` contains `"k"`.
- `RemoveThenSet_SameCycle_UpsertsNotDeletes` (adversarial: a naive save that applies deletes
  after upserts loses the row): `Remove("k"); Set("k", value)` → `Deletes` empty, `Upserts`
  contains `"k"`.

**Step 2:** `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~WorldStateTests`
— red (class missing), then implement minimally — green.

**Step 3: Commit** — `feat(worldstate): in-memory key/value store with diff-on-save`

| Invariant | Proved by |
|-----------|-----------|
| Tenants never mark dirty; changed state is detected by diff | `PlanSave_SkipsUnchangedValues`, `PlanSave_EmitsChangedAndNewKeys` |
| Remove-then-set cannot lose the row | `RemoveThenSet_SameCycle_UpsertsNotDeletes` |
| Corrupt blob degrades to empty tenant, not crash | `Get_CorruptJson_LogsAndReturnsDefault` |

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

**Tests** — `class WorldStatePersistenceTests : PlayerFirstSaveTestBase(["world_state"], withQuestStatus: false)`:

- `Set_Save_Load_RoundTripsTypedValue`: `Set("probe:key", slots)` where `slots` is a small
  `ItemSlot[]` built from a fixture template → `world.WorldState.Save(world)` (added in Task
  3; until then call `PlanSave` + `Database.EnqueueTransaction` inline — see note) → wait
  `while (world.Database.PendingCount > 0) Thread.Sleep(10);` → new `WorldState`, `Load`,
  `Get<ItemSlot[]>` returns equal content.
- `Save_WritesNothing_WhenUnchanged`: save once, drain, record
  `SELECT COUNT(*) FROM world_state`, mutate nothing, save, drain → count unchanged **and**
  the row bytes identical (`Execute<string>` on `value`).
- `Remove_PersistsDelete`: set, save, drain, remove, save, drain → `Count("SELECT COUNT(*)
  FROM world_state WHERE key='probe:key'") == 0`.

Note: if Task 3 lands first, use its `Save(world)` directly; otherwise this task may add a
`Save(GameWorld)` stub that runs `PlanSave()` + `EnqueueTransaction` with the on-commit
baseline update described in Task 3, and Task 3 only adds the event/setting around it.

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

- `Save(GameWorld world)` runs on the game thread (event pump or `Stop`). It calls
  `PlanSave()`; if both lists are empty it still re-arms the event and returns without
  touching the database.
- Non-empty → `world.Database.EnqueueTransaction(conn => { deletes then upserts as parameterized
  `INSERT INTO world_state(key, value) VALUES(@k,@v) ON CONFLICT(key) DO UPDATE SET value=@v`;
  deletes only for keys still absent from the live dictionary }, onCommit)`.
- `onCommit` executes **on the DB thread** (`Goose/Database.cs:249-253`). It must not touch
  game-thread state directly: guard `baseline` and `pendingDeletes` with one private lock
  object, taken by both `PlanSave` (game thread) and `onCommit` (DB thread), which applies
  the captured snapshot (baseline := snapshot entries, remove committed keys from
  pendingDeletes). This is the `Guild.BuildSave` recompute pattern (`Goose/Guild.cs:274,389`)
  with a lock instead of a sequence counter. One-line comment justified here ("onCommit runs
  on the DB thread").
- Deletes-before-upserts inside the transaction plus the "live key is never deleted" rule
  make the remove-then-set ordering safe at the SQL level too.
- End of `Save`: `AddSaveEvent(world)` — mirror of `GuildHandler.Save` re-arm
  (`Goose/GuildHandler.cs:106,113-120`), including `Math.Max(1, world.Settings.WorldSavePeriod)`
  (fixture settings leave it 0; the clamp rationale is the H6 comment at `:116`).
- `GameWorld.Stop` calls `WorldState.Save(this)` inside try/catch like the `LogHandler` flush
  (`GameWorld.cs:519-527`) — the re-arm during shutdown is inert because no event pump runs
  after `Stop` begins.

`WorldSaveEvent` is `GuildSaveEvent` verbatim with `world.WorldState.Save(world)`
(`Goose/Events/GuildSaveEvent.cs`).

**Tests:**

- `Save_NoChanges_EnqueuesNoDbWork`: fresh `WorldState` on fixture world →
  `Save(world)`; assert `world.Database.PendingCount == 0` (`Database.cs:43`) — but the
  fixture DB is not started, so `Enqueue` would throw if called: the test passing *is* the
  assertion (no throw, no work).
- `Save_RearmsWorldSaveEvent`: `Save(world)` → `world.EventHandler.Count` increased and
  `Peek()` is a `WorldSaveEvent` (`EventHandler.cs:314,316`; internal access is available to
  Goose.Tests the same way `EventHandlerTests` uses it).
- `Baseline_Update_AfterCommit` (integration, added to `WorldStatePersistenceTests`): after
  save + drain, a second `Save` produces no work — proves the on-commit baseline actually
  applied (would fail if onCommit were dropped, since the fixture world never pumps
  completions).

**Commit** — `feat(persistence): periodic world state saves and startup/shutdown wiring`

| Invariant | Proved by |
|-----------|-----------|
| Unchanged world state writes no SQL | `Save_NoChanges_EnqueuesNoDbWork`, `Save_WritesNothing_WhenUnchanged` |
| Save cycle always re-arms | `Save_RearmsWorldSaveEvent` |
| Baseline advances only after COMMIT | `Baseline_Update_AfterCommit` |

---

### Task 4: ItemContainer.SlotChanged event

**Files:**
- Modify: `Goose/ItemContainer.cs:25-34`
- Test: `Goose.Tests/ItemContainerSlotChangedTests.cs`

Add `public event Action<int, ItemSlot?, ItemSlot?>? SlotChanged;` (index, old, new). Fire it
at the end of `SetSlot` **only when `!ReferenceEquals(existing, itemSlot)`** — swaps produce
no-op writes (same-slot drag sets the same reference back) and subscribers must not see
phantom changes. The out-of-range early return (`ItemContainer.cs:27-31`) fires nothing.
No subscriber → null-delegate check only; bank and combine containers never subscribe.

**Tests:**

- `SetSlot_FiresWithOldAndNew`
- `SetSlot_SameReference_DoesNotFire` (adversarial: the naive implementation fires on every
  `SetSlot` and would make every swap broadcast two redundant packets in part 2)
- `SetSlot_OutOfRange_DoesNotFire`

**Commit** — `feat(containers): SlotChanged event on ItemContainer writes`

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
- Diff-on-save with on-commit baseline matches the design bullet "Dirty tracking";
  delete-vs-upsert rule matches "Delete-vs-upsert ordering".
- `WorldSavePeriod` default 300 in the json common block; clamp covers fixture/missing
  settings, matching the `GuildSavePeriod` precedent.
- `SlotChanged` fires from `SetSlot` only — design §ItemContainer change event.
- `PlayerRemoved` fires at end of `RemovePlayer` — design §Viewer registry bullet.

Not in this part (by design): `ChestHandler`, windows, validation, right-click wiring,
`CommunityChestPages` — all part 2.
