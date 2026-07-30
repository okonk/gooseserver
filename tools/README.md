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

Output as of 2026-07-30, on one machine with a warm cache:

    icons      4827 sprites  2048x3140  98.1% efficient  1.67 MB html
    parts      3261 sprites  2048x6903  96.0% efficient  1.89 MB html
    effects    1989 sprites  2048x3458  94.7% efficient  0.82 MB html
    Total 4.38 MB of HTML in 2.2s

Roughly 4.4 MB combined, comfortably inside the ~10 MB Apps Script project ceiling.

"Efficient" is the share of the atlas's area covered by sprites — the rest is packing waste that
still costs PNG bytes. Around 95% is the accepted floor. **Nothing enforces it**: no test asserts
it on real assets, so it is a number a human reads here. A drop much below 95% usually means a new
outsized sprite is forcing tall, half-empty shelves; worth a look, but it will never fail a build
for you.

### Known upstream asset defects

The tool reports skips grouped by sheet. One group is expected today, and it is a bug in the
client's art, not in the tool. Do not re-investigate:

- **Sheet 421 loses 19 icons.** The manifest lays it out as a 10x10 grid of 32px cells — a 320x320
  sheet — but `sheets/421.png` is 288x288, one row and one column short. The 10 rects at x=288 and
  the 10 at y=288 (sharing a corner) fall outside the image; the other 81 icons are unaffected.

A second group used to sit here — **sheet 4589 had no PNG**, losing the 20 frames that were the
whole of effect 290370. The 2026-07-30 client art dropped effect 290370 altogether, so nothing
references sheet 4589 any more and the tool no longer reports it. Listed only so a reader of the
older bundles knows where that effect went.

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

### Load and save time

The rule is "do not send what nobody reads":

* `readSheet` reads the sheet **once**. The display values exist for `Date` cells alone and no
  schema column is a date, so they are fetched only if the raw scan finds one.
* `writeRow` skips the duplicate-id scan when the posted id equals the one in the loaded snapshot for
  a row that already exists. Editing a name cannot take an id off anybody, and the scan reads the
  whole id column — 4,322 cells on NPC Spawns.
* The publish check fires all 21 reads **concurrently**. Phase two needs every sheet before it can
  validate anything, so ordering them only serialised 21 round trips. Replies land in slots and are
  walked in schema order, so the report is diffable however the network interleaves them.

The page itself is 4.9 MB, and 4.6 MB of that is the three sprite bundles, inlined by `Editor.html`
as base64 and therefore re-served in full on every sidebar open — the browser has no separate
resource to cache. **Fetching the two big ones on demand was tried and reverted.** `sprites-parts`
(1.98 MB) and `sprites-effects` (860 KB) are wanted by only 4 of the 21 sheets, so
`google.script.run.include(...)` per sheet cut the page to 2.17 MB — and it did not make the editor
feel better, which is the measurement that counts. It moved the wait rather than removing it: the
sheets that do draw characters are the ones designers spend their time on, and they traded a slower
page for a slower first record, plus a `Fetching parts art…` state and a second way for art to fail.
Anything tried here next should be measured in the sidebar before it is kept.

Also left undone: the record list still ships whole sheets (a narrow id + name read would cut the
payload ~20x but costs a round trip per record opened, and it touches the local post-save patch, the
id set and the no-reload-after-save behaviour); the list is not windowed (NPC Spawns is 4,322
buttons); and `writeRow` still calls `SpreadsheetApp.flush()` it does not need.

### The colour picker and the gallery

Two modules exist only to make a raw column value pickable by eye. `ColorPicker.control` is reached
from `composites.js` alone — the rgba control and `equipSlotsControl` are its only two call sites —
while the gallery is opened from both `pickers.js` and `composites.js`.

`src/colorpicker.js` is the popover for the RGBA columns: a saturation/value square, a hue strip,
and — the reason it exists rather than an `<input type="color">` — a **blend** strip for the alpha
channel. That channel is not opacity. `Scripts/UI/Icon.cs` in the client does
`mix(sprite.rgb, tint.rgb, tint.a)`, so alpha is how far the sprite is dragged towards the tint, and
a blend of zero means no tint at all. What happens to the r/g/b behind that zero differs by column:
`Equipped.format` discards them, collapsing the slot to `id,*`, so for `equipped_items` the colour is
not stored at all — which is why the popover's own note says so and why `Equipped.isFaithful` flags a
colour parked behind a zero blend. The composites rgba control keeps all four cells instead,
including at a blend of zero, and the game ignores the three it does not blend.

The square and the strip are painted from `hsvToRgb`; the blend strip alone goes through
`Sprites.applyTint`, against a mid-grey stand-in rather than a checkerboard, so the strip and the
sprite previews in the other modules cannot disagree about what a factor of 128 looks like. There are
no sprite previews in the popover itself.

`src/gallery.js` is the windowed grid over a sprite bundle: it builds only the rows in view plus two
rows of overscan, so opening `icons` does not pay for 4,827 sprites up front. Open it with

    Gallery.open({bundle, bundles, filter, current, onPick, opener})

`bundle` is `'icons' | 'parts' | 'effects'` and selects which key format the gallery parses out of
`bundles` (the `GOOSE_SPRITES` object). The three formats, as `Bundles.cs` writes them:

| bundle    | key format            | example      |
| --------- | --------------------- | ------------ |
| `icons`   | `sheet:graphic`       | `104:7`      |
| `parts`   | `category:id:clip`    | `Bodies:12:idle-down` |
| `effects` | `id:frame`            | `31:0`       |

A key with the wrong number of parts is skipped rather than coerced. Parts are deduplicated by
`category:id` across their four clips, and effects are taken at frame `0` only. `filter` narrows the candidates up front — `{sheet}` for
icons, `{category, locked}` for parts, where `locked` means the caller's category is fixed and the
chooser is not offered. `current` is in the same shape `onPick` reports, so a caller round-trips its
own cells, and `opener` is the Browse button focus returns to on close.

`onPick` reports the pieces of the key rather than the key itself, one shape per bundle:

| bundle    | `onPick` argument   |
| --------- | ------------------- |
| `icons`   | `{sheet, graphic}`  |
| `parts`   | `{category, id}`    |
| `effects` | `{id}`              |

Parts and effects drop the `:clip` and `:frame` suffix of their key on the way out: a caller picks a
part or an effect, not one of its frames.

### Character sprites in the form, and the rows a monster body kills

Four columns hold a **character part** id rather than an inventory graphic: `graphic_equip` on Items
and `body_id` / `hair_id` / `face_id` on NPCs and Spell Effects. All four are plain `Int` columns
belonging to no composite, so nothing about the schema says they are art at all —
`Layout.PART_GRAPHICS` is what says so, and `Pickers.partControl` is what gives each one a preview
and a click-to-browse over the `parts` atlas, locked to its own folder.

The table has two shapes, and they are not interchangeable. `{categoryFrom}` means the sprite folder
comes from **another cell** — the same `graphic_equip` id is a helmet or a boot depending on
`item_slot`, through the client's own map in `Appearance.slotFor` — so it is read live and a slot the
client never draws (Ring, Misc) has nothing to browse. `{category}` means the folder is fixed by the
column: a body is always a body. Note `face_id` → **Eyes**, the one folder whose name does not follow
from its column's.

`Layout.MONSTER_BODY` is the other half of the client's appearance rule. `Character.cs` renders a
body of **100 or more** alone — no hair, no face, no equipment, and the server does not even send
equipment for such a row — so for `body_id >= 100` the form hides `face_id`, `hair_id`, the hair tint
and `equipped_items`, live, as the cell is typed. Hiding does not clear: those cells round-trip
verbatim, so changing your mind loses nothing. Saving a row that has **just crossed** into that range
does clear them, which is the only place in the editor that writes a cell the user did not touch — it
fires on the crossing alone (never on a row that was already a monster), says so on the status line,
and re-renders the form afterwards so the equip-slot control cannot write the old equipment back.

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
