# Quest Icons Implementation Plan

**Goal:** Display configurable, viewer-specific available and ready quest icons above NPCs through a reusable sheet/graphic character-icon protocol.

**Architecture:** The server sends `CHI<loginId>,<sheet>,<graphic>` after NPC publication and at quest-relevant mutation boundaries. Shared quest predicates compute ready/available/none without caching derived state. The Godot client resolves the graphic through its existing `SpriteCache` and renders one native-size world-space `Sprite2D` on the target character.

**Tech Stack:** C#/.NET 10, xUnit, Godot 4 C#, Goose text protocol

---

## Workspaces and verified APIs

Server worktree: `/home/agent/workspace/illutiagooseserver/.worktrees/quest-icons`

Client worktree: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons`

Design: `docs/plans/2026-09-22-quest-icons-design.md`

Verified server boundaries:

- `ICharacter.LoginID` supplies the generic protocol target: `Goose/ICharacter.cs:11-16`.
- `P.MakeNPCCharacter` is the existing NPC publication packet: `Goose/Packets.cs:200-226`.
- `GameWorld.Send(Player, string)` frames and sends one packet: `Goose/GameWorld.cs:614-640`.
- Visible, alive NPC selection is `Map.GetNPCsInRange`: `Goose/Map.cs:165-177`.
- Quest candidate filtering currently lives in `QuestWindow.GetAvailableQuests`: `Goose/Quests/QuestWindow.cs:97-119`.
- Quest requirement evaluation, including `IQuestScript.IsMet`, currently lives in `QuestWindow.PlayerMeetsRequirements`: `Goose/Quests/QuestWindow.cs:306-351`.
- Script requirement API: `Goose/Scripting/IQuestScript.cs:14-19`.
- Captured server packets are available through `TestWorldFixture.CapturingPlayer.Sent`: `TestSupport/TestWorldFixture.cs:72-105`.
- Runtime `/setconfig` refresh can enumerate the published online-player list through `PlayerHandler.Players`: `Goose/PlayerHandler.cs:157-164`.

Verified client boundaries:

- Packet registration/removal is paired in `MapManager._Ready` and `_ExitTree`: `Scripts/MapManager.cs:94-155`.
- Character publication and replacement occur in `MapManager.OnMakeCharacter`: `Scripts/MapManager.cs:168-180`.
- Character removal occurs in `MapManager.OnEraseCharacter`: `Scripts/MapManager.cs:228-236`.
- `SpriteCache.Get(sheet, graphic)` caches `AtlasTexture` regions and returns null for unavailable graphics: `Scripts/Map/SpriteCache.cs:14-38`.
- Character overlay layout converges through `Character.RepositionOverlays`: `Scripts/Character/Character.cs:170-188`.
- The name label top is based on `Height` plus `NameTopOffset`: `Scripts/Character/Character.cs:146-178` and `Scripts/Overlays/BridgedNameLabel.cs:37-42`.
- Existing centered-sprite parity behavior is in `CharacterAnchor.SpriteOffset`: `Scripts/Character/CharacterAnchor.cs:14-17`.
- Characters/ordinary objects use Z 15 while foreground/roof layers use Z 30/40: `Scenes/Map.tscn:10-24` and `Scripts/Map/MapLayer.cs:16-26`.
- Absolute Z avoids adding the parent's Z, as demonstrated by spell overlays: `Scripts/MapManager.cs:368-376`.

No persistence or schema changes are required. The selected 32x32 graphic exists at `(2276, 332038)`, and sheet 2276 is already in `tools/SpriteBundle/sheets.json:4-8`.

### Task 1: Add server configuration and the generic CHI protocol

**Files:**
- Modify: `Goose/GooseSettings.cs:135-155`
- Modify: `Goose/GooseSettings.json:154-160`
- Modify: `Goose/GooseSettingsLoader.cs:52-71`
- Modify: `Goose/Commands/SetConfigCommand.cs:22-57`
- Modify: `Goose/Packets.cs:83-89`
- Modify: `Goose/GameWorld.cs:614-640`
- Create: `Goose.Tests/CharacterIconPacketTests.cs`
- Modify: `Goose.Tests/GooseSettingsLoaderTests.cs`
- Modify or create focused tests beside: `Goose.Tests/SetConfigCommandTests.cs`

**Mutation impact:**
- Source of truth changed: the four icon IDs in `GooseSettings`; runtime settings remain canonical.
- Important readers: the quest icon sender introduced in Task 2 and `/setconfig` reflection mutation in `Goose/Commands/SetConfigCommand.cs:22-57`.
- Derived/cached state affected: no cache; visible client icons need a later explicit quest-icon refresh when runtime values change.
- Required propagation sequence:
  1. Deserialize settings.
  2. Validate each `(sheet, graphic)` pair before publishing the `GameWorld`.
  3. For `/setconfig`, parse the candidate, apply it tentatively, validate, and restore the previous value on failure.
  4. Task 2 completes propagation by making a successful icon-setting change call the new refresh operation for ready players in `PlayerHandler.Players`.
- Invariants to preserve:
  - `(0,0)` is disabled.
  - `sheet > 0 && graphic >= 0` is valid.
  - Negative values and `sheet == 0 && graphic != 0` are rejected.
  - Existing JSON files missing these numeric fields receive initialized defaults.
  - Failed runtime changes leave settings and visible icons unchanged.
- Observable proof required: tests assert the final setting value and emitted `CHI`, not merely a validation helper result.

**Step 1: Write failing configuration and packet tests**

Add tests for:

- `P.CharacterIcon(character, 2276, 332038) == "CHI<id>,2276,332038"`.
- clear format `CHI<id>,0,0`.
- missing JSON properties default to `2276/332038` for both states.
- valid disabled and positive-sheet/zero-graphic pairs load.
- each invalid pair throws `FatalStartupException` with the setting pair named.
- `/setconfig` rejects and rolls back an invalid icon setting.

Use the temporary settings-file fixture in `Goose.Tests/GooseSettingsLoaderTests.cs:8-34,62-100` and the real command dispatch pattern used by other command tests.

**Step 2: Run focused tests to verify red**

```bash
cd /home/agent/workspace/illutiagooseserver/.worktrees/quest-icons
dotnet test Goose.Tests/Goose.Tests.csproj --filter 'FullyQualifiedName~CharacterIconPacketTests|FullyQualifiedName~GooseSettingsLoaderTests|FullyQualifiedName~SetConfigCommandTests'
```

Expected: FAIL because the settings and `CHI` factory do not exist.

**Step 3: Implement the settings contract and generic packet API**

Add initialized properties and shipped JSON values:

```csharp
public int QuestAvailableIconSheet { get; set; } = 2276;
public int QuestAvailableIconGraphic { get; set; } = 332038;
public int QuestReadyIconSheet { get; set; } = 2276;
public int QuestReadyIconGraphic { get; set; } = 332038;
```

Add one shared pair validator used by startup and `/setconfig`. Add:

```csharp
P.CharacterIcon(ICharacter character, int sheet, int graphic)
GameWorld.SendCharacterIcon(Player viewer, ICharacter character, int sheet, int graphic)
```

The `GameWorld` API only sends; it does not store icon state, infer quest state, or suppress duplicate packets.

Task 1 leaves successful runtime icon-setting propagation for Task 2, where the quest refresh API exists. Invalid changes must already roll back here.

**Step 4: Run focused tests to verify green**

Run the command from Step 2. Expected: PASS.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Wire format is exact and clear is explicit | `CharacterIconPacketTests` formatting cases |
| Missing old config receives shipped defaults | loader missing-fields regression test |
| Invalid runtime setting cannot become observable | `/setconfig` rollback test |
| Positive sheet with graphic zero stays valid | loader valid-pair theory |

**Step 5: Commit**

```bash
git add Goose/GooseSettings.cs Goose/GooseSettings.json Goose/GooseSettingsLoader.cs Goose/Commands/SetConfigCommand.cs Goose/Packets.cs Goose/GameWorld.cs Goose.Tests
git commit -m "feat: add generic character icon protocol"
```

### Task 2: Extract shared quest predicates and resolve NPC icon state

**Files:**
- Create: `Goose/Quests/QuestStateResolver.cs`
- Modify: `Goose/Quests/QuestHandler.cs:1-75`
- Modify: `Goose/Quests/QuestWindow.cs:33-39,79-138,306-389`
- Modify: `Goose/Commands/SetConfigCommand.cs:22-57`
- Create: `Goose.Tests/QuestIconResolverTests.cs`
- Modify as needed: `TestSupport/TestWorldFixture.cs:53-105`

**Mutation impact:**
- Source of truth changed: none; `Player.QuestsStarted`, `QuestsCompleted`, `QuestProgress`, inventory, and player attributes remain canonical.
- Important readers: quest-window listing/completion and the new icon sender.
- Derived/cached state affected: no derived state is stored. The resolver computes from canonical lists and requirement values each call.
- Required propagation sequence:
  1. Resolve ready quests first from active quest state.
  2. Evaluate all requirements, including `IQuestScript.IsMet`.
  3. If none are ready, resolve an inactive available quest.
  4. Map ready/available/none to configured IDs and send `CHI`, including `0,0` for none.
- Invariants to preserve:
  - Ready takes priority over available.
  - Reward inventory/spellbook/script gates are not readiness requirements.
  - Completed non-repeatable quests are unavailable.
  - Completed repeatable quests are available again only when inactive.
  - Existing low-level quest-window explanatory behavior is not accidentally removed.
- Observable proof required: tests use real quest/player/NPC objects and assert the resolved state and final packet. A runtime icon-setting test asserts existing visible players receive the newly configured payload.

**Step 1: Write failing resolver tests**

Create real-domain tests covering:

- no quests -> none/clear;
- inactive eligible quest -> available;
- started incomplete quest -> none;
- started complete quest -> ready;
- one available plus one ready -> ready;
- completed non-repeatable -> none;
- completed repeatable inactive -> available;
- class, minimum/maximum level, minimum/maximum experience, and prerequisite failures -> none;
- built-in item/gold/kill/talk/experience/nothing-equipped requirements;
- scripted `IsMet` true/false using `Goose.Tests/Fixtures/QuestScriptFixture.cs:6-33`;
- full inventory or spellbook and blocking reward scripts do not suppress ready;
- a successful `/setconfig` icon change refreshes ready players from `PlayerHandler.Players` and emits the new payload.

Include an adversarial test where a quest is both present in historical completion and still present in `QuestsStarted`; non-repeatable completion must not produce ready or available.

**Step 2: Run focused tests to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestIconResolverTests
```

Expected: FAIL because `QuestStateResolver` and icon send/refresh APIs do not exist.

**Step 3: Implement shared predicates**

Give `QuestStateResolver` explicit, side-effect-free contracts except for invoking configured script predicates:

```csharp
internal static bool IsActive(Quest quest, Player player)
internal static bool MeetsStartRequirements(Quest quest, Player player)
internal static bool IsAvailable(Quest quest, Player player)
internal static bool MeetsRequirements(Quest quest, Player player, GameWorld world)
internal static QuestIconState Resolve(NPC npc, Player player, GameWorld world)
```

`IsAvailable` must require inactivity and both minimum and maximum gates. Preserve the current UI's ability to show the “not right level” dialog by reusing atomic predicates in `QuestWindow` rather than blindly replacing `GetAvailableQuests` with the stricter icon predicate. Move requirement switch logic out of the window and have completion delegate to `MeetsRequirements`.

Add to `QuestHandler`:

```csharp
public void SendIcon(Player viewer, NPC npc, GameWorld world)
public void RefreshIcons(Player viewer, GameWorld world)
```

`RefreshIcons` must return safely when the player is not ready or has no map; otherwise it iterates `viewer.Map.GetNPCsInRange(viewer)` and sends one resolved packet per NPC. `SendIcon` always sends a packet, including clear, so stale client state cannot survive. Complete Task 1's runtime-setting propagation in `SetConfigCommand`: after a valid icon-property change, iterate `world.PlayerHandler.Players` and refresh ready players.

**Step 4: Run resolver and existing quest tests**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter 'FullyQualifiedName~QuestIconResolverTests|FullyQualifiedName~QuestOptionListTests|FullyQualifiedName~QuestCompletionTests|FullyQualifiedName~QuestWindowScriptTests|FullyQualifiedName~QuestNotRightLevelTests'
```

Expected: PASS with unchanged quest-window behavior.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Ready dominates available | mixed-NPC priority test |
| Reward delivery gates do not hide ready | full-space/blocking-reward tests |
| Script requirements use the production predicate | compiled-script true/false tests |
| Historical completed/start-list overlap cannot resurrect non-repeatable quest | adversarial overlap test |
| No qualifying state actively clears stale client state | captured `CHI<id>,0,0` assertion |

**Step 5: Commit**

```bash
git add Goose/Quests/QuestStateResolver.cs Goose/Quests/QuestHandler.cs Goose/Quests/QuestWindow.cs Goose/Commands/SetConfigCommand.cs Goose.Tests/QuestIconResolverTests.cs TestSupport
git commit -m "feat: resolve viewer-specific quest icons"
```

### Task 3: Publish initial icons after every NPC MKC path

**Files:**
- Modify: `Goose/NPC.cs:529-546,726-734`
- Modify: `Goose/Player.cs:1314-1318,1426-1432`
- Modify: `Goose/Events/DoneLoadingMapEvent.cs:78-85`
- Create: `Goose.Tests/QuestIconVisibilityTests.cs`

**Mutation impact:**
- Source of truth changed: none; this changes the packet publication sequence observed by one client.
- Important readers: the client character registry must observe `MKC` before `CHI`.
- Derived/cached state affected: client icon state is created only after character publication; no server cache exists.
- Required propagation sequence at each of five sites:
  1. Send viewer-specific NPC `MKC`.
  2. Call `QuestHandler.SendIcon(viewer, npc, world)` for that same viewer.
  3. Continue aggro/movement behavior unchanged.
- Invariants to preserve:
  - Never precompute one icon for all viewers in NPC movement/spawn loops.
  - `MKC` precedes `CHI` for each viewer.
  - GM invisibility and existing aggro behavior remain unchanged.
- Observable proof required: captured packet order, not only a call-count assertion.

**Step 1: Write failing ordering tests**

Cover at least:

- initial map load,
- player movement into NPC range,
- NPC spawn or movement into player range,
- same-map warp visibility rebuild.

Use `TestWorldFixture.CommandPlayerOn`, a real map/NPC, and `CapturingPlayer.Sent`. Assert the first matching `MKC<npcId>` index is lower than the first `CHI<npcId>` index and that different viewers can receive different icon payloads for the same NPC.

**Step 2: Run focused tests to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestIconVisibilityTests
```

Expected: FAIL because no visibility path sends `CHI`.

**Step 3: Add viewer-specific sends to all five publication sites**

Keep `P.MakeNPCCharacter(npc)` reuse where it already exists, but resolve/send icons inside each viewer loop. Do not append icon data to `MKC`.

**Step 4: Run focused tests and NPC/map regressions**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter 'FullyQualifiedName~QuestIconVisibilityTests|FullyQualifiedName~NPCSpawnRegistrationTests|FullyQualifiedName~MapTransitionEventGuardTests'
```

Expected: PASS.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Client never receives known-icon state before target publication | packet-index assertions |
| Same NPC can differ by viewer | two-player captured payload test |
| Every production NPC publication path is wired | one test per path or a cited path-coverage theory |

**Step 5: Commit**

```bash
git add Goose/NPC.cs Goose/Player.cs Goose/Events/DoneLoadingMapEvent.cs Goose.Tests/QuestIconVisibilityTests.cs
git commit -m "feat: send quest icons when NPCs enter view"
```

### Task 4: Refresh icons after quest lifecycle mutations

**Files:**
- Modify: `Goose/Quests/QuestWindow.cs:58-72,121-138,391-409`
- Modify: `Goose/Quests/AbandonConfirmWindow.cs:41-49`
- Modify: `Goose/Player.cs:1202-1225`
- Create: `Goose.Tests/QuestIconRefreshTests.cs`

**Mutation impact:**
- Source of truth changed: `QuestsStarted`, `QuestsCompleted`, and `QuestProgress` in `Player`.
- Important readers: quest windows, persistence in `Player.BuildSaveQuests` (`Goose/Player.cs:1227-1246`), and `QuestStateResolver`.
- Derived/cached state affected: only the viewer's current client icon; no server cache.
- Required propagation sequence:
  1. Complete the quest-list/progress mutation.
  2. Complete requirement consumption and reward delivery where applicable.
  3. Call `QuestHandler.RefreshIcons(player, world)` once at the outer operation boundary.
  4. For kill/talk, refresh only if at least one progress value actually changed.
- Invariants to preserve:
  - Acceptance creates progress before resolving icons.
  - Completion resolves after rewards, allowing newly unlocked prerequisite quests to appear.
  - Abandonment resolves after both started and progress entries are removed.
  - A nonmatching kill/talk does not scan or send icons.
- Observable proof required: tests assert final canonical quest state and final `CHI` payload.

**Step 1: Write failing transition tests**

Test visible NPC transitions:

- available -> no icon after acceptance with incomplete requirements;
- incomplete -> ready after matching kill/talk reaches its target;
- ready -> available/none after completion, including a newly unlocked follow-up quest;
- active -> available after abandonment;
- unrelated kill/talk emits no quest-icon refresh.

**Step 2: Run focused tests to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestIconRefreshTests
```

Expected: FAIL because lifecycle mutations do not publish icon changes.

**Step 3: Wire refreshes at completed mutation boundaries**

Change `StartQuest` to accept `GameWorld` or make its callers perform the refresh only after progress initialization. Completion refresh belongs after `TakeRequirements` and `GiveRewards`. Abandon refresh belongs inside the successful mutation branch. Track a local `changed` flag in `UpdatePossibleQuestProgress` and refresh once after iteration.

The public `QuestHandler.RefreshIcons` is the script-facing explicit refresh API for custom `IsMet` dependencies.

**Step 4: Run lifecycle and persistence regressions**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter 'FullyQualifiedName~QuestIconRefreshTests|FullyQualifiedName~QuestCompletionTests|FullyQualifiedName~QuestAbandonCommandTests|FullyQualifiedName~QuestScriptTests'
```

Expected: PASS.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Refresh observes fully initialized acceptance state | acceptance transition test |
| Completion can reveal prerequisite-dependent follow-up | chained-quest test |
| Unrelated progress does not produce traffic | packet-count regression test |
| Persisted quest collections remain canonical | existing save/completion tests plus state assertions |

**Step 5: Commit**

```bash
git add Goose/Quests/QuestWindow.cs Goose/Quests/AbandonConfirmWindow.cs Goose/Player.cs Goose.Tests/QuestIconRefreshTests.cs
git commit -m "feat: refresh icons on quest transitions"
```

### Task 5: Refresh icons after standard requirement-affecting mutations

**Files:**
- Modify: `Goose/Inventory.cs:78-103,302-361,456-584`
- Modify: `Goose/ItemContainerWindow.cs:29-59`
- Modify: `Goose/Player.cs:1482-1534,1611-1632,1796-1932`
- Modify direct bypasses where required: `Goose/Commands/ChangeClassCommand.cs:32-68`, `Goose/Commands/BuyVitaCommand.cs:23-48`, `Goose/Commands/BuyManaCommand.cs:23-48`, `Goose/Commands/GiveExperienceCommand.cs:17-26`, `Goose/Commands/MacroConfirmCommand.cs:23-31`, `Goose/Commands/HairdyeCommand.cs:20-44`, `Goose/CustomWindow.cs:166-177`
- Create: `Goose.Tests/QuestIconMutationRefreshTests.cs`

**Mutation impact:**
- Source of truth changed: inventory/equipped slots, gold, experience/level, class, and experience sold.
- Important readers: `QuestStateResolver.MeetsRequirements` and `IsAvailable`, normal status/inventory packets, stats, and persistence.
- Derived/cached state affected: current client icon only; no server quest-state cache.
- Required propagation sequence:
  1. Finish the domain transaction and its existing status/inventory/stat packets.
  2. Call `QuestHandler.RefreshIcons` against the final state.
  3. For compound equip/unequip, suppress nested inventory refreshes or use private core operations so no transient icon is published between inventory removal and equipped-slot publication.
  4. For inventory/container transfers that use `SetSlot` directly, refresh after both sides of the transfer are finalized.
  5. Direct property-mutating commands either route through canonical player methods or explicitly refresh after their final mutation.
- Invariants to preserve:
  - Failed/no-op mutations do not refresh.
  - Compound equipment changes expose only final state.
  - Offline/mapless players safely no-op.
  - Existing status, inventory, stat, level, and class side effects remain ordered and intact.
- Observable proof required: captured final icon transitions using real inventory/player mutations, including an adversarial no-transient-packet test for equipment.

**Step 1: Write failing mutation propagation tests**

Cover:

- item add/remove crossing an item requirement;
- equip/unequip crossing `NothingEquipped`, asserting no intermediate incorrect `CHI`;
- inventory-bank/container transfer crossing an item requirement;
- gold add/remove crossing a threshold;
- experience gain and level-up crossing minimum availability/readiness;
- class change crossing restrictions;
- experience-sold mutation crossing a requirement;
- failed removal or rejected mutation sends no icon refresh;
- mapless player mutation does not throw.

**Step 2: Run focused tests to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~QuestIconMutationRefreshTests
```

Expected: FAIL because standard mutations do not publish quest icons.

**Step 3: Add final-state refresh propagation**

Prefer canonical mutation methods over duplicating command-specific state changes. Where direct mutations cannot be removed safely in scope, add an explicit refresh after the complete operation. Keep batching private to `Inventory`; it is notification coalescing, not a quest-state cache.

Do not refresh from generic `SetSlot` itself because callers often perform multi-slot transactions. Refresh at the outer transfer methods after both canonical collections are final.

**Step 4: Run focused and subsystem regressions**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter 'FullyQualifiedName~QuestIconMutationRefreshTests|FullyQualifiedName~Inventory|FullyQualifiedName~Quest|FullyQualifiedName~ChangeClass|FullyQualifiedName~DoubleRoundTrip'
```

Expected: PASS.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Final inventory/equipment state and icon agree | item and nothing-equipped transition tests |
| Compound equip cannot expose transient ready state | adversarial packet-sequence test |
| Failed mutations produce no icon traffic | rejected-operation tests |
| Level/class/experience readers see final values | threshold transition tests using real methods |

**Step 5: Commit**

```bash
git add Goose/Inventory.cs Goose/ItemContainerWindow.cs Goose/Player.cs Goose/Commands Goose/CustomWindow.cs Goose.Tests/QuestIconMutationRefreshTests.cs
git commit -m "feat: refresh quest icons after player state changes"
```

### Task 6: Parse and dispatch CHI in the Godot client

**Files:**
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/Scripts/Network/Packets/CharacterIconPacket.cs`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/Scripts/MapManager.cs:94-155,168-180`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/tests/Goose2Client.Tests/CharacterIconPacketTests.cs`

**Mutation impact:**
- Source of truth changed: the target character's one current icon texture, owned by `Character` after Task 7.
- Important readers: rendering only; no persistence or model serialization reads it.
- Derived/cached state affected: `SpriteCache` may cache the requested atlas texture. No new cache is added.
- Required propagation sequence:
  1. Parse all three integers.
  2. Look up the character first.
  3. If absent, return without resolving/loading the sheet.
  4. Resolve through the map's existing `_cache`.
  5. Pass the resulting texture, including null, to `Character.SetIcon` from Task 7.
- Invariants to preserve:
  - Unknown IDs are ignored rather than queued.
  - Positive sheet with graphic zero is passed through.
  - `0,0`, missing manifest entries, and missing sheets all clear safely.
  - Listener registration/removal remains paired under `_listenersRegistered`.
- Observable proof required: parser tests plus Task 7 runtime dispatch proof.

**Step 1: Write failing parser tests**

Following `tests/Goose2Client.Tests/CharacterPacketInvisibleTests.cs:9-26`, parse:

- `CHI42,2276,332038`;
- `CHI42,0,0`;
- `CHI42,2276,0`.

Assert all three fields exactly.

**Step 2: Run parser tests to verify red**

```bash
cd /home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter FullyQualifiedName~CharacterIconPacketTests
```

Expected: FAIL because the packet type does not exist.

**Step 3: Implement parser and paired listener wiring**

Use the simple `PacketHandler` pattern in `Scripts/Network/Packets/EraseCharacterPacket.cs:6-18`. Add `Listen<CharacterIconPacket>` and matching `Remove<CharacterIconPacket>`. The handler must perform character lookup before `_cache.Get`.

Task 6 may temporarily call a Task 7 stub only within the same uncommitted client sequence; do not commit uncompilable code. It is acceptable to group Tasks 6 and 7 into one client commit if needed, while retaining separate review/test checkpoints.

**Step 4: Run parser tests**

Run Step 2. Expected: PASS.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Protocol fields preserve exact numeric IDs | positive parser case |
| Clear is represented without a special parser branch | `0,0` parser case |
| Graphic zero remains legal with a positive sheet | positive-sheet/zero-graphic case |

**Step 5: Commit or group with Task 7**

```bash
git add Scripts/Network/Packets/CharacterIconPacket.cs Scripts/MapManager.cs tests/Goose2Client.Tests/CharacterIconPacketTests.cs
git commit -m "feat: parse character icon packets"
```

### Task 7: Render one native-size world-space icon per character

**Files:**
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/Scripts/Character/Character.cs:40-57,170-188,212-270`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/Scripts/Character/CharacterAnchor.cs:6-17`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/tests/Goose2Client.Tests/CharacterAnchorTests.cs`
- Create or extend runtime self-test: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/Scripts/CharacterIconSelfTest.cs`
- Modify self-test dispatch: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/Scripts/GameManager.cs:214-219`
- Create runner: `/home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons/tools/tests/run_character_icon.sh`

**Mutation impact:**
- Source of truth changed: a private `Sprite2D` child and its texture on each live `Character`.
- Important readers: Godot rendering and `RepositionOverlays`.
- Derived/cached state affected: icon position derives from resting `Height`, texture dimensions, `NameTopOffset`, and a 2-world-pixel gap.
- Required propagation sequence:
  1. On a non-null texture, create the sprite once if absent, then replace its texture.
  2. Set `TextureFilter = Nearest`, `ZIndex = 20`, `ZAsRelative = false`, native scale, and no tint.
  3. Recompute position immediately.
  4. On null, clear/hide immediately without queue-free/recreate overlap.
  5. Existing `RepositionOverlays` calls after appearance/mount changes recompute the icon position.
  6. Parent `QueueFree` owns teardown; replacement `MKC` receives no transferred icon and waits for the following `CHI`.
- Invariants to preserve:
  - Exactly one icon sprite per character.
  - Texture replacement does not allocate another sprite.
  - Native dimensions and nearest filtering are retained.
  - The bottom edge is two world pixels above `-(bodyHeight + NameTopOffset)`.
  - Absolute Z 20 remains below foreground/roof layers.
- Observable proof required: pure position tests plus a Godot-hosted runtime test. Plain xUnit must not instantiate `Sprite2D` because Godot objects require an engine host.

**Step 1: Write failing pure layout tests**

Add `CharacterAnchor.IconPosition(bodyHeight, textureSize, nameTopOffset, gap)` or an equally explicit helper. Cover:

- the configured 32x32 icon at normal body height;
- odd width/height parity with integer texture corners;
- body-height changes;
- the 48px fallback used when `Height <= 0`.

Expected formula for centered textures:

```text
x = (width mod 2) * 0.5
y = -(bodyHeight + NameTopOffset + gap + height / 2)
```

**Step 2: Run pure tests to verify red**

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter FullyQualifiedName~CharacterAnchorTests
```

Expected: FAIL because icon positioning does not exist.

**Step 3: Implement character-owned icon rendering**

Add `Character.SetIcon(Texture2D? texture)` and call positioning from `RepositionOverlays`. Reuse the same node for replacements. Null must leave no visible stale texture.

Do not register the icon with `WorldTextBridge`; it must scale with the character/world.

**Step 4: Add a Godot-hosted runtime self-test**

Follow the argument-gated pattern in `Scripts/UiScaleSelfTest.cs:8-28`, `Scripts/GameManager.cs:214-219`, and `tools/tests/run_ui_scale.sh:1-7`. Use generated `ImageTexture` instances so the test does not depend on ignored generated sprite assets. Verify:

- first application creates one child;
- replacement reuses it and changes texture;
- clear removes visible texture immediately;
- nearest filter, absolute Z 20, and native scale;
- height relayout changes position;
- parent removal frees the child after advancing a frame.

Also exercise `MapManager` dispatch for unknown character and clear where practical in the engine-hosted harness.

**Step 5: Run client tests**

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter 'FullyQualifiedName~CharacterAnchorTests|FullyQualifiedName~CharacterIconPacketTests'
bash tools/tests/run_character_icon.sh
```

Expected: all PASS.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Icon scales as world pixel art, not UI text | native scale and nearest-filter runtime assertions |
| One active icon survives replacement without duplication | child identity/count runtime assertion |
| Clear cannot leave a one-frame stale sprite | immediate texture/visibility assertion |
| Layout remains above the name anchor across heights | pure theory plus runtime relayout |
| Erase/replacement teardown cannot leak icon nodes | frame-advanced parent cleanup assertion |

**Step 6: Commit**

```bash
git add Scripts/Character Scripts/CharacterIconSelfTest.cs Scripts/GameManager.cs Scripts/MapManager.cs Scripts/Network/Packets/CharacterIconPacket.cs tests/Goose2Client.Tests tools/tests/run_character_icon.sh
git commit -m "feat: render character icons in world space"
```

### Task 8: Cross-repository regression and manual protocol verification

**Files:**
- Modify only if findings require corrections: files from Tasks 1-7
- Review: `docs/plans/2026-09-22-quest-icons-design.md`

**Mutation impact:**
- Source of truth changed: none unless a regression fix is required.
- Important readers: complete server-to-client protocol flow.
- Derived/cached state affected: client `SpriteCache` only; no quest-state cache.
- Required propagation sequence under test:
  1. Server publishes NPC with `MKC`.
  2. Server resolves per-viewer quest state and sends `CHI`.
  3. Client publishes character, resolves cached atlas texture, and applies one sprite.
  4. A quest/player mutation completes, then server sends replacement/clear `CHI`.
  5. Client replaces/clears without recreating the character.
- Invariants to preserve: every approved design promise is represented by automated proof or the manual checklist below.
- Observable proof required: full suites plus packet/render smoke verification.

**Step 1: Run the full server suite**

```bash
cd /home/agent/workspace/illutiagooseserver/.worktrees/quest-icons
dotnet test Goose.sln
```

Expected baseline-compatible result: 0 failures. Baseline was 941 unit, 284 integration, and 163 tools tests, with environment-dependent tools tests possibly skipped when sibling client assets are unavailable.

**Step 2: Run the full client suite and engine self-test**

```bash
cd /home/agent/workspace/Goose2ClientGodot/.worktrees/quest-icons
dotnet test Goose2ClientGodot.sln
bash tools/tests/run_character_icon.sh
```

Expected baseline-compatible result: 0 failures. Baseline client solution totals included 664 `Goose2Client.Tests` and the existing map-editor suites.

**Step 3: Review protocol and mutation coverage adversarially**

Search all production NPC publication and canonical quest-state mutation sites again:

```bash
cd /home/agent/workspace/illutiagooseserver/.worktrees/quest-icons
grep -R 'P.MakeNPCCharacter' -n Goose --include='*.cs'
grep -R 'QuestsStarted\|QuestsCompleted\|QuestProgress' -n Goose --include='*.cs'
```

Every NPC publication must be followed by viewer-specific icon publication. Every canonical mutation must either refresh at its completed outer boundary or be documented as requiring the public script refresh API.

**Step 4: Manual Godot smoke test**

With server and client pointed at the same data:

1. Approach an NPC with an eligible inactive quest: icon `(2276,332038)` appears above the name and scales with the character.
2. Accept an incomplete quest: icon clears unless another quest qualifies.
3. Fulfill the final requirement while NPC remains visible: icon appears immediately.
4. Complete the quest: icon clears or changes according to another attached quest.
5. Abandon and repeat the quest where applicable.
6. Verify two players with different quest state see different icons on the same NPC.
7. Exercise inventory, equipment, gold, experience/level, and class threshold changes.
8. Send unknown/invalid `CHI`, duplicate `MKC`, and `ERC`; confirm no crash, stale icon, or leaked sprite.
9. Walk under foreground/roof tiles and toggle invisibility; confirm Z and parent visibility behavior.

**Step 5: Final design-alignment review**

Confirm:

- packet is exactly `CHI<loginId>,<sheet>,<graphic>`;
- `0,0` clears;
- initial `MKC` precedes `CHI`;
- ready dominates available;
- reward delivery gates do not hide ready;
- both shipped states use `(2276,332038)`;
- no server quest-state cache or packet suppression was introduced;
- icon is a native-size world child at absolute Z 20;
- no database/spreadsheet migration or sprite bundle edit was added.

**Step 6: Commit any final test-only or integration corrections in the owning repository**

Use a focused message such as:

```bash
git commit -am "test: verify quest icon integration"
```
