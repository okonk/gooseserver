# Revive System — Part 1: Server Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Dead players stay on their tile as corpses for `ReviveTimeout` seconds, can be revived in place by a Revive spell, and otherwise respawn as today.

**Architecture:** A standalone `Player.IsDead` flag (Player `State` stays `Ready`) set from `Player.Attacked`, a `RespawnTimeoutEvent` scheduled on the event queue, a single corpse gate in `SpellEffect.CastSpell`, explicit `IsDead` early-outs in the blocked event handlers, and a trailing `RespawnSeconds` token on player MKC/CHP packets. Map scripts opt out through a new `IMapScript.AllowsRevive` hook (arena opts out).

**Tech Stack:** C# / .NET 10, xUnit (`Goose.Tests`), Roslyn `.csx` map scripts.

Design: `docs/plans/2026-11-15-revive-design.md`. This is Part 1 of 2; the client is Part 2.

Follow `AGENTS.md`: no new comments or doc strings except wire-format details and non-obvious invariants.

---

## APIs verified

| API | Location |
|---|---|
| `Player.States` enum, `State` | `Goose/Player.cs:61-68` |
| `Player.Attacked` (death branch `CurrentHP <= 0`) | `Goose/Player.cs:2098-2195` (death branch 2145-2172) |
| `Player.NotifyDied(ICharacter, GameWorld) : RespawnLocation?` | `Goose/Player.cs:2197` |
| `Player.WarpTo(GameWorld, Map, int, int)` (cross-map sets `Map = null`, `State = LoadingMap`) | `Goose/Player.cs:1489`, `1569-1586` |
| `Player.AddRegenEvent(GameWorld)` | `Goose/Player.cs:1594` |
| `Player.SendCHPString(GameWorld)` (self + range) | `Goose/Player.cs:1733` |
| `Player.Windows`, `Player.Pets` (initialised to `[]` in ctor) | `Goose/Player.cs:389`, `433`, `580-585` |
| `Player.Mounted` | `Goose/Player.cs:2663` |
| `Player.BodyID`, `Player.CurrentBodyID` | `Goose/Player.cs:276` |
| `Player.MaxHP` / `MaxMP` | `Goose/Player.cs:193-200` |
| `Player.CastSpell` charges cost even when the effect returns false | `Goose/Player.cs:2320-2360` |
| `Pet.Destroy(GameWorld)` (does not mutate `Owner.Pets`), `Pet.IsAlive` | `Goose/Pet.cs:514`, `86` |
| `NPC.RemoveAggro(Player)` | `Goose/NPC.cs:1104` |
| `NPC.AddAggro(Player, long, bool, GameWorld)` | `Goose/NPC.cs:1031` |
| `NPC.AggroIfInRange(Player, GameWorld)` | `Goose/NPC.cs:1132` |
| `NPC.AggroTarget` | `Goose/NPC.cs:317` |
| `Window.Close(Player, GameWorld)` | `Goose/Window.cs:347` |
| `Inventory.MountDisplay()` (tinted branch trailing comma bug) | `Goose/Inventory.cs:971-995` |
| `Inventory.RemoveMountBuff(Item, GameWorld, bool = true)` | `Goose/Inventory.cs:1011` |
| `Inventory.GetEquippedSlot(EquipSlots)` | `Goose/Inventory.cs` (used at `Events/ToggleMountEvent.cs:11`) |
| `P.MakeCharacter`, `P.UpdateCharacter` | `Goose/Packets.cs:114-152`, `154-186` |
| `P.Notice(NoticeType, string)` → `"NTF" + (int)type + "," + msg` | `Goose/Packets.cs:43` |
| `BodyClassification.IsLayered(int)` (`< 100` or `10000-10099`) | `Goose/BodyClassification.cs:7` |
| `GameWorld.TimeNow` = `Stopwatch.GetTimestamp()`, `TimerFrequency` = `Stopwatch.Frequency` | `Goose/GameWorld.cs:182-191`, `212` |
| `GameWorld.Send(Player, string)` appends `"\x1"` and calls `player.Send` | `Goose/GameWorld.cs:824` |
| `EventHandler.AddEvent(Event)`, `RemoveEvent(Event)` (reference equality) | `Goose/EventHandler.cs:300`, `314` |
| Packet table `_SeedCommands` | `Goose/EventHandler.cs:116-163` |
| `Event` base (`Ticks`, `Player`, `Data`, `Ready`) | `Goose/Event.cs` |
| `RegenEvent` player branch (`State == Ready`) | `Goose/Events/RegenEvent.cs:66` |
| `LogoutEvent` in-game branch | `Goose/Events/LogoutEvent.cs:42` |
| `IMapScript`, `BaseMapScript.OnPlayerDied` | `Goose/Scripting/IMapScript.cs:28`, `Goose/Scripting/BaseMapScript.cs:121` |
| `SpellEffect.EffectTypes` (last = `Script`), `CastSpell`, `CanCastSpell`, `CastBindSpell` (animation pattern) | `Goose/SpellEffect.cs:69-92`, `1048-1088`, `855`, `667-697` |
| `CanCastSpell` requires `WorksNotInPVP` for player→player on non-PvP maps | `Goose/SpellEffect.cs:864` |
| `ArenaMap.OnPlayerDied` (warps the player itself) | `Goose/Data/Illutia/Scripts/Arena/ArenaMap.csx:24-45` |
| `DimensionMap` delegates via `DimensionHelpers.BaseMapScript(map, world)` (returns `IMapScript`) | `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx:157-160`, `DimensionHelpers.csx:24` |
| `DropGoldCommand.Execute(CommandContext, int)` | `Goose/Commands/DropGoldCommand.cs:6` |
| `/buyvita`, `/buymana` never touch `CurrentHP`/`CurrentMP` (grep) → stay allowed | `Goose/Commands/BuyVitaCommand.cs`, `BuyManaCommand.cs` |
| Test fixture: `TestWorldFixture(Action<GooseSettings>?)`, `AddBaseMap`, `AddNpcTemplate`, `SpawnNpc`, `CommandPlayerOn`, `AddOnlinePlayer` (assigns `LoginID`), `RunCommand`, `AddBaseSpellEffect`, `CapturingPlayer.Sent` | `TestSupport/TestWorldFixture.cs` |
| Mount fixture pattern (`AddBaseItemTemplate` with `Slot = Mount`, `Inventory.Equip` sets `Mounted`) | `Goose.Tests/MountSpeedTests.cs:25-64` |
| Pet construction in tests | `Goose.Tests/PetDestroyInvisibilityTests.cs:95-123` |
| `ArenaTestKit.AttachMapScript()`, `LobbyPlayer` | `Goose.Tests/Arena/ArenaMapTests.cs:30-40` |
| `InternalsVisibleTo` Goose.Tests / Goose.IntegrationTests | `Goose/Goose.csproj:18-25` |

Commands (run from the worktree root `/home/hayden/code/illutiagooseserver/.worktrees/revive`):

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~<Name>
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Baseline: 2868 passing.

### Design deltas (decided during planning)

- `Player.Revive` takes the reviver: `Revive(ICharacter reviver, GameWorld world)` (needed for the notice).
- The Revive spell effect must have `WorksNotInPVP = true` in spell data, otherwise `CanCastSpell` (`SpellEffect.cs:864`) rejects player→player on normal maps.
- A throwing `AllowsRevive` falls back to today's instant respawn (like `OnPlayerDied` falls back to the bind point).
- In the instant-respawn path, HP is now set by `Respawn` *after* `OnPlayerDied` runs (previously 50% before). Scripts see the player at ≤ 0 HP during `OnPlayerDied`.
- `ReviveTimeout` defaults to 60 in C# (prod's `/data/GooseSettings.json` won't have the key). Tests that assert instant respawn set `ReviveTimeout = 0`.

Update the design doc's "Server: Revive spell" section with the first two bullets in Task 3's commit.

---

## Shared test kit (created in Task 2, used by Tasks 2–6)

`Goose.Tests/Revive/ReviveTestKit.cs`:

```csharp
using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Revive;

internal sealed class ReviveTestKit : IDisposable
{
    public TestWorldFixture Fixture { get; }
    public GameWorld World => Fixture.World;
    public Map Map { get; }
    public Map BoundMap { get; }
    public NPC Killer { get; }

    public ReviveTestKit(int reviveTimeout = 60)
    {
        Fixture = new TestWorldFixture(s => s.ReviveTimeout = reviveTimeout);
        Map = Fixture.AddBaseMap(1, "Field", 40, 40);
        Map.CanCast = true;
        Map.CanUseItems = true;
        BoundMap = Fixture.AddBaseMap(2, "Town", 40, 40);
        var template = Fixture.AddNpcTemplate(500, "Wolf", t => t.AggroRange = 0);
        Killer = Fixture.SpawnNpc(Map, 8, 5, template);
    }

    public TestWorldFixture.CapturingPlayer PlayerAt(int x, int y, string name = "Tester")
    {
        var p = Fixture.CommandPlayerOn(Map, x, y, name);
        Fixture.AddOnlinePlayer(p);
        p.Level = 1;
        p.MaxStats = new AttributeSet { HP = 100, MP = 100 };
        p.CurrentHP = 100;
        p.CurrentMP = 100;
        p.BodyID = 1;
        p.CurrentBodyID = 1;
        p.HairA = 255;
        p.FaceID = 70;
        p.BoundMap = BoundMap;
        p.BoundX = 5;
        p.BoundY = 6;
        Map.Players.Add(p);
        Map.SetCharacter(p, x, y);
        return p;
    }

    public void Kill(Player p) => p.Attacked(Killer, p.CurrentHP + 1000, World);

    public void Dispose() => Fixture.Dispose();
}
```

---

### Task 1: `MountDisplay` trailing-comma fix

**Files:**
- Modify: `Goose/Inventory.cs:983-987`
- Test: `Goose.Tests/MountDisplayTests.cs` (create)

**Step 1: Write the failing test**

```csharp
using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class MountDisplayTests
{
    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player) Setup(int graphicA)
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 2);
        var template = fixture.AddBaseItemTemplate(651, "Tank", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 273;
            t.GraphicR = 10;
            t.GraphicG = 20;
            t.GraphicB = 30;
            t.GraphicA = graphicA;
        });
        var mount = new Item();
        mount.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(mount, fixture.World);
        Assert.True(player.Inventory.AddItem(mount, 1, fixture.World));
        Assert.True(player.Inventory.Equip(mount, fixture.World));
        return (fixture, player);
    }

    [Fact]
    public void Tinted_mount_has_no_trailing_comma()
    {
        var (fixture, player) = Setup(graphicA: 100);
        using (fixture)
            Assert.Equal("273,10,20,30,100", player.Inventory.MountDisplay());
    }

    [Fact]
    public void Untinted_mount_is_id_star()
    {
        var (fixture, player) = Setup(graphicA: 0);
        using (fixture)
            Assert.Equal("273,*", player.Inventory.MountDisplay());
    }

    [Fact]
    public void Unmounted_is_zero_star()
    {
        var (fixture, player) = Setup(graphicA: 100);
        using (fixture)
        {
            player.Mounted = false;
            Assert.Equal("0,*", player.Inventory.MountDisplay());
        }
    }
}
```

**Step 2:** `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~MountDisplayTests` → `Tinted_mount_has_no_trailing_comma` FAILS (`"273,10,20,30,100,"`).

**Step 3:** In `Goose/Inventory.cs`, change the tinted branch's last line from `item.Item.GraphicA + ",";` to `item.Item.GraphicA;`.

**Step 4:** Re-run the filter → PASS. Run the full suite; fix any existing test that pinned the trailing comma (update the expected string).

**Step 5: Commit**

```bash
git add Goose/Inventory.cs Goose.Tests/MountDisplayTests.cs
git commit -m "fix(inventory): tinted mount display has no trailing comma"
```

---

### Task 2: Dead state, respawn timeout and death flow

**Files:**
- Modify: `Goose/GooseSettings.cs` (add setting near `LogoutLagTime`, line ~96)
- Modify: `Goose/GooseSettings.json` (add `"ReviveTimeout": 60,` after `"SpRespawnPercentage": 1,` line 64)
- Modify: `Goose/Scripting/IMapScript.cs:28`, `Goose/Scripting/BaseMapScript.cs:121`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx:157-160`
- Modify: `Goose/Player.cs` (properties near line 68; `Attacked` 2100 and 2145-2172; new methods after `NotifyDied` ~2222)
- Create: `Goose/Events/RespawnTimeoutEvent.cs`
- Modify: `Goose/Events/RegenEvent.cs:66`
- Modify: `Goose/NPC.cs:1031`, `1132`
- Modify: `Goose.Tests/PlayerDiedHookTests.cs:23-29` (settings)
- Create: `Goose.Tests/Revive/ReviveTestKit.cs` (above), `Goose.Tests/Revive/ReviveDeathTests.cs`

**Step 1: Write the failing tests** — `Goose.Tests/Revive/ReviveDeathTests.cs`:

```csharp
using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Revive;

public class ReviveDeathTests
{
    [Fact]
    public void Death_leaves_a_corpse_on_the_tile()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.True(p.IsDead);
        Assert.Equal(0, p.CurrentHP);
        Assert.Equal(0, p.CurrentMP);
        Assert.Same(kit.Map, p.Map);
        Assert.Equal(5, p.MapX);
        Assert.Equal(5, p.MapY);
    }

    [Fact]
    public void Death_schedules_the_respawn_timeout()
    {
        using var kit = new ReviveTestKit(reviveTimeout: 60);
        var p = kit.PlayerAt(5, 5);
        long before = kit.World.TimeNow;

        kit.Kill(p);

        Assert.NotNull(p.PendingRespawnTimeout);
        Assert.InRange(p.RespawnDeadline, before + 59 * kit.World.TimerFrequency, kit.World.TimeNow + 60 * kit.World.TimerFrequency);
    }

    [Fact]
    public void Revive_disabled_respawns_at_bind_like_today()
    {
        using var kit = new ReviveTestKit(reviveTimeout: 0);
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
        Assert.Equal(5, p.MapX);
        Assert.Equal(6, p.MapY);
        Assert.Equal(50, p.CurrentHP);
        Assert.Equal(10, p.CurrentMP);
    }

    [Fact]
    public void Map_opt_out_respawns_at_bind_like_today()
    {
        using var kit = new ReviveTestKit();
        kit.Map.Script = ScriptStub.For<IMapScript>(new NoReviveMapScript());
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
    }

    [Fact]
    public void Throwing_opt_out_hook_respawns_at_bind_like_today()
    {
        using var kit = new ReviveTestKit();
        kit.Map.Script = ScriptStub.For<IMapScript>(new ThrowingReviveMapScript());
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
    }

    [Fact]
    public void Death_still_runs_OnPlayerDied()
    {
        using var kit = new ReviveTestKit();
        var recorder = new RecordingMapScript();
        kit.Map.Script = ScriptStub.For<IMapScript>(recorder);
        var p = kit.PlayerAt(5, 5);

        kit.Kill(p);

        Assert.Equal(1, recorder.Deaths);
        Assert.True(p.IsDead);
    }

    [Fact]
    public void Timeout_respawns_at_bind_with_half_hp_and_tenth_mp()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        p.PendingRespawnTimeout!.Ready(kit.World);

        Assert.False(p.IsDead);
        Assert.Null(p.PendingRespawnTimeout);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
        Assert.Equal(5, p.MapX);
        Assert.Equal(6, p.MapY);
        Assert.Equal(50, p.CurrentHP);
        Assert.Equal(10, p.CurrentMP);
    }

    [Fact]
    public void Timeout_uses_the_location_OnPlayerDied_returned()
    {
        using var kit = new ReviveTestKit();
        kit.Map.Script = ScriptStub.For<IMapScript>(new RecordingMapScript { Respawn = new RespawnLocation(kit.Map, 7, 8) });
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        p.PendingRespawnTimeout!.Ready(kit.World);

        Assert.Same(kit.Map, p.Map);
        Assert.Equal(7, p.MapX);
        Assert.Equal(8, p.MapY);
    }

    [Fact]
    public void Stale_timeout_does_nothing()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.BoundMap = kit.Map;
        p.BoundX = 10;
        p.BoundY = 10;
        kit.Kill(p);
        var first = p.PendingRespawnTimeout!;
        p.Respawn(kit.World);
        kit.Kill(p);

        first.Ready(kit.World);

        Assert.True(p.IsDead);
        Assert.NotSame(first, p.PendingRespawnTimeout);
    }

    [Fact]
    public void Corpse_ignores_damage_and_heals()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        p.Attacked(kit.Killer, 50, kit.World);
        p.Attacked(kit.Killer, -50, kit.World);

        Assert.Equal(0, p.CurrentHP);
        Assert.True(p.IsDead);
    }

    [Fact]
    public void Corpse_does_not_regen()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.MaxStats.HPPercentRegen = 0.5;
        kit.Kill(p);

        new RegenEvent { Data = p }.Ready(kit.World);

        Assert.Equal(0, p.CurrentHP);
    }

    [Fact]
    public void Death_clears_npc_aggro_and_corpses_cannot_gain_aggro()
    {
        using var kit = new ReviveTestKit();
        var brute = kit.Fixture.SpawnNpc(kit.Map, 6, 5, kit.Fixture.AddNpcTemplate(501, "Brute"));
        var p = kit.PlayerAt(5, 5);
        brute.AddAggro(p, 10, kit.World);
        Assert.Same(p, brute.AggroTarget);

        kit.Kill(p);
        Assert.Null(brute.AggroTarget);

        brute.AggroIfInRange(p, kit.World);
        brute.AddAggro(p, 10, kit.World);
        Assert.Null(brute.AggroTarget);
    }

    [Fact]
    public void Death_destroys_pets()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        var pet = new Pet
        {
            LoginID = 1001, Name = "Pet", Owner = p,
            Class = kit.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet { HP = 100 }, MaxStats = new AttributeSet { HP = 100 },
            HairA = 255, FaceID = 70, State = Player.States.Ready,
            Map = kit.Map, MapID = kit.Map.ID, MapX = 6, MapY = 6,
        };
        pet.CurrentHP = 100;
        kit.Map.Players.Add(pet);
        kit.Map.SetCharacter(pet, 6, 6);
        p.Pets.Add(pet);

        kit.Kill(p);

        Assert.False(pet.IsAlive);
    }

    [Fact]
    public void Death_closes_windows()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.Windows.Add(new Window { ID = 42, Type = Window.WindowTypes.Vendor });

        kit.Kill(p);

        Assert.Empty(p.Windows);
    }

    [Fact]
    public void Death_dismounts()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        var template = kit.Fixture.AddBaseItemTemplate(651, "Tank", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 273;
        });
        var mount = new Item();
        mount.LoadFromTemplate(template);
        kit.World.ItemHandler.AddAndAssignId(mount, kit.World);
        Assert.True(p.Inventory.AddItem(mount, 1, kit.World));
        Assert.True(p.Inventory.Equip(mount, kit.World));
        Assert.True(p.Mounted);

        kit.Kill(p);

        Assert.False(p.Mounted);
    }

    private sealed class NoReviveMapScript : BaseMapScript
    {
        public override bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world) => false;
    }

    private sealed class ThrowingReviveMapScript : BaseMapScript
    {
        public override bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class RecordingMapScript : BaseMapScript
    {
        public int Deaths { get; private set; }
        public RespawnLocation? Respawn { get; init; }

        public override RespawnLocation? OnPlayerDied(Map map, Player player, ICharacter killer, GameWorld world)
        {
            this.Deaths++;
            return this.Respawn;
        }
    }
}
```

Note: `new Window { ID = .., Type = .. }` — confirm `Window` has a parameterless constructor (`Goose/Window.cs:12`, properties `Type` line 96 and `ID` line 122 have public setters). If it has no parameterless ctor, construct it the way `Goose/StylistWindow.cs` does and add it to `p.Windows`.

**Step 2:** Run `--filter FullyQualifiedName~ReviveDeathTests` → compile FAIL (`IsDead`, `PendingRespawnTimeout`, `RespawnDeadline`, `Respawn`, `AllowsRevive`, `ReviveTimeout` missing).

**Step 3: Implement**

`Goose/GooseSettings.cs` (next to `LogoutLagTime`):

```csharp
        public int ReviveTimeout { get; set; } = 60;
```

`Goose/GooseSettings.json` after line 64: `"ReviveTimeout": 60,`

`Goose/Scripting/IMapScript.cs`, after `OnPlayerDied`:

```csharp
        bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world);
```

`Goose/Scripting/BaseMapScript.cs`, after `OnPlayerDied`:

```csharp
        public virtual bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world)
        {
            return true;
        }
```

`Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx`, after `OnPlayerDied` (line 160):

```csharp
    public override bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world)
    {
        return DimensionHelpers.BaseMapScript(map, world)?.AllowsRevive(map, player, killer, world) ?? true;
    }
```

`Goose/Player.cs` — properties after `public States State { get; set; }` (line 68):

```csharp
        public bool IsDead { get; private set; }
        public RespawnLocation? DeathRespawn { get; private set; }
        public long RespawnDeadline { get; private set; }
        internal RespawnTimeoutEvent? PendingRespawnTimeout { get; private set; }
```

(`RespawnTimeoutEvent` lives in `Goose.Events`; add `using Goose.Events;` at the top of `Player.cs` if it isn't already there.)

`Player.Attacked` line 2100:

```csharp
            if (this.State != States.Ready || this.IsDead) return;
```

Replace the death branch body (lines 2145-2172, from `if (this.CurrentHP <= 0)` through `this.SendBuffBar(world);` and its closing brace) with:

```csharp
            if (this.CurrentHP <= 0)
            {
                world.SendToMap(this.Map, P.ServerMessage(this.Name + " was slain by " + character.Name + "."));
                this.Die(character, world);
            }
```

(keep the existing `else { packet += P.VitalsPercentage(this); this.AddRegenEvent(world); }` that follows.)

New methods after `NotifyDied`:

```csharp
        private void Die(ICharacter killer, GameWorld world)
        {
            Map deathMap = this.Map;
            this.DeathRespawn = this.NotifyDied(killer, world);

            if (world.Settings.ReviveTimeout <= 0 || !this.MapAllowsRevive(deathMap, killer, world))
            {
                this.Respawn(world);
                this.RemoveDeathBuffs(world);
                return;
            }

            this.IsDead = true;
            this.CurrentHP = 0;
            this.CurrentMP = 0;

            this.RemoveDeathBuffs(world);
            this.Dismount(world);

            foreach (var pet in this.Pets.Where(p => p.IsAlive).ToList())
                pet.Destroy(world);
            foreach (var npc in this.Map.GetNPCsInRange(this))
                npc.RemoveAggro(this);
            foreach (var window in this.Windows.ToList())
                window.Close(this, world);

            this.RespawnDeadline = world.TimeNow + world.Settings.ReviveTimeout * world.TimerFrequency;
            this.PendingRespawnTimeout = new RespawnTimeoutEvent { Ticks = this.RespawnDeadline, Data = this };
            world.EventHandler.AddEvent(this.PendingRespawnTimeout);

            this.SendCHPString(world);
            this.SendVitals(world);
        }

        public void Respawn(GameWorld world)
        {
            this.CancelRespawnTimeout(world);
            this.IsDead = false;
            this.CurrentHP = (long)(this.MaxHP * 0.5);
            this.CurrentMP = (long)(this.MaxMP * 0.1);

            RespawnLocation? respawn = this.DeathRespawn;
            this.DeathRespawn = null;
            this.WarpTo(world, respawn?.Map ?? this.BoundMap, respawn?.X ?? this.BoundX, respawn?.Y ?? this.BoundY);

            this.AddRegenEvent(world);
            world.Send(this, P.VitalsPercentage(this));
            world.Send(this, P.StatusInfo(this));
        }

        private bool MapAllowsRevive(Map map, ICharacter killer, GameWorld world)
        {
            try
            {
                return map.Script?.Object.AllowsRevive(map, this, killer, world) ?? true;
            }
            catch (Exception e)
            {
                log.Error(e, "Map AllowsRevive {0} ({1}) player {2} ({3}) Exception", map.Name, map.ID, this.Name, this.LoginID);
                return false;
            }
        }

        private void CancelRespawnTimeout(GameWorld world)
        {
            if (this.PendingRespawnTimeout is null) return;
            world.EventHandler.RemoveEvent(this.PendingRespawnTimeout);
            this.PendingRespawnTimeout = null;
        }

        private void RemoveDeathBuffs(GameWorld world)
        {
            foreach (var b in this.Buffs.Where(b => !b.ItemBuff).ToList())
            {
                this.RemoveBuff(b, world, false, updateCharacter: true);
            }

            this.SendBuffBar(world);
        }

        private void Dismount(GameWorld world)
        {
            if (!this.Mounted) return;

            this.Mounted = false;
            ItemSlot? mount = this.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount);
            if (mount is not null) this.Inventory.RemoveMountBuff(mount.Item, world);
        }

        private void SendVitals(GameWorld world)
        {
            string vitals = P.VitalsPercentage(this);
            world.Send(this, vitals);
            world.Send(this, P.StatusInfo(this));
            foreach (var player in this.Map.GetPlayersInRange(this))
            {
                world.Send(player, vitals);
            }
        }
```

`Respawn` sends vitals only to self because a cross-map warp leaves `Map` null (`Player.cs:1580`).

`Goose/Events/RespawnTimeoutEvent.cs`:

```csharp
namespace Goose.Events
{
    public class RespawnTimeoutEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Player player = (Player)this.Data;
            if (!player.IsDead || player.PendingRespawnTimeout != this) return;

            player.Respawn(world);
        }
    }
}
```

`Goose/Events/RegenEvent.cs:66`:

```csharp
                if (player.State == Player.States.Ready && !player.IsDead)
```

`Goose/NPC.cs` — first line of `AddAggro(Player player, long value, bool wastaunt, GameWorld world)` (1031) and of `AggroIfInRange` (1132):

```csharp
            if (player.IsDead) return;
```

`Goose.Tests/PlayerDiedHookTests.cs` — add `ReviveTimeout = 0,` to the `GooseSettings` initializer (these tests pin today's instant respawn).

**Step 4:** Run `--filter FullyQualifiedName~ReviveDeathTests` → PASS. Run the full suite. Any other test that kills a player and asserts the bind/respawn warp: add `ReviveTimeout = 0` to its settings (or `new TestWorldFixture(s => s.ReviveTimeout = 0)`). Do not change production code to make such a test pass.

**Step 5: Commit**

```bash
git add Goose Goose.Tests
git commit -m "feat(revive): dead state, respawn timeout and AllowsRevive map hook"
```

---

### Task 3: Revive spell effect and corpse gate

**Files:**
- Modify: `Goose/SpellEffect.cs:69-92` (enum), `1048-1088` (`CastSpell`), add `CastReviveSpell` after `CastBindSpell` (~697)
- Modify: `Goose/Player.cs` (add `Revive` next to `Respawn`)
- Modify: `docs/plans/2026-11-15-revive-design.md` (design deltas)
- Test: `Goose.Tests/Revive/ReviveSpellTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
using Goose;
using Xunit;

namespace Goose.Tests.Revive;

public class ReviveSpellTests
{
    private static SpellEffect ReviveEffect(ReviveTestKit kit) => kit.Fixture.AddBaseSpellEffect(900, "Revive", e =>
    {
        e.EffectType = SpellEffect.EffectTypes.Revive;
        e.Effected = SpellEffect.SpellEffected.Player;
        e.WorksNotInPVP = true;
    });

    [Fact]
    public void Revive_stands_the_corpse_up_in_place_at_half_hp_and_mp()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);

        Assert.True(ReviveEffect(kit).Cast(healer, corpse, kit.World));

        Assert.False(corpse.IsDead);
        Assert.Null(corpse.PendingRespawnTimeout);
        Assert.Null(corpse.DeathRespawn);
        Assert.Same(kit.Map, corpse.Map);
        Assert.Equal(5, corpse.MapX);
        Assert.Equal(5, corpse.MapY);
        Assert.Equal(50, corpse.CurrentHP);
        Assert.Equal(50, corpse.CurrentMP);
    }

    [Fact]
    public void Revive_notifies_only_the_revived_player()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);
        healer.Sent.Clear();

        ReviveEffect(kit).Cast(healer, corpse, kit.World);

        Assert.Contains(corpse.Sent, s => s.Contains(P.Notice(NoticeType.Info, "Healer revived you.")));
        Assert.DoesNotContain(healer.Sent, s => s.Contains("revived"));
    }

    [Fact]
    public void Revive_broadcasts_chp()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);
        healer.Sent.Clear();

        ReviveEffect(kit).Cast(healer, corpse, kit.World);

        Assert.Contains(healer.Sent, s => s.StartsWith("CHP" + corpse.LoginID + ","));
    }

    [Fact]
    public void Revive_on_a_living_player_fails()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var target = kit.PlayerAt(5, 5, "Target");
        target.CurrentHP = 20;

        Assert.False(ReviveEffect(kit).Cast(healer, target, kit.World));
        Assert.Equal(20, target.CurrentHP);
    }

    [Fact]
    public void Revive_on_an_npc_fails()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var effect = ReviveEffect(kit);
        effect.Effected = SpellEffect.SpellEffected.NPCPlayer;

        Assert.False(effect.Cast(healer, kit.Killer, kit.World));
    }

    [Fact]
    public void Other_effects_do_nothing_to_a_corpse()
    {
        using var kit = new ReviveTestKit();
        var healer = kit.PlayerAt(4, 5, "Healer");
        var corpse = kit.PlayerAt(5, 5, "Corpse");
        kit.Kill(corpse);
        var buff = kit.Fixture.AddBaseSpellEffect(901, "Shield", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Effected = SpellEffect.SpellEffected.Player;
            e.WorksNotInPVP = true;
            e.Duration = 1000;
        });

        Assert.False(buff.Cast(healer, corpse, kit.World));
        Assert.Empty(corpse.Buffs);
        Assert.True(corpse.IsDead);
    }
}
```

**Step 2:** Run `--filter FullyQualifiedName~ReviveSpellTests` → compile FAIL (`EffectTypes.Revive`).

**Step 3: Implement**

`SpellEffect.EffectTypes`: append after `Script`:

```csharp
            Script,
            Revive
```

`SpellEffect.CastSpell` — first statement in the method body, before `return this.EffectType switch`:

```csharp
            if (target is Player { IsDead: true } corpse)
                return this.EffectType == EffectTypes.Revive && this.CastReviveSpell(caster, corpse, world);
```

(A Revive on a living target falls through the switch to `_ => false`.)

`CastReviveSpell`, after `CastBindSpell`:

```csharp
        public bool CastReviveSpell(ICharacter caster, Player target, GameWorld world)
        {
            if (!this.CanCastSpell(caster, target)) return false;

            target.Revive(caster, world);

            List<string> packets = [];
            if (this.Animation != 0) packets.Add(P.SpellPlayer(target.LoginID, this.Animation, this.AnimationFile));
            if (this.DoCastAnimation) packets.Add(P.Cast(caster));
            if (packets.Count == 0) return true;

            string packet = string.Join("\x1", packets);
            world.Send(target, packet);
            foreach (var player in target.Map.GetPlayersInRange(target))
            {
                world.Send(player, packet);
            }

            return true;
        }
```

`Player.Revive`, next to `Respawn`:

```csharp
        public void Revive(ICharacter reviver, GameWorld world)
        {
            if (!this.IsDead) return;

            this.CancelRespawnTimeout(world);
            this.IsDead = false;
            this.DeathRespawn = null;
            this.CurrentHP = this.MaxHP / 2;
            this.CurrentMP = this.MaxMP / 2;

            this.AddRegenEvent(world);
            this.SendCHPString(world);
            this.SendVitals(world);
            world.Send(this, P.Notice(NoticeType.Info, reviver.Name + " revived you."));
        }
```

Design doc: in "Server: Revive spell", change `target.Revive(world)` to `target.Revive(caster, world)` and add "Spell data must set `WorksNotInPVP = true` (and `WorksInPVP` as desired); `CanCastSpell` rejects player→player casts on non-PvP maps otherwise."

**Step 4:** Run the filter → PASS; run the full suite (e.g. `SpellCloneTests` must still pass; the enum change adds no property).

**Step 5: Commit**

```bash
git add Goose docs Goose.Tests
git commit -m "feat(revive): Revive spell effect and corpse-only-accepts-revive gate"
```

---

### Task 4: `RespawnSeconds` on MKC/CHP and base body while dead

**Files:**
- Modify: `Goose/Player.cs` (add `RespawnSeconds` next to the Task 2 properties)
- Modify: `Goose/Packets.cs:95-186` (MKC doc comment, `MakeCharacter`, `UpdateCharacter`)
- Test: `Goose.Tests/Revive/RevivePacketTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
using Goose;
using Xunit;

namespace Goose.Tests.Revive;

public class RevivePacketTests
{
    private static Item EquipMount(ReviveTestKit kit, Player p, int graphicA)
    {
        var template = kit.Fixture.AddBaseItemTemplate(651, "Tank", ItemTemplate.UseTypes.Armor, t =>
        {
            t.Slot = ItemTemplate.ItemSlots.Mount;
            t.GraphicEquipped = 273;
            t.GraphicA = graphicA;
        });
        var mount = new Item();
        mount.LoadFromTemplate(template);
        kit.World.ItemHandler.AddAndAssignId(mount, kit.World);
        Assert.True(p.Inventory.AddItem(mount, 1, kit.World));
        Assert.True(p.Inventory.Equip(mount, kit.World));
        return mount;
    }

    private static int LastToken(string packet) => int.Parse(packet[(packet.LastIndexOf(',') + 1)..]);

    [Fact]
    public void Alive_player_packets_end_with_zero()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);

        Assert.EndsWith(",0,*,0", P.MakeCharacter(p));
        Assert.EndsWith(",0,*,0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Corpse_packets_end_with_remaining_seconds()
    {
        using var kit = new ReviveTestKit(reviveTimeout: 60);
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        Assert.InRange(LastToken(P.MakeCharacter(p)), 59, 60);
        Assert.InRange(LastToken(P.UpdateCharacter(p)), 59, 60);
        Assert.EndsWith(",0,*," + p.RespawnSeconds, P.MakeCharacter(p));
    }

    [Fact]
    public void Untinted_mount_tail()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        EquipMount(kit, p, graphicA: 0);

        Assert.EndsWith(",273,*,0", P.MakeCharacter(p));
        Assert.EndsWith(",273,*,0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Tinted_mount_tail()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        EquipMount(kit, p, graphicA: 100);

        Assert.EndsWith(",273,255,255,255,100,0", P.MakeCharacter(p));
        Assert.EndsWith(",273,255,255,255,100,0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Non_layered_body_tail()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.CurrentBodyID = 105;

        int speed = p.CalculateMoveSpeed();

        Assert.EndsWith(",0,0," + speed + ",0,0", P.MakeCharacter(p));
        Assert.EndsWith(",0," + speed + ",0", P.UpdateCharacter(p));
    }

    [Fact]
    public void Corpse_shows_base_body_not_illusion()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        p.CurrentBodyID = 105;
        kit.Kill(p);

        Assert.StartsWith("CHP" + p.LoginID + ",1,", P.UpdateCharacter(p));
        Assert.Contains(",1,", P.MakeCharacter(p));
        Assert.DoesNotContain(",105,", P.MakeCharacter(p));
        Assert.Equal(105, p.CurrentBodyID);
    }

    [Fact]
    public void Npc_packets_are_unchanged_by_dead_state()
    {
        using var kit = new ReviveTestKit();
        string before = P.MakeNPCCharacter(kit.Killer);
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        Assert.Equal(before, P.MakeNPCCharacter(kit.Killer));
    }

    [Fact]
    public void Corpse_warped_while_dead_stays_dead_with_seconds()
    {
        using var kit = new ReviveTestKit();
        var viewer = kit.PlayerAt(10, 10, "Viewer");
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);
        viewer.Sent.Clear();

        p.WarpTo(kit.World, kit.Map, 9, 10);

        Assert.True(p.IsDead);
        var mkc = Assert.Single(viewer.Sent, s => s.StartsWith("MKC" + p.LoginID + ","));
        Assert.True(LastToken(mkc.TrimEnd('\x1')) >= 1);
    }
}
```

Notes for the implementer:
- The MKC/CHP layouts are at `Packets.cs:127-152` and `167-185`. The MKC for a layered body ends `...,<face>,<moveSpeed>,<isGM>,<mount>`, CHP ends `...,<face>,<moveSpeed>,<mount>`. `PlayerAt` leaves `Access` at its default (`Normal`) so `isGM` is `0`, and not mounted gives mount `0,*` — hence `",0,*,0"`.
- Non-layered tails: MKC `...,<invis>,<hideName>,<moveSpeed>,<isGM>,<respawn>`, CHP `...,<invis>,<moveSpeed>,<respawn>` (`Packets.cs:147-152`, `182-185`); today they end with a dangling `,`.
- If `Corpse_shows_base_body_not_illusion`'s MKC assertions are ambiguous against other fields, assert on the token at the body index (index 10 after splitting the `MKC`-stripped string on `,`) instead.

**Step 2:** Run `--filter FullyQualifiedName~RevivePacketTests` → FAIL (missing `RespawnSeconds`, wrong tails).

**Step 3: Implement**

`Goose/Player.cs`, next to `RespawnDeadline`:

```csharp
        public int RespawnSeconds => this.IsDead
            ? (int)Math.Max(1, Math.Ceiling((this.RespawnDeadline - System.Diagnostics.Stopwatch.GetTimestamp()) / (double)System.Diagnostics.Stopwatch.Frequency))
            : 0;
```

(`GameWorld.TimeNow`/`TimerFrequency` are `Stopwatch` values, `GameWorld.cs:189-191`, `212`; packet builders have no `GameWorld`.)

`Goose/Packets.cs`:

- MKC doc comment (line 99-102): append `,respawn_seconds` to the field list and add the line
  `* respawn_seconds = seconds until a dead player respawns, 0 while alive (players only)`.
- In both `MakeCharacter` and `UpdateCharacter`, before `bool layered`:

  ```csharp
            int body = player.IsDead ? player.BodyID : player.CurrentBodyID;
  ```

  and use `body` in place of `player.CurrentBodyID` for both the `layered` computation and the body field.
- `MakeCharacter` tail becomes:

  ```csharp
                          (player.Access > Player.AccessStatus.Normal ? "1" : "0") + "," + // Is GM
                          (layered ? player.Inventory.MountDisplay() + "," : "") + // Mount
                          player.RespawnSeconds; // Respawn seconds, 0 = alive
  ```

- `UpdateCharacter` tail becomes:

  ```csharp
                   player.CalculateMoveSpeed() + "," + // Move Speed
                   (layered ? player.Inventory.MountDisplay() + "," : "") + // Mount
                   player.RespawnSeconds; // Respawn seconds, 0 = alive
  ```

Leave `MakePetCharacter`, `UpdatePet`, `MakeNPCCharacter`, `UpdateNPC` and the hand-built MKCs (`HairdyeCommand`, `GmHaxCommand`, `CustomCommand`, `BodyStealerNPC.csx`) unchanged.

**Step 4:** Run the filter → PASS. Run the full suite. Existing tests that pin an exact player MKC/CHP string or `EndsWith` (search `Goose.Tests` for `MakeCharacter(` / `UpdateCharacter(` / `"MKC"` / `"CHP"`, e.g. `CharacterAppearancePacketTests`, `InvisibilityPacketTests`, `MountSpeedTests`) need `,0` appended to the expected player string; update the expectations, not the builders.

**Step 5: Commit**

```bash
git add Goose Goose.Tests
git commit -m "feat(revive): respawn seconds on player MKC/CHP and base body while dead"
```

---

### Task 4b: hide_name token on hand-built preview MKCs

Part 2 removes the client's "server omitted hide_name" MKC fallback. The only MKCs still
omitting it are the hand-built previews in the Illutia branches of `HairdyeCommand`
(`Goose/Commands/HairdyeCommand.cs:34-63`) and `CustomCommand`
(`Goose/Commands/CustomCommand.cs`, the Illutia branch ending `this.MountDisplay(...)` ~line 123).
`GmHaxCommand` and `BodyStealerNPC.csx` build CHPs, which have no hide_name field — leave them.
The non-Illutia `else` branches target another client and are left alone.

**Files:**
- Modify: `Goose/Commands/HairdyeCommand.cs`, `Goose/Commands/CustomCommand.cs`
- Test: `Goose.Tests/PreviewMkcHideNameTests.cs` (create) — or extend the existing hairdye/custom command tests if they already capture the preview MKC (search `Goose.Tests` for `"MKC9000"` / `"Hairdye Preview"` first and add the assertion there).

**Step 1: Failing test** — capture the preview MKC sent to the player and assert the token after
the invis `0` is a hide_name `0` followed by the face id:

```csharp
Assert.Contains(",0,0," + player.FaceID + ",", previewMkc);
```

(`FaceID` set to a distinctive value such as `71`, so the match is unambiguous.)

**Step 2:** Run it → FAIL.

**Step 3:** In both Illutia branches, after `"0" + "," + // Invis thing` insert:

```csharp
                    "0" + "," + // HideName
```

**Step 4:** Run the filter and the full suite → PASS.

**Step 5: Commit**

```bash
git add Goose Goose.Tests
git commit -m "fix(preview): hand-built preview MKCs send hide_name"
```

---

### Task 5: Block actions while dead

**Files:**
- Modify: `Goose/Player.cs` (add `RefuseWhileDead` next to `Revive`)
- Silent (add `&& !this.Player.IsDead` to the existing `if (this.Player.State == Player.States.Ready)`): `Goose/Events/MoveEvent.cs:20`, `FacingEvent.cs:25`, `PlayerAttackEvent.cs:17`, `EmoteEvent.cs:17`; `ToggleMountEvent.cs:7` becomes `if (this.Player.State != Player.States.Ready || this.Player.IsDead) return;`
- Notice (first line of `Ready`: `if (this.Player.RefuseWhileDead(world)) return;`): `PlayerCastSpellEvent.cs`, `InventoryUseEvent.cs`, `PlayerDropItemEvent.cs`, `PickupItemEvent.cs`, `DestroyItemEvent.cs`, `OpenCombineBagEvent.cs`, `WindowButtonClickEvent.cs`, `VendorPurchaseInventoryEvent.cs`, `VendorSellInventoryEvent.cs`, `InventoryToWindowEvent.cs`, `WindowToInventoryEvent.cs`, `WindowToWindowEvent.cs`, `EnchantWindowSlotEvent.cs`, `EnchantWindowCreateEvent.cs`, `CustomWindowSlotEvent.cs`, `CustomWindowCreateEvent.cs`, `StylistWindowChangeEvent.cs`, `AmountWindowSubmitEvent.cs` (all in `Goose/Events/`)
- NPC branch only: `Goose/Events/PlayerLeftClickEvent.cs` — after `if (npc.MapX != x || npc.MapY != y) continue;` (line ~55); `Goose/Events/PlayerRightClickEvent.cs` — first line inside `if (npc.MapX == x && npc.MapY == y) {` (line ~61). Insert `if (this.Player.RefuseWhileDead(world)) return;`
- `Goose/Commands/DropGoldCommand.cs:8` — after `var world = ctx.World;`: `if (ctx.Player.RefuseWhileDead(world)) return;`
- Test: `Goose.Tests/Revive/ReviveGatingTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
using Goose;
using Xunit;

namespace Goose.Tests.Revive;

public class ReviveGatingTests
{
    private static readonly string DeadNotice = P.Notice(NoticeType.Info, "You can't do that while dead.");

    private static (ReviveTestKit Kit, Testing.TestWorldFixture.CapturingPlayer Corpse) Corpse()
    {
        var kit = new ReviveTestKit();
        var corpse = kit.PlayerAt(5, 5);
        kit.Kill(corpse);
        corpse.Sent.Clear();
        return (kit, corpse);
    }

    [Theory]
    [InlineData("CAST1,1")]
    [InlineData("USE1")]
    [InlineData("DRP1,1")]
    [InlineData("GET")]
    [InlineData("DITM1")]
    [InlineData("OCB")]
    [InlineData("WBC1,1")]
    [InlineData("VPI1,1,1")]
    [InlineData("VSI1,1,1")]
    [InlineData("ITW1,1,1")]
    [InlineData("WTI1,1,1")]
    [InlineData("WTW1,1,1,1")]
    [InlineData("EWS1,1,1")]
    [InlineData("EWC1,1")]
    [InlineData("CWS1,1,1")]
    [InlineData("CWC1,1")]
    [InlineData("STC1,1,1")]
    [InlineData("AMT1,1")]
    [InlineData("LC8,5")]
    [InlineData("RC8,5")]
    [InlineData("/dropgold 5")]
    public void Blocked_action_sends_the_dead_notice(string packet)
    {
        var (kit, corpse) = Corpse();
        using (kit)
        {
            Assert.True(kit.Fixture.RunCommand(corpse, packet));
            Assert.Contains(corpse.Sent, s => s.Contains(DeadNotice));
        }
    }

    [Theory]
    [InlineData("M1")]
    [InlineData("M2")]
    [InlineData("F2")]
    [InlineData("ATT")]
    [InlineData("EMOT1")]
    [InlineData("MNT")]
    public void Silent_blocks_send_no_notice_and_do_not_move(string packet)
    {
        var (kit, corpse) = Corpse();
        using (kit)
        {
            var facing = corpse.Facing;

            Assert.True(kit.Fixture.RunCommand(corpse, packet));

            Assert.DoesNotContain(corpse.Sent, s => s.Contains(DeadNotice));
            Assert.Equal(5, corpse.MapX);
            Assert.Equal(5, corpse.MapY);
            Assert.Equal(facing, corpse.Facing);
        }
    }

    [Fact]
    public void Left_click_on_an_empty_tile_sends_no_notice()
    {
        var (kit, corpse) = Corpse();
        using (kit)
        {
            Assert.True(kit.Fixture.RunCommand(corpse, "LC10,10"));
            Assert.DoesNotContain(corpse.Sent, s => s.Contains(DeadNotice));
        }
    }

    [Fact]
    public void Corpse_can_chat()
    {
        var (kit, corpse) = Corpse();
        using (kit)
        {
            var listener = kit.PlayerAt(6, 6, "Listener");
            listener.Sent.Clear();

            Assert.True(kit.Fixture.RunCommand(corpse, ";hello there"));

            Assert.Contains(listener.Sent, s => s.Contains("hello there"));
            Assert.DoesNotContain(corpse.Sent, s => s.Contains(DeadNotice));
        }
    }
}
```

**Step 2:** Run `--filter FullyQualifiedName~ReviveGatingTests` → FAIL. If any packet string returns `false` from `RunCommand` (not registered / restricted), fix the test packet prefix against the table at `EventHandler.cs:116-163` rather than dropping the case.

**Step 3: Implement**

`Player.RefuseWhileDead`:

```csharp
        public bool RefuseWhileDead(GameWorld world)
        {
            if (!this.IsDead) return false;

            world.Send(this, P.Notice(NoticeType.Info, "You can't do that while dead."));
            return true;
        }
```

Then apply the edits listed under **Files**. Allowed actions (chat, `/tell`, info/group/guild commands, `CHANGE`, `SPLIT`, `SWAP`, `SBN`, `SBB`, `OBP`, `KBUF`, `DSPL`, `/buyvita`, `/buymana`, GM commands) are untouched.

**Step 4:** Run the filter → PASS; run the full suite.

**Step 5: Commit**

```bash
git add Goose Goose.Tests
git commit -m "feat(revive): block movement, combat, items and NPC interaction while dead"
```

---

### Task 6: `RSPN` respawn request and respawn on logout

**Files:**
- Create: `Goose/Events/RespawnRequestEvent.cs`
- Modify: `Goose/EventHandler.cs:159` (packet table)
- Modify: `Goose/Events/LogoutEvent.cs:42` (in-game `else` branch)
- Test: `Goose.Tests/Revive/RespawnRequestTests.cs` (create)

**Step 1: Write the failing tests**

```csharp
using Xunit;

namespace Goose.Tests.Revive;

public class RespawnRequestTests
{
    [Fact]
    public void Rspn_while_dead_respawns()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);
        kit.Kill(p);

        Assert.True(kit.Fixture.RunCommand(p, "RSPN"));

        Assert.False(p.IsDead);
        Assert.Equal(kit.BoundMap.ID, p.MapID);
    }

    [Fact]
    public void Rspn_while_alive_is_ignored()
    {
        using var kit = new ReviveTestKit();
        var p = kit.PlayerAt(5, 5);

        Assert.True(kit.Fixture.RunCommand(p, "RSPN"));

        Assert.Same(kit.Map, p.Map);
        Assert.Equal(5, p.MapX);
        Assert.Equal(100, p.CurrentHP);
    }
}
```

**Step 2:** Run `--filter FullyQualifiedName~RespawnRequestTests` → FAIL (`RunCommand` returns false: unknown packet).

**Step 3: Implement**

`Goose/Events/RespawnRequestEvent.cs`:

```csharp
namespace Goose.Events
{
    public class RespawnRequestEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready || !this.Player.IsDead) return;

            this.Player.Respawn(world);
        }
    }
}
```

`EventHandler._SeedCommands`, after `("STC", ...)`:

```csharp
                ("RSPN", Open(typeof(RespawnRequestEvent))),
```

(No other key starts with `RS`; `RPU`/`RC` diverge at the second character.)

`LogoutEvent`, first statement of the final `else` branch (line 42, before `if (player.Map is not null)`):

```csharp
                if (player.IsDead) player.Respawn(world);
```

A cross-map respawn leaves `player.Map` null and `MapID/MapX/MapY` at the respawn point (`Player.cs:1569-1586`), so the existing `if (player.Map is not null)` skip and `SaveToDatabase` save the respawn location. Logout has no unit test (it calls `SaveToDatabase`, which needs a DB); it is covered by the Part 2 manual check.

**Step 4:** Run the filter → PASS; run the full suite.

**Step 5: Commit**

```bash
git add Goose Goose.Tests
git commit -m "feat(revive): RSPN respawn request and respawn on logout while dead"
```

---

### Task 7: Arena opts out of revive

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Arena/ArenaMap.csx` (after `OnPlayerDied`, line 45)
- Test: `Goose.Tests/Arena/ArenaMapTests.cs`

**Step 1: Write the failing test** (add to `ArenaMapTests`):

```csharp
    [Fact]
    public void Arena_does_not_allow_revive()
    {
        var script = kit.AttachMapScript();
        var p1 = kit.LobbyPlayer(70001, "P1");

        Assert.False(script.Object.AllowsRevive(kit.ArenaMap, p1, p1, kit.World));
    }
```

**Step 2:** Run `--filter FullyQualifiedName~ArenaMapTests` → `Arena_does_not_allow_revive` FAILS (base returns true).

**Step 3: Implement** in `ArenaMap.csx`:

```csharp
    public override bool AllowsRevive(Map map, Player player, ICharacter killer, GameWorld world) => false;
```

**Step 4:** Run `--filter FullyQualifiedName~Arena` → all PASS, including `Death_eliminates_and_respawns_in_lobby` (now running with the default `ReviveTimeout = 60`, proving the opt-out path). Then run the whole project and the integration project:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Both green (integration covers the edited `DimensionMap.csx` compiling).

**Step 5: Commit**

```bash
git add Goose Goose.Tests
git commit -m "feat(revive): arena keeps instant respawn"
```

---

## Done criteria

- All `Goose.Tests` and `Goose.IntegrationTests` green.
- Spell data note for the owner: create a Revive spell effect with `effect_type = Revive` (22), `target_type = 0`, `spell_effected = 4` (Player), `works_not_in_pvp = TRUE` (columns per `CsvToSql/CsvToSql.Core/SpellEffectsCsvToSql.cs`) (plus animation/cost) in the spreadsheet. Out of scope for this plan's code.
- Part 2 (client) consumes: trailing `RespawnSeconds` on player MKC/CHP, base body while dead, `RSPN`.
