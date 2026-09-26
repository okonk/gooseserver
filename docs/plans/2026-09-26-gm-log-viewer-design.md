# GM Log Viewer — Design

## Goal

Add a searchable historical log viewer for game masters. A GM opens it with `/logs`, filters persisted audit rows, pages through a stable result set, reads event-specific summaries, and copies one event's details to the clipboard.

The feature spans:

- Server: `illutiagooseserver`
- Client: `Goose2ClientGodot`

It does not add live tailing, automatic refresh, bulk export, saved searches, or log retention.

## Access and opening

Add `AccessPrivilege.ViewLogs`. Game masters receive it through the existing all-privileges rule; lower access levels do not receive it by default.

`/logs` requires `ViewLogs` and opens one server-tracked `LogViewerWindow`. Add `WindowFrames.LogViewer = 29` and `WindowTypes.LogViewer`. If the GM runs `/logs` while a viewer is open, the server closes the old window and opens a fresh one. Preserving or focusing the existing client window is deferred.

The client adds a dedicated frame-29 window. The server remains authoritative for metadata, filter validation, authorization, querying, entity resolution, and event summaries. The client owns controls, table/detail presentation, paging state, and clipboard access.

Every query packet and every result delivery rechecks `ViewLogs`. Losing access, closing the window, disconnecting, or replacing the window invalidates outstanding results.

## Data freshness and UTC

The viewer searches persisted SQLite rows only. It does not merge `LogHandler.Pending` and does not force a flush. The client states that recent entries may be delayed by up to ten minutes.

Change new audit timestamps from `DateTime.Now` to `DateTime.UtcNow`. Persist timestamps canonically as UTC `DateTime.Ticks` in SQLite `INTEGER` values; the current `DateTime2` provider binding is not sortable or precision-safe. The startup migration treats parseable existing values as UTC because the deployed server has operated in UTC, rewrites them to ticks, and reports but preserves malformed values. Malformed-date rows are excluded from time-bounded searches rather than guessed.

Query boundaries and result timestamps use Unix milliseconds on the wire. Copied details use ISO 8601 UTC, for example `2026-09-26T01:23:24Z`. Date filtering uses a half-open UTC interval: `start <= log_date < end`.

## Event descriptors and legacy semantics

Add a central descriptor for every known `Log.Types` value and a generic fallback for unknown values. Each descriptor defines:

- display label;
- group;
- whether `otherid` is a player, item, guild, NPC template, map, or unused for participant matching;
- the projected related entity, which may instead come from stored text or map columns;
- semantic labels used in details;
- event-specific summary formatting.

Groups are:

- Communication: Chat, Shout, Auction, GuildChat, GroupChat, Tell
- Sessions/Security: JoinGame, LeaveGame, InvalidPassword
- Social: JoinGuild, LeaveGuild, JoinGroup, LeaveGroup
- Items/Economy: PickupItem, PlayerDropItem, GaveCredits, CreatedCustom, BuyFromVendor, SellToVendor, Rebirth, BuyGold, BuyExperience, GiveSpirit, ResetItem
- GM Actions: GetItem, ClassChange, GiveExperience, GiveGold, RespawnMap, SpawnedNPC, MacroCheck, MacroCheckConfirm, MacroCheckFailed, Ban, Kick, SetPassword, ViewLogs
- Other/Retired: retired type 14 and unknown values

`otherid` is polymorphic and must never be globally joined to players. Participant filtering always matches `playerid` and matches `otherid` only for descriptors where it represents a player. This includes guild/group counterparties, Tell, GaveCredits, GiveSpirit, GiveExperience, GiveGold, MacroCheck, Ban, Kick, SetPassword, and corrected ClassChange rows.

The formatter uses semantic labels such as Speaker/Recipient, GM/Target, or Member/Invited by. Known legacy text formats receive readable summaries. Malformed or unknown rows fall back to labeled stored fields without guessing. Examples include:

- `Alice said “Hello”`
- `Alice told Bob “Meet me in town”`
- `GM gave Bob 5,000 gold`
- `Alice bought 3 Health Potions from Merchant`
- `GM banned Bob`
- `Alice picked up Iron Sword ×1`

Spirit transfers currently write sender- and recipient-perspective rows. Both remain visible because historical rows have no transaction ID for safe deduplication.

Player names are resolved at query time. Old rows therefore show the player's current name, always accompanied by the stable ID. This is accepted behavior; name snapshots and rename history are out of scope.

## Historical repair and future logging

Extend the atomic, idempotent startup migration to:

- canonicalize parseable historical timestamps as UTC ticks before creating date indexes;
- backfill valid historical ClassChange target IDs from the first text token into `otherid` when `otherid` is zero;
- move historical RespawnMap map/coordinate values into the correct columns when its old shifted-field signature is present;
- log the count of malformed timestamp and event rows left unchanged.

Correct both current log call sites for future rows.

Append `Log.Types.ViewLogs` to the GM range. A fresh Search writes one ViewLogs event containing the GM, UTC range, participant, map, selected types/groups, and text query. Previous/Next page requests do not write additional audit events. The search audit remains subject to the normal persisted-only delay.

## Filters

The viewer supports:

- Participant: empty or an exact current character name, case-insensitive; `#playerID` is also accepted. A participant matches either player role when that role is semantically a player. Deleted players remain searchable by ID. If a name resolves to multiple database rows, the server asks the GM to use an ID.
- Event types: multi-select individual types with group-level select/clear controls.
- Map: one map or All. Current maps appear as `Name (#ID)`; raw `#mapID` input supports retired maps.
- Time: last hour, previous 24 hours, previous 7 days, previous 30 days, or a custom UTC start/end. The default is the previous 24 hours.
- Text: case-insensitive literal substring of stored `logs.text`. SQL wildcard characters are escaped and have no special meaning.

Any historical period may be searched, but one request may span at most 31 days. A text search may span at most 7 days because an arbitrary substring cannot use a normal B-tree text index. The server validates both limits.

Changing a filter resets paging. Only Search sends a fresh request; editing controls does not query automatically.

## Query and paging

Queries execute on the existing dedicated database thread. The game loop never waits for a log search.

Each page fetches 51 ordered rows, returns at most 50, and uses the extra row only to determine whether more data exists. Results order by `log_date DESC, rowid DESC`. Pagination uses a keyset boundary rather than `OFFSET`.

The first page captures `MAX(rowid)` as a snapshot ceiling. Every page includes `rowid <= snapshotCeiling`, so rows flushed while a GM is paging cannot shift the result set. A fresh Search creates a server-side viewer session containing the canonical validated filters, resolved participant ID, snapshot, and internal page boundaries.

The server issues random opaque page tokens bound to that viewer session. The first page receives its own nonempty token, so returning from page two to page one remains a Page action against the original snapshot rather than becoming a new Search. Modified, expired, cross-viewer, or cross-session tokens are rejected before querying. The client retains successful current-page tokens to implement Previous and uses the supplied next-page token for Next.

There is no exact `COUNT(*)`; the UI says that it is showing up to 50 rows and whether more rows are available.

Participant queries use an index-friendly union:

- rows where `playerid` matches;
- rows where `otherid` matches and the event descriptor declares it player-valued.

The union removes a row that matches both branches. All remaining structured, time, text, snapshot, and cursor constraints apply to both branches.

Add these indexes to fresh schema and existing databases:

- `logs(log_date DESC)`
- `logs(playerid, log_date DESC)`
- `logs(otherid, log_date DESC)`
- `logs(log_type, log_date DESC)`
- `logs(mapid, log_date DESC)`

Index creation is idempotent. The first production startup may take longer while indexes are built, so rollout requires a database backup and maintenance window.

## Concurrency

Only one request may be querying or delivering per viewer. A GM may submit a fresh search at most once per second. At most four database searches may be queued or running globally; further requests receive a busy response. The database worker remains serial, but these limits prevent unbounded queue growth ahead of saves and normal log writes.

Add a thread-safe game-thread completion queue to `GameWorld`, drained during `Update`. Database callbacks enqueue immutable search results or errors there. Only the game thread may inspect current player/window/access state or send packets. Shutdown marks the world as stopping before database teardown so late callbacks are dropped rather than retaining or mutating an abandoned world.

Rows are encoded into bounded chunks before response publication. The game thread sends them under a per-update byte budget and pauses a slow viewer when its socket buffer is elevated. This preserves complete ordinary log text without filling the existing one-megabyte send buffer. The global database slot is released when its completion is drained, while the viewer remains busy until its final success/error packet or lifecycle invalidation.

Closing a window cannot cancel SQLite work already running. Its completion is discarded after releasing query capacity. Request IDs suppress stale responses, and window IDs prevent an old response from populating a replacement viewer.

## Protocol

The protocol uses the existing delimiter-framed text transport. Strings that may contain delimiters or control characters are UTF-8 Base64. Packet length, field count, numeric ranges, Base64 validity, and selection count are validated server-side.

Opening uses the normal `MKW` packet with frame 29, followed by metadata:

- `LMT`: window ID, event type ID, encoded group, encoded label
- `LMM`: window ID, map ID, encoded map name
- `LMD`: window ID, default UTC start/end Unix milliseconds

Metadata is sent one record per packet to avoid oversized packets. Search remains disabled until `LMD`.

Client search packets use one opcode with explicit actions:

- Fresh `LQS`: window ID, request ID, `F`, UTC start/end, encoded participant, map ID, pipe-separated event type IDs, encoded text
- Page `LQS`: window ID, request ID, `P`, server-issued page token

Only Fresh is rate-limited and audited. Page carries no filters; the server uses the session-bound canonical query.

Server response packets:

- `LRB`: begin response for window/request
- `LRD`: one ordered chunk of a structured row
- `LRF`: finish response with `hasMore`, the current-page token, and an optional next-page token
- `LRX`: encoded search error

Each row is deterministic UTF-8 JSON, Base64-chunked into bounded packets. It includes row ID, UTC timestamp, raw signed numeric fields and validity flags, type, separate `otherid` semantics and projected related entity, resolved names, map/coordinates, readable summary, and original stored text. Oversized/corrupt result pages fail before `LRB`; normal pages are delivered over multiple ticks when necessary.

The client stages and reassembles chunks after `LRB`. Only a complete matching `LRF` atomically replaces the visible table, so an error, malformed chunk, close, or disconnect cannot display a partial result set. Copying details is local and sends no packet.

The LQS packet is registered as restricted by `ViewLogs`, in addition to checks in the search service and completion path.

## Client UI

The resizable viewer contains filters, a result table, and a detail panel.

Filters include UTC preset/custom controls, participant, grouped event multi-select, searchable map selector, text, Search, and Clear. Enter submits from a text field. Search and paging controls are disabled while a request runs, but filters remain editable. The result status identifies the applied filter state rather than implying that unsent edits produced the current rows.

Use a multi-column table with:

- UTC time
- Event type
- Primary player/entity
- Related player/entity
- Map
- Summary

Selecting a row opens details containing the full summary, ISO UTC timestamp, log row ID, numeric type, semantic entities and IDs, map/coordinates, and original stored text. A Copy details button places a stable plain-text representation on the clipboard. Selected names, types, and maps expose quick actions that fill the relevant filter without immediately searching.

Previous and Next use server-issued page tokens. Loading, busy, validation error, database error, and empty states are shown inline rather than in chat.

The window closes through the existing window-button flow. Client state is discarded on close. Cross-session persistence, live tailing, automatic refresh, arbitrary sorting, saved filters, and bulk export are deferred.

## Errors and privacy

Malformed packets are ignored or answered with a safe validation error according to whether a valid viewer/request can be identified. Invalid filters receive specific inline errors. Database exceptions are fully recorded through NLog while the client receives a generic search failure.

SQL is parameterized. Event type IDs are accepted only from the descriptor registry. Page tokens must belong to the current viewer search session; their shape alone grants nothing.

The single ViewLogs privilege exposes all stored log content, including tells and IP addresses, as explicitly chosen. Viewer searches are themselves audited.

## Testing

### Server

- Descriptor coverage for every active/retired type and generic unknown fallback.
- Correct `otherid` entity kind and semantic labels per type.
- Readable summaries for every known format and safe fallback for malformed text.
- Participant matching includes real counterparties and excludes equal-valued item, guild, NPC, and map IDs.
- Exact-name, duplicate-name, deleted-player-ID, map-ID, group/type, UTC range, literal text, and combined filters.
- 31-day and 7-day validation boundaries.
- Stable ordering when timestamps match; 50/51 behavior; token-based Next/Previous; no duplicates or omissions.
- Snapshot ceiling excludes rows inserted after page one, including after returning to page one.
- Page tokens reject mutation, expiration, cross-viewer use, and filter/session substitution.
- UTC-tick migration preserves exact instants and chronological ordering across historical provider formats.
- Index existence and query-plan checks for structured searches.
- Historical repair correctness and idempotence.
- UTC behavior under a non-UTC process timezone.
- `/logs` and LQS privilege enforcement, access revoked before completion, window replacement, close, disconnect, malformed packets, stale request IDs, rate limits, and global capacity.
- Database callback marshaling occurs on the game thread, late shutdown publication is dropped, and chunk delivery respects byte/buffer limits.
- Pending in-memory logs are excluded.
- One ViewLogs audit row per fresh Search and none for paging.

### Client

- LMT/LMM/LMD/LRB/LRD/LRF/LRX parsing and LQS formatting, including Base64 and malformed packets.
- Metadata completion and grouped event selection.
- UTC presets and custom validation.
- Chunk reassembly, bounded staging, and atomic commit.
- Stale window/request suppression.
- Current/next token history and paging controls.
- Table selection, details, quick-filter actions, and exact copied text.
- Loading, busy, empty, and error states.
- Frame-29 creation, close flow, resizing, UI scale, and layout metrics.

### Manual

Against a copied production database:

- open `/logs` with and without privilege;
- search broad structured ranges and seven-day text ranges;
- verify participant counterpart semantics;
- page while normal log flushing occurs;
- copy an event;
- close or disconnect during a query;
- verify current logs remain absent until persisted;
- inspect startup time and disk growth from index creation.

## Rollout and deferred work

Release the compatible Godot client before or alongside the server. Older clients continue playing but cannot render frame 29.

The logs table currently grows indefinitely. This design intentionally does not delete, archive, or compact logs. Retention and archival require a separate operational policy and design.
