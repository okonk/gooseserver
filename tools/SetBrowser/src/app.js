(function () {
  var STORAGE_KEY = 'goose-set-browser:v1';
  var GAME_KEY = 'goose-set-browser:game';
  var TINT_KEY = 'goose-set-browser:tint';
  var SLOTS = Sets.SLOT_ORDER;
  var SOURCES = SET_DATA.sources;
  var BUNDLES = GOOSE_SPRITES;
  var src = sourceOf(readPref(GAME_KEY));

  var ctx = { bundles: BUNDLES, images: { parts: null } };

  var TINTS = {
    lerp: { label: 'Lerp (old client)', dye: function (s, t) { return t; } },
    multiply: { label: '2× multiply', dye: function (s, t) { return Math.min(1, 2 * s * t); } },
    overlay: { label: 'Overlay', dye: function (s, t) { return s < 0.5 ? 2 * s * t : 1 - 2 * (1 - s) * (1 - t); } },
    softlight: { label: 'Soft light', dye: function (s, t) { return (1 - 2 * t) * s * s + 2 * t * s; } },
    luminance: { label: '2× luminance multiply', dye: function (s, t, l) { return Math.min(1, 2 * l * t); } },
    detail: { label: 'Lerp + shading (client)', dye: function (s, t, l, mean) { return Math.max(0, Math.min(1, t + l - mean)); } },
  };
  var tintMode = TINTS[readPref(TINT_KEY)] ? readPref(TINT_KEY) : 'detail';

  Sprites.draw = function (c, image, rect, dx, dy, tint) {
    if (!rect || !image) return;
    if (!tint || !tint.a) {
      c.drawImage(image, rect[0], rect[1], rect[2], rect[3], dx, dy, rect[2], rect[3]);
      return;
    }
    var off = document.createElement('canvas');
    off.width = rect[2];
    off.height = rect[3];
    var octx = off.getContext('2d');
    octx.drawImage(image, rect[0], rect[1], rect[2], rect[3], 0, 0, rect[2], rect[3]);
    var data = octx.getImageData(0, 0, rect[2], rect[3]);
    var px = data.data;
    var dye = TINTS[tintMode].dye;
    var f = tint.a / 255;
    var t = [tint.r / 255, tint.g / 255, tint.b / 255];
    function lumAt(i) { return (0.299 * px[i] + 0.587 * px[i + 1] + 0.114 * px[i + 2]) / 255; }
    var sum = 0;
    var n = 0;
    for (var j = 0; j < px.length; j += 4) {
      if (px[j + 3] > 0) { sum += lumAt(j); n += 1; }
    }
    var mean = n ? sum / n : 0.5;
    for (var i = 0; i < px.length; i += 4) {
      var lum = lumAt(i);
      for (var ch = 0; ch < 3; ch++) {
        var v = px[i + ch] / 255;
        px[i + ch] = Math.round(255 * (v + (dye(v, t[ch], lum, mean) - v) * f));
      }
    }
    octx.putImageData(data, 0, 0);
    c.drawImage(off, dx, dy);
  };
  var custom = load();
  var editing = blankSet();
  var activeSlot = 'Helm';

  function $(id) { return document.getElementById(id); }

  function el(tag, attrs, children) {
    var node = document.createElement(tag);
    Object.keys(attrs || {}).forEach(function (k) {
      if (k === 'text') node.textContent = attrs[k];
      else if (k.slice(0, 2) === 'on') node.addEventListener(k.slice(2), attrs[k]);
      else node.setAttribute(k, attrs[k]);
    });
    (children || []).forEach(function (c) { if (c) node.appendChild(c); });
    return node;
  }

  function sourceOf(key) {
    return SOURCES.find(function (x) { return x.key === key; }) || SOURCES[0];
  }

  function readPref(key) {
    try { return localStorage.getItem(key); } catch (e) { return null; }
  }

  function writePref(key, value) {
    try { localStorage.setItem(key, value); } catch (e) { /* preference only */ }
  }

  function gameOf(set) {
    return sourceOf(set.game).key;
  }

  function load() {
    try {
      var raw = localStorage.getItem(STORAGE_KEY);
      var parsed = raw ? JSON.parse(raw) : null;
      return parsed && Array.isArray(parsed.sets) ? parsed.sets : [];
    } catch (e) {
      return [];
    }
  }

  function persist() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ sets: custom }));
      return true;
    } catch (e) {
      return false;
    }
  }

  function blankSet() {
    return { id: null, game: src.key, name: '', classes: 0, slots: {} };
  }

  function clone(x) { return JSON.parse(JSON.stringify(x)); }

  function classNames(mask, game) { return Sets.classesOf(mask, sourceOf(game).classes); }

  function hex(s) {
    return '#' + [s.r, s.g, s.b].map(function (v) {
      return ('0' + (v | 0).toString(16)).slice(-2);
    }).join('');
  }

  function drawSet(canvas, slots, scale) {
    var equipped = Equipped.SLOTS.map(function (name) {
      var s = slots[name];
      if (!s || !(s.graphic > 0)) return Equipped.empty();
      return { graphic: s.graphic, r: s.r | 0, g: s.g | 0, b: s.b | 0, a: s.a | 0, tinted: true };
    });
    var armed = ['Shield', 'Weapon'].some(function (n) { return slots[n] && slots[n].graphic > 0; });
    Preview.character(canvas, {
      bodyId: Preview.BASE_BODY,
      bodyState: armed ? 4 : 3,
      equippedItems: Equipped.format(equipped),
    }, ctx, scale);
  }

  function preview(slots, scale) {
    var canvas = el('canvas', { class: 'stage' });
    canvas.width = Preview.CANVAS_W * scale;
    canvas.height = Preview.CANVAS_H * scale;
    if (ctx.images.parts) drawSet(canvas, slots, scale);
    return canvas;
  }

  function allSets() {
    var mine = custom.filter(function (s) { return gameOf(s) === src.key; }).map(function (s) {
      return Object.assign({ kind: 'custom', classNames: classNames(s.classes, s.game) }, s);
    });
    var auto = src.autoSets.map(function (s) { return Object.assign({ kind: 'auto' }, s); });
    return mine.concat(auto);
  }

  function setName(id) {
    var s = src.autoSets.find(function (x) { return x.id === id; });
    return s ? s.name : id;
  }

  function idRange(ids) {
    return ids.length > 1 ? '#' + ids[0] + '–' + ids[ids.length - 1] : '#' + ids[0];
  }

  // ---- Sets view -----------------------------------------------------------------------------

  function matches(set, q, cls, source, placeholders) {
    if (source !== 'all' && set.kind !== source) return false;
    if (!placeholders && set.placeholder) return false;
    if (cls === 'any' && set.classes) return false;
    if (cls !== 'all' && cls !== 'any' && !(set.classes & (1 << Number(cls)))) return false;
    if (!q) return true;
    var hay = [set.name].concat((set.items || []).map(function (i) { return i.name; }))
      .concat(SLOTS.map(function (s) { return set.slots[s] ? String(set.slots[s].graphic) : ''; }))
      .concat((set.duplicates || []).map(function (d) { return d.name; }))
      .join('\n').toLowerCase();
    return hay.indexOf(q) !== -1 || SLOTS.some(function (s) {
      return set.slots[s] && String(set.slots[s].graphic) === q;
    });
  }

  function renderSets() {
    var q = $('q').value.trim().toLowerCase();
    var cls = $('class-filter').value;
    var source = $('source-filter').value;
    var placeholders = $('show-placeholders').checked;
    var grid = $('set-grid');
    grid.textContent = '';

    var shown = allSets().filter(function (s) { return matches(s, q, cls, source, placeholders); });
    $('set-count').textContent = shown.length + ' sets';
    if (!shown.length) grid.appendChild(el('div', { class: 'empty', text: 'No sets match.' }));
    shown.forEach(function (s) { grid.appendChild(card(s)); });
  }

  function card(set) {
    var canvas = preview(set.slots, 2);

    var chips = [];
    if (set.kind === 'custom') chips.push(el('span', { class: 'chip mine', text: 'My set' }));
    (set.classNames.length ? set.classNames : ['Any class']).forEach(function (c) {
      chips.push(el('span', { class: 'chip', text: c }));
    });
    if (set.minLevel) chips.push(el('span', { class: 'chip', text: 'Lvl ' + set.minLevel }));
    if (set.mixedClasses) chips.push(el('span', { class: 'chip', text: 'Mixed classes' }));
    if (set.placeholder) chips.push(el('span', { class: 'chip', text: 'Graphic items' }));

    var byslot = {};
    (set.items || []).forEach(function (i) { byslot[i.slot] = i; });
    var rows = SLOTS.filter(function (s) { return set.slots[s]; }).map(function (s) {
      var g = set.slots[s];
      var gid = el('td', { class: 'gid', text: '#' + g.graphic });
      if (g.a > 0) {
        var sw = el('span', { class: 'swatch', title: 'tint ' + [g.r, g.g, g.b, g.a].join(',') });
        sw.style.background = hex(g);
        gid.appendChild(sw);
      }
      var item = byslot[s];
      var name = el('td', { class: 'item', title: item ? item.name + ' (item ' + item.id + ')' : '' });
      if (item) {
        name.appendChild(document.createTextNode(item.name + ' '));
        name.appendChild(el('small', { text: item.id }));
        if (set.mixedClasses) {
          name.appendChild(el('div', { class: 'item-classes',
                                       text: item.classNames.length ? item.classNames.join(' / ') : 'Any class' }));
        }
      }
      return el('tr', {}, [el('td', { text: s }), gid, name]);
    });

    var notes = [];
    if (set.duplicates && set.duplicates.length) {
      notes.push(el('div', { class: 'note', text: 'Same look: ' + set.duplicates.map(function (d) {
        return d.name + ' ' + idRange(d.itemIds);
      }).join(', ') }));
    }
    if (set.variants && set.variants.length) {
      notes.push(el('div', { class: 'note', text: 'Recolours: ' + set.variants.map(setName).join(', ') }));
    }

    var actions = set.kind === 'custom'
      ? [el('button', { text: 'Edit', onclick: function () { edit(set.id); } }),
         el('button', { class: 'danger', text: 'Delete', onclick: function () { remove(set.id); } })]
      : [el('button', { text: 'Copy to builder', onclick: function () { copyAuto(set); } })];

    return el('div', { class: 'card' }, [
      canvas,
      el('div', {}, [
        el('h3', { text: set.name || '(unnamed)' }),
        el('div', { class: 'meta' }, chips),
        el('table', { class: 'slots' }, [el('tbody', {}, rows)]),
      ].concat(notes).concat([el('div', { class: 'actions' }, actions)])),
    ]);
  }

  // ---- Builder -------------------------------------------------------------------------------

  function edit(id) {
    var s = custom.find(function (x) { return x.id === id; });
    if (!s) return;
    editing = clone(s);
    showView('builder');
    renderBuilder();
  }

  function copyAuto(set) {
    editing = { id: null, game: src.key, name: set.name + ' (copy)', classes: set.classes, slots: clone(set.slots) };
    showView('builder');
    renderBuilder();
  }

  function remove(id) {
    var s = custom.find(function (x) { return x.id === id; });
    if (!s || !confirm('Delete "' + (s.name || 'unnamed') + '"?')) return;
    custom = custom.filter(function (x) { return x.id !== id; });
    persist();
    if (editing.id === id) editing = blankSet();
    renderBuilder();
    renderSets();
  }

  function save() {
    editing.name = $('set-name').value.trim();
    if (!editing.name) { $('save-note').textContent = 'Give the set a name first.'; return; }
    if (!editing.id) editing.id = 'custom-' + Date.now().toString(36);
    editing.updatedAt = new Date().toISOString();
    var i = custom.findIndex(function (x) { return x.id === editing.id; });
    if (i === -1) custom.unshift(clone(editing)); else custom[i] = clone(editing);
    $('save-note').textContent = persist()
      ? 'Saved to this browser. Use Export JSON to keep a copy.'
      : 'Could not write to local storage — export JSON to keep this set.';
    renderBuilder();
    renderSets();
  }

  function renderBuilder() {
    $('editor-title').textContent = editing.id ? 'Editing set' : 'New set';
    $('set-name').value = editing.name || '';
    $('delete').disabled = !editing.id;

    var box = $('set-classes');
    box.textContent = '';
    var classes = sourceOf(editing.game).classes;
    Object.keys(classes).forEach(function (id) {
      var bit = 1 << Number(id);
      var input = el('input', { type: 'checkbox' });
      input.checked = !!(editing.classes & bit);
      input.addEventListener('change', function () {
        editing.classes = input.checked ? (editing.classes | bit) : (editing.classes & ~bit);
      });
      box.appendChild(el('label', {}, [input, document.createTextNode(classes[id])]));
    });

    renderSlotRows();
    redrawBig();
    renderMineList();
    renderThumbs();
  }

  function renderSlotRows() {
    var host = $('slot-rows');
    host.textContent = '';
    SLOTS.forEach(function (slot) {
      var s = editing.slots[slot] || { graphic: 0, r: 0, g: 0, b: 0, a: 0 };
      var idInput = el('input', { type: 'number', min: 0, value: s.graphic || '', placeholder: '—' });
      var color = el('input', { type: 'color', value: hex(s), title: 'Tint colour' });
      var alpha = el('input', { type: 'range', min: 0, max: 255, value: s.a || 0,
                                title: 'Tint blend (0 = untinted)' });

      function commit() {
        var graphic = parseInt(idInput.value, 10) || 0;
        var h = color.value;
        if (graphic > 0) {
          editing.slots[slot] = {
            graphic: graphic,
            r: parseInt(h.slice(1, 3), 16), g: parseInt(h.slice(3, 5), 16), b: parseInt(h.slice(5, 7), 16),
            a: parseInt(alpha.value, 10) || 0,
          };
        } else {
          delete editing.slots[slot];
        }
        redrawBig();
      }
      idInput.addEventListener('input', commit);
      color.addEventListener('input', commit);
      alpha.addEventListener('input', commit);

      host.appendChild(el('div', { class: 'slot-row' + (slot === activeSlot ? ' active' : '') }, [
        el('button', { class: 'slot-name', text: slot, title: 'Browse ' + Sets.CATEGORY[slot],
                       onclick: function () { activeSlot = slot; renderSlotRows(); renderThumbs(); } }),
        idInput, color, alpha,
        el('button', { class: 'clear', text: '×', title: 'Clear slot',
                       onclick: function () { delete editing.slots[slot]; renderSlotRows(); redrawBig(); renderThumbs(); } }),
      ]));
    });
  }

  function redrawBig() {
    var c = $('big-preview');
    if (ctx.images.parts) drawSet(c, editing.slots, 3);
  }

  function renderMineList() {
    var host = $('mine-list');
    host.textContent = '';
    var mine = custom.filter(function (s) { return gameOf(s) === src.key; });
    if (!mine.length) {
      host.appendChild(el('div', { class: 'note', text: 'Your saved ' + src.label + ' sets will appear here.' }));
      return;
    }
    mine.forEach(function (s) {
      host.appendChild(el('button', {
        class: s.id === editing.id ? 'current' : '',
        text: (s.name || '(unnamed)') + ' — ' + SLOTS.filter(function (k) { return s.slots[k]; }).length + ' slots',
        onclick: function () { edit(s.id); },
      }));
    });
  }

  function usage() {
    var mode = $('hide-filter').value;
    var used = {};
    function mark(slot, graphic, label) {
      var key = Sets.CATEGORY[slot] + ':' + graphic;
      (used[key] = used[key] || []).push(label);
    }
    allSets().forEach(function (s) {
      if (s.id === editing.id) return;
      SLOTS.forEach(function (k) { if (s.slots[k]) mark(k, s.slots[k].graphic, s.name); });
    });
    var items = {};
    src.items.forEach(function (i) {
      var key = Sets.CATEGORY[i.slot] + ':' + i.graphic;
      (items[key] = items[key] || []).push(i.name);
    });
    return { mode: mode, sets: used, items: items };
  }

  function categoryIds(category) {
    var ids = {};
    Object.keys(BUNDLES.parts.rects).forEach(function (key) {
      var parts = key.split(':');
      if (parts[0] === category && parts[2] !== 'mounted-idle-down') ids[parts[1]] = true;
    });
    return Object.keys(ids).map(Number).sort(function (a, b) { return a - b; });
  }

  function renderThumbs() {
    var category = Sets.CATEGORY[activeSlot];
    var u = usage();
    var q = $('graphic-q').value.trim();
    var host = $('thumbs');
    host.textContent = '';
    $('browser-title').textContent = category + ' → ' + activeSlot;

    var current = editing.slots[activeSlot] ? editing.slots[activeSlot].graphic : 0;
    var ids = categoryIds(category).filter(function (id) {
      var key = category + ':' + id;
      if (q && String(id).indexOf(q) === -1) return false;
      if (id === current) return true;
      if (u.mode === 'sets' && u.sets[key]) return false;
      if (u.mode === 'items' && (u.sets[key] || u.items[key])) return false;
      return true;
    });
    $('graphic-count').textContent = ids.length + ' shown';

    ids.forEach(function (id) {
      var key = category + ':' + id;
      var inSets = u.sets[key] || [];
      var byItems = u.items[key] || [];
      var slots = {};
      slots[activeSlot] = { graphic: id, r: 0, g: 0, b: 0, a: 0 };
      var canvas = preview(slots, 1);

      var title = ['#' + id];
      if (inSets.length) title.push('In sets: ' + inSets.join(', '));
      if (byItems.length) title.push('Items: ' + byItems.slice(0, 8).join(', ') + (byItems.length > 8 ? '…' : ''));

      host.appendChild(el('div', {
        class: 'thumb' + (id === current ? ' selected' : ''),
        title: title.join('\n'),
        onclick: function () { pick(id); },
      }, [
        canvas,
        el('span', { text: '#' + id }),
        el('span', { class: 'uses', text: inSets.length ? inSets.length + ' set' + (inSets.length > 1 ? 's' : '')
                                          : byItems.length ? byItems.length + ' item' + (byItems.length > 1 ? 's' : '') : 'unused' }),
      ]));
    });
    if (!ids.length) host.appendChild(el('div', { class: 'empty', text: 'Nothing left to show with this filter.' }));
  }

  function pick(id) {
    var prev = editing.slots[activeSlot] || { r: 0, g: 0, b: 0, a: 0 };
    editing.slots[activeSlot] = { graphic: id, r: prev.r, g: prev.g, b: prev.b, a: prev.a };
    renderSlotRows();
    redrawBig();
    Array.prototype.forEach.call(document.querySelectorAll('.thumb'), function (t) {
      t.classList.toggle('selected', t.querySelector('span').textContent === '#' + id);
    });
  }

  // ---- Export / import -----------------------------------------------------------------------

  function exportJson() {
    var payload = {
      exportedAt: new Date().toISOString(),
      dataGeneratedAt: SET_DATA.generatedAt,
      sources: SOURCES.map(function (x) {
        return { key: x.key, label: x.label, url: x.url, classes: x.classes, detectedSets: x.autoSets };
      }),
      customSets: custom.map(function (s) {
        return Object.assign({}, s, { game: gameOf(s), classNames: classNames(s.classes, s.game) });
      }),
    };
    var blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    var a = el('a', { href: URL.createObjectURL(blob), download: 'equipment-sets.json' });
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(function () { URL.revokeObjectURL(a.href); }, 1000);
  }

  function importJson(file) {
    file.text().then(function (text) {
      var parsed = JSON.parse(text);
      var incoming = parsed.customSets || parsed.sets || [];
      var added = 0;
      incoming.forEach(function (s) {
        if (!s || !s.slots) return;
        var copy = { id: s.id || 'custom-' + Date.now().toString(36) + added, game: sourceOf(s.game).key, name: s.name || '',
                     classes: s.classes | 0, slots: s.slots, updatedAt: s.updatedAt };
        var i = custom.findIndex(function (x) { return x.id === copy.id; });
        if (i === -1) custom.push(copy); else custom[i] = copy;
        added += 1;
      });
      persist();
      renderSets();
      renderBuilder();
      alert('Imported ' + added + ' set' + (added === 1 ? '' : 's') + '.');
    }).catch(function (e) { alert('Could not read that file: ' + e.message); });
  }

  // ---- Wiring --------------------------------------------------------------------------------

  function showView(name) {
    Array.prototype.forEach.call(document.querySelectorAll('[role="tab"]'), function (t) {
      t.setAttribute('aria-selected', String(t.dataset.view === name));
    });
    $('view-sets').hidden = name !== 'sets';
    $('view-builder').hidden = name !== 'builder';
  }

  function fillClassFilter() {
    var cf = $('class-filter');
    cf.textContent = '';
    cf.appendChild(el('option', { value: 'all', text: 'All' }));
    cf.appendChild(el('option', { value: 'any', text: 'No restriction' }));
    Object.keys(src.classes).forEach(function (id) {
      cf.appendChild(el('option', { value: id, text: src.classes[id] }));
    });
    $('source').textContent = src.autoSets.length + ' detected sets · data ' + SET_DATA.generatedAt.slice(0, 10);
  }

  function switchGame(key) {
    src = sourceOf(key);
    writePref(GAME_KEY, src.key);
    if (gameOf(editing) !== src.key) editing = blankSet();
    fillClassFilter();
    renderSets();
    renderBuilder();
  }

  function init() {
    $('boot').hidden = true;
    var tintSelect = $('tint-mode');
    Object.keys(TINTS).forEach(function (k) { tintSelect.appendChild(el('option', { value: k, text: TINTS[k].label })); });
    tintSelect.value = tintMode;
    tintSelect.addEventListener('change', function () {
      tintMode = tintSelect.value;
      writePref(TINT_KEY, tintMode);
      renderSets();
      redrawBig();
    });
    var game = $('game');
    SOURCES.forEach(function (x) { game.appendChild(el('option', { value: x.key, text: x.label })); });
    game.value = src.key;
    game.addEventListener('change', function () { switchGame(game.value); });
    fillClassFilter();

    Array.prototype.forEach.call(document.querySelectorAll('[role="tab"]'), function (t) {
      t.addEventListener('click', function () {
        showView(t.dataset.view);
        if (t.dataset.view === 'builder') renderBuilder();
      });
    });
    ['q', 'class-filter', 'source-filter', 'show-placeholders'].forEach(function (id) {
      $(id).addEventListener(id === 'q' ? 'input' : 'change', renderSets);
    });
    $('hide-filter').addEventListener('change', renderThumbs);
    $('graphic-q').addEventListener('input', renderThumbs);
    $('set-name').addEventListener('input', function () { editing.name = $('set-name').value; });
    $('save').addEventListener('click', save);
    $('new').addEventListener('click', function () { editing = blankSet(); $('save-note').textContent = ''; renderBuilder(); });
    $('delete').addEventListener('click', function () { if (editing.id) remove(editing.id); });
    $('export').addEventListener('click', exportJson);
    $('import').addEventListener('click', function () { $('import-file').click(); });
    $('import-file').addEventListener('change', function (e) {
      if (e.target.files[0]) importJson(e.target.files[0]);
      e.target.value = '';
    });

    var img = new Image();
    img.onload = function () {
      ctx.images.parts = img;
      renderSets();
      renderBuilder();
    };
    img.src = BUNDLES.parts.png;
    if (location.hash === '#builder') showView('builder');
    renderSets();
  }

  init();
})();
