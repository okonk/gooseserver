# Community Goals Part 2 (Ranking, Rewards, Claims) Implementation Plan

**Goal:** Freeze rankings (score or lottery) at completion, resolve brackets, grant community rewards (chest pages, world flags, scripts), and let players claim personal rewards exactly once.

**Architecture:** Ranking is computed inside `CheckCompletion` before `RunCompleted` fires and stored in `run.Results`. Rewards are `RewardSpec`s resolved by a `CommunityRewardRegistry`: quest-style built-ins go through a helper extracted from `QuestWindow.GiveRewards`; `ChestPages`/`WorldFlag` are community built-ins; `Script` resolves an `ICommunityRewardScript`. Claims are a handler method returning a result the Part 3 windows render.

**Tech Stack:** C# / .NET 10, Roslyn `.csx` scripts, System.Text.Json, xUnit.

Design: `docs/plans/2026-10-14-community-goals-design.md`. This is **Part 2 of 4**; it assumes Part 1 (`docs/plans/2026-10-14-community-goals-part1-foundation.md`) is merged: `CommunityGoalHandler`, `RewardSpec`, `GoalBracket`, `CommunityGoalRun.Results`, `RunCompleted`, flags dictionary + `dirtyFlags`, `UtcNow`.

---

## APIs verified

- `QuestWindow.GiveRewards(NPC, Player, GameWorld)` private, per-reward switch `Goose/Quests/QuestWindow.cs:446-584`; uses `npc` only for `SpellBuff` (`spellEffect.Cast(npc, player, world)`); `Script` rows call `IQuestScript.GiveReward`.
- `PlayerHasEnoughInventorySpaceForReward` counts 1 slot per Item reward + positive script `GetRequiredInventorySpace` `Goose/Quests/QuestWindow.cs:378-392`; `Inventory.GetNumberOfFreeSlots()` `Goose/Inventory.cs:163`.
- `Player.AddGold(long, GameWorld)` `Goose/Player.cs:1704`; `Inventory.AddItem(Item, long, GameWorld)` `Goose/Inventory.cs:78`.
- `RewardType` enum `Goose/Quests/QuestReward.cs:15-39`; `QuestReward` fields `LongValue/LongValue2/StringValue` `:41-49`.
- `ChestHandler.Load` sizes containers from `CommunityChestPages` `Goose/ChestHandler.cs:28-31`; `GetOrCreateContainer` `:72-83`; viewers registry `:9-11`.
- `CommunityChestWindow.MaxPages` set once from container size `Goose/CommunityChestWindow.cs:28`; title/buttons derive from it `:13-21`; page rerender via `SendCreate` (as `BankWindow.Clicked`).
- `ItemContainer.Resize(int)` grows in place, keeps object identity `Goose/ItemContainer.cs:21-25`.
- `BankPageItem.csx` grows bank containers + sets `BankWindow.MaxPages` `Goose/Data/Illutia/Scripts/Item/BankPageItem.csx:14-27`.
- `world.Random` `Goose/GameWorld.cs:182`; internal ctor with `Random` `:207`.
- Shipped-script test pattern: `Goose.Tests/ClassRewardScriptTests.cs:14-24` (compile from the Illutia data directory once per class).
- `/reloadsql` `CompileReferencedScripts` `Goose/Commands/ReloadSqlCommand.cs:53-66`.

## Conventions

As Part 1: no new comments unless non-obvious; game-thread only; namespace `Goose.CommunityGoals`.

---

### Task 0: Grow-to-pages helper (shared with the bank)

**Mutation impact:**
- Source of truth: `ItemContainer.slots` (`ItemContainer.cs:10`).
- Readers: `ItemContainerWindow` paging, `CommunityChestWindow.MaxPages`, `BankWindow.MaxPages`, `ChestHandler.Broadcast`, world-state save of chest containers.
- Propagation: `Resize` (same object, so viewers/subscriptions stay valid) → set each open window's `MaxPages` → for chest windows, `SendCreate` to refresh title/buttons.
- Invariant: never shrinks; whole pages only (`pages * slotsPerPage + 1`).

**Files:**
- Modify: `Goose/ItemContainer.cs` — `public bool GrowToPages(int pages, int slotsPerPage)` (returns true if it grew).
- Modify: `Goose/Data/Illutia/Scripts/Item/BankPageItem.csx:16-21` — use `container.GrowToPages(player.NumberOfBankPages, world.Settings.BankSlotsPerPage)`.
- Test: `Goose.Tests/ItemContainerGrowTests.cs`

**Tests:** grows to `pages*spp+1`; smaller request is a no-op returning false (adversarial: never shrinks, items in high slots survive); existing `SlotChanged` subscriber still fires after growth (identity preserved).

Commit `refactor(containers): add GrowToPages and use it for bank pages`.

---

### Task 1: World flags and chest bonus pages

**Mutation impact:**
- Source of truth: handler flag dictionary (Part 1 Task 5).
- Readers: `ChestHandler.Load` / `GetOrCreateContainer` sizing, open `CommunityChestWindow`s, persistence (`dirtyFlags`).
- Propagation for `chest_pages:{id}`: `AddToFlag` → mark dirty → `ChestHandler.ApplyBonusPages(world, id)` → `GrowToPages` → each viewer window: `MaxPages = (MaxSlots-1+spp-1)/spp`, `SendCreate(player, world)`.

**Files:**
- Modify: `Goose/CommunityGoals/CommunityGoalHandler.cs` — `public void SetFlag(string name, int value, int? sourceGoalId)`, `public int AddToFlag(string name, int delta, int? sourceGoalId)`; both mark the flag dirty and set `SetAt = UtcNow()`.
- Modify: `Goose/ChestHandler.cs` — `int PagesFor(GameWorld world, int npcTemplateId) => Math.Max(1, world.Settings.CommunityChestPages) + world.CommunityGoals.GetFlag("chest_pages:" + npcTemplateId)`; use it in `Load` (`:28-31`) and `GetOrCreateContainer` (`:77`); add `public void ApplyBonusPages(GameWorld world, int npcTemplateId)` (no-op if no container yet — it will be created at the right size).
- Tests: `Goose.Tests/CommunityGoals/WorldFlagTests.cs`, extend `Goose.Tests/ChestHandlerTests.cs`.

**Tests:** flag set/add marks dirty and round-trips through Part 1 persistence (integration test in `CommunityGoalPersistenceTests`); chest created after a +1 flag has `CommunityChestPages+1` pages; `ApplyBonusPages` on an open chest grows the **same** container, updates `MaxPages`, and the viewer receives a re-create (assert window packets); load order: flags loaded before chests (Part 1 Task 5 placed the load step) so a restarted server sizes chests with the bonus.

Commit `feat(community-goals): world flags and chest bonus pages`.

---

### Task 2: Extract quest reward granting

**Helper contract:** `QuestRewardGranter.Give(QuestReward reward, NPC npc, Player player, GameWorld world)` — grants one reward and sends the `[Quest Reward]` message exactly as today; does **not** check inventory space, complete quests, or persist. `QuestRewardGranter.RequiredInventorySlots(IEnumerable<QuestReward>, Player, GameWorld)` — the counting logic from `:378-392` without the free-slot comparison.

**Files:**
- Create: `Goose/Quests/QuestRewardGranter.cs` (move the switch body from `QuestWindow.cs:449-576` and `AddPlayerStats` `:586-591`).
- Modify: `Goose/Quests/QuestWindow.cs` — `GiveRewards` loops `QuestRewardGranter.Give`; `PlayerHasEnoughInventorySpaceForReward` uses `RequiredInventorySlots`.

**Tests:** existing quest reward tests stay green (`dotnet test Goose.Tests --filter FullyQualifiedName~Quest`); add `Goose.Tests/Quests/QuestRewardGranterTests.cs` covering Gold, Item (stack), Title through the helper directly.

Commit `refactor(quests): extract quest reward granting`.

---

### Task 3: Reward specs, registry and reward scripts

**Files (create):**
- `Goose/Scripting/ICommunityRewardScript.cs`:

```csharp
public interface ICommunityRewardScript
{
    string? CanGive(CommunityRewardContext ctx);
    int GetRequiredInventorySpace(CommunityRewardContext ctx);
    void Give(CommunityRewardContext ctx);
}
```

- `Goose/Scripting/BaseCommunityRewardScript.cs` (null / 0 / no-op).
- `Goose/CommunityGoals/CommunityRewardContext.cs` — `CommunityGoal Goal, CommunityGoalRun Run, Player? Player, NPC? Npc, GoalBracket? Bracket, JsonObject Params, GameWorld World`.
- `Goose/CommunityGoals/CommunityRewardRegistry.cs`:
  - `static IReadOnlyList<string> Validate(RewardSpec spec, bool community)` — errors for unknown type, missing `value`, `Script` without `script`, personal-only types used as community rewards and vice versa.
  - Personal built-ins: any `RewardType` name except `Script`, mapped `value` → `LongValue` (number) or `StringValue` (string), `value2` → `LongValue2` (Item stack defaults to 1). Same fields as the Quest Rewards sheet so authors use one vocabulary: `{ "type": "Item", "value": 120, "value2": 5 }`, `{ "type": "Title", "value": "Master Builder" }`.
  - Community built-ins: `{ "type": "ChestPages", "npc": 900, "pages": 1 }`, `{ "type": "WorldFlag", "name": "bridge_repaired" }`.
  - `Script` (both scopes): `{ "type": "Script", "script": "<path>", "params": { ... } }` → `world.ScriptHandler.GetScript<ICommunityRewardScript>(path)`.
  - `int RequiredSlots(IEnumerable<RewardSpec>, ctx)`; `string? CanGive(IEnumerable<RewardSpec>, ctx)`; `void Give(IEnumerable<RewardSpec>, ctx)` (personal: convert to `QuestReward` and call `QuestRewardGranter.Give(reward, ctx.Npc!, ...)`; community: flags/chest via Task 1; script: `Give(ctx with Params = spec.params)`).
- `Goose/Data/Illutia/Scripts/CommunityGoal/PotShare.csx` — `CanGive` null; `Give`: `long pot = ctx.Run.Totals.GetValueOrDefault(key); player.AddGold(pot * percent / 100, world)` with a `[Community Reward]` server message; params `key` (string), `percent` (int 1-100). Bad params → log error, give nothing (it is a data error, validated at load where possible).

**Modify:**
- `CommunityGoalDefinitionParser.Validate` — run `CommunityRewardRegistry.Validate` on every bracket reward (`community: false`) and community reward (`community: true`).
- `Goose/Commands/ReloadSqlCommand.cs:53-66` — also compile reward scripts referenced from `community_goals.properties` (walk `brackets[].rewards[]` and `communityRewards[]` for `type == "Script"`; tolerate bad JSON) as `ICommunityRewardScript`.
- `CommunityGoalHandler.LoadDefinitions` — resolve reward scripts at load so a missing/broken script skips the goal with an error (same rule as goal scripts).

**Tests:** `Goose.Tests/CommunityGoals/CommunityRewardRegistryTests.cs` — validation per rule; Item/Gold/Title conversion grants via the extracted helper; `RequiredSlots` counts items + script space; `PotShare.csx` compiled from the shipped data dir (pattern `ClassRewardScriptTests.cs:14-24`) pays 90% of a 184,000 total = 165,600 (adversarial: uses the param `key`, not a hardcoded "gold" — test with key `"donations"`).

Commit `feat(community-goals): reward specs, registry and PotShare`.

---

### Task 4: Ranking and results

**Mutation impact:**
- Source of truth: `run.Results` (`JsonObject`), written once inside `CheckCompletion` **before** `RunCompleted` fires, then marked dirty.
- Readers: `GetBracket`, claim flow, Part 3 results text/top-10, history after expiry.
- Invariant: computed exactly once per run; never recomputed on load/reload (adversarial test).

**Files:**
- Create: `Goose/CommunityGoals/Ranking.cs` — `static double Score(CommunityGoalDefinition, Contribution)` (Σ weight·amount over **current** requirement keys, weight default 1; amounts for removed keys ignored); `static long Entries(CommunityGoalDefinition, Contribution)` (Σ floor(amount[k] / entryCost[k])); `static List<int> Freeze(CommunityGoalDefinition, CommunityGoalRun, Random)`.
- Modify: `CommunityGoalHandler.CheckCompletion` — on `Completed`, `run.Results = BuildResults(...)` then raise `RunCompleted`.

Rules:
- `places` length = highest `rank` across brackets (0 if none).
- Score: order by score desc, then `LastAt` asc, then `PlayerId` asc; exclude score ≤ 0.
- Lottery: weighted draw without replacement over players with entries > 0 using `world.Random.NextInt64(totalEntries)`; remove the winner and repeat.
- `results` JSON: `{ "places": [playerId...], "top": [{ "player": id, "score": n }] (top 10 by score), "totals": {...} }` — `totals` duplicated so the summary survives contribution expiry.

**Tests:** `Goose.Tests/CommunityGoals/RankingTests.cs` — score tie broken by earlier `LastAt` (adversarial: later contributor with equal score ranks lower); lottery with seeded `Random` is deterministic and never repeats a player; a player with 0 entries never wins; fewer contributors than places → shorter `places`; zero contributors → empty `places`; reloading state (Part 1 `LoadStateRows`) keeps the stored `places` unchanged.

Commit `feat(community-goals): freeze rankings at completion`.

---

### Task 5: Brackets and community rewards on completion

**Files:**
- Modify: `Goose/Scripting/BaseCommunityGoalScript.cs` — `GetBracket`: iterate `Definition.Brackets` in order; `Rank` matches when `places.IndexOf(player.PlayerID) == rank-1`; `MinScore` matches when `(Measure is null ? Score : amounts[Measure]) >= MinScore`; first match wins; no contribution → null.
- Modify: `CommunityGoalHandler` — subscribe itself to `RunCompleted` in `Load`: `CommunityRewardRegistry.Give(def.CommunityRewards, ctx{Player=null, Npc=null})`. Community `SpellBuff`-like personal types are rejected at validation, so `Npc` is never needed here.
- Add `public int? BracketIndex(CommunityGoal, CommunityGoalRun, Player)` → `Definition.Brackets.IndexOf(script.GetBracket(...))`, null if none.

**Tests:** first-match across the mixed example (rank 1, then 2000+, 1000+, consolation); `measure` bracket uses raw logs not weighted score; no bracket for a non-contributor; completion applies `ChestPages` (+1 flag) and `WorldFlag`; a `Failed` run applies **no** community rewards (adversarial).

Commit `feat(community-goals): brackets and community rewards`.

---

### Task 6: Claims

**Mutation impact:**
- Source of truth: `Contribution.ClaimedBracket/ClaimedAt`.
- Readers: hub prize list (Part 3), persistence.
- Propagation: validate everything → `RewardRegistry.CanGive` → space check → `Give` → set claimed fields → mark contribution dirty.
- Invariants: all-or-nothing (refusal leaves inventory, gold and claim fields untouched); at most one claim per (goal, run, player).

**Files:** modify `CommunityGoalHandler`; create `Goose/CommunityGoals/ClaimResult.cs`; tests `Goose.Tests/CommunityGoals/CommunityGoalClaimTests.cs`.

```csharp
public record PendingPrize(CommunityGoal Goal, CommunityGoalRun Run, int BracketIndex);
public IReadOnlyList<PendingPrize> PendingPrizes(Player player, int npcTemplateId);
public ClaimResult Claim(CommunityGoal goal, CommunityGoalRun run, Player player, NPC npc, GameWorld world);
```

`PendingPrizes`: goals hosted by the NPC, runs with status `Completed`, contribution present, `ClaimedBracket == null`, bracket not null, not expired.

`Claim` refusals (message strings are part of the contract Part 3 shows): run not completed → `"This goal hasn't finished yet."`; no bracket → `"You have nothing to claim."`; already claimed → `"You've already claimed this reward."`; expired/removed → `"This reward has expired."`; script `CanGive` message verbatim; space → `"You need {n} free inventory slots to claim this."`.

**Tests:** happy path grants Item + Title and sets claimed fields; insufficient space refuses and changes nothing (adversarial: gold/titles from the same bracket are **not** granted either); double claim refused; claims across runs (run 1 prize claimable while run 2 active); expired run refuses; `PendingPrizes` lists only this NPC's goals.

Commit `feat(community-goals): claim personal rewards`.

---

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Containers never shrink; identity kept | `ItemContainerGrowTests` |
| Chest pages apply live and after restart | `ChestHandlerTests` bonus tests + persistence round-trip |
| Pot is read from a param key, not hardcoded | `PotShare` custom-key test |
| Ranking frozen once, not redrawn | reload test in `RankingTests` |
| Ties go to earlier contributor | tie test |
| Draw without replacement | lottery uniqueness test |
| Failed runs give no community rewards | brackets/completion test |
| Claims all-or-nothing, once | space + double-claim tests |

## Final checks

`dotnet test Goose.Tests` and `dotnet test Goose.IntegrationTests` green; reward JSON shapes, refusal strings and claim-window behaviour match the design doc.
