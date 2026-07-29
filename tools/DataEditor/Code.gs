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
 *   Checked: exposure is nil — it returns only this project's own HTML, already served
 *     to that user — but it is reachable from google.script.run, not just our template.
 *   Live: every include resolves; no module global is defined twice.
 *
 * readSheet
 *   Checked: rows[i] is always sheet row i+2, since getDataRange() is
 *     A1:(getLastRow, getLastColumn) — so a stray value below the data shows up as
 *     trailing blank rows and an append lands after it, keeping both views consistent.
 *     Empty sheet returns header [] rather than [''].
 *   Live: TOP RISK — getDisplayValues() returns display TEXT, so a cell's formatting
 *     becomes its value on the next save, silently, for every field of that record.
 *     Ranked by real exposure in this data:
 *       1. Decimal precision loss. 32 DECIMAL(9,4)/(5,4)/(5,2) columns across 9
 *          sheets (verified against schema.js), with live values at 3dp (npc_templates.hp_percent_regen,
 *          spell_effects.spell_crit / spell_damage / damage_reduce). A designer setting
 *          a rate column to 2dp is an ordinary thing to do and the loss is invisible.
 *       2. Thousands separators. item_value (166 rows >= 1000), class_info.level_up_exp
 *          (237), npc_templates.experience (85). Survives in en-US via idKey_'s comma
 *          strip, but breaks Task 2's numeric validation, and in a non-US locale lands
 *          as text in an INT column.
 *       3. Formula re-interpretation. No value starts with '=' today, but
 *          spell_effects.hp_change_formula has 50 starting with '-'; one leading '='
 *          turns data into a formula.
 *       4. Bool. All 30 are CHAR(1); formatting one as a checkbox yields "TRUE".
 *     Spot-check a decimal column, a >=1000 integer, and a Bool against real values.
 *   Size: fine, but note the outlier — NPC Spawns is 4,322 x 4, 6.7x the rows of any
 *     other sheet (next: Items 649 x 46, Spell Effects 259 x 76). Row count is what
 *     drives the per-row duplicate scan, the readSheetIndex loop and client rendering.
 *     Still inside the 6-minute and payload limits for the 21-sheet publish check.
 *
 * readSheetIndex
 *   Checked: the lastRow < 2 guard; blank-id rows are skipped. The name column is NOT
 *     always B — Items and NPCs, the two most-referenced FK targets, hold an enum in B
 *     — so the caller passes nameColumnIndex from GOOSE_SCHEMA.
 *   Live: an FK picker onto Items shows item_name, not "Armor"/"Weapon" repeated; a
 *     picker onto a six-default sheet (Maps, Spells, Quests, ...) still works with the
 *     argument omitted.
 *
 * writeRow  (all guards are reasoning, never executed)
 *   Live, each one its own test:
 *     - append to a sheet trimmed to exactly its data (exercises insertRowsAfter)
 *     - overwrite with a cells array NARROWER than the header (must throw, not
 *       half-write leaving the previous record's trailing values in place)
 *     - a stray value BELOW row 1 past the last schema column (must still write fine —
 *       width comes from the header row, not the sheet's data extent)
 *     - a stray value IN row 1 past the last schema column (must throw: it inflates the
 *       header width, and the error says to clear that cell)
 *     - writeRow with a blank or whitespace-only id (must skip the duplicate check, not
 *       match every blank id cell in the column)
 *     - the same id from two tabs (must throw, naming the row that already has it)
 *     - rowNumber = 1 AND rowNumber = "1" (both must refuse; google.script.run does not
 *       preserve types, so the string form is the realistic one)
 *     - rowNumber past the end of the sheet (must throw, not insert thousands of rows)
 *     - one of the nine no-pk sheets with idColumnIndex = -1: two rows sharing column A
 *       (e.g. two drops for one NPC) must both write
 *     - an id column with a numeric cell format, e.g. displaying "651.00" (idKey_
 *       must still catch the collision with 651)
 *   Residual risk, accepted per plan scope: the duplicate check and the write are NOT
 *     atomic. Two editors inside the same window can still collide. No LockService.
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
 * Used by Editor.html to inline the built modules. `name` normally comes from one of
 * our own scriptlets, but every global here is reachable via google.script.run and
 * doGet is deployed ANYONE_WITH_GOOGLE_ACCOUNT, so treat it as caller-supplied: it can
 * name any HTML file in the script project. Exposure is nil — it returns only this
 * project's own HTML, which is already served to that user — so no guard, but keep it
 * that way and keep callers to the dist/*.html modules.
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
 * columns are benign and must not break writes. Row 1 is the header the importer skips
 * (CsvToSqlBase.cs:28, `.Rows().Skip(1)`), so it is the only correct width reference.
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
 * through here, so 651, "651", "651.00", "1,024" and " 651 " all collapse to the same
 * key. Non-numeric ids fall back to a trimmed string.
 *
 * (Every id column in the schema is INTEGER/INT/SMALLINT, with live maxima in the
 * hundreds — item_templates 651, spell_effects 259 — so the hazard here is cell
 * formatting, not integer width.)
 */
function idKey_(value) {
  if (value === null || value === undefined) return '';
  // Strip thousands separators — a formatted id column displays 1024 as "1,024", which
  // Number() reads as NaN. Safe here: every id column is INTEGER/INT/SMALLINT.
  var text = String(value).trim().replace(/,/g, '');
  if (text === '') return '';
  var num = Number(text);
  return isNaN(num) ? text : String(num);
}

/**
 * Reads a whole worksheet. Returns the header row separately from the data rows so the
 * client can map positionally — the importer reads cells by index (CsvToSqlBase.cs:35),
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

/**
 * Reads the id and label columns of a sheet, for FK pickers.
 *
 * The id is always column A. The label is NOT always column B: of the eight FK target
 * sheets, the two most referenced put an enum there and the name one further along —
 * Items has item_usetype in B and item_name at index 2 (5 inbound refs), NPCs has
 * npc_type in B and npc_name at index 2 (3 refs). Hardcoding column B labels all 649
 * Items entries "Armor"/"Weapon"/"NoUse", which is useless for picking.
 *
 * So the caller passes `nameColumnIndex`, a 0-based index into the row, taken from
 * GOOSE_SCHEMA (which lives client-side and is not reachable from here). It defaults to
 * 1 for the six sheets where column B is the name. Returns {id, name} either way, so
 * only the argument changes for callers.
 */
function readSheetIndex(sheetName, nameColumnIndex) {
  var sheet = requireSheet_(sheetName);

  var nameIndex = typeof nameColumnIndex === 'number' && nameColumnIndex >= 1
    ? Math.floor(nameColumnIndex)
    : 1;

  var lastRow = sheet.getLastRow();
  if (lastRow < 2) return { sheet: sheetName, entries: [] };

  // Read out to the name column. nameIndex comes from GOOSE_SCHEMA, so it is always inside
  // the sheet's real width; a span past the grid would throw rather than yield undefined.
  var span = Math.max(2, nameIndex + 1);
  var values = sheet.getRange(2, 1, lastRow - 1, span).getDisplayValues();
  var entries = [];
  for (var i = 0; i < values.length; i++) {
    // "Is this id absent" is idKey_'s question, not isBlank_'s — a whitespace-only cell would
    // otherwise enter the picker and become a phantom id 0 in the client's FK validation set.
    if (idKey_(values[i][0]) === '') continue;
    entries.push({ id: values[i][0], name: values[i][nameIndex] });
  }

  return { sheet: sheetName, entries: entries };
}

/**
 * Writes one record. `cells` is an array aligned to column order; null, undefined or ''
 * entries are written as empty, which the importer treats as "use the SQL default"
 * (CsvToSqlBase.cs:36). rowNumber is 1-based including the header; pass 0 to append.
 * It is normalised with Number() first: google.script.run forwards a string straight
 * through, and a string row would defeat every strict comparison below — including the
 * one guarding the header.
 *
 * `idColumnIndex` is 0-based and gates the duplicate-id check, the only integrity check
 * in this file. Pass the index of the sheet's primary-key column, or -1 to disable the
 * check. Twelve sheets have a single pk at index 0; the other nine (NPC Spawns, NPC
 * Drops, NPC Vendor Items, Warptiles, Map Required Items, Combination Item Required,
 * Combination Item Result, Class Info, Class Levelup Spells) have NO pk and MUST pass
 * -1 — their column A is an Id-kind FK that legitimately repeats, so defaulting to 0
 * would reject every second drop, spawn or level row.
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

  // A sheet with no header has no column-order contract to write against, and nothing
  // bounds the write to the grid's width. Refuse rather than guess. (Schema sheets
  // always carry a header; reaching this means the wrong spreadsheet is open.)
  var width = headerWidth_(sheet);
  if (width === 0) {
    throw new Error('writeRow: sheet "' + sheetName + '" has no header row — nothing to write against');
  }
  if (cells.length !== width) {
    throw new Error(
      sheetName + ': got ' + cells.length + ' values for a header ' + width +
      ' columns wide. Check for a stray value in row 1 past the last real column; ' +
      'if row 1 is correct, the schema is out of date — re-run SchemaGen.');
  }

  // google.script.run does not preserve types, so a client that read the row off a DOM
  // attribute sends "1", and every strict test below — including the header guard —
  // would pass it through. Normalise once, here.
  //
  // Reject empty-ish values rather than letting Number() fold them to 0: "", null, false and
  // [] all coerce to 0, which is the append sentinel, so a client reading a missing DOM
  // attribute would silently append a duplicate instead of editing the row it meant.
  if (typeof rowNumber !== 'number' &&
      !(typeof rowNumber === 'string' && rowNumber.trim() !== '')) {
    throw new Error('writeRow: invalid row ' + rowNumber);
  }
  var requestedRow = Number(rowNumber);
  if (!isFinite(requestedRow) || Math.floor(requestedRow) !== requestedRow || requestedRow < 0) {
    throw new Error('writeRow: invalid row ' + rowNumber);
  }
  // Row 1 is the header the importer reads column order from; never overwrite it.
  if (requestedRow === 1) throw new Error('writeRow: refusing to overwrite the header row');

  var lastRow = sheet.getLastRow();

  // A stale client holding the row of a record another editor deleted would otherwise
  // insert thousands of blank rows and land the write nowhere near the intended record.
  // Nine sheets have no pk, so the duplicate scan below cannot catch that for them.
  if (requestedRow > lastRow + 1) {
    throw new Error(
      sheetName + ': row ' + requestedRow + ' is past the end of the sheet (' + lastRow +
      ' rows) — reload and retry');
  }

  var target = requestedRow > 0 ? requestedRow : lastRow + 1;

  // idKey_, not isBlank_: isBlank_ answers "write this cell empty", which is a different
  // question from "is an id present". A whitespace-only id passes isBlank_ but keys to
  // '', which then matches every blank id cell in the column.
  //
  // The typeof test is not decoration: `null >= 0` is true in JS, so a client passing
  // null would silently check column A. undefined and NaN already fall through.
  var idIndex = typeof idColumnIndex === 'number' && idColumnIndex >= 0 && idColumnIndex < cells.length
    ? Math.floor(idColumnIndex)
    : -1;
  var newId = idIndex >= 0 ? idKey_(cells[idIndex]) : '';

  if (newId !== '' && lastRow >= 2) {
    var ids = sheet.getRange(2, idIndex + 1, lastRow - 1, 1).getValues();
    for (var i = 0; i < ids.length; i++) {
      if (idKey_(ids[i][0]) === newId && (i + 2) !== target) {
        throw new Error(sheetName + ': id ' + newId + ' is already used by row ' + (i + 2));
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
