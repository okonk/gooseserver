// Wires everything together: sheet selection, record list, form, previews, save, publish check.
// This is the only module that talks to the server (google.script.run) and the only one that
// decides whether a record may be written.
//
// THE SAVE PATH HAS FOUR GATES, in this order, and each exists because the layer below it
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
//   3. AN FK WITH NO LIST TO CHECK IT AGAINST. Validation.validateCell passes an fk whose id set
//      is absent, because the sets load asynchronously and failing closed would block saves on
//      rows the user never touched — so a readSheetIndex failure would otherwise let a
//      nonexistent id through the only check there is for it. unverifiedRefs gates exactly the
//      columns this record USES (blank and '0' mean "none" and are exempt either way), reports
//      which sheet is missing, and re-requests it.
//   4. A GRAPHIC THAT DOES NOT RESOLVE. Pickers.graphicControl raises `wrapper.__graphicError`
//      when a COMPLETE graphic/sheet pair names art the loaded bundle does not have — the design
//      rule is that a non-blank, non-zero graphic must resolve in the bundle, and nothing below
//      can see the breach: both cells are optional INTEGERs, so validateCell passes a nonexistent
//      sheet:graphic as a perfectly good number and the only other signal is a blank canvas.
//      The control's other complaints are shown but NOT gated on, and pickers.js says why.
//   5. Validation.validateRecord, which is the ordinary per-column check.
//
// AND ONE THING THAT IS NOT A GATE: clearForMonsterBody, between gates 2 and 3. It does not refuse
// the save, it changes what the save writes — the face id, the hair id and the equipment of a body
// that has just crossed into the >= 100 range the client renders alone. It is the only place in the
// editor that writes a cell the user did not touch, which is why it fires only on the crossing and
// says on the status line what it did.
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

  // EVERY SERVER CALL IS ASYNCHRONOUS AND EVERY CALLBACK WRITES INTO ONE SHARED `state`, so a
  // callback that arrives after the user has moved on must not be allowed to write anything. Two
  // generation counters are the guard, and the rule is the same for both: capture the counter
  // when the request goes out, and return without touching `state` if it has moved on by the
  // time the answer comes back.
  //
  //   sheetToken — bumped by openSheet. A stale readSheet reply would otherwise leave sheet A's
  //     rows under sheet B's schema, and the next Save writes A's record into B. Nothing
  //     downstream can catch it: the row is well formed, the width matches and the id is free.
  //     A counter rather than a `state.sheetName !== sheetName` comparison because A -> B -> A
  //     must also discard B's reply, and by name that reply looks current.
  //   formToken — bumped by renderForm. Same failure one level down: two records opened while a
  //     bundle is decoding, and the LAST decode to land renders its record's values into the
  //     row slot of the other one.
  var state = {
    schema: null,
    sheetName: null,
    sheetToken: 0,
    formToken: 0,
    saving: false,
    // True from renderForm until its form actually lands in the DOM. The bundle decode between
    // the two is asynchronous, so in that window state.rowNumber already names the NEW record
    // while the container still shows the OLD one — and a Save would collect the old fields
    // under the new row number. save() refuses while this is up.
    formPending: false,
    loading: {},       // bundle name -> callbacks waiting on one in-flight decode
    rows: [],
    rowNumber: 0,
    ids: [],
    idSets: {},
    pickerData: {},
    bundles: {},
    images: {},
    imageCallbacks: [],
    formCallbacks: [],
    loaded: {},        // the values map the open form was rendered from, verbatim
    stopEffect: null,
    previewKey: null,
    checking: false,
    // Bundles whose PNG failed to decode. Kept rather than only reported at the moment of
    // failure: loadBundle's done() runs straight into openSheet, whose own status line would
    // overwrite the message within the same tick and leave the user with a blank preview and no
    // explanation. Every later status line carries it until the bundle loads.
    bundleErrors: [],
    // Referenced sheets whose id + name list failed to load, same reasoning as bundleErrors and
    // one more besides: this one is a HOLE IN VALIDATION, not just a blank preview.
    // Validation.validateCell waves an fk through when its id set is absent — it must, because
    // the sets load asynchronously and failing closed would block saves on rows the user has not
    // touched — so a transient failure here would let a nonexistent id be saved. unverifiedRefs()
    // is what closes that, and the array is what tells it which sheets to distrust.
    //
    // MUTATED IN PLACE, never reassigned: ctx() hands this array to every picker so a control
    // built before the failure still sees it.
    refErrors: [],
    retrying: false,
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
      // So an fk label can say "could not load Items" rather than "loading Items…" forever.
      refErrors: state.refErrors,
      // Queued, never invoked immediately: a control does its own first draw at build time, from
      // whatever art has decoded by then. That is ICONS AND ONLY ICONS — renderForm renders the
      // form as soon as the icons bundle is in hand and asks for the rest afterwards — so for a
      // control over a character part this queue is the NORMAL path to its first sprite, not just
      // the fallback for a slow bundle. Emptied when the next form is rendered and by clearForm,
      // so callbacks never accumulate across records or outlive the nodes they draw into.
      onImagesReady: function (fn) {
        if (typeof fn === 'function') state.imageCallbacks.push(fn);
      },
      // CROSS-FIELD CHANGE NOTIFICATION. A control redraws from its own inputs, so it cannot see
      // a cell that belongs to another control — Pickers.graphicControl needs graphic_r/g/b/a
      // (the Rgba composite's hidden inputs) and Pickers.partControl needs item_slot and
      // body_state. `fn(values)` is called with the whole collected form on every edit — as an
      // EFFECTIVE record (Forms.effective): every blank cell already reads as the SQL default the
      // importer will apply, which is what a preview has to draw. Controls seed their own cell from
      // the raw values they were built with and never from this.
      //
      // ONLY ON SUBSEQUENT EDITS, never immediately: every control is handed `values` at build
      // time and does its own first draw from them, so invoking here would draw twice. Cleared in
      // renderForm exactly as imageCallbacks is, so a callback holding the previous record's
      // canvas can never redraw into a detached node.
      //
      // It reuses the ONE collect() the delegated listener already does for refreshPreviews —
      // one traversal of a 76-column form per keystroke, two consumers.
      onFormChange: function (fn) {
        if (typeof fn === 'function') state.formCallbacks.push(fn);
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
    // A decode that FAILED is a result too. Retrying it on every record open re-attempted a
    // multi-megabyte decode each time — and, worse, re-opened the async gap between editRow
    // moving state.rowNumber and renderForm landing the new form, the one window where what the
    // user sees and where a save writes can disagree. Recovering from a truly transient decode
    // failure takes a page reload, which is what the bundleErrors warning amounts to anyway.
    if (state.bundleErrors.indexOf(name) !== -1) { done(); return; }

    state.bundles[name] = GOOSE_SPRITES[name];

    // One decode per bundle, ever. The state.images check above only closes the window AFTER the
    // first decode lands; two records opened before that both pass it, and two Images for a
    // multi-megabyte data URI then race to render the same slot. Waiters queue instead.
    if (state.loading[name]) { state.loading[name].push(done); return; }
    state.loading[name] = [done];

    function finish() {
      var waiting = state.loading[name] || [];
      delete state.loading[name];
      waiting.forEach(function (fn) { fn(); });
    }

    var img = new Image();
    img.onload = function () {
      state.images[name] = img;
      imagesChanged();
      finish();
    };
    img.onerror = function () {
      if (state.bundleErrors.indexOf(name) === -1) state.bundleErrors.push(name);
      status(warnings(), true);
      finish();
    };
    img.src = GOOSE_SPRITES[name].png;
  }

  function bundleWarning() {
    if (!state.bundleErrors.length) return '';
    return 'Failed to decode the ' + state.bundleErrors.join(' and ') +
           ' sprite bundle — previews will be blank';
  }

  // Says which sheet, and says what it COSTS: an unloaded list is not a cosmetic loss, it is the
  // id check for every fk pointing at that sheet.
  function refWarning() {
    if (!state.refErrors.length) return '';
    return 'Could not load ' + state.refErrors.join(' or ') + ' — ids pointing there cannot be ' +
           'checked, so records using them will not save until the list loads';
  }

  // Both warnings, for the one status line. Joined rather than one overwriting the other: they
  // have independent causes and a user with both problems needs to know about both.
  function warnings() {
    return [bundleWarning(), refWarning()].filter(function (w) { return w; }).join(' — ');
  }

  // Re-requests only the lists that failed. loadReferencedSheets already skips names it has, so
  // this asks for exactly the missing ones and nothing else.
  //
  // It does NOT re-render the open form, which would throw away whatever the user has typed. The
  // fk pickers read pickerData at use time so they heal on their own; a Bitmask source reads its
  // list at BUILD time, so a bitmask whose source arrives this late stays a raw number box until
  // the record is reopened — which the message says.
  // `wanted` is only for the message — loadReferencedSheets decides what to request, and it
  // requests every list this sheet is missing, which is a superset of the ones being reported.
  function retryReferencedSheets(wanted) {
    if (state.retrying || !state.schema) return;
    state.retrying = true;

    loadReferencedSheets(function () {
      state.retrying = false;
      if (state.refErrors.length) { status(warnings(), true); return; }
      status('Reloaded ' + wanted.join(' and ') + '. Save again — and reopen the record if a ' +
             'checkbox list is still showing as a number.');
    });
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

  // The 0-based index of the ONE extra cell a sheet's picker entries carry, or -1. Only Spell
  // Effects has one today: the effects atlas is keyed by ANIMATION id (spell_animation), not by
  // effect row id, so the Spells preview must resolve spell_effect_id through the row it names —
  // and the picker list is the only per-row data the client holds for a referenced sheet.
  function extraIndex(sheetName) {
    if (sheetName !== 'Spell Effects') return -1;
    var schema = schemaFor(sheetName);
    if (!schema) return -1;
    for (var i = 0; i < schema.columns.length; i++) {
      if (schema.columns[i].name === 'spell_animation') return i;
    }
    return -1;
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

  // No missing-element guard, matching renderList/renderForm/renderPreviews/save/publishCheck:
  // every one of the nine ids is in Editor.html, and a page missing one is broken in a way that
  // should fail loudly on the first call rather than run on silently with no status line.
  function status(message, isError) {
    var el = document.getElementById('status');
    el.textContent = message;
    el.className = isError ? 'error' : '';
  }

  // Stops any running effect animation and empties the preview panel. Both halves matter: the
  // interval outlives the canvas, and the canvas outlives the record.
  function clearPreviews() {
    if (state.stopEffect) { state.stopEffect(); state.stopEffect = null; }
    document.getElementById('previews').innerHTML = '';
    state.previewKey = null;
  }

  // Depth-first over real elements. Not querySelectorAll: __frozen and __graphicError are JS
  // properties, not attributes, so there is no selector that finds them.
  function walk(node) {
    // An explicit stack, pushing into ONE array: `out = out.concat(walk(child))` reallocated the
    // whole list at every node, which is quadratic on a 76-column form.
    var out = [];
    var stack = [node];
    while (stack.length) {
      var kids = stack.pop().children || [];
      for (var i = 0; i < kids.length; i++) {
        out.push(kids[i]);
        stack.push(kids[i]);
      }
    }
    return out;
  }

  // Empties the form and forgets the record it held. Both halves, always together: leaving the
  // previous sheet's controls in the DOM lets Forms.collect harvest whatever COLUMN NAMES the
  // two sheets share, and Save then appends that as a new record. Combination Item Required and
  // Combination Item Result share both of their columns, as do Class Levelup Spells and Class
  // Info, so the harvested row validates clean and appends — and with no pk, Code.gs's duplicate
  // scan is disabled by design and cannot see it either.
  function clearForm() {
    document.getElementById('form').innerHTML = '';
    state.loaded = {};
    state.rowNumber = 0;
    // The two callback registries go with the nodes they close over. renderForm also clears them,
    // which covers opening one record after another — but clearForm is the path that empties the
    // form WITHOUT rendering a new one (a sheet switch, a failed load), and a bundle decoding after
    // one of those would otherwise hand imagesChanged() a callback whose canvas is already
    // detached. That is what makes onFormChange's "can never redraw into a detached node" true of
    // the code and not only of the happy path.
    state.imageCallbacks = [];
    state.formCallbacks = [];
    // An emptied form is not a pending one: whatever render was in flight is now stale (the
    // token bump below sees to its callback), and Save against the empty container is refused
    // by its own guard rather than this one.
    state.formPending = false;
    // AND THE FORM TOKEN, for the callbacks that are in neither registry. renderForm's bundle
    // continuations are closures in loadBundle's waiter list, so emptying the registries cannot
    // reach them; only a token they no longer match can. Without this a parts decode landing after
    // a sheet switch collected {} off the emptied container, read the NEW sheet's schema, and
    // appended a blank character canvas under an empty form. It is also what makes renderForm's
    // second token check reachable, and therefore testable, at all.
    state.formToken++;
  }

  // Everything derived from the OPEN sheet's rows. The token scheme guards what a reply may
  // write; this guards the interval BEFORE the reply, which it cannot reach: openSheet swaps
  // state.schema synchronously, so from that moment until the rows land, every record button
  // still on screen is a button over ANOTHER sheet's row under this sheet's schema. Clicking one
  // builds a form out of the old row and Save writes it into this sheet — the same payload the
  // stale-callback bug produced, by a different route.
  //
  // It is not only a window. A failed read shows its error and returns without ever rebuilding
  // the list, so without this the editor would sit indefinitely on the previous sheet's records
  // with the new sheet's schema behind them, one click from a cross-sheet write.
  function clearRecords() {
    document.getElementById('records').innerHTML = '';
    state.rows = [];
    state.ids = [];
    state.idSets.__self = new Set();
  }

  /// Loads a sheet and rebuilds the record list.
  function openSheet(sheetName) {
    status('Loading ' + sheetName + '…');
    // A sheet switch ends without a form — openSheet stops at renderList — so renderPreviews,
    // which is the other place this happens, is never reached. Leaving it to renderPreviews
    // alone would run the previous sheet's animation forever, with its canvas still on screen
    // above the new sheet's empty form.
    clearPreviews();
    clearForm();
    clearRecords();
    state.sheetName = sheetName;
    state.schema = schemaFor(sheetName);

    if (!state.schema) { status('No schema for sheet ' + sheetName, true); return; }

    var token = ++state.sheetToken;
    var current = function () { return token === state.sheetToken; };

    google.script.run
      .withFailureHandler(function (e) { if (current()) status(e.message, true); })
      .withSuccessHandler(function (data) {
        if (!current()) return;
        state.rows = data.rows;
        collectIds();
        loadReferencedSheets(function () {
          // The one place staleness is decided for the referenced-sheet phase. Its own handlers
          // do NOT guard: an id + name list is sheet-agnostic and worth keeping whatever the
          // user did while it was in flight. What must not happen is RENDERING it — the picker
          // replies for sheet A can land after the user has moved to sheet B, and done() would
          // then draw A's records under B's schema.
          if (!current()) return;
          renderList();
          var warning = warnings();
          status(state.rows.length + ' records' + (warning ? ' — ' + warning : ''), !!warning);
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

    // NOT de-duplicated across calls, and the handlers below store unconditionally, so two
    // in-flight requests for ONE sheet name are last-writer-wins. Reachable in principle now
    // that a save drops a cache entry: if the older reply lands last it reinstates the pre-save
    // snapshot, and an FK to the record just created reads "does not exist" until the next sheet
    // open. Left as a note rather than a guard — the reviewer's attempt to construct it did not
    // produce two concurrent requests for one name, and a guard for a race nobody can
    // demonstrate is a guard nobody can test. It is self-healing either way.
    var names = Object.keys(needed).filter(function (n) { return !state.pickerData[n]; });
    if (!names.length) { done(); return; }

    var remaining = names.length;
    names.forEach(function (name) {
      google.script.run
        .withFailureHandler(function () {
          // Recorded, not swallowed. done() still runs — the form must render, since every
          // other field is editable and the fk fields are plain text inputs — but the record
          // can no longer be SAVED past unverifiedRefs(), and the warning says so.
          if (state.refErrors.indexOf(name) === -1) state.refErrors.push(name);
          remaining -= 1;
          if (!remaining) done();
        })
        .withSuccessHandler(function (data) {
          state.pickerData[name] = data.entries;
          state.idSets[name] = new Set(data.entries.map(function (e) { return Number(e.id); }));
          // A retry that succeeded clears the distrust for this sheet, so the save gate opens
          // again without a reload.
          var failedAt = state.refErrors.indexOf(name);
          if (failedAt !== -1) state.refErrors.splice(failedAt, 1);
          remaining -= 1;
          if (!remaining) done();
        })
        .readSheetIndex(name, nameIndex(schemaFor(name)), extraIndex(name));
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
    // A record button outlives the rows it was built from only if something has gone wrong, but
    // "wrong" here means rowToValues(undefined) quietly producing an all-blank record under a
    // real rowNumber — a form that looks like an empty record and saves like a wipe of row N.
    // Refuse instead.
    if (!state.rows[index]) return;
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

  // Whether ANY column of this sheet draws a character part, which is exactly the question
  // bundlesFor asks: one such control is enough to need the parts atlas. Read from the table rather
  // than from a list of sheet names, so a sheet gaining a part graphic cannot be forgotten.
  function hasPartGraphic(schema) {
    return schema.columns.some(function (c) {
      return !!Layout.partGraphic(schema.sheet, c.name);
    });
  }

  // The sheet's WORN-ITEM graphic and its spec — `{ name, spec }` — or null. Narrower than
  // hasPartGraphic above, and the narrowing is the point: this is what renderPreviews draws the
  // item-icon-plus-worn-character pair from, and that pair only makes sense for an INVENTORY ITEM
  // whose worn sprite hangs off another cell (`categoryFrom`, i.e. item_slot). The three appearance
  // ids on NPCs and Spell Effects are part graphics too, but they are layers of the character canvas
  // that panel already draws — asking for the item pair on those sheets would put a lone helmet
  // canvas and an empty icon box beside a whole character. Items is the only such sheet today.
  function wornGraphicOf(schema) {
    for (var i = 0; i < schema.columns.length; i++) {
      var spec = Layout.partGraphic(schema.sheet, schema.columns[i].name);
      if (spec && spec.categoryFrom) return { name: schema.columns[i].name, spec: spec };
    }
    return null;
  }

  // The sheet's first Graphic composite, or null — the inventory-icon pair the panel shows beside
  // the worn sprite. First, not "the one for Items": Spell Effects has two, and no sheet that has a
  // part graphic has more than one today.
  function graphicPairOf(schema) {
    var comps = (schema.composites || []).filter(function (c) { return c.kind === 'Graphic'; });
    return comps.length ? comps[0] : null;
  }

  // Which bundles this sheet's controls will ask for. Every sheet gets icons (the Graphic
  // composite and its previews); parts only where a character is drawn; effects only where an
  // animation is.
  function bundlesFor(schema) {
    var names = ['icons'];
    var hasBody = schema.columns.some(function (c) { return c.name === 'body_id'; });
    var hasEquip = (schema.composites || []).some(function (c) { return c.kind === 'EquipSlots'; });
    // A part graphic draws a character sprite too — Items has no body_id and no EquipSlots
    // composite, so without this its worn preview and its graphic_equip control would both sit
    // blank with "no character art loaded" forever. Read from the table rather than named here, so
    // a second sheet gaining one cannot be forgotten.
    if (hasBody || hasEquip || hasPartGraphic(schema)) names.push('parts');
    if (['Spells', 'Spell Effects'].indexOf(schema.sheet) !== -1) names.push('effects');
    return names;
  }

  function renderForm(values) {
    state.loaded = values;
    // Dropped BEFORE the new controls register theirs, so a callback holding a canvas from the
    // previous record cannot redraw into a detached node.
    state.imageCallbacks = [];
    state.formCallbacks = [];

    // Captured BEFORE the bundles are asked for. Opening a second record while the first is
    // still waiting on a decode would otherwise let the earlier render land last, putting one
    // record's values on screen under the other record's rowNumber and stored values — so Save
    // would write what you see into the row you cannot see.
    var token = ++state.formToken;

    // Between here and the render below, rowNumber/loaded already describe the new record while
    // the DOM still shows the previous one. save() refuses while this is up; the flag is only
    // dropped by the render that owns the CURRENT token, by clearForm, or by a newer renderForm
    // taking it over.
    state.formPending = true;

    // ICONS AND NOTHING ELSE BEFORE THE FORM. Waiting on every bundle this sheet needs put a
    // SECOND multi-megabyte PNG decode (parts is 1.98MB, icons 1.75MB) in front of the first field
    // of the first Items record of a session — on the sheet that is opened most, and usually first.
    // The download was already paid either way; sprites-parts.html is an unconditional include.
    //
    // The rest are asked for afterwards and land through imagesChanged(), which is exactly the path
    // onImagesReady exists for: every control that needs art subscribes, so a bundle that arrives
    // late fills a blank canvas rather than being missed. A form is far more useful with its fields
    // present and one small canvas blank for a moment than absent entirely.
    var rest = bundlesFor(state.schema).filter(function (n) { return n !== 'icons'; });

    loadBundle('icons', function () {
      if (token !== state.formToken) return;
      var container = document.getElementById('form');
      // The FORM gets the stored cells verbatim; the PANEL gets them resolved, for the reason
      // refreshPreviews gives. Same split Forms.render makes internally for its own controls.
      Forms.render(container, state.schema, values, ctx());
      renderPreviews(container, Forms.effective(values, state.schema.columns));
      state.formPending = false;

      if (!rest.length) return;
      loadBundles(rest, function () {
        if (token !== state.formToken) return;
        // The PANEL canvases are pushed the late bundle from here rather than subscribing through
        // onImagesReady: renderPreviews runs on every keystroke, so a registration inside it would
        // stack one callback per edit. The form's own controls need no such help — they subscribed
        // once, at build time, and imagesChanged() has already reached them.
        renderPreviews(container,
                       Forms.effective(Forms.collect(container, state.schema),
                                       state.schema.columns));
      });
    });
  }

  // The values the previews actually READ, as one string. The redraw is deduplicated by CONTENT:
  // typing in item_name or npc_title, which no preview reads, rebuilds nothing, and a keystroke
  // that reaches the delegated listener more than once still renders once.
  //
  // It is also what makes the frozen case come out right for free: while an equip slot holds a
  // typo the composite does not write its cell, so the key does not move and the preview does
  // not either — the same guarantee notify() gives, without a second frozen test that could
  // drift out of step with the first.
  function previewKey(values) {
    var parts = ['body_id', 'body_r', 'body_g', 'body_b', 'body_a', 'hair_id', 'hair_r',
                 'hair_g', 'hair_b', 'hair_a', 'face_id', 'body_state', 'equipped_items',
                 'spell_effect_id', 'spell_animation',
                 // The Items panel: the icon pair, the worn graphic, the tint the game applies to
                 // both, the slot that decides which sprite folder the worn one comes from, and
                 // the usetype that decides whether the worn canvas is drawn at all.
                 // body_state is already above, and picks the pose.
                 'graphic_tile', 'graphic_file', 'graphic_equip', 'item_slot', 'item_usetype',
                 'graphic_r', 'graphic_g', 'graphic_b', 'graphic_a'].map(function (name) {
      return name + '=' + str(values[name]);
    });
    return state.sheetName + '\u0000' + parts.join('\u0000');
  }

  // Re-renders the previews from the CURRENT form contents. Reading the form back rather than
  // the stored row is the point: the preview has to show the edit, not the record.
  //
  // RESOLVED ONCE, HERE. Everything downstream of this function draws — the panel, and every
  // control that watches a neighbouring cell — and a drawing needs the record the DATABASE will
  // hold: a blank cell is skipped on import (CsvToSqlBase.cs:27) and the column default lands, so
  // a blank body_state is an unarmed 3 and a blank body_id is body 1. Reading the raw cells drew a
  // 0 for each, which is a pose and a body nobody stored. Save still writes the raw cells.
  function refreshPreviews(container) {
    var values = Forms.effective(Forms.collect(container, state.schema), state.schema.columns);

    // Before the previewKey short-circuit, and deliberately NOT behind it. The two consumers
    // answer different questions: previewKey asks "would the panel look different", while a
    // control that subscribed asked "has any cell moved" — a control's own field is not in the
    // key, so gating this on the key would drop the very edit the subscriber is watching for.
    // Each callback owns its own cheapness.
    state.formCallbacks.forEach(function (fn) { fn(values); });

    if (previewKey(values) === state.previewKey) return;
    renderPreviews(container, values);
  }

  // `values` is an EFFECTIVE record — blanks already resolved to their column defaults by
  // Forms.effective. Every caller resolves before calling; nothing here re-derives a default.
  function renderPreviews(container, values) {
    var host = document.getElementById('previews');
    host.innerHTML = '';
    state.previewKey = previewKey(values);

    // Unconditional, not inside the effect branch: switching to a record with no effect would
    // otherwise leave the previous animation's interval running forever, drawing into a canvas
    // that is no longer in the tree.
    if (state.stopEffect) { state.stopEffect(); state.stopEffect = null; }


    // ONE REDRAW MECHANISM: the delegated input/change listener in init(). There is no
    // subscription hook for a composite to register against — composites.js used to offer
    // `wrapper.__onChange` and nothing here ever set it, so a control that "notified" it was
    // notifying nobody. That silence cost a release: the colour picker writes graphic_r/g/b/a and
    // equipped_items from mousedown, mousemove, keydown and click, none of which the browser
    // turns into a bubbling input/change, and the previews sat stale through every one of them.
    //
    // So the rule for a control whose write path does not bubble is to DISPATCH a real bubbling
    // event from a node inside this container (ColorPicker.control's fire() and
    // Composites.notify() are the two that do), not to revive a hook. Half 1 of the contract at
    // the top of composites.js states it; a preview-relevant cell written without one of those
    // two is a stale panel, and no test of that control alone will see it.

    if (state.schema.columns.some(function (c) { return c.name === 'body_id'; })) {
      var canvas = Forms.el('canvas',
        { width: Preview.CANVAS_W * Preview.CHARACTER_SCALE,
          height: Preview.CANVAS_H * Preview.CHARACTER_SCALE, class: 'appearance' });
      host.appendChild(canvas);

      Preview.character(canvas, {
        bodyId: values.body_id,
        bodyR: values.body_r, bodyG: values.body_g, bodyB: values.body_b, bodyA: values.body_a,
        hairId: values.hair_id,
        hairR: values.hair_r, hairG: values.hair_g, hairB: values.hair_b, hairA: values.hair_a,
        faceId: values.face_id,
        // Absent on Spell Effects, whose appearance override has no body_state at all — no column,
        // so no default either, and Forms.effective leaves it undefined. That reads as armed, which
        // is the only answer available for a sheet that does not carry the pose.
        bodyState: values.body_state,
        equippedItems: values.equipped_items || '',
      }, ctx(), Preview.CHARACTER_SCALE);
    }

    // A PART-GRAPHIC SHEET: two canvases, because such an item is two sprites and the pair is the
    // point. The tile is what a player sees in their bag; the equip graphic is what they see on
    // their character, and the same tint columns colour both. Items is the only such sheet today.
    //
    // Driven off Layout.PART_GRAPHICS and the schema, NOT off the sheet name — the same table
    // bundlesFor reads, and for the same reason: a second sheet gaining a part graphic gets the
    // parts bundle and a partControl from the table automatically, so a name spelled out here
    // would leave exactly that sheet with a control and no panel. previewKey below still lists its
    // column names literally; that is a cheap dedup key, and a new sheet's cells missing from it
    // costs a skipped redraw, not a wrong picture.
    var part = wornGraphicOf(state.schema);
    if (part) {
      // Read out of Layout.TINTS, like every other tint in the editor. null there means the game
      // draws this graphic plain, and Preview reads a missing channel as 0, which is untinted.
      var tintColumns = Layout.tintColumns(state.schema.sheet, part.name) || [];
      var tint = function (item) {
        return Object.assign(item, {
          r: values[tintColumns[0]], g: values[tintColumns[1]],
          b: values[tintColumns[2]], a: values[tintColumns[3]],
        });
      };

      var pair = graphicPairOf(state.schema);
      if (pair) {
        var icon = Forms.el('canvas',
          { width: Preview.ICON_BOX * Preview.ICON_SCALE,
            height: Preview.ICON_BOX * Preview.ICON_SCALE, class: 'item-icon' });
        host.appendChild(icon);
        Preview.itemIcon(icon, tint({
          graphic: values[pair.columns[0]], file: values[pair.columns[1]],
        }), ctx(), Preview.ICON_SCALE);
      }

      // ONLY FOR A WEARABLE RECORD. A NoUse or Scroll item is never drawn on a character, so a
      // worn-character canvas for one answers a question nobody asked — the same gate that hides
      // the graphic_equip and item_slot rows in the form (Layout.wearableGate; forms.js applies
      // it there). Within the gate it is still drawn even when the part graphic is empty or the
      // slot is undrawn: the canvas then shows the bare base body, which is a legible "this item
      // is not worn" rather than a gap in the panel that reads as a broken preview.
      var gate = Layout.wearableGate(state.schema.sheet);
      if (!gate || gate.values.indexOf(str(values[gate.column])) !== -1) {
        var worn = Forms.el('canvas',
          { width: Preview.CANVAS_W * Preview.CHARACTER_SCALE,
            height: Preview.CANVAS_H * Preview.CHARACTER_SCALE, class: 'worn' });
        host.appendChild(worn);
        Preview.wornItem(worn, tint({
          id: values[part.name], slot: values[part.spec.categoryFrom],
          bodyState: values.body_state,
        }), ctx(), Preview.CHARACTER_SCALE);
      }
    }

    // WHICH ID SPACE: the effects atlas is keyed by ANIMATION id — the space Sprites.effectFrames
    // takes and the space spell_animation holds. Spell Effects has that cell in the form; Spells
    // has spell_effect_id, the pk of a Spell Effects ROW, which must be resolved through that
    // row's spell_animation (carried on its picker entries as `extra`). Drawing the row id AS an
    // animation showed nothing for almost every spell — the two id spaces barely overlap — and an
    // unrelated animation where they collided, which reads as "my id is wrong" to a designer whose
    // id is right.
    var effectId = 0;
    if (state.sheetName === 'Spell Effects') effectId = num(values.spell_animation);
    else if (state.sheetName === 'Spells') effectId = effectAnimation(values.spell_effect_id);

    if (effectId > 0) {
      var anim = Forms.el('canvas',
        { width: Preview.EFFECT_SIZE * Preview.EFFECT_SCALE,
          height: Preview.EFFECT_SIZE * Preview.EFFECT_SCALE, class: 'effect' });
      host.appendChild(anim);
      state.stopEffect = Preview.effect(anim, effectId, ctx(), Preview.EFFECT_SCALE);
    }
  }

  // Spells only: the animation behind a spell_effect_id, or 0 when the id is blank, names no
  // known row, or the Spell Effects list has not arrived — a blank panel, never a wrong
  // animation. `extra` is the row's spell_animation, requested via extraIndex().
  function effectAnimation(effectRowId) {
    var id = num(effectRowId);
    if (id <= 0) return 0;
    var entries = state.pickerData['Spell Effects'] || [];
    for (var i = 0; i < entries.length; i++) {
      if (num(entries[i].id) === id) return num(entries[i].extra);
    }
    return 0;
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

  // A BODY THAT HAS BECOME A MONSTER TAKES THE DEAD CELLS WITH IT. Not a gate — it does not refuse
  // the save, it changes what the save writes — and the only place in the editor that edits a cell
  // the user did not: a body >= 100 renders alone (Character.cs:218-223, ported in
  // Appearance.layers), so the face id, the hair id and all six equipment slots are cells the client
  // will never read again, and forms.js has just hidden their rows. Leaving them behind stores
  // equipment the NPC does not have.
  //
  // ONLY ON THE CROSSING, decided against the values the record was LOADED with. A row that was
  // already a monster keeps whatever it stores: opening a record must not change it, and re-clearing
  // on every save of an untouched row would rewrite cells the user never saw a field for. A new
  // record loads blank, which reads as body 0 — a player body — so typing 150 into a new NPC counts
  // as a crossing and clears, which is right.
  //
  // Mutates `values` in place, so everything downstream — validateRecord, the cells array, the
  // loaded snapshot the next save diffs against — sees one record and not two. The FORM is left
  // alone here; save()'s success handler re-renders it from these values, because the equip-slot
  // composite holds its six slots in memory and would otherwise write the old equipment back on the
  // next edit. Returns the columns it cleared, for that decision and for the status line.
  function clearForMonsterBody(values) {
    var gate = Layout.monsterBodyGate(state.sheetName);
    if (!gate) return [];
    if (!Layout.isMonsterBody(state.sheetName, values)) return [];
    if (Layout.isMonsterBody(state.sheetName, state.loaded)) return [];

    var byName = Object.create(null);
    state.schema.columns.forEach(function (c) { byName[c.name] = c; });

    var cleared = [];
    gate.clear.forEach(function (name) {
      if (!byName[name]) return;
      // ZERO, not blank. Blank means "use the SQL default" (CsvToSqlBase.cs:36), which for face_id
      // and hair_id is itself 0 — so this is not about what imports, it is about what a human
      // reading the sheet sees: an explicit 0 is a cell someone decided about, where a blank is
      // indistinguishable from one nobody has filled in yet.
      //
      // equipped_items cannot be either: a blank or '0' emits a malformed token stream into the
      // MakeCharacter packet (Goose/Packets.cs:161), so its empty value is the six-slot form
      // Equipped.format produces — which is also the column's own SQL default.
      var empty = name === 'equipped_items' ? Equipped.format([]) : '0';
      if (str(values[name]) === empty) return;
      values[name] = empty;
      cleared.push(name);
    });

    return cleared;
  }

  // Gate 3: fk values this record cannot have checked. Validation.validateCell passes an fk whose
  // id set is ABSENT — deliberately, because the sets arrive asynchronously and failing closed
  // would block saves on rows the user never touched — so on its own that is a hole: a
  // readSheetIndex failure would let a nonexistent id through the one check that exists for it.
  //
  // This is the closed half of that pair, and it is narrow on purpose. Only the columns this
  // record actually USES are gated: blank and '0' mean "none" and validateCell exempts them
  // anyway, so a Spells record with spell_effect_id = 0 saves normally even when the Spell Effects
  // list is missing. What it refuses is exactly the case that would fail open — a real id with no
  // list to check it against.
  //
  // It reads idSets rather than refErrors, so it also covers a list that never arrived for any
  // other reason (a sheet missing from GOOSE_SCHEMA, a reply still in flight).
  function unverifiedRefs(values) {
    var sheets = [];
    state.schema.columns.forEach(function (c) {
      if (!c.ref || state.idSets[c.ref]) return;
      var value = str(values[c.name]).trim();
      if (value === '' || value === '0') return;
      if (sheets.indexOf(c.ref) === -1) sheets.push(c.ref);
    });
    return sheets;
  }

  // Gate 4: every graphic pair that cannot be resolved in the bundle. Collected rather than
  // counted so the message can name the columns — Spell Effects carries two of these controls
  // and "a graphic is invalid" would send the user hunting.
  function graphicErrors(container) {
    return walk(container)
      .filter(function (n) { return !!n.__graphicError; })
      .map(function (n) { return n.__graphicError; });
  }

  function save() {
    if (!state.schema) return;

    // publishCheck has state.checking; this is the same guard for the same reason, and it
    // matters more. Two clicks before the round-trip resolves issue two writeRow calls, and on
    // the nine sheets with no pk idColumnIndex is -1, so Code.gs's duplicate scan is disabled BY
    // DESIGN and both of them append. There is no second line of defence for those sheets.
    if (state.saving) { status('Still saving — one moment', true); return; }

    // The record is still rendering: rowNumber already names it but the DOM still holds the
    // previous form, so collecting now would save the old record's fields under the new row.
    if (state.formPending) { status('Still opening the record — one moment', true); return; }

    var container = document.getElementById('form');

    // No record is open — a sheet was loaded but nothing clicked. Collecting the empty
    // container fails every required column and reports "N problem(s)" with no form on screen
    // to show them under, which reads as a broken editor rather than a missing step.
    if (!container.children.length) {
      status('Open a record first — click one in the list, or New record.', true);
      return;
    }

    if (frozen(container)) {
      status('An equipment slot holds an invalid graphic id — fix it before saving. Edits to ' +
             'the other slots are not being recorded until you do.', true);
      return;
    }

    var values = Forms.collect(container, state.schema);

    var unfaithful = unfaithfulEdit(values);
    if (unfaithful) { status(unfaithful, true); return; }

    // AFTER gate 2, deliberately. Clearing equipped_items rewrites the cell, so running it first
    // would make a monster-body save on one of the five malformed rows look like the unfaithful edit
    // gate 2 refuses — when it is the opposite: gate 2 exists because rewriting six slots from a
    // GUESS loses equipment, and this writes the empty stream on a body that has no equipment at
    // all. The guess is not involved.
    var cleared = clearForMonsterBody(values);

    var unverified = unverifiedRefs(values);
    if (unverified.length) {
      // Reloading here rather than behind a button the user has to find: the failure is usually
      // transient, the fix needs no decision from them, and a save they have already asked for is
      // the moment they care. The message stands until the reply lands and replaces it.
      status('Cannot check this record\'s ids against ' + unverified.join(' and ') +
             ' — that list failed to load, so saving now could store an id that does not exist. ' +
             'Reloading it; try saving again in a moment.', true);
      retryReferencedSheets(unverified);
      return;
    }

    var graphics = graphicErrors(container);
    if (graphics.length) {
      status('Fix the graphic before saving — ' + graphics.join('; '), true);
      return;
    }

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

    // The record AS IT WAS LOADED, so writeRow can tell "the user edited this cell" from
    // "someone else did": a cell where posted equals loaded is never written (a concurrent edit
    // to it survives), and an edited cell whose sheet value matches neither is refused as a
    // conflict instead of silently overwriting the other editor. An append has no snapshot.
    var loadedCells = state.rowNumber > 0
      ? state.schema.columns.map(function (c) { return str(state.loaded[c.name]); })
      : null;

    // The Text columns, so writeRow pins their format to plain text before writing — setValues
    // parses strings like typed entry, so "1-2" in a description became a Date and "01" a 1.
    var textColumns = [];
    state.schema.columns.forEach(function (c, i) {
      if (c.kind === 'Text') textColumns.push(i);
    });

    status('Saving…');
    state.saving = true;
    // The write is bound to the sheet it was composed against, not to whatever is open when it
    // answers. Without this, saving Items and then switching to Maps ends with the Maps cache
    // untouched and the Items cache intact. savedFormToken is the same binding one level down:
    // whether the record the save came FROM is still the record on screen.
    var savedSheet = state.sheetName;
    var savedToken = state.sheetToken;
    var savedFormToken = state.formToken;
    google.script.run
      .withFailureHandler(function (e) {
        state.saving = false;
        if (savedToken === state.sheetToken) status(e.message, true);
      })
      .withSuccessHandler(function (written) {
        state.saving = false;
        // The sheet just changed, so its cached id + name list is out of date. Left alone, a new
        // record is invisible to every FK pointing at this sheet until the page is reloaded —
        // and that failure is CLOSED, not open: validation.js waves through an absent id set but
        // reports a real id as missing from a stale one. Dropped whatever the user is looking at
        // now: the sheet that CHANGED is the sheet whose cache is stale.
        delete state.pickerData[savedSheet];
        delete state.idSets[savedSheet];

        // AND RE-REQUESTED, not just dropped. The sheet on screen may REFERENCE the saved one —
        // the user moved to NPC Drops while an Items write was in flight, or is sitting on
        // Spell Effects, which references itself. Its pickers read pickerData at use time and
        // its saves are gated on idSets, so a dropped entry nothing re-requests is a picker
        // stuck on "loading…" and a save refused as "failed to load" until
        // retryReferencedSheets runs — which it only does AFTER a refused save.
        // loadReferencedSheets asks for exactly the lists the open sheet is missing, which for
        // most sheets is none.
        if (state.schema) loadReferencedSheets(function () {});

        if (savedToken !== state.sheetToken) return;

        // The write is patched into the LOCAL copy of the sheet rather than reloading it:
        // openSheet runs clearForm(), and the user may have opened another record and typed
        // into it while the write was in flight — a reload here silently threw that away and
        // snapped the form back to the saved row. The patch keeps the list labels, the id set
        // and the duplicate check current; other editors' rows refresh on the next sheet open,
        // as they always did.
        var row = num(written && written.row);
        var index = row - 2;
        if (index >= 0 && index <= state.rows.length) {
          state.rows[index] = state.schema.columns.map(function (c) {
            return str(values[c.name]);
          });
          collectIds();
          renderList();
        }

        var warning = warnings();
        // Says what was written that the user did not type. A cleared cell is invisible in the form
        // afterwards — its row is hidden — so without this the save silently changed the record.
        // The body is read through the gate's own column name rather than spelled here, so the
        // message cannot come to name a cell the rule does not read. cleared.length > 0 is only
        // possible when the gate exists.
        var note = cleared.length
          ? ' Cleared ' + cleared.join(', ') + ' — a body of ' +
            str(values[Layout.monsterBodyGate(savedSheet).column]) +
            ' renders alone, with no face, hair or equipment.'
          : '';
        status('Saved. Run /updatesql then /reloadsql in game to publish.' + note +
               (warning ? ' — ' + warning : ''), !!warning);

        // The record's own bookkeeping moves only while it is still the one on screen; after a
        // record switch, rowNumber and loaded describe the NEW form and must be left alone.
        // For an append this is what turns row 0 into the row the server chose, so a second
        // Save edits the record instead of appending a duplicate.
        if (savedFormToken !== state.formToken) return;
        if (row >= 2) state.rowNumber = row;
        state.loaded = values;

        // A CLEARED RECORD IS RE-RENDERED, and it is the only save that is. The cells above are the
        // form's own inputs for every other save, so the DOM already agrees with the sheet — but
        // clearForMonsterBody wrote cells behind the controls' backs, and one of them keeps state a
        // fresh value cannot reach: equipSlotsControl parsed the six slots at build time and holds
        // them in memory, so moving body_id back under 100 and touching a slot would write the
        // equipment we just cleared straight back into the cell. Rebuilding from `values` is what
        // makes the clear stick.
        if (cleared.length) renderForm(values);
      })
      .writeRow(savedSheet, state.rowNumber, cells, idIndex,
                { loaded: loadedCells, textColumns: textColumns });
  }

  /// Publish check: validate every record on every sheet before telling the user to publish.
  ///
  /// Two phases, because a per-sheet check cannot see the sheets it points at. Phase one reads
  /// all 21 sheets and builds one id set per sheet; phase two validates every row against that
  /// whole map. Checking against whatever sheet happens to be open would validate NPC Drops'
  /// item ids against, say, the Maps id set and report every row as broken.
  function publishCheck() {
    var panel = document.getElementById('publish-results');
    // Says so, rather than doing nothing: the run is 21 round trips and a whole spreadsheet's worth
    // of rows over the wire, so a user who clicks again after a few seconds of apparent silence
    // gets an answer instead of the same silence.
    if (state.checking) { status('Still checking all 21 sheets…'); return; }
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

    // ALL 21 READS AT ONCE, not one after the next. Phase two cannot start until every sheet is in
    // hand, so ordering them buys nothing and costs 21 serialised round trips end to end — which is
    // most of the wall clock of the whole check, since each reply is only a sheet's rows and the
    // validation itself is local. Fired together, the latencies overlap.
    //
    // ORDER IS RESTORED BY INDEX, never by arrival: each reply lands in its own slot and the slots
    // are walked in schema order once the last one is in. A report whose order depended on which
    // reply won the race would be a report nobody could compare with the previous run.
    var results = [];
    var failures = [];
    var remaining = sheets.length;

    function readAll() {
      // Not reachable with the shipped schema (21 sheets), but a fan-out of nothing must still
      // finish the run rather than wait for a reply that will never come.
      if (!remaining) { collected(); return; }

      sheets.forEach(function (schema, at) {
        google.script.run
          .withFailureHandler(function (e) {
            // The message, not the Error: `failures[at]` being a string is what tells a failed slot
            // from an untouched one below, and a sheet whose read failed must still be reported.
            failures[at] = e && e.message ? e.message : String(e);
            remaining -= 1;
            if (!remaining) collected();
          })
          .withSuccessHandler(function (data) {
            results[at] = { schema: schema, rows: data.rows };
            remaining -= 1;
            if (!remaining) collected();
          })
          .readSheet(schema.sheet);
      });
    }

    function collected() {
      sheets.forEach(function (schema, at) {
        if (typeof failures[at] === 'string') report(schema.sheet, '-', failures[at]);
        else if (results[at]) loaded.push(results[at]);
      });
      validateAll();
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

    readAll();
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
    // Delegated rather than per-control, because a per-control subscription would only ever reach
    // COMPOSITES: body_id, hair_id, face_id and body_state belong to no composite on NPCs, so
    // forms.js renders them as plain text inputs with no listener of their own, and without this,
    // typing 150 into body_id changes nothing on screen until the record is saved and re-opened.
    // `input` and `change` both bubble;
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
