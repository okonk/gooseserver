# In-game command system — design

Refactor the in-game slash commands into a proper command system: declarative
registration with automatic typed parameter parsing, centralized permission
enforcement, and a help system. Replaces the 59 hand-rolled `*CommandEvent`
classes and the static dispatch table in `EventHandler._SeedCommands()`.

## Current state

- 59 `*CommandEvent` classes in `Goose/Events/`, each parsing
  `((string)this.Data)` by hand (`Split(' ')`, `Substring(n)`), with silent
  `catch` blocks on bad input.
- Dispatch: `EventHandler._SeedCommands()` — static table of packet prefix →
  event type + optional `AccessPrivilege`, matched via a trie (longest prefix).
  The table is the single source of permission truth; a new command must state
  its access requirement explicitly (no silent default to open).
- `RegisterEvent(key, factory, privilege?)` exists for script-registered
  commands (dimension scripts register commands, e.g. `/resetitem`);
  reload re-registration must replace.
- No in-game help. Denied commands are swallowed (no reply) so unprivileged
  players cannot probe which commands exist.

## Decisions (from brainstorming)

- Keep the event pipeline: commands still produce events run on the game
  thread via the tick queue. No timing-semantics change.
- Attribute-declared command classes (not a central delegate table).
- Big-bang migration: all 59 commands converted, old event classes deleted.
- Help is privilege-filtered (anti-probing policy preserved).
- Subcommands are first-class; argument-dependent access via a `CheckAccess`
  hook.
- Script registration API retained and updated for the new system.
- Parse errors reply with the auto-generated usage line (was silent).
- `Player` / `Player?` are supported parameter types.
- Help opens a paged quest-frame window with sections; `/help <name>` shows
  command details (and the same-named section, if any).

## 1. Framework architecture

New folder `Goose/Commands/`:

- `CommandAttribute` — `[Command("/warp", AccessPrivilege.Warp, Help = "...", Section = "GM")]`.
  Privilege optional; omitting it means `Open` (any player), matching today's
  `Open(...)` vs `Restricted(...)` split. `Section` optional, default
  `"General"`.
- `SubcommandAttribute` — `[Subcommand("make", Help = "...")]` on handler
  methods; may carry its own privilege.
- `CommandContext` — `Player`, `GameWorld`, raw tokens, `ctx.Send(...)`.
- `CommandRegistry` — owns all definitions: a
  `ConcurrentDictionary<string, CommandDefinition>` plus the trie. Built-in
  commands discovered by scanning the Goose assembly once at startup; script
  commands registered via `Register`. Exposed as `world.Commands`.
- `CommandDefinition` — key, privilege (nullable), help text, section,
  parameter list (name, type, default, optional), subcommand map, handler
  (compiled delegate to the class method, or a script delegate).
- `CommandEvent` — single `Event` subclass carrying a parsed
  `CommandInvocation` (definition + bound arguments); `Ready` invokes the
  handler. All 59 old `*CommandEvent` classes are deleted.

Dispatch path unchanged in shape: `EventHandler.AddEvent(player, packet)` →
trie longest-prefix match → permission check (swallowed on deny, debug log) →
enqueue `CommandEvent`. Non-command packets (`LOGIN`, `M1`, `USE`, `;`, ...)
stay in the trie untouched.

Thread safety: the trie is replaced atomically (build a snapshot on
registration, swap the reference) so script registration from the
`/reloadscripts` background thread is safe; the game thread's read path is
lock-free.

Known property (kept from today): a command a script registered before a
reload but no longer registers after it stays live (stale).

## 2. Command declaration API

```csharp
[Command("/warp", AccessPrivilege.Warp, Section = "GM",
    Help = "Teleport to a map position.")]
public sealed class WarpCommand
{
    public void Execute(CommandContext ctx, int mapId = 1, int x = 50, int y = 50)
    {
        Map? map = ctx.World.MapHandler.GetMap(mapId);
        if (map is null || x < 1 || x >= map.Width + 1 || y < 1 || y >= map.Height + 1) return;
        ctx.Player.WarpTo(ctx.World, map, x, y);
    }
}
```

Parameter binding:

- Tokens after the command key are split on whitespace, bound positionally to
  `Execute` parameters.
- Supported types: `int`, `long`, `float`, `double`, `bool`, `string`,
  `Player`, and `string[] rest` as a final parameter (captures all remaining
  tokens). `Player?` / `string?` with a default are optional.
- `Player` binds the next token via `world.PlayerHandler.GetPlayer(name)`
  (online players only); no match → framework sends
  `Couldn't find player <name>.`. Commands that target offline players too
  (`/ban`, `/setpassword`, `/givecredits`, `/playerinfo`) use `string name`
  + `PlayerHandler.GetPlayerFromData(name)` (`Goose/PlayerHandler.cs:178`) in
  the handler body, not the `Player` parameter.
- Missing token for a non-optional parameter, or failed conversion →
  framework sends the auto-generated usage line. No per-command
  `try/catch` around parsing.
- `bool` parses: on/off/true/false/1/0 (case-insensitive).
- Usage strings are generated from the declaration: `/warp [mapId] [x] [y]`,
  `/broadcast <message>`, `/kick <player>`. Brackets = optional, angle
  brackets = required.
- `Player.State == Ready` is enforced by the framework: a `CommandEvent`
  whose player isn't `Ready` is a no-op (matches today's per-command checks,
  in one place).

Subcommands:

```csharp
[Command("/custom", Section = "Customs", Help = "Create a custom item.")]
public sealed class CustomCommand
{
    [Subcommand("make", Help = "Consume the ticket and source items.")]
    public void Make(CommandContext ctx, int r, int g, int b, int a, string[] rest) { ... }

    [Subcommand("help", Help = "Show how customisation works.")]
    public void Help(CommandContext ctx) { ... }
}
```

- First token selects the subcommand (case-insensitive, matching current
  `/custom` behavior); remaining tokens bind to its parameters.
- Bare command or unknown subcommand → framework sends that command's help
  (subcommand list + usage lines).
- Subcommand privileges are checked after the subcommand token matches,
  before its handler runs; denial is swallowed. Class-level privilege gates
  before subcommand dispatch.

Argument-dependent access (`/toggle`; `/givecredits`'s argument-dependent
behavior is plain validation, not access, and migrates verbatim): the command
class may override

```csharp
protected override AccessPrivilege? CheckAccess(CommandContext ctx, string[] args) =>
    args.Length > 0 && args[0] == "invis" ? AccessPrivilege.GMInvisible : null;
```

checked after the class-level privilege passes; denial is swallowed.

## 3. Help system

Two built-ins, both `Open`: `/help` and `/help <name>`.

Sections: `CommandAttribute.Section` (default `"General"`); script
registration takes a `section` parameter. Built-in sections (all 68 existing
commands explicitly assigned):

- **General**: `/help`, `/who`, `/tell`, `/shout`, `/random`, `/auction`,
  `/dropgold`, `/location`, `/refresh`, `/charinfo`, `/credits`, `/playtime`,
  `/changepassword`, `/buyvita`, `/buymana`, `/rank`, `/hairdye`, `/aether`,
  `/toggle`, `/mc`
- **Party**: `/group`, `/groupadd`, `/invite`, `/groupremove`, `/disband`,
  `/togglegroup`
- **Guild**: `/guild`, `/guildcreate`, `/guildadd`, `/guildremove`,
  `/guildmotd`, `/guildowner`, `/guildofficer`
- **Pets**: `/petlist`, `/petspawn`, `/petinfo`, `/petdamage`, `/petvita`,
  `/petdelete`
- **Customs**: `/custom`
- **GM**: `/warp`, `/summon`, `/approach`, `/kick`, `/ban`, `/unban`,
  `/broadcast`, `/getitem`, `/spawnnpc`, `/placespawn`, `/search`, `/mutemap`,
  `/playerinfo`, `/givegold`, `/giveexperience`, `/givecredits`, `/settitle`,
  `/setsurname`, `/changeclass`, `/changename`, `/checkname`, `/setpassword`,
  `/macrocheck`
- **Admin**: `/shutdown`, `/setaccess`, `/setconfig`, `/saveconfig`,
  `/respawnmap`, `/reloadscripts`, `/reloadsql`, `/updatesql`, `/hax`, `/gmhax`

`/help` opens a `HelpWindow` (new `WindowTypes.Help`, Quest frame, modeled on
`PlayerInfoWindow`):

- Page 1: visible sections with command counts. A section is visible if it
  contains at least one command the player may use.
- Subsequent pages: one section's commands (usage + one-line help), paginated
  if longer than a page. Back/Next walk: section list → section 1 → ...
- Re-opening `/help` stacks a new window (no special handling).

`/help warp` opens directly on a single-command page: help text, usage line,
subcommands with their own usage/help.

`/help dimensions` opens on the Dimensions section's page(s). Name resolution:
exact command name and section name are both checked; if both exist, the page
shows the command details first, then the section's list (and a warning is
logged at registration for the collision).

Privilege filtering: only commands the player may use are shown;
`/help hax` and `/help nonexistent` both get no reply for a player lacking the
privilege (anti-probing).

Word wrap: the quest window has a fixed line length. `HelpFormatter` wraps
text at a named constant (`MaxLineLength = 42`, confirmed limit TBD) on word
boundaries. Applies to help text, usage lines, and section lists in the
window. Chat usage-error messages do not wrap.

## 4. Permissions and security

- Privilege stated at declaration; omitting it means `Open`. No third state.
- Denial is swallowed: no reply, debug log only (dispatcher, `CheckAccess`,
  subcommand privilege, and help all consistent).
- The registry refuses to replace a restricted key with an open registration
  (existing policy, existing test).
- `CommandEvent` no-ops unless the player is `Ready`; commands remain
  `ClientOriginated` events so the queue's drop-during-map-load behavior
  applies.
- Non-command packets with in-handler privilege checks (e.g. `ChatEvent`'s
  `TalkWhileMuted`) are untouched.

## 5. Script registration API

`world.Commands.Register(...)`:

```csharp
world.Commands.Register(
    key: "/dimmoney",
    privilege: AccessPrivilege.Ban,   // or omit for Open
    section: "Dimensions",
    help: "Set a player's dimension money.",
    handler: (CommandContext ctx, Player target, int amount) => { ... });
```

- Same binding engine as built-ins: the delegate's parameters are inspected
  identically — typed parsing, `Player` resolution, `rest`, defaults,
  auto-generated usage.
- Validation: duplicate key → replace (reload semantics); key must start with
  `/` and contain no space, else rejected with a logged error; section-name
  collision with a command name → warning.
- Subcommands are not supported in the delegate API; scripts branch on their
  own `rest` first token. Help shows the script command as a single entry.
- Thread-safe (atomic trie swap). Stale commands after reload stay live
  (same as today).
- `EventHandler.RegisterEvent(key, factory)` is kept for **non-command
  packets only** (Aspereta.csx:237 registers the `GID` packet through it).
  Keys starting with `/` are rejected with a logged error — slash commands
  must go through `Commands.Register`. The
  `RegisterEvent(key, factory, privilege)` overload (unused anywhere) is
  deleted.
- The shipped dimension scripts migrate to the new API in Part 3:
  `Dimensions.csx:229-233` (`/dimension`, `/resetitem`, `/buygold`,
  `/buyexperience`, `/givesp`) and their event classes in
  `Dimensions/Commands.csx` become `world.Commands.Register` calls with
  section `"Dimensions"`. Their existing integration tests
  (`DimensionCommandGateTests`, `DimensionCurrencyCommandTests`, ...) are the
  regression net.

## 6. Migration and testing

Order:

1. Framework in `Goose/Commands/` (attributes, context, binder, registry,
   `CommandEvent`, `HelpFormatter`, `HelpWindow`, `/help`). Old table still
   runs.
2. Migrate all 59 commands in batches by area (GM/player, guild, pets,
   custom, ...). Each batch: new command class(es), old `*CommandEvent`
   deleted, entry removed from `_SeedCommands`. Non-command packets stay.
3. Migrate the dimension scripts to `world.Commands.Register` (Part 3);
   keep `RegisterEvent(key, factory)` for non-command packets, reject `/`
   keys there, delete the privilege overload and update its test.
   `EventHandler` dispatches via the registry.

Behavior preservation: same keys (including trailing spaces), same
privileges, same visible behavior. Intended changes only: parse errors reply
with usage (was silent); `/tell` to a missing player says "Couldn't find
player" (was silent); help exists. Edge-case logic (e.g. `/custom` slot
validation, `/warp` defaults) moves into handlers verbatim.

Tests:

- Unit (`Goose.Tests`): binder (each type, defaults, missing/invalid tokens,
  `rest`, `Player`/`Player?`, usage generation); registry (privilege gating,
  swallow-on-deny, downgrade refusal, replace-on-reregister, sections,
  thread-safe registration); help filtering (normal vs GM visibility,
  command+section collision shows both); `HelpFormatter` wrapping.
- Integration (`Goose.IntegrationTests`): representative set through the
  packet path — open commands (`/warp` with/without args, `/broadcast`),
  restricted denied + allowed (`/ban`), subcommand command (`/custom help`),
  `/help` visibility, script-registered command including a reload that
  replaces it.
- Existing `EventHandlerTests` and tests referencing deleted event classes
  updated in the same batch as their migration.

Risks: (a) a migrated command subtly changing behavior — mitigated by the
verbatim-move rule + integration sample; (b) quest-window line limit wrong —
single named constant.

## Out of scope

- Console commands (`Goose/Console`) — untouched.
- Non-command packets in the trie — untouched.
- Unload-time cleanup of stale script commands — kept as-is.
- Declared subcommands for script-registered commands — future work if needed.
- Pagination beyond Back/Next paging in the help window.
