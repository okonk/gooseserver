// Sprite lookup and tinting against the GOOSE_SPRITES bundles from tools/SpriteBundle. The three
// key schemes are minted in tools/SpriteBundle/Bundles.cs and are the contract with this file:
// icons "<sheet>:<graphic>", parts "<category>:<id>:<clip>", effects "<id>:<frameIndex>".
//
// WHAT THE BUNDLE CONSTRAINS. The committed bundles carry ONLY the four down-facing resting
// clips: idle-down, idle-no-equip-down, idle-equip-down and mounted-idle-down. That is exactly
// what a static south-facing preview needs, and it fixes two signatures here:
//
//   * No function takes a direction or a motion. There is no walk, attack, cast or non-down art
//     in the bundle to select, so a facing or animation control means REGENERATING THE BUNDLE
//     (tools/SpriteBundle, via its PartClips config) before it means changing this file.
//   * part() takes a BOOLEAN `equipped`, not the client's bodyState. bodyState's 4/5/6/7 weapon
//     variants only ever distinguish attack clips (AnimationNames.AttackVariant), and there are
//     no attack clips here — so the only thing the resting pose can use is armed vs unarmed.
//     Callers compute it as `bodyState !== 3`; this file has no reason to know that encoding.
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
  // (Scripts/Character/AnimationNames.cs:44-45): the unarmed list ends with -equip because Hands
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

  // The mounted resting pose for a body id. Mounts are just bodies (Appearance.CATEGORY maps
  // Mount -> Bodies), and only four of the 305 body ids ship a mounted-idle-down clip.
  //
  // NOTHING IN THIS EDITOR'S DATA CURRENTLY PRODUCES A MOUNT: appearance.js's closing note
  // explains why — the client reserves equipped slot 6 for one, but ParseEquippedItems only
  // fills slots 0-5 and the NPCs sheet has no mount column, so Appearance.layers never emits a
  // Mount layer. It has NO production caller — preview.js deliberately has no mount branch (see
  // its header) — and is kept only because the mount is a real fact about the client's atlas that
  // a future mount column would need. Deliberate, not forgotten.
  //
  // Never falls back to a standing clip: substituting idle-down would draw a body on foot in
  // the place the mount belongs.
  function mount(bundles, id) {
    var n = num(id);
    if (n <= 0) return null;

    var b = bundles.parts;
    if (!b) return null;
    return b.rects['Bodies:' + n + ':mounted-idle-down'] || null;
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

    // Appearance already clamps what it emits, but applyTint is public and a hand-built tint can
    // be out of range. Both clamps below are load-bearing and each covers a different failure:
    // an unclamped f leaves [0,1] and pushes the result PAST the tint colour (or, for a negative
    // alpha, away from it); an unclamped tint channel does the same at partial alpha, where the
    // byte range alone would not catch it.
    //
    // The OUTPUT needs no clamp: with f in [0,1], the tint channel in [0,255], and px in [0,255]
    // (every caller feeds a Uint8ClampedArray), each result is a convex combination of two valid
    // bytes and so is one itself. All three premises are needed — an out-of-range px would escape.
    var f = channel(tint.a) / 255;
    return [
      Math.round(px[0] + (channel(tint.r) - px[0]) * f),
      Math.round(px[1] + (channel(tint.g) - px[1]) * f),
      Math.round(px[2] + (channel(tint.b) - px[2]) * f),
      px[3],
    ];
  }

  // Tints an RGBA byte buffer in place. Split out of draw() so that draw() is nothing but canvas
  // plumbing and the per-pixel assertions have somewhere to live that does not need a canvas at
  // all. (Not for reachability: the stub canvas in the tests drives draw() end to end.)
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
    // No rect means the id has no art; no image means its bundle PNG has not decoded yet. Both
    // skip the layer. drawImage(null, ...) is a TypeError in a real DOM, which would abort the
    // whole render over one not-yet-loaded bundle rather than leaving one layer blank.
    if (!rect || !image) return;

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
    icon: icon, part: part, mount: mount, effectFrames: effectFrames,
    applyTint: applyTint, tintPixels: tintPixels, draw: draw, clipCandidates: clipCandidates,
  };
})();

if (typeof module !== 'undefined') module.exports = { Sprites: Sprites };
