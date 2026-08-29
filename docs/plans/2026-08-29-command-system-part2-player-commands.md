# Command System — Part 2: Migrate Player-Facing Commands Implementation Plan

**Goal:** Migrate the General, Party, Guild, and Pets command sections (38 commands) from legacy event classes to `[Command]` classes on the Part 1 framework, deleting the old events and legacy registrations.

**Architecture:** Each legacy `*CommandEvent` becomes a `BaseCommand`-derived class in `Goose/Commands/` whose `Execute` body is the old `Ready` body verbatim minus the state check and hand-parsing (the binder now supplies typed parameters). The legacy registration is removed from `EventHandler`'s seed table. Behavior is preserved; the only intended deltas are framework-generated usage replies on parse failure and the new `/help` visibility of these commands.

**Tech Stack:** C# / .NET 10, xUnit, Part 1 framework (`Goose/Commands/`).

**This is Part 2 of 3.** Prerequisite: Part 1 (`docs/plans/2026-08-29-command-system-part1-framework.md`) complete and green.

Design doc: `docs/plans/2026-08-29-command-system-design.md`

---

## APIs verified

| API | Location |
|---|---|
| Part 1 surface: `CommandAttribute`, `BaseCommand`, `CommandContext` (`Player`, `World`, `Registry`, `Args`, `Send`), `CommandBinder.Bind/Usage`, `CommandRegistry` | `Goose/Commands/` (Part 1) |
| Legacy seed table to edit (keys + privileges copied verbatim) | `Goose/EventHandler.cs:120` (`_SeedCommands`) |
`CommandRegistry.RegisterLegacy(key, Type, AccessPrivilege?)` (Part 1 Task 3) | `Goose/Commands/CommandRegistry.cs` |
| `TestWorldFixture.RunCommand / CommandPlayerOn / RegisterOnlinePlayer`, `CapturingPlayer.Sent` | `TestSupport/TestWorldFixture.cs:105,75,88` |
| `PlayerHandler.GetPlayer(string)` case-insensitive | `Goose/PlayerHandler.cs:133` |
| `Player.HasPrivilege` | `Goose/Player.cs:529` |

## Migration rule (applies to every command in this part)

1. Read the legacy event class in full.
2. Create the new command class in `Goose/Commands/` with the attribute (key verbatim including trailing space, privilege verbatim — `Open` entries omit it — section + help text per the design doc's section list).
3. Move the `Ready` body into `Execute` verbatim: drop the `Player.State == Ready` check (framework-enforced) and the token parsing (binder-supplied parameters). Keep every game-logic line, message string, and edge case unchanged.
4. Remove the key from `_SeedCommands` (the non-command packet entries never move).
5. Delete the legacy event class — **unless** it is referenced elsewhere (verified below); then keep it and leave its other key(s) registered legacy.
6. Add/adjust tests in the batch's test file; run the full suite.

**Deletion-safety (verified by grep, `Goose/**/*.cs` excluding the event file itself):** every Part 2 event is referenced only by `Goose/EventHandler.cs` — **except `RefreshPositionEvent`, which is also the `RPU` packet's event. Keep `RefreshPositionEvent.cs`; only its `/refresh` key migrates; `RPU` stays legacy.** `GiveCreditsCommandEvent`'s grep hit on `CreditsCommandEvent` is a substring of its own class name — safe.

**Two-key aliases:** `/invite ` + `/groupadd ` both map to `GroupAddEvent`; `/disband` + `/groupremove` both map to `GroupRemoveEvent`. One attribute = one key, so Task 0 adds multi-key support.

## Per-command parameter mapping (verified against the legacy sources)

| Key(s) | Legacy event | `Execute` parameters | Notes |
|---|---|---|---|
| `/who` | `WhoEvent` | `string? scope = null, string[] rest` | old code splits the packet: `/who`, `/who all [query...]`, `/who guild [query...]`. **Name-only form `/who bob` means scope=all with query including "bob"** (`WhoEvent.cs:48-51`: `players = all; query = join(search, 1, ...)`). Handler: if `scope` is null → all players, query = `string.Join(" ", [name, ..rest])`; else branch per legacy with query = `rest` joined |
| `/tell ` | `TellEvent` | `Player target, string[] rest` | message = `string.Join(" ", rest)`; keep the 300-char cap and `UpdateIdleStatus` call; missing target now gets "Couldn't find player" (intended delta) |
| `/shout ` | `ShoutCommandEvent` | `string[] rest` | message = join; keep mute check |
| `/random` | `RandomCommandEvent` | — (no args) | keep mute check |
| `/auction ` | `AuctionCommandEvent` | `string[] rest` | message = join; keep mute check |
| `/dropgold ` | `PlayerDropGoldEvent` | `int gold` | |
| `/location` | `LocationEvent` | — | |
| `/refresh` | `RefreshPositionEvent` | — | **class kept for `RPU`**; new command calls the same logic — extract the shared body into a static helper on the event class or duplicate the 2 lines; prefer the helper |
| `/charinfo` | `CharacterInfoCommandEvent` | — | |
| `/credits` | `CreditsCommandEvent` | — | |
| `/playtime` | `PlaytimeCommandEvent` | — | |
| `/changepassword ` | `ChangePasswordCommandEvent` | `string[] rest` | password = join — old `Substring(16)` is the **rest of the line**; passwords may contain spaces |
| `/buyvita` | `BuyVitaCommandEvent` | `int buys = 1` | **bare `/buyvita` (no trailing space in key) buys 1 in legacy** (`Split(' ')[1]` throws → catch → default 1) — the default preserves that; bad token → usage reply (intended delta) |
| `/buymana` | `BuyManaCommandEvent` | `int buys = 1` | same |
| `/rank` | `RankCommandEvent` | `string? arg = null` | old code strips one leading space then lowercases |
| `/hairdye` | `HairdyeCommandEvent` | `string[] rest` | **legacy has no bare-numeric dye path** — bare/`help` sends the usage line (`HairdyeCommandEvent.cs:13-16`), then the switch is `accept`/`gogodyeme`/`preview`/`kill` with no default case, and each verb takes four ints *after* the verb (`ParseRGBA` reads `tokens[1..4]`, so `/hairdye accept 255 0 0 255` is 5 tokens). Handler: `rest` empty or `rest[0] == "help"` → legacy usage message; else move the switch and `ParseRGBA` verbatim over `rest`; unknown verb → silent no-op (legacy parity) |
| `/aether ` | `AetherCommandEvent` | `decimal thres` | **requires `decimal` binder support (Task 0)** |
| `/mc ` | `MacroConfirmCommandEvent` | `string[] rest` | code = join — old `Substring("/mc ".Length)` is the rest of the line |
| `/group ` | `GroupChatEvent` | `string[] rest` | message = join |
| `/invite ` + `/groupadd ` | `GroupAddEvent` | `string name` | alias pair (Task 0) |
| `/disband` + `/groupremove` | `GroupRemoveEvent` | — (no typed params; handler reads `ctx.Remainder`) | **trailing-space edge is load-bearing** (`GroupRemoveEvent.cs:19-29`): `Split(' ')` without `RemoveEmptyEntries` distinguishes no-token (leave group) from empty-token (silent no-op). Handler: `Remainder` empty → leave-group path; `Remainder.Trim()` empty → silent return; else name = trimmed. A token-only binder would collapse the two and send `/groupremove ` down the leave path |
| `/togglegroup` | `ToggleGroupCommandEvent` | — | |
| `/toggle ` | `ToggleCommandEvent` | `string setting, string[] rest` | **`CheckAccess` override**: `setting` ∈ {`gm-invisible`, `invisible`} → `AccessPrivilege.GMInvisible`; ∈ {`who-invisible`, `whoinvisible`} → `AccessPrivilege.WhoInvisible`; else null. Case-insensitive compare (old code lowercases). Remove the now-redundant in-body `HasPrivilege` checks for those two cases only — every other case's logic is verbatim |
| `/guild ` | `GuildChatCommandEvent` | `string[] rest` | message = join |
| `/guildcreate ` | `GuildCreateCommandEvent` | `string[] rest` | guild name = join (may contain spaces) |
| `/guildadd ` | `GuildAddCommandEvent` | `string name` | |
| `/guildremove` | `GuildRemoveCommandEvent` | — (no typed params; handler reads `ctx.Remainder`) | same trailing-space edge as `/groupremove` (`Substring(12)` + `name.Length > 0 → Substring(1)`): empty remainder → leave-guild path, whitespace-only → the legacy rank-check/"Couldn't find player" path — move verbatim over `Remainder` |
| `/guildmotd` | `GuildMotdCommandEvent` | `string[] rest` | MOTD = join (may be empty = clear) |
| `/guildowner ` | `GuildOwnerCommandEvent` | `string name` | |
| `/guildofficer ` | `GuildOfficerCommandEvent` | `string name` | |
| `/petlist` | `PetListCommandEvent` | — | |
| `/petspawn ` | `PetSpawnCommandEvent` | `int id` | |
| `/petinfo ` | `PetInfoCommandEvent` | `int id` | |
| `/petdamage ` | `PetDamageCommandEvent` | `int petid, int buys = 1` | old code defaults both on parse failure; missing `buys` → 1, bad token → usage reply |
| `/petvita ` | `PetVitaCommandEvent` | `int petid, int buys = 1` | same |
| `/petdelete ` | `PetDeleteCommandEvent` | `int id` | |

---

### Task 0: Framework prerequisites — multi-key aliases and `decimal`

**Files:**
- Modify: `Goose/Commands/CommandAttribute.cs`, `Goose/Commands/CommandBinder.cs`, `Goose/Commands/CommandRegistry.cs`
- Test: `Goose.Tests/CommandBinderTests.cs` (add `decimal` cases), `Goose.Tests/CommandRegistryTests.cs` (add alias cases)

**Step 1: Failing tests**

- Binder: `decimal` binds from `"1.5"` (invariant); `"1,5"` → usage error; `decimal` with default works.
- Registry/discovery: a `[Command("/invite ", "/groupadd ", ...)]` class registers under **both** keys; `TryGet` hits both; help lists the **first** key as the command's usage key; replacing/downgrade checks apply per key (registering `/groupadd` again as open when the alias pair is restricted → refused).

**Step 2: Implement**

- `CommandAttribute(string key, ...)` → `CommandAttribute(string firstKey, string secondKey = null, ...)` or `params string[]` — pick `params string[]`; validate each key.
- `CommandDefinition` gains `string[] Keys`; first key is `PrimaryKey` (used for usage strings and help). Registry inserts all keys pointing at the same definition; the ordered list stores the definition once.
- Binder: add `decimal` (invariant parse) alongside the other numerics.

**Step 3: Run** `dotnet test Goose.Tests` — green.

**Step 4: Commit** `feat: command alias keys and decimal parameter support`

---

### Task 1: Migrate General batch A (12 commands)

**Files:**
- Create: `Goose/Commands/WhoCommand.cs`, `TellCommand.cs`, `ShoutCommand.cs`, `RandomCommand.cs`, `AuctionCommand.cs`, `DropGoldCommand.cs`, `LocationCommand.cs`, `RefreshCommand.cs`, `CharInfoCommand.cs`, `CreditsCommand.cs`, `PlaytimeCommand.cs`, `ChangePasswordCommand.cs`
- Modify: `Goose/EventHandler.cs` (`_SeedCommands`: remove the 12 `/` keys; keep `RPU`), `Goose/Events/RefreshPositionEvent.cs` (extract shared body to `public static void Refresh(Player, GameWorld)` helper if not already shaped for it)
- Delete: `Goose/Events/WhoEvent.cs`, `TellEvent.cs`, `ShoutCommandEvent.cs`, `RandomCommandEvent.cs`, `AuctionCommandEvent.cs`, `PlayerDropGoldEvent.cs`, `LocationEvent.cs`, `CharacterInfoCommandEvent.cs`, `CreditsCommandEvent.cs`, `PlaytimeCommandEvent.cs`, `ChangePasswordCommandEvent.cs` (**not** `RefreshPositionEvent.cs`)
- Test: `Goose.Tests/Part2GeneralATests.cs`

**Steps:** migrate per the rule + mapping table (Sections: all `General`). Tests (one per command is overkill; assert the behavior-bearing ones):

- `/tell`: `RegisterOnlinePlayer` a target; `RunCommand(p, "/tell Bob hello there")` → both players' `Sent` contain the tell lines; `RunCommand(p, "/tell Ghost hi")` → `Sent` contains `Couldn't find player Ghost.` (★ the intended delta, regression-pinned).
- `/who all`: with two online players, reply lists both; `/who` (no args) lists map players.
- `/dropgold 50`: gold decreases (assert via player's gold before/after) — or at minimum no usage error and the event's side effects fire.
- `/changepassword abc`: password path executes (assert no usage reply; use the event's own success/failure message).
- `/changepassword my secret pw`: full multi-word password reaches the handler (join, not first-token).
- `/buyvita` bare → buys 1 (legacy default preserved); `/buyvita abc` → usage reply.
- ★ Parse failure: `RunCommand(p, "/dropgold abc")` → `Sent` contains `Usage: /dropgold <gold>` (framework reply, was silent).
- `/refresh`: still works and `RPU` packet still dispatches (both keys alive).

Run `dotnet test` (both projects). Commit: `refactor: migrate General batch A commands to the command system`.

---

### Task 2: Migrate General batch B (7 commands)

**Files:**
- Create: `Goose/Commands/BuyVitaCommand.cs`, `BuyManaCommand.cs`, `RankCommand.cs`, `HairdyeCommand.cs`, `AetherCommand.cs`, `MacroConfirmCommand.cs`, `ToggleCommand.cs`
- Modify: `Goose/EventHandler.cs` (remove 7 keys)
- Delete: the 7 legacy events (`BuyVitaCommandEvent.cs`, `BuyManaCommandEvent.cs`, `RankCommandEvent.cs`, `HairdyeCommandEvent.cs`, `AetherCommandEvent.cs`, `MacroConfirmCommandEvent.cs`, `ToggleCommandEvent.cs`)
- Test: `Goose.Tests/Part2GeneralBTests.cs`

**Steps:** migrate per rule + table. Tests:

- ★ `/toggle gm-invisible` as Normal → swallowed (no reply, no state change); as GM → toggle state flips (assert `Player.ToggleSettings` or the event's reply).
- ★ `/toggle who-invisible` as Normal → swallowed; GM → works.
- `/toggle exp` (open case) as Normal → works, no privilege involvement.
- `/aether 1.5` → executes (decimal bound); `/aether abc` → usage reply.
- `/rank` no-arg and with-arg paths both execute.
- ★ `/hairdye accept 255 0 0 255` (sufficient gold) → dye path runs, cost charged; `/hairdye 255 0 0 255` (no verb) → silent no-op (legacy has no bare-numeric path — regression-pinned); `/hairdye accept 300 0 0 0` → legacy out-of-range refusal message.
- ★ `/groupremove ` (trailing space, empty name) → silent no-op, player stays in group; `/groupremove Bob` → Bob removed; bare `/disband` → leave-group path. (Requires `ctx.Remainder` — the token-only binder would break this.)

Run `dotnet test` (both). Commit: `refactor: migrate General batch B commands incl. /toggle CheckAccess`.

---

### Task 3: Migrate Party (6) and Guild (7)

**Files:**
- Create: `Goose/Commands/GroupCommand.cs`, `GroupAddCommand.cs` (keys `/invite `, `/groupadd `), `GroupRemoveCommand.cs` (keys `/disband`, `/groupremove`), `ToggleGroupCommand.cs`, `GuildCommand.cs`, `GuildCreateCommand.cs`, `GuildAddCommand.cs`, `GuildRemoveCommand.cs`, `GuildMotdCommand.cs`, `GuildOwnerCommand.cs`, `GuildOfficerCommand.cs`
- Modify: `Goose/EventHandler.cs` (remove 13 keys — both alias keys for each pair)
- Delete: `GroupChatEvent.cs`, `GroupAddEvent.cs`, `GroupRemoveEvent.cs`, `ToggleGroupCommandEvent.cs`, `GuildChatCommandEvent.cs`, `GuildCreateCommandEvent.cs`, `GuildAddCommandEvent.cs`, `GuildRemoveCommandEvent.cs`, `GuildMotdCommandEvent.cs`, `GuildOwnerCommandEvent.cs`, `GuildOfficerCommandEvent.cs`
- Test: `Goose.Tests/Part2PartyGuildTests.cs`

**Steps:** migrate per rule + table. Sections: Party / Guild. Tests:

- ★ Alias dispatch: `RunCommand(p, "/invite Bob")` and `RunCommand(p, "/groupadd Bob")` both reach the same handler (assert the event's reply/side effect for each).
- ★ Help shows the first key only: `/groupadd` appears in help as `/invite` (assert via `HelpFormatter.BuildPages` lines) — pins the alias help policy.
- `/guildmotd hello world` → MOTD set to `hello world` (join, spaces preserved); `/guildmotd` → cleared (old behavior).
- `/guildcreate Test Guild` → guild created with name `Test Guild`.
- `/group hi` → group chat reply path executes.

Run `dotnet test` (both). Commit: `refactor: migrate Party and Guild commands incl. alias pairs`.

---

### Task 4: Migrate Pets (6)

**Files:**
- Create: `Goose/Commands/PetListCommand.cs`, `PetSpawnCommand.cs`, `PetInfoCommand.cs`, `PetDamageCommand.cs`, `PetVitaCommand.cs`, `PetDeleteCommand.cs`
- Modify: `Goose/EventHandler.cs` (remove 6 keys)
- Delete: `PetListCommandEvent.cs`, `PetSpawnCommandEvent.cs`, `PetInfoCommandEvent.cs`, `PetDamageCommandEvent.cs`, `PetVitaCommandEvent.cs`, `PetDeleteCommandEvent.cs`
- Test: `Goose.Tests/Part2PetsTests.cs`

**Steps:** migrate per rule + table. Section: Pets. Tests:

- `/petdamage 1 2` → both ints bound (assert via the event's side effect or reply); `/petdamage 1` → `buys` defaults to 1; `/petdamage abc` → usage reply.
- `/petspawn 5`, `/petinfo 5`, `/petdelete 5` → execute paths; bad id → usage reply (was silent).
- `/petlist` → executes with no args.

Run `dotnet test` (both). Commit: `refactor: migrate Pets commands to the command system`.

---

### Task 5: Part 2 integration tests and design-compliance sweep

**Files:**
- Create: `Goose.IntegrationTests/Part2MigrationTests.cs`

**Steps:**

1. End-to-end through `RunCommand` (real dispatch → queue → `Ready`):
   - `/tell` to an online player (both sides receive), to a missing player (fixed message).
   - `/toggle gm-invisible` denied (Normal, swallowed) and allowed (GM).
   - `/guildmotd` with a multi-word MOTD.
   - `/who all` with two registered online players.
   - ★ `/help` now lists the migrated commands under General/Party/Guild/Pets for a GM, and hides nothing newly for a Normal player (compare section lists before/after via `HelpFormatter` with the live registry).
   - ★ A migrated command's parse failure replies with the usage line end-to-end (`/dropgold abc`).
2. Compliance sweep: grep `Goose/Events/` — no deleted class remains; `_SeedCommands` contains no key from this part's table; `git grep "GroupAddEvent\|TellEvent\|..."` returns nothing outside history.
3. Full suite: `dotnet test Goose.Tests && dotnet test Goose.IntegrationTests` — all green.

Commit: `test: Part 2 migration integration coverage and compliance sweep`.

---

## Invariant-to-test matrix (Part 2)

| Invariant | Proved by |
|---|---|
| Every migrated command keeps its key, privilege, and game logic | per-batch tests + full existing suite |
| Alias keys dispatch to one command; help shows first key | `Part2PartyGuildTests` ★ |
| `/toggle` per-argument privileges swallowed for Normal, active for GM | `Part2GeneralBTests` ★ |
| `RPU` packet still works after `/refresh` migration | `Part2GeneralATests` |
| Parse failures reply with usage (intended delta, pinned) | `Part2GeneralATests` ★, `Part2MigrationTests` ★ |
| `/tell` missing target → fixed message (intended delta, pinned) | `Part2GeneralATests` ★, `Part2MigrationTests` |
| Multi-word args (guild name, MOTD, who query) survive via `rest` join | `Part2PartyGuildTests`, `Part2GeneralATests` |
| Help reflects migrated commands with correct sections | `Part2MigrationTests` ★ |
| No legacy event class for a migrated command survives | Task 5 compliance sweep |

## Part 2 exit criteria

- 38 commands migrated; 35 legacy event classes deleted (38 keys − 2 alias pairs = 36 classes, minus `RefreshPositionEvent` kept for `RPU`); `_SeedCommands` holds only non-command packets, GM/Admin/Customs legacy entries, and `RPU`.
- `dotnet test` green across both test projects, including all dimension tests.
- `/help` shows the four migrated sections with correct per-privilege visibility.
