# Game Data Editor — Part 1: Column Descriptors and SQL Generation

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make column descriptors in `CsvToSql.Core` the single source of truth for game data schema, generating all DDL in memory and deleting `sqlTemplate.sql`.

**Architecture:** Each `*CsvToSql` converter returns an ordered `Column[]` instead of `string[]`. `CsvToSqlBase` derives value escaping from the descriptor kind, so the column list and transform logic can no longer drift. A DDL emitter turns descriptors into `CREATE TABLE` text, and `CsvToSqlConverter` assembles the whole script in memory. Correctness is proven by executing old and new scripts into SQLite and comparing schema plus every row.

**Tech Stack:** C# / .NET 10, ClosedXML 0.105.0, xunit 2.9.3, System.Data.SQLite 1.0.119.

**Design doc:** `docs/plans/2026-07-27-game-data-editor-design.md`

**Part 1 of 3.** Out of scope: `tools/SchemaGen`, `tools/SpriteBundle`, the Apps Script editor.

---

## APIs verified

Every citation below was read in this worktree.

| Fact | Location |
|---|---|
| `Convert(IXLWorksheet worksheet, string template, string tableName)` | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:12` |
| Cells read **positionally**: `row.Cell(i + 1).GetValue<string>()` | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:26` |
| Empty cell → column omitted from INSERT | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:27` |
| INSERT block uses `\n`, not `\r\n` | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:31-36` |
| `EscapeString` doubles single quotes | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:41-44` |
| `ConvertEnum(value, Type)` → `((int)Enum.Parse(...)).ToString()` | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:46-49` |
| `abstract class CsvToSqlBase` is **internal**, namespace `CsvToSql` | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:9-10` |
| `public static string Convert(string dataLinkId)` builds URL + downloads | `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs:16,44-46` |
| `converterMapping` is `Dictionary<string, dynamic>`, 21 entries | `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs:18-40` |
| Template loaded as embedded resource `CsvToSql.Core.sqlTemplate.sql` | `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs:48-52` |
| `<EmbeddedResource Include="sqlTemplate.sql" />` | `CsvToSql/CsvToSql.Core/CsvToSql.Core.csproj:8` |
| Missing worksheet leaves literal `{{table}}` in output | `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:39` |
| Call sites (signature must be preserved) | `Goose/GameWorld.cs:150`, `Goose/GameWorld.cs:201`, `Goose/Events/UpdateSqlCommandEvent.cs:32` |
| `Goose.Tests` references only `Goose.csproj` | `Goose.Tests/Goose.Tests.csproj:20` |
| xunit with global `using Xunit` | `Goose.Tests/Goose.Tests.csproj:16` |
| `System.Data.SQLite.Core` 1.0.119 available via Goose | `Goose/Goose.csproj:27` |

### Enum types (13, all nested in converters, namespace `CsvToSql`)

`ItemsCsvToSql.UseTypes:48`, `ItemsCsvToSql.ItemSlots:60`, `ItemsCsvToSql.ItemTypes:78`,
`SpellsCsvToSql.SpellTargets:33`, `NpcCsvToSql.Types:52`, `NpcCsvToSql.BehaviourTypes:60`,
`SpellEffectsCsvToSql.SpellDisplays:63`, `TargetTypes:72`, `SpellEffected:86`, `EffectTypes:112`,
`EnergyTypes:143`, `QuestRewardsCsvToSql.RewardType:31`, `QuestRequirementsCsvToSql.RequirementType:32`.

`RewardType` and `RequirementType` are declared **without** `public` — Task 2 makes them public.

### SQL types present in `sqlTemplate.sql`

`SMALLINT` ×101, `INT` ×82, `TEXT` ×41, `CHAR(1)` ×30, `DECIMAL(9,4)` ×25, `BIGINT` ×25,
`INTEGER` ×12 (always `INTEGER PRIMARY KEY`), `DECIMAL(5,2)` ×4, `DECIMAL(5,4)` ×2,
`VARCHAR(64)` ×1 (`combinations.combination_name`), `DECIMAL(9,2)` ×1.

### Indexes (only two)

```
CREATE INDEX npc_vendor_items_npc_template_id_idx ON npc_vendor_items(npc_template_id);   -- line 148
CREATE INDEX map_required_items_map_id_idx ON map_required_items(map_id);                 -- line 366
```

---

## Two corrections to the design, applied throughout this plan

### 1. The golden gate is semantic, not byte-identical

`sqlTemplate.sql` is CRLF throughout (verified with `cat -A`), but `CsvToSqlBase.cs:31-36`
appends `\n`. Current output is therefore a CRLF/LF mix. `combinations` (line 371) is also
tab-indented where every other table uses two spaces. Reproducing those quirks byte-for-byte
would freeze cosmetic accidents into the generator forever.

**Primary gate:** execute the old and new scripts into two SQLite databases and compare
`sqlite_master` plus every row of every table. That is what actually protects the data.

**Secondary gate:** compare generated text with line endings normalised to `\n` — catches
unintended semantic edits while permitting the deliberate whitespace normalisation.

### 2. Descriptors are a flat, ordered, 1:1 list

`CsvToSqlBase.cs:26` reads worksheet cells **positionally** — descriptor index `i` maps to
spreadsheet column `i + 1`. Composite kinds (`Graphic`, `Rgba`, `EquipSlots`) therefore must
**not** collapse multiple columns into one descriptor. They are a separate annotation layer
that references column names. Collapsing them would silently shift every subsequent column
by one and corrupt all 21 sheets.

---

## Task 0: Wire the test project to `CsvToSql.Core`

`Goose.Tests` cannot currently see the converters — it references only `Goose.csproj`, and
`CsvToSqlBase` plus most converters are internal.

**Files:**
- Modify: `Goose.Tests/Goose.Tests.csproj:19-21`
- Modify: `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:10`

**Step 1: Add the project reference**

In `Goose.Tests/Goose.Tests.csproj`, inside the existing `ItemGroup` that has the
`ProjectReference`:

```xml
  <ItemGroup>
    <ProjectReference Include="..\Goose\Goose.csproj" />
    <ProjectReference Include="..\CsvToSql\CsvToSql.Core\CsvToSql.Core.csproj" />
  </ItemGroup>
```

**Step 2: Make the base class public**

`CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:10` — change `abstract class CsvToSqlBase` to:

```csharp
    public abstract class CsvToSqlBase
```

**Step 3: Build**

Run: `dotnet build Goose.sln`
Expected: `0 Error(s)`. Warnings about inconsistent accessibility are expected here and
resolved in Task 2 when the converters become public.

If the build reports CS0060 or CS0050 (inconsistent accessibility) on converter classes,
make those classes `public` now — they all need it for the registry in Task 8 regardless.

**Step 4: Commit**

```bash
git add Goose.Tests/Goose.Tests.csproj CsvToSql/CsvToSql.Core/
git commit -m "test: make CsvToSql.Core visible to Goose.Tests"
```

---

## Task 1: Baseline fixture and a testable seam

The converter currently only accepts a Google Sheets id and downloads over the network. Tests
need a local workbook, and the refactor needs a recorded baseline.

**Files:**
- Create: `Goose.Tests/Fixtures/aspereta-data.xlsx`
- Create: `Goose.Tests/Fixtures/baseline.sql`
- Modify: `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs`
- Modify: `Goose.Tests/Goose.Tests.csproj`

**Step 1: Download the fixture workbook**

This is the sheet `GooseSettings.json` currently points at (`DataLinkId`
`1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU`). It has exactly the 21 worksheets the
converter maps — verified, no missing and no extra sheets.

```bash
mkdir -p Goose.Tests/Fixtures
ID=1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU
curl -sL -o Goose.Tests/Fixtures/aspereta-data.xlsx \
  "https://docs.google.com/spreadsheets/u/0/d/$ID/export?format=xlsx&id=$ID"
file Goose.Tests/Fixtures/aspereta-data.xlsx
```

Expected: `Microsoft Excel 2007+`, roughly 360 KB.

**Step 2: Add a stream overload**

In `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs`, extract the body of `Convert` so the
download is separated from the conversion. Keep the existing public signature — it is called
from `Goose/GameWorld.cs:150`, `Goose/GameWorld.cs:201` and
`Goose/Events/UpdateSqlCommandEvent.cs:32`.

Replace lines 42-47 (the URL build, download and resource load) with:

```csharp
            var url = $"https://docs.google.com/spreadsheets/u/0/d/{dataLinkId}/export?format=xlsx&id={dataLinkId}";
            var spreadsheet = new MemoryStream(new HttpClient().GetByteArrayAsync(url).Result);

            return ConvertWorkbook(spreadsheet);
        }

        /// <summary>Converts an already-loaded .xlsx stream. Exists so tests can run against a
        /// committed fixture instead of the network.</summary>
        public static string ConvertWorkbook(Stream spreadsheet)
        {
            var converterMapping = BuildConverterMapping();
```

Move the `converterMapping` dictionary (currently lines 18-40) into a private
`BuildConverterMapping()` method returning the same `Dictionary<string, dynamic>` — Task 8
replaces the `dynamic` with the registry, so do not redesign it yet.

The remainder of the method (resource load through `return sqlTemplate;`) stays in
`ConvertWorkbook`.

**Step 3: Record the baseline**

Add a temporary test that writes the current output to the fixture path.

Create `Goose.Tests/CsvToSqlBaselineTests.cs`:

```csharp
using CsvToSql.Core;

namespace Goose.Tests;

public class CsvToSqlBaselineTests
{
    private static string FixtureDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact(Skip = "Run manually to re-record the baseline")]
    public void RecordBaseline()
    {
        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        var sql = CsvToSqlConverter.ConvertWorkbook(fs);

        // Written to the source tree, not the build output, so it can be committed.
        var target = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "baseline.sql");
        File.WriteAllText(Path.GetFullPath(target), sql);
    }
}
```

Add the fixture copy rule to `Goose.Tests/Goose.Tests.csproj`:

```xml
  <ItemGroup>
    <None Include="Fixtures/**" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

**Step 4: Run the recorder**

```bash
dotnet test Goose.sln --filter "FullyQualifiedName~RecordBaseline" \
  -- xunit.methodDisplay=method
```

xunit will not run a skipped fact. Temporarily remove the `Skip` argument, run
`dotnet test Goose.sln --filter "FullyQualifiedName~RecordBaseline"`, confirm it passes, then
restore the `Skip`.

Verify the recording captured the working-tree defaults, not `HEAD`:

```bash
grep -c "INSERT INTO" Goose.Tests/Fixtures/baseline.sql
grep "face_id SMALLINT" Goose.Tests/Fixtures/baseline.sql
```

Expected: a non-zero INSERT count, and `face_id SMALLINT DEFAULT 0 NOT NULL` — the uncommitted
change from 70 to 0. If it shows `DEFAULT 70`, the working-tree edit to
`CsvToSql/CsvToSql.Core/sqlTemplate.sql` was lost; restore it before continuing.

**Step 5: Commit**

```bash
git add Goose.Tests/Fixtures Goose.Tests/CsvToSqlBaselineTests.cs \
        Goose.Tests/Goose.Tests.csproj CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs
git commit -m "test: record baseline SQL output from committed xlsx fixture"
```

---

## Task 2: `Column`, `SqlType` and the `Col` builder

**Files:**
- Create: `CsvToSql/CsvToSql.Core/Schema/SqlType.cs`
- Create: `CsvToSql/CsvToSql.Core/Schema/Column.cs`
- Create: `CsvToSql/CsvToSql.Core/Schema/Col.cs`
- Test: `Goose.Tests/Schema/ColumnTests.cs`

**Step 1: Write the failing test**

```csharp
using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class ColumnTests
{
    [Fact]
    public void Int_carries_type_and_default()
    {
        var c = Col.Int("stack", SqlType.Int, def: 1);

        Assert.Equal("stack", c.Name);
        Assert.Equal(ColumnKind.Int, c.Kind);
        Assert.Equal("1", c.Default);
        Assert.False(c.IsRequired);
    }

    [Fact]
    public void Required_column_has_no_default()
    {
        var c = Col.Text("item_name", SqlType.Text).Required();

        Assert.True(c.IsRequired);
        Assert.Null(c.Default);
    }

    [Fact]
    public void PrimaryKey_implies_required_and_integer()
    {
        var c = Col.Id("item_template_id").PrimaryKey();

        Assert.True(c.IsPrimaryKey);
        Assert.True(c.IsRequired);
        Assert.Equal(SqlType.Integer, c.Type);
    }

    [Fact]
    public void Ref_records_target_sheet()
    {
        var c = Col.Id("npc_template_id").Ref("NPCs");

        Assert.Equal("NPCs", c.RefSheet);
    }

    [Fact]
    public void Enum_exposes_member_names_in_declaration_order()
    {
        var c = Col.Enum<SampleKind>("kind");

        Assert.Equal(ColumnKind.Enum, c.Kind);
        Assert.Equal(new[] { "First", "Second" }, c.EnumNames);
    }

    private enum SampleKind { First = 0, Second = 1 }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~ColumnTests"`
Expected: FAIL — `CsvToSql.Core.Schema` does not exist.

**Step 3: Write the implementation**

`CsvToSql/CsvToSql.Core/Schema/SqlType.cs`:

```csharp
namespace CsvToSql.Core.Schema
{
    /// <summary>SQL column types used by the game data schema. The string value is emitted
    /// verbatim into CREATE TABLE.</summary>
    public sealed class SqlType
    {
        public string Sql { get; }
        private SqlType(string sql) => Sql = sql;

        public static readonly SqlType Integer = new("INTEGER");
        public static readonly SqlType SmallInt = new("SMALLINT");
        public static readonly SqlType Int = new("INT");
        public static readonly SqlType BigInt = new("BIGINT");
        public static readonly SqlType Text = new("TEXT");
        public static readonly SqlType Char1 = new("CHAR(1)");
        public static readonly SqlType Varchar64 = new("VARCHAR(64)");
        public static readonly SqlType Decimal94 = new("DECIMAL(9,4)");
        public static readonly SqlType Decimal92 = new("DECIMAL(9,2)");
        public static readonly SqlType Decimal52 = new("DECIMAL(5,2)");
        public static readonly SqlType Decimal54 = new("DECIMAL(5,4)");

        public override string ToString() => Sql;
    }
}
```

`CsvToSql/CsvToSql.Core/Schema/Column.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace CsvToSql.Core.Schema
{
    /// <summary>How a cell value is escaped on the way into SQL, and how the editor renders it.</summary>
    public enum ColumnKind { Id, Int, Decimal, Text, Bool, Enum }

    /// <summary>One spreadsheet column. Descriptors are a flat ordered list: index i maps to
    /// worksheet cell i + 1 (see CsvToSqlBase.Convert), so never merge or reorder them.</summary>
    public sealed class Column
    {
        public string Name { get; }
        public ColumnKind Kind { get; }
        public SqlType Type { get; }
        public string Default { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsPrimaryKey { get; private set; }
        public string RefSheet { get; private set; }
        public Type EnumType { get; private set; }
        public IReadOnlyList<string> EnumNames =>
            EnumType == null ? Array.Empty<string>() : Enum.GetNames(EnumType);

        internal Column(string name, ColumnKind kind, SqlType type, string def = null)
        {
            Name = name;
            Kind = kind;
            Type = type;
            Default = def;
        }

        public Column Required() { IsRequired = true; Default = null; return this; }
        public Column Ref(string sheet) { RefSheet = sheet; return this; }

        public Column PrimaryKey()
        {
            IsPrimaryKey = true;
            IsRequired = true;
            Default = null;
            return this;
        }

        internal Column WithEnum(Type enumType) { EnumType = enumType; return this; }
    }
}
```

`CsvToSql/CsvToSql.Core/Schema/Col.cs`:

```csharp
using System;

namespace CsvToSql.Core.Schema
{
    /// <summary>Factory methods for column descriptors. `def` is the SQL DEFAULT rendered
    /// verbatim; pass null for a column with no default.</summary>
    public static class Col
    {
        public static Column Id(string name, SqlType type = null, int? def = null) =>
            Make(name, ColumnKind.Id, type ?? SqlType.Integer, def?.ToString());

        public static Column Int(string name, SqlType type = null, int? def = null) =>
            Make(name, ColumnKind.Int, type ?? SqlType.Int, def?.ToString());

        public static Column Decimal(string name, SqlType type = null, string def = null) =>
            Make(name, ColumnKind.Decimal, type ?? SqlType.Decimal94, def);

        public static Column Text(string name, SqlType type = null, string def = null) =>
            Make(name, ColumnKind.Text, type ?? SqlType.Text, def);

        /// <summary>Stored as CHAR(1) '0'/'1'. Previously indistinguishable from Text —
        /// the converters marked these only with a `// booleans` comment.</summary>
        public static Column Bool(string name, bool? def = null) =>
            Make(name, ColumnKind.Bool, SqlType.Char1, def is null ? null : (def.Value ? "'1'" : "'0'"));

        public static Column Enum<T>(string name, SqlType type = null, int? def = null)
            where T : struct, System.Enum =>
            Make(name, ColumnKind.Enum, type ?? SqlType.SmallInt, def?.ToString())
                .WithEnum(typeof(T));

        private static Column Make(string name, ColumnKind kind, SqlType type, string def)
        {
            var c = new Column(name, kind, type, def);
            if (def == null) c.Required();
            return c;
        }
    }
}
```

Note `Make` marks a column required when it has no default — that mirrors the existing
semantics exactly: `CsvToSqlBase.cs:27` omits empty cells from the INSERT, so a `NOT NULL`
column without a `DEFAULT` must always have a value or SQLite rejects the row.

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~ColumnTests"`
Expected: PASS, 5 tests.

**Step 5: Commit**

```bash
git add CsvToSql/CsvToSql.Core/Schema Goose.Tests/Schema
git commit -m "feat: add column descriptor types for game data schema"
```

---

## Task 3: Composite annotations

Editor-facing groupings that must **not** alter the flat column list.

**Files:**
- Create: `CsvToSql/CsvToSql.Core/Schema/Composite.cs`
- Test: `Goose.Tests/Schema/CompositeTests.cs`

**Step 1: Write the failing test**

```csharp
using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class CompositeTests
{
    [Fact]
    public void Graphic_names_its_two_columns()
    {
        var g = Composite.Graphic("graphic_tile", file: "graphic_file");

        Assert.Equal(CompositeKind.Graphic, g.Kind);
        Assert.Equal(new[] { "graphic_tile", "graphic_file" }, g.Columns);
    }

    [Fact]
    public void Rgba_names_four_columns_in_order()
    {
        var c = Composite.Rgba("body_r", "body_g", "body_b", "body_a");

        Assert.Equal(new[] { "body_r", "body_g", "body_b", "body_a" }, c.Columns);
    }

    [Fact]
    public void Bitmask_records_source_sheet()
    {
        var b = Composite.Bitmask("class_restrictions", from: "Classes");

        Assert.Equal("Classes", b.SourceSheet);
        Assert.Equal(new[] { "class_restrictions" }, b.Columns);
    }

    [Fact]
    public void IdList_records_target_sheet()
    {
        var l = Composite.IdList("quest_ids", refSheet: "Quests");

        Assert.Equal("Quests", l.SourceSheet);
    }

    [Fact]
    public void EquipSlots_covers_one_column()
    {
        var e = Composite.EquipSlots("equipped_items");

        Assert.Equal(CompositeKind.EquipSlots, e.Kind);
        Assert.Equal(new[] { "equipped_items" }, e.Columns);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~CompositeTests"`
Expected: FAIL — `Composite` does not exist.

**Step 3: Write the implementation**

```csharp
using System.Collections.Generic;

namespace CsvToSql.Core.Schema
{
    public enum CompositeKind { Graphic, Rgba, Bitmask, IdList, EquipSlots }

    /// <summary>An editor-facing control spanning one or more columns. Purely an annotation —
    /// the flat Column[] list is unchanged, because worksheet cells are read positionally.</summary>
    public sealed class Composite
    {
        public CompositeKind Kind { get; }
        public IReadOnlyList<string> Columns { get; }
        public string SourceSheet { get; }

        private Composite(CompositeKind kind, string sourceSheet, params string[] columns)
        {
            Kind = kind;
            SourceSheet = sourceSheet;
            Columns = columns;
        }

        public static Composite Graphic(string tile, string file) =>
            new(CompositeKind.Graphic, null, tile, file);

        public static Composite Rgba(string r, string g, string b, string a) =>
            new(CompositeKind.Rgba, null, r, g, b, a);

        public static Composite Bitmask(string column, string from) =>
            new(CompositeKind.Bitmask, from, column);

        public static Composite IdList(string column, string refSheet) =>
            new(CompositeKind.IdList, refSheet, column);

        public static Composite EquipSlots(string column) =>
            new(CompositeKind.EquipSlots, null, column);
    }
}
```

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~CompositeTests"`
Expected: PASS, 5 tests.

**Step 5: Commit**

```bash
git add CsvToSql/CsvToSql.Core/Schema/Composite.cs Goose.Tests/Schema/CompositeTests.cs
git commit -m "feat: add composite column annotations for editor controls"
```

---

## Task 4: DDL emitter

**Files:**
- Create: `CsvToSql/CsvToSql.Core/Schema/TableDdl.cs`
- Test: `Goose.Tests/Schema/TableDdlTests.cs`

**Step 1: Write the failing test**

Expected output uses `\n` uniformly and two-space indentation — the deliberate normalisation
described above.

```csharp
using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class TableDdlTests
{
    [Fact]
    public void Emits_drop_create_and_columns()
    {
        var ddl = TableDdl.Emit("npc_drops", new[]
        {
            Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
            Col.Int("stack"),
            Col.Decimal("droprate"),
        }, indexes: null);

        Assert.Equal(
            "DROP TABLE IF EXISTS npc_drops;\n" +
            "CREATE TABLE npc_drops (\n" +
            "  npc_template_id INT NOT NULL,\n" +
            "  item_template_id INT NOT NULL,\n" +
            "  stack INT NOT NULL,\n" +
            "  droprate DECIMAL(9,4) NOT NULL\n" +
            ");\n", ddl);
    }

    [Fact]
    public void Primary_key_column_omits_not_null()
    {
        var ddl = TableDdl.Emit("classes", new[]
        {
            Col.Id("class_id").PrimaryKey(),
            Col.Text("class_name"),
        }, indexes: null);

        Assert.Contains("  class_id INTEGER PRIMARY KEY,\n", ddl);
    }

    [Fact]
    public void Default_precedes_not_null()
    {
        var ddl = TableDdl.Emit("t", new[] { Col.Int("stack", def: 1) }, indexes: null);

        Assert.Contains("  stack INT DEFAULT 1 NOT NULL\n", ddl);
    }

    [Fact]
    public void Text_default_is_quoted_by_caller()
    {
        var ddl = TableDdl.Emit("t", new[] { Col.Text("script_path", def: "''") }, indexes: null);

        Assert.Contains("  script_path TEXT DEFAULT '' NOT NULL\n", ddl);
    }

    [Fact]
    public void Bool_default_renders_as_quoted_char()
    {
        var ddl = TableDdl.Emit("t", new[] { Col.Bool("lore", def: false) }, indexes: null);

        Assert.Contains("  lore CHAR(1) DEFAULT '0' NOT NULL\n", ddl);
    }

    [Fact]
    public void Index_follows_create_table()
    {
        var ddl = TableDdl.Emit("npc_vendor_items", new[]
        {
            Col.Id("npc_template_id", SqlType.Int),
        }, indexes: new[] { "npc_template_id" });

        Assert.EndsWith(
            ");\n" +
            "CREATE INDEX npc_vendor_items_npc_template_id_idx " +
            "ON npc_vendor_items(npc_template_id);\n", ddl);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~TableDdlTests"`
Expected: FAIL — `TableDdl` does not exist.

**Step 3: Write the implementation**

```csharp
using System.Collections.Generic;
using System.Text;

namespace CsvToSql.Core.Schema
{
    /// <summary>Renders DROP/CREATE/CREATE INDEX for one table from its column descriptors.
    /// Replaces the hand-maintained sqlTemplate.sql.</summary>
    public static class TableDdl
    {
        public static string Emit(string table, IReadOnlyList<Column> columns,
                                  IReadOnlyList<string> indexes)
        {
            var sb = new StringBuilder();
            sb.Append($"DROP TABLE IF EXISTS {table};\n");
            sb.Append($"CREATE TABLE {table} (\n");

            for (int i = 0; i < columns.Count; i++)
            {
                var c = columns[i];
                sb.Append("  ").Append(c.Name).Append(' ').Append(c.Type.Sql);

                if (c.IsPrimaryKey)
                {
                    sb.Append(" PRIMARY KEY");
                }
                else
                {
                    if (c.Default != null) sb.Append(" DEFAULT ").Append(c.Default);
                    sb.Append(" NOT NULL");
                }

                if (i < columns.Count - 1) sb.Append(',');
                sb.Append('\n');
            }

            sb.Append(");\n");

            if (indexes != null)
                foreach (var col in indexes)
                    sb.Append($"CREATE INDEX {table}_{col}_idx ON {table}({col});\n");

            return sb.ToString();
        }
    }
}
```

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~TableDdlTests"`
Expected: PASS, 6 tests.

**Step 5: Commit**

```bash
git add CsvToSql/CsvToSql.Core/Schema/TableDdl.cs Goose.Tests/Schema/TableDdlTests.cs
git commit -m "feat: emit table DDL from column descriptors"
```

---

## Task 5: Descriptor path in `CsvToSqlBase`, piloted on `NpcDrops`

Add the descriptor path alongside the existing `string[]` path so converters can migrate one
at a time with the baseline green throughout.

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs`
- Modify: `CsvToSql/CsvToSql.Core/NpcDropsCsvToSql.cs`
- Test: `Goose.Tests/Schema/DescriptorTransformTests.cs`

**Step 1: Write the failing test**

```csharp
using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class DescriptorTransformTests
{
    [Fact]
    public void Text_is_escaped_and_quotes_doubled()
    {
        Assert.Equal("'Bob''s Hat'",
            DescriptorTransform.Apply(Col.Text("item_name").Required(), "Bob's Hat"));
    }

    [Fact]
    public void Numbers_pass_through_unquoted()
    {
        Assert.Equal("42", DescriptorTransform.Apply(Col.Int("stack", def: 1), "42"));
    }

    [Fact]
    public void Bool_is_quoted_like_text()
    {
        // Matches existing behaviour: booleans went through EscapeString.
        Assert.Equal("'1'", DescriptorTransform.Apply(Col.Bool("lore", def: false), "1"));
    }

    [Fact]
    public void Enum_name_becomes_its_integer_value()
    {
        Assert.Equal("1", DescriptorTransform.Apply(Col.Enum<Sample>("k"), "Second"));
    }

    private enum Sample { First = 0, Second = 1 }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~DescriptorTransformTests"`
Expected: FAIL — `DescriptorTransform` does not exist.

**Step 3: Write the implementation**

Create `CsvToSql/CsvToSql.Core/Schema/DescriptorTransform.cs`:

```csharp
using System;

namespace CsvToSql.Core.Schema
{
    /// <summary>Derives SQL literal escaping from a column's kind. Replaces each converter's
    /// hand-written TransformValue switch, which could drift from its column list.</summary>
    public static class DescriptorTransform
    {
        public static string Apply(Column column, string value)
        {
            switch (column.Kind)
            {
                case ColumnKind.Text:
                case ColumnKind.Bool:
                    return Escape(value);
                case ColumnKind.Enum:
                    return ((int)Enum.Parse(column.EnumType, value)).ToString();
                default:
                    return value;
            }
        }

        private static string Escape(string value) =>
            string.Format("'{0}'", value.Replace("'", "''"));
    }
}
```

In `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs`, add an overridable descriptor hook and use it
when present. Replace lines 12-40 with:

```csharp
        /// <summary>Descriptors for this sheet, or null while the converter still uses the
        /// legacy string[] path. Ordered 1:1 with worksheet columns.</summary>
        public virtual Column[] GetColumnDescriptors() => null;

        /// <summary>Editor-facing composite annotations. Does not affect column order.</summary>
        public virtual Composite[] GetComposites() => null;

        public string Convert(IXLWorksheet worksheet, string template, string tableName)
        {
            var descriptors = GetColumnDescriptors();
            string[] allColumns = descriptors != null
                ? descriptors.Select(d => d.Name).ToArray()
                : GetColumns();

            var sqlBuilder = new StringBuilder();

            foreach (var row in worksheet.Rows().Skip(1).Where(r => !r.IsEmpty()))
            {
                List<string> columns = new List<string>();
                List<string> values = new List<string>();

                for (int i = 0; i < allColumns.Length; i++)
                {
                    string value = row.Cell(i + 1).GetValue<string>();
                    if (value.Length == 0) continue;

                    columns.Add(allColumns[i]);
                    values.Add(descriptors != null
                        ? DescriptorTransform.Apply(descriptors[i], value)
                        : TransformValue(allColumns[i], value));
                }

                sqlBuilder.AppendFormat("INSERT INTO {0} (", tableName);
                sqlBuilder.Append(string.Join(", ", columns));
                sqlBuilder.Append(")\nVALUES (");
                sqlBuilder.Append(string.Join(", ", values));
                sqlBuilder.Append(");\n");
            }

            return template.Replace("{{" + tableName + "}}", sqlBuilder.ToString());
        }
```

Add `using CsvToSql.Core.Schema;` at the top, and relax the two abstract members to virtual so
migrated converters need not keep them:

```csharp
        protected virtual string TransformValue(string columnName, string value) => value;
        protected virtual string[] GetColumns() => null;
```

**Step 4: Migrate `NpcDropsCsvToSql`**

Types and defaults come from `sqlTemplate.sql:130-136` — all four columns are `NOT NULL` with
no default.

Replace the body of `CsvToSql/CsvToSql.Core/NpcDropsCsvToSql.cs`:

```csharp
using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class NpcDropsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
            Col.Int("stack"),
            Col.Decimal("droprate"),
        };
    }
}
```

**Step 5: Verify the baseline still matches**

Add the comparison test to `Goose.Tests/CsvToSqlBaselineTests.cs`:

```csharp
    [Fact]
    public void Output_matches_recorded_baseline()
    {
        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        var actual = CsvToSqlConverter.ConvertWorkbook(fs);
        var expected = File.ReadAllText(Path.Combine(FixtureDir, "baseline.sql"));

        Assert.Equal(Normalise(expected), Normalise(actual));
    }

    private static string Normalise(string sql) => sql.Replace("\r\n", "\n");
```

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~CsvToSqlBaselineTests"`
Expected: PASS. `npc_drops` INSERTs are byte-identical because all four columns are numeric
and were previously passed through unchanged by the `default:` case.

**Step 6: Commit**

```bash
git add CsvToSql/CsvToSql.Core Goose.Tests
git commit -m "feat: add descriptor path to CsvToSqlBase, migrate NpcDrops"
```

---

## Task 6: Migrate the remaining 20 converters

Do these in the four batches below, running the baseline test after **each batch**. Every
column's SQL type and default is read from `CsvToSql/CsvToSql.Core/sqlTemplate.sql` at the
cited lines — do not guess.

**Translation rules:**

| Template DDL | Descriptor |
|---|---|
| `x INTEGER PRIMARY KEY` | `Col.Id("x").PrimaryKey()` |
| `x INT NOT NULL` | `Col.Id("x", SqlType.Int)` or `Col.Int("x", SqlType.Int)` |
| `x SMALLINT DEFAULT 0 NOT NULL` | `Col.Int("x", SqlType.SmallInt, def: 0)` |
| `x BIGINT DEFAULT 0 NOT NULL` | `Col.Int("x", SqlType.BigInt, def: 0)` |
| `x TEXT DEFAULT '' NOT NULL` | `Col.Text("x", def: "''")` |
| `x TEXT NOT NULL` | `Col.Text("x")` |
| `x CHAR(1) DEFAULT '0' NOT NULL` | `Col.Bool("x", def: false)` |
| `x DECIMAL(9,4) DEFAULT 100 NOT NULL` | `Col.Decimal("x", def: "100")` |
| `x VARCHAR(64) NOT NULL` | `Col.Text("x", SqlType.Varchar64)` |

An `ConvertEnum(value, typeof(T))` case in the old `TransformValue` becomes
`Col.Enum<T>("x", SqlType.SmallInt, def: …)` — keep the *same* SQL type the template declares.
A column in the old `EscapeString` list that the template declares `CHAR(1)` is a `Bool`;
`TEXT`/`VARCHAR` stays `Text`.

**Batch A — join tables, no enums** (run baseline after)

| Converter | Template lines |
|---|---|
| `NpcSpawnsCsvToSql.cs` | 120-126 |
| `NpcVendorsCsvToSql.cs` | 140-148 (index on `npc_template_id`) |
| `WarpTilesCsvToSql.cs` | 323-331 |
| `MapRequiredItemsCsvToSql.cs` | 361-366 (index on `map_id`) |
| `CombinationItemRequiredCsvToSql.cs` | 384-388 |
| `CombinationItemResultsCsvToSql.cs` | 392-396 |
| `ClassLevelupSpellsCsvToSql.cs` | 477-482 |

Add `.Ref(...)` on every foreign key: `npc_template_id`→`NPCs`, `item_template_id`→`Items`,
`map_id`→`Maps`, `spell_id`→`Spells`, `class_id`→`Classes`, `combination_id`→`Combinations`.

**Batch B — tables with enums but no composites**

| Converter | Template lines | Enums |
|---|---|---|
| `QuestRequirementsCsvToSql.cs` | 173-181 | `RequirementType` |
| `QuestRewardsCsvToSql.cs` | 185-193 | `RewardType` |
| `QuestsCsvToSql.cs` | 153-169 | — |
| `TitleCsvToSql.cs` | 400-413 | `ItemsCsvToSql.UseTypes`, `ItemSlots` |
| `SurnameCsvToSql.cs` | 417-430 | same |

Make `RewardType` (`QuestRewardsCsvToSql.cs:31`) and `RequirementType`
(`QuestRequirementsCsvToSql.cs:32`) `public` — `Col.Enum<T>` requires it.

**Batch C — maps, classes, combinations**

| Converter | Template lines | Notes |
|---|---|---|
| `MapsCsvToSql.cs` | 335-357 | 8 `CHAR(1)` booleans → `Col.Bool` |
| `ClassesCsvToSql.cs` | 434-441 | |
| `ClassInfoCsvToSql.cs` | 445-473 | no primary key |
| `CombinationsCsvToSql.cs` | 371-380 | `VARCHAR(64)`; tab indentation is dropped deliberately |

Add to `CombinationsCsvToSql`: `Composite.Bitmask("class_restrictions", from: "Classes")`.

**Batch D — the four wide tables**

| Converter | Template lines | Composites |
|---|---|---|
| `ItemsCsvToSql.cs` | 4-51 | `Graphic("graphic_tile", file: "graphic_file")`, `Rgba("graphic_r","graphic_g","graphic_b","graphic_a")`, `Bitmask("class_restrictions", from: "Classes")` |
| `SpellsCsvToSql.cs` | 197-216 | `Graphic("spellbook_graphic", file: "spellbook_graphic_file")`, `Bitmask("class_restrictions", from: "Classes")` |
| `NpcCsvToSql.cs` | 56-116 | `Rgba("body_r",…)`, `Rgba("hair_r",…)`, `EquipSlots("equipped_items")`, `IdList("quest_ids", refSheet: "Quests")` |
| `SpellEffectsCsvToSql.cs` | 220-319 | `Graphic("spell_animation", file: "spell_animation_file")`, `Graphic("buff_graphic", file: "buff_graphic_file")`, `Rgba("body_r",…)`, `Rgba("hair_r",…)` |

`graphic_tile` (line 35) and `spellbook_graphic` (line 204) are `NOT NULL` with **no default** —
they must come out `Required()`. Every other graphic column defaults to 0.

Reference descriptor for `Items`, first ten columns, from `sqlTemplate.sql:5-14`:

```csharp
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("item_template_id").PrimaryKey(),
            Col.Enum<UseTypes>("item_usetype"),
            Col.Text("item_name"),
            Col.Text("item_description", def: "''"),
            Col.Int("player_hp", def: 0),
            Col.Int("player_mp", def: 0),
            Col.Int("player_sp", def: 0),
            Col.Int("stat_ac", SqlType.SmallInt, def: 0),
            Col.Int("stat_str", SqlType.SmallInt, def: 0),
            Col.Int("stat_sta", SqlType.SmallInt, def: 0),
            // … continue through sqlTemplate.sql:51
        };
```

**After each batch:**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~CsvToSqlBaselineTests"`
Expected: PASS.

If a batch fails, the diff is the signal — dump both strings to files and `diff` them:

```csharp
File.WriteAllText("/tmp/actual.sql", Normalise(actual));
File.WriteAllText("/tmp/expected.sql", Normalise(expected));
```

The usual causes are a wrong SQL type (changes nothing in INSERTs, only DDL), a `Text` column
typed as `Int` (loses quoting), or a column omitted, which shifts every later column.

**Commit after each batch:**

```bash
git add CsvToSql/CsvToSql.Core
git commit -m "refactor: migrate <batch> converters to column descriptors"
```

---

## Task 7: Registry, replacing the `dynamic` dictionary

**Files:**
- Create: `CsvToSql/CsvToSql.Core/Schema/SchemaRegistry.cs`
- Modify: `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs`
- Test: `Goose.Tests/Schema/SchemaRegistryTests.cs`

**Step 1: Write the failing test**

```csharp
using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class SchemaRegistryTests
{
    [Fact]
    public void Covers_all_twenty_one_sheets()
    {
        Assert.Equal(21, SchemaRegistry.Tables.Count);
    }

    [Fact]
    public void Every_table_has_descriptors()
    {
        foreach (var t in SchemaRegistry.Tables)
        {
            Assert.False(string.IsNullOrEmpty(t.Sheet));
            Assert.False(string.IsNullOrEmpty(t.Table));
            Assert.NotEmpty(t.Columns);
        }
    }

    [Fact]
    public void Maps_items_sheet_to_item_templates()
    {
        var items = SchemaRegistry.Tables.Single(t => t.Sheet == "Items");

        Assert.Equal("item_templates", items.Table);
        Assert.Equal("item_template_id", items.Columns[0].Name);
        Assert.True(items.Columns[0].IsPrimaryKey);
    }

    [Fact]
    public void Only_two_tables_declare_indexes()
    {
        var indexed = SchemaRegistry.Tables
            .Where(t => t.Indexes is { Count: > 0 })
            .Select(t => t.Table)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(new[] { "map_required_items", "npc_vendor_items" }, indexed);
    }

    [Fact]
    public void Every_foreign_key_targets_a_known_sheet()
    {
        var sheets = SchemaRegistry.Tables.Select(t => t.Sheet).ToHashSet();

        foreach (var t in SchemaRegistry.Tables)
            foreach (var c in t.Columns.Where(c => c.RefSheet != null))
                Assert.Contains(c.RefSheet, sheets);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~SchemaRegistryTests"`
Expected: FAIL — `SchemaRegistry` does not exist.

**Step 3: Write the implementation**

```csharp
using System.Collections.Generic;

namespace CsvToSql.Core.Schema
{
    /// <summary>One worksheet's schema: the sheet name the importer matches on, its target
    /// table, and its descriptors. Single source of truth for both SQL generation and the
    /// editor's generated schema.js.</summary>
    public sealed class TableSchema
    {
        public string Sheet { get; }
        public string Table { get; }
        public CsvToSqlBase Converter { get; }
        public IReadOnlyList<Column> Columns { get; }
        public IReadOnlyList<Composite> Composites { get; }
        public IReadOnlyList<string> Indexes { get; }

        public TableSchema(string sheet, string table, CsvToSqlBase converter,
                           IReadOnlyList<string> indexes = null)
        {
            Sheet = sheet;
            Table = table;
            Converter = converter;
            Columns = converter.GetColumnDescriptors();
            Composites = converter.GetComposites() ?? System.Array.Empty<Composite>();
            Indexes = indexes ?? System.Array.Empty<string>();
        }
    }

    public static class SchemaRegistry
    {
        /// <summary>Declaration order is emission order in the generated script.</summary>
        public static IReadOnlyList<TableSchema> Tables { get; } = new[]
        {
            new TableSchema("Items", "item_templates", new ItemsCsvToSql()),
            new TableSchema("NPCs", "npc_templates", new NpcCsvToSql()),
            new TableSchema("NPC Spawns", "npc_spawns", new NpcSpawnsCsvToSql()),
            new TableSchema("NPC Drops", "npc_drops", new NpcDropsCsvToSql()),
            new TableSchema("NPC Vendor Items", "npc_vendor_items", new NpcVendorsCsvToSql(),
                            new[] { "npc_template_id" }),
            new TableSchema("Quests", "quests", new QuestsCsvToSql()),
            new TableSchema("Quest Reqs", "quest_requirements", new QuestRequirementsCsvToSql()),
            new TableSchema("Quest Rewards", "quest_rewards", new QuestRewardsCsvToSql()),
            new TableSchema("Spells", "spells", new SpellsCsvToSql()),
            new TableSchema("Spell Effects", "spell_effects", new SpellEffectsCsvToSql()),
            new TableSchema("Warptiles", "warptiles", new WarpTilesCsvToSql()),
            new TableSchema("Maps", "maps", new MapsCsvToSql()),
            new TableSchema("Map Required Items", "map_required_items", new MapRequiredItemsCsvToSql(),
                            new[] { "map_id" }),
            new TableSchema("Combinations", "combinations", new CombinationsCsvToSql()),
            new TableSchema("Combination Item Required", "combination_item_required",
                            new CombinationItemRequiredCsvToSql()),
            new TableSchema("Combination Item Result", "combination_item_results",
                            new CombinationItemResultsCsvToSql()),
            new TableSchema("Titles", "item_titles", new TitleCsvToSql()),
            new TableSchema("Surnames", "item_surnames", new SurnameCsvToSql()),
            new TableSchema("Classes", "classes", new ClassesCsvToSql()),
            new TableSchema("Class Info", "class_info", new ClassInfoCsvToSql()),
            new TableSchema("Class Levelup Spells", "classes_levelup_spells",
                            new ClassLevelupSpellsCsvToSql()),
        };
    }
}
```

Note the emission order matches `sqlTemplate.sql`'s table order, not the alphabetical order of
the old `converterMapping` dictionary. Keep it that way so the normalised text diff stays small.

**Step 4: Point `CsvToSqlConverter` at the registry**

Replace `BuildConverterMapping()` and the worksheet loop in `ConvertWorkbook` with:

```csharp
            using (var workbook = new XLWorkbook(spreadsheet))
            {
                foreach (var schema in SchemaRegistry.Tables)
                {
                    if (!workbook.Worksheets.TryGetWorksheet(schema.Sheet, out var worksheet))
                        throw new InvalidOperationException(
                            $"Spreadsheet is missing required worksheet '{schema.Sheet}'.");

                    sqlTemplate = schema.Converter.Convert(worksheet, sqlTemplate, schema.Table);
                }
            }
```

This also fixes a latent bug: previously a missing worksheet left the literal `{{table}}`
placeholder in the output (`CsvToSqlBase.cs:39`), producing a script that fails obscurely at
execution time instead of a clear error.

Delete the `Dictionary<string, dynamic>` entirely.

**Step 5: Run all tests**

Run: `dotnet test Goose.sln`
Expected: PASS, including the baseline comparison.

**Step 6: Commit**

```bash
git add CsvToSql/CsvToSql.Core Goose.Tests/Schema/SchemaRegistryTests.cs
git commit -m "refactor: replace dynamic converter mapping with typed schema registry"
```

---

## Task 8: Generate the full script, delete `sqlTemplate.sql`

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/CsvToSqlConverter.cs`
- Modify: `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs`
- Modify: `CsvToSql/CsvToSql.Core/CsvToSql.Core.csproj:8`
- Delete: `CsvToSql/CsvToSql.Core/sqlTemplate.sql`
- Delete: `CsvToSql/CsvToSql.Console/sqlTemplate.sql`
- Test: `Goose.Tests/CsvToSqlEquivalenceTests.cs`

**Step 1: Write the failing test — the semantic gate**

This is the load-bearing test. It executes the recorded baseline and the newly generated
script into separate SQLite databases and compares schema and every row.

```csharp
using System.Data.SQLite;
using CsvToSql.Core;

namespace Goose.Tests;

public class CsvToSqlEquivalenceTests
{
    private static string FixtureDir => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact]
    public void Generated_script_produces_identical_database_to_baseline()
    {
        var baseline = File.ReadAllText(Path.Combine(FixtureDir, "baseline.sql"));

        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        var generated = CsvToSqlConverter.ConvertWorkbook(fs);

        var expected = Snapshot(baseline);
        var actual = Snapshot(generated);

        Assert.Equal(expected.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));
        foreach (var table in expected.Keys)
            Assert.Equal(expected[table], actual[table]);
    }

    /// <summary>Executes a script into a temp database and returns table name -> its CREATE
    /// statement followed by every row rendered as pipe-joined values, ordered by rowid.</summary>
    private static Dictionary<string, List<string>> Snapshot(string script)
    {
        var path = Path.Combine(Path.GetTempPath(), $"goose-{Guid.NewGuid():N}.db");
        try
        {
            using var conn = new SQLiteConnection($"Data Source={path}");
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = script;
                cmd.ExecuteNonQuery();
            }

            var tables = new List<(string Name, string Sql)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT name, sql FROM sqlite_master WHERE type IN ('table','index') " +
                    "AND name NOT LIKE 'sqlite_%' ORDER BY name";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    tables.Add((r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1)));
            }

            var result = new Dictionary<string, List<string>>();
            foreach (var (name, sql) in tables)
            {
                var rows = new List<string> { Normalise(sql) };

                // Indexes have no rows of their own.
                if (sql.StartsWith("CREATE TABLE", StringComparison.Ordinal))
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT * FROM {name} ORDER BY rowid";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        var vals = new string[r.FieldCount];
                        for (int i = 0; i < r.FieldCount; i++)
                            vals[i] = r.IsDBNull(i) ? "<null>" : r.GetValue(i).ToString();
                        rows.Add(string.Join("|", vals));
                    }
                }

                result[name] = rows;
            }

            return result;
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Collapses whitespace so the deliberate CRLF and indentation normalisation
    /// does not register as a schema difference.</summary>
    private static string Normalise(string sql) =>
        string.Join(" ", sql.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ")
                           .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~CsvToSqlEquivalenceTests"`
Expected: FAIL — the generated script is still template-driven, so this actually passes
trivially at first. That is fine and expected: it is a regression guard being installed
*before* the change, which is the point. Confirm it passes now, then proceed to Step 3 and
confirm it still passes after.

**Step 3: Generate the script from descriptors**

In `CsvToSqlConverter.ConvertWorkbook`, drop the embedded-resource load and build the script:

```csharp
        public static string ConvertWorkbook(Stream spreadsheet)
        {
            var sb = new StringBuilder();
            sb.Append("BEGIN TRANSACTION;\n\n");

            using (var workbook = new XLWorkbook(spreadsheet))
            {
                foreach (var schema in SchemaRegistry.Tables)
                {
                    if (!workbook.Worksheets.TryGetWorksheet(schema.Sheet, out var worksheet))
                        throw new InvalidOperationException(
                            $"Spreadsheet is missing required worksheet '{schema.Sheet}'.");

                    sb.Append(TableDdl.Emit(schema.Table, schema.Columns, schema.Indexes));
                    sb.Append('\n');
                    sb.Append(schema.Converter.BuildInserts(worksheet, schema.Table));
                    sb.Append('\n');
                }
            }

            sb.Append("COMMIT;\n");
            return sb.ToString();
        }
```

In `CsvToSqlBase`, rename `Convert` to `BuildInserts` and drop the template parameter — no
placeholder substitution remains:

```csharp
        public string BuildInserts(IXLWorksheet worksheet, string tableName)
        {
            var descriptors = GetColumnDescriptors();
            var sqlBuilder = new StringBuilder();

            foreach (var row in worksheet.Rows().Skip(1).Where(r => !r.IsEmpty()))
            {
                List<string> columns = new List<string>();
                List<string> values = new List<string>();

                for (int i = 0; i < descriptors.Length; i++)
                {
                    string value = row.Cell(i + 1).GetValue<string>();
                    if (value.Length == 0) continue;

                    columns.Add(descriptors[i].Name);
                    values.Add(DescriptorTransform.Apply(descriptors[i], value));
                }

                sqlBuilder.AppendFormat("INSERT INTO {0} (", tableName);
                sqlBuilder.Append(string.Join(", ", columns));
                sqlBuilder.Append(")\nVALUES (");
                sqlBuilder.Append(string.Join(", ", values));
                sqlBuilder.Append(");\n");
            }

            return sqlBuilder.ToString();
        }
```

Delete the now-unused `GetColumns`, `TransformValue`, `EscapeString` and `ConvertEnum` members
from `CsvToSqlBase`, and the leftover `TransformValue`/`GetColumns` overrides from every
converter.

**Step 4: Remove the template files**

```bash
git rm CsvToSql/CsvToSql.Core/sqlTemplate.sql CsvToSql/CsvToSql.Console/sqlTemplate.sql
```

Remove line 8 of `CsvToSql/CsvToSql.Core/CsvToSql.Core.csproj`:

```xml
    <EmbeddedResource Include="sqlTemplate.sql" />
```

Leave that `ItemGroup` out entirely if it becomes empty.

**Step 5: Run everything**

Run: `dotnet test Goose.sln`
Expected: PASS. The equivalence test proves the generated schema and all rows match the
baseline; the normalised text test tolerates only the whitespace changes.

The normalised-text baseline test **will** now fail on ordering and whitespace differences it
was not designed to tolerate. Delete `Output_matches_recorded_baseline` at this point — the
equivalence test supersedes it — and keep `baseline.sql` as the fixture the equivalence test
reads.

**Step 6: Verify against the real server**

The strongest end-to-end check available without a client:

```bash
cd Goose && rm -f bin/Debug/AsperetaGoose.db
dotnet run --project Goose.csproj updatesql 2>&1 | tail -20
```

Expected: no "Failed updating sql" in the output, and a fresh `.db` created. Compare table
counts against the pre-change database if you kept a copy:

```bash
sqlite3 bin/Debug/AsperetaGoose.db \
  "SELECT name, (SELECT COUNT(*) FROM pragma_table_info(name)) FROM sqlite_master WHERE type='table' ORDER BY name;"
```

**Step 7: Commit**

```bash
git add -A CsvToSql Goose.Tests
git commit -m "refactor: generate schema DDL from descriptors, delete sqlTemplate.sql"
```

---

## Task 9: Fix the broken console entry point

`CsvToSql/CsvToSql.Console/Program.cs:12` passes a full export URL into
`Convert(dataLinkId)`, which interpolates it into another URL template — that path cannot
work, and its hardcoded sheet id is stale relative to `GooseSettings.json`.

**Files:**
- Modify: `CsvToSql/CsvToSql.Console/Program.cs`

**Step 1: Take the id as an argument**

```csharp
using System;
using System.IO;

namespace CsvToSql.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                System.Console.Error.WriteLine("usage: CsvToSql.Console <google-sheet-id>");
                return 1;
            }

            var sql = CsvToSql.Core.CsvToSqlConverter.Convert(args[0]);
            File.WriteAllText("illutiaData.sql", sql);
            System.Console.WriteLine($"Wrote illutiaData.sql ({sql.Length} bytes)");
            return 0;
        }
    }
}
```

**Step 2: Verify**

```bash
dotnet build Goose.sln
dotnet run --project CsvToSql/CsvToSql.Console/CsvToSql.Console.csproj \
  -- 1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU
head -3 illutiaData.sql
```

Expected: `BEGIN TRANSACTION;` on the first line.

Also delete the stale committed output if present:

```bash
git rm --cached CsvToSql/CsvToSql.Console/illutiaData.sql 2>/dev/null || true
```

**Step 3: Commit**

```bash
git add CsvToSql/CsvToSql.Console
git commit -m "fix: take sheet id as argument in CsvToSql.Console"
```

---

## Definition of done

- `dotnet test Goose.sln` passes, including `CsvToSqlEquivalenceTests`.
- `sqlTemplate.sql` does not exist in either project, and no `EmbeddedResource` references it.
- All 21 converters expose `Column[]` and no `TransformValue` override remains.
- `SchemaRegistry.Tables` has 21 entries; no `dynamic` remains in `CsvToSqlConverter`.
- `dotnet run --project Goose/Goose.csproj updatesql` imports without error against a fresh database.
- `npc_templates.face_id` and `hair_id` default to `0`, matching the working-tree change that
  the deleted template carried.

## Notes for Part 2

`SchemaRegistry` is the input to `tools/SchemaGen`. `Column.EnumNames`, `RefSheet`,
`IsRequired`, `Default`, `Type.Sql` and the `Composite` list are exactly the fields the
editor's `schema.js` needs, so Part 2 is a serialiser over this registry with no further
schema archaeology.
