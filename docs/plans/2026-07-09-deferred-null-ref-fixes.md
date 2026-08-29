# Deferred Null Reference Bug Fixes — Implementation Plan

**Goal:** Fix the 19 latent null-reference bugs deferred in `docs/plans/2026-08-25-nullable-inventory.md` ("Latent bugs (deferred)" section), plus 2 of the 3 additional findings from the 2026-07-09 code sweep (null `ItemSlot.Item` in serialized blobs; null `Allies`/`Quests`/`EquippedItems` on script-registered templates). The third finding — `RenewBuff` not resyncing `BuffExpireEvent` when `BuffStacksOver` swaps effects (a buff first applied with duration 0 becomes permanent if renewed with a duration>0 effect) — is **behavioral, not an NRE**, and is recorded as a new deferred item in Task 8's doc pass rather than fixed here.

**Architecture:** The NRT annotation plan is complete (zero warnings); this plan changes behavior, not annotations. Every fix is one of four shapes: (a) a runtime guard that degrades gracefully (log + skip/reject/fall back), (b) load-time validation that turns a deferred NRE into a visible load error, (c) normalization at a registration boundary (script-registered templates), or (d) an execution-time guard at a single event-processing chokepoint. No control-flow redesign, no schema changes, no client-protocol changes.

**Tech Stack:** C# / .NET (Goose server), xUnit (`Goose.Tests` fast suite, `Goose.IntegrationTests` DB-backed suite, shared `TestSupport/TestWorldFixture.cs`), SQLite via the single-connection `Database` worker.

---

## Background and severity model

An NRE in an event handler does **not** crash the server: `EventHandler.Update` wraps every
`ev.Ready(world)` in try/catch (Goose/EventHandler.cs:361-382 — logs, drops the event, loop
continues), and the socket read/accept paths in `GameServer.GameLoop` are wrapped too.
The real cost is **silent per-incident failure**: lost kill rewards, a warp that does
nothing, and worst-case a player stuck in `LoadingMap` forever (a `DoneLoadingMapEvent`
NRE means `Player.State` never becomes `Ready` — unplayable until relog, only a log line).

Bugs #6 and #12 from the deferred list are **already closed** in current code (verified
2026-07-09): `NPCAttackEvent.Ready` guards `AggroTarget is null`
(Goose/Events/NPCAttackEvent.cs:11-12), and every `Event.NPC` consumer either guards or is
safe by construction. Task 3 updates the inventory doc for both; no code change.

Bug #4 (empty damages dictionary on NPC kill) is **currently unreachable**: the killer's
`AddAggro` (Goose/NPC.cs:1064-1105, always maintains `AggroTargetToValue`) runs earlier in
the same `Damage` call as the death check (Goose/NPC.cs:1102), single-threaded. The guard
is still added as defense against future death paths (scripts, GM commands).

## Scope decisions (do NOT do these)

- Do **not** make `Player.Map`, `Event.Player`, `Event.NPC`, `Event.Data` nullable — the
  ~169/~572/~29/~78 unguarded deref fan-out was explicitly rejected in the annotation plan.
  Task 8 closes the `Player.Map` transition window at the single event-execution chokepoint
  instead of touching call sites.
- Do **not** change the duration-0-buff-is-permanent semantics (no `BuffExpireEvent` is
  created when `Duration == 0`); Task 1 only stops the NRE for tick-type effects.
- Do **not** change `Class.MaxLevel => levels.Count` (Goose/Class.cs:25) — it is only
  correct for contiguous level sets, which Task 7 now validates at load with a logged error.
- Do **not** add `<WarningsAsErrors>nullable</WarningsAsErrors>` (deliberately deferred by
  the annotation plan).
- Do **not** fix the `RenewBuff`/`BuffExpireEvent` resync issue (behavioral; recorded in
  the doc pass).

## APIs verified

Citations for load-bearing APIs (line numbers at HEAD `81d5d89`):

- `EventHandler.Update(GameWorld)` — Goose/EventHandler.cs:361-382. Drains **all** events
  with `tick <= now` in one call (`while (this.events.TryPeek(...))` loop), each `Ready`
  wrapped in try/catch. The queue is a `PriorityQueue<Event, long>` (no efficient removal) —
  this is why Task 8 guards at execution time rather than purging the queue.
- `EventHandler.AddEvent(Player, string) → bool` — Goose/EventHandler.cs:283-315. Only
  client-packet entry point (`GameWorld.ParseData` → Goose/GameWorld.cs:279-288). Builds the
  event via `definition.EventFactory(player, packet)` or `GetOrCreateFactory(...)(...)` +
  `e.Player = player; e.Data = packet;` (:297-309), then `AddEvent(Event)` (:312). Command
  table: `"LCNT"` :125, `"DLM"` :126, `"PONG"` :155. `internal int Count` — :317.
- `Event` — Goose/Event.cs:8-28: no `Name` property; `Ticks`, `Player` (`Player`, `= null!`,
  null at runtime for NPC events), `Data`, `NPC`. Task 8's guard therefore uses type
  patterns (`ev is LoginContinuedEvent or PlayerPongEvent`) plus a `ClientOriginated` flag
  (new field, set only in `AddEvent(Player, string)`).
- **Internal events carry `Player` too** — the Task 8 filter must not apply to them:
  `BuffExpireEvent.Ready` (Goose/Events/BuffExpireEvent.cs:8-36) removes the buff at
  expiry and reschedules only when *not yet* expired — dropping an at-expiry event during
  map load makes the buff **permanent** (it is never re-queued). `BuffTickEvent.Ready`
  (Goose/Events/BuffTickEvent.cs:15-20) self-reschedules when the target player is not
  Ready, so a dropped tick is delayed, not lost. Both copy `Player`/`NPC` into their
  re-queued instances (BuffExpireEvent.cs:28-31, BuffTickEvent.cs:60-62).
- `Packets.ExpBar` — Goose/Packets.cs:29-48: derefs `GetLevel(player.Level)!` ×2 and
  `GetLevel(player.Level - 1)!` when `Level > 1` (:32/:41/:43).
- `InternalsVisibleTo` Goose.Tests + Goose.IntegrationTests — Goose/Goose.csproj:19-25.
- `Player.States { NotLoggedIn=0, LoadingGame, LoadingMap, Ready }` — Goose/Player.cs:60-66.
- `Player.Map` nulled in `WarpTo` — Goose/Player.cs:1366-1374; reassigned —
  Goose/Events/DoneLoadingMapEvent.cs:78. Warp is **inline**: `MoveEvent.Ready` calls
  `this.Player.WarpTo(world, warp.WarpMap, warp.WarpX, warp.WarpY)` directly (Goose/Events/MoveEvent.cs:117) —
  so a warp completes synchronously inside the first event's `Ready`.
- `MapHandler.LoadMaps` is two-pass: all maps registered (Goose/MapHandler.cs:67) before any
  `LoadData` (:74) — so `GetMap` null during tile load genuinely means unknown id.
- Warp tile resolution — Goose/Map.cs:547 (`GetMap(reader.GetInt32("warp_id"))!`).
- MoveEvent warp check — Goose/Events/MoveEvent.cs:111-124 (else branch already bounces the
  player back and sends `P.SetYourPosition`).
- `LoginContinuedEvent` `GetMap` — Goose/Events/LoginContinuedEvent.cs:21; MOTD deref :34.
- `DoneLoadingMapEvent` `GetMap` — Goose/Events/DoneLoadingMapEvent.cs:24; `State = Ready` :31.
- `Player.BoundMap` sites — Goose/Player.cs:569 (LoadFromAutoCreate), :701 (LoadFromReader), :1432 (ChangeClass).
- `NPC.AddBuff` tick NRE — Goose/NPC.cs:1523; `Player.AddBuff` — Goose/Player.cs:2194
  (both: `buff.BuffExpireEvent!.Ticks` after `if (Duration > 0)` creates the event).
- `OnMeleeHit/OnMeleeAttack` — Goose/NPC.cs:1696/1712, Goose/Player.cs:2487/2503
  (`OnMeleeHitSpell!.Cast` / `OnMeleeAttackSpell!.Cast`; properties are `SpellEffect?`,
  Goose/SpellEffect.cs:160-161, set from id lookup Goose/SpellHandler.cs:148-149).
- `CastScriptSpell` — Goose/SpellEffect.cs:1056-1068 (existing try/catch logs + returns
  false); dispatch `EffectTypes.Script => CastScriptSpell(…)` — Goose/SpellEffect.cs:1013.
- `P.SetYourPosition` = `"SUP"` + coords — Goose/Packets.cs:56.
- NPC kill block — Goose/NPC.cs:1102-1207: damages dict built :1131-1150; `highest` computed
  :1153-1160; **experience** :1162-1188 (`highest is Group` → `GainExperience`, else
  `((Player)highest!)…`); **buff removal** :1190-1199 (unconditional `RemoveBuff` loop —
  must stay unconditional); **drops** :1201-1205 (`DropItems` keyed on `highest`).
- `NPCHandler` quest list — Goose/NPCHandler.cs:109-111 (`QuestHandler.Get(q)!` into `NPC.Quests`).
- `QuestWindow.GetQuestProgressText` — Goose/Quests/QuestWindow.cs:217/222/227
  (`item!.Name`, `talkNPC!.Name`, `killNpc!.Name`).
- Vendor populate — Goose/Window.cs:153-163 (`P.ClearVendor()` then
  `for (int i = 1; i < this.NPC!.VendorItems!.Length; i++)` — already length-bounded; only
  the null deref needs guarding). Purchase — Goose/Events/VendorPurchaseInventoryEvent.cs:63
  (`npc.VendorItems![slotid]` — **no `slotid` bounds check today**, only a null-slot check
  at :66); the currency charge happens only after `AddItem` succeeds (:96-98), so a
  failed item load before the charge loses no gold.
- `GooseSettingsLoader.Load(string, string)` — Goose/GooseSettingsLoader.cs:21-55
  (Deserialize at :53-55). The 12 string props — Goose/GooseSettings.cs:6-10, :20, :24, :33, :75-77, :95.
- `NPCHandler.AddTemplate` — Goose/NPCHandler.cs:234-237 (bare dictionary insert, void).
  `ItemHandler.AddTemplate` — Goose/ItemHandler.cs:199-202 (same shape).
- `NPCTemplate.Allies` is `List<NPCTemplate>?` — Goose/NPCTemplate.cs:178; **`EquippedItems`
  is a `string`** ("items part of MKC string") — Goose/NPCTemplate.cs:166; `Quests`/`Drops`
  are non-nullable `= null!` lists (Goose/NPCTemplate.cs:208-215). Sheet NPCs always get a
  non-null `Allies` (Goose/NPCHandler.cs:125-150, unconditional at :150). `NPC.Allies`
  passthrough — Goose/NPC.cs:309; derefs — Goose/NPC.cs:569, :1015. `NPC.VendorItems` is a
  **get-only passthrough** (`{ get => this.NPCTemplate.VendorItems; }`) — Goose/NPC.cs:350.
- `NPC.LoadFromTemplate` — Goose/NPC.cs:595-660 (returns bool; `template.BaseStats` :610,
  `GetClass` :648, `GetLevel` :649). `SpawnNPC` checks its return (Goose/NPCHandler.cs:305-311)
  and calls `LoadFromTemplate` **directly** — scripts can pass a template they never
  registered via `AddTemplate`, so `LoadFromTemplate` must run the same validation.
- `Item.LoadFromTemplate` — Goose/Item.cs:152-175 (void; `template.BaseStats` :155).
  Call sites (all do `new Item(); item.LoadFromTemplate(t);` then use the item — a silent
  void return leaves a half-initialized item in the caller's hands):
  Goose/Quests/QuestWindow.cs:362, Goose/Events/DestroyItemEvent.cs:60,
  Goose/Events/GetItemCommandEvent.cs:56, Goose/Events/VendorPurchaseInventoryEvent.cs:87,
  Goose/Events/PlaceSpawnCommandEvent.cs:46, Goose/Events/CustomCommandEvent.cs:66,
  Goose/NPC.cs:1446 (drops), Goose/Player.cs:653 (starting items).
- `Inventory.Load` — Goose/Inventory.cs:906-978. Three independent queries, each:
  `Convert.ToString(query.ExecuteScalar())!` → `JsonHelper.Deserialize<ItemSlot?[]>(...)!`
  → **the deserialized array replaces the ctor-sized array** (`this.inventory = …`, :913;
  `this.equipped = …`, :927) or feeds `combineContainer.SetSlot(i, …)` (:977). Loops are
  `foreach` over the deserialized array (inventory/equipped) and `for i < combineSlots.Length`
  (combine) — a short blob makes `GetSlot` (Inventory.cs:172-178, bounds-checks against
  `settings.InventorySize` then indexes the now-short array) throw
  `IndexOutOfRangeException`; a long combine blob overflows `SetSlot`. `GetSlot`/equipped
  template derefs + `is null` log-but-continue at :920-922/:939-941/:971-973.
  Ctor sizes — Goose/Inventory.cs:43-52: `inventory = new ItemSlot[settings.InventorySize + 1]`,
  `equipped = new ItemSlot[settings.EquippedSize + 1]`, `combineContainer = new ItemContainer(settings.CombineBagSize + 1)`.
- `ItemContainer.MaxSlots` — Goose/ItemContainer.cs:12.
- `Spellbook.Load` — Goose/Spellbook.cs:35-54: `Deserialize<int[]>(...)!` then
  `for (int i = 1; i < this.spells.Length; i++) { var spellId = spellIds[i]; … }` — a short
  blob throws `IndexOutOfRangeException` at :45.
- `Player.LoadQuests` — Goose/Player.cs:814-849: `Deserialize<QuestStatus>(...)!` (type at
  Goose/Quests/QuestStatus.cs:9) then `foreach` over `questStatus.Started!`/`Completed!`/`Progress!`
  — a `"null"` blob or a blob with null lists NREs.
- `PlayerBank.Load` — Goose/PlayerBank.cs:23-55: per-row `Deserialize<ItemSlot[]>(...)!`
  then `for i < containerSlots.Length` with `container.SetSlot(i, …)` — a blob longer than
  the bank page overflows the container (`GetOrCreateContainer(…, world.Settings.BankSlotsPerPage)`,
  :36). `serialized_data` is `TEXT NOT NULL` (Goose/sql/banks.sql:4) — a corrupt-row test
  must use an **empty string**, not SQL NULL.
- `JsonHelper.Deserialize<T>(string)` — Goose/JsonHelper.cs:51-53: `JsonSerializer.Deserialize`
  throws `ArgumentNullException` on null input and `JsonException` on malformed input;
  `Deserialize("null")` **returns null** (valid JSON) — so every call site needs both an
  empty-string guard and a null-result guard.
- Gold item — Goose/GameWorld.cs:302-306 (`GetTemplate(GoldItemID)!`).
- `/placespawn` gold item — Goose/Events/PlaceSpawnCommandEvent.cs:46.
- `ClassHandler.LoadClasses` — Goose/ClassHandler.cs:33-75. **Orphan `class_info` row
  (unknown `class_id`) hits `if (cl is null) { return; }` at :69-72 — a bare `return` out
  of the `Database.Execute` lambda, aborting all remaining level loading.** `GetClass` :19-27.
  `Class.GetLevel` — Goose/Class.cs:19-23; `Class.MaxLevel` :25; `Class.levels` is a
  private dict (no public enumerator today — Task 7 adds `LevelIds`).
- `Player.LoadFromAutoCreate` — Goose/Player.cs:556-622 (class/level lookups :621-622,
  **with more initialization after**: `BodyState`, starting items at :638-653). Caller:
  Goose/Events/LoginEvent.cs:159 (auto-create on login — a false return rejects the login
  via the existing denial path).
- `Player.LoadFromReader` — Goose/Player.cs:680-755 (`PlayerID` read at :688, **before** the
  class/level lookups at :754-755, which are also **not** the end of the method —
  `ToggleSettings`/`AetherThreshold` follow at :756-758 — so a false return still leaves
  `PlayerID` set for the caller's `CurrentID` bookkeeping). Caller:
  Goose/PlayerHandler.cs:197 inside `LoadPlayerData` (:187-215) — **server-startup preload
  of every player row**, not per-login; a failed load means the player is simply not
  registered (login says "doesn't exist"), and the second-pass `LoadAdditional`
  (:211-214) skips them. `PlayerHandler` has **no logger field** — Task 7 adds one.
- `GameWorld` startup load chain — Goose/GameWorld.cs:245-300: sequential
  `LoadStep(name, action, count)` calls; `LoadStep` returns false only when the action
  **throws** (GameWorld.cs:338-343) — a zero item count is merely logged (:326-332), so
  "0 classes loaded" does **not** abort startup on its own. `"Maps"` is at :270,
  `"Classes"` at :273. Task 6 inserts the starting-map validation right after the Maps
  step; Task 7 adds the explicit zero-classes check after the Classes step. Both throw
  `FatalStartupException`, which `GameServer.Run` catches (Goose/GameServer.cs:92) and
  exits on **before** `GameServer.Start` binds the listen socket (:150-153).
- `Player.ChangeClass` — Goose/Player.cs:1412-1454. Lookups in order: `RemoveStats` (:1416),
  `MaxStats -= this.Class.GetLevel(this.Level)!.BaseStats` — old class @ old level (:1418),
  `Level = newLevel` (:1419), `Experience = this.Class.GetLevel(this.Level - 1)!.Experience`
  — **old class** @ (newLevel-1), because `Class` is not swapped until :1428 (:1426),
  `ClassID`/`Class` swap (:1427-1428), `BoundMap = GetMap(StartingMapID)!` (:1432),
  `AddStats(this.Class.GetLevel(this.Level)!.BaseStats)` — new @ newLevel (:1436), spell
  relearn loop `for (level = 1; level <= this.Level; level++) { if (level > this.Class.MaxLevel) break; this.Class.GetLevel(level)! }` —
  **new class**, bounded by `MaxLevel` (:1445-1453). Callers: Goose/Quests/QuestWindow.cs:435
  (rebirth reward, `newLevel = 5`), Goose/Player.cs:1463 (wrapper applying settings loss
  percent).
- `ChangeClassCommandEvent` — Goose/Events/ChangeClassCommandEvent.cs:35-79: finds
  `newClass` by name, then mutates `player.ClassID = newClass.ClassID` at :42 **before**
  `player.Class.GetLevel(player.Level)!` (old class, :45); level is **not** changed, so the
  post-swap `player.Class.GetLevel(player.Level)!` (:55) is new class @ *old* level;
  relearn loop `for (level = 1; level <= player.Level; level++) { if (level >
  player.Class.MaxLevel) break; player.Class.GetLevel(level)! }` — new class, bounded by
  `MaxLevel` (:61-69).
- `Pet.FromReader` — Goose/Pet.cs:199-262 (`GetClass` :209/:256, `GetLevel` :257). Columns
  read (the test fixture must supply **all** of them — `FakeDbDataReader`'s name indexer
  throws `KeyNotFoundException` on a missing key, Goose.Tests/Fakes/FakeDbDataReader.cs:15,
  and the `DataReaderExtensions` helpers route through that same indexer,
  Goose/DataReaderExtensions.cs:6-18): `pet_id, pet_title, pet_name, pet_surname,
  pet_level, class_id, experience, experience_sold, body_id, body_r, body_g, body_b,
  body_a, face_id, hair_id, hair_r, hair_g, hair_b, hair_a, pet_hp, pet_mp, pet_sp,
  stat_ac, stat_str, stat_sta, stat_int, stat_dex, res_fire, res_air, res_earth,
  res_spirit, res_water, weapon_damage` (enumerate the tail of the method in the red
  phase in case further columns follow :262).
  `Pet.FromCharacter` — Goose/Pet.cs:151/:166 (uses the tamer's class/level — safe once
  player load clamps; no change).
- Pet info window — Goose/Window.cs:267/274. Pet-tame success rate — Goose/SpellEffect.cs:890-891.
- Buy-command max-level checks — Goose/Events/BuyManaCommandEvent.cs:13,
  BuyVitaCommandEvent.cs:13 (body verified: `Ready` requires `State == Ready`, parses
  `Data` in an internal try/catch, then the level check — directly testable),
  PetDamageCommandEvent.cs:63, PetVitaCommandEvent.cs:63
  (`GetLevel(Level)!.Experience != 0` → `return`).
- `Player.ProcessLevelUp` — Goose/Player.cs:1787-1830: **verified safe** (loop condition and
  inner `is not null` checks cover every `GetLevel` deref); no change.
- `PlayerPongEvent` — Goose/Events/PlayerPongEvent.cs:13-19 (only touches `LastPing` — safe
  to allow during map load).
- Test fixtures: `TestWorldFixture` — TestSupport/TestWorldFixture.cs (`RunCommand` :105,
  `CommandPlayerOn` :82, `AddBaseMap` :43, `AddBaseSpellEffect` :56, `AddBaseSpell` :113,
  `AddBaseItemTemplate` :126, `SeedClass(int classId, string name, int maxLevel)` :141-149
  — seeds **contiguous** levels 1..maxLevel via reflection into `ClassHandler.classes`;
  Task 7 adds a `SeedClassLevels(int classId, string name, int[] levels)` overload for
  gapped sets using the same reflection). `FakeDbDataReader` — Goose.Tests/Fakes/FakeDbDataReader.cs
  (name-indexed dictionary; **missing key throws**, typed/ordinal accessors throw).
  DB-backed test base — Goose.IntegrationTests/PlayerFirstSaveTests.cs:6-40 (temp DB +
  `db.Execute` schema files from `AppContext.BaseDirectory/sql/`). NPC unit-test pattern —
  Goose.Tests/NPCSpawnRegistrationTests.cs. Vendor fixture — Goose.Tests/Fixtures/VendorFixture.cs
  (`Player` with `.Sent`, `Vendor`, `Map`, `Stock` :67). Settings-loader test pattern —
  Goose.Tests/GooseSettingsLoaderTests.cs.
- Schemas: `Goose/sql/maps.sql` (3-column minimal insert works), `Goose/sql/warptiles.sql`
  (map_id, map_x, map_y, warp_id, warp_x, warp_y), `Goose/sql/players.sql:58-84`
  (inventory/equipped/combinebag/spellbook), `Goose/sql/banks.sql:2-6` (bank_items,
  `serialized_data TEXT NOT NULL`), `Goose/sql/quests.sql:60` (quest_status). `npcs.sql`
  has 70 NOT NULL columns — sheet-based NPC load tests are impractical; see Task 3 seam.
- Map binary format (for Task 6's IT) — `IllutiaMapLoader`, Goose/Map.cs:441-475:
  `Int16 version, Int16 editorVersion, Int32 width, Int32 height`, then per tile
  (y=1..h, x=1..w): `Int32 flags` + 5 × (`Int32 graphic`, `Int16 sheet`). `Map.LoadData`
  (Goose/Map.cs:519-536) does `File.Open` with no existence check, and routes on
  `Settings.ServerType == "Illutia"` (:529) — code-built settings leave `ServerType` null,
  which would silently use `AsperetaMapLoader`.
- Loggers: files that already have `private static NLog.Logger log`: NPC, Player, Inventory,
  ItemHandler, EventHandler, GooseSettingsLoader, Map, GameWorld. Files that need the field
  added where this plan adds logging: `NPCHandler`, `ClassHandler`, `Pet`, `Spellbook`,
  `PlayerBank`, `PlayerHandler`, `LoginContinuedEvent`, `DoneLoadingMapEvent`. (One line
  each: `private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();`)

## Conventions

- **Test placement:** fast unit tests in `Goose.Tests`; anything needing a real SQLite DB in
  `Goose.IntegrationTests` (repo convention — `DatabaseTransactionTests`,
  `PlayerFirstSaveTests`, `PlayerPropertiesPersistenceTests` all live there).
- **Per-task gate (run before committing):**
  1. `dotnet build Goose.sln --no-incremental -p:WarningsAsErrors=nullable` → exit 0
     (no new nullable warnings — guards must not introduce any).
  2. Focused tests for the task pass.
  3. `dotnet test Goose.sln` → Goose.Tests 341+/0 failed, Tools.Tests 124+/0 failed.
- **Commit:** one commit per task, message `fix: <summary> (deferred null bug #N)`.
- **Logging style:** NLog structured params (`log.Error("map {0}: warp tile ...", id)`),
  matching existing call sites.
- **AGENTS.md:** no new comments in production or test code unless the "why" is non-obvious
  (a `!` whose safety depends on an invariant, a data-format detail, a non-obvious test
  setup). Test snippets below carry only such comments.

---

### Task 1: Buff and spell-effect guards (bugs #3, #9, #10)

**Files:**
- Modify: `Goose/NPC.cs:1519-1530` (AddBuff tick branch), `Goose/NPC.cs:1685-1720` (OnMeleeHit/OnMeleeAttack)
- Modify: `Goose/Player.cs:2190-2200` (AddBuff tick branch), `Goose/Player.cs:2477-2512` (OnMeleeHit/OnMeleeAttack)
- Modify: `Goose/SpellEffect.cs:1056-1068` (CastScriptSpell)
- Test: Create `Goose.Tests/BuffNullGuardTests.cs`

**Behavior:**
1. **Zero-duration tick buff (#3).** In both `AddBuff` overloads, the tick-scheduling branch
   (NPC.cs:1519-1530, Player.cs:2190-2200) dereferences `buff.BuffExpireEvent!.Ticks`, but
   the expire event is only created when `Duration > 0`. Change the condition to also
   require `buff.BuffExpireEvent is not null`:
   `if (buff.BuffExpireEvent is not null && buff.BuffExpireEvent.Ticks - world.TimeNow > …)`.
   A zero-duration buff keeps its current (permanent, no-expire) semantics — out of scope.
2. **OnMeleeHit/OnMeleeAttack with no linked spell (#9).** At all four sites, fetch the
   nullable spell into a local and guard:
   `SpellEffect? spell = b.SpellEffect.OnMeleeHitSpell; if (spell is not null && world.Random.Next(1, 10001) <= b.SpellEffect.OnMeleeHitSpellChance * 100) spell.Cast(this, hitter, world);`
   (same shape for `OnMeleeAttackSpell`). An OnMeleeHit effect with `on_hit_spell_effect_id = 0`
   simply does nothing.
3. **CastScriptSpell with null Script (#10).** Replace `this.Script!.Object.Cast(...)` with an
   explicit `if (this.Script is null) { log.Error("Spell effect {0} has type Script but no script loaded", this.ID); return false; }`.
   No behavior change (the existing catch already returns false) — this removes exception-as-flow-control.

**Step 1: Write the failing tests** (`Goose.Tests/BuffNullGuardTests.cs`, `TestWorldFixture`):

```csharp
[Fact]
public void AddBuff_ZeroDurationTickEffect_DoesNotThrow()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 1, 1);
    var effect = fixture.AddBaseSpellEffect(1, "tick0",
        e => { e.EffectType = SpellEffect.EffectTypes.Tick; e.Duration = 0; });
    var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };

    player.AddBuff(buff, fixture.World);

    Assert.Contains(buff, player.Buffs);
}
```

Expected RED: `NullReferenceException` at Goose/Player.cs:2194 (`BuffExpireEvent!` is null).
Add the NPC variant the same way: spawn via `fixture.World.NPCHandler.SpawnNPC(world, 1, 3, 3,
template, false)` (template per Goose.Tests/NPCSpawnRegistrationTests.cs:57-63 — needs
`BaseStats`, `ClassID` matching a `SeedClass` id, `Level` with a seeded level row) and call
`npc.AddBuff(buff, fixture.World)`.

```csharp
[Fact]
public void OnMeleeHit_NullLinkedSpell_DoesNotThrow()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 1, 1);
    var effect = fixture.AddBaseSpellEffect(1, "onhit",
        e => { e.EffectType = SpellEffect.EffectTypes.OnMeleeHit; e.OnMeleeHitSpellChance = 100m; });
    player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect });

    player.OnMeleeHit(player, fixture.World);

    Assert.Single(player.Buffs);
}
```

Expected RED: `NullReferenceException` at Goose/Player.cs:2487. Chance is 0-100 and the roll
is `Next(1, 10001) <= chance * 100`, so 100 makes the cast attempt deterministic. Add the
OnMeleeAttack variant.

Pin test for #10 (green in both phases — documents the contract):

```csharp
[Fact]
public void Cast_ScriptEffectWithoutScript_ReturnsFalse()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 1, 1);
    var effect = fixture.AddBaseSpellEffect(1, "scriptless",
        e => e.EffectType = SpellEffect.EffectTypes.Script);

    Assert.False(effect.Cast(player, player, fixture.World));
}
```

**Step 2:** Run `dotnet test Goose.Tests --filter FullyQualifiedName~BuffNullGuardTests` → the two guard tests FAIL with NRE, pin test passes.

**Step 3:** Implement the guards (items 1-3 above).

**Step 4:** Same command → all pass. Then the per-task gate.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Zero-duration tick buff doesn't NRE, buff is still applied | `AddBuff_ZeroDurationTickEffect_DoesNotThrow` (player + NPC) |
| OnMeleeHit/OnMeleeAttack with null linked spell is a no-op, not an NRE | `OnMeleeHit_NullLinkedSpell_DoesNotThrow` + attack variant |
| Script effect without a loaded script returns false | `Cast_ScriptEffectWithoutScript_ReturnsFalse` (pin) |

**Commit:** `fix: guard buff tick scheduling and melee reaction spells against nulls (deferred null bugs #3, #9, #10)`

---

### Task 2: Player data load robustness (bugs #7, #14, new: null `ItemSlot.Item`)

**Files:**
- Modify: `Goose/Inventory.cs:906-978` (Load)
- Modify: `Goose/Spellbook.cs:35-54` (Load) — add logger field
- Modify: `Goose/Player.cs:814-849` (LoadQuests)
- Modify: `Goose/PlayerBank.cs:23-55` (Load) — add logger field
- Modify: `Goose/GameWorld.cs:302-306` (gold item)
- Modify: `Goose/Events/PlaceSpawnCommandEvent.cs:44-50`
- Test: Create `Goose.IntegrationTests/PlayerLoadMissingRowTests.cs`

**Mutation impact:**
- Source of truth changed: the in-memory `Inventory`/`Spellbook`/`PlayerBank`/`QuestProgress`
  state built by the `Load` methods from `serialized_data` rows.
- Important readers: everything downstream of login (item ops, `RefreshStats`, `AddStats`,
  bank windows). The save path (`Inventory.BuildSave` etc.) serializes whatever the arrays
  hold — discarding a bad slot persists the discard on next save (intended: a slot with an
  unresolvable template is unusable anyway).
- Derived/cached state: `player.MaxStats` (equipped items add stats in `Inventory.Load`);
  skipping a slot must also skip its `AddStats` (it does — same loop body).
- Invariants to preserve: a player whose rows exist and are valid loads byte-identical state
  to today (no behavior change on good data); a missing/empty/corrupt row yields the same
  state as a freshly created player (empty arrays).
- Observable proof: the IT tests below assert final array contents, not log calls.

**Behavior:**
1. **Missing / empty / corrupt rows (#7).** In each `Load`, the raw cell
   (`Convert.ToString(ExecuteScalar())`) can be null (no row), `""` (empty cell), valid JSON
   `null` (`"null"` — deserializes to a null object), or malformed JSON (`JsonException`).
   All four cases degrade to the empty default with a log. Pattern per query:
   ```csharp
   string? raw = Convert.ToString(query.ExecuteScalar());
   T? data = null;
   if (!string.IsNullOrEmpty(raw))
   {
       try { data = JsonHelper.Deserialize<T>(raw); }
       catch (JsonException e) { log.Error("player {0}: <table> blob is corrupt; starting empty", playerId, e); }
   }
   if (data is null) { log.Warn("player {0}: no <table> row; starting empty", playerId); }
   ```
   (One helper per loader is fine; the table name and log wording differ per site.)
   - `Inventory.Load`: applies to all three queries (each independent — a missing row for
     one must not skip the others).
   - `Spellbook.Load` (Spellbook.cs:42-43): same; spells stay null.
   - `Player.LoadQuests` (Player.cs:821-849): same for the `QuestStatus` object, **plus**
     null-guard the three lists: `foreach (var started in questStatus.Started ?? [])`
     (same for `Completed`/`Progress`) — a `"null"` object deserializes with null lists.
   - `PlayerBank.Load` (PlayerBank.cs:37-39): per row, guard `string.IsNullOrEmpty` on the
     cell (the `DataReaderExtensions.GetString` call returns `""` for an empty cell) and
     wrap the deserialize in the same try/catch; skip the row on failure.
2. **Length normalization (Inventory + Bank).** The deserialized arrays replace the
   ctor-sized arrays, so a short blob makes `GetSlot` index out of range and a long combine
   blob overflows `SetSlot`. After a successful deserialize, copy into a fixed-size array
   matching the ctor (Inventory.cs:43-52): `inventory` → `settings.InventorySize + 1`,
   `equipped` → `settings.EquippedSize + 1`, combine → `settings.CombineBagSize + 1`
   (pad with null, truncate the excess, log if the lengths differed). In `PlayerBank.Load`,
   bound the `SetSlot` loop by the container: `i < containerSlots.Length && i < container.MaxSlots`
   (ItemContainer.cs:12), logging discarded excess.
3. **Unknown item template id (#14).** In the three `Inventory.Load` loops and the
   `PlayerBank.Load` loop, when `GetTemplate(invSlot.Item.TemplateID)` is null: log the error
   and **discard the slot** (do not `AddItem`, do not `RefreshStats`, do not `AddStats`, do
   not `SetSlot`). Convert the `foreach` loops to indexed `for` loops over the normalized
   array so the slot can be nulled in place (`this.inventory[i] = null` /
   `this.equipped[i] = null`; bank: simply don't `SetSlot`).
4. **Null `ItemSlot.Item` (new finding).** A corrupted blob may contain a slot object with
   `"Item": null`. Extend each loop's existing null check:
   `if (invSlot is null) continue; if (invSlot.Item is null) { log.Error("player {0}: slot with null item discarded", playerId); <discard as in 3>; continue; }`
5. **Short spellbook blob.** `for (int i = 1; i < this.spells.Length && i < spellIds.Length; i++)`
   (Spellbook.cs:45) — a short blob leaves the remaining slots null instead of throwing
   `IndexOutOfRangeException`; a long one is ignored past `spells.Length` (unchanged).
6. **Gold item (GameWorld.cs:305).** `ItemTemplate? goldTemplate = this.ItemHandler.GetTemplate(this.Settings.GoldItemID); if (goldTemplate is null) { log.Error("gold item template {0} not found; gold items disabled", this.Settings.GoldItemID); } else { <existing 3 lines>; }`
7. **`/placespawn` (PlaceSpawnCommandEvent.cs:46).** Same guard; when null, send
   `P.ServerMessage("Gold item template is missing.")` and `return`.

**Step 1: Write the failing tests** (`Goose.IntegrationTests/PlayerLoadMissingRowTests.cs`,
modeled on `PlayerFirstSaveTestBase` — Goose.IntegrationTests/PlayerFirstSaveTests.cs:6-40 —
with schema files `players`, `banks`, `quests`):

```csharp
[Fact]
public void Loaders_NoRows_LeaveStateEmpty()
{
    var player = MakePlayer();
    player.Inventory.Load(world);
    player.Spellbook.Load(world);
    player.LoadQuests(world);
    player.Bank.Load(world, player);

    for (int i = 1; i <= 30; i++) Assert.Null(player.Inventory.GetSlot(i));
    for (int i = 1; i <= 30; i++) Assert.Null(player.Spellbook.GetSlot(i));
}
```
Expected RED: `ArgumentNullException` from `JsonHelper.Deserialize(null)`
(Goose/JsonHelper.cs:52 → `JsonSerializer.Deserialize(null, …)`), propagated out of
`Database.Execute`.

Adversarial tests (same file; blobs built by `JsonHelper.Serialize` of hand-built objects —
the short property names come from `[JsonPropertyName]` attributes):
- `Inventory_Load_UnknownTemplateId_DiscardsSlot`: an `inventory` row whose JSON is a
  full-length array with one slot referencing unregistered template 999. Assert: no throw,
  that slot null, other valid slots survive. RED today: NRE in `RefreshStats` after the
  `log.Warn`.
- `Inventory_Load_NullItemInSlot_DiscardsSlot`: slot JSON `{"Item":null,"Stack":1}` →
  discarded, no throw. RED today: NRE at `AddItem(invSlot.Item, …)`.
- `Inventory_Load_JsonNullBlob_StartsEmpty`: `serialized_data = '"null"'` (i.e. the four
  characters `"null"`) → no throw, all slots null. RED today: NRE in the `foreach` over the
  null array.
- `Inventory_Load_MalformedJson_StartsEmpty`: `serialized_data = '{not json'` → no throw,
  all slots null. RED today: `JsonException` out of `Database.Execute`.
- `Inventory_Load_ShortArray_DoesNotThrowOnGetSlot`: an `inventory` row with a 3-element
  array → `Load` + `GetSlot(30)` no throw. RED today: `IndexOutOfRangeException`.
- `Spellbook_Load_ShortArray_LeavesRemainingSlotsNull`: a 2-element `int[]` blob → no
  throw, `GetSlot(30)` null. RED today: `IndexOutOfRangeException` at Spellbook.cs:45.
- `Bank_Load_EmptyStringCell_SkipsRow`: a `bank_items` row with `serialized_data = ''`
  (the column is `NOT NULL` — banks.sql:4 — so the corrupt-cell case is an empty string,
  not SQL NULL) → no throw, no container populated. RED today: `JsonException`.

**Step 2:** Run `dotnet test Goose.IntegrationTests --filter FullyQualifiedName~PlayerLoadMissingRowTests` → RED as above.

**Step 3:** Implement items 1-7.

**Step 4:** Tests green + per-task gate.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Missing row ⇒ empty defaults, no throw | `Loaders_NoRows_LeaveStateEmpty` (all four loaders) |
| JSON `null` blob / malformed JSON ⇒ empty defaults | `Inventory_Load_JsonNullBlob_StartsEmpty`, `Inventory_Load_MalformedJson_StartsEmpty` |
| Unknown template id ⇒ slot discarded, rest intact | `Inventory_Load_UnknownTemplateId_DiscardsSlot` |
| Null `Item` in blob ⇒ slot discarded | `Inventory_Load_NullItemInSlot_DiscardsSlot` |
| Short/long blobs never index out of range | `Inventory_Load_ShortArray_DoesNotThrowOnGetSlot`, `Spellbook_Load_ShortArray_LeavesRemainingSlotsNull` |
| Empty bank cell ⇒ row skipped | `Bank_Load_EmptyStringCell_SkipsRow` |
| Gold item / `/placespawn` guards | Deferred: both sit inside full-world startup / GM command paths that need a complete data set (all sheets) to exercise; the guards are 3-line null checks whose failure mode (missing log / missing message) is operator-visible. Covered by code review. |

**Commit:** `fix: treat missing, empty, or corrupt player data rows as empty instead of throwing (deferred null bugs #7, #14)`

---

### Task 3: NPC and quest data guards (bugs #4, #8, #17, #18) + close #6/#12 in the doc

**Files:**
- Modify: `Goose/NPC.cs:1162-1207` (kill reward + drops — **not** the buff-removal loop)
- Modify: `Goose/NPCHandler.cs:109-111` (quest list) — add logger field; extract internal static `ResolveQuests`
- Modify: `Goose/Quests/QuestWindow.cs:214-230` (GetQuestProgressText)
- Modify: `Goose/Window.cs:153-163` (vendor Populate), `Goose/Events/VendorPurchaseInventoryEvent.cs:63-66`
- Modify: `docs/plans/2026-08-25-nullable-inventory.md` (latent bugs #6, #12)
- Test: Create `Goose.Tests/QuestWindowNullTemplateTests.cs`; extend `Goose.Tests/NPCTemplateRegistrationTests.cs`; extend `Goose.Tests/VendorFixtureTests.cs`

**Behavior:**
1. **#4 kill reward.** The death block (NPC.cs:1102-1207) has three parts: experience
   (:1162-1188), buff removal (:1190-1199), drops (:1201-1205). Only experience and drops
   depend on `highest`; **buff removal must stay unconditional** — a dead NPC that keeps
   its buffs through respawn would carry them back to life. Wrap the experience block in
   `if (highest is not null) { …existing… } else { log.Warn("NPC {0} died with no damage entries; kill reward skipped", this.NPCTemplateID); }`
   and the `DropItems` calls in the same condition (a second `if (highest is not null)`
   around :1201-1205 is fine). Unreachable today (see Background) — defensive only.
2. **#18 NPC.Quests null entries.** `npcs.sql` has 70 NOT NULL columns, so a sheet-based load
   test is impractical; extract the resolution into a testable seam:
   ```csharp
   internal static List<Quest> ResolveQuests(int npcTemplateId, string rawQuestIds, QuestHandler handler)
   {
       var quests = new List<Quest>();
       foreach (string token in rawQuestIds.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
       {
           int id;
           if (!int.TryParse(token, out id)) { log.Error("NPC template {0}: bad quest id '{1}'", npcTemplateId, token); continue; }
           Quest? quest = handler.Get(id);
           if (quest is null) { log.Error("NPC template {0}: unknown quest {1}", npcTemplateId, id); continue; }
           quests.Add(quest);
       }
       return quests;
   }
   ```
   Call site (NPCHandler.cs:111) becomes `npc.Quests = ResolveQuests(npc.NPCTemplateID, reader.GetString("quest_ids"), world.QuestHandler);`.
   This also fixes a latent `FormatException` (the current `.Select(q => Convert.ToInt32(q))`
   throws on garbage tokens).
3. **#17 quest requirement display.** In `GetQuestProgressText` (QuestWindow.cs:217/222/227),
   replace the `!` derefs with null-coalesced display text:
   `text += $"{(item?.Name ?? "Unknown item")} ({requirement.Value2})\\n";` (and
   `"Unknown NPC"` for the two talk/kill lines). No throw; the window renders.
4. **#8 vendor paths.** `Window.Populate` vendor case (Window.cs:153-163):
   `if (this.NPC?.VendorItems is null) return;` before the loop (the `P.ClearVendor()` send
   at :155 stays — the window opens empty). The loop is already bounded by
   `VendorItems.Length`, so no length guard is needed here.
   `VendorPurchaseInventoryEvent` (:63): `if (npc.VendorItems is null || slotid < 0 || slotid >= npc.VendorItems.Length) return;`
   before the index — today only null-slot is checked, so a forged `slotid` past the end
   throws `IndexOutOfRangeException`. A purchase against a non-vendor/out-of-range slot is
   a forged/mismatched packet; silent return matches the neighboring guards (:51/:66).
5. **Doc close.** In `docs/plans/2026-08-25-nullable-inventory.md`, rewrite latent bugs #6
   and #12: mark **Closed 2026-07-09** with the verification note (#6: guard at
   Goose/Events/NPCAttackEvent.cs:11-12; #12: all five consumer events guard or are safe by
   construction — cite NPCAttackEvent.cs:11-12, NPCMoveEvent.cs:13-14, RegenEvent,
   BuffExpireEvent.cs:24-30, NPCSpawnEvent.cs:9). No code change.

**Step 1: Write the failing tests.**

```csharp
[Fact]
public void GetQuestProgressText_UnknownItemTemplate_RendersUnknownInsteadOfThrowing()
{
    string text = window.GetQuestProgressText(player, world);

    Assert.Contains("Unknown item", text);
}
```
Setup follows the pattern in Goose.Tests/QuestWindowScriptTests.cs:17-50 — a quest with
one Item requirement, `Value = 999`, no template registered. Expected RED:
`NullReferenceException` at QuestWindow.cs:217. Add the TalkToNPC and Kill variants
(Value = 999 against an empty `NPCHandler`).

```csharp
[Fact]
public void ResolveQuests_UnknownAndBadIds_AreFilteredNotNull()
{
    List<Quest> quests = NPCHandler.ResolveQuests(7, "1 999 notanumber", world.QuestHandler);

    Assert.Single(quests);
    Assert.DoesNotContain(quests, q => q is null);
}
```
Quest 1 is registered via `world.QuestHandler` (pattern:
Goose.Tests/QuestHandlerRegistrationTests.cs). This is a seam test (green on
introduction); the adversarial property is "unknown ids never enter `NPC.Quests` as
null". The old inline expression had no testable seam — noted here per the
test-introduction rule.

```csharp
[Fact]
public void VendorWindow_Populate_NonVendorNpc_DoesNotThrow()
{
    using var fixture = new VendorFixture();
    fixture.Vendor.NPCTemplate.VendorItems = null;

    var window = fixture.Player.Windows.First(w => w.Type == Window.WindowTypes.Vendor);
    window.Populate(fixture.Player, fixture.World);

    Assert.Contains(fixture.Player.Sent, m => m.StartsWith("CVN"));
}
```
Expected RED: NRE at Window.cs:156. (Verify the `P.ClearVendor` opcode prefix against
Goose/Packets.cs before asserting on it — adjust `StartsWith` to the actual opcode.)

```csharp
[Fact]
public void VendorPurchase_OutOfRangeSlotId_IsRejected()
{
    using var fixture = new VendorFixture();
    var ev = new VendorPurchaseInventoryEvent { Player = fixture.Player };
    ev.Data = <packet with slotid past VendorItems.Length>;

    ev.Ready(fixture.World);

    Assert.DoesNotContain(fixture.Player.Sent, m => m.StartsWith("Purchased"));
}
```
Expected RED: `IndexOutOfRangeException` at VendorPurchaseInventoryEvent.cs:63 (propagates —
direct `Ready` call). (Check the packet's `Data` shape in the red phase; the neighboring
guards at :51/:66 show the parse.)

**Step 2:** RED as above (seam test compiles once the seam exists — introduce the seam in
Step 3 first if ordering forces it; the other tests are red before any change).

**Step 3:** Implement items 1-4; update the doc (item 5).

**Step 4:** Tests green + per-task gate.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| `NPC.Quests` never contains null / bad tokens don't crash sheet load | `ResolveQuests_UnknownAndBadIds_AreFilteredNotNull` (seam; call-site change is mechanical) |
| Quest window renders with deleted item/NPC templates | `GetQuestProgressText_UnknownItemTemplate_…` + 2 variants |
| Vendor window on a non-vendor NPC is a no-op | `VendorWindow_Populate_NonVendorNpc_DoesNotThrow` |
| Out-of-range vendor `slotid` is rejected | `VendorPurchase_OutOfRangeSlotId_IsRejected` |
| Dead NPC loses buffs even with no damage entries | Code review: the buff-removal loop (NPC.cs:1190-1199) is deliberately left outside the `highest is not null` guards; reachable-path drops are regression-covered by Goose.IntegrationTests/DimensionDropTests.cs |
| #6 / #12 closed | Doc update (verification cited; guards already in code) |

**Commit:** `fix: guard NPC/quest/vendor paths against bad sheet data (deferred null bugs #4, #8, #17, #18; close #6, #12)`

---

### Task 4: GooseSettings string defaults (bug #19)

**Files:**
- Modify: `Goose/GooseSettingsLoader.cs:53-55`
- Modify: `Goose/FatalStartupException.cs` (add a one-argument constructor — the only
  constructor today, :5-7, requires an inner `Exception`)
- Test: extend `Goose.Tests/GooseSettingsLoaderTests.cs`

**Behavior:** After `Deserialize` (GooseSettingsLoader.cs:53-55), reject a null result
(a settings file containing the bare document `null` deserializes to null and would NRE in
the property loop below) with a startup exception, then default every null public writable
`string` property to `""` and log the missing field names:

Add to `FatalStartupException` (Goose/FatalStartupException.cs, alongside the existing
`:5-7` two-arg constructor):

```csharp
public FatalStartupException(string message) : base(message) { }
```

Then in the loader, after `Deserialize` (GooseSettingsLoader.cs:53-55):

```csharp
GooseSettings? settings = JsonSerializer.Deserialize<GooseSettings>(
    File.ReadAllText(settingsPath, Encoding.UTF8), JsonHelper.SettingsOptions);
if (settings is null)
    throw new FatalStartupException("GooseSettings.json is empty or null");

List<string> missing = [];
foreach (System.Reflection.PropertyInfo prop in
    typeof(GooseSettings).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
    .Where(p => p.PropertyType == typeof(string) && p.CanWrite))
{
    if (prop.GetValue(settings) is null)
    {
        prop.SetValue(settings, "");
        missing.Add(prop.Name);
    }
}
if (missing.Count > 0)
    log.Warn("GooseSettings.json is missing fields (defaulted to empty): {0}", string.Join(", ", missing));
return settings;
```

Reflection is deliberate: it covers the 12 current string properties
(Goose/GooseSettings.cs:6-10, 20, 24, 33, 75-77, 95) and any future ones, and touches only
`string` properties, so the one-way-property trap noted for `SetConfigCommandEvent` does not
apply. `GameServerIP = ""` fails loudly at bind time with `FatalStartupException`
(Goose/GameServer.cs:164-172) — the correct failure for a missing listen address. The
null-document rejection uses the same `FatalStartupException`. The loader runs from
`Program.Main` (Program.cs:20), **before** the `GameServer` is constructed, so the
exception propagates out of `Main` and the process exits non-zero with the message —
before any world or socket exists, which is the correct loud failure. (Contrast the
Task 6/7 startup validations, which throw from inside `GameWorld.Start` and are caught
by `GameServer.Run`'s `catch (FatalStartupException)` at Goose/GameServer.cs:92 — that
catch wraps `this.Start()` at :89, so those exits happen **before** the listen socket is
created at :150-153.)

**Step 1: Failing test** (extend GooseSettingsLoaderTests.cs, reuse its `root`/`baseDir`/`dataDir`):

```csharp
[Fact]
public void Load_MissingStringFields_DefaultsToEmpty()
{
    Directory.CreateDirectory(baseDir);
    File.WriteAllText(SettingsPath(baseDir), "{\"StartingMapID\": 7}");

    GooseSettings settings = GooseSettingsLoader.Load(baseDir, dataDir);

    Assert.Equal(7, settings.StartingMapID);
    foreach (string? value in new[]
    {
        settings.ServerVersion, settings.ServerType, settings.DatabaseName, settings.DataLinkId,
        settings.DataPath, settings.ServerName, settings.StartingItems, settings.GameServerIP,
        settings.MOTD, settings.StartingTitle, settings.StartingSurname, settings.DefaultGuildMOTD,
    })
        Assert.NotNull(value);
}
```
Expected RED: `Assert.NotNull` fails on the first null string (e.g. `ServerVersion`).

```csharp
[Fact]
public void Load_NullDocument_ThrowsFatalStartupException()
{
    Directory.CreateDirectory(baseDir);
    File.WriteAllText(SettingsPath(baseDir), "null");

    Assert.Throws<FatalStartupException>(() => GooseSettingsLoader.Load(baseDir, dataDir));
}
```
Expected RED: `NullReferenceException` (or `ArgumentNullException` from
`PropertyInfo.GetValue(null)`) instead of the fatal exception.

**Step 2:** RED. **Step 3:** Implement. **Step 4:** GREEN + per-task gate.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Settings JSON omitting string fields ⇒ non-null defaults, no NRE at `MOTD.Length` (LoginContinuedEvent.cs:34), `StartingItems.Split` (Player.cs:638), etc. | `Load_MissingStringFields_DefaultsToEmpty` (adversarial: minimal JSON) |
| Settings document that is JSON `null` ⇒ fatal startup error, not an NRE | `Load_NullDocument_ThrowsFatalStartupException` |
| Present fields unaffected | same test (`StartingMapID == 7`) + existing loader tests |

**Commit:** `fix: default missing GooseSettings string fields to empty (deferred null bug #19)`

---

### Task 5: Script-registered template validation and normalization (bug #5 + new: null `Allies`/`Quests`/`EquippedItems`)

**Files:**
- Modify: `Goose/NPCHandler.cs:234-237` (AddTemplate) + new `internal static ValidateAndNormalize(NPCTemplate)` — add logger field
- Modify: `Goose/ItemHandler.cs:199-202` (AddTemplate) + new `internal static Validate(ItemTemplate)`
- Modify: `Goose/NPC.cs:595-660` (LoadFromTemplate — call the shared validator)
- Modify: `Goose/Item.cs:152-175` (LoadFromTemplate — call the shared validator, return bool)
- Modify: the 8 `Item.LoadFromTemplate` call sites listed in Behavior 4 (2 further sites
  are excluded there with the reason — see the exclusion note)
- Test: extend `Goose.Tests/NPCTemplateRegistrationTests.cs` and `Goose.Tests/ItemHandlerRegistrationTests.cs`

**New findings this task covers:** `NPCHandler.AddTemplate` is the script API
(`NPCHandler.cs:232-237`; shipped scripts call it from
`Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx`). Sheet NPCs always get a non-null
`Allies` (NPCHandler.cs:150), but a script-built template that omits `Allies`
(`List<NPCTemplate>?`, NPCTemplate.cs:178) NREs at NPC.cs:569/:1015 on the aggro paths.
`NPC.LoadFromTemplate` also copies `template.Quests`/`template.EquippedItems` verbatim
(NPC.cs:651-657) — null on a bare template NREs in quest/attack paths. Shipped scripts
happen to set these (Npcs.csx:300/454); the API must enforce it.

**The bypass that drives the design:** scripts can pass a template **directly** to
`NPCHandler.SpawnNPC` (NPCHandler.cs:305) without ever calling `AddTemplate`, and
`SpawnNPC` → `NPC.LoadFromTemplate` is the only other funnel. Validation therefore lives in
a shared static that **both** `AddTemplate` and `LoadFromTemplate` call — registering a
template is not a safety boundary, `LoadFromTemplate` is.

**Behavior:**
1. `NPCHandler.ValidateAndNormalize` (shared, idempotent):
   ```csharp
   internal static bool ValidateAndNormalize(NPCTemplate template)
   {
       if (template is null || template.BaseStats is null || string.IsNullOrWhiteSpace(template.Name))
           return false;
       template.Allies ??= [];
       template.Quests ??= [];
       template.Drops ??= [];
       template.EquippedItems ??= "";
       return true;
   }
   ```
2. `NPCHandler.AddTemplate`:
   ```csharp
   public void AddTemplate(NPCTemplate template)
   {
       if (!ValidateAndNormalize(template))
       {
           log.Error("Refusing NPC template {0}: missing Name or BaseStats", template?.NPCTemplateID);
           return;
       }
       this.templates[template.NPCTemplateID] = template;
   }
   ```
   Reject (log + skip) rather than throw: one bad script template must not kill server
   startup, consistent with the contain-don't-crash policy.
3. `NPC.LoadFromTemplate`: at the top,
   `if (!NPCHandler.ValidateAndNormalize(template)) { log.Error("NPC template {0}: invalid template; spawn skipped", template?.NPCTemplateID); return false; }`
   — covers the direct-`SpawnNPC` bypass (sheet and script templates are already normalized
   by `AddTemplate`, so this is a no-op for them). Extend the existing class check at
   :648-649 to also fail on a missing level row:
   `this.Class = world.ClassHandler.GetClass(this.ClassID); if (this.Class is null || this.Class.GetLevel(this.Level) is null) { log.Error("NPC template {0}: class {1}/level {2} not found; spawn skipped", …); return false; }`
   (`SpawnNPC` already handles the false return — NPCHandler.cs:307.)
4. `Item.LoadFromTemplate` → **bool** (a silent void return leaves callers holding a
   half-initialized item). `ItemHandler.Validate(ItemTemplate)` mirrors the NPC rules
   (null / `BaseStats is null` / whitespace `Name` → false). `LoadFromTemplate` starts
   with `if (!ItemHandler.Validate(template)) return false;`, then runs the normal
   initialization unchanged and ends with `return true;` — a valid template must never
   return before initialization. Update the 8 call sites below:
   - Goose/Player.cs:653 (starting items): skip the item on false (Task 2's null-template
     guard at this site is subsumed — fold them together when editing).
   - Goose/NPC.cs:1446 (drops): skip that drop on false.
   - Goose/Quests/QuestWindow.cs:362 (reward item list): skip that item on false.
   - Goose/Events/DestroyItemEvent.cs:60: on false, don't destroy (log).
   - Goose/Events/GetItemCommandEvent.cs:56: on false, don't give the item (log).
   - Goose/Events/CustomCommandEvent.cs:66: skip that item on false.
   - Goose/Events/VendorPurchaseInventoryEvent.cs:87: on false, log + `return` **before**
     `RollTitleAndSurname`/`AddAndAssignId` — safe for gold: the currency charge happens
     only after `AddItem` succeeds (:96-98), which we never reach.
   - Goose/Events/PlaceSpawnCommandEvent.cs:46: on false, send the "Gold item template is
     missing." message and `return` (Task 2's guard at this site is subsumed — fold them).

   **Excluded — no change.** Two further call sites ignore the new bool and are safe
   because their templates come from the validated `ItemHandler`: `Goose/Inventory.cs:1102`
   (combine — `CombinationHandler` loads `ResultItems` via `ItemHandler.GetTemplate`,
   CombinationHandler.cs:102, and throws at load on a missing template) and
   `Goose/GameWorld.cs:305` (gold — Task 1 already null-guards this site). A template
   that passed `ItemHandler.AddTemplate` validation passes `Validate` again, so both
   calls return `true`.
5. `ItemHandler.AddTemplate`: same reject shape as NPC (`Validate` → log + skip). Sheet item
   loads flow through this method too; a sheet row with null `BaseStats` previously NREd
   later in `Item.LoadFromTemplate` — now rejected at registration with a log.

**Step 1: Failing tests.**

```csharp
[Fact]
public void AddTemplate_BareTemplate_RegistersWithNormalizedFields()
{
    var template = new NPCTemplate { NPCTemplateID = 42, Name = "Bare", BaseStats = new AttributeSet() };
    world.NPCHandler.AddTemplate(template);

    NPCTemplate? loaded = world.NPCHandler.GetNPCTemplate(42);
    Assert.NotNull(loaded);
    Assert.Empty(loaded!.Allies!);
    Assert.Empty(loaded.Quests);
    Assert.Empty(loaded.Drops);
    Assert.Equal("", loaded.EquippedItems);
}
```
Expected RED: `loaded.Allies` is null (today AddTemplate stores the template verbatim).

```csharp
[Fact]
public void AddTemplate_MissingBaseStats_IsRejected()
{
    var template = new NPCTemplate { NPCTemplateID = 43, Name = "Bad", BaseStats = null! };
    world.NPCHandler.AddTemplate(template);

    Assert.Null(world.NPCHandler.GetNPCTemplate(43));
}
```
RED: template is registered today. Item variant in ItemHandlerRegistrationTests.cs
(`GetTemplate(43)` null after AddTemplate with `BaseStats = null!`).

Adversarial — the original NRE, reached via the **bypass** (direct SpawnNPC, no AddTemplate):
```csharp
[Fact]
public void SpawnNPC_InvalidTemplate_Direct_ReturnsNullWithoutNre()
{
    var template = new NPCTemplate { NPCTemplateID = 44, Name = "Bad", BaseStats = null!,
        ClassID = ClassId, Level = 50 };
    NPC? npc = world.NPCHandler.SpawnNPC(world, MapId, 5, 6, template, false);

    Assert.Null(npc);
    Assert.DoesNotContain(world.MapHandler.GetMap(MapId)!.NPCs, n => n.NPCTemplateID == 44);
}
```
RED today: NRE at NPC.cs:610 (propagates out of `SpawnNPC` — a direct call, not an event).

Item bypass pin:
```csharp
[Fact]
public void LoadFromTemplate_InvalidTemplate_ReturnsFalse()
{
    var item = new Item();

    Assert.False(item.LoadFromTemplate(new ItemTemplate { ID = 45, Name = "", BaseStats = null! }));
}
```
RED today: doesn't compile (void return) — the bool return is introduced in Step 3; after
that, RED is `Assert.False` failing (today's body would run and NRE at Item.cs:155).

**Step 2:** RED. **Step 3:** Implement items 1-5 (seam first where compile order forces it).
**Step 4:** GREEN + per-task gate.

**Mutation impact:** `ValidateAndNormalize` mutates the passed template (fills null
collections/strings). Source of truth: the template object the script or sheet load built.
Readers: everything that reads `NPCTemplate.Allies/Quests/Drops/EquippedItems`
(NPC.cs:309/:569/:1015, quest progress, drop rolls, MKC string parsing). No derived state
beyond what the script already published — the copy constructor (NPCTemplate.cs:250-258)
already handles both null and non-null `Allies`. Invariant: after `ValidateAndNormalize`
returns true, every downstream `!` on those fields is provably safe — for both the
registered-template path and the direct-`SpawnNPC` path.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Registered script templates are fully populated (collections, EquippedItems) | `AddTemplate_BareTemplate_RegistersWithNormalizedFields` |
| Templates missing BaseStats/Name are not published | `AddTemplate_MissingBaseStats_IsRejected` + item variant |
| Direct SpawnNPC with a bad template fails cleanly (bypass covered) | `SpawnNPC_InvalidTemplate_Direct_ReturnsNullWithoutNre` |
| `Item.LoadFromTemplate` signals failure instead of leaving a half-built item | `LoadFromTemplate_InvalidTemplate_ReturnsFalse` + the 8 call-site checks (mechanical, reviewed at commit) |

**Commit:** `fix: validate and normalize NPC/item templates at every entry point (deferred null bug #5 + sweep findings)`

---

### Task 6: Map and warp resolution (bug #1)

**Files:**
- Modify: `Goose/Map.cs:543-553` (warp tile load)
- Modify: `Goose/Events/MoveEvent.cs:111-124` (warp check)
- Modify: `Goose/Events/LoginContinuedEvent.cs:19-30` — add logger field
- Modify: `Goose/Events/DoneLoadingMapEvent.cs:22-30` — add logger field
- Modify: `Goose/GameWorld.cs:270-271` (starting-map validation after the Maps LoadStep — throws `FatalStartupException`)
- Modify: `Goose/Player.cs` (BoundMap sites :569, :701, :1432 + new private helper)
- Test: Create `Goose.Tests/MapWarpNullGuardTests.cs`; Create `Goose.IntegrationTests/MapWarpLoadTests.cs`

**Behavior:**
1. **Fail fast at map load (Map.cs:547).** `MapHandler.LoadMaps` is two-pass
   (MapHandler.cs:67 vs :74), so a null here genuinely means unknown `warp_id`:
   ```csharp
   Map? warpMap = world.MapHandler.GetMap(reader.GetInt32("warp_id"));
   if (warpMap is null)
   {
       log.Error("map {0}: warp tile at ({1},{2}) references unknown map {3}; tile skipped",
           mapId, reader.GetInt32("map_x"), reader.GetInt32("map_y"), reader.GetInt32("warp_id"));
       continue;
   }
   warp.WarpMap = warpMap;
   ```
   (Read the four columns into locals first; the existing code re-reads `map_x`/`map_y`
   after the `WarpMap` line.) The pre-existing tile at that coordinate stays.
2. **MoveEvent defense (MoveEvent.cs:113).** `if (warp.WarpMap is not null && warp.WarpMap.PlayerCanJoin(this.Player, world))` —
   the existing else branch (bounce back + `P.SetYourPosition`) handles the null case.
   No logger needed: the load-time log in item 1 is the visible signal.
3. **Login with a deleted saved map (LoginContinuedEvent.cs:21).**
   ```csharp
   Map? map = world.MapHandler.GetMap(this.Player.MapID);
   if (map is null)
   {
       log.Error("Player {0}: saved map {1} not found; falling back to starting map {2}",
           this.Player.Name, this.Player.MapID, world.Settings.StartingMapID);
       this.Player.MapID = world.Settings.StartingMapID;
       this.Player.MapX = world.Settings.StartingMapX;
       this.Player.MapY = world.Settings.StartingMapY;
       map = world.MapHandler.GetMap(world.Settings.StartingMapID);
   }
   if (map is null)
   {
       world.Send(this.Player, P.LoginDenied("Server maps are unavailable."));
       world.GameServer!.Disconnect(this.Player.Sock);
       return;
   }
   ```
   The `GameServer!`/`Disconnect` pattern matches LoginEvent.cs:94-114.
4. **DoneLoadingMapEvent defense (:24).** `Map? map = world.MapHandler.GetMap(this.Player.MapID); if (map is null) { log.Error(...); world.GameServer!.Disconnect(this.Player.Sock); return; }`
   — after item 3 this is unreachable; keep it so a future state can't strand a player in
   `LoadingMap` (the worst case from the severity model).
5. **Starting-map validation at startup (GameWorld.cs).** After the `"Maps"` LoadStep
   (Goose/GameWorld.cs:270-271), before `"Classes"`:
   ```csharp
   if (this.MapHandler.GetMap(this.Settings.StartingMapID) is null)
       throw new FatalStartupException("starting map " + this.Settings.StartingMapID + " not found");
   ```
   **Must throw, not return:** `GameWorld.Start` returning normally still lets
   `GameServer.Start` create the listen socket (Goose/GameServer.cs:150-153) before
   `GameLoop` exits — a bound-but-dead server. Throwing propagates to the
   `catch (FatalStartupException)` in `GameServer.Run` (Goose/GameServer.cs:92, wrapping
   `this.Start()` at :89): message printed, world stopped, clean exit **before** bind. (A server whose
   starting map doesn't exist cannot log anyone in or rebind anyone.) This makes the `!`
   in item 6 provable: the fallback target is guaranteed to exist for the whole process
   lifetime (maps are loaded once at startup; the reload command is commented out,
   Goose/Events/ReloadSQLCommandEvent.cs:24).
6. **BoundMap (Player.cs:569/:701/:1432).** Add one private helper and use it at all three
   sites (replacing `world.MapHandler.GetMap(this.BoundID)!`):
   ```csharp
   private Map ResolveBoundMap(GameWorld world)
   {
       Map? map = world.MapHandler.GetMap(this.BoundID);
       if (map is null)
       {
           log.Error("Player {0}: bound map {1} not found; rebinding to starting map", this.Name, this.BoundID);
           this.BoundID = world.Settings.StartingMapID;
           this.BoundX = world.Settings.StartingMapX;
           this.BoundY = world.Settings.StartingMapY;
           map = world.MapHandler.GetMap(this.BoundID);
       }
       // Starting map existence is validated at startup (GameWorld LoadStep chain).
       return map!;
   }
   ```
   (The one comment is justified per AGENTS.md: it documents a non-obvious `!` invariant.)
   The same startup validation covers `ChangeClass`'s `GetMap(StartingMapID)!`
   (Player.cs:1432).

**Mutation impact (item 3):** source of truth `Player.MapID/MapX/MapY` (persisted via
`BuildSave`, Player.cs:963+). Readers: the `SCM` packet sent two lines later
(`P.SendCurrentMap(map)`, LoginContinuedEvent.cs:26) and `DoneLoadingMapEvent`, which
re-reads `MapID` (:24). The mutation happens before the SCM send, so the client is told to
load exactly the map the player's state says it is on. Invariant: player coords are always
on the map the client is loading. Item 6 mutates `BoundID/BoundX/BoundY` — same reasoning
(bound map is sent to the client on bind/warp, never cached elsewhere).

**Step 1: Failing tests.**

Unit (`Goose.Tests/MapWarpNullGuardTests.cs`, TestWorldFixture):
```csharp
[Fact]
public void MoveOntoWarpTileWithNullTargetMap_BouncesPlayerBack()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    // Tile index is y * Width + x (Goose/Map.cs:551), NOT y * (Width+1) + x.
    map.tiles[2 * map.Width + 2] = new WarpTile { WarpMap = null!, WarpX = 5, WarpY = 5 };
    var player = fixture.CommandPlayerOn(map, 2, 3);

    fixture.RunCommand(player, "M1");

    Assert.Equal(3, player.MapY);
    Assert.Contains(player.Sent, s => s.StartsWith("SUP"));
}
```
Expected RED: today the NRE at MoveEvent.cs:113 is swallowed by `EventHandler.Update`'s
catch **after** `MoveTo`, so the player is left standing on the warp tile (`MapY == 2`).

```csharp
[Fact]
public void LoginContinued_SavedMapMissing_FallsBackToStartingMap()
{
    using var fixture = new TestWorldFixture(s => { s.StartingMapID = 1; s.MOTD = ""; });
    fixture.AddBaseMap(1, "start");
    var player = fixture.CommandPlayerOn(fixture.AddBaseMap(2, "other"), 1, 1);
    player.State = Player.States.LoadingGame;
    player.MapID = 999;

    fixture.RunCommand(player, "LCNT");

    Assert.Equal(1, player.MapID);
    Assert.Equal(Player.States.LoadingMap, player.State);
}
```
Expected RED: NRE at LoginContinuedEvent.cs:21 (swallowed) → `MapID` stays 999.
(`MOTD = ""` avoids the unrelated bug #19 NRE further down the handler — fixture settings
are code-built and don't go through the Task 4 loader.)

IT (`Goose.IntegrationTests/MapWarpLoadTests.cs`, PlayerFirstSaveTestBase pattern, schemas
`maps`, `warptiles`):
```csharp
[Fact]
public void LoadMaps_UnknownWarpTarget_SkipsTileWithoutThrowing()
{
    // settings must set ServerType = "Illutia" (code-built settings leave it null, which
    // routes LoadData to AsperetaMapLoader — Goose/Map.cs:529-531).
    // Write two minimal Illutia map files into {DataPath}/Maps/ (BinaryWriter):
    //   Int16 version, Int16 editorVersion, Int32 width, Int32 height,
    //   then per tile (y=1..h, x=1..w): Int32 flags, 5 × (Int32 graphic, Int16 sheet)
    //   (format: Goose/Map.cs:441-475). Use 5×5, all flags 0.
    // insert maps rows 1 and 2 (maps.sql minimal columns: map_id, map_filename, map_name);
    // warptiles rows: (map 1, 3,3 → warp_id 999) and (map 1, 4,4 → warp_id 2)
    world.MapHandler.LoadMaps(world);

    Assert.Null(world.MapHandler.GetMap(1)!.GetTile(3, 3));
    Assert.IsType<WarpTile>(world.MapHandler.GetMap(1)!.GetTile(4, 4));
    Assert.Equal(2, ((WarpTile)world.MapHandler.GetMap(1)!.GetTile(4, 4)!).WarpMap.ID);
}
```
Expected RED: NRE at Map.cs:547 propagates out of `LoadMaps` (Database.Execute rethrows
worker exceptions). `Map.LoadData` (Goose/Map.cs:519-536) does `File.Open` with no
existence check, so the map files **must** exist — generate them in the test as above;
do not weaken the assertion to skip `LoadMaps`.

**Step 2:** RED. **Step 3:** Implement items 1-5. **Step 4:** GREEN + per-task gate.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Unknown `warp_id` at load ⇒ tile skipped, valid warps intact, no throw | `LoadMaps_UnknownWarpTarget_SkipsTileWithoutThrowing` |
| Stepping on a null-`WarpMap` tile bounces the player, no NRE | `MoveOntoWarpTileWithNullTargetMap_BouncesPlayerBack` |
| Login with deleted saved map ⇒ starting map, client told the same map | `LoginContinued_SavedMapMissing_FallsBackToStartingMap` |
| `DoneLoadingMapEvent`/`BoundMap` guards | Code review (defense-in-depth behind items 3/5/6; same 3-line shape, exercised by the same data conditions) |
| Missing starting map ⇒ `FatalStartupException` before socket bind; `ResolveBoundMap`'s `!` is then provable | Code review: 2-line throw after the Maps LoadStep (GameWorld.cs:270) — exercising it needs a full startup data set (IT); the invariant it establishes is cited in the helper's comment |

**Commit:** `fix: resolve missing map references at load and login instead of NREing (deferred null bug #1)`

---

### Task 7: Class and level robustness (bugs #15, #16) + change-class pre-validation

**Files:**
- Modify: `Goose/ClassHandler.cs:33-75` (orphan-row `return` → log + continue; post-load contiguity validation **rejecting** gapped classes; new `internal static ValidateLevels`; `GetFallbackClass`) — add logger field
- Modify: `Goose/Class.cs` (add `internal IEnumerable<int> LevelIds`)
- Modify: `Goose/Player.cs:556-622` (LoadFromAutoCreate → bool), `:680-755` (LoadFromReader → bool), `:809` (AddPet null check), new `internal static ResolveClassAndLevel`
- Modify: `Goose/PlayerHandler.cs:197` (skip failed players) — add logger field
- Modify: `Goose/GameWorld.cs:273-274` (zero-valid-classes check after the Classes LoadStep — throws `FatalStartupException`)
- Modify: `Goose/Events/LoginEvent.cs:159` (reject login on auto-create failure)
- Modify: `Goose/NPC.cs:648-649` (LoadFromTemplate class/level check — extends Task 5's guard)
- Modify: `Goose/Pet.cs:199-262` (FromReader → `Pet?`) — add logger field
- Modify: `Goose/Events/ChangeClassCommandEvent.cs:42-55` (pre-validate before mutation)
- Modify: `Goose/Player.cs:1412-1454` (ChangeClass — pre-validate before mutation)
- Modify: `Goose/Packets.cs:29-48` (ExpBar)
- Modify: `Goose/Window.cs:267-274` (pet info display)
- Modify: `Goose/SpellEffect.cs:890-891` (tame success rate)
- Modify: `Goose/Events/BuyManaCommandEvent.cs:13`, `BuyVitaCommandEvent.cs:13`, `PetDamageCommandEvent.cs:63`, `PetVitaCommandEvent.cs:63`
- Modify: `TestSupport/TestWorldFixture.cs` (`SeedClassLevels` overload)
- Test: Create `Goose.Tests/ClassLevelNullGuardTests.cs`

**Behavior:**
1. **Orphan `class_info` row (ClassHandler.cs:69-72).** Today an unknown `class_id` hits a
   bare `return` inside the `Database.Execute` lambda, aborting **all** remaining level
   loading — which would also silently defeat the validation in item 2. Replace with
   `log.Error("class_info row for unknown class {0} skipped", c.ClassID); continue;`.
2. **Load-time contiguity validation — reject gapped classes.** A gapped class (e.g.
   levels {5,6,7}) is unsafe at too many deref sites to guard individually: `ExpBar`
   reads `GetLevel(Level - 1)` (Packets.cs:41), and both change-class paths relearn spells
   across intermediate levels (Player.cs:1445-1453, ChangeClassCommandEvent.cs:61-69). Add
   `internal IEnumerable<int> LevelIds => this.levels.Keys;` to `Class` (Goose/Class.cs,
   next to `GetLevel` :19), extract the check as a testable seam, and **remove** failing
   classes after the `class_info` loop in `LoadClasses` (both loops share one
   `Database.Execute` lambda, ClassHandler.cs:40-75 — the check must run after all level
   rows are loaded):
   ```csharp
   internal static bool ValidateLevels(Class c)
   {
       var ids = c.LevelIds.OrderBy(i => i).ToList();
       return ids.Count > 0 && ids[0] == 1 && ids[ids.Count - 1] == ids.Count;
   }

   var rejected = this.classes.Values.Where(c => !ValidateLevels(c)).Select(c => c.ClassID).ToList();
   foreach (int id in rejected)
   {
       log.Error("class {0} ({1}): level rows must be contiguous 1..N; class rejected",
           id, this.classes[id].ClassName);
       this.classes.Remove(id);
   }
   ```
   **Delay `RankHandler.AddClass` until validation passes.** Today the class_info loop
   registers each class in `RankHandler` (ClassHandler.cs:59) — before any level rows are
   loaded, so a rejected class would leave a stale `ClassRanks` entry (RankHandler.cs:31-34)
   pointing at a class `GetClass` no longer returns, which `RankCommandEvent`
   (Goose/Events/RankCommandEvent.cs:46+) would deref. Remove the `AddClass` call from the
   class_info loop and run it for the survivors after the rejection loop:
   ```csharp
   foreach (Class c in this.classes.Values)
       world.RankHandler.AddClass(c);
   ```
   Rejected classes then flow into the existing `GetClass`-null paths with no stale
   registrations anywhere: player load falls back (item 4), NPC spawn skips (item 6),
   change class reports "Invalid class name".
   **Zero valid classes must fail startup explicitly.** `LoadStep` only *logs* the count
   (GameWorld.cs:326-332) — a zero count does not abort. In `GameWorld.Start`, after the
   `"Classes"` LoadStep (GameWorld.cs:273-274):
   ```csharp
   if (this.ClassHandler.Count == 0)
       throw new FatalStartupException("no valid classes loaded; check the classes/class_info tables");
   ```
   Same throw-not-return rationale as Task 6 item 5: `GameServer.Start` binds the listen
   socket unconditionally after `gameworld.Start()` returns (GameServer.cs:150-153), so a
   plain `return` yields a bound-but-dead server; the throw is caught by `GameServer.Run`
   (GameServer.cs:92) and exits before bind. The runtime clamp in item 4 remains as
   defense-in-depth for the seam's contract.
3. **Fallback class (ClassHandler).** `public Class? GetFallbackClass() =>
   this.classes.Values.OrderBy(c => c.ClassID).FirstOrDefault();`
4. **Class/level resolution seam (Player).** Extract the resolution into
   `internal static bool ResolveClassAndLevel(Player player, GameWorld world)` — used by
   **both** `LoadFromReader` and `LoadFromAutoCreate` at their class/level lookup sites:
   ```csharp
   internal static bool ResolveClassAndLevel(Player player, GameWorld world)
   {
       Class? cls = world.ClassHandler.GetClass(player.ClassID);
       if (cls is null)
       {
           cls = world.ClassHandler.GetFallbackClass();
           if (cls is null)
           {
               log.Error("Player {0}: no classes loaded; load failed", player.Name);
               return false;
           }
           log.Error("Player {0}: class {1} not found; using fallback class {2}",
               player.Name, player.ClassID, cls.ClassID);
           player.ClassID = cls.ClassID;   // keep the persisted row and scripts consistent
       }
       player.Class = cls;

       var levelIds = cls.LevelIds.OrderBy(i => i).ToList();
       if (levelIds.Count == 0)
       {
           log.Error("Player {0}: class {1} has no level rows; load failed", player.Name, player.ClassID);
           return false;
       }
       var atOrBelow = levelIds.Where(i => i <= player.Level).ToList();
       int validLevel = atOrBelow.Count > 0 ? atOrBelow[atOrBelow.Count - 1] : levelIds[0];
       if (validLevel != player.Level)
           log.Error("Player {0}: level {1} missing for class {2}; loading at level {3}",
               player.Name, player.Level, player.ClassID, validLevel);
       player.Level = validLevel;
       return true;
   }
   ```
   This establishes the real invariant — **after load, `Class.GetLevel(Level)` is
   non-null** — by clamping to an *existing* level row (highest ≤ saved, else the lowest
   existing), not to the literal 1. An empty class table or a class with no level rows
   fails the load instead of producing an unplayable player.
   **Placement:** the seam replaces only the
   `this.Class = world.ClassHandler.GetClass(this.ClassID)!;` line at each call site. The
   following `this.MaxStats += this.Class.GetLevel(this.Level)!.BaseStats;` stays where it
   is (the seam makes its `!` safe), and the rest of each method runs unchanged — both
   methods continue past the lookup (AutoCreate: `BodyState` + starting items;
   Reader: `ToggleSettings`/`AetherThreshold`), so an early `return false` skips only the
   tail of initialization of a player the caller discards.
5. **Load paths return bool; callers fail cleanly.**
   - `LoadFromReader` (Player.cs:680): at :754, replace the `GetClass` line with
     `if (!ResolveClassAndLevel(this, world)) return false;`, keep the `MaxStats +=` line,
     and add `return true;` as the method's final statement (the method continues past
     :755). `PlayerID` is read at :688, before the seam, so the caller's `CurrentID`
     bookkeeping stays correct.
   - `PlayerHandler.LoadPlayerData` (PlayerHandler.cs:197) — `PlayerHandler` has no logger
     today; add one (see Loggers in Verified APIs):
     ```csharp
     bool ok = player.LoadFromReader(world, reader);
     if (player.PlayerID >= this.CurrentID) this.CurrentID = player.PlayerID + 1;
     if (!ok) { log.Error("Player row {0} failed to load; skipped", player.PlayerID); continue; }
     ```
     (a failed player is simply not registered — login reports "doesn't exist"; the
     second-pass `LoadAdditional` at :211-214 skips them automatically).
   - `LoadFromAutoCreate` (Player.cs:556): same shape at :621 —
     `if (!ResolveClassAndLevel(this, world)) return false;`, keep the `MaxStats +=` line,
     `return true;` at the end.
   - `LoginEvent` (LoginEvent.cs:159): on false, reject the login via the existing
     denial path (`P.LoginDenied` + disconnect, per LoginEvent.cs:94-114).
6. **NPC.LoadFromTemplate (:648-649).** Extend Task 5's guard:
   `this.Class = world.ClassHandler.GetClass(this.ClassID); if (this.Class is null || this.Class.GetLevel(this.Level) is null) { log.Error("NPC template {0}: class {1}/level {2} not found; spawn skipped", …); return false; }`
7. **Pet.FromReader → `Pet?`.** (:209/:256-257) `pet.Class = world.ClassHandler.GetClass(pet.ClassID); if (pet.Class is null || pet.Class.GetLevel(pet.Level) is null) { log.Error("pet {0}: class {1}/level {2} not found; pet skipped", …); return null; }`.
   Caller Player.cs:809: `Pet? pet = Pet.FromReader(reader, world); if (pet is not null) this.AddPet(pet);`
8. **Change-class pre-validation (both paths mutate before they validate today).**
   - `ChangeClassCommandEvent` (ChangeClassCommandEvent.cs:42-55): insert **before**
     `player.ClassID = newClass.ClassID`:
     ```csharp
     if (player.Class.GetLevel(player.Level) is null || newClass.GetLevel(player.Level) is null)
     {
         world.Send(this.Player, P.ServerMessage("Cannot change class: level data missing."));
         return;
     }
     ```
     (the body's two lookups — old class at the player's level, :45, and new class at the
     same level after the swap, :55 — are now proven non-null; the relearn loop
     :61-69 is bounded by `MaxLevel` and safe by contiguity, item 2.)
   - `Player.ChangeClass` (Player.cs:1412-1454): the body performs four lookup groups —
     old@oldLevel (:1418), **old class**@(newLevel-1) when newLevel > 1 (:1426 — `Level`
     is reassigned at :1419 but `Class` is not swapped until :1428, so the deref is the
     *old* class), new@newLevel (:1436), and new@1..min(newLevel, MaxLevel) in the
     relearn loop (:1445-1453 — safe by contiguity, item 2). Insert **before**
     `RemoveStats` (:1416):
     ```csharp
     Class? dest = world.ClassHandler.GetClass(classid);
     if (this.Class.GetLevel(this.Level) is null || dest is null ||
         dest.GetLevel(newLevel) is null ||
         (newLevel > 1 && this.Class.GetLevel(newLevel - 1) is null))
     {
         log.Error("ChangeClass rejected for {0}: missing level data (class {1} level {2} -> class {3} level {4})",
             this.Name, this.ClassID, this.Level, classid, newLevel);
         return;
     }
     ```
     Note the `newLevel - 1` check is on `this.Class` (old), matching the deref at :1426 —
     checking `dest` there would leave the old-class deref unguarded.
     Callers (QuestWindow.cs:435 rebirth reward, Player.cs:1463 wrapper) get a logged no-op
     instead of a partially-changed player.
9. **ExpBar (Packets.cs:29-48).** At the top: `if (player.Class.GetLevel(player.Level) is null) return "TNL0,0,0," + player.ExperienceSold;`
   (flat empty bar; this packet goes out on every vitals update, so no logging here).
   The guard checks `GetLevel(Level)` first, which is also the first deref in the body —
   so a missing current level never reaches the `GetLevel(Level - 1)` deref at :41, and
   for a contiguous class with `Level ≥ 2` that deref is valid by construction (item 2).
10. **Pet info window (Window.cs:267-274).** Fetch once: `ClassLevel? level = pet.Class.GetLevel(pet.Level);`
    then `if (level is not null && level.Experience == 0) {…sold line…} else if (level is not null) {…next-level line…}` (skip the line entirely when null).
11. **Tame success rate (SpellEffect.cs:890-891).** `player.Class.GetLevel(player.Level)!.BaseStats.HP` →
    `((player.Class.GetLevel(player.Level)?.BaseStats.HP) ?? 0)` (same for MP).
12. **Buy commands ×4.** `GetLevel(this.Player.Level)!.Experience != 0` →
    `GetLevel(this.Player.Level)?.Experience != 0` — a missing level row now rejects the
    command (null ≠ 0) instead of NREing. Same one-token change in all four files.
13. **No change:** `Player.ProcessLevelUp` (Player.cs:1787-1830 — verified safe),
    `Pet.FromCharacter` (Pet.cs:151/:166 — uses the tamer's clamped class/level).

**Test support:** `TestWorldFixture.SeedClass` only seeds contiguous 1..maxLevel
(TestWorldFixture.cs:141-149). Add an overload using the same reflection insert:
`public void SeedClassLevels(int classId, string name, int[] levels)` (each with
`BaseStats = new AttributeSet(), Spells = new List<Spell>()`).

**Step 1: Failing tests** (`Goose.Tests/ClassLevelNullGuardTests.cs`, TestWorldFixture):

```csharp
[Fact]
public void ExpBar_MissingLevelRow_ReturnsFlatBar()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 1, 1);
    player.Level = 99;

    Assert.Equal("TNL0,0,0,0", P.ExpBar(player));
}
```
Expected RED: NRE at Packets.cs:32.

```csharp
[Fact]
public void SpawnNPC_UnknownClass_ReturnsNull()
{
    using var fixture = new TestWorldFixture();
    fixture.AddBaseMap(1, "m");
    var template = new NPCTemplate { NPCTemplateID = 1, Name = "x", ClassID = 999,
        Level = 5, BaseStats = new AttributeSet() };

    Assert.Null(fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 2, 2, template, false));
}
```
RED: NRE at NPC.cs:649. (Depends on Task 5's guard shape — sequence Task 7 after Task 5.)

```csharp
[Fact]
public void ValidateLevels_GappedOrEmpty_ReturnsFalse()
{
    Class Build(int[] levels)
    {
        var cls = new Class { ClassID = 1, ClassName = "t" };
        foreach (int l in levels)
            cls.AddLevel(new ClassLevel { Level = l, BaseStats = new AttributeSet(), Spells = new List<Spell>() });
        return cls;
    }

    Assert.False(ClassHandler.ValidateLevels(Build(new[] { 5, 6, 7 })));
    Assert.False(ClassHandler.ValidateLevels(Build([])));
    Assert.True(ClassHandler.ValidateLevels(Build(new[] { 1, 2, 3 })));
}
```
This is the seam test for item 2's rejection (green on introduction); the adversarial
property is that a {5,6,7} class — the shape that NREs `ExpBar` at Packets.cs:41 — never
reaches `ClassHandler.classes`.

```csharp
[Fact]
public void ResolveClassAndLevel_EmptyClassTable_ReturnsFalse()
{
    using var fixture = new TestWorldFixture();
    var classes = (Dictionary<int, Class>)typeof(ClassHandler)
        .GetField("classes", BindingFlags.NonPublic | BindingFlags.Instance)!
        .GetValue(fixture.World.ClassHandler)!;
    classes.Clear();
    var player = new Player(0) { Name = "t", ClassID = 0, Level = 5 };

    Assert.False(Player.ResolveClassAndLevel(player, fixture.World));
}
```
(The test file needs `using System.Reflection;` — same private-field access `SeedClass`
uses, TestWorldFixture.cs:141-149.)

```csharp
[Fact]
public void ResolveClassAndLevel_ClassWithoutLevelOne_ClampsToLowestExisting()
{
    using var fixture = new TestWorldFixture();
    // bypasses the load-time rejection on purpose — pins the seam's clamp contract
    fixture.SeedClassLevels(9, "Gapped", new[] { 5, 6, 7 });
    var player = new Player(0) { Name = "t", ClassID = 9, Level = 3 };

    Assert.True(Player.ResolveClassAndLevel(player, fixture.World));
    Assert.Equal(5, player.Level);
    Assert.NotNull(player.Class.GetLevel(player.Level));
}

[Fact]
public void ResolveClassAndLevel_MissingClass_FallsBackAndUpdatesClassId()
{
    using var fixture = new TestWorldFixture();
    var player = new Player(0) { Name = "t", ClassID = 999, Level = 5 };

    Assert.True(Player.ResolveClassAndLevel(player, fixture.World));
    Assert.Equal(0, player.ClassID);
    Assert.NotNull(player.Class.GetLevel(player.Level));
}
```
RED: seam doesn't exist (compile) — introduced in Step 3; the adversarial properties
(empty table, gapped class, ClassID consistency) are what the tests pin.

```csharp
[Fact]
public void PetFromReader_UnknownClass_ReturnsNull()
{
    using var fixture = new TestWorldFixture();
    var reader = new FakeDbDataReader(new Dictionary<string, object>
    {
        ["pet_id"] = 1, ["pet_title"] = "", ["pet_name"] = "p", ["pet_surname"] = "",
        ["pet_level"] = 5, ["class_id"] = 999, ["experience"] = 0L, ["experience_sold"] = 0L,
        ["body_id"] = 1, ["body_r"] = 0, ["body_g"] = 0, ["body_b"] = 0, ["body_a"] = 0,
        ["face_id"] = 1, ["hair_id"] = 1, ["hair_r"] = 0, ["hair_g"] = 0, ["hair_b"] = 0, ["hair_a"] = 0,
        ["pet_hp"] = 100L, ["pet_mp"] = 10L, ["pet_sp"] = 10L,
        ["stat_ac"] = 0, ["stat_str"] = 0, ["stat_sta"] = 0, ["stat_int"] = 0, ["stat_dex"] = 0,
        ["res_fire"] = 0, ["res_air"] = 0, ["res_earth"] = 0, ["res_spirit"] = 0, ["res_water"] = 0,
        ["weapon_damage"] = 0L,
        // every column Pet.FromReader reads (Pet.cs:199-262) — the fake reader's name
        // indexer throws KeyNotFoundException on a missing key (FakeDbDataReader.cs:15)
    });

    Assert.Null(Pet.FromReader(reader, fixture.World));
}
```
RED: NRE at Pet.cs:209. (FakeDbDataReader is name-indexed; if the method reads further
columns past :262, add them in the red phase.)

```csharp
[Fact]
public void ChangeClass_MissingDestinationLevel_RejectsBeforeMutation()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 1, 1);
    fixture.SeedClassLevels(7, "Short", new[] { 1, 2, 3 });
    fixture.World.ClassHandler.GetClass(0)!.GetLevel(50)!.BaseStats.HP = 200;
    player.Level = 50;   // class 0 (fixture default) has levels 1..50
    player.MaxStats.HP = 200;
    long maxBefore = player.MaxStats.HP;

    player.ChangeClass(7, 50, fixture.World, 0.07);

    Assert.Equal(0, player.ClassID);
    Assert.Equal(50, player.Level);
    Assert.Equal(maxBefore, player.MaxStats.HP);
}
```
RED: NRE at Player.cs:1436 (new class 7 has no level 50) **after** `ClassID`/`Level`/
`MaxStats` are already mutated. The non-zero source stat is what makes "no mutation"
provable: `CommandPlayerOn` leaves `MaxStats` all-zero, so with zero stats the assertion
would pass even though `MaxStats -= GetLevel(50).BaseStats` (Player.cs:1418) already
subtracted — any fix that swallows the NRE after that line still fails `MaxStats.HP`
(200 → 0).
(`player.Level = 50` is required: `CommandPlayerOn` leaves it at 0, and the pre-fix body
would then NRE at the *source*-level deref, Player.cs:1418, instead of the intended
destination-level path.)

```csharp
[Fact]
public void ChangeClass_MissingOldClassIntermediateLevel_RejectsBeforeMutation()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 1, 1);
    int[] src = new int[49];
    for (int i = 0; i < 48; i++) src[i] = i + 1;
    src[48] = 50;   // level 49 missing — the deref at Player.cs:1426
    fixture.SeedClassLevels(8, "Src", src);
    fixture.SeedClassLevels(7, "Dst", Enumerable.Range(1, 50).ToArray());
    var srcClass = fixture.World.ClassHandler.GetClass(8)!;
    srcClass.GetLevel(50)!.BaseStats.HP = 200;
    player.Class = srcClass;
    player.ClassID = 8;
    player.Level = 50;
    player.MaxStats.HP = 200;
    long maxBefore = player.MaxStats.HP;

    player.ChangeClass(7, 50, fixture.World, 0.07);

    Assert.Equal(8, player.ClassID);
    Assert.Equal(50, player.Level);
    Assert.Equal(maxBefore, player.MaxStats.HP);
}
```
Exercises the **old-class** guard specifically: the destination has level 50, so only
`this.Class.GetLevel(newLevel - 1)` (Player.cs:1426, old class, level 49) is missing.
RED pre-fix: NRE at :1426; the non-zero source stat makes the mutation observable the
same way (`MaxStats -= GetLevel(50).BaseStats` at :1418 drops `MaxStats.HP` 200 → 0
before the NRE).

Buy-command pin (direct `Ready` call so the exception propagates):
```csharp
[Fact]
public void BuyVita_MissingLevelRow_RejectsCommand()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 1, 1);
    player.Level = 99;
    var ev = new BuyVitaCommandEvent { Player = player };

    ev.Ready(fixture.World);
}
```
RED: NRE at BuyVitaCommandEvent.cs:13. (`Ready` requires `State == Ready` —
CommandPlayerOn sets it — and parses `Data` in an internal try/catch, so a bare event
works.)

**Step 2:** RED. **Step 3:** Implement items 1-13. **Step 4:** GREEN + per-task gate.

**Mutation impact (item 4):** `Player.Class`/`Player.ClassID`/`Player.Level` are load-time
state feeding `MaxStats`, `ExpBar`, quest checks, and the save row (`class_id`,
`player_level`). The fallback/clamp happens before any derived stat is computed (both load
methods set Class early), so everything downstream sees the corrected value; `ClassID` is
updated in memory so scripts and the next save agree with the in-memory `Class` (the
persisted row keeps its original bad `class_id` until the next save — the log line is the
data-repair signal; auto-rewriting saved rows at load is out of scope). Invariant: after a
successful load, `Class.GetLevel(Level)` is non-null for every online player; after a
failed load, the player is not online at all.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| `ExpBar` never NREs on a missing level row | `ExpBar_MissingLevelRow_ReturnsFlatBar` (hottest site) |
| NPC spawn with unknown class/level fails cleanly | `SpawnNPC_UnknownClass_ReturnsNull` |
| Pet with unknown class is skipped, not NRE | `PetFromReader_UnknownClass_ReturnsNull` |
| Empty class table ⇒ player load fails cleanly (no unplayable player) | `ResolveClassAndLevel_EmptyClassTable_ReturnsFalse` (seam; the LoadFromReader/LoginEvent wiring is the same bool checked at two call sites — reviewed at commit) |
| Class without level 1 ⇒ clamps to lowest existing level; `ClassID` stays consistent with `Class` | `ResolveClassAndLevel_ClassWithoutLevelOne_ClampsToLowestExisting`, `ResolveClassAndLevel_MissingClass_FallsBackAndUpdatesClassId` |
| Change class with missing level rows ⇒ rejected with **no mutation** | `ChangeClass_MissingDestinationLevel_RejectsBeforeMutation` (destination-level branch) + `ChangeClass_MissingOldClassIntermediateLevel_RejectsBeforeMutation` (old-class `newLevel - 1` branch, Player.cs:1426 — the two guards check different classes, so both need their own test). The ChangeClassCommandEvent guard is the same pre-validation shape at ChangeClassCommandEvent.cs:42 — reviewed at commit; its `world.Send` to the GM needs a second registered player, which the direct-method test covers more tightly |
| Zero valid classes ⇒ `FatalStartupException` before socket bind; no stale `ClassRanks` entry for a rejected class | Code review: the throw is a 2-line check after the Classes LoadStep (GameWorld.cs:273-274); the `RankHandler.AddClass` move is the same loop boundary as the rejection — both need a full startup data set to exercise (IT), reviewed at commit |
| Buy commands reject on missing level row | `BuyVita_MissingLevelRow_RejectsCommand` (+ same one-token `!`→`?` ×3) |
| Gapped class levels ({5,6,7}) never reach `ClassHandler.classes` — `ExpBar`'s `GetLevel(Level-1)` and the change-class relearn loops stay safe | `ValidateLevels_GappedOrEmpty_ReturnsFalse` (seam; the `LoadClasses` removal is the same predicate applied at load — reviewed at commit) |
| Orphan `class_info` row no longer aborts remaining level loading | Deferred from dedicated test: the bare `return` → log + `continue` at ClassHandler.cs:71 is a load-path change needing a full classes sheet (IT); one statement, reviewed at commit |

**Commit:** `fix: guard class/level lookups against missing rows and validate change-class before mutation (deferred null bugs #15, #16)`

---

### Task 8: Map-transition event guard + final doc pass (bug #2)

**Files:**
- Modify: `Goose/Event.cs` (new `internal bool ClientOriginated`)
- Modify: `Goose/EventHandler.cs:297-309` (set the flag in `AddEvent(Player, string)`), `:361-382` (Update) + new `internal int DroppedDuringMapLoad`
- Modify: `docs/plans/2026-08-25-nullable-inventory.md` (latent bugs section — all items)
- Test: Create `Goose.Tests/MapTransitionEventGuardTests.cs`

**Behavior:** `Player.Map` is null in two windows: initial login (state `LoadingGame` from
LoginEvent.cs:251 until `DoneLoadingMapEvent` sets `Map` at DoneLoadingMapEvent.cs:78) and
warps (state `LoadingMap`, `Map` nulled in `WarpTo` at Player.cs:1374). Rather than guard
~169 call sites, guard at the single event-execution chokepoint `EventHandler.Update`
(Goose/EventHandler.cs:361-382), immediately after the `Dequeue`:

```csharp
if (ev.ClientOriginated && ev.Player is Player p &&
    ((p.State == Player.States.LoadingGame && ev is not (LoginContinuedEvent or PlayerPongEvent)) ||
     (p.State == Player.States.LoadingMap && ev is not (DoneLoadingMapEvent or PlayerPongEvent))))
{
    this.DroppedDuringMapLoad++;
    log.Debug("Dropped {0} for {1} (state {2}).", ev.GetType().Name, p.Name, p.State);
    continue;
}
```

**Client-originated only.** The filter must not touch internal scheduled events: they
also carry `Player` (`BuffTickEvent`, `BuffExpireEvent` copy it into their re-queued
instances — BuffTickEvent.cs:60-62, BuffExpireEvent.cs:28-31), and dropping them is not
harmless. `BuffExpireEvent.Ready` removes the buff at expiry and reschedules only when
*not yet* expired (BuffExpireEvent.cs:18-35) — a dropped at-expiry event is never
re-queued, so the buff would become permanent. `ClientOriginated` is set to `true` in
both construction branches of `AddEvent(Player, string)` (EventHandler.cs:297-309 — the
`EventFactory` branch and the factory+`Player`/`Data` branch); internal events go through
`AddEvent(Event)` and keep the default `false`.

**Why execution time, not enqueue time:** `Update` drains *all* due events in one call
(:364-381), and warps happen **inline** inside a handler (`MoveEvent` → `WarpTo`,
MoveEvent.cs:117). Two packets can be enqueued while the player is `Ready`; the first
event warps the player (Map = null), and the second — already queued — would still NRE.
Checking state at enqueue time (the earlier draft of this task) does not close that race;
checking at execution time does, and it also covers events with future ticks that come due
mid-load. The `PriorityQueue` has no efficient removal, so purging queued player events on
warp is the wrong shape; a per-event guard at dequeue is one flag + `is`-pattern check.

- `LCNT` (LoginContinuedEvent) and `DLM` (DoneLoadingMapEvent) are the only state-advancing
  packets in each window; `PONG` only touches `LastPing` (PlayerPongEvent.cs:13-19).
  Type patterns are used because `Event` has no name property (Goose/Event.cs:8-28).
- NPC events have `Player == null` at runtime — the `ev.Player is Player p` pattern skips
  them (they are unaffected by the player's state).
- `DroppedDuringMapLoad` is an `internal` counter (same seam style as `Count`,
  EventHandler.cs:317) so tests can assert drops deterministically; `InternalsVisibleTo`
  covers Goose.Tests (Goose.csproj:19-25).
- `NotLoggedIn` needs no guard (socket disconnected, player unregistered).
- **No enqueue-time filter is added** — the execution-time guard subsumes it; events queued
  during a load are dropped when their tick comes, and same-tick events are dropped in the
  same drain.

**Mutation impact:** none — no state is mutated; client-originated events are simply not
run during the two windows. Invariant: while `Player.Map` is null, no *client-originated*
handler that dereferences it can execute from the queue — regardless of when the event was
enqueued; state-advance packets always get through; internal scheduled events
(buff tick/expire, pet events) run exactly as before.

**Step 1: Failing tests** (`Goose.Tests/MapTransitionEventGuardTests.cs`):

```csharp
[Fact]
public void TwoMovesEnqueuedBeforeWarp_SecondIsDropped()
{
    using var fixture = new TestWorldFixture();
    var map1 = fixture.AddBaseMap(1, "a");
    var map2 = fixture.AddBaseMap(2, "b");
    map1.tiles[2 * map1.Width + 2] = new WarpTile { WarpMap = map2, WarpX = 1, WarpY = 1 };
    var player = fixture.CommandPlayerOn(map1, 2, 3);

    fixture.World.EventHandler.AddEvent(player, "M1");
    fixture.World.EventHandler.AddEvent(player, "M1");
    fixture.World.EventHandler.Update(fixture.World);

    Assert.Equal(Player.States.LoadingMap, player.State);
    Assert.Null(player.Map);
    Assert.Equal(1, fixture.World.EventHandler.DroppedDuringMapLoad);
}
```
Expected RED: the second `M1` executes against `Map == null` → NRE swallowed by the
existing catch; `DroppedDuringMapLoad` doesn't exist (compile) — the counter is introduced
in Step 3, after which the RED is `Assert.Equal(1, …)` failing with 0 (the NRE is
contained, so the test fails on the counter, not the crash). This is the race the
enqueue-time draft missed: both events were enqueued while `Ready`.

```csharp
[Fact]
public void Update_DuringLoadingMap_DropsNonDlmPlayerEvents()
{
    using var fixture = new TestWorldFixture();
    var player = new Player(0) { Name = "t", State = Player.States.LoadingMap };

    fixture.World.EventHandler.AddEvent(player, "M1");
    fixture.World.EventHandler.Update(fixture.World);

    Assert.Equal(1, fixture.World.EventHandler.DroppedDuringMapLoad);
}
```
(Enqueued via `AddEvent(Player, string)` so the event is `ClientOriginated` — a
directly-constructed `MoveEvent` would not be filtered by design.)
RED: counter missing / 0.

```csharp
[Fact]
public void Update_DuringLoadingMap_AllowsPong()
{
    using var fixture = new TestWorldFixture();
    var player = new Player(0) { Name = "t", State = Player.States.LoadingMap };

    fixture.World.EventHandler.AddEvent(player, "PONG");
    fixture.World.EventHandler.Update(fixture.World);

    Assert.Equal(0, fixture.World.EventHandler.DroppedDuringMapLoad);
}
```
(DLM is not enqueued here because `DoneLoadingMapEvent.Ready` needs a valid `Player.MapID`
map; the allowlist membership is the same `is` pattern — the PONG case proves the
non-drop path and the regression test below proves Ready-state passthrough.)

```csharp
[Fact]
public void Update_DuringLoadingMap_RunsInternalExpireEvent()
{
    using var fixture = new TestWorldFixture();
    var player = new Player(0) { Name = "t", State = Player.States.LoadingMap };
    var effect = fixture.AddBaseSpellEffect(1, "d0", e => e.Duration = 0);
    var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };
    player.Buffs.Add(buff);
    var expire = new BuffExpireEvent { Player = player, Data = buff, Ticks = 0 };
    buff.BuffExpireEvent = expire;

    fixture.World.EventHandler.AddEvent(expire);
    fixture.World.EventHandler.Update(fixture.World);

    Assert.DoesNotContain(player.Buffs, b => b == buff);
    Assert.Equal(0, fixture.World.EventHandler.DroppedDuringMapLoad);
}
```
Pin test (green in both phases): a zero-duration buff is "expired" on first run
(`TimeNow - TimeCast >= 0 * TimerFrequency`), so `BuffExpireEvent.Ready` takes the
removal branch. This test fails against the flag-less guard draft — dropping the event
would leave the buff in `player.Buffs` forever, the permanent-buff regression.
(`Duration = 0` also means `Player.AddBuff` never creates the expire event itself, hence
the manual `buff.BuffExpireEvent = expire`.)

```csharp
[Fact]
public void Update_WhenReady_DropsNothing()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m");
    var player = fixture.CommandPlayerOn(map, 2, 3);

    fixture.RunCommand(player, "M1");

    Assert.Equal(0, fixture.World.EventHandler.DroppedDuringMapLoad);
    Assert.Equal(2, player.MapY);
}
```

**Step 2:** RED. **Step 3:** Implement the guard + counter. **Step 4:** GREEN + per-task gate.

**Final doc pass (same commit or a follow-up `docs:` commit):**
In `docs/plans/2026-08-25-nullable-inventory.md`, rewrite the "Latent bugs (deferred)"
section: mark each of #1-#19 with its resolution and the fixing task/commit from this plan
(#6/#12 closed-verified; #2 fixed by the Task 8 execution-time guard with the ~169-site
decision, the enqueue-vs-execution race, and the client-originated-only scoping (internal
buff events must keep running during the windows) recorded; #11/#13 kept as accepted design —
internal events use `NPC`/`Data`, contract documented). Record the sweep findings: null
`Allies`/`Quests`/`EquippedItems` on script templates — **fixed** (Task 5); null
`ItemSlot.Item` — **fixed** (Task 2); `RenewBuff` not resyncing `BuffExpireEvent` when
`BuffStacksOver` swaps effects — **new deferred behavioral item** (a buff first applied
with duration 0 becomes permanent if renewed with a duration>0 effect; fix requires a
behavioral decision on whether renewal should extend/replace the expiry, not just an NRE
guard).

**Full verification (after the doc pass):**
1. `dotnet build Goose.sln --no-incremental -p:WarningsAsErrors=nullable` → exit 0.
2. `dotnet test Goose.sln` → all green (Goose.Tests ≥ 341 + new, Tools.Tests 124+).
3. `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj` → ≥ 219 + new, 0 failed.
4. `grep -c "warning" ` on a `--no-incremental` sln build: only the 21 known
   non-nullable warnings (19 CS0168, 1 xUnit1012, 1 CA1416) — no new diagnostics of any kind.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Events already queued while `Ready` are dropped once an earlier event in the same drain warps the player | `TwoMovesEnqueuedBeforeWarp_SecondIsDropped` (the enqueue-time draft's gap) |
| No client event schedules a handler while `Player.Map` is null | `Update_DuringLoadingMap_DropsNonDlmPlayerEvents` |
| State-advance + keepalive packets always pass | `Update_DuringLoadingMap_AllowsPong` |
| Internal scheduled events (buff expire/tick) still run during the windows — no permanent buffs | `Update_DuringLoadingMap_RunsInternalExpireEvent` (pin; fails against the flag-less guard) |
| No over-filtering in normal play | `Update_WhenReady_DropsNothing` (regression) |
| Full-suite parity | Full verification steps 1-4 |

**Commit:** `fix: drop queued player events during map load instead of NREing in handlers (deferred null bug #2)` + `docs: resolve deferred null bug inventory`

---

## Task ordering and dependencies

- Tasks 1-4 are independent; any order.
- **Task 5 before Task 7** (Task 7's NPC guard extends Task 5's `LoadFromTemplate` guard).
- Task 6 is independent (its `LoginContinuedEvent` test sets `MOTD = ""` to sidestep the
  pre-Task-4 fixture null — fixture settings are code-built, so the explicit setting stays
  regardless of task order).
- Task 8 last (its doc pass summarizes everything).
