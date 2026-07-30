import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

// Editor.html is the one file nothing else can test: it is only ever evaluated by Apps Script.
// The failure mode it invites is silent — a module added to src/ but not to the include list
// simply never loads, and the first symptom is a ReferenceError in a deployed sidebar.

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');
const html = readFileSync(join(root, 'Editor.html'), 'utf8');

const included = [...html.matchAll(/include\('([^']+)'\)/g)].map((m) => m[1]);

test('every src module is included, and every include exists', () => {
  const modules = readdirSync(join(root, 'src'))
    .filter((f) => f.endsWith('.js'))
    .map((f) => f.replace(/\.js$/, ''));

  const missing = modules.filter((m) => !included.includes(m));
  assert.deepEqual(missing, [], 'src modules absent from Editor.html — they would never load');

  // The generated files build.mjs copies through alongside the wrapped modules.
  const generated = ['schema', 'sprites-icons', 'sprites-parts', 'sprites-effects'];
  const stale = included.filter((n) => !modules.includes(n) && !generated.includes(n));
  assert.deepEqual(stale, [], 'Editor.html includes a file build.mjs does not emit');
});

test('no module is included twice', () => {
  assert.equal(new Set(included).size, included.length);
});

test('includes are ordered so a module never precedes the data it reads at load time', () => {
  // Modules resolve each other as free globals at CALL time, so their relative order is free.
  // The generated data files are different: they assign GOOSE_SCHEMA and GOOSE_SPRITES at
  // parse time, and App.init() reads GOOSE_SCHEMA synchronously, so both must precede app.
  assert.ok(included.indexOf('schema') < included.indexOf('app'));
  assert.ok(included.indexOf('sprites-icons') < included.indexOf('app'));
});

test('every element id app.js looks up exists in the markup', () => {
  const app = readFileSync(join(root, 'src', 'app.js'), 'utf8');
  const ids = [...app.matchAll(/getElementById\('([^']+)'\)/g)].map((m) => m[1]);
  assert.ok(ids.length >= 9, 'expected app.js to look up the shell elements');

  const declared = new Set([...html.matchAll(/id="([^"]+)"/g)].map((m) => m[1]));
  const absent = [...new Set(ids)].filter((id) => !declared.has(id));
  assert.deepEqual(absent, [], 'app.js reads an element Editor.html does not declare');
});

test('the colour popover fits the narrowest column it ships in', () => {
  // The shipped Sheets sidebar is ~300px; after the page padding and a section's padding and
  // border the form column is ~258. A popover laid out as one flex row is 128 (square) + 14
  // (hue) + 14 (blend) + 120 (fields) + gaps + padding = ~308, which overflows it and turns the
  // whole page into a horizontal scroller. The fields block therefore sits BELOW the canvas
  // grid, as a full-width child of the popover.
  assert.match(html, /\.cp-grid\s*{[^}]*display:\s*grid/);
  assert.doesNotMatch(html, /\.cp-fields\s*{[^}]*grid-column/,
    'the fields block must not live inside the canvas grid — the grid caps its width');
  // Wide enough that the four channel fields hold three digits, capped for anything narrower.
  assert.match(html, /\.cp-pop\s*{[^}]*min-width:\s*236px/);
  assert.match(html, /\.cp-pop\s*{[^}]*max-width:/);
  // The number inputs' spinner buttons are hidden — they cost the width of two digits.
  assert.match(html, /\.cp-num::-webkit-inner-spin-button/);
});

test('the right-anchored popover the colour picker can ask for is styled', () => {
  // colorpicker.js sets data-align="right" on the wrapper and never reads it back: if the
  // stylesheet has no rule for it, the option is a no-op that looks implemented.
  const picker = readFileSync(join(root, 'src', 'colorpicker.js'), 'utf8');
  assert.match(picker, /data-align/);
  assert.match(html, /\[data-align="right"\][^{]*\.cp-pop\s*{[^}]*right:\s*0/);
});

// The stylesheet is heavily commented, and a stray `*/` is invisible: the tokens after it become
// part of the NEXT rule's prelude, which makes the prelude an invalid selector list, which per CSS
// error handling drops that whole rule. The regex assertions above cannot see it — they match the
// declaration text whether or not the rule it sits in is live.
const style = html.match(/<style>([\s\S]*?)<\/style>/)[1];

test('every CSS comment in the stylesheet is terminated', () => {
  assert.equal(
    (style.match(/\/\*/g) || []).length,
    (style.match(/\*\//g) || []).length,
    'unbalanced /* and */ in <style> — a stray marker silently kills the following rule',
  );
});

// Comments gone, so a rule swallowed by an unterminated comment simply is not here to match.
const css = style.replace(/\/\*[\s\S]*?\*\//g, '');
// Nested at-rule blocks (@media today, @keyframes or @supports tomorrow) are cut out whole before
// the flat prelude/body regex runs below: that regex cannot parse a nested block (it reads the
// at-rule prelude as a selector and the first inner rule as its body, then self-recovers on the
// stray `}` — which is why the top-level rules AFTER a media query still match today, by accident
// rather than by rule). Removing the blocks with a brace counter makes the parse honest. The
// consequence is that a selector moved INSIDE an @media block stops being found — hence the third
// cause named in every lookup's message.
//
// The prefix test is ANY at-rule, not @media alone: a @keyframes block dropped in above one of the
// rules queried below would otherwise resurrect exactly the mis-parse this helper exists to
// prevent. A statement at-rule with no block (@import, @charset) has no `{` before the next rule's
// one, so it takes its successor's braces with it — none is used here, and adding one would fail
// loudly in the lookups rather than silently.
const AT_RULE = /^@[a-z-]+/;
const stripAtBlocks = (text) => {
  let out = '';
  for (let i = 0; i < text.length;) {
    if (text[i] !== '@' || !AT_RULE.test(text.slice(i, i + 16))) {
      out += text[i]; i += 1; continue;
    }
    const open = text.indexOf('{', i);
    if (open === -1) break;
    let depth = 0;
    let j = open;
    for (; j < text.length; j++) {
      if (text[j] === '{') depth += 1;
      else if (text[j] === '}' && --depth === 0) { j += 1; break; }
    }
    i = j;
  }
  return out;
};
const topLevelCss = stripAtBlocks(css);
const NOT_FOUND = 'unterminated comment above it, a renamed selector, '
  + 'or a selector moved inside an @media block';
const ruleFor = (selector) => {
  const rules = [...topLevelCss.matchAll(/([^{}]+){([^}]*)}/g)];
  return rules.find((r) => r[1].trim() === selector);
};

test('the at-block stripper removes any nested at-rule, not only @media', () => {
  // Directly, because the consequence in the lookups below is invisible until someone adds the
  // second kind of at-rule: a surviving @keyframes block hands the flat regex a nested body and
  // the rule immediately after it stops being found.
  const stripped = stripAtBlocks('@keyframes spin { from { opacity: 0 } to { opacity: 1 } }\n'
    + '.after { color: red }');
  assert.equal(stripped.indexOf('keyframes'), -1);
  assert.equal(stripped.indexOf('opacity'), -1);
  const rules = [...stripped.matchAll(/([^{}]+){([^}]*)}/g)];
  assert.equal(rules.length, 1);
  assert.equal(rules[0][1].trim(), '.after');
});

test('the equipment slot row is a live grid with a track for every cell it holds', () => {
  // Four tracks per slot: label, graphic id, colour swatch, preview canvas — the graphic field
  // opens the browser itself, so there is no Browse track. The status line is the fifth child and
  // has NO track of its own — it spans the whole row underneath.
  const rule = ruleFor('.equip-slot');
  assert.ok(rule, `.equip-slot has no rule of its own — check for an ${NOT_FOUND}`);
  assert.match(rule[2], /display:\s*grid/);
  assert.match(rule[2], /grid-template-columns:\s*60px\s+1fr\s+28px\s+84px/);

  const status = ruleFor('.equip-slot .status');
  assert.ok(status, `.equip-slot .status has no rule — without it the status line takes a sixth `
    + `track that the grid does not declare, and lands in the next row's label cell. Check for `
    + `an ${NOT_FOUND}`);
  assert.match(status[2], /grid-column:\s*1\s*\/\s*-1/);
});

test('the graphic browser is a modal that covers the sticky header', () => {
  const modal = ruleFor('#modal');
  assert.ok(modal, `#modal has no rule of its own — check for an ${NOT_FOUND}`);
  // A dialog under the header is a dialog with its title bar bitten off.
  const header = ruleFor('header');
  const z = (rule) => Number(/z-index:\s*(\d+)/.exec(rule[2])[1]);
  assert.ok(z(modal) > z(header), 'the modal must stack above the sticky header');
  assert.match(modal[2], /position:\s*fixed/);

  // `display: flex` on #modal beats [hidden]'s display:none on equal specificity, so without a
  // rule of its own the browser would be permanently on screen. Nothing else in the suite could
  // see that: gallery.js sets `hidden` correctly either way.
  const off = ruleFor('#modal[hidden]');
  assert.ok(off, `#modal[hidden] has no rule — the modal would never hide. Check for an ${NOT_FOUND}`);
  assert.match(off[2], /display:\s*none/);
});

test('the tile grid matches the arithmetic gallery.js windows with', async () => {
  const { Gallery } = await import('../src/gallery.js');
  const grid = ruleFor('.gal-grid');
  assert.ok(grid, `.gal-grid has no rule of its own — check for an ${NOT_FOUND}`);
  const tile = ruleFor('.gal-tile');
  assert.ok(tile, `.gal-tile has no rule of its own — check for an ${NOT_FOUND}`);

  // The stylesheet and the module have to agree on all three numbers or a scroll offset lands on
  // the wrong row: windowFor() computes the slice from COLUMNS and ROW_HEIGHT, and only the CSS
  // decides where a tile actually goes.
  assert.match(grid[2], new RegExp(`repeat\\(${Gallery.COLUMNS},\\s*${Gallery.CELL}px\\)`));
  assert.match(grid[2], new RegExp(`gap:\\s*${Gallery.GAP}px`));
  assert.match(tile[2], new RegExp(`width:\\s*${Gallery.CELL}px`));
  assert.match(tile[2], new RegExp(`height:\\s*${Gallery.CELL}px`));
  assert.equal(Gallery.ROW_HEIGHT, Gallery.CELL + Gallery.GAP);
  // The tile centres an art element sized to exactly the sprite's rect, so the padding around a
  // small sprite never shows the atlas's neighbouring sprites.
  assert.match(tile[2], /display:\s*flex/);
  const art = ruleFor('.gal-art');
  assert.ok(art, `.gal-art has no rule of its own — check for an ${NOT_FOUND}`);
  // Pixel art, blown up. A smoothed tile is a blurry one.
  assert.match(art[2], /image-rendering:\s*pixelated/);
  // background-repeat, because a browser could otherwise tile the atlas across the element.
  assert.match(art[2], /background-repeat:\s*no-repeat/);
});

test('the gallery scroller opts out of scroll anchoring', () => {
  // The grid is windowed: every scroll rebuilds tiles and resizes the spacers, and the browser's
  // scroll anchoring reads those mutations as content shifting and "corrects" scrollTop — the
  // wheel-scroll-jumps-to-the-bottom bug. Nothing but this rule prevents it.
  const rule = ruleFor('.gal-scroll');
  assert.ok(rule, `.gal-scroll has no rule of its own — check for an ${NOT_FOUND}`);
  assert.match(rule[2], /overflow-anchor:\s*none/);
  assert.match(rule[2], /user-select:\s*none/);
});

test('the selected tile is visibly marked', () => {
  // gallery.js sets aria-selected and nothing else: with no rule for it a sighted keyboard user
  // has no idea which tile Enter would pick.
  const rule = ruleFor('.gal-tile[aria-selected="true"]');
  assert.ok(rule, `.gal-tile[aria-selected="true"] has no rule — check for an ${NOT_FOUND}`);
  assert.match(rule[2], /border-color|box-shadow|outline/);
});

test('the clickable graphic fields the controls emit are styled', () => {
  // pickers.js and composites.js mark every gallery-opening field with data-browse; the pointer
  // cursor is the one visual cue that a click does more than place a caret.
  const rule = ruleFor('input[data-browse]');
  assert.ok(rule, `input[data-browse] has no rule of its own — check for an ${NOT_FOUND}`);
  assert.match(rule[2], /cursor:\s*pointer/);
  const pickers = readFileSync(join(root, 'src', 'pickers.js'), 'utf8');
  assert.match(pickers, /data-browse/);
});

test('the two Items canvases are laid out as a row, not stacked with baseline gaps', () => {
  // #previews holds two canvases on Items and the pair IS the point. With no display they are
  // inline-level and 256 + 384 overflows the column into a stacked pair.
  const rule = ruleFor('#previews');
  assert.ok(rule, `#previews has no rule of its own — check for an ${NOT_FOUND}`);
  assert.match(rule[2], /display:\s*flex/);
  assert.match(rule[2], /flex-wrap:\s*wrap/);
});

test('both Items preview canvases are styled by class name', () => {
  // The classes app.js gives these two canvases are the only thing joining them to the pixelated
  // rendering and the checkerboard backdrop. A typo in either name leaves that canvas looking like
  // a plain smoothed image, which nothing else in this suite would notice.
  const rule = [...topLevelCss.matchAll(/([^{}]+){([^}]*)}/g)]
    .find((r) => /canvas\.preview/.test(r[1]));
  assert.ok(rule, `the canvas rendering rule was dropped — check for an ${NOT_FOUND}`);
  ['canvas.item-icon', 'canvas.worn'].forEach((selector) => {
    assert.ok(rule[1].split(',').some((part) => part.trim() === selector),
              `${selector} is not in the canvas rendering rule`);
  });
  assert.match(rule[2], /image-rendering:\s*pixelated/);
});

test('the icon and slot previews are still rubber-banded to their column', () => {
  const rule = ruleFor('.graphic canvas, .equip .preview');
  assert.ok(rule, `the preview sizing rule was dropped — check for an ${NOT_FOUND}`);
  assert.match(rule[2], /max-width:\s*100%/);
});

test('the colour swatch fits the 28px track it sits in inside an equipment slot', () => {
  // The default .swatch is 40px, which would overflow the 28px track into the preview column.
  const rule = ruleFor('.equip .swatch');
  assert.ok(rule, `.equip .swatch has no rule (the 40px default overflows the 28px track) — check for an ${NOT_FOUND}`);
  assert.match(rule[2], /width:\s*28px/);
});
