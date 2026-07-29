// Canvas previews. Layer computation lives in Appearance and sprite lookup in Sprites, both
// tested on their own; this file is rendering and nothing else.
//
// THE ANCHOR. CharacterAnchor.OffsetY (Scripts/Character/CharacterAnchor.cs:13) is the offset of
// a CENTRE-pivot sprite from the character's tile-bottom origin. A canvas drawImage takes the
// sprite's TOP edge, so the conversion is `origin + offsetY(h) - h/2`. At h = 48 that puts the
// bottom edge exactly on ORIGIN_Y, which is the whole point of the anchor: the feet land on the
// tile. Taller frames overhang downwards, as the client's comment says they should.
//
// NO MOUNT BRANCH, deliberately. Sprites.mount exists and mounted-idle-down clips are in the
// bundle, but Appearance.layers can never emit a Mount layer: MakeCharacterPacket.ParseEquippedItems
// fills equipped slots 0-5 only, Goose/Packets.cs:161 sends no equipment at all for a body id of
// 100 or more, and the NPCs sheet has no mount column. A branch here for it would be code no data
// can reach and no test can honestly cover, so it is a note instead. If a mount column ever
// arrives, Appearance.layers grows the layer and this file grows the branch — in that order.
var Preview = (function () {
  var CANVAS_W = 96;
  var CANVAS_H = 112;
  var ORIGIN_Y = 88;   // where the feet land
  var FRAME_MS = 125;  // speed 8.0 in the .tres clips
  // The effect canvas is square and smaller than the character canvas; that CANVAS_H is 112 and
  // this is 96 is not a relationship, and a caller reaching for CANVAS_W/H here would be relying
  // on a coincidence.
  var EFFECT_SIZE = 96;

  // How much bigger than logical each preview's canvas is drawn, so app.js and this file agree
  // on the number. 96x112 at 4x is 384x448; 96x96 at 2x is 192x192.
  var CHARACTER_SCALE = 4;
  var EFFECT_SCALE = 2;

  // parseInt(v, 10), matching Equipped.num(), Appearance.num(), Sprites.num() and
  // Composites.num() so no two modules disagree about what a spreadsheet cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  // Resting-pose armed/unarmed, the flag Sprites.part selects a clip with. Appearance.layers
  // does NOT carry it — it describes what to draw, not which pose — so the caller has to compute
  // it, and a forgotten argument is `undefined`, which is falsy and silently picks the unarmed
  // chain. On the shipped NPCs that is 462 layers across 186 rows resolving to a different rect,
  // so it is computed here, once, rather than left to each call site.
  //
  // A blank cell reads as 0, which is not 3 and so counts as armed. That matches the NPCs
  // column default of 1 (also armed); Items defaults to 3, but Items has no character preview.
  function isArmed(bodyState) {
    return num(bodyState) !== 3;
  }

  /// Composite character preview. Draws Appearance.layers in order, anchoring each sprite the
  /// way the client does. Returns { layers, drawn }: layers is what the data asks for, drawn is
  /// what the bundle actually had art for, and a gap between the two is a missing sprite rather
  /// than a bug in the maths.
  /// `scale` scales the CONTEXT, not the maths: the caller sizes the canvas CANVAS_W * scale by
  /// CANVAS_H * scale and everything below stays in logical pixels. Smoothing stays off — a
  /// scaled context resamples by default, and a blurry 4x sprite is worse than a small sharp one.
  function character(canvas, appearance, ctx, scale) {
    var s = scale || 1;
    var c = canvas.getContext('2d');
    c.setTransform(s, 0, 0, s, 0, 0);
    c.clearRect(0, 0, CANVAS_W, CANVAS_H);
    c.imageSmoothingEnabled = false;

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

  /// Effect animation: loops the effect's frames. Returns a stop function, which the caller MUST
  /// keep and call before starting another one or navigating away — an abandoned interval keeps
  /// drawing onto a canvas that is no longer in the tree.
  function effect(canvas, effectId, ctx, scale) {
    var s = scale || 1;
    var frames = Sprites.effectFrames((ctx && ctx.bundles) || {}, effectId);
    var image = (ctx && ctx.images) ? ctx.images.effects : null;
    var c = canvas.getContext('2d');
    // Same bargain as character(): the context carries the scale, the maths does not.
    c.setTransform(s, 0, 0, s, 0, 0);
    c.imageSmoothingEnabled = false;

    if (!frames.length) {
      c.clearRect(0, 0, EFFECT_SIZE, EFFECT_SIZE);
      return function () {};
    }

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
    isArmed: isArmed,
    CANVAS_W: CANVAS_W,
    CANVAS_H: CANVAS_H,
    EFFECT_SIZE: EFFECT_SIZE,
    CHARACTER_SCALE: CHARACTER_SCALE,
    EFFECT_SCALE: EFFECT_SCALE,
    ORIGIN_Y: ORIGIN_Y,
    FRAME_MS: FRAME_MS,
  };
})();

if (typeof module !== 'undefined') module.exports = { Preview: Preview };
