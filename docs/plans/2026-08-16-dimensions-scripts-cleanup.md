# Dimensions Scripts Cleanup Implementation Plan

**Goal:** De-duplicate the eight dimension `.csx` scripts, split the 1802-line
`Dimensions.csx`, and consolidate the whole feature into one folder
(`Scripts/Global/Dimensions/`), using Roslyn `#load` (enabled by a one-line engine
change), with zero behavior change.

**Architecture:** One line in `Script<T>.LoadScript()` —
`.WithFilePath(this.FilePath)` — anchors Roslyn's `#load`, so any script can pull in
declarations-only files. All dimension scripts move into the new
`Scripts/Global/Dimensions/` folder (user decision: "everything in 1 place"): the seven
other entry scripts, two shared files — `DimensionConstants.csx` (the seven
cross-script constants) and `DimensionHelpers.csx` (the small stateless functions the
scripts each copy: id math, the `dimension.max` read, base-script delegation, tier,
refusal message) — and the `Dimensions.csx` split into `partial class Dimensions` part
files plus two already-separate classes (the five commands, `SpiritCurrency`). The entry
file stays at `Scripts/Global/Dimensions.csx` because
`GameWorld.LoadGlobalScripts` scans that directory **non-recursively**
(`Goose/GameWorld.cs:693`); everything else in the feature may sit in the subfolder.

**Tech Stack:** C# 10, `Microsoft.CodeAnalysis.CSharp.Scripting` 5.6.0, xUnit.

---

## What this session verified (spike, run against the repo's exact package)

A temporary spike test was run through the **real `ScriptHandler`/`Script<T>` path**
(kept in the working tree as `Goose.Tests/LoadSpikeTests.cs`, promoted in Task 1). All
shapes the plan relies on passed:

1. `.WithFilePath(this.FilePath)` makes `#load` work. **Without it: `error CS8098:
   Cannot use #load after first token in file`** — so `#load` lines must be the
   **first tokens** of a script, *above* the `using` statements.
2. Sibling form `#load "X.csx"`, subdirectory form `#load "Dimensions/X.csx"`, and
   parent form `#load "../Global/Dimensions/X.csx"` all resolve relative to the
   script's own directory.
3. Multiple `#load` directives in one host compile.
4. One loaded file referencing declarations from **another** loaded file (part →
   constants) compiles — this is what lets the part files use `DimensionConstants`
   without carrying their own `#load` lines.
5. `partial class` assembled across a loaded file and the host, including a host that
   also has a top-level `return typeof(...)`.
6. `using` directives inside a loaded file are fine.
7. **Per-host independence**: each host script's compilation gets its **own copy** of
   the loaded file's mutable statics (host A set one to 1; host B still read 42).
   Shared files must therefore stay declarations-only, **const + stateless statics** —
   a mutable static would be a trap, not a share.

Also verified while writing this plan (not in the committed design doc):

- `GameWorld.LoadGlobalScripts` uses `Directory.EnumerateFiles(.../Scripts/Global,
  "*.csx")` — **non-recursive** (`Goose/GameWorld.cs:693`). The entry script must sit
  directly in `Scripts/Global/`; anything in the `Dimensions/` subfolder is never
  auto-run, and must never be made an entry (a declarations-only file would throw in
  `Activator.CreateInstance(null)` at `Goose/Scripting/Script.cs:49`). This is the only
  engine constraint on the layout: it pins the entry's path and nothing else. The moved
  entry scripts are only ever loaded through explicit `GetScript<T>("Scripts/…")` path
  strings, which follow the files.
- Test-class parallelization trap, hit live in this session: test classes that swap
  the static `GameWorld.Settings` must carry
  `[Collection(GameWorldSettingsCollection.Name)]`
  (`Goose.Tests/Collections/GameWorldSettingsCollection.cs:4`,
  `DisableParallelization = true`). The spike passed alone and **failed in the full
  run** until the attribute was added — it read another fixture's temp dir.
- Full suite (409 tests at the time) is green with the one-line engine change applied: the
  "existing scripts compile byte-identically" claim from the design doc holds; the
  only observable change is compile diagnostics now carrying real file names.

**APIs verified**

| API | Citation |
|---|---|
| `ScriptOptions.WithFilePath(string)` | public in `Microsoft.CodeAnalysis.CSharp.Scripting` 5.6.0; spike + staged `Goose/Scripting/Script.cs:38` |
| `Script<T>.LoadScript` (compile + run + `Activator.CreateInstance`) | `Goose/Scripting/Script.cs:28-52` |
| Compile-failure path: `script.Compile()` returns the diagnostics (it does not throw); the `ScriptException` is thrown from `script.RunAsync().Result` | `Goose/Scripting/Script.cs:44-46` |
| `ScriptHandler.GetScript<T>` caches by absolute path; missing file throws `FileNotFoundException` at `Script.cs:29` | `Goose/Scripting/ScriptHandler.cs:14-30` |
| `GameWorld.LoadGlobalScripts` non-recursive scan | `Goose/GameWorld.cs:688-699` |
| `Map.Script` is `Script<IMapScript>` | `Goose/Map.cs:59` |
| `ItemTemplate.Script` is `Script<IItemScript>` | `Goose/ItemTemplate.cs:118` |
| `SpellEffect.Script` is `Script<ISpellEffectScript>` | `Goose/SpellEffect.cs:225` |
| `Player.Properties.GetProperty<T>(string, T)` | used by all current scripts, e.g. `DimensionItem.csx:97` |
| `GlobalScriptFixture.ShippedScripts` / `InstallShippedScripts` | `Goose.Tests/Fixtures/GlobalScriptFixture.cs:20-29, 63-76` |
| csproj script copies (8 entries) | `Goose.Tests/Goose.Tests.csproj:22-37` |
| `GameWorldSettingsCollection` | `Goose.Tests/Collections/GameWorldSettingsCollection.cs:4-6` |

## The committed design doc, and where this plan differs

`docs/plans/2026-08-16-csx-load-shared-constants-design.md` (last commit) covers the
engine line, the shared constants file, and four consumers. This plan keeps all of
that, adds the helpers and the split, and consolidates the whole feature into
`Scripts/Global/Dimensions/` (user decision, 2026-08-16). Because the seven other
entry scripts move into the same folder, their `#load` lines are the short sibling
form (`"DimensionConstants.csx"`) instead of the design doc's
`"../Global/DimensionConstants.csx"`.

## What was copied (the answer to "they might copy things")

Confirmed, ten concrete copies across the eight entry scripts:

| # | Copied thing | Copies of |
|---|---|---|
| 1 | `private const int Offset = 100000;` (+ "must match Dimensions.csx" comment) | `DimensionItem.csx:14`, `DimensionMap.csx:24`, `DimensionTeleport.csx:18`, `DimensionSurname.csx:15` |
| 2 | `private const string MaxDimensionProperty = "dimension.max";` | `DimensionItem.csx:17`, `DimensionMap.csx:20`, `DimensionUnlock.csx:14` |
| 3 | `private const int SurnameIdBase/TitleIdBase` | `DimensionItem.csx:15-16` |
| 4 | `private const string SpiritCurrencyId = "spirit";` | `Rebirth.csx:18` |
| 5 | `ChangeClass(1, 1, …)` hardcoded with a "keep the two in step" comment | `Rebirth.csx:72` (vs `Dimensions.csx` `RebirthDestinationClassId/Level`) |
| 6 | `player.Properties.GetProperty<int>(MaxDimensionProperty, 0)` — the read; `DimensionUnlock.csx` also carries the matching write (`:26`) | `DimensionItem.csx:97,99`, `DimensionMap.csx:41`, `DimensionUnlock.csx:23` (write at `:26`), `Dimensions.csx:1409` |
| 7 | `private int DimensionOf(…) { … / Offset }` | `DimensionItem.csx:19`, `DimensionMap.csx:29-32`, `DimensionTeleport.csx:29-31`; inline in `DimensionSurname.csx:19` |
| 8 | `GetMap/GetTemplate(id % Offset)?.Script?.Object` (the `Inner(…)` delegation) | `DimensionMap.csx:34-37`, `DimensionItem.csx:21-23` |
| 9 | `"The void has rejected you. You have a maximum dimension of " + max + "."` | `DimensionMap.csx:52`, `DimensionMap.csx:91` (with `$7` prefix), `Dimensions.csx:1413` |
| 10 | `Tier(ItemTemplate)` (AttributeSet.java:405-419; surname copy adds a null check) | `Dimensions.csx:1052-1058`, `DimensionSurname.csx:58-66` |

`DimensionRarity.csx` copies nothing (its comment references a convention, not a
value). The warden/keeper appearance blocks are a single-file *pattern* duplication,
out of scope per the design doc.

## Final layout

```
Goose/Data/Illutia/Scripts/
  Global/
    Dimensions.csx                  ← ENTRY, path unchanged (auto-run by GameWorld.cs:693)
                                     ← ~250 lines: config consts (7 as aliases) + TryParseAmount + OnLoaded
    CrystalCritterSpawner.csx
    Dimensions/                     ← NEW folder; never scanned, holds the whole feature
      DimensionConstants.csx        ← 7 consts, #loaded by every other dimension script
      DimensionHelpers.csx          ← stateless helpers, #loaded by 5 of them + Dimensions.csx
      Npcs.csx                      ← partial Dimensions: templates/spawns/allies/warden/rebirth
      Maps.csx                      ← partial Dimensions: CloneMaps/MinExperienceFor/RewireWarps
      Items.csx                     ← partial Dimensions: item pass
      Spells.csx                    ← partial Dimensions: spell pass
      Commands.csx                  ← 5 command event classes (already separate classes)
      SpiritCurrency.csx            ← SpiritCurrency (already a separate class)
      DimensionMap.csx              ← moved from Scripts/Map/
      DimensionItem.csx             ← moved from Scripts/Item/
      DimensionSurname.csx          ← moved from Scripts/Item/
      DimensionRarity.csx           ← moved from Scripts/Item/
      DimensionUnlock.csx           ← moved from Scripts/Quest/
      Rebirth.csx                   ← moved from Scripts/Quest/
      DimensionTeleport.csx         ← moved from Scripts/Spell/
  Map/  Item/  Quest/  Spell/       ← keep their non-dimension scripts
```

`#load` map (every line is a first-line directive, above the `using`s):

| Host | #load lines |
|---|---|
| `Global/Dimensions.csx` | `"Dimensions/DimensionConstants.csx"`, `"Dimensions/DimensionHelpers.csx"`, `"Dimensions/Npcs.csx"`, `"Dimensions/Maps.csx"`, `"Dimensions/Items.csx"`, `"Dimensions/Spells.csx"`, `"Dimensions/Commands.csx"`, `"Dimensions/SpiritCurrency.csx"` |
| `Global/Dimensions/DimensionMap.csx`, `DimensionItem.csx`, `DimensionSurname.csx`, `DimensionUnlock.csx`, `DimensionTeleport.csx` | `"DimensionConstants.csx"`, `"DimensionHelpers.csx"` (siblings) |
| `Global/Dimensions/Rebirth.csx` | `"DimensionConstants.csx"` |
| `Global/Dimensions/DimensionRarity.csx` | — |

No nested `#load` anywhere: every host lists exactly what it needs, so the path
strings stay greppable per file (a loaded file that `#load`s another would hide that
dependency). Part files carry **no** `#load` lines — they compile inside
`Dimensions.csx`'s compilation and see everything the entry loaded (verified, spike
shape 4).

**Task order and why:** the folder move (Task 2) lands **before** any `#load` lines
exist, so it is a zero-content change (pure relocation + path strings), and the
consumers' `#load` lines are written exactly once, in their final sibling form, in
Tasks 3–4.

---

### Task 1: Enable `#load` in the engine + pin it with a regression test

**Status: both changes are in the working tree, not yet staged** —
`Goose/Scripting/Script.cs` is modified (unstaged), `Goose.Tests/LoadSpikeTests.cs`
is untracked (both made and verified while writing this plan). Review, then stage
and commit as-is or with renames.

**Files:**
- Modify: `Goose/Scripting/Script.cs:29-36` — add `.WithFilePath(this.FilePath)`
- Create: `Goose.Tests/ScriptLoadDirectiveTests.cs` — promote the untracked spike
  `Goose.Tests/LoadSpikeTests.cs` (rename the class/file; drop the "DELETED BEFORE
  MERGE" header; keep the `#load`-must-be-first-token fact in a comment)

**The permanent test** (the promoted spike) compiles, **through the real
`ScriptHandler`/`Script<T>` path**:

- a host with three `#load`s (a sibling file and two subdirectory files, the second
  of which is a part file that references the other loaded file's declarations) plus
  a `partial class` half and a top-level `return typeof(…)`;
- a second host in another directory carrying the fourth `#load`, loading the same
  shared file via `../…`.

Assertions: the const is visible at run time (a refactor that drops `WithFilePath`
fails inside `GetScript` — the CS8098 diagnostic surfaces as the `ScriptException`
thrown from `script.RunAsync().Result`, `Script.cs:46`); the part → other-loaded-file call
resolves; the partial half assembles; and **per-host independence** — host A writes a
mutable static in the loaded file, host B must still read the original value. That
last assertion is the adversarial one: it proves the fact the "declarations only,
no shared mutable state" rule rests on — a mutable static in a #loaded file is NOT
shared across hosts (each compilation gets its own copy), so a "shared" mutable
would just be a trap. It documents the rule's rationale; it does not enforce the
rule — nothing in the test stops a future edit from adding a mutable static to a
shared file (enforcement would need a structural test over the shared files' text).

**Required:** `[Collection(GameWorldSettingsCollection.Name)]` on the test class
(`Goose.Tests/Collections/GameWorldSettingsCollection.cs:4`) — the class swaps
`GameWorld.Settings`, and without the attribute it races every other settings-swapping
test class in the full run (verified failure mode: reads another fixture's temp dir).

**Step 1:** Review the working-tree diff:

```bash
git diff Goose/Scripting/Script.cs
git status --short
```

**Step 2:** Rename the spike, adjust its header/comment, keep the body:

```bash
mv Goose.Tests/LoadSpikeTests.cs Goose.Tests/ScriptLoadDirectiveTests.cs
```

(plain `mv`, not `git mv` — the spike is untracked, so `git mv` would fail with
"not under version control"; Step 4's `git add` stages the renamed file as new.)

and rename `LoadSpikeTests` → `ScriptLoadDirectiveTests` inside.

**Step 3:** Run the focused test, then the full suite.

```bash
dotnet test Goose.Tests --filter "FullyQualifiedName~ScriptLoadDirectiveTests"
dotnet test Goose.Tests
```

Expected: both green — full suite passes with no test-count reduction (the spike
already compiles into the suite as an untracked file, so the rename adds nothing).

**Step 4:** Commit.

```bash
git add Goose/Scripting/Script.cs Goose.Tests/ScriptLoadDirectiveTests.cs
git commit -m "scripting: anchor #load resolution with ScriptOptions.WithFilePath"
```

---

### Task 2: Move the seven entry scripts into `Global/Dimensions/`

A pure relocation — no `#load` lines exist yet, so **no script content changes in
this task**. Doing it first means Tasks 3–4 write the consumers' `#load` lines once,
in final form.

**Files:**
- Move (git mv, content unchanged):
  - `Goose/Data/Illutia/Scripts/Map/DimensionMap.csx` → `Goose/Data/Illutia/Scripts/Global/Dimensions/`
  - `Goose/Data/Illutia/Scripts/Item/DimensionItem.csx` → same
  - `Goose/Data/Illutia/Scripts/Item/DimensionSurname.csx` → same
  - `Goose/Data/Illutia/Scripts/Item/DimensionRarity.csx` → same
  - `Goose/Data/Illutia/Scripts/Quest/DimensionUnlock.csx` → same
  - `Goose/Data/Illutia/Scripts/Quest/Rebirth.csx` → same
  - `Goose/Data/Illutia/Scripts/Spell/DimensionTeleport.csx` → same
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` — the seven
  `GetScript<T>` path strings:
  - `:332` `"Scripts/Map/DimensionMap.csx"` → `"Scripts/Global/Dimensions/DimensionMap.csx"`
  - `:469` `"Scripts/Quest/DimensionUnlock.csx"` → `"Scripts/Global/Dimensions/DimensionUnlock.csx"`
  - `:657` `"Scripts/Quest/Rebirth.csx"` → `"Scripts/Global/Dimensions/Rebirth.csx"`
  - `:794` `"Scripts/Item/DimensionSurname.csx"` → `"Scripts/Global/Dimensions/DimensionSurname.csx"`
  - `:810` `"Scripts/Item/DimensionRarity.csx"` → `"Scripts/Global/Dimensions/DimensionRarity.csx"`
  - `:845` `"Scripts/Item/DimensionItem.csx"` → `"Scripts/Global/Dimensions/DimensionItem.csx"`
  - `:1274` `"Scripts/Spell/DimensionTeleport.csx"` → `"Scripts/Global/Dimensions/DimensionTeleport.csx"`
- Modify: `Goose.Tests/Goose.Tests.csproj:24-37` — the seven `<None Include>` source
  paths (the `Link="DimensionScripts/<name>.csx"` output names stay put, so the
  fixture's `Source` column and every test that reads `DimensionScripts/…` from the
  output are untouched).
- Modify: `Goose.Tests/Fixtures/GlobalScriptFixture.cs`:
  - `ShippedScripts` `:22-28` — seven Relative paths → `"Scripts/Global/Dimensions/<name>.csx"`;
  - the temp-dir loop `:44-45` — list becomes `{ "Global", "Global/Dimensions" }`
    (`Path.Combine(DataDirectory, "Scripts", "Global/Dimensions")` creates the
    subfolder; the other kind dirs were only needed for the scripts that just left,
    and `CompileSpellEffectScript` already creates `Scripts/Spell` on demand);
  - `CompileShippedMapScript` `:88` — prefix `"Scripts/Map/"` → `"Scripts/Global/Dimensions/"`;
  - the `<remarks>` at `:16-19` — note that all scripts now live in one folder.
- Modify: `Goose.Tests/DimensionRebirthTests.cs:243` —
  `"Scripts/Quest/Rebirth.csx"` → `"Scripts/Global/Dimensions/Rebirth.csx"`.

**Mutation impact:**
- Source of truth changed: file *locations* only; byte-identical content.
- Important readers (complete list — verified by grep, nothing else references these
  paths): the seven `GetScript` calls in `Dimensions.csx` (world-load path), the
  fixture table + `CompileShippedMapScript`, the csproj copies, and
  `DimensionRebirthTests.cs:243`.
- Failure mode if a reader is missed: `Script<T>.LoadScript` throws
  `FileNotFoundException("Couldn't find script …")` (`Goose/Scripting/Script.cs:29`) at
  world load or test setup — loud and early, never a silent no-op.
- `GameWorld.LoadGlobalScripts` (`Goose/GameWorld.cs:693`) is unaffected: it scans
  only `Scripts/Global` non-recursively; the entry stays there, and the moved files
  are never scan-eligible entries (they only ever load via explicit `GetScript` paths).
  No other engine code enumerates the `Scripts/…` folders (verified: that one
  `EnumerateFiles` is the only one).
- Derived/cached state: `ScriptHandler`'s cache keys on the new absolute paths
  automatically (per-process, in-memory only).
- Invariants: identical runtime behavior; the `Scripts/{Map,Item,Quest,Spell}` folders
  still hold their non-dimension scripts (`ArenaMap.csx`, `HealerNPC.csx`, …).
- Observable proof: full suite green, including `DimensionRebirthTests` (the direct
  test-body path) and every consumer suite that compiles the moved scripts.

**Step 1:** Move and update paths.

```bash
mkdir -p Goose/Data/Illutia/Scripts/Global/Dimensions
git mv Goose/Data/Illutia/Scripts/Map/DimensionMap.csx      Goose/Data/Illutia/Scripts/Global/Dimensions/
git mv Goose/Data/Illutia/Scripts/Item/DimensionItem.csx    Goose/Data/Illutia/Scripts/Global/Dimensions/
git mv Goose/Data/Illutia/Scripts/Item/DimensionSurname.csx Goose/Data/Illutia/Scripts/Global/Dimensions/
git mv Goose/Data/Illutia/Scripts/Item/DimensionRarity.csx  Goose/Data/Illutia/Scripts/Global/Dimensions/
git mv Goose/Data/Illutia/Scripts/Quest/DimensionUnlock.csx Goose/Data/Illutia/Scripts/Global/Dimensions/
git mv Goose/Data/Illutia/Scripts/Quest/Rebirth.csx         Goose/Data/Illutia/Scripts/Global/Dimensions/
git mv Goose/Data/Illutia/Scripts/Spell/DimensionTeleport.csx Goose/Data/Illutia/Scripts/Global/Dimensions/
```

Then the path edits listed above. Sanity grep afterwards:

```bash
grep -rn "Scripts/Map/Dimension\|Scripts/Item/Dimension\|Scripts/Quest/DimensionUnlock\|Scripts/Quest/Rebirth\|Scripts/Spell/DimensionTeleport" \
  Goose Goose.Tests --include="*.cs" --include="*.csx" --include="*.csproj" | grep -v "/bin/\|/obj/"
```

Expected: zero hits.

**Step 2: Run and commit.**

```bash
dotnet test Goose.Tests
```

Expected: green, with no test-count reduction — behavior-neutral.

```bash
git add -A Goose/Data/Illutia/Scripts/ Goose.Tests/Goose.Tests.csproj \
        Goose.Tests/Fixtures/GlobalScriptFixture.cs Goose.Tests/DimensionRebirthTests.cs
git commit -m "Dimensions: consolidate the entry scripts in Scripts/Global/Dimensions"
```

---

### Task 3: `DimensionConstants.csx` — one definition of the seven shared constants

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionConstants.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (7 consts → aliases)
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionItem.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Rebirth.csx`
- Modify: `Goose.Tests/Goose.Tests.csproj` (+1 `<None Include>`)
- Modify: `Goose.Tests/Fixtures/GlobalScriptFixture.cs` (+1 `ShippedScripts` entry)

**Mutation impact:**
- Source of truth changed: the seven constants gain a single definition
  (`DimensionConstants`); `Dimensions` keeps them as **const aliases**
  (`public const int Offset = DimensionConstants.Offset;`) so the ~80 in-compilation
  usages (`Offset` alone appears 67 times) do not churn. Const-initializer-of-const is
  legal C# and folds at compile time — no runtime change.
- Important readers: the consumer scripts' private consts (deleted here), the
  fixture's temp-data layout (a `#load` that can't resolve fails at script-compile
  time — inside `Script<T>.LoadScript`, the `ScriptException` thrown from
  `script.RunAsync().Result` (`Script.cs:46`), i.e. during `GetScript`
  — not inside `OnLoaded`: the entry's own `#load`s resolve during `LoadGlobalScripts`,
  before any `OnLoaded` runs; a consumer's failing `GetScript` merely happens to be
  called from within the entry's `OnLoaded`. Loud, with the script file name in the
  diagnostic).
- Derived/cached state: none — scripts are recompiled fresh on `GetScript`/`ReloadScripts`
  (`Goose/Scripting/ScriptHandler.cs:32-44`); `#load`ed files are re-read on reload.
- Invariants: `Enabled = false` still leaves nothing behind (the disabled-variant test
  compiles the entry, which `#load`s the shared file — declarations only, no side
  effects at compile time).
- Observable proof: the full dimension suite green, including
  `DimensionsScriptTests.Disabled_*` and every consumer test.

**Step 1: The shared file.**

`Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionConstants.csx`:

```csharp
using System;

/// <summary>Compile-time constants shared by every dimension script. Loaded with
/// #load, which merges this file's declarations into each host script's compilation
/// - one definition, no cross-file drift.
///
/// Rules: declarations only (a #loaded file is not an entry script, so no top-level
/// return), and const only. Each host script's compilation gets its own copy of this
/// class (pinned by ScriptLoadDirectiveTests), so a mutable static here would not be
/// shared across scripts - it would just be a trap.</summary>
public static class DimensionConstants
{
    /// <summary>Dimension n's copy of anything lives at baseId + Offset*n.
    /// Must exceed every base id: Illutia map ids reach 10044, so 10000 is too small.</summary>
    public const int Offset = 100000;

    /// <summary>Generated item_surnames ids (DimensionItem rolls SurnameIdBase + 0..5).</summary>
    public const int SurnameIdBase = 900000;

    /// <summary>Generated item_titles ids: Legendary, Stunted.</summary>
    public const int TitleIdBase = 900100;

    /// <summary>PlayerProperties key holding the player's unlocked maximum dimension.</summary>
    public const string MaxDimensionProperty = "dimension.max";

    /// <summary>Registry id for the spirit currency.</summary>
    public const string SpiritCurrencyId = "spirit";

    /// <summary>Where rebirth leaves the player. Rebirth.csx reads these; Dimensions.csx
    /// prefights the same pair against class_info.</summary>
    public const int RebirthDestinationClassId = 1;   // Commoner
    public const int RebirthDestinationLevel = 1;
}
```

(Keep the existing doc comments from `Dimensions.csx` for the entries that have real
explanations — `Offset`, `SurnameIdBase`/`TitleIdBase`, `SpiritCurrencyId`,
`RebirthDestination*` — carried over verbatim where they say something this summary
doesn't.)

**Step 2: `Dimensions.csx` aliases + first `#load` line.**

Top of file, **before the `using`s** (CS8098):

```csharp
#load "Dimensions/DimensionConstants.csx"
using System;
...
```

The seven consts become aliases; their doc comments stay, with "must match" wording
replaced by "single definition in DimensionConstants.csx". In particular the
`RebirthDestinationClassId/Level` comment ("Rebirth.csx compiles separately and cannot
read these, so it hardcodes the same 1 and 1 - keep the two in step") is now obsolete
— replace it with "read by Rebirth.csx through DimensionConstants".

**Step 3: The three consumers touched in this task.**

Each gets its `#load` line(s) at the top (above the `using`s) and drops its private
const(s); the "Must match Dimensions.csx / scripts compile independently" comments go
with the consts.

- `DimensionItem.csx`: delete `Offset`, `SurnameIdBase`, `TitleIdBase`,
  `MaxDimensionProperty` (lines 14-17). Add the `#load` line (Constants only —
  Helpers comes in Task 4). Usages:
  `SurnameIdBase + index` → `DimensionConstants.SurnameIdBase + index`;
  `TitleIdBase`/`TitleIdBase + 1` → `DimensionConstants.TitleIdBase …`;
  the two `MaxDimensionProperty` reads in `CanPickup` →
  `DimensionConstants.MaxDimensionProperty` (replaced here, so the file still
  compiles once its private const is gone; Task 4 switches both reads to
  `DimensionHelpers.MaxDimensionOf(player)`).
  Leave `Offset` usages for Task 4 (helpers).
- `DimensionMap.csx`: delete `MaxDimensionProperty` (line 20); add the Constants line.
  The read inside its private `MaxDimensionOf` → `DimensionConstants.MaxDimensionProperty`
  (replaced here, so the file still compiles once its private const is gone; Task 4
  deletes the private method and points its two call sites at
  `DimensionHelpers.MaxDimensionOf`). Leave `Offset` for Task 4.
- `Rebirth.csx`: delete `SpiritCurrencyId` (line 18); add the Constants line.
  `world.CurrencyHandler.Get(SpiritCurrencyId)` → `world.CurrencyHandler.Get(DimensionConstants.SpiritCurrencyId)`
  (both occurrences); `player.ChangeClass(1, 1, world, 0d)` (`:72`) →
  `player.ChangeClass(DimensionConstants.RebirthDestinationClassId,
  DimensionConstants.RebirthDestinationLevel, world, 0d)`, and rewrite the "hardcoded
  because .csx files compile separately" comment — the destination pair is now read
  from the shared constants, so the drift hazard is gone.

**Step 4: Fixture plumbing — the documented "add to BOTH lists" trap**
(`GlobalScriptFixture.cs:11`).

- `Goose.Tests.csproj`, alongside the existing entries (`:22-37`):

```xml
<None Include="../Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionConstants.csx"
      Link="DimensionScripts/DimensionConstants.csx" CopyToOutputDirectory="PreserveNewest" />
```

- `GlobalScriptFixture.ShippedScripts` (`:20-29`):

```csharp
("DimensionConstants.csx",   "Scripts/Global/Dimensions/DimensionConstants.csx"),
```

(The `Scripts/Global/Dimensions` temp dir was already created in Task 2.)

**Step 5: Run and commit.**

```bash
dotnet test Goose.Tests --filter "FullyQualifiedName~Dimension|FullyQualifiedName~SpiritCurrency"
dotnet test Goose.Tests
```

Expected: green. A wrong `#load` path fails at script-compile time (the
`ScriptException` from `script.RunAsync().Result`, `Script.cs:46` — inside `GetScript`,
not inside `OnLoaded`) with the script's file name in the message — if red, the path
is wrong, not the code.

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx \
        Goose/Data/Illutia/Scripts/Global/Dimensions/ \
        Goose.Tests/Goose.Tests.csproj Goose.Tests/Fixtures/GlobalScriptFixture.cs
git commit -m "Dimensions: single source for the seven cross-script constants via #load"
```

---

### Task 4: `DimensionHelpers.csx` — delete the copied functions

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionHelpers.csx`
- Modify: `Global/Dimensions/DimensionItem.csx`, `DimensionMap.csx`,
  `DimensionTeleport.csx`, `DimensionUnlock.csx`, `DimensionSurname.csx` (second
  `#load` line + copy deletion)
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (Helpers `#load` line;
  `ResetItemCommandEvent`/`DimensionCommandEvent` inline math → helpers)
- Modify: `Goose.Tests/Goose.Tests.csproj` (+1), `Goose.Tests/Fixtures/GlobalScriptFixture.cs` (+1 entry)

**Helper contracts** (all pure: no registration, no packets, no world mutation;
precondition is only that the handler lookup succeeds or returns null as documented;
each host compilation gets its own copy, so statics must stay stateless):

```csharp
using System;
using Goose;
using Goose.Scripting;

/// <summary>Stateless dimension math shared by every dimension script. #loaded, so
/// each host compilation gets its own copy - keep this file free of mutable statics
/// (pinned by ScriptLoadDirectiveTests). Everything here is a read or a compute.</summary>
public static class DimensionHelpers
{
    /// <summary>The dimension an id encodes: baseId + Offset*dim.</summary>
    public static int DimensionOf(int id) { return id / DimensionConstants.Offset; }

    /// <summary>The dimension-0 base id under a dimension id.</summary>
    public static int BaseId(int id) { return id % DimensionConstants.Offset; }

    /// <summary>The player's unlocked maximum dimension (0 = dimension 0 only).</summary>
    public static int MaxDimensionOf(Player player)
    {
        return player.Properties.GetProperty<int>(DimensionConstants.MaxDimensionProperty, 0);
    }

    /// <summary>The dimension-0 map's script, or null if the base map or its script is
    /// missing. The dimension script's delegation target.</summary>
    public static IMapScript BaseMapScript(Map map, GameWorld world)
    {
        return world.MapHandler.GetMap(BaseId(map.ID))?.Script?.Object;
    }

    /// <summary>The base template's script, or null. Same delegation role for items.</summary>
    public static IItemScript BaseItemScript(Item item, GameWorld world)
    {
        return world.ItemHandler.GetTemplate(BaseId(item.TemplateID))?.Script?.Object;
    }

    /// <summary>AttributeSet.java:405-419 tier, computed from the BASE template (a clone's
    /// value is already scaled by 3^dim and would put everything in the top tier). A
    /// missing base template - the feature was disabled and re-enabled around a data
    /// change - scores the lowest tier rather than throwing inside a roll.
    /// Abyss's top tier (1.5) keys off an SP-priced template; goose has no SP value, so
    /// that tier has no equivalent and is dropped.</summary>
    public static double Tier(ItemTemplate basic)
    {
        if (basic == null) return 0.25;
        if (basic.Value >= 10000000) return 1.0;
        if (basic.MinExperience > 0) return 0.75;
        if (basic.MinLevel == 50) return 0.5;
        return 0.25;
    }

    /// <summary>Shared gate refusal for /dimension, map entry and login re-check.</summary>
    public static string MaxDimensionRefusal(int max)
    {
        return "The void has rejected you. You have a maximum dimension of " + max + ".";
    }
}
```

Behavior notes:

- `Tier` unifies two copies that differ only in the null check
  (`Dimensions.csx:1052` has none, `DimensionSurname.csx:59` returns 0.25). Every
  `Dimensions.csx` call site passes a non-null base template, so unifying on the
  null-safe version is a no-op for it.
- `BaseMapScript`/`BaseItemScript` are the `Inner(…)` helpers, generalized; the typed
  return comes from `Map.Script` being `Script<IMapScript>` (`Goose/Map.cs:59`) and
  `ItemTemplate.Script` being `Script<IItemScript>` (`Goose/ItemTemplate.cs:118`).

**Consumer edits** (`#load` lines: `DimensionItem.csx` and `DimensionMap.csx` get the
Helpers line — they already have the Constants line from Task 3; `DimensionTeleport.csx`,
`DimensionUnlock.csx` and `DimensionSurname.csx` have no `#load` line yet and get
**both** lines — DimensionTeleport needs Constants for the `OffsetOf` fallback,
DimensionUnlock for the `dimension.max` write; DimensionSurname uses only the Helpers
after its edits and gets the Constants line for uniformity: every dimension consumer
carries both, the shared file is declarations-only, and a future Constants use there
is a const rename, not a recompile hunt. Delete
the private copies; the "scripts compile independently" comments go with them):

- `DimensionItem.csx`: delete `Offset` const + `DimensionOf` + `Inner`.
  `DimensionOf(item)` → `DimensionHelpers.DimensionOf(item.TemplateID)`;
  `Inner(item, world)` → `DimensionHelpers.BaseItemScript(item, world)`;
  `item.TemplateID % Offset` → `DimensionHelpers.BaseId(item.TemplateID)`;
  the two `DimensionConstants.MaxDimensionProperty` reads in `CanPickup` (rewritten
  in Task 3) → `DimensionHelpers.MaxDimensionOf(player)`; in `OnUseConsumableEvent`:
  `incoming.ID % Offset` → `BaseId(incoming.ID)`, `known.ID % Offset` → `BaseId(known.ID)`,
  `known.ID / Offset` → `DimensionOf(known.ID)`, `incoming.ID / Offset` → `DimensionOf(incoming.ID)`.
- `DimensionMap.csx`: delete `Offset` const + `DimensionOf` + `MaxDimensionOf` + `Inner`.
  `DimensionOf(map)` → `DimensionHelpers.DimensionOf(map.ID)`;
  both `MaxDimensionOf(player)` call sites → `DimensionHelpers.MaxDimensionOf(player)`
  (the deleted method's read already goes through `DimensionConstants.MaxDimensionProperty`
  since Task 3);
  `Inner(map, world)` → `DimensionHelpers.BaseMapScript(map, world)`;
  `map.ID % Offset` → `BaseId(map.ID)` (`OnPlayerEntered` fallback);
  `player.BoundID / Offset` → `DimensionOf(player.BoundID)`,
  `player.BoundID % Offset` → `BaseId(player.BoundID)` (`ClampBind`);
  both refusals → `DimensionHelpers.MaxDimensionRefusal(max)` (the login one keeps its
  `"$7"` prefix: `"$7" + DimensionHelpers.MaxDimensionRefusal(max)`).
- `DimensionTeleport.csx`: delete `Offset` const + `DimensionOf`.
  `OffsetOf`'s fallback → `DimensionConstants.Offset`;
  `DimensionOf(map)` → `DimensionHelpers.DimensionOf(map.ID)` (keep the `map == null ? 0`
  guard where it is).
- `DimensionUnlock.csx`: add the Constants + Helpers lines (it has no `#load` line
  yet — the #load map gives it both siblings). Delete the private
  `MaxDimensionProperty` (line 14). In `GiveReward`: the read
  `player.Properties.GetProperty<int>(MaxDimensionProperty, 0)` →
  `DimensionHelpers.MaxDimensionOf(player)`, and the WRITE
  `player.Properties[MaxDimensionProperty] = granted;` (line 26) →
  `player.Properties[DimensionConstants.MaxDimensionProperty] = granted;` — the write
  keeps the property key via `DimensionConstants` (there is no write helper), so the
  literal `"dimension.max"` survives in exactly one file and Task 6's grep holds.
- `DimensionSurname.csx`: add both lines (Constants for uniformity — it uses only the
  Helpers for now); delete `Offset` const + private `Tier`.
  `item.TemplateID / Offset` → `DimensionHelpers.DimensionOf(item.TemplateID)`;
  `Tier(world.ItemHandler.GetTemplate(item.TemplateID % Offset))` →
  `DimensionHelpers.Tier(world.ItemHandler.GetTemplate(DimensionHelpers.BaseId(item.TemplateID)))`.
- `Dimensions.csx`: add the Helpers `#load` line. In `DimensionCommandEvent`,
  `GetProperty<int>(Dimensions.MaxDimensionProperty, 0)` → `DimensionHelpers.MaxDimensionOf(this.Player)`
  and the refusal → `DimensionHelpers.MaxDimensionRefusal(max)`. In
  `ResetItemCommandEvent`, `item.TemplateID / Dimensions.Offset` →
  `DimensionHelpers.DimensionOf(item.TemplateID)` and
  `item.TemplateID % Dimensions.Offset` → `DimensionHelpers.BaseId(item.TemplateID)`.
  Also, now, while the file is still monolithic: the single call site `Tier(basic)`
  (`:1011`, inside `DimensionStats`) → `DimensionHelpers.Tier(basic)`, and delete the
  private `Tier` (`:1052-1058`) — Task 5's move then carries the call site into
  `Items.csx` verbatim, with no private `Tier` to drag along.

**Step: fixture plumbing** — same two edits as Task 3 for `DimensionHelpers.csx`
(csproj `<None Include>`, `ShippedScripts` entry; the temp dir exists since Task 2).

**Step: run and commit.**

```bash
dotnet test Goose.Tests --filter "FullyQualifiedName~Dimension"
dotnet test Goose.Tests
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx \
        Goose/Data/Illutia/Scripts/Global/Dimensions/ \
        Goose.Tests/Goose.Tests.csproj Goose.Tests/Fixtures/GlobalScriptFixture.cs
git commit -m "Dimensions: shared id math, gate reads and tier via #loaded helpers"
```

---

### Task 5: Split `Dimensions.csx` into the `Dimensions/` folder

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx`, `Maps.csx`,
  `Items.csx`, `Spells.csx`, `Commands.csx`, `SpiritCurrency.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx` (becomes the entry:
  `#load` block + `partial` on the class declaration + `Dimensions` class
  config/`TryParseAmount`/`OnLoaded`)
- Modify: `Goose.Tests/Goose.Tests.csproj` (+6), `Goose.Tests/Fixtures/GlobalScriptFixture.cs`
  (+6 `ShippedScripts` entries, update the `<remarks>` at `:16-19`)
- Modify (comment-only): `Goose.Tests/DimensionVendorStockTests.cs`,
  `Goose.Tests/DimensionResetItemTests.cs`, `Goose.Tests/SpiritCurrencyTests.cs` —
  the seven `.csx:NN` line citations this split invalidates (Step 1b)

**Why `partial class Dimensions` for the pass files, not separate classes:** every
method in the pass files is stateless (they take `world` explicitly and read only
consts and `static readonly` data), so `private` already works — with `partial`, the
split is a **pure move**: zero signature changes, zero visibility changes, `OnLoaded`
and every `Dimensions.X` reference stay valid, and the diff is lines relocating.
Separate static classes would force `private` → `public`/`static` rewrites across ~30
methods for no runtime difference. `Commands.csx` and `SpiritCurrency.csx` are already
top-level classes — pure moves, no `partial`.

**Part file shapes** (each starts with its own `using` block — usings are
per-compilation-unit — then the class header):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Quests;
using Goose.Scripting;

public partial class Dimensions
{
    // ...moved methods, verbatim...
}
```

Trim the `using` block per part (the pass files want roughly the example set —
`Goose.Quests` for `Npcs.csx`'s quest types, `Goose.Scripting` for the `GetScript<T>`
calls). Two traps verified: `ICurrency` is declared in namespace `Goose`
(`Goose/Currency/ICurrency.cs:1`) and `Event` in `Goose` (`Goose/Event.cs:8`) — so
`Commands.csx` and `SpiritCurrency.csx` need only `using System; using Goose;` and no
`Goose.Currency`/`Goose.Events` using. Unused usings are harmless if left in.

**What goes where** (methods move verbatim, including their `///` comments; the
`GetScript` path strings inside them are already final from Task 2):

| Part | Members |
|---|---|
| `Npcs.csx` | `CloneTemplates`, `ScaleTemplate`, `ScaleHP`, `ScaleDamage`, `ScaleAttackSpeed`, `ScaleExperience`, `ScaleRespawn`, `Recolour`, `CloneSpawns`, `RewireAllies`, `CreateUnlockChain`, `ValidateWardenClass`, `CreateWarden`, `CreateRebirthQuest` |
| `Maps.csx` | `CloneMaps`, `MinExperienceFor`, `RewireWarps` |
| `Items.csx` | `SurnameNames` (static readonly), `RegisterModifiers`, `ShouldClone`, `CloneItemTemplates`, `ScaleItemTemplate`, `RepointDrops`, `RepointVendorStock`, `DimensionStats`; the private `Tier` call sites use `DimensionHelpers.Tier` (no private copy) |
| `Spells.csx` | `PreflightSpellIds`, `CloneSpellEffects`, `RewireSpellEffects`, `AddEffectIfPresent`, `CloneSpells`, `RewriteTeleportEffects`, `ScaleSpellEffect`, `ScaleBuffStats`, `ScaleFormula`, `MorphTargetShape`, `DimensionPrefixes` (static readonly), `PrefixFor`, `DescriptionPrefixFor` |
| `Commands.csx` | `DimensionCommandEvent`, `ResetItemCommandEvent`, `BuyGoldCommandEvent`, `BuyExperienceCommandEvent`, `GiveSpiritCommandEvent` (all five top-level classes + their `///` headers) |
| `SpiritCurrency.csx` | `SpiritCurrency` |

The entry keeps: the `#load` block (8 lines, first tokens), its `using`s (all seven
current ones; any now-unused are harmless), and the
`Dimensions` class with the `// ---- Configuration ----` consts (the seven are already
aliases), `TryParseAmount` (called as `Dimensions.TryParseAmount` by the commands —
same compilation, stays), `OnLoaded`, and `return typeof(Dimensions);`.

**Mutation impact:**
- Source of truth changed: the location of the feature's source text only — no member
  moves across classes, no behavior.
- Important readers: `GameWorld.LoadGlobalScripts`
  (`Goose/GameWorld.cs:693`) — non-recursive, so the entry **must** remain at
  `Scripts/Global/Dimensions.csx`; the part files are declarations-only and are never
  passed to `GetScript<IGlobalScript>` (if one ever were, `Activator.CreateInstance`
  on a null `ReturnValue` throws at `Goose/Scripting/Script.cs:49` — loud, not silent).
- Derived/cached state: `ScriptHandler`'s cache is keyed by the entry path, which is
  unchanged; `ReloadScripts` re-runs `LoadScript` and re-reads the `#load`ed parts with
  the host (`Goose/Scripting/ScriptHandler.cs:32-44`).
- Invariants: `Enabled = false` leaves nothing behind; the disabled-variant tests
  (`DimensionsScriptTests.cs:27-33`, `DimensionRebirthTests.cs:159-166`,
  `DimensionTeleportScriptTests.cs:229`) read the shipped entry, flip
  `Enabled = true` → `false` (the line stays in the entry), and compile the variant
  from `Scripts/Global/` — its `#load "Dimensions/…"` paths resolve against the same
  installed parts.
- Observable proof: the full dimension suite (14 test classes) green, including the
  three disabled-mode tests, the `/` command tests (the commands now live in
  `Commands.csx`), and `SpiritCurrencyTests`.

**Step 1: Create the six part files** by moving the blocks above, and make the entry
class `partial` — `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:9` goes from

```csharp
public class Dimensions : BaseGlobalScript
```

to

```csharp
public partial class Dimensions : BaseGlobalScript
```

Every declaration of a partial class must carry `partial`, or the entry's own
compilation fails (CS0260) the moment the part files exist. Sanity check after the
move — the entry must contain nothing it lost:

```bash
# every moved member exactly once across entry + parts:
grep -c "private void CloneMaps\|private NPCTemplate ScaleTemplate\|class SpiritCurrency" \
  Goose/Data/Illutia/Scripts/Global/Dimensions.csx Goose/Data/Illutia/Scripts/Global/Dimensions/*.csx
```

**Step 1b: Fix the stale `.csx:NN` comment citations the split invalidates.** Seven
test comments cite dimension-script line numbers; the split renumbers the entry and
moves the cited members into part files, so they would point at wrong code (some
already drift today: `ShouldClone` is at `:828`, not the cited `:811`/`:777`, and
`DimensionItem.csx:65-72` cites the suffix-roll region, not `CanPickup` at `:94-101`).
Line numbers shift with every script edit — names do not. Convert all seven to
member-name references:

- `DimensionVendorStockTests.cs:13`, `DimensionResetItemTests.cs:12`,
  `SpiritCurrencyTests.cs:11` — `// Dimensions.csx:19` next to the test-local
  `private const int Offset = 100000;` (the tests keep their own literal: a
  test-assertion value in an independent compilation, not a `#load` consumer) →
  `// must match DimensionConstants.Offset`
- `DimensionVendorStockTests.cs:76`, `DimensionResetItemTests.cs:23` —
  `ShouldClone, Dimensions.csx:811` / `Dimensions.csx:777` → `ShouldClone (Items.csx)`
- `DimensionVendorStockTests.cs:166` — `DimensionItem.csx:65-72` →
  `CanPickup in DimensionItem.csx`
- `DimensionResetItemTests.cs:139` — `Dimensions.csx:987` →
  `the clone naming in ScaleItemTemplate (Items.csx)`

The one remaining `.csx:NN` citation in the tests — `ItemRerollTests.cs:40` citing
Aspereta's `ItemModifierScript.csx` — stays: not a dimension script, never moved by
this plan.

**Step 2: Fixture plumbing** — six `<None Include>` entries in `Goose.Tests.csproj`
(pattern from `:22-37`, `Link="DimensionScripts/<name>.csx"`), six `ShippedScripts`
entries (`"Scripts/Global/Dimensions/<name>.csx"`), and a one-line update to the
fixture's `<remarks>` ("…plus the six part files and two shared files that
`Dimensions.csx` `#load`s").

**Step 3: Update the maintained feature doc** (`docs/dimensions.md`). Its "Scripts"
section still describes the monolithic global script and names the old
`Scripts/Map/DimensionMap.csx` path. Historical design/implementation plans under
`docs/plans/` stay as-is; only the maintained doc changes. Rewrite the two bullets to
describe the final layout and the shared-file mechanism, e.g.:

```markdown
- **`Scripts/Global/Dimensions.csx`** — the entry, auto-run by
  `GameWorld.LoadGlobalScripts`, which scans `Scripts/Global` non-recursively (so the
  entry must stay at this path). All configuration at the top (`Enabled` toggle,
  number of dimensions, ID offset, scaling formulas, spirit prices); disable the
  feature by setting `Enabled = false`. Its body is split into part files under
  `Scripts/Global/Dimensions/` that it pulls in with first-line `#load` directives
  (anchored by `ScriptOptions.WithFilePath`; see
  `docs/plans/2026-08-16-dimensions-scripts-cleanup.md`), assembled at compile time as
  `partial class Dimensions`.
- **`Scripts/Global/Dimensions/`** — the rest of the feature: `DimensionConstants.csx`
  (the seven cross-script constants, one definition, `#load`ed by every consumer),
  `DimensionHelpers.csx` (stateless id math, the `dimension.max` read, base-script
  delegation, tier, refusal message), the entry's six part files, and the seven
  consumer scripts (`DimensionMap.csx`, `DimensionItem.csx`, `DimensionSurname.csx`,
  `DimensionRarity.csx`, `DimensionUnlock.csx`, `Rebirth.csx`, `DimensionTeleport.csx`)
  moved here from `Scripts/{Map,Item,Quest,Spell}`. `#load`ed files must stay
  declarations-only (const + stateless statics): each host compilation gets its own
  copy of a loaded file, so a mutable static there would not be shared (pinned by
  `ScriptLoadDirectiveTests`).
- **`DimensionMap.csx`** — attached to every cloned map by the entry; applies NPC
  scaling on (re)spawn and entry gating.
```

Leave the doc's other sections (world cloning, access, NPC scaling, spells, items,
spirit economy) untouched — they name behavior, not paths — **except** the stale
offset value: the doc still says the ID offset is `10000` in six places (`:104`,
`:149`, `:175`, `:182`, `:187`, `:228` — forms `10000·dim`, `10000·d`, bare
`(10000)`), but it is `100000` (bumped because Illutia map ids reach 10044 — see the
`Offset` comment). Pre-existing bug; fix all six to `100000` while the file is open
and already in this commit.

**Step 4: Run the whole suite** — `CompileShipped` compiles the real entry, which now
pulls in all eight `#load`ed files; a missing fixture file or bad path fails at
script-compile time, inside `GetScript` (the `ScriptException` from
`script.RunAsync().Result`, `Script.cs:46`) — before the entry's
`OnLoaded` ever runs.

```bash
dotnet test Goose.Tests
```

Expected: green, same count as Task 4 (no new tests — the split is behavior-neutral;
the suite *is* the regression net).

**Step 5: Commit.**

```bash
git add Goose/Data/Illutia/Scripts/Global/Dimensions.csx \
        Goose/Data/Illutia/Scripts/Global/Dimensions/ \
        Goose.Tests/Goose.Tests.csproj Goose.Tests/Fixtures/GlobalScriptFixture.cs \
        Goose.Tests/DimensionVendorStockTests.cs Goose.Tests/DimensionResetItemTests.cs \
        Goose.Tests/SpiritCurrencyTests.cs docs/dimensions.md
git commit -m "Dimensions: split the global script into the Global/Dimensions folder"
```

---

### Task 6: Wrap-up checks (no code, no commit unless something surfaces)

1. `grep -rn "Must match Dimensions" Goose/Data/Illutia/Scripts/` — expect zero hits
   (every drift comment died with its const).
2. `grep -rn "compile independently\|compile separately" Goose/Data/Illutia/Scripts/` —
   expect zero hits (the premise of the old comments no longer holds for these files).
3. `grep -rn "Offset *= *100000\|SurnameIdBase *= *900000\|TitleIdBase *= *900100\|MaxDimensionProperty *= *\"dimension.max\"\|SpiritCurrencyId *= *\"spirit\"" \
   Goose/Data/Illutia/Scripts/ --include=*.csx` — expect the assignments only in
   `DimensionConstants.csx`. Symbol-specific on purpose: the raw values also occur
   legitimately outside the shared file — `QuestIdBase = 900000` (the entry's own
   quest-id base, not one of the seven), `SpiritCurrency.Name` returning `"spirit"`
   (in `SpiritCurrency.csx` after Task 5), and a comment quoting `CurrencyId = "spirit"`.
4. `grep -rn "\.csx:[0-9]" Goose.Tests --include="*.cs" | grep -v "/bin/\|/obj/"` —
   expect exactly one hit: `ItemRerollTests.cs:40` citing Aspereta's
   `ItemModifierScript.csx` (not a dimension script, never moved). Every
   dimension-script citation in test comments is name-only after Task 5's Step 1b.
5. `grep -rn "Scripts/Map/Dimension\|Scripts/Item/Dimension\|Scripts/Quest/DimensionUnlock\|Scripts/Quest/Rebirth\|Scripts/Spell/DimensionTeleport" \
   Goose Goose.Tests --include="*.cs" --include="*.csx" --include="*.csproj" | grep -v "/bin/"`
   — expect zero hits (no stale old-folder path strings).
6. Confirm the Aspereta dataset is untouched (no dimension scripts, no `#load`; the
   engine change is a no-op for it — design doc "Scope").
7. If anything is off, fix under the task it belongs to; otherwise close out.

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| `#load` resolves through the real engine path (sibling, subdirectory, parent forms) | `ScriptLoadDirectiveTests` (red without `WithFilePath`: CS8098) |
| `#load` must precede all tokens | `ScriptLoadDirectiveTests` host file shape; CS8098 otherwise |
| Shared file is not a shared *runtime* object (the fact behind the const-only rule — the test proves the rationale, it does not enforce the rule against future edits) | `ScriptLoadDirectiveTests` per-host mutable-static assertion (adversarial) |
| One definition of each of the seven constants | compile structure + Task 6 greps; drift is now a compile error in the consumer, not a silent value mismatch |
| Consumers behave identically after de-duplication | all consumer suites: `DimensionItemScriptTests`, `DimensionMapScriptTests`, `DimensionTeleportScriptTests`, `DimensionModifierTests`, `DimensionRebirthTests`, `DimensionCurrencyCommandTests`, `DimensionCommandGateTests`, `DimensionResetItemTests`, `DimensionDropTests`, `DimensionVendorStockTests`, `DimensionItemTemplateTests`, `DimensionSpellScriptTests`, `DimensionScalingOverflowTests`, `SpiritCurrencyTests` |
| The folder move is behavior-neutral | Task 2 full-suite run (green, no test-count reduction) + `DimensionRebirthTests` (the one test with a direct path literal) + every consumer suite compiling the moved scripts |
| Split is behavior-neutral, `Enabled = false` still inert | `DimensionsScriptTests` (incl. `Disabled_*`), `DimensionRebirthTests.Disabled_*`, `DimensionTeleportScriptTests.Disabled_*` — the variant flips the flag in the real entry and `#load`s the real parts |
| A missing/moved shared file fails loudly at world load | `InstallShippedScripts` throws with an actionable message (`GlobalScriptFixture.cs:68-70`); a bad `#load` string fails inside `GetScript` with a `ScriptException` from `script.RunAsync().Result` (`Script.cs:46`) — for the entry, during `LoadGlobalScripts`; for a consumer, from within the entry's `OnLoaded`, but at compile time — with the script's file name in the diagnostic; a stale `GetScript` path throws `FileNotFoundException` at `Script.cs:29` |

## Risks / red-team notes

- **`#load` strings are the one new fragile thing** (design doc, accepted): a moved
  shared file fails at script-compile time inside world load — loud, and now with real
  file names in diagnostics. Same for the `GetScript` path strings the move touched:
  a miss throws `FileNotFoundException` at world load, not a silent no-op.
- **Part files must never become entries.** If someone adds a top-level `return` to a
  part file, or `LoadGlobalScripts` ever becomes recursive, the world load throws in
  `Activator.CreateInstance`. Keep the parts declarations-only; the folder being
  non-scanned is what makes that safe today.
- **Test parallelization:** any new test class touching `Goose.Tests` that swaps
  `GameWorld.Settings` needs `[Collection(GameWorldSettingsCollection.Name)]` —
  verified failure mode in this session (a spike passed alone, failed in the full run
  reading another fixture's temp dir).
- **Fixture "both lists" trap** (`GlobalScriptFixture.cs:11`): csproj `<None Include>`
  and `ShippedScripts` must move together; a miss fails at script-compile time in the
  fixture's `GetScript` (missing host file → `FileNotFoundException` at `Script.cs:29`;
  unresolvable `#load` → `ScriptException` from `script.RunAsync().Result` (`Script.cs:46`)), not at C# test compile time.
- **No persistence impact:** no DB, config or packet format changes; all values are
  compile-time consts and pure functions with identical arithmetic.

## Out of scope (per the committed design doc, reaffirmed)

- `#embed`, extra `#r` references, `#load` outside the dimension scripts.
- The Aspereta dataset.
- Option B (constants class in the core assembly) and Option C (load-time drift
  check) — C becomes unneeded once the values have one definition.
- The warden/keeper appearance pattern (single-file duplication of a shape, not of
  values).
- Touching the non-dimension scripts in `Scripts/{Map,Item,Quest,Spell}` — they keep
  their paths and their kind folders.
