# Stylist (Part 1 of 2: Server) Implementation Plan

**Goal:** A `stylist: true` NPC opens a Stylist window; the client's `STC<hair>,<face>` buys a
hair/face change for 2,000 gold per changed slot.

**Architecture:** `StylistWindow : Window` modelled on `EnchantWindow` (NPC-bound, opened from
`PlayerRightClickEvent`, range-checked on every request). One new client→server packet
`STC`, handled by `StylistWindowChangeEvent`. State change is `Player.HairID` / `FaceID` /
`Gold`, broadcast with `P.UpdateCharacter`. New log type 26 `Stylist`.

**Tech Stack:** C# / .NET 10, xUnit, `TestSupport/TestWorldFixture`.

Design: `docs/plans/2026-11-14-stylist-design.md`. Part 2 (client) builds on the wire protocol
here: `MKW<id>,34,Stylist,0,1,0,0,1,<npcLoginId>,…` then `ENW<id>`; client sends
`STC<hair>,<face>`.

Follow `AGENTS.md`: no new comments or doc strings unless the "why" is non-obvious.

---

## APIs verified

| API | Where |
|---|---|
| `Window` base: `ID`, `Frame`, `Type`, `Title`, `Buttons`, `NPC`, `Clicked(...)`, `Populate(...)` (virtual; no-op for unlisted types), `SendCreate`, `Close` | `Goose/Window.cs:12-351` (`SendCreate` 334, `Close` 345) |
| `WindowFrames` ends `Enchant = 33`; `WindowTypes` ends `Enchant` | `Goose/Window.cs:25-59`, `68-93` |
| `EnchantWindow` pattern: ctor sets id/frame/type/title/buttons/NPC, `player.Windows.Add`, `SendCreate`; `Open` closes existing via `FindOpen(player)?.Close(...)` | `Goose/EnchantWindow.cs:16-38` |
| Range check `Map.InRange(ICharacter, ICharacter)` (`RANGE_X = 24`, `RANGE_Y = 16`) | `Goose/Map.cs:156`, `17-18` |
| Right-click dispatch, `enchantTable` branch | `Goose/Events/PlayerRightClickEvent.cs:63-67` |
| Event registration list (`("EWC", Open(typeof(EnchantWindowCreateEvent)))`) | `Goose/EventHandler.cs:157-158` |
| Event parse pattern (`Player.State == Ready`, `Substring(3).Split(',')`, try/Convert) | `Goose/Events/EnchantWindowCreateEvent.cs` |
| `Player.Gold` (long), `RemoveGold(long, GameWorld)` — returns silently if amount > Gold; sends `P.StatusInfo` and refreshes quest icons | `Goose/Player.cs:284`, `1765-1772` |
| `HairID`, `FaceID` | `Goose/Player.cs:232`, `268` |
| Persistence: `player_gold`, `face_id`, `hair_id` all in the same `BuildUpdateQuery` | `Goose/Player.cs:1200`, `1216`, `1240-1241` |
| `P.UpdateCharacter(player)` → `CHP…`; `P.Notice(NoticeType.Info, msg)` → `NTF…` | `Goose/Packets.cs:155`, `43` |
| Broadcast pattern: send to player + `player.Map.GetPlayersInRange(player)` | `Goose/Data/Illutia/Scripts/Item/HairCutItem.csx`, `Goose/Map.cs:167` |
| `LogHandler.Log(Log.Types, Player, string, int otherid = 0)`; tests read `fixture.World.LogHandler.Pending` | `Goose/LogHandler.cs:34`, `Goose.Tests/EnchantWindowConfirmTests.cs:64` |
| `Log.Types` ends `Enchant` (25) before GM block | `Goose/Log.cs:37` |
| Log registry / formatter / tests touched for a new type | commit `9c0cc8a` (`LogEventRegistry.cs:34`, `LogFormatter.cs:91,131`, `LogEventDescriptorTests.cs`, `LogFormatterTests.cs:377`) |
| Settings: `HairdyeCommandCost` beside `HairDyePotionId`; JSON at `"HairdyeCommandCost": 5000` | `Goose/GooseSettings.cs:151`, `Goose/GooseSettings.json:159` |
| Fixture: `TestWorldFixture(configure)`, `AddBaseMap`, `CommandPlayerOn`, `AddOnlinePlayer`, `AddNpcTemplate`, `SpawnNpc(..., properties:)`, `RunCommand`, `CapturingPlayer.Sent` | `TestSupport/TestWorldFixture.cs:24,116,135-190`; usage `Goose.Tests/EnchantWindowTests.cs:15-48` |
| Second player in range for broadcast: `map.Players.Add(other)` | `Goose.Tests/Part3GmAdminTests.cs:191` |

Resolved design "to verify" items:
- **Persistence:** gold and hair/face are written by the same `BuildUpdateQuery`; no extra
  save is needed.
- **Gold refresh:** `RemoveGold` sends `P.StatusInfo`, so the client's gold stays current.

Persistence strategy: **no schema change** (existing `hair_id`, `face_id`, `player_gold`
columns; `logs.log_type` is an int).

---

### Task 0: Settings, enums, log type

**Files:**
- Modify: `Goose/GooseSettings.cs:151` (add after `HairDyePotionId`)
- Modify: `Goose/GooseSettings.json:159` (add after `HairdyeCommandCost`)
- Modify: `Goose/Window.cs` (`WindowFrames`, `WindowTypes`)
- Modify: `Goose/Log.cs:37`, `Goose/Logs/LogEventRegistry.cs:34`, `Goose/Logs/LogFormatter.cs:91,131`
- Test: `Goose.Tests/LogEventDescriptorTests.cs`, `Goose.Tests/LogFormatterTests.cs`

Add:

```csharp
public int StylistChangeCost { get; set; } = 2000;
public int StylistMaxHairID { get; set; } = 46;
public int StylistMaxFaceID { get; set; } = 18;
```

Defaults on the properties so `TestWorldFixture` (which builds `new GooseSettings { … }`) gets
them. JSON: `"StylistChangeCost": 2000, "StylistMaxHairID": 46, "StylistMaxFaceID": 18`.

`WindowFrames.Stylist = 34`; `WindowTypes.Stylist` (after `Enchant`); `Log.Types.Stylist`
(after `Enchant`, value 26).

Log text format (written in Task 1): `Stylist: hair 33 -> 12 face 2 -> 2 cost 2000`.
Registry: `new(26, "Stylist", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused)`.
Formatter summary: `26 => Stylist(c)` producing
`"{Player(c)} changed hair 33 -> 12, face 2 -> 2 for 2000 gold"`; any token mismatch →
`Fallback(c)` (same strictness as `Enchant`). Related-column switch: no entry (falls to
`_ => null`).

**Step 1: failing tests**
- `LogEventDescriptorTests`: add `[InlineData(26, "Stylist", LogEventGroup.ItemsEconomy, LogOtherIdKind.Unused)]`;
  change the unknown-id `[InlineData(26)]` to `[InlineData(27)]`.
- `LogFormatterTests`: summary theory rows
  - `"Stylist: hair 33 -> 12 face 2 -> 2 cost 2000"` → `"Alice changed hair 33 -> 12, face 2 -> 2 for 2000 gold"`
  - adversarial: `"Stylist: hair x -> 12 face 2 -> 2 cost 2000"` and a truncated
    `"Stylist: hair 33 -> 12"` → the existing fallback output (copy the expected string from an
    existing fallback row).

**Step 2: red** — `dotnet test Goose.Tests --filter "FullyQualifiedName~LogEventDescriptorTests|FullyQualifiedName~LogFormatterTests"`
fails (26 unknown).

**Step 3: implement** settings, enums, registry, formatter.

**Step 4: green** — same filter passes; `dotnet build` clean.

**Step 5: commit** `feat(stylist): settings, window frame and log type`

---

### Task 1: `StylistWindow` open + change

**Files:**
- Create: `Goose/StylistWindow.cs`
- Modify: `Goose/Events/PlayerRightClickEvent.cs:63-67`
- Test: `Goose.Tests/StylistWindowTests.cs`

**Mutation impact:**
- Source of truth changed: `Player.HairID` (`Player.cs:232`), `Player.FaceID` (`:268`),
  `Player.Gold` (`:284`); `player.Windows` (window registry).
- Important readers: `P.UpdateCharacter` / `P.MakeCharacter` (appearance packets to self and
  others), `P.StatusInfo` (gold), `BuildUpdateQuery` (`:1200`, persistence), any open
  `StylistWindow` (cost is recomputed from current values each request).
- Derived/cached state: none server-side. Clients cache appearance — updated only by the
  `CHP` broadcast below. Players entering range later get the new look from normal
  `MKC` (reads `HairID`/`FaceID` directly).
- Required propagation sequence (all on the game thread, inside the event):
  1. Validate (window, range, ids, cost > 0, gold ≥ cost) — any failure returns with **no**
     mutation.
  2. `player.RemoveGold(cost, world)` (sends `StatusInfo`).
  3. `player.HairID = hair; player.FaceID = face;`
  4. `string packet = P.UpdateCharacter(player);` send to `player` and each of
     `player.Map.GetPlayersInRange(player)`.
  5. `P.Notice(NoticeType.Info, "Your new look cost " + cost + " gold.")`.
  6. `world.LogHandler.Log(Log.Types.Stylist, player, "Stylist: hair {old} -> {new} face {old} -> {new} cost {cost}")`.
  Persistence happens on the normal player save (`BuildUpdateQuery`).
- Invariants:
  - Failure leaves `HairID`, `FaceID`, `Gold` unchanged and sends no `CHP`.
  - Charged amount = `StylistChangeCost × changed slots`, computed server-side.
  - Hair colour (`HairR/G/B/A`) unchanged.
- Observable proof: tests assert final `HairID`/`FaceID`/`Gold`, presence/absence of `CHP` for
  self and a nearby player, notice text, and the log entry.

**Shape:**

```csharp
public class StylistWindow : Window
{
    public StylistWindow(GameWorld world, Player player, NPC npc)  // as EnchantWindow ctor:
        // ID = ++player.LastWindowID; Frame = WindowFrames.Stylist; Type = WindowTypes.Stylist;
        // Title = "Stylist"; Buttons = "0,1,0,0,1"; NPC = npc; player.Windows.Add(this); SendCreate
    public static void Open(GameWorld world, Player player, NPC npc);
    public static StylistWindow? FindOpen(Player player);
    public override void Clicked(...);   // Exit/Close → player.Windows.Remove(this)
    public override void Populate(Player player, GameWorld world) { }
    public static void Change(GameWorld world, Player player, int hair, int face);
}
```

`Change` order (matches design): no window → return; `!Map.InRange(player, window.NPC!)` →
notice `"You are too far from the stylist."`; ids outside `1..StylistMaxHairID` /
`1..StylistMaxFaceID` → return silently; `cost == 0` → notice `"Nothing changed."`;
`player.Gold < cost` → notice `"You need " + cost + " gold."`; then the propagation sequence.

Right-click: after the `enchantTable` branch,
`if (npc.Properties.GetProperty("stylist", false)) { StylistWindow.Open(world, this.Player, npc); return; }`.

Task 1 tests call `StylistWindow.Change(fixture.World, player, h, f)` directly; Task 2 adds the
packet.

**Step 1: failing tests** (`StylistWindowTests`, fixture as `EnchantWindowTests.Setup` with an
Ambient NPC carrying `["stylist"] = true`; player `HairID = 33, FaceID = 2, Gold = 10000`)

| Invariant | Test |
|---|---|
| Right-click opens frame-34 window bound to NPC | `RightClick_opens_stylist_window` — asserts title/frame/type/buttons/NPC and `MKW{id},34,Stylist,0,1,0,0,1,{npc.LoginID},` + `ENW{id}` |
| Re-open replaces | `RightClick_second_open_replaces_first` — single window, `CLW{first.ID}` sent |
| Hair only −2,000 | `Change_hair_only_costs_one_slot` — Gold 8000, HairID 12, FaceID 2 |
| Face only −2,000 | `Change_face_only_costs_one_slot` |
| Both −4,000 | `Change_both_costs_two_slots` — also asserts `HairR/G/B/A` unchanged |
| No change is free | `Change_nothing_is_free_and_sends_no_update` — gold same, notice "Nothing changed.", no `CHP` |
| Broadcast | `Change_broadcasts_update_to_self_and_nearby` — second `CapturingPlayer` added via `map.Players.Add`, both receive a `CHP{player.LoginID},` packet |
| **Adversarial:** gold exactly short | `Change_with_one_gold_short_changes_nothing` — Gold 1999, hair change → unchanged, notice "You need 2000 gold.", no `CHP` (fails if implementation relies on `RemoveGold`'s silent guard and still applies the change) |
| **Adversarial:** both changed, enough for one | `Change_both_with_gold_for_one_changes_nothing` — Gold 3000 → no partial change |
| Out of range | `Change_out_of_range_notices_and_changes_nothing` — `player.MapX = 30` |
| No window | `Change_without_window_is_ignored` |
| Bad ids | `[Theory] Change_with_out_of_range_ids_is_ignored` — (0,2), (47,2), (33,0), (33,19), (-1,2); no gold, no notice, no `CHP` |
| Log | `Change_logs_old_and_new_ids_and_cost` — `Log.Types.Stylist` text `"Stylist: hair 33 -> 12 face 2 -> 5 cost 4000"` |

**Step 2: red** — `dotnet test Goose.Tests --filter FullyQualifiedName~StylistWindowTests`
fails to compile (no `StylistWindow`).

**Step 3: implement.**

**Step 4: green** — filter passes.

**Step 5: commit** `feat(stylist): stylist window opened from stylist NPCs`

---

### Task 2: `STC` packet

**Files:**
- Create: `Goose/Events/StylistWindowChangeEvent.cs`
- Modify: `Goose/EventHandler.cs:158` (add `("STC", Open(typeof(StylistWindowChangeEvent))),`)
- Test: `Goose.Tests/StylistWindowTests.cs`

Event mirrors `EnchantWindowCreateEvent`: requires `Player.States.Ready`; `Substring(3).Split(',')`;
exactly 2 tokens; `Convert.ToInt32` in try/catch → return on failure; call
`StylistWindow.Change`.

**Step 1: failing tests**
- `Stc_packet_changes_hair_and_face` — `RunCommand(player, "STC12,5")` → HairID 12, FaceID 5, Gold −4000.
- Adversarial `[Theory] Stc_malformed_is_ignored` — `"STC"`, `"STC12"`, `"STC12,5,1"`, `"STCa,5"`,
  `"STC12,"` → no change, no exception.

**Step 2: red** — STC tests fail (unknown packet: `RunCommand` returns false / no change).

**Step 3: implement.**

**Step 4: green** — `dotnet test Goose.Tests --filter FullyQualifiedName~StylistWindowTests`.

**Step 5: commit** `feat(stylist): STC change packet`

---

### Task 3: Full suite

Run `dotnet test Goose.Tests` and `dotnet build Goose.sln`. Expected: all pass (baseline 2,829
plus new tests), no new warnings in touched files. No commit unless fixes are needed.

Manual smoke happens in Part 2 once the client exists. Until then the window can be
exercised by setting `stylist: true` on any NPC's properties.

---

## Design alignment check

- Notices: "You are too far from the stylist.", "Nothing changed.", "You need {cost} gold.",
  "Your new look cost {cost} gold." — match design.
- Silent ignore for no window and invalid ids — matches design.
- Cost per changed slot, settings names and defaults — match design.
- Frame 34, title "Stylist", close-only buttons (`0,1,0,0,1` as enchanting) — match design.
- Hair colour untouched — asserted in `Change_both_costs_two_slots`.
