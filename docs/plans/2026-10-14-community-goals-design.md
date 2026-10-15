# Community goals design

Date: 2026-10-14
Branch: `feat-community-goals`

## Purpose

Server-wide goals that every player contributes to — donation drives, collective kill counts,
lotteries — with community rewards (chest pages, world flags, unlocking further goals, scripts)
and tiered personal rewards claimed from the hosting NPC. Goals are permanent milestones or
repeating events; both are built on **runs**.

## Current behavior (relevant)

- Quests are per-player (`Goose/Quests/*`); `OnlyOnePlayerCanComplete` + `quest_claims` is the
  only server-global quest state. Quest definitions are imported from CSV and DROP/CREATE'd on
  every import; live state lives in tables the importer never touches (`quest_claims`).
- Quest rewards are granted in the private `QuestWindow.GiveRewards`
  (`Goose/Quests/QuestWindow.cs:446`), with inventory-space pre-checks
  (`PlayerHasEnoughInventorySpaceForReward`, `IQuestScript.GetRequiredInventorySpace`).
- Kill credit: `NPC.cs:1226-1232` credits the top damager (or pet owner);
  `Group.GainExperience` calls `player.Killed(npc)` per in-range, level-eligible member
  (`Group.cs:259,264`). `Player.Killed` feeds quest progress (`Player.cs:1280`).
- Gold is `Player.Gold` (a number, not an inventory item); a gold item exists only on the ground
  (`GoldItemID`, `NPC.cs:1513`). Banked XP is likewise a player number.
- `ScriptHandler` caches one instance per script file (`IQuestScript.cs` header) — scripts must
  receive per-row/per-goal data on every call. Scripts live in `Goose/Data/Illutia/Scripts/`
  and reload off-thread with a game-thread swap.
- `OptionListWindow` renders clickable lines with optional icons and a **single-line, fixed
  height** heading (`OptionListMetrics.HeadingSize`). The Quest frame (`QuestInfoWindow`)
  renders multi-line text with Next/Back, and requires **server-side wrapping at ~50 chars**.
- `QuestWindow.Handle` (left-click) skips the list when exactly one quest is available.
- `ItemContainer.Resize` grows in place (`ItemContainer.cs:21`); `BankPageItem.csx` uses it and
  then updates open `BankWindow.MaxPages`. `CommunityChestWindow.MaxPages` is set once from the
  container size at construction (`CommunityChestWindow.cs:28`).
- `GameWorld` has an internal constructor taking a `Random` (`GameWorld.cs:207`); `world.Random`
  is the server RNG.

## Design

### Definitions

Imported table `community_goals` (new CsvToSql importer; replaced on every import):

| column | notes |
|---|---|
| `id` | int PK |
| `name`, `description` | description is server-wrapped for the Quest frame |
| `npc_template_id` | hosting NPC; several goals may share one |
| `prerequisite_goals` | comma list; each listed goal's **first run** must be `Completed` |
| `script_path` | empty → stock `BaseCommunityGoalScript` |
| `properties` | JSON: requirements, score weights, ranking, entryCost, brackets, community rewards, duration/closesAt, repeat, claimWindow |

Example (milestone):

```json
{
  "requirements": [
    { "key": "logs", "type": "Item", "item": 120, "target": 5000 },
    { "key": "gold", "type": "Gold", "target": 1000000 },
    { "key": "orcs", "type": "Kill", "npc": 44, "target": 10000 }
  ],
  "score": { "logs": 1, "gold": 0.01, "orcs": 5 },
  "brackets": [
    { "rank": 1, "rewards": [ { "type": "Title", "value": "Master Builder" } ] },
    { "minScore": 2000, "rewards": [ ... ] },
    { "minScore": 1, "rewards": [ ... ] }
  ],
  "communityRewards": [
    { "type": "ChestPages", "npc": 900, "pages": 1 },
    { "type": "WorldFlag", "name": "bridge_repaired" }
  ]
}
```

Example (weekly lottery):

```json
{
  "requirements": [ { "key": "gold", "type": "Gold" } ],
  "ranking": "lottery",
  "entryCost": { "gold": 1000 },
  "duration": "7d",
  "repeat": "onClose",
  "brackets": [
    { "rank": 1, "rewards": [ { "type": "Script", "script": "CommunityGoal/PotShare.csx", "params": { "key": "gold", "percent": 90 } } ] },
    { "rank": 2, "rewards": [ { "type": "Script", "script": "CommunityGoal/PotShare.csx", "params": { "key": "gold", "percent": 10 } } ] }
  ]
}
```

### Live state (own tables)

Created in `CreateDatabaseSchema` plus `CreateTableIfMissing` in `MigrateDatabaseSchema`;
never touched by the importer.

```sql
community_goal_runs (
  goal_id INT, run INT, status TEXT, started_at TEXT, completed_at TEXT NULL,
  totals TEXT,        -- JSON {key: amount}
  results TEXT NULL,  -- JSON: ranking places, top-10 summary
  PRIMARY KEY (goal_id, run))

community_goal_contributions (
  goal_id INT, run INT, player_id INT,
  amounts TEXT,       -- JSON {key: amount}
  last_at TEXT, claimed_bracket INT NULL, claimed_at TEXT NULL,
  PRIMARY KEY (goal_id, run, player_id))

world_flags (name TEXT PRIMARY KEY, value INT, set_at TEXT, source_goal_id INT)
```

- Run status: `Active`, `Completed`, `Failed`. Current run = highest `run` for the goal.
- History is the run rows; expiry deletes contribution rows only, `results` keeps the summary.
- Chest bonus pages are the world flag `chest_pages:{npcTemplateId}` (value = pages);
  `ChestHandler` adds it to `CommunityChestPages`. Plain flags use value 1.
- `CommunityGoalHandler` loads definitions + live state at startup (after Global Scripts,
  before Players), is the source of truth during play, and is **game-thread only**.
- Persistence: mutations mark rows dirty; a periodic save on the `WorldSavePeriod` cadence
  upserts dirty rows (and deletes expired ones) in **one transaction**, plan built on the game
  thread (WorldState precedent). A run's completion and its flags land in the same save.
  `GameWorld.Stop` flushes synchronously.
- **Accepted risk**: donations and claims rely on regular saves; a crash between player and
  community saves can duplicate or lose items/rewards. Judged very unlikely; no per-action saves.

### Contribution flow

Single entry point `CommunityGoalHandler.Contribute(goal, player?, key, amount)`:

1. Ignore if the goal has no active run.
2. `script.CanContribute` — refusal message to the player.
3. Clamp to `target - total` (no clamp for target-less requirements).
4. Add to `totals[key]` and, with a player, to their `amounts[key]`; set `lastAt`.
5. `script.OnContributed`, then `CheckCompletion`.

- **Donations** (item, gold, banked XP): the amount window's confirm callback re-validates window,
  range, active run, holdings; then **take, then credit** — only the clamped amount is taken.
- **Kills**: NPC death collects the credited player set (same rules as quest credit — range,
  level gap, pet owner) and calls `OnNpcKilled(npc, creditedPlayers)` once: total +1, each
  credited player +1. Requires refactoring `NPC.cs`/`Group.GainExperience` to collect the set
  without changing quest-credit behavior. A kill index (NPC template → requirements) makes
  non-goal kills one dictionary miss.
- **Custom**: `Requirement.Custom("boss_damage")` + any script calling
  `world.CommunityGoals.Contribute(...)`; display-only lines.
- XP donations use **banked XP only**.

### Completion and run lifecycle

`CheckCompletion` (after each contribution and from the periodic check):

- Completes when all targeted requirements are met, **or** `closesAt` / `startedAt + duration`
  passes, **or** `script.IsComplete` returns true.
- Timeout with unmet targets → `Failed`: no community rewards, no brackets, donations kept.
  Lotteries and target-less goals time out as `Completed`.
- On completion: freeze ranking → build `results` → mark row dirty → `script.OnCompleted` →
  community rewards (chest pages, flags, scripts) → server-wide announcement (completion only;
  scripts may announce more) → schedule next run if `repeat`.
- `CommunityGoalTickEvent` every 60s: time-based completion, starting runs whose `nextRunAt`
  passed (`repeat: "onClose"` starts immediately; `"every": "7d"` schedules), starting the first
  run of goals whose prerequisites are now met, claim-window expiry.
- `claimWindow`: one-run goals never expire; repeating goals default to 30 days (overridable).
  After the window **everything** unclaimed expires (prizes included); contribution rows are
  deleted, the run row/summary stays.
- A `Failed` one-run goal blocks its prerequisite chain permanently (known limitation; SQL fix).

### Ranking and brackets

- Score = weighted sum of a player's amounts (`score` weights, default 1). A bracket may set
  `"measure": "<key>"` to compare against one requirement instead.
- `"ranking": "score"` (default): highest score first; ties → earlier `lastAt`.
- `"ranking": "lottery"`: entries = score ÷ `entryCost`; weighted draw **without replacement**
  using `world.Random`, drawing as many places as the highest `rank` bracket. Others unranked.
  Ticket purchases are entered as ticket counts; the callback multiplies by the price.
- Ranking is frozen at completion and stored in `results`; never redrawn after reload/restart.
- Brackets are ordered; **first match only** (`rank`, `minScore`, optional `measure`).
  `minScore: 1` as the last bracket is the consolation prize. No match → nothing to claim.
- Zero entries / fewer contributors than places → fewer or no winners; announce "no winners";
  unpaid shares are simply not paid.

### Rewards (pluggable)

Reward entry `{ "type": ..., ...params }`:

- Built-in personal: the quest reward types (Gold, Item, Title, Surname, Experience,
  SpellBuff, …) via a shared helper extracted from `QuestWindow.GiveRewards`.
- Built-in community: `ChestPages`, `WorldFlag`.
- `{ "type": "Script", "script": "<path>", "params": {...} }` → `ICommunityRewardScript`:
  `CanGive(ctx)` (refusal or null), `GetRequiredInventorySpace(ctx)`, `Give(ctx)`; `ctx` =
  goal, run, results, player (null for community rewards), bracket, params.
- **No built-in pot concept.** `PotShare.csx` (shipped in game data) pays
  `ctx.Run.Totals[params.key] * percent / 100` gold.

**Chest pages at runtime**: extract the grow-in-place logic from `BankPageItem.csx` into a
shared helper (grow container via `ItemContainer.Resize`, never shrink; update open windows'
`MaxPages`). `ChestHandler` uses it when `chest_pages:*` changes; `BankPageItem.csx` is
switched to it.

### Script API

```csharp
public interface ICommunityGoalScript
{
    CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world);
    string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world);
    void OnContributed(CommunityGoal goal, CommunityGoalRun run, Player? player, string key, long amount, GameWorld world);
    bool IsComplete(CommunityGoal goal, CommunityGoalRun run, GameWorld world);
    void OnCompleted(CommunityGoal goal, CommunityGoalRun run, GameWorld world);
    string GetSummaryText(CommunityGoal goal, CommunityGoalRun run, GameWorld world);
    CommunityGoalBracket? GetBracket(CommunityGoal goal, CommunityGoalRun run, Player player, GameWorld world);
}
```

- One shared instance per file; goal/run passed on every call; no per-goal fields.
- `BaseCommunityGoalScript` implements everything from `properties`. `Define` parses JSON
  through public helpers custom scripts also use (`Requirement.Item/Gold/Experience/Kill/Custom`,
  `Bracket.Rank/MinScore`, `Ranking.Score/Lottery`) — one `CommunityGoalDefinition` shape.
- `Define` results are cached **on the goal**, and re-run for every goal on `/reloadsql` and
  `/reloadscripts` as part of the existing game-thread commit. Progress is keyed by requirement
  key, so it survives redefinition; amounts for removed keys are kept but ignored.
- `GetSummaryText` default: `72%` for targeted goals, raw total (e.g. `184,000 gold`) otherwise.
- Load validation (per goal; others still load): unknown requirement/reward type, duplicate
  keys, `measure` to a missing key, no termination (no targets, no duration/closesAt, stock
  script), lottery without `entryCost`, script compile failure → logged error, goal skipped.
- Goal rows deleted from the sheet: live rows kept, warning logged, prizes unreachable.

### NPC interaction and windows

Goal NPCs are **dedicated** in this release. **Left-click** on an NPC whose template hosts any
goal routes to `CommunityGoalWindow.Handle` and skips the quest flow.

1. **Hub** (`OptionListWindow`, heading "Community Goals"): unclaimed prizes (any run, any goal
   at this NPC) → active goals with `GetSummaryText` and time left → finished goals (latest run
   of each, visible for **14 days** after it ends, or its claim window if longer). Skipped when
   there is exactly one active goal and no prizes.
2. **Goal description** (Quest frame, server-wrapped at ~50 chars): description + status lines
   (progress, time left, or results for finished goals). Next / Back (to hub) / Close.
3. **Contribute** (`OptionListWindow`): one line per requirement with icon —
   `[Log] Logs 3,412 / 5,000 (you: 240)`; kill lines display-only; lottery
   `Tickets: 1,000g each – you hold 5 of 184`; "Top contributors" opens a top-10 list (shown for
   lotteries too). Donatable lines open the amount window.

**Amount window** (new `WindowFrames.Amount`, generic and reusable):

- Server: `AmountWindow(player, world, title, prompt, icon, min, max, default, Action<long, Player, GameWorld> onConfirm)`.
- Packets: `MKW` then `AMI<windowId>|<sheet>|<graphic>|r|g|b|a|<min>|<max>|<default>|<prompt>`
  (prompt is last so it may itself contain `|`; the client splits with a field limit);
  client confirm `AMT<windowId>,<amount>`; cancel via existing `WBC`.
- Client widget: icon, prompt, numeric input, Max, Donate, Cancel.
- Icons: item template graphic; gold → `GoldItemID` template graphic; XP → new settings
  `ExperienceIconSheet` / `ExperienceIconId` (placeholder art for now).
- Default value: Max for donations, 1 for tickets. Server re-validates min/max; callback
  re-validates goal state ("This goal has ended" if the run closed meanwhile).

**Claiming**: compute bracket (frozen ranking) → check inventory space across all rewards
(built-ins + script `GetRequiredInventorySpace`), refuse the whole claim if short → give all →
set `claimed_bracket`. Double claims refused.

### Testing

`Goose.Tests` (fast) and `Goose.IntegrationTests` (real DB); seeded `Random` via the internal
`GameWorld` constructor.

- Definition parsing: valid JSON round-trips; every validation rule rejects.
- Contribution: clamping; refusals mutate nothing; take-then-credit; kill credit total +1 and
  per-member +1; quest kill credit unchanged by the refactor.
- Completion: target met; timeout → `Failed`; lottery timeout → `Completed`; script
  `IsComplete`; `onClose` / `every` next run.
- Ranking: score tie → earlier `lastAt`; lottery without replacement (seeded); draw persisted
  once, not redrawn after reload.
- Brackets/claims: first match; mixed rank/threshold/consolation; insufficient space refuses
  whole claim; double claim refused; cross-run claims; expiry deletes contributions, keeps
  summary.
- Persistence (integration): dirty rows round-trip; completion + flags in one save; re-import
  leaves live tables untouched; orphan goal rows logged.
- Chest pages: grow in place keeps container identity and open viewers; `MaxPages` updated.
- Windows (Fakes): hub skip; amount window range rejection; ended-run rejection.
- Client (`Goose2Client.Tests`): `AMI`/`AMT` parse/format, amount widget; manual smoke.

## Deferred

- Multi-purpose NPC menus (shop / quests / goals from one click).
- Admin commands (force-complete, start run, reset) — SQL + restart for now.
- Server-wide timed buffs as a community reward.
- Milestone announcements (25/50/75%) — scripts may do their own.
- Drag-in donation box (would be a second entry into `Contribute`).
- Auto-expanding `OptionListWindow` heading.
- Final XP icon art.
