# Invisibility Feature Implementation Plan

**Goal:** Add buff-driven invisibility (players, pets, NPCs) with a see-invisible counter, the MKC/CHP "Invis thing" field, a new SINVS packet, attack/cast breaking, and NPC aggro gating — independent of GM invisibility.

**Architecture:** Invis state is derived from buff effect types: `Player`/`NPC` each carry `InvisibleBuffCount` and `SeeInvisibleBuffCount` maintained in their `AddBuff`/`RemoveBuff`. State transitions (0→1, 1→0) fire the side effects (CHP broadcast, aggro clear, SINVS). Melee attacks and successful casts call `BreakInvisibility`, which removes all `Invisible` buffs through the normal remove path. NPCs gain a `see_invisible` template flag ORed with their buff count.

**Tech Stack:** C#/.NET (Goose server, xunit tests, CsvToSql SQLite generator).

Design doc: `docs/plans/2026-08-17-invisibility-design.md` (approved).

---

## APIs verified (path:line from the worktree)

- `P.MakeCharacter` (player MKC) `Goose/Packets.cs:81`; the six `// Invis thing` hardcoded sites: `Goose/Packets.cs:112` (MakeCharacter), `:144` (UpdateCharacter), `:166` (UpdateNPC), `:193` (MakeNPCCharacter), `:221` (MakePetCharacter), `:244` (UpdatePet).
- GM check pattern: `player.Access > Player.AccessStatus.Normal` (`Goose/Packets.cs:128`).
- `Player.AddBuff(Buff, GameWorld, bool refreshbar, bool updateCharacter = true)` `Goose/Player.cs:2121`. Branches: loading early-return (`State <= States.LoadingGame`, adds buff + stats, returns) at :2124; `BuffDoesntStackOver` no-op return; renew branch (`b.SpellEffect = buff.SpellEffect`, returns) at ~:2149; new-add path (expiry event, `this.Buffs.Add(buff)` at ~:2196, packets, `SendBuffBar`).
- `Player.RemoveBuff(Buff, GameWorld, bool refreshbar, bool updateCharacter = true)` `Goose/Player.cs:2277` — `this.Buffs.Remove(buff)` first, then stats/expiry/packets (range broadcast only when `State == States.Ready`).
- `Player.Buffs` `Goose/Player.cs:362` (`List<Buff>`); `Player.Attack(ICharacter, GameWorld)` `Goose/Player.cs:1640`; `Player.Send(string)` `Goose/Player.cs:2455` (unconnected non-blocking socket → whole payload lands in `SendBuffer`).
- `NPC.AddBuff(Buff, GameWorld)` `Goose/NPC.cs:1469` (same branch shapes; range computed at top); `NPC.RemoveBuff(Buff, GameWorld)` `Goose/NPC.cs:1570` (range broadcast only when `State == States.Alive`); `NPC.Buffs` `Goose/NPC.cs:333`; `NPC.Attack` `Goose/NPC.cs:1370`; `NPC.AggroIfInRange(Player, GameWorld)` `Goose/NPC.cs:988`; `NPC.RemoveAggro(Player)` `Goose/NPC.cs:960`; `NPC.LoadFromTemplate` flag copies at ~`Goose/NPC.cs:612` (`CanBeStunned` etc.).
- `ICharacter.AddBuff/RemoveBuff` declarations `Goose/ICharacter.cs:140-141`. Implementors are exactly `Player` and `NPC` (verified — no third implementation; `Pet : Player` `Goose/Pet.cs:16`).
- `SpellEffect.Cast(ICharacter, ICharacter, GameWorld)` `Goose/SpellEffect.cs:1096`: PVP early-out, Target path `return this.CastSpell(...)` at :1102, AoE body ends `return true` at ~:1349. `CastBuffSpell` `Goose/SpellEffect.cs:740` with the existing Invisible aggro-clear block at :772-781. `EffectTypes.Invisible/SeeInvisible` `Goose/SpellEffect.cs:85-86`. `SpellEffect()` ctor initializes `Stats`, `BuffStacksOver`, `BuffDoesntStackOver` (`Goose/SpellEffect.cs:229`). `CanCastSpell` `Goose/SpellEffect.cs:844` — self-casts require `Effected & SpellEffected.Self`.
- `DoneLoadingMapEvent.Ready` `Goose/Events/DoneLoadingMapEvent.cs` — sends `P.MakeCharacter(this.Player)` to the player and in-range players; also calls `npc.AggroIfInRange` per in-range NPC.
- `NPCHandler` template load: `Goose/NPCHandler.cs:52-62` (`stunnable` at :60, pattern `("0".Equals(Convert.ToString(reader["x"])) ? false : true)`); `credit_dealer` uses explicit-`"0"` comparison at ~:108.
- `NpcCsvToSql` columns `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:10-26` (`Col.Bool("stunnable", def: false)` at :23).
- Snapshot test `Goose.Tests/CsvToSqlSnapshotTests.cs` — regenerable via `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.Tests --filter FullyQualifiedName~CsvToSqlSnapshot`; fixture input `Goose.Tests/Fixtures/aspereta-data.xlsx`; snapshot `Goose.Tests/Fixtures/generated.snapshot`.
- Test recipes: map sizing + class injection + `world.NPCHandler.SpawnNPC(world, MapId, x, y, template, shouldRespawn)` per `Goose.Tests/NPCSpawnRegistrationTests.cs:26-74` (map needs `characters`/`tiles` arrays sized `(Width+1)*(Height+1)`; class must have a row for the NPC's level — `NPC.LoadFromTemplate` dereferences `Class.GetLevel(Level)`); packet capture via unconnected socket + `SendBuffer` per `Goose.Tests/PlayerSendTests.cs:11-20`.
- `Map.GetPlayersInRange(ICharacter)` / `Map.GetNPCsInRange(ICharacter)` — used throughout (`Goose/SpellEffect.cs:755,774`).

## Persistence strategy

- Player data: no changes.
- `npc_templates` gains `see_invisible`: strategy is the project's normal CSV-pipeline flow — the owner adds the column to the spreadsheet, `CsvToSql` emits it (Task 7), the regenerated `npcs.sql` is applied to databases (in-game `/updatesql` or re-import). **No automatic migration in code.** `NPCHandler` reads `reader["see_invisible"]` and will throw at startup against an un-migrated DB — acceptable, same as any prior schema change; do not deploy the server before re-running the SQL.

## Concurrency invariant

Every hook added (AddBuff/RemoveBuff transitions, Attack, Cast, AggroIfInRange, map-load) runs on the game thread only — the same thread as the existing buff/aggro machinery. No marshaling or locking is added or needed.

## Per-task test setup notes (shared)

- World/settings: follow `Goose.Tests/NPCSpawnRegistrationTests.cs:15-52` exactly — `[Collection(GameWorldSettingsCollection.Name)]` + swap/restore the static `GameWorld.Settings` with an isolated temp `DataPath` in ctor/`Dispose`. Do NOT swap Settings without the collection attribute (parallel-collection flake).
- Player that can receive/observe packets: `var p = new Player(0);` then set `p.Inventory = new Inventory(p);` (the `Player(int)` ctor at `Goose/Player.cs:483` does NOT initialize it — only `LoadFromAutoCreate` :623 / `LoadFromReader` :767 do; `P.UpdateCharacter`/`P.StatusInfo`/`Player.Attack` all dereference it), `p.Class` (any `Class` with a level row), `p.BaseStats = new AttributeSet { HP = 100, MP = 100 }; p.MaxStats = p.BaseStats + new AttributeSet(); p.CurrentHP = 100; p.CurrentMP = 100;` — `MaxHP`/`MaxMP` are GETTER-ONLY (`Goose/Player.cs:196-208`, computed from `MaxStats`), VPU divides by them (`Goose/Packets.cs:406`), `p.State = Player.States.Ready`, and an unconnected non-blocking socket (PlayerSendTests pattern) so `GameWorld.Send` lands in `p.SendBuffer` (`Goose/GameWorld.cs:633` appends `\x1`; decode as ASCII for asserts).
- Two players on one map: size `map.characters`/`tiles`, `world.MapHandler.Maps[MapId] = map`, and for EACH player: `map.AddPlayer(p, world)` (`Goose/Map.cs:185`) + `map.PlaceCharacter(...)` + `map.SetCharacter(...)`. `Map.GetPlayersInRange` (`Goose/Map.cs:160`) iterates the private `players` list (populated ONLY by `AddPlayer`) and EXCLUDES the target character itself (`p != character`) — so range broadcasts reach bystanders only; direct self-sends (SINVS, own MKC) are separate `world.Send` calls, which is why tests assert on the subject's own `SendBuffer` for SINVS.
- NPC: `world.NPCHandler.SpawnNPC(world, MapId, x, y, new NPCTemplate { NPCTemplateID = 1, Name = "t", Level = 50, ClassID = classId, BaseStats = new AttributeSet(), AggroRange = 5 }, shouldRespawn: false)` after registering a level-50 class (NPCSpawnRegistrationTests `RegisterClass` reflection pattern).
- Buffs: `new Buff { Target = x, Caster = x, SpellEffect = new SpellEffect { EffectType = SpellEffect.EffectTypes.Invisible, Duration = 1000 } }` — the `SpellEffect()` ctor leaves `Stats`/stack lists non-null (`Goose/SpellEffect.cs:229`).

---

### Task 1: Packet — `P.SeeInvisible` and the six Invis field sites

**Files:**
- Modify: `Goose/Packets.cs:81-244` (the six `// Invis thing` sites)
- Test: `Goose.Tests/InvisibilityPacketTests.cs` (create)

**Step 1: Write the failing tests**

For each of `P.MakeCharacter` (player), `P.UpdateCharacter` (player), `P.MakePetCharacter`/`P.UpdatePet` (a `Pet`), `P.MakeNPCCharacter`/`P.UpdateNPC` (an `NPC`):

- Assert the packet equals what the fully-informed expected string is *only at the invis field*: set the player's `FaceID = 70` (unique non-default), no buffs → assert the packet contains `",0,70,"`; with the character's invis count at 1 (set the counter property directly — Task 2 adds it, so instead for Task 1 test the packet funcs with a **plain** character and assert `",0,70,"`, and defer the `",1,70,"` assertions to Task 2/3 tests once counters exist).

Red-phase note: with counters not yet existing, write Task 1's tests as: (a) new `P.SeeInvisible(true)` == `"SINVS1"` and `P.SeeInvisible(false)` == `"SINVS0"`; (b) all six packet builders still produce a parseable MKC/CHP with the invis field present as `"0"` — this pins the field's position (count commas) so Task 2's change cannot silently shift the layout.

**Step 2: Run (red)** — `dotnet test Goose.Tests --filter FullyQualifiedName~InvisibilityPacketTests` — FAIL: `P.SeeInvisible` does not exist.

**Step 3: Implement**

- Add near the other character packets:
  ```csharp
  public static Func<bool, string> SeeInvisible = (canSee) => "SINVS" + (canSee ? "1" : "0");
  ```
- Replace the six `"0" + "," + // Invis thing` with the character's state:
  - `MakeCharacter`/`UpdateCharacter`/pet builders: `(player.IsInvisible ? "1" : "0")` / `(pet.IsInvisible ? "1" : "0")`
  - `MakeNPCCharacter`/`UpdateNPC`: `(npc.IsInvisible ? "1" : "0")`
  - `IsInvisible` is added in Task 2 — so implement Task 1's field edits together with Task 2 (they cannot compile before it). **Execution note: do Step 3 of Task 1 together with Task 2's counters, in Task 2's commit.** Task 1's own commit contains only `P.SeeInvisible` + the tests from its Step 1 (minus the field-value asserts, which move to Task 2).
- Update the `// Invis thing` comments to `// Invisible` on the touched lines (now correct), and update the `invis = not sure at moment` line in the MKC doc comment (`Goose/Packets.cs:~79`) to state it's the Invisible-buff state. Do not touch the hardcoded sites in `HairdyeCommandEvent`/`CustomCommandEvent`/`GMHaxCommandEvent`.

**Step 4: Run (green)** — same filter — PASS.

**Step 5: Commit** — `git commit -m "Add SINVS packet"`. (Field-site edits ship in Task 2's commit.)

| Invariant | Proved by |
|---|---|
| SINVS wire format is bare `SINVS1`/`SINVS0` | `SeeInvisible_...` tests |
| Invis field position in all six MKC/CHP builders is stable | comma-position/field tests |

---

### Task 2: Counters, `IsInvisible`, `CanSeeInvisible`, template flag + wire the six packet sites

**Files:**
- Modify: `Goose/Player.cs` (counters + `IsInvisible` near `Buffs` :362; counter maintenance in `AddBuff` :2121 and `RemoveBuff` :2277)
- Modify: `Goose/NPC.cs` (same; property `CanSeeInvisible`; `LoadFromTemplate` ~:612)
- Modify: `Goose/NPCTemplate.cs` (new `public bool SeeInvisible { get; set; }`)
- Modify: `Goose/NPCHandler.cs:52-62` (read the column)
- Modify: `Goose/Packets.cs` (six Invis field sites, completing Task 1 Step 3)
- Test: `Goose.Tests/InvisibilityCounterTests.cs` (create)

**Mutation impact:**
- Source of truth: the counters are the authoritative invis state (public set — scripts may set them directly, per owner decision). `AddBuff`/`RemoveBuff` keep them in sync with the buff list (`Player.Buffs` `Goose/Player.cs:362`, `NPC.Buffs` `Goose/NPC.cs:333`) for buff-driven changes.
- Important readers: new `IsInvisible`/`CanSeeInvisible` (this task), packet builders (this task), later tasks (broadcasts, aggro gating), game scripts (direct writes).
- Buff-sync invariant (what this task maintains): at every quiescent point reached through buff operations, each counter equals the count of buffs whose `SpellEffect.EffectType` is `Invisible`/`SeeInvisible`. Direct script writes may (by design) deviate from that; tests only exercise buff operations.
- Required propagation sequence (every counter must move at every point a buff of either type enters/leaves the list):
  1. `Player.AddBuff`: (a) loading early-return branch — increment after `this.Buffs.Add(buff)`; (b) renew branch — when `b.SpellEffect` is replaced by a *different* effect type, decrement the old type's counter and increment the new type's (only if either is Invisible/SeeInvisible); (c) new-add path — increment after `this.Buffs.Add(buff)` (~:2196).
  2. `Player.RemoveBuff`: `List.Remove` returns bool — decrement only when it returned true (guards double-remove from negative counts), before the rest of the method body.
  3. `NPC.AddBuff`/`NPC.RemoveBuff`: same three points (NPC has no loading branch; it has the renew branch at ~:1480).
- Invariants to preserve: counters never negative; counters equal a fresh count over `Buffs` at every quiescent point; non-invis buff types don't touch counters; `BuffDoesntStackOver` no-op return leaves counters untouched (no buff was added).
- Observable proof required: tests assert `IsInvisible`/`CanSeeInvisible` and the packet field value after real `AddBuff`/`RemoveBuff` calls, not the counters directly only.

**Step 1: Write the failing tests** (`InvisibilityCounterTests`)

Using the shared setup notes:
1. Add an Invisible buff to a Ready player → `IsInvisible` true, `P.UpdateCharacter` field `",1,70,"` (FaceID 70). Remove it → `IsInvisible` false, `",0,70,"`.
2. Same for an NPC via `npc.AddBuff`/`npc.RemoveBuff` → `IsInvisible` true/false; `P.UpdateNPC` field flips.
3. Stacks: two different Invisible `SpellEffect`s → still invisible; remove one → still invisible; remove both → visible.
4. A `Buff`-type (non-invis) buff → counters unchanged.
5. Renew branch: player has Invisible spell A; add a second buff whose `SpellEffect.BuffStacksOver` contains A with a *non-invis* effect type → `IsInvisible` false (counter decremented on type change). (Adversarial: fails if the renew branch doesn't adjust.)
6. Double-remove (behavioral — `IsInvisible => count > 0` is false for both 0 AND -1, so it alone can't detect the bug): add an Invisible buff → `RemoveBuff` it TWICE → add a NEW Invisible buff → `IsInvisible` MUST be true (counter: 1, guarded removals keep 0, add → 1; unguarded removals give -1+1=0 → false). Then remove the new buff → `IsInvisible` false.
7. NPC `CanSeeInvisible`: template flag true → true with no buffs; false flag + one SeeInvisible buff → true; buff removed → false.
8. NPC duplicated bookkeeping (separate code from Player — renew `Goose/NPC.cs:1474-1505`, remove `:1570-1581`): (a) NPC has Invisible spell A; add a second buff whose `BuffStacksOver` contains A with a *non-invis* effect type → `npc.IsInvisible` false; (b) NPC SeeInvisible buff renewed to a non-SeeInvisible type, base flag off → `CanSeeInvisible` false; (c) the double-remove sequence from test 6 repeated on an NPC.
9. Template copy: `NPCTemplate.SeeInvisible` defaults false; the copy constructor `new NPCTemplate(other)` preserves it (adversarial: fails if the copy ctor isn't updated); `LoadFromTemplate` copies it onto a spawned NPC (`template.SeeInvisible = true` → `npc.CanSeeInvisible` true). The `NPCHandler` `reader["see_invisible"]` read itself is code-reviewed, not unit-tested (no DB-backed handler test exists in the suite).
10. Pet: `Pet` is a `Player` — add an Invisible buff to a pet → `pet.IsInvisible` true and `P.UpdatePet` field `1`.

Red: FAIL — counters/properties don't exist.

**Step 2: Run (red)** — `dotnet test Goose.Tests --filter FullyQualifiedName~InvisibilityCounterTests`.

**Step 3: Implement**

- `Player`: `public int InvisibleBuffCount { get; set; }`, `public int SeeInvisibleBuffCount { get; set; }` (public set is INTENTIONAL — scripts set the counters directly; the counter is the authoritative invis state, and `AddBuff`/`RemoveBuff` keep it in sync for buff-driven changes), `public bool IsInvisible { get { return this.InvisibleBuffCount > 0; } }`.
- `NPC`: same two counters + `IsInvisible`, plus `public bool CanSeeInvisible { get; set; }` — **note**: this must be computed, not stored: `public bool CanSeeInvisible { get { return this.SeesInvisibleBase || this.SeeInvisibleBuffCount > 0; } }` with `public bool SeesInvisibleBase { get; set; }` copied in `LoadFromTemplate` from `template.SeeInvisible` (next to `CanBeStunned`, NPC.cs:612).
- Counter maintenance: in each AddBuff/RemoveBuff branch per the propagation sequence above. Only `SpellEffect.EffectTypes.Invisible` and `SpellEffect.EffectTypes.SeeInvisible` move counters; guard `buff.SpellEffect` null the way existing `?.Script` code does.
- `NPCTemplate.SeeInvisible` (bool); ALSO copy it in the copy constructor `NPCTemplate(NPCTemplate other)` (`Goose/NPCTemplate.cs:212`) — `this.SeeInvisible = other.SeeInvisible;` (the copy ctor serves script-generated dimension variants, `Goose/Data/Illutia/Scripts/Global/Dimensions/Npcs.csx:102-108`); `NPCHandler`: `npc.SeeInvisible = "1".Equals(Convert.ToString(reader["see_invisible"]));` — use the explicit-`"1"` form. Do NOT copy the `stunnable`/`credit_dealer` idiom (`"0".Equals(x) ? false : true`, `Goose/NPCHandler.cs:60,105`), which treats NULL and any non-`"0"` value as true; we default to false.
- `Packets.cs`: replace the six `"0" + "," + // Invis thing` with `(x.IsInvisible ? "1" : "0")` per Task 1 Step 3 and fix the comments.

**Step 4: Run (green)** — counter tests pass; also run `--filter FullyQualifiedName~InvisibilityPacketTests` (Task 1 tests still green with the field sites wired).

**Step 5: Commit** — `git add ... && git commit -m "Invis/see-invis buff counters on Player and NPC; wire MKC/CHP invis field"`.

| Invariant | Proved by |
|---|---|
| Counter == count of matching-type buffs | stack/renew/double-remove tests (assert via `IsInvisible` + packet field) |
| Renew across types adjusts counters | test 5 (adversarial) |
| `CanSeeInvisible` = base flag OR buff count | test 7 |
| Pet inherits the Player path | test 9 |
| Column defaults to false / NULL-safe; copy ctor preserves flag | handler read uses explicit `"1"` compare (code-reviewed); tests 6, 8, 9 |

---

### Task 3: Transitions — CHP broadcast, NPC aggro clear, SINVS; remove old aggro-clear from `SpellEffect`

**Files:**
- Modify: `Goose/Player.cs` (`AddBuff`/`RemoveBuff` transition hooks; new private `BroadcastInvisChange(GameWorld)` and `ClearNPCAggroIfUnseen(GameWorld)`)
- Modify: `Goose/NPC.cs` (CHP broadcast on 0→1/1→0 of `InvisibleBuffCount`; private `BroadcastInvisChange(GameWorld)`)
- Modify: `Goose/SpellEffect.cs:772-781` (delete the Invisible aggro-clear block from `CastBuffSpell`)
- Test: `Goose.Tests/InvisibilityTransitionTests.cs` (create)

**Mutation impact:**
- Source of truth: buff lists (as Task 2). This task adds *server state the client observes* (CHP, SINVS) and *NPC aggro state* (`NPC.AggroTargetToValue`/`AggroTarget`, `Goose/NPC.cs:319,315`).
- Important readers: client (CHP toggles visibility), NPC move/attack loops (aggro), `NPC.RemoveAggro` (`Goose/NPC.cs:960`).
- Derived/cached state affected: none new; aggro values are mutated via the existing `RemoveAggro` (which re-derives `AggroTarget` from `AggroTargetToValue`).
- Required propagation sequence:
  1. Player `AddBuff`/`RemoveBuff`: capture `wasInvisible`/`wasSeeInvisible` at method entry. Apply all counter mutations for the call (loading branch, renew branch — which decrements the old type AND increments the new type in one branch — new-add). Then, ONCE at the end of the mutation path, compare entry snapshot vs. resulting counters and fire each transition at most once per call (renew of an invis spell for a different invis spell must produce no packets; invis→non-invis renew fires exactly one 1→0). Transitions fire only when `this.State == States.Ready` — the loading branch runs before `Player.Map` is assigned (by `DoneLoadingMapEvent`), so `ClearNPCAggroIfUnseen` would NRE otherwise. Side effects skipped during loading are covered by Task 6 (map load sends SINVS and MKC already carries the invis flag).
     - invisible 0→1: `ClearNPCAggroIfUnseen(world)` then `BroadcastInvisChange(world)`.
     - invisible 1→0: `BroadcastInvisChange(world)`.
     - SINVS: capture `wasCanSee = this.SeeInvisibleBuffCount > 0 || this.Access > AccessStatus.Normal` at entry; after the mutations compute `canSee = <same expression>`; when `State == Ready && canSee != wasCanSee`, `world.Send(this, P.SeeInvisible(canSee))`. Exactly matches the approved design: normal player 0→1 sends `SINVS1`, 1→0 sends `SINVS0`; a GM (state permanently true) receives NO packets on either transition.
  2. `ClearNPCAggroIfUnseen`: `foreach (NPC npc in this.Map.GetNPCsInRange(this)) if (!npc.CanSeeInvisible) npc.RemoveAggro(this);`
  3. `BroadcastInvisChange` (Player): when `this.State == States.Ready`, `foreach (Player p in this.Map.GetPlayersInRange(this)) world.Send(p, P.UpdateCharacter(this));`
  4. `SendSeeInvisibleState`: `world.Send(this, P.SeeInvisible(this.SeeInvisibleBuffCount > 0 || this.Access > AccessStatus.Normal));`
  5. `RemoveBuff`: same checks after the guarded decrement.
  6. NPC `AddBuff`/`RemoveBuff`: invisible 0→1/1→0 → `BroadcastInvisChange` = `foreach (Player p in this.Map.GetPlayersInRange(this)) world.Send(p, P.UpdateNPC(this));` (in `RemoveBuff` only when `State == States.Alive`, matching its existing range block). No aggro/SINVS side effects for NPCs.
  7. Delete `Goose/SpellEffect.cs:772-781` (the `EffectTypes.Invisible` aggro-clear in `CastBuffSpell`) — the player transition now owns it, and it now respects `CanSeeInvisible`.
- Invariants to preserve: an NPC that can see the player never loses aggro when they turn invisible; the removed SpellEffect block's old behavior (clear for all in-range NPCs) is subsumed; `RemoveAggro` on a player the NPC isn't aggro-ing at is a safe no-op (dictionary Remove, `Goose/NPC.cs:961`).
- Observable proof required: tests decode captured `SendBuffer` bytes for the actual CHP/SINVS packets and inspect `npc.AggroTarget` — not helper-call counts.

**Step 1: Write the failing tests** (`InvisibilityTransitionTests`, shared setup)

1. Two Ready players on one map (both with socket capture). B-player gets an Invisible buff → A-player's `SendBuffer` contains `CHP` with invis field `1` for B's LoginID. Remove → field `0`. (Adversarial: fails if the broadcast reuses the *pre*-change flag.)
2. Same for an NPC: in-range player receives `CHP` with `1` then `0`.
3. Aggro clear: NPC (AggroRange 5) with `npc.AggroTarget == player` (give it aggro via `npc.AddAggro(player, 1, world)` — verify the existing public method signature at `Goose/NPC.cs:~920`), NPC `CanSeeInvisible` false, player gets Invisible buff → `npc.AggroTarget == null`.
4. Adversarial companion: same setup but NPC spawned with `template.SeeInvisible = true` → `npc.AggroTarget` still == player after the buff.
5. SINVS: normal player gets SeeInvisible buff → own `SendBuffer` contains `SINVS1`; remove → `SINVS0`. GM player (`Access = AccessStatus.GameMaster`): add AND remove a SeeInvisible buff → NO SINVS packet in the buffer at all (design: GMs receive nothing on either transition; the Task 6 map-load send already establishes `SINVS1`).
6. Non-invis buff add/remove → no `CHP`-with-invis-flip and no `SINVS` in either buffer (adversarial: fails if the broadcast fires unconditionally).
7. Loading-state player (State `LoadingGame`, `Map == null`) gets Invisible buff: no NRE, no packets, counter correct, `IsInvisible` true. (Adversarial: fails if the transition block isn't Ready-gated — `ClearNPCAggroIfUnseen` dereferences `Map`.)

**Step 2: Run (red)** — `--filter FullyQualifiedName~InvisibilityTransitionTests`.

**Step 3: Implement** per the propagation sequence above.

**Step 4: Run (green)** — plus full `Goose.Tests` run to catch regressions from the deleted `CastBuffSpell` block (any existing test asserting the old clear would surface here).

**Step 5: Commit** — `git commit -m "Invis/see-invis transitions: CHP broadcast, sight-aware aggro clear, SINVS"`.

| Invariant | Proved by |
|---|---|
| CHP carries post-change state to in-range players | tests 1, 2 (adversarial) |
| Aggro cleared only for NPCs that can't see | tests 3, 4 (4 is the regression gate) |
| SINVS1/SINVS0 for normal player; zero SINVS packets for GM | test 5 |
| Unrelated buffs are silent | test 6 (adversarial) |
| Loading-state add is safe | test 7 |

---

### Task 4: `BreakInvisibility` + the three call sites

**Files:**
- Modify: `Goose/ICharacter.cs:140-141` (add `void BreakInvisibility(GameWorld world);` next to `AddBuff`/`RemoveBuff`)
- Modify: `Goose/Player.cs` (implementation; `Attack` :1640 call site)
- Modify: `Goose/NPC.cs` (implementation; `Attack` :1370 call site)
- Modify: `Goose/SpellEffect.cs` (`Cast` :1096 call site)
- Test: `Goose.Tests/InvisibilityBreakTests.cs` (create)

**Mutation impact:**
- Source of truth: buff lists; removal goes through the existing `RemoveBuff`, so Task 3's propagation (counters, CHP, aggro-clear is n/a here) fires unchanged.
- Important readers: everything reading `IsInvisible` (packets, Task 5 gating).
- Required propagation sequence: `BreakInvisibility` = snapshot own `Buffs` to a list, then for each buff with `EffectType == EffectTypes.Invisible` call the normal `RemoveBuff(buff, world)`. Snapshot first — `RemoveBuff` mutates the list.
- Invariants: two stacks both removed in one call; a non-invis buff survives; each removal broadcasts via the normal path (no extra manual packets); double-break is a safe no-op; a self-cast Invisible spell leaves the caster invisible (break first, then the cast re-grants).
- Observable proof: assert `IsInvisible` false + `Buffs` contents + that a bystander received the CHP flip, not a mock of `RemoveBuff`.

**Call sites (exactly three):**
1. `Player.Attack` (`Goose/Player.cs:1640`): first statement `this.BreakInvisibility(world);` (before `OnMeleeAttack`; covers pets via `Pet : Player`; breaking even when the PVP check returns early is intended — the attack attempt was made).
2. `NPC.Attack` (`Goose/NPC.cs:1370`): first statement `this.BreakInvisibility(world);`.
3. `SpellEffect.Cast` (`Goose/SpellEffect.cs:1096`): one statement immediately AFTER the PVP early-out (`if (!this.WorksInPVP && target.Map.CanPVP) return false;` at :1098) and BEFORE the `TargetType` branch — so it covers the Target path and the entire AoE body with a single insertion, and reveals before any effect is applied. Decision (settled 2026-08-17, option a): the act of casting reveals; a failed cast (`CastSpell` returns false) also reveals; the PVP early-out does NOT break (no cast executes). A self-cast Invisible spell therefore works: the old Invisible buff is removed, then the spell re-adds one — final state invisible, with a visible 1→0→1 CHP pair to bystanders.

**Step 1: Write the failing tests** (`InvisibilityBreakTests`, shared setup)

1. Invisible player `Attack`s an NPC → `player.IsInvisible` false, `player.Buffs` has no Invisible entries, other buffs (e.g. a Root) remain. Bystander player received CHP field flip 1→0.
2. NPC (buffed invisible) `Attack`s the player → NPC `IsInvisible` false.
3. Cast: invisible player, `new SpellEffect { EffectType = EffectTypes.Buff, TargetType = TargetTypes.Target, Effected = SpellEffected.Self, Duration = 1000 }`, `se.Cast(player, player, world)` returns true → player no longer invisible (cast reveals; no Invisible buffs remain). A non-invis buff the player had survives. Bystander buffer shows exactly one CHP flip 1→0.
4. Adversarial: `WorksInPVP = false` on a `CanPVP` map → `Cast` returns false → player STAYS invisible (PVP early-out must not break).
5. Two Invisible stacks + `Player.Attack` → both removed in one attack, `IsInvisible` false.
6. Self-invis spell: invisible player casts `new SpellEffect { EffectType = EffectTypes.Invisible, TargetType = TargetTypes.Target, Effected = SpellEffected.Self, Duration = 1000 }` on themselves → `Cast` returns true → `player.IsInvisible` TRUE (old buff removed by the break, new buff added by the cast; assert `Buffs` contains exactly the new `SpellEffect` instance, not the old one). Bystander buffer: a CHP 1→0 followed by a CHP 0→1 (adversarial: fails under the rejected after-break spec, where the player would end visible).
7. Failed cast reveals: invisible player casts a spell that `CanCastSpell` rejects for the target path (e.g. target NPC with `CanBeKilled = false` for an NPC-targeted `EffectTypes.Stun` — pick the simplest reject case available) → `Cast` returns false → player VISIBLE (break happens before `CastSpell`).

**Step 2: Run (red)** — `--filter FullyQualifiedName~InvisibilityBreakTests` — FAIL: `BreakInvisibility` missing (compile error is acceptable red here; interface member added in the same commit).

**Step 3: Implement** per above.

**Step 4: Run (green)** + full test run.

**Step 5: Commit** — `git commit -m "Melee attacks and casts break invisibility"`.

| Invariant | Proved by |
|---|---|
| Attack removes all Invisible stacks, keeps others | tests 1, 5 |
| Cast reveals before it applies; PVP early-out doesn't | tests 3, 4, 7 (4 and 7 adversarial) |
| Self-invis spell still works (break-then-regrant) | test 6 (adversarial) |
| NPCs and pets share the rule | tests 1 (player), 2 (NPC); pet covered by inheritance — explicit pet case optional |
| Removal propagates through normal RemoveBuff | test 1 bystander CHP assert |

---

### Task 5: Aggro gating in `AggroIfInRange`

**Files:**
- Modify: `Goose/NPC.cs:988-991` (`AggroIfInRange` early-outs)
- Test: `Goose.Tests/InvisibilityAggroTests.cs` (create)

**Mutation impact:**
- Source of truth: `NPC.AggroTargetToValue`/`AggroTarget` — this task only *skips* a write.
- Important readers: NPC move/attack event loops (they act on `AggroTarget`).
- Required propagation sequence: none — `AggroIfInRange` returns before `AddAggro` (NPC.cs:1005) and before the allied-splash loop (NPC.cs:1007-1024).
- Invariants: invisible + unseen → no aggro, no allied splash, no `NPCAngryEmote` packet; invisible + seen (template flag OR buff) → aggros exactly as visible; visible players unaffected; the existing `!player.IsGMInvisible` call-site gates (NPC.cs:543,704) remain untouched.

**Step 1: Write the failing tests** (shared setup; NPC AggroRange 5, within range of the player)

1. Invisible player (Task 2/3 machinery), NPC `SeesInvisibleBase` false, no NPC buffs → `npc.AggroIfInRange(player, world)` → `npc.AggroTarget == null`, in-range bystander buffer has no `EAM`/angry-emote packet.
2. Adversarial pair: NPC spawned with `template.SeeInvisible = true` → `AggroTarget == player` and the angry-emote packet WAS sent.
3. NPC with a SeeInvisible buff (count > 0), base flag false → aggros.
4. Visible player, base flag false → aggros (regression: gating must not suppress normal aggro).
5. Allied splash still propagates from a seeing NPC to a non-seeing ally that is within range (design decision G9b): two NPCs, A `SeeInvisible` true, B false, ally-linked (`AlliesString`/`Allies` per `NPCTemplate.Allies`); `A.AggroIfInRange(invisiblePlayer)` → `B.AggroTarget == player`. (Adversarial: fails if the implementer "helpfully" gates the splash loops too.)

**Step 2: Run (red)** — test 1 FAILs (NPC aggros today).

**Step 3: Implement** — in `AggroIfInRange`, after `if (this.AggroTarget != null) return;`:
```csharp
if (player.IsInvisible && !this.CanSeeInvisible) return;
```

**Step 4: Run (green)** + full run.

**Step 5: Commit** — `git commit -m "NPCs that can't see invisible players do not aggro on them"`.

| Invariant | Proved by |
|---|---|
| Unseen NPC + invisible player = no aggro/splash/emote | test 1 |
| Seen via flag OR buff = normal aggro | tests 2, 3 |
| Visible players unaffected | test 4 |
| Splash deliberately un-gated | test 5 (adversarial) |

---

### Task 6: SINVS on map load

**Files:**
- Modify: `Goose/Events/DoneLoadingMapEvent.cs` (in `Ready`, after `world.Send(this.Player, P.MakeCharacter(this.Player));`)
- Test: `Goose.Tests/InvisibilityMapLoadTests.cs` (create)

**Mutation impact:** server state the client observes (SINVS). No other state changes. Required propagation: none beyond the single send — MKC already carries the invis field for everyone else, and the receiving player's own client needs its own SINVS.

**Step 1: Write the failing tests** (shared setup; drive the real event)

Construct the flow the way `DoneLoadingMapEvent` expects: set `p.MapID = MapId`, `p.MapX`/`p.MapY`, `p.State = States.LoadingMap`, unconnected socket — and do NOT pre-call `AddPlayer`/`PlaceCharacter`/`SetCharacter` on the subject: `DoneLoadingMapEvent.Ready` itself calls `PlaceCharacter`+`SetCharacter` (`Goose/Events/DoneLoadingMapEvent.cs:30-34`) and `map.AddPlayer` (:80-81), so pre-adding would double-register the player in the private `players` list, and a pre-set grid tile could make the event's `PlaceCharacter` relocate the player. Only pre-register separate OBSERVER players (AddPlayer+place) if a test needs them. Then `var ev = new DoneLoadingMapEvent { Player = p, Ticks = world.TimeNow }; world.EventHandler.AddEvent(ev); world.EventHandler.Update(world);` — copy the exact drive pattern from `Goose.Tests/LoginEventNameLengthTests.cs:55,71`.

1. GM player (`Access = GameMaster`), no buffs → after DLM processing, buffer contains `SINVS1`.
2. Normal player with a SeeInvisible buff → `SINVS1`.
3. Adversarial: normal player, no buffs → buffer contains `SINVS0` (must be explicit, not absent).

**Step 2: Run (red)** — all three FAIL (no SINVS sent).

**Step 3: Implement** — in `DoneLoadingMapEvent.Ready`, immediately after the `P.MakeCharacter(this.Player)` self-send:
```csharp
world.Send(this.Player, P.SeeInvisible(
    this.Player.SeeInvisibleBuffCount > 0 ||
    this.Player.Access > Player.AccessStatus.Normal));
```
(Keep the existing `IsGMInvisible` gating intact — that's the unrelated GM feature.)

**Step 4: Run (green)** + full run.

**Step 5: Commit** — `git commit -m "Send SINVS state on map load"`.

| Invariant | Proved by |
|---|---|
| Map load always sends the current see-invis state (GM floor, buff-driven) | tests 1-3 (3 adversarial) |

---

### Task 7: CsvToSql `see_invisible` column + snapshot regeneration

**Files:**
- Modify: `CsvToSql/CsvToSql.Core/NpcCsvToSql.cs:22-26` (add the column in the bool-flag group)
- Modify: `Goose.Tests/Fixtures/generated.snapshot` (regenerated)

**Step 1: Implement the schema change (descriptor AND fixture in lockstep)**

`CsvToSqlBase` reads worksheet cells 1:1 positionally against the descriptor list — "cells are read positionally, so the order is load-bearing" (`CsvToSql/CsvToSql.Core/CsvToSqlBase.cs:11-12`, `row.Cell(i + 1)` → `descriptors[i]` at :33-39). So the descriptor insertion and the worksheet column insertion MUST land at the same position. Owner decision (2026-08-17): the column goes in the flag group, after `invincible` — and the owner will add the column at that position in the real spreadsheet.

1a. Insert into `NpcCsvToSql.GetColumnDescriptors()` immediately after `Col.Bool("invincible", def: false)` (line 26):
```csharp
Col.Bool("see_invisible", def: false),
```
1b. Insert the matching blank column into the snapshot fixture so the positional mapping stays aligned: `Goose.Tests/Fixtures/aspereta-data.xlsx`, sheet `NPCs` — insert one new column at index 19 (after `invincible (0)` at 18, before `hp (0)`), header `see_invisible (0)`, all data cells empty:
```bash
python3 -c "
import openpyxl
p = 'Goose.Tests/Fixtures/aspereta-data.xlsx'
wb = openpyxl.load_workbook(p)
ws = wb['NPCs']
ws.insert_cols(19)
ws.cell(row=1, column=19, value='see_invisible (0)')
wb.save(p)
"
```
(verified 2026-08-17: sheet `NPCs`, `invincible (0)` is column 18, `hp (0)` is column 19, 191 rows; openpyxl installed via `pip3 install --user --break-system-packages openpyxl`.)
Empty cells are omitted from INSERTs (`CsvToSqlBase.BuildInserts` skips `value.Length == 0`), so every row takes the default and the INSERT lines in the snapshot stay byte-identical.
This is a schema task (no pre-test; the snapshot test *is* the test and is expected to go red).

**Step 2: Verify red**

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~CsvToSqlSnapshot`
Expected: FAIL — the diff is the `npc_templates` DDL gaining exactly one column (`see_invisible`, default 0); INSERT lines unchanged (empty cells are omitted from INSERTs, so rows take the default without changing).

**Step 3: Regenerate and review**

Run: `GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.Tests --filter FullyQualifiedName~CsvToSqlSnapshot`
The test rewrites `Goose.Tests/Fixtures/generated.snapshot` and fails by design. Inspect `git diff Goose.Tests/Fixtures/generated.snapshot`:
- `npc_templates` DDL must gain exactly one column: `see_invisible` (bool/int, default 0).
- No INSERT line, other table, column, or default may change (the fixture column is blank, so every row takes the default).
If any INSERT line changes, the descriptor and fixture columns are misaligned — stop and investigate before committing.
Also review `git diff --stat`: the expected changed files are `NpcCsvToSql.cs`, the fixture xlsx, and the snapshot — nothing else.

**Step 4: Verify green**

Run the snapshot test without the env var → PASS. Then full `dotnet test Goose.Tests` → all green.

**Step 5: Commit** — `git commit -m "CsvToSql: add npc_templates.see_invisible column"`.

**Owner action (not a code task):** the real spreadsheet gains the `see_invisible` column in the SAME position (after `invincible`, before `hp`), and the regenerated `npcs.sql` is applied to the game DB before deploying this server build (see Persistence strategy).

| Invariant | Proved by |
|---|---|
| Generator emits the new column with default 0; nothing else drifts | snapshot diff review + green rerun |

---

## Task dependency order

1 → 2 → 3 → 4 → 5 → 6 (each builds on the previous: 2 needs 1's `P.SeeInvisible` tests present; 3's aggro-clear needs 2's `CanSeeInvisible`; 4/5/6 need 2-3). Task 7 is independent and can land any time. One commit per task.

## Final red-team notes

- **Threading:** all new code runs on the game thread (buff add/remove, attacks, casts, aggro, map load are all game-thread paths). No new locks.
- **Lifecycle:** on logout, `LogoutEvent` sets `State = NotLoggedIn` and removes the player from the map (`Goose/Events/LogoutEvent.cs:77,55-58`) BEFORE stripping buffs (:79-90), so the Task 3 `State == Ready` gate suppresses any invis-transition broadcast at logout. No CHP on logout is the expected behavior — do not write a test expecting one.
- **Failure behavior:** un-migrated DB (missing `see_invisible`) → `NPCHandler` throws at load. Intended (schema gate), documented in Persistence strategy.
- **`GameWorld.Send` drops sends for pets** (`Goose/GameWorld.cs:633`) — the bystander-capture tests must use real Players, not Pets, as receivers.
- **Test-helper reality:** map sizing, class injection (reflection), `SpawnNPC`, socket-capture recipes are all taken from existing passing tests cited in the API list; `DoneLoadingMapEvent` must be driven through `world.EventHandler` — the working `AddEvent(ev)` + `Update(world)` pattern is `Goose.Tests/LoginEventNameLengthTests.cs:55,71` and `Goose.Tests/Fixtures/GlobalScriptFixture.cs:201-209` (`EventHandlerTests.cs` only covers command registration).
