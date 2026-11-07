# Arena — Design

## Purpose

A scheduled wave-survival arena. Every few hours, everyone standing in the arena lobby is
teleported into the arena, where rounds of mobs spawn and grow stronger until the last
participant falls. Players earn Arena Coins (spent at an arena vendor on enchanting
essences, protection items and cosmetics) and compete on best-round and kill leaderboards.

The arena must be fun and rewarding across a very wide power range — from roughly 4M to
3B+ experience sold (about 1000x) — so mob damage taken and dealt is scaled per hit to the
player involved.

Everything arena-specific is script (`Scripts/Arena/`). The server gains a handful of
generic seams that are useful beyond the arena.

## Decisions at a glance

| Topic | Decision |
|---|---|
| Run model | Timed runs (default every 3h from an anchor time). Everyone on the lobby map at start is entered. |
| Announcements | Server-wide at 15, 10, 5 minutes before, and at start (configurable list). |
| Death / disconnect | Out for the run. No death penalty. Respawn in the lobby. Earnings kept. No resurrection (deferred). |
| Pets | Not allowed (`pets_enabled = 0` on the arena map). |
| PvP | Off in the arena. |
| Round advance | On full clear, after a short break. |
| Round timeout | **Run fails** — all remaining participants are eliminated with rounds cleared credited. 30s warning; straggler recall. |
| Difficulty | Mob HP and damage grow per round; mob count grows with players alive; boss every N rounds; elites ramp in. |
| Scaling | Per hit, from experience sold. Anchored at top players; everyone below is scaled down. |
| Defence | Mob damage runs through the engine's normal melee formula, so AC and DR apply. |
| Experience | Engine default — template XP to whoever the engine credits. |
| Drops | None. Rewards are currency only. |
| Currency | Arena Coins (`arena_coin`), data-defined, untradeable wallet balance. |
| Earning | Round-clear payout to survivors + kill bounty split by effective damage + end-of-run milestone bonus. |
| Leaderboards | Best round and total kills, weekly and all-time, top 10 + own rank, at the Arena Master NPC. |
| Kills | Every participant who dealt at least `killCreditMinShare` (5%) of the mob's tracked effective damage gets a kill. Grouping doesn't matter. Tracked damage is capped at the mob's remaining HP, so overkill doesn't count. |
| Wallet | Server `/wallet` command listing every currency with icon. |
| Shop | Ordinary vendor NPC in data with `currency_id = 'arena_coin'`; prices are item `Value`. |

## Server changes

All generic; none mention the arena.

### 1. Data-driven currencies

- New table `currencies(currency_id, name, short_name, icon_sheet, icon_graphic, vendor_buyback)`.
  Each row registers a `DataCurrency : ICurrency` at world load.
- Balances for data currencies live in `Player.Properties["currency.<id>"]`, which already
  persists as JSON in `players.player_properties` — no new table, saved atomically with the
  player. Gold, credits and script currencies (spirit) keep their existing storage.
- `ICurrency` gains an optional icon (default interface member returning `null`).
- `DataCurrency.GetBuyPrice` = `Value × stack`. `GetSellPrice` = `Value × stack / 2`, or `-1`
  (refuse) when `vendor_buyback = 0` or `Value = 0`.
- New nullable `currency_id` column on `item_templates` and `npc_templates`, loaded into the
  existing runtime `ItemTemplate.CurrencyId` / `NPCTemplate.CurrencyId`.
  `CurrencyHandler.Resolve` already prefers item over vendor over gold, so an item tagged
  `arena_coin` sold at a gold vendor resolves to Arena Coins and is refused — no gold leak.
- Unknown currency ids are validated **after** global scripts' `OnLoaded` (script currencies
  register there), logged, and cleared to default.
- `credit_dealer` is removed; NPCs that set it are migrated to `currency_id = 'credits'`.
- Game data editor descriptors gain the new tables/columns.

### 2. `/wallet`

A built-in command opening an `OptionListWindow` with one line per registered currency —
`[icon] Arena Coins: 1,240`. Currencies without an icon use a placeholder icon setting.
A client button may call it later.

### 3. `OnPlayerDied`

Fired from the player death branch (`Player.Attacked`, before the bind warp): the current
map's script first, then every global script. The map hook may return a respawn location override
so arena deaths land in the lobby rather than at the player's bind point.
The engine warps the player again after the hook returns, and a second cross-map warp NREs
(`WarpTo` dereferences `Map`, which the first cross-map warp set to null) — so the arena map
script performs the protocol-complete cross-map warp to the lobby itself and re-attaches the
player, leaving the engine's follow-up warp as a same-map move.

### 4. NPC-script damage hooks

- `long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world)`,
  default returns `damage`. Called in `NPC.Attacked` after buff intercepts, so buff effects
  apply first and the NPC script has the final say.
- `double MeleeBaseDamage(NPC npc, ICharacter target, double baseDamage, GameWorld world)`,
  default returns `baseDamage`. Called in `NPC.Attack` before the crit roll, so engine targeting,
  stun/root, crit, DR and AC all still apply. Arena mobs don't need their own attack loop.

### 5. Public `NPC.MitigateMelee`

A pure function (no RNG, no side effects) with the melee mitigation from `NPC.Attack`
(melee damage, DR, AC absorb, armor pierce, flat AC). `NPC.Attack` uses it, and
`/arena preview` uses it to show before/after-defence numbers.

### 6. `OnPlayerClicked` on NPC scripts

`bool OnPlayerClicked(NPC npc, Player player, GameWorld world)`, called on NPC left-click before
the community-goal and quest windows. Returning true means handled. The Arena Master hub uses it.

**Not a server change:** relogging onto the arena map. Login fires the map's
`OnPlayerEntered`, and the arena map script evicts non-participants there, the same way the
Halloween house does.

## Scripts (`Scripts/Arena/`)

```
Scripts/Global/Arena.csx     shim: #load ../Arena/ArenaController.csx; returns its type
Scripts/Arena/
  arena.json                 all tuning
  ArenaConfig.csx            load + validate arena.json
  ArenaMath.csx              pure functions: scaling, waves, payouts, ranking
  ArenaState.csx             run-state DTOs, stored in WorldState as JSON strings
  ArenaCore.csx              operations shared by every arena script
  ArenaBoard.csx             leaderboard storage and ranking
  ArenaController.csx        global script: timer, schedule, announcements, recovery, kills, GM commands
  ArenaMap.csx               arena map script: CanPlayerJoin, OnPlayerEntered, OnPlayerLeft, OnPlayerDied
  ArenaMob.csx               NPC script on arena mob templates
  ArenaMaster.csx            NPC script on the Arena Master: hub + leaderboards
```

Only the top level of `Scripts/Global/` is auto-loaded, so the shim keeps the arena in its
own folder. Each script compiles its `#load`ed files into its own assembly, so statics are not
shared between scripts. All shared state goes through `WorldState` as JSON strings.
`arena.json` re-reads automatically when its modified time changes.

## Run lifecycle

**Schedule.** `intervalHours` (3) from `anchorUtc` (e.g. 00:00 UTC). If a run is still
in progress at a scheduled start, that start is skipped.

**Announcements.** Server-wide at each `announceMinutes` entry (`[15, 10, 5]`) and at start.

**Start.**
1. Collect non-GM players on the lobby map. None → skip silently.
2. Warp each to a random free tile in the arena rect. They are the participants.
3. Short countdown, then round 1.

**Round.**
1. Spawn the wave (see Waves).
2. **Clear** (all wave mobs dead): survivors get the round payout and +1 rounds cleared;
   `breakSeconds` pause; next round.
3. **Timeout** (`roundSeconds`): warning at 30s left with the remaining count. Straggler
   recall teleports the last few mobs near a living participant. If time runs out, the run
   fails: all remaining participants are eliminated, rounds cleared credited.

**Elimination.** Death, or leaving the arena map for any reason (warp, teleport,
disconnect). The player's rounds cleared is final, their end-of-run bonus is computed, and
they respawn in the lobby. `ArenaMap.CanPlayerJoin` refuses anyone not in the run.

**End.** Last participant eliminated → despawn remaining arena mobs, pay end-of-run bonuses,
update leaderboards, announce the result.

**State and reload.** State is reconstructable, so `/reloadscripts` mid-run resumes:
- participants = an explicit id list in run state, saved **before** each warp-in; the arena
  map evicts anyone else in `OnPlayerEntered` (including on login);
- mobs = NPCs on the arena map tagged through `npc.Properties` (`arenaRound`, tier, damage
  log);
- counters (round, deadline, per-player rounds cleared and coins earned this run) in
  `WorldState["arena.run"]`;
- timers use the Halloween reload-safe pattern (re-resolve `Script.Object`, invoke
  reflectively, reschedule in `finally`).

After a **server restart** (`OnLoaded` only runs at process start, and `WorldState` loads after
it, so this runs from a one-shot timer) the controller aborts the run cleanly: despawn arena mobs, move anyone on the arena map to the lobby, pay
bonuses, clear run state.

**Config breakage mid-run.** If `arena.json` becomes invalid (or the arena/lobby map
disappears) while a run is active, the next tick ends the run cleanly ("arena unavailable")
rather than freezing; `EndRun` is null-safe for missing maps, so the arena self-heals and the
next scheduled start works once the config is fixed.

## Waves

- **Count:** `baseCount + perPlayer × aliveParticipants`, no cap. Boss rounds:
  `count × bossRoundAddFraction`.
- **Placement:** random free tiles in the arena rect, all at once, at least
  `minPlayerDistance` tiles from any participant.
- **Bands:** rounds grouped in bands of `bandSize`. Each band lists 2–3 normal templates and
  1 boss template. Past the last band, the last band repeats. Initial roster: ~6–9 normal +
  3 boss templates, arena-only, reusing existing graphics.
- **Elites:** from `eliteFromRound`, each mob has `eliteChance`, growing by
  `eliteChanceGrowth` per round up to `eliteChanceMax`. Elites get `eliteHpMult`,
  `eliteDmgMult`, `eliteBountyMult`, an "Elite" title and a tint.
- **Bosses:** every `bossEvery` rounds, the band's boss with
  `bossHpMult × (1 + bossHpPerPlayer × (alive − 1))`, `bossDmgMult`, `bossBountyMult`, plus
  adds.
- **Targeting:** on spawn, a mob aggroes the nearest participant. Engine aggro (threat,
  taunts) takes over from there. If the target dies or leaves, the mob re-aggroes the nearest
  participant.
- **Mob stats:** HP set from the formula through `TemporaryMaxHP` at spawn. Damage from the
  formula only. Templates supply looks, speed, ranges, AC and armor pierce; template XP is
  awarded by the engine; template drops should be empty.

## Scaling

One value per player, from experience sold:

```
s = clamp(xpSold, minXpSold, topXpSold) / topXpSold        // 0 < s ≤ 1
```

**Player → mob** (`ArenaMob.InterceptDamageTaken`):

```
effective = raw × s^(−a)                                    // a ≈ 0.75
```

Pool-driven spell damage grows about linearly with experience sold (measured log-log slope
0.97 on production data), so this leaves an advantage of about `(xpSold ratio)^(1−a)` from
grind — about 5.6x across the real range — while gear, spell damage %, crit and enchants
count in full.

**Mob HP:**

```
mobHp(round, tier) = anchorMobHp × hpGrowth^(round − anchorRound) × tierHpMult
```

**Mob → player** (`ArenaMob.MeleeBaseDamage`; engine attack loop):

```
weaponDmg = anchorMobWeaponDamage × dmgGrowth^(round − anchorRound) × tierDmgMult × s^b    // b ≈ 0.75
hit       = engine melee mitigation(weaponDmg, target)                                    // crit, DR, AC
```

**Anchor:** round 20, a 3B player: 12M HP, 65k weapon damage. With `hpGrowth` 1.10 and
`dmgGrowth` 1.08, round 1 is ~2.0M HP / ~15k weapon damage.

Round 20 normal mob (12M HP), `a = b = 0.75` (mid and low raw hits are estimates):

| Player | XP sold | `s` | Raw hit → effective | Hits to kill | Mob weapon damage before AC |
|---|---|---|---|---|---|
| Top | 3B | 1 | 5M → 5M | ~2.4 | 65k |
| Mid | 300M | 0.1 | 500k → 2.8M | ~4.3 | 11.6k |
| Low | 20M | 0.0067 | 35k → 1.5M | ~8 | 1.5k |

No class adjustment. If squishy classes suffer, per-class defence weights are the planned
follow-up.

## Rewards

- **Round clear:** each surviving participant gets `roundBase + roundStep × round` coins.
- **Kill bounty:** `mobBounty × tierBountyMult`, split by each attacker's share of the
  effective (post-scaling) damage the mob took, recorded in `ArenaMob`.
- **End-of-run bonus:** highest milestone reached from a table, e.g.
  `{5: 20, 10: 60, 15: 120, 20: 200}`.
- Round and kill coins are credited live; bonuses at run end.
- Rates are tuned so an average player reaching about round 15 earns a target amount per
  run, and shop prices are expressed in runs.

## NPCs and leaderboards

- **Arena Master** (lobby, data-defined, `script_path = Scripts/Arena/ArenaMaster.csx`).
  Left-click opens an `OptionListWindow` hub:
  - `Arena Coins: 1,240`
  - `Next run in 1h 42m` / `Run in progress: round 12, 8 alive`
  - `Last run: round {best} ({n} players)` when a last run exists
  - `The arena is closed.` when the config is disabled (an active run takes precedence over
    the closed state)
  - `Leaderboards` → Best round / Total kills → Weekly / All-time → top 10 + own rank.
- **Arena vendor** (lobby, data-defined): ordinary vendor with `currency_id = 'arena_coin'`.
- **Leaderboard storage:** `WorldState["arena.board"]` as a JSON string keyed by player id:
  all-time best round (with the time it was achieved) and total kills, plus an ISO week key
  with the weekly best round (with time) and weekly kills. No names are stored — names are
  resolved through `PlayerHandler.GetPlayerName` at display time, so renames are reflected.
  Weekly values reset lazily when the stored week key differs from the current ISO week, and
  weekly reads treat a stale week as zero without writing.
- **Leaderboard ordering:** best round is ranked by value descending, then time achieved
  ascending (whoever got there first), then player id; kills by value descending, then
  player id. Zero values are excluded — a player with no value in a view is unranked.

## Crowd spectators

The background crowd NPCs on the arena map (templates 291–371, 373–392, 394–440, all ambient
spawns) use `Scripts/Arena/ArenaCrowd.csx`. Each one shows a random emoticon to nearby players
every 20–60s and plays the attack animation (`ATT`) every 20–60s. Templates whose
`script_params` contains `fast` (394, 398, 400, 401, 402, 403, 411) play the attack animation
every 3–8s. The first action is spread randomly across the window so the crowd doesn't act in
unison. Timers retire when the NPC dies or respawns, and pick up script reloads.

## GM commands

- `/arena start` / `/arena stop` — force or abort a run.
- `/arena status` — phase, round, alive count, next start.
- `/arena preview <round> [raw] [player]` — for the named online player (or the caller):
  their power fraction `s`, normal/elite/boss HP for that round (alive = participants if a
  run is active, else 1), their effective hit for a sample raw value (the `raw` arg, default
  1,000,000), and mob weapon damage before and after `MitigateMelee` for all three tiers.

## Configuration (`arena.json`)

`lobbyMap`, `lobbySpawn`, `arenaMap`, `arenaRect`, `intervalHours`, `anchorUtc`, `announceMinutes`,
`countdownSeconds`, `breakSeconds`, `roundSeconds`, `timeoutWarningSeconds`,
`stragglerCount`, `minPlayerDistance`, `baseCount`, `perPlayer`, `bandSize`, `bands[]`
(`normals[]`, `boss`), `eliteFromRound`, `eliteChance`, `eliteChanceGrowth`,
`eliteChanceMax`, `eliteHpMult`, `eliteDmgMult`, `bossEvery`, `bossRoundAddFraction`,
`bossHpMult`, `bossHpPerPlayer`, `bossDmgMult`, `minXpSold`, `topXpSold`, `a`, `b`,
`anchorRound`, `anchorMobHp`, `anchorMobWeaponDamage`, `hpGrowth`, `dmgGrowth`,
`currencyId`, `roundBase`, `roundStep`, `mobBounty`, `eliteBountyMult`, `bossBountyMult`,
`milestones`.

Validation: `intervalHours` a positive divisor of 24 (the daily anchor sequence only stays
consistent when it divides the day), `anchorUtc` an `HH:mm` time, at least one band with at least
one normal template, `roundSeconds > timeoutWarningSeconds`, positive growth/anchor values,
`a`/`b` in `[0, 2]`. A failing config disables the arena and logs once; the arena also stays
disabled while `lobbyMap`/`arenaMap` are 0 or missing from the world.

## Testing

- Server seams: unit tests in `Goose.Tests` following existing hook tests
  (`MapPlayerCanJoinHookTests`, `GlobalNpcKilledHookTests`); currency persistence in
  `Goose.IntegrationTests`.
- Scripts: compiled and exercised like `WanderingChestScriptTests` — `ArenaMath` functions,
  controller state machine with controlled time, config validation.
- Manual: `/arena start`, `/arena preview`, editing `arena.json` live on a dev server.

## Plans

1. `2026-11-05-arena-part1-currencies.md` — data-driven currencies, `/wallet`
2. `2026-11-05-arena-part2-seams.md` — death hook, NPC damage hooks, `MitigateMelee`
3. `2026-11-05-arena-part3-core.md` — arena scripts
4. `2026-11-05-arena-part4-hub-leaderboards.md` — click hook, Arena Master, leaderboards

## Deferred

- Per-class defence weights.
- Weekly top-N rewards.
- Resurrection.
- Client wallet button.
- All tuning numbers (rewards, growth, exponents, anchor round).
- Elite special behaviour; role-based mob AI.
