# Title and surname selection design

Date: 2026-10-02
Branch: `feat/title-surname-selection`

## Purpose

Players currently display whatever title and surname was last written to them, and have no
way to see or choose between the ones they have earned. This adds a per-player collection
of unlocked titles and surnames, a `/title` and `/surname` picker window, and a GM command
to unlock one permanently.

## Current behavior

- `Player.Title` and `Player.Surname` are plain strings persisted in their own columns
  (`player_title`, `player_surname`) — `Goose/Player.cs:110-114`. They are rendered into the
  `MKC` packet (`Goose/Packets.cs:128-129`), so they are how other players see a name.
- Four places assign them: `SetTitleCommand`, `SetSurnameCommand` (GM), `QuestWindow.GiveRewards`
  (`Goose/Quests/QuestWindow.cs:399-406`), and `Player.CreateNew` from
  `Settings.StartingTitle`/`StartingSurname` (`Goose/Player.cs:604-605`).
- The three commands each copy-paste the same refresh block: `SNF` to the target, then
  `ERC`+`MKC` to everyone in range plus a group buff snapshot.
- That block never reaches the target themself: `Map.GetPlayersInRange` excludes the character
  it is given (`Goose/Map.cs:155-163`), and `SNF` carries no title field
  (`Goose/Packets.cs:415-442`). So the player whose title changed does not see it change.
- Quest rewards assign the field without any refresh at all, so a rewarded title is invisible
  to everyone until the player reloads their character.
- `OptionListWindow` already provides a paged, clickable list
  (`Goose/OptionListWindow.cs`, 8 lines per page via `Window.LineClickCount`), used by
  `/quests`, `/questabandon` and `/recipes`.
- `PropertiesDictionary` stores arbitrary per-player JSON in `players.player_properties`.
  Its converter materializes JSON arrays as `List<object?>`
  (`Goose/PropertiesDictionaryJsonConverter.cs:89-105`), so `GetProperty<List<string>>` throws
  `InvalidCastException` on any list that has survived a save/load round-trip.

## Design

### Storage

Two keys in the existing `player_properties` JSON blob:

- `titles` — array of strings
- `surnames` — array of strings

No schema change, no new columns. Persistence rides the existing `Properties` serialization in
`Player.SaveToDatabase` (`Goose/Player.cs:935`), so a title change is flushed by the normal
`PlayerSaveEvent`/logout cycle rather than by an extra write per pick.

- **No cap** on list size. Paging handles the display.
- **No backfill.** A player's current `Title`/`Surname` is not seeded into the list at load or
  at character creation. It enters the list only when something sets it through the new grant
  path. A veteran player therefore starts with an empty collection.

### PropertiesDictionary list support

`ConvertValue<T>` (`Goose/PropertiesDictionary.cs:74-121`) gains a collection branch: when the
target type is `List<E>`, `IList<E>`, `ICollection<E>`, `IReadOnlyList<E>`, `IEnumerable<E>` or
`E[]`, and the stored value is `IEnumerable`, materialize the target by running each element
through `ConvertValue<E>`.

- The `value is string` test must stay ahead of the new branch: `string` is `IEnumerable<char>`,
  and reading a string property as a list of characters would otherwise succeed.
- Element conversion recurses through the existing rules, so `List<long>` narrows to `List<int>`
  and nested dictionaries keep working.
- `HashSet<T>` and dictionary targets are not covered and keep throwing `InvalidCastException`.
- The returned collection is a copy. Callers must assign a new list back to the key, which is
  already the convention stated on `PropertiesDictionary.Clone()` — values are replaced, never
  mutated in place. One comment line records this on the new branch.

This makes `player.Properties.GetProperty<List<string>>("titles", new List<string>())` correct
whether the value was written this session or loaded from JSON.

### Player API

Additions to `Goose/Player.cs`. No new types.

| Member | Behaviour |
| --- | --- |
| `TitlesProperty`, `SurnamesProperty` consts | `"titles"`, `"surnames"` |
| `UnlockedTitles()`, `UnlockedSurnames()` | the stored `List<string>`; empty when the key is absent |
| `GrantTitle(string, GameWorld)`, `GrantSurname(string, GameWorld)` | trim; skip if blank; skip if an entry already matches under `OrdinalIgnoreCase`; assign a new list; then equip |
| `SetTitle(string, GameWorld)`, `SetSurname(string, GameWorld)` | assign the field, then refresh the display. Does not touch the list — this is the temporary path |
| `RefreshCharacterDisplay(GameWorld)` (private) | `ERC`+`MKC` to self, `ERC`+`MKC` to each player in range, then the group buff snapshot |

Equip and grant are separate because only one of them is a permanent unlock. The picker window
equips; quest rewards and `/granttitle` grant.

The stored string keeps the casing of the first grant. Insertion order is what is stored;
alphabetical order is a display concern only.

`RefreshCharacterDisplay` is guarded by both `player.State != Player.States.NotLoggedIn` and
`player.Map is not null`. `Map` is declared non-nullable but is null for a player loaded from the
database (`Goose/Player.cs:143`), and without the state check an offline grant would push packets
at a closed socket. A player receiving their own `MKC` is already precedented —
`DoneLoadingMapEvent` does exactly that at map load (`Goose/Events/DoneLoadingMapEvent.cs:44`).

### Commands

| Command | Privilege | Effect |
| --- | --- | --- |
| `/title`, `/surname` | none, `Section = "General"` | opens the picker window |
| `/settitle <name> <text>`, `/setsurname <name> <text>` | `SetTitle` / `SetSurname` | equip only; no longer adds to the list |
| `/granttitle <name> <text>`, `/grantsurname <name> <text>` | `SetTitle` / `SetSurname` (reused) | add to the list and equip |

`SetTitleCommand` and `SetSurnameCommand` keep their `GetPlayerFromData` lookup, their `SNF`
send, their success and "Couldn't find player." messages, and their offline `SaveToDatabase`
branch; only the assignment line changes. `GrantTitleCommand` and `GrantSurnameCommand` are new
files in `Goose/Commands/` with the same shape.

All four GM commands bind `(string name, string[] text)` so the value keeps the rest of the
line, matching the existing commands' `Split(' ', 3)` behaviour — a bare `string` parameter
would truncate a multi-word title at the first space.

`QuestWindow.GiveRewards`' `RewardType.Title` and `RewardType.Surname` cases call
`GrantTitle`/`GrantSurname` instead of assigning the field. That both unlocks the reward and
fixes the missing broadcast.

An empty argument to any of them equips `""` (clears the display) and adds nothing to the list.

### Picker window

`/title` opens a window titled `Titles`; `/surname` opens `Surnames`. Both first close any stale
player-owned option list (`w.Type == OptionList && w.NPC is null`), the same way
`QuestAbandonCommand` does, so repeated use does not stack windows. No `OpeningLine` header.

Lines are built as:

1. `UnlockedTitles()` sorted case-insensitively alphabetically,
2. `" (current)"` appended to the entry that matches the equipped value under
   `OrdinalIgnoreCase` — appended after sorting, so the marker never changes the order,
3. `Clear` as the final line.

`OptionListWindow` pages the result at 8 lines per page, putting `Clear` at the bottom of the
last page. A player with nothing unlocked gets a window containing only `Clear`; the command
always opens the window rather than printing a chat message, so there is one code path.

Clicking a title line equips that exact stored string via `SetTitle`; clicking `Clear` equips
`""`. The window closes itself before the callback runs and is not reopened, matching
`/questabandon`. Choosing from the window never adds or removes list entries — `Clear` clears
the display and keeps everything unlocked.

A title set temporarily with `/settitle` does not appear in the window at all, since the window
shows only what is stored. Picking a line replaces it.

## Deferred / out of scope

- **No revocation.** Nothing removes an entry from a player's list. A title granted by mistake
  stays in that player's window permanently.
- **No comma sanitization.** `MKC` is comma-delimited, so a title containing `,` corrupts the
  packet. Nothing in the codebase sanitizes this today for names or titles; quest reward strings
  are editor data and `/granttitle` text is typed by a GM. Left alone here.
- **No backfill of existing titles**, as decided above.
- **No argument form for `/title`** — it always opens the window.
- **No client work.** The window is entirely server-driven.
- **No data or script changes.** `GrantTitle`/`SetTitle` are public so quest and item scripts can
  use them later; nothing under `Goose/Data` changes in this design.
- **Stale window index.** A window opened before a GM grant indexes the list captured at open
  time. `OptionListWindow.LineClicked` bounds-checks against that captured list
  (`Goose/OptionListWindow.cs:83-85`), so it cannot throw; it equips the entry the player saw.

## Testing

- `Goose.Tests/PropertiesDictionaryTests.cs` (extend) — `List<string>` survives a `JsonHelper`
  round-trip and reads back via `GetProperty<List<string>>`; a `List<object?>` of strings reads
  as `List<string>`; `List<long>` narrows to `List<int>`; the empty list; and `GetProperty<string>`
  on `"abyss"` still returns the string rather than a character sequence.
- `Goose.Tests/TitleCollectionTests.cs` (new) — grant rules on a bare `Player`: trim, blank
  ignored, case-insensitive duplicate ignored, first casing kept, insertion order in storage,
  `SetTitle` not adding, `GrantTitle` adding and equipping.
- `Goose.Tests/TitleCommandTests.cs`, `SurnameCommandTests.cs` (new) — using
  `TestWorldFixture.RunCommand` and the `CapturingPlayer` pattern from
  `Goose.Tests/QuestOptionListTests.cs:11-16`: alphabetical lines, `" (current)"` only on the
  matching entry, `Clear` last, a line click equips and emits `ERC`+`MKC`, the `Clear` line
  equips `""`, a second `/title` does not stack windows, paging past 8 entries.
- `Goose.Tests/GrantTitleCommandTests.cs` (new) — `/settitle` equips without touching the list;
  `/granttitle` adds and equips; both still report "Couldn't find player."; an offline target is
  saved rather than broadcast.
- Broadcast coverage in the style of `Goose.Tests/PartyMemberBuffVisibilityTests.cs:316` — the
  acting player themself receives `MKC` (the behaviour the old commands lacked), and a
  quest-completion test that a rewarded title lands in the list and reaches nearby players.
- `Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs` (extend) — grant a title,
  `RunInsert`, reload, read `GetProperty<List<string>>("titles")`. This is the test that pins the
  `List<object?>` trap.

## Scope

One plan, ~6 tasks:

1. `ConvertValue<T>` collection branch + `PropertiesDictionaryTests` additions.
2. `Player` unlock/grant/equip/refresh API + `TitleCollectionTests`.
3. `/title` and `/surname` picker commands + their tests.
4. `/granttitle`, `/grantsurname`, and the `/settitle`/`/setsurname` switch to equip-only + tests.
5. Quest reward wiring + broadcast tests.
6. Integration persistence test.
