# Ground Zone Styles — Part 2: Client Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** The Godot client draws each ground zone from a named style (tinted floor, edges along the
zone's outside, corner marks, particles) instead of only a looping per-tile sprite, and previews styled
spells with the same look.

**Architecture:** Pure helpers do the thinking and are unit-tested: `ZoneEdges` (open sides and corners
from a tile set), `ZoneStyles` (the style table and the what-to-draw decision) and `ZoneParticlePoints`
(seeded emission points and particle counts). One `ZoneView : Node2D` per zone draws in `_Draw` and owns
one `CpuParticles2D`; `GroundZoneLayer` creates, fades and frees views, and hosts the two previews.

**Tech Stack:** C# / Godot 4.6 (GodotSharp 4.6.2), xunit 2.9.2. Tests compile every file under `Scripts/`
(`tests/Goose2Client.Tests/Goose2Client.Tests.csproj:14`), so pure helpers and Godot structs (`Color`,
`Vector2`) are testable; nodes are not.

**Design doc:** `/home/hayden/code/illutiagooseserver/.worktrees/zone-styles/docs/plans/2026-11-28-zone-styles-design.md`
(Part 2 of 2). **Requires Part 1** (server) for the manual checks in Task 8 only.

**Worktree:** `/home/hayden/code/Goose2ClientGodot/.worktrees/zone-styles`, branch `zone-styles`.
Baseline: `tests/Goose2Client.Tests` 1889 passed, 1 failed —
`StylistCatalogTests.EveryCatalogId_HasAnimationAsset` fails in any fresh worktree because generated
sprite assets are untracked; it passes in the main checkout. Ignore it throughout.

**House rule (AGENTS.md):** no new comments or doc strings unless the *why* is non-obvious.

**Commands:**
- Tests: `dotnet test tests/Goose2Client.Tests --filter <filter>`
- Game build (compiles `ZoneView` and the layer, which have no unit tests): `dotnet build Goose2ClientGodot.csproj`

---

## APIs verified

| API | Where | Notes |
|---|---|---|
| `ZoneAddPacket` | `Scripts/Network/Packets/ZoneAddPacket.cs` | reads `id, anim, file(ignored), remainingMs`, then tile pairs while `TokensRemaining() >= 2`, converting to 0-based |
| `SpellbookSlotPacket` | `Scripts/Network/Packets/SpellbookSlotPacket.cs:35-61` | ground/shape/size read when `TokensRemaining() >= 3` |
| `PacketParser` | `Scripts/Network/PacketParser.cs` | `GetString()` returns `""` for an empty token; `GetNextToken` throws at end of packet; `LengthRemaining()` is 0 after a trailing delimiter; `TokensRemaining()` ignores a trailing delimiter (`:106-120`) |
| `SpellInfo.FromPacket` | `Scripts/SpellInfo.cs:19-33` | copies packet fields |
| Existing client packet tests | `tests/Goose2Client.Tests/ZonePacketTests.cs`, `SpellbookSlotPacketTests.cs:37-50` | |
| `GroundZoneLayer` | `Scripts/Map/GroundZoneLayer.cs` | `Add`, `Remove`, `_Process` expiry via `ZoneBook`, `ShowPreview(tiles, color)`, `HidePreview()` |
| `ZoneBook.Add` returns false for a known id | `Scripts/Map/ZoneBook.cs:10-15` | |
| Layer created at z 12 | `Scripts/MapManager.cs:106`; packets wired `:131-132`, `:475-477` | |
| Preview caller | `Scripts/SpellTargetManager.cs:157-175` (`UpdatePreview`, called each frame from `_Process` `:46`) | |
| `SpellAnimation.Setup(int animationId, Vector2 spriteOffset = default, bool loop = false)` | `Scripts/Overlays/SpellAnimation.cs:10` | returns false when the animation is missing |
| `MapCoords.TileSize = 32`, `TileBottomCenter` | `Scripts/Map/MapCoords.cs:10,18` | Y-down; tile `(x, y)` covers world `[x*32, (x+1)*32)` |
| Generated textures | `Image.CreateEmpty(w, h, false, Image.Format.Rgba8)` + `ImageTexture.CreateFromImage` — `Scripts/Map/AmbienceManager.cs:117-129` | |
| Tweens | `CreateTween().TweenProperty(node, "prop", value, seconds)` — `Scripts/UI/…` (`TweenProperty` usages); `Tween.TweenCallback` exists in GodotSharp 4.6.2 | |
| `CpuParticles2D` members | GodotSharp 4.6.2 XML: `Amount`, `Lifetime`, `Emitting`, `Preprocess`, `EmissionShape`, `EmissionShapeEnum.Points`, `EmissionPoints` (`Vector2[]`), `Direction`, `Spread`, `Gravity`, `InitialVelocityMin/Max`, `ColorRamp` (`Gradient`), `Texture` | |
| `CanvasItem.DrawRect`, `DrawLine`, `DrawCircle` | GodotSharp 4.6.2; `DrawCircle` used at `Scripts/UI/…:57` | |

---

### Task 1: Parse the style from `ZADD` and `SSS`

**Files:**
- Modify: `Scripts/Network/Packets/ZoneAddPacket.cs`
- Modify: `Scripts/Network/Packets/SpellbookSlotPacket.cs`
- Modify: `Scripts/SpellInfo.cs`
- Test: `tests/Goose2Client.Tests/ZonePacketTests.cs`, `tests/Goose2Client.Tests/SpellbookSlotPacketTests.cs`

**Step 1: Update and add the tests**

`ZonePacketTests.cs` — replace the two `ZADD` tests with the new format and add an empty-style case:

```csharp
    [Fact]
    public void ZoneAdd_ParsesHeaderAndConvertsTilesToZeroBased()
    {
        var p = (ZoneAddPacket)new ZoneAddPacket().Parse(new PacketParser("ZADD7,120,4,heal,8000,10,10,11,10,10,11", "ZADD"));

        Assert.Equal(7, p.Id);
        Assert.Equal(120, p.AnimationId);
        Assert.Equal("heal", p.Style);
        Assert.Equal(8000, p.RemainingMs);
        Assert.Equal(new[] { (9, 9), (10, 9), (9, 10) }, p.Tiles);
    }

    [Fact]
    public void ZoneAdd_WithAnEmptyStyle_ParsesEmpty()
    {
        var p = (ZoneAddPacket)new ZoneAddPacket().Parse(new PacketParser("ZADD3,0,0,,1000,5,5", "ZADD"));

        Assert.Equal("", p.Style);
        Assert.Equal(1000, p.RemainingMs);
        Assert.Equal(new[] { (4, 4) }, p.Tiles);
    }

    [Fact]
    public void ZoneAdd_WithNoTiles_ParsesEmpty()
    {
        var p = (ZoneAddPacket)new ZoneAddPacket().Parse(new PacketParser("ZADD7,120,4,heal,8000", "ZADD"));

        Assert.Empty(p.Tiles);
    }
```

`SpellbookSlotPacketTests.cs` — add inside the class:

```csharp
        [Fact]
        public void ParsesZoneStyle()
        {
            var p = (SpellbookSlotPacket)new SpellbookSlotPacket().Parse(new PacketParser("SSS1,Sanctuary,0,1000,0,1,100,3,20000,0,1,5,3,heal", "SSS"));
            Assert.Equal(3, p.ShapeSize);
            Assert.Equal("heal", p.ZoneStyle);
        }

        [Fact]
        public void EmptyZoneStyle_ParsesEmpty()
        {
            var p = (SpellbookSlotPacket)new SpellbookSlotPacket().Parse(new PacketParser("SSS1,Fireball,5,1000,0,1,100,3,20000,0,0,0,0,", "SSS"));
            Assert.Equal("", p.ZoneStyle);
        }

        [Fact]
        public void PacketWithoutZoneStyle_ParsesEmpty()
        {
            var p = (SpellbookSlotPacket)new SpellbookSlotPacket().Parse(new PacketParser("SSS1,Fire Patch,5,1000,0,1,100,3,20000,0,1,5,2", "SSS"));
            Assert.Equal("", p.ZoneStyle);
            Assert.Equal("", SpellInfo.FromPacket(p).ZoneStyle);
        }
```

**Step 2: Run to verify they fail**

Run: `dotnet test tests/Goose2Client.Tests --filter "FullyQualifiedName~ZonePacketTests|FullyQualifiedName~SpellbookSlotPacketTests"`
Expected: build error — no `Style` / `ZoneStyle`.

**Step 3: Implement**

`ZoneAddPacket.cs` — add the property and read it after the ignored file field:

```csharp
        public string Style { get; set; } = "";
```

```csharp
        // ZADD id,anim,file,style,remainingMs,x1,y1,… — tiles are 1-based on the wire; the file is unused, as with SPA.
        public override object Parse(PacketParser p)
        {
            var packet = new ZoneAddPacket
            {
                Id = p.GetInt32(),
                AnimationId = p.GetInt32(),
            };
            p.GetInt32();
            packet.Style = p.GetString();
            packet.RemainingMs = p.GetInt64();
```

(the tile loop below is unchanged).

`SpellbookSlotPacket.cs` — add `public string ZoneStyle { get; set; } = "";` after `ShapeSize`, and inside
the `TokensRemaining() >= 3` block after `packet.ShapeSize = p.GetInt32();`:

```csharp
                    if (p.LengthRemaining() > 0)
                        packet.ZoneStyle = p.GetString();
```

`SpellInfo.cs` — add `public string ZoneStyle { get; set; } = "";` after `ShapeSize`, and
`ZoneStyle = packet.ZoneStyle ?? "",` in `FromPacket` after `ShapeSize = packet.ShapeSize,`.

**Step 4: Run the tests**

Run: `dotnet test tests/Goose2Client.Tests --filter "FullyQualifiedName~ZonePacketTests|FullyQualifiedName~SpellbookSlotPacketTests|FullyQualifiedName~SpellInfoTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add Scripts/Network/Packets/ZoneAddPacket.cs Scripts/Network/Packets/SpellbookSlotPacket.cs Scripts/SpellInfo.cs \
        tests/Goose2Client.Tests/ZonePacketTests.cs tests/Goose2Client.Tests/SpellbookSlotPacketTests.cs
git commit -m "feat(spells): parse zone style from ZADD and SSS"
```

---

### Task 2: `ZoneEdges` — open sides and corners

**Files:**
- Create: `Scripts/Map/ZoneEdges.cs`
- Test: `tests/Goose2Client.Tests/ZoneEdgesTests.cs`

**Step 1: Write the failing tests**

```csharp
using System.Collections.Generic;
using System.Linq;
using Goose2Client.Map;
using Xunit;

namespace Goose2Client.Tests;

public class ZoneEdgesTests
{
    private static List<(int X, int Y)> Square(int x0, int y0, int size) =>
        (from y in Enumerable.Range(y0, size) from x in Enumerable.Range(x0, size) select (x, y)).ToList();

    private static ZoneTileEdges At(List<ZoneTileEdges> edges, int x, int y) => edges.Single(e => e.X == x && e.Y == y);

    [Fact]
    public void A_single_tile_is_open_on_every_side_with_four_corners()
    {
        var e = Assert.Single(ZoneEdges.Compute(new[] { (5, 5) }));

        Assert.Equal(ZoneSides.Up | ZoneSides.Right | ZoneSides.Down | ZoneSides.Left, e.Open);
        Assert.Equal(4, e.OuterCorners.Count);
        Assert.Empty(e.InnerCorners);
    }

    [Fact]
    public void A_square_has_four_outer_corners_and_no_inner_ones()
    {
        var edges = ZoneEdges.Compute(Square(10, 10, 7));

        Assert.Equal(4, edges.Sum(e => e.OuterCorners.Count));
        Assert.Equal(0, edges.Sum(e => e.InnerCorners.Count));
        Assert.Equal(new[] { ZoneCorner.TopLeft }, At(edges, 10, 10).OuterCorners);
        Assert.Equal(new[] { ZoneCorner.BottomRight }, At(edges, 16, 16).OuterCorners);
        Assert.Equal(ZoneSides.Up, At(edges, 13, 10).Open);
        Assert.Equal(ZoneSides.None, At(edges, 13, 13).Open);
    }

    [Fact]
    public void A_plus_has_inner_corners_at_its_notches()
    {
        var plus = new[] { (5, 4), (4, 5), (5, 5), (6, 5), (5, 6) };

        var edges = ZoneEdges.Compute(plus);

        Assert.Equal(new[] { ZoneCorner.TopLeft, ZoneCorner.TopRight, ZoneCorner.BottomRight, ZoneCorner.BottomLeft },
                     At(edges, 5, 5).InnerCorners);
        Assert.Empty(At(edges, 5, 5).OuterCorners);
        Assert.Equal(ZoneSides.Up | ZoneSides.Left | ZoneSides.Right, At(edges, 5, 4).Open);
    }

    [Fact]
    public void A_zone_clipped_by_the_map_edge_still_outlines_its_remaining_tiles()
    {
        var clipped = Square(-3, -3, 7).Where(t => t.X >= 0 && t.Y >= 0).ToList();

        var edges = ZoneEdges.Compute(clipped);

        Assert.Equal(16, edges.Count);
        Assert.Equal(new[] { ZoneCorner.TopLeft }, At(edges, 0, 0).OuterCorners);
        Assert.Equal(4, edges.Sum(e => e.OuterCorners.Count));
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~ZoneEdgesTests`
Expected: build error — `ZoneEdges` doesn't exist.

**Step 3: Implement** `Scripts/Map/ZoneEdges.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Goose2Client.Map
{
    [Flags]
    public enum ZoneSides { None = 0, Up = 1, Right = 2, Down = 4, Left = 8 }

    public enum ZoneCorner { TopLeft, TopRight, BottomRight, BottomLeft }

    public sealed record ZoneTileEdges(int X, int Y, ZoneSides Open,
        IReadOnlyList<ZoneCorner> OuterCorners, IReadOnlyList<ZoneCorner> InnerCorners);

    public static class ZoneEdges
    {
        public static List<ZoneTileEdges> Compute(IEnumerable<(int X, int Y)> tiles)
        {
            var set = tiles.ToHashSet();
            var result = new List<ZoneTileEdges>();

            foreach (var (x, y) in set.OrderBy(t => t.Y).ThenBy(t => t.X))
            {
                bool up = !set.Contains((x, y - 1));
                bool right = !set.Contains((x + 1, y));
                bool down = !set.Contains((x, y + 1));
                bool left = !set.Contains((x - 1, y));

                var open = (up ? ZoneSides.Up : 0) | (right ? ZoneSides.Right : 0) |
                           (down ? ZoneSides.Down : 0) | (left ? ZoneSides.Left : 0);

                var outer = new List<ZoneCorner>();
                if (up && left) outer.Add(ZoneCorner.TopLeft);
                if (up && right) outer.Add(ZoneCorner.TopRight);
                if (down && right) outer.Add(ZoneCorner.BottomRight);
                if (down && left) outer.Add(ZoneCorner.BottomLeft);

                var inner = new List<ZoneCorner>();
                if (!up && !left && !set.Contains((x - 1, y - 1))) inner.Add(ZoneCorner.TopLeft);
                if (!up && !right && !set.Contains((x + 1, y - 1))) inner.Add(ZoneCorner.TopRight);
                if (!down && !right && !set.Contains((x + 1, y + 1))) inner.Add(ZoneCorner.BottomRight);
                if (!down && !left && !set.Contains((x - 1, y + 1))) inner.Add(ZoneCorner.BottomLeft);

                result.Add(new ZoneTileEdges(x, y, open, outer, inner));
            }

            return result;
        }
    }
}
```

**Step 4: Run the tests**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~ZoneEdgesTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Scripts/Map/ZoneEdges.cs tests/Goose2Client.Tests/ZoneEdgesTests.cs
git commit -m "feat(zones): work out zone outlines from tile neighbours"
```

---

### Task 3: `ZoneStyle`, the style table and the what-to-draw decision

**Files:**
- Create: `Scripts/Map/ZoneStyles.cs`
- Test: `tests/Goose2Client.Tests/ZoneStylesTests.cs`

**Step 1: Write the failing tests**

```csharp
using Goose2Client.Map;
using Xunit;

namespace Goose2Client.Tests;

public class ZoneStylesTests
{
    [Theory]
    [InlineData("heal")]
    [InlineData("earthquake")]
    [InlineData("empower")]
    [InlineData("poison")]
    public void Known_styles_are_found(string name)
    {
        var style = ZoneStyles.Get(name);

        Assert.NotNull(style);
        Assert.NotSame(ZoneStyles.Default, style);
    }

    [Fact]
    public void Lookup_ignores_case()
    {
        Assert.Same(ZoneStyles.Get("heal"), ZoneStyles.Get("HeAl"));
    }

    [Fact]
    public void An_empty_name_has_no_style()
    {
        Assert.Null(ZoneStyles.Get(""));
        Assert.Null(ZoneStyles.Get(null));
    }

    [Fact]
    public void An_unknown_name_falls_back_to_the_default()
    {
        Assert.Same(ZoneStyles.Default, ZoneStyles.Get("sparkles"));
    }

    [Fact]
    public void A_known_style_draws_with_the_packet_sprite_when_it_has_none()
    {
        var look = ZoneStyles.Resolve("heal", 815015);

        Assert.Same(ZoneStyles.Get("heal"), look.Style);
        Assert.Equal(815015, look.TileAnimation);
    }

    [Fact]
    public void An_unknown_style_draws_the_default_outline_and_keeps_the_sprite()
    {
        var look = ZoneStyles.Resolve("sparkles", 815015);

        Assert.Same(ZoneStyles.Default, look.Style);
        Assert.Equal(815015, look.TileAnimation);
    }

    [Fact]
    public void No_style_with_a_sprite_draws_only_the_sprite()
    {
        var look = ZoneStyles.Resolve("", 815015);

        Assert.Null(look.Style);
        Assert.Equal(815015, look.TileAnimation);
    }

    [Fact]
    public void No_style_and_no_sprite_draws_the_default_outline()
    {
        var look = ZoneStyles.Resolve("", 0);

        Assert.Same(ZoneStyles.Default, look.Style);
        Assert.Equal(0, look.TileAnimation);
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~ZoneStylesTests`
Expected: build error.

**Step 3: Implement** `Scripts/Map/ZoneStyles.cs` (values from the design's §4 table):

```csharp
using System;
using System.Collections.Generic;
using Godot;

namespace Goose2Client.Map
{
    public enum ZoneFloorPattern { None, Cracks, Haze }

    public enum ZoneEdgeKind { Solid, Rough, Soft }

    public enum ZoneParticle { None, Plus, Dust, Ember, Fog }

    public sealed record ZoneStyle
    {
        public Color Floor { get; init; }
        public float FloorPulse { get; init; }
        public float PulseSeconds { get; init; } = 1f;
        public ZoneFloorPattern FloorPattern { get; init; }
        public Color Edge { get; init; }
        public float EdgeWidth { get; init; } = 1f;
        public ZoneEdgeKind EdgeKind { get; init; }
        public Color Corner { get; init; }
        public float CornerLength { get; init; }
        public ZoneParticle Particle { get; init; }
        public Color ParticleColor { get; init; }
        public float ParticleRate { get; init; }
        public float ParticleLifetime { get; init; } = 1f;
        public float ParticleRise { get; init; }
        public int TileAnimation { get; init; }
    }

    public readonly record struct ZoneLook(ZoneStyle? Style, int TileAnimation);

    public static class ZoneStyles
    {
        public static readonly ZoneStyle Default = new()
        {
            Floor = new Color(1f, 1f, 1f, 0.06f),
            Edge = new Color(1f, 1f, 1f, 0.5f), EdgeWidth = 1f, EdgeKind = ZoneEdgeKind.Solid,
        };

        private static readonly Dictionary<string, ZoneStyle> Named = new(StringComparer.OrdinalIgnoreCase)
        {
            ["heal"] = new()
            {
                Floor = new Color(0.25f, 0.85f, 0.35f, 0.14f), FloorPulse = 0.3f, PulseSeconds = 2f,
                Edge = new Color(0.45f, 1f, 0.55f), EdgeWidth = 2f, EdgeKind = ZoneEdgeKind.Solid,
                Corner = new Color(0.7f, 1f, 0.75f), CornerLength = 6f,
                Particle = ZoneParticle.Plus, ParticleColor = new Color(0.4f, 1f, 0.5f),
                ParticleRate = 0.4f, ParticleLifetime = 1.2f, ParticleRise = 18f,
            },
            ["earthquake"] = new()
            {
                Floor = new Color(0.35f, 0.24f, 0.14f, 0.22f), FloorPulse = 0.15f, PulseSeconds = 0.6f,
                FloorPattern = ZoneFloorPattern.Cracks,
                Edge = new Color(0.6f, 0.45f, 0.3f), EdgeWidth = 2f, EdgeKind = ZoneEdgeKind.Rough,
                Particle = ZoneParticle.Dust, ParticleColor = new Color(0.65f, 0.52f, 0.38f),
                ParticleRate = 0.3f, ParticleLifetime = 0.8f, ParticleRise = 8f,
            },
            ["empower"] = new()
            {
                Floor = new Color(0.8f, 0.12f, 0.15f, 0.14f), FloorPulse = 0.35f, PulseSeconds = 1.5f,
                Edge = new Color(1f, 0.3f, 0.3f), EdgeWidth = 2f, EdgeKind = ZoneEdgeKind.Solid,
                Corner = new Color(1f, 0.5f, 0.25f), CornerLength = 6f,
                Particle = ZoneParticle.Ember, ParticleColor = new Color(1f, 0.55f, 0.2f),
                ParticleRate = 0.5f, ParticleLifetime = 0.9f, ParticleRise = 24f,
            },
            ["poison"] = new()
            {
                Floor = new Color(0.6f, 0.75f, 0.15f, 0.12f), FloorPulse = 0.25f, PulseSeconds = 3f,
                FloorPattern = ZoneFloorPattern.Haze,
                Edge = new Color(0.7f, 0.85f, 0.25f), EdgeWidth = 1f, EdgeKind = ZoneEdgeKind.Soft,
                Particle = ZoneParticle.Fog, ParticleColor = new Color(0.65f, 0.8f, 0.2f, 0.5f),
                ParticleRate = 0.15f, ParticleLifetime = 3f, ParticleRise = 0f,
            },
        };

        public static ZoneStyle? Get(string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Named.TryGetValue(name, out var style) ? style : Default;
        }

        public static ZoneLook Resolve(string? styleName, int packetAnimation)
        {
            var style = Get(styleName);
            if (style is null) return packetAnimation != 0 ? new(null, packetAnimation) : new(Default, 0);
            return new(style, style.TileAnimation != 0 ? style.TileAnimation : packetAnimation);
        }
    }
}
```

**Step 4: Run the tests**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~ZoneStylesTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Scripts/Map/ZoneStyles.cs tests/Goose2Client.Tests/ZoneStylesTests.cs
git commit -m "feat(zones): add zone style table and draw decision"
```

---

### Task 4: Particle points, counts, noise and textures

**Files:**
- Create: `Scripts/Map/ZoneParticles.cs`
- Test: `tests/Goose2Client.Tests/ZoneParticlesTests.cs`

**Step 1: Write the failing tests**

```csharp
using System.Linq;
using Goose2Client.Map;
using Xunit;

namespace Goose2Client.Tests;

public class ZoneParticlesTests
{
    private static readonly (int X, int Y)[] Plus = { (5, 4), (4, 5), (5, 5), (6, 5), (5, 6) };

    [Fact]
    public void Points_stay_inside_the_zone_tiles()
    {
        var points = ZoneParticles.Points(Plus, seed: 7);

        Assert.Equal(Plus.Length * ZoneParticles.PointsPerTile, points.Length);
        Assert.All(points, p => Assert.Contains(
            ((int)(p.X / MapCoords.TileSize), (int)(p.Y / MapCoords.TileSize)), Plus));
    }

    [Fact]
    public void The_same_seed_gives_the_same_points()
    {
        Assert.Equal(ZoneParticles.Points(Plus, 7), ZoneParticles.Points(Plus, 7));
        Assert.NotEqual(ZoneParticles.Points(Plus, 7), ZoneParticles.Points(Plus, 8));
    }

    [Fact]
    public void Amount_scales_with_tiles_and_is_capped()
    {
        var heal = ZoneStyles.Get("heal")!;

        Assert.Equal(2, ZoneParticles.Amount(heal, 4));
        Assert.Equal(ZoneParticles.MaxAlive, ZoneParticles.Amount(heal, 361));
        Assert.Equal(1, ZoneParticles.Amount(heal, 1));
    }

    [Fact]
    public void Noise_is_stable_and_in_range()
    {
        Assert.Equal(ZoneParticles.Noise(3, 4, 5, 6), ZoneParticles.Noise(3, 4, 5, 6));
        Assert.All(Enumerable.Range(0, 200), i => Assert.InRange(ZoneParticles.Noise(1, i, -i, 2), 0f, 1f));
    }
}
```

(`heal`: rate 0.4 × 4 tiles × 1.2s = 1.92 → 2; 361 tiles → 173 → capped at 60; 1 tile → 0.48 → at least 1.)

**Step 2: Run to verify they fail**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~ZoneParticlesTests`
Expected: build error.

**Step 3: Implement** `Scripts/Map/ZoneParticles.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Goose2Client.Map
{
    public static class ZoneParticles
    {
        public const int PointsPerTile = 4;
        public const int MaxAlive = 60;

        public static Vector2[] Points(IEnumerable<(int X, int Y)> tiles, int seed)
        {
            var rng = new Random(seed);
            var points = new List<Vector2>();
            foreach (var (x, y) in tiles.OrderBy(t => t.Y).ThenBy(t => t.X))
                for (int i = 0; i < PointsPerTile; i++)
                    points.Add(new Vector2(
                        (x + 0.05f + 0.9f * (float)rng.NextDouble()) * MapCoords.TileSize,
                        (y + 0.05f + 0.9f * (float)rng.NextDouble()) * MapCoords.TileSize));
            return points.ToArray();
        }

        public static int Amount(ZoneStyle style, int tiles) =>
            Math.Clamp((int)MathF.Round(style.ParticleRate * tiles * style.ParticleLifetime), 1, MaxAlive);

        public static float Noise(int seed, int x, int y, int i)
        {
            unchecked
            {
                uint h = (uint)seed * 374761393u + (uint)x * 668265263u + (uint)y * 2147483647u + (uint)i * 1274126177u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        private static readonly Dictionary<ZoneParticle, Texture2D> Textures = new();

        public static Texture2D Texture(ZoneParticle kind)
        {
            if (Textures.TryGetValue(kind, out var cached)) return cached;
            return Textures[kind] = kind switch
            {
                ZoneParticle.Plus => Build(7, (x, y) => x == 3 || y == 3 ? 1f : 0f),
                ZoneParticle.Ember => Build(2, (_, _) => 1f),
                ZoneParticle.Dust => Soft(4, 1f),
                _ => Soft(24, 0.5f),
            };
        }

        private static Texture2D Soft(int size, float peak)
        {
            float c = (size - 1) / 2f;
            return Build(size, (x, y) =>
                peak * Mathf.Clamp(1f - Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (c + 0.5f), 0f, 1f));
        }

        private static Texture2D Build(int size, Func<int, int, float> alpha)
        {
            var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y)));
            return ImageTexture.CreateFromImage(image);
        }
    }
}
```

`Texture` touches `Image`, which needs the engine; the tests above never call it.

**Step 4: Run the tests**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~ZoneParticlesTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Scripts/Map/ZoneParticles.cs tests/Goose2Client.Tests/ZoneParticlesTests.cs
git commit -m "feat(zones): seeded particle points, counts and textures"
```

---

### Task 5: `ZoneView` — draw floor, patterns, edges, corners; sprites and particles

**Files:**
- Create: `Scripts/Map/ZoneView.cs`

No unit tests (node drawing); Task 8 checks it in game. The view draws in world coordinates and sits at
the layer's origin.

**Step 1: Implement** `Scripts/Map/ZoneView.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using Godot;
using Goose2Client.Overlays;

namespace Goose2Client.Map
{
    public partial class ZoneView : Node2D
    {
        public const float FadeSeconds = 0.3f;

        private List<ZoneTileEdges> _edges = new();
        private List<(int X, int Y)> _tiles = new();
        private ZoneStyle? _style;
        private int _seed;
        private bool _preview;
        private bool _usable = true;
        private float _time;
        private CpuParticles2D? _particles;

        public void Setup(int seed, IReadOnlyCollection<(int X, int Y)> tiles, ZoneLook look)
        {
            _seed = seed;
            _style = look.Style;
            SetTiles(tiles);

            if (look.TileAnimation != 0)
                foreach (var (x, y) in _tiles)
                {
                    var s = new SpellAnimation { Name = $"Tile({x},{y})" };
                    AddChild(s);
                    if (!s.Setup(look.TileAnimation, new Vector2(0, -MapCoords.TileSize / 2f), loop: true)) { s.QueueFree(); continue; }
                    s.Position = MapCoords.TileBottomCenter(x, y);
                }

            if (_style is { Particle: not ZoneParticle.None } style)
                AddChild(_particles = CreateParticles(style));
        }

        public void SetPreview(List<(int X, int Y)> tiles, ZoneStyle style, bool usable)
        {
            if (_preview && _style == style && _usable == usable && _tiles.SequenceEqual(tiles)) return;
            _preview = true;
            _style = style;
            _usable = usable;
            SetTiles(tiles);
        }

        public void FadeIn()
        {
            Modulate = Modulate with { A = 0f };
            CreateTween().TweenProperty(this, "modulate:a", 1f, FadeSeconds);
        }

        public void FadeOutAndFree()
        {
            if (_particles != null) _particles.Emitting = false;
            var tween = CreateTween();
            tween.TweenProperty(this, "modulate:a", 0f, FadeSeconds);
            tween.TweenCallback(Callable.From(QueueFree));
        }

        private void SetTiles(IEnumerable<(int X, int Y)> tiles)
        {
            _tiles = tiles.ToList();
            _edges = ZoneEdges.Compute(_tiles);
            QueueRedraw();
        }

        public override void _Process(double delta)
        {
            if (_preview || _style is null) return;
            if (_style.FloorPulse <= 0f && _style.FloorPattern == ZoneFloorPattern.None) return;
            _time += (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_style is not { } style) return;

            float pulse = _preview || style.FloorPulse <= 0f
                ? 1f
                : 1f + style.FloorPulse * Mathf.Sin(Mathf.Tau * _time / style.PulseSeconds);
            const float ts = MapCoords.TileSize;

            foreach (var e in _edges)
            {
                var origin = new Vector2(e.X * ts, e.Y * ts);
                DrawRect(new Rect2(origin, new Vector2(ts, ts)), Tint(style.Floor, pulse));
                if (!_preview) DrawPattern(style, e, origin, pulse);
            }

            foreach (var e in _edges)
            {
                var origin = new Vector2(e.X * ts, e.Y * ts);
                DrawEdges(style, e, origin);
                DrawCorners(style, e, origin);
            }
        }

        private void DrawPattern(ZoneStyle style, ZoneTileEdges e, Vector2 origin, float pulse)
        {
            const float ts = MapCoords.TileSize;
            if (style.FloorPattern == ZoneFloorPattern.Cracks)
            {
                var color = Tint(style.Edge with { A = 0.45f }, pulse);
                for (int c = 0; c < 2; c++)
                {
                    var a = origin + new Vector2(N(e, c, 0), N(e, c, 1)) * ts;
                    var b = a + new Vector2(N(e, c, 2) - 0.5f, N(e, c, 3) - 0.5f) * ts * 0.5f;
                    var d = b + new Vector2(N(e, c, 4) - 0.5f, N(e, c, 5) - 0.5f) * ts * 0.4f;
                    DrawLine(a, b, color, 1f);
                    DrawLine(b, d, color, 1f);
                }
            }
            else if (style.FloorPattern == ZoneFloorPattern.Haze)
            {
                for (int c = 0; c < 3; c++)
                {
                    var drift = new Vector2(Mathf.Sin(_time * 0.5f + N(e, c, 0) * Mathf.Tau), Mathf.Cos(_time * 0.4f + N(e, c, 1) * Mathf.Tau)) * 3f;
                    var centre = origin + new Vector2(N(e, c, 2), N(e, c, 3)) * ts + drift;
                    DrawCircle(centre, 10f + 6f * N(e, c, 4), Tint(style.Floor with { A = style.Floor.A * 0.6f }, pulse));
                }
            }
        }

        private void DrawEdges(ZoneStyle style, ZoneTileEdges e, Vector2 origin)
        {
            const float ts = MapCoords.TileSize;
            float width = style.EdgeKind == ZoneEdgeKind.Soft ? 1f : style.EdgeWidth;
            float inset = 1f + width / 2f;
            var color = Tint(style.EdgeKind == ZoneEdgeKind.Soft ? style.Edge with { A = style.Edge.A * 0.5f } : style.Edge, 1f);

            var tl = origin + new Vector2(inset, inset);
            var tr = origin + new Vector2(ts - inset, inset);
            var br = origin + new Vector2(ts - inset, ts - inset);
            var bl = origin + new Vector2(inset, ts - inset);

            if (e.Open.HasFlag(ZoneSides.Up)) Side(style, e, 0, tl, tr, color, width);
            if (e.Open.HasFlag(ZoneSides.Right)) Side(style, e, 1, tr, br, color, width);
            if (e.Open.HasFlag(ZoneSides.Down)) Side(style, e, 2, bl, br, color, width);
            if (e.Open.HasFlag(ZoneSides.Left)) Side(style, e, 3, tl, bl, color, width);
        }

        private void Side(ZoneStyle style, ZoneTileEdges e, int side, Vector2 a, Vector2 b, Color color, float width)
        {
            if (style.EdgeKind != ZoneEdgeKind.Rough)
            {
                DrawLine(a, b, color, width);
                return;
            }

            var normal = (b - a).Orthogonal().Normalized();
            for (int i = 0; i < 4; i++)
            {
                if (N(e, side, 10 + i) < 0.2f) continue;
                var p = a.Lerp(b, i / 4f);
                var q = a.Lerp(b, (i + 0.8f) / 4f);
                var jitter = normal * (N(e, side, 20 + i) - 0.5f) * 2f;
                DrawLine(p + jitter, q + jitter, color, width);
            }
        }

        private void DrawCorners(ZoneStyle style, ZoneTileEdges e, Vector2 origin)
        {
            if (style.CornerLength <= 0f) return;
            const float ts = MapCoords.TileSize;
            float inset = 1f + style.EdgeWidth / 2f;
            float len = style.CornerLength;
            var color = Tint(style.Corner, 1f);
            float width = style.EdgeWidth + 1f;

            foreach (var corner in e.OuterCorners)
            {
                var (p, dx, dy) = Corner(corner, origin, ts, inset);
                DrawLine(p, p + new Vector2(dx * len, 0), color, width);
                DrawLine(p, p + new Vector2(0, dy * len), color, width);
            }

            foreach (var corner in e.InnerCorners)
            {
                var (p, dx, dy) = Corner(corner, origin, ts, -inset);
                DrawLine(p, p - new Vector2(dx * len / 2f, 0), color, width);
                DrawLine(p, p - new Vector2(0, dy * len / 2f), color, width);
            }
        }

        private static (Vector2 Point, float Dx, float Dy) Corner(ZoneCorner corner, Vector2 origin, float ts, float inset) => corner switch
        {
            ZoneCorner.TopLeft => (origin + new Vector2(inset, inset), 1f, 1f),
            ZoneCorner.TopRight => (origin + new Vector2(ts - inset, inset), -1f, 1f),
            ZoneCorner.BottomRight => (origin + new Vector2(ts - inset, ts - inset), -1f, -1f),
            _ => (origin + new Vector2(inset, ts - inset), 1f, -1f),
        };

        private float N(ZoneTileEdges e, int a, int b) => ZoneParticles.Noise(_seed, e.X, e.Y, a * 64 + b);

        private Color Tint(Color c, float pulse)
        {
            var a = Mathf.Clamp(c.A * pulse, 0f, 1f);
            if (_usable) return c with { A = a };
            float grey = (c.R + c.G + c.B) / 3f;
            return new Color(grey, grey, grey, a * 0.5f);
        }

        private CpuParticles2D CreateParticles(ZoneStyle style)
        {
            var ramp = new Gradient();
            ramp.SetColor(0, style.ParticleColor);
            ramp.SetColor(1, style.ParticleColor with { A = 0f });

            bool fog = style.Particle == ZoneParticle.Fog;
            float speed = fog ? 4f : style.ParticleRise / style.ParticleLifetime;

            return new CpuParticles2D
            {
                Name = "Particles",
                Amount = ZoneParticles.Amount(style, _tiles.Count),
                Lifetime = style.ParticleLifetime,
                Preprocess = style.ParticleLifetime,
                Texture = ZoneParticles.Texture(style.Particle),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Points,
                EmissionPoints = ZoneParticles.Points(_tiles, _seed),
                Direction = fog ? Vector2.Right : Vector2.Up,
                Spread = style.Particle switch { ZoneParticle.Fog => 180f, ZoneParticle.Dust => 60f, ZoneParticle.Ember => 20f, _ => 10f },
                Gravity = Vector2.Zero,
                InitialVelocityMin = speed * 0.7f,
                InitialVelocityMax = speed * 1.3f,
                ColorRamp = ramp,
                Emitting = true,
            };
        }
    }
}
```

**Step 2: Build**

Run: `dotnet build Goose2ClientGodot.csproj`
Expected: build succeeds with no new warnings in `ZoneView.cs`.

**Step 3: Commit**

```bash
git add Scripts/Map/ZoneView.cs
git commit -m "feat(zones): draw styled zones with edges, corners and particles"
```

---

### Task 6: Wire `ZoneView` into `GroundZoneLayer`, with fades

**Files:**
- Modify: `Scripts/Map/GroundZoneLayer.cs` (`Add`, `Remove`, `_Process`, `FreeSprites`)

**Step 1: Implement**

Replace `_sprites` with views:

```csharp
        private readonly Dictionary<int, ZoneView> _views = new();
```

```csharp
        public void Add(ZoneAddPacket p)
        {
            if (!_book.Add(p.Id, Time.GetTicksMsec(), p.RemainingMs))
                FreeView(p.Id, fade: false);

            var view = new ZoneView { Name = $"Zone{p.Id}" };
            AddChild(view);
            view.Setup(p.Id, p.Tiles, ZoneStyles.Resolve(p.Style, p.AnimationId));
            view.FadeIn();
            _views[p.Id] = view;
        }

        public void Remove(int id)
        {
            _book.Remove(id);
            FreeView(id, fade: true);
        }

        public override void _Process(double delta)
        {
            foreach (var id in _book.TakeExpired(Time.GetTicksMsec()))
                FreeView(id, fade: true);
        }

        private void FreeView(int id, bool fade)
        {
            if (!_views.Remove(id, out var view) || !GodotObject.IsInstanceValid(view)) return;
            if (fade) view.FadeOutAndFree();
            else view.QueueFree();
        }
```

Delete `FreeSprites`. Leave the preview members alone until Task 7.

**Step 2: Build and run the client tests**

```bash
dotnet build Goose2ClientGodot.csproj
dotnet test tests/Goose2Client.Tests
```

Expected: build succeeds; tests pass apart from the known `StylistCatalogTests` failure.

**Step 3: Commit**

```bash
git add Scripts/Map/GroundZoneLayer.cs
git commit -m "feat(zones): ground zone layer uses styled views with fades"
```

---

### Task 7: Styled aiming preview

**Files:**
- Modify: `Scripts/Map/GroundZoneLayer.cs` (preview members)
- Modify: `Scripts/SpellTargetManager.cs:157-175` (`UpdatePreview`)

**Step 1: Implement**

`GroundZoneLayer.cs` — keep `PreviewNode` for unstyled spells, add a styled preview view, and draw both
above live zones:

```csharp
        private readonly PreviewNode _preview = new() { Name = "Preview", ZIndex = 1 };
        private readonly ZoneView _styledPreview = new() { Name = "StyledPreview", ZIndex = 1, Visible = false };

        public override void _Ready()
        {
            AddChild(_preview);
            AddChild(_styledPreview);
        }

        public void ShowPreview(List<(int X, int Y)> tiles, Color color, ZoneStyle? style, bool usable)
        {
            if (style is null)
            {
                _styledPreview.Visible = false;
                _preview.Set(tiles, color);
                return;
            }

            _preview.Set(new(), default);
            _styledPreview.SetPreview(tiles, style, usable);
            _styledPreview.Visible = true;
        }

        public void HidePreview()
        {
            _preview.Set(new(), default);
            _styledPreview.Visible = false;
        }
```

`SpellTargetManager.cs`, at the end of `UpdatePreview`, replace
`mm.GroundZones.ShowPreview(tiles, color);` with:

```csharp
        mm.GroundZones.ShowPreview(tiles, color, Goose2Client.Map.ZoneStyles.Get(spell.ZoneStyle), usable);
```

**Step 2: Build and run the client tests**

```bash
dotnet build Goose2ClientGodot.csproj
dotnet test tests/Goose2Client.Tests
```

Expected: build succeeds; tests pass apart from the known `StylistCatalogTests` failure.

**Step 3: Commit**

```bash
git add Scripts/Map/GroundZoneLayer.cs Scripts/SpellTargetManager.cs
git commit -m "feat(zones): preview styled spells with their zone style"
```

---

### Task 8: Manual checks in game

Needs Part 1 merged and running locally, with the sheet updated (Part 1 Task 4) and `/updatesql` run.
Give a test character the four scrolls (ids 921–924 at planning time) or cast as a GM.

For each of Sanctuary, Earthquake, Empowering Ground and Poison Cloud:

1. The floor, edges, corners and particles match the design's §4 table; heal's `+` is green.
2. A 7×7 zone has clean corners; cast next to the map edge and the clipped side is outlined.
3. The zone fades in on cast and fades out when it ends.
4. At night the zone darkens with the map but is still readable.
5. Aiming Sanctuary, Empowering Ground and Poison Cloud shows the styled preview, still and without
   particles, matching where the zone lands, and greyed out of range. Earthquake (self-cast) has none.
6. Leave and rejoin the map mid-zone: the zone reappears with the remaining time.
7. Cast several zones at once and watch the frame rate.
8. Cast an old unstyled area spell to confirm its preview is still the plain square.

Note anything that needs tuning; colours and numbers live in `Scripts/Map/ZoneStyles.cs`.
