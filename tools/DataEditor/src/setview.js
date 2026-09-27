// Items edited a detected equipment set at a time. Sets.detect decides the membership; each member
// is a card of the columns that make up its look, and the set is drawn worn on the base body.
var SetView = (function () {
  var FIELDS = ['item_name', 'item_slot', 'graphic_equip', 'graphic_tile', 'graphic_file',
                'graphic_r', 'graphic_g', 'graphic_b', 'graphic_a', 'min_level',
                'class_restrictions'];
  var LOOK_FIELDS = ['graphic_tile', 'graphic_file', 'graphic_equip', 'graphic_r', 'graphic_g',
                     'graphic_b', 'graphic_a'];
  var TINT_FIELDS = ['graphic_r', 'graphic_g', 'graphic_b', 'graphic_a'];
  var OVERVIEW_SCALE = 2;

  var seq = 0;

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  function pkOf(schema) {
    return schema.columns.filter(function (c) { return c.pk; })[0] || null;
  }

  function available(schema) {
    if (!schema || !pkOf(schema)) return false;
    var names = schema.columns.map(function (c) { return c.name; });
    return FIELDS.every(function (f) { return names.indexOf(f) !== -1; });
  }

  function valuesOf(schema, row) {
    var values = {};
    schema.columns.forEach(function (c, i) {
      values[c.name] = row && row[i] !== undefined ? str(row[i]) : '';
    });
    return values;
  }

  // `rows` is state.rows (row i is sheet row i + 2); `classEntries` is the Classes id + name list.
  // A "same look" duplicate is its own entry here, since its rows are separate records to edit.
  function build(schema, rows, classEntries) {
    if (!available(schema)) return [];
    var pk = pkOf(schema).name;

    var classNames = {};
    (classEntries || []).forEach(function (e) {
      if (num(e.id) > 0) classNames[num(e.id)] = str(e.name);
    });

    var records = (rows || []).map(function (row, i) {
      return { rowNumber: i + 2, values: valuesOf(schema, row) };
    });
    var byId = {};
    records.forEach(function (r) { byId[num(r.values[pk])] = r; });

    var sets = Sets.detect(records.map(function (r) {
      var v = r.values;
      return { id: v[pk], name: v.item_name, slot: v.item_slot, graphic: v.graphic_equip,
               r: v.graphic_r, g: v.graphic_g, b: v.graphic_b, a: v.graphic_a,
               classes: v.class_restrictions, minLevel: v.min_level };
    }), classNames);

    function entry(key, name, classes, ids) {
      return {
        key: key,
        label: '#' + ids[0] + ' ' + name + (classes.length ? ' — ' + classes.join(', ') : ''),
        rows: ids.filter(function (id) { return byId[id]; })
          .map(function (id) { return byId[id]; }),
      };
    }

    var out = [];
    sets.forEach(function (set) {
      out.push(entry(set.id, set.name, set.classNames,
                     set.items.map(function (i) { return i.id; })));
      set.duplicates.forEach(function (d) {
        out.push(entry('auto-' + d.itemIds[0], d.name + ' (same look as ' + set.name + ')',
                       set.classNames, d.itemIds));
      });
    });
    out.forEach(function (s) { s.count = s.rows.length; });
    return out;
  }

  function subSchema(schema) {
    return {
      sheet: schema.sheet,
      columns: FIELDS.map(function (name) {
        return schema.columns.filter(function (c) { return c.name === name; })[0];
      }),
      composites: (schema.composites || []).filter(function (comp) {
        return comp.columns.every(function (n) { return FIELDS.indexOf(n) !== -1; });
      }),
    };
  }

  function current(state, card) {
    var values = {};
    Object.keys(card.__loaded).forEach(function (k) { values[k] = card.__loaded[k]; });
    var edited = Forms.collect(card, state.sub);
    state.sub.columns.forEach(function (c) { values[c.name] = edited[c.name]; });
    return values;
  }

  function buildCard(state, record) {
    var card = Forms.el('section', { class: 'set-item', 'data-set-row': String(record.rowNumber) });
    card.__rowNumber = record.rowNumber;
    card.__loaded = record.values;
    card.__callbacks = [];

    card.addEventListener('input', function () { notify(state, card); });
    card.addEventListener('change', function () { notify(state, card); });

    fill(state, card, record.values);
    return card;
  }

  function notify(state, card) {
    var now = Forms.effective(current(state, card), state.schema.columns);
    card.__callbacks.forEach(function (fn) { fn(now); });
    if (state.onChange) state.onChange();
  }

  function copyFields(state, card, source, fields) {
    var values = current(state, card);
    fields.forEach(function (f) { values[f] = str(source[f]); });
    fill(state, card, values);
    notify(state, card);
  }

  function fill(state, card, values) {
    var schema = state.schema;
    var sub = state.sub;
    var pk = pkOf(schema).name;

    card.innerHTML = '';
    var callbacks = card.__callbacks = [];

    var head = Forms.el('div', { class: 'set-item-head' });
    head.appendChild(Forms.el('h3', null, '#' + values[pk]));
    var actions = Forms.el('div', { class: 'set-item-actions' });
    if (state.chooseItem) {
      var look = Forms.el('button', { type: 'button', 'data-copy-look': '' }, 'Copy look from…');
      look.addEventListener('click', function () {
        var slot = str(current(state, card).item_slot);
        state.chooseItem({ title: 'Copy look from a ' + (slot || 'slotless') + ' item',
                           slot: slot },
                         function (source) { copyFields(state, card, source, LOOK_FIELDS); });
      });
      actions.appendChild(look);

      var colour = Forms.el('button', { type: 'button', 'data-copy-colour': '' },
                            'Copy colour from…');
      colour.addEventListener('click', function () {
        state.chooseItem({
          title: 'Copy colour from an item in this set',
          items: state.cards.filter(function (c) { return c !== card; })
            .map(function (c) { return current(state, c); }),
        }, function (source) { copyFields(state, card, source, TINT_FIELDS); });
      });
      actions.appendChild(colour);
    }
    var all = Forms.el('button', { type: 'button', 'data-colour-all': '' },
                       'Use colour on whole set');
    all.addEventListener('click', function () {
      var source = current(state, card);
      state.cards.forEach(function (c) {
        if (c !== card) copyFields(state, c, source, TINT_FIELDS);
      });
    });
    actions.appendChild(all);
    head.appendChild(actions);
    card.appendChild(head);

    var ctx = {};
    Object.keys(state.ctx || {}).forEach(function (k) { ctx[k] = state.ctx[k]; });
    ctx.idPrefix = 's' + (seq++) + '-';
    ctx.onFormChange = function (fn) { if (typeof fn === 'function') callbacks.push(fn); };

    var byName = Object.create(null);
    sub.columns.forEach(function (c) { byName[c.name] = c; });
    var leaders = Object.create(null);
    var claimed = Object.create(null);
    sub.composites.forEach(function (comp) {
      comp.columns.forEach(function (n) { claimed[n] = comp; });
      leaders[comp.columns[0]] = comp;
    });

    var effective = Forms.effective(values, schema.columns);
    sub.columns.forEach(function (column) {
      var comp = claimed[column.name];
      if (comp && leaders[column.name] !== comp) return;

      var row = Forms.el('div', { class: 'field' });
      row.appendChild(Forms.el('label', null,
                               comp ? Layout.labelFor(comp, column.name) : column.name));
      row.appendChild(comp
        ? Composites.control({ comp: comp, byName: byName, values: values, effective: effective,
                               ctx: ctx, sheet: schema.sheet, gallery: ctx.gallery })
        : Forms.columnControl({ column: column, ctx: ctx, sheet: schema.sheet, values: values,
                                effective: effective }));
      row.appendChild(Forms.el('div', { class: 'error', 'data-error-for': column.name }));
      card.appendChild(row);
    });
  }

  /// opts: { container, schema, set, ctx, onChange, onBack, chooseItem }. onChange runs after any
  /// edit in any card; onBack adds a button back to the overview. chooseItem({ title, slot } or
  /// { title, items }, done) offers the Items rows with that item_slot, or exactly `items`, and
  /// calls done(values) with the chosen record.
  function render(opts) {
    var container = opts.container;
    container.innerHTML = '';

    var state = {
      schema: opts.schema,
      sub: subSchema(opts.schema),
      ctx: opts.ctx,
      onChange: opts.onChange,
      chooseItem: opts.chooseItem,
      cards: [],
    };
    container.__setView = state;

    if (opts.onBack) {
      var back = Forms.el('button', { type: 'button', 'data-all-sets': '' }, '← All sets');
      back.addEventListener('click', opts.onBack);
      container.appendChild(back);
    }

    var head = Forms.el('div', { class: 'group-head' });
    head.appendChild(Forms.el('h3', null, opts.set ? opts.set.label : ''));
    container.appendChild(head);

    ((opts.set && opts.set.rows) || []).forEach(function (record) {
      var card = buildCard(state, record);
      state.cards.push(card);
      container.appendChild(card);
    });
    return container;
  }

  /// [{ rowNumber, values, loaded }] for every card, in Groups.ops' shape. Columns without a
  /// control on the card carry their loaded value, so a write never blanks them.
  function collect(container) {
    var state = container.__setView;
    if (!state) return [];
    return state.cards.map(function (card) {
      return { rowNumber: card.__rowNumber, values: current(state, card), loaded: card.__loaded };
    });
  }

  function isChanged(schema, row) {
    return schema.columns.some(function (c) {
      return str(row.values[c.name]) !== str(row.loaded[c.name]);
    });
  }

  function changed(container) {
    var state = container.__setView;
    if (!state) return [];
    return collect(container).filter(function (row) { return isChanged(state.schema, row); });
  }

  function cards(container) {
    var state = container.__setView;
    return state ? state.cards.slice() : [];
  }

  function drawSet(canvas, records, ctx, scale) {
    var slots = {};
    records.forEach(function (v) {
      var slot = Sets.WEARABLE[str(v.item_slot).trim()];
      if (!slot || num(v.graphic_equip) <= 0) return;
      slots[slot] = { graphic: num(v.graphic_equip), r: num(v.graphic_r), g: num(v.graphic_g),
                      b: num(v.graphic_b), a: num(v.graphic_a), tinted: true };
    });
    var armed = !!(slots.Shield || slots.Weapon);
    return Preview.character(canvas, {
      bodyId: Preview.BASE_BODY,
      bodyState: armed ? 4 : 3,
      equippedItems: Equipped.format(Equipped.SLOTS.map(function (name) {
        return slots[name] || Equipped.empty();
      })),
    }, ctx, scale);
  }

  function drawPreview(canvas, container, ctx, scale) {
    return drawSet(canvas, collect(container).map(function (row) { return row.values; }),
                   ctx, scale);
  }

  function drawIcon(canvas, values, ctx) {
    var rect = Sprites.icon((ctx && ctx.bundles) || {}, values.graphic_file, values.graphic_tile);
    if (!rect) return;
    var c = Sprites.scaled(canvas, 1, rect[2], rect[3]);
    Sprites.draw(c, (ctx && ctx.images) ? ctx.images.icons : null, rect, 0, 0,
                 Preview.tintOf({ r: values.graphic_r, g: values.graphic_g,
                                  b: values.graphic_b, a: values.graphic_a }));
  }

  /// Every set as a clickable tile: the set worn on the base body, and each piece's icon.
  /// opts: { container, sets, ctx, onOpen(key) }. Returns a redraw for when a bundle lands.
  function overview(opts) {
    var container = opts.container;
    container.innerHTML = '';

    var filter = Forms.el('input', { type: 'text', class: 'set-filter', 'data-set-filter': '',
                                     autocomplete: 'off', placeholder: 'Filter sets…',
                                     'aria-label': 'Filter sets' });
    container.appendChild(filter);
    var grid = Forms.el('div', { class: 'set-grid' });
    container.appendChild(grid);

    var tiles = (opts.sets || []).map(function (set) {
      var tile = Forms.el('button', { type: 'button', class: 'set-tile', 'data-set-key': set.key });
      var stage = Forms.el('canvas', { class: 'worn',
                                       width: Preview.CANVAS_W * OVERVIEW_SCALE,
                                       height: Preview.CANVAS_H * OVERVIEW_SCALE });
      tile.appendChild(stage);
      tile.appendChild(Forms.el('span', { class: 'set-name' }, set.label));
      var icons = Forms.el('span', { class: 'set-icons' });
      var iconCanvases = set.rows.map(function (row) {
        var icon = Forms.el('canvas', { class: 'item-icon', width: 32, height: 32,
                                        title: row.values.item_name });
        icons.appendChild(icon);
        return icon;
      });
      tile.appendChild(icons);
      tile.addEventListener('click', function () { opts.onOpen(set.key); });
      grid.appendChild(tile);
      return { set: set, tile: tile, stage: stage, icons: iconCanvases };
    });

    filter.addEventListener('input', function () {
      var needle = str(filter.value).toLowerCase();
      tiles.forEach(function (t) {
        var text = t.set.label + ' ' + t.set.rows.map(function (r) {
          return r.values.item_name;
        }).join(' ');
        t.tile.hidden = !!needle && text.toLowerCase().indexOf(needle) === -1;
      });
    });

    function redraw() {
      tiles.forEach(function (t) {
        var values = t.set.rows.map(function (r) { return r.values; });
        drawSet(t.stage, values, opts.ctx, OVERVIEW_SCALE);
        values.forEach(function (v, i) { drawIcon(t.icons[i], v, opts.ctx); });
      });
    }
    redraw();
    return redraw;
  }

  return {
    FIELDS: FIELDS,
    LOOK_FIELDS: LOOK_FIELDS,
    available: available,
    build: build,
    render: render,
    collect: collect,
    changed: changed,
    cards: cards,
    drawPreview: drawPreview,
    overview: overview,
  };
})();

if (typeof module !== 'undefined') module.exports = { SetView: SetView };
