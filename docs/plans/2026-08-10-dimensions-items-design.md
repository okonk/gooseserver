# Dimensions Part 4 — Items — Design

Date: 2026-08-10

## Summary

Give every dimension its own copy of the game's equipment and spell tomes, at
`baseId + 100000·dim`, with the abyss name prefixes, recolour, deterministic stat
scaling, rarity roll and suffix roll. Repoint dimension NPC drop tables at those
clones, so dimension loot — and, for the first time, the dimension spells Part 3
generated — becomes obtainable.

Parts 1–3 (world cloning, NPC scaling, access, spells) are merged at `a41a79f`.
The spirit economy remains out of scope; see [Out of scope](#out-of-scope).

Source of truth for behaviour is `~/code/abyssserver` (Java). Line references below
point at it or at this repository, as marked.

## Scope

**In scope**

- Five server extension points (all generic)
- Item template cloning: equipment and spell tomes
- Rarity titles and the six abyss suffixes, as registered `ItemModifier`s
- `DimensionItem.csx`: the roll, pickup gating, the tome upgrade rule, script delegation
- Drop-table repointing
- Map-script delegation — a Part 1 fix, shipped here because it shares the pattern

**Out of scope** — Part 5, the spirit economy

- Vendor purchase/sell overrides and dimension vendor stock
- `/resetitem`, `/rebirth`, SP as a currency
- Crafting restricted to a single dimension

## Decisions

| Decision | Value | Rationale |
|---|---|---|
| Clone set | Armor (766), Weapon (217), learn-spell Scroll (215) | ~1,199 × 6 = ~7,194 clones. Abyss never scales consumables (`Item.java:404`); money and NoUse items have nothing to scale |
| Item ids | `base + 100000·dim` | Base ids top out at 1606, so clones reach 601,606 — clear of Part 1's Warden templates (800,000) and quest ids (900,000) |
| Suffixes | Registered `ItemModifier` surnames | Items carry the normal `ItemProperties[SurnameId]`, so existing plumbing and a later `/resetitem` work unchanged |
| Rarities | Registered `ItemModifier` titles | Symmetric with suffixes |
| Modifier chance | `0` for all eight | `RollModifier` builds ranges as `(int)(Chance * 100)`; zero yields an empty range, so they can never be selected natively. The script picks them by id |
| Native rolls on dimension items | Suppressed via `IItemScript.OnRollModifiersEvent` | Dimension-0 loot keeps goose's twenty titles/surnames exactly as today; dimension items get only the abyss roll |
| Gear gating | `CanPickup` only, no equip hook | User decision. Blocks looting above your dimension; traded gear can still be worn |
| Value on clones | `base × 3^dim`, immediately | User decision. Final data now; the gold exploit is accepted until Part 5 — see [Known limitations](#known-limitations) |
| Tomes | `UseTypes.OneTime` + script | Enables the upgrade rule recorded in Part 3's design with no server change. Cost: they render as consumables client-side |
| Scripted base templates | Delegation | The wrapper forwards to the base template's script. Covers Okonk Illusion Sword and Zombie Leg Illusion |
| Stat scaling location | Baked into the cloned template's `BaseStats` | Equivalent to abyss's per-item add — see [Stat scaling](#stat-scaling) |
| `bonusStats` tier 1.5 | Dropped | Abyss keys it off `getIsSPValue()`; goose has no SP-value concept |
| `MeleeDamage` term | Faithful to abyss, `(int)` truncation included | `AttributeSet.java:433` grants `10·dim·tier` on a stat both servers apply as `damage *= (1 + MeleeDamage)` (`Player.java:1809`, `Goose/Player.cs:1616`) — i.e. `+1000%·dim·tier`, with any base product under 1.0 truncated to zero. Out of scale with every other term in the method (`0.04`, `0.015`), but ported as-is: balance is abyss's to own. User decision |

## Server changes

All five are generic and additive. `BaseItemScript` supplies defaults, so the four
existing item scripts are unaffected.

1. **`ItemHandler.AddTemplate(ItemTemplate)`** — register a generated template.
   Mirrors Part 3's `SpellHandler.AddSpell`.
2. **`ItemHandler.AddTitle` / `AddSurname(ItemModifier)`** — register generated
   modifiers.
3. **`IItemScript.CanPickup(Player, Item, GameWorld) → string`** — refusal message,
   or null to allow. Consulted in `PickupItemEvent` (`Goose/Events/PickupItemEvent.cs:88`)
   before `Inventory.AddItem`. Mirrors `IMapScript.CanPlayerJoin`, which Part 1 added.
4. **`IItemScript.OnRollModifiersEvent(Item, GameWorld) → bool`** — returning true
   skips the native rolls. Consulted at the **top** of `ItemHandler.RollTitleAndSurname`,
   above its non-armor/weapon early return (`Goose/ItemHandler.cs:244`), so tomes reach
   the script too.
5. **`ItemTemplate` copy constructor** — the additive pattern Part 3 used for
   `Spell`/`SpellEffect`.

## Template cloning

For each in-scope base template and each dimension 1–6:

| Field | Value | Source |
|---|---|---|
| `ID` | `base + 100000·dim` | |
| `Name` | `Powerful` / `Super Powerful` / `Supreme` / `Omnipotent` / `Almighty` / `Godly ` + base name | `Item.java:408–427` |
| `Description` | `"Abyss (n) "` + base description | `Item.java:429–430` |
| `GraphicR/G/B` | `max(base − 30·dim, 0)` | `Item.java:441–443` |
| `GraphicA` | `min(base + 30·dim, 200)` | `Item.java:444` |
| `Value` | `base × 3^dim` | `Item.java:445` |
| `BaseStats` | `base + dimensionDefault(dim, itemType: 0, baseTemplate)` | `AttributeSet.java:376` |
| `IsLore`, `IsBindOnPickup`, `IsBindOnEquip` | `false` | `Item.java:225–260` |
| `MinLevel`, `MinExperience`, `ClassRestrictions`, `Slot`, `Type` | copied | |
| `Script` | `Scripts/Item/DimensionItem.csx` | |
| `ScriptParams` | copied from base | so delegated scripts see their own params |

Tomes additionally get `UseType = OneTime` and `LearnSpellID = base + 100000·dim`.

### Stat scaling

`AttributeSet.dimensionDefault` (`AttributeSet.java:376–444`) computes two things at
once: a flat per-dimension bonus, and a suffix-specific bonus selected by `itemType`.
The split here is:

- **Flat part** — call it with `itemType: 0`, so every suffix term multiplies out to
  zero, and bake the result into the clone's `BaseStats`.
- **Suffix part** — the six terms, applied at roll time by `DimensionSurname.csx`.

Baking into the template is equivalent to abyss's per-item add. Abyss computes
`(template.BaseStats + item.BaseStats + dimensionDefault) × StatMultiplier`
(`Item.java:459–463`); goose's `RefreshStats` computes
`(template.BaseStats + item.BaseStats) × StatMultiplier` (`Goose/Item.cs:247–256`).
Folding `dimensionDefault` into `template.BaseStats` makes the two identical — in
particular a Legendary roll still multiplies the dimension bonus.

`bonusStats` (the item tier) is computed from the **base** template, before the value
scaling, or every clone would land in the top tier:

| Condition | Tier |
|---|---|
| `Value ≥ 10_000_000` | 1.0 |
| `MinExperience > 0` | 0.75 |
| `MinLevel == 50` | 0.5 |
| otherwise | 0.25 |

## `DimensionItem.csx`

One stateless script, shared by every clone — `ScriptHandler.GetScript` caches by path
(`Goose/Scripting/ScriptHandler.cs:19–31`), so all ~7,194 templates hold one reference.
Dimension is recovered as `item.TemplateID / Offset`.

- **`OnRollModifiersEvent`** — the abyss roll (`Item.java:359–402`): 45% suffix in six
  equal 7.5% bands, then 2% Legendary (`StatMultiplier` 1.25) / 2% Stunted (0.5).
  Applied through the registered modifiers, so `ItemProperties[SurnameId]` and
  `[TitleId]` are set as usual. Returns true, so no native modifier lands on a
  dimension item.
- **`CanPickup`** — refuses when `dim > player.Properties["dimension.max"]`.
- **`OnUseConsumableEvent`** — the tome upgrade rule, recorded verbatim in Part 3's
  design: find a known spell with the same base id; replace it when the incoming
  dimension is higher; refuse to consume when it is equal or lower.
- **Delegation** — resolves the base template's script via
  `ItemHandler.GetTemplate(id % Offset)?.Script` and forwards every `IItemScript`
  member to it after doing its own work. `item.ScriptParams` already carries the base
  template's params, because `LoadFromTemplate` copies them (`Goose/Item.cs:176`).

## Modifiers

Eight registrations, all `Chance = 0`:

- `Legendary`, `Stunted` — titles, backed by `DimensionRarity.csx`, which only sets
  `StatMultiplier`.
- `of Vita Regen`, `of Mana Regen`, `of Criticality`, `of Spell Damage`,
  `of Reduction`, `of Speed` — surnames, backed by `DimensionSurname.csx`, which takes
  the suffix index in `ScriptParams` and applies the corresponding terms from
  `dimensionDefault`, scaled by the item's dimension and tier:

| Suffix | Bonus |
|---|---|
| of Vita Regen | `HPPercentRegen += 0.015·dim·tier`, `HPStaticRegen += 1500·dim·tier` |
| of Mana Regen | `MPPercentRegen += 0.015·dim·tier`, `MPStaticRegen += 1500·dim·tier` |
| of Criticality | `SpellCrit += 0.04·dim·tier` |
| of Spell Damage | `SpellDamage += 0.04·dim·tier` |
| of Reduction | `DamageReduction += 0.04·dim·tier` |
| of Speed | `Haste += 0.04·dim·tier` |

The existing generic `ItemModifierScript.csx` cannot express dimension scaling — its
operations are fixed JSON values — hence a dedicated script.

## Drop tables

A new pass after item cloning walks each dimension NPC template and rebuilds `Drops`,
pointing each entry at the cloned item template where one exists. Gold and consumables
keep their base template.

Entries must be **new `NPCDropInfo` instances**: `NPCTemplate`'s copy constructor
(`Goose/NPCTemplate.cs:251`) copies the list but shares its elements, so mutating in
place would corrupt dimension 0.

Of the 1,309 drop rows, 836 armor / 115 weapon / 73 tome entries become
dimension-aware. The 59 distinct tomes that drop are the first obtainable source of
the dimension spells Part 3 generated.

A tome whose spell has no dimension clone — `PreflightSpellIds` can skip ids — keeps
its base `LearnSpellID` rather than pointing at a null.

`OnLoaded` ordering: clone item templates → clone NPC templates (Part 1) → repoint
drops.

## Map-script delegation

A Part 1 fix, shipped here because it is the same pattern. `Dimensions.csx:220` does
`clone.Script = mapScript` — a replacement, so the dimension clones of `ArenaMap.csx`
and `ZombieTownMap.csx` are inert.

1. `DimensionMap.csx` reads its dimension from `map.ID / Offset` instead of
   `ScriptParams`.
2. The clone loop passes the base map's `ScriptParams` through verbatim.
3. `DimensionMap` forwards every `IMapScript` member to
   `MapHandler.GetMap(map.ID % Offset)?.Script`.

## Testing

xUnit, following `DimensionsScriptTests`:

- Clone counts and id arithmetic; consumables, money and NoUse templates not cloned
- Name prefixes, description prefix, recolour clamps, value, cleared bind/LORE flags
- `dimensionDefault` flat scaling against hand-computed abyss values; tier
  classification at each boundary
- Seeded-`Random` roll distribution: 45% suffix in even bands, 2%/2% rarity
- Native roll suppression on dimension items; dimension-0 rolls unchanged
- Drop repointing, with dimension-0 tables asserted untouched
- Tome upgrade in all three cases: unknown spell, lower dimension, equal or higher
- `CanPickup` above and below the player's unlock
- Delegation forwarding, for both a scripted item and a scripted map

## Known limitations

Accepted, listed so they are not mistaken for bugs.

1. **Dimension loot sells to gold vendors for `3^dim` times base.** `Value` carries the
   future spirit price, and the vendor overrides that redirect it land in Part 5. A
   dim-6 drop pays 729× base gold until then.
2. **Vendor stock is untouched.** `npc_vendor_items` still points at base templates, so
   a dimension vendor sells dimension-0 gear. Part 5.
3. **No equip gating.** Gear above your dimension cannot be looted from the ground, but
   can be traded and then worn.
4. **Tomes render as consumables** rather than scrolls, the cost of the upgrade rule.
5. **`Enabled = false` orphans dimension items.** Inventory rows persist template ids
   that no longer resolve. Same class as Part 2's limitation 5 and Part 3's limitation
   2; the exact failure mode is to be confirmed during planning.

## Out of scope

Part 5, the spirit economy: vendor purchase/sell overrides, dimension vendor stock,
`/resetitem`, `/rebirth`, SP repurposed as currency, single-dimension crafting.
