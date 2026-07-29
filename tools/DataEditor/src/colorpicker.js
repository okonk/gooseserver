// A colour picker of our own, because the native <input type="color"> opens the OS picker — slow,
// modal, and with nowhere to put the one control this editor actually needs beside the colour: the
// BLEND FACTOR.
//
// THE ALPHA CHANNEL HERE IS NOT OPACITY. Icon.cs:9-11 is
//   COLOR.rgb = mix(sprite.rgb, tint.rgb, tint.a)
// so it is how far the sprite is dragged towards the tint colour, and it never touches the
// sprite's own transparency. Everything in this file says "blend" for that reason, and the blend
// strip is painted through Sprites.applyTint rather than through a checkerboard so the strip and
// the sprite previews cannot disagree about what a factor of 128 looks like.
//
// AND A FACTOR OF ZERO MEANS NO TINT AT ALL, WITH THE COLOUR NOT STORED. Equipped.format collapses
// a zero-alpha slot to `id,*` and Composites.rgbaControl keeps r/g/b in the cells while the game
// ignores them. The popover says so, in the control, because the swatch at blend 0 still shows a
// colour and there is nothing else on screen to explain why the sprite is untinted.
var ColorPicker = (function () {
  // Logical == backing-store pixels: these are gradients, not sprite art, so there is nothing to
  // scale up and Sprites.scaled has no part to play.
  var SV_SIZE = 128;
  var STRIP_W = 14;
  var STRIP_H = 128;

  // The last hue the strip can show. ONE number for three jobs — the hue the bottom pixel is
  // painted, the hue a click at the bottom reads, and the fraction the cursor sits at — because
  // three separate spellings of "the end of the strip" are three things to keep in step.
  var HUE_MAX = 359;

  // Session memory only, and deliberately not localStorage: the editor also runs inside a Sheets
  // sidebar iframe, where storage access can THROW rather than return null. A picker that dies on
  // a storage exception is worse than one that forgets.
  var RECENT_MAX = 8;
  var recent = [];

  // The stand-in for the sprite the blend strip is blending against. Mid grey rather than a
  // checkerboard: a checkerboard is the idiom for OPACITY, and reading the strip that way is the
  // exact misunderstanding this control exists to prevent.
  var BLEND_BASE = [128, 128, 128, 255];

  // parseInt(v, 10), matching Composites.num(), Sprites.num() and the rest, so no two modules can
  // disagree about what a cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  function channel(value) {
    return Math.min(255, Math.max(0, num(value)));
  }

  function clamp01(n) {
    if (!(n > 0)) return 0; // also catches NaN
    return n > 1 ? 1 : n;
  }

  // --- pure colour maths ----------------------------------------------------------------------
  //
  // rgbToHsv and hsvToRgb ROUND-TRIP EXACTLY over the byte cube, and that is a rule rather than a
  // happy accident: every drag of the SV square converts one way and every repaint converts back,
  // so a converter that drifted by one would walk a stored colour away from itself while the user
  // did nothing but open the popover. The greyscale axis is where naive implementations fail —
  // hue is undefined there, and any hue at all must give the same bytes back.

  // h is degrees in [0, 360), s and v are 0..1. h is 0 for a grey, which is a CHOICE, not a
  // reading: callers that have a hue worth keeping (the strip's position) must keep it themselves.
  function rgbToHsv(r, g, b) {
    var R = channel(r), G = channel(g), B = channel(b);
    var max = Math.max(R, G, B);
    var min = Math.min(R, G, B);
    var d = max - min;
    var h = 0;

    if (d !== 0) {
      if (max === R) h = 60 * (((G - B) / d) % 6);
      else if (max === G) h = 60 * (((B - R) / d) + 2);
      else h = 60 * (((R - G) / d) + 4);
      if (h < 0) h += 360;
    }

    return { h: h, s: max === 0 ? 0 : d / max, v: max / 255 };
  }

  // The inverse, in bytes. The rounding is what absorbs the float error the two conversions
  // accumulate: every intermediate is within ~1e-13 of an exact multiple of 1/255.
  function hsvToRgb(h, s, v) {
    var S = clamp01(s);
    var V = clamp01(v);
    var hue = h % 360;
    if (hue < 0) hue += 360;

    var c = V * S;
    var x = c * (1 - Math.abs(((hue / 60) % 2) - 1));
    var m = V - c;
    var sector = Math.floor(hue / 60) % 6;

    var trio = [[c, x, 0], [x, c, 0], [0, c, x], [0, x, c], [x, 0, c], [c, 0, x]][sector];
    return {
      r: Math.round((trio[0] + m) * 255),
      g: Math.round((trio[1] + m) * 255),
      b: Math.round((trio[2] + m) * 255),
    };
  }

  // null, not black, for anything that is not six hex digits: "I could not read this" and "this is
  // black" are different answers, and a half-typed hex field must not repaint the control. This
  // pair is the editor's ONLY definition of a hex colour — composites.js had a duplicate, and two
  // answers to "what is a hex colour" in one editor is one too many.
  function parseHex(text) {
    var m = /^#?([0-9a-fA-F]{6})$/.exec(
      (text === undefined || text === null ? '' : String(text)).trim());
    if (!m) return null;
    return {
      r: parseInt(m[1].slice(0, 2), 16),
      g: parseInt(m[1].slice(2, 4), 16),
      b: parseInt(m[1].slice(4, 6), 16),
    };
  }

  // Clamped, then padded to exactly two digits: 0 renders as one digit and 300 as three, and
  // taking the last two characters of either keeps the WRONG end.
  function formatHex(r, g, b) {
    function pair(value) {
      var text = channel(value).toString(16);
      return text.length < 2 ? '0' + text : text;
    }
    return '#' + pair(r) + pair(g) + pair(b);
  }

  // Where a pointer landed inside an element, as two fractions. The whole point is that the DOM
  // handlers do nothing but call this with getBoundingClientRect() — so all the arithmetic that
  // can be wrong is here, and is testable without a layout engine.
  //
  // A zero-width or zero-height rect reads as 0 rather than as NaN or Infinity. That is not
  // hypothetical: an element inside a hidden popover measures 0x0 in a real browser.
  function pointToValue(rect, clientX, clientY) {
    var r = rect || {};
    var w = r.width || 0;
    var h = r.height || 0;
    return {
      x: w ? clamp01((clientX - (r.left || 0)) / w) : 0,
      y: h ? clamp01((clientY - (r.top || 0)) / h) : 0,
    };
  }

  // --- the control ----------------------------------------------------------------------------

  function el(tag, attrs, text) {
    return Forms.el(tag, attrs, text);
  }

  // Fills a canvas from a per-pixel function. getImageData/putImageData rather than a CSS
  // gradient: the gradient a browser interpolates is not the colour this code would compute, and
  // only pixels can be asserted on.
  function paint(canvas, w, h, pixel) {
    var ctx = canvas.getContext('2d');
    // createImageData, not getImageData: every one of the four bytes of every pixel is written
    // below, so reading the old frame back off the GPU is a copy nobody looks at.
    var image = ctx.createImageData(w, h);
    var data = image.data;
    for (var y = 0; y < h; y++) {
      for (var x = 0; x < w; x++) {
        var at = (y * w + x) * 4;
        var px = pixel(x, y);
        data[at] = px[0];
        data[at + 1] = px[1];
        data[at + 2] = px[2];
        data[at + 3] = 255;
      }
    }
    ctx.putImageData(image, 0, 0);
  }

  // Adds a colour to the shared session list, most recent first, deduplicated on the hex. Alpha
  // is deliberately NOT part of the identity: the list answers "which colours have I used", and a
  // blend factor is a property of the slot being tinted, not of the colour.
  function remember(hex) {
    var at = recent.indexOf(hex);
    if (at !== -1) recent.splice(at, 1);
    recent.unshift(hex);
    if (recent.length > RECENT_MAX) recent.length = RECENT_MAX;
  }

  // True when `node` is `container` or anything inside it. Walks parentNode rather than using
  // Node.contains, which the test DOM does not model — the same helper, for the same reason, as
  // Pickers.within.
  function within(container, node) {
    for (var at = node; at; at = at.parentNode) {
      if (at === container) return true;
    }
    return false;
  }

  // ColorPicker.control({ r, g, b, a, withAlpha, align, onChange }) -> { node, set }
  //
  // `node` is the WRAPPER, not the swatch button: the popover is absolutely positioned and needs a
  // positioned ancestor of its own (.rgba is a plain block), and a <button> cannot legally contain
  // one. The swatch is node's first child and carries class="swatch".
  //
  // onChange({r,g,b,a}) fires on EVERY live movement and on nothing else — never at construction,
  // and never from set(). That is what lets Composites.rgbaControl keep its rule that an untouched
  // control does not rewrite its cells: no interaction, no call, no write.
  function control(options) {
    var opts = options || {};
    var withAlpha = !!opts.withAlpha;
    var onChange = typeof opts.onChange === 'function' ? opts.onChange : null;

    // Hue is kept OUTSIDE the rgb round trip on purpose. Drag the SV cursor into the black corner
    // and rgbToHsv can only report hue 0, so a state rebuilt from rgb each time would snap the hue
    // strip to red the moment a colour went black or grey and lose the user's place.
    var state = { h: 0, s: 0, v: 0, a: 255 };
    var open = false;
    // Whether anything has MOVED this control. The recent list is module-global and shared by
    // every picker on the page, so a form of six of them, opened and closed just to look, must
    // not evict eight remembered colours in favour of eight seeded ones.
    var touched = false;
    // The hue the SV square was last painted at, so a drag inside the square does not repaint it.
    // NaN never equals anything, so the first paint always happens.
    var paintedHue = NaN;

    var wrap = el('div', { class: 'colorpicker' });
    // The popover hangs off the LEFT of the swatch by default, which is right for a control at
    // the left of its column and wrong for one in the last track of a row — there is no CSS for
    // "am I about to overflow", so the caller that knows says so. Read only by the stylesheet.
    if (opts.align === 'right') wrap.setAttribute('data-align', 'right');
    var swatch = el('button', {
      type: 'button',
      class: 'swatch',
      'aria-haspopup': 'dialog',
      'aria-expanded': 'false',
    });

    var pop = el('div', { class: 'cp-pop', role: 'dialog', 'aria-label': 'colour picker' });
    pop.hidden = true;

    var sv = el('canvas', {
      class: 'cp-sv', width: SV_SIZE, height: SV_SIZE, tabindex: '0',
      // There is no ARIA role for a two-dimensional colour field — role=slider describes one
      // value, not two. So the square is an application region with an explicit instruction, and
      // the live readout below is what actually announces the numbers as they change.
      role: 'application',
      'aria-label': 'saturation and brightness, arrow keys adjust',
    });
    var svDot = el('div', { class: 'cp-dot' });
    var svWrap = el('div', { class: 'cp-field' });
    svWrap.appendChild(sv);
    svWrap.appendChild(svDot);

    var hue = el('canvas', {
      class: 'cp-hue', width: STRIP_W, height: STRIP_H, tabindex: '0',
      role: 'slider', 'aria-label': 'hue', 'aria-valuemin': '0',
      'aria-valuemax': String(HUE_MAX),
    });
    var hueDot = el('div', { class: 'cp-dot' });
    // The strips are one-dimensional, so only `top` ever moves — but .cp-dot centres itself by
    // pulling back half its width in BOTH axes, and a dot with no `left` at all resolves to x=0
    // and straddles the strip's left border. Stated here rather than in the stylesheet because
    // it is the same "where is the cursor" arithmetic as the `top` beside it.
    hueDot.style.left = '50%';
    var hueWrap = el('div', { class: 'cp-field' });
    hueWrap.appendChild(hue);
    hueWrap.appendChild(hueDot);

    var alpha = null, alphaDot = null, alphaWrap = null;
    if (withAlpha) {
      alpha = el('canvas', {
        class: 'cp-alpha', width: STRIP_W, height: STRIP_H, tabindex: '0',
        // "blend", never "opacity" or "alpha", including here: this label is the only description
        // a screen reader gets of what the strip does.
        role: 'slider', 'aria-label': 'blend', 'aria-valuemin': '0', 'aria-valuemax': '255',
      });
      alphaDot = el('div', { class: 'cp-dot' });
      alphaDot.style.left = '50%';
      alphaWrap = el('div', { class: 'cp-field' });
      alphaWrap.appendChild(alpha);
      alphaWrap.appendChild(alphaDot);
    }

    // A real text input, so a hex can be typed OR PASTED — the one thing every other part of this
    // control cannot do, and the reason a designer can carry a colour in from anywhere else.
    var hex = el('input', { type: 'text', class: 'cp-hex', autocomplete: 'off',
                            'aria-label': 'hex colour', maxlength: '7' });
    // aria-live, because the numbers are what the SV square cannot announce for itself.
    var channels = el('span', { class: 'cp-channels', 'aria-live': 'polite' });
    var blend = withAlpha ? el('span', { class: 'cp-blend' }) : null;

    var fields = el('div', { class: 'cp-fields' });
    fields.appendChild(hex);
    fields.appendChild(channels);
    if (blend) {
      fields.appendChild(blend);
      // Its own class, not the form's .hint: rgbaControl puts a .hint of its own (the four
      // column names) beside this control, and one selector finding both is one of them lost.
      fields.appendChild(el('div', { class: 'cp-note' },
        'blend 0 means no tint at all — the colour is not stored'));
    }

    var recentRow = el('div', { class: 'cp-recent' });

    var grid = el('div', { class: 'cp-grid' });
    grid.appendChild(svWrap);
    grid.appendChild(hueWrap);
    if (alphaWrap) grid.appendChild(alphaWrap);
    grid.appendChild(fields);
    pop.appendChild(grid);
    pop.appendChild(recentRow);

    wrap.appendChild(swatch);
    wrap.appendChild(pop);

    function rgb() {
      return hsvToRgb(state.h, state.s, state.v);
    }

    function currentHex() {
      var c = rgb();
      return formatHex(c.r, c.g, c.b);
    }

    function fire() {
      touched = true;
      if (!onChange) return;
      var c = rgb();
      onChange({ r: c.r, g: c.g, b: c.b, a: state.a });
    }

    function paintSv() {
      if (paintedHue === state.h) return;
      paintedHue = state.h;
      paint(sv, SV_SIZE, SV_SIZE, function (x, y) {
        var c = hsvToRgb(state.h, x / (SV_SIZE - 1), 1 - y / (SV_SIZE - 1));
        return [c.r, c.g, c.b];
      });
    }

    // Hue 0 at the TOP, increasing downwards — which is what makes ArrowDown mean "further round
    // the wheel" and also mean "down the strip", rather than one of the two.
    function paintHue() {
      paint(hue, STRIP_W, STRIP_H, function (x, y) {
        var c = hsvToRgb((y / (STRIP_H - 1)) * HUE_MAX, 1, 1);
        return [c.r, c.g, c.b];
      });
    }

    // The blend strip, through the SAME function the sprite previews tint with. Full blend at the
    // top (more is up), no tint at the bottom, where the strip is the bare grey stand-in — which
    // is the honest picture of what a factor of 0 does.
    function paintAlpha() {
      var c = rgb();
      paint(alpha, STRIP_W, STRIP_H, function (x, y) {
        var f = Math.round((1 - y / (STRIP_H - 1)) * 255);
        return Sprites.applyTint(BLEND_BASE, { r: c.r, g: c.g, b: c.b, a: f });
      });
    }

    function renderRecent() {
      recentRow.innerHTML = '';
      recent.forEach(function (colour) {
        var chip = el('button', {
          type: 'button', class: 'cp-recent-chip', 'data-color': colour,
          'aria-label': 'use ' + colour,
          // Focusable by default, and Tab through eight of them to leave the popover is worse
          // than reaching them with the mouse; the same call Pickers.fkControl makes about its
          // result rows.
          tabindex: '-1',
        });
        chip.style.background = colour;
        chip.addEventListener('click', function () {
          apply(parseHex(colour), true);
        });
        recentRow.appendChild(chip);
      });
    }

    // Repaints everything the state can affect. `keepHexText` is true when the hex FIELD is what
    // changed, in which case its text is left exactly as typed — rewriting it mid-keystroke would
    // fight the user for the caret.
    function refresh(keepHexText) {
      var c = rgb();
      var text = formatHex(c.r, c.g, c.b);

      swatch.setAttribute('data-color', text);
      swatch.style.background = text;
      swatch.setAttribute('aria-label', withAlpha
        ? 'colour ' + text + ', ' + state.a + ' / 255 blend'
        : 'colour ' + text);

      if (!keepHexText) hex.value = text;
      channels.textContent = 'R ' + c.r + '  G ' + c.g + '  B ' + c.b
        + (withAlpha ? '  A ' + state.a : '');
      if (blend) blend.textContent = state.a + ' / 255 blend';

      // Clamped to the strip's own maximum in BOTH places. The keyboard wraps modulo 360, so
      // state.h can sit between HUE_MAX and 360: unclamped that announces a 360 against an
      // aria-valuemax of 359, and hangs the cursor off the bottom of the strip.
      var hueAt = state.h > HUE_MAX ? 1 : state.h / HUE_MAX;
      hue.setAttribute('aria-valuenow', String(Math.min(HUE_MAX, Math.round(state.h))));
      hueDot.style.top = hueAt * 100 + '%';
      svDot.style.left = state.s * 100 + '%';
      svDot.style.top = (1 - state.v) * 100 + '%';
      if (alpha) {
        alpha.setAttribute('aria-valuenow', String(state.a));
        alpha.setAttribute('aria-valuetext', state.a + ' / 255 blend');
        alphaDot.style.top = (1 - state.a / 255) * 100 + '%';
      }

      // Painting a hidden popover is work no one can see, and in a browser it is work against
      // canvases that have not been laid out yet.
      if (!open) return;
      paintSv();
      if (alpha) paintAlpha();
    }

    // Adopts an rgb triple, KEEPING THE CURRENT HUE when the colour is one that has no hue of its
    // own. Black, white and every grey report hue 0, and taking that reading would drag the hue
    // strip to red every time a user dimmed a colour to nothing.
    function adopt(c) {
      var hsv = rgbToHsv(c.r, c.g, c.b);
      state.s = hsv.s;
      state.v = hsv.v;
      if (hsv.s > 0 && hsv.v > 0) state.h = hsv.h;
    }

    function apply(c, notify) {
      if (!c) return;
      adopt(c);
      refresh(false);
      if (notify) fire();
    }

    // Outside click. Registered only WHILE OPEN and dropped on close, because app.js rebuilds the
    // whole form for every record opened: a listener left on the document would outlive its
    // control, hold the detached wrapper alive, and accumulate one per tint field per record for
    // as long as the sidebar stays up.
    function outside(event) {
      if (within(wrap, event.target)) return;
      setOpen(false);
    }

    function setOpen(next) {
      open = !!next;
      pop.hidden = !open;
      swatch.setAttribute('aria-expanded', open ? 'true' : 'false');
      if (open) {
        document.addEventListener('mousedown', outside);
        renderRecent();
        refresh(false);
        return;
      }
      document.removeEventListener('mousedown', outside);
      // Closing is the commit point for the recent list — doing it per movement would fill all
      // eight slots from a single drag across the square — but only for a control that was
      // actually moved.
      if (touched) remember(currentHex());
    }

    function close() {
      if (!open) return;
      setOpen(false);
      swatch.focus();
    }

    // --- pointer ------------------------------------------------------------------------------
    //
    // Every handler is the same three lines: measure, convert, apply. pointToValue holds all the
    // arithmetic, so nothing that can be wrong needs a layout engine to test.

    function fromSv(event) {
      var at = pointToValue(sv.getBoundingClientRect(), event.clientX, event.clientY);
      state.s = at.x;
      state.v = 1 - at.y;
      refresh(false);
      fire();
    }

    function fromHueStrip(event) {
      var at = pointToValue(hue.getBoundingClientRect(), event.clientX, event.clientY);
      state.h = at.y * HUE_MAX;
      refresh(false);
      fire();
    }

    function fromAlphaStrip(event) {
      var at = pointToValue(alpha.getBoundingClientRect(), event.clientX, event.clientY);
      state.a = Math.round((1 - at.y) * 255);
      refresh(false);
      fire();
    }

    // Drag: the move and release listeners go on the DOCUMENT, not the canvas, so a pointer that
    // leaves the little square mid-drag keeps steering it instead of stopping dead at the edge.
    function drags(node, handler) {
      node.addEventListener('mousedown', function (event) {
        handler(event);

        function move(e) { handler(e); }
        function up() {
          document.removeEventListener('mousemove', move);
          document.removeEventListener('mouseup', up);
        }
        document.addEventListener('mousemove', move);
        document.addEventListener('mouseup', up);
      });
    }

    drags(sv, fromSv);
    drags(hue, fromHueStrip);
    if (alpha) drags(alpha, fromAlphaStrip);

    // --- keyboard -----------------------------------------------------------------------------

    // Shift is the coarse step everywhere, and every step is a whole BYTE (or a whole degree of
    // hue), so arrowing lands on values a designer can also type into the hex field.
    function step(event) {
      return event.shiftKey ? 10 : 1;
    }

    function arrow(event) {
      var k = event.key;
      if (k === 'ArrowLeft') return { x: -1, y: 0 };
      if (k === 'ArrowRight') return { x: 1, y: 0 };
      if (k === 'ArrowUp') return { x: 0, y: 1 };
      if (k === 'ArrowDown') return { x: 0, y: -1 };
      return null;
    }

    // Quantised to 1/255 on every press: s and v are floats carried straight from the pointer, and
    // adding 1/255 to 0.6274509803921569 forever would leave the arrow keys unable to reach a
    // round byte at all.
    function nudge(value, delta) {
      return clamp01((Math.round(value * 255) + delta) / 255);
    }

    sv.addEventListener('keydown', function (event) {
      var move = arrow(event);
      if (!move) return;
      // Otherwise the arrow ALSO scrolls the sidebar out from under the popover.
      event.preventDefault();
      state.s = nudge(state.s, move.x * step(event));
      state.v = nudge(state.v, move.y * step(event));
      refresh(false);
      fire();
    });

    hue.addEventListener('keydown', function (event) {
      var move = arrow(event);
      if (!move) return;
      event.preventDefault();
      // Down and right go FORWARD round the wheel, matching the strip, which is painted with 0 at
      // the top. Wraps, because hue does.
      var next = (state.h + (move.x - move.y) * step(event)) % 360;
      state.h = next < 0 ? next + 360 : next;
      refresh(false);
      fire();
    });

    if (alpha) {
      alpha.addEventListener('keydown', function (event) {
        var move = arrow(event);
        if (!move) return;
        event.preventDefault();
        state.a = channel(state.a + (move.x + move.y) * step(event));
        refresh(false);
        fire();
      });
    }

    // Escape and Enter both close and hand focus back to the swatch. Neither reverts: this control
    // reports every movement live, so the caller has already written the cells and "cancel" would
    // be a promise it cannot keep.
    pop.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' || event.key === 'Enter') {
        event.preventDefault();
        close();
      }
    });

    hex.addEventListener('input', function () {
      var c = parseHex(hex.value);
      // A half-typed hex is not a colour. Repainting from `null` would blank the control between
      // the third and sixth character.
      if (!c) return;
      adopt(c);
      refresh(true);
      fire();
    });

    swatch.addEventListener('click', function () { setOpen(!open); });

    swatch.addEventListener('keydown', function (event) {
      // A <button> already fires click for both keys in a real browser; this is here for the
      // Escape half, and to keep the two paths in one place.
      if (event.key === 'Escape' && open) {
        event.preventDefault();
        close();
      }
    });

    // CANCELLING MOUSEDOWN is what makes a click INSIDE the popover safe — moving focus is the
    // default action of mousedown, so preventing it keeps focus where it is and no blur races the
    // click, exactly as Pickers.fkControl argues. It is NOT prevented for the focusable parts
    // (the three canvases, the hex field), which need the click's default action to take focus at
    // all: arrow keys only work on a focused strip.
    pop.addEventListener('mousedown', function (event) {
      var target = event.target;
      var focusable = target === hex || (target && target.getAttribute
                                         && target.getAttribute('tabindex') === '0');
      if (!focusable) event.preventDefault();
    });

    // Seeded, then rendered — with no onChange call. Opening a record must not change it.
    adopt({ r: channel(opts.r), g: channel(opts.g), b: channel(opts.b) });
    state.a = withAlpha ? channel(opts.a) : 255;
    paintHue();
    refresh(false);

    return {
      node: wrap,
      // For a caller that has a new colour from somewhere other than this control — a record
      // reopened into the same form, say. Silent by design: set() is not a user movement, and a
      // control that called back here would rewrite cells nobody touched.
      set: function (colour) {
        var c = colour || {};
        adopt({ r: channel(c.r), g: channel(c.g), b: channel(c.b) });
        if (withAlpha && c.a !== undefined) state.a = channel(c.a);
        refresh(false);
      },
    };
  }

  return {
    control: control,
    rgbToHsv: rgbToHsv, hsvToRgb: hsvToRgb,
    parseHex: parseHex, formatHex: formatHex,
    pointToValue: pointToValue,
    // The shared session list. recent() copies so a caller cannot reorder the real one;
    // resetRecent() exists because the list outlives any single control — a test, or a form
    // rebuilt from scratch, needs a way back to empty.
    recent: function () { return recent.slice(); },
    resetRecent: function () { recent.length = 0; },
    RECENT_MAX: RECENT_MAX,
  };
})();

if (typeof module !== 'undefined') module.exports = { ColorPicker: ColorPicker };
