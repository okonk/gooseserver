# NPC Unreachable-Teleport Implementation Plan

**Goal:** Add an opt-in stuck behaviour (`TeleportAggroIfUnreachable`) that warps the aggro target to the NPC immediately when a bounded flood-fill proves the target is unreachable, closing the cheese window without false-positiving legitimate players.

**Architecture:** New `BehaviourTypes` enum value; a static-only reachability BFS on `Map` (clipped to the aggro-hold vision box); a fast path in `NPC.HandleAttackEvent` that runs before the existing timeout switch, which also gains the new mode on its `TeleportAggro` branch so kiting is still punished by the timer.

**Tech Stack:** C# / .NET 10, xUnit. Design: `docs/plans/2026-10-03-npc-unreachable-teleport-design.md`.

**Test placement note:** the design said `Goose.IntegrationTests`; these tests build a synthetic world, which per `docs/testing.md` belongs in `Goose.Tests` (precedent: `Goose.Tests/InvisibilityAggroTests.cs`). Same test cases, faster project.

---

## APIs verified

- `NPCTemplate.BehaviourTypes` — `Goose/NPCTemplate.cs:26-31` (`DoNothing=0, TeleportToAggro, TeleportAggro`)
- CSV mirror enum — `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:111-116`; column def `Col.Enum<BehaviourTypes>("stuck_behaviour", ...)` at `:82`
- DB load int-cast — `Goose/NPCHandler.cs:142-143`
- `NPC.Behaviour` / `NPC.BehaviourTimeout` — read live through `this.NPCTemplate` (`Goose/NPC.cs:354-355`); `SpawnNPC` stores the template by reference (`Goose/NPC.cs:652`), so mutating the template before/after spawn takes effect
- `NPC.LastAttackTime` — public get/set (`Goose/NPC.cs:356`); `TimeNow` is `Stopwatch.GetTimestamp()`, `TimerFrequency` is `Stopwatch.Frequency` (`Goose/GameWorld.cs:185-187, 208`) → tests rewind time by setting `LastAttackTime = world.TimeNow - N * world.TimerFrequency`
- Stuck switch — `Goose/NPC.cs:1336-1371`; `TeleportAggro` branch warps via `AggroTarget.WarpTo(world, this.Map, this.MapX, this.MapY, loseaggro: false)` (`:1341-1348`, ending with `SendMessageToRange(world, this.NPCTemplate.StuckMessage)` at `:1347`), then falls through to the attack check at `:1373-1383` (attack test is distance-only, no LOS)
- Attack scheduling guard — `AddAttackEvent` no-ops when `AttackSpeed <= 0 || AttackRange <= 0` (`Goose/NPC.cs:1409-1411`); tests call `HandleAttackEvent` directly, so this only constrains template values
- `Player.WarpTo(world, map, x, y, loseaggro)` — `Goose/Player.cs:1451`; same-map path sets position then `PlaceCharacter` (lands adjacent, not on, the NPC's tile)
- Aggro hold box — `Math.Abs(dx) < Map.RANGE_X (24) && Math.Abs(dy) < Map.RANGE_Y (16)` (`Goose/NPC.cs:442-444`; `Goose/Map.cs:16-17`, `public static int`)
- Tile blocking — `Map.IsTileBlocked` includes character occupancy (`Goose/Map.cs:414-439`) → **do not use for BFS**; `CanMoveTo` treats `WarpTile` as impassable for NPCs (`Goose/Map.cs:281-286`); tile array indexing `tiles[y * Width + x]`, valid coords `1..Width`, `1..Height` (`Goose/Map.cs:273, 417, 422`)
- `Map.SetTile(int, int, ITile)` — `Goose/Map.cs:612`; `BlockedTile` / `WarpTile` — `Goose/BlockedTile.cs`, `Goose/WarpTile.cs`
- NPC spawn needs a registered class — pattern via reflection into `ClassHandler.classes` (`Goose.Tests/InvisibilityAggroTests.cs:37-47`); `SpawnNPC(world, mapId, x, y, template, shouldRespawn: false)` (`Goose/NPCHandler.cs:367`)
- `npc.AddAggro(player, value, world)` — `Goose/NPC.cs:909-911`; sets `LastAttackTime = TimeNow` on first aggro (`:943-945`, which also sends `AggroMessage` via `SendMessageToRange`)
- `NPC.SendMessageToRange(GameWorld, string)` — private, `Goose/NPC.cs:914-923`; no-ops on empty message, else `P.ServerMessage` to `Map.GetPlayersInRange(this)`. Tests capture it via the player `SendBuffer` pattern (`Goose.Tests/InvisibilityAggroTests.cs:122`)
- Root/Stun checks read `buff.SpellEffect.EffectType` (`Goose/NPC.cs:1317-1333`); `Buff` construction pattern in `Goose.Tests/InvisibilityAggroTests.cs:113-120`

## Threading

Everything here runs on the game thread inside event handlers; the BFS scratch buffer on `Map` is safe without synchronisation. State this invariant once in the `CanReachTile` region, not per call site.

---

### Task 1: Add the new behaviour enum value

**Files:**
- Modify: `Goose/NPCTemplate.cs:26-31`
- Modify: `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:111-116`

**Step 1: Append the value in both enums** (must be appended — existing DB rows store ints; `NPCHandler.cs:142` casts them directly):

```csharp
public enum BehaviourTypes
{
    DoNothing = 0,
    TeleportToAggro,
    TeleportAggro,
    TeleportAggroIfUnreachable,
}
```

**Step 2: Build + run fast suite.** `dotnet test Goose.Tests/Goose.Tests.csproj` — expect 1637 passed. The CsvToSql snapshot test is unaffected (name-based mapping; no shipped row uses the new value).

**Step 3: Commit** — `git add -A && git commit -m "feat(npcs): add TeleportAggroIfUnreachable stuck behaviour value"`

**Mutation impact:**
- Source of truth: `stuck_behaviour` column → `NPCTemplate.Behaviour` (`Goose/NPCHandler.cs:142`)
- Readers: `NPC.Behaviour` getter (`Goose/NPC.cs:354`), switch in `HandleAttackEvent` (`Goose/NPC.cs:1339`) — switch gains a case in Task 3; until then the new value behaves like `DoNothing`
- Derived state: none (no persistence writes of `Behaviour`)
- Invariant: existing int values 0–2 unchanged — guaranteed by appending
- Proof: compile + existing suite green; behaviour proven in Task 3

---

### Task 2: `Map.CanReachTile` reachability BFS

**Files:**
- Modify: `Goose/Map.cs` (add near `IsTileBlocked`, ~line 440)
- Test: Create `Goose.Tests/MapReachabilityTests.cs`

**Step 1: Write the failing tests.** Synthetic 20×20 map, tiles/characters arrays allocated like `Goose.Tests/InvisibilityAggroTests.cs:28-31` (no `GameWorld` needed — `CanReachTile` is pure map state; if you find it needs the world, stop and reconsider the signature).

```csharp
[Fact] public void CanReachTile_GapInWall_ReturnsTrue() { ... }
[Fact] public void CanReachTile_SealedPocket_ReturnsFalse() { ... }
[Fact] public void CanReachTile_PortalOnlyRoute_ReturnsFalse() { ... }   // ring of WarpTile
[Fact] public void CanReachTile_CharactersArePassThrough_ReturnsTrue() { ... } // BlockedTile wall with doorway occupied by a Player; must still be reachable
```

Setup sketch: vertical `BlockedTile` wall at x=10 with a doorway at y=10; NPC side x=5, target side x=15. Sealed pocket: `BlockedTile` ring around (17,17). The pass-through test is the adversarial one: it fails if the implementation calls `IsTileBlocked` (which counts characters) instead of a static-only check — place any `ICharacter` in the doorway via `map.SetCharacter`.

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~MapReachabilityTests` — expect compile failure (method missing).

**Step 2: Implement.**

```csharp
public bool IsTileStaticBlocked(int x, int y)
{
    if (x < 1 || x > this.Width || y < 1 || y > this.Height) return true;
    ITile? tile = this.tiles[y * this.Width + x];
    return tile is BlockedTile or WarpTile;
}

// Runs on the game thread only; scratch buffers are not thread-safe.
public bool CanReachTile(int fromX, int fromY, int targetX, int targetY, int radius)
```

`CanReachTile` contract:
- 4-directional BFS from `(fromX, fromY)` over tiles where `!IsTileStaticBlocked`.
- Search clipped to the aggro-hold box: `[fromX - (RANGE_X - 1), fromX + (RANGE_X - 1)] × [fromY - (RANGE_Y - 1), fromY + (RANGE_Y - 1)]`, intersected with map bounds. Rationale (one comment allowed): matches the aggro-drop rule at `NPC.cs:442`, so "reachable" means reachable while the NPC can still hold aggro.
- Success when a visited tile satisfies `Math.Max(Math.Abs(x - targetX), Math.Abs(y - targetY)) <= radius` (Chebyshev, matching the attack check at `NPC.cs:1376-1377`); check the start tile too.
- Early exit on success. Visited tracking: two private fields on `Map` — `int[]? reachStamp` sized to the clipped box (reallocate only when a larger box is needed) and a `long reachStampCounter` incremented per call; index with `(x - minX) + (y - minY) * boxWidth`. No per-call allocation in steady state.
- Characters are never consulted.

**Step 3: Green.** Same filter command, expect 4 passed.

**Step 4: Commit** — `git commit -am "feat(maps): add static reachability flood fill bounded by aggro box"`

**Invariant → test matrix:**

| Invariant | Proved by |
|---|---|
| Only static geometry blocks the path | `CanReachTile_CharactersArePassThrough_ReturnsTrue` |
| Warp tiles are walls for NPCs | `CanReachTile_PortalOnlyRoute_ReturnsFalse` (mirrors `Map.cs:281-286`) |
| Success radius is Chebyshev `radius` | `SealedPocket` with `radius=1` false + `GapInWall` with `radius=1` adjacent-to-gap true |

---

### Task 3: NPC fast path + timeout branch

**Files:**
- Modify: `Goose/NPC.cs:1335-1371` (`HandleAttackEvent`)
- Test: Create `Goose.Tests/NPCUnreachableTeleportTests.cs`

**Step 1: Write the failing tests.** Fixture copies the `InvisibilityAggroTests` pattern: temp `DataPath`, `GameWorld`, 20×20 map with `tiles`/`characters` arrays, reflection class registration, `NewPlayer()` with unconnected socket, `SpawnNPC`. Template: `AttackRange = 1, AttackSpeed = 1, MoveSpeed = 0, AggroRange = 15, Behaviour = TeleportAggroIfUnreachable, BehaviourTimeout = 60`. `MoveSpeed = 0` keeps `AddMoveEvent` from scheduling (`NPC.cs:747`); tests call `npc.HandleAttackEvent(world)` directly. Assert warps by position: after a warp the player sits within Chebyshev 2 of the NPC (WarpTo → `PlaceCharacter` lands adjacent, not stacked).

1. **Cheese (red before this task):** sealed pocket at (17,17), NPC at (5,5), `AddAggro`, then `HandleAttackEvent` once → player warped near NPC. Fresh aggro means the 60s timer has NOT expired, so this can only pass via the fast path. Set the template's `StuckMessage` and assert it reaches the player's `SendBuffer` (pattern at `InvisibilityAggroTests.cs:122`) — the fast path must send it like the timer branches do.
2. **Reachable far player, timer fresh:** open map, player at (17,5) → `HandleAttackEvent` → player position unchanged (no fast-path warp; the fall-through attack at distance 12 can't fire).
3. **Reachable far player, timer expired (regression for the switch change):** same setup, `npc.LastAttackTime = world.TimeNow - 61 * world.TimerFrequency` → warped by the timer branch.
4. **Threshold:** unreachable pocket but player at (7,7) (Chebyshev 2 ≤ `max(4, AttackRange+1)`) → not warped.
5. **Rooted:** pocket setup + `Root` buff on the NPC (pattern from `InvisibilityAggroTests.cs:113-120`) → not warped.
6. **Old mode unchanged:** `Behaviour = TeleportAggro`, pocket, timer fresh → not warped (fast path must be mode-gated).

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter FullyQualifiedName~NPCUnreachableTeleportTests` — expect tests 1 and 3 to fail (no code path handles the new mode yet), and 2, 4, 5, 6 to pass trivially (current code never warps under the new mode). Tests 1 and 3 are the red drivers.

**Step 2: Implement the fast path** in `HandleAttackEvent`, inserted after the buff loop and before the existing `if (!rooted && ...timeout...)` block:

```csharp
if (!rooted && this.Behaviour == NPCTemplate.BehaviourTypes.TeleportAggroIfUnreachable &&
    this.AggroTarget is not null && this.AggroTarget.Map == this.Map &&
    Math.Max(Math.Abs(this.MapX - this.AggroTarget.MapX),
             Math.Abs(this.MapY - this.AggroTarget.MapY)) > Math.Max(4, this.AttackRange + 1) &&
    !this.Map.CanReachTile(this.MapX, this.MapY,
        this.AggroTarget.MapX, this.AggroTarget.MapY, this.AttackRange))
{
    this.AggroTarget.WarpTo(world, this.Map, this.MapX, this.MapY, false);
    this.LastAttackTime = world.TimeNow;
    this.SendMessageToRange(world, this.NPCTemplate.StuckMessage);
}
```

Note vs design doc: the warp falls through to the attack check below (exactly like the `TeleportAggro` timer branch at `NPC.cs:1341-1348`) instead of returning — the warped player is adjacent and gets hit this tick. The `LastAttackTime` reset is the spam guard: at most one warp per attack cycle. The `AggroTarget is null` check protects only the fast path; the pre-existing dereference at `NPC.cs:1373` is out of scope.

**Step 3: Extend the timeout switch** — add the new mode as a fall-through case onto the existing `TeleportAggro` branch (`NPC.cs:1341`) so the timer still warps under the new mode; the shared case inherits the `StuckMessage` send at `:1347` automatically.

**Step 4: Green** — full fast suite, expect 1658 + 10 new passed.

**Step 5: Commit** — `git commit -am "feat(npcs): teleport aggro target immediately when unreachable"`

**Mutation impact:**
- Source of truth changed: player position via `WarpTo` (`Goose/Player.cs:1451`) and `NPC.LastAttackTime` (`Goose/NPC.cs:356`)
- Readers: map grid (`SetCharacter` inside `WarpTo`), client packets (`WarpTo` sends ERC/MKC/SetYourPosition itself; `StuckMessage` goes out via `SendMessageToRange`), aggro maps (`loseaggro: false` keeps them — same as the existing branch at `NPC.cs:1343-1344`)
- Derived state: none beyond what `WarpTo` already maintains — no extra propagation needed
- Invariants: aggro preserved through the warp; at most one warp per attack cycle; rooted/stunned NPCs never warp
- Observable proof: tests assert final player position (not that `WarpTo` was called); test 5 proves the rooted gate; test 6 proves mode gating

---

### Task 4: Full verification + rollout notes

**Step 1:** `dotnet test Goose.Tests/Goose.Tests.csproj` (all green), then `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj` — the CsvToSql snapshot must be unchanged (no shipped row uses value 3).

**Step 2 (data, not code — @goose-game-data):** set the boss-map NPC's `stuck_behaviour` to `TeleportAggroIfUnreachable` and restore `stuck_timeout` from 5 to 20. Rollback = set `stuck_behaviour` back to `TeleportAggro`.

**Step 3:** Commit any stragglers; done.

---

## Design alignment check

| Design promise | Plan location |
|---|---|
| Enum appended, value 3, mirrored in CsvToSql | Task 1 |
| BFS: 4-dir, aggro-box clip, static-only, Chebyshev radius, early exit, stamp buffer | Task 2 |
| Threshold `max(4, AttackRange+1)`, radius `AttackRange` | Task 3 Step 2 |
| `WarpTo(..., loseaggro: false)` + `LastAttackTime` reset; `!rooted` gate; same-map + null guard | Task 3 Step 2 |
| Timer branch shared with `TeleportAggro` | Task 3 Step 3 |
| Tests 1–5 from design (in Goose.Tests, per testing.md) | Tasks 2–3 |
| Rollout: boss map opts in, timeout → 20s | Task 4 |
