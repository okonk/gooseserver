# Dimensions Part 1 — Server Extension Points Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add the seven generic server extension points the dimensions feature needs, plus the schema-migration mechanism the project currently lacks, with no behaviour change until Part 2's scripts arrive.

**Architecture:** Every change is generic — nothing in the server mentions dimensions. Scripts get a per-player property bag, the ability to register NPC templates and quests, a map entry-gate hook, and 64-bit HP/damage so the scaling formulas don't overflow.

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
| `NPCTemplate.WeaponDamage` is `int` | `Goose/NPCTemplate.cs:169` |
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

**Baseline:** `dotnet test Goose.sln` → 229 passed, 0 failed, 26 skipped.

**Working directory for every command:** `/home/hayden/code/illutiagooseserver/.worktrees/dimensions`

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
Expected: PASS (5 tests — 2 facts + 3 theory cases).

Then run the whole suite: `dotnet test Goose.sln` → 237 passed, 0 failed.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/sql/players.sql Goose.Tests/PlayerPropertiesTests.cs
git commit -m "feat: persist Player.Properties in players.player_properties"
```

---

## Task 3: Widen HP and weapon damage to long

**Files:**
- Modify: `Goose/AttributeSet.cs:14,15`
- Modify: `Goose/NPCTemplate.cs:169`
- Modify: every call site the compiler flags (34 `.HP` usages, 43 `WeaponDamage` usages)
- Test: `Goose.Tests/DimensionScalingOverflowTests.cs` (create)

**Why:** the abyss scaling formulas overflow `int.MaxValue` (2.147e9) from dimension 3 for boss-tier mobs and dimension 5 for everything, wrapping negative. See the design doc's table.

**No migration needed:** SQLite `INT` columns have INTEGER affinity and already store 64-bit values. The client parses HP as `long` (`Goose2Client/Assets/Scripts/Network/Packets/StatusInfoPacket.cs:12,15`).

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
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter DimensionScalingOverflowTests`
Expected: FAIL — `CS0266: cannot implicitly convert type 'long' to 'int'`.

**Step 3: Write minimal implementation**

Change the three declarations to `long`:

```csharp
// Goose/AttributeSet.cs:14,15
public long HP { get; set; }
public long MP { get; set; }

// Goose/NPCTemplate.cs:169
public long WeaponDamage { get; set; }
```

Leave `SP` as `int` — it is untouched by dimension scaling.

Then build and fix each error the compiler reports:

```bash
dotnet build Goose.sln 2>&1 | grep -E "error CS"
```

Expected error shapes and the correct fix for each:

- `CS0266 long -> int` on an assignment: widen the local/field to `long`.
- Reading from a `DbDataReader`: change `Convert.ToInt32(reader["player_hp"])` to `Convert.ToInt64(...)`. Same for `player_mp` and `npc_hp`/`weapon_damage`.
- `string.Format` / packet concatenation: no change needed, `long` formats identically.
- Arithmetic mixing `int` and `long`: C# promotes automatically; only explicit casts need touching.

**Do not** change anything to `int` to silence an error — that reintroduces the overflow.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter DimensionScalingOverflowTests`
Expected: PASS (2 tests).

Then the full suite: `dotnet test Goose.sln` → 239 passed, 0 failed. **All 105 pre-existing `Goose.Tests` must still pass** — this task touches combat and packet code, so a regression here is the main risk in the plan.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: widen HP, MP and WeaponDamage to long for dimension scaling"
```

---

## Task 4: NPCHandler.AddTemplate and NPCTemplate copying

**Files:**
- Modify: `Goose/NPCHandler.cs` (add method near `GetNPCTemplate`, `NPCHandler.cs:220`)
- Modify: `Goose/NPCTemplate.cs:195` (make `Quests` public, add copy constructor)
- Test: `Goose.Tests/NPCTemplateRegistrationTests.cs` (create)

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
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter NPCTemplateRegistrationTests`
Expected: FAIL — `NPCHandler` has no `AddTemplate`; `NPCTemplate` has no copy constructor; `Quests` is inaccessible (`CS0122`).

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
/// <summary>Registers a script-generated template. Overwrites any existing entry with the same id.</summary>
public void AddTemplate(NPCTemplate template)
{
    this.templates[template.NPCTemplateID] = template;
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter NPCTemplateRegistrationTests`
Expected: PASS (2 tests).

**Step 5: Commit**

```bash
git add Goose/NPCHandler.cs Goose/NPCTemplate.cs Goose.Tests/NPCTemplateRegistrationTests.cs
git commit -m "feat: allow scripts to register and copy NPC templates"
```

---

## Task 5: Make QuestHandler public and add AddQuest

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

## Task 6: IMapScript.CanPlayerJoin entry-gate hook

**Files:**
- Modify: `Goose/Scripting/IMapScript.cs` (add member)
- Modify: `Goose/Scripting/BaseMapScript.cs` (virtual default)
- Modify: `Goose/Map.cs:548` (consult it first)
- Test: `Goose.Tests/MapCanPlayerJoinTests.cs` (create)

**Why one hook covers everything:** `Map.PlayerCanJoin` is the sole gate for both warps (`Events/MoveEvent.cs:123`) and teleport spells (`SpellEffect.cs:727`).

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

A test driving `Map.PlayerCanJoin` end to end needs a `GameWorld` for `world.Send`; the `QuestScriptFixture` pattern (`Goose.Tests/Fixtures/QuestScriptFixture.cs`) shows how to stand one up if you want that coverage — add it under `[Collection(GameWorldSettingsCollection.Name)]`.

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
        log.Error(e, "Map CanPlayerJoin {0} Exception", this.Name);
    }
    if (refusal != null)
    {
        world.Send(player, "$7" + refusal);
        return false;
    }

    if (this.MinLevel != 0 && player.Level < this.MinLevel)
    // ... rest unchanged
```

The try/catch matches how `Map.LoadData` (`Map.cs:470`) and `NPC.OnMoveEvent` (`NPC.cs:367`) already guard script calls. `Map` has **no** logger field today — add one at the top of the class (`Map.cs:14`), matching `NPC.cs:19` exactly:

```csharp
private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
```

A silent `catch {}` like `Map.LoadData`'s would hide a broken gate script and let players through a map that meant to refuse them, so this one logs.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter MapCanPlayerJoinTests`
Expected: PASS (2 tests).

Existing map scripts (`ArenaMap.csx`, `ZombieTownMap.csx`) extend `BaseMapScript`, so they inherit the no-op and keep compiling. Verify with the full suite.

**Step 5: Commit**

```bash
git add Goose/Scripting/IMapScript.cs Goose/Scripting/BaseMapScript.cs Goose/Map.cs Goose.Tests/MapCanPlayerJoinTests.cs
git commit -m "feat: add IMapScript.CanPlayerJoin entry gate"
```

---

## Task 7: Raise MaxNPCs and verify the whole suite

**Files:**
- Modify: `Goose/GooseSettings.json:131`

**Why:** dimensions produce ~82,000 NPCs against a current cap of 15,000. `NPCHandler.GetNewID` (`NPCHandler.cs:232`) picks login IDs by random probe in `(MaxPlayers, MaxNPCs)`, so the cap must exceed the population with headroom — 250000 against 82k keeps the probe at ~1.5 tries. Login IDs are decimal text in packets (`Packets.cs:153`), so there is no wire-format ceiling.

**Step 1: Make the change**

```json
"MaxNPCs": 250000,
```

**Step 2: Verify the full suite**

Run: `dotnet test Goose.sln`
Expected: `Failed: 0`, Goose.Tests at 105 + the 14 added by this plan = 119 passed; Tools.Tests unchanged at 124 passed / 26 skipped.

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

- `dotnet test Goose.sln` reports 0 failures.
- The server starts twice in a row against the same database file.
- `players.player_properties` exists on both a fresh and a pre-existing database.
- No file under `Goose/Data/` has been touched — the scripts are Part 2.
