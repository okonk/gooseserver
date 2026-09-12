# Leveling Balance Analysis Implementation Plan

**Goal:** Produce a reproducible, self-contained HTML balance report for the level 1–50 leveling experience from `Goose/bin/Debug/AsperetaGoose.db`.

**Architecture:** A read-only Python generator will derive effective acquisition levels, filter XP-gated and excluded-vendor-only content, calculate progression metrics, and render embedded SVG charts and sortable/filterable HTML tables. The report will preserve raw metrics alongside clearly labeled authoring proxies rather than hiding unlike stats in one unexplained score.

**Tech Stack:** Python 3 standard library (`sqlite3`, `statistics`, `json`, `html`), HTML/CSS/JavaScript, inline SVG

---

## APIs verified

- Equipment kinds, slots, and types: `Goose/ItemTemplate.cs:12-56`.
- Class restriction bitmask is an allow-list and zero means unrestricted: `Goose/Class.cs:33-47`.
- Item stamina and intelligence add 25 HP/MP per point through player stat application: `Goose/Player.cs:1638-1642`; configured conversion values are `Goose/GooseSettings.json:91-92`.
- Melee damage and AC mechanics: `Goose/Player.cs:1735-1765`; attack interval uses weapon delay and haste: `Goose/Events/PlayerAttackEvent.cs:42-46`.
- Dexterity supplies dodge, capped at 50%: `Goose/Player.cs:1957-1971`.
- NPC effective stats are template stats plus class-level stats: `Goose/NPC.cs:612-663`; NPC attack mechanics are `Goose/NPC.cs:1414-1430`.
- Quest reward and requirement enum values: `Goose/Quests/QuestReward.cs:8-32`, `Goose/Quests/QuestRequirement.cs:8-18`.
- Quest XP is passed through normal experience gain: `Goose/Quests/QuestWindow.cs:397-400`; the live experience modifier is 2 in `Goose/GooseSettings.json:93-95`.
- Spell/effect enum meanings: `Goose/Spell.cs:11-16`, `Goose/SpellEffect.cs:20-108`.
- Formula damage applies spell damage/crit only when `spell_damage_effects` is set: `Goose/SpellEffect.cs:618-638`.
- Random item titles/surnames are rolled for drops, purchases, crafting, and quest rewards: `Goose/NPC.cs:1486`, `Goose/Events/VendorPurchaseInventoryEvent.cs:97`, `Goose/Inventory.cs:1167`, `Goose/Quests/QuestWindow.cs:363`.

### Task 1: Read-only extraction and scope filters

**Files:**
- Create: `reports/game_balance/generate_report.py`
- Read: `Goose/bin/Debug/AsperetaGoose.db`

**Steps:**
1. Open SQLite in URI read-only mode and assert required tables/columns exist.
2. Identify spawned sources, map XP gates, attached quests, item source paths, class spell grants, and scroll/quest spell grants.
3. Exclude items with `min_experience > 0` and items whose only sources are NPC 170 (`Phat Lewtz`) or credit dealers; retain an item when it has another eligible drop, vendor, quest, or recursively reachable crafting path.
4. Separate NPC level 50 transition content from XP-gated level-50 endgame maps rather than fitting both to one curve.
5. Add generator assertions proving excluded-vendor-only gear is absent, dual-source content is retained, all included drops/vendors are spawned, and the database remains unchanged.

### Task 2: Progression and gap models

**Files:**
- Modify: `reports/game_balance/generate_report.py`

**Steps:**
1. Compute effective item availability as the earliest eligible source level, bounded by the item requirement; recursively propagate ingredient availability into crafted outputs.
2. Compute effective quest level from authored minimum, kill/item requirements, and prerequisite chains.
3. Compute effective NPC HP/MP/AC/attributes from template plus class-level baselines, hit pressure, attacks per second, XP per effective HP, and same-level kill counts per level-up.
4. Preserve direct stat vectors and add transparent authoring proxies: effective HP contribution (`HP + 25×STA`), effective MP contribution (`MP + 25×INT`), weapon throughput (`10×(damage + STR) / delay / (1-haste)`), dodge percentage, and an explicitly labeled comparable-item budget.
5. Detect slot/class gaps from meaningful frontier upgrades, sparse five-level bands, missing early/late coverage, and long intervals between upgrades.
6. Group ranked spells into families, measure unlock spacing, cost as a percentage of class resource at unlock, cooldown, formula/buff growth, and source availability.
7. Analyze quest gold, raw/live XP, percent of an incremental level, requirements, item rewards, repeatability, and authored-vs-effective level mismatches.

### Task 3: Self-contained report

**Files:**
- Create: `reports/game_balance/leveling-balance-report.html`

**Steps:**
1. Render an executive summary with concrete findings and prioritized intervention candidates.
2. Add inline SVG charts for item availability, weapon/armor frontiers, NPC scaling, XP efficiency, quest rewards, and spell pacing.
3. Add compact searchable/filterable detail tables for included and excluded items, NPCs, quests, spells, and identified gaps.
4. Include exact slotting formulas, interpolation guidance, source-tier bands, and worked examples generated from actual neighboring items.
5. Embed all styles, scripts, and data so the report opens offline.

### Task 4: Verification

**Files:**
- Verify: `reports/game_balance/generate_report.py`
- Verify: `reports/game_balance/leveling-balance-report.html`

**Steps:**
1. Run `python3 reports/game_balance/generate_report.py` and require exit 0.
2. Run generator self-checks and independent SQLite count queries for included/excluded populations.
3. Parse the HTML with Python’s `html.parser`, confirm expected section IDs/charts/tables, and scan for non-finite values or unresolved placeholders.
4. Regenerate and compare checksums to prove deterministic output.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Database is never mutated | URI `mode=ro`, pre/post database checksum equality |
| XP-sold equipment does not enter leveling curves | Generator assertion over every included item |
| Phat Lewtz/credit-only items are excluded | Source-set assertion and named excluded audit table |
| Legitimately dual-sourced items remain included | Assertion for every retained item’s eligible source path |
| NPC effective stats include class-level baselines | Sample recomputation assertion against joined rows |
| Quest XP percentages use incremental thresholds and live ×2 modifier | Formula assertions on selected quests |
| HTML is offline and deterministic | no external URLs, parse check, equal checksums on two runs |

## Independently verified report anchors

- 659 total item templates; 576 pass the direct no-XP/level-50 scope, and 372 have a modeled eligible source chain.
- 309 no-XP equipment templates; 189 have a valid modeled source, 101 resolve before level 50, 88 first resolve at level 50, and 120 have no valid source.
- Excluded premium stock contains 30 unique item templates across Phat Lewtz and Credit Exchange.
- The 48 one-time quests award 774,000 raw XP and 649,500 gold in total.
- Level-50 NPCs split into 38 templates on non-XP-gated maps and 48 templates found only on XP-gated maps.
- Source database SHA-256: `1eac9e774f2ae3633868da1aa87a5f73df8a9a3c9e0cd65f0deef918c209f472`.
- Priority slot gaps: no reachable necklace before level 50, no sourced mount, ring first appears around level 23–25, pauldrons around level 33, and cloak has no meaningful upgrade between levels 15 and 50.
- Confirmed progression outliers include Searing Whip #214 and the unrestricted Poo Flinger armor, which outperform later class-specific alternatives.
- Confirmed spell anomalies include Elemental Strike 8 being strictly dominated by rank 9 and Backstab cooldowns progressing 18/23/27/23/18 seconds.
