// Builds a record form from GOOSE_SCHEMA plus Layout's grouping. One control per column,
// except where a composite claims several (see Composites, which must be defined before
// render/collect are called — build.mjs emits composites.html and Editor.html includes it
// ahead of forms.html).
var Forms = (function () {
  function el(tag, attrs, text) {
    var node = document.createElement(tag);
    if (attrs) Object.keys(attrs).forEach(function (k) { node.setAttribute(k, attrs[k]); });
    if (text !== undefined) node.textContent = text;
    return node;
  }

  // A cell arrives from the spreadsheet as whatever the Sheets API decided it was: '', a
  // string, or a NUMBER. `value || ''` would turn a numeric 0 into '' — which for a Bool or
  // an Enum-shaped cell means "blank, use the default" and silently drops a stored 0 on the
  // next save. Everything here is compared and written as a string, so coerce once, at the
  // edge, and never again.
  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  // Placeholder shows the SQL default so a blank field reads as "will use 0", not "unset".
  // Blank must stay blank on write (CsvToSqlBase.cs:27).
  //
  // Descriptor defaults are SQL literals: strings arrive quoted ("''", "'0,*,0,*'"), numbers
  // bare ("0"). Strip the quotes only as a matched PAIR — an unpaired quote is part of the
  // value, and stripping one end of it would show a default the database does not have.
  function placeholderFor(column) {
    if (column.required) return 'required';
    if (column.default === undefined || column.default === null) return '';
    var text = String(column.default);
    var quoted = /^'([\s\S]*)'$/.exec(text);
    if (quoted) text = quoted[1];
    // "" is a real default (the empty string) but renders as a bare "default " with nothing
    // after it, which reads as a bug rather than as a value.
    return text === '' ? 'default (blank)' : 'default ' + text;
  }

  // Assigning a <select>'s value to something no <option> carries leaves the select showing
  // nothing, and it then READS BACK as ''. For a cell holding an enum name that is not in
  // enumNames — a renamed enum member, a typo, a hand-edited row — that is silent data loss:
  // the field looks empty and the next save writes blank over it. Keep the stored value as a
  // real option, flagged, so it round-trips and Validation gets to report it.
  function withUnknown(select, value) {
    if (value === '') return;
    var options = select.getElementsByTagName('option');
    for (var i = 0; i < options.length; i++) {
      if (options[i].value === value) return;
    }
    select.appendChild(el('option', { value: value }, value + ' (not a valid value)'));
  }

  function scalarControl(column, rawValue) {
    var value = str(rawValue);

    if (column.kind === 'Enum') {
      var select = el('select', { name: column.name, id: 'f-' + column.name });
      if (!column.required) select.appendChild(el('option', { value: '' }, ''));
      (column.enumNames || []).forEach(function (n) {
        select.appendChild(el('option', { value: n }, n));
      });
      withUnknown(select, value);
      select.value = value;
      return select;
    }

    if (column.kind === 'Bool') {
      var box = el('select', { name: column.name, id: 'f-' + column.name });
      box.appendChild(el('option', { value: '' }, ''));
      box.appendChild(el('option', { value: '0' }, 'No'));
      box.appendChild(el('option', { value: '1' }, 'Yes'));
      withUnknown(box, value);
      box.value = value;
      return box;
    }

    // type="text" for numeric kinds too, deliberately. type="number" reads back '' for input
    // the browser cannot parse, so a typo'd "1o" would arrive here indistinguishable from a
    // blank — and blank means "use the SQL default", so the typo would silently write the
    // default instead of being reported. Validation.validateCell already produces a precise
    // message for every numeric kind; let it.
    var input = el('input', {
      name: column.name,
      id: 'f-' + column.name,
      type: 'text',
      placeholder: placeholderFor(column),
      autocomplete: 'off',
    });
    input.value = value;
    return input;
  }

  // Renders the whole record. `ctx` carries idSets, sprite bundles and picker data.
  function render(container, schema, values, ctx) {
    container.innerHTML = '';

    // Prototype-free, for the reason layout.js:118-121 gives and then one worse: with a plain
    // object, a column named 'toString' reads truthy from Object.prototype in BOTH claimed and
    // leaders, the two compare equal, and the name is treated as a composite leader — handing
    // Composites.control a function where a composite should be.
    var byName = Object.create(null);
    schema.columns.forEach(function (c) { byName[c.name] = c; });

    // Composites claim their columns so no duplicate control is rendered. The leader is the
    // first claimed column that the schema actually HAS — not blindly columns[0]. A composite
    // naming a column the descriptors dropped would otherwise elect an absent leader, and
    // every one of its siblings would then be skipped as "rendered by its leader" by a
    // control that never rendered, quietly making those columns uneditable.
    var claimed = Object.create(null);
    var leaders = Object.create(null);
    (schema.composites || []).forEach(function (comp) {
      var leader = null;
      comp.columns.forEach(function (n) {
        claimed[n] = comp;
        if (leader === null && byName[n]) leader = n;
      });
      if (leader !== null) leaders[leader] = comp;
    });

    if (Layout.needsRestart(schema.sheet)) {
      container.appendChild(el('div', { class: 'warn' },
        'Changes to ' + schema.sheet + ' need a full server restart — /reloadsql does not ' +
        'reload this table.'));
    }

    Layout.groupsFor(schema.sheet, schema.columns).forEach(function (group) {
      var section = el('section');
      section.appendChild(el('h3', null, group.title));
      var rendered = 0;

      // Layout.groupsFor only ever returns names taken from the columns handed to it, so
      // byName[name] is always a real column here.
      group.columns.forEach(function (name) {
        var column = byName[name];

        var comp = claimed[name];
        if (comp && leaders[name] !== comp) return;   // rendered by its leader

        var row = el('div', { class: 'field' });
        // A composite is several controls; there is no single id to point a label at, so it
        // gets a plain label rather than a `for` that resolves to nothing.
        row.appendChild(comp ? el('label', null, name)
                             : el('label', { for: 'f-' + name }, name));

        row.appendChild(comp ? Composites.control(comp, byName, values, ctx)
                             : scalarControl(column, values[name]));

        row.appendChild(el('div', { class: 'error', 'data-error-for': name }));
        section.appendChild(row);
        rendered++;
      });

      // A group whose every column was claimed by a composite led from an earlier group would
      // otherwise leave a heading with nothing under it.
      if (rendered) container.appendChild(section);
    });
  }

  // Reads the form back into a name -> string map. Missing and blank both come back as ''.
  // Named controls nested inside a composite are swept up here too; Composites.collect runs
  // afterwards and is the authority on its own columns.
  function collect(container, schema) {
    // A plain object deliberately, unlike render's internal maps: this one is handed back to
    // the caller and travels over google.script.run, so it stays an ordinary serialisable
    // object. It is safe as one because EVERY schema column is seeded below before anything
    // reads it, so no lookup can ever fall through to Object.prototype.
    var values = {};
    schema.columns.forEach(function (c) { values[c.name] = ''; });

    var inputs = container.querySelectorAll('[name]');
    for (var i = 0; i < inputs.length; i++) {
      values[inputs[i].getAttribute('name')] = str(inputs[i].value);
    }

    (schema.composites || []).forEach(function (comp) {
      var claimed = Composites.collect(comp, container);
      Object.keys(claimed).forEach(function (k) { values[k] = str(claimed[k]); });
    });

    return values;
  }

  // Every slot is cleared before any is filled, so a message from the previous submit cannot
  // survive next to a field that now validates. Matching is done by reading the attribute
  // rather than by building a selector out of the column name.
  function showErrors(container, errors) {
    var slots = container.querySelectorAll('[data-error-for]');
    // Prototype-free: `if (!byColumn['toString'])` is false on a plain object, so a column of
    // that name would never have its slot recorded and its error would be silently dropped.
    var byColumn = Object.create(null);
    for (var i = 0; i < slots.length; i++) {
      slots[i].textContent = '';
      var key = slots[i].getAttribute('data-error-for');
      if (!byColumn[key]) byColumn[key] = slots[i];
    }

    // An error for a column with no slot is dropped rather than thrown on: the id column of a
    // sheet is validated by Validation.validateId whether or not it is on screen, and losing
    // the whole error list to one orphan is worse than losing the orphan.
    errors.forEach(function (e) {
      if (byColumn[e.column]) byColumn[e.column].textContent = e.message;
    });
  }

  return {
    render: render,
    collect: collect,
    showErrors: showErrors,
    scalarControl: scalarControl,
    placeholderFor: placeholderFor,
    el: el,
  };
})();

if (typeof module !== 'undefined') module.exports = { Forms: Forms };
