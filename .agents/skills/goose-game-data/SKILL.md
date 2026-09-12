---
name: goose-game-data
description: Use when reading or editing the Goose game data spreadsheet through the gws CLI — creating or updating NPCs, items, quests, quest rewards, quest requirements, spawns, drops, vendor stock, or looking up game data ids.
---

# Goose game data (the Google Sheet behind the server)

The spreadsheet `1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU` ("Aspereta Goose Data
(Illutia)") is the source of truth for every piece of static game data: the running server
downloads it as XLSX and generates the SQL that populates `item_templates`,
`npc_templates`, `quests` and the rest.

Everything here goes through the `gws` CLI (Google Workspace CLI at `/usr/bin/gws`).

## Setup — do this before the first gws call

`gws` rewrites its OAuth token cache under `~/.config/gws` on **every** API call. That path
is outside the workspace, so under the workspace-write sandbox the call dies with:

```
Authentication failed: ... Failed to set permissions on token directory
'<home>/.config/gws': Read-only file system (os error 30)
```

Point `gws` at a writable copy inside the workspace instead:

```bash
cp -a ~/.config/gws <repo>/.gws && chmod -R u+rwX <repo>/.gws
export GOOGLE_WORKSPACE_CLI_CONFIG_DIR=$PWD/.gws
```

`<repo>/.gws` holds live OAuth credentials. It is already in `.gitignore` — keep it there,
never commit it or paste its contents anywhere.

`scripts/gsheets.py` does this for you: it defaults `GOOGLE_WORKSPACE_CLI_CONFIG_DIR` to
`<repo>/.gws` and prints the bootstrap command if the directory is missing. Confirm with:

```bash
python3 .agents/skills/goose-game-data/scripts/gsheets.py doctor
```

## Use the helper, not raw gws

`scripts/gsheets.py` wraps the Sheets API and enforces the invariants below (RAW writes, id
allocation, required columns, helper formulas, positional width). Reach for raw `gws` only
for something the helper does not cover.

| Command | What it does |
| --- | --- |
| `doctor` | check config, auth and spreadsheet access |
| `tabs` | list the 21 worksheets with sheet ids and grid sizes |
| `schema SHEET` | print every column: letter, SQL name, kind, required, pk, enum members |
| `read SHEET [--id N] [--where COL=VALUE] [--limit N] [--json]` | read rows |
| `nextid SHEET` | next free primary key |
| `create SHEET --set COL=VALUE ... [--dry-run]` | append a fully-formed row |
| `update SHEET --id N --set COL=VALUE ... [--dry-run]` | change values on one existing row |
| `batch FILE [--dry-run]` | apply a JSON list of creates and updates in one pass |

Columns can be named by SQL name (`item_name`), by header label (`name`, `type (Monster)`,
`weapon dmg`), or by letter (`C`). Values are coerced and validated against the generated
schema: an unknown enum member or a missing required column is refused before anything is
written.

```bash
S=.agents/skills/goose-game-data/scripts/gsheets.py
python3 $S schema Quests
python3 $S read "Quest Reqs" --where questid=5
python3 $S create Items --set usetype=Weapon --set name="Rusty Dagger" --set "graphic tile=332205" --dry-run
```

Always `--dry-run` first on a create or a non-trivial update, then drop the flag.

### Creating several rows at once

A feature like a quest touches four worksheets, and each separate command pays for its own
full-sheet read. `batch` does the whole thing in one process, reusing each worksheet read.
Write the operations file **inside the repository** — a file under `/tmp` is not visible to a
later command:

```json
[
  {"op": "create", "sheet": "Quests", "set": {"name": "Green Slime Cull", "min_level": "10"}},
  {"op": "create", "sheet": "Quest Reqs", "set": {"quest_id": "11", "requirement_type": "Kill",
                                                 "requirement_value": "40", "requirement_value2": "10"}}
]
```

Operations apply in list order, so a child row can reference a parent created earlier in the
same batch — but the parent's id must be one you can predict, because `create` only auto-allocates
an id for a sheet's primary key. Check `nextid` first and hard-code the references, then read the
result back to confirm they landed where you expected.

`--dry-run` does not advance any state, so every create on the same sheet reports the same target
row and id. That is the flag's known limitation, not a bug — use it to check the columns and
values, not the row numbers.

Quest and NPC text needs `\n` between lines (see the quest column reference), which in JSON means
`\\n`. Generate the file with a script that builds real newlines and replaces them, rather than
typing escapes by hand.

The Sheets API read quota is per minute. `gws` is retried up to three times with a backoff when it
returns 429, but a very large batch can still exceed it — prefer one `batch` over many `create`
commands.

## How the data reaches the server

- `CsvToSqlConverter.Convert` fetches
  `docs.google.com/spreadsheets/d/<id>/export?format=xlsx` and walks every worksheet in
  `SchemaRegistry`; a **missing worksheet makes the whole import throw**.
- `CsvToSqlBase.BuildInserts` reads each row **positionally** against its column descriptors and
  emits one INSERT per non-empty row. An empty cell is omitted from the INSERT entirely, so the
  column's declared SQL default applies.
- `TableDdl.Emit` emits `DROP TABLE IF EXISTS` + `CREATE TABLE` first, so an import **replaces**
  the table contents rather than merging.
- It runs at startup (`Goose/GameWorld.cs`) and on demand via the in-game GM command
  `/updatesql`. **The running server does not see a sheet edit until one of those happens.**

## Worksheet map

| Worksheet | Table | Cols | Primary key |
| --- | --- | --- | --- |
| Items | item_templates | 46 | item_template_id |
| NPCs | npc_templates | 59 | npc_id |
| NPC Spawns | npc_spawns | 5 | — (composite) |
| NPC Drops | npc_drops | 4 | — (composite) |
| NPC Vendor Items | npc_vendor_items | 5 | — (composite) |
| Quests | quests | 14 | id |
| Quest Reqs | quest_requirements | 8 | id |
| Quest Rewards | quest_rewards | 8 | id |
| Spells | spells | 15 | spell_id |
| Spell Effects | spell_effects | 76 | spell_effect_id |
| Maps | maps | 17 | map_id |
| Map Required Items | map_required_items | 2 | — |
| Warptiles | warptiles | 6 | — |
| Combinations | combinations | 7 | combination_id |
| Combination Item Required | combination_item_required | 2 | — |
| Combination Item Result | combination_item_results | 2 | — |
| Titles | item_titles | 11 | id |
| Surnames | item_surnames | 11 | id |
| Classes | classes | 5 | class_id |
| Class Info | class_info | 26 | — |
| Class Levelup Spells | classes_levelup_spells | 3 | — |

`tools/DataEditor/schema.js` (generated by `tools/SchemaGen`) is the full machine-readable
schema and the authority for column names, kinds, required flags and enum members.
`gsheets.py schema SHEET` prints it.

## Invariants — breaking one corrupts the import

1. **Column order is load-bearing.** Cells are read by index against the descriptors. Never
   insert, delete, reorder or rename a column in the schema range; append new data columns only
   at the end of a sheet's schema width.
2. **Row 1 is the header; data starts at row 2.** Never write row 1.
3. **An empty cell means "use the SQL default"** — not zero. Leave a column blank when the
   default is what you want.
4. **Enum cells hold the member name as text**, exactly as spelled in the schema
   (`OneHandedPierce`, not `1` or `onehandedpierce`). The importer calls `Enum.Parse`, which is
   case-sensitive and throws on an unknown name, so a typo breaks the entire server import,
   not just that row.
5. **Bool cells hold `0` or `1`.** They are `CHAR(1)` and are imported as string literals.
6. **Write text with `valueInputOption=RAW`.** `USER_ENTERED` parses like typed input, so a
   description of `1-2` becomes a Date and `01` becomes `1`. The helper uses RAW everywhere;
   if you hand-write a `gws values update`, pass `"valueInputOption": "RAW"`.
7. **Ids are positive whole numbers, unique per sheet**, and the convention is `max + 1`
   (`Validation.nextId`). The helper allocates this for you on `create`.
8. **Nine worksheets have no primary key** (NPC Spawns, NPC Drops, NPC Vendor Items,
   Warptiles, Map Required Items, Combination Item Required, Combination Item Result, Class
   Info, Class Levelup Spells). Their column A is a foreign key that repeats legitimately —
   never de-duplicate those rows.
9. **Named helper columns after the schema width hold VLOOKUP formulas** (`Quest (Don't
   touch)`, `Item Name (Don't touch)`, …). They are display-only, not imported. A new row needs
   them copied down or the sheet reads as blank there; the helper clones the previous row's
   formulas and shifts the row numbers.
10. **Ids referenced from elsewhere must exist.** Item ids, NPC ids, map ids and spell ids are
    looked up at runtime and shown as "Unknown item"/"Unknown NPC" when missing.

Header labels carry the column's default in parentheses (`facing (3)`, `keep requirement? (0)`,
`type (Monster)`), and `gsheets schema` prints those same defaults from the generated schema.
Enum members can carry non-consecutive values — `NPCTemplate.Types` is `Monster = 2`,
`Vendor = 10`, `Banker = 11`, `Quest = 12` — so always write the member *name* and let the
importer resolve it. Never guess the underlying number.

## Column reference

### NPCs — `npc_templates`

`A id` (pk) · `B type` Enum[Monster|Vendor|Banker|Quest] · `C name` · `D title` · `E surname` ·
`F respawn time` · `G facing` (3) · `H lvl` (1) · `I exp` · `J aggro range` · `K attack range` ·
`L attack speed` (2) · `M move speed` (2) · `N stationary` · `O stunnable` · `P rootable` ·
`Q slowable` · `R invincible` · `S see invisible` · `T hp` · `U mp` · `V sp` · `W class id` ·
`X ac` · `Y str` · `Z sta` · `AA dex` · `AB int` · `AC fr` · `AD wr` · `AE sr` · `AF ar` ·
`AG er` · `AH body state` (3) · `AI body id` (1) · `AJ body r` … `AM body a` ·
`AN face id` · `AO hair id` · `AP hair r` … `AS hair a` ·
`AT equipped items` (`0,*,0,*,0,*,0,*,0,*,0,*` — slot,id pairs) · `AU weapon dmg` (1) ·
`AV armor pierce` · `AW hp % reg` · `AX hp static reg` · `AY mp % reg` · `AZ mp static reg` ·
`BA alliance` · `BB stuck behaviour` Enum[DoNothing|TeleportToAggro|TeleportAggro] ·
`BC stuck timeout` (20) · `BD credit dealer` · `BE quest ids` (space/comma separated) ·
`BF script_path` (defaults to `Scripts/NPC/BaseNPC.csx`) · `BG script params`

Required: `id`, `name`. An NPC exists in the world only once it also has an **NPC Spawns** row.

An NPC's real HP and AC come from **two** places: `T hp` / `X ac` on the template **plus** the
row for its `class id` and `lvl` in the Class Info sheet — `MaxStats = template BaseStats +
ClassInfo BaseStats` (`NPC.cs:643-662`). A blank `T hp` therefore means "use the class table",
which is why most mobs carry no explicit hp while high-level bosses add a large number on top.

`N stationary` is **inverted** at load: `npc.CanMove = reader.GetString("stationary") != "1"`
(`NPCHandler.cs:98`). It behaves as a property of a *placement* rather than of the mob.
A spawn row's `E properties` cell is now the per-placement override mechanism for exactly this
kind of thing, and folding `stationary` into it is a live option.

`BA alliance` holds **template ids** that assist one another: when an NPC aggros, it pulls every
ally standing within that ally's own aggro range (`NPC.cs:569`, `1028`). Because it stores ids,
retiring a template means rewriting the alliance list on every NPC that named it.

### NPC Spawns / NPC Drops / NPC Vendor Items

- **NPC Spawns** — `A npc id` · `B map id` · `C map x` · `D map y` · `E properties` (JSON; blank = no overrides), the first four required.
  Keys are camelCase NPC property names; this cut supports exactly one, `canMove`, and its value
  must be the **JSON boolean** `true`/`false` — `1`/`0` is how the sheet's Bool columns are
  written, and it is valid JSON here but throws at load, which stops the server from starting.
  `canMove` overrides the NPC template's `stationary`/movement setting for that one spawn.
- **NPC Drops** — `A npc id` · `B item id` · `C stack size` · `D droprate`, all required.
- **NPC Vendor Items** — `A npc id` · `B item id` · `C stack` (1) · `D stats visible` (1) ·
  `E slot number` (required, 0-based). The NPC needs `type = Vendor` to show stock.

### Items — `item_templates`

`A id` (pk) · `B usetype` Enum[NoUse|OneTime|Armor|Weapon|Scroll|HairDye|Letter|Money|Recipe] ·
`C name` · `D description` · `E hp` · `F mp` · `G sp` · `H ac` · `I str` · `J sta` · `K dex` ·
`L int` · `M fr` · `N wr` · `O sr` · `P ar` · `Q er` · `R min exp` · `S min lvl` · `T max exp` ·
`U max lvl` · `V weap dmg` · `W weap dly` (10) ·
`X slot` Enum[Helmet|Shield|OneHanded|TwoHanded|Ring|Necklace|Pauldrons|Cloak|Belt|Gloves|Chest|Pants|Shoes|Mount|Misc] ·
`Y type` Enum[None|Plate|Leather|Cloth|Mail|OneHandedSword|TwoHandedSword|OneHandedBlunt|TwoHandedBlunt|OneHandedPierce|TwoHandedPierce|Fist] ·
`Z value` · `AA lore` · `AB bop` · `AC boe` · `AD event` · `AE graphic tile` ·
`AF graphic file` · `AG equip display` · `AH r` … `AK a` · `AL classes` ·
`AM stack` (1) · `AN body state` (3) · `AO effect` (spell effect id) · `AP effect %` (100) ·
`AQ learn spell id` · `AR credits` · `AS script path` · `AT script params`

Required: `id`, `usetype`, `name`, `graphic tile`. `AE graphic tile` / `AF graphic file` are
client sprite atlas coordinates — there is no way to invent them, so copy them from an existing
item of the same look (`python3 $S read Items --limit 50` then take a row's values).

`Z value` is the vendor **shelf price**. Vendors sell at full `value` but **buy back at half**
(`GoldCurrency`: `template.Value * stack` versus `stack * item.Value / 2`), and refuse items whose
value is 0 — the usual choice for a quest keepsake. Price gold rewards against the sell-to-vendor
figure, not the shelf price.

### Quests — `quests`

`A id` (pk) · `B name` · `C description` · `D pass text` · `E fail text` ·
`F classes` (bitmask — see below) · `G min experience` · `H max experience` ·
`I min level` · `J max level` · `K repeatable` · `L show progress` ·
`M only one player can complete` ·
`N prerequisite quest ids` (space/comma separated)

Required: `id`, `name`. A quest is reachable from an NPC through that NPC's `BE quest ids` column.

**Wrap `C`–`E` by hand at 50 characters.** The client's quest window is 50 wide, so break lines
with a **literal `\n`** — the two characters `\` and `n`, not a real newline. Across the 388
dialogue lines in the shipped sheet the longest is 42 and none exceeds 50; treat 50 as the hard
limit and break on a word boundary. In JSON that is `\\n`, so build the text with a script that
starts from real newlines and replaces them, rather than typing escapes by hand.

`F classes` is a **bitmask, not a class id**: the check is
`(classRestrictions & (1L << ClassID)) != 0` (`Class.CanUse`), so Priest is `32`, Magus `16`,
Warrior `8`, Rogue `4`, Commoner `2`, and values OR together (`12` = Rogue *or* Warrior). `0`
means any class. The `Items` sheet's `AL classes` uses the same encoding.

### Quest Reqs — `quest_requirements`

`A id` (pk) · `B quest id` · `C requirement type` · `D value` · `E value2` ·
`F keep requirement?` · `G script path` · `H script params`

Required: `id`, `quest id`, `requirement type`, `value`. What `value` / `value2` mean per type
(`QuestWindow.PlayerMeetsRequirements`):

| requirement type | value | value2 |
| --- | --- | --- |
| `Gold` | gold required | — |
| `Item` | item id | stack count |
| `Kill` | NPC id | number of kills |
| `TalkToNPC` | NPC id | number of talks |
| `ExperienceBanked` | experience | — |
| `ExperienceSold` | experience sold | — |
| `NothingEquipped` | — | — |
| `Script` | — | — (needs `script path`, or the server throws at load) |

`keep requirement?` decides whether the item/gold/experience is consumed on completion (`0`)
or left with the player (`1`).

### Quest Rewards — `quest_rewards`

`A id` (pk) · `B quest id` · `C reward type` · `D long value` · `E long value 2` ·
`F string value` · `G script path` · `H script params`

Required: `id`, `quest id`, `reward type`. What each field carries per type
(`QuestWindow.GiveRewards`):

| reward type | long value | long value 2 | string value |
| --- | --- | --- | --- |
| `Gold` | amount | — | — |
| `Item` | item id | stack | — |
| `Title` / `Surname` | — | — | the title text |
| `Teleport` | — | — | `mapId,x,y` |
| `Experience` | amount | — | — |
| `FaceGraphic` / `BodyGraphic` / `HairGraphic` | graphic id | — | — |
| `HairColour` / `BodyColour` | — | — | `r,g,b,a` |
| `ClassChange` | class id | — | — |
| `HP` `MP` `AC` `Stamina` `Strength` `Dexterity` `Intelligence` | amount | — | — |
| `SpellBuff` | spell effect id | — | — |
| `LearnSpell` | spell id | — | — |
| `Script` | — | — | — (needs `script path`) |

Class ids: 1 Commoner, 2 Rogue, 3 Warrior, 4 Magus, 5 Priest, 6 Game Master.

## Quest mechanics that bite

**A giver opens one window per eligible quest.** `QuestWindow.Handle` builds a window inside its
loop over the NPC's quests, so an NPC with two prerequisite-free quests opens **two** windows.
Every window is created at `0,0` (`Packets.cs` `MakeWindow` hardcodes the position), so they
stack and the player sees only the top one until they drag it aside. Two or three concurrent
quests per NPC is fine; more than that is a mess.

**Order `quest ids` deliberately — an unmet prerequisite aborts the whole list.** The loop does a
bare `return` (not `continue`) when a prerequisite is unmet, when the player is past `max level` /
`max experience`, or when `Class.CanUse` fails. So a quest with no prerequisite listed *after* a
chained one is invisible until that chain is finished. List independent quests first, then the
chain in prerequisite order:

```
quest ids = 41 42 43 44 45    41 is standalone, 42→43→44→45 is the chain
```

The same `return` means **per-class quests cannot share a giver**: a Warrior who reaches a
Rogue-only quest aborts the loop before their own is reached. One giver per class.

**Re-clicking a giver leaves stale windows.** There is no server-pushed window-close packet — the
protocol has only `MakeWindow`, `EndWindow` and `WindowTextLine`, so the server cannot dismiss a
window the client is showing. `Handle` drops its own records and builds new windows with fresh
ids, so clicking twice without closing the first leaves orphans on screen.

### Requirement and reward gotchas

- **`keep requirement?` = `0` consumes the requirement**, and `TakeRequirements` handles each type
  differently: `Gold` and `Item` are removed from the player, `ExperienceBanked` is subtracted,
  `Kill`/`TalkToNPC` **decrement the progress counter** by `value2`, and `ExperienceSold` /
  `NothingEquipped` are no-ops.
- **`NothingEquipped` is evaluated at turn-in** (`PlayerMeetsRequirements`, called when the player
  clicks to complete), so a player kills fully geared, strips, and hands in. Flavour only, never a
  challenge.
- **`ExperienceSold` only counts experience spent at max level** — it increments in `/buyvita` and
  `/buymana`, which both refuse unless the class level table has no experience requirement left.
  A useful gate for veteran-only content.
- **Reward strings are parsed without validation and will throw mid-reward.** `GiveRewards` does
  `StringValue.Split(',')` then indexes `m[1]`/`m[2]` for `Teleport`, and `int.Parse`s four parts
  for `HairColour`/`BodyColour`. A malformed value throws *after* `TakeRequirements` has already
  consumed the player's items. `Teleport` must be exactly `mapId,x,y`; colours exactly `r,g,b,a`.
- **Item rewards need a free inventory slot** and `LearnSpell` rewards a free spellbook slot —
  checked before completion, so the quest refuses rather than dropping the reward.

## Recipes

Look ids up with `read`; never guess one.

**Create an item**

```bash
S=.agents/skills/goose-game-data/scripts/gsheets.py
python3 $S create Items \
  --set usetype=Weapon --set name="Rusty Dagger" \
  --set description="A pitted blade, more rust than steel." \
  --set "weap dmg=6" --set "weap dly=12" --set slot=OneHanded --set type=OneHandedPierce \
  --set value=40 --set "min lvl=1" --set "graphic tile=332205" --set "graphic file=2278" \
  --set "equip display=2"
```

**Create an NPC** (then place it, or it never appears)

```bash
python3 $S create NPCs \
  --set type=Monster --set name="Goblin Scout" --set "respawn time=60" --set lvl=5 \
  --set hp=120 --set exp=45 --set "aggro range=6" --set "attack range=1" \
  --set "class id=4" --set "body id=10113" --set "move speed=2" --set "weapon dmg=8"

python3 $S create "NPC Spawns" --set "npc id=192" --set "map id=1" --set "map x=50" --set "map y=50"
python3 $S create "NPC Drops" --set "npc id=192" --set "item id=661" --set "stack size=1" --set droprate=25
```

`create` prints the row it wrote, including the id it allocated — reuse that id for the spawn,
drop and vendor rows.

**Create a quest with requirements and rewards**

```bash
python3 $S create Quests \
  --set name="Scout's Report" \
  --set description="The scouts have not reported back.\nThin their numbers." \
  --set "pass text=Good work. Take this." --set "fail text=You are not ready." \
  --set "min level required=3" --set "max level required=10" --set repeatable=0 --set "show progress=1"

python3 $S create "Quest Reqs" --set "quest id=11" --set "requirement type=Kill" \
  --set value=192 --set value2=10 --set "keep requirement?=0"

python3 $S create "Quest Rewards" --set "quest id=11" --set "reward type=Experience" --set "long value=500"
python3 $S create "Quest Rewards" --set "quest id=11" --set "reward type=Item" \
  --set "long value=661" --set "long value 2=1"
```

Attach the quest to an NPC with `update NPCs --id <npc> --set "quest ids=11"`.

**Place an NPC spawn** — check the tile is free and inside the map first. A map's dimensions are
the two little-endian Int32s at offset 4 of `Goose/Data/<DataPath>/Maps/Map1<0-padded map id>.map`,
after two Int16 version fields:

```bash
python3 $S create "NPC Spawns" --set "npc id=192" --set "map id=7" --set "map x=3" --set "map y=50"
```

Nothing rejects two NPCs on one tile — both exist, and the click handler uses the first match in
range. Filter `read "NPC Spawns"` by map id before choosing coordinates.

**Update an existing value**

```bash
python3 $S update Items --id 661 --set value=55 --set "weap dmg=7"
python3 $S update NPCs --id 84 --set "hp % reg=0.25" --set "respawn time=45"
```

`update` finds the row by primary key, writes only the cells whose value actually changed, and
prints the row afterwards. On a no-pk sheet (spawns, drops, vendor stock) edit in place by
reading the rows first and using `gws` directly with an explicit range — the helper will refuse
because there is no key to match on.

## Verifying a change

1. Read it back: `python3 $S read Items --id 661`.
2. If the server is running, the change is not live until `/updatesql` (GM console command) or a
   restart. A sheet edit alone changes nothing in game.
3. `git diff` will not show anything — the sheet is not in the repository. State what you wrote
   in your summary: sheet, row, id, and each value.

## Failure modes seen in practice

- `Read-only file system (os error 30)` on the token directory → the config-dir bootstrap above
  was skipped.
- `Quota exceeded for quota metric 'Read requests'` (HTTP 429) → too many full-sheet reads in a
  minute. `gws` is retried with a backoff, but collapse the work into one `batch`.
- `token_valid: false` with `token_error: Bad Request` in `gws auth status` → the cached access
  token expired; run `gws auth login` and re-check with `gws auth status`.
- `no X member named 'Y'` from the importer → an enum cell holds a value that is not an exact
  member name.
- `has no script_path` from the importer → a `Script` requirement or reward without `script path`.
