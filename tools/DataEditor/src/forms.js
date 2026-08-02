// Builds a record form from GOOSE_SCHEMA plus Layout's grouping. One control per column,
// except where a composite claims several (see Composites), and except a column with a `ref`,
// which goes to Pickers.fkControl.
//
// Both are resolved as FREE GLOBALS at call time, not at load time — nothing here runs during
// the script's own evaluation — so Editor.html may include composites.html and pickers.html in
// any order relative to this file, as long as all three are on the page before render() runs.
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

  // The column's SQL default as a CELL would hold it, or '' for a column that has none.
  //
  // Descriptor defaults are SQL literals: strings arrive quoted ("''", "'0,*,0,*'"), numbers
  // bare ("0"). Strip the quotes only as a matched PAIR — an unpaired quote is part of the
  // value, and stripping one end of it would give a default the database does not have.
  function defaultOf(column) {
    if (!column || column.default === undefined || column.default === null) return '';
    var text = String(column.default);
    var quoted = /^'([\s\S]*)'$/.exec(text);
    return quoted ? quoted[1] : text;
  }

  // A record as the IMPORTER will see it: every blank cell replaced by the value the database
  // would put there (CsvToSqlBase.cs:27 skips an empty cell, so the column default lands). This
  // is what a PREVIEW must read. The form's own fields keep the raw cells — blank must stay blank
  // on write — so the two maps travel side by side rather than one replacing the other.
  //
  // ENUM COLUMNS ARE LEFT BLANK, deliberately. A cell holds the enum NAME ('Helmet',
  // DescriptorTransform.cs:24) but the SQL default is the C# enum's NUMBER (item_slot: 20), and
  // enumNames carries no numbering to map one to the other — the members have explicit values, so
  // the index is not the number. Substituting '20' for a blank item_slot would hand every reader a
  // string from the wrong value space; leaving it blank keeps "unknown slot", which is what a
  // blank already meant and what every consumer already handles.
  //
  // A CELL NOT NAMED BY `columns` IS CARRIED THROUGH UNCHANGED rather than dropped. The caller
  // that has the whole sheet's descriptors loses nothing either way, but a caller holding a subset
  // of them — Composites.control, which knows only the columns it was handed — would otherwise
  // hand its control a record with the neighbouring cells missing, which is a worse answer than an
  // unresolved one. Resolving twice is therefore harmless: the second pass finds nothing blank.
  function effectiveValues(values, columns) {
    var out = {};
    Object.keys(values || {}).forEach(function (k) { out[k] = str(values[k]); });
    (columns || []).forEach(function (c) {
      var text = str(values ? values[c.name] : '');
      out[c.name] = (text === '' && c.kind !== 'Enum') ? defaultOf(c) : text;
    });
    return out;
  }

  // Placeholder shows the SQL default so a blank field reads as "will use 0", not "unset".
  // Blank must stay blank on write (CsvToSqlBase.cs:27).
  function placeholderFor(column) {
    if (column.required) return 'required';
    if (column.default === undefined || column.default === null) return '';
    var text = defaultOf(column);
    // "" is a real default (the empty string) but renders as a bare "default " with nothing
    // after it, which reads as a bug rather than as a value.
    return text === '' ? 'default (blank)' : 'default ' + text;
  }

  // Assigning a <select>'s value to something no <option> carries leaves the select showing
  // nothing, and it then READS BACK as ''. For a cell holding an enum name that is not in
  // enumNames — a renamed enum member, a typo, a hand-edited row — that is silent data loss:
  // the field looks empty and the next save writes blank over it. Keep the stored value as a
  // real option, flagged, so it round-trips and Validation gets to report it.
  function preserveUnknownValue(select, value) {
    if (value === '') return;
    var options = select.getElementsByTagName('option');
    for (var i = 0; i < options.length; i++) {
      if (options[i].value === value) return;
    }
    select.appendChild(el('option', { value: value }, value + ' (not a valid value)'));
  }

  // The prefix every control's element id is built from. One definition, because a control and
  // the <label for> pointing at it are built in different places and a second spelling of the
  // default would silently unlink them.
  function idPrefixOf(idPrefix) {
    return typeof idPrefix === 'string' && idPrefix !== '' ? idPrefix : 'f-';
  }

  // A TRI-STATE checkbox over a hidden cell, which is the whole point of it. save() writes a
  // cell only when Validation.validateCell says `write` (app.js), and a BLANK boolean means "use
  // the SQL default" (CsvToSqlBase.cs:27) — so a two-state box, which can only ever read back 0
  // or 1, would write 0 into every blank boolean on every save. Items has four blank-defaulted
  // booleans and NPCs five; merely opening such a record and saving it would rewrite them all.
  //
  // The checkbox carries the id (so the field's <label for> lands on it) and NO name; the hidden
  // input carries the name and IS the cell, seeded with the stored text verbatim, exactly as the
  // composite controls seed theirs. collect()'s [name] sweep therefore reads the cell and never
  // the box — a named checkbox would read back 'on' when ticked and its value attribute when not.
  function boolControl(column, value, idPrefix) {
    var prefix = idPrefixOf(idPrefix);
    var wrap = el('span', { class: 'boolean' });

    // A cell holding anything else has nowhere to go on a checkbox, and coercing it to ticked
    // would silently rewrite it. Same shape and same reason as preserveUnknownValue: keep the
    // value, flag it, and let Validation.validateCell report it.
    if (value !== '' && value !== '0' && value !== '1') {
      var raw = el('input', {
        name: column.name, id: prefix + column.name, type: 'text', autocomplete: 'off',
      });
      raw.value = value;
      wrap.appendChild(raw);
      wrap.appendChild(el('span', { class: 'status bad' }, 'not a 0/1 value'));
      return wrap;
    }

    var box = el('input', { id: prefix + column.name, type: 'checkbox' });
    box.checked = value === '1';
    box.indeterminate = value === '';

    var hidden = el('input', { type: 'hidden', name: column.name });
    hidden.value = value;

    // No dispatch of our own: `change` bubbles, so app.js's delegated listener already sees it.
    // The ordering is load-bearing — this listener is on the box itself, so it runs in the target
    // phase and writes hidden.value BEFORE the event reaches the delegated listener on the form
    // container, which therefore previews the new cell rather than the old one.
    box.addEventListener('change', function () {
      box.indeterminate = false;
      hidden.value = box.checked ? '1' : '0';
    });

    wrap.appendChild(box);
    wrap.appendChild(hidden);

    // A required column may not be blank at all, so there is no third state to return to.
    if (!column.required) {
      // aria-label as well as title: a button's text content OUTRANKS title in the accessible
      // name computation, so without it a screen reader announces "multiplication sign button"
      // and a touch user, who never sees a title tooltip, gets nothing at all.
      var clear = el('button', {
        type: 'button', class: 'clear', title: 'use the default', 'aria-label': 'use the default',
      }, '×');
      // Dispatched from the BUTTON rather than the box: a change on the box would run the
      // listener above and immediately write '0' back over the blank we just restored. It still
      // reaches the delegated preview listener, which is on the form container above both.
      clear.addEventListener('click', function () {
        box.indeterminate = true;
        box.checked = false;
        hidden.value = '';
        clear.dispatchEvent(new Event('change', { bubbles: true }));
      });
      wrap.appendChild(clear);
    }

    return wrap;
  }

  function scalarControl(column, rawValue, idPrefix) {
    var value = str(rawValue);
    var prefix = idPrefixOf(idPrefix);

    if (column.kind === 'Enum') {
      var select = el('select', { name: column.name, id: prefix + column.name });
      if (!column.required) select.appendChild(el('option', { value: '' }, ''));
      (column.enumNames || []).forEach(function (n) {
        select.appendChild(el('option', { value: n }, n));
      });
      preserveUnknownValue(select, value);
      select.value = value;
      return select;
    }

    if (column.kind === 'Bool') return boolControl(column, value, prefix);

    // type="text" for numeric kinds too, deliberately. type="number" reads back '' for input
    // the browser cannot parse, so a typo'd "1o" would arrive here indistinguishable from a
    // blank — and blank means "use the SQL default", so the typo would silently write the
    // default instead of being reported. Validation.validateCell already produces a precise
    // message for every numeric kind; let it.
    var input = el('input', {
      name: column.name,
      id: prefix + column.name,
      type: 'text',
      placeholder: placeholderFor(column),
      autocomplete: 'off',
    });
    input.value = value;
    return input;
  }

  // A column with a `ref` points at another sheet, and Pickers.fkControl is the typeahead for
  // one. Routing happens HERE rather than inside scalarControl so that composites — which reach
  // for Pickers themselves — are not routed twice.
  //
  // Pickers is resolved as a free global at call time, exactly as Composites is (Editor.html
  // includes pickers.html ahead of forms.html). The guard is not defensive padding: forms.js is
  // the only module that renders a whole record, and losing every field of a sheet because one
  // include is missing is a far worse failure than 26 columns falling back to a text box that
  // still holds, validates and saves the right value.
  //
  // A PART GRAPHIC is routed the same way and for the same reason. Layout.partGraphic(sheet, name)
  // answers non-null for a column holding a character-part sprite id — graphic_equip is the only
  // one — and Pickers.partControl gives it the preview it otherwise has none of. It is a plain Int
  // column with no composite, so nothing else would ever route it anywhere but scalarControl.
  //
  // ONE options object, `{ column, ctx, sheet, values, effective }` — and NO separate `value`.
  // partControl needs the whole values map, not just this cell (the sprite folder comes from
  // item_slot), so a cell parameter alongside it meant the same cell arriving twice by two routes,
  // with the copy going unread in that branch. `values[column.name]` is the single route now; the
  // two were always the same value, because render() passed values[name] for the column named
  // `name`.
  //
  // `effective` is the same record with blanks resolved to their SQL defaults. It goes only to
  // controls that read OTHER columns to decide what to draw; the cell this control edits comes
  // from `values`, raw, because that is what the field must show and what a save must write back.
  function columnControl(opts) {
    var column = opts.column;
    var ctx = opts.ctx;
    var sheet = opts.sheet;
    var values = opts.values || {};
    var value = values[column.name];

    // Carried on ctx rather than as a parameter of its own, exactly as `gallery` is (see the
    // partControl call below): the group table sets it once per row and every control the row
    // builds inherits it, with no signature growing a parameter that only one caller passes.
    var idPrefix = ctx && ctx.idPrefix;

    if (column.ref && typeof Pickers !== 'undefined' && Pickers && Pickers.fkControl) {
      return Pickers.fkControl(column, value, ctx);
    }

    var part = Layout.partGraphic(sheet, column.name);
    if (part && typeof Pickers !== 'undefined' && Pickers && Pickers.partControl) {
      return Pickers.partControl({
        column: column, values: values, effective: opts.effective, ctx: ctx, spec: part,
        tintColumns: Layout.tintColumns(sheet, column.name),
        // The graphic browser, if the caller has one to name. Undefined is the normal case and
        // means "use the Gallery global"; ctx is the carrier so no signature here grows a parameter
        // that every caller but a test would leave blank.
        gallery: ctx && ctx.gallery,
      });
    }

    return scalarControl(column, value, idPrefix);
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

    // Built ONCE, here, and handed to everything that draws: the previews, the gates and the
    // controls that read a neighbouring cell. A control computing its own defaults would be a
    // second rule about what a blank cell means, free to drift from this one. app.js recomputes it
    // per edit and pushes it through ctx.onFormChange, so the same map shape arrives at build time
    // and on every keystroke.
    var effective = effectiveValues(values, schema.columns);

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

    // ROWS THAT ONLY SOME RECORDS OF THIS SHEET HAVE A USE FOR — hidden, not skipped: their inputs
    // stay in the tree so Forms.collect still reads the stored cells back verbatim, and flipping the
    // deciding cell back shows them again without a re-render.
    //
    // TWO GATES, and a sheet may be subject to both (no sheet is today):
    //   the WEARABLE gate, on item_usetype — only Armor and Weapon items are drawn on a character,
    //     so graphic_equip and item_slot are noise for every other usetype;
    //   the MONSTER BODY gate, on body_id — a body >= 100 renders alone, so the face id, the hair id
    //     and its tint, and all six equipment slots are cells the client will never read.
    // Both are `{ columns, show(values) }` here and nothing below knows which is which; each table
    // and the rule that reads it live in Layout, so app.js's save path and preview panel gate on the
    // same answer this form does.
    var wearable = Layout.wearableGate(schema.sheet);
    var monster = Layout.monsterBodyGate(schema.sheet);
    var gates = [];
    if (wearable) {
      gates.push({
        columns: wearable.columns,
        show: function (current) {
          return wearable.values.indexOf(str(current[wearable.column])) !== -1;
        },
      });
    }
    if (monster) {
      gates.push({
        columns: monster.columns,
        show: function (current) { return !Layout.isMonsterBody(schema.sheet, current); },
      });
    }
    var gatedRows = [];

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
        row.appendChild(comp ? el('label', null, Layout.labelFor(comp, name))
                             : el('label', { for: idPrefixOf(ctx && ctx.idPrefix) + name }, name));

        // The sheet is threaded from the schema being rendered rather than read off ctx, so the
        // presentation table can never be consulted for a different sheet than the one on screen.
        row.appendChild(comp
          ? Composites.control({ comp: comp, byName: byName, values: values, effective: effective,
                                 ctx: ctx, sheet: schema.sheet, gallery: ctx && ctx.gallery })
          : columnControl({ column: column, ctx: ctx, sheet: schema.sheet, values: values,
                            effective: effective }));

        row.appendChild(el('div', { class: 'error', 'data-error-for': name }));
        section.appendChild(row);
        rendered++;

        // EVERY gate that claims this column, not the first: a row two gates disagree about must
        // stay hidden, and one `hidden` flag written twice would leave whichever ran last in
        // charge. No column is claimed twice today; this is what keeps that from being a
        // precondition of the mechanism.
        var owners = gates.filter(function (g) { return g.columns.indexOf(name) !== -1; });
        if (owners.length) gatedRows.push({ row: row, gates: owners });
      });

      // A group whose every column was claimed by a composite led from an earlier group would
      // otherwise leave a heading with nothing under it.
      if (rendered) container.appendChild(section);
    });

    // Applied once from the stored values, then re-applied on every edit: every gate's deciding cell
    // is an ordinary field of this same form, so the rows follow it live — typing 150 into body_id
    // hides the face and hair rows there and then. Hidden rows keep their values (see the note
    // above), so moving the cell back and forth loses nothing; app.js's save path is what turns a
    // crossing into cleared cells, and only once the user actually saves.
    if (gatedRows.length) {
      var applyGates = function (current) {
        gatedRows.forEach(function (entry) {
          entry.row.hidden = entry.gates.some(function (g) { return !g.show(current); });
        });
      };
      // The RESOLVED record, matching what onFormChange delivers on every later edit: a gate asks
      // what this row will BE, and a blank cell is the column default, not 0.
      applyGates(effective);
      if (ctx && typeof ctx.onFormChange === 'function') ctx.onFormChange(applyGates);
    }
  }

  // Reads the form back into a name -> string map. Missing and blank both come back as ''.
  // The result holds EXACTLY the schema's columns: a stray [name] in the tree, or a key
  // Composites.collect invents, is dropped rather than smuggled into the record. Named
  // controls nested inside a composite are swept up here too, and Composites.collect runs
  // afterwards and is the authority on its own columns.
  function collect(container, schema) {
    // A plain object deliberately, unlike render's internal maps: this one is handed back to
    // the caller and travels over google.script.run, so it stays an ordinary serialisable
    // object. It is safe as one because EVERY schema column is seeded below before anything
    // reads it, so no lookup can ever fall through to Object.prototype.
    var values = {};
    schema.columns.forEach(function (c) { values[c.name] = ''; });

    // hasOwnProperty against the seeding above, not `k in values` — the question is "is this a
    // column of this sheet", and a composite may name a sub-control anything it likes.
    function keep(name, value) {
      if (Object.prototype.hasOwnProperty.call(values, name)) values[name] = str(value);
    }

    var inputs = container.querySelectorAll('[name]');
    for (var i = 0; i < inputs.length; i++) {
      keep(inputs[i].getAttribute('name'), inputs[i].value);
    }

    (schema.composites || []).forEach(function (comp) {
      var claimed = Composites.collect(comp, container);
      Object.keys(claimed).forEach(function (k) { keep(k, claimed[k]); });
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
    columnControl: columnControl,
    placeholderFor: placeholderFor,
    defaultOf: defaultOf,
    effective: effectiveValues,
    el: el,
  };
})();

if (typeof module !== 'undefined') module.exports = { Forms: Forms };
