# Item Tooltip Extra Stats Design

## Problem

A title or surname can add spell damage, spell critical, melee damage, melee critical,
haste, damage reduction and regeneration. None of them appear on the item tooltip.

The tooltip's stat block is built client-side from positionally parsed `SIS`/`SBS` fields
(`Goose2ClientGodot/Scripts/UI/ItemTooltipText.cs:34`), and that packet carries only
damage, AC, HP, MP, SP, the four primaries, the five resists, requirements and the effect
string (`Goose/Packets.cs:439-484`). `AttributeSet` has thirteen further fields
(`Goose/AttributeSet.cs:11-36`); a title or surname can write twelve of them
(`ItemModifierScript.csx:10-30`, `DimensionSurname.csx:29-48`).

AC, HP and Strength suffixes show up today only by accident: they land in `TotalStats`,
which is what the packet already sends. Everything else has no channel at all. The abyss
`of Vita Regen` and `of Mana Regen` suffixes are a live example - their names advertise a
regeneration number the player cannot read anywhere.

Extending the spell effect description was considered and rejected. `Item.SpellEffect` is
a template property (`Goose/Item.cs:120`) shared by every instance, so a per-item line
cannot be attached to it: `P.ItemSlot` would have to assemble the string per item anyway,
which is the same change as appending a field. On top of that the client renders that
field as `Effect: <first token>` plus one indented line per `;` token
(`ItemTooltipText.cs:139-152`), so a stat-only item would need an invented effect name and
an item with a real proc would show its rolled suffix stats inside the proc's description.

## Protocol

Add one field, `ExtraStats`, after the currency name - the current last field - in both
`P.ItemSlot` (`Goose/Packets.cs:428`) and `P.VendorItemSlot` (`Goose/Packets.cs:488`).
Those two functions are the only places that emit item stat fields; every Illutia slot
packet (inventory, equipped, bank, combine bag) routes through `ItemSlot`, and the vendor
path is the sole duplicate (`Goose/Packets.cs:553-587`).

Comma-separated integers in this fixed order, percentages as basis points:

| # | Value | Source | Example |
| --- | --- | --- | --- |
| 1 | haste | `Haste` | `400` = 4% |
| 2 | spell damage | `SpellDamage` | |
| 3 | spell critical | `SpellCrit` | |
| 4 | melee damage | `MeleeDamage` | |
| 5 | melee critical | `MeleeCrit` | |
| 6 | damage reduction | `DamageReduction` | |
| 7 | HP regen percent | `HPPercentRegen` | `1500` = 15% |
| 8 | HP regen flat | `HPStaticRegen` | `1500` |
| 9 | MP regen percent | `MPPercentRegen` | |
| 10 | MP regen flat | `MPStaticRegen` | |
| 11 | SP regen percent | `SPPercentRegen` | |
| 12 | SP regen flat | `SPStaticRegen` | |

Percentages convert with `Utils.ExactProduct(value, 10000)` (`Goose/Utils.cs:50`), the
house helper for this conversion already used in combat (`Goose/SpellEffect.cs:630`),
cast to `int`. A stat below 0.5 basis points converts to zero and its line is omitted.

Trailing zeros are trimmed, so an ordinary item sends an empty field and the common case
costs one separator. An item with only a haste suffix sends `400`; one with only spell
damage sends `0,800`. Missing entries are zeros. The list is append-only: a later server
may add entries beyond the twelve and an old client ignores them, which appending another
top-level field would not allow.

Integers, not pre-formatted text: no culture or decimal-separator questions on the wire,
and the label text stays where every other tooltip stat's text already lives.

## Server

`ItemSlot` reads `item.TotalStats` - the item's full contribution, the same source as the
AC and HP lines, and the reason a stat is correct no matter whether a title, a surname, an
item script or a future template column produced it. `VendorItemSlot` keeps reading
`item.BaseStats`, since vendor stock carries no rolled modifiers and its payload is
normally empty.

`docs/dimensions.md:253` records the trailing-field rule ("clients that stop parsing at
`GraphicA` ignore the field"); extend it to cover both trailing fields so the next reader
finds the rule once.

## Client

Two parse sites, both already using the pattern this needs: `InventorySlotPacket`, whose
`Parse` is inherited by `BankSlotPacket` and `CombineBagSlotPacket` (both override only
`Prefix`) and therefore covers SIS, SBS and SCS; and `VendorSlotPacket`, which duplicates
the layout for SVS. Both read `CurrencyName` with
`LengthRemaining() > 0 ? p.GetString() : null` (`InventorySlotPacket.cs:107`,
`VendorSlotPacket.cs:59`); `ExtraStats` is read the same way immediately after and split
on `,` into twelve integers on `ItemStats`. An absent field means all zeros, so an old
server renders exactly today's tooltip. Every consumer already funnels through
`ItemStats.FromPacket(InventorySlotPacket)`.

One new block in `ItemTooltipText.Build`, after the resist lines and before the
class/level requirements, so the coloured stat block stays contiguous and requirements,
effect and value keep their positions:

```
+4% Spell Damage
+4% Spell Critical Chance
+4% Melee Damage
+4% Melee Critical Chance
+4% Melee Attack Speed
+4% Damage Reduction
+15% Health Regeneration
+1,500 Health Regeneration
```

Lines are emitted in wire order, only when non-zero, in the existing
`ItemTooltipColor.Stat` teal (`ItemTooltipControl.cs:120`) - these are stats, not effects,
so they do not borrow the effect tan.

Labels follow `SpellEffect.GetBuffDescription` (`Goose/SpellEffect.cs:441-452`) so an
item's own effect lines and its rolled suffix lines name the same stat the same way:
Spell Damage, Spell Critical Chance, Melee Damage, Melee Critical Chance, Melee Attack
Speed, Damage Reduction, and Health/Mana/Spirit Regeneration. Spirit Regeneration is new
text - no modifier reaches SP regen today, but the symmetry is free.

Formatting: percentages from basis points at up to two decimals with trailing zeros
trimmed (`400` -> `4%`, `450` -> `4.5%`); flat regeneration with the existing `N0`
thousands separator; positive values take a `+` and negative values a `-`, matching
`FormatNumber`'s handling of the AC and HP lines. Percentage and flat regeneration are
separate lines, so an abyss regen suffix shows both numbers - the character window instead
fuses them (`Goose/Window.cs:238`), but each number is separately meaningful on an item.

## Testing

Existing assertions that pin the trailing position need re-pointing: `fields[^2]` and
`fields[^1]` (`Goose.Tests/PacketCurrencyTests.cs:112-113`), `fields[^9]`
(`Goose.Tests/DoubleBehaviorTests.cs:107`), and the `LastField` helper
(`PacketCurrencyTests.cs:36`) used by five further assertions at lines 46, 58, 71, 85 and
97, all of which would silently start reading the new empty field. Add one assertion in
the spirit of `ItemSlot_KeepsGraphicAInItsExistingField`: the currency keeps its position
and `ExtraStats` is appended after it.

New server coverage: per emit site, no extra stats gives an empty payload; a
modifier-applied item gives basis points in the documented order with trailing zeros
trimmed; `StatMultiplier` scales the reported percentages (`AttributeSet.cs:196-201`), so
a x1.1 item reports `440` rather than `400`, matching what combat applies.

New client coverage: `ItemStatsTests` for SIS and SVS with the field, without it, a
partial payload, and extra entries beyond the twelve ignored; `ItemTooltipTextTests` for
exact line text, order and colour, omitted zeros, negative values, `450` -> `4.5%`, two
regeneration lines, and a null-field case asserting the output matches today's lines
exactly.

One end-to-end integration test in `Goose.IntegrationTests` beside
`DimensionModifierTests`/`DimensionItemScriptTests`: roll a real dimension suffix
(`of Speed`, `of Vita Regen`) and assert the payload through `P.ItemSlot`. That is the
original bug report, so it belongs at that level rather than only as a hand-set
`BaseStats` unit test.

## Rollout

Either order is safe in both directions - an old client ignores the trailing text, a new
client shows nothing extra against an old server - so the server ships first, when nothing
can regress, and the client build follows. There is no updater or version gate in the
client repo, so players on an old build see the lines only after they re-download. That is
cosmetic, and no enforcement is added.

## Non-goals

- Ground items: `DOB`/`MapItemTooltipControl` carry no stat block at all.
- NPC and pet equipment: graphics only, never tooltipped.
- The character window: it already shows the player's totals (`Goose/Window.cs:243-253`).
- Move Speed: no modifier sets it, and its item semantics are unclear - the buff text
  reads "Set Move Speed to X", not an increase.
- The Aspereta server type: `Aspereta.csx:440-515` reassigns `P.ItemSlot`, `P.BankSlot`,
  `P.CombineSlot` and `P.VendorSlot` with a different protocol whose client has no stat
  block, no effect string and no free-text channel. This change is Illutia-only by
  construction.

## Accepted consequences

- An item carrying both a rolled suffix and a buff effect on the same stat shows two lines
  naming that stat: the stat block is the item's own stats, the effect block is what its
  proc grants. Not a gameplay double-count, just two labels in one tooltip.
- The twelve-value order exists on both sides. The exact-payload server assertion, the
  exact-string client test and one wire-format comment at the emit site are what stop the
  two orders drifting; a layout comment is the kind AGENTS.md allows.
- Custom items inherit `statsItem.TotalStats` (`Goose/CustomItem.cs:71`) and rerolls
  rebuild `BaseStats` through `RefreshStats` (`Goose/ItemHandler.cs:299-308`), so the
  payload always reflects current state and there is no stale text to clear.

## Repositories

The protocol owner is this repo; the design lives here. Client tasks land in
`~/code/Goose2ClientGodot`, which needs its own branch once the server side is agreed.
