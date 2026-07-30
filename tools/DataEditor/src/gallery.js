// The graphic browser. Picking a graphic used to mean knowing its number, and there are 4,827
// icons — so this is a modal grid over a whole sprite bundle, filterable, that writes the cells
// back through a callback.
//
// THREE THINGS ABOUT IT ARE NOT NEGOTIABLE, and each is a decision the alternative silently loses:
//
//   1. TILES ARE CSS ATLAS WINDOWS, NOT CANVASES. Each tile is a <button> whose background is the
//      bundle PNG, offset to the sprite's rect. The browser composites them; there is no per-tile
//      canvas and no per-tile drawImage, which is what makes a thousand of them cheap. The PNG data
//      URI is named ONCE, in an injected <style> rule — copying a 1.7MB URI into 4,827 inline
//      styles would be several gigabytes of attribute text.
//   2. WINDOWING IS NOT OPTIONAL. 4,827 tiles in the DOM is a hang inside an Apps Script iframe.
//      Only the rows intersecting the viewport plus two rows of overscan are built; two spacer
//      divs carry the rest of the height so the scrollbar is honest.
//   3. THE MODAL IS LEFT EMPTY ON CLOSE. A hidden modal holding 4,827 stale nodes costs the same
//      memory as one on screen, and app.js rebuilds the form for every record opened.
//
// TILES ARE DRAWN UNTINTED, as decided for this round: the gallery answers "which sprite is this
// id", and a tint belongs to the record, not to the atlas.
//
// SIZING. Every cell is CELL square and every tile is scaled to FIT it, at no more than 2x. The
// median icon is 32x32, so the common case is exactly the 2x pixelated tile the design asked for;
// the 128x128 outliers scale down instead of being clipped to an unrecognisable top-left corner,
// which for a browser whose whole job is recognition would be the wrong trade. A uniform cell is
// also what makes the row a scroll offset lands on pure arithmetic — no measurement, no per-filter
// row height to keep in step with the CSS.
var Gallery = (function () {
  // Four columns of 64px cells is 272px with the gaps, which fits the ~300px Sheets sidebar — the
  // narrower of the two shipped entry points — without the grid scrolling sideways.
  var COLUMNS = 4;
  var CELL = 64;
  var GAP = 4;
  var ROW_HEIGHT = CELL + GAP;
  var MAX_SCALE = 2;
  var OVERSCAN = 2;

  // What a viewport of 0 means. There is no layout before the modal is shown (and none at all in
  // the test DOM), so the first render has no clientHeight to read; 480 is a plausible dialog and
  // the first scroll event corrects it. Too SMALL would be the dangerous direction — it would
  // render fewer rows than the user can see.
  var DEFAULT_VIEWPORT = 480;

  var HEADINGS = {
    icons: 'Inventory icons',
    parts: 'Character parts',
    effects: 'Effect animations',
  };

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  // parseInt(v, 10), matching Sprites.num() and the rest, so no two modules can disagree about
  // what a cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  function el(tag, attrs, text) {
    return Forms.el(tag, attrs, text);
  }

  function rectsOf(bundle) {
    return (bundle && bundle.rects) || {};
  }

  // --- the indexes ------------------------------------------------------------------------------
  //
  // One entry per TILE, carrying the rect to draw, the fields the filters read, and `pick` — the
  // object onPick receives. `pick` is built here rather than at click time so that "what does a
  // pick write" is a property of the index, and testable without a DOM.
  //
  // EVERY FIELD IS A STRING, including the ids: they go straight into text inputs that hold
  // spreadsheet cells, and a Number round-tripping through String() at the last moment would be
  // one more place for the two to disagree.

  function iconEntries(bundle) {
    var rects = rectsOf(bundle);
    var out = [];
    Object.keys(rects).forEach(function (key) {
      // "<sheet>:<graphic>", from Bundles.cs. Anything else is not an icon key; skipping rather
      // than coercing keeps a future third scheme out of the grid instead of listing it as
      // sheet NaN.
      var bits = key.split(':');
      if (bits.length !== 2) return;
      out.push({
        // `graphic` AND `id` hold the same string on purpose, and neither is spare: filterEntries's
        // id search reads e.id across all three bundles, and indexOf compares an icon on e.graphic
        // because that is the cell name the caller round-trips.
        key: key, rect: rects[key], sheet: bits[0], graphic: bits[1], id: bits[1],
        label: 'sheet ' + bits[0] + ' graphic ' + bits[1],
        pick: { sheet: bits[0], graphic: bits[1] },
      });
    });
    return out.sort(function (a, b) {
      return num(a.sheet) - num(b.sheet) || num(a.graphic) - num(b.graphic);
    });
  }

  /// The 125 sheet files with their counts, in NUMERIC order — '104' before '1006', which a string
  /// sort gets backwards.
  ///
  /// Takes the ENTRIES, not the bundle: open() has already built and sorted them, and re-indexing
  /// 4,827 keys to count them would be the second full pass in a module whose opening argument is
  /// that the naive approach is too expensive. Still pure — it reads the array and nothing else.
  function iconSheets(entries) {
    var counts = Object.create(null);
    entries.forEach(function (e) {
      counts[e.sheet] = (counts[e.sheet] || 0) + 1;
    });
    return Object.keys(counts)
      .sort(function (a, b) { return num(a) - num(b); })
      .map(function (sheet) { return { sheet: sheet, count: counts[sheet] }; });
  }

  function partEntries(bundle) {
    var rects = rectsOf(bundle);
    // DEDUPLICATED BY category:id ACROSS THE FOUR CLIPS. Without it every part would appear up to
    // four times, and the duplicates would differ only in a pose the browser does not show.
    var byId = Object.create(null);
    Object.keys(rects).forEach(function (key) {
      var bits = key.split(':');
      if (bits.length !== 3) return;
      byId[bits[0] + ':' + bits[1]] = { category: bits[0], id: bits[1] };
    });

    // The clip SHOWN is the one Sprites.clipCandidates would pick for an unarmed resting pose —
    // the same fallback chain the previews use, so a tile cannot show art the form then declines
    // to draw. Unarmed rather than armed because that is the default pose; the Hands sheets, which
    // only ever ship idle-equip, fall through to the third candidate rather than being dropped.
    var candidates = Sprites.clipCandidates(false);
    var out = [];
    Object.keys(byId).forEach(function (compound) {
      var at = byId[compound];
      for (var i = 0; i < candidates.length; i++) {
        var rect = rects[at.category + ':' + at.id + ':' + candidates[i]];
        if (!rect) continue;
        out.push({
          key: at.category + ':' + at.id + ':' + candidates[i], rect: rect,
          category: at.category, id: at.id,
          label: at.category + ' ' + at.id,
          pick: { category: at.category, id: at.id },
        });
        return;
      }
      // No candidate clip at all means the only art for this id is a mounted pose, which is not
      // what any of the six equip slots draws. Listing it would offer a sprite the form cannot
      // show — Sprites.part deliberately never falls back to a mounted clip.
    });

    return out.sort(function (a, b) {
      if (a.category !== b.category) return a.category < b.category ? -1 : 1;
      return num(a.id) - num(b.id);
    });
  }

  /// The 8 categories with their counts, alphabetically — which is also the order the folders are
  /// listed in everywhere else (Appearance.CATEGORY, the bundle config).
  ///
  /// Takes the ENTRIES for the reason iconSheets does.
  function partCategories(entries) {
    var counts = Object.create(null);
    entries.forEach(function (e) {
      counts[e.category] = (counts[e.category] || 0) + 1;
    });
    return Object.keys(counts).sort().map(function (category) {
      return { category: category, count: counts[category] };
    });
  }

  /// One tile per effect, at frame 0 — the resting frame every effect has (Sprites.effectFrames
  /// numbers them contiguously from 0). The gallery does not animate: 560 running animations in
  /// one grid is the hang windowing exists to prevent, arriving by another route.
  function effectEntries(bundle) {
    var rects = rectsOf(bundle);
    var out = [];
    Object.keys(rects).forEach(function (key) {
      var bits = key.split(':');
      if (bits.length !== 2 || bits[1] !== '0') return;
      out.push({
        key: key, rect: rects[key], id: bits[0],
        label: 'effect ' + bits[0],
        pick: { id: bits[0] },
      });
    });
    return out.sort(function (a, b) { return num(a.id) - num(b.id); });
  }

  function entriesFor(name, bundle) {
    if (name === 'parts') return partEntries(bundle);
    if (name === 'effects') return effectEntries(bundle);
    return iconEntries(bundle);
  }

  // --- filtering --------------------------------------------------------------------------------

  /// Keeps the entries matching every stated part of `f`. An absent part is not a filter.
  ///
  /// THE ID SEARCH IS SCOPED TO THE CHOSEN SHEET OR CATEGORY, because it is applied to the already
  /// narrowed list: an icon id means nothing without its sheet (the bundle key is sheet:graphic),
  /// and a Feet id searched out of a Helms browser would be a sprite the caller's slot cannot use.
  ///
  /// A PREFIX, not a substring, matching Pickers.search: '2' listing 12 and 200 buries the exact
  /// hit under everything that merely contains the digit.
  function filterEntries(entries, f) {
    var filter = f || {};
    var query = str(filter.query).trim();
    return entries.filter(function (e) {
      if (filter.sheet !== undefined && filter.sheet !== null && str(filter.sheet) !== ''
          && num(e.sheet) !== num(filter.sheet)) return false;
      if (filter.category && e.category !== filter.category) return false;
      if (query !== '' && str(e.id).indexOf(query) !== 0) return false;
      return true;
    });
  }

  // --- windowing --------------------------------------------------------------------------------

  /// Which slice of `count` entries to build for a scroll offset, and the two spacer heights that
  /// stand in for the rest. Pure arithmetic over a uniform row height — no element is measured, so
  /// this is the whole of the windowing logic and it is testable without a layout engine.
  ///
  /// The three heights always add up to rowCount * ROW_HEIGHT, whatever the offset. They have to: a
  /// spacer that came out short would make the scrollbar jump under the user's thumb on every
  /// re-render, and a spacer that came out short by a DIFFERENT amount each time would do it
  /// visibly.
  ///
  /// That total is one GAP taller than the grid actually is — the real grid is
  /// rowCount * ROW_HEIGHT - GAP, since there is no trailing gap after the last row. Constant, 4px,
  /// and identical on every render, so it cannot make the scrollbar move; it just means the scroller
  /// ends 4px below its content.
  ///
  /// `rowCount` and `firstRow` are returned for tests to assert against; render() reads only
  /// first/last/topPad/bottomPad, so nothing in production depends on them.
  function windowFor(count, scrollTop, viewport) {
    var rowCount = Math.ceil(count / COLUMNS);
    var view = viewport > 0 ? viewport : DEFAULT_VIEWPORT;
    // CLAMPED TO THE CONTENT, at both ends. A browser clamps scrollTop itself, but a list that has
    // just SHRUNK (the search narrowed it) leaves a stale offset past its new end — and an
    // unclamped one would put the whole window past the last row and render nothing at all, with a
    // topPad covering the emptiness so it looked like a loaded grid.
    var maxTop = Math.max(0, rowCount * ROW_HEIGHT - view);
    var top = Math.min(Math.max(0, scrollTop || 0), maxTop);

    var firstVisible = Math.floor(top / ROW_HEIGHT);
    var lastVisible = Math.ceil((top + view) / ROW_HEIGHT);

    var firstRow = Math.max(0, firstVisible - OVERSCAN);
    var lastRow = Math.min(rowCount, lastVisible + OVERSCAN);

    return {
      rowCount: rowCount,
      firstRow: firstRow,
      first: firstRow * COLUMNS,
      last: Math.min(count, lastRow * COLUMNS),
      topPad: firstRow * ROW_HEIGHT,
      bottomPad: (rowCount - lastRow) * ROW_HEIGHT,
    };
  }

  // How a sprite's rect becomes a background window. `k` scales the WHOLE atlas, so the offset
  // scales with it; the tile is then centred in the cell by adding the leftover half.
  //
  // Capped at MAX_SCALE so a 4x4 sprite is not blown up to fill 64 pixels, and floored by the fit
  // so a 128x128 one is not clipped.
  function tileStyle(bundle, rect) {
    var w = rect[2] || 1;
    var h = rect[3] || 1;
    var k = Math.min(MAX_SCALE, CELL / w, CELL / h);
    var padX = (CELL - w * k) / 2;
    var padY = (CELL - h * k) / 2;
    return {
      size: (bundle.width * k) + 'px ' + (bundle.height * k) + 'px',
      position: (padX - rect[0] * k) + 'px ' + (padY - rect[1] * k) + 'px',
    };
  }

  // --- the dialog -------------------------------------------------------------------------------

  // The one open instance, or null. Module-global because there is one #modal: a second open must
  // replace the first rather than nest inside it, and the bare close() below has to work for a
  // caller that never held a handle.
  //
  // NOTHING IN app.js CLOSES IT, and it does not need to: the modal is a fixed backdrop over the
  // whole viewport, so while it is up there is no record button, no sheet picker and no Save to
  // click. If a future layout lets the page be reached behind it, clearForm() is where the close
  // belongs — a dialog whose onPick writes into a form that has since been rebuilt would be
  // writing into detached inputs.
  var live = null;

  function close() {
    if (live) live.close(true);
  }

  /// Gallery.open({ bundle, bundles, filter, current, onPick, opener })
  ///
  ///   bundle  — 'icons' | 'parts' | 'effects'
  ///   bundles — the GOOSE_SPRITES map, i.e. ctx.bundles. Passed rather than read as a global so a
  ///             caller can open the browser over a bundle that has not been installed globally,
  ///             and so a test needs no global at all.
  ///   filter  — { sheet } for icons, { category, locked } for parts. `locked` means the caller
  ///             OWNS the category (an item's item_slot, an equip slot's name) and no chooser is
  ///             rendered: picking a Helms sprite for a Feet slot is never right.
  ///   current — the same shape onPick reports, so a caller round-trips its own cells: the matching
  ///             tile opens selected and scrolled to.
  ///   onPick  — receives the identifying parts of the key and nothing else, so the caller writes
  ///             whatever cells it owns: icons { sheet, graphic }, parts { category, id },
  ///             effects { id }. Icons give BOTH cells, which is what makes "graphic and sheet must
  ///             both be set" hard to trip from the browser.
  ///   opener  — the Browse button focus returns to on close. Passed rather than read from
  ///             document.activeElement, which nothing in this editor's test DOM models.
  function open(options) {
    var opts = options || {};
    // A second open REPLACES the first, and does not hand focus back to the first opener: the user
    // has moved on, and throwing focus at the button they came from would take it off the dialog
    // they just asked for.
    if (live) live.close(false);

    var host = document.getElementById('modal');
    if (!host) return null;

    var bundleName = str(opts.bundle) || 'icons';
    var bundle = (opts.bundles || {})[bundleName] || null;
    var all = entriesFor(bundleName, bundle);
    var onPick = typeof opts.onPick === 'function' ? opts.onPick : null;
    var opener = opts.opener || null;
    var wanted = opts.filter || {};
    var locked = bundleName === 'parts' && !!wanted.locked && !!wanted.category;

    var titleId = 'gal-title';
    host.innerHTML = '';
    host.hidden = false;

    // ONE copy of the atlas data URI, for every tile. Injected as a rule rather than set per tile:
    // the icons URI is 1.7MB, and 4,827 inline copies of it is not a style attribute, it is an
    // out-of-memory.
    if (bundle && bundle.png) {
      host.appendChild(el('style', { 'data-gal': 'atlas' },
        // The selector is built from the host's OWN id rather than spelled again: the tests assert
        // the URI is in the rule text, not that the selector matches anything, so a host that moved
        // would leave every tile without art and no test would see it.
        '#' + host.id + ' .gal-tile { background-image: url("' + bundle.png + '"); }'));
    }

    var dialog = el('div', {
      'data-gal': 'dialog', class: 'gal', role: 'dialog', 'aria-modal': 'true',
      'aria-labelledby': titleId,
    });

    var head = el('div', { class: 'gal-head' });
    head.appendChild(el('h2', { id: titleId, 'data-gal': 'title' }, HEADINGS[bundleName]
      || HEADINGS.icons));

    // The chooser, which is a different control per bundle — and for a locked category, NO control
    // at all. A disabled <select> that cannot change is a control that lies about being one; the
    // honest rendering of "the caller decided this" is text saying which.
    var sheetChooser = null;
    var categoryChooser = null;

    if (bundleName === 'icons') {
      sheetChooser = el('select', { 'data-gal': 'sheet', 'aria-label': 'sheet file' });
      iconSheets(all).forEach(function (s) {
        sheetChooser.appendChild(el('option', { value: s.sheet },
          s.sheet + ' (' + s.count + ')'));
      });
      // Defaulted to the sheet the record already names, so opening the browser from an item shows
      // that item's neighbourhood rather than whichever sheet sorts first. An unknown sheet — a
      // blank cell, or one naming a file the bundle does not have — leaves the select on its first
      // option rather than on nothing, which would read back as '' and filter the grid to empty.
      if (str(wanted.sheet) !== '') sheetChooser.value = str(num(wanted.sheet));
      if (sheetChooser.value === '' && sheetChooser.getElementsByTagName('option').length) {
        sheetChooser.value = sheetChooser.getElementsByTagName('option')[0].value;
      }
      head.appendChild(sheetChooser);
    } else if (bundleName === 'parts' && locked) {
      head.appendChild(el('span', { 'data-gal': 'locked', class: 'gal-locked' },
        wanted.category + ' only — the slot decides'));
    } else if (bundleName === 'parts') {
      categoryChooser = el('select', { 'data-gal': 'category', 'aria-label': 'category' });
      partCategories(all).forEach(function (c) {
        categoryChooser.appendChild(el('option', { value: c.category },
          c.category + ' (' + c.count + ')'));
      });
      if (wanted.category) categoryChooser.value = wanted.category;
      head.appendChild(categoryChooser);
    }

    var search = el('input', {
      'data-gal': 'search', type: 'text', autocomplete: 'off', placeholder: 'id',
      'aria-label': 'search by id',
    });

    var closeButton = el('button', { 'data-gal': 'close', type: 'button', class: 'gal-close' },
      'Close');
    head.appendChild(search);
    head.appendChild(closeButton);

    // aria-activedescendant, not a roving tabindex: windowing destroys and rebuilds tiles as the
    // grid scrolls, so focus parked on a tile would end up on a detached node with nowhere to go.
    // Focus stays on the scroller and an attribute says which tile is current — the same idiom, for
    // the same reason, as Pickers.fkControl's result list.
    var scroll = el('div', {
      'data-gal': 'scroll', class: 'gal-scroll', tabindex: '0', role: 'listbox',
      'aria-label': (HEADINGS[bundleName] || HEADINGS.icons) + ', arrow keys move, Enter picks',
    });
    var padTop = el('div', { 'data-gal': 'pad-top', role: 'presentation' });
    var grid = el('div', { 'data-gal': 'grid', class: 'gal-grid', role: 'presentation' });
    var padBottom = el('div', { 'data-gal': 'pad-bottom', role: 'presentation' });
    scroll.appendChild(padTop);
    scroll.appendChild(grid);
    scroll.appendChild(padBottom);

    var count = el('div', { 'data-gal': 'count', class: 'gal-count', 'aria-live': 'polite' });

    dialog.appendChild(head);
    dialog.appendChild(scroll);
    dialog.appendChild(count);
    host.appendChild(dialog);

    // The filtered list, and which index in it is current. Rebuilt by refilter(), which is the
    // only writer of either.
    var shown = [];
    var cursor = 0;
    // The list the count is measured against: everything the user could reach by working the
    // controls, which for a locked category is that category alone.
    var reachable = locked ? filterEntries(all, { category: wanted.category }) : all;

    function viewport() {
      return scroll.clientHeight > 0 ? scroll.clientHeight : DEFAULT_VIEWPORT;
    }

    function currentFilter() {
      return {
        sheet: sheetChooser ? sheetChooser.value : undefined,
        category: locked ? wanted.category : (categoryChooser ? categoryChooser.value : undefined),
        query: search.value,
      };
    }

    function describe() {
      if (!bundle) {
        count.textContent = 'the ' + bundleName + ' sprite bundle has not loaded — nothing to show';
        return;
      }
      // "of" WHAT THE USER CAN REACH, not what the bundle holds. Every other filter here is a
      // control they can change, so the whole bundle is the honest total — but a LOCKED category
      // is one they cannot leave, and "2 of 3" in a Helms browser counts two sprites that no
      // amount of clicking will show.
      count.textContent = shown.length + ' of ' + reachable.length + ' — click a tile to use it';
    }

    function render() {
      var w = windowFor(shown.length, scroll.scrollTop, viewport());
      padTop.style.height = w.topPad + 'px';
      padBottom.style.height = w.bottomPad + 'px';
      // Drops the previous tiles and, with them, their click handlers: the nodes are unreachable
      // afterwards, so nothing is left listening and nothing leaks.
      grid.innerHTML = '';

      for (var i = w.first; i < w.last; i++) {
        var entry = shown[i];
        var tile = el('button', {
          'data-gal': 'tile', 'data-id': entry.id, type: 'button', class: 'gal-tile',
          id: 'gal-tile-' + i, role: 'option', tabindex: '-1',
          title: entry.label, 'aria-label': entry.label,
        });
        if (bundle) {
          var s = tileStyle(bundle, entry.rect);
          tile.style.backgroundSize = s.size;
          tile.style.backgroundPosition = s.position;
        }
        if (i === cursor) tile.setAttribute('aria-selected', 'true');
        // `at` rather than `i`: the loop variable is function-scoped in ES5, so every handler
        // would report the last index.
        tile.addEventListener('click', (function (at) {
          return function () { pick(at); };
        })(i));
        grid.appendChild(tile);
      }

      // The RANGE TEST FIRST, then the subscript: `cursor - w.first` is negative for a cursor above
      // the window, and a negative subscript is a question worth not asking.
      var active = (cursor >= w.first && cursor < w.last)
        ? grid.getElementsByTagName('button')[cursor - w.first] : null;
      if (active) scroll.setAttribute('aria-activedescendant', active.getAttribute('id'));
      else scroll.removeAttribute('aria-activedescendant');
    }

    // Keeps the cursor's row inside the viewport by arithmetic rather than by measurement — the
    // row's top and bottom are known exactly, which scrollIntoView on a node that may not be
    // rendered yet could not be.
    function ensureVisible() {
      var row = Math.floor(cursor / COLUMNS);
      var top = row * ROW_HEIGHT;
      var view = viewport();
      if (top < scroll.scrollTop) scroll.scrollTop = top;
      else if (top + ROW_HEIGHT > scroll.scrollTop + view) {
        scroll.scrollTop = top + ROW_HEIGHT - view;
      }
    }

    // ensureVisible BEFORE render here, unlike on open, and that is safe rather than lucky: both
    // of these run after a render, so the spacers already carry a height and the scroller is
    // already scrollable — there is nothing for a browser's scrollTop clamp to swallow. In
    // refilter's case the cursor is additionally reset to 0 (no caller passes keepCursor), so the
    // only offset it ever asks for is a SMALLER one, which no clamp can refuse. Rendering first
    // would work too; it would just build a window twice for no gain.
    function refilter(keepCursor) {
      shown = filterEntries(all, currentFilter());
      if (!keepCursor) cursor = 0;
      if (cursor > shown.length - 1) cursor = Math.max(0, shown.length - 1);
      describe();
      ensureVisible();
      render();
    }

    function moveTo(next) {
      if (!shown.length) return;
      cursor = Math.min(shown.length - 1, Math.max(0, next));
      ensureVisible();
      render();
    }

    function pick(at) {
      var entry = shown[at];
      if (!entry) return;
      // Closed BEFORE the callback, so a caller whose onPick re-renders the form (which is most of
      // them) is not writing into a DOM this dialog still holds a cursor into.
      //
      // THE FOCUS RESTORE RIDES INSIDE THAT CLOSE, so the opener is focused before onPick runs.
      // Correct for every shipped onPick — they only dispatch `input` on a field — but one that
      // REBUILT the form would detach the button just focused and drop focus to <body>. Such a
      // caller has to refocus something itself; this dialog no longer exists by then.
      instance.close(true);
      if (onPick) onPick(entry.pick);
    }

    // Whether a node is the scroller or inside it — the grid, a tile, a spacer. A tile takes
    // focus on mousedown in real browsers, so its keydowns must still count as the grid's.
    // Walked by parentNode because the test DOM has no Node.contains.
    function inGrid(node) {
      for (var n = node; n; n = n.parentNode) {
        if (n === scroll) return true;
      }
      return false;
    }

    function onKey(event) {
      var key = event.key;

      if (key === 'Escape') {
        event.preventDefault();
        instance.close(true);
        return;
      }

      // The search field answers two keys of its own and leaves the rest to the input handler, so
      // the arrows still move the caret through the id being typed.
      //
      // ENTER PICKS HERE TOO. Focus starts in this field, and the tile the grid is already showing
      // as selected is highlighted right next to it — "type 42, press Enter" is the shortest path
      // through this whole dialog, and an Enter that did nothing would make the listbox's own
      // "Enter picks" label a lie for the first keystroke after open.
      if (event.target === search) {
        if (key === 'ArrowDown') {
          event.preventDefault();
          scroll.focus();
          return;
        }
        if (key === 'Enter') {
          event.preventDefault();
          pick(cursor);
        }
        return;
      }

      // Everything below is GRID navigation, and it belongs to the grid alone. This listener
      // sits on the DIALOG, so it also hears the chooser <select>s and the Close button — and
      // answering for them stole their keys: Enter on Close PICKED the highlighted tile into the
      // record the user was backing out of (preventDefault suppressed the button's own Enter
      // activation), and the arrows moved the grid cursor instead of the chooser's option, which
      // made the <select>s impossible to operate from the keyboard at all. Only Escape above is
      // dialog-wide; every other key on a head control is that control's own business.
      if (event.target !== scroll && !inGrid(event.target)) return;

      if (key === 'Enter') {
        event.preventDefault();
        pick(cursor);
        return;
      }

      var rowsPerPage = Math.max(1, Math.floor(viewport() / ROW_HEIGHT));
      var steps = {
        ArrowRight: 1, ArrowLeft: -1, ArrowDown: COLUMNS, ArrowUp: -COLUMNS,
        PageDown: rowsPerPage * COLUMNS, PageUp: -rowsPerPage * COLUMNS,
      };

      if (Object.prototype.hasOwnProperty.call(steps, key)) {
        // Otherwise the arrow ALSO scrolls the page behind the dialog.
        event.preventDefault();
        moveTo(cursor + steps[key]);
        return;
      }

      if (key === 'Home') { event.preventDefault(); moveTo(0); return; }
      if (key === 'End') { event.preventDefault(); moveTo(shown.length - 1); }
    }

    // The backdrop, and only the backdrop: the dialog is a child of #modal, so a click inside the
    // dialog bubbles here too and must not close anything.
    //
    // REMOVED ON CLOSE, unlike every other listener here. The others are on nodes this dialog owns
    // and die with them; #modal OUTLIVES the dialog, so one left behind would hold this closure —
    // and its entries — alive, and would accumulate one per open for as long as the sidebar stayed
    // up. Same reasoning as ColorPicker's document-level outside-click listener.
    function backdrop(event) {
      if (event.target === host) instance.close(true);
    }

    var instance = {
      close: function (restoreFocus) {
        if (live !== instance) return;
        live = null;
        host.removeEventListener('click', backdrop);
        // Emptied, not merely hidden: a modal holding 4,827 stale nodes behind `hidden` costs the
        // same memory as one on screen.
        host.innerHTML = '';
        host.hidden = true;
        if (restoreFocus && opener && typeof opener.focus === 'function') opener.focus();
      },
    };
    live = instance;

    // One keydown listener, on the dialog, for every key the whole browser answers. It is on the
    // dialog rather than the document so it dies with the dialog — a document listener left behind
    // would hold this closure, and its tiles, alive for as long as the sidebar stayed up.
    dialog.addEventListener('keydown', onKey);
    scroll.addEventListener('scroll', render);
    search.addEventListener('input', function () { refilter(false); });
    if (sheetChooser) sheetChooser.addEventListener('change', function () { refilter(false); });
    if (categoryChooser) {
      categoryChooser.addEventListener('change', function () { refilter(false); });
    }
    closeButton.addEventListener('click', function () { instance.close(true); });
    host.addEventListener('click', backdrop);

    shown = filterEntries(all, currentFilter());
    // The caller's current graphic opens SELECTED and scrolled to, so the browser starts at the
    // sprite the record already names rather than at the top of a list of 4,827.
    cursor = indexOf(shown, bundleName, opts.current);
    describe();
    // RENDERED TWICE, AND THE FIRST ONE IS NOT WASTE. A browser clamps an assignment to scrollTop
    // to scrollHeight - clientHeight, and before the first render the two spacers have no height
    // at all — so the scroller is not yet scrollable and ensureVisible's offset would clamp to 0,
    // opening the grid at row 0 with the selected tile outside the window and no
    // aria-activedescendant at all. The first render gives the spacers their height; only then can
    // the scroller be scrolled; the second render builds the window that offset lands on.
    render();
    ensureVisible();
    render();
    // Focus into the search field, which is the one control that does something useful with the
    // first keystroke. Arrow keys reach the grid from here (see onKey).
    search.focus();

    return instance;
  }

  // Where `current` sits in the filtered list, or 0. Compared on the identifying parts rather than
  // on the bundle key, because that is the shape the caller has: it is whatever onPick last gave
  // them, read back out of their own cells.
  function indexOf(entries, bundleName, current) {
    if (!current) return 0;
    for (var i = 0; i < entries.length; i++) {
      var e = entries[i];
      if (bundleName === 'icons' && num(e.sheet) === num(current.sheet)
          && num(e.graphic) === num(current.graphic)) return i;
      if (bundleName === 'parts' && e.category === str(current.category)
          && num(e.id) === num(current.id)) return i;
      if (bundleName === 'effects' && num(e.id) === num(current.id)) return i;
    }
    return 0;
  }

  return {
    open: open,
    close: close,
    isOpen: function () { return !!live; },
    iconEntries: iconEntries,
    iconSheets: iconSheets,
    partEntries: partEntries,
    partCategories: partCategories,
    effectEntries: effectEntries,
    filterEntries: filterEntries,
    windowFor: windowFor,
    tileStyle: tileStyle,
    COLUMNS: COLUMNS,
    CELL: CELL,
    GAP: GAP,
    ROW_HEIGHT: ROW_HEIGHT,
    OVERSCAN: OVERSCAN,
  };
})();

if (typeof module !== 'undefined') module.exports = { Gallery: Gallery };
