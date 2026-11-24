# Pet Menu Part 4 — Pet Name Tag and Pet Slot Items Implementation Plan

**Goal:** Pet renaming (core operation + logged), a Pet Name Tag item that uses the text input
window, and a Pet Slot item that raises the player's pet slots.

**Architecture:** `PetOperations.Rename` owns validation, persistence, the live on-screen refresh
and logging. The two `.csx` item scripts are thin flows over Part 1–3 APIs, tested by compiling the
shipped script in `Goose.IntegrationTests` (pattern `HairDyePotionScriptTests`).

**Tech Stack:** C# / .NET 10, Roslyn-compiled `.csx` scripts, xUnit.

Design: `docs/plans/2026-11-24-pet-menu-design.md`. Part 4 of 4 — requires Parts 1–3.

---

## APIs verified

| API | Location |
|---|---|
| `Log.Types` (last player-facing `Stylist = 26`) | `Goose/Log.cs:9-54` |
| `LogEventRegistry` entries `new(id, name, group, otherIdKind)` | `Goose/Logs/LogEventRegistry.cs:9-48` |
| `LogEventDescriptorTests` InlineData per type | `Goose.Tests/LogEventDescriptorTests.cs:36` |
| `LogHandler.Log(Log.Types, Player, string text, int otherid = 0)`; tests read `LogHandler.Pending` | `Goose/LogHandler.cs:34`, `Goose.Tests/StylistWindowTests.cs:340` |
| `P.EraseCharacter(loginId)` / `P.MakePetCharacter(pet)` (comma-delimited, includes `Name`) | `Goose/Packets.cs:85`, `:250` |
| `Map.GetPlayersInRange(character)` | `Goose/Map.cs:167` |
| Spawn broadcast pattern (MKC to players in range) | `Goose/Pet.cs:494-499` |
| PetInfo windows (`Type == PetInfo && Data == pet`, `Refresh`) | `Goose/Commands/PetInfoCommand.cs:32-38` |
| `Pet.SaveToDatabase` persists `pet_name` | `Goose/Pet.cs:300-320` |
| `IItemScript.OnUseConsumableEvent` — return false keeps the item | `Goose/Scripting/IItemScript.cs`, `Goose/Inventory.cs:589-616` |
| `BankPageItem.csx` (ScriptParams int pattern) | `Goose/Data/Illutia/Scripts/Item/BankPageItem.csx` |
| Integration test script linking (`ItemScripts/*.csx`) | `Goose.IntegrationTests/Goose.IntegrationTests.csproj:63-66` |
| Shipped item script test pattern | `Goose.IntegrationTests/HairDyePotionScriptTests.cs:9-33` |
| Pet persistence test pattern (`pets.sql`, `LoadPets`) | `Goose.IntegrationTests/PetCooldownTests.cs:7-46` |
| Player property persistence test | `Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs:25-50` |
| Part 1: `NameRules.IsValidPetName`, `Inventory.TryConsumeItem`, `Player.PetSlotsBonus`, `Player.MaxPets`, `PetOperations.Find` | Part 1 plan |
| Part 2: `PetMenu.PetLine` | Part 2 plan |
| Part 3: `TextInputWindow` (does not self-close), `Window.Close` | Part 3 plan, `Goose/Window.cs:348` |
| Item columns: `item_usetype` (`OneTime = 1`), `script_path`, `script_params` | `Goose/ItemHandler.cs:71,129,135`, `Goose/ItemTemplate.cs:12-15` |
| Spell columns: `spell_target` (`Self = 1`), effect `effect_type` | `Goose/SpellHandler.cs:249`, `:54`; `Goose/Spell.cs:11-16` |

---

### Task 1: `Log.Types.PetRename`

**Files:** `Goose/Log.cs` (`PetRename = 27` after `Stylist = 26`), `Goose/Logs/LogEventRegistry.cs`
(`new(27, "PetRename", LogEventGroup.Social, LogOtherIdKind.Unused)`),
`Goose.Tests/LogEventDescriptorTests.cs` (new InlineData row).

Values persist in `logs.log_type` — append only.

`dotnet test Goose.Tests --filter LogEventDescriptorTests` → pass. Commit with Task 2.

---

### Task 2: `PetOperations.Rename`

**Mutation impact:**
- Source of truth: `pet.Name`; persisted `pets.pet_name` via `pet.SaveToDatabase`.
- Readers: `MakePetCharacter` (other clients' view of a spawned pet), PetInfo window line 1
  (`Goose/Window.cs:309`), `/petlist`, pet menu lines, `PlayerInfoWindow` pets page.
- Propagation sequence:
  1. `pet.Name = name`
  2. `pet.SaveToDatabase(world)`
  3. if `pet.IsAlive`: `erase = P.EraseCharacter(pet.LoginID)`, `make = P.MakePetCharacter(pet)`;
     for each `p in pet.Map.GetPlayersInRange(pet)`: `world.Send(p, erase); world.Send(p, make)`.
     **Never** `Destroy()`/`Spawn()`: those reset HP/mode/target/buffs and hit cooldown/active-limit rules.
  4. `Refresh` each `owner.Windows` entry with `Type == PetInfo && Data == pet`.
  5. `world.LogHandler.Log(Log.Types.PetRename, owner, $"pet {pet.PetID}: {old} -> {name}")`.
- Invariants: invalid name changes nothing (no save, no packets, no log); LoginID unchanged.

**Files:** `Goose/PetOperations.cs`; test `Goose.Tests/PetRenameTests.cs`.

```csharp
public static PetResult Rename(Player owner, Pet pet, string name, GameWorld world)
```

Invalid → `"Pet names must be 3-16 letters, with single spaces between words."`.
Success → `"{old} is now known as {name}."`.

**Tests:**
- Invalid name → unchanged, no log in `LogHandler.Pending`.
- **Adversarial live refresh:** spawned pet (`map.CanSpawnPets = true`, `TrySpawn`), set
  `pet.CurrentHP = 1`, `pet.Mode = Follow`; second player in range on the map
  (`fixture.CommandPlayerOn` + `map.AddPlayer`); rename → bystander received `ERC{loginId}` then an
  `MKC{loginId},13,{newName},` packet, and pet keeps `LoginID`, `CurrentHP == 1`, `Mode == Follow`.
  Fails if implemented as Destroy+Spawn.
- Open PetInfo window refreshed (owner received a window line containing `"Name: {new}"`).
- Log entry with `PetRename` type.

Integration (`Goose.IntegrationTests/PetCooldownTests.cs`, reuse `CreatePersistedPet`): rename, flush
(`Database.Execute(_ => { })`), `LoadPets` → reloaded name equals new name.

Commit `feat(pets): rename operation with live refresh and logging`.

---

### Task 3: `PetSlotItem.csx`

**Mutation impact:** `Player.PetSlotsBonus` (Part 1) → persisted in `player_properties` on next
player save; `MaxPets` reads it live. No derived state found.

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Item/PetSlotItem.csx`
- Modify: `Goose.IntegrationTests/Goose.IntegrationTests.csproj` (link as `ItemScripts/PetSlotItem.csx`)
- Test: `Goose.IntegrationTests/PetSlotItemScriptTests.cs`

Behaviour:
- `slots = string.IsNullOrWhiteSpace(item.ScriptParams) ? 1 : int.Parse`; unparseable or ≤ 0 →
  `ServerMessage("This item has no valid pet slots configured.")`, return false (as `BankPageItem`).
- `room = settings.PetSlotsMax - player.MaxPets(settings)`; `room <= 0` → Notice
  `$"You already have the maximum number of pet slots ({settings.PetSlotsMax})."`, return false.
- `player.PetSlotsBonus += Math.Min(slots, room)`; Notice
  `$"You can now keep up to {player.MaxPets(settings)} pets."`; return true.

**Tests** (via `player.Inventory.UseConsumable(item, world)`, pattern `HairDyePotionScriptTests.cs:58-66`):
- empty params → +1, item consumed.
- params `"5"` with 2 slots of room → bonus +2 only (clamped), total == `PetSlotsMax`.
- **Adversarial:** at max → item still in inventory, bonus unchanged.
- Persistence: covered by `PlayerPropertiesPersistenceTests` (int round-trip of an arbitrary key,
  `:25-35`); add one case using `Player.PetSlotsBonusKey`.

Commit `feat(pets): pet slot item`.

---

### Task 4: `PetNameTag.csx`

**Mutation impact:** item stack (via `TryConsumeItem`) and pet name (via `Rename`). Order:
validate name → `TryConsumeItem` → `Rename`. If consume fails nothing is renamed. `Rename` can only
fail on validation, which is already checked, so the consume-then-rename order cannot lose the tag
without a rename.

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Item/PetNameTag.csx`
- Modify: `Goose.IntegrationTests/Goose.IntegrationTests.csproj` (link `ItemScripts/PetNameTag.csx`)
- Test: `Goose.IntegrationTests/PetNameTagScriptTests.cs`

Flow:
1. `OnUseConsumableEvent` always returns **false**. No pets → Notice `"You have no pets to name."`.
2. Close non-NPC OptionList windows; `OptionListWindow` title `"Name Tag - choose a pet"`, lines
   `PetMenu.PetLine(pet)`, callbacks capture `PetID`.
3. Click → re-resolve pet (missing → notice, stop) → `new TextInputWindow(player, world, "Name Tag",
   $"New name for {pet.Name}:", "Rename", pet.Name, NameRules.MaxLength, onConfirm)`.
4. `onConfirm(text, p, w)`:
   - re-resolve pet; missing → notice, `window.Close`.
   - `!NameRules.IsValidPetName(text)` → Notice with the rules message; window stays open.
   - `!p.Inventory.TryConsumeItem(item, 1, w)` → Notice `"Your name tag is gone."`, `window.Close`.
   - `PetOperations.Rename(p, pet, text, w)` → Notice result, `window.Close`.

The `onConfirm` lambda needs the window reference to close it: declare
`TextInputWindow? input = null; input = new TextInputWindow(..., (t, p, w) => { ... input!.Close(p, w); });`
(the `RecipesCommand.cs:33-37` pattern for self-referencing windows).

**Tests:**
- Use → item still in inventory, chooser window open; click pet → `TextInputWindow` open with
  default = current name.
- Valid submit → pet renamed, tag stack −1, text window closed.
- Invalid submit → name unchanged, tag kept, text window still open.
- **Adversarial:** tag moved/dropped before confirm (remove the stack via `RemoveItem`) → no rename,
  notice `"Your name tag is gone."`.
- Fallback: original stack removed but another tag stack exists → that stack is used, rename happens.

Commit `feat(pets): pet name tag item`.

---

### Task 5: Data rows (for the maintainer; no migration)

Add `docs/plans/2026-11-24-pet-menu-data-rows.md` listing, for the maintainer's normal data workflow:

- Item template **Pet Name Tag**: `item_usetype = 1` (OneTime), `script_path = "Scripts/Item/PetNameTag.csx"`,
  `script_params = ""`, stackable.
- Item template **Pet Slot**: `item_usetype = 1`, `script_path = "Scripts/Item/PetSlotItem.csx"`,
  `script_params = "1"`.
- Spell effect **Pet Menu**: `effect_type = 23`. Spell **Pet Menu**: `spell_target = 1` (Self),
  that effect id, no cost/aether as desired; give to classes/learn scroll as desired.
- Settings already in `GooseSettings.json` (`PetSlotsMax`, `ActivePetLimit`).

Commit `docs(pets): data rows for pet menu items and spell`.

---

## Final check

`dotnet test Goose.sln` → all pass. Then the manual smoke from Part 3 Task 4 with both repos built.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Rename never respawns the pet | adversarial live refresh test |
| Invalid names change nothing | Rename invalid test; name tag invalid submit |
| Tag consumed only on successful rename | name tag valid/invalid/gone tests |
| Slot item never exceeds `PetSlotsMax` | clamp + at-max tests |
| Renamed name persists | `PetCooldownTests` rename round-trip |
