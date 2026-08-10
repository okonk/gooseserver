# Dimensions Part 4 — Items Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Give every dimension its own copy of the game's equipment and spell tomes at `baseId + 100000·dim`, with abyss name prefixes, recolour, stat scaling, rarity and suffix rolls, pickup gating, and dimension-aware drop tables.

**Architecture:** Five small generic server additions (an `ItemTemplate` copy constructor, three `ItemHandler` registration methods, two `IItemScript` members with their call sites). Everything dimension-aware lives in a new generation pass in `Scripts/Global/Dimensions.csx` plus three new scripts: `DimensionItem.csx`, `DimensionSurname.csx`, `DimensionRarity.csx`.

**Tech Stack:** C# / .NET 10, SQLite (System.Data.SQLite), xUnit, Roslyn C# scripting (`.csx`).

**Design doc:** `docs/plans/2026-08-10-dimensions-items-design.md`

**Depends on:** Parts 1–3, merged at `a41a79f`.

**Out of scope:** Vendor overrides, dimension vendor stock, `/resetitem`, `/rebirth`, SP as currency. Do not write them here.

---

## APIs verified

Every citation was read from source in this worktree (or `~/code/abyssserver` where marked) before writing the plan.

| Fact | Location |
|---|---|
| `ItemHandler.templates/items/titles/surnames` are private dicts | `Goose/ItemHandler.cs:18–21` |
| `ItemHandler.GetTemplate(int)` returns null when absent | `Goose/ItemHandler.cs:189` |
| `ItemHandler.GetTemplates()` returns the live `templates.Values` | `Goose/ItemHandler.cs:42` |
| `ItemHandler.RollTitleAndSurname` early-returns for non-armor/weapon | `Goose/ItemHandler.cs:242–244` |
| `RollModifier` builds ranges as `(int)(Chance * 100)` | `Goose/ItemHandler.cs:270–279` |
| `AddAndAssignId` calls `Script?.Object.OnCreateEvent` in a swallowing try/catch | `Goose/ItemHandler.cs:197–209` |
| `ItemTemplate` has no constructor; full property list | `Goose/ItemTemplate.cs:61–115` |
| `ItemModifier` property list and `ApplyStats` | `Goose/ItemModifier.cs:12–22`, `:71` |
| `IItemScript` has exactly 3 members | `Goose/Scripting/IItemScript.cs:9–15` |
| `BaseItemScript` has 5 virtuals (2 unused by the interface) | `Goose/Scripting/BaseItemScript.cs:9–37` |
| `Item.LoadFromTemplate` **accumulates** `TotalStats` and copies `ScriptParams` | `Goose/Item.cs:155–177` |
| `Item.RefreshStats` = `(template.BaseStats + item.BaseStats) × StatMultiplier` | `Goose/Item.cs:247–257` |
| `Item.Script` proxies `Template.Script` | `Goose/Item.cs:133` |
| `Item.ItemProperties` is `Dictionary<ItemProperty, object>`, persisted as `props` | `Goose/Item.cs:77–78` |
| `ItemProperty` enum: `TitleId`, `SurnameId` | `Goose/Item.cs:14–18` |
| Pickup path: `Inventory.AddItem` call and its guards | `Goose/Events/PickupItemEvent.cs:86–101` |
| `UseConsumable` calls `OnUseConsumableEvent`, swallowing exceptions, `remove` defaults true | `Goose/Inventory.cs:416–436` |
| Scroll use path bypasses scripts entirely | `Goose/Inventory.cs:277–283` |
| `Spellbook.GetSlot`, `AddSpell`, `RemoveSpell` are public | `Goose/Spellbook.cs:173`, `:216`, `:248` |
| `Player.Spellbook` | `Goose/Player.cs:354` |
| `Player.Properties` is a `PropertiesDictionary` with `GetProperty<T>(key, default)` | `Goose/Player.cs:458`, `Goose/PropertiesDictionary.cs:43` |
| `NPCTemplate` copy ctor copies the `Drops` list but **shares its elements** | `Goose/NPCTemplate.cs:251` |
| `NPCDropInfo` fields: `DropRate`, `Stack`, `ItemTemplate` | `Goose/NPCDropInfo.cs:10–12` |
| NPC death drop path: `LoadFromTemplate` → `RollTitleAndSurname` → `AddAndAssignId` | `Goose/NPC.cs:1428–1433` |
| `ScriptHandler.GetScript<T>` caches by absolute path | `Goose/Scripting/ScriptHandler.cs:19–31` |
| `Map.Script` / `Map.ScriptParams` assigned by the clone loop | `Scripts/Global/Dimensions.csx:220–221` |
| `IMapScript` has 10 members | `Goose/Scripting/IMapScript.cs:11–24` |
| `GameWorld.RollChance(double)` takes a **fraction of 1**, not a percent | `Goose/GameWorld.cs:696` |
| `AttributeSet` fields and `+` / `*` operators | `Goose/AttributeSet.cs:14–39`, `:105`, `:173` |
| Item stats reach the wearer: `AddStats(item.TotalStats)` → `MaxStats` | `Goose/Inventory.cs:325`, `Goose/Player.cs:1616` |
| abyss `dimensionDefault` formula | `~/code/abyssserver/src/Abyss/AttributeSet.java:376–444` |
| abyss rarity/suffix roll and name/recolour/value | `~/code/abyssserver/src/Abyss/Item.java:359–446` |
| abyss disables bind/LORE for dimension items | `~/code/abyssserver/src/Abyss/Item.java:225–260` |

### Two facts worth stating plainly

1. **`RollChance` takes a fraction.** `ItemSurnameChancePercent` is `0.5` in `GooseSettings.json:152`, and `RollTitleAndSurname` passes it straight to `RollChance` — so the native surname roll fires on **50%** of dropped equipment, not 0.5%. This is pre-existing behaviour; do **not** change it. It only raises the stakes on Task 2's suppression hook.
2. **`Item.LoadFromTemplate` accumulates.** `this.TotalStats += this.Template.BaseStats` (`Goose/Item.cs:159`). Never call it twice on the same item.

---

## Configuration added to `Scripts/Global/Dimensions.csx`

Add to the existing configuration block, below `QuestIdBase`:

```csharp
    /// <summary>Generated ItemModifier ids. item_surnames/item_titles are sheet data with
    /// small ids; these sit far above so a new sheet row can never collide. The two
    /// dictionaries are separate (ItemHandler.cs:20,21), so the ranges only need to be
    /// distinct from sheet ids, not from each other.</summary>
    public const int SurnameIdBase = 900000;
    public const int TitleIdBase = 900100;
```

---

## Task 1: Server registration points

**Files:**
- Modify: `Goose/ItemTemplate.cs` (add copy constructor after the property block, ~line 116)
- Modify: `Goose/ItemHandler.cs` (add `AddTemplate`, `AddTitle`, `AddSurname` beside `GetTemplate`, ~line 195)
- Test: `Goose.Tests/ItemHandlerRegistrationTests.cs` (create)

**Step 1: Write the failing test**

```csharp
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class ItemHandlerRegistrationTests
{
    private static ItemTemplate Sample() => new ItemTemplate
    {
        ID = 42, Name = "Sword", Description = "A sword", UseType = ItemTemplate.UseTypes.Weapon,
        Slot = ItemTemplate.ItemSlots.OneHanded, Type = ItemTemplate.ItemTypes.OneHandedSword,
        MinLevel = 10, MaxLevel = 50, MinExperience = 100, MaxExperience = 200,
        BaseStats = new AttributeSet { HP = 5, Strength = 3, Haste = 0.25m },
        WeaponDamage = 7, WeaponDelay = 3, Value = 1000, ClassRestrictions = 6,
        GraphicEquipped = 1, GraphicTile = 2, GraphicFile = 3,
        GraphicR = 200, GraphicG = 150, GraphicB = 100, GraphicA = 120,
        IsLore = true, IsBindOnPickup = true, IsBindOnEquip = true, IsEvent = true,
        StackSize = 1, BodyState = 4, SpellEffectID = 9, SpellEffectChance = 5m,
        LearnSpellID = 11, Credits = 12, ScriptParams = "params",
    };

    [Fact]
    public void Copy_constructor_copies_every_property()
    {
        var copy = new ItemTemplate(Sample());

        Assert.Equal(42, copy.ID);
        Assert.Equal("Sword", copy.Name);
        Assert.Equal(ItemTemplate.UseTypes.Weapon, copy.UseType);
        Assert.Equal(ItemTemplate.ItemSlots.OneHanded, copy.Slot);
        Assert.Equal(ItemTemplate.ItemTypes.OneHandedSword, copy.Type);
        Assert.Equal(1000, copy.Value);
        Assert.Equal(6, copy.ClassRestrictions);
        Assert.Equal(120, copy.GraphicA);
        Assert.True(copy.IsLore && copy.IsBindOnPickup && copy.IsBindOnEquip && copy.IsEvent);
        Assert.Equal(4, copy.BodyState);
        Assert.Equal(11, copy.LearnSpellID);
        Assert.Equal("params", copy.ScriptParams);
        Assert.Equal(5, copy.BaseStats.HP);
        Assert.Equal(0.25m, copy.BaseStats.Haste);
    }

    [Fact]
    public void Copy_constructor_gives_the_copy_its_own_stats()
    {
        var basic = Sample();
        var copy = new ItemTemplate(basic);

        copy.BaseStats.HP = 999;

        // A shared AttributeSet would make every dimension clone mutate the base item.
        Assert.Equal(5, basic.BaseStats.HP);
    }

    [Fact]
    public void AddTemplate_registers_a_template_retrievable_by_id()
    {
        var world = new GameWorld(null);
        var template = Sample();

        world.ItemHandler.AddTemplate(template);

        Assert.Same(template, world.ItemHandler.GetTemplate(42));
        Assert.Contains(template, world.ItemHandler.GetTemplates());
    }

    [Fact]
    public void AddTitle_and_AddSurname_register_into_separate_dictionaries()
    {
        var world = new GameWorld(null);
        var title = new ItemModifier { Id = 1, Name = "Legendary" };
        var surname = new ItemModifier { Id = 1, Name = "of Speed" };

        world.ItemHandler.AddTitle(title);
        world.ItemHandler.AddSurname(surname);

        Assert.Equal(1, world.ItemHandler.TitleCount);
        Assert.Equal(1, world.ItemHandler.SurnameCount);
        Assert.Same(title, world.ItemHandler.GetTitle(1));
        Assert.Same(surname, world.ItemHandler.GetSurname(1));
    }
}
```

**Step 2: Run it and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~ItemHandlerRegistrationTests"`
Expected: compile errors — no `ItemTemplate` copy constructor, no `AddTemplate`/`AddTitle`/`AddSurname`/`GetTitle`/`GetSurname`.

**Step 3: Add the copy constructor**

In `Goose/ItemTemplate.cs`, immediately after the `ScriptParams` property (`:115`):

```csharp
        public ItemTemplate() { }

        /// <summary>Copies every field. Used by scripts that generate template variants
        /// (see Scripts/Global/Dimensions.csx). BaseStats is copied by value - a shared
        /// AttributeSet would let a generated clone mutate the sheet-authored original.</summary>
        public ItemTemplate(ItemTemplate other)
        {
            this.ID = other.ID;
            this.Name = other.Name;
            this.Description = other.Description;
            this.UseType = other.UseType;
            this.MinLevel = other.MinLevel;
            this.MaxLevel = other.MaxLevel;
            this.MinExperience = other.MinExperience;
            this.MaxExperience = other.MaxExperience;
            this.BaseStats = new AttributeSet() + other.BaseStats;
            this.WeaponDelay = other.WeaponDelay;
            this.WeaponDamage = other.WeaponDamage;
            this.Slot = other.Slot;
            this.Type = other.Type;
            this.GraphicEquipped = other.GraphicEquipped;
            this.GraphicTile = other.GraphicTile;
            this.GraphicFile = other.GraphicFile;
            this.GraphicR = other.GraphicR;
            this.GraphicG = other.GraphicG;
            this.GraphicB = other.GraphicB;
            this.GraphicA = other.GraphicA;
            this.Value = other.Value;
            this.IsLore = other.IsLore;
            this.IsBindOnPickup = other.IsBindOnPickup;
            this.IsBindOnEquip = other.IsBindOnEquip;
            this.IsEvent = other.IsEvent;
            this.ClassRestrictions = other.ClassRestrictions;
            this.StackSize = other.StackSize;
            this.BodyState = other.BodyState;
            this.SpellEffectID = other.SpellEffectID;
            this.SpellEffect = other.SpellEffect;
            this.SpellEffectChance = other.SpellEffectChance;
            this.LearnSpellID = other.LearnSpellID;
            this.Credits = other.Credits;
            this.Script = other.Script;
            this.ScriptParams = other.ScriptParams;
        }
```

**Step 4: Add the registration methods**

In `Goose/ItemHandler.cs`, after `GetTemplate` (`:195`):

```csharp
        /// <summary>Registers a generated template. Mirrors NPCHandler.AddTemplate
        /// (NPCHandler.cs:231) and SpellHandler.AddSpell. Overwrites silently, so callers
        /// generating ids must check GetTemplate first.</summary>
        public void AddTemplate(ItemTemplate template)
        {
            this.templates[template.ID] = template;
        }

        /// <summary>Registers a generated title. A modifier with Chance 0 can never be
        /// selected by RollModifier (its range is empty), so script-owned modifiers
        /// register at 0 and are applied explicitly.</summary>
        public void AddTitle(ItemModifier title)
        {
            this.titles[title.Id] = title;
        }

        public void AddSurname(ItemModifier surname)
        {
            this.surnames[surname.Id] = surname;
        }

        public ItemModifier GetTitle(int id)
        {
            return this.titles.TryGetValue(id, out ItemModifier title) ? title : null;
        }

        public ItemModifier GetSurname(int id)
        {
            return this.surnames.TryGetValue(id, out ItemModifier surname) ? surname : null;
        }
```

**Step 5: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~ItemHandlerRegistrationTests"`
Expected: 4 passing.

**Step 6: Run the whole suite** — the copy constructor changes `ItemTemplate`'s construction surface.

Run: `dotnet test`
Expected: 346 passing, 0 failed.

**Step 7: Commit**

```bash
git add Goose/ItemTemplate.cs Goose/ItemHandler.cs Goose.Tests/ItemHandlerRegistrationTests.cs
git commit -m "Add ItemTemplate copy constructor and ItemHandler registration methods"
```

---

## Task 2: Script hooks for pickup and modifier rolls

**Files:**
- Modify: `Goose/Scripting/IItemScript.cs`
- Modify: `Goose/Scripting/BaseItemScript.cs`
- Modify: `Goose/ItemHandler.cs:242` (`RollTitleAndSurname`)
- Modify: `Goose/Events/PickupItemEvent.cs:88`
- Test: `Goose.Tests/ItemScriptHookTests.cs` (create)

**Step 1: Write the failing test**

```csharp
using Goose.Scripting;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class ItemScriptHookTests
{
    private sealed class SpyScript : BaseItemScript
    {
        public bool Suppress;
        public int RollCalls;

        public override bool OnRollModifiersEvent(Item item, GameWorld world)
        {
            this.RollCalls++;
            return this.Suppress;
        }
    }

    private static (GameWorld World, Item Item, SpyScript Spy) Arrange()
    {
        var world = new GameWorld(null);
        var spy = new SpyScript();
        var template = new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "",
            UseType = ItemTemplate.UseTypes.Weapon, Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
            Script = ScriptStub.For<IItemScript>(spy),
        };
        world.ItemHandler.AddTemplate(template);

        var item = new Item();
        item.LoadFromTemplate(template);
        return (world, item, spy);
    }

    [Fact]
    public void Roll_hook_runs_before_the_use_type_filter()
    {
        var (world, item, spy) = Arrange();
        // A Scroll would be filtered out by RollTitleAndSurname's early return - the hook
        // must still see it, because dimension tomes need CanPickup and the upgrade rule.
        item.Template.UseType = ItemTemplate.UseTypes.Scroll;

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.Equal(1, spy.RollCalls);
    }

    [Fact]
    public void Returning_true_suppresses_the_native_rolls()
    {
        var (world, item, spy) = Arrange();
        spy.Suppress = true;
        world.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.False(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal("Sword", item.Name);
    }

    [Fact]
    public void Returning_false_leaves_the_native_rolls_running()
    {
        var (world, item, spy) = Arrange();
        spy.Suppress = false;
        world.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.True(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal("Sword of the Bear", item.Name);
    }

    [Fact]
    public void An_item_with_no_script_rolls_natively()
    {
        var world = new GameWorld(null);
        var template = new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "",
            UseType = ItemTemplate.UseTypes.Weapon, Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
        };
        var item = new Item();
        item.LoadFromTemplate(template);

        var exception = Record.Exception(() => world.ItemHandler.RollTitleAndSurname(item, world));

        Assert.Null(exception);
    }

    [Fact]
    public void CanPickup_defaults_to_allowing()
    {
        var (world, item, _) = Arrange();
        Assert.Null(new BaseItemScript().CanPickup(new Player(0), item, world));
    }
}
```

`ScriptStub` does not exist yet — add it in Step 3.

**Step 2: Run it and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~ItemScriptHookTests"`
Expected: compile errors — no `OnRollModifiersEvent`, no `CanPickup`, no `ScriptStub`.

**Step 3: Add the test helper**

`Script<T>` compiles a file in its constructor (`Goose/Scripting/Script.cs:26`), so tests cannot construct one around an in-memory object. Add an uninitialised-instance helper.

Create `Goose.Tests/Fixtures/ScriptStub.cs`:

```csharp
using System.Runtime.CompilerServices;
using Goose.Scripting;

namespace Goose.Tests;

/// <summary>Wraps an in-memory script object in a Script&lt;T&gt; without touching disk.
/// Script&lt;T&gt;'s constructor compiles a file (Script.cs:26), and Object has a private
/// setter (Script.cs:17), so the instance is allocated uninitialised and the backing
/// property is set by reflection.</summary>
public static class ScriptStub
{
    public static Script<T> For<T>(T instance)
    {
        var script = (Script<T>)RuntimeHelpers.GetUninitializedObject(typeof(Script<T>));
        typeof(Script<T>).GetProperty(nameof(Script<T>.Object))!
            .SetValue(script, instance);
        return script;
    }
}
```

If `Object`'s private setter is not reachable through `SetValue`, set the compiler-generated backing field instead:
`typeof(Script<T>).GetField("<Object>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(script, instance);`

**Step 4: Add the interface members**

`Goose/Scripting/IItemScript.cs`:

```csharp
    public interface IItemScript
    {
        void OnCreateEvent(Item item, GameWorld world);

        bool OnUseConsumableEvent(Player player, Item item, GameWorld world);

        void OnMeleeEvent(Player player, Item item, GameWorld world);

        /// <summary>Return a refusal message to block picking this item up, or null to
        /// allow. Consulted by PickupItemEvent. Mirrors IMapScript.CanPlayerJoin.</summary>
        string CanPickup(Player player, Item item, GameWorld world);

        /// <summary>Return true to suppress the native title/surname rolls, having done
        /// whatever rolling this item needs. Consulted by ItemHandler.RollTitleAndSurname
        /// before its use-type filter.</summary>
        bool OnRollModifiersEvent(Item item, GameWorld world);
    }
```

`Goose/Scripting/BaseItemScript.cs` — add beside the existing virtuals:

```csharp
        public virtual string CanPickup(Player player, Item item, GameWorld world)
        {
            return null;
        }

        public virtual bool OnRollModifiersEvent(Item item, GameWorld world)
        {
            return false;
        }
```

**Step 5: Wire the roll call site**

`Goose/ItemHandler.cs`, at the very top of `RollTitleAndSurname` — **above** the use-type early return, so scrolls reach the script:

```csharp
        public void RollTitleAndSurname(Item item, GameWorld world)
        {
            // Above the use-type filter deliberately: a script-owned item (dimension tomes)
            // must be able to claim the roll even when nothing native would apply to it.
            if (item.Script != null)
            {
                try
                {
                    if (item.Script.Object.OnRollModifiersEvent(item, world)) return;
                }
                catch (Exception e)
                {
                    log.Error(e, "Exception in OnRollModifiersEvent for template {templateId}", item.TemplateID);
                }
            }

            if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
                return;
            // ... existing body unchanged
```

`ItemHandler` has no logger yet. Add one at the top of the class, matching `ItemModifier.cs:10`:

```csharp
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
```

A throwing roll script must not silently produce an unrolled item, so this logs rather than swallowing — unlike `AddAndAssignId` (`:203`), whose empty catch is pre-existing and out of scope.

**Step 6: Wire the pickup call site**

`Goose/Events/PickupItemEvent.cs`, in the non-gold branch, before the `Inventory.AddItem` call (`:90`):

```csharp
                    if (tile.ItemSlot.Item.IsLore && this.Player.HasItem(tile.ItemSlot.Item.Template.ID)) return;

                    var refusal = tile.ItemSlot.Item.Script?.Object.CanPickup(this.Player, tile.ItemSlot.Item, world);
                    if (refusal != null)
                    {
                        world.Send(this.Player, P.ServerMessage(refusal));
                        return;
                    }

                    if (this.Player.Inventory.AddItem(tile.ItemSlot.Item, tile.ItemSlot.Stack, world))
```

**Step 7: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~ItemScriptHookTests"`
Expected: 5 passing.

**Step 8: Run the whole suite** — `IItemScript` gained members, so any test double implementing it directly breaks.

Run: `dotnet test`
Expected: all passing. If a test class implements `IItemScript` directly rather than deriving from `BaseItemScript`, change it to derive from `BaseItemScript`.

**Step 9: Commit**

```bash
git add Goose/Scripting/IItemScript.cs Goose/Scripting/BaseItemScript.cs Goose/ItemHandler.cs \
        Goose/Events/PickupItemEvent.cs Goose.Tests/ItemScriptHookTests.cs Goose.Tests/Fixtures/ScriptStub.cs
git commit -m "Add CanPickup and OnRollModifiersEvent item script hooks"
```

---

## Task 3: Clone equipment templates

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Modify: `Goose.Tests/Fixtures/GlobalScriptFixture.cs` (add `AddBaseItemTemplate`)
- Test: `Goose.Tests/DimensionItemTemplateTests.cs` (create)

**Step 1: Add the fixture helper**

In `GlobalScriptFixture`, beside `AddBaseSpell`:

```csharp
    /// <summary>Registers a base item template. Real ones come from item_templates
    /// (ItemHandler.cs:56); the clone path only reads the object.</summary>
    public ItemTemplate AddBaseItemTemplate(int id, string name, ItemTemplate.UseTypes useType,
                                            Action<ItemTemplate> configure = null)
    {
        var template = new ItemTemplate
        {
            ID = id, Name = name, Description = "A " + name, UseType = useType,
            Slot = ItemTemplate.ItemSlots.OneHanded, BaseStats = new AttributeSet(),
            GraphicR = 255, GraphicG = 255, GraphicB = 255, GraphicA = 100,
            StackSize = 1, ScriptParams = "",
        };
        configure?.Invoke(template);
        World.ItemHandler.AddTemplate(template);
        return template;
    }
```

**Step 2: Write the failing test**

```csharp
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionItemTemplateTests
{
    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Clones_equipment_once_per_dimension_with_prefix_and_recolour()
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.GraphicR = 200; t.GraphicG = 100; t.GraphicB = 20; t.GraphicA = 100; t.Value = 500; }));

        for (int dim = 1; dim <= 6; dim++)
            Assert.NotNull(fixture.World.ItemHandler.GetTemplate(50 + 100000 * dim));

        var dim3 = fixture.World.ItemHandler.GetTemplate(50 + 100000 * 3);
        Assert.Equal("Supreme Sword", dim3.Name);              // Item.java:416
        Assert.Equal("Abyss (3) A Sword", dim3.Description);   // Item.java:429
        Assert.Equal(110, dim3.GraphicR);                      // 200 - 30*3
        Assert.Equal(10, dim3.GraphicG);                       // 100 - 30*3
        Assert.Equal(0, dim3.GraphicB);                        // clamped at 0
        Assert.Equal(190, dim3.GraphicA);                      // 100 + 30*3
        Assert.Equal(500 * 27, dim3.Value);                    // base * 3^dim

        var dim6 = fixture.World.ItemHandler.GetTemplate(50 + 100000 * 6);
        Assert.Equal("Godly Sword", dim6.Name);
        Assert.Equal(200, dim6.GraphicA);                      // clamped at 200
    }

    [Fact]
    public void Clears_bind_and_lore_flags_on_clones()
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.IsLore = true; t.IsBindOnPickup = true; t.IsBindOnEquip = true; }));

        var dim1 = fixture.World.ItemHandler.GetTemplate(100050);
        Assert.False(dim1.IsLore);          // Item.java:225-260
        Assert.False(dim1.IsBindOnPickup);
        Assert.False(dim1.IsBindOnEquip);

        // The base template is untouched.
        Assert.True(fixture.World.ItemHandler.GetTemplate(50).IsLore);
    }

    [Fact]
    public void Does_not_clone_consumables_money_or_no_use_items()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseItemTemplate(60, "Potion", ItemTemplate.UseTypes.OneTime);
            f.AddBaseItemTemplate(61, "Gold", ItemTemplate.UseTypes.Money);
            f.AddBaseItemTemplate(62, "Quest Token", ItemTemplate.UseTypes.NoUse);
        });

        Assert.Null(fixture.World.ItemHandler.GetTemplate(100060));
        Assert.Null(fixture.World.ItemHandler.GetTemplate(100061));
        Assert.Null(fixture.World.ItemHandler.GetTemplate(100062));
    }

    [Theory]
    [InlineData(10_000_000, 0, 0, 1.0)]     // Value >= 10M
    [InlineData(0, 500, 0, 0.75)]           // MinExperience > 0
    [InlineData(0, 0, 50, 0.5)]             // MinLevel == 50
    [InlineData(0, 0, 20, 0.25)]            // everything else
    public void Scales_stats_by_dimension_and_tier(long value, long minExp, int minLevel, double tier)
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.Value = value; t.MinExperience = minExp; t.MinLevel = minLevel;
                   t.BaseStats = new AttributeSet { AC = 10, Strength = 20, HP = 100 }; }));

        var dim2 = fixture.World.ItemHandler.GetTemplate(50 + 100000 * 2);

        // AttributeSet.java:421 - a1.AC * (0.5*dim) + 10*dim*tier
        Assert.Equal(10 + (int)(10 * 1.0 + 10 * 2 * tier), dim2.BaseStats.AC);
        // AttributeSet.java:442 - a1.Strength * (0.5*dim) + 100*dim*tier
        Assert.Equal(20 + (int)(20 * 1.0 + 100 * 2 * tier), dim2.BaseStats.Strength);
        // AttributeSet.java:429 - a1.HP * dim + (10*dim)^4 * tier
        Assert.Equal(100 + (long)(100 * 2 + Math.Pow(20, 4) * tier), dim2.BaseStats.HP);
    }

    [Fact]
    public void Ports_the_melee_damage_truncation_faithfully()
    {
        using var fixture = Run(f => f.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.MinLevel = 20; t.BaseStats = new AttributeSet { MeleeDamage = 0.5m }; }));

        var dim2 = fixture.World.ItemHandler.GetTemplate(100050 + 100000);

        // AttributeSet.java:433 casts the whole term to int, so 0.5*2 = 1.0 survives but
        // any sub-1.0 product is truncated away. Tier 0.25, dim 2 -> (int)(1.0 + 10*2*0.25) = 6.
        Assert.Equal(0.5m + 6m, dim2.BaseStats.MeleeDamage);
    }

    [Fact]
    public void Refuses_to_overwrite_an_existing_template_id()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon);
        fixture.AddBaseItemTemplate(100050, "Impostor", ItemTemplate.UseTypes.Weapon);

        var script = fixture.CompileShipped();

        var exception = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));
        Assert.Contains("100050", exception.Message);
    }
}
```

Note the id arithmetic in `Refuses_to_overwrite`: the impostor is registered at a generated id, and the clone loop only treats `ID < Offset` as base data, so it is never itself cloned — it just occupies the slot dimension 1 wants.

**Step 3: Run it and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~DimensionItemTemplateTests"`
Expected: FAIL — `GetTemplate(100050)` returns null; nothing clones items yet.

**Step 4: Implement in `Dimensions.csx`**

Add `CloneItemTemplates(world);` to `OnLoaded` as the **first** call, before `CloneTemplates(world)` — Task 7 repoints NPC drop tables and needs the item clones to exist:

```csharp
        CloneItemTemplates(world);
        CloneTemplates(world);
        RewireAllies(world);
        // ... rest unchanged
```

Then add the methods:

```csharp
    /// <summary>Abyss name prefixes, Item.java:408-427.</summary>
    private static readonly string[] DimensionPrefixes =
    {
        "", "Powerful ", "Super Powerful ", "Supreme ", "Omnipotent ", "Almighty ", "Godly ",
    };

    /// <summary>Equipment and spell tomes get a copy per dimension. Consumables never scale
    /// in abyss (Item.java:404); money and NoUse items have nothing to scale.</summary>
    private bool ShouldClone(ItemTemplate t)
    {
        return t.UseType == ItemTemplate.UseTypes.Armor
            || t.UseType == ItemTemplate.UseTypes.Weapon
            || (t.UseType == ItemTemplate.UseTypes.Scroll && t.LearnSpellID > 0);
    }

    private void CloneItemTemplates(GameWorld world)
    {
        // Snapshot first: AddTemplate mutates the dictionary GetTemplates() enumerates
        // (ItemHandler.cs:42 hands back the live values collection).
        var baseTemplates = world.ItemHandler.GetTemplates()
            .Where(t => t.ID < Offset && ShouldClone(t)).ToList();

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseTemplates)
            {
                int id = basic.ID + Offset * dim;

                // AddTemplate overwrites silently, so a collision would quietly replace a
                // real item and change what every stored Item with that id resolves to.
                if (world.ItemHandler.GetTemplate(id) != null)
                    throw new Exception($"Dimension item template id {id} (base {basic.ID}, dim {dim}) "
                                        + "already exists. Offset is too small for this data set.");

                world.ItemHandler.AddTemplate(ScaleItemTemplate(world, basic, dim));
            }
        }
    }

    private ItemTemplate ScaleItemTemplate(GameWorld world, ItemTemplate basic, int dim)
    {
        var clone = new ItemTemplate(basic)
        {
            ID = basic.ID + Offset * dim,
            Name = DimensionPrefixes[dim] + basic.Name,
            Description = "Abyss (" + dim + ") " + basic.Description,

            // Item.java:441-444
            GraphicR = Math.Max(basic.GraphicR - 30 * dim, 0),
            GraphicG = Math.Max(basic.GraphicG - 30 * dim, 0),
            GraphicB = Math.Max(basic.GraphicB - 30 * dim, 0),
            GraphicA = Math.Min(basic.GraphicA + 30 * dim, 200),

            // Item.java:445. This is the spirit price; until Part 5's vendor overrides land
            // it is also a gold price, which is a known and accepted limitation.
            Value = (long)(basic.Value * Math.Pow(3, dim)),

            // Item.java:225-260 - dimension gear is freely tradeable.
            IsLore = false,
            IsBindOnPickup = false,
            IsBindOnEquip = false,
        };

        clone.BaseStats += DimensionStats(basic, dim);
        return clone;
    }

    /// <summary>AttributeSet.java:376, with itemType 0 - the flat per-dimension bonus only.
    /// The six suffix-specific terms live in DimensionSurname.csx, applied at roll time.
    ///
    /// Baking this into the template rather than adding it per item is equivalent: abyss
    /// computes (template + item + dimensionDefault) * StatMultiplier (Item.java:459), and
    /// goose computes (template + item) * StatMultiplier (Item.cs:247), so folding it into
    /// the template leaves Legendary/Stunted multiplying the same total.</summary>
    private AttributeSet DimensionStats(ItemTemplate basic, int dim)
    {
        var a1 = basic.BaseStats;
        double tier = Tier(basic);
        double half = 0.5 * dim;

        return new AttributeSet
        {
            AC = (int)(a1.AC * half + 10 * dim * tier),
            AirResist = (int)(a1.AirResist * half + 10 * dim * tier),
            EarthResist = (int)(a1.EarthResist * half + 10 * dim * tier),
            FireResist = (int)(a1.FireResist * half + 10 * dim * tier),
            WaterResist = (int)(a1.WaterResist * half + 10 * dim * tier),
            SpiritResist = (int)(a1.SpiritResist * half + 10 * dim * tier),
            Dexterity = (int)(a1.Dexterity * half + 15 * dim * tier),
            Stamina = (int)(a1.Stamina * half + 100 * dim * tier),
            Intelligence = (int)(a1.Intelligence * half + 100 * dim * tier),
            Strength = (int)(a1.Strength * half + 100 * dim * tier),

            HP = (long)(a1.HP * dim + Math.Pow(10 * dim, 4) * tier),
            MP = (long)(a1.MP * dim + Math.Pow(10 * dim, 4) * tier),

            DamageReduction = a1.DamageReduction * (decimal)half,
            Haste = a1.Haste * (decimal)half,
            SpellCrit = a1.SpellCrit * (decimal)half,
            SpellDamage = a1.SpellDamage * (decimal)half,
            HPPercentRegen = a1.HPPercentRegen * (decimal)half,
            MPPercentRegen = a1.MPPercentRegen * (decimal)half,
            HPStaticRegen = (int)(a1.HPStaticRegen * half),
            MPStaticRegen = (int)(a1.MPStaticRegen * half),

            // AttributeSet.java:433 casts the whole term to int. Ported faithfully, cast
            // included: the flat 10*dim*tier term dominates, and any base MeleeDamage
            // product below 1.0 truncates to nothing. MeleeDamage is a fraction on both
            // servers - damage *= (1 + MeleeDamage) at Player.java:1809 and Player.cs:1616 -
            // so this is a very large bonus by design. User decision, 2026-08-10.
            MeleeDamage = (int)((double)a1.MeleeDamage * dim + 10 * dim * tier),
        };
    }

    /// <summary>AttributeSet.java:405-419. Abyss's top tier (1.5) keys off an SP-priced
    /// template; goose has no SP value, so that tier has no equivalent and is dropped.
    /// Computed from the BASE template - the clone's value is already scaled by 3^dim and
    /// would put every clone in the top tier.</summary>
    private double Tier(ItemTemplate basic)
    {
        if (basic.Value >= 10000000) return 1.0;
        if (basic.MinExperience > 0) return 0.75;
        if (basic.MinLevel == 50) return 0.5;
        return 0.25;
    }
```

**Step 5: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~DimensionItemTemplateTests"`
Expected: 9 passing (the `[Theory]` contributes 4).

**Step 6: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/Fixtures/GlobalScriptFixture.cs \
        Goose.Tests/DimensionItemTemplateTests.cs
git commit -m "Dimensions: clone equipment templates with abyss scaling"
```

---

## Task 4: Spell tomes

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionItemTemplateTests.cs` (extend)

**Step 1: Write the failing test**

```csharp
    [Fact]
    public void Clones_tomes_as_consumables_pointing_at_the_dimension_spell()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var dim2 = fixture.World.ItemHandler.GetTemplate(70 + 100000 * 2);
        Assert.NotNull(dim2);
        // OneTime, not Scroll: Inventory.cs:277 learns scrolls with no script hook, so the
        // upgrade rule needs the consumable path (Inventory.cs:423).
        Assert.Equal(ItemTemplate.UseTypes.OneTime, dim2.UseType);
        Assert.Equal(91 + 100000 * 2, dim2.LearnSpellID);
        Assert.Equal("Super Powerful Tome of Firestorm", dim2.Name);
    }

    [Fact]
    public void Leaves_a_tome_alone_when_its_spell_was_never_cloned()
    {
        using var fixture = Run(f =>
            // No spell 91 registered, so Part 3's spell pass produces no clone for it.
            f.AddBaseItemTemplate(70, "Tome of Nothing", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91));

        var dim2 = fixture.World.ItemHandler.GetTemplate(70 + 100000 * 2);
        Assert.NotNull(dim2);
        // Pointing at a spell that does not exist would make the tome silently unusable
        // (Spellbook.LearnSpell returns false at Spellbook.cs:203). Keep the base spell.
        Assert.Equal(91, dim2.LearnSpellID);
        Assert.Equal(ItemTemplate.UseTypes.Scroll, dim2.UseType);
    }

    [Fact]
    public void Does_not_clone_scrolls_that_teach_nothing()
    {
        using var fixture = Run(f =>
            f.AddBaseItemTemplate(71, "Blank Scroll", ItemTemplate.UseTypes.Scroll));

        Assert.Null(fixture.World.ItemHandler.GetTemplate(100071));
    }
```

**Step 2: Run and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~DimensionItemTemplateTests"`
Expected: FAIL — the tome clone still has `UseType = Scroll` and `LearnSpellID = 91`.

**Step 3: Implement**

`CloneItemTemplates` must run **after** the spell passes so it can ask whether a dimension spell exists. Reorder `OnLoaded`:

```csharp
        CloneTemplates(world);
        RewireAllies(world);
        CloneMaps(world);
        RewireWarps(world);
        CloneSpawns(world);
        CreateUnlockChain(world);

        PreflightSpellIds(world);
        CloneSpellEffects(world);
        RewireSpellEffects(world);
        CloneSpells(world);
        RewriteTeleportEffects(world);

        // After the spell passes: tome clones point at dimension spells, which must exist
        // to be pointed at. Before RepointDrops (Task 7), which needs the item clones.
        CloneItemTemplates(world);
        RepointDrops(world);
```

Then in `ScaleItemTemplate`, after the object initialiser:

```csharp
        // Spell tomes: teach the dimension's copy of the spell, and become consumables so
        // DimensionItem.csx can implement the upgrade rule. Inventory.cs:277 learns Scroll
        // items directly with no script hook; Inventory.cs:423 gives OneTime items one.
        //
        // A spell with no dimension clone (PreflightSpellIds can skip ids) keeps its base
        // id and stays a plain Scroll - a tome pointing at a nonexistent spell would fail
        // silently at Spellbook.cs:203.
        if (basic.UseType == ItemTemplate.UseTypes.Scroll
            && world.SpellHandler.GetSpell(basic.LearnSpellID + Offset * dim) != null)
        {
            clone.UseType = ItemTemplate.UseTypes.OneTime;
            clone.LearnSpellID = basic.LearnSpellID + Offset * dim;
        }
```

**Step 4: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~DimensionItemTemplateTests"`
Expected: 12 passing.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionItemTemplateTests.cs
git commit -m "Dimensions: clone spell tomes as consumables teaching dimension spells"
```

---

## Task 5: Register the suffix and rarity modifiers

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Item/DimensionSurname.csx`
- Create: `Goose/Data/Illutia/Scripts/Item/DimensionRarity.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Modify: `Goose.Tests/Goose.Tests.csproj`, `Goose.Tests/Fixtures/GlobalScriptFixture.cs`
- Test: `Goose.Tests/DimensionModifierTests.cs` (create)

**Step 1: Register the new scripts with the test harness**

`Goose.Tests/Goose.Tests.csproj`, beside the existing four:

```xml
    <None Include="../Goose/Data/Illutia/Scripts/Item/DimensionItem.csx"
          Link="DimensionScripts/DimensionItem.csx" CopyToOutputDirectory="PreserveNewest" />
    <None Include="../Goose/Data/Illutia/Scripts/Item/DimensionSurname.csx"
          Link="DimensionScripts/DimensionSurname.csx" CopyToOutputDirectory="PreserveNewest" />
    <None Include="../Goose/Data/Illutia/Scripts/Item/DimensionRarity.csx"
          Link="DimensionScripts/DimensionRarity.csx" CopyToOutputDirectory="PreserveNewest" />
```

`GlobalScriptFixture.ShippedScripts` — add all three (the comment on that field already says both lists move together), and add `"Item"` to the directory list in the constructor:

```csharp
        ("DimensionItem.csx",        "Scripts/Item/DimensionItem.csx"),
        ("DimensionSurname.csx",     "Scripts/Item/DimensionSurname.csx"),
        ("DimensionRarity.csx",      "Scripts/Item/DimensionRarity.csx"),
```

```csharp
        foreach (var dir in new[] { "Global", "Map", "Quest", "Spell", "Item" })
```

`DimensionItem.csx` arrives in Task 6; create it as an empty stub deriving from `BaseItemScript` now so the fixture can install it, and fill it in there.

**Step 2: Write the failing test**

```csharp
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionModifierTests
{
    private static GlobalScriptFixture Run()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => t.MinLevel = 50);   // tier 0.5
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Registers_six_surnames_and_two_titles_that_can_never_roll_natively()
    {
        using var fixture = Run();

        Assert.Equal(6, fixture.World.ItemHandler.SurnameCount);
        Assert.Equal(2, fixture.World.ItemHandler.TitleCount);

        // Chance 0 makes RollModifier's range empty (ItemHandler.cs:272-277), so these can
        // only ever be applied explicitly by the dimension script.
        for (int i = 0; i < 6; i++)
            Assert.Equal(0, fixture.World.ItemHandler.GetSurname(900000 + i).Chance);

        Assert.Equal("of Vita Regen", fixture.World.ItemHandler.GetSurname(900000).Name);
        Assert.Equal("of Speed", fixture.World.ItemHandler.GetSurname(900005).Name);
        Assert.Equal("Legendary", fixture.World.ItemHandler.GetTitle(900100).Name);
        Assert.Equal("Stunted", fixture.World.ItemHandler.GetTitle(900101).Name);
    }

    [Theory]
    [InlineData(900002, "SpellCrit")]
    [InlineData(900003, "SpellDamage")]
    [InlineData(900004, "DamageReduction")]
    [InlineData(900005, "Haste")]
    public void Percentage_suffixes_add_four_percent_per_dimension_and_tier(int surnameId, string stat)
    {
        using var fixture = Run();
        var item = ItemOfDimension(fixture, dim: 3);   // tier 0.5

        fixture.World.ItemHandler.GetSurname(surnameId).ApplyStats(item, fixture.World);

        // AttributeSet.java:422,428,437,438 - 0.04 * dim * tier
        var expected = 0.04m * 3 * 0.5m;
        Assert.Equal(expected, StatOf(item.BaseStats, stat));
    }

    [Fact]
    public void Vita_regen_adds_both_regen_stats()
    {
        using var fixture = Run();
        var item = ItemOfDimension(fixture, dim: 3);

        fixture.World.ItemHandler.GetSurname(900000).ApplyStats(item, fixture.World);

        Assert.Equal(0.015m * 3 * 0.5m, item.BaseStats.HPPercentRegen);   // AttributeSet.java:430
        Assert.Equal((int)(1500 * 3 * 0.5), item.BaseStats.HPStaticRegen); // AttributeSet.java:431
        Assert.Equal(0, item.BaseStats.MPStaticRegen);
    }

    [Fact]
    public void Rarity_titles_only_touch_the_stat_multiplier()
    {
        using var fixture = Run();
        var legendary = ItemOfDimension(fixture, dim: 1);
        var stunted = ItemOfDimension(fixture, dim: 1);

        fixture.World.ItemHandler.GetTitle(900100).ApplyStats(legendary, fixture.World);
        fixture.World.ItemHandler.GetTitle(900101).ApplyStats(stunted, fixture.World);

        Assert.Equal(1.25, legendary.StatMultiplier);   // Item.java:394
        Assert.Equal(0.5, stunted.StatMultiplier);      // Item.java:398
    }

    private static Item ItemOfDimension(GlobalScriptFixture fixture, int dim)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(50 + 100000 * dim));
        return item;
    }

    private static decimal StatOf(AttributeSet stats, string name) => name switch
    {
        "SpellCrit" => stats.SpellCrit,
        "SpellDamage" => stats.SpellDamage,
        "DamageReduction" => stats.DamageReduction,
        "Haste" => stats.Haste,
        _ => throw new ArgumentException(name),
    };
}
```

**Step 3: Run and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~DimensionModifierTests"`
Expected: FAIL — `SurnameCount` is 0.

**Step 4: Write `DimensionSurname.csx`**

```csharp
using System;
using Goose;
using Goose.Scripting;

/// <summary>The six abyss suffixes. Each applies the suffix-specific terms from
/// AttributeSet.dimensionDefault (AttributeSet.java:376) - the flat part is already baked
/// into the dimension template by Dimensions.csx.
///
/// ScriptParams carries the suffix index 0-5, matching the registration order in
/// Dimensions.csx. The generic ItemModifierScript.csx cannot express this: its operations
/// are fixed JSON values with no access to the item's dimension.</summary>
public class DimensionSurname : BaseItemModifierScript
{
    /// <summary>Must match Dimensions.csx's Offset. Scripts compile independently.</summary>
    private const int Offset = 100000;

    public override void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world)
    {
        int dim = item.TemplateID / Offset;
        if (dim <= 0) return;

        double tier = Tier(world.ItemHandler.GetTemplate(item.TemplateID % Offset));
        decimal scale = (decimal)(dim * tier);
        int index = int.Parse(modifier.ScriptParams);

        switch (index)
        {
            case 0:   // of Vita Regen - AttributeSet.java:430,431
                item.BaseStats.HPPercentRegen += 0.015m * scale;
                item.BaseStats.HPStaticRegen += (int)(1500 * dim * tier);
                break;
            case 1:   // of Mana Regen - AttributeSet.java:435,436
                item.BaseStats.MPPercentRegen += 0.015m * scale;
                item.BaseStats.MPStaticRegen += (int)(1500 * dim * tier);
                break;
            case 2:   // of Criticality - AttributeSet.java:437
                item.BaseStats.SpellCrit += 0.04m * scale;
                break;
            case 3:   // of Spell Damage - AttributeSet.java:438
                item.BaseStats.SpellDamage += 0.04m * scale;
                break;
            case 4:   // of Reduction - AttributeSet.java:422
                item.BaseStats.DamageReduction += 0.04m * scale;
                break;
            case 5:   // of Speed - AttributeSet.java:428
                item.BaseStats.Haste += 0.04m * scale;
                break;
        }

        item.RefreshStats();
    }

    /// <summary>AttributeSet.java:405-419, on the base template. A missing base template
    /// (the feature was disabled and re-enabled around a data change) scores the lowest
    /// tier rather than throwing inside a roll.</summary>
    private double Tier(ItemTemplate basic)
    {
        if (basic == null) return 0.25;
        if (basic.Value >= 10000000) return 1.0;
        if (basic.MinExperience > 0) return 0.75;
        if (basic.MinLevel == 50) return 0.5;
        return 0.25;
    }
}

return typeof(DimensionSurname);
```

Confirm `BaseItemModifierScript` exists and its method signature matches, the way `ItemModifierScript.csx:44` uses it.

**Step 5: Write `DimensionRarity.csx`**

```csharp
using System;
using Goose;
using Goose.Scripting;

/// <summary>Legendary and Stunted, Item.java:391-401. ScriptParams carries the multiplier.
/// StatMultiplier scales the whole item including the baked dimension bonus, matching
/// abyss (Item.java:463).</summary>
public class DimensionRarity : BaseItemModifierScript
{
    public override void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world)
    {
        item.StatMultiplier *= double.Parse(modifier.ScriptParams);
        item.RefreshStats();
    }
}

return typeof(DimensionRarity);
```

**Step 6: Register them from `Dimensions.csx`**

Add `RegisterModifiers(world);` to `OnLoaded` immediately before `CloneItemTemplates(world);`, and:

```csharp
    /// <summary>Abyss suffix names, in the band order of Item.java:363-387.</summary>
    private static readonly string[] SurnameNames =
    {
        "of Vita Regen", "of Mana Regen", "of Criticality",
        "of Spell Damage", "of Reduction", "of Speed",
    };

    /// <summary>Registers the eight dimension modifiers. All at Chance 0: RollModifier
    /// (ItemHandler.cs:270) sizes each modifier's selection range as (int)(Chance * 100),
    /// so zero yields an empty range and these can never land on dimension-0 loot. The
    /// dimension script selects them explicitly by id.</summary>
    private void RegisterModifiers(GameWorld world)
    {
        var surnameScript = world.ScriptHandler.GetScript<IItemModifierScript>(
            "Scripts/Item/DimensionSurname.csx");

        for (int i = 0; i < SurnameNames.Length; i++)
        {
            world.ItemHandler.AddSurname(new ItemModifier
            {
                Id = SurnameIdBase + i,
                Name = SurnameNames[i],
                Chance = 0,
                Slot = ItemTemplate.ItemSlots.Misc,   // ModifierAppliesToItem treats Misc as "any slot"
                Script = surnameScript,
                ScriptParams = i.ToString(),
            });
        }

        var rarityScript = world.ScriptHandler.GetScript<IItemModifierScript>(
            "Scripts/Item/DimensionRarity.csx");

        world.ItemHandler.AddTitle(new ItemModifier
        {
            Id = TitleIdBase, Name = "Legendary", Chance = 0,
            Slot = ItemTemplate.ItemSlots.Misc,
            Script = rarityScript, ScriptParams = "1.25",
        });
        world.ItemHandler.AddTitle(new ItemModifier
        {
            Id = TitleIdBase + 1, Name = "Stunted", Chance = 0,
            Slot = ItemTemplate.ItemSlots.Misc,
            Script = rarityScript, ScriptParams = "0.5",
        });
    }
```

**Step 7: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~DimensionModifierTests"`
Expected: 8 passing.

**Step 8: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Item/ Goose/Data/Illutia/Scripts/Global/Dimensions.csx \
        Goose.Tests/Goose.Tests.csproj Goose.Tests/Fixtures/GlobalScriptFixture.cs \
        Goose.Tests/DimensionModifierTests.cs
git commit -m "Dimensions: register abyss suffix and rarity item modifiers"
```

---

## Task 6: `DimensionItem.csx`

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Item/DimensionItem.csx` (stub created in Task 5)
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (attach the script to clones)
- Test: `Goose.Tests/DimensionItemScriptTests.cs` (create)

**Step 1: Write the failing test**

```csharp
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionItemScriptTests
{
    private const string MaxDimension = "dimension.max";

    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange = null)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon, t => t.MinLevel = 50);
        arrange?.Invoke(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    private static Item ItemOf(GlobalScriptFixture fixture, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(templateId));
        return item;
    }

    [Fact]
    public void Every_clone_carries_the_dimension_script()
    {
        using var fixture = Run();
        var script = fixture.World.ItemHandler.GetTemplate(100050).Script;

        Assert.NotNull(script);
        // ScriptHandler caches by path (ScriptHandler.cs:24), so every clone shares one object.
        Assert.Same(script, fixture.World.ItemHandler.GetTemplate(600050).Script);
        // The base template is untouched.
        Assert.Null(fixture.World.ItemHandler.GetTemplate(50).Script);
    }

    [Fact]
    public void Rolls_a_suffix_on_roughly_forty_five_percent_of_items_in_even_bands()
    {
        using var fixture = Run();
        var counts = new Dictionary<string, int>();
        int suffixed = 0;

        for (int i = 0; i < 4000; i++)
        {
            var item = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(item, fixture.World);

            if (!item.HasProperty(ItemProperty.SurnameId)) continue;

            suffixed++;
            var name = fixture.World.ItemHandler.GetSurname(item.GetProperty<int>(ItemProperty.SurnameId)).Name;
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }

        // Item.java:359-388 - 45% total, six equal 7.5% bands. Wide bounds: this is a
        // distribution check, not an exact-value check.
        Assert.InRange(suffixed, 4000 * 0.38, 4000 * 0.52);
        Assert.Equal(6, counts.Count);
        foreach (var count in counts.Values)
            Assert.InRange(count, 4000 * 0.045, 4000 * 0.105);
    }

    [Fact]
    public void Rolls_rarity_titles_at_two_percent_each()
    {
        using var fixture = Run();
        int legendary = 0, stunted = 0;

        for (int i = 0; i < 4000; i++)
        {
            var item = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(item, fixture.World);

            if (!item.HasProperty(ItemProperty.TitleId)) continue;
            if (item.GetProperty<int>(ItemProperty.TitleId) == 900100) legendary++; else stunted++;
        }

        Assert.InRange(legendary, 4000 * 0.008, 4000 * 0.035);   // Item.java:393
        Assert.InRange(stunted, 4000 * 0.008, 4000 * 0.035);     // Item.java:397
    }

    [Fact]
    public void Applies_the_rolled_modifier_to_the_items_name_and_stats()
    {
        using var fixture = Run();

        // Roll until a suffix lands - the roll is random by design.
        Item item = null;
        for (int i = 0; i < 200 && item == null; i++)
        {
            var candidate = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(candidate, fixture.World);
            if (candidate.HasProperty(ItemProperty.SurnameId)) item = candidate;
        }

        Assert.NotNull(item);
        var surname = fixture.World.ItemHandler.GetSurname(item.GetProperty<int>(ItemProperty.SurnameId));
        Assert.Equal("Supreme Sword " + surname.Name, item.Name);
        Assert.NotEqual(new AttributeSet(), item.BaseStats);
    }

    [Fact]
    public void Suppresses_the_native_rolls_on_dimension_items()
    {
        using var fixture = Run();
        fixture.World.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        for (int i = 0; i < 50; i++)
        {
            var item = ItemOf(fixture, 300050);
            fixture.World.ItemHandler.RollTitleAndSurname(item, fixture.World);
            Assert.DoesNotContain("of the Bear", item.Name);
        }
    }

    [Fact]
    public void Refuses_pickup_above_the_players_unlocked_dimension()
    {
        using var fixture = Run();
        var script = fixture.World.ItemHandler.GetTemplate(300050).Script.Object;
        var player = new Player(0);
        player.Properties[MaxDimension] = 2;

        Assert.NotNull(script.CanPickup(player, ItemOf(fixture, 300050), fixture.World));
        Assert.Null(script.CanPickup(player, ItemOf(fixture, 200050), fixture.World));
        Assert.Null(script.CanPickup(player, ItemOf(fixture, 100050), fixture.World));
    }

    [Fact]
    public void A_tome_upgrades_a_lower_dimension_spell_in_place()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var player = new Player(0) { Spellbook = null };
        player.Spellbook = new Spellbook(player);
        player.Spellbook.AddSpell(fixture.World.SpellHandler.GetSpell(100091), fixture.World);

        var tome = ItemOf(fixture, 300070);
        var consumed = tome.Script.Object.OnUseConsumableEvent(player, tome, fixture.World);

        Assert.True(consumed);
        Assert.Equal(1, CountSpells(player, 300091));
        Assert.Equal(0, CountSpells(player, 100091));
    }

    [Fact]
    public void A_tome_refuses_when_the_known_spell_is_equal_or_higher()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var player = new Player(0);
        player.Spellbook = new Spellbook(player);
        player.Spellbook.AddSpell(fixture.World.SpellHandler.GetSpell(500091), fixture.World);

        var tome = ItemOf(fixture, 300070);

        // false = do not consume. Inventory.cs:433 removes the item only when true.
        Assert.False(tome.Script.Object.OnUseConsumableEvent(player, tome, fixture.World));
        Assert.Equal(1, CountSpells(player, 500091));
    }

    [Fact]
    public void A_tome_teaches_an_unknown_spell_outright()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseSpellEffect(7, "Firestorm Effect");
            f.AddBaseSpell(91, "Firestorm", 7);
            f.AddBaseItemTemplate(70, "Tome of Firestorm", ItemTemplate.UseTypes.Scroll,
                t => t.LearnSpellID = 91);
        });

        var player = new Player(0);
        player.Spellbook = new Spellbook(player);

        var tome = ItemOf(fixture, 300070);

        Assert.True(tome.Script.Object.OnUseConsumableEvent(player, tome, fixture.World));
        Assert.Equal(1, CountSpells(player, 300091));
    }

    [Fact]
    public void Forwards_to_the_base_templates_script()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        // A base template whose script records the calls it receives.
        var inner = new RecordingScript();
        fixture.AddBaseItemTemplate(51, "Okonk Sword", ItemTemplate.UseTypes.Weapon,
            t => { t.Script = ScriptStub.For<IItemScript>(inner); t.ScriptParams = "inner-params"; });
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var item = ItemOf(fixture, 100051);
        item.Script.Object.OnMeleeEvent(new Player(0), item, fixture.World);

        Assert.Equal(1, inner.MeleeCalls);
        // LoadFromTemplate copies ScriptParams (Item.cs:176), so the inner script still
        // reads the params it was written against.
        Assert.Equal("inner-params", item.ScriptParams);
    }

    private sealed class RecordingScript : BaseItemScript
    {
        public int MeleeCalls;
        public override void OnMeleeEvent(Player player, Item item, GameWorld world) => this.MeleeCalls++;
    }

    private static int CountSpells(Player player, int spellId)
    {
        int found = 0;
        for (int slot = 1; slot <= GameWorld.Settings.SpellbookSize; slot++)
            if (player.Spellbook.GetSlot(slot)?.ID == spellId) found++;
        return found;
    }
}
```

**Step 2: Run and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~DimensionItemScriptTests"`
Expected: FAIL — clones have no script attached.

**Step 3: Write `DimensionItem.csx`**

```csharp
using System;
using Goose;
using Goose.Scripting;

/// <summary>Attached to every generated dimension item template by Dimensions.csx. One
/// shared, stateless instance serves all of them - ScriptHandler caches by path
/// (ScriptHandler.cs:24) - so the dimension is recovered from the item, never stored.
///
/// Also forwards every call to the base template's script, so a scripted base item
/// (OkonkIllusionSword.csx, ZombieLegIllusion.csx) keeps working in every dimension.</summary>
public class DimensionItem : BaseItemScript
{
    /// <summary>Must match Dimensions.csx. Scripts compile independently.</summary>
    private const int Offset = 100000;
    private const int SurnameIdBase = 900000;
    private const int TitleIdBase = 900100;
    private const string MaxDimensionProperty = "dimension.max";

    private int DimensionOf(Item item) => item.TemplateID / Offset;

    private IItemScript Inner(Item item, GameWorld world)
    {
        return world.ItemHandler.GetTemplate(item.TemplateID % Offset)?.Script?.Object;
    }

    /// <summary>The abyss roll, Item.java:359-401. Returns true unconditionally: a
    /// dimension item never takes goose's native title/surname roll on top.</summary>
    public override bool OnRollModifiersEvent(Item item, GameWorld world)
    {
        int dim = DimensionOf(item);
        if (dim <= 0) return false;

        if (item.UseType == ItemTemplate.UseTypes.Armor || item.UseType == ItemTemplate.UseTypes.Weapon)
        {
            // Six equal 7.5% bands over the top 45% of the roll (Item.java:363-387).
            double roll = world.Random.NextDouble();
            if (roll >= 0.55)
            {
                int index = Math.Min((int)((roll - 0.55) / 0.075), 5);
                Apply(world.ItemHandler.GetSurname(SurnameIdBase + index), item, world, prefix: false);
            }

            // Item.java:391-401 - 2% each, rolled independently of the suffix.
            double rarity = world.Random.NextDouble();
            if (rarity > 0.98) Apply(world.ItemHandler.GetTitle(TitleIdBase), item, world, prefix: true);
            else if (rarity > 0.96) Apply(world.ItemHandler.GetTitle(TitleIdBase + 1), item, world, prefix: true);
        }

        Inner(item, world)?.OnRollModifiersEvent(item, world);
        return true;
    }

    /// <summary>Mirrors ItemHandler.RollTitleAndSurname's own application (ItemHandler.cs:247-265):
    /// name, then the id property, then the modifier's stats.</summary>
    private void Apply(ItemModifier modifier, Item item, GameWorld world, bool prefix)
    {
        if (modifier == null) return;

        item.Name = prefix ? modifier.Name + " " + item.Name : item.Name + " " + modifier.Name;
        item.ItemProperties[prefix ? ItemProperty.TitleId : ItemProperty.SurnameId] = modifier.Id;
        modifier.ApplyStats(item, world);
    }

    public override string CanPickup(Player player, Item item, GameWorld world)
    {
        int dim = DimensionOf(item);
        if (dim > player.Properties.GetProperty<int>(MaxDimensionProperty, 0))
            return "The void keeps what you cannot carry. You have a maximum dimension of "
                   + player.Properties.GetProperty<int>(MaxDimensionProperty, 0) + ".";

        return Inner(item, world)?.CanPickup(player, item, world);
    }

    /// <summary>Dimension tomes. Returning false leaves the item in the inventory
    /// (Inventory.cs:433). A known copy of the same spell at a lower dimension is
    /// replaced in place rather than accumulating a slot per dimension.</summary>
    public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
    {
        var incoming = world.SpellHandler.GetSpell(item.LearnSpellID);
        if (incoming == null) return Inner(item, world)?.OnUseConsumableEvent(player, item, world) ?? true;

        int baseId = incoming.ID % Offset;
        for (int slot = 1; slot <= GameWorld.Settings.SpellbookSize; slot++)
        {
            var known = player.Spellbook.GetSlot(slot);
            if (known == null || known.ID % Offset != baseId) continue;

            if (known.ID / Offset >= incoming.ID / Offset)
            {
                world.Send(player, P.ServerMessage("You already know a spell of that power."));
                return false;
            }

            player.Spellbook.RemoveSpell(slot, world);
            break;
        }

        return player.Spellbook.AddSpell(incoming, world);
    }

    public override void OnCreateEvent(Item item, GameWorld world)
    {
        Inner(item, world)?.OnCreateEvent(item, world);
    }

    public override void OnMeleeEvent(Player player, Item item, GameWorld world)
    {
        Inner(item, world)?.OnMeleeEvent(player, item, world);
    }
}

return typeof(DimensionItem);
```

If `P.ServerMessage` is not reachable from a `.csx` (the imports at `Script.cs:37` include `Goose`, and `P` is in that namespace — check `Goose/Packets.cs`), use `world.Send(player, P.ServerMessage(...))` exactly as `DimensionMap.csx:56` sends its refusal.

**Step 4: Attach the script in `Dimensions.csx`**

In `CloneItemTemplates`, resolve the script once and pass it to `ScaleItemTemplate`:

```csharp
        var itemScript = world.ScriptHandler.GetScript<IItemScript>("Scripts/Item/DimensionItem.csx");
```

and in `ScaleItemTemplate`'s initialiser:

```csharp
            // Replaces the base script rather than composing with it - DimensionItem.csx
            // forwards to the base template's script itself, so nothing is lost.
            Script = itemScript,
```

**Step 5: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~DimensionItemScriptTests"`
Expected: 10 passing.

**Step 6: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Item/DimensionItem.csx Goose/Data/Illutia/Scripts/Global/Dimensions.csx \
        Goose.Tests/DimensionItemScriptTests.cs
git commit -m "Dimensions: item script for rolls, pickup gating, tome upgrades and delegation"
```

---

## Task 7: Repoint drop tables

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionDropTests.cs` (create)

**Step 1: Write the failing test**

```csharp
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionDropTests
{
    private static GlobalScriptFixture Run()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        var sword = fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon);
        var potion = fixture.AddBaseItemTemplate(60, "Potion", ItemTemplate.UseTypes.OneTime);

        var npc = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        npc.BaseStats = new AttributeSet { HP = 3704 };
        npc.Drops = new List<NPCDropInfo>
        {
            new NPCDropInfo { ItemTemplate = sword, DropRate = 0.1m, Stack = 1 },
            new NPCDropInfo { ItemTemplate = potion, DropRate = 0.5m, Stack = 3 },
        };
        fixture.World.NPCHandler.AddTemplate(npc);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Dimension_npcs_drop_dimension_equipment()
    {
        using var fixture = Run();
        var drops = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 4).Drops;

        var sword = drops.Single(d => d.ItemTemplate.Name.EndsWith("Sword"));
        Assert.Equal(50 + 100000 * 4, sword.ItemTemplate.ID);
        Assert.Equal(0.1m, sword.DropRate);   // rate and stack are carried across unchanged
        Assert.Equal(1, sword.Stack);
    }

    [Fact]
    public void Consumable_drops_stay_at_dimension_zero()
    {
        using var fixture = Run();
        var drops = fixture.World.NPCHandler.GetNPCTemplate(100162).Drops;

        Assert.Equal(60, drops.Single(d => d.ItemTemplate.Name == "Potion").ItemTemplate.ID);
    }

    [Fact]
    public void The_base_drop_table_is_left_alone()
    {
        using var fixture = Run();
        var drops = fixture.World.NPCHandler.GetNPCTemplate(162).Drops;

        // NPCTemplate's copy constructor shares NPCDropInfo instances (NPCTemplate.cs:251),
        // so repointing must allocate new ones or every dimension rewrites dimension 0.
        Assert.Equal(50, drops.Single(d => d.ItemTemplate.Name == "Sword").ItemTemplate.ID);
        Assert.Equal(60, drops.Single(d => d.ItemTemplate.Name == "Potion").ItemTemplate.ID);
    }

    [Fact]
    public void Each_dimension_gets_its_own_drop_entries()
    {
        using var fixture = Run();

        var dim1 = fixture.World.NPCHandler.GetNPCTemplate(100162).Drops
            .Single(d => d.ItemTemplate.Name.EndsWith("Sword"));
        var dim2 = fixture.World.NPCHandler.GetNPCTemplate(200162).Drops
            .Single(d => d.ItemTemplate.Name.EndsWith("Sword"));

        Assert.NotSame(dim1, dim2);
        Assert.Equal(100050, dim1.ItemTemplate.ID);
        Assert.Equal(200050, dim2.ItemTemplate.ID);
    }
}
```

**Step 2: Run and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~DimensionDropTests"`
Expected: FAIL — dimension drops still point at template 50.

**Step 3: Implement**

`RepointDrops(world);` is already in `OnLoaded` from Task 4's reorder. Add:

```csharp
    /// <summary>Points each dimension NPC's drops at that dimension's item templates.
    /// Items with no clone - gold, consumables, quest tokens - keep the base template.
    ///
    /// Every entry is a NEW NPCDropInfo: NPCTemplate's copy constructor copies the list but
    /// shares its elements (NPCTemplate.cs:251), so mutating one in place would retarget the
    /// base template's drop table and every other dimension's along with it.</summary>
    private void RepointDrops(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.NPCHandler.GetTemplates()
                                       .Where(t => t.NPCTemplateID < Offset).ToList())
            {
                var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
                if (clone == null || basic.Drops == null) continue;

                var drops = new List<NPCDropInfo>();
                foreach (var drop in basic.Drops)
                {
                    var dimTemplate = world.ItemHandler.GetTemplate(drop.ItemTemplate.ID + Offset * dim);

                    drops.Add(new NPCDropInfo
                    {
                        ItemTemplate = dimTemplate ?? drop.ItemTemplate,
                        DropRate = drop.DropRate,
                        Stack = drop.Stack,
                    });
                }

                clone.Drops = drops;
            }
        }
    }
```

**Step 4: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~DimensionDropTests"`
Expected: 4 passing.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionDropTests.cs
git commit -m "Dimensions: point dimension drop tables at dimension item templates"
```

---

## Task 8: Map-script delegation

Fixes a Part 1 regression: `Dimensions.csx:220` replaces the base map's script, so the dimension clones of `ArenaMap.csx` and `ZombieTownMap.csx` do nothing.

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Map/DimensionMap.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:220-221`
- Test: `Goose.Tests/DimensionMapScriptTests.cs` (extend)

**Step 1: Write the failing test**

```csharp
    [Fact]
    public void Forwards_to_the_base_maps_script()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        var inner = new RecordingMapScript();
        basic.Script = ScriptStub.For<IMapScript>(inner);
        basic.ScriptParams = "inner-params";

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(100001);
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 6;

        clone.Script.Object.OnPlayerEntered(clone, player, fixture.World);

        Assert.Equal(1, inner.EnteredCalls);
        // The dimension now comes from the map id, so ScriptParams carries the base map's.
        Assert.Equal("inner-params", clone.ScriptParams);
    }

    [Fact]
    public void A_refusal_from_the_base_script_still_blocks_entry()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        basic.Script = ScriptStub.For<IMapScript>(new RecordingMapScript { Refusal = "Arena is closed." });

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(300001);
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 6;   // the dimension gate would allow this

        Assert.Equal("Arena is closed.", clone.Script.Object.CanPlayerJoin(clone, player, fixture.World));
    }

    [Fact]
    public void The_dimension_gate_still_wins_over_a_permissive_base_script()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        basic.Script = ScriptStub.For<IMapScript>(new RecordingMapScript());

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(300001);
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 1;

        Assert.Contains("maximum dimension", clone.Script.Object.CanPlayerJoin(clone, player, fixture.World));
    }

    private sealed class RecordingMapScript : BaseMapScript
    {
        public int EnteredCalls;
        public string Refusal;

        public override void OnPlayerEntered(Map map, Player player, GameWorld world) => this.EnteredCalls++;
        public override string CanPlayerJoin(Map map, Player player, GameWorld world) => this.Refusal;
    }
```

**Step 2: Run and watch it fail**

Run: `dotnet test --filter "FullyQualifiedName~DimensionMapScriptTests"`
Expected: FAIL — `EnteredCalls` is 0; `ScriptParams` is `"3"`.

**Step 3: Change the clone loop**

`Dimensions.csx:220-221`:

```csharp
                clone.Script = mapScript;
                // ScriptParams passes through untouched so a delegated base script reads the
                // params it was written against. DimensionMap takes its dimension from the
                // map id, which already encodes it.
                clone.ScriptParams = basic.ScriptParams;
```

**Step 4: Change `DimensionMap.csx`**

Replace `DimensionOf`:

```csharp
    /// <summary>The dimension is encoded in the map id (baseId + Offset*dim), so nothing
    /// needs to be stashed in ScriptParams - which is passed through to the base map's
    /// script instead.</summary>
    private int DimensionOf(Map map)
    {
        return map.ID / Offset;
    }

    private IMapScript Inner(Map map, GameWorld world)
    {
        return world.MapHandler.GetMap(map.ID % Offset)?.Script?.Object;
    }
```

Then forward from every member. `CanPlayerJoin` gates first, so the dimension refusal wins:

```csharp
    public override string CanPlayerJoin(Map map, Player player, GameWorld world)
    {
        int max = MaxDimensionOf(player);
        if (DimensionOf(map) > max)
            return "The void has rejected you. You have a maximum dimension of " + max + ".";

        return Inner(map, world)?.CanPlayerJoin(map, player, world);
    }
```

`OnPlayerEntered` keeps its existing body, with the forward appended after the gate passes (and **not** on the path that warps the player out — the base script must not see an entry that was rejected):

```csharp
        if (DimensionOf(map) <= max)
        {
            Inner(map, world)?.OnPlayerEntered(map, player, world);
            return;
        }
```

Forward the remaining seven members verbatim: `OnLoad`, `OnLoadTile`, `OnFinishedLoad`, `OnPlayerLeft`, `OnPlayerMove`, `OnPlayerChatEvent`, `OnNPCKilledEvent`, `OnNPCSpawnEvent`, `OnPetMove` (`Goose/Scripting/IMapScript.cs:11-24`). Example:

```csharp
    public override void OnPlayerLeft(Map map, Player player, GameWorld world)
    {
        Inner(map, world)?.OnPlayerLeft(map, player, world);
    }
```

Note the GM early return in `OnPlayerEntered` (`DimensionMap.csx:44`) returns before the forward. Move the privilege check below the forward, or forward explicitly on that path too — a GM entering a dimension Arena should still trigger the arena logic.

**Step 5: Run the tests**

Run: `dotnet test --filter "FullyQualifiedName~DimensionMapScriptTests"`
Expected: all passing, including the pre-existing Part 1 tests.

**Step 6: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Map/DimensionMap.csx Goose/Data/Illutia/Scripts/Global/Dimensions.csx \
        Goose.Tests/DimensionMapScriptTests.cs
git commit -m "Dimensions: delegate map clones to the base map's script"
```

---

## Task 9: Full-suite verification and manual smoke

**Step 1: Run everything**

Run: `dotnet test`
Expected: 0 failed. Report the count.

**Step 2: Boot the server against real data**

Run: `dotnet run --project Goose`

Watch the log for:
- No exception from `CloneItemTemplates` (a thrown "already exists" means the offset collides with real data — stop and report, do not raise the offset without asking).
- Template count grew by roughly `1199 × 6 = 7194`.

**Step 3: In-game smoke, as a GM**

1. `/dimension 1`, kill something that drops equipment. The drop should read `Powerful <name>`, roughly 45% of the time with a suffix, and be darker than the base item.
2. Check the item description reads `Abyss (1) …`.
3. Drop it, `/dimension 0`, have a second character with `dimension.max` 0 try to pick it up — the pickup should be refused with the void message.
4. Kill something that drops a tome in dimension 2, use it, confirm the spell learned is the dimension-2 copy and that a dimension-1 copy of the same spell is replaced rather than duplicated.
5. Walk into the dimension-1 Arena map and confirm arena behaviour still fires (Task 8).

**Step 4: Report**

State the test count, anything the smoke test surfaced, and the known limitations from the design that remain live — chiefly that dimension loot sells to gold vendors for `3^dim` times base until Part 5.

---

## Design alignment

Checked against `docs/plans/2026-08-10-dimensions-items-design.md`:

- Five server extension points — Tasks 1 and 2. ✅
- Clone set is armor, weapon, learn-spell scrolls — `ShouldClone`, Task 3. ✅
- Name prefixes, `"Abyss (n) "` description, recolour clamps, `Value × 3^dim`, cleared bind/LORE — Task 3. ✅
- Flat scaling baked into template `BaseStats`; tier from the base template — Task 3. ✅
- Suffix terms in the surname script, `Chance = 0` registrations — Task 5. ✅
- Roll odds, `CanPickup`, tome upgrade, delegation — Task 6. ✅
- Drop repointing with fresh `NPCDropInfo` — Task 7. ✅
- Map delegation, dimension from map id — Task 8. ✅

**One deliberate divergence from the design doc:** the design left `MeleeDamage`'s units open. The user chose the faithful abyss port (`(int)(base·dim + 10·dim·tier)`, truncation included) on 2026-08-10; the design's decisions table has been amended to record it.
