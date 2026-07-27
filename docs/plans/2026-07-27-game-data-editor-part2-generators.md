# Game Data Editor — Part 2: Schema and Sprite Generators

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Two command-line generators that turn the descriptor registry and the client's sprite assets into files the Apps Script editor can consume.

**Architecture:** `tools/SchemaGen` serialises `SchemaRegistry.Tables` to a `schema.js` global. `tools/SpriteBundle` crops sprites out of the client's atlas PNGs using rects from `manifest.json` and per-part `animations.tres`, shelf-packs them into three new atlases, and emits each as a base64 data URI plus a rect index inside a generated HTML file.

**Tech Stack:** C# / .NET 10, SixLabors.ImageSharp 3.1.12, System.Text.Json, xunit 2.9.3.

**Design doc:** `docs/plans/2026-07-27-game-data-editor-design.md`
**Depends on:** `docs/plans/2026-07-27-game-data-editor-part1-descriptors.md` (must be complete — this plan reads `CsvToSql.Core.Schema.SchemaRegistry`)

**Part 2 of 3.** Out of scope: the Apps Script editor UI (Part 3).

---

## APIs verified

Verified by compiling and running a spike against the real assets. Every API below executed successfully.

| Fact | Evidence |
|---|---|
| `Image.Load<Rgba32>(string path)` | Also used at `Goose2ClientGodot/tools/AssetConverter/src/AssetConverter/Png/PayloadDecoder.cs:20` |
| `image.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h)))` | Spike: cropped 320×320 sheet to 32×32 |
| `new Image<Rgba32>(width, height)` — initialises fully transparent | Spike: `px(32,0)=Rgba32(0, 0, 0, 0)` |
| `image[x, y]` get/set indexer on `Image<Rgba32>` | Spike: used for both read and write |
| `image.SaveAsPng(Stream)` | Also `AssetConverter/Png/PngWriter.cs:15` |
| ImageSharp version to match the client toolchain | `Goose2ClientGodot/tools/AssetConverter/src/AssetConverter/AssetConverter.csproj:11` → 3.1.12 |
| `manifest.json` deserialises to `record(int tileSize, Dictionary<string, Dictionary<string, int[]>> sheets)` | Spike: `tileSize=32 sheets=7450`, `20107/810003 = [96,0,32,32]` |
| Rects are pixels, used verbatim as `Rect2` | `Goose2ClientGodot/Scripts/Map/SpriteCache.cs:33` |
| `AsperetaSheets.GraphicBase = 700000` | `Goose2ClientGodot/tools/AssetConverter/src/AssetConverter/Aspereta/AsperetaSheets.cs:10` |
| Bulk pixel copy speed | Spike: 1.024 M px in 132 ms → ~1.7 s for the full 12.7 MPx workload |

### Two spike findings that change the implementation

**1. Do not use `DrawImage` to compose the atlas.** It is not pixel-exact. On a single 32×32
sprite the spike found 4 differing pixels — all fully transparent source pixels where
`Rgba32(1, 0, 0, 0)` became `Rgba32(0, 0, 0, 0)`. Visually irrelevant, but it makes strict
golden testing impossible and the blending semantics are an unnecessary risk. **Direct pixel
copy measured 0 mismatches** and is fast enough (1.7 s for the whole job). Use it.

**2. The obvious `.tres` clip regex is wrong.** This pattern looks right and silently returns
the wrong frame:

```csharp
// BROKEN — do not use
Regex.Match(text, @"\{""frames"": \[(.*?)\],""loop""[^}]*?""name"": &""" + clip + @"""");
```

The whole `animations = [...]` array is one line, and the frames content itself contains `}`,
so the lazy `(.*?)` happily spans *earlier clips* to reach the requested name. The captured
group then starts at the file's first frame, so every clip resolves to `Atlas_0`. The spike hit
exactly this: all four idle clips reported `sheet 115 rect 0,0,24,48`.

**Correct approach** — match every clip object globally, capturing frames and name together,
then look up by name:

```csharp
@"\{""frames"": \[(.*?)\],""loop"": (?:true|false),""name"": &""([^""]+)"",""speed"": [\d.]+\}"
```

Verified output for `Bodies/1`: 68 clips, `idle-no-equip-down`→`Atlas_22` (sheet 115, rect
0,48,24,48), `idle-down`→`Atlas_23`, `idle-equip-down`→`Atlas_59` (sheet **116**),
`mounted-idle-down`→`Atlas_151` (sheet 125, rect 0,80,**52,80** — the mount pose). Distinct and
correct.

**Consequence for the design doc's numbers:** the character-parts figures (3,261 frames,
1.12 MB) were measured with the broken parser, so they were counting the wrong frames. Sprite
*sizes* were real, so the magnitude holds, but treat the counts as approximate. The generator
must **print actual counts and byte sizes**, and no test may assert an exact byte total.

---

## Task 0: Scaffold the `tools/` projects

**Files:**
- Create: `tools/SchemaGen/SchemaGen.csproj`
- Create: `tools/SchemaGen/Program.cs`
- Create: `tools/SpriteBundle/SpriteBundle.csproj`
- Create: `tools/SpriteBundle/Program.cs`
- Create: `tools/Tools.Tests/Tools.Tests.csproj`
- Modify: `Goose.sln`

**Step 1: Create the three projects**

```bash
mkdir -p tools/SchemaGen tools/SpriteBundle tools/Tools.Tests
```

`tools/SchemaGen/SchemaGen.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Goose.Tools.SchemaGen</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\CsvToSql\CsvToSql.Core\CsvToSql.Core.csproj" />
  </ItemGroup>

</Project>
```

`tools/SpriteBundle/SpriteBundle.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Goose.Tools.SpriteBundle</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />
  </ItemGroup>

</Project>
```

`tools/Tools.Tests/Tools.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SchemaGen\SchemaGen.csproj" />
    <ProjectReference Include="..\SpriteBundle\SpriteBundle.csproj" />
  </ItemGroup>

</Project>
```

Package versions match `Goose.Tests/Goose.Tests.csproj:11-14` so the repo has one test stack.

**Step 2: Placeholder entry points**

`tools/SchemaGen/Program.cs`:

```csharp
namespace Goose.Tools.SchemaGen;

public static class Program
{
    public static int Main(string[] args) => 0;
}
```

`tools/SpriteBundle/Program.cs`: same shape, namespace `Goose.Tools.SpriteBundle`.

**Step 3: Add to the solution**

```bash
dotnet sln Goose.sln add tools/SchemaGen/SchemaGen.csproj \
                          tools/SpriteBundle/SpriteBundle.csproj \
                          tools/Tools.Tests/Tools.Tests.csproj
dotnet build Goose.sln
```

Expected: `0 Error(s)`, 7 projects built.

**Step 4: Commit**

```bash
git add tools Goose.sln
git commit -m "chore: scaffold SchemaGen and SpriteBundle tool projects"
```

---

## Task 1: Serialise the schema registry to JSON

**Files:**
- Create: `tools/SchemaGen/SchemaModel.cs`
- Test: `tools/Tools.Tests/SchemaModelTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose.Tools.SchemaGen;

namespace Tools.Tests;

public class SchemaModelTests
{
    [Fact]
    public void Includes_every_registered_sheet()
    {
        var model = SchemaModel.Build();

        Assert.Equal(21, model.Sheets.Count);
    }

    [Fact]
    public void Items_sheet_carries_table_and_primary_key()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");

        Assert.Equal("item_templates", items.Table);
        Assert.Equal("item_template_id", items.Columns[0].Name);
        Assert.True(items.Columns[0].Pk);
        Assert.True(items.Columns[0].Required);
        Assert.Equal("INTEGER", items.Columns[0].Sql);
    }

    [Fact]
    public void Enum_columns_expose_member_names()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");
        var usetype = items.Columns.Single(c => c.Name == "item_usetype");

        Assert.Equal("Enum", usetype.Kind);
        Assert.NotNull(usetype.EnumNames);
        Assert.NotEmpty(usetype.EnumNames!);
    }

    [Fact]
    public void Foreign_keys_expose_target_sheet()
    {
        var drops = SchemaModel.Build().Sheets.Single(s => s.Sheet == "NPC Drops");

        Assert.Equal("NPCs", drops.Columns.Single(c => c.Name == "npc_template_id").Ref);
        Assert.Equal("Items", drops.Columns.Single(c => c.Name == "item_template_id").Ref);
    }

    [Fact]
    public void Optional_columns_report_their_default()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");
        var desc = items.Columns.Single(c => c.Name == "item_description");

        Assert.False(desc.Required);
        Assert.Equal("''", desc.Default);
    }

    [Fact]
    public void Mandatory_graphic_columns_are_required()
    {
        var model = SchemaModel.Build();

        // sqlTemplate.sql:35 and :204 — NOT NULL with no DEFAULT.
        Assert.True(model.Sheets.Single(s => s.Sheet == "Items")
                         .Columns.Single(c => c.Name == "graphic_tile").Required);
        Assert.True(model.Sheets.Single(s => s.Sheet == "Spells")
                         .Columns.Single(c => c.Name == "spellbook_graphic").Required);
    }

    [Fact]
    public void Composites_are_reported_with_their_columns()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");
        var graphic = items.Composites.Single(c => c.Kind == "Graphic");

        Assert.Equal(new[] { "graphic_tile", "graphic_file" }, graphic.Columns);
    }

    [Fact]
    public void Indexes_are_reported()
    {
        var vendor = SchemaModel.Build().Sheets.Single(s => s.Sheet == "NPC Vendor Items");

        Assert.Equal(new[] { "npc_template_id" }, vendor.Indexes);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~SchemaModelTests"`
Expected: FAIL — `SchemaModel` does not exist.

**Step 3: Write the implementation**

```csharp
using CsvToSql.Core.Schema;

namespace Goose.Tools.SchemaGen;

/// <summary>Serialisation shape for the editor's schema.js. Property names are lowercase in
/// JSON (see SchemaJs) because the Apps Script side reads them directly.</summary>
public sealed record SchemaColumn(
    string Name,
    string Kind,
    string Sql,
    string? Default,
    bool Required,
    bool Pk,
    string? Ref,
    IReadOnlyList<string>? EnumNames);

public sealed record SchemaComposite(
    string Kind,
    IReadOnlyList<string> Columns,
    string? Source);

public sealed record SchemaSheet(
    string Sheet,
    string Table,
    IReadOnlyList<SchemaColumn> Columns,
    IReadOnlyList<SchemaComposite> Composites,
    IReadOnlyList<string> Indexes);

public sealed record SchemaRoot(IReadOnlyList<SchemaSheet> Sheets);

public static class SchemaModel
{
    /// <summary>Projects SchemaRegistry.Tables into the editor-facing shape. No schema
    /// knowledge lives here — it is a pure mapping.</summary>
    public static SchemaRoot Build() => new(
        SchemaRegistry.Tables.Select(t => new SchemaSheet(
            t.Sheet,
            t.Table,
            t.Columns.Select(c => new SchemaColumn(
                c.Name,
                c.Kind.ToString(),
                c.Type.Sql,
                c.Default,
                c.IsRequired,
                c.IsPrimaryKey,
                c.RefSheet,
                c.Kind == ColumnKind.Enum ? c.EnumNames : null)).ToList(),
            t.Composites.Select(x => new SchemaComposite(
                x.Kind.ToString(),
                x.Columns,
                x.SourceSheet)).ToList(),
            t.Indexes)).ToList());
}
```

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~SchemaModelTests"`
Expected: PASS, 8 tests.

**Step 5: Commit**

```bash
git add tools/SchemaGen/SchemaModel.cs tools/Tools.Tests/SchemaModelTests.cs
git commit -m "feat: project schema registry into editor-facing model"
```

---

## Task 2: Emit `schema.js`

**Files:**
- Create: `tools/SchemaGen/SchemaJs.cs`
- Modify: `tools/SchemaGen/Program.cs`
- Test: `tools/Tools.Tests/SchemaJsTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose.Tools.SchemaGen;

namespace Tools.Tests;

public class SchemaJsTests
{
    [Fact]
    public void Assigns_a_single_global()
    {
        var js = SchemaJs.Render(SchemaModel.Build());

        Assert.StartsWith("// Generated by tools/SchemaGen", js);
        Assert.Contains("var GOOSE_SCHEMA = {", js);
        Assert.EndsWith("};\n", js);
    }

    [Fact]
    public void Uses_lowercase_json_property_names()
    {
        var js = SchemaJs.Render(SchemaModel.Build());

        Assert.Contains("\"sheet\": \"Items\"", js);
        Assert.Contains("\"table\": \"item_templates\"", js);
        Assert.DoesNotContain("\"Sheet\":", js);
    }

    [Fact]
    public void Omits_null_optional_fields()
    {
        var js = SchemaJs.Render(SchemaModel.Build());

        // Non-enum, non-FK columns should not carry empty keys.
        Assert.DoesNotContain("\"enumNames\": null", js);
        Assert.DoesNotContain("\"ref\": null", js);
    }

    [Fact]
    public void Body_is_parseable_json()
    {
        var js = SchemaJs.Render(SchemaModel.Build());

        var start = js.IndexOf('{');
        var json = js[start..js.LastIndexOf('}')..] + "}";
        var doc = System.Text.Json.JsonDocument.Parse(json);

        Assert.Equal(21, doc.RootElement.GetProperty("sheets").GetArrayLength());
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~SchemaJsTests"`
Expected: FAIL — `SchemaJs` does not exist.

**Step 3: Write the implementation**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose.Tools.SchemaGen;

/// <summary>Renders the schema model as a JS file assigning one global. The Apps Script editor
/// includes this verbatim; it is not fetched or parsed at runtime.</summary>
public static class SchemaJs
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Render(SchemaRoot model)
    {
        var json = JsonSerializer.Serialize(model, Options);
        return "// Generated by tools/SchemaGen. Do not edit by hand.\n" +
               $"var GOOSE_SCHEMA = {json};\n";
    }
}
```

`tools/SchemaGen/Program.cs`:

```csharp
namespace Goose.Tools.SchemaGen;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: SchemaGen <output-path/schema.js>");
            return 1;
        }

        var js = SchemaJs.Render(SchemaModel.Build());
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
        File.WriteAllText(args[0], js);

        Console.WriteLine($"Wrote {args[0]} ({js.Length:N0} bytes, " +
                          $"{SchemaModel.Build().Sheets.Count} sheets)");
        return 0;
    }
}
```

**Step 4: Run the tests and the tool**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~SchemaJsTests"`
Expected: PASS, 4 tests.

```bash
dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js
head -20 tools/DataEditor/schema.js
```

Expected: the header comment, `var GOOSE_SCHEMA = {`, then `"sheets": [` with `"sheet": "Items"`.

**Step 5: Commit**

```bash
git add tools/SchemaGen tools/Tools.Tests/SchemaJsTests.cs tools/DataEditor/schema.js
git commit -m "feat: emit schema.js for the data editor"
```

---

## Task 3: Manifest loader

**Files:**
- Create: `tools/SpriteBundle/Manifest.cs`
- Test: `tools/Tools.Tests/ManifestTests.cs`

The manifest lives in the client repo, which is not part of this solution. Tests take its path
from an environment variable so CI without the client checkout skips rather than fails.

**Step 1: Write the failing test**

```csharp
using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class ManifestTests
{
    /// <summary>Client asset root. Defaults to a sibling checkout; override with
    /// GOOSE_CLIENT_ASSETS when the client lives elsewhere.</summary>
    internal static string? AssetRoot
    {
        get
        {
            var root = Environment.GetEnvironmentVariable("GOOSE_CLIENT_ASSETS")
                       ?? "../Goose2ClientGodot/Assets/Sprites";
            var full = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..", "..", root));
            return File.Exists(Path.Combine(full, "manifest.json")) ? full : null;
        }
    }

    [SkippableFact]
    public void Loads_sheets_and_rects()
    {
        Skip.If(AssetRoot is null, "client assets not available");

        var m = Manifest.Load(AssetRoot!);

        Assert.Equal(32, m.TileSize);
        Assert.Equal(7450, m.Sheets.Count);
    }

    [SkippableFact]
    public void Rect_matches_the_raw_manifest()
    {
        Skip.If(AssetRoot is null, "client assets not available");

        var m = Manifest.Load(AssetRoot!);

        // Verified against manifest.json: sheet 20107, graphic 810003.
        Assert.True(m.TryGetRect(20107, 810003, out var r));
        Assert.Equal((96, 0, 32, 32), (r.X, r.Y, r.W, r.H));
    }

    [SkippableFact]
    public void Missing_graphic_returns_false()
    {
        Skip.If(AssetRoot is null, "client assets not available");

        var m = Manifest.Load(AssetRoot!);

        // graphic_file 0 is the "no graphic" sentinel and is absent from the manifest.
        Assert.False(m.TryGetRect(0, 0, out _));
    }
}
```

`SkippableFact` needs `Xunit.SkippableFact`. Add to `tools/Tools.Tests/Tools.Tests.csproj`:

```xml
    <PackageReference Include="Xunit.SkippableFact" Version="1.5.23" />
```

and `<Using Include="Xunit" />` already covers `Skip`.

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~ManifestTests"`
Expected: FAIL — `Manifest` does not exist.

**Step 3: Write the implementation**

```csharp
using System.Text.Json;

namespace Goose.Tools.SpriteBundle;

public readonly record struct SpriteRect(int X, int Y, int W, int H);

/// <summary>Reads Assets/Sprites/manifest.json, produced by the client's
/// AssetConverter FrameManifestBuilder. Rects are pixel coordinates into
/// sheets/&lt;sheet&gt;.png and are used verbatim (see Scripts/Map/SpriteCache.cs:33).</summary>
public sealed class Manifest
{
    private sealed record Root(int tileSize, Dictionary<string, Dictionary<string, int[]>> sheets);

    public int TileSize { get; }
    public IReadOnlyDictionary<int, Dictionary<int, SpriteRect>> Sheets { get; }
    public string AssetRoot { get; }

    private Manifest(string assetRoot, int tileSize,
                     Dictionary<int, Dictionary<int, SpriteRect>> sheets)
    {
        AssetRoot = assetRoot;
        TileSize = tileSize;
        Sheets = sheets;
    }

    public static Manifest Load(string assetRoot)
    {
        using var fs = File.OpenRead(Path.Combine(assetRoot, "manifest.json"));
        var root = JsonSerializer.Deserialize<Root>(fs)
                   ?? throw new InvalidDataException("manifest.json did not deserialise");

        var sheets = new Dictionary<int, Dictionary<int, SpriteRect>>(root.sheets.Count);
        foreach (var (sheetKey, graphics) in root.sheets)
        {
            var inner = new Dictionary<int, SpriteRect>(graphics.Count);
            foreach (var (graphicKey, r) in graphics)
                inner[int.Parse(graphicKey)] = new SpriteRect(r[0], r[1], r[2], r[3]);

            sheets[int.Parse(sheetKey)] = inner;
        }

        return new Manifest(assetRoot, root.tileSize, sheets);
    }

    public bool TryGetRect(int sheet, int graphic, out SpriteRect rect)
    {
        rect = default;
        return Sheets.TryGetValue(sheet, out var g) && g.TryGetValue(graphic, out rect);
    }

    public string SheetPath(int sheet) =>
        Path.Combine(AssetRoot, "sheets", $"{sheet}.png");
}
```

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~ManifestTests"`
Expected: PASS, 3 tests (or skipped if the client checkout is absent).

**Step 5: Commit**

```bash
git add tools/SpriteBundle/Manifest.cs tools/Tools.Tests/ManifestTests.cs tools/Tools.Tests/Tools.Tests.csproj
git commit -m "feat: load client sprite manifest"
```

---

## Task 4: Shelf packer

**Files:**
- Create: `tools/SpriteBundle/ShelfPacker.cs`
- Test: `tools/Tools.Tests/ShelfPackerTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class ShelfPackerTests
{
    private static IReadOnlyList<(int W, int H)> Sizes(params (int, int)[] s) => s;

    [Fact]
    public void Places_tallest_first_and_starts_new_row_when_full()
    {
        var packed = ShelfPacker.Pack(Sizes((60, 10), (60, 20), (60, 30)), width: 120);

        // Sorted tallest-first: 30, 20, 10. First two share row 0, third wraps.
        Assert.Equal(new[] { 0, 1, 2 }, packed.Placements.Select(p => p.Index).ToArray());
        Assert.Equal((0, 0), (packed.Placements[0].X, packed.Placements[0].Y));
        Assert.Equal((60, 0), (packed.Placements[1].X, packed.Placements[1].Y));
        Assert.Equal((0, 30), (packed.Placements[2].X, packed.Placements[2].Y));
        Assert.Equal(120, packed.Width);
        Assert.Equal(40, packed.Height);
    }

    [Fact]
    public void Placements_report_original_indices()
    {
        // Input order short, tall. Packing sorts tall first, but placements must map back.
        var packed = ShelfPacker.Pack(Sizes((10, 5), (10, 50)), width: 100);

        var tall = packed.Placements.Single(p => p.Index == 1);
        Assert.Equal((0, 0), (tall.X, tall.Y));

        var shortOne = packed.Placements.Single(p => p.Index == 0);
        Assert.Equal(10, shortOne.X);
        Assert.Equal(0, shortOne.Y);
    }

    [Fact]
    public void No_two_sprites_overlap()
    {
        var rng = new Random(1234);
        var sizes = Enumerable.Range(0, 500)
            .Select(_ => (rng.Next(8, 100), rng.Next(8, 120)))
            .ToList();

        var packed = ShelfPacker.Pack(sizes, width: 2048);

        var occupied = new HashSet<(int, int)>();
        foreach (var p in packed.Placements)
        {
            var (w, h) = sizes[p.Index];
            for (int y = p.Y; y < p.Y + h; y++)
            for (int x = p.X; x < p.X + w; x++)
                Assert.True(occupied.Add((x, y)), $"overlap at {x},{y}");
        }
    }

    [Fact]
    public void Nothing_exceeds_the_configured_width()
    {
        var rng = new Random(99);
        var sizes = Enumerable.Range(0, 300).Select(_ => (rng.Next(8, 200), rng.Next(8, 80))).ToList();

        var packed = ShelfPacker.Pack(sizes, width: 512);

        foreach (var p in packed.Placements)
            Assert.True(p.X + sizes[p.Index].Item1 <= 512);
    }

    [Fact]
    public void Achieves_high_area_efficiency_on_uniform_input()
    {
        var sizes = Enumerable.Repeat((32, 32), 4096).ToList();

        var packed = ShelfPacker.Pack(sizes, width: 2048);

        var used = sizes.Sum(s => s.Item1 * s.Item2);
        var total = packed.Width * packed.Height;
        Assert.True(used / (double)total > 0.99, $"efficiency {used / (double)total:P1}");
    }

    [Fact]
    public void Empty_input_produces_empty_atlas()
    {
        var packed = ShelfPacker.Pack(Array.Empty<(int, int)>(), width: 2048);

        Assert.Empty(packed.Placements);
        Assert.Equal(0, packed.Height);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~ShelfPackerTests"`
Expected: FAIL — `ShelfPacker` does not exist.

**Step 3: Write the implementation**

```csharp
namespace Goose.Tools.SpriteBundle;

public readonly record struct Placement(int Index, int X, int Y);

public sealed record PackResult(int Width, int Height, IReadOnlyList<Placement> Placements);

/// <summary>Shelf (row) packing: sort tallest-first, fill a fixed-width row left to right,
/// start a new row when a sprite will not fit. Row height is its tallest sprite. Measured
/// 95-98% area efficiency on the real sprite sets, which is close enough to optimal that a
/// full bin packer is not worth the dependency.</summary>
public static class ShelfPacker
{
    public static PackResult Pack(IReadOnlyList<(int W, int H)> sizes, int width)
    {
        var order = Enumerable.Range(0, sizes.Count)
            .OrderByDescending(i => sizes[i].H)
            .ThenBy(i => i)          // stable, so output is deterministic
            .ToList();

        var placements = new List<Placement>(sizes.Count);
        int x = 0, y = 0, rowHeight = 0;

        foreach (var i in order)
        {
            var (w, h) = sizes[i];
            if (w > width)
                throw new ArgumentException(
                    $"sprite {i} is {w}px wide, wider than the {width}px atlas");

            if (x + w > width)
            {
                x = 0;
                y += rowHeight;
                rowHeight = 0;
            }

            placements.Add(new Placement(i, x, y));
            x += w;
            if (h > rowHeight) rowHeight = h;
        }

        return new PackResult(width, y + rowHeight, placements);
    }
}
```

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~ShelfPackerTests"`
Expected: PASS, 6 tests.

**Step 5: Commit**

```bash
git add tools/SpriteBundle/ShelfPacker.cs tools/Tools.Tests/ShelfPackerTests.cs
git commit -m "feat: add shelf packer for sprite atlases"
```

---

## Task 5: Atlas builder with exact pixel copy

**Files:**
- Create: `tools/SpriteBundle/AtlasBuilder.cs`
- Test: `tools/Tools.Tests/AtlasBuilderTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose.Tools.SpriteBundle;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tools.Tests;

public class AtlasBuilderTests
{
    [SkippableFact]
    public void Copies_pixels_exactly_including_transparent_rgb()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var manifest = Manifest.Load(ManifestTests.AssetRoot!);

        // Sheet 20107 contains fully transparent pixels whose RGB is (1,0,0). ImageSharp's
        // DrawImage zeroes those; direct pixel copy must preserve them.
        var sources = new[]
        {
            new SpriteRef("20107:810003", 20107, 810003),
            new SpriteRef("20107:810004", 20107, 810004),
        };

        using var built = AtlasBuilder.Build(manifest, sources, width: 2048);

        using var sheet = Image.Load<Rgba32>(manifest.SheetPath(20107));
        manifest.TryGetRect(20107, 810003, out var src);
        var dst = built.Rects["20107:810003"];

        for (int y = 0; y < src.H; y++)
        for (int x = 0; x < src.W; x++)
            Assert.Equal(sheet[src.X + x, src.Y + y],
                         built.Image[dst.X + x, dst.Y + y]);
    }

    [SkippableFact]
    public void Rect_index_preserves_sprite_dimensions_from_the_manifest()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var manifest = Manifest.Load(ManifestTests.AssetRoot!);

        var sources = new[] { new SpriteRef("k", 20107, 810003) };
        using var built = AtlasBuilder.Build(manifest, sources, width: 2048);

        manifest.TryGetRect(20107, 810003, out var src);
        Assert.Equal(src.W, built.Rects["k"].W);
        Assert.Equal(src.H, built.Rects["k"].H);
    }

    [SkippableFact]
    public void Skips_graphics_absent_from_the_manifest()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var manifest = Manifest.Load(ManifestTests.AssetRoot!);

        var sources = new[]
        {
            new SpriteRef("real", 20107, 810003),
            new SpriteRef("bogus", 999999, 1),
        };

        using var built = AtlasBuilder.Build(manifest, sources, width: 2048);

        Assert.True(built.Rects.ContainsKey("real"));
        Assert.False(built.Rects.ContainsKey("bogus"));
        Assert.Single(built.Skipped);
        Assert.Equal("bogus", built.Skipped[0]);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~AtlasBuilderTests"`
Expected: FAIL — `AtlasBuilder` does not exist.

**Step 3: Write the implementation**

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Goose.Tools.SpriteBundle;

/// <summary>One sprite to pull into a bundle. Key is what the editor looks up by.</summary>
public readonly record struct SpriteRef(string Key, int Sheet, int Graphic);

public sealed class BuiltAtlas : IDisposable
{
    public required Image<Rgba32> Image { get; init; }
    public required Dictionary<string, SpriteRect> Rects { get; init; }
    public required List<string> Skipped { get; init; }

    public void Dispose() => Image.Dispose();
}

/// <summary>Crops sprites out of the client's sheet PNGs and packs them into one atlas.
///
/// Uses direct pixel assignment rather than ctx.DrawImage: DrawImage is not pixel-exact,
/// it rewrites fully transparent pixels from Rgba32(1,0,0,0) to Rgba32(0,0,0,0). Invisible
/// in practice, but it defeats exact verification. Direct copy runs ~1.7s for the full
/// 12.7 MPx workload, so there is no reason to prefer the faster path.</summary>
public static class AtlasBuilder
{
    public static BuiltAtlas Build(Manifest manifest, IReadOnlyList<SpriteRef> sources, int width)
    {
        var resolved = new List<(SpriteRef Ref, SpriteRect Rect)>(sources.Count);
        var skipped = new List<string>();

        foreach (var s in sources)
        {
            if (manifest.TryGetRect(s.Sheet, s.Graphic, out var rect) &&
                File.Exists(manifest.SheetPath(s.Sheet)))
                resolved.Add((s, rect));
            else
                skipped.Add(s.Key);
        }

        var packed = ShelfPacker.Pack(
            resolved.Select(r => (r.Rect.W, r.Rect.H)).ToList(), width);

        var atlas = new Image<Rgba32>(packed.Width, Math.Max(packed.Height, 1));
        var rects = new Dictionary<string, SpriteRect>(resolved.Count);

        // Group by sheet so each PNG is decoded once — sheets are shared by many sprites.
        foreach (var group in packed.Placements.GroupBy(p => resolved[p.Index].Ref.Sheet))
        {
            using var sheet = Image.Load<Rgba32>(manifest.SheetPath(group.Key));

            foreach (var p in group)
            {
                var (sref, src) = resolved[p.Index];

                for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                    atlas[p.X + x, p.Y + y] = sheet[src.X + x, src.Y + y];

                rects[sref.Key] = new SpriteRect(p.X, p.Y, src.W, src.H);
            }
        }

        return new BuiltAtlas { Image = atlas, Rects = rects, Skipped = skipped };
    }
}
```

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~AtlasBuilderTests"`
Expected: PASS, 3 tests.

**Step 5: Commit**

```bash
git add tools/SpriteBundle/AtlasBuilder.cs tools/Tools.Tests/AtlasBuilderTests.cs
git commit -m "feat: build sprite atlases with exact pixel copy"
```

---

## Task 6: Godot `.tres` clip parser

**Files:**
- Create: `tools/SpriteBundle/TresParser.cs`
- Test: `tools/Tools.Tests/TresParserTests.cs`

**Step 1: Write the failing test**

The third test is the important one — it pins the bug described in *APIs verified*.

```csharp
using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class TresParserTests
{
    private static string PartPath(string category, int id) =>
        Path.Combine(ManifestTests.AssetRoot!, category, id.ToString(), "animations.tres");

    [SkippableFact]
    public void Parses_every_clip_in_the_file()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        // Verified: Bodies/1 declares 68 clips over 164 AtlasTexture sub-resources.
        Assert.Equal(68, parts.Clips.Count);
    }

    [SkippableFact]
    public void Resolves_first_frame_to_its_sheet_and_rect()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        // Verified by spike against Bodies/1/animations.tres.
        Assert.True(parts.TryGetFirstFrame("idle-no-equip-down", out var idle));
        Assert.Equal((115, 0, 48, 24, 48), (idle.Sheet, idle.X, idle.Y, idle.W, idle.H));

        Assert.True(parts.TryGetFirstFrame("mounted-idle-down", out var mounted));
        Assert.Equal((125, 0, 80, 52, 80), (mounted.Sheet, mounted.X, mounted.Y, mounted.W, mounted.H));
    }

    [SkippableFact]
    public void Distinct_clips_resolve_to_distinct_frames()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        // Regression guard: a lazy regex that spans clip boundaries makes every clip
        // resolve to the file's first frame. These three must differ.
        parts.TryGetFirstFrame("idle-no-equip-down", out var a);
        parts.TryGetFirstFrame("idle-equip-down", out var b);
        parts.TryGetFirstFrame("mounted-idle-down", out var c);

        Assert.NotEqual(a, b);
        Assert.NotEqual(b, c);
        Assert.NotEqual(a, c);
        Assert.Equal(116, b.Sheet);   // idle-equip-down lives on a different sheet
    }

    [SkippableFact]
    public void Absent_clip_reports_false()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        Assert.False(parts.TryGetFirstFrame("no-such-clip", out _));
    }

    [SkippableFact]
    public void Effect_animations_have_one_clip_named_after_the_id()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var dir = Directory.GetDirectories(Path.Combine(ManifestTests.AssetRoot!, "Effects"))
                           .OrderBy(d => d).First();
        var id = Path.GetFileName(dir);

        var parts = TresParser.Parse(Path.Combine(dir, "animations.tres"));

        Assert.Single(parts.Clips);
        Assert.Equal(id, parts.Clips.Keys.Single());
        Assert.True(parts.Clips[id].Count >= 1);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~TresParserTests"`
Expected: FAIL — `TresParser` does not exist.

**Step 3: Write the implementation**

```csharp
using System.Globalization;
using System.Text.RegularExpressions;

namespace Goose.Tools.SpriteBundle;

/// <summary>A frame's location: which sheet PNG, and the pixel rect within it.</summary>
public readonly record struct TresFrame(int Sheet, int X, int Y, int W, int H);

public sealed class TresFile
{
    /// <summary>Clip name to its ordered frame list.</summary>
    public required Dictionary<string, List<TresFrame>> Clips { get; init; }

    public bool TryGetFirstFrame(string clip, out TresFrame frame)
    {
        frame = default;
        if (!Clips.TryGetValue(clip, out var frames) || frames.Count == 0) return false;
        frame = frames[0];
        return true;
    }
}

/// <summary>Parses Godot .tres SpriteFrames resources produced by the client's AssetConverter.
///
/// The animations array is a single very long line and frame entries contain '}' characters,
/// so a per-clip lazy regex will span earlier clips and silently return the wrong frame.
/// Instead every clip object is matched globally, capturing frames and name together.</summary>
public static class TresParser
{
    private static readonly Regex ExtResource = new(
        @"\[ext_resource type=""Texture2D"" path=""res://Assets/Sprites/sheets/(\d+)\.png"" id=""([^""]+)""\]",
        RegexOptions.Compiled);

    private static readonly Regex SubResource = new(
        @"\[sub_resource type=""AtlasTexture"" id=""([^""]+)""\]\s*\natlas = ExtResource\(""([^""]+)""\)\s*\nregion = Rect2\(([\d.]+), ([\d.]+), ([\d.]+), ([\d.]+)\)",
        RegexOptions.Compiled);

    private static readonly Regex Clip = new(
        @"\{""frames"": \[(.*?)\],""loop"": (?:true|false),""name"": &""([^""]+)"",""speed"": [\d.]+\}",
        RegexOptions.Compiled);

    private static readonly Regex FrameRef = new(
        @"SubResource\(""([^""]+)""\)", RegexOptions.Compiled);

    public static TresFile Parse(string path)
    {
        var text = File.ReadAllText(path);

        var textures = new Dictionary<string, int>();
        foreach (Match m in ExtResource.Matches(text))
            textures[m.Groups[2].Value] = int.Parse(m.Groups[1].Value);

        var atlases = new Dictionary<string, TresFrame>();
        foreach (Match m in SubResource.Matches(text))
        {
            if (!textures.TryGetValue(m.Groups[2].Value, out var sheet)) continue;

            atlases[m.Groups[1].Value] = new TresFrame(
                sheet,
                Px(m.Groups[3].Value), Px(m.Groups[4].Value),
                Px(m.Groups[5].Value), Px(m.Groups[6].Value));
        }

        var clips = new Dictionary<string, List<TresFrame>>();
        foreach (Match m in Clip.Matches(text))
        {
            var frames = new List<TresFrame>();
            foreach (Match f in FrameRef.Matches(m.Groups[1].Value))
                if (atlases.TryGetValue(f.Groups[1].Value, out var frame))
                    frames.Add(frame);

            clips[m.Groups[2].Value] = frames;
        }

        return new TresFile { Clips = clips };
    }

    /// <summary>Rect2 components are written as floats ("0" or "0.0"); truncate to pixels.</summary>
    private static int Px(string value) =>
        (int)double.Parse(value, CultureInfo.InvariantCulture);
}
```

**Step 4: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~TresParserTests"`
Expected: PASS, 5 tests.

**Step 5: Commit**

```bash
git add tools/SpriteBundle/TresParser.cs tools/Tools.Tests/TresParserTests.cs
git commit -m "feat: parse Godot .tres sprite frame resources"
```

---

## Task 7: `sheets.json` config and the derive command

The icon bundle covers whole sheets referenced by the data, not just graphics in use — that is
what widens the palette. Which sheets those are gets derived once from the datasets and then
checked in, so the file is also the escape hatch for adding sheets the data has never touched.

**Files:**
- Create: `tools/SpriteBundle/BundleConfig.cs`
- Create: `tools/SpriteBundle/sheets.json`
- Test: `tools/Tools.Tests/BundleConfigTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class BundleConfigTests
{
    private static string ConfigPath => Path.Combine(
        AppContext.BaseDirectory, "sheets.json");

    [Fact]
    public void Loads_the_checked_in_config()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.NotEmpty(config.IconSheets);
        Assert.Equal(2048, config.AtlasWidth);
    }

    [Fact]
    public void Icon_sheets_include_both_datasets()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Contains(20107, config.IconSheets);   // Aspereta spellbook/buff icon sheet
        Assert.Contains(2269, config.IconSheets);    // Illutia item sheet
        Assert.Contains(20398, config.IconSheets);   // Aspereta item sheet
    }

    [Fact]
    public void Part_categories_cover_all_nine_directories()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Equal(
            new[] { "Bodies", "Chest", "Effects", "Eyes", "Feet", "Hair", "Hands", "Helms", "Legs" },
            config.PartCategories.OrderBy(c => c).ToArray());
    }

    [Fact]
    public void Part_clips_are_the_four_resting_poses()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Equal(
            new[] { "idle-no-equip-down", "idle-down", "idle-equip-down", "mounted-idle-down" },
            config.PartClips);
    }

    [Fact]
    public void Sentinel_sheet_zero_is_not_included()
    {
        var config = BundleConfig.Load(ConfigPath);

        // graphic_file 0 means "no graphic" and has no manifest entry.
        Assert.DoesNotContain(0, config.IconSheets);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~BundleConfigTests"`
Expected: FAIL — `BundleConfig` does not exist.

**Step 3: Write the config type**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose.Tools.SpriteBundle;

/// <summary>Which sheets and clips go into each bundle. Seeded by `SpriteBundle derive-sheets`
/// from the live datasets, then hand-editable: add sheet numbers here to make graphics
/// selectable in the editor before any data references them.</summary>
public sealed class BundleConfig
{
    [JsonPropertyName("atlasWidth")]
    public int AtlasWidth { get; init; } = 2048;

    /// <summary>Sheets whose every graphic goes into the icons bundle.</summary>
    [JsonPropertyName("iconSheets")]
    public List<int> IconSheets { get; init; } = new();

    [JsonPropertyName("partCategories")]
    public List<string> PartCategories { get; init; } = new();

    /// <summary>Resting-pose clips. body_state only selects equip vs no-equip for idle; its
    /// 4/5/6/7 weapon variants affect attack clips only (see AnimationNames.Candidates in
    /// the client), so no attack poses are needed for a static preview.</summary>
    [JsonPropertyName("partClips")]
    public List<string> PartClips { get; init; } = new();

    [JsonPropertyName("effectsCategory")]
    public string EffectsCategory { get; init; } = "Effects";

    public static BundleConfig Load(string path) =>
        JsonSerializer.Deserialize<BundleConfig>(File.ReadAllText(path))
        ?? throw new InvalidDataException($"{path} did not deserialise");
}
```

**Step 4: Add the derive command**

Add to `tools/SpriteBundle/Program.cs` a `derive-sheets` verb that reads one or more `.xlsx`
files and prints the union of referenced sheets. Column positions come from the descriptor
order in `CsvToSql.Core` — but SpriteBundle must not depend on that project (it would drag
ClosedXML into an image tool), so the derive command takes the columns as arguments.

Simplest correct approach: derive from the **committed SQLite database** instead, which already
has named columns. Create `tools/SpriteBundle/SheetDeriver.cs`:

```csharp
using System.Data.SQLite;

namespace Goose.Tools.SpriteBundle;

/// <summary>Derives the icon sheet list from a built game database. Run once per dataset and
/// union the results into sheets.json.</summary>
public static class SheetDeriver
{
    private static readonly string[] Queries =
    {
        "SELECT DISTINCT graphic_file FROM item_templates",
        "SELECT DISTINCT spellbook_graphic_file FROM spells",
        "SELECT DISTINCT buff_graphic_file FROM spell_effects WHERE buff_graphic > 0",
        "SELECT DISTINCT spell_animation_file FROM spell_effects WHERE spell_animation > 0",
    };

    public static SortedSet<int> Derive(string dbPath)
    {
        var sheets = new SortedSet<int>();

        using var conn = new SQLiteConnection($"Data Source={dbPath};Read Only=True");
        conn.Open();

        foreach (var sql in Queries)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                if (r.IsDBNull(0)) continue;
                var sheet = Convert.ToInt32(r.GetValue(0));
                if (sheet > 0) sheets.Add(sheet);   // 0 is the "no graphic" sentinel
            }
        }

        return sheets;
    }
}
```

Add `<PackageReference Include="System.Data.SQLite.Core" Version="1.0.119" />` to
`tools/SpriteBundle/SpriteBundle.csproj` — same version as `Goose/Goose.csproj:27`.

**Step 5: Seed `sheets.json`**

Build both datasets' databases, then derive. The Illutia dataset id is in
`Goose/GooseSettings.json` under the commented `// Illutia Config` block
(`1Ig7u4XHc1Vjk4Y1502bwHEVEDba3JTCUcrKwrcOPWyQ`).

```bash
dotnet run --project tools/SpriteBundle -- derive-sheets Goose/bin/Debug/AsperetaGoose.db
```

Union the output of both datasets by hand into `tools/SpriteBundle/sheets.json`. The expected
result is 125 sheets — 113 from Illutia, 22 from Aspereta, overlapping on 10.

```json
{
  "atlasWidth": 2048,
  "iconSheets": [
    9, 104, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419,
    420, 421, 422, 423, 424, 425, 426, 429, 430, 2174, 2184, 2213, 2215, 2268,
    2269, 2270, 2271, 2272, 2273, 2274, 2275, 2276, 2277, 2278, 2279, 2280,
    2281, 2282, 2324, 2355, 2356, 2357, 2358, 2359, 2360, 2361, 2362, 2363,
    2364, 2365, 2366, 2367, 2368, 2369, 2386, 2437, 2506, 2691, 2820, 2834,
    2835, 2876, 2900, 2903, 2904, 2947, 2958, 2980, 2981, 3081, 3125, 3146,
    3432, 3468, 3523, 3524, 3526, 3632, 3646, 3658, 3664, 3671, 3712, 3768,
    3770, 4120, 4128, 4235, 4238, 4340, 4358, 4386, 4401, 4413, 4470, 4515,
    4605, 4697, 4698, 4837, 4849, 4984, 5178, 5323, 5368, 5672, 5809, 6513,
    20107, 20397, 20398, 20399, 20400, 20401, 20402, 20403, 20404, 20406,
    20408, 20444
  ],
  "partCategories": [
    "Bodies", "Chest", "Effects", "Eyes", "Feet", "Hair", "Hands", "Helms", "Legs"
  ],
  "partClips": [
    "idle-no-equip-down", "idle-down", "idle-equip-down", "mounted-idle-down"
  ],
  "effectsCategory": "Effects"
}
```

Copy it to the test output via `tools/SpriteBundle/SpriteBundle.csproj`:

```xml
  <ItemGroup>
    <None Update="sheets.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

and in `tools/Tools.Tests/Tools.Tests.csproj`:

```xml
  <ItemGroup>
    <None Include="../SpriteBundle/sheets.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
```

**Step 6: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~BundleConfigTests"`
Expected: PASS, 5 tests.

**Step 7: Commit**

```bash
git add tools/SpriteBundle tools/Tools.Tests
git commit -m "feat: add sprite bundle config with derived icon sheet list"
```

---

## Task 8: Bundle emitters

Three bundles, three key schemes. The editor looks sprites up by these keys, so they are part
of the contract with Part 3.

| Bundle | Key format | Example |
|---|---|---|
| `icons` | `<sheet>:<graphic>` | `20107:810003` |
| `parts` | `<category>:<id>:<clip>` | `Bodies:1:idle-down` |
| `effects` | `<id>:<frameIndex>` | `1080:0` |

**Files:**
- Create: `tools/SpriteBundle/BundleWriter.cs`
- Create: `tools/SpriteBundle/Bundles.cs`
- Test: `tools/Tools.Tests/BundleWriterTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose.Tools.SpriteBundle;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tools.Tests;

public class BundleWriterTests
{
    [Fact]
    public void Renders_a_self_contained_html_fragment()
    {
        using var img = new Image<Rgba32>(4, 4);
        var rects = new Dictionary<string, SpriteRect> { ["a:1"] = new(0, 0, 2, 2) };

        var html = BundleWriter.Render("icons", img, rects);

        Assert.Contains("GOOSE_SPRITES", html);
        Assert.Contains("\"icons\"", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("\"a:1\": [0,0,2,2]", html);
        Assert.StartsWith("<script>", html);
        Assert.EndsWith("</script>\n", html);
    }

    [Fact]
    public void Base64_payload_decodes_to_a_valid_png()
    {
        using var img = new Image<Rgba32>(8, 8);
        img[3, 3] = new Rgba32(10, 20, 30, 255);

        var html = BundleWriter.Render("icons", img, new Dictionary<string, SpriteRect>());

        var marker = "data:image/png;base64,";
        var start = html.IndexOf(marker) + marker.Length;
        var end = html.IndexOf('"', start);
        var bytes = Convert.FromBase64String(html[start..end]);

        using var decoded = Image.Load<Rgba32>(bytes);
        Assert.Equal(8, decoded.Width);
        Assert.Equal(new Rgba32(10, 20, 30, 255), decoded[3, 3]);
    }

    [Fact]
    public void Does_not_overwrite_other_bundles_on_the_global()
    {
        using var img = new Image<Rgba32>(2, 2);

        var html = BundleWriter.Render("parts", img, new Dictionary<string, SpriteRect>());

        // Must initialise the global defensively so bundles load in any order.
        Assert.Contains("var GOOSE_SPRITES = GOOSE_SPRITES || {};", html);
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~BundleWriterTests"`
Expected: FAIL — `BundleWriter` does not exist.

**Step 3: Write the writer**

```csharp
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Goose.Tools.SpriteBundle;

/// <summary>Renders one bundle as an HTML fragment for the Apps Script project: the atlas as a
/// base64 data URI plus its rect index. Base64 costs 33% over the raw PNG; HtmlService gzips
/// on the way out, so the wire cost is close to the PNG size.</summary>
public static class BundleWriter
{
    public static string Render(string name, Image<Rgba32> atlas,
                               IReadOnlyDictionary<string, SpriteRect> rects)
    {
        using var ms = new MemoryStream();
        atlas.SaveAsPng(ms);
        var b64 = Convert.ToBase64String(ms.ToArray());

        var sb = new StringBuilder();
        sb.Append("<script>\n");
        sb.Append("// Generated by tools/SpriteBundle. Do not edit by hand.\n");
        sb.Append("var GOOSE_SPRITES = GOOSE_SPRITES || {};\n");
        sb.Append($"GOOSE_SPRITES[\"{name}\"] = {{\n");
        sb.Append($"  \"width\": {atlas.Width},\n");
        sb.Append($"  \"height\": {atlas.Height},\n");
        sb.Append($"  \"png\": \"data:image/png;base64,{b64}\",\n");
        sb.Append("  \"rects\": {\n");

        var i = 0;
        foreach (var (key, r) in rects.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            var comma = ++i < rects.Count ? "," : "";
            sb.Append($"    \"{key}\": [{r.X},{r.Y},{r.W},{r.H}]{comma}\n");
        }

        sb.Append("  }\n");
        sb.Append("};\n");
        sb.Append("</script>\n");
        return sb.ToString();
    }
}
```

**Step 4: Write the three bundle definitions**

```csharp
namespace Goose.Tools.SpriteBundle;

/// <summary>Collects the SpriteRefs for each bundle. Counts are reported at run time rather
/// than asserted — the exact totals depend on the client's current art.</summary>
public static class Bundles
{
    public static List<SpriteRef> Icons(Manifest manifest, BundleConfig config)
    {
        var refs = new List<SpriteRef>();

        foreach (var sheet in config.IconSheets)
        {
            if (!manifest.Sheets.TryGetValue(sheet, out var graphics)) continue;

            foreach (var graphic in graphics.Keys)
                refs.Add(new SpriteRef($"{sheet}:{graphic}", sheet, graphic));
        }

        return refs;
    }

    /// <summary>Character parts come from .tres clips rather than the manifest, so they are
    /// returned as explicit frames with their own keys.</summary>
    public static List<(string Key, TresFrame Frame)> Parts(string assetRoot, BundleConfig config)
    {
        var frames = new List<(string, TresFrame)>();

        foreach (var category in config.PartCategories)
        {
            var dir = Path.Combine(assetRoot, category);
            if (!Directory.Exists(dir)) continue;

            foreach (var partDir in Directory.EnumerateDirectories(dir))
            {
                var tres = Path.Combine(partDir, "animations.tres");
                if (!File.Exists(tres)) continue;

                var id = Path.GetFileName(partDir);
                var parsed = TresParser.Parse(tres);

                foreach (var clip in config.PartClips)
                    if (parsed.TryGetFirstFrame(clip, out var frame))
                        frames.Add(($"{category}:{id}:{clip}", frame));
            }
        }

        return frames;
    }

    /// <summary>Every frame of every effect animation. Each Effects/&lt;id&gt;/animations.tres
    /// holds a single clip named after the id, with no directional variants.</summary>
    public static List<(string Key, TresFrame Frame)> Effects(string assetRoot, BundleConfig config)
    {
        var frames = new List<(string, TresFrame)>();
        var dir = Path.Combine(assetRoot, config.EffectsCategory);
        if (!Directory.Exists(dir)) return frames;

        foreach (var effectDir in Directory.EnumerateDirectories(dir))
        {
            var tres = Path.Combine(effectDir, "animations.tres");
            if (!File.Exists(tres)) continue;

            var id = Path.GetFileName(effectDir);
            var parsed = TresParser.Parse(tres);

            foreach (var (_, clipFrames) in parsed.Clips)
                for (var i = 0; i < clipFrames.Count; i++)
                    frames.Add(($"{id}:{i}", clipFrames[i]));
        }

        return frames;
    }
}
```

`AtlasBuilder.Build` takes `SpriteRef`s resolved through the manifest, but parts and effects
already carry explicit rects. Add an overload to `tools/SpriteBundle/AtlasBuilder.cs`:

```csharp
    /// <summary>Packs frames whose rects are already known (from .tres) rather than looked up
    /// in the manifest.</summary>
    public static BuiltAtlas BuildFromFrames(Manifest manifest,
        IReadOnlyList<(string Key, TresFrame Frame)> frames, int width)
    {
        var resolved = new List<(string Key, TresFrame Frame)>(frames.Count);
        var skipped = new List<string>();

        foreach (var f in frames)
        {
            if (File.Exists(manifest.SheetPath(f.Frame.Sheet))) resolved.Add(f);
            else skipped.Add(f.Key);
        }

        var packed = ShelfPacker.Pack(
            resolved.Select(r => (r.Frame.W, r.Frame.H)).ToList(), width);

        var atlas = new Image<Rgba32>(packed.Width, Math.Max(packed.Height, 1));
        var rects = new Dictionary<string, SpriteRect>(resolved.Count);

        foreach (var group in packed.Placements.GroupBy(p => resolved[p.Index].Frame.Sheet))
        {
            using var sheet = Image.Load<Rgba32>(manifest.SheetPath(group.Key));

            foreach (var p in group)
            {
                var (key, src) = resolved[p.Index];

                for (int y = 0; y < src.H; y++)
                for (int x = 0; x < src.W; x++)
                    atlas[p.X + x, p.Y + y] = sheet[src.X + x, src.Y + y];

                rects[key] = new SpriteRect(p.X, p.Y, src.W, src.H);
            }
        }

        return new BuiltAtlas { Image = atlas, Rects = rects, Skipped = skipped };
    }
```

**Step 5: Run the tests**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~BundleWriterTests"`
Expected: PASS, 3 tests.

**Step 6: Commit**

```bash
git add tools/SpriteBundle tools/Tools.Tests/BundleWriterTests.cs
git commit -m "feat: define icon, part and effect sprite bundles"
```

---

## Task 9: CLI and end-to-end run

**Files:**
- Modify: `tools/SpriteBundle/Program.cs`
- Test: `tools/Tools.Tests/GoldenRectTests.cs`

**Step 1: Write the golden test**

This is the correctness gate the design calls for: rect indices must agree with the source
manifest for known pairs.

```csharp
using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class GoldenRectTests
{
    [SkippableFact]
    public void Icon_bundle_rect_dimensions_match_the_manifest()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var manifest = Manifest.Load(ManifestTests.AssetRoot!);
        var config = BundleConfig.Load(Path.Combine(AppContext.BaseDirectory, "sheets.json"));

        var refs = Bundles.Icons(manifest, config);
        using var built = AtlasBuilder.Build(manifest, refs, config.AtlasWidth);

        // Every packed sprite must keep the manifest's width and height.
        foreach (var (key, packed) in built.Rects)
        {
            var parts = key.Split(':');
            Assert.True(manifest.TryGetRect(int.Parse(parts[0]), int.Parse(parts[1]), out var src));
            Assert.Equal(src.W, packed.W);
            Assert.Equal(src.H, packed.H);
        }
    }

    [SkippableFact]
    public void Known_icons_are_present()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var manifest = Manifest.Load(ManifestTests.AssetRoot!);
        var config = BundleConfig.Load(Path.Combine(AppContext.BaseDirectory, "sheets.json"));

        using var built = AtlasBuilder.Build(manifest, Bundles.Icons(manifest, config),
                                             config.AtlasWidth);

        // 810003 is "fire" per aspereta-info/spellbookids.txt (110003 + 700000 offset).
        Assert.Contains("20107:810003", built.Rects.Keys);
    }

    [SkippableFact]
    public void Part_bundle_covers_every_category()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var config = BundleConfig.Load(Path.Combine(AppContext.BaseDirectory, "sheets.json"));
        var frames = Bundles.Parts(ManifestTests.AssetRoot!, config);

        foreach (var category in config.PartCategories)
            Assert.Contains(frames, f => f.Key.StartsWith(category + ":", StringComparison.Ordinal));
    }

    [SkippableFact]
    public void Every_part_key_is_unique()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var config = BundleConfig.Load(Path.Combine(AppContext.BaseDirectory, "sheets.json"));
        var frames = Bundles.Parts(ManifestTests.AssetRoot!, config);

        Assert.Equal(frames.Count, frames.Select(f => f.Key).Distinct().Count());
    }
}
```

**Step 2: Run it to verify it fails**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~GoldenRectTests"`
Expected: FAIL — `Bundles` members not yet reachable, or `sheets.json` not copied.

**Step 3: Write the CLI**

```csharp
using System.Diagnostics;

namespace Goose.Tools.SpriteBundle;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "derive-sheets")
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("usage: SpriteBundle derive-sheets <path/to/game.db>");
                return 1;
            }

            var sheets = SheetDeriver.Derive(args[1]);
            Console.WriteLine($"{sheets.Count} sheets referenced:");
            Console.WriteLine(string.Join(", ", sheets));
            return 0;
        }

        if (args.Length != 2)
        {
            Console.Error.WriteLine("usage: SpriteBundle <client-assets-dir> <output-dir>");
            Console.Error.WriteLine("       SpriteBundle derive-sheets <path/to/game.db>");
            return 1;
        }

        var assetRoot = args[0];
        var outDir = args[1];
        Directory.CreateDirectory(outDir);

        var configPath = Path.Combine(AppContext.BaseDirectory, "sheets.json");
        var config = BundleConfig.Load(configPath);
        var manifest = Manifest.Load(assetRoot);

        var sw = Stopwatch.StartNew();
        long total = 0;

        total += Emit("icons", outDir,
            () => AtlasBuilder.Build(manifest, Bundles.Icons(manifest, config), config.AtlasWidth));

        total += Emit("parts", outDir,
            () => AtlasBuilder.BuildFromFrames(manifest,
                      Bundles.Parts(assetRoot, config), config.AtlasWidth));

        total += Emit("effects", outDir,
            () => AtlasBuilder.BuildFromFrames(manifest,
                      Bundles.Effects(assetRoot, config), config.AtlasWidth));

        Console.WriteLine($"Total {total / 1024.0 / 1024.0:F2} MB of HTML in {sw.Elapsed.TotalSeconds:F1}s");
        return 0;
    }

    private static long Emit(string name, string outDir, Func<BuiltAtlas> build)
    {
        using var built = build();
        var html = BundleWriter.Render(name, built.Image, built.Rects);
        var path = Path.Combine(outDir, $"sprites-{name}.html");
        File.WriteAllText(path, html);

        var used = built.Rects.Values.Sum(r => (long)r.W * r.H);
        var area = (long)built.Image.Width * built.Image.Height;

        Console.WriteLine(
            $"{name,-8} {built.Rects.Count,6} sprites  " +
            $"{built.Image.Width}x{built.Image.Height}  " +
            $"{used * 100.0 / area:F0}% efficient  " +
            $"{new FileInfo(path).Length / 1024.0 / 1024.0:F2} MB html" +
            (built.Skipped.Count > 0 ? $"  ({built.Skipped.Count} skipped)" : ""));

        return new FileInfo(path).Length;
    }
}
```

Note the `Skipped` count is always printed when non-zero — silent truncation would make a
missing sheet look like a complete bundle.

**Step 4: Run the tests and the tool**

Run: `dotnet test Goose.sln --filter "FullyQualifiedName~GoldenRectTests"`
Expected: PASS, 4 tests.

```bash
dotnet run --project tools/SpriteBundle -- \
  ../Goose2ClientGodot/Assets/Sprites tools/DataEditor
```

Expected output shape (exact counts depend on current client art):

```
icons       4846 sprites  2048x3160  98% efficient  1.64 MB html
parts       ~3200 sprites  2048x....  9x% efficient  ~1.5 MB html
effects     ~2400 sprites  2048x....  9x% efficient  ~0.8 MB html
Total ~3.9 MB of HTML in ~30s
```

Sanity-check the outputs:

```bash
ls -lh tools/DataEditor/
grep -o '"width": [0-9]*' tools/DataEditor/sprites-icons.html | head -1
python3 -c "
import re,base64
h=open('tools/DataEditor/sprites-icons.html').read()
b=re.search(r'base64,([^\"]+)',h).group(1)
print('png bytes', len(base64.b64decode(b)))"
```

Expected: three `sprites-*.html` files, `"width": 2048`, and a PNG payload around 1.2 MB.

Confirm the total stays under the Apps Script project ceiling (~10 MB, community-measured):

```bash
du -ch tools/DataEditor/*.html tools/DataEditor/schema.js | tail -1
```

Expected: comfortably under 10 MB — around 4 MB.

**Step 5: Commit**

The generated bundles are committed so Part 3 can be developed and pasted without a client
checkout. They are regenerated only when the client's art changes.

```bash
git add tools/SpriteBundle/Program.cs tools/Tools.Tests/GoldenRectTests.cs \
        tools/DataEditor/sprites-icons.html tools/DataEditor/sprites-parts.html \
        tools/DataEditor/sprites-effects.html
git commit -m "feat: generate sprite bundles for the data editor"
```

---

## Task 10: Document regeneration

**Files:**
- Create: `tools/README.md`

**Step 1: Write it**

```markdown
# Generators

Both tools produce inputs for the Apps Script data editor in `tools/DataEditor/`.

## SchemaGen

Emits `schema.js` from the column descriptors in `CsvToSql.Core`. Run after adding or changing
any column:

    dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js

## SpriteBundle

Emits three sprite atlases as inlined HTML. Needs the client repo checked out alongside this
one. Run when the client's art changes:

    dotnet run --project tools/SpriteBundle -- \
      ../Goose2ClientGodot/Assets/Sprites tools/DataEditor

### Adding graphics the data has not referenced yet

`tools/SpriteBundle/sheets.json` lists which sheets go into the icon bundle. It was seeded from
the sheets the two datasets reference, so a sheet nobody has used will not appear and its
graphics cannot be picked in the editor. To add one, put its number in `iconSheets` and
regenerate.

To re-derive the list from a built database:

    dotnet run --project tools/SpriteBundle -- derive-sheets Goose/bin/Debug/AsperetaGoose.db

Run it once per dataset and union the results — the checked-in list covers both Illutia and
Aspereta.

## Deploying to Apps Script

The editor is a container-bound script, so each spreadsheet has its own script id and needs its
own deployment. Paste `schema.js` and the three `sprites-*.html` files into the Apps Script
editor, or push with `clasp`. The sprite bundles change rarely; `schema.js` changes whenever a
column does.
```

**Step 2: Commit**

```bash
git add tools/README.md
git commit -m "docs: document generator usage and regeneration"
```

---

## Definition of done

- `dotnet test Goose.sln` passes; tests that need the client checkout skip cleanly without it.
- `tools/DataEditor/schema.js` exists, assigns `GOOSE_SCHEMA`, and its body parses as JSON with
  21 sheets.
- `tools/DataEditor/sprites-{icons,parts,effects}.html` exist, each assigning into
  `GOOSE_SPRITES` with a decodable PNG and a rect index.
- Every packed rect keeps the source sprite's width and height (`GoldenRectTests`).
- Packing efficiency reported above 95% for all three bundles.
- Combined size of `schema.js` plus the three bundles is well under 10 MB.
- `tools/SpriteBundle/sheets.json` has 125 icon sheets covering both datasets.

## Contract for Part 3

The editor consumes exactly two globals:

- `GOOSE_SCHEMA.sheets[]` — `{sheet, table, columns[], composites[], indexes[]}`, where each
  column is `{name, kind, sql, default?, required, pk, ref?, enumNames?}`.
- `GOOSE_SPRITES[name]` — `{width, height, png, rects}` for `name` in `icons`, `parts`,
  `effects`, with these key formats:
  - `icons`: `<sheet>:<graphic>` — e.g. `20107:810003`
  - `parts`: `<category>:<id>:<clip>` — e.g. `Bodies:1:idle-down`
  - `effects`: `<id>:<frameIndex>` — e.g. `1080:0`

A rect is `[x, y, w, h]` in atlas pixel coordinates. Rendering a sprite is a canvas
`drawImage` with those source coordinates; tinting applies `mix(rgb, tint.rgb, tint.a)` with
alpha preserved, matching `Scripts/UI/Icon.cs` in the client.
