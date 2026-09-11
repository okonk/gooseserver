# Decimal → Double Implementation Plan

**Goal:** Replace every C# `decimal` and every SQLite `DECIMAL(p,s)` column type with `double` / `REAL` across the server, the CsvToSql data-import tooling, and the Google Sheets data editor, so the in-memory numeric type matches what SQLite already stores.

**Architecture:** SQLite has no decimal type — a `DECIMAL(9,4)` column carries NUMERIC affinity and is already stored as `REAL` (IEEE-754 double) or `INTEGER`. The C# `decimal` is therefore a pure in-memory choice; switching to `double` makes it match storage and deletes the `double → decimal → double` conversion noise. The change is **value-preserving for stored values** (they are already doubles on disk) and **mechanical in C#** (type, literal, and cast renames). It is **not** behavior-preserving in two observable ways, both small and both deliberate:

1. **Binary-float precision (the `(decimal)` quantizer).** The `(decimal)` casts in `AttributeSet.operator*` (`Goose/AttributeSet.cs:173`) and the `.csx` dimension scripts are an *active quantizer*, not noise: they move the arithmetic from binary `double` to exact decimal, so a `Math.Ceiling`/`Math.Floor`/`(long)` near an integer boundary can shift by 1 once they are dropped. Verified concretely: `25 * (decimal)0.28 = 7.0` → `7`, but `25 * 0.28 = 7.000000000000001` → `8`.
2. **Midpoint rounding.** `decimal.ToString("F0")` rounds half **away from zero**; `double.ToString("F0")` rounds half **to even**. A half-valued percentage (`10.5`) displays as `11` today and `10` after (`0.5`→`1`→`0`, `2.5`→`3`→`2`, `15.5`→`16`→`16`). `Math.Round` differs the same way (`Math.Round(2.135m,2)=2.14` vs `Math.Round(2.135d,2)=2.13`), which affects the aether-threshold message (`Goose/Player.cs:2198`). **This plan accepts the flip and pins it** (Task 5 asserts the new double values). The alternative — preserving today's `11`/`3` exactly by rounding `value * 100` with `MidpointRounding.AwayFromZero` before formatting in `GetPercentageDescription`/the snare branch (and the same for the aether message) — is deliberately **not** taken, but is the option to choose if behavior-preservation outweighs the flip; it would shrink the Task 5 midpoint test to a no-change assertion and drop the client spot-check item.

Three output paths are observable to a player or in the `.db`: the `:F0` spell/item descriptions (`Goose/SpellEffect.cs:314-318,457-458`), the string-interpolated SQL save text (no parameter binding — see Persistence), and `ToString()` in broadcast messages. The `int`-cast wire values (`Goose/Packets.cs:430,490`) and the `int` move speed (`Goose/Player.cs:1605`) are genuinely unchanged, because truncation is identical for both types.

**Tech Stack:** .NET 10 / C#, `System.Data.SQLite.Core`, Roslyn (runtime-compiled `.csx` scripts), CsvToSql (C# DDL generator), Google Apps Script + JS data editor.

**Revised 2026-09-02** to incorporate the findings in `review2.md` (an **external** review of this plan, provided as input and **not vendored in the repo**): the `:F0`/`Math.Round` midpoint delta and the `(decimal)` quantizer are now named as real behavior changes (was: "invisible in practice"); the `Goose.IntegrationTests`-not-in-`Goose.sln` hole is fixed (Task 0); real baselines are recorded, and the 6 pre-existing failures found at review time (stale command-help tests, `see_invisible`/quest-script DataEditor layout drift) have been fixed so every gate is plain green; the binder overflow case, the SQL-text safety, `PropertiesDictionary.cs`, the `DECIMAL(5, 2)` space variant, and the ~20 unlisted decimal-literal test files are all addressed; and the DataEditor's lost `DECIMAL(p,s)` guardrail is replaced (Task 3/4).

---

## Why this is safe (verified against the code)

- **Storage is already double.** `DECIMAL(p,s)` in SQLite → NUMERIC affinity → stored as `REAL`/`INTEGER`. Readers use `Convert.ToDouble` / `GetDouble`. **No data migration is needed.**
- **Schema DDL applies to fresh DBs only.** `GameWorld.CreateDatabaseSchema` (`Goose/GameWorld.cs:131-153`) runs only when the `.db` is missing (`createNew`, `:206-214`). `MigrateDatabaseSchema` (`:164-170`) runs every start but only calls `AddColumnIfMissing` — it never changes a column's type. So the `DECIMAL`→`REAL` DDL edit affects fresh DBs only; existing DBs keep their (already REAL-backed) columns. **No migration required.**
- **`onetimeupdates.sql` is dead.** It is not in the schema list (`GameWorld.cs:135-140`) and is SQL-Server syntax (`USE`, `CLUSTERED`, `DATETIME2`, `[dbo]`). Its `DECIMAL(18,4)` at `:35` is never applied. Leave it untouched.
- **The `int`-cast wire values are unchanged.** Move speed is `int` (`Player.CalculateMoveSpeed`, `Goose/Player.cs:1605`); spell-effect chance is `int`-cast (`(int)item.SpellEffectChance`, `Goose/Packets.cs:430,490`) — truncation is identical for `decimal` and `double`. Chance rolls already take `double` (`GameWorld.RollChance`, `Goose/GameWorld.cs:687`). `Item.StatMultiplier` is already `double` (`Goose/Item.cs:68`) and `ItemModifier.Chance` is already read via `GetDouble` (`Goose/ItemModifier.cs:38`).
- **The observable deltas are the `:F0`/`Math.Round` paths only** (named in the Architecture section): `Goose/SpellEffect.cs:314-318,457-458` and `Goose/Player.cs:2198`. These are pinned by Task 5, not assumed invisible.

## APIs verified (citations)

| API | Location |
|---|---|
| `GetDouble(this DbDataReader, string)` — exists | `Goose/DataReaderExtensions.cs:19` |
| `GetDecimal(this DbDataReader, string)` — remove | `Goose/DataReaderExtensions.cs:16-17` |
| `CommandBinder` numeric style `NumberStyles.Float & ~AllowThousands`, `InvariantCulture` | `Goose/Commands/CommandBinder.cs:60` |
| `CommandBinder` double branch (with `double.IsFinite`) — exists | `Goose/Commands/CommandBinder.cs:68` |
| `CommandBinder` decimal branch — remove | `Goose/Commands/CommandBinder.cs:70` |
| `CommandBinder` guard `!= typeof(decimal)` — remove term | `Goose/Commands/CommandBinder.cs:147-149` |
| `PropertiesDictionary.IsNumericType` `typeof(decimal)` term — remove (dead) | `Goose/PropertiesDictionary.cs:133` (stale comment `:90`) |
| `CreateDatabaseSchema` (fresh-only) | `Goose/GameWorld.cs:131-153` |
| `MigrateDatabaseSchema` (every start, add-column only) | `Goose/GameWorld.cs:164-170` |
| `SqlType.Decimal94/92/52/54` — replace with `Real` | `CsvToSql/CsvToSql.Core/Schema/SqlType.cs:17-20` |
| `ColumnKind { …, Decimal, … }` — rename to `Double` | `CsvToSql/CsvToSql.Core/Schema/Column.cs:7` |
| `Col.Decimal(…)` — rename to `Col.Double` | `CsvToSql/CsvToSql.Core/Schema/Col.cs:15-16` |
| DataEditor `decimalSpec` / `decimalMax` / DECIMAL block — remove | `tools/DataEditor/src/validation.js:27-39, 112-137` |
| DataEditor `RANGES` keyed by SQL type name (no `REAL`) | `tools/DataEditor/src/validation.js:6-15` |
| DataEditor numeric regex — keep (already accepts plain decimals) | `tools/DataEditor/src/validation.js:92` |
| DataEditor kind rendering — only `Enum`/`Bool` special-cased | `tools/DataEditor/src/forms.js:169,180,182-186` |
| `RollChance(double)` | `Goose/GameWorld.cs:687` |
| chance roll `item.SpellEffectChance * 1000` | `Goose/Inventory.cs:424` |
| wire int-cast `(int)item.SpellEffectChance` | `Goose/Packets.cs:430,490` |
| snare calc `(decimal)100.0` | `Goose/NPC.cs:749-750, 1396-1397` |
| damage calc (already `double`) | `Goose/Player.cs:1745-1763` |
| `:F0` main formatter `GetPercentageDescription` | `Goose/SpellEffect.cs:314-318` |
| `:F0` snare branch | `Goose/SpellEffect.cs:457-458` |
| `Math.Round(wait, 2)` aether message | `Goose/Player.cs:2198` |
| compiled-script cache (rebuilt per path on new build) | `Goose/Scripting/ScriptHandler.cs` (`scripts` dict, `GetScript<T>`); the comment at `SpellEffect.cs:237` only points at it |
| `GetDecimal` real call sites | `Goose/Player.cs:809`, `Goose/Pet.cs:283,285` |
| SQL save text string-interpolates the value (no parameters) | `Goose/Player.cs:1083,1166`, `Goose/Pet.cs:362,364,368,370,431,433,437,439` |
| `SetConfigCommand` reflection `Parse` path | `Goose/Commands/SetConfigCommand.cs:39-41` |
| integer division (keep truncation) | `Goose/Commands/BuyVitaCommand.cs:26`, `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs:35` |
| exp-modifier message `world.ExperienceModifier + "x"` | `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs:44` |

## Persistence strategy

- **Fresh DB:** new DDL emits `REAL` columns → created as `REAL`.
- **Existing DB:** schema files are not re-run; columns keep their `DECIMAL` declaration (cosmetic — SQLite ignores the type name) and their REAL-backed values. **No migration, no data change.**
- **`updatesql`:** re-imports data via CsvToSql into the same REAL-backed columns. Unaffected.
- **Chosen strategy: no migration needed.** The DDL edit is for fresh-DB correctness and tooling consistency only. Do **not** add an `ALTER ... ALTER COLUMN` migration — SQLite cannot change a column's declared type in place, and it would be a no-op anyway.
- **SQL text, not parameters.** The save statements build SQL by string-concatenating the numeric fields directly (`Player.cs:1083,1166`; `Pet.cs:362,364,368,370,431,433,437,439`) — there is **no parameter binding** on this path. `decimal.ToString()` can only emit plain digits **in a dot-locale** (the interpolation is `CurrentCulture`-dependent for *both* types — a comma-decimal locale renders `0.5` as `0,5` into the SQL today, too; a pre-existing latent issue, not a regression, but the "plain digits" claim is dot-locale-dependent). `double.ToString()` additionally emits scientific notation (`1E-07`, `1E+21`), `NaN`, and `∞`. SQLite's tokenizer accepts scientific notation (so the `.db` text dump may change shape), but **`NaN` and `∞` are not SQL numeric literals** — interpolating one makes the entire save statement invalid SQL. That failure mode is structurally impossible today and structurally possible after.
- **Scope: non-finite persisted values are out of scope — with one reachable path now guarded.** No reachable path today produces a non-finite persisted value. The command binder's `double.IsFinite` guard (`CommandBinder.cs:68`) rejects infinite command arguments, **but `/setconfig` bypasses the binder** — it parses by reflection (`SetConfigCommand.cs:39-41`, `double.Parse` with default styles), which accepts `NaN` and `1e400`→`∞` (verified; the literal `Infinity` is rejected by default `NumberStyles`, so it is *not* a reachable token). `decimal.Parse` rejects all three. So Task 1 adds a 3-line `double.IsFinite` guard to `SetConfigCommand` and Task 5 pins the rejection. The change also *widens the space of upstream bugs that could produce one* (`double` divide-by-zero yields `∞`/`NaN` where `decimal` threw `DivideByZeroException`); Task 5 pins the save-statement **numeric** text for finite values (see Task 5 SQL-text safety test — it asserts the interpolated value matches `^-?\d+(\.\d+)?$`, not the whole statement).

## Mutation impact (core domain field types)

- **Source of truth:** the in-memory domain objects (`AttributeSet`, `Spell`, `SpellEffect`, `Class`, `Player`, `NPC`/`NPCTemplate`, `Pet`, `Item`/`ItemTemplate`, `NPCDropInfo`, `GameWorld`, `GooseSettings`), loaded from SQLite at startup by the `*Handler` classes and saved back; the SQLite columns are the durable form.
- **Important readers:**
  - Persistence: `SpellHandler.cs`, `NPCHandler.cs`, `ItemHandler.cs`, `ClassHandler.cs` (load via `Decimal.Parse(reader.GetString(…))`); `Player.cs:809` and `Pet.cs:283,285` (load via `GetDecimal`); the save paths.
  - Client packets: `Packets.cs` (move speed `int`; spell-effect chance `int`-cast at `:430`/`:490`; HP/MP % `int`).
  - Scripts: `Goose/Data/{Illutia,Aspereta}/Scripts/**/*.csx` (runtime-compiled, cached per path).
  - Commands: `CommandBinder.cs`; `SetConfigCommand.cs` (reflection `Parse` — the parse itself works for both types, but a `double.IsFinite` guard is **added** because `/setconfig` bypasses the binder and `double.Parse` accepts `NaN`/`1e400`→`∞` where `decimal.Parse` threw; user-facing input handling also changes because `decimal.Parse`/`double.Parse` accept different token sets).
  - Derived calcs: damage (`Player.cs:1745-1763`, already `double`), snare (`NPC.cs:743-750,1390-1397`), chance rolls (`GameWorld.cs:687`, `Inventory.cs:424`), displayed percentages (`SpellEffect.cs:314-318,457-458`).
- **SQL-text interpolation (no parameters):** `Player.cs:1083,1166` and `Pet.cs:362,364,368,370,431,433,437,439` concatenate the numeric fields into SQL text. After the switch these are `double`, whose `ToString()` can emit `E`-notation/`NaN`/`∞` — see Persistence strategy. Task 5 pins this.
- **Integer division that must NOT be "fixed":** `BuyVitaCommand.cs:26` (`BaseStats.HP / IncreaseVitaBuyAmount`, `long / int`) and `PlayerCountExperienceModifierUpdateEvent.cs:35` (`Count / Interval`, `int / int`) are integer division. The type rename does **not** change the operand types, so the truncation stays. Do not add `.0` — that would silently change vita prices and the experience modifier. Related: `VitaCost * buyrate` moves from exact decimal to binary, so the `(long)` truncation can differ by 1 in rare cases — Task 5 adds one economy test.
- **Culture safety improves:** the `Decimal.Parse(reader.GetString(col))` sites (15 in `SpellHandler.cs`, 5 in `NPCHandler.cs`, 9 in `ClassHandler.cs`, 1 in `ItemHandler.cs`) use `CurrentCulture`; `reader.GetDouble(col)` → `Convert.ToDouble(object)` is culture-free for a boxed numeric. The repo has been bitten by this (`DimensionModifierTests.Rarity_multiplier_parses_invariant_under_any_culture`, `:81-103`). Task 5 extends that culture test to a former-`Decimal.Parse` column.
- **`!= 0` guards become binary-exact comparisons:** `Player.cs:2312,2501` (`Stats.Haste != Decimal.Zero`), `NPC.cs:410,1388` (`<= Decimal.Zero`), `NPC.cs:741` (`> Decimal.Zero`). IEEE guarantees `x - x == 0`, so equal-operand cancellation is still exact; the guard semantics do change for multi-term chains. Audit rather than assume.
- **Message text formatting:** `PlayerCountExperienceModifierUpdateEvent.cs:44` broadcasts `"...is now " + world.ExperienceModifier + "x"`. `decimal` preserves trailing zeros; `double` does not — `"0.10x"` becomes `"0.1x"`. Cosmetic but wire-visible.
- **Derived/cached state:** damage/regen/chance/speed are computed on the fly from the fields (no separate cache). The only cache is the compiled-script cache (`Goose/Scripting/ScriptHandler.cs`), invalidated by a new build. **No other derived state found.**
- **Propagation sequence:** the change is compile-time; stored values are unchanged. Runtime flow is load (`REAL`→`double`) → compute (`double`) → save (`double`→`REAL`) → packet (`double`→`int`/`:F0`). No runtime "refresh" is required because no stored value changes — only the in-memory type. Scripts recompile at load under the new build.
- **Invariants to preserve:**
  - Persisted values round-trip: read `REAL`→`double`, save `double`→`REAL`, reload equals the original.
  - Client DTO matches server state: the `int`-cast / `:F0` of the `double` equals what the client expects.
  - No value drift: a field's numeric value is identical before/after (same magnitude, different representation).
  - Save-statement **numeric** text stays plain digits for finite values (the interpolated value matches `^-?\d+(\.\d+)?$` — no exponent/`NaN`/`∞`).
- **Observable proof required:** the integration round-trip test (Task 1) and the adversarial precision tests (Task 5).

---

## Transformation cheat-sheet (C# and `.csx`)

| From | To |
|---|---|
| `decimal` / `Decimal` | `double` |
| `Decimal.Zero` | `0` |
| `Decimal.Parse(x)` | `double.Parse(x)` (or `reader.GetDouble(col)`) |
| `1.5m`, `1m`, `0.5m`, `(decimal).2` | `1.5`, `1.0`, `0.5`, `0.2` |
| `(decimal)x` | `(double)x` (or drop the cast) — **but** `(decimal)` is an active quantizer, not a no-op: it moves the arithmetic from binary `double` to exact decimal, so a `Math.Ceiling`/`Math.Floor`/`(long)` near an integer boundary can shift by 1 (e.g. `25 * (decimal)0.28 = 7.0` → `7`, but `25 * 0.28 = 7.000000000000001` → `8`). Pin the affected ladders in Task 5. |
| `decimal?` | `double?` |
| `DECIMAL(9,4)` / `(9,2)` / `(5,2)` / `(5, 2)`[space] / `(5,4)` | `REAL`. Note: `(5, 2)` has a space (`paypal.sql:9`) — match it with a regex, not a literal find-replace. `(5,4)` has **no live `.sql` occurrence** — it exists only in the generated `schema.js`/`generated.snapshot` via `SqlType.Decimal54`. |

---

## Tasks

### Task 0: Baseline + isolated worktree

**Files:** `Goose.sln` (add `Goose.IntegrationTests`).

**Step 0: Add `Goose.IntegrationTests` to `Goose.sln`.** It is currently **absent** from the solution, so `dotnet build Goose.sln` and a root `dotnet test` never compile or run it — a compile error in an integration test is invisible until an explicit `dotnet test Goose.IntegrationTests`. This is a latent hole in the repo, not just this plan. Run `dotnet sln Goose.sln add Goose.IntegrationTests/Goose.IntegrationTests.csproj`. After this, a root `dotnet test` runs unit + tools + integration.

**Step 1: Create an isolated worktree** (see @using-git-worktrees) so the refactor does not share a working tree with other work.

**Step 2: Record the real baselines (the suites are green).** The 6 failures recorded in an earlier revision of this plan (3 stale command-help integration tests — fallout from `77c65ae` "Help window rework" — and 3 `see_invisible`/quest-script DataEditor layout-drift tests) have since been fixed in `d3892ca` ("fix: stale help-text integration tests; update DataEditor for schema drift"), so every gate below is plain green and a root `dotnet test` (after Task 0 Step 0 wires in the integration suite) is **not** permanently red. Verified at planning time: `Goose.IntegrationTests` 255/0, DataEditor JS 1097/0.

- `dotnet build Goose.sln` → 0 warnings, 0 errors
- `dotnet test Goose.Tests` → 695 passed, 0 failed
- `dotnet test tools/Tools.Tests` → 150 passed, 0 failed
- `dotnet test Goose.IntegrationTests` → 255 passed, 0 failed
- `node --test "tools/DataEditor/test/*.test.js"` → 1097 passed, 0 failed

**Quote the JS command exactly** — pass the **glob**, not the bare directory (`tools/README.md:152`: a bare directory fails with `MODULE_NOT_FOUND` on Node 22). Re-run and confirm green; a new failure is yours.

**Step 3: No commit** (baseline only).

---

### Task 1: C# type change (domain + handlers + commands + binder) + coupled test updates

This is one compile-coupled unit: changing a domain field to `double` breaks every `decimal` assignment to it, so the domain, the readers, the commands, and the binder must land together for the build to go green. The `.csx` scripts are **not** part of this unit (they are Roslyn-compiled at runtime, not at build time) — they are Task 2.

**Files:**
- Modify (domain): `Goose/AttributeSet.cs`, `Goose/Spell.cs`, `Goose/SpellEffect.cs`, `Goose/Class.cs`, `Goose/Player.cs`, `Goose/NPC.cs`, `Goose/NPCTemplate.cs`, `Goose/Pet.cs`, `Goose/Item.cs`, `Goose/ItemTemplate.cs`, `Goose/IItem.cs`, `Goose/NPCDropInfo.cs`, `Goose/GameWorld.cs`, `Goose/GooseSettings.cs`, `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs`, `Goose/PropertiesDictionary.cs`
- Modify (readers): `Goose/SpellHandler.cs`, `Goose/NPCHandler.cs`, `Goose/ItemHandler.cs`, `Goose/ClassHandler.cs`, `Goose/DataReaderExtensions.cs`
- Modify (commands + binder): `Goose/Commands/ChangeClassCommand.cs`, `Goose/Commands/AetherCommand.cs`, `Goose/Commands/BuyVitaCommand.cs`, `Goose/Commands/BuyManaCommand.cs`, `Goose/Commands/PetVitaCommand.cs`, `Goose/Commands/PetDamageCommand.cs`, `Goose/Commands/PetSpawnCommand.cs`, `Goose/Commands/SetConfigCommand.cs`, `Goose/Commands/CommandBinder.cs`
- Modify: `Goose/Commands/SetConfigCommand.cs` — binds setting values by reflection (`getter.ReturnType.GetMethod("Parse")`); add a `double.IsFinite` guard on the parsed value because `/setconfig` bypasses the binder and `double.Parse` accepts `NaN`/`1e400`→`∞` (Task 1 Step 3b). Pinned by a Task 5 test.
- Modify (coupled tests): `Goose.Tests/SpellCloneTests.cs`, `Goose.Tests/DataReaderExtensionsTests.cs`, `Goose.Tests/CommandBinderTests.cs`, `Goose.Tests/Part3GmATests.cs`, `Goose.IntegrationTests/DimensionModifierTests.cs`, plus the decimal-literal sweep list in Step 4.

**Mutation impact:** see the "Mutation impact" section above. This task is the source-of-truth change; its observable proof is the round-trip test in Step 6.

**Step 1: Domain types**

Apply the cheat-sheet to every `decimal` member and local. Representative, non-obvious spots:
- `AttributeSet.cs:173` — `decimal multiplier = (decimal)mult;` → `double multiplier = mult;` (inside `operator*(AttributeSet, double)`, `:170`). This drops the active quantizer — the ladder test in Task 5 pins the result.
- `SpellEffect.cs` — `GetPercentageDescription(string, decimal, string)` param → `double`; the formula table `Dictionary<string, decimal>` → `Dictionary<string, double>`; `decimal value`, `decimal rs, ls`, and the `(decimal)result[…]` casts → `double` / `(double)`; and the **four** `value = Convert.ToDecimal(buffer)` calls in the formula parser (`:1383,1419,1493,1530`) → `Convert.ToDouble(buffer)`. These four are an implicit `decimal→double` conversion, so they survive a mechanical rename with a green build — the broad Task 6 `[dD]ecimal` gate is what catches a miss. The parser result feeds a `(long)` truncation (`:577-579`); current data formulas are plain integers (checked the snapshot), so the binary-vs-decimal risk there is theoretical, but the four sites must still be converted.
- `Player.cs:2196-2198` — `decimal wait = (((decimal)(…) - …))` → `double wait = ((… - …))` (drop the inner `(decimal)` cast); note `wait = Math.Round(wait, 2)` at `:2198` now rounds a `double` (half-to-even) — the aether message's midpoint behavior changes (pinned in Task 5).
- `NPC.cs:749-750,1396-1397` — `snared = (buff.SpellEffect.SnarePercent / (decimal)100.0)` → `snared = (buff.SpellEffect.SnarePercent / 100.0)`; `decimal snared` → `double snared`.
- `NPC.cs:410,741,1388` and `Player.cs:2312,2501` — `<= Decimal.Zero` / `> Decimal.Zero` / `!= Decimal.Zero` → `<= 0` / `> 0` / `!= 0`. (Note `NPC.cs:741` is `>`, not `<=`/`!=`.)
- `PropertiesDictionary.cs:133` — drop `|| type == typeof(decimal)` from `IsNumericType` (dead once nothing in the process produces `decimal`); update the now-stale comment at `:90` ("JSON deserializes … decimals as double" — the conversion target is `double`).
- **Do NOT "fix" the integer division** at `BuyVitaCommand.cs:26` (`BaseStats.HP / IncreaseVitaBuyAmount`) or `PlayerCountExperienceModifierUpdateEvent.cs:35` (`Count / Interval`) — the rename keeps them integer division; adding `.0` would change vita prices and the experience modifier.

**Step 2: Readers**

- `SpellHandler.cs`, `NPCHandler.cs`, `ItemHandler.cs`, `ClassHandler.cs`: every `X = Decimal.Parse(reader.GetString("col"))` → `X = reader.GetDouble("col")` (`GetDouble` already exists, `DataReaderExtensions.cs:19`). This also removes the `CurrentCulture` dependency (culture safety improves — see Mutation impact).
- `Player.cs:809` and `Pet.cs:283,285`: `reader.GetDecimal("col")` → `reader.GetDouble("col")`.
- `DataReaderExtensions.cs:16-17`: delete the `GetDecimal` extension (now unused — verify with `grep -rn "\.GetDecimal(" Goose` returning nothing).

**Step 3: Commands + binder**

- `ChangeClassCommand.cs` — `decimal? modifier` → `double?`, `decimal rate = modifier ?? 1m` → `double rate = modifier ?? 1.0`.
- `AetherCommand.cs` — `decimal thres` → `double thres`.
- `BuyVitaCommand.cs` / `BuyManaCommand.cs` / `PetVitaCommand.cs` / `PetDamageCommand.cs` — `decimal buyrate` → `double buyrate`; `(decimal).2` → `0.2`.
- `PetSpawnCommand.cs` — `decimal wait` → `double wait`; drop the `(decimal)` cast.
- `CommandBinder.cs` — delete the `decimal` branch (`:70`) and remove `&& underlying != typeof(decimal)` from the guard (`:147-149`). The `double` branch (`:68`, with `double.IsFinite`) already handles these params.
- **Step 3b — `SetConfigCommand.cs` non-finite guard.** After the reflective `Parse` (`:39-41`), reject a non-finite `double` result before invoking the setter: if the parsed value is a `double` and `!double.IsFinite(…)`, send the existing "Couldn't set value" message and `return` (3 lines). This closes the one reachable non-finite path the binder guard does not cover (`/setconfig` never goes through `CommandBinder`).

**Step 4: Coupled test updates (keep the suite compiling)**

- `SpellCloneTests.cs:98` — `[typeof(decimal)] = 1.5m` → `[typeof(double)] = 1.5`.
- `DataReaderExtensionsTests.cs` — **delete `GetDecimal_ReturnsValue` outright** (`:75-81`). Do not rename it: `GetDouble_ReturnsValue` (`:83-89`) already exists and a renamed copy would be a near-duplicate.
- `CommandBinderTests.cs` — `MPositional(…, decimal e, …)` → `double e`; `MDecimal(CommandContext, decimal)` → `double`; `MNullableDecimal(CommandContext, decimal?)` → `double?`; update the `"decimal"` dispatch strings to `"double"`. **Rework the overflow `InlineData`** — the original `InlineData("123456789012345678901234567890", "decimal")` existed to prove decimal's 28-digit range, which has no `double` analogue. Under the binder's style (`NumberStyles.Float & ~AllowThousands`, `InvariantCulture`, `CommandBinder.cs:60`): the 30-digit token **now binds** as `1.2345678901234568E+29` (finite) — this is a genuine behavior change (28-digit rejection becomes acceptance), so assert the **successful** bind; and use `1e400` for the **rejection** case (parses to `∞`, rejected by `double.IsFinite`).
- `Part3GmATests.cs:406` — `ChangeClass_binds_decimal_modifier` → `ChangeClass_binds_double_modifier` (assert the bound value is a `double`).
- `DimensionModifierTests.cs:112` — `private static decimal StatOf(…)` → `double`.
- **Exact-equality decision rule + decimal-literal sweep.** For every `Assert.Equal(decimalExpr, doubleField)` where the expected side recomputes a production formula in `decimal`, either write the expected expression in the production evaluation order with `double` literals, or switch to `Assert.Equal(expected, actual, precision)` — binary ops are not associative/commutative in general, so `0.04*(3*0.5)` and `(0.04*3)*0.5` are not guaranteed equal. Most of the swept sites assign exactly-representable values (`1.0`, `1.5`, `0.25`, `0.1`) and survive; the dangerous ones recompute the formula. Verified decimal-literal sites to sweep:
  - `Goose.IntegrationTests/`: `DimensionModifierTests.cs:49,61` (recompute `0.04m*3*0.5m`, `0.015m*3*0.5m`), `DimensionItemTemplateTests.cs:94,100`, `DimensionSpellScriptTests.cs:56,65,67,377,387`, `DimensionsScriptTests.cs:62`, `DimensionVendorStockTests.cs:38,154,173`, `DimensionDropTests.cs:19,20,36`
  - `Goose.Tests/`: `Part2GeneralBTests.cs:257,284,294,299`, `SpellCloneTests.cs:98`, `NPCSpawnRegistrationTests.cs:44`, `PetDestroyInvisibilityTests.cs:42,69,97`, `GameWorldSettingsIsolationTests.cs:23,24,70,71,110,117`, `CommandBinderTests.cs:62,153,353`, `EventHandlerIntervalTests.cs:110`, `BuffNullGuardTests.cs:52,67`, `ClassLevelNullGuardTests.cs:102,103`, `InvisibilityBreakTests.cs:43,70`, `LoginEventNameLengthTests.cs:49`, `InvisibilityCounterTests.cs:38,56,285`, `TestFixtureIsolationTests.cs:36,37,65,66`, `ItemHandlerRegistrationTests.cs:11,16,38`, `InvisibilityMapLoadTests.cs:43`, `InvisibilityAggroTests.cs:42,69`, `InvisibilityTransitionTests.cs:42,69`

**Step 5: Build + unit tests green**

Run: `dotnet build Goose.sln`
Expected: `Build succeeded.` (0 errors)

Run: `dotnet test Goose.Tests`
Expected: PASS, and **pass count = Task 0 `Goose.Tests` baseline (695) minus the enumerated deleted tests**. The only deletion in this task is `GetDecimal_ReturnsValue` (−1); the `CommandBinderTests` decimal→double changes are reworks (rename + rework the overflow `InlineData`), which net zero. Expect **694**. If you delete any additional test, subtract accordingly and record why.

**Step 6: Round-trip proof (regression on the source-of-truth change)**

The existing integration suite loads real rows from a generated DB. Confirm at least one test that loads a player/NPC/spell and asserts a former-`decimal` field equals its expected value (e.g. a `haste` or `spell_damage` value) still passes — this proves `REAL`→`double`→`REAL` round-trips. If no such assertion exists, add one to `Goose.IntegrationTests` that: loads a template row, asserts the `double` field equals the sheet value, saves the entity, reloads, and asserts the value is unchanged.

**Step 7: Commit**

```bash
git add -A
git commit -m "refactor: switch C# decimal to double across domain, readers, commands, binder"
```

---

### Task 2: `.csx` scripts (runtime-compiled)

These are Roslyn-compiled at server load (cached per path, `Goose/Scripting/ScriptHandler.cs`), so they are a **separate** compile unit from Task 1 — the C# build is already green. A missed script fails at **boot**, not build, so the gate is the integration suite (which boots the server).

> The reviewer suggested folding Tasks 1+2 into a single commit, since a half-migrated state (C# `double`, scripts still `decimal`) boots but still does decimal math. This plan keeps them separate because Task 1 is a valid green commit (the C# compile unit) and Task 2 isolates the runtime-compiled script unit — but **do not consider the refactor complete until Task 2 lands**: Task 2's integration-boot gate is the only thing that catches a missed script.

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Item/ItemModifierScript.csx`
- Modify: `Goose/Data/Aspereta/Scripts/Item/ItemModifierScript.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Spells.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Items.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionSurname.csx`

**Step 1: Apply the cheat-sheet to each script**

- `ItemModifierScript.csx` (both Illutia and Aspereta) — `item.BaseStats.X += (decimal)(value / 100);` (`:95-119`) → `item.BaseStats.X += value / 100;` (the target is now `double`; `value / 100` is `double`).
- `Dimensions/Spells.csx:274` — `decimal linear = 1m + 0.5m * dim;` → `double linear = 1.0 + 0.5 * dim;`.
- `Dimensions/Npcs.csx` — `private decimal ScaleAttackSpeed(decimal attackSpeed, int dim)` (`:153`) → `private double ScaleAttackSpeed(double attackSpeed, int dim)`; and the `m` literals at `:123,129,155,156,286,287,440,441` (`0.15m`, `0.004m`, `0.175m`, `0.2m`, `0.5m`, `0.7m`, `1m`) → plain doubles.
- `Dimensions/Items.csx:263-268` — `a1.X * (decimal)half` → `a1.X * half` (or `(double)half`).
- `Dimensions/DimensionSurname.csx` — `decimal scale = (decimal)(dim * tier);` (`:22`) → `double scale = dim * tier;`; and the `m` literals at `:30,34,38,41,44,47` (`0.015m`, `0.04m`) → plain doubles.

**Step 2: Verify scripts compile at load**

Run: `dotnet test Goose.IntegrationTests`
Expected: PASS. A `.csx` that still references `decimal` against a `double` field fails Roslyn compilation at server boot and fails the integration tests — this is the red signal for a missed script. (The dimension tests in `DimensionModifierTests` exercise the dimension scripts specifically.)

**Step 3: Commit**

```bash
git add -A
git commit -m "refactor: switch .csx game scripts from decimal to double"
```

---

### Task 3: SQL DDL + CsvToSql generator + snapshot + schema.js

**Files:**
- Modify (DDL): `Goose/sql/players.sql`, `Goose/sql/spells.sql`, `Goose/sql/classes.sql`, `Goose/sql/pets.sql`, `Goose/sql/items.sql`, `Goose/sql/npcs.sql`, `Goose/sql/paypal.sql`
- Modify (generator): `CsvToSql/CsvToSql.Core/Schema/SqlType.cs`, `CsvToSql/CsvToSql.Core/Schema/Column.cs`, `CsvToSql/CsvToSql.Core/Schema/Col.cs`, and every `CsvToSql/CsvToSql.Core/*CsvToSql.cs` that calls `Col.Decimal(…)`
- Modify (test): `Goose.Tests/Schema/TableDdlTests.cs`
- Regenerate: `Goose.IntegrationTests/Fixtures/generated.snapshot`, `tools/DataEditor/schema.js`

**Persistence note:** these DDL edits apply to fresh DBs only (see Persistence strategy). No migration is added or needed. `Goose/bin/{Debug,Release}/sql/*` are build copies — do **not** hand-edit; they are refreshed by the build.

**Step 1: DDL (regex, space-tolerant)**

Replace every `DECIMAL(…)` with `REAL` using a regex that tolerates the space variant: `sed -E 's/DECIMAL\([0-9]+, *[0-9]+\)/REAL/g'` across the seven DDL files. This catches `DECIMAL(5, 2)` (`paypal.sql:9`), which a literal find-replace on `DECIMAL(5,2)` would silently miss. Verify: `grep -rn "DECIMAL(" Goose/sql --exclude=onetimeupdates.sql` → empty. (Leave `onetimeupdates.sql` alone — dead SQL-Server syntax.) Note: `DECIMAL(5,4)` has no live `.sql` occurrence — it comes only from the generator (`SqlType.Decimal54`), so don't hunt for it in the `.sql` files.

**Step 2: Generator primitives**

- `SqlType.cs:17-20` — delete `Decimal94/92/52/54`; add `public static readonly SqlType Real = new("REAL");`.
- `Column.cs:7` — `enum ColumnKind { Id, Int, Decimal, Text, Bool, Enum }` → `{ Id, Int, Double, Text, Bool, Enum }`.
- `Col.cs:15-16` — `public static Column Decimal(string name, SqlType type = null, string def = null) => Make(name, ColumnKind.Decimal, type ?? SqlType.Decimal94, def);` → `public static Column Double(string name, SqlType type = null, string def = null) => Make(name, ColumnKind.Double, type ?? SqlType.Real, def);`.

**Step 3: Generator call sites**

In every `*CsvToSql.cs`, `Col.Decimal("x", SqlType.Decimal94, def: "0")` → `Col.Double("x", def: "0")` (drop the `SqlType` arg; it now defaults to `Real`). Affected files: `TitleCsvToSql.cs`, `SurnameCsvToSql.cs`, `SpellsCsvToSql.cs`, `SpellEffectsCsvToSql.cs`, `NpcDropsCsvToSql.cs`, `NpcCsvToSql.cs`, `ItemsCsvToSql.cs`, `ClassInfoCsvToSql.cs`, `ClassesCsvToSql.cs`.

**Step 4: Preserve the DataEditor guardrail (reviewer's top ask — strongly recommended)**

Dropping `DECIMAL(p,s)` removes the only machine-readable precision/scale contract in the project. Today `validation.js` uses it to reject designer input — e.g. `chance = 10` into `Titles.chance DECIMAL(5,4)` (2 integer digits > the 1 allowed; note `chance = 5.0` is **not** rejected — the fraction check strips the trailing zero, so `5.0` validates cleanly today and after), or `snare_percent = 123456.7891` into `DECIMAL(9,4)` (6 integer digits > the 5 allowed). After the switch, `RANGES` is keyed by SQL type name and `RANGES["REAL"]` is undefined, so a `REAL` cell is valid iff it matches the numeric regex — `Titles.chance = 123456.789012345678` would save cleanly and `ItemHandler.RollModifier` (`:366-368`, `(int)(modifier.Chance * 100)`) would compute a nonsense range. To not lose that guardrail, **carry the former precision/scale into the schema as descriptor fields**:

- Extend `Col.Double` with optional descriptor parameters (exact signature is a design choice), e.g. `Col.Double(name, int? scale = null, double? max = null, string def = null)`, and let `Column`/`SchemaModel` carry `scale`/`max` so `SchemaGen` emits them into `schema.js`.
- At each call site that was `Col.Decimal("chance", SqlType.Decimal54, …)` (implying `DECIMAL(5,4)`), pass the former scale/max, e.g. `Col.Double("chance", scale: 4, max: 9.9999)` — `DECIMAL(5,4)` allows 1 integer digit + 4 fraction digits, so the max magnitude is `9.9999`, **not** `99.9999` (copying `99.9999` would double the accepted range — exactly the guardrail regression this step exists to prevent).
- The DDL column stays `REAL`; the descriptors are **editor metadata only** (they do not change the SQLite type). Task 4 makes `validation.js` read them.

If this step is deferred (the refactor loses the guardrail), record that decision explicitly — do not silently drop the validation.

**Step 5: DDL test**

`Goose.Tests/Schema/TableDdlTests.cs` — `Col.Decimal("droprate")` → `Col.Double("droprate")`; the expected DDL string `"  droprate DECIMAL(9,4) NOT NULL\n"` → `"  droprate REAL NOT NULL\n"`.

**Step 6: Regenerate the two checked-in generated artifacts**

Run: `dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js`
Expected: `schema.js` now emits `kind: "Double"` and `sql: "REAL"` for the former-decimal columns (plus the `scale`/`max` descriptors from Step 4, if done).

Run: `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter CsvToSqlSnapshotTests`
Expected: the snapshot is rewritten (`generated.snapshot` now shows `REAL`). **Then inspect the diff** — it must contain only `DECIMAL(…)`→`REAL` changes, nothing else (a semantic snapshot; an unexpected diff means a call site was missed).

**Step 7: Build + tests green**

Run: `dotnet build Goose.sln` → `Build succeeded.`
Run: `dotnet test` → PASS, including `TableDdlTests`, `CsvToSqlSnapshotTests` (now matching the regenerated snapshot), and the `Checked_in_schema_js_is_up_to_date` test (`tools/Tools.Tests/SchemaJsTests.cs:62`; fails if `schema.js` is stale).

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor: emit REAL instead of DECIMAL in DDL and CsvToSql; carry precision/scale descriptors; regenerate schema.js and snapshot"
```

---

### Task 4: Google Sheets data editor (JS)

**Files:**
- Modify: `tools/DataEditor/src/validation.js`
- Modify (tests): `tools/DataEditor/test/validation.test.js`, `tools/DataEditor/test/forms.test.js`, `tools/DataEditor/test/code-gs.test.js`, `tools/DataEditor/test/fake-sheets.js`
- Modify (comments): `tools/DataEditor/Code.gs`

**Depends on:** Task 3 (the regenerated `schema.js` now carries `kind: "Double"` / `sql: "REAL"`, and the `scale`/`max` descriptors if Task 3 Step 4 was done).

**Step 1: `validation.js`**

- Delete `decimalSpec` (`:27-32`) and `decimalMax` (`:34-39`) — no `DECIMAL(p,s)` to parse once columns are `REAL`.
- Delete the DECIMAL digit-count block (`:112-137`, `var spec = decimalSpec(…); if (spec) { … }`).
- Update the comment at `:90` (`Numeric kinds: Id, Int, Decimal.` → `Id, Int, Double.`).
- **Keep** the numeric regex at `:92` (`/^-?(\d+)(?:\.(\d+))?$/`).
- **If Task 3 Step 4 was done:** read the column's `scale`/`max` descriptors and apply the same digit-count/magnitude guard to `REAL` columns that carry them (replacing the deleted `decimalSpec`/`decimalMax` logic, now driven by the descriptors instead of a `DECIMAL(p,s)` string). Columns without descriptors validate as a plain number.

**Step 2: JS tests**

- `validation.test.js` — the DECIMAL precision/scale tests (`:129-177`) are obsolete; replace them with `REAL` cases: a plain decimal is accepted, a non-number is rejected, and (adversarial) a value with too many integer digits is no longer rejected by digit-count **when the column has no descriptor** (it is now a valid `REAL`). If descriptors are present, add a case asserting an out-of-scale value is rejected.
- `forms.test.js:419-424` — the "Text, Int, Id and Decimal all render a text input" case: swap `Decimal` for `Double` (no `forms.js` change needed — `forms.js` only special-cases `Enum`/`Bool`, `:169,180`).
- `code-gs.test.js:154` — the "editing the formatted decimal writes the value the user typed, in full" test encoded the old DECIMAL display-format behavior; rework it for `REAL` (the cell holds the number as typed, no display-scale distinction).
- `fake-sheets.js:27` — update the DECIMAL comment.

**Step 3: `Code.gs` comments**

Update the DECIMAL-referencing comments (`:40, 60, 377, 385, 452`) to reflect `REAL`, and remove the now-obsolete "decimal precision loss / displayed to fewer places" notes (they described a `DECIMAL(p,s)` display feature that no longer exists).

**Step 4: JS tests green + local dist rebuild**

Run: `node --test "tools/DataEditor/test/*.test.js"` (the **glob**, per `tools/README.md:152`).
Expected: PASS. A new failure is yours.

Rebuild the (gitignored) `dist/` locally so the editor runs: `node tools/DataEditor/build.mjs`. **Do not commit `dist/`** (it is gitignored, `tools/DataEditor/dist/`).

**Step 5: Commit**

```bash
git add tools/DataEditor/src tools/DataEditor/test tools/DataEditor/Code.gs
git commit -m "refactor: data editor validates REAL columns; drop DECIMAL(p,s) digit-count rules"
```

---

### Task 5: Adversarial precision tests (proof for the real behavior deltas)

The refactor is value-preserving for stored values, so the existing suite is the regression net. These tests pin the `double` behavior at the exact spots where a naive implementation could drift — and at the two observable deltas (`:F0`/`Math.Round` midpoint, the `(decimal)` quantizer) so the change is deliberate rather than accidental. Each would fail on the most likely wrong implementation.

**Files:**
- Test: `Goose.Tests/` (new or existing test file for chance/damage/packet/binder/save-text/setconfig)

**Step 1: Write the tests**

- **Chance roll:** `world.RollChance(0.5)` (a `double`) over N=20000 rolls lands in a ~50% band (e.g. 48–52%). Catches a broken chance path or a residual `(decimal)` truncation. (`RollChance` is `GameWorld.cs:687`; the item-chance path is `Inventory.cs:424`.)
- **Multiplier ladder (replaces the `1.5 * 2.0` stat test).** The `(decimal)` casts are an active quantizer, so pin the real ladder the dimension scripts use. For `dim` 1–5, `mult = Math.Pow(1.25, dim)`; with `base = new AttributeSet { HP = 100, MP = 100 }`, assert `(base * mult).HP` and `.MP` equal the exact `long` values below (verified to match the current decimal arithmetic, so this pins rather than changes behavior):

  | dim | `mult` | `(base*mult).HP` |
  |---|---|---|
  | 1 | 1.25 | 125 |
  | 2 | 1.5625 | 157 |
  | 3 | 1.953125 | 196 |
  | 4 | 2.44140625 | 245 |
  | 5 | 3.0517578125 | 306 |

  (The concrete off-by-one the quantizer currently prevents: `25 * (decimal)0.28 = 7.0` → `7` vs `25 * 0.28 = 7.000000000000001` → `8`.)
- **`:F0` midpoint (new).** Pin the double behavior for a half-valued percentage. `GetPercentageDescription` / the snare branch (`SpellEffect.cs:314-318,457-458`) on `10.5` → `"10"` (double, half-to-even) where `decimal` gave `"11"` (half-away); on `2.5` → `"2"` (was `"3"`). Assert the **new** values so the flip is deliberate. Also pin `Math.Round(2.135, 2) == 2.13` (the aether message, `Player.cs:2198`).
- **Wire int-cast:** an item with `SpellEffectChance = 55.7` produces the packet int `55` (`(int)55.7`, `Packets.cs:430`). Catches a rounding change to the wire value.
- **Binder `double` (corrected).** The 30-digit token `123456789012345678901234567890` **binds** as `1.2345678901234568E+29` (finite) — assert the **successful** bind (behavior change: 28-digit rejection becomes acceptance). The token `1e400` parses to `∞` and is **rejected** by the `double.IsFinite` guard (`CommandBinder.cs:68`) — assert rejection, not a bind to `Infinity`.
- **Snare/move-speed:** a `SnarePercent` of `15` yields a snare factor of `0.15` (`NPC.cs:749`), and `CalculateMoveSpeed` still returns an `int` (`Player.cs:1605`).
- **NPC/Pet move-tick (new).** The move/attack tick count is a `(long)` truncation of a binary product: `ev.Ticks += (long)((this.MoveSpeed * (1 + snared)) * world.TimerFrequency)` (`NPC.cs:756`) and the attack analogue (`NPC.cs:1403`). Pin a handful of `(MoveSpeed, SnarePercent) → ticks` assertions so a binary-vs-decimal shift of the product is caught (cosmetic — walk speed off by one tick — but cheap to pin).
- **SQL-text safety (new).** Build a player/pet save statement (the string-interpolated SQL at `Player.cs:1083,1166`, `Pet.cs:362,364,368,370,431,433,437,439`) for a very small (`1e-7`) and a very large (`1e21`) finite value. **Do not** assert the whole statement contains no `E` — the `UPDATE` keyword and identifiers (`aether_threshold`, `player_properties`) contain `E`/`e`, so that assertion is trivially false. Instead: extract the interpolated numeric substring for each value and assert it matches `^-?\d+(\.\d+)?$` (plain digits, no exponent); and assert the statement contains no scientific-notation pattern (`/\d[eE][-+]?\d/`), no `NaN`, and no `∞`. (Non-finite values are out of scope — the binder's `double.IsFinite` guard and the new `SetConfigCommand` guard reject them — but the numeric text must stay valid SQL for finite values.)
- **`/setconfig` (new).** `SetConfigCommand` binds setting values by reflection (`getter.ReturnType.GetMethod("Parse")`, `SetConfigCommand.cs:39-41`). Pin `/setconfig BaseHPPercentRegen 0.02` (a fractional `double.Parse`). **Also pin the new non-finite rejection:** `/setconfig <doubleSetting> NaN` and `/setconfig <doubleSetting> 1e400` are rejected by the Task 1 `IsFinite` guard (verified: default `double.Parse` accepts both — `NaN`→`NaN`, `1e400`→`∞` — while `decimal.Parse` threw; use `NaN`/`1e400` as the cases, **not** the literal `Infinity`, which default `NumberStyles` rejects).
- **Culture (extend existing).** Extend `DimensionModifierTests.Rarity_multiplier_parses_invariant_under_any_culture` (`:81-103`) to cover a former-`Decimal.Parse` column, confirming `reader.GetDouble` is culture-free where `Decimal.Parse` used `CurrentCulture`.
- **Economy (new).** One test that `VitaCost * buyrate` and the `(long)` truncation at `BuyVitaCommand.cs:26`/`PlayerCountExperienceModifierUpdateEvent.cs:35` produce the expected integer price/modifier (guards against the binary-vs-decimal truncation shifting by 1, and against someone "fixing" the integer division).

**Step 2: Run to confirm they pass on the refactored code (green)**

Run: `dotnet test Goose.Tests --filter <the new tests>`
Expected: PASS. (These are proof/regression tests on already-refactored code, so they are green here; they are written to fail on a wrong implementation, not to drive the refactor.)

**Step 3: Commit**

```bash
git add -A
git commit -m "test: pin double behavior at chance roll, multiplier ladder, :F0 midpoint, wire int-cast, binder, save-text, setconfig"
```

---

### Task 6: Full verification

**Step 1: Clean build + all tests**

Run: `dotnet build Goose.sln` → `Build succeeded.`
Run: `dotnet test` → PASS (unit + tools + integration — integration now runs because Task 0 added `Goose.IntegrationTests` to the solution), pass count ≥ Task 0 baseline minus the enumerated deleted tests. **If Task 0 Step 0 was not done, run `dotnet test Goose.IntegrationTests` explicitly** — a root `dotnet test` alone skips it.

**Step 2: Grep for stragglers**

Run: `grep -rniE "[dD]ecimal" Goose --include=*.cs`
Expected: **empty** — the pattern is **case-insensitive `[dD]ecimal`** so the gate catches `Convert.ToDecimal` (`SpellEffect.cs:1383,1419,1493,1530`) and `public Decimal DropRate` (`NPCDropInfo.cs:7`), which a lowercase `\bdecimal\b\|Decimal\.` pattern silently misses (both are an implicit `decimal→double` conversion and survive a mechanical rename with a green build — the gate is what proves the migration is complete). After the refactor (including the four `Convert.ToDecimal`→`Convert.ToDouble`, `Decimal DropRate`→`double DropRate`, and dropping `typeof(decimal)` from `PropertiesDictionary.cs:133` + its `:90` comment), no `decimal`/`Decimal` reference remains under `Goose/`. (`FakeDbDataReader.GetDecimal` lives under `Goose.Tests/`, outside this search path, and is an abstract `DbDataReader` override that must stay regardless — it is not an exception to this gate.)

Run: `grep -rlnE "[0-9]m\b" Goose/Data --include=*.csx`
Expected: **empty** — no `decimal`-suffixed literals (`0.15m`, `1m`, …) remain in the runtime-compiled scripts. A missed `m` literal feeding `+=` on a now-`double` field compiles (implicit `decimal→double`) and stays silent, so this gate is the only completion check for the `.csx` unit (the Task 6 `*.cs` gate does not cover `*.csx`).

Run: `grep -rn "DECIMAL" Goose/sql CsvToSql tools/DataEditor/src --exclude=onetimeupdates.sql`
Expected: **empty** — the dead `onetimeupdates.sql:35` is the only `DECIMAL` in the searched directories and is declared out of scope, so it is excluded. (If Task 3 Step 4 added `scale`/`max` descriptors, they are not `DECIMAL` and do not trip this gate.)

**Step 3: Live client spot-check** (the client is a fixed external binary — the one axis the tests cannot cover)

Boot the server against a **fresh** DB (so the new `REAL` DDL is exercised) and, with a client connected, confirm:
- Stat display (move/attack speed, regen, damage) reads correctly.
- A chance-based effect (spell-effect chance, snare) triggers at the expected rate.
- A spell/item description with a half-valued percentage renders the new `:F0` value (e.g. `10.5` → `10%`).
- Aether threshold and a spell's HP/MP/SP percent cost behave correctly.
- A GM command with a fractional arg (`/aether 1.5`, `/changeclass Bob Warrior 1.5`) binds and applies.

**Step 4: No commit** (verification only).

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Persisted values round-trip (`REAL`→`double`→`REAL`) | Task 1 Step 6 integration round-trip test |
| `REAL` cell validates as a plain number (no digit-count rejection when no descriptor) | Task 4 `validation.test.js` REAL cases (adversarial: large integer digit count accepted) |
| DataEditor guardrail survives via `scale`/`max` descriptors (if Task 3 Step 4 done) | Task 4 `validation.test.js` descriptor cases |
| Chance roll uses the `double` value (no truncation) | Task 5 chance-roll test (adversarial) |
| `(decimal)` quantizer removal does not shift the multiplier ladder | Task 5 ladder test (pins exact `long` HP/MP) |
| `:F0`/`Math.Round` midpoint is the deliberate double behavior | Task 5 `:F0` + `Math.Round` test |
| Wire int-cast of a fractional chance truncates, not rounds | Task 5 wire int-cast test (adversarial) |
| Binder binds `double`; 30-digit token now binds, `1e400` rejected (no `Infinity`) | Task 5 binder test + Task 1 `CommandBinderTests` reworked overflow case |
| Save-statement **numeric** text stays plain digits (no exponent/`NaN`/`∞`) for finite values | Task 5 SQL-text safety test (asserts the interpolated value matches `^-?\d+(\.\d+)?$` — not the whole statement, which contains `E`/`e`) |
| `/setconfig` binds fractional `double` values and rejects non-finite (`NaN`/`1e400`) | Task 5 setconfig test (fractional bind + `IsFinite` rejection) |
| NPC/Pet move/attack tick `(long)` truncation of the binary product is stable | Task 5 move-tick test (`(MoveSpeed, SnarePercent) → ticks`) |
| Integer-division truncation preserved (vita price, exp modifier) | Task 1 warning + Task 5 economy test |
| DDL emits `REAL`; snapshot and `schema.js` match | Task 3 `TableDdlTests` + `CsvToSqlSnapshotTests` + `Checked_in_schema_js_is_up_to_date` |
| `.csx` scripts compile at load and carry no `decimal` literals | Task 2 integration boot (a missed script fails at boot) + Task 6 `grep -rlnE "[0-9]m\b" Goose/Data --include=*.csx` (catches a missed `m` literal that would compile silently) |
| Integration suite is compiled and run by the gates | Task 0 Step 0 (sln) + explicit `dotnet test Goose.IntegrationTests` |
| Existing behavior preserved (no test lost beyond the enumerated deletions) | Task 1 Step 5 pass count = baseline − deleted tests |

## Red-team notes (checked before writing)

- **Threading/lifecycle:** the change is compile-time; no new runtime thread/context is introduced. `GetDouble`/`GetDecimal` both route through the name indexer (`reader[column]`), so the single-threaded `Database` service is unaffected.
- **Persistence/schema:** strategy is explicit — **no migration**. DDL edits are fresh-DB-only (`GameWorld.cs:131-153`); `MigrateDatabaseSchema` (`:164-170`) never changes a column type; existing DBs keep REAL-backed values. No `ALTER ... ALTER COLUMN` is added (SQLite no-op).
- **SQL text is not parameterized:** the save statements concatenate the numeric fields into SQL text (`Player.cs:1083,1166`, `Pet.cs:362,364,368,370,431,433,437,439`), `CurrentCulture`-interpolated for **both** types (a comma-decimal locale renders `0.5` as `0,5` into the SQL today — pre-existing, not a regression). `double.ToString()` can emit `E`-notation/`NaN`/`∞`; `NaN`/`∞` are not SQL literals. Non-finite persisted values are out of scope (binder's `double.IsFinite` guard **plus** the new `SetConfigCommand` guard, since `/setconfig` bypasses the binder), and Task 5 pins the **numeric** text for finite values (the interpolated value must match `^-?\d+(\.\d+)?$` — not the whole statement, which contains `E`/`e`).
- **Defense-in-depth:** the binder's `double.IsFinite` guard (`CommandBinder.cs:68`) covers command args, but `/setconfig` bypasses the binder (reflection `Parse`, `SetConfigCommand.cs:39-41`), so Task 1 adds an `IsFinite` guard there too. A broader boundary guard (assert `double.IsFinite` where values enter the domain objects) is still advisory and not folded in — add it for the stronger guarantee that a non-finite value can never reach the save path.
- **The `(decimal)` casts are load-bearing quantizers**, not noise. Dropping them moves `Math.Ceiling`/`Math.Floor`/`(long)` from decimal to binary arithmetic; near an integer boundary the result can shift by 1 (verified: `25 * (decimal)0.28` → `7` vs `25 * 0.28` → `8`). Task 5 pins the real ladder.
- **Two midpoint-rounding deltas** (`:F0`, `Math.Round`) are user-visible in descriptions and the aether message. Named and pinned, not assumed invisible.
- **Integer-division sites** (`BuyVitaCommand.cs:26`, `PlayerCountExperienceModifierUpdateEvent.cs:35`) keep their truncation; the rename does not promote them to floating division. Do not "fix" them.
- **Editor guardrail:** dropping `DECIMAL(p,s)` removes the DataEditor's digit-count validation; Task 3 Step 4 / Task 4 carry the precision/scale as descriptors so the guardrail survives (reviewer's top ask). If deferred, the loss is recorded explicitly.
- **Failure behavior:** a missed `.csx` fails loudly at boot (Roslyn), not silently — the integration suite is the catch. A missed DDL call site is caught by the regenerated-snapshot diff (Task 3 Step 6) and the `schema.js` up-to-date test.
- **Test-helper reality:** `FakeDbDataReader` needs **no change** — it already overrides both `GetDecimal` (`Goose.Tests/Fakes/FakeDbDataReader.cs:33`, abstract, must stay) and `GetDouble` (`:34`), and the extension helpers route through the name indexer, so a `values["col"]` numeric object works for `Convert.ToDouble`.
- **Task order:** tests are updated in the same task as the code they reference (Task 1) so the build is green at each commit; the new adversarial tests (Task 5) are proof, not red-driven, because the refactor is value-preserving. The refactor is not complete until Task 2 (scripts) lands.
- **Code detail:** no full implementation bodies — only signatures, the transformation cheat-sheet, and small test snippets, per the plan's code-detail rules.

## Execution handoff

When ready to implement, run the tasks in order in an isolated worktree (@using-git-worktrees). For per-task execution with review, see @subagent-driven-development.
