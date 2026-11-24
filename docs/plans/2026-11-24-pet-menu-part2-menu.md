# Pet Menu Part 2 — Pet Menu Implementation Plan

**Goal:** A nested option-list pet menu (list → pet actions → amount / release confirm), opened by
`/pets` and a self-cast `PetMenu` spell effect.

**Architecture:** A static `Goose/PetMenu.cs` builds `OptionListWindow`s whose callbacks capture a
`PetID`, re-resolve it via `PetOperations.Find`, call Part 1's `PetOperations`, show the result as an
Info notice and reopen the relevant window.

**Tech Stack:** C# / .NET 10, xUnit, `TestWorldFixture`.

Design: `docs/plans/2026-11-24-pet-menu-design.md`. Part 2 of 4 — requires Part 1.

---

## APIs verified

| API | Location |
|---|---|
| `OptionListWindow(player, world, title, lines, onLineClicked, npc, startPage, lineGraphics, openingLine, lineItems, onBack)` — adds itself to `player.Windows` and sends | `Goose/OptionListWindow.cs:12-30` |
| Line click closes the window **before** invoking the callback with the absolute index | `Goose/OptionListWindow.cs:98-106` |
| Back on page 0 with `onBack` closes then calls `onBack` | `Goose/OptionListWindow.cs:76-86` |
| WBC line-click routing | `Goose/Events/WindowButtonClickEvent.cs:57` |
| `AmountWindow(player, world, title, prompt, icon, min, max, defaultValue, onConfirm)` — throws if `min > max` | `Goose/AmountWindow.cs:14-21` |
| `AmountWindow.Submit` closes then calls `onConfirm` | `Goose/AmountWindow.cs:44-55` |
| Close-existing-option-lists pattern | `Goose/Commands/TitleCommand.cs:11-12` |
| `[Command(...)]` attribute + `BaseCommand.Execute(CommandContext)` | `Goose/Commands/TitleCommand.cs:3-6` |
| `EffectTypes` enum (`Script = 21`, `Revive = 22`) | `Goose/SpellEffect.cs:69-93` |
| Effect dispatch switch | `Goose/SpellEffect.cs:1075-1113` |
| Self/Group spells force `target = this` | `Goose/Player.cs:2416-2422` |
| `Pet` is a `Player` subclass | `Goose/Pet.cs:13` |
| `P.Notice(NoticeType.Info, msg)` | `Goose/Packets.cs:43` |
| Part 1: `PetOperations.Find/TrySpawn/TryDespawn/SetMode/ShowInfo/Release/BuyVita/BuyDamage/MaxAffordable*/NextVitaCost/NextDamageCost/IsMaxLevel`, `Player.MaxPets(settings)` | `docs/plans/2026-11-24-pet-menu-part1-foundation.md` |
| `Pet.Modes` (`Neutral, Follow, Defend, Attack`) | `Goose/Pet.cs:45-51` |
| `Pet.NextRespawnTime` (unix seconds) | `Goose/Pet.cs:63` |
| Test: windows are inspectable via `player.Windows`; `OptionListWindow.LineClicked(line, 0, player, world)` drives clicks | `Goose/OptionListWindow.cs:98` |

Threading: all entry points (commands, spell cast, WBC, AMT) run as events on the game loop
(`World.EventHandler.Update`), so no marshalling is needed.

---

### Task 1: Root window and pet line format

**Files:**
- Create: `Goose/PetMenu.cs`
- Test: `Goose.Tests/PetMenuTests.cs`

```csharp
public static class PetMenu
{
    public static void Open(Player owner, GameWorld world);
    public static string PetLine(Pet pet);               // reused by the Part 4 name tag
    internal static void OpenPet(Player owner, int petId, GameWorld world);
}
```

`PetLine(pet)`: `$"{pet.Name}  Lv {pet.Level}{status}"` where status is
- alive, Attack → `"  [Out - Attacking]"`
- alive, other → `$"  [Out - {pet.Mode}]"`
- not alive, cooldown remaining `n > 0` → `$"  [Cooldown {n}s]"`
- otherwise `""`.

`Open`:
1. Close the owner's non-NPC `OptionList` windows (pattern `TitleCommand.cs:11-12`).
2. Title `$"Pets ({owner.Pets.Count}/{owner.MaxPets(world.Settings)})"`.
3. Snapshot `var ids = owner.Pets.Select(p => p.PetID).ToList()`; lines = `PetLine` per pet; click
   → `OpenPet(owner, ids[index], world)`.
4. No pets → single line `"You have no pets. Tame one!"` whose click does nothing.

`OpenPet` is a stub in this task (opens nothing) — filled in Task 2.

**Tests:** title counts; three line statuses (out/follow, attacking, cooldown, idle);
empty-state line; opening twice leaves exactly one non-NPC OptionList window.

Commit `feat(pets): pet menu root window`.

---

### Task 2: Pet window — details, spawn/despawn, modes, stale ids

**Files:** Modify `Goose/PetMenu.cs`; test `Goose.Tests/PetMenuTests.cs`.

`OpenPet(owner, petId, world)`:
- `var pet = PetOperations.Find(owner, petId)`; null → Notice `"That pet is no longer yours."`
  and `Open(owner, world)`.
- `OptionListWindow` title `pet.Name`, `onBack: (p, w) => Open(p, w)`, exactly 8 lines:

| # | Line | Click |
|---|---|---|
| 0 | `Details` | `PetOperations.ShowInfo` (then reopen pet window) |
| 1 | `Despawn` if alive else `Spawn` | `TryDespawn` / `TrySpawn` |
| 2–4 | `Mode: Neutral` / `Mode: Follow` / `Mode: Defend`, prefixed `"* "` when alive and current | `SetMode` |
| 5 | `IsMaxLevel ? $"Buy HP (next: {NextVitaCost:N0} exp)" : "Buy HP (max level only)"` | Task 3 |
| 6 | same for `Buy Damage` with `NextDamageCost` | Task 3 |
| 7 | `Release` | Task 4 |

Every click handler re-resolves the pet by id first (null → notice + root). Results are sent as
`P.Notice(NoticeType.Info, r.Message)`, then `OpenPet` is called again so labels reflect new state.
Format numbers with `CultureInfo.InvariantCulture`.

**Tests:**
- 8 lines; label `Spawn` when not out and `Despawn` after clicking it (map `CanSpawnPets = true`).
- Clicking a mode line on a non-spawned pet → notice containing `"isn't out"`, mode unchanged.
- Mode marker: spawned pet in Follow → line 3 starts with `"* "`, lines 2 and 4 don't.
- **Adversarial stale id:** open pet window, remove the pet from `owner.Pets` directly, click
  `Spawn` → pet not spawned, notice sent, the open window is the root (title starts `"Pets ("`).
  Fails if the callback captured the `Pet` object instead of the id.
- Back from pet window reopens root.

Commit `feat(pets): pet menu actions for spawn, despawn and modes`.

---

### Task 3: Buy HP / Damage via amount window

**Files:** Modify `Goose/PetMenu.cs`; test `Goose.Tests/PetMenuTests.cs`.

Click on line 5/6:
1. Re-resolve pet.
2. `long max = MaxAffordableVita/Damage(pet, settings)`; `max == 0` → notice
   (`IsMaxLevel` false → `"{name} must be max level to buy stats."`, else
   `"{name} doesn't have enough experience."`), reopen pet window, stop. This also protects
   `AmountWindow`'s `min > max` throw.
3. `new AmountWindow(owner, world, "Buy HP", $"How many HP buys for {name}? (max {max})",
   (0,0,0,0,0,0), 1, max, 1, (n, p, w) => { re-resolve; r = BuyVita(pet, n, w); notice; OpenPet })`.

`BuyVita` re-checks affordability itself, so exp changing between open and confirm is safe.

**Tests:** confirm via `window.Submit(2, player, world)` → stats/exp change, notice
`"Bought ..."`, pet window reopened. Not max level → no `AmountWindow` in `player.Windows`.
Pet released between open and confirm → notice, nothing bought, no exception.

Commit `feat(pets): pet menu stat buys`.

---

### Task 4: Release confirmation

**Files:** Modify `Goose/PetMenu.cs`; test `Goose.Tests/PetMenuTests.cs`.

Line 7 → `OptionListWindow` title `$"Release {name}?"`, lines
`$"Yes, release {name} forever"`, `$"No, keep {name}"`, `onBack` → `OpenPet`.
Yes → re-resolve, `PetOperations.Release`, notice, `Open` root. No → `OpenPet`.

**Tests:** Yes removes pet and shows root; No keeps pet and shows pet window; **adversarial:**
clicking Yes after the pet was already released elsewhere doesn't throw and doesn't double-save
(pet absent → notice path).

Commit `feat(pets): pet menu release with confirmation`.

---

### Task 5: `/pets` command and `PetMenu` spell effect

**Mutation impact:** `EffectTypes` values are persisted in `spell_effects.effect_type`
(`Goose/SpellHandler.cs:54`) — append only; never renumber.

**Files:**
- Create: `Goose/Commands/PetsCommand.cs`
- Modify: `Goose/SpellEffect.cs:69-93` (add `PetMenu = 23` after `Revive`), dispatch
  `Goose/SpellEffect.cs:1105-1110`
- Test: `Goose.Tests/PetMenuTests.cs`

```csharp
[Command("/pets", Section = "Pets", Help = "Open the pet menu.")]
public sealed class PetsCommand : BaseCommand
{
    public void Execute(CommandContext ctx) => PetMenu.Open(ctx.Player, ctx.World);
}
```

Dispatch arm: `EffectTypes.PetMenu when caster is Player owner && caster is not Pet => this.CastPetMenuSpell(owner, world)`
which calls `PetMenu.Open` and returns true.

**Tests:** `fixture.RunCommand(player, "/pets")` opens root; `new SpellEffect { EffectType = PetMenu }.CastSpell(player, player, world)`
returns true and opens root; casting with a `Pet` caster returns false and opens nothing.

Commit `feat(pets): /pets command and pet menu spell effect`.

---

## Final check

`dotnet test Goose.sln` → all pass.

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Callbacks never act on a pet no longer owned | stale-id tests (Tasks 2–4) |
| Actions on wrong state only notify | mode-on-unspawned test |
| AmountWindow never opened with max 0 | not-max-level buy test |
| Effect type numbering append-only | `PetMenu = 23` explicit value; dispatch test |
| Pet casters can't open menus | Pet-caster spell test |
