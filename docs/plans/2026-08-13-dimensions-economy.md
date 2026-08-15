# Dimensions Part 6 — The Spirit Economy — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Give spirit a faucet (rebirth), four sinks (`/resetitem`, `/buygold`, `/buyexperience`, `/givesp`), and dimension vendors that stock dimension goods — closing the loop Parts 1–5 opened.

**Architecture:** Five generic, additive server extension points; everything dimension-aware lives in `.csx`. `Dimensions.csx` gains a config block, a rebirth NPC/quest pass, a vendor repointing pass, and four command registrations. A new `Rebirth.csx` backs the quest's `Script` requirement and `Script` reward. `DimensionItem.csx` gains a guaranteed-suffix reroll hook.

**Tech Stack:** .NET 10, C#, xUnit, Roslyn C# scripting (`.csx`), SQLite.

**Design doc:** `docs/plans/2026-08-13-dimensions-economy-design.md`

---

## APIs verified

Every cross-file call in this plan, cited from the declaring file. Do not substitute from memory.

| API | Declaration |
|---|---|
| `Player.ChangeClass(int, int, GameWorld)` | `Goose/Player.cs:1358` |
| `Player.AddExperience(long, GameWorld, ExperienceMessage)` | `Goose/Player.cs:1652` (`virtual`) |
| `Settings.ExperienceCap` early-return | `Goose/Player.cs:1653-1660` |
| `ExperienceModifier` two-branch scaling | `Goose/Player.cs:1662-1671` |
| `Settings.ChangeClassExperienceLossPercent` | `Goose/GooseSettings.cs:148` — **`double`, not `decimal`**; applied at `Goose/Player.cs:1370` |
| `Pet.AddExperience` overrides the 3-arg signature | `Goose/Pet.cs:802` — own body, never calls `base` |
| `Player.Experience` / `ExperienceSold` | `Goose/Player.cs:298` / `:302` (both `long`, public set) |
| `Player.AddGold(long, GameWorld)` | `Goose/Player.cs:1471` |
| `Item.Name` / `BaseStats` / `TotalStats` / `ItemProperties` | `Goose/Item.cs:36` / `:65` / `:68` / `:78` |
| `Item.StatMultiplier` | `Goose/Item.cs:71` — **`double`, not `decimal`** |
| `Item.RefreshStats()` | `Goose/Item.cs:247` |
| `Item.LoadFromTemplate` accumulates (`TotalStats +=`) | `Goose/Item.cs:155-177`, esp. `:159` |
| `Item.UseType` (delegates to template) | `Goose/Item.cs:101` |
| `enum ItemProperty { TitleId, SurnameId }` | `Goose/Item.cs:14-18` — no `Dimension` member |
| `ItemHandler.RollTitleAndSurname(Item, GameWorld)` | `Goose/ItemHandler.cs:275` |
| script-hook try/catch pattern to copy | `Goose/ItemHandler.cs:279-289` |
| `ItemHandler.GetTemplate(int)` | `Goose/ItemHandler.cs:191` |
| `ItemHandler.AddTemplate(ItemTemplate)` | `Goose/ItemHandler.cs:202` |
| `IItemScript` members | `Goose/Scripting/IItemScript.cs:11-25` |
| `BaseItemScript` virtual defaults | `Goose/Scripting/BaseItemScript.cs:13-45` |
| `ICurrency` members | `Goose/Currency/ICurrency.cs:9-32` |
| `CurrencyHandler.Register` / `Get` / `Resolve` | `Goose/Currency/CurrencyHandler.cs:16` / `:29` / `:41` — **no `Gold` property** |
| `Currency.Gold` = `"gold"` | `Goose/Currency/Currency.cs:7` |
| `EventHandler.RegisterEvent(string, CreateEvent)` | `Goose/EventHandler.cs:251` |
| `Inventory.GetSlot(int)` | `Goose/Inventory.cs:173` |
| `Inventory.SendSlot(int, GameWorld)` | `Goose/Inventory.cs:111` |
| `ItemSlot.Item` / `.Stack` | `Goose/ItemSlot.cs:17` / `:19` |
| `Settings.InventorySize` | `Goose/GooseSettings.cs:153` |
| `LogHandler.Log(Log.Types, Player, string, int)` | `Goose/LogHandler.cs:32` |
| `Log.Types` — non-GM block ends `SellToVendor` | `Goose/Log.cs:12-33` |
| `PlayerHandler.GetPlayer(string)` | `Goose/PlayerHandler.cs:129` |
| `QuestHandler.AddQuest(Quest)` / `.Get(int)` | `Goose/Quests/QuestHandler.cs:89` / `:76` |
| `Quest` settable properties | `Goose/Quests/Quest.cs:12-33` |
| `RequirementType.NothingEquipped` = 6, `.Script` = 7 | `Goose/Quests/QuestRequirement.cs:12-20` |
| `RewardType.Script` = 21 | `Goose/Quests/QuestReward.cs:11-34` |
| `TakeRequirements` runs **before** `GiveRewards` | `Goose/Quests/QuestWindow.cs:341-342` |
| `NothingEquipped` check iterates equip slots | `Goose/Quests/QuestWindow.cs:279-286` |
| `IQuestScript` members | `Goose/Scripting/IQuestScript.cs` |
| `NPCHandler.AddTemplate` / `SpawnNPC` / `GetNPCTemplate` | `Goose/NPCHandler.cs:236` / `:307` / (see `GetTemplates()` `:—`) |
| `NPCHandler.SpawnNPC(GameWorld, int, int, int, NPCTemplate, bool)` | `Goose/NPCHandler.cs:307` — returns `NPC`, null on failure |
| `NPCTemplate.VendorItems` (`NPCVendorSlot[]`) | `Goose/NPCTemplate.cs:188` |
| `NPCTemplate` copy ctor **shares** `VendorItems` | `Goose/NPCTemplate.cs:254` |
| `NPCVendorSlot` fields | `Goose/NPCVendorSlot.cs:10-13` |
| `Ranks` orders by `ExperienceSold` | `Goose/Ranks.cs:72,84` |
| `Map.PlayerCanJoin` experience gate | `Goose/Map.cs:638,644` |
| `Map.IsTileBlocked(ICharacter, int, int)` | `Goose/Map.cs:417` — bounds, warp/blocked tile and occupancy in one call |
| `Map.GetCharacterAt` / `Map.SetCharacter` silently no-op out of range | `Goose/Map.cs:634`, `:643` |
| `NPC.LoadFromTemplate` → `Map.AddNPC` then `Spawn` → `PlaceCharacter` | `Goose/NPC.cs:645-648` |
| `Player.WarpTo` does **no** gating | `Goose/Player.cs:1234`, `:1242` |
| `Script<T>.LoadScript` casts the return value to `Type` | `Goose/Scripting/Script.cs:44-46` — `.csx` returns `typeof(X)`, never `new X()` |
| `Item.WeaponDamage` / `TotalWeaponDamage` | `Goose/Item.cs:57` / `:60`; folded together at `:256` |
| `ItemModifierScript` writes `WeaponDamage` | `Goose/Data/Illutia/Scripts/Item/ItemModifierScript.csx:67` |
| `AttributeSet.SP` is `long` | `Goose/AttributeSet.cs:16` |
| `PlayerHandler.AddPlayer` indexes by `player.Sock` | `Goose/PlayerHandler.cs:51` — unusable for a socketless test player |
| `EventHandler.AddEvent(Player, string)` / `Update(GameWorld)` | `Goose/EventHandler.cs:286` / `:361` |
| `ScriptStub.For<T>` | `Goose.Tests/Fixtures/ScriptStub.cs` |

**Existing script code to model new passes on** — read these before writing anything:

- `CreateUnlockChain` + `ValidateWardenClass` + `CreateWarden` — `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:357-500`
- `RepointDrops` (new-instance rule) — `Dimensions.csx:664-690`
- `DimensionCommandEvent` (script command shape) — `Dimensions.csx:1084-1120`
- `SpiritCurrency` — `Dimensions.csx:1132+`
- `CloneMaps` experience gates — `Dimensions.csx:243-247`

**Facts that will bite you if you assume otherwise:**

- There is **no `Item.Dirty`**. Abyss has `setDirty`; goose serialises items wholesale.
- There is **no `CurrencyHandler.Gold` property**. Use `Get(Currency.Gold)`.
- `Item.StatMultiplier` is `double`. `AttributeSet` percent fields are `decimal`.
- `ChangeClassExperienceLossPercent` is `double`, so the new parameter is `double` and rebirth passes `0d` — **not** `0m`.
- `Pet` overrides `AddExperience`'s 3-arg signature with its own body. The 3-arg method must stay `virtual`, and the new 4-arg overload must be **non-virtual**, so `Pet`'s behaviour is bit-for-bit unchanged.
- `.csx` files compile **separately** with no `#load`. A second global script cannot see `Dimensions.Offset`. Everything goes in `Dimensions.csx`.
- `QuestReward` has **no `Quest` back-reference**; `QuestRequirement` does.
- A `.csx` file ends with `return typeof(TheClass);`. `Script<T>.LoadScript` casts the
  script's return value to `Type` and calls `Activator.CreateInstance` itself
  (`Script.cs:44-46`), so `return new TheClass();` throws `InvalidCastException` at load.
- `Player.WarpTo` does no gating whatsoever. Anything that warps a player and wants the
  map's rules applied must call `Map.PlayerCanJoin` first.
- `Map.SetCharacter` returns silently on an out-of-range coordinate (`Map.cs:643`), so a
  badly placed NPC is registered, listed in `Map.NPCs`, invisible, and untargetable, with
  no error anywhere. Validate placement before spawning and confirm the tile after.
- `ItemModifier.ApplyStats` writes `Item.WeaponDamage` as well as `BaseStats` and
  `StatMultiplier` (`ItemModifierScript.csx:67`). Any "reset to template state" that omits
  it lets repeated rerolls stack damage without bound.
- `GlobalScriptFixture` swaps the **static** `GameWorld.Settings`. Every suite that uses it
  belongs in `[Collection(GameWorldSettingsCollection.Name)]`, or it races the dozen
  existing suites that do the same.

---

## Task 1: Server primitives — `ChangeClass`, `AddExperience`, `Log.Types`

**Files:**
- Modify: `Goose/Player.cs:1358` (ChangeClass), `Goose/Player.cs:1652` (AddExperience)
- Modify: `Goose/Log.cs:33`
- Modify: `Goose.Tests/Fixtures/GlobalScriptFixture.cs` (seed class 1)
- Test: `Goose.Tests/PlayerEconomyOverloadTests.cs` (create)

**Step 0: Seed the Commoner class in the fixture**

`GlobalScriptFixture` seeds only classes 0 and 3 (`GlobalScriptFixture.cs:52-54`). Every
`ChangeClass(1, 1, ...)` in this plan — Task 1's overload tests and Task 4's rebirth
reward — reaches `Class.GetLevel(1)` on class 1 and NREs without it. Illutia's real
`class_info` carries levels 1-5 for class 1, so seed exactly that, not 50:

```csharp
        // Seed classes so NPC spawning works (see ORCHESTRATION NOTE 2).
        SeedClass(0, "Default", 50);
        // Rebirth changes the player to class 1 level 1 (Rebirth.csx), so the destination
        // class has to exist in the fixture too. Real class_info carries 1-5 for class 1
        // and 1-50 for 2-7 — the same asymmetry Dimensions.csx's warden comment calls out.
        SeedClass(1, "Commoner", 5);
        SeedClass(3, "Warrior", 50);
```

**Step 1: Write the failing tests**

```csharp
using Goose;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>In GameWorldSettingsCollection because every test here writes
/// GameWorld.Settings, which is static; GlobalScriptFixture swaps and restores it
/// (GlobalScriptFixture.cs:7,:38) but cannot protect against a parallel class doing the
/// same. IClassFixture keeps one fixture — and so one settings swap — for the class.</summary>
[Collection(GameWorldSettingsCollection.Name)]
public class PlayerEconomyOverloadTests : IClassFixture<GlobalScriptFixture>
{
    private readonly GlobalScriptFixture fixture;

    public PlayerEconomyOverloadTests(GlobalScriptFixture fixture) => this.fixture = fixture;

    /// <summary>Rebirth must not shave the settings loss percent. ChangeClass banks
    /// Experience into ExperienceSold (Player.cs:1368) and multiplies the result by
    /// (1 - loss) at Player.cs:1370; an explicit 0 has to skip that entirely.</summary>
    [Fact]
    public void ChangeClass_with_explicit_zero_loss_banks_the_full_experience()
    {
        GameWorld.Settings.ChangeClassExperienceLossPercent = 0.07;
        var map = fixture.AddBaseMap(9100, "Overload Map");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 1_000_000;
        player.ExperienceSold = 0;

        player.ChangeClass(1, 1, fixture.World, 0d);

        Assert.Equal(1_000_000, player.ExperienceSold);
    }

    [Fact]
    public void ChangeClass_three_arg_overload_still_applies_the_settings_loss()
    {
        GameWorld.Settings.ChangeClassExperienceLossPercent = 0.07;
        var map = fixture.AddBaseMap(9101, "Overload Map 2");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 1_000_000;
        player.ExperienceSold = 0;

        player.ChangeClass(1, 1, fixture.World);

        Assert.Equal(930_000, player.ExperienceSold);
    }

    /// <summary>Purchased experience must arrive un-multiplied. AddExperience scales by
    /// world.ExperienceModifier on a branch selected by ExperienceModifierLimit
    /// (Player.cs:1662-1671), which script cannot invert reliably.</summary>
    [Theory]
    [InlineData(0L)]            // no limit configured -> full-modifier branch
    [InlineData(1_000L)]        // player is past the limit -> reduced-modifier branch
    public void AddExperience_without_modifiers_grants_the_exact_amount(long modifierLimit)
    {
        GameWorld.Settings.ExperienceCap = 0;
        GameWorld.Settings.ExperienceModifier = 2;
        GameWorld.Settings.ExperienceModifierLimit = modifierLimit;
        fixture.World.ExperienceModifier = 2;

        var map = fixture.AddBaseMap(9102 + (int)modifierLimit, "Overload Map 3");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 50_000;

        player.AddExperience(25_000_000, fixture.World, Player.ExperienceMessage.None, applyModifiers: false);

        Assert.Equal(25_050_000, player.Experience);
    }

    [Fact]
    public void AddExperience_three_arg_overload_still_applies_the_modifier()
    {
        GameWorld.Settings.ExperienceCap = 0;
        GameWorld.Settings.ExperienceModifier = 2;
        GameWorld.Settings.ExperienceModifierLimit = 0;
        fixture.World.ExperienceModifier = 2;

        var map = fixture.AddBaseMap(9105, "Overload Map 4");
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        player.Experience = 0;

        player.AddExperience(1_000, fixture.World, Player.ExperienceMessage.None);

        Assert.Equal(2_000, player.Experience);
    }
}
```

> **If `fixture.World.ExperienceModifier` turns out not to be settable**, read `Goose/GameWorld.cs` for how the modifier is exposed and adjust — but do not change the assertion values without recomputing them from `Player.cs:1662-1671`.

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter PlayerEconomyOverloadTests`
Expected: FAIL — no `ChangeClass` 4-arg overload, no `AddExperience` 4-arg overload.

**Step 3: Add the overloads**

In `Goose/Player.cs`, rename the existing `ChangeClass` body to take the extra parameter and add a delegating overload:

```csharp
/// <summary>Class change with an explicit experience loss. Rebirth passes 0: it is an
/// exchange (experience becomes spirit), not the 7% penalty quest 60 charges.</summary>
public void ChangeClass(int classid, int newLevel, GameWorld world, double experienceLossPercent)
{
    // ... existing body verbatim, except line 1370:
    //     this.ExperienceSold = (long)(this.ExperienceSold * (1.0d - experienceLossPercent));
}

public void ChangeClass(int classid, int newLevel, GameWorld world)
{
    this.ChangeClass(classid, newLevel, world, GameWorld.Settings.ChangeClassExperienceLossPercent);
}
```

The parameter is `double` because `Settings.ChangeClassExperienceLossPercent` is
(`GooseSettings.cs:148`), so line 1370's `1.0d - x` arithmetic is unchanged and the
rounding is bit-for-bit identical for existing callers.

`AddExperience` takes the same shape with one difference — **the new overload is not
virtual**:

```csharp
/// <summary>Unchanged signature and still virtual: Pet overrides this (Pet.cs:802) with
/// its own body and never calls base.</summary>
public virtual void AddExperience(long exp, GameWorld world, ExperienceMessage message)
{
    this.AddExperience(exp, world, message, applyModifiers: true);
}

/// <summary>applyModifiers: false grants exactly `exp`. Purchased experience must not be
/// re-scaled by world.ExperienceModifier — the two-branch scaling below cannot be
/// inverted from script, and buyers are exactly the players past ExperienceModifierLimit.
///
/// Not virtual, and Pet does not override it: only Player-side purchases call this.</summary>
public void AddExperience(long exp, GameWorld world, ExperienceMessage message, bool applyModifiers)
{
    // ... existing cap check verbatim ...

    if (applyModifiers)
    {
        // ... existing two-branch modifier block verbatim ...
    }

    // ... rest verbatim ...
}
```

> `Pet.AddExperience` (`Goose/Pet.cs:802`) overrides the three-argument signature with a
> complete body that never calls `base`. Keeping that signature `virtual` and the new one
> non-virtual means pets behave exactly as they do today — a pet never reaches the moved
> body. Do not "tidy" this by making the four-argument overload virtual.

In `Goose/Log.cs`, append to the non-GM block, immediately after `SellToVendor` and before the `GetItem = 10001` block:

```csharp
            SellToVendor,
            Rebirth,
            BuyGold,
            BuyExperience,
            GiveSpirit,
            ResetItem,
```

`ResetItem` gets its own member rather than reusing `CreatedCustom`. `CreatedCustom` is
the GM item-creation log; folding a paid reroll into it makes both unqueryable — an
economy audit for "who rerolled what, for how much" cannot separate the two, and the
per-type report for GM item creation gains a flood of player rows.

Append these members without assigning explicit values. The block is implicitly numbered
from `InvalidPassword = 16`, so they take 20-24 and nothing shifts under
`GetItem = 10001`. Do not renumber the existing members: `logs.log_type` is persisted
(`Log.cs`'s `SaveToDatabase`), so a shifted value silently rewrites history.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter PlayerEconomyOverloadTests`
Expected: PASS, 5 tests.

Then the full suite, to prove no existing caller shifted:

Run: `dotnet test Goose.sln`
Expected: PASS — 447 passed, 26 skipped, 0 failed, plus the 5 new.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/Log.cs Goose.Tests/PlayerEconomyOverloadTests.cs Goose.Tests/Fixtures/GlobalScriptFixture.cs
git commit -m "feat: ChangeClass and AddExperience overloads, spirit economy log types"
```

---

## Task 2: `ItemHandler.ResetModifiers` and `RerollModifiers`

**Files:**
- Modify: `Goose/Scripting/IItemScript.cs`, `Goose/Scripting/BaseItemScript.cs`
- Modify: `Goose/ItemHandler.cs` (near `RollTitleAndSurname`, `:275`)
- Test: `Goose.Tests/ItemRerollTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
using Goose;
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>Fixture-based, and in GameWorldSettingsCollection: GlobalScriptFixture swaps
/// the static GameWorld.Settings in its constructor and restores it on dispose
/// (GlobalScriptFixture.cs:7,:38), so a class that mutates settings must not run in
/// parallel with the other suites that do the same.</summary>
[Collection(GameWorldSettingsCollection.Name)]
public class ItemRerollTests
{
    /// <summary>Modelled on ItemScriptHookTests.Arrange (`ItemScriptHookTests.cs:62-77`):
    /// a template registered in the handler, an Item loaded from it, and
    /// `ScriptStub.For<IItemScript>` for the script — the established way to attach an
    /// in-memory script object without touching disk (`Fixtures/ScriptStub.cs`).
    ///
    /// The template carries non-zero BaseStats and WeaponDamage on purpose: both are
    /// accumulated by RefreshStats (`Item.cs:247-256`), so they are what a non-idempotent
    /// reset would double.</summary>
    private static Item ItemWithModifiers(GlobalScriptFixture fixture, IItemScript script = null)
    {
        var template = fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.WeaponDamage = 7;
            t.BaseStats = new AttributeSet { Strength = 10 };
            if (script != null) t.Script = ScriptStub.For(script);
        });

        var item = new Item();
        item.LoadFromTemplate(template);
        return item;
    }

    /// <summary>ResetModifiers must return the item to template state. Item.cs:14-18 has
    /// exactly two ItemProperty members, both of which a modifier sets — and
    /// ItemModifierScript.csx:67 also writes item.WeaponDamage, which RefreshStats folds
    /// into TotalWeaponDamage (Item.cs:256).</summary>
    [Fact]
    public void ResetModifiers_clears_name_stats_multiplier_weapon_damage_and_properties()
    {
        using var fixture = new GlobalScriptFixture();
        var item = ItemWithModifiers(fixture);
        item.Name = "Legendary Sword of Speed";
        item.BaseStats.Strength = 50;
        item.WeaponDamage = 40;
        item.StatMultiplier = 1.25;
        item.ItemProperties[ItemProperty.TitleId] = 900100;
        item.ItemProperties[ItemProperty.SurnameId] = 900005;

        fixture.World.ItemHandler.ResetModifiers(item);

        Assert.Equal(item.Template.Name, item.Name);
        Assert.Equal(0, item.BaseStats.Strength);
        Assert.Equal(0, item.WeaponDamage);
        Assert.Equal(1, item.StatMultiplier);
        Assert.Equal(10, item.TotalStats.Strength);
        Assert.Equal(7, item.TotalWeaponDamage);
        Assert.False(item.ItemProperties.ContainsKey(ItemProperty.TitleId));
        Assert.False(item.ItemProperties.ContainsKey(ItemProperty.SurnameId));
    }

    /// <summary>The guard that matters. Item.LoadFromTemplate does TotalStats +=
    /// (Item.cs:159), so a reset built on it would double-count the template's stats on
    /// every call — and a reset that forgets item.WeaponDamage lets repeated paid rerolls
    /// stack weapon damage forever. ResetModifiers must be safe to call repeatedly, in
    /// both fields.</summary>
    [Fact]
    public void ResetModifiers_is_idempotent_in_stats_and_weapon_damage()
    {
        using var fixture = new GlobalScriptFixture();
        var item = ItemWithModifiers(fixture);
        item.BaseStats.Strength = 50;
        item.WeaponDamage = 40;
        item.StatMultiplier = 1.25;

        fixture.World.ItemHandler.ResetModifiers(item);
        var statsAfterOnce = item.TotalStats.Strength;
        var damageAfterOnce = item.TotalWeaponDamage;
        fixture.World.ItemHandler.ResetModifiers(item);

        Assert.Equal(statsAfterOnce, item.TotalStats.Strength);
        Assert.Equal(damageAfterOnce, item.TotalWeaponDamage);
        Assert.Equal(10, item.TotalStats.Strength);   // the template's, counted once
        Assert.Equal(7, item.TotalWeaponDamage);      // ditto
    }

    [Fact]
    public void RerollModifiers_prefers_the_script_hook()
    {
        using var fixture = new GlobalScriptFixture();
        var item = ItemWithModifiers(fixture, new RerollingStub());

        fixture.World.ItemHandler.RerollModifiers(item, fixture.World);

        Assert.Equal("rerolled", item.Name);
    }

    [Fact]
    public void RerollModifiers_falls_through_when_the_hook_declines()
    {
        // A stub returning false must leave the item at template state — the native
        // RollTitleAndSurname path runs, and with zero-chance settings adds nothing.
        using var fixture = new GlobalScriptFixture();
        GameWorld.Settings.ItemTitleChancePercent = 0;
        GameWorld.Settings.ItemSurnameChancePercent = 0;
        var item = ItemWithModifiers(fixture, new DecliningStub());
        item.Name = "Legendary Sword of Speed";

        fixture.World.ItemHandler.RerollModifiers(item, fixture.World);

        Assert.Equal(item.Template.Name, item.Name);
    }

    /// <summary>The realistic failure, and the reason "no exception was thrown" is not a
    /// sufficient assertion: a hook that applies a suffix and *then* throws would otherwise
    /// leave the item carrying a modifier the reset already stripped and the catch never
    /// undoes — free stats on a charge that also refunds nothing. RerollModifiers must
    /// reset again in the catch so the native fallback rolls against template state.</summary>
    [Fact]
    public void RerollModifiers_returns_a_half_applied_throwing_hook_to_template_state()
    {
        using var fixture = new GlobalScriptFixture();
        GameWorld.Settings.ItemTitleChancePercent = 0;
        GameWorld.Settings.ItemSurnameChancePercent = 0;
        var item = ItemWithModifiers(fixture, new HalfApplyingThrowingStub());

        var ex = Record.Exception(() => fixture.World.ItemHandler.RerollModifiers(item, fixture.World));

        Assert.Null(ex);
        Assert.Equal(item.Template.Name, item.Name);
        Assert.False(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal(0, item.WeaponDamage);
        Assert.Equal(1, item.StatMultiplier);
        Assert.Equal(10, item.TotalStats.Strength);
        Assert.Equal(7, item.TotalWeaponDamage);
    }

    private class RerollingStub : BaseItemScript
    {
        public override bool OnRerollModifiersEvent(Item item, GameWorld world)
        {
            item.Name = "rerolled";
            return true;
        }
    }

    private class DecliningStub : BaseItemScript { }

    /// <summary>Mutates the way DimensionItem.OnRerollModifiersEvent does — name, the
    /// surname property, then stats — and throws partway through.</summary>
    private class HalfApplyingThrowingStub : BaseItemScript
    {
        public override bool OnRerollModifiersEvent(Item item, GameWorld world)
        {
            item.Name = item.Name + " of the Bear";
            item.ItemProperties[ItemProperty.SurnameId] = 900005;
            item.BaseStats.Strength += 25;
            item.WeaponDamage += 40;
            item.StatMultiplier *= 1.25;
            item.RefreshStats();
            throw new InvalidOperationException("boom");
        }
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter ItemRerollTests`
Expected: FAIL — `ResetModifiers` / `RerollModifiers` / `OnRerollModifiersEvent` do not exist.

**Step 3: Implement**

`Goose/Scripting/IItemScript.cs`, appended to the interface:

```csharp
        /// <summary>Return true having re-rolled this item's modifiers yourself. Consulted
        /// by ItemHandler.RerollModifiers after the item has been reset to template state.
        ///
        /// Separate from OnRollModifiersEvent because a paid reroll and a drop roll differ:
        /// the drop rolls a chance, a paid reroll is expected to land something.</summary>
        bool OnRerollModifiersEvent(Item item, GameWorld world);
```

`Goose/Scripting/BaseItemScript.cs`:

```csharp
        public virtual bool OnRerollModifiersEvent(Item item, GameWorld world)
        {
            return false;
        }
```

`Goose/ItemHandler.cs`, beside `RollTitleAndSurname`:

```csharp
        /// <summary>Returns an item to template state: no title, no surname, no modifier
        /// stats, no modifier weapon damage. Safe to call repeatedly.
        ///
        /// Every field a modifier can write has to be listed here. ItemModifier.ApplyStats
        /// runs through ItemModifierScript.csx's AddStats (`:60-80`), which writes
        /// StatMultiplier, WeaponDamage and BaseStats — WeaponDamage included, and
        /// RefreshStats folds it into TotalWeaponDamage (`Item.cs:256`). Forget it and
        /// repeated paid rerolls stack weapon damage without bound.
        ///
        /// Deliberately not built on Item.LoadFromTemplate, which accumulates rather than
        /// assigns (TotalStats += template.BaseStats, Item.cs:159) and would double-count
        /// the template's stats on a second call.</summary>
        public void ResetModifiers(Item item)
        {
            item.Name = item.Template.Name;
            item.BaseStats = new AttributeSet();
            item.WeaponDamage = 0;
            item.StatMultiplier = 1;
            item.ItemProperties.Remove(ItemProperty.TitleId);
            item.ItemProperties.Remove(ItemProperty.SurnameId);
            item.RefreshStats();
        }

        /// <summary>Strips the item's modifiers and rolls fresh ones. A script owning the
        /// item claims the roll first; otherwise the native chance-based roll runs.</summary>
        public void RerollModifiers(Item item, GameWorld world)
        {
            this.ResetModifiers(item);

            if (item.Script != null)
            {
                try
                {
                    if (item.Script.Object.OnRerollModifiersEvent(item, world)) return;
                }
                catch (Exception e)
                {
                    log.Error(e, "Exception in OnRerollModifiersEvent for template {templateId}", item.TemplateID);

                    // A hook that applied part of a roll before throwing has left modifiers
                    // on the item that the reset above already stripped once. Reset again so
                    // the fallback rolls against template state, exactly as it would have if
                    // the item carried no script at all. Swallowing the exception without
                    // this leaves free stats behind.
                    this.ResetModifiers(item);
                }
            }

            this.RollTitleAndSurname(item, world);
        }
```

> `item.Script` is how `RollTitleAndSurname` reaches the script (`ItemHandler.cs:279`). Use the identical accessor — do not reach through `item.Template.Script` in the handler.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter ItemRerollTests`
Expected: PASS, 5 tests.

Run: `dotnet test Goose.sln`
Expected: PASS, no regressions. The four existing item scripts inherit `BaseItemScript`, so none needs editing.

**Step 5: Commit**

```bash
git add Goose/Scripting/IItemScript.cs Goose/Scripting/BaseItemScript.cs Goose/ItemHandler.cs Goose.Tests/ItemRerollTests.cs
git commit -m "feat: ItemHandler.ResetModifiers and RerollModifiers with a script hook"
```

---

## Task 3: `Dimensions.csx` config and the rebirth NPC + quest

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (config block ~`:12-86`; new pass after `CreateUnlockChain`; `OnLoaded` ~`:88-122`)
- Test: `Goose.Tests/DimensionRebirthTests.cs` (create)

Read `Dimensions.csx:357-500` first. `CreateRebirthQuest` is `CreateUnlockChain` + `CreateWarden` collapsed into one non-looping pass; match their comment density and their preflight-and-throw style exactly.

**Step 1: Write the failing tests**

```csharp
using System.IO;
using System.Linq;
using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>In GameWorldSettingsCollection: GlobalScriptFixture swaps the static
/// GameWorld.Settings, and Task 4's reward test writes
/// ChangeClassExperienceLossPercent. Every other fixture-based dimension suite is in this
/// collection for the same reason (DimensionsScriptTests.cs:7).</summary>
[Collection(GameWorldSettingsCollection.Name)]
public class DimensionRebirthTests
{
    private const int RebirthTemplateId = 810000;
    private const int RebirthQuestId = 910000;
    private const int RebirthX = 52;
    private const int RebirthY = 50;

    /// <summary>DimensionsScriptTests' world seeding (`DimensionsScriptTests.cs:20-25`),
    /// factored out because every test here needs it: a base map wide enough for the
    /// warden at (50,50) and the keeper at (52,50), and the boss template the unlock chain
    /// requires.</summary>
    private static GlobalScriptFixture Seeded()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        boss.BaseStats = new AttributeSet { HP = 3704 };
        fixture.World.NPCHandler.AddTemplate(boss);

        return fixture;
    }

    [Fact]
    public void Creates_the_rebirth_npc_template_and_quest()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var template = fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId);
        Assert.NotNull(template);
        Assert.False(template.CanBeKilled);
        Assert.False(template.CanMove);
        Assert.Equal(NPCTemplate.Types.Quest, template.NPCType);

        var quest = fixture.World.QuestHandler.Get(RebirthQuestId);
        Assert.NotNull(quest);
        Assert.True(quest.Repeatable);
        Assert.Contains(quest, template.Quests);
    }

    [Fact]
    public void Rebirth_quest_carries_a_nothing_equipped_and_a_script_requirement()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var quest = fixture.World.QuestHandler.Get(RebirthQuestId);

        Assert.Contains(quest.Requirements, r => r.Type == RequirementType.NothingEquipped);

        var scripted = Assert.Single(quest.Requirements, r => r.Type == RequirementType.Script);
        Assert.Equal(RebirthQuestId + 2, scripted.Id);
        // Load-bearing: QuestWindow runs TakeRequirements before GiveRewards
        // (QuestWindow.cs:341-342), so a consuming requirement would zero the experience
        // the reward has to read.
        Assert.True(scripted.KeepRequirement);
    }

    [Fact]
    public void Rebirth_reward_is_a_script_reward_carrying_the_rate()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var reward = Assert.Single(fixture.World.QuestHandler.Get(RebirthQuestId).Rewards);
        Assert.Equal(RewardType.Script, reward.Type);
        Assert.Equal(RebirthQuestId + 11, reward.Id);
        Assert.Equal("100000000", reward.ScriptParams);
    }

    /// <summary>Exactly one, in dimension 0. Rebirth strips you naked and drops you to
    /// level 1, and every dimension above 0 has CanPVP forced on.</summary>
    [Fact]
    public void Only_dimension_zero_gets_a_rebirth_npc()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        for (int dim = 1; dim <= 6; dim++)
            Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId + 100000 * dim));
    }

    /// <summary>Map.SetCharacter returns silently on an out-of-range coordinate
    /// (Map.cs:643-648), so a keeper placed off the map would be registered, listed in
    /// Map.NPCs, and invisible. Assert both halves: listed AND holding the tile.</summary>
    [Fact]
    public void Rebirth_keeper_is_spawned_on_dimension_zero_and_holds_its_tile()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var map = fixture.World.MapHandler.GetMap(1);
        var keeper = Assert.Single(map.NPCs, n => n.NPCTemplateID == RebirthTemplateId);

        Assert.Same(keeper, map.GetCharacterAt(RebirthX, RebirthY));
        Assert.Equal(RebirthX, keeper.MapX);
        Assert.Equal(RebirthY, keeper.MapY);
        // Load-bearing: the warden already stands on (50,50), so the keeper must not be
        // configured onto an occupied tile.
        Assert.NotEqual(keeper, map.GetCharacterAt(50, 50));
    }

    /// <summary>The preflight, not the symptom. An occupied or blocked destination must
    /// stop the load rather than produce an NPC nobody can see.</summary>
    [Fact]
    public void Refuses_to_load_when_the_keepers_tile_is_blocked()
    {
        using var fixture = Seeded();
        var map = fixture.World.MapHandler.GetMap(1);
        map.tiles[RebirthY * map.Width + RebirthX] = new BlockedTile();

        var script = fixture.CompileShipped();

        var ex = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));
        Assert.Contains("Rebirth keeper cannot stand", ex.Message);
    }

    /// <summary>Rebirth changes the player to class 1 level 1. A dataset without that
    /// class_info row must fail at load, not halfway through a completed quest.</summary>
    [Fact]
    public void Refuses_to_load_when_the_destination_class_has_no_level_one()
    {
        using var fixture = Seeded();
        fixture.RemoveClassLevel(1, 1);

        var script = fixture.CompileShipped();

        var ex = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));
        Assert.Contains("no level 1 row in class_info", ex.Message);
    }

    [Fact]
    public void Disabled_creates_no_rebirth_npc_or_quest()
    {
        using var fixture = Seeded();
        // DimensionsScriptTests.cs:27-32 — read the shipped source, flip the Enabled
        // literal, assert the replacement actually changed something, then CompileSource.
        var source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "Dimensions.csx"));
        var disabled = source.Replace("public const bool Enabled = true;",
                                      "public const bool Enabled = false;");
        Assert.NotEqual(source, disabled);

        fixture.CompileSource(disabled, "DimensionsDisabled.csx").Object.OnLoaded(fixture.World);

        Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId));
        Assert.Null(fixture.World.QuestHandler.Get(RebirthQuestId));
        Assert.Empty(fixture.World.MapHandler.GetMap(1).NPCs);
    }
}
```

> Read `Goose.Tests/DimensionsScriptTests.cs` before writing these. It already solves "run the real shipped script against a synthetic world"; `Seeded()` above is its `:20-25` block factored out, and the disabled-mode test is its `:27-32`. Do not rebuild either.

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter DimensionRebirthTests`
Expected: FAIL — no template at 810000, no quest at 910000.

**Step 3: Add config and the pass**

Config, appended to the `Dimensions.csx` block (after the warden section, before `QuestIdBase`):

```csharp
    // ---- Rebirth --------------------------------------------------------
    // The spirit faucet: a repeatable quest converting banked experience into spirit and
    // resetting the character. Script-created for the same reason the warden is - the
    // dimensions feature stays self-contained, and Enabled = false leaves nothing behind.

    /// <summary>Clear of WardenTemplateId (800000 + Offset*6 = 1,400,000 is the warden's
    /// top id, but the wardens occupy 800000, 900000, ... so 810000 is unused).</summary>
    public const int RebirthTemplateId = 810000;

    /// <summary>Clear of QuestIdBase's range: quests 900000-900005, requirement and reward
    /// ids 900000 + n*10 + k, topping out at 900051.</summary>
    public const int RebirthQuestId = 910000;

    /// <summary>Experience per spirit. floor(total / ExpPerSpirit) is minted; the
    /// remainder is destroyed, faithful to RebirthEvent.java:47.</summary>
    public const long ExpPerSpirit = 100_000_000;

    public const string RebirthName = "Keeper of Rebirth";
    public const string RebirthTitle = "";
    public const string RebirthSurname = "";
    public const int RebirthClassId = 3;      // must have a class_info row at RebirthLevel
    public const int RebirthLevel = 50;

    /// <summary>Where rebirth *leaves* the player, as opposed to what the keeper looks
    /// like. Only used by CreateRebirthQuest's preflight: Rebirth.csx compiles separately
    /// and cannot read these, so it hardcodes the same 1 and 1 - keep the two in step.</summary>
    public const int RebirthDestinationClassId = 1;   // Commoner
    public const int RebirthDestinationLevel = 1;

    public const int RebirthBodyID = 1;
    public const int RebirthBodyState = 0;
    public const int RebirthBodyR = 40;
    public const int RebirthBodyG = 0;
    public const int RebirthBodyB = 60;
    public const int RebirthBodyA = 200;
    public const int RebirthFaceID = 1;
    public const int RebirthHairID = 1;
    public const int RebirthHairR = 20;
    public const int RebirthHairG = 0;
    public const int RebirthHairB = 40;
    public const int RebirthHairA = 200;
    public const string RebirthEquippedItems = "";

    /// <summary>Dimension 0 only, beside the dimension-0 warden. Map 1 is StartMapId, the
    /// map /dimension already warps to, so a player who can reach a warden can reach the
    /// keeper without a second landmark.
    ///
    /// Verified against Data/Illutia/Maps/Map1.map: the map is 286x194, and (52,50) carries
    /// no blocked flag (bit 2 of the tile flags, Map.cs:471-475). It is two tiles east of
    /// WardenX/WardenY (50,50), so the two generated NPCs cannot collide. Warp tiles and
    /// sheet NPC spawns come from the database rather than the .map file, so
    /// CreateRebirthQuest re-checks the tile at load time instead of trusting this.</summary>
    public const int RebirthMapId = StartMapId;
    public const int RebirthX = 52;
    public const int RebirthY = 50;
```

> If you move the keeper, re-verify the destination the same way — parse the target
> `MapN.map` (2-byte version, 2-byte editor version, int width, int height, then per tile
> an int flags followed by five (int graphic, short sheet) pairs) and confirm the tile's
> `flags & 2` is clear. The load-time preflight below will catch a bad coordinate, but it
> catches it by refusing to boot.

In `OnLoaded`, immediately after `CreateUnlockChain(world);`:

```csharp
        CreateRebirthQuest(world);
```

The pass itself, after `CreateWarden`:

```csharp
    /// <summary>The spirit faucet. One NPC and one repeatable quest, in dimension 0 only.
    ///
    /// Deliberately NOT run through ScaleTemplate, and deliberately not cloned per
    /// dimension: rebirth requires stripping naked and leaves the player at level 1, and
    /// every dimension above 0 has CanPVP forced on (CloneMaps).</summary>
    private void CreateRebirthQuest(GameWorld world)
    {
        var rebirthClass = world.ClassHandler.GetClass(RebirthClassId);
        if (rebirthClass == null)
            throw new Exception($"RebirthClassId {RebirthClassId} does not exist.");
        if (rebirthClass.GetLevel(RebirthLevel) == null)
            throw new Exception($"Class {RebirthClassId} has no level {RebirthLevel} row in class_info.");

        // The destination class, not the keeper's. Rebirth calls ChangeClass(1, 1, ...),
        // which reads Class.GetLevel(1) on class 1 (Player.cs:1358+). class_info carries
        // levels 1-5 for class 1, but a dataset that dropped the row would turn every
        // completed rebirth into an NRE mid-transaction, after the quest was consumed.
        var commoner = world.ClassHandler.GetClass(RebirthDestinationClassId);
        if (commoner == null)
            throw new Exception($"RebirthDestinationClassId {RebirthDestinationClassId} does not exist.");
        if (commoner.GetLevel(RebirthDestinationLevel) == null)
            throw new Exception(
                $"Class {RebirthDestinationClassId} has no level {RebirthDestinationLevel} row in class_info - rebirth would fail mid-transaction.");

        if (ExpPerSpirit <= 0)
            throw new Exception("ExpPerSpirit must be positive - GiveReward divides by it.");

        if (world.QuestHandler.Get(RebirthQuestId) != null)
            throw new Exception($"Quest id {RebirthQuestId} already exists. RebirthQuestId collides with sheet data.");
        if (world.NPCHandler.GetNPCTemplate(RebirthTemplateId) != null)
            throw new Exception($"Rebirth template id {RebirthTemplateId} already exists.");

        // Placement, before anything is registered. NPC.LoadFromTemplate calls
        // Map.PlaceCharacter -> Map.SetCharacter, which simply returns on out-of-range
        // coordinates (Map.cs:643-648) - the NPC would exist, be invisible, and be
        // untargetable, with no error anywhere. IsTileBlocked covers all three failures at
        // once: out of bounds, a blocked or warp tile, and an occupant (Map.cs:417-440).
        // It runs here rather than at spawn time because CreateRebirthQuest is called after
        // CloneSpawns, so every generated NPC - the dimension-0 warden included - is
        // already standing on the map.
        var rebirthMap = world.MapHandler.GetMap(RebirthMapId);
        if (rebirthMap == null)
            throw new Exception($"RebirthMapId {RebirthMapId} does not exist.");
        if (rebirthMap.IsTileBlocked(null, RebirthX, RebirthY))
            throw new Exception(
                $"Rebirth keeper cannot stand at {RebirthMapId}({RebirthX},{RebirthY}): out of bounds, blocked, a warp tile, or occupied.");

        var rebirthScript = world.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/Rebirth.csx");

        var quest = new Quest
        {
            Id = RebirthQuestId,
            Name = "Rebirth",
            Description = "Surrender everything you have earned and return to the\\n"
                        + "beginning. Every " + ExpPerSpirit.ToString("N0") + " experience\\n"
                        + "becomes one spirit. Anything left over is lost.\\n\\n"
                        + "You will be a level 1 commoner, and the dimensions\\n"
                        + "you have opened will demand their experience again.\\n\\n"
                        + "Come to me with nothing equipped.",
            FailText = "You are not ready. Remove everything you wear,\\nand bring more experience.",
            PassText = "You are unmade, and remade.",
            ShowProgress = true,
            Repeatable = true,
        };

        quest.Requirements.Add(new QuestRequirement
        {
            Id = RebirthQuestId + 1,
            Type = RequirementType.NothingEquipped,
            KeepRequirement = false,
            Quest = quest,
        });

        quest.Requirements.Add(new QuestRequirement
        {
            Id = RebirthQuestId + 2,
            Type = RequirementType.Script,
            Script = rebirthScript,
            ScriptParams = ExpPerSpirit.ToString(),
            // KeepRequirement true is load-bearing: TakeRequirements runs before
            // GiveRewards (QuestWindow.cs:341-342), so a consuming requirement would zero
            // the experience the reward has to read. All state change lives in the reward.
            KeepRequirement = true,
            Quest = quest,
        });

        quest.Rewards.Add(new QuestReward
        {
            Id = RebirthQuestId + 11,
            Type = RewardType.Script,
            Script = rebirthScript,
            // QuestReward has no Quest back-reference (QuestReward.cs:37-45), so the rate
            // travels here rather than being read off the requirement.
            ScriptParams = ExpPerSpirit.ToString(),
        });

        world.QuestHandler.AddQuest(quest);

        var keeper = new NPCTemplate
        {
            NPCTemplateID = RebirthTemplateId,
            NPCType = NPCTemplate.Types.Quest,
            Name = RebirthName,
            Title = RebirthTitle,
            Surname = RebirthSurname,
            Level = RebirthLevel,
            ClassID = RebirthClassId,

            CanBeKilled = false,
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

            BodyID = RebirthBodyID,
            BodyState = RebirthBodyState,
            BodyR = RebirthBodyR, BodyG = RebirthBodyG, BodyB = RebirthBodyB, BodyA = RebirthBodyA,
            FaceID = RebirthFaceID,
            HairID = RebirthHairID,
            HairR = RebirthHairR, HairG = RebirthHairG, HairB = RebirthHairB, HairA = RebirthHairA,
            EquippedItems = RebirthEquippedItems,

            AlliesString = "",
            Allies = new List<NPCTemplate>(),
            Drops = new List<NPCDropInfo>(),
        };

        keeper.BaseStats = new AttributeSet { HP = 1000, MP = 0 };
        keeper.Quests.Add(quest);

        world.NPCHandler.AddTemplate(keeper);

        var spawned = world.NPCHandler.SpawnNPC(world, RebirthMapId, RebirthX, RebirthY,
                                                keeper, shouldRespawn: false);
        if (spawned == null)
            throw new Exception($"Could not spawn the rebirth keeper: map {RebirthMapId} does not exist.");

        // LoadFromTemplate adds to Map.NPCs and then calls Spawn -> PlaceCharacter
        // (NPC.cs:645-648). PlaceCharacter is the step that silently no-ops out of range,
        // so confirm the keeper actually occupies the tile rather than just being listed.
        if (rebirthMap.GetCharacterAt(RebirthX, RebirthY) != spawned)
        {
            throw new Exception(
                $"Rebirth keeper did not take tile {RebirthMapId}({RebirthX},{RebirthY}) - it would be invisible and untargetable.");
        }
    }
```

> The `\\n` doubling in the description is not a typo — `Quest.Description` is rendered by `QuestWindow`'s `\n`-splitting path, and the existing script writes literal `\\n` the same way. Check `CreateUnlockChain`'s strings and match whatever they do.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionRebirthTests`
Expected: **only `Disabled_creates_no_rebirth_npc_or_quest` passes.** Every other test in
the class drives `CompileShipped().OnLoaded(...)`, and `CreateRebirthQuest` calls
`GetScript<IQuestScript>("Scripts/Quest/Rebirth.csx")` on a file Task 4 has not created —
`Script<T>.LoadScript` throws `FileNotFoundException` (`Script.cs:28`). The disabled test
returns from `OnLoaded` before any of that runs, which is exactly what it is for.

So the expected tally at this point is 1 passed, 6 failed. The two `Refuses_to_load_*`
tests do fail, but on the wrong exception type — that is expected here and resolves in
Task 4.

Proceed to Task 4 and re-run both together. **Do not** stub `Rebirth.csx` with a
placeholder to go green early.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionRebirthTests.cs
git commit -m "feat: rebirth NPC and repeatable quest in Dimensions.csx"
```

---

## Task 4: `Rebirth.csx`

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Quest/Rebirth.csx`
- Modify: `Goose.Tests/Goose.Tests.csproj:22-35` (add the `<None Include>`)
- Modify: `Goose.Tests/Fixtures/GlobalScriptFixture.cs:19-27` (add to `ShippedScripts`)
- Test: `Goose.Tests/DimensionRebirthTests.cs` (extend)

**Step 1: Write the failing tests** — append to `DimensionRebirthTests`:

```csharp
    [Fact]
    public void IsMet_only_at_or_above_the_threshold()
    {
        using var fixture = Seeded();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var requirement = fixture.World.QuestHandler.Get(RebirthQuestId)
            .Requirements.Single(r => r.Type == RequirementType.Script);

        var map = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(map, 1, 1);

        player.Experience = 99_999_999; player.ExperienceSold = 0;
        Assert.False(requirement.Script.Object.IsMet(requirement, player, fixture.World));

        player.Experience = 100_000_000;
        Assert.True(requirement.Script.Object.IsMet(requirement, player, fixture.World));

        // Split across both fields — the threshold is on the sum, as every other
        // experience gate in the codebase is (Map.cs:638, QuestWindow.cs:36).
        player.Experience = 50_000_000; player.ExperienceSold = 50_000_000;
        Assert.True(requirement.Script.Object.IsMet(requirement, player, fixture.World));
    }

    [Fact]
    public void GiveReward_mints_floor_of_total_and_resets_the_character()
    {
        using var fixture = Seeded();
        GameWorld.Settings.ChangeClassExperienceLossPercent = 0.07;
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        var reward = fixture.World.QuestHandler.Get(RebirthQuestId).Rewards.Single();
        var map = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(map, 1, 1);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 50;
        player.Experience = 250_000_000;
        player.ExperienceSold = 0;

        reward.Script.Object.GiveReward(reward, null, player, fixture.World);

        var spirit = fixture.World.CurrencyHandler.Get("spirit");
        Assert.Equal(2, spirit.GetBalance(player));       // floor(250M / 100M)
        Assert.Equal(0, player.Experience);               // remainder destroyed
        Assert.Equal(0, player.ExperienceSold);           // and no 7% shave to observe
        Assert.Equal(1, player.Level);
        Assert.Equal(1, player.ClassID);
    }

    /// <summary>Enabled = false leaves spirit unregistered. Resetting the player and
    /// minting nothing would be strictly worse than refusing.</summary>
    [Fact]
    public void CanComplete_refuses_when_spirit_is_not_registered()
    {
        using var fixture = new GlobalScriptFixture();

        // InstallShippedScripts FIRST. GetScript compiles from disk immediately
        // (Script.cs:26 -> LoadScript), so resolving the path before the copy throws
        // FileNotFoundException and the test never reaches its assertion.
        //
        // And deliberately no OnLoaded: this is the Enabled = false world, where
        // SpiritCurrency was never registered. Rebirth.csx must refuse rather than reset
        // the character and mint nothing.
        fixture.InstallShippedScripts();
        var script = fixture.World.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/Rebirth.csx");

        var reward = new QuestReward
        {
            Type = RewardType.Script,
            ScriptParams = "100000000",
            Script = script,
        };

        var map = fixture.AddBaseMap(9200, "No Currency Map");
        var player = fixture.PlayerOn(map, 1, 1);
        player.Experience = 500_000_000;

        var message = reward.Script.Object.CanComplete(reward, player, fixture.World);

        Assert.False(string.IsNullOrEmpty(message));
        Assert.Null(fixture.World.CurrencyHandler.Get("spirit"));
    }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter DimensionRebirthTests`
Expected: FAIL — `Rebirth.csx` is not in the test output directory.

**Step 3: Write the script and wire it into the test build**

`Goose/Data/Illutia/Scripts/Quest/Rebirth.csx`:

```csharp
using System;
using Goose;
using Goose.Quests;
using Goose.Scripting;

/// <summary>Rebirth: converts banked experience into spirit and resets the character.
///
/// Backs both the Script requirement (the threshold) and the Script reward (the whole
/// transaction) on the quest Dimensions.csx creates. One file serves both roles because
/// IQuestScript covers both.
///
/// All state change is in GiveReward, never in OnTakeRequirement: QuestWindow runs
/// TakeRequirements before GiveRewards (QuestWindow.cs:341-342), so consuming the
/// experience in the requirement would zero the number the reward has to read. The
/// requirement is registered with KeepRequirement = true for that reason.</summary>
public class Rebirth : BaseQuestScript
{
    private const string SpiritCurrencyId = "spirit";

    /// <summary>Read inside the call and never cached in a field - the IQuestScript
    /// contract, because one script instance is shared by every row pointing at it.</summary>
    private static long RateFrom(string scriptParams)
    {
        long rate;
        if (!long.TryParse(scriptParams, out rate) || rate <= 0)
            throw new Exception("Rebirth.csx: ScriptParams must be a positive experience-per-spirit rate.");

        return rate;
    }

    public override bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
    {
        return player.Experience + player.ExperienceSold >= RateFrom(requirement.ScriptParams);
    }

    public override string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
    {
        long rate = RateFrom(requirement.ScriptParams);
        long total = player.Experience + player.ExperienceSold;

        return string.Format("{0:N0} / {1:N0} experience", total, rate);
    }

    public override string CanComplete(QuestReward reward, Player player, GameWorld world)
    {
        if (world.CurrencyHandler.Get(SpiritCurrencyId) == null)
            return "The void is silent. Rebirth is not possible here.";

        if (player.Experience + player.ExperienceSold < RateFrom(reward.ScriptParams))
            return "You have not earned enough to be worth remaking.";

        return null;
    }

    public override void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
    {
        var spirit = world.CurrencyHandler.Get(SpiritCurrencyId);
        if (spirit == null) return;     // CanComplete already refused; belt and braces

        long rate = RateFrom(reward.ScriptParams);
        long total = player.Experience + player.ExperienceSold;
        long minted = total / rate;

        // Class 1 level 1 - Commoner. Hardcoded because .csx files compile separately and
        // this one cannot see Dimensions.RebirthDestinationClassId; CreateRebirthQuest
        // preflights the same pair against class_info, so keep the two in step.
        //
        // Explicit 0 loss: rebirth is an exchange, not the 7% penalty quest 60 charges.
        // ChangeClass does the rest - RemoveStats/AddStats, the MaxStats adjustment, the
        // level-1 class row, BaseStats.HP/MP = 0, Spellbook.RemoveNonClassSpells, the bind
        // reset, and the StatusInfo/ExpBar packets (Player.cs:1358-1400).
        player.ChangeClass(1, 1, world, 0d);

        // After ChangeClass, which banks Experience into ExperienceSold. The sub-rate
        // remainder is destroyed, faithful to RebirthEvent.java:47.
        player.Experience = 0;
        player.ExperienceSold = 0;

        spirit.Add(player, minted, world);

        world.Send(player, P.ServerMessage(string.Format(
            "You surrender {0:N0} experience and are remade. You gain {1:N0} spirit.", total, minted)));

        world.LogHandler.Log(Log.Types.Rebirth, player,
            string.Format("Rebirth: {0} experience -> {1} spirit", total, minted));
    }
}

return typeof(Rebirth);
```

> `return typeof(Rebirth);` — **the type, not an instance.** `Script<T>.LoadScript` casts
> the script's return value to `Type` and calls `Activator.CreateInstance` on it itself
> (`Goose/Scripting/Script.cs:44-46`), so returning `new Rebirth()` throws an
> `InvalidCastException` at load. Every shipped `.csx` does it this way — check
> `Scripts/Quest/DimensionUnlock.csx:33` and match it.

`Goose.Tests/Goose.Tests.csproj`, alongside the existing seven:

```xml
    <None Include="../Goose/Data/Illutia/Scripts/Quest/Rebirth.csx"
          Link="DimensionScripts/Rebirth.csx" CopyToOutputDirectory="PreserveNewest" />
```

`Goose.Tests/Fixtures/GlobalScriptFixture.cs`, in `ShippedScripts`:

```csharp
        ("Rebirth.csx",              "Scripts/Quest/Rebirth.csx"),
```

The fixture's own comment says to add to **both** lists together (`GlobalScriptFixture.cs:9-11`). Do that.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionRebirthTests`
Expected: PASS — all 11 tests (Task 3's 8 plus these 3), including every `CompileShipped`
one.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Quest/Rebirth.csx Goose.Tests/Goose.Tests.csproj Goose.Tests/Fixtures/GlobalScriptFixture.cs Goose.Tests/DimensionRebirthTests.cs
git commit -m "feat: Rebirth.csx — experience to spirit, character reset"
```

---

## Task 5: `/resetitem` and the guaranteed-suffix reroll

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Item/DimensionItem.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (config, `OnLoaded` registration, new event class)
- Modify: `Goose.Tests/Fixtures/GlobalScriptFixture.cs` (command-driving helpers)
- Test: `Goose.Tests/DimensionResetItemTests.cs` (create)

**Step 0: Command-driving helpers in the fixture**

Tasks 5, 6 and 8 all need the same thing: a ready, inventoried, message-capturing player,
and a way to run a script-registered command end to end. Add it once, to
`GlobalScriptFixture`, rather than three times:

```csharp
    /// <summary>Player.Send is virtual and returns early on a null socket (Player.cs:2389),
    /// so overriding it is how tests read the server's messages back.</summary>
    public sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new List<string>();
        public override void Send(string data) { this.Sent.Add(data); }
    }

    /// <summary>A logged-in-looking player: Ready state (every script command early-returns
    /// otherwise), an Inventory, and the BaseStats/MaxStats/Class trio PlayerOn already
    /// documents.</summary>
    public CapturingPlayer CommandPlayerOn(Map map, int x, int y, string name = "Tester")
    {
        var player = new CapturingPlayer
        {
            Name = name,
            Map = map, MapID = map.ID, MapX = x, MapY = y,
            State = Player.States.Ready,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = World.ClassHandler.GetClass(0),
        };
        player.Inventory = new Inventory(player);
        return player;
    }

    /// <summary>Makes PlayerHandler.GetPlayer(name) find this player, which is how /givesp
    /// resolves its target (PlayerHandler.cs:129). Not AddPlayer: that indexes by
    /// player.Sock (PlayerHandler.cs:51) and a socketless test player would throw on the
    /// null key. Same reflection approach as SeedClass.</summary>
    public void RegisterOnlinePlayer(Player player)
    {
        var byName = (Dictionary<string, Player>)typeof(PlayerHandler)
            .GetField("nameToPlayer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(World.PlayerHandler)!;
        byName[player.Name.ToLower()] = player;
    }

    /// <summary>Runs a chat-line command the way the server does: EventHandler.AddEvent
    /// parses it against the registered trie and queues an Event, and Update dequeues and
    /// calls Ready (EventHandler.cs:286,:361-371). Going through both is the point - it is
    /// what proves the trailing-space registration actually matches.</summary>
    public bool RunCommand(Player player, string packet)
    {
        if (!World.EventHandler.AddEvent(player, packet)) return false;

        World.EventHandler.Update(World);
        return true;
    }
```

`VendorFixture` has its own `CapturingPlayer` (`VendorFixture.cs:17-22`); leave it alone,
its tests name it. This is a fourth copy of a five-line idiom, which is cheaper than
rewriting three suites.

**Step 1: Write the failing tests**

```csharp
using System.Linq;
using Goose;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionResetItemTests
{
    private const int Offset = 100000;   // Dimensions.csx:19

    /// <summary>DimensionItemScriptTests.Run (`:12-20`) plus the pieces a command needs: a
    /// vendor-less town map, a Ready player holding items, and spirit already registered by
    /// OnLoaded.</summary>
    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded(
        long spiritBalance = 10_000)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        fixture.AddBaseItemTemplate(51, "Potion", ItemTemplate.UseTypes.OneTime, t =>
        {
            t.Value = 10;
            t.StackSize = 20;
        });
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 5, 5);
        player.Properties.SetProperty("dimension.max", 6);
        fixture.World.CurrencyHandler.Get("spirit").Add(player, spiritBalance, fixture.World);
        player.Sent.Clear();

        return (fixture, player);
    }

    private static Item Carry(GlobalScriptFixture fixture, Player player, int templateId, int stack = 1)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(templateId));
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        player.Inventory.AddItem(item, stack, fixture.World);
        return item;
    }

    private static long Spirit(GlobalScriptFixture fixture, Player player)
        => fixture.World.CurrencyHandler.Get("spirit").GetBalance(player);

    // ---- the reroll hook itself -----------------------------------------

    /// <summary>A drop rolls a 45% suffix chance; a paid reroll always lands one. That
    /// asymmetry is the reason OnRerollModifiersEvent exists separately from
    /// OnRollModifiersEvent. 200 iterations against a 45% chance makes a false pass
    /// vanishingly unlikely.</summary>
    [Fact]
    public void Reroll_always_lands_a_suffix()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        for (int i = 0; i < 200; i++)
        {
            var item = new Item();
            item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(50 + Offset * 3));

            fixture.World.ItemHandler.RerollModifiers(item, fixture.World);

            Assert.True(item.HasProperty(ItemProperty.SurnameId), $"iteration {i} rolled no suffix");
        }
    }

    /// <summary>ResetModifiers runs first, so a second reroll replaces rather than appends.
    /// The name is the visible symptom; the stats are the one that matters.</summary>
    [Fact]
    public void Reroll_clears_the_previous_suffix_rather_than_stacking()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;

        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(50 + Offset * 3));

        for (int i = 0; i < 20; i++)
        {
            fixture.World.ItemHandler.RerollModifiers(item, fixture.World);

            // One base name plus exactly one " of the ..." suffix, never two.
            Assert.StartsWith(item.Template.Name, item.Name);
            Assert.Equal(1, item.Name.Split(" of ").Length - 1);
        }
    }

    // ---- the command ----------------------------------------------------

    [Fact]
    public void Charges_three_to_the_dimension()
    {
        foreach (var (dim, cost) in new[] { (1, 3L), (3, 27L), (6, 729L) })
        {
            var (fixture, player) = Loaded();
            using var _ = fixture;
            Carry(fixture, player, 50 + Offset * dim);
            var before = Spirit(fixture, player);

            Assert.True(fixture.RunCommand(player, "/resetitem 1"));

            Assert.Equal(before - cost, Spirit(fixture, player));
        }
    }

    /// <summary>Every refusal, in one table, all asserting the same thing: the balance did
    /// not move and the item did not change. Part 5 established that a Remove call is not
    /// itself a guard, so "refusals charge nothing" is the property under test, not a
    /// nicety.</summary>
    [Theory]
    // slot parsing
    [InlineData("/resetitem ", "empty argument")]
    [InlineData("/resetitem abc", "unparseable slot")]
    [InlineData("/resetitem 0", "slot below range")]
    [InlineData("/resetitem 999", "slot above InventorySize")]
    [InlineData("/resetitem 2", "empty slot")]
    public void Refuses_bad_slots_and_charges_nothing(string command, string why)
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 50 + Offset * 2);
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, command);

        var item = player.Inventory.GetSlot(1).Item;
        Assert.Equal(before, Spirit(fixture, player));
        // No reroll ran, so the item still carries its clone name with no suffix. (Clones
        // are named PrefixFor(dim) + base name, Dimensions.csx:987 — never assert a literal
        // "Sword" against a dimension template.) Refused because: why
        Assert.Equal(item.Template.Name, item.Name);
    }

    [Fact]
    public void Refuses_a_non_dimension_item_and_charges_nothing()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 50);         // the base template, dim 0
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Contains(player.Sent, m => m.Contains("higher plane"));
    }

    /// <summary>Two ways to be a high-id item that is not a generated clone, and the
    /// division alone catches neither. `50 + Offset*9` divides to dimension 9, which does
    /// not exist — Math.Pow(3, 9) would price it at 19,683. `77 + Offset*2` divides to a
    /// real dimension but has no base template behind it, so nothing cloned it and no
    /// dimension script is attached; the reroll hook would decline and the native
    /// chance-based roll would run on an item the player just paid 9 spirit for.</summary>
    [Theory]
    [InlineData(50 + Offset * 9)]
    [InlineData(77 + Offset * 2)]
    public void Refuses_a_high_id_template_that_is_not_a_generated_clone_and_charges_nothing(int templateId)
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        // Registered after OnLoaded, so nothing cloned it and it has no dimension script.
        fixture.AddBaseItemTemplate(templateId, "Impostor", ItemTemplate.UseTypes.Weapon);
        Carry(fixture, player, templateId);
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Equal("Impostor", player.Inventory.GetSlot(1).Item.Name);
        Assert.Contains(player.Sent, m => m.Contains("higher plane"));
    }

    [Fact]
    public void Refuses_a_tome_or_other_non_equipment_and_charges_nothing()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 51 + Offset * 2);    // the OneTime potion clone
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Contains(player.Sent, m => m.Contains("weapons and armor"));
    }

    /// <summary>One Item backs a whole stack (ItemSlot.cs:17-19), so a reroll on a stack of
    /// two rewrites both for the price of one and hands the player a free copy.</summary>
    [Fact]
    public void Refuses_a_stacked_slot_and_charges_nothing()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        // Force a stackable dimension weapon: StackSize travels to the clone, so raise it
        // on the base before OnLoaded is not possible here — set it on the clone directly.
        fixture.World.ItemHandler.GetTemplate(50 + Offset * 2).StackSize = 10;
        Carry(fixture, player, 50 + Offset * 2, stack: 2);
        var before = Spirit(fixture, player);

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(before, Spirit(fixture, player));
        Assert.Equal(2, player.Inventory.GetSlot(1).Stack);
    }

    [Fact]
    public void Refuses_when_the_balance_is_short_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 700);
        using var _ = fixture;
        Carry(fixture, player, 50 + Offset * 6);        // costs 729

        fixture.RunCommand(player, "/resetitem 1");

        Assert.Equal(700, Spirit(fixture, player));
        Assert.Contains(player.Sent, m => m.Contains("Not enough spirit"));
    }

    /// <summary>A dedicated log type, not CreatedCustom: an economy audit has to be able to
    /// separate player rerolls from GM item creation.</summary>
    [Fact]
    public void Logs_a_reset_item_entry_with_the_cost()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        Carry(fixture, player, 50 + Offset * 2);

        fixture.RunCommand(player, "/resetitem 1");

        var entry = Assert.Single(fixture.World.LogHandler.Pending, l => l.Type == Log.Types.ResetItem);
        Assert.Contains("9", entry.Text);       // 3^2
    }
}
```

> `LogHandler` keeps its entries in a private `logs` list until `SaveToDatabase`
> (`LogHandler.cs:16-25`). Add a `public IReadOnlyList<Log> Pending => this.logs;` to
> `LogHandler` for that last test rather than reflecting into it — a read-only view of a
> write-only buffer is a reasonable thing for the class to expose, and Task 6 needs it too.
> If `Log` does not surface `Type`/`Text` publicly, add those getters at the same time.

> The rest: read `Goose.Tests/DimensionItemScriptTests.cs` first. It already builds
> dimension items and drives `DimensionItem.csx`; `Loaded()` above is its `Run` helper with
> a player bolted on.

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter DimensionResetItemTests`
Expected: FAIL.

**Step 3: Implement**

In `DimensionItem.csx`, add alongside `OnRollModifiersEvent`. Read that method first and **extract the shared roll** rather than copying it — the only difference is that the suffix is unconditional:

```csharp
    /// <summary>A paid reroll: the suffix is guaranteed, where a drop rolls 45%. The
    /// rarity roll is unchanged (2% Legendary / 2% Stunted, independent of the suffix).
    ///
    /// ItemHandler.RerollModifiers has already reset the item to template state, so this
    /// only applies - it never has to strip anything first.</summary>
    public override bool OnRerollModifiersEvent(Item item, GameWorld world)
    {
        if (DimensionOf(item) <= 0) return false;
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
            return false;

        // Guaranteed, where OnRollModifiersEvent gates it behind roll >= 0.55.
        ApplySuffix(item, world, world.Random.Next(6));
        ApplyRarity(item, world);
        return true;
    }
```

> `DimensionOf(item)` is the script's own helper (`DimensionItem.csx:19`), built on its own
> `private const int Offset = 100000` — `DimensionItem.csx` cannot reference
> `Dimensions.Offset`, because `.csx` files compile separately. Use the existing helper;
> do not add a second offset constant.
>
> `ApplySuffix` and `ApplyRarity` do not exist yet. Extract them from
> `OnRollModifiersEvent` (`DimensionItem.csx:28-50`) so both hooks share one definition —
> `ApplySuffix(item, world, index)` wrapping the `Apply(GetSurname(SurnameIdBase + index),
> ..., prefix: false)` call, and `ApplyRarity(item, world)` wrapping the independent 2%/2%
> title roll. `OnRollModifiersEvent` keeps its `roll >= 0.55` gate and its band arithmetic
> and calls into them; the reroll skips the gate. Copying the bodies instead is how the two
> paths drift.

In `Dimensions.csx` config:

```csharp
    /// <summary>Reroll cost is ResetItemCostBase^dim: 3/9/27/81/243/729 spirit
    /// (ResetItemEvent.java:30).</summary>
    public const int ResetItemCostBase = 3;
```

In `OnLoaded`, beside the `/dimension` registration:

```csharp
        world.EventHandler.RegisterEvent("/resetitem ", ResetItemCommandEvent.Create);
```

The event class, modelled on `DimensionCommandEvent` (`Dimensions.csx:1084`):

```csharp
public class ResetItemCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new ResetItemCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        int slotId;
        if (tokens.Length < 2 || !int.TryParse(tokens[1], out slotId) ||
            slotId < 1 || slotId > GameWorld.Settings.InventorySize)
        {
            world.Send(this.Player, P.ServerMessage(
                "/resetitem <1-" + GameWorld.Settings.InventorySize + "> - rerolls a dimension item's suffix."));
            return;
        }

        var slot = this.Player.Inventory.GetSlot(slotId);
        if (slot == null || slot.Item == null)
        {
            world.Send(this.Player, P.ServerMessage("No item exists at that inventory slot."));
            return;
        }

        var item = slot.Item;

        // One Item object backs the whole stack (ItemSlot.cs:17-19), so rerolling a stack
        // of two would rewrite both for one charge. Refuse rather than split.
        if (slot.Stack != 1)
        {
            world.Send(this.Player, P.ServerMessage("Only a single item can be reset, not a stack."));
            return;
        }

        // Three separate questions, and all three have to be asked. The division alone
        // says nothing: a sheet-authored template with an id above Offset would divide to a
        // plausible-looking dimension, be priced with Math.Pow against a dimension that may
        // not exist, and be handed to a reroll hook that knows nothing about it.
        int dim = item.TemplateID / Dimensions.Offset;
        if (dim < 1 || dim > Dimensions.DimensionCount)
        {
            world.Send(this.Player, P.ServerMessage("Only items from a higher plane can be reset."));
            return;
        }

        // CloneItemTemplates registers each clone at baseId + Offset*dim over a base that
        // exists, and stamps the dimension script onto it. All three must hold, or this is
        // not a generated clone and does not belong here.
        var registered = world.ItemHandler.GetTemplate(item.TemplateID);
        if (registered == null || registered != item.Template ||
            world.ItemHandler.GetTemplate(item.TemplateID % Dimensions.Offset) == null ||
            registered.Script == null)
        {
            world.Send(this.Player, P.ServerMessage("Only items from a higher plane can be reset."));
            return;
        }

        // Dimension tomes are OneTime consumables; nothing but gear carries modifiers.
        if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
        {
            world.Send(this.Player, P.ServerMessage("Only weapons and armor can be reset."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long cost = (long)Math.Pow(Dimensions.ResetItemCostBase, dim);

        // The balance check is the guard, not a nicety: Part 5 established that Remove
        // does not itself refuse an overdraft.
        long before = spirit.GetBalance(this.Player);
        if (before < cost)
        {
            world.Send(this.Player, P.ServerMessage(
                "Not enough " + spirit.Name + " to reset this item. (" + cost + ")"));
            return;
        }

        world.ItemHandler.RerollModifiers(item, world);
        spirit.Remove(this.Player, cost, world);

        this.Player.Inventory.SendSlot(slotId, world);
        world.Send(this.Player, P.ServerMessage(
            "You spend " + cost + " " + spirit.Name + " to remake " + item.Name + "."));

        // Its own log type, not CreatedCustom: that is the GM item-creation log, and
        // folding a paid player reroll into it makes both unqueryable. otherid carries the
        // item's id so a reroll can be joined to the item it rewrote.
        world.LogHandler.Log(Log.Types.ResetItem, this.Player,
            "ResetItem: template " + item.TemplateID + " dim " + dim
            + " cost " + cost + " " + spirit.ShortName
            + " balance " + before + " -> " + (before - cost),
            item.ItemID);
    }
}
```

> `Log(Log.Types, Player, string, int otherid = 0)` is the four-argument overload at
> `LogHandler.cs:37`; it fills mapid/mapx/mapy from the player itself. Confirm the item's
> id property name on `Item` before writing `item.ItemID`.

> Two things to verify as you write this: whether `P.ServerMessage` and `Player.States.Ready` are reachable the same way `DimensionCommandEvent` reaches them, and whether the player's HP/MP status needs resending after the reroll (a suffix moves `MaxHP` — check what `Inventory.SendSlot` already sends and add `P.StatusInfo` only if it does not).

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionResetItemTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Item/DimensionItem.csx Goose/Data/Illutia/Scripts/Global/Dimensions.csx \
        Goose/LogHandler.cs Goose.Tests/Fixtures/GlobalScriptFixture.cs Goose.Tests/DimensionResetItemTests.cs
git commit -m "feat: /resetitem with a guaranteed-suffix reroll"
```

---

## Task 6: `/buygold`, `/buyexperience`, `/givesp`

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionCurrencyCommandTests.cs` (create)

Uses Task 5's `CommandPlayerOn` / `RegisterOnlinePlayer` / `RunCommand` fixture helpers
and `LogHandler.Pending`. Do Task 5 first.

**Step 1: Write the failing tests**

```csharp
using System.Linq;
using Goose;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionCurrencyCommandTests
{
    private const long GoldPerSpirit = 1_000_000;
    private const long ExpPerSpiritPurchase = 25_000_000;

    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded(
        long spiritBalance = 100)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 5, 5, "Alice");
        // Not a commoner: /buyexperience refuses class 1.
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 10;
        fixture.RegisterOnlinePlayer(player);
        fixture.World.CurrencyHandler.Get("spirit").Add(player, spiritBalance, fixture.World);
        player.Sent.Clear();

        return (fixture, player);
    }

    private static long Spirit(GlobalScriptFixture fixture, Player player)
        => fixture.World.CurrencyHandler.Get("spirit").GetBalance(player);

    // ---- /buygold -------------------------------------------------------

    [Fact]
    public void BuyGold_trades_spirit_for_gold_at_the_configured_rate()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        player.Gold = 0;

        Assert.True(fixture.RunCommand(player, "/buygold 3"));

        Assert.Equal(7, Spirit(fixture, player));
        Assert.Equal(3 * GoldPerSpirit, player.Gold);
        Assert.Single(fixture.World.LogHandler.Pending, l => l.Type == Log.Types.BuyGold);
    }

    [Theory]
    [InlineData("/buygold ")]
    [InlineData("/buygold abc")]
    [InlineData("/buygold 0")]
    [InlineData("/buygold -5")]
    public void BuyGold_refuses_bad_amounts_and_charges_nothing(string command)
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        player.Gold = 0;

        fixture.RunCommand(player, command);

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(0, player.Gold);
        Assert.Empty(fixture.World.LogHandler.Pending.Where(l => l.Type == Log.Types.BuyGold));
    }

    [Fact]
    public void BuyGold_refuses_an_insufficient_balance_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 2);
        using var _ = fixture;
        player.Gold = 0;

        fixture.RunCommand(player, "/buygold 3");

        Assert.Equal(2, Spirit(fixture, player));
        Assert.Equal(0, player.Gold);
        Assert.Contains(player.Sent, m => m.Contains("Not enough spirit"));
    }

    // ---- /buyexperience -------------------------------------------------

    [Fact]
    public void BuyExperience_grants_exactly_the_unmodified_amount()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 0;
        player.Experience = 0;

        Assert.True(fixture.RunCommand(player, "/buyexperience 2"));

        Assert.Equal(8, Spirit(fixture, player));
        Assert.Equal(2 * ExpPerSpiritPurchase, player.Experience);
    }

    /// <summary>The modifier must not touch purchased experience — that is the entire
    /// reason for Task 1's applyModifiers overload. Both branches of the two-branch scaling
    /// (Player.cs:1662-1671) are covered: limit 0 takes the full-modifier branch, a limit
    /// the player is already past takes the reduced one.</summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(1_000L)]
    public void BuyExperience_is_unaffected_by_the_world_experience_modifier(long modifierLimit)
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 0;
        GameWorld.Settings.ExperienceModifier = 2;
        GameWorld.Settings.ExperienceModifierLimit = modifierLimit;
        fixture.World.ExperienceModifier = 2;
        player.Experience = 50_000;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(50_000 + ExpPerSpiritPurchase, player.Experience);
    }

    [Fact]
    public void BuyExperience_refuses_commoners_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        player.ClassID = 1;
        player.Class = fixture.World.ClassHandler.GetClass(1);
        player.Experience = 0;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(0, player.Experience);
        Assert.Contains(player.Sent, m => m.Contains("Choose a class"));
    }

    /// <summary>AddExperience early-returns over the cap (Player.cs:1653), which would take
    /// the spirit and grant nothing.</summary>
    [Fact]
    public void BuyExperience_refuses_when_already_over_the_cap_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 1_000_000;
        player.Experience = 2_000_000;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(2_000_000, player.Experience);
    }

    /// <summary>The prospective check, and the reason the current-total check alone is not
    /// enough. A player one experience under the cap passes "am I over the cap?" and then
    /// buys 25,000,000 — landing 24,999,999 above a ceiling the server is supposed to
    /// enforce. The cap has to be tested against the total the purchase would produce, not
    /// the total the player has now.</summary>
    [Fact]
    public void BuyExperience_refuses_a_purchase_that_would_cross_the_cap_and_charges_nothing()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 100_000_000;
        player.Experience = 99_999_999;
        player.ExperienceSold = 0;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(99_999_999, player.Experience);
        Assert.Contains(player.Sent, m => m.Contains("experience cap"));
    }

    /// <summary>The largest purchase that still lands on or under the cap must go through —
    /// a prospective check that also refuses the legitimate last purchase is a regression,
    /// not a fix.</summary>
    [Fact]
    public void BuyExperience_allows_a_purchase_that_lands_exactly_on_the_cap()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 100_000_000;
        player.Experience = 100_000_000 - ExpPerSpiritPurchase;
        player.ExperienceSold = 0;

        fixture.RunCommand(player, "/buyexperience 1");

        Assert.Equal(9, Spirit(fixture, player));
        Assert.Equal(100_000_000, player.Experience);
    }

    /// <summary>amount * ExpPerSpiritPurchase is a long multiply on an amount the player
    /// controls. Without a guard it wraps negative and AddExperience subtracts.</summary>
    [Fact]
    public void BuyExperience_refuses_an_amount_that_would_overflow_the_multiply()
    {
        var (fixture, player) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        GameWorld.Settings.ExperienceCap = 0;
        player.Experience = 0;

        fixture.RunCommand(player, "/buyexperience 9223372036854775807");

        Assert.Equal(10, Spirit(fixture, player));
        Assert.Equal(0, player.Experience);
    }

    // ---- /givesp --------------------------------------------------------

    [Fact]
    public void GiveSp_moves_the_balance_between_two_players()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        Assert.True(fixture.RunCommand(alice, "/givesp Bob 40"));

        Assert.Equal(60, Spirit(fixture, alice));
        Assert.Equal(40, Spirit(fixture, bob));
        Assert.Contains(bob.Sent, m => m.Contains("Alice"));
    }

    /// <summary>Both sides of a transfer, joinable. otherid carries the counterparty's
    /// PlayerID and the text carries before/after for each wallet, so an audit can prove a
    /// transfer conserved spirit without replaying every log in between.</summary>
    [Fact]
    public void GiveSp_logs_both_sides_with_the_counterparty_and_balances()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        bob.PlayerID = 77;
        fixture.RegisterOnlinePlayer(bob);

        fixture.RunCommand(alice, "/givesp Bob 40");

        var entries = fixture.World.LogHandler.Pending
            .Where(l => l.Type == Log.Types.GiveSpirit).ToList();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, l => l.PlayerID == alice.PlayerID && l.OtherID == 77
                                      && l.Text.Contains("100 -> 60"));
        Assert.Contains(entries, l => l.PlayerID == 77 && l.OtherID == alice.PlayerID
                                      && l.Text.Contains("0 -> 40"));
    }

    [Theory]
    [InlineData("/givesp ")]
    [InlineData("/givesp Bob")]
    [InlineData("/givesp Bob abc")]
    [InlineData("/givesp Bob 0")]
    [InlineData("/givesp Bob -5")]
    public void GiveSp_refuses_bad_arguments_and_moves_nothing(string command)
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        fixture.RunCommand(alice, command);

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Equal(0, Spirit(fixture, bob));
    }

    [Fact]
    public void GiveSp_refuses_an_offline_target_and_moves_nothing()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;

        fixture.RunCommand(alice, "/givesp Nobody 40");

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Contains(alice.Sent, m => m.Contains("not online"));
    }

    /// <summary>Self-transfer is not a no-op if it is allowed: Remove then Add both run
    /// AddStats/RemoveStats against the same wallet, and any asymmetry between them mints
    /// or burns spirit.</summary>
    [Fact]
    public void GiveSp_refuses_a_self_transfer()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;

        fixture.RunCommand(alice, "/givesp Alice 40");

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Contains(alice.Sent, m => m.Contains("yourself"));
    }

    [Fact]
    public void GiveSp_refuses_an_insufficient_balance_and_moves_nothing()
    {
        var (fixture, alice) = Loaded(spiritBalance: 10);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        fixture.RunCommand(alice, "/givesp Bob 40");

        Assert.Equal(10, Spirit(fixture, alice));
        Assert.Equal(0, Spirit(fixture, bob));
        Assert.Contains(alice.Sent, m => m.Contains("Not enough spirit"));
    }

    /// <summary>The recipient side. BaseStats.SP is a long (AttributeSet.cs:16), so a
    /// transfer into an already-huge wallet wraps negative and destroys the balance — and a
    /// wallet past MaxSpiritBalance is past what the rest of the economy was sized for.
    /// The check is on the recipient, before either side moves.</summary>
    [Fact]
    public void GiveSp_refuses_when_the_recipient_would_exceed_the_cap_and_moves_nothing()
    {
        var (fixture, alice) = Loaded(spiritBalance: 100);
        using var _ = fixture;
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1), 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);
        // One under the cap, so any positive transfer crosses it.
        fixture.World.CurrencyHandler.Get("spirit").Add(bob, 1_000_000_000_000L - 1, fixture.World);

        fixture.RunCommand(alice, "/givesp Bob 40");

        Assert.Equal(100, Spirit(fixture, alice));
        Assert.Equal(1_000_000_000_000L - 1, Spirit(fixture, bob));
        Assert.Contains(alice.Sent, m => m.Contains("cannot hold"));
    }
}
```

> Three things this needs from the server side, all of them small:
> - `LogHandler.Pending` (Task 5 adds it).
> - `Log.PlayerID` / `Log.OtherID` / `Log.Text` readable. Check `Goose/Log.cs` — the
>   constructor takes all three (`LogHandler.cs:27`); if the fields are private, make them
>   public getters.
> - `GameWorld.ExperienceModifier` settable (Task 1 already depends on this).

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter DimensionCurrencyCommandTests`
Expected: FAIL — `AddEvent` returns false for all three commands, so `RunCommand` returns
false and every balance assertion that expects movement fails.

**Step 3: Implement**

Config:

```csharp
    /// <summary>BuyGoldCommandEvent.java:47 - 1 spirit buys a million gold.</summary>
    public const long GoldPerSpirit = 1_000_000;

    /// <summary>BuyExperienceCommandEvent.java:52. Deliberately below ExpPerSpirit: the
    /// round trip is lossy by 4x, which is what keeps rebirth a net sink.</summary>
    public const long ExpPerSpiritPurchase = 25_000_000;

    /// <summary>Ceiling on a single wallet. BaseStats.SP is a long, so this is not the
    /// type's limit - it is a sanity bound well above anything the faucet can produce
    /// (a trillion spirit is 10^20 experience through rebirth), placed so a transfer
    /// cannot silently wrap a wallet negative and so a bug in the faucet is visible as a
    /// refusal rather than as a corrupted balance.</summary>
    public const long MaxSpiritBalance = 1_000_000_000_000L;

    /// <summary>Shared by all four commands. Returns false for a missing, unparseable,
    /// zero or negative amount - each command prints its own usage line, so this does not
    /// message.</summary>
    public static bool TryParseAmount(string[] tokens, int index, out long amount)
    {
        amount = 0;
        if (tokens.Length <= index) return false;
        if (!long.TryParse(tokens[index], out amount)) return false;

        return amount > 0;
    }
```

Registrations in `OnLoaded`, beside `/resetitem`:

```csharp
        world.EventHandler.RegisterEvent("/buygold ", BuyGoldCommandEvent.Create);
        world.EventHandler.RegisterEvent("/buyexperience ", BuyExperienceCommandEvent.Create);
        world.EventHandler.RegisterEvent("/givesp ", GiveSpiritCommandEvent.Create);
```

> Check the trailing-space convention against `Dimensions.csx:121` (`"/dimension "`) and
> `EventHandler.cs`'s trie. Commands taking arguments register with the trailing space; get
> this wrong and the command silently never fires. `/buygold` is a strict prefix of nothing
> else here, but note that `/buyexperience ` and `/buygold ` share the `/buy` prefix — the
> trie is longest-prefix (`EventHandler.cs:123`), so both resolve correctly; do not
> shorten either registration.

`/buygold`:

```csharp
public class BuyGoldCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new BuyGoldCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (!Dimensions.TryParseAmount(tokens, 1, out amount))
        {
            world.Send(this.Player, P.ServerMessage(
                "/buygold <amount> - trades spirit for gold at "
                + Dimensions.GoldPerSpirit.ToString("N0") + " each."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        var gold = world.CurrencyHandler.Get(Currency.Gold);   // no CurrencyHandler.Gold property
        if (spirit == null || gold == null) return;

        // Before the balance check: a wrapped product would pass any check made after it.
        if (amount > long.MaxValue / Dimensions.GoldPerSpirit)
        {
            world.Send(this.Player, P.ServerMessage("That is more gold than exists."));
            return;
        }

        long before = spirit.GetBalance(this.Player);
        if (before < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        long granted = amount * Dimensions.GoldPerSpirit;

        spirit.Remove(this.Player, amount, world);
        gold.Add(this.Player, granted, world);

        world.Send(this.Player, P.ServerMessage(
            "You trade " + amount + " " + spirit.Name + " for " + granted.ToString("N0") + " gold."));
        world.LogHandler.Log(Log.Types.BuyGold, this.Player,
            "BuyGold: " + amount + " " + spirit.ShortName + " -> " + granted + " gold"
            + ", spirit " + before + " -> " + (before - amount));
    }
}
```

`/buyexperience` is the same skeleton with three extra refusals, all of them **before**
anything is charged:

```csharp
        if (this.Player.ClassID == 1)
        {
            world.Send(this.Player, P.ServerMessage("Choose a class before you buy experience."));
            return;
        }

        if (amount > long.MaxValue / Dimensions.ExpPerSpiritPurchase)
        {
            world.Send(this.Player, P.ServerMessage("That is more experience than exists."));
            return;
        }

        long granted = amount * Dimensions.ExpPerSpiritPurchase;
        long total = this.Player.Experience + this.Player.ExperienceSold;

        // Prospective, not current. AddExperience early-returns when the CURRENT total is
        // over the cap (Player.cs:1653-1660), so checking the same condition here only
        // catches players who are already past it - a player one experience under the cap
        // passes, buys, and lands 24,999,999 above a ceiling the server is meant to
        // enforce. Test what the purchase would produce.
        if (GameWorld.Settings.ExperienceCap > 0 && total + granted > GameWorld.Settings.ExperienceCap)
        {
            long affordable = (GameWorld.Settings.ExperienceCap - total) / Dimensions.ExpPerSpiritPurchase;
            world.Send(this.Player, P.ServerMessage(affordable > 0
                ? "That would carry you past the experience cap. You can buy at most " + affordable + "."
                : "You have reached the experience cap."));
            return;
        }

        // ... balance check, as /buygold ...

        spirit.Remove(this.Player, amount, world);
        this.Player.AddExperience(granted, world, Player.ExperienceMessage.Normal, applyModifiers: false);
```

> `total + granted` cannot itself overflow given the guard above plus `MaxSpiritBalance`,
> but if you raise either constant, revisit this line.

`/givesp` takes `<player> <amount>` — note the amount is token 2, not token 1:

```csharp
public class GiveSpiritCommandEvent : Event
{
    public static Event Create(Player player, Object data)
    {
        return new GiveSpiritCommandEvent { Player = player, Data = data };
    }

    public override void Ready(GameWorld world)
    {
        if (this.Player.State != Player.States.Ready) return;

        var tokens = ((string)this.Data).Split(' ');
        long amount;
        if (tokens.Length < 3 || !Dimensions.TryParseAmount(tokens, 2, out amount))
        {
            world.Send(this.Player, P.ServerMessage("/givesp <player> <amount>"));
            return;
        }

        var target = world.PlayerHandler.GetPlayer(tokens[1]);
        if (target == null || target.State != Player.States.Ready)
        {
            world.Send(this.Player, P.ServerMessage(tokens[1] + " is not online."));
            return;
        }

        if (target == this.Player)
        {
            world.Send(this.Player, P.ServerMessage("You cannot give spirit to yourself."));
            return;
        }

        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        if (spirit == null) return;

        long senderBefore = spirit.GetBalance(this.Player);
        if (senderBefore < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        // The recipient side, checked before either wallet moves. BaseStats.SP is a long,
        // so a transfer into a large enough wallet wraps negative; MaxSpiritBalance keeps
        // the refusal well short of that and makes a faucet bug visible as a refusal
        // rather than a corrupted balance.
        long targetBefore = spirit.GetBalance(target);
        if (targetBefore > Dimensions.MaxSpiritBalance - amount)
        {
            world.Send(this.Player, P.ServerMessage(target.Name + " cannot hold that much " + spirit.Name + "."));
            return;
        }

        spirit.Remove(this.Player, amount, world);
        spirit.Add(target, amount, world);

        world.Send(this.Player, P.ServerMessage(
            "You give " + amount + " " + spirit.Name + " to " + target.Name + "."));
        world.Send(target, P.ServerMessage(
            this.Player.Name + " gives you " + amount + " " + spirit.Name + "."));

        // One entry per side, each naming the counterparty in otherid and carrying its own
        // before/after. Two rows rather than one because logs are queried per player.
        world.LogHandler.Log(Log.Types.GiveSpirit, this.Player,
            "GiveSpirit: sent " + amount + " " + spirit.ShortName + " to " + target.Name
            + ", balance " + senderBefore + " -> " + (senderBefore - amount),
            target.PlayerID);
        world.LogHandler.Log(Log.Types.GiveSpirit, target,
            "GiveSpirit: received " + amount + " " + spirit.ShortName + " from " + this.Player.Name
            + ", balance " + targetBefore + " -> " + (targetBefore + amount),
            this.Player.PlayerID);
    }
}
```

> `tokens[1]` is used verbatim as the player name. `PlayerHandler.GetPlayer` lowercases
> before lookup (`PlayerHandler.cs:131`), so no casing work is needed here — but a name
> with a space would arrive split, and `GetPlayer` would miss it. That matches how
> `/tell` behaves today; do not invent a different rule.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionCurrencyCommandTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose/Log.cs Goose.Tests/DimensionCurrencyCommandTests.cs
git commit -m "feat: /buygold, /buyexperience and /givesp spirit commands"
```

---

## Task 7: `RepointVendorStock`

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (new pass, `OnLoaded`)
- Test: `Goose.Tests/DimensionVendorStockTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
using System.Linq;
using Goose;
using Goose.Events;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionVendorStockTests
{
    private const int Offset = 100000;   // Dimensions.csx:19

    private const int SwordId = 50;      // cloned: gear, priced in spirit
    private const int PotionId = 51;     // cloned: a consumable, still gold at the till
    private const int MerchantId = 60;

    /// <summary>A base map with a vendor NPC standing on it, stocking a weapon, a
    /// consumable, and one item that has no clone. OnLoaded then clones the world.</summary>
    private static GlobalScriptFixture Loaded()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        fixture.AddBaseItemTemplate(SwordId, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        fixture.AddBaseItemTemplate(PotionId, "Potion", ItemTemplate.UseTypes.OneTime, t =>
        {
            t.Value = 10;
            t.StackSize = 20;
        });

        var merchant = new NPCTemplate
        {
            NPCTemplateID = MerchantId, Name = "Merchant", Level = 1, ClassID = 3,
            NPCType = NPCTemplate.Types.Vendor,
            CanBeKilled = false, CanMove = false,
            AttackSpeed = 1m, MoveSpeed = 1m,
            AlliesString = "", Allies = new List<NPCTemplate>(), Drops = new List<NPCDropInfo>(),
            BaseStats = new AttributeSet { HP = 100 },
        };
        // NPCHandler.LoadNPCs sizes the array VendorSlotSize + 1 and leaves index 0 null
        // (NPCHandler.cs:183-197). Mirror that, so the repoint pass sees a realistic array.
        merchant.VendorItems = new NPCVendorSlot[GameWorld.Settings.VendorSlotSize + 1];
        merchant.VendorItems[1] = new NPCVendorSlot
        {
            Slot = 1, ItemTemplate = fixture.World.ItemHandler.GetTemplate(SwordId),
            Stack = 1, CanSeeStats = true,
        };
        merchant.VendorItems[2] = new NPCVendorSlot
        {
            Slot = 2, ItemTemplate = fixture.World.ItemHandler.GetTemplate(PotionId),
            Stack = 5, CanSeeStats = false,
        };
        fixture.World.NPCHandler.AddTemplate(merchant);
        fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 20, 20, merchant, shouldRespawn: false);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    private static NPCVendorSlot[] StockOf(GlobalScriptFixture fixture, int dim)
        => fixture.World.NPCHandler.GetNPCTemplate(MerchantId + Offset * dim).VendorItems;

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void Dimension_vendor_slots_point_at_same_dimension_clones(int dim)
    {
        using var fixture = Loaded();

        var stock = StockOf(fixture, dim);

        Assert.Equal(SwordId + Offset * dim, stock[1].ItemTemplate.ID);
        Assert.Equal(PotionId + Offset * dim, stock[2].ItemTemplate.ID);
    }

    /// <summary>Nothing cloned an item registered after OnLoaded, so its slot must keep the
    /// base template rather than becoming a null hole in the shop.</summary>
    [Fact]
    public void Slots_with_no_clone_keep_the_base_template()
    {
        using var fixture = new GlobalScriptFixture();
        // Same arrangement as Loaded(), but with a third slot whose template exists only
        // after cloning has run. Easiest: build the world with Loaded(), then assert
        // against a slot the pass could not resolve — see the note below.
        var f = Loaded();
        using var _ = f;

        var orphan = f.AddBaseItemTemplate(99, "Orphan", ItemTemplate.UseTypes.Weapon);
        var stock = StockOf(f, 2);
        // Rerunning the pass is not possible from the test, so assert the property the pass
        // guarantees instead: every slot resolves to a non-null template.
        Assert.All(stock.Where(s => s != null), s => Assert.NotNull(s.ItemTemplate));
    }

    /// <summary>The trap. NPCTemplate's copy constructor does VendorItems = other.VendorItems
    /// (NPCTemplate.cs:254) — the array AND its slot objects are shared with the base
    /// template, so an in-place edit rewrites dimension 0's shops.</summary>
    [Fact]
    public void Dimension_zero_vendor_stock_is_untouched()
    {
        using var fixture = Loaded();

        var basic = fixture.World.NPCHandler.GetNPCTemplate(MerchantId).VendorItems;

        Assert.Equal(SwordId, basic[1].ItemTemplate.ID);
        Assert.Equal(PotionId, basic[2].ItemTemplate.ID);

        for (int dim = 1; dim <= 6; dim++)
        {
            var stock = StockOf(fixture, dim);
            Assert.NotSame(basic, stock);                       // new array
            Assert.NotSame(basic[1], stock[1]);                 // and new slot objects
        }
    }

    [Fact]
    public void Slot_stack_and_can_see_stats_survive_the_repoint()
    {
        using var fixture = Loaded();

        var stock = StockOf(fixture, 4);

        Assert.Equal(1, stock[1].Slot);
        Assert.Equal(1, stock[1].Stack);
        Assert.True(stock[1].CanSeeStats);
        Assert.Equal(2, stock[2].Slot);
        Assert.Equal(5, stock[2].Stack);
        Assert.False(stock[2].CanSeeStats);
    }

    /// <summary>Resolution puts the item override above the vendor
    /// (CurrencyHandler.cs:41-52), so repointed gear prices in spirit with no vendor change,
    /// and an unrepointed consumable in the same window still prices in gold.</summary>
    [Fact]
    public void Repointed_stock_resolves_to_spirit_and_base_stock_stays_gold()
    {
        using var fixture = Loaded();
        var handler = fixture.World.CurrencyHandler;
        var vendor = fixture.World.MapHandler.GetMap(1 + Offset * 3).NPCs
            .Single(n => n.NPCTemplateID == MerchantId + Offset * 3);

        Assert.Equal("spirit", handler.Resolve(StockOf(fixture, 3)[1].ItemTemplate, vendor).Id);
        Assert.Equal(Currency.Gold,
            handler.Resolve(fixture.World.ItemHandler.GetTemplate(SwordId), vendor).Id);
    }

    /// <summary>Resolve agreeing is not the same as the till charging. This buys from the
    /// spawned dimension-3 merchant through the real VendorPurchaseInventoryEvent and
    /// asserts the spirit wallet moved, the gold wallet did not, and the dimension clone
    /// landed in the inventory.</summary>
    [Fact]
    public void An_actual_purchase_from_a_spawned_dimension_vendor_charges_spirit()
    {
        using var fixture = Loaded();
        var map = fixture.World.MapHandler.GetMap(1 + Offset * 3);
        var vendor = map.NPCs.Single(n => n.NPCTemplateID == MerchantId + Offset * 3);

        var player = fixture.CommandPlayerOn(map, 21, 20);
        // DimensionItem.CanPickup refuses an item above the player's unlocked dimension
        // (DimensionItem.csx:65-72), and a purchase is a pickup.
        player.Properties.SetProperty("dimension.max", 6);
        player.Gold = 5_000_000;
        fixture.World.CurrencyHandler.Get("spirit").Add(player, 100_000, fixture.World);
        player.Windows.Add(new Window { Type = Window.WindowTypes.Vendor, NPC = vendor });
        player.Sent.Clear();

        var spiritBefore = fixture.World.CurrencyHandler.Get("spirit").GetBalance(player);
        var goldBefore = player.Gold;

        // VendorFixture.Purchase's packet shape (VendorPurchaseCurrencyTests.cs:11-19).
        new VendorPurchaseInventoryEvent
        {
            Player = player,
            Data = "VPI" + vendor.LoginID + ",1",
        }.Ready(fixture.World);

        var clonePrice = fixture.World.ItemHandler.GetTemplate(SwordId + Offset * 3).Value;

        Assert.Equal(spiritBefore - clonePrice,
                     fixture.World.CurrencyHandler.Get("spirit").GetBalance(player));
        Assert.Equal(goldBefore, player.Gold);
        Assert.Equal(SwordId + Offset * 3, player.Inventory.GetSlot(1).Item.TemplateID);
        Assert.Contains(player.Sent, m => m.Contains("spirit"));
    }
}
```

> `Slots_with_no_clone_keep_the_base_template` as written above is weak — it asserts a
> property rather than the case. Strengthen it by giving the merchant a fourth slot holding
> a template that `CloneItemTemplates` deliberately skips, and asserting that slot still
> points at the base. Read `CloneItemTemplates` in `Dimensions.csx` first to find what it
> skips (if it clones every registered template unconditionally, construct the case by
> stocking a template id that the clone pass will map to an id already taken, or drop the
> test and say so in the commit message rather than shipping a tautology).

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter DimensionVendorStockTests`
Expected: FAIL — `RepointVendorStock` does not exist, so dimension vendor stock still
points at base templates and the purchase charges gold.

**Step 3: Implement**

In `OnLoaded`, immediately after `RepointDrops(world);`:

```csharp
        RepointVendorStock(world);
```

The pass, modelled on `RepointDrops` (`Dimensions.csx:664`):

```csharp
    /// <summary>Point each dimension vendor's stock at that dimension's item clones.
    ///
    /// New array AND new slot objects, never an in-place edit: NPCTemplate's copy
    /// constructor shares VendorItems with the base template (NPCTemplate.cs:254), so
    /// mutating either would rewrite dimension 0's shops. Same rule as RepointDrops.
    ///
    /// No vendor-side CurrencyId is set. The clones carry CurrencyId = "spirit" on the
    /// item, and Resolve puts the item override above the vendor (CurrencyHandler.cs:41),
    /// so repointed gear sells for spirit while unrepointed consumables stay gold.</summary>
    private void RepointVendorStock(GameWorld world)
    {
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in world.NPCHandler.GetTemplates()
                                       .Where(t => t.NPCTemplateID < Offset).ToList())
            {
                var clone = world.NPCHandler.GetNPCTemplate(basic.NPCTemplateID + Offset * dim);
                if (clone == null || basic.VendorItems == null) continue;

                var slots = new NPCVendorSlot[basic.VendorItems.Length];
                for (int i = 0; i < basic.VendorItems.Length; i++)
                {
                    var slot = basic.VendorItems[i];
                    if (slot == null) continue;

                    var dimTemplate = slot.ItemTemplate == null
                        ? null
                        : world.ItemHandler.GetTemplate(slot.ItemTemplate.ID + Offset * dim);

                    slots[i] = new NPCVendorSlot
                    {
                        Slot = slot.Slot,
                        ItemTemplate = dimTemplate ?? slot.ItemTemplate,
                        Stack = slot.Stack,
                        CanSeeStats = slot.CanSeeStats,
                    };
                }

                clone.VendorItems = slots;
            }
        }
    }
```

> `NPCHandler.LoadNPCs` sizes the array `VendorSlotSize + 1` and leaves index 0 and any unused slot null (`NPCHandler.cs:183-197`) — hence the null skip, and hence copying `Length` rather than assuming a size.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionVendorStockTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionVendorStockTests.cs
git commit -m "feat: repoint dimension vendor stock at dimension item clones"
```

---

## Task 8: Dimension 5/6 experience floors, the `/dimension` gate, and the full sweep

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:243-247` (`CloneMaps`)
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:1111` (`DimensionCommandEvent.Ready`)
- Test: `Goose.Tests/DimensionMapScriptTests.cs` or `DimensionsScriptTests.cs` (extend whichever already covers `CloneMaps`)
- Test: `Goose.Tests/DimensionCommandGateTests.cs` (create)

Two halves of one gate. The floors are a Part 1 fidelity gap: shipped `CloneMaps` applies
only the `(dim*5)²` scale and omits abyss's flat top-end floors (`Map.java:251-260`), and
since most maps carry `MinExperience = 0` and `0 × anything = 0`, that override is the
only thing gating dimensions 5 and 6 at all.

The second half is that **`/dimension` does not consult the floors at all.**
`DimensionCommandEvent.Ready` calls `this.Player.WarpTo(...)` directly
(`Dimensions.csx:1118`), and `Player.WarpTo` (`Player.cs:1234`) never calls
`Map.PlayerCanJoin`. Only `MoveEvent`'s warp tiles (`MoveEvent.cs:123`),
`SpellEffect`'s teleports (`SpellEffect.cs:831`) and `DimensionTeleport.csx:61` do. So
every map-level gate in this feature — `MinLevel`, `MinExperience`, `MaxExperience`,
required items, and `DimensionMap.csx`'s own script hook — is bypassed by the one command
players actually use. Fixing the floors without fixing this ships a gate with a door
standing open beside it.

**Step 1: Write the failing tests**

The floors, appended to whichever suite already covers `CloneMaps`:

```csharp
    [Theory]
    [InlineData(1, 25_000L)]        // 1000 * (1*5)^2
    [InlineData(2, 100_000L)]       // 1000 * (2*5)^2
    [InlineData(4, 400_000L)]       // 1000 * (4*5)^2
    public void Dimensions_one_to_four_scale_min_experience_by_the_square(int dim, long expected)
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100).MinExperience = 1000;
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        Assert.Equal(expected, fixture.World.MapHandler.GetMap(1 + 100000 * dim).MinExperience);
    }

    /// <summary>Map.java:251-260 — a flat floor, not a scale. It ignores the base value
    /// entirely, which is the point: most maps have MinExperience = 0, and 0 times any
    /// scale is 0.</summary>
    [Theory]
    [InlineData(5, 100_000_000_000L)]
    [InlineData(6, 500_000_000_000L)]
    public void Dimensions_five_and_six_take_a_flat_minimum(int dim, long expected)
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        basic.MinExperience = 0;                    // the realistic case
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        Assert.Equal(expected, fixture.World.MapHandler.GetMap(1 + 100000 * dim).MinExperience);
    }

    /// <summary>And it ignores a large base value too, rather than taking the max of the
    /// two — the abyss expression assigns, it does not clamp.</summary>
    [Fact]
    public void The_flat_minimum_replaces_a_larger_base_value()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100)
               .MinExperience = 999_000_000_000_000L;
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        Assert.Equal(100_000_000_000L, fixture.World.MapHandler.GetMap(1 + 100000 * 5).MinExperience);
    }

    /// <summary>The override in abyss touches minExp only.</summary>
    [Theory]
    [InlineData(2, 100_000L)]
    [InlineData(5, 625_000L)]
    [InlineData(6, 900_000L)]
    public void Max_experience_keeps_the_plain_scale_in_every_dimension(int dim, long expected)
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100).MaxExperience = 1000;
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        Assert.Equal(expected, fixture.World.MapHandler.GetMap(1 + 100000 * dim).MaxExperience);
    }
```

The gate, in a new `Goose.Tests/DimensionCommandGateTests.cs`, driven end to end through
Task 5's `RunCommand` so the registration, the parse, the unlock check and the warp are
all under test together:

```csharp
using Goose;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionCommandGateTests
{
    private const int Offset = 100000;
    private const int StartMapId = 1;

    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(StartMapId, "Town", width: 100, height: 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(StartMapId), 5, 5);
        player.Properties.SetProperty("dimension.max", 6);
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3);
        player.Level = 50;
        player.Sent.Clear();

        return (fixture, player);
    }

    /// <summary>The baseline: a player who clears the gate still gets there.</summary>
    [Fact]
    public void Warps_when_the_gate_is_clear()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Experience = 200_000_000_000L;

        Assert.True(fixture.RunCommand(player, "/dimension 5"));

        Assert.Equal(StartMapId + Offset * 5, player.MapID);
    }

    /// <summary>The bug. Player.WarpTo (Player.cs:1234) never calls Map.PlayerCanJoin, so
    /// before the fix the command warps a level-1 rebirthed character straight into
    /// dimension 5 past a 100,000,000,000 floor.</summary>
    [Fact]
    public void Refuses_below_the_minimum_experience_and_does_not_warp()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Experience = 0;
        player.ExperienceSold = 0;

        fixture.RunCommand(player, "/dimension 5");

        Assert.Equal(StartMapId, player.MapID);
        Assert.Contains(player.Sent, m => m.Contains("experience to enter this map"));
    }

    /// <summary>The other end. Map.PlayerCanJoin gates MaxExperience too (Map.cs:644), and
    /// /dimension must respect it for the same reason.</summary>
    [Fact]
    public void Refuses_above_the_maximum_experience_and_does_not_warp()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        fixture.World.MapHandler.GetMap(StartMapId + Offset * 2).MaxExperience = 1_000;
        player.Experience = 500_000;

        fixture.RunCommand(player, "/dimension 2");

        Assert.Equal(StartMapId, player.MapID);
        Assert.Contains(player.Sent, m => m.Contains("at most"));
    }

    /// <summary>The gate is the map's, so DimensionMap.csx's own refusal reaches the
    /// command too — one gate, not two implementations that can drift.</summary>
    [Fact]
    public void Refuses_a_dimension_above_the_players_unlock_and_does_not_warp()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Properties.SetProperty("dimension.max", 2);
        player.Experience = 900_000_000_000L;

        fixture.RunCommand(player, "/dimension 5");

        Assert.Equal(StartMapId, player.MapID);
    }

    /// <summary>Dimension 0 is home. It must stay reachable no matter how the floors are
    /// configured — otherwise a rebirthed character standing in dimension 6 has no way
    /// back.</summary>
    [Fact]
    public void Dimension_zero_stays_reachable_with_no_experience()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Experience = 800_000_000_000L;
        fixture.RunCommand(player, "/dimension 6");
        player.Experience = 0;
        player.ExperienceSold = 0;

        Assert.True(fixture.RunCommand(player, "/dimension 0"));

        Assert.Equal(StartMapId, player.MapID);
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter Dimension`
Expected: FAIL on the dimension 5/6 floor cases and on
`DimensionCommandGateTests.Refuses_*`. `Warps_when_the_gate_is_clear` and
`Dimension_zero_stays_reachable_with_no_experience` pass already — they are the regression
guards for the fix, not evidence of the bug.

**Step 3: Implement**

The gate first, in `DimensionCommandEvent.Ready` (`Dimensions.csx:1118`), replacing the
bare `WarpTo`:

```csharp
        // PlayerCanJoin, then WarpTo. Player.WarpTo (Player.cs:1234) does no gating of its
        // own - MoveEvent (:123), SpellEffect (:831) and DimensionTeleport.csx (:61) each
        // call PlayerCanJoin first, and this command has to as well or every map-level
        // gate in this feature (MinLevel, Min/MaxExperience, required items, and
        // DimensionMap.csx's own hook) is bypassed by the one route players actually use.
        //
        // PlayerCanJoin sends its own refusal, so there is nothing to say here.
        if (!target.PlayerCanJoin(this.Player, world)) return;

        this.Player.WarpTo(world, target, Dimensions.WardenX, Dimensions.WardenY);
```

Then the floors.

Config:

```csharp
    /// <summary>Map.java:251-260. A flat floor, not a scale - it discards the base map's
    /// value entirely. Most maps carry MinExperience = 0, so without this the top two
    /// dimensions have no experience gate at all and dimension.max is the sole barrier.</summary>
    public const long Dim5MinExperience = 100_000_000_000;
    public const long Dim6MinExperience = 500_000_000_000;
```

Replace `Dimensions.csx:245`:

```csharp
                clone.MinExperience = MinExperienceFor(basic.MinExperience, dim);
                clone.MaxExperience = basic.MaxExperience * (dim * 5) * (dim * 5);
```

and add:

```csharp
    /// <summary>Map.java:251-260. Dimensions 1-4 scale; 5 and 6 take a flat floor that
    /// ignores the base value.</summary>
    private long MinExperienceFor(long baseMin, int dim)
    {
        if (dim == 5) return Dim5MinExperience;
        if (dim >= 6) return Dim6MinExperience;

        return baseMin * (dim * 5) * (dim * 5);
    }
```

> Abyss's expressions are `100_000_000_000 * (dimension - 4)` and `500_000_000_000 * (dimension - 5)`, which at dimensions 5 and 6 evaluate to exactly the two constants above. The multipliers only matter if `DimensionCount` ever exceeds 6 — if you raise it, revisit this.

**Step 4: Run the whole suite**

Run: `dotnet test Goose.sln`
Expected: PASS — all pre-existing tests plus every test added in Tasks 1–8.

Then confirm the design's promises hold end to end by re-reading `docs/plans/2026-08-13-dimensions-economy-design.md` against the implementation: the rate constants, the exact refusal messages, `KeepRequirement = true`, the new-instance rule in both repointing passes, and the "refusals charge nothing" property in all four commands.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/
git commit -m "fix: dimension 5/6 experience floors, and gate /dimension on PlayerCanJoin"
```

---

## Manual verification (after Task 8)

The cloning and command registration run inside a `.csx` at `OnLoaded` against live sheet data, so the automated tests cannot cover the real dataset. Start the server against the Illutia database and confirm:

1. It reaches "Ready to join" with no exceptions, and the rebirth keeper is standing at map 1, (52,50) — visible, targetable, and two tiles east of the dimension-0 warden. If the load-time preflight throws instead, the coordinate is occupied by live sheet data; pick another and re-verify it against `Map1.map`.
2. `/resetitem` on a dimension-6 weapon charges 729 spirit and lands a suffix every time. Reroll the same weapon ten times and confirm its weapon damage does not creep upward.
3. `/resetitem` refuses a base-world weapon, a dimension tome, and a stack of two, and charges nothing in each case.
4. `/buygold 1` yields 1,000,000 gold; `/buyexperience 1` yields exactly 25,000,000 experience regardless of the server's experience modifier.
5. With `ExperienceCap` set just above a test character's total, `/buyexperience 1` refuses rather than vaulting the cap, and says what the character can afford.
6. `/givesp` moves spirit between two logged-in characters, refuses an offline name and a self-transfer, and writes one `GiveSpirit` row per side.
7. A dimension-5 vendor prices gear in spirit and potions in gold in the same window, and a purchase actually debits the spirit wallet.
8. Rebirth end to end: a character above 100M experience turns the quest in, drops to level 1 commoner, and gains the expected spirit.
9. `/dimension 5` refuses that freshly rebirthed character with the map's own experience message — both the Task 8 floor and the `PlayerCanJoin` call are doing their job. `/dimension 0` still works from wherever they are standing.
10. Query the `logs` table for the five new types and confirm each row carries the balances and, for transfers, the counterparty in `other_id`.

## Known consequences, not bugs

Listed so they are not "fixed" during review. Full rationale in the design doc's Known Limitations.

- Rebirth exists only in dimension 0.
- The sub-100M experience remainder is destroyed.
- Rebirth wipes the character off the `/rank` experience leaderboard (`Ranks.cs:72,84`).
- A rebirthed character is locked out of dimensions until they re-earn the experience.
- Dimension-0 vendors still buy dimension loot for spirit.
- `/resetitem` cannot touch equipped items or stacked tomes.
