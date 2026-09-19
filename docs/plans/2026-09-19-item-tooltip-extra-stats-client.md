# Item Tooltip Extra Stats (Client) Implementation Plan

**Goal:** Parse the new trailing `ExtraStats` field on the item slot packets and render it
as tooltip stat lines, so a title or surname's haste, crit, damage and regeneration show
up on the item.

**Architecture:** The field is read with the existing optional-trailing-field pattern,
split into an `int[]` on `ItemStats`, and rendered by one loop in `ItemTooltipText.Build`
against a label table that defines the wire order, the label and whether the value is a
percentage or a flat amount. No Godot types enter the parsing or the formatting, so the
whole change is unit-testable without instantiating a scene.

**Tech Stack:** C# / .NET 10, Godot 4.6 (unused by the changed code), xUnit.

**Repo:** `~/code/Goose2ClientGodot`. This plan was written from the server repo because
the server session could not write here; move it to `docs/plans/` in this repo when the
client session starts, and work on a branch of its own.

**Companion plan:** `gooseserver docs/plans/2026-09-19-item-tooltip-extra-stats.md`, which
ships the field. Design: `2026-09-19-item-tooltip-extra-stats-design.md`.

---

## APIs verified

| Fact | Citation |
| --- | --- |
| Currency is read as an optional trailing field | `Scripts/Network/Packets/InventorySlotPacket.cs:107` |
| Same for the vendor layout, which duplicates `Parse` | `Scripts/Network/Packets/VendorSlotPacket.cs:59` |
| Bank and combine bag inherit `InventorySlotPacket.Parse`, overriding only `Prefix` | `Scripts/Network/Packets/BankSlotPacket.cs:6-8`, `CombineBagSlotPacket.cs:6-8` |
| `ItemStats` is the tooltip DTO; `FromPacket(InventorySlotPacket)` copies field by field | `Scripts/ItemStats.cs:5-53,55-105` |
| Tooltip stat lines are built here; resists end and class requirements begin | `Scripts/UI/ItemTooltipText.cs:106-111` |
| Existing signed-integer formatter, `N0` with a `+` for positives | `Scripts/UI/ItemTooltipText.cs:26-31` |
| Tooltip colours; `Stat` is the stat teal | `Scripts/UI/ItemTooltipText.cs:8-19`, `Scripts/UI/ItemTooltipControl.cs:120` |
| `Build` is Godot-free and consumed here | `Scripts/UI/ItemTooltipControl.cs:43-52` |
| The test project compiles every `Scripts/**/*.cs`, so no csproj change is needed | `tests/Goose2Client.Tests/Goose2Client.Tests.csproj:13-15` |
| Packet-body test helper that already appends trailing fields | `tests/Goose2Client.Tests/ItemStatsTests.cs:9-16` |
| Tooltip test shape | `tests/Goose2Client.Tests/ItemTooltipTextTests.cs:10-18` |

Test command:

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj
```

---

### Task 1: Parse the payload into `ItemStats`

**Files:**
- Modify: `Scripts/Network/Packets/InventorySlotPacket.cs` (property near `CurrencyName:52-54`, parse at `:107`)
- Modify: `Scripts/Network/Packets/VendorSlotPacket.cs` (parse at `:59`)
- Modify: `Scripts/ItemStats.cs` (property near `:53`, copy in `FromPacket`, parse helper)
- Test: `tests/Goose2Client.Tests/ItemStatsTests.cs`

**Mutation impact:**
- Source of truth changed: none stored. `ItemStats` is a per-tooltip DTO built from a
  packet; nothing caches it.
- Important readers: `ItemTooltipText.Build` via
  `ItemStats.FromPacket` (`Scripts/UI/InventoryWindow.cs:78`, `BankWindow.cs:99`,
  `CombineBagContainerWindow.cs:76`, `CharacterWindow.cs:116`, `VendorWindow.cs:95`,
  `HotbarWindow.cs:221`).
- Derived/cached state affected: no derived state found.
- Required propagation sequence: packet → `ItemStats.FromPacket` → `Build`, all
  synchronous within one tooltip refresh. No event, no persistence.
- Invariants to preserve: an absent field leaves every existing `ItemStats` value
  untouched, so tooltips against an older server are byte-identical to today's.
- Observable proof required: parsing tests for present, absent, partial and
  longer-than-known payloads, asserting the resulting array.

**Step 1: Write the failing tests**

Extend `tests/Goose2Client.Tests/ItemStatsTests.cs` using its existing
`InventorySlotBody(params string[] trailing)` helper, which already supports extra
trailing fields:

- `InventorySlot_reads_the_trailing_extra_stats` — `InventorySlotBody("gold", "400")` →
  `ExtraStats` is `[400]`.
- `InventorySlot_without_extra_stats_parses` (regression) — no trailing payload at all →
  `ExtraStats` is empty and every earlier field is where it was.
- `VendorSlot_reads_the_trailing_extra_stats` — same through `VendorSlotPacket`.
- `FromPacket_copies_extra_stats` — an `InventorySlotPacket` with
  `ExtraStats = "0,800"` → the `ItemStats` array is `[0, 800]`.
- `FromPacket_ignores_entries_beyond_the_known_stats` (adversarial) — a 16-entry payload
  parses without throwing and keeps the leading entries; a future server may append.
- `FromPacket_treats_malformed_entries_as_zero` — `"400,x"` → `[400, 0]`; malformed wire
  data must not throw inside packet handling.

**Step 2: Run to verify they fail (red)**

Run: `dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter FullyQualifiedName~extra_stats`
Expected: FAIL to compile — `ExtraStats` does not exist on either type.

**Step 3: Implement**

`InventorySlotPacket` and `VendorSlotPacket` each get the same treatment, mirroring the
currency field directly above:

```csharp
        /// <summary>Comma-separated item stats the client cannot otherwise show, in wire
        /// order: haste, spell damage, spell crit, melee damage, melee crit, damage
        /// reduction, then percent/flat pairs for HP, MP and SP regeneration. Percentages
        /// are basis points. Appended after the currency name, so an older server leaves
        /// this null.</summary>
        public string ExtraStats { get; set; }

        // in Parse, after the currency line
        ExtraStats = p.LengthRemaining() > 0 ? p.GetString() : null,
```

`ItemStats` gains the array and the split:

```csharp
        public int[] ExtraStats { get; set; } = Array.Empty<int>();

        private static int[] ParseExtraStats(string extraStats)
        {
            if (string.IsNullOrEmpty(extraStats)) return Array.Empty<int>();

            var parts = extraStats.Split(',');
            var values = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                int.TryParse(parts[i], NumberStyles.Integer, InvariantCulture, out values[i]);

            return values;
        }
```

and `FromPacket(InventorySlotPacket)` sets `ExtraStats = ParseExtraStats(packet.ExtraStats)`.
`NumberStyles` and `CultureInfo.InvariantCulture` need their usings; the file currently has
none for them.

**Step 4: Run to verify green**

Run: `dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj`
Expected: PASS, previous tests plus the new ones.

**Step 5: Commit**

```bash
git add Scripts/Network/Packets/InventorySlotPacket.cs Scripts/Network/Packets/VendorSlotPacket.cs \
        Scripts/ItemStats.cs tests/Goose2Client.Tests/ItemStatsTests.cs
git commit -m "Parse the trailing extra stats on item slots"
```

---

### Task 2: Render the lines

**Files:**
- Modify: `Scripts/UI/ItemTooltipText.cs` (label table near the top, loop between the
  resist block and the class-restriction block, `:106-111`)
- Test: `tests/Goose2Client.Tests/ItemTooltipTextTests.cs`

**Mutation impact:**
- Source of truth changed: none. Lines are derived per call from `ItemStats`.
- Important readers: `ItemTooltipControl.SetItem` (`:43-52`), which rebuilds the whole
  VBox each refresh and sizes the panel from its content (`:79-93`), so extra lines
  lengthen the tooltip without any layout constant changing.
- Derived/cached state affected: no derived state found.
- Required propagation sequence: none beyond the existing per-hover rebuild.
- Invariants to preserve: an item with no extra stats produces exactly the lines it
  produces today; zero-valued stats never produce a line; the block sits after the resists
  and before the requirements.
- Observable proof required: exact line text, order, colour; a null/empty payload changing
  nothing.

**Step 1: Write the failing tests**

Extend `tests/Goose2Client.Tests/ItemTooltipTextTests.cs`, following its existing shape
(build, then select the lines you care about):

- `Build_Extra_stats_lines_in_wire_order` — a full twelve-value array → the twelve
  expected texts in order.
- `Build_Omits_zero_extra_stats` (regression) — `ExtraStats = new int[12]` and also the
  default empty array → no `ItemTooltipColor.Stat` lines beyond the primaries, and the
  line list equals what it was before this change for the same item.
- `Build_Formats_fractional_percent` — `450` in the spell-damage slot → `"+4.5% Spell Damage"`.
- `Build_Formats_negative_extra_stat` — `-400` in the haste slot → `"-4% Melee Attack Speed"`.
- `Build_Reports_both_regen_halves` — `225` and `2400` → `"+2.25% Health Regeneration"`
  and `"+2,400 Health Regeneration"` as two lines.
- `Build_Places_extra_stats_before_the_requirements` — an item with a level requirement
  and an extra stat puts the stat line first.
- `Build_Ignores_entries_beyond_the_label_table` (adversarial) — a 14-entry array renders
  twelve lines and does not throw.

Expected values per wire index:

| Index | Label | Percent | Example |
| --- | --- | --- | --- |
| 0 | Melee Attack Speed | yes | `400` → `+4% Melee Attack Speed` |
| 1 | Spell Damage | yes | `450` → `+4.5% Spell Damage` |
| 2 | Spell Critical Chance | yes | |
| 3 | Melee Damage | yes | |
| 4 | Melee Critical Chance | yes | |
| 5 | Damage Reduction | yes | |
| 6 | Health Regeneration | yes | `225` → `+2.25% Health Regeneration` |
| 7 | Health Regeneration | no | `2400` → `+2,400 Health Regeneration` |
| 8 | Mana Regeneration | yes | |
| 9 | Mana Regeneration | no | |
| 10 | Spirit Regeneration | yes | |
| 11 | Spirit Regeneration | no | |

**Step 2: Run to verify they fail (red)**

Run: `dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter FullyQualifiedName~Extra_stats`
Expected: FAIL — no such lines are produced.

**Step 3: Implement**

One table defines the order, the label and the kind, next to the other private helpers:

```csharp
        private static readonly (string Label, bool IsPercent)[] ExtraStatLines =
        [
            ("Melee Attack Speed", true),
            ("Spell Damage", true),
            ("Spell Critical Chance", true),
            ("Melee Damage", true),
            ("Melee Critical Chance", true),
            ("Damage Reduction", true),
            ("Health Regeneration", true),
            ("Health Regeneration", false),
            ("Mana Regeneration", true),
            ("Mana Regeneration", false),
            ("Spirit Regeneration", true),
            ("Spirit Regeneration", false),
        ];

        private static string FormatPercent(int basisPoints) =>
            (basisPoints / 100m).ToString("0.##", Inv) + "%";

        private static string FormatExtraStat(int value, bool isPercent)
        {
            if (!isPercent) return FormatNumber(value);

            string text = FormatPercent(value);
            return value > 0 ? "+" + text : text;
        }
```

and one loop in `Build`, placed after the SpiritResist line (`:106-109`) and before the
class-restriction block (`:111`):

```csharp
            for (int i = 0; i < ExtraStatLines.Length && i < s.ExtraStats.Length; i++)
            {
                int value = s.ExtraStats[i];
                if (value == 0) continue;

                lines.Add((
                    $"{FormatExtraStat(value, ExtraStatLines[i].IsPercent)} {ExtraStatLines[i].Label}",
                    ItemTooltipColor.Stat));
            }
```

`Inv` and `FormatNumber` already exist in the file (`:24-31`). Nothing else in `Build`
changes, and `ItemTooltipControl` needs no edit.

**Step 4: Run to verify green**

Run: `dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj`
Expected: PASS.

**Step 5: Commit**

```bash
git add Scripts/UI/ItemTooltipText.cs tests/Goose2Client.Tests/ItemTooltipTextTests.cs
git commit -m "Render extra item stats in the tooltip"
```

---

### Task 3: Prove it against the real server

**Files:** none. This task produces evidence, not code.

**Step 1: Run the full client test project**

Run: `dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj`
Expected: PASS, no failures in the tooltip or map-editor suites.

**Step 2: Smoke against a server carrying the companion plan**

Start a server built from the server-side branch, run the client against it
(`GOOSE_HOST` / `GOOSE_PORT`, `run.sh`), and check:

- An abyss item with `of Speed` (or any suffixed drop) shows a `+N% Melee Attack Speed`
  line in the stat block, above the requirements.
- An `of Vita Regen` item shows both regeneration lines.
- An ordinary unsuffixed item's tooltip is unchanged.
- A vendor tooltip is unchanged (vendor stock carries no modifiers).
- The tooltip grows taller; its width is unchanged unless a line is longer than the name.

**Step 3: Record what the smoke found**

Add the observed result to the pull request description. If the layout needs a colour or
label change, make it here rather than in the server plan.

---

## Invariant-to-test matrix

| Invariant | Proved by |
| --- | --- |
| An older server's tooltip is unchanged | `Build_Omits_zero_extra_stats`, `InventorySlot_without_extra_stats_parses` |
| Values are read in wire order, percentages as basis points | `Build_Extra_stats_lines_in_wire_order`, table test cases |
| Signs and fractional percentages format correctly | `Build_Formats_fractional_percent`, `Build_Formats_negative_extra_stat` |
| Both halves of a regen stat render as two lines | `Build_Reports_both_regen_halves` |
| The block sits before the requirements | `Build_Places_extra_stats_before_the_requirements` |
| A longer payload from a future server is safe | `FromPacket_ignores_entries_beyond_the_known_stats`, `Build_Ignores_entries_beyond_the_label_table` |
| Malformed payloads do not break packet handling | `FromPacket_treats_malformed_entries_as_zero` |

## Design alignment

- Labels and order match the design's table exactly, including "Melee Attack Speed" rather
  than "Haste" and the invented "Spirit Regeneration" wording.
- Percent formatting is up to two decimals with trailing zeros trimmed, which is what the
  design's `400` → `4%` and `450` → `4.5%` examples require; flat regeneration uses the
  existing `N0` formatter.
- Regeneration renders as two lines, per the design's decision against the character
  window's fused form (`gooseserver Goose/Window.cs:238`).
- Colour is `ItemTooltipColor.Stat`; the design rejects the effect tan.
- The design's client edits are two parse sites plus one render site; bank and combine bag
  come along through inheritance, as the design's gap check verified.
- Deliberately not done: any change to `ItemTooltipControl`, the tooltip scene, or the
  layout metrics.

## Verification before handing off

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj
./build.sh linux          # optional, only if a build is needed for the smoke
```
