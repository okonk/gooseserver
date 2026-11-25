# Pet Menu — Data Rows

These rows are created through the maintainer's normal data workflow (game data spreadsheet); there is no migration or code change for them.

## Item templates

| Name | `item_usetype` | `script_path` | `script_params` | Stacking |
| --- | --- | --- | --- | --- |
| Pet Name Tag | `1` (OneTime) | `Scripts/Item/PetNameTag.csx` | `""` | stackable (`stack_size` > 1) |
| Pet Slot | `1` (OneTime) | `Scripts/Item/PetSlotItem.csx` | `"1"` | stackable (`stack_size` > 1) |

`script_params` on Pet Slot is the number of pet slots the item adds.

## Spell

- Spell effect **Pet Menu**: `effect_type = 23` (`SpellEffect.EffectTypes.PetMenu`).
- Spell **Pet Menu**: `spell_target = 1` (Self), pointing at that effect id. Cost/aether and class restrictions / learn scroll are up to the maintainer.

## Settings

Already in `GooseSettings.json` — no new settings needed:

- `PetSlotsMax` (default 30) — cap on total pet slots.
- `ActivePetLimit` (default 3) — max pets active at once.
