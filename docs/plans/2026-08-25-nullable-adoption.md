# Nullable Reference Types Adoption Plan

**Goal:** Enable `<Nullable>enable</Nullable>` for the `Goose` project and reach a zero-nullable-warning build without changing any runtime behavior.

**Architecture:** Inventory-first. The fix surface is unknown until the compiler runs, so this plan's first task captures the exact warning inventory and classifies it by responsibility area; a checkpoint then decides whether the fixes proceed under this plan or spawn a follow-up plan from the inventory. Fixes are annotation-only: no control-flow changes, no latent-bug fixes (those are recorded and deferred), so every commit is behavior-identical and gated by the full suites. **Prerequisite:** the mechanical plan and the DbDataReader helpers plan are complete, so the nullable diff does not interleave with other refactors and the `GetString` helper already carries its non-nullable `string` return type.

**Tech Stack:** .NET 10, nullable reference types (NRT), xUnit (fast gate: `Goose.sln`; integration project separate).

---

## APIs verified

- `Goose/Goose.csproj:3-7` — no `<Nullable>` today, no `TreatWarningsAsErrors` (nullable warnings will not break the build). The authoritative zero-warning check is the compiler's `nullable` warning group via `-p:WarningsAsErrors=nullable` (a documented special value of `WarningsAsErrors`): `dotnet build Goose.sln --no-incremental -p:WarningsAsErrors=nullable` must succeed. Verified empirically: with a CS8603 present, this flag turns it into a build error. **Trap:** `-p:NullableWarningsAsErrors=true` is silently ignored when passed via `-p:` (verified: build still succeeds with the warning) — do not use it. The group form is exact by construction — it covers every nullable diagnostic the compiler defines, including codes a hand-maintained grep list would miss (e.g. CS8597/CS8598 out/ref nullability mismatches).
- Three files opt in per-file: `Goose/Trie.cs:1`, `Goose/PropertiesDictionary.cs`, `Goose/PropertiesDictionaryJsonConverter.cs` (`#nullable enable`). No `#nullable disable` anywhere.
- Test/tool projects already compile with NRT enabled (`Goose.Tests`, `Goose.IntegrationTests`, `tools/*`) — enabling NRT in `Goose` can surface new warnings *in the test projects* where they consume now-annotated `Goose` types.
- Linked sources compiled into Goose.Tests: `TestSupport/TestWorldFixture.cs`, `TestSupport/ScriptStub.cs` (`Goose.Tests/Goose.Tests.csproj:30-31`) — in scope for fixes.
- `Goose/DataReaderExtensions.GetString` returns non-nullable `string` (verified contract: null and DBNull cells both yield `""` — see the helpers plan). Two consequences under NRT: (a) the **helper's own body emits CS8603** — `Convert.ToString(object?)` is BCL-annotated `string?` (verified: enabling NRT on the exact helper code produces CS8603) — fixed by a named, proof-cited `!` in Task 2; (b) **call sites need no `!`** — the return type is `string`, and some sites chain directly off the value (e.g. `Goose/Quests/Quest.cs:63` chains `.Split` off it).
- A `grep -E "warning (CS86|CS87|CS88)"` filter is **incomplete** — the official nullable-warning catalog includes codes outside that range (CS8597, CS8598, CS8599, …). Inventory capture therefore uses the full build log plus a superset range (`CS8[5-9]xx`) for triage, and the exact `-p:WarningsAsErrors=nullable` check above for the zero-proof. Also: never gate on a pipeline's exit status (`dotnet build | grep` hides a failed build, and `grep` exits 1 on zero matches) — build into a log, check its exit code, then search the log.
- Incremental builds suppress previously-reported warnings: the inventory build must use `--no-incremental`.
- `Goose.sln` fast gate = Goose.Tests + Tools.Tests; `Goose.IntegrationTests` separate (per the mechanical plan's verified sln membership and `docs/testing.md`).

## Invariants (must hold after every task)

1. `dotnet build Goose.sln` succeeds.
2. Fast suite green with the same test count and test identities as the pre-plan baseline (captured in Task 1). This plan adds no tests.
3. **No control-flow changes:** no new `if (x != null)` guards around previously unguarded dereferences, no new early returns, no `?? ""` / `?? default` substitutions. A warning is resolved by nullability annotation only: `?`, `!`, nullable generic arguments, and nullability attributes (`[MaybeNull]`, …). No `required`, no `init` — see Deferred.
4. **No latent-bug fixes:** where a warning reveals a genuine null-deref risk, the site is recorded (Task 4) and annotated to suppress the warning without changing behavior — the fix is a separate follow-up with its own tests.
5. Client-observable output byte-identical (invariants of the prior plans continue to hold).

---

### Task 1: Enable, capture, classify the inventory

**Files:**
- Modify: `Goose/Goose.csproj` (add `<Nullable>enable</Nullable>` — kept for the rest of the plan)
- Create: `docs/plans/2026-08-25-nullable-inventory.md` (the classified inventory — committed in Task 1 Step 5 as the first checkpoint, updated during Tasks 2–4, final update committed in Task 5)

**Step 1:** Add `<Nullable>enable</Nullable>` to `Goose/Goose.csproj`. Do **not** yet remove the three per-file `#nullable enable` directives (they are redundant but harmless; removing them is Task 5's diff).

**Step 2:** Capture the exact inventory. Build into a log and verify its exit code before searching (a pipeline would hide a failed build):

```bash
dotnet build Goose.sln --no-incremental > /tmp/nullable-inventory-build.txt 2>&1
echo "build exit: $?"
grep -E "warning CS8[5-9][0-9]{2}" /tmp/nullable-inventory-build.txt | sort > /tmp/nullable-inventory-raw.txt
wc -l /tmp/nullable-inventory-raw.txt
```

The `CS8[5-9]xx` range is a deliberate superset of the nullable diagnostics (it catches CS8597/CS8598-class codes a `CS86|87|88` filter misses); a handful of non-nullable codes in that range (e.g. CS8701/CS8702) may appear and are excluded during Step 4's classification. Record the count. Also capture the pre-plan test baseline if the prior plans' baseline file is unavailable (dedicated results directory + `LogFilePrefix`, as in the mechanical plan — a fixed `LogFileName` is overwritten across the two test projects):

```bash
dotnet test Goose.sln --no-restore --results-directory /tmp/goose-pre-nullable-trx --logger "trx;LogFilePrefix=pre" > /dev/null 2>&1
```

**Step 3: Checkpoint.** If the raw warning count exceeds **500**, stop here: commit nothing, revert the csproj change, and write a follow-up plan scoped from the raw inventory (e.g. split by area with per-area plans). Report the count and stop. Otherwise continue.

**Step 4:** Classify every warning into responsibility areas (file:line + warning code + area), writing `docs/plans/2026-08-25-nullable-inventory.md`:

1. **Model construction** — entity/model constructors, properties that must be set before use (`Player`, `NPC`, `Item`, quest models, …); annotated as non-nullable + `!` at the construction site where non-null is provable, or `T?` where absence is real.
2. **Database row mapping** — `FromReader`/`FromRow` paths and the former `Convert.ToString(reader[...])` sites (now `GetString`); the null-cell → `""` contract means most of these annotate cleanly as non-nullable. Includes the one named `!`: `DataReaderExtensions.GetString`'s body (`Convert.ToString(reader[column])!`) — proof: for every SQLite-supported cell value (null, `DBNull`, string, numeric, `byte[]`) `Convert.ToString` returns a non-null string; the class is `internal`, so no external `DbDataReader` implementation can reach it.
3. **Collections containing nullable slots** — `List<T?>`/`Dictionary<K, V?>` where absence is represented by null elements (inventory slots, optional targets).
4. **Packet/event inputs** — `Event.Data` unboxing sites, command-argument strings, packet payload parsing in `Goose/Events/*` and `Goose/Packets.cs`.
5. **Script-facing APIs** — the Roslyn scripting boundary (`ScriptHandler`, `GooseSettings` script surface) where nullability crosses into compiled scripts.
6. **Tests and fakes** — `Goose.Tests`, `Goose.IntegrationTests`, `TestSupport/*` (linked), fakes.

**Step 5:** Commit the csproj change **plus** the classified inventory doc as the first checkpoint commit: `chore: enable nullable reference types, capture warning inventory`. Every later commit in this plan is then self-contained: it builds on top of the enabling commit, and each area commit reduces the recorded warning count. The intermediate state builds with warnings — accepted, because `TreatWarningsAsErrors` is not set (APIs verified) and the suites gate behavior.

---

### Tasks 2–4: Fix phases by responsibility area

**Files:** per the classified inventory

One task per area group, in this order (each its own commit, message `refactor: nullable annotations (<area>)`):

- **Task 2:** areas 1 + 2 (models and row mapping) — the largest group; if its diff exceeds reviewability, split into two commits (models, then row mapping).
- **Task 3:** areas 3 + 4 (nullable collections and packet/event inputs).
- **Task 4:** areas 5 + 6 (script-facing APIs, tests and fakes).

Rules for every fix (invariants 3–4 are binding):
- Annotate: `T?`, `!` (only where non-null is provable by construction — cite the proof in the commit message when non-obvious), nullable generic arguments where absence is a real state, nullability attributes where the API contract warrants them. No `required`, no `init` (invariant 3; see Deferred).
- `GetString`: add the one named `!` in the helper body (Task 2, area 2); call sites need no `!` (non-nullable return type). Where a value is *expected* to be non-empty but the contract only guarantees `""`, leave it — do not add guards.
- Do not change signatures visible to compiled scripts (area 5) in a way that alters script-observed behavior; annotation-only changes are fine.
- Where a warning reveals a genuine latent null-deref: record it in the inventory doc's "Latent bugs (deferred)" section with file:line and a one-line description; annotate to suppress without changing behavior.
- After each task: build + fast suite green (invariants 1–2), and the warning count for that area is zero. Measure from a full log, checking the build's exit code first:

```bash
dotnet build Goose.sln --no-incremental > /tmp/nullable-phase-build.txt 2>&1
echo "build exit: $?"
grep -cE "warning CS8[5-9][0-9]{2}" /tmp/nullable-phase-build.txt
```

---

### Task 5: Zero-warning enforcement and cleanup

**Files:**
- Modify: `Goose/Trie.cs`, `Goose/PropertiesDictionary.cs`, `Goose/PropertiesDictionaryJsonConverter.cs` (remove now-redundant `#nullable enable`)
- Modify + commit: `docs/plans/2026-08-25-nullable-inventory.md` (final update — its "Latent bugs (deferred)" section was filled in during Tasks 2–4)

**Step 1:** Remove the three `#nullable enable` lines.

**Step 2:** Zero-warning proof — the compiler's own `nullable` warning group, exact by construction:

```bash
dotnet build Goose.sln --no-incremental -p:WarningsAsErrors=nullable
echo "exit: $?"
```

Expected: build succeeds, exit 0. (Any nullable diagnostic — including codes outside any grep range — fails the build; the flag's effect is verified in "APIs verified".)

**Step 3:** Full suites (the integration suite runs here, at final verification, not after every phase — the changes are annotation-only):

```bash
dotnet test Goose.sln --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Expected: green, test counts and identities matching the pre-plan baseline (re-run the Task 1 Step 2 TRX capture into `/tmp/goose-final-trx` and diff the extracted name+outcome lists, as in the mechanical plan's final verification).

**Step 4:** Commit: `refactor: nullable reference types complete (zero warnings)`

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| No runtime behavior change | Annotation-only rule (invariant 3) enforced by per-commit diff review; fast suite green at every commit with baseline-identical counts/identities; integration suite green at final verification (Task 5) — annotation-only changes cannot alter SQLite/packet behavior |
| Warning inventory is complete and classified | Task 1 Step 2 (`--no-incremental` full rebuild) + committed inventory doc |
| Scale is manageable before fixes start | Task 1 Step 3 checkpoint (500-warning threshold, revert-and-replan on breach) |
| `GetString` nullability is handled exactly once | Callers need no `!` (non-nullable return type, contract proven in the helpers plan); the helper body's CS8603 (verified) is fixed by the single named `!` in Task 2 with the SQLite-cell-value proof |
| Latent bugs are surfaced, not silently fixed | Inventory doc "Latent bugs (deferred)" section committed in Task 5; invariant 4 binds the fix phases |
| Zero warnings is real, not incremental-build luck or grep-range luck | Task 5 Step 2: `--no-incremental` plus `-p:WarningsAsErrors=nullable` (compiler-defined nullable group, not a code list; effect verified empirically) |

## Deferred (with reasons)

- **`required` / `init` modifiers:** not nullability annotations — `required` changes construction requirements for every consumer (including compiled scripts), and `init` restricts when a property may be assigned, breaking existing post-construction assignments. Both violate invariant 3. Revisit in a later API-modernization plan with its own behavior analysis.
- **Fixing the latent null-deref bugs the inventory reveals:** each is a behavior change requiring its own design and tests; recorded in the inventory doc, fixed in a follow-up.
- **`<Nullable>enable</Nullable>` for `CsvToSql.Core` / `tools/*`:** out of scope; the goal is the `Goose` server project. Revisit separately if desired.
- **Permanent `<WarningsAsErrors>nullable</WarningsAsErrors>` in the csproj:** consider as a follow-up once the codebase has been warning-clean for a while, so a transient build break doesn't mask a real regression.

## Execution notes

- Implement in the same worktree/branch lineage as the prior two plans, per @using-git-worktrees.
- The inventory doc is the load-bearing artifact: if Tasks 2–4 stall or the scope shifts, the inventory is sufficient to hand the work to a fresh plan or implementer.
- Do not run Tasks 2–4 against a stale build: always `--no-incremental` when measuring warning counts.
