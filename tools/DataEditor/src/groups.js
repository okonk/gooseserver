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

    (group ? group.rows : []).forEach(function (row) {
      body.appendChild(buildRow(state, columns, row.rowNumber, row.values));
    });
    updateCount(state);

    return container;
  }

  // Counted off the DOM rather than tracked alongside it, so adding and removing cannot leave
  // the header disagreeing with what is on screen.
  function updateCount(state) {
    var n = state.body.querySelectorAll('[data-group-row]').length;
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
    return out;
  }

  /// The rows removed since the panel was rendered, as [{ rowNumber, loaded }].
  function removed(container) {
    var state = container.__group;
    return state ? state.removed.slice() : [];
  }

  return {
    idKey: idKey,
    parentOf: parentOf,
    build: build,
    missingParents: missingParents,
    render: render,
    addRow: addRow,
    collect: collect,
    removed: removed,
  };
})();

if (typeof module !== 'undefined') module.exports = { Groups: Groups };
