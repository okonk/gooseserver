// A SpreadsheetApp small enough to read in one sitting, plus a loader that runs Code.gs against
// it. Code.gs used to be reasoned about and never executed — its header still lists the live
// checks — and the two things this covers are the ones reasoning was worst at: what a cell's
// VALUE is once formatting is involved, and which cells a save actually writes.
//
// IT IS A MODEL, NOT SHEETS. What it claims about Apps Script, and what therefore still has to
// hold live for these tests to mean anything:
//
//   * getValues() returns the cell's stored value — a number as a number, a boolean as a boolean,
//     a formula as its RESULT, an empty cell as ''.
//   * getDisplayValues() returns the same cells as formatted text.
//   * setValues() on a range replaces exactly those cells and nothing else, and a formula cell
//     that is not written keeps its formula.
//   * getRange() past the bottom of the grid throws rather than growing it.
//
// Everything Code.gs does not call is absent, deliberately: getFormulas, getUi, merged cells,
// data validation. LockService IS here — writeRow and saveBatch both take a document lock — but
// only as a counter: it models that the lock is taken and released, not contention, because one
// vm context has no second caller to contend with. setNumberFormat is present but only RECORDED (into
// sheet.writes, as { row, col, format }), because what the tests pin is that writeRow asks for
// the '@' pin on exactly the edited Text cells, and asks BEFORE writing the value — whether '@'
// actually suppresses Sheets' entry parsing is a live check, not something this fake can model.
//
// A CELL is either a plain value (number, string, boolean, null for empty) or a descriptor:
//
//   { value, display } — a cell whose FORMAT differs from its value. This is the whole point of
//     the exercise: DECIMAL 0.1235 displayed to 2dp as '0.12', item_value 1500 as '1,500'.
//   { formula, value } — a formula cell. getValues gives `value`; the formula survives only if
//     nothing writes over the cell, which is what the changed-cells-only write is for.
//
// Every setValues call is recorded in sheet.writes, in order, as { row, col, values }. That array
// is the assertion surface for "an unchanged save wrote nothing" — the claim no amount of reading
// the values back can make, because a rewrite with identical text is invisible in the values.
//
// Every READ is recorded the same way, in sheet.reads, as { kind: 'values' | 'display', row, col,
// numRows, numCols }. A read costs a round trip to the Sheets service and nothing about the values
// it returns can show how many were made, so this is the only way to state "the sheet is read once,
// not twice" or "an ordinary field edit does not scan the id column" as a test rather than as a
// claim in a comment.
//
// Every deleteRows call is recorded in sheet.deletes as { row, count }, in call order. Order is
// the assertion that matters: deletes must run bottom-up, and a top-down implementation leaves
// the same final grid when the runs happen not to overlap — so only the call sequence can tell
// the two apart.

import { readFileSync } from 'node:fs';
import { createContext, runInContext } from 'node:vm';

function isDescriptor(cell) {
  return cell !== null && typeof cell === 'object' && !(cell instanceof Date);
}

function rawOf(cell) {
  if (!isDescriptor(cell)) return cell === null || cell === undefined ? '' : cell;
  return 'value' in cell ? cell.value : '';
}

// Sheets' own rendering for the types that turn up here: '' for empty, TRUE/FALSE for booleans,
// String() for the rest — unless the cell states a display of its own, which is how a format is
// modelled.
function displayOf(cell) {
  if (isDescriptor(cell) && cell.display !== undefined) return String(cell.display);
  const raw = rawOf(cell);
  if (raw === '') return '';
  if (raw === true) return 'TRUE';
  if (raw === false) return 'FALSE';
  return String(raw);
}

class FakeSheet {
  // `grid` is rows of cells, row 0 being the header. maxRows/maxCols default to the data's own
  // extent, which is the trimmed-sheet case an append has to grow.
  constructor(name, grid, options = {}) {
    this.name = name;
    this.writes = [];
    this.reads = [];
    this.deletes = [];
    this.maxRows = options.maxRows === undefined ? grid.length : options.maxRows;
    this.maxCols = options.maxCols === undefined
      ? grid.reduce((w, row) => Math.max(w, row.length), 0)
      : options.maxCols;

    this.cells = [];
    for (let r = 0; r < this.maxRows; r++) {
      this.cells.push([]);
      for (let c = 0; c < this.maxCols; c++) {
        const row = grid[r] || [];
        this.cells[r].push(c < row.length && row[c] !== undefined ? row[c] : null);
      }
    }
  }

  _filled(cell) {
    return rawOf(cell) !== '';
  }

  getMaxRows() { return this.maxRows; }

  getMaxColumns() { return this.maxCols; }

  getLastRow() {
    for (let r = this.maxRows - 1; r >= 0; r--) {
      if (this.cells[r].some((cell) => this._filled(cell))) return r + 1;
    }
    return 0;
  }

  getLastColumn() {
    for (let c = this.maxCols - 1; c >= 0; c--) {
      if (this.cells.some((row) => this._filled(row[c]))) return c + 1;
    }
    return 0;
  }

  getDataRange() {
    return this.getRange(1, 1, Math.max(this.getLastRow(), 1), Math.max(this.getLastColumn(), 1));
  }

  // Throws past the grid, as the real one does — that is what makes writeRow's insertRowsAfter
  // load-bearing rather than decorative.
  getRange(row, col, numRows, numCols) {
    if (row < 1 || col < 1 || numRows < 1 || numCols < 1) {
      throw new Error('fake sheet: getRange out of bounds (' +
                      [row, col, numRows, numCols].join(', ') + ')');
    }
    if (row + numRows - 1 > this.maxRows || col + numCols - 1 > this.maxCols) {
      throw new Error('fake sheet: range past the grid (' + this.maxRows + 'x' + this.maxCols +
                      ') at ' + [row, col, numRows, numCols].join(', '));
    }
    return new FakeRange(this, row, col, numRows, numCols);
  }

  insertRowsAfter(after, count) {
    for (let i = 0; i < count; i++) {
      this.cells.splice(after + i, 0, new Array(this.maxCols).fill(null));
    }
    this.maxRows += count;
  }

  // The mirror image of insertRowsAfter: the rows go, everything below shifts UP, and the grid
  // shrinks. That shift is why saveBatch applies deletes last and from the bottom — a plan built
  // against the pre-delete sheet stays valid only until the first deleteRows call.
  //
  // 1-based position, `howMany` rows, matching Sheet.deleteRows(rowPosition, howMany).
  deleteRows(position, howMany) {
    if (position < 1 || howMany < 1 || position + howMany - 1 > this.maxRows) {
      throw new Error('fake sheet: deleteRows out of bounds (' + position + ', ' + howMany +
                      ') on a grid of ' + this.maxRows);
    }
    this.deletes.push({ row: position, count: howMany });
    this.cells.splice(position - 1, howMany);
    this.maxRows -= howMany;
  }

  // For assertions: the sheet as it now stands, raw values and display text.
  raw() { return this.cells.map((row) => row.map(rawOf)); }

  shown() { return this.cells.map((row) => row.map(displayOf)); }

  // The cell objects themselves, so a test can see that a formula descriptor survived rather
  // than being flattened to its result.
  cell(row, col) { return this.cells[row - 1][col - 1]; }
}

class FakeRange {
  constructor(sheet, row, col, numRows, numCols) {
    this.sheet = sheet;
    this.row = row;
    this.col = col;
    this.numRows = numRows;
    this.numCols = numCols;
  }

  _map(fn) {
    const out = [];
    for (let r = 0; r < this.numRows; r++) {
      const line = [];
      for (let c = 0; c < this.numCols; c++) {
        line.push(fn(this.sheet.cells[this.row - 1 + r][this.col - 1 + c]));
      }
      out.push(line);
    }
    return out;
  }

  _read(kind) {
    this.sheet.reads.push({
      kind, row: this.row, col: this.col, numRows: this.numRows, numCols: this.numCols,
    });
  }

  getValues() { this._read('values'); return this._map(rawOf); }

  getDisplayValues() { this._read('display'); return this._map(displayOf); }

  setNumberFormat(format) {
    this.sheet.writes.push({ row: this.row, col: this.col, format });
  }

  setValues(values) {
    if (values.length !== this.numRows || values.some((row) => row.length !== this.numCols)) {
      throw new Error('fake sheet: setValues shape does not match the range');
    }
    // Array.from, not .slice(): `values` was built inside the vm, so its arrays carry the vm's
    // Array prototype and assert.deepEqual — which compares prototypes — refuses them against an
    // ordinary array. Copied into this realm at the boundary, once.
    this.sheet.writes.push({
      row: this.row,
      col: this.col,
      values: Array.from(values, (row) => Array.from(row)),
    });
    for (let r = 0; r < this.numRows; r++) {
      for (let c = 0; c < this.numCols; c++) {
        this.sheet.cells[this.row - 1 + r][this.col - 1 + c] = values[r][c];
      }
    }
  }
}

// Loads Code.gs into a fresh context over these sheets and returns its globals plus the sheets.
// A fresh context per call, so one test's write cannot be seen by another.
export function loadCodeGs(sheetsByName, options = {}, settings = {}) {
  const sheets = {};
  Object.keys(sheetsByName).forEach((name) => {
    sheets[name] = new FakeSheet(name, sheetsByName[name], options[name] || {});
  });

  let flushes = 0;
  const sandbox = {
    SpreadsheetApp: {
      getActiveSpreadsheet: () => ({ getSheetByName: (name) => sheets[name] || null }),
      flush: () => { flushes += 1; },
    },
  };

  // LockService, which the fake used to leave out on the grounds that Code.gs never called it.
  // It does now. Modelled to the three methods Code.gs uses, and no further: waitLock either
  // takes the lock or throws (the real one throws on timeout — it does not return false; that is
  // tryLock), and releaseLock is expected in a finally.
  const locks = { acquired: 0, released: 0, held: false };
  sandbox.LockService = {
    getDocumentLock: () => ({
      waitLock: (timeoutMs) => {
        if (settings.lockFails) {
          throw new Error('Could not obtain lock after ' + timeoutMs + 'ms.');
        }
        locks.acquired += 1;
        locks.held = true;
      },
      releaseLock: () => { locks.released += 1; locks.held = false; },
      hasLock: () => locks.held,
    }),
  };

  const context = createContext(sandbox);
  const source = readFileSync(new URL('../Code.gs', import.meta.url), 'utf8');
  runInContext(source, context, { filename: 'Code.gs' });

  // Everything Code.gs returns crosses a realm boundary, and its objects and arrays carry the
  // vm's prototypes — which assert.deepEqual compares, so a structurally identical result would
  // be refused. These are plain JSON payloads on their way to google.script.run anyway, which is
  // exactly what survives this round trip.
  const toHost = (value) => (value === undefined ? undefined : JSON.parse(JSON.stringify(value)));

  return {
    sheets,
    flushes: () => flushes,
    locks: () => ({ ...locks }),
    readSheet: (...args) => toHost(sandbox.readSheet(...args)),
    readSheetIndex: (...args) => toHost(sandbox.readSheetIndex(...args)),
    writeRow: (...args) => toHost(sandbox.writeRow(...args)),
  };
}

// The same model, running MigrateClassRestrictions.gs — the one-off that inverts
// class_restrictions to an allow list. It is a separate loader rather than another entry in
// loadCodeGs because it needs two globals Code.gs has never touched (Logger, PropertiesService)
// and because the file it loads is meant to be deleted once it has been run: when it goes, so
// does this, and loadCodeGs is left exactly as it was.
//
// `properties` seeds the document properties, which is how a test can present a workbook that
// has already been migrated. The two entry points come back through JSON for the same reason
// loadCodeGs's do: a plan built inside the vm carries the vm's prototypes, and deepEqual
// compares those. Errors are deliberately NOT wrapped — assert.throws matches on the message.
export function loadMigrationGs(sheetsByName, { properties = {} } = {}) {
  const sheets = {};
  Object.keys(sheetsByName).forEach((name) => {
    sheets[name] = new FakeSheet(name, sheetsByName[name]);
  });

  const logs = [];
  const props = { ...properties };
  const sandbox = {
    SpreadsheetApp: {
      getActiveSpreadsheet: () => ({ getSheetByName: (name) => sheets[name] || null }),
      flush: () => {},
    },
    Logger: { log: (line) => logs.push(String(line)) },
    PropertiesService: {
      getDocumentProperties: () => ({
        getProperty: (key) => (key in props ? props[key] : null),
        setProperty: (key, value) => { props[key] = value; },
      }),
    },
    Date,
  };

  const context = createContext(sandbox);
  const source = readFileSync(new URL('../MigrateClassRestrictions.gs', import.meta.url), 'utf8');
  runInContext(source, context, { filename: 'MigrateClassRestrictions.gs' });

  const toHost = (value) => (value === undefined ? undefined : JSON.parse(JSON.stringify(value)));

  return {
    sheets,
    logs,
    properties: props,
    gs: {
      previewClassRestrictionsMigration: (...args) =>
        toHost(sandbox.previewClassRestrictionsMigration(...args)),
      applyClassRestrictionsMigration: (...args) =>
        toHost(sandbox.applyClassRestrictionsMigration(...args)),
    },
  };
}
