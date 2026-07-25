# Server Console Commands — Design

Date: 2026-07-25
Branch: `console-commands`

## Problem

There is no way to type commands at the server console. In particular there is no way
to grant a player GameMaster access without already having a GameMaster online: the
in-game `/setaccess` requires `AccessPrivilege.SetAccess`, which only GameMaster holds.
A freshly provisioned server has nobody who can run it, so the only route today is
editing the database by hand.

## Scope

Four commands, typed at the server console:

| Command | Purpose |
| --- | --- |
| `/setaccess <playername> [level]` | Set a player's access level. Defaults to `GameMaster`. |
| `/who` | List online players with map, level, and access. |
| `/shutdown` | Clean shutdown with the normal save path. |
| `/help` | Usage and description for each command. |

These are console-only. They do not appear in-game, and the existing in-game commands
are left untouched.

### Non-goals

- No shared implementation with the in-game `Events/` commands. The only genuine
  overlap is `/setaccess`, whose transferable logic is about five lines; a refactor of
  working code costs more than the duplication.
- No privilege model for console commands. Physical access to the server console is the
  authorization — that is the entire point of the feature.
- No command history or line editing. `Console.ReadLine()` gives whatever the terminal
  gives.

## Architecture

### Input path

The game loop is single-threaded and owns all player state, and `Console.ReadLine()`
blocks. Executing a command on the thread that read it would mutate `Player` objects
while the game loop reads them.

So: a **background reader thread** does nothing but `Console.ReadLine()` in a loop,
pushing non-blank lines onto a `ConcurrentQueue<string>`. The **game loop drains** that
queue each tick via `ConsoleCommandHandler.Update(GameWorld)`, called from
`GameServer.GameLoop` beside `SweepPreLoginConnections()`. All state mutation stays on
the game thread, and blocking on stdin never stalls the game.

Alternatives rejected:

- Polling `Console.KeyAvailable` in the game loop avoids the thread but requires
  hand-rolled echo and backspace handling, and `KeyAvailable` throws when stdin is
  redirected.
- Running the game loop on a background thread and the console on main restructures
  working startup code around the least important part of the system.

### Two guards that matter

**Start once.** `GameServer.Run()` rebuilds the `GameWorld` and re-enters `GameLoop`
after a crash. The reader thread is therefore started from `Run()` *before* the
`while(true)` restart loop — not from `Start()`. Starting it per-restart would leave two
threads racing for stdin, splitting typed input between them at random. The thread is
`IsBackground = true` so it cannot hold the process open at shutdown.

**Headless.** Under systemd or Docker, stdin is redirected and `ReadLine()` returns
`null` immediately at EOF, which would spin a naive loop hot forever. `Start()` returns
without spawning anything when `Console.IsInputRedirected` (the same check `Program.cs`
already makes before `Console.ReadKey()`), and the read loop breaks on a `null` return
regardless.

### File layout

Mirrors the existing `Events/` convention of one file per command.

```
Goose/Console/ConsoleCommandHandler.cs        thread, queue, dispatch table
Goose/Console/ConsoleCommandParser.cs         Parse() only — generic, shared
Goose/Console/Commands/SetAccessCommand.cs    TryParse + Run + access level matching
Goose/Console/Commands/WhoCommand.cs
Goose/Console/Commands/ShutdownCommand.cs
Goose/Console/Commands/HelpCommand.cs
```

The split that matters is **pure vs. world**, not parsing vs. everything. Generic
tokenizing is shared because every command uses it; `/setaccess`'s argument validation
lives next to its handler because nothing else ever calls it and the two change
together. Putting all validation in the parser would make it a grab bag as commands are
added.

Each command class exposes a static `Run(GameWorld, string[])` plus `Usage` and
`Description` strings. The dispatch table points at them, and `/help` renders from the
same table.

### Dispatch

`Dictionary<string, ConsoleCommand>` keyed on the first whitespace-separated token,
lowercased, with the leading `/` optional so both `/who` and `who` work. Unknown
commands print `Unknown command 'x'. Type /help.`

## Command behavior

### `/setaccess <playername> [level]`

Level defaults to `GameMaster`, which is the bootstrap case this exists for. Resolves
the player via `PlayerHandler.GetPlayerFromData`, so offline characters work.

The level is parsed **by enum name, case-insensitively**, matching the in-game
`/setaccess` exactly. Deliberately not `Enum.TryParse`, which also accepts numeric
strings — `"9"` and even undefined values like `"42"` would parse. Valid names:
`Deleted`, `Banned`, `Normal`, `Helper`, `EventMaster`, `Guide`, `GameMaster`.

On success, sets `player.Access` and — if `player.State == Player.States.NotLoggedIn` —
calls `player.SaveToDatabase(world)`. Online players are covered by the periodic save;
an offline one would otherwise sit in memory until something else wrote the row.

Failures print `No player named 'x'.` or
`Unknown access level 'x'. Valid: ...`

**Audit logging.** Both success and failure log via NLog, so a grant survives the
console scrolling away and a typo'd name that silently did nothing is visible later:

```
log.Info("Console /setaccess: {0} {1} -> {2}", player.Name, previous, player.Access);
log.Info("Console /setaccess failed: no player named {0}.", request.Name);
```

### `/who`

Walks `PlayerHandler.Players`, skipping `Pet` and any player not in `States.Ready`.
Prints `[MapName] Name (Level N, Access)` and a total.

Deliberately does **not** honor `IsGMInvisible` or `IsWhoInvisible` — the console
operator sees everyone.

### `/shutdown`

Sets `world.Running = false`, which is what the in-game `ShutdownCommandEvent` and
`GameServer.RequestShutdown()` both do. Runs the normal save-and-drain path, same as
Ctrl+C. No `GameServer` reference needed.

### `/help`

Renders `Usage` and `Description` for each dispatch table entry.

## Error handling

Each command runs inside a `try/catch` in the drain loop. A handler that throws logs the
exception and prints an error; it cannot take down the tick or the server. The reader
thread body is likewise wrapped, so an stdin IO error kills only console input.

## Testing

### Test project

`Goose.Tests/Goose.Tests.csproj` — net10.0, xUnit, `ProjectReference` to `Goose.csproj`,
added to `Goose.sln`. This is the first test project in the solution.

Under test:

- `ConsoleCommandParser.Parse` — blank and whitespace-only lines, `/who` and `who` both
  resolving, extra whitespace between tokens.
- `SetAccessCommand.TryParse` — `/setaccess Bob` defaulting to `GameMaster`;
  `/setaccess Bob guide` and `/setaccess bob GUIDE` both parsing; unknown level name
  rejected; numeric level `"9"` rejected; missing name rejected; extra trailing args.

Not unit tested: the reader thread, dispatch, and the handlers that touch `GameWorld`.
`GameWorld`'s constructor takes a `GameServer`, opens the database, and loads maps and
scripts, so it cannot be stood up in a test without introducing interfaces over
`PlayerHandler` and `GameWorld` — the refactor this design explicitly declined.

### Manual verification

1. `/setaccess` an offline character; confirm the database row changed.
2. `/setaccess` an online character; confirm GM commands work without relog.
3. `/who`, `/help`, `/shutdown`; confirm players save on shutdown.
4. Run with stdin redirected from `/dev/null`; confirm no hot spin and no crash.

### Known build risks

- `Goose.csproj` marks `Data/**`, `GooseSettings.json`, and `NLog.config` as
  `CopyToOutputDirectory`. Content items propagate to referencing projects, so the test
  project's output directory will receive a copy of the whole `Data` tree. Harmless;
  slower build and a fat `bin`. Suppressible if it becomes annoying.
- `System.ServiceProcess.ServiceController` is Windows-only and development is on Linux.
  The baseline build already emits `CA1416` as a warning, not an error, and the tests do
  not touch `GooseWindowsService`. If the test build does break on it, the fallback is
  linking `ConsoleCommandParser.cs` and `SetAccessCommand.cs` into the test project as
  `Compile Include` items rather than referencing the project.

## Deferred

**Access changes are not pushed to connected clients.** The server enforces privileges
live off `player.Access`, so GM commands work immediately, but anything access-dependent
in the client UI will not refresh until relog. The in-game `/setaccess` behaves the same
way, so this is parity rather than a regression. Not addressed here.
