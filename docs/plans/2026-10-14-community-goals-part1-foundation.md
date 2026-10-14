# Community Goals Part 1 (Foundation) Implementation Plan

**Goal:** Goal definitions, live state, script API, contribution core, completion and run lifecycle, and persistence for community goals — everything below the UI and rewards.

**Architecture:** An imported `community_goals` sheet holds definitions (fixed columns + JSON `properties` + `script_path`). `CommunityGoalHandler` (game-thread only) caches each goal's `CommunityGoalDefinition` from `ICommunityGoalScript.Define`, owns runs and contributions in memory, and persists dirty rows to three live tables on the `WorldSavePeriod` cadence. Part 2 adds ranking/rewards/claims, Part 3 the player-facing flow.

**Tech Stack:** C# / .NET 10, SQLite (System.Data.SQLite), Roslyn C# scripting (`.csx`), System.Text.Json, xUnit.

Design: `docs/plans/2026-10-14-community-goals-design.md` (this is Part 1 of 4).

---

## APIs verified

- `GameWorld` ctor builds handlers `Goose/GameWorld.cs:207-240`; internal ctor with `Random` `:207`.
- `CreateDatabaseSchema` schema-file list `Goose/GameWorld.cs:249-251`; `MigrateDatabaseSchema` + `CreateTableIfMissing` `:274-285,312-325`.
- Load order: Global Scripts `:466`, World State `:468-472`, Community Chests `:474`, Players `:478`.
- `GameWorld.Stop` world-state sync flush `:533-541`, followed by the pending-writes wait.
- `GameWorld.EnqueueCompletion(Action)` `Goose/GameWorld.cs:85` (internal, returns false once stopping).
- `Database.Execute` `Goose/Database.cs:171`, `Execute<T>` `:196`, `EnqueueTransaction(action, onCommit, onSettled)` `:238`, `ExecuteTransaction` `:246`. `onCommit` runs only after COMMIT; `onSettled` gets the exception or null.
- `Event` base `Goose/Event.cs:7`, `Ticks` initialised to `Stopwatch.GetTimestamp()` `:22`; `EventHandler.AddEvent` `Goose/EventHandler.cs:295`. Re-arm pattern: `WorldState.AddSaveEvent` `Goose/WorldState.cs:543-550` (clamp period ≥ 1).
- `ScriptHandler.GetScript<T>(relativePath)` `Goose/Scripting/ScriptHandler.cs:17` — one cached instance per file. `Script<T>.Object` `Goose/Scripting/Script.cs:12`; script imports `:42-45`.
- `/reloadsql`: off-thread `CompileReferencedScripts` then game-thread loaders `Goose/Commands/ReloadSqlCommand.cs:16-47,53-66`.
- `/reloadscripts`: `PrepareReload` off-thread, `commit()` on game thread `Goose/Commands/ReloadScriptsCommand.cs:16-27`.
- CsvToSql: `TableSchema` `CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs:9-34`, registry `:41-66`; missing worksheet throws `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs:30-32`. `Col.Id/Int/Text`, `.Ref(sheet)`, `.HeaderText`, `.PrimaryKey()` (`Schema/Col.cs`, `Schema/Column.cs:52-70`). JSON column precedent `NpcSpawnsCsvToSql.cs:13`.
- Schema tests: `Goose.Tests/Schema/SchemaRegistryTests.cs` (`Has_twenty_one_tables`, `LegacyPairs`, `Every_id_column_either_references_a_sheet_or_is_a_known_exception`); snapshot `Goose.IntegrationTests/CsvToSqlSnapshotTests.cs` (regenerate with `GOOSE_UPDATE_SNAPSHOT=1`); `schema.js` drift guard (`tools/README.md:48-60`).
- Test helpers: `TestWorldFixture` `TestSupport/TestWorldFixture.cs` (no DB started; `CompileQuestScript` pattern `:53-59` writes under `DataDirectory/Scripts/...`), `PlayerFirstSaveTestBase(schemaFiles, generatedTables)` `Goose.IntegrationTests/PlayerFirstSaveTests.cs:20-42`, `SchemaDdl.ForAll` `TestSupport/SchemaDdl.cs:20`.
- `PlayerHandler.GetPlayerName(int)` `Goose/PlayerHandler.cs:191`.
- `JsonHelper.DatabaseOptions` `Goose/JsonHelper.cs` (DB JSON options).

## Conventions for every task

- No new comments/doc strings unless the "why" is non-obvious (`AGENTS.md`).
- Namespace `Goose.CommunityGoals`, folder `Goose/CommunityGoals/`.
- Everything in `CommunityGoalHandler` runs on the **game thread**. Only the persistence plan's SQL runs on the DB thread, and it reads only the captured plan.
- Time comes from `CommunityGoalHandler.UtcNow` (`internal Func<DateTime>`, default `() => DateTime.UtcNow`) so tests can drive it. Persist timestamps as ISO-8601 `"o"`.
- Run focused tests: `dotnet test Goose.Tests --filter FullyQualifiedName~CommunityGoal` / `dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CommunityGoal`.

---

### Task 0: Optional worksheets in CsvToSql

The importer throws when a registered sheet is missing (`CsvToSqlConverter.cs:30-32`). The fixture workbook and the live Google sheet lack "Community Goals", so the new table must be **optional**: missing sheet → emit DDL, no inserts.

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs` (`TableSchema` gains `bool Optional`, ctor param `optional = false`)
- Modify: `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs:28-41`
- Test: `Goose.IntegrationTests/Schema/OptionalSheetTests.cs` (build an in-memory `XLWorkbook` with every required sheet copied from `Fixtures/aspereta-data.xlsx`, or simply load the fixture and assert behaviour of a test-only optional schema via a small internal overload `ConvertWorkbook(Stream, IReadOnlyList<TableSchema>)`)

**Step 1: failing tests**
- Optional schema, sheet absent → output contains `CREATE TABLE` for it and no `INSERT INTO <table>`.
- Required schema, sheet absent → still throws `InvalidOperationException` naming the sheet (regression).

**Step 2: red** — `dotnet test Goose.IntegrationTests --filter FullyQualifiedName~OptionalSheet` fails (no overload / no flag).

**Step 3: implement** — `ConvertWorkbook(Stream)` delegates to the overload with `SchemaRegistry.Tables`. On a missing sheet: `if (!schema.Optional) throw ...;` else append `TableDdl.Emit(...)` only.

**Step 4: green**, then **commit** `feat(csvtosql): allow optional worksheets`.

---

### Task 1: `community_goals` importer

**Files:**
- Create: `CsvToSql/CsvToSql.Core/CommunityGoalsCsvToSql.cs`
- Modify: `CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs` (append after "Quest Rewards": `new TableSchema("Community Goals", "community_goals", new CommunityGoalsCsvToSql(), optional: true)`)
- Modify: `Goose.Tests/Schema/SchemaRegistryTests.cs` — rename `Has_twenty_one_tables` → `Has_twenty_two_tables` (22); add `"Community Goals -> community_goals"` to `LegacyPairs` (the pairs test at `:173-175` compares it against the whole registry; update its doc wording "21" if it states a count)
- Regenerate: `Goose.IntegrationTests/Fixtures/generated.snapshot`, `tools/DataEditor/schema.js`

Columns (worksheet order):

```csharp
Col.Id("id", SqlType.Integer).PrimaryKey(),
Col.Text("name"),
Col.Text("description", def: "''"),
Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
Col.Text("prerequisite_goals", def: "''"),
Col.Text("script_path", def: "''"),
Col.Text("properties", SqlType.Text, "''"),
```

**Steps:** registry test red (count) → add importer + registration → `dotnet test Goose.Tests --filter FullyQualifiedName~Schema` green → `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot`, **read the diff** (expect only a new empty `community_goals` table), rerun without the flag → `dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js` → `dotnet test tools/Tools.Tests` green → commit `feat(csvtosql): add community_goals sheet`.

**Deployment note (put in commit body):** the Google sheet needs a "Community Goals" worksheet before goals can be authored; until then the table is created empty.

---

### Task 2: Live tables

**Files:**
- Create: `Goose/sql/community_goals_state.sql`
- Modify: `Goose/GameWorld.cs:249-251` (add `"community_goals_state"` to the schema list), `Goose/GameWorld.cs:274-285` (three `CreateTableIfMissing` calls with identical column lists)
- Test: `Goose.IntegrationTests/CommunityGoalSchemaTests.cs` (clone `WorldStateSchemaTests.cs`: invoke `CreateDatabaseSchema` via reflection → tables exist; empty DB + `MigrateDatabaseSchema()` → tables exist; running migrate twice is a no-op)

```sql
CREATE TABLE IF NOT EXISTS community_goal_runs (
  goal_id INT NOT NULL, run INT NOT NULL, status TEXT NOT NULL,
  started_at TEXT NOT NULL, completed_at TEXT NULL,
  totals TEXT NOT NULL DEFAULT '{}', results TEXT NULL,
  PRIMARY KEY (goal_id, run));
CREATE TABLE IF NOT EXISTS community_goal_contributions (
  goal_id INT NOT NULL, run INT NOT NULL, player_id INT NOT NULL,
  amounts TEXT NOT NULL DEFAULT '{}', last_at TEXT NOT NULL,
  claimed_bracket INT NULL, claimed_at TEXT NULL,
  PRIMARY KEY (goal_id, run, player_id));
CREATE TABLE IF NOT EXISTS world_flags (
  name TEXT PRIMARY KEY, value INT NOT NULL, set_at TEXT NOT NULL, source_goal_id INT NULL);
```

**Persistence strategy:** automatic — `CreateTableIfMissing` on every startup covers existing DBs; fresh DBs get the file. No data migration.

Red → implement → green → commit `feat(community-goals): add live state tables`.

---

### Task 3: Model, definition and JSON parsing

Pure data + parser; no handler yet.

**Files (create in `Goose/CommunityGoals/`):**
- `CommunityGoal.cs` — `Id, Name, Description, NpcTemplateId, List<int> PrerequisiteGoals, string ScriptPath, string PropertiesJson, Script<ICommunityGoalScript>? Script` (script set in Task 5), `CommunityGoalDefinition? Definition`.
- `CommunityGoalDefinition.cs` — `IReadOnlyList<GoalRequirement> Requirements`, `IReadOnlyDictionary<string,double> ScoreWeights`, `RankingKind Ranking` (`Score`/`Lottery`), `IReadOnlyDictionary<string,long>? EntryCost`, `TimeSpan? Duration`, `DateTime? ClosesAt`, `RepeatRule? Repeat` (`OnClose` | `Every(TimeSpan)`), `TimeSpan? ClaimWindow`, `IReadOnlyList<GoalBracket> Brackets`, `IReadOnlyList<RewardSpec> CommunityRewards`. Helpers: `bool HasTargets`, `bool Repeats`, `TimeSpan? EffectiveClaimWindow` (null when not repeating; repeating default 30 days).
- `GoalRequirement.cs` — `Key, RequirementKind Kind` (`Item, Gold, Experience, Kill, Custom`), `long? Target`, `int? ItemTemplateId`, `int? NpcTemplateId`, `string? Label`. Static factories `Requirement.Item(key, itemId, target?)`, `.Gold(key, target?)`, `.Experience(key, target?)`, `.Kill(key, npcId, target?)`, `.Custom(key, label, target?)`.
- `GoalBracket.cs` — `int? Rank`, `double? MinScore`, `string? Measure`, `IReadOnlyList<RewardSpec> Rewards`; factories `Bracket.Rank(n, rewards)`, `Bracket.MinScore(score, rewards, measure?)`.
- `RewardSpec.cs` — `string Type`, `JsonObject Params` (whole entry). Behaviour arrives in Part 2.
- `CommunityGoalRun.cs` — `GoalId, Run, RunStatus Status` (`Active, Completed, Failed`), `StartedAt, CompletedAt?`, `Dictionary<string,long> Totals`, `JsonObject? Results`, `Dictionary<int, Contribution> Contributions`.
- `Contribution.cs` — `PlayerId, Dictionary<string,long> Amounts, DateTime LastAt, int? ClaimedBracket, DateTime? ClaimedAt`.
- `CommunityGoalDefinitionParser.cs` — `static CommunityGoalDefinition Parse(string json)`; `static IReadOnlyList<string> Validate(CommunityGoalDefinition def, bool customScript)` returning error strings.

JSON shape is exactly the design doc examples. Durations: `"7d"`, `"12h"`, `"30m"` (`d|h|m` suffix, positive int). `repeat`: `"onClose"` or `{ "every": "7d" }`.

Validation errors (each its own test):
- unknown requirement `type`; missing/duplicate `key`; `Item` without `item`; `Kill` without `npc`;
- `score` or bracket `measure` referencing a missing key;
- bracket with neither `rank` nor `minScore`, or both;
- `ranking: "lottery"` without `entryCost`, or `entryCost` key not a requirement;
- no termination: `!HasTargets && Duration is null && ClosesAt is null && !customScript`;
- malformed duration / JSON (`Parse` throws `CommunityGoalDefinitionException` with the message).

Unknown **reward** types are validated in Part 2 (the reward registry lives there); here rewards parse structurally (must be objects with a string `type`).

**Tests:** `Goose.Tests/CommunityGoals/CommunityGoalDefinitionParserTests.cs` — both design examples parse to the expected definition (round-trip of every field); one test per validation rule; adversarial: a lottery whose only termination is `duration` is valid, a milestone with targets and no duration is valid, a target-less goal with neither is rejected unless `customScript: true`.

Red → implement → green → commit `feat(community-goals): add goal model and definition parser`.

---

### Task 4: Script API

**Files:**
- Create: `Goose/Scripting/ICommunityGoalScript.cs` (signatures exactly as the design doc §Script API; `CommunityGoalBracket` there is `GoalBracket`).
- Create: `Goose/Scripting/BaseCommunityGoalScript.cs`:
  - `Define` → `CommunityGoalDefinitionParser.Parse(goal.PropertiesJson)`.
  - `CanContribute` → null; `OnContributed`/`OnCompleted` → no-op; `IsComplete` → false.
  - `GetSummaryText` → targeted goals: integer percent of `Σ min(total,target) / Σ target` over targeted requirements (`"72%"`); otherwise the first requirement's total with label (`"184,000 gold"`, `N0` formatting).
  - `GetBracket` → `null` (Part 2 replaces).
- Modify: `Goose/Scripting/Script.cs:42-45` add `"Goose.CommunityGoals"` to `WithImports`.
- Modify: `TestSupport/TestWorldFixture.cs` add `CompileCommunityGoalScript(string body, string fileName = "G.csx")` mirroring `CompileQuestScript` (`:53-59`, folder `Scripts/CommunityGoal`).

**Tests** (`Goose.Tests/CommunityGoals/CommunityGoalScriptTests.cs`): base `Define` returns parsed definition; summary text for targeted (72%) and untargeted goals; a compiled custom `.csx` deriving `BaseCommunityGoalScript` that overrides `Define` using `Requirement.Custom(...)` compiles **without** a `using Goose.CommunityGoals;` line (proves the import).

Commit `feat(community-goals): add community goal script API`.

---

### Task 5: Handler — loading, definitions, kill index

**Files:**
- Create: `Goose/CommunityGoals/CommunityGoalHandler.cs`
- Modify: `Goose/GameWorld.cs` — property `public CommunityGoalHandler CommunityGoals { get; private set; }`, constructed beside `ChestHandler` (`:229`); `LoadStep("Community Goals", ...)` **after** World State (`:468-472`) and **before** Community Chests (`:474`) so Part 2's chest-page flags exist when chests size their containers.
- Test: `Goose.Tests/CommunityGoals/CommunityGoalHandlerLoadTests.cs`, `Goose.IntegrationTests/CommunityGoalLoadTests.cs`

API:

```csharp
public IReadOnlyDictionary<int, CommunityGoal> Goals { get; }
public void Load(GameWorld world);                       // definitions + state + AddSaveEvent + AddTickEvent
public void LoadDefinitions(GameWorld world);            // community_goals rows → goals; re-Define all
public void Redefine(GameWorld world);                   // re-run Define for every goal (reload hooks)
public CommunityGoalRun? CurrentRun(int goalId);
public IEnumerable<CommunityGoal> GoalsForNpc(int npcTemplateId);
internal void LoadDefinitionRows(IEnumerable<CommunityGoal> rows, GameWorld world);
internal void LoadStateRows(IEnumerable<CommunityGoalRun> runs, IEnumerable<(int GoalId, int Run, Contribution C)> contributions, IEnumerable<WorldFlag> flags);
internal Func<DateTime> UtcNow { get; set; }
```

Rules:
- Script: empty `script_path` → a single shared `BaseCommunityGoalScript` instance; otherwise `world.ScriptHandler.GetScript<ICommunityGoalScript>(path)`.
- `Define` + `Validate` per goal inside try/catch; any failure logs `log.Error` with goal id and **skips that goal** (not added to `Goals`); other goals load.
- Kill index `Dictionary<int npcTemplateId, List<(CommunityGoal, GoalRequirement)>>` rebuilt after every (re)definition.
- State rows whose goal id is not in `Goals`: log a warning once per goal id, keep them in memory so they are not deleted on save (they are simply unreachable).
- `world_flags` load into `Dictionary<string, WorldFlag>` (`Name, Value, SetAt, SourceGoalId`), with `public int GetFlag(string name)` (0 when absent). Part 2 writes flags.

**Publication boundary:** build the new goal dictionary and kill index fully, then swap both references in one step; a failed `LoadDefinitions` leaves the previous dictionary/index untouched.

**Tests:** valid rows load and index kills; invalid row skipped, siblings load; orphan state rows survive in memory and are logged; `Redefine` after editing `PropertiesJson` changes the cached definition and kill index; a throwing `Define` during `Redefine` keeps the old definition for that goal (adversarial). Integration: rows inserted into real `community_goals` + state tables load through `Load`.

Commit `feat(community-goals): load goals and state`.

---

### Task 6: Contribute and completion

**Mutation impact:**
- Source of truth: `CommunityGoalRun.Totals`, `Contribution.Amounts/LastAt`, `CommunityGoalRun.Status/CompletedAt` (Task 3 model).
- Readers: Part 3 windows (`GetSummaryText`, requirement lines), Part 2 ranking, persistence plan (Task 8).
- Derived state: dirty sets (`dirtyRuns`, `dirtyContributions`) — every mutation must add the key.
- Propagation: mutate → mark dirty → `script.OnContributed` → `CheckCompletion` → (if completing) set status/time, mark run dirty, `script.OnCompleted`, raise `RunCompleted` event (Part 2/3 subscribe for rewards/announcements).
- Invariants: no mutation when refused or inactive; totals never exceed target for targeted requirements; a run completes at most once.

**Files:** modify `CommunityGoalHandler.cs`; test `Goose.Tests/CommunityGoals/CommunityGoalContributeTests.cs`.

API:

```csharp
public ContributeResult Contribute(CommunityGoal goal, Player? player, string key, long amount, GameWorld world);
public long Remaining(CommunityGoal goal, string key);   // long.MaxValue when untargeted, 0 when inactive
public void CheckCompletion(CommunityGoal goal, GameWorld world);
public event Action<CommunityGoal, CommunityGoalRun, GameWorld>? RunCompleted;
```

`ContributeResult` = `(long Accepted, string? Refusal)`. Steps exactly as design §Contribution flow: inactive → `(0, "This goal has ended.")`; `CanContribute` refusal → `(0, msg)`; clamp to `Remaining`; `amount <= 0` after clamp → `(0, "That's no longer needed.")`; apply; `OnContributed`; `CheckCompletion`.

`CheckCompletion` (active run only):
- `targetsMet` = every targeted requirement total ≥ target (false when none targeted).
- `timedOut` = `ClosesAt <= now` or `StartedAt + Duration <= now`.
- `scriptDone` = `script.IsComplete`.
- `targetsMet || scriptDone` → `Completed`; else `timedOut` → `HasTargets ? Failed : Completed`.
- Raise `RunCompleted` only for `Completed`. Failed runs mark dirty and log.

**Tests:** clamping (4,990/5,000 + 50 → accepts 10); refusal leaves totals/contributions/dirty sets unchanged; untargeted requirement never clamps; reaching target completes once and raises `RunCompleted` once (adversarial: a second contribution after completion is refused and does not re-raise); timeout with unmet targets → `Failed`, no event; lottery-style untargeted + duration timeout → `Completed`; `IsComplete` script override completes; `Contribute` with `player == null` updates totals only.

Commit `feat(community-goals): contribute and complete runs`.

---

### Task 7: Run lifecycle and tick event

**Files:** create `Goose/Events/CommunityGoalTickEvent.cs` (clone of `WorldSaveEvent.cs`, calls `world.CommunityGoals.Tick(world)` and re-arms every 60s in a `finally`, like `WorldState.Save` `:514-524`); modify handler; test `Goose.Tests/CommunityGoals/CommunityGoalLifecycleTests.cs`.

`Tick(world)` in order:
1. `CheckCompletion` for each active run (time-based).
2. Start runs: for each goal with **no runs**, start run 1 when every prerequisite goal's **run 1** is `Completed` (missing prerequisite goal → not started, log once). For repeating goals whose current run is finished: `OnClose` → start next immediately; `Every(span)` → start when `currentRun.StartedAt + span <= now`.
3. Expiry: for repeating goals, for each finished run where `CompletedAt + EffectiveClaimWindow <= now` and contributions remain → remove all its contributions and mark them for delete.

Also call step 2 for the affected goal immediately inside `RunCompleted` handling for `OnClose` (so the next run opens without waiting 60s) — implement as: after completion in `CheckCompletion`, if `Repeat is OnClose` start next run.

`StartRun(goal)`: `run = (current?.Run ?? 0) + 1`, `Status = Active`, `StartedAt = now`, empty totals; mark dirty.

**Tests:** goal without prerequisites starts on first tick; prerequisite chain starts goal B only after A run 1 completes; `Failed` A blocks B (documents limitation); `onClose` opens run 2 immediately on completion; `every: 7d` opens run 2 only after 7 days from run 1's start; expiry removes contributions after 30 days for repeating goals and never for one-run goals; tick is idempotent (two ticks at same `now` start one run).

Commit `feat(community-goals): run lifecycle and tick`.

---

### Task 8: Persistence

**Mutation impact:**
- Source of truth: in-memory runs/contributions/flags; DB rows are a lagging copy.
- Propagation: game thread builds an immutable `SavePlan` (row values serialized to JSON strings with `JsonHelper.DatabaseOptions`, contribution deletes) and **clears** the dirty sets; DB thread executes the plan in one `EnqueueTransaction`; on `onSettled(exception != null)` re-mark the plan's keys dirty via `world.EnqueueCompletion` (game thread).
- Why no single-flight (unlike `WorldState`): each plan carries full row values and the DB queue is FIFO, so a later plan always reflects later state; deletes are only issued for contributions already removed from memory, and a removed contribution never reappears (expiry is terminal).
- Invariants: completion status and Part 2 flags written in the same plan land in one transaction; nothing in SQL touches live objects.

**Files:** create `Goose/Events/CommunityGoalSaveEvent.cs` (re-arm every `WorldSavePeriod`, clamp ≥ 1); modify handler (`Save(world)`, `SaveSync(world)`, `internal SavePlan PlanSave()`); modify `Goose/GameWorld.cs` `Stop` — `CommunityGoals.SaveSync(this)` in its own try/catch right after the world-state flush (`:533-541`); test `Goose.IntegrationTests/CommunityGoalPersistenceTests.cs` (base `PlayerFirstSaveTestBase(["players", "community_goals_state"], ["community_goals"])`).

SQL: `INSERT ... ON CONFLICT(goal_id, run) DO UPDATE SET ...` for runs; same with `(goal_id, run, player_id)` for contributions; `INSERT ... ON CONFLICT(name) DO UPDATE` for flags; `DELETE FROM community_goal_contributions WHERE goal_id=@g AND run=@r AND player_id=@p` for deletes.

`SaveSync` = `PlanSave` + `Database.ExecuteTransaction` (same as `WorldState.SaveSync` `:526-541`, without the in-flight spin).

**Tests:** contribute → save → new `GameWorld` on same file → `Load` → totals, amounts, `LastAt`, status round-trip; unchanged state produces an empty plan (no SQL); expiry deletes rows; a plan whose transaction fails (drop `community_goal_runs` before save) re-dirties its keys and a later save (after recreating the table) writes them (adversarial); `Stop` flushes unsaved contributions.

Commit `feat(community-goals): persist goal state`.

---

### Task 9: Reload hooks

**Files:** modify `Goose/Commands/ReloadSqlCommand.cs`, `Goose/Commands/ReloadScriptsCommand.cs`, handler; test `Goose.IntegrationTests/CommunityGoalReloadTests.cs` (follow `ReloadSqlCommandTests.cs`).

- `/reloadsql`: in `CompileReferencedScripts` (`:53-66`) add `ScriptPaths(conn, "community_goals")` compiled as `ICommunityGoalScript`, (Part 2 adds reward scripts referenced from `properties`). In the game-thread block, call `world.CommunityGoals.LoadDefinitions(world)` after `QuestHandler.LoadQuests`.
- `/reloadscripts`: after `commit()` (`:24`) call `world.CommunityGoals.Redefine(world)`.

**Tests:** editing a goal row's `properties` then `/reloadsql` changes the definition and keeps run progress; editing a goal script file then `/reloadscripts` changes `Define` output; a broken script on reload leaves the previous definition (adversarial).

Commit `feat(community-goals): reload goals with sql and scripts`.

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Missing optional sheet imports as empty table; required still throws | `OptionalSheetTests` |
| Invalid goal never loads, siblings unaffected | `CommunityGoalHandlerLoadTests` invalid-row test |
| Totals never exceed targets | clamping test |
| Refused contribution mutates nothing | refusal test (asserts dirty sets too) |
| Run completes exactly once | second-contribution adversarial test |
| Timeout with unmet targets fails, lottery completes | two timeout tests |
| Prerequisites gate on run 1 Completed | chain + Failed-blocks tests |
| Failed save re-dirties and later persists | adversarial persistence test |
| Progress survives definition reload | reload tests |

## Final checks

- `dotnet test Goose.Tests` and `dotnet test Goose.IntegrationTests` and `dotnet test tools/Tools.Tests` all green.
- Design alignment: run statuses, duration syntax, claim-window defaults (30d repeating / never one-run), load order before chests — match the design doc.
