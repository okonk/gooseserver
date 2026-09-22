# Quest Icons Design

Date: 2026-09-22

## Summary

Add a generic, viewer-specific character icon protocol and use it to display quest indicators above NPCs. The server selects an icon from each player's quest state, while the Godot client renders the selected sheet and graphic as a world-space pixel-art sprite.

This work spans `illutiagooseserver` and `Goose2ClientGodot`.

## Protocol and generic API

Add a `CHI` packet:

```text
CHI<characterLoginId>,<sheetId>,<graphicId>
```

A positive sheet selects a static manifest graphic. `0,0` clears the icon. A later packet replaces the character's one active icon.

The server exposes packet creation plus a generic method that sends or clears an icon for one viewer and any `ICharacter`. Icon state is viewer-specific and is not stored on the target character. Producers must resend an icon after a character re-enters view.

The server sends an NPC's initial `CHI` after its `MKC`. The client ignores packets for unknown character IDs. This relies on server packet ordering rather than queueing early icon packets.

Old clients ignore `CHI`; new clients connected to an old server display no icons.

## Client rendering

`Goose2ClientGodot` adds a `CharacterIconPacket` parser and registers it with `MapManager`. `MapManager` resolves `(sheet, graphic)` through its existing `SpriteCache` and applies the texture to the matching character.

The icon is a `Sprite2D` child of the character, not a `WorldTextBridge` element. It therefore shares the character's world transform, integer scaling, and pixel-art filtering. It retains the source graphic's native dimensions.

The sprite is horizontally centered, with its bottom edge a small gap above the name label's top anchor. Its position is recalculated when character height or texture changes. It uses absolute world Z 20, above characters and ordinary objects but below foreground and roof layers. Character invisibility and character removal naturally hide or remove it.

Invalid or missing graphics clear the visible icon safely. Animation, tinting, multiple simultaneous icons, and size clamping are out of scope.

The existing `SpriteCache` already caches loaded sheets and `(sheet, graphic)` atlas textures. No additional client cache is needed.

## Quest-state resolution

A shared resolver computes an icon for one `(player, NPC)`:

1. Use the ready icon if any attached quest is started and all its requirements are met.
2. Otherwise use the available icon if any attached quest is inactive and passes existing class, level, experience, prerequisite, completion, and repeatability rules.
3. Otherwise clear the icon.

Ready has priority over available. Script requirements participate through `IQuestScript.IsMet`.

Ready means quest requirements are complete. Inventory space, spellbook space, and scripted reward gates do not suppress the ready indicator; the quest window explains those temporary completion blocks.

Eligibility and requirement checks move from window-specific code into shared quest logic so the window and icon resolver cannot disagree. Completed repeatable quests become available again when inactive.

Available and ready are separately configurable even though their initial graphics are identical.

## Refresh behavior

Quest state is recalculated only at event boundaries, not continuously. The server sends a resolved icon:

- after an NPC's `MKC` when it enters view;
- after accepting, completing, or abandoning a quest;
- after kill or talk progress changes;
- after inventory, equipment, gold, experience, level, or class changes that can affect requirements or availability.

A refresh scans only NPCs currently visible to that player. There is no server-side computed-state cache: invalidating one correctly is harder than recalculating, especially because scripted requirements can inspect arbitrary state.

A public refresh method lets scripts notify the system after custom state changes. The server cannot infer arbitrary dependencies read by `IQuestScript.IsMet`; a script depending on custom world or property state must request a refresh itself.

Last-packet suppression is deferred unless profiling shows meaningful redundant traffic.

## Configuration

Add four `GooseSettings` values with shipped defaults:

```json
"QuestAvailableIconSheet": 2276,
"QuestAvailableIconGraphic": 332038,
"QuestReadyIconSheet": 2276,
"QuestReadyIconGraphic": 332038
```

`0,0` disables an indicator. Negative IDs and a zero sheet paired with a nonzero graphic are invalid. A positive sheet may use graphic zero if present in the manifest.

The selected graphic exists in the client manifest as a 32x32 frame. Sheet 2276 is already included in `tools/SpriteBundle/sheets.json`, so no bundle configuration change is required. No database or spreadsheet migration is required.

## Testing

Server coverage includes:

- `CHI` formatting and clearing;
- ready priority over available;
- availability, started, completed, prerequisite, class, level, experience, and repeatability behavior;
- built-in and scripted requirement readiness;
- reward-space gates not suppressing ready;
- clear state when no quest qualifies;
- `MKC` before `CHI` when an NPC enters view;
- refreshes after quest and standard player-state mutations;
- valid, disabled, and invalid settings combinations.

Client coverage includes:

- packet parsing;
- create, replace, and clear behavior;
- safe unknown-character and missing-graphic handling;
- native-size centering and vertical layout;
- repositioning after character height changes;
- icon cleanup when a character is erased or replaced.

Run both repositories' full test suites. Manually verify the available, accepted, ready, and completed transitions in the Godot client.

## Deferred work

- Animated or tinted icons
- Multiple icons per character
- Persisted generic character icon state
- Automatic polling of arbitrary scripted requirements
- Last-sent packet suppression
