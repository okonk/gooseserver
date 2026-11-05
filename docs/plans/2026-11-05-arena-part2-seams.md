# Arena Part 2: Server Seams — Implementation Plan

**Goal:** Add the three generic server seams the arena scripts need: a player-death hook with a
map respawn override, an NPC-script damage-taken intercept, and a public melee path that takes
a script-supplied base damage.

**Architecture:** Each seam follows an existing pattern. The death hook copies the global
`OnNPCKilled` dispatch (`NPC.cs:1381-1396`) and the map hook's try/log style (`Map.cs:197-225`).
The damage intercept sits next to the buff intercept in `NPC.Attacked`. The melee path splits
`NPC.Attack` so the formula's base-damage term is a parameter, without changing behaviour for
normal mobs.
Design: `docs/plans/2026-11-05-arena-design.md` (Part 2 of 4).

**Tech Stack:** .NET 10, C#, xUnit.

**Conventions:** follow `AGENTS.md` — no new comments unless the "why" is non-obvious. Short
interface doc comments matching the neighbouring members in `IMapScript.cs` are fine for the
override contract.

---

## APIs verified

| API | Location |
|---|---|
| Player death branch: HP/MP reset → "was slain" → `WarpTo(BoundMap…)` → regen → buff strip | `Goose/Player.cs:2142-2166` |
| `Player.Attacked` is `virtual`; `Pet.Attacked` overrides | `Goose/Player.cs:2096`, `Goose/Pet.cs:813` |
| `WarpTo` to another map calls `this.Map.RemovePlayer` → map `OnPlayerLeft` | `Goose/Player.cs:1497`, `:1576`; `Goose/Map.cs:215-225` |
| `IGlobalScript` / `BaseGlobalScript` | `Goose/Scripting/IGlobalScript.cs`, `BaseGlobalScript.cs` |
| Global dispatch loop with per-script try/log | `Goose/NPC.cs:1381-1396` (`NotifyKilled`) |
| `ScriptHandler.GetGlobalScripts()` | `Goose/Scripting/ScriptHandler.cs:86` |
| `IMapScript` / `BaseMapScript` (virtual defaults) | `Goose/Scripting/IMapScript.cs`, `BaseMapScript.cs:57-125` |
| `INPCScript` / `BaseNPCScript` | `Goose/Scripting/INPCScript.cs`, `BaseNPCScript.cs:29` |
| Only the `Base*Script` classes implement the interfaces (no other implementers in repo or scripts) | `grep ": IMapScript\|: INPCScript\|: IGlobalScript"` |
| `NPC.Attacked`: `OnAttackedEvent` (raw) → player branch → `DamageIntercept.Apply` at `:1218` → `damage <= 0` miss path → `CurrentHP -= damage` | `Goose/NPC.cs:1188-1235` |
| `NPC.Script` is the template's script | `Goose/NPC.cs:380` |
| `NPC.Attack`: base = `Strength + WeaponDamage + Level + (Level − target.Level)`; crit; melee dmg; DR; absorb; flat AC; packets; `Attacked`; `OnMeleeHit`/`OnMeleeAttack` if same map; miss text; `LastAttackTime` | `Goose/NPC.cs:1582-1628` |
| `Utils.CritMultiplier` (always rolls once) and `Utils.EffectiveDamageReduction` are `internal` — scripts cannot call them | `Goose/Utils.cs:62-80` |
| Relog onto a restricted map: login adds the player through `DoneLoadingMapEvent` → `map.AddPlayer` → `OnPlayerEntered`; Halloween evicts this way | `Goose/Events/DoneLoadingMapEvent.cs:101`, `Goose/Map.cs:197-207`, `Scripts/Map/HauntedHouse.csx:18-26`, `Scripts/Global/Halloween/HalloweenCommon.csx:289-297` |
| Test helpers: `ScriptStub.For<T>(instance)`; NPC-kills-player harness with a bound map | `TestSupport/ScriptStub.cs:12`, `Goose.Tests/NpcAttackTargetLostTests.cs:16-97` |

**Design change:** design §6 ("relog onto a restricted map") needs **no server change**. The
arena map script evicts non-participants in `OnPlayerEntered`, which also runs on login. Part 3
owns that. Update the design doc's server-changes list in Task 4.

**Persistence:** none.

---

### Task 1: `OnPlayerDied` with a map respawn override

**Files:**
- Create: `Goose/RespawnLocation.cs`
- Modify: `Goose/Scripting/IGlobalScript.cs`, `BaseGlobalScript.cs`, `IMapScript.cs`,
  `BaseMapScript.cs`, `Goose/Player.cs:2142-2150`
- Test: `Goose.Tests/PlayerDiedHookTests.cs`

**Contract:**

```csharp
public readonly record struct RespawnLocation(Map Map, int X, int Y);

// IGlobalScript / BaseGlobalScript (default no-op)
void OnPlayerDied(Player player, ICharacter killer, GameWorld world);

// IMapScript / BaseMapScript (default null)
/// <summary>Called on the map the player died on, before the respawn warp. Return a location
/// to respawn there instead of the bind point, or null for the bind point.</summary>
RespawnLocation? OnPlayerDied(Map map, Player player, ICharacter killer, GameWorld world);
```

**Sequence in the death branch:**
1. Existing: HP/MP reset, "was slain" message to the map.
2. New: `RespawnLocation? respawn = NotifyDied(character, world)`, which runs:
   a. the **map** hook on `this.Map` first, with try/catch → `log.Error`, treating an exception as `null`;
   b. then **every global** script's `OnPlayerDied`, each in its own try/catch → `log.Error`.
3. Changed: `WarpTo(world, respawn?.Map ?? BoundMap, respawn?.X ?? BoundX, respawn?.Y ?? BoundY)`.
4. Existing: regen, vitals, buff strip.

The hooks run while `player.Map` is still the death map, so scripts can tell where the death happened.

**Mutation impact:**
- Source of truth changed: the respawn destination (previously always the bind point).
- Readers: `WarpTo`, which fires `OnPlayerLeft` on the death map and `OnPlayerEntered` on the
  destination once the client finishes loading.
- Derived state: none. `BoundMap` is not modified. The override applies to this death only.
- Invariants: with no scripts, behaviour is identical. A throwing hook never prevents the
  respawn. The bind point is never changed.

**Step 1: Red.** Tests, using the `NpcAttackTargetLostTests` harness (copy its setup: maps
registered in `MapHandler.Maps`, class seeded, `PlayerAt`, `NpcAt`):
- `Death_without_scripts_respawns_at_bind_point` (regression)
- `Map_override_respawns_at_override_location`: `map.Script = ScriptStub.For<IMapScript>(overrideScript)` returning `(lobby, 3, 4)` → `player.Map == lobby`, `(MapX, MapY) == (3, 4)`, `BoundMap` unchanged.
- `Map_hook_sees_the_death_map_and_killer`: records `map`, `player.Map` and `killer` inside the hook → all equal the death map and the NPC.
- `Global_hook_receives_player_and_killer`, with the recorder registered like `GlobalNpcKilledHookTests.cs:18-25`.
- `Throwing_map_hook_falls_back_to_bind_point` (adversarial)
- `Throwing_global_script_does_not_stop_others_or_the_respawn` (adversarial)
- `Non_lethal_hit_does_not_fire_hooks`

Run: `dotnet test Goose.Tests --filter FullyQualifiedName~PlayerDiedHook` → compile failure.

**Step 2: Green.** Implement. Re-run → PASS. Then `dotnet test Goose.sln --no-restore`, because
the interface change must not break shipped scripts compiled in tests.

**Step 3: Commit** `feat(scripting): OnPlayerDied hook with map respawn override`

---

### Task 2: `InterceptDamageTaken` on NPC scripts

**Files:**
- Modify: `Goose/Scripting/INPCScript.cs`, `BaseNPCScript.cs`, `Goose/NPC.cs:1218`
- Test: `Goose.Tests/NpcInterceptDamageTakenTests.cs`

**Contract:**

```csharp
// INPCScript / BaseNPCScript (default returns damage)
long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world);
```

Inserted directly after `damage = DamageIntercept.Apply(this, character, damage, world);`
(`NPC.cs:1218`), inside the `character is Player` branch:

```csharp
damage = this.InterceptDamageTakenByScript(character, damage, world);
```

The private helper wraps the call in try/catch → `log.Error(e, "NPC InterceptDamageTaken {0} ({1}) Exception", …)`
and returns the unchanged damage on exception. The result then flows into the existing
`damage <= 0` miss path and `CurrentHP -= damage`, so aggro, kill credit, experience and the
`OnNPCKilled` dispatch all see the intercepted value.

**Mutation impact:**
- Source of truth: the `damage` local that subtracts from `CurrentHP`.
- Readers after it: miss path (`:1220-1231`), `CurrentHP`, `BattleTextDamage`, aggro and
  damage-share bookkeeping further down, death and kill credit.
- Derived state: none new.
- Invariants: no script → unchanged. Buff intercepts apply first. A throwing script leaves the
  damage unchanged. Returning ≤ 0 shows a miss and keeps the NPC alive.

**Step 1: Red.** Tests (map, NPC and player from `TestWorldFixture`, script set via
`npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(...)` like `ScriptHookLoggingTests.cs:51`):
- `Script_halves_damage_taken` → NPC HP drops by half.
- `Script_sees_damage_after_buff_intercepts` (adversarial ordering): a buff whose effect script
  doubles damage, plus an NPC script recording its input → the recorded input is the doubled
  value. Use `CompileSpellEffectScript` (`TestWorldFixture.cs:41`) for the buff script.
- `Script_returning_zero_is_a_miss_and_npc_survives`
- `Throwing_script_leaves_damage_unchanged` (adversarial)
- `Lethal_intercepted_damage_kills_and_credits_killer`: the script raises damage to above
  HP → the global `OnNPCKilled` fires with the attacker.

**Step 2: Green.** Implement. Re-run → PASS.

**Step 3: Commit** `feat(scripting): NPC script InterceptDamageTaken hook`

---

### Task 3: Public melee path with a supplied base damage

**Files:**
- Modify: `Goose/NPC.cs:1582-1628`
- Test: `Goose.Tests/NpcMeleeDamageTests.cs`

**Contract (helper extraction):**

```csharp
public void Attack(ICharacter character, GameWorld world)
    => this.AttackWithBaseDamage(character,
           this.MaxStats.Strength + this.WeaponDamage + this.Level + (this.Level - character.Level), world);

public void AttackWithBaseDamage(ICharacter character, double baseDamage, GameWorld world);

public double MitigateMelee(ICharacter character, double damage, GameWorld world);
```

- `MitigateMelee` is **pure**: no RNG, no packets, no state. It applies, in the original order:
  `× (1 + MaxStats.MeleeDamage)`, `× (1 − EffectiveDamageReduction(target DR, softCap))`,
  `× absorb(target AC, ArmorPierce, ACMultiplier, MaxAC)`, `− target AC × ACMultiplier / 25`.
  It returns the raw double; callers decide what ≤ 0 means.
- `AttackWithBaseDamage` holds the rest of the old `Attack` body unchanged:
  `BreakInvisibility`, `damage = baseDamage × CritMultiplier(...)` (**the single RNG roll stays
  here, so the shared random sequence is unchanged**), `MitigateMelee`, the attack packet,
  `Attacked`/`OnMeleeHit`/`OnMeleeAttack` or the miss text, and `LastAttackTime`.
- The arena calls `AttackWithBaseDamage` for real hits and `MitigateMelee` for `/arena preview`
  (no crit, no side effects).

**Mutation impact:**
- No state changes. This is a pure refactor plus one new pure function.
- Invariants: `Attack` damage is bit-identical for the same seed; RNG consumption is
  unchanged (exactly one `CritMultiplier` roll per attack).

**Step 1: Red-before-refactor.** Write characterisation tests **against the current code first**
and confirm they pass before refactoring:
- `Attack_damage_matches_formula_without_crit`: NPC Str 100, WeaponDamage 5000, Level 50, target
  level 1, AC 500, `ACMultiplier` 1, DR 0.1, MaxAC 3500, MeleeCrit 0. Compute the expected HP loss
  by hand from the formula at `NPC.cs:1586-1598` and assert it exactly.
- `Attack_with_seeded_crit_matches_reference`: MeleeCrit 0.5 with a seeded `Random`
  (`new TestWorldFixture(configure, new Random(1234))`). Record HP loss over 20 attacks and assert
  it equals the sequence recorded from the pre-refactor run, pasted into the test.

Then add the new tests (red until implemented):
- `MitigateMelee_is_deterministic_and_matches_attack_without_crit`: equals the hand-computed value
  above. Calling it twice doesn't advance `world.Random` (compare `Next()` from two identically
  seeded worlds).
- `AttackWithBaseDamage_uses_supplied_base`: base 65,000 with the same target → HP loss equals
  `MitigateMelee(65000)`, truncated to long.
- `AttackWithBaseDamage_zero_or_negative_after_mitigation_is_a_miss` (adversarial): tiny base,
  huge AC → HP unchanged, miss text sent.

**Step 2: Green.** Refactor `Attack` and implement. All tests pass, including both
characterisation tests unchanged.

**Step 3: Commit** `refactor(npc): split melee attack so scripts can supply base damage`

---

### Task 4: Verification and design doc

1. `dotnet test Goose.sln --no-restore` → all pass.
2. `dotnet test Goose.IntegrationTests --no-restore` → all pass. Shipped scripts recompile
   against the changed interfaces.
3. Update `docs/plans/2026-11-05-arena-design.md`:
   - server change §5 names `AttackWithBaseDamage` / `MitigateMelee`;
   - §6 becomes "no server change: the arena map evicts non-participants in
     `OnPlayerEntered`, which also runs on login";
   - §3 states the hook order (map hook first, then globals, before the warp).
4. Commit `docs(arena): seams as built`.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| No scripts → death unchanged | `Death_without_scripts_respawns_at_bind_point` |
| Map override redirects respawn, bind point untouched | `Map_override_respawns_at_override_location` |
| Hooks see the death map | `Map_hook_sees_the_death_map_and_killer` |
| Broken scripts never block respawn | `Throwing_map_hook_falls_back_to_bind_point`, `Throwing_global_script_does_not_stop_others_or_the_respawn` |
| NPC script intercept runs after buffs | `Script_sees_damage_after_buff_intercepts` |
| Intercepted damage drives the kill | `Lethal_intercepted_damage_kills_and_credits_killer` |
| Melee refactor is behaviour-preserving, including RNG | `Attack_damage_matches_formula_without_crit`, `Attack_with_seeded_crit_matches_reference` |
| Preview mitigation has no side effects | `MitigateMelee_is_deterministic_and_matches_attack_without_crit` |
