# Instance-Owned Game Settings Implementation Plan

**Goal:** Replace mutable static `GameWorld.Settings` with one explicit settings object owned by `GameServer` and supplied to each `GameWorld`, allowing independent test worlds to run in parallel.

**Architecture:** Extract file loading into `GooseSettingsLoader`; let `Program` load once and `GameServer` retain the object across world restarts. Migrate world, handler, event, domain, and script consumers to the associated world instance, then remove the temporary static compatibility path and broad xUnit collection.

**Tech Stack:** .NET 10, C#, xUnit, System.Text.Json, Roslyn `.csx` scripts, existing `Paths` path resolution.

---

**Dependency:** Complete `docs/plans/2026-08-25-fast-test-boundary.md` first. This plan assumes `Goose.IntegrationTests`, `TestSupport/TestWorldFixture.cs`, and `docs/testing.md` exist.

## APIs verified

- Static configuration is declared, loaded, and consumed during world construction in `Goose/GameWorld.cs:51,100-145,153-180`.
- `Paths.Initialize`, `ResolveBase`, and `ResolveData` are process-global path APIs at `Goose/Paths.cs:20-52`.
- `GameServer.Run` creates every replacement world inside its restart loop at `Goose/GameServer.cs:80-94`.
- `ScriptHandler.GetScript<T>` currently resolves paths from static settings and caches by resolved path at `Goose/Scripting/ScriptHandler.cs:10-30`.
- Every event receives its owning world through `Event.Ready(GameWorld world)` at `Goose/Event.cs:10-28`.
- Global and map scripts receive `GameWorld` through `IGlobalScript.OnLoaded` and every `IMapScript` hook at `Goose/Scripting/IGlobalScript.cs:9-12` and `Goose/Scripting/IMapScript.cs:9-24`.
- Item, spell-effect, and quest base-script hooks likewise receive `GameWorld`: `Goose/Scripting/BaseItemScript.cs:9-46`, `Goose/Scripting/BaseSpellEffectScript.cs:9-36`, and `Goose/Scripting/BaseQuestScript.cs:10-37`.
- `SetConfigCommandEvent` mutates the static settings object through reflection at `Goose/Events/SetConfigCommandEvent.cs:11-54`.
- `SaveConfigCommandEvent` is currently a no-op with persistence commented out at `Goose/Events/SaveConfigCommandEvent.cs:8-19`; this plan does not add configuration persistence.
- `GameWorld.ExperienceModifier` is derived once from configured `ExperienceModifier` and later changed by `PlayerCountExperienceModifierUpdateEvent`: `Goose/GameWorld.cs:98,180` and `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs:36-47`.

## Global invariants

- `GooseSettings.json` fields, defaults, precedence, first-run copy behavior, and serialization format do not change.
- One `GameServer` retains the same `GooseSettings` reference across all world restarts.
- Two independently constructed worlds may use conflicting settings without observing each other.
- `SetConfigCommandEvent` mutates only the settings object belonging to its supplied world.
- `SaveConfigCommandEvent` remains a no-op; persistence is out of scope.
- Runtime settings remain mutable on the game thread. This plan does not claim that one settings instance is safe for concurrent writers.
- Dimensions remain canonical `.csx` code and read settings through supplied world hooks.
- No ambient context, service locator, `AsyncLocal`, or final static compatibility property is permitted.
- There is no database/schema migration because no persisted model changes.

### Task 1: Extract settings loading and establish server ownership

**Files:**

- Create: `Goose/GooseSettingsLoader.cs`
- Create: `Goose.Tests/GooseSettingsLoaderTests.cs`
- Modify: `Goose/GameWorld.cs:100-145`
- Modify: `Goose/GameServer.cs:52-91`
- Modify: `Goose/Program.cs:18-36`
- Modify: `Goose.Tests/EventHandlerIntervalTests.cs`
- Modify: `Goose.Tests/EventHandlerQueueTests.cs`
- Modify: `Goose.Tests/GameServerStartupTests.cs`
- Modify: `Goose.Tests/LoginEventNameLengthTests.cs`
- Modify: `Goose.Tests/PlayerFirstSaveTests.cs`
- Modify: `Goose.Tests/PlayerSendTests.cs`
- Modify: `Goose.Tests/PreLoginReassemblyTests.cs`

**Mutation impact:**

- Source of truth changed: file selection/deserialization moves out of `GameWorld`; `GameServer` begins retaining the loaded reference while the static property remains the temporary publication point until Task 6.
- Important readers: all current `GameWorld.Settings` references; world construction at `GameWorld.cs:153-180`; server startup/restart at `GameServer.cs:80-94`; configuration mutation at `SetConfigCommandEvent.cs:11-54`.
- Derived/cached state affected: `GameWorld.ExperienceModifier` snapshots a configured value at construction; handlers and arrays may snapshot sizes during their own construction. No persisted or client cache is introduced by this task.
- Required propagation sequence:
  1. `Paths.Initialize` selects roots.
  2. The temporary `GameWorld` static initializer calls `GooseSettingsLoader.Load` once.
  3. `Program` reads that object from `GameWorld.Settings` and passes the same reference to `GameServer`.
  4. `GameServer` retains the exact reference for later worlds.
- Invariants to preserve:
  - Data directory wins over shipped configuration.
  - Missing data configuration copies the shipped file when roots differ.
  - Missing both files throws the existing `FileNotFoundException` shape.
  - Malformed JSON fails startup and does not produce a partial server.
- Observable proof required: loader tests inspect the returned values and copied file; server tests use `Assert.Same` on the retained object.

**Step 1: Write loader tests (red)**

Add focused tests for an internal two-root overload:

```csharp
internal static GooseSettings Load(string baseDirectory, string dataDirectory);
```

Tests:

- data file takes precedence when both roots contain settings;
- shipped file is copied to an empty distinct data directory and the copied values are returned;
- missing files throw `FileNotFoundException` naming the data target;
- malformed JSON throws and does not construct settings;
- same base/data root reads one file without copying over itself.

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter FullyQualifiedName~GooseSettingsLoaderTests
```

Expected: compile failure because `GooseSettingsLoader` does not exist.

**Step 2: Implement the loader (green)**

Create:

```csharp
public static class GooseSettingsLoader
{
    public static GooseSettings Load();
    internal static GooseSettings Load(string baseDirectory, string dataDirectory);
}
```

The public overload passes `Paths.BaseDir` and `Paths.DataDir`. The internal overload implements the exact selection/copy/deserialization sequence currently at `GameWorld.cs:110-144` using `JsonHelper.SettingsOptions`.

Remove `LoadSettings` from `GameWorld`, but retain `GameWorld.Settings` and its static constructor temporarily. Change the constructor body to:

```csharp
Settings = GooseSettingsLoader.Load();
```

Task 6 removes both the static property and initializer after every reader has migrated.

**Step 3: Give `GameServer` explicit ownership**

Add:

```csharp
public GooseSettings Settings { get; }
public GameServer(GooseSettings settings);
```

Throw `ArgumentNullException` for null. Remove the parameterless constructor and update tests to pass an explicit settings object.

In `Program.Main`, after path and logging setup, pass the already-loaded static reference into the new owner:

```csharp
var settings = GameWorld.Settings;
var server = new GameServer(settings);
```

This read is temporary and exists only to keep unmigrated readers coherent between Tasks 1 and 6 without loading the settings file twice.

**Step 4: Add ownership regression tests**

Add tests proving:

- the server exposes the exact supplied object;
- null is rejected;
- changing a property through the supplied object is visible through `server.Settings`.

The last test is adversarial against copying or re-deserializing settings in the constructor.

**Step 5: Run focused and full tests**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~GooseSettingsLoaderTests|FullyQualifiedName~GameServerStartupTests"
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Expected: all pass under the still-serialized compatibility boundary.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Existing settings precedence/copy behavior is unchanged | loader filesystem tests |
| Partial or invalid configuration cannot create a server | missing/malformed tests plus required constructor |
| Server retains, rather than copies, the settings object | `Assert.Same` ownership test |

**Step 6: Commit**

```bash
git add Goose/GooseSettingsLoader.cs Goose/GameWorld.cs Goose/GameServer.cs Goose/Program.cs Goose.Tests
git commit -m "refactor: make GameServer own loaded settings"
```

### Task 2: Add explicit world and script-handler configuration

**Files:**

- Modify: `Goose/GameWorld.cs:35-180`
- Modify: `Goose/GameServer.cs:80-94`
- Modify: `Goose/Scripting/ScriptHandler.cs:10-30`
- Create: `Goose.Tests/GameWorldSettingsIsolationTests.cs`
- Modify: `TestSupport/TestWorldFixture.cs`
- Modify: `Goose.Tests/BuiltInCurrencyTests.cs`
- Modify: `Goose.Tests/EventHandlerIntervalTests.cs`
- Modify: `Goose.Tests/EventHandlerQueueTests.cs`
- Modify: `Goose.Tests/InvisibilityAggroTests.cs`
- Modify: `Goose.Tests/InvisibilityBreakTests.cs`
- Modify: `Goose.Tests/InvisibilityCounterTests.cs`
- Modify: `Goose.Tests/InvisibilityMapLoadTests.cs`
- Modify: `Goose.Tests/InvisibilityTransitionTests.cs`
- Modify: `Goose.Tests/ItemHandlerRegistrationTests.cs`
- Modify: `Goose.Tests/ItemScriptHookTests.cs`
- Modify: `Goose.Tests/LoginEventNameLengthTests.cs`
- Modify: `Goose.Tests/MapPlayerCanJoinHookTests.cs`
- Modify: `Goose.Tests/NPCSpawnRegistrationTests.cs`
- Modify: `Goose.Tests/PetDestroyInvisibilityTests.cs`
- Modify: `Goose.Tests/PlayerFirstSaveTests.cs`
- Modify: `Goose.Tests/PlayerSendTests.cs`
- Modify: `Goose.Tests/PreLoginReassemblyTests.cs`
- Modify: `Goose.Tests/QuestScriptTests.cs`
- Modify: `Goose.Tests/ScriptLoadDirectiveTests.cs`
- Modify: `Goose.Tests/Fixtures/QuestScriptFixture.cs`
- Modify: `Goose.IntegrationTests/Fixtures/GlobalScriptFixture.cs`

**Mutation impact:**

- Source of truth changed: each world gains an instance configuration reference supplied by its creator.
- Important readers: world constructor initialization, `ScriptHandler.GetScript<T>`, every later migrated world consumer, test fixtures.
- Derived/cached state affected: `GameWorld.ExperienceModifier` and the script cache’s absolute-path keys. The cache remains per `ScriptHandler`; no global cache is added.
- Required propagation sequence:
  1. Creator chooses settings.
  2. World stores the exact reference before constructing handlers.
  3. World constructs `ScriptHandler` with the same reference.
  4. Script handler resolves paths from that reference and caches the resolved absolute path.
- Invariants to preserve:
  - Script cache hits still occur for repeated relative paths in one world.
  - Identical relative paths in worlds with different data roots resolve independently.
  - Failed script compilation does not publish a cache entry because insertion remains after construction.
- Observable proof required: real temporary scripts with identical relative names but different returned types/behavior are loaded through two worlds.

**Step 1: Write isolation tests (red)**

Add tests that construct two temporary data roots and two settings objects with conflicting `DataPath` and `ExperienceModifier` values. Assert:

- each world retains its supplied object by reference;
- each world snapshots its own initial `ExperienceModifier`;
- each world loads the script under its own data root;
- changing the legacy static property after construction does not redirect either world’s `ScriptHandler`.

Expected red failure: the current constructor has no settings parameter and `ScriptHandler` reads static state at `ScriptHandler.cs:21`.

**Step 2: Add the transitional world API**

Until Task 6, use a distinct instance property name to coexist with static `GameWorld.Settings`:

```csharp
public GooseSettings Configuration { get; }
public GameWorld(GooseSettings settings, GameServer server = null);
```

Require non-null settings. Assign `Configuration` before handlers. Set `ExperienceModifier` from `Configuration`.

Provide a temporary delegating constructor only while unmigrated call sites remain:

```csharp
public GameWorld(GameServer server) : this(GameWorld.Settings, server) { }
```

Task 6 removes it.

**Step 3: Make `ScriptHandler` settings-owned**

Change its constructor to:

```csharp
public ScriptHandler(GooseSettings settings);
```

Store the non-null reference privately. Resolve `GetScript<T>` paths using `settings.DataPathAbsolute`. Keep the existing dictionary and load-before-insert order.

Construct it from `GameWorld.Configuration`.

**Step 4: Pass server settings on every restart**

Change the publication sequence in `GameServer.Run` at `GameServer.cs:84-93` to:

```text
reset socket/connection collections
→ construct GameWorld(Settings, this)
→ complete world startup
→ enter game loop
```

The new world is assigned to the private `gameworld` field before `Start`, matching current behavior. If construction or startup fails, the existing catch/cleanup path remains responsible; no additional world registry exists.

**Step 5: Update direct construction sites and run tests**

Update call sites mechanically to pass their current settings where convenient. Remaining tests may use the temporary overload until Task 5.

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~GameWorldSettingsIsolationTests|FullyQualifiedName~ScriptLoadDirectiveTests|FullyQualifiedName~GameServerStartupTests"
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Expected: isolation tests now pass; both suites remain green.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Worlds retain distinct settings references | two-world `Assert.Same`/`Assert.NotSame` test |
| Script resolution cannot be redirected by another world | adversarial same-relative-path test |
| Restart-created worlds use server settings | focused `GameServer` construction/restart seam test or existing startup harness |

**Step 6: Commit**

```bash
git add Goose/GameWorld.cs Goose/GameServer.cs Goose/Scripting/ScriptHandler.cs Goose.Tests Goose.IntegrationTests TestSupport
git commit -m "refactor: give each GameWorld explicit settings"
```

### Task 3: Migrate core world, handler, and domain consumers

**Files:**

- Modify: `Goose/GameWorld.cs`
- Modify: `Goose/GameServer.cs`
- Modify: `Goose/BankWindow.cs`
- Modify: `Goose/CombinationHandler.cs`
- Modify: `Goose/Group.cs`
- Modify: `Goose/GuildHandler.cs`
- Modify: `Goose/Inventory.cs`
- Modify: `Goose/ItemHandler.cs`
- Modify: `Goose/LoginThrottle.cs`
- Modify: `Goose/Map.cs`
- Modify: `Goose/MapHandler.cs`
- Modify: `Goose/NPC.cs`
- Modify: `Goose/NPCHandler.cs`
- Modify: `Goose/Pet.cs`
- Modify: `Goose/Player.cs`
- Modify: `Goose/PlayerHandler.cs`
- Modify: `Goose/Ranks.cs`
- Modify: `Goose/SpellEffect.cs`
- Modify: `Goose/Spellbook.cs`
- Test: existing focused tests for each affected subsystem

**Mutation impact:**

- Source of truth changed: reads move from process-global `GameWorld.Settings` to the `Configuration` owned by the world participating in the operation.
- Important readers: inventory/bank sizing, player creation and experience, NPC limits and behavior, map and item loading, guild/rank intervals, spell effects, throttling.
- Derived/cached state affected: arrays and containers sized during construction/load; `world.ExperienceModifier`; handler registries populated from DB. No new derived state is added.
- Required propagation sequence:
  1. Use the existing `GameWorld world` argument when present.
  2. For world-owned code, use `this.Configuration`.
  3. If no world reaches a constructor that snapshots a size/limit, pass `GooseSettings` or the narrow scalar from the owning world.
  4. Do not read the temporary static bridge in migrated files.
- Invariants to preserve:
  - Existing container sizes and gameplay calculations use the same fields.
  - Runtime `world.ExperienceModifier` remains distinct from configured base `ExperienceModifier`.
  - A failed load does not partially publish new handler entries beyond current behavior.
- Observable proof required: existing domain tests assert final inventory, experience, map, NPC, and persistence outcomes using real objects.

**Step 1: Group current static reads by call path**

Run, including both qualified and unqualified settings access inside `GameWorld`:

```bash
rg -n "GameWorld\.Settings" Goose/GameWorld.cs Goose/GameServer.cs Goose/BankWindow.cs Goose/CombinationHandler.cs Goose/Group.cs Goose/GuildHandler.cs Goose/Inventory.cs Goose/ItemHandler.cs Goose/LoginThrottle.cs Goose/Map.cs Goose/MapHandler.cs Goose/NPC.cs Goose/NPCHandler.cs Goose/Pet.cs Goose/Player.cs Goose/PlayerHandler.cs Goose/Ranks.cs Goose/SpellEffect.cs Goose/Spellbook.cs
rg -n "\bSettings\." Goose/GameWorld.cs
```

For each match, record whether `world`, `this.Configuration`, or an injected scalar is the narrowest existing ownership path. Do not add a new global accessor.

**Step 2: Add adversarial isolation cases before migration**

Extend existing tests to cover at least:

- two inventories created for worlds with different `InventorySize` values retain their own capacities;
- experience calculations for two worlds with conflicting configured modifiers use the correct world;
- NPC ID allocation/limits use the supplied world’s limits.

Expected red behavior: at least one case observes whichever settings object was assigned globally last.

**Step 3: Migrate core and handler reads**

Replace reads according to the ownership map. In `GameWorld`, replace both `GameWorld.Settings` and unqualified `Settings` member access with `Configuration`. Signature changes must make ownership explicit. Avoid storing both `GameWorld` and `GooseSettings` when the object already retains the world.

Keep `GameWorld.ExperienceModifier` initialization and `PlayerCountExperienceModifierUpdateEvent` semantics unchanged. Do not make `SetConfig` retroactively recompute this derived runtime field.

**Step 4: Run subsystem tests after each coherent group**

Run filters for inventory/currency, player/economy, NPC/map, guild/persistence, and spell tests after their corresponding files compile. Then run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
rg -n "GameWorld\.Settings" Goose/GameWorld.cs Goose/GameServer.cs Goose/BankWindow.cs Goose/CombinationHandler.cs Goose/Group.cs Goose/GuildHandler.cs Goose/Inventory.cs Goose/ItemHandler.cs Goose/LoginThrottle.cs Goose/Map.cs Goose/MapHandler.cs Goose/NPC.cs Goose/NPCHandler.cs Goose/Pet.cs Goose/Player.cs Goose/PlayerHandler.cs Goose/Ranks.cs Goose/SpellEffect.cs Goose/Spellbook.cs
rg -n "\bSettings\." Goose/GameWorld.cs
```

Expected: both suites pass; the first `rg` returns no matches in this task’s files. The second may match the static initializer only; no runtime method in `GameWorld` may read the temporary static property.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Snapshot sizes come from the owning world | conflicting-size inventory test |
| Experience uses configured base plus the correct world runtime modifier | two-world economy test |
| Handler limits do not leak between worlds | conflicting NPC-limit test |
| Existing persistence and packet-visible outcomes remain unchanged | existing player/guild/inventory tests |

**Step 5: Commit**

```bash
git add Goose Goose.Tests Goose.IntegrationTests
git commit -m "refactor: use world settings in core gameplay"
```

### Task 4: Migrate events, commands, and canonical scripts

**Files:**

- Modify: `Goose/Events/BuffTickEvent.cs`
- Modify: `Goose/Events/BuyManaCommandEvent.cs`
- Modify: `Goose/Events/BuyVitaCommandEvent.cs`
- Modify: `Goose/Events/ClearMapItemsEvent.cs`
- Modify: `Goose/Events/CreditsUpdateEvent.cs`
- Modify: `Goose/Events/CustomCommandEvent.cs`
- Modify: `Goose/Events/DestroyItemEvent.cs`
- Modify: `Goose/Events/DestroySpellEvent.cs`
- Modify: `Goose/Events/GuildCreateCommandEvent.cs`
- Modify: `Goose/Events/HairdyeCommandEvent.cs`
- Modify: `Goose/Events/InventoryChangeSlotEvent.cs`
- Modify: `Goose/Events/InventorySplitEvent.cs`
- Modify: `Goose/Events/InventoryToWindowEvent.cs`
- Modify: `Goose/Events/InventoryUseEvent.cs`
- Modify: `Goose/Events/LoginContinuedEvent.cs`
- Modify: `Goose/Events/LoginEvent.cs`
- Modify: `Goose/Events/MoveEvent.cs`
- Modify: `Goose/Events/PetDamageCommandEvent.cs`
- Modify: `Goose/Events/PetVitaCommandEvent.cs`
- Modify: `Goose/Events/PickupItemEvent.cs`
- Modify: `Goose/Events/PlaceSpawnCommandEvent.cs`
- Modify: `Goose/Events/PlayerCastSpellEvent.cs`
- Modify: `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs`
- Modify: `Goose/Events/SaveConfigCommandEvent.cs`
- Modify: `Goose/Events/SetConfigCommandEvent.cs`
- Modify: `Goose/Events/SpellInfoEvent.cs`
- Modify: `Goose/Events/SpellbookBackEvent.cs`
- Modify: `Goose/Events/SpellbookNextEvent.cs`
- Modify: `Goose/Events/SpellbookSwapEvent.cs`
- Modify: `Goose/Events/UpdateSqlCommandEvent.cs`
- Modify: `Goose/Events/VendorPurchaseInventoryEvent.cs`
- Modify: `Goose/Events/VendorSellInventoryEvent.cs`
- Modify: `Goose/Events/WindowToInventoryEvent.cs`
- Modify: `Goose/Data/Aspereta/Scripts/Global/Aspereta.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Commands.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionItem.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Maps.csx`
- Modify: `Goose/Data/Illutia/Scripts/Spell/ArenaMapDebuff.csx`
- Modify: `Goose/Data/Illutia/Scripts/Spell/ZombieMapDebuff.csx`
- Test: event-focused unit tests and the complete integration project

**Mutation impact:**

- Source of truth changed: events and scripts read and mutate the supplied world’s configuration.
- Important readers: every `Event.Ready(world)` implementation, global/map/item/spell hooks, `SetConfigCommandEvent`, configured timing and purchase/limit calculations.
- Derived/cached state affected: `world.ExperienceModifier` is not automatically recomputed when `SetConfig` changes the base setting; scheduled event ticks remain values computed when enqueued. This preserves current behavior.
- Required propagation sequence:
  1. Event/script receives `world` through the verified hook.
  2. It reads or mutates `world.Configuration`.
  3. Existing gameplay methods consume final values through that same world.
  4. Existing packet/broadcast calls report the result.
- Invariants to preserve:
  - Invalid `SetConfig` property names still report failure without mutation.
  - Parse failures still leave the setting unchanged.
  - Successful changes affect only the supplied world’s object.
  - `SaveConfigCommandEvent` remains a no-op.
- Observable proof required: tests assert final property values on two real world settings objects and existing player-visible messages where already covered.

**Step 1: Add `SetConfig` isolation tests (red)**

Using real `GameWorld` objects and a GM-ready player, add tests proving:

- a valid change mutates `world.Configuration`;
- another world’s conflicting settings remain unchanged;
- an unknown property leaves both unchanged and sends the existing error;
- an unparsable value leaves the target setting unchanged.

Expected red behavior: the valid change mutates the process-global object rather than the supplied world.

**Step 2: Migrate event files**

Every event already receives `GameWorld world` through `Event.Ready` at `Goose/Event.cs:27`. Replace static reads with `world.Configuration`. Preserve existing scheduling calculations, ordering, broadcasts, and exception behavior.

For `SetConfigCommandEvent`, invoke reflection getters/setters on `world.Configuration`. Do not redesign reflection parsing in this scope.

For `SaveConfigCommandEvent`, remove or update stale static references only; keep it behaviorally inert.

**Step 3: Migrate scripts through existing hooks**

Replace static reads with the `world` parameter already present on global, map, item, spell, and quest hooks. Where a helper defined inside one `.csx` part lacks a world parameter, thread the same world through the helper call rather than introducing static state.

Keep all logic in `.csx`; do not convert scripts to compiled production classes.

**Step 4: Run focused and integration coverage**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --filter "FullyQualifiedName~SetConfig|FullyQualifiedName~EventHandler|FullyQualifiedName~LoginEvent|FullyQualifiedName~InventoryChangeSlot"
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
rg -n "GameWorld\.Settings" Goose/Events Goose/Data --glob '*.cs' --glob '*.csx'
```

Expected: focused tests and all canonical script integration tests pass; `rg` returns no active matches.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Configuration mutation is world-local | adversarial two-world `SetConfig` test |
| Invalid mutation leaves state unchanged | unknown-property and parse-failure tests |
| Canonical scripts retain behavior | complete `Goose.IntegrationTests` run |
| No persistence was accidentally introduced | `SaveConfigCommandEvent` remains inert and no file-write test is added |

**Step 5: Commit**

```bash
git add Goose/Events Goose/Data Goose.Tests Goose.IntegrationTests
git commit -m "refactor: use world settings in events and scripts"
```

### Task 5: Migrate fixtures and all test code off static settings

**Files:**

- Modify: `TestSupport/TestWorldFixture.cs`
- Modify: `Goose.Tests/Fixtures/QuestScriptFixture.cs`
- Modify: `Goose.Tests/Fixtures/VendorFixture.cs`
- Modify: `Goose.IntegrationTests/Fixtures/GlobalScriptFixture.cs`
- Modify: `Goose.Tests/EventHandlerIntervalTests.cs`
- Modify: `Goose.Tests/GameServerStartupTests.cs`
- Modify: `Goose.Tests/InventoryChangeSlotTests.cs`
- Modify: `Goose.Tests/InvisibilityAggroTests.cs`
- Modify: `Goose.Tests/InvisibilityBreakTests.cs`
- Modify: `Goose.Tests/InvisibilityCounterTests.cs`
- Modify: `Goose.Tests/InvisibilityMapLoadTests.cs`
- Modify: `Goose.Tests/InvisibilityTransitionTests.cs`
- Modify: `Goose.Tests/ItemScriptHookTests.cs`
- Modify: `Goose.Tests/LoginEventNameLengthTests.cs`
- Modify: `Goose.Tests/MapPlayerCanJoinHookTests.cs`
- Modify: `Goose.Tests/NPCSpawnRegistrationTests.cs`
- Modify: `Goose.Tests/PetDestroyInvisibilityTests.cs`
- Modify: `Goose.Tests/PlayerEconomyOverloadTests.cs`
- Modify: `Goose.Tests/QuestScriptTests.cs`
- Modify: `Goose.Tests/ResetModifiersTests.cs`
- Modify: `Goose.Tests/ScriptLoadDirectiveTests.cs`
- Modify: `Goose.Tests/VendorPurchaseCurrencyTests.cs`
- Modify: `Goose.IntegrationTests/DimensionCurrencyCommandTests.cs`
- Modify: `Goose.IntegrationTests/DimensionItemScriptTests.cs`
- Modify: `Goose.IntegrationTests/DimensionRebirthTests.cs`
- Modify: `Goose.IntegrationTests/DimensionVendorStockTests.cs`
- Modify temporarily: `Goose.Tests/Collections/GameWorldSettingsCollection.cs`
- Modify temporarily: `Goose.IntegrationTests/Collections/GameWorldSettingsCollection.cs`

**Mutation impact:**

- Source of truth changed: each fixture’s `Settings` property replaces save/assign/restore of global settings.
- Important readers: all tests that configure sizes, limits, experience, data paths, and script resolution; fixtures composing `TestWorldFixture`.
- Derived/cached state affected: constructed inventories, vendor arrays, script handlers, and world runtime modifiers. Tests must configure values before constructing the dependent object unless the production behavior intentionally reads settings dynamically.
- Required propagation sequence:
  1. Fixture creates settings.
  2. Optional configuration callback mutates them.
  3. Fixture constructs world with the same object.
  4. Test mutates `fixture.Settings` only for behavior that production reads dynamically.
  5. Disposal removes owned resources without restoring any global settings.
- Invariants to preserve:
  - Fixture cleanup cannot affect another fixture’s settings.
  - Configuration needed for constructor-sized arrays is applied before construction.
  - No test relies on execution order.
- Observable proof required: parallel construction tests use conflicting values and inspect real containers/scripts.

**Step 1: Update fixture contracts**

`TestWorldFixture` must expose:

```csharp
public GooseSettings Settings { get; }
public TestWorldFixture(Action<GooseSettings> configure = null);
```

Construct `World` with `new GameWorld(Settings)`. Remove `previousSettings`, static assignment, and restoration. Disposal deletes only owned files/directories.

Apply the same ownership pattern to `QuestScriptFixture` and `GlobalScriptFixture`. Make `VendorFixture` size vendor slots from its composed fixture’s settings.

**Step 2: Migrate all test reads and writes**

- Replace `GameWorld.Settings` with `fixture.Settings`, `world.Configuration`, or a local `settings` variable.
- Replace `new GameServer()` with `new GameServer(settings)`.
- Replace temporary global save/restore `try/finally` blocks with local object construction.
- Configure values before creating worlds/containers when their sizes are snapshotted.

Run `rg` after each test group. No new test-only static settings holder is allowed.

**Step 3: Add fixture concurrency regression tests**

Create two fixtures concurrently with conflicting `DataPath`, `InventorySize`, `VendorSlotSize`, and `ExperienceModifier`. Assert final values and real constructed container sizes remain isolated after both are active.

Add an adversarial disposal test: dispose fixture A while fixture B remains active, then prove B can still resolve its script/data path and use its settings.

**Step 4: Run both suites while collections remain**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
rg -n "GameWorld\.Settings" Goose.Tests Goose.IntegrationTests TestSupport --glob '*.cs'
```

Expected: both pass; `rg` returns no matches. Collections remain until Task 6 so this task isolates fixture correctness from scheduling changes.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Disposing one fixture cannot roll back another | adversarial overlapping-lifetime test |
| Constructor-snapshotted settings use configured values | real inventory/vendor-size assertions |
| Script resolution remains fixture-local | surviving fixture script-load assertion |

**Step 5: Commit**

```bash
git add TestSupport Goose.Tests Goose.IntegrationTests
git commit -m "test: isolate settings per fixture"
```

### Task 6: Remove compatibility state and restore parallelism

**Files:**

- Modify: `Goose/GameWorld.cs`
- Modify: `Goose/Program.cs`
- Modify: all active source references to `.Configuration`
- Delete: `Goose.Tests/Collections/GameWorldSettingsCollection.cs`
- Delete: `Goose.IntegrationTests/Collections/GameWorldSettingsCollection.cs`
- Modify: tests carrying either collection attribute

**Mutation impact:**

- Source of truth changed: the temporary static `GameWorld.Settings` and transitional `Configuration` name collapse into the final instance `GameWorld.Settings` property.
- Important readers: every migrated production/test reference and `Program`’s temporary bridge assignment.
- Derived/cached state affected: none; this is final ownership cleanup and scheduling activation.
- Required propagation sequence:
  1. Change `Program` from reading `GameWorld.Settings` to calling `GooseSettingsLoader.Load()` directly.
  2. Remove static property and delegating constructor.
  3. Rename instance `Configuration` to `Settings` across source, scripts, and tests.
  4. Build before changing xUnit collections.
  5. Remove broad settings collections and enable default scheduling.
- Invariants to preserve:
  - Every world still requires non-null settings.
  - No source can access settings without a world/server/explicit reference.
  - Remaining process-global `Paths` tests stay narrowly serialized if concurrent mutation is proven unsafe.
- Observable proof required: structural searches, compile-time enforcement, two-world tests, and repeated parallel suite execution.

**Step 1: Remove the bridge and finalize the API**

Final `GameWorld` surface:

```csharp
public GooseSettings Settings { get; }
public GameWorld(GooseSettings settings, GameServer server = null);
```

Change `Program` to:

```csharp
var settings = GooseSettingsLoader.Load();
var server = new GameServer(settings);
```

Delete:

- static `GameWorld.Settings`;
- transitional `GameWorld.Configuration` name;
- temporary `GameWorld(GameServer)` overload;
- temporary assignment in `Program`.

Update all `.Configuration` references to `.Settings`.

**Step 2: Prove structural completion**

Run:

```bash
rg -n "static GooseSettings Settings|GameWorld\.Settings|\.Configuration" Goose Goose.Tests Goose.IntegrationTests TestSupport --glob '*.cs' --glob '*.csx'
```

Interpretation:

- `static GooseSettings Settings`: zero matches.
- `GameWorld.Settings`: zero matches because static type access is forbidden.
- `.Configuration`: zero matches.
- Instance `.Settings` uses are expected and checked by compilation.

**Step 3: Remove broad settings collections**

Delete both `GameWorldSettingsCollection` definitions and remove their attributes/imports.

Search remaining collection definitions. Retain serialization only for a concrete global such as `Paths` or environment variables, and name the collection after that resource. Do not serialize tests merely because they construct worlds.

**Step 4: Run repeated parallel verification**

Run the unit project ten consecutive times with normal xUnit parallelization:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Repeat ten times, stopping on the first failure and diagnosing the actual shared resource before adding any collection.

Run integration once normally, then at least three consecutive times to expose script/temp-path ordering issues:

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Expected: all runs pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Static settings are impossible to use | zero-result structural searches plus build |
| Unit tests are order- and schedule-independent | ten consecutive default-parallel runs |
| Integration fixture paths/settings are isolated | repeated integration runs |
| Genuine global resources remain explicit | review of remaining collection definitions |

**Step 5: Commit**

```bash
git add Goose Goose.Tests Goose.IntegrationTests TestSupport
git commit -m "refactor: remove global GameWorld settings"
```

### Task 7: Final performance, behavior, and documentation verification

**Files:**

- Modify: `docs/testing.md`
- Record during implementation: `/tmp/goose-unit-final-*.txt`
- Record during implementation: `/tmp/goose-integration-final.txt`

**Step 1: Run the complete build and both boundaries**

```bash
dotnet build Goose.sln --no-restore
dotnet test Goose.sln --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore
```

Expected: all pass. Integration remains excluded from the solution.

**Step 2: Recheck configuration behavior**

Run focused tests for:

- settings loader precedence/copy/failure;
- server reference ownership;
- two-world isolation;
- `SetConfig` world-local mutation and failure-no-change;
- script resolution under conflicting data roots;
- fixture overlapping lifetimes.

Expected: all pass using real settings/world objects.

**Step 3: Measure the unit project**

Run one warm-up, then three `/usr/bin/time -f '%e'` measurements of:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore
```

Expected: median wall time below 5.0 seconds. Record all three values in `docs/testing.md` with date and environment note.

**Step 4: Verify no lost tests or stale boundaries**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --no-restore --list-tests
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-restore --list-tests
rg -n "GameWorldSettingsCollection|static GooseSettings Settings|GameWorld\.Settings|GooseWindowsService|System.ServiceProcess" Goose Goose.Tests Goose.IntegrationTests TestSupport --glob '*.cs' --glob '*.csx' --glob '*.csproj'
git diff --check
```

Expected: combined discovery is not below the Part 1 count; structural search returns no active matches; diff check passes.

**Step 5: Update documentation**

Update `docs/testing.md` with:

- final unit timing;
- the fact that worlds own settings and may be tested in parallel;
- any remaining narrow serialized collections and their exact shared resource;
- unchanged explicit integration command.

**Step 6: Commit**

```bash
git add docs/testing.md
git commit -m "docs: record isolated test performance"
```

## Part 2 completion checklist

- [ ] Configuration loading is separate from `GameWorld` construction.
- [ ] `GameServer` owns one non-null settings reference across restarts.
- [ ] Every `GameWorld` owns an explicit settings reference.
- [ ] Script handlers resolve paths from their owning world.
- [ ] Core, event, command, and script code has no static settings access.
- [ ] `SetConfig` mutates only the supplied world; `SaveConfig` remains a no-op.
- [ ] Test fixtures do not save, replace, or restore global settings.
- [ ] Broad settings collections are removed.
- [ ] Ten consecutive unit runs pass with default parallelization.
- [ ] Unit median wall time remains below five seconds.
- [ ] Integration tests pass and remain outside `Goose.sln`.
