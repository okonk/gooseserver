# .NET 10, System.Text.Json, GetNewID, SQLite Isolation — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Retarget Goose + CsvToSql to .NET 10 with updated NuGet packages, replace Newtonsoft.Json with System.Text.Json (keeping existing DB JSON readable), fix server-full LoginID allocation, and route all SQLite access through a single dedicated writer/connection.

**Architecture:** Upgrade TFMs and packages first so the tree still builds. Migrate JSON next (shared `JsonHelper` + attribute renames) so inventory/quest blobs keep working. Fix `PlayerHandler` ID allocation so a full server returns `LoginID == 0` instead of hanging. Replace public `SqlConnection` + dual-thread use with a `Database` service that owns one connection on one thread; game code only calls `Execute` (sync, for loads / `last_insert_rowid`) or `Enqueue` (async writes).

**Tech Stack:** .NET 10, System.Text.Json (in-box), System.Data.SQLite.Core (updated), NLog, Roslyn scripting, ClosedXML

**Out of scope:** `IllutiaClientDataReader/*` (old .NET Framework 4.0 WinForms tools). Password hashing, connection DoS limits, and script sandboxing from the security review are separate work.

---

## APIs verified (current code)

| API | Location |
|-----|----------|
| `PlayerHandler.GetNewID` infinite `do/while` + `Random.Next(1, MaxPlayers)` | `Goose/PlayerHandler.cs:83-91` |
| `idToPlayer = new Player[MaxPlayers]` (indices `0..MaxPlayers-1`) | `Goose/PlayerHandler.cs:25` |
| Login full-server check expects `LoginID == 0` | `Goose/Events/LoginEvent.cs:221-228` |
| `AddPlayer` always writes `idToPlayer[LoginID]` (unsafe if `0`) | `Goose/PlayerHandler.cs:47-51` |
| `DatabaseWriter.Add(DbCommand)` + `Run` on background thread | `Goose/DatabaseWriter.cs:16-39` |
| Writer started in ctor via `LaunchDatabaseWriterThread` | `Goose/GameWorld.cs:121`, `683-688` |
| Connection opened on main thread: `CreateDbConnection` / `CreateDatabase` | `Goose/GameWorld.cs:124-168` |
| Public shared connection: `GameWorld.SqlConnection` | `Goose/GameWorld.cs:41` |
| Newtonsoft settings: `IgnoreAndPopulate` + `Ignore` nulls | `Goose/GameWorld.cs:77-87` |
| Settings load from `GooseSettings.json` (file has `//` comments) | `Goose/GameWorld.cs:87`, `Goose/GooseSettings.json` |
| Guild save uses sync `ExecuteNonQuery` + `last_insert_rowid()` on main thread | `Goose/Guild.cs:180-214` |
| Scripts import Newtonsoft | `Goose/Scripting/Script.cs:34-37`, `Data/**/ItemModifierScript.csx`, `HealerNPC.csx` |
| Host SDK | `dotnet --version` → `10.0.109` present |

**Target package versions (as of plan write; re-check with `dotnet add package` if needed):**

| Package | From | To |
|---------|------|-----|
| TFM | `net8.0` | `net10.0` |
| `Microsoft.CodeAnalysis.CSharp` | 3.4.0 | 5.6.0 |
| `Microsoft.CodeAnalysis.CSharp.Scripting` | 3.4.0 | 5.6.0 |
| `NLog` | 4.6.8 | 6.1.4 |
| `System.Data.SQLite.Core` | 1.0.112 | 1.0.119 |
| `System.ServiceProcess.ServiceController` | 4.7.0 | 10.0.0 (or latest 10.x matching runtime) |
| `ClosedXML` (CsvToSql.Core) | 0.102.2 | 0.105.0 |
| `Newtonsoft.Json` | 12.0.3 | **remove** |

**SQLite package choice:** Keep `System.Data.SQLite.Core` (API already used everywhere via `SQLiteConnection` / `SQLiteParameter`). Isolation is the goal, not a provider swap. Switching to `Microsoft.Data.Sqlite` would force a wide rename with no functional win for this server.

---

## Phase overview

```
Task 1  TFM + NuGet upgrades, build green (still Newtonsoft)
Task 2  System.Text.Json helper + model attributes
Task 3  Replace all JsonConvert call sites + scripts
Task 4  GetNewID / full-server handling
Task 5  Database service (single connection + thread)
Task 6  Migrate all SQL call sites off world.SqlConnection
Task 7  Smoke / regression checklist + Readme
```

Commit after each task.

---

### Task 1: Target .NET 10 and update NuGet packages

**Files:**
- Modify: `Goose/Goose.csproj`
- Modify: `CsvToSql/CsvToSql.Core/CsvToSql.Core.csproj`
- Modify: `CsvToSql/CsvToSql.Console/CsvToSql.Console.csproj`
- Modify: `Readme.md` (SDK note: .NET 8 → .NET 10)

**Step 1: Bump TFMs**

In all three csproj files set:

```xml
<TargetFramework>net10.0</TargetFramework>
```

**Step 2: Update Goose packages**

In `Goose/Goose.csproj` replace the `PackageReference` group with:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="5.6.0" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="5.6.0" />
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  <PackageReference Include="NLog" Version="6.1.4" />
  <PackageReference Include="System.Data.SQLite.Core" Version="1.0.119" />
  <PackageReference Include="System.ServiceProcess.ServiceController" Version="10.0.0" />
</ItemGroup>
```

Keep Newtonsoft for this task only so the upgrade is isolated; Task 3 removes it.

**Step 3: Update CsvToSql.Core**

```xml
<PackageReference Include="ClosedXML" Version="0.105.0" />
```

**Step 4: Restore and build**

```bash
dotnet restore Goose.sln
dotnet build Goose.sln -c Release
dotnet build CsvToSql/CsvToSql.sln -c Release
```

Expected: build succeeds. Fix any NLog 6 or Roslyn 5 API breaks if they appear (usually none for current usage: `LogManager.GetCurrentClassLogger()`, `CSharpScript.Create`).

**Step 5: NLog config check**

Open `Goose/NLog.config`. If build/runtime warns about obsolete config, align with [NLog 6 config](https://nlog-project.org/) (often still works as-is). Smoke: run server briefly and confirm log lines appear.

**Step 6: Commit**

```bash
git add Goose/Goose.csproj CsvToSql/CsvToSql.Core/CsvToSql.Core.csproj CsvToSql/CsvToSql.Console/CsvToSql.Console.csproj Readme.md Goose/NLog.config
git commit -m "chore: target net10.0 and update NuGet packages"
```

---

### Task 2: System.Text.Json infrastructure and model attributes

**Files:**
- Create: `Goose/JsonHelper.cs`
- Modify: `Goose/GameWorld.cs` (replace `JsonSerializerSettings` with STJ options)
- Modify: `Goose/Item.cs` (attributes)
- Modify: `Goose/Quests/QuestStatus.cs` (attributes + ctor)
- Modify: `Goose/ItemSlot.cs` (default Stack initializer)

**Step 1: Add `JsonHelper`**

Create `Goose/JsonHelper.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose
{
    public static class JsonHelper
    {
        /// <summary>
        /// Options for inventory/bank/quest/spellbook blobs stored in SQLite.
        /// Must remain compatible with historical Newtonsoft output (short names, omitted defaults/nulls).
        /// </summary>
        public static JsonSerializerOptions DatabaseOptions { get; } = CreateDatabaseOptions();

        /// <summary>
        /// Options for GooseSettings.json (// comments, trailing commas allowed).
        /// </summary>
        public static JsonSerializerOptions SettingsOptions { get; } = CreateSettingsOptions();

        private static JsonSerializerOptions CreateDatabaseOptions()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = false,
                // Do not rename properties; [JsonPropertyName] supplies short names.
                PropertyNamingPolicy = null,
                WriteIndented = false,
                // Dictionary&lt;ItemProperty, object&gt; and similar
                Converters = { new JsonStringEnumConverter() },
            };
            return options;
        }

        private static JsonSerializerOptions CreateSettingsOptions()
        {
            return new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
            };
        }

        public static string Serialize<T>(T value) =>
            JsonSerializer.Serialize(value, DatabaseOptions);

        public static T Deserialize<T>(string json) =>
            JsonSerializer.Deserialize<T>(json, DatabaseOptions);
    }
}
```

**Notes for implementer:**
- Newtonsoft used `DefaultValueHandling.IgnoreAndPopulate`. STJ does not honor `[DefaultValue]`. Rely on property initializers + missing-property defaults when deserializing.
- Omitting `WhenWritingDefault` intentionally: Newtonsoft often omitted zeros, but writing zeros is still valid on read. Prefer not dropping non-null default-looking values that matter (e.g. explicit `0` graphics). If blob size is a concern, add `WhenWritingDefault` later and re-test round-trips.
- `JsonStringEnumConverter` makes `ItemProperty` keys stringify as names (e.g. `"TitleId"`). Verify a real inventory blob that contains `props`; if existing data used numeric enum keys, switch to a custom converter that accepts both number and string. **Gate:** before deleting Newtonsoft, dump one inventory row and inspect `props`.

**Step 2: Rewrite attributes on models**

`Item.cs` — replace `using Newtonsoft.Json` with `using System.Text.Json.Serialization` and map:

| Newtonsoft | System.Text.Json |
|------------|------------------|
| `[JsonProperty(PropertyName = "id")]` | `[JsonPropertyName("id")]` |
| `[JsonIgnore]` | `[JsonIgnore]` |

Same for all short names: `tid`, `desc`, `ge`, `gt`, `gf`, `r`, `g`, `b`, `a`, `wdmg`, `stats`, `props`.

`QuestStatus.cs`:

```csharp
using System.Text.Json.Serialization;

public class QuestProgress
{
    [JsonPropertyName("id")]
    public int QuestId { get; set; }
    [JsonPropertyName("rid")]
    public int RequirementId { get; set; }
    [JsonPropertyName("p")]
    public long Progress { get; set; }

    public QuestProgress() { }

    public QuestProgress(int questId, int requirementId, long progress)
    {
        QuestId = questId;
        RequirementId = requirementId;
        Progress = progress;
    }
}
```

`ItemSlot.cs` — ensure missing Stack deserializes usefully:

```csharp
public long Stack { get; set; } = 1;
```

**Step 3: Point `GameWorld` at STJ for settings only (still Newtonsoft for DB in this task if preferred)**

Replace:

```csharp
Settings = JsonConvert.DeserializeObject<GooseSettings>(File.ReadAllText("GooseSettings.json", Encoding.UTF8));
```

with:

```csharp
Settings = JsonSerializer.Deserialize<GooseSettings>(
    File.ReadAllText("GooseSettings.json", Encoding.UTF8),
    JsonHelper.SettingsOptions);
```

Remove or replace `JsonSerializerSettings` static property with `JsonHelper.DatabaseOptions` (or delete and use `JsonHelper` only).

**Step 4: Build**

```bash
dotnet build Goose/Goose.csproj -c Release
```

Expected: succeeds (Newtonsoft still referenced until Task 3).

**Step 5: Commit**

```bash
git add Goose/JsonHelper.cs Goose/Item.cs Goose/Quests/QuestStatus.cs Goose/ItemSlot.cs Goose/GameWorld.cs
git commit -m "feat: add System.Text.Json helper and migrate model attributes"
```

---

### Task 3: Swap all Newtonsoft usage (app + scripts)

**Files:**
- Modify: `Goose/Inventory.cs`
- Modify: `Goose/PlayerBank.cs`
- Modify: `Goose/Spellbook.cs`
- Modify: `Goose/Player.cs`
- Modify: `Goose/Scripting/Script.cs`
- Modify: `Goose/Data/Illutia/Scripts/Item/ItemModifierScript.csx`
- Modify: `Goose/Data/Illutia/Scripts/NPC/HealerNPC.csx`
- Modify: `Goose/Data/Aspereta/Scripts/Item/ItemModifierScript.csx`
- Modify: `Goose/Goose.csproj` (remove Newtonsoft package)

**Step 1: Replace call sites**

Pattern:

```csharp
// Before
JsonConvert.SerializeObject(x, GameWorld.JsonSerializerSettings)
JsonConvert.DeserializeObject<T>(json, GameWorld.JsonSerializerSettings)

// After
JsonHelper.Serialize(x)
JsonHelper.Deserialize<T>(json)
```

Remove `using Newtonsoft.Json` from those files.

**Step 2: Round-trip sanity (manual or small console check)**

With a local `IllutiaGoose.db` if available:

1. Query one `inventory.serialized_data` row.
2. `JsonHelper.Deserialize<ItemSlot[]>(json)` should not throw.
3. Re-serialize and deserialize again; TemplateID / ItemID / Stack should match.

If no DB is present, craft a minimal JSON matching historical shape:

```json
[{"Item":{"id":1,"tid":2,"Name":"Sword","stats":{},"props":{}},"Stack":1},null]
```

and unit-smoke via a temporary test or `dotnet script` / small Program debug path. Prefer adding a tiny xUnit project only if you already want tests; otherwise a one-off `dotnet run` harness is enough for this legacy codebase.

**Step 3: Scripts + Roslyn imports**

`Script.cs` LoadScript options:

```csharp
var scriptOptions = ScriptOptions.Default
    .WithReferences(
        Assembly.GetExecutingAssembly(),
        typeof(System.Text.Json.JsonSerializer).Assembly)
    .WithImports(
        "System", "System.Collections.Generic", "System.Linq",
        "System.Text.Json",
        "Goose", "Goose.Events", "Goose.Quests", "Goose.Scripting");
```

Update each `.csx` that used Newtonsoft, e.g. `ItemModifierScript.csx`:

```csharp
// Before
using Newtonsoft.Json;
var operations = JsonConvert.DeserializeObject<ModifierOperation[]>(modifier.ScriptParams);

// After
using System.Text.Json;
var operations = JsonSerializer.Deserialize<ModifierOperation[]>(
    modifier.ScriptParams, JsonHelper.DatabaseOptions);
```

Same for `HealerNPC.csx`.

**Step 4: Remove package**

Delete Newtonsoft `PackageReference` from `Goose.csproj`. Grep to confirm zero remaining references:

```bash
rg -n "Newtonsoft|JsonConvert|JsonProperty\b|JsonSerializerSettings" Goose CsvToSql || true
```

**Step 5: Build + quick server start**

```bash
dotnet build Goose.sln -c Release
dotnet run --project Goose/Goose.csproj -c Release
```

Expected: loads maps/items/players without JSON exceptions. `/reloadscripts` or startup global scripts load cleanly.

**Step 6: Commit**

```bash
git add -A Goose/
git commit -m "refactor: replace Newtonsoft.Json with System.Text.Json"
```

---

### Task 4: Fix GetNewID and full-server handling

**Files:**
- Modify: `Goose/PlayerHandler.cs`
- Modify: `Goose/Events/LoginEvent.cs` (only if needed after AddPlayer change)
- Optional smoke: document manual steps

**Problem recap:**
- `Random.Next(1, MaxPlayers)` never uses index `MaxPlayers` and loops forever when all IDs are taken.
- Login already handles `LoginID == 0` but `GetNewID` never returns 0.
- `AddPlayer` would assign `idToPlayer[0] = player` if LoginID is 0.

**Step 1: Expand ID table and implement linear allocation**

In `PlayerHandler`:

```csharp
// Support LoginIDs 1..MaxPlayers inclusive; 0 = none / full
private Player[] idToPlayer = new Player[GameWorld.Settings.MaxPlayers + 1];

public int GetNewID(GameWorld world)
{
    for (int id = 1; id <= GameWorld.Settings.MaxPlayers; id++)
    {
        if (this.idToPlayer[id] == null)
            return id;
    }
    return 0;
}

public void AddPlayer(Player player, GameWorld world)
{
    player.LoginID = this.GetNewID(world);
    this.players.Add(player);
    this.sockToPlayer[player.Sock] = player;
    if (player.LoginID != 0)
        this.idToPlayer[player.LoginID] = player;
}

public void RemovePlayer(Player player)
{
    this.sockToPlayer.Remove(player.Sock);
    this.players.Remove(player);
    if (player.LoginID != 0 && player.LoginID < this.idToPlayer.Length)
        this.idToPlayer[player.LoginID] = null;
    player.LoginID = 0;
}

public void AssignNewId(GameWorld world, Player player)
{
    if (player.LoginID != 0 && player.LoginID < this.idToPlayer.Length)
        this.idToPlayer[player.LoginID] = null;

    player.LoginID = this.GetNewID(world);
    if (player.LoginID != 0)
        this.idToPlayer[player.LoginID] = player;
}
```

**Step 2: Confirm LoginEvent full path**

Existing code at `LoginEvent.cs:221-228` already:

1. `AddPlayer`
2. If `LoginID == 0` → deny, disconnect, `RemovePlayer`

No change required if Step 1 is correct. Ensure disconnect does not double-remove sock (already OK).

**Step 3: NPC ID range check**

`NPCHandler` assigns IDs via `Random.Next(MaxPlayers + 1, MaxNPCs)` (`Goose/NPCHandler.cs` ~233). After expanding player IDs to include `MaxPlayers`, player and NPC spaces still meet at boundary but should not collide if NPCs start at `MaxPlayers + 1`. Confirm that line still starts at `MaxPlayers + 1`.

**Step 4: Manual verification**

1. Temporarily set `"MaxPlayers": 2` in `GooseSettings.json`.
2. Log in two characters → both succeed.
3. Third login → `LNO` / “server is full”, server stays responsive (no hang).
4. One logout, fourth login → succeeds.
5. Restore `MaxPlayers` to 200.

**Step 5: Commit**

```bash
git add Goose/PlayerHandler.cs
git commit -m "fix: allocate LoginIDs without hang when server is full"
```

---

### Task 5: Introduce single-connection Database service

**Files:**
- Create: `Goose/Database.cs` (or rewrite `Goose/DatabaseWriter.cs` in place)
- Modify: `Goose/GameWorld.cs` (ownership, startup/shutdown)
- Delete or gut: old `DatabaseWriter` public surface after migration

**Design**

One thread owns one `SQLiteConnection`. All access is marshalled:

| Method | Semantics | Use |
|--------|-----------|-----|
| `Execute(Action<SQLiteConnection>)` | Blocks caller until done on DB thread | Startup loads, guild create needing `last_insert_rowid`, rare sync reads |
| `Execute<T>(Func<SQLiteConnection, T>)` | Same, returns value | Scalar / reader materialization |
| `Enqueue(Action<SQLiteConnection>, Action<Exception> onComplete = null)` | Fire-and-forget write | Player/item/log saves |
| `PendingCount` | Queue depth | Shutdown wait |
| `Start(connectionString)` / `Stop()` | Lifecycle | Open WAL, join thread |

**Step 1: Implement `Database`**

```csharp
using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace Goose
{
    public sealed class Database
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly BlockingCollection<WorkItem> _queue = new();
        private SQLiteConnection _connection;
        private Task _loopTask;
        private readonly int _dbThreadId;

        private abstract class WorkItem { }
        private sealed class SyncWork : WorkItem
        {
            public Action<SQLiteConnection> Action;
            public Func<SQLiteConnection, object> Func;
            public object Result;
            public Exception Error;
            public ManualResetEventSlim Done = new(false);
        }
        private sealed class AsyncWork : WorkItem
        {
            public Action<SQLiteConnection> Action;
            public Action<Exception> OnComplete;
        }

        public int PendingCount => _queue.Count;

        public void Start(string databaseName)
        {
            // FailIfMissing false for create path handled by caller before Start,
            // or open with Create if missing — match existing CreateDbConnection behavior.
            var cs = string.Format(
                "Data Source={0}.db; Version=3; Journal Mode=WAL; BusyTimeout=5000;",
                databaseName);
            _connection = new SQLiteConnection(cs);
            _connection.Open();

            // Optional but recommended:
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys=ON;";
                cmd.ExecuteNonQuery();
            }

            _loopTask = Task.Factory.StartNew(Loop, TaskCreationOptions.LongRunning);
        }

        private void Loop()
        {
            foreach (var item in _queue.GetConsumingEnumerable())
            {
                try
                {
                    if (item is SyncWork sync)
                    {
                        try
                        {
                            if (sync.Func != null)
                                sync.Result = sync.Func(_connection);
                            else
                                sync.Action(_connection);
                        }
                        catch (Exception e)
                        {
                            sync.Error = e;
                            log.Error(e, "SQL sync work failed");
                        }
                        finally
                        {
                            sync.Done.Set();
                        }
                    }
                    else if (item is AsyncWork async)
                    {
                        try
                        {
                            async.Action(_connection);
                            async.OnComplete?.Invoke(null);
                        }
                        catch (Exception e)
                        {
                            log.Error(e, "SQL async work failed");
                            async.OnComplete?.Invoke(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error(e, "Database loop error");
                }
            }

            _connection?.Close();
            _connection?.Dispose();
        }

        public void Execute(Action<SQLiteConnection> action)
        {
            var work = new SyncWork { Action = action };
            _queue.Add(work);
            work.Done.Wait();
            if (work.Error != null) throw work.Error;
        }

        public T Execute<T>(Func<SQLiteConnection, T> func)
        {
            var work = new SyncWork { Func = conn => func(conn) };
            _queue.Add(work);
            work.Done.Wait();
            if (work.Error != null) throw work.Error;
            return (T)work.Result;
        }

        public void Enqueue(Action<SQLiteConnection> action, Action<Exception> onComplete = null)
        {
            _queue.Add(new AsyncWork { Action = action, OnComplete = onComplete });
        }

        public void Stop()
        {
            _queue.CompleteAdding();
            _loopTask?.Wait(TimeSpan.FromMinutes(2));
        }
    }
}
```

**Critical rule:** Never call `CreateCommand` on a connection from the game thread. Commands are created **inside** `action`/`func` on the DB thread.

**Deadlock rule:** Game thread may `Execute` while holding no DB lock. DB thread must **never** call back into game code that then `Execute`s again (no re-entrant sync). Callbacks from `Enqueue` (`onComplete`) currently send packets from `UpdateSqlCommandEvent` — that is OK if they only touch game state / sockets, not `Execute`. Document this.

**Step 2: Wire into `GameWorld`**

Replace:

```csharp
public DbConnection SqlConnection { get; set; }
public DatabaseWriter DatabaseWriter { get; set; }
```

with:

```csharp
public Database Database { get; private set; }
```

Constructor: create `Database` but do **not** open until `Start()`.

`Start()` flow:

1. Ensure DB file exists (existing create logic can open a temporary connection on the main thread **only for first-time create**, then close it; or implement create SQL entirely via `Database.Start` + `Execute`). Prefer: if file missing, create empty file + open Database + `Execute` all schema SQL + optional Google import; if file exists, `Start` with FailIfMissing semantics.
2. `Database.Start(Settings.DatabaseName)`
3. All handler loads use `world.Database.Execute(conn => { ... })`
4. Remove `LaunchDatabaseWriterThread` / old writer.

`Stop()`:

```csharp
// existing player saves enqueue...
while (Database.PendingCount > 0)
    Thread.Sleep(100);
Database.Stop();
```

**Step 3: First-time DB creation**

Move `CreateDatabase` / `ExecuteSql` so schema scripts run inside `Database.Execute` after `Start`. Connection string without `FailIfMissing` when creating.

**Step 4: Commit skeleton (still compiling with temporary adapters if needed)**

If migration of all call sites is large, temporary compatibility shim is OK for one commit:

```csharp
// TEMP — remove in Task 6
[Obsolete]
public void EnqueueCommand(string sql, Action<SQLiteCommand> bind = null, Action<Exception> cb = null)
{
    Database.Enqueue(conn =>
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        bind?.Invoke(cmd);
        cmd.ExecuteNonQuery();
    }, cb);
}
```

Prefer finishing Task 6 in the same session rather than leaving the shim long-term.

```bash
git add Goose/Database.cs Goose/GameWorld.cs
git commit -m "feat: add single-threaded Database service for SQLite"
```

---

### Task 6: Migrate all SQL call sites

**Files (every `SqlConnection` / `DatabaseWriter` use):**

| Area | Files |
|------|--------|
| Startup loads | `ItemHandler`, `MapHandler`, `Map`, `NPCHandler`, `SpellHandler`, `ClassHandler`, `CombinationHandler`, `QuestHandler`, `GuildHandler`, `PlayerHandler`, `ChatFilter`, `GameWorld` |
| Player persist | `Player`, `Inventory`, `Spellbook`, `PlayerBank`, `Pet` |
| Guild | `Guild`, `GuildHandler` |
| Logs | `Log`, `LogHandler` |
| Events | `UpdateSqlCommandEvent`, `CreditsUpdateEvent` (if re-enabled), `GuildSaveEvent` path |

**Migration patterns**

**A. Async write (was `DatabaseWriter.Add(command)`)**

```csharp
// Before
var command = world.SqlConnection.CreateCommand();
command.CommandText = query;
command.Parameters.Add(new SQLiteParameter("@playerName", DbType.String) { Value = this.Name });
world.DatabaseWriter.Add(command);

// After
var name = this.Name; // capture locals for closure
// ... capture all values used in SQL ...
world.Database.Enqueue(conn =>
{
    using var command = conn.CreateCommand();
    command.CommandText = query;
    command.Parameters.Add(new SQLiteParameter("@playerName", DbType.String) { Value = name });
    command.ExecuteNonQuery();
});
```

**B. Sync read (startup / load)**

```csharp
// Before
var command = world.SqlConnection.CreateCommand();
command.CommandText = "SELECT * FROM item_templates";
var reader = command.ExecuteReader();
while (reader.Read()) { ... }

// After
world.Database.Execute(conn =>
{
    using var command = conn.CreateCommand();
    command.CommandText = "SELECT * FROM item_templates";
    using var reader = command.ExecuteReader();
    while (reader.Read()) { ... }
});
```

Materialize into lists inside the lambda so readers never escape the DB thread.

**C. Guild save with `last_insert_rowid`**

Must use `Execute` (sync), not `Enqueue`, so `this.ID` is set before return:

```csharp
world.Database.Execute(conn =>
{
    using var command = conn.CreateCommand();
    // insert...
    command.ExecuteNonQuery();
    command.CommandText = "SELECT last_insert_rowid()";
    this.ID = Convert.ToInt32(command.ExecuteScalar());
});
```

**D. `/updatesql`**

```csharp
world.Database.Enqueue(conn =>
{
    using var command = conn.CreateCommand();
    command.CommandText = sqlData;
    command.ExecuteNonQuery();
}, ex => UpdateCompletedCallback(ex, world));
```

Fix ROLLBACK path: only roll back if you began a transaction. Prefer:

```csharp
world.Database.Enqueue(conn =>
{
    using var tx = conn.BeginTransaction();
    try
    {
        using var command = conn.CreateCommand();
        command.Transaction = tx;
        command.CommandText = sqlData;
        command.ExecuteNonQuery();
        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}, callback);
```

**E. Remove dead API**

- Delete `GameWorld.SqlConnection`
- Delete `DatabaseWriter.cs` (or empty obsolete type)
- Grep: `SqlConnection|DatabaseWriter` must be clean

**Step: Build after each logical group** (handlers load → player save → guild → events) so breakage is local.

```bash
dotnet build Goose/Goose.csproj -c Release
```

**Commit:**

```bash
git add Goose/
git commit -m "refactor: route all SQLite access through Database service"
```

---

### Task 7: Integration smoke checklist and docs

**Step 1: Smoke script (manual)**

With a clean or copied DB:

1. `dotnet run --project Goose/Goose.csproj -c Release`
2. Confirm log: maps, items, players loaded; “Ready to join.”
3. Login existing character — inventory/equipment/spells/quests load.
4. Move item, cast spell, drop/pickup gold, logout, login again — persistence holds.
5. Create new character (if auto-create on) — appears in DB after save period / logout.
6. Set `MaxPlayers` to 1 temporarily — second client gets full message; server does not hang.
7. GM `/updatesql` only if spreadsheet access is available (optional).
8. Restart server — no SQLite “database is locked” spam under normal play.

**Step 2: Update `Readme.md`**

- Install .NET **10** SDK (not 8).
- Note SQLite uses WAL mode after this change (file siblings `*.db-wal`, `*.db-shm` may appear).

**Step 3: Final commit**

```bash
git add Readme.md docs/plans/2026-07-23-dotnet10-json-sqlite-loginid.md
git commit -m "docs: note .NET 10 and Database/WAL behavior"
```

---

## Risk register

| Risk | Mitigation |
|------|------------|
| STJ cannot read old inventory JSON | Inspect live blobs; dual-read fallback only if needed; preserve `[JsonPropertyName]` short names |
| Script JSON options differ | Pass `JsonHelper.DatabaseOptions` into script deserializes |
| `Execute` from game loop blocks tick | Keep loads at startup; keep hot-path writes on `Enqueue`; guild save is rare |
| Deadlock if DB callback → `Execute` | Document; never call `Execute` from `Enqueue` completion for heavy work — schedule an Event instead |
| NLog 6 config break | Test logging immediately in Task 1 |
| Roslyn 5 scripting break | Compile one `.csx` at startup |
| WAL + copy/backup habits | Document that backups should checkpoint or copy wal too |

---

## Suggested PR / commit sequence

1. `chore: target net10.0 and update NuGet packages`
2. `feat: add System.Text.Json helper and migrate model attributes`
3. `refactor: replace Newtonsoft.Json with System.Text.Json`
4. `fix: allocate LoginIDs without hang when server is full`
5. `feat: add single-threaded Database service for SQLite`
6. `refactor: route all SQLite access through Database service`
7. `docs: note .NET 10 and Database/WAL behavior`

---

## Execution handoff

Plan is ready to implement task-by-task. Recommended order is exactly Tasks 1→7 so each commit stays buildable.

**Do not start coding until the user picks an execution mode** (see message after this file is saved).
