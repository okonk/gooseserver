# DbDataReader Helper Implementation Plan

**Goal:** Make the 417 `Convert.To*(reader["col"])` row-mapping call sites in `Goose/` readable through five small extension methods that are exact 1:1 aliases of the `Convert` calls they replace — zero behavior change.

**Architecture:** One new static class (`Goose/DataReaderExtensions.cs`) with five expression-bodied methods that delegate to `Convert.To*` on the same `reader[column]` object. Call sites migrate in subsystem-sized batches, each gated by the fast suite plus the integration suite (real SQLite). The helper contract is pinned first by unit tests against the existing `FakeDbDataReader`. **Prerequisite:** the mechanical plan (`docs/plans/2026-08-25-modern-csharp-mechanical.md`) is complete — in particular its Task 3 (implicit usings), which this plan's new file relies on.

**Tech Stack:** .NET 10, `System.Data.Common.DbDataReader`, xUnit, existing `Goose.Tests.Fakes.FakeDbDataReader`.

---

## APIs verified

- **The conversion contract, empirically verified on the installed runtime (net10.0):**
  - `Convert.ToInt32(DBNull.Value)` → **throws `InvalidCastException`** (not 0).
  - `Convert.ToInt32((string)null)` → **0**.
  - `Convert.ToInt32("42")` → 42 (current-culture parse, `FormatException` on garbage).
  - `Convert.ToString(DBNull.Value)` → **`""`** (empty string, not null).
  - `Convert.ToString((object)null)` → **`""`** (empty string, not null — per the BCL contract for `Convert.ToString(Object)`).
  These are the *current production behaviors* of `Convert.ToInt32(reader["col"])` / `Convert.ToString(reader["col"])` for NULL and null cells (a SQLite NULL cell surfaces as `DBNull.Value`). The helpers preserve them exactly. "Cleaning up" any of these semantics (e.g. DBNull → 0, or null → `""` vs `null`) is a behavior change and is out of scope.
- Reader sites use `System.Data.Common.DbDataReader`: e.g. `Goose/Quests/Quest.cs:42` (`public static Quest FromReader(DbDataReader reader, Dictionary<int, Quest> quests)`), with `Convert.ToInt32(reader["id"])` at `:44` and `Convert.ToString(reader["name"])` at `:51`. **Verified migration inventory (grep `Convert\.To\w+(\s*\w+["` over `Goose/`):** 417 `Convert.To*(reader["..."])` sites = 237 `ToInt32` + 120 `ToString` + 56 `ToInt64` + 3 `ToDecimal` + 1 `ToDouble`. Do not confuse with two other quantities: 421 total `reader["..."]` indexer accesses (some feed non-`Convert` uses) and ~512 total `Convert.To*` calls (includes non-reader arguments such as `Convert.ToInt32(parts[0])` and `Convert.ToDateTime`). Task 1 re-measures per method for the baseline.
- Existing fake: `Goose.Tests/Fakes/FakeDbDataReader.cs:6` — name-indexed (`this[string]` served from a `Dictionary<string, object>`; a `null` value in the dictionary yields a null cell), every typed/ordinal accessor throws `NotSupportedException`. Currently driven by `Goose.Tests/QuestScriptLoadingTests.cs`. Its throwing accessors make it an adversarial trap: any future rewrite of the helpers to typed accessors (`GetInt32(ordinal)`) fails those tests loudly.
- No existing `GetInt32(string)` member or call site exists; extension resolution is unambiguous for `DbDataReader`-typed receivers (the instance `GetInt32(int)` does not match a string argument). Namespace lookup works from `Goose.Quests`/`Goose.Events` via enclosing-namespace rules (the extension lives in `Goose`).
- No `reader[non-literal]` call sites exist — every column name is a string literal, so the helper's `string column` parameter matches all sites.
- `Goose.sln` fast gate = Goose.Tests + Tools.Tests (per the mechanical plan's verified sln membership); `Goose.IntegrationTests` is separate and exercises real SQLite (per `docs/testing.md` placement rules).

## Invariants (must hold after every task)

1. `dotnet build Goose.sln` succeeds.
2. Fast suite: after Task 2 the total is mechanical-plan baseline + 10 (the 10 contract tests from Task 1); Tasks 3–4 keep that total. No existing test name disappears or becomes skipped.
3. Integration suite green after every migration batch.
4. No client-observable change: every migrated line is a pure rename to the alias; SQL text and packet strings byte-identical.
5. `this.` prefixes untouched; no comment rewrites except the one stale fake doc comment named in Task 3 (AGENTS.md obligation).

---

### Task 1: Contract tests (red)

**Files:**
- Create: `Goose.Tests/DataReaderExtensionsTests.cs`

**Step 1:** Write the 10 tests against the not-yet-existing extensions, using `Goose.Tests.Fakes.FakeDbDataReader` (constructor takes `Dictionary<string, object>`; index by column name). Per AGENTS.md, no comments in new test code — the adversarial intent is carried by the test names and the fake's throwing accessors:

```csharp
[Fact]
public void GetInt32_ReturnsValue()
[Fact]
public void GetInt32_OnDBNull_ThrowsInvalidCastException()
[Fact]
public void GetInt32_OnNullCell_ReturnsZero()
[Fact]
public void GetInt32_OnTextCell_Parses()
[Fact]
public void GetString_ReturnsValue()
[Fact]
public void GetString_OnDBNull_ReturnsEmptyString()
[Fact]
public void GetString_OnNullCell_ReturnsEmptyString()
[Fact]
public void GetInt64_ReturnsValue()
[Fact]
public void GetDecimal_ReturnsValue()
[Fact]
public void GetDouble_ReturnsValue()
```

Cell fixtures: `7`/`42L`/`4.5m`/`3.5d` for the value tests; `DBNull.Value` and **`null!`** entries in the dictionary for the DBNull/null tests (the `!` is required — the test project compiles with nullable enabled and `Dictionary<string, object>` rejects a plain `null` with CS8625; the suppression is deliberate, because the test intentionally stores a null cell to verify the legacy conversion behavior); the string `"42"` for the text-cell test. The three "ugly" behaviors (DBNull throws, null → 0, null/DBNull → `""`) are asserted *as-is* because they are today's production semantics.

**Step 2: Run to verify red.**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~DataReaderExtensions
```

Expected: fails to compile (extension methods missing).

---

### Task 2: Implement the helpers (green)

**Files:**
- Create: `Goose/DataReaderExtensions.cs`

**Step 1:** Implement exactly (no comments — the aliasing contract is the non-obvious part and is documented in this plan; the code is self-evident):

```csharp
using System.Data.Common;

namespace Goose
{
    internal static class DataReaderExtensions
    {
        public static int GetInt32(this DbDataReader reader, string column)
            => Convert.ToInt32(reader[column]);

        public static long GetInt64(this DbDataReader reader, string column)
            => Convert.ToInt64(reader[column]);

        public static string GetString(this DbDataReader reader, string column)
            => Convert.ToString(reader[column]);

        public static decimal GetDecimal(this DbDataReader reader, string column)
            => Convert.ToDecimal(reader[column]);

        public static double GetDouble(this DbDataReader reader, string column)
            => Convert.ToDouble(reader[column]);
    }
}
```

Contract: each method is a verbatim alias of the `Convert.To*` call it replaces, operating on the same `reader[column]` object. No DBNull special-casing, no culture change, no new exception types. The class is **`internal`**: the non-null contract of `GetString` (null and DBNull cells yield `""`) is proven for SQLite-supported cell values (null, `DBNull`, strings, numbers, `byte[]`), not for arbitrary `DbDataReader` implementations whose cells might carry objects whose `ToString()` returns null; `internal` scopes the API to the server and its tests, where that proof holds. `InternalsVisibleTo` for `Goose.Tests` and `Goose.IntegrationTests` already exists (`Goose/Goose.csproj:18-21`), so the tests compile unchanged. When the nullable plan enables `<Nullable>enable</Nullable>`, this file will emit CS8603 on `GetString` (verified: `Convert.ToString(object?)` is BCL-annotated `string?`); the nullable plan carries the named `!` fix with the proof. Do not add `#nullable` directives here.

**Step 2: Green.**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~DataReaderExtensions
```

Expected: all 10 pass.

**Step 3:** Build + full fast suite (invariants 1–2).

**Step 4:** Commit: `refactor: add DbDataReader conversion helpers with contract tests`

---

### Task 3: Fix the stale fake doc comment

**Files:**
- Modify: `Goose.Tests/Fakes/FakeDbDataReader.cs:4-6`

**Step 1:** The doc comment says "if a FromReader starts calling GetInt32/GetString, this fake should fail loudly". After Task 4, `FromReader` paths call the *extension* methods of those names, which route through the name indexer and do not throw; the fake still fails loudly only on direct typed/ordinal accessor calls. Update the comment to say exactly that. AGENTS.md: fix comments that become wrong on touched behavior.

**Step 2:** Group with Task 4's first commit (test-fixture-only change, no separate commit needed).

---

### Task 4: Migrate call sites in subsystem batches

**Files:** all `Goose/**/*.cs` containing `Convert.To*(reader["..."])` (417 sites after re-measure)

**Step 1 (baseline re-measure):** Record the per-method counts and require each to reach zero at the end:

```bash
for m in ToInt32 ToString ToInt64 ToDecimal ToDouble; do
  count=$(rg -o "Convert\\.${m}\\(reader\\[\"" Goose -g '*.cs' | wc -l)
  printf '%s %s\n' "$m" "$count"
done
```

Expected baseline: `ToInt32 237`, `ToString 120`, `ToInt64 56`, `ToDecimal 3`, `ToDouble 1` (command verified to produce exactly these counts). The `\[\"` is load-bearing — the pattern must match the literal `[` between the receiver name and the column-name quote, or it silently reports zero. Re-run this exact command for the final zero check.

**Step 2:** Migrate in three batches, one commit each. Within a batch: `Convert.ToInt32(reader["x"])` → `reader.GetInt32("x")`; same for `ToInt64`/`ToString`/`ToDecimal`/`ToDouble` with `reader[...]` arguments. Build + fast suite after each batch; **integration suite after each batch** (reader mapping feeds real-SQLite paths):

1. **Quests** — `Goose/Quests/*.cs` (Quest, QuestRequirement, QuestReward, QuestWindow row mapping).
2. **Player and guilds** — `Goose/Player.cs`, `Goose/Guild.cs`, `Goose/GuildHandler.cs`, `Goose/ClassHandler.cs`, `Goose/ChatFilter.cs` (the highest-risk persistence surface; integration suite matters most here).
3. **Everything else** — remaining `Goose/**/*.cs` sites (NPC, Item, events, etc.).

After each batch, verify the batch's diff contains only the rename (invariant 4) and that no `Convert.To*(reader[...])` site in the batch's scope remains.

**Step 3 (exclusions — do NOT touch in this plan):**
- `Convert.ToInt32(someString)` on non-reader strings (command-arg parsing in `Goose/Events/*`) — `int.Parse` is *not* equivalent (`int.Parse(null)` throws `ArgumentNullException` where `Convert.ToInt32(null)` returns 0); needs per-site null-precondition proof. Deferred.
- `Convert.ToDateTime` (`Goose/Player.cs:779`), `Convert.ToBase64String`, and any other non-`reader[...]`/non-int/long/string/decimal/double `Convert` use.

**Step 4 (final check):** Re-run the exact Step 1 command — every method must be 0 (the pattern's hardcoded `reader["` receiver excludes false positives like `Convert.ToInt32(parts[0])`; verified that all migration-site receivers are named `reader`). Full fast suite + integration suite green.

Commit messages: `refactor: migrate quest row mapping to DbDataReader helpers`, `refactor: migrate player/guild row mapping to DbDataReader helpers`, `refactor: migrate remaining row mapping to DbDataReader helpers`.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| DBNull cell → `InvalidCastException` through `GetInt32` | `GetInt32_OnDBNull_ThrowsInvalidCastException` (adversarial: fails for a helper using `reader.GetInt32(ordinal)`, which throws `NotSupportedException` under the fake and `InvalidCastException`-*differently* against real SQLite, and for a "cleaned up" helper returning 0) |
| Null cell → `0` through `GetInt32` | `GetInt32_OnNullCell_ReturnsZero` (fails for a helper that treats null like DBNull) |
| Text-stored numeric cell still parses | `GetInt32_OnTextCell_Parses` (fails for a helper using typed `GetInt32(ordinal)`, which throws `InvalidCastException` on a text cell) |
| Null/DBNull cells → `""` through `GetString` | `GetString_OnDBNull_ReturnsEmptyString`, `GetString_OnNullCell_ReturnsEmptyString` (adversarial: fail for a helper using `reader.GetString(ordinal)`, which throws under the fake) |
| No behavior change across migration | Fast suite (baseline+10 count, identity-stable) + integration suite green after every batch; per-batch diff review (invariant 4) |
| `GetString` returns non-null for SQLite cell values | `GetString_*` tests (null and DBNull cells → `""`); non-nullable `string` return type on an `internal` class, so the contract is scoped to the server's SQLite readers |

## Deferred (with reasons)

- **`Convert.ToInt32(string)` → `int.Parse`/`int.TryParse`:** null-semantics difference (verified: `Convert.ToInt32(null)` → 0, `int.Parse(null)` → `ArgumentNullException`). Follow-up with per-site null-precondition proof and new tests.
- **Changing the DBNull/null cell semantics themselves** (e.g. making NULL numeric columns yield 0 instead of throwing): a behavior change with data-quality implications; needs its own design and tests.
- **The CS8603 `!` on `GetString`:** required once the nullable plan enables `<Nullable>enable</Nullable>` (`Convert.ToString(object?)` is BCL-annotated `string?`; CS8603 verified). Carried as a named, proof-cited item in the nullable plan's row-mapping area — not added here, where nullable is still disabled.

## Execution notes

- Implement in the same worktree/branch as the mechanical plan (or a follow-up branch off it), per @using-git-worktrees.
- The migration is a pure rename: a scripted pass (`perl -pi -e 's/Convert\.ToInt32\(reader\["([^"]+)"\]\)/reader.GetInt32("$1")/g'` and siblings) is appropriate, but each batch's diff must be reviewed before the build gate.
- One commit per batch; the fake-comment fix rides along with batch 1.
