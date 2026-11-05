# Arena Part 3: Arena Core Scripts — Implementation Plan

**Goal:** Ship the arena game mode as scripts: schedule and announcements, lobby → arena
teleport, rounds and waves, per-hit scaling, elimination, Arena Coin payouts, restart/reload
recovery, and GM commands.

**Architecture:** All arena code lives in `Scripts/Arena/`. Each `.csx` compiles `#load`ed files
into its own assembly, so **statics are not shared** between the controller, map and mob
scripts. All shared state therefore goes through `WorldState` as JSON strings, and all shared
logic is static functions in `#load`ed files (`ArenaConfig`, `ArenaMath`, `ArenaState`,
`ArenaCore`). The controller is a global script whose single timer re-resolves the live script
object each tick (the Halloween reload-safe pattern).
Design: `docs/plans/2026-11-05-arena-design.md` (Part 3 of 4). **Requires Parts 1 and 2.**

**Tech Stack:** C# scripts (`.csx`, Roslyn), .NET 10, xUnit.

**Conventions:** `AGENTS.md` — no new comments unless the "why" is non-obvious (the
assembly-isolation rule above deserves one line in `ArenaCore.csx`).

---

## APIs verified

| API | Location |
|---|---|
| Only the top level of `Scripts/Global/*.csx` is auto-loaded; `OnLoaded` runs **once** per process | `Goose/GameWorld.cs:912-938` |
| `/reloadscripts` swaps `Script.Object` in place via `CompileForSwap`; `OnLoaded` is **not** re-run | `Goose/Scripting/ScriptHandler.cs:51-76` |
| Startup order: Global Scripts `OnLoaded` (`:476`) **before** `WorldState.Load` (`:478`) | `Goose/GameWorld.cs:476-482` |
| Reload-safe timer: one `ScriptTimerEvent` from `OnLoaded`, reschedule in `finally`, re-resolve current object by file name, invoke reflectively | `Scripts/Global/HalloweenInvasion.csx:16-31,130-142` |
| `ScriptTimerEvent.Create(action, period, world)` / `Reschedule(period, world)`; exceptions logged | `Goose/Events/ScriptTimerEvent.cs:9-44` |
| Config cached by file mtime, validated, logs once, returns a disabled config on error | `Scripts/Global/Halloween/HalloweenConfig.csx:290-470` |
| `WorldState.Get<T>` / `Set(key, object)` / `Remove`; serialise-and-diff persistence | `Goose/WorldState.cs:53-95` |
| Script command registration with GM privilege and subcommand args | `Scripts/Global/Halloween.csx:24-26` (`Action<CommandContext, string, string[]>`), `Goose/Commands/CommandRegistry.cs:20-24` |
| `NPCHandler.SpawnNPC(world, mapId, x, y, template, shouldRespawn, properties)` → `NPC?` | `Goose/NPCHandler.cs:384-391` |
| Spawn sequence: `OnSpawnEvent` runs **before** `CurrentHP = MaxHP`, aggro reset, and the spawn packet | `Goose/NPC.cs:722-772` |
| `NPC.TemporaryMaxHP`; `MaxHP => TemporaryMaxHP ?? MaxStats.HP` | `Goose/NPC.cs:119-134` |
| `NPC.Properties`, `Title`, `BodyR/G/B/A`, `AggroTarget`, `AggroTargetToValue` | `Goose/NPC.cs:56,165,201,314,322` |
| `NPC.AddAggro(player, value, world)` sets the target if none and starts move/attack events | `Goose/NPC.cs:1011-1050` |
| `NPC.Despawn`, `Hide`, `Reveal(world, x, y)` | `Goose/NPC.cs:776-813` |
| `Map.IsTileBlocked(null, x, y)`, `Map.NPCs`, `Map.Players`, `GetPlayersInRange` | `Goose/Map.cs:79,167,429,926` |
| Free-tile pick loop | `Scripts/Global/HalloweenInvasion.csx:95-104` |
| Global `OnNPCKilled(npc, killer, credited, world)` — `credited` is the engine's kill credit, groups included | `Goose/NPC.cs:1381-1396`, `Goose.Tests/GlobalNpcKilledHookTests.cs` |
| Map hooks `OnPlayerEntered` (also on login via `DoneLoadingMapEvent.cs:101`), `OnPlayerLeft` (also on logout via `LogoutEvent.cs:54`), `CanPlayerJoin` | `Goose/Map.cs:197-225,834-850` |
| Eviction-on-enter precedent | `Scripts/Map/HauntedHouse.csx:18-26` |
| `Player.WarpTo(world, map, x, y)` — does **not** consult `CanPlayerJoin` | `Goose/Player.cs:1489-1580` |
| `Player.PlayerID` (database id), `Player.ExperienceSold` (long), `Player.IsGM` | `Goose/Player.cs:102,292,425` |
| `world.SendToAll(P.ServerMessage(text))`; `P.ServerMessage` prefixes `$7` | `Scripts/Global/HalloweenInvasion.csx:92`, `Goose/Packets.cs:39` |
| `PlayerHandler.GetPlayer(string name)`, `Players` | `Goose/PlayerHandler.cs:136,163` |
| Part 2: `IMapScript.OnPlayerDied → RespawnLocation?`, `INPCScript.InterceptDamageTaken`, `INPCScript.MeleeBaseDamage`, `NPC.MitigateMelee` | `docs/plans/2026-11-05-arena-part2-seams.md` |
| Part 1: `CurrencyHandler.Get(id)`, `ICurrency.Add` | `docs/plans/2026-11-05-arena-part1-currencies.md` |
| Shipped-script test pattern: copy `.csx` into the fixture data dir, compile, call statics through a host script reflectively | `Goose.Tests/Halloween/HalloweenTestKit.cs:28-92,221` |

**Design refinements from verification** (Task 9 updates the design doc):
- **`/arena reload` is dropped.** The config re-reads automatically when `arena.json`'s mtime
  changes, like Halloween's.
- **Mob damage does not need a script-owned attack loop.** `ArenaMob.MeleeBaseDamage` (Part 2)
  replaces the base per target, and the engine keeps targeting, stun/root and AC/DR.
- **Restart recovery runs from a one-shot timer**, because `WorldState` loads after `OnLoaded`.
- **Participants are an explicit id list** in run state. `WarpTo` skips `CanPlayerJoin`, so the
  controller records a participant **before** warping them, and `ArenaMap.OnPlayerEntered`
  evicts anyone not on the list. That covers relog, walking in, and GM-summoned players.
- **Times are wall-clock UTC** (`DateTime.UtcNow`), passed into `Tick(world, nowUtc)` so tests
  control time. The anchor is `anchorUtc: "HH:mm"`.

**Persistence:** `WorldState["arena.run"]` (JSON string; present only while a run is active)
and `WorldState["arena.lastRun"]` (summary for Part 4's hub). No schema change.

**Manual data the user creates** (listed in the final report, not code):
- Lobby and arena maps. Arena map: `pvp_enabled 0`, `pets_enabled 0`, `bind_enabled 0`,
  `script_path Scripts/Arena/ArenaMap.csx`.
- Arena mob templates (≈6–9 normals, 3 bosses): `script_path Scripts/Arena/ArenaMob.csx`, no
  drops, `respawn_time 0`, a sensible `attack_range` / `aggro_range`.
- `arena.json` map ids, rect, lobby spawn, and band template ids. While any of those are 0 the
  arena stays disabled and logs once.

---

## File layout

```
Scripts/Global/Arena.csx          #load "../Arena/ArenaController.csx"; return typeof(ArenaController);
Scripts/Arena/arena.json          shipped config (map ids 0 → disabled)
Scripts/Arena/ArenaConfig.csx     POCOs + Get(world) with mtime cache + validation
Scripts/Arena/ArenaMath.csx       pure static functions
Scripts/Arena/ArenaState.csx      run-state DTOs + Load/Save through WorldState JSON strings
Scripts/Arena/ArenaCore.csx       operations shared by every arena script (#loads the three above)
Scripts/Arena/ArenaController.csx global script: timer, recovery, OnNPCKilled, /arena commands
Scripts/Arena/ArenaMap.csx        map script
Scripts/Arena/ArenaMob.csx        NPC script
```

Test kit: `Goose.Tests/Arena/ArenaTestKit.cs` copies `Scripts/Arena/*` and the shim into the
fixture data dir, writes a test `arena.json`, registers test maps and templates, and compiles a
host script exposing `ArenaMath`/`ArenaCore` statics, like `HalloweenTestKit.cs:69,200-215`.

---

### Task 1: Config, shim and test kit

**Files:** create `Scripts/Global/Arena.csx`, `Scripts/Arena/arena.json`,
`Scripts/Arena/ArenaConfig.csx`, a minimal `Scripts/Arena/ArenaController.csx` (`OnLoaded` no-op),
`Goose.Tests/Arena/ArenaTestKit.cs`, `Goose.Tests/Arena/ArenaConfigTests.cs`.

**Config shape** (camelCase JSON, case-insensitive, comments and trailing commas allowed):

```
lobbyMap, lobbySpawn[x,y], arenaMap, arenaRect[x,y,w,h],
intervalHours, anchorUtc, announceMinutes[], countdownSeconds, breakSeconds, roundSeconds,
timeoutWarningSeconds, stragglerCount, minPlayerDistance,
baseCount, perPlayer, bandSize, bands[{normals[], boss}],
eliteFromRound, eliteChance, eliteChanceGrowth, eliteChanceMax, eliteHpMult, eliteDmgMult,
eliteTitle, eliteTint[r,g,b,a],
bossEvery, bossRoundAddFraction, bossHpMult, bossHpPerPlayer, bossDmgMult,
minXpSold, topXpSold, a, b, anchorRound, anchorMobHp, anchorMobWeaponDamage, hpGrowth, dmgGrowth,
currencyId, roundBase, roundStep, mobBounty, eliteBountyMult, bossBountyMult, milestones{round: coins}
```

Shipped defaults are the design values: interval 3h, anchor `00:00`, announce `[15,10,5]`,
countdown 10s, break 15s, round 180s, warning 30s, stragglers 3, min distance 3, base 4,
per-player 2, band 10, elites from round 3 at 5% +2%/round up to 30% (×5 HP, ×2 damage), boss
every 5 (adds ×0.5, HP ×40, +0.25 per extra player, damage ×3), xp sold 4M…3B, a = b = 0.75,
anchor round 20 / 12,000,000 HP / 65,000 weapon damage, growth 1.10 / 1.08, currency
`arena_coin`, round 5 + 1/round, bounty 1 (elite ×5, boss ×25), milestones `{5:20,10:60,15:120,20:200}`.
Map ids 0 and empty bands.

**`ArenaConfig.Get(world)`** copies `HalloweenConfig.Get` (`:295-470`): path
`DataPathAbsolute + "/Scripts/Arena/arena.json"`, mtime cache, log once per bad version.
**`Enabled`** is false when any of `lobbyMap`, `arenaMap`, `arenaRect`, `lobbySpawn` or `bands`
is missing, or a referenced map isn't in `world.MapHandler.Maps`. **Validation errors**
(→ disabled config, logged once): `a`/`b` outside `[0, 2]`, growth ≤ 0, `minXpSold` ≤ 0 or
`topXpSold` < `minXpSold`, `bandSize` < 1, a band with no normals, `roundSeconds` ≤
`timeoutWarningSeconds`.

**Tests (red → green):**
- `Shipped_config_parses_and_is_disabled_until_maps_are_set` (reads the real shipped file)
- `Valid_test_config_is_enabled`
- `Invalid_exponent_disables_and_logs_once` (adversarial: two `Get`s, one error)
- `Editing_the_file_reloads_without_script_reload` (rewrite the file, bump mtime → new values)
- `Shim_compiles_and_loads_controller`

**Commit** `feat(arena): config, shim and test kit`

---

### Task 2: `ArenaMath`

**Files:** create `Scripts/Arena/ArenaMath.csx`; test `Goose.Tests/Arena/ArenaMathTests.cs`.

Pure, static, no `GameWorld` reads except the config passed in:

```csharp
double PowerFraction(long xpSold, ArenaConfig c)                 // clamp(xp, min, top) / top
long   EffectiveDamage(long raw, double s, ArenaConfig c)        // raw × s^(−a), saturating
long   MobHp(int round, MobTier tier, int alive, ArenaConfig c)  // anchorMobHp × hpGrowth^(round−anchorRound) × tier mult (boss: × (1 + bossHpPerPlayer × (alive−1)))
double MobWeaponDamage(int round, MobTier tier, double s, ArenaConfig c)
int    WaveCount(int round, int alive, ArenaConfig c)            // boss rounds: ceil(count × bossRoundAddFraction)
bool   IsBossRound(int round, ArenaConfig c)
double EliteChance(int round, ArenaConfig c)
ArenaBand BandFor(int round, ArenaConfig c)                      // last band repeats
long   RoundPayout(int round, ArenaConfig c)
long   Bounty(MobTier tier, ArenaConfig c)
long   MilestoneBonus(int roundsCleared, ArenaConfig c)          // highest reached, else 0
Dictionary<int,long> SplitBounty(long bounty, IReadOnlyDictionary<int,long> damageByPlayer) // largest-remainder, sums exactly
DateTime NextStartUtc(DateTime nowUtc, ArenaConfig c)            // first anchor + k·interval strictly after now
```

`enum MobTier { Normal, Elite, Boss }`.

**Tests**, including the design's worked numbers:
- `PowerFraction_clamps_both_ends`, `Zero_xp_sold_counts_as_min`
- `Effective_damage_matches_design_table` (3B: 5M → 5M; 300M: 500k → ≈2.8M; 20M: 35k → ≈1.5M, ±1%)
- `MobHp_at_anchor_round_is_anchor`, `MobHp_round_1_is_about_2M`
- `Boss_hp_scales_with_alive_players`
- `Weapon_damage_scales_down_for_low_xp` (300M → ≈11.6k at round 20)
- `Effective_damage_never_overflows` (adversarial: `long.MaxValue` raw with s = min)
- `Split_bounty_sums_exactly_and_is_proportional` (adversarial: 3 players, odd totals)
- `Split_bounty_with_no_damage_is_empty`
- `Band_repeats_last_after_the_end`, `Elite_chance_caps`
- `NextStart_is_strictly_after_now_and_aligned_to_anchor` (exactly on an anchor → the next one)
- `Milestone_takes_highest_reached`

**Commit** `feat(arena): scaling, wave and payout math`

---

### Task 3: Run state and shared operations

**Files:** create `Scripts/Arena/ArenaState.csx`, `Scripts/Arena/ArenaCore.csx`; test
`Goose.Tests/Arena/ArenaCoreTests.cs`.

**State** (stored as one JSON string under `arena.run`; `Load` returns null when absent;
`Save` always `Set`s a fresh string):

```csharp
enum ArenaPhase { Countdown, Fighting, Break, Ended }
class ArenaParticipant { int Id /* PlayerID */; string Name; bool Alive; int RoundsCleared; long Coins; int Kills; }
class ArenaRun { string RunId; DateTime StartedUtc; ArenaPhase Phase; DateTime PhaseUntilUtc;
                 int Round; bool Warned; Dictionary<int, ArenaParticipant> Participants; }
```

A JSON string rather than an object, because every arena script has its own copy of these types.
`WorldState.Get<T>` would deserialise a fresh copy on every type mismatch (`WorldState.cs:70-79`),
and a mutation without `Set` would silently be lost.

**`ArenaCore` operations** (static; each loads, mutates and saves state; idempotent):
- `StartRun(world, nowUtc)` — no-op if a run exists or the config is disabled. Collects
  non-GM, `State == Ready` players whose `Map.ID == lobbyMap`. None → return. Otherwise **save
  state with all participants first**, then `WarpTo` each to a free tile in `arenaRect` (fall
  back to the rect centre). Phase `Countdown` until now + `countdownSeconds`. Announces start.
- `Eliminate(world, playerId, reason)` — no-op unless the participant exists and is `Alive`.
  Marks dead, final `RoundsCleared` stands, sends a personal summary, then calls `EndIfEmpty`.
  **Does not warp.** The death path respawns via `ArenaMap.OnPlayerDied`. Leaving is already a
  departure. The timeout and stop paths warp explicitly.
- `EndIfEmpty(world, nowUtc)` → `EndRun` when no participant is alive.
- `EndRun(world, reason)` — despawn every NPC on the arena map tagged `arena.run == RunId`.
  Warp any remaining alive participants to the lobby spawn and mark them dead. Pay each
  participant `MilestoneBonus(RoundsCleared)`. Write `arena.lastRun` (run id, ended time, best
  round, participant results). Call the leaderboard extension point
  `ArenaCore.RecordResults(world, run)` (a no-op stub until Part 4). Remove `arena.run`.
  Announce the result.
- `Pay(world, playerId, amount, reason)` — `CurrencyHandler.Get(config.currencyId)?.Add(...)` if
  the player is online, adds to `Participant.Coins`, and sends `"+N Arena Coins (reason)"`.
  Unknown currency → log once, skip.
- `IsParticipant(world, playerId)` → alive participant in the active run.

**Mutation impact:**
- Sources of truth: `arena.run`, player wallets (Part 1 properties), player position.
- Readers: every arena script; Part 4 hub (`arena.run`, `arena.lastRun`); `ArenaMap.OnPlayerEntered`
  (participant check).
- Ordering invariant: a participant is saved **before** `WarpTo`, so the `OnPlayerEntered` that
  follows sees them.
- Idempotency: `Eliminate` from both `OnPlayerDied` and the `OnPlayerLeft` caused by the respawn
  warp counts once.

**Tests:**
- `Start_moves_lobby_players_and_skips_gms_and_other_maps`
- `Start_with_empty_lobby_creates_no_run`
- `Participant_is_saved_before_warp`: a map script stub on the arena map records
  `IsParticipant` inside `OnPlayerEntered` → true. Drive `OnPlayerEntered` the way
  `DoneLoadingMapEvent` does (`map.AddPlayer`).
- `Eliminate_twice_counts_once` (adversarial)
- `Last_elimination_ends_run_pays_milestones_and_clears_state`
- `End_run_despawns_only_this_runs_mobs` (adversarial: an untagged NPC on the arena map survives)
- `State_round_trips_through_world_state_save_and_load` (`WorldState.Save` and `Load` against a
  temp DB, as in `CommunityChestPersistenceTests`)

**Commit** `feat(arena): run state and shared operations`

---

### Task 4: `ArenaMob` script

**Files:** create `Scripts/Arena/ArenaMob.csx`; test `Goose.Tests/Arena/ArenaMobTests.cs`.

Properties set by the spawner (Task 5): `arena.run` (string), `arena.round` (int),
`arena.tier` (int `MobTier`), `arena.alive` (int, alive count at spawn, for boss HP).

- `OnSpawnEvent`: no `arena.run` → do nothing (a stray spawn behaves like its template).
  Otherwise `TemporaryMaxHP = MobHp(...)`. Elite: `Title = eliteTitle`, `BodyR/G/B/A = eliteTint`.
  This runs before the engine sets `CurrentHP` and sends the spawn packet (`NPC.cs:722-772`), so
  no extra packet is needed.
- `InterceptDamageTaken(npc, attacker, damage)`: attacker not a `Player`, or no `arena.run` → return
  `damage`. Otherwise `eff = EffectiveDamage(damage, PowerFraction(attacker.ExperienceSold))`,
  add `eff` to `npc.Properties["arena.dmg." + player.PlayerID]` (the database id,
  `Player.cs:102`; `LoginID` is a reusable session slot), and return `eff`.
- `MeleeBaseDamage(npc, target, base)`: target not a `Player`, or no `arena.run` → `base`.
  Otherwise `MobWeaponDamage(round, tier, PowerFraction(target.ExperienceSold))`.
- Inherits `BaseNPCScript` movement and attack (engine aggro and taunts).

**Tests** (template scripted with the real `ArenaMob.csx` through the kit):
- `Spawn_sets_hp_from_round_and_tier`, `Elite_gets_title_and_tint`
- `Untagged_spawn_keeps_template_hp` (adversarial)
- `Low_xp_player_damage_is_lifted_and_logged`
- `Top_xp_player_damage_is_unchanged`
- `Mob_hits_low_xp_player_for_less_than_top_xp_player`: two players with equal AC; `npc.Attack`
  each → HP loss ratio ≈ `s^b`.
- `Damage_from_non_player_is_not_scaled`

**Commit** `feat(arena): mob script scaling and damage log`

---

### Task 5: Waves, round loop, kills and payouts

**Files:** modify `ArenaCore.csx`, `ArenaController.csx`; test `Goose.Tests/Arena/ArenaRoundTests.cs`.

**`ArenaCore.Tick(world, nowUtc)`** (single entry point, driven by the controller):
- No run → nothing (scheduling is Task 7).
- `Countdown` and time is up → `StartRound(1)`.
- `Fighting`:
  - Alive arena mobs for this round == 0 → **clear**: each alive participant gets
    `RoundsCleared = Round` and `RoundPayout(Round)`. Phase `Break` until now + `breakSeconds`.
  - Otherwise, time left ≤ `timeoutWarningSeconds` and `!Warned` → map message
    `"[Arena] {n} enemies remain! {s}s left!"`, set `Warned`, and **straggler recall**: if
    remaining ≤ `stragglerCount`, `Hide` and then `Reveal` each mob on a free tile within 2 of a
    random alive participant.
  - `now ≥ PhaseUntilUtc` → **fail**: `EndRun(world, "time ran out on round N")`.
- `Break` and time is up → `StartRound(Round + 1)`.

**`StartRound(n)`:** alive = alive participants. `count = WaveCount`. On a boss round, spawn the
band's boss first (tier `Boss`). For each remaining mob: tier `Elite` with `EliteChance(n)`
(`world.Random`), otherwise `Normal`; template uniform from the band's normals; tile = a free
tile in `arenaRect` at least `minPlayerDistance` (Chebyshev) from every participant, 50 tries,
then **any** free tile. Spawn with the four properties, `shouldRespawn: false`. Then
`npc.AddAggro(nearest alive participant, 1, world)`. Phase `Fighting` until now +
`roundSeconds`; `Warned = false`. Map message `"[Arena] Round {n}!"` (`"Boss round!"` when it is
one). A template id missing from `NPCHandler` → log and skip that mob. If **zero** spawned,
treat the round as cleared on the next tick, so a bad config never stalls a run.

**Kills and bounties** (`ArenaController.OnNPCKilled`): ignore NPCs without this run's
`arena.run`. Each `credited` participant gets `Kills++`. The bounty
`Bounty(tier)` is split with `SplitBounty` over the `arena.dmg.*` properties, restricted to
participants, and paid via `Pay`. Clear detection itself stays in `Tick`, so kills and clears
can't race.

**Re-aggro:** in `Tick` during `Fighting`, any arena mob whose `AggroTarget` is null or not an
alive participant gets `AddAggro(nearest alive participant)`.

**Tests** (drive `ArenaCore.Tick` with explicit times; kill mobs via `npc.Attacked` with huge damage):
- `Countdown_then_round_1_spawns_base_plus_per_player`
- `Spawns_respect_min_player_distance`, `Spawns_fall_back_when_crowded` (adversarial: a tiny rect)
- `Clear_pays_round_payout_and_breaks_then_next_round`
- `Boss_round_spawns_boss_and_fewer_adds`
- `Timeout_warns_once_then_fails_run_and_warps_survivors_to_lobby`
- `Straggler_recall_moves_last_mobs_near_a_participant`
- `Bounty_split_by_effective_damage_and_kills_counted_for_credited_group` (adversarial: a
  non-participant damager gets nothing)
- `Missing_template_round_does_not_stall`
- `Mob_retargets_when_target_eliminated`

**Commit** `feat(arena): rounds, waves, kills and payouts`

---

### Task 6: `ArenaMap` script

**Files:** create `Scripts/Arena/ArenaMap.csx`; test `Goose.Tests/Arena/ArenaMapTests.cs`.

- `CanPlayerJoin`: GM → null. `IsParticipant` → null. Otherwise
  `"The arena is closed. Wait in the lobby for the next run."`
- `OnPlayerEntered`: not a GM and not `IsParticipant` → `WarpTo` the lobby spawn with that
  message. This covers relog after a run, and warps that bypass `CanPlayerJoin`.
- `OnPlayerDied(map, player, killer)`: participant → `Eliminate(world, id, "slain by " + killer.Name)`.
  Always returns `RespawnLocation(lobby, lobbySpawn)` when the config is enabled, so nobody
  respawns in the arena. Otherwise null.
- `OnPlayerLeft(map, player)`: participant → `Eliminate(world, id, "left the arena")`.

**Tests:**
- `Non_participant_cannot_join_and_gm_can`
- `Relogging_non_participant_is_evicted_to_lobby`
- `Death_eliminates_and_respawns_in_lobby` (real death via `npc.Attack` on a 1-HP player, as in Part 2's harness)
- `Death_then_left_eliminates_once` (adversarial: the respawn warp fires `OnPlayerLeft` too)
- `Disconnect_eliminates`: `map.RemovePlayer`, the logout path at `LogoutEvent.cs:54`.

**Commit** `feat(arena): arena map script`

---

### Task 7: Controller — schedule, announcements, recovery, commands

**Files:** finish `Scripts/Arena/ArenaController.csx`; test `Goose.Tests/Arena/ArenaControllerTests.cs`.

- `OnLoaded`:
  - one repeating 1-second timer → `TickCurrent(world, DateTime.UtcNow)`, which resolves the
    live controller by file name `Arena.csx` and invokes `Tick` reflectively (the
    `HalloweenInvasion.csx:130-142` pattern);
  - a **one-shot** 2-second timer → `RecoverCurrent(world)`;
  - registers `/arena ` (`AccessPrivilege.SpawnNPC`, section "Arena") as
    `Action<CommandContext, string, string[]>`, whose body also re-resolves the current
    controller.
- `Recover(world)`: if `arena.run` exists, this is a restart (`OnLoaded` never runs on a script
  reload), so `EndRun(world, "server restarted")`. Also despawn any NPC on the arena map carrying
  an `arena.run` property, and warp any non-GM player on the arena map to the lobby.
- `Tick(world, nowUtc)`:
  - config disabled → return;
  - **announcements**: for each `m` in `announceMinutes`, when `NextStartUtc − m min` falls in
    `(lastTick, now]` → `SendToAll("[Arena] The arena opens in {m} minutes. Gather in the lobby!")`.
    `lastTick` lives in `WorldState["arena.lastTickUtc"]` so a reload doesn't repeat or skip;
    first tick after load = now;
  - **start**: when a scheduled start falls in `(lastTick, now]` → `StartRun`. If a run is
    active, skip it and log;
  - then `ArenaCore.Tick`.
- Commands:
  - `/arena start` → `StartRun` now;
  - `/arena stop` → `EndRun("stopped by a GM")`;
  - `/arena status` → phase, round, alive, next start;
  - `/arena preview <round> [player]` → for the named online player, or the caller: `s`; normal,
    elite and boss HP; their effective hit for a sample raw value (the arg, or 1,000,000); and
    mob weapon damage before and after `npc.MitigateMelee` for normal, elite and boss. Use a
    throwaway `NPC` built from the first band normal's template (`LoadFromTemplate` is not
    needed: set `MaxStats`, `ArmorPierce`, `Level` directly).

**Tests:**
- `Announces_15_10_5_once_each`: tick across 16 minutes in 30-second steps.
- `Starts_on_anchor_and_skips_when_run_active`
- `Restart_recovery_ends_stale_run_and_pays_bonuses` (adversarial: state present at first tick)
- `Reload_does_not_trigger_recovery`: swap the script object, tick → run continues.
- `Preview_prints_values_matching_math`
- `Commands_require_gm`

**Commit** `feat(arena): controller schedule, recovery and GM commands`

---

### Task 8: Integration — shipped scripts compile together, end-to-end run

**Files:** `Goose.IntegrationTests/ArenaEndToEndTests.cs`; add the arena scripts to the
integration project's copied-scripts list, in the same style as
`GlobalScriptFixture.ShippedScripts`.

- `All_arena_scripts_compile`
- `Full_run_two_players`: lobby → start → round 1 cleared (coins paid) → round 2 → one player
  dies (lobby, eliminated) → the other leaves → run ends, milestones paid, `arena.run` gone,
  `arena.lastRun` written. Uses a real `DataCurrency` from Part 1.

Run `dotnet test Goose.sln --no-restore` and `dotnet test Goose.IntegrationTests --no-restore`
→ PASS.

**Commit** `test(arena): end-to-end run`

---

### Task 9: Docs and manual smoke

- Update `docs/plans/2026-11-05-arena-design.md` with the refinements listed at the top.
- Manual on a dev server: create the two maps and two mob templates; set `arena.json`;
  `/arena start` alone; check round flow, `/wallet` coins, death → lobby, `/arena preview 20`.
- Final report: the manual data list from the top of this plan.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Only lobby non-GMs enter; participants recorded before warp | `Start_moves_lobby_players_and_skips_gms_and_other_maps`, `Participant_is_saved_before_warp` |
| Nobody re-enters or lingers in the arena | `Non_participant_cannot_join_and_gm_can`, `Relogging_non_participant_is_evicted_to_lobby` |
| Every exit eliminates exactly once | `Death_then_left_eliminates_once`, `Eliminate_twice_counts_once`, `Disconnect_eliminates` |
| Scaling matches the design numbers | `Effective_damage_matches_design_table`, `Mob_hits_low_xp_player_for_less_than_top_xp_player` |
| Coins conserve | `Split_bounty_sums_exactly_and_is_proportional` |
| Non-participants earn nothing | `Bounty_split_by_effective_damage_and_kills_counted_for_credited_group` |
| Timeout fails the run | `Timeout_warns_once_then_fails_run_and_warps_survivors_to_lobby` |
| Bad data never stalls a run | `Missing_template_round_does_not_stall`, `Spawns_fall_back_when_crowded` |
| Restart cleans up; reload resumes | `Restart_recovery_ends_stale_run_and_pays_bonuses`, `Reload_does_not_trigger_recovery` |
| State survives persistence | `State_round_trips_through_world_state_save_and_load` |
| Announcements fire once each | `Announces_15_10_5_once_each` |
