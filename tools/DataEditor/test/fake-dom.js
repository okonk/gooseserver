// A DOM small enough to read in one sitting and faithful in the two places forms.js can be
// wrong: <select>.value refusing a value no <option> carries, and an <input>'s DIRTY VALUE
// FLAG. Everything else is the minimum the module touches.
//
// Install with installFakeDom(); it sets globalThis.document and returns it.

class FakeNode {
  constructor(tag) {
    this.tagName = String(tag).toUpperCase();
    this.attributes = new Map();
    this.children = [];
    this.parentNode = null;
    this._text = '';
    // An input's live value tracks its `value` CONTENT ATTRIBUTE until something assigns to
    // .value, which raises the dirty value flag and decouples the two for good (HTML,
    // "value" IDL attribute / dirty value flag). Task 10's bitmask checkboxes are built as
    // el('input', { value: id }) and then read back through .value, so a fake that ignored
    // the attribute would report '' for every one of them. Selects derive their value from
    // the selected option, so _value is unused for them.
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
    return attr === null ? '' : attr;
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
