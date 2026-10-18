# Halloween Event Part 3 (Boss Scripts) Implementation Plan

**Goal:** Script the Halloween bosses: damage-taken rules (the Weeper's cap, the Groundskeeper's
curve), % max-HP hits, bleed, the Weeper's patrol/leash/lunge/footsteps, the stalker's chase, and
the Groundskeeper's phases, HP scaling, lights out and adds.

**Architecture:** NPC scripts (`Scripts/NPC/*.csx`) overriding `OnSpawnEvent`, `OnMoveEvent`,
`OnAttackEvent`, `OnKilledEvent`; spell effect scripts (`Scripts/Spell/*.csx`) using the existing
`InterceptDamage` hook on a permanent buff the boss gives itself. Movement uses Part 1's
`NPC.PathStepTo`/`StepTo`. Config, flags, feed, candy and stalker spawning come from Part 2's
`Scripts/Global/Halloween/` files and `halloween.json`.

**Tech Stack:** .NET 10, Roslyn C# scripting, xUnit (`Goose.Tests`).

Design: artifact "Haunted House Event" (https://claude.ai/artifact/PnXDddVa1NG5nFqmjL48vK).
Depends on Part 1 (all tasks) and Part 2 Tasks 1, 3, 7.

**Repo rule (AGENTS.md):** no new comments or doc strings unless the "why" is non-obvious.

---

## APIs verified

| API | Location |
|---|---|
| NPC script hooks; base `OnMoveEvent` → `HandleMoveEvent`, base `OnAttackEvent` → `HandleAttackEvent` | `Goose/Scripting/BaseNPCScript.cs` |
| Boss override precedent (`OnAttackEvent`, `ScriptStore`, `ScriptTimerEvent`, `target.Attacked`) | `Goose/Data/Illutia/Scripts/NPC/GrandGloopBoss.csx` |
| `NPCAttackEvent` only fires while `AggroTarget` is set; clears `AttackEvent` first | `Goose/Events/NPCAttackEvent.cs` |
| `NPC.AddAttackEvent` (needs aggro, `AttackSpeed > 0`, `AttackRange > 0`); `AddMoveEvent` (needs `MoveSpeed > 0`) | `Goose/NPC.cs:1428-1452`, `Goose/NPC.cs:747-772` |
| `NPC.AddAggro(player, value, wastaunt, world)`; kill credit sums only `Aggro.Damage` | `Goose/NPC.cs:931-1003`, `Goose/NPC.cs:1180-1200` |
| `NPC.Attacked`: script `OnAttackedEvent(raw)` → `CanBeKilled` miss → `DamageIntercept.Apply` → HP/aggro → death path | `Goose/NPC.cs:1088-1135` |
| `DamageIntercept.Apply` runs every buff's `InterceptDamage(buff, attacker, raw, current)` in order | `Goose/DamageIntercept.cs` |
| `ShardOfInvincibility` intercept precedent | `Goose/Data/Illutia/Scripts/Spell/ShardOfInvincibility.csx` |
| `NPC.AddBuff`: duration 0 → no expiry event (permanent); same effect renews instead of stacking | `Goose/NPC.cs:1571-1600` |
| `world.SpellHandler.GetSpellEffect(id)` | used in `GrandGloopBoss.csx:93` |
| `Player.Attacked(npc, damage)`: intercept (player shields), GM miss, dodge roll, death → warp to bind | `Goose/Player.cs:2066-2140` |
| `NPC.Attack` applies AC/DR/crit (what % hits bypass) | `Goose/NPC.cs:1458-1505` |
| `NPC.TemporaryMaxHP` overrides `MaxHP` | `Goose/NPC.cs:113-128` |
| `NPC.MoveTo(world, x, y)` teleports to any tile with packets (used by `TeleportToAggro`) | `Goose/NPC.cs:530`, `Goose/NPC.cs:1374-1392` |
| `NPC.OnMeleeAttack` casts a buff's `OnAttack` spell on hit | `Goose/NPC.cs:1805-1818` |
| `P.Attack(npc)`, `P.VitalsPercentage(npc)`, `P.SpellPlayer`, `P.EraseCharacter`, `P.MapAmbience` | `Goose/Packets.cs:400,479,464` |
| `MapAmbience.Broadcast(map, world)` restores the map's real lighting | `Goose/MapAmbience.cs:98-101` |
| `Player.CalculateMoveSpeed()` → current ms per tile (base 320, mounts ~200-220); client speed = 1000/value tiles/s | `Goose/Player.cs:1710-1714`, client `CharacterMotionTests.cs:37` |
| Part 1: `Map.FindPath`, `NPC.StepTo`, `NPC.PathStepTo`, `NPC.Despawn`, `Player.ScriptState` | Part 1 Tasks 1-3, 7 |
| Part 2: `HalloweenConfig.Get`, `Halloween.*` helpers, `GiveCandy`, `SpawnStalker` properties | Part 2 Tasks 1, 3, 7 |

## Config additions (`halloween.json`)

```json
"effects": { "weeperCap": 0, "groundskeeperCurve": 0, "ghostSnareOnHit": 0 },
"weeper": {
  "start": [38, 36], "capPerHit": 100000,
  "hitPercent": 0.08, "stragglerRadius": 5, "stragglerMultiplier": 2,
  "lungeSeconds": 15, "lungeRadius": 6,
  "bleedPercentPerStack": 0.01, "bleedMaxStacks": 5, "bleedSeconds": 10, "bleedTickSeconds": 2,
  "leashRadius": 12, "leashSeconds": 10, "leashMaxDistance": 40, "resetSeconds": 30,
  "footstepRadius": 10, "footstepSeconds": 8,
  "loops": {
    "upstairs": [[33,36],[33,24],[10,24],[10,11],[30,11],[30,20],[52,20],[52,12],[64,12],[64,20],[80,20],[80,12],[92,12],[92,20],[33,20],[33,36]],
    "service":  [[30,40],[22,40],[22,45],[4,45],[4,35],[22,35],[22,46],[19,47],[19,53],[4,53],[4,58],[15,58],[15,60],[22,58],[22,53],[19,52],[19,47],[22,46],[22,40],[30,40]],
    "party":    [[50,36],[50,39],[58,39],[58,34],[98,34],[98,58],[74,58],[74,69],[56,69],[56,86],[82,86],[82,69],[74,69],[74,58],[58,58],[58,40],[50,40],[50,36]]
  }
},
"stalkerChase": { "chaseSeconds": 25, "startDelaySeconds": 1, "speedOffset": 0.02,
                  "warnRadius": 3, "warnSeconds": 5 },
"groundskeeper": {
  "hpPerPlayer": 8000000, "phases": [0.75, 0.5, 0.25, 0.10],
  "hitPercent": 0.10, "bigHitPercent": 0.20, "bigHitEvery": 4, "bigHitWarnSeconds": 1.5,
  "enrageAttackSpeed": 1.5, "bloodyDamageMultiplier": 0.25,
  "lightsOutSeconds": 6, "lightsOutAlpha": 950,
  "adds": { "placeSettings": [0, 8], "ghosts": [0, 6], "bloody": [0, 2] },
  "emptyDespawnSeconds": 60,
  "curve": { "floor": 10000, "band": 70000, "exponent": 0.4 }
}
```

The Weeper loops are the routes validated on Map10048 (they walk with greedy movement, so they are
safe with A*; the data plan can thin them). `adds` entries are `[templateId, count]`. The Groundskeeper
hit numbers are starting values (the design fixes "% of max HP" but not the numbers); tune on the test
server. Spell effect rows (`effects`) and boss templates are created by the data plan.

## Threading, persistence, publication

- Game thread only. No persistence. Boss phase and mode live in `NPC.Properties` (survive reload);
  timing and bleed state live in `NPC.ScriptStore` (rebuilt if missing; losing it on reload resets
  a bleed or a lunge timer, which is harmless).
- Every boss adds its permanent intercept buff in `OnSpawnEvent`. The death path removes buffs
  (`NPC.cs:1245-1255`); respawn re-adds them.

---

### Task 1: Damage-taken scripts (Weeper cap, Groundskeeper curve)

**Files:**
- Create: `Spell/HalloweenDamageCap.csx`, `Spell/HalloweenDamageCurve.csx` (`#load` Part 2 config)
- Modify: `Global/Halloween/HalloweenCommon.csx` (add `AddPermanentBuff(npc, effectId, world)`)
- Test: `Goose.Tests/Halloween/HalloweenDamageRuleTests.cs`

**Rules (`InterceptDamage(buff, attacker, raw, current, world)`):**
- `current <= 0` (heals, misses) → return `current` unchanged.
- Cap: `Math.Min(current, weeper.capPerHit)`.
- Curve: `current <= floor` → `current`; else
  `floor + band * Math.Pow((current - floor) / (double)band, exponent)`, rounded. Then, if any Bloody
  Ghost add of this Groundskeeper is alive on the map (`Properties` `gkAdd == "bloody"`), multiply by
  `bloodyDamageMultiplier`.
- Both read config through `HalloweenConfig.Get(world)`; a disabled config passes damage through.

`AddPermanentBuff`: look up the effect; return if missing or 0; add
`new Buff { Caster = npc, Target = npc, SpellEffect = effect }` with `npc.AddBuff`.

**Step 1: Tests** (spell effect added with `AddBaseSpellEffect` + compiled script; NPC with the buff
attacked by a player)
- `Cap_limits_each_hit_to_100k` (raw 3M → HP drops 100k; raw 50k → 50k).
- `Curve_examples_match_design` (theory: 5k→5k, 20k→42k, 40k→60k, 80k→80k, 300k→134k,
  800k→195k, 3M→324k, each ±1k).
- `Curve_is_continuous_at_floor` (10k → 10k, 10,001 → ≈10k + small) - adversarial against an
  off-by-floor formula.
- `Heals_pass_through`.
- `Bloody_ghosts_alive_reduce_curve_damage`.
- `Disabled_config_passes_damage_through`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): boss damage cap and curve`

---

### Task 2: Shared boss helpers (% hits, stragglers, bleed, compass)

**Files:**
- Create: `Global/Halloween/HalloweenBoss.csx` (`public static class HalloweenBoss`)
- Test: `Goose.Tests/Halloween/HalloweenBossHelperTests.cs`

**API:**

```csharp
public static long PercentHit(NPC npc, Player target, double percent, GameWorld world)
    // damage = ceil(target.MaxHP * percent); send P.Attack(npc) to range; target.Attacked(npc, damage).
    // Bypasses AC and damage reduction on purpose (NPC.Attack applies them; this does not).
    // Player shields (intercept buffs), GM immunity and dodge still apply inside Player.Attacked.
public static bool IsStraggler(Player target, double radius)
    // no other Ready, non-GM player on the same map within Chebyshev radius
public static void AddBleed(NPC npc, Player target, long now, GameWorld world)
public static void TickBleeds(NPC npc, long now, GameWorld world)
public static string Compass(int fromX, int fromY, int toX, int toY)   // "north", "south-east", …
public static List<Player> PlayersNear(NPC npc, double radius)          // Ready, non-GM, same map
```

Bleed state: `npc.ScriptStore` holds a `BossState` (defined in this file) with
`Dictionary<int playerId, (int stacks, long expiresAt)>`. `AddBleed` adds one stack (max
`bleedMaxStacks`) and sets `expiresAt = now + bleedSeconds`. `TickBleeds` (called by the boss's own
2 s timer, Task 3) hits each entry for `stacks * bleedPercentPerStack` of max HP via `PercentHit`
without the attack packet, and drops entries that expired, whose player left the map, or who died
(`State != Ready` or map changed after `Attacked`).

**Step 1: Tests**
- `PercentHit_ignores_armour` (target with huge AC loses exactly 8% of max HP; dodge disabled via
  settings `MaxDodgeChance = 0`).
- `IsStraggler_true_alone_false_with_ally_within_5_ignores_gms`.
- `Bleed_stacks_to_five_and_refreshes_duration` (adversarial: sixth stack stays at 5).
- `Bleed_expires_after_10s_without_hits`.
- `Bleed_stops_when_target_leaves_map`.
- `Compass_names_eight_directions`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): boss helpers for % hits and bleed`

---

### Task 3: The Weeper

**Files:**
- Create: `NPC/Weeper.csx`
- Test: `Goose.Tests/Halloween/WeeperScriptTests.cs`

The Weeper is a normal respawning spawn row (data plan: one spawn at `weeper.start`, respawn
20-30 min, `AttackSpeed` 2, `AttackRange` 1, `AggroRange` 6, `MoveSpeed` 1.2, 12M HP, 100k XP).

**State:** `NPC.Properties["weeperLoop"]` (loop name) and `["weeperWaypoint"]` (index);
`ScriptStore` = `WeeperState` (`lastCombatAt`, `nextLungeAt`, `nextFootstepsAt`, `leashStartedAt`,
`leftRouteAt (x, y)`, bleed state from Task 2).

**OnSpawnEvent:** `AddPermanentBuff(weeperCap)`; pick a loop other than the previous one (stored on
`Properties["weeperLastLoop"]`, carried over because respawn reuses the same NPC object); waypoint 0;
`world.SendToMap(houseMap, P.Notice(…"A low weeping echoes through the house…"))`; start a 2 s
`ScriptTimerEvent` loop that, while the NPC is alive, calls `TickBleeds` and the footsteps check, and
stops when the NPC is dead.

**OnMoveEvent(npc, world)** (does not call `HandleMoveEvent`; always ends with `npc.AddMoveEvent`):
1. **Leash/reset:**
   - In combat (`AggroTarget != null`): if no Ready player within `leashRadius` → start or continue
     `leashStartedAt`; after `leashSeconds`, or if Manhattan distance from `leftRouteAt` exceeds
     `leashMaxDistance` → clear `AggroTargetToValue`, `AggroTarget = null`, set
     `lastCombatAt = now`.
   - Out of combat and `now - lastCombatAt >= resetSeconds` and `CurrentHP < MaxHP` →
     `CurrentHP = MaxHP`, send `P.VitalsPercentage` to range, clear bleeds he applied? (no - bleeds
     are on players and expire on their own).
2. **Chase:** `AggroTarget` set → record `leftRouteAt` on first chase step; if not adjacent,
   `PathStepTo(target.MapX, target.MapY, isBlocked: tile occupied by any character other than the
   target)` → `StepTo`.
3. **Patrol:** go to the current waypoint with `PathStepTo` (occupancy-aware); on arrival advance;
   at the end of a loop (back at the start), pick a different loop.
4. After leaving combat, the next patrol target is the **nearest** waypoint of the current loop
   (Manhattan), then the loop continues from there.

**OnAttackEvent(npc, world)** (does not call `HandleAttackEvent`; always ends with
`npc.AddAttackEvent`):
- Stunned → return after rescheduling.
- **Lunge** (in combat, `now >= nextLungeAt`): pick a random other player from
  `PlayersNear(npc, lungeRadius)`; `MoveTo` a free tile adjacent to them; switch aggro with
  `npc.AddAggro(p, npc.AggroValue.Damage + npc.AggroValue.Taunt + 1, wastaunt: true, world)` (taunt,
  so kill credit, which sums `Damage` only, is unaffected); feed "The Weeper lunges at {name}!";
  `nextLungeAt = now + lungeSeconds`.
- **Swing:** target adjacent (`AttackRange`) → `pct = hitPercent * (IsStraggler ? stragglerMultiplier : 1)`;
  `PercentHit`; `AddBleed`; `lastCombatAt = now`.

**Footsteps** (from the 2 s loop): every `footstepSeconds`, each Ready player within
`footstepRadius` who is not in his `AggroTargetToValue` gets
"Something is dragging its feet… to the {Compass(player → weeper)}.".

**OnAttackedEvent:** `lastCombatAt = now`.

**Step 1: Tests** (house-like map with walls; Weeper template with this script; FixedRandom)
- `Spawn_adds_cap_buff_and_announces`.
- `Patrol_reaches_waypoints_around_walls` (drive `OnMoveEvent` N times; visits waypoint 1 through a
  doorway - adversarial vs greedy).
- `Next_loop_differs_from_previous`.
- `Swing_takes_8_percent_and_16_for_stragglers`.
- `Swing_adds_bleed_stack`.
- `Lunge_moves_next_to_another_player_and_switches_target_without_changing_damage_credit`
  (`AggroTargetToValue[p].Damage` unchanged).
- `Leash_drops_aggro_after_10s_with_nobody_near` and `Leash_drops_aggro_past_40_tiles_off_route`.
- `Heals_to_full_after_30s_out_of_combat_not_before`.
- `Returns_to_nearest_waypoint_after_leash`.
- `Footsteps_reach_nearby_idle_players_with_direction_not_fighters`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): the Weeper`

---

### Task 4: The stalker

**Files:**
- Create: `NPC/Stalker.csx`
- Test: `Goose.Tests/Halloween/StalkerScriptTests.cs`

Spawned only by Part 2 `Halloween.SpawnStalker` with `stalkerTarget` (PlayerID), `stalkerMode`
(`chase`/`hunt`), `spawnedAt`. Template (data plan): `CanBeKilled = false` (hits show as misses,
`NPC.cs:1105-1114`), `AttackSpeed = 0` (never schedules attacks), `AggroRange = 0`, `MoveSpeed` any
positive value (the script overwrites it every step).

**OnSpawnEvent:** set `npc.MoveSpeed = startDelaySeconds` so the first move event waits 1 s.

**OnMoveEvent** (always reschedules with `AddMoveEvent` unless it ended the chase):
1. Resolve the target: a Ready player on `npc.Map` with that `PlayerID`. Missing (logged out,
   warped, left the map, died) → `EndChase(survived: false)`.
2. `chase` mode: target inside the Foyer room (Part 2 `RoomAt`) → `EndChase(false)` with no message
   to the target beyond "The revving stops at the doorway.". `now - spawnedAt >= chaseSeconds` →
   `EndChase(true)`.
3. `hunt` mode: target back in costume (`IsInCostume`) → `EndChase(false)`.
4. Adjacent to the target (Chebyshev 1) → `Caught()`.
5. Speed: `npc.MoveSpeed = target.CalculateMoveSpeed() / 1000.0 + (mode == chase ? +speedOffset : -speedOffset)`,
   floored at 0.05.
6. Step: `dir = npc.PathStepTo(tx, ty, isBlocked: (x, y) => map.GetCharacterAt(x, y) is NPC)` - NPCs
   are obstacles, players are not. Compute the next tile; if a **non-target** player stands there →
   `BodyBlock(that player)` and do not move this step. Else `StepTo(dir)`. No path → stay (repath
   next step).
7. Warning: within `warnRadius` and `now >= hw.stalkerWarnAt` → feed target "He's right behind
   you…", `hw.stalkerWarnAt = now + warnSeconds`.

**Caught():** `chase` → warp target to `foyerSpawn`, feed "You wake up in the foyer, shaking.";
`hunt` → warp to the grove at `groveExit`, feed "He throws you out of the front door.". Then
`EndChase(false)`.

**BodyBlock(p):** warp `p` to the grove at `groveExit`, feed `p` "The chainsaw catches you. You land
in the pumpkin patch.".

**EndChase(survived):** `survived` → feed "The revving fades into the walls." and
`Halloween.GiveCandy(target, cfg.candy.stalkerSurvived)`; in `chase` mode set
`hw.stalkerGraceUntil = now + graceSeconds` on the target (also when caught or escaped); clear
`hw.stalkerNpc`; `npc.Despawn(world)` (no kill credit, no drops, no candy from the kill path).

**Step 1: Tests**
- `Waits_one_second_then_follows_target_through_a_doorway`.
- `Speed_tracks_target_mount_each_step` (target speed 200 → `MoveSpeed` 0.22; hunt → 0.18).
- `Players_do_not_block_his_path_npcs_do` (an NPC in a 1-wide corridor forces a detour or a wait;
  a player in the corridor gets body-blocked).
- `Body_blocker_is_thrown_to_grove_and_target_is_not`.
- `Catching_in_chase_sends_target_to_foyer_and_sets_grace`.
- `Catching_in_hunt_throws_target_out`.
- `Surviving_25s_gives_10_candy_and_despawns` (no candy from the kill path: it is a despawn).
- `Foyer_ends_chase_without_candy`.
- `Costume_back_on_ends_hunt_without_candy`.
- `Target_logout_ends_chase`.
- `Hits_on_him_miss` (`CanBeKilled = false`).

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): the stalker`

---

### Task 5: The Groundskeeper

**Files:**
- Create: `NPC/Groundskeeper.csx`
- Test: `Goose.Tests/Halloween/GroundskeeperScriptTests.cs`

Spawned by Part 2's midnight summon (`shouldRespawn: false`). Template (data plan): melee,
`AttackSpeed` 2.5, `AttackRange` 1, large `AggroRange`.

**State:** `Properties["gkPhase"]` (0-4, next phase index), `ScriptStore` = swing counter, big-hit
pending flag, `lastPlayerSeenAt`.

**OnSpawnEvent:** `AddPermanentBuff(groundskeeperCurve)`; `Rescale(npc, fraction: 1.0)`; announce
server-wide "[Event] The Groundskeeper has risen in the basement!"; start a 5 s loop for the empty
check (below).

**Rescale(npc, fraction):** `n = max(1, PlayersNear(npc, Map.RANGE_X).Count)`;
`npc.TemporaryMaxHP = hpPerPlayer * n`; `npc.CurrentHP = (long)(fraction * npc.MaxHP)`; send
`P.VitalsPercentage(npc)` to range.

**Phase check** (start of every `OnAttackEvent`): while `gkPhase < phases.Length` and
`CurrentHP / MaxHP <= phases[gkPhase]`, run phase `gkPhase` and increment. A burst that skips a
threshold runs each skipped phase in order.

| Phase | Threshold | Actions |
|---|---|---|
| 0 | 75% | `Rescale(current fraction)`; lights out; vanish behind a random player and switch aggro (taunt, as the Weeper's lunge); spawn `placeSettings` adds |
| 1 | 50% | `Rescale`; vanish again; spawn `ghosts` adds, each given the `ghostSnareOnHit` permanent buff (an `OnAttack` effect, so `NPC.OnMeleeAttack` applies the snare on hit) |
| 2 | 25% | `Rescale`; vanish again; spawn `bloody` adds (damage reduction comes from Task 1's curve check) |
| 3 | 10% | enrage: `npc.AttackSpeed = enrageAttackSpeed` |

Adds spawn on free tiles within 6 of the boss, `shouldRespawn: false`, properties
`{"gkAdd": "placeSettings"|"ghosts"|"bloody"}` (no candy tag → no candy, Part 2 Task 3), and get aggro
on a random nearby player. Bloody Ghost adds use their own template id with no drops (data plan), so
"drops nothing when the Groundskeeper spawns it" holds without engine changes.

**Lights out:** `world.SendToMap(map, P.MapAmbience(lightsOutAlpha, lightsOutAlpha, tint, fill, 0, 0, weather))`
using the map's current `Ambience.Lighting` colours and `CurrentWeather`; after `lightsOutSeconds`
a timer calls `map.Ambience.Broadcast(map, world)`. Players who enter mid-blackout see normal light,
which is acceptable.

**OnAttackEvent swing:** every `bigHitEvery`-th swing is a big hit: feed the range "The
Groundskeeper revs his chainsaw!" and schedule the `bigHitPercent` hit after `bigHitWarnSeconds` on
whoever is adjacent then (none → nothing); other swings use `hitPercent` with stragglers doubled
(Task 2). Ends with `AddAttackEvent`.

**Empty basement:** the 5 s loop despawns the boss and all `gkAdd` NPCs if no Ready non-GM player has
been on the map for `emptyDespawnSeconds`, with "The Groundskeeper returns to the dark." to the
map. Otherwise a failed attempt would leave ambushes switched off until a restart.

**OnKilledEvent:** despawn every live `gkAdd` NPC on the map; server-wide "[Event] The
Groundskeeper has fallen!". Quest credit (Every Midnight), XP, candy (config `byTemplate`) and drops
come from the normal kill path and data.

**Step 1: Tests**
- `Spawn_scales_hp_by_players_present`.
- `Crossing_75_runs_phase_once_with_rescale_and_adds` (HP fraction preserved after rescale).
- `Burst_past_two_thresholds_runs_both_phases_in_order` (adversarial).
- `Ghost_adds_get_snare_buff`; `Bloody_adds_tagged_and_reduce_damage_while_alive` (with Task 1 script).
- `Lights_out_sends_dark_packet_then_restores`.
- `Big_hit_is_warned_then_lands_for_20_percent`.
- `Enrage_speeds_up_swings`.
- `Despawns_with_adds_when_basement_empty_for_60s`.
- `Death_despawns_adds`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): the Groundskeeper`

---

## Final checks

- `dotnet test Goose.sln --no-restore` and `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore`.
- Manual smoke after the data plan: `/halloween weeper` (watch him patrol and leash), `/halloween
  stalker` (outrun him, then let him catch you, then mount and check he keeps pace), `/halloween
  groundskeeper` with two players (HP ~16M, phases at 75/50/25/10).

## Red-team notes

- **Kill credit:** lunges and phase switches use taunt aggro; kill credit sums damage only
  (`NPC.cs:1180-1200`), so loot still goes to whoever did the damage.
- **Dodge:** `Player.Attacked` rolls dodge for every positive hit, including % hits and bleed ticks.
  The design says "armour doesn't decide it"; dodge is separate and stays.
- **Script reload:** phase and mode are in `NPC.Properties`; timers check `npc.State == Alive` and
  stop otherwise; a reload mid-fight resets swing counters and bleeds only.
- **Stalker vs. kill path:** he cannot be killed and always leaves through `Despawn`, so no kill
  hooks, XP or drops fire for him.
- **Weeper and Foyer:** A* takes the shortest path; the loops avoid the Foyer and nothing forbids
  crossing it, matching the design.
