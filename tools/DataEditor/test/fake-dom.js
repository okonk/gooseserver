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
// STILL MISSING (Task 10's prerequisite): checkedness, appendChild does not detach from a
// previous parent, no input value sanitization, ask-for-reset does not skip disabled options,
// no removeChild, no classList.

// Real focus/blur do not bubble; every other event this fake is asked to carry does.
const NON_BUBBLING = new Set(['focus', 'blur']);

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
    // every id. (Whether such a box is TICKED is .checked, a separate thing this fake does
    // not model at all — Task 10 will have to add it.) Selects derive their value from the
    // selected option, so _value is unused for them.
    this._value = '';
    this._dirty = false;
    this.selectedIndex = -1;
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

  addEventListener(type, handler) {
    const key = String(type);
    if (!this._listeners.has(key)) this._listeners.set(key, []);
    const list = this._listeners.get(key);
    // The real DOM discards a duplicate (type, handler, capture) registration rather than
    // calling the handler twice.
    if (!list.includes(handler)) list.push(handler);
  }

  removeEventListener(type, handler) {
    const list = this._listeners.get(String(type));
    if (!list) return;
    const at = list.indexOf(handler);
    if (at !== -1) list.splice(at, 1);
  }

  // Returns false when the default was prevented, as dispatchEvent does.
  dispatchEvent(event) {
    if (!event.target) event.target = this;
    let node = this;
    while (node) {
      const list = node._listeners.get(event.type);
      // A copy: a handler that rebuilds the list must not change who else is called for THIS
      // event.
      if (list) for (const handler of list.slice()) {
        event.currentTarget = node;
        handler.call(node, event);
      }
      if (NON_BUBBLING.has(event.type)) break;
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
    if (attr !== null) return attr;
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
    this._value = text;
    this._dirty = true;
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

// Builds an event and dispatches it from `node`. Returns false if a handler cancelled it.
export function fire(node, type) {
  const event = {
    type: String(type),
    target: null,
    currentTarget: null,
    defaultPrevented: false,
    preventDefault() { this.defaultPrevented = true; },
  };
  return node.dispatchEvent(event);
}

export function installFakeDom() {
  globalThis.document = { createElement: (tag) => new FakeNode(tag) };
  return globalThis.document;
}

export function createElement(tag) {
  return new FakeNode(tag);
}

// Depth-first list of every element, for assertions about structure.
export function walk(node) {
  return node._descendants();
}
