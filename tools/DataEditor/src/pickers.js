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

  // The tint a set of tint columns currently asks for, read out of a collected form. Null when
  // the graphic has no tint columns at all (Layout.TINTS says which do), which is not the same as
  // a tint of zero: Sprites.draw treats both as "draw it plain", so the distinction is only about
  // whether there is anything to read.
  //
  // The COERCION is Preview.tintOf's and not a copy of it. Four cells to a tint object is one
  // boundary, not two: the hazard it exists for (Sprites.draw's no-tint shortcut is `!tint.a`,
  // and the string '0' is truthy, so a raw cell would take the per-pixel path to compute an
  // identity blend) is stated once, there, along with why clamping is left to Sprites.applyTint.
  // Only the SIGNATURES differ — tintOf reads named fields off an item, this reads four column
  // names out of a form — so this is the adapter and nothing more.
  //
  // Preview is resolved as a free global at call time, exactly as it is for isArmed below.
  function tintFrom(tintColumns, values) {
    if (!tintColumns) return null;
    return Preview.tintOf({
      r: values[tintColumns[0]],
      g: values[tintColumns[1]],
      b: values[tintColumns[2]],
      a: values[tintColumns[3]],
    });
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

  // The list to show for a field the user has NOT typed into — the id it already holds, first, then
  // the head of the sheet. search() is the wrong function for that job and was the complaint: a
  // field showing "42 — Iron Sword" searches on '42', which matches 42 and every id it prefixes, so
  // a control with a value selected listed essentially that value and the user had to CLEAR the
  // field before the sheet's other rows would appear at all.
  //
  // The current id is hoisted to the front rather than left in sheet order, for two reasons: it is
  // the row the list opens on (refresh marks it active, so Down and Up walk out from it), and at 4
  // rows on screen out of LIMIT it is the only way it is visible without scrolling.
  //
  // STILL CAPPED AT LIMIT, so this is the head of a big sheet and not a second copy of it — Items is
  // 1,595 rows. Everything past the cap is reached by typing, which switches back to search().
  function browse(entries, id) {
    var wanted = key(id);
    var exact = [], rest = [];

    for (var i = 0; i < entries.length; i++) {
      if (key(entries[i].id) === wanted) exact.push(entries[i]);
      else rest.push(entries[i]);
    }

    return exact.concat(rest).slice(0, LIMIT);
  }

  // The graphic browser, or null when it is not on the page. Resolved at CALL time — like every
  // other cross-module reference in this file — and overridable through the control's options
  // object, which is what Task 6's collapse to options objects was for: a test can hand one control
  // a spy without reaching for a global, and a build that omits gallery.html degrades to a form
  // with no Browse buttons rather than a form that throws on the first click.
  //
  // ColorPicker.within/Pickers.within already set the precedent for a two-line helper duplicated
  // across modules with a stated reason; composites.js carries the same one.
  function galleryOf(opts) {
    if (opts && opts.gallery) return opts.gallery;
    return (typeof Gallery !== 'undefined' && Gallery) ? Gallery : null;
  }

  // Makes the PREVIEW CANVAS open the browser on click — there is no separate Browse button, and
  // it is deliberately NOT the text field: a field that opens a dialog on click cannot be clicked
  // into for a hand edit, so putting the browse on the picture leaves the field an ordinary text
  // box (click in, type, done) while the canvas — the thing that LOOKS like art to change — does
  // the browsing. The canvas is also the opener the dialog hands focus back to.
  //
  // A canvas is not natively interactive, so the click alone would leave keyboard and screen
  // reader users with no path at all: role=button and tabindex=0 put it in the tab order as a
  // button, and Enter/Space activate it the way they activate a real one. `label` lands on title
  // and aria-label so both a hover tooltip and a screen reader say what it does; aria-haspopup
  // says a dialog is coming. `onOpen(opener)` does the opening — each control states its own
  // bundle, filter and callback. It may decline (partControl's undrawn slots have nothing to
  // browse), which leaves the click an ordinary click.
  function browseOnClick(canvas, label, onOpen) {
    canvas.setAttribute('data-browse', '');
    canvas.setAttribute('title', label);
    canvas.setAttribute('aria-label', label);
    canvas.setAttribute('aria-haspopup', 'dialog');
    canvas.setAttribute('role', 'button');
    canvas.setAttribute('tabindex', '0');
    canvas.addEventListener('click', function () { onOpen(canvas); });
    canvas.addEventListener('keydown', function (event) {
      if (event.key !== 'Enter' && event.key !== ' ') return;
      // Space would otherwise also scroll the page — the same page-scroll default a real
      // button's activation suppresses.
      event.preventDefault();
      onOpen(canvas);
    });
    return canvas;
  }

  // True when `node` is the list or anything inside it. Walks parentNode rather than using
  // Node.contains, which the test DOM does not model.
  function within(container, node) {
    for (var at = node; at; at = at.parentNode) {
      if (at === container) return true;
    }
    return false;
  }

  // FK control: a combobox that DISPLAYS "id — name" once the id resolves, over a hidden input
  // that carries the bare id. Writes only the id back to the sheet — the split exists because the
  // display and the cell genuinely differ now: "42 — Iron Sword" is what a designer wants to see
  // in the field, and is not a value the sheet may ever receive. The status label underneath is
  // kept for what the field cannot say — none / not found / loading / failed — and cleared when
  // the name is in the field, where it would say the same thing twice.
  //
  // The list is a COMBOBOX in the aria-activedescendant style: focus never leaves the input, and
  // which row is current is stated by an attribute rather than by where focus is. That is what
  // makes the two input paths agree — the mouse path keeps focus on the input by cancelling
  // mousedown (see below), so a keyboard path that moved focus into the list would need the
  // opposite behaviour from the same blur handler.
  function fkControl(column, value, ctx) {
    // See forms.js columnControl for why this rides on ctx.
    var prefix = Forms.idPrefixOf(ctx && ctx.idPrefix);
    var wrap = Forms.el('div', { class: 'picker' });
    var listId = prefix + column.name + '-list';
    var input = Forms.el('input', {
      // NO name — Forms.collect must never sweep the display text into the record. The id keeps
      // prefix + name so the field's <label for> still lands here, on the thing a user types into.
      id: prefix + column.name,
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

    // THE CELL. Seeded verbatim — str(), not `value || ''`: a stored 0 means "none" and is a
    // REAL value, and blank means "use the SQL default", a different number entirely. Verbatim
    // also means a padded ' 042 ' round-trips through Forms.collect untouched until the user
    // actually picks or types something, honouring "opening a record must not change it".
    var hidden = Forms.el('input', { type: 'hidden', name: column.name });
    hidden.value = str(value);

    // What our own display() writes, and the only shape it writes: "<canonical id> — <name>".
    // Text matching it is read back as its id; anything else is the user's own text, verbatim.
    var FORMATTED = /^(-?\d+) — /;

    function idOf(text) {
      var m = FORMATTED.exec(str(text));
      return m ? m[1] : str(text);
    }

    var label = Forms.el('span', { class: 'resolved' });
    var list = Forms.el('div', { class: 'results', id: listId, role: 'listbox' });
    list.hidden = true;

    // The rows currently in the list, in list order, and which of them is active. -1 is a real
    // state, not "nothing yet": the list opens with NO active row, so the first Enter after a
    // keystroke does nothing rather than silently accepting whichever row happened to be first.
    var rows = [];
    var active = -1;

    // Whether the field still holds what display() put there, i.e. whether the user has typed since.
    // It is what picks browse() over search() in refresh(), and the distinction is the fix for
    // "a selected value filters the list down to itself": a field the user has not touched is not a
    // QUERY, it is a value, and the list beside it should offer the alternatives.
    //
    // Set true by display() — which is every path that rewrites the field from the cell: build,
    // accept, and blur — so a filter typed and then abandoned reverts to browsing when the field is
    // focused again.
    var pristine = true;

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
      var v = str(hidden.value).trim();
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
        // While the field holds the raw id (the user is typing), the label is the live "this is
        // what that id means" feedback. Once display() has put "id — name" IN the field, the
        // label saying the name again is the duplication this control was reworked to remove.
        label.textContent = FORMATTED.test(str(input.value)) ? ''
          : (str(hit.name) || '(unnamed)');
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

    // Rewrites the FIELD from the cell: "id — name" when the id resolves, the cell's own text
    // verbatim when it does not (blank, none, a typo, a list that has not arrived). Runs at
    // build, on accept and on blur — never per keystroke, which would fight the caret.
    //
    // The id half is CANONICAL (key()), so a stored ' 042 ' displays as "42 — Iron Sword" while
    // the CELL keeps ' 042 ' until the user themselves changes something.
    function display() {
      var v = str(hidden.value).trim();
      var hit = (v === '' || v === '0') ? null : find(v);
      input.value = hit ? key(v) + ' — ' + (str(hit.name) || '(unnamed)') : str(hidden.value);
      // The field now holds the CELL rather than anything the user typed, so the next refresh
      // browses. See `pristine`.
      pristine = true;
      resolve();
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

    // Writes the CANONICAL id into the cell, so picking ' 042 ' from a hand-edited sheet stores
    // '42' — and puts "id — name" in the field through display(). Reads the id back off the row
    // rather than from a closure so the mouse and keyboard paths are the same code and cannot
    // drift apart.
    function accept(row) {
      hidden.value = row.getAttribute('data-id');
      setOpen(false);
      display();
    }

    function refresh() {
      // idOf, not the raw text: a field showing "42 — Iron Sword" is the id 42, and the whole
      // display string matches nothing at all.
      //
      // WHICH LIST depends on whether the user has typed. Untouched, the field is a value and the
      // list is the sheet with that value at the top (browse); typed into, it is a query and the
      // list is the matches (search). Using search for both is what made a filled-in picker show
      // only its own value.
      var wanted = idOf(input.value);
      var results = pristine ? browse(entries(), wanted) : search(entries(), wanted);
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

      // THE CURRENT VALUE OPENS AS THE ACTIVE ROW, and only in browse mode, where browse() has just
      // put it at index 0. That is what makes the list navigable from where the record already is:
      // Down moves to the next row rather than to the first, and Enter on it is a no-op re-accept of
      // the value already in the cell — never the accident the "no row active" rule guards against,
      // which is about a row the user did not choose. A blank or unresolvable field has no exact
      // match, so results[0] is not it and nothing is marked.
      if (pristine && rows.length && key(results[0].id) === key(wanted)) setActive(0);
    }

    // Every keystroke writes the cell: what the user typed, verbatim — exactly what the old
    // single-input control stored — EXCEPT when they are editing our own "id — name" text, where
    // idOf peels the display back to the id it stands for. So backspacing through the name half
    // never smuggles "42 — Iron Swor" into the sheet.
    input.addEventListener('input', function () {
      // Before refresh(), which reads it: the field is now the user's text, so it is a query.
      pristine = false;
      hidden.value = idOf(input.value);
      refresh();
      resolve();
    });
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
    //
    // Leaving the field is also when a HAND-TYPED id earns its display: the user typed '43',
    // moved on, and the field now reads "43 — Iron Shield". display() leaves unresolvable text
    // exactly as typed, so nothing a user wrote is ever rewritten under them.
    input.addEventListener('blur', function (event) {
      if (event && within(list, event.relatedTarget)) return;
      setOpen(false);
      display();
    });

    display();
    wrap.appendChild(input);
    wrap.appendChild(hidden);
    wrap.appendChild(label);
    wrap.appendChild(list);
    return wrap;
  }

  // Graphic control: the graphic and sheet cells side by side, a canvas preview, and a status
  // line. Blank or 0 in both means "no graphic".
  //
  // ONE OPTIONS OBJECT, not five positionals: `{ graphicColumn, fileColumn, values, ctx,
  // tintColumns }`. The two columns are the reason. The Graphic composite declares them
  // [graphic, file] (graphic_tile + graphic_file, spell_animation + spell_animation_file, …)
  // while Sprites.icon takes (bundles, SHEET, graphic) — so a positional call swapping the two
  // resolves nothing at all and does it silently. Named, the swap is not expressible; the tests
  // still assert the resolution order anyway.
  //
  // `opts.tintColumns` is Layout.tintColumns(sheet, graphicColumn.name) — the four cells whose
  // colour the game blends into this graphic, or null for a graphic it draws plain. They belong to
  // ANOTHER control (the Rgba composite's hidden inputs), so they cannot be read from this
  // control's own two fields; ctx.onFormChange is how the current values arrive after an edit.
  function graphicControl(opts) {
    var graphicColumn = opts.graphicColumn;
    var fileColumn = opts.fileColumn;
    var values = opts.values || {};
    var ctx = opts.ctx;
    var tintColumns = opts.tintColumns;

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

    // WHICH ATLAS THIS COLUMN'S NUMBER INDEXES. 'icons' for every graphic pair but Spell Effects'
    // spell_animation, which is an effect id — read from Layout by the caller rather than decided
    // here, so nothing in this file names a sheet.
    var galleryBundle = opts.galleryBundle || 'icons';

    // AN EFFECT IS NOT AN ICON, and the difference reaches further than the browse button. An
    // effect id names a whole animation in the effects atlas and Sprites.effectFrames takes it
    // ALONE — the file cell is not part of the lookup key. So for an effects column the sheet cell
    // is stored and edited but never resolved against, which is why the preview, the status line
    // and the save gate below all fork on this flag rather than only the gallery call.
    var isEffects = galleryBundle === 'effects';

    var gInput = cell(graphicColumn, isEffects ? 'effect' : 'graphic');
    var fInput = cell(fileColumn, isEffects ? 'file' : 'sheet');
    var status = Forms.el('span', { class: 'status' });

    // A pick writes BOTH cells of an icons pair, which is exactly what makes "graphic and sheet must
    // both be set" hard to trip from the browser. An EFFECT pick has no sheet to write, so the file
    // cell is left alone — spell_animation_file is stored 0 in 176 of the 259 shipped rows and the
    // server sends both cells through verbatim, so filling it in would be inventing data.
    //
    // ON THE CANVAS, not the graphic field: the field stays a plain text box a designer can click
    // into and retype, and the picture is what opens the picture browser (see browseOnClick).
    browseOnClick(canvas, 'browse the ' + galleryBundle + ' atlas for ' + graphicColumn.name,
      function (opener) {
        var gallery = galleryOf(opts);
        if (!gallery) return;
        gallery.open({
          bundle: galleryBundle,
          bundles: (ctx && ctx.bundles) || {},
          // For the blank-icon filter alone; the tiles draw from the CSS atlas rule either way.
          images: (ctx && ctx.images) || {},
          opener: opener,
          // Read at CLICK time, not at build time: a designer who has just typed a different sheet
          // number expects the browser to open on that sheet.
          filter: galleryBundle === 'icons' ? { sheet: fInput.value } : {},
          current: galleryBundle === 'icons'
            ? { sheet: fInput.value, graphic: gInput.value }
            : { id: gInput.value },
          onPick: function (choice) {
            gInput.value = choice.graphic !== undefined ? choice.graphic : choice.id;
            if (choice.sheet !== undefined) fInput.value = choice.sheet;
            // ONE dispatched event does both jobs: it runs this control's own `input` listener (so
            // the preview and the status line follow) and bubbles to app.js's delegated listener
            // (so the panel previews do too). Calling redraw() as well would draw twice.
            gInput.dispatchEvent(new Event('input', { bubbles: true }));
          },
        });
      });

    // The last form state seen, which the tint is read out of. Seeded with the record's own values
    // so the FIRST draw is tinted too — a preview that only picked the tint up after an unrelated
    // keystroke would show every freshly opened record plain.
    // The tint is the only cross-field read here, and it takes the RESOLVED record for
    // partControl's reason: a blank tint channel is the column default, not necessarily 0.
    var latest = opts.effective || values;

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
    //   - HALF A PAIR IS REPORTED, NOT REFUSED. No shipped row has one — all 1,595 item_templates
    //     rows set graphic_tile and graphic_file together or set neither — so blocking would refuse
    //     nothing that exists either. It stays a report because the server sends both cells to the
    //     client exactly as stored (SpellEffect.cs:520), so a half pair is a value the game has a
    //     defined reading of, and inventing a refusal for it is a rule nothing asked for.
    //   - A complete pair naming art the bundle does not have is the one the design rule speaks
    //     to — "a non-blank, non-zero graphic must resolve in the bundle" — and the one nothing
    //     else can see: both cells are optional INTEGERs, so Validation passes a nonexistent
    //     sheet:graphic as a perfectly good number and the only other signal is a blank canvas.
    //     All 649 Items, 152 Spells and 146 buff graphics in the shipped data resolve, so this
    //     refuses nothing that exists today.
    //
    // EFFECTS TAKE describeEffect INSTEAD — see there for why they get no `block` at all.
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

    // The same job for an effect id, and a SHORTER one: the file cell is not part of the lookup,
    // so there is no pair to be half of and nothing to say about a sheet.
    //
    // NO SAVE GATE, for partControl's reason rather than graphicControl's — the design rule that
    // justifies blocking an icon ("every shipped pair resolves") is simply not true here. 13 of the
    // 183 shipped spell_effects rows that set spell_animation name an id the committed effects
    // bundle does not have (267593, 267653, 267667, 267989, 268371, 282129, 285788 and 286986, the
    // last on 6 rows). Blocking would lock those 13 rows out of saving over a cell the designer may
    // not even be editing, and the bundle is a snapshot of client art rather than the authority on
    // what the server will accept. So this reports the miss and stops there.
    function describeEffect(rect, checkable) {
      var g = str(gInput.value).trim();
      if (g === '' || g === '0') return { text: 'no effect', bad: false };
      if (!isWhole(g)) return { text: 'effect must be a whole number', bad: true };
      if (!checkable) {
        return { text: 'cannot check effect ' + g + ' — no effect art loaded', bad: true };
      }
      if (!rect) return { text: 'no art for effect ' + g, bad: true };
      return { text: '', bad: false };
    }

    function redraw() {
      // The raw cells go straight to Sprites, which does its own parseInt-based coercion. A
      // Number() here as well would be a SECOND rule for what a cell means — and the two
      // disagree ('1e3' is 1000 to one and 1 to the other), so the preview could resolve a
      // different sprite from the one the lookup key names.
      var bundles = (ctx && ctx.bundles) || {};
      // FRAME 0, not an animation. The gallery's effect tiles show the same resting frame for the
      // same reason: this control is built per record and has no teardown hook to stop an interval
      // with, so an animated inline preview would leave a timer drawing onto a detached canvas on
      // every record change. The Spell Effects panel is where the animation lives (app.js keeps
      // state.stopEffect precisely so it can stop it), and this only has to confirm which effect
      // the cell names.
      //
      // Clipped at ICON_BOX like everything else here, which costs 2 of the 47 distinct shipped
      // effects their edges (one 96x96, one 128x128; the median is 48x48) — the same bundle fact
      // ICON_BOX already states, and the panel shows those two whole anyway.
      var rect = isEffects ? (Sprites.effectFrames(bundles, gInput.value)[0] || null)
                           : Sprites.icon(bundles, fInput.value, gInput.value);

      var atlas = isEffects ? 'effects' : 'icons';
      var checkable = !!(bundles[atlas] && bundles[atlas].rects);
      var state = isEffects ? describeEffect(rect, checkable) : describe(rect, checkable);
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
      Sprites.draw(target, (ctx && ctx.images) ? ctx.images[atlas] : null, rect,
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
    //
    // A TINTED ONE THEREFORE REDRAWS TWICE PER KEYSTROKE IN ITS OWN FIELD, and that is known, not
    // a mystery to be rediscovered: app.js's delegated listener fires on `input`, so typing in
    // graphic_tile runs redraw() from the handler above and again from this callback. Left alone
    // deliberately — dropping the direct handlers would make the preview depend on a subscription
    // this control cannot see the state of, and the cost is one extra draw of one small canvas.
    // partControl has the same shape for the same reason.
    if (tintColumns && ctx && typeof ctx.onFormChange === 'function') {
      ctx.onFormChange(function (current) { latest = current; redraw(); });
    }

    wrap.appendChild(canvas);
    wrap.appendChild(gInput);
    wrap.appendChild(fInput);
    wrap.appendChild(status);
    return wrap;
  }

  // The sprite folder one part-graphic cell draws from, as `{ category, mounted, drawn }`, for
  // either shape of Layout.partGraphic's spec. THE WHOLE DIFFERENCE between an appearance id and an
  // equip graphic lives in these eight lines, and everything below reads the answer rather than the
  // spec:
  //
  //   `spec.category` — a FIXED folder (body_id is always a body). There is no cell to read, so it
  //     is always drawn and never a mount: Appearance.layers pushes these three ids into the Body,
  //     Hair and Eyes slots unconditionally.
  //   `spec.categoryFrom` — the folder comes from another cell, through the client's own slot map in
  //     Appearance.slotFor. `drawn: false` is the honest answer for Ring, Misc and a blank — the
  //     client has no layer for them at all — and Mount is a body in a MOUNTED clip, which is a
  //     different lookup rather than the same one in another folder.
  //
  // Read from the LIVE form, never from build-time values, for the reason redraw() gives.
  function folderOf(spec, values) {
    if (spec.category) return { category: spec.category, mounted: false, drawn: true };
    var slot = Appearance.slotFor(values[spec.categoryFrom]);
    if (!slot) return { category: null, mounted: false, drawn: false };
    return { category: Appearance.CATEGORY[slot] || null, mounted: slot === 'Mount', drawn: true };
  }

  // Part control: one id field over a CHARACTER PART sprite rather than an inventory icon, plus a
  // preview and a status line. Every such column is a plain Int with no composite — which is why
  // they had no preview at all and why forms.js routes them here explicitly, off the same table
  // (Layout.PART_GRAPHICS) that decides which folder each one draws from.
  //
  // Takes one options object, `{ column, values, effective, ctx, spec, tintColumns }`, for
  // graphicControl's reason: several same-typed arguments in a row that no reader can order from
  // the call site.
  //
  // `values` seeds this control's own cell and must stay RAW — blank means "use the SQL default"
  // and has to write back blank. Every CROSS-FIELD read below (the folder, the tint, the pose)
  // takes `effective` instead, where a blank cell already reads as the default the importer will
  // apply, so the preview shows the row the database will hold rather than a zero nobody stored.
  // ctx.onFormChange delivers the same resolved shape on every later edit.
  //
  // WHAT MAKES IT DIFFERENT FROM graphicControl: there is no sheet cell — the sprite FOLDER comes
  // from the spec (see folderOf), which for graphic_equip means from another column entirely. That,
  // plus the tint and the armed/unarmed pose, is up to three cross-field reads, and all of them
  // arrive through ctx.onFormChange.
  //
  // NO SAVE GATE. graphicControl publishes __graphicError because the design rule for an inventory
  // icon is that a complete pair must resolve, and every shipped pair does. Nothing of the kind has
  // been established for graphic_equip, so refusing a save on it would be inventing a rule and
  // could lock rows that ship today. The status line reports a miss; that is all it does.
  function partControl(opts) {
    var column = opts.column;
    var values = opts.values || {};
    var ctx = opts.ctx;
    var spec = opts.spec;
    var tintColumns = opts.tintColumns;

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
    var latest = opts.effective || values;

    // The sprite folder this row can be BROWSED in, or null when there is nothing to browse. One
    // function for the whole question, because it is asked from two places (the click handler and
    // the enable/disable branch) and two spellings of it could drift.
    //
    // A Mount is null even though it HAS a folder: its art is a mounted clip, which the gallery does
    // not index because Sprites.part deliberately never falls back to one — so Bodies would list 305
    // standing bodies of which four have the mounted pose this cell actually needs.
    function browsableCategory() {
      var folder = folderOf(spec, latest);
      if (!folder.drawn || folder.mounted) return null;
      return folder.category;
    }

    // LOCKED TO THE ROW'S OWN CATEGORY, always. The folder comes from item_slot, so browsing Helms
    // for a Feet item would offer a sprite the client would never draw there — the gallery renders
    // no chooser at all in that case rather than a disabled one.
    //
    // With no browsable folder there is nothing to open: the click stays an ordinary click and the
    // status line says why the canvas is blank. See browsableCategory for which slots have none.
    //
    // ON THE CANVAS, for graphicControl's reason: the id field stays a plain text box.
    browseOnClick(canvas, 'browse the character parts atlas for ' + column.name,
      function (opener) {
        var gallery = galleryOf(opts);
        var folder = browsableCategory();
        if (!gallery || !folder) return;
        gallery.open({
          bundle: 'parts',
          bundles: (ctx && ctx.bundles) || {},
          opener: opener,
          filter: { category: folder, locked: true },
          current: { category: folder, id: input.value },
          onPick: function (choice) {
            input.value = choice.id;
            input.dispatchEvent(new Event('input', { bubbles: true }));
          },
        });
      });

    function redraw() {
      var target = Sprites.scaled(canvas, PART_SCALE, PART_W, PART_H);
      var text = str(input.value).trim();
      // The folder is read from the LIVE form, so changing item_slot from Helmet to Shoes moves this
      // preview into another folder without the record being reopened. For a fixed-category column
      // the answer never moves, which is why nothing here branches on which shape the spec is.
      var folder = folderOf(spec, latest);
      var category = folder.category;

      function say(message, bad) {
        status.textContent = message;
        status.className = bad ? 'status bad' : 'status';
      }

      // First, because it is a fact about the ROW and not about this cell: an id in a slot the
      // client never draws is not an error, and calling it one would flag most of the Ring and
      // Misc rows in the sheet. The canvas stays blank and the line says why it is blank.
      // Unreachable for a fixed-category column, which is always drawn.
      if (!folder.drawn) { say('this slot is not drawn on the character', false); return; }

      if (text === '' || text === '0') { say('no graphic', false); return; }
      // A non-whole cell already fails Validation's numeric check on the column itself, which
      // reports it under the field — same reasoning as graphicControl's `block`-narrower-than-`bad`
      // note, so this is shown and not gated.
      if (!isWhole(text)) { say('graphic must be a whole number', true); return; }

      var bundles = (ctx && ctx.bundles) || {};
      if (!bundles.parts || !bundles.parts.rects) {
        say('cannot check ' + category + ' graphic ' + text + ' — no character art loaded', true);
        return;
      }

      // A mount is a body in a MOUNTED clip, and Sprites.part deliberately never falls back to
      // one — so the two lookups are genuinely different functions, not one with a flag.
      var rect = folder.mounted ? Sprites.mount(bundles, text)
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
    browse: browse,
    fkControl: fkControl,
    graphicControl: graphicControl,
    partControl: partControl,
    // For composites.equipSlotsControl, whose slot previews open the browser the same way — one
    // definition of "a canvas that acts as a browse button" rather than a copy per module.
    browseOnClick: browseOnClick,
    LIMIT: LIMIT,
    ICON_BOX: ICON_BOX,
    PART_W: PART_W,
    PART_H: PART_H,
    PART_SCALE: PART_SCALE,
  };
})();

if (typeof module !== 'undefined') module.exports = { Pickers: Pickers };
