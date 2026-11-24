# Pet Menu, Name Tags and Pet Slots — Design

## Purpose

Pets are managed today through slash commands (`/petlist`, `/petspawn`, `/petdelete`,
`/petvita`, `/petdamage`) and targeted mode spells. This adds:

- A **pet menu** built from option list windows: list pets, then per-pet details, spawn/despawn,
  mode, buy HP / damage, and release with confirmation.
- A **Pet Name Tag** item that renames a pet, using a new generic **text input window**.
- **Per-player pet slots** stored in player properties, raised by a **Pet Slot** item.
- An **active pet limit** (pets spawned at once), default 3.

## Rules

### Settings (`GooseSettings.cs` / `GooseSettings.json`)

| Setting | Default | Meaning |
|---|---|---|
| `PetCountLimit` (existing) | 5 | Base pet slots |
| `PetSlotsMax` (new) | 30 | Ceiling on **total** slots (base + bonus) |
| `ActivePetLimit` (new) | 3 | Max pets spawned at once per player |

### Pet slots

- `player.Properties["pet_slots_bonus"]` (int, default 0). Persisted via `player_properties`; no
  schema change.
- `Player.MaxPets => Math.Min(PetCountLimit + bonus, PetSlotsMax)`.
- Tame is refused when `Pets.Count >= MaxPets` (today `==`); the message points to `/pets`.
- Lowering any limit never removes pets; it only blocks taming/spawning beyond it.

### Active limit

- Enforced only in `PetOperations.TrySpawn`. Admin/script paths that call `pet.Spawn` directly
  bypass it.
- Players already over the limit at deploy keep their spawned pets; logout despawns all pets, so
  it self-corrects.

### Pet names

- Player names (unchanged): 3–16 ASCII letters (`LoginEvent.cs`). Extracted to
  `NameRules.IsValidPlayerName` and used by `LoginEvent`.
- Pet names: `NameRules.IsValidPetName` — trimmed, 3–16 chars, ASCII letters plus single spaces
  between words. No uniqueness check.
- Renames are logged via `LogHandler` (owner, pet id, old → new) using a fitting existing log
  type or a new `Log.Types.PetRename`.

## Server

### `PetOperations` (shared pet logic)

Static helpers returning a result (success + message); callers decide how to display it.
Existing commands become thin wrappers with unchanged syntax and messages.

| Operation | Rules |
|---|---|
| `TrySpawn(player, pet)` | map allows pets, not already out, cooldown elapsed, active count < `ActivePetLimit` → `pet.Spawn` |
| `TryDespawn(player, pet)` | must be out → `DestroyWithCooldown` (same 120s cooldown as the destroy spell) |
| `SetMode(pet, mode)` | must be out → clear target, set Neutral/Follow/Defend. Reused by the three mode spells |
| `BuyVita(pet, n)` / `BuyDamage(pet, n)` | existing rising-cost loop, spends pet exp; only at max level (class level exp == 0) |
| `MaxAffordableVita(pet)` / `MaxAffordableDamage(pet)` | simulate the same loop without spending |
| `Release(player, pet)` | `Destroy`, remove from `Pets`, `Delete = true`, save |
| `Rename(pet, name, world)` | validate, set `Name`, save; if out, send `EraseCharacter` then `MakePetCharacter` to players in range (**packets only — never `Destroy`/`Spawn`**, which would reset HP/mode/target/buffs); refresh any open `PetInfo` window for the pet |

Actions that don't apply to the pet's current state (spawn while out, mode while not out, etc.)
do nothing and send an Info notice.

### Pet menu (`Goose/PetMenu.cs`)

Opened by `/pets` (Help entry in the Pets section) and by a new spell effect type
`EffectTypes.PetMenu = 23` (`Revive` already holds 22) (self-cast, no target). Opening closes the player's existing non-NPC
option list windows (same pattern as `/titles`, `/quests`).

**Root window** — title `Pets (<count>/<MaxPets>)`, one line per pet, paged by
`OptionListWindow`:

```
Fluffy        Lv 30   [Out - Follow]
Grimtooth     Lv 22   [Cooldown 45s]
Sir Hops      Lv 12
```

Attack mode shows as `[Out - Attacking]`. No pets → single line "You have no pets. Tame one!".
No line icons.

**Pet window** — title is the pet name, Back returns to root, exactly 8 lines:

1. Details → existing `PetInfo` window (as `/petinfo`)
2. Spawn / Despawn (label follows state)
3. Mode: Neutral
4. Mode: Follow
5. Mode: Defend (current mode marked with `*`)
6. Buy HP (next: <cost> exp)
7. Buy Damage (next: <cost> exp)
8. Release

After an action the pet window is reopened with fresh state.

**Buy HP / Damage** — not max level or can't afford one buy → notice. Otherwise `AmountWindow`
from 1 to max affordable (default 1); confirm → buy → notice "Bought X hp for Y exp." → reopen
pet window.

**Release** — confirmation option list `Release <name>?` with lines "Yes, release <name>
forever" / "No, keep <name>". Yes → release → root. No/Back → pet window.

**Stale state** — callbacks capture `PetID` and re-resolve on every click; a missing pet sends a
notice and reopens the root.

Attack stays a targeted spell (it needs a map target). The mode spells stay as hotbar shortcuts.

### `TextInputWindow` (generic)

```csharp
new TextInputWindow(player, world, title, prompt, buttonText, defaultText, maxLength,
    onConfirm: (text, player, world) => ...);
```

- New `WindowFrames.TextInput = 35`, `WindowTypes.TextInput`.
- `Submit(text)`: trim, strip control characters, drop if empty or longer than `maxLength`, then
  call `onConfirm`. The window stays open until the caller closes it, so invalid input can be
  retried. No cancel callback (close just closes).
- Server → client: `TXI<windowId>|<maxLength>|<buttonText>|<defaultText>|<prompt>` (prompt
  last so it may contain `|`; `|` stripped from button/default text).
- Client → server: `TXT<windowId>,<text>` (text is the raw tail after the first comma).
  New `TextInputSubmitEvent`, registered beside `AMT` in `EventHandler`.

### `Inventory.TryConsumeItem(Item item, long count, GameWorld world)`

Reusable deferred-consume helper:

1. If the exact `item` is still in an inventory slot, remove `count` from that stack.
2. Otherwise remove from the first inventory stack with the same template id.
3. Otherwise return `false` and change nothing.

### Item scripts (`Goose/Data/Illutia/Scripts/Item/`)

**`PetSlotItem.csx`** — `ScriptParams` = slots to add (default 1). At `PetSlotsMax` → notice
"You already have the maximum number of pet slots (<max>)." and not consumed. Otherwise raise
`pet_slots_bonus` (clamped so total ≤ max), notice "You can now keep up to <MaxPets> pets.",
consumed.

**`PetNameTag.csx`** —

1. Use returns `false` (not consumed yet). No pets → notice. Otherwise option list
   `Name Tag — choose a pet` with the root menu's pet lines.
2. Click → `TextInputWindow` (prompt "New name for <name>:", button "Rename", default = current
   name, max 16).
3. Confirm → pet missing → notice. Invalid name → notice, window stays open.
   `TryConsumeItem(item, 1)` false → notice "Your name tag is gone.", close.
4. Otherwise `PetOperations.Rename`, close, notice "<old> is now known as <new>."

## Client (`Goose2ClientGodot`)

- `TextInputInfoPacket` (`TXI`).
- `TextInputWindow.tscn` / `.cs` modelled on `AmountWindow`: prompt label, `LineEdit` with
  `MaxLength`, confirm button labelled `buttonText`, close button. Enter submits `TXT`.
- Register in `WindowFrames` and the default window layout.
- Focus: while the `LineEdit` is focused, typing must not trigger movement/hotkeys/chat — follow
  whatever `AmountInput` does.

## Data rows (added by the maintainer, not migrations)

The plan lists exact column values for:

- Item template: Pet Name Tag (script `Item/PetNameTag.csx`).
- Item template: Pet Slot (script `Item/PetSlotItem.csx`, `ScriptParams` `1`).
- Spell + spell effect: Pet Menu (effect type `PetMenu` = 23, self-cast).

## Testing

**Unit (`Goose.Tests`)**

- `MaxPets` base / bonus / clamp; tame refused at and above `MaxPets`.
- `TrySpawn` refusals (active limit, cooldown, already out, no-pet map) and success.
- `TryDespawn` sets cooldown; notice when not out. `SetMode` notice when not out; clears target.
- `BuyVita/Damage` vs `MaxAffordable*` agree; refused below max level.
- `Release` removes and flags delete.
- `IsValidPlayerName` / `IsValidPetName` edge cases; `LoginEvent` behaviour unchanged.
- `TextInputWindow.Submit` trim / empty / too long / control chars / stays open.
- `TextInputSubmitEvent` malformed packet, wrong window id, commas preserved.
- `TryConsumeItem` exact stack, fallback stack, none.
- `PetMenu` stale `PetID` → notice + root; pet window 8 lines with correct label and mode marker.
- Slot and name tag scripts consume only on success (scripts directly if loadable in tests,
  otherwise via a core helper the script calls).
- Existing pet command tests pass through the wrappers.

**Integration (`Goose.IntegrationTests`)**

- `pet_slots_bonus` save/load round-trip.
- Renamed pet name save/load round-trip.

**Client**

- `TextInputWindowSceneTests`: loads, `TXI` populates prompt/button/default/max length, Enter
  sends `TXT`.

## Deferred / to verify during planning

- Confirm the client's `AmountInput` focus handling to mirror for the text input.
- Confirm `EraseCharacter` + `MakePetCharacter` renders a renamed pet correctly on the client.
- Pick an existing log type for renames or add `Log.Types.PetRename`.
- Resolved: shipped `.csx` item scripts are compiled in `Goose.IntegrationTests` (see `HairDyePotionScriptTests`).
