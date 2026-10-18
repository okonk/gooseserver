# Halloween Event Part 1 (Engine) Implementation Plan

**Goal:** Add the server engine support the Halloween event scripts need: A* path search, NPC
despawn and registry release for temporary NPCs, a global "NPC killed" hook, map hooks for
dropping/picking up items, item script equip/unequip events, a transient per-player script state
bag, and a setting to turn off player-count XP scaling.

**Architecture:** Small, independent additions to `Goose/` with no event content. Each is a
reusable API that Part 2 (house scripts) and Part 3 (boss scripts) call. Default NPC movement,
drop, pickup and equip behaviour is unchanged when no script uses the new hooks.

**Tech Stack:** .NET 10, xUnit (`Goose.Tests`, fast project), Roslyn C# scripting (`.csx`).

Design: the Halloween event design doc (artifact "Haunted House Event",
https://claude.ai/artifact/PnXDddVa1NG5nFqmjL48vK). Part 2:
`docs/plans/2026-10-18-halloween-part2-house-scripts.md`. Part 3:
`docs/plans/2026-10-18-halloween-part3-boss-scripts.md`. Game data (sheet rows) is a separate,
later plan.

**Repo rule (AGENTS.md):** no new comments or doc strings unless the "why" is non-obvious.

**Already done, not in this plan:** the event message feed is the existing notice channel
(`P.Notice(NoticeType.Info, "[Event] ...")`, `Goose/Packets.cs:43`, client `NoticeFeed`), and the
darkness effect is the map ambience lighting (`P.MapAmbience`, `Goose/Packets.cs:464`, restored with
`MapAmbience.Broadcast`, `Goose/MapAmbience.cs:98`). The tier 1 XP bonus sets `world.ExperienceModifier` once Task 8
turns off the player-count scaling that overwrites it (Part 2, Task 2).

---

## APIs verified

| API | Location |
|---|---|
| `Map.IsTileStaticBlocked(x, y)` (BlockedTile or WarpTile, out of bounds = blocked) | `Goose/Map.cs:455-460` |
| `Map.IsTileBlocked(ignore, x, y)` (static + occupied by another character) | `Goose/Map.cs:428-453` |
| `Map.CanReachTile` BFS with per-map stamp scratch buffers, "game thread only" | `Goose/Map.cs:462-526` |
| `Map.tiles` / `Map.characters` indexed `y * Width + x`, 1-based coords | `Goose/Map.cs:63-64`, `Goose/Map.cs:810-831` |
| `Map.CanMoveTo(character, x, y)` (adjacent + not blocked; NPCs never enter warps) | `Goose/Map.cs:284-307` |
| `Map.AddNPC` / `Map.RemoveNPC` (map list only) | `Goose/Map.cs:232-245` |
| `NPC.HandleMoveEvent` tail: step, `CanMoveTo`, `MoveTo`, `Facing`/`FaceTo`, `AddMoveEvent` | `Goose/NPC.cs:497-524` |
| `NPC.MoveTo(world, x, y)` (any tile, sends MOC/MKC/ERC, aggro checks) | `Goose/NPC.cs:530-597` |
| `NPC.NextStepTo` greedy (unchanged) | `Goose/NPC.cs:778-866` |
| `NPC.AddMoveEvent` uses `MoveSpeed` (seconds per step) | `Goose/NPC.cs:747-772` |
| `NPCMoveEvent.Ready` clears `MoveEvent` and skips non-alive NPCs | `Goose/Events/NPCMoveEvent.cs` |
| `NPCAttackEvent.Ready` skips dead NPCs | `Goose/Events/NPCAttackEvent.cs` |
| `BuffTickEvent` skips non-alive NPC targets | `Goose/Events/BuffTickEvent.cs:16` |
| NPC death path in `NPC.Attacked`: hooks, erase, `RemoveNPC` when `!ShouldRespawn`, credit, `CommunityGoals.OnNpcKilled` x2, buffs removed, drops | `Goose/NPC.cs:1137-1260` |
| `Group.GainExperience` returns the credited players (in range, level rules) | `Goose/Group.cs:232-272` |
| `NPCHandler.npcs` / `idToNPC`; `AssignNewId`; `GetNewID` loops until a free id in `(MaxPlayers, MaxNPCs)` | `Goose/NPCHandler.cs:17`, `Goose/NPCHandler.cs:287-307` |
| `NPCHandler.SpawnNPC(world, mapId, x, y, template, shouldRespawn, properties)` | `Goose/NPCHandler.cs:373-381` |
| `NPC.Properties` (from spawn `properties`) | `Goose/NPC.cs:50`, `Goose/NPC.cs:609` |
| Hand-rolled despawn in two scripts (no engine helper today) | `Goose/Data/Illutia/Scripts/Global/WanderingChest.csx:200-210`, `Goose/Data/Illutia/Scripts/NPC/EasterEvent.csx:27-40` |
| `IGlobalScript.OnLoaded` only; `BaseGlobalScript` | `Goose/Scripting/IGlobalScript.cs`, `Goose/Scripting/BaseGlobalScript.cs` |
| Global scripts registered via `ScriptHandler.TryAdd`; dictionary of `IScript` keyed by path | `Goose/GameWorld.cs:893-918`, `Goose/Scripting/ScriptHandler.cs:9,42-48` |
| `/reloadscripts` swaps `Script<T>.Object` in place (fresh instance, `OnLoaded` not re-run for existing globals) | `Goose/Scripting/ScriptHandler.cs:50-75`, `Goose/Scripting/Script.cs:27-31`, `Goose/Commands/ReloadScriptsCommand.cs` |
| `IMapScript` members; `BaseMapScript` virtual no-ops | `Goose/Scripting/IMapScript.cs`, `Goose/Scripting/BaseMapScript.cs` |
| `DimensionMap.csx` forwards every map hook to the base map's script | `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx:112-155` |
| `PlayerDropItemEvent`: bound check, `RemoveItem`, `PlaceItem`, stack-or-`AddItem`, log | `Goose/Events/PlayerDropItemEvent.cs:33-68` |
| `PickupItemEvent`: tile resolution, owner rules, item `CanPickup` gate (fail closed) | `Goose/Events/PickupItemEvent.cs:15-130` |
| `Inventory.Equip` → `EquipCore`, nested `Unequip` calls at 365/432/442; public `Unequip(EquipSlots)`; `Unequip(int)` delegates | `Goose/Inventory.cs:340-450`, `Goose/Inventory.cs:599-670` |
| Other equip callers: `Inventory.Use` (318), `DestroyItemEvent` (57), `InventoryUseEvent` (42) | as listed |
| `BaseItemScript.OnEquipEvent/OnUnequipEvent` exist but are never called (not on `IItemScript`) | `Goose/Scripting/BaseItemScript.cs:34-42` |
| `Player.Properties` is persisted (`player_properties`) - not for transient state | `Goose/Player.cs:475`, `Goose/Player.cs:1115` |
| Fixture: `TestWorldFixture.AddBaseMap/PlayerOn/CommandPlayerOn/AddBaseItemTemplate`, `CapturingPlayer.Sent` | `TestSupport/TestWorldFixture.cs:69-180` |
| `ScriptStub.For<T>(instance)` wraps an in-memory script object | `TestSupport/ScriptStub.cs` |
| NPC template shape used in tests | `Goose.Tests/NPCUnreachableTeleportTests.cs:118-138` |
| `InternalsVisibleTo` test assemblies | `Goose/Goose.csproj:20-25` |

## Threading, persistence, publication

- **Thread:** every new API runs on the game thread (event handlers, script hooks, commands). `Map`
  path-search scratch buffers follow the `CanReachTile` precedent (per-map, not thread-safe).
- **Persistence:** none. No schema changes. `Player.ScriptState` (Task 7) is deliberately not
  persisted.
- **Publication:** Task 1 defines the removal order for temporary NPCs: erase packet to range →
  clear map tile → `State = Dead` → remove buffs → `Map.RemoveNPC` → `NPCHandler.Release`. Pending
  move/attack/buff events already no-op on dead NPCs.

---

### Task 0: Test fixture NPC helpers

**Files:**
- Modify: `TestSupport/TestWorldFixture.cs`

Add two helpers used by Tasks 1, 3, 4 and by Parts 2-3:

```csharp
public NPCTemplate AddNpcTemplate(int id, string name, Action<NPCTemplate>? configure = null)
public NPC SpawnNpc(Map map, int x, int y, NPCTemplate template, bool shouldRespawn = false,
    PropertiesDictionary? properties = null)
```

`AddNpcTemplate` builds the shape in `NPCUnreachableTeleportTests.cs:118-135` (class 3, level 50,
`BaseStats = new AttributeSet { HP = 1000 }`, `CanBeKilled = true`, `Drops = []`, `Allies = []`),
applies `configure`, registers it with `World.NPCHandler.AddTemplate`, and returns it. `SpawnNpc`
calls `World.NPCHandler.SpawnNPC` and asserts non-null.

No test of its own; Task 1's tests exercise it. Commit with Task 1.

---

### Task 1: Despawn and registry release for temporary NPCs

Today a non-respawning NPC that dies is removed from its map (`NPC.cs:1173`) but never from
`NPCHandler.npcs` or `NPCHandler.idToNPC`. Ambushes, adds, the stalker and the town invasion will
spawn tens of thousands of temporary NPCs over two weeks. The leak grows memory, and `GetNewID`
(`NPCHandler.cs:287-296`) random-probes until it finds a free id, so it slows as the id space
(`MaxNPCs` 250000) fills and never terminates once it is full.

**Files:**
- Modify: `Goose/NPCHandler.cs` (add `Release`, `internal bool IsRegistered(int loginId)`)
- Modify: `Goose/NPC.cs` (call `Release` in the death path; add `Despawn`)
- Modify: `Goose/Data/Illutia/Scripts/Global/WanderingChest.csx:200-210` (use `npc.Despawn`)
- Test: `Goose.Tests/NpcDespawnTests.cs`

**API:**

```csharp
// NPCHandler
public void Release(NPC npc)          // removes from npcs; removes idToNPC[npc.LoginID] only if it maps to npc
// NPC
public void Despawn(GameWorld world)  // permanent removal of a live NPC; no-op unless Alive
```

**Mutation impact:**
- Source of truth changed: `NPCHandler.npcs`, `NPCHandler.idToNPC` (`NPCHandler.cs:17`), `Map.npcs`,
  `Map.characters`, `NPC.State`, `NPC.Buffs`.
- Important readers: `NPCHandler.NPCCount` (`NPCHandler.cs:244`), `GetNewID` (`:287`),
  `AssignNewId` (`:298`), `Map.NPCs` (`Map.cs:828`, read by `WanderingChest.Appear` and future
  ambush caps), `Map.GetCharacterAt` (blocking), `NPCMoveEvent`/`NPCAttackEvent`/`BuffTickEvent`
  (state checks).
- Derived/cached state affected: none beyond the two registries. Client view is updated by the
  `P.EraseCharacter` packet.
- Required propagation sequence for `Despawn` (game thread):
  1. return if `State != Alive`;
  2. send `P.EraseCharacter(LoginID)` to `Map.GetPlayersInRange(this)`;
  3. `Map.SetCharacter(null, MapX, MapY)`; `State = Dead`; `MoveEvent = null`; `CurrentHP = 0`;
     `ShouldRespawn = false`;
  4. remove every buff with `RemoveBuff` (same loop as the death path, `NPC.cs:1245-1255`);
  5. `Map.RemoveNPC(this)`; `world.NPCHandler.Release(this)`.
- Death path: in `NPC.Attacked`, the `else { this.Map.RemoveNPC(this); }` branch (`NPC.cs:1171-1174`)
  also calls `world.NPCHandler.Release(this)`. Respawning NPCs are untouched.
- Invariants to preserve:
  - A respawning NPC that dies stays registered and keeps its id until `AssignNewId` on respawn.
  - `Release` never removes another NPC's id mapping (id reuse after release).
  - Kill credit, drops and hooks in the death path still run for non-respawning NPCs (Release is
    after the erase packet is built, and nothing after it reads the registries).
- Observable proof required: registry membership via `IsRegistered` and `NPCCount`, map membership,
  and tile occupancy after the operation.

**Step 1: Write the failing tests** (`NpcDespawnTests`, using Task 0 helpers)

- `Killing_a_non_respawning_npc_releases_its_id_and_registry_entry`: spawn with
  `shouldRespawn: false`, kill via `npc.Attacked(player, npc.CurrentHP, world)`; assert
  `!IsRegistered(id)`, `NPCCount` dropped by 1, not in `map.NPCs`.
- `Killing_a_respawning_npc_keeps_it_registered` (adversarial: fails if Release is put in the shared
  path): `shouldRespawn: true`; assert still registered and still counted.
- `Despawn_removes_npc_from_map_tile_and_registries`: assert `map.GetCharacterAt` is null,
  `State == Dead`, not registered, not in `map.NPCs`, an `ERC` packet reached an in-range
  `CapturingPlayer`.
- `Despawn_twice_is_a_noop`: second call sends nothing and does not throw.
- `Releasing_twice_does_not_touch_other_npcs`: spawn A and B (both temporary), despawn A, call
  `Release(A)` again; B is still registered and `NPCCount` dropped by exactly 1. The owner check
  (`idToNPC[id] == npc` before removing) is what keeps a later NPC that drew A's old id registered.
- `Despawn_removes_buffs`: add a permanent buff (duration 0, `AddBaseSpellEffect`) before despawn;
  assert `npc.Buffs` empty.

**Step 2: Run to verify red**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~NpcDespawnTests`
Expected: compile failure (`Despawn`, `Release`, `IsRegistered` missing).

**Step 3: Implement** per the sequence above. Replace `WanderingChest.Despawn` body with
`npc.Despawn(world)` (keep the private wrapper or inline it); `WanderingChestScriptTests` must stay
green.

**Step 4: Run green**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~NpcDespawnTests|FullyQualifiedName~WanderingChestScriptTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add TestSupport/TestWorldFixture.cs Goose/NPCHandler.cs Goose/NPC.cs \
  Goose/Data/Illutia/Scripts/Global/WanderingChest.csx Goose.Tests/NpcDespawnTests.cs
git commit -m "feat(npc): despawn helper and registry release for temporary NPCs"
```

| Invariant | Proved by |
|---|---|
| Temporary NPC death frees id and registry | `Killing_a_non_respawning_npc_releases_its_id_and_registry_entry` |
| Respawning NPCs keep registration | `Killing_a_respawning_npc_keeps_it_registered` |
| Despawn clears tile, map list, registry, buffs | `Despawn_removes_npc_from_map_tile_and_registries`, `Despawn_removes_buffs` |
| Release is idempotent and identity-safe | `Releasing_twice_does_not_touch_other_npcs` |

---

### Task 2: A* path search on Map

**Files:**
- Modify: `Goose/Map.cs` (add `FindPath` next to `CanReachTile`)
- Test: `Goose.Tests/MapPathfindingTests.cs`

**API:**

```csharp
public List<(int X, int Y)>? FindPath(int fromX, int fromY, int toX, int toY,
    Func<int, int, bool>? isBlocked = null, int maxExpanded = 20000)
```

**Contract:**
- 4-directional, unit cost, Manhattan heuristic (admissible, so paths are shortest).
- Returns the tiles to step onto in order, excluding the start and including the goal. Returns an
  empty list when `from == to`. Returns `null` when the goal is unreachable, either endpoint is out
  of bounds or statically blocked, or more than `maxExpanded` nodes are expanded.
- A tile is passable when `!IsTileStaticBlocked(x, y)` and `isBlocked?.Invoke(x, y) != true`. The
  goal tile is exempt from `isBlocked` (callers path to an occupied target tile, e.g. a player) but
  not from static blocking. The start tile is never tested.
- Deterministic: ties on f-score broken by lower h, then by insertion order (use a counter as the
  secondary priority), so identical inputs give identical paths.
- Pure query: mutates nothing but per-map scratch buffers. Game thread only, same as `CanReachTile`
  (reuse the stamp-array technique: `long[] pathStamp`, `int[] cameFrom`, `int[] gScore` sized
  `(Width + 1) * (Height + 1)`; a new stamp per call avoids clearing).
- Uses `PriorityQueue<int, (int f, int h, int seq)>`.

**Step 1: Write the failing tests** (map built like `MapReachabilityTests.NewMap`, 20x20)

- `Open_map_returns_shortest_path_length` (from (2,2) to (6,5) → 7 steps, last = goal, each step
  adjacent to the previous).
- `Routes_around_a_wall_through_the_doorway` (wall at x=10 except y=15; from (5,5) to (15,5):
  every tile passes `!IsTileStaticBlocked`, path visits (10,15)). Adversarial: a greedy step-to
  (`NextStepTo`-style) would stall at the wall.
- `Sealed_goal_returns_null` (ring of BlockedTile around goal).
- `Statically_blocked_goal_returns_null`.
- `Same_tile_returns_empty_list`.
- `Predicate_blocks_tiles_but_not_the_goal`: `isBlocked` blocks a column except the goal tile
  which sits in that column; path exists and ends at the goal; a second call with the doorway
  tile blocked by the predicate returns null.
- `WarpTile_is_impassable` (NPCs never step on warps, matching `CanMoveTo`).
- `Expansion_limit_returns_null` (`maxExpanded: 5` on a long open route).
- `Same_inputs_give_same_path` (two calls equal).

**Step 2: Red** - `dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~MapPathfindingTests`
Expected: compile failure (`FindPath` missing).

**Step 3: Implement.**

**Step 4: Green** - same command, PASS.

**Step 5: Commit** `feat(map): A* path search`

---

### Task 3: NPC step helpers for scripted movement

Scripts that opt in to A* need to take one step along a path without duplicating the tail of
`HandleMoveEvent`. Default movement is unchanged: `HandleMoveEvent` calls the extracted method.

**Files:**
- Modify: `Goose/NPC.cs:497-524` (extract), add two methods
- Test: `Goose.Tests/NpcPathStepTests.cs`

**API:**

```csharp
public bool StepTo(Direction direction, GameWorld world)
    // the existing tail: compute (x, y); if CanMoveTo → MoveTo + Facing = direction, return true;
    // else FaceTo(direction), return false. Does NOT schedule a move event.
public Direction? PathStepTo(int x, int y, GameWorld world, Func<int, int, bool>? isBlocked = null)
    // Map.FindPath from (MapX, MapY) to (x, y); null if no path or already there; else the
    // direction of the first step. isBlocked is passed straight to FindPath.
```

`HandleMoveEvent` becomes `... this.StepTo(direction, world); this.AddMoveEvent(world);` so its
behaviour is byte-for-byte the same.

**Mutation impact:**
- Source of truth changed: none new; `StepTo` performs the same `MoveTo`/`Facing` mutation as the
  existing tail (`NPC.cs:513-521`).
- Important readers: unchanged (`NPCMoveEvent`, `MoveTo` packet fan-out).
- Derived state: none found.
- Invariants: existing NPC movement tests stay green (`NPCUnreachableTeleportTests`,
  `NpcAttackTargetLostTests`); `StepTo` never schedules events, so a script controls its cadence
  through `MoveSpeed` + `AddMoveEvent`.

**Step 1: Tests**
- `StepTo_moves_when_free_and_faces_when_blocked`.
- `PathStepTo_returns_first_step_around_wall` (wall with doorway; direction leads toward the
  doorway, not straight into the wall - adversarial against greedy).
- `PathStepTo_returns_null_when_unreachable_or_at_target`.
- `PathStepTo_respects_occupancy_predicate`: `isBlocked = (x, y) => map.GetCharacterAt(x, y) is NPC`
  with an NPC parked in a 1-wide corridor → null; without the predicate → a direction.

**Step 2-4:** red (`StepTo`/`PathStepTo` missing) → implement → green with
`--filter "FullyQualifiedName~NpcPathStepTests|FullyQualifiedName~NPCUnreachableTeleportTests|FullyQualifiedName~NpcAttackTargetLostTests"`.

**Step 5: Commit** `feat(npc): StepTo and PathStepTo for scripted movement`

---

### Task 4: Global "NPC killed" hook

The candy script needs every kill on every map, with the credited players. Map scripts only see
their own map (`OnNPCKilledEvent`), and that runs before credit is computed.

**Files:**
- Modify: `Goose/Scripting/IGlobalScript.cs`, `Goose/Scripting/BaseGlobalScript.cs`
- Modify: `Goose/Scripting/ScriptHandler.cs` (add `GetGlobalScripts`)
- Modify: `Goose/NPC.cs:1205-1240` (dispatch next to both `CommunityGoals.OnNpcKilled` calls)
- Test: `Goose.Tests/GlobalNpcKilledHookTests.cs`

**API:**

```csharp
// IGlobalScript / BaseGlobalScript (virtual no-op)
void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world);
// ScriptHandler
public List<Script<IGlobalScript>> GetGlobalScripts()   // snapshot under gate: scripts.Values.OfType<Script<IGlobalScript>>()
```

Implementation: refactor the two `world.CommunityGoals.OnNpcKilled(this, credited, world)` calls into
a private `NotifyKilled(ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)` that calls
`CommunityGoals.OnNpcKilled` then, for each global script, `script.Object.OnNPCKilled(...)` in its
own try/catch (log `"Global OnNPCKilled {0} npc {1} ({2}) Exception"`). The snapshot is taken per
kill so scripts added by `/reloadscripts` are seen, and `script.Object` is read at call time so
swapped instances are used.

**Mutation impact:**
- Source of truth changed: none (new read-only dispatch).
- Readers: global scripts (Part 2 candy).
- Ordering invariant: dispatch happens after experience and quest credit (`Group.GainExperience` /
  `Player.Killed`) and before buffs are stripped and drops roll, so `npc.Properties`, `npc.Level`,
  `npc.Map` are still valid. `npc.State` is already `Dead`.
- Pet kills pass `credited = [pet.Owner]`, same as community goals.
- No dispatch when `highest is null` (no damage entries), same as community goals.

**Step 1: Tests** (recording global script via `ScriptStub.For<IGlobalScript>`, registered with
`World.ScriptHandler.TryAdd`)
- `Solo_kill_dispatches_with_killer_credited`.
- `Group_kill_dispatches_once_with_credited_members_in_range` (one member far away is not in
  `credited`).
- `Throwing_global_script_does_not_stop_others_or_the_kill` (two scripts, first throws; second
  still called; NPC still dead and drops/credit unaffected).
- `Non_respawning_npc_still_has_properties_at_dispatch` (spawn with properties
  `{"candy": "ambush"}`; recorded properties match) - guards against moving the dispatch after
  Task 1's release or a future cleanup.

**Step 2-4:** red (interface member missing) → implement → green
(`--filter FullyQualifiedName~GlobalNpcKilledHookTests`), then run the whole fast project because the
interface change touches every global script compile path:
`dotnet test Goose.Tests/Goose.Tests.csproj --no-restore`.

**Step 5: Commit** `feat(scripting): global OnNPCKilled hook with credited players`

---

### Task 5: Map hooks for dropping and picking up items

Used by the basement pentagram: candles dropped on the five points register, and placed candles
can't be picked up.

**Files:**
- Modify: `Goose/Scripting/IMapScript.cs`, `Goose/Scripting/BaseMapScript.cs`
- Modify: `Goose/Events/PlayerDropItemEvent.cs:33-68`, `Goose/Events/PickupItemEvent.cs:80-100`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx` (forward the three new hooks)
- Test: `Goose.Tests/MapItemHookTests.cs`

**API (IMapScript + BaseMapScript virtual defaults returning null / no-op):**

```csharp
string? CanPlayerDropItem(Map map, Player player, Item item, long stack, GameWorld world);
void OnPlayerDroppedItem(Map map, Player player, ItemTile tile, GameWorld world);
string? CanPlayerPickupItem(Map map, Player player, ItemTile tile, GameWorld world);
```

**Sequencing:**
- Drop: after the stack/bound validation (`PlayerDropItemEvent.cs:33-40`) and **before**
  `Inventory.RemoveItem`, call `CanPlayerDropItem`. Non-null → `P.ServerMessage(refusal)` and return
  with inventory unchanged. Exception → fail closed with "You cannot drop that here." and log (same
  policy as the pickup gate, `PickupItemEvent.cs:96-104`).
- After the tile is final (either the stacked `maptile` or the newly added `tile`), call
  `OnPlayerDroppedItem` with **that** tile (the stack branch must pass `maptile`, whose coordinates
  are the real ones; `PlaceItem` may have moved the drop). Exception → log only.
- Pickup: after the owner/time rules and before the gold branch, call `CanPlayerPickupItem` for any
  item (gold included). Non-null → message and return; exception → fail closed with "You cannot
  pick that up right now.".

**Mutation impact:**
- Source of truth: `Inventory` slots and `Map.items`/`Map.tiles` (unchanged code paths).
- Invariant: a refused drop leaves inventory and map untouched; a refused pickup leaves the tile on
  the map and inventory untouched.
- Readers: `ClearMapItemsEvent` sweep still removes placed candles after 3 hours (design intent).

**Step 1: Tests** (`ScriptStub.For<IMapScript>` recording script on `map.Script`; drive the events
with `Data = "DRP<slot>,<stack>"` / pickup event on a `CapturingPlayer`)
- `Refused_drop_keeps_item_in_inventory_and_sends_message`.
- `Dropped_item_hook_receives_final_tile_coordinates` (occupy the player's tile with a
  non-stackable item so `PlaceItem` moves the drop; hook tile X/Y equal the moved coordinates -
  adversarial against passing the pre-placement tile).
- `Dropped_item_hook_receives_existing_tile_when_stacking`.
- `Throwing_drop_gate_fails_closed`.
- `Refused_pickup_leaves_tile_on_map`.
- `Pickup_gate_runs_for_gold`.
- `No_map_script_behaves_as_before` (drop then pickup round trip).

**Step 2-4:** red → implement → green
(`--filter "FullyQualifiedName~MapItemHookTests"`); then
`dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~DimensionMapScriptTests`
to prove the forwarding script still compiles and forwards.

**Step 5: Commit** `feat(map-scripts): drop and pickup hooks with refusal`

---

### Task 6: Wire the item script equip and unequip events

`BaseItemScript` already declares `OnEquipEvent` and `OnUnequipEvent` (`BaseItemScript.cs:34-42`),
but they are not on `IItemScript` and nothing calls them. The Party Costume and mask item scripts
use them for the costume check (Part 2 Task 5).

**Files:**
- Modify: `Goose/Scripting/IItemScript.cs` (add both members; `BaseItemScript` already implements them)
- Modify: `Goose/Inventory.cs:340-450`, `Goose/Inventory.cs:599-670`
- Test: `Goose.Tests/ItemEquipHookTests.cs`

**API (IItemScript):**

```csharp
void OnEquipEvent(Player player, Item item, GameWorld world);
void OnUnequipEvent(Player player, Item item, GameWorld world);
```

**Implementation:**
- Rename the body of `public bool Unequip(EquipSlots, GameWorld)` to
  `private bool UnequipCore(EquipSlots, GameWorld)`. `EquipCore` and `UnequipConflictingSlots` call
  `UnequipCore` (lines 365, 432, 442).
- While a public `Equip` or `Unequip` runs, `EquipCore`/`UnequipCore` append
  `(Item item, bool equipped)` to a pending list instead of calling scripts. Each public method
  flushes the list once it finishes (in the order the changes happened), calling
  `item.Script?.Object.OnUnequipEvent/OnEquipEvent` in its own try/catch that logs
  (`"Item OnEquipEvent {0} ({1}) player {2} ({3}) Exception"`). A failed `Equip` still flushes the
  unequips that did happen.
- Why deferred: swapping a costume for a mask unequips the costume inside `EquipCore` before the
  mask is on. A costume check run at that moment would see neither and warn the player. Flushing
  after the public call means every hook sees the final loadout.

**Mutation impact:**
- Source of truth: `Inventory.equipped` (unchanged logic).
- Readers: item scripts (Part 2 costume and masks).
- Invariants: each item that left or entered a slot gets exactly one call; every call observes the
  final equipment state; a throwing item script does not undo or block the equip.

**Step 1: Tests** (`CompileItemScript` with a recording script that logs the call and the helm slot's
template id at call time)
- `Equip_calls_OnEquipEvent_once`.
- `Unequip_calls_OnUnequipEvent_once`; `Unequip_of_empty_slot_calls_nothing`.
- `Swapping_helms_calls_unequip_then_equip_both_seeing_the_new_helm` (adversarial: fails if the
  nested unequip calls the script immediately).
- `Two_hander_over_shield_calls_shield_unequip_and_weapon_equip`.
- `Throwing_item_script_does_not_block_equip`.

**Step 2-4:** red → implement → green
(`--filter "FullyQualifiedName~ItemEquipHookTests|FullyQualifiedName~QuestIconMutationRefreshTests|FullyQualifiedName~CostumeHideSlotsTests|FullyQualifiedName~EquipGraphicCommandTests|FullyQualifiedName~ItemScriptHookTests"`).

**Step 5: Commit** `feat(items): call item script equip and unequip events`

---

### Task 7: Transient per-player script state

Event state that must be shared between scripts (ambush cooldown read by both the house and the
basement, stalker grace, "in a chase") cannot live in script statics: every script compilation gets
its own copy (`ScriptLoadDirectiveTests` shape 8), and `/reloadscripts` replaces instances. It must
not go in `Player.Properties`, which is saved to the database.

**Files:**
- Modify: `Goose/Player.cs` (near `Properties`, line 475)
- Test: `Goose.Tests/PlayerScriptStateTests.cs`

**API:**

```csharp
public Dictionary<string, long> ScriptState { get; } = new();
```

`Player` itself is never JSON-serialized; the save path serializes only `Properties`
(`Player.cs:1011`), so no attribute is needed.

Values are `long` (ticks, counters, ids) so a reloaded script can read what the previous compilation
wrote; script-defined types would not survive a reload.

**Mutation impact:**
- Source of truth: in-memory only, lifetime of the `Player` object (cleared on logout because the
  object is dropped).
- Readers: Part 2/3 scripts only.
- Invariant: never persisted - the save path writes `player_properties` from
  `JsonHelper.Serialize(this.Properties.Clone())` only (`Player.cs:1011`).

**Step 1: Tests**
- `ScriptState_starts_empty_and_holds_values`.
- `ScriptState_does_not_leak_into_properties` (set a ScriptState key; `JsonHelper.Serialize(player.Properties.Clone())`
  does not contain it).

**Step 2-4:** red → implement → green (`--filter FullyQualifiedName~PlayerScriptStateTests`).

**Step 5: Commit** `feat(player): transient script state`

---

### Task 8: Setting to turn off player-count XP scaling

`PlayerCountExperienceModifierUpdateEvent` is scheduled unconditionally (`GameWorld.cs:450-453`) and
every `IdleTimeout` (600 s) sets `world.ExperienceModifier = Settings.ExperienceModifier + (count / PlayerCountExperienceModifierInterval) * PlayerCountExperienceModifier`
(integer division, so with the shipped interval of 1000 the bonus term is 0). Any script that changes
`world.ExperienceModifier` is overwritten within 10 minutes. With scaling off, nothing but startup
(`GameWorld.cs:242`) and scripts writes it, and the tier 1 bonus can use it directly (Part 2 Task 2).

**Files:**
- Modify: `Goose/GooseSettings.cs` (near `PlayerCountExperienceModifier`, line 142-143)
- Modify: `Goose/GooseSettings.json` (add `"PlayerCountExperienceModifierEnabled": false`)
- Modify: `Goose/GameWorld.cs:450-453` (schedule only when enabled)
- Test: `Goose.Tests/PlayerCountExperienceSettingTests.cs`

**API:** `public bool PlayerCountExperienceModifierEnabled { get; set; } = true;`

Default `true` keeps existing behaviour for any settings file without the key; the shipped
`GooseSettings.json` sets it to `false`.

**Mutation impact:**
- Source of truth: `world.ExperienceModifier` (`GameWorld.cs:196`).
- Readers: `Player.AddExperience` (`Player.cs:1943-1955`). Pets read `Settings.ExperienceModifier`
  (`Pet.cs:885`), so they are unaffected either way.
- Invariant: with the setting off, a value written to `world.ExperienceModifier` stays until
  something writes it again.
- The scheduling sits in world startup, which needs a database, so extract lines 450-453 into
  `internal void SchedulePlayerCountExperienceModifier()` (returns early when disabled) and call it
  from the same place. Tests call it directly on a `TestWorldFixture` world.

**Step 1: Tests** (inspect the queue with `EventHandler.Count` / `Peek()`, `EventHandler.cs:315-317`)
- `Scaling_event_is_not_scheduled_when_disabled` (`Count` unchanged).
- `Scaling_event_is_scheduled_when_enabled` (`Count` + 1, `Peek()` is a
  `PlayerCountExperienceModifierUpdateEvent`).
- `Shipped_settings_disable_scaling` (load `Goose/GooseSettings.json` the way
  `GooseSettingsLoaderTests` does; the flag is false).

**Step 2-4:** red → implement → green (`--filter FullyQualifiedName~PlayerCountExperienceSettingTests`).

**Step 5: Commit** `feat(settings): option to disable player-count XP scaling`

---

## Final checks

- `dotnet test Goose.sln --no-restore` - all fast tests green.
- `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore` - the shipped
  scripts (Dimensions, WanderingChest) still compile against the changed interfaces.

## Red-team notes

- **Interface additions break scripts that implement `IMapScript`/`IGlobalScript` directly.** All
  shipped scripts derive from the base classes (`grep ": IMapScript\|: IGlobalScript"` finds none),
  so default implementations on the base classes are enough.
- **Dimension clones:** every new map hook (Task 5) is forwarded by `DimensionMap.csx`, so a base map's
  script sees clone maps too. Part 2 scripts must check `map.ID` against their configured base map.
- **Despawn vs. kill credit:** `Despawn` grants no experience, drops or candy and does not call
  `OnKilledEvent`/`OnNPCKilledEvent`/`OnNPCKilled`. That is intended (ambush timeouts, stalker vanish).
- **A* cost:** the house map is about 100x90; one search is at most ~9k expansions. Part 3 repaths
  the stalker every step (one stalker per target, at most 3) and the Weeper every step. Well under
  1 ms each; `maxExpanded` bounds the worst case.
