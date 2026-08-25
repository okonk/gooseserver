# Fast Unit-Test Boundary Implementation Plan

**Goal:** Make `Goose.Tests` complete in under five seconds by moving dimensions and CSV snapshot integration coverage into an explicitly invoked project while preserving every test.

**Architecture:** `Goose.sln` remains the unit-test boundary. A new `Goose.IntegrationTests` project owns the canonical dimensions-script and workbook/SQLite coverage but is deliberately omitted from the solution. Shared lightweight world construction is linked into both test assemblies without adding a test-utilities assembly.

**Tech Stack:** .NET 10, xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, Roslyn scripting, System.Data.SQLite, MSBuild project references and linked content.

---

## APIs verified

- `Goose.Tests` currently lacks `IsTestProject` and copies all fixtures plus 17 dimensions scripts: `Goose.Tests/Goose.Tests.csproj:3-65`.
- `Goose` grants internal access only to `Goose.Tests`: `Goose/Goose.csproj:17-21`.
- Windows service support consists of the package and compile metadata in `Goose/Goose.csproj:29-48`, the branch in `Goose/Program.cs:26-34`, and the two `GooseWindowsService` source files.
- The dimensions fixture copies canonical output scripts into a unique temporary data directory and compiles them through the world `ScriptHandler`: `Goose.Tests/Fixtures/GlobalScriptFixture.cs:21-118`.
- Its lightweight map/player/item/spell/class helpers are in `Goose.Tests/Fixtures/GlobalScriptFixture.cs:121-271`, and disposal restores static settings and deletes the temp directory at `Goose.Tests/Fixtures/GlobalScriptFixture.cs:274-278`.
- Snapshot regeneration uses `CallerFilePath` to locate the source fixture and refuses to pass after rewriting it: `Goose.Tests/CsvToSqlSnapshotTests.cs:38-76`.
- Snapshot execution owns and deletes its temporary SQLite database: `Goose.Tests/CsvToSqlSnapshotTests.cs:120-186`.

## Baseline invariants

- `dotnet test Goose.sln` must execute unit-test projects only.
- `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj` must be the only normal command that executes the moved dimensions and CSV snapshot tests.
- The combined discovered test count must not decrease.
- Test names and assertions move unchanged unless fixture type names or namespaces require a mechanical update.
- Canonical `.csx`, workbook, and snapshot contents must have one source copy.
- This plan does not move dimensions logic into compiled C# and does not introduce global compiled-script caching.

### Task 1: Capture the baseline and fix .NET 10 test discovery

**Files:**

- Modify: `Goose.Tests/Goose.Tests.csproj:3-8`
- Record during implementation: `/tmp/goose-tests-before.txt`

**Step 1: Capture the pre-change assembly baseline**

Build the current test project, then invoke the built test assembly directly because the missing `IsTestProject` prevents the normal project command from executing tests:

```bash
dotnet build Goose.Tests/Goose.Tests.csproj
dotnet vstest Goose.Tests/bin/Debug/net10.0/Goose.Tests.dll --ListTests > /tmp/goose-tests-before.txt
dotnet vstest Goose.Tests/bin/Debug/net10.0/Goose.Tests.dll
```

Expected: the assembly run passes; `/tmp/goose-tests-before.txt` contains every currently discovered test. Record its count and total duration in the implementation notes.

**Step 2: Demonstrate the discovery defect (red)**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --list-tests --verbosity diagnostic
```

Expected: the diagnostic output says the project is skipped because it has no `IsTestProject` property.

**Step 3: Declare the project as a test project**

Add this property beside `IsPackable`:

```xml
<IsTestProject>true</IsTestProject>
```

Do not change package versions in this task.

**Step 4: Verify project-based discovery (green)**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --list-tests
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Expected: the project command discovers the same count captured through `dotnet vstest`, and all tests pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Project invocation runs tests instead of succeeding with zero tests | `dotnet test ... --list-tests` matches `/tmp/goose-tests-before.txt` |
| Discovery fix does not alter behavior | Full `dotnet test Goose.Tests/Goose.Tests.csproj` passes |

**Step 5: Commit**

```bash
git add Goose.Tests/Goose.Tests.csproj
git commit -m "test: declare Goose.Tests as a test project"
```

### Task 2: Remove obsolete Windows service hosting

**Files:**

- Delete: `Goose/GooseWindowsService.cs`
- Delete: `Goose/GooseWindowsService.Designer.cs`
- Modify: `Goose/Goose.csproj:29-48`
- Modify: `Goose/Program.cs:1-36`
- Test: `Goose.Tests/GameServerStartupTests.cs`

**Step 1: Capture the structural red state**

Run:

```bash
rg -n "GooseWindowsService|ServiceBase|System.ServiceProcess|System.ServiceProcess.ServiceController" Goose
```

Expected: matches in `Program.cs`, `Goose.csproj`, and both service files.

**Step 2: Remove service support**

- Delete both service files.
- Remove `using System.ServiceProcess;` and the complete `if (args.Contains("-service"))` branch from `Program.Main`.
- Remove the `System.ServiceProcess.ServiceController` package reference.
- Remove the `Compile Update` item group for the service and designer.
- Leave `GameServer.Run`, Ctrl+C, SIGTERM, and `--datadir` handling unchanged.
- Do not add replacement hosting code or a compatibility shim for `-service`.

Historical documents under `docs/plans/` and `docs/code-review-*.md` remain unchanged.

**Step 3: Build and run startup coverage**

Run:

```bash
dotnet build Goose/Goose.csproj --no-restore
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~GameServerStartupTests
rg -n "GooseWindowsService|ServiceBase|System.ServiceProcess|System.ServiceProcess.ServiceController" Goose
```

Expected: build and focused tests pass; the final `rg` returns no matches.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Normal executable hosting still builds | `dotnet build Goose/Goose.csproj` |
| Existing startup failure behavior remains | `GameServerStartupTests` |
| No dormant Windows service dependency remains | zero-result structural `rg` |

**Step 4: Commit**

```bash
git add Goose/Goose.csproj Goose/Program.cs Goose/GooseWindowsService.cs Goose/GooseWindowsService.Designer.cs
git commit -m "refactor: remove Windows service hosting"
```

### Task 3: Create the explicit integration project and move the CSV snapshot

**Files:**

- Create: `Goose.IntegrationTests/Goose.IntegrationTests.csproj`
- Move: `Goose.Tests/CsvToSqlSnapshotTests.cs` to `Goose.IntegrationTests/CsvToSqlSnapshotTests.cs`
- Create: `Goose.IntegrationTests/Fixtures/CsvToSqlSnapshotFixture.cs`
- Move: `Goose.Tests/Fixtures/aspereta-data.xlsx` to `Goose.IntegrationTests/Fixtures/aspereta-data.xlsx`
- Move: `Goose.Tests/Fixtures/generated.snapshot` to `Goose.IntegrationTests/Fixtures/generated.snapshot`
- Modify: `Goose/Goose.csproj:17-21`
- Modify: `Goose.Tests/Goose.Tests.csproj:17-19`
- Verify unchanged: `Goose.sln`

**Step 1: Create the project file**

Create the integration project with the same test package versions as `Goose.Tests`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>
  <ItemGroup>
    <None Include="Fixtures/**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Goose\Goose.csproj" />
    <ProjectReference Include="..\CsvToSql\CsvToSql.Core\CsvToSql.Core.csproj" />
  </ItemGroup>
</Project>
```

Do not add this project to `Goose.sln`.

**Step 2: Grant integration tests internal access**

Add a second `InternalsVisibleTo` assembly attribute in `Goose/Goose.csproj` for `Goose.IntegrationTests`. Preserve the existing `Goose.Tests` attribute.

**Step 3: Move the snapshot test and extract its fixture**

- Move the test and both fixture files with history.
- Change the test namespace to `Goose.IntegrationTests`.
- Move workbook loading, SQLite execution/rendering, expected-snapshot reading, and source-snapshot writing into `CsvToSqlSnapshotFixture`.
- Keep environment-variable branching and assertions in `CsvToSqlSnapshotTests`.
- Preserve `GOOSE_UPDATE_SNAPSHOT`, `CallerFilePath`, first-difference reporting, and deletion behavior exactly.
- Narrow `Goose.Tests` fixture copying so it no longer expects the moved workbook or snapshot. Other fixture files still needed by unit tests must continue to copy.

Use the approved fixture contract:

```csharp
public sealed class CsvToSqlSnapshotFixture : IDisposable
{
    public string SourceSnapshotPath { get; }

    public CsvToSqlSnapshotFixture();
    public string GenerateSnapshot();
    public string ReadExpectedSnapshot();
    public void RegenerateSnapshot(string contents);
    public void Dispose();
}
```

The fixture source file lives inside `Goose.IntegrationTests/Fixtures`, so its `CallerFilePath`-derived source path must combine the declaring file's directory directly with `generated.snapshot`; it must not append a second `Fixtures` segment. `GenerateSnapshot` must keep the existing `try/finally` deletion of its temporary SQLite database.

**Helper contract:**

- Ownership: the fixture owns workbook input handles and temporary SQLite state; it does not own the checked-in source snapshot.
- Preconditions: fixture assets have been copied to `AppContext.BaseDirectory/Fixtures`.
- Postconditions: `GenerateSnapshot` returns the same deterministic text as the current test and leaves no temporary database.
- Mutation: only `RegenerateSnapshot` writes the checked-in snapshot, and only the test calls it after checking `GOOSE_UPDATE_SNAPSHOT`.
- Failure behavior: workbook, SQL, or SQLite failures propagate after cleanup; regeneration remains an explicit failing test path.

**Mutation impact:**

- Source of truth changed: fixture paths move from `Goose.Tests/Fixtures` to `Goose.IntegrationTests/Fixtures`.
- Important readers: `FixtureDir`, `SourceFixtureDir`, and snapshot regeneration in `Goose.Tests/CsvToSqlSnapshotTests.cs:38-76` before the move.
- Derived/cached state affected: copied output fixtures; no runtime cache or persisted production data exists.
- Required propagation sequence:
  1. Move source fixtures.
  2. Make the integration project copy them.
  3. Compile the moved source so `CallerFilePath` resolves the new source directory.
  4. Verify regeneration writes the new source snapshot and deliberately fails.
- Invariants to preserve:
  - Normal execution never mutates the source snapshot.
  - Opt-in regeneration writes the source fixture, not the output copy.
  - Temporary SQLite state is removed on success and failure.
- Observable proof required: run both normal and regeneration modes and inspect the exact changed path.

**Step 4: Verify normal snapshot execution**

Run:

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter FullyQualifiedName~CsvToSqlSnapshotTests
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~CsvToSqlSnapshotTests
dotnet sln Goose.sln list
```

Expected: the integration project passes one snapshot test; the unit filter finds none; the solution list omits `Goose.IntegrationTests`.

**Step 5: Verify adversarial regeneration behavior**

Save the current snapshot hash, run regeneration, then restore the intentional generated change only after verifying the test failed and the source integration fixture changed:

```bash
sha256sum Goose.IntegrationTests/Fixtures/generated.snapshot
GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter FullyQualifiedName~CsvToSqlSnapshotTests
git diff -- Goose.IntegrationTests/Fixtures/generated.snapshot
```

Expected: the test fails with the existing “Rewrote … Review the diff” message; any diff is at `Goose.IntegrationTests/Fixtures/generated.snapshot`, never under `bin/`. If content is unchanged, the path in the failure still proves source targeting.

Re-run normally and expect PASS.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Snapshot executes only in integration project | paired filtered commands |
| Regeneration targets source fixture and never silently passes | opt-in adversarial run |
| Solution remains unit-only | `dotnet sln Goose.sln list` |

**Step 6: Commit**

```bash
git add Goose.IntegrationTests Goose.Tests/Goose.Tests.csproj Goose/Goose.csproj Goose.Tests/CsvToSqlSnapshotTests.cs Goose.Tests/Fixtures/aspereta-data.xlsx Goose.Tests/Fixtures/generated.snapshot
git commit -m "test: move CSV snapshot to integration tests"
```

### Task 4: Extract lightweight world test support

**Files:**

- Create: `TestSupport/TestWorldFixture.cs`
- Modify: `Goose.Tests/Goose.Tests.csproj`
- Modify: `Goose.IntegrationTests/Goose.IntegrationTests.csproj`
- Modify: `Goose.Tests/Fixtures/VendorFixture.cs`
- Modify: `Goose.Tests/ChatMessageLengthTests.cs`
- Modify: `Goose.Tests/PlayerEconomyOverloadTests.cs`
- Modify: `Goose.Tests/ResetModifiersTests.cs`
- Modify: `Goose.Tests/SpellEffectScriptDescriptionTests.cs`
- Modify: `Goose.Tests/VendorPurchaseCurrencyTests.cs`
- Later modify/move: `Goose.Tests/Fixtures/GlobalScriptFixture.cs`

**Step 1: Add fixture contract tests through real consumers (red)**

Update one read-only unit test from each required helper surface to construct `TestWorldFixture` instead of `GlobalScriptFixture`:

- map/player/command: one `ChatMessageLengthTests` case;
- item/spell/class helpers: one `ResetModifiersTests` or `PlayerEconomyOverloadTests` case;
- tiny arbitrary spell script: one `SpellEffectScriptDescriptionTests` case;
- vendor composition: one `VendorPurchaseCurrencyTests` case.

Run the four focused tests. Expected: compile failure because `Goose.Testing.TestWorldFixture` does not exist.

**Step 2: Create the linked lightweight fixture**

Create `TestSupport/TestWorldFixture.cs` in namespace `Goose.Testing` with this contract:

```csharp
public sealed class TestWorldFixture : IDisposable
{
    public string DataDirectory { get; }
    public GooseSettings Settings { get; }
    public GameWorld World { get; }

    public TestWorldFixture(Action<GooseSettings> configure = null);
    public Map AddBaseMap(int id, string name, int width = 10, int height = 10);
    public SpellEffect AddBaseSpellEffect(int id, string name, Action<SpellEffect> configure = null);
    public Player PlayerOn(Map map, int x, int y);
    public CapturingPlayer CommandPlayerOn(Map map, int x, int y, string name = "Tester");
    public void RegisterOnlinePlayer(Player player);
    public bool RunCommand(Player player, string packet);
    public Spell AddBaseSpell(int id, string name, int effectId, Action<Spell> configure = null);
    public ItemTemplate AddBaseItemTemplate(int id, string name, ItemTemplate.UseTypes useType, Action<ItemTemplate> configure = null);
    public void SeedClass(int classId, string name, int maxLevel);
    public Script<ISpellEffectScript> CompileSpellEffectScript(string body, string fileName);
    public void Dispose();
}
```

For Part 1 only, preserve the existing static-settings save/install/restore sequence because Part 2 removes it. The constructor must:

1. create a unique temporary data directory;
2. build the same minimum settings currently used by `GlobalScriptFixture`;
3. invoke `configure` before constructing the world;
4. assign the temporary settings to `GameWorld.Settings`;
5. construct the world and seed classes 0, 1, and 3.

The fixture intentionally does not install shipped dimensions scripts. `CompileSpellEffectScript` may create and compile one tiny synthetic spell script because generic script behavior remains unit coverage.

Link the same source file into both test projects:

```xml
<Compile Include="..\TestSupport\TestWorldFixture.cs" Link="Fixtures\TestWorldFixture.cs" />
```

**Helper contract:**

- Ownership: the fixture owns its temp directory and test settings; it does not own canonical script or workbook assets.
- Preconditions: tests must dispose the fixture; configuration is applied before `GameWorld` construction.
- Postconditions: the world contains the three seeded classes and built-in currencies; no shipped dimensions scripts are installed.
- Publication: no shared registry is published. Reflection helpers mutate only dictionaries owned by the fixture’s world.
- Failure behavior: disposal restores previous static settings and removes the owned directory if it exists.

**Step 3: Migrate all remaining non-dimensions consumers**

Replace `GlobalScriptFixture` with `TestWorldFixture` in the listed unit tests and make `VendorFixture` compose it. Preserve assertions. Use `fixture.Settings`, not new static reads, when the helper already exposes the value; Part 2 removes remaining static reads comprehensively.

Do not migrate `Dimension*Tests`, `DimensionsScriptTests`, or `SpiritCurrencyTests` in this task.

**Step 4: Run focused and full unit tests (green)**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~ChatMessageLengthTests|FullyQualifiedName~PlayerEconomyOverloadTests|FullyQualifiedName~ResetModifiersTests|FullyQualifiedName~SpellEffectScriptDescriptionTests|FullyQualifiedName~VendorPurchaseCurrencyTests"
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Expected: all focused tests and the full unit project pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Lightweight tests do not install shipped dimensions | zero-result `rg "CompileShipped|InstallShippedScripts" TestSupport` |
| Helper extraction preserves observable gameplay behavior | existing focused consumer tests |
| Vendor fixture still uses a fully initialized synthetic player/world | existing purchase and sell tests |
| Fixture cleanup restores Part 1 global state | full suite remains order-independent under existing collection |

**Step 5: Commit**

```bash
git add TestSupport Goose.Tests Goose.IntegrationTests/Goose.IntegrationTests.csproj
git commit -m "test: extract lightweight world fixture"
```

### Task 5: Move all dimensions coverage and canonical scripts

**Files:**

- Move: `Goose.Tests/DimensionCommandGateTests.cs`
- Move: `Goose.Tests/DimensionCurrencyCommandTests.cs`
- Move: `Goose.Tests/DimensionDropTests.cs`
- Move: `Goose.Tests/DimensionItemScriptTests.cs`
- Move: `Goose.Tests/DimensionItemTemplateTests.cs`
- Move: `Goose.Tests/DimensionMapScriptTests.cs`
- Move: `Goose.Tests/DimensionModifierTests.cs`
- Move: `Goose.Tests/DimensionRebirthTests.cs`
- Move: `Goose.Tests/DimensionResetItemTests.cs`
- Move: `Goose.Tests/DimensionScalingOverflowTests.cs`
- Move: `Goose.Tests/DimensionSpellScriptTests.cs`
- Move: `Goose.Tests/DimensionTeleportScriptTests.cs`
- Move: `Goose.Tests/DimensionVendorStockTests.cs`
- Move: `Goose.Tests/DimensionsScriptTests.cs`
- Move: `Goose.Tests/SpiritCurrencyTests.cs`
- Move: `Goose.Tests/Fixtures/GlobalScriptFixture.cs`
- Create: `Goose.IntegrationTests/Collections/GameWorldSettingsCollection.cs`
- Modify: `Goose.IntegrationTests/Goose.IntegrationTests.csproj`
- Modify: `Goose.Tests/Goose.Tests.csproj:21-56`

**Step 1: Add integration collection and script links**

Create an integration-local `GameWorldSettingsCollection` with `DisableParallelization = true`. It is temporary until Part 2 removes mutable static settings.

Move the 17 existing canonical script links from the unit project to the integration project without changing their `DimensionScripts/...` output paths. `GlobalScriptFixture.InstallShippedScripts` depends on those exact paths at `Goose.Tests/Fixtures/GlobalScriptFixture.cs:74-84` before the move.

**Step 2: Move the fixture and all approved tests**

- Move the files with history.
- Change root namespaces from `Goose.Tests` to `Goose.IntegrationTests`.
- Change fixture/collection imports to integration namespaces and `Goose.Testing` where appropriate.
- Make `GlobalScriptFixture` compose `TestWorldFixture`, expose its `Settings`, `World`, and `DataDirectory`, and retain only shipped-script installation, dimensions compilation, and dimensions-only helpers such as `RemoveClassLevel`.
- Update the missing-script error to name `Goose.IntegrationTests.csproj`.
- Preserve every test method name and assertion.

`DimensionScalingOverflowTests` moves even if it does not compile scripts because the approved boundary includes every `Dimension*Tests` class.

**Step 3: Verify destination discovery and source absence**

Run:

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore --list-tests
rg --files Goose.Tests | rg '/Dimensions?[^/]*Tests\.cs$|/SpiritCurrencyTests\.cs$|/CsvToSqlSnapshotTests\.cs$'
rg -n "CompileShipped\(|CompileShippedMapScript\(" Goose.Tests --glob '*.cs'
rg -n "DimensionScripts" Goose.Tests/Goose.Tests.csproj
```

Expected: the integration list contains every moved test; all three `rg` checks return no unit-project matches.

**Step 4: Run integration and unit projects**

Run:

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Expected: both pass independently. Integration execution may retain the existing long duration in this plan.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Every approved dimensions test moved intact | integration `--list-tests` compared with baseline |
| Unit project cannot accidentally compile shipped dimensions | zero-result source and project `rg` checks |
| Integration tests execute canonical scripts | existing script assertions plus linked canonical paths |
| Static settings remain serialized until Part 2 | integration collection definition and passing suite |

**Step 5: Commit**

```bash
git add Goose.Tests Goose.IntegrationTests TestSupport
git commit -m "test: move dimensions coverage to integration tests"
```

### Task 6: Verify counts, performance, and documentation

**Files:**

- Create: `docs/testing.md`
- Modify only if stale after moves: `Goose.Tests/Goose.Tests.csproj`
- Modify only if stale after moves: `Goose.IntegrationTests/Goose.IntegrationTests.csproj`

**Step 1: Verify combined discovery count**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --list-tests > /tmp/goose-unit-after.txt
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore --list-tests > /tmp/goose-integration-after.txt
```

Count fully qualified test entries using the same parsing method used for `/tmp/goose-tests-before.txt`. Expected: unit plus integration equals or exceeds the baseline. Any increase from new fixture/ownership tests must be recorded; any decrease blocks completion unless the exact intentionally consolidated tests are listed.

**Step 2: Verify the solution boundary**

Run:

```bash
dotnet sln Goose.sln list
dotnet test Goose.sln --no-restore
```

Expected: `Goose.IntegrationTests` is absent; all solution unit-test projects pass.

**Step 3: Measure the unit project**

Run one unmeasured warm-up:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Then run the same command three times under `/usr/bin/time -f '%e'`, recording wall-clock seconds. Expected: the median is below 5.0 seconds. Do not substitute test-framework “Duration” for process wall time.

If the median is not below five seconds, stop and profile the remaining unit tests before changing scope. Do not move additional tests solely by filename; use measured integration behavior as the boundary.

**Step 4: Document commands and boundaries**

Create `docs/testing.md` containing:

- `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore` for the fast project;
- `dotnet test Goose.sln --no-restore` for all solution unit projects;
- `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore` for integration coverage;
- the existing `GOOSE_UPDATE_SNAPSHOT=1` regeneration command and its deliberate failure behavior;
- test placement rules: tiny synthetic scripts stay unit; shipped dimensions and workbook/SQLite snapshots are integration;
- the baseline and final discovered counts and measured median.

**Step 5: Final verification**

Run:

```bash
dotnet build Goose.sln --no-restore
dotnet test Goose.sln --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
git diff --check
```

Expected: all commands pass, `Goose.Tests` has a measured median below five seconds, and the integration project remains outside the solution.

**Step 6: Commit**

```bash
git add docs/testing.md Goose.Tests/Goose.Tests.csproj Goose.IntegrationTests/Goose.IntegrationTests.csproj
git commit -m "docs: document unit and integration test boundaries"
```

## Part 1 completion checklist

- [ ] Normal project-based test discovery works under .NET 10.
- [ ] Windows service code and dependency are removed.
- [ ] `Goose.sln` contains unit-test projects only.
- [ ] CSV snapshot and all approved dimensions tests pass in `Goose.IntegrationTests`.
- [ ] `Goose.Tests` copies no integration assets and calls no shipped-dimensions helpers.
- [ ] Combined discovery count has not decreased.
- [ ] Warm median `Goose.Tests` wall time is below five seconds.
- [ ] `docs/testing.md` records commands, boundaries, counts, and timing.
