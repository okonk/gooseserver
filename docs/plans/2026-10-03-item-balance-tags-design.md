# Item Balance Tags — Design

Date: 2026-10-03

## Problem

Balancing passes so far scored every equippable item from its stats alone and forced each
harder-to-get item to beat the easier ones in its slot by 10%. Without knowing what an item is
*for*, the rules kept misfiring:

- HP/MP pairs from one boss (Snake Helm/Tiara, Ancient Garb/Robe) were forced to beat each other.
- All-class items were pushed into caster or melee shapes depending on which class failed.
- Deliberately strong items (Doom Robe) set the bar for everything after them.
- The solver inflated AC-heavy caster pieces (Devastators Robes ×3) to reach MP targets.

The fix is to record each item's intended purpose and relative power, then balance from that.

## Goals

- Tag every equippable item (weapons, shields, armour, accessories) with its intended audience,
  stat profile, power and alternative group.
- Edit the tags in the Data Editor, next to the item stats and a live per-class score.
- Feed the tags to a balancer that generates and checks stats.

Non-goals: changing the game server, adding columns to the Items sheet, editing the live DB.

## Decisions

| Decision | Choice |
|---|---|
| Storage | New **Item Balance** tab in the game data spreadsheet, one row per item |
| Importer | Tab is **editor-only**: registered for SchemaGen, never read by `CsvToSqlConverter` |
| Editing | New **Balance** view on the Items sheet in the Data Editor, alongside Records and Sets |
| Scoring | One implementation, `tools/DataEditor/src/balance.js`, used by the editor and a Node CLI |
| Prefill | Script guesses every tag from current stats and sources; the user reviews instead of filling blanks |
| Step / source | Stored explicitly (prefilled), not derived live in the editor |

## Tag sheet: `Item Balance`

| Column | Kind | Values | Meaning |
|---|---|---|---|
| `item_template_id` | Id, pk, ref Items | | The item |
| `audience` | Bitmask from Classes | 0 = all, 12 = melee, 48 = caster, any mix | Who the item is **meant** for (may be narrower than `class_restrictions`) |
| `profile` | Enum | `Balanced`, `HP`, `MP`, `Tank`, `Dodge`, `Damage` | Primary stat shape |
| `profile2` | Enum, optional | same | Secondary shape (e.g. `HP` + `Tank`) |
| `power` | Enum | `Normal`, `Special`, `Weak`, `Exempt` | Normal follows the curve; Special sits above it; Weak is starter/filler; Exempt is never touched |
| `power_pct` | Int, default 0 | e.g. `+20` | Fine adjustment on top of `power` (Fighting Katana +20) |
| `group` | Text, optional | e.g. `snake-20m` | Items in a group are alternatives |
| `step` | Enum | `Levelling`, `Punchy`, `HayFray`, `Sewers`, `Nagan`, `Savage`, `Nibbles`, `XP20M`, `XP100M`, `XP200M`, `XP400M` | Content step |
| `source` | Enum | `Vendor`, `Common`, `Uncommon`, `Rare`, `Crafted`, `RareBoss`, `HardCraft`, `Prestige`, `Special` | Source tier (multiplier) |
| `lock` | Bool | | Keep current stats even if the generator disagrees |
| `note` | Text | | Why |

Profiles:

- **Balanced**: HP ≈ MP, STR ≈ STA ≈ DEX ≈ INT. The default for all-class items.
- **HP / MP**: leans to one pool (the HP or MP variant of a pair).
- **Tank**: AC-heavy (Warrior tank line, Gold).
- **Dodge**: DEX-leaning (Rogue, Devastators).
- **Damage**: STR / weapon damage for melee audiences, spell damage or crit for casters.

## Scoring rules (`balance.js`)

Points = HP-equivalents / 25. Items are scored from the viewpoint of the harder item being
compared (its level and step).

| | Warrior | Rogue | Magus | Priest |
|---|---|---|---|---|
| HP / MP split (STA = 25 HP, INT = 25 MP) | 90 / 10 | 55 / 45 | 25 / 75 | 35 / 65 |
| 1 AC = | 10 HP tank line, 5 HP damage line | 1 HP | 0.5 HP | 0.5 HP |
| 1 DEX = (20 DEX = 1% dodge) | 4 HP | 6 HP | 2 HP | 2 HP |
| 1 STR / weapon damage, below L50 | 50 HP | 50 HP | 20 HP | 35 HP |
| 1 STR / weapon damage, L50 up to Nibbles II | 15 HP | 15 HP | 0 | 0 |
| 1 STR / weapon damage, 20M+ | 0 | 0 | 0 | 0 |
| Melee damage / crit / haste effects | scaled like STR | same | same | same |
| Spell damage / crit effects | from L50 | from L50 | always | from L50 |

- % effects (1%): melee damage 150 HP, spell damage 125, crit 100, haste 150, damage reduction 200
  (half for casters). From 20M they scale with the class pool at 20M / 100M / 200M / 400M XP.
- Upgrade rule: an item that is harder to get (level, step and source all ≥) must score at least
  1.1× the best easier item in its slot, for its audience. Warriors pass on the tank **or** damage
  line.
- Groups: items in one group don't have to beat each other. For each class in the audience, the
  group's best item must be the upgrade, and every member must be the upgrade for at least one class.
- `Special` items are not used as the bar for later items. `Exempt` items are skipped entirely.

## Data Editor: Balance view

On the Items sheet, a third toggle (Records / Sets / **Balance**):

- **List**: one entry per slot (Helmet, Chest, … Ring, Weapon, Caster weapon).
- **Table** for the selected slot, sorted by step then level, one row per item:
  - name, level, classes, step, source;
  - HP / MP / AC / STR / STA / DEX / INT / weapon damage, effect name;
  - per-class score (W / R / M / P) and the upgrade ratio vs the best easier item, coloured
    (red < 1.0, amber < 1.1, green ≥ 1.1);
  - tag cells as inline controls (audience checkboxes, profile dropdowns, power, %, group, lock,
    note).
- Group members are drawn together with a bracket so pairs read as alternatives.
- Filters: audience class, step range, "only failing".
- Saving writes changed tag rows through the existing `saveBatch`; a missing row is appended.
- Stats stay read-only here; clicking a name opens the item in the Records view.

## Prefill

A one-off script writes one row per equippable item (run once to seed the tab; after that the tab
is the source of truth and is edited in the Balance view):

- `step`, `source`: the curated values from the balance workbook; otherwise derived from the XP gate,
  the boss that drops it (Minita Sewers bosses → `Sewers`), vendor or craft. Credit-shop items,
  items with no source and stat-less look-only copies are `Exempt`.
- `audience`: `class_restrictions`, narrowed to melee or caster when the item clearly leans one way.
- `profile` / `profile2`: from the stat shape (AC share, DEX share, HP vs MP, STR/weapon damage).
- `group`: same step + slot + `class_restrictions` + level + source.
- `power`: `Exempt` for items with no source; `Special` for Doom Robe; `Normal` otherwise.
- `note`, `lock`: carried from decisions already made (Thick Skin, Rusty Claw, Encased Claw,
  Beefs Immortality, Coral Sword, approved weapon damages).

The output is reviewed as a JSON/CSV diff before the tab is created and filled.

## Balancer (later)

`tools/Balance/balance.mjs` reads the item, effect and tag data (exported with
`gsheets.py read --json`), then:

1. `check`: reports failing upgrades per slot (same rules as the editor).
2. `generate`: produces stats from a slot budget (level, step) × source multiplier × power,
   split by profile and audience and rounded to 5 (HP, MP, AC, weapon damage). The budget curve
   (gear's share of a character's pool at each step) and the STA/INT mix are inputs, decided
   separately.
3. Writes a JSON plan in the existing apply format (from/to per field), reviewed before applying.

## Parts

1. **Schema + prefill.**
   - Editor-only registration in `CsvToSql.Core` (`SchemaRegistry.EditorOnlyTables`).
   - SchemaGen emits it with `editorOnly: true`; a test proves `CsvToSqlConverter` never reads it.
   - `balance.js` scorer with unit tests.
   - Prefill script; create and fill the tab after review.
2. **Balance view** in the Data Editor, with tests on the fake DOM / fake sheets harness.
3. **Balancer CLI**: `check` first, `generate` once the budget curve is decided.

## Open questions

- Budget curve and STA/INT mix (needed for part 3 only).
- Whether the Balance view should also allow editing stats inline, or stay tags-only (start
  tags-only).
