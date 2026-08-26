# Mechanical C# Modernization Implementation Plan

**Goal:** Bring the `Goose` server project from 2010-era C# style to idiomatic net10.0 / C# 14 (net10.0's default language version) — interpolation, collection expressions, expression-bodied members, enumerated switch expressions, modern null checks — with zero behavior change and zero test-count change.

**Architecture:** A sequence of small, independently committable mechanical passes over `Goose/`, each gated by a full build + fast test run. The intended end-state style is defined first, in `.editorconfig` (Task 2), so later passes implement a stated policy rather than ad-hoc taste. No new APIs are introduced in this plan; the `DbDataReader` helper work is a separate plan (`2026-08-25-dbdatareader-helpers.md`) and nullable adoption is a third (`2026-08-25-nullable-adoption.md`). `this.` prefixes are intentionally left in place (deferred by decision).

**Tech Stack:** .NET 10 (`net10.0`, C# 14), xUnit (fast gate: `Goose.sln` = Goose.Tests + Tools.Tests; integration project separate), System.Data.SQLite, NLog.

---

## APIs verified

- `Goose/Goose.csproj:3-7` — no `<Nullable>`, no `<ImplicitUsings>`, no `TreatWarningsAsErrors`. Test/tool projects already set both.
- `.editorconfig` is 5 lines: `root = true` plus one `[*.cs]` section containing `indent_style = space`. New keys go into that existing section.
- EditorConfig key names verified against the Roslyn 5.6.0 assemblies in the installed SDK tooling (`Microsoft.CodeAnalysis.CSharp.Features.dll` / `Microsoft.CodeAnalysis.Features.dll`, option identifiers present: `VarForBuiltInTypes`, `VarWhenTypeIsApparent`, `VarElsewhere`, `PreferBraces`, `PreferNullCheck`, `PredefinedType`, `CollectionInitializer`, `ObjectInitialization`, `ExplicitTupleNames`). Note: the repo's `Microsoft.CodeAnalysis.CSharp` package reference pins the compiler version, but the Features assemblies come from the SDK tooling, not from project package references. **Not** present in either assembly: `PreferInterpolatedString` and any string-Format style option — no editorconfig key exists for the Format→interpolation policy, so Task 4 carries it.
- All ~103 `string.Format(` sites in `Goose/` use literal format strings (verified; no non-literal formats, no `{{` escapes). One multi-line call at `Goose/Database.cs:59-61` must not be mangled by single-line tooling. Client-visible format specifiers exist (`{0:N0}`, `{0:F0}` in `Goose/PlayerInfoWindow.cs:45-60`, `Goose/Quests/QuestWindow.cs:216-236`) and must be preserved.
- `Goose/EventHandler.cs:16` declares `class EventHandler` in namespace `Goose`, shadowing `System.EventHandler`. Verified: no file uses the BCL `EventHandler` unqualified, so ImplicitUsings changes no name resolution. No other `Goose/` type collides with an implicit-using name (swept: `Timer`, `Path`, `File`, `Task`, `Random`, `Monitor`, `Directory`, `HttpClient`, `EventArgs`, …).
- `Goose.sln` membership (verified): `Goose`, `CsvToSql.Core`, `CsvToSql.Console`, `Goose.Tests`, `SchemaGen`, `SpriteBundle`, `tools/Tools.Tests`. The fast gate `dotnet test Goose.sln` runs Goose.Tests (~321) + Tools.Tests (~111) ≈ 432 tests. `Goose.IntegrationTests` is **not** in the sln (~10x slower, per `docs/testing.md`).
- **Multi-char `Split` trap (verified at runtime):** `Split(' ', ',', StringSplitOptions.RemoveEmptyEntries)` binds to `Split(char, int, StringSplitOptions)` with `count = ','` = 44 — it splits on the space only, capped at 44 elements (`"a,b c d"` → `[a,b | c | d]`). Neither `Split(char, char, StringSplitOptions)` nor a 5-`char` overload exists in .NET 10 (verified by enumerating all `String.Split` overloads). The multi-separator form must be a `char[]`: `Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)`. Single-char forms (`Split(' ')`, `Split(' ', StringSplitOptions)`, `TrimEnd('\x1')`) are fine — those overloads exist.
- Known `foreach` hazard: `Goose/Quests/QuestWindow.cs:280` — `foreach (Inventory.EquipSlots slot in Enum.GetValues(typeof(Inventory.EquipSlots)))`. The explicit type unboxes each `object` element; `var` would infer `object` and change semantics.
- Site inventories are point-in-time greps; treat as approximate. No task uses these totals as pass criteria except comparisons against the Task 1 baseline.

## Invariants (must hold after every task)

1. `dotnet build Goose.sln` succeeds.
2. `dotnet test Goose.sln --no-restore` passes with **exactly the baseline test count** — this plan adds no tests — and no existing test name disappears or becomes skipped.
3. No client-observable change: packet strings, SQL text, and stdout output are byte-identical. This plan contains no intentional observable changes (the one candidate — routing speedhack output through NLog — was moved to Deferred for exactly this reason).
4. `git diff` of each commit contains only the pattern that task names — no drive-by edits, no `this.` removal, no comment rewrites (AGENTS.md: leave unrelated comments alone).

---

### Task 1: Capture baseline

**Files:** none (record only)

**Step 1:** Run and save. `Goose.IntegrationTests` is not in `Goose.sln`, so the solution build does not restore it — restore it explicitly before its first `--no-restore` invocation:

```bash
dotnet restore Goose.IntegrationTests/Goose.IntegrationTests.csproj
{ dotnet build Goose.sln \
  && dotnet test Goose.sln --no-restore \
  && dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore; } > /tmp/goose-baseline.txt 2>&1
echo "baseline exit: $?"
```

Expected: build clean, both suites green, exit 0. If anything is red before we start, stop and triage first — the refactor must not inherit a broken baseline.

**Step 2:** Record the discovered test names for identity comparison. A fixed TRX `LogFileName` is overwritten across the two test projects in the solution, so use a dedicated results directory plus `LogFilePrefix` (per `dotnet test` TRX guidance):

```bash
dotnet test Goose.sln --no-restore --results-directory /tmp/goose-baseline-trx --logger "trx;LogFilePrefix=baseline" > /dev/null 2>&1
python3 - <<'EOF' > /tmp/goose-test-names-baseline.txt
import glob, xml.etree.ElementTree as ET
rows = []
for f in sorted(glob.glob('/tmp/goose-baseline-trx/*.trx')):
    for r in ET.parse(f).getroot().iter():
        if r.tag.endswith('UnitTestResult'):
            rows.append(f"{r.get('outcome')}\t{r.get('testName')}")
print('\n'.join(sorted(rows)))
EOF
wc -l /tmp/goose-test-names-baseline.txt
```

This file (name + outcome per test, sorted) is the identity baseline; the final verification diffs against it.

**Step 3:** No commit.

---

### Task 2: EditorConfig style policy

**Files:**
- Modify: `.editorconfig` (add keys to the existing `[*.cs]` section)

**Step 1:** Add to the existing `[*.cs]` section (do not create a second section). All key names verified against Roslyn 5.6.0 (APIs verified). Suggestion severity only — no build enforcement, so no new warning noise:

```editorconfig
# var policy: explicit for built-in types, var when the type is apparent
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_object_initialization = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion
csharp_prefer_braces = false:none
```

The var values implement "use var when apparent" deliberately — not "always var" — matching the reviewer-approved policy. There is no editorconfig key for string.Format→interpolation (verified absent); that policy lives in Task 4.

**Step 2:** No build impact by construction (suggestion severities). Verify `dotnet build Goose.sln` output is unchanged.

**Step 3:** Commit: `chore: define modern C# style policy in editorconfig`

---

### Task 3: ImplicitUsings for Goose

**Files:**
- Modify: `Goose/Goose.csproj` (add `<ImplicitUsings>enable</ImplicitUsings>` in the first `PropertyGroup`)
- Modify: every `Goose/**/*.cs` with redundant using lines

**Step 1:** Add the property. The SDK implicit set is: `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Net.Http`, `System.Threading`, `System.Threading.Tasks`.

**Step 2:** Delete exactly those using lines (whole-line exact matches only) from all `Goose/**/*.cs`. Keep `using System.Data;`, `using System.Text;`, `using System.Runtime.InteropServices;`, NLog, etc.

**Step 3:** Build + fast suite (invariants 1–2). The `Goose.EventHandler` shadowing check is the only resolution risk; if the build shows an ambiguity error, qualify that one site with `Goose.`, do not revert.

**Step 4:** Commit: `refactor: enable implicit usings in Goose project`

---

### Task 4: string.Format → string interpolation

**Files:** all `Goose/**/*.cs` containing `string.Format(` (~103 sites, all literal format strings — verified)

**Step 1:** Convert each `string.Format("{0} ...", a, b)` to `$"...{a}...{b}"`. Rules:
- Keep format specifiers: `string.Format("{0:0.##}", x)` → `$"{x:0.##}"`. Client-visible `{0:N0}`/`{0:F0}` sites (`PlayerInfoWindow.cs:45-60`, `QuestWindow.cs:216-236`) must come out byte-identical — interpolation is culture-identical to the corresponding `IFormattable` path.
- Literal braces: `string.Format("{{0}}", x)` → `"{0}"` (drop the Format call) or `$"{{0}}"` where the argument is still used elsewhere — whichever keeps the diff minimal.
- Repeated indices (`{0}` used twice) inline the variable twice.
- Argument evaluation order: `string.Format` evaluates arguments in call-argument order; interpolation evaluates expressions in placeholder order. Convert only where the two orders are identical — the *i*-th argument corresponds to placeholder `{i}` in the same order — or where every argument is provably side-effect-free (literals, fields, simple member access). Otherwise leave the site and note it in the commit message.
- Do not touch `string.Format` calls whose format string is not a literal (none expected; if found, leave and note in the commit message).
- Handle the multi-line call at `Goose/Database.cs:59-61` by hand, not single-line tooling.

**Step 2:** Build + fast suite. Spot-check client-visible strings: diff the commit and confirm every changed line is a pure Format→`$""` rewrite (invariant 3).

**Step 3:** Commit: `refactor: replace string.Format with interpolation`

---

### Task 5: Collection expressions and char-based separators

**Files:** all `Goose/**/*.cs` matching the patterns below, plus the four `Array.Empty` sites listed in Step 1

**Step 1:** Apply, pattern by pattern:
- `= new List<T>()` / `= new Dictionary<K, V>()` / `= new HashSet<T>()` no-arg field/local initializers (~83 sites) → `= [];` (e.g. `Goose/Quests/Quest.cs:37-39` constructor: `this.Requirements = [];`). An *empty* collection expression compiles for any target with a parameterless constructor (verified: List, HashSet, Dictionary, Queue, Stack, ObservableCollection all accept `= [];`), so every no-arg site is convertible.
- Copy-constructor argument position → spread: `new List<T>(collection)` → `[.. collection]`, **only for targets with a single-arg `Add`** (List, HashSet, …). A spread into `Dictionary` is a compile error (CS9215, verified), so `Goose/Item.cs:222` (`new Dictionary<ItemProperty, object>(this.ItemProperties)`) is **excluded — stays as-is**. A per-site non-null proof is required because a null source fails differently: `new List<T>(null)` and `[.. null]`→List both throw `ArgumentNullException`, but `new HashSet<T>(null)` throws `ArgumentNullException` while `[.. null]`→HashSet throws `NullReferenceException` (verified at runtime). The 7 convertible sites and their proofs:
  - `Goose/NPCTemplate.cs:260-262` (Allies, Drops, Quests) — each already guarded by `is null ? … :`; the spread branch only runs on a non-null source.
  - `Goose/Guild.cs:291` — `OnlineMembers` is assigned exactly once in the codebase (constructor at `:61`, a new collection; verified by grep).
  - `Goose/SpellEffect.cs:299-301` (BuffStacksOver, BuffDoesntStackOver) — each already guarded by `== null ? … :`.
  - `Goose/AccessLevels.cs:60` — source is a LINQ query (`Enum.GetValues(…).Cast<…>()`), never null.
- **Explicit exclusions — do NOT convert:** `Goose/LoginThrottle.cs:42` and `Goose/Currency/CurrencyHandler.cs:12` (`new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)` — the argument is an `IComparer`, not a collection; a scripted pass would emit nonsense). No `new List<T>(int capacity)` or `new List<T>() { a, b }` sites exist.
- `Array.Empty<T>()` → `[]` (4 sites, all outside `Goose/`: `CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs:31-32`, `CsvToSql/CsvToSql.Core/Schema/Column.cs:33`, `Goose.Tests/Schema/TableDdlTests.cs:75`).
- `Split(new[] { ' ', ',' }, ...)` → `Split([' ', ','], ...)` (5 sites, e.g. `Goose/Quests/Quest.cs:63`). The collection expression produces the `char[]` the `Split(char[], StringSplitOptions)` overload takes. **Do not** write `Split(' ', ',', ...)` — that binds to `Split(char, int, StringSplitOptions)` with `count = 44` (APIs verified).
- `Split(" ".ToCharArray())` / `Split(",".ToCharArray())` → `Split(' ')` / `Split(',')` (~29 sites across `Goose/Events/*`, e.g. `Goose/Events/PlayerCastSpellEvent.cs:28`, `Goose/Events/LoginEvent.cs:64`, `Goose/ChatFilter.cs:39`).
- `TrimEnd("\x1".ToCharArray())` → `TrimEnd('\x1')` (4 sites: `Goose/NPC.cs:602,1049,1232`, `Goose/SpellEffect.cs:1333` — the last discards its result, a pre-existing no-op; preserve the no-op, do not "fix" it).

Do not convert `new()` target-typed uses (already modern) or constructor calls not listed above.

**Step 2:** Build + fast suite. **Step 3:** Commit: `refactor: use collection expressions and char-based separators`

---

### Task 6: Expression simplifications

**Files:** all `Goose/**/*.cs` matching the patterns below

**Step 1:** Apply:
- Redundant bool ternary (30 sites, all literal `? true : false` / `? false : true` — verified no value-branch cases): `("0".Equals(x) ? false : true)` → `x != "0"`; `(cond ? true : false)` → `cond` (e.g. `Goose/Quests/Quest.cs:57-59`).
- `using (var x = ...)` block form (32 sites) → `using var x = ...;` **only when the original using block already extends to the end of its enclosing scope** — then disposal timing is provably identical. A using declaration disposes at the end of its *enclosing scope*, so converting a mid-scope block extends the resource's lifetime (readers, commands, files) and can change behavior — e.g. on `Goose/Database.cs:74` the catch at `:80-89` disposes `_connection`; a `using var cmd` would dispose `cmd` only at lambda exit, *after* the connection. Blocks not reaching scope end stay in block form, no exceptions. (Known mid-scope sites that must stay: `Goose/ClassHandler.cs:48,67,118`, `Goose/Inventory.cs:914,933,964`, `Goose/Map.cs:543,562`, `Goose/Database.cs:74`.)
- Null comparisons (~309 sites): `x == null` → `x is null`, `x != null` → `x is not null`. **Audit the distinct static types at the comparison sites before converting** (grep the LHS expressions, look up declarations): the conversion is safe only where the type does not overload `operator ==` (an overloaded `==` can treat null differently from `is null`). Verified so far: all Goose-defined types are safe (no `operator ==` is declared anywhere in the repo — the only custom operators are `Aggro`'s `<`/`>`), and the BCL types appearing at these sites (string, List, Socket, Match, Exception, DataReader, …) do not overload `==`. Any site whose type cannot be positively confirmed stays unconverted and is noted in the commit message.
- `foreach (T x in ...)` → `foreach (var x in ...)` **only where `T` is exactly the declared element type of the collection** (no implicit conversion is elided). Never convert where the enumerable is `Enum.GetValues(...)` or any non-generic source: `Goose/Quests/QuestWindow.cs:280` (`foreach (Inventory.EquipSlots slot in Enum.GetValues(typeof(Inventory.EquipSlots)))`) must stay — `var` there infers `object` and changes semantics. When in doubt about a site's element type, leave it.
- Double-cast division: `(double)diff / (double)world.TimerFrequency` → `diff / (double)world.TimerFrequency` — exactly one site, `Goose/Events/MoveEvent.cs:77`. Safe: with one operand already `double`, the other is promoted before division, so no integer-overflow path exists. Do not touch other casts.
- `Enum.GetValues(typeof(X))` → `Enum.GetValues<X>()` where the call is immediately followed by `.Cast<X>()` (inconsistent today, e.g. `Goose/PlayerInfoWindow.cs:69` vs `Goose/Console/Commands/SetAccessCommand.cs:110`).
- No LINQ-terminal sub-task: verified only 3 `.First()/.Last()/.Single()/.Count()` sites exist in `Goose/` (`Goose/Events/SetAccessCommandEvent.cs:33`, `Goose/Quests/QuestWindow.cs:304,313`) and none has an indexable receiver — nothing to convert.

**Step 2:** Build + fast suite. **Step 3:** Commit: `refactor: simplify expressions (ternaries, using var, is-null, foreach var, Enum.GetValues<T>)`

---

### Task 7: Expression-bodied properties

**Files:** all `Goose/**/*.cs` with single-return block properties (97 sites, e.g. `Goose/PlayerInfoWindow.cs:11-14`)

**Step 1:** Convert `public override string Title { get { return ...; } }` → `public override string Title { get => ...; }`. Only single-statement (one `return`, no local variables, no try/catch) getters. Multi-statement getters stay.

**Step 2:** Build + fast suite. **Step 3:** Commit: `refactor: expression-bodied single-return properties`

---

### Task 8: Switch expressions (enumerate, then convert the list)

**Files:** determined by the Step 1 enumeration

"Convert all suitable switches" is too subjective for a mechanical pass, so this task is two-phase: enumerate first, then convert only the enumerated list.

**Step 1: Enumerate.** Grep all switch statements (~228 `case` labels codebase-wide) and classify each:
- **Convert:** the switch is a pure assignment or return of computed values; every case arm is side-effect-free (value assignments to the switched-on target or literals); no `goto`; no multi-statement cases with control flow.
- **Keep:** any case with side effects, `break`-fallthrough semantics that matter, `goto`, or a missing `default` whose fall-through behavior cannot be expressed as a no-op arm.

Record the convert list (file:line) in the commit message of Step 2's commit so the scope is reviewable.

**Step 2: Convert the enumerated list.** Worked example — the direction switch `Goose/Events/MoveEvent.cs:97-110` (`direction` is pre-validated to 1..4 at `:60`; the original has **no** `default:`, so fall-through is a no-op):

```csharp
(x, y) = direction switch
{
    1 => (x, y - 1),
    2 => (x + 1, y),
    3 => (x, y + 1),
    4 => (x - 1, y),
    _ => (x, y),
};
```

Rules: the `_` arm (required for int switch expressions) always receives the original's no-`default` fall-through behavior — a no-op. Never give `_` a side effect the original did not have. Reassign the same variables the original mutated; do not introduce intermediate `(dx, dy)` values that are computed but not applied.

**Step 3:** Build + fast suite. **Step 4:** Commit: `refactor: switch expressions for enumerated side-effect-free switches`

---

### Final verification

```bash
dotnet build Goose.sln
dotnet test Goose.sln --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Compare against `/tmp/goose-baseline.txt`: identical pass/fail counts, all green. For test identity, re-run the Task 1 Step 2 TRX capture into `/tmp/goose-final-trx` and extract `/tmp/goose-test-names-final.txt` the same way, then:

```bash
diff /tmp/goose-test-names-baseline.txt /tmp/goose-test-names-final.txt
```

Expected: empty diff (this plan adds no tests). Confirm `grep -rc "string\.Format(" Goose --include=*.cs` is at its post-refactor level.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| No behavior change across all tasks | Fast-sln suite green at every commit with baseline-identical counts and test identities; integration suite green at the end |
| Client-visible strings unchanged | Invariant 3 spot-check in Task 4/5/6 steps (diff review); packet-building tests in the existing suite |
| ImplicitUsings changes no name resolution | Build + suite in Task 3; `Goose.EventHandler` shadow verified in "APIs verified" |
| `using var` conversions preserve disposal timing | Task 6 rule: only blocks that already reach scope end are converted (timing-identical by construction); known mid-scope sites listed as must-stay |
| `foreach var` conversions preserve element type | Task 6 rule: only exact-element-type sites; `Enum.GetValues` foreach explicitly excluded |
| EditorConfig keys are real | Key identifiers verified present in Roslyn 5.6.0 assemblies (APIs verified) |

## Deferred (with reasons)

- **Speedhack output → NLog (`Goose/Events/MoveEvent.cs:82-84`):** an intentional operational behavior change (message moves from stdout to the NLog target). One-line follow-up: add the standard `log` field and `log.Warn($"SUSPECTED SPEEDHACK: {this.Player.Name} 15sq/{secs}sec = {rate}");`. Not part of a zero-behavior-change pass.
- **`Convert.ToInt32(string)` → `int.Parse`:** not equivalent — `Convert.ToInt32(null)` returns `0`, `int.Parse(null)` throws `ArgumentNullException` (both verified on the installed runtime). Every call site would need a per-site null-precondition proof. Separate follow-up.
- **`DbDataReader` helpers + `Convert.To*(reader["..."])` migration:** separate plan, `docs/plans/2026-08-25-dbdatareader-helpers.md`.
- **Nullable reference types:** separate plan, `docs/plans/2026-08-25-nullable-adoption.md`.
- **`DateTime.Now` → `DateTime.UtcNow`:** `unban_date` is persisted (`Goose/Player.cs:875,1052`, `Goose/sql/players.sql:53`, migration `Goose/sql/onetimeupdates.sql:24`). Switching clock source mixes local and UTC values in existing databases without a row migration. Needs its own design.
- **`Task.Run` without `CancellationToken` (3 sites, e.g. `Goose/Events/UpdateSqlCommandEvent.cs:19`):** `GameServer` shutdown is a flag (`RequestShutdown`, `Goose/GameServer.cs:372`); there is no token to pass. Plumbing one is a feature, not a style fix.
- **Records/`init` for data classes (`QuestProgress`, `QuestRequirement`, `Buff`):** style preference with a large diff and a JSON-serialization surface (`Goose/PropertiesDictionaryJsonConverter.cs`); revisit after nullable is settled.
- **Removing `this.` (4153 sites):** explicitly deferred by decision.
- **`EnforceCodeStyleInBuild` / `AnalysisLevel latest-recommended`:** would flood the build with warnings before the codebase is at the bar; revisit after the Task 2 policy has proven out in editors.

## Execution notes

- Implement in a dedicated worktree per @using-git-worktrees (branch e.g. `modern-csharp-mechanical` off `master`).
- Tasks are mechanical: scripted regex passes (perl/sed) are fine for the simple patterns, but every commit must be diff-reviewed for false positives before the build gate — the suites catch semantic breakage, not a mangled string literal.
- One commit per task. No task may touch patterns owned by another task.
- Ordering matters: Task 3 (implicit usings) before any task that adds new files; Task 2 first so the policy precedes the transformations.
