// Wires everything together: sheet selection, record list, form, previews, save, publish check.
// This is the only module that talks to the server (google.script.run) and the only one that
// decides whether a record may be written.
//
// THE SAVE PATH HAS THREE GATES, in this order, and each exists because the layer below it
// cannot see the problem:
//
//   1. FROZEN COMPOSITES. Composites.equipSlotsControl raises `wrapper.__frozen` the instant one
//      of its six graphic fields holds a typo, and stops writing the cell — including the edits
//      the user made to the OTHER five slots, which then live only in memory. Saving through
//      that silently discards them, so save() refuses while any node in the form is frozen.
//   2. AN UNFAITHFUL equipped_items. Five rows in the shipped data hold a string Equipped.parse
//      can only guess at. The control seeds the cell verbatim, so merely OPENING one is safe —
//      but editing a single slot rewrites all six from that guess, shifting slots and dropping
//      equipment. Equipped.isFaithful reports it exactly (264 well-formed rows true, 5 malformed
//      false), so a save that CHANGES an unfaithful cell is refused outright. Refused, not
//      warned: the damage is silent, invisible in the form afterwards, and the fix — repairing
//      the cell in the sheet itself — is available and takes seconds. A warning the user can
//      click through would be a warning they click through.
//   3. Validation.validateRecord, which is the ordinary per-column check.
//
// THE NAME COLUMN IS NOT ALWAYS B. Code.gs cannot reach GOOSE_SCHEMA, so readSheetIndex takes a
// 0-based nameColumnIndex from this side. Items has item_usetype in B and item_name at index 2
// (5 inbound refs), NPCs has npc_type in B and npc_name at 2. Hardcoding 1 labels all 649 Items
// entries "Armor"/"Weapon"/"NoUse". nameIndex() derives it from the schema rather than listing
// the exceptions, so a new sheet cannot be forgotten.
var App = (function () {
  // How many problems the publish check keeps. The panel shows 100; the rest are counted. The
  // cap is on the ARRAY, so a sheet with a systematically broken column cannot grow it without
  // bound (NPC Spawns alone is 4,322 rows).
  var MAX_PROBLEMS = 1000;
  var SHOWN_PROBLEMS = 100;

  var state = {
    schema: null,
    sheetName: null,
    rows: [],
    header: [],
    rowNumber: 0,
    ids: [],
    idSets: {},
    pickerData: {},
    bundles: {},
    images: {},
    imageCallbacks: [],
    loaded: {},        // the values map the open form was rendered from, verbatim
    stopEffect: null,
    previewKey: null,
    checking: false,
    // Bundles whose PNG failed to decode. Kept rather than only reported at the moment of
    // failure: loadBundle's done() runs straight into openSheet, whose own status line would
    // overwrite the message within the same tick and leave the user with a blank preview and no
    // explanation. Every later status line carries it until the bundle loads.
    bundleErrors: [],
  };

  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  function ctx() {
    return {
      bundles: state.bundles,
      images: state.images,
      pickerData: state.pickerData,
      // Queued, never invoked immediately: renderForm loads every bundle the sheet needs BEFORE
      // Forms.render runs, so a control's own build-time redraw already has its images. The
      // queue is what covers a bundle that arrives late (or not at all), and it is emptied when
      // the next form is rendered, so callbacks never accumulate across records.
      onImagesReady: function (fn) {
        if (typeof fn === 'function') state.imageCallbacks.push(fn);
      },
    };
  }

  // Every registered callback runs on EVERY bundle that lands, not just the first. The queue is
  // not cleared here: a control that needs `parts` would otherwise be dropped by `icons`
  // arriving first, and would never redraw when its own bundle finally decoded.
  function imagesChanged() {
    state.imageCallbacks.forEach(function (fn) { fn(); });
  }

  /// Decodes a bundle's data URI once. Bundles load lazily: icons up front, parts and effects
  /// only when a sheet needs them. done() is always called, exactly once, including on failure —
  /// a missing bundle must leave the form renderable without art, not unrendered.
  function loadBundle(name, done) {
    if (state.images[name]) { done(); return; }
    if (typeof GOOSE_SPRITES === 'undefined' || !GOOSE_SPRITES[name]) { done(); return; }

    state.bundles[name] = GOOSE_SPRITES[name];

    var img = new Image();
    img.onload = function () {
      state.images[name] = img;
      imagesChanged();
      done();
    };
    img.onerror = function () {
      if (state.bundleErrors.indexOf(name) === -1) state.bundleErrors.push(name);
      status(bundleWarning(), true);
      done();
    };
    img.src = GOOSE_SPRITES[name].png;
  }

  function bundleWarning() {
    if (!state.bundleErrors.length) return '';
    return 'Failed to decode the ' + state.bundleErrors.join(' and ') +
           ' sprite bundle — previews will be blank';
  }

  // Sequential, so `done` runs once after the last one. Duplicates in `names` are free —
  // loadBundle returns immediately for a bundle that is already decoded.
  function loadBundles(names, done) {
    var i = 0;
    function next() {
      if (i >= names.length) { done(); return; }
      var name = names[i];
      i += 1;
      loadBundle(name, next);
    }
    next();
  }

  function schemaFor(sheetName) {
    return GOOSE_SCHEMA.sheets.filter(function (s) { return s.sheet === sheetName; })[0];
  }

  // The 0-based index of the column a human recognises the row by: the first Text column.
  // Column A is always the id, so index 0 is never the answer; 1 is the fallback, which is what
  // Code.gs defaults to. Checked against all eight FK targets: Items 2, NPCs 2, and 1 for
  // Classes, Spells, Spell Effects, Maps, Quests and Combinations.
  function nameIndex(schema) {
    if (!schema) return 1;
    for (var i = 1; i < schema.columns.length; i++) {
      if (schema.columns[i].kind === 'Text') return i;
    }
    return 1;
  }

  function status(message, isError) {
    var el = document.getElementById('status');
    if (!el) return;
    el.textContent = message;
    el.className = isError ? 'error' : '';
  }

  // Stops any running effect animation and empties the preview panel. Both halves matter: the
  // interval outlives the canvas, and the canvas outlives the record.
  function clearPreviews() {
    if (state.stopEffect) { state.stopEffect(); state.stopEffect = null; }
    var host = document.getElementById('previews');
    if (host) host.innerHTML = '';
    state.previewKey = null;
  }

  // Depth-first over real elements. Not querySelectorAll: __frozen is a JS property, not an
  // attribute, so there is no selector that finds it.
  function walk(node) {
    var out = [];
    var kids = node.children || [];
    for (var i = 0; i < kids.length; i++) {
      out.push(kids[i]);
      out = out.concat(walk(kids[i]));
    }
    return out;
  }

  /// Loads a sheet and rebuilds the record list. `selectRow` is a spreadsheet row number to
  /// re-open once the list is up — used after a save, so the record the user was editing is
  /// still in front of them instead of the form vanishing.
  function openSheet(sheetName, selectRow, note) {
    status('Loading ' + sheetName + '…');
    // A sheet switch usually ends without a form — openSheet stops at renderList unless
    // selectRow is given — so renderPreviews, which is the other place this happens, is never
    // reached. Leaving it to renderPreviews alone would run the previous sheet's animation
    // forever, with its canvas still on screen above the new sheet's empty form.
    clearPreviews();
    state.sheetName = sheetName;
    state.schema = schemaFor(sheetName);
    state.rowNumber = 0;

    if (!state.schema) { status('No schema for sheet ' + sheetName, true); return; }

    google.script.run
      .withFailureHandler(function (e) { status(e.message, true); })
      .withSuccessHandler(function (data) {
        state.header = data.header;
        state.rows = data.rows;
        collectIds();
        loadReferencedSheets(function () {
          renderList();
          // `note` keeps the save confirmation on screen through the reload that follows it —
          // otherwise the record count overwrites it and the save looks like it did nothing.
          var warning = bundleWarning();
          status((note || (state.rows.length + ' records')) +
                 (warning ? ' — ' + warning : ''), !!warning);
          if (selectRow) {
            var index = num(selectRow) - 2;
            if (index >= 0 && index < state.rows.length) editRow(index);
          }
        });
      })
      .readSheet(sheetName);
  }

  /// Own-sheet id set, for duplicate detection. The schema's column order IS the sheet's column
  /// order — the importer reads cells by index (CsvToSqlBase.cs:35) — so indexing the row by the
  /// pk's position in schema.columns is the same cell readSheet returned.
  function collectIds() {
    var pk = state.schema.columns.filter(function (c) { return c.pk; })[0];
    var ids = [];

    if (pk) {
      var index = state.schema.columns.indexOf(pk);
      state.rows.forEach(function (r) {
        if (str(r[index]).trim() !== '') ids.push(Number(r[index]));
      });
    }

    state.idSets.__self = new Set(ids);
    state.ids = ids;
  }

  /// FK targets and Bitmask sources need their id + name lists. Composites.Bitmask reads its
  /// source at BUILD time (unlike IdList, which is lazy), so this must finish before the form is
  /// rendered or class_restrictions degrades to a raw-mask text box.
  function loadReferencedSheets(done) {
    var needed = Object.create(null);
    state.schema.columns.forEach(function (c) { if (c.ref) needed[c.ref] = true; });
    (state.schema.composites || []).forEach(function (comp) {
      if (comp.source) needed[comp.source] = true;
    });

    var names = Object.keys(needed).filter(function (n) { return !state.pickerData[n]; });
    if (!names.length) { done(); return; }

    var remaining = names.length;
    names.forEach(function (name) {
      google.script.run
        .withFailureHandler(function () { remaining -= 1; if (!remaining) done(); })
        .withSuccessHandler(function (data) {
          state.pickerData[name] = data.entries;
          state.idSets[name] = new Set(data.entries.map(function (e) { return Number(e.id); }));
          remaining -= 1;
          if (!remaining) done();
        })
        .readSheetIndex(name, nameIndex(schemaFor(name)));
    });
  }

  function rowToValues(row) {
    var values = {};
    state.schema.columns.forEach(function (c, i) {
      values[c.name] = row && row[i] !== undefined ? row[i] : '';
    });
    return values;
  }

  function renderList() {
    var list = document.getElementById('records');
    list.innerHTML = '';

    var label = nameIndex(state.schema);
    state.rows.forEach(function (row, index) {
      var text = str(row[0]) === '' ? '?' : str(row[0]);
      if (label > 0) text += ' — ' + str(row[label]);
      var button = Forms.el('button', { type: 'button', class: 'record' }, text);
      button.addEventListener('click', function () { editRow(index); });
      list.appendChild(button);
    });
  }

  // rows[i] is spreadsheet row i + 2: readSheet returns values.slice(1), so index 0 is row 2.
  // Code.gs refuses row 1 (the header) and anything past lastRow + 1.
  function editRow(index) {
    state.rowNumber = index + 2;
    renderForm(rowToValues(state.rows[index]));
  }

  function newRecord() {
    if (!state.schema) return;
    state.rowNumber = 0;
    var values = rowToValues(null);

    var pk = state.schema.columns.filter(function (c) { return c.pk; })[0];
    if (pk) values[pk.name] = String(Validation.nextId(state.ids));

    renderForm(values);
  }

  // Which bundles this sheet's controls will ask for. Every sheet gets icons (the Graphic
  // composite and its previews); parts only where a character is drawn; effects only where an
  // animation is.
  function bundlesFor(schema) {
    var names = ['icons'];
    var hasBody = schema.columns.some(function (c) { return c.name === 'body_id'; });
    var hasEquip = (schema.composites || []).some(function (c) { return c.kind === 'EquipSlots'; });
    if (hasBody || hasEquip) names.push('parts');
    if (['Spells', 'Spell Effects'].indexOf(schema.sheet) !== -1) names.push('effects');
    return names;
  }

  function renderForm(values) {
    state.loaded = values;
    // Dropped BEFORE the new controls register theirs, so a callback holding a canvas from the
    // previous record cannot redraw into a detached node.
    state.imageCallbacks = [];

    loadBundles(bundlesFor(state.schema), function () {
      var container = document.getElementById('form');
      Forms.render(container, state.schema, values, ctx());
      renderPreviews(container, values);
    });
  }

  // The values the previews actually READ, as one string. Two mechanisms ask for a redraw and
  // both can fire for a single keystroke — a composite's __onChange at the target, then the
  // delegated listener as the same event bubbles to the form — so the redraw is deduplicated by
  // CONTENT rather than by trying to reason about listener order. That also means typing in
  // item_name or npc_title, which no preview reads, rebuilds nothing.
  //
  // It is what makes the frozen case come out right for free: while an equip slot holds a typo
  // the composite does not write its cell, so the key does not move and the preview does not
  // either — the same guarantee notify() gives, without a second frozen test that could drift
  // out of step with the first.
  function previewKey(values) {
    var parts = ['body_id', 'body_r', 'body_g', 'body_b', 'body_a', 'hair_id', 'hair_r',
                 'hair_g', 'hair_b', 'hair_a', 'face_id', 'body_state', 'equipped_items',
                 'spell_effect_id', 'spell_animation'].map(function (name) {
      return name + '=' + str(values[name]);
    });
    return state.sheetName + '\u0000' + parts.join('\u0000');
  }

  // Re-renders the previews from the CURRENT form contents. Reading the form back rather than
  // the stored row is the point: the preview has to show the edit, not the record.
  function refreshPreviews(container) {
    var values = Forms.collect(container, state.schema);
    if (previewKey(values) === state.previewKey) return;
    renderPreviews(container, values);
  }

  function renderPreviews(container, values) {
    var host = document.getElementById('previews');
    host.innerHTML = '';
    state.previewKey = previewKey(values);

    // Unconditional, not inside the effect branch: switching to a record with no effect would
    // otherwise leave the previous animation's interval running forever, drawing into a canvas
    // that is no longer in the tree.
    if (state.stopEffect) { state.stopEffect(); state.stopEffect = null; }


    // Every composite control notifies through __onChange on the node Composites.control
    // returned, and Task 8's contract names that as the preview's redraw signal. Set on EVERY
    // node rather than on the ones that LOOK like composites: only a composite ever calls it,
    // the property is inert everywhere else, and sniffing for a class name would silently stop
    // working the day a control is renamed.
    //
    // It is currently REDUNDANT, and honestly so: every composite writes its cell from an
    // `input` or `change` handler, and those bubble to the delegated listener in init(), so a
    // mutation sweep cannot kill this line. It stays for the path that does not bubble —
    // idListControl's chip buttons write the cell from a `click` — which today touches only
    // quest_ids, a column no preview reads. The day a composite changes a previewed cell from a
    // button, this is the only thing that would notice.
    //
    // Both routes end in refreshPreviews, which is keyed on content, so one keystroke arriving
    // twice still redraws once. A rejected equip-slot typo redraws NEITHER way: the composite
    // skips notify while frozen, and the cell it did not write leaves the key unmoved.
    var redraw = function () { refreshPreviews(container); };
    walk(container).forEach(function (node) { node.__onChange = redraw; });

    if (state.schema.columns.some(function (c) { return c.name === 'body_id'; })) {
      var canvas = Forms.el('canvas',
        { width: Preview.CANVAS_W, height: Preview.CANVAS_H, class: 'appearance' });
      host.appendChild(canvas);

      Preview.character(canvas, {
        bodyId: values.body_id,
        bodyR: values.body_r, bodyG: values.body_g, bodyB: values.body_b, bodyA: values.body_a,
        hairId: values.hair_id,
        hairR: values.hair_r, hairG: values.hair_g, hairB: values.hair_b, hairA: values.hair_a,
        faceId: values.face_id,
        // Absent on Spell Effects, whose appearance override has no body_state; a missing cell
        // reads as armed, matching the NPCs default of 1.
        bodyState: values.body_state,
        equippedItems: values.equipped_items || '',
      }, ctx());
    }

    var effectColumn = state.sheetName === 'Spells' ? 'spell_effect_id'
      : (state.sheetName === 'Spell Effects' ? 'spell_animation' : null);

    if (effectColumn) {
      var effectId = num(values[effectColumn]);
      if (effectId > 0) {
        var anim = Forms.el('canvas', { width: 96, height: 96, class: 'effect' });
        host.appendChild(anim);
        state.stopEffect = Preview.effect(anim, effectId, ctx());
      }
    }
  }

  // Gate 1: any frozen composite. Composites.equipSlotsControl holds the cell while a graphic
  // field has a typo, so a save then writes the OLD string and silently drops every edit made
  // to the other five slots.
  function frozen(container) {
    return walk(container).some(function (n) { return n.__frozen === true; });
  }

  // Gate 2: an unfaithful stored equipped_items that the user has now changed. Unchanged is
  // fine — the control seeds the cell verbatim, so opening a malformed row and saving it writes
  // back exactly what was there.
  function unfaithfulEdit(values) {
    var comp = (state.schema.composites || []).filter(function (c) {
      return c.kind === 'EquipSlots';
    })[0];
    if (!comp) return null;

    var column = comp.columns[0];
    var stored = str(state.loaded[column]);
    if (Equipped.isFaithful(stored)) return null;
    if (str(values[column]) === stored) return null;

    return column + ' in this row is malformed and cannot be edited safely — the editor would ' +
           'rewrite all six slots from its best guess. Fix the cell in the spreadsheet first. ' +
           'Stored value: ' + stored;
  }

  function save() {
    if (!state.schema) return;
    var container = document.getElementById('form');

    if (frozen(container)) {
      status('An equipment slot holds an invalid graphic id — fix it before saving. Edits to ' +
             'the other slots are not being recorded until you do.', true);
      return;
    }

    var values = Forms.collect(container, state.schema);

    var unfaithful = unfaithfulEdit(values);
    if (unfaithful) { status(unfaithful, true); return; }

    // The row's own id is the one it was LOADED with, not the one now in the field. Reading the
    // field instead would exempt whatever the user just typed from the duplicate check — type an
    // id another record already has and it would validate as "its own".
    var pk = state.schema.columns.filter(function (c) { return c.pk; })[0];
    var ownId = state.rowNumber > 0 && pk ? Number(state.loaded[pk.name]) : null;

    var result = Validation.validateRecord(state.schema.columns, values, state.idSets, ownId);
    Forms.showErrors(container, result.errors);

    if (!result.ok) {
      status(result.errors.length + ' problem(s) — fix them before saving', true);
      return;
    }

    // Blank stays blank: a cell is only written when the user supplied a value.
    var cells = state.schema.columns.map(function (c) {
      var check = Validation.validateCell(c, values[c.name], state.idSets);
      return check.write ? values[c.name] : null;
    });

    // -1, not 0, for the nine sheets with no pk: their column A is an Id-kind FK that
    // legitimately repeats, and 0 would make Code.gs reject every second drop, spawn or level.
    var idIndex = pk ? state.schema.columns.indexOf(pk) : -1;

    status('Saving…');
    google.script.run
      .withFailureHandler(function (e) { status(e.message, true); })
      .withSuccessHandler(function (written) {
        openSheet(state.sheetName, written && written.row,
                  'Saved. Run /updatesql then /reloadsql in game to publish.');
      })
      .writeRow(state.sheetName, state.rowNumber, cells, idIndex);
  }

  /// Publish check: validate every record on every sheet before telling the user to publish.
  ///
  /// Two phases, because a per-sheet check cannot see the sheets it points at. Phase one reads
  /// all 21 sheets and builds one id set per sheet; phase two validates every row against that
  /// whole map. Checking against whatever sheet happens to be open would validate NPC Drops'
  /// item ids against, say, the Maps id set and report every row as broken.
  function publishCheck() {
    var panel = document.getElementById('publish-results');
    if (state.checking) return;
    state.checking = true;

    var sheets = GOOSE_SCHEMA.sheets.slice();
    panel.innerHTML = '';
    panel.appendChild(Forms.el('p', null, 'Checking all ' + sheets.length + ' sheets…'));

    var loaded = [];
    var problems = [];
    var dropped = 0;

    function report(sheet, row, message) {
      if (problems.length >= MAX_PROBLEMS) { dropped += 1; return; }
      problems.push({ sheet: sheet, row: row, message: message });
    }

    var index = 0;
    function readNext() {
      if (index >= sheets.length) { validateAll(); return; }
      var schema = sheets[index];
      index += 1;

      google.script.run
        .withFailureHandler(function (e) {
          report(schema.sheet, '-', e.message);
          readNext();
        })
        .withSuccessHandler(function (data) {
          loaded.push({ schema: schema, rows: data.rows });
          readNext();
        })
        .readSheet(schema.sheet);
    }

    function validateAll() {
      var idSets = {};
      loaded.forEach(function (entry) {
        var pk = entry.schema.columns.filter(function (c) { return c.pk; })[0];
        if (!pk) return;
        var at = entry.schema.columns.indexOf(pk);
        var ids = [];
        entry.rows.forEach(function (row) {
          if (str(row[at]).trim() !== '') ids.push(Number(row[at]));
        });
        idSets[entry.schema.sheet] = new Set(ids);
      });

      loaded.forEach(function (entry) {
        var schema = entry.schema;
        var pk = schema.columns.filter(function (c) { return c.pk; })[0];
        var at = pk ? schema.columns.indexOf(pk) : -1;
        // Duplicates are counted here rather than left to validateId: validateRecord is given
        // each row's OWN id as ownId, so it never mistakes a row for a clash with itself.
        var seen = Object.create(null);

        // Every sheet's id set BY NAME, so an FK is checked against the sheet it actually points
        // at, plus this sheet's own set as __self. A plain object, keyed by sheet name — no
        // sheet is called __self, and validateCell only ever reads column.ref out of it.
        //
        // __self being the RIGHT sheet is currently unobservable — every row is validated with
        // its own id as ownId, and validateId only complains when the set holds an id that is
        // not ownId — so a mutation sweep cannot kill it. It is correct rather than merely
        // harmless, and duplicates are found by the `seen` map below, which does not depend on
        // it. Left in because validateRecord's contract says __self is the sheet's own ids.
        var sets = { __self: idSets[schema.sheet] || new Set() };
        Object.keys(idSets).forEach(function (n) { sets[n] = idSets[n]; });

        entry.rows.forEach(function (row, i) {
          var values = {};
          schema.columns.forEach(function (c, ci) {
            values[c.name] = row[ci] === undefined ? '' : row[ci];
          });

          var ownId = null;
          if (at >= 0 && str(row[at]).trim() !== '') {
            ownId = Number(row[at]);
            var key = String(ownId);
            if (seen[key]) report(schema.sheet, i + 2, 'id ' + str(row[at]) +
                                  ' is already used by row ' + seen[key]);
            else seen[key] = i + 2;
          }

          var r = Validation.validateRecord(schema.columns, values, sets, ownId);
          r.errors.forEach(function (e) {
            report(schema.sheet, i + 2, e.message);
          });
        });
      });

      show();
    }

    function show() {
      state.checking = false;
      panel.innerHTML = '';

      if (!problems.length) {
        panel.appendChild(Forms.el('p', { class: 'ok' },
          'All sheets valid. Publish with /updatesql then /reloadsql in game.'));
      } else {
        panel.appendChild(Forms.el('p', { class: 'error' },
          problems.length + (dropped ? '+' : '') + ' problem(s) — do not publish yet:'));
        problems.slice(0, SHOWN_PROBLEMS).forEach(function (p) {
          panel.appendChild(Forms.el('div', { class: 'problem' },
            p.sheet + ' row ' + p.row + ': ' + p.message));
        });
        var hidden = problems.length - SHOWN_PROBLEMS + dropped;
        if (hidden > 0) {
          panel.appendChild(Forms.el('p', null, 'and ' + hidden + ' more not shown'));
        }
      }

      // All twelve, from Layout.RESTART_ONLY rather than a list repeated here — /reloadsql
      // reloads five loaders only, and LoadTitles/LoadSurnames are never called by it at all.
      panel.appendChild(Forms.el('p', { class: 'warn' },
        'These ' + Layout.RESTART_ONLY.length + ' sheets need a full server restart rather ' +
        'than /reloadsql: ' + Layout.RESTART_ONLY.join(', ')));
    }

    readNext();
  }

  function init() {
    var picker = document.getElementById('sheet-picker');
    GOOSE_SCHEMA.sheets.forEach(function (s) {
      picker.appendChild(Forms.el('option', { value: s.sheet }, s.sheet));
    });

    picker.addEventListener('change', function () { openSheet(picker.value); });
    document.getElementById('new-record').addEventListener('click', newRecord);
    document.getElementById('save').addEventListener('click', save);
    document.getElementById('publish-check').addEventListener('click', publishCheck);

    // Delegated preview refresh, registered ONCE on the form container — which outlives every
    // record, so registering it per render would stack a handler per record opened.
    //
    // Delegated because __onChange only ever reaches COMPOSITES: body_id, hair_id, face_id and
    // body_state belong to no composite on NPCs, so forms.js renders them as plain text inputs
    // with no listener of their own, and without this, typing 150 into body_id changes nothing
    // on screen until the record is saved and re-opened. `input` and `change` both bubble;
    // `input` covers typing, `change` covers a <select> and a value committed without one.
    var form = document.getElementById('form');
    function onEdit() { if (state.schema) refreshPreviews(form); }
    form.addEventListener('input', onEdit);
    form.addEventListener('change', onEdit);

    loadBundle('icons', function () { openSheet(GOOSE_SCHEMA.sheets[0].sheet); });
  }

  // The internals are exported for the tests, which drive this module the way the page does —
  // through init() and the buttons — and then need to assert on what it asked the server for.
  return {
    init: init,
    save: save,
    openSheet: openSheet,
    newRecord: newRecord,
    editRow: editRow,
    publishCheck: publishCheck,
    nameIndex: nameIndex,
    bundlesFor: bundlesFor,
    __state: state,
  };
})();

if (typeof module !== 'undefined') module.exports = { App: App };
