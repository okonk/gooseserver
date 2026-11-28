# Ground Zone Styles — Part 1: Server and Sheet Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ground zone effects carry a client-owned style name (`zone_style`) from the sheet through the
server to the client in `ZADD` and `SSS`, and the sheet holds the four styled zone spells.

**Architecture:** One new text column on `spell_effects`, added through CsvToSql only. The loader reads it
into `SpellEffect.ZoneStyle`; a comma in it rejects the zone at load because both packets are
comma-separated. `ZADD` gains the style before its tile list, `SSS` gains it as the last field. The sheet
work runs last so the live sheet changes alongside the server code.

**Tech Stack:** C# / .NET 10, xunit, SQLite integration harness, Google Sheets via
`.agents/skills/goose-game-data/scripts/gsheets.py`.

**Design doc:** `docs/plans/2026-11-28-zone-styles-design.md` (Part 1 of 2; the client is Part 2).

**Worktree:** `/home/hayden/code/illutiagooseserver/.worktrees/zone-styles`, branch `zone-styles`.
Baseline: `Goose.Tests` 3143 passed.

**House rule (AGENTS.md):** no new comments or doc strings unless the *why* is non-obvious.

---

## APIs verified

| API | Where | Notes |
|---|---|---|
| CsvToSql column list, zone columns last | `CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs:104-110` | `Col.Text("script_path", def: "''")` is the text-column pattern |
| Loader reads zone columns | `Goose/SpellHandler.cs:136-139` | `reader.GetString("oneffect_text")` at `:66` is the text pattern |
| Load-time zone rejection | `Goose/SpellHandler.cs:148-153` (call), `:316-322` (`GroundZoneProblem`) | warn + remove + `continue` |
| `SpellEffect` zone properties | `Goose/SpellEffect.cs:150-152`; copy ctor `:281-283` | `OnEffectText` (`:136`) is a `string … = null!` text property |
| Copy-ctor guard | `Goose.Tests/SpellCloneTests.cs:27-33` | reflection fills every settable property (`string` → `"sample"`, `:104`), so a new property missing from the ctor fails it |
| `P.ZoneAdd` | `Goose/Packets.cs:60-62` | `ZADD id,anim,file,remainingMs,tiles…` |
| `P.SpellSlot` | `Goose/Packets.cs:706-726` | empty slot `:710` stays unchanged |
| Exact `ZADD` test | `Goose.Tests/GroundZones/ZonePacketTests.cs:18` | map-join test reads `remainingMs` at `Split(',')[3]` (`:56`) |
| `SSS` tail tests | `Goose.Tests/SpellCooldownPacketTests.cs:61` (`[^4]`), `:78`, `:86`, `:98` | every tail grows by one field |
| Zone load tests | `Goose.IntegrationTests/GroundZoneLoadTests.cs` | `effect_type = 24` is `GroundZone`; `target_type = 5` is Area |
| Validation tests | `Goose.Tests/Spells/GroundZoneValidationTests.cs:5-17` | `Zone(configure)` helper |
| Snapshot regeneration | `Goose.IntegrationTests/CsvToSqlSnapshotTests.cs` | `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot` |
| SchemaGen | `tools/SchemaGen/Program.cs` | `dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js` |
| Spell costs paid after `Cast` | `Goose/Player.cs:2477-2497` | the `%ccmp` snapshot sees full MP before the 100% MP cost |
| Sheet helper reads its schema from its own checkout; batch updates key on `"id"` | `.agents/skills/goose-game-data/scripts/gsheets.py:16,24,102,409` | run the **worktree's** copy so `zone_style` is known; point `GOOGLE_WORKSPACE_CLI_CONFIG_DIR` at the main checkout's `.gws` |

---

### Task 1: `zone_style` column, loader, copy and load check

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs:110` (after `zone_overlaps`)
- Modify: `Goose/SpellEffect.cs:152` (property), `:283` (copy ctor)
- Modify: `Goose/SpellHandler.cs:139` (loader), `:316-322` (`GroundZoneProblem`)
- Modify (generated): `Goose.IntegrationTests/Fixtures/generated.snapshot`, `tools/DataEditor/schema.js`
- Test: `Goose.Tests/Spells/GroundZoneValidationTests.cs`, `Goose.IntegrationTests/GroundZoneLoadTests.cs`

**Step 1: Write the failing tests**

Append to `Goose.Tests/Spells/GroundZoneValidationTests.cs` (inside the class):

```csharp
    [Fact]
    public void A_style_with_a_comma_is_rejected()
    {
        Assert.NotNull(SpellHandler.GroundZoneProblem(Zone(e => e.ZoneStyle = "heal,big")));
    }

    [Fact]
    public void A_plain_style_name_is_fine()
    {
        Assert.Null(SpellHandler.GroundZoneProblem(Zone(e => e.ZoneStyle = "heal")));
    }
```

Append to `Goose.IntegrationTests/GroundZoneLoadTests.cs` (before `RunSql`):

```csharp
    [Fact]
    public void Reads_the_zone_style_and_defaults_it_to_empty()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_style) VALUES (20, 'Sanctuary', 6, 24, 8, 5, 3, 'heal')");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size) VALUES (21, 'Plain Zone', 6, 24, 8, 5, 3)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_style) VALUES (22, 'Bad Style', 6, 24, 8, 5, 3, 'a,b')");

        Load();

        Assert.Equal("heal", world.SpellHandler.GetSpellEffect(20)!.ZoneStyle);
        Assert.Equal("", world.SpellHandler.GetSpellEffect(21)!.ZoneStyle);
        Assert.Null(world.SpellHandler.GetSpellEffect(22));
    }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~GroundZoneValidationTests`
Expected: build error — `SpellEffect` has no `ZoneStyle`.

**Step 3: Implement**

`CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs`, after `Col.Bool("zone_overlaps", def: false),`:

```csharp
            Col.Text("zone_style", def: "''"),
```

`Goose/SpellEffect.cs`, after `public bool ZoneOverlaps { get; set; }`:

```csharp
        public string ZoneStyle { get; set; } = "";
```

and in the copy ctor after `this.ZoneOverlaps = other.ZoneOverlaps;`:

```csharp
            this.ZoneStyle = other.ZoneStyle;
```

`Goose/SpellHandler.cs`, after `effect.ZoneOverlaps = reader.GetString("zone_overlaps") != "0";`:

```csharp
                effect.ZoneStyle = reader.GetString("zone_style");
```

and in `GroundZoneProblem`, before the final `return null;`:

```csharp
            if (effect.ZoneStyle.Contains(',')) return "zone_style can't contain a comma";
```

**Step 4: Regenerate the snapshot and the DataEditor schema**

```bash
GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js
git diff --stat
```

Expected: `generated.snapshot` and `tools/DataEditor/schema.js` change; `schema.js` gains a `zone_style`
entry after `zone_overlaps`. Nothing else in `schema.js` changes.

**Step 5: Run the tests**

```bash
dotnet test Goose.Tests --filter "FullyQualifiedName~GroundZoneValidationTests|FullyQualifiedName~SpellCloneTests"
dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~GroundZoneLoadTests|FullyQualifiedName~CsvToSqlSnapshot"
```

Expected: PASS. `SpellCloneTests.SpellEffect_copy_carries_every_property` proves the copy ctor line.

**Step 6: Commit**

```bash
git add CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs Goose/SpellEffect.cs Goose/SpellHandler.cs \
        Goose.Tests/Spells/GroundZoneValidationTests.cs Goose.IntegrationTests/GroundZoneLoadTests.cs \
        Goose.IntegrationTests/Fixtures/generated.snapshot tools/DataEditor/schema.js
git commit -m "feat(spells): add zone_style column for ground zones"
```

---

### Task 2: Send the style in `ZADD` and `SSS`

**Files:**
- Modify: `Goose/Packets.cs:60-62` (`ZoneAdd`), `:706-726` (`SpellSlot`)
- Test: `Goose.Tests/GroundZones/ZonePacketTests.cs`, `Goose.Tests/SpellCooldownPacketTests.cs`

**Step 1: Update the tests to the new formats**

`Goose.Tests/GroundZones/ZonePacketTests.cs`, in `ZoneAdd_lists_tiles_row_by_row`, change the effect and
the assertion:

```csharp
            Effect = new SpellEffect { Animation = 120, AnimationFile = 4, ZoneStyle = "heal" },
```

```csharp
        Assert.Equal("ZADD7,120,4,heal,8000,10,10,11,10,10,11", P.ZoneAdd(zone, 8000));
```

Add below it:

```csharp
    [Fact]
    public void ZoneAdd_sends_an_empty_style_when_unset()
    {
        var zone = new GroundZone { Id = 3, Effect = new SpellEffect(), Tiles = [(5, 5)] };

        Assert.Equal("ZADD3,0,0,,1000,5,5", P.ZoneAdd(zone, 1000));
    }
```

In the map-join test, `remainingMs` moves one field right:

```csharp
        long remaining = long.Parse(add.Substring(4).Split(',')[4]);
```

`Goose.Tests/SpellCooldownPacketTests.cs`:
- `:61` — `Split(',')[^4]` becomes `Split(',')[^5]`.
- `:78` — `Assert.EndsWith("," + spell.Aether + ",0,0,0,0,", packet);`
- `:86` — `Assert.EndsWith(",10000,1500,0,0,0,", packet);`
- `:98` — `Assert.EndsWith(",10000,0,1,5,2,", P.SpellSlot(spell, 1, 0, 0));`

Add after `SpellSlot_AppendsGroundTargetingShapeAndSize`:

```csharp
        [Fact]
        public void SpellSlot_AppendsZoneStyle()
        {
            var spell = NewSpell();
            spell.SpellEffect.ZoneStyle = "poison";

            Assert.EndsWith(",0,0,0,poison", P.SpellSlot(spell, 1, 0, 0));
        }

        [Fact]
        public void SpellSlot_EmptySlotIsUnchanged()
        {
            Assert.Equal("SSS4,,0,0,0,0,0,0,0", P.SpellSlot(null, 4, 0, 0));
        }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~ZonePacketTests|FullyQualifiedName~SpellCooldownPacketTests"`
Expected: FAIL on the exact-string and `EndsWith` assertions (the empty-slot test passes already).

**Step 3: Implement**

`Goose/Packets.cs`, `ZoneAdd`:

```csharp
        public static Func<GroundZone, long, string> ZoneAdd = (zone, remainingMs) =>
            "ZADD" + zone.Id + "," + zone.Effect.Animation + "," + zone.Effect.AnimationFile + "," +
            zone.Effect.ZoneStyle + "," + remainingMs + "," +
            string.Join(",", zone.Tiles.OrderBy(t => t.Y).ThenBy(t => t.X).Select(t => t.X + "," + t.Y));
```

`SpellSlot`, replace the last line `spell.SpellEffect.TargetSize;` with:

```csharp
                    spell.SpellEffect.TargetSize + "," +
                    spell.SpellEffect.ZoneStyle;
```

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~ZonePacketTests|FullyQualifiedName~SpellCooldownPacketTests|FullyQualifiedName~GroundZones"`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Packets.cs Goose.Tests/GroundZones/ZonePacketTests.cs Goose.Tests/SpellCooldownPacketTests.cs
git commit -m "feat(spells): send the zone style in ZADD and SSS"
```

---

### Task 3: Verify the server change and rebuild the DataEditor

**Step 1: Full test runs**

```bash
dotnet test Goose.Tests
dotnet test Goose.IntegrationTests
```

Expected: both pass. `Goose.Tests` should be 3143 + 4 = 3147.

**Step 2: Rebuild the DataEditor locally**

```bash
node tools/DataEditor/build.mjs
grep -c zone_style tools/DataEditor/dist/schema.html
```

Expected: `Built N modules into dist/` and a count of at least 1. `dist/` is gitignored; nothing to commit.
**Do not run `gclasp push`** — the user pushes the editor.

---

### Task 4: Sheet — header and the four zone spells

Run this task only when the user is ready for the sheet to change; the live server reads it on its next
`/updatesql`. CsvToSql reads columns by position, so an extra trailing column is harmless to the current
server, but run it alongside merging this branch.

**Setup** (from the worktree, so the helper reads this branch's `schema.js`):

```bash
export GOOGLE_WORKSPACE_CLI_CONFIG_DIR=/home/hayden/code/illutiagooseserver/.gws
S=.agents/skills/goose-game-data/scripts/gsheets.py
python3 $S doctor
```

**Step 1: Add the `zone style` header (column CB)**

The Spell Effects grid currently ends at CA (sheetId `497846300`), so append one column first:

```bash
ID=1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU
gws sheets spreadsheets batchUpdate --params "{\"spreadsheetId\":\"$ID\"}" \
  --json '{"requests":[{"appendDimension":{"sheetId":497846300,"dimension":"COLUMNS","length":1}}]}'
gws sheets spreadsheets values update \
  --params "{\"spreadsheetId\":\"$ID\",\"range\":\"Spell Effects!CB1\",\"valueInputOption\":\"RAW\"}" \
  --json '{"values":[["zone style"]]}'
python3 $S schema "Spell Effects" | tail -2
```

Expected: the schema's last line is `CB zone_style Text default=''`.

**Step 2: Check the ids**

```bash
for s in "Spell Effects" Spells Items; do python3 $S nextid "$s"; done
```

At planning time these were **346**, **169**, **923**. If they differ, shift every id below (and every
reference to it) by the same amount before writing.

**Step 3: Write the batch file**

Create `zone-styles.batch.json` in the worktree root (the helper can't see `/tmp`; the file is deleted in
Step 5). Use a script so `\n` in descriptions becomes the two characters `\` `n`:

```bash
python3 - <<'EOF'
import json
ops = [
  {"op": "update", "sheet": "Spell Effects", "id": "344", "set": {
      "target_size": "3", "spell_animation": "0", "zone_style": "heal"}},
  {"op": "update", "sheet": "Spell Effects", "id": "345", "set": {
      "target_size": "3", "spell_animation": "0", "zone_style": "earthquake"}},
  {"op": "update", "sheet": "Spells", "id": "168", "set": {
      "spell_target": "Self", "ground_targetable": "0",
      "spell_description": "Split the earth around you; foes standing on it\ntake a fifth of your current health every second."}},

  {"op": "create", "sheet": "Spell Effects", "set": {
      "spell_effect_id": "346", "spell_effect_name": "Empowering Ground Buff",
      "spell_effected": "SelfPlayer", "effect_type": "Buff", "spell_damage": "0.25",
      "works_not_in_pvp": "1", "buff_graphic": "810035", "buff_graphic_file": "20107"}},
  {"op": "create", "sheet": "Spell Effects", "set": {
      "spell_effect_id": "347", "spell_effect_name": "Empowering Ground",
      "spell_display": "Tile", "target_type": "Area", "target_size": "3",
      "spell_effected": "SelfPlayer", "effect_type": "GroundZone", "effect_duration": "30",
      "do_cast_animation": "0", "works_not_in_pvp": "1",
      "zone_buff_effect_id": "346", "zone_overlaps": "1", "zone_style": "empower"}},
  {"op": "create", "sheet": "Spell Effects", "set": {
      "spell_effect_id": "348", "spell_effect_name": "Poison Cloud",
      "spell_display": "Tile", "target_type": "Area", "target_size": "3",
      "spell_effected": "NPC", "effect_type": "GroundZone", "effect_duration": "10",
      "do_cast_animation": "0", "hp_change_formula": "-0.1 * %cchp - 0.1 * %ccmp",
      "zone_tick_ms": "1000", "zone_style": "poison"}},

  {"op": "create", "sheet": "Spells", "set": {
      "spell_id": "169", "spell_name": "Empowering Ground",
      "spell_description": "Charge the ground with power; allies standing\non it deal 25% more spell damage.",
      "spell_target": "Target", "class_restrictions": "16", "spell_aether": "60000",
      "spellbook_graphic": "810035", "spellbook_graphic_file": "20107",
      "mp_percent_cost": "100", "spell_effect_id": "347", "ground_targetable": "1"}},
  {"op": "create", "sheet": "Spells", "set": {
      "spell_id": "170", "spell_name": "Poison Cloud",
      "spell_description": "Release a choking cloud; foes standing in it\nare poisoned every second.",
      "spell_target": "Target", "class_restrictions": "4", "spell_aether": "30000",
      "spellbook_graphic": "810014", "spellbook_graphic_file": "20107",
      "mp_percent_cost": "100", "spell_effect_id": "348", "ground_targetable": "1"}},

  {"op": "create", "sheet": "Items", "set": {
      "item_template_id": "923", "usetype": "Scroll", "item_name": "Scroll: Empowering Ground",
      "description": "Teaches the Magus spell Empowering Ground.", "min lvl": "50", "lore": "1",
      "bop": "1", "graphic tile": "820110", "graphic file": "20398", "classes": "16",
      "learn spell id": "169"}},
  {"op": "create", "sheet": "Items", "set": {
      "item_template_id": "924", "usetype": "Scroll", "item_name": "Scroll: Poison Cloud",
      "description": "Teaches the Rogue spell Poison Cloud.", "min lvl": "50", "lore": "1",
      "bop": "1", "graphic tile": "820110", "graphic file": "20398", "classes": "4",
      "learn spell id": "170"}},
]
for op in ops:
    for k, v in op["set"].items():
        op["set"][k] = v.replace("\n", "\\n")
json.dump(ops, open("zone-styles.batch.json", "w"), indent=1)
EOF
```

**Step 4: Dry run, then write**

```bash
python3 $S batch zone-styles.batch.json --dry-run
python3 $S batch zone-styles.batch.json
```

Expected dry run: three updates and seven creates, no refusals. Batch updates take the row's primary key
in `"id"` (`gsheets.py:409`).

**Step 5: Read back and clean up**

```bash
python3 $S read "Spell Effects" --id 344
python3 $S read "Spell Effects" --id 345
python3 $S read "Spell Effects" --id 346
python3 $S read "Spell Effects" --id 347
python3 $S read "Spell Effects" --id 348
python3 $S read Spells --id 168
python3 $S read Spells --id 169
python3 $S read Spells --id 170
python3 $S read Items --id 923
python3 $S read Items --id 924
rm zone-styles.batch.json
```

Check against the design's sheet table: sizes 3, animations 0, the four style names, Earthquake `Self`
with `ground targetable = 0`, the zone buff link 347 → 346, the Poison Cloud formula, classes 16 and 4.
Report every row written (sheet, id, values) in the summary — `git diff` shows none of it.
