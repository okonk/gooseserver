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

  return {
    idKey: idKey,
    parentOf: parentOf,
    build: build,
    missingParents: missingParents,
  };
})();

if (typeof module !== 'undefined') module.exports = { Groups: Groups };
