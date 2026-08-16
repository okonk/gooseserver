# Shared `.csx` constants via `#load` — Design

Date: 2026-08-16

## Summary

The dimensions scripts duplicate compile-time constants across four
independently-compiled `.csx` files because `Script.cs` compiles each script
from a raw string, which disables Roslyn's built-in `#load` directive. This
design removes the duplication with a one-line engine change that enables
`#load` (generic, benefits every script) and a declarations-only constants
file that the four dimension scripts load.

The duplication was flagged during the `dimensions-economy` branch review
(2026-08-15) as a maintenance hazard, not a bug: as of branch tip `8610c28`
all copies agree. The hazard is that a future edit to one copy fails
silently — e.g. changing `Offset` in `Dimensions.csx` but not in
`DimensionItem.csx` would not error at load; it would roll suffixes from the
wrong id space or make `/resetitem` refuse valid items with "Only items from
a higher plane can be reset."

This is option A of the three options considered in that review. Options B
(a constants class in the core assembly) and C (a load-time drift check) are
covered under [Alternatives](#alternatives-considered) if this design is
revisited.

## Root cause

`Goose/Scripting/Script.cs`, `Script<T>.LoadScript()`:

```csharp
string scriptContents = File.ReadAllText(this.FilePath);
// ...
var scriptOptions = ScriptOptions.Default
    .WithReferences(Assembly.GetExecutingAssembly(), /* ... */)
    .WithImports("System", /* ... */);

var script = CSharpScript.Create(scriptContents, scriptOptions);
```

The script is handed to the compiler as text. The compilation therefore has
no file path, and the built-in `#load` directive — which resolves its
argument relative to the containing script's directory — has nothing to
anchor against. Any `#load` in a shipped script fails at script-compile time
inside `script.Compile()`.

One API detail that shapes the fix: in
`Microsoft.CodeAnalysis.CSharp.Scripting` **5.6.0** (the version this repo
references), `ScriptSource` is an *internal* type, so the obvious
`CSharpScript.Create(ScriptSource.FromFile(path), options)` form is not
reachable from the public API. The public hook is
`ScriptOptions.WithFilePath(string)` (verified against the package's
exported surface on 2026-08-16).

## Verified mechanism

On 2026-08-16 a spike test was written into the `dimensions-economy`
worktree, run against the repo's exact package version, and passed (it was
deleted; the permanent regression test is [below](#test-impact)). Two temp
files:

```csharp
// SharedConstants.csx  (declarations only — no top-level return)
public static class SharedConstants
{
    public const int Offset = 12345;
}
```

```csharp
// Consumer.csx
#load "../SharedConstants.csx"

public class Consumer
{
    public static int Off { get { return SharedConstants.Offset; } }
}

return typeof(Consumer);
```

compiled with:

```csharp
var options = ScriptOptions.Default
    .WithReferences(typeof(Player).Assembly, /* ... */)
    .WithImports("System", /* ... */)
    .WithFilePath(consumerPath);          // <-- the one added line

var script = CSharpScript.Create(File.ReadAllText(consumerPath), options);
script.Compile();                          // zero diagnostics
// running the script returns typeof(Consumer); Off == 12345
```

So the mechanism is settled: set `WithFilePath` and `#load` works, relative
to the script's own directory. The spike used the parent-directory form
(`../SharedConstants.csx`), which is what the item/map/quest scripts need;
the sibling form (`Dimensions.csx` loading its own directory) is untested by
the spike and gets confirmed by the regression test.

## Scope

**In scope**

- `Script.LoadScript()`: add `.WithFilePath(this.FilePath)` to the
  `ScriptOptions`
- New file `Goose/Data/Illutia/Scripts/Global/DimensionConstants.csx`
  (declarations only)
- `#load` + const cleanup in `Dimensions.csx`, `DimensionItem.csx`,
  `DimensionMap.csx`, `Rebirth.csx`
- Test-fixture plumbing (csproj copy + `ShippedScripts` entry) and a
  regression test pinning `#load` support itself

**Out of scope**

- Any other use of `#load` (`#embed`, `#r` of additional assemblies)
- The Aspereta dataset (its `Scripts/` tree has no dimension scripts and no
  `#load`; the engine change is a no-op for it)
- Deduplicating values that are *not* cross-file constants — e.g. the warden
  and keeper appearance blocks in `Dimensions.csx` are single-file
  duplication of a *pattern*, not of values

## Design

### Engine change

`Goose/Scripting/Script.cs`, in `LoadScript()`:

```csharp
var scriptOptions = ScriptOptions.Default
    .WithReferences(
        Assembly.GetExecutingAssembly(),
        typeof(System.Text.Json.JsonSerializer).Assembly)
    .WithImports(
        "System", "System.Collections.Generic", "System.Linq",
        "System.Text.Json",
        "Goose", "Goose.Events", "Goose.Quests", "Goose.Scripting")
    .WithFilePath(this.FilePath);          // anchors #load/#embed/#r resolution
```

Effects:

- `#load` paths in any script now resolve relative to that script's
  directory. `this.FilePath` is already absolute
  (`ScriptHandler.GetScript` builds it from `DataPathAbsolute`), so no
  path-join work is needed.
- Compiler diagnostics now carry the real script file name instead of an
  anonymous source — strictly better for the log lines that already surface
  script compile errors.
- `ReloadScripts()` re-runs `LoadScript`, so a `#load`ed file is re-read on
  reload, same as the host script. Consistent.
- No shipped script uses `#load` today, so existing scripts compile
  byte-identically; the only observable change is diagnostic file names.

### The shared file

`Goose/Data/Illutia/Scripts/Global/DimensionConstants.csx`:

```csharp
/// <summary>Compile-time constants shared by every dimension script. Loaded
/// with #load, which merges this file's declarations into each host
/// script's compilation — one definition, no cross-file drift.
///
/// Rules: declarations only (a #loaded file is not an entry script, so no
/// top-level return), and const only. Each host script's assembly gets its
/// own copy of this class, so a mutable static here would not actually be
/// shared across scripts — it would just be a trap.</summary>
public static class DimensionConstants
{
    public const int Offset = 100000;
    public const int SurnameIdBase = 900000;
    public const int TitleIdBase = 900100;
    public const string MaxDimensionProperty = "dimension.max";
    public const string SpiritCurrencyId = "spirit";
    public const int RebirthDestinationClassId = 1;
    public const int RebirthDestinationLevel = 1;
}
```

Two notes on the shape:

- **Why not `#load "Dimensions.csx"` from the others?** `Dimensions.csx`
  ends in `return typeof(Dimensions);` — a top-level statement. A `#load`ed
  file is merged into the host compilation, where a second top-level return
  is invalid. The shared file has to be a separate declarations-only file.
- **Why `RebirthDestinationClassId/Level`?** Today `Rebirth.csx` hardcodes
  `ChangeClass(1, 1, world, 0d)` with a "keep the two in step" comment
  pointing at the same values in `Dimensions.csx`. Same hazard, same fix;
  moving them in means `Rebirth.csx` calls
  `player.ChangeClass(DimensionConstants.RebirthDestinationClassId,
  DimensionConstants.RebirthDestinationLevel, world, 0d)`.

### The consumers

Current duplication, as of branch tip `8610c28`:

| File | Duplicated tokens |
|---|---|
| `Scripts/Global/Dimensions.csx` | origin of all of the above |
| `Scripts/Item/DimensionItem.csx` | `Offset`, `SurnameIdBase`, `TitleIdBase`, `MaxDimensionProperty` |
| `Scripts/Map/DimensionMap.csx` | `MaxDimensionProperty`, `Offset` |
| `Scripts/Quest/Rebirth.csx` | `SpiritCurrencyId`, destination class/level (inline literals) |

Each gets the directive at the top of the file, above its `using` statements
(the spike's consumer had no `using`s; the shipped scripts do, so the
placement is confirmed by the regression test compiling the real files),
and drops its private copies:

- `Dimensions.csx`: `#load "DimensionConstants.csx"` (sibling, same dir)
- `DimensionItem.csx`: `#load "../Global/DimensionConstants.csx"`
- `DimensionMap.csx`: `#load "../Global/DimensionConstants.csx"`
- `Rebirth.csx`: `#load "../Global/DimensionConstants.csx"`

In `Dimensions.csx` the existing public consts stay as **const aliases** so
the ~80 in-file usages (`Offset` alone appears 67 times) don't churn:

```csharp
public const int Offset = DimensionConstants.Offset;
```

A const initializer referencing another const is legal C#. The other three
files delete their private consts and reference `DimensionConstants.X`
directly (few usages each; the old "must match Dimensions.csx" comments go
with the consts).

The `#load` strings are the one new fragile thing this design introduces:
if the shared file is ever moved, the load fails at script-compile time
inside world load — loud, with the script's file name now in the diagnostic
(first effect above), not silent. Accepted.

### Where the work lands

The four consumer files exist only on the `dimensions-economy` branch
(unmerged as of this writing). The whole change — engine line, shared file,
consumer edits, fixture plumbing — belongs on that branch; master receives
it via the merge. If the branch is merged first, the same diff applies on
master.

## Test impact

1. **Regression test for the mechanism itself** (the permanent form of the
   spike): compile a consumer script that `#load`s a shared file *through the
   real `Script<T>`/`ScriptHandler` path* — not a hand-rolled
   `CSharpScript.Create` — and assert the shared const is visible at run
   time. This pins `WithFilePath` inside `LoadScript()`; a refactor that
   drops the line fails it.
2. **Fixture plumbing — the documented "add to BOTH lists" trap**
   (`GlobalScriptFixture.cs:11`):
   - `Goose.Tests.csproj`: `<None Include>` for the new file with
     `Link="DimensionScripts/DimensionConstants.csx"` alongside the other
     dimension scripts.
   - `GlobalScriptFixture.ShippedScripts`:
     `("DimensionConstants.csx", "Scripts/Global/DimensionConstants.csx")`.
   The fixture's temp data dir mirrors the real `Scripts/{Global,Map,Quest,Spell,Item}`
   layout, so the same relative `#load` paths work in tests and in
   production — but only if both lists are updated together.
3. **Existing dimension suites are the end-to-end check**: `CompileShipped`
   compiles the real shipped `Dimensions.csx`, which pulls in the `#load`ed
   file; a wrong path fails inside `OnLoaded` at compile time, not as a
   drift at run time.

## Alternatives considered

**B — constants class in the core assembly.** A
`public static class DimensionConstants` in `Goose/`, referenced by the
scripts the way they already reference `Goose.*`. Simplest and most robust:
typed, compile-time, no engine change, no string paths, no fixture changes.
Rejected for now because it puts a dimension-named type into the server
assembly and moves structural values out of the `.csx` config block,
against the documented self-containment decision ("nothing dimension-specific
enters the server" — currency system design, 2026-08-11; the dimensions
economy design carries the same rule). The rule's *rationale* (no lingering
objects, a genuine `Enabled = false` off switch) is not actually violated by
an inert const class — if that rule is relaxed, B becomes the obvious
choice and this design is retired.

**C — no deduplication; a load-time drift check.** In
`Dimensions.OnLoaded`, where the item/map/rebirth script instances are
already in hand, assert each consumer's duplicated consts equal
`Dimensions.*` (make the consts `public` on the three scripts, or read them
via reflection). ~30 lines, no engine change, shippable independently and
immediately. Converts the silent-mismatch hazard into a loud load failure
with an actionable message. If the engine change is deferred, C is the
interim; it can also ship *alongside* A as belt-and-braces, at which point
it degrades to a test-only assertion.

## Open questions

1. **Name.** `DimensionConstants.csx` / `DimensionConstants` vs a less
   feature-flavoured name (`DimensionShared`, ...). The file is
   dimension-specific content in a dataset-specific directory, so a
   dimension name is honest; decide at implementation.
2. **Alias style in `Dimensions.csx`.** Const aliases (minimal diff, chosen
   above) vs rewriting the ~80 usages to `DimensionConstants.X` (no indirection,
   bigger diff). Leaning aliases.
3. **`#r` via search paths.** Not needed — the scripts reference only the
   executing assembly plus System.Text.Json. If a future script needs an
   extra reference, `WithReferences(string[])` or a metadata resolver is the
   generic extension; deliberately not built now.
