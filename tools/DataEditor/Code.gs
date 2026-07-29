/**
 * Server side of the game data editor. Runs as the user accessing it
 * (appsscript.json executeAs USER_ACCESSING), so the spreadsheet's own sharing
 * permissions are the access control.
 *
 * This is the only code in the project that writes to the spreadsheet, so every
 * data-loss risk routes through writeRow(). It is deliberately paranoid: it would
 * rather fail a legitimate write with a clear error than half-write a record.
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

/** Internal: true for the values that mean "leave this cell empty". */
function isBlank_(value) {
  return value === null || value === undefined || value === '';
}

/**
 * Internal: a comparable string for an id cell.
 *
 * Deliberately NOT getDisplayValues(): a wide BIGINT id renders as "1.0E+15" once the
 * column picks up a numeric format, and the duplicate check would then silently fail
 * to match a real collision. Raw values give us the number, and String() on a number
 * only reaches exponent form past 1e21, far beyond any id here.
 */
function idKey_(value) {
  if (value === null || value === undefined) return '';
  if (typeof value === 'number') return String(value);
  return String(value).trim();
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

  var width = sheet.getLastColumn();
  if (width > 0 && cells.length !== width) {
    throw new Error(
      'writeRow: got ' + cells.length + ' values for a sheet ' + width +
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

  // getRange() past the bottom of the grid throws; appending to a full sheet has to
  // grow it first. (A sheet trimmed to exactly its data hits this on the first append.)
  var maxRows = sheet.getMaxRows();
  if (target > maxRows) sheet.insertRowsAfter(maxRows, target - maxRows);

  var out = cells.map(function (c) { return isBlank_(c) ? '' : c; });
  sheet.getRange(target, 1, 1, out.length).setValues([out]);
  SpreadsheetApp.flush();

  return { row: target };
}

/** Who is editing, for the UI header. '' for users outside the domain. */
function whoAmI() {
  return Session.getActiveUser().getEmail() || 'unknown';
}
