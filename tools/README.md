# Data editor generators

Both tools produce inputs for the Apps Script data editor in `tools/DataEditor/`. Their outputs
(`schema.js` and the three `sprites-*.html`) are **checked in**, so the editor front end can be
developed without a client checkout. Both are byte-reproducible: regenerating without changing any
input leaves the working tree clean.

Design and rationale: `docs/plans/2026-07-27-game-data-editor-part2-generators.md`.

Paths below are one machine's. Substitute your own checkout locations; the commands assume you run
them from the repo root.

## What the front end receives

The generated files assign exactly two globals.

`GOOSE_SCHEMA.sheets[]` — one entry per spreadsheet sheet, `{sheet, table, columns[], composites[],
indexes[]}`. Each column is `{name, kind, sql, required, pk}` plus optional `default`, `ref` and
`enumNames`.

`GOOSE_SPRITES[name]` for `name` in `icons`, `parts`, `effects` — `{width, height, png, rects}`.
`png` is a `data:image/png;base64,…` atlas; `rects` maps a sprite key to `[x, y, w, h]` in atlas
pixel coordinates. The three key formats are minted in `tools/SpriteBundle/Bundles.cs`:

| bundle    | key                      | example                  |
| --------- | ------------------------ | ------------------------ |
| `icons`   | `<sheet>:<graphic>`      | `104:50744`              |
| `parts`   | `<category>:<id>:<clip>` | `Bodies:10101:idle-down` |
| `effects` | `<id>:<frameIndex>`      | `1080:0`                 |

Drawing a sprite is a canvas `drawImage` with the rect as source coordinates. Tinting applies
`mix(rgb, tint.rgb, tint.a)` with alpha preserved, matching `Scripts/UI/Icon.cs` in the client.

## SchemaGen

Emits `schema.js` from the column descriptors in `CsvToSql.Core`. Run after adding or changing any
column:

    dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js

`Checked_in_schema_js_is_up_to_date` fails if you forget.

## SpriteBundle

Emits three sprite atlases as inlined HTML. Needs the client repo checked out. Run when the
client's art changes:

    CLIENT=/home/hayden/code/Goose2ClientGodot/Assets/Sprites
    dotnet run --project tools/SpriteBundle -- "$CLIENT" tools/DataEditor

Give the client an **absolute path**, or one relative to your shell's cwd. A `../Goose2ClientGodot`
style path does not work from a git worktree, where the repo root is `.worktrees/<branch>/` and the
sibling checkout is two levels further up.

Output as of 2026-07-29, on one machine with a warm cache:

    icons      4827 sprites  2048x3140  98.1% efficient  1.67 MB html
    parts      3261 sprites  2048x6903  96.0% efficient  1.89 MB html
    effects    2412 sprites  2048x4097  95.3% efficient  0.83 MB html
    Total 4.39 MB of HTML in 2.3s

Roughly 4.4 MB combined, comfortably inside the ~10 MB Apps Script project ceiling.

"Efficient" is the share of the atlas's area covered by sprites — the rest is packing waste that
still costs PNG bytes. Around 95% is the accepted floor. **Nothing enforces it**: no test asserts
it on real assets, so it is a number a human reads here. A drop much below 95% usually means a new
outsized sprite is forcing tall, half-empty shelves; worth a look, but it will never fail a build
for you.

### Known upstream asset defects

The tool reports skips grouped by sheet. Two groups are expected today, and both are bugs in the
client's art, not in the tool. Do not re-investigate:

- **Sheet 421 loses 19 icons.** The manifest lays it out as a 10x10 grid of 32px cells — a 320x320
  sheet — but `sheets/421.png` is 288x288, one row and one column short. The 10 rects at x=288 and
  the 10 at y=288 (sharing a corner) fall outside the image; the other 81 icons are unaffected.
- **Sheet 4589 has no PNG at all.** Its 20 frames are the whole of effect 290370, so that effect is
  absent from `sprites-effects.html` entirely.

### Adding graphics the data has not referenced yet

`tools/SpriteBundle/sheets.json` lists which sheets go into the icon bundle. It was seeded from the
sheets the two datasets reference, so a sheet nobody has used will not appear and its graphics
cannot be picked in the editor. To add one, put its number in `iconSheets` and regenerate.

To re-derive the list, pass **every** dataset's database in one invocation — the tool unions them
itself:

    dotnet run --project tools/SpriteBundle -- derive-sheets \
      Goose/bin/Debug/IllutiaGoose.db \
      /home/hayden/code/illutiagooseserver/Goose/bin/Debug/AsperetaGoose.db

    Goose/bin/Debug/IllutiaGoose.db: 113 sheets
    .../AsperetaGoose.db: 22 sheets
    union: 125 sheets

Counts go to stderr; stdout is the `iconSheets` array body, indented and wrapped, ready to paste
over the existing one unedited. Running it against a single database and pasting that output would
silently drop the other dataset's sheets — the checked-in list covers both Illutia and Aspereta.

Those `.db` files are **server build artifacts**, so a fresh worktree has none until you build and
run there (see `Readme.md`, "Connecting to the database"). Their names come from `DatabaseName` in
`GooseSettings.json`, so Illutia and Aspereta are the same server run under two different configs —
which is why the two paths above point at two different checkouts rather than sitting side by side.
Use whatever paths your own builds produced.

## Tests

    CLIENT=/home/hayden/code/Goose2ClientGodot/Assets/Sprites
    GOOSE_CLIENT_ASSETS="$CLIENT" dotnet test tools/Tools.Tests

`GOOSE_CLIENT_ASSETS` points the asset-gated tests at the client. Without it (and without a sibling
checkout at the default location) around 20 tests **skip** — a green run then proves nothing about
the sprite *builder*, though `BundleArtifactTests` still covers the committed bundles. A
`GOOSE_CLIENT_ASSETS` that is set but wrong fails loudly rather than skipping, so a typo cannot
masquerade as a checkout-less machine.

### Why schema.js has a drift guard and the bundles do not

`schema.js` derives purely from sources in this repo, so an equality test against a fresh render is
always actionable — hence `Checked_in_schema_js_is_up_to_date`.

The bundles derive from a separate client checkout at an unpinned revision. An equality test would
have to be asset-gated, so it would pass in CI and fail on exactly those machines that *do* have a
checkout, every time the client's art moved ahead — unfixable from this repo. `BundleArtifactTests`
therefore checks the committed files structurally (header, embedded PNG, non-empty rect index),
catching a truncated write or an accidental deletion. This asymmetry is deliberate; please don't
"fix" it.

### Merge conflicts on the bundles

`.gitattributes` marks `sprites-*.html` as generated, non-diffable and `-merge`. Each embeds its
atlas as a single base64 line around a megabyte long, which cannot be merged textually. A conflict
there is resolved by regenerating the bundles, never by hand-editing them.

## DataEditor

Apps Script sources live in `tools/DataEditor/`. Pure logic is under `src/` as plain `.js` so it
can be unit-tested:

    node --test "tools/DataEditor/test/*.test.js"

Pass the glob, not the bare directory. With no `package.json` anywhere above it, Node reads a
positional directory as a module entrypoint and fails with `MODULE_NOT_FOUND` rather than
recursing into it.

`Code.gs` is covered too, via `test/fake-sheets.js` — a small `SpreadsheetApp` model that Code.gs
runs against in a `vm` context. It exists for the two claims that reasoning alone kept getting
wrong: that a cell's value is its stored value rather than its formatting, and that saving a record
writes only the cells that changed. What it assumes about Apps Script is listed at the top of that
file; the checks that genuinely need a live spreadsheet are still listed in `Code.gs`'s own header.

### The colour picker and the gallery

Two modules exist only to make a raw column value pickable by eye, and both sit behind `pickers.js`.

`src/colorpicker.js` is the popover for the RGBA columns: a saturation/value square, a hue strip,
and — the reason it exists rather than an `<input type="color">` — a **blend** strip for the alpha
channel. That channel is not opacity. `Scripts/UI/Icon.cs` does `mix(sprite.rgb, tint.rgb, tint.a)`,
so alpha is how far the sprite is dragged towards the tint, and a blend of zero means no tint at all
with the stored r/g/b ignored. The popover paints its previews through `Sprites.applyTint`, so what
it shows is what the client will draw.

`src/gallery.js` is the windowed grid over a sprite bundle: it renders only the rows in view, so
opening `icons` does not pay for 4827 sprites up front. Open it with

    Gallery.open({bundle, bundles, filter, current, onPick, opener})

`bundle` is `'icons' | 'parts' | 'effects'` and selects which key format the gallery parses out of
`bundles` (the `GOOSE_SPRITES` object). `filter` narrows the candidates up front — `{sheet}` for
icons, `{category, locked}` for parts, where `locked` means the caller's category is fixed and the
chooser is not offered. `current` is in the same shape `onPick` reports, so a caller round-trips its
own cells, and `opener` is the Browse button focus returns to on close.

`onPick` reports the pieces of the key rather than the key itself, one shape per bundle:

| bundle    | `onPick` argument   |
| --------- | ------------------- |
| `icons`   | `{sheet, graphic}`  |
| `parts`   | `{category, id}`    |
| `effects` | `{id}`              |

Parts and effects drop the per-clip and per-frame suffix on the way out: a caller picks a part or an
effect, not one of its frames.

Apps Script has no `.js` file type, so build wraps each module into `dist/*.html`, alongside copies
of `schema.js` and the sprite bundles:

    node tools/DataEditor/build.mjs

`dist/` is generated and gitignored — every byte of it derives from checked-in sources.

## Deploying to Apps Script

The editor is a container-bound script, so each spreadsheet has its own script id and needs its own
deployment. Build first, then deploy `dist/` — either `clasp push` or paste the files into the
Apps Script editor. Deploy once per spreadsheet. The sprite bundles change rarely; `schema.js`
changes whenever a column does.

For `clasp`, write a `tools/DataEditor/.clasp.json` pointing at that spreadsheet's script id, with
`dist` as the root:

    {"scriptId": "<this spreadsheet's script id>", "rootDir": "dist"}

It is gitignored, because the script id differs per spreadsheet. There is deliberately no
`.claspignore`: `dist/` is exactly the deployable set by construction, and clasp's default patterns
already push just the manifest and the `.html` files. Note that `.claspignore` patterns are matched
relative to `rootDir`, not to the project directory — a pattern written as `dist/**` would match
nothing here.

`rootDir` is not a convenience. A pushed file is named by its path relative to `rootDir`, so
pushing from the project directory would upload `dist/app` rather than `app`, and every
`include('app')` in `Editor.html` would fail to resolve.

On Arch and Manjaro the `nodejs-google-clasp` package installs the binary as **`gclasp`**, not
`clasp` — the name `clasp` was already taken. Every `clasp` command here is `gclasp` on those
systems; nothing else differs.

First-time setup, once per machine:

    gclasp login                 # browser OAuth, writes ~/.clasprc.json
    gclasp login --status        # confirms which account

`clasp push` also needs the Apps Script API switched on for that Google account, once, at
<https://script.google.com/home/usersettings>. Without it, push fails with a "User has not enabled
the Apps Script API" error that says nothing about where to go.

The script id comes from the spreadsheet, not from clasp: open it, **Extensions → Apps Script**
(which creates the bound project on first use), then **Project Settings → IDs**. With `.clasp.json`
written, a deploy is:

    node tools/DataEditor/build.mjs
    cd tools/DataEditor && gclasp push

`clasp push` uploads the sources. Turning them into a reachable web app is still a one-time manual
step in the Apps Script editor — **Deploy → New deployment → Web app**, execute as *user accessing*,
access *anyone with a Google account*. Later pushes update the code behind that deployment.
