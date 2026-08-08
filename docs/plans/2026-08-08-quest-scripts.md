# Quest Scripts Implementation Plan

**Goal:** Let a quest requirement or reward delegate its logic to a C# `.csx` script, via new `Script` values in `RequirementType` and `RewardType`.

**Architecture:** Follow the pattern the other five scripted systems already use. One new `IQuestScript` interface plus `BaseQuestScript` no-op base in `Goose/Scripting/`. `quest_requirements` and `quest_rewards` each gain `script_path` / `script_params` columns; their `FromReader` methods resolve the script through the existing `ScriptHandler` cache and throw at load time if a `Script`-typed row has no script. `QuestWindow` gets five new `case` arms plus one new window state for a script-supplied "cannot complete" message.

**Tech Stack:** C# / .NET 10, `Microsoft.CodeAnalysis.CSharp.Scripting` 5.6.0, SQLite, xunit 2.9.3. Editor schema flows `CsvToSql.Core` descriptors → `SchemaRegistry` → `SchemaGen` → `tools/DataEditor/schema.js`.

**Design doc:** `docs/plans/2026-08-08-quest-scripts-design.md`

---

## APIs verified

Every citation below was opened and read in this worktree. Nothing here is remembered.

| API | Location | Notes |
|---|---|---|
| `Script<T>.Script(string filePath)`, `.Object`, `.LoadScript()` | `Goose/Scripting/Script.cs:19,17,25` | ctor compiles eagerly; `LoadScript` throws `FileNotFoundException` on a missing file |
| Script imports available to `.csx` | `Goose/Scripting/Script.cs:36-39` | already includes `Goose.Quests` and `System.Text.Json` — no change needed |
| `ScriptHandler.GetScript<T>(string filePath)` | `Goose/Scripting/ScriptHandler.cs:20` | prefixes `GameWorld.Settings.DataPathAbsolute + "/"`; **caches by resolved path, one instance shared across rows** |
| `ScriptHandler.ReloadScripts()` | `Goose/Scripting/ScriptHandler.cs:33` | walks its own cache, so `/reloadscripts` needs no new wiring |
| `IItemModifierScript` + `ItemModifier.FromReader(reader, world, dict)` script-loading block | `Goose/ItemModifier.cs:24,42-48` | the exact shape Task 2 copies |
| `enum RequirementType` (7 members, `NothingEquipped` last) | `Goose/Quests/QuestRequirement.cs:10-19` | `Script` appends as 7 |
| `class QuestRequirement` (internal), `FromReader(DbDataReader)` | `Goose/Quests/QuestRequirement.cs:21,35` | gains `world` param |
| `enum RewardType` (21 members, `LearnSpell` last) | `Goose/Quests/QuestReward.cs:11-33` | `Script` appends as 21 |
| `class QuestReward` (internal), `FromReader(DbDataReader)` | `Goose/Quests/QuestReward.cs:35,44` | gains `world` param |
| `class Quest` (internal), `FromReader(reader, quests)` | `Goose/Quests/Quest.cs:11,41` | visibility only |
| `class QuestProgress` (internal) | `Goose/Quests/QuestProgress.cs:9` | visibility only |
| `QuestHandler.LoadQuests(world)` — the only caller of both `FromReader`s | `Goose/Quests/QuestHandler.cs:45,66` | req at :45, reward at :66 |
| `QuestWindow` (internal), `enum QuestWindowState` | `Goose/Quests/QuestWindow.cs:8,10` | new state appended |
| `QuestWindow.Populate` switch | `Goose/Quests/QuestWindow.cs:92,95` | new `case` at :115-117 neighbourhood |
| `QuestWindow.Clicked` gate chain | `Goose/Quests/QuestWindow.cs:151-166` | `PlayerMeetsRequirements` :151, inventory :153, spellbook :157, `CompleteQuest` :164 |
| `QuestWindow.GetQuestProgressText(player, world)` | `Goose/Quests/QuestWindow.cs:189`, `OrderBy(r => r.Type)` at :193 | `Script`=7 sorts last |
| `QuestWindow.PlayerMeetsRequirements(Player)` + `default: return false` | `Goose/Quests/QuestWindow.cs:229,264-265` | signature gains `world` |
| `QuestWindow.GiveRewards(npc, player, world)` | `Goose/Quests/QuestWindow.cs:305`, switch :311 | `LearnSpell` arm sets no `rewardMessage` — precedent for a silent arm |
| `QuestWindow.TakeRequirements(player, world)` + `KeepRequirement` guard | `Goose/Quests/QuestWindow.cs:459,463`, `default: break` :489-490 | new arm goes inside the guard |
| `Window.SendCreate`, `Window.Populate`, `Window.Buttons` | `Goose/Window.cs:295,155,84` | |
| `GameWorld.LoadStep(name, action, countFn)` — logs `Fatal`, returns false, aborts | `Goose/GameWorld.cs:333,347-352` | `LoadStep("Quests", ...)` at :276 |
| `ReloadSqlCommandEvent` calls `LoadQuests` inside try/catch | `Goose/Events/ReloadSQLCommandEvent.cs:26,37-41` | reports `"Failed reloading sql: " + e.Message` to the GM |
| `GameWorld.ScriptHandler`, `GameWorld(GameServer)` ctor | `Goose/GameWorld.cs:48,139` | ctor accepts `null` server (verified by probe) |
| `GameWorld.QuestHandler` (internal) | `Goose/GameWorld.cs:47` | stays internal |
| `GameWorld.Send(Player, string)` | `Goose/GameWorld.cs:552` | no-ops safely when the player has no socket |
| `P.ServerMessage` | `Goose/Packets.cs:12` | `"$7" + message` |
| `Inventory.GetNumberOfFreeSlots()`, `HasItem(int, long)` | `Goose/Inventory.cs:129,798` | used by the example script |
| `Player.Gold` | `Goose/Player.cs:294` | |
| `Player.TalkedTo` (internal) | `Goose/Player.cs:1023` | called by the `QuestWindow` ctor; dereferences `npc.NPCTemplate.NPCTemplateID`, so the fixture NPC needs a template |
| `Player(int unused)` ctor — initializes `Windows`, `QuestProgress`, `QuestsCompleted`, `QuestsStarted` | `Goose/Player.cs:465-481` | **`new Player()` leaves these null**; tests must use `new Player(0)` |
| `Inventory(Player)` ctor reads `InventorySize`/`EquippedSize`/`CombineBagSize` | `Goose/Inventory.cs:45-54` | `Player(int)` does **not** create an Inventory; tests assign one |
| `Spellbook(Player)` ctor | `Goose/Spellbook.cs:23` | same — assigned by the test fixture |
| `Inventory.GetNumberOfFreeSlots` loops to `Settings.InventorySize` | `Goose/Inventory.cs:129-138` | returns 0 unless `InventorySize` is set in fixture settings |
| `Player.RemoveGold` → `P` packet → `Player.MaxHP` | `Goose/Player.cs:1428`, `Goose/Packets.cs:372`, `Goose/Player.cs:198` | **NREs on a fixture player**: `MaxHP` needs stats the fixture does not build. Avoid `Gold`/stat requirement and reward rows in `CompleteQuest` tests — see Task 3 test approach |
| `JsonHelper.DatabaseOptions` | `Goose/JsonHelper.cs:12` | what `HealerNPC.csx` uses to read `ScriptParams` |
| `GooseSettings.DataPathAbsolute` | `Goose/GooseSettings.cs:18-21` | absolute `DataPath` passes through unchanged — how tests point at a temp script dir |
| `Col.Text(name, def:)`, `Col.Enum<T>(name, sqlType)` | `CsvToSql.Core/Schema` — used at `SpellEffectsCsvToSql.cs:105-106`, `QuestRequirementsCsvToSql.cs:11` | |
| `QuestRequirementsCsvToSql` / `QuestRewardsCsvToSql` mirrored enums | `CsvToSql.Core/QuestRequirementsCsvToSql.cs:15-24`, `QuestRewardsCsvToSql.cs:18-40` | must stay in sync with the server enums |
| `SchemaModel.Build()`, `SchemaJs.Render` | `tools/SchemaGen/SchemaModel.cs:34`, `Program.cs:13-14` | `SchemaGen <output/schema.js>` |
| Snapshot regeneration flag | `Goose.Tests/CsvToSqlSnapshotTests.cs:29,56` | `GOOSE_UPDATE_SNAPSHOT=1` |

### Harness facts established by throwaway probe (probes deleted)

These were run as real tests in this worktree and passed, then removed. They are why the test tasks below are known-feasible rather than hoped-for:

1. A minimal `DbDataReader` subclass overriding only `this[string name]` (plus abstract stubs that throw) is sufficient to drive `FromReader`, because `FromReader` only ever indexes by column name. **Task 0 adds it as a real fixture.**
2. `new Script<T>(absolutePath)` compiles and instantiates a `.csx` inside the xunit host — Roslyn scripting works in-process, no server required.
3. `new GameWorld(null)` succeeds (no `GameServer` needed) as long as `GameWorld.Settings` is assigned first; it constructs a real `ScriptHandler`.
4. `GameWorld.Settings = new GooseSettings { DataPath = <absolute temp dir> }` makes `ScriptHandler.GetScript` resolve `"Scripts/Quest/X.csx"` under that temp dir. This is how script-loading tests avoid depending on shipped game data.
5. **`QuestWindow` is internal and `Goose.Tests` cannot see it** — `error CS0122`. Adding an `InternalsVisibleTo("Goose.Tests")` assembly attribute to `Goose/Goose.csproj` fixes it and the solution still builds. **Task 0 does this.**
6. **A `QuestWindow` CAN be constructed in-process, but only with a fully-built fixture.** Three NREs were hit and resolved while proving this, and each one is a trap the implementer would otherwise rediscover:
   - `new Player()` leaves `Windows` null → `QuestWindow.cs:39` NREs. Use **`new Player(0)`** (`Player.cs:465`), which initializes `Windows` and the three quest collections.
   - `player.TalkedTo` (`QuestWindow.cs:40`) dereferences `npc.NPCTemplate.NPCTemplateID`, so the NPC needs `NPCTemplate = new NPCTemplate { NPCTemplateID = ... }`.
   - `Player(int)` does **not** create `Inventory`/`Spellbook`; assign both, and set `InventorySize`, `EquippedSize`, `CombineBagSize`, `SpellbookSize` in the fixture `GooseSettings` or `GetNumberOfFreeSlots()` returns 0 and the inventory gate blocks every completion.
   - **`CompleteQuest` with a `Gold` requirement or reward NREs**: `Player.RemoveGold` (`:1428`) builds a status packet that reads `Player.MaxHP` (`:198`), which needs stat machinery the fixture does not construct. Script-only quests are unaffected because their take/give paths call only script hooks. This verified fixture completes a quest successfully:

```csharp
GameWorld.Settings = new GooseSettings
{
    DataPath = "Data/Illutia", ExperienceModifier = 1,
    InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
};
var world = new GameWorld(null);
var player = new Player(0) { Class = new Class { ClassID = 1, ClassName = "C" }, Gold = 100 };
player.Inventory = new Inventory(player);
player.Spellbook = new Spellbook(player);
var npc = new NPC { NPCTemplate = new NPCTemplate { NPCTemplateID = 5 } };
var quest = new Quest { Id = 1, Name = "Q", Description = "d" };
var window = new QuestWindow(npc, player, quest, world);   // works
window.CompleteQuest(npc, player, world);                   // works, given no Gold/stat rows
```

---

## Persistence strategy

Explicit, because updating `CREATE TABLE` SQL does **not** touch existing databases:

- **Fresh installs:** new columns inline in `Goose/sql/quests.sql`.
- **Existing databases:** checked-in `ALTER TABLE` statements appended to `Goose/sql/onetimeupdates.sql`, matching how `spell_effects.script_path` shipped (`Goose/sql/onetimeupdates.sql:27-28`).
- **No automatic migration in code.** An operator runs the update SQL. Per `docs/DEPLOY.md` the server is stopped for DB edits.
- Both columns are `NOT NULL DEFAULT ''`, so existing rows migrate without a data backfill and every pre-existing quest keeps working untouched.

---

## Task 0: Prerequisites — test visibility and a reader fixture

Nothing here changes runtime behaviour; it unblocks every later test task.

**Files:**
- Modify: `Goose/Goose.csproj` (new `ItemGroup` before the `sql/*.sql` one at :17)
- Create: `Goose.Tests/Fakes/FakeDbDataReader.cs`

**Step 1: Expose internals to the test project**

`QuestWindow` stays internal per the design, and `Quest`/`QuestRequirement`/`QuestReward` are internal until Task 1 — tests need to reach all of them. Add to `Goose/Goose.csproj`:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
    <_Parameter1>Goose.Tests</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

**Step 2: Add the reader fake**

A `DbDataReader` whose only real member is the string indexer, because that is the only member `FromReader` uses (`Goose/Quests/QuestRequirement.cs:39-43` indexes `reader["id"]` etc. and never calls a typed getter).

```csharp
using System.Collections;
using System.Data.Common;

namespace Goose.Tests.Fakes;

/// <summary>Drives the various FromReader methods, which only ever index by column name.
/// Every other member throws: if a FromReader starts calling GetInt32/GetString, this fake
/// should fail loudly rather than silently return a default.</summary>
public sealed class FakeDbDataReader : DbDataReader
{
    private readonly Dictionary<string, object> values;

    public FakeDbDataReader(Dictionary<string, object> values) => this.values = values;

    public override object this[string name] => values[name];

    public override int FieldCount => values.Count;
    public override bool HasRows => true;
    public override bool IsClosed => false;
    public override int Depth => 0;
    public override int RecordsAffected => 0;
    public override bool NextResult() => false;
    public override bool Read() => false;

    public override object this[int ordinal] => throw new NotSupportedException();
    public override bool GetBoolean(int i) => throw new NotSupportedException();
    public override byte GetByte(int i) => throw new NotSupportedException();
    public override long GetBytes(int i, long o, byte[]? b, int bo, int l) => throw new NotSupportedException();
    public override char GetChar(int i) => throw new NotSupportedException();
    public override long GetChars(int i, long o, char[]? b, int bo, int l) => throw new NotSupportedException();
    public override string GetDataTypeName(int i) => throw new NotSupportedException();
    public override DateTime GetDateTime(int i) => throw new NotSupportedException();
    public override decimal GetDecimal(int i) => throw new NotSupportedException();
    public override double GetDouble(int i) => throw new NotSupportedException();
    public override Type GetFieldType(int i) => throw new NotSupportedException();
    public override float GetFloat(int i) => throw new NotSupportedException();
    public override Guid GetGuid(int i) => throw new NotSupportedException();
    public override short GetInt16(int i) => throw new NotSupportedException();
    public override int GetInt32(int i) => throw new NotSupportedException();
    public override long GetInt64(int i) => throw new NotSupportedException();
    public override string GetName(int i) => throw new NotSupportedException();
    public override int GetOrdinal(string name) => throw new NotSupportedException();
    public override string GetString(int i) => throw new NotSupportedException();
    public override object GetValue(int i) => throw new NotSupportedException();
    public override int GetValues(object[] values) => throw new NotSupportedException();
    public override bool IsDBNull(int i) => throw new NotSupportedException();
    public override IEnumerator GetEnumerator() => throw new NotSupportedException();
}
```

**Step 3: Verify the baseline still passes**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 81` (the fake is unreferenced so far; this only proves `InternalsVisibleTo` did not break the build).

**Step 4: Commit**

```bash
git add Goose/Goose.csproj Goose.Tests/Fakes/FakeDbDataReader.cs
git commit -m "test: expose Goose internals to tests and add a DbDataReader fake"
```

---

## Task 1: `IQuestScript`, `BaseQuestScript`, and quest type visibility

**Files:**
- Create: `Goose/Scripting/IQuestScript.cs`, `Goose/Scripting/BaseQuestScript.cs`
- Modify: `Goose/Quests/Quest.cs:11`, `Goose/Quests/QuestRequirement.cs:10,21`, `Goose/Quests/QuestReward.cs:11,35`, `Goose/Quests/QuestProgress.cs:9`
- Test: `Goose.Tests/QuestScriptTests.cs`

**Mutation impact:**
- Source of truth changed: type accessibility only — `Goose/Quests/Quest.cs:11`, `QuestRequirement.cs:10,21`, `QuestReward.cs:11,35`, `QuestProgress.cs:9`. No field, no persisted value, no runtime state.
- Important readers: `Goose/Quests/QuestWindow.cs`, `Goose/Quests/QuestHandler.cs`, `Goose/Player.cs:445-447,1020-1061`, `Goose/NPC.cs`, `Goose/PlayerInfoWindow.cs`. All are in the same assembly and unaffected by widening.
- Derived/cached state affected: none. Widening accessibility cannot change behaviour of existing in-assembly callers.
- Required propagation sequence:
  1. Add `public` to the six type/enum declarations.
  2. Confirm no *public* signature now leaks a still-internal type (C# would emit CS0050/CS0051 — "inconsistent accessibility"). `Quest.Requirements`/`Rewards` are `List<QuestRequirement>`/`List<QuestReward>`, both promoted together, so this is consistent. `QuestRequirement.Quest` returns `Quest`, promoted. `QuestProgress.Requirement` returns `QuestRequirement`, promoted.
  3. Leave `QuestWindow` internal, `GameWorld.QuestHandler` internal (`Goose/GameWorld.cs:47`), and `Player.QuestsStarted`/`QuestsCompleted`/`QuestProgress` internal (`Goose/Player.cs:445-447`).
- Invariants to preserve:
  - `QuestWindow` is not reachable from a script.
  - The compiler, not a reviewer, enforces accessibility consistency.
- Observable proof required: the solution compiles, and a `.csx` compiled at runtime can name `QuestRequirement`/`QuestReward`/`RequirementType` — which is the actual point of the change and cannot be proven by a compile-time reference from `Goose.Tests` (that project sees internals anyway thanks to Task 0). **The test must go through Roslyn.**

**Step 1: Write the failing tests**

`Goose.Tests/QuestScriptTests.cs`. Test 1 is the adversarial one: it fails today because `.csx` compilation cannot see internal types, and `Goose.Tests`'s own `InternalsVisibleTo` does not help a dynamically compiled script.

```csharp
using Goose.Quests;
using Goose.Scripting;

namespace Goose.Tests;

public class QuestScriptTests
{
    /// <summary>Writes a .csx into a temp data dir and compiles it the way the server does.
    /// Absolute DataPath passes through DataPathAbsolute unchanged (GooseSettings.cs:18-21), so
    /// this never touches shipped game data.</summary>
    private static IQuestScript Compile(string body)
    {
        var dir = Path.Combine(Path.GetTempPath(), "quest-script-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "Scripts", "Quest"));
        File.WriteAllText(Path.Combine(dir, "Scripts", "Quest", "T.csx"), body);

        GameWorld.Settings = new GooseSettings
        {
            DataPath = dir, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        var world = new GameWorld(null);
        return world.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/T.csx").Object;
    }

    [Fact]
    public void A_script_can_name_the_quest_types_it_is_handed()
    {
        // Red before Task 1: Quest/QuestRequirement/RequirementType are internal, so Roslyn
        // reports CS0122 and Script<T>.LoadScript throws out of script.Compile().
        var script = Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        => requirement.Type == RequirementType.Script && requirement.Quest != null;
}
return typeof(T);
");
        var quest = new Quest { Id = 1 };
        var req = new QuestRequirement { Type = RequirementType.Script, Quest = quest };

        // Player(0), not Player() — the parameterless ctor leaves collections null (Player.cs:465).
        Assert.True(script.IsMet(req, new Player(0), null));
    }

    [Fact]
    public void Base_defaults_allow_completion_and_add_nothing()
    {
        var script = new BaseQuestScript();
        var player = new Player(0);
        var req = new QuestRequirement { Type = RequirementType.Script };
        var reward = new QuestReward { Type = RewardType.Script };

        Assert.True(script.IsMet(req, player, null));
        Assert.Equal("", script.GetProgressText(req, player, null));
        Assert.Null(script.CanComplete(reward, player, null));
        script.OnTakeRequirement(req, player, null);   // must not throw
        script.GiveReward(reward, null, player, null); // must not throw
    }
}
```

**Step 2: Run to verify red**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestScriptTests`
Expected: both fail — `BaseQuestScript`/`IQuestScript` do not exist yet (CS0246 at build). After only the two new files are added but before the visibility change, test 1 would fail at runtime with a Roslyn `CS0122 'Quest' is inaccessible` surfacing from `script.Compile()`. Both red states are expected and distinct; the second is the one the visibility change fixes.

**Step 3: Implement**

`Goose/Scripting/IQuestScript.cs`:

```csharp
using Goose.Quests;

namespace Goose.Scripting
{
    /// <summary>Hooks for RequirementType.Script and RewardType.Script rows. One interface covers
    /// both roles so a single script file can implement a paired requirement + reward behaviour;
    /// a script only interested in one role inherits the other's no-ops from BaseQuestScript.
    ///
    /// IMPORTANT: ScriptHandler caches ONE instance per file path, shared by every row pointing at
    /// that file (ScriptHandler.cs:20-30). ScriptParams is per-ROW. Deserialize
    /// requirement.ScriptParams / reward.ScriptParams inside each call — never cache it in a field
    /// between calls, or a second row using the same script will read the first row's params.</summary>
    public interface IQuestScript
    {
        // Requirement role
        bool IsMet(QuestRequirement requirement, Player player, GameWorld world);
        string GetProgressText(QuestRequirement requirement, Player player, GameWorld world);
        void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world);

        // Reward role
        /// <summary>null or empty to allow completion; otherwise the message shown to the player
        /// instead of completing the quest. Supports \n the same way quest Description does.</summary>
        string CanComplete(QuestReward reward, Player player, GameWorld world);
        void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world);
    }
}
```

`Goose/Scripting/BaseQuestScript.cs`: `public class BaseQuestScript : IQuestScript` with a public parameterless ctor and the defaults from the design table — `IsMet` → `true`, `GetProgressText` → `""`, `OnTakeRequirement` → empty, `CanComplete` → `null`, `GiveReward` → empty. All `virtual`, matching `BaseItemScript` (`Goose/Scripting/BaseItemScript.cs`).

Then add `public` to: `Quest` (`Quest.cs:11`), `RequirementType` and `QuestRequirement` (`QuestRequirement.cs:10,21`), `RewardType` and `QuestReward` (`QuestReward.cs:11,35`), `QuestProgress` (`QuestProgress.cs:9`).

**Step 4: Verify green**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestScriptTests`
Expected: `Passed: 2`.
Then the full suite: `dotnet test Goose.Tests/Goose.Tests.csproj` → `Passed: 83`.

| Invariant | Proved by |
|---|---|
| A `.csx` can name the quest types passed to its hooks | `A_script_can_name_the_quest_types_it_is_handed` (adversarial: fails on the internal types, which no compile-time test would catch) |
| Base defaults never block completion or add text | `Base_defaults_allow_completion_and_add_nothing` |
| `QuestWindow` stays unreachable from scripts | Compile-time: it keeps no access modifier; no script test references it |
| Accessibility stays consistent | Compile-time: CS0050/CS0051 would fire |

**Step 5: Commit**

```bash
git add Goose/Scripting/IQuestScript.cs Goose/Scripting/BaseQuestScript.cs Goose/Quests Goose.Tests/QuestScriptTests.cs
git commit -m "feat: add IQuestScript and make quest types public"
```

---

## Task 2: Enum values, schema columns, and script loading

Grouped deliberately: the enum value, the column, and the `FromReader` validation are one atomic change — adding the enum without the column gives a type nothing can configure, and adding the column without validation gives the silent-failure mode the design rejects.

**Files:**
- Modify: `Goose/Quests/QuestRequirement.cs` (enum + properties + `FromReader`), `Goose/Quests/QuestReward.cs` (same), `Goose/Quests/QuestHandler.cs:45,66` (pass `world`)
- Modify: `Goose/sql/quests.sql`, `Goose/sql/onetimeupdates.sql`
- Modify: `CsvToSql/CsvToSql.Core/QuestRequirementsCsvToSql.cs`, `CsvToSql/CsvToSql.Core/QuestRewardsCsvToSql.cs`
- Test: `Goose.Tests/QuestScriptLoadingTests.cs`

**Mutation impact:**
- Source of truth changed:
  - Enum members: `Goose/Quests/QuestRequirement.cs:10-19` (`Script` = 7), `Goose/Quests/QuestReward.cs:11-33` (`Script` = 21). Mirrored in `CsvToSql.Core/QuestRequirementsCsvToSql.cs:15-24` and `QuestRewardsCsvToSql.cs:18-40`.
  - Persisted schema: `quest_requirements` and `quest_rewards` gain `script_path`, `script_params`.
  - New in-memory fields: `QuestRequirement.Script`/`ScriptParams`, `QuestReward.Script`/`ScriptParams`.
- Important readers:
  - `RequirementType` is switched on at `QuestWindow.cs:195` (progress text), `:233` (met check), `:465` (take), and `Player.cs:1028` (`UpdatePossibleQuestProgress`). `RewardType` at `QuestWindow.cs:311`, `:277`, `:283`. Task 3 adds the `Script` arms; **until then a `Script` requirement hits `default: return false` (`:264-265`) and a `Script` reward falls through `GiveRewards` silently.** That is why Task 3 must land before any data uses the new types.
  - The enum *names* are read by the editor: `SchemaColumn.EnumNames` (`tools/SchemaGen/SchemaModel.cs:45`) is populated for `ColumnKind.Enum`, so appending a member changes `schema.js` and the DataEditor dropdown.
  - `Player.UpdatePossibleQuestProgress` (`Goose/Player.cs:1028`) filters by `RequirementType`; it is only ever called with `Kill`/`TalkToNPC` (`:1020,:1025`), so appending `Script` cannot affect it.
- Derived/cached state affected:
  - `ScriptHandler.scripts` dictionary gains entries — path-keyed, shared, and already covered by `ReloadScripts` (`ScriptHandler.cs:33`), so `/reloadscripts` picks up quest scripts with no new code.
  - `Goose.Tests/Fixtures/generated.snapshot` records every column of every table and **will fail** until regenerated.
  - `tools/DataEditor/schema.js` is a generated artifact and must be regenerated.
  - Player save data (`quest_status`, `Player.cs:1043-1061`) stores quest ids and requirement ids only, never a `RequirementType` value, so no saved-data migration is needed.
- Required propagation sequence:
  1. Append `Script` to both server enums **at the end** — existing rows store numeric values, so inserting mid-enum would silently reinterpret shipped data.
  2. Append the same member to both mirrored `CsvToSql` enums, keeping index parity with the server.
  3. Add `script_path`/`script_params` to `quests.sql` (fresh) *and* `onetimeupdates.sql` (existing DBs).
  4. Append the two `Col.Text` descriptors so `SchemaRegistry` → `SchemaGen` picks them up.
  5. Add `Script`/`ScriptParams` properties to both classes.
  6. In both `FromReader`s: read `script_params`, resolve `script_path` via `world.ScriptHandler.GetScript<IQuestScript>`, then throw if type is `Script` and `Script` is null.
  7. Update both call sites in `QuestHandler.LoadQuests` (`:45`, `:66`) to pass `world`. `LoadQuests` already has `world` as a parameter.
  8. Regenerate `schema.js`, then regenerate the CsvToSql snapshot and **read the diff** — it should show exactly four added columns and nothing else.
- Invariants to preserve:
  - Existing numeric enum values in shipped data keep their meaning.
  - A `Script`-typed row with no resolvable script aborts loading rather than producing an uncompletable quest.
  - Existing quests with no script load unchanged, with `Script == null` and `ScriptParams == ""`.
  - The exception message names the row id and quest id, because `/reloadsql` shows only `e.Message` to the GM (`ReloadSQLCommandEvent.cs:40`).
- Observable proof required: tests assert the thrown message and the resulting object state, not that `GetScript` was called.

**Step 1: Write the failing tests**

`Goose.Tests/QuestScriptLoadingTests.cs`. The first two are the regression-focused ones — they encode the design's fail-fast decision, and would pass under the rejected "silently leave Script null" implementation only if inverted.

```csharp
using Goose.Quests;
using Goose.Scripting;
using Goose.Tests.Fakes;

namespace Goose.Tests;

public class QuestScriptLoadingTests
{
    private static GameWorld WorldWithScriptDir(out string dir)
    {
        dir = Path.Combine(Path.GetTempPath(), "quest-load-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "Scripts", "Quest"));
        File.WriteAllText(Path.Combine(dir, "Scripts", "Quest", "Ok.csx"), @"
using Goose; using Goose.Quests; using Goose.Scripting;
public class Ok : BaseQuestScript { }
return typeof(Ok);
");
        GameWorld.Settings = new GooseSettings { DataPath = dir, ExperienceModifier = 1 };
        return new GameWorld(null);
    }

    private static FakeDbDataReader RequirementRow(int type, string scriptPath) =>
        new(new Dictionary<string, object>
        {
            ["id"] = 42,
            ["requirement_type"] = type,
            ["requirement_value"] = 0L,
            ["requirement_value2"] = 0L,
            ["keep_requirement"] = "0",
            ["script_path"] = scriptPath,
            ["script_params"] = "{}",
        });

    private static FakeDbDataReader RewardRow(int type, string scriptPath) =>
        new(new Dictionary<string, object>
        {
            ["id"] = 43,
            ["reward_type"] = type,
            ["long_value"] = 0L,
            ["long_value2"] = 0L,
            ["string_value"] = "",
            ["script_path"] = scriptPath,
            ["script_params"] = "{}",
        });

    [Fact]
    public void A_script_requirement_without_a_script_path_fails_loading()
    {
        var world = WorldWithScriptDir(out _);
        var quest = new Quest { Id = 9 };

        var e = Assert.Throws<Exception>(() =>
            QuestRequirement.FromReader(RequirementRow((int)RequirementType.Script, ""), world, quest));

        // /reloadsql shows only e.Message to the GM (ReloadSQLCommandEvent.cs:40), so both ids
        // must be in the message or the GM cannot find the bad row.
        Assert.Contains("42", e.Message);
        Assert.Contains("9", e.Message);
    }

    [Fact]
    public void A_script_reward_without_a_script_path_fails_loading()
    {
        var world = WorldWithScriptDir(out _);
        var quest = new Quest { Id = 9 };

        var e = Assert.Throws<Exception>(() =>
            QuestReward.FromReader(RewardRow((int)RewardType.Script, ""), world, quest));

        Assert.Contains("43", e.Message);
        Assert.Contains("9", e.Message);
    }

    [Fact]
    public void A_script_path_naming_a_missing_file_fails_loading()
    {
        var world = WorldWithScriptDir(out _);
        Assert.ThrowsAny<Exception>(() => QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Nope.csx"), world, new Quest { Id = 9 }));
    }

    [Fact]
    public void A_script_requirement_with_a_script_loads()
    {
        var world = WorldWithScriptDir(out _);
        var req = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Ok.csx"), world, new Quest { Id = 9 });

        Assert.NotNull(req.Script);
        Assert.NotNull(req.Script.Object);
        Assert.Equal("{}", req.ScriptParams);
    }

    [Fact]
    public void A_non_script_requirement_without_a_script_loads_unchanged()
    {
        // The regression guard for every quest already in the shipped data.
        var world = WorldWithScriptDir(out _);
        var req = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Gold, ""), world, new Quest { Id = 9 });

        Assert.Equal(RequirementType.Gold, req.Type);
        Assert.Null(req.Script);
    }

    [Fact]
    public void Two_rows_sharing_a_script_path_share_one_instance()
    {
        // Pins the shared-instance behaviour the interface doc warns about, so a future change
        // to per-row instances is a deliberate, visible decision.
        var world = WorldWithScriptDir(out _);
        var a = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Ok.csx"), world, new Quest { Id = 9 });
        var b = QuestRequirement.FromReader(
            RequirementRow((int)RequirementType.Script, "Scripts/Quest/Ok.csx"), world, new Quest { Id = 10 });

        Assert.Same(a.Script, b.Script);
    }
}
```

Note the signature these tests imply: `FromReader(DbDataReader reader, GameWorld world, Quest quest)`. The `quest` parameter is needed for the error message, and for requirements it also replaces `QuestHandler.cs:46`'s separate `req.Quest = quest;` assignment.

**Step 2: Run to verify red**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestScriptLoadingTests`
Expected: build failure — `RequirementType.Script` does not exist, and `FromReader` takes one argument.

**Step 3: Implement**

Enums (append only):

```csharp
// Goose/Quests/QuestRequirement.cs
NothingEquipped,
Script,          // 7 — logic lives in script_path

// Goose/Quests/QuestReward.cs
LearnSpell,
Script,          // 21 — logic lives in script_path
```

Mirror both in `CsvToSql.Core/QuestRequirementsCsvToSql.cs:15-24` and `QuestRewardsCsvToSql.cs:18-40`, in the same position.

Properties on both classes, matching `ItemModifier.cs:21-22`:

```csharp
public Script<IQuestScript> Script { get; set; }
public string ScriptParams { get; set; }
```

`QuestRequirement.FromReader` — new signature and trailing block:

```csharp
public static QuestRequirement FromReader(DbDataReader reader, GameWorld world, Quest quest)
{
    var requirement = new QuestRequirement();
    requirement.Quest = quest;
    // ... existing field reads unchanged (QuestRequirement.cs:39-43) ...

    requirement.ScriptParams = Convert.ToString(reader["script_params"]);
    string scriptPath = Convert.ToString(reader["script_path"]);
    if (!string.IsNullOrEmpty(scriptPath))
    {
        requirement.Script = world.ScriptHandler.GetScript<IQuestScript>(scriptPath);
    }

    if (requirement.Type == RequirementType.Script && requirement.Script == null)
    {
        throw new Exception($"Quest requirement {requirement.Id} (quest {quest.Id}) has type Script but no script_path");
    }

    return requirement;
}
```

`QuestReward.FromReader` mirrors this against `RewardType.Script`, with `"Quest reward {reward.Id} (quest {quest.Id}) has type Script but no script_path"`. `QuestReward` has no `Quest` property today; take `quest` for the message only and do not add one (YAGNI — nothing reads it).

`QuestHandler.LoadQuests`: change `:45` to `QuestRequirement.FromReader(reader, world, quest)` and drop the now-redundant `req.Quest = quest;` at `:46`; change `:66` to `QuestReward.FromReader(reader, world, quest)`.

SQL — `Goose/sql/quests.sql`, inline in both `CREATE TABLE`s:

```sql
  keep_requirement CHAR(1) DEFAULT '0',
  script_path TEXT DEFAULT '' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL
```

```sql
  string_value TEXT DEFAULT '',
  script_path TEXT DEFAULT '' NOT NULL,
  script_params TEXT DEFAULT '' NOT NULL
```

`Goose/sql/onetimeupdates.sql`, appended (same style as `:27-28`):

```sql
ALTER TABLE quest_requirements ADD script_path TEXT DEFAULT '' NOT NULL;
ALTER TABLE quest_requirements ADD script_params TEXT DEFAULT '' NOT NULL;
ALTER TABLE quest_rewards ADD script_path TEXT DEFAULT '' NOT NULL;
ALTER TABLE quest_rewards ADD script_params TEXT DEFAULT '' NOT NULL;
```

Descriptors — append to both `GetColumnDescriptors()` arrays, exactly as `SpellEffectsCsvToSql.cs:105-106`:

```csharp
Col.Text("script_path", def: "''"),
Col.Text("script_params", def: "''"),
```

**Step 4: Verify green, then regenerate the two artifacts**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestScriptLoadingTests   # Passed: 6
```

The snapshot test now fails — expected, and its own docs say the diff is the review (`CsvToSqlSnapshotTests.cs:24-32`):

```bash
GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.Tests --filter FullyQualifiedName~CsvToSqlSnapshot
git diff Goose.Tests/Fixtures/generated.snapshot
```

Read it: the only changes must be `script_path`/`script_params` appearing on `quest_requirements` and `quest_rewards`. Anything else means a descriptor was edited wrongly — stop and fix rather than committing the diff.

```bash
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js
git diff --stat tools/DataEditor/schema.js
```

Then the whole suite: `dotnet test Goose.Tests/Goose.Tests.csproj` → `Passed: 89`.

| Invariant | Proved by |
|---|---|
| `Script` row with no path aborts loading, naming row + quest | `A_script_requirement_without_a_script_path_fails_loading`, `..._reward_...` (both adversarial: they fail on the silent-null implementation) |
| Missing script file aborts loading | `A_script_path_naming_a_missing_file_fails_loading` |
| A valid script row resolves an instance and keeps its params | `A_script_requirement_with_a_script_loads` |
| Pre-existing non-script rows load unchanged | `A_non_script_requirement_without_a_script_loads_unchanged` |
| One instance shared per path (the `ScriptParams` trap) | `Two_rows_sharing_a_script_path_share_one_instance` |
| Enum values append, never shift | `generated.snapshot` diff review + editor `EnumNames` in `schema.js` diff |
| Existing DBs can migrate | `onetimeupdates.sql` reviewed by hand; no automated proof (no migration harness exists) |

**Step 5: Commit**

```bash
git add Goose/Quests Goose/sql CsvToSql/CsvToSql.Core Goose.Tests/QuestScriptLoadingTests.cs \
        Goose.Tests/Fixtures/generated.snapshot tools/DataEditor/schema.js
git commit -m "feat: add Script requirement/reward types with script_path columns"
```

---

## Task 3: QuestWindow integration

**Files:**
- Modify: `Goose/Quests/QuestWindow.cs` — `QuestWindowState` (:10-19), `Populate` (:95), `Clicked` (:151-166), `GetQuestProgressText` (:189), `PlayerMeetsRequirements` (:229), `GiveRewards` (:311), `TakeRequirements` (:465)
- Test: `Goose.Tests/QuestWindowScriptTests.cs`

**Mutation impact:**
- Source of truth changed: quest completion control flow, plus a new `scriptCannotCompleteMessage` field and `QuestWindowState.QuestScriptCannotComplete` member.
- Important readers: `Populate` (`:92`) renders `state`; `Clicked` (`:131`) advances it. `state` is private to `QuestWindow` and never serialized or sent as a raw value — only the resulting text goes to the client via `P.WindowTextLine` (`:120`).
- Derived/cached state affected: `CompleteQuest` (`:289`) mutates `player.QuestsCompleted` and `player.QuestProgress`, which `Player.BuildSaveQuests` (`Player.cs:1043-1061`) persists. The new `CanComplete` gate sits *before* `CompleteQuest`, so a blocked completion must leave both collections untouched — that is the key invariant here.
- Required propagation sequence:
  1. `PlayerMeetsRequirements` gains `GameWorld world`; update the single call at `:151`.
  2. Add the `RequirementType.Script` arm before `default:` at `:264`.
  3. Add the `Script` arm to `GetQuestProgressText`'s switch (`:195`), appending only when the returned text is non-empty.
  4. Add the `Script` arm to `TakeRequirements`'s switch (`:465`), inside the `!requirement.KeepRequirement` guard at `:463`.
  5. Insert the `CanComplete` gate into `Clicked` after the spellbook check (`:157-160`) and before the `else` that calls `CompleteQuest` (`:161-165`).
  6. Add the `Script` arm to `GiveRewards` (`:311`), leaving `rewardMessage` null.
- Invariants to preserve:
  - A blocked `CanComplete` adds nothing to `player.QuestsCompleted` and removes nothing from `player.QuestProgress`.
  - `keep_requirement = 1` means `OnTakeRequirement` is not called.
  - An empty `GetProgressText` contributes no line.
  - Scripted progress lines follow built-in ones (`OrderBy(r => r.Type)` at `:193`, `Script` = 7 is highest).
- Observable proof required: assert `player.QuestsCompleted` / `QuestProgress` contents and the accumulated progress text — not that a hook was invoked.

**Test approach.** A `QuestWindow` can be built and driven in-process — verified, see harness fact 6 — using the exact fixture recorded there. Reuse it as a shared `QuestFixture` helper in this test file. Three rules the fixture imposes, each learned from an actual NRE:

- `new Player(0)`, never `new Player()`.
- The NPC needs an `NPCTemplate`.
- **Test quests must contain only `Script` requirement/reward rows.** A `Gold` or stat row makes `CompleteQuest` NRE inside `Player.RemoveGold` → `MaxHP`. This is not a limitation in practice: the behaviour under test is the script arms, and `Script`-only quests exercise them fully.

The one test that genuinely needs a built-in row alongside a scripted one is `Script_progress_text_is_appended_after_built_in_lines`, and it is safe because `GetQuestProgressText` only formats strings — it never calls `RemoveGold`.

Assert against the three seams directly: `PlayerMeetsRequirements`, `GetQuestProgressText`, and `CompleteQuest`. For the `CanComplete` gate, expose the new helper as `internal` (not private) so it can be asserted without driving packet-level `Clicked` input — `Clicked`'s only added logic is `if (helper() is non-empty) set state`, which the state assertion covers.

**Scripts must not mutate `player.Gold` as their observable side effect** (the first draft of this plan did exactly that): `Gold` is a plain field so setting it is safe, but reading it back proves little and it is one step from the `RemoveGold` NRE path. Prefer a side effect the test owns outright — a `static` counter or list on the script class, read back through `script.GetType().GetField(...)`, or simplest, have the script append to a `List<string>` exposed as a public static member and assert its contents.

**Step 1: Write the failing tests**

Sketch — the implementer fills in the shared fixture. Test scripts are compiled with the Task 1 `Compile` helper (extract it to a shared `QuestScriptFixture` when Task 3 needs it; that refactor is part of this task).

```csharp
[Fact] public void A_script_requirement_that_is_not_met_fails_the_quest()          // IsMet false → PlayerMeetsRequirements false
[Fact] public void A_script_requirement_that_is_met_passes_the_quest()             // IsMet true, no other reqs → true
[Fact] public void Script_progress_text_is_appended_after_built_in_lines()         // Gold + Script reqs → script line last
[Fact] public void An_empty_script_progress_text_adds_no_line()                    // base default → line count unchanged
[Fact] public void A_blocking_CanComplete_leaves_the_quest_uncompleted()           // ADVERSARIAL
[Fact] public void A_null_CanComplete_allows_completion()
[Fact] public void GiveReward_runs_on_completion()                                 // script sets a static flag; assert the flag
[Fact] public void OnTakeRequirement_runs_when_the_requirement_is_not_kept()
[Fact] public void OnTakeRequirement_is_skipped_when_keep_requirement_is_set()     // ADVERSARIAL
```

The two adversarial ones spelled out, since they encode decisions a wrong implementation would silently invert:

```csharp
[Fact]
public void A_blocking_CanComplete_leaves_the_quest_uncompleted()
{
    // Adversarial: passes a broken implementation that checks CanComplete *after* CompleteQuest
    // only if we assert persisted-collection state, so that is what we assert — not the message.
    var script = Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public static bool GaveReward = false;
    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
        => ""No room in your pack."";
    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
        => GaveReward = true;
}
return typeof(T);
");
    var (world, npc, player, quest) = QuestFixture(script, rewardType: RewardType.Script);
    var window = new QuestWindow(npc, player, quest, world);

    var message = window.GetScriptCannotCompleteMessage(player, world);

    Assert.Equal("No room in your pack.", message);
    Assert.Empty(player.QuestsCompleted);                                   // nothing persisted
    Assert.False((bool)script.GetType().GetField("GaveReward")!.GetValue(null)!);  // reward never ran
}

[Fact]
public void OnTakeRequirement_is_skipped_when_keep_requirement_is_set()
{
    // keep_requirement is the configured way to say "consume nothing", so the hook must not be
    // called at all. An implementation that adds the arm outside the guard at :463 fails here.
    var script = Compile(@"
using Goose; using Goose.Quests; using Goose.Scripting;
public class T : BaseQuestScript
{
    public static int TakeCalls = 0;
    public override void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world)
        => TakeCalls++;
}
return typeof(T);
");
    var (world, npc, player, quest) = QuestFixture(script, requirementType: RequirementType.Script);
    quest.Requirements[0].KeepRequirement = true;

    new QuestWindow(npc, player, quest, world).CompleteQuest(npc, player, world);

    Assert.Equal(0, (int)script.GetType().GetField("TakeCalls")!.GetValue(null)!);
}
```

Each compiled script gets a **fresh temp directory and therefore a fresh `ScriptHandler` cache entry**, so `static` counters do not bleed between tests. Do not share one script file across two tests that both assert a counter.

**Step 2: Run to verify red**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestWindowScriptTests`
Expected: build failure (`GetScriptCannotCompleteMessage` missing), then, once the helper exists but the arms do not, `A_script_requirement_that_is_not_met_fails_the_quest` fails *green-ishly* for the wrong reason — `default: return false` (`:264-265`) already returns false for a `Script` requirement. So pair it with `A_script_requirement_that_is_met_passes_the_quest`, which fails red until the arm exists. Both must be present; the "is met" one is the load-bearing test.

**Step 3: Implement**

New state member and field:

```csharp
QuestProgress,
QuestScriptCannotComplete,
```
```csharp
private string scriptCannotCompleteMessage;
```

`Populate` arm:

```csharp
case QuestWindowState.QuestScriptCannotComplete:
    text = this.scriptCannotCompleteMessage;
    break;
```

The existing `\\n` split at `:118` then handles multi-line script text with no further work.

`PlayerMeetsRequirements(Player player, GameWorld world)` — new arm before `default:`:

```csharp
case RequirementType.Script:
    if (!requirement.Script.Object.IsMet(requirement, player, world))
        return false;
    break;
```

`Script` is non-null by Task 2's load-time validation; no null check, matching `SpellEffect`'s treatment of a validated script.

`GetQuestProgressText` arm:

```csharp
case RequirementType.Script:
    var scriptText = requirement.Script.Object.GetProgressText(requirement, player, world);
    if (!string.IsNullOrEmpty(scriptText))
        text += scriptText + "\\n";
    break;
```

`TakeRequirements` arm (inside the `KeepRequirement` guard):

```csharp
case RequirementType.Script:
    requirement.Script.Object.OnTakeRequirement(requirement, player, world);
    break;
```

New helper — `internal` so tests can call it, and named for what it returns:

```csharp
/// <summary>The first blocking message from a Script reward, or null if every scripted reward
/// allows completion. Called before CompleteQuest so a block leaves player state untouched.</summary>
internal string GetScriptCannotCompleteMessage(Player player, GameWorld world)
{
    foreach (var reward in this.quest.Rewards.Where(r => r.Type == RewardType.Script))
    {
        var message = reward.Script.Object.CanComplete(reward, player, world);
        if (!string.IsNullOrEmpty(message)) return message;
    }

    return null;
}
```

`Clicked` gate, inserted after the spellbook branch at `:157-160`:

```csharp
else if (this.GetScriptCannotCompleteMessage(player, world) is string cannotComplete)
{
    this.scriptCannotCompleteMessage = cannotComplete;
    this.state = QuestWindowState.QuestScriptCannotComplete;
}
```

`GiveRewards` arm — deliberately leaves `rewardMessage` null, like the `LearnSpell` arm:

```csharp
case RewardType.Script:
    reward.Script.Object.GiveReward(reward, npc, player, world);
    break;
```

**Step 4: Verify green**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestWindowScriptTests` → all pass.
Then: `dotnet test Goose.Tests/Goose.Tests.csproj` → 89 + the new tests.

| Invariant | Proved by |
|---|---|
| Blocked completion persists nothing | `A_blocking_CanComplete_leaves_the_quest_uncompleted` (adversarial) |
| `keep_requirement` suppresses `OnTakeRequirement` | `OnTakeRequirement_is_skipped_when_keep_requirement_is_set` (adversarial) |
| `IsMet` gates the quest both ways | `A_script_requirement_that_is_{not_,}met_...` |
| Empty progress text adds no line | `An_empty_script_progress_text_adds_no_line` |
| Scripted progress lines come last | `Script_progress_text_is_appended_after_built_in_lines` |
| `GiveReward` runs on completion | `GiveReward_runs_on_completion` |
| Script hooks are not wrapped in try/catch | Compile-time/by inspection: deliberate, per design; matches `Map.cs:137` |

**Step 5: Commit**

```bash
git add Goose/Quests/QuestWindow.cs Goose.Tests/QuestWindowScriptTests.cs
git commit -m "feat: wire quest scripts into QuestWindow"
```

---

## Task 4: Example script and documentation

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Quest/ExampleQuestScript.csx`
- Modify: `Goose/Goose.csproj` if `Data/**` is not already copied to output (check the existing `None Update` groups at `:17-33` first — the other `Scripts/` dirs ship somehow, so most likely nothing is needed)

**Step 1: Write the example**

It must demonstrate the shared-instance rule from the design: deserialize `ScriptParams` **inside each call**, never into a field. This is the one thing a copy-pasting script author will get wrong, and `HealerNPC.csx` models the wrong pattern.

```csharp
using Goose;
using Goose.Quests;
using Goose.Scripting;
using System.Text.Json;

/// <summary>Both roles in one file: as a requirement, the player must hold N of an item AND be
/// above a gold floor; as a reward, hand back an item only if there is a free inventory slot.
///
/// NOTE the ScriptParams handling. ScriptHandler caches ONE instance per file path, shared by
/// every quest_requirements/quest_rewards row pointing here, but ScriptParams is per-row.
/// Deserializing into a field would make row B read row A's params. Always deserialize from the
/// row handed to the call, on every call.</summary>
private class ExampleParams
{
    public int itemId { get; set; }
    public int count { get; set; }
    public long minGold { get; set; }
}

public class ExampleQuestScript : BaseQuestScript
{
    private static ExampleParams Params(string scriptParams) =>
        JsonSerializer.Deserialize<ExampleParams>(scriptParams, JsonHelper.DatabaseOptions);

    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
    {
        var p = Params(requirement.ScriptParams);
        return player.Gold >= p.minGold && player.Inventory.HasItem(p.itemId, p.count);
    }

    public override string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
    {
        var p = Params(requirement.ScriptParams);
        var template = world.ItemHandler.GetTemplate(p.itemId);
        return string.Format("{0} ({1}) and {2:N0} gp", template.Name, p.count, p.minGold);
    }

    public override void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world)
    {
        var p = Params(requirement.ScriptParams);
        player.Inventory.RemoveItem(p.itemId, p.count, world);
    }

    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
    {
        return player.Inventory.GetNumberOfFreeSlots() > 0
            ? null
            : "You need a free inventory slot\\nbefore I can hand this over.";
    }

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var p = Params(reward.ScriptParams);
        var template = world.ItemHandler.GetTemplate(p.itemId);
        if (template == null) return;

        var item = new Item();
        item.LoadFromTemplate(template);
        world.ItemHandler.RollTitleAndSurname(item, world);
        world.ItemHandler.AddAndAssignId(item, world);
        player.Inventory.AddItem(item, p.count, world);

        world.Send(player, P.ServerMessage("[Quest Reward]: " + template.Name));
    }
}

return typeof(ExampleQuestScript);
```

Before committing, verify each API this example calls actually exists with these signatures — `Inventory.RemoveItem`, `ItemHandler.GetTemplate`, `RollTitleAndSurname`, `AddAndAssignId`, `Inventory.AddItem`, `Item.LoadFromTemplate` are all used by `QuestWindow.GiveRewards` (`:317-336`) and `TakeRequirements` (`:471`), so copy the exact call shapes from there rather than trusting this sketch.

**Step 2: Prove it compiles**

Add one test to `QuestScriptTests` that loads the *shipped* example through `ScriptHandler` against the real `Data/Illutia` path, asserting `Object` is non-null. This catches a syntax error or a renamed API in the example, which is otherwise dead code nothing exercises.

**Step 3: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Quest/ExampleQuestScript.csx Goose.Tests/QuestScriptTests.cs
git commit -m "docs: add an example quest script covering both roles"
```

---

## Task 5: Full verification

**Step 1: Whole suite**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj
dotnet test tools/Tools.Tests/Tools.Tests.csproj
```
Expected: all green. Baseline before this work was 81 in `Goose.Tests`.

**Step 2: Confirm generated artifacts are in sync**

```bash
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js
git status --short tools/DataEditor/schema.js
```
Expected: no diff — Task 2 already regenerated it. A diff here means a descriptor changed after regeneration.

**Step 3: Server smoke test**

Start the server against a database migrated with the new `onetimeupdates.sql` statements and confirm `Loading Quests:` completes. Then, with a `Script` requirement row whose `script_path` is blank, confirm startup aborts with the row-naming message, and that `/reloadsql` on a running server reports `Failed reloading sql: Quest requirement <id> (quest <id>) has type Script but no script_path`. Finally confirm `/reloadscripts` reloads a quest script without a restart.

---

## Design alignment check

Walked against `docs/plans/2026-08-08-quest-scripts-design.md`:

- Interface members and `BaseQuestScript` defaults match the design's table exactly. ✅
- `CanComplete` returns the message; `null`/empty allows completion. ✅
- Six types promoted to public; `QuestWindow` stays internal. ✅ (with the one addition the design did not anticipate: `InternalsVisibleTo` in Task 0, needed because tests must reach the internal `QuestWindow`)
- `RequirementType.Script` = 7, `RewardType.Script` = 21, appended. ✅
- Fail-fast at load with row + quest id in the message. ✅
- `script_path` on a non-`Script` row is a silent no-op — no validation added. ✅
- `keep_requirement` governs `OnTakeRequirement`. ✅
- Scripted progress lines last via existing `OrderBy(r => r.Type)`. ✅
- `GiveRewards` `Script` arm silent; script owns its messaging. ✅
- No try/catch around hooks; no truncation of script text. ✅
- Example script deserializes `ScriptParams` per call. ✅
- Deferred items stay deferred: no counter progress, no script access to quest history, no fix for partial reload. ✅

## Deviations from the design worth noting at review

1. **`Goose.csproj` gains `InternalsVisibleTo("Goose.Tests")`** (Task 0). The design said `QuestWindow` stays internal — it does; this only lets the test assembly see it. Verified not to break the build.
2. **`FromReader` takes `(reader, world, quest)`**, not just `(reader, world)`. The `quest` argument is what puts the quest id in the error message the GM sees, and for requirements it absorbs the `req.Quest = quest;` line at `QuestHandler.cs:46`.
3. **`GetScriptCannotCompleteMessage` is `internal`, not private**, so the gate is testable without synthesizing packet-level `Clicked` input.
4. **`Script`-only test quests.** `CompleteQuest` cannot be exercised in-process with a `Gold` or stat row (NRE via `RemoveGold` → `MaxHP`, harness fact 6). Tests therefore use quests whose rows are all `Script`. This is a test-harness constraint, not a product limitation, and it does not reduce coverage of the arms this feature adds.
