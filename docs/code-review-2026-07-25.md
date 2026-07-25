# Goose Server — Security & Reliability Review

**Date:** 2026-07-25
**Scope:** `Goose/` (server), `CsvToSql/` (data import)
**Baseline:** `master` @ `129bf71`
**Build status:** succeeds, 0 errors, 28 warnings
**Test coverage:** none — there are no automated tests anywhere in the repository

---

## Status

Fixed in the working tree since this review was written:

| Finding | Fix |
|---|---|
| C1 — negative split dupe | `Inventory.cs` — reject `stackSize <= 0` in `SplitSlots` |
| C3 — slot index off-by-one | `ItemContainerWindow.ValidateSlotIndex` now uses `<`; `ItemContainer.GetSlot`/`SetSlot` bounds-check and log |
| C4 — MD5 password hashing | New `Goose/PasswordHasher.cs` (PBKDF2-HMAC-SHA256, 210k iterations, self-describing hash with rehash-on-login). All MD5 and `RNGCryptoServiceProvider` use removed |
| H4 — unbounded receive buffer | `GameWorld.Received` drops a connection past `MaxReceiveBufferSize` (64 KB) with no delimiter |
| H6 (partial) — unguarded send | `Player.Send()` null-guards `sock` and `SendBuffer`; `GameServer` write predicate uses `?.SendBuffer?.Count` |
| Exception isolation | `EventHandler.Update` contains per-event exceptions; `GameServer.GameLoop` wraps accept/receive/send per socket and drops only the offending connection via a new `DropSocket`. `GameWorld.Update` has a backstop that escalates to the restart path after 10 consecutive failures |

Fixed in a second round:

| Finding | Fix |
|---|---|
| H3 — crafting cost bypass | `CombinationHandler.GetMatch` requires at least the recipe quantity; `Inventory.Combine` tallies stack quantities rather than slot counts, so matching and consumption finally agree on units |
| H7 — bind laundering via split | New `Item.CloneWithoutId` copies all per-item state (`IsBound`, rolled stats, description, title/surname) instead of rebuilding from the template |
| H5 — no connection cap, no rate limiting | New `LoginThrottle` locks out by IP and by account name after repeated failures; `GameServer` enforces `MaxConnections`, `MaxConnectionsPerIP` and a pre-login timeout |
| M6 — no cross-entity transaction | New `Database.EnqueueTransaction`; each sub-save became a `BuildSave` returning composable work, and a full player save now commits as one transaction |
| M5 — no signal handling | `Console.CancelKeyPress` and a SIGTERM `PosixSignalRegistration` request a graceful stop; `GameWorld.Stop` now flushes `LogHandler`; `Console.ReadKey` is guarded behind `IsInputRedirected` |
| M1 — `/hax` ungated, and its root cause | Authorization moved into the `EventHandler` dispatch table: every entry declares Open or Restricted, so a new command cannot default to open. `/hax` and `/gmhax` now require the new `AccessPrivilege.Debug` |

**Still open and deliberately skipped:** H1, `/changepassword` requires no current password, so a hijacked live session remains a full account takeover.

Password storage changed format with **no migration path** — this was accepted because there was no production data to preserve. Any pre-existing `password_hash` values are now unverifiable and those accounts must have passwords reset. The password length cap was also raised from 10 to 16 characters (the client protocol's password field width).

Everything else below remains open.

---

## Architectural context (read this first)

One fact sets the severity for everything below: **the server is a single thread with no exception isolation.**

- `EventHandler.cs:228` calls `ev?.Ready(world)` with no `try`/`catch`.
- `GameWorld.Update()` (`GameWorld.cs:565`) has no handler.
- `GameServer.GameLoop()` (`GameServer.cs:107-164`) has no handler around `Accept()`, `NewConnection()`, `Received()`, `player.Send()`, or `Update()`.
- The only handler is `GameServer.Run()` (`GameServer.cs:40-70`), which writes `crashlog.txt`, sleeps 10 seconds, and **rebuilds the entire `GameWorld` from disk**.

Consequently, any unhandled exception originating from any one player is a **full server outage that disconnects every player** — not a dropped connection. Several bugs below are reachable with a single packet from any logged-in client, which promotes them from "crash" to "one-packet denial of service."

A second consequence: `GameServer.Stop()` calls `NLog.LogManager.Shutdown()` (`GameServer.cs:184`), and `Stop()` is invoked from the `Run()` catch block. After the first crash-restart, **logging is permanently dead for the life of the process**, so every subsequent incident is invisible.

---

## Critical

### C1. Item duplication via negative split amount

**Files:** `Goose/Inventory.cs:211-254` (`SplitSlots`), `Goose/Events/InventorySplitEvent.cs:52-59`

`InventorySplitEvent` validates `id1` and `id2` bounds but **never validates `amount`**. `SplitSlots` guards only the upper bound:

```csharp
if (stackSize > slot1.Stack) return;
```

A negative value passes.

**Exploit:**
1. Place any item in inventory slot 1; leave slot 2 empty.
2. Send `SPLIT1,2,-1000000`.
3. The `slot2 == null` branch clones the item into slot 2 with `Stack = -1000000`.
4. `stackSize != slot1.Stack`, so line 250 runs `slot1.Stack -= stackSize` → slot 1 becomes `1 + 1000000`.
5. Destroy slot 2 (`DITM2`), leaving a clean 1,000,001 stack.
6. Sell to a vendor for `Stack * Value / 2` gold, or drop/distribute the items.

Because the `slot2 == null` path never consults `CanStack`/`StackSize`, **this works on non-stackable items as well.**

**Fix:** reject `stackSize <= 0`, and require `CanStack` semantics on the new-slot path.

---

### C2. Arbitrary SQL execution through the Google Sheets data import

**Files:** `CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:22-36`, all `*CsvToSql.cs` converters
**Execution sinks:** `Goose/GameWorld.cs:140`, `Goose/GameWorld.cs:190-197`, `Goose/Events/UpdateSqlCommandEvent.cs:42-43`

Inserts are built by raw string concatenation. Each converter's `TransformValue` escapes only the columns it names explicitly; every other column falls through:

```csharp
default:
    return value;   // raw, unquoted, unvalidated
```

(`ItemsCsvToSql.cs:43-44`, `NpcCsvToSql.cs:47-48`, `MapsCsvToSql.cs:38-39`, `NpcSpawnsCsvToSql.cs:20-21`, and every other converter.)

`EscapeString` itself (`CsvToSqlBase.cs:42-45`) is correct for SQLite string literals. The hole is entirely the unescaped numeric/default path.

**Exploit:** anyone with edit access to the sheet at `DataLinkId` (`Goose/GooseSettings.json:27`) enters the following into any numeric column (e.g. `item_template_id` or any stat):

```
1), (999); UPDATE players SET access_status=9 WHERE player_name='attacker'; --
```

The generated script is executed wholesale against the live database with full rights: grant GM access, mint gold, read or rewrite password hashes.

The sheet is a **third-party-editable trust boundary being treated as trusted code.** The fetch is also anonymous over `new HttpClient().GetByteArrayAsync(...).Result` with no timeout and a fresh `HttpClient` per call (`CsvToSqlConverter.cs:42-43`), which blocks a thread indefinitely if Google stalls.

**Fix:** parameterize the generated inserts, or at minimum validate every non-escaped column with `long.TryParse`/`double.TryParse` and fail the import on mismatch.

---

### C3. Remote server crash via off-by-one in window slot validation

**Files:** `Goose/ItemContainerWindow.cs:84-87`, `Goose/ItemContainer.cs:26-29`

```csharp
public virtual bool ValidateSlotIndex(int index)
{
    return (index > 0 && index <= ItemContainer.MaxSlots);   // should be <
}
```

`MaxSlots` is the array *length*, so valid indices are `0..MaxSlots-1`. `ItemContainer.GetSlot` has no bounds check of its own. Compare `Populate` (`ItemContainerWindow.cs:15`), which correctly uses `i < MaxSlots`, and `BankWindow.cs:79-82`, which overrides `ValidateSlotIndex` safely.

`CombineBagWindow` uses the base implementation with a container of size `CombineBagSize + 1 = 11` (`GooseSettings.json:144`, `Inventory.cs:52`) and a constant window ID of `22` (`CombineBagWindow.cs:11`).

**Exploit:** open the combine bag, send `WTI22,11,1` (or `ITW1,22,11`). `ValidateSlotIndex(11)` returns true → `slots[11]` → `IndexOutOfRangeException` → per the architectural note, the whole server restarts with no player saves.

**Fix:** change `<=` to `<`, and add bounds checks inside `ItemContainer.GetSlot`/`SetSlot`.

---

### C4. Passwords are single-round MD5 with no brute-force protection

**Files:** `Goose/Player.cs:530-534` (create), `Goose/Player.cs:2347-2350` (`SetPassword`), `Goose/Events/LoginEvent.cs:169-172` (verify)

The scheme is `md5(salt + password + ServerName)`, one iteration, hex-encoded. Per-user salt is present, so rainbow tables don't apply — but a commodity GPU does ~10^10 MD5/s, and passwords are capped at **10 characters** (`ChangePasswordCommandEvent.cs:33`, `GMSetPasswordCommandEvent.cs:47`) with a 3-character minimum. Any database read (the `.db` file, a backup, C2 above, a stolen host) exhausts the entire keyspace offline.

**No brute-force protection exists.** Grepping for `ratelimit|throttle|attempts|lockout|failedlogin` across `Goose/` returns zero hits. A failed password only logs (`LoginEvent.cs:177`) and closes the socket; the attacker reconnects and retries. Server-side cost per attempt is one MD5 — no work factor.

**Salt entropy is also silently reduced.** `Player.cs:524-527`:

```csharp
rng.GetNonZeroBytes(saltBytes);
string salt = Encoding.ASCII.GetString(saltBytes);   // every byte >= 0x80 becomes '?'
```

This cuts a nominal 128-bit salt to roughly 72 bits of real entropy. Verification (`LoginEvent.cs:167`) repeats the same lossy conversion, so it is at least self-consistent. The same `ASCII.GetBytes` treatment is applied to the password itself, so non-ASCII password characters all collapse to `?`.

**Fix:** migrate to Argon2id / bcrypt / scrypt / PBKDF2 with a real work factor; add per-IP and per-account throttling with exponential backoff; raise the password length cap; keep salt bytes as `byte[]` rather than round-tripping through ASCII.

---

## High

### H1. `/changepassword` requires no current password

**File:** `Goose/Events/ChangePasswordCommandEvent.cs:22-38`

The handler checks only `State == Ready`, then calls `SetPassword` on the new value. No re-authentication, no confirmation.

Any momentary control of an authenticated session — an unattended client, an on-path attacker, a session replay — becomes **permanent account takeover with one packet**: `/changepassword newpass\x1`. The legitimate owner is then locked out, and there is no recovery flow anywhere in the codebase.

**Fix:** require the current password as an argument and verify it before calling `SetPassword`.

---

### H2. Credentials and sessions travel in cleartext with no session token

**Files:** `Goose/GameServer.cs:141-155`, `Goose/Events/LoginEvent.cs:65-101`

- Plain path: `LOGINname,password` in ASCII over raw TCP.
- "Encrypted" path: XOR against the hardcoded 5-byte key `"Tamra"` (`LoginEvent.cs:86-90`) — a public constant, i.e. encoding, not encryption.
- `LoginEvent.cs:81` comments *"we ignore checking the hash since we don't really care at this point"*, so the 32-byte MD5 integrity field is never validated.

Post-login, identity is bound solely to the TCP socket (`PlayerHandler.sockToPlayer`, `PlayerHandler.cs:115-121`) with no per-session secret. A passive observer recovers every password verbatim; an on-path attacker can inject `/changepassword x` into an established session.

**Fix:** at minimum, tunnel over TLS. Failing that (client compatibility), a challenge-response handshake would stop passive password recovery, though not injection.

---

### H3. Crafting cost bypass — combine with fewer ingredients than the recipe requires

**Files:** `Goose/CombinationHandler.cs:139-142`, `Goose/Inventory.cs:1045-1076`

```csharp
if (c <= 0 || req.Value < c)   // rejects too many, never rejects too few
{
    matched = false;
    break;
}
```

`GetMatch` accepts a match whenever the player's count is in `[1, req.Value]`. It never requires `c >= req.Value`. `Combine`'s consumption loop then does `slotcount -= count` with the comment *"don't care if it's negative since the check above catches it"* — the check above does not catch it.

**Exploit:** for a recipe needing 3× X + 1× Y, place **1** X and **1** Y in the combine bag and press Combine. `combineHash = {X:1, Y:1}`, both satisfy `c <= req.Value`, so it matches; `slotcount = 1 - 3 = -2` frees the slot; the result item is created. The output is crafted for a fraction of its intended cost.

**Fix:** require the summed stack count per template to be `>= req.Value`.

---

### H4. Unbounded per-connection receive buffer

**Files:** `Goose/Player.cs:512-515`, `Goose/GameWorld.cs:546-555`

```csharp
public void Received(string data)
{
    this.Buffer += data;   // no cap
}
```

Trimming happens only when a `\x1` delimiter arrives (`GameWorld.cs:549`). A client streaming bytes containing no delimiter grows the string without limit, and `+=` makes it O(n²): at ~100 MB accumulated, every 8 KB read copies 100 MB **on the game loop thread**, so the server stalls long before it reaches `OutOfMemoryException`. No packet size limit exists anywhere in the codebase.

**Fix:** cap `Buffer` (e.g. 64 KB) and disconnect on overflow; switch to a `StringBuilder` or byte accumulator.

---

### H5. No connection cap, no pre-login timeout

**Files:** `Goose/GameServer.cs:129-133`, `Goose/GameWorld.cs:520-536`, `Goose/PlayerHandler.cs:91`

`this.sockets.Add(newSocket)` is unbounded, with no per-IP limit. `MaxPlayers` is enforced only when a LoginID is assigned (`PlayerHandler.cs:91`) — long after the socket is permanently held. The ping timeout (`Player.cs:1810-1815`) applies only to logged-in players, so a socket that connects and sends nothing sits in the list forever.

Worse, several `LoginEvent.Ready` early-returns (`:68`, `:75`, `:92`, `:99`) exit **without** calling `Disconnect`, so a malformed login leaks the socket permanently.

`Socket.Select` (`GameServer.cs:113-116`) walks that list twice per ~1 ms iteration, so idle connections linearly degrade the single game thread toward file-descriptor exhaustion — all well below `MaxPlayers: 200`.

**Fix:** cap concurrent sockets and per-IP connections; add a pre-login handshake timeout; ensure every `LoginEvent` failure path disconnects.

---

### H6. `Player.Send()` is unguarded and called directly from the game loop

**Files:** `Goose/Player.cs:2329-2336`, `Goose/GameServer.cs:122`

```csharp
public void Send()
{
    lock (socketLock)
    {
        var bytesSent = this.sock.Send(this.SendBuffer.ToArray());
        this.SendBuffer.RemoveRange(0, bytesSent);
    }
}
```

No `sock == null` check (the string overload at `:2317` has one; this one does not) and no `try`/`catch`. Called from `GameServer.cs:122` (`player?.Send()`), itself outside any handler.

**Exploit:** connect, log in, stop reading from the socket until `SendBuffer` fills, then send RST. `Socket.Select` marks it writable, `Send` throws `SocketException`/`ObjectDisposedException`, and the whole server restarts.

Related, same file: `GameServer.cs:114` reads `GetPlayer(s)?.SendBuffer.Count > 0` — the `?.` guards `GetPlayer` but not `.SendBuffer`, which is initialized only in `Player.OnLogin()` (`Player.cs:495-499`, called at `LoginEvent.cs:237`), 16 lines *after* `AddPlayer` inserts into `sockToPlayer` (`LoginEvent.cs:221`).

Also unguarded: `listen.Accept()` (`GameServer.cs:129`) and `GameWorld.NewConnection`'s `sock.RemoteEndPoint.ToString()` (`GameWorld.cs:470`), which throws `ObjectDisposedException` on a connection reset before accept. Note that `PlayerCountExperienceModifierUpdateEvent.cs:20-31` already wraps the identical call in `catch (ObjectDisposedException)` with the comment "eat exception to stop crash" — the failure mode is known, just not fixed at the accept path.

---

### H7. Bind / ownership laundering through split

**File:** `Goose/Inventory.cs:228-236`

The new slot is built via `new Item(); LoadFromTemplate(slot1.Item.Template)` and only re-applies `IsBindOnPickup`. `IsBound`, `BaseStats`, `StatMultiplier`, `Description`, and `ItemProperties` (title/surname) are **not** copied.

**Exploit:** equip a bind-on-equip item (`Inventory.cs:386-389` sets `IsBound = true`), unequip it, then `SPLIT<slot>,<emptyslot>,1`. Since `stackSize == slot1.Stack`, the original is nulled and replaced by an identical-template item with `IsBound == false` — now freely droppable, bypassing the check at `PlayerDropItemEvent.cs:52`. Also strips the `Custom` marker used by `DestroyItemEvent.cs:52,66`.

---

## Medium

### M1. `/hax` is ungated

**File:** `Goose/Events/HaxCommandEvent.cs:19-25`

The gate is only `if (this.Player.State == Player.States.Ready)` — no privilege check. Line 23 does `world.Send(this.Player, ((string)this.Data).Substring(5))`, writing an attacker-chosen raw packet into the connection.

Impact is limited to **the attacker's own socket** (`GameWorld.cs:575-583` sends to the single passed player), so this is client-side spoofing and client-parser probing, not privilege escalation. It is plainly a leftover debug command: its sibling `/gmhax` (`GMHaxCommandEvent.cs:21`) *is* gated on `Access == GameMaster`.

**Fix:** add the same gate, or remove the registration at `EventHandler.cs:70`.

> This was the **only** authorization gap found. A full command-to-privilege audit of every `*CommandEvent.cs` appears in the appendix; every other privileged command has a correct check, and there are no inverted comparison operators.

---

### M2. `/updatesql` is broken and always fails (nested transaction)

**Files:** `Goose/Events/UpdateSqlCommandEvent.cs:37`, `CsvToSql/CsvToSql.Core/sqlTemplate.sql:1`

The handler opens `conn.BeginTransaction()`, then executes a script that itself begins with `BEGIN TRANSACTION;` (and `COMMIT;` at `:485`). SQLite rejects the inner `BEGIN` ("cannot start a transaction within a transaction"), so the command throws, `tx.Rollback()` runs, and the admin always sees "Failed updating sql." The startup path (`GameWorld.cs:190-197`) has no outer transaction, which is why this went unnoticed.

Separately, the script issues `DROP TABLE IF EXISTS` against 22 tables on the live database while the game thread holds in-memory references built from those tables. Even once the transaction bug is fixed, `ReloadSql` and a live `/updatesql` are not atomic with respect to game state.

---

### M3. GM password reset is never persisted

**File:** `Goose/Events/GMSetPasswordCommandEvent.cs:44-52`

`player.SetPassword(password)` mutates the in-memory `Player` only. `BanCommandEvent.cs:52` and `SetAccessCommandEvent` both call `player.SaveToDatabase(world)` for offline targets — this handler does not, and `PlayerSaveEvent.cs` skips players in `States.NotLoggedIn`.

**Scenario:** an account is compromised; a GM runs `/setpassword Victim newpass` while the attacker is offline; the server restarts (or crashes — `GameServer.cs:47-68` reloads from DB). `LoadPlayerData` re-reads the old `password_hash` and the attacker's password works again. The GM has no indication the reset was lost.

---

### M4. Ban is not persisted if the target is stuck in `LoadingGame`

**Files:** `Goose/Events/BanCommandEvent.cs:45-53`, `Goose/Events/LogoutEvent.cs:36-41`

`BanCommandEvent` sets `Access = Banned` in memory, then for an online player calls `world.LostConnection(player.Sock)` and relies on `LogoutEvent` to persist it. `LogoutEvent`'s `LoadingGame` branch does **not** call `SaveToDatabase` (only the `Ready`/`LoadingMap` branch does) and sets `State = NotLoggedIn`, so the pending `PlayerSaveEvent` also skips.

**Exploit:** a custom client logs in and never sends `LCNT`, so `State` stays `LoadingGame` indefinitely (it answers `PING` with `PONG` to defeat the 198s timeout — `Player.cs:1812`; `PlayerPongEvent.cs` has no state gate). A GM bans it; the socket drops but nothing is written. After the next restart the account is `Normal` again.

---

### M5. No signal handling — up to 3 minutes of state lost on any non-graceful exit

**Files:** `Goose/Program.cs:16-33`, `Goose/GameServer.cs:172-185`

No `Console.CancelKeyPress` or `AppDomain.ProcessExit` handler is installed, and `GameServer.Stop` runs only when the game loop exits normally. Ctrl+C, `systemctl stop`, or SIGTERM kills the process with all authoritative state in RAM — the database is a write-behind mirror flushed every `PlayerSavePeriod` = 180s (`GooseSettings.json:94`, driver at `Player.cs:1821-1825`).

**Related — buffered logs are dropped on every shutdown.** `LogHandler.cs:17-29` accumulates `Log` objects in a `List`, flushed only from `PlayerCountExperienceModifierUpdateEvent.cs:52` on a 600s cadence (`GooseSettings.json:125`). `GameWorld.Stop` (`GameWorld.cs:435-458`) saves players and stops the DB but **never calls `LogHandler.Save`** — so up to 10 minutes of audit/anti-dupe logs are lost even on a clean shutdown, and always on a crash.

---

### M6. No cross-entity transaction on save — item/gold dupe window

**File:** `Goose/Player.cs:840-1004`

`SaveToDatabase` issues the `players` row, `Inventory.Save`, `Spellbook.Save`, `Bank.Save`, each pet, and `SaveQuests` as *separate* `Enqueue` work items, each committing in its own implicit transaction.

- **Intra-player:** a crash between the `players` UPDATE and `Inventory.Save` persists new gold with the old inventory (or vice versa) — buy an item, crash, keep both.
- **Inter-player:** A gives an item to B; B's autosave fires, A's does not, crash → on restart both have it. No mechanism exists to save both sides of a transfer atomically.

**Fix:** wrap one player's full save in a single `Enqueue`d transaction, and save both sides of a transfer inside one work item.

---

### M7. Free / negative-cost credit purchases via `int` overflow

**File:** `Goose/Events/VendorPurchaseInventoryEvent.cs:95,114`

`slot.ItemTemplate.Credits * slot.Stack` is `int * int` in both the affordability check and the deduction (`Item.Credits`/`ItemTemplate.Credits` are `int`, `NPCVendorSlot.Stack` is `int`). If the product overflows negative, the check `> this.Player.Credits` passes and `this.Player.Credits -= cost` **increases** credits. The gold path (`:87`, `:126`) is safe because `Value` is `long`.

`Player.Credits` is `int` (`Player.cs:427`) while gold and experience are `long`; `GiveCreditsCommandEvent.cs:54` does `player.Credits += credits` with no overflow guard.

---

### M8. Cross-thread mutation of live game state from `Task.Run` GM commands

**Files:** `Goose/Events/ReloadSQLCommandEvent.cs:29`, `Goose/Events/ReloadScriptsCommandEvent.cs:27-34`

`ReloadSQLCommandEvent` runs `SpellHandler.LoadSpellEffects/LoadSpells`, `ItemHandler.LoadTemplates/RefreshItemStats`, `QuestHandler.LoadQuests`, and `NPCHandler.LoadNPCTemplates` on a thread-pool thread. These write `Dictionary` instances the game loop concurrently reads (`SpellHandler.cs:146`, `NPCHandler.cs:119`; `SpellEffect.BuffStacksOver` is reassigned at `SpellHandler.cs:134-135` and `Add`ed at `:165/:184` while combat enumerates it at `NPC.cs:1501`). A concurrent `Dictionary` write plus read can hang inside `FindEntry` at 100% CPU, or throw.

`ReloadScriptsCommandEvent` already carries a `// TODO: This is bad` comment; it mutates `ScriptHandler`'s dictionary and runs script `OnLoaded` off-thread, which can reach `EventHandler.AddEvent` and race the game loop's `SortedList` enumeration.

**The only `lock` in the entire server is `Player.socketLock`.** There is no locking on `SpellHandler`, `NPCHandler`, `ItemHandler`, `MapHandler`, `PlayerHandler`, or `EventHandler`.

---

### M9. Buff list mutated while being iterated

**Files:** `Goose/Player.cs:2241` (`OnMeleeAttack`), `Goose/Player.cs:2222` (`OnMeleeHit`), `Goose/NPC.cs:1475`, `:1603`, `:1619`

`OnMeleeAttack` iterates `this.Buffs` and calls `b.SpellEffect.OnMeleeAttackSpell.Cast(this, this, world)`. The target is `this`, so `Cast` → `SpellEffect.CastBuffSpell` (`SpellEffect.cs:685`) → `target.AddBuff` → `Player.cs:2071 this.Buffs.Add(buff)` **inside the `foreach`** → `InvalidOperationException` → whole-server restart.

`OnMeleeAttackSpell` is also never null-checked and is `null` whenever `on_attack_spell_effect_id` points at a missing effect (`SpellHandler.cs:151` silently stores null).

Logic bug in the same method: the `hit` parameter of `OnMeleeAttack` is unused — the proc is cast on the attacker rather than the victim.

---

### M10. Guaranteed NRE for Tick/Root/Stun effects with `effect_duration <= 0`

**Files:** `Goose/Player.cs:2059`, `Goose/NPC.cs:1522`

Both read `buff.BuffExpireEvent.Ticks`, but `BuffExpireEvent` is assigned only inside the `if (buff.SpellEffect.Duration > 0)` block immediately above. A data row with duration 0 and effect type Tick/TickBuff/Viral/Root/Stun — plausible for permanent item buffs, which `Inventory.cs:368` builds exactly this way — crashes the world the first time it is applied.

---

### M11. Item-buff tick events reschedule forever and survive un-equip

**Files:** `Goose/Events/BuffTickEvent.cs:15,64,79`, `Goose/Inventory.cs:547-560`, `Goose/Player.cs:2141`

`if (!buff.ItemBuff && buff.BuffExpireEvent == null) return;` never short-circuits for item buffs, and `CheckAndAddBuffTickEvent` skips the expiry check for them, so it unconditionally schedules another tick. Un-equip calls `RemoveBuff`, which removes only `BuffExpireEvent` — the tick event is never cancelled. Every equip of a tick-effect item permanently adds one more perpetual event that keeps applying its effect to a buff the player no longer has: unbounded event-list growth plus phantom damage/healing.

---

### M12. Second-order SQL injection: `Pet.EquippedItems`

**Files:** `Goose/Pet.cs:348`, `Goose/Pet.cs:419`

These concatenate `"'" + this.EquippedItems + "'"` directly into the pet INSERT/UPDATE, while name/title/surname on the *same statements* are parameterized. The value originates from `npc_templates.equipped_items` (`NPCHandler.cs:65` → `NPC.cs:618` → `Pet.cs:188`) — i.e. from the Google Sheet, where it *is* escaped at import. A stored value legitimately containing `'` therefore round-trips into a raw concatenation. This survives even if C2 is fixed.

`Player.cs:861-862` and `:931-932` concatenate `PasswordHash`/`PasswordSalt`; these are hex and Base64 so they are not exploitable today, but they are the only remaining unparameterized string columns on the player save and should be parameterized on principle.

---

### M13. `CreditsUpdateEvent` — injection plus redeem-before-persist

**File:** `Goose/Events/CreditsUpdateEvent.cs:22-58`

Line 53: `"UPDATE paypal_payments SET redeemed='1' WHERE txn_id='" + r + "';"` — `txn_id` comes from a row written by an external payment-notification path outside this codebase. If that writer doesn't sanitize, this is injection from outside the game.

Separately, credits are added to the in-memory `Player`, the row is marked `redeemed='1'` immediately in autocommit, and the offline player's persistence is a *later*, separate `SaveToDatabase` at `:60`. A crash in between permanently consumes the payment with no credits delivered. No transaction spans the two.

*(Currently commented out of the schedule at `GameWorld.cs:402-404` — fix before re-enabling.)*

---

### M14. `Database` service — blocking waits, unbounded queue, silent write loss

**File:** `Goose/Database.cs`

- **`:188`, `:209`** — `work.Done.Wait()` with no timeout. `Loop` (`:117-170`) catches per-item exceptions so ordinary SQL failures won't kill the thread, but any escape that ends the loop leaves every caller — including the single game thread — parked permanently with no diagnostic. Add a timeout plus a fault flag, and fail fast on `_loopTask.IsCompleted`.
- **`:19`** — `_queue` is an unbounded `BlockingCollection`. Each player save enqueues ~6 items carrying full serialized JSON (`Inventory.cs:852-856`); if the DB thread falls behind (large import, disk stall, WAL checkpoint), memory grows without limit. No backpressure, no queue-depth alarm.
- **`:250-255`** — on a 2-minute `Stop()` timeout it logs and returns, leaving `_started` true. No caller checks anything: `GameWorld.Stop` (`GameWorld.cs:456`) proceeds to "Finished shutting down" and the process exits, losing every queued save. The `while (PendingCount > 0) Sleep(100)` at `GameWorld.cs:452-455` can't see the in-flight item, so it isn't the safety net it appears to be. `Stop` should return a bool and the caller should log loudly.
- **`:54-55`** — sets `Journal Mode=Wal` and `busy_timeout` but never sets `synchronous` explicitly, leaving durability to the provider default. Make it explicit and consider a periodic `wal_checkpoint(TRUNCATE)`.

---

### M15. Game state mutated on the DB thread from `Enqueue` callbacks

**Files:** `Goose/Player.cs:922`, `Goose/Pet.cs:364`

`this.AutoCreatedNotSaved = false;` executes inside the `Enqueue` closure — on the DB thread, unsynchronized with the game thread that reads the same field at `Player.cs:849`. A new character whose save is enqueued and then saved again before the DB thread drains enqueues a second INSERT with the same `player_id` → primary-key violation, logged at `Database.cs:153` and swallowed.

`Guild.Save` (`Guild.cs:181-266`) has the same shape via `Database.Execute`, mutating `this.ID` (`:199`), `player.GuildID` (`:206`) and `this.Members` (`:262`) on the DB thread — safe only because the game thread happens to be blocked waiting. `UpdateSqlCommandEvent.cs:70,75` calls `world.Send` from the DB thread, which will stall the entire DB queue if the socket blocks.

---

### M16. Outbound packets silently dropped for slow clients

**File:** `Goose/GameWorld.cs:581-588`

`player.Send(data)` is wrapped in `catch (Exception) { }`. Sockets are non-blocking (`GameServer.cs:130`), so when a client's send window is full, `Socket.Send` throws `SocketException(WouldBlock)` rather than returning a partial count — the partial-buffering path at `Player.cs:2323-2325` is bypassed and the packet is discarded with no log. User-visible symptom: invisible NPCs and items, ghost characters, desynced HP for laggy players, with nothing in the logs.

---

### M17. Windows service shutdown is racy

**File:** `Goose/GooseWindowsService.cs:24,29`

`server` is assigned inside the started task; `OnStop` calls `server.Stop()` — a `NullReferenceException` if the SCM stops the service before the task body runs. Even when non-null, `Stop()` runs on the SCM thread concurrently with `GameLoop`: `GameWorld.Stop` iterates and saves `PlayerHandler.Players` while the game loop adds to and removes from that same `List` (`PlayerHandler.cs:50,80`) → `InvalidOperationException` mid-save, and `GameServer.Stop:179` closes sockets out from under `Socket.Select`.

Related: `GameServer.Run` (`:60-63`) calls `this.Stop()` from the catch block, which `GameLoop:162` may already have called. The `stopping` flag guards one ordering but not the reverse, so `GameWorld.Stop` can run twice against a `Database` whose `_started` is already false, throwing out of `SaveToDatabase` and aborting the remaining players' saves.

---

### M18. `RemoveEvent` removes by timestamp without an identity check

**File:** `Goose/EventHandler.cs:208`

`this.events.Remove(e.Ticks)` deletes whatever occupies that key. The concrete trigger is `MacroCheckCommandEvent.cs:54`, which does `player.MacroCheckEvent.Ticks += (long)(300 * world.TimerFrequency);` — **mutating `Ticks` on an event already inside the `SortedList`**. That desynchronizes the object from its sort key, so a later `RemoveEvent` deletes an unrelated player's save, respawn, or buff event. Callers at `Player.cs:2143` and `NPC.cs:1570` run constantly.

---

### M19. Account enumeration and name squatting via login responses

**File:** `Goose/Events/LoginEvent.cs:112,157,179`

Three distinguishable failures: `"Character does not exist."`, `"Wrong password for character."`, `"Character is already logged in."` With `AutoCharacterCreation: false`, this builds a valid-account list before brute-forcing. With `AutoCharacterCreation: true` (the shipped default, `GooseSettings.json:45`) it is worse: probing a non-existent name silently **creates** that character with the attacker's password, so every plausible name can be pre-squatted, and "already logged in" doubles as an online-status oracle for GM accounts.

Related (`LoginEvent.cs:139-160` vs `:214-228`): `AddPlayerToData(player)` runs at `:151`, but the lockdown check (`:214`) and server-full check (`:222`) come *after* and both return without `RemovePlayerFromData`. During `LockdownModeEnabled` maintenance, `LOGINAdminName,pw` is denied but still claims the name with the attacker's hash. The `NewCharactersPerDayPerIP` counter is likewise incremented at `:136` before the name is validated.

---

### M20. `Pet.LoginIDToPet` is static and leaks across crash-restarts

**File:** `Goose/Pet.cs:21,26`

The dictionary is `static`, so it survives the `GameWorld` rebuild in `GameServer.Run`. After a restart, stale pets from the dead world still occupy login IDs; `Pet.GetLoginID` scans for a free one and the table grows indefinitely across restarts.

---

## Low

- **`Inventory.cs:113`** — `if (i < 1 && i > GameWorld.Settings.InventorySize) return;` uses `&&` where `||` was intended; the guard can never fire. `this.inventory[i]` on the next line is unprotected. Currently masked by callers validating independently.
- **`Inventory.cs:184-187`** — `SetSlot` has no bounds check; safe only because `InventoryToWindowEvent.cs:45` and `WindowToInventoryEvent.cs:45` validate first.
- **`ItemContainer.cs:21-29`** — `SetSlot`/`GetSlot` have no bounds checks at all (root cause of C3).
- **`PlayerBank.cs:44-55`** — `Load` writes `containerSlots.Length` entries into a container sized from the *current* `NumberOfBankPages`; lowering a player's bank pages makes login throw.
- **`Map.cs:301-353`** — `PlaceItem` is a `while(true)` with no termination condition if no free or stackable tile exists; a fully covered map hangs the server thread on drop.
- **`EventHandler.cs:189-195`** — `AddEvent` recurses once per key collision instead of looping. `StackOverflowException` is uncatchable in .NET, so this would kill the process outright rather than hitting the `Run()` handler. Hard to force (ticks come from `Stopwatch.GetTimestamp()`), but it should be a `while` loop.
- **`LoginEvent.cs:175`** — `PasswordHash.Equals(hash)` is a non-constant-time compare. Both sides are server-computed hashes and network jitter dwarfs the signal, so this is not practically exploitable; noted for completeness.
- **`sql/create.sql:9`** — `CREATE LOGIN GooseServer WITH password='password1';` A legacy MSSQL bootstrap credential committed to the repo. Almost certainly dead now that the server uses SQLite, but it is a live credential if any operator ran it.

---

## Code quality

**Swallowed exceptions, 20+ sites**, each confirmed by a `CS0168` build warning on an unused `e`: `Map.cs:139,157,413,450,472,521` (map script load/enter/leave failures vanish), `Player.cs:2081,2154`, `NPC.cs:1543,1581`, `BuffTickEvent.cs:30`, `Inventory.cs:434`, `ItemHandler.cs:203`, `Pet.cs:654`, `PlayerAttackEvent.cs:65`, `ChatEvent.cs:76`, `GameWorld.cs:478,503`. Every one is a script or logic bug that will be invisible in production.

**Obsolete crypto types** (SYSLIB warnings): `RNGCryptoServiceProvider` at `Player.cs:524,2341`; `MD5CryptoServiceProvider` at `Player.cs:530,2347` and `LoginEvent.cs:169`. Both are `IDisposable` and are never disposed. Replace with `RandomNumberGenerator.Fill` / `MD5.HashData` — though see C4, MD5 should not be here at all.

**Cross-platform** — `Program.cs:22` calls `ServiceBase.Run` unconditionally on a `net10.0` TFM (`CA1416`); `-service` on Linux throws `PlatformNotSupportedException`. Guard with `OperatingSystem.IsWindows()` or move to `Microsoft.Extensions.Hosting.WindowsServices`. `Program.cs:32`'s `Console.ReadKey()` throws `InvalidOperationException` when stdin is redirected (systemd, Docker), turning a clean shutdown into a crash exit.

**Dead code** — `PlayerHandler.cs:6` `using System.Data.SqlClient;` (pre-SQLite era); large commented-out blocks at `GameWorld.cs:627-651` and `:402-404`; `InstaLevelCommandEvent.cs` is entirely commented out and not registered in the dispatcher; `SaveConfigCommandEvent`'s body is commented out.

**Dependencies** (`Goose/Goose.csproj`) — nothing dangerously stale. `Microsoft.CodeAnalysis.CSharp[.Scripting]` 5.6.0 restores fine, but pinning Roslyn independently of the SDK is a known source of load conflicts once the SDK ships a newer compiler. `System.ServiceProcess.ServiceController` 10.0.0 is Windows-only and referenced unconditionally (see `CA1416`). `System.Data.SQLite.Core` 1.0.119 is fine.

---

## Scalability

Not bugs today, but the two things that will cap player count:

1. **`EventHandler` is O(n) per operation on a `SortedList`.** `AddEvent` (`:198`) is an O(n) array insert; `Update` calls `IndexOfValue` (`:230`) — an O(n) *linear* scan — once per ready event, and `RemoveAt` is another O(n) shift. With one move, attack, and regen event per NPC plus buff ticks per player, `events` reaches tens of thousands and every tick costs O(k·n). A `PriorityQueue<Event,long>` plus a cancellation flag on `Event` fixes this and M18 together.
2. **Per-move O(P + N) map scans.** `Map.GetPlayersInRange`/`GetNPCsInRange` (`Map.cs:102,116`) are full linear scans, and `NPC.MoveTo` (`NPC.cs:511`) calls both — twice for players — on **every NPC step**, plus a third full NPC scan when aggroed (`NPC.cs:562`). That is O(N·(N+P)) per movement period per map. `PlayerHandler.GetPlayer(string)`/`IsLoggedIn` (`:131,:146`) are also linear with a `ToLower()` allocation per candidate. A spatial grid or per-tile bucketing is the fix.

---

## Investigated and found NOT vulnerable

Recorded so these aren't re-audited.

**SQL:** every player-controlled string reaching SQL is bound via `SQLiteParameter` — player name/title/surname, guild name and MOTD, pet names, chat text in `logs`, and all serialized inventory/bank/spellbook/quest JSON (`Player.cs:916-919,986-989`; `Guild.cs:194-195,215-216`; `Pet.cs:360-362,432-434`; `Log.cs:95-96`; `Inventory.cs:864-885`; `PlayerBank.cs:76-78`; `Spellbook.cs:71-72`). The concatenated `WHERE` clauses elsewhere (`GuildHandler.cs:48`, `Map.cs:488,507`, `QuestHandler.cs:40,61`, `Inventory.cs:902,921,952`, `Player.cs:787,803`, `PlayerBank.cs:33`, `NPCHandler.cs:154,172`, `Spellbook.cs:42`) all interpolate `int`s. `EscapeString` in the CSV converter is itself correct.

**Packet parsing:** broadly defensive. Every `Substring(N)` in `Goose/Events/*.cs` uses `N == key.Length` for its `stringToEvent` key, which `StartsWith` guarantees — all 39 files containing no `try` were checked, plus every secondary `Substring(1)` (`RankCommandEvent.cs:37`, `GuildMotdCommandEvent.cs:34`, `GuildRemoveCommandEvent.cs:36`). `Convert.ToInt32` calls in `InventoryUseEvent`, `WindowButtonClickEvent`, `PlayerLeftClickEvent`, `InventorySplitEvent`, `VendorSell/PurchaseInventoryEvent`, and `WindowToWindow/Inventory` are all inside `catch (Exception)`. `MoveEvent.cs:69`/`FacingEvent.cs:52` index `[1]` safely. `Packets.cs` is outbound-only except `Emote` (`:290-302`), which is `TryParse`- and range-guarded. `LoginEvent.cs:85-100`'s attacker-controlled length bytes are inside `catch { return; }`.

**Scripting:** no player-reachable arbitrary code execution. `Goose/Scripting/Script.cs:26-49` compiles only `File.ReadAllText(this.FilePath)`, and paths come from SQL data or disk enumeration — **no player-supplied string ever reaches the compiler.** `ScriptOptions` applies no sandboxing, but that is moot without an input path. The one code-execution chain is GM-only (`/setconfig DataLinkId` → `/updatesql` → `/reloadsql` + `/reloadscripts`, exploiting the lack of path normalization at `ScriptHandler.cs:21`) — a GM compromising their own host, not player→GM escalation. Worth hardening as defense-in-depth.

**Auth:** duplicate/concurrent login is correctly serialized — `EventHandler.Update` runs `Ready()` single-threaded, so the `IsLoggedIn` check (`LoginEvent.cs:110`) and `AddPlayer` (`:221`) are atomic with respect to each other. `/changename` (`ChangeNameCommandEvent.cs:49-51`) correctly re-keys `allNameToPlayer`. `LoginID` is server-assigned (`PlayerHandler.cs:89-97`) and never accepted from a client packet. Argument-index crashes in `/ban`, `/unban`, `/kick` are unreachable because the dispatcher keys include a trailing space.

**Economy:** vendor arithmetic has no round-trip profit — buy `Value * stack`, sell `stack * Value / 2` (`VendorSellInventoryEvent.cs:98`), with stack and slot bounds both validated (`:52`, `:82`). `Map.PlaceItem` only selects coordinates and does not insert into `tiles`, so drop-stacking is not a dupe. No trade system exists (`WindowFrames.Trade` is declared but unused), so there is no trade-window race surface. `InventoryChangeSlotEvent`, `InventoryUseEvent`, `DestroyItemEvent`, `WindowToWindowEvent`, `BankWindow.ValidateSlotIndex`, and banker-range checks all validate correctly. `GetItemCommandEvent` is privilege-gated and rejects `stack <= 0`.

**Concurrency:** `Database.cs` is genuinely single-threaded and its `Start`/`Stop`/dispose paths are correct — `Execute` re-entrancy from the DB thread is handled, `Stop` is idempotent, and the connection is disposed on both failure and normal paths. `GameServer.GameLoop` snapshots its socket lists (`:113`) before `Socket.Select`, so mutating `this.sockets` from `Disconnect` mid-iteration is safe.

**Event queue ordering:** a reviewer flagged the removal condition at `EventHandler.cs:230` (`IndexOfValue(ev) < readyEvents.Count`) as silently cancelling recurring timers. This was traced and **rejected**: self-rescheduling events re-add the same object at a higher key while the original entry remains at the lower key, and `IndexOfValue` returns the first (lower) match, so the correct entry is removed. Events created during `Ready` always receive timestamps after the ready block, so they don't shift indices either. `Event.Ticks` (`Event.cs:24`) and `GameWorld.TimeNow` (`GameWorld.cs:62`) both use `Stopwatch` ticks consistently. The code is confusing and worth rewriting as a `PriorityQueue`, but there is no live bug here. (M18 is a genuinely different mechanism.)

---

## Recommended fix order

1. **C1** — reject `stackSize <= 0` in `SplitSlots`. One line; closes a total-economy-break dupe.
2. **C3** — `<=` → `<` in `ValidateSlotIndex`, plus bounds checks in `ItemContainer`. One line; closes a one-packet remote server crash.
3. **Exception isolation** — wrap the per-socket body of `GameLoop` (accept, receive, send) and the `ev.Ready(world)` dispatch in `try`/`catch` that disconnects only the offending socket. This demotes M9, M10, and every future parsing bug from "server outage" to "one dropped client," and is the single highest-leverage change in this list.
4. **H4 / H5** — cap `Player.Buffer` and disconnect on overflow; cap concurrent sockets and per-IP connections; add a pre-login timeout; fix the leaking `LoginEvent` early-returns.
5. **C4 / H1** — migrate password hashing to Argon2id or bcrypt with a login-throttling layer; require the current password on `/changepassword`; raise the 10-character cap.
6. **C2** — parameterize or strictly validate the CSV-to-SQL numeric path.
7. **M5** — install signal handlers and flush `LogHandler` in `GameWorld.Stop`.
8. Move `NLog.LogManager.Shutdown()` out of the crash path so incidents after the first remain diagnosable.

Given there are no tests at all, items 1–3 are worth pairing with regression tests as the seed of a test project.

---

## Appendix — command authorization audit

Every `*CommandEvent.cs` was checked. The dispatcher is `Goose/EventHandler.cs:31-183` — a `Dictionary<string, CreateEvent>` prefix map whose `AddEvent` (`:170-183`) does `packet.StartsWith(key)` and constructs the event with **zero access checking**. All authorization is delegated to each event's `Ready()`.

Note: `CustomCommandEvent.cs` is *not* the dispatcher — it is the `/custom` item-customization command (requires a custom ticket, intentionally unprivileged).

| Command | Check location | Privilege | Min level | Correct? |
|---|---|---|---|---|
| `/setaccess` | SetAccessCommandEvent.cs:25-26 | SetAccess | GameMaster | Yes |
| `/gmhax` | GMHaxCommandEvent.cs:21 | `Access == GameMaster` | GameMaster | Yes |
| **`/hax `** | **HaxCommandEvent.cs:21 — only `State == Ready`** | **none** | **Normal** | **NO — see M1** |
| `/getitem ` | GetItemCommandEvent.cs:27 | SpawnItem | GameMaster | Yes |
| `/giveexperience ` | GiveExperienceCommandEvent.cs:21-22 | GiveExperience | GameMaster | Yes |
| `/givecredits ` | GiveCreditsCommandEvent.cs:21 | n/a — player-to-player, debits sender at :55 | Normal | Yes, by design |
| `/shutdown` | ShutdownCommandEvent.cs:27 | Shutdown | GameMaster | Yes |
| `/setconfig ` | SetConfigCommandEvent.cs:22-23 | SetConfig | GameMaster | Yes |
| `/saveconfig` | SaveConfigCommandEvent.cs:21-22 | `Access == GameMaster` | GameMaster | Yes (body commented out) |
| `/reloadscripts` | ReloadScriptsCommandEvent.cs:24-25 | ReloadScripts | GameMaster | Yes |
| `/reloadsql` | ReloadSQLCommandEvent.cs:24-25 | ReloadSQL | GameMaster | Yes |
| `/updatesql` | UpdateSqlCommandEvent.cs:24-25 | ReloadSQL | GameMaster | Yes |
| `/changename ` | ChangeNameCommandEvent.cs:21-22 | ChangeName | GameMaster | Yes |
| `/checkname ` | CheckNameCommandEvent.cs:21-22 | ChangeName | GameMaster | Yes |
| `/settitle ` | SetTitleCommandEvent.cs:21-22 | SetTitle | Guide | Yes |
| `/setsurname ` | SetSurnameCommandEvent.cs:21-22 | SetSurname | Guide | Yes |
| `/broadcast ` | BroadcastCommandEvent.cs:21-22 | Broadcast | EventMaster | Yes |
| `/setpassword ` | GMSetPasswordCommandEvent.cs:22-23 | SetPassword | GameMaster | Yes (but see M3) |
| `/ban ` | BanCommandEvent.cs:27-28 | Ban | Guide | Yes (but see M4) |
| `/unban ` | UnbanCommandEvent.cs:21-22 | Ban | Guide | Yes |
| `/kick ` | KickCommandEvent.cs:27-28 | Kick | Guide | Yes |
| `/macrocheck ` | MacroCheckCommandEvent.cs:22-23 | MacroCheck | Guide | Yes |
| `/playerinfo ` | PlayerInfoCommandEvent.cs:22-23 | PlayerInfoCheck | Guide | Yes |
| `/mutemap` | MuteMapEvent.cs:25-26 | MuteMap | EventMaster | Yes |
| `/respawnmap` | RespawnMapCommandEvent.cs:21-22 | RespawnMap | GameMaster | Yes |
| `/search ` | SearchCommandEvent.cs:23 | Search | GameMaster | Yes |
| `/spawnnpc ` | SpawnNPCCommandEvent.cs:26 | SpawnNPC | GameMaster | Yes |
| `/placespawn` | PlaceSpawnCommandEvent.cs:21 | PlaceSpawn | GameMaster | Yes |
| `/changeclass ` | ChangeClassCommandEvent.cs:21-22 | ClassChange | GameMaster | Yes |
| `/warp `, `/summon `, `/approach ` | WarpEvent.cs:26, SummonEvent.cs:26-27, ApproachEvent.cs:26 | Warp / Summon / Approach | Helper–Guide | Yes |
| `/toggle gm-invisible` | ToggleCommandEvent.cs:80 | GMInvisible | GameMaster | Yes |
| `/toggle who-invisible` | ToggleCommandEvent.cs:106 | WhoInvisible | EventMaster | Yes |
| `/instalevel` | none | n/a | unreachable | N/A — dead code, not registered |
| `/custom`, `/hairdye`, `/aether`, `/pet*`, `/guild*`, `/buyvita`, `/buymana`, `/changepassword`, `/charinfo`, `/credits`, `/playtime`, `/rank`, `/togglegroup`, `/mc`, `/tell`, `/who`, `/group*` | none | none | Normal | Yes, by design |

All checks use `HasPrivilege(...)` set-membership or `Access == GameMaster`. The only ordinal comparison in the codebase is `WhoEvent.cs:68` (`this.Player.Access < player.Access`, hiding higher-ranked invisible staff); its direction is correct given the enum ordering at `Player.cs:75-84` (Deleted=0, Banned=1, Normal=2, Helper=3, EventMaster=6, Guide=7, GameMaster=9).

**Architectural note:** the dispatcher performs no authorization and no state gating, so every check is duplicated per-handler. That design is why M1 exists, and it means any future command defaults to open. A privilege annotation on the dispatch table would make omissions structurally impossible rather than review-dependent.

**One `/setconfig` nit** (Low, GM-only): `SetConfigCommandEvent.cs:27` splits into at most 2 tokens with no length check, then indexes `tokens[1]` at `:46` and `:56`. `/setconfig Foo` with no value throws `IndexOutOfRangeException` out of `Ready()`, which — per the architectural note — restarts the server. Self-inflicted, but trivially fixed.
