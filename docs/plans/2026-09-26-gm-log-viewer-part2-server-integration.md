# GM Log Viewer — Part 2 of 3: Server Integration Implementation Plan

**Status:** Approved design, revised implementation plan only

**Goal:** Integrate the revised Part 1 persisted-log query foundation into the live server: authorize and open one GM viewer, maintain one server-bound search session, accept explicit Fresh/Page actions, schedule bounded immutable database work, marshal outcomes to the game thread, issue unguessable page tokens, pace bounded result delivery, audit Fresh exactly once, and invalidate safely on close/replacement/access loss/disconnect/shutdown.

**Part dependency:** This is **Part 2 of 3**. It depends on the revised Part 1 plan in `docs/plans/2026-09-26-gm-log-viewer-part1-server-query.md`: integer UTC-tick storage, signed-`Int64` row projections, separate `LogOtherIdKind`/projected related entities, `LogFreshSearchInput`, `LogQueryValidator.ValidateFresh`, immutable `LogSearchQuery`, internal `LogPageCursor`, `LogQueryEngine.Execute`, and immutable `LogSearchPage`/`LogQueryRow` outcomes. Part 1 must land first. Part 3 will implement the Godot client against the exact protocol and staging rules defined here.

**Scope boundary:** Server integration only. Do not change Part 1 migration/schema/query SQL/filter validation/entity resolution/formatting/keyset internals. Do not add client code. Part 2 owns wire parsing, random page tokens, viewer/session state, async scheduling, pacing, lifecycle, and auditing.

**Architecture:** Game-thread code parses/admit requests and owns all player/window/session/token/rate/capacity/delivery state. Fresh validates and executes in one `Database.Enqueue` item. Page resolves a server-issued token before enqueue and executes the session’s immutable validated base query with its server-held internal cursor; it never revalidates client filters or names. DB callbacks publish immutable outcomes to a stopping-aware general completion queue. `GameWorld.Update` runs events, drains completions, then pumps at most 32 KiB of log response packets. Global query capacity is released when a DB completion is drained; the viewer remains busy until its final LRF/LRX is sent or delivery is invalidated.

**Repository rule:** Planned production and test changes add no comments or doc strings. Existing unrelated comments remain untouched.

---

## APIs verified against the current tree

### Existing server APIs

| Fact/API | Verified location |
|---|---|
| `AccessPrivilege` is an enum; GameMaster receives every enum value while lower levels use explicit allow-lists | `Goose/AccessLevels.cs:5-75` |
| Attributed commands declare privileges and receive current `Player`/`GameWorld` through `CommandContext` | `Goose/Commands/CommandAttribute.cs:5-22`, `Goose/Commands/CommandContext.cs:3-21` |
| Command privilege is checked before `CommandEvent` creation, but mutable access can change before `Ready` executes | `Goose/EventHandler.cs:206-247` |
| Non-command packet definitions support `Restricted(Type, AccessPrivilege)` and check privilege before event creation | `Goose/EventHandler.cs:66-86,250-282` |
| Built-in packet registrations are seeded in `_SeedCommands` | `Goose/EventHandler.cs:98-104,115-164` |
| Window frames currently end at `Custom = 28`; window types currently end at `Generic` | `Goose/Window.cs:24-54,62-81` |
| `Window.Create` allocates a player-local ID and sends MKW, `Populate`, then ENW | `Goose/Window.cs:102-128` |
| `Window.Close` removes server state before CLW; WBC resolves a tracked window by ID and invokes virtual `Clicked` | `Goose/Window.cs:315-330`, `Goose/Events/WindowButtonClickEvent.cs:17-63` |
| Players own `LastWindowID` and `Windows`; normal construction starts at ID 1000 with an empty window list | `Goose/Player.cs:388-389,493-516` |
| Existing window packet builders are `P.MakeWindow`, `P.EndWindow`, and `P.CloseWindow` | `Goose/Packets.cs:714-733` |
| Current map IDs/names are available on the game thread through `MapHandler.Maps` | `Goose/MapHandler.cs:22-27,89-101` |
| `GameWorld` owns handlers/database and exposes monotonic `Stopwatch` timestamp/frequency | `Goose/GameWorld.cs:25-47,77-88,100-130` |
| Incoming buffered transport is capped at 64 KiB and split on `\x1` before event dispatch | `Goose/GameWorld.cs:56-68,502-600` |
| `GameWorld.Update` currently runs only `EventHandler.Update` | `Goose/GameWorld.cs:602-611` |
| `GameWorld.Send` appends `\x1`; send failure calls `LostConnection` | `Goose/GameWorld.cs:614-643` |
| Socket output is ASCII, has a 1 MiB buffered-send ceiling, and exposes current `SendBuffer.Count` | `Goose/Player.cs:39-41,2735-2768` |
| `LostConnection` is idempotent per socket but currently disconnects before delayed logout removes the player | `Goose/GameWorld.cs:458-489`, `Goose/Events/LogoutEvent.cs:21-98` |
| `PlayerHandler.GetPlayer(Socket)` resolves the currently registered player; removal clears socket/name/login-ID indexes | `Goose/PlayerHandler.cs:51-59,82-90,121-127` |
| `Database.Enqueue` runs action and callback on the DB thread; a later `Execute` is an ordered test barrier | `Goose/Database.cs:116-169,171-224` |
| `Database.PendingCount` excludes in-flight work; `Database.Stop` can time out with a worker still alive | `Goose/Database.cs:40-44,281-318` |
| Current shutdown saves pending logs and then waits/stops the database; there is no later normal update | `Goose/GameWorld.cs:398-434` |
| `LogHandler.Pending` exposes buffered entries; explicit player-ID/map-coordinate logging is available | `Goose/LogHandler.cs:9-12,19-37` |
| In-game and console set-access paths directly mutate `Player.Access` | `Goose/Commands/SetAccessCommand.cs:6-22`, `Goose/Console/Commands/SetAccessCommand.cs:53-81` |
| Unit fixtures provide capturing players/direct dispatch; integration tests already use temporary DBs and ordered barriers | `TestSupport/TestWorldFixture.cs:93-143`, `Goose.IntegrationTests/DatabaseTransactionTests.cs:5-74` |
| Production internals are visible to both server test assemblies | `Goose/Goose.csproj:19-25` |

### Revised Part 1 APIs consumed unchanged

| Contract consumed by Part 2 | Revised Part 1 plan reference |
|---|---|
| New timestamps and query boundaries are canonical UTC `Int64` ticks; no provider `DateTime` conversion belongs in Part 2 | “Canonical timestamp storage”; Task 1 |
| Raw `rowid`, type, player/other/map IDs, and coordinates remain signed `Int64` with integer-validity flags | “Raw persisted numeric fields”; Task 4 projection contract |
| Known metadata/filter IDs remain `Int32`; unknown broad-search row types remain renderable through generic fallback | “Raw persisted numeric fields”; “Event IDs and broad searches” |
| `LogOtherIdKind` describes stored `otherid`; `LogRelatedEntity` is a separate projected label/kind/`long?` ID/name/quick-filter value | “Descriptor semantics versus row projection”; Task 2 |
| `LogFreshSearchInput` has only fresh filters; `ValidateFresh` returns immutable `LogSearchQuery` with resolved `ParticipantId` and no client cursor | Task 3 “Pin the models” and “Implement validation” |
| `LogSearchQuery.WithCursor(LogPageCursor)` preserves validated filters and resolved participant identity | Task 3 model contract |
| `LogPageCursor` is internal and holds snapshot ceiling plus nullable exact tick/rowid boundary; Part 1 has no string/Base64 cursor API | “Internal paging contract”; Task 5 |
| `LogQueryEngine.Execute(SQLiteConnection, LogSearchQuery)` returns immutable rows, `HasMore`, and internal `NextCursor` | Task 5 “Implement `LogQueryEngine`” |
| A boundary-less `LogPageCursor.ForFirstPage(snapshot)` replays page one under the original snapshot | Task 5, immediately after the engine execution sequence |
| `LogQueryRow` contains UTC ticks, signed-`Int64` raw fields, descriptor labels/group/`OtherIdKind`, projected primary/related/map entities, summary, and original text | Task 4 “Write resolution and exact projection tests” |

Part 2 must not decode internal cursor fields from the wire, re-resolve participant names during Page, reinterpret `otherid`, truncate raw numerics, or recreate summaries.

---

## Fixed server integration contracts

### Authorization and viewer lifecycle

- Append `AccessPrivilege.ViewLogs` after existing privilege values. GameMaster receives it via `Enum.GetValues`; lower allow-lists remain unchanged.
- Add `Window.WindowFrames.LogViewer = 29` and append `Window.WindowTypes.LogViewer`.
- `/logs` is attributed with `ViewLogs`, but both `LogsCommand.Execute` and `LogViewerWindow.Open` also require current `Player.State == Ready` and current `HasPrivilege(ViewLogs)`. A revocation between dispatch and `Ready` opens nothing and sends nothing.
- One `LogViewerWindow` is tracked per player. Reopen removes/CLWs the old viewer before publishing the new one.
- Close, replacement, access loss, disconnect, and shutdown clear that viewer’s search session, page-token dictionaries, and pending delivery. They do not cancel running SQLite work or release DB query capacity early.

### Metadata

Variable metadata text uses strict UTF-8 standard Base64:

```text
LMT{windowId},{knownTypeId},{groupB64},{labelB64}
LMM{windowId},{knownMapId},{mapNameB64}
LMD{windowId},{defaultStartUnixMs},{defaultEndUnixMs}
```

Known descriptor/map IDs are `Int32`. LMT is ID ordered, includes retired 14/ViewLogs 10013, and uses exact group display strings `Communication`, `Sessions/Security`, `Social`, `Items/Economy`, `GM Actions`, `Other/Retired`. LMM is ascending map ID. LMD is last, with one captured UTC `end` and `start = end - 86_400_000`; ENW follows through normal window creation.

### Explicit LQS actions

Fresh and Page are distinct, case-sensitive actions with distinct exact field counts after removing `LQS`:

```text
LQS{windowId},{requestId},F,{startMs},{endMs},{participantB64},{mapId},{typeIds},{textB64}
LQS{windowId},{requestId},P,{pageTokenB64Url}
```

- Fresh has exactly **9** comma-separated fields after `LQS`; Page has exactly **4**.
- `windowId` and `requestId` are invariant positive `Int32` decimals.
- Action is exactly `F` or `P`; no empty/lowercase/unknown action is accepted.
- Complete LQS length is at most 8,192 ASCII characters.
- Fresh start/end are signed `Int64` Unix milliseconds; map and pipe-separated selected known IDs are non-negative `Int32`. Type element count is at most `LogEventRegistry.Known.Count`; semantic registry validation remains Part 1’s job.
- Fresh participant/text decode through strict standard Base64 to at most 64/4,096 UTF-8 bytes. Empty values are valid. Fresh creates `LogFreshSearchInput` and is the only action that calls `ValidateFresh`, consumes the one-second rate limit, and writes ViewLogs audit.
- Page has no filters. Its token is exactly one canonical unpadded Base64Url encoding of 16 random bytes: 22 characters from `[A-Za-z0-9_-]`. It resolves in the current viewer session before capacity reservation or DB enqueue. Unknown, modified, cross-viewer, old-generation, or old-filter tokens receive `Log page token is invalid or expired.` and do no DB work/audit/rate mutation.
- If a bounded malformed packet exposes positive window/request IDs and that viewer is currently tracked/authorized, return `Malformed log search request.`; otherwise discard silently.

### Strict text and random token helpers

- `ProtocolTextCodec` uses canonical padded RFC 4648 Base64 over `UTF8Encoding(false, true)`. Decode rejects whitespace, invalid alphabet/padding/padding bits, non-multiple-of-four length, invalid UTF-8, and caller byte-limit overflow.
- `LogPageTokenCodec.Create()` uses `RandomNumberGenerator.Fill` for exactly 128 random bits and canonical unpadded Base64Url. On the negligible dictionary collision, regenerate before publication.
- `LogPageTokenCodec.IsCanonical` validates exact length/alphabet/round-trip form without accepting standard Base64, padding, whitespace, or alternate spellings.
- Page tokens are opaque capabilities but authorization still applies. Dictionary membership is required; cryptographic shape alone grants nothing.

### Server-bound search session

Each open `LogViewerWindow` owns at most one `LogViewerSearchSession`:

- immutable validated base `LogSearchQuery` with `Cursor == null`, including the participant ID resolved by the successful Fresh;
- a monotonically increasing viewer-local session generation;
- the first page’s immutable result and, when derivable from Part 1 page state, its boundary-less replay cursor;
- an ordinal dictionary from random token to nullable internal `LogPageCursor`;
- a reverse internal-cursor lookup so replaying a page does not mint unbounded duplicate tokens.

The first-page current token maps to `null`. Resolving that token means page one of this session, never “new Fresh”: use the stored boundary-less `LogPageCursor.ForFirstPage(originalSnapshot)` when the fresh result supplied a continuation snapshot; for a one-page result with no `NextCursor`, replay the immutable stored first-page result. Thus P-to-page-one cannot capture a new snapshot or include post-Fresh inserts.

For a successful Fresh response candidate:

1. Keep the existing committed session while validation/query/serialization run.
2. Build a new candidate session from the validated base query and first page.
3. Generate a nonempty current token mapped to null.
4. If `HasMore`, generate/reuse a next token mapped to Part 1 `NextCursor`.
5. Prebuild and size-check the complete response.
6. Only then atomically replace the old committed session and begin delivery.

Validation failure, DB failure, stale completion, serialization/row/response oversize, or inability to start delivery preserves the prior committed session. A successful Page never changes filters/base participant/session generation; it reuses the supplied token as current and publishes a next token only for Part 1 `NextCursor`.

### LRB/LRD/LRF/LRX and row JSON

```text
LRB{windowId},{requestId}
LRD{windowId},{requestId},{rowOrdinal},{chunkIndex},{chunkCount},{base64Segment}
LRF{windowId},{requestId},{hasMore0or1},{currentPageTokenB64Url},{nextPageTokenB64Url}
LRX{windowId},{requestId},{safeMessageB64}
```

- `rowOrdinal` and `chunkIndex` are zero-based non-negative `Int32`; `chunkCount` is positive `Int32`.
- Every successful page has a nonempty 22-character current token. Next is nonempty exactly when `hasMore == 1`; otherwise the final field is empty.
- For Fresh, current is newly issued for page one. For Page, current is exactly the successfully resolved supplied token.
- LRX uses standard UTF-8 Base64. LRF page tokens are already Base64Url and are not nested in `ProtocolTextCodec` Base64.

Each Part 1 row is deterministically serialized as this exact JSON property order:

```json
{
  "rowId": 0,
  "utcMilliseconds": 0,
  "typeId": 0,
  "typeIsInteger": true,
  "eventLabel": "",
  "eventGroup": "",
  "otherIdKind": "Unused",
  "primary": {
    "label": "",
    "kind": "Player",
    "id": 0,
    "name": "",
    "canQuickFilter": false
  },
  "related": null,
  "map": null,
  "raw": {
    "playerId": 0,
    "playerIdIsInteger": true,
    "otherId": 0,
    "otherIdIsInteger": true,
    "mapId": 0,
    "mapIdIsInteger": true,
    "mapX": 0,
    "mapXIsInteger": true,
    "mapY": 0,
    "mapYIsInteger": true
  },
  "summary": "",
  "originalText": ""
}
```

Contracts for the JSON values:

- `rowId`, `typeId`, all non-null entity/map IDs, and all `raw` numeric values are JSON signed-`Int64` numbers end-to-end. Unknown/out-of-`Int32` broad-search types remain deliverable with generic labels.
- `utcMilliseconds` is a signed `Int64` produced from Part 1’s valid UTC ticks with `DateTimeOffset.ToUnixTimeMilliseconds`; no provider conversion or loss-prone `Int32` narrowing occurs.
- `typeIsInteger` and raw validity flags preserve Part 1 corruption metadata.
- `otherIdKind` is the stable `LogOtherIdKind` name. `related` is independently the optional Part 1 projected `LogRelatedEntity`; it is never synthesized from `otherIdKind`.
- `primary` and non-null `related` use exactly `label`, `kind`, nullable signed-`Int64` `id`, `name`, and `canQuickFilter`. `kind` uses stable `LogEntityKind` names including `StoredValue`.
- `map` is null or exactly `id` (signed `Int64`), `name`, and `canQuickFilter`.
- JSON is emitted with `Utf8JsonWriter`/`System.Text.Json`, `Indented=false`, `SkipValidation=false`, `JavaScriptEncoder.Default`, explicit nulls, invariant numbers, no BOM, and no optional/extension properties. Property order above is written explicitly.
- Serialize the complete row first. Reject it if UTF-8 length exceeds 262,144 bytes; never truncate `originalText`, summary, or names.
- Standard-Base64 the complete JSON bytes once, then split that ASCII string into contiguous segments of at most 12,288 characters. Segments may split Base64 quartets; the client concatenates all chunks in index order before one Base64 decode/JSON parse.
- Prebuild LRB, every LRD chunk, and LRF before publishing LRB. The total ASCII bytes including one `\x1` delimiter per packet may not exceed 4,194,304. A row/response/build failure sends only `Result is too large to display. Narrow the search.` for size errors or `Log search failed.` for unexpected failures, preserving any prior Fresh session.

### Admission, capacity, audit, and active phases

A viewer has one active request state with phase `Querying` or `Delivering`. Any LQS while either phase is active receives `A log search is already running.` and does not mutate session/audit/rate/capacity.

Admission order:

1. Recheck current registered player, `Ready`, `ViewLogs`, exact viewer object/window ID, request ID, and no active request.
2. For P, validate token shape and resolve it against the current session/generation before any capacity/rate/audit work.
3. For F only, enforce one admitted Fresh per player per monotonic second.
4. For DB-backed work, reject when four queries are already queued/running/completed-but-not-drained.
5. Mark `Querying`, reserve one global DB slot, and enqueue immutable work.
6. If `Database.Enqueue` throws synchronously, roll back active/slot; F consumes no rate/audit.
7. After successful F enqueue, record the fresh timestamp and append exactly one ViewLogs audit. P never does either.

The audit JSON remains deterministic with `startUtcMilliseconds`, `endUtcMilliseconds`, decoded participant, map ID, sorted/distinct event IDs, represented group labels (or `["All"]`), and decoded text. It snapshots GM ID/map/X/Y through the explicit `LogHandler.Log` overload and remains pending/persisted-only.

When a DB completion is drained, release the global slot exactly once before stale/session/delivery handling. A current outcome transitions the viewer from `Querying` to `Delivering`; it does not become idle until final LRF/LRX or invalidation. Slow delivery therefore does not consume one of four DB slots, but it still blocks that viewer.

### Paced delivery

- `GameWorld.Update` order is exactly: `EventHandler.Update` → completion drain → log delivery pump.
- The delivery pump has one global 32,768-byte budget per update, counting ASCII packet bytes plus each transport delimiter.
- A packet is sent only if it fits the remaining budget. Because every LRD segment is at most 12 KiB plus bounded headers, a queued response always makes progress from a full budget.
- A delivery pauses while `player.SendBuffer?.Count > 262,144`; it remains queued and active. Other viewers are serviced round-robin so one slow socket cannot block all deliveries.
- Recheck stopping, registered player/socket identity, `Ready`, `ViewLogs`, exact viewer object/window ID, request ID/Delivering phase, and session generation before each packet and immediately after each `world.Send`.
- Final LRF atomically completes successful delivery and clears active state while retaining the committed session. Final LRX clears active state while retaining the prior session. Any lifecycle failure discards remaining packets, clears active delivery, and clears the session when caused by close/replacement/access loss/disconnect/shutdown.
- Partial LRB/LRD without LRF is intentionally harmless: Part 3 stages by window/request and commits only at LRF.

### Publication and teardown boundaries

- **Window:** publish in `player.Windows` before MKW. Remove and clear session/delivery before CLW. Client close removes without CLW echo.
- **Fresh session:** prior session remains committed through DB work and response prebuild. Replacement occurs atomically only after a deliverable Fresh response is fully validated and prebuilt.
- **Page capability:** token dictionary lookup happens on the game thread before enqueue. Work receives immutable base query/cursor/session generation; DB code never reads viewer dictionaries.
- **DB work:** F carries immutable `LogFreshSearchInput`; its one work item calls `ValidateFresh` then `Execute`. P carries the immutable already-validated base query plus resolved internal cursor and calls only `Execute`.
- **Completion:** DB callback only attempts `GameWorld.EnqueueCompletion`; no player/window/session/send/capacity mutation occurs there.
- **Stopping gate:** `GameWorld.Stop` atomically marks stopping before saves or DB shutdown, closes completion publication, clears queued completions and log deliveries/sessions, then runs existing persistence/DB stop. `EnqueueCompletion` returns false and drops publications after stopping begins, including callbacks from a DB worker that outlives a `Database.Stop` timeout. Clear queues again after `Database.Stop`; never assume it drained.
- **Disconnect:** after the pending-logout idempotence guard, resolve `PlayerHandler.GetPlayer(sock)` before socket removal; if found, invalidate viewer/session/delivery without sends, then disconnect/schedule logout. Running DB work retains capacity until its completion is drained or the world is abandoned during stop.

---

## Task 1: Define explicit action parsing, strict codecs, page capabilities, and bounded row serialization

**Files:**
- Create: `Goose/ProtocolTextCodec.cs`
- Create: `Goose/Logs/LogPageTokenCodec.cs`
- Create: `Goose/Logs/LogSearchPacket.cs`
- Create: `Goose/Logs/LogRowJsonSerializer.cs`
- Create: `Goose/Logs/LogProtocolPackets.cs`
- Create: `Goose.Tests/ProtocolTextCodecTests.cs`
- Create: `Goose.Tests/LogPageTokenCodecTests.cs`
- Create: `Goose.Tests/LogSearchPacketTests.cs`
- Create: `Goose.Tests/LogRowJsonSerializerTests.cs`
- Create: `Goose.Tests/LogProtocolPacketTests.cs`

### Step 1: Write failing codec/action/token tests

`ProtocolTextCodecTests` covers empty/ASCII/delimiters/control/full Unicode round trips; canonical padded vectors; malformed alphabet/whitespace/length/padding/padding bits/UTF-8; exact UTF-8 byte limits; and non-throwing failure.

`LogPageTokenCodecTests`:

1. Generate many tokens; every token is 22-character canonical unpadded Base64Url and decodes to exactly 16 bytes.
2. Use an injectable RNG in tests to prove all 128 bits are represented and collision retry is handled by session publication.
3. Reject 21/23 characters, `+`, `/`, `=`, whitespace, altered alphabet, and noncanonical encodings.
4. Flip one character without changing length and prove it remains syntactically canonical but is a different dictionary key.

`LogSearchPacketTests` pins both exact grammars:

1. Parse valid F into immutable identity/action/Part 1 `LogFreshSearchInput`; no token/cursor exists.
2. Parse valid P into identity/action/opaque token; no fresh filter model exists.
3. Reject F with 8/10 fields and P with 3/5 fields, action case changes, mixed F/P fields, old cursor-bearing packet shape, overflow/non-positive identities, bad numeric/type list fields, malformed text Base64, malformed token shape, and packet length 8,193.
4. Cover signed Unix milliseconds, map/type zero, empty selections/text/participant, known-count selection maximum, and exact/one-over UTF-8 byte limits.
5. `TryIdentify` recovers only bounded positive window/request IDs for safe malformed LRX routing.

### Step 2: Write failing row/packet tests

`LogRowJsonSerializerTests` constructs revised Part 1 rows and asserts:

1. Exact property order/names/nesting/null behavior above with no BOM/whitespace.
2. Every raw numeric at `Int64.MinValue`/`Int64.MaxValue` round-trips as JSON `Int64`; known metadata IDs remain `Int32` elsewhere.
3. Unknown type above `Int32.MaxValue` retains its raw value, generic event label/group, `StoredValue` relation where projected, and full original text.
4. `LogOtherIdKind` remains distinct from an independently projected related entity for pickup/GetItem/RespawnMap/vendor/unknown examples from Part 1.
5. UTC ticks map exactly to Unix milliseconds, including sub-millisecond truncation and pre-epoch values.
6. Unicode, commas, pipes, `\x1`, NUL, and large ordinary original text survive serialize/Base64/reassemble/parse byte-for-byte.
7. Exactly 256 KiB serialized UTF-8 is accepted; one byte over returns a safe size result without truncation.

`LogProtocolPacketTests` asserts exact LMT/LMM/LMD/LRB/LRD/LRF/LRX shapes, invariant numerics, zero-based chunk indexes/ordinals, chunk count, maximum 12,288-character segment, current-token-required LRF, next-token iff `hasMore`, and exact 4 MiB response accounting including delimiters. Concatenate every LRD segment before Base64 decoding to prove chunk boundaries may split quartets.

### Step 3: Run red

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~ProtocolTextCodecTests|FullyQualifiedName~LogPageTokenCodecTests|FullyQualifiedName~LogSearchPacketTests|FullyQualifiedName~LogRowJsonSerializerTests|FullyQualifiedName~LogProtocolPacketTests"
```

### Step 4: Implement pure protocol helpers

- Keep standard text Base64 and random Base64Url token APIs separate.
- Model parsed F/P as a discriminated immutable request; impossible mixed states must not be representable.
- Parser owns framing/primitive/byte/count/token-shape checks only. It never calls Part 1 validation or accesses session dictionaries.
- `LogRowJsonSerializer` writes revised Part 1 projections explicitly with `Utf8JsonWriter`; do not use reflection-dependent property order.
- `LogProtocolPackets.BuildResponse` accepts already-chosen current/next tokens and returns either one immutable prebuilt packet list with total byte count or one safe size/build failure. It performs no session mutation.

### Helper contracts

| Helper | Trust boundary | Contract |
|---|---|---|
| `ProtocolTextCodec` | Arbitrary text fields | Canonical standard Base64/strict UTF-8 |
| `LogPageTokenCodec` | Random server capability / untrusted P token | 128-bit canonical unpadded Base64Url shape |
| `LogSearchPacket` | Untrusted LQS | Exact F/P grammar and bounded primitive fields |
| `LogRowJsonSerializer` | Immutable Part 1 row | Exact deterministic JSON or oversize failure, never truncation |
| `LogProtocolPackets` | Immutable metadata/result values | Exact packet/chunk/LRF contracts and 4 MiB preflight |

### Mutation impact

Pure helpers only. No player, viewer, session, rate, capacity, DB, audit, delivery, or shutdown state changes.

### Step 5: Green and commit

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~ProtocolTextCodecTests|FullyQualifiedName~LogPageTokenCodecTests|FullyQualifiedName~LogSearchPacketTests|FullyQualifiedName~LogRowJsonSerializerTests|FullyQualifiedName~LogProtocolPacketTests"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/ProtocolTextCodec.cs Goose/Logs/LogPageTokenCodec.cs Goose/Logs/LogSearchPacket.cs Goose/Logs/LogRowJsonSerializer.cs Goose/Logs/LogProtocolPackets.cs Goose.Tests/ProtocolTextCodecTests.cs Goose.Tests/LogPageTokenCodecTests.cs Goose.Tests/LogSearchPacketTests.cs Goose.Tests/LogRowJsonSerializerTests.cs Goose.Tests/LogProtocolPacketTests.cs
git commit -m "feat: define bounded GM log viewer protocol"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| F and P cannot be confused or smuggle each other’s fields | exact field-count/action theories |
| Page tokens are random 128-bit Base64Url capabilities, never Part 1 cursors | token codec/API tests |
| Full signed-`Int64` rows and unknown types survive wire projection | row JSON extreme/fallback tests |
| Stored `OtherIdKind` and projected relation remain separate | event projection JSON tests |
| Full ordinary text is preserved; exceptional rows/responses fail before LRB | 256 KiB/4 MiB boundary tests |
| Chunk reconstruction is deterministic despite quartet splits | chunk concatenate/decode test |

---

## Task 2: Add ViewLogs, Ready-safe `/logs`, frame 29, metadata, and empty viewer sessions

**Files:**
- Modify: `Goose/AccessLevels.cs:5-75`
- Modify: `Goose/Window.cs:24-82`
- Create: `Goose/LogViewerWindow.cs`
- Create: `Goose/Logs/LogViewerSearchSession.cs`
- Create: `Goose/Commands/LogsCommand.cs`
- Create: `Goose.Tests/LogsCommandTests.cs`
- Create: `Goose.Tests/LogViewerWindowTests.cs`
- Create: `Goose.Tests/LogViewerSearchSessionTests.cs`

### Step 1: Write failing command/window/session tests

`LogsCommandTests`:

1. Existing privilege values remain unchanged; ViewLogs appends; only GameMaster gets it by current policy.
2. Normal/Guide/etc. `/logs` is swallowed with no window/packets.
3. A Ready authorized GM opens exactly one close-only frame-29 `GM Log Viewer`.
4. A not-Ready GM opens nothing even when invoking command execution directly.
5. Dispatch `/logs` while authorized, revoke before `CommandEvent.Ready`, then run it: no window/packet.
6. Direct `LogViewerWindow.Open` also refuses not-Ready or unauthorized players.

`LogViewerWindowTests` pins MKW → ordered LMT → ascending LMM → LMD → ENW, strict Base64 map names, exact previous-24-hour range, replacement CLW-before-new-MKW, and WBC close without CLW echo.

`LogViewerSearchSessionTests` uses revised Part 1 immutable models:

1. Session retains a cursorless validated base query and resolved participant ID.
2. First token maps to null; continuation token maps to exact internal `LogPageCursor` object/value.
3. Same cursor reuses one token; a generated-token collision retries without overwriting.
4. Token lookup is ordinal and scoped to the session instance/generation.
5. Session replacement/clear makes every old token unreachable.
6. Renaming or duplicating the participant after construction cannot alter stored `ParticipantId`.

### Step 2: Run red

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogsCommandTests|FullyQualifiedName~LogViewerWindowTests|FullyQualifiedName~LogViewerSearchSessionTests"
```

### Step 3: Implement authoritative open/session lifecycle

- Append enum values without renumbering.
- `LogsCommand.Execute` rechecks Ready and privilege immediately before calling `Open`; `Open` repeats both checks and returns success/failure.
- Opening closes all old viewers from a snapshot, which clears session/active/delivery state before CLW, then creates/adds/sends one replacement.
- `LogViewerSearchSession` is server-only and never serializes internal cursors. It owns generation, immutable base, first-page replay state, token dictionaries, and collision-safe issue/lookup.
- Viewer close clears session immediately. Query/delivery cancellation remains discard-only when later tasks attach work.

### Mutation impact

- **Access:** ViewLogs is GM-only under existing generated/explicit sets.
- **Open:** Only Ready/currently authorized players mutate `LastWindowID`/`Windows`.
- **Replacement/close:** Session and all token capabilities die with the old viewer before client notification.
- **Metadata:** Read-only snapshots of registry/maps; no DB query/audit occurs.

### Step 4: Green and commit

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogsCommandTests|FullyQualifiedName~LogViewerWindowTests|FullyQualifiedName~LogViewerSearchSessionTests"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/AccessLevels.cs Goose/Window.cs Goose/LogViewerWindow.cs Goose/Logs/LogViewerSearchSession.cs Goose/Commands/LogsCommand.cs Goose.Tests/LogsCommandTests.cs Goose.Tests/LogViewerWindowTests.cs Goose.Tests/LogViewerSearchSessionTests.cs
git commit -m "feat: open Ready-authorized log viewer sessions"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Attribute-time authorization is not trusted at execution/open time | revoke-before-Ready/direct-open tests |
| Exactly one frame-29 viewer/session exists | replacement tests |
| Reopen/close destroys all old capabilities | old-session token tests |
| Session base fixes participant ID and filters | immutable-base/name-change tests |
| Metadata remains ordered/delimiter-safe and ends at LMD | packet-order/round-trip tests |

---

## Task 3: Add stopping-safe game-thread completions and delivery ordering hooks

**Files:**
- Modify: `Goose/GameWorld.cs:25-47,100-130,398-434,602-611`
- Create: `Goose.Tests/GameWorldCompletionQueueTests.cs`
- Create: `Goose.Tests/GameWorldStoppingTests.cs`

### Step 1: Write failing completion/stopping tests

`GameWorldCompletionQueueTests`:

1. Background publication runs only on the thread that calls `Update`.
2. Multiple completions run FIFO after due events.
3. One throwing completion is logged and does not block the rest.
4. A DB callback publishes, a later `Database.Execute` proves DB completion, and no game mutation occurs until `Update`.
5. A test delivery hook observes final order events → completions → delivery pump.

`GameWorldStoppingTests` avoids a two-minute timeout:

1. Queue completions and delivery work, invoke the internal stopping transition, and assert both queues clear without execution/send.
2. Publication racing the stopping transition is serialized: it is either accepted before and then cleared, or rejected after; it can never remain queued.
3. Hold a fake/test DB callback publication until after stopping begins, then invoke it and assert `EnqueueCompletion` returns false.
4. Invoke another delayed publication after the test’s simulated `Database.Stop` return boundary and assert it is still rejected. This directly models the real timeout case without waiting two minutes.
5. Empty/repeated stopping/clearing is idempotent.

### Step 2: Run red

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~GameWorldCompletionQueueTests|FullyQualifiedName~GameWorldStoppingTests"
```

### Step 3: Implement publication gate and update order

- Add a private completion queue plus one lock guarding stopping transition versus enqueue; `EnqueueCompletion` returns `bool`.
- Under the gate, reject once stopping is true; otherwise enqueue. The stopping transition sets the flag and clears queued completions under the same gate.
- Drain FIFO after `EventHandler.Update`, isolating/logging each callback exception.
- Add the log-delivery pump call after completion drain; until Task 5 it is an empty service hook.
- At the first line of `GameWorld.Stop`, enter stopping and clear completions/deliveries/sessions before player/log saves or any database wait/stop. Clear again after `Database.Stop`, regardless of whether it drained or timed out.
- DB callbacks treat a false publication result as terminal abandonment and retain no retry/reference.

### Mutation impact

- **Thread ownership:** Completion actions remain game-thread-only.
- **Tick order:** queued close/access events beat completion; completion can create delivery; delivery may send later in the same update within budget.
- **Stopping:** no new completion becomes reachable once teardown starts, even if the DB worker survives stop timeout.
- **Teardown:** queued response/session references clear without sends; no assumption that `PendingCount` or `Database.Stop` proves all callbacks finished.

### Step 4: Green and commit

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~GameWorldCompletionQueueTests|FullyQualifiedName~GameWorldStoppingTests"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/GameWorld.cs Goose.Tests/GameWorldCompletionQueueTests.cs Goose.Tests/GameWorldStoppingTests.cs
git commit -m "feat: add stopping-safe game-thread completions"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| DB publications never execute game logic off-thread | thread/barrier tests |
| Events run before completions and delivery | ordering test |
| Stop/enqueue race cannot strand work | gate race test |
| A callback after stop timeout is harmless | delayed post-stop publication test |
| Teardown clears, never delivers, queued responses | stopping queue test |

---

## Task 4: Implement explicit Fresh/Page async flow, server-bound tokens, limits, and Fresh-only auditing

**Files:**
- Modify: `Goose/EventHandler.cs:83-86,115-164`
- Modify: `Goose/GameWorld.cs:29-47,100-130`
- Create: `Goose/Events/LogQueryEvent.cs`
- Create: `Goose/Logs/LogSearchAuditFormatter.cs`
- Create: `Goose/Logs/LogSearchService.cs`
- Modify: `Goose/LogViewerWindow.cs`
- Create: `Goose.Tests/LogSearchAuditFormatterTests.cs`
- Create: `Goose.IntegrationTests/LogSearchServerFixture.cs`
- Create: `Goose.IntegrationTests/LogSearchAdmissionTests.cs`
- Create: `Goose.IntegrationTests/LogSearchSessionPagingTests.cs`

### Step 1: Write failing admission/audit tests

`LogSearchAuditFormatterTests` pins compact deterministic JSON, exact escaping/order, sorted/distinct type IDs, registry-order groups, and `["All"]`.

`LogSearchAdmissionTests` uses real SQLite/Part 1 APIs, fake clocks, online capturing GMs, and DB gates:

1. Restricted dispatch swallows Normal LQS; service recheck catches revoke between event creation and `Ready`.
2. Valid F marks Querying, reserves one DB slot, calls `ValidateFresh` then `Execute` on the same connection/work item.
3. P never calls `ValidateFresh`; it executes only the stored base with resolved internal cursor.
4. Same viewer rejects another F/P while Querying or Delivering.
5. F at 999 ms is rate-limited; exactly 1,000 ms is admitted; replacement cannot bypass per-player clock.
6. P is never rate-limited and never updates the F timestamp.
7. Four blocked DB queries include the running item and reject a fifth; draining one completion releases one slot even though that viewer begins slow delivery.
8. Synchronous enqueue failure rolls back Querying/capacity and consumes no F rate/audit.
9. Every successfully enqueued F appends one exact pending ViewLogs entry, including validation/DB/oversize failure; P and every rejected path append none.
10. Pending audit is absent from the same persisted-only F result.
11. Malformed identifiable current request receives malformed LRX; invalid/stale identity is silent.
12. Canonical-looking unknown P token rejects before DB/capacity/audit/rate.

### Step 2: Write failing session/token paging tests

`LogSearchSessionPagingTests`:

1. Successful F commits a new session only after response prebuild and returns current page-one token plus next token iff `HasMore`.
2. Validation/DB/serialization/row-size/response-size failure preserves an existing committed session and its tokens.
3. P with the next token returns that token as current and a server-issued next token derived from Part 1 `NextCursor`.
4. P with page-one token (mapped null) replays the original snapshot. Insert a newer and an older matching row after F/page two, request Previous-to-page-one, and assert original page-one row IDs/order.
5. Previous-to-page-one adds no ViewLogs audit and does not hit F rate limiting.
6. Change participant name and create an ambiguous duplicate after F; P still uses stored resolved `ParticipantId` and succeeds unchanged.
7. Flip one token character while retaining length; reject before DB.
8. Token from another viewer, another successful Fresh/filter set, cleared/reopened viewer, or old session generation rejects before DB.
9. Old token cannot select a different base query/filter/participant, even if its internal cursor shape would be valid.
10. Repeated valid P reuses the token for an equal next cursor instead of growing duplicates.
11. Sending `P,<garbage>` cannot perform a fresh query, bypass Fresh audit, or create a session; only issued membership authorizes Page.
12. Broad search returns an unknown signed-`Int64` event type through fallback JSON without token/session failure.

### Step 3: Run red

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogSearchAuditFormatterTests"
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogSearchAdmissionTests|FullyQualifiedName~LogSearchSessionPagingTests"
```

### Step 4: Implement immutable work/outcome/session publication

- Register `("LQS", Restricted(typeof(LogQueryEvent), ViewLogs))`; `LogQueryEvent.Ready` requires Ready and delegates raw input.
- Add `GameWorld.LogSearches` with injectable UTC/monotonic/token RNG seams for tests.
- Service owns game-thread-only global DB count and per-player last-F timestamp.
- Viewer owns active `(requestId, phase, sessionGeneration)` plus committed session.
- Immutable work variants:
  - Fresh: identity/location/audit snapshot + `LogFreshSearchInput`.
  - Page: identity + captured session reference/generation + immutable base query + nullable token cursor resolved on game thread.
- Fresh DB action calls `ValidateFresh`; on success calls engine in the same action and returns validated base query plus page. Page action never validates and calls engine with `BaseQuery.WithCursor(resolvedCursor)`; null page-one token is translated through session first-page replay state, never treated as a new Fresh.
- Immutable callback outcome carries no mutable handler/window lookup. Callback only attempts stopping-aware completion publication.
- Completion releases global DB capacity first. It then verifies viewer/request Querying and captured session generation/lifecycle before building a candidate session/response or LRX delivery.
- For F, append audit only after `Database.Enqueue` returns. For P, no audit/rate mutation anywhere.
- Prebuild all chunks before committing a Fresh candidate session. Commit candidate tokens/session and transition to Delivering atomically. Failures transition to Delivering only for safe LRX and retain prior session.

### Mutation impact

- **F:** Reserves DB capacity, consumes rate/audits after successful enqueue, and may atomically replace session only after successful query/prebuild.
- **P:** Can only use current-session capabilities; never accepts filters/names, validates Fresh, rate-limits, or audits.
- **Capacity:** Counts DB work through completion drain, not delivery duration.
- **Viewer:** Querying and Delivering both block new work. Session generation prevents old completions/tokens from attaching to a replacement search.
- **Participant/filter identity:** Immutable Part 1 base query fixes resolved participant and filters for every P.

### Step 5: Green and commit

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogSearchAuditFormatterTests"
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogSearchAdmissionTests|FullyQualifiedName~LogSearchSessionPagingTests"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/EventHandler.cs Goose/GameWorld.cs Goose/Events/LogQueryEvent.cs Goose/Logs/LogSearchAuditFormatter.cs Goose/Logs/LogSearchService.cs Goose/LogViewerWindow.cs Goose.Tests/LogSearchAuditFormatterTests.cs Goose.IntegrationTests/LogSearchServerFixture.cs Goose.IntegrationTests/LogSearchAdmissionTests.cs Goose.IntegrationTests/LogSearchSessionPagingTests.cs
git commit -m "feat: bind log paging to server search sessions"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Only F validates filters, consumes rate, and audits once | F/P call and audit matrix |
| P token membership binds viewer/session/filter/participant | cross-scope/old-generation tests |
| Garbage P cannot masquerade as unaudited Fresh | audit-bypass test |
| Failed F preserves committed session | failure matrix |
| Successful F replaces session only after full prebuild | publication-order test |
| Previous page one preserves snapshot after inserts | post-insert replay test |
| Participant rename/ambiguity cannot alter P semantics | resolved-ID test |
| Global four releases at completion drain, independent of delivery | blocked-query/slow-delivery test |

---

## Task 5: Pace bounded delivery and harden close/access/disconnect/shutdown behavior

**Files:**
- Modify: `Goose/Logs/LogSearchService.cs`
- Modify: `Goose/GameWorld.cs:398-489,602-643`
- Modify: `Goose/Commands/SetAccessCommand.cs:6-22`
- Modify: `Goose/Console/Commands/SetAccessCommand.cs:53-81`
- Modify: `Goose/LogViewerWindow.cs`
- Create: `Goose.IntegrationTests/LogSearchDeliveryTests.cs`
- Create: `Goose.IntegrationTests/LogSearchLifecycleTests.cs`
- Modify: `Goose.IntegrationTests/LogSearchServerFixture.cs`

### Step 1: Write failing paced-delivery tests

`LogSearchDeliveryTests`:

1. After DB barrier but before `Update`, no LRB/LRD/LRF/LRX is sent.
2. One update sends no more than 32,768 bytes including delimiters; a multi-update large response retains Querying→Delivering→idle phases correctly.
3. Rows with large but <=256 KiB serialized originals produce multiple ordered <=12 KiB LRD segments; reconstruct exact JSON/original text.
4. One-byte-over row and one-byte-over 4 MiB response send only safe LRX, no LRB/partial LRD, and preserve prior session.
5. Empty page sends LRB then LRF with nonempty current token and empty next.
6. A 51-row page emits 50 row JSONs and LRF with current/next tokens; unknown Int64 type/raw fields survive.
7. Validation error/database exception emits paced LRX only; NLog retains full DB exception while client text is generic.
8. Set `SendBuffer.Count` to 262,145: delivery sends nothing and stays active; reduce to threshold or below and it resumes.
9. A permanently slow/would-block socket never receives more than per-update budget and log delivery never drives buffer near the existing 1 MiB disconnect ceiling.
10. Two viewers are round-robin serviced so a paused viewer does not starve another.
11. LRB/chunks across updates without LRF never constitute a committed client result; only final LRF completes the server request/session response contract.
12. Global DB capacity is already free while the response is still Delivering, but the same viewer’s new LQS is rejected.

### Step 2: Write failing lifecycle tests

`LogSearchLifecycleTests` gates DB work or pauses delivery and covers:

1. WBC close during Querying or after partial LRD clears session/delivery; DB completion releases capacity and sends nothing further.
2. `/logs` replacement invalidates old query/delivery/tokens even with reused request ID.
3. In-game/console access downgrade immediately closes/clears; direct access mutation is caught before the next packet send.
4. Revoke after LRB but before remaining chunks: no LRF is sent, so staged client rows cannot commit.
5. `LostConnection` first resolves the current player from the socket, invalidates without CLW, then disconnects; delayed logout registration cannot permit delivery.
6. Player not Ready, removed/replaced registration, wrong viewer object/ID, wrong request/phase, or changed session generation discards delivery.
7. Every stale path releases DB capacity exactly once at completion drain; close/revoke/disconnect never release a running slot early.
8. Send failure during any packet triggers disconnect invalidation and stops the rest in that update.
9. Stop during Querying or Delivering clears completions/deliveries/sessions, sends nothing, and later callbacks are rejected even if DB stop timed out.
10. Admitted F audit is saved by existing shutdown persistence; P/rejected work contributes no audit.

### Step 3: Run red

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogSearchDeliveryTests|FullyQualifiedName~LogSearchLifecycleTests"
```

### Step 4: Implement fair delivery pump and lifecycle hooks

- Store immutable prebuilt response packets in per-viewer delivery state; do not regenerate JSON while pumping.
- Pump globally with 32 KiB budget and round-robin response states. Pause a state above the 256 KiB send-buffer watermark without clearing it.
- Before/after every send, apply the full lifecycle predicate. Final LRF/LRX alone clears Delivering; partial abandonment clears state/session according to lifecycle cause.
- Add `OnAccessChanged`: when ViewLogs is absent, clear delivery/session and close tracked viewers before returning from both known access commands. Completion-time/direct-mutation checks remain mandatory.
- Strengthen `LostConnection`: after idempotence guard, capture `PlayerHandler.GetPlayer(sock)` while socket mapping still exists; invalidate that player without sends; only then call `GameServer.Disconnect` and schedule logout.
- `OnStopping` clears all service rate/session/delivery references without sends. Completion publication remains governed by Task 3’s stopping gate.
- Never release global DB capacity from close/access/disconnect/pump. Only completion drain or synchronous enqueue rollback owns that transition.

### Mutation impact

- **Delivery:** Immutable response state may span ticks; at most 32 KiB is added globally per update and slow buffers pause above 256 KiB.
- **Viewer active state:** Remains busy through LRF/LRX; DB capacity and viewer availability intentionally diverge after completion.
- **Session:** Successful Fresh session survives final LRF; old session survives failed Fresh LRX; close/replacement/access/disconnect/stop destroys it.
- **Access/disconnect:** Known access changes invalidate synchronously. Socket identity is resolved before removal, closing the delayed-logout gap.
- **Shutdown:** Stops publication first, clears queued work, and is safe even when DB stop does not drain.

### Step 5: Green, full verification, and commit

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogSearchDeliveryTests|FullyQualifiedName~LogSearchLifecycleTests"
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~ProtocolTextCodecTests|FullyQualifiedName~LogPageTokenCodecTests|FullyQualifiedName~LogSearchPacketTests|FullyQualifiedName~LogRowJsonSerializerTests|FullyQualifiedName~LogProtocolPacketTests|FullyQualifiedName~LogsCommandTests|FullyQualifiedName~LogViewerWindowTests|FullyQualifiedName~LogViewerSearchSessionTests|FullyQualifiedName~GameWorldCompletionQueueTests|FullyQualifiedName~GameWorldStoppingTests|FullyQualifiedName~LogSearchAuditFormatterTests"
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogSearchAdmissionTests|FullyQualifiedName~LogSearchSessionPagingTests|FullyQualifiedName~LogSearchDeliveryTests|FullyQualifiedName~LogSearchLifecycleTests"
dotnet build Goose.sln --no-restore
dotnet test Goose.Tests/Goose.Tests.csproj --no-build --no-restore
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-build --no-restore
dotnet test Goose.sln --no-build --no-restore
```

```bash
git add Goose/Logs/LogSearchService.cs Goose/GameWorld.cs Goose/Commands/SetAccessCommand.cs Goose/Console/Commands/SetAccessCommand.cs Goose/LogViewerWindow.cs Goose.IntegrationTests/LogSearchServerFixture.cs Goose.IntegrationTests/LogSearchDeliveryTests.cs Goose.IntegrationTests/LogSearchLifecycleTests.cs
git commit -m "feat: pace and harden log viewer delivery"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Full response is prebuilt before LRB and ordinary text is never truncated | large-row/oversize tests |
| At most 32 KiB sends per update and >256 KiB buffers pause | budget/watermark tests |
| Slow delivery cannot consume DB capacity or admit same-viewer work | phase/capacity test |
| Client cannot commit a partial response | close/revoke mid-delivery tests |
| Lifecycle is checked after every send | send-failure/revoke tests |
| Disconnect invalidates while player is still socket-resolvable | LostConnection ordering test |
| Shutdown remains safe with an outliving DB worker | stopping callback test |

---

## Final design alignment and red-team review

### Design alignment checklist

- **Part 1 compatibility:** Uses UTC ticks, signed-`Int64` raw rows, `ValidateFresh`, immutable base queries, internal `LogPageCursor`, `NextCursor`, separate `OtherIdKind`/projected relations, and generic unknown fallback. No old DateTime provider or string cursor assumptions remain.
- **Authorization:** `/logs` is checked at dispatch, execution, and direct open. LQS is restricted and rechecked at admission/completion/every send.
- **Actions:** F and P have explicit, incompatible field counts. Only F carries filters, validates, rate-limits, and audits.
- **Session binding:** P requires a random current-viewer/current-generation token. Base filters and resolved participant ID are immutable; no client cursor/filter can be substituted.
- **Previous:** Page-one token maps to null session cursor semantics and replays the original snapshot; post-Fresh inserts do not move page one.
- **Protocol:** Metadata remains one record per packet. Rows are deterministic chunked JSON with full Int64/raw/semantic data. LRF has current and optional next capabilities.
- **Bounds:** 256 KiB per row, 12 KiB Base64 chunks, 4 MiB prebuilt response, 32 KiB/update, and 256 KiB send-buffer pause prevent one response from triggering the 1 MiB disconnect path.
- **Concurrency:** One viewer request spans Querying plus Delivering. Four-slot capacity covers DB work only and releases on game-thread completion drain.
- **Threading:** DB callbacks publish immutable outcomes only. Events run before completions, which run before paced delivery.
- **Audit:** Exactly one ViewLogs entry per admitted F, including admitted failures; none for P or rejected requests. Garbage P cannot bypass because it cannot create/alter a session query.
- **Lifecycle:** Close/replacement/access loss/disconnect/stop clear sessions/tokens/delivery and suppress stale output without early DB-capacity release.
- **Stopping:** Publication closes before DB shutdown and remains closed if `Database.Stop` times out; queues are cleared without sends.
- **Client boundary:** Part 3 stages LRB/LRD chunks and commits only at LRF; no total count/live tail/bulk export is introduced.

### Red-team cases that must be green before Part 3

| Attack/regression | Required defense |
|---|---|
| Old cursor-bearing LQS or lowercase/mixed action | Exact F=9/P=4 grammar rejects |
| P includes replacement filters/participant | No such P fields; exact count rejects |
| Same-length one-character token modification | Canonical shape may pass, dictionary membership fails before DB |
| Token copied across viewer, Fresh filter/session, reopen, or generation | Session-local ordinal dictionary and captured generation reject |
| Participant renamed or made ambiguous after F | Stored resolved `ParticipantId`; P never validates name |
| Garbage P used to avoid F audit/rate | Cannot create query/session; rejects before DB; no data returned |
| New rows inserted before Previous-to-page-one | Null first-page token resolves original snapshot/replay state |
| P-to-page-one creates an extra ViewLogs event | P has no audit path; explicit regression test |
| Unknown/out-of-Int32 stored event/numeric values | Signed-Int64 JSON plus generic descriptor; no narrowing failure |
| `OtherIdKind` mechanically becomes related entity | Both fields serialized separately from Part 1 contracts |
| Huge original text | Full value if row/response bounds permit; otherwise safe LRX before LRB, never truncation |
| Base64 chunks split quartets | Client concatenates exact ordered segments before decoding |
| 50 large rows exceed memory/wire budget | 4 MiB preflight rejects entire response before LRB |
| Slow/would-block client approaches send cap | 32 KiB/update and pause above 256 KiB; round-robin other viewers |
| Close/revoke after LRB | Per-send lifecycle stops chunks/LRF; client cannot atomically commit |
| Delivery lasts while other searches run | DB slot already released; same viewer remains blocked |
| DB callback races queued close/access event | Event executes first; completion/delivery sees invalid state |
| Disconnect leaves player registered until logout | Resolve/invalidate from socket before disconnect/removal |
| Stop begins while callback is in flight | Locked stopping gate rejects/clears publication; no drain assumption |
| Database.Stop times out | Later callback still sees stopping and drops publication |
| Fresh query validates but serialization fails | Audit remains exactly once; old committed session survives; safe LRX only |

### Part 3 handoff contract

Part 3 may rely on the exact F/P LQS grammars, standard text Base64, 22-character page capabilities, LMT/LMM/LMD ordering, chunked deterministic row JSON, zero-based chunk indexes, LRF current/next token semantics, paced multi-tick delivery, and LRB/LRF atomic staging rule. It must discard staged rows on LRX, close, disconnect, replacement, or any request/window mismatch and must concatenate all chunks before Base64/JSON decoding.
