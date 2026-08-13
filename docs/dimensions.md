# Dimensions

A port of the abyssserver "dimension" feature to the goose server, implemented
almost entirely through scripts, with a small set of generic extension points
added to the server.

## What abyss has

A **dimension** is a parallel, scaled-up copy of the entire world. Dimension 0
is the normal game; dimensions 1–6 ("Abyss" realms) are procedurally amplified
clones of it. Nothing dimension-specific is hand-authored except the quest
chain that unlocks each realm.

### World

- Every map is cloned once per dimension at load time. Each clone has its own
  player/NPC/item lists, so players in different dimensions can never see each
  other — isolation is structural, there is no per-observer visibility filter.
- Map names get a `" (n)"` suffix; PVP is forced on in every dimension > 0;
  map min/max experience entry gates scale by `(dim*5)^2`.
- Warps, teleport spells, login, and binds all resolve to the map copy in the
  player's current dimension, so you stay in your dimension until you leave
  explicitly.

### Access

- `/dimension <n>` warps the player to the starting map of dimension n.
- A player's maximum dimension is derived from completed quests: finishing the
  Abyss quest for dimension n unlocks dimension n+1. Entering a map (or picking
  up an item) above your unlocked dimension is rejected.

### NPCs

Every spawn is duplicated per dimension. Dimension copies are level 50,
immune to root/stun (but slowable), recolored darker per dimension, and scaled:

| Property | Formula |
|---|---|
| HP | `(base + 100000·2^dim) · 4.7^dim` (×2 at dim ≥ 5 for low-HP mobs) |
| Damage | `base·4^dim + 100000·max(0, 4^dim − 3)` (×20 at dim ≥ 5 for weak mobs) |
| Attack speed | `max(base − 0.175·dim, 0.2)` |
| Move speed | `max(base − 0.15·dim, 0.15)`; attack range `+dim` |
| Experience | `(exp + level·100) · 3^min(4,dim)`, doubling again per dim past 4 |
| Respawn | `min(base · 0.85^dim, 3600/(1+dim))`, dim capped at 4 |

### Spells

Full per-dimension copies of every spell and spell effect, keyed
`(id, dimension)`. Dimension versions get:

- Name prefixes: Powerful / Super Powerful / Supreme / Omnipotent / Almighty /
  Godly; description prefix "Abyss (n)".
- Damage/heal formulas wrapped as `(formula) · 1.25^dim` (extra ×1.15 for
  single-target spells).
- AoE growth: target size `+dim`; small shapes morph into bigger ones
  (Cross/Plus → Area, LineFront → Plus/TriangleFront).
- Duration ×1.15^dim, HP/MP static costs ×3^dim, aether ×0.9^dim,
  taunt aggro ×3^dim.
- Buff stats scaled (HP/MP ×(dim+1)², most stats ×(1 + 0.5·dim)).

Players learn the copy for the dimension they are in; the spellbook stores the
dimension per learned spell.

### Items

Items carry a dimension. Drops inherit the killing NPC's dimension; vendor
stock inherits the vendor's dimension. Dimension equipment gets:

- **Deterministic prefix** (always applied): the same Powerful…Godly name
  prefix, "Abyss (n)" description, per-dimension recolor, value/SP value
  ×3^dim.
- **Rarity roll**: 2% "Legendary " (stats ×1.25), 2% "Stunted " (stats ×0.5).
- **Suffix modifier ("surname")**, 45% chance in six equal 7.5% bands:
  of Vita Regen / Mana Regen / Criticality / Spell Damage / Reduction / Speed.
  The suffix drives a targeted bonus stat block scaled by dimension and item
  tier.
- Bound/LORE/bind-on-pickup/equip flags are disabled (dimension gear is freely
  tradeable); consumables never scale (always dimension 0).
- `/resetitem <slot>` rerolls the suffix on a dimension item for `3^dim` SP,
  with the roll biased so a suffix is guaranteed.
- Crafting requires all ingredients from one dimension and produces output at
  that dimension.

### SP ("spirit") economy

- **Faucets**: `/rebirth` converts total experience into SP at 1 SP per 100M XP
  and resets the character to a level-1 commoner; selling SP-valued items to
  vendors pays `SPValue / 2` (dimension loot is ×3^dim); `/givesp` transfers.
- **Sinks**: `/resetitem` rerolls, SP-priced vendor stock, SP→gold and SP→XP
  conversion commands, cosmetic customization commands.
- The loop: dimensions give huge XP → rebirth converts XP to SP → SP rerolls
  gear and buys perks → better gear pushes higher dimensions.

### Database

`dimension` columns on `players`, `spellbook`, `items` (plus item bonus-type
and SP-value columns) and `quest_templates`.

---

## Goose implementation plan

Same feature, but dimension identity is encoded in **ID offsets**
(`id + 10000·dim` for maps, spells, spell effects, and item templates) instead
of composite keys and schema changes. Because inventories, spellbooks, and the
player's saved map all persist plain IDs, dimension state round-trips through
the existing database with no migrations.

Everything lives in one script pair, with a handful of small generic extension
points added to the server.

### Server changes (small, generic)

1. **`Player.Properties`** — a persisted `Dictionary<string, string>` (JSON
   column) for arbitrary per-player script data. Dimensions uses it for unlock
   progress (max dimension).
2. **`ItemProperty.Dimension`** — one new enum member, used to tag item
   instances (pickup gating, reset command). Serialization already handles it.
3. **`SpellHandler.AddSpell` / `AddSpellEffect`** — allow scripts to register
   generated spells/effects.
4. **`ItemHandler.AddTemplate`** — allow scripts to register generated item
   templates. Optionally `AddTitle`/`AddSurname` for script-registered item
   modifiers (alternative: plain `item_modifiers` DB rows).
5. **Vendor overrides** — reassignable static delegates (same pattern as the
   `Packets.cs` packet builders), consulted at the top of the two vendor
   events; return true = handled, false = normal gold flow:
   - `VendorPurchaseInventoryEvent.PurchaseOverride(npc, player, slot, world)`
   - `VendorSellInventoryEvent.SellOverride(npc, player, slot, world)`

   Static delegates (not NPC-script virtuals) so the handler covers *every*
   vendor, which the dimension-0 sell block requires.

Settings-only change: `BaseSPPercentRegen` / `BaseSPStaticRegen` set to 0 in
`GooseSettings.json` (SP becomes a currency, see below).

### Scripts

- **`Scripts/Global/Dimensions.csx`** — all configuration at the top
  (`Enabled` toggle, number of dimensions, ID offset, scaling formulas, spirit
  prices). Disable the feature by setting `Enabled = false`.
- **`Scripts/Map/DimensionMap.csx`** — attached to every cloned map by the
  global script; applies NPC scaling on (re)spawn and entry gating.

#### World cloning (script, at `OnLoaded`)

Global scripts load after maps/NPCs, and `MapHandler.Maps`, `Map.tiles`, and
`WarpTile.WarpMap` are all public, so the script can:

1. For each base map × dimension: build a clone (`ID = base + 10000·dim`,
   name suffix, PVP on), call `map.LoadData(world)`, add to
   `world.MapHandler.Maps`, assign `DimensionMap.csx` as its map script.
2. Second pass: rewrite every clone's warp tiles to point at the
   same-dimension clone, keeping players inside their dimension.
3. Re-spawn the base map's NPC spawns onto each clone with
   `shouldRespawn: true` (respawning is self-sustaining on the NPC), pointing
   drop tables at dimension item templates.

#### Access (script)

- `/dimension <n>` registered via `world.EventHandler.RegisterEvent`; checks
  unlock progress in `Player.Properties`, warps to the clone of the starting
  map. Unlocks granted by the Abyss quest chain.
- Relog restores dimension for free: the player's saved `map_id` is already a
  dimension map.
- `DimensionMap.OnPlayerEntered` enforces unlock/entry gates.

#### NPC scaling (script)

`DimensionMap.OnNPCSpawnEvent` applies the abyss formulas to the public
`BaseStats`/`MaxStats`/`WeaponDamage`/`Experience` — on every spawn including
respawns.

#### Spells (script)

Pre-generate per-dimension `Spell`/`SpellEffect` copies at `id + 10000·dim`
via `SpellHandler.AddSpell/AddSpellEffect`, mirroring the abyss scaling
(prefixes, formula wrapping, `TargetSize + dim` for AoE growth, duration,
costs, buff stats). Clone all effects first, then rewire cross-references
(on-melee-hit spells, buff stacking lists) to same-dimension clones, then
clone spells. The spellbook persists spell IDs as JSON, so learned dimension
spells survive relogs unchanged. Spell-teaching items in dimension d carry
`LearnSpellID + 10000·d`, so "learn spells at your dimension" falls out
naturally.

#### Items (script)

Clone each `ItemTemplate` to `base + 10000·dim`: name prefix, recolor,
deterministic dimension stat scaling, `SpellEffectID`/`LearnSpellID` pointed
at the offset spell clones, `Value` set to the **spirit price** (×3^dim).
`Item` serializes `TemplateID`, so dimension items persist untouched.

- **Titles**: the always-on Powerful…Godly prefix is baked into the cloned
  template name (deterministic, like abyss). Goose's existing random title
  system still runs on top for rare extra titles.
- **Surnames**: the six abyss suffixes become real goose surname
  `ItemModifier`s whose `IItemModifierScript`s apply the dimension-scaled
  bonus. The dimension script rolls them itself at drop creation with abyss
  odds (45%, six equal bands) rather than the global 0.5% gate, storing
  `ItemProperties[SurnameId]` as usual.
- **`/resetitem <slot>`**: script-registered command; requires a dimension
  item, charges `3^dim` spirit, rerolls the suffix (guaranteed hit), rebuilds
  stats from the template + surname script.
- Rarity (Legendary/Stunted) rolled by the script at item creation via
  `StatMultiplier`.

#### Spirit economy (script + settings)

SP is repurposed as the spirit currency, displayed for free in the client's
SP bar:

- Wallet = `BaseStats.SP`, which already persists as `players.player_sp`.
  Earning/spending adjusts `BaseStats.SP` and tops `CurrentSP` up to `MaxSP`
  (current == max always; regen zeroed via settings).
- **Faucet**: the rebirth quest grants spirit (goose has a suitable quest to
  build on); selling dimension loot to dimension vendors pays `Value / 2`.
- **Vendors**: the two vendor override delegates route dimension-template
  transactions through spirit instead of gold, and block selling dimension
  items at dimension-0 vendors entirely. Normal items fall through to the
  usual gold flow.

Data prerequisites to verify: no illutia spell rows have nonzero SP costs
(casting deducts SP unconditionally at `Player.cs:1917`), and no class grants
per-level SP (`classes.sql player_sp`) — either would leak the wallet.

### Constraints / watch-outs

- All base map/spell/effect/item-template IDs must stay below the offset
  (10000) — or pick a larger offset.
- Total NPC count multiplies by (dimensions + 1); confirm no login-ID or
  capacity ceiling.
- Goose's own 0.5% surname roll can stack a second suffix on dimension drops;
  either accept it or skip the dimension roll when `SurnameId` is set.
- The item and vendor slot packets carry a trailing currency name
  (`Packets.cs:470,527`), so tooltips read "Value: 4,500 spirit" rather than
  guessing gold. Clients that stop parsing at `GraphicA` ignore the field.
