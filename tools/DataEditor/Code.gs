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
 *   Checked: reads RAW values through cellText_, not display text. What that fixes, and
 *     what each item was when this read getDisplayValues() and a save rewrote every cell
 *     of the record from the text the client was holding:
 *       1. Decimal precision loss. 32 DECIMAL(9,4)/(5,4)/(5,2) columns across 9 sheets
 *          (verified against schema.js), with live values at 3dp
 *          (npc_templates.hp_percent_regen, spell_effects.spell_crit / spell_damage /
 *          damage_reduce). A rate column displayed to 2dp lost the other digits, and a
 *          designer setting a column to 2dp is an ordinary thing to do. Raw values carry
 *          full stored precision.
 *       2. Thousands separators. item_value (166 rows >= 1000), class_info.level_up_exp
 *          (237), npc_templates.experience (85) came back as "1,500" — survivable in
 *          en-US via idKey_'s comma strip, text in an INT column anywhere else. A raw
 *          number has no separators.
 *       3. Formula re-interpretation, in both directions. Reading raw gives a formula's
 *          RESULT, and writeRow's changed-cells-only write then leaves the formula
 *          untouched; and Validation refuses a Text value the user has begun with '='
 *          rather than storing data as a formula. (No value starts with '=' today;
 *          spell_effects.hp_change_formula has 50 starting with '-', which is fine.)
 *       4. Bool. All 30 are CHAR(1). A checkbox cell now reads back 'true' and is
 *          reported as "must be 0 or 1" instead of being written back as "TRUE".
 *   Checked: ONE read of the sheet, not two. The display values exist for Date cells alone, so they
 *     are fetched only when the raw scan finds one — which the shipped data never does. Same
 *     isDate_ on both sides, so the values a cell needs are the values that were read.
 *   Live: spot-check a DECIMAL column displayed to fewer places than it stores, an
 *     integer >= 1000 in a separator format, a Bool, and a formula cell — open the record,
 *     save it UNCHANGED, and confirm every cell is byte-for-byte what it was. That last
 *     one is the whole contract: a save must not alter a cell the user did not edit.
 *   Live: format a cell as a date and confirm the record still shows the sheet's own rendering of
 *     it — that is the one path where the display pass is still fetched.
 *   Size: fine, but note the outlier — NPC Spawns is 4,322 x 4, 6.7x the rows of any
 *     other sheet (next: Items 649 x 46, Spell Effects 259 x 76). Row count is what
 *     drives the per-row duplicate scan, the readSheetIndex loop and client rendering.
 *     Still inside the 6-minute and payload limits for the 21-sheet publish check.
 *
 * readSheetIndex
 *   Checked: the lastRow < 2 guard; blank-id rows are skipped. The name column is NOT
 *     always B — Items and NPCs, the two most-referenced FK targets, hold an enum in B
 *     — so the caller passes nameColumnIndex from GOOSE_SCHEMA. Ids (and the optional
 *     extra column) are RAW values through cellText_, matching readSheet: the client
 *     does Number(entry.id), so a display value like "1,024" would poison the FK set
 *     and block saves. Only the label is display text.
 *   Live: an FK picker onto Items shows item_name, not "Armor"/"Weapon" repeated; a
 *     picker onto a six-default sheet (Maps, Spells, Quests, ...) still works with the
 *     argument omitted; format an FK target's id column with a separator and confirm
 *     a referencing record still saves.
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
 *     - EDIT a field other than the id on a pk sheet with a loaded snapshot, then RENUMBER a row
 *       onto an id another row holds: the first must not read the id column at all, the second must
 *       still be refused
 *     - append a record whose id another row already has WITH a snapshot in the request (the
 *       snapshot is null for an append, so the scan must still run)
 *     - rowNumber = 1 AND rowNumber = "1" (both must refuse; google.script.run does not
 *       preserve types, so the string form is the realistic one)
 *     - rowNumber past the end of the sheet (must throw, not insert thousands of rows)
 *     - one of the nine no-pk sheets with idColumnIndex = -1: two rows sharing column A
 *       (e.g. two drops for one NPC) must both write
 *     - an id column with a numeric cell format, e.g. displaying "651.00" (idKey_
 *       must still catch the collision with 651)
 *     - save an UNCHANGED record and confirm nothing was written: a formula cell still
 *       holds its formula, a 4dp value displayed to 2dp still holds 4dp
 *     - edit ONE field of a record whose neighbours hold a formula and a separator-
 *       formatted integer: only that field's cell may change
 *     - clear a field that had a value (a blank must still overwrite, not be skipped)
 *     - edit two ADJACENT fields and two SEPARATED fields (the run-grouping above writes
 *       one range for the first and two for the second; both must land)
 *     - two editors on one row: A opens it, B edits cell X, A edits cell Y and saves —
 *       X must keep B's value and Y must land (the loaded-snapshot merge)
 *     - two editors on one CELL: B edits X, A also edits X and saves — A must be refused
 *       with the message naming the column, and nothing else on the row written
 *     - a Text cell edited to "1-2" and to "01" (must store the text, not a Date or a 1 —
 *       the '@' format pin)
 *   Residual risk, accepted per plan scope: the check and the write are not a transaction. A
 *     save that fails part-way through its own writes leaves the earlier ones standing, and
 *     nothing rolls them back. Two editors no longer interleave — every write path in this
 *     file holds the document lock (withDocumentLock_) — so an id can no longer be taken
 *     twice inside the check-then-write window. The loaded-snapshot merge still narrows a
 *     genuine two-editor conflict to the cells both sides edited.
 * ---------------------------------------------------------------------------------
 */

/**
 * How long a save waits for the document lock. Generous: the alternative to waiting is telling
 * a user their save failed because someone else was mid-save, and a save is milliseconds of
 * sheet work. Well inside the 6-minute execution limit.
 */
var LOCK_TIMEOUT_MS = 30000;

/**
 * Internal: runs fn holding the document lock, and releases it whatever fn does.
 *
 * A DOCUMENT lock, not a script lock: the editor is container-bound, so the thing two callers
 * contend for is this spreadsheet. waitLock THROWS on timeout — it does not return false, which
 * is tryLock — so a caller that reaches fn is holding the lock.
 *
 * This closes the check-then-write window the header used to list as accepted risk. It is not a
 * transaction: fn can still fail part-way through its own writes, and nothing rolls those back.
 * What it buys is that no two saves interleave.
 */
function withDocumentLock_(fn) {
  var lock = LockService.getDocumentLock();
  lock.waitLock(LOCK_TIMEOUT_MS);
  try {
    return fn();
  } finally {
    lock.releaseLock();
  }
}

/**
 * Internal: which cells of a row a save must write, and which columns another editor changed
 * underneath it. Reads nothing and writes nothing — the caller supplies the row as it currently
 * stands, which is what lets a batch plan every row of every sheet from one read before it
 * touches anything.
 *
 * `currentRaw` / `currentShown` are the row's cells as getValues / getDisplayValues give them;
 * `out` is the posted record with blanks already folded to ''; `loaded` is the record AS THE
 * CLIENT READ IT, or null for a caller that has no snapshot. `currentShown` may itself be null
 * ONLY when the caller knows the row holds no Date cells: with it null a Date cell reads as the
 * empty string (cellText_ gets '' as its display text), so the cell always looks changed — a
 * spurious write without `loaded`, and with `loaded` a spurious conflict that refuses the whole
 * save. Any caller whose row can hold a Date must pass display values.
 *
 * A save posts the WHOLE record, every column, whether the user edited it or not — so writing
 * the whole row unconditionally meant every cell was rewritten from the text the client happened
 * to be holding. cellText_ removed the formatting half of that; this removes the rest, by never
 * touching a cell whose value has not changed:
 *   - a FORMULA cell keeps its formula. getValues() gives its RESULT, so the client can only ever
 *     post the result back; comparing result to result finds them equal and the cell is left
 *     alone. (Editing that field still replaces the formula, which is the one case where that is
 *     what the user asked for.)
 *   - a text cell holding something the sheet would re-parse on entry — "01", "-50", anything
 *     Sheets reads as a number — keeps exactly what it has.
 * A blank incoming cell still CLEARS a non-blank one: out[] has already folded null to '', so
 * "the user emptied this field" compares unequal and is written.
 *
 * The three-way rule, unchanged from where it grew up inside writeRow:
 *   - a cell where posted equals loaded was never edited and is NEVER written, so a concurrent
 *     edit to it survives instead of being reverted to the stale copy;
 *   - a cell that already holds what the user wants needs no write;
 *   - a cell the user DID edit whose current value matches neither loaded nor posted was also
 *     changed by someone else — a conflict, reported rather than overwritten.
 * Without `loaded` it degrades to "write what differs", which is last-writer-wins per cell.
 *
 * Returns { writeAt: number[], conflicts: number[] }, both 0-based column indexes, ascending.
 */
function planRowWrite_(currentRaw, currentShown, out, loaded, width) {
  var writeAt = [];
  var conflicts = [];

  for (var c = 0; c < width; c++) {
    var current = cellText_(currentRaw[c], currentShown ? currentShown[c] : '');
    var posted = String(out[c]);

    if (loaded) {
      var was = isBlank_(loaded[c]) ? '' : String(loaded[c]);
      if (posted === was) continue;
      if (current === posted) continue;
      if (current !== was) { conflicts.push(c); continue; }
    } else if (current === posted) {
      continue;
    }

    writeAt.push(c);
  }

  return { writeAt: writeAt, conflicts: conflicts };
}

/**
 * Internal: writes the planned cells of one row as contiguous runs, so an ordinary edit is one
 * setValues call rather than one per column. Pins '@' on a Text cell BEFORE writing it —
 * setValues parses strings like typed entry, so "1-2" in a description becomes a Date and "01"
 * becomes 1.
 *
 * `textColumns` is a 0-based index -> true map, not an array: it is consulted per written cell.
 */
function writeRuns_(sheet, target, out, writeAt, textColumns) {
  var runs = [];
  writeAt.forEach(function (c) {
    var open = runs.length ? runs[runs.length - 1] : null;
    if (open && open.at + open.values.length === c) open.values.push(out[c]);
    else runs.push({ at: c, values: [out[c]] });
  });

  runs.forEach(function (run) {
    run.values.forEach(function (value, k) {
      if (textColumns[run.at + k] && !isBlank_(value)) {
        sheet.getRange(target, run.at + k + 1).setNumberFormat('@');
      }
    });
    sheet.getRange(target, run.at + 1, 1, run.values.length).setValues([run.values]);
  });
}

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
 * Internal: is this cell a Date?
 *
 * Not `value instanceof Date`: that answers "was this made by MY realm's Date", which is false for
 * a Date that crossed a context boundary — exactly the case it would be asked about. The tag test
 * asks what the value IS.
 *
 * ONE definition, shared by cellText_ (which needs the display value for a Date and for nothing
 * else) and by readSheet (which decides whether to pay for the display values at all). They cannot
 * drift: if this said no where cellText_ said yes, readSheet would hand it a display value it never
 * read.
 */
function isDate_(value) {
  return Object.prototype.toString.call(value) === '[object Date]';
}

/**
 * Internal: a cell's value as the text the client edits. THE one definition of that —
 * readSheet hands these to the client and writeRow compares against them to decide what
 * changed, so a cell the user did not touch round-trips to itself by construction.
 *
 * RAW value, not the display value. getDisplayValues() returns the cell as FORMATTED,
 * which the client then posts back as the new value, so a whole-row save silently
 * rewrote every cell as its own formatting:
 *   - a DECIMAL(9,4) shown to 2dp lost the other two digits (32 decimal columns across
 *     9 sheets, with live values at 3dp)
 *   - item_value 1500 came back as "1,500" — text in an INT column outside en-US
 *   - a Bool formatted as a checkbox came back as "TRUE"
 * The raw value has none of those: a number is a number, and String() on it is the full
 * stored precision with no separators.
 *
 * The ONE case where the display value is closer is a Date, which no column in the schema
 * is (every sql type is INT/BIGINT/SMALLINT/INTEGER/DECIMAL/CHAR(1)/VARCHAR/TEXT — checked
 * against schema.js) and which String() would render as "Mon Jan 01 1900 ...". A
 * date-formatted cell in a numeric column is already broken data; showing the user what
 * the sheet shows them keeps it recognisable, and validation reports it either way.
 *
 * A boolean cell (a real checkbox) reads back as 'true'/'false' and Validation rejects it
 * with "must be 0 or 1", which is the same outcome the display value's "TRUE" produced —
 * deliberately NOT coerced to 0/1 here, because inventing a value for a cell whose type is
 * wrong is how the formatting bug worked in the first place.
 *
 * `display` is therefore only ever READ for a Date, which is why readSheet is free not to fetch the
 * display values at all when the sheet holds none — both sides ask isDate_.
 */
function cellText_(raw, display) {
  if (raw === null || raw === undefined || raw === '') return '';
  if (isDate_(raw)) return String(display);
  return String(raw);
}

/**
 * Internal: a comparable key for an id cell.
 *
 * The client sees ids as strings and sends them back as strings, while the sheet holds
 * them as numbers, so a raw comparison compares a string with a number and misses every
 * collision. Both sides go through here, so 651, "651", "651.00", "1,024" and " 651 " all
 * collapse to the same key. Non-numeric ids fall back to a trimmed string.
 *
 * Still needed now that readSheet returns RAW values rather than display text: the client
 * may hold an id it never read from the sheet at all (Validation.nextId's suggestion, or a
 * hand-typed one), and the duplicate scan below reads the id column with getValues, so the
 * two sides are a string and a number even when no cell format is involved.
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

  // ONE PASS OVER THE SHEET, normally. cellText_ reads the display value for a Date cell and for
  // nothing else, and no column in the schema is a date (every sql type is
  // INT/BIGINT/SMALLINT/INTEGER/DECIMAL/CHAR(1)/VARCHAR/TEXT — checked against schema.js), so the
  // second whole-sheet read was paid on every sheet open for a case that does not occur in the
  // shipped data. It is not GONE: a stray date-formatted cell still gets its display text, because
  // the raw values are scanned for the Date tag first and the display pass is fetched if one turns
  // up. That scan is a local type test per cell, against a second round trip carrying the whole
  // sheet again (Items is 649 x 46; NPC Spawns 4,322 x 4).
  //
  // Both reads are of ONE range, as before — they cannot disagree about extent.
  var range = sheet.getDataRange();
  var raw = range.getValues();

  var shown = null;
  for (var r = 0; r < raw.length && !shown; r++) {
    for (var c = 0; c < raw[r].length; c++) {
      if (isDate_(raw[r][c])) { shown = range.getDisplayValues(); break; }
    }
  }

  var values = raw.map(function (row, i) {
    return row.map(function (cell, j) { return cellText_(cell, shown ? shown[i][j] : ''); });
  });

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
 *
 * The ID IS THE RAW VALUE, through cellText_, for the same reason readSheet is: the client
 * builds its FK validation set with Number(entry.id), so a thousands-separator format on the
 * id column would ship "1,024", read as NaN, and every hand-typed 1024 in a referencing sheet
 * would be refused as "does not exist" — validation here fails CLOSED, so a formatted column
 * blocks saves. The NAME stays display text: it labels the picker, nothing parses it, and for
 * a label what the sheet shows is what the user recognises.
 *
 * `extraColumnIndex` (0-based, optional) adds one more RAW cell per entry as `extra`. One
 * caller today: the Spells preview needs spell_effect_id resolved to the Spell Effects row's
 * spell_animation — the effects atlas is keyed by animation id, not by effect row id — and
 * this list is the only per-row data the client holds for a referenced sheet.
 */
function readSheetIndex(sheetName, nameColumnIndex, extraColumnIndex) {
  var sheet = requireSheet_(sheetName);

  var nameIndex = typeof nameColumnIndex === 'number' && nameColumnIndex >= 1
    ? Math.floor(nameColumnIndex)
    : 1;
  var extraIndex = typeof extraColumnIndex === 'number' && extraColumnIndex >= 0
    ? Math.floor(extraColumnIndex)
    : -1;

  var lastRow = sheet.getLastRow();
  if (lastRow < 2) return { sheet: sheetName, entries: [] };

  // Read out to the widest column asked for. Both indexes come from GOOSE_SCHEMA, so they are
  // always inside the sheet's real width; a span past the grid would throw rather than yield
  // undefined.
  var span = Math.max(2, nameIndex + 1, extraIndex + 1);
  var range = sheet.getRange(2, 1, lastRow - 1, span);
  var raw = range.getValues();
  var shown = range.getDisplayValues();

  var entries = [];
  for (var i = 0; i < raw.length; i++) {
    var id = cellText_(raw[i][0], shown[i][0]);
    // "Is this id absent" is idKey_'s question, not isBlank_'s — a whitespace-only cell would
    // otherwise enter the picker and become a phantom id 0 in the client's FK validation set.
    if (idKey_(id) === '') continue;
    var entry = { id: id, name: shown[i][nameIndex] };
    if (extraIndex >= 0) entry.extra = cellText_(raw[i][extraIndex], shown[i][extraIndex]);
    entries.push(entry);
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
 * at the same time cannot both take the same suggested id: the scan and the write run under
 * one document lock (withDocumentLock_), so nobody else's write lands inside the
 * check-then-write window. What remains is that the write itself is not a transaction — a
 * save that fails part-way through its own writes leaves the earlier cells standing, and
 * nothing rolls them back. The check is skipped when the posted id equals the one in
 * `options.loaded` for a row that already exists — that save cannot take an id off anybody — so
 * an ordinary field edit no longer reads the whole id column. See the scan itself for why the
 * sheet's current id does not come into it.
 *
 * `options.loaded` is the record AS THE CLIENT READ IT, same width and encoding as `cells`.
 * With it, "what changed" is decided against what the USER saw, not against the sheet's
 * current values — the difference is every cell another editor touched during the edit:
 *   - a cell where posted equals loaded was never edited and is NEVER written, so a
 *     concurrent edit to it survives instead of being silently reverted to the stale copy;
 *   - a cell the user DID edit whose current value matches neither loaded nor posted was
 *     ALSO changed by someone else — refused, naming the columns, before anything is
 *     written. (On the nine no-pk sheets this is additionally what catches a row that
 *     shifted under a stale rowNumber: the current row matches nothing the client loaded.)
 * Without options.loaded the old current-value diff still runs, so a caller that predates
 * the snapshot degrades to "last writer wins per cell" rather than breaking.
 *
 * `options.textColumns` lists the 0-based indexes of the schema's Text columns. A written
 * cell in one of them gets its number format pinned to '@' (plain text) first, because
 * setValues parses strings like typed entry: "1-2" in a description column became a Date,
 * "01" became 1. Only cells actually being written are touched — pinning unwritten cells
 * would alter formatting the user never asked about.
 */
function writeRow(sheetName, rowNumber, cells, idColumnIndex, options) {
  return withDocumentLock_(function () {
    return writeRowLocked_(sheetName, rowNumber, cells, idColumnIndex, options);
  });
}

function writeRowLocked_(sheetName, rowNumber, cells, idColumnIndex, options) {
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

  var opts = options || {};
  var loaded = Array.isArray(opts.loaded) ? opts.loaded : null;
  if (loaded && loaded.length !== width) {
    throw new Error(
      sheetName + ': the loaded snapshot is ' + loaded.length + ' values wide for a header ' +
      width + ' columns wide — reload and retry');
  }
  var textColumns = {};
  (Array.isArray(opts.textColumns) ? opts.textColumns : []).forEach(function (i) {
    if (typeof i === 'number' && i >= 0 && i < width) textColumns[Math.floor(i)] = true;
  });

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

  // NOT WHEN THE ID DID NOT MOVE. The scan reads the whole id column — 4,322 cells on NPC Spawns,
  // 649 on Items — and it exists to catch an id being TAKEN: a new record, or a record renumbered
  // onto one another row already has. Editing any other field of an existing row cannot introduce a
  // collision, so re-reading the column to prove it is a round trip per save that can only ever
  // answer "no".
  //
  // The three conditions are each load-bearing:
  //   - `loaded`, because without the snapshot there is nothing to compare the id against. A caller
  //     that predates it keeps the scan.
  //   - target <= lastRow, so an APPEND is always scanned. An append takes a row nobody held.
  //   - the ids matching by idKey_, the same folding the scan itself uses ('651' vs 651 vs '651.00').
  // What it does NOT check is the id in the SHEET at that row: if another editor renumbered the row
  // under us, the id cell is one this save posts unchanged and therefore never writes, so it still
  // introduces no collision. A duplicate that was already in the column stays there either way —
  // this save did not make it, and the publish check is what reports it.
  var idUnchanged = loaded && idIndex >= 0 && target <= lastRow &&
                    idKey_(loaded[idIndex]) === newId;

  if (newId !== '' && lastRow >= 2 && !idUnchanged) {
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

  // Which cells to write is planRowWrite_'s rule — see there. Only for a row that already
  // exists; an append has nothing to compare against. The whole row is planned before anything
  // is written, because a conflict must refuse the entire save, not land half of it first.
  if (target <= lastRow) {
    var before = sheet.getRange(target, 1, 1, width);
    var plan = planRowWrite_(before.getValues()[0], before.getDisplayValues()[0], out, loaded, width);

    if (plan.conflicts.length) {
      var header = sheet.getRange(1, 1, 1, width).getDisplayValues()[0];
      var names = plan.conflicts.map(function (c) { return String(header[c]); });
      throw new Error(
        sheetName + ' row ' + target + ': ' + names.join(', ') + ' changed in the sheet while ' +
        'you were editing — nothing was written. Reload the record and re-apply your edit.');
    }

    writeRuns_(sheet, target, out, plan.writeAt, textColumns);
  } else {
    for (var t = 0; t < out.length; t++) {
      if (textColumns[t] && !isBlank_(out[t])) {
        sheet.getRange(target, t + 1).setNumberFormat('@');
      }
    }
    sheet.getRange(target, 1, 1, out.length).setValues([out]);
  }

  SpreadsheetApp.flush();

  return { row: target };
}

/**
 * Saves several sheets' worth of edits, appends and deletions as one operation.
 *
 * `ops` is an array of per-sheet entries, in the order they should be APPLIED — put a parent
 * sheet before its children, so a batch that fails part-way leaves an incomplete parent (which
 * a retry completes) rather than children pointing at a row that was never written:
 *
 *   [{ sheet, idColumnIndex, textColumns, writes, appends, deletes }, ...]
 *
 *   writes:  [{ row, cells, loaded }]   row is 1-based including the header, as writeRow's is
 *   appends: [{ cells }]
 *   deletes: [{ row, loaded }]
 *
 * `cells` and `loaded` are arrays exactly as wide as that sheet's header, same encoding as
 * writeRow's. `idColumnIndex` is 0-based, or -1 for the nine sheets with no primary key.
 *
 * EVERY SHEET IS PLANNED BEFORE ANY SHEET IS WRITTEN. That is the whole point of the call: a
 * stale row in the last entry refuses the first entry's writes too, so an NPC and its drops
 * cannot half-save against a sheet that moved. All problems across all sheets are collected and
 * reported together, so a user fixes them in one pass rather than one error at a time.
 *
 * NOT A TRANSACTION. Once the checks pass, the writes go out sheet by sheet, and an Apps Script
 * failure part-way through leaves the earlier ones standing. The document lock means no other
 * save interleaves; it does not mean this one is atomic. See the header's residual-risk note.
 */
function saveBatch(ops) {
  if (!Array.isArray(ops) || ops.length === 0) {
    throw new Error('saveBatch: ops must be a non-empty array');
  }

  var seen = {};
  ops.forEach(function (entry) {
    var name = String(entry && entry.sheet);
    if (Object.prototype.hasOwnProperty.call(seen, name)) {
      throw new Error('saveBatch: sheet "' + name + '" appears twice in one batch');
    }
    seen[name] = true;
  });

  return withDocumentLock_(function () {
    var plans = ops.map(planSheetOps_);

    var problems = [];
    plans.forEach(function (plan) { problems = problems.concat(plan.problems); });
    if (problems.length) throw new Error(problems.join('\n'));

    return applyPlans_(plans);
  });
}

/**
 * Internal: one sheet's ops checked against the sheet as it currently stands. READS ONLY.
 *
 * Returns everything applying needs — the resolved sheet, the header width, the folded cells,
 * the per-row write plans — plus `problems`, the human-readable refusals. A shape error that
 * makes planning impossible (no header, wrong-width array) throws immediately; a data
 * disagreement (a conflict, a duplicate id) goes into `problems` so the caller can report every
 * one of them at once.
 */
function planSheetOps_(entry) {
  var sheetName = String(entry.sheet);
  var sheet = requireSheet_(sheetName);

  var width = headerWidth_(sheet);
  if (width === 0) {
    throw new Error('saveBatch: sheet "' + sheetName + '" has no header row — nothing to write against');
  }

  var writes = Array.isArray(entry.writes) ? entry.writes : [];
  var appends = Array.isArray(entry.appends) ? entry.appends : [];
  var deletes = Array.isArray(entry.deletes) ? entry.deletes : [];

  var textColumns = {};
  (Array.isArray(entry.textColumns) ? entry.textColumns : []).forEach(function (i) {
    if (typeof i === 'number' && i >= 0 && i < width) textColumns[Math.floor(i)] = true;
  });

  var idIndex = typeof entry.idColumnIndex === 'number' &&
                entry.idColumnIndex >= 0 && entry.idColumnIndex < width
    ? Math.floor(entry.idColumnIndex)
    : -1;

  var lastRow = sheet.getLastRow();

  // ONE read of the sheet, with the display values fetched only if a Date turns up — the same
  // trade readSheet makes (see readSheet's comment above), and for the same reason: cellText_
  // reads `display` for a Date and for nothing else, and no column in the schema is a date.
  var raw = [];
  var shown = null;
  if (lastRow >= 1) {
    var range = sheet.getDataRange();
    raw = range.getValues();
    for (var r = 0; r < raw.length && !shown; r++) {
      for (var c = 0; c < raw[r].length; c++) {
        if (isDate_(raw[r][c])) { shown = range.getDisplayValues(); break; }
      }
    }
  }

  var problems = [];
  var claimed = {};

  function fold(cells, what) {
    if (!Array.isArray(cells) || cells.length !== width) {
      throw new Error(
        sheetName + ': got ' + (Array.isArray(cells) ? cells.length : 'no') +
        ' values for a header ' + width + ' columns wide (' + what + ')');
    }
    return cells.map(function (cell) { return isBlank_(cell) ? '' : cell; });
  }

  function targetRow(row, what) {
    if (typeof row !== 'number' && !(typeof row === 'string' && String(row).trim() !== '')) {
      throw new Error(sheetName + ': invalid row ' + row + ' (' + what + ')');
    }
    var n = Number(row);
    if (!isFinite(n) || Math.floor(n) !== n || n < 0) {
      throw new Error(sheetName + ': invalid row ' + row + ' (' + what + ')');
    }
    if (n === 1) throw new Error(sheetName + ': refusing to touch the header row');
    if (n > lastRow) {
      throw new Error(
        sheetName + ': row ' + n + ' is past the end of the sheet (' + lastRow +
        ' rows) — reload and retry');
    }
    if (Object.prototype.hasOwnProperty.call(claimed, String(n))) {
      problems.push(sheetName + ': row ' + n + ' appears in more than one operation');
    }
    claimed[String(n)] = what;
    return n;
  }

  var header = sheet.getRange(1, 1, 1, width).getDisplayValues()[0];

  // WRITES. Planned, not applied — planRowWrite_ takes the row as it stands and answers which
  // cells to write, so nothing here touches the sheet.
  var plannedWrites = [];
  writes.forEach(function (w) {
    var row = targetRow(w.row, 'write');
    var out = fold(w.cells, 'write row ' + row);
    var loaded = Array.isArray(w.loaded) ? fold(w.loaded, 'write row ' + row + ' snapshot') : null;

    var currentRaw = raw[row - 1] || [];
    var currentShown = shown ? shown[row - 1] : null;
    var plan = planRowWrite_(currentRaw, currentShown, out, loaded, width);

    if (plan.conflicts.length) {
      var names = plan.conflicts.map(function (c) { return String(header[c]); });
      problems.push(
        sheetName + ' row ' + row + ': ' + names.join(', ') + ' changed in the sheet while you ' +
        'were editing — nothing was written. Reload and re-apply your edit.');
      return;
    }

    // A row whose every cell already agrees is not an operation. Dropping it here keeps the
    // returned count honest and saves a setValues call per untouched row.
    if (plan.writeAt.length) {
      plannedWrites.push({ row: row, out: out, writeAt: plan.writeAt });
    }
  });

  // DELETES. Stricter than a write: the row must still be, cell for cell, what the client was
  // looking at. A write may leave alone the cells the user did not edit; there is no partial
  // version of removing a row, so anything that moved under it is a refusal.
  var plannedDeletes = [];
  deletes.forEach(function (d) {
    var row = targetRow(d.row, 'delete');
    var loaded = fold(d.loaded, 'delete row ' + row + ' snapshot');

    var currentRaw = raw[row - 1] || [];
    var currentShown = shown ? shown[row - 1] : null;
    var moved = [];
    for (var c = 0; c < width; c++) {
      var current = cellText_(currentRaw[c], currentShown ? currentShown[c] : '');
      if (current !== String(loaded[c])) moved.push(String(header[c]));
    }

    if (moved.length) {
      problems.push(
        sheetName + ' row ' + row + ': ' + moved.join(', ') + ' changed in the sheet while you ' +
        'were editing — the row was not deleted. Reload and re-apply your edit.');
      return;
    }

    plannedDeletes.push({ row: row });
  });

  var plannedAppends = appends.map(function (a, i) {
    return { out: fold(a.cells, 'new row ' + (i + 1)) };
  });

  if (idIndex >= 0) {
    checkBatchIds_(sheetName, idIndex, raw, lastRow, plannedWrites, plannedAppends,
                   plannedDeletes, writes, problems);
  }

  return {
    name: sheetName,
    sheet: sheet,
    width: width,
    textColumns: textColumns,
    writes: plannedWrites,
    appends: plannedAppends,
    deletes: plannedDeletes,
    problems: problems,
  };
}

/**
 * Internal: no id this batch CLAIMS may collide, checked against the sheet AS IT WILL BE.
 *
 * Two things this deliberately does NOT do, both inherited from writeRow:
 *   - a duplicate already sitting in the column that this batch does not touch is left alone.
 *     This save did not make it; the publish check is what reports it. So the untouched rows are
 *     seeded first and silently, and only a claimed id can be refused.
 *   - a write that does not MOVE its id claims nothing, so an ordinary field edit on a sheet
 *     that already has a duplicate is not refused for a collision it did not cause.
 * And one it does: a row being DELETED releases its id in the same batch, so replacing a record
 * in one call is not refused by the id it is itself giving up.
 */
function checkBatchIds_(sheetName, idIndex, raw, lastRow, plannedWrites, plannedAppends,
                        plannedDeletes, rawWrites, problems) {
  var deleted = {};
  plannedDeletes.forEach(function (d) { deleted[String(d.row)] = true; });

  // Every write's posted id, by row — including the ones planRowWrite_ dropped as no-ops, since
  // a row whose id did not move still HOLDS that id in the post-batch sheet.
  var posted = {};
  rawWrites.forEach(function (w) {
    var row = Number(w.row);
    if (Array.isArray(w.cells)) posted[String(row)] = idKey_(w.cells[idIndex]);
  });

  var byKey = {};
  var claims = [];

  for (var row = 2; row <= lastRow; row++) {
    if (deleted[String(row)]) continue;

    var currentKey = idKey_((raw[row - 1] || [])[idIndex]);
    var isPosted = Object.prototype.hasOwnProperty.call(posted, String(row));
    var key = isPosted ? posted[String(row)] : currentKey;
    if (key === '') continue;

    // A posted id equal to the one already there is not a claim — writeRow's idUnchanged rule.
    // Treat the row as untouched so a pre-existing duplicate is not blamed on it.
    if (isPosted && key !== currentKey) claims.push({ key: key, who: 'row ' + row });
    else if (!byKey[key]) byKey[key] = 'row ' + row;
  }

  plannedAppends.forEach(function (a, i) {
    var key = idKey_(a.out[idIndex]);
    if (key !== '') claims.push({ key: key, who: 'new row ' + (i + 1) });
  });

  claims.forEach(function (claim) {
    if (byKey[claim.key]) {
      problems.push(sheetName + ': id ' + claim.key + ' (' + claim.who + ') is already used by ' +
                    byKey[claim.key]);
    } else {
      byKey[claim.key] = claim.who;
    }
  });
}

/** Internal: applies the plans. Task 5 fills this in; planning must be provably separate. */
function applyPlans_(plans) {
  return plans.map(function (plan) {
    return { sheet: plan.name, written: 0, appended: 0, deleted: 0 };
  });
}
