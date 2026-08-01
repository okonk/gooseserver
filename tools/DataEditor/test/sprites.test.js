import { test } from 'node:test';
import assert from 'node:assert/strict';
import { realBundles, skipWithoutBundles } from './real-bundles.js';

const { Sprites } = await import('../src/sprites.js');

// The real bundles, or null on a checkout that has not built them — see real-bundles.js. Every
// test below that dereferences `real` carries `...skipWithoutBundles` for that reason.
const real = realBundles;

// The `0:...` / `...:0:...` keys do not exist in the real bundles and never will — they are here
// so that "id 0 means no sprite" is a rule this fixture can catch being broken, rather than an
// assertion that only holds because the real atlas happens not to contain the key.
const bundles = {
  icons: { width: 64, height: 64, png: 'data:image/png;base64,AAA', rects: { '20107:810003': [96, 0, 32, 32], '0:810003': [1, 1, 1, 1], '20107:0': [2, 2, 2, 2], '0:0': [3, 3, 3, 3] } },
  parts: { width: 64, height: 64, png: 'data:image/png;base64,AAA', rects: { 'Bodies:1:idle-down': [0, 48, 24, 48], 'Bodies:0:idle-down': [4, 4, 4, 4], 'Bodies:2:mounted-idle-down': [8, 8, 24, 48], 'Bodies:0:mounted-idle-down': [6, 6, 6, 6] } },
  effects: { width: 64, height: 64, png: 'data:image/png;base64,AAA', rects: { '1080:0': [0, 0, 16, 16], '1080:1': [16, 0, 16, 16], '0:0': [5, 5, 5, 5] } },
};

test('icon lookup uses sheet:graphic', () => {
  assert.deepEqual(Sprites.icon(bundles, 20107, 810003), [96, 0, 32, 32]);
});

test('missing icon returns null', () => {
  assert.equal(Sprites.icon(bundles, 999, 1), null);
});

test('graphic 0 is treated as none', () => {
  assert.equal(Sprites.icon(bundles, 0, 0), null);
  assert.equal(Sprites.icon(bundles, 20107, 0), null);
  assert.equal(Sprites.icon(bundles, 0, 810003), null);
});

test('part lookup falls back through the clip candidates', () => {
  // AnimationNames.Candidates: unarmed prefers idle-no-equip, then idle, then idle-equip.
  const r = Sprites.part(bundles, 'Bodies', 1, false);
  assert.deepEqual(r, [0, 48, 24, 48]);
});

test('missing part returns null rather than a wrong sprite', () => {
  assert.equal(Sprites.part(bundles, 'Helms', 999, false), null);
});

test('effect frames enumerate until a gap', () => {
  assert.equal(Sprites.effectFrames(bundles, 1080).length, 2);
  assert.equal(Sprites.effectFrames(bundles, 9999).length, 0);
});

test('tint blends rgb and preserves source alpha', () => {
  // Icon.cs:9-11 — mix(t.rgb, tint.rgb, tint.a), COLOR.a = t.a.
  const out = Sprites.applyTint([100, 100, 100, 200], { r: 200, g: 0, b: 0, a: 128 });
  assert.equal(out[3], 200, 'alpha preserved');
  assert.equal(out[0], Math.round(100 + (200 - 100) * (128 / 255)));
  assert.equal(out[1], Math.round(100 + (0 - 100) * (128 / 255)));
  assert.equal(out[2], Math.round(100 + (0 - 100) * (128 / 255)));
});

test('zero blend alpha leaves pixels untouched', () => {
  assert.deepEqual(Sprites.applyTint([10, 20, 30, 40], { r: 255, g: 255, b: 255, a: 0 }),
                   [10, 20, 30, 40]);
});

test('fully transparent pixels stay transparent', () => {
  const out = Sprites.applyTint([0, 0, 0, 0], { r: 255, g: 0, b: 0, a: 255 });
  assert.equal(out[3], 0);
});

// --- clip candidate ordering, pinned to the client ---------------------------------------

test('clipCandidates orders by AnimationNames.Candidates("idle", ...)', () => {
  assert.deepEqual(Sprites.clipCandidates(false),
                   ['idle-no-equip-down', 'idle-down', 'idle-equip-down']);
  assert.deepEqual(Sprites.clipCandidates(true),
                   ['idle-equip-down', 'idle-down', 'idle-no-equip-down']);
});

test('every clipCandidates name exists in the real parts bundle', { ...skipWithoutBundles }, () => {
  const present = new Set(Object.keys(real.parts.rects).map((k) => k.split(':').slice(2).join(':')));
  for (const clip of Sprites.clipCandidates(false).concat(Sprites.clipCandidates(true))) {
    assert.ok(present.has(clip), `${clip} missing from sprites-parts.html`);
  }
});

test('the real bundle resolves a part for every category Appearance emits', { ...skipWithoutBundles }, () => {
  // Appearance.CATEGORY's value set. A category name that is not a parts key means a whole
  // slot silently never renders.
  const categories = ['Bodies', 'Hair', 'Eyes', 'Chest', 'Helms', 'Legs', 'Feet', 'Hands'];
  const first = {};
  for (const key of Object.keys(real.parts.rects)) {
    const [category, id] = key.split(':');
    if (first[category] === undefined) first[category] = id;
  }
  for (const category of categories) {
    assert.ok(first[category] !== undefined, `${category} absent from the bundle`);
    assert.ok(Sprites.part(real, category, first[category], false), `${category} did not resolve`);
    assert.ok(Sprites.part(real, category, first[category], true), `${category} equipped miss`);
  }
});

test('the real bundle resolves the specific ids the plan cites', { ...skipWithoutBundles }, () => {
  assert.deepEqual(Sprites.part(real, 'Bodies', 1, false), real.parts.rects['Bodies:1:idle-no-equip-down']);
  assert.deepEqual(Sprites.part(real, 'Bodies', 1, true), real.parts.rects['Bodies:1:idle-equip-down']);
  assert.equal(Sprites.icon(real, 104, 1), null);
});

test('a part with only a mounted clip resolves to null, not a mounted pose', { ...skipWithoutBundles }, () => {
  // Chest has one id whose animations.tres carries mounted-idle-down and no idle-*-down.
  const ids = {};
  for (const key of Object.keys(real.parts.rects)) {
    const [category, id, clip] = key.split(':');
    if (category !== 'Chest') continue;
    ids[id] = ids[id] || new Set();
    ids[id].add(clip);
  }
  const mountedOnly = Object.keys(ids).filter((id) => ![...ids[id]].some((c) => c.startsWith('idle')));
  assert.equal(mountedOnly.length, 1, 'bundle changed: expected exactly one mounted-only Chest id');
  assert.equal(Sprites.part(real, 'Chest', mountedOnly[0], false), null);
});

test('real effect frame runs are contiguous from 0, so effectFrames returns them whole', { ...skipWithoutBundles }, () => {
  const counts = {};
  for (const key of Object.keys(real.effects.rects)) {
    const [id] = key.split(':');
    counts[id] = (counts[id] || 0) + 1;
  }
  const ids = Object.keys(counts);
  assert.ok(ids.length > 100, 'expected the full effects corpus');
  for (const id of ids) assert.equal(Sprites.effectFrames(real, id).length, counts[id], `effect ${id}`);
});

// --- spreadsheet cells arrive as strings ---------------------------------------------------

test('ids and sheets coerce like the other modules, so string cells resolve', () => {
  assert.deepEqual(Sprites.icon(bundles, '20107', '810003'), [96, 0, 32, 32]);
  assert.deepEqual(Sprites.icon(bundles, ' 20107 ', '0810003'), [96, 0, 32, 32]);
  assert.deepEqual(Sprites.part(bundles, 'Bodies', '01', false), [0, 48, 24, 48]);
  assert.deepEqual(Sprites.part(bundles, 'Bodies', ' 1', false), [0, 48, 24, 48]);
  assert.equal(Sprites.effectFrames(bundles, '01080').length, 2);
});

test('a non-numeric or zero cell is "none", not a lookup', () => {
  assert.equal(Sprites.icon(bundles, '20107', 'abc'), null);
  assert.equal(Sprites.icon(bundles, '0', '810003'), null);
  assert.equal(Sprites.part(bundles, 'Bodies', '0', false), null);
  assert.equal(Sprites.part(bundles, 'Bodies', '', false), null);
  assert.equal(Sprites.effectFrames(bundles, '0').length, 0);
  assert.equal(Sprites.effectFrames(bundles, 'x').length, 0);
});

test('a negative id draws nothing', () => {
  assert.equal(Sprites.part(bundles, 'Bodies', -1, false), null);
  assert.equal(Sprites.icon(bundles, 20107, -1), null);
  assert.equal(Sprites.effectFrames(bundles, -1).length, 0);
});

// --- absent bundles --------------------------------------------------------------------------

test('an absent bundle is a miss, not a crash', () => {
  assert.equal(Sprites.icon({}, 20107, 810003), null);
  assert.equal(Sprites.part({}, 'Bodies', 1, false), null);
  assert.deepEqual(Sprites.effectFrames({}, 1080), []);
});

// --- tinting -----------------------------------------------------------------------------

test('applyTint never aliases its input', () => {
  const px = [10, 20, 30, 40];
  assert.notEqual(Sprites.applyTint(px, { r: 1, g: 2, b: 3, a: 0 }), px);
  assert.notEqual(Sprites.applyTint(px, null), px);
});

test('a missing tint is the identity', () => {
  assert.deepEqual(Sprites.applyTint([10, 20, 30, 40], null), [10, 20, 30, 40]);
  assert.deepEqual(Sprites.applyTint([10, 20, 30, 40], undefined), [10, 20, 30, 40]);
});

test('a full-alpha tint replaces rgb outright', () => {
  assert.deepEqual(Sprites.applyTint([10, 20, 30, 40], { r: 200, g: 100, b: 50, a: 255 }),
                   [200, 100, 50, 40]);
});

test('applyTint clamps out-of-range colour channels before blending', () => {
  // Partial alpha is the ONLY case where the input clamp is observable: at a=255 the blend is
  // f=1 and the output clamp gives 255 either way. At a=128, r=500 blends to 251 unclamped
  // against the 128 a valid byte would produce.
  assert.deepEqual(Sprites.applyTint([0, 0, 0, 255], { r: 500, g: -500, b: 300, a: 128 }),
                   [128, 0, 128, 255]);
  assert.deepEqual(Sprites.applyTint([0, 0, 0, 255], { r: 500, g: -500, b: 0, a: 255 }),
                   [255, 0, 0, 255]);
});

test('a negative blend alpha is a no-op, not a blend away from the tint', () => {
  // A negative alpha is truthy, so the `!tint.a` guard does not catch it. The lower clamp on f
  // is what stops it: unclamped, a=-100 gives f=-0.392 and drags each channel AWAY from the
  // tint and straight out of byte range (0 -> -100), which is also why the blend needs no
  // output clamp of its own. The delta has to be large enough that rounding cannot hide it.
  assert.deepEqual(Sprites.applyTint([0, 0, 0, 255], { r: 255, g: 255, b: 255, a: -100 }),
                   [0, 0, 0, 255]);
  assert.deepEqual(Sprites.applyTint([100, 100, 100, 255], { r: 0, g: 0, b: 0, a: -1 }),
                   [100, 100, 100, 255]);
});

test('tintPixels rewrites an RGBA buffer in place', () => {
  const buf = [10, 20, 30, 40, 0, 0, 0, 0];
  Sprites.tintPixels(buf, { r: 200, g: 100, b: 50, a: 255 });
  assert.deepEqual(buf, [200, 100, 50, 40, 200, 100, 50, 0]);
});

test('tintPixels with no blend alpha leaves the buffer alone', () => {
  const buf = [10, 20, 30, 40];
  Sprites.tintPixels(buf, { r: 255, g: 0, b: 0, a: 0 });
  assert.deepEqual(buf, [10, 20, 30, 40]);
  Sprites.tintPixels(buf, null);
  assert.deepEqual(buf, [10, 20, 30, 40]);
});

test('a tint object with no alpha field is NoTint, not NaN', () => {
  // Appearance always emits `a`, but a hand-built tint may not — and an undefined blend factor
  // would otherwise propagate NaN into every channel.
  assert.deepEqual(Sprites.applyTint([10, 20, 30, 40], { r: 255, g: 0, b: 0 }), [10, 20, 30, 40]);
  const buf = [10, 20, 30, 40];
  Sprites.tintPixels(buf, { r: 255, g: 0, b: 0 });
  assert.deepEqual(buf, [10, 20, 30, 40]);
});

test('an out-of-range blend alpha clamps to a full blend, it does not overshoot', () => {
  // Unclamped, a=300 gives f=1.18 and pushes the channel PAST the tint colour before the
  // output clamp catches it — 100 -> 218, not 200.
  assert.deepEqual(Sprites.applyTint([100, 100, 100, 40], { r: 200, g: 200, b: 200, a: 300 }),
                   [200, 200, 200, 40]);
});

test('blend results round rather than truncate', () => {
  // 10 * (13/255) = 0.5098 — Icon.cs blends in floats and the canvas byte is the nearest one.
  assert.deepEqual(Sprites.applyTint([0, 0, 0, 255], { r: 10, g: 10, b: 10, a: 13 }),
                   [1, 1, 1, 255]);
});

test('effectFrames returns the rects themselves, in frame order', () => {
  assert.deepEqual(Sprites.effectFrames(bundles, 1080), [[0, 0, 16, 16], [16, 0, 16, 16]]);
});

test('part returns the matching rect, not merely something truthy', () => {
  assert.deepEqual(Sprites.part(bundles, 'Bodies', 1, true), [0, 48, 24, 48]);
});

test('tintPixels blends each pixel against its own source channels', () => {
  // A partial blend is the only case where mixing the source channels up is visible: at a=255
  // the source contributes nothing at all.
  const buf = [100, 0, 0, 255];
  Sprites.tintPixels(buf, { r: 0, g: 0, b: 0, a: 128 });
  assert.deepEqual(buf, [Math.round(100 - 100 * (128 / 255)), 0, 0, 255]);
});

// --- draw ----------------------------------------------------------------------------------
//
// draw() is canvas plumbing, but "plumbing" is exactly where a swapped rect argument hides, so
// it runs here against a stub canvas rather than being left to the manual smoke test.

function stubCanvas() {
  const calls = [];
  const ctx = {
    calls,
    drawImage: (...args) => calls.push(['drawImage', ...args]),
    getImageData: (x, y, w, h) => {
      calls.push(['getImageData', x, y, w, h]);
      return { data: pixels.slice(0, w * h * 4) };
    },
    putImageData: (img, x, y) => calls.push(['putImageData', x, y, [...img.data]]),
  };
  return ctx;
}

let pixels = [];
let offscreen = null;

// The stub honours its arguments the way the real DOM does — createElement('div') has no 2d
// context and getContext('webgl') is not a CanvasRenderingContext2D — so a wrong string here
// fails rather than passing silently.
globalThis.document = {
  createElement: (tag) => {
    if (tag !== 'canvas') return { getContext: () => null };
    offscreen = {
      tagName: 'canvas',
      getContext: (type) => (type === '2d' ? (offscreen.ctx = offscreen.ctx || stubCanvas()) : null),
    };
    return offscreen;
  },
};

test('draw with no rect is a no-op', () => {
  const ctx = stubCanvas();
  Sprites.draw(ctx, 'img', null, 5, 6, { r: 1, g: 2, b: 3, a: 255 });
  assert.deepEqual(ctx.calls, []);
});

test('untinted draw blits the rect straight through at 1:1', () => {
  const ctx = stubCanvas();
  Sprites.draw(ctx, 'img', [10, 20, 30, 40], 5, 6, null);
  assert.deepEqual(ctx.calls, [['drawImage', 'img', 10, 20, 30, 40, 5, 6, 30, 40]]);
});

test('a zero-alpha tint takes the untinted path — no offscreen canvas', () => {
  const ctx = stubCanvas();
  offscreen = null;
  Sprites.draw(ctx, 'img', [10, 20, 30, 40], 5, 6, { r: 255, g: 0, b: 0, a: 0 });
  assert.deepEqual(ctx.calls, [['drawImage', 'img', 10, 20, 30, 40, 5, 6, 30, 40]]);
  assert.equal(offscreen, null, 'no offscreen canvas should have been created');
});

test('a tinted draw sizes the offscreen canvas to the rect and blits the tinted result', () => {
  pixels = [100, 0, 0, 255, 0, 0, 0, 0];
  const ctx = stubCanvas();
  Sprites.draw(ctx, 'img', [10, 20, 2, 1], 5, 6, { r: 0, g: 0, b: 0, a: 255 });

  assert.equal(offscreen.width, 2);
  assert.equal(offscreen.height, 1);
  // The source rect is copied to the offscreen origin, not to dx/dy.
  assert.deepEqual(offscreen.ctx.calls[0], ['drawImage', 'img', 10, 20, 2, 1, 0, 0, 2, 1]);
  // Read back the whole rect, width then height — transposing it reads the wrong pixels.
  assert.deepEqual(offscreen.ctx.calls[1], ['getImageData', 0, 0, 2, 1]);
  // Written back to the offscreen ORIGIN, not to dx/dy: the offscreen canvas is only as big as
  // the rect, so writing at dx/dy puts every tinted pixel off its edge and the sprite renders
  // untinted.
  assert.deepEqual(offscreen.ctx.calls[2], ['putImageData', 0, 0, [0, 0, 0, 255, 0, 0, 0, 0]]);
  // ...and the offscreen canvas, already the right size, goes down at dx/dy.
  assert.deepEqual(ctx.calls, [['drawImage', offscreen, 5, 6]]);
});

// --- mount ---------------------------------------------------------------------------------

test('mount looks up the mounted-idle-down clip', () => {
  assert.deepEqual(Sprites.mount(bundles, 2), [8, 8, 24, 48]);
});

test('mount does not fall back to a standing clip', () => {
  // Bodies:1 has every idle clip and no mounted one. Substituting idle would draw a standing
  // body where the mount should be.
  assert.equal(Sprites.mount(bundles, 1), null);
});

test('mount treats 0, a negative id and junk as no mount', () => {
  assert.equal(Sprites.mount(bundles, 0), null);
  assert.equal(Sprites.mount(bundles, '0'), null);
  assert.equal(Sprites.mount(bundles, -2), null);
  assert.equal(Sprites.mount(bundles, 'abc'), null);
});

test('mount coerces string cells', () => {
  assert.deepEqual(Sprites.mount(bundles, '2'), [8, 8, 24, 48]);
  assert.deepEqual(Sprites.mount(bundles, '02'), [8, 8, 24, 48]);
  assert.deepEqual(Sprites.mount(bundles, ' 2'), [8, 8, 24, 48]);
});

test('mount survives an absent parts bundle', () => {
  assert.equal(Sprites.mount({}, 2), null);
});

test('mount resolves against the real bundle', { ...skipWithoutBundles }, () => {
  // Exactly four Bodies ids ship a mounted-idle-down clip; 1 is one of them and 10101 is not.
  assert.deepEqual(Sprites.mount(real, 1), real.parts.rects['Bodies:1:mounted-idle-down']);
  assert.equal(Sprites.mount(real, 10101), null);

  const mounts = Object.keys(real.parts.rects)
    .filter((k) => k.startsWith('Bodies:') && k.endsWith(':mounted-idle-down'));
  assert.equal(mounts.length, 4, 'bundle changed: expected four mountable bodies');
  for (const key of mounts) assert.deepEqual(Sprites.mount(real, key.split(':')[1]), real.parts.rects[key]);
});

test('draw with no image yet is a no-op on both paths', () => {
  // Task 11 loads the three bundle PNGs lazily; a redraw while one is still decoding must skip
  // that layer, not take down the whole render with a TypeError from drawImage(null, ...).
  const ctx = stubCanvas();
  offscreen = null;
  Sprites.draw(ctx, null, [10, 20, 30, 40], 5, 6, null);
  Sprites.draw(ctx, undefined, [10, 20, 30, 40], 5, 6, { r: 1, g: 2, b: 3, a: 255 });
  assert.deepEqual(ctx.calls, []);
  assert.equal(offscreen, null, 'no offscreen canvas should have been created');
});

// --- scaled ---------------------------------------------------------------------------------

// A <canvas> stub with the four things scaled() touches. width/height are plain properties, so
// the test can see both what it was told and what scaled() changed it to.
function scalableCanvas(w, h) {
  const calls = [];
  const ctx = {
    calls,
    clearRect: (...args) => calls.push(['clearRect', ...args]),
    setTransform: (...args) => calls.push(['setTransform', ...args]),
    set imageSmoothingEnabled(v) { calls.push(['imageSmoothingEnabled', v]); },
  };
  return { width: w, height: h, getContext: () => ctx };
}

test('scaled sizes the backing store from the scale and prepares the context', () => {
  // The point of the helper: the canvas dimensions and the transform cannot disagree, because
  // one function computes both from the same scale.
  const node = scalableCanvas(0, 0);
  const c = Sprites.scaled(node, 4, 96, 112);

  assert.equal(node.width, 384);
  assert.equal(node.height, 448);
  assert.deepEqual(c.calls, [
    ['setTransform', 4, 0, 0, 4, 0, 0],
    ['imageSmoothingEnabled', false],
    // In LOGICAL units — it follows the transform, so clearing 96x112 clears the whole 384x448.
    ['clearRect', 0, 0, 96, 112],
  ]);
});

test('scaled corrects a canvas that was sized for a different scale', () => {
  const node = scalableCanvas(96, 112);
  Sprites.scaled(node, 2, 96, 112);
  assert.equal(node.width, 192);
  assert.equal(node.height, 224);
});

test('scaled leaves an already-correct size alone', () => {
  // Assigning canvas.width resets the context in a real DOM, so a redraw on every keystroke
  // must not touch it. The stub records assignments to prove it does not.
  const node = scalableCanvas(0, 0);
  const written = [];
  Object.defineProperty(node, 'width', {
    get: () => 128, set: (v) => written.push(['width', v]),
  });
  Object.defineProperty(node, 'height', {
    get: () => 128, set: (v) => written.push(['height', v]),
  });

  Sprites.scaled(node, 2, 64, 64);
  assert.deepEqual(written, []);
});

test('scaled treats a missing scale as 1x', () => {
  // The unscaled callers (and every test that omits the argument) go through the same path.
  const node = scalableCanvas(0, 0);
  const c = Sprites.scaled(node, undefined, 40, 56);
  assert.equal(node.width, 40);
  assert.equal(node.height, 56);
  assert.deepEqual(c.calls[0], ['setTransform', 1, 0, 0, 1, 0, 0]);
});
