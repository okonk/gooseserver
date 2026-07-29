# Game Data Editor — Round 2: usability from first real use

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fold eight findings from the first hands-on session with the editor into the shipped
build: checkbox booleans, previews you can actually see, editable NPC equipment colours, item
equipment previews, a graphic browser, honest composite labels, and a colour picker that is ours
rather than the operating system's.

**Architecture:** Unchanged. Pure-logic modules under `tools/DataEditor/src/`, unit-tested with
`node --test` against `test/fake-dom.js`; `build.mjs` wraps each into `dist/<name>.html` for Apps
Script. Two new modules join the ten: `colorpicker.js` and `gallery.js`.

**Depends on:** `docs/plans/2026-07-27-game-data-editor-part3-editor.md`, complete and deployed.

**Not in scope:** regenerating `schema.js` or the sprite bundles (no descriptor or art change is
needed — every mapping this plan adds is presentation, and `layout.js` is where presentation
lives); animation, facing or non-down poses (the bundle carries only the four down-facing resting
clips, so a facing control means regenerating the bundle first); mount previews.

---

## Decisions taken before writing this

Asked and answered in session, and the tasks below assume them:

| Question | Decision |
|---|---|
| Boolean checkbox and the blank cell | **Tri-state.** Blank renders `indeterminate` and stays blank on save until clicked. |
| Colour picker depth | **Full popover** — SV square, hue strip, alpha strip, hex, R/G/B/A fields, recent colours. |
| Graphic browser contents | **Whole bundle, filterable, drawn untinted.** |
| Room for a 4× character preview | **Widen the preview column to 400px.** |

---

## APIs verified

Verified for this round, in the client checkout at `/home/hayden/code/Goose2ClientGodot` and in
`Goose/`. Everything Part 3 verified still holds and is not repeated.

| Fact | Location |
|---|---|
| An item's `graphic_r/g/b/a` tints the **inventory icon** | `Scripts/UI/ItemSlot.cs:37` → `Icon.Apply(_icon, GraphicFile, GraphicId, GraphicR, …)` |
| The same four values tint the **equipped sprite**, spliced into `equipped_items` | `Goose/Inventory.cs:700-712` — `GraphicEquipped + "," + GraphicR + …`, and `id + ",*"` when `GraphicA == 0` |
| `graphic_equip` is the id the character wears; `graphic_tile` is the inventory icon | `Goose/ItemHandler.cs:101`, `Goose/Inventory.cs:704` |
| `item_slot` → equip slot | `Goose/Inventory.cs:602-655` `ItemSlotToEquipSlot`: Helmet→Head, Chest→Chest, Pants→Legs, Shoes→Feet, Shield→Shield, OneHanded/TwoHanded→Weapon, Mount→Mount |
| Only six equip slots reach the character | `Goose/Inventory.cs:692` — `EquippedDisplay` sends Chest, Head, Legs, Feet, Shield, Weapon. Ring, Necklace, Pauldrons, Cloak, Belt, Gloves and Misc are **invisible**. |
| An `Enum` cell holds the member **name**, not its number | `CsvToSql.Core/Schema/DescriptorTransform.cs:24` — `Enum.Parse(EnumType, value)` on import. So `item_slot` reads as `"Helmet"`, `"OneHanded"`, and Task 6 maps by name; `Misc = 20` in `ItemsCsvToSql.cs:106` never reaches the editor as a number. |
| Bundle shapes, from the committed files | icons 4,827 sprites over 125 sheet files, median 32×32, max 128×128; parts 3,261 over 8 categories (`Bodies, Chest, Eyes, Feet, Hair, Hands, Helms, Legs`) × 4 clips; effects 560 ids, 2,412 frames |

---

## Task 0: Extend the fake DOM

Four later tasks need behaviour `test/fake-dom.js` does not model. Doing it once up front keeps
each task's diff to its own module.

**Add, each with a test in a new `test/fake-dom.test.js` asserting the modelled behaviour:**

1. `input.indeterminate` — a plain boolean property, independent of `.checked`, cleared by a user
   click in a real browser. Model the property; the *clearing* is the control's job, not the DOM's.
2. `element.getBoundingClientRect()` — returns a rect from assignable `__rect` test state,
   defaulting to all zeroes. The colour picker's pointer maths is a pure function taking a rect, so
   this only needs to be settable, not simulated.
3. `element.style` — a plain object whose keys round-trip. The gallery sets `style.height` and
   `style.backgroundPosition`; today assigning `.style.x` silently does nothing and no test can see
   the difference between a positioned tile and an unpositioned one.
4. `element.scrollTop` / `clientHeight` / `scrollHeight` — assignable numbers, plus a `scroll`
   event that dispatches like the existing model. The gallery's windowing reads these.
5. `canvas.getContext('2d')` stub gains `setTransform` and `imageSmoothingEnabled`, recorded in the
   existing call log, so scale can be asserted.

**Verify:** `node --test tools/DataEditor/test/` — all existing suites still pass untouched.

---

## Task 1: Honest labels for composite fields

**The bug the user found:** the tint control on Items is labelled `graphic_r`. So is NPCs' body
tint (`body_r`) and hair tint (`hair_r`), and Spell Effects' two.

**Root cause:** `src/forms.js:161` labels a composite with its *leader column's* name — the first
column the composite claims, which for `Rgba` is the red channel. The label was never wrong for
`Bitmask`, `IdList` or `EquipSlots`, which claim exactly one column each; it has been wrong for
every `Rgba` and misleading for every `Graphic` since the day they shipped.

**Design:** the label is presentation, so it goes in `layout.js` next to `LAYOUTS` — not into the
descriptors, and not into `schema.js`, which is generated.

**Step 1 — `Layout.labelFor(comp)`**

```js
// Rgba:       ['graphic_r','graphic_g','graphic_b','graphic_a'] -> 'graphic tint'
// Graphic:    ['graphic_tile','graphic_file']                   -> 'graphic_tile + sheet'
// Bitmask/IdList/EquipSlots: the single column's own name, unchanged.
```

Derive the `Rgba` prefix by stripping the shared leading text of the four names and the trailing
`_`; do not hardcode `graphic`/`body`/`hair`. A composite whose columns share no prefix falls back
to the leader name, so a future `Rgba` cannot produce a label like ` tint`.

**Step 2 — sub-labels inside the control.** `Composites.rgbaControl` gains a `.hint` line naming
the four columns it writes, and the blend slider gets its own inline label carrying the alpha
column's name:

```
graphic tint   [swatch]  graphic_a [====|====] 128 / 255 blend
               graphic_r graphic_g graphic_b graphic_a
```

That answers both halves of the complaint: the field is a tint, and the slider is `graphic_a`.

**Step 3 — `forms.js` uses it.** `row.appendChild(comp ? el('label', null, Layout.labelFor(comp)) …)`.

**Tests:** `layout.test.js` — every composite in `schema.js`, all thirteen, produces a label that
is non-empty, contains no `_r`/`_g`/`_b` suffix for `Rgba`, and names only columns that exist.
`forms.test.js` — an `Rgba` field's `<label>` text is `graphic tint`, and the four column names
still appear somewhere in the field so a designer can find the cell in the sheet.

---

## Task 2: Tri-state checkbox for `Bool`

**Step 1 — the control.** `Forms.scalarControl`'s `Bool` branch returns a `<span class="boolean">`
holding:

- `<input type="checkbox" id="f-<name>">` — **no `name`**, so `Forms.collect`'s `[name]` sweep
  never sees it.
- `<input type="hidden" name="<name>">` seeded with `str(value)` **verbatim**, exactly as the
  composite controls seed theirs. This is the cell.
- a `clear` button (`×`, `title="use the default"`), rendered only when the column is not
  `required`.

State mapping, and the rule that makes it safe:

| stored | checkbox | hidden | on save |
|---|---|---|---|
| `''` | `indeterminate` | `''` | writes nothing — the SQL default stands |
| `'0'` | unchecked | `'0'` | `0` |
| `'1'` | checked | `'1'` | `1` |
| anything else | *falls back to a text input* | the stored text | reported by `Validation` |

The fallback is the same shape as `preserveUnknownValue` and exists for the same reason: a cell
holding `2` must round-trip and be reported, not be silently normalised to checked. Render the
text input plus a `.status.bad` reading `not a 0/1 value`.

**Step 2 — the listener.** On `change`: clear `indeterminate`, write `'1'`/`'0'` into the hidden
input, and dispatch nothing extra — `change` bubbles, so `app.js`'s delegated preview refresh
already sees it. The clear button sets `indeterminate = true`, `checked = false`, hidden `= ''`,
and dispatches a `change` so the preview follows.

**Why tri-state is load-bearing:** `save()` writes a cell only when `Validation.validateCell`
returns `write` (`app.js:676`), and blank means "use the SQL default"
(`CsvToSql.Core/CsvToSqlBase.cs:27`). A two-state checkbox would write `0` into every blank boolean
on every save — the exact class of silent rewrite commit `cfa8f3a` exists to prevent. Items has
four blank-defaulted booleans (`lore`, `bindonpickup`, `bindonequip`, `event`); NPCs has five.

**Tests:** `forms.test.js` — each of the four stored states renders and collects back the value it
was given, unchanged, with no interaction; a click on a blank box collects `'1'`; a second click
collects `'0'`; the clear button returns it to `''`; an unknown value survives a render/collect
round trip. `app.test.js` — opening a record with blank booleans and saving writes `null` for them,
i.e. the existing "does not rewrite untouched cells" assertion extended to the new control.

---

## Task 3: Previews you can see

**The complaint:** the 48px icon preview and the 96×112 character preview are too small to judge.

**Design — scale the context, not the maths.** Every preview keeps its logical size and its
existing arithmetic; only the canvas backing store grows and the context takes a
`setTransform(scale, 0, 0, scale, 0, 0)` before anything is drawn. Nothing about anchoring,
centring or `Sprites.draw` changes, so Part 3's preview tests stay valid as written.

| preview | logical | scale | on screen | where |
|---|---|---|---|---|
| graphic composite icon | 64×64 (was 48) | 2 | 128×128 | `Pickers.graphicControl` |
| equip slot part | 40×56 | 2 | 80×112 | `Composites.equipSlotsControl` |
| character | 96×112 | 4 | 384×448 | `Preview.character` |
| effect | 96×96 | 2 | 192×192 | `Preview.effect` |

The icon box grows from 48 to 64 logical pixels as well as scaling: the median icon is 32×32 but
the bundle holds sprites up to 128×128, and 48 clips them today. 64 covers the common large sizes;
anything bigger is still clipped, which is a bundle fact, not a regression — say so in a comment
rather than growing the box to 128 and leaving every 32px icon adrift in whitespace.

**Step 1** — `Preview.character(canvas, appearance, ctx, scale)` and
`Preview.effect(canvas, effectId, ctx, scale)`, `scale` defaulting to 1. Both call
`c.setTransform(scale, 0, 0, scale, 0, 0)` immediately after `getContext`, and both keep
`imageSmoothingEnabled = false` — a scaled context resamples by default, and a blurry 4× sprite
would be worse than the small sharp one.

**Step 2** — `app.js:renderPreviews` builds canvases at `CANVAS_W * 4` / `CANVAS_H * 4` and passes
4; the effect canvas at `EFFECT_SIZE * 2` and passes 2. Export `Preview.CHARACTER_SCALE = 4` and
`Preview.EFFECT_SCALE = 2` so the two places that need the number agree.

**Step 3 — room for it.** `Editor.html`: `main { grid-template-columns: 200px 1fr 400px; }`. The
`max-width: 700px` single-column fallback (which is what the Sheets sidebar gets) is unchanged; add
`#previews canvas { max-width: 100%; height: auto; }` so a 384px canvas in a 300px sidebar shrinks
rather than overflowing.

**Tests:** `preview.test.js` — `setTransform` is recorded with the scale passed, `drawImage`
destinations are unchanged from the unscaled case (the transform does the work, not the maths), and
`imageSmoothingEnabled` is false. `pickers.test.js` — the icon canvas is 128×128 with a logical box
of 64.

---

## Task 4: Our own colour picker — `src/colorpicker.js`

**Why:** the native `<input type="color">` opens the OS picker, which on this user's desktop is
slow and modal. Three controls need a colour: the `Rgba` composite (Task 1's field), the six new
equipment tints (Task 5), and nothing else yet — but all three want the *blend alpha* in the same
popover, which no native control offers.

**Public shape:**

```js
ColorPicker.control({ r, g, b, a, withAlpha, onChange })  // -> { node, set(rgba) }
```

`node` is a swatch `<button>`; clicking or pressing Enter/Space opens a popover anchored under it.
`onChange({r,g,b,a})` fires on every live movement, so the caller writes cells and redraws previews
exactly as it does today with `input`.

**Layout:**

```
┌───────────────┬─┬─┐
│               │ │ │   hex  #a4331f
│   SV square   │H│A│   R 164   G  51
│               │ │ │   B  31   A 128
└───────────────┴─┴─┘   ┌──────────────┐
recent ■ ■ ■ ■ ■ ■      │ 128/255 blend│
                        └──────────────┘
```

- **SV square** and **hue strip**: `<canvas>` painted once per hue change. Not CSS gradients —
  the same nearest-neighbour canvas the rest of the editor uses, and pixel-exact under test.
- **Alpha strip** is labelled **blend**, not opacity, and its readout says
  `128 / 255 blend`, matching what `Icon.cs:9-11` actually does. Rendered only when
  `withAlpha`. Include the standing note: **alpha 0 means no tint at all, and the colour is not
  stored** — `Equipped.format` collapses a zero-alpha slot to `id,*` and `Composites.rgbaControl`
  keeps r/g/b but the game ignores them.
- **Recent colours**: last eight distinct colours chosen this session, in memory only. Not
  `localStorage` — the editor also runs inside a Sheets sidebar iframe, where storage access can
  throw, and a picker that dies on a storage exception is worse than one that forgets.

**Pure functions, tested without a DOM:** `rgbToHsv`, `hsvToRgb`, `parseHex`, `formatHex`, and
`pointToValue(rect, clientX, clientY)` → `{x: 0..1, y: 0..1}` clamped. The pointer handlers do
nothing but call `pointToValue` with `getBoundingClientRect()`, which is why Task 0 only has to make
that rect settable.

**Round-trip rule, and it needs a test:** `rgbToHsv` → `hsvToRgb` must return the original bytes for
all 16.7M combinations of interest — test the 4,096 combinations on a 16-step lattice plus every
greyscale value, where hue is undefined and a naive implementation drifts.

**Keyboard and a11y:** arrow keys move the SV cursor by 1 (shift: 10), the hue and alpha strips by
1/10; `Escape` closes and restores focus to the swatch; `Enter` closes; the popover is
`role="dialog"` with `aria-label`, the swatch is `aria-haspopup="dialog"` `aria-expanded`, and the
hex field is a real text input so a value can be typed or pasted. Clicking outside closes — using
the same cancelled-`mousedown` technique `Pickers.fkControl` uses, for the reason stated there.

**Step — swap it in.** `Composites.rgbaControl` replaces `<input type="color">` with
`ColorPicker.control({..., withAlpha: true})` and drops the separate range slider; `sync()` becomes
the `onChange` body. The rule that an untouched control never writes its cells is unchanged and its
existing test must still pass.

**Include order:** `colorpicker` before `composites` in `Editor.html`.

---

## Task 5: NPC equipment colours

**The complaint:** there is no way to change an NPC's equipment colours. There is not — 
`equipSlotsControl` renders a graphic field per slot and nothing else, while `equipped_items`
carries `r,g,b,a` per slot and `Equipped.parse` has been reading them all along.

**Step 1** — each of the six slot rows gains a `ColorPicker.control({withAlpha: true})` seeded from
`slots[index]`. Its `onChange` writes `slots[index].r/g/b/a`, sets
`slots[index].tinted = a > 0`, then calls the existing `sync()` and `redraw()`.

`tinted` tracking alpha is what keeps `Equipped.format` honest: it emits the compact `id,*` form for
a zero-alpha slot, so setting alpha to 0 discards the colour. The slot's status line says so the
moment alpha reaches 0 — `colour not stored while blend is 0` — rather than letting a designer pick
a colour, park the alpha and find it gone tomorrow.

**Step 2 — the row grows.** `.equip-slot` becomes
`grid-template-columns: 60px 1fr 28px 84px auto` (label, graphic, swatch, preview, status).

**What does not change, and must be re-asserted:** the freeze gate (`wrap.__frozen`) still covers
only the graphic fields — a colour cannot be typo'd, since the picker cannot emit a non-byte — and
the unfaithful-row gate in `app.js:572` still refuses any edit, colour included, to one of the five
malformed shipped rows. Add a test for the second: setting a colour on an unfaithful row is refused
with the same message a graphic edit gets.

**Tests:** `composites.test.js` — a colour change writes the five-token form
(`12,164,51,31,128`); dropping alpha to 0 rewrites that slot to `12,*`; the preview redraw is
called with the slot as its tint; an untouched control still writes its stored string verbatim.

---

## Task 6: Item equipment preview, and tint that reaches the tile

Two complaints, one root: `graphic_equip` has no preview, and `graphic_r/g/b/a` visibly affects
nothing in the editor even though the game applies it to *both* the tile and the worn sprite.

**Step 1 — the cross-field problem.** `Pickers.graphicControl` redraws from its own two inputs
only, so it cannot know about `graphic_r`. Give `ctx` a second registry, next to `onImagesReady`:

```js
ctx.onFormChange(fn)   // fn(values) — called from app.js's delegated input/change listener
```

`app.js:init`'s existing delegated listener already collects the form on every edit for
`refreshPreviews`; pass those same `values` to each registered callback and clear the registry in
`renderForm` exactly as `imageCallbacks` is cleared. One collection per edit, two consumers — not a
second traversal.

**Step 2 — which columns tint which graphic.** Presentation again, so `layout.js`:

```js
var TINTS = { Items: { graphic_tile: ['graphic_r','graphic_g','graphic_b','graphic_a'],
                       graphic_equip: ['graphic_r','graphic_g','graphic_b','graphic_a'] } };
```

Only Items. Spells' `spellbook_graphic` and Spell Effects' four graphics have no tint columns in
their sheets, and inventing one would tint a preview the game draws plain. A `layout.test.js` check
asserts every name in `TINTS` exists in that sheet's schema, in both directions for the tint
columns.

**Step 3 — `graphic_tile` preview tints.** `graphicControl` takes the tint columns, registers an
`onFormChange` redraw, and passes `{r,g,b,a}` to `Sprites.draw`, which already blends exactly as
`Icon.cs` does. Nothing else about the control moves.

**Step 4 — `graphic_equip` gets a control.** It is a plain `Int` column with no composite, so route
it in `forms.js` the way `ref` columns are routed to `Pickers.fkControl`: a `Layout.partGraphic(sheet, column)`
lookup returns `{ categoryFrom: 'item_slot' }`, and `Pickers.partControl(column, values, ctx, spec)`
renders the id field, an 80×112 preview and a status line.

The category comes from `item_slot`, live, via `onFormChange`:

| `item_slot` | category | note |
|---|---|---|
| Helmet | `Helms` | |
| Chest | `Chest` | |
| Pants | `Legs` | |
| Shoes | `Feet` | |
| Shield, OneHanded, TwoHanded | `Hands` | shield and weapon share the folder |
| Mount | `Bodies` | mounted clip; `Sprites.mount` already exists and has had no caller until now |
| Ring, Necklace, Pauldrons, Cloak, Belt, Gloves, Misc | *none* | status: `this slot is not drawn on the character` |

Source: `Goose/Inventory.cs:602-655` for the slot map and `Goose/Inventory.cs:692` for which six
reach `EquippedDisplay`. Match on the **enum member name** — the cell holds `"Helmet"`, not `0`
(`DescriptorTransform.cs:24`) — and treat an empty or unrecognised `item_slot` as "not drawn"
rather than guessing a category. `Preview.isArmed(values.body_state)` picks the clip, the same rule
`equipSlotsControl` uses.

**Step 5 — the Items preview panel.** `renderPreviews` currently draws a character only when the
sheet has `body_id`, which Items does not. Add an Items branch: two canvases side by side,

- **inventory icon** — `graphic_tile`/`graphic_file` at 4×, tinted;
- **worn** — `graphic_equip` in its `item_slot` category, tinted, drawn *over body 1* in the correct
  draw order so a helmet is not a shape floating in space. Body 1 is the shipped player body
  (`CharacterLayout.cs:56-69` gives it underwear, which `Appearance.layers` already handles).

Both read from the live form, so `previewKey` grows `graphic_tile`, `graphic_file`, `graphic_equip`,
`graphic_r/g/b/a`, `item_slot` and `body_state`. The key is what stops a keystroke in
`item_description` from rebuilding two canvases 40 times a second.

**Tests:** `pickers.test.js` — `partControl` resolves each of the seven mapped slots to its
category and reports the seven unmapped ones; a tint edit redraws the tile with the new tint;
`app.test.js` — the Items preview appears for an Items record and not for a Quests one, and
`previewKey` changes for each of the new columns and for none of the old unrelated ones.

---

## Task 7: The graphic browser — `src/gallery.js`

**The complaint:** picking a graphic means knowing its number. There are 4,827 icons.

**Public shape:**

```js
Gallery.open({ bundle, filter, current, onPick })   // bundle: 'icons' | 'parts' | 'effects'
```

`onPick` receives the identifying parts of the key, so the caller writes whatever cells it owns:
icons give `{ sheet, graphic }` — which is why picking from the gallery fills **both** cells of a
`Graphic` composite and makes "graphic and sheet must both be set" hard to trip; parts give
`{ category, id }`; effects give `{ id }`.

**Rendering — CSS atlas tiles, not canvases.** Each tile is a `<button>` with the bundle PNG as
`background-image`, `background-position: -x -y`, the rect's width/height, and
`image-rendering: pixelated` at 2×. The browser composites them; no per-tile canvas, no per-tile
`drawImage`. Drawn **untinted**, as decided.

**Windowing is not optional.** 4,827 tiles in the DOM is a hang inside an Apps Script iframe.
Render only the rows intersecting the viewport plus two rows of overscan, with a spacer div above
and below carrying the remaining height. The row height is uniform per bundle — take it from the
tallest rect in the current filter — so the row a scroll offset lands on is arithmetic, not a
measurement.

**Filters, per bundle:**

- **icons** — a sheet chooser listing the 125 sheet files with counts, plus an id search.
  Default the chooser to the sheet already in the record's `graphic_file`, so opening the browser
  from an item shows that item's neighbourhood rather than sheet `104`.
- **parts** — the 8 categories; preselected to the caller's category (from `item_slot` or the equip
  slot name) and locked when the caller supplies one, since picking a Helms sprite for a Feet slot
  is never right. Deduplicated by id across the four clips, showing the clip
  `Sprites.clipCandidates` would pick.
- **effects** — 560 ids, frame 0 as the tile.

**Keyboard and a11y:** `role="dialog"` `aria-modal="true"` with a labelled heading; grid navigation
with arrows/Home/End/PageUp/PageDown, `Enter` picks, `Escape` closes; focus moves into the search
field on open and returns to the invoking Browse button on close; the current graphic's tile is
`aria-selected` and scrolled into view on open.

**Wiring — a Browse button beside each graphic field:**

| field | bundle | filter |
|---|---|---|
| `graphic_tile` + `graphic_file` (Items), `spellbook_graphic` (Spells), `buff_graphic` (Spell Effects) | icons | current sheet |
| `graphic_equip` (Items) | parts | category from `item_slot`, locked |
| each of the six equip slots (NPCs) | parts | `Appearance.CATEGORY[slotName]`, locked |
| `spell_animation` (Spell Effects) | effects | — |

**Markup:** one `<div id="modal" hidden>` in `Editor.html`, above the sticky header's z-index.
`Gallery.open` owns it and must leave it empty on close — a modal holding 4,827 stale nodes behind
`hidden` costs the same memory as one on screen.

**Tests:** `gallery.test.js` — the icon index groups 4,827 keys into 125 sheets with correct counts;
search by id narrows to matching graphics; windowing renders a bounded tile count and the right
slice for a given `scrollTop`; a pick reports `{sheet, graphic}` and closes; `Escape` closes without
picking; the parts filter locked to a category never lists another one; opening twice leaves no
duplicate nodes. `pickers.test.js`/`composites.test.js` — the Browse button writes both cells and
triggers a redraw.

---

## Task 8: Build, deploy, smoke test, document

1. `node tools/DataEditor/build.mjs` — confirm `dist/colorpicker.html` and `dist/gallery.html` exist
   and `Editor.html`'s include list carries both, in dependency order
   (`colorpicker` → `composites`; `gallery` → `pickers`).
2. `node --test tools/DataEditor/test/` — everything green, including every Part 3 test unmodified
   except where a task above says otherwise.
3. `dotnet test tools/Tools.Tests` — `Checked_in_schema_js_is_up_to_date` and the bundle artifact
   tests must still pass untouched; this round regenerates neither.
4. `clasp push` from `tools/DataEditor/dist`, then smoke-test **in the deployed web app and in the
   Sheets sidebar**, since the 400px preview column and the modal behave differently in a 300px
   iframe:
   - Items: toggle `lore` from blank → checked → blank; confirm a save with it blank writes nothing.
   - Items: browse for a `graphic_tile`, confirm both cells fill; change the tint and watch both the
     tile and the worn preview follow.
   - NPCs: colour an equipment slot, confirm the character preview updates and the saved
     `equipped_items` holds the five-token slot.
   - Spell Effects: confirm the two graphic fields still gate a save independently (Part 3's
     `__graphicError`).
5. Update `tools/README.md` with the two new modules and the gallery's key formats.

---

## Definition of done

- [ ] Booleans are tri-state checkboxes; a blank boolean survives an open-and-save untouched.
- [ ] Composite fields are labelled by what they are; no field on any sheet is labelled `*_r`.
- [ ] Icon previews are 128px, equip slots 80×112, the character 384×448, effects 192px.
- [ ] The preview column is 400px and still collapses for the Sheets sidebar.
- [ ] Every colour is chosen in our popover; the OS picker never opens.
- [ ] Each of an NPC's six equipment slots has an editable colour that reaches `equipped_items`.
- [ ] An item's tint is visible on both its inventory tile and its worn sprite, live.
- [ ] `graphic_equip` has a preview whose category follows `item_slot`.
- [ ] A Browse button opens a filterable, keyboard-navigable browser for icons, parts and effects,
      and picking an icon fills both the graphic and the sheet cell.
- [ ] `node --test` green; `dotnet test tools/Tools.Tests` green; `schema.js` and the three
      `sprites-*.html` byte-identical to what is checked in.

---

## Known limitations, by design

- **No facing, no animation, no walk cycle.** The committed bundle carries four down-facing resting
  clips and nothing else; any of those means regenerating the bundle first.
- **Icons above 128px are still clipped** by the 64-logical-pixel preview box. Widening the box to
  fit the largest sprite in the bundle would strand every 32px icon in whitespace.
- **A zero blend alpha still discards the colour** on equipment slots, because that is what
  `Equipped.format` writes and what the wire format says. The picker warns; it does not work around
  it.
- **Recent colours do not survive a reload.** In-memory by choice — a Sheets sidebar iframe can
  refuse storage access, and a picker that throws is worse than one that forgets.
- **Ring, Necklace, Pauldrons, Cloak, Belt, Gloves and Misc items get no worn preview.** Not an
  omission: `EquippedDisplay` never sends them, so the game does not draw them either.
