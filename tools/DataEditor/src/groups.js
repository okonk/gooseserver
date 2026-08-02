// Join sheets, edited a parent at a time. `NPC Drops` is npc_template_id + item_template_id, and
// as a flat list of records that reads "1 — 1", "1 — 2", "1 — 4471": every entry is a pair of
// numbers, and an NPC's drops are however many of them happen to be adjacent. Grouped, it is one
// entry per NPC and one table per click.
//
// This half is the model — which parent a row belongs to, and what the parent list looks like.
// The table itself is below it and the ops a save posts are below that; keeping the three apart
// is what lets the awkward cases (a parent that is not in the parent sheet, a blank parent cell,
// an id the sheet stores as a number and the client holds as text) be stated as values.
var Groups = (function () {

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  // The same folding Code.gs's idKey_ does, and for the same reason: the sheet holds ids as
  // numbers, the client holds them as strings, and a group keyed on raw text would split one
  // NPC's drops across "1", "1.00" and " 1 ".
  //
  // The Number fold is only sound because every id column is INT/SMALLINT — no id is large
  // enough to lose precision, and none is a string that Number would mangle.
  function idKey(value) {
    var text = str(value).trim().replace(/,/g, '');
    if (text === '') return '';
    var n = Number(text);
    return isNaN(n) ? text : String(n);
  }

  /// The parent column of a grouped sheet — `{ column, index, ref }` — or null.
  ///
  /// The COLUMN comes from Layout, the REF from the schema. Nothing here names a sheet, so the
  /// table and the schema cannot come to disagree about which sheet a parent id points at.
  function parentOf(schema) {
    if (!schema) return null;
    var name = Layout.groupParent(schema.sheet);
    if (!name) return null;

    for (var i = 0; i < schema.columns.length; i++) {
      if (schema.columns[i].name === name) {
        return { column: name, index: i, ref: schema.columns[i].ref || null };
      }
    }
    return null;
  }

  /// The sheet's rows, grouped. `rows` is state.rows — row i is spreadsheet row i + 2 — and
  /// `entries` is the parent sheet's id + name list as loadReferencedSheets stores it, or null.
  ///
  /// Returns [{ key, id, label, count, orphan, rows: [{ rowNumber, values }] }], real groups
  /// first in numeric id order, then orphans, then the one blank-parent group. Sorting the
  /// unreachable ones last keeps them out of the way without hiding them: a drop pointing at an
  /// NPC that no longer exists is dead data the editor should let you find and delete.
  ///
  /// [] means both "not a grouped sheet" and "grouped, but empty"; a caller that needs to tell
  /// them apart asks parentOf.
  function build(schema, rows, entries) {
    var parent = parentOf(schema);
    if (!parent) return [];

    var names = Object.create(null);
    (entries || []).forEach(function (e) { names[idKey(e.id)] = str(e.name); });

    var byKey = Object.create(null);
    var order = [];

    (rows || []).forEach(function (row, i) {
      var key = idKey(row[parent.index]);

      if (!(key in byKey)) {
        var known = key !== '' && (key in names);
        byKey[key] = {
          key: key,
          id: key,
          // The referenced SHEET's name, not a hand-written singular per sheet: "(not in NPCs)"
          // needs no table of nouns and says exactly where to go and look.
          label: key === '' ? '(no parent)'
               : known ? key + ' — ' + names[key]
               : key + ' — (not in ' + (parent.ref || 'the parent sheet') + ')',
          orphan: !known,
          rows: [],
        };
        order.push(byKey[key]);
      }

      var values = {};
      schema.columns.forEach(function (c, j) {
        values[c.name] = row[j] !== undefined ? str(row[j]) : '';
      });

      byKey[key].rows.push({ rowNumber: i + 2, values: values });
    });

    order.sort(function (a, b) {
      // Blank last of all, then orphans, then real groups by id. Rank first so the numeric
      // comparison below only ever runs between two groups of the same kind.
      var rank = function (g) { return g.key === '' ? 2 : (g.orphan ? 1 : 0); };
      if (rank(a) !== rank(b)) return rank(a) - rank(b);
      var na = Number(a.key);
      var nb = Number(b.key);
      if (isNaN(na) || isNaN(nb)) return a.key < b.key ? -1 : (a.key > b.key ? 1 : 0);
      return na - nb;
    });

    order.forEach(function (g) { g.count = g.rows.length; });
    return order;
  }

  /// The parent entries with no group yet. The New-group picker offers EVERY parent — one way to
  /// reach any of them — and uses this only to say which are empty.
  function missingParents(groups, entries) {
    var has = Object.create(null);
    (groups || []).forEach(function (g) { has[g.key] = true; });
    return (entries || []).filter(function (e) { return !has[idKey(e.id)]; });
  }

  // A row's controls all share this prefix, and no two rows share one. Sequential rather than
  // derived from the row number, because an appended row has no row number yet and two of them
  // would collide on 0.
  //
  // The counter is MODULE-wide, not per panel: ids live in one document, so two panels each
  // starting at 0 would mint `g0-` twice — the very collision the prefix exists to prevent, and
  // mounting several panels at once is the reason render takes its container as a parameter.
  var seq = 0;

  // How many rows a group draws before it asks. Every row is a handful of controls, and an FK
  // cell is a typeahead with its own listeners — cheap individually, not in the hundreds.
  //
  // A CAP ON DRAWING, NOT ON THE MODEL. The undrawn rows stay in state.pending and are put back
  // by collect(), so a save from a capped group leaves them exactly as they were. Capping the
  // model instead would post the drawn rows and make the rest look like removals.
  var RENDER_CAP = 100;

  function prefixFor(n) {
    return 'g' + n + '-';
  }

  // The columns a group table shows: everything but the parent, which the group already says.
  function visibleColumns(schema, parent) {
    return schema.columns.filter(function (c) { return c.name !== parent.column; });
  }

  /// Renders one group's rows as a table into `container`.
  ///
  /// opts: { container, schema, group, ctx, ids }
  ///   group — one entry from build(); its `rows` are the records, `id` the parent
  ///   ctx    — app.js's ctx(), for the pickers. A per-row idPrefix is added on top of it.
  ///   ids    — every id already in the sheet, for allocating a pk on a new row
  ///
  /// The container is passed in rather than looked up: a parent-centric editor mounts several of
  /// these inside one form, and a module that reaches for document.getElementById('form') could
  /// only ever have one. Re-rendering the same container replaces its rows and forgets what had
  /// been removed, which is what makes it safe to switch groups in place.
  ///
  /// A null `group` draws an empty panel, for display only: addRow on one would mint rows whose
  /// parent cell is blank. Task 5's caller always has a real group — synthesizing one for a
  /// parent with no rows yet — so nothing reaches addRow through this door.
  function render(opts) {
    var container = opts.container;
    var schema = opts.schema;
    var group = opts.group;
    var parent = parentOf(schema);

    container.innerHTML = '';

    var state = {
      schema: schema,
      parent: parent,
      parentId: group ? group.id : '',
      ids: (opts.ids || []).slice(),
      ctx: opts.ctx,
      removed: [],
      body: null,
      count: null,
      // The rows the cap did not draw. Model, not markup: collect() puts them back.
      pending: [],
      // Whether a duplicate pass has run on this panel, so show-all can mark what it draws.
      marked: false,
    };
    container.__group = state;

    var columns = visibleColumns(schema, parent);

    var head = Forms.el('div', { class: 'group-head' });
    head.appendChild(Forms.el('h3', null, group ? group.label : ''));
    state.count = Forms.el('span', { class: 'count' });
    head.appendChild(state.count);
    container.appendChild(head);

    var table = Forms.el('div', { class: 'group-table' });
    var header = Forms.el('div', { class: 'group-header' });
    columns.forEach(function (c) { header.appendChild(Forms.el('span', null, c.name)); });
    header.appendChild(Forms.el('span', null, ''));       // the remove button's column
    table.appendChild(header);

    var body = Forms.el('div', { class: 'group-body' });
    table.appendChild(body);
    container.appendChild(table);
    state.body = body;

    var all = group ? group.rows : [];
    var shown = all.slice(0, RENDER_CAP);
    state.pending = all.slice(RENDER_CAP);

    shown.forEach(function (row) {
      body.appendChild(buildRow(state, columns, row.rowNumber, row.values));
    });

    if (state.pending.length) {
      var more = Forms.el('button', { type: 'button', 'data-show-all': '' },
                          'Show all ' + all.length + ' rows');
      more.addEventListener('click', function () {
        state.pending.forEach(function (row) {
          body.appendChild(buildRow(state, columns, row.rowNumber, row.values));
        });
        state.pending = [];
        if (more.parentNode) more.parentNode.removeChild(more);
        updateCount(state);
        // A mark pass that has already run counted the rows just drawn but could not tint them —
        // the note said "3 of them are not on screen yet" and this is the click that puts them
        // there. Re-marking is what makes that promise good.
        if (state.marked) markDuplicates(container, state.schema);
      });
      container.appendChild(more);
    }

    updateCount(state);

    return container;
  }

  // Counted off the DOM rather than tracked alongside it, so adding and removing cannot leave
  // the header disagreeing with what is on screen — plus the rows the cap held back, which are
  // as much a part of the group as the drawn ones. The header answers "how many rows does this
  // NPC have", not "how many did we draw"; the show-all button is what says the difference.
  function updateCount(state) {
    var n = state.body.querySelectorAll('[data-group-row]').length +
            (state.pending || []).length;
    state.count.textContent = n + (n === 1 ? ' row' : ' rows');
  }

  // A group table draws no labels — the column names are header spans, and a span above a column
  // names nothing as far as a screen reader is concerned. Forms.render is where labels normally
  // come from and the table deliberately does not use it, so the name is attached here instead.
  //
  // Only if the control does not already carry one: a picker that has labelled its own input
  // knows more about what it built than this does.
  function nameControl(control, name) {
    if (!control) return;
    var found = control.getAttribute && control.getAttribute('name') !== null ? control
              : control.querySelector ? control.querySelector('[name]') : null;
    if (!found) return;
    if (found.getAttribute('aria-label') !== null) return;
    if (found.getAttribute('aria-labelledby') !== null) return;
    found.setAttribute('aria-label', name);
  }

  // One record. `rowNumber` is 0 for a row being added.
  function buildRow(state, columns, rowNumber, values) {
    var schema = state.schema;
    var prefix = prefixFor(seq++);

    var row = Forms.el('div', { class: 'group-row', 'data-group-row': String(rowNumber) });
    row.__rowNumber = rowNumber;
    // The record AS LOADED, so the save can tell an edited cell from one another editor touched.
    // Null for an appended row: there is nothing it was loaded from.
    row.__loaded = rowNumber > 0 ? values : null;
    // The parent cell never reaches the DOM, so it is carried here and put back by collect().
    row.__parent = state.parentId;

    // Blanks resolved to their SQL defaults, for the controls that read a neighbouring cell.
    // No grouped sheet has such a control today; passed anyway so that stays true by
    // construction rather than by luck.
    var effective = Forms.effective(values, schema.columns);

    // ONE ctx per row, differing only in idPrefix. Object.assign is not available in this
    // dialect, so it is spelled out; the rest of ctx is shared by reference, which is what
    // makes refErrors' mutation-in-place visible to every row.
    var ctx = {};
    Object.keys(state.ctx || {}).forEach(function (k) { ctx[k] = state.ctx[k]; });
    ctx.idPrefix = prefix;

    columns.forEach(function (column) {
      var cell = Forms.el('div', { class: 'group-cell' });

      if (column.pk) {
        // Allocated, not typed. The id of a child row is bookkeeping — nothing in the game
        // refers to a quest requirement by id — so offering it for editing is offering a way to
        // collide with another row for no gain.
        var id = Forms.el('input', { name: column.name, type: 'text', readonly: 'readonly',
                                     id: prefix + column.name, class: 'pk',
                                     'aria-label': column.name });
        id.readOnly = true;
        id.value = values[column.name];
        cell.appendChild(id);
      } else {
        var control = Forms.columnControl({
          column: column, ctx: ctx, sheet: schema.sheet, values: values, effective: effective,
        });
        nameControl(control, column.name);
        cell.appendChild(control);
      }

      cell.appendChild(Forms.el('div', { class: 'error', 'data-error-for': column.name }));
      row.appendChild(cell);
    });

    // In a cell of its own like every other column, so it lays out on the table grid and picks
    // up whatever a row-level tint later paints across the cells.
    var removeCell = Forms.el('div', { class: 'group-cell' });
    var remove = Forms.el('button', {
      type: 'button', class: 'remove', 'data-remove': '',
      title: 'remove this row', 'aria-label': 'remove this row',
    }, '×');
    remove.addEventListener('click', function () {
      // A row that exists in the sheet is recorded for deletion; one that was only ever on
      // screen just goes. Posting a delete for row 0 would be a delete of nothing.
      if (row.__rowNumber > 0) {
        state.removed.push({ rowNumber: row.__rowNumber, loaded: row.__loaded });
      }
      if (row.parentNode) row.parentNode.removeChild(row);
      updateCount(state);
    });
    removeCell.appendChild(remove);
    row.appendChild(removeCell);

    return row;
  }

  /// Adds a blank row to an open panel. The parent cell is filled from the group and a pk, if the
  /// sheet has one, is allocated locally — incrementing, so several new rows in one save cannot
  /// take the same id.
  function addRow(container) {
    var state = container.__group;
    if (!state) return null;

    var schema = state.schema;
    var columns = visibleColumns(schema, state.parent);
    var pk = schema.columns.filter(function (c) { return c.pk; })[0];

    var values = {};
    schema.columns.forEach(function (c) { values[c.name] = ''; });
    values[state.parent.column] = state.parentId;

    if (pk) {
      var id = Validation.nextId(state.ids);
      values[pk.name] = String(id);
      state.ids.push(id);
    }

    var row = buildRow(state, columns, 0, values);
    state.body.appendChild(row);
    updateCount(state);
    return row;
  }

  /// The rows still on screen, as [{ rowNumber, values, loaded }].
  ///
  /// Forms.collect is scoped to the container it is given, so each row is collected on its own
  /// and the result holds exactly this sheet's columns. The parent cell is put back afterwards
  /// because it is never rendered — which is also what makes reparenting impossible by accident.
  function collect(container, schema) {
    var state = container.__group;
    if (!state) return [];

    var rows = state.body.querySelectorAll('[data-group-row]');
    var out = [];
    for (var i = 0; i < rows.length; i++) {
      var values = Forms.collect(rows[i], schema);
      values[state.parent.column] = rows[i].__parent;
      out.push({ rowNumber: rows[i].__rowNumber, values: values, loaded: rows[i].__loaded });
    }

    // The rows the cap did not draw, unchanged: `loaded` is a COPY of `values`, equal cell for
    // cell, so the op builder finds nothing changed and emits no write — which is exactly right,
    // the user never saw them. A copy rather than the same object because a caller that later
    // edited `values` would otherwise rewrite the snapshot it is being diffed against, and the
    // edit would vanish instead of being written.
    (state.pending || []).forEach(function (row) {
      var loaded = {};
      Object.keys(row.values).forEach(function (k) { loaded[k] = row.values[k]; });
      out.push({ rowNumber: row.rowNumber, values: row.values, loaded: loaded });
    });

    return out;
  }

  // The class attribute edited as the whitespace-separated list it is. classList would be
  // shorter, but this touches only setAttribute/getAttribute — the two operations every target
  // this file has to run on supports — and it cannot clobber the classes it was not asked about,
  // which a naive `className = 'group-row duplicate'` would.
  function classes(node) {
    return str(node.getAttribute('class')).split(/\s+/).filter(function (c) { return c !== ''; });
  }

  function setDuplicate(node, on) {
    var list = classes(node).filter(function (c) { return c !== 'duplicate'; });
    if (on) list.push('duplicate');
    node.setAttribute('class', list.join(' '));
  }

  /// Marks rows that duplicate another row of the same group, and answers how many. A warning,
  /// not a refusal: two Map Required Items naming one item is meaningless, but two drops of one
  /// item for one NPC is arguable, and the editor is not where that gets settled.
  ///
  /// Every mark is cleared before any is applied, so a duplicate the user has just fixed does
  /// not stay flagged.
  ///
  /// The tally runs over collect()'s output — the same records validate() sees, with the parent
  /// cell restored and the UNDRAWN rows included — so the count in the status line and the rows
  /// tinted on screen cannot disagree. Undrawn rows count toward the tally but cannot be marked:
  /// they have no element. That is the honest reading — a drawn row really can duplicate one the
  /// cap held back — and it means marked <= returned count, never the other way round. A caller
  /// that wants to say so counts the `.duplicate` elements and compares.
  ///
  /// `records` is collect()'s output when the caller already has it — the save path does — so the
  /// marks and the ops it is about to post are computed from one reading of the DOM rather than
  /// two.
  function markDuplicates(container, schema, records) {
    var state = container.__group;
    if (!state) return 0;

    records = records || collect(container, schema);
    // Remembered so the show-all handler knows to mark the rows it draws.
    state.marked = true;
    var counts = Object.create(null);
    var keys = [];

    records.forEach(function (row) {
      var key = rowKeyOf(schema, row.values);
      keys.push(key);
      counts[key] = (counts[key] || 0) + 1;
    });

    var rows = drawnRows(container);
    var duplicates = 0;
    // collect() emits the drawn rows first, in this same order, then the pending ones — so
    // keys[i] is row i's key for every i the DOM has.
    for (var i = 0; i < rows.length; i++) {
      var dup = counts[keys[i]] > 1;
      setDuplicate(rows[i], dup);
      if (dup) duplicates++;
    }

    // The undrawn rows, which have nothing to mark but still count.
    for (var j = rows.length; j < keys.length; j++) {
      if (counts[keys[j]] > 1) duplicates++;
    }

    return duplicates;
  }

  /// The rows a panel currently has on screen, as elements, in collect()'s order.
  ///
  /// Exported so a caller that needs to line something up against collect()'s output — error
  /// messages, duplicate marks — asks the panel rather than the document. querySelectorAll on
  /// #form would find the rows of EVERY panel mounted in it, and the moment there are two the
  /// slice would be off by a whole group with nothing to say so.
  function drawnRows(container) {
    var state = container.__group;
    return state ? state.body.querySelectorAll('[data-group-row]') : [];
  }

  /// The rows removed since the panel was rendered, as [{ rowNumber, loaded }].
  function removed(container) {
    var state = container.__group;
    return state ? state.removed.slice() : [];
  }

  // The record as an array in schema column order — which IS the sheet's column order, because
  // the importer reads cells by index (CsvToSqlBase.cs:35). A cell Validation says not to write
  // becomes null, so a blank optional cell stays blank and the column's SQL default applies;
  // writing 0 instead would pin a value that was tracking the default.
  function cellsOf(schema, values, idSets) {
    return schema.columns.map(function (c) {
      var check = Validation.validateCell(c, values[c.name], idSets);
      return check.write ? values[c.name] : null;
    });
  }

  function snapshotOf(schema, loaded) {
    return schema.columns.map(function (c) { return str(loaded[c.name]); });
  }

  /// One sheet's op-set for saveBatch, from the rows on screen and the rows removed.
  ///
  /// CALL ONLY ON RECORDS validate() HAS PASSED: a blank required cell is written as null here
  /// like any other blank, which on an existing row is a write that CLEARS it.
  ///
  /// `present` is collect()'s output, `gone` is removed()'s. A row whose every cell still equals
  /// what it was loaded with yields NOTHING — the server would skip it anyway, and leaving it out
  /// keeps the reported count honest.
  function ops(schema, present, gone, idSets) {
    var writes = [];
    var appends = [];

    (present || []).forEach(function (row) {
      var cells = cellsOf(schema, row.values, idSets);

      // On the row NUMBER alone. A row that exists in the sheet but arrived with no snapshot is
      // still an update — appending it instead would add a second copy of a record already
      // there, and this is the only place in the module that could. An empty snapshot posts as a
      // row of blanks, which the server reads as a conflict and refuses: the safe failure.
      if (row.rowNumber > 0) {
        var loaded = snapshotOf(schema, row.loaded || {});
        var changed = false;
        for (var i = 0; i < cells.length; i++) {
          if (str(cells[i]) !== loaded[i]) { changed = true; break; }
        }
        if (changed) writes.push({ row: row.rowNumber, cells: cells, loaded: loaded });
        return;
      }

      appends.push({ cells: cells });
    });

    var deletes = (gone || []).map(function (row) {
      // Same fail-safe as a write: no snapshot means a row of blanks, which the server compares
      // against what is in the sheet and refuses, rather than deleting a row sight unseen.
      return { row: row.rowNumber, loaded: snapshotOf(schema, row.loaded || {}) };
    });

    var pk = schema.columns.filter(function (c) { return c.pk; })[0];
    var textColumns = [];
    schema.columns.forEach(function (c, i) { if (c.kind === 'Text') textColumns.push(i); });

    return {
      sheet: schema.sheet,
      // -1 for the eight grouped sheets with no pk: their column A is an Id-kind FK that
      // legitimately repeats, and 0 would make the server reject every second drop or spawn.
      idColumnIndex: pk ? schema.columns.indexOf(pk) : -1,
      textColumns: textColumns,
      writes: writes,
      appends: appends,
      deletes: deletes,
    };
  }

  /// What makes a row the row it is, as one comparable string.
  ///
  /// TWO ROWS ARE THE SAME RECORD WHEN THIS MATCHES. The columns come from Layout.groupKey — a
  /// per-sheet judgement, not something derivable from the schema — and a sheet with no entry
  /// falls back to every non-pk column, which never calls two genuinely different rows the same.
  /// Both consumers (validate's warning, and the row tinting above it) go through here, so the
  /// count in the message and the rows marked on screen cannot disagree.
  function rowKeyOf(schema, values) {
    var names = Layout.groupKey(schema.sheet);
    var columns = names
      ? schema.columns.filter(function (c) { return names.indexOf(c.name) !== -1; })
      : schema.columns.filter(function (c) { return !c.pk; });

    return columns.map(function (c) {
      // Ids folded the way build() folds them, so 1 and "1.00" are one row, not two. The
      // separator is a unit separator rather than a space: joining on one would make ['a b','c']
      // and ['a','b c'] the same key.
      return c.kind === 'Id' ? idKey(values[c.name]) : str(values[c.name]);
    }).join('\u001f');
  }

  /// Every row validated, plus a count of rows that duplicate another row in the group.
  ///
  /// `duplicates` is the MODEL-level tally over whatever rows it was handed. The UI does not read
  /// it: the status line's number comes from markDuplicates, which tallies over the whole group
  /// — drawn and undrawn alike — and tints as it counts, so the number and the marks agree.
  ///
  /// ownId is the pk the row was LOADED with, exactly as the single-record save() takes it — from
  /// the loaded state and never from the field, so a pk typed over another row's id cannot exempt
  /// itself. Without it every EXISTING row of a grouped sheet that has a pk (Quest Reqs, Quest
  /// Rewards) fails its own duplicate check: its id is in idSets.__self because the sheet it was
  /// just read from is what built that set, and the whole group is unsavable.
  ///
  /// null for an appended row, correctly: addRow allocated its id with nextId over state.ids, so
  /// it is not in __self and has nothing to be exempted from.
  function validate(schema, present, idSets) {
    var pk = schema.columns.filter(function (c) { return c.pk; })[0];

    var rows = (present || []).map(function (row) {
      // Number(), and gated on rowNumber like the single-record path: validateId compares with
      // !== against a number, so the string the sheet was read as would never match.
      var ownId = (pk && row.rowNumber > 0 && row.loaded)
        ? Number(row.loaded[pk.name]) : null;
      var result = Validation.validateRecord(schema.columns, row.values, idSets, ownId);
      return { rowNumber: row.rowNumber, errors: result.errors };
    });

    // Tallied first, then summed: a key seen three times is three duplicate rows, and counting
    // as they arrive means special-casing the second one. This way the count is simply how many
    // rows share their key with another.
    var seen = Object.create(null);
    (present || []).forEach(function (row) {
      var key = rowKeyOf(schema, row.values);
      seen[key] = (seen[key] || 0) + 1;
    });

    var duplicates = 0;
    Object.keys(seen).forEach(function (k) { if (seen[k] > 1) duplicates += seen[k]; });

    return {
      ok: rows.every(function (r) { return r.errors.length === 0; }),
      rows: rows,
      duplicates: duplicates,
    };
  }

  return {
    idKey: idKey,
    parentOf: parentOf,
    build: build,
    missingParents: missingParents,
    render: render,
    addRow: addRow,
    collect: collect,
    drawnRows: drawnRows,
    removed: removed,
    ops: ops,
    rowKeyOf: rowKeyOf,
    markDuplicates: markDuplicates,
    validate: validate,
    RENDER_CAP: RENDER_CAP,
  };
})();

if (typeof module !== 'undefined') module.exports = { Groups: Groups };
