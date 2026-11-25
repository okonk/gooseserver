# Pet Menu Part 1 — Foundation Implementation Plan

**Goal:** Add per-player pet slots, an active pet limit, shared pet operations, pet name rules and a
deferred-consume inventory helper, with existing pet commands and mode spells delegating to them.

**Architecture:** A new static `PetOperations` class owns every pet action's rules and returns a
`PetResult` (no packets sent); commands, mode spells and (Part 2) the pet menu decide how to show
the message. Pet slots are a `pet_slots_bonus` entry in `Player.Properties` (no schema change).

**Tech Stack:** C# / .NET 10, xUnit, `TestWorldFixture` (`TestSupport/TestWorldFixture.cs`).

Design: `docs/plans/2026-11-24-pet-menu-design.md`. Part 1 of 4.

AGENTS.md: no new comments/doc strings unless the "why" is non-obvious.

---

## APIs verified

| API | Location |
|---|---|
| `GooseSettings.PetCountLimit` | `Goose/GooseSettings.cs:142`, json `Goose/GooseSettings.json:150` |
| Missing settings default silently (warn only) | `Goose/GooseSettingsLoader.cs:58-70` |
| `Player.Properties` (`PropertiesDictionary`, persisted in `player_properties`) | `Goose/Player.cs:481-482`, save `Goose/Player.cs:1022` |
| `PropertiesDictionary.GetProperty<T>(key, default)` (numeric conversion) | `Goose/PropertiesDictionary.cs:43` |
| `Player.Pets` / `AddPet` | `Goose/Player.cs:440`, `Goose/Player.cs:2955` |
| Tame limit check (`==`) | `Goose/SpellEffect.cs:945-949` |
| Mode spells (Defend/Destroy/Follow/Neutral) | `Goose/SpellEffect.cs:975-1040` |
| Spell dispatch for pet effects | `Goose/SpellEffect.cs:1105-1110` |
| `Pet.Spawn` (no-op if alive; resets Mode to Neutral) | `Goose/Pet.cs:464-508` |
| `Pet.Destroy` / `DestroyWithCooldown` (`CooldownSeconds = 120`, saves) | `Goose/Pet.cs:514-560`, const `Goose/Pet.cs:16` |
| `Pet.IsAlive => Map is not null` | `Goose/Pet.cs:88` |
| `Pet.SaveToDatabase` (enqueues transaction) | `Goose/Pet.cs:300-305` |
| `Map.CanSpawnPets` | used `Goose/Commands/PetSpawnCommand.cs:9` |
| Existing command bodies | `Goose/Commands/PetSpawnCommand.cs`, `PetDeleteCommand.cs`, `PetVitaCommand.cs`, `PetDamageCommand.cs`, `PetInfoCommand.cs` |
| Vita buy must `RemoveStats(BaseStats, world, false)` before and `AddStats(BaseStats, world)` after | `Goose/Commands/PetVitaCommand.cs:41,66` |
| Max-level gate `Class.GetLevel(Level)?.Experience != 0` | `Goose/Commands/PetVitaCommand.cs:39` |
| `ClassLevel.Experience` settable | `Goose/ClassLevel.cs:14` |
| `Utils.MultiplyAndTruncate` | used `Goose/Commands/PetDamageCommand.cs:49` |
| PetInfo window creation (generic `Window`, `Type = PetInfo`, `Data = pet`) | `Goose/Commands/PetInfoCommand.cs:32-48` |
| `Inventory.GetSlot(i)`, `RemoveItem(Item, n, world)` (by reference), `RemoveItem(templateId, n, world)` | `Goose/Inventory.cs:276`, `:622`, `:668` |
| `UseConsumable` removes 1 when script returns true | `Goose/Inventory.cs:567-617` |
| Login name rules (3–16, ASCII letters, two separate messages) | `Goose/Events/LoginEvent.cs:145-157` |
| `P.Notice(NoticeType, msg)` / `P.ServerMessage(msg)` | `Goose/Packets.cs:43`, `:39` |
| Test helpers: `CommandPlayerOn`, `AddBaseMap`, `RunCommand`, `AddBaseItemTemplate`, `SeedClass` | `TestSupport/TestWorldFixture.cs:142,73,180,201,216` |
| Existing pet test helpers `WorldAndPlayer` / `MakePet` | `Goose.Tests/Part2PetsTests.cs:10-32` |

`TestWorldFixture` does **not** set `PetCountLimit` (defaults 0); tests that tame must configure it
via `new TestWorldFixture(s => s.PetCountLimit = 5)`. New settings get property initialisers so the
fixture gets sane defaults.

---

### Task 0: Settings

**Files:**
- Modify: `Goose/GooseSettings.cs:142` (beside `PetCountLimit`)
- Modify: `Goose/GooseSettings.json:150`

Add:

```csharp
public int PetSlotsMax { get; set; } = 30;
public int ActivePetLimit { get; set; } = 3;
```

json: `"PetSlotsMax": 30,` and `"ActivePetLimit": 3,` after `"PetCountLimit": 5,`.

No test of its own (covered by Tasks 2 and 4). Build: `dotnet build Goose.sln` → succeeds.
Commit with Task 1.

---

### Task 1: `NameRules`

**Files:**
- Create: `Goose/NameRules.cs`
- Modify: `Goose/Events/LoginEvent.cs:145-157`
- Test: `Goose.Tests/NameRulesTests.cs`

API:

```csharp
public static class NameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 16;
    public static bool IsLetters(string name);          // non-empty, every char A-Z / a-z
    public static bool IsValidPlayerName(string name);  // length in range && IsLetters
    public static bool IsValidPetName(string name);     // see below
}
```

`IsValidPetName(name)`: caller passes the already-trimmed text; returns true when length is
3–16, first and last chars are letters, every char is an ASCII letter or `' '`, and there is no
`"  "` (double space). Note `MakePetCharacter` is comma-delimited (`Goose/Packets.cs:250`), so
the letter/space whitelist is also the wire-safety guarantee.

`LoginEvent`: replace the literal `3`/`16` with `NameRules.MinLength`/`MaxLength` and the `All(...)`
lambda with `NameRules.IsLetters(name)`. **Keep both existing messages and their order** (length
first, then letters).

**Step 1: failing tests** (`[Theory]` tables):

- Player valid: `"Bob"`, `"Abcdefghijklmnop"` (16). Invalid: `"Bo"`, 17 letters, `"Bob1"`,
  `"Bob Smith"`, `"Bób"` (non-ASCII — adversarial: `char.IsLetter` would wrongly accept it).
- Pet valid: `"Rex"`, `"Sir Fluff"`, `"A B C"`, 16 chars with spaces. Invalid: `"Re"`, 17 chars,
  `"Sir  Fluff"`, `" Rex"`, `"Rex "`, `"Rex2"`, `"Rex,Jr"`, `"Fífí"`, `""`.

**Step 2:** `dotnet test Goose.Tests --filter NameRulesTests` → fails (type missing).

**Step 3:** implement; update `LoginEvent`.

**Step 4:** `dotnet test Goose.Tests --filter "NameRulesTests|Login"` → pass.

**Step 5:** commit `feat(pets): settings for pet slots and active limit, shared name rules`.

---

### Task 2: `Player.MaxPets` and pet-slot bonus; tame limit

**Mutation impact:**
- Source of truth: `Player.Properties["pet_slots_bonus"]` (`Goose/Player.cs:482`), persisted with
  the player row (`Goose/Player.cs:1022`). Settings `PetCountLimit` / `PetSlotsMax`.
- Readers: `CastTameSpell` (`Goose/SpellEffect.cs:945`); later Part 2 root title and Part 4 slot item.
- Derived state: `MaxPets` is computed on read; no cache. No derived state found.
- Propagation: setting the property is enough; the next player save persists it.
- Invariants: `MaxPets ≤ PetSlotsMax`; missing key = bonus 0; lowering a limit never removes pets.

**Files:**
- Modify: `Goose/Player.cs` (near `Pets`, line 440)
- Modify: `Goose/SpellEffect.cs:945-949`
- Test: `Goose.Tests/PetSlotsTests.cs`, extend `Goose.Tests/TameSpellTests.cs`

API on `Player`:

```csharp
public const string PetSlotsBonusKey = "pet_slots_bonus";
public int PetSlotsBonus
{
    get => this.Properties.GetProperty<int>(PetSlotsBonusKey, 0);
    set => this.Properties[PetSlotsBonusKey] = value;
}
public int MaxPets(GooseSettings settings) => Math.Min(settings.PetCountLimit + this.PetSlotsBonus, settings.PetSlotsMax);
```

(`MaxPets` takes settings because `Player` has no world reference; match existing style of passing
`world.Settings`.)

Tame: `if (player.Pets.Count >= player.MaxPets(world.Settings))`, message
`"You have reached the maximum amount of pets. Use /pets to release one."`.

**Tests:**
- `MaxPets` = base when no key; base + bonus; clamped at `PetSlotsMax` (base 5, bonus 40, max 30 → 30).
- Tame refused at `MaxPets` with bonus (base 1, bonus 1, 2 pets → refused, no new pet).
- **Adversarial:** player with `Pets.Count` *above* `MaxPets` (base 1, 3 pets) is still refused —
  fails on the old `==` check. Use the untameable test's setup
  (`Goose.Tests/TameSpellTests.cs:8-20`) with a tameable NPC; refusal happens before the success
  roll so randomness doesn't matter.

Commands: `dotnet test Goose.Tests --filter "PetSlotsTests|TameSpellTests"`.

Commit `feat(pets): per-player pet slot bonus`.

---

### Task 3: `Inventory.TryConsumeItem`

**Mutation impact:**
- Source of truth: inventory slots (`Goose/Inventory.cs` `this.inventory[i]`).
- Readers: client via `SendSlot` (called inside both `RemoveItem` overloads), quest icons via
  `RefreshQuestIcons`.
- Propagation: delegate to existing `RemoveItem(Item, n, world)` (`:622`) which sends the slot and
  refreshes quest icons. **Do not** use `RemoveItem(int templateId, ...)` for the fallback: it skips
  `RefreshQuestIcons` on its early-return paths (`:684-695`). Instead find the first slot whose
  `Item.TemplateID == item.TemplateID && Stack >= count` and call `RemoveItem(slot.Item, count, world)`.
- Invariants: returns false and mutates nothing when no stack can supply `count`.

**Files:**
- Modify: `Goose/Inventory.cs` (after `RemoveItem(int,...)`, ~line 708)
- Test: `Goose.Tests/TryConsumeItemTests.cs`

```csharp
public bool TryConsumeItem(Item item, long count, GameWorld world)
```

1. If any inventory slot holds `item` (reference equality) with `Stack >= count` →
   `RemoveItem(item, count, world)`; return true.
2. Else first slot (1..InventorySize) with same `TemplateID` and `Stack >= count` → remove from it; true.
3. Else false.

**Tests** (items via `AddBaseItemTemplate(..., OneTime, t => t.StackSize = 10)`, `Item.LoadFromTemplate`,
`World.ItemHandler.AddAndAssignId`, `Inventory.AddItem` — pattern in
`Goose.IntegrationTests/HairDyePotionScriptTests.cs:9-23`):
- exact stack of 3 → 2 remain in that slot.
- exact item gone (removed via `RemoveItem(item, stack)`), another stack of same template in a
  different slot → that stack drops by 1, returns true. **Adversarial:** put the other stack in a
  later slot and an unrelated template in an earlier slot — fails if the fallback ignores template.
- no stacks → false, inventory unchanged.

Commit `feat(inventory): TryConsumeItem with same-template fallback`.

---

### Task 4: `PetOperations` — spawn, despawn, modes

**Mutation impact:**
- Source of truth: `Pet.Map` (alive), `Pet.NextRespawnTime`, `Pet.Mode`, `Pet.Target` (`Goose/Pet.cs`).
- Readers: `PetMoveEvent` (`Goose/Events/PetMoveEvent.cs:12-61`), `PetAttackEvent`,
  `Player.Attacked` defend hook (`Goose/Player.cs:2172`).
- Propagation: `Pet.Spawn` / `DestroyWithCooldown` already broadcast and persist — call them, don't
  reimplement.
- Invariants: spawn never exceeds `ActivePetLimit` via this path; failure leaves pet untouched.

**Files:**
- Create: `Goose/PetOperations.cs`
- Modify: `Goose/Commands/PetSpawnCommand.cs`, `Goose/SpellEffect.cs:975-1040`
- Test: `Goose.Tests/PetOperationsTests.cs`; existing `Goose.Tests/Part2PetsTests.cs` must stay green.

```csharp
public readonly record struct PetResult(bool Ok, string Message);

public static class PetOperations
{
    public static Pet? Find(Player owner, int petId);
    public static int ActiveCount(Player owner);                 // Pets.Count(p => p.IsAlive)
    public static PetResult TrySpawn(Player owner, Pet pet, GameWorld world);
    public static PetResult TryDespawn(Pet pet, GameWorld world);
    public static PetResult SetMode(Pet pet, Pet.Modes mode);    // Neutral/Follow/Defend only
}
```

`TrySpawn` checks, in order, with messages:
1. `!owner.Map.CanSpawnPets` → `"Pets are disabled in this map."`
2. `pet.IsAlive` → `"That pet is already spawned."`
3. cooldown → `"You must wait {n} seconds to spawn this pet."` (same computation as
   `PetSpawnCommand.cs:44-50`)
4. `ActiveCount(owner) >= world.Settings.ActivePetLimit` →
   `"You can only have {limit} pets out at once."`
5. `pet.Spawn(world)` → `Ok`, message `"{pet.Name} has been summoned."`

`TryDespawn`: not alive → `"{pet.Name} isn't out."`; else `DestroyWithCooldown` →
`"{pet.Name} has been dismissed."`.

`SetMode`: `mode == Attack` → `ArgumentOutOfRangeException` (programmer error); not alive →
`"{pet.Name} isn't out."`; else `Target = null; Mode = mode;` → `"{pet.Name} is now in {mode} mode."`.

Callers:
- `PetSpawnCommand`: keep id validation + lookup messages; then
  `var r = PetOperations.TrySpawn(...); if (!r.Ok) Send(ServerMessage(r.Message));`
  (success stays silent as today — `Part2PetsTests.cs:53-67` asserts no error message only).
- Mode spells: keep the `pet.Owner != caster` checks; body becomes `return PetOperations.SetMode(pet, X).Ok;`.
  Destroy spell → `return PetOperations.TryDespawn(pet, world).Ok;`.

**Tests:**
- `TrySpawn` refusals: map disabled, already out, cooldown, active limit (settings
  `ActivePetLimit = 2`, 2 spawned, third refused, third `IsAlive == false`, message contains `"2 pets"`).
- **Adversarial:** pets that are *not* alive don't count — 5 pets, 1 alive, limit 2 → spawn succeeds.
- Success: pet alive on owner's map.
- `TryDespawn` alive → not alive, `NextRespawnTime` ≈ now+120; not alive → message, cooldown unchanged.
- `SetMode` not alive → not ok, mode unchanged; alive with Target set → target cleared, mode set.
- `/petspawn` at active limit sends the limit message (command-level regression).

Commands: `dotnet test Goose.Tests --filter "PetOperationsTests|Part2PetsTests"`.

Commit `refactor(pets): shared spawn/despawn/mode operations with active pet limit`.

---

### Task 5: `PetOperations` — buys

**Mutation impact:**
- Source of truth: `pet.Experience`, `pet.ExperienceSold`, `pet.BaseStats.HP`, `pet.WeaponDamage`.
- Derived state: `pet.MaxStats` / `MaxHP` derive from `BaseStats` via `RemoveStats`/`AddStats`
  (`Goose/Commands/PetVitaCommand.cs:41,66`). Vita **must** wrap the loop in those calls; damage
  does not touch stats.
- Readers: PetInfo window (`Goose/Window.cs:302-333`), pet combat.
- Invariants: cost formula and integer division unchanged; current HP not lowered
  (`Part2PetsTests.PetVita_does_not_lower_current_hp`).

**Files:**
- Modify: `Goose/PetOperations.cs`, `Goose/Commands/PetVitaCommand.cs`, `Goose/Commands/PetDamageCommand.cs`
- Test: `Goose.Tests/PetOperationsTests.cs`

```csharp
public const int MaxAffordableBuyCap = 100_000;
public static bool IsMaxLevel(Pet pet);                           // Class.GetLevel(Level)?.Experience == 0
public static long NextVitaCost(Pet pet, GooseSettings s);        // formula from PetVitaCommand.cs:48-50
public static long NextDamageCost(Pet pet, GooseSettings s);      // formula from PetDamageCommand.cs:47-49
public static long MaxAffordableVita(Pet pet, GooseSettings s);
public static long MaxAffordableDamage(Pet pet, GooseSettings s);
public static PetResult BuyVita(Pet pet, long buys, GameWorld world);
public static PetResult BuyDamage(Pet pet, long buys, GameWorld world);
```

`IsMaxLevel` returns false when `GetLevel` is null (today's `?.Experience != 0` treats null as
"not max" — preserve).

`MaxAffordable*`: simulate the buy loop on local copies of exp and HP/damage, stop when unaffordable
or at `MaxAffordableBuyCap` (guards a zero cost setting from looping forever). Returns 0 when not max level.

`Buy*`: not max level → `"{pet.Name} must be max level to buy stats."`; zero bought →
`"{pet.Name} doesn't have enough experience."`; else existing messages
`"Bought {bought} hp for {exp} experience."` / `"Bought {bought} damage for {exp} experience."`.

Commands keep their `buys <= 0` / id validation and send `ServerMessage(r.Message)` for every result
(the two new failure messages replace silent returns; no test asserts silence —
`Part2PetsTests.cs:282-410` checked).

**Tests:**
- Agreement (**adversarial**): for pets with exp values around cost boundaries, buying
  `MaxAffordableVita(pet)` succeeds and leaves `Experience < NextVitaCost(pet)`; same for damage.
  Fails if the simulation diverges from the real loop (e.g. forgets the rising cost).
- Not max level (`fixture.World.ClassHandler.GetClass(0)!.GetLevel(1)!.Experience = 100`) → 0
  affordable, buy refused, exp unchanged.
- Cap: settings `PetVitaCost = 0` → `MaxAffordableVita == MaxAffordableBuyCap`.
- Existing `PetVita_*` / `PetDamage_*` tests stay green.

Commit `refactor(pets): shared stat buys with max-affordable calculation`.

---

### Task 6: `PetOperations` — release and info window

**Mutation impact:**
- Source of truth: `owner.Pets` list, `pet.Delete` flag, `pets` table row.
- Propagation sequence (from `Goose/Commands/PetDeleteCommand.cs:32-35`): `pet.Destroy(world)` →
  `owner.Pets.Remove(pet)` → `pet.Delete = true` → `pet.SaveToDatabase(world)`. Then close any open
  PetInfo windows for it: `foreach (w in owner.Windows.Where(w => w.Type == PetInfo && w.Data == pet).ToList()) w.Close(owner, world)`
  (`Window.Close` `Goose/Window.cs:348-352`).
- Invariants: released pet is never in `Pets`; save happens once.

**Files:**
- Modify: `Goose/PetOperations.cs`, `Goose/Commands/PetDeleteCommand.cs`, `Goose/Commands/PetInfoCommand.cs`
- Test: `Goose.Tests/PetOperationsTests.cs`

```csharp
public static PetResult Release(Player owner, Pet pet, GameWorld world);   // "{name} has been released."
public static void ShowInfo(Player owner, Pet pet, GameWorld world);       // body of PetInfoCommand.cs:32-48
```

`PetDeleteCommand` calls `Release` (success stays silent as today). `PetInfoCommand` calls `ShowInfo`.

**Tests:** release of a spawned pet → not alive, removed, `Delete == true`; an open PetInfo window for
it is removed from `owner.Windows` and a `CLW` packet was sent. Existing `PetDelete_*` / `PetInfo_*`
tests green.

Commit `refactor(pets): shared release and info window`.

---

## Final check

`dotnet test Goose.sln` → all pass (baseline 542 integration + 2939 unit, plus new).

## Invariant-to-test matrix

| Invariant | Proved by |
|---|---|
| Tame refused at or above `MaxPets` | `TameSpellTests` above-limit case (fails on `==`) |
| `MaxPets` ≤ `PetSlotsMax` | `PetSlotsTests` clamp case |
| Only alive pets count for active limit | `PetOperationsTests` 5 pets / 1 alive |
| Max-affordable matches the real buy loop | agreement tests |
| Fallback consume respects template | `TryConsumeItemTests` adversarial slot order |
| Non-ASCII names rejected | `NameRulesTests` `"Bób"` / `"Fífí"` |
| Command behaviour unchanged | existing `Part2PetsTests` |
