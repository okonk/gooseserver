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
// CONTRACT WITH THE PREVIEW PANEL (app.js's renderPreviews and its delegated listener), two
// halves:
//
//   1. A CHANGE THIS FILE ACCEPTS MUST REACH THE FORM CONTAINER AS A BUBBLING input/change EVENT.
//      That one delegated listener is the whole redraw mechanism — there is no subscription hook
//      (there was a `wrapper.__onChange` here for a while; nothing ever set it, so it read like a
//      working signal twice over and is gone). Most paths get this for free from the browser: a
//      checkbox fires `change`, a text field fires `input`. The paths that DO NOT are the ones to
//      watch, and each has to dispatch for itself — ColorPicker.control does it for every colour
//      movement (see fire() there), and notify() below does it for the chip buttons.
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

  // The equip-slot preview's logical box, and the factor its canvas backing store is scaled by
  // so the sprite is legible on screen. The maths is unchanged; only the context is scaled.
  //
  // READ THROUGH TO Pickers rather than restated. An .equip-slot row and Pickers.partControl show
  // the SAME thing — one equipped sprite in the form column — and two different boxes for it would
  // make the form look uneven for no reason, so the numbers have to agree. They now cannot fail to:
  // there is one copy.
  //
  // A FUNCTION, not three `var`s in this IIFE's body. Reading Pickers here at evaluation time
  // would make composites.js throw ReferenceError unless Editor.html happened to include pickers
  // first — a dead page from an include reorder, and a coupling no test could see. Every other
  // cross-module use in this file resolves as a free global at CALL time; this now does too.
  function slotBox() {
    return { w: Pickers.PART_W, h: Pickers.PART_H, scale: Pickers.PART_SCALE };
  }

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

  // BIT CONVENTION, FROM THE SERVER. Goose/Class.cs's CanUse is
  //   CanUse(mask) => mask == 0 || (mask & 2^ClassID) != 0
  // so the bit INDEX is the class_id itself — bit 0 belongs to no class, the shipped classes
  // are 1-6 — and a SET bit means that class is ALLOWED. An ALL-ZERO mask is the exception:
  // it is "no restriction", every class, not "no class". Both halves are easy to invert and
  // neither fails loudly: an off-by-one locks items to the neighbouring class and an inversion
  // locks them to everyone else. So these two are a plain bit-index <-> id map and nothing here
  // adds or subtracts one.
  //
  // `&` is deliberately NOT used: it coerces to int32, so bit 31 comes back negative and bit 32
  // and up come back as 0 — which would read as an empty allow list, and be rendered as one.
  function bitsToIds(mask) {
    var m = num(mask);
    var ids = [];
    // The bound also covers 0 and every negative mask — neither is >= 1, so the loop stops
    // before its first test and no bit is reported. A negative is not a mask at all, and
    // inventing permissions for one would be worse than reporting none.
    for (var bit = 0; bit <= MAX_BIT; bit++) {
      var place = Math.pow(2, bit);
      if (place > m) break;
      if (Math.floor(m / place) % 2 === 1) ids.push(bit);
    }
    return ids;
  }

  // Deduplicated: `mask += 2^id` over a list holding an id twice silently CARRIES into the next
  // bit, turning a Rogue's permission into a Warrior's. Anything that is not a whole number in
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

  // The redraw signal for a write the browser gives no event of its own — see half 1 of the
  // contract at the top. `change` rather than `input`: these paths commit a whole value at once
  // (a chip added or removed), which is what `change` describes. Dispatched from the control's
  // wrapper, which is inside the form container the listener sits on.
  function notify(wrap) {
    wrap.dispatchEvent(new Event('change', { bubbles: true }));
  }

  // A hidden input seeded with the stored text. Every control writes its column through one of
  // these, so Forms.collect's [name] sweep picks it up unchanged.
  function cell(column, values) {
    var node = Forms.el('input', { type: 'hidden', name: column });
    node.value = str(values[column]);
    return node;
  }

  // RGBA: one swatch over four cells. The colour AND the blend factor live in ColorPicker's
  // popover — a native <input type="color"> opens the OS picker, which has nowhere to put the
  // blend, so the two used to be a swatch and a separate range slider that named its own cell.
  // One control now owns both, and the hint below is what still names the four cells.
  function rgbaControl(comp, values) {
    var cols = comp.columns;
    var wrap = Forms.el('div', { class: 'rgba' });

    var hidden = cols.map(function (name) { return cell(name, values); });

    // Outside the popover, because the blend factor is the one thing about this field worth
    // seeing without opening anything: a tint at 0 renders as no tint at all, and the swatch
    // beside it still shows a colour.
    var readout = Forms.el('span', { class: 'readout' });

    function describe(a) {
      readout.textContent = channel(a) + ' / 255 blend';
    }

    // Writes ALL FOUR cells, including at a blend of zero. Blanking r/g/b when the blend is 0
    // would destroy a parked colour — the same loss Equipped.isFaithful reports for a
    // zero-alpha tint in equipped_items — and blanking the alpha would hand the cell back to
    // the SQL default rather than storing the decision the user just made.
    //
    // Only ever reached from an onChange, which is what keeps an untouched control from
    // rewriting its row: the picker is SEEDED with a clamped reading of the stored channels and
    // reports nothing until something moves, so the cells keep the unclamped originals.
    var picker = ColorPicker.control({
      r: channel(values[cols[0]]),
      g: channel(values[cols[1]]),
      b: channel(values[cols[2]]),
      a: channel(values[cols[3]]),
      withAlpha: true,
      onChange: function (colour) {
        hidden[0].value = String(colour.r);
        hidden[1].value = String(colour.g);
        hidden[2].value = String(colour.b);
        hidden[3].value = String(colour.a);
        describe(colour.a);
        // No dispatch here: ColorPicker.control fires a bubbling `input` from its own wrapper
        // immediately after this returns, which is how the previews hear about the four cells
        // just written. One signal per movement, from the one place that knows a movement
        // happened.
      },
    });

    describe(values[cols[3]]);
    // The field's own <label> says "graphic tint" (Layout.labelFor) — deliberately not a column
    // name, because this one control writes four cells. The hint is what lets a designer still
    // find those cells in the sheet: it lists all four, in order.
    wrap.appendChild(picker.node);
    wrap.appendChild(readout);
    wrap.appendChild(Forms.el('div', { class: 'hint' }, cols.join(' ')));
    hidden.forEach(function (node) { wrap.appendChild(node); });
    return wrap;
  }

  // Bitmask: a checkbox per row of the referenced sheet. Checked means ALLOWED, and nothing
  // checked (mask 0) means every class — see the bit convention above.
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
    // item/spell masks set it (426 rows) and the other four — 22, 34, 38, 50 — leave it clear.
    // Rebuilding the mask from the checkboxes alone would rewrite every row in the first group;
    // OR-ing in a fixed bit 0 would rewrite every row in the second.
    var stored = bitsToIds(values[column]);
    var known = Object.create(null);
    entries.forEach(function (e) { known[num(e.id)] = true; });
    var foreign = stored.filter(function (bit) { return !known[bit]; });

    wrap.appendChild(Forms.el('div', { class: 'hint' }, 'checked = can use'));

    // Nothing checked is NOT "no class can use this" — the server reads a zero mask as
    // unrestricted. Saying so where the boxes are is the only place a designer would look, and
    // the alternative (silently ticking every box on open) would rewrite rows just for being
    // opened, which is the one thing this file refuses to do.
    var allHint = Forms.el('div', { class: 'hint' }, '');

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
      // No dispatch: sync() only ever runs from a checkbox's own `change`, which bubbles to the
      // delegated listener by itself once this returns.
      hidden.value = String(idsToBits(ticked.concat(foreign)));
      showAllHint();
    }

    function showAllHint() {
      allHint.textContent = num(hidden.value) === 0 ? 'no restriction — every class can use it' : '';
    }

    // Seeded from the STORED value, not from the boxes: a mask holding only foreign bits is
    // non-zero with nothing ticked, and calling it unrestricted would be a lie.
    showAllHint();

    wrap.appendChild(allHint);
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
    // The add field's OWN native `change` is stopped here, exactly as the colour picker stops its
    // hex field's `input`: sync() already dispatches a `change` from the wrapper, so letting the
    // native one through as well would deliver two to the form container and run Forms.collect
    // and every formCallback twice per add. The field carries no `name`, so nothing downstream
    // wants the native event.
    add.addEventListener('change', function (e) {
      e.stopPropagation();
      addId();
    });

    renderChips();
    wrap.appendChild(chips);
    wrap.appendChild(add);
    wrap.appendChild(button);
    wrap.appendChild(hidden);
    return wrap;
  }

  // The graphic browser, or null when it is not on the page. Resolved at CALL time and overridable
  // through the control's options object, exactly as Pickers.galleryOf does it — pickers.js:123
  // states the reason a two-line helper is duplicated across modules rather than shared, and this
  // is the copy it names.
  function galleryOf(opts) {
    if (opts && opts.gallery) return opts.gallery;
    return (typeof Gallery !== 'undefined' && Gallery) ? Gallery : null;
  }

  // EquipSlots: six labelled rows over the equipped_items token stream — a graphic field, a
  // colour picker for the slot's tint, and a preview of the two together.
  //
  // `opts` is the same options bag Pickers' controls take — { gallery, effective } today. Threaded
  // whole rather than as a bare `gallery` positional so this seam has the shape every other gallery
  // seam in the round has, and so a second option costs no signature.
  //
  // `values` is the RAW record, because the hidden cell below has to keep the stored string
  // verbatim; `opts.effective` is the same record with blanks resolved, and only the pose reads it.
  function equipSlotsControl(comp, values, ctx, opts) {
    var column = comp.columns[0];
    var effective = (opts && opts.effective) || values;
    var wrap = Forms.el('div', { class: 'equip' });
    var raw = str(values[column]);
    var slots = Equipped.parse(raw);
    var box = slotBox();
    var SLOT_W = box.w;
    var SLOT_H = box.h;
    var SLOT_SCALE = box.scale;

    var hidden = Forms.el('input', { type: 'hidden', name: column });
    // Blank is the one value that cannot stand: it emits ",," into the MakeCharacter packet and
    // desynchronises it. Anything else keeps its stored text until an edit lands.
    hidden.value = raw.trim() === '' ? Equipped.format(slots) : raw;

    // bodyState only distinguishes armed from unarmed in a resting pose (Sprites.part's note).
    // 3 is unarmed, and BOTH sheets that carry the column default to 3 — so a blank cell is an
    // unarmed row, and reading it as a bare 0 posed every unfilled NPC and item armed.
    //
    // `from` is therefore an EFFECTIVE record (Forms.effective): blanks already resolved to their
    // SQL defaults. Both the build-time call and every onFormChange call are given one, so this
    // does not re-derive the default and cannot disagree with the rest of the form about it.
    function armed(from) {
      return num(str(from && from.body_state).trim()) !== 3;
    }

    // NOT read once at build time. body_state lives outside this composite, so nothing in these
    // six rows can see it change, and a character switched from armed to unarmed left every slot
    // showing the pose it was built with. ctx.onFormChange is the cross-field notification, and
    // the six redraws below are what it drives.
    var equipped = armed(effective);
    var redraws = [];

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
      // No dispatch, for the reason bitmaskControl gives: both callers already produce a bubbling
      // event of their own — the graphic field's native `input`, and the `input` ColorPicker
      // dispatches after the slot's onChange. Note that both fire even when this returned early
      // on a typo; the listener then re-collects an UNCHANGED cell, so the previews stay put.
    }

    Equipped.SLOTS.forEach(function (slotName, index) {
      var row = Forms.el('div', { class: 'equip-slot' });
      row.appendChild(Forms.el('label', null, slotName));

      var input = Forms.el('input', {
        type: 'text', class: 'slot-graphic', placeholder: 'graphic id', autocomplete: 'off',
      });
      input.value = String(slots[index].graphic);

      var canvas = Forms.el('canvas', { width: SLOT_W * SLOT_SCALE, height: SLOT_H * SLOT_SCALE,
                                        class: 'preview' });
      var status = Forms.el('span', { class: 'status' });

      // CLICKING THE SLOT'S PREVIEW CANVAS OPENS THE BROWSER, LOCKED to the slot's own sprite
      // folder — picking a Helms sprite for a Feet slot is never right, and Appearance.CATEGORY
      // is the client's own map from one to the other (Shield and Weapon both land on Hands). On
      // the CANVAS rather than the graphic field, matching Pickers.browseOnClick and for its
      // reason: a field that opens a dialog on click cannot be clicked into for a hand edit, so
      // the picture browses and the field stays a plain text box. No separate Browse button
      // either way: the row is a fixed grid against a ~258px sidebar column. The canvas is the
      // opener the dialog hands focus back to.
      //
      // A pick goes in through the input's own `input` event rather than by calling sync() here, so
      // the typo check, the freeze gate, the redraw and app.js's delegated preview all run on the
      // one path they already run on for a typed id.
      Pickers.browseOnClick(canvas, 'browse ' + slotName + ' graphics', function (opener) {
        var gallery = galleryOf(opts);
        if (!gallery) return;
        gallery.open({
          bundle: 'parts',
          bundles: (ctx && ctx.bundles) || {},
          opener: opener,
          filter: { category: Appearance.CATEGORY[slotName], locked: true },
          current: { category: Appearance.CATEGORY[slotName], id: input.value },
          onPick: function (choice) {
            input.value = choice.id;
            input.dispatchEvent(new Event('input', { bubbles: true }));
          },
        });
      });

      // Whether this slot's blend is sitting at 0 having been MOVED there. Not read from the
      // stored value: every untinted slot arrives as `id,*` with a === 0, and six rows warning
      // about a colour nobody chose is noise.
      //
      // The other stored shape that WOULD lose something — a real colour parked behind a zero
      // blend, `12,164,51,31,0` — never reaches this note: Equipped.scan flags that exact shape
      // unfaithful (src/equipped.js zero-alpha check), so app.js's faithfulness gate refuses any
      // save that changes the row. Relax that check and this row silently stops warning.
      var discarding = false;

      // One writer for the row's status line, because two conditions share it. The typo wins:
      // it is the one that has frozen the cell, and burying it under an informational note
      // would hide the reason the field has stopped saving.
      function showStatus() {
        if (bad[index]) {
          status.textContent = 'graphic must be a whole number';
          status.className = 'status bad';
          return;
        }
        // Plain .status, not .bad — a blend of 0 is a legal value, and the note describes what
        // it does rather than refusing it.
        status.textContent = discarding ? 'colour not stored while blend is 0' : '';
        status.className = 'status';
      }

      function redraw() {
        // Scaled context, so the maths below stays in logical pixels; only the backing store
        // knows about SLOT_SCALE. Sprites.scaled owns the rest of that bargain.
        var target = Sprites.scaled(canvas, SLOT_SCALE, SLOT_W, SLOT_H);

        // Appearance.CATEGORY is the one mapping from slot to sprite folder — Shield and Weapon
        // both land on 'Hands'. Repeating it here would be a second copy to keep in step.
        var category = Appearance.CATEGORY[slotName];
        var rect = Sprites.part((ctx && ctx.bundles) || {}, category, slots[index].graphic,
                                equipped);
        // Sprites.draw ignores a null rect, but the centring below reads rect[2] first.
        if (!rect) return;

        Sprites.draw(target, (ctx && ctx.images) ? ctx.images.parts : null, rect,
                     Math.floor((SLOT_W - rect[2]) / 2),
                     Math.floor((SLOT_H - rect[3]) / 2),
                     slots[index]);
      }

      input.addEventListener('input', function () {
        var text = str(input.value).trim();
        if (text !== '' && !WHOLE.test(text)) {
          bad[index] = true;
          showStatus();
          // Through sync() rather than returning outright, so __frozen is raised the moment the
          // typo appears — not on the next good edit, by which time a save has already happened.
          sync();
          return;
        }
        delete bad[index];
        showStatus();
        slots[index].graphic = num(text);
        sync();
        redraw();
      });

      // The colour half of the slot. There is no freeze gate here and does not need to be: the
      // picker reports whole bytes or nothing, so a colour cannot be typo'd the way a graphic
      // field can. app.js's unfaithful-row gate still covers the whole cell, colour included.
      //
      // `tinted` tracks the blend, which is what keeps Equipped.format honest: it collapses a
      // zero-alpha slot to `id,*`, so pulling the blend to 0 genuinely DISCARDS the colour. The
      // status line says so at the moment it happens rather than leaving a designer to find the
      // colour gone tomorrow.
      //
      // Unlike rgbaControl there is no VISIBLE blend readout: this row has five cells in a fixed
      // grid and no track to spare for a sixth. The blend surfaces here are the popover, the
      // swatch's title tooltip and its aria-label — all three carry "N / 255 blend", so the
      // number is reachable with the popover shut, just not glanceable across six rows at once.
      var picker = ColorPicker.control({
        r: slots[index].r, g: slots[index].g, b: slots[index].b, a: slots[index].a,
        withAlpha: true,
        // The swatch sits in a 28px track partway along the row, so a popover anchored to its
        // left edge would hang off the side of the sidebar.
        align: 'right',
        onChange: function (colour) {
          slots[index].r = colour.r;
          slots[index].g = colour.g;
          slots[index].b = colour.b;
          slots[index].a = colour.a;
          slots[index].tinted = colour.a > 0;
          discarding = !slots[index].tinted;
          showStatus();
          sync();
          redraw();
        },
      });

      redraw();
      redraws.push(redraw);
      if (ctx && typeof ctx.onImagesReady === 'function') ctx.onImagesReady(redraw);

      row.appendChild(input);
      // In grid track order: label, graphic, swatch, preview — and the status line below,
      // spanning the whole row.
      row.appendChild(picker.node);
      row.appendChild(canvas);
      row.appendChild(status);
      wrap.appendChild(row);
    });

    // The pose, kept current. GATED ON A CHANGE OF POSE, not run per notification: onFormChange
    // fires on every edit anywhere in the form, and six part lookups plus six canvas redraws for
    // a keystroke in an unrelated field is work nobody can see. body_state resolves to one of two
    // poses, so the gate holds for every edit but the one that actually flips it.
    if (ctx && typeof ctx.onFormChange === 'function') {
      ctx.onFormChange(function (current) {
        var next = armed(current);
        if (next === equipped) return;
        equipped = next;
        redraws.forEach(function (fn) { fn(); });
      });
    }

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

  // byName as a LIST, for Forms.effective, which takes column descriptors. Object.create(null) has
  // no inherited keys, so every key here is a column the caller named.
  function descriptorsOf(byName) {
    return Object.keys(byName || {}).map(function (n) { return byName[n]; });
  }

  // Takes one options object, `{ comp, byName, values, effective, ctx, sheet }`. Named rather than
  // positional because most of them are maps or descriptor bags with no order a reader could infer
  // from a call site, and every kind below reads a different subset of them.
  //
  // `sheet` is only for the presentation tables in Layout — which graphic this sheet tints, today.
  // A caller that omits it gets the untinted control, which is what every sheet but Items gets
  // anyway.
  function control(opts) {
    var comp = opts.comp;
    var byName = opts.byName;
    var values = opts.values || {};
    var ctx = opts.ctx;
    var sheet = opts.sheet;

    var node;
    switch (comp.kind) {
      case 'Graphic':
        node = Pickers.graphicControl({
          graphicColumn: byName[comp.columns[0]],
          fileColumn: byName[comp.columns[1]],
          values: values, effective: opts.effective, ctx: ctx,
          tintColumns: Layout.tintColumns(sheet, comp.columns[0]),
          // Which atlas the browser shows. Layout answers 'icons' for everything but Spell Effects'
          // spell_animation, so the control needs no fallback of its own.
          galleryBundle: Layout.galleryBundle(sheet, comp.columns[0]),
          gallery: opts.gallery,
        });
        break;
      case 'Rgba': node = rgbaControl(comp, values); break;
      case 'Bitmask': node = bitmaskControl(comp, values, ctx); break;
      case 'IdList': node = idListControl(comp, values, ctx); break;
      case 'EquipSlots':
        // The effective record travels for body_state: a blank cell means the SQL default (an
        // UNARMED 3 on both sheets that have the column), and the pose the six slot previews draw
        // depends on it. Falls back to Forms.effective from byName's descriptors when the caller
        // supplied no resolved record, so the pose is right whichever way this control is built.
        node = equipSlotsControl(comp, values, ctx, {
          gallery: opts.gallery,
          effective: opts.effective || Forms.effective(values, descriptorsOf(byName)),
        });
        break;
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
  };
})();

if (typeof module !== 'undefined') module.exports = { Composites: Composites };
