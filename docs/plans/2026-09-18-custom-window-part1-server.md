# Custom Item Window — Part 1: Server Implementation Plan

**Goal:** Add the server side of the custom item window: a `CustomWindow` opened by using a custom ticket, `CWS`/`CWC` packet handling, `CWG` replies, and the create/consume/place flow.

**Architecture:** A new `CustomWindow : Window` (frame 28) owned by the player, opened from the ticket's item script. Two new client→server packets (`CWS`, `CWC`) are registered in the `EventHandler` packet trie like `WBC`; the server replies with `CWG` (equipped id + pose) or server messages. Shared validation/build logic is factored out of `CustomCommand` into a new `CustomItem` static class so `/custom` and the window behave identically.

**Tech Stack:** C# / .NET 10, xUnit (`Goose.Tests`, `Goose.IntegrationTests`), Roslyn item scripts (`.csx`), gws CLI for game data.

**Design doc:** `docs/plans/2026-09-18-custom-window-design.md` (this is Part 1 of 2; Part 2 is the Godot client).

Work in the worktree: `/home/agent/workspace/illutiagooseserver/.worktrees/custom-window` (branch `custom-window`).

## APIs verified

| API | Citation |
| --- | --- |
| `Event` base: `Player`, `Data` (string), `Ready(GameWorld)` | `Goose/Event.cs:5-31` |
| Packet registration: `("WBC", Open(typeof(WindowButtonClickEvent)))` in ctor map | `Goose/EventHandler.cs:152` |
| Event state gate: `this.Player.State == Player.States.Ready` | `Goose/Events/WindowButtonClickEvent.cs:14` |
| `Window` enums: `WindowFrames` (last = `OptionList = 27`), `WindowTypes` (last = `Recipe`) | `Goose/Window.cs:29-77` |
| `Window.Create` sends `MKW` + `Populate` + `ENW`; `Window.Close` removes + sends `CLW` | `Goose/Window.cs:86-99, 310-315` |
| `P.MakeWindow` / `P.EndWindow` / `P.CloseWindow` / `P.ServerMessage` / `P.InventorySlot` | `Goose/Packets.cs:632, 643, 648, 9, 553` |
| `Inventory.UseConsumable` — main inventory only, calls `OnUseConsumableEvent`, removes item only if it returns `true` (fail-closed on exception) | `Goose/Inventory.cs:401-447` |
| `Inventory.RemoveItem(Item, long, GameWorld)` — nulls slot (stack==1) or decrements; sends slot update | `Goose/Inventory.cs:452-495` |
| `Inventory.AddItem(Item, long, GameWorld)` — first free/stackable slot, sends update, `bool` | `Goose/Inventory.cs:78-103` |
| `Inventory.GetNumberOfFreeSlots()` | `Goose/Inventory.cs:129-137` |
| `Inventory.GetSlot(int)` / `SetSlot(int, ItemSlot?)` | `Goose/ItemContainer.cs:36`, `Goose/Inventory.cs:184` |
| `ItemHandler.AddAndAssignId(Item, GameWorld)` — assigns id, fires `OnCreateEvent`, stores item | `Goose/ItemHandler.cs:235-251` |
| Template script binding: `template.Script = world.ScriptHandler.GetScript<IItemScript>(scriptPath)` from DB `script_path` | `Goose/ItemHandler.cs:128-131` |
| `Script<T>` loads/compiles a `.csx` file; script ends `return typeof(X);` | `Goose/Scripting/Script.cs:14-60`, `Goose/Data/Illutia/Scripts/Item/HairCutItem.csx` |
| `Item.Custom` derived from `Description.StartsWith("Custom created by ")` — drives destroy→ripped-ticket | `Goose/Item.cs:129`, `Goose/Events/DestroyItemEvent.cs:41` |
| Existing `/custom` logic to factor: `ValidateCustomSlots`, `ParseRGBA`, item-build block in `Make` | `Goose/Commands/CustomCommand.cs:281-330, 355-364, 135-165` |
| Test harness: `TestWorldFixture` (`CommandPlayerOn`, `RunCommand(player, packet)` dispatches raw packets through the real `EventHandler`, `CapturingPlayer.Sent`, `AddBaseItemTemplate`) | `TestSupport/TestWorldFixture.cs:66-156` |
| `/custom` test pattern to mirror | `Goose.Tests/Part3CustomTests.cs:1-40` |
| `CompileSpellEffectScript` pattern for compiling a test script from a string | `TestSupport/TestWorldFixture.cs:37-43` |
| `Log.Types.CreatedCustom` | `Goose/Log.cs:29` |
| Settings: `CustomTicketId` (production value 643) | `Goose/GooseSettings.cs:143`, `Goose/GooseSettings.json:19` |
| Item sheet `script path` column (AS) | goose-game-data skill, Items column reference |

## Protocol (fixed by the design doc — Part 2 depends on these exact strings)

- `CWS<lookInvSlot>,<statsInvSlot>` — client→server, full two-slot state on every drop/clear, `0` = empty.
- `CWG<equippedId>,<pose>` — server→client, only in reply to a valid `CWS` with non-zero look slot. `pose` = look item `BodyState`.
- `CWC<lookInvSlot>,<statsInvSlot>,<r>,<g>,<b>,<a>,<name>` — client→server create request. Name may contain spaces; commas are stripped by the server after parsing.
- Window: `MKW` with `WindowFrames.Custom = 28`, title `Custom`, buttons `0,1,0,0,1`.

---

### Task 1: Factor shared customisation logic into `CustomItem`

**Files:**
- Create: `Goose/CustomItem.cs`
- Modify: `Goose/Commands/CustomCommand.cs` (use the helpers; behaviour unchanged)
- Test: `Goose.Tests/CustomItemTests.cs`

**Mutation impact:**
- Source of truth changed: `/custom` behaviour currently inline in `Goose/Commands/CustomCommand.cs` (`ValidateCustomSlots` 281-330, `ParseRGBA` 355-364, item-build block in `Make` 135-165).
- Important readers: `Goose.Tests/Part3CustomTests.cs` (full behaviour suite for `/custom`), `Goose.Tests/CommandDispatchTests.cs` / `HelpTests.cs` (command surface).
- Derived/cached state affected: none.
- Required propagation sequence: pure refactor — move logic verbatim into `CustomItem`, call it from `CustomCommand`. No packet or state change.
- Invariants to preserve: every `Part3CustomTests` test stays green unmodified; `/custom` error messages byte-identical.
- Observable proof required: `dotnet test Goose.Tests` fully green (821 baseline) with `Part3CustomTests` untouched.

**Step 1: Write the failing tests**

`Goose.Tests/CustomItemTests.cs` — unit tests for the new `Goose.CustomItem` static class:

- `ValidateItems` (signature: `static bool ValidateItems(GameWorld world, Player player, Item statsItem, Item lookItem)` — sends the server message itself, mirroring today's `ValidateCustomSlots` messages):
  - both valid same-type equipment → true
  - stats Chest + look Helmet → false (adversarial: catches a helper that drops the same-type rule)
  - stats OneHanded + look TwoHanded → true (the weapon exception)
  - either item a Ring/Necklace/Pauldrons/Cloak/Belt/Gloves → false
  - either item not Armor/Weapon use-type → false
- `ParseRGBA` (signature: `static string? ParseRGBA(int r, int g, int b, int a, int maxAlpha = 255)`):
  - r/g/b 0 and 255 → null; -1 and 256 → error (per channel)
  - `maxAlpha: 200`: a=200 → null, a=201 → error (adversarial: catches hard-coded 255)
  - default: a=255 → null
- `SanitizeName` (signature: `static string? SanitizeName(string raw)` — trim, strip commas, truncate to 255, null if empty result):
  - `"  My Sword  "` → `"My Sword"`
  - `"a,b,c"` → `"abc"`
  - 300 chars → 255 chars
  - `""` / `"   "` / `",,"` → null
- `BuildCustomItem` (signature: `static Item BuildCustomItem(Item statsItem, Item lookItem, int r, int g, int b, int a, string name, string playerName)` — the item-build block from `CustomCommand.Make`):
  - stats copied from stats item (StatMultiplier, BaseStats/TotalStats clones, TotalWeaponDamage, IsBound, ScriptParams, TitleId/SurnameId properties when present)
  - look copied from look item (BodyState, GraphicEquipped, GraphicTile, GraphicFile) + new RGBA
  - `Name` = sanitized name; `Description` = `"Custom created by " + playerName` (this prefix is load-bearing for `Item.Custom`, `Goose/Item.cs:129`)
  - adversarial: mutating `statsItem.BaseStats` after the call does not affect the built item (clone check)

Run: `dotnet test Goose.Tests --filter CustomItemTests` — expected FAIL (class does not exist).

**Step 2: Implement `Goose/CustomItem.cs`**

Move the logic verbatim from `CustomCommand` (messages included). Then refactor `CustomCommand`:
- `ValidateCustomSlots` body → null-check the two combine-bag slots, then call `CustomItem.ValidateItems`.
- `ParseRGBA` calls → `CustomItem.ParseRGBA(r, g, b, a)` (default maxAlpha keeps `/custom` at 255).
- `Make` name handling → `CustomItem.SanitizeName(string.Join(" ", name))`; item-build block → `CustomItem.BuildCustomItem`.

**Step 3: Green**

Run: `dotnet test Goose.Tests` — expected: all pass, including unmodified `Part3CustomTests`.

**Step 4: Commit**

```bash
git add Goose/CustomItem.cs Goose/Commands/CustomCommand.cs Goose.Tests/CustomItemTests.cs
git commit -m "Factor shared custom item validation and build logic"
```

---

### Task 2: `CustomWindow` and window plumbing

**Files:**
- Modify: `Goose/Window.cs` — add `Custom = 28` to `WindowFrames` (after `OptionList = 27`), add `Custom` to `WindowTypes` (after `Recipe`)
- Create: `Goose/CustomWindow.cs`
- Test: `Goose.Tests/CustomWindowTests.cs`

**Step 1: Write the failing tests**

`Goose.Tests/CustomWindowTests.cs` (harness pattern from `Part3CustomTests.cs:8-16`):

- Constructing a `CustomWindow` for a player adds it to `player.Windows`, sets `Frame == WindowFrames.Custom`, `Type == WindowTypes.Custom`, `Buttons == "0,1,0,0,1"`, and sends `MKW` (assert a `player.Sent` entry starts with `MKW` and contains `,28,Custom,0,1,0,0,1,`) followed by `ENW<id>`.
- `Clicked(Window.ButtonTypes.Close, ...)` and `Exit` remove the window from `player.Windows` and send **no** `CLW` (client-originated close is not echoed — same pattern as `Window.cs:133-141` for Bank/CombineBag).
- Constructor signature: `CustomWindow(Player player, GameWorld world, int ticketSlotId)` — stores the ticket's **inventory slot id** (not the `ItemSlot` object, which can be replaced).

Run: `dotnet test Goose.Tests --filter CustomWindowTests` — expected FAIL.

**Step 2: Implement**

`Goose/CustomWindow.cs`:

- `public class CustomWindow : Window`
- Fields: `int TicketSlotId`.
- Ctor sets `ID = ++player.LastWindowID`, `Title = "Custom"`, `Buttons = "0,1,0,0,1"`, `Frame = WindowFrames.Custom`, `Type = WindowTypes.Custom`, adds to `player.Windows`, calls `SendCreate(player, world)`.
- `override void Clicked(...)`: `Close`/`Exit` → `player.Windows.Remove(this)`. No other buttons are shown.
- Static guard used by the ticket script and tests: `public static CustomWindow? FindOpen(Player player)` → `player.Windows.OfType<CustomWindow>().FirstOrDefault()`.

**Step 3: Green + full suite**

Run: `dotnet test Goose.Tests` — expected: all pass.

**Step 4: Commit**

```bash
git add Goose/Window.cs Goose/CustomWindow.cs Goose.Tests/CustomWindowTests.cs
git commit -m "Add CustomWindow (frame 28) for ticket-driven customisation"
```

---

### Task 3: `CWS`/`CWC` events, `CWG` reply, create flow

**Files:**
- Create: `Goose/Events/CustomWindowSlotEvent.cs` (`CWS`)
- Create: `Goose/Events/CustomWindowCreateEvent.cs` (`CWC`)
- Modify: `Goose/EventHandler.cs:152` area — register `("CWS", Open(typeof(CustomWindowSlotEvent)))` and `("CWC", Open(typeof(CustomWindowCreateEvent)))` in the ctor packet map
- Modify: `Goose/CustomWindow.cs` — add `HandleSlot(...)` and `Create(...)`
- Test: `Goose.Tests/CustomWindowPacketTests.cs`

**Packet parsing (mirror `WindowButtonClickEvent.cs:13-45`):**

- `CWS`: `((string)this.Data).Substring(3).Split(',')` — exactly 2 ints; refuse otherwise. Gate on `Player.State == Player.States.Ready`.
- `CWC`: `Substring(3).Split(',', 7)` — exactly 7 parts; first six parse as int, seventh is the raw name (may contain spaces; commas stripped by `SanitizeName`). Refuse on parse failure.

**`CustomWindowSlotEvent` behaviour** (delegates to `CustomWindow.HandleSlot(world, player, lookSlotId, statsSlotId)`):

1. No open `CustomWindow` → ignore silently (stale packet).
2. Resolve each non-zero id via `player.Inventory.GetSlot(id)` (main inventory only — combine bag/equipped/bank unreachable by construction). Non-zero id with null slot → server message `"Items missing for customisation"`, no `CWG`.
3. Reject if look id == stats id (same item cannot be both), or either id == the window's `TicketSlotId`.
4. Per-item rules: each non-zero item must pass the equipment/invisible-slot checks (use the `CustomItem.ValidateItems` per-item portion — factor it as `static bool ValidateSingleItem(GameWorld world, Player player, Item item)` returning false + message, with `ValidateItems` = both singles + the same-type rule).
5. Same-type rule only when both non-zero.
6. On success with non-zero look slot: `world.Send(player, "CWG" + lookItem.GraphicEquipped + "," + lookItem.BodyState)`.
7. On any failure: `P.ServerMessage` with the existing `/custom` wording; no `CWG`.

**`CustomWindowCreateEvent` behaviour** (delegates to `CustomWindow.Create(world, player, lookSlotId, statsSlotId, r, g, b, a, rawName)`):

1. No open window → ignore silently.
2. Re-fetch fresh: ticket = `player.Inventory.GetSlot(TicketSlotId)`, look/stats = `GetSlot(...)`. Ticket must exist and `TemplateID == world.Settings.CustomTicketId`; look/stats must exist and be non-zero and distinct.
3. `CustomItem.ValidateItems(world, player, statsItem, lookItem)`.
4. `CustomItem.ParseRGBA(r, g, b, a, maxAlpha: 200)`; `CustomItem.SanitizeName(rawName)` (null → refuse).
5. Optimistic capacity: `player.Inventory.GetNumberOfFreeSlots() + freedByConsumes >= 1` where `freedByConsumes` = 2 (look and stats are equipment, stack 1) + 1 if the ticket slot's `Stack == 1`. Runs before any mutation.
6. Consume, in order: `RemoveItem(lookItem, 1, world)`, `RemoveItem(statsItem, 1, world)`, `RemoveItem(ticketItem, 1, world)` (`Goose/Inventory.cs:452` — nulls or decrements and sends the slot update).
7. `var item = CustomItem.BuildCustomItem(statsItem, lookItem, r, g, b, a, name, player.Name);` then `world.ItemHandler.AddAndAssignId(item, world)` (`Goose/ItemHandler.cs:235`).
8. Place: if the ticket slot is now empty → `SetSlot(TicketSlotId, new ItemSlot { Item = item, Stack = 1 })` + `SendSlot(TicketSlotId, world)`; else `player.Inventory.AddItem(item, 1, world)` (first free slot; step 5 guarantees success).
9. Log: `world.LogHandler.Log(Log.Types.CreatedCustom, player, $"{item.Name} ({item.TemplateID}) {lookItem.TemplateID}|{r},{g},{b},{a}", item.ItemID)` — same format as `CustomCommand.cs:190-191`.
10. `this.Close(player, world)` (sends `CLW`, `Goose/Window.cs:310`).

On any failure at steps 2-5: server message, **nothing consumed**, window stays open.

**Step 1 (TDD order): write the failing tests first** in `Goose.Tests/CustomWindowPacketTests.cs`. Scenario helper: fixture with `CustomTicketId = 823` (pattern `Part3CustomTests.cs:8-16`), ticket/stats/look templates (900/901, both `Weapon`, default slot `OneHanded` from `AddBaseItemTemplate`), items placed in main inventory slots via `player.Inventory.SetSlot(i, new ItemSlot { Item = ... })` (pattern `Part3CustomTests.cs:30-34` but on the main inventory), open a `CustomWindow` directly. Drive packets with `fixture.RunCommand(player, "CWS...")` / `"CWC..."` (dispatches through the real `EventHandler`, `TestWorldFixture.cs:122-128`).

Tests:

- `CWS` valid look only (`CWS5,0` with a weapon in slot 5) → `player.Sent` contains `"CWG<equippedId>,<pose>"` (set `GraphicEquipped`/`BodyState` on the template to known values, e.g. 777/6).
- `CWS` valid stats only (`CWS0,6`) → **no** `CWG` (adversarial: catches sending `CWG` for the stats slot).
- `CWS` both valid, same type → `CWG` once.
- `CWS` both valid, different types (chest template + weapon template) → server message, no `CWG`.
- `CWS` 1H + 2H weapons → `CWG` (exception preserved).
- `CWS` with a non-equipment item (use-type `NoUse` template) → message, no `CWG`.
- `CWS` with look id == stats id → message, no `CWG`.
- `CWS` with an id equal to the ticket slot → message, no `CWG`.
- `CWS` with an empty inventory slot id (non-zero, null slot) → message, no `CWG`.
- `CWS` with no open window → nothing sent.
- `CWC` happy path (`CWC5,6,10,20,30,40,My Sword`): ticket, look, and stats slots all emptied/decremented; a new item exists in the freed ticket slot with `Name "My Sword"`, `GraphicR/G/B/A = 10/20/30/40`, `GraphicEquipped`/`BodyState` from the look item, stats from the stats item, `Description "Custom created by <name>"`; `CLW<windowId>` sent; window removed from `player.Windows`.
- `CWC` with `a = 201` → message, nothing consumed (all three slots still hold their items) (adversarial: catches the 255 cap).
- `CWC` with `a = 200` → succeeds (boundary).
- `CWC` with empty name (`CWC5,6,10,20,30,40,`) → message, nothing consumed.
- `CWC` with commas in name (`...,a,My,Sword`) → succeeds, `Name == "MySword"`.
- `CWC` where the look item was moved out of its slot after a prior `CWS` → message, nothing consumed (fresh re-validation).
- `CWC` with look id == stats id → message, nothing consumed.
- Optimistic capacity: set `InventorySize` small (e.g. 5 via the `GooseSettings` configure delegate), fill every slot (ticket + look + stats + 2 fillers), `CWC` → succeeds and the new item lands in a freed slot.
- Ticket with `Stack = 2`: `CWC` → ticket slot keeps `Stack = 1`, new item placed in a different free slot.

Run: `dotnet test Goose.Tests --filter CustomWindowPacketTests` — expected FAIL (packets unregistered: `RunCommand` returns false / nothing sent).

**Step 2: Implement** the two event classes, register them in `Goose/EventHandler.cs`, add `HandleSlot`/`Create` to `CustomWindow`, and factor `ValidateSingleItem` out of `CustomItem.ValidateItems` (Task 1's tests keep covering the combined path).

**Step 3: Green + full suites**

Run: `dotnet test Goose.Tests && dotnet test Goose.IntegrationTests` — expected: all pass (275 integration baseline).

**Step 4: Commit**

```bash
git add Goose/Events/CustomWindowSlotEvent.cs Goose/Events/CustomWindowCreateEvent.cs \
        Goose/EventHandler.cs Goose/CustomWindow.cs Goose/CustomItem.cs \
        Goose.Tests/CustomWindowPacketTests.cs
git commit -m "Handle CWS/CWC packets and the custom window create flow"
```

---

### Task 4: Ticket item script + test helper + game data

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Item/CustomTicket.csx`
- Modify: `TestSupport/TestWorldFixture.cs` — add `CompileItemScript`
- Test: `Goose.IntegrationTests/CustomTicketScriptTests.cs`
- Game data: Items sheet, item 643, column AS (`script path`) — via gws CLI

**Step 1: Add the test helper** (prerequisite for the test)

`TestSupport/TestWorldFixture.cs`, mirroring `CompileSpellEffectScript` (`TestWorldFixture.cs:37-43`):

```csharp
public Script<IItemScript> CompileItemScript(string body, string fileName)
{
    Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Item"));
    var relativePath = "Scripts/Item/" + fileName;
    File.WriteAllText(Path.Combine(DataDirectory, relativePath), body);
    return World.ScriptHandler.GetScript<IItemScript>(relativePath);
}
```

**Step 2: Write the failing integration tests**

`Goose.IntegrationTests/CustomTicketScriptTests.cs`:

- Fixture: `TestWorldFixture(s => s.CustomTicketId = 823)`, base map, `CommandPlayerOn`; ticket template 823 with `template.Script = fixture.CompileItemScript(shippedScriptBody, "CustomTicket.csx")` — read the shipped file's contents from `Goose/Data/Illutia/Scripts/Item/CustomTicket.csx` so the test exercises the real script text (pattern: `GlobalScriptFixture.CompileShipped`, `Goose.IntegrationTests/Fixtures/GlobalScriptFixture.cs:80`).
- Place a ticket item in inventory slot 1; `player.Inventory.UseConsumable(item, world)` (`Goose/Inventory.cs:401`):
  - window opens: `player.Sent` contains an `MKW` with `,28,Custom,`; the ticket item is **still in slot 1** (script returns false → not consumed, `Goose/Inventory.cs:431-445`).
  - second `UseConsumable` → refusal server message, still exactly one `MKW` sent, ticket still present.
- Adversarial: a broken-script regression is already covered by the fail-closed path in `UseConsumable` — no extra test.

Run: `dotnet test Goose.IntegrationTests --filter CustomTicketScriptTests` — expected FAIL (script file doesn't exist / helper missing).

**Step 3: Implement the shipped script**

`Goose/Data/Illutia/Scripts/Item/CustomTicket.csx` (pattern: `HairCutItem.csx` — class deriving `BaseItemScript`, ends with `return typeof(CustomTicket);`):

- `OnUseConsumableEvent`:
  - if `CustomWindow.FindOpen(player) is not null` → `world.Send(player, P.ServerMessage("You are already customising an item."))`, return `false`.
  - find the ticket's inventory slot id (loop `player.Inventory.GetSlot(i)` for `slot.Item == item`); if not found → return `false` (fail-closed, item kept).
  - `new CustomWindow(player, world, ticketSlotId);` return `false` (the ticket is only consumed on a successful create).
- The csx compiles against the server assembly with imports `Goose`, `Goose.Scripting` etc. (`Goose/Scripting/Script.cs:29-35`) — no file I/O or new references needed.

**Step 4: Green**

Run: `dotnet test Goose.IntegrationTests --filter CustomTicketScriptTests && dotnet test Goose.Tests` — expected: all pass.

**Step 5: Game data — point the production ticket at the script**

Using the goose-game-data skill (`@.agents/skills/goose-game-data/SKILL.md`), from the repo root:

```bash
S=.agents/skills/goose-game-data/scripts/gsheets.py
python3 $S read Items --where item_template_id=643      # confirm the ticket row
python3 $S update Items --id 643 --set "script path=Scripts/Item/CustomTicket.csx" --dry-run
python3 $S update Items --id 643 --set "script path=Scripts/Item/CustomTicket.csx"
python3 $S read Items --where item_template_id=643      # confirm it landed
```

Note: a running server picks this up on next start or `/updatesql` (skill, "How the data reaches the server"). The Aspereta data set has its own ticket id if/when it is wired up — out of scope here (YAGNI).

**Step 6: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Item/CustomTicket.csx TestSupport/TestWorldFixture.cs \
        Goose.IntegrationTests/CustomTicketScriptTests.cs
git commit -m "Open the custom window from the custom ticket item script"
```

(The spreadsheet edit is external to git; note it in the commit body or PR description.)

---

## Invariant-to-test matrix

| Invariant | Proved by |
| --- | --- |
| `/custom` behaviour unchanged by the refactor | unmodified `Part3CustomTests` green (Task 1) |
| `CWG` sent only for a valid look-slot drop | `CWS valid stats only → no CWG` (Task 3) |
| Same-type rule with 1H/2H exception | `CWS both valid different types` + `1H + 2H` (Task 3) |
| Look/stats ids distinct and not the ticket slot | `CWS look id == stats id`, `CWS id == ticket slot` (Task 3) |
| A capped at 200 server-side | `CWC a = 201` refused, `a = 200` succeeds (Task 3) |
| Name sanitization (trim/commas/255/empty) | `CustomItemTests.SanitizeName` + `CWC` comma/empty-name tests (Tasks 1, 3) |
| Fresh re-validation at create; failure consumes nothing | `CWC look item moved`, `CWC a=201` nothing-consumed asserts (Task 3) |
| Optimistic capacity: full inventory allowed when consumes free slots | `Optimistic capacity` test (Task 3) |
| Ticket consumed only on successful create | `CustomTicketScriptTests` (kept on use) + `CWC` happy-path ticket-emptied assert (Tasks 3, 4) |
| Result item fields (stats/look/RGBA/name/description) | `CWC` happy-path field asserts + `CustomItemTests.BuildCustomItem` (Tasks 1, 3) |
| `Item.Custom` destroy→ripped-ticket still works | `Description` prefix asserted in happy path (`Goose/Item.cs:129`) |
| One window per player | `CustomTicketScriptTests` second-use refusal (Task 4) |

## Design alignment

- Packet strings exactly as the design doc: `CWS<l>,<s>`, `CWG<id>,<pose>`, `CWC<l>,<s>,<r>,<g>,<b>,<a>,<name>`.
- Window: frame 28, title `Custom`, buttons `0,1,0,0,1`.
- Error messages reuse the `/custom` wording (`"Items missing for customisation"`, `"Items to be customised must be equipment and must be visible items."`, `"Items to be customised must be of the same equipment type."`) so client-side UX matches.
- `pose` = look item `BodyState` (design: "pose = body state").
- Create is driven by `CWC` only; the OK button in the `MKW` flags is chrome the client maps to `CWC` (no `WBC` handling needed server-side).

## Out of scope (Part 2, client)

Everything in `Goose2ClientGodot`: `WindowFrames.Custom`, the window UI, preview, colour picker, packets. The protocol strings above are the contract.
