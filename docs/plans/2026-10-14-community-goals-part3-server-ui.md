# Community Goals Part 3 (Player-Facing Server) Implementation Plan

**Goal:** Let players see, contribute to and claim from community goals at a dedicated NPC, credit kills, and announce completions.

**Architecture:** Left-click on an NPC hosting goals opens `CommunityGoalWindow.Handle` (hub → description → contribute). Donations go through a new generic `AmountWindow` (frame 31, `AMI`/`AMT` packets) whose confirm callback calls `CommunityGoalHandler.Contribute` with take-then-credit. NPC death collects the credited player set once and calls `OnNpcKilled`.

**Tech Stack:** C# / .NET 10, xUnit with `TestWorldFixture.CapturingPlayer` packet capture.

Design: `docs/plans/2026-10-14-community-goals-design.md`. **Part 3 of 4**; assumes Parts 1–2 merged (`Contribute`, `Remaining`, `RunCompleted`, `PendingPrizes`, `Claim`, `GetSummaryText`, `results`).

---

## APIs verified

- Left-click routing: `PlayerLeftClickEvent` loops NPCs in range, `npc.Quests.Any()` → `QuestWindow.Handle` `Goose/Events/PlayerLeftClickEvent.cs:47-55`.
- `QuestWindow.Handle` single-quest skip and `OptionListWindow` usage `Goose/Quests/QuestWindow.cs:60-88`.
- NPC kill credit `Goose/NPC.cs:1205-1236`; `Group.GainExperience(NPC, GameWorld)` `Goose/Group.cs:232`, per-member `player.Killed` `:259,264` (range + level-gap rules `:245-256`).
- Banked XP is `Player.Experience` (`QuestStateResolver.cs:106-107`); quests take it with `player.AddExperience(-amount, world, Player.ExperienceMessage.None)` `Goose/Quests/QuestWindow.cs:661-663`.
- Gold: `Player.Gold` `Goose/Player.cs:284`, `RemoveGold(long, GameWorld)` `:1716`.
- Items: `Inventory.GetInventoryItemFamilyCount(baseTemplateId)` `Goose/Inventory.cs:954`, `TryRemoveInventoryItemFamily(baseTemplateId, n, world)` `:968` (quests count families; `ItemTemplate.BaseTemplateID`).
- Item graphics: `ItemTemplate.GraphicFile` (sheet) / `GraphicTile` (id) / `GraphicR..A` `Goose/ItemTemplate.cs:77-82`; line icon tuple `(Sheet, Graphic, R, G, B, A)` `Goose/OptionListWindow.cs:6`.
- `OptionListWindow(player, world, title, lines, onLineClicked, npc, startPage, lineGraphics, openingLine, lineItems)` `Goose/OptionListWindow.cs:11`; 8 lines/page (`Window.LineClickCount` `Goose/Window.cs:63`); Back/Next used for paging `:34-38,72-90`. `WindowLineItem` replaces the line text with the item name `Goose/Packets.cs:489-496` — so requirement lines use `lineGraphics`, not `lineItems`.
- Quest frame multi-line text: `QuestInfoWindow.Populate` sends `P.WindowTextLine` per `\n`-split line `Goose/Quests/QuestInfoWindow.cs:29-40`; Next/Back handling `:56-76`; button string order `combine,close,back,next,ok` (`OptionListWindow.cs:36`).
- `Window.SendCreate` `Goose/Window.cs:319`, `Close` `:330-334`, `ButtonTypes` `:13-21`, `WindowFrames` `:24-56` (last `GenericContainer = 30`), `WindowTypes` `:64-85`.
- Packets: `P.MakeWindow` `Goose/Packets.cs:738-747`, `P.WindowTextLine` `:470`, `P.WindowLine` `:477`, `P.ServerMessage` `:38`.
- Packet registration table `Goose/EventHandler.cs:120-161` (e.g. `("CWC", Open(typeof(CustomWindowCreateEvent)))`); parsing precedent `Goose/Events/CustomWindowCreateEvent.cs`.
- Range check `Map.InRange(player, npc)` (used by `BankWindow.BankerInRange` `Goose/BankWindow.cs:66-69`).
- Broadcast `world.SendToAll(string)` `Goose/GameWorld.cs:838`; precedent `[World First]` message `QuestWindow.cs:425-429`.
- Existing wrap logic (help only, 53 chars) `Goose/Commands/HelpFormatter.cs:10-40`.
- Test player capture: `TestWorldFixture.CommandPlayerOn(map, x, y)` returns `CapturingPlayer` with `Sent` `TestSupport/TestWorldFixture.cs:93-113`.

## Conventions

As Parts 1–2. All window handlers run on the game thread (packet events).

---

### Task 0: Text wrapping helper

**Files:** create `Goose/TextWrap.cs` — `public static List<string> Lines(string text, int width = 50)`: splits on literal `\n` (the two-character sequence the quest data uses) and real newlines, then word-wraps each paragraph; words longer than `width` are hard-split; blank paragraphs preserved. Test `Goose.Tests/TextWrapTests.cs` (boundary at exactly 50, long word, `\n` paragraphs, empty string → one empty line).

Commit `feat(ui): add server-side text wrapping`.

---

### Task 1: Kill credit refactor

**Mutation impact:**
- Source of truth: quest progress via `Player.Killed` (`Player.cs:1278-1281`) — **must not change**.
- New reader: `CommunityGoalHandler.OnNpcKilled(NPC npc, IReadOnlyList<Player> credited, GameWorld world)`.
- Propagation: `Group.GainExperience` returns the members it called `Killed` on; the solo/pet branches build a one-element list (pet → owner); NPC death calls `OnNpcKilled` once after credit.

**Files:** modify `Goose/Group.cs:232-268` (return `List<Player>`), `Goose/NPC.cs:1205-1236`; handler `OnNpcKilled` — looks up the Part 1 kill index by `npc.NPCTemplateID`; for each (goal, requirement) with an active run: `Contribute(goal, null, key, 1)` for the total, then add +1 to each credited player's amount **without** a second total increment. Implement as an internal `Contribute` overload taking a player list: total += 1 (clamped), each credited player's amount += accepted.

**Tests:** `Goose.Tests/CommunityGoals/KillCreditTests.cs` — solo kill: total +1, player +1; group of 3 eligible + 1 out of range: total +1, three contributors +1, the out-of-range member none; pet kill credits owner; non-goal NPC kill leaves all goals untouched; regression: quest kill progress for a group is unchanged (assert `QuestProgress` values as before the refactor).

Commit `feat(community-goals): credit kills once per NPC death`.

---

### Task 2: AmountWindow and packets

**Files:**
- Modify: `Goose/Window.cs` — `WindowFrames.Amount = 31`, `WindowTypes.Amount`.
- Create: `Goose/AmountWindow.cs`:

```csharp
public AmountWindow(Player player, GameWorld world, string title, string prompt,
    (int Sheet, int Graphic, int R, int G, int B, int A) icon,
    long min, long max, long defaultValue, Action<long, Player, GameWorld> onConfirm, NPC? npc = null)
public void Submit(long amount, Player player, GameWorld world)
```

  `Buttons = "0,1,0,0,0"`; `Populate` sends `P.AmountInfo`. `Submit`: reject (log at debug, no callback) when `amount < min || amount > max`; otherwise `Close` then `onConfirm`. Exit/Close buttons remove the window.
- Modify: `Goose/Packets.cs` — `AmountInfo = (windowId, icon, min, max, def, prompt) => $"AMI{windowId}|{sheet}|{graphic}|{r}|{g}|{b}|{a}|{min}|{max}|{def}|{prompt}"`. **Prompt is the last field** so it may contain `|`; the client splits with a field limit. (Design listed prompt before min/max — this order is the contract; Part 4 mirrors it.)
- Create: `Goose/Events/AmountWindowSubmitEvent.cs` — parses `AMT<windowId>,<amount>` (`long.TryParse`, invariant), requires `Player.State == Ready`, finds `player.Windows.OfType<AmountWindow>().FirstOrDefault(w => w.ID == id)`, calls `Submit`.
- Modify: `Goose/EventHandler.cs:120-161` — `("AMT", Open(typeof(AmountWindowSubmitEvent)))`.

**Tests:** `Goose.Tests/AmountWindowTests.cs` — create sends `MKW…,31,…` then the exact `AMI` string; valid submit invokes callback once and closes (`CLW`); out-of-range, negative, non-numeric and unknown window id never invoke the callback (adversarial); prompt containing `|` round-trips as the final field.

Commit `feat(ui): add generic amount window`.

---

### Task 3: OptionListWindow back hook

Contribute screen needs Back → description, but `OptionListWindow` uses Back for paging.

**Files:** modify `Goose/OptionListWindow.cs` — optional ctor param `Action<Player, GameWorld>? onBack = null`; `GetPagingButtons` enables Back when `page > 0 || onBack is not null`; `Clicked(Back)` on page 0 with `onBack` → `Close` then `onBack`. Existing callers unchanged.

**Tests:** extend existing option-list tests (find with `grep -rl OptionListWindow Goose.Tests`): page-0 Back without hook stays disabled (regression); with hook, page-0 Back invokes it once; page>0 Back still pages.

Commit `feat(ui): option list back hook`.

---

### Task 4: Goal windows (hub, description, contribute, top contributors)

**Files:**
- Create: `Goose/CommunityGoals/CommunityGoalWindow.cs` (static `Handle(NPC, Player, GameWorld)` + builders), `Goose/CommunityGoals/CommunityGoalInfoWindow.cs` (`Window`, `Frame = Quest`, `Type = WindowTypes.CommunityGoal` — add enum value).
- Modify: `Goose/Events/PlayerLeftClickEvent.cs:47-55` — before the quest check: `if (world.CommunityGoals.GoalsForNpc(npc.NPCTemplateID).Any()) { CommunityGoalWindow.Handle(npc, Player, world); return; }`.

Behaviour:
- `Handle` closes existing goal/option/amount windows for this NPC (as `QuestWindow.Handle` `:62-63`), calls `player.TalkedTo(npc, world)`.
- **Hub** (`OptionListWindow`, title = NPC name, opening line `"Community Goals"`): lines in order — each `PendingPrizes` → `"Claim: {goal.Name} (Run {n})"`; each active run → `"{goal.Name} – {GetSummaryText}"` + `" – {timeLeft}"` when timed (`"2d 4h"`, `"3h 12m"`, `"5m"`); finished runs (latest run per goal whose `CompletedAt` is within `max(14 days, claim window)`) → `"{goal.Name} – Completed"` / `"– Failed"`. Hub is skipped (open description directly) when exactly one active goal and no prizes and no finished lines.
- **Description** (`CommunityGoalInfoWindow`): `TextWrap.Lines(goal.Description)` then a blank line then status lines: active → `"Progress: {summary}"`, `"Ends in {timeLeft}"`; finished → `"Completed {date:yyyy-MM-dd}"` and, for lotteries/ranked, `"1st: {name}"`… from `results.places` (names via `PlayerHandler.GetPlayerName`, `"Unknown"` fallback). Buttons: Next (active only) / Back (when opened from hub) / Close.
- **Contribute** (`OptionListWindow` with `onBack` → description): one line per requirement:
  - Item: icon from template; `"{template.Name} {total:N0} / {target:N0} (you: {mine:N0})"` (omit `/ target` when untargeted).
  - Gold: icon from `Settings.GoldItemID` template; Experience: icon `(Settings.ExperienceIconSheet, Settings.ExperienceIconId, 0,0,0,0)` — add both settings (default 0 = no icon, placeholder art to be chosen).
  - Kill: icon none; display-only (click → `ServerMessage("Slay {npcName} to contribute.")`).
  - Custom: label + totals, display-only.
  - Lottery goals: the entry-cost requirement line reads `"Tickets: {cost:N0}g each – you hold {myEntries} of {allEntries}"`.
  - Final line `"Top contributors"` → OptionList of up to 10 `"{i}. {name} – {score:N0}"` (live ranking by score for active runs; `results.top` for finished), Back → contribute.
- **Prize line click** → `world.CommunityGoals.Claim(...)`; refusal → `ServerMessage(refusal)`; success → reopen hub.

**Tests:** `Goose.Tests/CommunityGoals/CommunityGoalWindowTests.cs` with `CapturingPlayer` — left-click on a goal NPC opens the hub (and not the quest window even if the template has quests); single active goal skips to description; description text lines are ≤ 50 chars; contribute lines show totals and icons (assert `WNF` fields); finished goal older than 14 days not listed (adversarial boundary at 14d ± 1 minute); prize claim success removes the line on reopen.

Commit `feat(community-goals): goal NPC windows`.

---

### Task 5: Donations through the amount window

**Mutation impact:**
- Sources of truth: `Player.Gold`, `Player.Experience`, inventory slots; run totals/contributions.
- Propagation: confirm → re-validate → **take** (exact accepted amount) → `Contribute` credit → reopen contribute window (fresh totals).
- Invariants: never credit without taking; never take more than credited; take/credit both happen or neither.

**Files:** modify `CommunityGoalWindow.cs`; test `Goose.Tests/CommunityGoals/DonationTests.cs`.

Flow for a donatable line:
1. Compute `have` (item family count / `Gold` / `Experience`), `remaining = Remaining(goal, key)`.
2. Lottery (`entryCost[key]` present): amount unit is tickets — `max = min(have / cost, remaining / cost)`, default 1, prompt `"Buy how many tickets? ({cost:N0}g each)"`. Otherwise `max = min(have, remaining)`, default `max`, prompt `"Donate how many {label}? You have {have:N0}, {remaining:N0} still needed."` (omit needed when untargeted). `max == 0` → `ServerMessage` and no window.
3. On confirm (`units`): `amount = units * (cost ?? 1)`; re-check: window NPC still `Map.InRange`, run still active (else `"This goal has ended."`), `have >= amount`; `accepted = min(amount, Remaining)` (lottery: round down to whole tickets); take `accepted` (`TryRemoveInventoryItemFamily` / `RemoveGold` / `AddExperience(-accepted, …, None)`); if taking fails → message, no credit; then `Contribute(goal, player, key, accepted)`; assert result accepted == taken (log error if not — cannot happen on the game thread).
4. Reopen the contribute window.

**Tests:** donate 237 logs → inventory −237, total +237, mine +237; goal needs 10, player sends 50 → only 10 taken (adversarial); player drops items between open and confirm → no take, no credit; run completes between open and confirm → `"This goal has ended."`, nothing taken; out of range → nothing; ticket purchase of 5 at 1,000g → gold −5,000, entries +5; XP donation reduces `Experience` without changing level.

Commit `feat(community-goals): donations`.

---

### Task 6: Completion announcement

**Files:** handler subscribes in `Load` to `RunCompleted`: `world.SendToAll(P.ServerMessage(...))` — `"[Community] {goal.Name} has been completed!"`; lotteries/ranked with `places`: append `" Winner: {name}"` (1st place) or `" No winners."` when places is empty and the goal has rank brackets. Failed runs: no announcement.

**Tests:** completion broadcasts once to all online `CapturingPlayer`s; lottery winner name appears; empty lottery says "No winners."; failed run sends nothing.

Commit `feat(community-goals): announce completions`.

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Quest kill credit unchanged | regression in `KillCreditTests` |
| One total increment per NPC death | group kill test |
| Amount window never trusts client values | out-of-range/unknown-id tests |
| Take exactly what is credited | over-donation + race tests in `DonationTests` |
| Goal NPCs bypass quest flow | left-click test |
| Description fits the Quest frame | ≤ 50-char line test |
| 14-day finished-goal window | boundary test |

## Final checks

`dotnet test Goose.Tests` and `dotnet test Goose.IntegrationTests` green; `AMI` field order and frame id 31 recorded for Part 4.
