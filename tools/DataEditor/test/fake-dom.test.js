// The fake DOM's own tests. Everything else in this suite asserts on a MODULE and trusts the
// fake underneath it; these assert on the fake, for the handful of behaviours whose whole value
// is that they diverge from the obvious implementation — indeterminate being independent of
// checked, a rect that is all zeroes until a test says otherwise, style keys that round-trip
// rather than vanishing, and a scroll event that dispatches like every other event.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { installFakeDom, createElement, fire } from './fake-dom.js';

installFakeDom();

test('indeterminate defaults to false and is independent of checked', () => {
  const box = createElement('input');
  box.setAttribute('type', 'checkbox');
  assert.equal(box.indeterminate, false);

  box.indeterminate = true;
  // A tri-state box is indeterminate WITHOUT being ticked; the two flags never imply each other.
  assert.equal(box.indeterminate, true);
  assert.equal(box.checked, false);

  box.checked = true;
  assert.equal(box.indeterminate, true, 'ticking a box does not clear indeterminate');

  box.indeterminate = false;
  assert.equal(box.checked, true, 'clearing indeterminate does not untick a box');
});

test('a click does not clear indeterminate — that is the control\'s job', () => {
  const box = createElement('input');
  box.setAttribute('type', 'checkbox');
  box.indeterminate = true;
  fire(box, 'click');
  // A real browser clears it as a DEFAULT ACTION, and this fake has none (see fake-dom.js).
  // A control that relies on the browser doing it must clear it itself to be testable here.
  assert.equal(box.indeterminate, true);
});

test('new Event does not bubble unless the init dict asks it to', () => {
  const parent = createElement('div');
  const child = parent.appendChild(createElement('button'));
  const heard = [];
  parent.addEventListener('change', (e) => { heard.push(e.type); });

  child.dispatchEvent(new Event('change'));
  assert.deepEqual(heard, [], 'a plain Event stops at its target');

  child.dispatchEvent(new Event('change', { bubbles: true }));
  assert.deepEqual(heard, ['change']);
});

test('an Event init field the fake does not model throws rather than being ignored', () => {
  assert.throws(() => new Event('change', { composed: true }), /composed/);
});

test('getBoundingClientRect is all zeroes until a test assigns rect', () => {
  const bar = createElement('div');
  assert.deepEqual(bar.getBoundingClientRect(), {
    x: 0, y: 0, left: 0, top: 0, right: 0, bottom: 0, width: 0, height: 0,
  });

  bar.rect = { left: 20, top: 5, width: 200, height: 12 };
  const rect = bar.getBoundingClientRect();
  assert.equal(rect.left, 20);
  assert.equal(rect.top, 5);
  assert.equal(rect.width, 200);
  assert.equal(rect.height, 12);
  // The redundant fields are derived, not zero-filled: right is left + width, and x mirrors left.
  assert.equal(rect.right, 220);
  assert.equal(rect.bottom, 17);
  assert.equal(rect.x, 20);
  assert.equal(rect.y, 5);
});

test('a partial rect is still self-consistent', () => {
  const bar = createElement('div');
  // Only the two fields a horizontal track cares about. The vertical ones collapse to a
  // zero-height rect at the origin rather than contradicting the ones given.
  bar.rect = { left: 20, width: 200 };
  assert.deepEqual(bar.getBoundingClientRect(), {
    x: 20, y: 0, left: 20, top: 0, right: 220, bottom: 0, width: 200, height: 0,
  });

  // x/y are accepted as an alternative spelling of left/top.
  bar.rect = { x: 4, y: 8, width: 10, height: 20 };
  const rect = bar.getBoundingClientRect();
  assert.equal(rect.left, 4);
  assert.equal(rect.top, 8);
  assert.equal(rect.right, 14);
  assert.equal(rect.bottom, 28);
});

test('a fired pointer event carries clientX and clientY, defaulting to 0', () => {
  const track = createElement('div');
  track.rect = { left: 20, width: 200 };
  const seen = [];
  track.addEventListener('mousedown', (e) => seen.push([e.clientX, e.clientY]));

  fire(track, 'mousedown', { clientX: 120, clientY: 7 });
  fire(track, 'mousedown');
  assert.deepEqual(seen, [[120, 7], [0, 0]]);

  // What the pair is for: an offset within the element, measured against its rect.
  assert.equal(seen[0][0] - track.getBoundingClientRect().left, 100);
});

test('style keys round-trip', () => {
  const tile = createElement('div');
  assert.equal(tile.style.height, undefined);

  tile.style.height = '480px';
  tile.style.backgroundPosition = '-32px -64px';
  assert.equal(tile.style.height, '480px');
  assert.equal(tile.style.backgroundPosition, '-32px -64px');
});

test('each element gets its own style object', () => {
  const a = createElement('div');
  const b = createElement('div');
  a.style.height = '10px';
  assert.equal(b.style.height, undefined);
});

test('scrollTop, clientHeight and scrollHeight default to 0 and are assignable', () => {
  const box = createElement('div');
  assert.equal(box.scrollTop, 0);
  assert.equal(box.clientHeight, 0);
  assert.equal(box.scrollHeight, 0);

  box.scrollTop = 120;
  box.clientHeight = 300;
  box.scrollHeight = 4000;
  assert.equal(box.scrollTop, 120);
  assert.equal(box.clientHeight, 300);
  assert.equal(box.scrollHeight, 4000);
});

test('a scroll event dispatches to its own listeners and does not bubble', () => {
  const parent = createElement('div');
  const box = parent.appendChild(createElement('div'));
  const seen = [];
  box.addEventListener('scroll', (e) => seen.push(e.currentTarget));
  parent.addEventListener('scroll', (e) => seen.push(e.currentTarget));

  box.scrollTop = 40;
  assert.equal(fire(box, 'scroll'), true);
  // scroll on an element does not bubble, so the ancestor's listener must not run.
  assert.deepEqual(seen, [box]);
  // Firing a scroll event does not touch scrollTop, and assigning scrollTop fires nothing:
  // the two are independent here, so a windowing test states the geometry it means.
  assert.equal(box.scrollTop, 40);
});

test('the 2d context records setTransform and imageSmoothingEnabled', () => {
  const canvas = createElement('canvas');
  const ctx = canvas.getContext('2d');
  assert.equal(ctx.imageSmoothingEnabled, true);

  ctx.setTransform(2, 0, 0, 2, 0, 0);
  ctx.imageSmoothingEnabled = false;
  assert.equal(ctx.imageSmoothingEnabled, false);

  assert.deepEqual(ctx.calls, [
    ['setTransform', 2, 0, 0, 2, 0, 0],
    ['imageSmoothingEnabled', false],
  ]);
});
