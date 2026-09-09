# Backstab Spell Effect Design

## Goal

Add a reusable `Backstab.csx` spell-effect script. Backstab checks the tile directly in front of the caster, deals rank-scaled formula damage to an eligible occupant, gains a 1.5× damage bonus when the occupant faces the same direction as the caster, and always displays the attack and tile animations.

## Scope

- Add `Goose/Data/Illutia/Scripts/Spell/Backstab.csx`.
- Extract formula HP/MP calculation and damage modifiers into a reusable `SpellEffect` API.
- Refactor ordinary formula casts to use the extracted API without behavior changes.
- Add coverage to `Goose.IntegrationTests`.
- Do not change existing Backstab spell-effect data or generated SQL.
- Do not add a scripted item description.

## Architecture

`SpellEffect` will expose a formula-result helper for scripts. It will parse supplied HP and MP formulas and preserve the current formula-spell processing rules and ordering:

- spell critical strikes;
- caster spell-damage scaling;
- the existing PvP exception for damaging player targets;
- the server-wide HP damage modifier.

`CastFormulaSpell` will delegate its HP and MP calculation to this helper. Its damage application, status updates, and animations remain unchanged.

`Backstab.csx` will own its positional targeting and visual behavior. It will reuse `SpellEffect.CanCastSpell` for target eligibility and the extracted formula helper for damage processing.

## Cast Flow

The script receives the caster as both caster and nominal target. It ignores the nominal target for hit detection.

1. Derive one coordinate from the caster's position and facing: north for 1, east for 2, south for 3, and west for 4.
2. Broadcast `P.Attack(caster)` and `P.SpellTile(frontX, frontY, thisEffect.Animation, thisEffect.AnimationFile)` to the caster and nearby players.
3. Send those packets even when the coordinate is outside the map, the tile is empty, or its occupant is ineligible. This matches existing `LineFront` tile-animation behavior.
4. Read a positive multiplier from `thisEffect.ScriptParams` using invariant culture. Missing, non-numeric, zero, and negative values fall back to `2`.
5. Look up the occupant with `caster.Map.GetCharacterAt(frontX, frontY)`.
6. Treat an empty tile or a target rejected by `thisEffect.CanCastSpell(caster, occupant)` as a miss while returning success.
7. Evaluate `-multiplier * (%cstr + %cwdmg + %clevel)` through the shared formula helper.
8. If the occupant's facing equals the caster's facing, multiply the processed HP damage by 1.5.
9. Apply positive damage through `occupant.Attacked(caster, damage, world)`.

Caster facing values are assumed to be the valid runtime values 1–4.

## Error Handling

The existing `CastScriptSpell` exception boundary will log unexpected script failures and return false. Invalid multiplier configuration is not exceptional and silently uses `2`.

A miss returns true so using Backstab against an empty or rejected tile remains a completed cast. Animation packets are independent of whether damage is applied.

## Validation

All new tests will live in `Goose.IntegrationTests` and compile the canonical Backstab script rather than duplicating its body.

Coverage will verify:

- each facing resolves the correct front tile;
- the nominal self target is not treated as the victim;
- a configured multiplier replaces the original `2`;
- invalid and non-positive multipliers fall back to `2`;
- matching facings apply the 1.5× bonus;
- different facings do not apply the bonus;
- spell crit, spell damage, PvP handling, and the global damage modifier remain consistent with formula spells;
- empty, out-of-bounds, and ineligible tiles take no damage;
- caster attack and tile-effect packets are sent on hits and misses;
- existing formula spell behavior remains unchanged after the helper extraction.
