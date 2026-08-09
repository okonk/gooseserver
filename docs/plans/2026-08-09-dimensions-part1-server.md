# Dimensions Part 1 — Server Extension Points Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add the eight generic server extension points the dimensions feature needs, plus the schema-migration mechanism the project currently lacks, with no behaviour change until Part 2's scripts arrive.

**Architecture:** Every change is generic — nothing in the server mentions dimensions. Scripts get a per-player property bag, the ability to register NPC templates, spawn NPCs and register quests, a supported map-clone API, a map entry-gate hook, and 64-bit HP/damage so the scaling formulas don't overflow.

**Two of these tasks exist because a Part 2 script would otherwise have to reach around the server and get it subtly wrong:** `SpawnNPC` (Task 4), because `LoadFromTemplate` alone leaves NPCs out of `NPCHandler.NPCCount`; and `Map.CloneAs` (Task 5), because `requiredItems` is private and a hand-built clone silently drops item-gated entry.

**Tech Stack:** C# / .NET 10, SQLite (System.Data.SQLite), xUnit, Roslyn C# scripting (`.csx`).

**Design doc:** `docs/plans/2026-08-09-dimensions-design.md`

**Out of scope — Part 2:** `Scripts/Global/Dimensions.csx` and `Scripts/Map/DimensionMap.csx`, world cloning, NPC scaling, `/dimension`, the warden and quest chain. Do not write them here.

---

## APIs verified

Every citation below was read from source in this worktree before writing the plan.

| Fact | Location |
|---|---|
| `Player.LoadFromReader` indexes reader by column name | `Goose/Player.cs:655–700` |
| `Player` INSERT query + `SQLiteParameter` binding | `Goose/Player.cs:842–913` |
| `Player` UPDATE query + `SQLiteParameter` binding | `Goose/Player.cs:918–982` |
| `AttributeSet.HP` / `.MP` are `int` | `Goose/AttributeSet.cs:14,15` |
| `AttributeSet.operator*` casts HP/MP back to `int` | `Goose/AttributeSet.cs:180,181` |
| `AttributeSet.Clone` copies HP/MP | `Goose/AttributeSet.cs:75,76` |
| `ICharacter.CurrentHP`/`MaxHP`/`MP` are **already** `long` | `Goose/ICharacter.cs:48–60` |
| `ICharacter.WeaponDamage` is `int` | `Goose/ICharacter.cs:88` |
| `NPC.WeaponDamage` is `int`; used in `NPC.Attack` | `Goose/NPC.cs:302`, `:1370` |
| `Player.WeaponDamage` is `virtual int` over `Inventory.GetWeaponDamage()` | `Goose/Player.cs:1572`, `Goose/Inventory.cs:821` |
| `Pet.WeaponDamage` is `override int`, read with `Convert.ToInt32` | `Goose/Pet.cs:76`, `:265` |
| `NPCHandler` reads HP/MP/damage with `Convert.ToInt32` | `Goose/NPCHandler.cs:54,81,82` |
| `SpellEffect` formula table is `Dictionary<string, decimal>` | `Goose/SpellEffect.cs:1269` |
| `NPCTemplate.WeaponDamage` is `int` | `Goose/NPCTemplate.cs:169` |
| `NPCHandler.npcs` is private; only `LoadNPCs` adds to it | `Goose/NPCHandler.cs:16`, `:280` |
| `NPC.LoadFromTemplate` adds to map + login-ID lookup, not `npcs` | `Goose/NPC.cs:585–648` |
| `NPC.LoadFromTemplate` dereferences `Class.GetLevel(Level)` | `Goose/NPC.cs:635–636` |
| `ClassHandler.GetClass` returns `null` for unknown ids | `Goose/ClassHandler.cs:26–35` |
| `Class.GetLevel` returns `null` for unknown levels | `Goose/Class.cs:22–25` |
| `NPC.Allies` delegates to the template; checks are by reference | `Goose/NPC.cs:321`, `:559`, `:1000` |
| `Map.requiredItems` is private, used by `PlayerCanJoin` | `Goose/Map.cs:64`, `:573` |
| `Map.Muted` | `Goose/Map.cs:55` |
| `Map` constructor allocates players/requiredItems/npcs/items | `Goose/Map.cs:83–89` |
| `Player.BoundID` / `BoundMap`, and death warps to them | `Goose/Player.cs:226–238`, `:671–674`, `:1775` |
| `NPCTemplate.Quests` is `internal` | `Goose/NPCTemplate.cs:195` |
| `NPC.cs` aliases `this.Quests = template.Quests` | `Goose/NPC.cs:637` |
| `NPCHandler.templates` private dict | `Goose/NPCHandler.cs:15` |
| `NPCHandler` resolves `quest_ids` at load | `Goose/NPCHandler.cs:108` |
| `QuestHandler` is `internal` (no modifier) | `Goose/Quests/QuestHandler.cs:8` |
| `GameWorld.QuestHandler` is `internal` | `Goose/GameWorld.cs:47` |
| `Map.PlayerCanJoin` body | `Goose/Map.cs:548–584` |
| `IMapScript` interface members | `Goose/Scripting/IMapScript.cs:10–21` |
| `BaseMapScript` virtual no-ops | `Goose/Scripting/BaseMapScript.cs:57–105` |
| `CreateDatabaseSchema` runs only when `.db` is absent | `Goose/GameWorld.cs:166–186`, `212–221` |
| `players.sql` has no `DROP TABLE` (live data) | `Goose/sql/players.sql:1` |
| `JsonHelper.Serialize` / `.Deserialize` | `Goose/JsonHelper.cs:47–51` |
| `FakeDbDataReader` supports only `this[string]` | `Goose.Tests/Fakes/FakeDbDataReader.cs:19` |
| `QuestScriptFixture.Compile` returns `Script<IQuestScript>` | `Goose.Tests/Fixtures/QuestScriptFixture.cs:25–30` |
| Tests needing `GameWorld.Settings` use this collection | `Goose.Tests/Collections/GameWorldSettingsCollection.cs:3` |
| `MaxNPCs` is 15000 | `Goose/GooseSettings.json:131` |
| Source `PropertiesDictionary` | `~/code/3dMMO-Server/server/MMO.Server/Utilities/PropertiesDictionary.cs` |
| Source JSON converter | `~/code/3dMMO-Server/server/MMO.Server/Utilities/PropertiesDictionaryJsonConverter.cs` |

**Baseline:** `dotnet test Goose.sln` → 229 passed (Goose.Tests 105, Tools.Tests 124),
0 failed, 26 skipped. Verified by running it, not from memory.

**Working directory for every command:** `/home/hayden/code/illutiagooseserver/.worktrees/dimensions`

## Test budget

This plan adds **32** test cases, all in `Goose.Tests`. Tools.Tests is untouched at
124 passed / 26 skipped. Check the running total after each task:

| Task | Adds | Goose.Tests | Suite total passed |
|---|---|---|---|
| 0 — schema migration | 1 | 106 | 230 |
| 1 — PropertiesDictionary | 3 | 109 | 233 |
| 2 — Player.Properties persistence | 7 | 116 | 240 |
| 3 — widen to `long` | 4 | 120 | 244 |
| 4 — NPC template + NPC registration | 6 | 126 | 250 |
| 5 — `Map.CloneAs` | 3 | 129 | 253 |
| 6 — QuestHandler | 2 | 131 | 255 |
| 7 — `CanPlayerJoin` | 6 | 137 | 261 |
| 8 — MaxNPCs | 0 | 137 | 261 |

Counts include every `[Theory]` case individually, which is how the runner reports them.

---

## Task 0: Schema migration mechanism

**Why this is first:** `CreateDatabaseSchema()` (`GameWorld.cs:166`) runs *only* when the `.db` file does not exist (`GameWorld.cs:212–219`). Unlike the 11 sheet-imported schemas, `players.sql` has no `DROP TABLE` — it holds live player data and is created exactly once. `sql/onetimeupdates.sql` is dead MS SQL Server script (`USE IllutiaGoose`, `PRIMARY KEY CLUSTERED`) and is not executed by anything.

So there is **no way to add a column to an existing database**. Task 2 needs one. This task builds it.

**Files:**
- Modify: `Goose/GameWorld.cs` (add method near `CreateDatabaseSchema`, call it from `Start`)
- Test: `Goose.Tests/SchemaMigrationTests.cs` (create)

**Step 1: Write the failing test**

```csharp
using System.Data.SQLite;

namespace Goose.Tests;

public class SchemaMigrationTests
{
    [Fact]
    public void Adds_a_missing_column_and_is_idempotent()
    {
        using var conn = new SQLiteConnection("Data Source=:memory:");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE players (player_id INT PRIMARY KEY);";
            cmd.ExecuteNonQuery();
        }

        Assert.False(GameWorld.ColumnExists(conn, "players", "player_properties"));

        GameWorld.AddColumnIfMissing(conn, "players", "player_properties", "TEXT DEFAULT '' NOT NULL");
        Assert.True(GameWorld.ColumnExists(conn, "players", "player_properties"));

        // Running again must not throw - migrations run on every startup.
        GameWorld.AddColumnIfMissing(conn, "players", "player_properties", "TEXT DEFAULT '' NOT NULL");
        Assert.True(GameWorld.ColumnExists(conn, "players", "player_properties"));
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter SchemaMigrationTests`
Expected: FAIL — compile error, `GameWorld` does not contain `ColumnExists` / `AddColumnIfMissing`.

**Step 3: Write minimal implementation**

Add to `Goose/GameWorld.cs`, immediately after `ExecuteSql` (`GameWorld.cs:188`). Both helpers are `internal static`, which the test project can reach — `Goose.csproj:18` declares `InternalsVisibleTo("Goose.Tests")`.

```csharp
/// <summary>Runs on every startup, not just on a fresh database. `players` holds live
/// data and is never dropped, so new columns on it have to arrive this way.</summary>
private void MigrateDatabaseSchema()
{
    this.Database.Execute(conn =>
    {
        AddColumnIfMissing(conn, "players", "player_properties", "TEXT DEFAULT '' NOT NULL");
    });
}

internal static bool ColumnExists(SQLiteConnection connection, string table, string column)
{
    using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA table_info(" + table + ")";
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        if (string.Equals(Convert.ToString(reader["name"]), column, StringComparison.OrdinalIgnoreCase))
            return true;
    }
    return false;
}

internal static void AddColumnIfMissing(SQLiteConnection connection, string table, string column, string definition)
{
    if (ColumnExists(connection, table, column)) return;

    using var command = connection.CreateCommand();
    command.CommandText = "ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition;
    command.ExecuteNonQuery();
}
```

Then call it in `Start`, replacing the block at `GameWorld.cs:212–221`:

```csharp
bool createNew = !File.Exists(databasePath);
this.Database.Start(databasePath);
if (createNew)
{
    log.Info("DB file not found, creating...");
    CreateDatabaseSchema();
}
MigrateDatabaseSchema();   // <-- new: runs for fresh and existing databases alike
log.Info("Connected.");
```

Running migrations on fresh databases too is deliberate — it keeps one code path and makes each migration self-verifying.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter SchemaMigrationTests`
Expected: PASS (1 test).

**Step 5: Commit**

```bash
git add Goose/GameWorld.cs Goose.Tests/SchemaMigrationTests.cs
git commit -m "feat: add idempotent SQLite column migration on startup"
```

---

## Task 1: Port PropertiesDictionary

**Files:**
- Create: `Goose/PropertiesDictionary.cs`
- Create: `Goose/PropertiesDictionaryJsonConverter.cs`
- Test: `Goose.Tests/PropertiesDictionaryTests.cs`

**Note on the port:** the source converter consults a generated `PropertyValueTypeRegistry` for custom object types (`PropertiesDictionaryJsonConverter.cs:94,119`). Goose has no such registry and does not need one — **drop those lookups**. Primitives, nested dictionaries and lists are enough. Also drop the `[RegisterOrmLiteConverter]` attribute from `PropertiesDictionary` (`PropertiesDictionary.cs:10`); there is no OrmLite here.

**Step 1: Write the failing test**

```csharp
namespace Goose.Tests;

public class PropertiesDictionaryTests
{
    [Fact]
    public void Round_trips_through_JsonHelper_preserving_types()
    {
        var props = new PropertiesDictionary { ["dimension.max"] = 3, ["name"] = "abyss", ["on"] = true };

        var restored = JsonHelper.Deserialize<PropertiesDictionary>(JsonHelper.Serialize(props));

        // JSON integers come back as long; GetProperty<int> must narrow them.
        Assert.Equal(3, restored.GetProperty<int>("dimension.max"));
        Assert.Equal("abyss", restored.GetProperty<string>("name"));
        Assert.True(restored.GetProperty<bool>("on"));
    }

    [Fact]
    public void Missing_keys_use_the_default_or_throw()
    {
        var props = new PropertiesDictionary();

        Assert.Equal(0, props.GetProperty<int>("dimension.max", 0));
        Assert.False(props.TryGetProperty<int>("dimension.max", out _));
        Assert.Throws<KeyNotFoundException>(() => props.GetProperty<int>("dimension.max"));
    }

    [Fact]
    public void Clone_is_a_shallow_snapshot()
    {
        var props = new PropertiesDictionary { ["a"] = 1 };
        var copy = props.Clone();
        props["b"] = 2;

        Assert.Equal(1, copy.GetProperty<int>("a"));
        Assert.False(copy.ContainsKey("b"));
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter PropertiesDictionaryTests`
Expected: FAIL — compile error, type `PropertiesDictionary` not found.

**Step 3: Write minimal implementation**

Copy `PropertiesDictionary.cs` from the source path verbatim into `Goose/PropertiesDictionary.cs` with these edits: namespace `Goose`, remove the `using MMO.Shared.Attributes;` line and the `[RegisterOrmLiteConverter(...)]` attribute, keep `[JsonConverter(typeof(PropertiesDictionaryJsonConverter))]`.

Copy the converter into `Goose/PropertiesDictionaryJsonConverter.cs` with: namespace `Goose`, remove `using MMO.Server.Database;`, and in both `ReadArray` and `ReadObject` delete the `PropertyValueTypeRegistry.KeyToType` branch so they always fall through to the generic list / nested-dictionary path.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter PropertiesDictionaryTests`
Expected: PASS (3 tests).

**Step 5: Commit**

```bash
git add Goose/PropertiesDictionary.cs Goose/PropertiesDictionaryJsonConverter.cs Goose.Tests/PropertiesDictionaryTests.cs
git commit -m "feat: add PropertiesDictionary for typed script property storage"
```

---

## Task 2: Wire Player.Properties through persistence

**Files:**
- Modify: `Goose/sql/players.sql` (add column to CREATE TABLE, for fresh databases)
- Modify: `Goose/Player.cs` (property, `LoadFromReader`, INSERT, UPDATE)
- Test: `Goose.Tests/PlayerPropertiesTests.cs` (create)
- Test: `Goose.Tests/PlayerPropertiesPersistenceTests.cs` (create — real SQLite round-trip)

**Step 1: Write the failing test**

```csharp
using Goose.Tests.Fakes;

namespace Goose.Tests;

public class PlayerPropertiesTests
{
    [Fact]
    public void Defaults_to_an_empty_bag()
    {
        Assert.NotNull(new Player(0).Properties);
        Assert.Empty(new Player(0).Properties);
    }

    [Fact]
    public void Reads_the_player_properties_column()
    {
        var player = new Player(0);
        player.LoadPropertiesFromColumn("{\"dimension.max\":4}");

        Assert.Equal(4, player.Properties.GetProperty<int>("dimension.max"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Tolerates_an_empty_column(string value)
    {
        // Existing rows get '' from the ALTER TABLE default, so this is the common case.
        var player = new Player(0);
        player.LoadPropertiesFromColumn(value);

        Assert.NotNull(player.Properties);
        Assert.Empty(player.Properties);
    }
}
```

**A parse helper is not persistence.** The three tests above prove `LoadPropertiesFromColumn`
works; they prove nothing about the hand-rolled INSERT and UPDATE strings, which are where
a forgotten parameter or a missing comma actually breaks. Add a real round-trip against
a temporary SQLite file — one for a brand-new player (INSERT), one for an existing player
(UPDATE):

```csharp
using System.Data.SQLite;

namespace Goose.Tests;

/// <summary>Exercises the real INSERT/UPDATE strings in Player.cs. The parse-helper tests
/// above cannot catch an unbound @playerProperties parameter or a missing comma.</summary>
public class PlayerPropertiesPersistenceTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "player-props-" + Guid.NewGuid().ToString("N") + ".db");

    private SQLiteConnection OpenWithPlayersTable()
    {
        var conn = new SQLiteConnection("Data Source=" + dbPath + "; Version=3;");
        conn.Open();
        using var cmd = conn.CreateCommand();
        // The shipped schema, so a column added to players.sql without being added to the
        // INSERT column list fails here rather than in production.
        cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "players.sql"));
        cmd.ExecuteNonQuery();
        return conn;
    }

    [Fact]
    public void A_new_player_row_persists_and_reloads_its_properties()
    {
        using var conn = OpenWithPlayersTable();

        var player = MakeMinimalPlayer(playerId: 1);
        player.Properties["dimension.max"] = 3;
        RunInsert(player, conn);

        Assert.Equal(3, ReloadProperties(conn, 1).GetProperty<int>("dimension.max"));
    }

    [Fact]
    public void An_existing_player_row_persists_a_changed_property()
    {
        using var conn = OpenWithPlayersTable();

        var player = MakeMinimalPlayer(playerId: 1);
        player.Properties["dimension.max"] = 3;
        RunInsert(player, conn);

        player.Properties["dimension.max"] = 5;
        RunUpdate(player, conn);

        Assert.Equal(5, ReloadProperties(conn, 1).GetProperty<int>("dimension.max"));
    }

    private static PropertiesDictionary ReloadProperties(SQLiteConnection conn, int playerId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT player_properties FROM players WHERE player_id=" + playerId;
        var loaded = new Player(0);
        loaded.LoadPropertiesFromColumn(Convert.ToString(cmd.ExecuteScalar()));
        return loaded.Properties;
    }

    public void Dispose()
    {
        SQLiteConnection.ClearAllPools();
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }
}
```

`MakeMinimalPlayer`, `RunInsert` and `RunUpdate` are the part to work out during
implementation, and the shape depends on what `Player.cs` exposes:

- If the INSERT/UPDATE bodies can be reached without a live `GameWorld` — extract the query
  text and parameter binding into `internal` methods (e.g. `BuildInsertCommand(SQLiteConnection)`
  / `BuildUpdateCommand(SQLiteConnection)`) that both the save path and the test call. This
  is the preferred shape: it is a pure refactor of `Player.cs:842–982` and it makes the
  strings testable at all.
- `MakeMinimalPlayer` sets only what the NOT NULL columns need; every other field keeps its
  default. Objects the save path dereferences (`Inventory`, `Spellbook`, …) may need
  stubbing — if that turns into more than a few lines, that is a signal to do the extraction
  above rather than to drop the test.

Do not substitute a hand-written INSERT in the test for the real one. The point is to
execute the strings that ship.

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter PlayerPropertiesTests`
Expected: FAIL — compile error, `Player` has no `Properties` / `LoadPropertiesFromColumn`.

**Step 3: Write minimal implementation**

In `Goose/Player.cs`, add the property alongside the other public state:

```csharp
/// <summary>Arbitrary per-player storage for scripts. Persisted as JSON in players.player_properties.</summary>
public PropertiesDictionary Properties { get; set; } = new PropertiesDictionary();

/// <summary>Split out from LoadFromReader so it can be tested without a reader.</summary>
internal void LoadPropertiesFromColumn(string json)
{
    this.Properties = string.IsNullOrWhiteSpace(json)
        ? new PropertiesDictionary()
        : (JsonHelper.Deserialize<PropertiesDictionary>(json) ?? new PropertiesDictionary());
}
```

In `LoadFromReader`, after the `hair_a` line (`Player.cs:693`):

```csharp
this.LoadPropertiesFromColumn(Convert.ToString(reader["player_properties"]));
```

In the INSERT (`Player.cs:842`), add `player_properties` to the column list after `macrocheck_failures`, and `", @playerProperties"` to the VALUES list after `this.MacroCheckFailures`.

In the UPDATE (`Player.cs:918`), change the `macrocheck_failures` line to keep its trailing comma and add before the `WHERE`:

```csharp
"macrocheck_failures=" + this.MacroCheckFailures + ", " +
"player_properties=@playerProperties " +
"WHERE player_id=" + this.PlayerID;
```

Snapshot the value on the game thread alongside `playerName` and friends, then bind it in **both** `savePlayerRow` lambdas (`Player.cs:903–911` and `Player.cs:973–981`), matching the existing style:

```csharp
var playerProperties = JsonHelper.Serialize(this.Properties.Clone());
...
command.Parameters.Add(new SQLiteParameter("@playerProperties", DbType.String) { Value = playerProperties });
```

`Clone()` is why the source class has it — the save runs off the game thread, and the snapshot stops serialization racing a concurrent key add.

In `Goose/sql/players.sql`, add to the CREATE TABLE so fresh databases match:

```sql
player_properties TEXT DEFAULT '' NOT NULL,
```

Task 0's `MigrateDatabaseSchema` already adds it to existing databases.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter PlayerPropertiesTests`
Expected: PASS (7 tests — 4 facts + 3 theory cases, across both test classes).

Then run the whole suite: `dotnet test Goose.sln` → 240 passed, 0 failed.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/sql/players.sql Goose.Tests/PlayerPropertiesTests.cs Goose.Tests/PlayerPropertiesPersistenceTests.cs
git commit -m "feat: persist Player.Properties in players.player_properties"
```

---

## Task 3: Widen HP and weapon damage to long

**Files:**
- Modify: `Goose/AttributeSet.cs:14,15,180,181`
- Modify: `Goose/NPCTemplate.cs:169`
- Modify: `Goose/ICharacter.cs:88`, `Goose/NPC.cs:302`, `Goose/Player.cs:1572`, `Goose/Pet.cs:76`
- Modify: `Goose/Inventory.cs:821`, `Goose/NPCHandler.cs:54,81,82`, `Goose/Pet.cs:265`
- Modify: every call site the compiler flags
- Test: `Goose.Tests/DimensionScalingOverflowTests.cs` (create)

**Why:** the abyss scaling formulas overflow `int.MaxValue` (2.147e9) from dimension 3 for boss-tier mobs and dimension 5 for everything, wrapping negative. See the design doc's table.

**No migration needed:** SQLite `INT` columns have INTEGER affinity and already store 64-bit values. The client parses HP as `long` (`Goose2Client/Assets/Scripts/Network/Packets/StatusInfoPacket.cs:12,15`).

**Half the widening is already done:** `ICharacter.CurrentHP`, `MaxHP`, `CurrentMP` and
`MaxMP` are `long` today (`ICharacter.cs:48–60`). What is missing is the values that feed
them.

### The audit list

**Do not drive this task from compiler errors alone.** An explicit narrowing cast compiles
happily and truncates at runtime — `AttributeSet.operator*` is exactly this shape today.
Every row below must be visited and its outcome recorded, whether or not the build
complains:

| # | Site | Today | Required outcome |
|---|---|---|---|
| 1 | `AttributeSet.HP` / `.MP` | `int` | `long` |
| 2 | `AttributeSet.operator+` / `operator-` (HP, MP) | int arithmetic | long arithmetic — no cast |
| 3 | `AttributeSet.operator*` HP/MP (`:180,181`) | `(int)Math.Ceiling(...)` | `(long)Math.Ceiling(...)`. `long * decimal` is `decimal`, whose range dwarfs `long`, so only the cast changes |
| 4 | `AttributeSet.Clone` (`:75,76`) | copies HP/MP | still copies — verify it is not narrowing after the change |
| 5 | `NPCTemplate.WeaponDamage` | `int` | `long` |
| 6 | `NPC.WeaponDamage` (`:302`) | `int` | `long` |
| 7 | `ICharacter.WeaponDamage` (`:88`) | `int` | `long` |
| 8 | `Player.WeaponDamage` (`:1572`) + `Inventory.GetWeaponDamage` | `int` | `long`. Item damage stays `int` — items are not dimension-scaled — so this widens on return |
| 9 | `Pet.WeaponDamage` (`Pet.cs:76`) | `override int` | `long`, to satisfy #7 |
| 10 | `Pet` DB read (`Pet.cs:265`) | `Convert.ToInt32` | `Convert.ToInt64` |
| 11 | `Pet` INSERT/UPDATE (`Pet.cs:361`, `:432`) | string concat | unchanged — `long` formats identically |
| 12 | `NPCHandler` DB reads (`:54,81,82`) | `Convert.ToInt32` | `Convert.ToInt64` |
| 13 | `Player` DB reads of `player_hp` / `player_mp` | `Convert.ToInt32` | `Convert.ToInt64` |
| 14 | `NPC.Attack` damage (`NPC.cs:1370`) | `double` accumulator | unchanged — `long` promotes to `double` implicitly |
| 15 | `Player.Attack` damage (`Player.cs:1543–1549`) | `double` accumulator | unchanged, but check the `WeaponDamage == 1` sentinel still reads correctly |
| 16 | `SpellEffect` formula symbols (`:1274`, `:1284`) | `Dictionary<string, decimal>` | unchanged — `long` → `decimal` is implicit |
| 17 | Packets containing HP/MP/damage | string concat | unchanged — verify no `int.Parse` round-trip on the way out |
| 18 | `Window.cs:285` pet weapon damage line | string concat | unchanged |

Record the outcome of each row in the commit message or a scratch note. "The build is
clean" is not evidence that rows 3, 4, 14 and 17 were looked at.

**Do not** change anything to `int` to silence an error — that reintroduces the overflow.
`SP` stays `int`; it is untouched by dimension scaling.

**Step 1: Write the failing test**

```csharp
namespace Goose.Tests;

public class DimensionScalingOverflowTests
{
    /// <summary>abyss NPC.java:927 - (base + 100000*2^dim) * 4.7^dim.
    /// King Terror at dimension 3 is 3.21e9, past int.MaxValue.</summary>
    [Fact]
    public void HP_holds_values_past_int_max()
    {
        long scaled = (long)((30_143_269L + 100_000L * (long)Math.Pow(2, 3)) * Math.Pow(4.7, 3));
        Assert.True(scaled > int.MaxValue);

        var stats = new AttributeSet { HP = scaled, MP = scaled };

        Assert.Equal(scaled, stats.HP);
        Assert.Equal(scaled, stats.MP);
    }

    /// <summary>abyss NPC.java:936 - base*4^dim + 100000*max(0, 4^dim-3), x20 when base < 10m.
    /// A 200k-damage mob at dimension 5 is 6.1e9.</summary>
    [Fact]
    public void WeaponDamage_holds_values_past_int_max()
    {
        long scaled = (long)(200_000L * Math.Pow(4, 5) + 100_000L * Math.Max(0, Math.Pow(4, 5) - 3)) * 20L;
        Assert.True(scaled > int.MaxValue);

        Assert.Equal(scaled, new NPCTemplate { WeaponDamage = scaled }.WeaponDamage);
    }

    /// <summary>Guards AttributeSet.cs:180 - operator* casts HP and MP to int today, which
    /// compiles fine and truncates silently. This is the failure the audit list exists for.</summary>
    [Fact]
    public void Multiplying_an_AttributeSet_does_not_truncate_past_int_max()
    {
        var stats = new AttributeSet { HP = 3_000_000_000L, MP = 3_000_000_000L };

        var doubled = stats * 2.0;

        Assert.Equal(6_000_000_000L, doubled.HP);
        Assert.Equal(6_000_000_000L, doubled.MP);
    }

    [Fact]
    public void Copying_an_AttributeSet_does_not_truncate_past_int_max()
    {
        var stats = new AttributeSet { HP = 6_000_000_000L, MP = 6_000_000_000L };

        Assert.Equal(6_000_000_000L, stats.Clone().HP);
        Assert.Equal(6_000_000_000L, stats.Clone().MP);
        // operator+ against an empty set is the copy idiom the NPCTemplate copy ctor uses.
        Assert.Equal(6_000_000_000L, (stats + new AttributeSet()).HP);
        Assert.Equal(12_000_000_000L, (stats + stats).MP);
    }
}
```

One more test belongs with Task 4, once `SpawnNPC` exists, because it needs a real map and
a real class — it is listed here so the audit's coverage is visible in one place:

```csharp
/// <summary>The template-level tests above only prove the field holds the value. This one
/// runs a high-damage template through NPC.LoadFromTemplate and its damage path, which is
/// where an int on NPC.WeaponDamage or ICharacter.WeaponDamage would surface.</summary>
[Fact]
public void A_high_damage_template_survives_the_NPC_damage_path()
{
    // Build the world/map/class fixture, then:
    var template = new NPCTemplate { NPCTemplateID = 1, Name = "Overflow", Level = 50,
                                     ClassID = <a class with a level-50 row>,
                                     WeaponDamage = 6_000_000_000L };
    template.BaseStats.HP = 7_000_000_000L;

    var npc = world.NPCHandler.SpawnNPC(world, mapId, 5, 5, template, shouldRespawn: false);

    Assert.Equal(6_000_000_000L, npc.WeaponDamage);
    Assert.Equal(7_000_000_000L, npc.MaxHP);

    npc.Attack(target, world);

    // Damage is a double accumulator (NPC.cs:1370); the assertion is that a 6e9 weapon
    // one-shots a target rather than healing it, which is what a wrapped int would do.
    Assert.True(target.CurrentHP <= 0);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter DimensionScalingOverflowTests`
Expected: FAIL — `CS0266: cannot implicitly convert type 'long' to 'int'`.

**Step 3: Write minimal implementation**

Change the declarations to `long`:

```csharp
// Goose/AttributeSet.cs:14,15
public long HP { get; set; }
public long MP { get; set; }

// Goose/NPCTemplate.cs:169, Goose/NPC.cs:302
public long WeaponDamage { get; set; }

// Goose/ICharacter.cs:88
long WeaponDamage { get; }

// Goose/Player.cs:1572, Goose/Pet.cs:76
public virtual long WeaponDamage { ... }
public override long WeaponDamage { get; set; }
```

Then work the **audit list above, row by row**. Use the build as a first sweep, not as the
definition of done:

```bash
dotnet build Goose.sln 2>&1 | grep -E "error CS"
grep -rnE "\(int\)[^;]*(HP|MP|WeaponDamage)" --include=*.cs Goose
grep -rn "Convert.ToInt32" --include=*.cs Goose | grep -iE "hp|mp|weapon_damage"
```

The two greps are what catch rows 3, 10, 12 and 13 — none of which produce a compiler
error. Expected error shapes from the build, and the correct fix for each:

- `CS0266 long -> int` on an assignment: widen the local/field to `long`.
- Reading from a `DbDataReader`: change `Convert.ToInt32(reader["player_hp"])` to `Convert.ToInt64(...)`. Same for `player_mp` and `npc_hp`/`weapon_damage`.
- `string.Format` / packet concatenation: no change needed, `long` formats identically.
- Arithmetic mixing `int` and `long`: C# promotes automatically; only explicit casts need touching.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter DimensionScalingOverflowTests`
Expected: PASS (4 tests).

Then the full suite: `dotnet test Goose.sln` → 244 passed, 0 failed. **All 105 pre-existing `Goose.Tests` must still pass** — this task touches combat and packet code, so a regression here is the main risk in the plan.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: widen HP, MP and WeaponDamage to long for dimension scaling"
```

---

## Task 4: NPCHandler registration — templates and NPCs

**Files:**
- Modify: `Goose/NPCHandler.cs` (`AddTemplate` near `GetNPCTemplate` `:220`; `AddNPC` / `SpawnNPC` near `LoadNPCs` `:258`)
- Modify: `Goose/NPCTemplate.cs:195` (make `Quests` public, add copy constructor)
- Test: `Goose.Tests/NPCTemplateRegistrationTests.cs` (create)
- Test: `Goose.Tests/NPCSpawnRegistrationTests.cs` (create)

**Why `SpawnNPC` and not `new NPC().LoadFromTemplate(...)`:** `LoadFromTemplate` adds the
NPC to its `Map` and, through `Spawn` → `AssignNewId`, to the login-ID lookup. It does
**not** add it to `NPCHandler.npcs` — `LoadNPCs` does that inline at `NPCHandler.cs:280`,
and it is the only thing that ever does. An NPC created any other way is invisible to
`NPCHandler.NPCCount`. Part 2 creates ~70,000 of them, so without this the count the
design is verified against (~82k) would be wrong by an order of magnitude, and every
warden would be missing from the collection too.

**Step 1: Write the failing test**

```csharp
using Goose.Quests;

namespace Goose.Tests;

public class NPCTemplateRegistrationTests
{
    [Fact]
    public void Registered_templates_are_retrievable()
    {
        var handler = new NPCHandler();
        var template = new NPCTemplate { NPCTemplateID = 100162, Name = "King Terror (1)" };

        handler.AddTemplate(template);

        Assert.Same(template, handler.GetNPCTemplate(100162));
        Assert.Contains(template, handler.GetTemplates());
    }

    [Fact]
    public void Copy_constructor_copies_scalars_and_detaches_the_quest_list()
    {
        var quest = new Quest { Id = 1 };
        var original = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", WeaponDamage = 365, Level = 40 };
        original.Quests.Add(quest);

        var copy = new NPCTemplate(original) { NPCTemplateID = 100162 };

        Assert.Equal(100162, copy.NPCTemplateID);
        Assert.Equal("Shadow Dog", copy.Name);
        Assert.Equal(365, copy.WeaponDamage);
        Assert.Equal(40, copy.Level);

        // Detached: attaching a dimension quest must not touch the base template.
        copy.Quests.Add(new Quest { Id = 900001 });
        Assert.Single(original.Quests);
        Assert.Equal(2, copy.Quests.Count);
    }

    /// <summary>Allies are copied into a new list, but the entries still point at the
    /// templates the original allied with. Part 2 rewires them per dimension; this test
    /// pins the contract so that pass is written against something stated, not assumed.</summary>
    [Fact]
    public void Copy_constructor_detaches_the_ally_list_but_keeps_its_entries()
    {
        var ally = new NPCTemplate { NPCTemplateID = 5 };
        var original = new NPCTemplate { NPCTemplateID = 162, Allies = new List<NPCTemplate> { ally } };

        var copy = new NPCTemplate(original) { NPCTemplateID = 100162 };
        copy.Allies.Clear();

        Assert.Single(original.Allies);
        Assert.Same(ally, original.Allies[0]);
    }
}
```

And the spawn-registration tests. These need a `GameWorld` with a map and a class, so they
use the settings collection the same way `QuestScriptFixture`-based tests do:

```csharp
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class NPCSpawnRegistrationTests
{
    [Fact]
    public void A_spawned_npc_is_counted_by_the_handler()
    {
        // world with one map at id 1 and one class carrying a level-50 row
        var before = world.NPCHandler.NPCCount;

        var npc = world.NPCHandler.SpawnNPC(world, 1, 5, 5, template, shouldRespawn: true);

        Assert.NotNull(npc);
        Assert.Equal(before + 1, world.NPCHandler.NPCCount);
        Assert.Contains(npc, world.MapHandler.GetMap(1).NPCs);
        Assert.NotEqual(0, npc.LoginID);
    }

    [Fact]
    public void An_npc_on_a_map_that_does_not_exist_is_not_registered()
    {
        var before = world.NPCHandler.NPCCount;

        // LoadFromTemplate returns false when GetMap is null (NPC.cs:589).
        Assert.Null(world.NPCHandler.SpawnNPC(world, 999999, 5, 5, template, shouldRespawn: true));
        Assert.Equal(before, world.NPCHandler.NPCCount);
    }
}
```

Plus `A_high_damage_template_survives_the_NPC_damage_path` from Task 3, which lands here
because it needs `SpawnNPC`.

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter "NPCTemplateRegistrationTests|NPCSpawnRegistrationTests"`
Expected: FAIL — `NPCHandler` has no `AddTemplate` / `SpawnNPC`; `NPCTemplate` has no copy constructor; `Quests` is inaccessible (`CS0122`).

**Step 3: Write minimal implementation**

In `Goose/NPCTemplate.cs:195`, make the list public:

```csharp
public List<Quest> Quests { get; set; }
```

Add a copy constructor. Copy every scalar; give the copy its own `Quests` and `Drops` lists so a dimension variant can diverge, but share `Script` (compiled scripts are cached per path and stateless across templates):

```csharp
/// <summary>Copy constructor for script-generated variants. Lists are new instances so a
/// variant can attach its own quests/drops without mutating the template it came from.</summary>
public NPCTemplate(NPCTemplate other) : this()
{
    this.NPCType = other.NPCType;
    this.Behaviour = other.Behaviour;
    this.BehaviourTimeout = other.BehaviourTimeout;
    this.NPCTemplateID = other.NPCTemplateID;
    this.Name = other.Name;
    this.Title = other.Title;
    this.Surname = other.Surname;
    this.Facing = other.Facing;
    // AttributeSet has no Add method - operator+ (AttributeSet.cs:105) returns a new
    // instance with every field summed, so adding an empty set is the copy idiom.
    this.BaseStats = other.BaseStats + new AttributeSet();
    this.HairID = other.HairID;   this.HairR = other.HairR;
    this.HairG = other.HairG;     this.HairB = other.HairB;   this.HairA = other.HairA;
    this.BodyR = other.BodyR;     this.BodyG = other.BodyG;
    this.BodyB = other.BodyB;     this.BodyA = other.BodyA;
    this.FaceID = other.FaceID;   this.BodyID = other.BodyID; this.BodyState = other.BodyState;
    this.Experience = other.Experience;
    this.Level = other.Level;
    this.ClassID = other.ClassID;
    this.RespawnTime = other.RespawnTime;
    this.AggroRange = other.AggroRange;
    this.AttackRange = other.AttackRange;
    this.CanBeRooted = other.CanBeRooted;
    this.CanBeStunned = other.CanBeStunned;
    this.CanBeSlowed = other.CanBeSlowed;
    this.CanBeKilled = other.CanBeKilled;
    this.AttackSpeed = other.AttackSpeed;
    this.MoveSpeed = other.MoveSpeed;
    this.CanMove = other.CanMove;
    this.EquippedItems = other.EquippedItems;
    this.WeaponDamage = other.WeaponDamage;
    this.AlliesString = other.AlliesString;
    this.CreditDealer = other.CreditDealer;
    this.Script = other.Script;
    this.ScriptParams = other.ScriptParams;
    this.ArmorPierce = other.ArmorPierce;
    this.VendorItems = other.VendorItems;
    this.Allies = other.Allies is null ? null : new List<NPCTemplate>(other.Allies);
    this.Drops = other.Drops is null ? null : new List<NPCDropInfo>(other.Drops);
    this.Quests = other.Quests is null ? new List<Quest>() : new List<Quest>(other.Quests);
}
```

Verify against `Goose/NPCTemplate.cs:23–201` that no property is missed. Note the parameterless constructor (`NPCTemplate.cs:203`) initialises only `Quests`, leaving `Drops`, `Allies` and `VendorItems` null — hence the null guards above.

In `Goose/NPCHandler.cs`, next to `GetNPCTemplate` (`NPCHandler.cs:220`):

```csharp
/// <summary>Registers a script-generated template. Overwrites any existing entry with the
/// same id - callers that must not collide should check GetNPCTemplate first.</summary>
public void AddTemplate(NPCTemplate template)
{
    this.templates[template.NPCTemplateID] = template;
}
```

And the NPC registration API, next to `LoadNPCs` (`NPCHandler.cs:258`):

```csharp
/// <summary>Registers an already-loaded NPC so NPCCount and anything enumerating the
/// handler's npcs can see it. LoadFromTemplate does not do this - it only adds the NPC to
/// its map and to the login-id lookup.</summary>
public void AddNPC(NPC npc)
{
    this.npcs.Add(npc);
}

/// <summary>The supported way to create an NPC at runtime: loads it from the template and
/// registers it. Returns null if the map does not exist, in which case nothing is
/// registered. Every caller - LoadNPCs included - should go through this rather than
/// calling LoadFromTemplate directly, so there is one definition of "spawned".</summary>
public NPC SpawnNPC(GameWorld world, int mapId, int mapX, int mapY, NPCTemplate template, bool shouldRespawn)
{
    var npc = new NPC();
    if (!npc.LoadFromTemplate(world, mapId, mapX, mapY, template, shouldRespawn)) return null;

    this.AddNPC(npc);
    return npc;
}
```

Then rewrite the body of `LoadNPCs` (`NPCHandler.cs:266–291`) to call `SpawnNPC` instead of
constructing and registering inline. This is the point of the task — two ways to spawn an
NPC is how the inconsistency arose in the first place:

```csharp
NPCTemplate template = this.GetNPCTemplate(npc_id);
if (template == null) continue;               // log bad id
if (this.SpawnNPC(world, map_id, map_x, map_y, template, shouldRespawn: true) == null)
{
    // couldn't load map
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter "NPCTemplateRegistrationTests|NPCSpawnRegistrationTests|DimensionScalingOverflowTests"`
Expected: PASS (5 new tests — 3 template, 2 spawn — plus the damage-path test moved from Task 3).

**Step 5: Commit**

```bash
git add Goose/NPCHandler.cs Goose/NPCTemplate.cs Goose.Tests/NPCTemplateRegistrationTests.cs Goose.Tests/NPCSpawnRegistrationTests.cs
git commit -m "feat: allow scripts to register and copy NPC templates and spawn NPCs"
```

---

## Task 5: Map.CloneAs

**Files:**
- Modify: `Goose/Map.cs` (add `CloneAs` near the constructor, `Map.cs:83`)
- Test: `Goose.Tests/MapCloneTests.cs` (create)

**Why this belongs in the server:** Part 2 needs 960 copies of maps. Reconstructing one in
a script from public fields drops `requiredItems` — it is a private field (`Map.cs:64`)
that `PlayerCanJoin` enforces (`Map.cs:573`), so every dimension clone of a
key-gated map would silently become free to enter. `Muted` is a second easy miss. A clone
API keeps "what makes a map a map" in one place, next to the fields.

**Step 1: Write the failing test**

```csharp
namespace Goose.Tests;

public class MapCloneTests
{
    private static Map MakeBase()
    {
        var map = new Map
        {
            ID = 1, Name = "Town", FileName = "Map1.map", Width = 10, Height = 10,
            MinLevel = 5, MaxLevel = 20, MinExperience = 100, MaxExperience = 200,
            CanPVP = false, CanChat = true, CanBind = true, Muted = true,
            ScriptParams = "base-params",
            tiles = new ITile[11 * 11],
            characters = new ICharacter[11 * 11],
        };
        map.SetTile(3, 3, new BlockedTile());
        return map;
    }

    [Fact]
    public void Copies_every_map_setting_including_Muted()
    {
        var clone = MakeBase().CloneAs(100001, "Town (1)");

        Assert.Equal(100001, clone.ID);
        Assert.Equal("Town (1)", clone.Name);
        Assert.Equal("Map1.map", clone.FileName);
        Assert.Equal(10, clone.Width);
        Assert.Equal(10, clone.Height);
        Assert.Equal(5, clone.MinLevel);
        Assert.Equal(20, clone.MaxLevel);
        Assert.Equal(100, clone.MinExperience);
        Assert.Equal(200, clone.MaxExperience);
        Assert.True(clone.CanChat);
        Assert.True(clone.CanBind);
        Assert.True(clone.Muted);
        Assert.Equal("base-params", clone.ScriptParams);
    }

    /// <summary>The reason this API exists: requiredItems is private, so a clone
    /// assembled from public fields would bypass item-gated entry entirely.</summary>
    [Fact]
    public void Copies_required_items_without_sharing_the_list()
    {
        var basic = MakeBase();
        basic.AddRequiredItem(1234);

        var clone = basic.CloneAs(100001, "Town (1)");
        clone.AddRequiredItem(5678);

        Assert.Equal(new[] { 1234 }, basic.RequiredItems);
        Assert.Equal(new[] { 1234, 5678 }, clone.RequiredItems);
    }

    [Fact]
    public void Gives_the_clone_its_own_occupancy_state_but_shares_tiles()
    {
        var basic = MakeBase();
        var clone = basic.CloneAs(100001, "Town (1)");

        Assert.NotSame(basic.characters, clone.characters);
        Assert.Equal(basic.characters.Length, clone.characters.Length);
        Assert.NotSame(basic.Players, clone.Players);
        Assert.NotSame(basic.NPCs, clone.NPCs);
        Assert.NotSame(basic.Items, clone.Items);
        Assert.Empty(clone.Players);
        Assert.Empty(clone.NPCs);

        // tiles is a new array holding the same tile objects - BlockedTile is a stateless
        // marker (BlockedTile.cs:8) and WarpTiles get replaced by the caller.
        Assert.NotSame(basic.tiles, clone.tiles);
        Assert.Same(basic.GetTile(3, 3), clone.GetTile(3, 3));
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter MapCloneTests`
Expected: FAIL — `Map` has no `CloneAs`, `AddRequiredItem` or `RequiredItems`.

**Step 3: Write minimal implementation**

Two small accessors first, so `requiredItems` can be populated and asserted on without
being made public:

```csharp
/// <summary>Item template ids a player must carry to enter. Read-only - use
/// AddRequiredItem to populate.</summary>
public IReadOnlyList<int> RequiredItems { get { return this.requiredItems; } }

public void AddRequiredItem(int itemTemplateId)
{
    this.requiredItems.Add(itemTemplateId);
}
```

`Map.LoadData` (`Map.cs:511`) then uses `AddRequiredItem` rather than touching the field.

```csharp
/// <summary>Copies this map as a new map at another id. Every setting comes across,
/// including the private requiredItems list, which is why this lives here and not in a
/// script. The clone gets its own occupancy state - characters, players, npcs, items -
/// which is what keeps two copies of the same map independent.
///
/// tiles is a shallow copy: the tile objects are shared. BlockedTile is a stateless
/// marker; WarpTile is expected to be replaced by the caller if it should point somewhere
/// else; ItemTile only ever appears at runtime, after loading.
///
/// Deliberately not LoadData: that re-parses the .map file and issues two SQL queries
/// keyed on the new id (Map.cs:466-520), which match no rows for a clone.</summary>
public Map CloneAs(int id, string name)
{
    var clone = new Map
    {
        ID = id,
        Name = name,
        FileName = this.FileName,
        Width = this.Width,
        Height = this.Height,
        MinLevel = this.MinLevel,
        MaxLevel = this.MaxLevel,
        MinExperience = this.MinExperience,
        MaxExperience = this.MaxExperience,
        CanPVP = this.CanPVP,
        CanChat = this.CanChat,
        CanAuction = this.CanAuction,
        CanShout = this.CanShout,
        CanCast = this.CanCast,
        CanBind = this.CanBind,
        CanUseItems = this.CanUseItems,
        CanSpawnPets = this.CanSpawnPets,
        Muted = this.Muted,
        Script = this.Script,
        ScriptParams = this.ScriptParams,
        tiles = (ITile[])this.tiles.Clone(),
        characters = new ICharacter[this.characters.Length],
    };

    clone.requiredItems.AddRange(this.requiredItems);
    return clone;
}
```

`ScriptStore` is deliberately **not** copied — it is per-map runtime state owned by
whatever script is attached.

Verify against `Map.cs:24–64` that no field is missed; adding a field to `Map` later and
forgetting it here is the failure mode this task is guarding against, so leave a comment on
the field block pointing at `CloneAs`.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter MapCloneTests`
Expected: PASS (3 tests).

**Step 5: Commit**

```bash
git add Goose/Map.cs Goose.Tests/MapCloneTests.cs
git commit -m "feat: add Map.CloneAs for script-generated map variants"
```

---

## Task 6: Make QuestHandler public and add AddQuest

**Files:**
- Modify: `Goose/Quests/QuestHandler.cs:8`
- Modify: `Goose/GameWorld.cs:47`
- Test: `Goose.Tests/QuestHandlerRegistrationTests.cs` (create)

**Context:** `NPCHandler.cs:108` resolves `npc_templates.quest_ids` against `QuestHandler` at template-load time, which runs **before** global scripts. Sheet-authored `quest_ids` therefore cannot reference script-created quests — Part 2's script attaches them to the warden template itself, which is why Task 4 made `NPCTemplate.Quests` public.

**Step 1: Write the failing test**

```csharp
using Goose.Quests;

namespace Goose.Tests;

public class QuestHandlerRegistrationTests
{
    [Fact]
    public void Registered_quests_are_retrievable_by_id()
    {
        var handler = new QuestHandler();
        var quest = new Quest { Id = 900001, Name = "Abysmal Terror (1)" };

        handler.AddQuest(quest);

        Assert.Same(quest, handler.Get(900001));
        Assert.Same(quest, handler.Quests[900001]);
    }

    [Fact]
    public void Unknown_ids_return_null()
    {
        Assert.Null(new QuestHandler().Get(900001));
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter QuestHandlerRegistrationTests`
Expected: FAIL — `CS0122 QuestHandler is inaccessible due to its protection level`.

**Step 3: Write minimal implementation**

```csharp
// Goose/Quests/QuestHandler.cs:8
public class QuestHandler

// Goose/GameWorld.cs:47
public QuestHandler QuestHandler { get; set; }
```

Add next to `Get` (`QuestHandler.cs:76`):

```csharp
/// <summary>Registers a script-generated quest. Overwrites any existing entry with the same id.</summary>
public void AddQuest(Quest quest)
{
    this.Quests[quest.Id] = quest;
}
```

Making `GameWorld.QuestHandler` public may surface accessibility errors elsewhere (a public member cannot expose a less-accessible type). Build and fix whatever the compiler reports — the design already anticipates `Quest`, `QuestRequirement`, `QuestReward` and the enums being public from the quest-scripts change.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter QuestHandlerRegistrationTests`
Expected: PASS (2 tests).

**Step 5: Commit**

```bash
git add Goose/Quests/QuestHandler.cs Goose/GameWorld.cs Goose.Tests/QuestHandlerRegistrationTests.cs
git commit -m "feat: expose QuestHandler to scripts with AddQuest"
```

---

## Task 7: IMapScript.CanPlayerJoin entry-gate hook

**Files:**
- Modify: `Goose/Scripting/IMapScript.cs` (add member)
- Modify: `Goose/Scripting/BaseMapScript.cs` (virtual default)
- Modify: `Goose/Map.cs:548` (consult it first)
- Test: `Goose.Tests/MapCanPlayerJoinTests.cs` (create)

**Why one hook covers everything:** `Map.PlayerCanJoin` is the sole gate for both warps (`Events/MoveEvent.cs:123`) and teleport spells (`SpellEffect.cs:727`).

**This gate fails closed.** A script that throws refuses entry. Everywhere else in `Map`
and `NPC` a script exception is swallowed and execution continues, because those hooks are
cosmetic. This one decides who may enter a map: a `NullReferenceException` in a gate script
must not silently unlock every dimension for every player. Refuse with a generic message,
log the exception, and let the shouting happen in the log rather than in the game.

**Step 1: Write the failing test**

```csharp
using Goose.Scripting;

namespace Goose.Tests;

public class MapCanPlayerJoinTests
{
    private sealed class RefusingMapScript : BaseMapScript
    {
        public override string CanPlayerJoin(Map map, Player player, GameWorld world) => "denied";
    }

    [Fact]
    public void Base_script_allows_by_default()
    {
        Assert.Null(new BaseMapScript().CanPlayerJoin(null, null, null));
    }

    [Fact]
    public void A_refusing_script_blocks_entry()
    {
        Assert.Equal("denied", new RefusingMapScript().CanPlayerJoin(null, null, null));
    }
}
```

**Those two tests are not enough on their own.** They only prove `BaseMapScript` has a
method with the right default; they say nothing about whether `Map.PlayerCanJoin` ever
calls it, sends the refusal, honours the GM bypass, or what happens when the script throws
— which is the entire contract Part 2's gating depends on. Add four end-to-end tests
driving the real `Map.PlayerCanJoin`. It needs a `GameWorld` for `world.Send`, so follow
the `QuestScriptFixture` pattern (`Goose.Tests/Fixtures/QuestScriptFixture.cs`) and put
them under `[Collection(GameWorldSettingsCollection.Name)]`:

```csharp
[Collection(GameWorldSettingsCollection.Name)]
public class MapPlayerCanJoinHookTests
{
    private sealed class RefusingMapScript : BaseMapScript
    {
        public override string CanPlayerJoin(Map map, Player player, GameWorld world) => "denied";
    }

    private sealed class ThrowingMapScript : BaseMapScript
    {
        public override string CanPlayerJoin(Map map, Player player, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void A_map_with_no_script_still_allows_entry()
    {
        // Script is null on most maps; the ?. must not turn into a refusal.
        Assert.True(MapWith(script: null).PlayerCanJoin(OrdinaryPlayer(), world));
    }

    [Fact]
    public void A_refusing_script_stops_entry_and_the_player_is_told_why()
    {
        var player = OrdinaryPlayer();

        Assert.False(MapWith(new RefusingMapScript()).PlayerCanJoin(player, world));
        Assert.Contains("denied", SentTo(player));
    }

    [Fact]
    public void A_GM_bypasses_the_script_gate()
    {
        // The privilege check is before the hook (Map.cs:550), so the script never runs.
        var gm = PlayerWith(AccessPrivilege.IgnoreMapRequirements);

        Assert.True(MapWith(new RefusingMapScript()).PlayerCanJoin(gm, world));
    }

    /// <summary>Fail closed. A gate script that throws must not admit the player.</summary>
    [Fact]
    public void A_throwing_script_refuses_entry()
    {
        var player = OrdinaryPlayer();

        Assert.False(MapWith(new ThrowingMapScript()).PlayerCanJoin(player, world));
        Assert.NotEmpty(SentTo(player));
    }
}
```

`MapWith` wraps a `BaseMapScript` instance in whatever shape `Map.Script` expects. `Script<T>`
(`Goose/Scripting/Script.cs`) compiles from a file path, so either add an internal
constructor/setter that takes a pre-built object for testing, or compile a one-line `.csx`
through the fixture. Prefer the fixture — it exercises the real path and needs no
production change; the trade is a slower test.

`SentTo(player)` needs whatever the test project already uses to observe `world.Send`; if
there is nothing, a `Player` subclass capturing sent packets is the smallest thing that
works.

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter MapCanPlayerJoinTests`
Expected: FAIL — `BaseMapScript` has no `CanPlayerJoin`.

**Step 3: Write minimal implementation**

Add to `Goose/Scripting/IMapScript.cs`, after `OnPlayerEntered` (`IMapScript.cs:14`):

```csharp
/// <summary>Return a refusal message to block entry, or null to allow.
/// Consulted by Map.PlayerCanJoin, which gates warps and teleport spells alike.</summary>
string CanPlayerJoin(Map map, Player player, GameWorld world);
```

Add the no-op default to `Goose/Scripting/BaseMapScript.cs`:

```csharp
public virtual string CanPlayerJoin(Map map, Player player, GameWorld world)
{
    return null;
}
```

In `Goose/Map.cs:548`, insert immediately after the privilege check so GMs keep bypassing gates:

```csharp
public bool PlayerCanJoin(Player player, GameWorld world)
{
    if (player.HasPrivilege(AccessPrivilege.IgnoreMapRequirements)) return true;

    string refusal = null;
    try
    {
        refusal = this.Script?.Object.CanPlayerJoin(this, player, world);
    }
    catch (Exception e)
    {
        // Fail CLOSED. This is an access-control gate, not a cosmetic hook: a broken gate
        // script must refuse rather than admit. The player gets a generic message; the
        // detail goes to the log, where someone can act on it.
        log.Error(e, "Map CanPlayerJoin {0} Exception", this.Name);
        refusal = "You cannot enter this map right now.";
    }
    if (refusal != null)
    {
        world.Send(player, "$7" + refusal);
        return false;
    }

    if (this.MinLevel != 0 && player.Level < this.MinLevel)
    // ... rest unchanged
```

`Map` has **no** logger field today — add one at the top of the class (`Map.cs:14`), matching `NPC.cs:19` exactly:

```csharp
private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
```

This deliberately differs from `Map.LoadData` (`Map.cs:470`) and `NPC.OnMoveEvent`
(`NPC.cs:367`), which swallow and continue. Those hooks decorate; this one decides.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter "MapCanPlayerJoinTests|MapPlayerCanJoinHookTests"`
Expected: PASS (6 tests).

Existing map scripts (`ArenaMap.csx`, `ZombieTownMap.csx`) extend `BaseMapScript`, so they inherit the no-op and keep compiling. Verify with the full suite.

**Step 5: Commit**

```bash
git add Goose/Scripting/IMapScript.cs Goose/Scripting/BaseMapScript.cs Goose/Map.cs Goose.Tests/MapCanPlayerJoinTests.cs Goose.Tests/MapPlayerCanJoinHookTests.cs
git commit -m "feat: add IMapScript.CanPlayerJoin entry gate"
```

---

## Task 8: Raise MaxNPCs and verify the whole suite

**Files:**
- Modify: `Goose/GooseSettings.json:131`

**Why:** dimensions produce ~82,000 NPCs against a current cap of 15,000. `NPCHandler.GetNewID` (`NPCHandler.cs:232`) picks login IDs by random probe in `(MaxPlayers, MaxNPCs)`, so the cap must exceed the population with headroom — 250000 against 82k keeps the probe at ~1.5 tries. Login IDs are decimal text in packets (`Packets.cs:153`), so there is no wire-format ceiling.

**Step 1: Make the change**

```json
"MaxNPCs": 250000,
```

**Step 2: Verify the full suite**

Run: `dotnet test Goose.sln`
Expected: `Failed: 0`, Goose.Tests at 105 + the 32 added by this plan = 137 passed; Tools.Tests unchanged at 124 passed / 26 skipped; suite total 261 passed, 26 skipped. If the number differs, reconcile against the test-budget table at the top before moving on — a silent shortfall means a task's tests were dropped.

**Step 3: Verify the server still starts**

This is the only end-to-end check in Part 1 — it confirms the migration runs against a real database and that widening HP did not break startup.

```bash
dotnet run --project Goose/Goose.csproj
```

Expected: reaches "Connected." and completes its load steps without exceptions. Confirm the migration applied:

```bash
sqlite3 Goose/bin/Debug/IllutiaGoose.db "PRAGMA table_info(players);" | grep player_properties
```

Expected: one row naming `player_properties`. Run the server a second time to confirm the migration is idempotent and does not error on the now-present column.

**Step 4: Commit**

```bash
git add Goose/GooseSettings.json
git commit -m "feat: raise MaxNPCs to 250000 for dimension NPC population"
```

---

## Done when

- `dotnet test Goose.sln` reports 0 failures and **261 passed** (Goose.Tests 137).
- The server starts twice in a row against the same database file.
- `players.player_properties` exists on both a fresh and a pre-existing database, and a
  property written to a player survives a save and reload.
- Every row of the Task 3 audit list has a recorded outcome.
- `NPCHandler.LoadNPCs` goes through `SpawnNPC`; nothing in the tree calls
  `LoadFromTemplate` directly any more.
- No file under `Goose/Data/` has been touched — the scripts are Part 2.
