/**
 * ONE-OFF MIGRATION. Converts every class_restrictions column in the workbook from the old
 * DENY list (set bit = that class CANNOT use the row) to the ALLOW list the server now reads
 * (set bit = that class CAN use it; Goose/Class.cs, CanUse).
 *
 * Run it once, from the Apps Script editor, against the game data spreadsheet:
 *
 *     previewClassRestrictionsMigration()   // logs what it WOULD do, writes nothing
 *     applyClassRestrictionsMigration()     // writes
 *
 * Then delete this file. It is not part of the editor and nothing calls it.
 *
 * IT IS NOT IDEMPOTENT — running it twice inverts the data back, and the second inversion is
 * not even the identity (see BIT 0 below). applyClassRestrictionsMigration therefore records a
 * flag in the document's properties and refuses a second run. Pass `true` only if you have
 * restored the sheet from version history and genuinely need to run it again.
 *
 * THE CONVERSION, per cell:
 *
 *   blank                   -> left alone. Blank means "SQL default", which is 0 either way.
 *   every class denied      -> left alone AND REPORTED. There is no allow-list value that means
 *                             "nobody": 0 is the unrestricted sentinel, so the honest answer is
 *                             a human decision, not a number this script can pick.
 *   every class allowed     -> 0, not the all-classes mask. 0 is what a class added LATER
 *                             inherits, and a row that never restricted anyone should keep that.
 *   anything else           -> the sum of 2^class_id over the classes that were NOT denied,
 *                              LESS the Game Master (see CLASS_RESTRICTION_IGNORED_CLASSES_) —
 *                              unless it is the only class left, which is a real "GM only" row.
 *
 * BIT 0, AND ANY OTHER BIT NO CLASS CLAIMS, IS DROPPED. 9 of the 13 shipped masks set bit 0 and
 * there is no class 0 — under the deny convention that bit meant nothing, but under the allow
 * convention a leftover bit 0 is the difference between 0 (everyone) and 1 (nobody real), which
 * is the one value that must not be arrived at by accident. The editor's bitmask control
 * preserves foreign bits on an ordinary edit, on purpose; this migration is where they go.
 *
 * COLUMNS ARE FOUND BY POSITION, not by header text. The importer reads cells positionally
 * (CsvToSqlBase.BuildInserts walks the descriptors in order) and the sheet headers are human
 * labels that do not match the column names — 'classes (0)' on three sheets, 'class
 * restrictions (0)' on the fourth — so position is the only thing both sides agree on. The
 * indices below are the ones in the descriptors; if a column is ever inserted to the left of
 * one, this file is wrong and so is the importer.
 */

var CLASS_RESTRICTION_TARGETS_ = [
  { sheet: 'Items', column: 38 },         // ItemsCsvToSql.cs
  { sheet: 'Spells', column: 5 },         // SpellsCsvToSql.cs
  { sheet: 'Combinations', column: 7 },   // CombinationsCsvToSql.cs
  { sheet: 'Quests', column: 6 },         // QuestsCsvToSql.cs
];

var CLASS_RESTRICTION_CLASSES_SHEET_ = 'Classes';

/**
 * Classes that are not game content, matched against the NAME column of the Classes sheet
 * (case- and space-insensitive). A deny mask that merely failed to mention one of these was not
 * a decision anybody made — nothing in the old data restricted the Game Master — so carrying it
 * into the allow list would put a staff class in the item's requirements line and make every
 * "Rogue only" row read as "Rogue or Game Master".
 *
 * NOT the same as excluding it from the migration. A row that meant "GM only" — every real class
 * denied, this one not — is a decision, and it survives: see the rule in gmAwareAllowList_.
 */
var CLASS_RESTRICTION_IGNORED_CLASSES_ = ['game master'];

var CLASS_RESTRICTION_DONE_KEY_ = 'class_restrictions_migrated_to_allow_list';

/** Logs the plan and returns it. Writes nothing. */
function previewClassRestrictionsMigration() {
  var plan = planClassRestrictionsMigration_();
  Logger.log(renderClassRestrictionsPlan_(plan));
  return plan;
}

/**
 * Applies the plan. `force` skips the already-migrated guard — see the header before using it.
 */
function applyClassRestrictionsMigration(force) {
  var props = PropertiesService.getDocumentProperties();
  if (!force && props.getProperty(CLASS_RESTRICTION_DONE_KEY_)) {
    throw new Error(
      'This workbook has already been migrated (' + props.getProperty(CLASS_RESTRICTION_DONE_KEY_) +
      '). Running again would invert the data back. Restore from version history first, then ' +
      'call applyClassRestrictionsMigration(true).');
  }

  var plan = planClassRestrictionsMigration_();

  plan.sheets.forEach(function (entry) {
    var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(entry.sheet);
    // Contiguous runs, so 649 rows of Items is a handful of writes rather than one per row —
    // and unchanged cells are still never written, which is the rule the editor's own writeRow
    // keeps and the reason a formula in this column would survive.
    runs_(entry.changes).forEach(function (run) {
      sheet.getRange(run.row, entry.column, run.values.length, 1)
        .setValues(run.values.map(function (v) { return [v]; }));
    });
  });

  props.setProperty(CLASS_RESTRICTION_DONE_KEY_, new Date().toISOString());
  SpreadsheetApp.flush();

  Logger.log(renderClassRestrictionsPlan_(plan));
  return plan;
}

/** Internal: what would change, per sheet, without touching anything. */
function planClassRestrictionsMigration_() {
  var ss = SpreadsheetApp.getActiveSpreadsheet();
  var classes = readClasses_(ss);
  if (!classes.length) {
    throw new Error('No class ids found in "' + CLASS_RESTRICTION_CLASSES_SHEET_ +
                    '" — without them there is nothing to invert against.');
  }

  var classIds = classes.map(function (cls) { return cls.id; });
  var ignoredIds = classes.filter(function (cls) { return cls.ignored; })
    .map(function (cls) { return cls.id; });

  var plan = {
    classIds: classIds,
    ignoredIds: ignoredIds,
    sheets: [],
    changed: 0,
    unusable: 0,
    unreadable: 0,
  };

  CLASS_RESTRICTION_TARGETS_.forEach(function (target) {
    var sheet = ss.getSheetByName(target.sheet);
    if (!sheet) throw new Error('No worksheet named "' + target.sheet + '"');

    var lastRow = sheet.getLastRow();
    var entry = {
      sheet: target.sheet,
      column: target.column,
      changes: [],     // { row, from, to }
      unusable: [],    // { row, from } — every class denied; no allow-list value says that
      unreadable: [],  // { row, from } — not a whole number
    };
    plan.sheets.push(entry);
    if (lastRow < 2) return;

    var values = sheet.getRange(2, target.column, lastRow - 1, 1).getValues();

    values.forEach(function (cell, i) {
      var row = i + 2;
      var text = cell[0] === null || cell[0] === undefined ? '' : String(cell[0]).trim();
      if (text === '') return;
      if (!/^\d+$/.test(text)) {
        entry.unreadable.push({ row: row, from: text });
        return;
      }

      var denied = Number(text);
      var allowed = classIds.filter(function (id) { return !bitSet_(denied, id); });

      if (!allowed.length) {
        entry.unusable.push({ row: row, from: denied });
        return;
      }

      var to = allowed.length === classIds.length
        ? 0
        : idsToMask_(gmAwareAllowList_(allowed, ignoredIds));
      if (to !== denied) entry.changes.push({ row: row, from: denied, to: to });
    });

    plan.changed += entry.changes.length;
    plan.unusable += entry.unusable.length;
    plan.unreadable += entry.unreadable.length;
  });

  return plan;
}

/**
 * Internal: an ignored class is kept ONLY when it is the whole answer.
 *
 *   allowed = [2, 6(GM)] -> [2].     "Rogue" was the decision; the GM was never mentioned.
 *   allowed = [6(GM)]    -> [6].     "GM only" IS a decision, and dropping it would leave an
 *                                    empty mask, which is 0 — the unrestricted sentinel, and the
 *                                    exact opposite of what the row says.
 *
 * The all-classes case never reaches here: it becomes 0 in the caller, which covers the GM too.
 */
function gmAwareAllowList_(allowed, ignoredIds) {
  var real = allowed.filter(function (id) { return ignoredIds.indexOf(id) === -1; });
  return real.length ? real : allowed;
}

/** Internal: { id, name, ignored } per row of the Classes sheet. Id in column A, name in B. */
function readClasses_(ss) {
  var sheet = ss.getSheetByName(CLASS_RESTRICTION_CLASSES_SHEET_);
  if (!sheet) throw new Error('No worksheet named "' + CLASS_RESTRICTION_CLASSES_SHEET_ + '"');

  var lastRow = sheet.getLastRow();
  if (lastRow < 2) return [];

  var width = Math.min(2, sheet.getLastColumn());
  return sheet.getRange(2, 1, lastRow - 1, width).getValues()
    .map(function (cells) {
      var id = String(cells[0] === null || cells[0] === undefined ? '' : cells[0]).trim();
      var name = width > 1 ? String(cells[1] === null || cells[1] === undefined ? '' : cells[1]) : '';
      return { id: id, name: name };
    })
    .filter(function (cls) { return /^\d+$/.test(cls.id); })
    .map(function (cls) {
      return {
        id: Number(cls.id),
        name: cls.name.trim(),
        ignored: CLASS_RESTRICTION_IGNORED_CLASSES_.indexOf(normalizeClassName_(cls.name)) !== -1,
      };
    });
}

/** Internal: 'Game  Master ' and 'game master' are the same name. */
function normalizeClassName_(name) {
  return String(name).trim().toLowerCase().replace(/\s+/g, ' ');
}

/**
 * Internal: is bit `index` set in `mask`?
 *
 * Division rather than `&`, matching composites.js: `&` coerces to int32, so bit 31 comes back
 * negative and bit 32 and up come back as 0 — which here would read as "that class was never
 * denied" and hand it access.
 */
function bitSet_(mask, index) {
  var place = Math.pow(2, index);
  return Math.floor(mask / place) % 2 === 1;
}

/** Internal: 2^id summed over ids. */
function idsToMask_(ids) {
  return ids.reduce(function (mask, id) { return mask + Math.pow(2, id); }, 0);
}

/** Internal: consecutive rows batched into single writes. */
function runs_(changes) {
  var out = [];
  changes.forEach(function (change) {
    var last = out[out.length - 1];
    if (last && change.row === last.row + last.values.length) last.values.push(change.to);
    else out.push({ row: change.row, values: [change.to] });
  });
  return out;
}

/** Internal: the plan as text, for the execution log. */
function renderClassRestrictionsPlan_(plan) {
  var lines = ['classes: ' + plan.classIds.join(', ')];

  if (plan.ignoredIds.length) {
    lines.push('not added to any allow list: ' + plan.ignoredIds.join(', ') +
               ' (' + CLASS_RESTRICTION_IGNORED_CLASSES_.join(', ') +
               ') — kept only where it is the only class left');
  } else if (CLASS_RESTRICTION_IGNORED_CLASSES_.length) {
    // Silence here would look like "there is no GM", when it more likely means the name in the
    // sheet has changed and every migrated mask is about to name a staff class.
    lines.push('WARNING: no class matched ' + CLASS_RESTRICTION_IGNORED_CLASSES_.join(', ') +
               ' — check the name column of the ' + CLASS_RESTRICTION_CLASSES_SHEET_ + ' sheet');
  }

  plan.sheets.forEach(function (entry) {
    lines.push(entry.sheet + ': ' + entry.changes.length + ' cells to change');
    entry.unreadable.forEach(function (bad) {
      lines.push('  row ' + bad.row + ': "' + bad.from + '" is not a mask — LEFT ALONE');
    });
    entry.unusable.forEach(function (bad) {
      lines.push('  row ' + bad.row + ': ' + bad.from + ' denied every class — LEFT ALONE, ' +
                 'decide by hand which classes may use it');
    });
  });

  lines.push('total: ' + plan.changed + ' changed, ' + plan.unusable + ' unusable, ' +
             plan.unreadable + ' unreadable');
  return lines.join('\n');
}
