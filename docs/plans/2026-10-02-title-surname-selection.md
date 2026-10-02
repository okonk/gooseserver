# Title and Surname Selection Implementation Plan

**Goal:** Let players pick which unlocked title/surname they display, from a collection the server fills when a title is granted by a GM or a quest reward.

**Architecture:** Two JSON keys (`titles`, `surnames`) in the existing `players.player_properties` blob, accessed through new `Player` methods that split *equip* (write the field, republish the character) from *grant* (append to the collection, then equip). `/title` and `/surname` render the collection in the existing `OptionListWindow`; `/settitle`/`/setsurname` become equip-only and new `/granttitle`/`/grantsurname` grant-and-equip.

**Tech Stack:** C# / .NET 10, xUnit, SQLite. Design: [2026-10-02-title-surname-selection-design.md](2026-10-02-title-surname-selection-design.md).

**Worktree:** all paths relative to `.worktrees/feat/title-surname-selection`.

---

## APIs verified

| API | Location | Note |
| --- | --- | --- |
| `PropertiesDictionary.ConvertValue<T>` | `Goose/PropertiesDictionary.cs:74-121` | private static; numeric + enum conversions only today |
| `GetProperty<T>(key, default)` | `Goose/PropertiesDictionary.cs:40-48` | returns default when key absent, else `ConvertValue<T>` |
| JSON array → `List<object?>` | `Goose/PropertiesDictionaryJsonConverter.cs:89-105` | the reason `GetProperty<List<string>>` throws today |
| `Clone()` replace-don't-mutate convention | `Goose/PropertiesDictionary.cs:136-146` | values are replaced, never mutated in place |
| `Player.Title` / `Player.Surname` | `Goose/Player.cs:110-114` | persisted in `player_title` / `player_surname` |
| `Player.Map` | `Goose/Player.cs:143` | declared non-nullable, null for a player loaded from the DB |
| `Player.Properties` | `Goose/Player.cs:475` | serialized on save at `Goose/Player.cs:935` |
| `Player.States` / `Player.State` | `Goose/Player.cs:61-68` | fresh `Player(0)` starts `NotLoggedIn` |
| `Map.GetPlayersInRange` | `Goose/Map.cs:155-163` | **excludes** the character passed in |
| `P.EraseCharacter` / `P.MakeCharacter` | `Goose/Packets.cs:83`, `:111` | `MKC` carries title at `:128-129` |
| `P.StatusInfo` | `Goose/Packets.cs:415-442` | carries no title field |
| `Group.SendBuffSnapshotIfVisible` | `Goose/Group.cs:121-130` | self-guards on group, state, map, range, GM invisibility |
| `OptionListWindow` ctor | `Goose/OptionListWindow.cs:10-26` | adds itself to `player.Windows` and sends |
| `OptionListWindow.LineClicked` | `Goose/OptionListWindow.cs:81-89` | bounds-checks the captured list, closes, then calls back |
| `Window.LineClickCount` | `Goose/Window.cs:62` | 8 lines per page |
| `Window.Close` / `SendCreate` | `Goose/Window.cs:328`, `:317` | server-side close sends `CLW` |
| Command arg split | `Goose/Commands/CommandEvent.cs:36` | `Split(' ', RemoveEmptyEntries)` |
| `string[]` binds the rest of the line | `Goose/Commands/CommandBinder.cs:26-33` | multi-word titles survive this way |
| Attribute privilege gate | `Goose/Commands/CommandRegistry.cs:211` | `def.Privilege is null \|\| player.HasPrivilege(...)` |
| Commands auto-register by reflection | `Goose/Commands/CommandRegistry.cs:17,30` | a new file in `Goose/Commands/` needs no registration |
| `AccessPrivilege.SetTitle` / `SetSurname` | `Goose/AccessLevels.cs:21-22`, Guide set at `:61` | reused by the grant commands; no enum change |
| `PlayerHandler.GetPlayerFromData` | `Goose/PlayerHandler.cs:178-186` | online **and** offline players, case-insensitive |
| `QuestWindow.GiveRewards` | `Goose/Quests/QuestWindow.cs:371`, cases at `:399-406` | assigns the field with no broadcast today |
| `RewardType.Title` / `Surname` | `Goose/Quests/QuestReward.cs:12-13` | value in `StringValue` |
| `TestWorldFixture.CapturingPlayer` | `TestSupport/TestWorldFixture.cs:93-98` | collects sent packets in `.Sent` |
| `TestWorldFixture.CommandPlayerOn` | `TestSupport/TestWorldFixture.cs:100-113` | `State = Ready`, `Map` set, inventory built |
| `TestWorldFixture.RunCommand` | `TestSupport/TestWorldFixture.cs:138-143` | real `AddEvent` + `EventHandler.Update` path |
| `RegisterOnlinePlayer` / `RegisterDatabasePlayer` | `TestSupport/TestWorldFixture.cs:115-136` | both needed for a GM command targeting a name |
| `PartyMemberBuffVisibilityTests` ordering helper | `Goose.Tests/PartyMemberBuffVisibilityTests.cs:316-329` | asserts `ERC`, `MKC`, `PBC`, `PBA` order |
| Integration save helpers | `Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs:60,78,94` | `MakeMinimalPlayer`, `RunInsert`, `RunUpdate` |

No schema change and no migration: the two keys live in the existing `player_properties` TEXT column, which `Player.SaveToDatabase` already serializes (`Goose/Player.cs:935`).

---

## Task 1: Collection support in `PropertiesDictionary`

**Files:**
- Modify: `Goose/PropertiesDictionary.cs:74-121` (`ConvertValue<T>`)
- Test: `Goose.Tests/PropertiesDictionaryTests.cs`

**Why first:** every later task reads the unlocked lists through `GetProperty<List<string>>`, which today throws `InvalidCastException` on any list that has been through JSON.

**Contract for the new branch:**
- Applies when the target type is `List<E>`, `IList<E>`, `ICollection<E>`, `IReadOnlyList<E>`, `IEnumerable<E>` or `E[]`, and the stored value is a non-null `IEnumerable` that is **not** a `string`.
- Elements go through `ConvertValue<E>` recursively, so `List<long>` narrows to `List<int>` and nested dictionaries keep working.
- The result is always a **new** collection — never the stored instance. `value is T` at `Goose/PropertiesDictionary.cs:85` short-circuits ahead of this branch and hands back the live reference when the types already match, so callers must not rely on identity and must not mutate what they got. One comment line on the branch records that.
- `HashSet<T>`, dictionaries, and non-collection targets are unchanged and still throw `InvalidCastException`.
- Failure behaviour: an element that cannot convert propagates the existing `InvalidCastException` from `ConvertValue<E>`.

**Step 1: Write the failing tests**

Add to `Goose.Tests/PropertiesDictionaryTests.cs`:

```csharp
[Fact]
public void String_lists_survive_the_json_round_trip()
{
    var props = new PropertiesDictionary { ["titles"] = new List<string> { "Lord", "Lady" } };

    var restored = JsonHelper.Deserialize<PropertiesDictionary>(JsonHelper.Serialize(props))!;

    Assert.Equal(new List<string> { "Lord", "Lady" }, restored.GetProperty<List<string>>("titles"));
}

[Fact]
public void Object_lists_convert_to_the_requested_element_type()
{
    var props = new PropertiesDictionary { ["counts"] = new List<object?> { 1L, 2L } };

    Assert.Equal(new List<int> { 1, 2 }, props.GetProperty<List<int>>("counts"));
}

[Fact]
public void A_string_is_not_read_as_a_list_of_characters()
{
    var props = new PropertiesDictionary { ["name"] = "abyss" };

    Assert.Throws<InvalidCastException>(() => props.GetProperty<List<string>>("name"));
}

[Fact]
public void The_returned_list_is_a_copy_of_the_stored_one()
{
    var stored = new List<string> { "Lord" };
    var props = new PropertiesDictionary { ["titles"] = stored };

    var read = props.GetProperty<List<string>>("titles");
    read.Add("Mutated");

    Assert.Single(stored);
    Assert.Single(props.GetProperty<List<string>>("titles"));
}
```

The third and fourth are the adversarial ones: the third fails if the branch tests the value's enumerability before excluding `string`; the fourth fails if the branch is reached only by conversion and the `value is T` fast path is left returning the live list.

**Step 2: Run to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~PropertiesDictionaryTests"
```
Expected: FAIL — `InvalidCastException` on the round-trip and element-conversion tests, and `A_string_is_not_read_as_a_list_of_characters` failing by returning `["a","b",...]` once the branch exists (it may pass trivially at this point; that is fine, it earns its keep in step 3).

**Step 3: Implement**

In `ConvertValue<T>`, after the enum block (`Goose/PropertiesDictionary.cs:106-118`) and before the final throw, add the branch described by the contract. A shape that satisfies it:

```csharp
if (value is not string && TryGetCollectionElementType(targetType, out var elementType) &&
    value is System.Collections.IEnumerable source)
{
    var list = (System.Collections.IList)Activator.CreateInstance(
        typeof(List<>).MakeGenericType(elementType))!;
    foreach (var element in source)
        list.Add(ConvertElement(element, elementType));

    if (!targetType.IsArray)
        return (T)list;

    var array = Array.CreateInstance(elementType, list.Count);
    list.CopyTo(array, 0);
    return (T)array;
}
```

plus two private statics: `TryGetCollectionElementType(Type, out Type)` (array element type, or the single generic argument when the definition is one of the five listed interfaces/classes) and `ConvertElement(object?, Type)`, which invokes `ConvertValue<T>` generically — cache one `MethodInfo` from `typeof(PropertiesDictionary).GetMethod(nameof(ConvertValue), BindingFlags.NonPublic | BindingFlags.Static)` and `MakeGenericMethod(elementType)` per element type. A `List<E>` satisfies all four interface targets, so one instance covers every non-array case.

**Step 4: Run to verify green**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~PropertiesDictionaryTests"
```
Expected: PASS, 7 facts. Then the whole unit suite to confirm nothing that reads a property regressed:

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q
```
Expected: `Passed! - Failed: 0, Passed: 1581` (1577 baseline + 4 new).

**Step 5: Commit**

```bash
git add Goose/PropertiesDictionary.cs Goose.Tests/PropertiesDictionaryTests.cs
git commit -m "feat(properties): read stored lists as typed collections"
```

| Invariant | Proved by |
| --- | --- |
| A stored list reads as `List<string>` after JSON round-trip | `String_lists_survive_the_json_round_trip` |
| Elements use the same conversion rules as scalars | `Object_lists_convert_to_the_requested_element_type` |
| `string` is never enumerated as a collection | `A_string_is_not_read_as_a_list_of_characters` |
| Getters never hand out the stored instance | `The_returned_list_is_a_copy_of_the_stored_one` |

---

## Task 2: `Player` unlock, grant, equip and display refresh

**Files:**
- Modify: `Goose/Player.cs` (near the `Properties` block at `:475`, and the private helpers below it)
- Modify: `Goose/Commands/SetTitleCommand.cs:15-35`, `Goose/Commands/SetSurnameCommand.cs:15-35`, `Goose/Commands/ChangeNameCommand.cs:31-53`
- Test: `Goose.Tests/TitleCollectionTests.cs` (new)

**Mutation impact:**
- Source of truth changed: `Player.Title` / `Player.Surname` (`Goose/Player.cs:110-114`) stay canonical for *what is displayed*; the new `titles` / `surnames` keys in `Player.Properties` (`Goose/Player.cs:475`) are canonical for *what is unlocked*. They are independent by design — a temporary title is displayed but not unlocked.
- Important readers: `P.MakeCharacter` (`Goose/Packets.cs:128-129`, the only place the display value reaches a client), `P.WhoCommand` output (`Goose/Commands/WhoCommand.cs:44`), the save path (`Goose/Player.cs:930-931`), and the new picker window.
- Derived/cached state affected: the client-side character object. `MKC` is the only packet carrying title, so the mutation propagates only through `ERC`+`MKC`. `SNF` does not carry it (`Goose/Packets.cs:415-442`) — sending `SNF` alone leaves every observer stale, which is exactly today's bug for the acting player.
- Required propagation sequence:
  1. assign `Title`/`Surname` on the `Player` object,
  2. if unlocked, append to the `Properties` list (a new `List<string>` assigned back),
  3. `ERC` then `MKC` to the player themself (`GetPlayersInRange` excludes them — `Goose/Map.cs:155-163`),
  4. `ERC` then `MKC` to each player in range,
  5. `Group.SendBuffSnapshotIfVisible` per viewer, after their `MKC`, so the party buff snapshot is not attached to an erased character.
- Invariants to preserve:
  - The stored list never holds a blank entry, and never holds two entries equal under `OrdinalIgnoreCase`.
  - The stored list keeps the casing of the first grant and its insertion order.
  - Granting is idempotent: granting the same title twice leaves one entry.
  - Equipping never mutates the list; the list is only ever replaced wholesale.
  - Nothing is sent for a player who is not logged in or has no map — `Player.Map` is null for a player loaded from the DB (`Goose/Player.cs:143`), and `QuestCompletionTests` drives rewards with both at their defaults.
  - Persistence stays on the normal save cycle: no `SaveToDatabase` call inside these methods.
- Observable proof required: tests assert `player.Title` and the stored list contents, and that `Sent` contains `ERC`/`MKC` — not that a helper was called.

**Step 1: Write the failing tests**

New `Goose.Tests/TitleCollectionTests.cs`, on a bare `Player(0)` with a `TestWorldFixture` for the `GameWorld`:

```csharp
namespace Goose.Tests;

public class TitleCollectionTests : IDisposable
{
    private readonly TestWorldFixture fixture = new();
    private GameWorld World => this.fixture.World;

    public void Dispose() => this.fixture.Dispose();

    [Fact]
    public void Granting_a_title_unlocks_and_equips_it()
    {
        var player = new Player(0);

        player.GrantTitle("Lord of the Vast", this.World);

        Assert.Equal("Lord of the Vast", player.Title);
        Assert.Equal(new List<string> { "Lord of the Vast" }, player.UnlockedTitles());
    }

    [Fact]
    public void Equipping_a_title_does_not_unlock_it()
    {
        var player = new Player(0);

        player.SetTitle("Temporary", this.World);

        Assert.Equal("Temporary", player.Title);
        Assert.Empty(player.UnlockedTitles());
    }

    [Fact]
    public void Granting_a_different_casing_of_a_stored_title_adds_no_entry()
    {
        var player = new Player(0);

        player.GrantTitle("Lord", this.World);
        player.GrantTitle("  LORD  ", this.World);

        Assert.Equal(new List<string> { "Lord" }, player.UnlockedTitles());
        Assert.Equal("LORD", player.Title);
    }

    [Fact]
    public void Granting_blank_unlocks_nothing_and_clears_the_display()
    {
        var player = new Player(0);
        player.GrantTitle("Lord", this.World);

        player.GrantTitle("   ", this.World);

        Assert.Equal(new List<string> { "Lord" }, player.UnlockedTitles());
        Assert.Equal("", player.Title);
    }

    [Fact]
    public void Grants_keep_insertion_order_and_surnames_are_a_separate_list()
    {
        var player = new Player(0);

        player.GrantTitle("Zeta", this.World);
        player.GrantTitle("Alpha", this.World);
        player.GrantSurname("Smith", this.World);

        Assert.Equal(new List<string> { "Zeta", "Alpha" }, player.UnlockedTitles());
        Assert.Equal(new List<string> { "Smith" }, player.UnlockedSurnames());
    }

    [Fact]
    public void A_granted_title_survives_a_properties_round_trip()
    {
        var player = new Player(0);
        player.GrantTitle("Lord", this.World);

        var reloaded = new Player(0);
        reloaded.LoadPropertiesFromColumn(JsonHelper.Serialize(player.Properties.Clone()));

        Assert.Equal(new List<string> { "Lord" }, reloaded.UnlockedTitles());
    }

    [Fact]
    public void Equipping_offline_sends_nothing_and_touches_no_map()
    {
        var player = new Player(0);

        player.GrantTitle("Lord", this.World);

        Assert.Equal(Player.States.NotLoggedIn, player.State);
        Assert.Equal("Lord", player.Title);
    }
}
```

`A_granted_title_survives_a_properties_round_trip` is the adversarial one for Task 1: it fails if the grant path stores something the loader cannot read back.

**Step 2: Run to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~TitleCollectionTests"
```
Expected: FAIL — compile error, `Player` has no `GrantTitle`/`SetTitle`/`UnlockedTitles`.

**Step 3: Implement**

Signatures to add to `Goose/Player.cs`:

```csharp
public const string TitlesProperty = "titles";
public const string SurnamesProperty = "surnames";

public List<string> UnlockedTitles();          // fresh list, blank entries dropped, empty when absent
public List<string> UnlockedSurnames();
public void GrantTitle(string title, GameWorld world);     // trim → unlock → equip
public void GrantSurname(string surname, GameWorld world);
public void SetTitle(string title, GameWorld world);       // assign + display refresh only
public void SetSurname(string surname, GameWorld world);
private void Unlock(string key, string value);
internal void RefreshCharacterDisplay(GameWorld world);
```

`RefreshCharacterDisplay` is `internal`, not `private`, because `ChangeNameCommand` needs it directly (step 3). Commands live in the same assembly (`Goose/Commands/`), so this adds no public surface.

Behavioural requirements, in order for the grant path:

1. `Unlock` returns without touching `Properties` when `value` is null, empty or whitespace, or when an existing entry matches under `OrdinalIgnoreCase`. Otherwise it appends the **trimmed** value to a fresh `List<string>` built from `GetProperty<List<string>>(key, new List<string>())` and assigns it to the key. Build the new list explicitly — do not add to the list the getter returned, because the `value is T` fast path (`Goose/PropertiesDictionary.cs:85`) hands back the stored instance when it is already a `List<string>`, and mutating it breaks the `Clone()` convention (`Goose/PropertiesDictionary.cs:136-146`).
2. `GrantTitle` trims its argument once and passes the trimmed value to both `Unlock` and `SetTitle`, so the displayed string and the stored string are identical.
3. `SetTitle` assigns the field as given and calls `RefreshCharacterDisplay`. It never reads or writes `Properties`.
4. `RefreshCharacterDisplay` returns immediately unless `State != States.NotLoggedIn && Map is not null`, then performs steps 3-5 of the propagation sequence above. `SendBuffSnapshotIfVisible` needs no extra guarding — it already checks group, state, map, range and GM invisibility (`Goose/Group.cs:121-127`).
5. `UnlockedTitles`/`UnlockedSurnames` read through the same `GetProperty<List<string>>(key, new List<string>())` and filter out null/whitespace entries, so a hand-edited or legacy value cannot put a blank line in a window.

`SetTitleCommand.cs:15-35` becomes: `player.SetTitle(titleText, world)` in place of the assignment plus the whole `if (player.Map is not null)` block, with `world.Send(player, P.StatusInfo(player))` kept in the online branch before it and `player.SaveToDatabase(world)` kept in the offline branch. `SetSurnameCommand.cs` mirrors it. `ChangeNameCommand.cs:31-53` keeps its rename, its `AddPlayerToData` and its `SNF` send, and swaps its inline `ERC`/`MKC` loop for `player.RefreshCharacterDisplay(world)` — a name change needs the same republish with no title involved, and calling the helper directly is how it gets reused instead of duplicated a fourth time.

**Step 4: Run to verify green**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~TitleCollectionTests|FullyQualifiedName~PartyMemberBuffVisibilityTests|FullyQualifiedName~Part3GmATests"
```
Expected: PASS. `PartyMemberBuffVisibilityTests` (`Goose.Tests/PartyMemberBuffVisibilityTests.cs:300-357`) is the regression net for the broadcast extraction: it asserts `ERC`, `MKC`, `PBC`, `PBA` ordering for `/settitle`, `/setsurname` and `/changename`, so a helper that reorders or drops the snapshot fails here.

Then the full suite: `dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q` → all pass.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/Commands/SetTitleCommand.cs Goose/Commands/SetSurnameCommand.cs Goose/Commands/ChangeNameCommand.cs Goose.Tests/TitleCollectionTests.cs
git commit -m "feat(players): unlock and equip titles and surnames separately"
```

| Invariant | Proved by |
| --- | --- |
| Grant unlocks + equips; equip alone does not unlock | `Granting_a_title_unlocks_and_equips_it`, `Equipping_a_title_does_not_unlock_it` |
| Dedupe is case-insensitive, first casing wins, trim applied | `Granting_a_different_casing_of_a_stored_title_adds_no_entry` |
| Blank never stored, always clears display | `Granting_blank_unlocks_nothing_and_clears_the_display` |
| Titles and surnames are independent, order preserved | `Grants_keep_insertion_order_and_surnames_are_a_separate_list` |
| Stored list reads back after save/load | `A_granted_title_survives_a_properties_round_trip` |
| Offline grant sends nothing | `Equipping_offline_sends_nothing_and_touches_no_map` |
| Existing broadcast order unchanged | `PartyMemberBuffVisibilityTests.SetTitle_RepublishesCharacterThenSnapshotToInGroupViewer` |

---

## Task 3: `/title` and `/surname` picker windows

**Files:**
- Create: `Goose/Commands/TitleCommand.cs`, `Goose/Commands/SurnameCommand.cs`
- Test: `Goose.Tests/TitleCommandTests.cs`, `Goose.Tests/SurnameCommandTests.cs`

**Mutation impact:**
- Source of truth changed: `Player.Title` / `Player.Surname` via `Player.SetTitle` (`Goose/Player.cs:110-114`).
- Important readers: `P.MakeCharacter` (`Goose/Packets.cs:128-129`); the picker window itself on the next open.
- Derived/cached state affected: the `List<string>` captured when the window was built. `OptionListWindow.LineClicked` indexes and bounds-checks that captured list (`Goose/OptionListWindow.cs:83-85`), so a grant landing while the window is open equips what the player saw rather than throwing.
- Required propagation sequence: entirely inside `SetTitle` — assign, `ERC`+`MKC` to self, `ERC`+`MKC` to range, buff snapshot. The command adds no propagation of its own.
- Invariants to preserve:
  - The window never adds or removes list entries; `Clear` equips `""` and leaves the list intact.
  - One picker window per player at a time.
  - The list is sorted before the `(current)` marker is appended, so equipping cannot reorder the window.
- Observable proof required: assert `player.Title` and `player.Sent` packets after a line click, and assert the rendered `WNF` lines.

**Step 1: Write the failing tests**

`Goose.Tests/TitleCommandTests.cs`, using the fixture helpers (`TestSupport/TestWorldFixture.cs:100-143`) and a second in-range player to prove the broadcast. `CommandPlayerOn` sets `player.Map` but does **not** put the player in `map.players`, which is what `GetPlayersInRange` iterates (`Goose/Map.cs:155-163`) — so every player that must observe a broadcast needs `map.AddPlayer(player, fixture.World)` (`Goose/Map.cs:184`), exactly as `PartyMemberBuffVisibilityTests.cs:23-26` does:

```csharp
namespace Goose.Tests;

public class TitleCommandTests
{
    private static TestWorldFixture Setup(out Map map, out TestWorldFixture.CapturingPlayer player)
    {
        var fixture = new TestWorldFixture();
        map = fixture.AddBaseMap(1, "m", 60, 40);
        player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        map.AddPlayer(player, fixture.World);
        return fixture;
    }

    [Fact]
    public void Title_lists_unlocked_options_alphabetically_with_clear_last()
    {
        using var fixture = Setup(out _, out var player);
        player.GrantTitle("Zeta", fixture.World);
        player.GrantTitle("alpha", fixture.World);
        player.Sent.Clear();

        Assert.True(fixture.RunCommand(player, "/title"));

        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToList();
        Assert.Contains("alpha|0|0|0|0|*", lines[0]);
        Assert.Contains("Zeta (current)|0|0|0|0|*", lines[1]);
        Assert.Contains("Clear|0|0|0|0|*", lines[2]);
    }

    [Fact]
    public void Picking_a_line_equips_it_and_republishes_the_character_to_self_and_range()
    {
        using var fixture = Setup(out var map, out var player);
        var other = fixture.CommandPlayerOn(map, 6, 5, "Other");
        map.AddPlayer(other, fixture.World);
        player.GrantTitle("Lord", fixture.World);
        player.GrantTitle("Duke", fixture.World);
        player.SetTitle("Lord", fixture.World);
        player.Sent.Clear();
        other.Sent.Clear();

        Assert.True(fixture.RunCommand(player, "/title"));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.LineClicked(1, 0, player, fixture.World);

        Assert.Equal("Duke", player.Title);
        Assert.Contains(player.Sent, s => s.StartsWith("ERC" + player.LoginID));
        Assert.Contains(player.Sent, s => s.StartsWith("MKC" + player.LoginID) && s.Contains("Duke"));
        Assert.Contains(other.Sent, s => s.StartsWith("MKC" + player.LoginID) && s.Contains("Duke"));
        Assert.Equal(new List<string> { "Lord", "Duke" }, player.UnlockedTitles());
    }

    [Fact]
    public void Clear_empties_the_display_and_keeps_everything_unlocked()
    {
        using var fixture = Setup(out _, out var player);
        player.GrantTitle("Lord", fixture.World);

        Assert.True(fixture.RunCommand(player, "/title"));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.LineClicked(1, 0, player, fixture.World);

        Assert.Equal("", player.Title);
        Assert.Equal(new List<string> { "Lord" }, player.UnlockedTitles());
    }

    [Fact]
    public void Opening_the_picker_twice_does_not_stack_windows()
    {
        using var fixture = Setup(out _, out var player);

        fixture.RunCommand(player, "/title");
        fixture.RunCommand(player, "/title");

        Assert.Single(player.Windows);
    }

    [Fact]
    public void An_empty_collection_still_offers_clear()
    {
        using var fixture = Setup(out _, out var player);
        player.Sent.Clear();

        fixture.RunCommand(player, "/title");

        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToList();
        Assert.Single(lines);
        Assert.Contains("Clear", lines[0]);
    }

    [Fact]
    public void More_than_eight_titles_pages_and_keeps_clear_on_the_last_page()
    {
        using var fixture = Setup(out _, out var player);
        for (var i = 0; i < 9; i++)
            player.GrantTitle("T" + i, fixture.World);
        player.Sent.Clear();

        fixture.RunCommand(player, "/title");

        Assert.DoesNotContain("Clear", string.Join("|", player.Sent.Where(s => s.StartsWith("WNF"))));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, fixture.World);
        Assert.Contains(player.Sent.Where(s => s.StartsWith("WNF")).Skip(8), s => s.Contains("Clear"));
    }
}
```

`Goose.Tests/SurnameCommandTests.cs` mirrors the first three facts against `/surname`, `GrantSurname` and `player.Surname`, plus one fact asserting the two windows are independent (`/surname` lists surnames only).

**Step 2: Run to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~TitleCommandTests"
```
Expected: FAIL — `RunCommand` returns `true` but nothing happens (no `/title` key in the registry), so `player.Windows` is empty and the `Assert.IsType<OptionListWindow>` casts fail.

**Step 3: Implement**

One command per file, shaped like `Goose/Commands/QuestAbandonCommand.cs:19-31` — no privilege, `Section = "General"`, auto-registered by reflection (`Goose/Commands/CommandRegistry.cs:17`):

```csharp
[Command("/title", Section = "General", Help = "Choose your displayed title.")]
```

Body requirements: close stale pickers with the same predicate `/questabandon` uses (`w.Type == Window.WindowTypes.OptionList && w.NPC is null`); take `player.UnlockedTitles()`; order it by `StringComparer.OrdinalIgnoreCase`; append `" (current)"` where the entry matches `player.Title` under `OrdinalIgnoreCase`; append `"Clear"` as the last line; construct `OptionListWindow(player, world, "Titles", lines, callback, null)` with no `OpeningLine`. The callback equips `unlocked[line]` when `line < unlocked.Count`, otherwise `""` — the captured list, not a re-read, so the index means what the player saw. `SurnameCommand` is identical with `SurnamesProperty`, `UnlockedSurnames`, `SetSurname`, and the window title `Surnames`.

**Step 4: Run to verify green**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~TitleCommandTests|FullyQualifiedName~SurnameCommandTests"
```
Expected: PASS.

**Step 5: Commit**

```bash
git add Goose/Commands/TitleCommand.cs Goose/Commands/SurnameCommand.cs Goose.Tests/TitleCommandTests.cs Goose.Tests/SurnameCommandTests.cs
git commit -m "feat(commands): /title and /surname pickers list unlocked options"
```

| Invariant | Proved by |
| --- | --- |
| Sorted before the marker is appended | `Title_lists_unlocked_options_alphabetically_with_clear_last` |
| Picking never mutates the collection | `Picking_a_line_equips_it_and_republishes_the_character_to_self_and_range` |
| `Clear` clears display, keeps unlocks | `Clear_empties_the_display_and_keeps_everything_unlocked` |
| One picker at a time | `Opening_the_picker_twice_does_not_stack_windows` |
| Paging keeps `Clear` reachable | `More_than_eight_titles_pages_and_keeps_clear_on_the_last_page` |

---

## Task 4: `/granttitle`, `/grantsurname`, and equip-only GM set commands

**Files:**
- Create: `Goose/Commands/GrantTitleCommand.cs`, `Goose/Commands/GrantSurnameCommand.cs`
- Modify: `Goose/Commands/SetTitleCommand.cs:10-16`, `Goose/Commands/SetSurnameCommand.cs:10-16`
- Test: `Goose.Tests/GrantTitleCommandTests.cs`

**Mutation impact:**
- Source of truth changed: `titles` / `surnames` in `Player.Properties` (`Goose/Player.cs:475`) for grants; `Player.Title`/`Surname` (`Goose/Player.cs:110-114`) for both commands.
- Important readers: the picker window (Task 3), `P.MakeCharacter` (`Goose/Packets.cs:128-129`), `Player.SaveToDatabase` (`Goose/Player.cs:930-935`).
- Derived/cached state affected: for an **offline** target the in-memory `Player` in `allNameToPlayer` is the only copy until `SaveToDatabase` runs — the grant must not rely on the periodic save for a player who is not logged in.
- Required propagation sequence:
  1. resolve the target with `world.PlayerHandler.GetPlayerFromData(name)` (`Goose/PlayerHandler.cs:178`),
  2. `GrantTitle` / `GrantSurname` (unlock + equip + display refresh, all guarded inside),
  3. for `State == NotLoggedIn`, `player.SaveToDatabase(world)` — matching what `SetTitleCommand.cs:39` does today,
  4. `ctx.Send` the confirmation to the acting GM.
- Invariants to preserve:
  - `/settitle` no longer writes the collection; `/granttitle` does.
  - A multi-word value survives: bind `(string name, string[] title)` and `string.Join(" ", title)`, relying on the rest-of-line binding at `Goose/Commands/CommandBinder.cs:26-33`. A bare `string` parameter truncates at the first space.
  - An empty value equips `""` and unlocks nothing.
  - Privilege is unchanged: `AccessPrivilege.SetTitle` / `SetSurname`, enforced by the attribute gate at `Goose/Commands/CommandRegistry.cs:211`. No `AccessLevels.cs` edit.
  - The GM's own client is unaffected: the acting player is the GM, the target gets the packets.
- Observable proof required: assert `player.Title` and `player.UnlockedTitles()` after the command runs through the real event path, not that a method was called.

**Step 1: Write the failing tests**

`Goose.Tests/GrantTitleCommandTests.cs`, following `PartyMemberBuffVisibilityTests` (`Goose.Tests/PartyMemberBuffVisibilityTests.cs:316-329`): register the target with both `RegisterOnlinePlayer` and `RegisterDatabasePlayer` so `GetPlayerFromData` resolves it, and use `fixture.CommandPlayerOn` + `Access = Player.AccessStatus.GameMaster` for the actor.

```csharp
[Fact]
public void Granttitle_unlocks_and_equips()
{
    using var fixture = new TestWorldFixture();
    var map = fixture.AddBaseMap(1, "m", 60, 40);
    var target = fixture.CommandPlayerOn(map, 5, 5, "Target");
    var gm = fixture.CommandPlayerOn(map, 6, 5, "Gm");
    gm.Access = Player.AccessStatus.GameMaster;
    fixture.RegisterOnlinePlayer(target);
    fixture.RegisterDatabasePlayer(target);

    Assert.True(fixture.RunCommand(gm, "/granttitle Target Lord of the Vast"));

    Assert.Equal("Lord of the Vast", target.Title);
    Assert.Equal(new List<string> { "Lord of the Vast" }, target.UnlockedTitles());
}

[Fact]
public void Settitle_equips_without_unlocking()
{
    // same setup
    Assert.True(fixture.RunCommand(gm, "/settitle Target Temporary Title"));

    Assert.Equal("Temporary Title", target.Title);
    Assert.Empty(target.UnlockedTitles());
}

[Fact]
public void Granting_twice_to_the_same_player_stores_one_entry()
{
    // same setup
    fixture.RunCommand(gm, "/granttitle Target Lord");
    fixture.RunCommand(gm, "/granttitle Target lord");

    Assert.Equal(new List<string> { "Lord" }, target.UnlockedTitles());
}

[Fact]
public void Unknown_target_reports_failure_and_unlocks_nothing()
{
    // same setup, no registered target
    Assert.True(fixture.RunCommand(gm, "/granttitle Nobody Lord"));

    Assert.Contains(gm.Sent, s => s.Contains("Couldn't find player"));
}

[Fact]
public void An_offline_target_is_saved_rather_than_broadcast_to()
{
    // a CommandPlayerOn-style player with State = NotLoggedIn and Map left null,
    // registered with RegisterDatabasePlayer only
    Assert.True(fixture.RunCommand(gm, "/granttitle Offline Lord"));

    Assert.Equal("Lord", offline.Title);
    Assert.Equal(new List<string> { "Lord" }, offline.UnlockedTitles());
}

[Fact]
public void An_unprivileged_actor_changes_nothing()
{
    // same setup, but gm.Access = Player.AccessStatus.Normal
    Assert.True(fixture.RunCommand(gm, "/granttitle Target Lord"));

    Assert.Empty(target.UnlockedTitles());
    Assert.NotEqual("Lord", target.Title);
}
```

Write the offline fact with a real `Player(0)` whose `State` stays `NotLoggedIn` and whose `Map` is left null, registered with `RegisterDatabasePlayer` only, and assert on state rather than packets — the adversarial part is that a helper missing the state guard would call `Map.GetPlayersInRange` on a null map and throw. The privilege fact is pinned by the dispatch gate: a matched-but-refused command is swallowed at `Goose/EventHandler.cs:214-224`, which returns `true` from `AddEvent` without running the handler, so `RunCommand` succeeds while state stays untouched.

**Step 2: Run to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~GrantTitleCommandTests"
```
Expected: FAIL — `/granttitle` is unknown so `RunCommand` dispatches nothing and the unlock assertions fail; `Settitle_equips_without_unlocking` fails because Task 2 left `/settitle` calling `SetTitle` (which is already equip-only), so expect that one to pass — if it does, keep it as the regression pin against a future re-merge of grant into set.

**Step 3: Implement**

`GrantTitleCommand` is `SetTitleCommand` with `player.GrantTitle(titleText, world)` instead of the assign-and-broadcast, keeping the `GetPlayerFromData` lookup, the "Couldn't find player." branch, and the offline `SaveToDatabase`:

```csharp
[Command("/granttitle ", AccessPrivilege.SetTitle, Section = "GM", Help = "Unlock a title for a player and equip it.")]
public sealed class GrantTitleCommand : BaseCommand
{
    public void Execute(CommandContext ctx, string name, string[] title)
    {
        var world = ctx.World;
        string titleText = string.Join(" ", title);

        Player? player = world.PlayerHandler.GetPlayerFromData(name);
        if (player is null)
        {
            ctx.Send("Couldn't find player.");
            return;
        }

        player.GrantTitle(titleText, world);
        ctx.Send("Granted title successfully.");

        if (player.State == Player.States.NotLoggedIn)
            player.SaveToDatabase(world);
    }
}
```

Note the trailing space in the key: argument-taking commands carry it (`Goose/Commands/SetTitleCommand.cs:3`), no-argument commands do not (`Goose/Commands/QuestsCommand.cs:5`). `GrantSurnameCommand` mirrors it with `AccessPrivilege.SetSurname`.

`SetTitleCommand`/`SetSurnameCommand` keep their existing structure from Task 2; confirm the online branch sends `SNF` and that the inline `ERC`/`MKC` loop is gone, and update only the `Help` text to say the title is not remembered (`Help = "Set a player's title without unlocking it."`).

**Step 4: Run to verify green**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~GrantTitleCommandTests|FullyQualifiedName~Part3GmATests|FullyQualifiedName~PartyMemberBuffVisibilityTests"
```
Expected: PASS. `Part3GmATests.SetTitle_sets_title_with_spaces` (`Goose.Tests/Part3GmATests.cs:372-387`) is the regression pin for the multi-word binding.

**Step 5: Commit**

```bash
git add Goose/Commands/GrantTitleCommand.cs Goose/Commands/GrantSurnameCommand.cs Goose/Commands/SetTitleCommand.cs Goose/Commands/SetSurnameCommand.cs Goose.Tests/GrantTitleCommandTests.cs
git commit -m "feat(commands): /granttitle and /grantsurname unlock titles permanently"
```

| Invariant | Proved by |
| --- | --- |
| Grant unlocks, set does not | `Granttitle_unlocks_and_equips`, `Settitle_equips_without_unlocking` |
| Multi-word values survive the binder | `Granttitle_unlocks_and_equips`, `Part3GmATests.SetTitle_sets_title_with_spaces` |
| Re-granting is idempotent | `Granting_twice_to_the_same_player_stores_one_entry` |
| Unknown target leaves state untouched | `Unknown_target_reports_failure_and_unlocks_nothing` |
| Offline grant persists, never broadcasts | `An_offline_target_is_saved_rather_than_broadcast_to` |
| Privilege still gates the command | the `AccessStatus.Normal` fact |

---

## Task 5: Quest rewards grant instead of assigning

**Files:**
- Modify: `Goose/Quests/QuestWindow.cs:399-406`
- Test: `Goose.Tests/QuestTitleRewardTests.cs` (new)

**Mutation impact:**
- Source of truth changed: same two places as Task 4, reached from the quest completion path (`Goose/Quests/QuestWindow.cs:366` → `GiveRewards`).
- Important readers: the picker window, `P.MakeCharacter` (`Goose/Packets.cs:128-129`), and every player in range who previously had to wait for a relog.
- Derived/cached state affected: none beyond the client character object. No derived state found in the quest system itself — `Quest.Rewards` is read-only here.
- Required propagation sequence: `GrantTitle` / `GrantSurname` does it all (unlock, equip, `ERC`+`MKC` to self and range, buff snapshot). The reward case keeps its existing `rewardMessage` line so the "[Quest Reward]: Title: …" chat text is unchanged.
- Invariants to preserve:
  - A rewarded title is unlocked **and** equipped.
  - The reward message text is unchanged.
  - Completion for a player with no map (the `QuestCompletionTests` fixture shape) sends nothing and does not throw.
- Observable proof required: assert the stored list and the packets received by an in-range player after a real window click, not that `GiveRewards` ran.

**Step 1: Write the failing tests**

`Goose.Tests/QuestTitleRewardTests.cs`, built on the `QuestCompletionTests.Fixture` pattern (`Goose.Tests/QuestCompletionTests.cs:9-58`) but with `fixture.CommandPlayerOn` players so the broadcast is observable:

```csharp
[Fact]
public void A_title_reward_unlocks_and_equips_it()
{
    // fixture with quest.Rewards.Add(new QuestReward { Type = RewardType.Title, StringValue = "Dragon Slayer" })
    CompleteViaWindow(fixture);

    Assert.Equal("Dragon Slayer", player.Title);
    Assert.Equal(new List<string> { "Dragon Slayer" }, player.UnlockedTitles());
}

[Fact]
public void A_title_reward_reaches_nearby_players_immediately()
{
    // player at (5,5), other at (6,5), both on the map
    CompleteViaWindow(fixture);

    Assert.Contains(other.Sent, s => s.StartsWith("MKC" + player.LoginID) && s.Contains("Dragon Slayer"));
}

[Fact]
public void A_surname_reward_unlocks_into_the_surname_list()
{
    CompleteViaWindow(fixture);

    Assert.Equal("Smith", player.Surname);
    Assert.Equal(new List<string> { "Smith" }, player.UnlockedSurnames());
    Assert.Empty(player.UnlockedTitles());
}

[Fact]
public void Repeating_a_rewardable_title_does_not_duplicate_the_entry()
{
    // complete the same repeatable quest twice
    Assert.Equal(new List<string> { "Dragon Slayer" }, player.UnlockedTitles());
}
```

**Step 2: Run to verify red**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~QuestTitleRewardTests"
```
Expected: FAIL — `UnlockedTitles()` is empty (the reward assigns the field) and the in-range player receives no `MKC`.

**Step 3: Implement**

Replace the two cases at `Goose/Quests/QuestWindow.cs:399-406` with `player.GrantTitle(reward.StringValue, world)` and `player.GrantSurname(reward.StringValue, world)`, keeping each `rewardMessage` assignment exactly as it is.

**Step 4: Run to verify green**

```bash
dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q --filter "FullyQualifiedName~Quest"
```
Expected: PASS — including the pre-existing `QuestCompletionTests`, which complete quests for a player with no map and so prove the guard in `RefreshCharacterDisplay` holds on that path.

**Step 5: Commit**

```bash
git add Goose/Quests/QuestWindow.cs Goose.Tests/QuestTitleRewardTests.cs
git commit -m "feat(quests): title and surname rewards unlock into the player collection"
```

| Invariant | Proved by |
| --- | --- |
| Rewarded title is unlocked and equipped | `A_title_reward_unlocks_and_equips_it` |
| The change is visible without a relog | `A_title_reward_reaches_nearby_players_immediately` |
| Surname rewards use the surname list | `A_surname_reward_unlocks_into_the_surname_list` |
| Repeatable quests do not duplicate | `Repeating_a_rewardable_title_does_not_duplicate_the_entry` |
| No-map completion is safe | existing `QuestCompletionTests` |

---

## Task 6: Persistence proof through the real save path

**Files:**
- Modify: `Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs`

**Mutation impact:**
- Source of truth changed: nothing — this task adds no behaviour, it proves that `titles` written by `GrantTitle` reaches SQLite and comes back readable.
- Important readers: `Player.LoadPropertiesFromColumn` (`Goose/Player.cs:478-483`) feeding `UnlockedTitles`.
- Derived/cached state affected: none. No derived state found.
- Required propagation sequence: `GrantTitle` → `Properties["titles"]` → `JsonHelper.Serialize(Properties.Clone())` (`Goose/Player.cs:935`) → the real INSERT/UPDATE string from `Player.cs` → reload.
- Invariants to preserve: the JSON stays a plain array of strings that an older server build would ignore rather than choke on; a reload reads as `List<string>`.
- Observable proof required: read the value back out of the database, not out of the in-memory player.

**Step 1: Write the failing test**

Add to `Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs`, reusing `MakeMinimalPlayer` / `RunInsert` / `RunUpdate` (`Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs:60,78,94`). `TestWorldFixture` is compiled into this project (`Goose.IntegrationTests/Goose.IntegrationTests.csproj:70`, used by `BackstabScriptTests.cs:115-120`), so use a real `GameWorld` rather than a null one.

```csharp
[Fact]
public void An_unlocked_title_round_trips_through_the_real_save_path()
{
    using var conn = OpenWithPlayersTable();
    using var fixture = new TestWorldFixture();

    var player = MakeMinimalPlayer(playerId: 1);
    player.GrantTitle("Lord of the Vast", fixture.World);
    RunInsert(player, conn);

    var reloaded = ReloadProperties(conn, 1);
    Assert.Equal(new List<string> { "Lord of the Vast" }, reloaded.GetProperty<List<string>>("titles"));

    var again = MakeMinimalPlayer(playerId: 1);
    again.LoadPropertiesFromColumn(JsonHelper.Serialize(reloaded));
    again.GrantTitle("Duke", fixture.World);
    RunUpdate(again, conn);

    Assert.Equal(new List<string> { "Lord of the Vast", "Duke" },
        ReloadProperties(conn, 1).GetProperty<List<string>>("titles"));
}
```

Both `GrantTitle` calls run against a player whose `State` is `NotLoggedIn` and whose `Map` is null, so `RefreshCharacterDisplay` returns at its first guard and touches nothing in the fixture world — the world exists only to satisfy the signature. The assertions that matter are the two `GetProperty<List<string>>` reads on data that came out of SQLite: the first is the test Task 1 exists to satisfy, and the second proves a reloaded list can be appended to and written back.

**Step 2: Run to verify red**

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --nologo -v q --filter "FullyQualifiedName~PlayerPropertiesPersistenceTests"
```
Expected: FAIL on the first assertion if Task 1 is somehow absent (`InvalidCastException`); it may already pass, in which case it is the regression pin and stays.

**Step 3: Run to verify green**

```bash
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --nologo -v q
```
Expected: PASS, all existing persistence facts plus the new one.

**Step 4: Commit**

```bash
git add Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs
git commit -m "test(persistence): unlocked titles survive the real save path"

dotnet test Goose.Tests/Goose.Tests.csproj --nologo -v q
dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --nologo -v q
```
Expected: both green — the full-suite gate for the whole feature.

## Notes for the implementer

- **"Surname" is overloaded in this codebase.** `ItemHandler.surnames` and `ItemModifier` (`Goose/ItemHandler.cs:168-170,220-222`) are *item* name affixes rolled onto loot. They have nothing to do with `Player.Surname` and are not touched here.
- **The other `Title`/`Surname` assignments stay as plain field writes.** `Player.LoadFromReader` (`Goose/Player.cs:741-742`) must not broadcast during login, and `Player.LoadFromAutoCreate` (`Goose/Player.cs:604-605`) must not put `Settings.StartingTitle` into the new list — that would be the backfill this design rejects. `Pet.cs:143-144` is likewise left alone; pets never own a picker.
- **`ChangeNameCommand` is in Task 2 only to absorb duplicated broadcast code.** Its observable behaviour must not change; `PartyMemberBuffVisibilityTests.ChangeName_RepublishesCharacterThenSnapshotToInGroupViewer` is the proof.

---

## Out of scope

Nothing here revokes an unlocked title, sanitizes commas in title text (which corrupts `MKC` — accepted in the design), backfills existing `player_title` values into the new lists, or changes the client or any file under `Goose/Data`.
