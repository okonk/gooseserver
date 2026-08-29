# Swallowed Exception Logging Implementation Plan

**Goal:** Replace all try/catch blocks that silently swallow exceptions (TODO-log placeholders, empty `catch (Exception e) { }` bodies, and all `// log bad ...` comment-only placeholders on trusted-data/invariant paths) with real NLog logging that includes both human-readable names and unique IDs, fix two genuine bugs hiding in swallowed paths (consumable consumed on script exception; GM told a config change succeeded when it failed), and clean up the deliberate swallows.

**Architecture:** The codebase already has per-class NLog loggers (`private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();`) and an established `log.Error(e, "... {0} ...", args)` convention (e.g. `Goose/EventHandler.cs:387`, `Goose/NPC.cs:390`). This plan extends that convention to the ~50 unlogged sites. Log messages always pair a unique ID with the name (IDs are stable; names are not): NPC → `Name (NPCTemplateID)`, Player → `Name (LoginID)`, Item → `Name (TemplateID)`, SpellEffect → `Name (ID)`, Map → `Name (ID)`.

**Tech Stack:** C# / .NET 10, NLog 6.1.4, xUnit.

## Decisions (confirmed)

- `Inventory.UseConsumable` script exception → **fail closed**: set `remove = false` so the item is NOT consumed.
- `Map.OnLoadTile` (per-tile hook, up to W×H×4–5 calls per map load) → **keep per-tile logging** (accept the spam; include `x, y, layer` so it's actionable).
- Log format: name + ID, e.g. `"NPC OnAttackedEvent {0} ({1}) Exception", npc.Name, npc.NPCTemplateID`.

## APIs verified

- NLog logger field pattern: `Goose/EventHandler.cs:15`, `Goose/GameWorld.cs:26` (`private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();`)
- `log.Error(Exception, string, params object[])` usage: `Goose/NPC.cs:390`; `log.Warn(string, params object[])`: `Goose/Player.cs:888`; `log.Error(string, params object[])` (exception last): `Goose/Player.cs:882`
- Files that **already have** a `log` field: `Goose/NPC.cs`, `Goose/Player.cs`, `Goose/Pet.cs`, `Goose/Map.cs`, `Goose/ItemHandler.cs`, `Goose/Inventory.cs`, `Goose/NPCHandler.cs`, `Goose/GameWorld.cs`, `Goose/GameServer.cs`, `Goose/Events/LoginEvent.cs`, `Goose/Events/UpdateSqlCommandEvent.cs`
- Files that **need** the logger field added: `Goose/Events/ChatEvent.cs`, `Goose/Events/BuffTickEvent.cs`, `Goose/Events/PlayerAttackEvent.cs`, `Goose/SpellHandler.cs`, `Goose/Events/SetConfigCommandEvent.cs`
- ID/name fields: `NPC.NPCTemplateID` (`Goose/NPC.cs:145`), `NPC.Name` (`Goose/NPC.cs:153`), `Player.LoginID` (`Goose/Player.cs:126`), `Player.Name` (`Goose/Player.cs:106`), `Item.TemplateID` (`Goose/Item.cs:29`), `SpellEffect.ID` (`Goose/SpellEffect.cs:112`), `SpellEffect.Name` (`Goose/SpellEffect.cs:113`), `Map.Name` (`Goose/Map.cs:28`), `Pet.Owner` (`Goose/Pet.cs:60`)
- `Inventory.UseConsumable(Item, GameWorld)`: `Goose/Inventory.cs:400`; `RemoveItem` call at `Goose/Inventory.cs:439`
- `Player.AddBuff(Buff, GameWorld)`: `Goose/Player.cs:2205,2214`; `NPC.AddBuff`: `Goose/NPC.cs:1512`
- `NPCHandler.LoadNPCTemplates(GameWorld)`: `Goose/NPCHandler.cs:42` (allies parse at :145-166); `internal static ResolveQuests(int, string, QuestHandler)` pattern declared at `Goose/NPCHandler.cs:24` (called at :127), tested without a DB in `Goose.Tests/NPCTemplateRegistrationTests.cs:50`; `InternalsVisibleTo("Goose.Tests")` is set (`Goose/Goose.csproj:20-24`)
- `Player.LoadFromAutoCreate(string, string, GameWorld)`: `Goose/Player.cs:596`; starting-items loop at :686-713; `world.Settings.StartingItems` is a space-separated string
- `SetConfigCommandEvent.Ready`: `Goose/Events/SetConfigCommandEvent.cs:8-58`; success broadcast at :51; string branch :31-34; unknown-property error message pattern at :24
- Script-hook test pattern: `ScriptStub.For<IXxxScript>(instance)` wraps an in-memory `BaseXxxScript` subclass with no disk/Roslyn — `TestSupport/ScriptStub.cs:10`, used at `Goose.Tests/ItemScriptHookTests.cs:64`. Use this for ALL hook tests (the older `.csx`-via-`ScriptHandler` pattern in `Goose.Tests/MapPlayerCanJoinHookTests.cs` is not needed).
- Fixture: `TestSupport/TestWorldFixture.cs` — `PlayerOn` (:64), `CommandPlayerOn` (:82), `AddBaseItemTemplate` (:126), `SeedClass` (:141), `CompileSpellEffectScript` (:35), `RunCommand` (:105)
- `PlayerHandler.AddPlayer(Player, GameWorld)`: `Goose/PlayerHandler.cs:51`; `PlayerHandler.Players`: `Goose/PlayerHandler.cs:160`; `GameWorld.SendToAll` iterates `PlayerHandler.Players` (`Goose/GameWorld.cs:641-648`) — a fixture player only receives broadcasts if registered via `AddPlayer`
- NLog test capture: `NLog.Targets.MemoryTarget` + `LogManager.Configuration` (NLog 6.1.4, referenced by `Goose.Tests.csproj` transitively). **Parallelism constraint:** `Goose.Tests` has no `xunit.runner.json` and no `[Collection]` attributes, so test classes run in parallel, and `CapturingLog` swaps the *global* NLog config. ALL tests using `CapturingLog` must live in ONE test class (`ScriptHookLoggingTests`) so two captures never race.

---

### Task 1: Character & map lifecycle hook logging (the 7 `// TODO: need a logging system` sites)

**Files:**
- Modify: `Goose/Events/ChatEvent.cs:66-72` (add logger field)
- Modify: `Goose/NPC.cs:1073-1078`, `Goose/NPC.cs:1118-1126`
- Modify: `Goose/Player.cs:1276-1281`
- Modify: `Goose/Pet.cs:670-675`
- Modify: `Goose/Map.cs:190-195`, `Goose/Map.cs:208-213`
- Create: `Goose.Tests/CapturingLog.cs`
- Create: `Goose.Tests/ScriptHookLoggingTests.cs`

**Step 1: Create the shared log-capture test helper**

`Goose.Tests/CapturingLog.cs` — swaps the global NLog config for a `MemoryTarget` and restores the previous config on dispose. The swap is global and test classes run in parallel (no `xunit.runner.json`), so running the capture while ANY unrelated test class emits logs would redirect their output into the memory target — and, worse, two captures could race. Serialize it properly: declare a collection in the test file and put `ScriptHookLoggingTests` in it:

```csharp
[CollectionDefinition("NLog", DisableParallelization = true)]
public class NLogCollection { }

[Collection("NLog")]
public class ScriptHookLoggingTests : IClassFixture<TestWorldFixture>, IDisposable
```

`DisableParallelization = true` keeps the whole collection from running concurrently with any other collection/class, so the global config is only ever swapped by one test at a time. All capture tests in this plan live in this one class:

```csharp
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Goose.Tests;

public sealed class CapturingLog : IDisposable
{
    public MemoryTarget Target { get; } = new();
    private readonly LoggingConfiguration? previous;

    public CapturingLog()
    {
        this.previous = LogManager.Configuration;
        var config = new LoggingConfiguration();
        config.AddTarget("mem", this.Target);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, this.Target);
        LogManager.Configuration = config;
    }

    public IEnumerable<string> Messages => this.Target.LogEvents.Select(e => e.FormattedMessage);

    public void Dispose() => LogManager.Configuration = this.previous;
}
```

**Step 2: Write the failing tests**

`Goose.Tests/ScriptHookLoggingTests.cs`, using `TestWorldFixture` for world/map/player construction and `ScriptStub` for scripts:

1. `Map_AddPlayer_WithThrowingOnPlayerEntered_LogsMapAndPlayerIds` — map with a throwing `OnPlayerEntered` script attached via `ScriptStub.For<IMapScript>(new ThrowingEnteredScript())` (subclass `BaseMapScript`, `throw` in the override); set `player.LoginID = 42`; call `map.AddPlayer(player, world)`; assert no exception escapes AND a log message contains the map name, the map ID, the player name, and `42`.
2. `Npc_Attacked_WithThrowingOnAttackedEvent_LogsNpcNameAndTemplateId` — create the NPC via `world.NPCHandler.SpawnNPC(world, mapId, x, y, template, false)` exactly as existing tests do (`Goose.Tests/BuffNullGuardTests.cs:35`, `Goose.Tests/InvisibilityBreakTests.cs:96` — do NOT hand-construct an `NPC`; it needs a map, initialized stats, and aggro state). The template must have `NPCTemplateID = 100162`, a valid class/level, `CanBeKilled = true` (defaults to false, `Goose/NPCTemplate.cs:150`; an unkillable NPC returns before HP reduction, `Goose/NPC.cs:1087-1095`), and `BaseStats = new AttributeSet { HP = 100 }` — positive HP so the 10-damage test exercises ordinary post-hook damage, not the death path. Attach the throwing `OnAttackedEvent` script via `ScriptStub.For<INPCScript>(...)` after spawn. Call `npc.Attacked(player, 10, world)`; assert `npc.CurrentHP` was reduced (processing continued past the hook) AND the log contains the NPC name and `100162`.
3. `Npc_Killed_WithThrowingOnKilledEvent_MapHookStillRuns` — spawned killable NPC with a script that throws in `OnKilledEvent`, on a map whose `IMapScript` stub records `OnNPCKilledEvent` invocations; kill the NPC (damage ≥ HP via `Attacked`); assert the map hook WAS invoked (today a throwing NPC hook skips it — this test proves the split) AND the log contains `NPC OnKilledEvent` with the NPC name and template ID.

All three tests use `using var log = new CapturingLog();`.

**Step 3: Run to verify red**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ScriptHookLoggingTests`
Expected: FAIL — containment assertions pass, log-content assertions fail (nothing is logged today).

**Step 4: Implement**

Replace each `// TODO: need a logging system` body with `log.Error(e, ...)`, keeping the containing `try/catch` (containment is load-bearing — see `Goose/EventHandler.cs:388-393`):

| Site | Message |
|---|---|
| `Goose/Events/ChatEvent.cs:70` | `log.Error(e, "NPC OnPlayerChatEvent {0} ({1}) Exception", npc.Name, npc.NPCTemplateID)` |
| `Goose/NPC.cs:1077` | `log.Error(e, "NPC OnAttackedEvent {0} ({1}) Exception", this.Name, this.NPCTemplateID)` |
| `Goose/NPC.cs:1125` | **Split the combined try into two** (`Goose/NPC.cs:1118-1126` currently wraps both hooks, so a throwing NPC hook prevents the map hook from running and the log can't attribute the failure): `try { this.Script?.Object.OnKilledEvent(this, character, world); } catch (Exception e) { log.Error(e, "NPC OnKilledEvent {0} ({1}) Exception", this.Name, this.NPCTemplateID); }` then `try { this.Map.Script?.Object.OnNPCKilledEvent(this.Map, this, character, world); } catch (Exception e) { log.Error(e, "Map OnNPCKilledEvent {0} ({1}) npc {2} ({3}) Exception", this.Map.Name, this.Map.ID, this.Name, this.NPCTemplateID); }` |
| `Goose/Player.cs:1280` | `log.Error(e, "Map OnPlayerMove {0} ({1}) player {2} ({3}) Exception", this.Map.Name, this.Map.ID, this.Name, this.LoginID)` |
| `Goose/Pet.cs:674` | `log.Error(e, "Map OnPetMove {0} ({1}) owner {2} ({3}) Exception", this.Map.Name, this.Map.ID, this.Owner.Name, this.Owner.LoginID)` |
| `Goose/Map.cs:194` | `log.Error(e, "Map OnPlayerEntered {0} ({1}) player {2} ({3}) Exception", this.Name, this.ID, player.Name, player.LoginID)` |
| `Goose/Map.cs:212` | `log.Error(e, "Map OnPlayerLeft {0} ({1}) player {2} ({3}) Exception", this.Name, this.ID, player.Name, player.LoginID)` |

Add `private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();` to `ChatEvent.cs` only (the others already have one).

Note: the `NPC.cs:1125` split is a behavior change — today a throwing `OnKilledEvent` skips `OnNPCKilledEvent`; after the split both hooks always run. The rest of this task is log-only.

**Step 5: Green + commit**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ScriptHookLoggingTests` — expected PASS. Then `dotnet build Goose.sln` (clean build).

```bash
git add Goose/Events/ChatEvent.cs Goose/NPC.cs Goose/Player.cs Goose/Pet.cs Goose/Map.cs Goose.Tests/CapturingLog.cs Goose.Tests/ScriptHookLoggingTests.cs
git commit -m "fix: log script-hook exceptions in character/map lifecycle paths"
```

| Invariant | Proved by |
|---|---|
| Throwing `OnPlayerEntered` cannot escape `AddPlayer`, and is logged with map + player identity | `Map_AddPlayer_WithThrowingOnPlayerEntered_LogsMapAndPlayerIds` |
| Throwing `OnAttackedEvent` cannot skip HP reduction, and is logged with NPC name + template ID | `Npc_Attacked_WithThrowingOnAttackedEvent_LogsNpcNameAndTemplateId` |
| Remaining 5 sites log on throw | Log-only one-line additions beside existing containment; no behavior change beyond log emission (C# emits no warning for unused catch variables, so there is nothing to verify at compile time — the full suite is the only gate) |
| Throwing `OnKilledEvent` no longer skips `OnNPCKilledEvent` | `Npc_Killed_WithThrowingOnKilledEvent_MapHookStillRuns` |

---

### Task 2: Buff/item/tile hook logging + fail-closed consumables (12 empty `catch (Exception e) { }` sites)

**Files:**
- Modify: `Goose/Events/BuffTickEvent.cs:25-28` (add logger field)
- Modify: `Goose/Events/PlayerAttackEvent.cs:54-57` (add logger field)
- Modify: `Goose/NPC.cs:1574-1577`, `Goose/NPC.cs:1686-1689`
- Modify: `Goose/Player.cs:2293-2296`, `Goose/Player.cs:2483-2486`
- Modify: `Goose/ItemHandler.cs:245-248`
- Modify: `Goose/Inventory.cs:430-434`
- Modify: `Goose/Map.cs:464-467`, `Goose/Map.cs:501-504`, `Goose/Map.cs:523-526`, `Goose/Map.cs:581-584`
- Test: `Goose.Tests/ScriptHookLoggingTests.cs` (extend)

**Mutation impact (Inventory fail-closed):**
- Source of truth changed: inventory contents, mutated by `RemoveItem` at `Goose/Inventory.cs:438` (called from `UseConsumable`, `Goose/Inventory.cs:399`).
- Important readers: `RemoveItem` sends the client update packet itself; inventory is persisted on logout (blob save; the corrupt-blob *load* handling is at `Goose/Inventory.cs:1014`); gold/stack counts derive from the slots.
- Behavior change: previously a throwing `OnUseConsumableEvent` left `remove == true` and **consumed the item**; now the item is kept. No other code path reads the exception, so no propagation beyond `RemoveItem` being skipped.
- Accepted edge: the spell-effect roll at `Goose/Inventory.cs:424-427` casts *before* the script hook, so on the (rare) path where the effect rolled AND the script threw, the player keeps the item and gets the effect. Accepted — a broken script shouldn't cost the player's item; double-dipping requires both a broken script and a successful random roll.
- Invariants: script returning `true` (or no script) still consumes exactly 1; script returning `false` still keeps the item (existing behavior, unchanged); failure leaves inventory unchanged.

**Step 1: Write the failing tests**

Extend `Goose.Tests/ScriptHookLoggingTests.cs` (`ScriptStub` scripts + `CapturingLog` from Task 1; player/item construction via `TestWorldFixture`, `TestSupport/TestWorldFixture.cs:126` `AddBaseItemTemplate`):

1. `UseConsumable_ThrowingScript_DoesNotConsumeItem` (adversarial — **fails today** because the item is removed): item with throwing `OnUseConsumableEvent` script via `ScriptStub.For<IItemScript>(...)`, and `item.Template.SpellEffectID = 0; item.Template.SpellEffect = null;` (deterministic — no effect roll; `Item.SpellEffect` is getter-only and forwards to the template, `Goose/Item.cs:120`, so it must be set on the template); call `player.Inventory.UseConsumable(item, world)`; assert the item is still in the inventory (count unchanged) and a log message contains the item name and `TemplateID`.
2. `UseConsumable_ScriptReturningTrue_StillConsumes` (regression guard for the fail-closed change): script returns `true`; assert count decreased.
3. `UseConsumable_ScriptReturningFalse_KeepsItem` (existing behavior guard): script returns `false`; assert count unchanged.
4. `Player_AddBuff_ThrowingOnBuffAdded_BuffAppliedAndLogged`: the player MUST be at `State = Ready` — use `fixture.CommandPlayerOn(map, x, y)` (sets `State = Ready`, `TestSupport/TestWorldFixture.cs:82-95`), NOT `PlayerOn` (leaves State at its default, and `AddBuff` returns before invoking `OnBuffAdded` when `State <= LoadingGame`, `Goose/Player.cs:2220-2230`). `Buff` whose `SpellEffect` has a throwing `OnBuffAdded` script attached via `ScriptStub.For<ISpellEffectScript>(...)` (`TestWorldFixture.AddBaseSpellEffect` :56; `SpellEffect` constructor initializes `Stats`/`BuffDoesntStackOver` — precedent `Goose.Tests/BuffNullGuardTests.cs:8-19`); call `player.AddBuff(buff, world)`; assert the buff is in `player.Buffs` and stats applied (the hook runs after application, `Goose/Player.cs:2285-2296`), and the log contains the spell effect name and `ID`.
5. `Player_RemoveBuff_ThrowingOnBuffRemoved_Logged`: same setup (Ready-state player), throwing `OnBuffRemoved`; call `player.RemoveBuff(buff, world)` (`Goose/Player.cs:2438`); assert the buff is gone from `player.Buffs` (removal happens before the hook) and the log contains the spell effect name and `ID`.

**Step 2: Run to verify red**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ScriptHookLoggingTests`
Expected: `UseConsumable_ThrowingScript_DoesNotConsumeItem` FAILS (item consumed today); `Player_AddBuff_ThrowingOnBuffAdded_BuffAppliedAndLogged` FAILS on the log assertion only; the two regression guards PASS.

**Step 3: Implement**

| Site | Message |
|---|---|
| `Goose/Events/BuffTickEvent.cs:27` | `log.Error(e, "SpellEffect OnBuffTick {0} ({1}) target {2} ({3}) Exception", buff.SpellEffect.Name, buff.SpellEffect.ID, buff.Target?.Name, buff.Target?.LoginID)` — `ICharacter` exposes both (`Goose/ICharacter.cs:14-16`) |
| `Goose/Events/PlayerAttackEvent.cs:56` | `log.Error(e, "Item OnMeleeEvent {0} ({1}) player {2} ({3}) Exception", weaponSlot.Item.Name, weaponSlot.Item.TemplateID, this.Player.Name, this.Player.LoginID)` |
| `Goose/NPC.cs:1576` | `log.Error(e, "SpellEffect OnBuffAdded {0} ({1}) target {2} ({3}) Exception", buff.SpellEffect.Name, buff.SpellEffect.ID, buff.Target?.Name, buff.Target?.LoginID)` |
| `Goose/NPC.cs:1688` | `log.Error(e, "SpellEffect OnBuffRemoved {0} ({1}) target {2} ({3}) Exception", buff.SpellEffect.Name, buff.SpellEffect.ID, buff.Target?.Name, buff.Target?.LoginID)` |
| `Goose/Player.cs:2295` | same as NPC `OnBuffAdded` |
| `Goose/Player.cs:2485` | same as NPC `OnBuffRemoved` |
| `Goose/ItemHandler.cs:247` | `log.Error(e, "Item OnCreateEvent {0} ({1}) Exception", item.Name, item.TemplateID)` |
| `Goose/Inventory.cs:433` | `remove = false; log.Error(e, "Item OnUseConsumableEvent {0} ({1}) player {2} ({3}) Exception; item kept", item.Name, item.TemplateID, player.Name, player.LoginID)` |
| `Goose/Map.cs:466` | `log.Error(e, "Map OnLoadTile {0} ({1}) at {2},{3} layer {4} Exception", map.Name, map.ID, x, y, k)` |
| `Goose/Map.cs:503` | same as :466 (Aspereta loader) |
| `Goose/Map.cs:525` | `log.Error(e, "Map OnLoad {0} ({1}) Exception", this.Name, this.ID)` |
| `Goose/Map.cs:583` | `log.Error(e, "Map OnFinishedLoad {0} ({1}) Exception", this.Name, this.ID)` |

Per confirmed decision, `OnLoadTile` logs per-tile (no rate limiting). Add the logger field to `BuffTickEvent.cs` and `PlayerAttackEvent.cs`.

**Step 4: Green + commit**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ScriptHookLoggingTests` — expected PASS. Then `dotnet build Goose.sln` (clean build).

```bash
git add Goose/Events/BuffTickEvent.cs Goose/Events/PlayerAttackEvent.cs Goose/NPC.cs Goose/Player.cs Goose/ItemHandler.cs Goose/Inventory.cs Goose/Map.cs Goose.Tests/ScriptHookLoggingTests.cs
git commit -m "fix: log buff/item/tile script-hook exceptions; keep consumable when script throws"
```

| Invariant | Proved by |
|---|---|
| Throwing `OnUseConsumableEvent` no longer consumes the item | `UseConsumable_ThrowingScript_DoesNotConsumeItem` (red today) |
| Normal consume paths unchanged | `UseConsumable_ScriptReturningTrue_StillConsumes`, `UseConsumable_ScriptReturningFalse_KeepsItem` |
| Throwing `OnBuffAdded` doesn't skip buff application; logged with effect ID | `Player_AddBuff_ThrowingOnBuffAdded_BuffAppliedAndLogged` |
| Throwing `OnBuffRemoved` doesn't undo removal; logged with effect ID | `Player_RemoveBuff_ThrowingOnBuffRemoved_Logged` |
| Remaining 9 sites log on throw | 12 sites changed; tests directly cover 3 (`Inventory.cs:433`, `Player.cs:2295`, `Player.cs:2485`). The untested 9 are log-only additions beside existing containment: `BuffTickEvent.cs:27`, `PlayerAttackEvent.cs:56` (melee), `NPC.cs:1576, 1688` (NPC-side buff hooks — the Player-side twins are tested), `ItemHandler.cs:247` (item creation), and `Map.cs:466, 503, 525, 583` (tile loaders and `OnLoad`/`OnFinishedLoad`, need a binary map-file fixture) — deferred |

---

### Task 3: Placeholder logging for data loads & reference resolution (25 sites)

All sites below are `// log ...` comments where the log never happened. Pattern for every site: capture the offending id into a local if it is currently inlined in the lookup, then emit one `log.Warn` (or `log.Error` where an exception is available) pairing the owning entity's name+ID with the bad reference. Behavior is unchanged for all valid configured values — every `continue`/`return`/filter stays exactly as-is. The one intentional robustness change: null/empty `StartingItems` and empty buff-effect lists, which today NRE or silently throw-and-swallow, now normalize to "no entries".

**Files:**
- Modify: `Goose/Player.cs:695-713` (starting items in `LoadFromAutoCreate`, `Goose/Player.cs:596`), `Goose/Player.cs:2117`
- Modify: `Goose/NPCHandler.cs:145-166` (allies parse in `LoadNPCTemplates`, `Goose/NPCHandler.cs:42`), `Goose/NPCHandler.cs:215, 319, 320`
- Modify: `Goose/ClassHandler.cs:137, 144, 151`
- Modify: `Goose/SpellHandler.cs:158, 165-168, 177, 184-187, 262` (add logger field — `SpellHandler` has none)
- Modify: `Goose/ItemHandler.cs:119`
- Modify: `Goose/Spellbook.cs:145, 224`
- Modify: `Goose/Inventory.cs:180, 218, 324, 559, 680`
- Test: `Goose.Tests/ScriptHookLoggingTests.cs` (extend — same class, for the `CapturingLog` parallelism constraint)

**Step 1: Refactor the allies parse into a testable static (mirrors `ResolveQuests`)**

Extract `Goose/NPCHandler.cs:145-166` into:

```csharp
internal static List<NPCTemplate> ResolveAllies(NPCTemplate npc, string alliesString, NPCHandler handler)
```

- Contract: pure resolver — looks up allies via `handler.GetNPCTemplate`; does not mutate anything. Preserved behavior: ids are parsed lazily, so the result is **allies up to the first unparseable token** (the `Select(Convert.ToInt32)` aborts into the catch) — `"7 notanumber 1"` yields `[7]`, not `[7, 1]`. Keep that; do not "fix" it to skip bad tokens.
- Inside: per-id `if (a is null)` → `log.Warn("npc {0} ({1}): bad ally template id {2}", npc.Name, npc.NPCTemplateID, ally)`; outer `catch (Exception e)` → `log.Error(e, "npc {0} ({1}): failed parsing allies '{2}'", npc.Name, npc.NPCTemplateID, alliesString)`.
- Caller in `LoadNPCTemplates` becomes `npc.Allies = ResolveAllies(npc, npc.AlliesString, this);`
- Precedent: `internal static ResolveQuests` (`Goose/NPCHandler.cs:24`), tested without a DB in `Goose.Tests/NPCTemplateRegistrationTests.cs:50`; `InternalsVisibleTo("Goose.Tests")` already set (`Goose/Goose.csproj:20-24`).

**Step 2: Starting items logging in `Player.LoadFromAutoCreate`**

First, normalize the split at `Goose/Player.cs:687`: `world.Settings.StartingItems.Split(' ')` → `(world.Settings.StartingItems ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)`. Two reasons: `StartingItems` is `null!`-initialized in the class (`Goose/GooseSettings.cs:24`) — only the shipped config supplies a value — so null must mean "no starting items"; and `"".Split(' ')` yields one empty token whose `Convert.ToInt32` throws, which without `RemoveEmptyEntries` would fire the new catch log for **every** auto-create with no starting items. (The `items.Length > 0` guard at :688 now also behaves as intended.)

At `Goose/Player.cs:695-713` (three placeholder comments). Identity note: `LoginID` is assigned later by `PlayerHandler.AddPlayer` (`Goose/PlayerHandler.cs:53`), so during auto-create use `this.Name` + `this.PlayerID` (assigned at `Goose/Player.cs:601-602`), not `LoginID`:
- template null (:697): `log.Warn("player {0} ({1}): bad starting item id {2}", this.Name, this.PlayerID, items[i]);`
- no inventory space (:706): `log.Warn("player {0} ({1}): no inventory space for starting item {2} ({3})", this.Name, this.PlayerID, item.Name, templateid);`
- catch (:709): capture the exception — `catch (Exception e)` — and `log.Error(e, "player {0} ({1}): failed loading starting item {2}", this.Name, this.PlayerID, items[i]);`

**Step 3a: Normalize the buff-effect list splits**

`Goose/SpellHandler.cs:151` and :170: `s.BuffStacksOverString.Split(' ')` / `s.BuffDoesntStackOverString.Split(' ')` → add `StringSplitOptions.RemoveEmptyEntries`. Both columns default to `''` in the schema (`Goose/sql/spells.sql:131-132`), so today `"".Split(' ')` produces one empty token whose conversion is **silently swallowed by the empty catch** — the new catch logging would otherwise emit an error for most spell effects on every startup. With `RemoveEmptyEntries`, only genuinely nonnumeric tokens reach the catch. No behavior change for populated lists (empty tokens were already no-ops).

**Step 3b: The remaining 20 sites** (18 placeholder comments + 2 empty catches in `SpellHandler`)

| Site | Suggested message |
|---|---|
| `Goose/ClassHandler.cs:137` | `log.Warn("class levels: bad class id {0}", classId)` (capture `reader.GetInt32("class_id")` into a local) |
| `Goose/ClassHandler.cs:144` | `log.Warn("class levels: bad level {0} for class {1}", levelId, classId)` |
| `Goose/ClassHandler.cs:151` | `log.Warn("class levels: bad spell id {0} for class {1} level {2}", spellId, classId, levelId)` |
| `Goose/SpellHandler.cs:158` | `log.Warn("spell {0} ({1}): bad spell effect id {2} in buff-stacks-over", s.Name, s.ID, effectid)` |
| `Goose/SpellHandler.cs:165-168` | the empty `catch (Exception) { }` after the loop body — capture `e`: `log.Error(e, "spell {0} ({1}): bad spell effect token '{2}' in buff-stacks-over", s.Name, s.ID, effectid)` (non-numeric tokens currently vanish here) |
| `Goose/SpellHandler.cs:177` | same shape for the second buff-effect list |
| `Goose/SpellHandler.cs:184-187` | same as :165-168 for the second list's catch |
| `Goose/SpellHandler.cs:262` | `log.Warn("spell {0} ({1}): bad spell effect id {2}", spell.Name, spell.ID, spell.SpellEffectID)` |
| `Goose/ItemHandler.cs:119` | `log.Warn("item template {0} ({1}): bad spell effect id {2}", template.Name, template.ID, template.SpellEffectID)` (`ItemTemplate.ID`, `Goose/ItemTemplate.cs:58`) |
| `Goose/NPCHandler.cs:215` | the branch is entered for EITHER a missing item template OR an out-of-range slot — capture `reader.GetInt32("item_template_id")` into a local and log both: `log.Warn("npc {0} ({1}): bad vendor entry, slot {2}, item template id {3}", template.Name, template.NPCTemplateID, vslot.Slot, itemId)` |
| `Goose/NPCHandler.cs:319` | `log.Warn("npc spawns: bad npc id {0}", npc_id)` |
| `Goose/NPCHandler.cs:320` | the `SpawnNPC(...) is null` branch (comment says "couldn't load map" but it's a spawn failure): `log.Warn("npc spawns: failed to spawn npc {0} ({1}) on map {2} at {3},{4}", template.Name, npc_id, map_id, map_x, map_y)` |
| `Goose/Spellbook.cs:145` | `log.Warn("player {0} ({1}): bad spellbook slot {2}", this.player.Name, this.player.LoginID, slot)` |
| `Goose/Spellbook.cs:224` | `log.Warn("player {0} ({1}): bad spell id {2} in LearnSpell", this.player.Name, this.player.LoginID, spellid)` |
| `Goose/Inventory.cs:180` | `log.Warn("player {0} ({1}): bad inventory slot id {2}", this.player.Name, this.player.LoginID, i)` |
| `Goose/Inventory.cs:218` | `log.Warn("player {0} ({1}): slot out of range in SplitSlots {2}/{3}", this.player.Name, this.player.LoginID, id1, id2)` |
| `Goose/Inventory.cs:324` | `log.Warn("player {0} ({1}): failed to remove item {2} ({3}) before equipping", this.player.Name, this.player.LoginID, item.Name, item.TemplateID)` |
| `Goose/Inventory.cs:559` | `log.Warn("player {0} ({1}): no buff to remove while unequipping slot {2}", this.player.Name, this.player.LoginID, equipslot)` |
| `Goose/Inventory.cs:680` | `log.Warn("player {0} ({1}): bad equipped slot id {2}", this.player.Name, this.player.LoginID, i)` |
| `Goose/Player.cs:2117` | `log.Warn("player {0} ({1}): invalid target {2} ({3}) for spell {4} ({5}), casting on self", this.Name, this.LoginID, target?.Name, target?.LoginID, spell.Name, spell.ID)` |

(Verify each in-scope variable name against the actual code at implementation time; the table gives the intended content, not literal code.)

**Step 4: Write the tests** (in `ScriptHookLoggingTests`, with `CapturingLog`)

1. `ResolveAllies_BadIds_KeepsValidOnesAndLogsBadIds` — `NPCHandler` with templates 100 (the NPC itself), 7 and 1 registered (`handler.AddTemplate` with non-null `BaseStats`, pattern `Goose.Tests/NPCTemplateRegistrationTests.cs:13`); call `NPCHandler.ResolveAllies(npc100, "999 7 1", handler)`; assert result contains exactly the templates for 7 and 1, and the log contains `bad ally template id 999` plus `100`.
2. `ResolveAllies_NonNumeric_LogsErrorAndReturnsEmpty` — `"notanumber"`; assert empty result + error log.
3. `LoadFromAutoCreate_BadStartingItemId_LogsWarning` — plain `TestWorldFixture` world (no socket pair needed); set `s.StartingItems = "999 123"` in the fixture configure callback, register item template 123 via `AddBaseItemTemplate`, add a base map matching `Settings.StartingMapID`; call `player.LoadFromAutoCreate(name, password, world)`; assert the log contains `bad starting item id 999` and the inventory holds the 123 item.

**Step 5: Red/green + commit**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ScriptHookLoggingTests` — red first for the log assertions (and for `ResolveAllies` compile), then green.

```bash
git add Goose/Player.cs Goose/NPCHandler.cs Goose/ClassHandler.cs Goose/SpellHandler.cs Goose/ItemHandler.cs Goose/Spellbook.cs Goose/Inventory.cs Goose.Tests/ScriptHookLoggingTests.cs
git commit -m "fix: implement the // log ... placeholders in data loaders and reference resolution"
```

| Invariant | Proved by |
|---|---|
| Bad ally ids are filtered out AND logged with the NPC's identity | `ResolveAllies_BadIds_KeepsValidOnesAndLogsBadIds` |
| Unparseable allies string doesn't abort template load; lazy-parse truncation preserved | `ResolveAllies_NonNumeric_LogsErrorAndReturnsEmpty` |
| Bad starting item id logged; good ids still granted | `LoadFromAutoCreate_BadStartingItemId_LogsWarning` |
| All 25 sites log on the documented condition; behavior unchanged for valid configured values | Log-only one-line additions beside existing `continue`/`return`; no state mutation; full suite green. The null/empty normalizations (`StartingItems ?? ""`, `RemoveEmptyEntries`) are an intentional robustness change: null config used to NRE, empty lists used to throw-and-silently-swallow — both now mean "no entries". Individual tests deferred for the Step-3b sites (each is DB-loader- or packet-driven and would need its own fixture for zero behavioral coverage) |

---

### Task 4: SetConfigCommand — stop reporting success on parse failure

**Files:**
- Modify: `Goose/Events/SetConfigCommandEvent.cs:43-55`
- Test: `Goose.Tests/SetConfigCommandTests.cs` (extend `UnparsableValue_LeavesTheTargetSettingUnchanged` and `NumericChange_MutatesOnlyTheSuppliedWorld`)

**Mutation impact:**
- Source of truth: `world.Settings` properties — unchanged by this fix (both before and after, a failed parse leaves the setting untouched).
- Readers: the GM (server message) and all players (`SendToAll` broadcast at :51).
- Behavior change: on parse failure the GM now receives an error message and the false `[GM] Set Game Setting X to: Y` broadcast (sent to **all players** today) no longer fires. No persisted data, no client-observed state beyond the message.

**Step 1: Extend the tests (red)**

Key setup facts: (a) `SendToAll` only reaches players registered in `PlayerHandler.Players` (`Goose/GameWorld.cs:641-648`), and `CommandPlayerOn` does NOT register. (b) `PlayerHandler.AddPlayer` inserts `player.Sock` as a dictionary key (`Goose/PlayerHandler.cs:56`), and fixture players have a **null** `Sock` (`TestSupport/TestWorldFixture.cs:82-94`) — a null key throws. So before registering, assign each GM a distinct throwaway socket: `gmA.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);` (unconnected is fine — it's only used as a key and by `Disconnect`'s `Close`), and dispose the sockets in the test's `Dispose` (socket-creation precedent: `Goose.Tests/LoginEventNameLengthTests.cs`). Then register: `fixtureA.World.PlayerHandler.AddPlayer(gmA, fixtureA.World)` (`Goose/PlayerHandler.cs:51`; `CommandPlayerOn` sets `State = Ready` which passes the `> LoadingGame` filter).

In `UnparsableValue_LeavesTheTargetSettingUnchanged`, after registering `gmA`:

```csharp
Assert.Contains(gmA.Sent, m => m.Contains("Couldn't set value 'nope' for IdleTimeout."));
Assert.DoesNotContain(gmA.Sent, m => m.Contains("[GM] Set Game Setting"));
```

In `NumericChange_MutatesOnlyTheSuppliedWorld` and `StringChange_MutatesOnlyTheSuppliedWorld`, register the GMs and assert the broadcast still fires on success: `Assert.Contains(gmA.Sent, m => m.Contains("[GM] Set Game Setting IdleTimeout to: 55"));` (and the `ServerName` equivalent for the string test).

Add two malformed-command tests (both **fail today** — exact `/setconfig` throws `ArgumentOutOfRangeException` out of `Substring(11)` into the `EventHandler.cs:387` backstop with no message to the GM):
- `MissingValue_SendsUsage` — `RunCommand(gmA, "/setconfig")` and `RunCommand(gmA, "/setconfig ")`; assert the GM receives the usage message and no exception escapes.
- `UnknownProperty_SendsError` already exists as `UnparsableValue_...`'s sibling — verify `RunCommand(gmA, "/setconfig Nope 1")` gets `Couldn't find Game Setting: Nope.` (existing behavior, guard against regression).

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~SetConfigCommandTests`
Expected: FAIL — `UnparsableValue` red on the error-message assertion (never sent today) and the `DoesNotContain` (false success broadcast IS sent today; only meaningful because the GM is now registered); `MissingValue_SendsUsage` red (exact `/setconfig` throws out of `Substring(11)` today); the new broadcast assertions in `NumericChange`/`StringChange` red (GMs weren't registered).

**Step 2: Implement**

`Goose/Events/SetConfigCommandEvent.cs:12-20`: guard the command **before** the substring (exact `/setconfig` is 10 chars; `Substring(11)` throws first):

Replace the existing `string data = ((string)this.Data).Substring(11);` with the full sequence below (the guards must run before any substring, since exact `/setconfig` is only 10 chars):

```csharp
string data = (string)this.Data;
if (data.Length < 11) { world.Send(this.Player, P.ServerMessage("Usage: /setconfig <setting> <value>")); return; }
string rest = data.Substring(11).Trim();
if (rest.Length == 0) { world.Send(this.Player, P.ServerMessage("Usage: /setconfig <setting> <value>")); return; }
string[] tokens = rest.Split(' ', 2);
if (tokens.Length < 2) { world.Send(this.Player, P.ServerMessage("Usage: /setconfig <setting> <value>")); return; }
```

`Goose/Events/SetConfigCommandEvent.cs:45-48`: replace the empty `catch { }` with (add the logger field to this class — it has none):

```csharp
catch (Exception e)
{
    log.Error(e, "SetConfigCommand {0} {1} Exception", tokens[0], tokens[1]);
    world.Send(this.Player, P.ServerMessage("Couldn't set value '" + tokens[1] + "' for " + tokens[0] + "."));
    return;
}
```

The GM message is deliberately generic ("Couldn't set") because the catch also covers the `parser!` NRE and `setter.Invoke` failures — the actual exception goes to the log, which is the whole point of this plan. The `return` skips the `SendToAll` success broadcast at :51. The string-typed branch (:31-34) is left as-is (out of scope; its failures are caught by the `EventHandler.cs:387` backstop).

**Step 3: Green + commit**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~SetConfigCommandTests` — expected PASS (the extended `NumericChange` test now proves the success broadcast still fires on real changes).

```bash
git add Goose/Events/SetConfigCommandEvent.cs Goose.Tests/SetConfigCommandTests.cs
git commit -m "fix: report parse failure to GM instead of broadcasting false success"
```

| Invariant | Proved by |
|---|---|
| Unparsable value → GM gets error, no false success broadcast, setting unchanged | extended `UnparsableValue_LeavesTheTargetSettingUnchanged` (red today) |
| Successful change still broadcasts | existing `NumericChange_MutatesOnlyTheSuppliedWorld`, `StringChange_MutatesOnlyTheSuppliedWorld` |

---

### Task 5: Deliberate-swallow cleanup (comments, bare catches, silent rollback)

**Files:**
- Modify: `Goose/Events/UpdateSqlCommandEvent.cs:41-43`
- Modify: `Goose/GameServer.cs:102, 131, 342`
- Modify: `Goose/GameWorld.cs:430, 441-456, 597-608, 624-630`
- Modify: `Goose/Events/LoginEvent.cs:85-88`
- Modify: `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs:24-28`

Behavior changes: the rollback logging, plus two small robustness fixes in `GameWorld` (send-exception now drops the connection; `LostConnection` can no longer fail to log or schedule logout) — each gets a focused test. The full suite is the regression gate.

**Step 1: The one real fix — silent rollback failure**

`Goose/Events/UpdateSqlCommandEvent.cs:41-43` — if the ROLLBACK after a mid-script SQL failure also fails, the shared connection is left in a broken state and no one would know. Replace `catch { }` with:

```csharp
catch (Exception rollbackEx)
{
    log.Error(rollbackEx, "Failed to roll back after script failure");
}
```

(mirrors `Goose/Database.cs:264-267`).

**Step 2: Analyzer cleanup — bare `catch { }` → `catch (Exception) { }`**

- `Goose/GameServer.cs:102, 131` — `StopWorld()` from the crash handler; silence is correct (the crash itself is logged at :109-111). No comment needed beyond the existing ones.
- `Goose/GameServer.cs:342` — `sock.Close()` on an already-dead socket; silence correct. (`Goose/GameServer.cs:499` is already `catch (Exception)` — no change.)

**Step 3: Focused tests for the two behavior changes (write first, verify red)** (in `ScriptHookLoggingTests`; these need a world WITH a `GameServer`, so construct locally with the pattern `Goose.Tests/EventHandlerQueueTests.cs:20` — `new GameWorld(new GooseSettings(), new GameServer(new GooseSettings()))` — rather than the fixture's server-less world):

1. `GameWorld_LostConnection_DisposedSocket_LogsAndSchedulesLogout` — create a socket, `Close()` it (makes `RemoteEndPoint` throw), call `world.LostConnection(sock)`; assert the log contains `Connection lost` and `world.EventHandler.Peek() is LogoutEvent` (`Peek` is `internal`, `Goose/EventHandler.cs:349`; `InternalsVisibleTo("Goose.Tests")` is set). Red today: the log never fires and nothing is scheduled.
2. `GameWorld_Send_ThrowingPlayerSend_DropsConnection` — a `Player` subclass that overrides `Send(string)` to throw (deterministic; precedent `CapturingPlayer` at `Goose.Tests/ItemScriptHookTests.cs:48` — `Player.Send` is `virtual`, `Goose/Player.cs:2658`). Setup must assign a name — `PlayerHandler.AddPlayer` calls `player.Name.ToLower()` (`Goose/PlayerHandler.cs:57`) and a null name throws during setup:

```csharp
private sealed class ThrowingPlayer : Player
{
    public ThrowingPlayer() : base(0) { }
    public override bool Send(string data) => throw new InvalidOperationException("test");
}

var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
var player = new ThrowingPlayer { Name = "Thrower", Sock = socket };
world.PlayerHandler.AddPlayer(player, world);
```

Call `world.Send(player, "x")`; assert `world.EventHandler.Peek() is LogoutEvent`. Postcondition note: do NOT assert removal from `world.PlayerHandler.Players` — `LostConnection` only schedules the `LogoutEvent` (`Goose/GameServer.cs:426` `UnregisterConnection` drops socket bookkeeping, not the player); removal happens when the logout event fires. Red today: the exception is swallowed and no event is scheduled.

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ScriptHookLoggingTests` — both new tests FAIL.

**Step 4: Fix the two inaccurate swallows in `GameWorld` (green)**

- `Goose/GameWorld.cs:597-608` (`Send`): today the `catch (Exception) { }` swallows a *throwing* `player.Send` **without** dropping the connection — only the `false`-return path calls `LostConnection`. A socket that throws on send is dead; keeping it is wrong. Replace with: `catch (Exception e) { log.Error(e, "Player {0} send threw, dropping connection", player.Name); this.LostConnection(player.Sock); }` — mirrors the `false` path two lines above.
- `Goose/GameWorld.cs:441-456` (`LostConnection`): the `sock.RemoteEndPoint` read at :446 is INSIDE the broad try, so a disposed socket throws *in the log statement itself* and the `//eaten` catch then skips both the log AND the logout-event scheduling. Also, even with the endpoint read fixed, `Disconnect` throwing would still skip `AddEvent` — so do NOT keep them in one try. Restructure as three independently-guarded steps, so log + disconnect + logout-scheduling are each always attempted:

```csharp
preLoginBuffers.Remove(sock);
string endpoint;
try { endpoint = sock.RemoteEndPoint!.ToString(); } catch { endpoint = "unknown"; }
log.Info("Connection lost: " + endpoint);
try { this.GameServer!.Disconnect(sock); }
catch (Exception e) { log.Error(e, "Disconnect failed for {0}", endpoint); }
try
{
    Event ev = new LogoutEvent();
    ev.Data = sock;
    ev.Ticks += (this.Settings.LogoutLagTime * this.TimerFrequency);
    this.EventHandler.AddEvent(ev);
}
catch (Exception e) { log.Error(e, "Failed to schedule logout for {0}", endpoint); }
```
- `Goose/GameWorld.cs:624-630` (`SendRaw`): keep silent, but fix the rationale in a comment — pre-login rejection path; the caller (`LoginEvent`) disconnects after sending, so no drop is needed here.
- `Goose/GameWorld.cs:430` — handshake send in `NewConnection`; client may already be gone; comment only.

**Step 5: Comments-only cleanups**

Per AGENTS.md, each comment at most one or two lines, and only where the WHY (why silence is safe / why this catch shape) is non-obvious — no comments that merely restate the catch:

- `Goose/Events/LoginEvent.cs:85` — bare `catch { return; }` → `catch (Exception) { return; }` with a comment: malformed login packet from an untrusted client; dropping is the response.
- `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs:24-28` — delete the `TODO`; replace with: the socket may be disposed mid-enumeration as a player disconnects; that player simply isn't counted this tick. Keep the `catch (ObjectDisposedException)` (a `Sock` null-check can't observe disposal; the throw is the signal).

**Step 6: Full verification + commit**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~ScriptHookLoggingTests` (the two new tests now PASS), then `dotnet build Goose.sln` (no new warnings) and `dotnet test Goose.sln` (full suite green).

```bash
git add Goose/Events/UpdateSqlCommandEvent.cs Goose/GameServer.cs Goose/GameWorld.cs Goose/Events/LoginEvent.cs Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs Goose.Tests/ScriptHookLoggingTests.cs
git commit -m "fix: drop connection when send throws; make LostConnection log and schedule logout unconditionally"
```

| Invariant | Proved by |
|---|---|
| Rollback failure after script SQL failure is logged | Pattern-identical to `Goose/Database.cs:264` (which has no dedicated test either); full suite green |
| Throwing `player.Send` triggers the drop path (was: silently swallowed) | `GameWorld_Send_ThrowingPlayerSend_DropsConnection` (red today) — asserts the `LogoutEvent` is scheduled; player removal itself happens when that event fires |
| `LostConnection` always logs and always schedules logout, even when endpoint lookup or `Disconnect` throws | `GameWorld_LostConnection_DisposedSocket_LogsAndSchedulesLogout` (red today) + the three independently-guarded steps |

---

## Out of scope (deliberately untouched)

- The ~40 `catch (Exception)` default-value resets in `Goose/Events/*` command/handler parsing — untrusted client input falling back to safe defaults; logging every malformed packet is noise.
- The `// log bad packet` malformed-packet-shape checks at event entry in `Goose/Events/*` (`ChatEvent.cs:32`, `FacingEvent.cs:38, 42`, `KillBuffEvent.cs:19, 33`, `MoveEvent.cs:55`, `InventoryChangeSlotEvent.cs:36`, `InventorySplitEvent.cs:43`, `InventoryUseEvent.cs:30`, etc.) — same untrusted-client-input reasoning: these reject packet *shapes* from potentially probing clients before any state is touched. Distinct from Task 3's post-parse invariant violations, which indicate a real bug or a corrupted reference and are logged.
- `KillBuffEvent.cs:26`, `PasswordHasher.cs:85`, `Database.cs:296`, `Player.cs:2684`, `GameServer.cs:268, 402`, `PropertiesDictionary.cs:100, 114` — intentional, already correct (commented or catch-filter rethrows).
- `EventHandler.cs:387`, `Map.cs:619`, `PickupItemEvent.cs:96`, `GameWorld.cs:231, 361`, `Database.cs` — already log properly.
