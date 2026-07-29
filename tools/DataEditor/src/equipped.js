// equipped_items is a comma-separated token stream, NOT fixed-width pairs. Each slot is
// either "<graphic>,*" (no tint) or "<graphic>,<r>,<g>,<b>,<a>" — see
// Scripts/Network/Packets/MakeCharacterPacket.cs:109-133. The DB string is spliced straight
// into the packet by Goose/Packets.cs:161, so this format is the wire format.
//
// The client is NOT lenient about that format. PacketParser.GetNextToken() throws
// InvalidOperationException past the end of the packet and Convert.ToInt32 throws
// FormatException on "", "1.5" and "14*" — taking the whole MakeCharacter packet down with
// it, not just the offending slot. An editor cannot behave that way: it has to open every row
// in the shipped data, including the handful that are already malformed. So parse() degrades
// gracefully exactly where the client would reject, and none of its leniency — the zero-fill,
// num()'s NaN handling, the truncated-tint demotion — implies parity with the client.
//
// That leniency silently rewrites malformed input into different, valid-looking data, so
// isFaithful() reports whether a string survives parse+format intact; the write-back path
// uses it to refuse to save rows it would corrupt. Checking values against column rules is
// validation.js's job, but format() is the last thing between the UI and the wire, so it
// coerces its input rather than trusting it.
//
// HAND-OFF TO TASK 9: that coercion is silent and reports nothing. A user typing "abc" into
// a graphic field gets 0 with no feedback, and the row then saves clean because format()
// already sanitised it — isFaithful() only inspects STORED strings, so it never sees the
// typo either. equipSlotsControl must validate that field itself; nothing here will.
var Equipped = (function () {
  // Character.cs:206-212. The mount is slot 6 and is not part of equipped_items.
  var SLOTS = ['Chest', 'Helm', 'Legs', 'Feet', 'Shield', 'Weapon'];

  var INTEGER = /^\d+$/;

  function empty() {
    return { graphic: 0, r: 0, g: 0, b: 0, a: 0, tinted: false };
  }

  function num(token) {
    var n = parseInt(token, 10);
    return isNaN(n) ? 0 : n;
  }

  // A graphic id indexes a sprite table, so a negative or fractional one is meaningless —
  // and Convert.ToInt32 would throw on the fractional case. Coercion goes through num() so
  // format() and parse() cannot drift apart on inputs like '1e3' or '0x10'; num() also
  // absorbs Infinity and NaN, which would otherwise reach the wire as literal text.
  function graphicOf(value) {
    var n = num(value);
    return n > 0 ? n : 0;
  }

  // Icon.cs:23 divides each channel by 255 without clamping, so an out-of-range byte skews
  // the blend rather than erroring. Clamp here: the UI binds these to free-text inputs.
  // The clamp is deliberately silent because it is not the only line of defence — a stored
  // value that needed clamping is one isFaithful() reports, so the write-back gate raises it
  // rather than saving a quietly altered colour. Clamp at the boundary, flag at the gate.
  function channelOf(value) {
    var n = num(value);
    return Math.min(255, Math.max(0, n));
  }

  // The shared engine behind parse() and isFaithful(). `faithful` goes false whenever the
  // token stream was not consumed as exactly six well-formed slots — a non-numeric token, a
  // truncated tint, or tokens left over — which is precisely when format(parse(raw)) stops
  // meaning what raw meant.
  function scan(raw) {
    var text = (raw === null || raw === undefined) ? '' : String(raw);
    var tokens = text.split(',').map(function (t) { return t.trim(); });
    var slots = [];
    var faithful = true;
    var i = 0;

    // Blank is not a valid stored value at all: Goose/Packets.cs:161 splices
    // EquippedItems + ",", so a blank cell on a CurrentBodyID < 100 NPC emits ",," and
    // desynchronises the whole packet. The column's schema default is literally
    // '0,*,0,*,0,*,0,*,0,*,0,*' (GooseData.sql:1397). Canonicalising blank REPAIRS a broken
    // value, so it stays faithful — flagging it would block the very save that fixes it.
    if (text.trim() === '') {
      while (slots.length < SLOTS.length) slots.push(empty());
      return { slots: slots, faithful: true };
    }

    // Every branch pushes exactly one slot, so this always terminates at SLOTS.length.
    while (slots.length < SLOTS.length) {
      if (i >= tokens.length || tokens[i] === '') {
        slots.push(empty());
        faithful = false;
        i += 1;
        continue;
      }

      if (!INTEGER.test(tokens[i])) faithful = false;
      var graphic = num(tokens[i]);
      i += 1;

      if (tokens[i] === '*') {
        slots.push({ graphic: graphic, r: 0, g: 0, b: 0, a: 0, tinted: false });
        i += 1;
      } else {
        var tint = {
          graphic: graphic,
          r: num(tokens[i]),
          g: num(tokens[i + 1]),
          b: num(tokens[i + 2]),
          a: num(tokens[i + 3]),
          tinted: true,
        };
        // A tint running off the end of the stream is not real tint data — num() invented
        // the missing channels. Reporting tinted:true would tell appearance.js to blend a
        // colour nobody supplied, so demote the slot and flag the string as not understood.
        if (i + 3 >= tokens.length) {
          tint.tinted = false;
          // Belt and braces: a truncated tint always also trips the zero-fill or leftover
          // check below, so this assignment is redundant today. Kept so the branch stays
          // correct on its own terms, not because anything depends on it.
          faithful = false;
        } else {
          for (var c = 0; c < 4; c++) {
            if (!INTEGER.test(tokens[i + c]) || num(tokens[i + c]) > 255) faithful = false;
          }
          // format() collapses a zero-alpha tint to the compact "id,*" form, which is
          // render-identical (Icon.cs blends by a) but DISCARDS r/g/b. All-zero channels
          // lose nothing; a parked colour behind a zero alpha does, so flag it — otherwise
          // the write-back gate would certify a save that silently drops the colour.
          if (num(tokens[i + 3]) === 0 &&
              (num(tokens[i]) !== 0 || num(tokens[i + 1]) !== 0 || num(tokens[i + 2]) !== 0)) {
            faithful = false;
          }
        }
        slots.push(tint);
        i += 4;
      }
    }

    // Anything past the sixth slot is data we silently dropped.
    if (i !== tokens.length) faithful = false;

    return { slots: slots, faithful: faithful };
  }

  function parse(raw) {
    return scan(raw).slots;
  }

  // True when parse+format preserves raw's meaning. False means parse() had to guess, and
  // writing the reformatted value back would rewrite the row into different equipment.
  function isFaithful(raw) {
    return scan(raw).faithful;
  }

  function format(slots) {
    var parts = [];
    for (var i = 0; i < SLOTS.length; i++) {
      var s = (slots && slots[i]) || empty();
      var graphic = graphicOf(s.graphic);
      var a = channelOf(s.a);
      // a === 0 means no blend (Icon.cs:23 mixes by a), so emit the compact form — it
      // renders identically and matches how existing rows look.
      if (!s.tinted || !a) parts.push(graphic + ',*');
      else parts.push([graphic, channelOf(s.r), channelOf(s.g), channelOf(s.b), a].join(','));
    }
    return parts.join(',');
  }

  return {
    SLOTS: SLOTS,
    parse: parse,
    format: format,
    empty: empty,
    isFaithful: isFaithful,
  };
})();

if (typeof module !== 'undefined') module.exports = { Equipped: Equipped };
