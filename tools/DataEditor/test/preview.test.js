import { test } from 'node:test';
import assert from 'node:assert/strict';
import { installFakeDom, createElement } from './fake-dom.js';

installFakeDom();

const { Equipped } = await import('../src/equipped.js');
globalThis.Equipped = Equipped;
const { Appearance } = await import('../src/appearance.js');
globalThis.Appearance = Appearance;
const { Sprites } = await import('../src/sprites.js');
globalThis.Sprites = Sprites;

const { Preview } = await import('../src/preview.js');

// A bundle carrying one rect per (category, id, clip) the tests need. Rects are
// [x, y, w, h] in the bundle PNG, the shape tools/SpriteBundle emits.
function partsBundle(rects) {
  return { bundles: { parts: { rects } }, images: { parts: { fake: 'parts.png' } } };
}

function canvas(w, h) {
  const node = createElement('canvas');
  node.width = w === undefined ? Preview.CANVAS_W : w;
  node.height = h === undefined ? Preview.CANVAS_H : h;
  return node;
}

function drawCalls(node) {
  return node.getContext('2d').calls.filter((c) => c[0] === 'drawImage');
}

// --- the armed/unarmed flag (carry-forward #15) ----------------------------------------------

test('isArmed is bodyState !== 3, through parseInt', () => {
  assert.equal(Preview.isArmed(3), false);
  assert.equal(Preview.isArmed('3'), false);
  assert.equal(Preview.isArmed(1), true);
  assert.equal(Preview.isArmed(7), true);
  // A blank cell is 0, which is armed — matching the NPCs column default of 1. Items defaults
  // to 3, but Items has no character preview.
  assert.equal(Preview.isArmed(''), true);
  assert.equal(Preview.isArmed(undefined), true);
});

test('the armed flag reaches Sprites.part and selects a DIFFERENT clip', () => {
  // The same body id with both clips present. Which one is drawn is the only observable
  // difference between armed and unarmed, and a forgotten argument would be undefined ->
  // falsy -> the unarmed chain, silently.
  const ctx = partsBundle({
    'Bodies:1:idle-equip-down': [0, 0, 48, 48],
    'Bodies:1:idle-no-equip-down': [64, 0, 48, 48],
  });

  const armed = canvas();
  Preview.character(armed, { bodyId: 1, bodyState: 1 }, ctx);
  assert.equal(drawCalls(armed)[0][2], 0);   // sx of idle-equip-down

  const unarmed = canvas();
  Preview.character(unarmed, { bodyId: 1, bodyState: 3 }, ctx);
  assert.equal(drawCalls(unarmed)[0][2], 64);   // sx of idle-no-equip-down
});

// --- anchoring ------------------------------------------------------------------------------

test('a 48px sprite lands its feet exactly on ORIGIN_Y', () => {
  // CharacterAnchor.OffsetY is the offset of a CENTRE-pivot sprite; drawImage takes the TOP
  // edge, hence the extra -h/2. At h=48 that puts the bottom edge on the origin, which is the
  // property the whole anchor exists for.
  const ctx = partsBundle({ 'Bodies:1:idle-down': [0, 0, 48, 48] });
  const node = canvas();
  Preview.character(node, { bodyId: 1, bodyState: 1 }, ctx);

  const call = drawCalls(node)[0];
  const dy = call[7];
  assert.equal(dy + 48, Preview.ORIGIN_Y);
  assert.equal(dy, 40);
});

test('a taller sprite overhangs downwards, as the client comments say it should', () => {
  const ctx = partsBundle({ 'Bodies:1:idle-down': [0, 0, 32, 64] });
  const node = canvas();
  Preview.character(node, { bodyId: 1, bodyState: 1 }, ctx);

  const call = drawCalls(node)[0];
  // offsetY(64) = max(8,0) - 32 = -24; centre 64, top 32, bottom 96 — 8px below the origin.
  assert.equal(call[7], 32);
  assert.equal(call[7] + 64, 96);
});

test('sprites are centred horizontally on the canvas', () => {
  const ctx = partsBundle({ 'Bodies:1:idle-down': [0, 0, 33, 48] });
  const node = canvas();
  Preview.character(node, { bodyId: 1, bodyState: 1 }, ctx);
  // floor((96 - 33) / 2)
  assert.equal(drawCalls(node)[0][6], 31);
});

// --- layering -------------------------------------------------------------------------------

test('layers are drawn in Appearance sort order, back to front', () => {
  const ctx = partsBundle({
    'Bodies:1:idle-down': [0, 0, 48, 48],
    'Hair:5:idle-down': [10, 0, 48, 48],
    'Chest:7:idle-down': [20, 0, 48, 48],
  });
  const node = canvas();

  const result = Preview.character(node, {
    bodyId: 1, hairId: 5, bodyState: 1,
    equippedItems: '7,*,0,*,0,*,0,*,0,*,0,*',
  }, ctx);

  // Body (order 3) then Chest (7) then Hair (8) — the client sorts AFTER applying, so hair is
  // drawn over the chest piece, not under it.
  assert.deepEqual(drawCalls(node).map((c) => c[2]), [0, 20, 10]);
  // Legs 3 underwear is added for body 1 but has no art here, so it is a layer that is not
  // drawn — which is exactly what the two counters are for.
  assert.equal(result.layers, 4);
  assert.equal(result.drawn, 3);
});

test('a layer with no art is skipped, not drawn at the wrong size', () => {
  const node = canvas();
  const result = Preview.character(node, { bodyId: 99, bodyState: 1 }, partsBundle({}));
  assert.equal(result.layers, 1);
  assert.equal(result.drawn, 0);
  assert.deepEqual(drawCalls(node), []);
});

test('the canvas is cleared before anything is drawn', () => {
  const node = canvas();
  Preview.character(node, { bodyId: 1, bodyState: 1 },
                    partsBundle({ 'Bodies:1:idle-down': [0, 0, 48, 48] }));
  const calls = node.getContext('2d').calls;
  // The transform comes first — it decides what the clearRect's logical size means.
  assert.deepEqual(calls[0], ['setTransform', 1, 0, 0, 1, 0, 0]);
  assert.deepEqual(calls[1], ['clearRect', 0, 0, Preview.CANVAS_W, Preview.CANVAS_H]);
});

test('the tint travels with the layer', () => {
  // Sprites.draw takes the tinted path — offscreen canvas, getImageData, putImageData — only
  // when the tint has a non-zero blend factor, so the call trail is the assertion.
  const ctx = partsBundle({ 'Bodies:1:idle-down': [0, 0, 4, 4] });

  const tinted = canvas();
  Preview.character(tinted, { bodyId: 1, bodyState: 1, bodyR: 255, bodyA: 128 }, ctx);
  assert.equal(drawCalls(tinted).length, 1);
  assert.equal(drawCalls(tinted)[0].length, 4);   // drawImage(off, dx, dy)

  const plain = canvas();
  Preview.character(plain, { bodyId: 1, bodyState: 1, bodyR: 255, bodyA: 0 }, ctx);
  assert.equal(drawCalls(plain)[0].length, 10);   // the nine-argument source-rect form
});

test('a body id of 100 or more draws the body alone', () => {
  const ctx = partsBundle({
    'Bodies:150:idle-down': [0, 0, 48, 48],
    'Hair:5:idle-down': [10, 0, 48, 48],
    'Chest:7:idle-down': [20, 0, 48, 48],
  });
  const node = canvas();
  const result = Preview.character(node, {
    bodyId: 150, hairId: 5, bodyState: 1, equippedItems: '7,*,0,*,0,*,0,*,0,*,0,*',
  }, ctx);

  assert.equal(result.layers, 1);
  assert.deepEqual(drawCalls(node).map((c) => c[2]), [0]);
});

test('a missing parts bundle or image leaves the canvas blank rather than throwing', () => {
  const blank = canvas();
  // Body 1 plus the underwear legs Appearance adds for it: two layers asked for, none drawable.
  assert.deepEqual(Preview.character(blank, { bodyId: 1, bodyState: 1 }, {}),
                   { layers: 2, drawn: 0 });

  // Bundle present, PNG still decoding: the rect resolves, so the layer counts as drawn, but
  // Sprites.draw refuses a null image rather than throwing a TypeError at drawImage.
  const decoding = canvas();
  const result = Preview.character(decoding, { bodyId: 1, bodyState: 1 },
                                   { bundles: { parts: { rects: { 'Bodies:1:idle-down': [0, 0, 48, 48] } } },
                                     images: {} });
  assert.equal(result.drawn, 1);
  assert.deepEqual(drawCalls(decoding), []);
});

// --- effects --------------------------------------------------------------------------------

// setInterval is replaced rather than waited on: a test that slept 125ms per frame would be the
// slowest thing in the suite and still flaky.
function withFakeTimers(body) {
  const realSet = globalThis.setInterval;
  const realClear = globalThis.clearInterval;
  const timers = new Map();
  let next = 1;

  globalThis.setInterval = (fn, ms) => { timers.set(next, { fn, ms }); return next++; };
  globalThis.clearInterval = (id) => { timers.delete(id); };

  try {
    return body({
      timers,
      tick(times = 1) {
        for (let i = 0; i < times; i++) [...timers.values()].forEach((t) => t.fn());
      },
    });
  } finally {
    globalThis.setInterval = realSet;
    globalThis.clearInterval = realClear;
  }
}

const effectsCtx = {
  bundles: { effects: { rects: { '4:0': [0, 0, 32, 32], '4:1': [32, 0, 32, 32],
                                 '4:2': [64, 0, 32, 32] } } },
  images: { effects: { fake: 'effects.png' } },
};

test('the first frame is on screen immediately, not one interval later', () => {
  withFakeTimers(({ timers }) => {
    const node = canvas(96, 96);
    const stop = Preview.effect(node, 4, effectsCtx);
    assert.equal(drawCalls(node).length, 1);
    assert.equal(drawCalls(node)[0][2], 0);
    assert.equal([...timers.values()][0].ms, Preview.FRAME_MS);
    stop();
  });
});

test('frames loop in order and wrap', () => {
  withFakeTimers(({ tick }) => {
    const node = canvas(96, 96);
    const stop = Preview.effect(node, 4, effectsCtx);
    tick(4);
    assert.deepEqual(drawCalls(node).map((c) => c[2]), [0, 32, 64, 0, 32]);
    stop();
  });
});

test('the returned stop function really stops the timer', () => {
  withFakeTimers(({ tick, timers }) => {
    const node = canvas(96, 96);
    const stop = Preview.effect(node, 4, effectsCtx);
    stop();
    assert.equal(timers.size, 0);
    tick(3);
    assert.equal(drawCalls(node).length, 1);
  });
});

test('an effect with no frames clears the canvas and starts no timer', () => {
  withFakeTimers(({ timers }) => {
    const node = canvas(96, 96);
    const stop = Preview.effect(node, 999, effectsCtx);
    assert.equal(timers.size, 0);
    assert.deepEqual(node.getContext('2d').calls, [
      ['setTransform', 1, 0, 0, 1, 0, 0],
      ['imageSmoothingEnabled', false],
      ['clearRect', 0, 0, 96, 96],
    ]);
    assert.equal(typeof stop, 'function');
    stop();   // must be safe to call
  });
});

test('effect frames are centred on the canvas', () => {
  withFakeTimers(() => {
    const node = canvas(96, 96);
    const stop = Preview.effect(node, 4, effectsCtx);
    const call = drawCalls(node)[0];
    assert.equal(call[6], 32);
    assert.equal(call[7], 32);
    stop();
  });
});

test('constants are pinned', () => {
  assert.equal(Preview.CANVAS_W, 96);
  assert.equal(Preview.CANVAS_H, 112);
  assert.equal(Preview.ORIGIN_Y, 88);
  assert.equal(Preview.FRAME_MS, 125);
});

// --- scaled previews -------------------------------------------------------------------------
// The bargain: the canvas backing store grows, the context is scaled once, and NONE of the
// anchoring or centring arithmetic changes. So the test for a scaled preview is that the
// drawImage destinations are byte-for-byte the ones the unscaled preview produced.

test('character scales the CONTEXT and leaves the destinations alone', () => {
  const rects = { 'Bodies:1:idle-equip-down': [0, 0, 31, 61] };
  const plain = canvas();
  Preview.character(plain, { bodyId: 1, bodyState: 1 }, partsBundle(rects));

  const big = canvas(Preview.CANVAS_W * 4, Preview.CANVAS_H * 4);
  Preview.character(big, { bodyId: 1, bodyState: 1 }, partsBundle(rects), 4);

  const calls = big.getContext('2d').calls;
  assert.deepEqual(calls[0], ['setTransform', 4, 0, 0, 4, 0, 0]);
  assert.deepEqual(calls[1], ['clearRect', 0, 0, Preview.CANVAS_W, Preview.CANVAS_H]);
  assert.deepEqual(drawCalls(big), drawCalls(plain));
  assert.equal(big.getContext('2d').imageSmoothingEnabled, false);
});

test('effect scales the CONTEXT and leaves the destinations alone', () => {
  withFakeTimers(() => {
    const plain = canvas(96, 96);
    Preview.effect(plain, 4, effectsCtx)();

    const big = canvas(Preview.EFFECT_SIZE * 2, Preview.EFFECT_SIZE * 2);
    Preview.effect(big, 4, effectsCtx, 2)();

    const calls = big.getContext('2d').calls;
    assert.deepEqual(calls[0], ['setTransform', 2, 0, 0, 2, 0, 0]);
    assert.deepEqual(drawCalls(big), drawCalls(plain));
    assert.equal(big.getContext('2d').imageSmoothingEnabled, false);
  });
});

test('the two preview scales are exported so app.js and preview.js agree', () => {
  assert.equal(Preview.CHARACTER_SCALE, 4);
  assert.equal(Preview.EFFECT_SCALE, 2);
});
