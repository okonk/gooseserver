# Enchanting — Design

## Purpose

A long-term item progression sink that sits beside the existing title/surname modifiers
without competing with them. Players add elemental enchant levels to weapons and armor at an
enchanting table, consuming elemental essences. High levels risk destroying the item unless a
protection item is used.

Enchanting is a standalone feature. The upcoming arena (separate design) is one *source* of
essences and protection items via its shop; other sources (drops, quests) can be added later
without touching enchanting code.

## Rules

- Every enchantable item has up to **10 enchant levels**, freely mixed across elements
  (e.g. 6 Earth + 4 Fire). The item's level is the sum of its element counts.
- Each attempt consumes **1 essence** of the chosen element. No gold cost.
- Levels +1..+5 cannot fail. Levels +6..+10 have a failure chance, default
  `30/40/50/60/70%` for the target level.
- **Failure without protection:** the item is destroyed.
- **Failure with protection:** the item keeps its level, its **max level drops by 1**, and the
  protection item is consumed. Protection is consumed only when it saves the item.
- Enchants are permanent: no removal, no respec. Levels never go down, so there is no
  "fail on purpose to switch element" path.
- Enchantable: `UseType` Weapon or Armor, in inventory (not equipped). **Excluded:** mounts
  (`ItemSlots.Mount`) and backpacks.

| Element | Stat | Per level (default) |
|---|---|---|
| Earth | Spell damage | 1% |
| Water | Spell crit | 1% |
| Fire | Damage reduction | 0.5% (DR is curve-mapped, never reaches cap) |
| Air | MP % regen | 0.04% |
| Spirit | HP % regen | 0.04% |

Per-item values are intentionally generous; tune during balancing.

## Data model

New `ItemProperty` keys (persisted through the existing `ItemProperties` dictionary):

- `EnchantEarth`, `EnchantWater`, `EnchantFire`, `EnchantAir`, `EnchantSpirit` — level count
  per element.
- `EnchantMaxLoss` — number of max levels lost to protected failures.
  `max = EnchantMaxLevel - EnchantMaxLoss`.

Reading must cope with values that round-trip through JSON (`JsonElement`, not `int`); the
code handles this, covered by a save/load round-trip test.

## Stats

Enchant stats are **derived, not stored**. `Item.RefreshStats(GooseSettings)` (`Goose/Item.cs:275`,
now taking settings) adds them *after* the title multiplier:

```
total = (template + BaseStats) * StatMultiplier
total += EnchantStats(counts × per-level settings)
```

- Never multiplied by a title's `StatMultiplier`.
- Changing a per-level setting rebalances every existing enchanted item on next refresh.
- Because nothing is written to `BaseStats`, clearing `BaseStats` never loses enchants.

## Name

The item name ends in ` +N` (N = total level), always last:
"Ancient Longsword of the Bear +7". Rebuilt whenever the level changes and after every name
rewrite:

- `ItemHandler.SetModifier` — strip ` +N`, apply title/surname, re-append.
- `ItemHandler.ResetModifiers` — resets title/surname/stats only; **keeps enchant
  properties** and re-appends ` +N`. Its only caller is Dimensions `/resetitem` (suffix
  reroll); removing enchants there would be a paid respec.
- `CustomItem.BuildCustomItem` — copies the **whole `ItemProperties` dictionary** from the
  stats item (replacing the per-key Title/Surname copies, future-proofing new properties) and
  appends ` +N` to the custom name.

## Display

- Enchant stats fold into the item's normal stat lines (tooltip already renders two-decimal
  percents via basis points).
- Stats window regen (`Goose/Window.cs:251`) changes from whole-percent to two decimals.
- Max level and per-element breakdown are shown only in the enchant window.

## Enchanting table window

**Opening.** An NPC whose spawn row carries `{"enchantTable": true}`, read in
`PlayerRightClickEvent` (same pattern as `communityChest`). Opens `EnchantWindow` (new
`WindowTypes`/`WindowFrames` value mirrored in the client). Re-opening replaces the existing
window. Out of range, the table refuses slot changes and attempts (community chest
precedent) rather than auto-closing.

**Layout.**

```
┌ Enchanting Table ───────────────┐
│ [Item]  [Essence]  [Protection] │
│ Longsword of the Bear +7 (max 9)│
│ Earth 4: +4% spell damage       │
│ Fire 3:  +1.5% damage reduction │
│ Next: Earth +1% spell damage    │
│ Failure chance: 30%             │
│ Protected: yes                  │
│            [ Enchant ]          │
└─────────────────────────────────┘
```

**Slots reference inventory slots** (Custom window precedent, `Goose/CustomWindow.cs`). Items
never leave the inventory, so disconnects cannot strand them.

### Protocol

| Packet | Direction | Format |
|---|---|---|
| `EWS` | C→S | `EWS<itemSlot>,<essenceSlot>,<protectionSlot>` (0 = empty) |
| `EWP` | S→C | `EWP<windowId>,<itemSlot>,<essenceSlot>,<protectionSlot>,<canEnchant>` then `\|<line>` per display line. Echoes the slots the server accepted (rejected = 0); lines are server-rendered so the client holds no stat tables |
| `EWC` | C→S | `EWC<itemSlot>,<essenceSlot>,<protectionSlot>` — confirm |

Exact field order is fixed in the Part 1 plan and mirrored by Part 2.

`EWS` validates placement, stores slot ids + item references, replies `EWP` (a rejected slot is
echoed as 0 with the reason as a line, e.g. "This item can't be enchanted further"). Protection on a
safe level (+1..+5) is ignored and previewed as "not needed".

`EWC` re-resolves each slot and refuses ("Items changed, place them again.") if any slot id
or item reference differs from the last confirmed `EWS` — the `CustomWindow.Create` guard
(`CustomWindow.cs:118-125`). Nothing is consumed on refusal.

### Identifying items

Settings, following the `CustomTicketId` precedent; no schema change:

- `EnchantEssenceTemplates` — template id → element.
- `EnchantProtectionTemplateId`.

## Resolving an attempt

On `EWC`, after validation:

1. Target level = current + 1; refuse if above max.
2. Consume one essence.
3. Roll `EnchantFailPercent[target - 1]`.
4. **Success:** increment element count, rebuild name, refresh stats, resend inventory slot,
   notice, fresh `EWP`. Protection untouched.
5. **Protected failure:** consume protection, `EnchantMaxLoss += 1`, level unchanged. Notice:
   "The enchantment fizzled. Your protection held, but the item can now only reach +X".
6. **Unprotected failure:** destroy the item via the existing inventory removal path,
   "...shattered" notice, clear the window's item slot. Window stays open.

Runs inside `SuppressQuestIconRefresh`, as `CustomWindow.Create` does.

## Logging

New `Log.Types.Enchant` on every attempt: template, item id, element, level before/after, max,
protected, outcome (success / protected-fail / destroyed). Destroyed items log the full item
so a GM can rebuild it from the log viewer.

## Settings

- `EnchantMaxLevel` — 10.
- `EnchantFailPercent` — `[0,0,0,0,0,30,40,50,60,70]`.
- `EnchantPerLevel` — per-element stat values (table above).
- `EnchantEssenceTemplates`, `EnchantProtectionTemplateId`.

Startup validation (fatal on bad config): `EnchantFailPercent.Length == EnchantMaxLevel`;
each essence template id exists and maps to exactly one element; protection template exists.

## Game data

Five essence templates and one protection template, authored via the game-data pipeline.
GMs spawn them for testing until the arena exists.

## Testing

Server (`Goose.Tests`):

- Enchant stats apply after title multiplier; changing a per-level setting changes an
  existing item's totals.
- ` +N` lands last after `SetModifier`, `ResetModifiers`, custom creation.
- `ResetModifiers` keeps enchant properties.
- `BuildCustomItem` copies all properties.
- Property save/load round-trip.
- Attempts with seeded RNG: success, protected failure (max drops, protection consumed),
  unprotected failure (item gone), +1..+5 never fail, full item refused, mount/backpack
  refused.
- Validation: mismatched slot / moved item refused, nothing consumed.
- Settings validation failures.
- Stats window shows two-decimal regen.

Client (`Goose2ClientGodot`): `EWP` parse tests (`BuffBarPacketTests` pattern).

## Parts

- **Part 1 — Server:** properties, derived stats, name handling, reset/custom/SetModifier
  changes, settings + validation, regen display, `EnchantWindow`, packets, NPC property,
  attempt resolution, logging, tests.
- **Part 2 — Client (`Goose2ClientGodot`):** enchant window widget, slot drag, `EWP` parsing,
  `EWS`/`EWC` senders.

## Deferred (consciously)

- Essence/protection sources and prices — arena design.
- Clearing a window slot immediately when its item leaves the inventory (confirm-time
  validation covers correctness).
- Any enchant removal, including GM tooling.
- Enchanting mounts and backpacks.
- Balance tuning beyond defaults.
