// Typeahead over another sheet's id + name, and the graphic picker over GOOSE_SPRITES.icons.
//
// Both controls write ONLY the underlying cells, by name, so Forms.collect gathers them with
// every other field and neither needs a collect() of its own.
var Pickers = (function () {
  // How many rows the dropdown will show. The sheets behind it are big — Items is 649 rows and
  // NPC Spawns 4,322 — so this is a cap on a list a human is going to read, not on the search:
  // a query with more hits than this is a query that needs another character, and the list
  // stays scannable instead of becoming a second copy of the sheet.
  var LIMIT = 50;

  // The icon preview's logical box, and the factor the canvas backing store is scaled by so it
  // is legible on screen — 64 at 2x, so 128 CSS pixels of canvas.
  //
  // THE BOX IS 64 because the median icon is 32x32 but the bundle holds sprites up to 128x128,
  // and the previous 48 clipped them; 64 covers the common large sizes. Anything bigger is still
  // clipped — a bundle fact, not a regression, and preferable to a 128 box leaving every 32px
  // icon adrift in whitespace.
  var ICON_BOX = 64;
  var ICON_SCALE = 2;

  // The worn-part preview's logical box and its scale, so the canvas is 80x112. Deliberately the
  // same numbers as Composites' SLOT_W/SLOT_H/SLOT_SCALE: this shows one equipped sprite in the
  // form column, which is exactly what an equip-slot row shows, and two different boxes for the
  // same thing would make the form look uneven for no reason. Taller than the icon box because a
  // character part is tall and narrow where an icon is square.
  var PART_W = 40;
  var PART_H = 56;
  var PART_SCALE = 2;

  // Slots inside LIMIT that name hits keep even when id-prefix hits could fill the whole list.
  // Without it, one digit typed against Items produces >100 id-prefix hits (1, 10-19, 100-199,
  // …) and slicing after concatenation would hide EVERY name match behind them — so a designer
  // hunting "Sword" who typed "1" first would be told, wrongly, that there is nothing else.
  //
  // 25 IS MEASURED, NOT GUESSED. Over 8,144 realistic queries against indexes built from the
  // real IllutiaGoose.db (Items 1595 rows, NPCs 655, Spell Effects 623), the reserve wasted a
  // slot in zero of them, and exactly ten (sheet, query) pairs saturated it: Items 1-5, NPCs
  // 1/3/4/5 and Spell Effects 2. Saturation is not a graceful degradation here, because name
  // hits are kept in SHEET ROW ORDER — the ten that survived a reserve of 10 were reliably the
  // least useful ones, so Items "1" showed "Hair Cut: 1" and "Face: 16" while hiding "Flame
  // Sword - 1H Graphic". At 25 all ten cases show every name hit they have (163 of 163), and
  // the cost is 15 id-prefix rows out of buckets of 100-716 that the user has to refine anyway.
  var NAME_RESERVE = 25;

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  // Ids arrive from the Sheets API as whatever it decided the cell was — the number 42, '42',
  // or ' 042 ' from a hand-typed row — and Validation.validateCell compares them with
  // Number(value) against a Set of Numbers. Matching on the raw text instead would make the
  // picker say "not found in Items" for a value the validator is perfectly happy to save.
  // Anything that is not an integer literal is left exactly as it is: it is not an id, and
  // Validation will report it as such under the field.
  //
  // ONE KNOWN DIVERGENCE from Validation, in the safe direction: '42.0' passes
  // Validation.validateCell (its numeric regex allows a fraction, and Number('42.0') is 42)
  // but is not an integer literal here, so the picker calls it "not found" while the validator
  // would save it. It takes a TEXT-FORMATTED cell to produce one — Sheets hands back a numeric
  // 42.0 as 42 — and over-reporting is the right way round: the label is a hint, the error
  // slot under the field is the authority.
  function key(value) {
    var text = str(value).trim();
    return /^-?\d+$/.test(text) ? String(Number(text)) : text;
  }

  function isWhole(text) {
    return /^\d+$/.test(text);
  }

  // parseInt(v, 10), matching Sprites.num(), Appearance.num(), Equipped.num(), Preview.num() and
  // Composites.num() so no two modules disagree about what a spreadsheet cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  // The tint a set of tint columns currently asks for, read out of a collected form. Null when
  // the graphic has no tint columns at all (Layout.TINTS says which do), which is not the same as
  // a tint of zero: Sprites.draw treats both as "draw it plain", so the distinction is only about
  // whether there is anything to read.
  //
  // No clamping and no zero-alpha collapse here — Sprites.applyTint clamps every channel and
  // treats a zero blend factor as NoTint, colour and all, exactly as Icon.cs does. A second copy
  // of either rule is a second rule that can drift.
  function tintFrom(tintColumns, values) {
    if (!tintColumns) return null;
    return {
      r: num(values[tintColumns[0]]),
      g: num(values[tintColumns[1]]),
      b: num(values[tintColumns[2]]),
      a: num(values[tintColumns[3]]),
    };
  }

  // Three buckets, best first: exact id, then id prefix, then name substring. An entry lands in
  // AT MOST ONE — matching both an id prefix and a name is one row in the list, not two.
  function search(entries, query) {
    // key() trims, so a query of pure whitespace arrives here as the empty query.
    var q = key(str(query).toLowerCase());
    if (q === '') return entries.slice(0, LIMIT);

    var exact = [], idPrefix = [], nameHit = [];

    for (var i = 0; i < entries.length; i++) {
      var e = entries[i];
      var id = key(e.id);
      var name = str(e.name).toLowerCase();

      if (id === q) exact.push(e);
      else if (id.indexOf(q) === 0) idPrefix.push(e);
      else if (name.indexOf(q) !== -1) nameHit.push(e);
    }

    // Budget rather than a plain slice: exact matches first, then id-prefix hits get whatever
    // is left MINUS the reserve, and name hits take the rest. When either bucket is short the
    // other simply spills into the space, so the list is only ever cut when there is genuinely
    // too much to show.
    var room = LIMIT - exact.length;
    var reserved = Math.min(nameHit.length, NAME_RESERVE, room);
    return exact.concat(idPrefix.slice(0, room - reserved), nameHit).slice(0, LIMIT);
  }

  // True when `node` is the list or anything inside it. Walks parentNode rather than using
  // Node.contains, which the test DOM does not model.
  function within(container, node) {
    for (var at = node; at; at = at.parentNode) {
      if (at === container) return true;
    }
    return false;
  }

  // FK control: a text input holding the id, a live label showing the resolved name, and a
  // results list. Writes only the id back to the sheet.
  //
  // The list is a COMBOBOX in the aria-activedescendant style: focus never leaves the input, and
  // which row is current is stated by an attribute rather than by where focus is. That is what
  // makes the two input paths agree — the mouse path keeps focus on the input by cancelling
  // mousedown (see below), so a keyboard path that moved focus into the list would need the
  // opposite behaviour from the same blur handler.
  function fkControl(column, value, ctx) {
    var wrap = Forms.el('div', { class: 'picker' });
    var listId = 'f-' + column.name + '-list';
    var input = Forms.el('input', {
      name: column.name,
      id: 'f-' + column.name,
      type: 'text',
      autocomplete: 'off',
      placeholder: Forms.placeholderFor(column),
      // role=combobox rather than a bare text input: without it a screen reader announces an
      // ordinary field and never mentions that a list of suggestions appeared underneath it.
      // aria-autocomplete=list is the honest value — typing filters the list and does NOT
      // complete the field's text, which 'both' would promise.
      role: 'combobox',
      'aria-autocomplete': 'list',
      'aria-expanded': 'false',
      'aria-controls': listId,
    });
    // str(), not `value || ''`: a stored 0 means "none" and is a REAL value. Blanking it here
    // would write blank on the next save, and blank means "use the SQL default" — which for
    // most of these columns is a different number entirely (see Forms.str).
    input.value = str(value);

    var label = Forms.el('span', { class: 'resolved' });
    var list = Forms.el('div', { class: 'results', id: listId, role: 'listbox' });
    list.hidden = true;

    // The rows currently in the list, in list order, and which of them is active. -1 is a real
    // state, not "nothing yet": the list opens with NO active row, so the first Enter after a
    // keystroke does nothing rather than silently accepting whichever row happened to be first.
    var rows = [];
    var active = -1;

    // Read through to ctx on every use rather than capturing the array once.
    // App.loadReferencedSheets fills pickerData over google.script.run, and a control built
    // before its sheet lands would otherwise be an empty picker forever.
    function entries() {
      return (ctx && ctx.pickerData && ctx.pickerData[column.ref]) || [];
    }

    function find(text) {
      var wanted = key(text);
      var all = entries();
      for (var i = 0; i < all.length; i++) {
        if (key(all[i].id) === wanted) return all[i];
      }
      return null;
    }

    function resolve() {
      var v = str(input.value).trim();
      // Blank and the literal '0' are "none" — the same two Validation.validateCell exempts
      // from its FK check, trim included. '00' is deliberately NOT among them: Validation
      // looks that one up and reports it, so the picker must not quietly call it none.
      //
      // A blank REQUIRED fk also reads "none", in the neutral style, where validateCell says
      // "is required". Deliberate: this label answers "what does this id point at", and the
      // error slot Forms.showErrors fills is what says whether the cell may stay that way.
      if (v === '' || v === '0') {
        label.textContent = 'none';
        label.className = 'resolved';
        return;
      }

      var hit = find(v);
      if (hit) {
        label.textContent = str(hit.name) || '(unnamed)';
        label.className = 'resolved';
        return;
      }

      // An empty picker list means the sheet has not arrived yet, not that the id is wrong.
      // Saying "not found" then would accuse the user of a bad value on every freshly opened
      // record.
      if (entries().length > 0) {
        label.textContent = 'not found in ' + column.ref;
        label.className = 'resolved bad';
        return;
      }

      // Except when it has already failed. "loading Items…" on a list that is never coming is a
      // wait with no end, and it hides the reason the save is about to be refused. Marked bad,
      // because it is: App's save gate will not write an id it cannot check.
      var failed = (ctx && ctx.refErrors && ctx.refErrors.indexOf(column.ref) !== -1);
      label.textContent = failed ? 'could not load ' + column.ref : 'loading ' + column.ref + '…';
      label.className = failed ? 'resolved bad' : 'resolved';
    }

    // Both halves of "is the list showing" move together — the attribute is what a screen reader
    // reads, and hiding without clearing it would announce an open list that is not there.
    // Closing also drops the active row: reopening starts from no selection again.
    function setOpen(open) {
      list.hidden = !open;
      input.setAttribute('aria-expanded', open ? 'true' : 'false');
      if (!open) setActive(-1);
    }

    // Marks one row current WITHOUT moving focus. Any index outside the list means "none", so
    // callers can pass -1 or an index into a list that has just been rebuilt.
    function setActive(next) {
      if (rows[active]) {
        rows[active].className = 'result';
        rows[active].removeAttribute('aria-selected');
      }

      active = (next >= 0 && next < rows.length) ? next : -1;
      if (active < 0) {
        input.removeAttribute('aria-activedescendant');
        return;
      }

      var row = rows[active];
      row.className = 'result active';
      row.setAttribute('aria-selected', 'true');
      // The class is what a sighted user sees and aria-activedescendant is what a screen reader
      // follows; both are needed, and neither moves focus off the input.
      input.setAttribute('aria-activedescendant', row.id);
      // .results is a 200px box over as many as LIMIT rows, so arrowing past the fifth one walks
      // out of view without this.
      row.scrollIntoView({ block: 'nearest' });
    }

    // Writes the CANONICAL id, so picking ' 042 ' from a hand-edited sheet stores '42'. Reads it
    // back off the row rather than from a closure so the mouse and keyboard paths are the same
    // code and cannot drift apart.
    function accept(row) {
      input.value = row.getAttribute('data-id');
      setOpen(false);
      resolve();
    }

    function refresh() {
      var results = search(entries(), input.value);
      // Drops the previous rows and, with them, their click handlers: the nodes are
      // unreachable afterwards, so nothing is left listening and nothing leaks.
      list.innerHTML = '';
      // Before the rows are replaced, so aria-activedescendant never names a detached node and
      // the new list opens with nothing active.
      setActive(-1);
      rows = [];

      results.forEach(function (e, index) {
        var id = key(e.id);
        var row = Forms.el('button', {
          type: 'button',
          class: 'result',
          'data-id': id,
          // An id per row, because aria-activedescendant can only point at one.
          id: listId + '-' + index,
          role: 'option',
          // LOAD-BEARING, not just an ARIA nicety: these are <button>s, so they are focusable by
          // default, and Tab out of the input would otherwise walk through up to LIMIT of them
          // one at a time before reaching the next field.
          tabindex: '-1',
        }, id + ' — ' + str(e.name));
        row.addEventListener('click', function () { accept(row); });
        rows.push(row);
        list.appendChild(row);
      });

      setOpen(results.length > 0);
    }

    input.addEventListener('input', function () { refresh(); resolve(); });
    // Focusing an empty field shows the head of the list, so the control answers "what can go
    // in here?" without the user having to guess a first character.
    input.addEventListener('focus', refresh);

    input.addEventListener('keydown', function (event) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        // Otherwise the arrow key ALSO moves the caret to the far end of the field, so walking
        // the list scrambles where the next typed character lands.
        event.preventDefault();
        // Reopens a list dismissed with Escape, rather than making the user retype a character.
        if (list.hidden) refresh();
        if (!rows.length) return;

        var step = event.key === 'ArrowDown' ? 1 : -1;
        // Wraps, and enters from the correct end: with nothing active, Down takes the first row
        // and Up the last.
        setActive(active < 0 ? (step === 1 ? 0 : rows.length - 1)
                             : (active + step + rows.length) % rows.length);
        return;
      }

      if (event.key === 'Enter') {
        // Only when a row is actually current. Enter on a hand-typed id is left entirely alone —
        // it is not this control's key, and swallowing it would break whatever the form does with
        // it.
        if (active >= 0) {
          event.preventDefault();
          accept(rows[active]);
        }
        return;
      }

      if (event.key === 'Escape') {
        // Guarded, because Escape with the list already shut belongs to the dialog or the sidebar
        // around this control, not to us.
        if (!list.hidden) {
          event.preventDefault();
          setOpen(false);
        }
        return;
      }

      // Tab with a row current takes it, then lets focus move on as normal (no preventDefault):
      // having arrowed to a row, leaving the field is a commitment, not a cancellation. With no
      // row current, Tab is an ordinary Tab and blur closes the list.
      if (event.key === 'Tab' && active >= 0) accept(rows[active]);
    });

    // Cancelling mousedown is what makes this safe: moving focus is the DEFAULT ACTION of
    // mousedown, so preventing it keeps focus on the input and blur never fires — the click
    // then lands on a list that is still there. The alternative (hide on blur after a ~150ms
    // timer, hoping the click wins) is a race that loses on a slow frame, and losing it means
    // the user's click silently does nothing.
    list.addEventListener('mousedown', function (event) { event.preventDefault(); });

    // relatedTarget is the node focus is moving TO. The mouse path above means focus normally
    // never leaves the input at all, so this covers the other ways focus can land on a row —
    // a row.focus() from assistive tech, or a browser that focuses the mousedown target anyway —
    // where hiding the list would pull it out from under the focus that just arrived in it.
    input.addEventListener('blur', function (event) {
      if (event && within(list, event.relatedTarget)) return;
      setOpen(false);
    });

    resolve();
    wrap.appendChild(input);
    wrap.appendChild(label);
    wrap.appendChild(list);
    return wrap;
  }

  // Graphic control: the graphic and sheet cells side by side, a canvas preview, and a status
  // line. Blank or 0 in both means "no graphic".
  //
  // ARGUMENT ORDER IS THE SCHEMA'S, NOT Sprites'. The Graphic composite declares its columns
  // [graphic, file] (graphic_tile + graphic_file, spell_animation + spell_animation_file, …)
  // while Sprites.icon takes (bundles, SHEET, graphic). Swapping the two resolves nothing at
  // all and does it silently, so the order is asserted in the tests.
  //
  // `tintColumns` is Layout.tintColumns(sheet, graphicColumn.name) — the four cells whose colour
  // the game blends into this graphic, or null for a graphic it draws plain. They belong to
  // ANOTHER control (the Rgba composite's hidden inputs), so they cannot be read from this
  // control's own two fields; ctx.onFormChange is how the current values arrive after an edit.
  function graphicControl(graphicColumn, fileColumn, values, ctx, tintColumns) {
    var wrap = Forms.el('div', { class: 'graphic' });
    // A 64-pixel logical box drawn at 2x — see ICON_BOX for why 64.
    var canvas = Forms.el('canvas',
      { width: ICON_BOX * ICON_SCALE, height: ICON_BOX * ICON_SCALE, class: 'preview' });

    function cell(column, fallback) {
      var node = Forms.el('input', {
        name: column.name,
        id: 'f-' + column.name,
        type: 'text',
        autocomplete: 'off',
        placeholder: Forms.placeholderFor(column) || fallback,
      });
      node.value = str(values[column.name]);
      return node;
    }

    var gInput = cell(graphicColumn, 'graphic');
    var fInput = cell(fileColumn, 'sheet');
    var status = Forms.el('span', { class: 'status' });

    // The last form state seen, which the tint is read out of. Seeded with the record's own values
    // so the FIRST draw is tinted too — a preview that only picked the tint up after an unrelated
    // keystroke would show every freshly opened record plain.
    var latest = values;

    // Says WHY nothing is on the canvas. A blank preview is otherwise indistinguishable from a
    // typo, and nothing else catches one: the cells are optional, so Validation passes a graphic
    // that names art the bundle does not have, and Equipped.format/isFaithful never see this
    // column at all.
    //
    // Which is also why one of these states is not only shown but PUBLISHED, on
    // wrap.__graphicError for the save path to gate on (see the contract at the top of app.js).
    //
    // ONE of them: `block` is narrower than `bad`, and deliberately.
    //   - Not a whole number already fails Validation's numeric check on the column itself, which
    //     reports it under the field. A second refusal would say the same thing twice.
    //   - HALF A PAIR IS LEGAL AND SHIPPED. 176 of the 259 Spell Effects rows set spell_animation
    //     with spell_animation_file left 0, and the server sends both to the client exactly as
    //     stored (SpellEffect.cs:520). Refusing it would lock two thirds of that sheet.
    //   - A complete pair naming art the bundle does not have is the one the design rule speaks
    //     to — "a non-blank, non-zero graphic must resolve in the bundle" — and the one nothing
    //     else can see: both cells are optional INTEGERs, so Validation passes a nonexistent
    //     sheet:graphic as a perfectly good number and the only other signal is a blank canvas.
    //     All 649 Items, 152 Spells and 146 buff graphics in the shipped data resolve, so this
    //     refuses nothing that exists today.
    function describe(rect, checkable) {
      var g = str(gInput.value).trim();
      var f = str(fInput.value).trim();
      var noGraphic = (g === '' || g === '0');
      var noSheet = (f === '' || f === '0');

      if (noGraphic && noSheet) return { text: 'no graphic', bad: false };
      // A blank cell is not a typo — it is half a pair, which is its own message. Only a cell
      // with something in it that is not a whole number is one.
      if ((g !== '' && !isWhole(g)) || (f !== '' && !isWhole(f))) {
        return { text: 'graphic and sheet must be whole numbers', bad: true };
      }
      if (noGraphic || noSheet) return { text: 'graphic and sheet must both be set', bad: true };
      // No bundle, no verdict. Blocking here would brick every sheet on a deploy where the
      // 1.7MB icons include failed to load — and loadBundle is explicit that a missing bundle
      // leaves the form usable without art rather than unusable.
      if (!checkable) {
        return { text: 'cannot check sheet ' + f + ' graphic ' + g + ' — no icon art loaded',
                 bad: true };
      }
      if (!rect) return { text: 'no art for sheet ' + f + ' graphic ' + g, bad: true, block: true };
      return { text: '', bad: false };
    }

    function redraw() {
      // The raw cells go straight to Sprites, which does its own parseInt-based coercion. A
      // Number() here as well would be a SECOND rule for what a cell means — and the two
      // disagree ('1e3' is 1000 to one and 1 to the other), so the preview could resolve a
      // different sprite from the one the lookup key names.
      var bundles = (ctx && ctx.bundles) || {};
      var rect = Sprites.icon(bundles, fInput.value, gInput.value);

      var state = describe(rect, !!(bundles.icons && bundles.icons.rects));
      status.textContent = state.text;
      status.className = state.bad ? 'status bad' : 'status';
      // Named, because a form can hold several of these (Spell Effects has two) and "fix the
      // graphic" would not say which. Null rather than '' so the save path's test is a plain
      // truthiness check on a property most nodes do not have at all.
      wrap.__graphicError = state.block ? (graphicColumn.name + ': ' + state.text) : null;

      // Scaled context, so everything below is in logical pixels: only the backing store knows
      // about ICON_SCALE. Sprites.scaled owns the rest of that bargain.
      var target = Sprites.scaled(canvas, ICON_SCALE, ICON_BOX, ICON_BOX);
      // Load-bearing, not defensive: Sprites.draw would ignore a null rect happily, but the
      // centring below reads rect[2] first and would throw before ever reaching it.
      if (!rect) return;

      // Sprites.draw is a no-op while the bundle PNG is still decoding, which is why redraw is
      // also registered below rather than only run once here.
      Sprites.draw(target, (ctx && ctx.images) ? ctx.images.icons : null, rect,
                   Math.floor((ICON_BOX - rect[2]) / 2),
                   Math.floor((ICON_BOX - rect[3]) / 2),
                   tintFrom(tintColumns, latest));
    }

    gInput.addEventListener('input', redraw);
    fInput.addEventListener('input', redraw);

    redraw();
    if (ctx && typeof ctx.onImagesReady === 'function') ctx.onImagesReady(redraw);
    // Only worth subscribing when there is a cross-field cell to watch. An untinted graphic redraws
    // from its own two `input` handlers and nothing else can change what it shows, so registering
    // unconditionally would run a redraw per keystroke in item_description for no reason at all.
    if (tintColumns && ctx && typeof ctx.onFormChange === 'function') {
      ctx.onFormChange(function (current) { latest = current; redraw(); });
    }

    wrap.appendChild(canvas);
    wrap.appendChild(gInput);
    wrap.appendChild(fInput);
    wrap.appendChild(status);
    return wrap;
  }

  // Part control: one id field over a CHARACTER PART sprite rather than an inventory icon, plus a
  // preview and a status line. graphic_equip is the only such column today — a plain Int with no
  // composite, which is why it had no preview at all and why forms.js routes it here explicitly.
  //
  // WHAT MAKES IT DIFFERENT FROM graphicControl: there is no sheet cell. The sprite FOLDER comes
  // from another column entirely (`spec.categoryFrom`, i.e. item_slot), through the client's own
  // slot map in Appearance.slotFor — so the same id is a helmet or a pair of boots depending on a
  // cell this control does not own. That, plus the tint and the armed/unarmed pose, is three
  // cross-field reads, and all three arrive through ctx.onFormChange.
  //
  // NO SAVE GATE. graphicControl publishes __graphicError because the design rule for an inventory
  // icon is that a complete pair must resolve, and every shipped pair does. Nothing of the kind has
  // been established for graphic_equip, so refusing a save on it would be inventing a rule and
  // could lock rows that ship today. The status line reports a miss; that is all it does.
  function partControl(column, values, ctx, spec, tintColumns) {
    var wrap = Forms.el('div', { class: 'graphic' });
    var canvas = Forms.el('canvas',
      { width: PART_W * PART_SCALE, height: PART_H * PART_SCALE, class: 'preview' });

    var input = Forms.el('input', {
      name: column.name,
      id: 'f-' + column.name,
      type: 'text',
      autocomplete: 'off',
      placeholder: Forms.placeholderFor(column) || 'graphic',
    });
    // str(), not `value || ''`: a stored 0 is "no equip graphic" and a REAL value, and blanking it
    // would write blank — which means "use the SQL default" — on the next save.
    input.value = str(values[column.name]);

    var status = Forms.el('span', { class: 'status' });
    var latest = values;

    function redraw() {
      var target = Sprites.scaled(canvas, PART_SCALE, PART_W, PART_H);
      var text = str(input.value).trim();
      // The slot is read from the LIVE form, so changing item_slot from Helmet to Shoes moves this
      // preview into another folder without the record being reopened.
      var slot = Appearance.slotFor(latest[spec.categoryFrom]);

      function say(message, bad) {
        status.textContent = message;
        status.className = bad ? 'status bad' : 'status';
      }

      // First, because it is a fact about the ROW and not about this cell: an id in a slot the
      // client never draws is not an error, and calling it one would flag most of the Ring and
      // Misc rows in the sheet. The canvas stays blank and the line says why it is blank.
      if (!slot) { say('this slot is not drawn on the character', false); return; }

      if (text === '' || text === '0') { say('no graphic', false); return; }
      // A non-whole cell already fails Validation's numeric check on the column itself, which
      // reports it under the field — same reasoning as graphicControl's `block`-narrower-than-`bad`
      // note, so this is shown and not gated.
      if (!isWhole(text)) { say('graphic must be a whole number', true); return; }

      var category = Appearance.CATEGORY[slot];
      var bundles = (ctx && ctx.bundles) || {};
      if (!bundles.parts || !bundles.parts.rects) {
        say('cannot check ' + category + ' graphic ' + text + ' — no character art loaded', true);
        return;
      }

      // A mount is a body in a MOUNTED clip, and Sprites.part deliberately never falls back to
      // one — so the two lookups are genuinely different functions, not one with a flag.
      var rect = slot === 'Mount' ? Sprites.mount(bundles, text)
        : Sprites.part(bundles, category, text, Preview.isArmed(latest.body_state));

      if (!rect) { say('no art for ' + category + ' graphic ' + text, true); return; }
      say('', false);

      Sprites.draw(target, (ctx && ctx.images) ? ctx.images.parts : null, rect,
                   Math.floor((PART_W - rect[2]) / 2),
                   Math.floor((PART_H - rect[3]) / 2),
                   tintFrom(tintColumns, latest));
    }

    input.addEventListener('input', redraw);

    redraw();
    if (ctx && typeof ctx.onImagesReady === 'function') ctx.onImagesReady(redraw);
    // Unconditional, unlike graphicControl's: the category alone is a cross-field read, so this
    // control has something to follow whether or not it is tinted.
    if (ctx && typeof ctx.onFormChange === 'function') {
      ctx.onFormChange(function (current) { latest = current; redraw(); });
    }

    wrap.appendChild(canvas);
    wrap.appendChild(input);
    wrap.appendChild(status);
    return wrap;
  }

  return {
    search: search,
    fkControl: fkControl,
    graphicControl: graphicControl,
    partControl: partControl,
    LIMIT: LIMIT,
    PART_W: PART_W,
    PART_H: PART_H,
    PART_SCALE: PART_SCALE,
  };
})();

if (typeof module !== 'undefined') module.exports = { Pickers: Pickers };
