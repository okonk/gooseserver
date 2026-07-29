// Controls spanning several columns. The flat column list is untouched — these read and write
// the same underlying cells, by name, through hidden (or plain) inputs, so Forms.collect
// gathers them with everything else and collect() below has nothing to add.
//
// ONE RULE RUNS THROUGH ALL FOUR CONTROLS: opening a record must not change it. Every control
// seeds its cell with the STORED TEXT, verbatim, and only reformats once the user has actually
// moved something. Without that, merely opening an NPC would rewrite equipped_items and
// class_restrictions on the next save — and five rows in the shipped data are malformed in ways
// Equipped.parse silently repairs. The single exception is a BLANK equipped_items, which is not
// a value the server can use at all (Goose/Packets.cs:161 splices it into the packet); repairing
// that one is always right.
//
// CONTRACT WITH TASK 11, two halves:
//
//   1. Every control calls `wrapper.__onChange()` after a change it accepted, if the property is
//      set. That is how the preview learns to redraw. It is a plain property rather than a
//      dispatched Event because nothing else in this editor listens to the DOM, and a synthetic
//      Event would need a constructor the Apps Script sandbox and the test fake both lack.
//
//   2. THE SAVE PATH MUST REFUSE WHILE AN EQUIP SLOT IS FLAGGED. equipSlotsControl freezes the
//      whole cell while any of the six graphic fields holds a typo — it has to, because
//      Equipped.format would coerce the typo to 0 and Equipped.isFaithful only ever inspects
//      STORED strings, so a save would look clean. The cost is that edits to the OTHER five
//      slots, made while the field is frozen, are held in memory and never reach the cell:
//      saving then silently discards them. `wrapper.__frozen` is true exactly while that is the
//      case; Save must block on it and say so, not just refuse quietly.
var Composites = (function () {
  // The kinds SchemaGen emits. Pinned so a new kind arriving in schema.js is caught by a test
  // rather than by a designer finding an unrenderable field.
  var KINDS = ['Graphic', 'Rgba', 'Bitmask', 'IdList', 'EquipSlots'];

  // class_restrictions is a BIGINT, but every bit above 52 is past Number's integer range and
  // could not be read back out of a spreadsheet cell faithfully anyway. Stopping here keeps the
  // arithmetic exact; the shipped masks use bits 0-5, and bit 7 only in Quests (which declares
  // the column but has no Bitmask composite).
  //
  // A bit above 52 is therefore DROPPED SILENTLY, and the cell collapses to the truncated value
  // on the first interaction. Nothing in the shipped data comes near it and the column has no
  // room for such a mask in practice, but the silence is the risk, not the truncation: if this
  // control ever meets a real 64-bit mask it will quietly shorten it. Reporting it needs a
  // status line and a decision about what the user is supposed to do, which is why it is a note
  // here rather than a guess in code.
  var MAX_BIT = 52;

  var WHOLE = /^\d+$/;

  // parseInt(v, 10), matching Equipped.num(), Appearance.num() and Sprites.num() so the modules
  // cannot disagree about what a cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  function channel(value) {
    return Math.min(255, Math.max(0, num(value)));
  }

  // BIT CONVENTION, FROM THE SERVER. Goose/Class.cs:34 is
  //   CanUse(mask) => (mask & 2^ClassID) == 0
  // so the bit INDEX is the class_id itself — bit 0 belongs to no class, the shipped classes
  // are 1-6 — and a SET bit means that class is RESTRICTED, not permitted. Both halves are easy
  // to invert and neither fails loudly: an off-by-one locks items to the neighbouring class and
  // an inversion locks them to everyone else. So these two are a plain bit-index <-> id map and
  // nothing here adds or subtracts one.
  //
  // `&` is deliberately NOT used: it coerces to int32, so bit 31 comes back negative and bit 32
  // and up come back as 0 — which would read as "no restriction at all".
  function bitsToIds(mask) {
    var m = num(mask);
    var ids = [];
    // The bound also covers 0 and every negative mask — neither is >= 1, so the loop stops
    // before its first test and no bit is reported. A negative is not a mask at all, and
    // inventing restrictions for one would be worse than reporting none.
    for (var bit = 0; bit <= MAX_BIT; bit++) {
      var place = Math.pow(2, bit);
      if (place > m) break;
      if (Math.floor(m / place) % 2 === 1) ids.push(bit);
    }
    return ids;
  }

  // Deduplicated: `mask += 2^id` over a list holding an id twice silently CARRIES into the next
  // bit, turning a Rogue restriction into a Warrior one. Anything that is not a whole number in
  // range is dropped rather than raised to a fractional or negative power.
  function idsToBits(ids) {
    var mask = 0;
    // A plain object is safe here where the rest of this codebase reaches for a null prototype:
    // every key is a bit INDEX, and Object.prototype has no numeric properties to collide with.
    var seen = {};

    (ids || []).forEach(function (id) {
      var text = str(id).trim();
      if (!WHOLE.test(text)) return;
      var bit = Number(text);
      if (bit > MAX_BIT || seen[bit]) return;
      seen[bit] = true;
      mask += Math.pow(2, bit);
    });
    return mask;
  }

  // NPCHandler.cs accepts space OR comma; the editor writes the space-separated form back.
  function parseIdList(raw) {
    return str(raw).split(/[\s,]+/).filter(function (t) { return t !== ''; });
  }

  function formatIdList(ids) {
    return (ids || []).join(' ');
  }

  // Icon.cs:9-11 — the alpha channel is a BLEND FACTOR against the sprite, not opacity. Zero is
  // NoTint, whatever r/g/b say.
  function isTinted(tint) {
    return !!(tint && num(tint.a));
  }

  // Clamped, then padded to exactly two digits. Neither is optional: 0 renders as one digit and
  // 300 as three, and taking the last two characters of either keeps the WRONG end.
  function toHex(r, g, b) {
    function pair(value) {
      var text = channel(value).toString(16);
      return text.length < 2 ? '0' + text : text;
    }
    return '#' + pair(r) + pair(g) + pair(b);
  }

  // null, not a zeroed colour, for anything that is not six hex digits: "I could not read this"
  // and "this is black" are different answers, and the caller uses the difference to leave the
  // stored cells alone.
  function fromHex(hex) {
    var m = /^#?([0-9a-fA-F]{6})$/.exec(str(hex).trim());
    if (!m) return null;
    return {
      r: parseInt(m[1].slice(0, 2), 16),
      g: parseInt(m[1].slice(2, 4), 16),
      b: parseInt(m[1].slice(4, 6), 16),
    };
  }

  function notify(wrap) {
    if (typeof wrap.__onChange === 'function') wrap.__onChange();
  }

  // A hidden input seeded with the stored text. Every control writes its column through one of
  // these, so Forms.collect's [name] sweep picks it up unchanged.
  function cell(column, values) {
    var node = Forms.el('input', { type: 'hidden', name: column });
    node.value = str(values[column]);
    return node;
  }

  // RGBA: one swatch plus a blend slider over four cells.
  function rgbaControl(comp, values) {
    var cols = comp.columns;
    var wrap = Forms.el('div', { class: 'rgba' });

    var hidden = cols.map(function (name) { return cell(name, values); });

    var swatch = Forms.el('input', { type: 'color' });
    swatch.value = toHex(values[cols[0]], values[cols[1]], values[cols[2]]);

    var blend = Forms.el('input', { type: 'range', min: '0', max: '255' });
    blend.value = String(channel(values[cols[3]]));

    var readout = Forms.el('span', { class: 'readout' });

    function describe() {
      readout.textContent = blend.value + ' / 255 blend';
    }

    // Writes ALL FOUR cells, including at a blend of zero. Blanking r/g/b when the blend is 0
    // would destroy a parked colour — the same loss Equipped.isFaithful reports for a
    // zero-alpha tint in equipped_items — and blanking the alpha would hand the cell back to
    // the SQL default rather than storing the decision the user just made.
    //
    // Only ever reached from a listener, which is what keeps an untouched control from
    // rewriting its row: the swatch shows a CLAMPED reading of the stored channels, and the
    // cells keep the unclamped originals until something actually changes.
    function sync() {
      var rgb = fromHex(swatch.value);
      // A colour input sanitises its own value, so this is unreachable in a browser. Bailing
      // beats writing three zeroes over a colour on the strength of a value we cannot read.
      if (rgb) {
        hidden[0].value = String(rgb.r);
        hidden[1].value = String(rgb.g);
        hidden[2].value = String(rgb.b);
      }
      hidden[3].value = String(channel(blend.value));
      describe();
      notify(wrap);
    }

    swatch.addEventListener('input', sync);
    blend.addEventListener('input', sync);

    describe();
    wrap.appendChild(swatch);
    wrap.appendChild(blend);
    wrap.appendChild(readout);
    hidden.forEach(function (node) { wrap.appendChild(node); });
    return wrap;
  }

  // Bitmask: a checkbox per row of the referenced sheet. Checked means RESTRICTED.
  function bitmaskControl(comp, values, ctx) {
    var column = comp.columns[0];
    var wrap = Forms.el('div', { class: 'bitmask' });
    var entries = (ctx && ctx.pickerData && ctx.pickerData[comp.source]) || [];

    // An empty list is a sheet that has not arrived, and checkboxes built from it would be an
    // empty control forever — the field would be silently uneditable. Fall back to the raw
    // number so the row is never held hostage by a load order.
    if (entries.length === 0) {
      // No id: Forms.render gives a composite a plain <label> with no `for`, because a
      // composite has no single control to point one at. An id here would resolve to nothing.
      var raw = Forms.el('input', { type: 'text', name: column, autocomplete: 'off' });
      raw.value = str(values[column]);
      wrap.appendChild(raw);
      wrap.appendChild(Forms.el('span', { class: 'status' },
        comp.source + ' has not loaded — editing the raw bitmask'));
      return wrap;
    }

    var hidden = cell(column, values);

    // Bits belonging to no listed row are kept exactly as they are, SET OR CLEAR. This is not
    // hypothetical and it is not a constant: bit 0 belongs to no class, 9 of the 13 shipped
    // item/spell masks set it (~230 rows) and the other four — 22, 34, 38, 50 — leave it clear.
    // Rebuilding the mask from the checkboxes alone would rewrite every row in the first group;
    // OR-ing in a fixed bit 0 would rewrite every row in the second.
    var stored = bitsToIds(values[column]);
    var known = Object.create(null);
    entries.forEach(function (e) { known[num(e.id)] = true; });
    var foreign = stored.filter(function (bit) { return !known[bit]; });

    wrap.appendChild(Forms.el('div', { class: 'hint' }, 'checked = cannot use'));

    var boxes = entries.map(function (e) {
      var id = num(e.id);
      var label = Forms.el('label', { class: 'check' });
      var box = Forms.el('input', { type: 'checkbox', value: String(id) });
      box.checked = stored.indexOf(id) !== -1;
      box.addEventListener('change', sync);
      label.appendChild(box);
      label.appendChild(Forms.el('span', null, id + ' ' + str(e.name)));
      wrap.appendChild(label);
      return box;
    });

    // Declared after its listeners are registered; hoisting is what makes that legal, and the
    // order is deliberate — the boxes are what the function reads.
    function sync() {
      var ticked = boxes.filter(function (b) { return b.checked; })
        .map(function (b) { return b.value; });
      hidden.value = String(idsToBits(ticked.concat(foreign)));
      notify(wrap);
    }

    wrap.appendChild(hidden);
    return wrap;
  }

  // IdList: chips over a space-separated list of ids, plus a field to add one.
  function idListControl(comp, values, ctx) {
    var column = comp.columns[0];
    var wrap = Forms.el('div', { class: 'idlist' });
    var hidden = cell(column, values);
    var chips = Forms.el('div', { class: 'chips' });

    var ids = parseIdList(values[column]);

    // Read through to ctx on every use rather than captured once, for the reason
    // Pickers.fkControl gives: App.loadReferencedSheets fills pickerData asynchronously.
    function entries() {
      return (ctx && ctx.pickerData && ctx.pickerData[comp.source]) || [];
    }

    function nameOf(id) {
      var all = entries();
      for (var i = 0; i < all.length; i++) {
        if (num(all[i].id) === num(id)) return str(all[i].name) || '(unnamed)';
      }
      return entries().length ? '(not found in ' + comp.source + ')' : '…';
    }

    function renderChips() {
      chips.innerHTML = '';
      ids.forEach(function (id, index) {
        var chip = Forms.el('span', { class: 'chip' }, id + ' ' + nameOf(id));
        var remove = Forms.el('button', { type: 'button', class: 'remove' }, '×');
        // Safe despite closing over `index`: every mutation re-runs renderChips, which throws
        // these nodes away and rebuilds them against the new array. A stale closure is never
        // reachable because the node holding it is no longer in the tree.
        remove.addEventListener('click', function () {
          ids.splice(index, 1);
          sync();
        });
        chip.appendChild(remove);
        chips.appendChild(chip);
      });
    }

    function sync() {
      hidden.value = formatIdList(ids);
      renderChips();
      notify(wrap);
    }

    var add = Forms.el('input', {
      type: 'text', class: 'add', placeholder: 'add id', autocomplete: 'off',
    });

    // Deduplicated NUMERICALLY: '1' and '01' are one quest, and two chips for it would write
    // the id into the list twice. Anything that is not a whole number is not an id and is
    // dropped — quest_ids is a Text column, so nothing downstream would report it.
    function addId() {
      var text = str(add.value).trim();
      if (!WHOLE.test(text)) return;
      var wanted = num(text);
      for (var i = 0; i < ids.length; i++) {
        if (num(ids[i]) === wanted) { add.value = ''; return; }
      }
      // Cleared only once the id is IN the list. A rejected entry stays on screen so the user
      // can see and fix what they typed, rather than watching it vanish without explanation.
      add.value = '';
      ids.push(String(wanted));
      sync();
    }

    // Both paths, because `change` on a text input fires on blur or Enter — a user who types an
    // id and then clicks Save would otherwise lose it to a race between blur and the click.
    var button = Forms.el('button', { type: 'button', class: 'add-button' }, 'add');
    button.addEventListener('click', addId);
    add.addEventListener('change', addId);

    renderChips();
    wrap.appendChild(chips);
    wrap.appendChild(add);
    wrap.appendChild(button);
    wrap.appendChild(hidden);
    return wrap;
  }

  // EquipSlots: six labelled graphic fields with previews, over the equipped_items token stream.
  function equipSlotsControl(comp, values, ctx) {
    var column = comp.columns[0];
    var wrap = Forms.el('div', { class: 'equip' });
    var raw = str(values[column]);
    var slots = Equipped.parse(raw);

    var hidden = Forms.el('input', { type: 'hidden', name: column });
    // Blank is the one value that cannot stand: it emits ",," into the MakeCharacter packet and
    // desynchronises it. Anything else keeps its stored text until an edit lands.
    hidden.value = raw.trim() === '' ? Equipped.format(slots) : raw;

    // bodyState only distinguishes armed from unarmed in a resting pose (Sprites.part's note).
    // 3 is unarmed; Items defaults to 3 and NPCs to 1. Read once, at build time: the control
    // has no handle on the body_state field, and Task 11's preview is what re-renders the form.
    //
    // A BLANK cell means "use the SQL default", which forms.js is scrupulous about and this is
    // not: num('') is 0, not the descriptor's 1. The answer comes out right for the only sheet
    // that has this composite — 0 !== 3, same as 1 !== 3 — but by luck, not by rule. Fixing it
    // properly means reading the column descriptor, which this function is not given; it takes
    // the values map alone. Left as-is deliberately, and flagged so a second sheet with an
    // unarmed default does not inherit the luck.
    var equipped = num(values.body_state) !== 3;

    var bad = Object.create(null);
    wrap.__frozen = false;

    function anyBad() {
      return Object.keys(bad).length > 0;
    }

    // Refuses to write while any field holds a typo. Equipped.format would coerce 'abc' to 0
    // and isFaithful only ever inspects STORED strings, so nothing else in the stack catches
    // one — the row would save clean with the slot silently emptied.
    //
    // While frozen, edits to the OTHER five slots go into `slots` and stop there, so a save
    // would drop them. __frozen is what the save path blocks on; see the contract at the top.
    function sync() {
      wrap.__frozen = anyBad();
      if (anyBad()) return;
      hidden.value = Equipped.format(slots);
      notify(wrap);
    }

    Equipped.SLOTS.forEach(function (slotName, index) {
      var row = Forms.el('div', { class: 'equip-slot' });
      row.appendChild(Forms.el('label', null, slotName));

      var input = Forms.el('input', {
        type: 'text', class: 'slot-graphic', placeholder: 'graphic id', autocomplete: 'off',
      });
      input.value = String(slots[index].graphic);

      var canvas = Forms.el('canvas', { width: 40, height: 56, class: 'preview' });
      var status = Forms.el('span', { class: 'status' });

      function redraw() {
        var target = canvas.getContext('2d');
        target.clearRect(0, 0, canvas.width, canvas.height);

        // Appearance.CATEGORY is the one mapping from slot to sprite folder — Shield and Weapon
        // both land on 'Hands'. Repeating it here would be a second copy to keep in step.
        var category = Appearance.CATEGORY[slotName];
        var rect = Sprites.part((ctx && ctx.bundles) || {}, category, slots[index].graphic,
                                equipped);
        // Sprites.draw ignores a null rect, but the centring below reads rect[2] first.
        if (!rect) return;

        Sprites.draw(target, (ctx && ctx.images) ? ctx.images.parts : null, rect,
                     Math.floor((canvas.width - rect[2]) / 2),
                     Math.floor((canvas.height - rect[3]) / 2),
                     slots[index]);
      }

      input.addEventListener('input', function () {
        var text = str(input.value).trim();
        if (text !== '' && !WHOLE.test(text)) {
          bad[index] = true;
          status.textContent = 'graphic must be a whole number';
          status.className = 'status bad';
          // Through sync() rather than returning outright, so __frozen is raised the moment the
          // typo appears — not on the next good edit, by which time a save has already happened.
          sync();
          return;
        }
        delete bad[index];
        status.textContent = '';
        status.className = 'status';
        slots[index].graphic = num(text);
        sync();
        redraw();
      });

      redraw();
      if (ctx && typeof ctx.onImagesReady === 'function') ctx.onImagesReady(redraw);

      row.appendChild(input);
      row.appendChild(canvas);
      row.appendChild(status);
      wrap.appendChild(row);
    });

    wrap.appendChild(hidden);
    return wrap;
  }

  // Last resort. A control with no [name] would drop every one of the composite's columns from
  // Forms.collect and blank them on the next save, so an unknown kind degrades to plain fields
  // rather than to nothing.
  function unsupportedControl(comp, byName, values) {
    var wrap = Forms.el('div', { class: 'unsupported' });
    wrap.appendChild(Forms.el('div', { class: 'status bad' },
      'unsupported composite: ' + comp.kind));
    comp.columns.forEach(function (name) {
      if (!byName[name]) return;
      wrap.appendChild(Forms.el('label', { for: 'f-' + name }, name));
      wrap.appendChild(Forms.scalarControl(byName[name], values[name]));
    });
    return wrap;
  }

  // Forms.render appends exactly ONE error slot per field, keyed on the composite's leader, so
  // a validation error against any other column it owns has nowhere to go. These slots come
  // first in document order and Forms.showErrors keeps the first slot it finds per column.
  function addErrorSlots(node, comp, byName) {
    comp.columns.slice(1).forEach(function (name) {
      if (byName[name]) node.appendChild(Forms.el('div', { class: 'error',
                                                          'data-error-for': name }));
    });
    return node;
  }

  function control(comp, byName, values, ctx) {
    var node;
    switch (comp.kind) {
      case 'Graphic':
        node = Pickers.graphicControl(byName[comp.columns[0]], byName[comp.columns[1]],
                                      values, ctx);
        break;
      case 'Rgba': node = rgbaControl(comp, values); break;
      case 'Bitmask': node = bitmaskControl(comp, values, ctx); break;
      case 'IdList': node = idListControl(comp, values, ctx); break;
      case 'EquipSlots': node = equipSlotsControl(comp, values, ctx); break;
      default: node = unsupportedControl(comp, byName, values); break;
    }
    return addErrorSlots(node, comp, byName);
  }

  // Composite controls keep their state in inputs named after their columns, so Forms.collect
  // already picks them up. Nothing extra to gather.
  function collect() { return {}; }

  return {
    control: control, collect: collect, KINDS: KINDS,
    bitsToIds: bitsToIds, idsToBits: idsToBits,
    parseIdList: parseIdList, formatIdList: formatIdList,
    isTinted: isTinted, toHex: toHex, fromHex: fromHex,
  };
})();

if (typeof module !== 'undefined') module.exports = { Composites: Composites };
