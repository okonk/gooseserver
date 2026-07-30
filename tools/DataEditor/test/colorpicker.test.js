import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { installFakeDom, fire, walk } from './fake-dom.js';

const source = readFileSync(fileURLToPath(new URL('../src/colorpicker.js', import.meta.url)),
                            'utf8');

installFakeDom();

const { Forms } = await import('../src/forms.js');
globalThis.Forms = Forms;
const { Sprites } = await import('../src/sprites.js');
globalThis.Sprites = Sprites;

const { ColorPicker } = await import('../src/colorpicker.js');

// --- pure colour maths ------------------------------------------------------------------------

test('rgbToHsv and hsvToRgb round-trip every colour on a 16-step lattice', () => {
  // 4,096 combinations rather than all 16.7M: the lattice hits every sector of the wheel and both
  // ends of every channel, and the failure this guards against — a rounding rule that is off by
  // one — is not sensitive to the colour it happens on.
  const steps = [];
  for (let i = 0; i < 16; i++) steps.push(i * 17); // 0, 17, ... 255
  let checked = 0;

  for (const r of steps) {
    for (const g of steps) {
      for (const b of steps) {
        const hsv = ColorPicker.rgbToHsv(r, g, b);
        const back = ColorPicker.hsvToRgb(hsv.h, hsv.s, hsv.v);
        assert.deepEqual(back, { r, g, b }, `drifted at ${r},${g},${b}`);
        checked++;
      }
    }
  }
  assert.equal(checked, 4096);
});

test('rgbToHsv and hsvToRgb round-trip every grey, where hue is undefined', () => {
  // The axis a naive implementation loses: max === min, so the hue formula divides by zero and
  // the saturation is 0 — every hue must give the same bytes back.
  for (let n = 0; n <= 255; n++) {
    const hsv = ColorPicker.rgbToHsv(n, n, n);
    assert.equal(hsv.s, 0);
    assert.deepEqual(ColorPicker.hsvToRgb(hsv.h, hsv.s, hsv.v), { r: n, g: n, b: n });
    // ...and from any hue at all, not just the 0 rgbToHsv reports.
    assert.deepEqual(ColorPicker.hsvToRgb(217, 0, n / 255), { r: n, g: n, b: n });
  }
});

test('rgbToHsv reads the primaries', () => {
  assert.deepEqual(ColorPicker.rgbToHsv(255, 0, 0), { h: 0, s: 1, v: 1 });
  assert.deepEqual(ColorPicker.rgbToHsv(0, 255, 0), { h: 120, s: 1, v: 1 });
  assert.deepEqual(ColorPicker.rgbToHsv(0, 0, 255), { h: 240, s: 1, v: 1 });
});

test('hsvToRgb wraps the hue rather than clipping it', () => {
  assert.deepEqual(ColorPicker.hsvToRgb(360, 1, 1), { r: 255, g: 0, b: 0 });
  assert.deepEqual(ColorPicker.hsvToRgb(-120, 1, 1), { r: 0, g: 0, b: 255 });
});

test('hsvToRgb clamps s and v into range', () => {
  assert.deepEqual(ColorPicker.hsvToRgb(0, 5, 5), { r: 255, g: 0, b: 0 });
  assert.deepEqual(ColorPicker.hsvToRgb(0, -1, -1), { r: 0, g: 0, b: 0 });
});

test('parseHex takes six digits with or without a #, and nothing else', () => {
  assert.deepEqual(ColorPicker.parseHex('#A4331F'), { r: 164, g: 51, b: 31 });
  assert.deepEqual(ColorPicker.parseHex('  a4331f '), { r: 164, g: 51, b: 31 });
  // null, not black: "I could not read this" and "this is black" are different answers.
  assert.equal(ColorPicker.parseHex('#a43'), null);
  assert.equal(ColorPicker.parseHex('#a4331g'), null);
  assert.equal(ColorPicker.parseHex(''), null);
  assert.equal(ColorPicker.parseHex(null), null);
});

test('formatHex pads to two digits and clamps out-of-range channels', () => {
  assert.equal(ColorPicker.formatHex(0, 0, 0), '#000000');
  assert.equal(ColorPicker.formatHex(164, 51, 31), '#a4331f');
  assert.equal(ColorPicker.formatHex(999, -4, 'x'), '#ff0000');
});

test('formatHex and parseHex are inverses', () => {
  for (let n = 0; n <= 255; n++) {
    assert.deepEqual(ColorPicker.parseHex(ColorPicker.formatHex(n, 255 - n, n)),
      { r: n, g: 255 - n, b: n });
  }
});

// --- pointToValue -----------------------------------------------------------------------------

test('pointToValue reports the fraction of the way across and down', () => {
  const rect = { left: 20, top: 5, width: 200, height: 100 };
  assert.deepEqual(ColorPicker.pointToValue(rect, 120, 55), { x: 0.5, y: 0.5 });
  assert.deepEqual(ColorPicker.pointToValue(rect, 20, 5), { x: 0, y: 0 });
  assert.deepEqual(ColorPicker.pointToValue(rect, 220, 105), { x: 1, y: 1 });
});

test('pointToValue clamps a pointer that left the element', () => {
  // A drag continues on the document, so the pointer really does go outside — unclamped, it would
  // give a saturation above 1 and hsvToRgb would have to defend itself.
  const rect = { left: 20, top: 5, width: 200, height: 100 };
  assert.deepEqual(ColorPicker.pointToValue(rect, -400, 900), { x: 0, y: 1 });
  assert.deepEqual(ColorPicker.pointToValue(rect, 9000, -900), { x: 1, y: 0 });
});

test('pointToValue reads a zero-sized rect as 0, not NaN', () => {
  // What an element inside a hidden popover measures.
  assert.deepEqual(ColorPicker.pointToValue({ left: 0, top: 0, width: 0, height: 0 }, 5, 5),
    { x: 0, y: 0 });
  assert.deepEqual(ColorPicker.pointToValue(null, 5, 5), { x: 0, y: 0 });
});

// --- the control ------------------------------------------------------------------------------

function make(extra) {
  ColorPicker.resetRecent();
  const seen = [];
  const picker = ColorPicker.control(Object.assign({
    r: 164, g: 51, b: 31, a: 128, withAlpha: true,
    onChange(colour) { seen.push(colour); },
  }, extra || {}));
  const node = picker.node;
  const find = (cls) => node.querySelector(`[class="${cls}"]`);
  return { picker, node, seen, find, swatch: find('swatch'), pop: find('cp-pop') };
}

function open(c) {
  fire(c.swatch, 'click');
  return c;
}

// The last frame put onto a canvas, as the flat RGBA byte array the recording context logged.
function frame(canvas) {
  const puts = canvas.getContext('2d').calls.filter((c) => c[0] === 'putImageData');
  assert.ok(puts.length, 'nothing was painted onto this canvas');
  return puts[puts.length - 1][3];
}

function pixelAt(canvas, x, y) {
  const data = frame(canvas);
  const at = (y * canvas.width + x) * 4;
  return [data[at], data[at + 1], data[at + 2], data[at + 3]];
}

test('the node is a wrapper carrying the swatch and a closed popover', () => {
  const c = make();
  assert.equal(c.node.tagName, 'DIV');
  assert.equal(c.node.getAttribute('class'), 'colorpicker');
  assert.equal(c.swatch.tagName, 'BUTTON');
  // type=button, or it submits the form it sits in.
  assert.equal(c.swatch.getAttribute('type'), 'button');
  assert.equal(c.pop.hidden, true);
});

test('the swatch shows the seeded colour', () => {
  const c = make();
  assert.equal(c.swatch.getAttribute('data-color'), '#a4331f');
  assert.equal(c.swatch.style.background, '#a4331f');
});

test('building the control reports nothing', () => {
  // The rule Composites.rgbaControl depends on: opening a record must not change it, so a control
  // nobody has touched must never call back.
  const c = make();
  assert.deepEqual(c.seen, []);
});

test('out-of-range seed channels are clamped into the swatch', () => {
  const c = make({ r: 999, g: -4, b: 'x', a: 900 });
  assert.equal(c.swatch.getAttribute('data-color'), '#ff0000');
  assert.deepEqual(c.seen, []);
});

test('clicking the swatch opens the popover, and again closes it', () => {
  const c = make();
  assert.equal(c.swatch.getAttribute('aria-haspopup'), 'dialog');
  assert.equal(c.swatch.getAttribute('aria-expanded'), 'false');

  open(c);
  assert.equal(c.pop.hidden, false);
  assert.equal(c.pop.getAttribute('role'), 'dialog');
  assert.ok(c.pop.getAttribute('aria-label'));
  assert.equal(c.swatch.getAttribute('aria-expanded'), 'true');

  fire(c.swatch, 'click');
  assert.equal(c.pop.hidden, true);
  assert.equal(c.swatch.getAttribute('aria-expanded'), 'false');
});

test('opening paints the SV square from the current hue', () => {
  const c = open(make({ r: 255, g: 0, b: 0, a: 255 }));
  const sv = c.find('cp-sv');
  // Top-left is unsaturated and full brightness (white), bottom-left black, top-right the hue.
  assert.deepEqual(pixelAt(sv, 0, 0), [255, 255, 255, 255]);
  assert.deepEqual(pixelAt(sv, 0, sv.height - 1), [0, 0, 0, 255]);
  assert.deepEqual(pixelAt(sv, sv.width - 1, 0), [255, 0, 0, 255]);
});

test('the hue strip runs from red at the top all the way round', () => {
  const c = open(make());
  const hue = c.find('cp-hue');
  const dominant = (y) => {
    const px = pixelAt(hue, 0, y);
    return 'rgb'[px.indexOf(Math.max(px[0], px[1], px[2]))];
  };
  assert.deepEqual(pixelAt(hue, 0, 0), [255, 0, 0, 255]);
  // A third of the way down green leads, two thirds down blue — the wheel, not a red-to-red fade.
  // Which pixel is EXACTLY green depends on the strip's height, so the test asks for the shape of
  // the ramp rather than re-deriving the module's own arithmetic.
  assert.equal(dominant(0), 'r');
  assert.equal(dominant(Math.round((hue.height - 1) / 3)), 'g');
  assert.equal(dominant(Math.round((hue.height - 1) * 2 / 3)), 'b');
  // The bottom is back at the top of the wheel, not past it.
  assert.equal(dominant(hue.height - 1), 'r');
});

test('the blend strip is the tint applied to a grey, not a transparency checkerboard', () => {
  // Sprites.applyTint is the one rule for what a blend factor does (Icon.cs:9-11). Painting the
  // strip any other way would let the strip and the sprite previews disagree.
  const c = open(make({ r: 0, g: 0, b: 0, a: 255 }));
  const alpha = c.find('cp-alpha');
  // Bottom of the strip is blend 0: the bare grey stand-in, untouched by a black tint.
  assert.deepEqual(pixelAt(alpha, 0, alpha.height - 1), [128, 128, 128, 255]);
  // Top is a full blend: all the way to the tint colour.
  assert.deepEqual(pixelAt(alpha, 0, 0), [0, 0, 0, 255]);
});

test('clicking the SV square reports the colour under the pointer', () => {
  const c = open(make());
  const sv = c.find('cp-sv');
  sv.rect = { left: 0, top: 0, width: 100, height: 100 };
  // Full saturation, full brightness, at the seeded hue — the pure form of #a4331f.
  fire(sv, 'mousedown', { clientX: 100, clientY: 0 });

  const hsv = ColorPicker.rgbToHsv(164, 51, 31);
  assert.deepEqual(c.seen, [Object.assign(ColorPicker.hsvToRgb(hsv.h, 1, 1), { a: 128 })]);
  assert.equal(c.swatch.getAttribute('data-color'),
    ColorPicker.formatHex(c.seen[0].r, c.seen[0].g, c.seen[0].b));
});

test('a drag keeps steering after the pointer leaves the square, and stops on release', () => {
  const c = open(make());
  const sv = c.find('cp-sv');
  sv.rect = { left: 0, top: 0, width: 100, height: 100 };

  fire(sv, 'mousedown', { clientX: 100, clientY: 0 });
  // Off the left edge entirely: clamped to saturation 0, which is white, not ignored.
  fire(document, 'mousemove', { clientX: -50, clientY: 0 });
  assert.deepEqual(c.seen[1], { r: 255, g: 255, b: 255, a: 128 });

  fire(document, 'mouseup');
  fire(document, 'mousemove', { clientX: 50, clientY: 50 });
  assert.equal(c.seen.length, 2, 'a move after release still moved the colour');
});

test('the hue strip sets the hue and repaints the square', () => {
  const c = open(make());
  const hue = c.find('cp-hue');
  const sv = c.find('cp-sv');
  hue.rect = { left: 0, top: 0, width: 14, height: 359 };

  fire(hue, 'mousedown', { clientX: 0, clientY: 120 });
  assert.equal(hue.getAttribute('aria-valuenow'), '120');
  // Saturation and brightness are unchanged, so the reported colour is the old one rotated.
  const hsv = ColorPicker.rgbToHsv(164, 51, 31);
  assert.deepEqual(c.seen[0], Object.assign(ColorPicker.hsvToRgb(120, hsv.s, hsv.v), { a: 128 }));
  // The square now shows green at full saturation and brightness.
  assert.deepEqual(pixelAt(sv, sv.width - 1, 0), [0, 255, 0, 255]);
});

test('the blend strip sets the factor without touching the colour', () => {
  const c = open(make());
  const alpha = c.find('cp-alpha');
  alpha.rect = { left: 0, top: 0, width: 14, height: 255 };

  // 55 of 255 down a strip whose top is full blend.
  fire(alpha, 'mousedown', { clientX: 0, clientY: 55 });
  assert.deepEqual(c.seen, [{ r: 164, g: 51, b: 31, a: 200 }]);
});

test('the blend readout says blend, never opacity', () => {
  // Icon.cs:9-11 — the factor drags the sprite towards the tint. It is not transparency, and the
  // word "opacity" anywhere here would teach the wrong thing about what a 128 does.
  const c = open(make());
  const blend = c.find('cp-blend');
  assert.equal(blend.textContent, '128 / 255 blend');
  assert.doesNotMatch(blend.textContent, /opacity/i);
  assert.equal(c.find('cp-alpha').getAttribute('aria-label'), 'blend');
});

test('the popover states that blend 0 stores no tint at all', () => {
  // The standing note. At blend 0 the swatch still shows a colour, and nothing else on screen
  // explains why the sprite is untinted or why Equipped.format drops the colour entirely.
  const c = open(make());
  assert.match(c.find('cp-note').textContent, /blend 0 means no tint at all/);
  assert.match(c.find('cp-note').textContent, /not stored/);
});

test('without withAlpha there is no blend strip and no blend readout', () => {
  const c = open(make({ withAlpha: false }));
  assert.equal(c.find('cp-alpha'), null);
  assert.equal(c.find('cp-blend'), null);
  assert.equal(c.find('cp-note'), null);
  // ...and the reports carry a full blend, since there is nothing to say otherwise.
  const sv = c.find('cp-sv');
  sv.rect = { left: 0, top: 0, width: 100, height: 100 };
  fire(sv, 'mousedown', { clientX: 0, clientY: 0 });
  assert.deepEqual(c.seen, [{ r: 255, g: 255, b: 255, a: 255 }]);
});

test('the channel readout names every channel it reports', () => {
  const c = open(make());
  // Off-screen rather than gone: an input's value is announced when focus arrives on it, not when
  // a drag rewrites it, so this is still the only thing that speaks a moving colour.
  assert.equal(c.find('cp-live').textContent, 'R 164  G 51  B 31  A 128');
});

// --- editable channels ------------------------------------------------------------------------

// The four number fields, in R G B A order.
function nums(c) {
  return walk(c.node).filter((n) => n.className === 'cp-num');
}

test('the channels are editable fields, seeded with the colour', () => {
  const c = open(make());
  assert.deepEqual(nums(c).map((n) => n.value), ['164', '51', '31', '128']);
  assert.deepEqual(nums(c).map((n) => n.getAttribute('aria-label')),
    ['red', 'green', 'blue', 'blend']);
});

test('typing a channel moves the colour and reports it', () => {
  const c = open(make());
  const [r] = nums(c);

  r.value = '200';
  fire(r, 'input');

  assert.deepEqual(c.seen, [{ r: 200, g: 51, b: 31, a: 128 }]);
  // The other fields follow, and the hex with them.
  assert.deepEqual(nums(c).map((n) => n.value), ['200', '51', '31', '128']);
  assert.equal(c.find('cp-hex').value, ColorPicker.formatHex(200, 51, 31));
});

test('typing a blend is the whole point: 128 exactly, without touching the strip', () => {
  // A hex is six digits wide, so this is the one value the rest of the control cannot express.
  const c = open(make({ a: 255 }));
  const a = nums(c)[3];

  a.value = '128';
  fire(a, 'input');

  assert.equal(c.seen[0].a, 128);
  assert.equal(c.find('cp-blend').textContent, '128 / 255 blend');
});

test('a channel field clamps to a byte rather than reporting a colour that is not one', () => {
  const c = open(make());
  const [, g] = nums(c);

  g.value = '900';
  fire(g, 'input');
  assert.equal(c.seen[0].g, 255);
});

test('a half-typed channel is left alone, not read as a colour', () => {
  const c = open(make());
  const [r] = nums(c);

  // Clearing 164 to type 200 passes through ''. Snapping to black on the way is the bug.
  r.value = '';
  fire(r, 'input');
  assert.deepEqual(c.seen, [], 'an empty field must not be read as 0');

  r.value = '2';
  fire(r, 'input');
  assert.deepEqual(c.seen, [{ r: 2, g: 51, b: 31, a: 128 }]);
  assert.equal(r.value, '2', 'the field being typed in must keep its text');
});

test('a channel field left empty goes back to the real value on blur', () => {
  const c = open(make());
  const [r] = nums(c);

  r.value = '';
  fire(r, 'input');
  fire(r, 'blur');
  assert.equal(r.value, '164');
});

test('a channel edit fires one bubbling input event, not two', () => {
  const c = open(make());
  const host = document.createElement('div');
  host.appendChild(c.node);
  let seen = 0;
  host.addEventListener('input', () => { seen++; });

  const [r] = nums(c);
  r.value = '200';
  fire(r, 'input');

  // The field's own native event is stopped; fire() dispatches the real one after onChange has
  // written the caller's cells. Two would redraw the previews from stale values first.
  assert.equal(seen, 1);
});

test('the channel fields follow a movement they did not cause', () => {
  const c = open(make());
  const hex = c.find('cp-hex');

  hex.value = '#00ff00';
  fire(hex, 'input');

  assert.deepEqual(nums(c).map((n) => n.value), ['0', '255', '0', '128']);
});

test('a picker with no alpha has three channel fields, not four', () => {
  const c = open(make({ withAlpha: false }));
  assert.deepEqual(nums(c).map((n) => n.getAttribute('aria-label')), ['red', 'green', 'blue']);
});

test('a mousedown on a channel field keeps its default action, so it can take focus', () => {
  const c = open(make());
  // dispatchEvent reports !defaultPrevented, so true means the click still gets to move focus.
  assert.equal(fire(nums(c)[0], 'mousedown'), true);
  // ...where a click on the popover's own chrome is still cancelled, which is what keeps the
  // popover from closing under a click inside it.
  assert.equal(fire(c.find('cp-nums'), 'mousedown'), false);
});

// --- keyboard ---------------------------------------------------------------------------------

test('arrow keys move the SV cursor by one byte, shift by ten', () => {
  const c = open(make({ r: 100, g: 100, b: 100, a: 255 }));
  const sv = c.find('cp-sv');

  // Grey, so saturation is 0 and brightness is 100/255: right raises saturation, down lowers
  // brightness, and both do it in whole bytes.
  fire(sv, 'keydown', { key: 'ArrowDown' });
  assert.deepEqual(c.seen[0], { r: 99, g: 99, b: 99, a: 255 });

  fire(sv, 'keydown', { key: 'ArrowUp', shiftKey: true });
  assert.deepEqual(c.seen[1], { r: 109, g: 109, b: 109, a: 255 });

  fire(sv, 'keydown', { key: 'ArrowRight', shiftKey: true });
  // Saturation 10/255 at brightness 109/255, on the hue the grey kept (0).
  assert.deepEqual(c.seen[2],
    Object.assign(ColorPicker.hsvToRgb(0, 10 / 255, 109 / 255), { a: 255 }));
});

test('an arrow key on the SV square is not left to scroll the page as well', () => {
  const c = open(make());
  const sv = c.find('cp-sv');
  assert.equal(fire(sv, 'keydown', { key: 'ArrowDown' }), false, 'default was not prevented');
  // A key the control does not own is left entirely alone.
  assert.equal(fire(sv, 'keydown', { key: 'a' }), true);
});

test('arrow keys move the hue by one degree, shift by ten, and wrap', () => {
  const c = open(make({ r: 255, g: 0, b: 0, a: 255 }));
  const hue = c.find('cp-hue');

  fire(hue, 'keydown', { key: 'ArrowDown' });
  assert.equal(hue.getAttribute('aria-valuenow'), '1');
  fire(hue, 'keydown', { key: 'ArrowDown', shiftKey: true });
  assert.equal(hue.getAttribute('aria-valuenow'), '11');
  // Backwards past 0 goes round the wheel rather than sticking at red.
  fire(hue, 'keydown', { key: 'ArrowUp', shiftKey: true });
  fire(hue, 'keydown', { key: 'ArrowUp', shiftKey: true });
  assert.equal(hue.getAttribute('aria-valuenow'), '351');
  assert.equal(c.seen.length, 4);
});

test('arrow keys move the blend by one, shift by ten, and clamp at both ends', () => {
  const c = open(make({ r: 1, g: 2, b: 3, a: 250 }));
  const alpha = c.find('cp-alpha');

  fire(alpha, 'keydown', { key: 'ArrowUp' });
  assert.deepEqual(c.seen[0], { r: 1, g: 2, b: 3, a: 251 });
  fire(alpha, 'keydown', { key: 'ArrowUp', shiftKey: true });
  assert.deepEqual(c.seen[1], { r: 1, g: 2, b: 3, a: 255 });
  assert.equal(alpha.getAttribute('aria-valuetext'), '255 / 255 blend');

  for (let i = 0; i < 30; i++) fire(alpha, 'keydown', { key: 'ArrowDown', shiftKey: true });
  assert.equal(c.seen[c.seen.length - 1].a, 0);
});

test('Escape closes the popover and hands focus back to the swatch', () => {
  const c = open(make());
  assert.equal(fire(c.find('cp-sv'), 'keydown', { key: 'Escape' }), false);
  assert.equal(c.pop.hidden, true);
  assert.equal(c.swatch.getAttribute('aria-expanded'), 'false');
  assert.equal(c.swatch.focusCalls, 1);
});

test('Enter closes the popover too', () => {
  const c = open(make());
  fire(c.find('cp-hex'), 'keydown', { key: 'Enter' });
  assert.equal(c.pop.hidden, true);
  assert.equal(c.swatch.focusCalls, 1);
});

// --- hex field --------------------------------------------------------------------------------

test('the hex field is a real text input, seeded with the current colour', () => {
  const c = open(make());
  const hex = c.find('cp-hex');
  assert.equal(hex.tagName, 'INPUT');
  assert.equal(hex.getAttribute('type'), 'text');
  assert.equal(hex.value, '#a4331f');
});

test('typing a hex reports it', () => {
  const c = open(make());
  const hex = c.find('cp-hex');
  hex.value = '#0080ff';
  fire(hex, 'input');
  assert.deepEqual(c.seen, [{ r: 0, g: 128, b: 255, a: 128 }]);
  assert.equal(c.swatch.getAttribute('data-color'), '#0080ff');
  // The text is left exactly as typed rather than rewritten under the caret.
  assert.equal(hex.value, '#0080ff');
});

test('a half-typed hex changes nothing', () => {
  const c = open(make());
  const hex = c.find('cp-hex');
  hex.value = '#008';
  fire(hex, 'input');
  assert.deepEqual(c.seen, []);
  assert.equal(c.swatch.getAttribute('data-color'), '#a4331f');
});

test('a hex with no hue of its own leaves the hue strip where it was', () => {
  // Dimming a colour to black and back must not snap the strip to red: black reports hue 0, and
  // adopting that reading would lose the user's place on the wheel.
  const c = open(make({ r: 0, g: 255, b: 0, a: 255 }));
  const hue = c.find('cp-hue');
  assert.equal(hue.getAttribute('aria-valuenow'), '120');

  const hex = c.find('cp-hex');
  hex.value = '#000000';
  fire(hex, 'input');
  assert.equal(hue.getAttribute('aria-valuenow'), '120');
});

// --- outside click ----------------------------------------------------------------------------

test('a mousedown outside the control closes the popover', () => {
  const c = open(make());
  const elsewhere = document.body.appendChild(document.createElement('div'));
  fire(elsewhere, 'mousedown');
  assert.equal(c.pop.hidden, true);
  // Not a focus move: the user is on their way somewhere else, and stealing focus back would
  // fight the click that is about to land.
  assert.equal(c.swatch.focusCalls, 0);
});

test('a mousedown inside the popover leaves it open', () => {
  const c = open(make());
  fire(c.find('cp-fields'), 'mousedown');
  assert.equal(c.pop.hidden, false);
});

test('a mousedown on the popover chrome is cancelled, so focus never leaves for a blur', () => {
  // Pickers.fkControl's technique: moving focus is the DEFAULT ACTION of mousedown, so cancelling
  // it keeps focus where it is and the click lands on a popover that is still there.
  const c = open(make());
  assert.equal(fire(c.find('cp-fields'), 'mousedown'), false);
  // ...but NOT on the parts that must be focusable, or the arrow keys would have nothing to act
  // on and the hex field could not be clicked into at all.
  assert.equal(fire(c.find('cp-sv'), 'mousedown'), true);
  assert.equal(fire(c.find('cp-hue'), 'mousedown'), true);
  assert.equal(fire(c.find('cp-alpha'), 'mousedown'), true);
  assert.equal(fire(c.find('cp-hex'), 'mousedown'), true);
});

// --- recent colours ---------------------------------------------------------------------------

test('closing the popover remembers the colour, most recent first and deduplicated', () => {
  const c = make();
  const hex = () => c.find('cp-hex');

  open(c);
  hex().value = '#111111';
  fire(hex(), 'input');
  fire(c.swatch, 'click');

  open(c);
  hex().value = '#222222';
  fire(hex(), 'input');
  fire(c.swatch, 'click');

  open(c);
  hex().value = '#111111';
  fire(hex(), 'input');
  fire(c.swatch, 'click');

  assert.deepEqual(ColorPicker.recent(), ['#111111', '#222222']);
});

test('opening and closing without touching anything remembers nothing', () => {
  // The list is module-global, shared by every control on the page, and Task 5 puts six of these
  // in one form. A designer opening six slot pickers just to LOOK would otherwise evict the whole
  // list and replace it with eight colours they never chose.
  const c = make();
  open(c);
  fire(c.swatch, 'click');
  assert.deepEqual(ColorPicker.recent(), []);

  // Nor does closing by clicking away, or by Escape. (Not deepEqual: an outside click closes
  // every open popover on the page, including any a previous test left up.)
  open(c);
  fire(document, 'mousedown');
  open(c);
  fire(c.pop, 'keydown', { key: 'Escape' });
  assert.ok(ColorPicker.recent().indexOf('#a4331f') === -1, 'the seeded colour was remembered');
});

test('a control that was moved remembers its colour on close', () => {
  // The other direction: any live movement is a choice, so the colour is worth keeping.
  const c = make();
  open(c);
  const sv = c.find('cp-sv');
  sv.rect = { left: 0, top: 0, width: 100, height: 100 };
  fire(sv, 'mousedown', { clientX: 100, clientY: 0 });
  fire(c.swatch, 'click');
  assert.deepEqual(ColorPicker.recent(), [c.swatch.getAttribute('data-color')]);
});

test('the recent list keeps the last eight and no more', () => {
  const c = make();
  for (let i = 0; i < 12; i++) {
    open(c);
    const hex = c.find('cp-hex');
    hex.value = '#0000' + String(i).padStart(2, '0');
    fire(hex, 'input');
    fire(c.swatch, 'click');
  }
  assert.equal(ColorPicker.RECENT_MAX, 8);
  assert.equal(ColorPicker.recent().length, 8);
  assert.equal(ColorPicker.recent()[0], '#000011');
});

test('the recent list is shared between controls and is memory only', () => {
  const first = make();
  open(first);
  const hex = first.find('cp-hex');
  hex.value = '#abcdef';
  fire(hex, 'input');
  fire(first.swatch, 'click');

  // The list lives in the module, not in storage: a Sheets sidebar iframe can THROW on storage
  // access, and a picker that dies on that exception is worse than one that forgets. Nothing here
  // reads any, which is why a second control built from scratch already has the colour.
  assert.doesNotMatch(source, /(localStorage|sessionStorage)\s*[.[]/);

  const second = ColorPicker.control({ r: 0, g: 0, b: 0, withAlpha: true });
  fire(second.node.querySelector('[class="swatch"]'), 'click');
  const chips = second.node.querySelectorAll('[class="cp-recent-chip"]');
  assert.equal(chips.length, 1);
  assert.equal(chips[0].getAttribute('data-color'), '#abcdef');
});

test('clicking a recent colour reports it and keeps the blend', () => {
  const c = make();
  open(c);
  const hex = c.find('cp-hex');
  hex.value = '#abcdef';
  fire(hex, 'input');
  fire(c.swatch, 'click');
  open(c);

  c.seen.length = 0;
  fire(c.find('cp-recent').children[0], 'click');
  assert.deepEqual(c.seen, [{ r: 171, g: 205, b: 239, a: 128 }]);
});

test('a control can ask for a popover anchored to its right edge', () => {
  // The popover is ~182px wide and hangs off the LEFT of the swatch, which is right for a control
  // at the left of its column (.rgba) and wrong for one in the last, auto-sized track of an
  // .equip-slot row, where there is no 182px to the right of the swatch. The stylesheet keys off
  // this attribute; nothing in the JS reads it back.
  assert.equal(make().node.getAttribute('data-align'), null);
  assert.equal(make({ align: 'right' }).node.getAttribute('data-align'), 'right');
});

// --- the visible cursors ----------------------------------------------------------------------
//
// The dots are the only part of the control that says WHERE the current colour is. Nothing else
// in this file looks at them, so an offset here — a percentage against the wrong maximum, or a
// centring margin that only half applies — is invisible to every other test.

// In document order: the SV cursor, the hue cursor, and (with a blend strip) the blend cursor.
function dots(c) {
  return c.node.querySelectorAll('[class="cp-dot"]');
}

// A percentage as a number. The hue axis divides by 359 what it multiplied by 359, so the string
// is within float noise of the round number rather than equal to it.
function pct(value) {
  assert.match(value, /^-?[0-9.e+-]+%$/);
  return parseFloat(value);
}

function near(value, want) {
  assert.ok(Math.abs(pct(value) - want) < 1e-6, `${value} is not ${want}%`);
}

test('the strip cursors are centred across their strips, not hung off the left edge', () => {
  // .cp-dot pulls itself back by half its width in BOTH axes, which is only a centring if both
  // axes are positioned. A strip dot with no `left` resolves to x=0 and straddles the border.
  const c = open(make());
  const [, hueDot, alphaDot] = dots(c);
  assert.equal(hueDot.style.left, '50%');
  assert.equal(alphaDot.style.left, '50%');
});

test('a click on the SV square moves the SV cursor to the point clicked', () => {
  const c = open(make());
  const sv = c.find('cp-sv');
  sv.rect = { left: 0, top: 0, width: 100, height: 100 };

  fire(sv, 'mousedown', { clientX: 25, clientY: 75 });
  const [svDot] = dots(c);
  // Saturation runs left to right, brightness bottom to top: a quarter across is 25%, and three
  // quarters DOWN is a brightness of 0.25, which is 75% from the top.
  near(svDot.style.left, 25);
  near(svDot.style.top, 75);

  fire(sv, 'mousedown', { clientX: 100, clientY: 0 });
  near(svDot.style.left, 100);
  near(svDot.style.top, 0);
});

test('a click on the hue strip moves the hue cursor to the point clicked', () => {
  const c = open(make());
  const hue = c.find('cp-hue');
  hue.rect = { left: 0, top: 0, width: 14, height: 100 };
  const hueDot = dots(c)[1];

  // The strip is painted across 0..359, so the cursor must be measured against the same 359 —
  // dividing by 360 instead leaves the dot short of where the click landed.
  fire(hue, 'mousedown', { clientY: 40, clientX: 0 });
  assert.equal(hue.getAttribute('aria-valuenow'), String(Math.round(0.4 * 359)));
  near(hueDot.style.top, 40);

  fire(hue, 'mousedown', { clientY: 100, clientX: 0 });
  near(hueDot.style.top, 100);
  fire(hue, 'mousedown', { clientY: 0, clientX: 0 });
  near(hueDot.style.top, 0);
});

test('the hue cursor stays on the strip when the keyboard wraps past the last degree', () => {
  // ArrowDown wraps modulo 360, so state.h can sit between 359 and 360 — above the 359 the strip
  // is painted with. The dot must stop at the bottom rather than hang below it.
  const c = open(make());
  const hue = c.find('cp-hue');
  hue.rect = { left: 0, top: 0, width: 14, height: 100 };
  fire(hue, 'mousedown', { clientY: 100, clientX: 0 }); // 359
  fire(hue, 'keydown', { key: 'ArrowDown', shiftKey: true }); // 369 % 360 -> 9
  fire(hue, 'keydown', { key: 'ArrowUp' }); // 8
  fire(hue, 'keydown', { key: 'ArrowUp', shiftKey: true }); // -2 -> 358
  fire(hue, 'keydown', { key: 'ArrowDown' }); // 359

  near(dots(c)[1].style.top, 100);
  // ...and the announced value never exceeds the maximum it is announced against.
  assert.equal(hue.getAttribute('aria-valuemax'), '359');
  assert.ok(Number(hue.getAttribute('aria-valuenow')) <= 359);
});

test('aria-valuenow never rounds past aria-valuemax', () => {
  const c = open(make());
  const hue = c.find('cp-hue');
  hue.rect = { left: 0, top: 0, width: 14, height: 1000 };
  // 358.64 degrees, then one ArrowDown: 359.64, which Math.round alone announces as 360 against
  // a maximum of 359.
  fire(hue, 'mousedown', { clientY: 999, clientX: 0 });
  fire(hue, 'keydown', { key: 'ArrowDown' });
  assert.equal(hue.getAttribute('aria-valuenow'), '359');
});

test('a click on the blend strip moves the blend cursor to the point clicked', () => {
  const c = open(make());
  const alpha = c.find('cp-alpha');
  alpha.rect = { left: 0, top: 0, width: 14, height: 100 };
  const alphaDot = dots(c)[2];

  // Full blend at the top, none at the bottom.
  fire(alpha, 'mousedown', { clientY: 0, clientX: 0 });
  assert.equal(alpha.getAttribute('aria-valuenow'), '255');
  near(alphaDot.style.top, 0);

  fire(alpha, 'mousedown', { clientY: 100, clientX: 0 });
  assert.equal(alpha.getAttribute('aria-valuenow'), '0');
  near(alphaDot.style.top, 100);
});

// --- structure --------------------------------------------------------------------------------

test('everything in the popover is inside the wrapper', () => {
  // The popover is absolutely positioned; a node outside the wrapper would have no positioned
  // ancestor and would anchor to the page instead of to the swatch.
  const c = open(make());
  const nodes = walk(c.node);
  assert.ok(nodes.length > 5);
  nodes.forEach((n) => {
    for (let at = n; at; at = at.parentNode) if (at === c.node) return;
    assert.fail('a node escaped the wrapper');
  });
});

test('the outside-click listener lives only as long as the popover is open', () => {
  // app.js rebuilds the whole form for every record opened. A document listener left behind would
  // outlive its control, hold the detached wrapper alive, and stack up one per tint field per
  // record for as long as the sidebar is up.
  const before = (document._listeners.get('mousedown') || []).length;
  const c = make();
  assert.equal((document._listeners.get('mousedown') || []).length, before);

  open(c);
  assert.equal((document._listeners.get('mousedown') || []).length, before + 1);

  fire(c.swatch, 'click');
  assert.equal((document._listeners.get('mousedown') || []).length, before);
});
