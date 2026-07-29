// A DOM small enough to read in one sitting and faithful in the two places forms.js can be
// wrong: <select>.value refusing a value no <option> carries, and an <input>'s DIRTY VALUE
// FLAG. Everything else is the minimum the module touches.
//
// Install with installFakeDom(); it sets globalThis.document and returns it.
//
// One deliberate infidelity: querySelectorAll and getElementsByTagName return real Arrays,
// not NodeList/HTMLCollection, so .map/.filter/.find work on them HERE and would throw in a
// browser. Module code must keep using index loops.
//
// ADDED FOR TASK 9 (pickers), each because pickers.js would otherwise be untestable or, worse,
// testably wrong in a way the browser would not be:
//
//   * An EVENT MODEL — addEventListener/removeEventListener/dispatchEvent, with propagation up
//     the parentNode chain and a preventDefault that dispatchEvent reports. `focus` and `blur`
//     do NOT bubble, as in a real DOM; that is load-bearing for a picker that listens on the
//     input and on the results list at once. Use fire(node, type) to build and send one.
//   * `hidden` REFLECTED onto the content attribute. This is the fidelity that matters most
//     here: a fake where `hidden` were a plain own property would let `el('div', { hidden:
//     'hidden' })` followed by `node.hidden = false` "show" a list that a browser still hides,
//     because the attribute would still be there.
//   * `className` reflected onto the class attribute, same reason in the other direction.
//   * `removeAttribute`, which reflection needs.
//   * `width`/`height` reflected on a <canvas> AS NUMBERS — setAttribute('width', 48) really
//     does set canvas.width, and centring maths reading undefined would silently produce NaN
//     offsets that no assertion about "it drew" would catch.
//   * `getContext('2d')` on a <canvas>: a memoised recording stub (same object every call, as
//     in a browser) whose calls array is the assertion surface for Sprites.draw. Anything that
//     is not a canvas, or any type but '2d', returns null.
//
// ADDED FOR TASK 10 (composites):
//
//   * CHECKEDNESS — `.checked` as the IDL attribute, distinct from the `checked` content
//     attribute (which supplies only the default) and from `.value` (which on a checkbox is the
//     id it carries). Assigning it raises the dirty checkedness flag. The bitmask control reads
//     all three and a fake that conflated them would pass on a mask it got wrong.
//   * PART of the VALUE SANITIZATION ALGORITHM, for `color` and `range` only. A colour input
//     reads back nothing but lowercase #rrggbb, and a range input clamps to min/max — the rgba
//     control reads both back, so without this the tests would exercise inputs a browser cannot
//     make. Colour is complete; RANGE IS NOT — see below.
//
// STILL MISSING. Structural: appendChild does not detach from a previous parent, value
// sanitization for every OTHER type (a text input strips CR/LF in a browser and does not here),
// ask-for-reset does not skip disabled options, no removeChild, no classList, and a checkbox has
// no radio-group or form-reset behaviour.
//
// And THREE DIVERGENCES INSIDE the range sanitization that is here, each of which a browser gets
// right and this does not:
//
//   * No STEP SNAPPING. On range(min=0, max=255), `.value = '3.7'` reads back '3.7' here and '4'
//     in a browser, because the default step is 1 and the value is snapped to it.
//   * EXPONENT NOTATION is rejected. '1e2' is a valid HTML floating-point number and a browser
//     reads it back as 100; this returns the midpoint, 127.5, as if it were garbage.
//   * NO RE-SANITIZATION when `min`, `max` or `type` is changed AFTER `.value` was assigned. A
//     browser re-runs the algorithm on an attribute change; this keeps the old value.
//
// None is reachable from composites.js, which only ever assigns whole numbers already inside
// the range. Task 11 could reach all three.
//
// In the event model specifically — each of these THROWS rather than diverging quietly, so a
// test that needs one fails loudly instead of passing for the wrong reason:
//
//   * addEventListener/removeEventListener options (capture, once, passive) are refused.
//   * fire() refuses an event type whose bubbles/cancelable flags are not in EVENT_FLAGS.
//   * stopImmediatePropagation does not exist.
//
// And one that is simply absent: there are NO DEFAULT ACTIONS — a click does not focus, submit,
// or toggle anything, so anything relying on one has to be driven explicitly.
//
// ADDED FOR TASK 11 (app shell), because app.js drives the page rather than one control:
//
//   * A DOCUMENT TREE. `document` is now a node in its own right — an element of tag #document
//     that carries createElement, appendChild, the event model and getElementById, plus a `body`
//     child, so App.init's five getElementById lookups and document-level listeners work. It is
//     a real node rather than an object with a lookup table so that a detached element genuinely
//     is NOT findable: getElementById walks the tree, exactly as a browser does, which is what
//     makes "the previous form's canvas is gone" observable.
//   * `id` REFLECTED onto the content attribute, both ways, like className and hidden.
//   * installFakeImage(): the async decode of a sprite bundle. Assigning `.src` queues the load
//     rather than firing it; the controller's load()/fail() drain the queue, so a test decides
//     when parts arrives relative to icons. That ordering is the whole point — it is what made
//     the shared image-callback queue testable.
//   * installGoogleScriptRun(server): the Apps Script client stub, modelled on the real
//     contract. withSuccessHandler/withFailureHandler return A NEW RUNNER carrying the handler
//     (they do NOT mutate google.script.run, which would leak handlers between calls), calling a
//     server function returns undefined and is ASYNCHRONOUS, the success handler receives the
//     server's return value, and the failure handler receives the thrown Error. Calls queue;
//     flush() drains them, including calls made from inside a handler, so a whole cascade
//     resolves deterministically with no timers.

class FakeNode {
  constructor(tag) {
    this.tagName = String(tag).toUpperCase();
    this.attributes = new Map();
    this.children = [];
    this.parentNode = null;
    this._listeners = new Map();
    this._context = null;
    this._text = '';
    // An input's live value tracks its `value` CONTENT ATTRIBUTE until something assigns to
    // .value, which raises the dirty value flag and decouples the two for good (HTML, "value"
    // IDL attribute / dirty value flag). Modelled because Task 10's bitmask control carries
    // each class id in the value ATTRIBUTE of a checkbox — el('input', { value: id }) — and
    // reads it back through .value; a fake that ignored the attribute would report '' for
    // every id. (Whether such a box is TICKED is .checked, a separate thing — see below.)
    // Selects derive their value from the selected option, so _value is unused for them.
    this._value = '';
    this._dirty = false;
    // Checkedness, the IDL attribute — a DIFFERENT thing from the `checked` content attribute
    // (which only supplies the default) and from `.value` (which on a checkbox is the id it
    // carries, defaulting to 'on'). Assigning .checked raises the dirty checkedness flag, after
    // which the content attribute no longer has any say. Task 10's bitmask control depends on
    // all three being distinct.
    this._checked = false;
    this._dirtyChecked = false;
    this.selectedIndex = -1;
  }

  get checked() {
    if (this._dirtyChecked) return this._checked;
    return this.getAttribute('checked') !== null;
  }

  set checked(value) {
    this._checked = !!value;
    this._dirtyChecked = true;
  }

  setAttribute(name, value) {
    this.attributes.set(String(name), String(value));
  }

  getAttribute(name) {
    return this.attributes.has(String(name)) ? this.attributes.get(String(name)) : null;
  }

  removeAttribute(name) {
    this.attributes.delete(String(name));
  }

  // A boolean attribute: PRESENT means hidden, whatever its value — hidden="" and
  // hidden="false" both hide. Assigning false must therefore remove it, not blank it.
  get hidden() {
    return this.getAttribute('hidden') !== null;
  }

  set hidden(value) {
    if (value) this.setAttribute('hidden', '');
    else this.removeAttribute('hidden');
  }

  get id() {
    return this.getAttribute('id') || '';
  }

  set id(value) {
    this.setAttribute('id', String(value));
  }

  // Walks the tree, as a browser does — an element removed from the document is not findable,
  // and the FIRST match in tree order wins when a page has a duplicate id.
  getElementById(id) {
    const want = String(id);
    return this._descendants().find((n) => n.getAttribute('id') === want) || null;
  }

  get className() {
    return this.getAttribute('class') || '';
  }

  set className(value) {
    this.setAttribute('class', String(value));
  }

  // Only a <canvas> reflects width/height as numbers; on anything else the property does not
  // exist, exactly as in a browser.
  get width() {
    return this.tagName === 'CANVAS' ? Number(this.getAttribute('width') || 0) : undefined;
  }

  set width(value) {
    this.setAttribute('width', value);
  }

  get height() {
    return this.tagName === 'CANVAS' ? Number(this.getAttribute('height') || 0) : undefined;
  }

  set height(value) {
    this.setAttribute('height', value);
  }

  // Memoised, like the real thing: two getContext('2d') calls on one canvas return the SAME
  // context, so a module that fetches it twice still draws onto one surface.
  getContext(type) {
    if (this.tagName !== 'CANVAS' || type !== '2d') return null;
    if (!this._context) this._context = recordingContext();
    return this._context;
  }

  // THROWS on a third argument rather than dropping it. `true`/`{ capture: true }` would make
  // the listener run BEFORE the target's, and `{ once: true }` would make it run exactly once
  // — a fake that silently ignored either would pass a test the browser fails. Refusing is the
  // loud direction; implement them here the day a module needs one.
  addEventListener(type, handler) {
    if (arguments.length > 2) {
      throw new Error('fake DOM does not model addEventListener options (capture/once/passive)');
    }
    const key = String(type);
    if (!this._listeners.has(key)) this._listeners.set(key, []);
    const list = this._listeners.get(key);
    // The real DOM discards a duplicate (type, handler, capture) registration rather than
    // calling the handler twice.
    if (!list.includes(handler)) list.push(handler);
  }

  removeEventListener(type, handler) {
    if (arguments.length > 2) {
      throw new Error('fake DOM does not model removeEventListener options (capture)');
    }
    const list = this._listeners.get(String(type));
    if (!list) return;
    const at = list.indexOf(handler);
    if (at !== -1) list.splice(at, 1);
  }

  // Returns false when the default was prevented, as dispatchEvent does.
  //
  // Whether the event travels up the tree is the EVENT's business, not a table kept in here:
  // `new Event(type)` does not bubble unless you ask it to, and the per-type defaults live in
  // fire() where the type is known. A hardcoded "everything except focus and blur bubbles"
  // would deliver a mouseenter dispatched on a child to an ancestor's listener.
  dispatchEvent(event) {
    if (!event.target) event.target = this;
    let node = this;
    while (node) {
      const list = node._listeners.get(event.type);
      // A copy, so a handler that rebuilds the list does not change who else is called for
      // THIS event — but a listener REMOVED during the dispatch must not fire, so the live
      // list is re-checked before each call. Both halves are real DOM behaviour.
      if (list) for (const handler of list.slice()) {
        const live = node._listeners.get(event.type);
        if (!live || !live.includes(handler)) continue;
        event.currentTarget = node;
        handler.call(node, event);
      }
      if (!event.bubbles || event.propagationStopped) break;
      node = node.parentNode;
    }
    return !event.defaultPrevented;
  }

  appendChild(child) {
    child.parentNode = this;
    this.children.push(child);
    // A single-line <select> always shows something once it has options: adding one to a
    // select with nothing selected makes the FIRST option selected, not the new one. That
    // matters — it is why appending an option AFTER assigning an unmatched value does not
    // rescue the value.
    if (this.tagName === 'SELECT' && child.tagName === 'OPTION' && this.selectedIndex === -1) {
      this.selectedIndex = 0;
    }
    return child;
  }

  get textContent() {
    if (this.children.length === 0) return this._text;
    return this._text + this.children.map((c) => c.textContent).join('');
  }

  set textContent(text) {
    this._text = String(text);
    this.children.forEach((c) => { c.parentNode = null; });
    this.children = [];
  }

  set innerHTML(html) {
    if (html !== '') throw new Error('fake DOM only supports innerHTML = ""');
    this.children.forEach((c) => { c.parentNode = null; });
    this.children = [];
    this._text = '';
  }

  get innerHTML() {
    return this.children.length || this._text ? '<...>' : '';
  }

  _options() {
    return this.children.filter((c) => c.tagName === 'OPTION');
  }

  get value() {
    if (this.tagName === 'SELECT') {
      const options = this._options();
      const chosen = options[this.selectedIndex];
      return chosen ? chosen.value : '';
    }
    if (this.tagName === 'OPTION') {
      // An <option> with no value attribute falls back to its text, as HTML specifies.
      const attr = this.getAttribute('value');
      return attr === null ? this.textContent : attr;
    }
    // Clean input: the value IS the content attribute. Dirty: the attribute is ignored.
    if (this._dirty) return this._value;
    const attr = this.getAttribute('value');
    if (attr !== null) return this._sanitize(attr);
    // A checkbox or radio with no value attribute reads back as 'on', not '' — its value mode
    // is "default/on". Everything else defaults to the empty string.
    const type = String(this.getAttribute('type') || '').toLowerCase();
    return (type === 'checkbox' || type === 'radio') ? 'on' : '';
  }

  set value(next) {
    const text = String(next);
    if (this.tagName === 'SELECT') {
      // The whole point of this fake: no matching option means nothing is selected, and the
      // select then reads back as ''. Browsers do not invent an option for you.
      this.selectedIndex = this._options().findIndex((o) => o.value === text);
      return;
    }
    this._value = this._sanitize(text);
    this._dirty = true;
  }

  // The VALUE SANITIZATION ALGORITHM, for the two types Task 10's rgba control reads back.
  // Modelling it is load-bearing in both directions: a colour input NEVER reads back anything
  // but #rrggbb lowercase, so a control that tried to parse arbitrary text there would be
  // testing a case the browser cannot produce; and a range input CLAMPS, so a stored channel of
  // 900 comes back as 255 rather than as 900. Every other type is left alone — in particular
  // text inputs do NOT strip CR/LF here, which a browser does.
  _sanitize(text) {
    const type = String(this.getAttribute('type') || '').toLowerCase();
    if (type === 'color') {
      return /^#[0-9a-fA-F]{6}$/.test(text) ? text.toLowerCase() : '#000000';
    }
    if (type === 'range') {
      const min = Number(this.getAttribute('min') || 0);
      const max = Number(this.getAttribute('max') === null ? 100 : this.getAttribute('max'));
      // "If the value is not a valid floating-point number, set it to the default value" —
      // which for range is the midpoint, or min when the midpoint is above max.
      const n = /^-?\d+(\.\d+)?$/.test(String(text).trim()) ? Number(text)
        : Math.max(min, Math.min(max, min + (max - min) / 2));
      return String(Math.max(min, Math.min(max, n)));
    }
    return text;
  }

  getElementsByTagName(tag) {
    const want = String(tag).toUpperCase();
    return this._descendants().filter((n) => n.tagName === want);
  }

  _descendants() {
    const out = [];
    for (const child of this.children) {
      out.push(child);
      out.push(...child._descendants());
    }
    return out;
  }

  // Supports exactly what forms.js uses: [attr] and [attr="value"].
  _matches(selector) {
    const m = /^\[([a-zA-Z-]+)(?:="([^"]*)")?\]$/.exec(selector);
    if (!m) throw new Error('fake DOM cannot parse selector: ' + selector);
    const actual = this.getAttribute(m[1]);
    if (actual === null) return false;
    return m[2] === undefined || actual === m[2];
  }

  querySelectorAll(selector) {
    return this._descendants().filter((n) => n._matches(selector));
  }

  querySelector(selector) {
    return this.querySelectorAll(selector)[0] || null;
  }
}

// Records every call so a test can assert what was drawn, and returns pixel data of the size
// asked for. Deliberately NOT a canvas: it knows nothing about compositing.
function recordingContext() {
  const calls = [];
  return {
    calls,
    clearRect(...args) { calls.push(['clearRect', ...args]); },
    drawImage(...args) { calls.push(['drawImage', ...args]); },
    getImageData(x, y, w, h) {
      calls.push(['getImageData', x, y, w, h]);
      return { data: new Array(w * h * 4).fill(0) };
    },
    putImageData(image, x, y) { calls.push(['putImageData', x, y, [...image.data]]); },
  };
}

// Per-type event flags, from the HTML and UI Events specs. A type that is not here THROWS
// rather than being guessed at: guessing wrong about bubbling or cancelability is exactly the
// kind of divergence that makes a fake pass what a browser fails.
const EVENT_FLAGS = {
  click: { bubbles: true, cancelable: true },
  mousedown: { bubbles: true, cancelable: true },
  mouseup: { bubbles: true, cancelable: true },
  keydown: { bubbles: true, cancelable: true },
  keyup: { bubbles: true, cancelable: true },
  submit: { bubbles: true, cancelable: true },
  // input and change bubble but are NOT cancelable — preventDefault on them does nothing at
  // all, so dispatchEvent still returns true.
  input: { bubbles: true, cancelable: false },
  change: { bubbles: true, cancelable: false },
  focus: { bubbles: false, cancelable: false },
  blur: { bubbles: false, cancelable: false },
  mouseenter: { bubbles: false, cancelable: false },
  mouseleave: { bubbles: false, cancelable: false },
};

// Builds an event and dispatches it from `node`. Returns false only if a handler cancelled a
// CANCELABLE event, exactly as dispatchEvent does.
export function fire(node, type) {
  const flags = EVENT_FLAGS[String(type)];
  if (!flags) throw new Error('fake DOM does not know the flags for event type: ' + type);

  const event = {
    type: String(type),
    target: null,
    currentTarget: null,
    bubbles: flags.bubbles,
    cancelable: flags.cancelable,
    defaultPrevented: false,
    propagationStopped: false,
    preventDefault() {
      // A non-cancelable event ignores this outright — it does not even set the flag.
      if (this.cancelable) this.defaultPrevented = true;
    },
    // Stops the walk to the ancestors, but lets the remaining listeners on THIS node run.
    // stopImmediatePropagation is deliberately absent: calling it throws rather than quietly
    // doing the weaker thing.
    stopPropagation() { this.propagationStopped = true; },
  };
  return node.dispatchEvent(event);
}

export function installFakeDom() {
  const doc = new FakeNode('#document');
  doc.createElement = (tag) => new FakeNode(tag);
  doc.body = doc.appendChild(new FakeNode('body'));
  globalThis.document = doc;
  return doc;
}

// An <img> whose decode is under the test's control. `new Image()` gives a node with the usual
// onload/onerror properties; assigning .src ENQUEUES the load instead of performing it, so a
// test can land `icons` before `parts` (or never land one at all) and see what the module does.
export function installFakeImage() {
  const pending = [];

  class FakeImage {
    constructor() {
      this.onload = null;
      this.onerror = null;
      this._src = '';
    }

    get src() { return this._src; }

    set src(value) {
      this._src = String(value);
      pending.push(this);
    }
  }

  globalThis.Image = FakeImage;

  return {
    pending,
    // Decodes everything queued so far, oldest first. Handlers that queue more images do NOT
    // run in this pass — call again — which keeps each step of a cascade separately observable.
    load() {
      const batch = pending.splice(0, pending.length);
      batch.forEach((img) => { if (img.onload) img.onload(); });
      return batch.length;
    },
    fail() {
      const batch = pending.splice(0, pending.length);
      batch.forEach((img) => { if (img.onerror) img.onerror(); });
      return batch.length;
    },
  };
}

// google.script.run, modelled on its real contract rather than on what is convenient:
//
//   * withSuccessHandler / withFailureHandler return a runner FOR CHAINING and do not mutate the
//     one they were called on. (In Apps Script the returned object is a new runner; a stub that
//     mutated a single shared object would let one call's handler fire for the next call.)
//   * a server call returns undefined and completes ASYNCHRONOUSLY.
//   * the success handler receives the server function's return value; the failure handler
//     receives the Error it threw.
//   * a call with no handler is legal and its result is dropped.
//
// `server` maps function name -> implementation. Calls are recorded in `calls` before they run,
// so a test can assert on the ARGUMENTS even for a function that throws.
export function installGoogleScriptRun(server) {
  const queue = [];
  const calls = [];

  function makeRunner(onSuccess, onFailure) {
    const runner = {
      withSuccessHandler: (fn) => makeRunner(fn, onFailure),
      withFailureHandler: (fn) => makeRunner(onSuccess, fn),
    };

    Object.keys(server).forEach((name) => {
      runner[name] = (...args) => {
        calls.push({ name, args });
        queue.push(() => {
          let value;
          try {
            value = server[name](...args);
          } catch (error) {
            if (onFailure) onFailure(error);
            return;
          }
          if (onSuccess) onSuccess(value);
        });
        return undefined;
      };
    });

    return runner;
  }

  globalThis.google = { script: { run: makeRunner(null, null) } };

  return {
    calls,
    queue,
    // Drains until nothing is left, so a handler that issues the next call is followed through.
    // The cap turns a cycle into a failed test rather than a hung one.
    flush(limit = 500) {
      let ran = 0;
      while (queue.length) {
        if (++ran > limit) throw new Error('google.script.run stub: too many chained calls');
        queue.shift()();
      }
      return ran;
    },
    // One step, for asserting on intermediate state.
    step() {
      if (!queue.length) return false;
      queue.shift()();
      return true;
    },
  };
}

export function createElement(tag) {
  return new FakeNode(tag);
}

// Depth-first list of every element, for assertions about structure.
export function walk(node) {
  return node._descendants();
}
