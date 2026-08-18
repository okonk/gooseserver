# Goose Server — Code Review Round 2

**Date:** 2026-08-15
**Scope:** `Goose/` (server core, ~27k LOC)
**Supersedes/follows:** `docs/code-review-2026-07-25.md` (all of its findings were fixed in-tree; this review re-examined the whole server and found new issues)
**Build status:** succeeds, 0 errors, 1 warning (CA1416 in `Tools.Tests/BundleStageTests.cs` — `File.SetUnixFileMode` unreachable on Windows)
**Test status:** 411/411 pass (`Goose.Tests`) — *corrected 2026-08-18: the 323 figure was wrong; the baseline at the start of the fix work was 411.*

**Method:** 5 sequential focused review passes (networking/protocol, database layer, inventory/commerce, combat/quests/buffs/pets, scripting/console/startup). Every HIGH+ finding was re-verified by direct code trace before inclusion. One false positive was rejected (see "Rejected").

---

## Summary

Overall code quality is high: extensive doc comments, deliberate error containment (per-event, per-socket catches, crash backstop), connection throttling, and a real test suite. Bugs concentrate in a few systemic patterns:

1. **Missing identity invariants** on slot operations (the CRITICAL dupe).
2. **Unvalidated config intervals** vs. the `EventHandler.Update` "always schedule in the future" invariant (hard-freeze on `=0`).
3. **Fragile crash-restart lifecycle** (skipped saves on next shutdown, dead logging, no fast-fail).
4. **A handful of missing bounds on untrusted input** (login reassembly, name length, chat length).
5. **Scripts run in-process on the game thread with no watchdog.**

**Priority order:** 1) CRITICAL dupe (one-line fix, live exploit) → 2) liveness batch (freeze / data-loss / packet-loss) → 3) untrusted-input bounds → 4) data-integrity/transactionality → 5) scripting hardening (design work).

---

## CRITICAL

### C1. `CHANGE n,n` dupes any stackable item (player-reachable, infinite gold) ✅ verified — ✅ FIXED (commit d7c38cc, branch fix/c1-change-swap-dup: same-slot guards in `Inventory.SwapSlots` + `ItemSlot.SwapSlots`, regression tests in `Goose.Tests/InventoryChangeSlotTests.cs`). Ops note: accounts exploited before this fix still hold duped stacks.

**Files:** `Goose/Events/InventoryChangeSlotEvent.cs:36`, `Goose/Inventory.cs:193-203`, `Goose/ItemSlot.cs:55-59`

The `CHANGE` (change slot) packet validates `id1`/`id2` against range only — it allows `id1 == id2`. With equal ids, `Inventory.SwapSlots` fetches the **same `ItemSlot` object** for both parameters:

```csharp
// Inventory.cs:193
ItemSlot fromSlot = this.GetSlot(fromSlotId);   // same object when id1 == id2
ItemSlot toSlot   = this.GetSlot(toSlotId);
ItemSlot.SwapSlots(ref fromSlot, ref toSlot);   // ItemSlot.cs:55
// ItemSlot.cs:57-58 — from.Item == to.Item (same object):
else if (from.Item.TemplateID == to.Item.TemplateID && to.CanStack(from))
{ to.Stack += from.Stack; from = null; }
// Inventory.cs:200-201
this.SetSlot(fromSlotId, fromSlot);             // inventory[n] = null
this.SetSlot(toSlotId, toSlot);                 // inventory[n] = same object, DOUBLED
```

`to.CanStack(from)` is `CanStack(self, self.Stack)` and passes whenever `2*Stack <= StackSize` (or `StackSize == 0`). The stack doubles in place; `from = null` nulls only the local.

**Exploit loop:** double until `2S > StackSize` → `SPLIT` in half → double both halves. Total roughly doubles per cycle, forever. Any template with `StackSize >= 2` and `item_value > 0` converts to gold via vendor sell. `CHANGE` is an Open (all-player) packet.

**Fix:** `if (fromSlotId == toSlotId) return;` at the top of `Inventory.SwapSlots`. Optionally also `if (from == to) return;` at the top of `ItemSlot.SwapSlots` as defense in depth. Add a regression test: send `CHANGE n,n`, assert stack unchanged.

---

## HIGH

### H1. Pre-login packets are never reassembled across TCP segments ✅ verified — ✅ FIXED (branch `fix/code-review-highs`, commits f77dbb6, 05d01fb, c57d89f: per-socket pre-login buffer with 4096 cap and drop-on-oversize in `GameWorld.Received`; classic login dispatches only once name **and** password fields are complete, Illutia at ≥71 chars; buffer cleaned up on login and `LostConnection`; regression tests in `Goose.Tests/PreLoginReassemblyTests.cs`).

**Files:** `Goose/GameWorld.cs:498-521` (`Received`), `Goose/Events/LoginEvent.cs:56-95`

For a socket with no `Player` yet, `Received` hands the raw 8KB chunk straight to a fresh `LoginEvent`; there is no pre-login buffer. Both login formats require the *entire* packet in one segment:

- classic: `packet.StartsWith("LOGIN")` **and** `IndexOf(',')` in the same chunk (`LoginEvent.cs:58-66`)
- Illutia: `data.Length >= 69` in one chunk (`LoginEvent.cs:82`)

If an 81+-byte login is split (small packets do fragment on lossy/congested links), the first chunk returns **silently with no reply**, and the second chunk is decoded as garbage and dropped too — the client hangs or retries forever. Realistic on any non-LAN link.

**Fix:** keep a small per-socket pre-login `StringBuilder` (bound it like `MaxReceiveBufferSize`) in `GameWorld`, and dispatch a `LoginEvent` only once a complete packet is present.

### H2. Send path silently drops whole packets when the TCP send buffer is full ✅ verified — ✅ FIXED (branch `fix/code-review-highs`, commits aa50d7c, a017fdf, 8ca68c5: would-block sends buffer the entire payload into `Player.SendBuffer` (`Send(string)` returns bool); 1 MiB cap with connection drop in `GameWorld.Send`; new sends are held while the buffer has pending bytes so packets can't be reordered; regression tests in `Goose.Tests/PlayerSendTests.cs`).

**Files:** `Goose/Player.cs:2389-2402` (`Send(string)`), `Goose/GameWorld.cs:591-602` (`Send`)

`Player.Send(string)` calls `this.sock.Send(bytes)` on a **non-blocking** socket (set at accept, `GameServer.cs:196`). When the OS buffer is full, `Send` throws `SocketException` (WOULDBLOCK/EAGAIN); the `bytesSent != bytes.Length` branch only handles the *partial* case. `GameWorld.Send` swallows the exception with an empty `catch { }` (`GameWorld.cs:598-601`), so the packet is lost: `SendBuffer` never receives it, the `writeList` flush has nothing to send, and the client desyncs permanently (missing MOC/position/MKC data). Worse, a client that keeps PONGing but never reads passes the ping timeout and lives forever as a zombie accumulating invisible losses. (The separate `Send()` flush method is safe: its exception is caught in `GameServer`'s write loop and drops the connection — acceptable.)

**Fix:** catch the would-block exception in `Player.Send` and append the *entire* byte array to `SendBuffer`; bound `SendBuffer` (e.g. 1–2 MB) and drop the connection when exceeded.

### H3. Auto-created character names have no maximum length ✅ verified — ✅ FIXED (branch `fix/code-review-highs`, commit 7bf84b3: auto-creation rejects names longer than 16 letters with a denial + disconnect, matching the client's 16-byte login field; regression tests in `Goose.Tests/LoginEventNameLengthTests.cs`).

**Files:** `Goose/Events/LoginEvent.cs:131-156`, `Goose/sql/players.sql`

Only `name.Length < 3` and a letters-only check exist; there is no upper bound. `player_name` is unbounded SQLite TEXT, so a ~60KB name of letters creates a valid character. That name is embedded in every chat broadcast (`P.Chat(loginId, player.Name, message)`, `Packets.cs:62`) and in each per-chat DB log row — combines with H4 into large amplification, and likely overflows client name fields.

**Fix:** cap name length at creation (e.g. 16, matching the client's 16-byte login field).

### H4. Unbounded chat/auction/tell length + unbounded in-memory log buffer = memory DoS from one client — ✅ PARTIALLY FIXED (branch `fix/code-review-highs`, commit c55f507: chat/tell/auction payloads capped at 300 chars with silent drop before any log/broadcast; regression tests in `Goose.Tests/ChatMessageLengthTests.cs`). Scope decision 2026-08-18: the `LogHandler` in-memory buffer cap / chat rate-limit are **not** done — left open.

**Files:** `Goose/Events/ChatEvent.cs:27-40`, `Goose/Events/AuctionCommandEvent.cs:33-39`, `Goose/LogHandler.cs:11-26`, `Goose/GameWorld.cs:348`

A chat packet is `";"` + up to the 64KB receive-buffer cap; there is no length check on `message` (only `message.Length == 1` at `ChatEvent.cs:33`). Every chat/auction call does `world.LogHandler.Log(...)`, and `LogHandler.Save` runs only on the `IdleTimeout` cadence (default 600s); each log becomes its own item on the **unbounded** `BlockingCollection` DB queue (`Database.cs:20`). One auto-created character can push tens of thousands of up-to-64KB strings per flush window (multi-GB) plus unbounded DB-queue backlog, and broadcasts 64KB to every player in range per message.

**Fix:** cap message length (e.g. 256 chars) in the chat/tell/auction handlers, rate-limit chat per player, and cap `LogHandler.logs` (drop or flush-early when full).

### H5. Repeatable-quest progress is unbounded → repeatable (or infinite) reward claims ✅ verified

**Files:** `Goose/Player.cs:1093` (`UpdatePossibleQuestProgress`), `Goose/Quests/QuestWindow.cs:341` (`CompleteQuest`), `:524` (`TakeRequirements`)

`progress.Value++` has no cap at `requirement.Value2`. On complete, `TakeRequirements` decrements only when `!requirement.KeepRequirement`: `progress.Value = Math.Max(0, progress.Value - requirement.Value2)`, while `PlayerMeetsRequirements` passes on `p.Value >= p.Requirement.Value2`.

Trace: kill 100 mobs of a repeatable quest requiring 10 → complete (rewards) → progress 90 → re-open window (allowed for repeatable) → complete 9 more times with zero further kills. Worse: with `keep_requirement=1`, progress is never decremented → every re-visit completes instantly, forever.

**Fix:** cap progress at `Value2` on increment, and always reset kill/talk progress to 0 in `CompleteQuest` for repeatable quests.

### H6. Zero/negative interval settings → infinite loop that hard-freezes the game thread ✅ verified — ✅ FIXED (branch `fix/code-review-highs`, commits a3eb769, bbd1897, 3ac4ec9: every self-rescheduling site clamps its interval to ≥1 period, incl. `GuildSavePeriod`/`RegenSpeed` caught during review; regression tests in `Goose.Tests/EventHandlerIntervalTests.cs` et al. Note: `CreditsUpdateEvent` reschedules from unclamped `CreditUpdateInterval` but is dead code — registration is commented out at `GameWorld.cs:~352`; latent, not fixed).

**Files:** `Goose/EventHandler.cs:361-377` + reschedule sites

`EventHandler.Update` drains a `PriorityQueue` with `while (tick <= now)`, and its own doc comment (lines 361-364) warns a self-re-enqueueing event "at or before now … would be re-processed in this same loop". Several recurring events compute their re-enqueue tick from **unvalidated JSON settings**, so a 0 or negative value re-enqueues at `now` on every pass → the while-loop never exits, the server hangs with no log, and `RequestShutdown` (flag-only, `GameServer.cs:327`) cannot break in. Confirmed paths:

| Setting | Sites | Effect at 0 |
|---|---|---|
| `IdleTimeout` | `GameWorld.cs:348`, `Events/PlayerCountExperienceModifierUpdateEvent.cs:50` | freezes on the **first tick after boot** |
| `ItemGroundSweepTime` | `MapHandler.cs:81`, `Events/ClearMapItemsEvent.cs:33` | same-tick spin, sweeping all ground items every iteration |
| `SpellEffectPeriod` | `Events/BuffTickEvent.cs:77`, `NPC.cs:1532`, `Player.cs:2140` | spins while any buff exists, re-running tick spells each pass |
| `PlayerSavePeriod` | `Player.cs:1897`, `Events/PlayerSaveEvent.cs:14` | spins on first PONG, hammering the DB; `Player.cs:1887` also disconnects on every ping |
| (script) `TimeSpan.Zero` | `Events/ScriptTimerEvent.cs:29,37` | same spin via `ScriptTimerEvent.Create/Reschedule` |

The team is already aware of this class: `PreLoginTimeoutSeconds` is clamped (`Math.Max(1, …)` in `SweepPreLoginConnections`) — nothing else is.

**Fix:** clamp every interval to ≥1 at the reschedule sites, or centrally in `EventHandler.AddEvent` / settings validation at startup.

### H7. After any crash, the next clean shutdown silently skips player saves and DB drain ✅ verified — ✅ FIXED (branch `fix/code-review-highs`, commit 01309f0: crash path now calls the new `StopWorld()` (no `stopping` flag, no NLog shutdown) so the next signal runs the full `Stop()` with saves + DB drain; bind/IP-parse failures throw `FatalStartupException` and exit with code 1 instead of restart-looping; crashlog write guarded; startup tests in `Goose.Tests/GameServerStartupTests.cs`).

**Files:** `Goose/GameServer.cs:60` (`stopping`), `:107` (crash path calls `Stop()`), `:274` (`if (!stopping) this.Stop();`), `:288` (`stopping = true`)

`stopping` is set only in `Stop()` and **never reset**. Crash path: `Run()`'s catch → `this.Stop()` → `stopping = true` permanently, then `Sleep(10s); continue;` restarts the world in the **same** `GameServer` instance. On the *next* Ctrl+C/SIGTERM: `RequestShutdown` → `GameLoop` exits → `if (!stopping) this.Stop();` is **skipped** → `GameWorld.Stop()` (player saves + `Database.PendingCount` drain, `GameWorld.cs:395-440`) never runs → process exits with up to `PlayerSavePeriod` (default 180s) of unsaved progress discarded — the exact loss the signal-handler comment in `Program.cs:40-47` says was fixed.

**Related crash-handler defects (same block):**
- The crash path calls `NLog.LogManager.Shutdown()` (`GameServer.cs:296`) and nothing reloads `NLog.config` afterward → **all logging is silently dead after the first crash**.
- `Run()`'s restart loop never fast-fails on persistent startup errors: port in use, or `IPAddress.Parse` failure on a bad `GameServerIP` (`GameServer.cs:134`), spins forever — full world reload + "Crashed:" dump every 10s, no exit.
- The crashlog write (`GameServer.cs:101`) is unguarded: if the data dir is unwritable (container without a volume), the throw inside the catch replaces the original exception and the process dies unhandled with a misleading IO error.

**Fix:** reset `stopping = false` when the restart loop creates a new world (or make the flag per-iteration); do not `LogManager.Shutdown()` on the restart path; treat bind/parse failures as fatal config errors (clear message, exit); wrap the crashlog write in try/catch.

### H8. First-save flag cleared before COMMIT; mid-transaction failure permanently loses a new character/pet row ✅ verified — ✅ FIXED (branch `fix/code-review-highs`, commit e513162: `Database.EnqueueTransaction` gained an `onCommit` hook that runs only after COMMIT succeeds; player + pet `AutoCreatedNotSaved` flags clear in `onCommit`; the standalone pet-save path (tame spell) uses its own one-item transaction; rollback tests at Database and player level in `Goose.Tests/DatabaseTransactionTests.cs` / `PlayerFirstSaveTests.cs`).

**Files:** `Goose/Player.cs:857-864`, `Goose/Pet.cs:371-378`

```csharp
savePlayerRow = conn =>
{
    using var command = BuildInsertCommand(conn, insertQuery, ...);
    command.ExecuteNonQuery();
    // Only clear after a successful insert so a failed first save can retry INSERT.
    this.AutoCreatedNotSaved = false;   // Player.cs:863 — runs on the DB thread, inside the txn
};
```

`SaveToDatabase` runs the INSERT together with inventory/spellbook/bank/quest/pet upserts in one `EnqueueTransaction`. The flag is cleared as soon as the INSERT **executes** — not after COMMIT. If any later statement fails (constraint violation, disk error, failed COMMIT), the rollback undoes the INSERT, but `AutoCreatedNotSaved` is already `false` → every subsequent save builds an `UPDATE players … WHERE player_id=N` that matches 0 rows → the character exists in memory but is **never persisted**; on restart the account is gone while its inventory/bank upserts remain as orphans. The flag is also mutated on the DB thread while the game thread reads it (secondary race). Same pattern for pets.

**Fix:** use `INSERT … ON CONFLICT(player_id) DO UPDATE`, or clear the flag only in a post-commit step.

### H9. Guild writes are auto-commit, synchronous on the game thread, outside the player-save transaction ✅ verified — ✅ FIXED (branch `fix/code-review-highs`, commits fcf0083, b80a203, a6f037e, 9b7664f, 4c9ca5d: `Guild.BuildSave()` returns a pure-SQL in-transaction callback (idempotent `ON CONFLICT(guild_id, player_id) DO UPDATE` member upserts) + a post-commit in-memory transition action; the guild work item runs first inside the player-save transaction and the players row binds the in-transaction assigned ID; 300s cadence path is async; all in-memory guild transitions deferred to post-COMMIT so rollback leaves memory untouched; regression tests in `Goose.Tests/GuildSaveTests.cs`. Residual (accepted, plan-sanctioned): brand-new guilds registered via `GuildHandler.Save` keep the synchronous save — a crash between that commit and the player-row commit leaves a transient desync that self-heals on the next save (idempotent upserts).)

**Files:** `Goose/Player.cs:851`, `Goose/Guild.cs:181-265`, `Goose/GuildHandler.cs:91-103`

`SaveToDatabase` calls `this.Guild.Save(world)` (sync `Database.Execute`) *before* enqueuing the player transaction. Three problems:

1. The guild row + `guild_members` rows commit independently of the players-row transaction. A crash between them leaves `guild_members` committed with `players.guild_id=0`; on re-join `INSERT INTO guild_members (guild_id, player_id, …)` (`Guild.cs:239`) hits the composite PK and throws; `this.Dirty = false;` (`Guild.cs:265`) is skipped → the guild re-fails and re-throws on **every** 300s `GuildSaveEvent` until the DB is fixed by hand.
2. `Execute` blocks the game thread per dirty guild, serially — the whole tick (and every previously queued save) stalls for each round-trip.
3. If it throws during `LogoutEvent`, the EventHandler backstop aborts the rest of logout (player left on map, not removed from the handler).

**Fix:** fold guild persistence into the player-save transaction (or at least `EnqueueTransaction` it immediately after the player row), and make member upserts idempotent (`ON CONFLICT … DO UPDATE`).

### H10. Scripts run in-process on the game thread with zero isolation, timeout, or resource limit

**Files:** `Goose/Scripting/Script.cs:33-48` + all hook call sites

`.csx` files are compiled with the full BCL plus the whole server assembly referenced; scripts can (and do) use `File`, `Process`, reflection. Hooks are invoked **synchronously on the game thread** inside `EventHandler.Update` with nothing that can preempt a loop:

- `NPC.cs:371` — `OnMoveEvent`, fires on **every step of every NPC** (MaxNPCs default 250000)
- `Player.cs:1153` — `OnPlayerMove`, per move packet
- `ChatEvent.cs:65/73` — per chat line (a hostile player can drive a slow/looping hook)
- `BuffTickEvent.cs:28` — per buff per `SpellEffectPeriod`

A single `while(true)` (typo included) freezes the entire server for every player; only SIGKILL recovers. Sandbox escape is low priority (operator-provided scripts), but the unbounded runtime is a pure availability hole with no watchdog.

**Fix (minimal):** per-tick wall-clock budget in `GameWorld.Update` that logs and skips overruns; longer-term, run hooks under a watchdog/thread-interrupt or cap hook runtime.

---

## MEDIUM

| # | Finding | Files | Status / notes |
|---|---------|--------|----------------|
| M1 | `Socket.Select` timeout of 2000ms stalls the entire game update when no socket I/O is pending (ticks lag up to 2s, run at 0.5Hz on quiet populations; contradicts the "every 5ms" doc comment) | `GameServer.cs:173` | ✅ verified. Fix: ~10–20ms timeout. |
| M2 | Pet respawn timer is never armed or persisted — dead pets respawn instantly; owner never notified on pet death | `Pet.cs:66,268,788`, `Events/PetSpawnCommandEvent.cs:59` | Found independently by two passes. `NextRespawnTime` read from DB and checked, but never written. Fix: set `NextRespawnTime = TimeNow + RespawnTime * TimerFrequency` on death and persist it (see also M13). |
| M3 | Melee damage can be negative → attacks heal the target (`damage -= AC*ACMult/25` unbounded below; `Attacked` does `CurrentHP -= damage` with no floor; triggered when `AC*ACMult > MaxAC`) | `Player.cs:1604-1621, 1825`; `NPC.cs:1369-1395` | ✅ verified. Fix: clamp `Math.Max(1, …)` or treat ≤0 as a miss. |
| M4 | No minimum attack/cast rate floor: `WeaponDelay=0` → `delay=0` → every ATT packet fully processed; `Aether=0` spells have no cooldown (client limited only by TCP throughput) | `Events/PlayerAttackEvent.cs:43`, `Player.cs:1965` | Fix: minimum interval, or per-player packet-rate limit in `EventHandler.AddEvent(Player, string)`. |
| M5 | Infinite `BuffTickEvent` loop for item buffs after logout: logout keeps `ItemBuff`s; the tick event's early-stop is gated on `!buff.ItemBuff`, so a logged-out player with a ticking item buff (Tick/Viral/etc.) re-enqueues forever, re-running `CastFormulaSpell` on the detached object and pinning the dead player in memory | `Events/BuffTickEvent.cs:14-19,77`, `Events/LogoutEvent.cs:83-93` | ✅ verified. Fix: early-stop also when target is logged out, or cancel pending tick events on logout. |
| M6 | `Random.Next(0, 0)` in title/surname roll when no modifier applies to the item type → throw. Quest path runs `TakeRequirements` before `GiveRewards`, so a throwing reward **consumes requirements, marks the quest complete, and drops all remaining rewards**. Vendor purchase silently fails (item not granted, no charge → that gear becomes unbuyable); NPC drops abort mid-drop | `ItemHandler.cs:334`, `Quests/QuestWindow.cs:331-342,366`, `VendorPurchaseInventoryEvent.cs:92`, `NPC.cs:1433` | Fix: `if (nextStart == 0) return null;` before the roll. |
| M7 | Null template → NRE in `RefreshStats` soft-locks login: character holding an item whose template row was removed/reloaded can never log in again (retry re-runs load, re-throws via `Database.Execute` rethrow). `ItemHandler.RefreshItemStats` also aborts mid-iteration | `Inventory.cs:920-923,942,974`, `Item.cs:292` | Fix: skip `RefreshStats` when `Template == null`; null-guard `RefreshStats`. |
| M8 | Shutdown busy-wait on `Database.PendingCount` has no timeout; `Database.Stop()`'s 2-minute cap can return with `_started` still true and the connection open | `GameWorld.cs:430-433`, `Database.cs:262-293` | Fix: call `Database.Stop()` directly (it already drains via `_loopTask.Wait`); treat its timeout as fatal for the exit path. |
| M9 | `/reloadsql` runs `Load*` reader loops on the **DB worker thread** and Roslyn-compiles scripts there — concurrent read/write of plain `Dictionary`s the game loop reads every tick, and compilation blocks the single DB queue (backs up every save/log into the unbounded queue) | `Events/ReloadSQLCommandEvent.cs:17-36`, `Database.cs:194` | GM-triggered. Fix: marshal reload through the game loop (queue an event; swap in pre-built snapshots atomically); never compile scripts on the DB thread. |
| M10 | `/reloadscripts` runs on a `Task` thread while the game loop keeps running: swaps `Script<T>.Object` mid-invocation, unsynchronized `ScriptHandler.scripts` dictionary access (can race a concurrent `/reloadsql`), global-script `OnLoaded` may touch the event heap / command trie / player list | `Events/ReloadScriptsCommandEvent.cs:17-33` (its own TODO admits "wrong thread"), `Scripting/Script.cs:48`, `ScriptHandler.cs:24-27` | Fix: marshal through the game loop, or stop-the-world + lock `ScriptHandler`. |
| M11 | Reload leaves stale script state and can partially reload: (a) `LoadGlobalScripts` skips already-loaded files so global `OnLoaded` **never re-runs** — `ScriptTimerEvent` closures keep firing the *old* script instance forever, never unregistered; (b) one failing `.csx` aborts the rest (mixed old/new code; deleted file → `FileNotFoundException`, same effect); (c) `RunAsync().Result` unbounded — top-level `while(true)` hangs the reload task and the requester never gets a reply | `ScriptHandler.cs:32-46`, `GameWorld.cs:691-699`, `Script.cs:27,45` | Fix: re-run `OnLoaded` on a tracked set (give scripts a timer-unregistration API), keep loading remaining scripts per-file, bound `RunAsync` with a timeout. |
| M12 | No UNIQUE constraint on `players.player_name`; duplicates via `/updatesql` (arbitrary GM CSV import) or manual SQL silently orphan a character: `LoadPlayerData` overwrites the name index (last row wins) and the orphaned row can't be logged into, renamed, or saved — gold/items unreachable without raw SQL. Same class (cosmetic): `guilds.guild_name` has no UNIQUE and `/guildcreate` never checks it | `sql/players.sql`, `PlayerHandler.cs:195-205,216`, `Events/GuildCreateCommandEvent.cs:36-46` | Fix: `CREATE UNIQUE INDEX ON players(lower(player_name))` (needs dedupe migration), or at minimum fail startup loudly on duplicates. |
| M13 | Pet taming can double-INSERT the same pet: standalone `newpet.SaveToDatabase(world)` (non-transactional INSERT enqueue) + the owner's next save transaction both may issue `INSERT INTO pets` if the snapshot still sees `AutoCreatedNotSaved == true` → UNIQUE violation rolls back the **entire** player save (players row, inventory, bank, spellbook, quests, all pets) until the next save period; window grows with any DB-queue backlog (M9) | `SpellEffect.cs:922`, `Player.cs:887-890`, `Pet.cs:295-297` | Fix: enqueue the pet INSERT only as part of the owner's next save, or `INSERT … ON CONFLICT(pet_id) DO UPDATE`. |
| M14 | Malformed-packet spam → unbounded NLog error volume: every untrusted parse that throws (e.g. `M1a` → `Convert.ToInt32("a")`) is contained per-event (good) but logs a full stack trace; one LAN client generates tens of thousands per second; NLog async targets buffer unbounded while disk lags. (The 10-failure world-restart backstop is effectively unreachable because `GameWorld.Update` == `EventHandler.Update`, which contains its own exceptions.) | `EventHandler.cs:374-383`, `Events/MoveEvent.cs:57` (~30 handlers use `Convert.ToInt32`) | Fix: pre-validate in the parse stage, or count malformed packets per player and log a single warning with a counter. |
| M15 | `Player.OnMeleeAttack` OnAttack procs cast on the **caster** instead of the hit target; `NPC.OnMeleeAttack` casts on the victim — one of the two is wrong (suspected; verify against intended data). On players the proc either no-ops or self-casts every proc | `Player.cs:2315` vs `NPC.cs:1628` | Fix: make both `Cast(this, hit, world)`. |
| M16 | `Quest.OnlyOnePlayerCanComplete` is loaded from the sheet but never enforced — any number of players can complete a world quest; compounds H5 | `Quests/Quest.cs:28`, `Quests/QuestWindow.cs` (absent) | Fix: track completions (global counter or NPC `ScriptStore`) and gate completion. |
| M17 | Player-count experience modifier: (a) `int / int` division — `uniquenonafkips.Count / PlayerCountExperienceModifierInterval` (default 1000) is always 0 below 1000 concurrent unique IPs, so the feature is dead on any small server; (b) `Interval=0` → `DivideByZeroException` before the re-queue → event discarded permanently; (c) reschedules on `IdleTimeout` instead of any interval setting | `Events/PlayerCountExperienceModifierUpdateEvent.cs:38,50`, `GameWorld.cs:348` | ✅ verified. Fix: `(decimal)count / Math.Max(1, interval)`; explicit clamped update interval. |
| M18 | DestroyItemEvent: unequip-merge defeats the remove → infinite `RippedCustomTicket` dupe (conditional). `Unequip` may merge the unequipped instance into an earlier same-template stack; `RemoveItem(item, n)` matches the exact instance only → finds nothing, item survives; but `wasCustom` was captured before unequip, so the ticket is still granted. Re-equip + `DITM` repeats. Trigger: equipped item whose `Description` starts `"Custom created by "` + same-template stack in an earlier slot + one free slot | `Events/DestroyItemEvent.cs:43-68`, `Inventory.cs:414,453` | Fix: grant the ticket only when the remove actually succeeded (or remove by template with a count check after unequip). |
| M19 | Every bank window has ID 21 → with two bank NPCs open, I2/W2/W3 packets route to the *first* `BankWindow` in `player.Windows` — items move into the other bank's container, client view desyncs. Server-authoritative, no value gain | `BankWindow.cs:29,40`, `Events/InventoryToWindowEvent.cs:51-56`, `WindowToWindowEvent.cs:60-71` | Fix: unique window ids (`++player.LastWindowID` as `Window.Create` does). |
| M20 | `/setaccess` mutates and saves player state from the console reader thread: `player.Access = …; player.SaveToDatabase(world)` off the game thread (gated to `NotLoggedIn` players so inventory is quiescent, but `Access` is read by the game thread concurrently) | `Console/Commands/SetAccessCommand.cs:86-96` | Fix: enqueue the change as an event. |
| M21 | `ItemModifier.ModifierAppliesToItem` max-level/max-experience checks compare the item's **MIN** values in both conjuncts → a modifier capped at MaxLevel 20 lands on a level 5–100 item (rolled titles/surnames above intended cap) | `ItemModifier.cs:55,58` | Fix: compare `item.MaxLevel` / `item.MaxExperience`. |
| M22 | `AddItem` empty-slot path ignores `StackSize` cap and accepts `stack <= 0` (merge branch is capped; all current callers pass server-controlled stacks — sheet row with bad stack yields over-stacked/zero-stack items; not player-controllable today) | `Inventory.cs:86-92` | Fix: clamp `stack` to `Item.StackSize` when `> 0`, reject `stack <= 0`. |
| M23 | Quest requirement check skips `Custom` items but consumption eats them → player's named/rolled custom items (earlier slots) are consumed instead of plain equivalents (player loss only) | `Inventory.cs:798-815` vs `:488-520`, `Quests/QuestWindow.cs:263,514` | Fix: apply the same `!Custom` predicate in both paths. |
| M24 | MP can go negative: `target.CurrentMP += mpresult` with negative formula result; setter clamps only the top → negative MP persists for the session, breaks mana checks, shows garbage vitals | `SpellEffect.cs:605`, `Player.cs:173-178` | Fix: `Math.Max(0, …)` (same for NPC). |
| M25 | PBKDF2 (210k iterations) runs on the game thread for every login (suspected tens-to-hundreds of ms each; concurrent logins serialize against the tick) | `Events/LoginEvent.cs:172`, `PasswordHasher.cs:46` | Fix: thread pool + pending-login state. Needs a runtime magnitude check. |
| M26 | Negative experience paths: macro-check penalty `Experience -= 2000000` has no floor (negative XP saved); `ExperienceBanked` requirement calls `AddExperience(-value)` which silently early-returns when over `ExperienceCap` → quest completes, player keeps the XP **and** gets the reward | `Events/MacroCheckEvent.cs:25`, `Quests/QuestWindow.cs:528`, `Player.cs:1652-1674` | Fix: floor at 0; make the XP-charge failure block completion. |
| M27 | OnMeleeAttack procs fire even when the attack is PVP-blocked (proc call precedes the PVP guard) — procs trigger against players with no actual attack on non-PVP maps | `Player.cs:1593-1600` | Fix: move the proc call after the PVP guard. |
| M28 | Other players' pets are targetable by any player-target spell (pets are in `Map.players`; `GetNPCsInRange`-style filters don't apply). Hostile effects can kill someone else's pet / heals dumpable; combined with M2 (broken pet respawn) pet-kiting is free. Probably intended PvP flavor — flag for decision | `Events/PlayerCastSpellEvent.cs`, `Pet.cs` (`Pet : Player`), `Map.cs:158-164` | Consider an ownership/`Pet` check for hostile effects. |

---

## LOW

- **L1.** `/givegold` accepts negative amounts and can drive offline balances negative (GM-only). `Events/GiveGoldCommandEvent.cs:39-45`. Reject `gold <= 0`, clamp bounds.
- **L2.** `pets.next_respawn_time` read but never written (schema column dead; respawn timers reset on restart). `Pet.cs:268` vs `:352-392`. (Same root as M2.)
- **L3.** ASCII-only wire protocol: receive `Encoding.ASCII.GetString` mangles bytes >0x7F to `?` in both directions — likely intended for this client, suspected silent corruption only if extended chars ever flow. `GameServer.cs:240`, `Player.cs:2393`.
- **L4.** Event factory cache can latch a throwing delegate: `type.GetConstructor(Type.EmptyTypes)` result dereferenced without null check; a constructor-less event type NREs on every packet of that command (latent — all current events have default ctors). `EventHandler.cs:29-35`.
- **L5.** `Random.Next(1, this.Level)` in melee damage throws at Level ≤ 1 (contained; attack no-ops). `Player.cs:1609`. `Math.Max(2, Level)`.
- **L6.** NPC double-death if a script kills the NPC inside `OnAttackedEvent` (no `State == Dead` re-check after the script hook) — defensive gap for future scripts. `NPC.cs:1047-1049`.
- **L7.** Map chat script hook `OnPlayerChatEvent` is the one map/NPC hook pair missing its try/catch (NPC call 8 lines above is wrapped). `Events/ChatEvent.cs:73` vs `:62-68`.
- **L8.** `GameWorld.Running` written from signal-handler threads without `volatile` (technically a data race; harmless on x64). `GameWorld.cs:83`, `GameServer.cs:327`.
- **L9.** Windows service mode: `OnStop` before `server` is assigned NREs; `OnStop` during the 10s crash-restart sleep → zombie server back up inside the "stopped" service process; `--datadir` silently ignored in service mode (SCM args arrive in `OnStart`, which drops them — only `GOOSE_DATADIR` works). `GooseWindowsService.cs:16-24`.
- **L10.** ChatFilter: dictionary keys stored verbatim from the `wordfilter` table but lookups lowercase — uppercase filter words never match; a duplicate row throws at load and aborts startup. `ChatFilter.cs:28,41`. Normalize keys, `TryAdd`.

---

## Rejected (false positive)

- **"Perma-root/perma-stun on respawning NPCs"** — claimed that casting Root/Stun on a *dead* NPC survives respawn because `BuffExpireEvent` early-returns for dead targets and `Spawn` doesn't clear buffs. **Rejected on trace:** (1) dead NPCs are not targetable — `Map.GetNPCsInRange` filters `State == Alive` (`Map.cs:172-181`) and the cast event only targets via that list; (2) the NPC death handler removes **all** buffs on death (`NPC.cs:1177-1188`) and `NPC.RemoveBuff` cancels the pending expiry event (`NPC.cs:1568-1576`), so no buff can survive to respawn. The `BuffExpireEvent` dead-NPC early return is defensive dead code.

---

## Areas checked and found OK (no action)

- **SQL injection:** none. Every string-built query concatenates only int/long/decimal/enum; all user-controllable strings are parameter-bound or JSON-serialized; `PasswordHash`/`Salt` are server-generated.
- **DB queue worker correctness:** per-item try/catch (one bad item neither poisons the queue nor kills the worker); `SyncWork.Done` in `finally`; re-entrant `Execute` from the DB thread runs inline (no self-deadlock); connection owned by the worker; all commands/readers `using`-disposed; WAL + 5s busy timeout configured.
- **Player save atomicity/ordering:** `SaveToDatabase` snapshots on the game thread and persists players + inventory + equipped + combinebag + bank + spellbook + pets + quests in one FIFO `EnqueueTransaction`; logout save → re-login reuses the same in-memory object, so an in-flight old save cannot overwrite newer state.
- **`ParseData` reassembly (post-login):** correct single pass, in-place `Remove(0, start)`, empty packets safely rejected by the trie, 64KB no-delimiter cap drops the connection.
- **Connection limits:** total + per-IP enforced at accept, decremented on every close path with double-remove safety; pre-login sweep drops silent pre-auth sockets; shared 8KB receive buffer safe (single-threaded).
- **LoginThrottle:** sliding window per IP + per name, lockout reset on success, `Prune` bounds memory at 4096 keys, game-thread-only.
- **SplitSlots / RemoveItem / Combine / vendor buy-sell / bank pages / drop-pickup / gold paths:** all count-conserving with bounds checks (traced in detail); vendor buy charges only on successful `AddItem`, sell refuses negative prices before the item leaves the bag; `Map.PlaceItem` tile merges are same-template + CanStack-verified.
- **Player death/respawn:** players have no lingering dead state (instant warp to bound location at 50% HP); no dead-player casting/attacking (ATT/CAST/RC/LC require `States.Ready`); no death-dupe item paths; NPC death single-shot with top-of-`Attacked` dead guard.
- **Proc recursion:** spell damage does not re-trigger melee hooks; no infinite proc chain. `EventHandler.Update` dequeues before `Ready` (self-reschedule safe); heap-safe under concurrent `AddEvent`.
- **Spellbook/cooldowns:** server-authoritative per slot, swap moves `lastcast` with the spell; invalid-target spam rate-tracked (`SuspectedMacroCount` → disconnect).
- **Console commands:** only four exist (`setaccess`, `who`, `shutdown`, `help`); the reader thread only enqueues lines — all execution happens on the game thread in `ConsoleCommandHandler.Update` with per-command try/catch; `Console.IsInputRedirected` guard prevents Docker/systemd hot read-loop.
- **Script error containment at call sites:** every NPC/map/item/buff/spell hook is wrapped (fail-soft; access gates fail closed) — a throwing script cannot take down the server (only *hang* it, see H10). The quest call sites are the exception (M6 + quest atomicity).
- **Script compilation:** all `GetScript` sites run at world load or on reload-task threads; runtime `NPC.Script`/`Item.Script` resolve to cached template fields — no first-spawn compilation on the game thread.
- **Zero-setting audit:** `LogoutLagTime=0`, `RespawnTimeBackoff=0`, `NewCharactersPerDayPerIP=0` (explicit unlimited), `RankUpdatePeriod` safe; the dangerous set is exactly H6's list. `PreLoginTimeoutSeconds` already clamped. *Audit error corrected 2026-08-18: `GuildSavePeriod=0` was claimed safe here but is NOT — `GuildSaveEvent` self-reschedules at period 0 and spins the game thread; it was caught during the H6 fix and is clamped now (commit a3eb769).*
- **Startup/shutdown basics:** `--datadir`/env parsing correct; data-dir auto-create + settings copy; DB open failures surface cleanly; load failure → clean exit; Ctrl+C/SIGTERM cancel default handlers and route through `RequestShutdown`.

---

## Suggested fix batches

1. **Batch A (do first — live exploit + liveness):** C1 (one line + regression test), H6 (clamp intervals centrally), H7 (reset `stopping`, NLog on restart path, fast-fail on bind errors, guard crashlog), H2 (send-buffer the whole payload + cap).
2. **Batch B (untrusted input):** H1 (pre-login reassembly), H3 (name cap), H4 (chat cap + log cap), M14 (malformed-packet log throttle), M4 (attack/cast floor).
3. **Batch C (data integrity):** H5 (quest progress cap/reset), H8 + M13 (INSERT ON CONFLICT), H9 (guild in transaction), M12 (unique name index + dedupe), M18/M23 (custom-item paths), M26.
4. **Batch D (scripting — design work):** H10 (tick watchdog), M9/M10 (marshal reloads through the game loop), M11 (reload semantics: OnLoaded re-run, per-file failures, timeout), M20.
5. **Batch E (small fixes):** M1 (select timeout), M3, M5, M6, M7, M8, M17, M19, M21, M22, M24, M27, L-items.
