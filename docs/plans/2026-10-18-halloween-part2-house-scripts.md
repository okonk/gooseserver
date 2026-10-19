# Halloween Event Part 2 (House and Event Scripts) Implementation Plan

**Goal:** Script the Halloween event's systems: event and tier flags with GM commands, the tier 1
XP bonus, candy, ambushes, the house and basement map scripts (costume check, basement gate,
pentagram, midnight clock and summon), the chainsaw event, the Party Costume, the quest scripts,
and the tier 3 town invasion.

**Architecture:** `.csx` scripts on the existing hooks plus the Part 1 engine hooks. All tunables and
all template/map ids live in one JSON file read at runtime, so the data plan fills in ids and the
test server can be tuned by editing the file. Shared logic is in `#load`ed files under
`Scripts/Global/Halloween/`. Boss behaviour (Weeper, stalker movement, Groundskeeper) is Part 3; this
part only spawns those NPCs with the right properties.

**Tech Stack:** .NET 10, Roslyn C# scripting (`.csx`, `#load`), System.Text.Json, xUnit (`Goose.Tests`).

Design: artifact "Haunted House Event" (https://claude.ai/artifact/PnXDddVa1NG5nFqmjL48vK).
Depends on: `docs/plans/2026-10-18-halloween-part1-engine.md` (all tasks). Part 3:
`docs/plans/2026-10-18-halloween-part3-boss-scripts.md`.

**Repo rule (AGENTS.md):** no new comments or doc strings unless the "why" is non-obvious.

---

## APIs verified

| API | Location |
|---|---|
| Global scripts: only top-level `Scripts/Global/*.csx` are compiled and run (`OnLoaded`) | `Goose/GameWorld.cs:898-918` |
| `#load` forms (sibling, subdir, parent), must precede usings; statics are per host compilation | `Goose.Tests/ScriptLoadDirectiveTests.cs:7-15` |
| Script default imports include `System.Text.Json`, `Goose`, `Goose.Events`, `Goose.Quests`, `Goose.CommunityGoals`, `Goose.Scripting` | `Goose/Scripting/Script.cs:44-52` |
| `/reloadscripts` swaps instances; existing globals' `OnLoaded` is not re-run | `Goose/Commands/ReloadScriptsCommand.cs`, `Goose/GameWorld.cs:910-918` |
| Script state kept across reload only in engine objects (`Map.ScriptStore`, `NPC.ScriptStore`, `NPC.Properties`, `Player.ScriptState` from Part 1 Task 7, world flags) | `Goose/Map.cs:57`, `Goose/NPC.cs:50,372` |
| Global script load runs **before** world flags load (`Global Scripts` step 475, `Community Goals` step 483) | `Goose/GameWorld.cs:475-483` |
| World flags: `world.CommunityGoals.GetFlag(name)` / `SetFlag(name, value, sourceGoalId)`; persisted to `world_flags` | `Goose/CommunityGoals/CommunityGoalHandler.cs:158-171,665-670` |
| Community goal `WorldFlag` reward sets flag to 1 | `Goose/CommunityGoals/CommunityRewardRegistry.cs:116-120` |
| `world.Commands.Register(key, privilege, section, help, Delegate)`; key with trailing space takes args | `Goose/Commands/CommandRegistry.cs:23-24`, `Goose/Data/Illutia/Scripts/Global/WanderingChest.csx:107` |
| Command binder: `CommandContext` first, then `string`, `int`, `string[]` rest | `Goose/Commands/CommandBinder.cs:16-72` |
| `ScriptTimerEvent.Create(action, period, world)` / `Reschedule` | `Goose/Events/ScriptTimerEvent.cs` |
| `world.TimeNow` (Stopwatch ticks), `world.TimerFrequency`, `world.Random` (read-only) | `Goose/GameWorld.cs:181-191` |
| `world.ExperienceModifier` set from `Settings.ExperienceModifier` at startup; with Part 1 Task 8's setting off nothing else writes it | `Goose/GameWorld.cs:196,242`, Part 1 Task 8 |
| XP formula multiplies by `world.ExperienceModifier` (+ per-player `AdditionalExperienceModifier`) | `Goose/Player.cs:1943-1955` |
| Item script `OnEquipEvent` / `OnUnequipEvent`, called after the whole equip/unequip finishes | Part 1 Task 6 |
| `P.Notice(NoticeType.Info, text)` (event feed) | `Goose/Packets.cs:43`, `Goose/NoticeType.cs` |
| `P.ServerMessage`, `world.Send`, `world.SendToMap`, `world.SendToAll` | `Goose/GameWorld.cs:799-887` |
| `Map.Players`, `Map.NPCs`, `Map.GetPlayersInRange`, `Map.GetCharacterAt`, `Map.IsTileBlocked`, `Map.ID`, `Map.Width/Height` | `Goose/Map.cs` |
| `Map.PlayerCanJoin` → script `CanPlayerJoin` refusal sent as `$7…`; GMs bypass | `Goose/Map.cs:739-757` |
| Login places a player on the saved map without `PlayerCanJoin`; `OnPlayerEntered` from `Map.AddPlayer` | `Goose/Map.cs:196-208`, `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx:34-41` |
| `OnPlayerMove` called after each player step | `Goose/Player.cs:1373-1380` |
| `Player.WarpTo(world, map, x, y)`; `Player.Facing`; `Player.Group.Players`; `Player.State`; `Player.IsGMInvisible`; `Player.HasPrivilege` | `Goose/Player.cs:1459`, `:147`, `:385`, `:605` |
| `Inventory.GetEquippedSlots()`, `GetInventorySlots()`, `AddItem(item, stack, world)` (stacks first), `HasItem` | `Goose/Inventory.cs:61-128,893` |
| `ItemSlot.CanStack` / `SameStackIdentity` (template, name, desc, look, params) | `Goose/ItemSlot.cs:24-60` |
| `ItemHandler.AddAndAssignId` registers every created item forever and fires `OnCreateEvent` | `Goose/ItemHandler.cs:265-280` |
| Ground items: `ItemTile`, `Map.PlaceItem`, `Map.AddItem`, `Map.RemoveItem`, `Map.GetTile` | `Goose/Map.cs:253-283,368-423,718` |
| `NPCHandler.SpawnNPC(world, mapId, x, y, template, shouldRespawn, properties)`; `GetNPCTemplate` | `Goose/NPCHandler.cs:249,373` |
| `NPC.AddAggro(player, value, world)`, `AddAttackEvent`, `AddMoveEvent`, `AggroTarget`, `Properties`, `State`, `Despawn` (Part 1) | `Goose/NPC.cs:911,1428,747` |
| `IQuestScript` requirement/reward members; `QuestRequirement.ScriptParams` / `QuestReward.ScriptParams` per row | `Goose/Scripting/IQuestScript.cs`, `Goose/Quests/QuestRequirement.cs:33-34`, `Goose/Quests/QuestReward.cs:41-42` |
| Shared script instance per file; parse `ScriptParams` per call | `Goose/Data/Illutia/Scripts/Quest/ClassReward.csx:14-17` |
| Item `OnCreateEvent` runs from `AddAndAssignId` (quest rewards included); `Item.GraphicEquipped` etc. are persisted per item | `Goose/ItemHandler.cs:272`, `Goose/Quests/QuestRewardGranter.cs:22-25`, `Goose/Item.cs:38-52` |
| Dimension clones: map/NPC/item ids at `base + 100000 * n`; dimension NPC clones are all level 50 | `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionConstants.csx:15`, `.../Npcs.csx:115` |
| Test precedent for copying a shipped script into a fixture data dir | `Goose.Tests/WanderingChestScriptTests.cs:177-184` |
| `TestWorldFixture`, `CapturingPlayer`, `RunCommand`; Part 1 Task 0 `AddNpcTemplate` / `SpawnNpc` | `TestSupport/TestWorldFixture.cs` |

## Cross-cutting rules

- **Config file:** `Goose/Data/Illutia/Scripts/Global/Halloween/halloween.json`, read through
  `HalloweenConfig.Get(world)` which caches by file modification time. Editing the file applies on
  the next call; no reload needed. A missing or invalid file logs one error per modification time
  and returns `HalloweenConfig.Disabled` (every feature off). Ids default to 0; a feature whose id
  is 0 is skipped, so the scripts can ship before the data plan.
- **Event switch:** world flag `halloween.active` (1 = on). Tiers: `halloween.tier1/2/3`. Every
  feature checks `Halloween.Active(world)`.
- **Base maps only:** house, basement and grove ids come from config. Scripts ignore any other map,
  including dimension clones (Part 1 forwards map hooks to clones). Candy ignores maps with
  `ID >= 100000`, because dimension NPC clones are all level 50 and would otherwise all drop candy.
- **Reload safety:** no script keeps state in instance fields or statics that must survive
  `/reloadscripts`. Per-player state is `Player.ScriptState` (`long` values, keys prefixed `hw.`);
  per-NPC state is `NPC.Properties`; counts (live ambush mobs, Phantoms, stalkers) are recomputed
  from `Map.NPCs` each time. Timers re-resolve the current script object through
  `map.Script.Object` / the global registry rather than capturing `this`.
- **Testability:** logic lives in public methods taking `(…, long now, Random rng)` so tests pass a
  fixed time and a deterministic `Random` subclass (`NextDouble`/`Next` are virtual). Hooks just
  forward `world.TimeNow` and `world.Random`.
- **Messages:** short-lived ones go to the event feed with `P.Notice(NoticeType.Info, "[Event] …")`;
  server-wide announcements use `world.SendToAll(P.ServerMessage("[Event] …"))`.

## Threading, persistence, publication

- **Thread:** all hooks, timers and commands run on the game thread.
- **Persistence:** world flags (existing table). No schema change. `Player.ScriptState` and
  `world.ExperienceModifier` are in-memory; the controller re-applies the XP bonus at startup from the
  tier 1 flag.
- **Publication:** spawned NPCs are published by `SpawnNPC` (registered, on map, packets sent) before
  the script tags them, so tags are passed as spawn `properties`, never set afterwards.

## File layout

```
Goose/Data/Illutia/Scripts/
  Global/Halloween.csx                 event controller (commands, flags, XP bonus at startup, eviction, chainsaw)
  CommunityGoal/HalloweenXpBonus.csx   tier 1 reward: applies the XP bonus
  Global/HalloweenCandy.csx            candy on OnNPCKilled
  Global/HalloweenInvasion.csx         tier 3 town invasion
  Global/Halloween/halloween.json      config
  Global/Halloween/HalloweenConfig.csx config DTO + cached loader
  Global/Halloween/HalloweenCommon.csx flags, map checks, costume check, player state, notices, candy grant, stalker spawn
  Global/Halloween/HalloweenAmbush.csx ambush rolls, outcomes, placement, cleanup
  Map/HauntedHouse.csx                 house map script
  Map/HauntedBasement.csx              basement map script
  Item/HalloweenCostume.csx            Party Costume and masks: random look on create, costume check on equip/unequip
  Quest/HalloweenWearing.csx           "wearing item" requirement
  Quest/HalloweenDoorman.csx           teleport-to-Foyer reward
Goose.Tests/Halloween/                 tests + HalloweenTestKit
```

---

### Task 1: Config, common helpers and the test kit

**Files:**
- Create: `Global/Halloween/halloween.json`, `Global/Halloween/HalloweenConfig.csx`, `Global/Halloween/HalloweenCommon.csx`
- Create: `Goose.Tests/Halloween/HalloweenTestKit.cs`, `Goose.Tests/Halloween/HalloweenCommonTests.cs`

**Config shape** (starting values from the design; ids 0 until the data plan):

```json
{
  "maps": { "house": 0, "basement": 0, "grove": 0,
            "groveExit": [38, 37], "foyerSpawn": [43, 53], "basementArrival": [0, 0] },
  "xpBonus": 0.5,
  "items": { "candy": 0, "partyCostume": 0, "masks": [0, 0, 0, 0, 0], "ritualCandle": 0 },
  "npcs": { "stalker": 0, "groundskeeper": 0, "wailingGhost": 0, "flickeringGhost": 0,
            "pinkPhantom": 0, "bloodyGhost": 0, "restlessBones": 0 },
  "candy": { "levelOnly": 50, "baseChance": 0.25, "baseAmount": [1, 1],
             "byTemplate": { "0": [1, 3] }, "stalkerSurvived": 10 },
  "rooms": [ { "name": "Foyer", "rect": [0, 0, 0, 0], "weight": 0, "table": "house" } ],
  "ambush": {
    "baseChance": 0.006, "basementWeight": 2.5, "defaultWeight": 1.0,
    "cooldownSeconds": 40, "basementCooldownSeconds": 30, "fakeCooldownSeconds": 15,
    "groupCooldownRadius": 8, "packRadius": 6, "packMax": 5,
    "despawnSeconds": 90, "maxLivePerMap": 15, "maxPhantomsHouse": 1, "maxPhantomsBasement": 2,
    "tables": {
      "house":    { "fake": 50, "ghosts": 45, "phantom": 3,  "stalker": 2 },
      "master":   { "fake": 40, "ghosts": 35, "phantom": 22, "stalker": 3 },
      "basement": { "fake": 20, "bones": 45, "bloody": 25, "phantom": 8, "stalker": 2 }
    },
    "packs": { "wailing": 2, "flickering": 3, "bones": [3, 4], "bloody": [1, 2] },
    "candy": { "ghost": [3, 5], "bones": [3, 5], "bloody": [3, 5], "phantom": [15, 25] },
    "fakeScares": { "house": ["The portrait's eyes follow you…", "A door slams upstairs."],
                    "basement": ["Something drips in the dark."],
                    "directional": ["Footsteps behind you."] }
  },
  "stalker": { "graceSeconds": 300, "maxLive": 3, "spawnBehind": 4,
               "chainsawMinMinutes": 20, "chainsawMaxMinutes": 40, "chainsawMinPlayers": 3,
               "chainsawDelaySeconds": 5, "costumeGraceSeconds": 10 },
  "pentagram": { "points": [[67, 4], [58, 10], [76, 10], [62, 21], [72, 21]], "centre": [67, 13] },
  "invasion": { "towns": [], "templates": [], "count": 20, "everyMinutes": 30, "lifetimeMinutes": 15 }
}
```

The pentagram points and centre are read from the shipped `Map10039.map` (the five candle-point
tiles use graphic 710511 on sheet 20415, layer 1; all six tiles are walkable). Room rectangles and
arrival tiles are map data and are filled in by the data plan. A tile outside every room uses `defaultWeight` and table `house`. A room with weight 0 never
rolls (Foyer). The Master Bedroom's room entry uses `"table": "master"`.

**HalloweenConfig.csx:** `public sealed class HalloweenConfig` mirroring the JSON (properties with
defaults), `public static HalloweenConfig Disabled`, and
`public static HalloweenConfig Get(GameWorld world)` reading
`world.Settings.DataPathAbsolute + "/Scripts/Global/Halloween/halloween.json"` with
`JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = Skip, AllowTrailingCommas = true }`.
Cache key: `File.GetLastWriteTimeUtc`. Each host compilation has its own cache (statics are per host),
which is fine: the cache is only a performance measure.

**HalloweenCommon.csx** (`public static class Halloween`), each a small pure helper:
- `Active(world)`, `Tier(world, n)` (flags), `SetActive(world, bool)`, `SetTier(world, n, bool)`.
- `IsHouse(map, cfg)`, `IsBasement(map, cfg)`, `IsEventMap(map, cfg)` - exact id match.
- `IsInCostume(player, cfg)`: any equipped item whose `TemplateID` is `partyCostume` or in `masks`.
- `RoomAt(cfg, x, y)` → room or null; `Weight`/`Table` resolution as above.
- `GetState(player, key)` / `SetState(player, key, value)` over `player.ScriptState` (missing = 0).
- `Feed(world, player, text)` → `P.Notice(NoticeType.Info, "[Event] " + text)`.
- `Ticks(world, seconds)`.
- `GiveCandy(world, player, amount)` and `SpawnStalker(world, target, mode, now)` are defined in
  Tasks 3 and 7 and live in this file.

**HalloweenTestKit (test-side):** builds a `TestWorldFixture`, copies the shipped
`Scripts/Global/Halloween/*.csx` plus the script under test into the fixture data dir (pattern of
`WanderingChestScriptTests.CompileScript`), writes a test `halloween.json` built from a C# object
with real ids from the fixture (house map 48, basement 39, grove 38, template ids from
`AddNpcTemplate`), and exposes `SetConfig(Action<JsonObject>)`, `Script<T> Compile<T>(folder, file)`,
`Call(scriptObject, method, args)` (reflection, as `WanderingChestScriptTests.Update`), and a
`FixedRandom : Random` with queued `NextDouble`/`Next` values.

**Step 1: Tests** (`HalloweenCommonTests`; a tiny host `.csx` written by the kit that `#load`s the
two files and exposes static wrappers)
- `Missing_config_disables_everything` and `Invalid_json_disables_and_logs_once`.
- `Edited_config_is_picked_up_without_reload` (rewrite the file with a later mtime).
- `IsInCostume_true_for_costume_or_any_mask_false_for_other_helm`.
- `RoomAt_returns_named_room_and_null_outside`.
- `Clone_map_is_not_an_event_map` (id 48 vs 100048) - adversarial for the dimension rule.
- `ScriptState_round_trips_through_GetState_SetState`.

**Step 2-4:** red → implement → green (`--filter FullyQualifiedName~Halloween.HalloweenCommonTests`).

**Step 5: Commit** `feat(halloween): config, common helpers and test kit`

---

### Task 2: Event controller, GM commands, XP bonus and end-of-event eviction

**Files:**
- Create: `Global/Halloween.csx`, `CommunityGoal/HalloweenXpBonus.csx`
- Test: `Goose.Tests/Halloween/HalloweenControllerTests.cs`

**Behaviour:**
- `OnLoaded`: registers commands, then starts a 30 s self-rescheduling `ScriptTimerEvent` that calls
  `Tick(world, now, rng)` on the **current** controller object (look it up through
  `world.ScriptHandler.GetGlobalScripts()` by file path, Part 1 Task 4, so a reload swaps the logic).
  The first tick runs 1 s after load, which is after world flags load (`GameWorld.cs:475-483`).
- Commands (privilege `AccessPrivilege.SpawnNPC`, section "Halloween"), one key `"/halloween "`
  with `(CommandContext ctx, string sub, string[] rest)`:
  - `on` / `off` → `SetActive`; `off` also evicts immediately (below).
  - `tier <1-3> <0|1>` → `SetTier`.
  - `status` → active, tiers, live ambush mobs/Phantoms/stalkers per event map, next chainsaw time.
  - `resetcd` → clears the caller's `hw.*` cooldown and grace keys.
  - `ambush <fake|ghosts|phantom|bones|bloody|stalker>` → `HalloweenAmbush.Trigger` on the caller
    (Task 4) ignoring cooldowns and chance.
  - `stalker` → `Halloween.SpawnStalker(caller, chase)` (Task 7).
  - `weeper` / `groundskeeper` → spawn the Part 3 boss next to the caller (`shouldRespawn: false`).
  Unknown sub-commands print usage.
- `Tick`:
  1. **Eviction:** when not active, warp every non-GM player on the house or basement map to the
     grove at `groveExit` with feed text "The party is over. The door closes behind you.".
  2. **Chainsaw event:** Task 7.
- **XP bonus:** set at tier 1 and again at startup, not maintained in between.
  `Halloween.ApplyXpBonus(world)` (in `HalloweenCommon.csx`) sets
  `world.ExperienceModifier = world.Settings.ExperienceModifier + cfg.xpBonus` (set from the settings
  base, not added, so applying it twice can't stack) and is called from two places:
  - the tier 1 community goal bracket's `Script` reward, `CommunityGoal/HalloweenXpBonus.csx` (a
    `BaseCommunityRewardScript`, params `{"script": "Scripts/CommunityGoal/HalloweenXpBonus.csx"}`;
    the registry loads it from `params.script`, `CommunityRewardRegistry.cs:180-181`), which also
    announces it server-wide;
  - the controller's `OnLoaded`, through a one-shot `ScriptTimerEvent` 1 s later (world flags load
    after global scripts, `GameWorld.cs:475-483`): if `Active && Tier(1)`, apply it. A restart
    therefore keeps the bonus without a GM.
  Requires Part 1 Task 8 (scaling off), or the player-count event would reset it within 10 minutes.
  The bonus is not removed when the event is switched off; it lasts until the next restart with the
  event off.

**Mutation impact:**
- Source of truth: world flags (`halloween.*`); `world.ExperienceModifier` (reward script only).
- Readers: `Player.AddExperience` formula (`Player.cs:1943-1955`); every Halloween script via
  `Active`/`Tier`.
- Propagation: `off` evicts in the command itself and again on every tick (covers players who log in
  inside the house). The XP reward writes `world.ExperienceModifier` once; `Player.AddExperience`
  reads it on the next kill.
- Invariants: the modifier is `base + bonus` after the grant, never bonus twice; a GM is never
  evicted.

**Step 1: Tests**
- `Xp_bonus_reward_sets_world_modifier_to_base_plus_bonus` (call `Give` with a
  `CommunityRewardContext` built like the community goal reward tests).
- `Granting_xp_bonus_twice_does_not_stack` (adversarial: fails if `Give` adds to the current value).
- `Startup_applies_bonus_when_tier1_flag_is_set` (set the flags, run `OnLoaded`, fire the one-shot
  timer action; modifier is base + bonus) and `Startup_leaves_base_without_tier1`.
- `Off_evicts_players_from_house_and_basement_to_grove_but_not_gms`.
- `Halloween_command_on_off_tier_status` via `fixture.RunCommand` and `CapturingPlayer.Sent`.
- `Tick_calls_current_script_object_after_swap` (replace `Script.Object` via the swap path and
  assert the new instance's tick ran).

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): event controller, commands and tier 1 XP bonus`

---

### Task 3: Candy

**Files:**
- Create: `Global/HalloweenCandy.csx`; add `Halloween.GiveCandy` to `HalloweenCommon.csx`
- Test: `Goose.Tests/Halloween/HalloweenCandyTests.cs`

**Rules** (`OnNPCKilled(npc, killer, credited, world)`, Part 1 Task 4):
1. Return unless `Active`, `cfg.items.candy != 0`, `npc.Map.ID < 100000`.
2. Amount range for this NPC, first match wins:
   - `npc.Properties` has `candyMin`/`candyMax` (ambush mobs, Phantoms, stalker survival never
     comes here) → that range, always awarded;
   - `cfg.candy.byTemplate[templateId]` → that range, always awarded (pumpkins, house mobs, Weeper,
     Groundskeeper, Pink Phantom from spawn data);
   - `npc.ShouldRespawn && npc.Level == cfg.candy.levelOnly` → `baseAmount` with `baseChance`;
   - otherwise none (adds, invasion mobs and other temporary NPCs without tags).
3. Roll separately for each credited player (`rng.Next(min, max + 1)`; the base chance per player).
4. `GiveCandy`.

**`GiveCandy(world, player, amount)`:**
- If an inventory slot holding candy can stack `amount` (`ItemSlot.CanStack`), add to it through
  `Inventory.AddItem` with a fresh **unregistered** `Item` (loaded from the template, `ItemID` 0): the
  stack path discards the object, so nothing is added to `ItemHandler`'s registry.
- Otherwise create the item with `AddAndAssignId` (it needs a real id in its own slot) and
  `AddItem`. If the bag is full, drop it at the player's feet (`ItemTile` with `Owner = player`,
  `Map.PlaceItem`, then stack onto an existing tile or `Map.AddItem`, as `PlayerDropItemEvent` does).
- Feed text: "+{amount} Candy".

Why the stack-first path: `AddAndAssignId` keeps every created item in `ItemHandler.items` forever
(`ItemHandler.cs:279`). Candy is granted on most kills for every credited player, so registering an
object per grant would grow that dictionary by hundreds of thousands of entries over the event.

**Step 1: Tests**
- `Level50_respawning_mob_awards_base_candy_with_chance_per_player` (FixedRandom: player A passes,
  B fails).
- `Non_level50_mob_awards_nothing`.
- `Template_table_overrides_level_rule`.
- `Tagged_temporary_npc_awards_its_range`; `Untagged_temporary_npc_awards_nothing` (adversarial for
  adds).
- `Dimension_map_kill_awards_nothing` (map 100038).
- `Inactive_event_awards_nothing`.
- `Candy_stacks_without_registering_a_new_item` (count `ItemHandler.GetItems()` before/after a
  stacking grant: unchanged; first grant: +1).
- `Full_bag_drops_candy_at_feet`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): candy from level 50 mobs and event mobs`

---

### Task 4: Ambush engine

**Files:**
- Create: `Global/Halloween/HalloweenAmbush.csx` (`public static class HalloweenAmbush`)
- Test: `Goose.Tests/Halloween/HalloweenAmbushTests.cs`

**API:**

```csharp
public static bool OnStep(GameWorld world, Map map, Player player, long now, Random rng)   // roll + apply; true if anything happened
public static void Trigger(GameWorld world, Map map, Player player, string outcome, long now, Random rng) // apply one outcome
public static int LiveAmbushMobs(Map map)
public static int LivePhantoms(Map map)
```

**`OnStep` sequence** (returns false at the first failing check):
1. `Active`, event map, player `State == Ready`, not GM-invisible.
2. Room weight > 0 (Foyer is 0); basement uses `basementWeight`.
3. Basement only: no live Groundskeeper on the map.
4. `now >= GetState(player, "hw.ambushReadyAt")`.
5. `rng.NextDouble() < baseChance * weight`.
6. Outcome = weighted pick from the room's table (`house`, `master`, `basement`).
7. `Trigger`.

**`Trigger(outcome)`:**
- `fake`: feed one line from the area's list or, 1 in 4, a `directional` line; a directional line
  stores `hw.hintDir` = the direction it names relative to the player (behind = opposite of
  `Facing`). Cooldown `fakeCooldownSeconds`, **this player only**.
- `ghosts`: 2/3 Wailing (pack `wailing`) or 1/3 Flickering (pack `flickering`); `bones`, `bloody`:
  ranges from `packs`; `phantom`: one Pink Phantom.
  - Pack size + 1 for each other Ready player within `packRadius`, capped at `packMax` (Phantom is
    always 1).
  - Caps: if `LiveAmbushMobs(map) + size > maxLivePerMap`, shrink to fit; if nothing fits, the
    outcome becomes `fake`. A Phantom past `maxPhantoms*` becomes `ghosts` (basement: `bones`).
  - Placement: tiles 2-4 ahead in `Facing` (or toward `hw.hintDir` when set; then clear it), inside
    the same room rect, not `Map.IsTileBlocked`. Fall back to the ring of tiles at distance 1-3
    around the player. Mobs that find no tile are dropped from the pack.
  - Spawn each with `SpawnNPC(..., shouldRespawn: false, properties)` where properties =
    `{"ambush": 1, "candyMin": a, "candyMax": b, "spawnedAt": now}`; then `AddAggro(player, 1)`,
    `AddAttackEvent`, `AddMoveEvent` so they go for the triggering player.
  - Despawn timer: `ScriptTimerEvent` after `despawnSeconds`: if the NPC is alive and its
    `AggroTarget` is null or out of range → `Despawn`; else reschedule 30 s.
  - Feed: "Ghosts pour out of the walls!" / "Bones rise from the piles!" / "A pink glow fills the
    room…".
- `stalker`: `Halloween.SpawnStalker(world, player, "chase", now)` (Task 7). If it refuses (cap or
  grace), the outcome becomes `fake`.
- Real outcomes set `hw.ambushReadyAt = now + cooldown` (house or basement value) on the triggering
  player **and every Ready player within `groupCooldownRadius`** on the same map.

**Mutation impact:**
- Source of truth: `Map.NPCs` (+ NPC registry), `Player.ScriptState` cooldown keys.
- Readers: caps (`LiveAmbushMobs`/`LivePhantoms` count `Map.NPCs` where `State == Alive` and
  `Properties` has `ambush`), candy (Task 3 reads `candyMin/Max`), status command.
- Invariants: live ambush mobs per map never exceed the cap; a group shares one cooldown; fake
  scares never set the group cooldown; Foyer never rolls; ambushes never roll in the basement while
  the Groundskeeper is alive.

**Step 1: Tests** (house map 48 built with a Foyer rect, a Library rect and a Master Bedroom rect;
FixedRandom)
- `Foyer_never_rolls` and `Gm_invisible_never_rolls`.
- `Roll_below_chance_triggers_and_sets_group_cooldown` (second player within 8 tiles also on
  cooldown; a third at 12 tiles is not).
- `Fake_scare_sets_only_own_short_cooldown` (adversarial: fails if fake uses the group path).
- `Pack_grows_with_nearby_players_and_caps_at_five`.
- `Mobs_spawn_ahead_of_the_player_in_the_same_room` (facing Up: every mob y < player y, inside the
  room rect).
- `Live_cap_shrinks_pack_and_turns_full_cap_into_fake`.
- `Second_phantom_becomes_ghosts_in_house`.
- `Basement_never_rolls_while_groundskeeper_alive`.
- `Unfought_ambush_mob_despawns_after_timeout` (drive the timer action directly; fought mob is
  rescheduled instead).
- `Ambush_mob_is_tagged_for_candy` (properties present at spawn).

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): ambush engine`

---

### Task 5: House map script

**Files:**
- Create: `Map/HauntedHouse.csx` (`#load "../Global/Halloween/HalloweenCommon.csx"`, `…Ambush.csx`, `…Config.csx`)
- Test: `Goose.Tests/Halloween/HauntedHouseMapTests.cs`

**Hooks:**
- `CanPlayerJoin`: not the configured base house map (a dimension clone) → "The house only exists in
  this world."; event inactive → "The house is dark and the door is locked."; otherwise null.
  The Doorman's teleport (Task 8) uses `WarpTo`, so this gate only applies to other routes in.
- `OnPlayerEntered`: inactive → evict to the grove (non-GM); otherwise `Halloween.CheckCostume`.
- `OnPlayerMove`: `HalloweenAmbush.OnStep(world, map, player, world.TimeNow, world.Random)`.
- `OnPlayerLeft`: clear `hw.costumeDeadline`.
- Equipment changes reach the check through the costume and mask item script (Task 8), which calls
  the same `Halloween.CheckCostume` from `OnEquipEvent`/`OnUnequipEvent`. Taking off some other item
  can't change costume state, so only those items need the hook.

**Costume check** (`Halloween.CheckCostume(world, player, now)` in `HalloweenCommon.csx`; returns
immediately unless the player is on the house or basement map and the event is active):
- In costume → clear `hw.costumeDeadline`; if a costume-hunt stalker is out for this player, despawn
  it with no candy (Part 3 stalker reads `hw.huntCancelled`; here set that key and call
  `Halloween.DespawnStalkerFor(player)`, defined in Task 7).
- Not in costume, no deadline set → feed "You feel exposed. Put your costume back on.", set
  `hw.costumeDeadline = now + costumeGraceSeconds`, schedule a `ScriptTimerEvent` for the grace
  period that re-checks: still on this map, still out of costume, deadline unchanged →
  `SpawnStalker(player, "hunt", now)` (bypasses grace and the live cap).
- GMs are exempt.

**Step 1: Tests**
- `Clone_house_refuses_entry` and `Inactive_house_refuses_entry`.
- `Removing_costume_warns_then_spawns_hunt_stalker_after_grace`.
- `Putting_costume_back_on_within_grace_cancels_the_hunt` (adversarial: timer must re-check state).
- `Swapping_costume_for_a_mask_does_not_warn` (both item hooks see the final loadout, Part 1 Task 6).
- `Entering_without_costume_starts_the_grace` (login inside the house).
- `Player_step_rolls_ambush` (FixedRandom forces a ghost outcome).

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): house map script`

---

### Task 6: Basement map script (gate, ambush table, pentagram, clock, summon)

**Files:**
- Create: `Map/HauntedBasement.csx`
- Test: `Goose.Tests/Halloween/HauntedBasementMapTests.cs`

**Hooks:**
- `CanPlayerJoin`: clone → refuse; inactive → refuse; `!Tier(2)` → "The basement door won't budge."
  (the stairs are a normal warp tile in the map data, so this is the gate the design describes).
- `OnPlayerEntered`, `OnPlayerMove`: as the house (ambushes use the
  `basement` table automatically through `IsBasement`).
- `CanPlayerDropItem(item)`: item is a Ritual Candle and the player stands on a pentagram point that
  already holds one → "A candle already burns here."; otherwise null.
- `OnPlayerDroppedItem(tile)`: a Ritual Candle tile on a point → feed to everyone on the map "A
  candle flickers to life." (count lit points in the message, "3 of 5").
- `CanPlayerPickupItem(tile)`: a Ritual Candle on a point → "The candle is fixed to the floor.".
- `OnLoad`: start a 60 s self-rescheduling timer that calls the current script object's
  `ClockTick(world, map, DateTime.Now)`.

**Pentagram state is derived, never cached:** `LitPoints(map)` checks `Map.GetTile(x, y)` for an
`ItemTile` whose item is a candle at each configured point. The 3-hour ground sweep and restarts
therefore can't desynchronise it.

**`ClockTick(world, map, DateTime local)`** (public, takes the time for tests):
- Minute 45, once per hour (key `hw.clockWarnedHour` stored on `map.ScriptStore` as a boxed long;
  losing it on reload only risks a repeated warning) → feed to the map "Somewhere in the house, a
  clock begins to tick louder…".
- Minute 0, once per hour:
  - Groundskeeper alive → nothing.
  - All five points lit → remove the five candle tiles (`Map.RemoveItem`), send "The clock strikes
    midnight. The candles gutter out." to the map, spawn the Groundskeeper at `pentagram.centre`
    (`shouldRespawn: false`), and despawn every live ambush mob on the map.
  - Otherwise → "Nothing answers." Partial candles stay.

**Step 1: Tests**
- `Basement_refuses_without_tier2`.
- `Candle_on_point_is_counted_and_cannot_be_picked_up`.
- `Second_candle_on_lit_point_is_refused`.
- `Swept_candle_is_no_longer_counted` (remove the tile directly; `LitPoints` drops - adversarial
  against caching).
- `Midnight_with_five_candles_consumes_them_and_spawns_groundskeeper`.
- `Midnight_with_four_candles_answers_nothing_and_keeps_them`.
- `No_summon_while_groundskeeper_alive`.
- `Quarter_to_warning_once_per_hour`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): basement map script with pentagram and midnight summon`

---

### Task 7: Stalker spawning and the chainsaw event

Part 3 owns how the stalker moves and what happens when he catches someone. This task owns when he
appears and the limits.

**Files:**
- Modify: `Global/Halloween/HalloweenCommon.csx` (`SpawnStalker`, `DespawnStalkerFor`, `LiveStalkers`)
- Modify: `Global/Halloween.csx` (chainsaw in `Tick`)
- Test: `Goose.Tests/Halloween/HalloweenStalkerSpawnTests.cs`

**`SpawnStalker(world, target, mode, now)` → bool:**
- `mode` is `"chase"` or `"hunt"`. Refuse when the stalker template id is 0, the target is not on
  the house or basement map, the target already has a live stalker, or the basement Groundskeeper is
  alive and the target is in the basement.
- `chase` only: refuse when `now < hw.stalkerGraceUntil` or `LiveStalkers(world) >= maxLive`.
- Tile: `spawnBehind` tiles behind the target (opposite `Facing`), walking back toward the target
  until a free, same-room tile is found; fall back to any free tile at distance 2-4.
- `SpawnNPC(..., shouldRespawn: false, {"stalkerTarget": target.PlayerID, "stalkerMode": mode,
  "spawnedAt": now})`; set `hw.stalkerNpc` = NPC `LoginID` on the target.
- Feed to the target "A chainsaw revs behind you!".
- `DespawnStalkerFor(player)`: despawns the NPC in `hw.stalkerNpc` if alive and clears the key.
- `LiveStalkers(world)`: alive NPCs with `stalkerTarget` on the house and basement maps.

Part 3 sets `hw.stalkerGraceUntil` when a chase ends; grace is not set here.

**Chainsaw (controller `Tick`):** keep `nextChainsawAt` in the controller (a reload re-rolls it,
which is harmless). When due: if `Active`, the Groundskeeper is not alive, and at least
`chainsawMinPlayers` Ready players are in the house, send "A chainsaw revs somewhere in the house…"
to everyone on the house map, then after `chainsawDelaySeconds` pick a random costumed, Ready,
non-GM player on the house map outside the Foyer and call `SpawnStalker(chase)`. Roll the next time
uniformly in `[chainsawMinMinutes, chainsawMaxMinutes]`.

**Step 1: Tests**
- `Spawns_behind_target_with_properties`.
- `Refuses_during_grace_and_over_cap_for_chase` and `Hunt_ignores_grace_and_cap` (adversarial).
- `One_stalker_per_target`.
- `Chainsaw_needs_three_players_and_skips_foyer_and_uncostumed`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): stalker spawning and chainsaw event`

---

### Task 8: Costume and mask item script, "wearing" requirement and the Doorman teleport

**Files:**
- Create: `Item/HalloweenCostume.csx`, `Quest/HalloweenWearing.csx`, `Quest/HalloweenDoorman.csx`
- Test: `Goose.Tests/Halloween/HalloweenItemQuestScriptTests.cs`

**HalloweenCostume** (set on the Party Costume and all five masks):
- `OnEquipEvent` / `OnUnequipEvent` → `Halloween.CheckCostume(world, player, world.TimeNow)`.
- `OnCreateEvent`, Party Costume only (masks have empty `ScriptParams` and keep their template look):
  `item.ScriptParams` is a JSON array of looks
`[{"graphic": 19, "r": 0, "g": 0, "b": 0, "a": 0}, …]` (one per guest costume; values in the data
plan). Pick one with `world.Random` and set `GraphicEquipped` and the colour fields. Those fields are
saved with the item (`Item.cs:38-52`), so the look is fixed for that item. Invalid params → log and
keep the template look. The `hideOtherSlots` property comes from the template (already supported).

**HalloweenWearing (requirement):** `IsMet` → the player has an equipped item whose template id is in
`requirement.ScriptParams` (`{"items": [ids]}`; the Party Costume quests list the costume id).
`GetProgressText` → "Wearing your costume" / "Put on your costume". `OnTakeRequirement` → nothing
(the costume is not taken).

**HalloweenDoorman (reward):** `CanComplete` → refuse when the event is inactive ("The door is
locked tonight."); `GiveReward` → `player.WarpTo(world, houseMap, foyerSpawn)` and feed "The door locks
behind you." `GetRequiredInventorySpace` → 0.

**Step 1: Tests**
- `Unequipping_costume_in_house_starts_grace_and_reequipping_cancels_it`.
- `Mask_without_params_keeps_template_look`.
- `Costume_gets_one_of_the_configured_looks_and_keeps_it` (two creations with a seeded random get
  looks from the list; JSON round trip keeps `GraphicEquipped`).
- `Wearing_requirement_met_only_when_equipped` (in inventory but not equipped → not met).
- `Doorman_reward_warps_to_foyer` and `Doorman_refuses_when_inactive`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): party costume, wearing requirement and doorman teleport`

---

### Task 9: Town invasion (tier 3)

**Files:**
- Create: `Global/HalloweenInvasion.csx`
- Test: `Goose.Tests/Halloween/HalloweenInvasionTests.cs`

Follows `EasterEvent.csx`, using `NPC.Despawn` (Part 1) instead of the hand-written removal.

- `OnLoaded` starts a 60 s timer calling the current object's `Tick(world, now, rng)`.
- When `Active && Tier(3)` and `now >= nextAt`: despawn the previous wave's survivors, pick a town
  from `invasion.towns` (map id + rect), spawn `count` NPCs from `invasion.templates` on random free
  tiles in the rect with `shouldRespawn: false` and `{"invasion": 1}` (no candy tag, so no candy -
  design rule), announce server-wide "[Event] The party has spilled into {town}!", and set
  `nextAt = now + everyMinutes`. Survivors despawn after `lifetimeMinutes`.
- Wave membership is found from `Map.NPCs` with the `invasion` property, so a reload loses nothing.

**Step 1: Tests**
- `No_wave_before_tier3`; `Wave_spawns_count_in_rect_and_announces`.
- `Next_wave_despawns_survivors`; `Survivors_despawn_after_lifetime`.

**Step 2-4:** red → implement → green.

**Step 5: Commit** `feat(halloween): tier 3 town invasion`

---

## Final checks

- `dotnet test Goose.sln --no-restore`.
- `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore` (shipped scripts
  still compile; the new globals compile in the real data dir with the shipped `halloween.json`).
- Manual smoke on a test server after the data plan: `/halloween on`, walk the Library, `/halloween
  status`, `/halloween ambush ghosts`, take the costume off, light the pentagram with GM-spawned
  candles and wait for :00 (or call `ClockTick` via a temporary `/halloween clock` if needed).

## Red-team notes

- **Global script ordering:** world flags load after global scripts (`GameWorld.cs:475-483`), so
  nothing reads flags in `OnLoaded`; the first tick is 1 s later.
- **Reload:** timers call through the current script object. Map `ScriptStore` contents typed by a
  script class are lost on reload (new compilation, new type), so only harmless data goes there.
- **Dimension clones:** every hook checks the exact base map id; clones are refused at the gate.
- **Volume:** ambush mobs are temporary NPCs; Part 1 Task 1 releases them, and Task 4's cap bounds the
  live count. Candy avoids registering an item per grant.
- **GM safety:** GMs are never evicted, never warned about costumes, and never chosen by the
  chainsaw event; they can still trigger everything by command.
