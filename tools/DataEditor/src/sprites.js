// Sprite lookup and tinting against the GOOSE_SPRITES bundles from tools/SpriteBundle. The three
// key schemes are minted in tools/SpriteBundle/Bundles.cs and are the contract with this file:
// icons "<sheet>:<graphic>", parts "<category>:<id>:<clip>", effects "<id>:<frameIndex>".
//
// The committed bundles only carry the four down-facing resting clips (idle-down,
// idle-no-equip-down, idle-equip-down, mounted-idle-down), which is exactly what a static
// south-facing preview needs. Nothing here has a direction or motion parameter because there is
// no other art to select; adding a facing control means regenerating the bundle first.
var Sprites = (function () {
  // Every id here can arrive as a spreadsheet cell, i.e. a STRING, and the key is built by
  // concatenation — so '01', ' 1' and 1 must all resolve to the same sprite instead of missing.
  // parseInt(v, 10), matching Equipped.num() and Appearance.num() so the three modules cannot
  // disagree about what a cell means.
  function num(value) {
    var n = parseInt(value, 10);
    return isNaN(n) ? 0 : n;
  }

  function channel(value) {
    return Math.min(255, Math.max(0, value));
  }

  // Resting-pose clip preference, from AnimationNames.Candidates("idle", bodyState, Down)
  // (Scripts/Character/AnimationNames.cs:41-42): the unarmed list ends with -equip because Hands
  // weapon and shield sheets only ever ship idle-equip. bodyState's 4/5/6/7 weapon variants only
  // affect attack clips, so a boolean is all the resting pose can use.
  function clipCandidates(equipped) {
    return equipped
      ? ['idle-equip-down', 'idle-down', 'idle-no-equip-down']
      : ['idle-no-equip-down', 'idle-down', 'idle-equip-down'];
  }

  function icon(bundles, sheet, graphic) {
    var s = num(sheet);
    var g = num(graphic);
    // 0 means "no graphic"; a negative id indexes nothing. Neither appears in the bundle, so
    // this is a shortcut rather than a rule — but it keeps a blank cell from being a lookup.
    if (s <= 0 || g <= 0) return null;

    var b = bundles.icons;
    if (!b) return null;
    return b.rects[s + ':' + g] || null;
  }

  function part(bundles, category, id, equipped) {
    var n = num(id);
    if (n <= 0) return null;

    var b = bundles.parts;
    if (!b) return null;

    var candidates = clipCandidates(equipped);
    for (var i = 0; i < candidates.length; i++) {
      var rect = b.rects[category + ':' + n + ':' + candidates[i]];
      if (rect) return rect;
    }
    // Missing art hides the slot — never substitute another clip. mounted-idle-down is
    // deliberately NOT a fallback: the one Chest id that has only a mounted sheet would
    // otherwise draw a mounted pose on a standing character.
    return null;
  }

  // Frames of an effect animation, in order. Each effect is a single clip whose frames are
  // numbered contiguously from 0 (Bundles.cs Effects), verified across all 560 effects in the
  // committed bundle, so stopping at the first absent index is exhaustive. The loop needs no cap:
  // rects has finitely many keys, so a missing index is always reached.
  function effectFrames(bundles, effectId) {
    var id = num(effectId);
    var b = bundles.effects;
    if (!b || id <= 0) return [];

    var frames = [];
    for (var i = 0; ; i++) {
      var rect = b.rects[id + ':' + i];
      if (!rect) break;
      frames.push(rect);
    }
    return frames;
  }

  // Icon.cs:9-11 — COLOR.rgb = mix(t.rgb, tint.rgb, tint.a), COLOR.a = t.a. tint.a is a BLEND
  // FACTOR, not opacity: it never touches the source alpha, so a transparent pixel stays
  // transparent and a zero factor is NoTint. Icon.cs:23 is where the /255 lives.
  //
  // Always returns a fresh array, including on the no-tint path: aliasing the caller's pixel
  // array would make "did this tint?" observable through a later mutation.
  function applyTint(px, tint) {
    if (!tint || !tint.a) return [px[0], px[1], px[2], px[3]];

    // Appearance already clamps what it emits, but applyTint is public and a hand-built tint
    // can be out of range; a channel outside 0-255 would silently wrap in a Uint8ClampedArray's
    // neighbours' favour rather than erroring.
    var f = channel(tint.a) / 255;
    return [
      channel(Math.round(px[0] + (channel(tint.r) - px[0]) * f)),
      channel(Math.round(px[1] + (channel(tint.g) - px[1]) * f)),
      channel(Math.round(px[2] + (channel(tint.b) - px[2]) * f)),
      px[3],
    ];
  }

  // Tints an RGBA byte buffer in place. Split out of draw() so the pixel maths is reachable
  // without a DOM: draw() itself is only canvas plumbing around this.
  function tintPixels(data, tint) {
    // Pure optimisation, not a rule: applyTint already treats a missing or zero blend factor as
    // the identity, so removing this line changes only how long a full-sprite no-op takes.
    if (!tint || !tint.a) return;

    for (var i = 0; i < data.length; i += 4) {
      var out = applyTint([data[i], data[i + 1], data[i + 2], data[i + 3]], tint);
      data[i] = out[0];
      data[i + 1] = out[1];
      data[i + 2] = out[2];
      data[i + 3] = out[3];
    }
  }

  // Draws one rect from a bundle onto a canvas context, applying the tint per-pixel when needed.
  // Tinting requires pixel access, so it goes through an offscreen canvas.
  function draw(ctx, image, rect, dx, dy, tint) {
    if (!rect) return;

    if (!tint || !tint.a) {
      ctx.drawImage(image, rect[0], rect[1], rect[2], rect[3], dx, dy, rect[2], rect[3]);
      return;
    }

    var off = document.createElement('canvas');
    off.width = rect[2];
    off.height = rect[3];
    var octx = off.getContext('2d');
    octx.drawImage(image, rect[0], rect[1], rect[2], rect[3], 0, 0, rect[2], rect[3]);

    var data = octx.getImageData(0, 0, rect[2], rect[3]);
    tintPixels(data.data, tint);
    octx.putImageData(data, 0, 0);

    ctx.drawImage(off, dx, dy);
  }

  return {
    icon: icon, part: part, effectFrames: effectFrames,
    applyTint: applyTint, tintPixels: tintPixels, draw: draw, clipCandidates: clipCandidates,
  };
})();

if (typeof module !== 'undefined') module.exports = { Sprites: Sprites };
