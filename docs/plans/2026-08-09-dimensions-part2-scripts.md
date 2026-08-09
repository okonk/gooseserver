# Dimensions Part 2 — Scripts Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Clone the world into 6 scaled dimensions and gate access to them, entirely from three `.csx` scripts using the extension points Part 1 added.

**Architecture:** `Scripts/Global/Dimensions.csx` runs at `OnLoaded` — after maps, templates and spawns are all loaded — and generates dimension copies of NPC templates, maps and spawns at `id + 100000·dim`. `Scripts/Map/DimensionMap.csx` attaches to every clone and enforces entry gating plus the login and bind clamps. `Scripts/Quest/DimensionUnlock.csx` is the quest reward that grants a dimension. Unlock progress lives in `Player.Properties`.

**Task order:** 0, 1, 2, 2b, 3, 4, 5, 6, 7, 7b, 8. Tasks 2b (ally rewiring) and 7b (keeping the disabled path tested) were added after review; Task 3 also creates a stub `DimensionMap.csx` that Task 5 fills in, because Task 3's cloning resolves that path.

**Tech Stack:** C# scripting (Roslyn `.csx`), xUnit.

**Design doc:** `docs/plans/2026-08-09-dimensions-design.md`
**Depends on:** `docs/plans/2026-08-09-dimensions-part1-server.md` — every task below uses at least one thing it added. Do not start until Part 1 is merged and its tests pass.

---

## APIs verified

| Fact | Location |
|---|---|
| Global scripts load **last**, after maps/templates/spawns | `Goose/GameWorld.cs:270–322` |
| `LoadGlobalScripts` enumerates `Scripts/Global/*.csx` and calls `OnLoaded` | `Goose/GameWorld.cs:652–660` |
| `IGlobalScript.OnLoaded(GameWorld)` | `Goose/Scripting/IGlobalScript.cs:11` |
| `MapHandler.Maps` is a public `Dictionary<int, Map>` | `Goose/MapHandler.cs:30` |
| `MapHandler.LoadMaps` builds `Map` + schedules `ClearMapItemsEvent` per map | `Goose/MapHandler.cs:36–84` |
| `Map` public fields: `ID`, `Name`, `FileName`, `CanPVP`, `Min/MaxExperience`, `tiles`, `characters`, `Script` | `Goose/Map.cs:24–64` |
| `Map.AddPlayer` fires `OnPlayerEntered` | `Goose/Map.cs:131–143` |
| `Map.PlayerCanJoin` consults `CanPlayerJoin` (Part 1) | `Goose/Map.cs:548` |
| `WarpTile.WarpMap` / `WarpX` / `WarpY` are public | `Goose/WarpTile.cs:10–12` |
| `BlockedTile` is an empty marker class | `Goose/BlockedTile.cs:8` |
| `NPC.LoadFromTemplate(world, map_id, map_x, map_y, template, shouldRespawn)` | `Goose/NPC.cs:585` |
| `LoadFromTemplate` does **not** register with `NPCHandler.npcs` — use `SpawnNPC` (Part 1) | `Goose/NPCHandler.cs:280` |
| `NPCHandler.SpawnNPC` / `AddNPC` / `AddTemplate` (Part 1 task 4) | `Goose/NPCHandler.cs` |
| `Map.CloneAs` / `RequiredItems` / `AddRequiredItem` (Part 1 task 5) | `Goose/Map.cs` |
| `NPC.LoadFromTemplate` dereferences `Class.GetLevel(Level)` with no null check | `Goose/NPC.cs:635–636` |
| `class_info` has levels 1–5 for class 1, 1–50 for classes 2–7 | April 2025 snapshot |
| `NPC.Allies` delegates to the template; checks are reference equality | `Goose/NPC.cs:321`, `:559`, `:1000` |
| `Map.requiredItems` is private, enforced by `PlayerCanJoin` | `Goose/Map.cs:64`, `:573` |
| `Player.BoundID` / `BoundMap`; death warps to them | `Goose/Player.cs:226–238`, `:1775` |
| `QuestWindow` invokes `reward.Script.Object.GiveReward` | `Goose/Quests/QuestWindow.cs:481` |
| `IQuestScript` contract: one instance per file, read `ScriptParams` per call | `Goose/Scripting/IQuestScript.cs:9–17` |
| `NPCHandler.GetNPCTemplate(int)` / `GetTemplates()` | `Goose/NPCHandler.cs:220`, `:19` |
| `NPCTemplate.Types.Quest = 12` | `Goose/NPCTemplate.cs:21` |
| `NPC.cs` aliases `this.Quests = template.Quests` | `Goose/NPC.cs:637` |
| `Player.cs` records kill progress by template ID | `Goose/Player.cs:1020` |
| `Player.WarpTo(world, map, x, y)` | `Goose/Player.cs:1175` |
| `EventHandler.RegisterEvent(string, CreateEvent)` | `Goose/EventHandler.cs:251` |
| `delegate Event CreateEvent(Player player, Object data)` | `Goose/EventHandler.cs:53` |
| Script command registration idiom | `Goose/Data/Aspereta/Scripts/Global/Aspereta.csx:237` |
| `ScriptHandler.GetScript<IMapScript>(relativePath)` | `Goose/MapHandler.cs:67` |
| Script test fixture pattern | `Goose.Tests/Fixtures/QuestScriptFixture.cs` |
| Scaling formulas | `~/code/abyssserver/src/Abyss/NPC.java:869–967` |
| Entry-refusal message | `~/code/abyssserver/src/Abyss/Map.java:588` |

**Working directory for every command:** `/home/hayden/code/illutiagooseserver/.worktrees/dimensions`

**Script location:** `Goose/Data/Illutia/Scripts/` — Illutia only. Do not touch `Goose/Data/Aspereta/`.

---

## Task 0: Test fixture for global scripts

**Why:** the tasks below generate thousands of objects inside a `.csx`. Asserting on the *results* (generated templates, cloned maps) is a real integration test; asserting on formula helpers via reflection is not. This fixture makes that possible.

`QuestScriptFixture` (`Goose.Tests/Fixtures/QuestScriptFixture.cs`) is the template to copy — it swaps `GameWorld.Settings`, builds a `GameWorld(null)`, writes a script into a temp data directory, compiles it and restores settings on dispose.

**Files:**
- Create: `Goose.Tests/Fixtures/GlobalScriptFixture.cs`

**The fixture must install the whole shipped script set, not just `Dimensions.csx`.**
`Dimensions.csx` resolves two other scripts at run time — `Scripts/Map/DimensionMap.csx`
when it clones maps (Task 3) and `Scripts/Quest/DimensionUnlock.csx` when it builds the
unlock rewards (Task 7). `ScriptHandler.GetScript` compiles from
`DataPathAbsolute + "/" + path` (`ScriptHandler.cs:21`), so a script missing from the temp
data directory blows up inside `OnLoaded` — every map-cloning test would fail on a
`FileNotFoundException` long before reaching its assertions. Create all three script
directories and copy every dimension script.

**Step 1: Write the fixture**

```csharp
using Goose.Scripting;

namespace Goose.Tests.Fixtures;

public sealed class GlobalScriptFixture : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;

    /// <summary>Every dimension script, by the relative path the server resolves it at.
    /// Copied to output by Goose.Tests.csproj (see Task 1). Add to BOTH lists together -
    /// a script missing here fails inside OnLoaded, not at compile time.</summary>
    private static readonly (string Source, string Relative)[] ShippedScripts =
    {
        ("Dimensions.csx",      "Scripts/Global/Dimensions.csx"),
        ("DimensionMap.csx",    "Scripts/Map/DimensionMap.csx"),
        ("DimensionUnlock.csx", "Scripts/Quest/DimensionUnlock.csx"),
    };

    public string DataDirectory { get; }
    public GameWorld World { get; }

    public GlobalScriptFixture()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "global-script-" + Guid.NewGuid().ToString("N"));
        foreach (var dir in new[] { "Global", "Map", "Quest" })
            Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", dir));

        GameWorld.Settings = new GooseSettings
        {
            DataPath = DataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        World = new GameWorld(null);
    }

    /// <summary>Installs every shipped dimension script into the temp data dir. Call this
    /// before compiling anything - Dimensions.csx loads the map and quest scripts while it
    /// runs, so a partial install fails at OnLoaded rather than at compile time.</summary>
    public void InstallShippedScripts()
    {
        foreach (var (source, relative) in ShippedScripts)
        {
            var from = Path.Combine(AppContext.BaseDirectory, "DimensionScripts", source);
            if (!File.Exists(from))
                throw new FileNotFoundException(
                    $"{source} is not in the test output. Add its <None Include> to Goose.Tests.csproj.", from);

            File.Copy(from, Path.Combine(DataDirectory, relative), overwrite: true);
        }
    }

    /// <summary>Compiles the real shipped Dimensions.csx, so tests exercise what ships
    /// rather than a paraphrase of it.</summary>
    public Script<IGlobalScript> CompileShipped(string fileName = "Dimensions.csx")
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/" + fileName);
    }

    /// <summary>As CompileShipped, for the map script - Task 5's tests drive it directly.</summary>
    public Script<IMapScript> CompileShippedMapScript(string fileName = "DimensionMap.csx")
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IMapScript>("Scripts/Map/" + fileName);
    }

    /// <summary>Compiles an arbitrary script body, for the one test that needs a variant of
    /// the shipped script (Task 9's disabled-mode test).</summary>
    public Script<IGlobalScript> CompileSource(string body, string fileName)
    {
        InstallShippedScripts();
        var relativePath = "Scripts/Global/" + fileName;
        File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
        return World.ScriptHandler.GetScript<IGlobalScript>(relativePath);
    }

    /// <summary>A base map with hand-built tile arrays. Real maps get theirs from
    /// Map.LoadData reading a .map file (Map.cs:466); clones never call it, so a
    /// synthetic base is enough to exercise the clone path.</summary>
    public Map AddBaseMap(int id, string name, int width = 10, int height = 10)
    {
        var map = new Map
        {
            ID = id, Name = name, FileName = "Map" + id + ".map",
            Width = width, Height = height,
            tiles = new ITile[(width + 1) * (height + 1)],
            characters = new ICharacter[(width + 1) * (height + 1)],
        };
        World.MapHandler.Maps[id] = map;
        return map;
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        if (Directory.Exists(DataDirectory)) Directory.Delete(DataDirectory, recursive: true);
    }
}
```

`Map.Width` and `Map.Height` are settable auto-properties (`Map.cs:35,40`) — the map loaders assign them directly (`Map.cs:393–396`) — so the object initialiser above works.

**Step 2: Commit**

```bash
git add Goose.Tests/Fixtures/GlobalScriptFixture.cs
git commit -m "test: add fixture for compiling and running global scripts"
```

---

## Task 1: Dimensions.csx skeleton and configuration

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionsScriptTests.cs` (create)

**Step 1: Write the failing test**

```csharp
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionsScriptTests
{
    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Disabled_by_configuration_changes_nothing()
    {
        // Dimensions.Enabled is false in the shipped script until the feature is switched on.
        // Task 9 replaces this with a variant-compiled test once the shipped flag flips.
        using var fixture = Run(f => f.AddBaseMap(1, "Town"));

        Assert.Single(fixture.World.MapHandler.Maps);
        Assert.Null(fixture.World.MapHandler.GetMap(100001));
    }
}
```

**Resolving the script path.** The test runner's working directory is the test binary's output folder, not the repo root, so a repo-relative path will not resolve. Copy the scripts to output instead — `Goose.Tests.csproj:18` already uses this pattern for `Fixtures/**`. Add to `Goose.Tests/Goose.Tests.csproj`:

```xml
<ItemGroup>
  <None Include="../Goose/Data/Illutia/Scripts/Global/Dimensions.csx"
        Link="DimensionScripts/Dimensions.csx" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

`GlobalScriptFixture` reads from `AppContext.BaseDirectory/DimensionScripts/`, so nothing
else in the tests needs a path.

Add the `DimensionMap.csx` entry in Task 3 (the first task whose script *loads* it, not
Task 5 where it is first written — see below) and the `DimensionUnlock.csx` entry in Task 7,
when those files first exist. An `Include` pointing at a missing file copies nothing, and
the fixture's explicit `FileNotFoundException` is there to make that failure legible.

**Ordering constraint the original plan had wrong.** Task 3 clones maps and attaches
`Scripts/Map/DimensionMap.csx` to every clone, which means `GetScript<IMapScript>` resolves
that path — and therefore the file must exist — *before* Task 5 writes it. Either:

- create `DimensionMap.csx` as a bare `public class DimensionMap : BaseMapScript { }` in
  Task 3 and fill it in at Task 5 (simplest), or
- move Task 5 before Task 3.

Take the first. Add its `<None Include>` at the same time.

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — the script file does not exist, so `File.Copy` throws `FileNotFoundException`.

**Step 3: Write minimal implementation**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Events;
using Goose.Quests;
using Goose.Scripting;

public class Dimensions : BaseGlobalScript
{
    // ---- Configuration -------------------------------------------------
    // Ships disabled. Flip to true once the world is verified to clone cleanly.
    public const bool Enabled = false;

    /// <summary>Dimensions above 0. Abyss shipped 6.</summary>
    public const int DimensionCount = 6;

    /// <summary>Dimension n's copy of anything lives at baseId + Offset*n.
    /// Must exceed every base id: Illutia map ids reach 10044, so 10000 is too small.</summary>
    public const int Offset = 100000;

    /// <summary>Map /dimension n warps to.</summary>
    public const int StartMapId = 1;

    /// <summary>NPC template gating each dimension.</summary>
    public const int BossTemplateId = 162;

    // ---- Warden ---------------------------------------------------------
    // The quest giver. It does not exist in sheet data, so everything about it is
    // configured here. One template per dimension at WardenTemplateId + Offset*dim.

    /// <summary>Base id for the generated warden templates. Must not collide with a
    /// sheet-authored npc_id - the script checks and refuses to overwrite.</summary>
    public const int WardenTemplateId = 800000;

    public const string WardenName = "Warden of the Void";
    public const string WardenTitle = "";
    public const string WardenSurname = "";

    /// <summary>Any class works as long as it has a row for WardenLevel. class_info only
    /// carries levels 1-5 for class 1 (Commoner); classes 2-7 carry 1-50. Level 50 on
    /// class 1 makes Class.GetLevel return null and NPC.LoadFromTemplate throws at
    /// NPC.cs:636. The script validates this at startup rather than at spawn time.</summary>
    public const int WardenClassId = 3;      // Warrior
    public const int WardenLevel = 50;

    /// <summary>Appearance. These are the same fields npc_templates carries, so anything
    /// legal for a sheet-authored NPC is legal here.</summary>
    public const int WardenBodyID = 1;
    public const int WardenBodyState = 0;
    public const int WardenBodyR = 40;
    public const int WardenBodyG = 0;
    public const int WardenBodyB = 60;
    public const int WardenBodyA = 200;
    public const int WardenFaceID = 1;
    public const int WardenHairID = 1;
    public const int WardenHairR = 20;
    public const int WardenHairG = 0;
    public const int WardenHairB = 40;
    public const int WardenHairA = 200;

    /// <summary>MKC-string fragment, exactly as npc_templates.equipped_items
    /// (NPCHandler.cs:65, rendered at Packets.cs:161). Empty for no visible equipment.</summary>
    public const string WardenEquippedItems = "";

    /// <summary>Quest-giver placement, per dimension, on that dimension's start map.</summary>
    public const int WardenMapId = StartMapId;
    public const int WardenX = 50;
    public const int WardenY = 50;

    /// <summary>Quest ids are deterministic: QuestProgress persists keyed on
    /// requirement.Id (Player.cs:1020 / QuestWindow.cs:268), so a counter-assigned id
    /// would orphan in-flight kill progress on restart.</summary>
    public const int QuestIdBase = 900000;

    public const string MaxDimensionProperty = "dimension.max";

    public override void OnLoaded(GameWorld world)
    {
        if (!Enabled) return;
    }
}
```

**On the warden's stats.** Everything except appearance is fixed rather than configurable,
because each has one right answer:

| Field | Value | Why |
|---|---|---|
| `NPCType` | `Types.Quest` (12) | It offers quests |
| `CanBeKilled` | `false` | Every dimension > 0 forces `CanPVP = true`; a killable quest giver in an open-PVP zone is a griefing target. `NPCHandler.cs:63` maps this from `invincible` |
| `CanMove` | `false` | Players need to find it where it was put |
| `Level` | `WardenLevel` (50) | Cosmetic for an invincible NPC, but it must have a class level row |
| `ClassID` | `WardenClassId` (3) | Must resolve, and must have a row at `WardenLevel` |
| `BaseStats.HP` | small non-zero, e.g. 1000 | It cannot die; the value only affects what the client renders |
| `WeaponDamage`, `AggroRange` | 0 | It is not a combatant |
| `RespawnTime` | irrelevant, spawn with `shouldRespawn: false` | It never dies |

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS (1 test).

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionsScriptTests.cs
git commit -m "feat: add Dimensions.csx skeleton with configuration"
```

---

## Task 2: Clone and scale NPC templates

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionsScriptTests.cs`

Formulas are transcribed from `~/code/abyssserver/src/Abyss/NPC.java`. Abyss applies them per spawn (`NPC.java:860`); baking them into the cloned template is equivalent and makes each dimension's boss a distinct template id, which is what lets the stock `Kill` requirement tell dimensions apart.

**Step 1: Write the failing tests**

```csharp
[Fact]
public void Clones_each_template_once_per_dimension_with_scaled_stats()
{
    using var fixture = Run(f =>
    {
        var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40,
                                  WeaponDamage = 365, RespawnTime = 50, Experience = 750,
                                  AttackSpeed = 1.5m, MoveSpeed = 1.5m, AttackRange = 1,
                                  CanBeRooted = true, CanBeStunned = true, CanBeSlowed = false };
        t.BaseStats.HP = 3704;
        f.World.NPCHandler.AddTemplate(t);
    });

    var dim3 = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 3);
    Assert.NotNull(dim3);
    Assert.Equal("Shadow Dog (3)", dim3.Name);

    // NPC.java:927 - (base + 100000*2^dim) * 4.7^dim
    Assert.Equal((long)((3704 + 100000 * Math.Pow(2, 3)) * Math.Pow(4.7, 3)), dim3.BaseStats.HP);
    // NPC.java:936 - base*4^dim + 100000*max(0, 4^dim-3)
    Assert.Equal((long)(365 * Math.Pow(4, 3) + 100000 * Math.Max(0, Math.Pow(4, 3) - 3)), dim3.WeaponDamage);
    // NPC.java:954 - (exp + level*100) * 3^min(4,dim)
    Assert.Equal((long)((750 + 40 * 100) * Math.Pow(3, 3)), dim3.Experience);
    // NPC.java:899 - every dimension mob is level 50
    Assert.Equal(50, dim3.Level);
    // NPC.java:881 - immune to root and stun, but slowable
    Assert.False(dim3.CanBeRooted);
    Assert.False(dim3.CanBeStunned);
    Assert.True(dim3.CanBeSlowed);
    // NPC.java:869 - attack range grows with dimension
    Assert.Equal(1 + 3, dim3.AttackRange);
}

[Fact]
public void Leaves_the_base_template_untouched()
{
    using var fixture = Run(f =>
    {
        var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        t.BaseStats.HP = 3704;
        f.World.NPCHandler.AddTemplate(t);
    });

    var basic = fixture.World.NPCHandler.GetNPCTemplate(162);
    Assert.Equal("Shadow Dog", basic.Name);
    Assert.Equal(3704, basic.BaseStats.HP);
    Assert.Equal(40, basic.Level);
}

[Fact]
public void Applies_the_dimension_five_multipliers()
{
    using var fixture = Run(f =>
    {
        var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40, WeaponDamage = 365 };
        t.BaseStats.HP = 3704;   // <= 35,000,000, so HP doubles at dim >= 5
        f.World.NPCHandler.AddTemplate(t);
    });

    var dim5 = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 5);

    Assert.Equal((long)((3704 + 100000 * Math.Pow(2, 5)) * Math.Pow(4.7, 5)) * 2, dim5.BaseStats.HP);
    // base < 10,000,000 so damage is multiplied by 20
    Assert.Equal((long)(365 * Math.Pow(4, 5) + 100000 * Math.Max(0, Math.Pow(4, 5) - 3)) * 20, dim5.WeaponDamage);
    // This value exceeds int.MaxValue - it only fits because Part 1 widened the fields.
    Assert.True(dim5.BaseStats.HP > int.MaxValue);
}
```

Flip `Enabled` to `true` in the script as part of this task. Task 1's disabled test breaks
when you do — **Task 7b replaces it with one that compiles a flag-flipped copy of the
shipped source**, so the escape hatch stays covered. Do not just delete it. If you would
rather not carry a red test between here and Task 7b, do Task 7b's rewrite now; it does not
depend on anything Tasks 3–7 add.

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — `GetNPCTemplate(100162)` returns null.

**Step 3: Write minimal implementation**

Add to `Dimensions.csx`:

```csharp
public override void OnLoaded(GameWorld world)
{
    if (!Enabled) return;

    CloneTemplates(world);
}

private void CloneTemplates(GameWorld world)
{
    // Snapshot first: AddTemplate mutates the dictionary GetTemplates() enumerates.
    var baseTemplates = world.NPCHandler.GetTemplates().ToList();

    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var template in baseTemplates)
        {
            int id = template.NPCTemplateID + Offset * dim;

            // AddTemplate overwrites silently (NPCHandler.cs, Part 1 task 4). A base id
            // large enough to land on another dimension's slot would quietly replace a
            // generated template and produce a world that is wrong in a way nothing
            // reports. Refuse loudly instead.
            if (world.NPCHandler.GetNPCTemplate(id) != null)
                throw new Exception($"Dimension template id {id} (base {template.NPCTemplateID}, dim {dim}) "
                                    + "already exists. Offset is too small for this data set.");

            world.NPCHandler.AddTemplate(ScaleTemplate(template, dim));
        }
    }
}

private NPCTemplate ScaleTemplate(NPCTemplate basic, int dim)
{
    var clone = new NPCTemplate(basic)
    {
        NPCTemplateID = basic.NPCTemplateID + Offset * dim,
        Name = basic.Name + " (" + dim + ")",
        Level = 50,                                   // NPC.java:899
        AttackRange = basic.AttackRange + dim,        // NPC.java:869
        CanBeRooted = false,                          // NPC.java:881
        CanBeStunned = false,
        CanBeSlowed = true,
        AttackSpeed = ScaleAttackSpeed(basic.AttackSpeed, dim),
        MoveSpeed = Math.Max(basic.MoveSpeed - 0.15m * dim, 0.15m),   // NPC.java:907
        WeaponDamage = ScaleDamage(basic.WeaponDamage, dim),
        Experience = ScaleExperience(basic.Experience, basic.Level, dim),
        RespawnTime = ScaleRespawn(basic.RespawnTime, dim),
    };

    clone.BaseStats.HP = ScaleHP(basic.BaseStats.HP, dim);
    clone.BaseStats.HPPercentRegen = basic.BaseStats.HPPercentRegen + 0.004m * (dim + 1);  // NPC.java:879

    Recolour(clone, dim);   // NPC.java:1019
    return clone;
}

/// <summary>NPC.java:927</summary>
private long ScaleHP(long basehp, int dim)
{
    long hp = (long)((basehp + 100000 * Math.Pow(2, dim)) * Math.Pow(4.7, dim));
    if (dim >= 5 && basehp <= 35000000) hp *= 2;
    return hp;
}

/// <summary>NPC.java:936</summary>
private long ScaleDamage(long baseDamage, int dim)
{
    long damage = (long)(baseDamage * Math.Pow(4, dim) + 100000 * Math.Max(0, Math.Pow(4, dim) - 3));
    if (dim >= 5 && baseDamage < 10000000) damage *= 20;
    return damage;
}

/// <summary>NPC.java:945. The dim>=5 branch raises the value back to 0.7 - faithful, if odd.</summary>
private decimal ScaleAttackSpeed(decimal attackSpeed, int dim)
{
    attackSpeed = Math.Max(attackSpeed - 0.175m * dim, 0.2m);
    if (dim >= 5 && attackSpeed > 0.5m) attackSpeed = 0.7m;
    return attackSpeed;
}

/// <summary>NPC.java:954</summary>
private long ScaleExperience(long experience, int level, int dim)
{
    double multi = Math.Pow(3, Math.Min(4, dim));
    if (dim >= 5) multi *= Math.Pow(2, dim - 4);
    return (long)((experience + level * 100) * multi);
}

/// <summary>NPC.java:963. Respawn stops shortening past dimension 4.</summary>
private int ScaleRespawn(int respawnTime, int dim)
{
    dim = Math.Min(4, dim);
    return Math.Min((int)(respawnTime * Math.Pow(0.85, dim)), 3600 / (1 + dim));
}

/// <summary>NPC.java:1019 - darker and more opaque per dimension.</summary>
private void Recolour(NPCTemplate t, int dim)
{
    t.HairR = Math.Max(t.HairR - dim * 30, 0);
    t.HairG = Math.Max(t.HairG - dim * 30, 0);
    t.HairB = Math.Max(t.HairB - dim * 30, 0);
    t.HairA = Math.Min(t.HairA + dim * 30, 200);
    t.BodyR = Math.Max(t.BodyR - dim * 30, 0);
    t.BodyG = Math.Max(t.BodyG - dim * 30, 0);
    t.BodyB = Math.Max(t.BodyB - dim * 30, 0);
    t.BodyA = Math.Min(t.BodyA + dim * 30, 200);
}
```

`AttackSpeed`/`MoveSpeed` are `decimal` in goose (`NPCTemplate.cs:153,157`) but `double` in abyss — hence the `m` suffixes. `Experience` is `long` (`NPCTemplate.cs:113`) and `WeaponDamage` is `long` after Part 1.

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionsScriptTests.cs
git commit -m "feat: clone and scale NPC templates per dimension"
```

---

## Task 2b: Rewire allies within each dimension

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionsScriptTests.cs`

**Why this is not part of Task 2:** the `NPCTemplate` copy constructor carries `Allies`
across as a new list holding the **base** templates. Ally checks are reference comparisons
against the template — `NPC.Allies` is `NPCTemplate.Allies` (`NPC.cs:321`) and both call
sites do `this.Allies.Contains(npc.NPCTemplate)` (`NPC.cs:559`, `NPC.cs:1000`) — so a
dimension-3 mob whose ally list points at dimension-0 templates never recognises the
dimension-3 mob standing next to it. Mobs silently stop assisting each other in every
dimension.

It has to be a second pass because Task 2 creates templates in dictionary order: when
template A is cloned, ally B's clone may not exist yet.

**Step 1: Write the failing test**

```csharp
[Fact]
public void Allies_point_at_the_same_dimensions_templates()
{
    using var fixture = Run(f =>
    {
        var dog = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        var wolf = new NPCTemplate { NPCTemplateID = 163, Name = "Shadow Wolf", Level = 40 };
        dog.Allies = new List<NPCTemplate> { wolf };
        wolf.Allies = new List<NPCTemplate> { dog };
        f.World.NPCHandler.AddTemplate(dog);
        f.World.NPCHandler.AddTemplate(wolf);
    });

    var dim3Dog = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 3);
    var dim3Wolf = fixture.World.NPCHandler.GetNPCTemplate(163 + 100000 * 3);

    // Reference identity is what NPC.cs:559 compares, so Same, not Equal.
    Assert.Same(dim3Wolf, Assert.Single(dim3Dog.Allies));
    Assert.Same(dim3Dog, Assert.Single(dim3Wolf.Allies));

    // The base templates keep their own allies.
    Assert.Same(fixture.World.NPCHandler.GetNPCTemplate(163),
                Assert.Single(fixture.World.NPCHandler.GetNPCTemplate(162).Allies));
}

[Fact]
public void An_ally_with_no_dimension_clone_is_dropped_rather_than_left_pointing_at_dimension_zero()
{
    using var fixture = Run(f =>
    {
        var dog = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        // An ally id that resolved at load time but is not in the handler now.
        dog.Allies = new List<NPCTemplate> { new NPCTemplate { NPCTemplateID = 999 } };
        f.World.NPCHandler.AddTemplate(dog);
    });

    Assert.Empty(fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 3).Allies);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — the dimension-3 dog's ally is the dimension-0 wolf.

**Step 3: Write minimal implementation**

Add `RewireAllies(world);` to `OnLoaded` immediately after `CloneTemplates(world);`:

```csharp
/// <summary>Second pass over the templates Task 2 created: repoint every ally at the
/// same dimension's clone. Ally checks compare template references (NPC.cs:559, :1000),
/// so a dimension mob allied to a dimension-0 template recognises nothing.
///
/// Separate from CloneTemplates because clone order is dictionary order - an ally's clone
/// may not exist yet at the moment the template referencing it is built.</summary>
private void RewireAllies(GameWorld world)
{
    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var basic in world.NPCHandler.GetTemplates()
                                   .Where(t => t.NPCTemplateID < Offset).ToList())
        {
            var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
            if (clone == null || basic.Allies == null) continue;

            var allies = new List<NPCTemplate>();
            foreach (var ally in basic.Allies)
            {
                // An ally with no clone is dropped, not left pointing across dimensions.
                var dimAlly = world.NPCHandler.GetNPCTemplate(ally.NPCTemplateID + Offset * dim);
                if (dimAlly != null) allies.Add(dimAlly);
            }

            clone.Allies = allies;
            // Keep the string form consistent - nothing re-parses it after load, but a
            // divergent AlliesString is a trap for anyone debugging from a dump.
            clone.AlliesString = string.Join(" ", allies.Select(a => a.NPCTemplateID));
        }
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: rewire dimension NPC allies to same-dimension templates"
```

---

## Task 3: Clone maps and rewire warps

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Create: `Goose/Data/Illutia/Scripts/Map/DimensionMap.csx` (stub — Task 5 fills it in)
- Modify: `Goose.Tests/Goose.Tests.csproj` (copy `DimensionMap.csx` to output)
- Test: `Goose.Tests/DimensionsScriptTests.cs`

**The stub comes first.** `CloneMaps` resolves `Scripts/Map/DimensionMap.csx` through
`ScriptHandler.GetScript`, which compiles the file, so it has to exist before this task's
tests can pass. Create it as:

```csharp
using Goose;
using Goose.Scripting;

public class DimensionMap : BaseMapScript
{
}
```

and add its `<None Include>` to `Goose.Tests.csproj` alongside `Dimensions.csx`.

**Step 1: Write the failing tests**

```csharp
[Fact]
public void Clones_each_map_once_per_dimension()
{
    using var fixture = Run(f => f.AddBaseMap(1, "Town"));

    var dim2 = fixture.World.MapHandler.GetMap(1 + 100000 * 2);

    Assert.NotNull(dim2);
    Assert.Equal("Town (2)", dim2.Name);
    Assert.True(dim2.CanPVP);                     // PVP is forced on in every dimension
    Assert.NotSame(fixture.World.MapHandler.GetMap(1).characters, dim2.characters);
    Assert.NotSame(fixture.World.MapHandler.GetMap(1).tiles, dim2.tiles);
}

[Fact]
public void Warps_point_at_the_same_dimension()
{
    using var fixture = Run(f =>
    {
        var town = f.AddBaseMap(1, "Town");
        var cave = f.AddBaseMap(2, "Cave");
        town.SetTile(3, 3, new WarpTile { WarpMap = cave, WarpX = 7, WarpY = 8 });
    });

    var dim2Town = fixture.World.MapHandler.GetMap(1 + 100000 * 2);
    var warp = (WarpTile)dim2Town.GetTile(3, 3);

    Assert.Equal(2 + 100000 * 2, warp.WarpMap.ID);   // the dimension-2 Cave, not the base one
    Assert.Equal(7, warp.WarpX);
    Assert.Equal(8, warp.WarpY);

    // The base map's warp must be untouched.
    var baseWarp = (WarpTile)fixture.World.MapHandler.GetMap(1).GetTile(3, 3);
    Assert.Equal(2, baseWarp.WarpMap.ID);
}

[Fact]
public void Blocked_tiles_are_shared_not_duplicated()
{
    using var fixture = Run(f =>
    {
        var town = f.AddBaseMap(1, "Town");
        town.SetTile(4, 4, new BlockedTile());
    });

    // BlockedTile is an empty marker (BlockedTile.cs:8), so sharing the reference is safe.
    Assert.Same(fixture.World.MapHandler.GetMap(1).GetTile(4, 4),
                fixture.World.MapHandler.GetMap(100001).GetTile(4, 4));
}

/// <summary>The clone must not become a way around a key-gated map. requiredItems is
/// private (Map.cs:64) and enforced by PlayerCanJoin (Map.cs:573), which is why Part 1
/// added Map.CloneAs rather than leaving the script to rebuild public fields.</summary>
[Fact]
public void Clones_keep_the_base_maps_required_items_and_mute_state()
{
    using var fixture = Run(f =>
    {
        var vault = f.AddBaseMap(1, "Vault");
        vault.AddRequiredItem(1234);
        vault.Muted = true;
    });

    var dim2 = fixture.World.MapHandler.GetMap(1 + 100000 * 2);

    Assert.Equal(new[] { 1234 }, dim2.RequiredItems);
    Assert.True(dim2.Muted);
}

[Fact]
public void Map_ids_do_not_collide_with_existing_maps()
{
    // A base map already sitting on a generated id must be a loud failure, not a silent
    // overwrite - MapHandler.Maps is a plain dictionary.
    var fixture = new GlobalScriptFixture();
    fixture.AddBaseMap(1, "Town");
    fixture.AddBaseMap(100001, "Impostor");

    using (fixture)
    {
        var ex = Assert.Throws<Exception>(
            () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
        Assert.Contains("100001", ex.Message);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — `GetMap(200001)` returns null.

**Step 3: Write minimal implementation**

Add to `OnLoaded` after `CloneTemplates(world)`:

```csharp
CloneMaps(world);
RewireWarps(world);
```

```csharp
private void CloneMaps(GameWorld world)
{
    var baseMaps = world.MapHandler.Maps.Values.ToList();
    var mapScript = world.ScriptHandler.GetScript<IMapScript>("Scripts/Map/DimensionMap.csx");

    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var basic in baseMaps)
        {
            int id = basic.ID + Offset * dim;

            // MapHandler.Maps is a plain dictionary - a collision would silently replace a
            // real map and strand whoever is standing on it.
            if (world.MapHandler.GetMap(id) != null)
                throw new Exception($"Dimension map id {id} (base {basic.ID}, dim {dim}) already exists. "
                                    + "Offset is too small for this data set.");

            // Map.CloneAs (Part 1 task 5) carries everything across, including the private
            // requiredItems list and Muted. Rebuilding public fields here instead would
            // drop item-gated entry on every dimension copy of a key-gated map.
            var clone = basic.CloneAs(id, basic.Name + " (" + dim + ")");

            clone.CanPVP = true;                      // forced on in every dimension
            // Entry gates scale by (dim*5)^2
            clone.MinExperience = basic.MinExperience * (dim * 5) * (dim * 5);
            clone.MaxExperience = basic.MaxExperience * (dim * 5) * (dim * 5);
            clone.Script = mapScript;                 // see follow-up 1: replaces, not composes
            clone.ScriptParams = dim.ToString();      // DimensionMap reads its dimension from here

            world.MapHandler.Maps[clone.ID] = clone;

            // MapHandler.LoadMaps:78 schedules one of these per map; clones need it too
            // or dropped items never sweep off the ground.
            Event sweep = new ClearMapItemsEvent();
            sweep.Ticks += world.TimerFrequency * GameWorld.Settings.ItemGroundSweepTime;
            sweep.Data = clone;
            world.EventHandler.AddEvent(sweep);
        }
    }
}

private void RewireWarps(GameWorld world)
{
    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var basic in world.MapHandler.Maps.Values.Where(m => m.ID < Offset).ToList())
        {
            var clone = world.MapHandler.GetMap(basic.ID + Offset * dim);

            for (int i = 0; i < clone.tiles.Length; i++)
            {
                var warp = clone.tiles[i] as WarpTile;
                if (warp == null) continue;

                var target = warp.WarpMap == null
                    ? null
                    : world.MapHandler.GetMap(warp.WarpMap.ID + Offset * dim);

                // A warp whose target has no clone stays pointed at the base map - it is
                // an exit from the dimension rather than a broken link.
                clone.tiles[i] = new WarpTile
                {
                    WarpMap = target ?? warp.WarpMap,
                    WarpX = warp.WarpX,
                    WarpY = warp.WarpY,
                };
            }
        }
    }
}
```

`ClearMapItemsEvent` is already `public` (`Goose/Events/ClearMapItemsEvent.cs:13`), as is the abstract `Event` base (`Goose/Event.cs:10`), so the script can construct it directly.

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: clone maps per dimension and rewire warps in-dimension"
```

---

## Task 4: Clone NPC spawns

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionsScriptTests.cs`

**Step 1: Write the failing test**

```csharp
[Fact]
public void Spawns_the_dimension_template_on_the_dimension_map()
{
    using var fixture = Run(f =>
    {
        f.AddBaseMap(1, "Town", width: 100, height: 100);
        var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        t.BaseStats.HP = 3704;
        f.World.NPCHandler.AddTemplate(t);

        f.World.NPCHandler.SpawnNPC(f.World, 1, 50, 50, t, shouldRespawn: true);
    });

    var dim1Map = fixture.World.MapHandler.GetMap(100001);
    var spawned = dim1Map.NPCs.Single();

    Assert.Equal(162 + 100000, spawned.NPCTemplate.NPCTemplateID);
    Assert.Equal(50, spawned.SpawnX);
    Assert.Equal(50, spawned.SpawnY);
}

/// <summary>The done criteria are stated in NPCCount ("~82,000 NPCs"), so the generated
/// spawns must actually be registered with the handler. Only SpawnNPC (Part 1 task 4) does
/// that - NPC.LoadFromTemplate adds to the map and the login-id lookup and nothing else.</summary>
[Fact]
public void Generated_spawns_are_registered_with_the_handler()
{
    using var fixture = Run(f =>
    {
        f.AddBaseMap(1, "Town", width: 100, height: 100);
        var t = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        t.BaseStats.HP = 3704;
        f.World.NPCHandler.AddTemplate(t);
        f.World.NPCHandler.SpawnNPC(f.World, 1, 50, 50, t, shouldRespawn: true);
    });

    // 1 base spawn + one per dimension (6). The script's types are not visible to the test
    // assembly, so the count is a literal. The warden chain (Task 7) adds more; this
    // assertion becomes a >= once that lands - keep it exact until then.
    Assert.Equal(7, fixture.World.NPCHandler.NPCCount);
}
```

`Map.NPCs` is a public accessor over the map's npc list (`Goose/Map.cs:618`), so the assertions above work as written.

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — the dimension map has no NPCs.

**Step 3: Write minimal implementation**

Add `CloneSpawns(world);` to `OnLoaded` after `RewireWarps(world);`:

```csharp
private void CloneSpawns(GameWorld world)
{
    // Snapshot the base spawns before spawning anything - each map's npc list grows as we
    // go, and Map.NPCs hands back the live list (Map.cs:618).
    var baseSpawns = world.MapHandler.Maps.Values
        .Where(m => m.ID < Offset)
        .SelectMany(m => m.NPCs.ToList())
        .ToList();

    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var basic in baseSpawns)
        {
            var template = world.NPCHandler.GetNPCTemplate(basic.NPCTemplate.NPCTemplateID + Offset * dim);
            if (template == null) continue;

            // SpawnNPC, NOT new NPC().LoadFromTemplate(...): the latter adds the NPC to its
            // map and to the login-id lookup but not to NPCHandler.npcs, so it would never
            // appear in NPCCount. See Part 1 task 4.
            //
            // shouldRespawn: true - respawning is self-sustaining on the NPC, matching how
            // LoadNPCs creates the base spawns.
            world.NPCHandler.SpawnNPC(world, basic.Map.ID + Offset * dim,
                                      basic.SpawnX, basic.SpawnY, template, shouldRespawn: true);
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: clone NPC spawns onto dimension maps"
```

---

## Task 5: DimensionMap.csx entry gating

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Map/DimensionMap.csx` (the Task 3 stub)
- Test: `Goose.Tests/DimensionMapScriptTests.cs` (create)

Two gates are needed, because they cover different paths:

- `CanPlayerJoin` (Part 1) covers warps (`MoveEvent.cs:123`) and teleport spells (`SpellEffect.cs:727`).
- `OnPlayerEntered` (`Map.cs:137`) covers **login** — a player whose saved `map_id` is a dimension map is placed directly onto it without passing `PlayerCanJoin`. Abyss needed a separate check for the same reason (`Player.java:1458`).

**And the bind is a third thing.** Relocating the player off the map is not enough:
`BoundID`/`BoundMap` (`Player.cs:671–674`) are what death warps to (`Player.cs:1775`), and
a bind set inside a dimension survives the relocation. A player whose `dimension.max` is
reduced would keep a locked-dimension bind and re-enter it by dying — the design calls for
clamping both (`docs/plans/2026-08-09-dimensions-design.md`, "Login clamp"), so
`OnPlayerEntered` clamps the bound map too.

**Step 1: Write the failing test**

```csharp
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionMapScriptTests
{
    [Fact]
    public void Refuses_players_below_the_required_dimension()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var map = fixture.AddBaseMap(300001, "Town (3)");
        map.ScriptParams = "3";

        var player = new Player(0);
        player.Properties["dimension.max"] = 1;

        var refusal = script.Object.CanPlayerJoin(map, player, fixture.World);

        Assert.Equal("The void has rejected you. You have a maximum dimension of 1.", refusal);
    }

    [Fact]
    public void Allows_players_at_or_above_the_required_dimension()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var map = fixture.AddBaseMap(300001, "Town (3)");
        map.ScriptParams = "3";

        var player = new Player(0);
        player.Properties["dimension.max"] = 3;

        Assert.Null(script.Object.CanPlayerJoin(map, player, fixture.World));
    }

    [Fact]
    public void Players_with_no_progress_default_to_dimension_zero()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var map = fixture.AddBaseMap(100001, "Town (1)");
        map.ScriptParams = "1";

        Assert.NotNull(script.Object.CanPlayerJoin(map, new Player(0), fixture.World));
    }

    // ---- The login and bind clamps ------------------------------------------------

    [Fact]
    public void Entering_a_locked_dimension_map_relocates_the_player_to_dimension_zero()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim3 = fixture.AddBaseMap(300001, "Town (3)", width: 100, height: 100);
        dim3.ScriptParams = "3";

        var player = PlayerOn(dim3, x: 50, y: 50);
        player.Properties["dimension.max"] = 0;

        script.Object.OnPlayerEntered(dim3, player, fixture.World);

        Assert.Equal(1, player.Map.ID);
    }

    /// <summary>The design calls for clamping bound_id as well as map_id. Without this a
    /// player whose progress is reduced keeps a bind inside a locked dimension and returns
    /// there every time they die (Player.cs:1775).</summary>
    [Fact]
    public void A_bind_inside_a_locked_dimension_is_clamped_to_dimension_zero()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var town = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim3 = fixture.AddBaseMap(300001, "Town (3)", width: 100, height: 100);
        dim3.ScriptParams = "3";

        var player = PlayerOn(dim3, x: 50, y: 50);
        player.Properties["dimension.max"] = 0;
        player.BoundID = 300001;
        player.BoundMap = dim3;
        player.BoundX = 40;
        player.BoundY = 40;

        script.Object.OnPlayerEntered(dim3, player, fixture.World);

        Assert.Equal(1, player.BoundID);
        Assert.Same(town, player.BoundMap);
        Assert.Equal(40, player.BoundX);      // coordinates survive; only the map changes
        Assert.Equal(40, player.BoundY);
    }

    [Fact]
    public void A_bind_the_player_still_has_access_to_is_left_alone()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim3 = fixture.AddBaseMap(300001, "Town (3)", width: 100, height: 100);
        dim3.ScriptParams = "3";

        var player = PlayerOn(dim3, x: 50, y: 50);
        player.Properties["dimension.max"] = 3;
        player.BoundID = 300001;
        player.BoundMap = dim3;

        script.Object.OnPlayerEntered(dim3, player, fixture.World);

        Assert.Equal(300001, player.BoundID);
        Assert.Same(dim3, player.BoundMap);
    }

    /// <summary>Binds are clamped even when the map being entered is fine - a player can
    /// walk into dimension 0 carrying a dimension-5 bind.</summary>
    [Fact]
    public void A_locked_bind_is_clamped_even_when_the_current_map_is_allowed()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var town = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim1 = fixture.AddBaseMap(100001, "Town (1)", width: 100, height: 100);
        dim1.ScriptParams = "1";
        var dim5 = fixture.AddBaseMap(500001, "Town (5)", width: 100, height: 100);
        dim5.ScriptParams = "5";

        var player = PlayerOn(dim1, x: 50, y: 50);
        player.Properties["dimension.max"] = 1;
        player.BoundID = 500001;
        player.BoundMap = dim5;

        script.Object.OnPlayerEntered(dim1, player, fixture.World);

        Assert.Equal(1, player.Map.ID);       // the current map was allowed - no relocation
        Assert.Equal(1, player.BoundID);      // but the bind was not
        Assert.Same(town, player.BoundMap);
    }
}
```

`PlayerOn(map, x, y)` builds a `Player` already placed on a map — whatever the minimum is
for `WarpTo` to work in a test world. If `WarpTo` turns out to need more of a live world
than a test can stand up, assert on `player.MapID` instead of `player.Map.ID` and drive the
relocation through the same path the script uses.

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter DimensionMapScriptTests`
Expected: FAIL — the script file does not exist.

**Step 3: Write minimal implementation**

```csharp
using System;
using Goose;
using Goose.Scripting;

public class DimensionMap : BaseMapScript
{
    private const string MaxDimensionProperty = "dimension.max";

    /// <summary>Dimensions.csx sets ScriptParams to the dimension number when it clones the map.</summary>
    private int DimensionOf(Map map)
    {
        int dim;
        return int.TryParse(map.ScriptParams, out dim) ? dim : 0;
    }

    private int MaxDimensionOf(Player player)
    {
        return player.Properties.GetProperty<int>(MaxDimensionProperty, 0);
    }

    /// <summary>Gates warps (MoveEvent.cs:123) and teleport spells (SpellEffect.cs:727).</summary>
    public override string CanPlayerJoin(Map map, Player player, GameWorld world)
    {
        int max = MaxDimensionOf(player);
        if (DimensionOf(map) <= max) return null;

        // Map.java:588
        return "The void has rejected you. You have a maximum dimension of " + max + ".";
    }

    /// <summary>Login places a player straight onto their saved map without consulting
    /// PlayerCanJoin, so the gate is re-checked here and violators are sent to the
    /// dimension-0 copy of wherever they were. The bind is clamped too - see below.</summary>
    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        int max = MaxDimensionOf(player);

        // Order matters: clamp the bind first, so it is corrected even when this map is
        // allowed and the early return below fires.
        ClampBind(player, max, world);

        if (DimensionOf(map) <= max) return;

        var fallback = world.MapHandler.GetMap(map.ID % Offset);
        if (fallback == null) return;

        world.Send(player, "$7The void has rejected you. You have a maximum dimension of "
                           + max + ".");
        player.WarpTo(world, fallback, player.MapX, player.MapY);
    }

    /// <summary>Relocating the player is not enough on its own. BoundID/BoundMap
    /// (Player.cs:671-674) are what death warps to (Player.cs:1775), so a bind set inside a
    /// dimension survives the relocation and lets a player whose progress was reduced walk
    /// straight back in by dying. Clamp it to the dimension-0 map, keeping the coordinates.
    ///
    /// A dimension map's id is baseId + Offset*dim, so the base is id % Offset. If that map
    /// has somehow gone (a re-import between sessions), fall back to the starting map -
    /// leaving a bind pointing at a map that does not exist would strand the player on
    /// death.</summary>
    private void ClampBind(Player player, int max, GameWorld world)
    {
        int boundDim = player.BoundID / Offset;
        if (boundDim <= max) return;

        var baseMap = world.MapHandler.GetMap(player.BoundID % Offset);
        if (baseMap != null)
        {
            // Same map one dimension down to 0, same coordinates.
            player.BoundID = baseMap.ID;
            player.BoundMap = baseMap;
            return;
        }

        // The base map is gone - a re-import between sessions. A bind pointing at a map
        // that does not exist strands the player on death, so send them to the start.
        var start = world.MapHandler.GetMap(GameWorld.Settings.StartingMapID);
        if (start == null) return;

        player.BoundID = start.ID;
        player.BoundMap = start;
        player.BoundX = GameWorld.Settings.StartingMapX;
        player.BoundY = GameWorld.Settings.StartingMapY;
    }
}
```

`boundDim` is `BoundID / Offset` rather than a `ScriptParams` lookup: the bound map may not
be loaded as an object here, and the id encodes the dimension by construction. Same
arithmetic, no dependency on the other map's script being attached.

`dimension.max` is not written by this script — the clamp only reads it. The write happens
in `DimensionUnlock.csx` (Task 7) and persists through `player_properties` (Part 1 task 2).

**Cross-script references do not work.** `ScriptHandler.GetScript` (`Goose/Scripting/ScriptHandler.cs:19`) compiles each `.csx` independently against the Goose assembly, so `DimensionMap.csx` cannot see `Dimensions.Offset`. Declare it locally:

```csharp
/// <summary>Must match Dimensions.csx's Offset. Scripts compile independently,
/// so this cannot be shared.</summary>
private const int Offset = 100000;
```

and add a matching comment in `Dimensions.csx`. (Within a single file this is a non-issue — `DimensionCommandEvent` in Task 6 lives in `Dimensions.csx` and references `Dimensions.DimensionCount` directly.)

**Fallback coordinates.** `player.MapX`/`MapY` are the coordinates the player occupied on the dimension map, which is the same size as its base map — clones copy `Width`/`Height` verbatim in Task 3 — so they are always in bounds on the base map. `Map.PlaceCharacter` (`Map.cs:249`) relocates anyone landing on a blocked tile, so no additional clamping is needed.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter DimensionMapScriptTests`
Expected: PASS (7 tests — 3 gating, 4 clamp).

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add DimensionMap.csx entry gating and login clamp"
```

---

## Task 6: The /dimension command

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionsScriptTests.cs`

**Step 1: Write the failing test**

Registration goes through `world.EventHandler.RegisterEvent(key, factory)` (`EventHandler.cs:251`), where `CreateEvent` is `Event CreateEvent(Player player, Object data)` (`EventHandler.cs:53`).

**Key format matters.** `_SeedCommands` (`EventHandler.cs:123–150`) registers argument-taking slash commands with a **trailing space** — `"/tell "`, `"/summon "`, `"/warp "`, `"/dropgold "` — while argument-less ones do not (`"/who"`). Matching is longest-prefix over the raw packet (`EventHandler.cs:286`). So the key is `"/dimension "`, with the trailing space.

```csharp
[Fact]
public void Registers_the_dimension_command()
{
    using var fixture = Run(f => f.AddBaseMap(1, "Town"));

    // AddEvent returns false when no command matches the packet prefix (EventHandler.cs:286).
    Assert.True(fixture.World.EventHandler.AddEvent(new Player(0), "/dimension 1"));
    Assert.False(fixture.World.EventHandler.AddEvent(new Player(0), "/notacommand"));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — no command registered.

**Step 3: Write minimal implementation**

```csharp
public class DimensionCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new DimensionCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        int dim;
        if (tokens.Length < 2 || !int.TryParse(tokens[1], out dim) || dim < 0 || dim > Dimensions.DimensionCount)
        {
            world.Send(this.Player, P.ServerMessage("/dimension <0-" + Dimensions.DimensionCount + ">"));
            return;
        }

        int max = this.Player.Properties.GetProperty<int>(Dimensions.MaxDimensionProperty, 0);
        if (dim > max)
        {
            world.Send(this.Player, P.ServerMessage(
                "The void has rejected you. You have a maximum dimension of " + max + "."));
            return;
        }

        var target = world.MapHandler.GetMap(Dimensions.StartMapId + Dimensions.Offset * dim);
        if (target == null)
        {
            world.Send(this.Player, P.ServerMessage("That dimension does not exist."));
            return;
        }

        this.Player.WarpTo(world, target, Dimensions.WardenX, Dimensions.WardenY);
    }
}
```

Register it in `OnLoaded`, with the trailing space:

```csharp
world.EventHandler.RegisterEvent("/dimension ", DimensionCommandEvent.Create);
```

`Event` is a public abstract class (`Goose/Event.cs:10`) and `P` is a public static class (`Goose/Packets.cs:8`), so both are reachable from scripts — `Aspereta.csx:237` already registers an event this way.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add /dimension warp command"
```

---

## Task 7: Warden NPC and the unlock quest chain

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Create: `Goose/Data/Illutia/Scripts/Quest/DimensionUnlock.csx`
- Modify: `Goose.Tests/Goose.Tests.csproj` (copy `DimensionUnlock.csx` to output)
- Test: `Goose.Tests/DimensionsScriptTests.cs`

The boss spawn is data. `BossTemplateId` names a template that is spawned in dimension 0;
`CloneSpawns` copies that spawn into every dimension, which is what gives dimensions 1–6
their bosses. Nothing in this task creates spawns.

**Step 1: Write the failing test**

```csharp
[Fact]
public void Creates_one_unlock_quest_per_dimension()
{
    using var fixture = Run(f =>
    {
        f.AddBaseMap(1, "Town", width: 100, height: 100);
        var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        boss.BaseStats.HP = 3704;
        f.World.NPCHandler.AddTemplate(boss);
    });

    for (int dim = 0; dim < 6; dim++)
    {
        var quest = fixture.World.QuestHandler.Get(900000 + dim);
        Assert.NotNull(quest);

        var requirement = quest.Requirements.Single();
        Assert.Equal(RequirementType.Kill, requirement.Type);
        // Dimension n's quest wants dimension n's boss - a distinct template id, which is
        // what makes the stock Kill requirement dimension-aware (Player.cs:1020).
        Assert.Equal(162 + 100000 * dim, requirement.Value);
        Assert.Equal(1, requirement.Value2);

        Assert.Equal(RewardType.Script, quest.Rewards.Single().Type);
    }
}

[Fact]
public void Quest_ids_are_deterministic_across_runs()
{
    using var first = Run(SeedBoss);
    using var second = Run(SeedBoss);

    Assert.Equal(first.World.QuestHandler.Get(900003).Requirements.Single().Id,
                 second.World.QuestHandler.Get(900003).Requirements.Single().Id);
}

private static void SeedBoss(GlobalScriptFixture f)
{
    f.AddBaseMap(1, "Town", width: 100, height: 100);
    var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
    boss.BaseStats.HP = 3704;
    f.World.NPCHandler.AddTemplate(boss);
}

[Fact]
public void Wardens_carry_their_dimensions_quest()
{
    using var fixture = Run(SeedBoss);

    var warden = fixture.World.NPCHandler.GetTemplates()
        .Single(t => t.NPCType == NPCTemplate.Types.Quest && t.Quests.Any(q => q.Id == 900002));

    Assert.Single(warden.Quests);
}

/// <summary>The warden is built from configuration, not from sheet data, so the
/// configuration is the only thing that decides what players see and whether it can be
/// killed. A killable quest giver in a forced-PVP dimension is a griefing target.</summary>
[Fact]
public void Wardens_use_the_configured_appearance_and_cannot_be_killed()
{
    using var fixture = Run(SeedBoss);

    var warden = fixture.World.NPCHandler.GetNPCTemplate(800000 + 100000 * 2);

    Assert.NotNull(warden);
    Assert.Equal(NPCTemplate.Types.Quest, warden.NPCType);
    Assert.False(warden.CanBeKilled);
    Assert.False(warden.CanMove);
    Assert.Equal(50, warden.Level);
    Assert.Equal(3, warden.ClassID);

    // Appearance comes from config verbatim - dimension recolouring must NOT be applied.
    Assert.Equal(1, warden.BodyID);
    Assert.Equal(1, warden.FaceID);
    Assert.Equal(1, warden.HairID);
    Assert.Equal(40, warden.BodyR);
    Assert.Equal(20, warden.HairR);
    Assert.Equal("", warden.EquippedItems);
}

/// <summary>NPC.LoadFromTemplate dereferences Class.GetLevel(Level) at NPC.cs:636 with no
/// null check, so a warden on a class with no row at WardenLevel throws mid-spawn and
/// leaves the world half-built. Class 1 has levels 1-5 only, which is exactly the shape of
/// this mistake.</summary>
[Fact]
public void A_warden_class_with_no_row_at_the_warden_level_is_rejected_up_front()
{
    var fixture = new GlobalScriptFixture();
    SeedBoss(fixture);
    fixture.RemoveClassLevel(classId: 3, level: 50);

    using (fixture)
    {
        var ex = Assert.Throws<Exception>(
            () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
        Assert.Contains("50", ex.Message);
    }
}

[Fact]
public void Wardens_are_spawned_on_the_start_map_of_every_dimension_that_has_a_quest()
{
    using var fixture = Run(SeedBoss);

    // Dimensions 0-5 each offer the quest that unlocks the dimension above them. Dimension
    // 6 is the top, so it has no quest and no warden.
    for (int dim = 0; dim < 6; dim++)
    {
        var map = fixture.World.MapHandler.GetMap(1 + 100000 * dim);
        Assert.Contains(map.NPCs, n => n.NPCTemplate.NPCTemplateID == 800000 + 100000 * dim);
    }

    Assert.DoesNotContain(fixture.World.MapHandler.GetMap(1 + 100000 * 6).NPCs,
                          n => n.NPCTemplate.NPCType == NPCTemplate.Types.Quest);
}

// ---- Id collisions ---------------------------------------------------------------

/// <summary>AddTemplate and AddQuest both overwrite silently. Generated ids landing on
/// sheet-authored rows would replace real content with no diagnostic at all, so every
/// generated id space gets a preflight check.</summary>
[Theory]
[InlineData("npc template", 800000)]      // warden base id
[InlineData("npc template", 100162)]      // dimension-1 clone of the seeded boss
public void Generated_npc_template_ids_must_not_already_exist(string _, int id)
{
    var fixture = new GlobalScriptFixture();
    SeedBoss(fixture);
    fixture.World.NPCHandler.AddTemplate(new NPCTemplate { NPCTemplateID = id, Name = "Impostor" });

    using (fixture)
    {
        var ex = Assert.Throws<Exception>(
            () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
        Assert.Contains(id.ToString(), ex.Message);
    }
}

[Fact]
public void Generated_quest_ids_must_not_already_exist()
{
    var fixture = new GlobalScriptFixture();
    SeedBoss(fixture);
    fixture.World.QuestHandler.AddQuest(new Quest { Id = 900003, Name = "Sheet-authored" });

    using (fixture)
    {
        var ex = Assert.Throws<Exception>(
            () => fixture.CompileShipped().Object.OnLoaded(fixture.World));
        Assert.Contains("900003", ex.Message);
    }
}

/// <summary>Requirement and reward ids are the persistence key for in-flight quest
/// progress, so a collision there corrupts saved progress rather than just content.</summary>
[Fact]
public void Requirement_and_reward_ids_do_not_collide_with_each_other()
{
    using var fixture = Run(SeedBoss);

    var ids = new List<int>();
    for (int dim = 0; dim < 6; dim++)
    {
        var quest = fixture.World.QuestHandler.Get(900000 + dim);
        ids.AddRange(quest.Requirements.Select(r => r.Id));
        ids.AddRange(quest.Rewards.Select(r => r.Id));
    }

    Assert.Equal(ids.Count, ids.Distinct().Count());
}

// ---- The reward actually grants the dimension ------------------------------------

/// <summary>The chain is only worth anything if completing a quest raises dimension.max
/// and that survives a save. Nothing above tests the reward script at all.</summary>
[Fact]
public void Completing_a_quest_raises_dimension_max()
{
    using var fixture = Run(SeedBoss);

    var reward = fixture.World.QuestHandler.Get(900002).Rewards.Single();
    var player = new Player(0);

    reward.Script.Object.GiveReward(reward, npc: null, player, fixture.World);

    // Quest index 2 unlocks dimension 3.
    Assert.Equal(3, player.Properties.GetProperty<int>("dimension.max", 0));
}

[Fact]
public void The_reward_raises_but_never_lowers_dimension_max()
{
    using var fixture = Run(SeedBoss);

    var player = new Player(0);
    player.Properties["dimension.max"] = 5;

    var reward = fixture.World.QuestHandler.Get(900000).Rewards.Single();
    reward.Script.Object.GiveReward(reward, npc: null, player, fixture.World);

    Assert.Equal(5, player.Properties.GetProperty<int>("dimension.max", 0));
}

/// <summary>And it must persist - the property is only useful if it comes back after a
/// restart. This closes the loop with Part 1's player_properties column.</summary>
[Fact]
public void A_granted_dimension_survives_a_save_and_reload()
{
    using var fixture = Run(SeedBoss);

    var reward = fixture.World.QuestHandler.Get(900002).Rewards.Single();
    var player = new Player(0);
    reward.Script.Object.GiveReward(reward, npc: null, player, fixture.World);

    var json = JsonHelper.Serialize(player.Properties.Clone());
    var reloaded = new Player(0);
    reloaded.LoadPropertiesFromColumn(json);

    Assert.Equal(3, reloaded.Properties.GetProperty<int>("dimension.max", 0));
}
```

`RemoveClassLevel` is a fixture helper for the warden-class test — the fixture has to seed
at least one class with a level-50 row for any of these tests to spawn an NPC at all, so it
also needs a way to take it away again. If `ClassHandler` has no removal API, seed the
fixture's classes through a helper that the test can parameterise instead.

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — `QuestHandler.Get(900000)` returns null.

**Step 3: Write minimal implementation**

Add `CreateUnlockChain(world);` to `OnLoaded` after `CloneSpawns(world);`.

### `Dimensions.csx`

```csharp
/// <summary>One quest per dimension: kill that dimension's boss, unlock the next
/// dimension. Quest n is offered by dimension n's warden, so the chain walks the player
/// outward one dimension at a time.
///
/// This differs from abyss, where quest 1 was auto-granted at character creation
/// (Player.java:1072) and auto-completed on the kill with no NPC involved (Quests.java:80).
/// Goose quests are NPC-offered through QuestWindow with explicit accept and turn-in, so
/// the warden supplies the giver rather than the quest engine gaining an auto-grant
/// path.</summary>
private void CreateUnlockChain(GameWorld world)
{
    ValidateWardenClass(world);

    var rewardScript = world.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/DimensionUnlock.csx");

    for (int dim = 0; dim < DimensionCount; dim++)
    {
        int questId = QuestIdBase + dim;

        // AddQuest overwrites silently, and quest ids are the persistence key for
        // in-flight progress. A collision with a sheet-authored quest must be loud.
        if (world.QuestHandler.Get(questId) != null)
            throw new Exception($"Quest id {questId} already exists. QuestIdBase collides with sheet data.");

        var quest = new Quest
        {
            Id = questId,
            Name = "Abysmal Terror (" + (dim + 1) + ")",
            Description = "Slay the terror that stalks dimension " + dim + ".",
            FailText = "The terror still lives.",
            PassText = "The void yields. Dimension " + (dim + 1) + " is open to you.",
            ShowProgress = true,
            Repeatable = false,
            // Chain: quest n requires quest n-1. Quest 0 is the entry point.
            PrerequisiteQuests = dim == 0 ? new List<int>() : new List<int> { QuestIdBase + dim - 1 },
        };

        quest.Requirements.Add(new QuestRequirement
        {
            // Deterministic - QuestProgress persists keyed on requirement.Id
            // (Player.cs:1020, QuestWindow.cs:268). A counter-assigned id would orphan
            // in-flight kill progress on every restart.
            Id = QuestIdBase + dim * 10,
            Type = RequirementType.Kill,
            // Dimension n's boss is a distinct template id, which is the whole reason the
            // stock Kill requirement is dimension-aware with no engine change.
            Value = BossTemplateId + Offset * dim,
            Value2 = 1,
            KeepRequirement = false,
            Quest = quest,
        });

        quest.Rewards.Add(new QuestReward
        {
            Id = QuestIdBase + dim * 10 + 1,
            Type = RewardType.Script,
            Script = rewardScript,
            // QuestReward has no Quest back-reference (QuestReward.cs:37-45), unlike
            // QuestRequirement, so the reward cannot derive its dimension from its quest.
            // ScriptParams carries it, and one script file serves all six rewards.
            ScriptParams = (dim + 1).ToString(),
        });

        world.QuestHandler.AddQuest(quest);

        CreateWarden(world, dim, quest);
    }
}

/// <summary>NPC.LoadFromTemplate does Class.GetLevel(Level).BaseStats with no null check
/// (NPC.cs:635-636). ClassHandler.GetClass returns null for an unknown id and
/// Class.GetLevel returns null for a level the class has no row for - class 1 (Commoner)
/// stops at level 5 while classes 2-7 reach 50. Either mistake would throw halfway through
/// building the world, so check once, up front, with a message that says what to fix.</summary>
private void ValidateWardenClass(GameWorld world)
{
    var wardenClass = world.ClassHandler.GetClass(WardenClassId);
    if (wardenClass == null)
        throw new Exception($"WardenClassId {WardenClassId} does not exist.");

    if (wardenClass.GetLevel(WardenLevel) == null)
        throw new Exception($"Class {WardenClassId} has no level {WardenLevel} row in class_info. "
                            + "Pick a class that reaches WardenLevel, or lower WardenLevel.");
}

/// <summary>The quest giver for one dimension. Built from configuration rather than cloned
/// from a base template - there is no warden in sheet data to clone.
///
/// Deliberately NOT run through ScaleTemplate: scaling an invincible quest giver's HP and
/// damage is meaningless, and the dimension recolour would fight the configured look.</summary>
private void CreateWarden(GameWorld world, int dim, Quest quest)
{
    int templateId = WardenTemplateId + Offset * dim;

    if (world.NPCHandler.GetNPCTemplate(templateId) != null)
        throw new Exception($"Warden template id {templateId} already exists. "
                            + "WardenTemplateId collides with sheet data.");

    var warden = new NPCTemplate
    {
        NPCTemplateID = templateId,
        NPCType = NPCTemplate.Types.Quest,
        Name = WardenName + (dim == 0 ? "" : " (" + dim + ")"),
        Title = WardenTitle,
        Surname = WardenSurname,
        Level = WardenLevel,
        ClassID = WardenClassId,

        CanBeKilled = false,     // maps to npc_templates.invincible (NPCHandler.cs:63)
        CanMove = false,
        CanBeRooted = false,
        CanBeStunned = false,
        CanBeSlowed = false,

        WeaponDamage = 0,
        AggroRange = 0,
        AttackRange = 1,
        AttackSpeed = 1m,
        MoveSpeed = 1m,
        RespawnTime = 0,
        Experience = 0,

        BodyID = WardenBodyID,
        BodyState = WardenBodyState,
        BodyR = WardenBodyR, BodyG = WardenBodyG, BodyB = WardenBodyB, BodyA = WardenBodyA,
        FaceID = WardenFaceID,
        HairID = WardenHairID,
        HairR = WardenHairR, HairG = WardenHairG, HairB = WardenHairB, HairA = WardenHairA,
        EquippedItems = WardenEquippedItems,

        AlliesString = "",
        Allies = new List<NPCTemplate>(),
        Drops = new List<NPCDropInfo>(),
    };

    warden.BaseStats = new AttributeSet { HP = 1000, MP = 0 };

    // Sheet-authored quest_ids are resolved at template-load time (NPCHandler.cs:108),
    // which runs before global scripts - so a script-created quest can never be attached
    // through data. It has to be attached here. NPC.cs:637 aliases template.Quests rather
    // than copying, so attaching before spawning is sufficient.
    warden.Quests.Add(quest);

    world.NPCHandler.AddTemplate(warden);

    // shouldRespawn: false - it cannot be killed, so it never needs to come back.
    if (world.NPCHandler.SpawnNPC(world, WardenMapId + Offset * dim,
                                  WardenX, WardenY, warden, shouldRespawn: false) == null)
    {
        throw new Exception($"Could not spawn the dimension-{dim} warden: map "
                            + (WardenMapId + Offset * dim) + " does not exist.");
    }
}
```

Note `CreateUnlockChain` runs **after** `CloneTemplates`, so the wardens are created once
each and never see the scaler — `CloneTemplates` has already finished its snapshot of the
base templates by then.

`Quest.PrerequisiteQuests` is a `List<int>` (`Goose/Quests/Quest.cs:30`), so it is assigned
directly rather than through the space/comma-separated string form `FromReader` parses
(`Quest.cs:62`). `QuestReward.Script` is a settable `Script<IQuestScript>`
(`QuestReward.cs:44`); `FromReader` populates it with
`world.ScriptHandler.GetScript<IQuestScript>(scriptPath)` (`QuestReward.cs:61`), and this
does the same thing directly, since these rewards never come from a database row.

### `Scripts/Quest/DimensionUnlock.csx`

```csharp
using System;
using Goose;
using Goose.Quests;
using Goose.Scripting;

/// <summary>The RewardType.Script reward on every dimension unlock quest. Raises the
/// player's maximum dimension to the one this quest grants.
///
/// One instance is shared by all six rewards - ScriptHandler caches one object per file
/// path (ScriptHandler.cs:20-30) - so the dimension is read from reward.ScriptParams on
/// every call and never cached in a field. That is the IQuestScript contract.</summary>
public class DimensionUnlock : BaseQuestScript
{
    private const string MaxDimensionProperty = "dimension.max";

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        int granted;
        if (!int.TryParse(reward.ScriptParams, out granted)) return;

        // Raise, never lower. Completing an earlier quest out of order - or a repeat after
        // a data change - must not take access away.
        int current = player.Properties.GetProperty<int>(MaxDimensionProperty, 0);
        if (granted <= current) return;

        player.Properties[MaxDimensionProperty] = granted;

        world.Send(player, P.ServerMessage(
            "The void yields. You may now enter dimension " + granted + "."));
    }
}
```

Persistence needs nothing further here: `Player.Properties` is serialised into
`players.player_properties` by the ordinary save path (Part 1 task 2), so the grant is
durable as soon as the player's next save runs.

Add the `<None Include>` for `DimensionUnlock.csx` to `Goose.Tests.csproj` at the same
time, and to `GlobalScriptFixture.ShippedScripts` if it is not already listed there.

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add warden NPCs and the dimension unlock quest chain"
```

---

## Task 7b: Enable the shipped script, and keep the disabled path tested

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (`Enabled = true`)
- Test: `Goose.Tests/DimensionsScriptTests.cs`

Task 2 flips `Enabled` to `true`, which invalidates Task 1's
`Disabled_by_configuration_changes_nothing`. **Do not simply delete it.** `Enabled = false`
is the documented way to return the server to stock behaviour (design doc, "Scripts"), and
the done criteria below depend on it — deleting the only test of that path leaves the
feature's escape hatch unverified.

Replace it with a test that compiles a variant of the shipped script with the flag off. The
variant is the shipped source with one line changed, so it still fails if the real script's
`OnLoaded` stops honouring the flag:

```csharp
[Fact]
public void Disabled_by_configuration_changes_nothing()
{
    using var fixture = new GlobalScriptFixture();
    fixture.AddBaseMap(1, "Town", width: 100, height: 100);
    var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
    boss.BaseStats.HP = 3704;
    fixture.World.NPCHandler.AddTemplate(boss);

    var source = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "Dimensions.csx"));
    var disabled = source.Replace("public const bool Enabled = true;",
                                  "public const bool Enabled = false;");
    Assert.NotEqual(source, disabled);   // the flag line moved - fix this test, not the script

    fixture.CompileSource(disabled, "DimensionsDisabled.csx").Object.OnLoaded(fixture.World);

    Assert.Single(fixture.World.MapHandler.Maps);
    Assert.Null(fixture.World.MapHandler.GetMap(100001));
    Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(100162));
    Assert.Null(fixture.World.QuestHandler.Get(900000));
    Assert.Equal(0, fixture.World.NPCHandler.NPCCount);
    // No /dimension command either - the whole feature is off, not just the world.
    Assert.False(fixture.World.EventHandler.AddEvent(new Player(0), "/dimension 1"));
}
```

The `Assert.NotEqual` is the guard that matters: if someone renames or reformats the flag,
this test would otherwise silently start asserting that the *enabled* script does nothing,
and pass for the wrong reason.

**Commit**

```bash
git add -A
git commit -m "test: keep the disabled-flag path covered after enabling dimensions"
```

---

## Task 8: End-to-end verification

No new code. This is the only check that exercises real Illutia data at full scale, and the numbers it produces decide whether the deferred idle-tick suppression (design follow-up 2) is needed.

**Step 0: Confirm `BossTemplateId` is spawned in dimension 0**

```bash
sqlite3 Goose/bin/Debug/IllutiaGoose.db "SELECT map_id, map_x, map_y FROM npc_spawns WHERE npc_id = 162;"
```

Step 3 needs it. Zero rows means the chain cannot be completed.

**Step 1: Start the server**

```bash
dotnet run --project Goose/Goose.csproj
```

Expected: reaches "Finished loading game. Ready to join." with no exceptions. Record how long the "Global Scripts" load step takes.

**Step 2: Check the generated world**

Expected counts, from the design doc's scale table:

| Check | Expected |
|---|---|
| `world.MapHandler.Count` | 1,120 (160 base × 7) |
| `world.NPCHandler.NPCCount` | ~81,696 (11,670 × 7, + 6 wardens) |
| `world.NPCHandler.TemplateCount` | ~4,591 (655 × 7, + 6 wardens) |

`NPCCount` reaching ~82k is only true because every generated spawn goes through
`NPCHandler.SpawnNPC` (Part 1 task 4). If this number comes back at ~11,670 — the base
spawns alone — something is calling `NPC.LoadFromTemplate` directly again.

Use the server console commands (`Goose/Console/`) if they expose these, otherwise add a temporary log line and remove it before committing.

Also record process RSS — 1,120 maps' tile and character arrays plus ~82k NPC objects is the memory cost the design accepted without measuring.

**Step 3: Walk the world**

As a GM character:

1. `/dimension 1` — expect refusal, since `dimension.max` starts at 0.
2. Find the dimension-0 warden at the configured position — confirm it looks the way the
   config says, offers "Abysmal Terror (1)", and cannot be attacked.
3. Accept the quest, kill the dimension-0 boss, turn in — expect the unlock message and
   `dimension.max = 1` in `players.player_properties` after the next save.
4. `/dimension 1` — expect to arrive on the dimension-1 start map.
5. Walk through a warp — expect to land on another **dimension-1** map, not the base one.
6. Attack a mob — expect a `" (1)"`-suffixed name, a darker recolour, and hugely inflated HP.
   Pull a second mob of an allied type and confirm it assists (the ally rewiring in Task 2b).
7. Bind in dimension 1, die, and confirm you respawn in dimension 1.
8. Log out and back in — expect to still be in dimension 1.
9. Set `dimension.max` back to 0 with the server stopped, log in — expect the
   `OnPlayerEntered` clamp to eject you to the dimension-0 map **and** to rewrite
   `bound_id` to the dimension-0 map. Die and confirm you respawn in dimension 0.
10. Confirm a key-gated base map's dimension copy still demands the key (the
    `requiredItems` path `Map.CloneAs` preserves).

**Step 4: Confirm the known limitations behave as documented**

These are expected, not bugs (design doc "Known limitations"):

- A teleport spell cast in dimension 1 lands in dimension 0.
- Dimension mobs drop ordinary dimension-0 loot.
- `ArenaMap`/`ZombieTownMap` clones behave as plain maps.

**Step 5: Commit any fixes, then record the measurements**

Add the observed startup time, map/NPC counts and RSS to the design doc's follow-up 2 so the idle-tick decision has data behind it.

```bash
git add -A
git commit -m "docs: record dimension world load measurements"
```

---

## Done when

- `dotnet test Goose.sln` reports 0 failures.
- The server starts with `Enabled = true` and reports ~1,120 maps and ~82k NPCs — the NPC
  count coming from `NPCHandler.NPCCount`, not from a count of map npc lists.
- A player can be gated out of, unlocked into, warped around inside, and clamped back out of
  a dimension — **both** their current map and their bind.
- Completing an unlock quest raises `dimension.max` and it survives a restart.
- Setting `Enabled = false` returns the server to stock behaviour, and a test proves it.
- Every shipped dimension script (`Dimensions.csx`, `DimensionMap.csx`,
  `DimensionUnlock.csx`) is installed by `GlobalScriptFixture` and exercised by tests.
