# Party Member Buffs Implementation Plan

**Goal:** Show each visible party member's non-item effects as compact timed icons below their HP/MP bars, synchronized through incremental server packets.

**Architecture:** The server emits separate clear, upsert, and remove packets keyed by player login ID and spell-effect ID. It publishes snapshots only at existing character-visibility boundaries and emits deltas from the canonical `Player.Buffs` mutation paths. The Godot client keeps ordered per-member effect state in `PartyWindow`, renders 16x16 informational icons, and clears state when character visibility ends.

**Tech Stack:** C#/.NET 10, xUnit, string packet protocol, Godot 4 C#, `.tscn` scenes, existing UI-scale and tooltip infrastructure

---

## Worktrees and design

- Server: `/home/agent/workspace/illutiagooseserver/.worktrees/party-member-buffs`
- Client: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs`
- Approved design: `docs/plans/2026-09-23-party-member-buffs-design.md`
- No persistence or schema changes are required.
- Both repositories' baseline test suites passed before planning. Existing analyzer warnings remain in both baselines.

## APIs verified

### Server

- Regular group roster publication is `Group.SendPartyWindow(Player, GameWorld)` and emits `GUD` slots without visibility filtering: `Goose/Group.cs:89-111`.
- Normal visibility uses strict same-map `< RANGE_X`/`< RANGE_Y` checks: `Goose/Map.cs:144-162`.
- Character range-entry and range-exit publication is bilateral in `Player.MoveTo`: `Goose/Player.cs:1267-1350`.
- Same-map warp erases old characters before publishing the new range: `Goose/Player.cs:1373-1431`.
- Map load publishes both character directions before setting `Player.Map`, adding the player to the map, and sending the party roster: `Goose/Events/DoneLoadingMapEvent.cs:54-109`.
- Canonical player effect mutations occur in `Player.AddBuff`, `RenewBuff`, and `RemoveBuff`: `Goose/Player.cs:2231-2420,2496-2566`.
- A stacking upgrade mutates the existing `Buff` object's `TimeCast`, `SpellEffect`, and `Caster`: `Goose/Player.cs:2387-2389`.
- Regular buff timing is currently calculated in `Player.SendBuffBar`: `Goose/Player.cs:2572-2594`.
- Expiration calls the same `Player.RemoveBuff` path: `Goose/Events/BuffExpireEvent.cs:16-20`.
- Name/title/surname changes erase and immediately republish a player character, so they also require a replacement snapshot: `Goose/Commands/ChangeNameCommand.cs:40-51`, `Goose/Commands/SetTitleCommand.cs:22-33`, `Goose/Commands/SetSurnameCommand.cs:22-33`.
- `TestWorldFixture.CommandPlayerOn` creates a ready capturing player but does not add it to `Map.Players` or assign a distinct login ID: `TestSupport/TestWorldFixture.cs:91-113`.

### Client

- `PacketManager.Listen<T>` constructs and registers a packet handler by prefix; no central registry edit is needed: `Scripts/Network/PacketManager.cs:12-21`.
- `PacketParser.GetRemaining()` returns the un-tokenized suffix needed for a trailing name containing commas: `Scripts/Network/PacketParser.cs:27-33`.
- `MapManager` subscribes to `MKC`/`ERC` before creating the persistent HUD, and mutates its character dictionary in those callbacks: `Scripts/MapManager.cs:94-120,170-182,230-238`.
- `PartyWindow` already subscribes to `GUD`, `MKC`, `ERC`, and vitals packets, but hard-codes eight rows while the server defaults to ten slots: `Scripts/UI/PartyWindow.cs:9-35`, `Goose/GooseSettings.json:154-158`.
- Existing party rows resolve already-visible characters through `CurrentMapManager.GetCharacter`: `Scripts/UI/PartyMember.cs:25-39`.
- `Icon.Apply` takes graphic file before graphic ID and applies nearest-neighbor filtering: `Scripts/UI/Icon.cs:13-25`.
- `BuffEffect` provides the established deadline, live tooltip, and sweep behavior: `Scripts/UI/BuffEffect.cs:47-68,86-148`.
- `BuffSweepBar.Update` uses `CooldownOverlay.ComputeProgress(..., growthMode: true)` and switches to red at the danger threshold: `Scripts/UI/BuffSweepBar.cs:16-35`.
- `TimeSpan.FormatDuration()` produces the existing human-readable duration format: `Scripts/Helpers.cs:7-20`.
- `UiScaleLayout.Snapshot` captures only nodes present at the end of `_Ready`; later dynamic effect nodes require explicit relayout: `Scripts/UiScaleLayout.cs:28-38`.
- Client xUnit tests compile `Scripts/**/*.cs` directly: `tests/Goose2Client.Tests/Goose2Client.Tests.csproj:13-15`.

## Task 1: Add server protocol builders and shared duration calculation

**Files:**
- Modify: `Goose/Buff.cs:9-16`
- Modify: `Goose/Packets.cs:665-683`
- Modify: `Goose/Player.cs:2572-2594`
- Create: `Goose.Tests/PartyMemberBuffPacketTests.cs`

**Mutation impact:**
- Source of truth changed: no gameplay state changes; `Buff.TimeCast` and `SpellEffect.Duration` remain canonical in `Goose/Buff.cs:11-15` and `Goose/SpellEffect.cs`.
- Important readers: regular `BUF` serialization in `Goose/Player.cs:2572-2594`; new party packet serialization in `Goose/Packets.cs`.
- Derived/cached state affected: remaining and total milliseconds are derived at send time; no cache is added.
- Required propagation sequence:
  1. Read `TimeCast`, duration, `world.TimeNow`, and `world.TimerFrequency`.
  2. Clamp remaining milliseconds to `[0, totalMs]`.
  3. Reuse the result in both regular `BUF` and party `PBA` builders.
- Invariants to preserve:
  - Existing `BUF` payloads remain byte-for-byte equivalent.
  - Permanent effects produce `(0, 0)`.
  - A partially elapsed or overdue effect cannot report more than total or less than zero.
- Observable proof required: packet tests assert exact payloads and the existing `BuffBarPacketTests` remain green.

**Step 1: Write failing packet and duration tests**

Add tests for:

- `PBA42,7,810020,20107,59000,120000,Strength, Greater`
- `PBR42,7`
- `PBC42`
- full, partially elapsed, overdue, and permanent duration pairs;
- unchanged regular `BUF` output after extracting the timing calculation.

The effect name must be the final field. Use a comma-containing name adversarially so a future client parser can prove it consumes the full suffix.

**Step 2: Run the focused tests and verify red**

Run:

```bash
cd /home/agent/workspace/illutiagooseserver/.worktrees/party-member-buffs
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~PartyMemberBuffPacketTests
```

Expected: compile/test failure because the party packet builders and shared duration API do not exist.

**Step 3: Implement the shared timing API and builders**

Add a small `Buff` method returning `(long RemainingMs, long TotalMs)` from a `GameWorld`. Its contract:

- It reads only the buff and world clock.
- It does not mutate the buff, event, player, or world.
- Duration zero returns `(0, 0)`.
- Timed values use the exact arithmetic and clamping currently in `Player.SendBuffBar`.

Add packet builders with these exact shapes:

```text
PBA<loginId>,<effectId>,<graphicId>,<graphicFile>,<remainingMs>,<totalMs>,<name>
PBR<loginId>,<effectId>
PBC<loginId>
```

Refactor `SendBuffBar` to call the shared method without changing slot filtering or empty-slot behavior.

**Step 4: Run focused and regression tests**

Run:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~PartyMemberBuffPacketTests|FullyQualifiedName~BuffBarPacketTests"
```

Expected: all selected tests pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Exact packet field order, with name last | comma-containing exact `PBA` assertion |
| Permanent effects report zero durations | permanent duration test |
| Remaining duration stays clamped | full/partial/overdue tests |
| Existing regular buff protocol is unchanged | existing `BuffBarPacketTests` plus exact regression assertion |

**Step 5: Commit**

```bash
git add Goose/Buff.cs Goose/Packets.cs Goose/Player.cs Goose.Tests/PartyMemberBuffPacketTests.cs
git commit -m "feat: add party buff protocol packets"
```

## Task 2: Route snapshots and mutation deltas through groups

**Files:**
- Modify: `Goose/Group.cs:9-112`
- Modify: `Goose/Player.cs:2231-2420,2496-2566`
- Create: `Goose.Tests/PartyMemberBuffSyncTests.cs`

**Mutation impact:**
- Source of truth changed: `Player.Buffs` remains canonical at `Goose/Player.cs:2238,2302,2505`; this task adds observations after successful mutations.
- Important readers: stat calculation, scripts, invisibility counters, regular `BUF`, and the new party clients all read the same `Buff` objects through `Player.AddBuff`/`RemoveBuff`.
- Derived/cached state affected: client party-effect state becomes a derived view; the server stores no recipient cache.
- Required propagation sequence:
  1. Complete or identify the canonical list mutation.
  2. For a non-item effect and ready visible group recipients, serialize timing from the resulting buff.
  3. Send `PBA`, `PBR`, or replacement `PBR` then `PBA`.
  4. Continue existing scripts, stats, status, character, and regular-bar publication in their current order unless tests require the party send later.
- Invariants to preserve:
  - Rejected adds publish no delta.
  - Loading-game additions publish no delta and are recovered by a later snapshot.
  - `refreshbar: false` suppresses regular `BUF` for both new adds and renewals, but not party deltas.
  - Duplicate removal publishes no second `PBR`.
  - Item effects never enter party snapshots or deltas.
  - Same-ID renewal preserves one client identity; different-ID replacement removes the old identity before adding the new one.
- Observable proof required: capturing players assert the final ordered packet stream, not a helper invocation.

**Step 1: Write failing group snapshot and delta tests**

Use real `Player`, `Group`, and `Buff` objects. Test:

- Snapshot emits `PBC` before ordered `PBA` packets.
- Empty snapshot still emits `PBC`.
- Item effects are excluded regardless of `ShowItemBuffs`.
- Add publishes one upsert.
- Add with `refreshbar: false` still publishes `PBA` but no new `BUF`.
- Same-ID renewal publishes one `PBA` and resets remaining time, including an adversarial `refreshbar: false` case that must not emit `BUF`.
- Stacking upgrade publishes ordered `PBR(old)` then `PBA(new)`.
- Rejected non-stacking add publishes nothing.
- Actual removal publishes one `PBR`; duplicate removal does not publish another.
- Expiration reaches the same removal delta through `BuffExpireEvent`.
- `Group.RemovePlayer` clears roster membership and the removed/disbanded players receive no later deltas.

Fixture requirements:

- Configure `PartyWindowMax` and `BuffBarVisibleSize` where those packets are asserted.
- Assign distinct nonzero `LoginID` values.
- Add capturing players to the map with `map.AddPlayer` before testing range-based delivery.
- Clear `Sent` after setup because grouping and buff mutation emit unrelated packets.
- Set `TimeCast` on manually constructed timed buffs.

**Step 2: Run and verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~PartyMemberBuffSyncTests
```

Expected: failures because group snapshot/delta helpers do not exist and mutations emit no party packets.

**Step 3: Implement explicit group helper contracts**

Add helpers with unambiguous viewer/target roles:

- `SendBuffSnapshot(viewer, target, world)` sends exactly one `PBC`, then one `PBA` per non-item `target.Buffs` entry in list order.
- A delta recipient enumerator returns ready group members other than the target that are on the same map, inside normal strict range, and allowed to see the target under GM-invisibility rules.
- Add/upsert publication sends one `PBA` to those recipients.
- Remove publication sends one `PBR`.
- Replacement publication sends `PBR(oldEffectId)` immediately followed by `PBA(newBuff)` to each recipient.

Preconditions and postconditions:

- Snapshot callers have already decided that the character is being published to the viewer.
- Helpers send packets only; they never mutate `Group.Players`, `Player.Buffs`, map membership, timers, or stats.
- A snapshot is safe for an empty effect list and always actively clears stale state.

Do not make `SendPartyWindow` automatically snapshot every listed member: `GUD` membership is global while effect visibility is range-limited.

**Step 4: Attach deltas to canonical mutations**

- New add: publish after `this.Buffs.Add(buff)` and invisibility bookkeeping succeeds.
- Renewal: pass `refreshbar` into `RenewBuff`, capture the old effect ID before mutating `existingBuff.SpellEffect`, publish after `TimeCast`, `SpellEffect`, and `Caster` hold the replacement values, and call `SendBuffBar` only when `refreshbar` is true.
- Removal: retain the boolean result of `this.Buffs.Remove(buff)` and publish only when it is true.
- Skip item buffs and the early loading-game branch.
- Keep party publication independent of `refreshbar`.

Do not add separate expiration logic; `BuffExpireEvent` already enters `Player.RemoveBuff` at `Goose/Events/BuffExpireEvent.cs:16-20`.

**Step 5: Run focused tests**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~PartyMemberBuffSyncTests|FullyQualifiedName~BuffBarPacketTests|FullyQualifiedName~BuffNullGuardTests"
```

Expected: all selected tests pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Snapshot clears before rebuilding ordered state | snapshot packet-order test |
| Item effects never leak | snapshot and delta item-filter tests |
| `refreshbar` suppresses only regular refresh | new-add and same-ID-renewal `refreshbar: false` tests |
| Upgrade cannot leave old effect ID cached | exact `PBR(old)` then `PBA(new)` test |
| Double removal cannot create duplicate remove | duplicate-removal test |
| Expiry uses authoritative remove path | event-handler expiration test |

**Step 6: Commit**

```bash
git add Goose/Group.cs Goose/Player.cs Goose.Tests/PartyMemberBuffSyncTests.cs
git commit -m "feat: publish party buff deltas"
```

## Task 3: Publish snapshots at character-visibility boundaries

**Files:**
- Modify: `Goose/Group.cs:27-47`
- Modify: `Goose/Player.cs:1267-1350,1373-1431`
- Modify: `Goose/Events/DoneLoadingMapEvent.cs:54-109`
- Modify: `Goose/Commands/ChangeNameCommand.cs:40-51`
- Modify: `Goose/Commands/SetTitleCommand.cs:22-33`
- Modify: `Goose/Commands/SetSurnameCommand.cs:22-33`
- Create: `Goose.Tests/PartyMemberBuffVisibilityTests.cs`

**Mutation impact:**
- Source of truth changed: no map or group state ownership changes; `Map.Players`, `Group.Players`, and `Player.Buffs` remain canonical.
- Important readers: map character publication (`MKC`/`ERC`), party roster publication (`GUD`), and new party snapshot handlers.
- Derived/cached state affected: client visibility and party-effect caches must be published in a safe order.
- Required propagation sequence:
  1. Publish/confirm the viewer's current `GUD` membership.
  2. Publish the target character with `MKC` under existing GM rules.
  3. Send `PBC` then current `PBA` entries.
  4. On range exit, existing `ERC` causes immediate client clear; no server cache teardown is needed.
- Publication boundary: a target becomes eligible for party-effect packets only after the viewer can observe both the party slot and the map character. Readers must never observe party effects for a target hidden by GM invisibility.
- Invariants to preserve:
  - `MKC` precedes the first snapshot packet.
  - Group membership alone does not reveal out-of-range effects.
  - Range exit publishes `ERC`; re-entry publishes a fresh clear-plus-state snapshot.
  - Cross-map warp waits for destination map load instead of publishing from a null/intermediate map.
- Observable proof required: packet ordering and absence assertions on real capturing players.

**Step 1: Write failing lifecycle tests**

Cover:

- Group creation/addition while members are already in range: affected `GUD` packets precede bilateral snapshots.
- Group addition out of range: no snapshot.
- Movement into range: each visible direction gets `MKC` before `PBC`/`PBA`.
- Movement out of range: each direction gets `ERC` and no extra party packet.
- Re-entry: fresh `PBC` repairs stale state before current upserts.
- Same-map warp: erase first, then bilateral `MKC`, then snapshots.
- Destination map load: party roster precedes snapshots delivered to the loading player; existing peers receive target snapshot only when the target is publishable.
- Name, title, and surname refreshes: each `ERC`/`MKC` pair is followed by the target's current snapshot, so informational character republication cannot permanently clear icons.
- GM-invisible target: no character and no party-effect snapshot.
- Removed, loading, unrelated, different-map, and exact-range-boundary recipients get no deltas/snapshots.

Use maps at least 60x40 because the fixture default 10x10 map is smaller than visibility range. Configure `PartyWindowMax = 10`, assign distinct nonzero login IDs, and add ready players to `Map.Players`; otherwise the fixture emits no `GUD` rows and range enumeration cannot see the players. Follow the event-driving pattern in `Goose.Tests/QuestIconVisibilityTests.cs:45-58` for `DoneLoadingMapEvent`.

**Step 2: Run and verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~PartyMemberBuffVisibilityTests
```

Expected: no snapshot packets at lifecycle boundaries.

**Step 3: Add snapshot publication without changing registry ownership**

- `Group.AddPlayer`: after every affected viewer has received its refreshed party window, send snapshots only for newly established visible relationships, in both directions. Do not re-snapshot unrelated existing pairs.
- `Player.MoveTo`: in `afterRange.Except(beforeRange)`, place each directional snapshot immediately after the corresponding directional `MKC`/admin publication branch.
- Same-map `Player.WarpTo`: keep the initial bilateral `ERC`; place each directional snapshot immediately after its corresponding new `MKC` branch.
- `DoneLoadingMapEvent`: preserve character publication and map registration. After `SendPartyWindow` establishes the loading player's rows, send snapshots for visible party peers to that player. Also send the loading target's snapshot to existing visible party peers after their target `MKC`; ensure the helper's GM and membership checks prevent leakage.
- `ChangeNameCommand`, `SetTitleCommand`, and `SetSurnameCommand`: after each visible viewer receives the replacement `MKC`, send that target's snapshot to the viewer. Keep the command's existing status and persistence behavior unchanged.
- Cross-map `WarpTo` adds no snapshot because it changes state to `LoadingMap`, removes map membership, and nulls `Map` at `Goose/Player.cs:1441-1457`.

All of these execute on the existing game/event thread; no scheduling or synchronization layer is introduced.

**Step 4: Run server tests**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --filter "FullyQualifiedName~PartyMemberBuff|FullyQualifiedName~QuestIconVisibilityTests|FullyQualifiedName~InvisibilityTransitionTests"
```

Expected: all selected tests pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Roster and character exist before effects | group-add/map-load packet-order tests |
| Out-of-range membership reveals nothing | out-of-range and boundary tests |
| Visibility loss clears via existing protocol | bilateral `ERC` assertion |
| Re-entry repairs stale state | `ERC`, then later `PBC` before `PBA` assertion |
| Character refresh cannot leave icons cleared | name/title/surname `ERC`, `MKC`, `PBC`, `PBA` order tests |
| GM invisibility is not bypassed | absent `MKC` and absent `PB*` assertion |

**Step 5: Commit**

```bash
git add Goose/Group.cs Goose/Player.cs Goose/Events/DoneLoadingMapEvent.cs Goose/Commands/ChangeNameCommand.cs Goose/Commands/SetTitleCommand.cs Goose/Commands/SetSurnameCommand.cs Goose.Tests/PartyMemberBuffVisibilityTests.cs
git commit -m "feat: snapshot buffs for visible party members"
```

## Task 4: Parse party packets and model ordered client state

**Files:**
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/Network/Packets/PartyBuffAddPacket.cs`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/Network/Packets/PartyBuffRemovePacket.cs`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/Network/Packets/PartyBuffClearPacket.cs`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/PartyMemberEffectState.cs`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/tests/Goose2Client.Tests/PartyBuffPacketTests.cs`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/tests/Goose2Client.Tests/PartyMemberEffectStateTests.cs`

**Mutation impact:**
- Source of truth changed: server packets remain authoritative; the new client state is a disposable derived cache keyed by current party slot login IDs and effect IDs.
- Important readers: `PartyWindow` will read ordered effects to reconcile `PartyMember` nodes.
- Derived/cached state affected: slot membership, character visibility, ordered effect data, and local expiry deadlines.
- Required propagation sequence:
  1. `GUD` assigns a login ID to a slot and clears the prior ID if changed.
  2. `MKC` marks a current member visible, or `GUD` seeds visibility from `MapManager.GetCharacter` when the character predates group membership.
  3. `PBC` clears stale effects.
  4. `PBA` inserts or updates only a current visible member.
  5. `PBR` removes one identity.
  6. `ERC` marks the member non-visible and clears all effects before any late delta can be accepted.
- Invariants to preserve:
  - Same-ID upsert resets timing without changing insertion order.
  - Remove closes the ordered gap.
  - Local time reaching zero never mutates the cache.
  - Unknown/non-visible IDs cannot recreate stale icons.
- Observable proof required: pure state tests assert complete ordered contents and eligibility after every operation.

**Step 1: Write failing parser tests**

Test all three packet classes, including:

- 64-bit durations;
- zero-duration permanent effect;
- `PBA` name containing commas, parsed with `PacketParser.GetRemaining()`;
- exact login/effect IDs.

**Step 2: Write failing pure-state tests**

Pin:

- slot assignment and replacement;
- visibility enter and erasure;
- rejection of unknown and current-but-non-visible members;
- ordered insertion;
- same-ID renewal preserving position and replacing the expiry deadline;
- single removal and full clear;
- `PBR(old)` followed by `PBA(new)` replacement order;
- no local-expiry removal.

Use an injected/current timestamp or store an explicit deadline supplied by the operation so tests do not sleep or depend on wall-clock timing.

**Step 3: Run and verify red**

```bash
cd /home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter "FullyQualifiedName~PartyBuffPacketTests|FullyQualifiedName~PartyMemberEffectStateTests"
```

Expected: compile failure because packet and state types do not exist.

**Step 4: Implement minimal packet and state types**

Packet prefixes are exactly `PBA`, `PBR`, and `PBC`. Parse all fixed `PBA` fields with typed token methods, then parse `Name` with `GetRemaining()`.

The pure state type owns no Godot nodes and performs no packet registration. Each accepted `PBA` stores one absolute `ExpiresAt` computed from an injected/current timestamp plus `RemainingMs`; permanent effects use no deadline. Its operations return whether state changed and expose the ordered effects needed to reconcile one member. It may use an ordered list plus dictionary, but must make update-in-place versus append behavior explicit and deterministic. Unrelated later reconciliation must reuse the stored deadline rather than recomputing `now + RemainingMs`, which would incorrectly extend effects.

Publication/teardown order inside the state:

- Slot replacement removes old member state before publishing the new slot ID.
- Erasure marks invisible before clearing effects, preventing a concurrent/later callback in the same packet stream from accepting a stale upsert.
- Failed eligibility leaves state unchanged.

**Step 5: Run focused tests**

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter "FullyQualifiedName~PartyBuffPacketTests|FullyQualifiedName~PartyMemberEffectStateTests"
```

Expected: all selected tests pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Trailing names can contain commas | adversarial parser test |
| Renewal cannot reorder an icon | same-ID upsert order test |
| Late delta cannot restore erased state | `Erase` then `Upsert` rejection test |
| Replacement cannot retain old ID | `PBR(old)` then `PBA(new)` state test |
| Deadline zero is not authoritative removal | explicit expired-entry retention test |

**Step 6: Commit in the client worktree**

```bash
git add Scripts/Network/Packets/PartyBuff* Scripts/PartyMemberEffectState.cs tests/Goose2Client.Tests/PartyBuffPacketTests.cs tests/Goose2Client.Tests/PartyMemberEffectStateTests.cs
git commit -m "feat: model party member buff state"
```

## Task 5: Build the compact party-effect presentation

**Files:**
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/UI/PartyEffect.cs`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scenes/UI/PartyEffect.tscn`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/UI/BuffSweepBar.cs:5-35`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/tests/Goose2Client.Tests/PartyEffectPresentationTests.cs`

**Mutation impact:**
- Source of truth changed: no authoritative state changes; a `PartyEffect` displays one cached effect entry.
- Important readers: Godot frame processing, `TooltipManager`, `Icon.Apply`, and `BuffSweepBar`.
- Derived/cached state affected: local deadline, current tooltip text, sweep progress/color, and loaded icon texture.
- Required propagation sequence:
  1. Apply the state entry, including its fixed absolute deadline and total duration; do not derive a new deadline during reconciliation.
  2. Resolve the icon through `Icon.Apply(file, id, ...)`.
  3. Each frame calculate clamped display remaining without deleting the node.
  4. Update dark sweep; use red at `<= 10s`.
  5. While hovered, update `Name (<duration> remaining)`; permanent effects use name only.
- Invariants to preserve:
  - Base icon size is 16x16.
  - No countdown label, blink, or `_GuiInput` removal path exists.
  - Permanent effects have no sweep.
  - Expired timed effects stay fully swept/red until removed by state reconciliation.
- Observable proof required: pure presentation helpers and runtime scene assertions; no test should require sleeping.

**Step 1: Write failing presentation tests**

Extract/test only small deterministic helpers:

- tooltip text for 83 seconds is `Name (1m 23s remaining)`;
- zero-duration tooltip is `Name`;
- normal sweep color before the final ten seconds;
- red sweep color at ten seconds and below;
- progress is full at/past zero via existing `CooldownOverlay.ComputeProgress` tests.

**Step 2: Run and verify red**

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter "FullyQualifiedName~PartyEffectPresentationTests|FullyQualifiedName~CooldownOverlayGrowthTests"
```

Expected: failures because the compact presentation and testable danger-color helper do not exist.

**Step 3: Implement `PartyEffect` and scene**

The scene contains only:

- a 16x16 root `Control` or `Panel` with nonzero minimum size;
- `Icon` `TextureRect`, nearest-neighbor through `Icon.Apply`;
- `Sweep` `BuffSweepBar` covering the same 16x16 bounds.

`PartyEffect` follows the deadline and live-hover behavior of `BuffEffect`, but accepts the absolute deadline already stored in the state entry. It deliberately omits countdown, blink, slot number, double-click callback, and `_GuiInput`. Its root receives hover input while icon/sweep children ignore it. Timed effects continue processing when remaining time reaches zero so the full red sweep persists; only permanent effects skip timer processing. On removal/clear, hide any tooltip owned by the node before freeing it.

Expose a small internal/static color selector from `BuffSweepBar`; `_Draw` must call that selector so the unit test proves the rendered branch rather than a duplicate calculation.

**Step 4: Run focused tests**

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter "FullyQualifiedName~PartyEffectPresentationTests|FullyQualifiedName~CooldownOverlayGrowthTests"
```

Expected: all selected tests pass.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| Time appears only in tooltip | scene has no countdown node; tooltip tests |
| Permanent effect has no timer text/sweep | permanent helper test plus runtime scene assertion |
| Danger state begins at ten seconds | threshold boundary tests |
| Party icon cannot remove a buff | compile/scene structure: no network callback or input handler; runtime manual check deferred to Task 7 |

**Step 5: Commit**

```bash
git add Scripts/UI/PartyEffect.cs Scripts/UI/BuffSweepBar.cs Scenes/UI/PartyEffect.tscn tests/Goose2Client.Tests/PartyEffectPresentationTests.cs
git commit -m "feat: add compact party buff icons"
```

## Task 6: Wire party state to rows and update scalable layout

**Files:**
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/UI/PartyWindow.cs:9-92`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/UI/PartyMember.cs:7-46`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scenes/UI/PartyMember.tscn:9-71`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scenes/UI/PartyWindow.tscn:6-21`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/PartyMemberMetrics.cs:5-8`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/tests/Goose2Client.Tests/PartyMemberMetricsTests.cs`
- Create: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/tests/Goose2Client.Tests/PartyWindowCapacityTests.cs`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/Scripts/UiScaleSelfTest.cs:176-183,218-223,477-495`
- Modify: `/home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs/tools/tests/run_ui_scale.sh:1-7`

**Mutation impact:**
- Source of truth changed: `PartyWindow` owns the derived state from Task 4; each `PartyMember` owns only rendered node instances for its current state.
- Important readers: packet callbacks, party roster/vitals rendering, UI scaling, tooltip lifecycle, and the runtime self-test.
- Derived/cached state affected: per-row effect node dictionary, HBox child order, party-row geometry, and dynamically spawned node scale.
- Required propagation sequence:
  1. Register packet listeners after rows exist.
  2. On `GUD`, clear old slot state, assign the new ID, seed visibility from `CurrentMapManager.GetCharacter`, and reconcile an empty/current row.
  3. On `MKC`, mark current member visible and retain no stale effects until snapshot packets arrive.
  4. On accepted `PBC`/`PBA`/`PBR`, reconcile only the affected row in state order.
  5. On `ERC`, mark invisible, clear state and nodes immediately, then keep the roster row/vitals behavior.
  6. On `_ExitTree`, unregister every listener before nodes are destroyed.
  7. On UI-scale changes, apply static geometry and explicitly relayout all dynamic effect nodes.
- Publication boundary: construct row/state first, register listeners, then expose packet callbacks; teardown removes listeners before discarding UI state.
- Invariants to preserve:
  - Existing HP/MP updates remain unchanged.
  - The client constructs ten rows, matching the server's default `PartyWindowMax`.
  - Party frame width stays 87 px.
  - Rows grow to 50 px and never overlap vertically.
  - Effect icons are 16 px with 1 px gaps and extend right without wrapping/clipping.
  - Six icons occupy 101 px and therefore prove intentional overflow.
  - Reconciliation updates an existing same-ID node instead of replacing/reordering it.
- Observable proof required: xUnit metric/state assertions plus a real Godot scene/self-test.

**Step 1: Update failing metric tests**

Change expected member sizes to:

- 1x: `87x50`
- 1.5x: `131x75`
- 2x: `174x100`

Add deterministic helpers/tests for icon size, gap, and row width. Assert six icons consume `6 * 16 + 5 * 1 = 101` px while the frame remains 87 px. Add a capacity test asserting `PartyWindow.MaxMembers == 10`; because `GameManager._partySlots` is allocated from that constant (`Scripts/GameManager.cs:55-56`), no separate slot-array edit is needed.

**Step 2: Run and verify red**

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter FullyQualifiedName~PartyMemberMetricsTests
```

Expected: old 33 px row metrics fail.

**Step 3: Add the row geometry**

At 1x:

- Change party member and content height from 33 to 50.
- Keep existing name/frame/HP/MP geometry unchanged; MP ends at y=32.
- Add `Content/EffectRow` as an `HBoxContainer` at x=0, y=34, height 16, authored width 87, separation 1.
- Keep `ClipContents` false on the effect row and relevant ancestors.
- Change `PartyWindow.MaxMembers` from 8 to 10.
- Change `MemberList` and `PartyWindow` height from 271 to 509 (`10 * 50 + 9 * 1`); with the existing y=70 top offset, the authored bottom becomes 579.
- Keep all party widths at 87.

**Step 4: Implement row reconciliation and packet lifecycle**

`PartyMember` owns an effect-ID-to-`PartyEffect` node map and an ordered reconciliation method:

- Existing IDs receive updated effect/timing data and the state's fixed deadline without node replacement or deadline recomputation.
- New IDs instantiate the packed scene and are placed in state order.
- Missing IDs hide their tooltip, hide immediately, and `QueueFree`.
- Full clear empties the row without hiding the member content itself.

`PartyWindow`:

- Owns the state instance and constructs ten member rows.
- Registers/removes `PBA`, `PBR`, and `PBC` listeners alongside existing listeners.
- Keeps packet observer callbacks as thin casts that delegate to internal packet-application methods. Those methods feed `GUD`, `MKC`, `ERC`, `PBC`, `PBA`, and `PBR` into state and reconcile the affected row, providing a real runtime self-test seam without exposing public gameplay APIs.
- On `GUD`, seed visibility with `CurrentMapManager?.GetCharacter(loginId) != null` because a player can join a group after their `MKC` was already handled.
- Ignore `PBA` that the state rejects for unknown/non-visible members.
- Continue existing vitals updates independently of effect state.

Because `MapManager` registers before HUD creation (`Scripts/MapManager.cs:94-120`), its character dictionary is updated before `PartyWindow` receives normal `MKC`/`ERC` callbacks. Do not depend only on callback order; retain the `GetCharacter` seed for late group membership.

**Step 5: Handle dynamic scaling explicitly**

`PartyWindow.Relayout` must:

- Apply the one-time static `_geom` snapshot.
- Set each member's scaled minimum size.
- Tell each member to relayout its effect row/nodes using `UiScaleApplier.Instance.Factor`.

New effects created after `_Ready` must initialize from the current factor immediately; they are absent from `UiScaleLayout.Snapshot` and cannot rely on it.

**Step 6: Extend and harden the runtime self-test**

Update `UiScaleSelfTest` to:

- assert ten party rows and the 509 px base member-list height;
- expect 174x100 at 2x and 87x50 after restoring 1x;
- verify `Content/EffectRow`, `PartyEffect/Icon`, and `PartyEffect/Sweep` paths;
- exercise `PartyWindow`'s internal packet-application methods with `GUD`, `MKC`, `PBC`, `PBA`, renewal, `PBR`, and `ERC` objects, asserting real row creation/update/removal and that renewal retains the same node;
- populate six synthetic effects through that same application path before capturing the 1x baseline;
- verify 16 px icons/1 px gaps at 1x and 32 px/2 px at 2x;
- verify the row begins below MP, the frame remains 87 px, the sixth child extends beyond x=87, and clipping is disabled;
- verify the 2x-to-1x round trip remains bit-identical.

Update `tools/tests/run_ui_scale.sh` to build the C# project before launching Godot and create minimal missing generated assets, following `tools/tests/run_character_icon.sh:2-20`. At minimum, provide a fallback empty `Assets/Sprites/manifest.json`; include the animation-heights fallback if the self-test reaches character construction.

**Step 7: Run focused and runtime tests**

```bash
dotnet test tests/Goose2Client.Tests/Goose2Client.Tests.csproj --filter "FullyQualifiedName~PartyMember|FullyQualifiedName~PartyBuff|FullyQualifiedName~PartyEffect"
tools/tests/run_ui_scale.sh
```

Expected: all xUnit tests pass and the headless runner prints its final UI-scale success marker with exit code 0. If no C#-capable Godot binary exists, report the runner as environment-blocked rather than treating it as passed.

**Invariant-to-test matrix:**

| Invariant | Proved by |
|-----------|-----------|
| All server-supported slots are representable | capacity test plus ten-row runtime assertion |
| Existing visible member can join and receive effects | pure state test plus internal callback/runtime row assertion |
| `ERC` clears before a stale delta | state test plus internal callback/runtime row-empty assertion |
| Renewal retains node/order | internal callback/runtime node-identity assertion |
| Rows do not overlap vertically | 50 px metric and 509 px list self-test |
| Unlimited row extends without widening/clipping | six-icon 101 px metric and scene assertion |
| Dynamic icons follow current scale | 1x/2x/1x runtime assertions |

**Step 8: Commit**

```bash
git add Scripts/UI/PartyWindow.cs Scripts/UI/PartyMember.cs Scenes/UI/PartyMember.tscn Scenes/UI/PartyWindow.tscn Scripts/PartyMemberMetrics.cs tests/Goose2Client.Tests/PartyMemberMetricsTests.cs tests/Goose2Client.Tests/PartyWindowCapacityTests.cs Scripts/UiScaleSelfTest.cs tools/tests/run_ui_scale.sh
git commit -m "feat: show buffs below party members"
```

## Task 7: Cross-repository verification and manual smoke test

**Files:**
- Verify only; fix defects in the owning task's files and commit them to the corresponding repository.

**Step 1: Run the complete server suite**

```bash
cd /home/agent/workspace/illutiagooseserver/.worktrees/party-member-buffs
dotnet test Goose.sln --no-restore
```

Expected baseline-compatible result:

- `Goose.Tests`: at least 1009 existing tests plus new party-buff tests, zero failures.
- `Goose.IntegrationTests`: 284 tests, zero failures.
- `Tools.Tests`: 137 passed, 26 environment-dependent skips, zero failures.

Existing analyzer warnings are baseline; introduce no new warnings in changed files.

**Step 2: Run the complete client suite**

```bash
cd /home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs
dotnet test --no-restore
tools/tests/run_ui_scale.sh
```

Expected: every test project passes; the Godot UI-scale self-test exits 0 when a compatible Godot binary is available. Compare warnings against baseline and introduce none in changed files.

**Step 3: Inspect protocol compatibility and repository state**

```bash
cd /home/agent/workspace/illutiagooseserver/.worktrees/party-member-buffs
git status --short
git diff master...HEAD --check

cd /home/agent/workspace/Goose2ClientGodot/.worktrees/party-member-buffs
git status --short
git diff master...HEAD --check
```

Expected: clean worktrees and no whitespace errors. Confirm old clients can ignore unknown `PB*` prefixes and the new client simply receives no party icons from an old server.

**Step 4: Manual live-server matrix**

With two grouped clients:

1. Start in range with no effects: no icons after the clear snapshot.
2. Add several beneficial effects and a poison/root/stun: all non-item effects appear in server order.
3. Equip/apply an item effect: it does not appear in the party row.
4. Renew the same effect: its position remains and tooltip time resets.
5. Upgrade a stacking effect to a different effect ID: old icon disappears before the replacement appears.
6. Wait for final ten seconds: sweep turns red; icon does not blink and has no countdown text.
7. Hover: tooltip name and time update; permanent effect shows name only.
8. Let the timer reach zero: icon remains fully red until server removal arrives, then disappears.
9. Walk out of range: icons clear immediately while the party roster remains.
10. Change buffs while out of range, then return: clear-plus-snapshot shows only current effects.
11. Warp within the map and across maps: no stale icons survive erasure/map load.
12. Fill all ten party slots: every server-supported member has a client row and can show effects.
13. Display at least six effects: icons remain on one 16 px row, extend beyond the 87 px frame, and do not wrap or clip.
14. Test UI scale 1x and 2x: party rows and dynamic icons scale and return cleanly to 1x.
15. Click and double-click party icons: no `KBUF` packet is sent and no effect is removed.

**Step 5: Final invariant matrix**

| End-to-end invariant | Automated proof | Manual proof |
|----------------------|-----------------|--------------|
| Server sends only non-item visible-party effects | server sync/visibility tests | steps 2-3, 9-10 |
| Incremental add/renew/remove stays consistent | server delta + client state tests | steps 4-8 |
| Visibility exit/re-entry cannot leave stale state | lifecycle and state adversarial tests | steps 9-11 |
| Name/icon/time presentation matches design | parser/presentation/self-tests | steps 6-8 |
| All ten server party slots are represented | capacity and UI-scale self-tests | step 12 |
| Unlimited compact row matches approved layout | metrics and UI-scale self-test | steps 13-14 |
| Party icons cannot remove another player's effect | absence of removal/input path | step 15 |

No additional commit is required if verification is clean. Any correction should be committed in the repository that owns it with a focused `fix:` commit.
