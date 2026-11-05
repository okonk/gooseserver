# Arena Part 4: Arena Master Hub and Leaderboards — Implementation Plan

**Goal:** Give the lobby an Arena Master NPC whose left-click opens a hub showing your Arena Coins,
the next or current run and the last run, and best-round and total-kills leaderboards with weekly
and all-time views.

**Architecture:** One small server seam: an NPC-script left-click hook, since nothing lets a
script handle NPC clicks today. Leaderboard records live in `WorldState["arena.board"]` as a
JSON string, written by `ArenaCore.RecordResults` (the Part 3 stub) at run end. The hub is
`OptionListWindow` screens, following `CommunityGoalWindow`.
Design: `docs/plans/2026-11-05-arena-design.md` (Part 4 of 4). **Requires Parts 1–3.**

**Tech Stack:** .NET 10, C#, C# scripts, xUnit.

**Conventions:** `AGENTS.md` — no new comments unless the "why" is non-obvious.

---

## APIs verified

| API | Location |
|---|---|
| NPC left-click: finds the NPC at the clicked tile → community goals → quests; **no script hook** | `Goose/Events/PlayerLeftClickEvent.cs:48-64` |
| `INPCScript` / `BaseNPCScript` virtual defaults | `Goose/Scripting/INPCScript.cs`, `BaseNPCScript.cs:9-38` |
| `OptionListWindow(player, world, title, lines, onLineClicked, npc, …, onBack)`; a line click closes the window then calls back | `Goose/OptionListWindow.cs:12,99-107` |
| Top-N list window precedent with back navigation and `PlayerHandler.GetPlayerName(id)` | `Goose/CommunityGoals/CommunityGoalWindow.cs:290-311` |
| Community goal window tests (click and line navigation) | `Goose.Tests/CommunityGoals/CommunityGoalWindowTests.cs` |
| `WorldState` JSON-string storage rule and run state | Part 3 plan, "Run state and shared operations" |
| `ArenaCore.RecordResults(world, run)` stub; `ArenaRun.Participants` (`Id` = `PlayerID`, `RoundsCleared`, `Kills`) | Part 3 plan, Task 3 |
| `ArenaMath.NextStartUtc`; `arena.run`; `arena.lastRun` | Part 3 plan, Tasks 2–3 |
| `CurrencyHandler.Get(id).GetBalance(player)` | Part 1 plan, Task 1 |
| `System.Globalization.ISOWeek.GetYear` / `GetWeekOfYear` | .NET BCL |

**Persistence:** `WorldState["arena.board"]`. No schema change.

**Manual data:** an Arena Master NPC template (`npc_type` vendor or ambient, no quests,
`script_path Scripts/Arena/ArenaMaster.csx`) and its spawn in the lobby. The Arena Coins vendor
is a plain vendor row with `currency_id = arena_coin` (Part 1).

---

### Task 1: NPC-script left-click hook

**Files:** modify `Goose/Scripting/INPCScript.cs`, `BaseNPCScript.cs`,
`Goose/Events/PlayerLeftClickEvent.cs:51-63`; test `Goose.Tests/NpcClickHookTests.cs`.

```csharp
// INPCScript / BaseNPCScript (default false)
bool OnPlayerClicked(NPC npc, Player player, GameWorld world);   // true = handled
```

In the NPC loop, after the tile match and **before** the community-goal check: call it inside
try/catch (`log.Error(e, "NPC OnPlayerClicked {0} ({1}) Exception", …)`, treat an exception as
false). If it returns true → `return`.

**Tests:**
- `Script_handles_click_and_suppresses_quest_window`: an NPC with a quest and a script returning
  true → no quest window opens.
- `Script_returning_false_falls_through_to_quests` (regression)
- `Throwing_script_falls_through` (adversarial)
- `Click_on_another_tile_does_not_invoke_script`

**Commit** `feat(scripting): NPC script OnPlayerClicked hook`

---

### Task 2: Leaderboard storage and ranking

**Files:** create `Scripts/Arena/ArenaBoard.csx` (`#load`ed by `ArenaCore.csx`); replace the
`RecordResults` stub; test `Goose.Tests/Arena/ArenaBoardTests.cs`.

```csharp
class ArenaBoardEntry { int Id; int BestRound; DateTime BestAtUtc; long Kills;
                        string Week; int WeekBestRound; DateTime WeekBestAtUtc; long WeekKills; }
class ArenaBoard { Dictionary<int, ArenaBoardEntry> Players; }

static string WeekKey(DateTime utc)                    // "2026-W45"
static ArenaBoard Load(GameWorld world)                // empty when absent or unparsable (log once)
static void Save(GameWorld world, ArenaBoard board)    // Set a fresh JSON string
static void Record(GameWorld world, ArenaRun run, DateTime nowUtc)
static List<(int Id, long Value)> Top(ArenaBoard b, ArenaBoardKind kind, bool weekly, string week, int n)
static (int Rank, long Value)? RankOf(ArenaBoard b, int id, ArenaBoardKind kind, bool weekly, string week)
enum ArenaBoardKind { BestRound, Kills }
```

- `Record`, for every participant in the run (alive or not):
  - if the entry's `Week` ≠ the current week, reset the weekly fields and set `Week`;
  - `Kills += k`, `WeekKills += k`;
  - if `RoundsCleared > BestRound` → set `BestRound` and `BestAtUtc = now`, and the same for the
    weekly fields.

  It's called once per run from `EndRun`, so a participant is never double-counted.
- **Weekly reads are lazy:** an entry whose `Week` ≠ the requested week counts as 0 for weekly views.
- **Order:**
  - Best round: value desc, then `BestAtUtc` asc (whoever got there first), then id.
  - Kills: value desc, then id.
  - Zero values are excluded.
- Names come from `world.PlayerHandler.GetPlayerName(id)` at display time, so renames are reflected.

**Tests:**
- `Record_keeps_max_best_round_and_sums_kills`
- `New_week_resets_weekly_but_not_all_time` (adversarial: a stale `Week` from last week)
- `Weekly_view_ignores_stale_entries_without_writes`
- `Best_round_tie_goes_to_earlier_achiever`
- `RankOf_outside_top_ten` (15 players)
- `WeekKey_iso_boundary`: 2026-12-31 and 2027-01-01 map to ISO weeks correctly.
- `EndRun_records_each_participant_once`: Part 3's end-run path with two participants → board
  has two entries with the run's values.

**Commit** `feat(arena): leaderboard storage and ranking`

---

### Task 3: Arena Master hub

**Files:** create `Scripts/Arena/ArenaMaster.csx`; test `Goose.Tests/Arena/ArenaMasterTests.cs`.

`OnPlayerClicked` → `Hub(npc, player, world)`; returns true.

**Hub** (`"Arena"`):
1. `Arena Coins: 1,240`: the balance of `config.currencyId`. Click → reopen the hub.
2. `Run in progress: round {Round}, {alive} alive` while `arena.run` exists, otherwise
   `Next run in {h}h {m}m` from `NextStartUtc` (`"Next run in under a minute"` below 60s), or
   `The arena is closed.` when the config is disabled. Click → reopen the hub.
3. `Last run: round {best} ({n} players)` from `arena.lastRun`, if present.
4. `Leaderboards` → board menu.

**Board menu** (`"Leaderboards"`, back → hub): `Best round - this week`,
`Best round - all time`, `Total kills - this week`, `Total kills - all time`.

**Board list** (back → board menu): `"{i}. {name} - {value:N0}"` for the top 10, then `""` and
`"You: #{rank} - {value:N0}"` or `"You: unranked"`. `"No entries yet."` when empty.

Every window is opened with `npc` so it closes when the player walks away, like the community-goal windows.

**Tests** (real `ArenaMaster.csx` via the kit; click via `PlayerLeftClickEvent` `LC{x},{y}`):
- `Click_opens_hub_with_balance_and_next_run`
- `Hub_shows_run_in_progress`
- `Disabled_config_shows_closed`
- `Leaderboard_navigation_and_back`
- `Board_shows_top_ten_and_own_rank`
- `Unranked_player_sees_unranked`

**Commit** `feat(arena): Arena Master hub and leaderboards`

---

### Task 4: Verification and docs

1. `dotnet test Goose.sln --no-restore` and `dotnet test Goose.IntegrationTests --no-restore` → PASS.
2. Extend `ArenaEndToEndTests.Full_run_two_players` (Part 3) to assert the board after the run.
3. Update the design doc: server change list gains `OnPlayerClicked`; leaderboard ordering rules.
4. Manual: click the Arena Master on a dev server and walk every screen.
5. Final report: the manual data rows for Parts 1–4.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Script click handling never breaks quests/goals | `Script_returning_false_falls_through_to_quests`, `Throwing_script_falls_through` |
| Weekly boards reset without a scheduled job | `New_week_resets_weekly_but_not_all_time`, `Weekly_view_ignores_stale_entries_without_writes` |
| All-time best only ever rises | `Record_keeps_max_best_round_and_sums_kills` |
| A run is recorded exactly once | `EndRun_records_each_participant_once` |
| Ties are deterministic | `Best_round_tie_goes_to_earlier_achiever` |
| Own rank is always shown | `RankOf_outside_top_ten`, `Unranked_player_sees_unranked` |
