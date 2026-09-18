# Modifier Candidate Pool Implementation Plan

**Goal:** Make title and surname row chances independent candidate rolls, then uniformly choose at most one successful modifier from each pool.

**Architecture:** Preserve the outer title/surname gates and existing application path in `RollTitleAndSurname`. Replace `RollModifier`'s normalized weighted ranges with an independently rolled candidate list and one uniform index selection. Repair unrelated help tests by calculating their expected visible counts from the real command registry.

**Tech Stack:** C# 14, .NET 10, xUnit

---

## APIs verified

- `ItemHandler.RollTitleAndSurname` owns the outer gates and applies the selected modifier to item name, properties, and stats: `Goose/ItemHandler.cs:313-353`.
- `ItemHandler.RollModifier` currently filters through `ModifierAppliesToItem` and converts chances into weighted ranges: `Goose/ItemHandler.cs:355-380`.
- `GameWorld.RollChance(double)` accepts probability fractions: `Goose/GameWorld.cs:715-718`.
- Applicable-item filtering is defined by `ItemModifier.ModifierAppliesToItem`: `Goose/ItemModifier.cs:51-66`.
- Help renders each section's visible command count from `CommandRegistry.IsUsableBy`: `Goose/Commands/HelpFormatter.cs:62-76`.
- Tests can derive real sections through `CommandRegistry.Sections`, whose commands are the published command definitions: `Goose/Commands/CommandRegistry.cs:175-205` and `Goose/Commands/CommandRegistry.cs:347-356`.

### Task 1: Make help integration expectations data-driven

**Files:**
- Modify: `Goose.IntegrationTests/CommandFrameworkTests.cs:32-51`
- Modify: `Goose.IntegrationTests/Part2MigrationTests.cs:28-39,132-157`
- Modify: `Goose.IntegrationTests/Part3MigrationTests.cs:19-20,104-148`

**Mutation impact:**
- Source of truth changed: test expectations only; built-in commands remain owned by `CommandRegistry`.
- Important readers: the three integration-test classes listed above.
- Derived/cached state affected: no runtime derived state.
- Required propagation sequence: create expected labels from `registry.Sections`, count only definitions accepted by `CommandRegistry.IsUsableBy(player, definition)`, then compare rendered help lines with those labels.
- Invariants to preserve: normal users cannot see privileged sections; game masters see all built-in sections; migrated commands remain listed.
- Observable proof required: the four previously failing tests pass without hardcoded command totals.

**Steps:**
1. Add a small test helper that returns `"{section.Name} ({visibleCount})"` from the actual registry and player.
2. Replace hardcoded section-count labels while retaining exact section-name and access assertions.
3. Run the four previously failing tests and expect all to pass.
4. Commit as `Fix help tests to use registered command counts`.

| Invariant | Proved by |
|-----------|-----------|
| Normal users do not see GM/Admin sections | `Help_hides_privileged_sections_from_normal_players`, `Help_normal_sees_only_the_open_sections` |
| GM users see every built-in section | `Help_gm_sees_all_seven_builtin_sections` |
| Migrated player commands remain discoverable | `Help_lists_migrated_commands_for_gm_and_hides_nothing_new_for_normal` |

### Task 2: Select one independently rolled modifier candidate

**Files:**
- Create: `Goose.Tests/ItemModifierSelectionTests.cs`
- Modify: `Goose/ItemHandler.cs:355-380`
- Modify: `Goose/GameWorld.cs:79-104`
- Modify: `Goose.IntegrationTests/DimensionModifierTests.cs:19-34`
- Modify: `reports/game_balance/generate_report.py:1400,1504-1506`
- Modify: `reports/game_balance/report_renderer.py:700`
- Modify: `reports/game_balance/leveling-balance-report.html:16`

**Mutation impact:**
- Source of truth changed: the selection algorithm in `ItemHandler.RollModifier`; authored `ItemModifier.Chance` remains loaded from `item_titles` and `item_surnames` by `ItemModifier.FromReader` at `Goose/ItemModifier.cs:22-48`.
- Important readers: `RollTitleAndSurname` at `Goose/ItemHandler.cs:313-353`, invoked for drops, inventory combinations, purchases, and quest rewards.
- Derived/cached state affected: no caches. The selected modifier still propagates through the existing item name mutation, `TitleId`/`SurnameId` property assignment, and `ApplyStats` call. The generated balance-report narrative must describe candidate chances rather than weighted outcomes.
- Required propagation sequence: filter applicable rows, independently call `world.RollChance(modifier.Chance)` for each, return null for no successes, otherwise call `world.Random.Next(candidates.Count)` once and return that candidate; the unchanged caller applies it. Add an internal constructor seam so tests can supply a deterministic `Random` without changing production construction.
- Invariants to preserve: inapplicable and zero-chance rows never enter the pool; at most one modifier per title/surname pool is applied; every successful candidate has equal winner probability; title and surname outer gates remain unchanged.
- Observable proof required: a partial-chance singleton sometimes applies and sometimes yields none; two guaranteed candidates both win across repeated real-domain rolls, while each item has only one modifier property.

**Steps:**
1. Add deterministic regression tests using real `GameWorld`, `ItemTemplate`, `Item`, and `ItemModifier` objects with a sequence-driven `Random`. Confirm the singleton partial-chance test fails because the old implementation always picks it.
2. Add an internal `GameWorld` constructor overload that accepts a `Random`; keep the public constructor behavior unchanged.
3. Replace weighted ranges with a candidate list and uniform selection.
4. Remove the stale dimension-test comment and verify both script-owned title chances as well as surname chances remain zero.
5. Update the balance-report source and generated narrative to describe candidate admission and uniform winner selection.
6. Run `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~ItemModifierSelectionTests` and expect all tests to pass.
7. Validate the checked-in report with `assert_report_invariants`, then run `dotnet test Goose.sln --no-restore` and expect the full solution to pass.
8. Commit as `Fix title and surname candidate selection`.

| Invariant | Proved by |
|-----------|-----------|
| A row chance controls candidate admission rather than normalized weight | Partial-chance singleton test observes both selected and empty outcomes |
| Successful candidates receive equal winner treatment | Two guaranteed-candidate distribution test observes both within broad symmetric bounds |
| Only one modifier is applied per pool | Every generated item has at most one modifier ID and one resulting modifier name |
| Existing outer gates and application path remain intact | Existing `ItemScriptHookTests` and full solution suite |

No persistence or schema migration is needed. No runtime registry publication or lifecycle boundary changes.
