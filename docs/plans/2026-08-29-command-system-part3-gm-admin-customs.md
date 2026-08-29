# Command System — Part 3: GM/Admin/Customs + Dimension Scripts Implementation Plan

**Goal:** Migrate the remaining commands — Customs (`/custom`), GM (23), Admin (10) — to the command system, migrate the shipped dimension scripts from `RegisterEvent` to `world.Commands.Register`, and harden `RegisterEvent` so only non-command packets can use it.

**Architecture:** Same migration rule as Part 2: legacy `Ready` body moves verbatim into a typed `Execute`; parsing disappears into the binder; legacy registrations and event classes are deleted. `/custom` becomes the first real subcommand command. The dimension `.csx` scripts' five commands become delegate registrations in section `Dimensions`, after which `EventHandler.RegisterEvent` rejects `/` keys outright and the privilege overload is deleted.

**Tech Stack:** C# / .NET 10, xUnit, runtime-compiled `.csx` scripts, Part 1/2 framework.

**This is Part 3 of 3.** Prerequisites: Parts 1 and 2 complete and green.

Design doc: `docs/plans/2026-08-29-command-system-design.md`

---

## APIs verified

| API | Location |
|---|---|
| `PlayerHandler.GetPlayer(string)` — online players, case-insensitive | `Goose/PlayerHandler.cs:133` |
| `PlayerHandler.GetPlayerFromData(string)` — in-memory DB incl. offline | `Goose/PlayerHandler.cs:178` |
| `EventHandler.RegisterEvent(string, CreateEvent)` / `(string, CreateEvent, AccessPrivilege)` | `Goose/EventHandler.cs:248,267` |
| `CommandRegistry.Register(key, privilege, section, help, handler)` (Part 1) | `Goose/Commands/CommandRegistry.cs` |
| Dimension script registration (5 commands) | `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:229-233` |
| Dimension command event classes to replace | `Goose/Data/Illutia/Scripts/Global/Dimensions/Commands.csx` |
| Aspereta non-command registration (`"GID"` → `ItemInfoEvent`) — must keep working | `Goose/Data/Aspereta/Scripts/Global/Aspereta.csx:237` |
| `Dimensions.TryParseAmount` = `long.TryParse` + `> 0` | `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:185-192` |
| `GlobalScriptFixture` (`CompileShipped`, `CommandPlayerOn`, `RunCommand` via `TestWorldFixture`) | `Goose.IntegrationTests/Fixtures/GlobalScriptFixture.cs`, `TestSupport/TestWorldFixture.cs:105` |
| Existing dimension regression net | `Goose.IntegrationTests/DimensionCommandGateTests.cs`, `DimensionCurrencyCommandTests.cs`, `DimensionItemScriptTests.cs`, ... |
| `/custom` legacy body (subcommands help/preview/kill/create\|make, `ParseRGBA`, `ValidateCustomSlots`, `EquippedDisplay`, `MountDisplay`) | `Goose/Events/CustomCommandEvent.cs` |
| `SubcommandAttribute` (Part 1, single name) | `Goose/Commands/SubcommandAttribute.cs` |

**Player-resolution split (verified per event):** binder `Player` param (online) for `/summon`, `/approach`, `/kick`, `/macrocheck`. `string name` + `GetPlayerFromData` (kept in handler body) for `/ban`, `/unban`, `/setpassword`, `/givecredits`, `/playerinfo`, `/givegold`, `/giveexperience`, `/settitle`, `/setsurname`, `/changeclass`, `/changename`, `/checkname`, `/setaccess`.

## Migration rule

Identical to Part 2's rule (read event → new class with verbatim key/privilege/section/help → `Execute` = old body minus state check and parsing → remove legacy registration → delete event class after grep-verified no other references → tests → full suite).

## Per-command parameter mapping (verified against legacy sources)

| Key | Legacy event | `Execute` parameters | Notes |
|---|---|---|---|
| `/summon ` | `SummonEvent` | `Player target` | keep the `State != Ready → "Player is still loading a map."` check in-body |
| `/approach ` | `ApproachEvent` | `Player target` | same shape as summon |
| `/kick ` | `KickCommandEvent` | `Player target` | keep `State != NotLoggedIn` guard + `LostConnection` + kick log |
| `/unban ` | `UnbanCommandEvent` | `string name` | `GetPlayerFromData` in-body |
| `/ban ` | `BanCommandEvent` | `string name, int? days = null` | `GetPlayerFromData` in-body; keep the ban-type switch on `days` presence |
| `/broadcast ` | `BroadcastCommandEvent` | `string[] rest` | message = join; keep the `[Access]:` prefix formatting |
| `/playerinfo ` | `PlayerInfoCommandEvent` | `string name` | `GetPlayerFromData` in-body; opens `PlayerInfoWindow` |
| `/givegold ` | `GiveGoldCommandEvent` | `string name, long gold` | `GetPlayerFromData` in-body |
| `/giveexperience ` | `GiveExperienceCommandEvent` | `string name, long exp` | same |
| `/givecredits ` | `GiveCreditsCommandEvent` | `string name, int credits` | `GetPlayerFromData` in-body; keep the silent `credits <= 0` return (validation, not access — no `CheckAccess` needed) |
| `/settitle ` | `SetTitleCommandEvent` | `string name, string[] rest` | title = join — legacy `Split(' ', 3)` makes `tokens[2]` the **rest of the line** (titles may contain spaces); `GetPlayerFromData` in-body |
| `/setsurname ` | `SetSurnameCommandEvent` | `string name, string[] rest` | surname = join; same |
| `/changeclass ` | `ChangeClassCommandEvent` | `string name, string cl, decimal? modifier = null` | `GetPlayerFromData` in-body; `decimal` binder from Part 2 Task 0 |
| `/changename ` | `ChangeNameCommandEvent` | `string oldname, string newname` | two `GetPlayerFromData` lookups in-body |
| `/checkname ` | `CheckNameCommandEvent` | `string[] rest` | name = join (old `Substring(11)` = rest of line); `GetPlayerFromData` in-body |
| `/setpassword ` | `GMSetPasswordCommandEvent` | `string name, string[] rest` | password = join — legacy `Split(' ', 3)` makes `tokens[2]` the rest of the line (passwords may contain spaces); `GetPlayerFromData` in-body |
| `/macrocheck ` | `MacroCheckCommandEvent` | `string[] rest` | name = join; online `GetPlayer` in-body |
| `/warp ` | `WarpEvent` | `int mapId = 1, int mapx = 50, int mapy = 50` | keep bounds check + map-exists check. Legacy only warps when `tokens.Length == 4` (`Goose/Events/WarpEvent.cs:30`), and the key's trailing space means bare `/warp` never matches — the defaults are unreachable in legacy. Preserve exactly: in `Execute`, `if (ctx.Args.Length != 3) return;` (silent). The parameter defaults stay (harmless, and they make the usage line read `/warp [mapId] [mapx] [mapy]`) |
| `/getitem ` | `GetItemCommandEvent` | `int id, string? arg2 = null, string? arg3 = null` | mixed token: `arg2` is a stack int or the word `powerful`; `arg3` may be `powerful` — keep the in-body parsing verbatim |
| `/spawnnpc ` | `SpawnNPCCommandEvent` | `int id` | keep `id <= 0` silent return |
| `/placespawn` | `PlaceSpawnCommandEvent` | `int npcId` | old self-usage messages become the framework usage line (intended delta) |
| `/search ` | `SearchCommandEvent` | `string command, string[] rest` | name = join — legacy `Split(' ', 3)` makes `tokens[2]` the rest of the line (the regex is built over it); keep search bodies; old self-usage becomes framework usage |
| `/mutemap` | `MuteMapEvent` | — | no args |
| `/shutdown` | `ShutdownCommandEvent` | — | verbatim |
| `/setaccess` | `SetAccessCommandEvent` | `string name, string access` | `GetPlayerFromData` in-body |
| `/setconfig ` | `SetConfigCommandEvent` | `string setting, string[] rest` | value = join (may contain spaces; old `Split(' ', 2)`); old self-usage becomes framework usage |
| `/saveconfig` | `SaveConfigCommandEvent` | — | verbatim (no-op body with comment — keep the comment, it explains why) |
| `/respawnmap` | `RespawnMapCommandEvent` | — | verbatim |
| `/reloadscripts` | `ReloadScriptsCommandEvent` | — | verbatim, including the `Task.Run` and its TODO comment |
| `/reloadsql` | `ReloadSqlCommandEvent` | — | verbatim |
| `/updatesql` | `UpdateSqlCommandEvent` | — | verbatim |
| `/hax ` | `HaxCommandEvent` | — (no typed params) | **raw fidelity**: legacy sends `Substring(5)` verbatim — `ctx.World.Send(ctx.Player, ctx.Remainder)` with no `P.ServerMessage` wrap. A token join would normalize doubled/trailing spaces and corrupt the injected packet |
| `/gmhax ` | `GMHaxCommandEvent` | — (no typed params) | same raw-fidelity rule: the raw remainder is embedded in the CHP packet; move the in-body packet building verbatim over `ctx.Remainder` |
| `/custom` | `CustomCommandEvent` | subcommands, Task 3 | |

Deletion-safety: before each batch's deletions, re-run the Part 2 grep pattern for that batch's class names; every Part 3 event is currently referenced only by `Goose/EventHandler.cs` (verify, and if any new reference appears, keep the class and flag it).

---

### Task 0: Multi-name subcommands (for `/custom`'s `create`|`make` alias)

**Files:**
- Modify: `Goose/Commands/SubcommandAttribute.cs`, `Goose/Commands/CommandRegistry.cs` (discovery validation)
- Test: `Goose.Tests/CommandRegistryTests.cs`

**Step 1: Failing tests** — a `[Subcommand("make", "create", ...)]` method is reachable via both first tokens (case-insensitive); the help page lists the **first** name; unknown tokens still get the subcommand list.

**Step 2: Implement** — `SubcommandAttribute(params string[] names)`; definition stores all names, first is primary (help/usage).

**Step 3: Run** `dotnet test Goose.Tests` — green. **Step 4: Commit** `feat: subcommand alias names`

---

### Task 1: Migrate GM batch A — target-player commands (17)

**Files:**
- Create: `Goose/Commands/SummonCommand.cs`, `ApproachCommand.cs`, `KickCommand.cs`, `UnbanCommand.cs`, `BanCommand.cs`, `BroadcastCommand.cs`, `PlayerInfoCommand.cs`, `GiveGoldCommand.cs`, `GiveExperienceCommand.cs`, `GiveCreditsCommand.cs`, `SetTitleCommand.cs`, `SetSurnameCommand.cs`, `ChangeClassCommand.cs`, `ChangeNameCommand.cs`, `CheckNameCommand.cs`, `SetPasswordCommand.cs`, `MacroCheckCommand.cs`
- Modify: `Goose/EventHandler.cs` (remove 17 keys), `TestSupport/TestWorldFixture.cs` (add `RegisterDatabasePlayer(Player)` — reflection-populates the private `allNameToPlayer` dict, `Goose/PlayerHandler.cs:19`, mirroring the existing `RegisterOnlinePlayer` at `TestSupport/TestWorldFixture.cs:88`; needed because `GetPlayerFromData` targets are often offline/DB-only)
- Delete: the 17 legacy events
- Test: `Goose.Tests/Part3GmATests.cs`

**Steps:** migrate per rule + table. Section: `GM`. Tests:

- ★ `/ban Bob 30` (GM, `RegisterOnlinePlayer` + fixture DB as needed) vs Normal → swallowed, no reply.
- ★ `/summon Ghost` → `Couldn't find player Ghost.`; `/summon Bob` with Bob in `LoadingMap` → `Player is still loading a map.` (in-body check survived).
- `/broadcast hello world` → `world.SendToAll` message contains `[Normal]: hello world` (join preserved spaces).
- ★ `/givecredits Bob 0` → silent no-op (validation return kept, not converted to a usage error).
- `/changeclass Bob Warrior 1.5` → decimal modifier bound; `/changeclass Bob Warrior` → modifier null.
- `/playerinfo Bob` → `PlayerInfoWindow` added to the viewer's `Windows`.

Run `dotnet test` (both). Commit: `refactor: migrate GM target-player commands`.

---

### Task 2: Migrate GM batch B + Admin (16)

**Files:**
- Create: `Goose/Commands/WarpCommand.cs`, `GetItemCommand.cs`, `SpawnNpcCommand.cs`, `PlaceSpawnCommand.cs`, `SearchCommand.cs`, `MuteMapCommand.cs`, `ShutdownCommand.cs`, `SetAccessCommand.cs`, `SetConfigCommand.cs`, `SaveConfigCommand.cs`, `RespawnMapCommand.cs`, `ReloadScriptsCommand.cs`, `ReloadSqlCommand.cs`, `UpdateSqlCommand.cs`, `HaxCommand.cs`, `GmHaxCommand.cs`
- Modify: `Goose/EventHandler.cs` (remove 16 keys)
- Delete: the 16 legacy events
- Test: `Goose.Tests/Part3GmAdminTests.cs`

**Steps:** migrate per rule + table. Sections: `GM` (first 6) / `Admin` (rest). Tests:

- ★ `/warp 2 5 5` → warps to 2,5,5; `/warp 2` (partial args) → silent no-op, player does not move (the in-body `ctx.Args.Length != 3` guard preserves the legacy `tokens.Length == 4` all-or-nothing behavior — regression-pinned); bare `/warp` → no trie match (trailing-space key), `RunCommand` returns false, nothing sent (legacy parity).
- `/getitem 5 2` → item added with stack 2; `/getitem 5 powerful` → powerful path; `/getitem 5 2 powerful` → both.
- ★ `/hax M1,5,5` → raw packet echoed to the player unmodified (must not gain a `$7` prefix); ★ `/hax  M1,5,5` (double space) → the doubled space survives in `Sent` (raw `ctx.Remainder` fidelity — a token join would fail this).
- `/setconfig foo bar baz` → value = `bar baz` (join).
- `/search item sword` → regex results unchanged (assert one known match from the fixture templates).
- `/mutemap` → map `Muted` flips and map broadcast sent.
- ★ `/shutdown` as Normal → swallowed, `world.Running` stays true; as GM → `world.Running` becomes false (the legacy body's only effect, `Goose/Events/ShutdownCommandEvent.cs` — assert it directly).

Run `dotnet test` (both). Commit: `refactor: migrate GM world commands and Admin commands`.

---

### Task 3: Migrate `/custom` (subcommands)

**Files:**
- Create: `Goose/Commands/CustomCommand.cs`
- Modify: `Goose/EventHandler.cs` (remove `/custom`)
- Delete: `Goose/Events/CustomCommandEvent.cs`
- Test: `Goose.Tests/Part3CustomTests.cs`

**Steps:**

- Subcommands: `help` (no args), `kill` (no args), `preview` (`int r, int g, int b, int a, string[] rest`), `make` with alias names `make`/`create` (same params). **name = `string.Join(" ", rest)`** — legacy `Split(' ', 6, RemoveEmptyEntries)` makes `tokens[5]` the rest of the line, so `/custom make 1 2 3 4 My Sword` names the item "My Sword"; a single `string` param would truncate it.
- Move `ParseRGBA`, `ValidateCustomSlots`, `EquippedDisplay`, `MountDisplay` into the new class verbatim (they are static/instance helpers on the legacy class).
- Section: `Customs`.

Tests:

- ★ Bare `/custom` → subcommand list (help/kill/preview/make with usage), not the old ticket-usage line; `/custom help` → the ticket instructions (kept).
- ★ `/custom make 255 0 0 255 MySword` and `/custom create 255 0 0 255 MySword` → identical behavior (alias pinned); `/custom make 255 0 0 255 My Sword` → item named `My Sword` (multi-word name via rest join — regression-pinned).
- `/custom make 300 0 0 0 X` → the legacy `invalid r value` message (in-body `ParseRGBA` kept).
- `/custom preview ...` → preview packet path executes (assert the `MKC`-shaped packet in `Sent`).
- Missing combine-bag ticket → the legacy refusal message.

Run `dotnet test` (both). Commit: `refactor: migrate /custom to subcommands`.

---

### Task 4: Migrate dimension scripts and harden `RegisterEvent`

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:229-233` (five `RegisterEvent` calls → five `world.Commands.Register` calls), `Goose/Data/Illutia/Scripts/Global/Dimensions/Commands.csx` (five event classes → five static handler methods or lambdas), `Goose/EventHandler.cs:248` (`RegisterEvent`: reject `/` keys with a logged error; delete the privilege overload at :267), `Goose.Tests/EventHandlerTests.cs`
- Test: existing `Goose.IntegrationTests/Dimension*Tests` are the primary net; add `Goose.IntegrationTests/DimensionCommandRegistrationTests.cs`

**Steps:**

1. For each of the five commands, convert the event's `Ready` body into a handler lambda registered in `OnLoaded` with section `"Dimensions"` and a one-line help:
   - `/dimension ` → `(CommandContext ctx, int dim)` — keep `dim < 0 || dim > DimensionCount` refusal and the `PlayerCanJoin` gate verbatim.
   - `/resetitem ` → `(CommandContext ctx, int slotId)` — keep the `1..InventorySize` refusal and all in-body logic.
   - `/buygold ` → `(CommandContext ctx, long amount)` — keep `amount > 0` (replaces `TryParseAmount`'s role; delete `TryParseAmount` if now unused — grep first).
   - `/buyexperience ` → `(CommandContext ctx, long amount)` — same.
   - `/givesp ` → `(CommandContext ctx, Player target, long amount)` — keep the `target.State != Ready → "<name> is not online."` check in-body (binder only resolves existence; message for a never-online name changes from "`<name>` is not online." to "Couldn't find player `<name>`." — intended, note in commit message).
   - Registration keys keep their trailing spaces; privileges: all five were `Open` — register open (omitted privilege).
2. Delete the five event classes from `Commands.csx`.
3. Harden `EventHandler.RegisterEvent(string, CreateEvent)`: key starts with `/` → log error, return without registering. Delete `RegisterEvent(string, CreateEvent, AccessPrivilege)`. Update `Goose.Tests/EventHandlerTests.cs`: the downgrade-policy test now covers the registry (already in Part 1/2 tests) — replace with: `/` key rejected, non-`/` key (e.g. `"GID"`) still registers and dispatches.
4. New integration tests (`GlobalScriptFixture`, mirroring `DimensionCommandGateTests` setup):
   - `RunCommand(player, "/dimension 5")` still warps (regression through the new registration path).
   - ★ `/dimension` with no arg → framework usage reply (was the custom `/dimension <0-6>` line — intended delta, pinned).
   - ★ `/givesp Bob 10` end-to-end: spirit transfers, both players messaged (uses `RegisterOnlinePlayer`).
   - ★ Aspereta-style non-command registration still works: `world.EventHandler.RegisterEvent("GID", factory)` dispatches.
   - ★ `RegisterEvent("/sneaky ", factory)` → not dispatchable, error logged.
5. Full suite: `dotnet test Goose.Tests && dotnet test Goose.IntegrationTests` — all dimension tests must pass unmodified.

Commit: `refactor: dimension scripts use Commands.Register; RegisterEvent restricted to non-command packets`

---

### Task 5: Final integration, compliance sweep, design alignment

**Files:**
- Create: `Goose.IntegrationTests/Part3MigrationTests.cs`

**Steps:**

1. End-to-end: `/warp 2 5 5` (GM) warps; `/warp 2 5 5` (Normal) swallowed; `/custom make ...` full flow with fixture combine bag; `/help` — the fixture loads **no dimension scripts**, so the sections are exactly the seven built-in ones: a GM sees all seven (incl. `GM`/`Admin`), a Normal player sees only `General`/`Party`/`Guild`/`Pets`/`Customs`; `/help custom` shows the four subcommands (`make`, not `create`, as the listed name).
2. Compliance sweep:
   - Delete `Goose/Events/InstaLevelCommandEvent.cs` — unreferenced dead code (verified: no references anywhere); without this the sweep below would trip on it.
   - `ls Goose/Events/*CommandEvent.cs` → only `RefreshPositionEvent.cs` remains (kept for the `RPU` packet); list it explicitly in the commit message.
   - `_SeedCommands` contains zero `/` keys.
   - `git grep "RegisterEvent"` → only the non-command `GID` usage in Aspereta.csx, the definition, and its test.
   - No `.csx` file references a deleted event class.
3. Design alignment: walk the design doc's promises (usage formats, "Couldn't find player" wording, swallow-on-deny, section list, 42-char wrap, collision behavior) against the implemented strings; fix divergences or note them as accepted deltas.
4. Full suite green.

Commit: `test: Part 3 final integration and compliance sweep`

---

## Invariant-to-test matrix (Part 3)

| Invariant | Proved by |
|---|---|
| Restricted GM/Admin commands swallowed for Normal, active for GM | `Part3GmATests` ★, `Part3GmAdminTests` ★, `Part3MigrationTests` |
| Offline-target commands use `GetPlayerFromData`, not the binder `Player` | `Part3GmATests` (ban/givegold on DB-only player) |
| `/hax` raw pass-through unmodified | `Part3GmAdminTests` ★ |
| `/warp` defaults + all-or-nothing decision pinned | `Part3GmAdminTests` ★ |
| `/custom` subcommands incl. `create` alias; in-body validation kept | `Part3CustomTests` ★ |
| Dimension commands behave identically through `Commands.Register` | existing `Dimension*Tests` (unmodified) + `DimensionCommandRegistrationTests` |
| `RegisterEvent` rejects `/` keys, still serves non-command packets | `DimensionCommandRegistrationTests` ★ + `EventHandlerTests` |
| Aspereta `GID` registration unaffected | `DimensionCommandRegistrationTests` ★ |
| Help shows all sections with correct visibility; `/help custom` lists subcommands | `Part3MigrationTests` |
| No legacy `/` command survives in the seed table | Task 5 compliance sweep |

## Part 3 exit criteria (project completion)

- Zero `/` keys in `_SeedCommands`; every in-game slash command runs through `CommandRegistry`.
- All 70 referenced legacy command event classes deleted (58 suffixed + 12 non-suffixed like `WarpEvent`/`WhoEvent`), plus the dead `InstaLevelCommandEvent.cs`; only `RefreshPositionEvent` survives, justified in the Task 5 commit message.
- Dimension scripts register via `world.Commands.Register`; `RegisterEvent` is non-command-only.
- `dotnet test` green across `Goose.Tests` and `Goose.IntegrationTests`, including all pre-existing dimension and Aspereta tests.
- `/help` window shows the full section model; parse errors reply with usage; denials stay silent.
