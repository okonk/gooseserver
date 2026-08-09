# Dimensions Part 3 — Spells Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Generate per-dimension copies of every spell and spell effect at `id + 100000·dim` with abyss scaling, give buffs a dimension ladder so higher-dimension buffs beat lower ones, and make teleport spells resolve within the caster's dimension.

**Architecture:** Two small generic additions to the server (`SpellHandler` registration/enumeration, and a script hook for spell-effect descriptions) plus two additive copy constructors. Everything dimension-aware lives in a new generation pass in `Scripts/Global/Dimensions.csx` and a new `Scripts/Spell/DimensionTeleport.csx`.

**Tech Stack:** C# / .NET 10, SQLite (System.Data.SQLite), xUnit, Roslyn C# scripting (`.csx`).

**Design doc:** `docs/plans/2026-08-09-dimensions-spells-design.md`

**Depends on:** Parts 1 and 2, merged at `f1de68c`.

**Out of scope:** Item template cloning, spell tomes, the spellbook upgrade rule (it lives in the items part as an `IItemScript` — the design records the code so it is not lost), the spirit economy. Do not write them here.

---

## APIs verified

Every citation below was read from source in this worktree before writing the plan.

| Fact | Location |
|---|---|
| `SpellHandler.effects` / `.spells` are private dicts | `Goose/SpellHandler.cs:16,17` |
| `SpellHandler.GetSpellEffect(int)` / `GetSpell(int)` | `Goose/SpellHandler.cs:205`, `:270` |
| `SpellHandler.EffectCount` / `Count` | `Goose/SpellHandler.cs:199`, `:264` |
| `GetSpellByName` scans values by name | `Goose/SpellHandler.cs:277` |
| `LoadSpellEffects` resolves cross-refs in a second pass | `Goose/SpellHandler.cs:149–192` |
| `LoadSpells` skips rows whose effect is missing | `Goose/SpellHandler.cs:250–254` |
| `Spell` full property list (12 properties) | `Goose/Spell.cs:21–41` |
| `SpellEffect` full property list | `Goose/SpellEffect.cs:113–225` |
| `SpellEffect.Script` is `Script<ISpellEffectScript>`, `ScriptParams` a string | `Goose/SpellEffect.cs:223,225` |
| `SpellEffect.GetItemDescription(GameWorld)` is a `yield` iterator | `Goose/SpellEffect.cs:398` |
| `case EffectTypes.Teleport` in that switch | `Goose/SpellEffect.cs:446–452` |
| `default:` branch falls to `GetBuffDescription("")` | `Goose/SpellEffect.cs:470–472` |
| `GetItemDescription` callers | `Goose/SpellInfoWindow.cs:50`, `Goose/Packets.cs:461,515` |
| `SpellEffect.CastTeleportSpell` body | `Goose/SpellEffect.cs:702–737` |
| `CastSpell` dispatch, `Teleport when target is Player` | `Goose/SpellEffect.cs:939` |
| `CastScriptSpell` has no target guard, logs and returns false | `Goose/SpellEffect.cs:975–985` |
| `SpellEffect.CanCastSpell` is public | `Goose/SpellEffect.cs:739` |
| `SpellEffect.log` static NLog logger | `Goose/SpellEffect.cs:16` |
| `TargetTypes` enum — identical to abyss | `Goose/SpellEffect.cs:32–41` |
| `EffectTypes` enum, `Script` is the last member | `Goose/SpellEffect.cs:72–95` |
| `ParseFormula` numeric literals via `Convert.ToDecimal(buffer)` | `Goose/SpellEffect.cs:1311,1347,1421,1458` |
| `ISpellEffectScript` members | `Goose/Scripting/ISpellEffectScript.cs:11–17` |
| `BaseSpellEffectScript` virtuals | `Goose/Scripting/BaseSpellEffectScript.cs:13–30` |
| `Player.AddBuff` stacking checks | `Goose/Player.cs:2074`, `:2083` |
| `NPC.AddBuff` stacking checks | `Goose/NPC.cs:1473`, `:1477` |
| `AttributeSet.Clone()` copies all 26 fields | `Goose/AttributeSet.cs:74–105` |
| `AttributeSet` property list | `Goose/AttributeSet.cs:14–39` |
| `Spellbook` persists a plain `int[]` of spell IDs | `Goose/Spellbook.cs:44`, `:77` |
| `ScriptHandler.GetScript` caches one instance per path | `Goose/Scripting/ScriptHandler.cs:19–30` |
| Scripts end with `return typeof(X);` | `Goose/Data/Illutia/Scripts/Map/DimensionMap.csx:113` |
| `Script<T>` compiles and instantiates the returned `Type` | `Goose/Scripting/Script.cs:26–48` |
| `Dimensions.csx` `OnLoaded` pass order | `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:77–89` |
| `Dimensions.csx` `Enabled` / `Offset` / `DimensionCount` | `Dimensions.csx:12,15,19` |
| `DimensionMap.csx` `DimensionOf(map)` reads `Map.ScriptParams` | `DimensionMap.csx:14–18` |
| `CloneMaps` sets `clone.ScriptParams = dim.ToString()` | `Dimensions.csx:188–228` |
| `GlobalScriptFixture` script list and dirs | `Goose.Tests/Fixtures/GlobalScriptFixture.cs:17–21,29–30` |
| `Goose.Tests.csproj` `<None Include>` block | `Goose.Tests/Goose.Tests.csproj:20–27` |
| `GameWorld.SpellHandler` / `.ScriptHandler` are public | `Goose/GameWorld.cs:41`, `:48` |
| `Map.GetPlayersInRange` / `PlayerCanJoin` public | `Goose/Map.cs:158`, `:604` |
| `Player.WarpTo` / `BoundMap` / `BoundX` public | `Goose/Player.cs:1234`, `:238`, `:230` |
| `P` is a public static class; `P.SpellPlayer` a `Func` | `Goose/Packets.cs:8`, `:22` |
| `GameWorld.Send(Player, string)` public | `Goose/GameWorld.cs:585` |
| Scaling source of truth | `~/code/abyssserver/src/Abyss/SpellHandler.java:226–330` |
| Stat scaling source of truth | `~/code/abyssserver/src/Abyss/AttributeSet.java:347–374` |
| Abyss teleport resolves against caster dimension | `~/code/abyssserver/src/Abyss/SpellEffect.java:833` |

**Baseline:** `dotnet test Goose.sln` → **295 passed** (Goose.Tests 171, Tools.Tests 124), 0 failed, 26 skipped. Verified by running it in this worktree, not from memory.

**Working directory for every command:** `/home/hayden/code/illutiagooseserver/.worktrees/dimensions-spells`

**Script location:** `Goose/Data/Illutia/Scripts/` — Illutia only. Do not touch `Goose/Data/Aspereta/`.

## Data facts, measured

From `~/Downloads/IllutiaGoose_2025-04-15.db`:

| Query | Result |
|---|---|
| `select count(*) from spells` | 207 |
| `select count(*) from spell_effects` | 623 |
| `select count(*) from spell_effects where effect_type=5` (Teleport) | 26 |
| `select max(spell_id) from spells` | 208 |
| `select max(spell_effect_id) from spell_effects` | 623 |

Both max IDs are far below the 100000 offset, so no collision preflight can fire on real data — the collision guards exist for bad configuration, matching Part 2.

## A culture note that matters

`ParseFormula` reads numeric literals with `Convert.ToDecimal(buffer)` and **no format provider**, so it parses in the current culture. Task 6 wraps formulas with a fractional multiplier, and that literal must use `.` as the separator.

This is safe and already required: shipped sheet data contains formulas like `0.10 * %ccmp` and `-6.6 * ((%cstr) + (%cwdmg) + (%clevel))`, which the same parser already reads. A culture with a `,` decimal separator would break the base game today. So format the multiplier with `CultureInfo.InvariantCulture` to match the existing data convention, and do not "fix" the parser here.

## Test budget

This plan adds **32** test cases, all in `Goose.Tests`. Tools.Tests is untouched at 124 passed / 26 skipped. Check the running total after each task:

| Task | Adds | Goose.Tests | Suite total passed |
|---|---|---|---|
| 0 — `SpellHandler` registration | 3 | 174 | 298 |
| 1 — copy constructors | 5 | 179 | 303 |
| 2 — script description hook | 3 | 182 | 306 |
| 3 — fixture, stub script, csproj | 0 | 182 | 306 |
| 4 — clone and scale effects | 7 | 189 | 313 |
| 5 — rewire refs and stacking ladder | 5 | 194 | 318 |
| 6 — clone and scale spells | 4 | 198 | 322 |
| 7 — teleport rewrite and script | 5 | 203 | 327 |
| 8 — end-to-end verification | 0 | 203 | 327 |

Task 4 is 4 `[Fact]` plus one 3-case `[Theory]`; the runner counts theory cases individually, hence 7.

Counts include every `[Theory]` case individually, which is how the runner reports them.

---

## Task 0: SpellHandler registration and enumeration

**Why first:** every later task needs a way to put a generated spell into the handler, and the test fixture needs a way to seed base spells. Mirrors `NPCHandler.AddTemplate` / `GetTemplates()` from Part 1 task 4.

**Files:**
- Modify: `Goose/SpellHandler.cs` (add next to `GetSpellEffect` `:205` and `GetSpell` `:270`)
- Test: `Goose.Tests/SpellHandlerRegistrationTests.cs` (create)

**Step 1: Write the failing test**

```csharp
namespace Goose.Tests;

public class SpellHandlerRegistrationTests
{
    [Fact]
    public void Registered_effects_are_retrievable_and_enumerable()
    {
        var handler = new SpellHandler();
        var effect = new SpellEffect { ID = 100042, Name = "Powerful Firestorm" };

        handler.AddSpellEffect(effect);

        Assert.Same(effect, handler.GetSpellEffect(100042));
        Assert.Contains(effect, handler.GetSpellEffects());
        Assert.Equal(1, handler.EffectCount);
    }

    [Fact]
    public void Registered_spells_are_retrievable_and_enumerable()
    {
        var handler = new SpellHandler();
        var spell = new Spell { ID = 100091, Name = "Powerful Bless" };

        handler.AddSpell(spell);

        Assert.Same(spell, handler.GetSpell(100091));
        Assert.Contains(spell, handler.GetSpells());
        Assert.Equal(1, handler.Count);
    }

    /// <summary>Overwriting is deliberate and matches NPCHandler.AddTemplate. The dimension
    /// script preflights for collisions itself rather than relying on the handler to refuse.</summary>
    [Fact]
    public void Registering_the_same_id_twice_overwrites()
    {
        var handler = new SpellHandler();
        handler.AddSpell(new Spell { ID = 5, Name = "First" });
        handler.AddSpell(new Spell { ID = 5, Name = "Second" });

        Assert.Equal("Second", handler.GetSpell(5).Name);
        Assert.Equal(1, handler.Count);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter SpellHandlerRegistrationTests`
Expected: FAIL — compile error, `SpellHandler` does not contain `AddSpellEffect` / `GetSpellEffects` / `AddSpell` / `GetSpells`.

**Step 3: Write minimal implementation**

Add to `Goose/SpellHandler.cs`, immediately after `GetSpellEffect` (`:210`):

```csharp
/// <summary>Registers a script-generated effect. Overwrites any existing entry with the
/// same id - callers that must not collide should check GetSpellEffect first.</summary>
public void AddSpellEffect(SpellEffect effect)
{
    this.effects[effect.ID] = effect;
}

/// <summary>Every loaded effect, for scripts that need to enumerate rather than look up.</summary>
public IEnumerable<SpellEffect> GetSpellEffects()
{
    return this.effects.Values;
}
```

And after `GetSpell` (`:275`):

```csharp
/// <summary>Registers a script-generated spell. Overwrites any existing entry with the same id.</summary>
public void AddSpell(Spell spell)
{
    this.spells[spell.ID] = spell;
}

public IEnumerable<Spell> GetSpells()
{
    return this.spells.Values;
}
```

Both return the live `Values` collection. Callers that mutate the dictionary while enumerating must snapshot with `.ToList()` first — Tasks 4–7 all do, and say so.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter SpellHandlerRegistrationTests`
Expected: PASS (3 tests).

**Step 5: Commit**

```bash
git add Goose/SpellHandler.cs Goose.Tests/SpellHandlerRegistrationTests.cs
git commit -m "feat: let scripts register and enumerate spells and spell effects"
```

---

## Task 1: Spell and SpellEffect copy constructors

**Files:**
- Modify: `Goose/Spell.cs` (add constructor after the property block `:41`)
- Modify: `Goose/SpellEffect.cs` (add constructor after the property block `:225`)
- Test: `Goose.Tests/SpellCloneTests.cs` (create)

**Step 1: Write the failing test**

```csharp
namespace Goose.Tests;

public class SpellCloneTests
{
    [Fact]
    public void Spell_copy_carries_every_scalar()
    {
        var effect = new SpellEffect { ID = 7 };
        var original = new Spell
        {
            ID = 91, Name = "Firestorm", Description = "Burns", Target = Spell.SpellTargets.Group,
            ClassRestrictions = 12, Aether = 3000, Graphic = 4, GraphicFile = 2,
            HPStaticCost = 10, HPPercentCost = 0.5m, MPStaticCost = 20, MPPercentCost = 0.25m,
            SPStaticCost = 0, SPPercentCost = 0m,
            SpellEffectID = 7, SpellEffect = effect,
        };

        var copy = new Spell(original) { ID = 400091 };

        Assert.Equal(400091, copy.ID);
        Assert.Equal("Firestorm", copy.Name);
        Assert.Equal("Burns", copy.Description);
        Assert.Equal(Spell.SpellTargets.Group, copy.Target);
        Assert.Equal(12, copy.ClassRestrictions);
        Assert.Equal(3000, copy.Aether);
        Assert.Equal(10, copy.HPStaticCost);
        Assert.Equal(0.5m, copy.HPPercentCost);
        Assert.Equal(20, copy.MPStaticCost);
        Assert.Equal(0.25m, copy.MPPercentCost);
        Assert.Equal(7, copy.SpellEffectID);
        Assert.Same(effect, copy.SpellEffect);
        Assert.Equal(91, original.ID);          // the original is untouched
    }

    [Fact]
    public void SpellEffect_copy_carries_every_scalar()
    {
        var original = new SpellEffect
        {
            ID = 42, Name = "Firestorm", Animation = 3, AnimationFile = 1,
            TargetType = SpellEffect.TargetTypes.Cross, TargetSize = 2,
            EffectType = SpellEffect.EffectTypes.Buff, Duration = 60000,
            MinimumLevelEffected = 1, MaximumLevelEffected = 99,
            HPFormula = "-5 * %clevel", MPFormula = "", TauntAggro = 500,
            TeleportMapID = 12, TeleportMapX = 30, TeleportMapY = 40,
            OnMeleeHitSpellID = 9, OnMeleeAttackSpellID = 8,
            BuffStacksOverString = "1 2", BuffDoesntStackOverString = "3",
            ScriptParams = "p",
        };

        var copy = new SpellEffect(original) { ID = 400042 };

        Assert.Equal(400042, copy.ID);
        Assert.Equal("Firestorm", copy.Name);
        Assert.Equal(3, copy.Animation);
        Assert.Equal(SpellEffect.TargetTypes.Cross, copy.TargetType);
        Assert.Equal(2, copy.TargetSize);
        Assert.Equal(SpellEffect.EffectTypes.Buff, copy.EffectType);
        Assert.Equal(60000, copy.Duration);
        Assert.Equal("-5 * %clevel", copy.HPFormula);
        Assert.Equal(500, copy.TauntAggro);
        Assert.Equal(12, copy.TeleportMapID);
        Assert.Equal(30, copy.TeleportMapX);
        Assert.Equal(9, copy.OnMeleeHitSpellID);
        Assert.Equal("1 2", copy.BuffStacksOverString);
        Assert.Equal("p", copy.ScriptParams);
    }

    /// <summary>Task 5 rewires each clone's stacking lists independently. Sharing the list
    /// instance would make every dimension's rewiring overwrite the last.</summary>
    [Fact]
    public void SpellEffect_copy_detaches_the_stacking_lists()
    {
        var other = new SpellEffect { ID = 1 };
        var original = new SpellEffect { ID = 42 };
        original.BuffStacksOver.Add(other);
        original.BuffDoesntStackOver.Add(other);

        var copy = new SpellEffect(original) { ID = 400042 };
        copy.BuffStacksOver.Clear();
        copy.BuffDoesntStackOver.Add(new SpellEffect { ID = 2 });

        Assert.Single(original.BuffStacksOver);
        Assert.Same(other, original.BuffStacksOver[0]);
        Assert.Single(original.BuffDoesntStackOver);
        Assert.Equal(2, copy.BuffDoesntStackOver.Count);
    }

    /// <summary>Task 4 scales the clone's stats in place. A shared AttributeSet would scale
    /// the base effect too, and every dimension would compound on the last.</summary>
    [Fact]
    public void SpellEffect_copy_gets_its_own_AttributeSet()
    {
        var original = new SpellEffect { ID = 42 };
        original.Stats.HP = 100;
        original.Stats.MoveSpeed = 5;

        var copy = new SpellEffect(original) { ID = 400042 };
        copy.Stats.HP = 999;

        Assert.NotSame(original.Stats, copy.Stats);
        Assert.Equal(100, original.Stats.HP);
        Assert.Equal(5, copy.Stats.MoveSpeed);
    }

    /// <summary>Compiled scripts are cached per path and stateless, so sharing the reference
    /// is correct - and it is what lets Task 7 assign one script to all teleport effects.</summary>
    [Fact]
    public void SpellEffect_copy_shares_the_script_reference()
    {
        var original = new SpellEffect { ID = 42, Script = null, ScriptParams = "x" };

        var copy = new SpellEffect(original);

        Assert.Null(copy.Script);
        Assert.Equal("x", copy.ScriptParams);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter SpellCloneTests`
Expected: FAIL — compile error, no such constructor `Spell(Spell)` / `SpellEffect(SpellEffect)`.

**Step 3: Write minimal implementation**

Neither `Spell` nor `SpellEffect` declares any constructor today (verified — `grep -n "public SpellEffect(" Goose/SpellEffect.cs` returns nothing). Adding a copy constructor therefore removes the implicit parameterless one, which existing object-initialiser code depends on (`SpellHandler.cs:43`, `:230`), so both types need an explicit parameterless constructor back.

`Stats` (`SpellEffect.cs:177`), `BuffStacksOver` (`:170`) and `BuffDoesntStackOver` (`:171`) have **no field initialisers** — they are null on a bare `new SpellEffect()` today, and `LoadSpellEffects` only works because it assigns all three explicitly (`SpellHandler.cs:86,134,135`). The tests above build effects directly and call `.BuffStacksOver.Add(...)`, so the parameterless constructor must initialise them:

```csharp
// Goose/SpellEffect.cs, after the property block at :225
public SpellEffect()
{
    this.Stats = new AttributeSet();
    this.BuffStacksOver = new List<SpellEffect>();
    this.BuffDoesntStackOver = new List<SpellEffect>();
    this.BuffStacksOverString = "";
    this.BuffDoesntStackOverString = "";
}

/// <summary>Copy constructor for script-generated dimension variants. Lists and the
/// AttributeSet are new instances so a variant can be rewired and rescaled without
/// mutating the effect it came from. Script is shared - compiled scripts are cached per
/// path (ScriptHandler.cs:19) and hold no per-effect state.
///
/// Every property on this type must be copied here. Adding a field above without adding
/// it here is the failure mode this constructor exists to guard against.</summary>
public SpellEffect(SpellEffect other) : this()
{
    this.ID = other.ID;
    this.Name = other.Name;
    this.Animation = other.Animation;
    this.AnimationFile = other.AnimationFile;
    this.Display = other.Display;
    this.TargetType = other.TargetType;
    this.TargetSize = other.TargetSize;
    this.Effected = other.Effected;
    this.MinimumLevelEffected = other.MinimumLevelEffected;
    this.MaximumLevelEffected = other.MaximumLevelEffected;
    this.EffectType = other.EffectType;
    this.Duration = other.Duration;
    this.DoAttackAnimation = other.DoAttackAnimation;
    this.DoCastAnimation = other.DoCastAnimation;
    this.SpellDamageEffects = other.SpellDamageEffects;
    this.EnergyType = other.EnergyType;
    this.HPFormula = other.HPFormula;
    this.MPFormula = other.MPFormula;
    this.SPFormula = other.SPFormula;
    this.OnEffectText = other.OnEffectText;
    this.OffEffectText = other.OffEffectText;
    this.TauntAggro = other.TauntAggro;
    this.TeleportMapID = other.TeleportMapID;
    this.TeleportMapX = other.TeleportMapX;
    this.TeleportMapY = other.TeleportMapY;
    this.WorksInPVP = other.WorksInPVP;
    this.WorksNotInPVP = other.WorksNotInPVP;
    this.OnlyHitsOneNPC = other.OnlyHitsOneNPC;
    this.BuffCanBeRemoved = other.BuffCanBeRemoved;
    this.BuffGraphic = other.BuffGraphic;
    this.BuffGraphicFile = other.BuffGraphicFile;
    this.RandomJoinChance = other.RandomJoinChance;
    this.OnMeleeAttackSpellID = other.OnMeleeAttackSpellID;
    this.OnMeleeHitSpellID = other.OnMeleeHitSpellID;
    this.OnMeleeAttackSpell = other.OnMeleeAttackSpell;
    this.OnMeleeHitSpell = other.OnMeleeHitSpell;
    this.OnMeleeAttackSpellChance = other.OnMeleeAttackSpellChance;
    this.OnMeleeHitSpellChance = other.OnMeleeHitSpellChance;
    this.SnarePercent = other.SnarePercent;
    this.BuffStacksOverString = other.BuffStacksOverString;
    this.BuffDoesntStackOverString = other.BuffDoesntStackOverString;
    this.HairID = other.HairID;  this.HairR = other.HairR;  this.HairG = other.HairG;
    this.HairB = other.HairB;    this.HairA = other.HairA;
    this.BodyR = other.BodyR;    this.BodyG = other.BodyG;
    this.BodyB = other.BodyB;    this.BodyA = other.BodyA;
    this.FaceID = other.FaceID;  this.BodyID = other.BodyID;
    this.Script = other.Script;
    this.ScriptParams = other.ScriptParams;

    // New instances, not shared references - see the class doc above.
    this.Stats = other.Stats == null ? new AttributeSet() : other.Stats.Clone();
    this.BuffStacksOver = other.BuffStacksOver == null
        ? new List<SpellEffect>() : new List<SpellEffect>(other.BuffStacksOver);
    this.BuffDoesntStackOver = other.BuffDoesntStackOver == null
        ? new List<SpellEffect>() : new List<SpellEffect>(other.BuffDoesntStackOver);
}
```

Verify against `Goose/SpellEffect.cs:113–225` that no property is missed. Leave a comment on the property block pointing at this constructor.

`AttributeSet.Clone()` (`AttributeSet.cs:74`) already copies all 26 fields, so it is the right tool — do not hand-roll the copy.

Then `Goose/Spell.cs`, after `:41`:

```csharp
public Spell() { }

/// <summary>Copy constructor for script-generated dimension variants. SpellEffect is a
/// shared reference; the caller repoints it at the same dimension's effect clone.</summary>
public Spell(Spell other)
{
    this.ID = other.ID;
    this.Name = other.Name;
    this.Description = other.Description;
    this.Target = other.Target;
    this.ClassRestrictions = other.ClassRestrictions;
    this.Aether = other.Aether;
    this.Graphic = other.Graphic;
    this.GraphicFile = other.GraphicFile;
    this.HPStaticCost = other.HPStaticCost;
    this.HPPercentCost = other.HPPercentCost;
    this.MPStaticCost = other.MPStaticCost;
    this.MPPercentCost = other.MPPercentCost;
    this.SPStaticCost = other.SPStaticCost;
    this.SPPercentCost = other.SPPercentCost;
    this.SpellEffectID = other.SpellEffectID;
    this.SpellEffect = other.SpellEffect;
}
```

**Watch out:** adding a parameterless constructor to `SpellEffect` that initialises `Stats` changes behaviour in `LoadSpellEffects` only in that the assignment at `SpellHandler.cs:86` now overwrites a non-null default. Harmless. Run the full suite in step 4 to confirm.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter SpellCloneTests`
Expected: PASS (5 tests).

Then the whole suite: `dotnet test Goose.sln` → 303 passed, 0 failed.

**Step 5: Commit**

```bash
git add Goose/Spell.cs Goose/SpellEffect.cs Goose.Tests/SpellCloneTests.cs
git commit -m "feat: add Spell and SpellEffect copy constructors for script variants"
```

---

## Task 2: ISpellEffectScript.GetItemDescription

**Why:** Task 7 rewrites teleport effects to `EffectTypes.Script`, which drops them out of the `case EffectTypes.Teleport` branch of `GetItemDescription` (`SpellEffect.cs:446`) into `default:` (`:470`). That loses the *"Teleport to Kanaphuk (12, 30)"* line in the spell info window (`SpellInfoWindow.cs:50`) and item tooltips (`Packets.cs:461,515`) — in dimension 0 too. This hook gives the script a way to supply the line instead.

**Files:**
- Modify: `Goose/Scripting/ISpellEffectScript.cs` (add member)
- Modify: `Goose/Scripting/BaseSpellEffectScript.cs` (virtual default returning null)
- Modify: `Goose/SpellEffect.cs:398` (consult the script first)
- Test: `Goose.Tests/SpellEffectScriptDescriptionTests.cs` (create)

**Step 1: Write the failing test**

These need an `ISpellEffectScript` instance without compiling a `.csx`. `SpellEffect.Script` is a `Script<ISpellEffectScript>` whose constructor loads from disk (`Script.cs:20`), so the test cannot hand-build one — the consult must therefore be written against something a test can substitute. Use a `Script<T>` built through the real `ScriptHandler` from a temp `.csx`, the same way `GlobalScriptFixture` does; a tiny inline script is enough.

```csharp
using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class SpellEffectScriptDescriptionTests
{
    private const string DescribingScript = @"
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class Describer : BaseSpellEffectScript
{
    public override IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        return new[] { ""Scripted line one"", ""Scripted line two"" };
    }
}

return typeof(Describer);
";

    private const string SilentScript = @"
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class Silent : BaseSpellEffectScript { }

return typeof(Silent);
";

    [Fact]
    public void A_script_supplying_lines_replaces_the_built_in_description()
    {
        using var fixture = new GlobalScriptFixture();
        var effect = new SpellEffect
        {
            ID = 1, EffectType = SpellEffect.EffectTypes.Script,
            Script = fixture.CompileSpellEffectScript(DescribingScript, "Describer.csx"),
        };

        Assert.Equal(new[] { "Scripted line one", "Scripted line two" },
                     effect.GetItemDescription(fixture.World).ToArray());
    }

    /// <summary>Returning null must fall through, so every existing spell-effect script is
    /// unaffected by this change.</summary>
    [Fact]
    public void A_script_returning_null_falls_through_to_the_built_in_switch()
    {
        using var fixture = new GlobalScriptFixture();
        var effect = new SpellEffect
        {
            ID = 1, EffectType = SpellEffect.EffectTypes.Stun,
            Script = fixture.CompileSpellEffectScript(SilentScript, "Silent.csx"),
        };

        Assert.Equal(new[] { "Stun" }, effect.GetItemDescription(fixture.World).ToArray());
    }

    [Fact]
    public void An_effect_with_no_script_uses_the_built_in_switch()
    {
        using var fixture = new GlobalScriptFixture();
        var effect = new SpellEffect { ID = 1, EffectType = SpellEffect.EffectTypes.Root };

        Assert.Equal(new[] { "Root" }, effect.GetItemDescription(fixture.World).ToArray());
    }
}
```

`CompileSpellEffectScript(body, fileName)` is a new fixture helper — add it here alongside the existing `CompileSource` (`GlobalScriptFixture.cs:85`), following the same shape:

```csharp
/// <summary>Compiles an arbitrary spell-effect script body from the temp data dir.</summary>
public Script<ISpellEffectScript> CompileSpellEffectScript(string body, string fileName)
{
    Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Spell"));
    var relativePath = "Scripts/Spell/" + fileName;
    File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
    return World.ScriptHandler.GetScript<ISpellEffectScript>(relativePath);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Goose.sln --filter SpellEffectScriptDescriptionTests`
Expected: FAIL — compile error, `BaseSpellEffectScript` has no `GetItemDescription` to override, and no such fixture helper.

**Step 3: Write minimal implementation**

```csharp
// Goose/Scripting/ISpellEffectScript.cs - add to the interface
/// <summary>Lines to show in place of the built-in description. Return null or an empty
/// sequence to fall through to SpellEffect's own switch.</summary>
IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world);
```

`ISpellEffectScript.cs` already has `using System.Collections.Generic;` (`:2`).

```csharp
// Goose/Scripting/BaseSpellEffectScript.cs - add the virtual
public virtual IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world)
{
    return null;
}
```

In `Goose/SpellEffect.cs`, rename the existing iterator and wrap it. `GetItemDescription` is a `yield` iterator, so the consult cannot simply `return` from the top — materialise the script's lines first:

```csharp
public IEnumerable<string> GetItemDescription(GameWorld world)
{
    var scripted = this.ScriptItemDescription(world);
    if (scripted != null && scripted.Count > 0) return scripted;

    return this.BuiltInItemDescription(world);
}

/// <summary>Lets a spell-effect script replace the built-in description. Used by
/// DimensionTeleport.csx, whose effects are Script-typed and so would otherwise fall to
/// the default branch below and lose their destination line.</summary>
private List<string> ScriptItemDescription(GameWorld world)
{
    if (this.Script == null) return null;

    try
    {
        var lines = this.Script.Object.GetItemDescription(this, world);
        return lines == null ? null : lines.ToList();
    }
    catch (Exception e)
    {
        log.Error(e, "Describe Spell {0} Exception", this.Name);
        return null;
    }
}

private IEnumerable<string> BuiltInItemDescription(GameWorld world)
{
    // ...the existing switch body, unchanged...
}
```

The rename is the whole change to the existing body — do not edit the switch. `log` is already declared at `SpellEffect.cs:16`, and the `try`/`catch` mirrors `CastScriptSpell` (`:975–985`) so a broken script degrades to the built-in text rather than taking down a tooltip.

**Step 4: Run test to verify it passes**

Run: `dotnet test Goose.sln --filter SpellEffectScriptDescriptionTests`
Expected: PASS (3 tests).

**Step 5: Commit**

```bash
git add Goose/Scripting/ISpellEffectScript.cs Goose/Scripting/BaseSpellEffectScript.cs \
        Goose/SpellEffect.cs Goose.Tests/Fixtures/GlobalScriptFixture.cs \
        Goose.Tests/SpellEffectScriptDescriptionTests.cs
git commit -m "feat: let spell effect scripts supply their own item description"
```

---

## Task 3: Fixture, stub teleport script, csproj wiring

**Why the stub comes before Task 7 fills it in:** Task 7's generation step resolves `Scripts/Spell/DimensionTeleport.csx` through `ScriptHandler.GetScript`, which compiles the file (`ScriptHandler.cs:22`, `Script.cs:27`). Tasks 4–6 run `OnLoaded` end-to-end in their tests, so the file must exist and compile from Task 4 onwards. This is the same ordering constraint Part 2 hit with `DimensionMap.csx`.

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Spell/DimensionTeleport.csx` (stub)
- Modify: `Goose.Tests/Goose.Tests.csproj:20–27` (copy it to output)
- Modify: `Goose.Tests/Fixtures/GlobalScriptFixture.cs` (Spell dir, script list, seed helpers)

**Step 1: Create the stub**

```csharp
using System;
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class DimensionTeleport : BaseSpellEffectScript
{
}

return typeof(DimensionTeleport);
```

**Step 2: Wire it into the test output**

Add to the existing `<ItemGroup>` at `Goose.Tests/Goose.Tests.csproj:20`:

```xml
<None Include="../Goose/Data/Illutia/Scripts/Spell/DimensionTeleport.csx"
      Link="DimensionScripts/DimensionTeleport.csx" CopyToOutputDirectory="PreserveNewest" />
```

**Step 3: Extend the fixture**

In `Goose.Tests/Fixtures/GlobalScriptFixture.cs`:

- Add to `ShippedScripts` (`:17–21`):
  ```csharp
  ("DimensionTeleport.csx", "Scripts/Spell/DimensionTeleport.csx"),
  ```
- Add `"Spell"` to the directory list at `:29`:
  ```csharp
  foreach (var dir in new[] { "Global", "Map", "Quest", "Spell" })
  ```
- Add seed helpers, next to `AddBaseMap` (`:92`):

  ```csharp
  /// <summary>Registers a base spell effect. Real ones come from the spell_effects table
  /// (SpellHandler.cs:29); the clone path only reads the object, so a synthetic one is enough.</summary>
  public SpellEffect AddBaseSpellEffect(int id, string name, Action<SpellEffect> configure = null)
  {
      var effect = new SpellEffect { ID = id, Name = name, MaximumLevelEffected = 99 };
      configure?.Invoke(effect);
      World.SpellHandler.AddSpellEffect(effect);
      return effect;
  }

  /// <summary>Registers a base spell pointing at an already-registered effect.</summary>
  public Spell AddBaseSpell(int id, string name, int effectId, Action<Spell> configure = null)
  {
      var spell = new Spell
      {
          ID = id, Name = name, Description = "",
          SpellEffectID = effectId,
          SpellEffect = World.SpellHandler.GetSpellEffect(effectId),
      };
      configure?.Invoke(spell);
      World.SpellHandler.AddSpell(spell);
      return spell;
  }
  ```

**Step 4: Verify nothing regressed**

Run: `dotnet test Goose.sln`
Expected: 306 passed, 0 failed. No new tests — this task is scaffolding, and the existing `DimensionsScriptTests` prove the fixture still drives `OnLoaded`.

If `DimensionsScriptTests` fails with `FileNotFoundException` naming `DimensionTeleport.csx`, the `<None Include>` path is wrong — that exception exists in the fixture (`:57–59`) precisely to make this legible.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Spell/DimensionTeleport.csx Goose.Tests/Goose.Tests.csproj \
        Goose.Tests/Fixtures/GlobalScriptFixture.cs
git commit -m "test: scaffold spell script fixture support and DimensionTeleport stub"
```

---

## Task 4: Clone and scale spell effects

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionSpellScriptTests.cs` (create)

Formulas are transcribed from `~/code/abyssserver/src/Abyss/SpellHandler.java:288–330` and `AttributeSet.java:347–374`.

**Step 1: Write the failing tests**

```csharp
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionSpellScriptTests
{
    private const int Offset = 100000;

    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Clones_each_effect_once_per_dimension_with_the_name_prefix()
    {
        using var fixture = Run(f => f.AddBaseSpellEffect(42, "Firestorm"));

        Assert.Equal("Supreme Firestorm", fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3).Name);
        Assert.Equal("Godly Firestorm", fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 6).Name);
        Assert.Equal("Firestorm", fixture.World.SpellHandler.GetSpellEffect(42).Name);   // base untouched
        Assert.Null(fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 7));
    }

    [Fact]
    public void Scales_duration_taunt_and_target_size()
    {
        using var fixture = Run(f => f.AddBaseSpellEffect(42, "Firestorm", e =>
        {
            e.Duration = 60000;
            e.TauntAggro = 500;
            e.TargetSize = 2;
            e.TargetType = SpellEffect.TargetTypes.Area;
        }));

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

        Assert.Equal((long)(60000 * Math.Pow(1.15, 3)), dim3.Duration);                    // SpellHandler.java:295
        Assert.Equal((long)(500 * Math.Pow(3, 3) + 100000 * Math.Pow(20, 3)), dim3.TauntAggro);  // :298
        Assert.Equal(2 + 3, dim3.TargetSize);                                              // :302
    }

    /// <summary>AttributeSet.java:347. Unlike abyss we clone the set first, so MoveSpeed and
    /// SP survive rather than being silently zeroed - recorded as a deliberate deviation.</summary>
    [Fact]
    public void Scales_buff_stats_and_preserves_unscaled_fields()
    {
        using var fixture = Run(f => f.AddBaseSpellEffect(42, "Bless", e =>
        {
            e.Stats.HP = 100;  e.Stats.MP = 50;
            e.Stats.HPStaticRegen = 3;
            e.Stats.AC = 10;   e.Stats.Strength = 4;
            e.Stats.SpellDamage = 0.2m;
            e.Stats.MoveSpeed = 5;  e.Stats.SP = 7;
        }));

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

        Assert.Equal(100 * 4 * 4, dim3.Stats.HP);                 // x (dim+1)^2
        Assert.Equal(50 * 4 * 4, dim3.Stats.MP);
        Assert.Equal((int)(3 * Math.Pow(4, 3)), dim3.Stats.HPStaticRegen);
        Assert.Equal((int)(10 * 2.5m), dim3.Stats.AC);            // x (1 + 0.5*dim)
        Assert.Equal(4 * 3, dim3.Stats.Strength);                 // x dim
        Assert.Equal(0.2m * 2.5m, dim3.Stats.SpellDamage);

        // Not in abyss's scaled list, and not zeroed here.
        Assert.Equal(5, dim3.Stats.MoveSpeed);
        Assert.Equal(7, dim3.Stats.SP);

        Assert.Equal(100, fixture.World.SpellHandler.GetSpellEffect(42).Stats.HP);   // base untouched
    }

    /// <summary>SpellHandler.java:290-294. Dimension buffs only land on level-50 targets,
    /// which is every dimension mob; damage effects stay castable on anything.</summary>
    [Theory]
    [InlineData((int)SpellEffect.EffectTypes.Buff, 50)]
    [InlineData((int)SpellEffect.EffectTypes.Permanent, 50)]
    [InlineData((int)SpellEffect.EffectTypes.Formula, 1)]
    public void Sets_minimum_level_effected_by_effect_type(int effectType, int expected)
    {
        using var fixture = Run(f => f.AddBaseSpellEffect(42, "Thing", e =>
        {
            e.EffectType = (SpellEffect.EffectTypes)effectType;
            e.MinimumLevelEffected = 20;
        }));

        Assert.Equal(expected, fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3).MinimumLevelEffected);
    }
}
```

That is 7 reported test cases: 4 `[Fact]` plus the 3-case `[Theory]`.

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionSpellScriptTests`
Expected: FAIL — `GetSpellEffect(300042)` returns null.

**Step 3: Write minimal implementation**

Add `CloneSpellEffects(world);` to `OnLoaded` in `Dimensions.csx`, after `CreateUnlockChain(world);` and before the `RegisterEvent` line:

```csharp
/// <summary>Dimension name prefixes. SpellHandler.java:235.</summary>
private static readonly string[] DimensionPrefixes =
{
    "", "Powerful ", "Super Powerful ", "Supreme ", "Omnipotent ", "Almighty ", "Godly ",
};

private string PrefixFor(int dim)
{
    return dim >= 0 && dim < DimensionPrefixes.Length ? DimensionPrefixes[dim] : "";
}

/// <summary>SpellHandler.java:226.</summary>
private string DescriptionPrefixFor(int dim)
{
    return dim > 0 ? "Abyss (" + dim + ") " : "";
}

/// <summary>Step 1 of the spell pass: one scaled copy of every effect per dimension.
/// Cross-references are left pointing at dimension-0 effects here and rewired by
/// RewireSpellEffects - clone order is dictionary order, so a referenced effect's clone
/// may not exist yet at the moment this runs.</summary>
private void CloneSpellEffects(GameWorld world)
{
    // Snapshot: AddSpellEffect mutates the dictionary GetSpellEffects() enumerates.
    var baseEffects = world.SpellHandler.GetSpellEffects().ToList();

    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var basic in baseEffects)
        {
            int id = basic.ID + Offset * dim;

            // AddSpellEffect overwrites silently. A base id large enough to land on
            // another dimension's slot would quietly replace a generated effect.
            if (world.SpellHandler.GetSpellEffect(id) != null)
                throw new Exception($"Dimension spell effect id {id} (base {basic.ID}, dim {dim}) "
                                    + "already exists. Offset is too small for this data set.");

            world.SpellHandler.AddSpellEffect(ScaleSpellEffect(basic, dim));
        }
    }
}

/// <summary>SpellHandler.java:288-330, applied in abyss's order - the formula wrap reads
/// TargetType before the shape morph rewrites it.</summary>
private SpellEffect ScaleSpellEffect(SpellEffect basic, int dim)
{
    var clone = new SpellEffect(basic)
    {
        ID = basic.ID + Offset * dim,
        Name = PrefixFor(dim) + basic.Name,
        Duration = (long)(basic.Duration * Math.Pow(1.15, dim)),
        TargetSize = basic.TargetSize + dim,
    };

    // SpellHandler.java:290-294
    clone.MinimumLevelEffected =
        (basic.EffectType == SpellEffect.EffectTypes.Buff ||
         basic.EffectType == SpellEffect.EffectTypes.Permanent) ? 50 : 1;

    // SpellHandler.java:298
    if (basic.TauntAggro > 0)
        clone.TauntAggro = (long)(basic.TauntAggro * Math.Pow(3, dim) + 100000 * Math.Pow(20, dim));

    ScaleBuffStats(clone.Stats, dim);

    // SpellHandler.java:307-308, then :310-328. Order matters: targetScale comes from the
    // ORIGINAL target type, before the morph below rewrites it.
    clone.HPFormula = ScaleFormula(basic.HPFormula, basic.TargetType, dim);
    clone.MPFormula = ScaleFormula(basic.MPFormula, basic.TargetType, dim);

    MorphTargetShape(clone, basic.TargetType, basic.TargetSize, dim);

    return clone;
}

/// <summary>AttributeSet.java:347. The set is already a clone (SpellEffect copy
/// constructor), so fields abyss omits keep their base value instead of being zeroed -
/// notably MoveSpeed and SP. Deliberate deviation, see the design doc.</summary>
private void ScaleBuffStats(AttributeSet stats, int dim)
{
    decimal linear = 1m + 0.5m * dim;

    stats.HP = stats.HP * (dim + 1) * (dim + 1);
    stats.MP = stats.MP * (dim + 1) * (dim + 1);

    stats.HPStaticRegen = (int)(stats.HPStaticRegen * Math.Pow(4, dim));
    stats.MPStaticRegen = (int)(stats.MPStaticRegen * Math.Pow(4, dim));

    stats.AC = (int)(stats.AC * linear);
    stats.DamageReduction *= linear;
    stats.Haste *= linear;
    stats.HPPercentRegen *= linear;
    stats.MPPercentRegen *= linear;
    stats.MeleeCrit *= linear;
    stats.MeleeDamage *= linear;
    stats.SpellCrit *= linear;
    stats.SpellDamage *= linear;

    stats.FireResist *= dim;
    stats.AirResist *= dim;
    stats.EarthResist *= dim;
    stats.WaterResist *= dim;
    stats.SpiritResist *= dim;
    stats.Strength *= dim;
    stats.Stamina *= dim;
    stats.Intelligence *= dim;
    stats.Dexterity *= dim;
}

/// <summary>SpellHandler.java:260. Single-target spells get an extra 1.15.
///
/// InvariantCulture is required: ParseFormula reads literals with Convert.ToDecimal and no
/// format provider (SpellEffect.cs:1311), and shipped sheet data already uses '.' as the
/// separator ("0.10 * %ccmp"), so '.' is the convention the parser is fed everywhere.</summary>
private string ScaleFormula(string formula, SpellEffect.TargetTypes targetType, int dim)
{
    if (string.IsNullOrEmpty(formula)) return formula;

    double targetScale = targetType == SpellEffect.TargetTypes.Target ? 1.15 : 1.0;
    double multiplier = targetScale * Math.Pow(1.25, dim);

    return "(" + formula + ") * "
           + multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>SpellHandler.java:310-328. Small shapes grow into bigger ones.</summary>
private void MorphTargetShape(SpellEffect clone, SpellEffect.TargetTypes targetType, int baseSize, int dim)
{
    if (targetType == SpellEffect.TargetTypes.Cross || targetType == SpellEffect.TargetTypes.Plus)
    {
        clone.TargetSize = dim;
        clone.TargetType = SpellEffect.TargetTypes.Area;
    }
    else if (targetType == SpellEffect.TargetTypes.LineFront)
    {
        clone.TargetSize = baseSize == 3 ? dim + 1 : dim;
        clone.TargetType = baseSize <= 1
            ? SpellEffect.TargetTypes.Plus
            : SpellEffect.TargetTypes.TriangleFront;
    }
}
```

`Math` is available — `Dimensions.csx` already uses `Math.Pow` in `ScaleHP` (`:280`).

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionSpellScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: clone and scale spell effects per dimension"
```

---

## Task 5: Rewire cross-references and build the stacking ladder

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionSpellScriptTests.cs`

**Why a second pass:** clone order is dictionary order, so when effect A is cloned, the clone of the effect it references may not exist yet — the same reason Part 2 split `RewireAllies` out of `CloneTemplates`.

**Why the stacking ladder:** `Player.AddBuff` (`Player.cs:2074,2083`) and `NPC.AddBuff` (`NPC.cs:1473,1477`) compare `SpellEffect` **references**. `BuffDoesntStackOver` is checked first and refuses the incoming buff; `BuffStacksOver` replaces the existing one in place. Populating those lists across dimensions makes higher-dimension buffs beat lower ones with no server change.

**Step 1: Write the failing tests**

```csharp
[Fact]
public void Cross_references_point_at_the_same_dimensions_effects()
{
    using var fixture = Run(f =>
    {
        f.AddBaseSpellEffect(9, "Retaliate");
        f.AddBaseSpellEffect(42, "Thorns", e =>
        {
            e.OnMeleeHitSpellID = 9;
            e.OnMeleeHitSpell = f.World.SpellHandler.GetSpellEffect(9);
        });
    });

    var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

    Assert.Equal(9 + Offset * 3, dim3.OnMeleeHitSpellID);
    Assert.Same(fixture.World.SpellHandler.GetSpellEffect(9 + Offset * 3), dim3.OnMeleeHitSpell);

    // The base effect keeps its own reference.
    Assert.Same(fixture.World.SpellHandler.GetSpellEffect(9),
                fixture.World.SpellHandler.GetSpellEffect(42).OnMeleeHitSpell);
}

[Fact]
public void A_cross_reference_with_no_clone_is_dropped_rather_than_left_at_dimension_zero()
{
    using var fixture = Run(f =>
        f.AddBaseSpellEffect(42, "Thorns", e =>
        {
            // An id that resolved at load time but is not in the handler now.
            e.OnMeleeHitSpellID = 999;
            e.OnMeleeHitSpell = new SpellEffect { ID = 999 };
        }));

    var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

    Assert.Null(dim3.OnMeleeHitSpell);
    Assert.Equal(0, dim3.OnMeleeHitSpellID);
}

/// <summary>The ladder: a dimension-3 buff replaces every copy at dimension 3 or below.</summary>
[Fact]
public void A_buff_stacks_over_its_own_lower_dimension_copies()
{
    using var fixture = Run(f => f.AddBaseSpellEffect(42, "Bless",
        e => e.EffectType = SpellEffect.EffectTypes.Buff));

    var handler = fixture.World.SpellHandler;
    var dim3 = handler.GetSpellEffect(42 + Offset * 3);

    Assert.Contains(handler.GetSpellEffect(42), dim3.BuffStacksOver);
    Assert.Contains(handler.GetSpellEffect(42 + Offset), dim3.BuffStacksOver);
    Assert.Contains(handler.GetSpellEffect(42 + Offset * 2), dim3.BuffStacksOver);
    Assert.DoesNotContain(handler.GetSpellEffect(42 + Offset * 4), dim3.BuffStacksOver);
}

/// <summary>And is refused outright by any higher-dimension copy already applied.</summary>
[Fact]
public void A_buff_does_not_stack_over_its_own_higher_dimension_copies()
{
    using var fixture = Run(f => f.AddBaseSpellEffect(42, "Bless",
        e => e.EffectType = SpellEffect.EffectTypes.Buff));

    var handler = fixture.World.SpellHandler;

    var dim3 = handler.GetSpellEffect(42 + Offset * 3);
    Assert.Contains(handler.GetSpellEffect(42 + Offset * 4), dim3.BuffDoesntStackOver);
    Assert.Contains(handler.GetSpellEffect(42 + Offset * 6), dim3.BuffDoesntStackOver);
    Assert.DoesNotContain(handler.GetSpellEffect(42 + Offset * 2), dim3.BuffDoesntStackOver);

    // The dimension-0 effect gets the same treatment, or the base spell would overwrite
    // its own upgrades.
    var basic = handler.GetSpellEffect(42);
    Assert.Contains(handler.GetSpellEffect(42 + Offset), basic.BuffDoesntStackOver);
}

/// <summary>The ladder extends to every entry in the base list, not just the effect itself.
/// Without this a dim-3 Bless meeting a dim-0 Minor Bless matches neither list and applies
/// as a second buff, stacking both stat blocks.</summary>
[Fact]
public void The_ladder_extends_to_entries_from_the_base_stacking_list()
{
    using var fixture = Run(f =>
    {
        var minor = f.AddBaseSpellEffect(41, "Minor Bless",
            e => e.EffectType = SpellEffect.EffectTypes.Buff);
        f.AddBaseSpellEffect(42, "Bless", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.BuffStacksOver.Add(minor);
        });
    });

    var handler = fixture.World.SpellHandler;
    var dim3 = handler.GetSpellEffect(42 + Offset * 3);

    Assert.Contains(handler.GetSpellEffect(41), dim3.BuffStacksOver);              // dim 0 Minor Bless
    Assert.Contains(handler.GetSpellEffect(41 + Offset * 3), dim3.BuffStacksOver); // dim 3 Minor Bless
    Assert.DoesNotContain(handler.GetSpellEffect(41 + Offset * 5), dim3.BuffStacksOver);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionSpellScriptTests`
Expected: FAIL — the dimension-3 effect's `OnMeleeHitSpell` is the dimension-0 effect, and the stacking lists are unpopulated.

**Step 3: Write minimal implementation**

Add `RewireSpellEffects(world);` to `OnLoaded` immediately after `CloneSpellEffects(world);`:

```csharp
/// <summary>Step 2 of the spell pass. Two jobs, both of which need every clone to exist:
/// repoint each clone's melee-reaction references at its own dimension, and build the
/// dimension ladder on the buff stacking lists.</summary>
private void RewireSpellEffects(GameWorld world)
{
    var baseEffects = world.SpellHandler.GetSpellEffects()
                           .Where(e => e.ID < Offset).ToList();

    // Snapshot the base lists before touching anything: the dim-0 pass below rewrites the
    // base effect's own BuffDoesntStackOver, and later dimensions must still read the
    // original list.
    var baseStacksOver = baseEffects.ToDictionary(e => e.ID, e => e.BuffStacksOver.ToList());
    var baseDoesntStackOver = baseEffects.ToDictionary(e => e.ID, e => e.BuffDoesntStackOver.ToList());

    // Melee reactions: dimension copies only. The base effect keeps what it loaded.
    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var basic in baseEffects)
        {
            var clone = world.SpellHandler.GetSpellEffect(basic.ID + Offset * dim);
            if (clone == null) continue;

            // A reference with no clone is dropped, not left pointing across dimensions -
            // same rule RewireAllies applies to NPC allies.
            clone.OnMeleeAttackSpell = world.SpellHandler.GetSpellEffect(
                basic.OnMeleeAttackSpellID + Offset * dim);
            clone.OnMeleeAttackSpellID = clone.OnMeleeAttackSpell == null
                ? 0 : clone.OnMeleeAttackSpell.ID;

            clone.OnMeleeHitSpell = world.SpellHandler.GetSpellEffect(
                basic.OnMeleeHitSpellID + Offset * dim);
            clone.OnMeleeHitSpellID = clone.OnMeleeHitSpell == null
                ? 0 : clone.OnMeleeHitSpell.ID;
        }
    }

    // The stacking ladder covers dimension 0 as well.
    for (int dim = 0; dim <= DimensionCount; dim++)
    {
        foreach (var basic in baseEffects)
        {
            var effect = world.SpellHandler.GetSpellEffect(basic.ID + Offset * dim);
            if (effect == null) continue;

            var stacks = new List<SpellEffect>();
            foreach (var entry in baseStacksOver[basic.ID].Concat(new[] { basic }))
                for (int k = 0; k <= dim; k++)
                    AddEffectIfPresent(world, stacks, entry.ID + Offset * k);

            var doesnt = new List<SpellEffect>();
            foreach (var entry in baseDoesntStackOver[basic.ID])
                for (int k = 0; k <= DimensionCount; k++)
                    AddEffectIfPresent(world, doesnt, entry.ID + Offset * k);
            for (int k = dim + 1; k <= DimensionCount; k++)
                AddEffectIfPresent(world, doesnt, basic.ID + Offset * k);

            effect.BuffStacksOver = stacks;
            effect.BuffDoesntStackOver = doesnt;

            // Keep the string forms consistent. Nothing re-parses them after load, but a
            // divergent string is a trap for anyone debugging from a dump - same reasoning
            // as AlliesString in RewireAllies.
            effect.BuffStacksOverString = string.Join(" ", stacks.Select(e => e.ID));
            effect.BuffDoesntStackOverString = string.Join(" ", doesnt.Select(e => e.ID));
        }
    }
}

private void AddEffectIfPresent(GameWorld world, List<SpellEffect> into, int id)
{
    var effect = world.SpellHandler.GetSpellEffect(id);
    if (effect != null && !into.Contains(effect)) into.Add(effect);
}
```

Note the `dim == 0` case adds the effect to its own `BuffStacksOver` (via the `Concat(new[] { basic })` at `k == 0`). Harmless — `AddBuff` checks `buff.SpellEffect == b.SpellEffect` first (`Player.cs:2082`), so the self-entry is never reached.

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionSpellScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: rewire dimension spell effect references and add the buff stacking ladder"
```

---

## Task 6: Clone and scale spells

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx`
- Test: `Goose.Tests/DimensionSpellScriptTests.cs`

**Step 1: Write the failing tests**

```csharp
[Fact]
public void Clones_each_spell_once_per_dimension_pointing_at_the_same_dimensions_effect()
{
    using var fixture = Run(f =>
    {
        f.AddBaseSpellEffect(42, "Firestorm");
        f.AddBaseSpell(91, "Firestorm", 42, s => s.Description = "Burns");
    });

    var handler = fixture.World.SpellHandler;
    var dim3 = handler.GetSpell(91 + Offset * 3);

    Assert.NotNull(dim3);
    Assert.Equal("Supreme Firestorm", dim3.Name);
    Assert.Equal("Abyss (3) Burns", dim3.Description);
    Assert.Equal(42 + Offset * 3, dim3.SpellEffectID);
    Assert.Same(handler.GetSpellEffect(42 + Offset * 3), dim3.SpellEffect);
}

[Fact]
public void Scales_aether_and_static_costs()
{
    using var fixture = Run(f =>
    {
        f.AddBaseSpellEffect(42, "Firestorm");
        f.AddBaseSpell(91, "Firestorm", 42, s =>
        {
            s.Aether = 10000; s.HPStaticCost = 50; s.MPStaticCost = 100;
            s.SPStaticCost = 7; s.MPPercentCost = 0.25m;
        });
    });

    var dim3 = fixture.World.SpellHandler.GetSpell(91 + Offset * 3);

    Assert.Equal((long)(10000 * Math.Pow(0.9, 3)), dim3.Aether);          // SpellHandler.java:279
    Assert.Equal((int)(50 * Math.Pow(3, 3)), dim3.HPStaticCost);          // :280
    Assert.Equal((int)(100 * Math.Pow(3, 3)), dim3.MPStaticCost);         // :281
    Assert.Equal(7, dim3.SPStaticCost);                                   // abyss leaves SP alone
    Assert.Equal(0.25m, dim3.MPPercentCost);                              // percent costs unscaled
}

[Fact]
public void Leaves_the_base_spell_untouched()
{
    using var fixture = Run(f =>
    {
        f.AddBaseSpellEffect(42, "Firestorm");
        f.AddBaseSpell(91, "Firestorm", 42, s => { s.Aether = 10000; s.Description = "Burns"; });
    });

    var basic = fixture.World.SpellHandler.GetSpell(91);

    Assert.Equal("Firestorm", basic.Name);
    Assert.Equal("Burns", basic.Description);
    Assert.Equal(10000, basic.Aether);
    Assert.Equal(42, basic.SpellEffectID);
}

/// <summary>The single-target extra 1.15 and the InvariantCulture separator, which
/// ParseFormula depends on (SpellEffect.cs:1311).</summary>
[Fact]
public void Wraps_damage_formulas_with_the_dimension_multiplier()
{
    using var fixture = Run(f =>
    {
        f.AddBaseSpellEffect(42, "Bolt", e =>
        {
            e.TargetType = SpellEffect.TargetTypes.Target;
            e.HPFormula = "-5 * %clevel";
        });
        f.AddBaseSpellEffect(43, "Nova", e =>
        {
            e.TargetType = SpellEffect.TargetTypes.Area;
            e.HPFormula = "-5 * %clevel";
        });
    });

    var handler = fixture.World.SpellHandler;
    var single = (1.15 * Math.Pow(1.25, 2)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    var area = Math.Pow(1.25, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);

    Assert.Equal("(-5 * %clevel) * " + single, handler.GetSpellEffect(42 + Offset * 2).HPFormula);
    Assert.Equal("(-5 * %clevel) * " + area, handler.GetSpellEffect(43 + Offset * 2).HPFormula);
    Assert.Contains(".", handler.GetSpellEffect(42 + Offset * 2).HPFormula);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionSpellScriptTests`
Expected: FAIL — `GetSpell(300091)` returns null.

**Step 3: Write minimal implementation**

Add `CloneSpells(world);` to `OnLoaded` immediately after `RewireSpellEffects(world);`:

```csharp
/// <summary>Step 3 of the spell pass. Runs after RewireSpellEffects so every effect clone
/// exists and is fully wired before a spell points at one.</summary>
private void CloneSpells(GameWorld world)
{
    var baseSpells = world.SpellHandler.GetSpells().ToList();

    for (int dim = 1; dim <= DimensionCount; dim++)
    {
        foreach (var basic in baseSpells)
        {
            int id = basic.ID + Offset * dim;

            if (world.SpellHandler.GetSpell(id) != null)
                throw new Exception($"Dimension spell id {id} (base {basic.ID}, dim {dim}) "
                                    + "already exists. Offset is too small for this data set.");

            var effect = world.SpellHandler.GetSpellEffect(basic.SpellEffectID + Offset * dim);

            // LoadSpells drops a spell whose effect is missing (SpellHandler.cs:250); do
            // the same rather than registering a spell that cannot be cast.
            if (effect == null) continue;

            world.SpellHandler.AddSpell(new Spell(basic)
            {
                ID = id,
                Name = PrefixFor(dim) + basic.Name,
                Description = DescriptionPrefixFor(dim) + basic.Description,
                Aether = (long)(basic.Aether * Math.Pow(0.9, dim)),          // SpellHandler.java:279
                HPStaticCost = (int)(basic.HPStaticCost * Math.Pow(3, dim)), // :280
                MPStaticCost = (int)(basic.MPStaticCost * Math.Pow(3, dim)), // :281
                SpellEffectID = effect.ID,
                SpellEffect = effect,
            });
        }
    }
}
```

`SPStaticCost` and all three percent costs come across unchanged via the copy constructor — abyss does not scale them.

**Step 4: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionSpellScriptTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add -A
git commit -m "feat: clone and scale spells per dimension"
```

---

## Task 7: In-dimension teleport

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Spell/DimensionTeleport.csx` (fill in the Task 3 stub)
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (the rewrite pass)
- Test: `Goose.Tests/DimensionTeleportScriptTests.cs` (create)

**Why the rewrite covers dimension 0:** class level-up spells stay dimension 0 by design, so the dimension-0 teleport is the one every player actually holds. Leaving it alone would let anyone teleport straight out of a dimension.

**Why the rewrite runs last:** clones must still be `Teleport`-typed when Task 4 copies them, so one pass over `GetSpellEffects()` afterwards converts base and clones together.

**Step 1: Write the failing tests**

```csharp
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionTeleportScriptTests
{
    private const int Offset = 100000;

    [Fact]
    public void Every_teleport_effect_is_rewritten_to_a_script_effect()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseSpellEffect(42, "Gate", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Teleport;
            e.TeleportMapID = 1; e.TeleportMapX = 5; e.TeleportMapY = 6;
        });

        using (fixture)
        {
            fixture.CompileShipped().Object.OnLoaded(fixture.World);

            foreach (var id in new[] { 42, 42 + Offset, 42 + Offset * 6 })
            {
                var effect = fixture.World.SpellHandler.GetSpellEffect(id);
                Assert.Equal(SpellEffect.EffectTypes.Script, effect.EffectType);
                Assert.NotNull(effect.Script);
                Assert.Equal(Offset.ToString(), effect.ScriptParams);

                // The destination data survives - the script reads it.
                Assert.Equal(1, effect.TeleportMapID);
                Assert.Equal(5, effect.TeleportMapX);
            }
        }
    }

    /// <summary>Non-teleport effects must not be touched.</summary>
    [Fact]
    public void Other_effect_types_keep_their_type()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseSpellEffect(43, "Bless", e => e.EffectType = SpellEffect.EffectTypes.Buff);

        using (fixture)
        {
            fixture.CompileShipped().Object.OnLoaded(fixture.World);

            Assert.Equal(SpellEffect.EffectTypes.Buff,
                         fixture.World.SpellHandler.GetSpellEffect(43 + Offset * 3).EffectType);
            Assert.Null(fixture.World.SpellHandler.GetSpellEffect(43 + Offset * 3).Script);
        }
    }

    [Fact]
    public void Casting_from_a_dimension_map_lands_in_that_dimension()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileSpellEffectScriptFromShipped();

        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim3Town = fixture.AddBaseMap(1 + Offset * 3, "Town (3)", width: 100, height: 100);
        dim3Town.ScriptParams = "3";
        var dim3Cave = fixture.AddBaseMap(2 + Offset * 3, "Cave (3)", width: 100, height: 100);
        dim3Cave.ScriptParams = "3";
        fixture.AddBaseMap(2, "Cave", width: 100, height: 100);

        var effect = new SpellEffect
        {
            ID = 42, EffectType = SpellEffect.EffectTypes.Script, ScriptParams = Offset.ToString(),
            TeleportMapID = 2, TeleportMapX = 7, TeleportMapY = 8,
            Effected = SpellEffect.SpellEffected.Self, MaximumLevelEffected = 99,
        };

        var player = fixture.PlayerOn(dim3Town, x: 50, y: 50);

        Assert.True(script.Object.Cast(effect, player, player, fixture.World));
        Assert.Equal(2 + Offset * 3, player.Map.ID);
        Assert.Equal(7, player.MapX);
    }

    [Fact]
    public void Casting_from_dimension_zero_lands_in_dimension_zero()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileSpellEffectScriptFromShipped();

        var town = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseMap(2, "Cave", width: 100, height: 100);
        fixture.AddBaseMap(2 + Offset * 3, "Cave (3)", width: 100, height: 100);

        var effect = new SpellEffect
        {
            ID = 42, EffectType = SpellEffect.EffectTypes.Script, ScriptParams = Offset.ToString(),
            TeleportMapID = 2, TeleportMapX = 7, TeleportMapY = 8,
            Effected = SpellEffect.SpellEffected.Self, MaximumLevelEffected = 99,
        };

        var player = fixture.PlayerOn(town, x: 50, y: 50);

        Assert.True(script.Object.Cast(effect, player, player, fixture.World));
        Assert.Equal(2, player.Map.ID);
    }

    /// <summary>A destination with no clone in the caster's dimension falls back to the base
    /// map - an exit from the dimension rather than a broken spell. Same rule RewireWarps
    /// applies to warp tiles.</summary>
    [Fact]
    public void A_destination_with_no_clone_falls_back_to_the_base_map()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileSpellEffectScriptFromShipped();

        fixture.AddBaseMap(2, "Cave", width: 100, height: 100);
        var dim3Town = fixture.AddBaseMap(1 + Offset * 3, "Town (3)", width: 100, height: 100);
        dim3Town.ScriptParams = "3";

        var effect = new SpellEffect
        {
            ID = 42, EffectType = SpellEffect.EffectTypes.Script, ScriptParams = Offset.ToString(),
            TeleportMapID = 2, TeleportMapX = 7, TeleportMapY = 8,
            Effected = SpellEffect.SpellEffected.Self, MaximumLevelEffected = 99,
        };

        var player = fixture.PlayerOn(dim3Town, x: 50, y: 50);

        Assert.True(script.Object.Cast(effect, player, player, fixture.World));
        Assert.Equal(2, player.Map.ID);
    }
}
```

Two more fixture helpers are needed. Add them next to the existing ones:

```csharp
/// <summary>Compiles the shipped DimensionTeleport.csx, so tests exercise what ships.</summary>
public Script<ISpellEffectScript> CompileSpellEffectScriptFromShipped(
    string fileName = "DimensionTeleport.csx")
{
    InstallShippedScripts();
    return World.ScriptHandler.GetScript<ISpellEffectScript>("Scripts/Spell/" + fileName);
}

/// <summary>A Player already placed on a map, which is the minimum WarpTo needs. Moved
/// here verbatim from DimensionMapScriptTests.cs:145, which Part 2 added for the same
/// purpose.</summary>
public Player PlayerOn(Map map, int x, int y)
{
    return new Player(0)
    {
        Map = map,
        MapID = map.ID,
        MapX = x,
        MapY = y,
    };
}
```

**This is a move, not a copy.** `PlayerOn` is currently a private static helper at
`Goose.Tests/DimensionMapScriptTests.cs:145–154`. Delete it there and update that file's
three call sites (`:62`, `:82`, `:106`) to use the fixture's copy, so there is one
definition. Those tests already hold a `GlobalScriptFixture`, so the change is
`PlayerOn(dim3, ...)` → `fixture.PlayerOn(dim3, ...)`.

**Step 2: Run tests to verify they fail**

Run: `dotnet test Goose.sln --filter DimensionTeleportScriptTests`
Expected: FAIL — teleport effects are still `EffectTypes.Teleport`, and the stub script's inherited `Cast` returns `true` without warping.

**Step 3: Write the script**

Replace `Goose/Data/Illutia/Scripts/Spell/DimensionTeleport.csx`:

```csharp
using System;
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

/// <summary>Replaces SpellEffect.CastTeleportSpell (SpellEffect.cs:702) for every
/// teleport effect, including dimension 0. The only behaviour change is that the
/// destination resolves in the caster's dimension - abyss does the same thing by passing
/// the caster's dimension to getMap (SpellEffect.java:833).
///
/// One instance serves every teleport effect: ScriptHandler caches one object per path
/// (ScriptHandler.cs:19), so this class must stay stateless and read everything from
/// thisEffect and its ScriptParams.</summary>
public class DimensionTeleport : BaseSpellEffectScript
{
    /// <summary>Used only if Dimensions.csx did not set ScriptParams.</summary>
    private const int DefaultOffset = 100000;

    private int OffsetOf(SpellEffect effect)
    {
        int offset;
        return int.TryParse(effect.ScriptParams, out offset) && offset > 0 ? offset : DefaultOffset;
    }

    /// <summary>Dimensions.csx sets ScriptParams to the dimension number when it clones a
    /// map, the same convention DimensionMap.csx reads.</summary>
    private int DimensionOf(Map map)
    {
        int dim;
        return map != null && int.TryParse(map.ScriptParams, out dim) ? dim : 0;
    }

    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target,
                              GameWorld world)
    {
        // CastSpell guards Teleport with "target is Player" (SpellEffect.cs:939);
        // CastScriptSpell (:975) has no such guard, so it has to be repeated here.
        var player = target as Player;
        if (player == null) return false;

        if (!thisEffect.CanCastSpell(caster, target)) return false;

        if (thisEffect.Animation != 0)
        {
            var range = target.Map.GetPlayersInRange(target);
            string packet = P.SpellPlayer(target.LoginID, thisEffect.Animation, thisEffect.AnimationFile);
            world.Send(player, packet);
            foreach (var other in range) world.Send(other, packet);
        }

        Map map = ResolveDestination(thisEffect, caster, world);

        // A missing destination means "return to bound spot" - used for gate spells.
        if (map == null)
        {
            player.WarpTo(world, player.BoundMap, player.BoundX, player.BoundY);
            return true;
        }

        if (!map.PlayerCanJoin(player, world)) return false;

        player.WarpTo(world, map, thisEffect.TeleportMapX, thisEffect.TeleportMapY);
        return true;
    }

    /// <summary>The dimension clone of the destination, falling back to the base map when
    /// that dimension has no copy - an exit from the dimension rather than a dead spell.</summary>
    private Map ResolveDestination(SpellEffect thisEffect, ICharacter caster, GameWorld world)
    {
        if (thisEffect.TeleportMapID == 0) return null;

        int dim = DimensionOf(caster.Map);
        return world.MapHandler.GetMap(thisEffect.TeleportMapID + OffsetOf(thisEffect) * dim)
            ?? world.MapHandler.GetMap(thisEffect.TeleportMapID);
    }

    /// <summary>Restores the line the built-in switch would have produced
    /// (SpellEffect.cs:446), which the rewrite to EffectTypes.Script would otherwise drop.
    /// Resolved against the base map, since a description is rendered outside any cast.</summary>
    public override IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        var map = world.MapHandler.GetMap(thisEffect.TeleportMapID);
        if (map == null) return new[] { "Teleport to bound location" };

        return new[]
        {
            "Teleport to " + map.Name + " (" + thisEffect.TeleportMapX + ", "
                + thisEffect.TeleportMapY + ") in your current dimension"
        };
    }
}

return typeof(DimensionTeleport);
```

**Step 4: Write the rewrite pass**

Add `RewriteTeleportEffects(world);` to `OnLoaded` immediately after `CloneSpells(world);`:

```csharp
/// <summary>Step 4 of the spell pass, and the last thing the spell work does. Every
/// teleport effect - dimension 0 included - becomes a script effect so its destination
/// resolves in the caster's dimension.
///
/// Dimension 0 is deliberate: class level-up spells stay at dimension 0, so that copy is
/// the teleport every player actually holds. Skipping it would leave a way out of any
/// dimension.
///
/// Runs after CloneSpellEffects so the clones were still Teleport-typed when they were
/// copied, and one pass here converts base and clones together.</summary>
private void RewriteTeleportEffects(GameWorld world)
{
    var script = world.ScriptHandler.GetScript<ISpellEffectScript>("Scripts/Spell/DimensionTeleport.csx");

    foreach (var effect in world.SpellHandler.GetSpellEffects().ToList())
    {
        if (effect.EffectType != SpellEffect.EffectTypes.Teleport) continue;

        effect.EffectType = SpellEffect.EffectTypes.Script;
        effect.Script = script;
        effect.ScriptParams = Offset.ToString();
    }
}
```

`TeleportMapID`/`X`/`Y` are left in place — the script reads them.

**Step 5: Run tests to verify they pass**

Run: `dotnet test Goose.sln --filter DimensionTeleportScriptTests`
Expected: PASS (5 tests).

**Step 6: Commit**

```bash
git add -A
git commit -m "feat: resolve teleport spells within the caster's dimension"
```

---

## Task 8: End-to-end verification

**Files:** none — this task runs things and records what it found.

**Step 1: Full suite**

Run: `dotnet test Goose.sln`
Expected: **327 passed**, 0 failed, 26 skipped. Tools.Tests must still be 124 passed / 26 skipped.

If the count differs from the budget table, reconcile it — a missing test is as much a defect as a failing one.

**Step 2: Confirm the disabled path**

Set `Enabled = false` in `Dimensions.csx`, run the suite, and confirm the dimension tests fail *because nothing was generated* rather than by throwing. Then set it back to `true`. This is a manual check that the escape hatch still works end to end — Part 2 covers the flag itself with a compiled variant test.

**Step 3: Start the server against Illutia data**

Follow the project's normal start procedure. Confirm:

- No exceptions during the Global Scripts load step.
- Spell count `207 × 7 = 1,449` and effect count `623 × 7 = 4,361`. `SpellHandler.Count` and `.EffectCount` are the values to read.
- Startup time and RSS for the global-scripts step, recorded alongside Part 2's measurement (633 ms / 461 MB on Aspereta). Spells add ~5,000 objects against Part 2's ~82,000 NPCs, so the delta should be small — if it is not, say so rather than rounding it away.

**Step 4: Walk the teleport path**

1. Enter dimension 3 (`/dimension 3`, requires unlock progress — grant it via `Player.Properties` if needed).
2. Cast a teleport spell.
3. Confirm the destination map is the dimension-3 copy, not the dimension-0 one.
4. Return to dimension 0 and cast the same spell; confirm it lands in dimension 0.
5. Open the spell info window for that spell and confirm a destination line still renders, now ending *"in your current dimension"*.

**Step 5: Record the results**

Append the measurements to the design doc's testing section, the way Part 2 recorded its startup measurement under follow-up 2. Do not skip this — the numbers are the only evidence the scale assumptions held.

**Step 6: Commit**

```bash
git add -A
git commit -m "docs: record dimension spell verification measurements"
```

---

## Done when

- `dotnet test Goose.sln` reports 327 passed, 0 failed, 26 skipped.
- Every base spell and effect has six dimension copies at `id + 100000·dim`, with abyss scaling, and the base objects are unchanged apart from the stacking ladder and the teleport rewrite.
- Cross-references and stacking lists resolve within their own dimension, and the ladder makes a higher-dimension buff replace a lower one while the reverse is refused.
- Every teleport effect is script-backed, resolves its destination in the caster's dimension, and still renders a destination line in the spell info window.
- Nothing in `Goose/` mentions dimensions.
