# Item Tooltip Extra Stats (Server) Implementation Plan

**Goal:** Put the twelve item stats a title or surname can add — but that the tooltip cannot
show today — onto the item slot packets as one trailing `ExtraStats` field.

**Architecture:** A single append-only wire field built at send time from the stats the
packet already reads: `item.TotalStats` for `P.ItemSlot`, `item.BaseStats` for
`P.VendorItemSlot`. Percentages travel as basis points. No state is stored, no schema
changes, no migration. The client half is a separate plan
(`2026-09-19-item-tooltip-extra-stats-client.md`) in the `Goose2ClientGodot` repo.

**Tech Stack:** C# / .NET 10, xUnit, `TestWorldFixture` / `VendorFixture` (fast project),
`GlobalScriptFixture` (integration project, shipped dimensions data).

**Design doc:** `docs/plans/2026-09-19-item-tooltip-extra-stats-design.md`.

---

## APIs verified

| Fact | Citation |
| --- | --- |
| Packet builders live in `public static class P` | `Goose/Packets.cs:5` |
| `P.ItemSlot` emits `item.TotalStats.*` and ends with the currency name | `Goose/Packets.cs:428-484` |
| `P.VendorItemSlot` emits `item.BaseStats.*` and ends with the currency name | `Goose/Packets.cs:488-541` |
| Every Illutia slot packet routes through these two | `Goose/Packets.cs:543-587` |
| Fraction → 1/10000 helper, `internal static decimal ExactProduct(double, long)` | `Goose/Utils.cs:50-53` |
| Same helper already used for a crit-percentage conversion | `Goose/SpellEffect.cs:630,633` |
| `StatMultiplier` scales every `AttributeSet` field, including the twelve | `Goose/AttributeSet.cs:196-201` |
| The twelve fields exist on `AttributeSet` | `Goose/AttributeSet.cs:11-36` |
| Fast fixture: `AddBaseItemTemplate(int, string, ItemTemplate.UseTypes, Action<ItemTemplate>?)` | `TestSupport/TestWorldFixture.cs:151` |
| Vendor fixture: `Carry`, `VendorDealsIn`, `World`, `Vendor` | `Goose.Tests/Fixtures/VendorFixture.cs:78-110` |
| Integration fixture: `AddBaseMap`, `AddBaseItemTemplate`, `CompileShipped()` | `Goose.IntegrationTests/Fixtures/GlobalScriptFixture.cs` |
| Dimension templates sit at `50 + 100000 * dim`; tier 0.5 at min level 50 | `Goose.IntegrationTests/DimensionModifierTests.cs:12-15,158-163` |
| Registered abyss suffixes: `of Vita Regen` = 900000, `of Speed` = 900005 | `Goose.IntegrationTests/DimensionModifierTests.cs:31-32` |
| `ApplyStats` runs the modifier script, which calls `RefreshStats` | `Goose/ItemHandler` → `ItemModifier.ApplyStats` (`Goose/ItemModifier.cs:68-78`), `DimensionSurname.csx:51` |

Test commands (from `docs/testing.md`):

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Baseline on this worktree (2026-09-19, before any change): fast **899 passed**, integration
**283 passed**, 0 failed. `docs/testing.md:71-73` still says 321/219 — stale, not this
plan's problem.

---

### Task 1: Put the payload on `P.ItemSlot`

**Files:**
- Modify: `Goose/Packets.cs` (add a private helper near the top of `P`; append one field in `ItemSlot`, `:480-484`)
- Create: `Goose.Tests/PacketExtraStatsTests.cs`
- Modify: `Goose.Tests/PacketCurrencyTests.cs:36` (helper) and `:46,58,71,85,97,112-113`
- Modify: `Goose.Tests/DoubleBehaviorTests.cs:107`

**Mutation impact:**
- Source of truth changed: none. The field is derived at send time from
  `item.TotalStats` (`Goose/Packets.cs:453-465`), the same value the AC and HP lines
  already read. `RefreshStats` (`Goose/Item.cs:248-258`) remains the only writer.
- Important readers: the client tooltip
  (`Goose2ClientGodot/Scripts/UI/ItemTooltipText.cs:34`) via `SIS`; bank and combine bag
  inherit the same layout client-side (`BankSlotPacket`/`CombineBagSlotPacket` override
  only `Prefix`).
- Derived/cached state affected: no derived state found. Nothing stores or caches the
  payload.
- Required propagation sequence: none beyond the existing send. A stat changed by
  `ItemModifier.ApplyStats` is already reflected because every modifier script calls
  `item.RefreshStats()` before the next send (`DimensionSurname.csx:51`,
  `ItemModifierScript.csx:39-51`).
- Invariants to preserve:
  - every field the client already parses keeps its position; `ExtraStats` is appended
    after the currency name, never inserted;
  - an item with no extra stats adds one empty field and no visible change;
  - the currency name stays in its existing field.
- Observable proof required: packet-level assertions on the final string, including a
  regression test that the currency field did not move.

**Step 1: Write the failing tests**

Create `Goose.Tests/PacketExtraStatsTests.cs`. Reuse the `VendorFixture` shape from
`PacketCurrencyTests.cs:24-36` (template with `BaseStats = new AttributeSet()`,
`fixture.Carry(template)`).

Tests to write:

- `ItemSlot_sends_nothing_extra_for_an_ordinary_item` — last field is `""` and the packet
  ends with `|`.
- `ItemSlot_appends_after_the_currency` (adversarial) — an item with a haste stat still
  has `"gold"` at `fields[^2]`; fails on any implementation that inserts the field or
  reorders the layout.
- `ItemSlot_reports_basis_points_in_the_documented_order` — set every one of the twelve
  stats on `item.BaseStats`, `item.RefreshStats()`, assert the exact last field.
- `ItemSlot_trims_trailing_zeros_and_keeps_leading_ones` — spell damage only →
  `"0,800"`; haste only → `"400"`.
- `ItemSlot_reports_total_stats_scaled_by_the_multiplier` — `StatMultiplier = 1.1` and
  `SpellDamage = 0.04` → `"0,440"`, pinning that the field follows `TotalStats` and not
  `BaseStats` (`AttributeSet.cs:196-201`).
- `ItemSlot_reports_both_halves_of_a_regen_stat` — `HPPercentRegen = 0.015`,
  `HPStaticRegen = 1500` → `"0,0,0,0,0,0,150,1500"`.

Use `AttributeSet` fields directly plus `RefreshStats()`; do not build the payload by hand,
the assertion is on the packet.

**Step 2: Run the tests to verify they fail (red)**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~PacketExtraStats`
Expected: FAIL — the last field is the currency name (`"gold"`), not the payload.

**Step 3: Implement the payload**

In `Goose/Packets.cs`, inside `P`, add one private helper and one `.NET` array literal.
Percentages convert exact-then-round, not `Math.Round(value * 10000)`, because a double
product such as `0.04 * 3 * 0.5` is `0.060000000000000005` and truncating an inexact
250-equivalent would drop a basis point:

```csharp
private static int ExtraStatPercent(double value) =>
    (int)Math.Round(Utils.ExactProduct(value, 10000), MidpointRounding.AwayFromZero);

// Order is append-only and mirrored by the client (Goose2ClientGodot ItemStats.cs /
// ItemTooltipText.Build): haste, spellDamage, spellCrit, meleeDamage, meleeCrit,
// damageReduction, hpPercentRegen, hpStaticRegen, mpPercentRegen, mpStaticRegen,
// spPercentRegen, spStaticRegen.
private static string ExtraStatsPayload(AttributeSet stats)
{
    int[] values =
    [
        ExtraStatPercent(stats.Haste),
        ExtraStatPercent(stats.SpellDamage),
        ExtraStatPercent(stats.SpellCrit),
        ExtraStatPercent(stats.MeleeDamage),
        ExtraStatPercent(stats.MeleeCrit),
        ExtraStatPercent(stats.DamageReduction),
        ExtraStatPercent(stats.HPPercentRegen),
        stats.HPStaticRegen,
        ExtraStatPercent(stats.MPPercentRegen),
        stats.MPStaticRegen,
        ExtraStatPercent(stats.SPPercentRegen),
        stats.SPStaticRegen,
    ];

    int last = Array.FindLastIndex(values, value => value != 0);
    return last < 0 ? "" : string.Join(",", values, 0, last + 1);
}
```

Then append it in `P.ItemSlot` (`Goose/Packets.cs:480-484`), after the currency name:

```csharp
                    world.CurrencyHandler.Resolve(item.Template, null).Name + "|" +
                    ExtraStatsPayload(item.TotalStats);
```

Do not touch `P.VendorItemSlot` in this task — Task 2 owns it.

**Step 4: Re-point the position assertions this breaks**

Three end-indexed assertions and one helper move by exactly one field:

- `Goose.Tests/PacketCurrencyTests.cs:36` — the helper is documented as "the name is the
  last field"; it now reads the payload. Change it to read the second-to-last field
  (`packet.Split('|')[^2]`) and update its comment to say the extra-stats payload is
  appended after the name. All five call sites (`:46,58,71,85,97`) then keep their
  expected values unchanged.
- `Goose.Tests/PacketCurrencyTests.cs:112-113` — `fields[^2]` → `fields[^3]` (GraphicA),
  `fields[^1]` → `fields[^2]` (currency).
- `Goose.Tests/DoubleBehaviorTests.cs:107` — `fields[^9]` → `fields[^10]`
  (`spellEffectChance`). The test's comment names it as the ninth field from the end;
  update the comment too.

Do not delete these tests: they are what proves the append-only rule.

**Step 5: Run to verify green**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore`
Expected: PASS, 899 baseline tests adjusted and all green, plus the new ones.

**Step 6: Commit**

```bash
git add Goose/Packets.cs Goose.Tests/PacketExtraStatsTests.cs \
        Goose.Tests/PacketCurrencyTests.cs Goose.Tests/DoubleBehaviorTests.cs
git commit -m "Send the item's extra stats on the item slot packet"
```

---

### Task 2: Same payload on the vendor packet

**Files:**
- Modify: `Goose/Packets.cs:531-540` (`VendorItemSlot` tail)
- Test: `Goose.Tests/PacketExtraStatsTests.cs` (add one test)

**Mutation impact:**
- Source of truth changed: none. Vendor stock is an `ItemTemplate`, so this reads
  `item.BaseStats` — the field the neighbouring lines already use
  (`Goose/Packets.cs:513-525`). Vendor items carry no rolled modifiers, so the payload is
  normally empty.
- Important readers: the vendor tooltip via `SVS` (`Goose2ClientGodot` `VendorSlotPacket`).
- Derived/cached state affected: no derived state found.
- Required propagation sequence: none.
- Invariants to preserve: the vendor layout stays field-for-field identical to the
  inventory layout up to the appended payload.
- Observable proof required: a stocked template with a haste stat produces `"400"` in the
  last field, and the currency still arrives in the field before it.

**Step 1: Write the failing test**

`VendorItemSlot_reports_template_stats` in `Goose.Tests/PacketExtraStatsTests.cs`: build a
template with `BaseStats.Haste = 0.04`, call
`P.VendorItemSlot(template, fixture.World, fixture.Vendor, 1, 1)`
(`VendorFixture.Vendor` is public), assert the last field is `"400"` and `fields[^2]` is
the currency.

**Step 2: Run to verify it fails (red)**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~VendorItemSlot_reports_template_stats`
Expected: FAIL — the last field is the currency name.

**Step 3: Implement**

Append the same helper after the vendor currency name (`Goose/Packets.cs:540`):

```csharp
                    world.CurrencyHandler.Resolve(item, vendor).Name + "|" +
                    ExtraStatsPayload(item.BaseStats);
```

**Step 4: Run to verify green**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore`
Expected: PASS. `PacketCurrencyTests` vendor assertions stay green because they read the
second-to-last field.

**Step 5: Commit**

```bash
git add Goose/Packets.cs Goose.Tests/PacketExtraStatsTests.cs
git commit -m "Send the extra stats on the vendor slot packet too"
```

---

### Task 3: Record the rule where the last trailing field was recorded

**Files:**
- Modify: `docs/dimensions.md:253-255`

**Step 1: Update the watch-out**

The line currently reads that the item and vendor slot packets carry a trailing currency
name and that clients which stop parsing at `GraphicA` ignore the field. Extend it to name
both trailing fields, in order: currency, then the extra-stats payload. Keep it to two or
three lines in the same voice; do not restructure the section.

**Step 2: Verify no other doc claims the currency is last**

Run: `grep -rn "trailing currency\|last field" docs/ Goose/ Goose.Tests/`
Expected: only `docs/dimensions.md` and the updated test comments.

**Step 3: Commit**

```bash
git add docs/dimensions.md
git commit -m "Note the extra stats field beside the trailing currency name"
```

---

### Task 4: Prove a real abyss suffix reaches the packet

**Files:**
- Create: `Goose.IntegrationTests/DimensionTooltipStatsTests.cs`
- Reference: `Goose.IntegrationTests/DimensionModifierTests.cs:8-16,158-173`

**Mutation impact:**
- Source of truth changed: none. The test applies a registered modifier to a real
  dimension item, which mutates `item.BaseStats` and calls `RefreshStats`
  (`DimensionSurname.csx:51`); the packet then reads `TotalStats`.
- Important readers: the client tooltip, through `SIS`.
- Derived/cached state affected: `Item.TotalStats` is recomputed by `RefreshStats`.
- Required propagation sequence: `GetSurname(id).ApplyStats(item, world)` →
  script writes `BaseStats` → `item.RefreshStats()` → `P.ItemSlot` reads `TotalStats`.
- Invariants to preserve: the payload is the item's *whole* contribution — the dimension
  template's own scaled stats plus what the modifier added — not the modifier's delta
  alone.
- Observable proof required: the payload of an item carrying a real suffix, asserted
  through `P.ItemSlot`, for both a single-stat suffix and the two-part regen suffix.

**Step 1: Write the test**

Mirror `DimensionModifierTests.Run()` and its `ItemOfDimension` helper, with one change:
give the base template non-zero stats, because the dimension bake scales the *base*
template's stats (`Items.csx:241-279`) and a zero base makes template and modifier
contributions indistinguishable.

```csharp
fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon, t =>
{
    t.MinLevel = 50;              // Tier -> 0.5 (DimensionHelpers.csx:41-48)
    t.BaseStats.Haste = 0.02;
    t.BaseStats.HPStaticRegen = 100;
});
fixture.CompileShipped().Object.OnLoaded(fixture.World);
```

Applying a suffix through `ApplyStats` rather than rolling is deliberate: these surnames
are registered with `Chance = 0` and the dimension roll is random
(`DimensionModifierTests.cs:23-32`), so `ApplyStats` is the deterministic path the existing
suite already uses.

Cases, at dim 3 (`half = 0.5 * 3 = 1.5`, `tier = 0.5`):

- `of Speed` (900005): the clone's template has `Haste = 0.02 * 1.5 = 0.03`; the suffix
  adds `0.04 * 3 * 0.5 = 0.06`; total `0.09` → last field `"900"`.
- `of Vita Regen` (900000): template `HPStaticRegen = (int)(100 * 1.5) = 150`; the suffix
  adds `0.015 * 1.5 = 0.0225` percent and `(int)(1500 * 3 * 0.5) = 2250` flat → totals
  `0.0225` and `2400` → last field `"0,0,0,0,0,0,225,2400"`.

Assert on `P.ItemSlot(item, fixture.World, 1, 1).Split('|')`. `0.03 + 0.06` is not exact in
binary — the round-trip value is `0.089999999999999997` or `0.09000000000000001` — so this
test is also what pins `ExtraStatPercent`'s round rather than a truncation, which would
report `899`.

**Step 2: Prove the test is load-bearing (mutation check)**

After Task 1 is in place the test passes. Confirm it can fail: temporarily change
`ExtraStatsPayload(item.TotalStats)` to `ExtraStatsPayload(item.BaseStats)` in
`P.ItemSlot` and re-run. Expected: FAIL — `of Speed` reads `"600"` and `of Vita Regen`
reads `"0,0,0,0,0,0,225,2250"`, because the template's own contribution is missing.
Revert the mutation; do not commit it.

Before Task 1 lands, the same test fails with the last field reading `"gold"`, which is
the ordinary red state.

**Step 3: Run to verify green**

Run: `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore`
Expected: PASS, 283 baseline plus the new tests, 34 s or so.

**Step 4: Commit**

```bash
git add Goose.IntegrationTests/DimensionTooltipStatsTests.cs
git commit -m "Cover a real abyss suffix reaching the tooltip payload"
```

---

## Invariant-to-test matrix

| Invariant | Proved by |
| --- | --- |
| The payload is appended, never inserted; the currency keeps its field | `PacketExtraStatsTests.ItemSlot_appends_after_the_currency`, re-pointed `PacketCurrencyTests.ItemSlot_KeepsGraphicAInItsExistingField` |
| An item with no extra stats is unchanged on the wire | `PacketExtraStatsTests.ItemSlot_sends_nothing_extra_for_an_ordinary_item` |
| Basis points, in the documented order, trailing zeros trimmed | `ItemSlot_reports_basis_points_in_the_documented_order`, `ItemSlot_trims_trailing_zeros_and_keeps_leading_ones` |
| The payload follows `TotalStats`, so `StatMultiplier` is included | `ItemSlot_reports_total_stats_scaled_by_the_multiplier` |
| Both halves of a regen stat are reported | `ItemSlot_reports_both_halves_of_a_regen_stat` |
| Vendor payload comes from template `BaseStats` | `VendorItemSlot_reports_template_stats` |
| A real registered suffix reaches the packet | `DimensionTooltipStatsTests.Dimension_item_reports_the_suffix_and_the_template_together` (of Speed → `900`) |
| The payload is template + modifier, not the modifier alone | same test; fails with `600` / `...225,2250` under the `BaseStats` mutation |
| Cumulative double error does not cost a basis point | same test: `0.03 + 0.06` reports `900`, not `899` |

## Design alignment

- Field name `ExtraStats`, one field, appended after the currency name in both emit sites:
  matches the design's Protocol section.
- Twelve values in the order printed in the helper comment: matches the design's table.
- `Utils.ExactProduct` with a round, rather than the design's shorthand "convert with
  `ExactProduct`… cast to `int`": rounding the exact product is the same conversion for
  every value in the design's table and additionally survives cumulative double error.
  Below 0.5 basis points still becomes 0 and the line is still omitted.
- Task 4's fixture uses non-zero base stats, which the design did not specify. Without
  them, `Dimensions.csx` bakes `base * half` of zero and the template's contribution is
  invisible, so a `BaseStats`-only bug would pass the test.
- Failure mode, decided rather than guarded: `Utils.ExactProduct` runs the value through
  `decimal.Parse`, which throws on NaN or Infinity, and that would break the packet send
  rather than just the display. No guard is added — item stats are only written by scripts
  (template stats come from integer database columns, `Goose/ItemHandler.cs:75-88`), and
  the same helper already runs on these values in combat (`Goose/SpellEffect.cs:630`). A
  review that wants belt and braces adds `if (!double.IsFinite(value)) return 0;` to
  `ExtraStatPercent`.
- Trimming: trailing zeros only, so `0,800` keeps its leading zero. Matches the design.
- Illutia only: no edit to `Goose/Data/Aspereta/Scripts/Global/Aspereta.csx:440-515`.
- Deliberately not done here: Move Speed, ground items, NPC equipment, the client side.

## Verification before handing off

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
grep -rn "ExtraStatsPayload" Goose/ | wc -l   # 3: helper, two call sites
```

The server side is shippable on its own: an old client ignores a trailing field
(`ReflectedIllutiaClient/InvItem.cs:55-101` reads a fixed 43 fields), so nothing regresses
before the client plan lands.
