# Dimensions Part 2 — Scripts Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Clone the world into 6 scaled dimensions and gate access to them, entirely from two `.csx` scripts using the extension points Part 1 added.

**Architecture:** `Scripts/Global/Dimensions.csx` runs at `OnLoaded` — after maps, templates and spawns are all loaded — and generates dimension copies of NPC templates, maps and spawns at `id + 100000·dim`. `Scripts/Map/DimensionMap.csx` attaches to every clone and enforces entry gating. Unlock progress lives in `Player.Properties`.

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

**Step 1: Write the fixture**

```csharp
using Goose.Scripting;

namespace Goose.Tests.Fixtures;

public sealed class GlobalScriptFixture : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;

    public string DataDirectory { get; }
    public GameWorld World { get; }

    public GlobalScriptFixture()
    {
        DataDirectory = Path.Combine(Path.GetTempPath(), "global-script-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Global"));
        Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Map"));
        GameWorld.Settings = new GooseSettings
        {
            DataPath = DataDirectory, ExperienceModifier = 1,
            InventorySize = 30, EquippedSize = 20, CombineBagSize = 10, SpellbookSize = 30,
        };
        World = new GameWorld(null);
    }

    /// <summary>Copies the real Dimensions.csx into the temp data dir and compiles it,
    /// so tests exercise the shipped script rather than a paraphrase of it.</summary>
    public Script<IGlobalScript> CompileShipped(string sourcePath, string fileName)
    {
        var relativePath = "Scripts/Global/" + fileName;
        File.Copy(sourcePath, Path.Combine(DataDirectory, relativePath), overwrite: true);
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
    private const string ScriptPath = "Goose/Data/Illutia/Scripts/Global/Dimensions.csx";

    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        arrange(fixture);
        fixture.CompileShipped(ScriptPath, "Dimensions.csx").Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Disabled_by_configuration_changes_nothing()
    {
        // Dimensions.Enabled is false in the shipped script until the feature is switched on.
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
  <None Include="../Goose/Data/Illutia/Scripts/Map/DimensionMap.csx"
        Link="DimensionScripts/DimensionMap.csx" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

and resolve in the test as:

```csharp
private static readonly string ScriptPath =
    Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "Dimensions.csx");
```

Add the `DimensionMap.csx` entry in Task 5 and a `DimensionUnlock.csx` entry in Task 7, when those files first exist — an `Include` pointing at a missing file copies nothing and would leave the test failing on a confusing `FileNotFoundException`.

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

    /// <summary>NPC template gating each dimension. Abyss used 162 (King Terror).</summary>
    public const int BossTemplateId = 162;

    /// <summary>Quest-giver placement, per dimension, on that dimension's start map.</summary>
    public const int WardenTemplateId = 0;   // 0 = create one from scratch
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

Flip `Enabled` to `true` in the script as part of this task — the disabled test from Task 1 must be updated to set expectations accordingly, or deleted in favour of a test that compiles a copy with `Enabled = false`. Prefer keeping the shipped script enabled and dropping the Task 1 test once these replace it.

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

## Task 3: Clone maps and rewire warps

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionsScriptTests.cs`

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
            var clone = new Map
            {
                ID = basic.ID + Offset * dim,
                Name = basic.Name + " (" + dim + ")",
                FileName = basic.FileName,
                Width = basic.Width,
                Height = basic.Height,
                MinLevel = basic.MinLevel,
                MaxLevel = basic.MaxLevel,
                // Entry gates scale by (dim*5)^2
                MinExperience = basic.MinExperience * (dim * 5) * (dim * 5),
                MaxExperience = basic.MaxExperience * (dim * 5) * (dim * 5),
                CanPVP = true,                       // forced on in every dimension
                CanChat = basic.CanChat,
                CanAuction = basic.CanAuction,
                CanShout = basic.CanShout,
                CanCast = basic.CanCast,
                CanBind = basic.CanBind,
                CanUseItems = basic.CanUseItems,
                CanSpawnPets = basic.CanSpawnPets,
                Script = mapScript,
                ScriptParams = dim.ToString(),       // DimensionMap reads its dimension from here

                // Shallow tile copy: BlockedTiles are stateless and shared, WarpTiles are
                // replaced in RewireWarps, ItemTiles only ever appear at runtime.
                // Deliberately NOT Map.LoadData - that re-parses the .map file and issues
                // two SQL queries keyed on the clone's id, which match no rows (Map.cs:466).
                tiles = (ITile[])basic.tiles.Clone(),
                characters = new ICharacter[(basic.Width + 1) * (basic.Height + 1)],
            };

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

        var npc = new NPC();
        npc.LoadFromTemplate(f.World, 1, 50, 50, t, shouldRespawn: true);
    });

    var dim1Map = fixture.World.MapHandler.GetMap(100001);
    var spawned = dim1Map.NPCs.Single();

    Assert.Equal(162 + 100000, spawned.NPCTemplate.NPCTemplateID);
    Assert.Equal(50, spawned.SpawnX);
    Assert.Equal(50, spawned.SpawnY);
}
```

`Map.NPCs` is a public accessor over the map's npc list (`Goose/Map.cs:618`), so the assertion above works as written.

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — the dimension map has no NPCs.

**Step 3: Write minimal implementation**

Add `CloneSpawns(world);` to `OnLoaded` after `RewireWarps(world);`:

```csharp
private void CloneSpawns(GameWorld world)
{
    // Snapshot before spawning - the handler's npc list grows as we go.
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

            // shouldRespawn: true - respawning is self-sustaining on the NPC, matching
            // how NPCHandler.LoadNPCs:277 creates the base spawns.
            new NPC().LoadFromTemplate(world, basic.Map.ID + Offset * dim,
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
- Create: `Goose/Data/Illutia/Scripts/Map/DimensionMap.csx`
- Test: `Goose.Tests/DimensionMapScriptTests.cs` (create)

Two gates are needed, because they cover different paths:

- `CanPlayerJoin` (Part 1) covers warps (`MoveEvent.cs:123`) and teleport spells (`SpellEffect.cs:727`).
- `OnPlayerEntered` (`Map.cs:137`) covers **login** — a player whose saved `map_id` is a dimension map is placed directly onto it without passing `PlayerCanJoin`. Abyss needed a separate check for the same reason (`Player.java:1458`).

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
        var script = fixture.CompileMapScript("Goose/Data/Illutia/Scripts/Map/DimensionMap.csx");
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
        var script = fixture.CompileMapScript("Goose/Data/Illutia/Scripts/Map/DimensionMap.csx");
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
        var script = fixture.CompileMapScript("Goose/Data/Illutia/Scripts/Map/DimensionMap.csx");
        var map = fixture.AddBaseMap(100001, "Town (1)");
        map.ScriptParams = "1";

        Assert.NotNull(script.Object.CanPlayerJoin(map, new Player(0), fixture.World));
    }
}
```

Add a `CompileMapScript(string sourcePath)` method to `GlobalScriptFixture` mirroring `CompileShipped` but returning `Script<IMapScript>` and copying into `Scripts/Map/`.

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
    /// dimension-0 copy of wherever they were.</summary>
    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        if (DimensionOf(map) <= MaxDimensionOf(player)) return;

        int baseId = map.ID % Dimensions.Offset;
        var fallback = world.MapHandler.GetMap(baseId);
        if (fallback == null) return;

        world.Send(player, "$7The void has rejected you. You have a maximum dimension of "
                           + MaxDimensionOf(player) + ".");
        player.WarpTo(world, fallback, player.MapX, player.MapY);
    }
}
```

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
Expected: PASS (3 tests).

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
- Test: `Goose.Tests/DimensionsScriptTests.cs`

**Verify first (blocking):** does `BossTemplateId` 162 have a spawn in the live data?

```bash
sqlite3 Goose/bin/Debug/IllutiaGoose.db "SELECT * FROM npc_spawns WHERE npc_id = 162;"
```

In the April 2025 snapshot this returns **zero rows**, which would make quest 0 uncompletable and block the whole chain at step one. If live data also returns nothing, `Dimensions.csx` must place a dimension-0 spawn of the boss as part of this task. Resolve this before writing code.

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
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: FAIL — `QuestHandler.Get(900000)` returns null.

**Step 3: Write minimal implementation**

Add `CreateUnlockChain(world);` to `OnLoaded` after `CloneSpawns(world);`. For each `dim` in `0..DimensionCount-1`:

- Build a `Quest` with `Id = QuestIdBase + dim`, a name like `"Abysmal Terror (" + (dim+1) + ")"`, and `PrerequisiteQuests = dim == 0 ? new List<int>() : new List<int> { QuestIdBase + dim - 1 }`.
- One `QuestRequirement` with `Id = QuestIdBase + dim * 10`, `Type = RequirementType.Kill`, `Value = BossTemplateId + Offset * dim`, `Value2 = 1`, `Quest = quest`.
- One `QuestReward` with `Id = QuestIdBase + dim * 10 + 1`, `Type = RewardType.Script`, pointing at a reward script that sets `dimension.max = dim + 1`.
- `world.QuestHandler.AddQuest(quest)`.
- A warden template per dimension: copy or create one with `NPCType = NPCTemplate.Types.Quest`, register via `AddTemplate`, add the quest to its `Quests` list, and spawn it at `(WardenX, WardenY)` on that dimension's start map.

The `RewardType.Script` reward needs a `.csx` at `Scripts/Quest/DimensionUnlock.csx` implementing `IQuestScript.GiveReward` to write `player.Properties[MaxDimensionProperty] = <granted dimension>`.

Two API facts to build against:

- `QuestReward` has **no** `Quest` back-reference (`Goose/Quests/QuestReward.cs:37–45` — unlike `QuestRequirement`, which does have `public Quest Quest`). So the reward cannot derive its dimension from its quest. Carry it in `reward.ScriptParams` instead, which is exactly what that field is for; one script file then serves all six quests.
- `QuestReward.Script` is a settable `Script<IQuestScript>` property (`QuestReward.cs:44`). `FromReader` populates it with `world.ScriptHandler.GetScript<IQuestScript>(scriptPath)` (`QuestReward.cs:61`); do the same directly, since these rewards never come from a database row.

`Quest.PrerequisiteQuests` is a `List<int>` (`Goose/Quests/Quest.cs:30`), so assign it directly rather than through the space/comma-separated string form `FromReader` parses (`Quest.cs:62`).

Because `NPCHandler.cs:108` resolves `quest_ids` at template-load time — before global scripts — the warden's quests must be attached by this script, not by sheet data. `NPC.cs:637` aliases `template.Quests`, so attaching before spawning is sufficient.

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionsScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: add warden NPCs and the dimension unlock quest chain"
```

---

## Task 8: End-to-end verification

No new code. This is the only check that exercises real Illutia data at full scale, and the numbers it produces decide whether the deferred idle-tick suppression (design follow-up 2) is needed.

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
| `world.NPCHandler.NPCCount` | ~81,690 (11,670 × 7) |
| `world.NPCHandler.TemplateCount` | ~4,585 (655 × 7) |

Use the server console commands (`Goose/Console/`) if they expose these, otherwise add a temporary log line and remove it before committing.

Also record process RSS — 1,120 maps' tile and character arrays plus ~82k NPC objects is the memory cost the design accepted without measuring.

**Step 3: Walk the world**

As a GM character:

1. `/dimension 1` — expect refusal, since `dimension.max` starts at 0.
2. Grant yourself the unlock (kill the dimension-0 boss, or set `dimension.max` directly in the DB with the server stopped — see Readme step 8 for why the server must be stopped).
3. `/dimension 1` — expect to arrive on the dimension-1 start map.
4. Walk through a warp — expect to land on another **dimension-1** map, not the base one.
5. Attack a mob — expect a `" (1)"`-suffixed name, a darker recolour, and hugely inflated HP.
6. Log out and back in — expect to still be in dimension 1.
7. Set `dimension.max` back to 0 with the server stopped, log in — expect the `OnPlayerEntered` clamp to eject you to the dimension-0 map.

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
- The server starts with `Enabled = true` and reports ~1,120 maps and ~82k NPCs.
- A player can be gated out of, unlocked into, warped around inside, and clamped back out of a dimension.
- Setting `Enabled = false` returns the server to stock behaviour.
