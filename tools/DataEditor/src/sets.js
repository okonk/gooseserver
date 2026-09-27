var Sets = (function () {
  var WEARABLE = {
    Helmet: 'Helm', Chest: 'Chest', Pants: 'Legs', Shoes: 'Feet',
    Shield: 'Shield', OneHanded: 'Weapon', TwoHanded: 'Weapon',
  };

  var SLOT_ORDER = ['Helm', 'Chest', 'Legs', 'Feet', 'Shield', 'Weapon'];

  var CATEGORY = {
    Helm: 'Helms', Chest: 'Chest', Legs: 'Legs', Feet: 'Feet', Shield: 'Hands', Weapon: 'Hands',
  };

  var MAX_ID_GAP = 3;
  var INTERLEAVE_SPAN = 12;
  var FAMILY_SPAN = 200;
  var ARMOUR = toSet(['Helm', 'Chest', 'Legs', 'Feet']);

  var CLASS_WORDS = toSet(['warrior', 'rogue', 'magu', 'magus', 'priest', 'bard', 'knight', 'sorcerer']);

  var STOP = toSet([
    'of', 'the', 'a', 'graphic', 'gfx', '1h', '2h', 'cp', 'lp', 'lvl', 'w',
    'helm', 'helmet', 'hat', 'cap', 'hood', 'mask', 'crown', 'laurel', 'headband', 'bandana',
    'chest', 'chestplate', 'chestplace', 'shirt', 'tunic', 'robe', 'coat', 'cape', 'garb', 'gown',
    'jacket', 'dress', 'top', 'pant', 'legging', 'legplate', 'leg', 'boot', 'shoe', 'sandal',
    'slipper', 'feet', 'shield', 'guard', 'buckler', 'sword', 'dagger', 'staff', 'blade', 'wand',
  ]);

  function toSet(list) {
    var out = {};
    list.forEach(function (w) { out[w] = true; });
    return out;
  }

  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  function cleanName(name) {
    return String(name || '').replace(/\bgraphic\b/ig, '').replace(/\s+/g, ' ').trim();
  }

  function stem(word) {
    var w = word.toLowerCase().replace(/'s$/, '').replace(/[^a-z0-9]/g, '');
    return w.length > 3 && /s$/.test(w) && !/ss$/.test(w) ? w.slice(0, -1) : w;
  }

  function keyWords(name) {
    return cleanName(name).split(/[\s\/-]+/).map(stem).filter(function (w) {
      return w && !STOP[w];
    });
  }

  function classesOf(mask, classNames) {
    var m = num(mask);
    var out = [];
    for (var id in classNames) {
      if (m & (1 << num(id))) out.push(classNames[id]);
    }
    return out;
  }

  function normalizeItem(row) {
    var slot = WEARABLE[String(row.slot || '').trim()];
    var graphic = num(row.graphic);
    if (!slot || graphic <= 0) return null;
    var a = Math.min(255, Math.max(0, num(row.a)));
    return {
      id: num(row.id),
      name: String(row.name || '').trim(),
      itemSlot: String(row.slot).trim(),
      slot: slot,
      graphic: graphic,
      r: a ? num(row.r) : 0,
      g: a ? num(row.g) : 0,
      b: a ? num(row.b) : 0,
      a: a,
      classes: num(row.classes),
      minLevel: num(row.minLevel),
      words: keyWords(row.name),
    };
  }

  function sameTint(x, y) {
    return x.a > 0 && x.a === y.a && x.r === y.r && x.g === y.g && x.b === y.b;
  }

  function sharesWord(x, y) {
    return x.words.some(function (w) { return !CLASS_WORDS[w] && y.words.indexOf(w) !== -1; });
  }

  function linked(x, y) {
    if (x.classes !== y.classes) return false;
    if (sharesWord(x, y) || sameTint(x, y)) return true;
    return x.classes > 0 && x.minLevel > 0 && x.minLevel === y.minLevel;
  }

  function fitsIn(run, item) {
    return run.every(function (m) { return m.slot !== item.slot; }) &&
      run.some(function (m) { return linked(m, item); });
  }

  function inSlotBlock(items, i) {
    return [items[i - 1], items[i + 1]].some(function (n) {
      return n && n.slot === items[i].slot && n.classes !== items[i].classes &&
        Math.abs(n.id - items[i].id) <= 2;
    });
  }

  function interleaves(items, prior, item, index) {
    var last = prior[prior.length - 1];
    if (!inSlotBlock(items, index) || !inSlotBlock(items, items.indexOf(last))) return false;
    if (ARMOUR[item.slot] && prior.every(function (m) { return ARMOUR[m.slot]; })) return true;
    return prior.some(function (m) { return sharesWord(m, item) || sameTint(m, item); });
  }

  function runs(items) {
    var out = [];
    var run = null;
    var latestByClass = {};
    items.forEach(function (item, index) {
      var last = run && run[run.length - 1];
      if (last && item.id - last.id <= MAX_ID_GAP && fitsIn(run, item)) {
        run.push(item);
        latestByClass[item.classes] = run;
        return;
      }
      // Sheets that list one slot for every class in turn put a class's own pieces a few ids
      // apart; latestByClass holding this run means no same-class item came in between.
      var prior = item.classes > 0 && latestByClass[item.classes];
      if (prior && item.id - prior[prior.length - 1].id <= INTERLEAVE_SPAN && fitsIn(prior, item) &&
          interleaves(items, prior, item, index)) {
        prior.push(item);
        return;
      }
      run = [item];
      out.push(run);
      latestByClass[item.classes] = run;
    });
    return out;
  }

  function families(items, groups) {
    var claimed = {};
    groups.forEach(function (g) { g.forEach(function (i) { claimed[i.id] = true; }); });

    var byKey = {};
    items.forEach(function (i) {
      var lead = stems(i.name)[0];
      if (claimed[i.id] || !lead || CLASS_WORDS[lead] || STOP[lead]) return;
      var key = lead + '|' + i.classes + '|' + isPlaceholder(i);
      (byKey[key] = byKey[key] || []).push(i);
    });

    var out = [];
    Object.keys(byKey).forEach(function (key) {
      var pool = byKey[key].slice();
      for (;;) {
        var pick = SLOT_ORDER.map(function (slot) {
          return pool.find(function (i) { return i.slot === slot; });
        }).filter(Boolean);
        if (pick.filter(function (i) { return ARMOUR[i.slot]; }).length < 2) break;
        pool = pool.filter(function (i) { return pick.indexOf(i) === -1; });
        pick.sort(function (x, y) { return x.id - y.id; });
        var span = pick[pick.length - 1].id - pick[0].id;
        var oneLevel = pick.every(function (i) { return i.minLevel === pick[0].minLevel; });
        if (span <= FAMILY_SPAN && oneLevel) out.push(pick);
      }
    });
    return out;
  }

  function wordsOf(name) {
    return cleanName(name).split(' ');
  }

  function majorityPrefix(names) {
    var lists = names.map(wordsOf);
    var need = Math.max(2, Math.ceil(names.length / 2));
    var best = '';
    for (var n = 1; n <= 6; n++) {
      var counts = {};
      lists.forEach(function (ws) {
        if (ws.length <= n) return;
        var key = ws.slice(0, n).join(' ');
        counts[key] = (counts[key] || 0) + 1;
      });
      var found = Object.keys(counts).filter(function (k) { return counts[k] >= need; })[0];
      if (!found) break;
      best = found;
    }
    return best;
  }

  function majoritySuffix(names) {
    var need = Math.max(2, Math.ceil(names.length / 2));
    var counts = {};
    names.forEach(function (name) {
      var m = / of (?:the )?(.+)$/i.exec(cleanName(name));
      if (m) counts[m[1]] = (counts[m[1]] || 0) + 1;
    });
    return Object.keys(counts).filter(function (k) { return counts[k] >= need; })[0] || '';
  }

  function commonWord(names) {
    var counts = {};
    var display = {};
    names.forEach(function (name) {
      var seen = {};
      wordsOf(name).forEach(function (w) {
        var s = stem(w);
        if (!s || STOP[s] || seen[s]) return;
        seen[s] = true;
        counts[s] = (counts[s] || 0) + 1;
        display[s] = display[s] || w;
      });
    });
    var best = Object.keys(counts).sort(function (x, y) { return counts[y] - counts[x]; })[0];
    return best && counts[best] >= 2 ? display[best] : '';
  }

  function nameFor(items) {
    var names = items.map(function (i) { return i.name; });
    var found = majorityPrefix(names) || majoritySuffix(names) || commonWord(names);
    if (found) return found.replace(/'s$/, '');
    var leads = [];
    names.forEach(function (n) {
      var w = cleanName(n).split(' ')[0].replace(/'s$/, '');
      if (leads.indexOf(w) === -1) leads.push(w);
    });
    return leads.length <= 3 ? leads.join(' / ') : leads.slice(0, 3).join(' / ') + ' …';
  }

  function stems(name) {
    return cleanName(name).split(/[\s\/-]+/).map(stem).filter(Boolean);
  }

  function carries(item, name) {
    var want = stems(name);
    var have = stems(item.name);
    return want.length > 0 && want.every(function (w, i) { return have[i] === w; });
  }

  function isPlaceholder(item) {
    return /\bgraphic\b/i.test(item.name);
  }

  function completeByName(groups, items) {
    var claimed = {};
    groups.forEach(function (g) { g.forEach(function (i) { claimed[i.id] = true; }); });

    return groups.map(function (group) {
      var name = nameFor(group);
      var first = group[0];
      var classNamed = stems(name).every(function (w) { return CLASS_WORDS[w]; });
      return slotEntries(group).map(function (entry) {
        if (entry.item && carries(entry.item, name)) return entry.item;
        if (classNamed && (entry.slot === 'Shield' || entry.slot === 'Weapon')) return entry.item;
        var best = null;
        items.forEach(function (c) {
          if (claimed[c.id] || c.slot !== entry.slot || c.classes !== first.classes) return;
          if (isPlaceholder(c) !== isPlaceholder(first) || !carries(c, name)) return;
          if (!best || Math.abs(c.id - first.id) < Math.abs(best.id - first.id)) best = c;
        });
        if (!best) return entry.item;
        claimed[best.id] = true;
        return best;
      }).filter(Boolean).sort(function (x, y) { return x.id - y.id; });
    });
  }

  function mergeSameName(groups, strict) {
    var out = [];
    groups.forEach(function (g) {
      var name = nameFor(g);
      var into = out.find(function (o) {
        return o.name === name && (!strict || o.items[0].classes === g[0].classes) &&
          isPlaceholder(o.items[0]) === isPlaceholder(g[0]) &&
          o.items.every(function (m) { return g.every(function (i) { return i.slot !== m.slot; }); });
      });
      if (into) into.items = into.items.concat(g).sort(function (x, y) { return x.id - y.id; });
      else out.push({ name: name, items: g });
    });
    return out.map(function (o) { return o.items; });
  }

  function mergeMirrors(groups) {
    var shapes = {};
    groups.forEach(function (g) {
      shapes[g[0].classes + '#' + signature(slotsOf(g), false)] = true;
    });
    var used = {};
    var out = [];
    groups.forEach(function (g, i) {
      if (used[i]) return;
      var partner = -1;
      groups.forEach(function (h, j) {
        if (partner !== -1 || j <= i || used[j]) return;
        if (h[0].classes !== g[0].classes || h[0].minLevel !== g[0].minLevel) return;
        if (!g.concat(h).every(function (x) { return ARMOUR[x.slot]; })) return;
        if (!g.every(function (x) { return h.every(function (y) { return x.slot !== y.slot; }); })) return;
        if (shapes[g[0].classes + '#' + signature(slotsOf(g.concat(h)), false)]) partner = j;
      });
      if (partner === -1) { out.push(g); return; }
      used[partner] = true;
      out.push(g.concat(groups[partner]).sort(function (x, y) { return x.id - y.id; }));
    });
    return out;
  }

  function slotEntries(group) {
    return SLOT_ORDER.map(function (slot) {
      return { slot: slot, item: group.find(function (i) { return i.slot === slot; }) || null };
    });
  }

  function slotsOf(items) {
    var slots = {};
    items.forEach(function (i) {
      slots[i.slot] = { graphic: i.graphic, r: i.r, g: i.g, b: i.b, a: i.a };
    });
    return slots;
  }

  function signature(slots, withTint) {
    return SLOT_ORDER.filter(function (s) { return slots[s]; }).map(function (s) {
      var x = slots[s];
      return s + ':' + x.graphic + (withTint ? ':' + [x.r, x.g, x.b, x.a].join(',') : '');
    }).join('|');
  }

  function detect(rows, classNames) {
    var items = rows.map(normalizeItem).filter(Boolean)
      .sort(function (x, y) { return x.id - y.id; });

    var sets = [];
    var bySignature = {};

    var groups = runs(items).filter(function (run) { return run.length >= 2; });
    groups = mergeSameName(groups.concat(families(items, groups))
      .sort(function (x, y) { return x[0].id - y[0].id; }), true);

    mergeMirrors(mergeSameName(completeByName(groups, items), false)).forEach(function (run) {
      var slots = slotsOf(run);
      var sig = signature(slots, true);
      var members = run.map(function (i) {
        return { id: i.id, name: i.name, itemSlot: i.itemSlot, slot: i.slot,
                 graphic: i.graphic, r: i.r, g: i.g, b: i.b, a: i.a,
                 classes: i.classes, classNames: classesOf(i.classes, classNames || {}) };
      });
      var mixedClasses = run.some(function (i) { return i.classes !== run[0].classes; });
      var classes = mixedClasses
        ? run.reduce(function (acc, i) { return acc | i.classes; }, 0)
        : run[0].classes;

      if (bySignature[sig]) {
        bySignature[sig].duplicates.push({
          name: nameFor(run),
          itemIds: members.map(function (m) { return m.id; }),
        });
        return;
      }

      var set = {
        id: 'auto-' + run[0].id,
        name: nameFor(run),
        classes: classes,
        classNames: classesOf(classes, classNames || {}),
        mixedClasses: mixedClasses,
        minLevel: run[0].minLevel,
        placeholder: run.every(isPlaceholder),
        items: members,
        slots: slots,
        duplicates: [],
        variants: [],
      };
      bySignature[sig] = set;
      sets.push(set);
    });

    var byShape = {};
    sets.forEach(function (s) {
      var shape = signature(s.slots, false);
      (byShape[shape] = byShape[shape] || []).push(s);
    });
    sets.forEach(function (s) {
      s.variants = byShape[signature(s.slots, false)]
        .filter(function (o) { return o !== s; })
        .map(function (o) { return o.id; });
    });

    return sets;
  }

  return {
    detect: detect,
    normalizeItem: normalizeItem,
    nameFor: nameFor,
    classesOf: classesOf,
    signature: signature,
    SLOT_ORDER: SLOT_ORDER,
    CATEGORY: CATEGORY,
    WEARABLE: WEARABLE,
  };
})();

if (typeof module !== 'undefined') module.exports = { Sets: Sets };
