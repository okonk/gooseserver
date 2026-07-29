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

  function isNumericSql(sql) {
    return RANGES[sql] !== undefined || sql.indexOf('DECIMAL') === 0;
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
      return { ok: true, write: true };
    }

    // Numeric kinds: Id, Int, Decimal.
    if (isNumericSql(column.sql) || column.kind !== 'Text') {
      if (!/^-?\d+(\.\d+)?$/.test(value)) {
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
            message: column.name + ' must be between ' + range[0] + ' and ' + range[1] +
                     ' (' + column.sql + ')',
          };
        }
      }
    }

    // Foreign key: 0 and blank both mean "none".
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
    var value = String(raw || '').trim();
    if (value === '') return { ok: false, message: 'id is required' };
    if (!/^\d+$/.test(value)) return { ok: false, message: 'id must be a whole number' };

    var n = Number(value);
    if (existingIds.has(n) && n !== ownId) {
      return { ok: false, message: 'id ' + n + ' is already used' };
    }
    return { ok: true };
  }

  function nextId(ids) {
    if (!ids || ids.length === 0) return 1;
    return Math.max.apply(null, ids) + 1;
  }

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
