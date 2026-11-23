# Ground AoE Zones — Part 3: Client Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Goose2ClientGodot draws active ground zones as looping tile animations, lets the player cast
a ground-targetable spell by clicking an empty tile, and shows a translucent preview of the area a spell
will cover while targeting.

**Architecture:** Pure logic lives in small engine-free classes that the xUnit project can test:
`SpellShapes.Tiles` (a port of the server's `SpellEffect.ShapeTiles`, checked against the server's own
fixture), `ZoneBook` (zone expiry bookkeeping) and `GroundTargetRule` (range check). A `GroundZoneLayer`
Node2D on the map (z 12: above ground tiles, below items and characters) owns the zone sprites and the
preview. `MapManager` routes the new packets and empty-tile clicks/hover to it and to
`SpellTargetManager`.

**Tech Stack:** Godot 4.6 C# (GodotSharp), xUnit (`tests/Goose2Client.Tests` compiles `Scripts/**/*.cs`;
pure logic only — no engine calls in tests).

**Design doc:** `docs/plans/2026-11-21-ground-aoe-design.md` (Part 3 of 3), in the *server* repo.
**Requires Part 2** on the server for anything to show up in-game; the client changes are safe against an
older server (no `ZADD`, no new `SSS` fields → nothing changes).

**Client worktree (create in Task 0):** `/home/hayden/code/Goose2ClientGodot/.worktrees/ground-aoe`,
branch `ground-aoe`. All paths below are relative to it.

**House rule (client `AGENTS.md`):** no new comments or doc strings unless the "why" is non-obvious
(wire-format details are allowed).

Test commands (from the client worktree):

```bash
dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~<Name>
dotnet test tests/Goose2Client.Tests
```

Godot build check (also generates `.cs.uid` files for new scripts — commit them):

```bash
/usr/bin/godot-mono --headless --path . --build-solutions --quit
```

---

## APIs verified (client repo paths)

| API | Where | Notes |
|---|---|---|
| `SpellbookSlotPacket.Parse` (reads `CooldownRemainingMs` only if `LengthRemaining() > 0`) | `Scripts/Network/Packets/SpellbookSlotPacket.cs:31-50` | tests `tests/Goose2Client.Tests/SpellbookSlotPacketTests.cs` |
| `SpellInfo.FromPacket` | `Scripts/SpellInfo.cs:16-27` | used by `SpellbookWindow.cs:111` and `HotbarWindow.cs:268` |
| `PacketParser` (`GetInt32`, `GetBool`, `TokensRemaining` — 0 at end, trailing `,` not a token) | `Scripts/Network/PacketParser.cs:63-120` | |
| `PacketManager.Handle` drops unknown prefixes silently | `Scripts/Network/PacketManager.cs:46-71` | |
| `SpellTilePacket` converts server 1-based tiles to 0-based (`GetInt32() - 1`) and ignores the file | `Scripts/Network/Packets/SpellTilePacket.cs:16-24` | zones must do the same |
| `MapManager._Ready` listener registration / `_ExitTree` removal | `Scripts/MapManager.cs:104-127`, `:138-167` | |
| `MapManager.OnSpellTile` (how one-shot tile animations are placed) | `Scripts/MapManager.cs:442-449` | `SpellAnimation.Setup(id, new Vector2(0, -TileSize/2))` at `TileBottomCenter` |
| `MapManager.HandleWorldClick` targeting branch (only character hits are handled) | `Scripts/MapManager.cs:318-339` | |
| `MapManager.HandleWorldHover` / `ClearWorldHover` | `Scripts/MapManager.cs:369-373`; called from `Scripts/WorldViewport.cs:283,289` | |
| `MapManager.LocalPlayer`, `GetCharacter`, `Characters` | `Scripts/MapManager.cs:52-54` | |
| Map z-order: ground `MapLayer` z = layer×10 (0,10,…); items `Objects` z 14; `Characters`/`Objects1`/`Effects` z 15 | `Scripts/Map/MapLayer.cs:25`, `Scenes/Map.tscn:15-28`, `Scripts/MapManager.cs:99` | zones go at z 12 |
| `SpellAnimation.Setup(animationId, spriteOffset)` sets a one-shot `OverlayLifetime` | `Scripts/Overlays/SpellAnimation.cs:9-41` | |
| `WorldOverlay._Process` does nothing when `Lifetime == null` | `Scripts/Overlays/WorldOverlay.cs:8-15` | a looping overlay just leaves it null |
| `MapCoords.TileSize`, `TileCenter`, `TileBottomCenter`, `WorldToTile` | `Scripts/Map/MapCoords.cs:10-24` | |
| `SpellTargetManager` (`_target`, `_pendingSpell`, `IsTargeting`, `Cast`, `CastOnClickedTarget`, `CastOnTarget`, `ExitTargeting`, `GetViewRange`, `_Process`) | `Scripts/SpellTargetManager.cs:10-55`, `:100-137`, `:189-198`, `:355-388` | |
| `SpellbookWindow.UseSpell` — `SpellTargetType.None` casts immediately, others enter targeting | `Scripts/UI/SpellbookWindow.cs:114-140` | `Self` spells never show a preview |
| `NetworkClient.CastSpell(slot, targetId)` sends `CAST{slot+1},{id}` | `Scripts/Network/NetworkClient.cs:271-275` | |
| `Character.X`, `Y` (0-based), `Facing` | `Scripts/Character/Character.cs:14-16` | |
| Client `Direction` is `Up, Right, Down, Left` (0-3) | `Scripts/Direction.cs` | names match the server fixture's `facing` strings |
| `GameManager.Instance.SpellCooldownManager.Cast(slot, cooldown)` | used at `Scripts/SpellTargetManager.cs:365` | |
| `TargetBoxColor.CurrentColor()` | used at `Scripts/SpellTarget.cs:25` | preview tint |
| Test project copies `Fixtures/**` to output | `tests/Goose2Client.Tests/Goose2Client.Tests.csproj` | the folder doesn't exist yet |
| Server fixture to copy | server repo `Goose.Tests/Spells/shape-tiles.json` (Part 1 Task 2) | |

### Wire formats (from Part 2)

- `SSS…,cooldownMs,remainingMs,groundTargetable,shape,size` — 3 trailing ints; empty slots don't have them.
  `shape` is the server `TargetTypes` value: Target 0, LineFront 1, Cross 2, Plus 3, Random 4, Area 5,
  TriangleFront 6.
- `ZADD id,anim,file,remainingMs,x1,y1,x2,y2,…` — tiles are **1-based**, rows in Y-then-X order.
- `ZDEL id`
- Client → server: `CAST{slot+1},0,{x+1},{y+1}`.

### Decisions taken while planning

1. **Previews show for every targeted spell with an area shape**, not just ground zones, because the
   `SSS` fields come with every spell. An existing area nuke gets an accurate preview on its target.
   `Target` and `Random` shapes never preview.
2. **The animation file field is ignored**, same as `SPA` today (`SpellTilePacket.cs` reads only the id).
3. **Zones are cleared on map change for free:** `GroundZoneLayer` is a child of the map scene, which is
   freed on map change, and the server re-sends `ZADD` on entry.

---

### Task 0: Client worktree and baseline

```bash
cd /home/hayden/code/Goose2ClientGodot
git check-ignore -q .worktrees/x && echo ignored
git worktree add .worktrees/ground-aoe -b ground-aoe
cd .worktrees/ground-aoe
dotnet test tests/Goose2Client.Tests
```

Expected: `ignored` printed (if not, add `.worktrees/` to `.gitignore` and commit first). Record the
baseline. The revive plan saw exactly one pre-existing failure in a fresh worktree,
`StylistCatalogTests.EveryCatalogId_HasAnimationAsset`, because `Assets/*` is gitignored; treat that one as
pre-existing and nothing else.

Generated sprites: `SpellAnimation.Setup` loads `res://Assets/Sprites/Effects/<id>/animations.tres`. For
manual testing in Task 7, run from the main checkout or copy/symlink `Assets/Sprites` into the worktree
the way earlier client work did.

---

### Task 1: Parse the new `SSS` fields

**Files:**
- Create: `Scripts/SpellShape.cs`
- Modify: `Scripts/Network/Packets/SpellbookSlotPacket.cs`
- Modify: `Scripts/SpellInfo.cs`
- Test: `tests/Goose2Client.Tests/SpellbookSlotPacketTests.cs`, `tests/Goose2Client.Tests/SpellInfoTests.cs`

**Step 1: Write the failing tests**

Append to `SpellbookSlotPacketTests`:

```csharp
        [Fact]
        public void ParsesGroundTargetingShapeAndSize()
        {
            var p = (SpellbookSlotPacket)new SpellbookSlotPacket().Parse(new PacketParser("SSS1,Fire Patch,5,1000,0,1,100,3,20000,0,1,5,2", "SSS"));
            Assert.True(p.GroundTargetable);
            Assert.Equal(SpellShape.Area, p.Shape);
            Assert.Equal(2, p.ShapeSize);
        }

        [Fact]
        public void OlderServerWithoutShapeFields_DefaultsToTarget()
        {
            var p = (SpellbookSlotPacket)new SpellbookSlotPacket().Parse(new PacketParser("SSS1,Fireball,5,1000,0,1,100,3,20000,4500", "SSS"));
            Assert.False(p.GroundTargetable);
            Assert.Equal(SpellShape.Target, p.Shape);
            Assert.Equal(0, p.ShapeSize);
        }
```

Append to `SpellInfoTests`:

```csharp
    [Fact]
    public void FromPacket_CopiesGroundTargetingAndShape()
    {
        var packet = new SpellbookSlotPacket
        {
            SlotNumber = 0, Name = "Fire Patch", TargetType = SpellTargetType.NPC,
            GroundTargetable = true, Shape = SpellShape.Plus, ShapeSize = 3,
        };

        var info = SpellInfo.FromPacket(packet);

        Assert.True(info.GroundTargetable);
        Assert.Equal(SpellShape.Plus, info.Shape);
        Assert.Equal(3, info.ShapeSize);
    }
```

**Step 2: Run to verify they fail**

Run: `dotnet test tests/Goose2Client.Tests --filter "FullyQualifiedName~SpellbookSlotPacketTests|FullyQualifiedName~SpellInfoTests"`
Expected: build error — `SpellShape`, `GroundTargetable`, `Shape`, `ShapeSize` don't exist.

**Step 3: Implement**

`Scripts/SpellShape.cs`:

```csharp
namespace Goose2Client
{
    // Values mirror the server's SpellEffect.TargetTypes, as sent in SSS.
    public enum SpellShape
    {
        Target = 0,
        LineFront,
        Cross,
        Plus,
        Random,
        Area,
        TriangleFront,
    }
}
```

`SpellbookSlotPacket` — add properties:

```csharp
        public bool GroundTargetable { get; set; }

        public SpellShape Shape { get; set; }

        public int ShapeSize { get; set; }
```

and in `Parse`, replace

```csharp
                if (p.LengthRemaining() > 0)
                    packet.CooldownRemainingMs = p.GetInt64();
```

with

```csharp
                if (p.LengthRemaining() > 0)
                    packet.CooldownRemainingMs = p.GetInt64();
                if (p.TokensRemaining() >= 3)
                {
                    packet.GroundTargetable = p.GetBool();
                    packet.Shape = (SpellShape)p.GetInt32();
                    packet.ShapeSize = p.GetInt32();
                }
```

`SpellInfo` — add `GroundTargetable`, `Shape`, `ShapeSize` properties and copy them in `FromPacket`.

**Step 4: Run the tests**

Run: `dotnet test tests/Goose2Client.Tests --filter "FullyQualifiedName~SpellbookSlotPacketTests|FullyQualifiedName~SpellInfoTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add Scripts/SpellShape.cs Scripts/Network/Packets/SpellbookSlotPacket.cs Scripts/SpellInfo.cs \
  tests/Goose2Client.Tests/SpellbookSlotPacketTests.cs tests/Goose2Client.Tests/SpellInfoTests.cs
git commit -m "feat(spells): parse ground targeting and shape from SSS"
```

---

### Task 2: Port the shape tiles, checked against the server fixture

**Files:**
- Create: `Scripts/SpellShapes.cs`
- Create: `tests/Goose2Client.Tests/Fixtures/shape-tiles.json` (copied verbatim from the server)
- Create: `tests/Goose2Client.Tests/SpellShapesTests.cs`

**Step 1: Copy the fixture**

```bash
mkdir -p tests/Goose2Client.Tests/Fixtures
cp /home/hayden/code/illutiagooseserver/.worktrees/ground-aoe/Goose.Tests/Spells/shape-tiles.json \
   tests/Goose2Client.Tests/Fixtures/shape-tiles.json
```

(Use the server's merged `master` copy instead if the server worktree is gone.) Do not edit it here: the
server owns it. If a shape ever changes, the server test changes first and the file is copied again.

**Step 2: Write the failing test**

`tests/Goose2Client.Tests/SpellShapesTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Goose2Client;
using Xunit;

namespace Goose2Client.Tests;

public class SpellShapesTests
{
    private sealed record ShapeCase(string Name, string Shape, int Size, string Facing, bool Legacy, int[][] Tiles);

    private static List<ShapeCase> Cases() => JsonSerializer.Deserialize<List<ShapeCase>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "shape-tiles.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    [Fact]
    public void Matches_every_server_case()
    {
        foreach (var c in Cases())
        {
            var expected = c.Tiles.Select(t => (t[0], t[1])).ToList();
            var actual = SpellShapes.Tiles(Enum.Parse<SpellShape>(c.Shape), c.Size, 10, 10, Enum.Parse<Direction>(c.Facing));
            Assert.True(expected.SequenceEqual(actual), $"{c.Name}: got {string.Join(" ", actual)}");
        }
    }

    [Fact]
    public void Random_has_no_predictable_tiles()
    {
        Assert.Empty(SpellShapes.Tiles(SpellShape.Random, 2, 10, 10, Direction.Down));
    }

    [Theory]
    [InlineData(SpellShape.Target, false)]
    [InlineData(SpellShape.Random, false)]
    [InlineData(SpellShape.Area, true)]
    [InlineData(SpellShape.LineFront, true)]
    public void Only_predictable_area_shapes_preview(SpellShape shape, bool previews)
    {
        Assert.Equal(previews, SpellShapes.HasPreview(shape));
    }
}
```

**Step 3: Run to verify it fails**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~SpellShapesTests`
Expected: build error — `SpellShapes` doesn't exist.

**Step 4: Implement**

`Scripts/SpellShapes.cs` — a line-for-line port of the server's `SpellEffect.ShapeTiles` (Part 1 Task 3),
with the client's `Direction` enum:

```csharp
using System.Collections.Generic;

namespace Goose2Client
{
    // Port of the server's SpellEffect.ShapeTiles; tests/Goose2Client.Tests/Fixtures/shape-tiles.json is the shared contract.
    public static class SpellShapes
    {
        public static bool HasPreview(SpellShape shape) => shape is not (SpellShape.Target or SpellShape.Random);

        public static List<(int X, int Y)> Tiles(SpellShape shape, int size, int ox, int oy, Direction facing)
        {
            var tiles = new List<(int X, int Y)>();

            switch (shape)
            {
                case SpellShape.Target:
                    tiles.Add((ox, oy));
                    break;

                case SpellShape.LineFront:
                {
                    int x = ox;
                    int y = oy;
                    for (int i = 1; i <= size; i++)
                    {
                        (x, y) = facing switch
                        {
                            Direction.Up => (x, y - 1),
                            Direction.Right => (x + 1, y),
                            Direction.Down => (x, y + 1),
                            Direction.Left => (x - 1, y),
                            _ => (x, y),
                        };
                        tiles.Add((x, y));
                    }
                    break;
                }

                case SpellShape.Area:
                    for (int y = oy - size; y <= oy + size; y++)
                        for (int x = ox - size; x <= ox + size; x++)
                            tiles.Add((x, y));
                    break;

                case SpellShape.Cross:
                    for (int y = oy - size; y <= oy + size; y++)
                        for (int x = ox - size; x <= ox + size; x++)
                            if (x == ox - (y - oy) || x == ox + (y - oy))
                                tiles.Add((x, y));
                    break;

                case SpellShape.Plus:
                    for (int y = oy - size; y <= oy + size; y++)
                        for (int x = ox - size; x <= ox + size; x++)
                            if (x == ox || y == oy)
                                tiles.Add((x, y));
                    break;

                case SpellShape.TriangleFront:
                {
                    int x = ox;
                    int y = oy;
                    for (int i = 1; i <= size; i++)
                    {
                        (x, y) = facing switch
                        {
                            Direction.Up => (ox - i, y - 1),
                            Direction.Right => (x + 1, oy - i),
                            Direction.Down => (ox - i, y + 1),
                            Direction.Left => (x - 1, oy - i),
                            _ => (x, y),
                        };

                        for (int j = 0; j < i * 2 - 1; j++)
                        {
                            (x, y) = facing switch
                            {
                                Direction.Up => (x + 1, y),
                                Direction.Right => (x, y + 1),
                                Direction.Down => (x + 1, y),
                                Direction.Left => (x, y + 1),
                                _ => (x, y),
                            };
                            tiles.Add((x, y));
                        }
                    }
                    break;
                }
            }

            return tiles;
        }
    }
}
```

The client works in 0-based tiles and the fixture in the server's 1-based ones; shapes are pure offsets
from the anchor, so the same numbers hold in both.

**Step 5: Run the tests**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~SpellShapesTests`
Expected: PASS.

**Step 6: Commit**

```bash
git add Scripts/SpellShapes.cs tests/Goose2Client.Tests/Fixtures/shape-tiles.json tests/Goose2Client.Tests/SpellShapesTests.cs
git commit -m "feat(spells): port area spell shapes for previews"
```

---

### Task 3: `ZADD` / `ZDEL` packets and zone bookkeeping

**Files:**
- Create: `Scripts/Network/Packets/ZoneAddPacket.cs`, `Scripts/Network/Packets/ZoneDeletePacket.cs`
- Create: `Scripts/Map/ZoneBook.cs`
- Create: `tests/Goose2Client.Tests/ZonePacketTests.cs`, `tests/Goose2Client.Tests/ZoneBookTests.cs`

**Step 1: Write the failing tests**

`tests/Goose2Client.Tests/ZonePacketTests.cs`:

```csharp
using Goose2Client.Network;
using Goose2Client.Network.Packets;
using Xunit;

namespace Goose2Client.Network.Packets.Tests;

public class ZonePacketTests
{
    [Fact]
    public void ZoneAdd_ParsesHeaderAndConvertsTilesToZeroBased()
    {
        var p = (ZoneAddPacket)new ZoneAddPacket().Parse(new PacketParser("ZADD7,120,4,8000,10,10,11,10,10,11", "ZADD"));

        Assert.Equal(7, p.Id);
        Assert.Equal(120, p.AnimationId);
        Assert.Equal(8000, p.RemainingMs);
        Assert.Equal(new[] { (9, 9), (10, 9), (9, 10) }, p.Tiles);
    }

    [Fact]
    public void ZoneAdd_WithNoTiles_ParsesEmpty()
    {
        var p = (ZoneAddPacket)new ZoneAddPacket().Parse(new PacketParser("ZADD7,120,4,8000", "ZADD"));

        Assert.Empty(p.Tiles);
    }

    [Fact]
    public void ZoneDelete_ParsesId()
    {
        var p = (ZoneDeletePacket)new ZoneDeletePacket().Parse(new PacketParser("ZDEL7", "ZDEL"));

        Assert.Equal(7, p.Id);
    }
}
```

`tests/Goose2Client.Tests/ZoneBookTests.cs`:

```csharp
using Goose2Client.Map;
using Xunit;

namespace Goose2Client.Tests;

public class ZoneBookTests
{
    [Fact]
    public void A_zone_expires_once_its_time_is_up()
    {
        var book = new ZoneBook();
        Assert.True(book.Add(7, nowMs: 1000, remainingMs: 8000));

        Assert.Empty(book.TakeExpired(8999));
        Assert.Equal(new[] { 7 }, book.TakeExpired(9000));
        Assert.Empty(book.TakeExpired(20000));
    }

    [Fact]
    public void Re_adding_a_known_id_reports_a_replacement()
    {
        var book = new ZoneBook();
        book.Add(7, 0, 1000);

        Assert.False(book.Add(7, 0, 5000));
        Assert.Empty(book.TakeExpired(1000));
    }

    [Fact]
    public void Remove_reports_whether_the_zone_was_known()
    {
        var book = new ZoneBook();
        book.Add(7, 0, 1000);

        Assert.True(book.Remove(7));
        Assert.False(book.Remove(7));
        Assert.Empty(book.TakeExpired(5000));
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test tests/Goose2Client.Tests --filter "FullyQualifiedName~ZonePacketTests|FullyQualifiedName~ZoneBookTests"`
Expected: build error.

**Step 3: Implement**

`Scripts/Network/Packets/ZoneAddPacket.cs`:

```csharp
using System.Collections.Generic;

namespace Goose2Client.Network.Packets
{
    public class ZoneAddPacket : PacketHandler
    {
        public int Id { get; set; }

        public int AnimationId { get; set; }

        public long RemainingMs { get; set; }

        public List<(int X, int Y)> Tiles { get; set; } = new();

        public override string Prefix { get; } = "ZADD";

        // ZADD id,anim,file,remainingMs,x1,y1,… — tiles are 1-based on the wire; the file is unused, as with SPA.
        public override object Parse(PacketParser p)
        {
            var packet = new ZoneAddPacket
            {
                Id = p.GetInt32(),
                AnimationId = p.GetInt32(),
            };
            p.GetInt32();
            packet.RemainingMs = p.GetInt64();

            while (p.TokensRemaining() >= 2)
                packet.Tiles.Add((p.GetInt32() - 1, p.GetInt32() - 1));

            return packet;
        }
    }
}
```

`Scripts/Network/Packets/ZoneDeletePacket.cs`:

```csharp
namespace Goose2Client.Network.Packets
{
    public class ZoneDeletePacket : PacketHandler
    {
        public int Id { get; set; }

        public override string Prefix { get; } = "ZDEL";

        public override object Parse(PacketParser p) => new ZoneDeletePacket { Id = p.GetInt32() };
    }
}
```

`Scripts/Map/ZoneBook.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace Goose2Client.Map
{
    public sealed class ZoneBook
    {
        private readonly Dictionary<int, ulong> _expiresAt = new();

        public bool Add(int id, ulong nowMs, long remainingMs)
        {
            bool isNew = !_expiresAt.ContainsKey(id);
            _expiresAt[id] = nowMs + (ulong)System.Math.Max(0, remainingMs);
            return isNew;
        }

        public bool Remove(int id) => _expiresAt.Remove(id);

        public List<int> TakeExpired(ulong nowMs)
        {
            var expired = _expiresAt.Where(kv => kv.Value <= nowMs).Select(kv => kv.Key).ToList();
            foreach (var id in expired) _expiresAt.Remove(id);
            return expired;
        }
    }
}
```

Check that `PacketHandler` is `public abstract class` with `Prefix`/`Parse` as used above
(`Scripts/Network/PacketHandler.cs:8`); `SpellTilePacket` is declared `class` (internal) — either is fine.

**Step 4: Run the tests**

Run: `dotnet test tests/Goose2Client.Tests --filter "FullyQualifiedName~ZonePacketTests|FullyQualifiedName~ZoneBookTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add Scripts/Network/Packets/ZoneAddPacket.cs Scripts/Network/Packets/ZoneDeletePacket.cs Scripts/Map/ZoneBook.cs \
  tests/Goose2Client.Tests/ZonePacketTests.cs tests/Goose2Client.Tests/ZoneBookTests.cs
git commit -m "feat(spells): ZADD/ZDEL packets and zone expiry bookkeeping"
```

---

### Task 4: Draw zones on the map

**Files:**
- Modify: `Scripts/Overlays/SpellAnimation.cs` (`loop` parameter)
- Create: `Scripts/Map/GroundZoneLayer.cs`
- Modify: `Scripts/MapManager.cs` (create the layer; listen/remove `ZoneAddPacket`, `ZoneDeletePacket`)

No unit tests — this is engine code; the logic it relies on is covered by Task 3. Verified manually in
Task 7.

**Step 1: Let `SpellAnimation` loop**

Change the signature to `public bool Setup(int animationId, Vector2 spriteOffset = default, bool loop = false)`
and replace `Lifetime = new OverlayLifetime(lengthSeconds);` with:

```csharp
            if (loop)
                sprite.AnimationFinished += () => sprite.Play(clip);
            else
                Lifetime = new OverlayLifetime(lengthSeconds);
```

`AnimationFinished` fires only for non-looping clips, so a clip that already loops in its `.tres` keeps
looping on its own and the handler never runs; either way the shared `SpriteFrames` is not modified.
`WorldOverlay._Process` skips nodes with no `Lifetime`, so a looping overlay lives until freed.

**Step 2: The layer**

`Scripts/Map/GroundZoneLayer.cs`:

```csharp
using System.Collections.Generic;
using Godot;
using Goose2Client.Network.Packets;
using Goose2Client.Overlays;

namespace Goose2Client.Map
{
    public partial class GroundZoneLayer : Node2D
    {
        private readonly ZoneBook _book = new();
        private readonly Dictionary<int, List<Node2D>> _sprites = new();

        public void Add(ZoneAddPacket p)
        {
            if (!_book.Add(p.Id, Time.GetTicksMsec(), p.RemainingMs))
                FreeSprites(p.Id);

            var sprites = new List<Node2D>();
            foreach (var (x, y) in p.Tiles)
            {
                var s = new SpellAnimation { Name = $"Zone{p.Id}({x},{y})" };
                AddChild(s);
                if (!s.Setup(p.AnimationId, new Vector2(0, -MapCoords.TileSize / 2f), loop: true)) { s.QueueFree(); continue; }
                s.Position = MapCoords.TileBottomCenter(x, y);
                sprites.Add(s);
            }
            _sprites[p.Id] = sprites;
        }

        public void Remove(int id)
        {
            _book.Remove(id);
            FreeSprites(id);
        }

        public override void _Process(double delta)
        {
            foreach (var id in _book.TakeExpired(Time.GetTicksMsec()))
                FreeSprites(id);
        }

        private void FreeSprites(int id)
        {
            if (!_sprites.Remove(id, out var sprites)) return;
            foreach (var s in sprites)
                if (GodotObject.IsInstanceValid(s)) s.QueueFree();
        }
    }
}
```

**Step 3: Wire it into `MapManager`**

Add a field and property:

```csharp
    public Goose2Client.Map.GroundZoneLayer GroundZones { get; private set; } = null!;
```

In `_Ready`, after the `_objectLayer` block (`:99-102`):

```csharp
        GroundZones = new Goose2Client.Map.GroundZoneLayer { Name = "GroundZones", ZIndex = 12 };
        AddChild(GroundZones);
```

z 12 puts it above ground layers 0 and 1 (z 0 and 10) and below dropped items (14) and characters (15).
The node has `YSortEnabled` off, so its children draw in insertion order, all at z 12.

Register with the other listeners (`:104-127`):

```csharp
        pm.Listen<ZoneAddPacket>(OnZoneAdd);
        pm.Listen<ZoneDeletePacket>(OnZoneDelete);
```

remove them in `_ExitTree` (`:144-166`):

```csharp
        pm.Remove<ZoneAddPacket>(OnZoneAdd);
        pm.Remove<ZoneDeletePacket>(OnZoneDelete);
```

and add the handlers next to `OnSpellTile`:

```csharp
    private void OnZoneAdd(object packetObj) => GroundZones.Add((ZoneAddPacket)packetObj);

    private void OnZoneDelete(object packetObj) => GroundZones.Remove(((ZoneDeletePacket)packetObj).Id);
```

**Step 4: Build**

```bash
/usr/bin/godot-mono --headless --path . --build-solutions --quit
dotnet test tests/Goose2Client.Tests
```

Expected: builds; tests at baseline.

**Step 5: Commit**

```bash
git add Scripts/Overlays/SpellAnimation.cs Scripts/Map/GroundZoneLayer.cs Scripts/MapManager.cs Scripts/**/*.uid
git commit -m "feat(spells): draw ground zones as looping tile animations"
```

---

### Task 5: Cast a ground-targetable spell on a clicked tile

**Files:**
- Create: `Scripts/GroundTargetRule.cs`
- Modify: `Scripts/Network/NetworkClient.cs` (after `CastSpell`, `:271-275`)
- Modify: `Scripts/SpellTargetManager.cs`
- Modify: `Scripts/MapManager.cs:318-339` (targeting branch of `HandleWorldClick`)
- Create: `tests/Goose2Client.Tests/GroundTargetRuleTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose2Client;
using Xunit;

namespace Goose2Client.Tests;

public class GroundTargetRuleTests
{
    private static SpellInfo Spell(bool ground) => new() { Name = "Fire Patch", GroundTargetable = ground };

    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(30, 10, true)]
    [InlineData(31, 10, false)]
    [InlineData(10, 18, true)]
    [InlineData(10, 19, false)]
    [InlineData(-1, 10, false)]
    [InlineData(10, 100, false)]
    public void Accepts_only_tiles_on_the_map_and_in_view(int x, int y, bool ok)
    {
        Assert.Equal(ok, GroundTargetRule.CanCastAt(Spell(true), player: (10, 10), tile: (x, y),
            viewRange: (20, 8), mapSize: (100, 100)));
    }

    [Fact]
    public void Refuses_spells_that_are_not_ground_targetable()
    {
        Assert.False(GroundTargetRule.CanCastAt(Spell(false), (10, 10), (11, 10), (20, 8), (100, 100)));
    }
}
```

The `<=` view check matches `SpellTargetManager.IsUsableTarget` (`:196-197`). The client's view box (about
20×11 tiles) is smaller than the server's (`RANGE_X/Y` 24/16), so anything the client sends is in range for
the server.

**Step 2: Run to verify it fails**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~GroundTargetRuleTests`
Expected: build error.

**Step 3: Implement**

`Scripts/GroundTargetRule.cs`:

```csharp
namespace Goose2Client
{
    public static class GroundTargetRule
    {
        public static bool CanCastAt(SpellInfo spell, (int X, int Y) player, (int X, int Y) tile,
            (int X, int Y) viewRange, (int Width, int Height) mapSize)
            => spell.GroundTargetable &&
               tile.X >= 0 && tile.Y >= 0 && tile.X < mapSize.Width && tile.Y < mapSize.Height &&
               System.Math.Abs(tile.X - player.X) <= viewRange.X &&
               System.Math.Abs(tile.Y - player.Y) <= viewRange.Y;
    }
}
```

`NetworkClient`, after `CastSpell`:

```csharp
        public void CastSpellAt(int slot, int x, int y)
        {
            if (LocalPlayerDead) return;
            Send($"CAST{slot + 1},0,{x + 1},{y + 1}");
        }
```

`SpellTargetManager`, next to `CastOnClickedTarget`:

```csharp
    public void CastOnClickedTile(int x, int y)
    {
        if (!IsTargeting || _pendingSpell is not { } spell) return;
        if (!CanCastAt(spell, x, y)) return;

        _holdGate.Spend();
        _hotkeyConfirmFrame = Engine.GetProcessFrames();
        _pendingSpell = null;
        if (!GameManager.Instance.IsLocalPlayerDead && !GameManager.Instance.IsLoadingMap)
        {
            LastTargetableSpell = spell;
            GameManager.Instance.SpellCooldownManager.Cast(spell.SlotNumber, spell.Cooldown);
            GameManager.Instance.NetworkClient.CastSpellAt(spell.SlotNumber, x, y);
        }
        ExitTargeting();
    }

    private bool CanCastAt(SpellInfo spell, int x, int y)
    {
        var player = GameManager.Instance.CurrentMapManager?.LocalPlayer;
        var map = GameManager.Instance.CurrentMap;
        if (player == null || map == null) return false;
        var view = GetViewRange();
        return GroundTargetRule.CanCastAt(spell, (player.X, player.Y), (x, y), (view.X, view.Y), (map.Width, map.Height));
    }
```

This mirrors `ConfirmTarget` → `CastOnTarget` (`:355-369`): spend the hold gate, null `_pendingSpell`
before sending, then `ExitTargeting`. An out-of-range click leaves targeting up, the same as clicking
nothing today. Check `GameManager.Instance.CurrentMap` exposes `Width`/`Height` (it is used as
`CurrentMap?.Width` at `SpellTargetManager.cs:263`).

`MapManager.HandleWorldClick`, targeting branch — after the `if (hit != null) { … }` block, add:

```csharp
                else
                {
                    var (tx, ty) = MapCoords.WorldToTile(worldPos);
                    _lastLcTile = new Vector2I(tx, ty);
                    _lastLcMsec = Time.GetTicksMsec();
                    GameManager.Instance.SpellTargetManager?.CastOnClickedTile(tx, ty);
                }
```

The `_lastLc*` stamp is for the same reason as the character branch: the cast ends targeting, so the second
click of a double-click would otherwise send an `LC`.

**Step 4: Run the tests and build**

```bash
dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~GroundTargetRuleTests
/usr/bin/godot-mono --headless --path . --build-solutions --quit
```

Expected: PASS; builds.

**Step 5: Commit**

```bash
git add Scripts/GroundTargetRule.cs Scripts/Network/NetworkClient.cs Scripts/SpellTargetManager.cs Scripts/MapManager.cs \
  tests/Goose2Client.Tests/GroundTargetRuleTests.cs Scripts/**/*.uid
git commit -m "feat(spells): click a tile to cast a ground-targetable spell"
```

---

### Task 6: Preview shadow while targeting

**Files:**
- Create: `Scripts/SpellPreview.cs`
- Modify: `Scripts/Map/GroundZoneLayer.cs` (preview child)
- Modify: `Scripts/SpellTargetManager.cs` (`_Process`, hover, `ExitTargeting`)
- Modify: `Scripts/MapManager.cs:369-373` (`HandleWorldHover`, `ClearWorldHover`)
- Create: `tests/Goose2Client.Tests/SpellPreviewTests.cs`

**Step 1: Write the failing test**

```csharp
using Goose2Client;
using Xunit;

namespace Goose2Client.Tests;

public class SpellPreviewTests
{
    private static SpellInfo Spell(SpellShape shape, int size, bool ground) =>
        new() { Name = "S", Shape = shape, ShapeSize = size, GroundTargetable = ground };

    [Fact]
    public void Ground_spells_anchor_on_the_hovered_tile()
    {
        var anchor = SpellPreview.Anchor(Spell(SpellShape.Area, 1, true), hover: (5, 6), target: (1, 1));

        Assert.Equal((5, 6), anchor);
    }

    [Fact]
    public void Other_spells_anchor_on_the_reticle_target()
    {
        var anchor = SpellPreview.Anchor(Spell(SpellShape.Area, 1, false), hover: (5, 6), target: (1, 1));

        Assert.Equal((1, 1), anchor);
    }

    [Fact]
    public void Ground_spells_fall_back_to_the_target_with_no_hover()
    {
        Assert.Equal((1, 1), SpellPreview.Anchor(Spell(SpellShape.Area, 1, true), hover: null, target: (1, 1)));
    }

    [Fact]
    public void Tiles_are_clipped_to_the_map()
    {
        var tiles = SpellPreview.Tiles(Spell(SpellShape.Area, 1, true), anchor: (0, 0), Direction.Down, mapSize: (10, 10));

        Assert.Equal(new[] { (0, 0), (1, 0), (0, 1), (1, 1) }, tiles);
    }

    [Fact]
    public void Shapes_without_a_preview_give_no_tiles()
    {
        Assert.Empty(SpellPreview.Tiles(Spell(SpellShape.Target, 0, false), (5, 5), Direction.Down, (10, 10)));
        Assert.Empty(SpellPreview.Tiles(Spell(SpellShape.Random, 2, false), (5, 5), Direction.Down, (10, 10)));
    }
}
```

**Step 2: Run to verify it fails**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~SpellPreviewTests`
Expected: build error.

**Step 3: Implement the pure part**

`Scripts/SpellPreview.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace Goose2Client
{
    public static class SpellPreview
    {
        public static (int X, int Y) Anchor(SpellInfo spell, (int X, int Y)? hover, (int X, int Y) target)
            => spell.GroundTargetable && hover is { } h ? h : target;

        public static List<(int X, int Y)> Tiles(SpellInfo spell, (int X, int Y) anchor, Direction facing,
            (int Width, int Height) mapSize)
        {
            if (!SpellShapes.HasPreview(spell.Shape)) return new();

            return SpellShapes.Tiles(spell.Shape, spell.ShapeSize, anchor.X, anchor.Y, facing)
                .Where(t => t.X >= 0 && t.Y >= 0 && t.X < mapSize.Width && t.Y < mapSize.Height)
                .Distinct()
                .ToList();
        }
    }
}
```

**Step 4: Run the test**

Run: `dotnet test tests/Goose2Client.Tests --filter FullyQualifiedName~SpellPreviewTests`
Expected: PASS.

**Step 5: Draw the preview**

In `GroundZoneLayer`, add a nested drawing node and two public methods:

```csharp
        private readonly PreviewNode _preview = new() { Name = "Preview" };

        public override void _Ready() => AddChild(_preview);

        public void ShowPreview(List<(int X, int Y)> tiles, Color color) => _preview.Set(tiles, color);

        public void HidePreview() => _preview.Set(new(), default);

        private sealed partial class PreviewNode : Node2D
        {
            private List<(int X, int Y)> _tiles = new();
            private Color _color;

            public void Set(List<(int X, int Y)> tiles, Color color)
            {
                _tiles = tiles;
                _color = color;
                QueueRedraw();
            }

            public override void _Draw()
            {
                var size = new Vector2(MapCoords.TileSize, MapCoords.TileSize);
                foreach (var (x, y) in _tiles)
                    DrawRect(new Rect2(MapCoords.TileCenter(x, y) - size / 2f, size), _color);
            }
        }
```

If Godot's source generator rejects a nested `partial` Node class, move `PreviewNode` to its own file
`Scripts/Map/GroundZonePreview.cs` as `public partial class GroundZonePreview : Node2D`.

**Step 6: Drive it from `SpellTargetManager`**

Add a field `private (int X, int Y)? _hoverTile;` and:

```csharp
    public void OnWorldHover(int x, int y) => _hoverTile = (x, y);

    public void ClearWorldHover() => _hoverTile = null;

    private void UpdatePreview()
    {
        var mm = GameManager.Instance.CurrentMapManager;
        if (mm == null || !GodotObject.IsInstanceValid(mm)) return;

        var player = mm.LocalPlayer;
        var map = GameManager.Instance.CurrentMap;
        if (!IsTargeting || _pendingSpell is not { } spell || _target == null || player == null || map == null)
        {
            mm.GroundZones.HidePreview();
            return;
        }

        var anchor = SpellPreview.Anchor(spell, _hoverTile, (_target.X, _target.Y));
        var tiles = SpellPreview.Tiles(spell, anchor, player.Facing, (map.Width, map.Height));
        bool usable = !spell.GroundTargetable || _hoverTile == null || CanCastAt(spell, anchor.X, anchor.Y);
        var color = usable ? TargetBoxColor.CurrentColor() with { A = 0.35f } : new Color(0.5f, 0.5f, 0.5f, 0.35f);
        mm.GroundZones.ShowPreview(tiles, color);
    }
```

Call `UpdatePreview()` at the top of `_Process` (before the hold-gate early return — the preview must follow
the mouse and the player's facing every frame) and at the end of `ExitTargeting()`.

`MapManager`:

```csharp
    public void HandleWorldHover(Vector2 worldPos)
    {
        Goose2Client.UI.TooltipManager.Instance?.ShowNameTooltip(CharacterAt(worldPos));
        var (tx, ty) = MapCoords.WorldToTile(worldPos);
        GameManager.Instance.SpellTargetManager?.OnWorldHover(tx, ty);
    }

    public void ClearWorldHover()
    {
        Goose2Client.UI.TooltipManager.Instance?.HideNameTooltip();
        GameManager.Instance.SpellTargetManager?.ClearWorldHover();
    }
```

Check that `Color` supports `with { A = … }` in this GodotSharp version (it is a struct with a settable `A`).
If not, use `var c = TargetBoxColor.CurrentColor(); c.A = 0.35f;`.

**Step 7: Build and run all tests**

```bash
/usr/bin/godot-mono --headless --path . --build-solutions --quit
dotnet test tests/Goose2Client.Tests
```

Expected: builds; baseline + new tests pass.

**Step 8: Commit**

```bash
git add Scripts/SpellPreview.cs Scripts/Map/GroundZoneLayer.cs Scripts/SpellTargetManager.cs Scripts/MapManager.cs \
  tests/Goose2Client.Tests/SpellPreviewTests.cs Scripts/**/*.uid
git commit -m "feat(spells): preview spell areas while targeting"
```

---

### Task 7: Manual verification against a Part 2 server

Run the Part 2 server locally with the Fire Patch, Sanctuary and Tar Pit spells from Part 1/2's manual steps,
and this client from the worktree (with sprites available — see Task 0).

1. **Preview:** select Fire Patch. A translucent 3×3 follows the mouse over empty ground and snaps to a
   character under the reticle; it goes grey past the edge of the view. A plain area nuke on a target also
   previews; a single-target spell doesn't.
2. **Tile click:** click empty ground in range. The zone appears there, looping, under characters and
   dropped items, over the floor. The cooldown starts. A click out of view does nothing and keeps targeting.
3. **Character click / keyboard:** cast Fire Patch on a mob with the reticle and Enter. The zone stays where
   the mob stood when it walks off.
4. **Lifetime:** the zone vanishes when the server sends `ZDEL`; kill the server's network mid-zone (or
   block packets) and confirm the client still clears it when `remainingMs` runs out.
5. **Join mid-zone:** log a second client in near an active zone. It appears with the right remaining time.
6. **Map change:** warp away and back during a zone. No orphaned sprites on the new map; the zone reappears
   on return if it is still active.
7. **Looping art (open item from the design):** note which effect animations loop cleanly and which look
   wrong when repeated, so the data can pick suitable ones or new art can be requested.

Report what you saw for each step, including anything that looked off.
