# Testing

The tests are split into two projects with different speed profiles:

- `Goose.Tests` — the fast project. In `Goose.sln`. No fixture assets and no
  shipped game data except the single shipped quest script that
  `QuestScriptTests` compiles (kept in the fast project under the
  measured-behavior rule below): every other test builds a tiny synthetic
  world in memory.
- `Goose.IntegrationTests` — **not** in `Goose.sln`. Runs against the shipped
  Illutia game data (dimensions scripts, the workbook, recorded SQLite
  output), so it is an order of magnitude slower than the fast project.

## Commands

```bash
# fast project only
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore

# all solution unit-test projects
dotnet test Goose.sln --no-restore

# integration coverage
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

`dotnet test Goose.sln` therefore never runs the integration project. Both
test projects share the fixture sources linked from `TestSupport/` and the
production project references, and nothing else.

## Snapshot regeneration

`Goose.IntegrationTests/CsvToSqlSnapshotTests` compares the SQL generated from
the shipped workbook against `Fixtures/generated.snapshot`. To rewrite the
snapshot after an intentional change to the workbook or the generator:

```bash
GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot
```

The test then **fails on purpose** after writing the new snapshot, so the
regeneration is never silent; run the tests a second time without the flag to
verify against the new snapshot.

## Placement rules

- Tests that build a tiny synthetic world (inline scripts, small hand-made
  data) belong in `Goose.Tests`.
- Tests that load the shipped Illutia dimensions or any shipped game data, and
  the workbook/SQLite snapshot, belong in `Goose.IntegrationTests`.
- Real SQLite persistence behavior belongs in `Goose.IntegrationTests`:
  anything that opens a `System.Data.SQLite` database and runs the real
  INSERT/UPDATE/save strings through it (player first save, guild save,
  player properties, database transaction queue).
- The boundary is measured behavior, not file location or naming: if a test
  only exists because it "looks integration-ish", it stays a unit test until
  a measurement says otherwise.

## Baseline and final counts

Before the split (2026-08-25, `dotnet vstest --ListTests` on the single
`Goose.Tests` project): **518** discovered tests, wall clock ~75 s.

After the split:

| project | tests |
| --- | --- |
| `Goose.Tests` | 299 |
| `Goose.IntegrationTests` | 219 |
| total | 518 |

The total is unchanged: the moves are renames into the
`Goose.IntegrationTests` namespace, with no tests added or dropped.

Measured wall time of `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore`
after the split: 4.74 s / 4.73 s / 4.67 s over three runs (median **4.73 s**,
target < 5.0 s). Process wall time, not the xunit per-test duration.
