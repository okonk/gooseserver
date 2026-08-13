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

---

## Task 1: Server primitives — `ChangeClass`, `AddExperience`, `Log.Types`

**Files:**
- Modify: `Goose/Player.cs:1358` (ChangeClass), `Goose/Player.cs:1652` (AddExperience)
- Modify: `Goose/Log.cs:33`
- Test: `Goose.Tests/PlayerEconomyOverloadTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
using Goose;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

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
```

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter PlayerEconomyOverloadTests`
Expected: PASS, 5 tests.

Then the full suite, to prove no existing caller shifted:

Run: `dotnet test Goose.sln`
Expected: PASS — 447 passed, 26 skipped, 0 failed, plus the 5 new.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/Log.cs Goose.Tests/PlayerEconomyOverloadTests.cs
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
using Xunit;

namespace Goose.Tests;

public class ItemRerollTests
{
    private static (GameWorld World, Item Item) ItemWithModifiers()
    {
        // Model the setup on ItemScriptHookTests.cs — reuse its world/template helpers
        // rather than inventing a second way to build a GameWorld in tests.
        throw new NotImplementedException("build from ItemScriptHookTests");
    }

    /// <summary>ResetModifiers must return the item to template state. Item.cs:14-18 has
    /// exactly two ItemProperty members, both of which a modifier sets.</summary>
    [Fact]
    public void ResetModifiers_clears_name_stats_multiplier_and_properties()
    {
        var (world, item) = ItemWithModifiers();
        item.Name = "Legendary Sword of Speed";
        item.BaseStats.Strength = 50;
        item.StatMultiplier = 1.25;
        item.ItemProperties[ItemProperty.TitleId] = 900100;
        item.ItemProperties[ItemProperty.SurnameId] = 900005;

        world.ItemHandler.ResetModifiers(item);

        Assert.Equal(item.Template.Name, item.Name);
        Assert.Equal(0, item.BaseStats.Strength);
        Assert.Equal(1, item.StatMultiplier);
        Assert.False(item.ItemProperties.ContainsKey(ItemProperty.TitleId));
        Assert.False(item.ItemProperties.ContainsKey(ItemProperty.SurnameId));
    }

    /// <summary>The guard that matters. Item.LoadFromTemplate does TotalStats +=
    /// (Item.cs:159), so a reset built on it would double-count the template's stats on
    /// every call. ResetModifiers must be safe to call repeatedly.</summary>
    [Fact]
    public void ResetModifiers_is_idempotent()
    {
        var (world, item) = ItemWithModifiers();
        item.Template.BaseStats.Strength = 10;

        world.ItemHandler.ResetModifiers(item);
        var afterOnce = item.TotalStats.Strength;
        world.ItemHandler.ResetModifiers(item);

        Assert.Equal(afterOnce, item.TotalStats.Strength);
        Assert.Equal(10, item.TotalStats.Strength);
    }

    [Fact]
    public void RerollModifiers_prefers_the_script_hook()
    {
        var (world, item) = ItemWithModifiers();
        item.Template.Script = ScriptStubFor(new RerollingStub());

        world.ItemHandler.RerollModifiers(item, world);

        Assert.Equal("rerolled", item.Name);
    }

    [Fact]
    public void RerollModifiers_falls_through_when_the_hook_declines()
    {
        // A stub returning false must leave the item at template state — the native
        // RollTitleAndSurname path runs, and with zero-chance settings adds nothing.
        var (world, item) = ItemWithModifiers();
        GameWorld.Settings.ItemTitleChancePercent = 0;
        GameWorld.Settings.ItemSurnameChancePercent = 0;
        item.Template.Script = ScriptStubFor(new DecliningStub());

        world.ItemHandler.RerollModifiers(item, world);

        Assert.Equal(item.Template.Name, item.Name);
    }

    [Fact]
    public void RerollModifiers_swallows_and_logs_a_throwing_hook()
    {
        var (world, item) = ItemWithModifiers();
        item.Template.Script = ScriptStubFor(new ThrowingStub());

        var ex = Record.Exception(() => world.ItemHandler.RerollModifiers(item, world));

        Assert.Null(ex);
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

    private class ThrowingStub : BaseItemScript
    {
        public override bool OnRerollModifiersEvent(Item item, GameWorld world)
            => throw new InvalidOperationException("boom");
    }
}
```

> **Before writing these**, read `Goose.Tests/ItemScriptHookTests.cs` and `Goose.Tests/Fixtures/ScriptStub.cs`. Task 2's stubs must be wired the way that file already wires item-script stubs — replace `ItemWithModifiers()` and `ScriptStubFor(...)` with whatever those files establish. Do not invent a parallel mechanism.

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
        /// stats. Safe to call repeatedly.
        ///
        /// Deliberately not built on Item.LoadFromTemplate, which accumulates rather than
        /// assigns (TotalStats += template.BaseStats, Item.cs:159) and would double-count
        /// the template's stats on a second call.</summary>
        public void ResetModifiers(Item item)
        {
            item.Name = item.Template.Name;
            item.BaseStats = new AttributeSet();
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
using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class DimensionRebirthTests
{
    private const int RebirthTemplateId = 810000;
    private const int RebirthQuestId = 910000;

    [Fact]
    public void Creates_the_rebirth_npc_template_and_quest()
    {
        using var fixture = new GlobalScriptFixture();
        // Mirror DimensionsScriptTests' world seeding — base map, class rows, then run OnLoaded.
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
        using var fixture = new GlobalScriptFixture();
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
        using var fixture = new GlobalScriptFixture();
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
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShipped();
        script.Object.OnLoaded(fixture.World);

        for (int dim = 1; dim <= 6; dim++)
            Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId + 100000 * dim));
    }

    [Fact]
    public void Disabled_creates_no_rebirth_npc_or_quest()
    {
        using var fixture = new GlobalScriptFixture();
        // Follow DimensionsScriptTests' disabled-mode test: CompileSource with Enabled flipped.
        var script = fixture.CompileSource(DisabledSource(), "DimensionsDisabled.csx");
        script.Object.OnLoaded(fixture.World);

        Assert.Null(fixture.World.NPCHandler.GetNPCTemplate(RebirthTemplateId));
        Assert.Null(fixture.World.QuestHandler.Get(RebirthQuestId));
    }
}
```

> Read `Goose.Tests/DimensionsScriptTests.cs` before writing these and copy its world-seeding and disabled-mode helpers verbatim. It already solves "run the real shipped script against a synthetic world"; do not rebuild that.

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

    /// <summary>Dimension 0 only. PLACEHOLDER - move this to wherever the rebirth NPC
    /// should actually stand.</summary>
    public const int RebirthMapId = StartMapId;
    public const int RebirthX = 52;
    public const int RebirthY = 50;
```

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

        if (ExpPerSpirit <= 0)
            throw new Exception("ExpPerSpirit must be positive - GiveReward divides by it.");

        if (world.QuestHandler.Get(RebirthQuestId) != null)
            throw new Exception($"Quest id {RebirthQuestId} already exists. RebirthQuestId collides with sheet data.");
        if (world.NPCHandler.GetNPCTemplate(RebirthTemplateId) != null)
            throw new Exception($"Rebirth template id {RebirthTemplateId} already exists.");

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

        if (world.NPCHandler.SpawnNPC(world, RebirthMapId, RebirthX, RebirthY,
                                      keeper, shouldRespawn: false) == null)
        {
            throw new Exception($"Could not spawn the rebirth keeper: map {RebirthMapId} does not exist.");
        }
    }
```

> The `\\n` doubling in the description is not a typo — `Quest.Description` is rendered by `QuestWindow`'s `\n`-splitting path, and the existing script writes literal `\\n` the same way. Check `CreateUnlockChain`'s strings and match whatever they do.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionRebirthTests`
Expected: 4 of 5 PASS. The `CompileShipped` tests fail until Task 4 creates `Rebirth.csx` — `GetScript` resolves a path that does not exist yet.

If that is the failure you see, proceed to Task 4 and re-run both together. **Do not** stub `Rebirth.csx` with a placeholder to go green early.

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
        using var fixture = new GlobalScriptFixture();
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
        using var fixture = new GlobalScriptFixture();
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
        var reward = new QuestReward
        {
            Type = RewardType.Script,
            ScriptParams = "100000000",
            Script = fixture.World.ScriptHandler.GetScript<IQuestScript>("Scripts/Quest/Rebirth.csx"),
        };
        fixture.InstallShippedScripts();

        var map = fixture.AddBaseMap(9200, "No Currency Map");
        var player = fixture.PlayerOn(map, 1, 1);

        var message = reward.Script.Object.CanComplete(reward, player, fixture.World);

        Assert.False(string.IsNullOrEmpty(message));
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

return new Rebirth();
```

> The trailing `return new Rebirth();` is how every other `.csx` in this repo hands back its instance — confirm against `Scripts/Quest/DimensionUnlock.csx` and match it exactly.

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
Expected: PASS — all 8 tests, including Task 3's `CompileShipped` ones.

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
- Test: `Goose.Tests/DimensionResetItemTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
public class DimensionResetItemTests
{
    /// <summary>A drop rolls a 45% suffix chance; a paid reroll always lands one. That
    /// asymmetry is the reason OnRerollModifiersEvent exists separately from
    /// OnRollModifiersEvent.</summary>
    [Fact]
    public void Reroll_always_lands_a_suffix()
    {
        // Run the hook many times over a dimension item and assert SurnameId is set every
        // single time. Model the item construction on DimensionItemScriptTests.
    }

    [Fact]
    public void Reroll_clears_the_previous_suffix_rather_than_stacking()
    {
        // Reroll twice; the name must never accumulate two suffixes.
    }

    [Fact] public void Refuses_a_non_dimension_item() { }
    [Fact] public void Refuses_a_tome_or_other_non_equipment() { }
    [Fact] public void Refuses_an_empty_slot_and_an_out_of_range_slot() { }

    /// <summary>The load-bearing one. Part 5 established that a Remove call is not itself
    /// a guard, so every refusal path must leave the balance untouched.</summary>
    [Fact] public void Refusals_charge_nothing() { }

    [Fact]
    public void Charges_three_to_the_dimension()
    {
        // dim 1 -> 3, dim 3 -> 27, dim 6 -> 729.
    }

    [Fact] public void Refuses_when_the_balance_is_short_and_charges_nothing() { }
}
```

> Fill these in against `Goose.Tests/DimensionItemScriptTests.cs`, which already builds dimension items and drives `DimensionItem.csx`. Reuse its helpers.

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
        int dim = item.TemplateID / Dimensions.Offset;   // NOTE: see caveat below
        if (dim < 1) return false;

        ApplySuffix(item, world, RollSuffixIndex(world));
        ApplyRarity(item, world);
        return true;
    }
```

> **Caveat:** `DimensionItem.csx` cannot reference `Dimensions.Offset` — separate compilation. Read how the shipped `DimensionItem.csx` gets its offset today (it has its own constant) and use that same one.

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
        int dim = item.TemplateID / Dimensions.Offset;
        if (dim < 1)
        {
            world.Send(this.Player, P.ServerMessage("Only items from a higher plane can be reset."));
            return;
        }

        // Dimension tomes are OneTime consumables that can stack, and a reroll would
        // rewrite every item in the stack.
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
        if (spirit.GetBalance(this.Player) < cost)
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
        world.LogHandler.Log(Log.Types.CreatedCustom, this.Player,
            "ResetItem: template " + item.TemplateID + " (" + cost + " " + spirit.ShortName + ")");
    }
}
```

> Two things to verify as you write this: whether `P.ServerMessage` and `Player.States.Ready` are reachable the same way `DimensionCommandEvent` reaches them, and whether the player's HP/MP status needs resending after the reroll (a suffix moves `MaxHP` — check what `Inventory.SendSlot` already sends and add `P.StatusInfo` only if it does not).

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionResetItemTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Item/DimensionItem.csx Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionResetItemTests.cs
git commit -m "feat: /resetitem with a guaranteed-suffix reroll"
```

---

## Task 6: `/buygold`, `/buyexperience`, `/givesp`

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionCurrencyCommandTests.cs` (create)

**Step 1: Write the failing tests**

One test per happy path and one per refusal, all asserting balances directly:

```csharp
[Fact] public void BuyGold_trades_spirit_for_gold_at_the_configured_rate() { }
[Fact] public void BuyGold_refuses_zero_and_negative_amounts_and_charges_nothing() { }
[Fact] public void BuyGold_refuses_an_insufficient_balance_and_charges_nothing() { }

[Fact] public void BuyExperience_grants_exactly_the_unmodified_amount() { }
/// <summary>The modifier must not touch purchased experience — that is the entire
/// reason for Task 1's applyModifiers overload.</summary>
[Fact] public void BuyExperience_is_unaffected_by_the_world_experience_modifier() { }
[Fact] public void BuyExperience_refuses_commoners_and_charges_nothing() { }
/// <summary>AddExperience early-returns over the cap (Player.cs:1653), which would take
/// the spirit and grant nothing.</summary>
[Fact] public void BuyExperience_refuses_over_the_experience_cap_and_charges_nothing() { }

[Fact] public void GiveSp_moves_the_balance_between_two_players() { }
[Fact] public void GiveSp_refuses_negative_amounts() { }
[Fact] public void GiveSp_refuses_an_offline_target() { }
[Fact] public void GiveSp_refuses_a_self_transfer() { }
[Fact] public void GiveSp_refuses_an_insufficient_balance_and_moves_nothing() { }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter DimensionCurrencyCommandTests`
Expected: FAIL.

**Step 3: Implement**

Config:

```csharp
    /// <summary>BuyGoldCommandEvent.java:47 - 1 spirit buys a million gold.</summary>
    public const long GoldPerSpirit = 1_000_000;

    /// <summary>BuyExperienceCommandEvent.java:52. Deliberately below ExpPerSpirit: the
    /// round trip is lossy by 4x, which is what keeps rebirth a net sink.</summary>
    public const long ExpPerSpiritPurchase = 25_000_000;
```

Registrations in `OnLoaded`:

```csharp
        world.EventHandler.RegisterEvent("/buygold ", BuyGoldCommandEvent.Create);
        world.EventHandler.RegisterEvent("/buyexperience ", BuyExperienceCommandEvent.Create);
        world.EventHandler.RegisterEvent("/givesp ", GiveSpiritCommandEvent.Create);
```

> Check the trailing-space convention against `Dimensions.csx:121` (`"/dimension "`) and `EventHandler.cs`'s trie. Commands taking arguments register with the trailing space; get this wrong and the command silently never fires.

All three follow the same skeleton. `/buygold`:

```csharp
        var spirit = world.CurrencyHandler.Get(Dimensions.SpiritCurrencyId);
        var gold = world.CurrencyHandler.Get(Currency.Gold);   // no CurrencyHandler.Gold property
        if (spirit == null || gold == null) return;

        if (spirit.GetBalance(this.Player) < amount)
        {
            world.Send(this.Player, P.ServerMessage("Not enough " + spirit.Name + "."));
            return;
        }

        spirit.Remove(this.Player, amount, world);
        gold.Add(this.Player, amount * Dimensions.GoldPerSpirit, world);
```

`/buyexperience` adds two refusals **before** charging:

```csharp
        if (this.Player.ClassID == 1)
        {
            world.Send(this.Player, P.ServerMessage("Choose a class before you buy experience."));
            return;
        }

        // AddExperience early-returns over the cap (Player.cs:1653-1660), which would take
        // the spirit and grant nothing.
        if (GameWorld.Settings.ExperienceCap > 0 &&
            this.Player.Experience + this.Player.ExperienceSold > GameWorld.Settings.ExperienceCap)
        {
            world.Send(this.Player, P.ServerMessage("You have reached the experience cap."));
            return;
        }

        // ... balance check ...
        spirit.Remove(this.Player, amount, world);
        this.Player.AddExperience(amount * Dimensions.ExpPerSpiritPurchase, world,
                                  Player.ExperienceMessage.Normal, applyModifiers: false);
```

`/givesp` takes `<player> <amount>`, refuses `amount <= 0`, refuses when
`world.PlayerHandler.GetPlayer(name)` is null, refuses a self-transfer, checks the
balance, then `Remove` from the sender and `Add` to the receiver, messaging both sides.

Every one of the three logs through `world.LogHandler.Log` with the Task 1 enum member.

**Step 4: Run to verify they pass**

Run: `dotnet test Goose.Tests --filter DimensionCurrencyCommandTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose.Tests/DimensionCurrencyCommandTests.cs
git commit -m "feat: /buygold, /buyexperience and /givesp spirit commands"
```

---

## Task 7: `RepointVendorStock`

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (new pass, `OnLoaded`)
- Test: `Goose.Tests/DimensionVendorStockTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
[Fact] public void Dimension_vendor_slots_point_at_same_dimension_clones() { }
[Fact] public void Slots_with_no_clone_keep_the_base_template() { }

/// <summary>The trap. NPCTemplate's copy constructor does VendorItems = other.VendorItems
/// (NPCTemplate.cs:254) — the array AND its slot objects are shared with the base
/// template, so an in-place edit rewrites dimension 0's shops.</summary>
[Fact]
public void Dimension_zero_vendor_stock_is_untouched()
{
    // Assert the base template's slots still point at base templates AFTER OnLoaded,
    // and that the arrays are not reference-equal.
}

[Fact] public void Slot_stack_and_can_see_stats_survive_the_repoint() { }

/// <summary>Resolution puts the item override above the vendor
/// (CurrencyHandler.cs:41-52), so repointed gear prices in spirit with no vendor change,
/// and an unrepointed consumable in the same window still prices in gold.</summary>
[Fact] public void Repointed_stock_resolves_to_spirit_and_consumables_stay_gold() { }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter DimensionVendorStockTests`
Expected: FAIL.

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

## Task 8: Dimension 5/6 experience floors, and the full sweep

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:243-247` (`CloneMaps`)
- Test: `Goose.Tests/DimensionMapScriptTests.cs` or `DimensionsScriptTests.cs` (extend whichever already covers `CloneMaps`)

This closes a Part 1 fidelity gap: shipped `CloneMaps` applies only the `(dim*5)²` scale and omits abyss's flat top-end floors (`Map.java:251-260`). Since most maps carry `MinExperience = 0` and `0 × anything = 0`, that override is the only thing gating dimensions 5 and 6 at all.

**Step 1: Write the failing tests**

```csharp
[Fact]
public void Dimensions_one_to_four_scale_min_experience_by_the_square()
{
    // base 1000, dim 2 -> 1000 * (2*5)^2 = 100_000
}

/// <summary>Map.java:251-260 — a flat floor, not a scale. It ignores the base value
/// entirely, which is the point: most maps have MinExperience = 0.</summary>
[Theory]
[InlineData(5, 100_000_000_000L)]
[InlineData(6, 500_000_000_000L)]
public void Dimensions_five_and_six_take_a_flat_minimum(int dim, long expected)
{
    // A base map with MinExperience = 0 must still come out at the floor.
}

[Fact]
public void Max_experience_keeps_the_plain_scale_in_every_dimension()
{
    // The override in abyss touches minExp only.
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter Dimension`
Expected: FAIL on the dimension 5/6 cases only.

**Step 3: Implement**

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
git commit -m "fix: port abyss's flat dimension 5/6 minimum experience floors"
```

---

## Manual verification (after Task 8)

The cloning and command registration run inside a `.csx` at `OnLoaded` against live sheet data, so the automated tests cannot cover the real dataset. Start the server against the Illutia database and confirm:

1. It reaches "Ready to join" with no exceptions, and the rebirth keeper is standing at `RebirthMapId, RebirthX, RebirthY`.
2. `/resetitem` on a dimension-6 weapon charges 729 spirit and lands a suffix every time.
3. `/buygold 1` yields 1,000,000 gold; `/buyexperience 1` yields exactly 25,000,000 experience regardless of the server's experience modifier.
4. A dimension-5 vendor prices gear in spirit and potions in gold in the same window.
5. Rebirth end to end: a character above 100M experience turns the quest in, drops to level 1 commoner, and gains the expected spirit.
6. `/dimension 5` refuses a freshly rebirthed character — the flat floor from Task 8 is doing its job.

## Known consequences, not bugs

Listed so they are not "fixed" during review. Full rationale in the design doc's Known Limitations.

- Rebirth exists only in dimension 0.
- The sub-100M experience remainder is destroyed.
- Rebirth wipes the character off the `/rank` experience leaderboard (`Ranks.cs:72,84`).
- A rebirthed character is locked out of dimensions until they re-earn the experience.
- Dimension-0 vendors still buy dimension loot for spirit.
- `/resetitem` cannot touch equipped items or stacked tomes.
