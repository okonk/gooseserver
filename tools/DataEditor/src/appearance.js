// Which sprites a character is built from, in draw order — a port of
// Character.ApplyAppearance (Scripts/Character/Character.cs:202-245) for a static, south-facing
// preview. Pure computation: no canvas, no image loading. Task 11 turns these layers into pixels.
//
// The draw order is CharacterLayout.SortOrder ((int)slot + 2, CharacterLayout.cs:20-37), NOT the
// order ApplySlot is called in at Character.cs:231-240. The call order puts Hair before Chest,
// which would draw hair UNDER the chest piece — the client sorts afterwards (ApplyDrawOrder,
// Character.cs:347-354) and so does this.
//
// The preview faces Down, so Shield and Weapon keep their base order; only Right/Up/Left move
// them (CharacterLayout.cs:25-36). If the preview ever gains a facing control, sortOrder() is the
// one function that has to grow a direction argument.
//
// NOT ported: ApplySlot's second removal branch (Character.cs:266) drops any slot whose
// animations.tres does not exist, so the client silently skips an id with no art. A layer here is
// "what the data asks for", not "what can be drawn" — resolving an id to a sprite belongs to the
// bundle lookup (Task 5) and the preview (Task 11), which decide what to do about a miss.
var Appearance = (function () {
  // CharacterLayout.cs:6 — the enum's numeric order IS the base sort order.
  var SLOT_INDEX = {
    Mount: 0, Body: 1, Eyes: 2, Feet: 3, Legs: 4, Chest: 5, Hair: 6, Helm: 7,
    Shield: 8, Weapon: 9,
  };

  // CharacterLayout.cs:39-52 — sprite folder per slot. Mounts are just bodies, and shields and
  // weapons both render from Hands.
  var CATEGORY = {
    Body: 'Bodies', Mount: 'Bodies', Hair: 'Hair', Eyes: 'Eyes', Chest: 'Chest',
    Helm: 'Helms', Legs: 'Legs', Feet: 'Feet', Shield: 'Hands', Weapon: 'Hands',
  };

  // Which character slot an ITEM's equip slot renders into — Goose/Inventory.cs:602-655 for the
  // mapping and :692 for which of them reach EquippedDisplay at all. Combined with CATEGORY above
  // this is the whole route from an item_slot cell to a sprite folder, and it is the client's own
  // route: Helmet -> Helm -> Helms, Shield and both weapon slots -> Hands, Mount -> Bodies.
  //
  // KEYED ON THE ENUM MEMBER NAME, not the number. The spreadsheet cell holds "Helmet"
  // (DescriptorTransform.cs:24), so an index-keyed table would miss every row.
  //
  // THE ABSENT SLOTS ARE THE POINT. Ring, Necklace, Pauldrons, Cloak, Belt, Gloves and Misc are
  // never drawn on the character — the client has no layer for them — so they are left OUT rather
  // than mapped to a plausible-looking folder, and slotFor answers null. An empty or unrecognised
  // cell gets the same answer for the same reason: "not drawn" is honest, and guessing a category
  // would put a shield sprite on a ring.
  var EQUIP_SLOT = {
    Helmet: 'Helm', Chest: 'Chest', Pants: 'Legs', Shoes: 'Feet',
    Shield: 'Shield', OneHanded: 'Weapon', TwoHanded: 'Weapon', Mount: 'Mount',
  };

  /// The character slot an item_slot value renders into, or null when the slot is not drawn.
  /// Own-property lookup: 'constructor' is not an item_slot, and reaching Object.prototype for one
  /// would hand the caller a function where a slot name belongs.
  function slotFor(itemSlot) {
    var name = (itemSlot === undefined || itemSlot === null) ? '' : String(itemSlot).trim();
    return Object.prototype.hasOwnProperty.call(EQUIP_SLOT, name) ? EQUIP_SLOT[name] : null;
  }

  // Every field here can arrive as a spreadsheet cell, i.e. a STRING. `'11' === 11` is false, so
  // without this the underwear and monster-body rules would silently stop firing for real rows.
  // parseInt, not Number or parseFloat: these are C# ints on the wire, and a graphic id indexes a
  // sprite table, so '1.9' must become 1 and not 1.9. Matches Equipped.num() so the two modules
  // cannot disagree about what a cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  // Icon.cs:23 divides each channel by 255 without clamping — an out-of-range byte skews the
  // blend rather than erroring. A canvas needs a valid byte, so clamp here. num() first: a
  // spreadsheet cell is a string, and Math.max(0, '') is 0 but Math.max(0, 'x') is NaN.
  // An out-of-range stored channel is a data defect; reporting it is validation.js's job.
  function channel(value) {
    return Math.min(255, Math.max(0, num(value)));
  }

  // ONE layer object, or null for a slot that draws nothing. Every rule about what a layer IS
  // lives here: the <=0 drop, the channel clamping, the zero-alpha collapse and the sort order.
  //
  // Exported because layers() is not the only thing that builds one. Preview.wornItem has to add a
  // Mount layer by hand — equipped_items has six slots and Mount is not one of them, so it cannot
  // go through layers() at all (see the closing note) — and a hand-built object literal there
  // would be a SECOND way to make a layer, free to drift from the clamping and collapse above.
  function layer(slot, id, r, g, b, alpha) {
    var graphic = num(id);
    if (graphic <= 0) return null;
    // Character.cs:251-258 — a tint with alpha 0 is NoTint, colour and all. The alpha is a
    // blend factor, not opacity, so a "parked" colour behind a zero alpha never renders.
    var a2 = channel(alpha);
    return {
      slot: slot,
      category: CATEGORY[slot],
      id: graphic,
      r: a2 > 0 ? channel(r) : 0,
      g: a2 > 0 ? channel(g) : 0,
      b: a2 > 0 ? channel(b) : 0,
      a: a2,
      order: sortOrder(slot),
    };
  }

  // Down-facing draw order, back to front. Higher is nearer the viewer. Deliberately unguarded:
  // an unknown slot name gives NaN, which is a caller bug (the slot names are a closed set) and
  // shows up immediately rather than silently sorting to a plausible-looking position.
  function sortOrder(slot) {
    return SLOT_INDEX[slot] + 2;
  }

  // CharacterAnchor.cs:13, taking a frame height in pixels. C# integer division truncates toward
  // zero, hence Math.trunc — that is what the source says. Math.floor would in fact behave
  // identically for every height that exists: trunc and floor differ only on negative
  // non-integers, the first term is negative exactly when Math.max(..., 0) clamps it away, and
  // the second is negative only for a negative height. No test can distinguish them, and none
  // pretends to.
  function offsetY(height) {
    return Math.max(Math.trunc((height - 48) / 2), 0) - Math.trunc(height / 2);
  }

  function layers(a) {
    var eq = Equipped.parse(a.equippedItems);

    var slots = {
      Chest: eq[0], Helm: eq[1], Legs: eq[2],
      Feet: eq[3], Shield: eq[4], Weapon: eq[5],
    };

    // bodyId is coerced here because the rules below COMPARE it (>= 100, === 1, === 11) and
    // those comparisons are what a raw string breaks. hairId and faceId are only ever passed
    // on, so push() — the single coercion boundary for every id — handles them.
    var bodyId = num(a.bodyId);
    var hairId = a.hairId;
    var faceId = a.faceId;

    // Character.cs:218-223 — a monster or morph body (>= 100) renders the body alone. The server
    // does not even send equipment for those rows (Goose/Packets.cs:161). The body's OWN tint
    // survives: only the ids are zeroed.
    if (bodyId >= 100) {
      hairId = 0;
      faceId = 0;
      // Fresh object per slot: sharing one would make the underwear rewrite below ambiguous and
      // is a trap for anything that later mutates a slot in place.
      for (var slot in slots) slots[slot] = Equipped.empty();
    }

    // CharacterLayout.cs:56-69 — the two player bodies get underwear in slots that are empty.
    // Every other body, monsters included, gets none. Underwear is never tinted
    // (Character.cs:227-229 forces NoTint).
    //
    // `=== 0`, not `<= 0`: the client's guard is `if (equippedLegsId != 0) return 0`
    // (CharacterLayout.cs:58, :66), so a NEGATIVE stored id suppresses underwear too and then
    // draws nothing itself (push() drops it). Widening this to `<= 0` would put underwear on a
    // character the client leaves bare-legged.
    if (slots.Legs.graphic === 0) {
      if (bodyId === 1) slots.Legs = underwear(3);
      else if (bodyId === 11) slots.Legs = underwear(4);
    }
    if (slots.Chest.graphic === 0 && bodyId === 11) slots.Chest = underwear(8);

    var out = [];

    // Character.cs:264 removes any slot whose graphic is <= 0, so a 0 (empty) or a negative id
    // (Equipped.parse does not clamp, and neither does a spreadsheet cell) draws nothing.
    function push(slot, id, r, g, b, alpha) {
      var built = layer(slot, id, r, g, b, alpha);
      if (built) out.push(built);
    }

    function pushEquip(slot) {
      var s = slots[slot];
      push(slot, s.graphic, s.r, s.g, s.b, s.a);
    }

    push('Body', bodyId, a.bodyR, a.bodyG, a.bodyB, a.bodyA);
    push('Hair', hairId, a.hairR, a.hairG, a.hairB, a.hairA);
    push('Eyes', faceId, 0, 0, 0, 0);   // Character.cs:233 — eyes are always NoTint
    pushEquip('Chest');
    pushEquip('Helm');
    pushEquip('Legs');
    pushEquip('Feet');
    pushEquip('Shield');
    pushEquip('Weapon');

    // Orders are distinct per slot, so the sort is total and stability never matters.
    out.sort(function (x, y) { return x.order - y.order; });
    return out;
  }

  function underwear(graphic) {
    return { graphic: graphic, r: 0, g: 0, b: 0, a: 0, tinted: false };
  }

  // NOTE: there is no Mount layer. The client reserves equipped slot 6 for one, but
  // MakeCharacterPacket.ParseEquippedItems only ever fills slots 0-5, and the NPCs sheet has no
  // mount column — so nothing in this editor's data can produce one. Mount stays in SLOT_INDEX
  // and CATEGORY because the draw order and folder for it are still facts about the client.
  return {
    layers: layers, layer: layer, offsetY: offsetY, sortOrder: sortOrder, slotFor: slotFor,
    CATEGORY: CATEGORY, EQUIP_SLOT: EQUIP_SLOT,
  };
})();

if (typeof module !== 'undefined') module.exports = { Appearance: Appearance };
