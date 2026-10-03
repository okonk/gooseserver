// The Balance view: every equippable item of one slot as a table row with its stats, per-class
// score, upgrade check and Item Balance tags. Tags are edited inline; stats stay read-only.
var BalanceView = (function () {
  var TAG_SHEET = 'Item Balance';
  var EFFECT_SHEET = 'Spell Effects';

  var SLOT_OF = {
    Helmet: 'Helmet', Chest: 'Chest', Pants: 'Pants', Shoes: 'Shoes', Pauldrons: 'Pauldrons',
    Gloves: 'Gloves', Cloak: 'Cloak', Belt: 'Belt', Necklace: 'Necklace', Ring: 'Ring',
    Shield: 'Shield', OneHanded: 'Weapon', TwoHanded: 'Weapon',
  };
  var SLOT_ORDER = ['Helmet', 'Chest', 'Pants', 'Shoes', 'Pauldrons', 'Gloves', 'Cloak', 'Belt',
                    'Necklace', 'Ring', 'Shield', 'Weapon', 'Caster weapon'];
  var STATS = [['hp', 'player_hp', 'HP'], ['mp', 'player_mp', 'MP'], ['ac', 'stat_ac', 'AC'],
               ['str', 'stat_str', 'STR'], ['sta', 'stat_sta', 'STA'], ['dex', 'stat_dex', 'DEX'],
               ['int', 'stat_int', 'INT'], ['wd', 'weapon_damage', 'Dmg']];
  var EFFECT_FIELDS = ['melee_damage', 'spell_damage', 'melee_crit', 'spell_crit', 'haste',
                       'damage_reduce', 'hp_static_regen', 'mp_static_regen', 'hp_percent_regen',
                       'mp_percent_regen'];
  var SHORT = { Warrior: 'W', Rogue: 'R', Magus: 'M', Priest: 'P' };
  var GROUP_TINTS = 4;

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  function num(value) {
    var n = parseFloat(value);
    return isNaN(n) ? 0 : n;
  }

  function schemaOf(sheet) {
    return GOOSE_SCHEMA.sheets.filter(function (s) { return s.sheet === sheet; })[0] || null;
  }

  function available(schema) {
    return !!schema && schema.sheet === 'Items' && !!schemaOf(TAG_SHEET);
  }

  function valuesOf(schema, row) {
    var values = {};
    schema.columns.forEach(function (c, i) {
      values[c.name] = row && row[i] !== undefined ? str(row[i]) : '';
    });
    return values;
  }

  function enumNames(column) {
    var c = schemaOf(TAG_SHEET).columns.filter(function (x) { return x.name === column; })[0];
    return (c && c.enumNames) || [];
  }

  function defaultTag(item) {
    return {
      item_template_id: str(item.id), audience: str(num(item.classes)), profile: 'Balanced',
      profile2: 'None', power: 'Normal', power_pct: '0', group: '', step: 'Levelling',
      source: 'Common', lock: '0', note: '',
    };
  }

  // Joins Items, Spell Effects and Item Balance rows into one entry per equippable item.
  // `rows` are readSheet rows (row i is sheet row i + 2) for each of the three sheets.
  function build(itemsSchema, itemRows, effectRows, tagRows) {
    var effectSchema = schemaOf(EFFECT_SHEET);
    var tagSchema = schemaOf(TAG_SHEET);

    var effects = {};
    (effectRows || []).forEach(function (row) {
      var v = valuesOf(effectSchema, row);
      effects[str(v.spell_effect_id)] = v;
    });

    var tags = {};
    (tagRows || []).forEach(function (row, i) {
      var v = valuesOf(tagSchema, row);
      if (str(v.item_template_id) !== '') tags[str(v.item_template_id)] = { rowNumber: i + 2, values: v };
    });

    var items = [];
    (itemRows || []).forEach(function (row) {
      var v = valuesOf(itemsSchema, row);
      var slot = SLOT_OF[str(v.item_slot)];
      if (!slot || str(v.item_template_id) === '') return;

      var e = effects[str(v.spell_effect_id)] || null;
      var effect = {};
      if (e) EFFECT_FIELDS.forEach(function (f) { effect[f] = num(e[f]); });

      var item = { id: num(v.item_template_id), name: str(v.item_name), slot: slot,
                   level: num(v.min_level), classes: num(v.class_restrictions), effect: effect,
                   effectName: e ? str(e.spell_effect_name) : '' };
      STATS.forEach(function (s) { item[s[0]] = num(v[s[1]]); });

      var tag = tags[str(v.item_template_id)];
      item.tagRow = tag ? tag.rowNumber : 0;
      item.tagLoaded = tag ? tag.values : {};
      item.tagValues = tag ? Object.assign({}, tag.values) : defaultTag(item);
      applyTag(item);
      items.push(item);
    });
    return items;
  }

  function applyTag(item) {
    var t = item.tagValues;
    item.step = t.step || 'Levelling';
    item.tag = { audience: num(t.audience), profile: t.profile, profile2: t.profile2,
                 power: t.power || 'Normal', power_pct: num(t.power_pct), group: str(t.group),
                 source: t.source || 'Common' };
  }

  function slotOf(item) {
    return Balance.slotOf(item);
  }

  // [{ slot, count, failing, untagged }] in display order, for the slot list.
  function slots(items) {
    var result = Balance.check(items);
    var failing = {};
    result.failures.forEach(function (f) { failing[f.item.id] = true; });
    var bySlot = {};
    items.forEach(function (item) {
      var s = slotOf(item);
      var entry = bySlot[s] = bySlot[s] || { slot: s, count: 0, failing: 0, untagged: 0 };
      entry.count += 1;
      if (failing[item.id]) entry.failing += 1;
      if (!item.tagRow) entry.untagged += 1;
    });
    return SLOT_ORDER.filter(function (s) { return bySlot[s]; }).map(function (s) { return bySlot[s]; });
  }

  function sortRows(list) {
    return list.slice().sort(function (a, b) {
      return (Balance.stepIndex(a.step) - Balance.stepIndex(b.step)) ||
             (a.level - b.level) || (a.id - b.id);
    });
  }

  function select(name, options, value) {
    var s = Forms.el('select', { 'data-tag': name, 'aria-label': name });
    options.forEach(function (o) {
      s.appendChild(Forms.el('option', { value: o }, o));
    });
    s.value = value;
    return s;
  }

  function input(name, value, attrs) {
    return Forms.el('input', Object.assign({ type: 'text', 'data-tag': name, 'aria-label': name,
                                             value: value }, attrs || {}));
  }

  function audienceControl(bits) {
    var wrap = Forms.el('span', { class: 'audience', 'data-tag': 'audience' });
    Balance.CLASS_ORDER.forEach(function (cls) {
      var bit = Balance.CLASSES[cls].bit;
      var box = Forms.el('input', { type: 'checkbox', 'data-class-bit': String(bit),
                                    'aria-label': cls });
      box.checked = (num(bits) & bit) !== 0;
      var label = Forms.el('label', { title: cls });
      label.appendChild(box);
      label.appendChild(Forms.el('span', null, SHORT[cls]));
      wrap.appendChild(label);
    });
    return wrap;
  }

  function readRow(row) {
    var v = Object.assign({}, row.item.tagValues);
    var bits = 0;
    row.node.querySelectorAll('[data-class-bit]').forEach(function (box) {
      if (box.checked) bits += num(box.getAttribute('data-class-bit'));
    });
    v.audience = String(bits);
    row.node.querySelectorAll('[data-tag]').forEach(function (control) {
      if (control.tagName !== 'SELECT' && control.tagName !== 'INPUT') return;
      var name = control.getAttribute('data-tag');
      if (control.getAttribute('type') === 'checkbox') v[name] = control.checked ? '1' : '0';
      else v[name] = str(control.value).trim();
    });
    v.item_template_id = str(row.item.id);
    return v;
  }

  function ratioText(r) {
    if (!r || r.ratio === null) return '—';
    if (!isFinite(r.ratio)) return 'new';
    return r.ratio.toFixed(2) + '× ' + SHORT[r.cls] + ' vs ' + r.vs.name;
  }

  // Draws the table for `slot` into `container` and remembers its rows on container.__balance.
  // `onOpenItem(id)` opens an item in the Records view; `onChange` runs after any tag edit.
  function render(opts) {
    var container = opts.container;
    var all = opts.items;
    var slotItems = sortRows(all.filter(function (i) { return slotOf(i) === opts.slot; }));

    container.innerHTML = '';
    var bar = Forms.el('div', { class: 'balance-filters' });
    var onlyFailing = Forms.el('input', { type: 'checkbox', 'data-filter': 'failing',
                                          'aria-label': 'Only failing' });
    var failingLabel = Forms.el('label');
    failingLabel.appendChild(onlyFailing);
    failingLabel.appendChild(Forms.el('span', null, ' Only failing'));
    var classFilter = select('class-filter', ['All classes'].concat(Balance.CLASS_ORDER), 'All classes');
    classFilter.removeAttribute('data-tag');
    classFilter.setAttribute('data-filter', 'class');
    bar.appendChild(Forms.el('strong', null, opts.slot));
    bar.appendChild(failingLabel);
    bar.appendChild(classFilter);
    container.appendChild(bar);

    var table = Forms.el('table', { class: 'balance' });
    var head = Forms.el('tr');
    ['Item', 'Lvl', 'Classes'].concat(STATS.map(function (s) { return s[2]; }))
      .concat(['Effect', 'W', 'R', 'M', 'P', 'Upgrade', 'Audience', 'Profile', 'Profile 2',
               'Power', '%', 'Group', 'Step', 'Source', 'Lock', 'Note'])
      .forEach(function (h) { head.appendChild(Forms.el('th', null, h)); });
    table.appendChild(head);

    var groupTint = {};
    var rows = slotItems.map(function (item) {
      var t = item.tagValues;
      var classes = [];
      if (!item.tagRow) classes.push('untagged');
      var group = str(t.group);
      if (group) {
        if (groupTint[group] === undefined) groupTint[group] = Object.keys(groupTint).length % GROUP_TINTS;
        classes.push('group-' + groupTint[group]);
      }
      var tr = Forms.el('tr', { 'data-item': String(item.id), class: classes.join(' ') });

      var name = Forms.el('button', { type: 'button', class: 'link', 'data-open-item': String(item.id) },
                          '#' + item.id + ' ' + item.name);
      name.addEventListener('click', function () { if (opts.onOpenItem) opts.onOpenItem(item.id); });
      var nameCell = Forms.el('td');
      nameCell.appendChild(name);
      tr.appendChild(nameCell);
      tr.appendChild(Forms.el('td', null, String(item.level)));
      tr.appendChild(Forms.el('td', null, Balance.classesOf(item.classes).map(function (c) {
        return SHORT[c];
      }).join('') || '—'));
      STATS.forEach(function (s) {
        tr.appendChild(Forms.el('td', { class: 'num' }, item[s[0]] ? String(item[s[0]]) : ''));
      });
      tr.appendChild(Forms.el('td', { class: 'effect' }, item.effectName));
      Balance.CLASS_ORDER.forEach(function (cls) {
        tr.appendChild(Forms.el('td', { class: 'num', 'data-score': cls }));
      });
      tr.appendChild(Forms.el('td', { 'data-upgrade': '' }));

      var controls = [
        audienceControl(t.audience),
        select('profile', enumNames('profile'), t.profile || 'Balanced'),
        select('profile2', enumNames('profile2'), t.profile2 || 'None'),
        select('power', enumNames('power'), t.power || 'Normal'),
        input('power_pct', str(t.power_pct || '0'), { size: '3' }),
        input('group', str(t.group), { size: '12' }),
        select('step', enumNames('step'), t.step || 'Levelling'),
        select('source', enumNames('source'), t.source || 'Common'),
      ];
      controls.forEach(function (c) {
        var td = Forms.el('td');
        td.appendChild(c);
        tr.appendChild(td);
      });
      var lock = Forms.el('input', { type: 'checkbox', 'data-tag': 'lock', 'aria-label': 'lock' });
      lock.checked = str(t.lock) === '1';
      var lockCell = Forms.el('td');
      lockCell.appendChild(lock);
      tr.appendChild(lockCell);
      var noteCell = Forms.el('td');
      noteCell.appendChild(input('note', str(t.note), { size: '24' }));
      tr.appendChild(noteCell);

      table.appendChild(tr);
      return { item: item, node: tr, touched: false };
    });
    container.appendChild(table);

    var state = { slot: opts.slot, rows: rows, items: all };
    container.__balance = state;

    function refresh() {
      rows.forEach(function (row) {
        row.item.tagValues = readRow(row);
        applyTag(row.item);
      });
      var result = Balance.check(slotItems);
      var failures = {};
      result.failures.forEach(function (f) { (failures[f.item.id] = failures[f.item.id] || []).push(f); });

      var cls = classFilter.value;
      rows.forEach(function (row) {
        var mine = result.rows.filter(function (r) { return r.item === row.item; });
        Balance.CLASS_ORDER.forEach(function (c) {
          var cell = row.node.querySelectorAll('[data-score="' + c + '"]')[0];
          var r = mine.filter(function (x) { return x.cls === c; })[0];
          cell.textContent = r ? r.score.toFixed(0) : '';
        });
        var fail = failures[row.item.id];
        var worst = fail ? fail[0] : mine.filter(function (r) { return r.ratio !== null; })
          .sort(function (a, b) { return a.ratio - b.ratio; })[0];
        var upgrade = row.node.querySelectorAll('[data-upgrade]')[0];
        upgrade.textContent = ratioText(worst);
        upgrade.className = fail ? 'up-fail' : worst && worst.severity === 2 ? 'up-close' : worst ? 'up-ok' : '';

        var audience = Balance.audienceOf(row.item);
        row.node.hidden = (onlyFailing.checked && !fail) ||
                          (cls !== 'All classes' && audience.indexOf(cls) === -1);
      });
    }

    function onEdit(event) {
      var tr = event.target;
      while (tr && tr.getAttribute && !tr.getAttribute('data-item')) tr = tr.parentNode;
      rows.forEach(function (row) { if (row.node === tr) row.touched = true; });
      refresh();
      if (opts.onChange) opts.onChange();
    }
    table.addEventListener('input', onEdit);
    table.addEventListener('change', onEdit);
    onlyFailing.addEventListener('change', refresh);
    classFilter.addEventListener('change', refresh);

    refresh();
    return state;
  }

  function same(a, b) {
    var tagSchema = schemaOf(TAG_SHEET);
    return tagSchema.columns.every(function (c) { return str(a[c.name]) === str(b[c.name]); });
  }

  // Rows to save: a tagged row whose values differ from what was loaded, or an untagged row the
  // user has edited. Shape matches Groups.ops' input.
  function changed(container) {
    var state = container && container.__balance;
    if (!state) return [];
    return state.rows.filter(function (row) {
      var values = readRow(row);
      return row.item.tagRow ? !same(values, row.item.tagLoaded) : row.touched;
    }).map(function (row) {
      return { rowNumber: row.item.tagRow, values: readRow(row), loaded: row.item.tagLoaded };
    });
  }

  return {
    TAG_SHEET: TAG_SHEET,
    EFFECT_SHEET: EFFECT_SHEET,
    SLOT_ORDER: SLOT_ORDER,
    available: available,
    tagSchema: function () { return schemaOf(TAG_SHEET); },
    build: build,
    slots: slots,
    render: render,
    changed: changed,
  };
})();

if (typeof module !== 'undefined') module.exports = { BalanceView: BalanceView };
