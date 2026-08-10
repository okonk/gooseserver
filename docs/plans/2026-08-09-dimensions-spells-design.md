# Dimensions Part 3 — Spells — Design

Date: 2026-08-09

## Summary

Generate per-dimension copies of every spell and spell effect at `id + 100000·dim`,
mirroring the abyss scaling, and make teleport spells resolve within the caster's
dimension.

This is part 3 of the dimensions feature. Parts 1 (server extension points) and 2 (world
cloning) are merged at `f1de68c`. Items and the spirit economy remain out of scope — see
[Out of scope](#out-of-scope).

Source of truth for behaviour is `~/code/abyssserver` (Java). Line references point at it.

## Scope

**In scope**

- Per-dimension `Spell` and `SpellEffect` clones, with abyss scaling
- Cross-reference rewiring (`OnMeleeAttackSpell`, `OnMeleeHitSpell`, buff stacking lists)
- A dimension ladder on buff stacking, so higher-dimension buffs beat lower ones
- In-dimension teleport resolution, via a spell-effect script

**Out of scope**

- Acquisition. Class level-up spells stay dimension 0 by decision (see below); the only
  path to a dimension spell is a dimension spell tome, which arrives with item cloning.
  This part generates the spells; nothing in the game can learn them yet.
- Item template clones, prefixes, rarity, surnames, `/resetitem`
- Spirit (SP) economy, vendor overrides, `/rebirth`

## Scale

Measured against `~/Downloads/IllutiaGoose_2025-04-15.db`:

| Entity | Base | Max base ID | ×6 dimension clones |
|---|---|---|---|
| Spells | 207 | 208 | 1,242 |
| Spell effects | 623 | 623 | 3,738 |
| — of which `Teleport` | 26 | | 156 |

Both max IDs sit far below the 100000 offset. Negligible next to the ~82,000 NPCs Part 2
already generates.

## Decisions

| Decision | Value | Rationale |
|---|---|---|
| Clone keying | `id + 100000·dim`, same offset as Parts 1–2 | Spellbook persists a plain `int[]` of spell IDs (`Spellbook.cs:77`), so dimension spells round-trip with no schema change |
| Class level-up spells | Always dimension 0 | User decision. Abyss learns them at the player's current dimension (`Player.java:1587,1892`), which only pays off once `/rebirth` exists to re-level inside a dimension. Deferred with the economy |
| Acquisition | Dimension spell tomes only | Follows from the above. A dimension-`d` tome carries `LearnSpellID + 100000·d` |
| Tome collision | Upgrade in place | A known dimension-0 spell is replaced by a higher-dimension copy in its existing slot; same or lower is refused |
| Where the upgrade lives | Item script, in the **items** part | Needs no server change at all — see [Spellbook upgrade](#spellbook-upgrade-deferred-to-the-items-part) |
| Teleport resolution | Rewrite `Teleport` effects to `EffectTypes.Script` | User decision. Abyss resolves `getMap(id, caster.getDimension())` at cast time (`SpellEffect.java:833`) and never rewrites `TeleportMapID` |
| Teleport rewrite covers dimension 0 | Yes | Class spells are dimension 0, so that is the teleport every player actually holds. Skipping it leaves the escape hatch open |
| Destination with no clone in the caster's dimension | Fall back to the base map | **Deliberate deviation from abyss**, which does an exact `(map id, dimension)` lookup and treats a miss as null — i.e. warps to the bound spot. Falling back to the base map matches what Part 2 already does for warp tiles (`Dimensions.csx:179`, `target ?? warp.WarpMap`), so a destination that was never cloned behaves the same whether you walk to it or teleport to it. See limitation 5 |
| Buff stacking | Dimension ladder, extended to every list entry | Diverges from abyss, which used same-dimension lists only. See [Buff stacking](#buff-stacking) |
| `dimensionSpellStats` | Clone the `AttributeSet`, then scale | Abyss builds a fresh set and fills only the listed fields, silently zeroing `SP` and `MoveSpeed`. Treated as an abyss bug |
| Name decoration | `Powerful…Godly` prefix, per abyss | Inconsistent with the `" (n)"` suffix Parts 1–2 use for maps and NPCs, accepted: the prefix reads better in a spellbook and distinguishes tiers at a glance |

---

## Server changes

Two, both generic — neither mentions dimensions. Plus two additive copy constructors.

### 1. `SpellHandler` registration and enumeration

Mirrors `NPCHandler.AddTemplate` / `GetTemplates()` from Part 1.

```csharp
public void AddSpell(Spell spell);              // overwrites an existing id
public void AddSpellEffect(SpellEffect effect); // overwrites an existing id
public IEnumerable<Spell> GetSpells();
public IEnumerable<SpellEffect> GetSpellEffects();
```

`GetSpellEffects()` is what the teleport rewrite iterates. `AddSpell`/`AddSpellEffect` are
also what lets `GlobalScriptFixture` seed base spells in tests.

### 2. `ISpellEffectScript.GetItemDescription`

`SpellEffect.GetItemDescription` (`SpellEffect.cs:398`) has a `case EffectTypes.Teleport`
(`:446`) rendering *"Teleport to Kanaphuk (12, 30)"*. Rewriting teleport effects to
`EffectTypes.Script` drops them to the `default:` branch and loses that line — in the spell
info window (`SpellInfoWindow.cs:50`) and item tooltips (`Packets.cs:461,515`), in
dimension 0 too.

```csharp
// null or empty => fall through to the built-in switch, so existing scripts are unaffected
IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world);
```

`SpellEffect.GetItemDescription` consults `this.Script` first. `BaseSpellEffectScript`
gets a virtual returning null.

### 3. Copy constructors

`Spell(Spell)` and `SpellEffect(SpellEffect)`, mirroring `NPCTemplate(NPCTemplate)` from
Part 1 task 4. Both give the copy its own list instances (`BuffStacksOver`,
`BuffDoesntStackOver`) and its own `AttributeSet` via the `stats + new AttributeSet()`
idiom. `Script` is shared — compiled scripts are cached per path and stateless.

Every property must be copied; the failure mode this guards against is a field added to
`SpellEffect` later and forgotten here. Leave a comment on the field block pointing at the
constructor, as `Map.CloneAs` does.

---

## Scripts

### Generation pass in `Dimensions.csx`

Runs at `OnLoaded`, gated on the existing `Enabled` flag, in a preflight plus four ordered
steps. The order is load-bearing.

**Step 0 — preflight every id.** Before anything is registered, check that every base spell
and effect id is in `0 .. Offset-1`, and that no generated id is already taken. Two reasons
it is a preflight rather than a check inside the clone loops. A throw from halfway through
leaves the handler half-mutated — thousands of effects registered, spells not, references
unwired — which is worse to hand someone than a clean refusal. And "base" means `ID < Offset`
everywhere downstream (step 2 filters on exactly that), so an id above the offset is not
merely a collision risk: it would be cloned in step 1 and then skipped in step 2, producing a
spell that exists but never stacks or resolves correctly.

**Step 1 — clone effects.** For each base effect × dims 1–6, copy and scale (below).

**Step 2 — rewire cross-references.** A separate pass for the same reason ally rewiring
was in Part 2: clone order is dictionary order, so a referenced effect's clone may not
exist when the effect referencing it is built. Rewires `OnMeleeAttackSpell` /
`OnMeleeAttackSpellID`, `OnMeleeHitSpell` / `OnMeleeHitSpellID`, and both buff stacking
lists. A reference with no same-dimension clone is dropped rather than left pointing at
dimension 0 — same rule Part 2 applied to allies.

**Step 3 — clone spells.** For each base spell × dims 1–6, copy and scale, pointing
`SpellEffectID` and `SpellEffect` at the same-dimension effect clone.

**Step 4 — rewrite teleports.** One pass over `GetSpellEffects()`, covering base and
clones together, setting `EffectType = Script`, `Script = DimensionTeleport.csx` and
`ScriptParams = Offset`. Must run after steps 1–3 so clones are still `Teleport`-typed
when copied.

A bad or colliding ID throws from step 0 with the offending ID, matching how Part 2 handles
map and template collisions — `AddSpell` overwrites silently otherwise. Once the range check
passes, `id + Offset·dim` over `dim ∈ 1..6` is injective, so the collision half of the
preflight is a backstop rather than a reachable branch; it stays in because the cost of being
wrong is a real spell silently disappearing.

### Scaling

Transcribed from `SpellHandler.java:275–330` and `AttributeSet.dimensionSpellStats`
(`AttributeSet.java:347`).

**Name prefixes** by dimension: `Powerful`, `Super Powerful`, `Supreme`, `Omnipotent`,
`Almighty`, `Godly`. Description prefix `"Abyss (n) "`.

**Spell**, dimension > 0:

| Field | Formula |
|---|---|
| `Name` | prefix + name |
| `Description` | `"Abyss (n) "` + description |
| `Aether` | `× 0.9^dim` |
| `HPStaticCost`, `MPStaticCost` | `× 3^dim` |
| `SPStaticCost` | **unchanged** — abyss leaves it alone |

**SpellEffect**, dimension > 0, applied in this order:

1. `Name` = prefix + name
2. `MinimumLevelEffected` = 50 for `Buff`/`Permanent`, else 1
3. `Duration × 1.15^dim`
4. `Stats` scaled (table below)
5. `TauntAggro`, when > 0: `× 3^dim + 100000 × 20^dim`
6. `TargetSize += dim`
7. `HPFormula`, `MPFormula` → `"(" + formula + ") * " + targetScale × 1.25^dim`, where
   `targetScale` is `1.15` for `TargetTypes.Target` and `1.0` otherwise. Empty formulas
   are left alone. **Computed before step 8** — abyss reads `TargetType` first
8. Shape morph:
   - `Cross` → `Area`, `TargetSize = dim`
   - `Plus` → `Area`, `TargetSize = dim`
   - `LineFront` → `TargetSize = dim + 1` if the base size was 3, else `dim`; type becomes
     `Plus` if the base size was ≤ 1, else `TriangleFront`

`TargetTypes` and `EffectTypes` are enum-identical between goose and abyss, so the morph
transcribes directly.

**Stats scaling.** Unlike abyss, start from a clone of the base `AttributeSet` so
unlisted fields survive, then scale:

| Multiplier | Fields |
|---|---|
| `× (dim+1)²` | `HP`, `MP` |
| `× 4^dim` | `HPStaticRegen`, `MPStaticRegen` |
| `× (1 + 0.5·dim)` | `AC`, `DamageReduction`, `Haste`, `HPPercentRegen`, `MPPercentRegen`, `MeleeCrit`, `MeleeDamage`, `SpellCrit`, `SpellDamage` |
| `× dim` | `FireResist`, `AirResist`, `EarthResist`, `WaterResist`, `SpiritResist`, `Strength`, `Stamina`, `Intelligence`, `Dexterity` |
| unchanged | everything else, including `SP` and `MoveSpeed` |

Abyss returns a fresh `AttributeSet` populated with only the scaled fields, which zeroes
`SP` and `MoveSpeed` — a movement-speed buff stops working entirely in dimensions. Treated
as a bug and not reproduced.

### Buff stacking

`Player.AddBuff` (`Player.cs:2074–2083`) and `NPC.AddBuff` (`NPC.cs:1473–1477`) already
provide both behaviours, as reference comparisons against `SpellEffect`. This is therefore
pure list population — no server change.

- `BuffDoesntStackOver.Contains(existing)` → the new buff is refused, *"The buff had no
  effect."*
- `buff.SpellEffect == b.SpellEffect || BuffStacksOver.Contains(existing)` → replaces in
  place, refreshing the timer and swapping the stats

Checked in that order, so `DoesntStackOver` wins ties — the precedence the ladder wants.

For the dimension-`d` copy of effect `E`, where `clones(X)` means every dimension copy of
`X` including dimension 0:

```
StacksOver(E,d)      = { clone(X,k) : X ∈ baseStacksOver(E) ∪ {E},  k ≤ d }
DoesntStackOver(E,d) = { clone(X,k) : X ∈ baseDoesntStackOver(E),   any k }
                     ∪ { clone(X,k) : X ∈ baseStacksOver(E) ∪ {E},  k > d }
```

Read the two `baseStacksOver(E) ∪ {E}` lines together: every effect the base `E` supersedes,
plus `E` itself, is partitioned by dimension — copies at or below `d` are stacked over,
copies above `d` refuse the cast. Nothing in that set can land in neither list, which is the
double-application bug below.

Worked through: Supreme Bless (dim 3) over plain Bless (dim 0) replaces it; plain Bless
over Supreme Bless is refused. A same-dimension recast still takes the existing
`buff.SpellEffect == b.SpellEffect` path, untouched.

This also mutates the **dimension-0** effect's `DoesntStackOver` to include dims 1–6 of
itself *and of everything it stacks over*, so the base spell can overwrite neither its own
upgrades nor a higher-dimension copy of a lesser buff it supersedes. That mutates a shared
base object and is gated behind `Enabled`.

**Why the ladder extends to every list entry, not just `E`.** If base Bless stacks over
Minor Bless, a dim-3 Bless meeting a dim-0 Minor Bless would match neither list and be
added as a *second* buff, applying both stat blocks at once. So `StacksOver` covers the
whole base list, at every dimension up to `d`.

**And why `DoesntStackOver` extends to it too.** The mirror case is the same bug: dim-3
Bless meeting a dim-**5** Minor Bless. Dimension 5 is above 3, so it is not in
`StacksOver`, and putting only higher copies of *Bless itself* into `DoesntStackOver` leaves
dim-5 Minor Bless in neither list — both buffs apply. Extending both halves of the ladder to
every base-list entry closes it: every dimension copy of everything `E` relates to lands in
exactly one list. Abyss used same-dimension lists only and has both variants of the
double-application bug.

The asymmetry that remains is inherited from the sheet data, not introduced here: base Minor
Bless does not list Bless, so casting Minor Bless while Bless is up stacks a second buff at
dimension 0 today, and the ladder reproduces that at every dimension rather than fixing it.

### `Scripts/Spell/DimensionTeleport.csx`

Implements `ISpellEffectScript.Cast`, re-implementing `SpellEffect.CastTeleportSpell`
(`SpellEffect.cs:702`) with one change — the map resolves in the caster's dimension:

```csharp
var dim = DimensionOf(caster.Map);                    // Map.ScriptParams, as DimensionMap.csx
var map = world.MapHandler.GetMap(TeleportMapID + Offset * dim)
       ?? world.MapHandler.GetMap(TeleportMapID);     // no clone -> the base map
```

**Two** behaviour changes, then, not one: the dimension lookup, and the base-map fallback on
the second line. Abyss has no fallback — its `getMap(id, dimension)` miss returns null, which
drops into the bound-location path below. The fallback is the deliberate deviation recorded
in [Decisions](#decisions) and [limitation 5](#known-limitations); everything else is
verbatim.

Everything else carries across verbatim: the `CanCastSpell` guard, the `P.SpellPlayer`
animation broadcast to the target and everyone in range, the null-map fallback warping to
the player's bound spot (gate spells), and the `PlayerCanJoin` refusal. All of those
members are public, so the script needs nothing further from the server.

Two details the rewrite must not lose:

- **The `target is Player` guard.** `SpellEffect.cs:939` dispatches
  `EffectTypes.Teleport when target is Player`; `CastScriptSpell` (`:975`) has no such
  guard, so the script must check it itself or teleport effects will fire on NPCs.
- **Statelessness.** `ScriptHandler.GetScript` caches one instance per path, so all 26
  teleport effects share a single `DimensionTeleport` object. It must read `thisEffect`
  and `ScriptParams` per call and hold no fields — the same contract `IQuestScript`
  documents.

`ScriptParams` carries the offset so the script is not hardcoded against `Dimensions.csx`.

It also implements `GetItemDescription`, returning *"Teleport to Kanaphuk (12, 30) in your
current dimension"* — strictly better than the line it replaces, and resolved against the
base map name since the description is rendered outside any cast.

### Spellbook upgrade, deferred to the items part

The upgrade-in-place rule needs **no server change**, and belongs with the tomes that
trigger it.

`Inventory.cs:279` calls `player.LearnSpell` directly for `UseTypes.Scroll` with no script
hook, so tomes cannot be intercepted as they stand. But dimension tomes are generated by
the items part's script, which can emit them as `UseTypes.OneTime` with an `IItemScript`
attached. `OnUseConsumableEvent` already exists (`Inventory.cs:427`) and already returns a
bool for whether to consume the item; `Player.Spellbook`, `Spellbook.GetSlot`,
`RemoveSpell` and `AddSpell` are all public.

```csharp
// items part -- DimensionTome.csx, recorded here so the rule is not lost
public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
{
    var incoming = world.SpellHandler.GetSpell(item.LearnSpellID);
    var known = FindSlotByBaseId(player.Spellbook, incoming.ID % Offset);

    if (known.HasValue)
    {
        if (known.Value.Spell.ID / Offset >= incoming.ID / Offset) return false;  // same or lower
        player.Spellbook.RemoveSpell(known.Value.Slot, world);
    }
    return player.Spellbook.AddSpell(incoming, world);
}
```

Cost: tomes render as consumables rather than scrolls in the client. Cosmetic, and the
items part regenerates those templates regardless.

---

## Known limitations

Accepted, listed so they are not mistaken for bugs.

1. **Nothing can learn a dimension spell yet.** Acquisition is a dimension spell tome,
   which arrives with item cloning. This part is verified by integration tests and by the
   teleport behaviour, which is the only player-visible change on a running server.
2. **Disabling the feature loses learned dimension spells.** Spellbook slots persist raw
   IDs. With `Enabled = false`, `GetSpell(400091)` returns null and `Spellbook.Load`
   (`Spellbook.cs:44`) silently nulls the slot. Same class as Part 2's limitation 5.
3. **Class level-up spells are dimension 0 forever.** Abyss learns them at the player's
   current dimension; that only becomes meaningful once `/rebirth` resets a character to
   level 1, so it is deferred with the economy.
4. **`GetSpellByName` sees dimension copies.** `SpellHandler.cs:277` returns the first name
   match. Prefixed clone names mean no collision with base spells, but a caller searching
   for `"Powerful Firestorm"` now resolves.
5. **A teleport whose destination was never cloned lands in dimension 0.** Per the decision
   above, a missing dimension map falls back to the base map rather than to the bound spot,
   so such a spell is an exit from the dimension. Abyss would send the player to their bind
   instead. Only reachable for maps excluded from cloning; with Part 2 cloning every map it
   cannot fire on shipped data.
6. **The stacking ladder inherits the sheet data's asymmetries.** It makes every dimension
   copy of a related effect resolve to exactly one list, but it does not add relationships
   the base data never had — if base Minor Bless does not list Bless, Minor Bless still
   stacks a second buff on top of Bless, in every dimension as at dimension 0.

## Follow-ups

1. **Extending the `IItemScript` hook to `UseTypes.Scroll`.** Would let dimension tomes
   stay scrolls rather than becoming consumables. Not needed for correctness.
2. **Class spells at the player's current dimension**, per limitation 3, once `/rebirth`
   lands.

## Testing

**Unit-testable in `Goose.Tests`** — `SpellHandler.AddSpell` / `AddSpellEffect` /
`GetSpells` / `GetSpellEffects`; the `Spell` and `SpellEffect` copy constructors;
`ISpellEffectScript.GetItemDescription` falling through to the built-in switch when the
script returns null.

The copy constructors get a **reflection-driven** completeness guard rather than a
hand-written list of assertions: the test sets every public property on the source via
reflection and then walks the properties again on the copy. A property added to `Spell` or
`SpellEffect` and forgotten in the constructor fails the test the day it is added, which a
hand-written list cannot do. Deep-vs-shared semantics (`Stats`, the two stacking lists,
`Script`) are asserted separately, because the guard can only prove the value came across.

**Integration-testable via `GlobalScriptFixture`** — everything the generation pass
produces: scaled spells and effects at the right IDs, the base objects left untouched,
cross-reference rewiring within a dimension, references with no clone dropped, the buff
stacking ladder in all three directions (lower, higher, and a *related* effect at a higher
dimension), the shape morph for `Cross`, `Plus` and both `LineFront` branches, formula
wrapping, the ID preflight refusing bad configuration before anything is mutated, and
teleport effects rewritten to `Script` type.

Teleport behaviour is tested **through the production dispatch path** — `SpellEffect.CastSpell`
on the effect retrieved from `SpellHandler` after `OnLoaded` — not by calling the script
object directly. Calling `Script.Object.Cast` proves the script works; only the dispatch path
proves the rewrite attached it, that `EffectTypes.Script` routes to it, and that the script
re-implements the `target is Player` guard that `CastSpell` applied to `EffectTypes.Teleport`
and does not apply to `EffectTypes.Script`. That covers the destination, the bound-location
fallback, a `PlayerCanJoin` refusal, an NPC target, and `GetItemDescription` on the shipped
rewritten effect.

The **disabled path** is a compiled variant, the same technique Part 2 uses
(`DimensionsScriptTests.Disabled_by_configuration_changes_nothing`): read the shipped source,
substitute `Enabled = false`, compile that, and positively assert no spells or effects were
generated and that teleport effects still have `EffectTypes.Teleport`. Editing the shipped
file and expecting the suite to fail is not a test.

The fixture needs extending: a `Scripts/Spell/` directory, `DimensionTeleport.csx` in
`ShippedScripts` and in `Goose.Tests.csproj` as a `<None Include>`, and a helper to seed
base spells and effects (which `AddSpell`/`AddSpellEffect` now allow).

**Manual** — start the server with `Enabled = true`, confirm spell and effect counts
(1,449 and 4,361), then cast a teleport spell from inside dimension 3 and confirm it lands
in dimension 3 rather than dimension 0. Check a teleport spell's tooltip still renders a
destination line.

## Implementation parts

Single part. The server surface is two small additions plus two copy constructors; the
rest is one generation pass in an existing script and one new spell script.
