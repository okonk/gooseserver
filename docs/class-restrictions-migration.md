# Migrating `class_restrictions` to an allow list

`class_restrictions` used to be a **deny** list: the bit at index `class_id` was set for every
class that *could not* use the item, spell, combination or quest. It is now an **allow** list —
the bit is set for every class that *can*.

The reason is what happens when a class is added. Under the deny convention a new class could use
everything, and making that false meant finding every row that had ever meant to exclude anyone.
Under the allow convention a new class starts with the unrestricted rows and nothing else, which
is the safe direction to be wrong in.

`Goose/Class.cs`, `CanUse`, is the whole convention:

```csharp
public bool CanUse(long classRestrictions)
{
    return classRestrictions == 0 || (classRestrictions & (1L << this.ClassID)) != 0;
}
```

**0 means "no restriction", not "no class."** It is the only value that does not name a set of
classes, and it exists so that a row nobody has thought about stays usable — including by classes
added later. There is deliberately no value meaning "nobody may use this".

Bit indices are class ids, unchanged: bit 2 is class 2. Bit 0 belongs to no class.

| classes allowed | mask |
| --- | --- |
| everyone (and everyone added later) | `0` |
| Rogue (2) | `4` |
| Warrior (3) + Magus (4) | `24` |
| Magus (4) + Priest (5) | `48` |

## Converting the data

The data lives in the Google Sheets workbook the importer reads, so the conversion happens there —
there is no live-database step. All four tables that carry the column (`item_templates`, `spells`,
`combinations`, `quests`) are dropped and recreated from the sheet by `CsvToSql`, so a database
built after the sheet is converted is already correct, and one built before is fixed by rebuilding.

### The script (recommended)

`tools/DataEditor/MigrateClassRestrictions.gs` does the whole workbook. Copy it into the
spreadsheet's Apps Script project (Extensions → Apps Script → add a file), then from the editor's
function dropdown:

1. **`previewClassRestrictionsMigration`** — writes nothing. Read the execution log: it lists the
   class ids it inverted against, how many cells each sheet would change, and every cell it
   refuses to convert.
2. **File → Version history** on the spreadsheet, so there is a named restore point.
3. **`applyClassRestrictionsMigration`** — writes. It records a flag in the document properties
   and refuses to run a second time, because a second run inverts the data back.

Then delete the file; it is a one-off and is not part of the editor.

What it does per cell, and why:

- **blank** → left alone. Blank means "use the SQL default", which is 0 under either convention.
- **denied nobody** (`0`, or a mask setting only bits no class claims) → `0`. Not the all-classes
  mask: `0` is what a class added later inherits, and a row that never restricted anyone should
  keep that.
- **denied everybody** → **left alone and reported.** There is no allow-list value for "nobody",
  so this is a decision, not a conversion. Decide which classes may use those rows and set them
  by hand.
- **not a number** → left alone and reported.
- anything else → the sum of `2^class_id` over the classes that were *not* denied, **less the
  Game Master** (see below).

### The Game Master is not promoted into an allow list

No shipped mask ever denied the Game Master, so its absence from a deny mask was not a decision
anybody made — it is just what "restrict this to Rogues" looked like when there was nothing to say
about staff. Carrying it across would make every such row read *"Rogue or Game Master"* wherever
the classes are listed, in the client's item info line as much as in the editor.

So a converted mask names only real classes. The one exception is a row where the GM is **all that
is left** — every other class denied — which is a genuine "GM only" decision: dropping it there
would leave an empty allow list, and an empty allow list is `0`, meaning *everyone*. A row that
explicitly denied the GM keeps it out, as it always did.

The class is matched by **name** against the `Classes` sheet (`game master`, case- and
space-insensitive; the list is `CLASS_RESTRICTION_IGNORED_CLASSES_` at the top of the file). If no
row matches, the script says so in the log and carries on — silence there would look like "this
workbook has no GM" when it more likely means the name has changed, and every migrated mask is
then about to name a staff class.

This does not change how GMs reach restricted items: that has always been access privileges
(`AccessPrivilege.IgnoreItemRequirements`), not the data. Worth knowing that the item-use gate in
`Player` does not consult that privilege — the spell-cast path does — which was true before this
change too.

Bits belonging to no class — bit 0, set by most of the shipped masks — are **dropped**. Under the
deny convention bit 0 meant nothing; under the allow convention a leftover bit 0 is the difference
between `0` (everyone) and `1` (only a class that does not exist, i.e. nobody).

It finds the column **by position**, not by header text: the importer reads cells positionally and
the sheet headers are human labels that do not match the column names (`classes (0)` on three
sheets, `class restrictions (0)` on the fourth). The indices are in the file, next to the
descriptor each came from. If a column is ever inserted to the left of one, both that file and the
importer are wrong.

### By hand, with a formula

For a single sheet, or to check the script's work: put this in a spare column, with `$A$1` holding
the sum of `2^class_id` over the **real** classes — leave the Game Master out, so `62` for classes
1-5 with a GM at 6 — and `B2` holding the old mask.

```
=IF(BITAND(B2, $A$1) = 0, 0, BITAND(BITXOR(B2, $A$1), $A$1))
```

`BITXOR(B2, $A$1)` is the inversion; the outer `BITAND` drops bit 0 and any other bit no class
claims; the `IF` is the "denied nobody → 0" case. Copy the result column, paste-special *values
only* over the original, then delete the helper column.

It has no equivalent of the script's reports, and a row that denied every real class comes out as
`0` — *everyone* — which is the one way this formula can quietly do the wrong thing. Find those
rows first with a second helper column and settle them by hand: each is either "GM only" (`64`,
where the GM sits at class 6) or a row nothing can use, which the allow list cannot express.

```
=IF(BITAND(B2, $A$1) = $A$1, "CHECK", "")
```

## What changed in the server

Nothing loads or stores the column differently — it is still `class_restrictions`, still a
`BIGINT`, still read at startup by `ItemHandler`, `SpellHandler`, `CombinationHandler` and
`Quest`. Only the meaning of the bits changed, so **an unconverted workbook produces a server that
silently allows exactly the wrong classes.** Convert the sheet and the server together.

Every gate now routes through `Class.CanUse` rather than open-coding the mask test:
`Player.CanUseItem` and the spell-cast path, `Inventory` (combining), `Spellbook` (class change),
`QuestWindow`, `ItemTemplate.FigureClassRestrictions` (which builds the client's item-info line)
and `Data/Aspereta/Scripts/Global/Aspereta.csx`.

The editor's bitmask control now reads *checked = can use*, and says "no restriction — every class
can use it" when the mask is 0, since an empty grid of checkboxes otherwise reads as "nobody".
