# Generators

Both tools produce inputs for the Apps Script data editor in `tools/DataEditor/`. Their outputs
(`schema.js` and the three `sprites-*.html`) are **checked in**, so the editor front end can be
developed without a client checkout. Both are byte-reproducible: regenerating without changing
any input leaves the working tree clean.

## SchemaGen

Emits `schema.js` from the column descriptors in `CsvToSql.Core`. Run after adding or changing
any column:

    dotnet run --project tools/SchemaGen -- tools/DataEditor/schema.js

    Wrote .../tools/DataEditor/schema.js (69,483 bytes, 21 sheets)

`Checked_in_schema_js_is_up_to_date` fails if you forget.

## SpriteBundle

Emits three sprite atlases as inlined HTML. Needs the client repo checked out. Run when the
client's art changes:

    dotnet run --project tools/SpriteBundle -- \
      /home/hayden/code/Goose2ClientGodot/Assets/Sprites tools/DataEditor

Give the client an **absolute path**, or one relative to your shell's cwd. A `../Goose2ClientGodot`
style path does not work from a git worktree, where the repo root is `.worktrees/<branch>/` and the
sibling checkout is two levels further up.

Current output (2026-07-29):

    icons      4827 sprites  2048x3140  98.1% efficient  1.67 MB html
    parts      3261 sprites  2048x6903  96.0% efficient  1.89 MB html
    effects    2412 sprites  2048x4097  95.3% efficient  0.83 MB html
    Total 4.39 MB of HTML in 2.3s

4.39 MB combined, comfortably inside the ~10 MB Apps Script project ceiling.

### Known upstream asset defects

The tool reports skips grouped by sheet. Two groups are expected today, and both are bugs in the
client's art, not in the tool. Do not re-investigate:

- **Sheet 421 loses 19 icons.** The manifest's rects for it start at x=288, but `sheets/421.png`
  is only 288x288, so every one of them lies entirely outside the image.
- **Sheet 4589 has no PNG at all.** Its 20 frames are the whole of effect 290370, so that effect
  is absent from `sprites-effects.html` entirely.

### Adding graphics the data has not referenced yet

`tools/SpriteBundle/sheets.json` lists which sheets go into the icon bundle. It was seeded from the
sheets the two datasets reference, so a sheet nobody has used will not appear and its graphics
cannot be picked in the editor. To add one, put its number in `iconSheets` and regenerate.

To re-derive the list from built databases, pass **every** dataset in one invocation — the tool
unions them itself:

    dotnet run --project tools/SpriteBundle -- derive-sheets \
      Goose/bin/Debug/IllutiaGoose.db \
      /home/hayden/code/illutiagooseserver/Goose/bin/Debug/AsperetaGoose.db

    Goose/bin/Debug/IllutiaGoose.db: 113 sheets
    .../AsperetaGoose.db: 22 sheets
    union: 125 sheets

Counts go to stderr; stdout is the `iconSheets` array body, indented and wrapped, ready to paste
over the existing one unedited. Running it against a single database and pasting that output would
silently drop the other dataset's sheets — the checked-in list covers both Illutia and Aspereta.

## Tests

    GOOSE_CLIENT_ASSETS=/home/hayden/code/Goose2ClientGodot/Assets/Sprites \
      dotnet test tools/Tools.Tests

`GOOSE_CLIENT_ASSETS` points the asset-gated tests at the client. Without it (and without a sibling
checkout at the default location) 20 of the 124 tests **skip** — they assert nothing, and a green
run proves nothing about the sprite pipeline. A `GOOSE_CLIENT_ASSETS` that is set but wrong fails
loudly rather than skipping, so a typo cannot masquerade as a checkout-less machine.

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
atlas as one ~1.6 MB base64 line, which cannot be merged textually. A conflict there is resolved by
regenerating the bundles, never by hand-editing them.

## Deploying to Apps Script

The editor is a container-bound script, so each spreadsheet has its own script id and needs its own
deployment. Paste `schema.js` and the three `sprites-*.html` files into the Apps Script editor, or
push with `clasp`. The sprite bundles change rarely; `schema.js` changes whenever a column does.
