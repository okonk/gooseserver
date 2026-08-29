# Command System — Part 1: Framework Implementation Plan

**Goal:** Build the new command framework (attributes, binder, registry, `CommandEvent`, help system) alongside the still-running legacy dispatch table, so Parts 2–3 can migrate commands one batch at a time.

**Architecture:** Commands become `[Command]`-attributed classes with typed `Execute` methods; a `CommandRegistry` (owned by `GameWorld` as `world.Commands`) holds definitions in an atomically-published immutable snapshot (trie + lookup + ordered list). `EventHandler.AddEvent` checks the command trie first, then the existing packet trie (unchanged). Legacy commands register as "legacy" definitions that run the existing event classes untouched. Help opens a paged `HelpWindow` (Quest frame) with privilege filtering and sections.

**Tech Stack:** C# / .NET 10, xUnit, existing `Trie<T>`, `TestWorldFixture` harness.

**This is Part 1 of 3.** Part 2 migrates General/Party/Guild/Pets commands; Part 3 migrates Customs/GM/Admin + the dimension scripts.

Design doc: `docs/plans/2026-08-29-command-system-design.md`

---

## APIs verified

| API | Location |
|---|---|
| `Trie<T>.Insert(string, T)` / `TryGetValue` / `TryGetLongestPrefix(string, out T, out int)` | `Goose/Trie.cs:28,49,72` |
| `Event` — `Ticks`, `internal bool ClientOriginated`, `Player`, `Data`, `abstract Ready(GameWorld)` | `Goose/Event.cs:7-28` |
| `EventHandler.AddEvent(Player, string)` (dispatch entry, swallow-on-deny + debug log) | `Goose/EventHandler.cs:283` |
| `EventHandler.RegisterEvent(string, CreateEvent)` / `(string, CreateEvent, AccessPrivilege)` | `Goose/EventHandler.cs:248,267` |
| `AccessLevels.HasPrivilege(Player, AccessPrivilege)` | `Goose/AccessLevels.cs:74` |
| `Player.HasPrivilege(AccessPrivilege)` | `Goose/Player.cs:529` |
| `Player.States` (`NotLoggedIn, LoadingGame, LoadingMap, Ready`) | `Goose/Player.cs:61-68` |
| `PlayerHandler.GetPlayer(string name)` — case-insensitive | `Goose/PlayerHandler.cs:133` |
| `GameWorld.Send(Player, string)` | `Goose/GameWorld.cs:589` |
| `GameWorld` ctor creates `new EventHandler()` | `Goose/GameWorld.cs:105` |
| `P.ServerMessage(string)` / `P.MakeWindow(Window)` / `P.WindowTextLine(int, int, string)` / `P.EndWindow(Window)` | `Goose/Packets.cs:9,631,422,642` |
| `Window.ButtonTypes` (Exit, Combine, Close, Back, Next, ShowOk) | `Goose/Window.cs:13-21` |
| `Window.WindowTypes` (1..11, next free value 12) / `WindowFrames.Quest` | `Goose/Window.cs:54-66,24+` |
| `Window.Buttons` — `"showCombine,showClose,showBack,showNext,showOK"` | `Goose/Window.cs` (comment above `Buttons` property) |
| `Window.SendCreate(Player, GameWorld)` — MakeWindow + Populate + EndWindow | `Goose/Window.cs:292` |
| `WindowButtonClickEvent` routes to `window.Clicked(button, npcid, id2, id3, player, world)` | `Goose/Events/WindowButtonClickEvent.cs:47-51` |
| `PlayerInfoWindow` — no-NPC Quest-frame window, `pageNumber`, Back/Next via `SendCreate`, `player.Windows.Add`, `++player.LastWindowID` | `Goose/PlayerInfoWindow.cs` (ctor ~:24, `Clicked` :135-157) |
| `TestWorldFixture.RunCommand(player, packet)` — `AddEvent` + `Update` | `TestSupport/TestWorldFixture.cs:105` |
| `TestWorldFixture.CommandPlayerOn` (Ready-state `CapturingPlayer`), `.Sent`, `RegisterOnlinePlayer` | `TestSupport/TestWorldFixture.cs:75,66,88` |
| `Event.ClientOriginated` is `internal` — new event class must live in the Goose assembly | `Goose/Event.cs:10` |
| Dimension scripts register via `world.EventHandler.RegisterEvent("/dimension ", ...)` in `.csx` (migrated in Part 3, not Part 1) | `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:229-233` |

## Rules that bind every task

- No comments/doc strings on new code except where the "why" is non-obvious (AGENTS.md).
- One commit per task.
- The legacy table must keep working byte-for-byte through this part: no key, privilege, or behavior changes to any existing command.
- Anti-probing policy: permission denial at dispatch → no reply, debug log only.

---

### Task 1: Command attributes, context, and the parameter binder

**Files:**
- Create: `Goose/Commands/CommandAttribute.cs`, `Goose/Commands/SubcommandAttribute.cs`, `Goose/Commands/BaseCommand.cs`, `Goose/Commands/CommandContext.cs`, `Goose/Commands/CommandBinder.cs`
- Test: `Goose.Tests/CommandBinderTests.cs`

**Step 1: Define the metadata types**

```csharp
// Goose/Commands/CommandAttribute.cs
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandAttribute : Attribute
{
    public string Key { get; }
    public AccessPrivilege? Privilege { get; }   // null = open
    public string Section { get; set; } = "General";
    public string Help { get; set; } = null!;

    // Attribute constructor parameters cannot be nullable value types, and
    // `this(key, null)` does not compile against a non-nullable enum — separate bodies:
    public CommandAttribute(string key) { Key = key; }
    public CommandAttribute(string key, AccessPrivilege privilege) { Key = key; Privilege = privilege; }
}

// Goose/Commands/SubcommandAttribute.cs — same shape, no Section
[AttributeUsage(AttributeTargets.Method)]
public sealed class SubcommandAttribute : Attribute
{
    public string Name { get; }
    public AccessPrivilege? Privilege { get; }
    public string Help { get; set; } = null!;
    public SubcommandAttribute(string name) { Name = name; }
    public SubcommandAttribute(string name, AccessPrivilege privilege) { Name = name; Privilege = privilege; }
}
```

- `BaseCommand`: abstract class with `protected virtual AccessPrivilege? CheckAccess(CommandContext ctx, string[] args) => null;` **plus an internal non-virtual forwarder** `internal AccessPrivilege? CheckAccessInternal(CommandContext ctx, string[] args) => CheckAccess(ctx, args);` — `CommandEvent` calls the forwarder (it can't call the protected member); subclasses override the protected one, the surface stays clean.
- `CommandContext`: `Player Player`, `GameWorld World`, `CommandRegistry Registry`, `string[] Args` (tokens after the command key, split on whitespace with empty entries removed, including a subcommand token), `string Remainder` (the raw packet text after the key, lossless), `void Send(string message)` → `World.Send(Player, P.ServerMessage(message))`.
- **Binder extras policy: extra tokens beyond the declared parameters (no `rest`) are ignored** — legacy commands mostly ignored extras (`/kick Bob extra`); erroring on them would change dozens of commands. **Whitespace normalization is a documented delta** (legacy `Substring`/`Split` saw raw text); commands sensitive to raw whitespace take `ctx.Remainder`.
- Key validation helper (used by Task 2): key must start with `/` and contain no spaces except an optional single trailing space.

**Step 2: Write the failing binder tests**

`CommandBinder` contract (test against this):

```csharp
// Bind tokens to a parameter list. Returns null args + error message on failure.
public static (object?[] args, string? error) Bind(
    GameWorld world, Player player, string key,
    ParameterInfo[] parameters, string[] tokens);
// Usage string from key + parameters (also used by help and error replies).
public static string Usage(string key, ParameterInfo[] parameters);
```

Test cases (adversarial ones marked ★):

| Case | Expected |
|---|---|
| `int`, `long`, `float`, `double`, `string` bound positionally | args populated |
| `int` with default, token missing | default used |
| ★ non-optional `int`, token missing | error = `Usage: /cmd <name>` |
| ★ `int` token `"abc"` | error = usage line (no exception escapes) |
| ★ numeric parsing uses invariant culture (`"1.5"` for `double` ok, `"1,5"` fails) | as stated |
| `bool` from `on/off/true/false/1/0` case-insensitive; `"maybe"` → usage error | as stated |
| ★ exact usage strings: `(ctx, int n)` → `Usage: /testcmd <n>`; `(ctx, string command, string name, string[] rest)` → `Usage: /search <command> <name>` (rest omitted); `(ctx, string a, string? b = null)` → `Usage: /cmd <a>` (optional omitted) | exact strings |
| `string[] rest` as final param captures all remaining tokens; none → empty array | as stated |
| ★ `string[] rest` not in final position → rejected at discovery (tested in Task 2) | n/a here |
| `Player` param: token resolves via `PlayerHandler.GetPlayer` (use `RegisterOnlinePlayer`) | bound to that `Player` |
| ★ `Player` param, unknown name | error = `Couldn't find player <name>.` |
| `Player?` with `= null` default, token missing | bound to null |
| ★ extra tokens beyond the parameters, no `rest` | **ignored** (bound args correct, no error) — `/kick Bob extra` parity |
| `Usage`: optional → `[name]`, required → `<name>`, rest → `<name>`; key trailing space trimmed | e.g. `/warp [mapId] [x] [y]` |

Red: tests fail to compile (no `CommandBinder`). Green: implement minimal binder.

**Step 3: Implement `CommandBinder`**

Binding algorithm: iterate `parameters` (skip the leading `CommandContext` — it is injected by the invoker, not the binder); for each, `rest` (`string[]`) consumes all remaining tokens; `Player` resolves the next token (missing → default if present else usage error; unresolvable → the fixed "Couldn't find player" message); numerics parse invariant-culture; `bool` per the set above; missing token with `HasDefaultValue` → default; otherwise usage error. `Usage` renders the key (trailing space trimmed) + one segment per **required** parameter (no default, not a `string[]`); optional parameters and `string[]` tails are omitted — this is also why the internal parameter name `rest` can never leak into a usage line.

**Step 4: Run tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~CommandBinderTests`
Expected: all pass.

**Step 5: Commit**

```bash
git add Goose/Commands Goose.Tests/CommandBinderTests.cs
git commit -m "feat: command attributes, context, and typed parameter binder"
```

---

### Task 2: CommandDefinition and CommandRegistry

**Files:**
- Create: `Goose/Commands/CommandDefinition.cs`, `Goose/Commands/CommandRegistry.cs`
- Modify: `Goose/GameWorld.cs` (add `public CommandRegistry Commands { get; }` property; construct in the ctor next to `this.EventHandler = new EventHandler();` at `Goose/GameWorld.cs:105`)
- Test: `Goose.Tests/CommandRegistryTests.cs`

**Step 1: Define `CommandDefinition`**

Exact model (replaces any earlier sketch — implement this, not a `Key`/`SubcommandNames` variant):

```csharp
internal sealed class CommandDefinition
{
    public string[] Keys { get; }                  // every registered key (trie + ByKey)
    public string PrimaryKey => Keys[0];           // usage strings + help
    public AccessPrivilege? Privilege { get; }     // null = open
    public string? Section { get; }                // null = legacy (excluded from help)
    public string Help { get; }
    // new-style (one of the two invoker groups is non-null):
    public object? Instance { get; }               // BaseCommand instance (attributed commands)
    public Delegate? Handler { get; }              // script-registered delegate
    public MethodInfo? ExecuteMethod { get; }      // null for subcommand-only commands
    public List<SubcommandInfo> Subcommands { get; }
    // legacy:
    public Delegate? LegacyFactory { get; }        // (Player, Object) => Event
    public Type? LegacyType { get; }
}
public sealed record SubcommandInfo(
    string PrimaryName, string[] Names, MethodInfo Method,
    ParameterInfo[] Parameters, string Help, AccessPrivilege? Privilege);
```

Discovery model for new-style commands: at scan time instantiate the class once (`Activator.CreateInstance`), capture the `Execute` `MethodInfo` and any `[Subcommand]` methods. **Valid shapes (exactly one must hold):** (a) exactly one `Execute`, with zero or more subcommands; (b) zero `Execute` and at least one subcommand (subcommand-only, e.g. `/custom` in Part 3). **Rejected:** zero targets, or more than one `Execute`. Test a real subcommand-only class in this task (two `[Subcommand]` methods, no `Execute` → discovered and dispatchable). Invocation per command is reflection `MethodInfo.Invoke` — commands are user-input-rate, not hot path; do not cache delegates per invocation.

**Step 2: Write the failing registry tests**

`CommandRegistry` surface:

```csharp
public void SeedBuiltins();                       // scans typeof(GameWorld).Assembly for [Command]
// two overloads — open commands (scripts) omit the privilege entirely:
public bool Register(string key, string section, string help, Delegate handler);            // privilege = null (open)
public bool Register(string key, AccessPrivilege privilege, string section,
                     string help, Delegate handler);
public bool TryGet(string key, out CommandDefinition definition);   // hits on any alias key
CommandSnapshot Snapshot { get; }                  // single volatile reference: { Trie, ByKey, Ordered }
public IReadOnlyList<CommandSection> Sections { get; }  // derived from Snapshot.Ordered; CommandSection { Name, List<CommandDefinition> }
public static bool IsUsableBy(Player player, CommandDefinition def); // null privilege = true
// internal seams (Goose.Tests has InternalsVisibleTo):
internal void SeedAttributedTypes(IEnumerable<Type> types);  // SeedBuiltins() = SeedAttributedTypes(Goose assembly types)
internal bool RegisterKeys(string[] keys, AccessPrivilege? privilege, string section, string help, Delegate handler);
internal IReadOnlyList<string> FindNameCollisions();         // section-name vs command-name collisions
```

Tests (★ adversarial):

| Case | Expected |
|---|---|
| Discovery seam: `SeedAttributedTypes(IEnumerable<Type>)` is the test entry point (the test assembly is never scanned by `SeedBuiltins`). Here: seeding an empty list finds nothing and doesn't throw; seeding a **subcommand-only** test class (two `[Subcommand]` methods, no `Execute`) → discovered and dispatchable; a class with two `Execute` methods → rejected + logged; a class with neither → rejected | as stated |
| `Register` valid key → `TryGet` hit, `Trie` contains it | true |
| ★ `Register` key without `/` prefix, or with an internal space (`"/bad key"`) | returns false, logged, `TryGet` miss |
| ★ `Register` replacing a restricted key with an open one | returns false (downgrade refused), original definition intact |
| `Register` replacing a restricted key with a different restricted privilege | replaces — **privileges are capabilities, not an ordered hierarchy** (Ban is not "more restrictive" than Warp); the only refused direction is restricted → open |
| ★ `RegisterKeys(["/a ", "/b "], ...)` where one key conflicts with a different definition | returns false, **nothing registered** (atomic: the non-conflicting key is not half-registered either), original definitions intact |
| `Register` same key again with new help/handler | replaces in place — `Ordered`/`Sections` show the definition once, no duplicate |
| ★ `Register` handler whose `string[] rest` is not the final parameter | returns false |
| `Sections` groups by `Section`, preserves registration order | as stated |
| ★ concurrent `Register` from 8 threads while another thread reads **one captured `Snapshot`** (all reads through that single reference stay mutually consistent; the final published snapshot has every key resolvable) | as stated |
| ★ `FindNameCollisions` — section exists, then a command registered whose trimmed primary key matches the section name; and the reverse order (command first, section introduced later) → the collision is reported in **both** orders, and each publish logs a warning per collision | both orders |
| `IsUsableBy`: null privilege → true; `Access` Normal vs `AccessPrivilege.Ban` → false/true for GM | as stated |

Red: compile failure. Green: implement.

**Step 3: Implement `CommandRegistry`**

- Storage is an **immutable snapshot** published through a single `volatile` reference: `CommandSnapshot { Trie<CommandDefinition> Trie (every alias key → def), IReadOnlyDictionary<string, CommandDefinition> ByKey (every alias key), IReadOnlyList<CommandDefinition> Ordered }`. **Immutability is by construction discipline**: each mutation builds brand-new `Trie`/`Dictionary`/`List` instances and publishes them; a published instance is never mutated afterwards (the trie is the existing mutable `Trie<T>` class — the rule is simply that a published trie is never touched again, every mutation builds a new one). `CommandDefinition` carries `string[] Keys` + `PrimaryKey` (first key — usage strings and help). Readers (dispatch, help) take one snapshot reference and can never observe a half-updated state; help and dispatch always see the same version. (Alias-ready now so Part 2's multi-key attributes need no storage rework; Part 1 registrations simply have `Keys.Length == 1`.)
- Mutation protocol (one lock, used by `SeedBuiltins` and `Register`): **1.** validate the complete definition first — every key's format, handler shape (delegate, first param `CommandContext`, `rest` final), no key conflict with a *different* definition, no restricted → open downgrade on any key; any failure → return false, nothing mutated. **2.** build the new ordered list (replace the existing definition in place if any key was already registered to it, else append), new `ByKey`, new trie. **3.** publish the new snapshot. Rebuild is O(total keys) and only happens on registration (startup + script reloads).
- `SeedBuiltins`: `typeof(GameWorld).Assembly.GetTypes()`, filter `[Command]` + assignable to `BaseCommand`, validate (key format, valid shape per Task 1, `rest` final, duplicate keys → log error and skip), instantiate, capture methods, insert.
- `Sections`: derived from the snapshot's ordered list (cheap; help is user-input-rate). **Legacy definitions (`RegisterLegacy`) have `Section == null` and are excluded from help/`Sections`** until Parts 2–3 migrate them with real metadata.

**Step 4: Wire `GameWorld.Commands`**

Add the property and construct in the ctor. `SeedBuiltins()` is called from `EventHandler`'s ctor (Task 3) so the existing single construction point still drives startup.

**Step 5: Run tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~CommandRegistryTests`
Expected: all pass. Then `dotnet test Goose.Tests` — full suite still green.

**Step 6: Commit**

```bash
git add Goose/Commands Goose/GameWorld.cs Goose.Tests/CommandRegistryTests.cs
git commit -m "feat: command registry with discovery, script registration, and permission policy"
```

---

### Task 3: CommandEvent and EventHandler dispatch wiring

**Files:**
- Create: `Goose/Commands/CommandEvent.cs`
- Modify: `Goose/EventHandler.cs` (dispatch in `AddEvent(Player, string)` :283; `_SeedCommands` :120 becomes legacy registrations; `RegisterEvent` overloads :248/:267)
- Test: `Goose.Tests/CommandDispatchTests.cs`; update `Goose.Tests/EventHandlerTests.cs`

**Step 1: Write the failing dispatch tests**

Use `TestWorldFixture` (`RunCommand`, `CommandPlayerOn`, `CapturingPlayer.Sent`). Register test commands via `world.Commands.Register` (they live in the test assembly, so use `Register`, not discovery):

| Case | Expected |
|---|---|
| Registered open command runs: `RunCommand(player, "/testcmd 5")` → handler observed (e.g. `ctx.Send` captured) | `Sent` contains the reply |
| ★ Registered restricted command, Normal player: `RunCommand` returns true, `Sent` empty, no exception | swallowed |
| Same, GM player (`Access = GameMaster`) | runs |
| ★ Legacy command unchanged: `RunCommand(player, "/who")` behaves as before (no crash, same replies as pre-change) | baseline |
| ★ Unknown `/nope` → `RunCommand` false (no match), `Sent` empty | as stated |
| Non-command packet still dispatches: `";"` chat path and e.g. `"PONG"` still match the packet trie | as stated |
| Parse error: command with required `int`, `RunCommand(player, "/testcmd")` | `Sent` contains `Usage: /testcmd <n>` |
| Player not `Ready` (`State = LoadingMap`): registered command is a no-op | `Sent` empty |
| `CheckAccess` override: token `"invis"` + Normal player → swallowed; GM → runs | as stated |
| Subcommand command: bare key → sends subcommand list; unknown sub → same; valid sub → runs with remaining tokens bound | as stated |
| ★ Subcommand privilege: sub requires `AccessPrivilege.Ban`; Normal → swallowed even though command itself is open | as stated |
| `EventHandler.RegisterEvent("/evil", factory)` (slash key) | **Part 1: warning logged, still registered** (shipped `.csx` scripts depend on it until Part 3); hard rejection lands in Part 3 |
| `EventHandler.RegisterEvent("GID", factory)` (non-slash) | still works (Aspereta dependency) |
| Existing `RegisterEvent_DoesNotReplaceRestrictedCommandWithOpenFactory` semantics now live in the registry: re-test via `world.Commands.Register` replacing `/shutdown` open | refused |
| (different-length alias binding — the regression test for the matched-length cut point — is in **Part 2 Task 3**, where the multi-key API lands: `/invite Bob` vs `/groupadd Bob` must both bind `name == "Bob"` exactly) | — |

Red: new behaviors fail (commands not dispatched). Green: implement.

**Step 2: Implement `CommandEvent`**

`CommandEvent : Event` carrying `CommandDefinition Definition`, `string Packet`, `int MatchedLength` (from the trie match — the **sole** source for the cut point; never recompute it from key lengths). `Ready(GameWorld)`:

1. `Player is not { State: Player.States.Ready }` → debug log, return (framework-level replacement of the per-command state checks).
2. `string matchedKey = Packet[..matchedLength]` where `matchedLength` comes from `TryGetLongestPrefix` (passed in by `AddEvent`) — **never `def.Key.Length` or `PrimaryKey.Length`**: alias keys can have different lengths (`/invite ` vs `/groupadd `), and the matched key is the only correct cut point. `Remainder = Packet.Substring(matchedKey.Length)`; `Args = Remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries)`.
3. If `def` has subcommands: first token selects (case-insensitive); none/unknown → `ctx.Send` the subcommand list (each: `name` + usage + help) and return.
4. `CheckAccess` (via the command instance) → privilege → `!AccessLevels.HasPrivilege` → debug log, return (swallowed).
5. Subcommand privilege → same swallow.
6. `CommandBinder.Bind` on the selected target's parameters → error → `ctx.Send(error)`, return.
7. Invoke: `Execute`/subcommand method via the captured `MethodInfo` with `[ctx, ...args]`; exceptions propagate to the existing per-event catch in `EventHandler.Update` (`Goose/EventHandler.cs` Update loop) so logging behavior is unchanged.

**Step 3: Rewire `EventHandler`**

- Ctor: after `_SeedCommands()`, call `world-less` `SeedBuiltins` — the registry is passed in or the ctor takes it; simplest: `GameWorld` ctor creates `Commands` **before** `EventHandler` and passes it: `this.EventHandler = new EventHandler(this.Commands);` (update `Goose/GameWorld.cs:105`).
- `AddEvent(Player, string)`: first `registry.Snapshot.Trie.TryGetLongestPrefix(packet, out def, out len)`. If hit: legacy definition → existing code path (factory/type instantiation, `ClientOriginated = true`, enqueue). New-style: class-level privilege check with the existing swallow + debug log (`Goose/EventHandler.cs:283` block), then enqueue `CommandEvent` **carrying `len`** (the matched length, per Task 3 step 2). If miss: fall through to the packet trie exactly as today.
- `_SeedCommands`: convert the table to registrations — non-command packet entries unchanged in the packet trie; every `/` entry becomes `registry.RegisterLegacy(key, typeof(XCommandEvent), privilege)` (same keys, same trailing spaces, same privileges — copy verbatim). `RegisterLegacy` takes no section/help — legacy commands stay out of help until migrated (Task 2 storage note).
- Rename the nested `EventHandler.CommandDefinition` to `PacketDefinition` (it now describes non-command packets only; avoids colliding with the new top-level `Goose.Commands.CommandDefinition`). The `Open`/`Restricted` helpers become `PacketDefinition.Open/Restricted` for the packet table.
- `RegisterEvent`: **unchanged signatures in this part** — both overloads keep working for all keys, with a warning logged for `/` keys (shipped `.csx` scripts depend on this until Part 3; see the ⚠ ordering constraint below). Hard rejection + overload deletion land in Part 3 Task 5.

**Mutation impact (dispatch path):**
- Source of truth: command definitions move from the private `_SeedCommands` table into `CommandRegistry`; the packet trie keeps the non-command entries.
- Readers: `EventHandler.AddEvent` (only reader of command definitions); `TestWorldFixture.RunCommand` and all integration tests exercise it.
- Propagation: `GameWorld` ctor order `Commands` → `EventHandler(Commands)`; `SeedBuiltins` inside `EventHandler` ctor.
- Invariants: every legacy key dispatches to the same event type with the same privilege; denial stays swallowed; `AddEvent` return values unchanged (true on match-or-refused, false on no match).
- Proof: the legacy `/who` baseline test, the `/shutdown` refusal test, and the full existing test suite (395 tests) staying green.

**Step 4: Run tests**

Run: `dotnet test Goose.Tests` then `dotnet test Goose.IntegrationTests`
Expected: all green (dimension integration tests still pass — the `.csx` scripts still use `RegisterEvent` with `/` keys in this part!).

⚠ **Ordering constraint:** until Part 3 migrates the dimension scripts, `RegisterEvent` must still **accept** `/` keys for the shipped `.csx` scripts, or `DimensionCommandGateTests`/`DimensionCurrencyCommandTests` break. Therefore in this part: `RegisterEvent(string, CreateEvent)` keeps working for all keys but logs a warning for `/` keys; the hard rejection lands in Part 3 together with the script migration. Adjust the test table above: the `/evil` case asserts *warning logged, still registered* in Part 1.

**Step 5: Commit**

```bash
git add Goose/Commands/CommandEvent.cs Goose/EventHandler.cs Goose/GameWorld.cs Goose.Tests
git commit -m "feat: dispatch commands through the registry; legacy commands register as legacy definitions"
```

---

### Task 4: HelpFormatter, HelpWindow, and the /help command

**Files:**
- Create: `Goose/Commands/HelpFormatter.cs`, `Goose/Commands/HelpWindow.cs`, `Goose/Commands/HelpCommand.cs`
- Modify: `Goose/Window.cs` (add `Help = 12` to `WindowTypes`, `Goose/Window.cs:54-66`)
- Test: `Goose.Tests/HelpTests.cs`

**Step 1: Write the failing help tests**

`HelpFormatter` (static):

```csharp
public const int MaxLineLength = 42;
public static List<string> Wrap(string line);                    // word wrap, hard-break overlong words
public static List<List<string>> BuildPages(Player player, CommandRegistry registry,
                                            string? name);       // pages for the window
```

Tests (★ adversarial):

| Case | Expected |
|---|---|
| `Wrap`: line ≤ 42 → unchanged; line > 42 breaks at the last word boundary ≤ 42; single word > 42 hard-breaks at 42 | exact lines |
| ★ Normal player pages: sections with only restricted commands absent; GM sees them | section lists differ |
| Section page lines: usage + `" - " + help`, wrapped, continuation indented 2 spaces | exact format |
| ★ `name` = command the player lacks privilege for → `BuildPages` returns null (caller sends nothing) | null |
| `name` unknown → null | null |
| ★ `name` matching both a command and a section (register a test section named after a command) → command detail page first, then the section's commands | both present, order |
| Command page: help text, usage line, subcommands with usage+help | exact format |
| ★ open command with one open + one restricted subcommand: Normal's command page lists only the open subcommand; GM's lists both | subcommand filtering |
| ★ section whose wrapped output exceeds `MaxLinesPerPage` (register enough test commands) → split across ≥ 3 pages, no line lost or duplicated | pagination |
| `Sections` order in page 1 = registration order; each line `Name (count)` | exact format |

`HelpWindow` tests (use `CommandPlayerOn`, build pages directly):

| Case | Expected |
|---|---|
| Open: `player.Windows` contains it; `Sent` contains `P.MakeWindow` + text lines + `P.EndWindow` | as stated |
| `Buttons`: page 0 → back hidden; middle page → both; last page → next hidden | `"0,1,0,1,0"` / `"0,1,1,1,0"` / `"0,1,1,0,0"` |
| `Clicked(Next)` / `Clicked(Back)` re-send with adjacent page; `Clicked(Close)` removes from `player.Windows` | as stated |
| ★ `Clicked(Next)` on last page / `Clicked(Back)` on page 0 → clamped, no crash | stays on page |

Red: compile failure. Green: implement.

**Step 2: Implement**

- `HelpWindow : Window` — model on `Goose/PlayerInfoWindow.cs`: ctor sets `ID = ++player.LastWindowID`, `Frame = WindowFrames.Quest`, `Type = WindowTypes.Help`, stores `List<List<string>> pages` + `pageNumber`; `Populate` sends each line via `P.WindowTextLine(this.ID, i, line)` (`Goose/Packets.cs:422`); `Clicked` per the PlayerInfoWindow pattern (`Goose/PlayerInfoWindow.cs:135-157`) with clamping; static `Open(GameWorld, Player, List<List<string>> pages)`.
- `HelpFormatter.BuildPages`:
  - `name` null → `[SectionListPage, SectionPage(s) for each visible section]`.
  - command (case-insensitive key match, trailing space trimmed on the input) → visibility check (null → no reply); else `[CommandPage (+ section commands appended if a same-named section exists)]`.
  - section (case-insensitive) → `[SectionPage]` (only visible commands).
  - Visibility: `CommandRegistry.IsUsableBy(player, def)`. **Subcommands are filtered too**: a subcommand line is shown only if the command is usable *and* the subcommand's own privilege passes — an open command with a restricted subcommand must not reveal the subcommand's name/usage to unprivileged players.
  - **Pagination height**: `public const int MaxLinesPerPage = 32;` — **every** page type (section list, section pages, command-detail pages) passes through the same height splitter at this limit. 32 is the demonstrated floor: `PlayerInfoWindow` sends 32 text lines in its base case (34 window calls − `MakeWindow` − `EndWindow`, and *more* when bank pages loop in) and renders correctly in production. Still worth one in-game glance at a full GM page; if the client truncates, lower the constant — it is a single constant.
  - **Legacy definitions are skipped** (`Section == null`) — in Part 1, `/help` shows only `/help` itself plus any script-registered commands; sections grow as Parts 2–3 migrate.
- `HelpCommand : BaseCommand`:

```csharp
[Command("/help", Section = "General", Help = "Show command help.")]
public sealed class HelpCommand : BaseCommand
{
    public void Execute(CommandContext ctx, string? name = null)
    {
        var pages = HelpFormatter.BuildPages(ctx.Player, /* registry via ctx? see below */, name);
        if (pages is not null) HelpWindow.Open(ctx.World, ctx.Player, pages);
    }
}
```

Registry access: `CommandContext.Registry` (set by `CommandEvent`; declared in Task 1).

- `WindowTypes.Help = 12`: `Window.Create`'s type→frame switch (`Goose/Window.cs:99-102`) has no `Help` case, so `_ => this.Frame` keeps the Quest frame — verify no other `WindowTypes` switch needs a case (search `WindowTypes.` in `Goose/`; `Window.Refresh` and `Clicked` defaults are safe).

**Step 3: Run tests**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~HelpTests` then the full `Goose.Tests` suite.
Expected: green. Note: `SeedBuiltins` now discovers `HelpCommand` — the Task 3 legacy-baseline tests remain valid.

**Step 4: Commit**

```bash
git add Goose/Commands Goose/Window.cs Goose.Tests/HelpTests.cs
git commit -m "feat: paged help window with sections, privilege filtering, and word wrap"
```

---

### Task 5: Framework integration tests

**Files:**
- Create: `Goose.IntegrationTests/CommandFrameworkTests.cs`

**Step 1: Write the integration tests**

Use `TestWorldFixture` (linked into `Goose.IntegrationTests`, verified in its csproj). These run the real dispatch → queue → `Ready` path end-to-end:

| Case | Expected |
|---|---|
| `RunCommand(player, "/help")` → `Sent` contains a `P.MakeWindow`-shaped packet; window added to `player.Windows` | window opened |
| Privilege filtering end-to-end: `world.Commands.Register("/itestgm ", AccessPrivilege.Ban, "Admin", "Test.", (CommandContext ctx) => ctx.Send("gm ok"))`; Normal's `/help` pages omit the `Admin` section, GM's include it (assert on `Sent` text lines). Legacy commands are not in help yet in Part 1 — do not assert on them | filtering holds end-to-end |
| `world.Commands.Register("/itestcmd ", "General", "Test.", (CommandContext ctx, int n) => ctx.Send("got " + n))` (open overload — no privilege argument) then `RunCommand(player, "/itestcmd 7")` → `Sent` contains `got 7` | script-style registration works through the queue |
| Re-register the same key with a new handler → next run uses the new handler (reload semantics) | replaced |
| ★ Registration from a background `Task` while the game thread (test thread) runs `RunCommand` concurrently for 100 iterations → no exceptions, command resolvable after the join | thread-safe in the real path |
| Legacy regression sample: `/who` (no args) and a restricted legacy command refused for Normal (`/ban x` → no reply) behave as before | baseline intact |

**Step 2: Run**

Run: `dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CommandFrameworkTests` then the full `Goose.IntegrationTests` and `Goose.Tests` suites.
Expected: all green.

**Step 3: Commit**

```bash
git add Goose.IntegrationTests/CommandFrameworkTests.cs
git commit -m "test: end-to-end coverage for the command framework"
```

---

## Invariant-to-test matrix (Part 1)

| Invariant | Proved by |
|---|---|
| Parse failure never throws; replies with usage | `CommandBinderTests` (bad token, missing token) + dispatch test |
| `Player` param unresolvable → fixed message, no crash | `CommandBinderTests` + dispatch test |
| Privilege denial swallowed at dispatch (anti-probing) | `CommandDispatchTests` (restricted, Normal player: `Sent` empty, returns true) |
| Subcommand privilege checked, swallowed | `CommandDispatchTests` ★ |
| `CheckAccess` per-argument gating | `CommandDispatchTests` |
| Downgrade of a restricted key refused | `CommandRegistryTests` ★ + reworked `EventHandlerTests` case |
| `/` keys via `RegisterEvent` warned (Part 1) / rejected (Part 3) | `CommandDispatchTests` (Part 1: warning + registered) |
| Replace-on-reregister (script reload semantics) | `CommandRegistryTests` + `CommandFrameworkTests` |
| Registration thread-safe vs dispatch | `CommandRegistryTests` ★ + `CommandFrameworkTests` ★ |
| Help never reveals commands the player lacks | `HelpTests` ★ + `CommandFrameworkTests` |
| Command+section name collision shows both | `HelpTests` ★ |
| Help lines ≤ 42 chars | `HelpTests` wrap cases |
| Legacy commands byte-for-byte unchanged | `CommandDispatchTests` baseline + full existing suite (395) |
| Non-`Ready` player: command no-op | `CommandDispatchTests` |

## Part 1 exit criteria

- `dotnet test` green across `Goose.Tests` and `Goose.IntegrationTests` (including all pre-existing dimension tests — the `.csx` scripts still run on `RegisterEvent`).
- `/help` works in-game; all 72 legacy command keys dispatch exactly as before.
- No file in `Goose/Events/*CommandEvent.cs` modified yet (migration is Parts 2–3).
