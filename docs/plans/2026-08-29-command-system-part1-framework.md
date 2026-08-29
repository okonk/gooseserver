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
    public string? Usage { get; set; }   // verbatim usage override (text after the `Usage: ` prefix)

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
    public string? Usage { get; set; }   // verbatim usage override (text after the `Usage: ` prefix)
    public SubcommandAttribute(string name) { Name = name; }
    public SubcommandAttribute(string name, AccessPrivilege privilege) { Name = name; Privilege = privilege; }
}
```

- `BaseCommand`: **`public abstract class BaseCommand`** (public because `HelpCommand` and the command classes planned in Parts 2–3 derive from it) with `protected virtual AccessPrivilege? CheckAccess(CommandContext ctx, string[] args) => null;` **plus an internal non-virtual forwarder** `internal AccessPrivilege? CheckAccessInternal(CommandContext ctx, string[] args) => CheckAccess(ctx, args);` — `CommandEvent` calls the forwarder (it can't call the protected member); subclasses override the protected one, the surface stays clean.
- `CommandContext`: **`public sealed class CommandContext`** (public because separately compiled `.csx` script handlers name it in their signatures). Members: `Player Player`, `GameWorld World`, `CommandRegistry Registry`, `string[] Args` (tokens after the command key, split on `' '` — the protocol's single-space delimiter — with empty entries removed; **includes the subcommand token**: the binder receives only the tokens after it, per the invoker's step 6, while `CheckAccess` and handlers see the full array), `string Remainder` (the raw packet text after the key, lossless), `string Usage` (the selected target's precomputed usage line, override applied — for parse-error replies and in-body empty-input guards), `void Send(string message)` → `World.Send(Player, P.ServerMessage(message))`.
- **Binder extras policy: extra tokens beyond the declared parameters (no `rest`) are ignored** — legacy commands mostly ignored extras (`/kick Bob extra`); erroring on them would change dozens of commands. **Whitespace normalization is a documented delta** (legacy `Substring`/`Split` saw raw text); commands sensitive to raw whitespace take `ctx.Remainder`.
- Key validation helper (used by Task 2): key must start with `/` and contain no spaces except an optional single trailing space.

**Step 2: Write the failing binder tests**

`CommandBinder` contract (test against this):

```csharp
// Bind tokens to a parameter list. Returns null args + error message on failure.
// usage = the selected target's precomputed usage line (override already applied, `Usage: ` prefix included).
// Missing token / conversion failure → error = usage. Unknown Player → the specific
// "Couldn't find player <name>." error (not the usage line).
// No key parameter — the complete precomputed usage string is already supplied.
public static (object?[] args, string? error) Bind(
    GameWorld world, Player player,
    ParameterInfo[] parameters, string[] tokens, string usage);
// Usage string from key + parameters (also used by help and error replies).
// usageOverride (from CommandDefinition.UsageOverride / SubcommandInfo.UsageOverride) wins verbatim.
public static string Usage(string key, ParameterInfo[] parameters, string? usageOverride = null);
```

Test cases (adversarial ones marked ★):

| Case | Expected |
|---|---|
| `int`, `long`, `float`, `double`, `decimal`, `string` bound positionally | args populated |
| `int` with default, token missing | default used |
| ★ non-optional `int`, token missing | error = `Usage: /cmd <name>` |
| ★ `int` token `"abc"` | error = usage line (no exception escapes) |
| ★ numeric parsing uses invariant culture (`"1.5"` ok, `"1,5"` fails — for `double` and `decimal`) | as stated |
| `bool` from `on/off/true/false/1/0` case-insensitive; `"maybe"` → usage error | as stated |
| ★ exact usage strings: `(ctx, int n)` → `Usage: /testcmd <n>`; **subcommand usage keys are composed**: no-arg `[Subcommand("help")]` on `/custom` (registered without a trailing space) → `Usage: /custom help`, no-arg `[Subcommand("kill")]` → `Usage: /custom kill` (never bare `Usage: /custom`); alias-primary: a no-arg subcommand reached via its alias still prints the **primary** name (`Usage: /custom make`, not `/custom create`); `(ctx, string command, string name, string[] query)` → `Usage: /search <command> <name> [query...]`; `(ctx, string required, string[] message)` → `Usage: /cmd <required> [message...]`; `(ctx, string a, string? b = null)` → `Usage: /cmd <a> [b]`; `(ctx, int? mapId = null, int? mapx = null, int? mapy = null)` → `Usage: /warp [mapId] [mapx] [mapy]`; with override `Usage = "/custom make <r> <g> <b> <a> <name...>"` → `Usage: /custom make <r> <g> <b> <a> <name...>` regardless of the algorithm | exact strings |
| `string[]` tail as final param captures all remaining tokens; none → empty array | as stated |
| ★ a `string[]` parameter not in final position → rejected at discovery (tested in Task 2) | n/a here |
| `Player` param: token resolves via `PlayerHandler.GetPlayer` (use `RegisterOnlinePlayer`) | bound to that `Player` |
| ★ `Player` param, unknown name | error = `Couldn't find player <name>.` |
| `Player?` with `= null` default, token missing | bound to null |
| ★ `int?` / `decimal?` (nullable of a supported value type): token missing → bound to `null` (the default); valid token ("5" / "1.5") → parsed underlying value; invalid token ("abc") → usage error, no exception | as stated |
| ★ extra tokens beyond the parameters, no `rest` | **ignored** (bound args correct, no error) — `/kick Bob extra` parity |
| `Usage`: **exact algorithm** — required scalar → `<name>`; defaulted scalar → `[name]`; `string[]` tail → always `[name...]` (optional by default — a tail's requiredness is a property of the command, not inferable from parameter types, so it is never inferred; commands where the tail is effectively required carry a `Usage` override); key trailing space trimmed; an explicit `Usage` override (below) wins over the algorithm entirely | `/warp [mapId] [mapx] [mapy]` |

Red: tests fail to compile (no `CommandBinder`). Green: implement minimal binder.

**Step 3: Implement `CommandBinder`**

Binding algorithm: iterate `parameters` (skip the leading `CommandContext` — it is injected by the invoker, not the binder); for each, `rest` (`string[]`) consumes all remaining tokens; `Player` resolves the next token (missing → default if present else usage error; unresolvable → the fixed "Couldn't find player" message); numerics parse invariant-culture; **`Nullable<T>` is supported whenever `T` is a supported value type** — parse via `Nullable.GetUnderlyingType(parameterType)`; a missing optional token produces the default (typically `null`), a present-but-invalid token is a usage error like any other numeric; `bool` per the set above; missing token with `HasDefaultValue` → default; otherwise usage error.

**Usage generation — the one algorithm (this text replaces every earlier variant):**
1. If the definition/subcommand carries an explicit `Usage` override, use it verbatim after `Usage: ` and stop. Override: `public string? Usage { get; set; }` on both `CommandAttribute` and `SubcommandAttribute` (class-level applies to the bare command; subcommand-level to that subcommand) — for exceptional legacy commands whose real syntax the algorithm can't express. **The framework prefixes `Usage: ` on every usage line it emits** (parse-error replies, in-body guard replies, help detail lines) — legacy `/hairdye` and `/custom` usage lines had no prefix; that is an **intended formatting delta**, test-pinned in Parts 2–3.
2. Otherwise render the **usage key** + one segment per parameter, in order. **Usage key:** bare command → `definition.PrimaryKey` (trailing space trimmed); subcommand → the **composed** `$"{definition.PrimaryKey.TrimEnd()} {subcommand.PrimaryName}"` — the primary name always, even when the subcommand was invoked via an alias (e.g. `/custom create` still prints `/custom make …`). The usage key is used everywhere a usage line is generated: the dispatch precompute (invoker step 6), binder errors, the bare/unknown-subcommand list (step 4), and `HelpFormatter`.
   - required scalar (no default, not `string[]`) → `<name>`
   - defaulted scalar → `[name]`
   - `string[]` tail → always `[name...]` (optional by default). A tail's requiredness is a property of the command, not inferable from parameter types, so it is never inferred — commands where the tail is effectively required carry a `Usage` override (e.g. `/broadcast`, `/tell`, `/custom make`, `/setconfig`).
3. Tail parameter names must be meaningful (`message`, `query`, `name` — not `rest`); the algorithm renders whatever the parameter is called, so the name shows up in usage.
4. **Carrying the override:** `CommandDefinition.UsageOverride` and `SubcommandInfo.UsageOverride` (set from the attribute's `Usage` property at discovery; `null` for script-registered commands, which get the algorithm). `CommandEvent` precomputes the selected target's usage line (`CommandBinder.Usage(usageKey, target.Parameters, target.UsageOverride)` — `usageKey` per step 2 above), stores it in `CommandContext.Usage`, **and passes it to `Bind`** — so the parse-error reply, in-body guards (`ctx.Send(ctx.Usage)`), and the binder's own error string are one and the same precomputed line; `HelpFormatter` uses the same binder call for help lines.

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
    public string? UsageOverride { get; }          // verbatim usage line (no `Usage: ` prefix)
    // new-style (one of the two invoker groups is non-null):
    public object? Instance { get; }               // BaseCommand instance (attributed commands)
    public Delegate? Handler { get; }              // script-registered delegate
    public MethodInfo? ExecuteMethod { get; }      // null for subcommand-only commands
    public List<SubcommandInfo> Subcommands { get; }
    // legacy:
    public Delegate? LegacyFactory { get; }        // (Player, Object) => Event
    public Type? LegacyType { get; }
}
internal sealed record SubcommandInfo(
    string PrimaryName, string[] Names, MethodInfo Method,
    ParameterInfo[] Parameters, string Help, AccessPrivilege? Privilege, string? UsageOverride);
```

Discovery model for new-style commands: at scan time instantiate the class once (`Activator.CreateInstance`), capture the `Execute` `MethodInfo` and any `[Subcommand]` methods. **Valid shapes (exactly one must hold):** (a) exactly one `Execute`, with zero or more subcommands; (b) zero `Execute` and at least one subcommand (subcommand-only, e.g. `/custom` in Part 3). **Rejected:** zero targets, or more than one `Execute`. **Signature validation (every target — attributed `Execute`/`[Subcommand]` methods and script-registered delegates — is checked at registration, never left to fail at bind/invoke time):** first parameter is exactly `CommandContext`; every parameter type is in the binder's supported set (the same set the binder test table pins); `string[]` may appear only as the final parameter. Attributed violations: fail-fast exception in `SeedBuiltins` (built-ins must be correct at startup); script `Register` violations: return `false`. Tests cover each rejection case. Test a real subcommand-only class in this task (two `[Subcommand]` methods, no `Execute` → discovered and dispatchable). Invocation per command is reflection `MethodInfo.Invoke` — commands are user-input-rate, not hot path; do not cache delegates per invocation.

**Step 2: Write the failing registry tests**

`CommandRegistry` surface:

```csharp
public sealed class CommandRegistry   // public — GameWorld.Commands exposes it
{
public void SeedBuiltins();                       // scans typeof(GameWorld).Assembly for [Command]
// two overloads — open commands (scripts) omit the privilege entirely:
public bool Register(string key, string section, string help, Delegate handler);            // privilege = null (open)
public bool Register(string key, AccessPrivilege privilege, string section,
                     string help, Delegate handler);
internal bool TryGet(string key, out CommandDefinition definition);   // hits on any alias key
internal CommandSnapshot Snapshot { get; }            // single volatile reference: { Trie, ByKey, Ordered }
internal IReadOnlyList<CommandSection> Sections { get; }  // derived from Snapshot.Ordered; CommandSection { Name, List<CommandDefinition> }
internal static bool IsUsableBy(Player player, CommandDefinition def); // null privilege = true
}
```

**Accessibility (must compile — no inconsistent accessibility):** the exact split — **public:** `CommandRegistry` (with its `Register` overloads and `SeedBuiltins`), `BaseCommand`, `CommandContext`, `CommandAttribute`, `SubcommandAttribute`; **internal:** `CommandDefinition`, `SubcommandInfo`, `CommandSnapshot`, `CommandSection`, `CommandBinder`, `HelpFormatter`, `CommandEvent` (an `Event` subclass queued internally; scripts never name it). Public signatures use public types only (`string`, `AccessPrivilege`, `Delegate`, `CommandContext`). `HelpWindow` (Goose assembly) and all tests (friend assembly via `InternalsVisibleTo`) use the internal surface.
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
| ★ `RegisterKeys(["/a ", "/b "], ...)` where the two keys belong to **two different** existing definitions | returns false, both originals intact (cross-definition conflict) |
| ★ multi-key replacement frees all old keys: register def A under `/invite ` + `/groupadd `, then `RegisterKeys(["/groupadd "], ...)` with def B → def B owns `/groupadd ` **only**; `/invite ` is gone from the trie and `ByKey` | old aliases disappear unless re-registered |
| ★ new multi-key registration with one occupied + one new key: A owns `/invite `; `RegisterKeys(["/invite ", "/groupadd "], ...)` with B → B owns **both** keys, A removed | alias set grows on replacement |
| ★ downgrade protection on multi-key replacement: A **restricted** under `/a `; `RegisterKeys(["/a "], ...)` **open** → false, A unchanged; open → restricted → true; restricted → different-restricted → true | capabilities rule — only restricted → open is refused |
| ★ ordering: replacing a definition keeps its position in `Ordered` (first occurrence); a brand-new definition appends | help order stable across in-place re-registration |
| `Register` same key again with new help/handler | replaces in place — `Ordered`/`Sections` show the definition once, no duplicate |
| ★ `Register` handler whose `string[]` parameter is not the final one | returns false |
| `Sections` groups by `Section`, preserves registration order | as stated |
| ★ concurrent `Register` from 8 threads while another thread reads **one captured `Snapshot`** (all reads through that single reference stay mutually consistent; the final published snapshot has every key resolvable) | as stated |
| ★ `FindNameCollisions` — section exists, then a command registered whose trimmed primary key matches the section name; and the reverse order (command first, section introduced later) → the collision is reported in **both** orders, and each publish logs a warning per collision | both orders |
| `IsUsableBy`: null privilege → true; `Access` Normal vs `AccessPrivilege.Ban` → false/true for GM | as stated |

Red: compile failure. Green: implement.

**Step 3: Implement `CommandRegistry`**

- Storage is an **immutable snapshot** published through a single `volatile` reference: `CommandSnapshot { Trie<CommandDefinition> Trie (every alias key → def), IReadOnlyDictionary<string, CommandDefinition> ByKey (every alias key), IReadOnlyList<CommandDefinition> Ordered }`. **Immutability is by construction discipline**: each mutation builds brand-new `Trie`/`Dictionary`/`List` instances and publishes them; a published instance is never mutated afterwards (the trie is the existing mutable `Trie<T>` class — the rule is simply that a published trie is never touched again, every mutation builds a new one). `CommandDefinition` carries `string[] Keys` + `PrimaryKey` (first key — usage strings and help). Readers (dispatch, help) take one snapshot reference and can never observe a half-updated state; help and dispatch always see the same version. (Alias-ready now so Part 2's multi-key attributes need no storage rework; Part 1 registrations simply have `Keys.Length == 1`.)
- Mutation protocol (one lock, used by `SeedBuiltins` and `Register`) — **exact algorithm**: a registration owns its full key set; replacing a definition frees **all** of its keys.
  1. Validate every key's format (and handler shape for `Register`); any failure → return false, nothing mutated.
  2. Collect the set of existing definitions owning any of the new keys. If it contains **two or more distinct definitions** → return false (cross-definition conflict), nothing mutated.
  3. If exactly one (the *replaced* definition): its keys are all freed — any subset may be re-registered, and keys **not** re-registered disappear. If the **replaced** privilege is restricted and the **new** privilege is open → return false (downgrade protection — the only refused direction). Open → restricted, and restricted → different-restricted, are both allowed (capabilities rule).
  4. Build the new ordered list (replaced definition removed; new definition inserted at the replaced one's **first** occurrence position, else appended), new `ByKey`, new trie → publish the new snapshot.
  Rebuild is O(total keys) and only happens on registration (startup + script reloads). This is the script-reload semantic: re-registering a command's keys takes over whatever those keys touched, and old aliases not re-registered are gone.
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
| Subcommand command: bare key → sends subcommand list; unknown sub → same; valid sub → runs bound from the tokens **after** the subcommand token (`/cmd make 1 2 3 4 Name` binds `r == 1` — the subcommand token itself is never a parameter) | as stated |
| ★ subcommand list is privilege-filtered: command with one open + one restricted subcommand — Normal player's bare/unknown-sub list shows only the open one; GM sees both; a direct call of the restricted sub by Normal is silently denied (debug log, no reply) | anti-probing parity with help |
| ★ subcommand command whose `CheckAccess` override denies the caller: bare key and unknown sub both get **no reply** (denial precedes the subcommand-list response) | CheckAccess ordering |
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
3. `CheckAccess` — **attributed commands only** (script definitions have `Handler`, no `Instance`; they skip this and rely on their registered static privilege checked in `AddEvent`):
   ```csharp
   if (def.Instance is BaseCommand command
       && command.CheckAccessInternal(ctx, Args) is { } p
       && !ctx.Player.HasPrivilege(p)) { debug log; return; }   // swallowed
   ```
   **Runs before any subcommand response** — a subcommand command whose argument-dependent access denies this caller gets no reply at all, not even the subcommand list.
4. If `def` has subcommands: first token selects (case-insensitive); none/unknown → `ctx.Send` the subcommand list (each: `name` + **composed-usage-key** usage line + help) **filtered by privilege — only subcommands whose own privilege passes for this player** (the same anti-probing rule as help: a restricted subcommand's name/usage must not appear in the list) and return.
5. Subcommand privilege → same swallow.
6. Precompute the selected target's usage line into `ctx.Usage`: `string usageKey = subcommand is null ? def.PrimaryKey : $"{def.PrimaryKey.TrimEnd()} {subcommand.PrimaryName}";` then `CommandBinder.Usage(usageKey, target.Parameters, target.UsageOverride)` (binder algorithm + `UsageOverride`). **Target tokens:** `string[] targetTokens = subcommand is null ? Args : Args[1..];` — the subcommand token is a selector, not a parameter (otherwise `/custom make 1 2 3 4 Name` would try to bind `"make"` as `int r`). Then `CommandBinder.Bind(world, ctx.Player, target.Parameters, targetTokens, ctx.Usage)` → error → `ctx.Send(error)`, return. `ctx.Args` keeps the full array for `CheckAccess` and handlers that need the raw command context.
7. Invoke: **attributed commands** — `Execute`/subcommand method via the captured `MethodInfo` with `[ctx, ...args]`. **Script-registered commands** (`def.Handler` non-null): the parameter source is `def.Handler.GetType().GetMethod("Invoke")!.GetParameters()` — the delegate's **own** `Invoke` parameters, which are exactly what `DynamicInvoke` accepts (`Handler.Method.GetParameters()` can differ for closed static or open instance delegates) — binding (step 6), usage generation (no override exists for scripts), and the rest-last validation all use it; invoke with `def.Handler.DynamicInvoke([ctx, ..args])`; `HelpFormatter` uses the same parameter source for script usage lines. **Exception hygiene (both paths):** wrap the invoke in `try { ... } catch (TargetInvocationException tie) { if (tie.InnerException is not null) ExceptionDispatchInfo.Capture(tie.InnerException).Throw(); throw; }` — reflection's wrapper is dropped and the **original stack trace is preserved** (`ExceptionDispatchInfo`, not `throw tie.InnerException`, which would reset the stack), so a migrated command that throws `InvalidOperationException` reaches the existing per-event catch in `EventHandler.Update` as `InvalidOperationException` with its real frames (diagnostics parity with the legacy events, which threw directly).

**Step 3: Rewire `EventHandler`**

- Ctor: after `_SeedCommands()`, call `world-less` `SeedBuiltins` — the registry is passed in or the ctor takes it; simplest: `GameWorld` ctor creates `Commands` **before** `EventHandler` and passes it: `this.EventHandler = new EventHandler(this.Commands);` (update `Goose/GameWorld.cs:105`).
- `AddEvent(Player, string)`: first `registry.Snapshot.Trie.TryGetLongestPrefix(packet, out def, out len)`. If hit: legacy definition → existing code path (factory/type instantiation, `ClientOriginated = true`, enqueue). New-style: class-level privilege check with the existing swallow + debug log (`Goose/EventHandler.cs:283` block), then enqueue — **exact initialization, including the inherited `Event` state:**
  ```csharp
  var ev = new CommandEvent(def, packet, len) { Player = player, ClientOriginated = true };
  ```
  `Player` is inherited `Event` state that `Ready`/`World`/`Send` depend on; `ClientOriginated = true` preserves the legacy execution-time drop behavior (e.g. packets dropped while a map is loading). If miss: fall through to the packet trie exactly as today.
- `_SeedCommands`: convert the table to registrations — non-command packet entries unchanged in the packet trie; every `/` entry becomes `registry.RegisterLegacy(key, typeof(XCommandEvent), privilege)` (same keys, same trailing spaces, same privileges — copy verbatim). `RegisterLegacy` takes no section/help — legacy commands stay out of help until migrated (Task 2 storage note).
- Rename the nested `EventHandler.CommandDefinition` to `PacketDefinition` (it now describes non-command packets only; avoids colliding with the new top-level `Goose.Commands.CommandDefinition`). The `Open`/`Restricted` helpers become `PacketDefinition.Open/Restricted` for the packet table.
- `RegisterEvent`: **unchanged signatures in this part** — both overloads keep working for all keys, with a warning logged for `/` keys (shipped `.csx` scripts depend on this until Part 3; see the ⚠ ordering constraint below). Hard rejection + overload deletion land in Part 3 Task 3.

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
public static List<List<string>>? BuildPages(Player player, CommandRegistry registry,
                                            string? name);       // pages for the window; null = nothing to show (caller sends nothing)
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
| ★ restricted command + public section, same name: Normal's `BuildPages` returns **only the section pages** (command invisible → omitted, section not suppressed); GM's returns command details + section | independent visibility resolution |
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
| ★ line numbering is one-based: the first `WindowTextLine` of a fresh page uses line **1** (assert on the packet, don't just trust the implementation) | line 1, then 2, 3… |

Red: compile failure. Green: implement.

**Step 2: Implement**

- `HelpWindow : Window` — model on `Goose/PlayerInfoWindow.cs`. **Complete creation contract:**
  - ctor: `ID = ++player.LastWindowID`, `Frame = WindowFrames.Quest`, `Type = WindowTypes.Help`, stores `List<List<string>> pages` + `pageNumber`, then calls `SendCreate(player, world)` (the create packet serializes `Title` and `Buttons` — the base `Window` values are null, so both must be overridden: `Goose/PlayerInfoWindow.cs:27`).
  - `Title => "Command Help"` (override; `Goose/PlayerInfoWindow.cs:7` is the pattern).
  - `Buttons => $"0,1,{(pageNumber == 0 ? 0 : 1)},{(pageNumber == pages.Count - 1 ? 0 : 1)},0"` — back hidden on page 0, next hidden on the last page (the exact strings from the test table above).
  - `Populate` sends each line via `P.WindowTextLine(this.ID, lineNumber, line)` (`Goose/Packets.cs:422`) with **one-based** `lineNumber` (first line = 1) — every existing window starts at 1 (`Goose/PlayerInfoWindow.cs:37`).
  - `Clicked` per the PlayerInfoWindow pattern (`Goose/PlayerInfoWindow.cs:135-157`): Next/Back adjust `pageNumber` **with clamping** (never below 0 / above the last page) and re-send via `SendCreate` (re-sends title, buttons, and lines); Close/Exit removes from `player.Windows`.
  - static `Open(GameWorld, Player, List<List<string>> pages)` constructs the window and adds it to `player.Windows` (the `PlayerInfoWindow.Open` pattern, `Goose/PlayerInfoWindow.cs:30`).
- `HelpFormatter.BuildPages`:
  - `name` null → `[SectionListPage, SectionPage(s) for each visible section]`. Subcommand lines use the **composed usage key** (`/custom make …`, per the usage algorithm) — never the bare command key.
  - **Name resolution — command and section visibility are resolved independently; every visible result is shown.** Input (case-insensitive, trailing space trimmed) matching a command and/or a section: visible command (`IsUsableBy`) → command details first; visible section (has ≥1 visible entry) → section page(s) after. Either one alone → just that one. Neither visible → `null` (no reply). An *inaccessible* same-named command never suppresses a visible section (anti-probing applies per-result, not to the name as a whole).
  - section (case-insensitive) → `[SectionPage]` (only visible commands).
  - Visibility: `CommandRegistry.IsUsableBy(player, def)`. **Subcommands are filtered too**: a subcommand line is shown only if the command is usable *and* the subcommand's own privilege passes — an open command with a restricted subcommand must not reveal the subcommand's name/usage to unprivileged players.
  - **Pagination height**: `public const int MaxLinesPerPage = 19;` — **every** page type (section list, section pages, command-detail pages) passes through the same height splitter at this limit. 19 is the demonstrated value: `PlayerInfoWindow`'s base page sends exactly 19 `WindowTextLine` lines (`Goose/PlayerInfoWindow.cs:40-58`) and renders correctly in production; the larger bank/equipment pages are separate windows, not evidence for a single window. If in-game testing shows the client safely takes more, the constant is the single place to raise it.
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
| Command+section name collision shows both (visible viewer) | `HelpTests` ★ |
| ★ Inaccessible command never suppresses a same-named visible section | `HelpTests` |
| Help lines ≤ 42 chars | `HelpTests` wrap cases |
| Legacy commands byte-for-byte unchanged | `CommandDispatchTests` baseline + full existing suite (395) |
| Non-`Ready` player: command no-op | `CommandDispatchTests` |

## Part 1 exit criteria

- `dotnet test` green across `Goose.Tests` and `Goose.IntegrationTests` (including all pre-existing dimension tests — the `.csx` scripts still run on `RegisterEvent`).
- `/help` works in-game; all 72 legacy command keys dispatch exactly as before.
- No file in `Goose/Events/*CommandEvent.cs` modified yet (migration is Parts 2–3).
