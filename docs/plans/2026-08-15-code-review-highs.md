# Code Review 2026-08-15 — HIGH Findings Implementation Plan

**Goal:** Fix HIGH findings H1, H2, H3, H4, H6, H7, H8, H9 from `docs/code-review-2026-08-15.md` (H5 and H10 are out of scope this round).

**Architecture:** One self-contained task per finding, each with its own commit and regression tests. Liveness fixes first (H6, H7, H2), then untrusted-input bounds (H1, H3, H4), then data integrity (H8, H9). No schema migrations are required. All work is inside `Goose/` + `Goose.Tests/`.

**Tech Stack:** .NET (C#), xUnit, System.Data.SQLite, NLog.

---

## APIs verified (do not re-derive; trust these citations)

- `Event` (Goose/Event.cs): `public long Ticks { get; set; }`, `public Object Data`, `public Player Player`, `public NPC NPC`. **Constructor sets `Ticks = Stopwatch.GetTimestamp()` (now)** — recurring events use `ev.Ticks += period`; `ClearMapItemsEvent` uses assignment `this.Ticks = world.TimeNow + period`.
- `GameWorld.TimeNow` (GameWorld.cs:74) = `Stopwatch.GetTimestamp()` — same clock as `Event.Ticks`. `EventHandler.Update` (EventHandler.cs:361) drains `while (tick <= now)`.
- `EventHandler` (Goose/EventHandler.cs): `AddEvent(Event)` :329, `RemoveEvent(Event)` :343, `Update(GameWorld)` :361; heap field `events` is private (no count accessor today).
- `GameWorld.Settings` (GameWorld.cs:51): `public static GooseSettings Settings { get; set; }` — mutable static; tests must save/restore around mutations. Relevant fields (GooseSettings.cs): `int PlayerSavePeriod` :85, `decimal SpellEffectPeriod` :87, `int ItemGroundSweepTime` :104, `int IdleTimeout` :135, `int PreLoginTimeoutSeconds` :122.
- `GameWorld.Received(Socket, string)` (GameWorld.cs:500): if `PlayerHandler.GetPlayer(sock)` is null, wraps the **raw chunk** in a `LoginEvent` (`ev.Data = new object[] { sock, data }`). No pre-login buffer exists.
- `GameWorld.LostConnection(Socket)` (GameWorld.cs:467): logs, calls `GameServer.Disconnect(sock)`, enqueues `LogoutEvent` — all inside a try/catch that swallows exceptions.
- `GameWorld.Send(Player, string)` (GameWorld.cs:591-603): the **only** caller of `Player.Send(string)`; swallows all exceptions with an empty catch. Skips `Pet`s.
- `Player.Send(string)` (Player.cs:2406-2422): `lock (socketLock)` → `sock.Send(bytes)`; only handles the partial-send return, not the would-block `SocketException`. `Player.Send()` flush (Player.cs:424-431) is unchanged and its exception is handled by GameServer's write loop (drops the connection).
- `Player`: `public List<byte> SendBuffer { get; private set; }` (Player.cs:42) — **initialized in `OnLogin()` (Player.cs:509), so it can be null before login**; `public Socket Sock { get; set; }` (Player.cs:35); `public States State { get; set; }` (Player.cs:69); `public bool AutoCreatedNotSaved { get; set; }` (Player.cs:94).
- `GameServer` (Goose/GameServer.cs): `private bool stopping = false` (:61) set only in `Stop()` (:288); `Run()` restart loop (:79-118) — crash path appends crashlog **unguarded** (:101-106), calls `this.Stop()` (:110-112), sleeps 10s, continues; `Start()` (:128-139) does `IPAddress.Parse(GameWorld.Settings.GameServerIP)` + `listen.Bind` with no error handling; `GameLoop` ends with `if (!stopping) this.Stop();` (:274-275); `Stop()` = set flag, `gameworld.Stop()`, close `this.sockets`, `NLog.LogManager.Shutdown()` (:287-299); `Disconnect(Socket)` (:306-311) and `DropSocket` (:382-404) both use `this.sockets` / `this.connections` / `this.connectionsPerIP`, which are **assigned only in `Run()`'s loop** — null on an unstarted `GameServer` (test-relevant). `RequestShutdown()` (:332-341) only sets `gameworld.Running = false`.
- `LoginEvent` (Goose/Events/LoginEvent.cs): classic parse at :58-66 (`StartsWith("LOGIN")`, requires `IndexOf(',')`, `Length < 6` guard); Illutia parse at :69-87 (XOR "Tamra" over ASCII bytes, `data.Length < 69` after skipping 2 leading bytes — i.e. a complete Illutia login string is **≥ 71 chars**); short name/password check at :105-110; auto-creation block at :144-156 (only checks letters-only via `name.All(...)`; **no max length**); `new Player(0); player.Sock = sock; player.LoadFromAutoCreate(name, password, world); world.PlayerHandler.AddPlayerToData(player);` at :150-153.
- `ChatEvent` (Goose/Events/ChatEvent.cs:19-40): message = `((string)this.Data)` with first char stripped (the `;`); only guard is `message.Length == 1 return;`. Then map mute check, `world.LogHandler.Log(Log.Types.Chat, ...)`, broadcast via `P.Chat`, script hooks.
- `TellEvent` (Goose/Events/TellEvent.cs:33-46): `info = ((string)this.Data).Substring(6)`; `message = info.Substring(name.Length + 1)`; guard is `message.Length > 0` only; logs `Log.Types.Tell` then delivers.
- `AuctionCommandEvent` (Goose/Events/AuctionCommandEvent.cs:17-31): `data = ((string)this.Data).Substring(9)`; guard `data.Length <= 0 return;`; logs `Log.Types.Auction` then broadcasts.
- `Database` (Goose/Database.cs): `public sealed class`; `Start(string databasePath)` :46; `Execute`/`Execute<T>` sync; `Enqueue(action, onComplete)` :227; `EnqueueTransaction(Action<SQLiteConnection>)` :241 — wraps `Enqueue` and issues `BEGIN;` … `action(conn)` … `COMMIT;` with ROLLBACK+rethrow on failure. **No post-commit hook today.** `PendingCount` :39; `Stop()` :262. Per-item try/catch in the worker loop (Database.cs:127-161).
- `Player.SaveToDatabase(GameWorld)` (Player.cs:841-901): snapshots on game thread; `if (this.GuildID == 0 && this.Guild != null) this.Guild.Save(world);` at :851 (synchronous `Database.Execute`); builds `work` list (`savePlayerRow`, `Inventory.BuildSave()`, `Spellbook.BuildSave()`, `Bank.BuildSave(this)`, per-pet `pet.BuildSave()`, `BuildSaveQuests()`); one `world.Database.EnqueueTransaction(conn => foreach part: part(conn))` at :897-901. INSERT branch clears `this.AutoCreatedNotSaved = false` **inside the work item** (Player.cs:863) — before COMMIT.
- `Player.BuildInsertQuery()` (Player.cs:910-968) bakes `this.GuildID` into the SQL text; `Player.BuildUpdateQuery()` (Player.cs:997-1053) same (`"guild_id=" + this.GuildID` at :1020). `BuildInsertCommand`/`BuildUpdateCommand` (Player.cs:1000-1010 / :1055-1067) are called **on the DB thread inside the work-item lambda** and bind `@playerName/@playerTitle/@playerSurname/@unbanDate/@playerProperties`.
- `Pet.BuildSave()` (Pet.cs:313-420): returns `Action<SQLiteConnection>`; INSERT branch clears `this.AutoCreatedNotSaved = false` inside the work item (Pet.cs:377); also has Delete and UPDATE branches.
- `Guild` (Goose/Guild.cs): `Save(GameWorld)` :181-265 — synchronous `Database.Execute`: if `ID == 0` INSERT `guilds` then `SELECT last_insert_rowid()` → `this.ID`, set `player.GuildID = this.ID` for `OnlineMembers`; else UPDATE `guilds`; then per dirty `status`: `GuildRanks.Deleted` → DELETE from `guild_members`, `JustAdded` → plain INSERT, else UPDATE; clears `status.Dirty`/`JustAdded` and `this.Dirty` at the end.
- `guild_members` schema (Goose/sql/guilds.sql): `PRIMARY KEY (guild_id, player_id)` → `INSERT … ON CONFLICT(guild_id, player_id) DO UPDATE SET guild_rank=…` is valid.
- `GuildHandler.Save(GameWorld)` (Goose/GuildHandler.cs:90-105): sync-saves newguilds, registers `this.guilds[guild.ID]`, then saves every dirty guild, clears newguilds, reschedules `GuildSaveEvent` (`AddSaveEvent` :113-120, period `GuildSavePeriod` — audited safe at 0).
- Test harness: `new GameWorld(new GameServer())` constructs fine unstarted (used by `Goose.Tests/EventHandlerQueueTests.cs`). `new Player(0)`, `new Map()` (public ctor, Map.cs:87; `Map.CanChat`/`Muted` settable, Map.cs:50/57) all usable in tests. `InternalsVisibleTo` for Goose.Tests is set in `Goose/Goose.csproj:18` — `internal` seams are reachable from tests. Persistence-test pattern: execute `BuildInsertQuery`/`BuildInsertCommand` against a temp SQLite file using the shipped `sql/players.sql` (`Goose.Tests/PlayerPropertiesPersistenceTests.cs`).

## Conventions for every task

- **Settings mutation in tests:** `GameWorld.Settings` is a mutable static. Any test that changes it must save the old value and restore in `finally` (xUnit may run classes in parallel in the same process).
- **Comments:** per AGENTS.md, no new comments/doc strings unless the "why" is non-obvious (cite the finding, e.g. "H6:" / "H2:" where a guard exists only because of a verified bug). Keep any such comment ≤ 2 lines.
- **Build/test:** `dotnet build Goose.sln` and `dotnet test Goose.Tests` (323 tests pass at baseline). One commit per task, message `Fix Hx: <short description>`.

---

### Task 1: H6 — zero/negative config intervals can hard-freeze the game thread

**Problem:** `EventHandler.Update` spins forever if a self-re-enqueueing event reschedules at or before `now`. Several recurring events compute the reschedule from unvalidated settings; a 0 (or negative) value re-enqueues at `now` every pass → infinite loop, silent hang.

**Change (clamp to ≥1 period at every reschedule site):**

1. `Goose/GameWorld.cs:349` — startup schedule of `PlayerCountExperienceModifierUpdateEvent`: `updateExperienceModifier.Ticks += this.TimerFrequency * Math.Max(1, GameWorld.Settings.IdleTimeout);`
2. `Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs:50` — `this.Ticks += world.TimerFrequency * Math.Max(1, GameWorld.Settings.IdleTimeout);`
3. `Goose/MapHandler.cs:81` — `ev.Ticks += world.TimerFrequency * Math.Max(1, GameWorld.Settings.ItemGroundSweepTime);`
4. `Goose/Events/ClearMapItemsEvent.cs:33` — `this.Ticks = world.TimeNow + world.TimerFrequency * Math.Max(1, GameWorld.Settings.ItemGroundSweepTime);`
5. `Goose/Events/BuffTickEvent.cs` (`CheckAndAddBuffTickEvent`, the line `ev.Ticks += (long)(GameWorld.Settings.SpellEffectPeriod * world.TimerFrequency);`) — clamp `SpellEffectPeriod` (it is `decimal`): `(long)(Math.Max(1m, GameWorld.Settings.SpellEffectPeriod) * world.TimerFrequency)`.
6. `Goose/NPC.cs:1532` — same clamp in the NPC buff-add path.
7. `Goose/Player.cs:2140` — same clamp in the player buff-add path.
8. `Goose/Player.cs` `AddSaveEvent` (~1880-1901): compute `long savePeriodTicks = (long)(Math.Max(1, GameWorld.Settings.PlayerSavePeriod) * world.TimerFrequency);` once and use it for **both** the ping-timeout comparison (`(world.TimeNow - this.LastPing) > savePeriodTicks * 1.10`) and the `PlayerSaveEvent` schedule. At `PlayerSavePeriod = 0` the comparison currently disconnects on every PONG.
9. `Goose/Events/ScriptTimerEvent.cs` `Create` (:28-32) and `Reschedule` (:35-41): guarantee the computed tick delta is at least one tick, e.g. `long ticks = Math.Max(1, (long)(world.TimerFrequency * period.TotalSeconds));` — `TimeSpan.Zero` from a script must not reschedule at `now` (the `Reschedule` case is the spin).

Do **not** change `EventHandler.Update` itself and do **not** clamp one-shot (non-self-rescheduling) events. `GuildSavePeriod`/`LogoutLagTime` etc. were audited safe — leave them.

**Tests** (`Goose.Tests/EventHandlerIntervalTests.cs` or extend `EventHandlerQueueTests`): for each self-rescheduling event, set the relevant setting to 0 (save/restore), drive `Ready` with `new GameWorld(new GameServer())`, assert the rescheduled `Ticks > world.TimeNow` (i.e. a future tick, and `Update(world)` returns without running it again — no spin). Practical coverage:
- `ClearMapItemsEvent` with `Data = new Map()`, `ItemGroundSweepTime = 0` (adversarial: this is the literal freeze).
- `PlayerCountExperienceModifierUpdateEvent` with `IdleTimeout = 0` (LogHandler is empty on a fresh test world, so its `Save` call is a no-op).
- `ScriptTimerEvent.Create`/`Reschedule` with `TimeSpan.Zero` (assert returned event's `Ticks`).
- `Player.AddSaveEvent` with `PlayerSavePeriod = 0`: build a player (`new Player(0)`, set `State`, `Sock` may stay null — assert no `LostConnection` effect by asserting a `PlayerSaveEvent` is enqueued, e.g. via a new `internal int Count => this.events.Count;` on `EventHandler` used to compare before/after), and that its tick is in the future.
- `BuffTickEvent`/`AddBuff` clamp sites (Player/NPC) need heavy Buff/SpellEffect construction; cover by asserting the shared clamp expression is used — if constructing a minimal `Buff` proves practical, do it; otherwise note in the report that those sites are covered by review (they are three identical one-line clamps).

Red phase: with the setting at 0, `ev.Ticks` equals `now` (≤ now), so the future-tick assertion fails before the fix.

**Commit:** `Fix H6: clamp recurring-event intervals so 0/negative settings reschedule in the future instead of freezing the game thread`

---

### Task 2: H7 — crash-restart lifecycle loses saves, kills logging, never fast-fails

**Problem (all in `Goose/GameServer.cs`):** after any crash, `Stop()` is called from the restart path, setting `stopping = true` permanently and calling `NLog.LogManager.Shutdown()`. The *next* Ctrl+C/SIGTERM then skips `Stop()` in `GameLoop` (`if (!stopping) this.Stop();`) → no player saves, no DB drain. Additionally: bind/`IPAddress.Parse` failures restart forever every 10s; the crashlog write can throw inside the catch and kill the process with a misleading IO error.

**Changes:**

1. Split `Stop()` into the world teardown and the final-exit steps:
   - `public void Stop()` → sets `this.stopping = true`, calls a new `private void StopWorld()`, then `NLog.LogManager.Shutdown()`.
   - `private void StopWorld()` → `this.gameworld.Stop();` + close every socket in `this.sockets` (best-effort per socket).
   - Crash path in `Run()` replaces `try { this.Stop(); } catch { }` with `try { this.StopWorld(); } catch { }` — no `stopping` flag, no NLog shutdown, so the restarted world logs and the next signal runs the full `Stop()` (saves + drain).
2. Fast-fail on bind errors: extract the listen-socket creation from `Start()` into `internal Socket CreateListenSocket()` (socket creation + `IPAddress.Parse(GameWorld.Settings.GameServerIP)` + `Bind` + `Listen(10)`). On any exception throw a new `sealed class FatalStartupException : Exception` (file `Goose/FatalStartupException.cs`) with a clear message including the failing IP/port. `Run()` gets a `catch (FatalStartupException e)` **before** the generic catch: print it, `Environment.ExitCode = 1; break;` (no restart, no crashlog). World-load failures (`gameworld.Start()`) keep the existing crash-restart behavior.
3. Guard the crashlog write in `Run()`'s catch: wrap the `File.AppendText` block in its own try/catch; on failure log the IO error and continue with the normal restart flow.

**Tests** (`Goose.Tests/GameServerStartupTests.cs`):
- `CreateListenSocket` with `GameWorld.Settings.GameServerIP = "not-an-ip"` (save/restore) → throws `FatalStartupException`.
- `CreateListenSocket` with a port already bound (open a listener on `127.0.0.1:0` first, reuse its port) → throws `FatalStartupException`.
- Happy path: valid IP/port → returns a listening socket (dispose it).
- The `stopping`/NLog restructure is process-global lifecycle; verify by code review, do **not** call `GameServer.Stop()` from tests (it would shut down NLog for the whole test process).

Red phase: the `FatalStartupException` tests fail (today `Start` throws raw `FormatException`/`SocketException`, or `IPAddress.Parse` failure surfaces differently) before the fix.

**Commit:** `Fix H7: crash restart no longer skips saves/logging; fatal bind errors exit instead of restart-looping`

---

### Task 3: H2 — send path silently drops whole packets when the TCP send buffer is full

**Problem:** `Player.Send(string)` calls `sock.Send(bytes)` on a non-blocking socket; when the OS buffer is full the call **throws** `SocketException` (would-block) instead of returning a partial count, and `GameWorld.Send` swallows it → the whole packet is lost → permanent client desync.

**Changes:**

1. `Goose/Player.cs`:
   - Add `public const int MaxSendBufferSize = 1024 * 1024;` (1 MiB).
   - Change `public virtual void Send(string data)` to `public virtual bool Send(string data)`. Inside the existing `lock (socketLock)`: on `SocketException` from `this.sock.Send(bytes)`, append the **entire** `bytes` to `SendBuffer` (initialize `SendBuffer` if null — it is only created in `OnLogin()`). After the lock, if `SendBuffer != null && SendBuffer.Count > MaxSendBufferSize` return `false`; otherwise `true`. Keep the existing partial-send branch. Do not change the `Send()` flush method.
2. `Goose/GameWorld.cs` `Send(Player, string)` (the only caller): if `player.Send(data)` returns `false`, log a warning (name + "send buffer exceeded") and call `this.LostConnection(player.Sock)` instead of silently continuing. Keep the existing try/catch for other exceptions.
3. `Goose/GameServer.cs`: initialize `sockets`, `connections`, `connectionsPerIP` in the constructor (e.g. field initializers `= new();` in addition to the re-assignment in `Run()`). Without this, `Disconnect`/`DropSocket` NRE on an unstarted server, which `LostConnection`'s swallow-all catch hides — and which the tests below need.

**Tests** (`Goose.Tests/PlayerSendTests.cs`):
- **Adversarial (the H2 bug):** `var p = new Player(0); p.OnLogin(); p.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false };` — an unconnected socket makes `Send` throw `SocketException` deterministically. `p.Send("ABC\x1")` must not throw and must leave the **complete** payload in `p.SendBuffer` (assert count + bytes) and return `true` (below cap). Before the fix this throws.
- Cap: prefill `p.SendBuffer` (public getter) near `MaxSendBufferSize`, then `p.Send(...)` on the throwing socket → returns `false`.
- Happy path: real loopback pair (listener on `127.0.0.1:0`, accepted socket set non-blocking) → `p.Send("HI\x1")` → peer reads exactly `"HI\x1"`.
- `GameWorld.Send` overflow path: with a player whose `SendBuffer` is already over the cap and a throwing socket, `world.Send(player, "x")` must drop the connection (assert via `internal int Count` on `EventHandler` that a `LogoutEvent` got enqueued, or that the socket was closed).

**Commit:** `Fix H2: buffer full payload when a send would block and drop connections whose send buffer overflows`

---

### Task 4: H1 — pre-login packets are never reassembled across TCP segments

**Problem:** for sockets without a `Player` yet, `GameWorld.Received` hands each raw chunk straight to a fresh `LoginEvent`. Both login formats require the whole packet in one segment; a split login is silently dropped twice and the client hangs.

**Changes (all in `Goose/GameWorld.cs`):**

1. Add `private readonly Dictionary<Socket, StringBuilder> preLoginBuffers = new();` and `private const int MaxPreLoginBufferSize = 4096;` (a complete login is < 200 bytes; the cap bounds a stalled/attacking pre-login socket).
2. In `Received(Socket sock, string data)`, replace the `player == null` branch:
   - If `PlayerHandler.GetPlayer(sock) != null` (the player branch), also `preLoginBuffers.Remove(sock)` — cleanup once login completes.
   - Else: get-or-create the buffer, `Append(data)`. If `buffer.Length > MaxPreLoginBufferSize` → log warn, `preLoginBuffers.Remove(sock)`, `this.LostConnection(sock)`, return.
   - Completion check on `string s = buffer.ToString()`: complete when `(s.StartsWith("LOGIN", StringComparison.Ordinal) && s.IndexOf(',') >= 0) || s.Length >= 71` (71 = 2 header bytes + 69, the minimum Illutia login; `LoginEvent` re-validates).
   - When complete: `preLoginBuffers.Remove(sock)` and enqueue the `LoginEvent` with `ev.Data = new object[] { sock, s }` (as today, but the *assembled* string).
3. In `LostConnection(Socket sock)` (GameWorld.cs:467): `preLoginBuffers.Remove(sock);` so a dropped pre-login socket can't leak its buffer.
4. Test seam: `internal string PreLoginPending(Socket sock)` → buffered string or null. (`InternalsVisibleTo` is already configured.)

Leave `LoginEvent`'s own validation untouched (defense in depth — it already handles short/malformed packets).

**Tests** (`Goose.Tests/PreLoginReassemblyTests.cs`) with `new GameWorld(new GameServer())` and unconnected test sockets:
- **Adversarial (the H1 bug):** split classic login `"LOGINabcd,passw0rd,ALPHA33,3.5.2"` into two `Received` calls at an arbitrary point (e.g. after `"LOGINa"`). After chunk 1: `PreLoginPending(sock)` returns the partial string and no `LoginEvent` was enqueued (use the `internal int Count` accessor from Task 3, or add `internal int Count` on `EventHandler` if Task 3 didn't). After chunk 2: `PreLoginPending(sock) == null` and exactly one `LoginEvent` was enqueued. Before the fix, chunk 1 already dispatches a (garbage) `LoginEvent`.
- Illutia: a 71-char payload delivered in three chunks → no event until the third, then exactly one.
- Cap: a pre-login socket fed ~4.2KB of non-LOGIN data → buffer removed, connection dropped (assert `PreLoginPending(sock) == null`; assert socket closed or a `LogoutEvent` enqueued).
- Cleanup: after `preLoginBuffers` has data for a socket, `LostConnection(sock)` removes it.

**Commit:** `Fix H1: reassemble pre-login data across TCP segments before dispatching LoginEvent`

---

### Task 5: H3 — auto-created character names have no maximum length

**Change:** in `Goose/Events/LoginEvent.cs` auto-creation block (~:144-156), reject names longer than 16 characters before `new Player(0)`:
`if (name.Length < 3 || name.Length > 16) { world.SendRaw(sock, P.LoginDenied("Character name must be 3-16 letters.")); world.GameServer.Disconnect(sock); return; }`
This replaces/supersedes the earlier `< 3` rejection only in scope for auto-creation (the generic short-name check at :105-110 stays). 16 matches the client's 16-byte login name field. Letters-only check stays where it is. Existing (already-created) over-long names are out of scope.

**Tests** (`Goose.Tests/LoginEventNameLengthTests.cs`): loopback socket pair + `new GameWorld(new GameServer())`; set `GameWorld.Settings.AutoCharacterCreation = true` (save/restore). Enqueue `LoginEvent` with `Data = new object[] { serverSock, "LOGIN" + 17 letters + ",passw0rd,ALPHA33,3.5.2" }`, run `world.EventHandler.Update(world)`. Assert: no player was added to the handler (`world.PlayerHandler.GetPlayerFromData(name) == null` / count unchanged), the client side received a denial (`LNO…`), and the server socket was closed/disconnected. Also a 16-letter name is accepted (player created). Before the fix the 17-letter name creates a character.

**Commit:** `Fix H3: cap auto-created character names at 16 characters`

---

### Task 6: H4 — unbounded chat/tell/auction length (cap at 300; no log-buffer cap)

**Scope decision (user):** cap message length at 300 characters in the chat, tell, and auction handlers. Do **not** cap or rate-limit `LogHandler`'s in-memory buffer in this task.

**Changes:**
1. `Goose/Events/ChatEvent.cs`: right after extracting the message (`message = message.Substring(1, ...)`), `if (message.Length > 300) return;` (silent drop — same treatment as other invalid packets; the 1-char guard stays).
2. `Goose/Events/TellEvent.cs`: after computing `message`, drop if `message.Length > 300` (no send, no log).
3. `Goose/Events/AuctionCommandEvent.cs`: drop if `data.Length > 300` after `Substring(9)`.
   Use one named constant if it keeps the three sites readable (e.g. `internal const int MaxMessageLength = 300;` on a shared spot or per-class private const — implementer's call; keep it simple).

**Tests** (`Goose.Tests/ChatMessageLengthTests.cs`): build `new GameWorld(new GameServer())`, a `new Map()` with `CanChat = true` (and settable `Muted = false`), a player with `State = Player.States.Ready`, `player.Map = map`, minimal identity fields. Assert via `world.LogHandler.Pending`:
- chat of exactly 300 chars (packet `";" + msg`) → one `Log.Types.Chat` entry pending.
- chat of 301 chars → **no** entry (before the fix: entry present — adversarial).
- tell 300 → logged; tell 301 → not logged, and recipient (second player on the same map if easy, else omit delivery assertion) gets nothing.
- auction packet (`"SAAUCT..."`? — use the real 9-char command prefix the event `Substring(9)`s; verify the prefix against `EventHandler`'s trie registration for the auction command) 300 → logged; 301 → not logged.

**Commit:** `Fix H4: cap chat/tell/auction message length at 300 characters`

---

### Task 7: H8 — first-save flag cleared before COMMIT can permanently lose a new character/pet row

**Problem:** `Player.SaveToDatabase`'s INSERT work item clears `AutoCreatedNotSaved` the moment the INSERT *executes*; if a later statement in the same transaction fails, the rollback undoes the INSERT but the flag is already false → every future save issues an UPDATE matching 0 rows → the character is never persisted. Same pattern in `Pet.BuildSave` (Pet.cs:377).

**Changes:**

1. `Goose/Database.cs`: extend `EnqueueTransaction` to `public void EnqueueTransaction(Action<SQLiteConnection> action, Action onCommit = null)` — invoke `onCommit` **after** `RunSql(conn, "COMMIT;")` succeeds (not after ROLLBACK; if COMMIT itself throws, `onCommit` must not run — the existing rethrow path handles that).
2. `Goose/Player.cs` `SaveToDatabase`: capture `bool isNew = this.AutoCreatedNotSaved;` on the game thread at build time. Remove the `this.AutoCreatedNotSaved = false;` line from the INSERT work item. Pass `isNew ? () => this.AutoCreatedNotSaved = false : null` as `onCommit` to `EnqueueTransaction`. (Runs on the DB thread, same as today's in-work-item clear — no new reader/writer pair.)
3. `Goose/Pet.cs` `BuildSave`: remove the `this.AutoCreatedNotSaved = false;` from the INSERT work item. In `Player.SaveToDatabase`, capture each pet's `AutoCreatedNotSaved` when building `work` and clear any that were true in the same `onCommit` lambda (pets are always saved inside the owner's transaction).
   - Note: `SpellEffect.cs:922` calls a standalone `newpet.SaveToDatabase(world)` (finding M13, out of scope). Check that call site: if it relies on the pet's flag being cleared by the work item itself, it must pass its own `onCommit` (or a one-item `EnqueueTransaction` with the clear). Verify and adapt so pet creation still marks the pet saved.

**Tests:**
- `Goose.Tests/DatabaseTransactionTests.cs`: temp SQLite DB via `new Database(); db.Start(path)`; (a) `EnqueueTransaction(conn => { insert row; throw new Exception(); }, () => committed = true)` → row absent (rolled back) **and** `committed == false` (adversarial: fails on any pre-commit clear); (b) success → row present and `committed == true`. Dispose `db` (Stop) after.
- Player-level (extend the `PlayerPropertiesPersistenceTests` pattern with a real `Database`): fresh player with `AutoCreatedNotSaved = true`, `SaveToDatabase(world)` against a temp DB with the shipped `players.sql` → after the queue drains, the players row exists and `AutoCreatedNotSaved == false`.
- If a practical way exists to fail a later statement in the player transaction (e.g. a second table missing a column) to assert the flag stays true after rollback, add it; otherwise state in the report that the rollback case is covered by the Database-level test.

**Commit:** `Fix H8: clear first-save flags only after the save transaction commits`

---

### Task 8: H9 — guild writes are auto-commit, synchronous on the game thread, outside the player-save transaction

**Problem:** `Player.SaveToDatabase` sync-calls `Guild.Save(world)` (`Database.Execute`, blocks the game thread per round-trip) *before* the player's transaction. A crash between the guild commit and the player-row commit leaves `guild_members` rows without the matching `players.guild_id`; the re-join then hits the composite PK on every 300s `GuildSaveEvent` until fixed by hand.

**Changes:**

1. `Goose/Guild.cs`: add `public Action<SQLiteConnection> BuildSave()` mirroring the `Pet.BuildSave`/`Inventory.BuildSave` pattern:
   - On the game thread, snapshot `Name`, `MOTD`, and the dirty members (`List<(int PlayerID, GuildRanks Rank)>` for `status.Dirty` entries, plus whether `this.ID == 0`).
   - The returned callback (DB thread, inside the caller's transaction): if new → `INSERT INTO guilds (guild_name, guild_motd) VALUES (@name, @motd)` then `SELECT last_insert_rowid()` → `this.ID`, and set `player.GuildID = this.ID` for every `OnlineMembers` entry (same as today's sync code); otherwise → `UPDATE guilds SET … WHERE guild_id=…`.
   - Member changes become **idempotent upserts**: `GuildRanks.Deleted` → `DELETE FROM guild_members WHERE guild_id=… AND player_id=…`; otherwise `INSERT INTO guild_members (guild_id, player_id, guild_rank) VALUES (…,…,…) ON CONFLICT(guild_id, player_id) DO UPDATE SET guild_rank=…` (composite PK confirmed in `sql/guilds.sql` — the `JustAdded` branch is no longer needed for correctness; keep the flags' existing bookkeeping as-is, clearing them at the end of the callback like today).
   - `Save(GameWorld)` (used by `GuildHandler.Save` on the 300s cadence) keeps working: implement it as `world.Database.EnqueueTransaction(this.BuildSave())` so both paths share one idempotent code path and no guild save blocks the game thread. (The `newguilds` registration `this.guilds[guild.ID] = guild` in `GuildHandler.Save` must happen only once the ID is known — move it into the `Enqueue` `onComplete` callback, or keep the sync `Save` for newguilds only. Implementer: pick the minimal variant and note the choice; the correctness requirement is that no game-thread code reads `guild.ID` before the INSERT has committed.)
2. `Goose/Player.cs` `SaveToDatabase`:
   - Replace `if (this.GuildID == 0 && this.Guild != null) this.Guild.Save(world);` with: `if (this.Guild != null && (this.GuildID == 0 || this.Guild.Dirty)) work.Add(0, this.Guild.BuildSave());` — the guild work item must run **first** in the transaction, before the player row.
   - The player row must persist the guild ID assigned by that first work item: in `BuildInsertQuery`/`BuildUpdateQuery` replace the baked-in `this.GuildID` literal with a `@guildId` parameter, and in `BuildInsertCommand`/`BuildUpdateCommand` (which run on the DB thread inside the work-item lambda) bind `this.GuildID` **at call time** — i.e. add a `int guildId` parameter to both builders and pass `this.GuildID` from inside the `savePlayerRow` lambda (read on the DB thread after the guild work item ran). All other scalars keep their game-thread snapshot semantics; update the doc comments on those four methods only where they'd be wrong.
   - Update the internal call sites in `Goose.Tests/PlayerPropertiesPersistenceTests.cs` (and any other `BuildInsertCommand`/`BuildUpdateCommand` callers the compiler finds).
3. Invariants: one player save = one transaction containing guild row + guild_members + players row + inventory/etc.; a mid-transaction failure rolls back all of it; member upserts are safe to re-run (rollback retry or double-save).

**Tests** (`Goose.Tests/GuildSaveTests.cs`, temp DB with shipped `players.sql` + `guilds.sql`):
- New guild through the player path: player (`new Player(0)`, `AutoCreatedNotSaved = true`, minimal NOT NULL fields) with `Guild = <new Guild, ID 0>` and the player added to `guild.Members` as a dirty/JustAdded member (check `Guild`/`GuildMemberStatus` construction APIs in `Guild.cs` and mirror how `Events/GuildCreateCommandEvent.cs` builds it). `player.SaveToDatabase(world)` with a real `Database`; drain the queue. Assert: `guilds` has exactly 1 row; `guild_members` has the (guild_id, player_id) row; `players.guild_id` equals that guild id (adversarial: before the fix the players row has `guild_id=0` or the guild save ran outside the transaction).
- Idempotency: re-run `guild.BuildSave()`'s callback (or the second save) without clearing dirty flags → no PK violation, same rows (the exact failure that today re-throws on every `GuildSaveEvent`).

**Commit:** `Fix H9: persist guilds inside the player-save transaction with idempotent member upserts`

---

## Final pass (after all tasks)

- `dotnet build Goose.sln` clean; `dotnet test Goose.Tests` all green (baseline 323 + new tests).
- Update `docs/code-review-2026-08-15.md`: mark H1, H2, H3, H4, H6, H7, H8, H9 as FIXED with commit hashes (leave H5, H10 open).
- Full-implementation code review, then finish the branch (PR to master).
