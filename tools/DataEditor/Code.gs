/**
 * Server side of the game data editor. Runs as the user accessing it
 * (appsscript.json executeAs USER_ACCESSING), so the spreadsheet's own sharing
 * permissions are the access control.
 *
 * This is the only code in the project that writes to the spreadsheet, so every
 * data-loss risk routes through writeRow(). It is deliberately paranoid: it would
 * rather fail a legitimate write with a clear error than half-write a record.
 *
 * ---------------------------------------------------------------------------------
 * VERIFICATION LIST
 *
 * Nothing in this file can run outside Apps Script, so it has no unit tests. This list
 * is the substitute: what was checked by reasoning, and what only a live spreadsheet
 * can settle. Work through it during the manual smoke test and when reviewing changes.
 *
 * onOpen
 *   Checked: building a menu is permitted in a simple trigger; the sidebar call happens
 *     in the authorised menu handler, not here.
 *   Live: the menu appears; first-run authorisation completes.
 *
 * doGet / showSidebar
 *   Checked: both name the template 'Editor', matching the Editor.html that build.mjs
 *     passes through to dist/.
 *   Live: both entry points render with no template error.
 *
 * include
 *   Checked: nothing. `name` comes from our own scriptlets but can reach any HTML file
 *     in the script project.
 *   Live: every include resolves; no module global is defined twice.
 *
 * readSheet
 *   Checked: rows[i] is always sheet row i+2, since getDataRange() is
 *     A1:(getLastRow, getLastColumn) — so a stray value below the data shows up as
 *     trailing blank rows and an append lands after it, keeping both views consistent.
 *     Empty sheet returns header [] rather than [''].
 *   Live: TOP RISK — getDisplayValues() returns display text, so any cell formatted as
 *     a date, percentage, or with separators reads back as its formatting and would be
 *     written back that way. Spot-check ids and numeric columns against real values.
 *   Size: not a concern. Largest sheets are Items 649x46 and Spell Effects 259x76,
 *     far inside the 6-minute and payload limits even for the 21-sheet publish check.
 *
 * readSheetIndex
 *   Checked: the lastRow < 2 guard; blank-id rows are skipped.
 *   Live: assumes id in column A and name in column B for every FK target. Try a
 *     picker on a sheet whose column B is not a name.
 *
 * writeRow  (all guards are reasoning, never executed)
 *   Live, each one its own test:
 *     - append to a sheet trimmed to exactly its data (exercises insertRowsAfter)
 *     - overwrite with a cells array NARROWER than the header (must throw, not
 *       half-write leaving the previous record's trailing values in place)
 *     - a sheet carrying a stray column beyond the schema (must still write fine —
 *       width comes from the header row, not the sheet's data extent)
 *     - writeRow with a blank id (must skip the duplicate check, not match every
 *       blank cell in the column)
 *     - the same id from two tabs (must throw the "just taken" error)
 *     - rowNumber = 1 (must refuse; it is the importer's column-order contract)
 *     - an id column with a numeric cell format, e.g. displaying "651.00" (idKey_
 *       must still catch the collision with 651)
 *   Residual risk, accepted per plan scope: the duplicate check and the write are NOT
 *     atomic. Two editors inside the same window can still collide. No LockService.
 *
 * whoAmI
 *   Checked: appsscript.json's explicit oauthScopes omits userinfo.email, so this can
 *     throw as well as return ''; both degrade to 'unknown'.
 *   Live: nothing calls it. Delete it if Task 11 still does not.
 * ---------------------------------------------------------------------------------
 */

function onOpen() {
  // Simple trigger. Building a menu is allowed here without authorisation; the
  // menu item itself runs showSidebar() as a normal, fully authorised call.
  SpreadsheetApp.getUi()
    .createMenu('Game Data')
    .addItem('Open editor', 'showSidebar')
    .addToUi();
}

function doGet() {
  return HtmlService.createTemplateFromFile('Editor')
    .evaluate()
    .setTitle('Goose Game Data Editor')
    .addMetaTag('viewport', 'width=device-width, initial-scale=1');
}

function showSidebar() {
  var html = HtmlService.createTemplateFromFile('Editor')
    .evaluate()
    .setTitle('Game Data Editor');
  SpreadsheetApp.getUi().showSidebar(html);
}

/**
 * Used by Editor.html to inline the built modules. `name` comes from a scriptlet in
 * our own template, never from user input, but note it can name any HTML file in the
 * script project — keep it to the dist/*.html modules.
 */
function include(name) {
  return HtmlService.createHtmlOutputFromFile(name).getContent();
}

/** Internal: the sheet, or a readable error naming what was missing. */
function requireSheet_(sheetName) {
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(sheetName);
  if (!sheet) throw new Error('No worksheet named "' + sheetName + '"');
  return sheet;
}

/**
 * Internal: how many columns the record occupies, taken from the HEADER ROW.
 *
 * Not getLastColumn() — that is the last column with content anywhere in the sheet, so
 * a stray note parked to the right of the data would inflate it. The importer never
 * looks past the schema (CsvToSqlBase.BuildInserts loops the descriptors), so extra
 * columns are benign and must not break writes. Row 1 is the column-order contract
 * (CsvToSqlBase.cs:11-13), so it is the only correct reference for record width.
 */
function headerWidth_(sheet) {
  var extent = sheet.getLastColumn();
  if (extent === 0) return 0;

  var header = sheet.getRange(1, 1, 1, extent).getDisplayValues()[0];
  var width = header.length;
  while (width > 0 && String(header[width - 1]).trim() === '') width--;
  return width;
}

/** Internal: true for the values that mean "leave this cell empty". */
function isBlank_(value) {
  return value === null || value === undefined || value === '';
}

/**
 * Internal: a comparable key for an id cell.
 *
 * The client sees ids as display strings (readSheet uses getDisplayValues) and sends
 * them back as strings, while the sheet holds them as numbers. A numeric cell format
 * on an id column is enough to make those disagree — the client reads "651.00", posts
 * "651.00", and a raw comparison against 651 misses a genuine collision, silently
 * passing the one check that stops two editors taking the same id. Both sides go
 * through here, so 651, "651", "651.00" and " 651 " all collapse to the same key.
 * Non-numeric ids fall back to a trimmed string.
 *
 * (Every id column in the schema is INTEGER/INT/SMALLINT, with live maxima in the
 * hundreds — item_templates 651, spell_effects 259 — so the hazard here is cell
 * formatting, not integer width.)
 */
function idKey_(value) {
  if (value === null || value === undefined) return '';
  var text = String(value).trim();
  if (text === '') return '';
  var num = Number(text);
  return isNaN(num) ? text : String(num);
}

/**
 * Reads a whole worksheet. Returns the header row separately from the data rows so the
 * client can map positionally — the importer reads cells by index (CsvToSqlBase.cs:26),
 * not by header name.
 *
 * Row i of `rows` is spreadsheet row i + 2, which is the numbering writeRow() expects.
 * That holds even if a stray value sits far below the data: getDataRange() and
 * getLastRow() agree on where the sheet ends, so the stray shows up as trailing blank
 * rows here and an append lands after it. Both views stay consistent.
 */
function readSheet(sheetName) {
  var sheet = requireSheet_(sheetName);

  // getDataRange() on an empty sheet is a 1x1 range holding '', which would report a
  // header of [''] . Report a genuinely empty sheet as empty instead.
  if (sheet.getLastRow() === 0 || sheet.getLastColumn() === 0) {
    return { sheet: sheetName, header: [], rows: [], lastRow: 0 };
  }

  var values = sheet.getDataRange().getDisplayValues();

  return {
    sheet: sheetName,
    header: values[0],
    rows: values.slice(1),
    lastRow: sheet.getLastRow(),
  };
}

/** Reads only the first two columns of a sheet, for FK pickers (id + name). */
function readSheetIndex(sheetName) {
  var sheet = requireSheet_(sheetName);

  var lastRow = sheet.getLastRow();
  if (lastRow < 2) return { sheet: sheetName, entries: [] };

  var values = sheet.getRange(2, 1, lastRow - 1, 2).getDisplayValues();
  var entries = [];
  for (var i = 0; i < values.length; i++) {
    if (values[i][0] === '') continue;
    entries.push({ id: values[i][0], name: values[i][1] });
  }

  return { sheet: sheetName, entries: entries };
}

/**
 * Writes one record. `cells` is an array aligned to column order; null, undefined or ''
 * entries are written as empty, which the importer treats as "use the SQL default"
 * (CsvToSqlBase.cs:27). rowNumber is 1-based including the header; pass 0 to append.
 *
 * `cells` must be exactly as wide as the sheet. A shorter array would leave the trailing
 * columns holding the PREVIOUS record's values while the client believed it had written
 * the whole record — a silent, plausible-looking corruption. A longer one would either
 * spill past the schema or throw deep inside setValues. Both are refused up front.
 *
 * Re-checks for a duplicate id immediately before writing, so two editors adding records
 * at the same time are unlikely to both take the same suggested id. This is a narrowing,
 * not a guarantee: the check and the write are not atomic, so a collision inside that
 * window still gets through. Fixing that properly needs LockService and is out of scope.
 */
function writeRow(sheetName, rowNumber, cells, idColumnIndex) {
  var sheet = requireSheet_(sheetName);

  if (!Array.isArray(cells)) throw new Error('writeRow: cells must be an array');
  if (cells.length === 0) throw new Error('writeRow: cells is empty');

  // A sheet with no header has no column-order contract to write against, and nothing
  // bounds the write to the grid's width. Refuse rather than guess. (Schema sheets
  // always carry a header; reaching this means the wrong spreadsheet is open.)
  var width = headerWidth_(sheet);
  if (width === 0) {
    throw new Error('writeRow: sheet "' + sheetName + '" has no header row — nothing to write against');
  }
  if (cells.length !== width) {
    throw new Error(
      'writeRow: got ' + cells.length + ' values for a header ' + width +
      ' columns wide — refusing to write a partial record. Regenerate the schema.');
  }

  // Row 1 is the header the importer reads column order from; never overwrite it.
  if (rowNumber === 1) throw new Error('writeRow: refusing to overwrite the header row');
  if (rowNumber < 0) throw new Error('writeRow: invalid row ' + rowNumber);

  var target = rowNumber > 0 ? rowNumber : sheet.getLastRow() + 1;

  if (idColumnIndex >= 0 && idColumnIndex < cells.length && !isBlank_(cells[idColumnIndex])) {
    var newId = idKey_(cells[idColumnIndex]);
    var lastRow = sheet.getLastRow();

    if (lastRow >= 2) {
      var ids = sheet.getRange(2, idColumnIndex + 1, lastRow - 1, 1).getValues();
      for (var i = 0; i < ids.length; i++) {
        if (idKey_(ids[i][0]) === newId && (i + 2) !== target) {
          throw new Error('id ' + newId + ' was just taken by another editor — reload and retry');
        }
      }
    }
  }

  // No matching column guard is needed: out.length equals the header width, and a
  // header cell in that column means the grid is already at least that wide.
  //
  // getRange() past the bottom of the grid throws; appending to a full sheet has to
  // grow it first. (A sheet trimmed to exactly its data hits this on the first append.)
  var maxRows = sheet.getMaxRows();
  if (target > maxRows) sheet.insertRowsAfter(maxRows, target - maxRows);

  var out = cells.map(function (c) { return isBlank_(c) ? '' : c; });
  sheet.getRange(target, 1, 1, out.length).setValues([out]);
  SpreadsheetApp.flush();

  return { row: target };
}

/**
 * Who is editing, for the UI header.
 *
 * appsscript.json declares an explicit oauthScopes block that does NOT include
 * userinfo.email, which suppresses auto-scoping — so this can throw, not just return
 * '', and it returns '' anyway for users outside the domain. Widening the scopes is a
 * permissions decision and nothing currently calls this, so it just degrades.
 */
function whoAmI() {
  try {
    return Session.getActiveUser().getEmail() || 'unknown';
  } catch (e) {
    return 'unknown';
  }
}
