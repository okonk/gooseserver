# One-Time Quests Implementation Plan

**Goal:** Make `Quest.OnlyOnePlayerCanComplete` functional: the first completion claims the quest
server-wide, hides it from non-starters, and blocks all later completions with a message naming the
claimer.

**Architecture:** New live table `quest_claims` (never touched by the spreadsheet re-import) is
mirrored in `QuestHandler.Claims`, loaded at startup and written through on completion. Gating
lives in the four existing quest flow points (`GetAvailableQuests`, `StartQuest`,
`QuestStateResolver.IsAvailable`, `QuestWindow.Clicked`/`CompleteQuest`). The game thread is
single-threaded, so first-completer-wins needs no locking.

**Tech Stack:** C# / .NET 10, System.Data.SQLite, xUnit. Design:
`docs/plans/2026-10-02-one-time-quests-design.md`.

---

## APIs verified

- `Quest.OnlyOnePlayerCanComplete` — `Goose/Quests/Quest.cs:25` (loaded at `:59`)
- `QuestHandler.Quests` / `LoadQuests(GameWorld)` — `Goose/Quests/QuestHandler.cs:7,14` (single
  `world.Database.Execute` block)
- `QuestWindow` — enum `QuestWindowState` `Goose/Quests/QuestWindow.cs:7-17`;
  `GetAvailableQuests(NPC, Player)` `:97` (callers: `:55`, `Goose.Tests/QuestCompletionTests.cs:95`);
  `StartQuest` `:121` (callers `:60,:71`; already no-ops when started, `:123`);
  `Clicked` meets-requirements branch `:217`; `CompleteQuest` `:350` (started-removal `:361`,
  progress-removal `:364`); window text uses literal `"\\n"` (`:173`)
- `QuestStateResolver.IsAvailable(Quest, Player)` — `Goose/Quests/QuestStateResolver.cs:35`; only
  caller is `Resolve` `:66` (has `world`). `IsActive` `:12` already makes starters fail
  `IsAvailable` via `!IsActive` `:40`
- `QuestHandler.RefreshIcons(viewer, world)` — `Goose/Quests/QuestHandler.cs:108`; no-op unless
  `viewer.State == Ready && viewer.Map is not null` (`:110`) — safe in DB-less unit fixtures
- `GameWorld.CreateDatabaseSchema` file array — `Goose/GameWorld.cs:245-248` (runs only when the DB
  file is new, `:335-340`); `MigrateDatabaseSchema` `:272` (every startup; contains
  `CreateTableIfMissing` `:304`, an `internal static` helper); `LogSchemaMigrator.Migrate(conn)`
  runs there too and creates its own tables (`Goose/Logs/LogSchemaMigrator.cs:56`)
- `Database.Execute(Action)` — `Goose/Database.cs:166`; `Execute<T>(Func)` `:191`;
  `Enqueue(Action<SQLiteConnection>, Action<Exception?>? = null)` `:211` — throws
  `InvalidOperationException` if the DB was never `Start`ed; queue is FIFO, so a later synchronous
  `Execute` is a flush barrier for earlier `Enqueue`d work
- `PlayerHandler` — private `allNameToPlayer` holds every non-deleted player
  (`LoadPlayerData`, `Goose/PlayerHandler.cs:185+`); `GetPlayerFromData` `:175`
- `Player.PlayerID` — `Goose/Player.cs:102`, settable; the `Player(int)` ctor parameter is
  **unused** (`Player.cs:569`) — tests must assign `PlayerID` explicitly.
  `QuestsCompleted`/`QuestsStarted` internal, `Player.cs:462` region; `QuestProgress` list holds
  `QuestProgress { Requirement, Value }` (`Goose/Quests/QuestProgress.cs`)
- `NPC.Quests` internal settable — `Goose/NPC.cs:365`; test pattern
  `fixture.Npc.Quests = [fixture.Quest];` (`Goose.Tests/QuestCompletionTests.cs:91`)
- Unit fixture — `TestSupport/TestWorldFixture.cs`: `CommandPlayerOn` `:100` (State=Ready, Map set),
  `RegisterDatabasePlayer` `:123` (fills `allNameToPlayer`), `RunCommand` `:138`. Fixture does NOT
  start the DB; precedent for starting it: `Goose.Tests/GrantTitleCommandTests.cs:93-99`
  (`World.Database.Start(temp test.db)` then run `sql/players.sql` from `AppContext.BaseDirectory`)
- Integration base — `PlayerFirstSaveTestBase(schemaFiles, generatedTables)`
  (`Goose.IntegrationTests/PlayerFirstSaveTests.cs:20-40`); `SchemaDdl.ForAll(...)`
  (`TestSupport/SchemaDdl.cs`) generates `quests`/`quest_requirements`/`quest_rewards` DDL
  (`CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs:49-51`)
- `sql/*.sql` copies to Goose output and lands in test `BaseDirectory/sql/`
  (`Goose/Goose.csproj:29-31`; confirmed in `Goose.Tests/bin/Debug/net10.0/sql/`)
- `InternalsVisibleTo Goose.Tests / Goose.IntegrationTests` — `Goose/Goose.csproj:19-24`
- Clicked driving pattern — `window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID,
  0, 0, player, world)` (`Goose.Tests/QuestCompletionTests.cs:59`)
- Upsert precedent for write-through — `Goose/Player.cs:1320-1326`

Per AGENTS.md: no new comments unless a "why" is non-obvious (the single-game-thread invariant and
the `INSERT OR REPLACE` crash-reopen rationale qualify, one line each).

---

### Task 0: `QuestClaim` + `quest_claims` table + schema wiring

**Files:**
- Create: `Goose/Quests/QuestClaim.cs`
- Create: `Goose/sql/quest_claims.sql`
- Modify: `Goose/GameWorld.cs:245-248` (file array), `:272` (`MigrateDatabaseSchema`, also make it
  `internal`)
- Test: `Goose.IntegrationTests/QuestClaimSchemaTests.cs`

**Step 1: Write the failing tests**

```csharp
public class QuestClaimSchemaTests : PlayerFirstSaveTestBase
{
    public QuestClaimSchemaTests() : base(["players"]) { }

    [Fact]
    public void Fresh_schema_script_creates_the_table()
    {
        world.Database.Execute(conn => RunSqlFile(conn, "quest_claims"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='quest_claims'"));
    }

    [Fact]
    public void Migration_creates_the_table_on_an_existing_database()
    {
        world.MigrateDatabaseSchema();
        Assert.Equal(1, Count("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='quest_claims'"));
    }
}
```

`RunSqlFile` mirrors `PlayerFirstSaveTestBase`'s private `RunSql` reading
`Path.Combine(AppContext.BaseDirectory, "sql", name + ".sql")`. The second test is the adversarial
one: an implementation that only ships the `.sql` file (fresh-DB-only) fails it.

**Step 2: Red** — `dotnet test Goose.IntegrationTests --filter QuestClaimSchemaTests` fails to
compile (`MigrateDatabaseSchema` private / no sql file).

**Step 3: Implement**

```sql
-- Goose/sql/quest_claims.sql
CREATE TABLE quest_claims (
  quest_id INT PRIMARY KEY,
  player_id INT NOT NULL,
  completed_at TEXT NOT NULL
);
```

- Add `"quest_claims"` to the `CreateDatabaseSchema` array (`Goose/GameWorld.cs:245-248`).
- In `MigrateDatabaseSchema` add
  `CreateTableIfMissing(conn, "quest_claims", "quest_id INT PRIMARY KEY, player_id INT NOT NULL, completed_at TEXT NOT NULL");`
  and change the method to `internal void MigrateDatabaseSchema()` (matches the existing
  `internal static` schema helpers; needed for the migration test).
- `Goose/Quests/QuestClaim.cs`:

```csharp
namespace Goose.Quests
{
    public class QuestClaim
    {
        public int QuestId { get; set; }
        public int PlayerId { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
```

**Step 4: Green** — same filter passes. **Step 5: Commit**
`feat(quests): quest_claims table for one-time quest claims`

---

### Task 1: `QuestHandler` claim cache, load, and write-through

**Files:**
- Modify: `Goose/Quests/QuestHandler.cs` (fields near `:7`, `LoadQuests` `:14-71`, new methods)
- Test: `Goose.IntegrationTests/QuestClaimPersistenceTests.cs` (write-through + reload),
  `Goose.Tests/QuestOneTimeTests.cs` (starts here, dictionary-only tests)

**Step 1: Write the failing tests**

Unit (no DB started; asserts pure dictionary surface):

```csharp
[Fact]
public void No_quest_is_claimed_before_any_claim_exists()
{
    using var fixture = new TestWorldFixture();
    Assert.False(fixture.World.QuestHandler.IsClaimed(9));
}
```

Integration (real DB; this is the risky axis — SQL, queue, round-trip):

```csharp
public class QuestClaimPersistenceTests : PlayerFirstSaveTestBase
{
    public QuestClaimPersistenceTests()
        : base(["players"], ["quests", "quest_requirements", "quest_rewards"]) { }

    [Fact]
    public void Claiming_writes_a_row_and_reload_restores_it()
    {
        InsertQuestRow(9, oneTime: true);
        var player = new Player(0) { Name = "Hero", PlayerID = 7 };

        world.QuestHandler.Claim(world.QuestHandler.Get(9)!, player, world);
        Assert.Equal(1, Count("SELECT COUNT(*) FROM quest_claims WHERE quest_id=9 AND player_id=7"));

        var reloaded = new QuestHandler();
        reloaded.LoadQuests(world);
        Assert.True(reloaded.IsClaimed(9));
        Assert.True(reloaded.TryGetClaim(9, out var claim));
        Assert.Equal(7, claim.PlayerId);
    }
}
```

`InsertQuestRow` inserts into `quests` all 14 columns `Quest.FromReader` reads
(`Goose/Quests/Quest.cs:41-60`; bools are TEXT `'0'`/`'1'`). Adversarial: an in-memory-only
implementation fails the reload half; a load that ignores `player_id` fails the last assert.

**Step 2: Red** — `Claim`/`IsClaimed`/`TryGetClaim`/`Claims` don't exist.

**Step 3: Implement on `QuestHandler`**

```csharp
public Dictionary<int, QuestClaim> Claims { get; } = [];

public bool IsClaimed(int questId) => Claims.ContainsKey(questId);

public bool TryGetClaim(int questId, out QuestClaim claim) => Claims.TryGetValue(questId, out claim!);

public void Claim(Quest quest, Player player, GameWorld world)
{
    Claims[quest.Id] = new QuestClaim
    {
        QuestId = quest.Id,
        PlayerId = player.PlayerID,
        CompletedAt = DateTime.UtcNow,
    };

    // OR REPLACE: a crash between claim and flush re-opens the quest, and re-claiming must not
    // hit the primary key. Runs on the game thread only; the dictionary is the source of truth.
    var claim = Claims[quest.Id];
    world.Database.Enqueue(conn =>
    {
        using var command = conn.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO quest_claims (quest_id, player_id, completed_at) VALUES (@quest_id, @player_id, @completed_at)";
        command.Parameters.Add(new SQLiteParameter("@quest_id", DbType.Int32) { Value = claim.QuestId });
        command.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = claim.PlayerId });
        command.Parameters.Add(new SQLiteParameter("@completed_at", DbType.String) { Value = claim.CompletedAt.ToString("o") });
        command.ExecuteNonQuery();
    }, e => { if (e is not null) log.Error(e, "Failed to persist quest claim {0}", quest.Id); });
}
```

Add the NLog `log` field (pattern from `Goose/Database.cs:14`) and
`using System.Data.SQLite;` (upsert precedent: `Goose/Player.cs:1320-1326`). At the end of
`LoadQuests`' existing `Execute` block:

```csharp
this.Claims.Clear();
using (var command = conn.CreateCommand())
{
    command.CommandText = "SELECT quest_id, player_id, completed_at FROM quest_claims";
    using (var reader = command.ExecuteReader())
    {
        while (reader.Read())
        {
            this.Claims[reader.GetInt32("quest_id")] = new QuestClaim
            {
                QuestId = reader.GetInt32("quest_id"),
                PlayerId = reader.GetInt32("player_id"),
                CompletedAt = DateTime.Parse(reader.GetString("completed_at"), null, System.Globalization.DateTimeStyles.RoundtripKind),
            };
        }
    }
}
```

(`GetString`/`GetInt32` by name come from `Goose/DataReaderExtensions.cs`, already used at
`Quest.cs:41`.)

**Mutation impact:**
- Source of truth changed: `QuestHandler.Claims` (runtime), `quest_claims` table (durable).
- Important readers: `QuestStateResolver` and `QuestWindow` gating (Task 3-4); startup load in
  `LoadQuests`; nothing serializes claims to clients.
- Derived state: none beyond the dictionary/table pair.
- Propagation: game thread mutates dictionary → `Enqueue` INSERT → DB thread applies; a later
  synchronous `Execute` (FIFO, `Database.cs:211`) observes it.
- Invariants: dictionary and table agree after flush; reload repopulates; failed INSERT logs and
  leaves the dictionary authoritative for the session.
- Proof: the round-trip test above asserts the row AND the reloaded `TryGetClaim` values.

**Step 4: Green** — `dotnet test` both filters. **Step 5: Commit**
`feat(quests): quest claim cache with write-through persistence`

---

### Task 2: `PlayerHandler.GetPlayerName`

**Files:**
- Modify: `Goose/PlayerHandler.cs` (near `GetPlayerFromData` `:175`)
- Test: `Goose.Tests/QuestOneTimeTests.cs`

**Step 1: Failing test** — register an offline player via
`fixture.RegisterDatabasePlayer(...)` (`TestWorldFixture.cs:123`, sets `allNameToPlayer`), assert
`GetPlayerName(id)` returns the name and `null` for an unknown id.

**Step 2: Red** — method missing.

**Step 3: Implement**

```csharp
public string? GetPlayerName(int playerId)
{
    return this.allNameToPlayer.Values.FirstOrDefault(p => p.PlayerID == playerId)?.Name;
}
```

O(n) scan is fine: the only caller builds a rare blocked message.

**Step 4: Green. Step 5: Commit** `feat(players): resolve player name by id from loaded data`

---

### Task 3: Hide claimed quests from new starters

**Files:**
- Modify: `Goose/Quests/QuestWindow.cs:97-119` (`GetAvailableQuests` +`world` param), `:55` and
  `:121-140` (`StartQuest` guard), `Goose/Quests/QuestStateResolver.cs:35-43,66`
  (`IsAvailable` +`world` param)
- Modify: `Goose.Tests/QuestCompletionTests.cs:95` (call-site signature)
- Test: `Goose.Tests/QuestOneTimeTests.cs`

**Step 1: Failing tests**

1. Claimed one-time quest absent from `QuestWindow.GetAvailableQuests(npc, player, world)` for a
   fresh player; present when `player.QuestsStarted.Add(quest)` first (starter exception —
   adversarial against a blanket hide).
2. `QuestStateResolver.Resolve` returns `None` (not `Available`) for a claimed quest on an
   otherwise-eligible non-starter; returns `Ready` for a starter who meets requirements (the
   starter path goes through `IsActive` `QuestStateResolver.cs:12`, untouched).
3. `StartQuest` on a claimed quest leaves `QuestsStarted` empty (stale-window race).

**Step 2: Red.**

**Step 3: Implement** — one rule everywhere, no identity comparison:

- `GetAvailableQuests(NPC npc, Player player, GameWorld world)`: skip when
  `quest.OnlyOnePlayerCanComplete && world.QuestHandler.IsClaimed(quest.Id) &&
  !player.QuestsStarted.Any(q => q.Id == quest.Id)`. Update callers `QuestWindow.cs:55` and
  `QuestCompletionTests.cs:95`.
- `IsAvailable(Quest, Player, GameWorld)`: return false when
  `quest.OnlyOnePlayerCanComplete && world.QuestHandler.IsClaimed(quest.Id)`. No starter exception
  needed — starters already fail via `!IsActive` (`:40`). Update `Resolve` `:66`.
- `StartQuest`: after the already-started early return (`:123`), refuse when claimed and not
  started.

**Mutation impact:** no state mutated — read-path gating over `Claims` only. Propagation is the
existing icon send in `QuestHandler.SendIcon` (`QuestHandler.cs:91`) which now resolves to the new
state.

**Step 4: Green — full Goose.Tests suite** (regression: `QuestCompletionTests`,
`QuestIcon*Tests` must stay green; non-one-time quests take the identical old path).
**Step 5: Commit** `feat(quests): hide claimed one-time quests from new starters`

---

### Task 4: Turn-in block, eviction, and claim write

**Files:**
- Modify: `Goose/Quests/QuestWindow.cs` (enum `:7-17`, `GetCurrentText` `:158-195`, `Clicked`
  `:217-237`, `CompleteQuest` `:350-355`)
- Test: `Goose.Tests/QuestOneTimeTests.cs`

**Step 1: Failing tests**

Fixture note: tests that run a *successful* completion of a one-time quest need a started DB
(`Claim` → `Enqueue` throws otherwise, `Database.cs:215`): follow
`GrantTitleCommandTests.cs:93-99` — `World.Database.Start(temp)` + run `sql/quest_claims.sql` from
`AppContext.BaseDirectory`. Block-path tests need no DB.

1. Second player attempts turn-in of a claimed quest (claim injected into
   `QuestHandler.Claims`; use a Gold requirement so consumption is observable): after
   `Clicked(Next, ...)`, the window's `GetCurrentText` contains the claimer's name (register the
   claimer with `RegisterDatabasePlayer`) or `"Someone"` when unregistered; `player.Gold` unchanged;
   quest absent from `QuestsStarted`; its `QuestProgress` entries dropped; quest NOT in
   `QuestsCompleted`. Adversarial: any implementation that completes-then-blocks fails the
   gold/completed asserts; evicting only the started list fails the progress assert (stale kills
   would still credit — see `QuestWindow.cs:357-358`).
2. First completion claims: with DB started, complete via window flow, then
   `QuestHandler.IsClaimed(quest.Id)` is true and a second player's identical flow is blocked.
3. `Repeatable = true` + one-time: claimer completes once (claim written), then re-starts and
   re-turns-in — blocked, no rewards.
4. Regression: `OnlyOnePlayerCanComplete = false` completion path unchanged (existing
   `QuestCompletionTests` cover this; keep green).

**Step 2: Red.**

**Step 3: Implement**

- Add `QuestAlreadyClaimed` to `QuestWindowState`.
- `GetCurrentText` case:

```csharp
case QuestWindowState.QuestAlreadyClaimed:
    world.QuestHandler.TryGetClaim(quest.Id, out var claim);
    var claimer = claim is not null ? world.PlayerHandler.GetPlayerName(claim.PlayerId) : null;
    text = $"{claimer ?? "Someone"} has already completed this quest.\\nIt can only be completed once.";
    break;
```

- `Clicked`, first branch inside `if (this.PlayerMeetsRequirements(...))` (`:217`), before the
  inventory check:

```csharp
if (quest.OnlyOnePlayerCanComplete && world.QuestHandler.IsClaimed(quest.Id))
{
    player.QuestsStarted.RemoveAll(q => q.Id == quest.Id);
    player.QuestProgress.RemoveAll(p => p.Requirement.Quest.Id == quest.Id);
    world.QuestHandler.RefreshIcons(player, world);
    this.state = QuestWindowState.QuestAlreadyClaimed;
}
else if (!this.PlayerHasEnoughInventorySpaceForReward(player, world))
...
```

- `CompleteQuest`, after `QuestsCompleted.Add` (`:354`):

```csharp
if (quest.OnlyOnePlayerCanComplete && !world.QuestHandler.IsClaimed(quest.Id))
    world.QuestHandler.Claim(quest, player, world);
```

**Mutation impact:**
- Source of truth: `QuestHandler.Claims` + `quest_claims` (Task 1); `player.QuestsStarted` /
  `player.QuestProgress` (persisted by `Player.SaveToDatabase`, `Goose/Player.cs:1310-1313`).
- Important readers: `/quests` via `GetActiveQuests` (`Goose/Commands/QuestsCommand.cs:13`) —
  sees the eviction; kill/`TalkToNPC` crediting reads `QuestProgress`; icons read both via
  `QuestStateResolver.Resolve`.
- Derived state: NPC icons. Propagation: `RefreshIcons` sends per-tile icon packets for this viewer
  (`QuestHandler.cs:108-115`); other viewers' stale Ready icons self-correct on click (blocked +
  evicted) or relog — accepted design gap, no broadcast.
- Invariants: blocked turn-in consumes nothing and completes nothing; eviction persists with the
  next player save; first completer's claim is in the dictionary before any other player's
  `Clicked` can run (single game thread).
- Proof: test 1 asserts gold/started/progress/completed end state, not that a helper was called;
  test 2 proves claim-through-real-flow including the DB write.

**Step 4: Green — full Goose.Tests suite. Step 5: Commit**
`feat(quests): block turn-in of claimed one-time quests and evict from log`

---

### Task 5: End-to-end persistence proof + full suite

**Files:**
- Modify: `Goose.IntegrationTests/QuestClaimPersistenceTests.cs`
- Test command: `dotnet test Goose.Tests && dotnet test Goose.IntegrationTests`

**Step 1: Add the restart test** — complete a one-time quest through the real window flow against
the real DB (player with gold requirement satisfied, `QuestsStarted` seeded, `Clicked(Next)`),
flush via a synchronous `Execute`, then `new QuestHandler().LoadQuests(world)` and assert the
second handler claims it and `GetAvailableQuests` hides it from a fresh player. This is the
design's "claim survives restart" promise on the real save/load machinery.

**Step 2-4:** red (fails only if the write-through or load regresses), green, then run both full
suites (baseline: 1611 + 430 passing).

**Step 5: Commit** `test(quests): one-time quest claims survive reload end-to-end`

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Existing DBs get the table automatically | Task 0 `Migration_creates_the_table_on_an_existing_database` |
| Claim round-trips dictionary ↔ table | Task 1 `Claiming_writes_a_row_and_reload_restores_it` |
| Claimed quest invisible to non-starters, visible to starters | Task 3 tests 1-2 |
| Blocked turn-in mutates nothing but evicts | Task 4 test 1 (adversarial) |
| First completer wins, everyone else blocked forever | Task 4 test 2; Task 5 restart test |
| Claimer cannot re-complete a repeatable one-time quest | Task 4 test 3 |
| Non-one-time quests unchanged | Full existing suites (Tasks 3-5) |

Deferred (from design): no admin reset command; no icon broadcast to other viewers; orphan claim
rows need manual `DELETE`.
