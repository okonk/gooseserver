# Mount Toggle Implementation Plan

**Goal:** Make the mount item stay equipped as the normal state, with a new `MNT` client
packet toggling the mounted state (visual, speed buff, combat block) instead of
unequip/re-equip.

**Architecture:** A session-only `Player.Mounted` flag becomes the source of truth for
"mounted". `IsMounted` requires the flag plus an equipped mount item. The mount's item
buff (speed) moves from the equip/unequip lifecycle to the mount-state transitions via
two `Inventory` helpers. A new `ToggleMountEvent` handles the `MNT` packet. Equipping a
mount auto-mounts; login is always dismounted; death keeps the state (item buffs already
survive death).

**Tech Stack:** C# / .NET 10, xUnit, SQLite (integration tests), existing packet-trie
event dispatch.

Design doc: `docs/plans/2026-09-27-mount-toggle-design.md`

## APIs verified

| API | Location |
| --- | --- |
| `Player.IsMounted(GameWorld)` | `Goose/Player.cs:2494` |
| `Player.SendCHPString(GameWorld)` (CHP to self + range) | `Goose/Player.cs:1608` |
| `Player.CalculateMoveSpeed()` (peeks `moveSpeed` queue) | `Goose/Player.cs:1618` |
| `Player.AddBuff(Buff, GameWorld, bool refreshbar, bool updateCharacter = true)` | `Goose/Player.cs:2248` |
| `Player.RemoveBuff(Buff, GameWorld, bool refreshbar, bool updateCharacter = true)` | `Goose/Player.cs:2526` |
| `Player.Attacked(ICharacter, long, GameWorld)` (death at HP ≤ 0; GMs always "miss") | `Goose/Player.cs:1964` |
| `Player` ctor initializes `Buffs = []` | `Goose/Player.cs:493` |
| `Inventory.EquipCore` (buff block at 379-389, CHP send at 390-398) | `Goose/Inventory.cs:348` |
| `Inventory.Unequip(EquipSlots, GameWorld)` (slot nulled at 602, buff block at 604-624, CHP at 626-635) | `Goose/Inventory.cs:590` |
| `Inventory.Load` equipped loop (buff block at 1026-1036) | `Goose/Inventory.cs:969` |
| `Inventory.MountDisplay()` | `Goose/Inventory.cs:791` |
| `Inventory.GetEquippedSlot(EquipSlots)` | `Goose/Inventory.cs:724` |
| `Inventory.EquipSlots.Mount = 14` | `Goose/Inventory.cs:32` |
| `Map.CanUseItems` | `Goose/Map.cs:52` |
| `P.ServerMessage(string)` | `Goose/Packets.cs:38` |
| `P.HashMessage(string)` (`"#" + message`) | `Goose/Packets.cs:47` |
| `Inventory.Use` no-items refusal sends `P.HashMessage("You can't use items in this map.")` | `Goose/Inventory.cs:304-307` |
| `LoginContinuedEvent` (per-login LCNT transition; State == LoadingGame; sends buff bar) | `Goose/Events/LoginContinuedEvent.cs:21-75` |
| Players load once at startup into `allNameToPlayer`; logout keeps item buffs and reuses the Player on relogin | `Goose/PlayerHandler.cs:188-216`, `Goose/Events/LogoutEvent.cs:77-89` |
| `Player.CurrentHP` setter clamps to `MaxHP` | `Goose/Player.cs:160-167` |
| `LoadFromReader` seeds the `moveSpeed` queue with `BaseStats.MoveSpeed` | `Goose/Player.cs:825-826` |
| `Map.CanUseItems` is a plain bool (default `false`); `AddBaseMap` does not set it — fixture maps are no-items unless set | `Goose/Map.cs:52`, `TestSupport/TestWorldFixture.cs:61-71` |
| `MapHandler.GetMap` reads the same dictionary `AddBaseMap` writes | `Goose/MapHandler.cs:89-92` |
| Packet registration list (`_SeedCommands`) | `Goose/EventHandler.cs:116-160` |
| `TestWorldFixture.CommandPlayerOn` (State=Ready, Inventory set, no BoundMap) | `TestSupport/TestWorldFixture.cs:100` |
| `TestWorldFixture.AddBaseMap/AddBaseItemTemplate/AddBaseSpellEffect` | `TestSupport/TestWorldFixture.cs:61,159,74` |
| Client-packet dispatch in tests: `fixture.World.EventHandler.AddEvent(player, "MNT")` + `Update` | pattern in `Goose.Tests/MapTransitionEventGuardTests.cs:17-20` |
| `PlayerFirstSaveTestBase` (`MakePlayer`, `world`, `EquippedSize = 20`; `Insert`/`SlotJson`/`FullArray` are private to `PlayerLoadMissingRowTests`, not on the base) | `Goose.IntegrationTests/PlayerFirstSaveTests.cs:6` |
| `SlotJson`/`FullArray`/`Insert` helper pattern | `Goose.IntegrationTests/PlayerLoadMissingRowTests.cs:121-152` |
| `Item.SpellEffect` passthrough to `Template.SpellEffect` | `Goose/Item.cs:120` |
| `LoadSlots` normalizes JSON array to expected size | `Goose/Inventory.cs:1084-1094` |

Notes:

- `AddBuff`/`RemoveBuff` with `updateCharacter: false` suppress stat-driven CHP, but they can still append a CHP for illusion (`SpellEffect.BodyID != 0`, Player.cs:2352-2355) or invisibility transitions (Player.cs:2372). The "exactly one CHP" invariant therefore applies to stat-only mount effects (all current mounts, e.g. effect 259); a mount with a body/invisibility effect would emit an additional CHP through the helpers — same behavior as today's equip/unequip paths.
- `Unequip` nulls `this.equipped[(int)equipslot]` **before** removing the item buff
  (Inventory.cs:602 vs 604), so buff-removal helpers must take the item/slot as a
  parameter, not re-fetch the mount slot.
- "Login is always dismounted" has **two** paths: server startup (`Inventory.Load`
  skips the mount buff) and relogin (the same `Player` object is reused —
  `PlayerHandler.LoadPlayerData` loads once at startup, `LogoutEvent` keeps item
  buffs — so `LoginContinuedEvent` must dismount explicitly).
- AGENTS.md: no new comments/doc strings unless the "why" is non-obvious (the
  session-only invariant on `Mounted` and the login-skip in `Load` qualify; keep each
  to one line).

---

### Task 1: Mount state + equip/unequip/login lifecycle

**Files:**
- Modify: `Goose/Player.cs` (add `Mounted` property above `IsMounted` at :2494; change `IsMounted` body)
- Modify: `Goose/Inventory.cs` (two new helpers near `MountDisplay` at :791; `EquipCore` buff block :379-389; `Unequip` buff block :604-624; `Load` buff block :1026-1036)
- Modify: `Goose/Events/LoginContinuedEvent.cs` (dismount-on-relogin hook before `SendBuffBar`, :67)
- Test: `Goose.Tests/MountSpeedTests.cs`

**Mutation impact:**
- Source of truth changed: new `Player.Mounted` (session-only, not persisted); mount
  item buff moves from equip/unequip to mount-state transitions.
- Important readers: `IsMounted` → `Goose/Events/PlayerAttackEvent.cs:39`,
  `Goose/Events/PlayerCastSpellEvent.cs:21`; buff stats → `CalculateMoveSpeed`
  (`Goose/Player.cs:1618`) → move-speed field of `CHP`/`MKC`
  (`Goose/Packets.cs:112,151`); buff bar via `AddBuff`/`RemoveBuff` refreshbar.
- Derived/cached state affected: `moveSpeed` queue (via `AddStats`/`RemoveStats` inside
  `AddBuff`/`RemoveBuff`); no persistence (state is session-only by design).
- Required propagation sequence:
  1. `EquipCore`: set `player.Mounted = true`, then `ApplyMountBuff` (buff + stats),
     then the existing `SendEquippedSlot` + CHP + `StatusInfo` + `WeaponSpeed` sends
     (Inventory.cs:390-401).
  2. `Unequip`: set `player.Mounted = false`, then `RemoveMountBuff` (stats + buff),
     then the existing CHP sends (Inventory.cs:626-635).
  3. `ToggleMountEvent` (Task 3): flip flag → helper → `SendCHPString`.
  4. `LoginContinuedEvent` (relogin): if `Mounted`, set `Mounted = false` and
     `RemoveMountBuff` — before `SendBuffBar` so the buff bar reflects the removal.
     `Inventory.Load` (startup) needs no flag change (`Mounted` defaults to false);
     it only skips the buff.
- Invariants to preserve:
  - `Mounted` is true only while a mount item is equipped (via the equip/unequip
    paths); a player can never log in mounted — on startup (Load skip) or on
    relogin (LCNT hook).
  - The mount buff is present iff mounted and the item has a spell effect.
  - Replacement (equipping a second mount) leaves the player mounted with the new
    mount's effect.
- Observable proof required: tests below assert `IsMounted`, `CalculateMoveSpeed()`,
  and the equipped/inventory slot contents — not helper calls.

**Step 1: Write the failing tests**

Add to `Goose.Tests/MountSpeedTests.cs` (the existing `Fixture` class already builds a
"Tank" mount, item 651, with effect 259 / +128 move speed; `BaseSpeed = 320`).

First, in the `Fixture` constructor, set `map.CanUseItems = true` after
`AddBaseMap` — `Map.CanUseItems` defaults to `false` and `AddBaseMap` never sets it
(Map.cs:52, TestWorldFixture.cs:61-71), while real maps default to items-enabled
(MapHandler.cs:57). Without this, every mount-on transition in this file is refused.
Also set `Settings.MOTD = ""` — `MOTD` is `null!` (GooseSettings.cs:75) and the
relogin test's `LCNT` dispatch dereferences `MOTD.Length`
(LoginContinuedEvent.cs:51).

```csharp
[Fact]
public void Equipping_the_mount_sets_the_mounted_state()
{
    using var fixture = new Fixture(equipMount: false);

    Assert.True(fixture.Player.Inventory.Equip(fixture.Mount, fixture.World.World));

    Assert.True(fixture.Player.Mounted);
    Assert.True(fixture.Player.IsMounted(fixture.World.World));
    Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
}

[Fact]
public void IsMounted_false_when_equipped_but_state_off()
{
    using var fixture = new Fixture();

    fixture.Player.Mounted = false;

    Assert.False(fixture.Player.IsMounted(fixture.World.World));
}

[Fact]
public void Unequipping_the_mount_clears_the_mounted_state()
{
    using var fixture = new Fixture();

    Assert.True(fixture.Player.Inventory.Unequip(Inventory.EquipSlots.Mount, fixture.World.World));

    Assert.False(fixture.Player.Mounted);
    Assert.False(fixture.Player.IsMounted(fixture.World.World));
    Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
}

[Fact]
public void Equipping_a_second_mount_switches_speed_and_stays_mounted()
{
    using var fixture = new Fixture();

    var effect2 = fixture.World.AddBaseSpellEffect(260, "Mount Speed III", e =>
    {
        e.EffectType = SpellEffect.EffectTypes.Buff;
        e.Stats = new AttributeSet { MoveSpeed = 200 };
    });
    var template2 = fixture.World.AddBaseItemTemplate(652, "Horse", ItemTemplate.UseTypes.Armor, t =>
    {
        t.Slot = ItemTemplate.ItemSlots.Mount;
        t.GraphicEquipped = 274;
        t.SpellEffect = effect2;
    });
    var horse = new Item();
    horse.LoadFromTemplate(template2);
    fixture.World.World.ItemHandler.AddAndAssignId(horse, fixture.World.World);
    Assert.True(fixture.Player.Inventory.AddItem(horse, 1, fixture.World.World));
    Assert.True(fixture.Player.Inventory.Equip(horse, fixture.World.World));

    Assert.True(fixture.Player.Mounted);
    Assert.Equal(200, fixture.Player.CalculateMoveSpeed());
    Assert.Contains(fixture.Player.Inventory.GetInventorySlots(),
        s => s is not null && s.Item.TemplateID == 651);
}
```

Adversarial focus: `IsMounted_false_when_equipped_but_state_off` fails on any
implementation that derives "mounted" from the equipped slot or the buff alone;
`Equipping_a_second_mount_switches_speed_and_stays_mounted` fails if the old mount's
buff is not removed before the new one is added (speed would be 128+200 or 128).

Add the relogin test (same file) — it exercises the `LoginContinuedEvent` hook through
the real packet path. `LCNT` is registered at `Goose/EventHandler.cs:125`; the event
requires `State == LoadingGame` and `MapHandler.GetMap(Player.MapID)` to resolve
(`CommandPlayerOn` sets `MapID`, `AddBaseMap` registers the map):

```csharp
[Fact]
public void Relogin_dismounts_a_mounted_player()
{
    using var fixture = new Fixture();
    Assert.True(fixture.Player.Mounted);
    Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());

    fixture.Player.Spellbook = new Spellbook(fixture.Player, fixture.World.Settings);
    fixture.Player.State = Player.States.LoadingGame;
    fixture.Player.Sent.Clear();
    fixture.World.EventHandler.AddEvent(fixture.Player, "LCNT");
    fixture.World.EventHandler.Update(fixture.World.World);

    Assert.False(fixture.Player.Mounted);
    Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
    Assert.DoesNotContain(fixture.Player.Buffs, b => b.ItemBuff);
}
```

`CommandPlayerOn` does not initialize `Spellbook`, and `LoginContinuedEvent` calls
`SendSpellbook` (Player.cs:2105) — set it as `MapWarpNullGuardTests.cs:29` does.
The fixture constructor also sets `Settings.MOTD = ""`: `MOTD` is `null!`
(GooseSettings.cs:75) and `LoginContinuedEvent` dereferences `MOTD.Length`
(LoginContinuedEvent.cs:51) — `MapWarpNullGuardTests.cs:23` works around the same
NRE the same way.

**Step 2: Run tests to verify they fail (red)**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~MountSpeedTests" --nologo -v q`
Expected: compile error — `Player.Mounted` does not exist.

**Step 3: Implement**

1. `Goose/Player.cs`, directly above `IsMounted` (:2494):

```csharp
// Session-only: deliberately not persisted (login is always dismounted).
public bool Mounted { get; set; }
```

2. `IsMounted` body (keep the Aspereta guard):

```csharp
return this.Mounted && this.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount) is not null;
```

3. `Goose/Inventory.cs`, two new helpers (place near `MountDisplay`, :791). They take
the item because `Unequip` nulls the slot array entry before the buff is removed:

```csharp
public void ApplyMountBuff(Item item, GameWorld world)
{
    if (item.SpellEffect is null) return;

    Buff buff = new Buff();
    buff.Caster = this.player;
    buff.Target = this.player;
    buff.ItemBuff = true;
    buff.SpellEffect = item.SpellEffect;

    this.player.AddBuff(buff, world, true, updateCharacter: false);
}

public void RemoveMountBuff(Item item, GameWorld world, bool refreshbar = true)
{
    if (item.SpellEffect is null) return;

    Buff? remove = null;
    foreach (var buff in this.player.Buffs)
    {
        if (buff.ItemBuff && buff.SpellEffect == item.SpellEffect)
        {
            remove = buff;
            break;
        }
    }

    if (remove is not null)
        this.player.RemoveBuff(remove, world, refreshbar, updateCharacter: false);
}
```

Helper contracts:
- `ApplyMountBuff(item, world)`: precond — `item` is the mount item being equipped on
  this player; the caller sets `player.Mounted`. No-op when `item.SpellEffect` is null.
  Postcond — buff on `player.Buffs`, stats applied, no stat-driven CHP sent (caller
  sends the CHP; illusion/invisibility effects can still append one — see Notes).
- `RemoveMountBuff(item, world, refreshbar = true)`: no-op when no effect or no
  matching buff. Postcond — buff removed, stats removed, no stat-driven CHP sent
  (caller sends the CHP); buff bar refreshed iff `refreshbar` (mirrors
  `RemoveBuff`'s parameter style, Player.cs:2526). The relogin path passes
  `refreshbar: false` and relies on `LoginContinuedEvent`'s own `SendBuffBar`
  (LoginContinuedEvent.cs:67).

4. `EquipCore` (:379-389): replace the generic spell-effect buff block with:

```csharp
if (equipslot == EquipSlots.Mount)
{
    this.player.Mounted = true;
    this.ApplyMountBuff(slot.Item, world);
}
else if (slot.Item.SpellEffect is not null)
{
    // …existing buff block unchanged…
}
```

The mount branch sits outside the null-effect check so visual-only mounts still set the
flag.

5. `Unequip` (:604-624): mirror image —

```csharp
if (equipslot == EquipSlots.Mount)
{
    this.player.Mounted = false;
    this.RemoveMountBuff(slot.Item, world);
}
else if (slot.Item.SpellEffect is not null)
{
    // …existing buff block unchanged…
}
```

6. `Load` (:1026-1036): skip the item buff for the mount slot (login is always
dismounted) — guard the existing `if (equipSlot.Item.SpellEffect is not null)` block
with `i != (int)EquipSlots.Mount` (loop index `i` is the equipped-array index,
Inventory.cs:1001), with a one-line comment citing the login-dismounted invariant.
`AddStats` for the mount item stays (mount items carry no direct stats, but keep it
generic).

7. `Goose/Events/LoginContinuedEvent.cs`, relogin path — players are loaded once at
   startup (`PlayerHandler.LoadPlayerData`, PlayerHandler.cs:188) and `LogoutEvent`
   keeps item buffs (LogoutEvent.cs:77-89), so a mounted player who reconnects would
   otherwise stay mounted. Insert immediately after
   `this.Player.State = Player.States.LoadingMap;` (:40) — before `P.StatusInfo`
   (:63) so the login `SNF` reflects the dismounted stats, not stale mounted ones:

```csharp
// Dismount on login: the mounted state is session-only.
if (this.Player.Mounted)
{
    this.Player.Mounted = false;
    ItemSlot? mountSlot = this.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount);
    if (mountSlot is not null)
        this.Player.Inventory.RemoveMountBuff(mountSlot.Item, world, refreshbar: false);
}
```

Safe during `LoadingMap`: `RemoveBuff` skips range sends unless `State == Ready`
(Player.cs:2579) and `updateCharacter: false` suppresses stat-driven CHP.
`refreshbar: false` keeps the later `SendBuffBar` (:67) the single authoritative buff
bar send — no early or duplicate buff bar packet before the map packets.

**Step 4: Run tests to verify they pass (green)**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~MountSpeedTests" --nologo -v q`
Expected: all MountSpeedTests pass, including the 9 pre-existing facts (equipping a
mount now auto-mounts, so their `IsMounted`/speed assertions still hold).

Then the full unit suite: `dotnet test Goose.Tests --nologo -v q` — Expected: all pass.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/Inventory.cs Goose/Events/LoginContinuedEvent.cs Goose.Tests/MountSpeedTests.cs
git commit -m "feat(mounts): mounted state gates mount speed; equipping auto-mounts"
```

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Equipping a mount auto-mounts (speed + state) | `Equipping_the_mount_sets_the_mounted_state` |
| `IsMounted` requires the flag, not just the slot | `IsMounted_false_when_equipped_but_state_off` |
| Unequip dismounts and restores base speed | `Unequipping_the_mount_clears_the_mounted_state` + pre-existing `Unequipping_the_mount_restores_the_base_speed` |
| Replacement keeps mounted, switches effect, old mount in inventory | `Equipping_a_second_mount_switches_speed_and_stays_mounted` |
| Relogin dismounts a mounted player (reused Player object) | `Relogin_dismounts_a_mounted_player` |
| Startup load is dismounted | Task 5 (integration) |

---

### Task 2: Mount display gating

**Files:**
- Modify: `Goose/Inventory.cs` (`MountDisplay`, :791-816)
- Test: `Goose.Tests/MountSpeedTests.cs`

**Mutation impact:**
- Source of truth changed: `MountDisplay()` output now depends on `Player.Mounted`.
- Important readers: `P.MakeCharacter` (`Goose/Packets.cs:112`), `P.UpdateCharacter`
  (`Goose/Packets.cs:151`), `GmHaxCommand` (`Goose/Commands/GmHaxCommand.cs:41`) — all
  via `MountDisplay()`, so all pick up the change with no signature change.
- Derived/cached state affected: none (CHP/MKC are built on demand).
- Invariants to preserve:
  - CHP/MKC mount field is the graphic iff mounted and a mount item is equipped;
    `"0,*"` otherwise.
  - `CharacterAppearancePacketTests` stays green (its fixture equips a mount, which
    auto-mounts).
- Observable proof required: tests assert the exact CHP/MKC tail strings.

**Step 1: Write the failing tests**

Add to `Goose.Tests/MountSpeedTests.cs` (fixture mount: `GraphicEquipped = 273`,
fixture template defaults `GraphicR/G/B = 255`, `GraphicA = 100`):

```csharp
[Fact]
public void Mount_display_shown_while_mounted()
{
    using var fixture = new Fixture();

    Assert.EndsWith("273,255,255,255,100,", fixture.MakeCharacter());
    Assert.EndsWith("273,255,255,255,100,", P.UpdateCharacter(fixture.Player));
}

[Fact]
public void Mount_display_hidden_when_equipped_but_dismounted()
{
    using var fixture = new Fixture();
    fixture.Player.Mounted = false;

    Assert.EndsWith("0,*", fixture.MakeCharacter());
    Assert.EndsWith("0,*", P.UpdateCharacter(fixture.Player));
}
```

**Step 2: Run tests to verify they fail (red)**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~MountSpeedTests" --nologo -v q`
Expected: `Mount_display_hidden_when_equipped_but_dismounted` FAILS (graphic still
shown); `Mount_display_shown_while_mounted` passes.

**Step 3: Implement**

`MountDisplay` (Inventory.cs:793-794): gate the slot fetch on the flag —

```csharp
ItemSlot? item = this.player.Mounted ? this.GetEquippedSlot(EquipSlots.Mount) : null;
```

Nothing else changes (the `null` branch already emits `"0,*"`).

**Step 4: Run tests to verify they pass (green)**

Run: `dotnet test Goose.Tests --nologo -v q`
Expected: all pass, including `CharacterAppearancePacketTests` (auto-mount keeps its
expected mount graphic).

**Step 5: Commit**

```bash
git add Goose/Inventory.cs Goose.Tests/MountSpeedTests.cs
git commit -m "feat(mounts): hide mount display in CHP/MKC while dismounted"
```

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Graphic shown while mounted | `Mount_display_shown_while_mounted` |
| Graphic hidden when equipped but dismounted | `Mount_display_hidden_when_equipped_but_dismounted` |
| Existing appearance packets unchanged for mounted players | pre-existing `CharacterAppearancePacketTests` |

---

### Task 3: `MNT` packet and `ToggleMountEvent`

**Files:**
- Create: `Goose/Events/ToggleMountEvent.cs`
- Modify: `Goose/EventHandler.cs` (packet list in `_SeedCommands`, :116-160)
- Test: `Goose.Tests/MountSpeedTests.cs`

**Mutation impact:**
- Source of truth changed: `Player.Mounted` and the mount buff, now also mutated by a
  client packet.
- Important readers: same as Task 1/2 (CHP/MKC mount field + move speed, `ATT`/`CAST`
  gate via `IsMounted`).
- Derived/cached state affected: `moveSpeed` queue via the buff helpers.
- Required propagation sequence (per toggle):
  1. Guards: `State == Ready`; mount slot exists
     (`(int)Inventory.EquipSlots.Mount <= world.Settings.EquippedSize` — Aspereta
     no-op); mount item equipped.
  2. If mounting (currently dismounted) and `!Player.Map.CanUseItems` → send
     `P.HashMessage("You can't use items in this map.")` (same packet type and text
     as `Inventory.Use`, Inventory.cs:304-307; `P.HashMessage` is `"#" + message`,
     Packets.cs:47) and return. Dismounting is never gated.
  3. Flip `Player.Mounted`.
  4. `ApplyMountBuff` / `RemoveMountBuff` with the equipped mount item.
  5. `Player.SendCHPString(world)` (Player.cs:1608) — one CHP to self + range; for
     stat-only mount effects the helpers emit no CHP (`updateCharacter: false`
     suppresses stat-driven CHP; the illusion/invisibility CHP append in
     `AddBuff`/`RemoveBuff` does not trigger for stat-only effects).
- Invariants to preserve:
  - No state change and no CHP on any no-op path.
  - Mounting is gated by `CanUseItems`; dismounting is not.
  - Exactly one CHP per effective toggle for stat-only mount effects (helpers
    suppress their own).
- Observable proof required: tests assert `Mounted`, `CalculateMoveSpeed()`, the CHP
  tail, and the refusal message.

**Step 1: Write the failing tests**

Add to `Goose.Tests/MountSpeedTests.cs`. Dispatch pattern (same as
`MapTransitionEventGuardTests.cs:17-20`): `fixture.World.EventHandler.AddEvent(player,
"MNT")` then `fixture.World.EventHandler.Update(fixture.World.World)`.

```csharp
[Fact]
public void MNT_toggles_the_mounted_state_and_speed()
{
    using var fixture = new Fixture();
    Assert.True(fixture.Player.Mounted);

    fixture.Player.Sent.Clear();
    fixture.World.EventHandler.AddEvent(fixture.Player, "MNT");
    fixture.World.EventHandler.Update(fixture.World.World);

    Assert.False(fixture.Player.Mounted);
    Assert.False(fixture.Player.IsMounted(fixture.World.World));
    Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
    var chp = fixture.Player.Sent.Single(s => s.StartsWith("CHP"));
    Assert.EndsWith("0,*", chp);

    fixture.Player.Sent.Clear();
    fixture.World.EventHandler.AddEvent(fixture.Player, "MNT");
    fixture.World.EventHandler.Update(fixture.World.World);

    Assert.True(fixture.Player.Mounted);
    Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
    var chp2 = fixture.Player.Sent.Single(s => s.StartsWith("CHP"));
    Assert.EndsWith("273,255,255,255,100,", chp2);
}

[Fact]
public void MNT_without_a_mount_is_a_noop()
{
    using var fixture = new Fixture(equipMount: false);
    fixture.Player.Sent.Clear();

    fixture.World.EventHandler.AddEvent(fixture.Player, "MNT");
    fixture.World.EventHandler.Update(fixture.World.World);

    Assert.False(fixture.Player.Mounted);
    Assert.DoesNotContain(fixture.Player.Sent, s => s.StartsWith("CHP"));
}

[Fact]
public void MNT_refuses_to_mount_on_a_no_items_map_but_allows_dismount()
{
    using var fixture = new Fixture();
    Assert.True(fixture.Player.Mounted);

    fixture.Player.Map.CanUseItems = false;

    fixture.Player.Sent.Clear();
    fixture.World.EventHandler.AddEvent(fixture.Player, "MNT");
    fixture.World.EventHandler.Update(fixture.World.World);
    Assert.False(fixture.Player.Mounted);
    Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());

    fixture.Player.Sent.Clear();
    fixture.World.EventHandler.AddEvent(fixture.Player, "MNT");
    fixture.World.EventHandler.Update(fixture.World.World);
    Assert.False(fixture.Player.Mounted);
    Assert.Contains(fixture.Player.Sent, s => s == "#You can't use items in this map.");
}

[Fact]
public void MNT_works_for_a_visual_only_mount_without_a_buff()
{
    using var fixture = new Fixture(equipMount: false);

    var template = fixture.World.AddBaseItemTemplate(653, "Statue", ItemTemplate.UseTypes.Armor, t =>
    {
        t.Slot = ItemTemplate.ItemSlots.Mount;
        t.GraphicEquipped = 300;
    });
    var statue = new Item();
    statue.LoadFromTemplate(template);
    fixture.World.World.ItemHandler.AddAndAssignId(statue, fixture.World.World);
    Assert.True(fixture.Player.Inventory.AddItem(statue, 1, fixture.World.World));
    Assert.True(fixture.Player.Inventory.Equip(statue, fixture.World.World));

    Assert.True(fixture.Player.Mounted);
    Assert.True(fixture.Player.IsMounted(fixture.World.World));
    Assert.Empty(fixture.Player.Buffs);
    Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
    Assert.EndsWith("300,255,255,255,100,", fixture.MakeCharacter());

    fixture.Player.Sent.Clear();
    fixture.World.EventHandler.AddEvent(fixture.Player, "MNT");
    fixture.World.EventHandler.Update(fixture.World.World);

    Assert.False(fixture.Player.Mounted);
    Assert.False(fixture.Player.IsMounted(fixture.World.World));
    var chp = fixture.Player.Sent.Single(s => s.StartsWith("CHP"));
    Assert.EndsWith("0,*", chp);
}

[Fact]
public void USE_on_the_mount_slot_unequips_and_dismounts()
{
    using var fixture = new Fixture();
    Assert.True(fixture.Player.Mounted);

    // Client mount slot id: InventorySize(30) + (int)EquipSlots.Mount(14) + 1 = 45
    fixture.World.EventHandler.AddEvent(fixture.Player, "USE45");
    fixture.World.EventHandler.Update(fixture.World.World);

    Assert.False(fixture.Player.Mounted);
    Assert.False(fixture.Player.IsMounted(fixture.World.World));
    Assert.Equal(BaseSpeed, fixture.Player.CalculateMoveSpeed());
    Assert.Contains(fixture.Player.Inventory.GetInventorySlots(),
        s => s is not null && s.Item.TemplateID == 651);
}
```

**Step 2: Run tests to verify they fail (red)**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~MountSpeedTests" --nologo -v q`
Expected: the four `MNT_*` tests FAIL — `AddEvent(player, "MNT")` returns false
(unregistered packet), state never changes. `USE_on_the_mount_slot_unequips_and_dismounts`
passes already (it pins Task 1's unequip behavior through the real packet path —
the deliberate "sell my mount" route).

**Step 3: Implement**

Create `Goose/Events/ToggleMountEvent.cs`:

```csharp
namespace Goose.Events
{
    public class ToggleMountEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;

            if ((int)Inventory.EquipSlots.Mount > world.Settings.EquippedSize) return;

            ItemSlot? mountSlot = this.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount);
            if (mountSlot is null) return;

            if (!this.Player.Mounted && !this.Player.Map.CanUseItems)
            {
                world.Send(this.Player, P.HashMessage("You can't use items in this map."));
                return;
            }

            this.Player.Mounted = !this.Player.Mounted;
            if (this.Player.Mounted)
            {
                this.Player.Inventory.ApplyMountBuff(mountSlot.Item, world);
            }
            else
            {
                this.Player.Inventory.RemoveMountBuff(mountSlot.Item, world);
            }

            this.Player.SendCHPString(world);
        }
    }
}
```

Register in `Goose/EventHandler.cs` `_SeedCommands` packet list (next to `"PONG"`,
:137):

```csharp
("MNT", Open(typeof(ToggleMountEvent))),
```

**Step 4: Run tests to verify they pass (green)**

Run: `dotnet test Goose.Tests --nologo -v q`
Expected: all pass.

**Step 5: Commit**

```bash
git add Goose/Events/ToggleMountEvent.cs Goose/EventHandler.cs Goose.Tests/MountSpeedTests.cs
git commit -m "feat(mounts): MNT packet toggles mounted state"
```

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Toggle flips state, speed, and CHP graphic in both directions | `MNT_toggles_the_mounted_state_and_speed` |
| No mount → no state change, no CHP | `MNT_without_a_mount_is_a_noop` |
| Mounting gated by `CanUseItems`, dismounting not | `MNT_refuses_to_mount_on_a_no_items_map_but_allows_dismount` |
| Visual-only mount: toggle flips graphic + combat block with no buff | `MNT_works_for_a_visual_only_mount_without_a_buff` |
| `USE` on the mount slot still unequips (deliberate sell path) | `USE_on_the_mount_slot_unequips_and_dismounts` |

---

### Task 4: Death keeps the player mounted (test-only)

**Files:**
- Test: `Goose.Tests/MountSpeedTests.cs`

No production code changes: death strips only non-item buffs
(`Goose/Player.cs:2025-2036`), and the mount buff is an item buff, so the state and
speed survive by construction. This test pins that behavior.

**Step 1: Write the test**

Add to `Goose.Tests/MountSpeedTests.cs`:

```csharp
[Fact]
public void Death_keeps_the_player_mounted()
{
    using var fixture = new Fixture();
    var map = fixture.Player.Map;
    fixture.Player.BoundMap = map;
    fixture.Player.BoundID = map.ID;
    fixture.Player.BoundX = fixture.Player.MapX;
    fixture.Player.BoundY = fixture.Player.MapY;
    fixture.Player.MaxStats.HP = 100;
    fixture.Player.MaxStats.Dexterity = -1; // dodge check is Random.Next(0, 10001) <= dex*100/100 (Player.cs:1992-1997); negative dex makes the 1/10001 dodge impossible
    fixture.Player.CurrentHP = 1;

    var attacker = new NPC { Name = "Slime", LoginID = 99 };
    fixture.Player.Attacked(attacker, 100, fixture.World.World);

    Assert.True(fixture.Player.Mounted);
    Assert.True(fixture.Player.IsMounted(fixture.World.World));
    Assert.Equal(MountSpeed, fixture.Player.CalculateMoveSpeed());
    Assert.Contains(fixture.Player.Buffs, b => b.ItemBuff);
}
```

Notes: the fixture player is not a GM (GMs always "miss" in `Attacked`,
Player.cs:1978); the `CurrentHP` setter clamps to `MaxHP` (Player.cs:160-167) and the
fixture's `MaxStats.HP` is 0, so `MaxStats.HP` must be set for the "low HP, then
killed" precondition to be honest; same-map `WarpTo` on death needs `BoundMap` set
(Player.cs:2020); `CommandPlayerOn` does not set it (TestWorldFixture.cs:100-114).

**Step 2: Run test**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~MountSpeedTests" --nologo -v q`
Expected: PASS (behavior already holds — this is a regression pin; if it fails, the
death path or the buff lifecycle is wrong and Task 1's assumptions need revisiting).

**Step 3: Commit**

```bash
git add Goose.Tests/MountSpeedTests.cs
git commit -m "test(mounts): death keeps player mounted"
```

---

### Task 5: Login loads the mount equipped but dismounted (test-only)

**Files:**
- Create: `Goose.IntegrationTests/MountLoadTests.cs`

Pins the `Inventory.Load` skip (Task 1 step 6) through the real DB path.

**Step 1: Write the test**

```csharp
using System.Data.SQLite;
using Xunit;

namespace Goose.IntegrationTests;

public class MountLoadTests : PlayerFirstSaveTestBase
{
    public MountLoadTests() : base(["players", "banks"]) { }

    [Fact]
    public void Loading_a_saved_mount_equips_it_dismounted_without_the_speed_buff()
    {
        var effect = new SpellEffect
        {
            ID = 259,
            Name = "Mount Speed II",
            EffectType = SpellEffect.EffectTypes.Buff,
            Stats = new AttributeSet { MoveSpeed = 128 },
        };
        world.SpellHandler.AddSpellEffect(effect);
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 651,
            Name = "Tank",
            Slot = ItemTemplate.ItemSlots.Mount,
            SpellEffect = effect,
            BaseStats = new AttributeSet(),
        });

        var player = MakePlayer();
        // MakePlayer sets BaseStats but not MaxStats; AddStats dereferences it
        // (Player.cs:155,1653) and the equipped loop calls AddStats.
        player.MaxStats = new AttributeSet();
        player.BaseStats.MoveSpeed = 320;
        // Seed the move-speed queue the way LoadFromReader does (Player.cs:825-826);
        // MakePlayer does not go through LoadFromReader.
        var queue = (PriorityQueue<int, int>)typeof(Player)
            .GetProperty("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(player)!;
        queue.Enqueue(320, 320);

        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((14, SlotJson(651)))));

        player.Inventory.Load(world);

        var mount = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount);
        Assert.NotNull(mount);
        Assert.Equal(651, mount!.Item.TemplateID);
        Assert.False(player.Mounted);
        Assert.Empty(player.Buffs);
        // 128 if the mount buff were (wrongly) applied: the queue is a min-heap, so
        // the mount speed would win over the 320 base.
        Assert.Equal(320, player.CalculateMoveSpeed());
    }

    private void Insert(string sql, (string name, object value)? arg = null)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (arg is not null)
                cmd.Parameters.Add(new SQLiteParameter(arg.Value.name, arg.Value.value));
            cmd.ExecuteNonQuery();
        });
    }

    private static string SlotJson(int templateId) =>
        JsonHelper.Serialize(new ItemSlot
        {
            Item = new Item { TemplateID = templateId, BaseStats = new AttributeSet() },
            Stack = 1,
        });

    private static string FullArray(params (int index, string json)[] slots)
    {
        // EquippedSize is 20 in PlayerFirstSaveTestBase; the equipped array is size+1.
        var entries = new string[21];
        foreach (var (index, json) in slots)
            entries[index] = json;
        return "[" + string.Join(",", entries.Select(e => e ?? "null")) + "]";
    }
}
```

The `Insert`/`SlotJson`/`FullArray` helpers mirror
`Goose.IntegrationTests/PlayerLoadMissingRowTests.cs:121-152` (those are private there,
so they are duplicated; `FullArray` is sized 21 to match `EquippedSize = 20` and avoid
the length-mismatch warn in `LoadSlots`, Inventory.cs:1092). Only
`using System.Data.SQLite;` is needed — `Goose` types resolve via the parent namespace
(same as `PlayerLoadMissingRowTests.cs:1-4`), and `System.Linq` comes from implicit
usings.

Adversarial focus: if the `Load` mount-skip from Task 1 is missing, `player.Buffs`
contains the mount buff and `CalculateMoveSpeed()` returns 128, so both assertions
fail. (Reconnect dismount is covered separately by Task 1's
`Relogin_dismounts_a_mounted_player` — this test covers the startup-load path.)

**Step 2: Run test**

Run: `dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~MountLoadTests" --nologo -v q`
Expected: PASS.

**Step 3: Run the full suite**

Run: `dotnet test Goose.sln --nologo -v q`
Expected: all unit + integration tests pass.

**Step 4: Commit**

```bash
git add Goose.IntegrationTests/MountLoadTests.cs
git commit -m "test(mounts): login loads mount equipped but dismounted"
```

---

## Final verification

```bash
dotnet test Goose.sln --nologo -v q
```

Expected: all tests pass (baseline was 1550 unit + 428 integration at
commit 8157ff6).

Out of scope (per design doc): client-side hotkey/rendering (Goose2ClientGodot),
script/GM removal of the mount item from the equip slot, old-client behavior.
