// Typeahead over another sheet's id + name, and the graphic picker over GOOSE_SPRITES.icons.
//
// Both controls write ONLY the underlying cells, by name, so Forms.collect gathers them with
// every other field and neither needs a collect() of its own.
var Pickers = (function () {
  // How many rows the dropdown will show. The sheets behind it are big — Items is 649 rows and
  // NPC Spawns 4,322 — so this is a cap on a list a human is going to read, not on the search:
  // a query with more hits than this is a query that needs another character, and the list
  // stays scannable instead of becoming a second copy of the sheet.
  var LIMIT = 50;

  // Slots inside LIMIT that name hits keep even when id-prefix hits could fill the whole list.
  // Without it, one digit typed against Items produces >100 id-prefix hits (1, 10-19, 100-199,
  // …) and slicing after concatenation would hide EVERY name match behind them — so a designer
  // hunting "Sword" who typed "1" first would be told, wrongly, that there is nothing else.
  var NAME_RESERVE = 10;

  function str(value) {
    return (value === undefined || value === null) ? '' : String(value);
  }

  // Ids arrive from the Sheets API as whatever it decided the cell was — the number 42, '42',
  // or ' 042 ' from a hand-typed row — and Validation.validateCell compares them with
  // Number(value) against a Set of Numbers. Matching on the raw text instead would make the
  // picker say "not found in Items" for a value the validator is perfectly happy to save.
  // Anything that is not an integer literal is left exactly as it is: it is not an id, and
  // Validation will report it as such under the field.
  function key(value) {
    var text = str(value).trim();
    return /^-?\d+$/.test(text) ? String(Number(text)) : text;
  }

  function isWhole(text) {
    return /^\d+$/.test(text);
  }

  // Three buckets, best first: exact id, then id prefix, then name substring. An entry lands in
  // AT MOST ONE — matching both an id prefix and a name is one row in the list, not two.
  function search(entries, query) {
    // key() trims, so a query of pure whitespace arrives here as the empty query.
    var q = key(str(query).toLowerCase());
    if (q === '') return entries.slice(0, LIMIT);

    var exact = [], idPrefix = [], nameHit = [];

    for (var i = 0; i < entries.length; i++) {
      var e = entries[i];
      var id = key(e.id);
      var name = str(e.name).toLowerCase();

      if (id === q) exact.push(e);
      else if (id.indexOf(q) === 0) idPrefix.push(e);
      else if (name.indexOf(q) !== -1) nameHit.push(e);
    }

    // Budget rather than a plain slice: exact matches first, then id-prefix hits get whatever
    // is left MINUS the reserve, and name hits take the rest. When either bucket is short the
    // other simply spills into the space, so the list is only ever cut when there is genuinely
    // too much to show.
    var room = LIMIT - exact.length;
    var reserved = Math.min(nameHit.length, NAME_RESERVE, room);
    return exact.concat(idPrefix.slice(0, room - reserved), nameHit).slice(0, LIMIT);
  }

  // FK control: a text input holding the id, a live label showing the resolved name, and a
  // results list. Writes only the id back to the sheet.
  function fkControl(column, value, ctx) {
    var wrap = Forms.el('div', { class: 'picker' });
    var input = Forms.el('input', {
      name: column.name,
      id: 'f-' + column.name,
      type: 'text',
      autocomplete: 'off',
      placeholder: Forms.placeholderFor(column),
    });
    // str(), not `value || ''`: a stored 0 means "none" and is a REAL value. Blanking it here
    // would write blank on the next save, and blank means "use the SQL default" — which for
    // most of these columns is a different number entirely (see Forms.str).
    input.value = str(value);

    var label = Forms.el('span', { class: 'resolved' });
    var list = Forms.el('div', { class: 'results' });
    list.hidden = true;

    // Read through to ctx on every use rather than capturing the array once.
    // App.loadReferencedSheets fills pickerData over google.script.run, and a control built
    // before its sheet lands would otherwise be an empty picker forever.
    function entries() {
      return (ctx && ctx.pickerData && ctx.pickerData[column.ref]) || [];
    }

    function find(text) {
      var wanted = key(text);
      var all = entries();
      for (var i = 0; i < all.length; i++) {
        if (key(all[i].id) === wanted) return all[i];
      }
      return null;
    }

    function resolve() {
      var v = str(input.value).trim();
      // Blank and the literal '0' are "none" — the same two Validation.validateCell exempts
      // from its FK check, trim included. '00' is deliberately NOT among them: Validation
      // looks that one up and reports it, so the picker must not quietly call it none.
      if (v === '' || v === '0') {
        label.textContent = 'none';
        label.className = 'resolved';
        return;
      }

      var hit = find(v);
      if (hit) {
        label.textContent = str(hit.name) || '(unnamed)';
        label.className = 'resolved';
        return;
      }

      // An empty picker list means the sheet has not arrived yet, not that the id is wrong.
      // Saying "not found" then would accuse the user of a bad value on every freshly opened
      // record.
      var loaded = entries().length > 0;
      label.textContent = loaded ? 'not found in ' + column.ref : 'loading ' + column.ref + '…';
      label.className = loaded ? 'resolved bad' : 'resolved';
    }

    function refresh() {
      var results = search(entries(), input.value);
      // Drops the previous rows and, with them, their click handlers: the nodes are
      // unreachable afterwards, so nothing is left listening and nothing leaks.
      list.innerHTML = '';

      results.forEach(function (e) {
        var id = key(e.id);
        var row = Forms.el('button', { type: 'button', class: 'result', 'data-id': id },
                           id + ' — ' + str(e.name));
        // Writes the CANONICAL id, so picking ' 042 ' from a hand-edited sheet stores '42'.
        row.addEventListener('click', function () {
          input.value = id;
          list.hidden = true;
          resolve();
        });
        list.appendChild(row);
      });

      list.hidden = results.length === 0;
    }

    input.addEventListener('input', function () { refresh(); resolve(); });
    // Focusing an empty field shows the head of the list, so the control answers "what can go
    // in here?" without the user having to guess a first character.
    input.addEventListener('focus', refresh);

    // Cancelling mousedown is what makes this safe: moving focus is the DEFAULT ACTION of
    // mousedown, so preventing it keeps focus on the input and blur never fires — the click
    // then lands on a list that is still there. The alternative (hide on blur after a ~150ms
    // timer, hoping the click wins) is a race that loses on a slow frame, and losing it means
    // the user's click silently does nothing.
    list.addEventListener('mousedown', function (event) { event.preventDefault(); });
    input.addEventListener('blur', function () { list.hidden = true; });

    resolve();
    wrap.appendChild(input);
    wrap.appendChild(label);
    wrap.appendChild(list);
    return wrap;
  }

  // Graphic control: the graphic and sheet cells side by side, a canvas preview, and a status
  // line. Blank or 0 in both means "no graphic".
  //
  // ARGUMENT ORDER IS THE SCHEMA'S, NOT Sprites'. The Graphic composite declares its columns
  // [graphic, file] (graphic_tile + graphic_file, spell_animation + spell_animation_file, …)
  // while Sprites.icon takes (bundles, SHEET, graphic). Swapping the two resolves nothing at
  // all and does it silently, so the order is asserted in the tests.
  function graphicControl(graphicColumn, fileColumn, values, ctx) {
    var wrap = Forms.el('div', { class: 'graphic' });
    var canvas = Forms.el('canvas', { width: 48, height: 48, class: 'preview' });

    function cell(column, fallback) {
      var node = Forms.el('input', {
        name: column.name,
        id: 'f-' + column.name,
        type: 'text',
        autocomplete: 'off',
        placeholder: Forms.placeholderFor(column) || fallback,
      });
      node.value = str(values[column.name]);
      return node;
    }

    var gInput = cell(graphicColumn, 'graphic');
    var fInput = cell(fileColumn, 'sheet');
    var status = Forms.el('span', { class: 'status' });

    // Says WHY nothing is on the canvas. A blank preview is otherwise indistinguishable from a
    // typo, and nothing else catches one: the cells are optional, so Validation passes a graphic
    // that names art the bundle does not have, and Equipped.format/isFaithful never see this
    // column at all.
    function describe(rect) {
      var g = str(gInput.value).trim();
      var f = str(fInput.value).trim();
      var noGraphic = (g === '' || g === '0');
      var noSheet = (f === '' || f === '0');

      if (noGraphic && noSheet) return { text: 'no graphic', bad: false };
      // A blank cell is not a typo — it is half a pair, which is its own message. Only a cell
      // with something in it that is not a whole number is one.
      if ((g !== '' && !isWhole(g)) || (f !== '' && !isWhole(f))) {
        return { text: 'graphic and sheet must be whole numbers', bad: true };
      }
      if (noGraphic || noSheet) return { text: 'graphic and sheet must both be set', bad: true };
      if (!rect) return { text: 'no art for sheet ' + f + ' graphic ' + g, bad: true };
      return { text: '', bad: false };
    }

    function redraw() {
      // The raw cells go straight to Sprites, which does its own parseInt-based coercion. A
      // Number() here as well would be a SECOND rule for what a cell means — and the two
      // disagree ('1e3' is 1000 to one and 1 to the other), so the preview could resolve a
      // different sprite from the one the lookup key names.
      var rect = Sprites.icon((ctx && ctx.bundles) || {}, fInput.value, gInput.value);

      var state = describe(rect);
      status.textContent = state.text;
      status.className = state.bad ? 'status bad' : 'status';

      var target = canvas.getContext('2d');
      target.clearRect(0, 0, canvas.width, canvas.height);
      // Load-bearing, not defensive: Sprites.draw would ignore a null rect happily, but the
      // centring below reads rect[2] first and would throw before ever reaching it.
      if (!rect) return;

      // Sprites.draw is a no-op while the bundle PNG is still decoding, which is why redraw is
      // also registered below rather than only run once here.
      Sprites.draw(target, (ctx && ctx.images) ? ctx.images.icons : null, rect,
                   Math.floor((canvas.width - rect[2]) / 2),
                   Math.floor((canvas.height - rect[3]) / 2),
                   null);
    }

    gInput.addEventListener('input', redraw);
    fInput.addEventListener('input', redraw);

    redraw();
    if (ctx && typeof ctx.onImagesReady === 'function') ctx.onImagesReady(redraw);

    wrap.appendChild(canvas);
    wrap.appendChild(gInput);
    wrap.appendChild(fInput);
    wrap.appendChild(status);
    return wrap;
  }

  return { search: search, fkControl: fkControl, graphicControl: graphicControl, LIMIT: LIMIT };
})();

if (typeof module !== 'undefined') module.exports = { Pickers: Pickers };
