# Backpack Design

**Date:** 2026-10-29
**Status:** Approved
**Repos:** `illutiagooseserver` (server), `Goose2ClientGodot` (client)

## Summary

A new equip slot holds a backpack: extra, upgradeable, storage-only inventory space opened
with a hotkey (default **N**). Contents belong to the player, not the item. A backpack can
only leave the slot when empty, so contents can never change hands. Upgrades are repeatable
NPC quests with a scripted reward that grows the equipped backpack in place.

## Decisions

| Topic | Decision |
|-------|----------|
| Non-empty backpack leaving the slot | Refused. Unequip, swap and destroy-while-equipped all require an empty backpack. |
| Opening an unequipped backpack | Not supported. Using a backpack from inventory equips it (it is `UseType.Armor`) and opens the window. |
| Storage | `players.backpack` TEXT column holding the serialized `ItemContainer`. No new table, no container id. |
| Size | Per item: `ItemProperty.BackpackSlots` / `BackpackColumns`, falling back to template `properties` (`backpackSlots`, `backpackColumns`). |
| What items in the backpack can do | Storage only, like a bank. Must move to inventory to use, equip, sell, hotbar, combine, or count toward quest requirements. |
| Lore checks | `Inventory.HasItem(templateid)` includes the backpack, so every lore gate covers it. |
| Nested backpacks | Allowed. An empty backpack item stored inside the backpack is just an item. |
| Window shape | Server-chosen slots and columns per backpack/tier, no paging. |
| Upgrades | Repeatable NPC quests, `RewardType.Script` reward `BackpackUpgrade.csx`. |
| Tier sizes | Content data (e.g. 10, 15/20, ...). Not fixed in code. |
| Size display | Derived into `Item.Description`: template description + "Holds N items." |

## Server

### Data and equip rules

- `Inventory.EquipSlots.Backpack = 15`; `ItemTemplate.ItemSlots.Backpack = 14`, mapped in
  `Inventory.ItemSlotToEquipSlot`. `GooseSettings.EquippedSize` 14 → 15 (settings JSON and
  commented example). Old saves are padded by `Inventory.LoadSlots`.
- `ItemProperty.BackpackSlots`, `ItemProperty.BackpackColumns` on the item. Template base size
  in `ItemTemplate.Properties` (JSON `properties` column, same mechanism as `hideOtherSlots`),
  so no schema or editor change for templates.
- `Inventory.backpackContainer`: `ItemContainer(slots + 1)`, 1-based like the others.
- `players.backpack TEXT DEFAULT '' NOT NULL`, added via `GameWorld.AddColumnIfMissing`, read
  in player load, written in `Player.BuildUpdateQuery` / insert, so it's saved atomically with the
  rest of the player.
- All unequip paths go through `Inventory.UnequipCore`; it refuses with "Empty your backpack
  first." when the backpack slot is being vacated and the container holds items. An open
  backpack window closes when the backpack is unequipped.
- Equip resizes the container to the item's size (always empty at that point).
- Description is regenerated whenever the size is set (load, equip, upgrade).

### Load edge cases

- **Saved contents, saved backpack slot empty:** log every item at Error (template id, name,
  stack, item JSON) and clear the contents.
- **Saved contents, backpack discarded on load (missing template / null item):** keep the
  column contents untouched (not loaded into a usable container, not cleared) until a valid
  backpack is equipped. Protects against a template deletion or renumbering wiping everyone's
  backpack.
- **More saved items than the equipped backpack's size:** log and discard the excess (same as
  bank's "excess discarded").
- **Item in the backpack slot whose template slot is not Backpack:** log only.

### Window and protocol

- `WindowFrames.GridContainer = 32` (generic: a server-sized grid);
  `WindowTypes.Backpack` (meaning).
- `BackpackWindow : ItemContainerWindow`. Base `CanDeposit`/`CanWithdraw`: all contents are
  already the player's and can't change hands.
- `MKW`'s two trailing ints (currently always `0,0`) carry `slots,columns`.
- Slot packets reuse `P.GenericWindowSlot` / `P.ClearGenericWindowSlot`.
- New client→server packet `OBP` toggles: if a backpack window is open, close it
  (`Window.Close`), else open; with no backpack equipped, notice
  "You don't have a backpack equipped."
- Equipping a backpack from inventory opens the window.
- An upgrade while open resends `MKW` with the new size.

### Upgrade quest script

`Data/Illutia/Scripts/Quest/BackpackUpgrade.csx`, `script_params`
`{"from": 10, "to": 20, "columns": 5}` (`columns` optional). Params are parsed per call, never
cached (one script instance is shared by all reward rows, same as `ClassReward.csx`).

- `CanComplete`: block if no backpack is equipped ("You need to be wearing your backpack."),
  if current `BackpackSlots != from` ("This upgrade is for a 10-slot backpack."), or if params
  are malformed / `to <= from` (misconfigured message). Runs before requirements are taken.
- `GiveReward`: set `BackpackSlots`/`BackpackColumns` on the equipped item, grow the container
  in place, regenerate the description, resend the equip slot, resend an open backpack window,
  message "[Quest Reward]: Backpack upgraded to 20 slots."
- `GetRequiredInventorySpace`: 0.
- Content: an NPC with repeatable quests per tier. The `from` check enforces order;
  `PrerequisiteQuests` is optional.

## Client (Goose2ClientGodot)

- `WindowFrames.GridContainer = 32`; `MakeWindowPacket.Unknown1/2` read as slot count and columns
  for this frame.
- `GridContainerWindowManager : BaseMultipleWindowManager<GridContainerWindow>`, multi-instance
  per window id. Also routes generic slot / clear-slot packets by window id.
  `GridContainerWindow.OnMakeWindow` rebuilds the grid (clears, doesn't append) so a resize from
  an upgrade works.
- Drag in/out like `GenericContainerWindow.DropItem`.
- Equip slot index 14 at `(152, 34)` in `CharacterEquipmentLayout`.
- `ToggleBackpack` in `InputActionCatalog` (Windows category), default **N** (unbound today),
  sends `OBP`; appears in the key bindings window.

## Testing

- **Goose.Tests:** size fallback to template; equip resizes; unequip/swap/destroy of a non-empty
  backpack refused (adversarial: via each `UnequipCore` caller); `HasItem` sees backpack items
  and lore pickup is refused; `BackpackWindow` drags; `OBP` open/close/no-backpack.
- **Goose.IntegrationTests:** save/load round trip of `players.backpack`; column added to an old
  DB; orphaned contents logged and cleared; contents preserved when the backpack template is
  missing; old 14-slot equipped blobs load.
- **Script tests:** `BackpackUpgrade` blocks wrong tier / missing backpack / bad params; upgrades
  in place keeping contents.
- **Client:** `KeyBindingsSelfTest` entry; manual check of grid resize after upgrade.

## Out of scope

- Obtaining the first backpack (vendor, quest, drop): content only.
- Using items directly from the backpack, quest requirements counting backpack items.

## Known baseline issue

`Goose.IntegrationTests.LogSearchDeliveryTests.ValidationAndDbErrorsEmitPacedLrxOnly_NlogRetainsFullDbException`
fails on master (`a656268`) with "Collection was modified" at `LogSearchDeliveryTests.cs:293`.
Unrelated to this feature.
