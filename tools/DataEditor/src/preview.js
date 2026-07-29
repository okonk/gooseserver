// Canvas previews. Layer computation lives in Appearance and sprite lookup in Sprites, both
// tested on their own; this file is rendering and nothing else.
//
// THE ANCHOR. CharacterAnchor.OffsetY (Scripts/Character/CharacterAnchor.cs:13) is the offset of
// a CENTRE-pivot sprite from the character's tile-bottom origin. A canvas drawImage takes the
// sprite's TOP edge, so the conversion is `origin + offsetY(h) - h/2`. At h = 48 that puts the
// bottom edge exactly on ORIGIN_Y, which is the whole point of the anchor: the feet land on the
// tile. Taller frames overhang downwards, as the client's comment says they should.
//
// THE MOUNT BRANCH IS IN wornItem AND NOWHERE ELSE. Appearance.layers still cannot emit a Mount
// layer — MakeCharacterPacket.ParseEquippedItems fills equipped slots 0-5 only, Goose/Packets.cs:161
// sends no equipment at all for a body id of 100 or more, and the NPCs sheet has no mount column —
// so character(), which draws exactly what layers() asks for, has none.
//
// What reaches a mount is the ITEMS side: an item whose item_slot is Mount is a real, editable row,
// and Inventory.cs:602-655 renders it from the Bodies folder's mounted clip. wornItem therefore
// builds that layer itself rather than waiting for layers() to grow one, which is also what finally
// gave Sprites.mount a caller.
var Preview = (function () {
  var CANVAS_W = 96;
  var CANVAS_H = 112;
  var ORIGIN_Y = 88;   // where the feet land
  var FRAME_MS = 125;  // speed 8.0 in the .tres clips
  // The effect canvas is square and smaller than the character canvas; that CANVAS_H is 112 and
  // this is 96 is not a relationship, and a caller reaching for CANVAS_W/H here would be relying
  // on a coincidence.
  var EFFECT_SIZE = 96;

  // The item icon's logical box. 64 for the reason Pickers.ICON_BOX gives — the median icon is
  // 32x32 but the bundle holds sprites up to 128x128, and a 48 box clipped them.
  var ICON_BOX = 64;

  // The body the worn preview draws the item on. 1 is the shipped player body, and the one
  // CharacterLayout.cs:56-69 gives underwear to, so a chest or leg piece is judged against the same
  // silhouette the game shows. Not configurable: an item is worn by a player.
  var BASE_BODY = 1;

  // How much bigger than logical each preview's canvas is drawn, so app.js and this file agree
  // on the number. 96x112 at 4x is 384x448; 96x96 at 2x is 192x192.
  var CHARACTER_SCALE = 4;
  var EFFECT_SCALE = 2;
  // 64x64 at 4x is 256x256. Bigger than graphicControl's 2x copy of the same sprite on purpose:
  // this one is in the preview panel, where the question is what the art looks like.
  var ICON_SCALE = 4;

  // parseInt(v, 10), matching Equipped.num(), Appearance.num(), Sprites.num() and
  // Composites.num() so no two modules disagree about what a spreadsheet cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  // A tint out of four spreadsheet cells. num() is load-bearing rather than tidy: Sprites.draw's
  // no-tint shortcut is `!tint.a`, and the STRING '0' is truthy — so an untinted item handed
  // through raw would take the per-pixel offscreen path to compute the identity blend. Same
  // coercion Appearance.channel does before Appearance.layers emits a tint, so the two agree.
  //
  // Clamping is left to Sprites.applyTint, which does it for every caller and does it once.
  function tintOf(item) {
    return { r: num(item.r), g: num(item.g), b: num(item.b), a: num(item.a) };
  }

  // Resting-pose armed/unarmed, the flag Sprites.part selects a clip with. Appearance.layers
  // does NOT carry it — it describes what to draw, not which pose — so the caller has to compute
  // it, and a forgotten argument is `undefined`, which is falsy and silently picks the unarmed
  // chain. On the shipped NPCs that is 462 layers across 186 rows resolving to a different rect,
  // so it is computed here, once, rather than left to each call site.
  //
  // A blank cell reads as 0, which is not 3 and so counts as armed. That matches the NPCs column
  // default of 1 (also armed). Items DEFAULTS TO 3, and now has previews that read this — so a
  // blank body_state on an Items row previews armed where the database would say unarmed. The same
  // divergence composites.js flags for equipSlotsControl, from the same cause (a control is given
  // the values map, not the column descriptors) and left the same way rather than half-fixed here.
  function isArmed(bodyState) {
    return num(bodyState) !== 3;
  }

  /// Composite character preview. Draws Appearance.layers in order, anchoring each sprite the
  /// way the client does. Returns { layers, drawn }: layers is what the data asks for, drawn is
  /// what the bundle actually had art for, and a gap between the two is a missing sprite rather
  /// than a bug in the maths.
  /// `scale` scales the CONTEXT, not the maths: Sprites.scaled sizes the canvas CANVAS_W * scale
  /// by CANVAS_H * scale and everything below stays in logical pixels.
  function character(canvas, appearance, ctx, scale) {
    var c = Sprites.scaled(canvas, scale, CANVAS_W, CANVAS_H);

    var layers = Appearance.layers(appearance);
    var equipped = isArmed(appearance.bodyState);
    var bundles = (ctx && ctx.bundles) || {};
    var image = (ctx && ctx.images) ? ctx.images.parts : null;
    var drawn = 0;

    layers.forEach(function (layer) {
      var rect = Sprites.part(bundles, layer.category, layer.id, equipped);
      // Missing art hides the slot, as the client does (Character.cs:266). Load-bearing rather
      // than defensive: the centring below reads rect[2] before Sprites.draw ever sees it.
      if (!rect) return;

      var dx = Math.floor((CANVAS_W - rect[2]) / 2);
      // Math.floor throughout this file's own centring maths. Appearance.offsetY uses
      // Math.trunc because it is porting C# integer division; here every height is positive, so
      // the two agree, and one rule is easier to check than two.
      var dy = ORIGIN_Y + Appearance.offsetY(rect[3]) - Math.floor(rect[3] / 2);

      Sprites.draw(c, image, rect, dx, dy, layer);
      drawn += 1;
    });

    return { layers: layers.length, drawn: drawn };
  }

  /// An item's INVENTORY ICON: one icon-bundle sprite, centred, tinted. The same sprite
  /// Pickers.graphicControl shows beside the cells, in the preview panel and bigger — the panel is
  /// where a designer looks to judge the art, and the control is where they look to fix the number.
  function itemIcon(canvas, item, ctx, scale) {
    var c = Sprites.scaled(canvas, scale, ICON_BOX, ICON_BOX);
    var rect = Sprites.icon((ctx && ctx.bundles) || {}, item.file, item.graphic);
    // Sprites.draw ignores a null rect, but the centring below reads rect[2] first.
    if (!rect) return { drawn: 0 };

    Sprites.draw(c, (ctx && ctx.images) ? ctx.images.icons : null, rect,
                 Math.floor((ICON_BOX - rect[2]) / 2),
                 Math.floor((ICON_BOX - rect[3]) / 2),
                 tintOf(item));
    return { drawn: 1 };
  }

  /// An item's WORN sprite, on a body. The equip graphic alone is a shape floating in space — a
  /// helmet is unreadable without a head under it — so this draws BASE_BODY first and puts the item
  /// in its client draw order relative to it.
  ///
  /// The base body and the item go through Appearance.layers TOGETHER, via an equipped_items string,
  /// rather than the item being appended to a body-only layer list. That is not a detour: the client
  /// only draws underwear into a slot that is EMPTY (CharacterLayout.cs:56-69), so a Pants item
  /// appended afterwards would sit at the same draw order as the underwear it is supposed to
  /// replace and the two would fight. Building the string lets the one implementation of that rule
  /// decide, as it does for every NPC.
  ///
  /// A MOUNT CANNOT GO THROUGH IT: equipped_items has six slots and Mount is not one of them
  /// (appearance.js's closing note says why the client's slot 6 is never filled). It is added as a
  /// layer of its own, at its own sort order, which puts it behind the body exactly as
  /// CharacterLayout does.
  function wornItem(canvas, item, ctx, scale) {
    var c = Sprites.scaled(canvas, scale, CANVAS_W, CANVAS_H);

    var slot = Appearance.slotFor(item.slot);
    var id = num(item.id);
    var worn = slot && id > 0;

    var slots = Equipped.SLOTS.map(function (name) {
      if (!worn || name !== slot) return Equipped.empty();
      // tinted: true hands the colour to Equipped.format, which collapses it to the compact
      // `id,*` form anyway when the blend is 0 — so a zero-blend item round-trips as untinted.
      return Object.assign({ graphic: id, tinted: true }, tintOf(item));
    });

    var layers = Appearance.layers({
      bodyId: BASE_BODY,
      equippedItems: Equipped.format(slots),
    });

    // A mount is not an equipped slot, so it is appended and the list re-sorted. Every order is
    // distinct per slot, so the sort stays total and its stability never matters.
    if (worn && slot === 'Mount') {
      layers = layers.concat([Object.assign({
        slot: slot, category: Appearance.CATEGORY[slot], id: id,
        order: Appearance.sortOrder(slot),
      }, tintOf(item))]);
      layers.sort(function (x, y) { return x.order - y.order; });
    }

    var equipped = isArmed(item.bodyState);
    var bundles = (ctx && ctx.bundles) || {};
    var image = (ctx && ctx.images) ? ctx.images.parts : null;
    var drawn = 0;

    layers.forEach(function (layer) {
      // Sprites.part deliberately never falls back to a mounted clip, so the mount takes the other
      // lookup rather than a flag on the same one.
      var rect = layer.slot === 'Mount' ? Sprites.mount(bundles, layer.id)
        : Sprites.part(bundles, layer.category, layer.id, equipped);
      if (!rect) return;

      Sprites.draw(c, image, rect,
                   Math.floor((CANVAS_W - rect[2]) / 2),
                   ORIGIN_Y + Appearance.offsetY(rect[3]) - Math.floor(rect[3] / 2),
                   layer);
      drawn += 1;
    });

    return { layers: layers.length, drawn: drawn, slot: slot };
  }

  /// Effect animation: loops the effect's frames. Returns a stop function, which the caller MUST
  /// keep and call before starting another one or navigating away — an abandoned interval keeps
  /// drawing onto a canvas that is no longer in the tree.
  function effect(canvas, effectId, ctx, scale) {
    var frames = Sprites.effectFrames((ctx && ctx.bundles) || {}, effectId);
    var image = (ctx && ctx.images) ? ctx.images.effects : null;
    // Same bargain as character(): the context carries the scale, the maths does not. The clear
    // Sprites.scaled does is also the whole of the no-frames case below.
    var c = Sprites.scaled(canvas, scale, EFFECT_SIZE, EFFECT_SIZE);

    if (!frames.length) return function () {};

    var i = 0;
    // Drawn once up front as well as on the interval, so the first frame is on screen
    // immediately rather than 125ms later.
    function step() {
      var rect = frames[i % frames.length];
      i += 1;

      c.clearRect(0, 0, EFFECT_SIZE, EFFECT_SIZE);
      Sprites.draw(c, image, rect,
                   Math.floor((EFFECT_SIZE - rect[2]) / 2),
                   Math.floor((EFFECT_SIZE - rect[3]) / 2), null);
    }

    step();
    var timer = setInterval(step, FRAME_MS);

    return function () { clearInterval(timer); };
  }

  return {
    character: character,
    effect: effect,
    itemIcon: itemIcon,
    wornItem: wornItem,
    isArmed: isArmed,
    CANVAS_W: CANVAS_W,
    CANVAS_H: CANVAS_H,
    EFFECT_SIZE: EFFECT_SIZE,
    ICON_BOX: ICON_BOX,
    BASE_BODY: BASE_BODY,
    CHARACTER_SCALE: CHARACTER_SCALE,
    EFFECT_SCALE: EFFECT_SCALE,
    ICON_SCALE: ICON_SCALE,
    ORIGIN_Y: ORIGIN_Y,
    FRAME_MS: FRAME_MS,
  };
})();

if (typeof module !== 'undefined') module.exports = { Preview: Preview };
