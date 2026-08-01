// Validation rules derived from the column descriptors in CsvToSql.Core.
// Blank means "use the SQL default" (CsvToSqlBase.cs:27 skips empty cells), so a blank
// optional cell is valid AND must not be written — writing 0 would pin a value that was
// previously tracking the default.
var Validation = (function () {
  var RANGES = {
    SMALLINT: [-32768, 32767],
    INT: [-2147483648, 2147483647],
    INTEGER: [-2147483648, 2147483647],
    // NOTE: the BIGINT bounds are approximate — they are not exactly representable as
    // IEEE-754 doubles, so both the bound and any parsed value round the same way. That is
    // good enough to reject obviously-out-of-range input; exact BIGINT handling (BigInt or
    // string comparison) is deferred.
    BIGINT: [-9223372036854775808, 9223372036854775807],
  };

  // The BIGINT bounds above do not survive Number formatting (they render as
  // -9223372036854776000), so error messages use the exact literals instead.
  var RANGE_TEXT = {
    BIGINT: ['-9223372036854775808', '9223372036854775807'],
  };

  function rangeText(sql, index) {
    return RANGE_TEXT[sql] ? RANGE_TEXT[sql][index] : String(RANGES[sql][index]);
  }

  // "DECIMAL(5,2)" -> { precision: 5, scale: 2 }. Anything else -> null.
  function decimalSpec(sql) {
    var m = /^DECIMAL\((\d+),\s*(\d+)\)$/.exec(sql || '');
    if (!m) return null;
    return { precision: Number(m[1]), scale: Number(m[2]) };
  }

  // Largest magnitude a DECIMAL(p,s) can hold, as a display string: p - s integer
  // digits then s fraction digits, all nines.
  function decimalMax(spec) {
    var whole = new Array(spec.precision - spec.scale + 1).join('9') || '0';
    return spec.scale > 0 ? whole + '.' + new Array(spec.scale + 1).join('9') : whole;
  }

  function validateCell(column, raw, idSets) {
    var value = (raw === null || raw === undefined) ? '' : String(raw).trim();

    if (value === '') {
      if (column.required) {
        return { ok: false, write: false, message: column.name + ' is required' };
      }
      return { ok: true, write: false };
    }

    if (column.kind === 'Enum') {
      var names = column.enumNames || [];
      if (names.indexOf(value) === -1) {
        return {
          ok: false, write: true,
          message: '"' + value + '" is not a valid ' + column.name +
                   ' — expected one of: ' + names.join(', '),
        };
      }
      return { ok: true, write: true };
    }

    if (column.kind === 'Bool') {
      if (value !== '0' && value !== '1') {
        return { ok: false, write: true, message: column.name + ' must be 0 or 1' };
      }
      return { ok: true, write: true };
    }

    if (column.kind === 'Text') {
      // The store is a spreadsheet, and a cell entered as '=...' becomes a FORMULA — the
      // sheet then holds a computed value, or #NAME?, where the importer expects the text.
      // Refused rather than escaped: nothing in this data begins with '=' (the closest is
      // spell_effects.hp_change_formula, 50 rows beginning with '-', which is ordinary text),
      // so there is no legitimate value to rescue and an escaping scheme would be one more
      // thing that has to round-trip exactly.
      //
      // Only Text can reach this. A leading '=' fails every other kind's own check first —
      // the numeric regex, the enum name list, Bool's 0-or-1.
      if (value.charAt(0) === '=') {
        return {
          ok: false, write: true,
          message: column.name + ' cannot start with "=" — the spreadsheet would store it ' +
                   'as a formula instead of as text',
        };
      }
      return { ok: true, write: true };
    }

    // Numeric kinds: Id, Int, Decimal. ColumnKind is closed (Column.cs:7) and the other
    // three kinds have all returned above, so everything reaching here is numeric.
    var parts = /^-?(\d+)(?:\.(\d+))?$/.exec(value);
    if (!parts) {
      return { ok: false, write: true, message: column.name + ' must be a number' };
    }

    var range = RANGES[column.sql];
    if (range) {
      var n = Number(value);
      if (!Number.isInteger(n)) {
        return { ok: false, write: true, message: column.name + ' must be a whole number' };
      }
      if (n < range[0] || n > range[1]) {
        return {
          ok: false, write: true,
          message: column.name + ' must be between ' + rangeText(column.sql, 0) + ' and ' +
                   rangeText(column.sql, 1) + ' (' + column.sql + ')',
        };
      }
    }

    // DECIMAL(p,s) is checked by digit count, not magnitude: p total digits, s of them
    // after the point, so p - s before it. Too many integer digits is a MySQL error.
    // Too many fraction digits is not — MySQL truncates with a warning — but silently
    // rounding someone's Titles.chance is worse than telling them, so we reject.
    // Zeros that only pad the display are stripped from both ends first.
    var spec = decimalSpec(column.sql);
    if (spec) {
      var whole = parts[1].replace(/^0+(?=\d)/, '');
      var fraction = (parts[2] || '').replace(/0+$/, '');
      // DECIMAL(p,p) holds only a fraction, so "0" is the only legal integer part.
      if (spec.precision === spec.scale ? whole !== '0'
                                        : whole.length > spec.precision - spec.scale) {
        return {
          ok: false, write: true,
          message: column.name + ' must be between -' + decimalMax(spec) + ' and ' +
                   decimalMax(spec) + ' (' + column.sql + ')',
        };
      }
      if (fraction.length > spec.scale) {
        return {
          ok: false, write: true,
          message: column.name + ' allows at most ' + spec.scale + ' decimal place' +
                   (spec.scale === 1 ? '' : 's') + ' (' + column.sql + ')',
        };
      }
    }

    // Foreign key: 0 and blank both mean "none". An unknown ref sheet is allowed through
    // deliberately — idSets may be partially loaded, and failing closed would block saves
    // on rows the user has not touched.
    //
    // That is HALF a pair, and the other half is App.unverifiedRefs (app.js gate 3), which
    // refuses to save a record whose fk columns point at a list that is missing. Fail open
    // here so a load in flight does not report every id as broken; fail closed there so a
    // load that FAILED cannot let a nonexistent id be written. Neither is safe alone.
    if (column.ref && idSets && value !== '0') {
      var known = idSets[column.ref];
      if (known && !known.has(Number(value))) {
        return {
          ok: false, write: true,
          message: column.name + ' = ' + value + ' does not exist in ' + column.ref,
        };
      }
    }

    return { ok: true, write: true };
  }

  function validateId(raw, existingIds, ownId) {
    var value = (raw === null || raw === undefined) ? '' : String(raw).trim();
    if (value === '') return { ok: false, message: 'id is required' };
    if (!/^\d+$/.test(value) || Number(value) < 1) {
      return { ok: false, message: 'id must be a positive whole number' };
    }

    var n = Number(value);
    if (existingIds.has(n) && n !== ownId) {
      return { ok: false, message: 'id ' + n + ' is already used' };
    }
    return { ok: true };
  }

  // Accepts an array or a Set. Junk cells in the id column are ignored rather than
  // poisoning the result with NaN. Ids are positive whole numbers by contract (see
  // validateId), so the running max floors at 0 and truncates: the suggestion must be
  // something validateId will then accept, and max+1 over a negative or fractional max
  // would hand the user an id its own validation refuses.
  function nextId(ids) {
    if (!ids) return 1;
    var list = (typeof Array.from === 'function') ? Array.from(ids) : ids;
    var max = 0;
    for (var i = 0; i < list.length; i++) {
      var n = Math.floor(Number(list[i]));
      if (Number.isFinite(n) && n > max) max = n;
    }
    return max + 1;
  }

  // idSets is keyed by referenced sheet name and holds that sheet's id Set; the reserved
  // key __self holds the current sheet's own ids, used for duplicate detection. ownId is
  // the id of the row being edited (null when adding), exempted from that check.
  function validateRecord(columns, values, idSets, ownId) {
    var errors = [];

    for (var i = 0; i < columns.length; i++) {
      var c = columns[i];
      var raw = values[c.name];

      if (c.pk) {
        var idResult = validateId(raw, (idSets && idSets.__self) || new Set(), ownId);
        if (!idResult.ok) errors.push({ column: c.name, message: idResult.message });
        continue;
      }

      var r = validateCell(c, raw, idSets);
      if (!r.ok) errors.push({ column: c.name, message: r.message });
    }

    return { ok: errors.length === 0, errors: errors };
  }

  return {
    validateCell: validateCell,
    validateId: validateId,
    nextId: nextId,
    validateRecord: validateRecord,
  };
})();

if (typeof module !== 'undefined') module.exports = { Validation: Validation };
