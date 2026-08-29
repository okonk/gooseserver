# Nullable Reference Types — Classified Warning Inventory

Captured at HEAD `8e72576` using a temporary local `<Nullable>enable</Nullable>`
edit to `Goose/Goose.csproj` (the permanent enable landed in `93f41d4`, after
this capture).
This is the load-bearing artifact for the Nullable Reference Types Adoption Plan: Tasks 2–4
fix warnings by area and reduce the per-area counts below to zero; Task 5 proves the build is
warning-free. If any task stalls, this inventory is sufficient to hand the work to a fresh
plan: every warning has file:line, code, message, and area.

## Gate decision (Task 1 Step 3)

The prescribed capture (`grep -E "warning CS8[5-9][0-9]{2}" | sort | wc -l`) returned a raw
count of **978**, above the 500 threshold. However, the MSBuild log emits **every warning
line exactly twice** (the compiler output block is repeated), verified line-for-line.
Deduplicated (`sort -u`) counts:

| Scope | Raw (wc -l) | Unique |
|---|---|---|
| `dotnet build Goose.sln` (Goose + Goose.Tests + tools) | 978 | **489** (Goose 442 + Goose.Tests 47) |
| + `Goose.IntegrationTests` (separate build, not in the sln) | — | **502** total at baseline (+13; **39** after Task 2 — 13 baseline + 26 cascades, see area 6) |

The plan owner accepted the deduplicated count as the metric on 2026-08-25: 489 ≤ 500, so
the Step 3 gate does **not** trip and the plan proceeds. All counts in this document are
deduplicated unique warnings.

`Goose.IntegrationTests` is deliberately omitted from `Goose.sln` (fast-test-boundary
plan), so its warnings (13 at baseline, 39 after Task 2) are only visible when building
the project directly:
`dotnet build Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-incremental`.
They are listed under area 6, clearly marked. (The IT count includes 4 `TestSupport`
warnings because the IT project *links* `TestSupport/TestWorldFixture.cs` and
`ScriptStub.cs` via `Compile Include`; those same 4 texts also appear in the
Goose.Tests build.)

## Capture procedure

```bash
dotnet build Goose.sln --no-incremental > /tmp/nullable-inventory-build.txt 2>&1   # exit 0
grep -E "warning CS8[5-9][0-9]{2}" /tmp/nullable-inventory-build.txt | sort -u
dotnet build Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-incremental
grep -E "warning CS8[5-9][0-9]{2}" <log> | sort -u
```

The `CS8[5-9]xx` range is a deliberate superset of the nullable diagnostics. In practice
**only CS86xx codes appeared** — no CS85xx/CS87xx/CS89xx codes (e.g. CS8701/CS8702) were
emitted, so no non-nullable codes needed excluding.

### Per-code breakdown (per-scope entries: 489 in `Goose.sln` + 13 in `Goose.IntegrationTests` = 502)

The counts below are **per-scope entries** and sum to 502. Distinct warning **texts**
are **498**: 4 `TestSupport` texts appear in both the Goose.Tests and the
IntegrationTests builds, so CS8625 is 102 distinct / 106 per-scope (all other codes are
identical under both counts).

| Code | Meaning | Count |
|---|---|---|
| CS8618 | Non-nullable field/property must contain a non-null value when exiting constructor | 189 |
| CS8625 | Cannot convert null literal to non-nullable reference type | 106 |
| CS8600 | Converting null literal or possible null value to non-nullable type | 101 |
| CS8603 | Possible null reference return | 51 |
| CS8602 | Dereference of a possibly null reference | 32 |
| CS8604 | Possible null reference argument | 11 |
| CS8601 | Possible null reference assignment | 9 |
| CS8605 | Cannot use null-forgiving operator on expression of non-nullable type | 2 |
| CS8620 | Argument cannot be used for parameter specified as [AllowNull] | 1 |
| | **Total** | **502** |

## Areas and targets

1. **Model construction** — entity/model constructors and properties that must be set
   before use (`Player`, `NPC`, `Item`, quest models, …). Fix: annotate non-nullable +
   `!` at the construction site where non-null is provable, or `T?` where absence is real.
2. **Database row mapping** — `FromReader`/`FromRow` paths, `ExecuteScalar`/
   `Convert.ToString` sites, `JsonHelper.Deserialize` of DB-serialized columns. Includes
   the one named `!`: `DataReaderExtensions.GetString`'s body — see note below.
3. **Collections containing nullable slots** — `List<T?>`/`Dictionary<K, V?>` where
   absence is represented by null elements or a failed lookup (inventory/equipped slots,
   map tiles, spell slots, registry lookups in handlers).
4. **Packet/event inputs** — `Event.Data`/event payload properties, command-argument
   strings, packet payload parsing and construction in `Goose/Events/*`,
   `Goose/Packets.cs`, the console command surface, and connection/socket handling.
5. **Script-facing APIs** — the Roslyn scripting boundary (`ScriptHandler`, `Script`,
   `Base*Script`, `GooseSettings` script surface) where nullability crosses into compiled
   scripts.
6. **Tests and fakes** — `Goose.Tests`, `Goose.IntegrationTests`, linked
   `TestSupport/*`.

| Area | Unique warnings (target for Tasks 2–4) | Status |
|---|---|---|
| 1. Model construction | 0 (was 218) | fixed by Task 2 |
| 2. Database row mapping | 0 (was 37) | fixed by Task 2 |
| 3. Collections containing nullable slots | 0 (was 80) | fixed by Task 3 |
| 4. Packet/event inputs | 0 (was 74) | fixed by Task 3 |
| 5. Script-facing APIs | 0 (was 28) | fixed by Task 4 (5 entries had already been fixed as side effects of Task 3: `PickupItemEvent.cs:91/94` and `Map.cs:605/608` `string? refusal` locals; `QuestWindow.GetScriptCannotCompleteMessage` → `string?`) |
| 6. Tests and fakes (76 Goose.Tests + 232 Goose.IntegrationTests) | 0 (was 308) | fixed by Task 4 |
| **Total** | **0** (was 502) | sln 0 + IT 0 |

### Task 2 completion record

Areas 1 + 2 fixed with annotation-only changes (no control-flow changes, no
`required`/`init`, no `?? default`). Verification at the Task 2 commit:

- `dotnet build Goose.sln --no-incremental` exit 0; **233** unique nullable warnings
  remain (489 baseline − 256 fixed). All 255 area 1/2 inventory entries are gone;
  zero new warnings versus the baseline set **for the sln build**. (The plan's earlier
  "234 remaining" projection assumed 255 fixes; Task 2 also eliminated one area 6
  warning as a side effect.)
- Tests: Goose.Tests 341 passed / 0 failed; Tools.Tests 124 passed / 0 failed.
- `Goose.IntegrationTests` (not in the sln) still compiles, exit 0, but now emits
  **39** unique nullable warnings: the 13 baseline entries plus **26 new cascade
  warnings** at consuming sites of the new `?` annotations (e.g. `Assert.Single` /
  `Enumerable.Single` on now-nullable `NPCTemplate.Drops`/`Allies`). The
  `NPCTemplate.BaseStats` `= null!` unification also silently resolved 5 potential
  cascade sites (`BaseStats.HP` derefs in `DimensionsScriptTests.cs:74, 117, 175,
  225, 288`), so the cascade count is 26 rather than the 31 originally recorded.
  Per the orchestrator decision the remaining IT sites are **not** annotated in
  Task 2 — they are area 6 / Task 4 scope. The 26 cascade sites are listed in
  area 6, marked "Task 2 cascade — fix in Task 4".

Annotation strategy used:

- Property set at construction and dereferenced unguarded elsewhere → `= null!`
  (e.g. `NPCTemplate.Name`, `NPCTemplate.BaseStats`, `ItemTemplate.BaseStats`,
  `GameWorld.LoginThrottle`).
- Property genuinely nullable at runtime → `T?`, with `!` at the few unguarded
  dereference sites where non-null is provable by construction or flow
  (e.g. `NPCTemplate.VendorItems` — `!` at `Window.cs` populate and
  `VendorPurchaseInventoryEvent`, which run only for vendor NPCs; `NPC.AggroTarget`
  — `!` in `HandleAttackEvent`, which runs only while the NPC has an attack event).
- Property genuinely nullable but with a massive unguarded dereference fan-out
  (`Player.Map`/`Pet.Map`, ~169 sites; `Player.Map` null during map transition,
  `Pet.Map` null after logout) → kept non-nullable `= null!` and recorded the
  latent bug below instead of churning 169 call sites.

Non-obvious `!` proofs (beyond the per-site ones above):

- `DataReaderExtensions.GetString`: `Convert.ToString(reader[column])!` — for every
  SQLite cell value (null, `DBNull`, string, numeric, `byte[]`) `Convert.ToString`
  returns a non-null string; the class is `internal`.
- `Inventory.Load`/`Spellbook.Load`/`Player.LoadQuests`/`PlayerBank`: `!` on
  `Convert.ToString(ExecuteScalar())` and `JsonHelper.Deserialize<…>(…)!` — the row
  is known to exist (looked up by the player's own id); a missing row is the latent
  bug recorded below.
- `PlayerBank.cs:39` deserializes bank slots as `ItemSlot[]` while
  `Inventory.Load` (Inventory.cs:913/932/963) uses `ItemSlot?[]` for the
  same JSON shape — the runtime `is null` guard works either way; recorded
  as a known cosmetic inconsistency (no change made).
- `Player.LoadQuests`: `questStatus.Started!`/`Completed!`/`Progress!`
  (Player.cs:824–838) — `BuildSaveQuests` (Player.cs:1159–1162) always populates
  all three arrays before serializing, so a deserialized row always has them set.
- `NPC.HandleAttackEvent`: `this.Allies!.Contains(…)` — sheet-loaded NPCs always get
  an (possibly empty) `Allies` list from `NPCHandler`.
- `SpellEffect.CastScriptSpell`: `this.Script!.Object` — reached only for
  `EffectTypes.Script`, which always has a script at load; a failed load is the
  latent bug recorded below (the NRE is caught and logged by the existing try/catch).
- Roslyn note: `?.` on a property (e.g. `buff.SpellEffect?.Script`) resets the
  compiler's tracked null state for that property, so a following plain dereference
  of the same property needs its own `!` (NPC.cs `OnMeleeHit`/`OnMeleeAttack`,
  Player.cs equivalents).

Classification convention: a warning is assigned to the area that owns the nullability
contract being fixed. E.g. `this.NPC.MoveEvent = null` inside `NPCMoveEvent` is area 1
(the `NPC.MoveEvent` property is what gets annotated), while `Event`'s own payload
properties (`Player`, `Data`, `NPC`) are area 4.

### Task 3 completion record

Areas 3 + 4 fixed with annotation-only changes (no control-flow changes, no
`required`/`init`, no `?? default`). Verification at the Task 3 commit:

- `dotnet build Goose.sln --no-incremental` exit 0; **104** unique nullable
  warnings remain. All 154 area 3/4 inventory entries are gone; the remainder
  is area 5 (28) + area 6 sln-scope (76). The plan's projection was 79 (233 −
  154); the actual is 104 = 79 + 32 new area 6 test cascades − 2 area 6
  side-effect fixes (`CurrencyHandlerTests` null literals, now legal
  `Resolve(ItemTemplate?, NPC?)` args) − 5 area 5 side-effect fixes
  (`PickupItemEvent.cs:91/94`, `Map.cs:605/608` `string? refusal` locals;
  `QuestWindow.GetScriptCannotCompleteMessage` → `string?`).
- Tests: Goose.Tests 341 passed / 0 failed; Tools.Tests 124 passed / 0 failed.
- `Goose.IntegrationTests` (not in the sln) compiles, exit 0, and now emits
  **232** unique nullable warnings (was 39): +192 new cascade warnings at
  consuming sites of the new `?` annotations, plus
  `DimensionsScriptTests.cs(288,87)` re-emerging (the Task 2 record marked it
  FIXED, but `dim5` became nullable once `NPCHandler.GetNPCTemplate` returned
  `NPCTemplate?`). Per the task decision the test/IT sites are **not**
  annotated in Task 3 — they are area 6 / Task 4 scope. The new sites are
  listed in area 6, marked "Task 3 cascade — fix in Task 4".

Annotation strategy used:

- Slot collections → element-nullable arrays: `Inventory`/`ItemContainer`
  `ItemSlot?[]`, `Map.tiles` `ITile?[]` (+ `GetTile` → `ITile?`), `Spellbook`
  `Spell?[]`, `PlayerHandler.idToPlayer` `Player?[]`. Registry/slot lookups
  return `T?`: `GetSlot`/`GetEquippedSlot`/`RemoveItem`, `GetTile`,
  `GetCharacterAt`, `GetPlayer`/`GetPlayerFromData`, `GetMap`,
  `GetNPCTemplate`/`SpawnNPC`, `GetClass`, `Class.GetLevel`, `GetTemplate`,
  `GetTitle`/`GetSurname`/`RollModifier`, `GetSpell`/`GetSpellEffect`/
  `GetSpellByName`, `QuestHandler.Get`, `GuildHandler.GetGuild`,
  `CurrencyHandler.Get`/`Resolve(ItemTemplate?, NPC?)`, `LoginThrottle`
  out-params, `ConsoleCommandParser.Parse` → `ParsedCommand?`,
  `ItemContainerWindow.GetSlot`/`SetSlot` → `ItemSlot?`.
- `Event.Player`, `Event.NPC`, `Event.Data` kept non-nullable `= null!` —
  mirroring the Task 2 `Player.Map` decision: ~572 `this.Player` / ~29
  `this.NPC` / ~78 `Data`-cast unguarded derefs; making them `?` would cascade
  into every event handler. Latent bugs recorded below.
- `NPCTemplate.VendorItems` stays `NPCVendorSlot[]?` — its element-nullability
  sites were not area 3 entries.
- `!` at call sites where non-null is provable: guarded lookups
  (`if (x is null) return;`), validated slots (`VendorSellInventoryEvent`
  `sellslot!` — the stack is pre-validated and the game loop is single-
  threaded), validated command slots (`CustomCommandEvent.cs:61-63,127`
  `combineBag.GetSlot(n)!` — provably safe: slot 1 is validated at `Ready`
  entry (lines 14–19) and slots 2/3 by `ValidateCustomSlots` before each
  use), static packet funcs (`MakePetCharacter!`/`UpdatePet!` —
  non-readonly static fields read inside a lambda are "possibly null" to the
  compiler; both are initialized at type load), `RemoteEndPoint!.ToString()!`
  (Roslyn taints the result of a call on a possibly-null receiver, so both `!`
  are needed), and registry lookups where a miss is a data bug (recorded
  below).
- Non-obvious `!` proof: `SetConfigCommandEvent.cs:31/33/41/42` `getter!`/
  `setter!` on the `GetGetMethod()`/`GetSetMethod()` results — provable
  because all 127 `GooseSettings` accessors are `get; set;` pairs (a future
  one-way property would NRE silently).

New area 6 cascades (fix in Task 4): 32 sln-scope (Goose.Tests/TestSupport) +
193 IT (192 listed in the Task 3 cascade list + 1 re-emergence,
`DimensionsScriptTests.cs(288,87)`, tracked in the Task 2 section) — listed
in area 6.

### Task 4 completion record

Areas 5 + 6 fixed with annotation-only changes (no control-flow changes, no
`required`/`init`, no `?? default`). Verification at the Task 4 commit:

- `dotnet build Goose.sln --no-incremental` exit 0; **0** unique nullable
  warnings (was 104 = area 5 28 + area 6 sln-scope 76).
- `dotnet build Goose.IntegrationTests/Goose.IntegrationTests.csproj
  --no-incremental` exit 0; **0** unique nullable warnings in the
  `[...Goose.IntegrationTests.csproj]` scope (was 232). The only remaining
  diagnostics in that log are 19 pre-existing CS0168 (unused variable),
  outside the nullable range.
- Tests: Goose.Tests 341 passed / 0 failed; Tools.Tests 124 passed / 0 failed.

Annotation strategy used:

Area 5 (script-facing boundary, 28 entries):

- `GooseSettings`'s 12 string properties → `= null!`: the only construction
  path is `GooseSettingsLoader` JSON deserialization and the shipped
  `Goose/GooseSettings.json` sets every field; an operator-edited file missing
  a field is recorded as latent bug #19. `GooseSettingsLoader.Load` →
  `Deserialize(...)!` (`JsonSerializer.Deserialize` throws on invalid input
  and returns an instance for a valid object document).
- `Script<T>.Object` → `= default!` plus `!` at the
  `Activator.CreateInstance(scriptType)!` assignment (CreateInstance throws or
  returns an instance; `Object` is null only before `Load()`, and every
  consumer goes through `ScriptHandler.Get`, which loads).
- `ScriptHandler.Get` local → `IScript? script = null` (`TryGetValue` out).
- Null is a real state in the script contracts, so `?`, not `!`:
  `IItemScript.CanPickup` / `IMapScript.CanPlayerJoin` /
  `IQuestScript.CanComplete` → `string?` (null = allow; the `Base*Script`
  defaults return null), `ISpellEffectScript.GetItemDescription` →
  `IEnumerable<string>?` (null = fall through to the built-in description),
  `BaseMapScript.GetDynamicTile` → `DynamicTile?` (null = no dynamic tile),
  and `SpellEffect.ScriptItemDescription` → `List<string>?`. These are
  annotation-only at the Roslyn boundary: compiled scripts (`.csx`) dispatch
  dynamically and observe identical behavior (the null returns are unchanged).

Area 6 (tests and fakes, 308 entries):

- `!` at fixture construction sites (fixtures control construction — the norm):
  `TestWorldFixture` `GetClass(0)!` (classes 0/1/3 are seeded in the
  constructor before `World` is exposed), `AddBaseSpell`'s
  `GetSpellEffect(effectId)!` (tests add the effect immediately before), and
  `Action<T>? configure = null` on the optional fixture parameters.
- Integration tests: `!` on lookups of entities the same test registered
  moments earlier — `MapHandler.GetMap(id)!` (`AddBaseMap` or a dimension
  clone created by the shipped `OnLoaded`), `ClassHandler.GetClass(n)!`,
  `ItemHandler.GetTemplate(id)!`, `NPCHandler.GetNPCTemplate(id)!`,
  `SpellHandler.GetSpell/GetSpellEffect(id)!`, `CurrencyHandler.Get("spirit")!`
  (registered by the shipped `OnLoaded` — pinned by
  `SpiritCurrencyTests.OnLoaded_RegistersSpirit`), `Inventory.GetSlot(n)!`
  (the item was just placed by the purchase under test), `Script!.Object`
  (the test asserts that template/map ships its script), and
  `NPCTemplate.VendorItems!` in `DimensionVendorStockTests.StockOf` (the base
  merchant always has `VendorItems` assigned by the fixture, and dimension
  clones inherit the array via the copy constructor, NPCTemplate.cs:256).
- `?` where null is a real local state the test exercises:
  `DimensionItemScriptTests` `Item? item = null` (the no-roll path leaves it
  null), `GlobalScriptFixture` `Run(Action<GlobalScriptFixture>? arrange = null)`.
- `PlayerPropertiesPersistenceTests`: `Convert.ToString(ExecuteScalar())!` —
  the row was inserted/updated by the same test immediately before (the
  load-path variant of this pattern is latent bug #7).
- Tests asserting null-on-miss of the new `T?` lookups
  (`Assert.Null(GetTemplate(50)!.Script)`, `Assert.Null(GetTemplate(1)!.CurrencyId)`,
  `Assert.Null(script.CanPickup(...))`) were annotated, not changed — the
  assertion is the contract.

New latent bug recorded: #19 (`GooseSettings` string properties null when the
settings JSON omits the field).

Empty-string divergence across the three script gates (pre-existing, unchanged by
the annotations): `IItemScript.CanPickup` and `IMapScript.CanPlayerJoin` consumers
check `is not null`, so a script returning `""` refuses with an empty message,
while `IQuestScript.CanComplete` treats `""` as allow (`!string.IsNullOrEmpty`).
Noted so script authors don't port habits across the three gates.

### Task 5 completion record

Plan complete. Removed the three now-redundant per-file `#nullable enable`
directives (line 1 of `Goose/Trie.cs`, `Goose/PropertiesDictionary.cs`,
`Goose/PropertiesDictionaryJsonConverter.cs` — the project enables NRT globally
since Task 1; the directives were pre-project-wide opt-ins). Removal changed
nothing: no new warnings appeared.

Final verification (2026-08-26):

- Zero-proof: `dotnet build Goose.sln --no-incremental
  -p:WarningsAsErrors=nullable` exit 0 — any nullable diagnostic (including
  codes outside any grep range) would fail that build (a build run with the
  flag). The 21 remaining warnings are all outside the nullable group: 19
  pre-existing CS0168 (unused variable) in Goose.csproj, 1 xUnit1012
  (Goose.Tests/PlayerPropertiesTests.cs, analyzer diagnostic), 1 CA1416
  (Tools.Tests/BundleStageTests.cs, analyzer diagnostic).
- `dotnet test Goose.sln` (TRX at `/tmp/goose-final-trx/`,
  `LogFilePrefix=final`): Goose.Tests 341 passed / 0 failed; Tools.Tests 124
  passed / 26 skipped / 0 failed — counts match the pre-plan baseline exactly.
- TRX identity diff vs `/tmp/goose-pre-nullable-trx/` (testName + outcome
  from every `<UnitTestResult>` element, 491 recorded results per capture =
  341 Goose.Tests + 150 Tools.Tests; the 26 skipped Tools.Tests tests are
  recorded by the trx logger as `outcome="NotExecuted"` in each capture):
  **empty** — every recorded test identity and outcome is identical to the
  baseline.
- Integration suite (not in the sln-only baseline; bar is green + a sensible
  count): `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj`
  — **219 passed / 0 failed**.

Deferred: a permanent `<WarningsAsErrors>nullable</WarningsAsErrors>` in
`Goose/Goose.csproj` is deliberately not adopted now (per the plan) so a
transient build break can't mask a real regression while the codebase is
young. The zero-proof command above is the re-verification procedure to run
when checking for nullable-warning regressions; the 21 remaining non-nullable
warnings are outside the `nullable` group and do not block adopting the flag
later.

### Area 2 note: `DataReaderExtensions.GetString`

`Goose/DataReaderExtensions.cs(14)` — `GetString` returns non-nullable `string` (null and
`DBNull` cells both yield `""`); the baseline body `Convert.ToString(reader[column])`
(convert result is `string?`) emitted CS8603. Task 2 added the plan's one named `!`:
`Convert.ToString(reader[column])!`. Proof: for every SQLite-supported cell value (null,
`DBNull`, string, numeric, `byte[]`) `Convert.ToString` returns a non-null string, and
the class is `internal`, so no external `DbDataReader` implementation can reach it.

## Pre-plan test baseline

- Location: `/tmp/goose-pre-nullable-trx/` (TRX, `LogFilePrefix=pre`); log
  `/tmp/goose-pre-nullable-test.log`.
- Captured at `8e72576` before the csproj change. Exit 0: Goose.Tests 341 passed /
  0 failed; Tools.Tests 124 passed / 26 skipped / 0 failed.
- (The older `/tmp/goose-baseline-trx` predates the fast-test-boundary split and the
  DbDataReader migration commits; do not use it.)

## Latent bugs (deferred)

Behavior-affecting findings recorded while annotating. None were fixed by the
annotation work; each was suppressed with `!` at the site(s) noted. Status as of
the `deferred-null-ref-fixes` branch:

1. **`WarpTile.WarpMap` is null when `MapHandler.GetMap` fails to resolve the target
   map** — NRE at `MoveEvent.cs:113` (`warp.WarpMap.PlayerCanJoin`). `WarpMap` is
   non-nullable `Map` with `= null!` (Goose/WarpTile.cs:7); the move path does not
   guard. The same `MapHandler.GetMap` failure (unknown id) also NREs at
   DoneLoadingMapEvent.cs:24–28 (`map.PlaceCharacter`) and, via the `!` sites Task 3
   added, at `Player.BoundMap` (Player.cs:569/701/1432 — warping to a bound spot
   with an invalid `bound_id`) and `LoginContinuedEvent.cs:21` (unguarded
   `P.SendMapFlags(map)`). `NPC.LoadFromTemplate` (NPC.cs:599) has the same pattern
   but is covered by its existing `is null` check.
   — **Fixed (Task 6, `5e21b9f` + `b3bf2b2`)**: no NRE on any of these paths. An
   unknown `warp_id` in a `warptiles` row is skipped at map load (logged; the
   pre-existing tile at that coordinate stays), and stepping on a null-`WarpMap`
   warp tile at runtime bounces the player back to their previous position
   (MoveEvent else-branch). A saved map (login) or bound map that no longer
   exists falls back to the starting map (`LoginContinuedEvent`,
   `Player.ResolveBoundMap`); the login fallback is asserted to reach the client
   (`MapWarpNullGuardTests`).
2. **`Player.Map` is null during map transition; `Pet.Map` is null after logout**
   — ~169 unguarded dereferences across the codebase assume the map is set for the
   lifetime of the object. Kept non-nullable `= null!` to avoid churning every call
   site. For `Player`, `Map` is null only between `LoadingMap` nulling it
   (Player.cs:1374) and `DoneLoadingMapEvent` reassigning it
   (DoneLoadingMapEvent.cs:78); logout guards on `player.Map is not null`
   (LogoutEvent.cs:42) and never nulls it. `Pet.Map` is null after logout
   (Pet.cs:536).
   — **Fixed for the player map-transition window (Task 8, `587cb6a`)**: rather than guarding the ~169 call sites, the
   single event-execution chokepoint `EventHandler.Update` drops client-originated
   events at **execution time** (immediately after `Dequeue`) while
   `Player.State` is `LoadingGame` (allowing only `LCNT`/`PONG`) or `LoadingMap`
   (allowing only `DLM`/`PONG`). Execution time, not enqueue time: `Update` drains
   all due events in one call and a warp happens **inline** inside an earlier
   event's `Ready` (`MoveEvent` → `WarpTo`), so an event enqueued while `Ready`
   can still execute against `Map == null` — an enqueue-time filter misses that
   race; the per-event guard at dequeue closes it and also covers events with
   future ticks that come due mid-load. Client-originated only
   (`Event.ClientOriginated`, set in both construction branches of
   `AddEvent(Player, string)`): internal scheduled events (`BuffTickEvent`, `BuffExpireEvent`) also
   carry `Player`, and `BuffExpireEvent.Ready` reschedules only when *not yet*
   expired — dropping an at-expiry event during a load would leave the buff
   permanent, so they must keep running in both windows. Drops are counted in
   `EventHandler.DroppedDuringMapLoad`. The post-logout window is **not** covered
   by this fix: after logout (`NotLoggedIn`) the player is disconnected but queued
   client events can still run with `Map == null`, contained only by the per-event
   catch. That is a retained accepted risk (the plan explicitly decided
   `NotLoggedIn` needs no guard, since logout already guards on
   `player.Map is not null`) and a follow-up candidate.
3. **Tick-type buff with `Duration == 0`** — `BuffExpireEvent` is only created when the
   duration is positive, so `NPC.AddBuff` (NPC.cs:1523) and `Player.AddBuff`
   (Player.cs:2194) NRE on `buff.BuffExpireEvent!.Ticks` for a zero-duration
   Tick/Viral/Root/Stun effect.
   — **Fixed (Task 1, `eaa9e2f`)**: zero-duration tick-type effects no longer
   schedule an expire event and the tick path guards the null.
4. **`NPC` kill-reward path NREs when the damages dictionary is empty** —
   `NPC.cs:1171` dereferences the "highest damager" lookup result, which is null when
   no damage entries exist.
   — **Fixed (Task 3, `a079dfd`)**: the kill-reward path guards the empty case.
5. **`NPC.LoadFromTemplate` NREs on a code-built template with null `BaseStats`**
   (NPC.cs:610, `template.BaseStats`; the property is non-nullable `= null!`, like
   `ItemTemplate.BaseStats`, which has the identical NRE path in
   `Item.LoadFromTemplate`). Sheet-loaded templates always have stats; only
   hand-built templates can trip this.
   — **Fixed (Task 5, `2432657` + `53dd798`)**: NPC/item templates are validated
   and normalized at every entry point, and `LoadFromTemplate` results are
   checked at the gold and combine sites.
6. **`NPC.HandleAttackEvent` NREs if `AggroTarget` is cleared while the attack event
   is still pending** (NPC.cs:1290/1301/1318/1320, `this.AggroTarget!`) —
   **Closed 2026-07-09.** Verified: `NPCAttackEvent.Ready` guards before dispatch
   (Goose/Events/NPCAttackEvent.cs:11-12), so `HandleAttackEvent` never runs with a
   cleared aggro target.
7. **Missing DB row on load throws** — `Inventory.Load` (Inventory.cs:913/932/963),
   `Spellbook.Load` (Spellbook.cs:43), `Player.LoadQuests` (Player.cs:822) and
   `PlayerBank` (PlayerBank.cs:39–40) force `Convert.ToString(ExecuteScalar())!` /
   `JsonHelper.Deserialize<…>(…)!`; a missing row makes `ExecuteScalar()` return
   null, `Convert.ToString(null)` returns null, and `JsonSerializer.Deserialize(null)`
   throws `ArgumentNullException` (a NULL/empty `serialized_data` cell in
   `PlayerBank` throws `JsonException`).
   — **Fixed (Task 2, `f7b43de` + `ca4df7c`)**: missing, empty, or corrupt player
   data rows load as empty instead of throwing; `GetGold` fails soft when the
   gold item is disabled.
8. **`NPC.VendorItems` is null for non-vendor NPCs** — the only guard is
   `PlayerRightClickEvent.cs:58`; other vendor-window paths (`Window.cs` populate,
   `VendorPurchaseInventoryEvent`) use `!` and assume a vendor NPC.
   — **Fixed (Task 3, `a079dfd`)**: the remaining vendor-window paths guard
   `VendorItems`.
9. **`SpellEffect.OnMeleeHitSpell` / `OnMeleeAttackSpell` are null when the
   `*SpellID` column is 0** — NRE at NPC.cs:1696/1712 and Player.cs:2487/2503 if an
   OnMeleeHit/OnAttack effect has no linked spell.
   — **Fixed (Task 1, `eaa9e2f`)**: the melee reaction paths guard the null
   linked spell.
10. **`SpellEffect.Script` is null when the script file fails to load** —
    `CastScriptSpell` (SpellEffect.cs:1060) NREs on `this.Script!.Object`; the existing
    try/catch logs it.
    — **Fixed (Task 1, `eaa9e2f`)**: `CastScriptSpell` guards the null script.
11. **`Event.Player` is null for internal (non-player) events** — `Event.Player`
    is kept non-nullable `= null!` (Task 3, mirroring the `Player.Map` decision):
    ~572 unguarded `this.Player` derefs across event handlers. Only
    `LoginEvent.Ready` guards (`if (this.Player is not null) return;`); an
    internal event (NPC/pet/guild/macro-fired) with a null `Player` NREs at the
    first `this.Player` use.
    — **Accepted design (kept)**: internal events use `NPC`/`Data` rather than
    `Player`; the contract is documented, so no per-event guards are added.
12. **`Event.NPC` is null for player-originated events** — same treatment
    (~29 `this.NPC` derefs); only `RegenEvent` guards on `this.NPC is not null` —
    **Closed 2026-07-09.** Verified: all five consumer events guard or are safe by
    construction — NPCAttackEvent.cs:11-12, NPCMoveEvent.cs:13-14, RegenEvent
    (`this.NPC is not null`), BuffExpireEvent.cs:24-30 (the `this.NPC` branch is
    only reached when `buff.Target is NPC`, which implies `this.NPC` is set),
    NPCSpawnEvent.cs:9.
13. **`Event.Data` is null for internal events that set no payload** — kept
    non-nullable `Object` `= null!` (an `Object?` would cascade into ~78 cast
    sites); handlers that cast `(string)this.Data` / `(Socket)this.Data` etc.
    NRE or throw `InvalidCastException` if the event fires without a payload.
    — **Accepted design (kept)**: same contract as #11 — internal events that
    need a payload are documented to set one; no per-event guards are added.
14. **Unknown item template id → `ItemHandler.GetTemplate` returns null** —
    NRE sites: `Inventory.Load` (Inventory.cs:921/940/972; NRE later in
    `RefreshStats`/item ops), `PlayerBank` load (PlayerBank.cs:47 →
    `RefreshStats`), the gold item in `GameWorld` (GameWorld.cs:305) and
    `PlaceSpawnCommandEvent.cs:46` (`LoadFromTemplate(null)`). Guarded,
    warning-only sites (a working `is null` check skips the entry — no NRE):
    the spell-effect link (ItemHandler.cs:116 — a bad effect id logs and
    skips the template), SpellHandler.cs:258, NPCHandler.cs:167/189, and
    `NPC.LoadFromTemplate` (NPC.cs:599). Same class as #7: a bad id in saved
    data / settings.
    — **Fixed (Task 2, `f7b43de` + `ca4df7c`)**: unknown template ids in saved
    data are skipped and the gold path fails soft.
15. **Unknown `ClassID` → `ClassHandler.GetClass` returns null** — the `!`
    assignments are NPC.cs:648, Pet.cs:209/256, Player.cs:621/754/1428; the
    NRE occurs at the subsequent `this.Class.GetLevel(...)!` dereference
    (NPC.cs:649, Pet.cs:257, Player.cs:622/755/1436).
    — **Fixed (Task 7, `f431514` + `340ed49`)**: class lookups are guarded
    against missing rows and change-class validates before mutating.
16. **`Class.GetLevel` returns null for an unregistered level → NRE** at the ~25
    `!` sites (Packets.cs `ExpBar`, the Player/Pet level-up loops, NPC/Pet stat
    application, Window pet display, SpellEffect.cs:890/891, BuyMana/BuyVita/
    ChangeClass/Pet command events).
    — **Fixed (Task 7, `f431514` + `340ed49`)**: level lookups are guarded at
    the dereference sites.
17. **Quest requirement display NREs on deleted templates** —
    QuestWindow.cs:217/222/227 (`item!.Name`, `talkNPC!.Name`, `killNpc!.Name`):
    a quest requirement pointing at a removed item/NPC template NREs when the
    quest window renders.
    — **Fixed (Task 3, `a079dfd`)**: the quest window guards deleted templates.
18. **`NPC.Quests` can contain null entries** — NPCHandler.cs:111
    (`QuestHandler.Get(q)!`): a bad quest id in an NPC sheet's quest list puts
    a null element into `NPC.Quests`, NREing quest progress/completion paths.
    — **Fixed (Task 3, `a079dfd`)**: bad quest ids in NPC sheets are filtered
    out.
19. **`GooseSettings` string properties are null when the settings JSON omits the
    field** — Task 4 annotated the 12 string properties `= null!` (the only
    construction path is `GooseSettingsLoader` JSON deserialization, and the shipped
    `Goose/GooseSettings.json` sets every field). An operator-edited settings file
    missing a field leaves the property null and NREs at the unguarded derefs, e.g.
    `LoginContinuedEvent.cs:34` (`MOTD.Length`), `Player.cs:638`
    (`StartingItems.Split(' ')`), `LoginEvent.cs:255` (`ServerName`),
    `GuildCreateCommandEvent.cs:50/55` (`DefaultGuildMOTD`), `GameServer.cs:159`
    (`GameServerIP`). Same class as #7/#14: bad operator data, unguarded. The
    `!` at `GooseSettingsLoader.cs:54` also covers a settings file containing
    the JSON literal `null` (Deserialize → null → the `GameServer` ctor's
    existing `ArgumentNullException`).
    — **Fixed (Task 4, `28860ae`)**: missing string fields default to empty.

### Sweep findings (Task 5)

- Null `Allies`/`Quests`/`EquippedItems` on script-built templates — **fixed**
  (Task 5, `2432657`): templates are normalized at every entry point.
- Null `ItemSlot.Item` — **fixed** (Task 2, `f7b43de` + `ca4df7c`): slots with
  no item load as empty.
- `RenewBuff` does not resync `BuffExpireEvent` when `BuffStacksOver` swaps
  effects — **new deferred behavioral item**: a buff first applied with
  duration 0 (no expire event) becomes permanent if renewed with a duration>0
  effect. Fixing requires a behavioral decision on whether renewal should
  extend or replace the expiry, not just an NRE guard.


## Classified inventory

Format: `file(line,col): warning CSxxxx: message`. Paths relative to the repository root.

### Area 1 — Model construction (218) — FIXED by Task 2

All 218 entries below were eliminated by annotation-only changes; entries are retained
for traceability (line numbers refer to the `8e72576` baseline).

Goose/Buff.cs(11,27): warning CS8618: Non-nullable property 'Caster' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Buff.cs(12,27): warning CS8618: Non-nullable property 'Target' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Buff.cs(13,28): warning CS8618: Non-nullable property 'SpellEffect' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Buff.cs(16,22): warning CS8618: Non-nullable property 'BuffExpireEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Class.cs(14,23): warning CS8618: Non-nullable property 'ClassName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ClassLevel.cs(26,29): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ClassLevel.cs(28,28): warning CS8618: Non-nullable property 'Spells' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Combination.cs(9,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Combination.cs(16,35): warning CS8618: Non-nullable property 'ResultItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Combination.cs(18,37): warning CS8618: Non-nullable property 'RequiredHash' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Currency/CurrencyHandler.cs(41,25): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Currency/CurrencyHandler.cs(42,48): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/BuffExpireEvent.cs(18,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/LogoutEvent.cs(56,49): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/MacroCheckEvent.cs(15,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/MacroConfirmCommandEvent.cs(28,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/NPCAttackEvent.cs(9,36): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/NPCMoveEvent.cs(13,34): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetAttackEvent.cs(10,31): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetAttackEvent.cs(21,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetAttackEvent.cs(29,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetMoveEvent.cs(10,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetMoveEvent.cs(34,34): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/ToggleCommandEvent.cs(79,58): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/GameServer.cs(61,27): warning CS8618: Non-nullable field 'IP' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/GameServer.cs(67,16): warning CS8618: Non-nullable field 'gameworld' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/GameServer.cs(67,16): warning CS8618: Non-nullable field 'listen' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/GameWorld.cs(97,16): warning CS8618: Non-nullable property 'CharactersCreatedPerIP' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GameWorld.cs(97,16): warning CS8618: Non-nullable property 'LoginThrottle' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GameWorld.cs(97,70): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/GameWorld.cs(326,79): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Group.cs(59,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Group.cs(80,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Guild.cs(54,16): warning CS8618: Non-nullable property 'MOTD' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Guild.cs(54,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Guild.cs(149,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/ItemContainerWindow.cs(7,30): warning CS8618: Non-nullable property 'ItemContainer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemContainerWindow.cs(65,47): warning CS8602: Dereference of a possibly null reference.
Goose/ItemContainerWindow.cs(66,45): warning CS8602: Dereference of a possibly null reference.
Goose/Item.cs(137,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Item.cs(137,16): warning CS8618: Non-nullable property 'Template' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemModifier.cs(11,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemModifier.cs(19,44): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemModifier.cs(20,23): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'CurrencyId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'SpellEffect' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable field 'characters' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable field 'tiles' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'FileName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'ScriptStore' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(419,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Map.cs(436,70): warning CS8602: Dereference of a possibly null reference.
Goose/NPC.cs(69,20): warning CS8618: Non-nullable property 'Map' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(73,29): warning CS8618: Non-nullable property 'MaxStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(149,28): warning CS8618: Non-nullable property 'NPCTemplate' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(153,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(157,23): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(161,23): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(169,29): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(237,22): warning CS8618: Non-nullable property 'Class' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(286,23): warning CS8618: Non-nullable property 'EquippedItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(299,23): warning CS8618: Non-nullable property 'AggroTarget' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(303,22): warning CS8618: Non-nullable property 'AggroValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(307,42): warning CS8618: Non-nullable property 'AggroTargetToValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(314,22): warning CS8618: Non-nullable property 'MoveEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(319,22): warning CS8618: Non-nullable property 'AttackEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(321,27): warning CS8618: Non-nullable property 'Buffs' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(363,30): warning CS8618: Non-nullable property 'Quests' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(369,23): warning CS8618: Non-nullable property 'ScriptStore' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(450,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(454,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(531,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(703,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(705,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(928,69): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(978,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(990,36): warning CS8601: Possible null reference assignment.
Goose/NPC.cs(1116,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(1118,38): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(1151,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(1171,47): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(1171,47): warning CS8602: Dereference of a possibly null reference.
Goose/NPC.cs(1300,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(1466,40): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(1548,17): warning CS8602: Dereference of a possibly null reference.
Goose/NPC.cs(1648,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPCDropInfo.cs(9,29): warning CS8618: Non-nullable property 'ItemTemplate' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCHandler.cs(108,42): warning CS8601: Possible null reference assignment.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Allies' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'AlliesString' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'CurrencyId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Drops' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'EquippedItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'VendorItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(215,16): warning CS8618: Non-nullable property 'Allies' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(215,16): warning CS8618: Non-nullable property 'Drops' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(257,27): warning CS8601: Possible null reference assignment.
Goose/NPCTemplate.cs(258,26): warning CS8601: Possible null reference assignment.
Goose/NPCVendorSlot.cs(8,29): warning CS8618: Non-nullable property 'ItemTemplate' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Paths.cs(34,56): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Paths.cs(39,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'AttackEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'EquippedItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'MoveEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'Owner' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'Target' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(535,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Pet.cs(536,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Pet.cs(655,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(492,16): warning CS8618: Non-nullable field 'sock' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Bank' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'BoundMap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Class' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Group' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Guild' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Inventory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'MacroCheckEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Map' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'MaxStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'PasswordHash' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'PasswordSalt' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'SendBuffer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Spellbook' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable field 'sock' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Bank' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'BoundMap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Buffer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Buffs' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Class' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Group' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Guild' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Inventory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'MacroCheckEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Map' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'MaxStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'moveSpeed' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'PasswordHash' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'PasswordSalt' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Pets' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'QuestProgress' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'QuestsCompleted' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'QuestsStarted' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'SendBuffer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Spellbook' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Windows' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(1199,39): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(1318,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(1370,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(1374,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(2218,17): warning CS8602: Dereference of a possibly null reference.
Goose/Player.cs(2396,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(2410,17): warning CS8602: Dereference of a possibly null reference.
Goose/Program.cs(60,20): warning CS8603: Possible null reference return.
Goose/Program.cs(71,13): warning CS8602: Dereference of a possibly null reference.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'FailText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'PassText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestProgress.cs(8,33): warning CS8618: Non-nullable property 'Requirement' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestRequirement.cs(24,22): warning CS8618: Non-nullable property 'Quest' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestRequirement.cs(33,37): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestRequirement.cs(34,23): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestReward.cs(40,23): warning CS8618: Non-nullable property 'StringValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestReward.cs(41,37): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestReward.cs(42,23): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestStatus.cs(30,22): warning CS8618: Non-nullable property 'Started' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestStatus.cs(31,22): warning CS8618: Non-nullable property 'Completed' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestStatus.cs(32,32): warning CS8618: Non-nullable property 'Progress' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestWindow.cs(23,16): warning CS8618: Non-nullable field 'scriptCannotCompleteMessage' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Quests/QuestWindow.cs(347,40): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Ranks.cs(62,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Ranks.cs(86,30): warning CS8601: Possible null reference assignment.
Goose/Ranks.cs(90,36): warning CS8602: Dereference of a possibly null reference.
Goose/Spell.cs(40,16): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Spell.cs(40,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Spell.cs(40,16): warning CS8618: Non-nullable property 'SpellEffect' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'HPFormula' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'MPFormula' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OffEffectText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OnEffectText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OnMeleeAttackSpell' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OnMeleeHitSpell' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'SPFormula' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(332,20): warning CS8603: Possible null reference return.
Goose/SpellEffect.cs(824,92): warning CS8602: Dereference of a possibly null reference.
Goose/SpellEffect.cs(921,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/SpellEffect.cs(956,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/SpellEffect.cs(974,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/WarpTile.cs(7,20): warning CS8618: Non-nullable property 'WarpMap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(70,31): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(81,31): warning CS8618: Non-nullable property 'Buttons' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(83,20): warning CS8618: Non-nullable property 'NPC' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(87,23): warning CS8618: Non-nullable property 'Data' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.

### Area 2 — Database row mapping (37) — FIXED by Task 2

All 37 entries below were eliminated by annotation-only changes; entries are retained
for traceability (line numbers refer to the `8e72576` baseline).

Goose/Database.cs(17,34): warning CS8618: Non-nullable field '_connection' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(18,22): warning CS8618: Non-nullable field '_loopTask' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(27,45): warning CS8618: Non-nullable field 'Action' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(28,51): warning CS8618: Non-nullable field 'Func' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(29,27): warning CS8618: Non-nullable field 'Result' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(30,30): warning CS8618: Non-nullable field 'Error' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(36,45): warning CS8618: Non-nullable field 'Action' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(37,38): warning CS8618: Non-nullable field 'OnComplete' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(58,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Database.cs(89,39): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(108,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(148,58): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(167,31): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(204,54): warning CS8603: Possible null reference return.
Goose/Database.cs(218,93): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(238,91): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(313,25): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/DataReaderExtensions.cs(14,16): warning CS8603: Possible null reference return.
Goose/Events/CreditsUpdateEvent.cs(23,72): warning CS8604: Possible null reference argument for parameter 'name' in 'Player PlayerHandler.GetPlayerFromData(string name)'.
Goose/Events/CreditsUpdateEvent.cs(36,50): warning CS8620: Argument of type '(Player player, int credits, string?)' cannot be used for parameter 'item' of type '(Player Player, int Credits, string TxnId)' in 'void List<(Player Player, int Credits, string TxnId)>.Add((Player Player, int Credits, string TxnId) item)' due to differences in the nullability of reference types.
Goose/Events/CreditsUpdateEvent.cs(39,42): warning CS8604: Possible null reference argument for parameter 'item' in 'void List<string>.Add(string item)'.
Goose/Guild.cs(274,99): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(912,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(913,73): warning CS8604: Possible null reference argument for parameter 'json' in 'ItemSlot[] JsonHelper.Deserialize<ItemSlot[]>(string json)'.
Goose/Inventory.cs(931,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(932,72): warning CS8604: Possible null reference argument for parameter 'json' in 'ItemSlot[] JsonHelper.Deserialize<ItemSlot[]>(string json)'.
Goose/Inventory.cs(962,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(963,75): warning CS8604: Possible null reference argument for parameter 'json' in 'ItemSlot[] JsonHelper.Deserialize<ItemSlot[]>(string json)'.
Goose/JsonHelper.cs(49,13): warning CS8603: Possible null reference return.
Goose/Pet.cs(296,65): warning CS8604: Possible null reference argument for parameter 'onCommit' in 'void Database.EnqueueTransaction(Action<SQLiteConnection> action, Action onCommit = null)'.
Goose/Player.cs(821,42): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Player.cs(822,71): warning CS8604: Possible null reference argument for parameter 'json' in 'QuestStatus JsonHelper.Deserialize<QuestStatus>(string json)'.
Goose/Player.cs(904,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Player.cs(933,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Player.cs(952,16): warning CS8604: Possible null reference argument for parameter 'onCommit' in 'void Database.EnqueueTransaction(Action<SQLiteConnection> action, Action onCommit = null)'.
Goose/Spellbook.cs(42,42): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Spellbook.cs(43,62): warning CS8604: Possible null reference argument for parameter 'json' in 'int[] JsonHelper.Deserialize<int[]>(string json)'.

### Area 3 — Collections containing nullable slots (80) — FIXED by Task 3

Goose/ChatFilter.cs(38,69): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Class.cs(21,20): warning CS8603: Possible null reference return.
Goose/ClassHandler.cs(27,50): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ClassHandler.cs(32,20): warning CS8603: Possible null reference return.
Goose/CombinationHandler.cs(151,20): warning CS8603: Possible null reference return.
Goose/Currency/CurrencyHandler.cs(29,50): warning CS8603: Possible null reference return.
Goose/Currency/CurrencyHandler.cs(30,20): warning CS8603: Possible null reference return.
Goose/Events/CustomCommandEvent.cs(84,97): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/CustomCommandEvent.cs(87,99): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/CustomCommandEvent.cs(98,51): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/CustomCommandEvent.cs(107,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/RankCommandEvent.cs(46,90): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GuildHandler.cs(70,20): warning CS8603: Possible null reference return.
Goose/Inventory.cs(180,20): warning CS8603: Possible null reference return.
Goose/Inventory.cs(249,39): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(459,49): warning CS8603: Possible null reference return.
Goose/Inventory.cs(463,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(481,20): warning CS8603: Possible null reference return.
Goose/Inventory.cs(504,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(516,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(537,45): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(542,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(680,20): warning CS8603: Possible null reference return.
Goose/ItemContainer.cs(35,24): warning CS8603: Possible null reference return.
Goose/Item.cs(226,60): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Item.cs(227,24): warning CS8603: Possible null reference return.
Goose/Item.cs(234,24): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(190,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemHandler.cs(193,20): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(219,20): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(219,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemHandler.cs(224,20): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(224,54): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemHandler.cs(361,20): warning CS8603: Possible null reference return.
Goose/ItemModifier.cs(26,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemModifier.cs(27,48): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemSlot.cs(14,21): warning CS8618: Non-nullable property 'Item' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemSlot.cs(49,33): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemSlot.cs(50,24): warning CS8601: Possible null reference assignment.
Goose/ItemSlot.cs(51,22): warning CS8601: Possible null reference assignment.
Goose/ItemSlot.cs(57,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/ItemTile.cs(16,25): warning CS8618: Non-nullable property 'ItemSlot' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTile.cs(22,23): warning CS8618: Non-nullable property 'Owner' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/LoginThrottle.cs(74,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/LoginThrottle.cs(94,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Map.cs(261,56): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Map.cs(584,87): warning CS8603: Possible null reference return.
Goose/Map.cs(671,87): warning CS8603: Possible null reference return.
Goose/MapHandler.cs(91,20): warning CS8603: Possible null reference return.
Goose/NPCHandler.cs(38,43): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(39,60): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(225,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(226,51): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(229,20): warning CS8603: Possible null reference return.
Goose/NPCHandler.cs(308,98): warning CS8603: Possible null reference return.
Goose/PlayerBank.cs(94,39): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/PlayerBank.cs(95,62): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/PlayerHandler.cs(86,51): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/PlayerHandler.cs(106,51): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/PlayerHandler.cs(124,20): warning CS8603: Possible null reference return.
Goose/PlayerHandler.cs(133,20): warning CS8603: Possible null reference return.
Goose/PlayerHandler.cs(183,20): warning CS8603: Possible null reference return.
Goose/Quests/Quest.cs(43,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/Quest.cs(44,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/QuestHandler.cs(75,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/QuestHandler.cs(77,54): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/QuestHandler.cs(82,20): warning CS8603: Possible null reference return.
Goose/Spellbook.cs(255,37): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Spellbook.cs(305,38): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/SpellHandler.cs(38,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(39,55): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(204,34): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(205,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(206,20): warning CS8603: Possible null reference return.
Goose/SpellHandler.cs(238,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(239,54): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(282,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(283,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(284,20): warning CS8603: Possible null reference return.
Goose/SpellHandler.cs(300,20): warning CS8603: Possible null reference return.

### Area 4 — Packet/event inputs (74) — FIXED by Task 3

Goose/Console/Commands/SetAccessCommand.cs(13,23): warning CS8618: Non-nullable field 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/Commands/SetAccessCommand.cs(43,23): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Console/Commands/SetAccessCommand.cs(44,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Console/ConsoleCommandHandler.cs(11,44): warning CS8618: Non-nullable field 'Run' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandHandler.cs(12,23): warning CS8618: Non-nullable field 'Usage' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandHandler.cs(13,23): warning CS8618: Non-nullable field 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandHandler.cs(53,60): warning CS8602: Dereference of a possibly null reference.
Goose/Console/ConsoleCommandHandler.cs(99,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Console/ConsoleCommandHandler.cs(121,48): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Console/ConsoleCommandHandler.cs(127,65): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Console/ConsoleCommandParser.cs(10,23): warning CS8618: Non-nullable field 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandParser.cs(11,25): warning CS8618: Non-nullable field 'Args' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandParser.cs(34,57): warning CS8603: Possible null reference return.
Goose/Console/ConsoleCommandParser.cs(37,17): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Event.cs(19,16): warning CS8618: Non-nullable property 'Data' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Event.cs(19,16): warning CS8618: Non-nullable property 'NPC' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Event.cs(19,16): warning CS8618: Non-nullable property 'Player' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/EventHandler.cs(29,38): warning CS8602: Dereference of a possibly null reference.
Goose/EventHandler.cs(64,32): warning CS8618: Non-nullable field 'EventTypeId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/EventHandler.cs(65,32): warning CS8618: Non-nullable field 'EventFactory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/EventHandler.cs(250,55): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/EventHandler.cs(285,67): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/EventHandler.cs(365,44): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/CustomCommandEvent.cs(366,20): warning CS8603: Possible null reference return.
Goose/Events/HairdyeCommandEvent.cs(182,20): warning CS8603: Possible null reference return.
Goose/Events/LoginEvent.cs(47,25): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/LoginEvent.cs(47,25): warning CS8602: Dereference of a possibly null reference.
Goose/Events/LoginEvent.cs(48,18): warning CS8602: Dereference of a possibly null reference.
Goose/Events/MacroCheckEvent.cs(9,23): warning CS8618: Non-nullable property 'Code' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Events/PetDamageCommandEvent.cs(47,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetDeleteCommandEvent.cs(29,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetInfoCommandEvent.cs(29,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetSpawnCommandEvent.cs(40,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetVitaCommandEvent.cs(47,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs(19,41): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs(19,41): warning CS8602: Dereference of a possibly null reference.
Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs(20,34): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(20,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(28,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(29,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(31,21): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(33,21): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(41,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(42,25): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(43,44): warning CS8601: Possible null reference assignment.
Goose/Events/SetConfigCommandEvent.cs(43,44): warning CS8602: Dereference of a possibly null reference.
Goose/Events/VendorPurchaseInventoryEvent.cs(38,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/VendorSellInventoryEvent.cs(42,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WhoEvent.cs(24,32): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(41,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(42,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(50,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(55,18): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(162,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(232,44): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(248,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(400,23): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(400,23): warning CS8602: Dereference of a possibly null reference.
Goose/GameServer.cs(428,57): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(455,34): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameWorld.cs(71,20): warning CS8603: Possible null reference return.
Goose/GameWorld.cs(71,58): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameWorld.cs(402,47): warning CS8602: Dereference of a possibly null reference.
Goose/GameWorld.cs(426,48): warning CS8602: Dereference of a possibly null reference.
Goose/GameWorld.cs(478,60): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Group.cs(109,50): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Packets.cs(81,39): warning CS8602: Dereference of a possibly null reference.
Goose/Packets.cs(118,39): warning CS8602: Dereference of a possibly null reference.
Goose/Packets.cs(292,44): warning CS8603: Possible null reference return.
Goose/Packets.cs(295,96): warning CS8603: Possible null reference return.
Goose/Packets.cs(298,97): warning CS8603: Possible null reference return.
Goose/Packets.cs(480,66): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(2470,44): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Spellbook.cs(152,53): warning CS8625: Cannot convert null literal to non-nullable reference type.

### Area 5 — Script-facing APIs (28) — FIXED by Task 4

All 28 entries below were eliminated by annotation-only changes (see the Task 4
completion record); entries are retained for traceability (line numbers refer to the
`8e72576` baseline). The 5 entries marked FIXED by Task 3 remain resolved.

Goose/Events/PickupItemEvent.cs(91,38): warning CS8600: Converting null literal or possible null value to non-nullable type. — FIXED by Task 3 (side effect: `refusal` local is now `string?`)
Goose/Events/PickupItemEvent.cs(94,35): warning CS8600: Converting null literal or possible null value to non-nullable type. — FIXED by Task 3 (side effect: `refusal` local is now `string?`, accepting the `string?` result of `Script?.Object.CanPickup`)
Goose/GooseSettings.cs(6,23): warning CS8618: Non-nullable property 'ServerVersion' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(7,23): warning CS8618: Non-nullable property 'ServerType' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(8,23): warning CS8618: Non-nullable property 'DatabaseName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(9,23): warning CS8618: Non-nullable property 'DataLinkId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(10,23): warning CS8618: Non-nullable property 'DataPath' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(20,23): warning CS8618: Non-nullable property 'ServerName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(24,23): warning CS8618: Non-nullable property 'StartingItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(33,23): warning CS8618: Non-nullable property 'GameServerIP' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(75,23): warning CS8618: Non-nullable property 'MOTD' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(76,23): warning CS8618: Non-nullable property 'StartingTitle' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(77,23): warning CS8618: Non-nullable property 'StartingSurname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(95,23): warning CS8618: Non-nullable property 'DefaultGuildMOTD' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettingsLoader.cs(52,20): warning CS8603: Possible null reference return.
Goose/Map.cs(605,30): warning CS8600: Converting null literal or possible null value to non-nullable type. — FIXED by Task 3 (side effect: `refusal` local is now `string?`)
Goose/Map.cs(608,27): warning CS8600: Converting null literal or possible null value to non-nullable type. — FIXED by Task 3 (side effect: `refusal` local is now `string?`, accepting the `string?` result of `Script?.Object.CanPlayerJoin`)
Goose/Quests/QuestWindow.cs(323,20): warning CS8603: Possible null reference return. — FIXED by Task 3 (side effect: `GetScriptCannotCompleteMessage` now returns `string?`)
Goose/Scripting/BaseItemScript.cs(26,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseMapScript.cs(35,32): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/BaseMapScript.cs(36,61): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/BaseMapScript.cs(39,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseMapScript.cs(78,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseQuestScript.cs(27,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseSpellEffectScript.cs(31,20): warning CS8603: Possible null reference return.
Goose/Scripting/Script.cs(14,16): warning CS8618: Non-nullable property 'Object' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Scripting/Script.cs(44,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/Script.cs(44,27): warning CS8601: Possible null reference assignment.
Goose/Scripting/ScriptHandler.cs(20,30): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/ScriptHandler.cs(21,57): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellEffect.cs(474,45): warning CS8603: Possible null reference return.
Goose/SpellEffect.cs(479,24): warning CS8603: Possible null reference return.
Goose/SpellEffect.cs(484,24): warning CS8603: Possible null reference return.

### Area 6 — Tests and fakes (308 after Task 3 = 76 sln-scope + 232 IT; 85 after Task 2; baseline 60) — FIXED by Task 4

All 308 entries below were eliminated by annotation-only changes (see the Task 4
completion record); entries are retained for traceability. Entries marked FIXED by
Task 2/Task 3 remain resolved.

Goose.Tests (76 after Task 3 = 46 after Task 2 − 2 Task 3 side-effect fixes + 32 Task 3 cascades; 43 at baseline — plus the 4 shared `TestSupport` entries listed under the IT build below, 47 in the sln scope at baseline; built via Goose.sln):

Goose.Tests/BuiltInCurrencyTests.cs(86,77): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/CurrencyHandlerTests.cs(91,71): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/CurrencyHandlerTests.cs(123,92): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/CurrencyHandlerTests.cs(134,82): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/GameServerStartupTests.cs(30,25): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/GameServerStartupTests.cs(30,25): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/GameServerStartupTests.cs(47,33): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/GameServerStartupTests.cs(47,33): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/GameServerStartupTests.cs(62,71): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/GameWorldSettingsIsolationTests.cs(61,16): warning CS8605: Unboxing a possibly null value.
Goose.Tests/GameWorldSettingsIsolationTests.cs(61,21): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/InvisibilityTransitionTests.cs(374,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/LoginEventNameLengthTests.cs(37,64): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/LoginEventNameLengthTests.cs(37,64): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/MapCanPlayerJoinTests.cs(15,55): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(15,61): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(15,67): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(21,70): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(21,76): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(21,82): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapPlayerCanJoinHookTests.cs(55,37): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapPlayerCanJoinHookTests.cs(92,70): warning CS8625: Cannot convert null literal to non-nullable reference type. — FIXED by Task 2 (side effect: `Map.Script` is now nullable, so the test's `new Map { …, Script = null }` initializer is legal)
Goose.Tests/PacketCurrencyTests.cs(24,62): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/PacketCurrencyTests.cs(95,35): warning CS8604: Possible null reference argument for parameter 'arg1' in 'string Func<Window, ItemTemplate, GameWorld, int, long, string>.Invoke(Window arg1, ItemTemplate arg2, GameWorld arg3, int arg4, long arg5)'.
Goose.Tests/PlayerSendTests.cs(24,64): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/PlayerSendTests.cs(24,64): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/PreLoginReassemblyTests.cs(118,55): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/PreLoginReassemblyTests.cs(118,55): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/QuestScriptTests.cs(40,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(41,62): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(42,56): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(43,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(44,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(44,49): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/ScriptLoadDirectiveTests.cs(50,16): warning CS8605: Unboxing a possibly null value.
Goose.Tests/ScriptLoadDirectiveTests.cs(50,21): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/VendorPurchaseCurrencyTests.cs(36,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(49,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(65,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(83,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(100,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(117,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/WhoEventTests.cs(30,87): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(12,63): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(56,96): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(113,92): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(127,78): warning CS8625: Cannot convert null literal to non-nullable reference type.

Goose.IntegrationTests (232 after Task 3 = 13 baseline + 26 Task 2 cascades + 192 Task 3 cascades + 1 re-emergence (`DimensionsScriptTests.cs(288,87)`, tracked in the Task 2 list below); 39 after Task 2; separate build — project not in Goose.sln):

Goose.IntegrationTests/DimensionItemScriptTests.cs(11,82): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionItemScriptTests.cs(93,21): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.IntegrationTests/DimensionItemScriptTests.cs(152,50): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionMapScriptTests.cs(244,23): warning CS8618: Non-nullable field 'Refusal' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose.IntegrationTests/DimensionRebirthTests.cs(212,49): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionsScriptTests.cs(603,54): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionsScriptTests.cs(618,54): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionsScriptTests.cs(632,54): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs(56,41): warning CS8604: Possible null reference argument for parameter 'json' in 'void Player.LoadPropertiesFromColumn(string json)'.
TestSupport/TestWorldFixture.cs(12,63): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(56,96): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(113,92): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(127,78): warning CS8625: Cannot convert null literal to non-nullable reference type.

Task 2 cascade — fix in Task 4 (26 new warnings at consuming sites of the new `?` annotations; 5 of the originally recorded 31 were resolved by the `NPCTemplate.BaseStats` `= null!` unification and are marked FIXED below; not annotated in Task 2 per orchestrator decision):

Goose.IntegrationTests/DimensionDropTests.cs(34,21): warning CS8604: Possible null reference argument for parameter 'source' in 'NPCDropInfo Enumerable.Single<NPCDropInfo>(IEnumerable<NPCDropInfo> source, Func<NPCDropInfo, bool> predicate)'.
Goose.IntegrationTests/DimensionDropTests.cs(46,26): warning CS8604: Possible null reference argument for parameter 'source' in 'NPCDropInfo Enumerable.Single<NPCDropInfo>(IEnumerable<NPCDropInfo> source, Func<NPCDropInfo, bool> predicate)'.
Goose.IntegrationTests/DimensionDropTests.cs(57,26): warning CS8604: Possible null reference argument for parameter 'source' in 'NPCDropInfo Enumerable.Single<NPCDropInfo>(IEnumerable<NPCDropInfo> source, Func<NPCDropInfo, bool> predicate)'.
Goose.IntegrationTests/DimensionDropTests.cs(66,20): warning CS8604: Possible null reference argument for parameter 'source' in 'NPCDropInfo Enumerable.Single<NPCDropInfo>(IEnumerable<NPCDropInfo> source, Func<NPCDropInfo, bool> predicate)'.
Goose.IntegrationTests/DimensionDropTests.cs(68,20): warning CS8604: Possible null reference argument for parameter 'source' in 'NPCDropInfo Enumerable.Single<NPCDropInfo>(IEnumerable<NPCDropInfo> source, Func<NPCDropInfo, bool> predicate)'.
Goose.IntegrationTests/DimensionItemScriptTests.cs(132,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(157,24): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(182,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(202,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(219,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionMapScriptTests.cs(158,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionMapScriptTests.cs(178,42): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionMapScriptTests.cs(194,46): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionMapScriptTests.cs(213,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionMapScriptTests.cs(234,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(181,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(212,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(117,27): warning CS8602: Dereference of a possibly null reference. — FIXED by Task 2 (side effect: `NPCTemplate.BaseStats` is now `= null!`, so the `BaseStats.HP` deref no longer warns)
Goose.IntegrationTests/DimensionsScriptTests.cs(175,28): warning CS8602: Dereference of a possibly null reference. — FIXED by Task 2 (side effect: `NPCTemplate.BaseStats` is now `= null!`, so the `BaseStats.HP` deref no longer warns)
Goose.IntegrationTests/DimensionsScriptTests.cs(225,28): warning CS8602: Dereference of a possibly null reference. — FIXED by Task 2 (side effect: `NPCTemplate.BaseStats` is now `= null!`, so the `BaseStats.HP` deref no longer warns)
Goose.IntegrationTests/DimensionsScriptTests.cs(249,45): warning CS8604: Possible null reference argument for parameter 'collection' in 'NPCTemplate Assert.Single<NPCTemplate>(IEnumerable<NPCTemplate> collection)'.
Goose.IntegrationTests/DimensionsScriptTests.cs(250,44): warning CS8604: Possible null reference argument for parameter 'collection' in 'NPCTemplate Assert.Single<NPCTemplate>(IEnumerable<NPCTemplate> collection)'.
Goose.IntegrationTests/DimensionsScriptTests.cs(254,35): warning CS8604: Possible null reference argument for parameter 'collection' in 'NPCTemplate Assert.Single<NPCTemplate>(IEnumerable<NPCTemplate> collection)'.
Goose.IntegrationTests/DimensionsScriptTests.cs(270,22): warning CS8604: Possible null reference argument for parameter 'collection' in 'void Assert.Empty(IEnumerable collection)'.
Goose.IntegrationTests/DimensionsScriptTests.cs(288,87): warning CS8602: Dereference of a possibly null reference. — FIXED by Task 2, re-emerged in Task 3 (`dim5` is now `NPCTemplate?` via `GetNPCTemplate`)
Goose.IntegrationTests/DimensionsScriptTests.cs(603,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(618,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(632,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(74,83): warning CS8602: Dereference of a possibly null reference. — FIXED by Task 2 (side effect: `NPCTemplate.BaseStats` is now `= null!`, so the `BaseStats.HP` deref no longer warns)
Goose.IntegrationTests/DimensionVendorStockTests.cs(107,31): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(63,12): warning CS8603: Possible null reference return.

Task 3 cascade — fix in Task 4, sln scope (32 new warnings in Goose.Tests/TestSupport at consuming sites of the new `?` annotations; 2 side-effect fixes: `CurrencyHandlerTests.cs(123,92)/(134,82)` null literals are now legal `Resolve(ItemTemplate?, NPC?)` args):

Goose.Tests/ConsoleCommandParserTests.cs(25,33): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/ConsoleCommandParserTests.cs(34,39): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/ConsoleCommandParserTests.cs(43,43): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/Fixtures/VendorFixture.cs(48,21): warning CS8601: Possible null reference assignment.
Goose.Tests/InventoryChangeSlotTests.cs(75,25): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/InvisibilityAggroTests.cs(110,9): warning CS8603: Possible null reference return.
Goose.Tests/InvisibilityBreakTests.cs(96,31): warning CS8603: Possible null reference return.
Goose.Tests/InvisibilityCounterTests.cs(84,9): warning CS8603: Possible null reference return.
Goose.Tests/InvisibilityMapLoadTests.cs(71,19): warning CS8601: Possible null reference assignment.
Goose.Tests/InvisibilityTransitionTests.cs(109,9): warning CS8603: Possible null reference return.
Goose.Tests/NPCSpawnRegistrationTests.cs(109,38): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/NPCSpawnRegistrationTests.cs(120,21): warning CS8601: Possible null reference assignment.
Goose.Tests/NPCSpawnRegistrationTests.cs(121,19): warning CS8601: Possible null reference assignment.
Goose.Tests/NPCSpawnRegistrationTests.cs(125,24): warning CS8601: Possible null reference assignment.
Goose.Tests/NPCSpawnRegistrationTests.cs(77,30): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/PlayerEconomyOverloadTests.cs(106,28): warning CS8601: Possible null reference assignment.
Goose.Tests/PlayerEconomyOverloadTests.cs(133,24): warning CS8601: Possible null reference assignment.
Goose.Tests/PlayerEconomyOverloadTests.cs(22,24): warning CS8601: Possible null reference assignment.
Goose.Tests/PlayerEconomyOverloadTests.cs(41,24): warning CS8601: Possible null reference assignment.
Goose.Tests/PlayerEconomyOverloadTests.cs(72,24): warning CS8601: Possible null reference assignment.
Goose.Tests/PreLoginReassemblyTests.cs(82,34): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/PreLoginReassemblyTests.cs(86,34): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/SetAccessCommandTests.cs(14,33): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/SetAccessCommandTests.cs(26,53): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/SetAccessCommandTests.cs(32,88): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/SetAccessCommandTests.cs(41,90): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/SetAccessCommandTests.cs(64,33): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/SpellHandlerRegistrationTests.cs(40,32): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/TestFixtureIsolationTests.cs(91,16): warning CS8602: Dereference of a possibly null reference.
TestSupport/TestWorldFixture.cs(119,27): warning CS8601: Possible null reference assignment.
TestSupport/TestWorldFixture.cs(71,21): warning CS8601: Possible null reference assignment.
TestSupport/TestWorldFixture.cs(91,21): warning CS8601: Possible null reference assignment.

Task 3 cascade — fix in Task 4, IT (192 new sites at consuming sites of the new `?` annotations; plus `DimensionsScriptTests.cs(288,87)` re-emerging — see above):

Goose.IntegrationTests/DimensionCommandGateTests.cs(104,22): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionCommandGateTests.cs(18,46): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionCommandGateTests.cs(21,24): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionCommandGateTests.cs(65,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(147,24): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(20,46): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(23,24): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(235,43): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(253,43): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(26,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(279,43): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(320,43): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(33,12): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(339,43): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(342,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionCurrencyCommandTests.cs(360,43): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionDropTests.cs(32,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionDropTests.cs(44,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionDropTests.cs(53,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionDropTests.cs(66,20): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionDropTests.cs(68,20): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(106,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(154,35): warning CS8604: Possible null reference argument for parameter 'spell' in 'bool Spellbook.AddSpell(Spell spell, GameWorld world)'.
Goose.IntegrationTests/DimensionItemScriptTests.cs(177,35): warning CS8604: Possible null reference argument for parameter 'spell' in 'bool Spellbook.AddSpell(Spell spell, GameWorld world)'.
Goose.IntegrationTests/DimensionItemScriptTests.cs(24,31): warning CS8604: Possible null reference argument for parameter 'template' in 'void Item.LoadFromTemplate(ItemTemplate template)'.
Goose.IntegrationTests/DimensionItemScriptTests.cs(32,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(36,29): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(38,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemScriptTests.cs(56,24): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(100,33): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(178,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(178,42): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(26,39): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(35,37): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(46,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(51,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionItemTemplateTests.cs(83,60): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionMapScriptTests.cs(155,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionMapScriptTests.cs(175,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionMapScriptTests.cs(191,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionMapScriptTests.cs(208,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionMapScriptTests.cs(231,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionModifierTests.cs(108,31): warning CS8604: Possible null reference argument for parameter 'template' in 'void Item.LoadFromTemplate(ItemTemplate template)'.
Goose.IntegrationTests/DimensionModifierTests.cs(28,29): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(30,39): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(31,34): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(32,35): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(33,33): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(46,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(59,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(73,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(74,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionModifierTests.cs(95,13): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(109,36): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(126,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(164,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(174,27): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(178,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionRebirthTests.cs(200,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(202,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionRebirthTests.cs(204,24): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionRebirthTests.cs(215,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(62,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionRebirthTests.cs(79,36): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(137,20): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(180,34): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(207,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(214,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(29,46): warning CS8604: Possible null reference argument for parameter 'map' in 'CapturingPlayer TestWorldFixture.CommandPlayerOn(Map map, int x, int y, string name = "Tester")'.
Goose.IntegrationTests/DimensionResetItemTests.cs(31,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(40,31): warning CS8604: Possible null reference argument for parameter 'template' in 'void Item.LoadFromTemplate(ItemTemplate template)'.
Goose.IntegrationTests/DimensionResetItemTests.cs(47,12): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(69,24): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionResetItemTests.cs(88,24): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(113,61): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(117,57): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(118,32): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(180,38): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(185,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(204,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(221,53): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(22,43): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(23,41): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(240,66): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(24,35): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(247,62): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(271,53): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(298,66): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(306,54): warning CS8604: Possible null reference argument for parameter 'item' in 'bool List<SpellEffect>.Contains(SpellEffect item)'.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(335,48): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(338,49): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(383,56): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(402,35): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(41,57): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(432,52): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(433,50): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(434,30): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(459,35): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(467,48): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(468,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(471,46): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(495,35): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(502,48): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(503,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(504,46): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(62,35): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(73,27): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionSpellScriptTests.cs(90,32): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(202,35): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(208,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(224,36): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(249,45): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(250,44): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(254,35): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(270,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(305,24): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(306,24): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(320,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.IntegrationTests/DimensionsScriptTests.cs(320,30): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(322,38): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(327,24): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.IntegrationTests/DimensionsScriptTests.cs(327,34): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(328,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(341,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(342,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(360,38): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(379,23): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(459,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(460,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(529,29): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(532,31): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(584,26): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(600,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(617,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(630,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(653,32): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(669,32): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(682,40): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionsScriptTests.cs(696,32): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(108,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(110,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(127,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(130,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(143,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(144,27): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(145,26): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(149,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(166,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(169,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(184,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(187,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(200,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(235,56): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(54,62): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(78,26): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(79,25): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(92,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/DimensionTeleportScriptTests.cs(95,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(105,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(161,38): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionVendorStockTests.cs(180,38): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionVendorStockTests.cs(200,32): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(205,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(218,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(222,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(226,28): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(234,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(237,44): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(238,52): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(249,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(270,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(277,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(281,28): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(291,26): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(294,22): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(296,44): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/DimensionVendorStockTests.cs(47,38): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionVendorStockTests.cs(52,38): warning CS8601: Possible null reference assignment.
Goose.IntegrationTests/DimensionVendorStockTests.cs(63,12): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/SpiritCurrencyTests.cs(101,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/SpiritCurrencyTests.cs(119,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/SpiritCurrencyTests.cs(122,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/SpiritCurrencyTests.cs(39,32): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/SpiritCurrencyTests.cs(51,21): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/SpiritCurrencyTests.cs(64,31): warning CS8604: Possible null reference argument for parameter 'template' in 'void Item.LoadFromTemplate(ItemTemplate template)'.
Goose.IntegrationTests/SpiritCurrencyTests.cs(66,39): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/SpiritCurrencyTests.cs(80,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
Goose.IntegrationTests/SpiritCurrencyTests.cs(83,9): warning CS8602: Dereference of a possibly null reference.
Goose.IntegrationTests/SpiritCurrencyTests.cs(98,39): warning CS8604: Possible null reference argument for parameter 'map' in 'Player TestWorldFixture.PlayerOn(Map map, int x, int y)'.
TestSupport/TestWorldFixture.cs(119,27): warning CS8601: Possible null reference assignment.
TestSupport/TestWorldFixture.cs(71,21): warning CS8601: Possible null reference assignment.
TestSupport/TestWorldFixture.cs(91,21): warning CS8601: Possible null reference assignment.
