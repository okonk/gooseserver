# Server Console Commands Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Let an operator type commands at the server console, principally `/setaccess <name>` to grant GameMaster on a server where nobody has it yet.

**Architecture:** A background thread reads stdin and pushes lines onto a `ConcurrentQueue`. The single-threaded game loop drains that queue each tick and runs handlers on the game thread, so no player state is ever mutated concurrently. Commands live one-per-file under `Goose/Console/`, mirroring the existing `Goose/Events/` convention. Argument validation is pure and static so it can be unit tested without standing up a `GameWorld`.

**Tech Stack:** C#, .NET 10, NLog, xUnit (new to this repo).

**Design doc:** `docs/plans/2026-07-25-server-console-commands-design.md`

---

## APIs verified

Every cross-file call this plan makes, cited from source in this worktree:

| API | Location | Signature |
| --- | --- | --- |
| `PlayerHandler.GetPlayerFromData` | `Goose/PlayerHandler.cs:185` | `public Player GetPlayerFromData(string name)` — returns `null` if absent; lowercases the name internally |
| `PlayerHandler.Players` | `Goose/PlayerHandler.cs:167` | `public List<Player> Players { get; }` — online players only |
| `Player.SaveToDatabase` | `Goose/Player.cs:829` | `public virtual void SaveToDatabase(GameWorld world)` |
| `Player.States` | `Goose/Player.cs:62-68` | `enum { NotLoggedIn = 0, LoadingGame, LoadingMap, Ready }` |
| `Player.State` | `Goose/Player.cs:69` | `public States State { get; set; }` |
| `Player.AccessStatus` | `Goose/Player.cs:75-84` | `enum { Deleted = 0, Banned, Normal, Helper = 3, EventMaster = 6, Guide = 7, GameMaster = 9 }` |
| `Player.Access` | `Goose/Player.cs:85` | `public AccessStatus Access { get; set; }` |
| `Player.Name` | `Goose/Player.cs:107` | `public string Name { get; set; }` |
| `Player.Map` | `Goose/Player.cs:144` | `public Map Map { get; set; }` |
| `Player.Level` | `Goose/Player.cs:306` | `public int Level { get; set; }` |
| `Map.Name` | `Goose/Map.cs:29` | `public string Name { get; set; }` |
| `Pet` | `Goose/Pet.cs:16` | `public class Pet : Player` — appears in `PlayerHandler.Players`, must be filtered |
| `GameWorld.PlayerHandler` | `Goose/GameWorld.cs:34` | `public PlayerHandler PlayerHandler { get; set; }` |
| `GameWorld.Running` | `Goose/GameWorld.cs:82` | `public bool Running { get; set; }` — setting `false` exits the game loop and runs the save path |
| `GameServer.Run` | `Goose/GameServer.cs:66` | `public void Run()` — contains the `while(true)` crash-restart loop |
| `GameServer.GameLoop` | `Goose/GameServer.cs:139` | `public void GameLoop()` |
| `GameServer.gameworld` | `Goose/GameServer.cs:44` | `private GameWorld gameworld;` |
| `SweepPreLoginConnections()` call site | `Goose/GameServer.cs:230` | insertion point for the drain |

**Precedent for the in-game equivalent:** `Goose/Events/SetAccessCommandEvent.cs` — its access-level parse (name match, case-insensitive, `.First()` inside a `try`) is what we mirror. `Goose/Events/ShutdownCommandEvent.cs:27` sets `world.Running = false`, which is exactly what console `/shutdown` does.

## Namespace warning — read before writing any file

The new files live in `Goose/Console/`, but their namespace **must be `Goose.ConsoleCommands`**, never `Goose.Console`.

A `Goose.Console` namespace shadows `System.Console` for every file in it, so `Console.WriteLine(...)` stops resolving to the type and the files fail to compile. C# does not require folder and namespace to match, so the folder stays `Console/` while the namespace differs. All six new files use `namespace Goose.ConsoleCommands`.

---

## Task 0: Test project scaffold

This repo has no test project. This task adds the first one; nothing else in the plan can be tested until it exists.

**Files:**
- Create: `Goose.Tests/Goose.Tests.csproj` (via template)
- Modify: `Goose.sln`

**Step 1: Generate the project**

```bash
cd /home/hayden/code/illutiagooseserver/.worktrees/console-commands
dotnet new xunit -o Goose.Tests
```

**Step 2: Add the project reference**

```bash
dotnet add Goose.Tests/Goose.Tests.csproj reference Goose/Goose.csproj
```

**Step 3: Add to the solution**

```bash
dotnet sln Goose.sln add Goose.Tests/Goose.Tests.csproj
```

**Step 4: Delete the template's placeholder test**

```bash
rm -f Goose.Tests/UnitTest1.cs
```

**Step 5: Verify it builds and runs**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj`

Expected: builds with 0 errors, `Passed! - Failed: 0, Passed: 0`. Warnings are expected and fine — the baseline `Goose` build already emits 20, including `CA1416` about `ServiceBase.Run` being Windows-only. That is a warning, not an error, and nothing here touches `GooseWindowsService`.

If the test build *does* fail on the Windows-only `System.ServiceProcess.ServiceController` reference, the fallback is to drop the `ProjectReference` and instead link the two pure files into the test project:

```xml
<ItemGroup>
  <Compile Include="../Goose/Console/ConsoleCommandParser.cs" />
  <Compile Include="../Goose/Console/Commands/SetAccessCommand.cs" />
</ItemGroup>
```

Do not do this pre-emptively — only if step 5 errors.

**Step 6: Commit**

```bash
git add Goose.Tests Goose.sln
git commit -m "test: add Goose.Tests xunit project"
```

---

## Task 1: ConsoleCommandParser

Generic line tokenizing, shared by every command. Pure — no game state.

**Files:**
- Create: `Goose/Console/ConsoleCommandParser.cs`
- Test: `Goose.Tests/ConsoleCommandParserTests.cs`

**Step 1: Write the failing tests**

Create `Goose.Tests/ConsoleCommandParserTests.cs`:

```csharp
using Goose.ConsoleCommands;
using Xunit;

namespace Goose.Tests
{
    public class ConsoleCommandParserTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Parse_ReturnsNullForBlankLines(string line)
        {
            Assert.Null(ConsoleCommandParser.Parse(line));
        }

        [Theory]
        [InlineData("/who")]
        [InlineData("who")]
        [InlineData("  /WHO  ")]
        public void Parse_StripsSlashAndLowercasesName(string line)
        {
            var parsed = ConsoleCommandParser.Parse(line);

            Assert.Equal("who", parsed.Name);
            Assert.Empty(parsed.Args);
        }

        [Fact]
        public void Parse_SplitsArgumentsOnRunsOfWhitespace()
        {
            var parsed = ConsoleCommandParser.Parse("/setaccess   Bob    guide");

            Assert.Equal("setaccess", parsed.Name);
            Assert.Equal(new[] { "Bob", "guide" }, parsed.Args);
        }

        [Fact]
        public void Parse_PreservesArgumentCase()
        {
            var parsed = ConsoleCommandParser.Parse("/setaccess BoB");

            Assert.Equal(new[] { "BoB" }, parsed.Args);
        }
    }
}
```

**Step 2: Run to verify it fails**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj`

Expected: FAIL — `CS0246: The type or namespace name 'ConsoleCommands' does not exist`.

**Step 3: Write the implementation**

Create `Goose/Console/ConsoleCommandParser.cs`:

```csharp
using System;
using System.Linq;

namespace Goose.ConsoleCommands
{
    /**
     * ParsedCommand, a console line split into its command name and arguments
     *
     */
    public sealed class ParsedCommand
    {
        public string Name;
        public string[] Args;
    }

    /**
     * ConsoleCommandParser, generic console line tokenizing
     *
     * Only the parts every command shares live here. Per command argument
     * validation belongs next to that command's handler, so this does not become a
     * dumping ground as commands are added.
     *
     */
    public static class ConsoleCommandParser
    {
        /**
         * Parse, splits a console line into command name and arguments
         *
         * Returns null for a blank line. The leading slash is optional so that both
         * "/who" and "who" work, and the name is lowercased for dispatch. Argument
         * case is preserved, since player names are arguments.
         *
         */
        public static ParsedCommand Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            string[] tokens = line.Trim().Split(
                (char[])null, StringSplitOptions.RemoveEmptyEntries);

            return new ParsedCommand
            {
                Name = tokens[0].TrimStart('/').ToLowerInvariant(),
                Args = tokens.Skip(1).ToArray()
            };
        }
    }
}
```

**Step 4: Run to verify it passes**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj`

Expected: PASS — `Failed: 0, Passed: 7`.

**Step 5: Commit**

```bash
git add Goose/Console/ConsoleCommandParser.cs Goose.Tests/ConsoleCommandParserTests.cs
git commit -m "feat: add console command line parser"
```

---

## Task 2: SetAccessCommand argument validation

The pure half of `/setaccess`: defaulting, name presence, level validation. This is the logic worth testing.

**Files:**
- Create: `Goose/Console/Commands/SetAccessCommand.cs`
- Test: `Goose.Tests/SetAccessCommandTests.cs`

**Step 1: Write the failing tests**

Create `Goose.Tests/SetAccessCommandTests.cs`:

```csharp
using Goose;
using Goose.ConsoleCommands;
using Xunit;

namespace Goose.Tests
{
    public class SetAccessCommandTests
    {
        [Fact]
        public void TryParse_DefaultsToGameMaster()
        {
            Assert.True(SetAccessCommand.TryParse(new[] { "Bob" }, out var request, out _));

            Assert.Equal("Bob", request.Name);
            Assert.Equal(Player.AccessStatus.GameMaster, request.Level);
        }

        [Theory]
        [InlineData("guide")]
        [InlineData("GUIDE")]
        [InlineData("Guide")]
        public void TryParse_MatchesLevelNameCaseInsensitively(string level)
        {
            Assert.True(SetAccessCommand.TryParse(new[] { "Bob", level }, out var request, out _));

            Assert.Equal(Player.AccessStatus.Guide, request.Level);
        }

        [Fact]
        public void TryParse_RejectsMissingName()
        {
            Assert.False(SetAccessCommand.TryParse(new string[0], out var request, out string error));

            Assert.Null(request);
            Assert.Contains("Usage: /setaccess", error);
        }

        [Fact]
        public void TryParse_RejectsUnknownLevelName()
        {
            Assert.False(SetAccessCommand.TryParse(new[] { "Bob", "wizard" }, out _, out string error));

            Assert.Contains("Unknown access level 'wizard'.", error);
            Assert.Contains("GameMaster", error);
        }

        /**
         * Enum.TryParse would accept these. The in game /setaccess matches by name
         * only, and "42" is not even a defined value, so both must be refused.
         */
        [Theory]
        [InlineData("9")]
        [InlineData("42")]
        public void TryParse_RejectsNumericLevels(string level)
        {
            Assert.False(SetAccessCommand.TryParse(new[] { "Bob", level }, out _, out _));
        }

        [Fact]
        public void TryParse_IgnoresExtraTrailingArguments()
        {
            Assert.True(SetAccessCommand.TryParse(new[] { "Bob", "guide", "junk" }, out var request, out _));

            Assert.Equal("Bob", request.Name);
            Assert.Equal(Player.AccessStatus.Guide, request.Level);
        }
    }
}
```

**Step 2: Run to verify it fails**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter SetAccessCommandTests`

Expected: FAIL — `CS0103: The name 'SetAccessCommand' does not exist`.

**Step 3: Write the implementation (validation half only)**

Create `Goose/Console/Commands/SetAccessCommand.cs`:

```csharp
using System;

namespace Goose.ConsoleCommands
{
    /**
     * SetAccessRequest, a validated /setaccess argument list
     *
     * Holds only what the line said. Whether a player by that name exists is the
     * handler's problem, since answering that needs the world.
     *
     */
    public sealed class SetAccessRequest
    {
        public string Name;
        public Player.AccessStatus Level;
    }

    /**
     * SetAccessCommand, /setaccess <playername> [level]
     *
     * Exists so a server where nobody holds GameMaster can grant it. The in game
     * /setaccess requires AccessPrivilege.SetAccess, which only GameMaster has, so
     * on a fresh server it cannot be used at all.
     *
     */
    public static class SetAccessCommand
    {
        public const string Usage = "/setaccess <playername> [level]";
        public const string Description =
            "Set a player's access level. Defaults to GameMaster. Works on offline players.";

        /**
         * TryParse, validates the argument shape
         *
         * The level defaults to GameMaster, which is the case this command exists
         * for. Extra arguments beyond the level are ignored rather than refused,
         * matching how the in game command splits its input.
         *
         */
        public static bool TryParse(string[] args, out SetAccessRequest request, out string error)
        {
            request = null;
            error = null;

            if (args.Length < 1)
            {
                error = "Usage: " + Usage + ". Levels: " + LevelNames();
                return false;
            }

            var level = Player.AccessStatus.GameMaster;

            if (args.Length > 1 && !TryParseLevel(args[1], out level))
            {
                error = "Unknown access level '" + args[1] + "'. Valid: " + LevelNames();
                return false;
            }

            request = new SetAccessRequest { Name = args[0], Level = level };
            return true;
        }

        /**
         * TryParseLevel, matches an access level by name, case insensitively
         *
         * Deliberately not Enum.TryParse: that also accepts numeric strings, so "9"
         * and even undefined values like "42" would parse. Matching the in game
         * /setaccess in Events/SetAccessCommandEvent.cs means names only.
         *
         */
        public static bool TryParseLevel(string text, out Player.AccessStatus level)
        {
            foreach (Player.AccessStatus value in Enum.GetValues<Player.AccessStatus>())
            {
                if (value.ToString().Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    level = value;
                    return true;
                }
            }

            level = Player.AccessStatus.Normal;
            return false;
        }

        private static string LevelNames()
        {
            return string.Join("|", Enum.GetNames<Player.AccessStatus>());
        }
    }
}
```

**Step 4: Run to verify it passes**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter SetAccessCommandTests`

Expected: PASS — `Failed: 0, Passed: 9`.

**Step 5: Commit**

```bash
git add Goose/Console/Commands/SetAccessCommand.cs Goose.Tests/SetAccessCommandTests.cs
git commit -m "feat: add /setaccess console argument validation"
```

---

## Task 3: SetAccessCommand execution

The world-touching half. Not unit tested — `GameWorld`'s constructor takes a `GameServer`, opens the database, and loads maps and scripts, so it cannot be constructed in a test. Covered by the manual verification in Task 7.

**Files:**
- Modify: `Goose/Console/Commands/SetAccessCommand.cs`

**Step 1: Add the logger field**

Add to the top of the `SetAccessCommand` class body, above `Usage`:

```csharp
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
```

**Step 2: Add the Run method**

Add to `SetAccessCommand`, after `TryParse`:

```csharp
        /**
         * Run, resolves the player and applies the access change
         *
         */
        public static void Run(GameWorld world, string[] args)
        {
            if (!TryParse(args, out SetAccessRequest request, out string error))
            {
                Console.WriteLine(error);
                return;
            }

            Player player = world.PlayerHandler.GetPlayerFromData(request.Name);
            if (player == null)
            {
                Console.WriteLine("No player named '" + request.Name + "'.");
                log.Info("Console /setaccess failed: no player named {0}.", request.Name);
                return;
            }

            Player.AccessStatus previous = player.Access;
            player.Access = request.Level;

            // Logged in players are covered by the periodic save. An offline one
            // would otherwise hold the change in memory until something else wrote
            // the row, and a restart before then would silently lose the grant.
            if (player.State == Player.States.NotLoggedIn)
            {
                player.SaveToDatabase(world);
            }

            Console.WriteLine("Set " + player.Name + " from " + previous + " to " + player.Access + ".");
            log.Info("Console /setaccess: {0} {1} -> {2}", player.Name, previous, player.Access);
        }
```

**Step 3: Verify it compiles**

Run: `dotnet build Goose/Goose.csproj`

Expected: 0 errors.

If you see `CS0234` or `CS0117` on `Console.WriteLine`, the namespace is wrong — re-read the namespace warning at the top of this plan.

**Step 4: Commit**

```bash
git add Goose/Console/Commands/SetAccessCommand.cs
git commit -m "feat: add /setaccess console command execution"
```

---

## Task 4: Who, Shutdown, and Help commands

Three small commands. No unit tests — all three are world lookups or one-line state changes.

**Files:**
- Create: `Goose/Console/Commands/WhoCommand.cs`
- Create: `Goose/Console/Commands/ShutdownCommand.cs`
- Create: `Goose/Console/Commands/HelpCommand.cs`

**Step 1: Create WhoCommand**

Create `Goose/Console/Commands/WhoCommand.cs`:

```csharp
using System;

namespace Goose.ConsoleCommands
{
    /**
     * WhoCommand, /who
     *
     * Unlike the in game /who this ignores IsGMInvisible and IsWhoInvisible. The
     * console operator should see everyone who is actually connected.
     *
     */
    public static class WhoCommand
    {
        public const string Usage = "/who";
        public const string Description = "List online players with map, level, and access.";

        public static void Run(GameWorld world, string[] args)
        {
            int matches = 0;

            foreach (Player player in world.PlayerHandler.Players)
            {
                if (player is Pet) continue;
                if (player.State != Player.States.Ready) continue;

                Console.WriteLine("[" + (player.Map?.Name ?? "?") + "] " + player.Name +
                                  " (Level " + player.Level + ", " + player.Access + ")");
                matches++;
            }

            Console.WriteLine(matches + " online.");
        }
    }
}
```

**Step 2: Create ShutdownCommand**

Create `Goose/Console/Commands/ShutdownCommand.cs`:

```csharp
using System;

namespace Goose.ConsoleCommands
{
    /**
     * ShutdownCommand, /shutdown
     *
     * Same mechanism as the in game /shutdown and as GameServer.RequestShutdown:
     * clearing Running exits the game loop, which then saves players and drains the
     * database queue before returning.
     *
     */
    public static class ShutdownCommand
    {
        public const string Usage = "/shutdown";
        public const string Description = "Shut the server down, saving players first.";

        public static void Run(GameWorld world, string[] args)
        {
            Console.WriteLine("Shutting down.");
            world.Running = false;
        }
    }
}
```

**Step 3: Create HelpCommand**

Create `Goose/Console/Commands/HelpCommand.cs`. It renders from the dispatch table rather than a second hardcoded list, so a new command cannot be missing from help:

```csharp
using System;
using System.Collections.Generic;

namespace Goose.ConsoleCommands
{
    /**
     * HelpCommand, /help
     *
     */
    public static class HelpCommand
    {
        public const string Usage = "/help";
        public const string Description = "Show this list.";

        public static void Run(IEnumerable<ConsoleCommand> commands)
        {
            Console.WriteLine("Console commands:");

            foreach (ConsoleCommand command in commands)
            {
                Console.WriteLine("  " + command.Usage.PadRight(34) + command.Description);
            }
        }
    }
}
```

**Step 4: Commit**

These do not compile on their own — `ConsoleCommand` arrives in Task 5. Commit anyway so the tasks stay bite-sized, and verify the build at the end of Task 5.

```bash
git add Goose/Console/Commands/WhoCommand.cs Goose/Console/Commands/ShutdownCommand.cs Goose/Console/Commands/HelpCommand.cs
git commit -m "feat: add /who, /shutdown, /help console commands"
```

---

## Task 5: ConsoleCommandHandler

The reader thread, the queue, and the dispatch table.

**Files:**
- Create: `Goose/Console/ConsoleCommandHandler.cs`

**Step 1: Write the implementation**

Create `Goose/Console/ConsoleCommandHandler.cs`:

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Goose.ConsoleCommands
{
    /**
     * ConsoleCommand, a dispatch table entry
     *
     */
    public sealed class ConsoleCommand
    {
        public Action<GameWorld, string[]> Run;
        public string Usage;
        public string Description;
    }

    /**
     * ConsoleCommandHandler, commands typed at the server console
     *
     * The game loop is single threaded and owns all player state, and
     * Console.ReadLine blocks. So a background thread does nothing but read lines
     * onto a queue, and the game loop drains that queue on its own thread. Nothing
     * here mutates game state off the game thread.
     *
     * There is no privilege model: physical access to the server console is the
     * authorization, which is the point of the feature. The in game /setaccess
     * requires GameMaster, so on a fresh server nobody can grant it.
     *
     */
    public class ConsoleCommandHandler
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly ConcurrentQueue<string> pending = new();
        private readonly Dictionary<string, ConsoleCommand> commands;

        public ConsoleCommandHandler()
        {
            this.commands = new Dictionary<string, ConsoleCommand>
            {
                { "setaccess", new ConsoleCommand {
                    Run = SetAccessCommand.Run,
                    Usage = SetAccessCommand.Usage,
                    Description = SetAccessCommand.Description } },
                { "who", new ConsoleCommand {
                    Run = WhoCommand.Run,
                    Usage = WhoCommand.Usage,
                    Description = WhoCommand.Description } },
                { "shutdown", new ConsoleCommand {
                    Run = ShutdownCommand.Run,
                    Usage = ShutdownCommand.Usage,
                    Description = ShutdownCommand.Description } },
                { "help", new ConsoleCommand {
                    Run = (world, args) => HelpCommand.Run(this.commands.Values),
                    Usage = HelpCommand.Usage,
                    Description = HelpCommand.Description } }
            };
        }

        /**
         * Start, spawns the reader thread
         *
         * Must be called once, from GameServer.Run before its restart loop. Calling
         * it per restart would leave two threads blocked on stdin, splitting typed
         * input between them at random.
         *
         */
        public void Start()
        {
            // Under systemd or Docker stdin is redirected, ReadLine returns null
            // immediately, and the read loop would spin hot forever. Program.cs makes
            // the same check before Console.ReadKey.
            if (Console.IsInputRedirected)
            {
                log.Info("Console commands disabled: stdin is redirected.");
                return;
            }

            var thread = new Thread(this.ReadLoop)
            {
                IsBackground = true,
                Name = "ConsoleCommands"
            };

            thread.Start();

            log.Info("Console commands enabled. Type /help.");
        }

        /**
         * ReadLoop, the reader thread body
         *
         */
        private void ReadLoop()
        {
            try
            {
                while (true)
                {
                    string line = Console.ReadLine();

                    if (line == null) break; // EOF, nothing more is coming

                    if (!string.IsNullOrWhiteSpace(line)) this.pending.Enqueue(line);
                }
            }
            catch (Exception e)
            {
                // Only console input dies here, not the server.
                log.Error(e, "Console reader stopped.");
            }
        }

        /**
         * Update, runs queued commands on the game thread
         *
         * Called once per tick from GameServer.GameLoop.
         *
         */
        public void Update(GameWorld world)
        {
            while (this.pending.TryDequeue(out string line))
            {
                ParsedCommand parsed = ConsoleCommandParser.Parse(line);

                if (parsed == null) continue;

                if (!this.commands.TryGetValue(parsed.Name, out ConsoleCommand command))
                {
                    Console.WriteLine("Unknown command '" + parsed.Name + "'. Type /help.");
                    continue;
                }

                try
                {
                    command.Run(world, parsed.Args);
                }
                catch (Exception e)
                {
                    // A bad command must not take down the tick.
                    log.Error(e, "Error running console command '{0}'.", parsed.Name);
                    Console.WriteLine("Command failed, see log.");
                }
            }
        }
    }
}
```

**Step 2: Verify everything compiles**

Run: `dotnet build Goose/Goose.csproj`

Expected: 0 errors. This is the first point at which Task 4's files compile.

**Step 3: Verify tests still pass**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj`

Expected: `Failed: 0, Passed: 16`.

**Step 4: Commit**

```bash
git add Goose/Console/ConsoleCommandHandler.cs
git commit -m "feat: add console command handler with reader thread and dispatch"
```

---

## Task 6: Wire into GameServer

**Files:**
- Modify: `Goose/GameServer.cs` (field near `:44`, `Run` at `:66`, `GameLoop` at `:230`)

**Step 1: Add the using and the field**

Add to the using block at the top of `Goose/GameServer.cs`:

```csharp
using Goose.ConsoleCommands;
```

Add the field immediately after `private GameWorld gameworld;` (`GameServer.cs:44`):

```csharp
        /**
         * Console commands. Created once and started before the restart loop below,
         * since a second reader thread would compete for stdin.
         */
        private readonly ConsoleCommandHandler consoleCommands = new();
```

**Step 2: Start the reader before the restart loop**

In `Run()` (`GameServer.cs:66`), add the start call as the first statement, **above** `while (true)`:

```csharp
        public void Run()
        {
            this.consoleCommands.Start();

            while (true)
            {
```

This placement is load-bearing. `Run`'s `while(true)` rebuilds the `GameWorld` and re-enters `GameLoop` after a crash; starting the thread inside the loop or inside `Start()` would spawn a new reader on every restart.

**Step 3: Drain each tick**

In `GameLoop()`, immediately after `this.SweepPreLoginConnections();` (`GameServer.cs:230`):

```csharp
                this.SweepPreLoginConnections();

                this.consoleCommands.Update(this.gameworld);
```

**Step 4: Verify the build**

Run: `dotnet build Goose/Goose.csproj`

Expected: 0 errors, and no new warnings beyond the 20 in the baseline.

**Step 5: Commit**

```bash
git add Goose/GameServer.cs
git commit -m "feat: run console commands from the game loop"
```

---

## Task 7: Manual verification

Nothing here is automatable — every check needs a live server with a database. Work through all of it and report actual output; do not claim any step passed without pasting what the console printed.

**Step 1: Start the server**

```bash
cd /home/hayden/code/illutiagooseserver/.worktrees/console-commands
dotnet run --project Goose/Goose.csproj
```

Expected: the log line `Console commands enabled. Type /help.`

**Step 2: `/help`**

Type `/help`. Expected: four commands listed with usage and descriptions.

**Step 3: Unknown command**

Type `/nonsense`. Expected: `Unknown command 'nonsense'. Type /help.`

**Step 4: `/setaccess` on an offline player**

Pick a character name that exists but is not logged in.

Type `/setaccess <name>`. Expected: `Set <name> from Normal to GameMaster.`

Then confirm the change reached disk — the offline path calls `SaveToDatabase` precisely so it does not sit in memory:

```bash
sqlite3 <path-to-db> "SELECT name, access FROM players WHERE name='<name>';"
```

Expected: access is `9` (`GameMaster`).

**Step 5: Bad input**

- `/setaccess` → `Usage: /setaccess <playername> [level]. Levels: Deleted|Banned|...`
- `/setaccess NoSuchPlayer` → `No player named 'NoSuchPlayer'.`
- `/setaccess <name> wizard` → `Unknown access level 'wizard'. Valid: ...`
- `/setaccess <name> 9` → same unknown-level error. Numeric levels are refused on purpose.

**Step 6: `/setaccess` on an online player**

Log a character in, then from the console: `/setaccess <name> gamemaster`.

Expected: the confirmation line, and that character can immediately use an in-game GM command (try `/who all`) **without relogging**. Privileges are read live off `player.Access`.

Known and accepted: nothing is pushed to the client, so any access-dependent client UI will not refresh until relog. The in-game `/setaccess` behaves identically.

**Step 7: `/who`**

Type `/who` with at least one character online. Expected: `[MapName] Name (Level N, Access)` per player, then `N online.` Pets must not appear.

**Step 8: Check the audit log**

Confirm the NLog output contains a `Console /setaccess:` line for each grant and a `Console /setaccess failed:` line for the unknown-name attempt.

**Step 9: `/shutdown`**

Type `/shutdown`. Expected: `Shutting down.`, players saved, process exits cleanly — same behavior as Ctrl+C.

**Step 10: Headless**

```bash
dotnet run --project Goose/Goose.csproj < /dev/null
```

Expected: the log line `Console commands disabled: stdin is redirected.`, no reader thread, no CPU spin, and the server otherwise runs normally. Stop it with Ctrl+C and confirm the clean shutdown path still runs.

**Step 11: Commit any fixes**

If any step needed a fix, commit it with a message describing the actual defect.

---

## Design alignment check

Confirmed against `docs/plans/2026-07-25-server-console-commands-design.md`:

- Four commands, console-only, no in-game exposure — Tasks 2–5.
- `/setaccess` defaults to `GameMaster`, resolves offline players via `GetPlayerFromData`, saves when `NotLoggedIn` — Task 3.
- Level parsed by name only, numerics refused, matching `Events/SetAccessCommandEvent.cs` — Task 2, with tests.
- Audit logging on both success and failure, exact format from the design — Task 3.
- `/who` ignores invisibility flags and skips pets — Task 4.
- `/shutdown` sets `world.Running = false` — Task 4.
- `/help` renders from the dispatch table — Task 4/5.
- Reader thread started once, `IsBackground`, skipped when stdin redirected, breaks on `null` — Task 5/6.
- Drain runs on the game thread beside `SweepPreLoginConnections` — Task 6.
- Per-command `try/catch` in the drain; reader body wrapped — Task 5.
- One file per command under `Goose/Console/`, generic parse shared — Tasks 1–5.
- xUnit project covering `Parse` and `TryParse` only — Tasks 0–2.
- Deferred, not implemented: pushing access changes to connected clients.
