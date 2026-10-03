// Item balance scoring: per-class points for an equippable item, and the upgrade check across a
// slot driven by the Item Balance tags. Shared by the editor's Balance view and the balancer CLI.
var Balance = (function () {
  var CLASSES = {
    Warrior: { bit: 8, hp: 0.9, mp: 0.1, ac: 10, acDamage: 5, dex: 4, strBelow50: 50, str50: 15 },
    Rogue: { bit: 4, hp: 0.55, mp: 0.45, ac: 1, dex: 6, strBelow50: 50, str50: 15 },
    Magus: { bit: 16, hp: 0.25, mp: 0.75, ac: 0.5, dex: 2, strBelow50: 20, str50: 0 },
    Priest: { bit: 32, hp: 0.35, mp: 0.65, ac: 0.5, dex: 2, strBelow50: 35, str50: 0 },
  };
  var CLASS_ORDER = ['Warrior', 'Rogue', 'Magus', 'Priest'];
  var MELEE_BITS = 12;
  var CASTER_BITS = 48;

  var STEPS = ['Levelling', 'Punchy', 'HayFray', 'Sewers', 'Nagan', 'Savage', 'Nibbles',
               'XP20M', 'XP100M', 'XP200M', 'XP400M'];
  var FIRST_XP_STEP = STEPS.indexOf('XP20M');
  var LAST_PRE_XP_STEP = STEPS.indexOf('Nibbles');

  var SOURCE_MULTIPLIER = {
    Vendor: 0.8, Common: 1, Uncommon: 1.1, Rare: 1.2, Crafted: 1.2, RareBoss: 1.35,
    HardCraft: 1.5, Prestige: 1.7, Special: 1,
  };

  // Endgame pools: % effects are worth more once players have bought HP/MP with XP. Damage
  // shares and XP costs per point are the class spell coefficients and vita/mana costs / 50.
  var POOL = {
    Warrior: { hpShare: 0.96, mpShare: 0.04, hp: 7700, mp: 3900, xpPerHp: 2000, xpPerMp: 4000 },
    Rogue: { hpShare: 0.44, mpShare: 0.56, hp: 5200, mp: 5200, xpPerHp: 3000, xpPerMp: 3000 },
    Magus: { hpShare: 0.07, mpShare: 0.93, hp: 3852, mp: 7674, xpPerHp: 4000, xpPerMp: 2000 },
    Priest: { hpShare: 0.1, mpShare: 0.9, hp: 4300, mp: 6400, xpPerHp: 3600, xpPerMp: 2400 },
  };
  var STEP_XP = { XP20M: 2e7, XP100M: 1e8, XP200M: 2e8, XP400M: 4e8 };
  var XP_SPENT_ON_POOLS = 0.9;
  var PRE_XP_POOL = 12500;

  var UPGRADE_MARGIN = 1.1;
  var CLOSE_LEVEL_GAP = 5;
  var CLEAR_UPGRADE = 1.25;

  function num(value) {
    var n = parseFloat(value);
    return isNaN(n) ? 0 : n;
  }

  function stepIndex(step) {
    if (typeof step === 'number') return step;
    var i = STEPS.indexOf(String(step));
    return i === -1 ? 0 : i;
  }

  function classesOf(bits) {
    bits = num(bits);
    return CLASS_ORDER.filter(function (c) { return bits === 0 || (bits & CLASSES[c].bit); });
  }

  function isCasterWeapon(item) {
    var bits = num(item.classes);
    return item.slot === 'Weapon' && bits !== 0 && (bits & MELEE_BITS) === 0;
  }

  function slotOf(item) {
    return isCasterWeapon(item) ? 'Caster weapon' : item.slot;
  }

  function effectScale(cls, step) {
    var s = stepIndex(step);
    if (s < FIRST_XP_STEP) return 1;
    var p = POOL[cls];
    var w = CLASSES[cls];
    var xp = STEP_XP[STEPS[s]] * XP_SPENT_ON_POOLS;
    var pool = w.hp * (p.hp + xp * p.hpShare / p.xpPerHp) + w.mp * (p.mp + xp * p.mpShare / p.xpPerMp);
    return Math.max(1, pool / PRE_XP_POOL);
  }

  // HP-equivalent weight of one STR (and one weapon damage) from the viewpoint of a player at
  // `level` and `step`: melee stops mattering at L50 and is gone by 20M.
  function strengthWeight(cls, level, step) {
    if (num(level) < 50) return CLASSES[cls].strBelow50;
    if (stepIndex(step) <= LAST_PRE_XP_STEP) return CLASSES[cls].str50;
    return 0;
  }

  // Points for `item` to a `cls` player at `view` ({level, step}). `line` is 'damage' for the
  // Warrior damage line (half AC, no damage reduction); anything else is the default line.
  function score(item, cls, view, line) {
    var w = CLASSES[cls];
    var e = item.effect || {};
    var level = view ? view.level : item.level;
    var step = view ? view.step : item.step;
    var damageLine = cls === 'Warrior' && line === 'damage';

    var str = strengthWeight(cls, level, step);
    var weaponWeight = (!isCasterWeapon(item) || cls === 'Priest') ? str : 0;
    var acWeight = damageLine ? w.acDamage : w.ac;

    var hpEquivalent =
      2 * w.hp * (num(item.hp) + 25 * num(item.sta)) +
      2 * w.mp * (num(item.mp) + 25 * num(item.int)) +
      acWeight * num(item.ac) + w.dex * num(item.dex) +
      str * num(item.str) + weaponWeight * num(item.wd);

    var points = (num(e.melee_damage) * 600 + num(e.melee_crit) * 400 + num(e.haste) * 600) * str / 50;
    if (cls === 'Magus' || num(level) >= 50) points += num(e.spell_damage) * 500 + num(e.spell_crit) * 400;
    if (!damageLine) points += num(e.damage_reduce) * 800 * (cls === 'Magus' || cls === 'Priest' ? 0.5 : 1);
    points += num(e.hp_static_regen) / 100 * 4 + num(e.hp_percent_regen) * 1000 +
              (num(e.mp_static_regen) / 100 * 4 + num(e.mp_percent_regen) * 1000) * w.mp * 2;

    return hpEquivalent / 25 + points * effectScale(cls, step);
  }

  function linesFor(cls) {
    return cls === 'Warrior' ? ['tank', 'damage'] : [null];
  }

  function tagOf(item) {
    return item.tag || {};
  }

  function audienceOf(item) {
    var tag = tagOf(item);
    var audience = num(tag.audience);
    var wearable = classesOf(item.classes);
    if (!audience) return wearable;
    return wearable.filter(function (c) { return audience & CLASSES[c].bit; });
  }

  function multiplierOf(item) {
    var m = SOURCE_MULTIPLIER[tagOf(item).source];
    return m === undefined ? 1 : m;
  }

  function powerOf(item) {
    return tagOf(item).power || 'Normal';
  }

  // `a` is something a player would have before reaching `b`, so `b` should beat it.
  function easierThan(a, b) {
    if (a === b) return false;
    var power = powerOf(a);
    if (power === 'Special' || power === 'Exempt') return false;
    if (num(a.level) <= 1 && num(b.level) > 5) return false;
    var sa = stepIndex(a.step);
    var sb = stepIndex(b.step);
    var ma = multiplierOf(a);
    var mb = multiplierOf(b);
    if (sb >= FIRST_XP_STEP) {
      var pair = [tagOf(a).source, tagOf(b).source].sort().join('/');
      if (pair === 'Crafted/RareBoss') return false;
    }
    if (num(a.level) > num(b.level) || sa > sb || ma > mb) return false;
    return num(a.level) !== num(b.level) || sa !== sb || ma !== mb;
  }

  function groupKey(item) {
    var group = tagOf(item).group;
    return group ? 'g:' + group : 'i:' + item.id;
  }

  // One row per (item, class in its audience): its score, the best easier item and the ratio.
  function compareSlot(items) {
    var rows = [];
    items.forEach(function (b) {
      var power = powerOf(b);
      if (power === 'Exempt' || power === 'Weak') return;
      if (b.slot === 'Weapon' && !isCasterWeapon(b) && stepIndex(b.step) >= FIRST_XP_STEP) return;

      audienceOf(b).forEach(function (cls) {
        var easier = items.filter(function (a) {
          return easierThan(a, b) && classesOf(a.classes).indexOf(cls) !== -1;
        });
        var view = { level: b.level, step: b.step };
        var mine = score(b, cls, view, null);
        if (!easier.length) {
          rows.push({ item: b, cls: cls, score: mine, ratio: null, vs: null, severity: 0 });
          return;
        }

        var best = null;
        linesFor(cls).forEach(function (line) {
          var top = null;
          var topScore = -Infinity;
          easier.forEach(function (a) {
            var s = score(a, cls, view, line);
            if (s > topScore) { top = a; topScore = s; }
          });
          var ratio = topScore > 0 ? score(b, cls, view, line) / topScore : Infinity;
          if (!best || ratio > best.ratio) best = { ratio: ratio, vs: top, line: line, vsScore: topScore };
        });

        var gap = num(b.level) - num(best.vs.level);
        var target = gap < CLOSE_LEVEL_GAP ? UPGRADE_MARGIN : 1;
        var severity = (best.ratio < 1 || best.ratio < target) ? 1 : best.ratio < CLEAR_UPGRADE ? 2 : 0;
        rows.push({
          item: b, cls: cls, score: mine, ratio: best.ratio, vs: best.vs, line: best.line,
          severity: severity, target: target,
          need: target * best.vsScore / score(b, cls, view, best.line),
        });
      });
    });
    return rows;
  }

  // Applies the group rules to compareSlot rows and returns the failures that need fixing.
  // For each class, a group's best member must be the upgrade; every member must also be the
  // upgrade for at least one class in its audience. A failing group is fixed through the member
  // that class suits best relative to its other classes.
  function failures(rows) {
    var byItem = {};
    rows.forEach(function (r) {
      if (r.ratio === null) return;
      (byItem[r.item.id] = byItem[r.item.id] || []).push(r);
    });

    function affinity(r) {
      var best = Math.max.apply(null, byItem[r.item.id].map(function (x) { return x.ratio; }));
      return best > 0 ? r.ratio / best : 0;
    }
    function suited(list) {
      return list.slice().sort(function (x, y) {
        return (affinity(y) - affinity(x)) || (y.ratio - x.ratio);
      })[0];
    }

    var byGroupClass = {};
    rows.forEach(function (r) {
      if (r.ratio === null) return;
      var key = groupKey(r.item) + '|' + r.cls;
      (byGroupClass[key] = byGroupClass[key] || []).push(r);
    });

    var out = [];
    var seen = {};
    Object.keys(byGroupClass).forEach(function (key) {
      var list = byGroupClass[key];
      var top = list.reduce(function (x, y) { return y.ratio > x.ratio ? y : x; });
      if (top.severity !== 1) return;
      var pick = suited(list);
      out.push(pick);
      seen[pick.item.id] = true;
    });

    Object.keys(byItem).forEach(function (id) {
      if (seen[id]) return;
      var list = byItem[id];
      if (list.every(function (r) { return r.severity === 1; })) out.push(suited(list));
    });
    return out;
  }

  // Runs compareSlot + failures for every slot in `items`.
  function check(items) {
    var bySlot = {};
    items.forEach(function (item) {
      var slot = slotOf(item);
      (bySlot[slot] = bySlot[slot] || []).push(item);
    });
    var result = { rows: [], failures: [] };
    Object.keys(bySlot).forEach(function (slot) {
      var rows = compareSlot(bySlot[slot]);
      result.rows = result.rows.concat(rows);
      result.failures = result.failures.concat(failures(rows));
    });
    return result;
  }

  return {
    CLASSES: CLASSES,
    CLASS_ORDER: CLASS_ORDER,
    STEPS: STEPS,
    SOURCE_MULTIPLIER: SOURCE_MULTIPLIER,
    stepIndex: stepIndex,
    classesOf: classesOf,
    slotOf: slotOf,
    isCasterWeapon: isCasterWeapon,
    effectScale: effectScale,
    strengthWeight: strengthWeight,
    score: score,
    audienceOf: audienceOf,
    easierThan: easierThan,
    compareSlot: compareSlot,
    failures: failures,
    check: check,
  };
})();

if (typeof module !== 'undefined') module.exports = { Balance: Balance };
