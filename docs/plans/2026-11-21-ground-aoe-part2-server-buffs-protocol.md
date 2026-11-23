# Ground AoE Zones — Part 2: Zone Buffs and Protocol Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Zones can hold a buff on whoever stands on their active tiles (removed the moment they leave),
the server tells clients about zones with `ZADD`/`ZDEL`, the spellbook slot packet carries the shape
needed for a client preview, and a ground-targetable spell can be cast on a tile with `CAST slot,0,x,y`.

**Architecture:** `Buff` gains `SourceZone`. Zone buffs never get an expiry event. One method,
`GroundZones.RecheckBuffs(character)`, works out which zone buffs a character *should* have from their
tile and reconciles that with what they hold; it is called from every place a character's tile or
liveness changes, and for everyone on a zone's tiles when a zone is created or removed. Zone packets go
to the whole map, and to each player as they join it. `Player.CastSpell` gets a tile overload that shares
all of its cost/aether/stun logic.

**Tech Stack:** C# / .NET 10, xunit 2.9.3.

**Design doc:** `docs/plans/2026-11-21-ground-aoe-design.md` (Part 2 of 3). **Requires Part 1**
(`docs/plans/2026-11-21-ground-aoe-part1-server-foundation.md`) to be merged into this branch first; the
line numbers below for files Part 1 touches (`SpellEffect.cs`, `GroundZones.cs`) are given as anchors
("after X"), not numbers.

**Worktree:** `/home/hayden/code/illutiagooseserver/.worktrees/ground-aoe`, branch `ground-aoe`.

**House rule (AGENTS.md):** no new comments or doc strings unless the *why* is non-obvious.

---

## APIs verified

| API | Location | Notes |
|---|---|---|
| `Buff` | `Goose/Buff.cs:9-27` | `GetDurations` at 19-26 feeds the buff bar |
| `Player.AddBuff(buff, world, refreshbar, updateCharacter)` | `Goose/Player.cs:2526-2665` | renew-or-skip loop 2545-2561; expiry scheduled when `Duration > 0` at 2565-2575 |
| `Player.RemoveBuff(buff, world, refreshbar, updateCharacter)` | `Goose/Player.cs:2808-…` | removes the expire event if present |
| `NPC.AddBuff` / `RemoveBuff` | `Goose/NPC.cs:1741-1840`, `:1902` | expiry at 1762-1772 |
| Death removes non-item buffs | `Goose/Player.cs:2306-2314` (`RemoveDeathBuffs`) | zone buffs are non-item → removed for free |
| Logout removes non-item buffs | `Goose/Events/LogoutEvent.cs:81-91` | same |
| NPC death / despawn remove all buffs | `Goose/NPC.cs:1353-1363`, `:821-837` | |
| `KillBuffEvent` | `Goose/Events/KillBuffEvent.cs:31-39` | gate on `BuffCanBeRemoved` at 37 |
| `Player.MoveTo` | `Goose/Player.cs:1396-1410` | `SetCharacter` at 1407-1408 |
| `Player.WarpTo` same-map branch | `Goose/Player.cs:1525-1543` | other-map branch 1571-1589 calls `Map.RemovePlayer` |
| `DoneLoadingMapEvent` → `Map.AddPlayer` after placement, state `Ready` | `Goose/Events/DoneLoadingMapEvent.cs:38-44`, `:101` | covers login, cross-map warp and respawn |
| `Map.AddPlayer` / `RemovePlayer` | `Goose/Map.cs:197-227` | `Pet.cs:490` also calls `AddPlayer` (pets are `Player`s) |
| `NPC.MoveTo` | `Goose/NPC.cs:562-570` | |
| `NPC.Spawn` | `Goose/NPC.cs:722-772` | `SetCharacter` at 746, ends with `AddMoveEvent` |
| `NPC.Hide` / `Reveal` | `Goose/NPC.cs:776-803` | |
| `Player.Revive` | `Goose/Player.cs:2262-2275` | |
| `SpellEffect.CastSpell` Snare is NPC-only | `Goose/SpellEffect.cs:1103` | zone buffs keep that rule |
| `SpellEffect.CanCastSpell` immunities (`CanBeSlowed`, `CanBeStunned`, `CanBeRooted`) | `Goose/SpellEffect.cs:893-917` | checked against the *buff* effect |
| `SpellHandler.LoadSpellEffects` post-load linking pass | `Goose/SpellHandler.cs:147-200` | `OnMeleeAttackSpell` resolved at 149-150 |
| Dimension clones rewire references | `Goose/Data/Illutia/Scripts/Global/Dimensions/Spells.csx:103-123` | test pattern `Goose.IntegrationTests/DimensionSpellScriptTests.cs:164-207` |
| Dimension clones grow `TargetSize` by `dim` | `Spells.csx:245`, shape morph `:320-333` | see "Known consequence" below |
| `P.SpellSlot` | `Goose/Packets.cs:700-716` | only caller `Goose/Spellbook.cs:177,181` |
| Existing `SSS` tests that pin the tail | `Goose.Tests/SpellCooldownPacketTests.cs:72-87` | must be updated |
| `GameWorld.SendToMap(map, data)` | `Goose/GameWorld.cs:903-912` | only `Ready` players |
| `PlayerCastSpellEvent` | `Goose/Events/PlayerCastSpellEvent.cs:15-85` | only handles `t.Length == 2` |
| `"CAST"` registration | `Goose/EventHandler.cs:140` | |
| `Player.CastSpell` | `Goose/Player.cs:2381-2510` | effect dispatch at 2433-2456 |
| `TestWorldFixture.RunCommand(player, packet)` | `TestSupport/TestWorldFixture.cs:180-186` | |
| `ReviveTestKit` (`PlayerAt`, `Kill`) | `Goose.Tests/Revive/ReviveTestKit.cs` | real death/revive setup |
| Client tolerates unknown prefixes and extra `SSS` fields | `Goose2ClientGodot/Scripts/Network/PacketManager.cs:46-71`, `Packets/SpellbookSlotPacket.cs:33-48` | server can ship before the client |
| No client prefix starts with `Z` | grep of `Prefix { get; } =` in `Goose2ClientGodot/Scripts` | `ZADD`/`ZDEL` are free |

### Decisions taken while planning (differences from the design doc)

1. **Zone buffs can't be clicked off.** The design said "ignore `buff_removable`; it comes back on the
   next step". Blocking the click in `KillBuffEvent` is simpler and avoids a buff that flickers back.
2. **Allowed zone buff types are `Buff`, `TickBuff` and `Snare`.** Anything else (including Stun and Root)
   rejects the zone at load. `TickBuff` zone buffs show their stats but don't replay their tick animation,
   because `BuffTickEvent` only runs for buffs with an expiry event (`BuffTickEvent.cs:14`).
3. **A zone doesn't replace a timed copy of the same buff.** If the character already has that buff from
   an ordinary cast (or anything in its stacking lists), the zone leaves it alone. When the timed copy
   expires, the zone copy is added on the character's next step or the next zone change on their tile.
4. **Who gets the buff:** the character must pass *both* the zone effect's `CanCastSpell` (who the zone
   affects) and the buff effect's `CanCastSpell` (NPC immunities such as `CanBeSlowed`), and Snare only
   applies to NPCs.

5. **Test setup note:** player-on-player effects only apply on non-PvP maps when `WorksNotInPVP` is set
   (`SpellEffect.cs:885`), and NPC templates default to `CanBeSlowed = false` (`NPCTemplate.cs:146`). The
   tests below set both where they need them.

### Known consequence to raise with the user

Dimension clones grow `TargetSize` by `dim` (`Spells.csx:245`). A 3×3 base zone becomes 15×15 at
dimension 6. This plan doesn't change that; flag it in the final summary.

---

### Task 1: Link and validate zone buffs

**Files:**
- Modify: `Goose/SpellEffect.cs` (property + copy ctor)
- Modify: `Goose/SpellHandler.cs` (post-load pass, new check)
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Spells.csx:103-123`
- Test: `Goose.Tests/Spells/GroundZoneValidationTests.cs`, `Goose.IntegrationTests/GroundZoneLoadTests.cs`,
  `Goose.IntegrationTests/DimensionSpellScriptTests.cs`

**Step 1: Write the failing tests**

Append to `Goose.Tests/Spells/GroundZoneValidationTests.cs`:

```csharp
    [Fact]
    public void A_zone_without_a_buff_link_is_fine()
    {
        Assert.Null(SpellHandler.ZoneBuffProblem(Zone(), null));
    }

    [Fact]
    public void An_unknown_zone_buff_is_rejected()
    {
        Assert.NotNull(SpellHandler.ZoneBuffProblem(Zone(e => e.ZoneBuffEffectID = 99), null));
    }

    [Theory]
    [InlineData(SpellEffect.EffectTypes.Buff, true)]
    [InlineData(SpellEffect.EffectTypes.TickBuff, true)]
    [InlineData(SpellEffect.EffectTypes.Snare, true)]
    [InlineData(SpellEffect.EffectTypes.Stun, false)]
    [InlineData(SpellEffect.EffectTypes.Root, false)]
    [InlineData(SpellEffect.EffectTypes.Formula, false)]
    [InlineData(SpellEffect.EffectTypes.GroundZone, false)]
    public void Only_buff_snare_and_tickbuff_can_be_zone_buffs(SpellEffect.EffectTypes type, bool allowed)
    {
        var buff = new SpellEffect { ID = 99, EffectType = type };

        var problem = SpellHandler.ZoneBuffProblem(Zone(e => e.ZoneBuffEffectID = 99), buff);

        Assert.Equal(allowed, problem is null);
    }
```

Append to `Goose.IntegrationTests/GroundZoneLoadTests.cs`:

```csharp
    [Fact]
    public void Links_zone_buffs_and_drops_zones_with_a_bad_one()
    {
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, effect_duration) " +
               "VALUES (10, 'Ward', 6, 1, 30)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, effect_duration) " +
               "VALUES (11, 'Stun', 6, 7, 3)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_buff_effect_id) VALUES (12, 'Sanctuary', 6, 23, 8, 5, 1, 10)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_buff_effect_id) VALUES (13, 'Stun Field', 6, 23, 8, 5, 1, 11)");
        Insert("INSERT INTO spell_effects (spell_effect_id, spell_effect_name, spell_effected, effect_type, " +
               "effect_duration, target_type, target_size, zone_buff_effect_id) VALUES (14, 'Ghost Field', 6, 23, 8, 5, 1, 404)");

        Load();

        Assert.Same(world.SpellHandler.GetSpellEffect(10), world.SpellHandler.GetSpellEffect(12)!.ZoneBuffEffect);
        Assert.Null(world.SpellHandler.GetSpellEffect(13));
        Assert.Null(world.SpellHandler.GetSpellEffect(14));
    }
```

Append to `Goose.IntegrationTests/DimensionSpellScriptTests.cs`:

```csharp
    [Fact]
    public void Zone_buffs_point_at_the_same_dimensions_buff()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(9, "Ward", e => e.EffectType = SpellEffect.EffectTypes.Buff);
            f.AddBaseSpellEffect(42, "Sanctuary", e =>
            {
                e.EffectType = SpellEffect.EffectTypes.GroundZone;
                e.ZoneBuffEffectID = 9;
                e.ZoneBuffEffect = f.World.SpellHandler.GetSpellEffect(9);
            });
        });

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3)!;

        Assert.Equal(9 + Offset * 3, dim3.ZoneBuffEffectID);
        Assert.Same(fixture.World.SpellHandler.GetSpellEffect(9 + Offset * 3), dim3.ZoneBuffEffect);
        Assert.Same(fixture.World.SpellHandler.GetSpellEffect(9),
                    fixture.World.SpellHandler.GetSpellEffect(42)!.ZoneBuffEffect);
    }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~GroundZoneValidationTests`
Expected: build error — `ZoneBuffProblem` and `SpellEffect.ZoneBuffEffect` don't exist.

**Step 3: Implement**

`Goose/SpellEffect.cs`, after `public bool ZoneOverlaps { get; set; }`:

```csharp
        public SpellEffect? ZoneBuffEffect { get; set; }
```

and in the copy ctor after `this.ZoneOverlaps = other.ZoneOverlaps;`:

```csharp
            this.ZoneBuffEffect = other.ZoneBuffEffect;
```

`Goose/SpellHandler.cs` — add next to `GroundZoneProblem`:

```csharp
        internal static string? ZoneBuffProblem(SpellEffect zone, SpellEffect? buff)
        {
            if (zone.ZoneBuffEffectID == 0) return null;
            if (buff is null) return "unknown zone_buff_effect_id " + zone.ZoneBuffEffectID;
            if (buff.EffectType is not (SpellEffect.EffectTypes.Buff or SpellEffect.EffectTypes.TickBuff or SpellEffect.EffectTypes.Snare))
                return "zone buffs must be Buff, TickBuff or Snare, not " + buff.EffectType;
            return null;
        }
```

In the post-load pass (`foreach (var s in this.effects.Values)` at `:147`), after the
`s.OnMeleeHitSpell = …` line:

```csharp
                s.ZoneBuffEffect = this.GetSpellEffect(s.ZoneBuffEffectID);
```

After that `foreach` loop closes (still inside the `Execute` lambda):

```csharp
            foreach (var bad in this.effects.Values
                         .Where(e => e.EffectType == SpellEffect.EffectTypes.GroundZone)
                         .Select(e => (Effect: e, Problem: ZoneBuffProblem(e, e.ZoneBuffEffect)))
                         .Where(p => p.Problem is not null)
                         .ToList())
            {
                log.Warn("spell effect {0} ({1}): {2}", bad.Effect.Name, bad.Effect.ID, bad.Problem);
                this.effects.Remove(bad.Effect.ID);
            }
```

`.ToList()` is required: removing from `this.effects` while enumerating it throws.

`Spells.csx`, inside the `for (int dim = 1; …)` melee rewiring loop after the `OnMeleeHitSpellID`
assignment. The guard matters: without it, `0 + Offset * dim` could resolve a real effect.

```csharp
                if (basic.ZoneBuffEffectID != 0)
                {
                    clone.ZoneBuffEffect = world.SpellHandler.GetSpellEffect(
                        basic.ZoneBuffEffectID + Offset * dim);
                    clone.ZoneBuffEffectID = clone.ZoneBuffEffect == null
                        ? 0 : clone.ZoneBuffEffect.ID;
                }
```

**Step 4: Run the tests**

```bash
dotnet test Goose.Tests --filter "FullyQualifiedName~GroundZoneValidationTests|FullyQualifiedName~SpellCloneTests"
dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~GroundZoneLoadTests|FullyQualifiedName~DimensionSpellScriptTests"
```

Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/SpellEffect.cs Goose/SpellHandler.cs Goose/Data/Illutia/Scripts/Global/Dimensions/Spells.csx \
  Goose.Tests/Spells/GroundZoneValidationTests.cs Goose.IntegrationTests/GroundZoneLoadTests.cs \
  Goose.IntegrationTests/DimensionSpellScriptTests.cs
git commit -m "feat(spells): link and validate ground zone buffs"
```

---

### Task 2: Zone-held buffs never expire and can't be clicked off

**Files:**
- Modify: `Goose/Buff.cs`
- Modify: `Goose/Player.cs:2565` and `Goose/NPC.cs:1762` (expiry guard)
- Modify: `Goose/Events/KillBuffEvent.cs:37`
- Create: `Goose.Tests/GroundZones/ZoneHeldBuffTests.cs`

**Step 1: Write the failing tests**

```csharp
namespace Goose.Tests.GroundZones;

public class ZoneHeldBuffTests
{
    private static (GroundZoneTestKit kit, GroundZone zone, SpellEffect ward) Setup()
    {
        var kit = new GroundZoneTestKit();
        var ward = kit.Fixture.AddBaseSpellEffect(50, "Ward", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Effected = SpellEffect.SpellEffected.NPCPlayer;
            e.Duration = 30;
            e.BuffCanBeRemoved = true;
            e.Stats = new AttributeSet { AC = 5 };
        });
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => e.HPFormula = "0"), caster, 10, 10, kit.World);
        return (kit, zone, ward);
    }

    private static Buff ZoneBuff(ICharacter target, GroundZone zone, SpellEffect effect, GameWorld world) => new()
    {
        Target = target, Caster = zone.Caster, SpellEffect = effect, TimeCast = world.TimeNow, SourceZone = zone,
    };

    [Fact]
    public void A_player_zone_buff_gets_no_expiry_event()
    {
        var (kit, zone, ward) = Setup();
        using var _ = kit;
        var p = kit.PlayerAt(10, 10, "Walker");
        var buff = ZoneBuff(p, zone, ward, kit.World);

        p.AddBuff(buff, kit.World);

        Assert.Contains(buff, p.Buffs);
        Assert.Null(buff.BuffExpireEvent);
        Assert.Equal((0, 0), buff.GetDurations(kit.World));
    }

    [Fact]
    public void An_npc_zone_buff_gets_no_expiry_event()
    {
        var (kit, zone, ward) = Setup();
        using var _ = kit;
        var npc = kit.NpcAt(10, 10);
        var buff = ZoneBuff(npc, zone, ward, kit.World);

        npc.AddBuff(buff, kit.World);

        Assert.Contains(buff, npc.Buffs);
        Assert.Null(buff.BuffExpireEvent);
    }

    [Fact]
    public void An_ordinary_buff_still_expires()
    {
        var (kit, _, ward) = Setup();
        using var _k = kit;
        var p = kit.PlayerAt(10, 10, "Walker");
        var buff = new Buff { Target = p, Caster = p, SpellEffect = ward, TimeCast = kit.World.TimeNow };

        p.AddBuff(buff, kit.World);

        Assert.NotNull(buff.BuffExpireEvent);
    }

    [Fact]
    public void A_zone_buff_can_not_be_clicked_off()
    {
        var (kit, zone, ward) = Setup();
        using var _ = kit;
        var p = kit.PlayerAt(10, 10, "Walker");
        p.AddBuff(ZoneBuff(p, zone, ward, kit.World), kit.World);

        kit.Fixture.RunCommand(p, "KBUF1");

        Assert.Single(p.Buffs);
    }
}
```

`KBUF` is registered at `Goose/EventHandler.cs:149`.

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ZoneHeldBuffTests`
Expected: build error — `Buff.SourceZone` doesn't exist.

**Step 3: Implement**

`Goose/Buff.cs` — add after `public Event? BuffExpireEvent { get; set; }`:

```csharp
        public GroundZone? SourceZone { get; set; }
```

and change the first line of `GetDurations`:

```csharp
            long totalMs = this.SourceZone is null && this.SpellEffect.Duration > 0 ? this.SpellEffect.Duration * 1000 : 0;
```

`Goose/Player.cs:2565` and `Goose/NPC.cs:1762` — both read `if (buff.SpellEffect.Duration > 0)`.
Change each to:

```csharp
            if (buff.SpellEffect.Duration > 0 && buff.SourceZone is null)
```

`Goose/Events/KillBuffEvent.cs:37`:

```csharp
                if (!buff.SpellEffect.BuffCanBeRemoved || buff.SourceZone is not null) return;
```

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~ZoneHeldBuffTests|FullyQualifiedName~Buff"`
Expected: PASS (new tests plus the existing buff test classes).

**Step 5: Commit**

```bash
git add Goose/Buff.cs Goose/Player.cs Goose/NPC.cs Goose/Events/KillBuffEvent.cs Goose.Tests/GroundZones/ZoneHeldBuffTests.cs
git commit -m "feat(spells): zone-held buffs don't expire and can't be clicked off"
```

---

### Task 3: `RecheckBuffs`, driven by zone creation and removal

**Files:**
- Modify: `Goose/GroundZones.cs`
- Create: `Goose.Tests/GroundZones/ZoneBuffTests.cs`

**Step 1: Write the failing tests**

```csharp
namespace Goose.Tests.GroundZones;

public class ZoneBuffTests
{
    private static SpellEffect Buff(GroundZoneTestKit kit, int id, SpellEffect.EffectTypes type = SpellEffect.EffectTypes.Buff) =>
        kit.Fixture.AddBaseSpellEffect(id, "Buff " + id, e =>
        {
            e.EffectType = type;
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.Duration = 30;
        });

    private static SpellEffect BuffZone(GroundZoneTestKit kit, int id, SpellEffect buff, Action<SpellEffect>? configure = null) =>
        kit.Zone(id, e =>
        {
            e.HPFormula = "0";
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.ZoneBuffEffectID = buff.ID;
            e.ZoneBuffEffect = buff;
            configure?.Invoke(e);
        });

    private static ICharacter Caster(GroundZoneTestKit kit)
    {
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        return caster;
    }

    private static Buff? Held(ICharacter c, SpellEffect effect) =>
        c.Buffs.SingleOrDefault(b => b.SpellEffect == effect && b.SourceZone is not null);

    [Fact]
    public void Creating_a_zone_buffs_everyone_on_it()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var npc = kit.NpcAt(11, 11);
        var outside = kit.NpcAt(13, 10);

        var zone = kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        Assert.Same(zone, Held(p, ward)!.SourceZone);
        Assert.NotNull(Held(npc, ward));
        Assert.Null(Held(outside, ward));
    }

    [Fact]
    public void Expiry_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var zone = kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        kit.Map.GroundZones.Tick(zone, zone.ExpiresAt, kit.World);

        Assert.Null(Held(p, ward));
    }

    [Fact]
    public void A_newer_zone_with_another_buff_swaps_it()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var haste = Buff(kit, 51);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        kit.Map.GroundZones.Create(BuffZone(kit, 2, haste), Caster(kit), 10, 10, kit.World);

        Assert.Null(Held(p, ward));
        Assert.NotNull(Held(p, haste));
    }

    [Fact]
    public void A_newer_zone_with_the_same_buff_takes_it_over_without_reapplying()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);
        var original = Held(p, ward)!;

        var newer = kit.Map.GroundZones.Create(BuffZone(kit, 2, ward), Caster(kit), 10, 10, kit.World);

        Assert.Same(original, Held(p, ward));
        Assert.Same(newer, original.SourceZone);
    }

    [Fact]
    public void Overlapping_zones_keep_the_buff_until_the_last_one_ends()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var first = kit.Map.GroundZones.Create(BuffZone(kit, 1, ward, e => e.ZoneOverlaps = true), Caster(kit), 10, 10, kit.World);
        var second = kit.Map.GroundZones.Create(BuffZone(kit, 2, ward, e => e.ZoneOverlaps = true), Caster(kit), 10, 10, kit.World);

        kit.Map.GroundZones.Tick(second, second.ExpiresAt, kit.World);
        Assert.Same(first, Held(p, ward)!.SourceZone);

        kit.Map.GroundZones.Tick(first, first.ExpiresAt, kit.World);
        Assert.Null(Held(p, ward));
    }

    [Fact]
    public void Characters_the_zone_cannot_affect_get_nothing()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");

        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward, e => e.Effected = SpellEffect.SpellEffected.NPC),
            Caster(kit), 10, 10, kit.World);

        Assert.Empty(p.Buffs);
    }

    [Fact]
    public void Snare_zones_only_snare_npcs()
    {
        using var kit = new GroundZoneTestKit();
        var tar = Buff(kit, 50, SpellEffect.EffectTypes.Snare);
        var p = kit.PlayerAt(10, 10, "Walker");
        var npc = kit.Fixture.SpawnNpc(kit.Map, 11, 10,
            kit.Fixture.AddNpcTemplate(501, "Slowable", t => { t.AggroRange = 0; t.CanBeSlowed = true; }));

        kit.Map.GroundZones.Create(BuffZone(kit, 1, tar), Caster(kit), 10, 10, kit.World);

        Assert.Empty(p.Buffs);
        Assert.NotNull(Held(npc, tar));
    }

    [Fact]
    public void A_timed_copy_of_the_buff_is_left_alone()
    {
        using var kit = new GroundZoneTestKit();
        var ward = Buff(kit, 50);
        var p = kit.PlayerAt(10, 10, "Walker");
        var timed = new Buff { Target = p, Caster = p, SpellEffect = ward, TimeCast = kit.World.TimeNow };
        p.AddBuff(timed, kit.World);

        kit.Map.GroundZones.Create(BuffZone(kit, 1, ward), Caster(kit), 10, 10, kit.World);

        Assert.Same(timed, Assert.Single(p.Buffs));
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ZoneBuffTests`
Expected: FAIL — nothing applies zone buffs yet (`Held` returns null).

**Step 3: Implement**

In `Goose/GroundZones.cs` add:

```csharp
        public void RecheckBuffs(ICharacter character, GameWorld world)
        {
            var wanted = this.WantedBuffs(character);

            foreach (var held in character.Buffs.Where(b => b.SourceZone?.Map == this.map).ToList())
            {
                if (wanted.Remove(held.SpellEffect, out var zone)) held.SourceZone = zone;
                else character.RemoveBuff(held, world);
            }

            foreach (var (effect, zone) in wanted)
            {
                if (character.Buffs.Any(b => b.SpellEffect == effect ||
                                             effect.BuffStacksOver.Contains(b.SpellEffect) ||
                                             effect.BuffDoesntStackOver.Contains(b.SpellEffect)))
                    continue;

                character.AddBuff(new Buff
                {
                    Target = character,
                    Caster = zone.Caster,
                    SpellEffect = effect,
                    TimeCast = world.TimeNow,
                    CasterMPAtCast = zone.Caster.CurrentMP,
                    SourceZone = zone,
                }, world);
            }
        }

        private Dictionary<SpellEffect, GroundZone> WantedBuffs(ICharacter character)
        {
            var wanted = new Dictionary<SpellEffect, GroundZone>();
            if (character.Map != this.map || !IsAlive(character)) return wanted;

            int x = character.MapX;
            int y = character.MapY;
            if (this.map.GetCharacterAt(x, y) != character) return wanted;

            foreach (var zone in this.zones)
            {
                if (zone.Effect.ZoneBuffEffect is not SpellEffect buff) continue;
                if (!zone.Tiles.Contains((x, y)) || !this.IsActiveAt(zone, x, y)) continue;
                if (!zone.Effect.CanCastSpell(zone.Caster, character, this.map)) continue;
                if (!buff.CanCastSpell(zone.Caster, character, this.map)) continue;
                if (buff.EffectType == SpellEffect.EffectTypes.Snare && character is not NPC) continue;

                wanted[buff] = zone;
            }

            return wanted;
        }

        private void RecheckTiles(GroundZone zone, GameWorld world)
        {
            foreach (var (x, y) in zone.Tiles)
                if (this.map.GetCharacterAt(x, y) is ICharacter character)
                    this.RecheckBuffs(character, world);
        }
```

`zones` is in creation order, so for two zones with the same buff the newer one wins `wanted[buff]` and
becomes the `SourceZone`.

Call `RecheckTiles`:
- in `Create`, after the ownership loop and before `this.Apply(zone, world);`
- in `Remove`, as its last statement, for overlapping zones too — so change `Remove` to:

```csharp
        private void Remove(GroundZone zone, GameWorld world)
        {
            this.zones.Remove(zone);

            if (!zone.Overlaps)
            {
                foreach (var tile in zone.Tiles)
                {
                    if (this.owners.GetValueOrDefault(tile) != zone) continue;

                    var next = this.zones.LastOrDefault(z => !z.Overlaps && z.Tiles.Contains(tile));
                    if (next is null) this.owners.Remove(tile);
                    else this.owners[tile] = next;
                }
            }

            this.RecheckTiles(zone, world);
        }
```

and update the call in `Tick` to `this.Remove(zone, world);`.

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~ZoneBuffTests|FullyQualifiedName~GroundZonesTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/GroundZones.cs Goose.Tests/GroundZones/ZoneBuffTests.cs
git commit -m "feat(spells): ground zones hold buffs on whoever stands in them"
```

---

### Task 4: Recheck zone buffs wherever a character's tile or liveness changes

**Files:**
- Modify: `Goose/Player.cs` — `MoveTo` (after `:1408`), `WarpTo` same-map branch (after `:1543`),
  `Revive` (end of `:2262-2275`)
- Modify: `Goose/NPC.cs` — `MoveTo` (after `:570`), `Spawn` (before the final `AddMoveEvent`), `Hide`
  (end), `Reveal` (after `SetCharacter`)
- Modify: `Goose/Map.cs` — `AddPlayer` (after `this.players.Add`), `RemovePlayer` (after `this.players.Remove`)
- Create: `Goose.Tests/GroundZones/ZoneBuffMovementTests.cs`

**Step 1: Write the failing tests**

```csharp
using Goose.Tests.Revive;

namespace Goose.Tests.GroundZones;

public class ZoneBuffMovementTests
{
    private static (SpellEffect ward, SpellEffect zone) Effects(Goose.Testing.TestWorldFixture fixture)
    {
        var ward = fixture.AddBaseSpellEffect(50, "Ward", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.Duration = 30;
        });
        var zone = fixture.AddBaseSpellEffect(1, "Sanctuary", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.GroundZone;
            e.TargetType = SpellEffect.TargetTypes.Area;
            e.TargetSize = 1;
            e.Duration = 10;
            e.ZoneTickMs = 1000;
            e.Effected = SpellEffect.SpellEffected.SelfNPCPlayer;
            e.WorksNotInPVP = true;
            e.HPFormula = "0";
            e.MPFormula = "0";
            e.WorksInPVP = true;
            e.ZoneBuffEffectID = ward.ID;
            e.ZoneBuffEffect = ward;
        });
        return (ward, zone);
    }

    private static Player Caster(Goose.Testing.TestWorldFixture fixture, Map map)
    {
        var caster = fixture.PlayerOn(map, 30, 30);
        caster.Facing = Direction.Down;
        return caster;
    }

    private static bool HasZoneBuff(ICharacter c, SpellEffect ward) =>
        c.Buffs.Any(b => b.SpellEffect == ward && b.SourceZone is not null);

    [Fact]
    public void Walking_in_and_out_adds_and_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var p = kit.PlayerAt(12, 10, "Walker");
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        Assert.False(HasZoneBuff(p, ward));

        p.MoveTo(kit.World, 11, 10);
        Assert.True(HasZoneBuff(p, ward));

        p.MoveTo(kit.World, 12, 10);
        Assert.False(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Npcs_walking_in_and_out_are_tracked_too()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var npc = kit.NpcAt(12, 10);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        npc.MoveTo(kit.World, 11, 10);
        Assert.True(HasZoneBuff(npc, ward));

        npc.MoveTo(kit.World, 12, 10);
        Assert.False(HasZoneBuff(npc, ward));
    }

    [Fact]
    public void A_same_map_warp_out_of_the_zone_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        p.WarpTo(kit.World, kit.Map, 25, 25);

        Assert.False(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Leaving_the_map_removes_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var town = kit.Fixture.AddBaseMap(2, "Town", 20, 20);
        var p = kit.PlayerAt(10, 10, "Walker");
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        p.WarpTo(kit.World, town, 5, 5);

        Assert.False(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Joining_the_map_on_a_zone_tile_applies_the_buff()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        var p = kit.Fixture.CommandPlayerOn(kit.Map, 10, 10, "Arriving");
        kit.Map.SetCharacter(p, 10, 10);

        kit.Map.AddPlayer(p, kit.World);

        Assert.True(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Death_removes_the_buff_and_revive_brings_it_back()
    {
        using var kit = new ReviveTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var p = kit.PlayerAt(10, 10);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);
        Assert.True(HasZoneBuff(p, ward));

        kit.Kill(p);
        Assert.True(p.IsDead);
        Assert.False(HasZoneBuff(p, ward));

        kit.Map.GroundZones.RecheckBuffs(p, kit.World);
        Assert.False(HasZoneBuff(p, ward));

        p.Revive(p, kit.World);
        Assert.True(HasZoneBuff(p, ward));
    }

    [Fact]
    public void Hidden_npcs_lose_the_buff_and_get_it_back_when_revealed_inside()
    {
        using var kit = new GroundZoneTestKit();
        var (ward, zone) = Effects(kit.Fixture);
        var npc = kit.NpcAt(10, 10);
        kit.Map.GroundZones.Create(zone, Caster(kit.Fixture, kit.Map), 10, 10, kit.World);

        npc.Hide(kit.World);
        Assert.False(HasZoneBuff(npc, ward));

        npc.Reveal(kit.World, 11, 10);
        Assert.True(HasZoneBuff(npc, ward));
    }
}
```

The death test assumes `ReviveTestKit.Kill` leaves the corpse on its tile (revive is enabled with
`reviveTimeout = 60`). If `p.IsDead` is false after `Kill`, read `Goose.Tests/Revive/ReviveDeathTests.cs`
for the setup it uses and mirror it — don't weaken the assertions.

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ZoneBuffMovementTests`
Expected: FAIL on the movement, warp, join, revive and reveal assertions.

**Step 3: Implement**

Add this exact line at each hook point (`this.Map` for `Player`/`NPC` methods, `this` inside `Map`):

```csharp
            this.Map.GroundZones.RecheckBuffs(this, world);
```

- `Player.MoveTo` — after the `if (!IsGMInvisible) this.Map.SetCharacter(this, …)` block (`:1407-1408`).
- `Player.WarpTo` — same-map branch, after the `if (!IsGMInvisible) { PlaceCharacter; SetCharacter }`
  block (`:1537-1542`), before `world.Send(this, P.SetYourPosition(this));`.
- `Player.Revive` — last line of the method.
- `NPC.MoveTo` — after `this.Map.SetCharacter(this, this.MapX, this.MapY);` (`:570`).
- `NPC.Spawn` — immediately before the final `this.AddMoveEvent(world);`.
- `NPC.Hide` — last line, after `this.Hidden = true;`.
- `NPC.Reveal` — after `this.Hidden = false;`.

In `Map.AddPlayer` and `Map.RemovePlayer`, directly after `this.players.Add(player);` /
`this.players.Remove(player);`:

```csharp
            this.GroundZones.RecheckBuffs(player, world);
```

In `RemovePlayer` the player is already off their tile (`WarpTo` clears it at `:1575-1579`), so the recheck
removes every zone buff from this map. In `AddPlayer` they are already placed (`DoneLoadingMapEvent.cs:38-44`).

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~GroundZones"`
Expected: PASS.

Run: `dotnet test Goose.Tests`
Expected: PASS. `RecheckBuffs` is a no-op on maps with no zones, so movement tests elsewhere are unaffected.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/NPC.cs Goose/Map.cs Goose.Tests/GroundZones/ZoneBuffMovementTests.cs
git commit -m "feat(spells): recheck zone buffs when characters move, warp, die or revive"
```

---

### Task 5: `ZADD` / `ZDEL`

**Files:**
- Modify: `Goose/Packets.cs` (after `SpellTile`, `:51-58`)
- Modify: `Goose/GroundZones.cs` (`Create`, `Remove`, new `SendAll`)
- Modify: `Goose/Map.cs` (`AddPlayer`)
- Create: `Goose.Tests/GroundZones/ZonePacketTests.cs`

**Step 1: Write the failing tests**

```csharp
namespace Goose.Tests.GroundZones;

public class ZonePacketTests
{
    private static List<string> Packets(Goose.Testing.TestWorldFixture.CapturingPlayer p) =>
        string.Concat(p.Sent).Split('\x1').Where(s => s.Length > 0).ToList();

    [Fact]
    public void ZoneAdd_lists_tiles_row_by_row()
    {
        var zone = new GroundZone
        {
            Id = 7,
            Effect = new SpellEffect { Animation = 120, AnimationFile = 4 },
            Tiles = [(11, 10), (10, 11), (10, 10)],
        };

        Assert.Equal("ZADD7,120,4,8000,10,10,11,10,10,11", P.ZoneAdd(zone, 8000));
        Assert.Equal("ZDEL7", P.ZoneDelete(7));
    }

    [Fact]
    public void Creation_and_expiry_are_sent_to_the_whole_map()
    {
        using var kit = new GroundZoneTestKit();
        var near = kit.PlayerAt(11, 10, "Near");
        var far = kit.PlayerAt(39, 39, "Far");
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;

        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => { e.Duration = 8; e.HPFormula = "0"; }),
            caster, 10, 10, kit.World);
        kit.Map.GroundZones.Tick(zone, zone.ExpiresAt, kit.World);

        foreach (var p in new[] { near, far })
        {
            Assert.Contains(Packets(p), s => s.StartsWith("ZADD" + zone.Id + ",") && s.Contains(",8000,"));
            Assert.Contains("ZDEL" + zone.Id, Packets(p));
        }
    }

    [Fact]
    public void A_joining_player_is_told_about_active_zones_with_the_time_left()
    {
        using var kit = new GroundZoneTestKit();
        var caster = kit.Fixture.PlayerOn(kit.Map, 30, 30);
        caster.Facing = Direction.Down;
        var zone = kit.Map.GroundZones.Create(kit.Zone(1, e => { e.Duration = 8; e.HPFormula = "0"; }),
            caster, 10, 10, kit.World);
        var arriving = kit.Fixture.CommandPlayerOn(kit.Map, 20, 20, "Arriving");
        kit.Map.SetCharacter(arriving, 20, 20);

        kit.Map.AddPlayer(arriving, kit.World);

        var add = Assert.Single(Packets(arriving), s => s.StartsWith("ZADD"));
        long remaining = long.Parse(add.Substring(4).Split(',')[3]);
        Assert.InRange(remaining, 7000, 8000);
    }
}
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ZonePacketTests`
Expected: build error — `P.ZoneAdd` / `P.ZoneDelete` don't exist.

**Step 3: Implement**

`Goose/Packets.cs`, after `SpellTile`:

```csharp
        public static Func<GroundZone, long, string> ZoneAdd = (zone, remainingMs) =>
            "ZADD" + zone.Id + "," + zone.Effect.Animation + "," + zone.Effect.AnimationFile + "," + remainingMs + "," +
            string.Join(",", zone.Tiles.OrderBy(t => t.Y).ThenBy(t => t.X).Select(t => t.X + "," + t.Y));

        public static Func<int, string> ZoneDelete = (id) => "ZDEL" + id;
```

`Goose/GroundZones.cs`:
- In `Create`, after `this.zones.Add(zone);`:
  ```csharp
              world.SendToMap(this.map, P.ZoneAdd(zone, effect.Duration * 1000));
  ```
- In `Remove`, after `this.zones.Remove(zone);`:
  ```csharp
              world.SendToMap(this.map, P.ZoneDelete(zone.Id));
  ```
- New method:
  ```csharp
          public void SendAll(Player player, GameWorld world)
          {
              long now = world.TimeNow;
              foreach (var zone in this.zones)
                  world.Send(player, P.ZoneAdd(zone, Math.Max(0, (zone.ExpiresAt - now) * 1000 / world.TimerFrequency)));
          }
  ```

`Goose/Map.cs` `AddPlayer`, after the `RecheckBuffs` line from Task 4:

```csharp
            this.GroundZones.SendAll(player, world);
```

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~GroundZones"`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Packets.cs Goose/GroundZones.cs Goose/Map.cs Goose.Tests/GroundZones/ZonePacketTests.cs
git commit -m "feat(spells): ZADD/ZDEL zone packets"
```

---

### Task 6: Ground-targeting and shape fields on `SSS`

**Files:**
- Modify: `Goose/Packets.cs:700-716`
- Modify: `Goose.Tests/SpellCooldownPacketTests.cs:72-87`

**Step 1: Update and add tests**

In `SpellCooldownPacketTests`, change the two tail assertions to include the three new fields:

```csharp
            Assert.EndsWith("," + spell.Aether + ",0,0,0,0", packet);
```

```csharp
            Assert.EndsWith(",10000,1500,0,0,0", packet);
```

and add:

```csharp
        [Fact]
        public void SpellSlot_AppendsGroundTargetingShapeAndSize()
        {
            var spell = NewSpell();
            spell.GroundTargetable = true;
            spell.SpellEffect.TargetType = SpellEffect.TargetTypes.Area;
            spell.SpellEffect.TargetSize = 2;

            Assert.EndsWith(",10000,0,1,5,2", P.SpellSlot(spell, 1, 0, 0));
        }
```

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~SpellCooldownPacketTests`
Expected: FAIL on the three tail assertions.

**Step 3: Implement**

`P.SpellSlot`:

```csharp
        public static Func<Spell?, int, int, long, string> SpellSlot = (spell, slotId, targetType, remainingMs) =>
        {
            if (spell is null)
            {
                return "SSS" + slotId + ",,0,0,0,0,0,0,0";
            }

            return "SSS" + slotId + "," +
                    spell.Name + "," +
                    spell.SpellEffect.Animation + "," +
                    "1000," +
                    "0," +
                    targetType + "," +
                    spell.Graphic + "," +
                    spell.GraphicFile + "," +
                    spell.Aether + "," +
                    remainingMs + "," +
                    (spell.GroundTargetable ? 1 : 0) + "," +
                    (int)spell.SpellEffect.TargetType + "," +
                    spell.SpellEffect.TargetSize;
        };
```

The empty-slot form is unchanged: the client only reads the slot number from an empty slot
(`SpellbookSlotPacket.cs:35-37`).

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~SpellCooldownPacketTests`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Packets.cs Goose.Tests/SpellCooldownPacketTests.cs
git commit -m "feat(spells): send ground targeting, shape and size in SSS"
```

---

### Task 7: `CAST slot,0,x,y`

**Files:**
- Modify: `Goose/SpellEffect.cs` — `CastGroundZone` signature, new `CastAt`
- Modify: `Goose/Player.cs:2381-2456` — shared private overload, new `CastSpellAt`
- Modify: `Goose/Events/PlayerCastSpellEvent.cs`
- Create: `Goose.Tests/GroundZones/GroundTargetCastTests.cs`

**Step 1: Write the failing tests**

```csharp
namespace Goose.Tests.GroundZones;

public class GroundTargetCastTests
{
    private static GroundZoneTestKit Kit(out TestWorldFixture.CapturingPlayer caster, Action<Spell>? configure = null)
    {
        var kit = new GroundZoneTestKit();
        caster = kit.PlayerAt(10, 10);
        var effect = kit.Zone(1, e => e.HPFormula = "0");
        var spell = kit.Fixture.AddBaseSpell(1, "Fire Patch", effect.ID, s =>
        {
            s.Target = Spell.SpellTargets.Target;
            s.GroundTargetable = true;
            configure?.Invoke(s);
        });
        caster.Spellbook.AddSpell(spell, kit.World);
        return kit;
    }

    [Fact]
    public void Casting_on_a_tile_drops_the_zone_there()
    {
        using var kit = Kit(out var caster);

        kit.Fixture.RunCommand(caster, "CAST1,0,15,12");

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((15, 12), zone.Tiles);
        Assert.Equal(9, zone.Tiles.Count);
    }

    [Fact]
    public void Tile_casts_pay_costs_and_start_the_cooldown()
    {
        using var kit = Kit(out var caster, s => { s.MPStaticCost = 30; s.Aether = 10000; });

        kit.Fixture.RunCommand(caster, "CAST1,0,15,12");
        kit.Fixture.RunCommand(caster, "CAST1,0,16,12");

        Assert.Equal(70, caster.CurrentMP);
        Assert.Single(kit.Map.GroundZones.All);
    }

    [Fact]
    public void Spells_that_are_not_ground_targetable_ignore_tile_casts()
    {
        using var kit = Kit(out var caster, s => s.GroundTargetable = false);

        kit.Fixture.RunCommand(caster, "CAST1,0,15,12");

        Assert.Empty(kit.Map.GroundZones.All);
        Assert.Equal(1, caster.SuspectedMacroCount);
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(41, 12)]
    [InlineData(34, 12)]
    [InlineData(10, 26)]
    public void Tiles_off_the_map_or_out_of_view_are_rejected(int x, int y)
    {
        using var kit = Kit(out var caster);

        kit.Fixture.RunCommand(caster, $"CAST1,0,{x},{y}");

        Assert.Empty(kit.Map.GroundZones.All);
    }

    [Fact]
    public void A_nonzero_target_with_coordinates_is_rejected()
    {
        using var kit = Kit(out var caster);

        kit.Fixture.RunCommand(caster, "CAST1,5,15,12");

        Assert.Empty(kit.Map.GroundZones.All);
    }

    [Fact]
    public void Ordinary_two_part_casts_still_work()
    {
        using var kit = Kit(out var caster);
        var npc = kit.NpcAt(12, 10);

        kit.Fixture.RunCommand(caster, "CAST1," + npc.LoginID);

        var zone = Assert.Single(kit.Map.GroundZones.All);
        Assert.Contains((12, 10), zone.Tiles);
    }
}
```

Add `using Goose.Testing;` at the top for `TestWorldFixture`.

Range: the caster is at (10,10) and `RANGE_X = 24`, `RANGE_Y = 16` (`Map.cs:17-18`), with the same strict
`<` that `GetPlayersInRange` uses (`Map.cs:170-171`). So x=34 (distance 24) and y=26 (distance 16) are out.

**Step 2: Run to verify they fail**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~GroundTargetCastTests`
Expected: build error (`Spell.GroundTargetable` exists from Part 1, but `CastSpellAt` doesn't), then FAIL on the
tile casts once it compiles.

**Step 3: Implement**

`Goose/SpellEffect.cs` — change Part 1's `CastGroundZone(ICharacter caster, ICharacter anchor, GameWorld world)`
to take a map and tile, and update its one call in `Cast()`:

```csharp
            if (this.EffectType == EffectTypes.GroundZone)
                return this.CastGroundZone(caster, target.Map, target.MapX, target.MapY, world);
```

```csharp
        public bool CastAt(ICharacter caster, Map map, int x, int y, GameWorld world)
        {
            if (this.EffectType != EffectTypes.GroundZone) return false;
            if (!this.WorksInPVP && map.CanPVP) return false;

            caster.BreakInvisibility(world);
            return this.CastGroundZone(caster, map, x, y, world);
        }

        private bool CastGroundZone(ICharacter caster, Map map, int x, int y, GameWorld world)
        {
            if (!MapAllowsCasting(caster, map)) return false;

            map.GroundZones.Create(this, caster, x, y, world);

            if (this.DoCastAnimation)
            {
                string packet = P.Cast(caster);
                if (caster is Player player) world.Send(player, packet);
                foreach (var p in map.GetPlayersInRange(caster))
                    world.Send(p, packet);
            }

            return true;
        }
```

`Goose/Player.cs` — rename the body of `public void CastSpell(int spellslot, ICharacter target, GameWorld world)`
into a private overload with an optional tile, and keep the public signature as a one-liner:

```csharp
        public void CastSpell(int spellslot, ICharacter target, GameWorld world)
            => this.CastSpell(spellslot, target, null, world);

        public bool CastSpellAt(int spellslot, int x, int y, GameWorld world)
        {
            Spell? spell = this.Spellbook.GetSlot(spellslot);
            if (spell is null || !spell.GroundTargetable || spell.Target != Spell.SpellTargets.Target ||
                spell.SpellEffect.EffectType != SpellEffect.EffectTypes.GroundZone)
                return false;
            if (x < 1 || x > this.Map.Width || y < 1 || y > this.Map.Height) return false;
            if (Math.Abs(x - this.MapX) >= Map.RANGE_X || Math.Abs(y - this.MapY) >= Map.RANGE_Y) return false;

            this.CastSpell(spellslot, this, (x, y), world);
            return true;
        }

        private void CastSpell(int spellslot, ICharacter target, (int X, int Y)? tile, GameWorld world)
        {
            // … existing body, unchanged, except the dispatch below …
        }
```

In the existing body, replace the dispatch block (`:2433-2456`):

```csharp
                    if (spell.Target == Spell.SpellTargets.Group)
                    {
                        …
                    }
                    else
                    {
                        spell.SpellEffect.Cast(this, target, world, cast);
                    }
```

with:

```csharp
                    if (tile is { } at)
                    {
                        spell.SpellEffect.CastAt(this, this.Map, at.X, at.Y, world);
                    }
                    else if (spell.Target == Spell.SpellTargets.Group)
                    {
                        …unchanged…
                    }
                    else
                    {
                        spell.SpellEffect.Cast(this, target, world, cast);
                    }
```

Passing `this` as `target` for tile casts is safe: the Self/Group rewrite at `:2415-2421` only applies to
Self/Group spells, and `CastSpellAt` only accepts `Target` spells.

`Goose/Events/PlayerCastSpellEvent.cs` — pull the macro-suspicion block into a local function and handle
the 4-part form. Replace the body inside `if (this.Player.State == Player.States.Ready) { … }` after the
mount check with:

```csharp
                string packet = ((string)this.Data).Substring(4);
                string[] t = packet.Split(',');

                if (t.Length == 4)
                {
                    if (int.TryParse(t[0], out int slot) && t[1] == "0" &&
                        int.TryParse(t[2], out int x) && int.TryParse(t[3], out int y) &&
                        slot >= 1 && slot <= world.Settings.SpellbookSize &&
                        this.Player.CastSpellAt(slot, x, y, world))
                        return;

                    this.SuspectMacro(world);
                    return;
                }

                int spellid = 0;
                int target = 0;

                if (t.Length == 2)
                {
                    …existing parsing and target lookup, unchanged…

                        this.SuspectMacro(world);
                    }
                }
```

and add:

```csharp
        private void SuspectMacro(GameWorld world)
        {
            this.Player.SuspectedMacroCount++;
            if (this.Player.SuspectedMacroCount > 10000)
            {
                world.LostConnection(this.Player.Sock);
            }

            if (this.Player.SuspectedMacroCount > 100)
                log.Info("SUSPECTED MACRO: " + this.Player.Name);
        }
```

replacing the inline block at `:72-79` with the call. Update the class's `Syntax:` doc comment line to
`CASTspellbookid,targetid or CASTspellbookid,0,x,y`, since the existing comment is now incomplete.

**Step 4: Run the tests**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~GroundTargetCastTests|FullyQualifiedName~SpellCooldownPacketTests|FullyQualifiedName~GroundZoneCastTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/SpellEffect.cs Goose/Player.cs Goose/Events/PlayerCastSpellEvent.cs Goose.Tests/GroundZones/GroundTargetCastTests.cs
git commit -m "feat(spells): cast ground-targetable spells on a tile"
```

---

### Task 8: Full verification and manual smoke

**Step 1: Run every suite**

```bash
dotnet build Goose.sln -v q
dotnet test Goose.Tests
dotnet test Goose.IntegrationTests
```

Expected: 0 errors; all pass. Report the real counts.

**Step 2: Manual smoke with the current (unmodified) client**

1. Add data: a Sanctuary zone (Self, Area 2, 10s, `hp_change_formula` `+5`, `spell_effected` SelfPlayer,
   `works_not_in_pvp` 1, `zone_buff_effect_id` → a +AC Buff) and a Tar Pit zone (Target, Area 1, 6s,
   formula `0`, buff → a 40% Snare, `spell_effected` NPC).
2. Cast Sanctuary. Expected: heals each 2s while inside; the +AC buff appears on the bar with no
   countdown; it disappears the step you leave and returns when you step back in; clicking it does nothing.
3. Cast Tar Pit on a mob. Expected: it slows while inside, normal speed outside.
4. Log in with a second character mid-zone. Expected: no errors in the server log, and the client shows
   nothing new (the current client ignores `ZADD`/`ZDEL`).
5. Use `/reloadsql` and confirm nothing about active zones throws (existing zones keep their old effect
   reference until they expire).
