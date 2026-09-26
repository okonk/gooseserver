# GM Log Viewer — Part 1 of 3: Server Data and Query Foundation Implementation Plan

**Status:** Approved design, revised implementation plan only

**Goal:** Build the server data/query foundation for persisted GM audit logs: canonical UTC-tick storage, an automatic atomic migration and historical repair, central event/entity semantics, readable structured projections, validated fresh-search filters, and stable SQLite keyset paging.

**Part dependency:** This is **Part 1 of 3**. It depends on `docs/plans/2026-09-26-gm-log-viewer-design.md` and the existing single-threaded `Database` service. Part 2 consumes these APIs for privilege enforcement, `/logs`, server window/session state, explicit Fresh/Page wire actions, server-bound random page tokens, query capacity, database-thread scheduling, game-thread completion, and Fresh-only rate limiting/auditing. Part 3 implements the Godot client.

**Scope boundary:** Do not add `/logs`, `AccessPrivilege.ViewLogs`, frame/window types, packet parsing, client-visible cursor serialization, random page-token/session storage, async delivery, game-thread completion queues, capacity coordination, rate limiting, search auditing, or client code here. Add `Log.Types.ViewLogs` and its descriptor now for registry completeness, but Part 1 does not write it.

**Architecture:** Keep the query core synchronous and connection-bound. Part 1 exposes fresh-filter validation and `LogQueryEngine.Execute(SQLiteConnection, LogSearchQuery)`. Part 2 must run fresh validation and the first query in one `Database.Enqueue` item, retain the resulting validated query (including resolved `ParticipantId`) in the viewer session, and use server-issued random tokens to map Page actions to internal `LogPageCursor` values. The engine reads persisted SQLite rows only; it never merges or flushes `LogHandler.Pending`, touches player/window state, uses `OFFSET`, or runs `COUNT(*)`.

**Tech stack:** C# / .NET 10, System.Data.SQLite.Core 1.0.119, NLog, xUnit.

**Repository rule:** Planned production and test changes add no comments or doc strings. Existing unrelated comments remain untouched.

---

## APIs verified against the current tree

| Fact/API | Verified location |
|---|---|
| Persisted event IDs are `Log.Types`; active values are 0–13, 15–24, and 10001–10012, with retired ID 14 deliberately left as a persisted gap | `Goose/Log.cs:9-51` |
| `Log` currently captures `DateTime.Now` | `Goose/Log.cs:62-72` |
| Log inserts currently bind `@logDate` as `DbType.DateTime2`, the unsafe provider-dependent behavior replaced by this plan | `Goose/Log.cs:74-101` |
| Logs remain in memory until `LogHandler.Save`; `Pending` exposes that read-only buffer | `Goose/LogHandler.cs:7-36` |
| Fresh database creation executes `Goose/sql/logs.sql` | `Goose/GameWorld.cs:137-150` |
| Startup migration runs for both fresh and existing databases on the database connection | `Goose/GameWorld.cs:169-179`, `Goose/GameWorld.cs:231-241` |
| Existing idempotent helpers are `ColumnExists`, `AddColumnIfMissing`, and `CreateTableIfMissing` | `Goose/GameWorld.cs:181-215` |
| `Database.Execute<T>` and `Database.Enqueue` are the dedicated-database-thread APIs | `Goose/Database.cs:196-224` |
| `logs` has no primary key, so SQLite `rowid` is available; `log_date` is currently declared `DATETIME2` and has no index | `Goose/sql/logs.sql:1-10` |
| `ClassChange` currently stores its target only in the first text token and writes `otherid = 0` | `Goose/Commands/ChangeClassCommand.cs:77-79` |
| `RespawnMap` currently shifts map ID/X/Y into `otherid`/`mapid`/`mapx` | `Goose/Commands/RespawnMapCommand.cs:20-22` |
| Existing command tests exercise both affected commands and can inspect `LogHandler.Pending` without a database | `Goose.Tests/Part3GmATests.cs:405-444`, `Goose.Tests/Part3GmAdminTests.cs:319-334`, `Goose/LogHandler.cs:9-12` |
| Player ID/name/access are persisted in `players`; `player_id` is the primary key | `Goose/sql/players.sql:1-8` |
| The in-memory player-data index excludes deleted rows and collapses names, so it cannot implement duplicate-name/deleted-player search semantics | `Goose/PlayerHandler.cs:175-186`, `Goose/PlayerHandler.cs:188-211` |
| Current maps persist `map_id` and `map_name` | `CsvToSql/CsvToSql.Core/MapsCsvToSql.cs:7-11` |
| Current NPC templates persist `npc_id` and `npc_name` | `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:7-13` |
| Current guilds persist `guild_id` and `guild_name` | `Goose/sql/guilds.sql:1-5` |
| Item instances are serialized inside inventory blobs, not normalized by item instance ID | `Goose/sql/players.sql:58-76` |
| Tests can access internal production helpers through existing `InternalsVisibleTo` declarations | `Goose/Goose.csproj:19-25` |
| Shipped SQL scripts copy to test output | `Goose/Goose.csproj:28-31` |
| Pickup/drop text formats are written as gold text or item ID/template/name/stack | `Goose/Events/PickupItemEvent.cs:85,119-121`, `Goose/Events/PlayerDropItemEvent.cs:65-67` |
| Vendor text contains item name/template/stack/cost/currency and `otherid` is an NPC template ID | `Goose/Events/VendorPurchaseInventoryEvent.cs:110-112`, `Goose/Events/VendorSellInventoryEvent.cs:90-92` |
| Guild/group counterparties and GM targets already use `otherid`, except ClassChange | `Goose/Commands/GuildAddCommand.cs:22`, `Goose/Commands/GuildRemoveCommand.cs:14,32`, `Goose/Commands/GroupAddCommand.cs:40`, `Goose/Commands/GroupRemoveCommand.cs:19,32`, `Goose/Commands/GiveExperienceCommand.cs:35-37`, `Goose/Commands/GiveGoldCommand.cs:30-32` |
| ResetItem, BuyGold, BuyExperience, and both GiveSpirit perspectives have explicit stored formats | `Goose/Data/Illutia/Scripts/Global/Dimensions/Commands.csx:135-139,175-177,232-234,287-294` |
| Rebirth has an explicit stored format | `Goose/Data/Illutia/Scripts/Global/Dimensions/Rebirth.csx:83-84` |
| Integration-test temporary-database lifecycle and WAL cleanup patterns already exist | `Goose.IntegrationTests/DatabaseTransactionTests.cs:5-17,67-75` |

---

## Fixed contracts and decisions

### Canonical timestamp storage

- Fresh `logs.log_date` is `INTEGER NOT NULL`.
- The integer is UTC `DateTime.Ticks`: signed 64-bit 100-nanosecond ticks since `0001-01-01T00:00:00`, in the valid `DateTime` range.
- `Log.Time` remains a UTC `DateTime`; `Log.SaveToDatabase` rejects a non-UTC value before enqueueing and binds `Time.Ticks` with `DbType.Int64`.
- Validated search boundaries retain the wire-facing Unix-millisecond input but convert once to UTC ticks. SQL compares integers: `@startTicks <= log_date AND log_date < @endTicks`.
- Query ordering and page boundaries use integer ticks directly. No provider `DateTime` read/write conversion remains in the query path.
- Every query explicitly includes `typeof(log_date) = 'integer'`; malformed historical text is preserved for operators but excluded from viewer results.

### Existing timestamp migration

Within the same transaction as ClassChange/RespawnMap repair and index creation:

1. Materialize every noncanonical timestamp candidate (`rowid`, SQLite storage type, raw value) before issuing updates.
2. Keep valid in-range SQLite integer ticks unchanged.
3. Parse text written by known System.Data.SQLite modes in this order: ISO-8601 (including compact provider forms); invariant-culture provider text; current-culture provider text; then decimal tick text. Parsing is exception-contained, and trying ISO first prevents compact `yyyyMMddHHmmss` text from being mistaken for ticks.
4. An explicit `Z`/offset is normalized to UTC. A zone-less historical value is treated as UTC without local-time conversion because the deployed server operated in UTC.
5. Rewrite each parseable value to an SQLite integer using an `Int64` parameter.
6. Preserve null/blob/real/unparseable/out-of-range values byte-for-byte, count them as malformed timestamps, and log that count only after commit.

### Raw persisted numeric fields

- `RawLogRow` and `LogQueryRow` store `rowid`, numeric type, `playerid`, `otherid`, `mapid`, `mapx`, and `mapy` as signed `Int64`, with per-field integer-storage validity for corrupt non-integer cells.
- Reader conversion never calls `GetInt32` for persisted log fields. SQLite integer values across the full signed-64-bit domain are retained exactly. Null/text/blob/real numeric cells produce value `0` plus `IsInteger == false`, so they cannot abort a broad search or masquerade as valid ID zero.
- A known descriptor is selected only when the raw type is in the positive/nonnegative `Int32` domain as appropriate and registered. Otherwise the generic unknown descriptor is used.
- Player/guild/NPC/map resolution and quick-filter eligibility occur only for positive IDs `<= Int32.MaxValue`. Out-of-domain values remain visible as stored numeric values but are never cast or looked up.
- Filter IDs and resolved `ParticipantId` remain validated `Int32` values.

### Descriptor semantics versus row projection

- `LogEventDescriptor.OtherIdKind` is only the meaning of the stored `otherid` column and drives participant-query semantics.
- `LogFormatter.Project` returns `LogFormattedEvent(Summary, Related)`; `Related` is an optional structured `LogRelatedEntity(Label, Kind, long? Id, Name, CanQuickFilter)`.
- A projected related entity may come from `otherid`, parsed stored text, or map columns. It is not copied mechanically from `OtherIdKind`.
- `LogOtherIdKind` and projected `LogEntityKind` are separate enums. The latter includes `StoredValue` for unknown raw `otherid` values so fallback rendering does not guess an entity type.

### Event IDs and broad searches

- Append `ViewLogs = 10013`; do not renumber persisted values.
- Register retired type 14 as `Received Credits (Retired)` without adding it back to `Log.Types`.
- Empty type selection means all rows, including unknown stored numeric types. A non-empty selection accepts only registry IDs, including 14 and 10013.
- Generic fallback renders unknown rows but does not make arbitrary unknown IDs valid filter input.

### Participant semantics

`OtherIdKind.Player` applies exactly to event IDs:

`5, 6, 8, 9, 13, 15, 23, 10002, 10003, 10004, 10007, 10010, 10011, 10012`.

Every row may match `playerid`. The `otherid` branch matches only that registry-derived set. Numeric collisions with item, guild, NPC, map, retired, malformed, or unknown rows never count as participants.

### Internal paging contract

- Part 1 does not encode, decode, sign, Base64-wrap, or accept a client cursor.
- `LogPageCursor` is immutable and internal. It contains `SnapshotCeiling` plus nullable `BeforeUtcTicks`/`BeforeRowId`; both boundary fields are absent for replaying the first page of an existing snapshot and both are present for a continuation.
- A fresh validated query has no cursor and captures `COALESCE(MAX(rowid), 0)`.
- A Page action in Part 2 retrieves a server-bound cursor and copies the stored base query with that internal cursor. It never re-resolves participant text or accepts replacement filters.
- Engine validation rejects negative snapshot ceilings, mismatched nullable boundary fields, out-of-range ticks, boundary ticks outside the query interval, non-positive boundary row IDs, and boundary row IDs above the snapshot before any SELECT.
- Every page applies `rowid <= snapshotCeiling`, orders by `log_date DESC, rowid DESC`, fetches 51, returns at most 50, and returns `LogSearchPage.NextCursor` as another internal object based on row 50.
- A continuation applies `log_date < @beforeTicks OR (log_date = @beforeTicks AND rowid < @beforeRowId)`.
- Part 2 derives/stores a first-page replay cursor from the captured snapshot and issues random tokens for Previous/Next. No client receives these internal values.

---

## Task 1: Canonicalize timestamps and atomically migrate/repair logs

**Files:**
- Modify: `Goose/Log.cs:9-101`
- Modify: `Goose/sql/logs.sql:1-10`
- Modify: `Goose/GameWorld.cs:169-179,231-241`
- Create: `Goose/Logs/LogSchemaMigrator.cs`
- Modify: `Goose/Commands/ChangeClassCommand.cs:77-79`
- Modify: `Goose/Commands/RespawnMapCommand.cs:20-22`
- Create: `Goose.Tests/LogUtcTests.cs`
- Create: `Goose.Tests/ProcessEnvironmentCollection.cs`
- Create: `Goose.Tests/LogSchemaMigrationTests.cs`
- Modify: `Goose.Tests/Part3GmATests.cs:405-444`
- Modify: `Goose.Tests/Part3GmAdminTests.cs:319-334`

### Step 1: Write failing UTC/storage tests

`LogUtcTests` runs in a non-parallel process-environment collection and restores `TZ`, current culture, and cached timezone data in `finally`:

1. `New_log_uses_utc_in_a_non_utc_process_timezone` creates a log under `Pacific/Honolulu` and asserts `Time.Kind == Utc` and a before/after UTC window.
2. `Saved_log_is_an_integer_with_the_exact_utc_ticks` assigns a known UTC value with seven fractional-second digits through the existing `Time` setter, starts a temporary `Database`, applies shipped logs DDL, saves the log, drains via a later `Execute`, and asserts `typeof(log_date) == "integer"` and `Convert.ToInt64(log_date) == log.Time.Ticks`.
3. `Save_rejects_non_utc_time_before_enqueue` assigns Local and Unspecified values and asserts no insert is queued.
4. `Saved_ticks_order_across_month_and_year_boundaries` writes exact UTC values around month/year rollovers and proves integer `ORDER BY log_date DESC` gives chronological order.

### Step 2: Write failing migration/repair tests

`LogSchemaMigrationTests` also runs in the non-parallel `ProcessEnvironment` collection, uses real in-memory SQLite, and uses an explicit old `DATETIME2` table when testing historical rows:

1. `Fresh_schema_declares_integer_log_date_and_all_five_indexes` checks `PRAGMA table_info(logs)` plus exact index names `logs_log_date_idx`, `logs_playerid_log_date_idx`, `logs_otherid_log_date_idx`, `logs_log_type_log_date_idx`, and `logs_mapid_log_date_idx`.
2. `Provider_datetime_text_under_non_utc_timezone_and_culture_becomes_utc_ticks` sets `TZ=Pacific/Honolulu` and `CurrentCulture=fr-FR`, inserts with the old `DbType.DateTime2` binding, captures the provider-produced text, migrates, and asserts exact UTC wall-clock ticks.
3. `Iso_text_variants_become_exact_utc_ticks` covers zone-less provider ISO, `Z`, and an explicit offset; offset input normalizes to the same UTC instant.
4. `Mixed_integer_and_text_timestamps_are_canonicalized_without_losing_ticks` combines valid integer ticks, provider text, ISO text, and exact seven-digit fractional seconds.
5. `Malformed_timestamp_storage_is_preserved_counted_and_not_integer` covers arbitrary text, null where an old malformed schema permits it, blob, real, numeric overflow text, and integer values outside valid `DateTime` ticks.
6. `Timestamp_migration_orders_across_month_and_year_boundaries` migrates mixed historical formats around both boundaries and checks integer ordering.
7. `Existing_logs_table_gains_all_indexes_and_second_run_is_idempotent` checks index creation on an old table.
8. `ClassChange_repair_moves_only_a_valid_positive_first_token` covers valid, already-fixed, empty, nonnumeric, zero, negative, and > `Int32.MaxValue` target tokens.
9. `RespawnMap_repair_unshifts_only_a_fully_valid_legacy_signature` covers the valid `otherid=7,mapid=12,mapx=34,mapy=0` case and independently invalidates every source field: wrong SQLite storage type, null, negative/out-of-range map ID/X/Y, and nonzero/out-of-range destination `mapy`.
10. `Migration_materializes_candidates_before_updates` seeds multiple adjacent timestamp/ClassChange/RespawnMap candidates and proves none are skipped by updating while reading.
11. `Second_run_reports_zero_repairs_but_recounts_preserved_malformed_rows` asserts repaired counts drop to zero while malformed timestamp/ClassChange/RespawnMap counts remain unchanged and are logged again.
12. `Timestamp_repairs_and_indexes_are_atomic` creates a conflicting schema object for a required index, expects migration failure, and proves timestamp rewrites and historical repairs rolled back.
13. `Counts_are_logged_only_after_commit` forces rollback and asserts no success/repair-count message was emitted.

Extend command tests:

- `ChangeClass_binds_double_modifier`: assign distinct GM/target player IDs and assert pending `ClassChange.OtherID` is the target while map/coordinates remain the GM location.
- `RespawnMap_revives_dead_npcs_and_broadcasts`: assert pending `RespawnMap.OtherID == 0` and exact GM map/X/Y.

### Step 3: Run red

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogUtcTests|FullyQualifiedName~LogSchemaMigrationTests|FullyQualifiedName~Part3GmATests.ChangeClass_binds_double_modifier|FullyQualifiedName~Part3GmAdminTests.RespawnMap_revives_dead_npcs_and_broadcasts"
```

Expected failures: local/provider DateTime storage, absent migration/indexes, partial historical semantics, and shifted future fields.

### Step 4: Implement writes and one atomic migration

`LogSchemaMigrator.Migrate(SQLiteConnection)` returns immutable `LogMigrationResult` with timestamp repaired/malformed counts and ClassChange/RespawnMap repaired/malformed counts.

Exact helper contracts:

- Start one `SQLiteTransaction`; assign it to every probe, select, update, and index command.
- Materialize timestamp, ClassChange, and RespawnMap candidate lists completely before the first update.
- Timestamp parsing follows “Existing timestamp migration” above. Updates bind `DbType.Int64` ticks by `rowid`.
- ClassChange candidates have integer `log_type = 10002` and zero/null `otherid`; parse only the first whitespace token with invariant `int.TryParse` and require `1..Int32.MaxValue`.
- RespawnMap candidates have integer `log_type = 10005` and nonzero `otherid`. Require every source cell to have SQLite integer storage, require `otherid` (map ID) in `1..Int32.MaxValue`, old `mapid`/`mapx` (X/Y) in `0..Int32.MaxValue`, and old `mapy == 0`. Only then write `otherid=0,mapid=old otherid,mapx=old mapid,mapy=old mapx`.
- Do not require a current map/player row; retired IDs remain meaningful.
- Create all five indexes only after timestamp normalization and repairs, then commit.
- Emit repaired/malformed NLog counts after `Commit()` returns. A rollback path logs the migration exception but no success counts. Preserved malformed candidates are intentionally counted on every startup.

Change fresh DDL to `log_date INTEGER NOT NULL` and add the five `CREATE INDEX IF NOT EXISTS` statements. Change new logs to `DateTime.UtcNow`; reject `Time.Kind != DateTimeKind.Utc` before enqueueing; bind `Time.Ticks` as `DbType.Int64`. Append `ViewLogs = 10013`.

Use named call arguments:

- ClassChange: `otherid: player.PlayerID`, followed by GM map ID/X/Y.
- RespawnMap: `otherid: 0`, followed by GM map ID/X/Y.

### Mutation impact

- **Pending state:** `Log.Time` changes from local to UTC. All current writers use the same constructor.
- **Persisted representation:** Every parseable historical date becomes an integer UTC tick value. Malformed values remain physically unchanged and are excluded later with `typeof(log_date)='integer'`.
- **Historical semantics:** Only fully validated ClassChange/RespawnMap candidates change. Every candidate list is materialized before mutation.
- **Atomicity:** Timestamp rewrites, event repairs, and index creation commit together or roll back together. Counts describe committed state only.
- **Schema/performance:** Five indexes are added to fresh/existing databases after normalization. The first production startup may lock SQLite and consume disk; require a backup and maintenance window.

### Step 5: Green and commit

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogUtcTests|FullyQualifiedName~LogSchemaMigrationTests|FullyQualifiedName~Part3GmATests.ChangeClass_binds_double_modifier|FullyQualifiedName~Part3GmAdminTests.RespawnMap_revives_dead_npcs_and_broadcasts"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/Log.cs Goose/sql/logs.sql Goose/GameWorld.cs Goose/Logs/LogSchemaMigrator.cs Goose/Commands/ChangeClassCommand.cs Goose/Commands/RespawnMapCommand.cs Goose.Tests/LogUtcTests.cs Goose.Tests/ProcessEnvironmentCollection.cs Goose.Tests/LogSchemaMigrationTests.cs Goose.Tests/Part3GmATests.cs Goose.Tests/Part3GmAdminTests.cs
git commit -m "feat: canonicalize and repair persisted logs"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| New timestamps persist as exact UTC `Int64` ticks and non-UTC values cannot enter the queue | UTC/storage/rejection tests |
| Integer ordering is chronological across month/year boundaries | saved/migrated ordering tests |
| Known provider/localized/ISO historical text migrates exactly | provider/ISO/mixed tests |
| Malformed dates remain unchanged and count on every run | malformed and second-run tests |
| Candidate updates cannot skip rows | materialization test |
| Respawn repair validates every shifted source cell | invalid-source theory |
| Rewrites, repairs, and indexes are atomic; logging is post-commit | rollback/log timing tests |
| Future ClassChange/RespawnMap rows use corrected fields | extended command tests |

---

## Task 2: Add descriptors and structured formatter/projection semantics

**Files:**
- Create: `Goose/Logs/LogEventDescriptor.cs`
- Create: `Goose/Logs/LogEventRegistry.cs`
- Create: `Goose/Logs/LogFormatter.cs`
- Create: `Goose.Tests/LogEventDescriptorTests.cs`
- Create: `Goose.Tests/LogFormatterTests.cs`

### Step 1: Write descriptor tests

Define `LogEventGroup`, `LogOtherIdKind`, `LogEntityKind`, immutable `LogEventDescriptor`, `LogRelatedEntity`, and `LogFormattedEvent`. Registry keys and lookup input are `Int64`; known descriptor IDs remain `Int32`.

Pin this `OtherIdKind` matrix:

| IDs/events | Group | Primary label | `OtherIdKind` |
|---|---|---|---|
| 0 Chat, 1 Shout, 2 Auction, 10 GroupChat | Communication | Speaker | Unused |
| 3 JoinGame, 4 LeaveGame | Sessions/Security | Player | Unused |
| 5 JoinGuild | Social | Member | Player |
| 6 LeaveGuild | Social | Member | Player |
| 7 GuildChat | Communication | Speaker | Guild |
| 8 JoinGroup | Social | Invited by | Player |
| 9 LeaveGroup | Social | Member | Player |
| 11 PickupItem, 12 PlayerDropItem | Items/Economy | Player | Unused |
| 13 Tell | Communication | Speaker | Player |
| 14 Received Credits (Retired) | Other/Retired | Player | Unused |
| 15 GaveCredits | Items/Economy | Giver | Player |
| 16 InvalidPassword | Sessions/Security | Account | Unused |
| 17 CreatedCustom | Items/Economy | Creator | Item |
| 18 BuyFromVendor, 19 SellToVendor | Items/Economy | Buyer/Seller | NpcTemplate |
| 20 Rebirth, 21 BuyGold, 22 BuyExperience | Items/Economy | Player/Buyer | Unused |
| 23 GiveSpirit | Items/Economy | Player | Player |
| 24 ResetItem | Items/Economy | Player | Item |
| 10001 GetItem | GM Actions | GM | Unused |
| 10002 ClassChange, 10003 GiveExperience, 10004 GiveGold | GM Actions | GM | Player |
| 10005 RespawnMap | GM Actions | GM | Unused |
| 10006 SpawnedNPC | GM Actions | GM | NpcTemplate |
| 10007 MacroCheck | GM Actions | GM | Player |
| 10008 MacroCheckConfirm, 10009 MacroCheckFailed | GM Actions | Player | Unused |
| 10010 Ban, 10011 Kick, 10012 SetPassword | GM Actions | GM | Player |
| 10013 ViewLogs | GM Actions | GM | Unused |

Assert every enum value plus retired 14 has exactly one descriptor, labels/IDs are unique, groups match the design, `PlayerValuedOtherTypeIds` equals the fixed set, and these raw type values all return generic Other/Retired descriptors without throwing: `-1`, `Int32.MaxValue + 1L`, and `Int64.MaxValue`.

### Step 2: Write formatter/projection tests

`LogFormatter.Project(LogFormatContext)` returns summary plus a structured related entity. Pin both summary and exact related projection for:

| Event | Structured `Related` source/contract |
|---|---|
| PickupItem / PlayerDropItem | Parse non-gold `itemId templateId multi word name stack`; label `Item`, kind Item, ID = parsed item instance ID, name = stored item name. Gold text has Item/Gold with no quick-filterable ID. |
| GetItem | Parse from the right as `multi word name itemId stack`; label Item, kind Item, parsed item ID/name even though descriptor `OtherIdKind` is Unused. |
| CreatedCustom | Label Item, kind Item, ID = `otherid`, name parsed from custom text; quick-filter only when ID is positive Int32. |
| ResetItem | Label Item, kind Item, ID = `otherid`, no invented name because its text has only template/dimension/cost/balance. |
| RespawnMap | Label Map, kind Map, ID/name sourced from `mapid` and map resolution, not `otherid`. |
| BuyFromVendor / SellToVendor | Label Merchant, kind NpcTemplate, ID = `otherid`, current resolved NPC-template name. |
| Unknown type with nonzero `otherid` | Label `Stored other ID`, kind StoredValue, raw signed-64-bit ID, no lookup and no quick filter. |

Also pin Tell/guild/group/credit/GM target projections from player-valued `otherid`, GuildChat from guild-valued `otherid`, SpawnedNPC from NPC-valued `otherid`, and null `Related` where no semantic relation exists.

Summary theories cover every known format: communication; sessions/IP; social membership; credits; both item layouts; custom/vendor; Rebirth/BuyGold/BuyExperience; both GiveSpirit perspectives without deduplication; ResetItem; ClassChange/GiveExperience/GiveGold; RespawnMap/SpawnedNPC/Macro/Ban/Kick/SetPassword; ViewLogs; retired and unknown fallback.

Malformed theories include missing tokens, nonnumeric/overflow IDs, unmatched delimiters, extra delimiters, empty text, and all raw numeric fields at `Int64.MinValue`/`Int64.MaxValue`. Fallback must retain raw signed-64-bit fields, map coordinates, and original text without casting, resolving, or throwing.

### Step 3: Run red

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogEventDescriptorTests|FullyQualifiedName~LogFormatterTests"
```

### Step 4: Implement registry and projector

Contracts:

- `Known` is immutable and ID ordered.
- `TryGetKnown(long, out descriptor)` first checks `Int32` range; `Get(long)` otherwise returns generic fallback.
- `PlayerValuedOtherTypeIds` derives from `OtherIdKind`, never a duplicated query constant.
- `LogFormatter.Project` owns format-specific related projection; descriptor `OtherIdKind` remains untouched.
- Parsing is invariant, anchored, overflow-safe, and supports names with spaces by reading fixed fields from the ends.
- Name lookup results are supplied through immutable context; no mutable handler access occurs.
- `CanQuickFilter` is true only for semantically filterable positive `Int32` IDs. A readable `Int64` ID may still be displayed when false.

### Step 5: Green and commit

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogEventDescriptorTests|FullyQualifiedName~LogFormatterTests"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/Logs/LogEventDescriptor.cs Goose/Logs/LogEventRegistry.cs Goose/Logs/LogFormatter.cs Goose.Tests/LogEventDescriptorTests.cs Goose.Tests/LogFormatterTests.cs
git commit -m "feat: project semantic entities from persisted logs"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Participant semantics come only from descriptor `OtherIdKind` | exact player-valued set |
| Related projection may come from other ID, text, or map columns | pinned projection table tests |
| Pickup/drop/GetItem/custom/reset/respawn/vendor project exact kinds and IDs | event-specific projection tests |
| Unknown/oversized types and IDs never cast or guess | signed-64-bit fallback tests |
| Every known valid format is readable; malformed rows are safe | valid/malformed theories |

---

## Task 3: Validate fresh-search filters and define internal page state

**Files:**
- Create: `Goose/Logs/LogSearchModels.cs`
- Create: `Goose/Logs/LogQueryValidator.cs`
- Create: `Goose.Tests/LogQueryValidationTests.cs`
- Create: `Goose.Tests/LogSearchModelTests.cs`

### Step 1: Pin the models

`LogFreshSearchInput` is the only untrusted model in Part 1:

- `long StartUtcMilliseconds`
- `long EndUtcMilliseconds`
- `string Participant`
- `int MapId` (`0` = All)
- `IReadOnlyCollection<int> EventTypeIds` (empty = all)
- `string Text`

It contains no cursor/token/action field.

`LogSearchQuery` is immutable and contains:

- `long StartUtcTicks`, `long EndUtcTicks`
- nullable positive `int ParticipantId`
- nullable positive `int MapId`
- immutable deduplicated/sorted known `Int32` event IDs
- literal text
- nullable internal `LogPageCursor`

`LogPageCursor` contains `long SnapshotCeiling`, `long? BeforeUtcTicks`, and `long? BeforeRowId`. A fresh validator result always has `Cursor == null`. `WithCursor` returns a copy, leaving the stored base query/filter/ParticipantId unchanged.

`LogValidationResult` is non-throwing and contains either a query or stable error code/message. Page-cursor structural errors are engine programmer/session-state errors, not fresh-filter validation errors.

### Step 2: Write fresh-validator tests

`LogQueryValidationTests` uses minimal real `players` data and covers:

- Unix millisecond bounds convert exactly to UTC ticks;
- `start >= end` and out-of-range Unix values fail;
- exactly 31 days is valid; 31 days + 1 ms fails;
- non-empty or whitespace-only text uses the 7-day limit; exactly 7 days is valid and +1 ms fails;
- embedded NUL in text fails with `InvalidText`; embedded NUL in participant fails with `InvalidParticipant` before database lookup;
- empty type list means all; duplicates sort/deduplicate; 14 and 10013 pass; unknown IDs fail;
- map 0 means all, any positive Int32 map is accepted for retired maps, and negatives fail;
- trimmed empty participant means all;
- `#42` resolves directly without requiring a current player row; malformed/zero/negative/overflow forms fail as ID errors;
- exact names use parameterized `COLLATE NOCASE`, include deleted rows, fail if missing, and fail as ambiguous at two case-insensitive matches;
- successful output stores only resolved `ParticipantId`, not the participant name;
- text containing `%`, `_`, backslash, quotes, or SQL fragments remains unchanged in the validated query for later literal escaping.

`LogSearchModelTests` asserts:

- no public/string/Base64 cursor API exists in Part 1 models;
- `WithCursor` preserves every validated filter and resolved `ParticipantId`;
- first-page cursor has both boundary fields null; continuation cursor has both populated;
- model collections cannot be mutated through the input list after validation.

### Step 3: Run red

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogQueryValidationTests|FullyQualifiedName~LogSearchModelTests"
```

### Step 4: Implement validation

`LogQueryValidator.ValidateFresh(SQLiteConnection, LogFreshSearchInput)` checks deterministically:

1. null/NUL string conditions;
2. Unix conversion and `start < end`;
3. 31-day range;
4. text-sensitive 7-day range;
5. map ID;
6. registry-backed event IDs;
7. participant.

Name resolution remains:

```sql
SELECT player_id
FROM players
WHERE player_name = @name COLLATE NOCASE
ORDER BY player_id
LIMIT 2;
```

Do not use `PlayerHandler.GetPlayerFromData`, which excludes deleted players and cannot report duplicates (`Goose/PlayerHandler.cs:175-211`). No “now” restriction is added. Convert accepted endpoints once to ticks and discard the source milliseconds from the query model.

### Step 5: Green and commit

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~LogQueryValidationTests|FullyQualifiedName~LogSearchModelTests"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/Logs/LogSearchModels.cs Goose/Logs/LogQueryValidator.cs Goose.Tests/LogQueryValidationTests.cs Goose.Tests/LogSearchModelTests.cs
git commit -m "feat: validate fresh log searches"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Fresh boundaries become exact UTC ticks with 31/7-day limits | boundary tests |
| NUL never reaches SQLite LIKE/name lookup | NUL tests |
| Deleted/duplicate/name/ID participant semantics are deterministic | participant tests |
| Paging cannot change participant resolution or filters | `WithCursor` preservation test |
| Part 1 exposes no client cursor codec | model API test |

---

## Task 4: Build robust SQLite filtering, participant union, and entity resolution

**Files:**
- Create: `Goose/Logs/LogQuerySqlBuilder.cs`
- Create: `Goose/Logs/LogQueryReader.cs`
- Create: `Goose/Logs/LogEntityResolver.cs`
- Create: `Goose.IntegrationTests/LogQueryFixture.cs`
- Create: `Goose.IntegrationTests/LogQueryFilteringTests.cs`
- Create: `Goose.IntegrationTests/LogQueryProjectionTests.cs`
- Create: `Goose.IntegrationTests/LogQueryPlanTests.cs`

### Step 1: Build the integration fixture

`LogQueryFixture` owns a temporary SQLite database, applies shipped `logs.sql`, and creates minimal real-column `players`, `guilds`, `maps`, and `npc_templates` tables. Seed helpers bind canonical dates as `DbType.Int64` ticks and numeric fields as `Int64`; a separate raw helper can insert malformed storage classes. Dispose removes DB/WAL/SHM files using the verified integration-test pattern.

### Step 2: Write filtering and malformed-row tests

`LogQueryFilteringTests` calls internal reader helpers with a fixed snapshot and proves:

1. every query contains `typeof(log_date) = 'integer'`;
2. exact start ticks are included and exact end ticks excluded;
3. malformed text/blob/real/null date rows are preserved in SQLite but excluded;
4. mixed canonical/malformed dates cannot disturb ordering;
5. one/multiple type, map, and group-expanded type filters work;
6. text binding is exactly `%` + escaped literal + `%`, escaping in order `\` → `\\`, `%` → `\%`, `_` → `\_`, with `LIKE @text ESCAPE '\' COLLATE NOCASE`;
7. `%`, `_`, and backslash match literally while ordinary text remains case-insensitive;
8. combined time/type/map/text filters intersect;
9. primary participant branch includes every `playerid` match;
10. secondary branch includes only registry `OtherIdKind.Player` types;
11. item/guild/NPC/map/retired/unknown numeric collisions do not match;
12. a both-role row appears once through `UNION`;
13. time/text/type/map/snapshot/boundary predicates appear in both branches;
14. empty type selection includes unknown and out-of-Int32 type values; explicit known selection excludes them;
15. signed-64-bit type/player/other/map/coordinate values do not throw or truncate;
16. non-integer malformed numeric cells are normalized by the reader as unusable signed-64-bit fields and never descriptor-resolved/quick-filtered;
17. SQL-injection strings remain literal parameters.

Every branch uses:

```sql
typeof(log_date) = 'integer'
AND log_date >= @startTicks
AND log_date < @endTicks
AND rowid <= @snapshotCeiling
```

Participant shape remains a deduplicating `UNION`; only parameter names and registry-derived known type placeholders are generated into SQL.

### Step 3: Write resolution and exact projection tests

`LogEntityResolver` bulk-loads IDs only when the raw value is positive Int32:

- players for primary and player-valued related IDs;
- guilds for guild-valued `otherid`;
- NPC templates for NPC-valued `otherid`;
- maps for positive Int32 `mapid` and future map-valued relations.

`LogQueryProjectionTests` pins:

- current primary/related names with stable IDs;
- renamed and deleted players;
- missing/out-of-domain IDs with numeric fallback and `CanQuickFilter == false`;
- GuildChat guild resolution;
- vendor/spawn NPC resolution;
- map resolution independent of `otherid`;
- exact structured projections for PickupItem, PlayerDropItem, GetItem, CreatedCustom, ResetItem, RespawnMap, both vendor events, and unknown rows as defined in Task 2;
- raw `Int64` type/player/other/map/X/Y survive into `LogQueryRow` unchanged for SQLite integer storage;
- malformed text uses fallback without losing the original text.

`LogQueryRow` contains row ID, UTC tick timestamp, raw signed-64-bit fields, event/group labels, structured primary/related entities and quick-filter flags, map reference, summary, and original text. Part 2 can convert valid ticks to Unix milliseconds for wire output without requerying.

### Step 4: Write resilient query-plan tests

Use deterministic skewed data (at least 10,000 canonical integer-date rows, selective participant/type/map values), run `ANALYZE`, and inspect `EXPLAIN QUERY PLAN` for production commands.

Do not assert one exact index when SQLite can validly choose among declared indexes. Assert instead:

- no relevant logs branch reports an unindexed full-table `SCAN logs`;
- each branch reports `SEARCH logs USING INDEX` or `SEARCH logs USING COVERING INDEX`;
- the chosen index belongs to the appropriate allowed set: time branch `{logs_log_date_idx}`, primary participant `{logs_playerid_log_date_idx, logs_log_date_idx}`, related participant `{logs_otherid_log_date_idx, logs_log_date_idx}`, type `{logs_log_type_log_date_idx, logs_log_date_idx}`, map `{logs_mapid_log_date_idx, logs_log_date_idx}`;
- compound-union temporary sorting is allowed and is not mistaken for a full logs scan.

No `INDEXED BY` is added to production SQL.

### Step 5: Run red

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogQueryFilteringTests|FullyQualifiedName~LogQueryProjectionTests|FullyQualifiedName~LogQueryPlanTests"
```

### Step 6: Implement query helpers

Contracts:

- `LogQuerySqlBuilder.Build(LogSearchQuery, long snapshotCeiling, int limit)` returns command text plus parameter binding; no user text is concatenated.
- Bind start/end/boundary ticks, snapshot, raw participant ID, and limit as `DbType.Int64`; known filter IDs remain bounded integers.
- Bind text as `%` + escaped literal + `%`; validator guarantees no NUL.
- `LogQueryReader` fully materializes before returning. For each numeric column it checks the provider value is an SQLite integer, stores the exact signed `Int64` and `IsInteger=true`, or stores `0` and `IsInteger=false`; it never parses corrupt text as an ID.
- Descriptor lookup requires usable type storage plus registered Int32 value.
- `LogEntityResolver` uses bounded parameterized `IN` lists from at most 51 candidates and returns immutable lookup maps.
- No reader or mutable world handler escapes/touches the database thread.

### Mutation impact

This task is read-only. It does not normalize malformed rows opportunistically, flush pending logs, mutate handlers, or cache names. Names may change between Page executions; stable IDs and the persisted result membership remain authoritative.

### Step 7: Green and commit

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogQueryFilteringTests|FullyQualifiedName~LogQueryProjectionTests|FullyQualifiedName~LogQueryPlanTests"
dotnet build Goose/Goose.csproj --no-restore
```

```bash
git add Goose/Logs/LogQuerySqlBuilder.cs Goose/Logs/LogQueryReader.cs Goose/Logs/LogEntityResolver.cs Goose.IntegrationTests/LogQueryFixture.cs Goose.IntegrationTests/LogQueryFilteringTests.cs Goose.IntegrationTests/LogQueryProjectionTests.cs Goose.IntegrationTests/LogQueryPlanTests.cs
git commit -m "feat: query and project canonical log rows"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Only canonical integer timestamps are searchable | storage-type/malformed-date tests |
| Literal text search includes surrounding wildcard and escapes metacharacters | binding and wildcard tests |
| Participant matching follows `OtherIdKind`, not numeric coincidence | union inclusion/exclusion theories |
| Full signed-64-bit persisted numerics cannot abort broad search | Int64/malformed-storage tests |
| Resolution and quick filters stay inside positive Int32 domain | projection/resolution tests |
| Related entities are formatter-projected, not mechanically copied | exact event projection tests |
| Structured branches avoid unindexed full logs scans | resilient query-plan tests |

---

## Task 5: Add stable 50/51 paging with internal cursors

**Files:**
- Create: `Goose/Logs/LogQueryEngine.cs`
- Create: `Goose.IntegrationTests/LogQueryPagingTests.cs`
- Modify: `Goose.IntegrationTests/LogQueryFixture.cs`

### Step 1: Write paging/cursor tests

`LogQueryPagingTests` covers:

1. zero rows returns empty rows, `HasMore == false`, and `NextCursor == null`;
2. exactly 50 returns 50/no next; 51 returns 50/next based on row 50, not lookahead row 51;
3. 101 rows traverse 50/50/1 without duplicate/omitted row IDs;
4. exact ticks differing by 1 order correctly;
5. identical ticks order by descending `rowid` across boundaries;
6. mixed dates order correctly across month and year boundaries;
7. continuation SQL uses exact tick/rowid keyset and no `OFFSET`;
8. rows inserted after page one are excluded whether their ticks are newer, between existing rows, or older;
9. a fresh query after insert captures a new snapshot and sees new rows;
10. derive a boundary-less first-page cursor from the original snapshot, insert rows, replay it, and assert the exact original first-page row IDs and next cursor;
11. a row updated after page one remains in the insertion snapshot, documenting approved rowid membership semantics;
12. participant-union paging preserves order/deduplication at 50/51;
13. renaming the participant or creating a duplicate name after Fresh does not change Page matching because the stored query uses resolved `ParticipantId`;
14. database has one matching row while `LogHandler.Pending` has another; only persisted SQLite data appears;
15. generated SQL contains `MAX(rowid)` only for cursorless Fresh, never `COUNT(*)` or `OFFSET`;
16. cursor validation rejects negative snapshot, only-one-null boundary, out-of-range/beyond-filter ticks, non-positive row ID, and row ID above snapshot before page SQL executes;
17. cursorless calls capture a fresh ceiling; boundary-less internal cursors reuse their stored ceiling;
18. `LogSearchPage.NextCursor` is an internal object with exact `Int64` snapshot/ticks/rowid and no encoded string representation.

### Step 2: Run red

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogQueryPagingTests"
```

### Step 3: Implement `LogQueryEngine`

Contract:

```csharp
internal LogSearchPage Execute(SQLiteConnection connection, LogSearchQuery query)
```

Execution sequence:

1. Structurally validate `query.Cursor` before SQL.
2. If null, run `SELECT COALESCE(MAX(rowid), 0) FROM logs`; otherwise reuse its snapshot.
3. A boundary-less cursor reads the first page under that snapshot. A continuation cursor adds the tick/rowid predicate.
4. Read at most 51 candidates; `HasMore` is true only for 51.
5. Remove only the lookahead row, resolve retained rows, and project immutable results.
6. If `HasMore`, create `NextCursor(snapshot, row50.LogDateTicks, row50.RowId)`; otherwise return null.
7. Return no total count and no serialized token.

`LogPageCursor.ForFirstPage(page.NextCursor.SnapshotCeiling)` creates the boundary-less replay object Part 2 stores behind a random page token whenever `HasMore` is true. If there is no next page, no first-page replay token is needed. Internal values never appear on the wire.

Part 2 execution contract:

- **Fresh action:** carries filters; validates/resolves once; applies Fresh rate limit; captures snapshot; audits one ViewLogs row; stores base query and random server-bound first/next page tokens.
- **Page action:** carries only viewer/request identity plus a random server-issued page token; retrieves base query + internal cursor; does not accept filters/participant text; does not rate-limit as Fresh and does not audit; still uses global capacity/access/window/stale checks.
- Previous uses a previously issued random token. Internal cursor values never cross the trust boundary.

### Mutation impact

Paging is read-only. Snapshot membership is insertion-based: later inserts are excluded, while updates to pre-snapshot rows can change presentation/content. Server-side Part 2 session state, not Part 1, binds filters and cursors to one viewer/search.

### Step 4: Green, full verification, and commit

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~LogQueryPagingTests"
dotnet test Goose.Tests/Goose.Tests.csproj
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj
dotnet build Goose.sln --no-restore
dotnet test Goose.sln --no-build --no-restore
```

```bash
git add Goose/Logs/LogQueryEngine.cs Goose.IntegrationTests/LogQueryPagingTests.cs Goose.IntegrationTests/LogQueryFixture.cs
git commit -m "feat: add internal snapshot paging for log searches"
```

### Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Page size is 50 with one-row lookahead | 0/50/51 tests |
| Exact-tick and equal-tick ordering has no gaps/duplicates | tick/rowid traversal tests |
| Month/year boundaries order chronologically as integer ticks | cross-boundary tests |
| Inserts cannot shift continuation or replayed first page | insert and first-page replay tests |
| Fresh gets a new snapshot | fresh-after-insert test |
| Participant identity remains stable after name changes | resolved-ID paging test |
| Malformed internal cursors fail before SQL | structural cursor theory |
| No client cursor encoding, count, or offset exists | API/SQL assertions |
| Pending memory rows are excluded | pending-vs-persisted test |

---

## Final design alignment and red-team review

### Design alignment checklist

- **Canonical UTC:** New writes bind exact UTC ticks as SQLite integers. Historical known text formats normalize atomically; malformed values remain preserved, counted, logged, and query-excluded.
- **Migration:** Timestamp conversion, ClassChange/RespawnMap repair, and all indexes commit together. Candidate readers close before updates; success counts log post-commit; malformed counts intentionally repeat.
- **Schema:** Fresh and existing databases receive indexes over integer-date rows; no provider DateTime binding remains.
- **Numeric robustness:** Raw persisted numeric columns are signed `Int64`; descriptor/resolution/quick-filter narrowing is explicit and domain-checked.
- **Canonical semantics:** Every active type, retired 14, ViewLogs, and unknown fallback has a descriptor. `OtherIdKind` alone drives participant SQL.
- **Structured projection:** Related label/kind/ID/name can originate from `otherid`, text, or map columns; pinned item/map/vendor/unknown cases prevent accidental coupling.
- **Fresh validation:** Exact name/ID participant resolution, type/map/range limits, and NUL rejection occur once. The validated query stores `ParticipantId`.
- **Text filtering:** Parameterized LIKE binds surrounding `%` plus escaped literal; `%`, `_`, and backslash have no wildcard meaning.
- **Query semantics:** Canonical integer dates only, half-open ticks, semantic participant union, current entity/map resolution, and no pending-memory merge.
- **Paging:** 51/50 keyset pages, exact ticks + rowid, `MAX(rowid)` snapshot, stable first-page replay, no `OFFSET`, and no `COUNT(*)`.
- **Trust boundary:** Part 1 has only internal cursors. Part 2 owns random viewer/search-bound page tokens and explicit Fresh/Page behavior.

### Red-team cases that must be green before Part 2 starts

| Attack/regression | Required defense |
|---|---|
| Provider/culture/timezone writes lexically misorder dates | one-time parse to UTC integer ticks; exact cross-month/year tests |
| Mixed integer/text historical dates | parseable text normalizes; malformed text remains and `typeof(log_date)='integer'` excludes it |
| Migration updates while reader is open | every candidate set materialized before mutation |
| Index creation/commit fails after repairs | one transaction rolls back timestamp and event changes; no success counts logged |
| Respawn source has one corrupt shifted field | validate all four source cells/storage classes; leave/count malformed |
| Raw type/entity/coordinate exceeds Int32 | retain/display Int64; no narrowing until checked |
| Unknown/invalid type accidentally maps to a known descriptor | known lookup requires usable registered Int32 value |
| Related entity is assumed to equal `otherid` | formatter emits explicit structured projection per event |
| Participant ID equals item/guild/NPC/map/unknown `otherid` | registry-derived player branch excludes it |
| Participant is both primary and player-valued related | `UNION` returns once |
| Participant is renamed after Fresh | Page uses stored resolved ID, not name re-resolution |
| `%`, `_`, backslash, quote, SQL text, or NUL in text | NUL validation; parameterization; surrounding escaped LIKE pattern |
| Malformed date/type/entity storage enters broad search | malformed dates excluded; numeric reader never throws; unusable fields never resolve |
| Many rows share one exact tick | rowid tie-break and exact internal cursor preserve traversal |
| New row is inserted before Previous returns to page one | boundary-less cursor replays original snapshot ceiling |
| Client invents/modifies cursor internals | client receives random Part 2 token only; internal cursor is server-bound and structurally checked |
| Planner selects a valid alternate index | tests allow appropriate declared indexes but forbid unindexed full logs scans |
| First startup on a large production DB | backup + maintenance window; one atomic idempotent migration |

### Explicitly deferred to Part 2

- `AccessPrivilege.ViewLogs`, `/logs`, window frame/type, replacement/close/disconnect state.
- Explicit Fresh/Page packet action and all framing/Base64/length/field validation.
- Per-viewer search session holding the immutable base query and random unguessable page-token → internal-cursor mappings.
- Fresh-only one-second rate limiting and exactly one `ViewLogs` audit row; Page actions do neither.
- Global capacity, one outstanding query, busy handling, database enqueueing, game-thread completion/revalidation, stale request/window suppression.
- Mapping `LogQueryRow` ticks to Unix milliseconds and internal page state to random wire tokens.

### Explicitly deferred to Part 3

All client metadata handling, filter controls, explicit Fresh/Page requests, random page-token history, response staging, table/details UI, quick filters, clipboard formatting, resizing, and frame-29 rendering.
