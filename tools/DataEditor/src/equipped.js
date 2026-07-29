// equipped_items is a comma-separated token stream, NOT fixed-width pairs. Each slot is
// either "<graphic>,*" (no tint) or "<graphic>,<r>,<g>,<b>,<a>" — see
// Scripts/Network/Packets/MakeCharacterPacket.cs:109-133. The DB string is spliced straight
// into the packet by Goose/Packets.cs:161, so this format is the wire format.
//
// parse() deliberately does not validate: it is lenient in the same direction the client is,
// so an odd row still renders in the editor instead of throwing. Rejecting bad values is
// validation.js's job.
var Equipped = (function () {
  // Character.cs:206-212. The mount is slot 6 and is not part of equipped_items.
  var SLOTS = ['Chest', 'Helm', 'Legs', 'Feet', 'Shield', 'Weapon'];

  function empty() {
    return { graphic: 0, r: 0, g: 0, b: 0, a: 0, tinted: false };
  }

  function num(token) {
    var n = parseInt(token, 10);
    return isNaN(n) ? 0 : n;
  }

  function parse(raw) {
    var text = (raw === null || raw === undefined) ? '' : String(raw);
    var tokens = text.trim().split(',').map(function (t) { return t.trim(); });
    var slots = [];
    var i = 0;

    // Every branch pushes exactly one slot, so this always terminates at SLOTS.length.
    while (slots.length < SLOTS.length) {
      if (i >= tokens.length || tokens[i] === '') {
        slots.push(empty());
        i += 1;
        continue;
      }

      var graphic = num(tokens[i]);
      i += 1;

      if (tokens[i] === '*') {
        slots.push({ graphic: graphic, r: 0, g: 0, b: 0, a: 0, tinted: false });
        i += 1;
      } else if (i >= tokens.length) {
        // The stream ended after the graphic id. There is no tint data to carry, so this is
        // an untinted slot — claiming tinted:true would tell appearance.js to blend a colour
        // nobody supplied.
        slots.push({ graphic: graphic, r: 0, g: 0, b: 0, a: 0, tinted: false });
      } else {
        var tint = {
          graphic: graphic,
          r: num(tokens[i]),
          g: num(tokens[i + 1]),
          b: num(tokens[i + 2]),
          a: num(tokens[i + 3]),
          tinted: true,
        };
        // A tint truncated mid-way is only partly real; treat it as untinted for the same
        // reason as above rather than inventing zeros for the missing channels.
        if (i + 3 >= tokens.length) tint.tinted = false;
        slots.push(tint);
        i += 4;
      }
    }

    return slots;
  }

  function format(slots) {
    var parts = [];
    for (var i = 0; i < SLOTS.length; i++) {
      var s = (slots && slots[i]) || empty();
      // a === 0 means no blend (Icon.cs:9-11 mixes by a), so emit the compact form — it
      // renders identically and matches how existing rows look.
      if (!s.tinted || !s.a) parts.push(s.graphic + ',*');
      else parts.push([s.graphic, s.r, s.g, s.b, s.a].join(','));
    }
    return parts.join(',');
  }

  return { SLOTS: SLOTS, parse: parse, format: format, empty: empty };
})();

if (typeof module !== 'undefined') module.exports = { Equipped: Equipped };
