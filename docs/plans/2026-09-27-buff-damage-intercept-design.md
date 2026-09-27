# Buff damage interception + Shard of Invincibility fix

Date: 2026-09-27
Branch: `feat/buff-damage-intercept`

## Problem

Commit `4eb82ad` ("let crit overflow past 100% and soft-cap damage reduction") replaced
`damage *= (1 - DamageReduction)` with `damage *= 1 - Utils.EffectiveDamageReduction(...)`,
which caps effective reduction at 75% for a 100% stat (soft cap 0.5). The Shard of
Invincibility — item 471, OneTime, casting spell effect 192 (60s buff, `damage_reduce=1`) —
no longer makes the player invulnerable; they take 25% damage.

The soft cap stays (it is the intended general rule). The shard is fixed by giving buff
spell effects a way to intercept damage via their script.

## Chosen approach

- The shard's effect **drops the `damage_reduce` stat entirely**; the script is the single
  source of truth (approved: option A). A missing script fails loudly at server start
  (script compile), not silently in combat.
- A new `InterceptDamage` hook on the spell-effect script interface lets each buff on the
  target rewrite the incoming damage value.
- The hook runs **after PvP scaling, before the miss/dodge/damage logic** (approved
  rearrangement): the script sees the post-PvP value as `rawDamage`, and its output still
  flows through dodge and the sign/zero handling. A script returning 0 lands in the
  existing `damage == 0` → miss-text path, reproducing the pre-break shard behaviour.
- The hook returns `long` (not nullable, not double): the base class returns
  `currentDamage` (pass-through), the pipeline assigns unconditionally, and scripts do
  fractional math internally with an explicit cast at the return (approved).

## 1. Core mechanism

### Interface + base

`Goose/Scripting/ISpellEffectScript.cs` gains:

```csharp
long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world);
```

`Goose/Scripting/BaseSpellEffectScript.cs` implements it returning `currentDamage`.
The interface must carry the method because `Script<ISpellEffectScript>` hands the
pipeline the interface type.

Semantics: `rawDamage` is the value that would land if no buff touched it (post-PvP
scaling); `currentDamage` is the value accumulated through earlier buffs in the chain.
The return value becomes the new `currentDamage` for subsequent buffs and, if last, the
damage actually applied. Returning a negative value turns damage into a heal (the sign
branch in `Attacked` handles it); the default pass-through leaves heals untouched.

### Pipeline helper

New file `Goose/DamageIntercept.cs`:

```csharp
namespace Goose
{
    internal static class DamageIntercept
    {
        public static long Apply(ICharacter target, ICharacter attacker, long damage, GameWorld world)
        {
            long raw = damage;
            foreach (Buff buff in target.Buffs)
            {
                Script<ISpellEffectScript>? script = buff.SpellEffect?.Script;
                if (script is null) continue;
                try
                {
                    damage = script.Object.InterceptDamage(buff, attacker, raw, damage, world);
                }
                catch (Exception e)
                {
                    log.Error(e, "SpellEffect InterceptDamage {0} ({1}) target {2} ({3}) Exception",
                        buff.SpellEffect.Name, buff.SpellEffect.ID, target.Name, target.LoginID);
                }
            }
            return damage;
        }
    }
}
```

- Buffs run in `Buffs` list order; each script's output feeds the next.
- Per-buff try/catch, same shape as the existing `OnBuffAdded`/`OnBuffTick` call sites:
  a throwing script is logged and skipped; damage passes through unmodified; subsequent
  scripts still run.

### `ICharacter.Buffs`

`Goose/ICharacter.cs` gains `List<Buff> Buffs { get; }`. `Player` and `NPC` already
declare `List<Buff> Buffs { get; set; }` and satisfy it; `Pet : Player` inherits.
Only `NPC` and `Player` implement `ICharacter` directly — no other implementors to touch.

### `Attacked` call sites

New order in all three implementations:

```
state/alive check
pvp scaling (damage > 0 && attacker is Player)     ← moved to top
damage = DamageIntercept.Apply(this, character, damage, world)   ← new
damage == 0 / GM / damage <= 0 → miss text
dodge roll
damage > 0 → damage text, else heal text
apply to HP
```

- `Player.Attacked` (Player.cs:1964): the PvP 1/3 line moves from inside the
  `damage > 0` branch to the top, gated on `damage > 0` so heals are not scaled;
  pipeline next; old line deleted.
- `Pet.Attacked` (Pet.cs:763): same shape with its 1/2 rule.
- `NPC.Attacked` (NPC.cs:1073): no PvP scaling; pipeline after the `CanBeKilled`
  early-return, before the `damage <= 0` miss check. The NPC template script's
  `OnAttackedEvent` stays first and keeps observing the pre-buff value (deliberate:
  do not change what existing NPC scripts see).

## 2. The shard

### Script

New file `Goose/Data/Illutia/Scripts/Spell/ShardOfInvincibility.csx` (ships with the
build via the existing `Data/**` copy):

```csharp
using Goose;
using Goose.Scripting;

public class ShardOfInvincibility : BaseSpellEffectScript
{
    public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
    {
        return currentDamage > 0 ? 0 : currentDamage;
    }

    public override IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        yield return "Reduces all damage taken to 0.";
    }
}

return typeof(ShardOfInvincibility);
```

- Only positive damage is zeroed; heals (negative) pass through — invincibility means
  "takes no damage", not "cannot be healed".
- `GetItemDescription` overrides the built-in stat-based description in the item preview
  packets and the spell info window (without it, dropping `damage_reduce` leaves the
  preview with no lines). Wording is a placeholder, editable later.

### Sheet edit (gsheets.py helper, dry-run first)

Spell Effects row 192:

| column | new value |
| --- | --- |
| `damage_reduce` | *(cleared)* |
| `script path` | `Scripts/Spell/ShardOfInvincibility.csx` |
| `oneffect text` | `You are invincible.` |

Verify the helper can clear a cell (`--set damage_reduce=""`); if not, that one cell
goes through raw `gws values update` with `valueInputOption=RAW`.

No item row changes (471 already references effect 192). Item flow verified:
`UseConsumable` → `effect.Cast` → `CastBuffSpell` → `AddBuff`; `SpellHandler` attaches
the `Script` to the effect at load, so the buff carries it.

### Going live

Deploy a build containing the csx → sheet edit → `/updatesql` (the script compiles when
the effect loads; `/reloadscripts` is for later csx tweaks). Client-visible result: hits
on a sharded player show the miss text, as before the break.

## 3. Testing

### Pipeline tests — `Goose.Tests/DamageInterceptTests.cs`

Fixture-based (`TestWorldFixture`, same pattern as `BuffNullGuardTests`):

- No buffs / buff without script / buff with base script → damage unchanged
- One modifying script → its value returned
- Two scripts → second receives first's output as `currentDamage`, original as `rawDamage`
- Throwing script → logged, damage unchanged, subsequent scripts still run
- Negative (heal) value passes through unmodified by default

### Integration tests — `Goose.IntegrationTests/ShardOfInvincibilityScriptTests.cs`

Backstab pattern: the real `ShardOfInvincibility.csx` linked into the test output via
csproj `<Content ... CopyToOutputDirectory>`.

- Player with the shard buff takes `Attacked(attacker, 100)` → HP unchanged, miss text in
  sent packets
- Same player without the buff → takes the damage (control)
- PvP ordering: player attacker on a PvP map, victim carries a recording script → the
  `rawDamage` observed equals the post-1/3-scaled value
- Heal through the shard buff (`Attacked` with negative) → HP increases
- NPC with the shard buff → HP unchanged, miss text (generic mechanism on NPC targets)
- `effect.GetItemDescription` returns the custom line
- One end-to-end test through `Player.Attack` (full melee path)

Tests set Dexterity low so the dodge roll cannot interfere (Backstab tests use -1).

## Approved non-issues / deferred gaps

- **Double-sharding**: two shard buffs can coexist (no `buff_doesnt_stack_over`); both
  scripts return 0 — harmless. Existing stacking behaviour, unchanged.
- **Item buffs in scope**: an equipped item with a scripted spell effect would also
  intercept. Feature of the generic mechanism; no gate.
- **Stunned/not-Ready targets**: pipeline sits after the existing state checks; a stunned
  player skips damage processing entirely, as today.
- **`OnAttackedEvent`** (NPC template scripts) sees the pre-buff damage value.
- **Heals flow through the pipeline** (negative damage); scripts may modify them, base
  default does not.
- **Mechanism is generic**: NPCs/pets with scripted buffs get interception too; nothing
  uses it today beyond the shard.

## Task breakdown (single plan, ~5 tasks)

1. Core mechanism: `ICharacter.Buffs`, `InterceptDamage` on interface + base,
   `DamageIntercept.Apply` helper
2. Rearrange the three `Attacked` call sites (Player, Pet, NPC)
3. Shard csx script + sheet edit (row 192)
4. Pipeline tests (`Goose.Tests/DamageInterceptTests.cs`)
5. Integration tests (`Goose.IntegrationTests/ShardOfInvincibilityScriptTests.cs`)
