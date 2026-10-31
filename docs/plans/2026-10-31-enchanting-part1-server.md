# Enchanting Part 1: Server — Implementation Plan

**Goal:** Server-side enchanting: per-element enchant levels on items, derived stats, ` +N`
names, an enchanting-table window driven by `EWS`/`EWP`/`EWC`, attempt resolution with
failure/protection, and logging.

**Architecture:** Enchant state lives in `Item.ItemProperties` (counts per element + max
loss). A static `Enchanting` helper owns all rules (levels, stats, name suffix, attempt
resolution) and is pure over `Item` + `EnchantSettings` + `Random`, so it is tested without a
world. `EnchantWindow` is a thin protocol layer modelled on `CustomWindow`: slots reference
inventory slots, the server stores the last confirmed slot ids + item references, and `EWC`
refuses anything that drifted.

**Tech Stack:** .NET 10, C#, xUnit (`Goose.Tests`), Roslyn `.csx` scripts.

**Design:** `docs/plans/2026-10-31-enchanting-design.md`. Part 2 (client) consumes the
protocol defined in Task 6.

**Build/test note:** the shell exports a stale `MSBuildSDKsPath` (10.0.111). Prefix commands
with `env -u MSBuildSDKsPath`, e.g.
`env -u MSBuildSDKsPath dotnet test Goose.Tests/Goose.Tests.csproj --filter <name>`.
Baseline: 2545 passing.

**Comments:** follow `AGENTS.md` — no new comments/doc strings unless the *why* is
non-obvious (invariant, external quirk, wire format).

---

## APIs verified

- `ItemProperty` enum — `Goose/Item.cs:11-17` (`TitleId, SurnameId, BackpackSlots,
  BackpackColumns`). Persisted as JSON dictionary keys by name
  (`JsonHelper.DatabaseOptions` has `JsonStringEnumConverter`, `Goose/JsonHelper.cs:29`), so
  appending members is safe.
- `Item.ItemProperties` (`[JsonPropertyName("props")]`) — `Goose/Item.cs:76-77`;
  `GetProperty<T>` handles `JsonElement` — `Goose/Item.cs:255-268`; `HasProperty` —
  `Goose/Item.cs:270-273`; `Copy` clones the dictionary — `Goose/Item.cs:225`.
- `Item.RefreshStats()` — `Goose/Item.cs:275-285`: `TotalStats = (Template.BaseStats +
  BaseStats) * StatMultiplier`; `TotalStats` is `[JsonIgnore]` (`Goose/Item.cs:65-66`), so
  it is recomputed on every load.
- `RefreshStats()` callers (all must change signature in Task 1):
  `Goose/ItemHandler.cs:313,337,371`, `Goose/ChestHandler.cs:57`, `Goose/PlayerBank.cs:84`,
  `Goose/Inventory.cs:1273,1302,1408`,
  `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionSurname.csx:51`,
  `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionRarity.csx:17`,
  `Goose/Data/Illutia/Scripts/Item/ItemModifierScript.csx:56`,
  `Goose/Data/Aspereta/Scripts/Item/ItemModifierScript.csx:56`, and
  `Goose.Tests/PacketExtraStatsTests.cs`. Every caller has a `GameWorld` (or
  `this.settings` in `Inventory`).
- `AttributeSet` operators `+`, `*` — `Goose/AttributeSet.cs:102,170`; percent stats are
  fractions (`ItemModifierScript.csx` divides by 100: `SpellDamage += value / 100`).
- `ItemHandler.ResetModifiers(Item)` — `Goose/ItemHandler.cs:329-338`; only production caller
  `Goose/Data/Illutia/Scripts/Global/Dimensions/Commands.csx:120`; tests
  `Goose.Tests/ResetModifiersTests.cs`.
- `ItemHandler.SetModifier(item, kind, modifier, world)` — `Goose/ItemHandler.cs:340-372`
  (strips title/surname by name, reapplies).
- `CustomItem.BuildCustomItem` — `Goose/CustomItem.cs:74-104`; copies only
  `TitleId`/`SurnameId` (`:96-100`); clones `TotalStats` from the stats item (`:80`) and
  sets `Name = name` (`:91`).
- Stats window regen lines — `Goose/Window.cs:256,258`
  (`Math.Round(player.MaxStats.HPPercentRegen * 100, 0)`).
- Item tooltip already sends basis points (`Goose/Packets.cs:8-9`, `ExtraStatPercent`) and the
  client prints `"0.##"` — no tooltip change needed.
- Settings: `GooseSettings` — `Goose/GooseSettings.cs`; loaded with
  `JsonHelper.SettingsOptions` (`Goose/JsonHelper.cs:34-43`, case-insensitive, comments
  allowed, **no** enum converter); load-time validation precedent
  `ValidateQuestIconSettings` (`Goose/GooseSettings.cs:175-185`, called
  `Goose/GooseSettingsLoader.cs:71`); `FatalStartupException`.
- Template load step — `Goose/GameWorld.cs:406-407`; post-load fatal check precedent
  `Goose/GameWorld.cs:424-425`.
- `ItemTemplate.UseTypes` / `ItemSlots` (incl. `Mount`, `Backpack`) —
  `Goose/ItemTemplate.cs:12-41`.
- Inventory: `GetSlot(i)` — `Goose/Inventory.cs:253`; `SendSlot(i, world)` — `:191`
  (sends only); `RemoveItem(Item, long, GameWorld)` — `:579-617` (returns null if stack too
  small; clears slot + `SendSlot` at stack==count); `SuppressQuestIconRefresh` — `:172-183`.
- `world.Random` — `Goose/GameWorld.cs:183-186`; injectable via internal
  `GameWorld(settings, server, random)` — `Goose/GameWorld.cs:209`; test RNG pattern
  `Goose.Tests/CritMultiplierTests.cs:8-18` (subclass `Random`, override `Next(int,int)`).
- Window base: `Clicked` — `Goose/Window.cs:142`; `SendCreate` — `Goose/Window.cs:327-334`;
  `Close` — `:338-342`; `WindowTypes` ends at `Backpack` (`Goose/Window.cs:66-90`);
  `WindowFrames` ends at `GridContainer = 32` (`Goose/Window.cs:55-57`); `MakeWindow` format
  — `Goose/Packets.cs:754-764`; `P.Notice` — `Goose/Packets.cs:43`.
- `CustomWindow` reference flow — `Goose/CustomWindow.cs` (`HandleSlot` stores
  ids + references; `Create` re-resolves and compares with `ReferenceEquals`).
- NPC dispatch — `Goose/Events/PlayerRightClickEvent.cs:53-71`
  (`npc.Properties.GetProperty("communityChest", false)` inside the `Banker` branch);
  range helper `Map.InRange(ICharacter, ICharacter)` — `Goose/Map.cs:156`; chest refuses
  out-of-range operations rather than auto-closing (`Goose/CommunityChestWindow.cs:132-139`).
- Packet registry — `Goose/EventHandler.cs:147-156` (`("CWS", Open(typeof(...)))`); event
  parse pattern `Goose/Events/CustomWindowSlotEvent.cs:13-35` (`State == Ready`,
  `Substring(3).Split(',')`). Events run on the game loop thread via `Event.Ready`.
- Logging: `Log.Types` — `Goose/Log.cs:9-52` (last player type `ResetItem` = 24);
  `LogHandler.Log(type, player, text, otherid)` — `Goose/LogHandler.cs:34`;
  `LogEventRegistry` — `Goose/Logs/LogEventRegistry.cs:33`; `LogFormatter` switch —
  `Goose/Logs/LogFormatter.cs:93`; tests `Goose.Tests/LogEventDescriptorTests.cs:34`,
  `Goose.Tests/LogFormatterTests.cs`.
- Test fixture — `TestSupport/TestWorldFixture.cs`: `new TestWorldFixture(configure)`
  (`:14`), `AddBaseMap` (`:69`), `AddNpcTemplate` (`:90`), `SpawnNpc` (`:112`),
  `CommandPlayerOn` → `CapturingPlayer` with `Sent` (`:131-150`), `AddBaseItemTemplate`
  (`:197`, default `Slot = OneHanded`, `StackSize = 1`). `CustomWindowTests` usage —
  `Goose.Tests/CustomWindowTests.cs:9-17`.

## Persistence strategy

No schema change. Enchant state is new keys in the existing `props` JSON on items. Old items
simply lack the keys (= level 0). Settings are additive with code defaults.

## Global invariants

| Invariant | Proved by |
|---|---|
| Enchant stats are added after, never multiplied by, `StatMultiplier` | Task 1 `RefreshStats_adds_enchant_stats_after_title_multiplier` |
| Changing per-level settings changes existing items | Task 1 `RefreshStats_reflects_current_per_level_settings` |
| Enchant counts survive save/load | Task 1 `Enchant_properties_round_trip_through_database_json` |
| ` +N` is always the last name token | Task 2 tests for `SetModifier`, `ResetModifiers`, `BuildCustomItem` |
| Reset never removes enchants | Task 2 `ResetModifiers_keeps_enchants_and_suffix` |
| Levels never decrease | Task 4 attempt tests (no outcome lowers any count) |
| Nothing is consumed on a refused `EWC` | Task 7 adversarial tests |

---

### Task 0: Enchant settings

**Files:**
- Create: `Goose/EnchantSettings.cs`
- Modify: `Goose/GooseSettings.cs` (add property), `Goose/GameWorld.cs` (validate after the
  Item Templates step, `:406-407`), `Goose/GooseSettings.json` (document defaults)
- Test: `Goose.Tests/EnchantSettingsTests.cs`

```csharp
public enum EnchantElement { Earth, Water, Fire, Air, Spirit }

public class EnchantSettings
{
    public int MaxLevel { get; set; } = 10;
    public int[] FailPercent { get; set; } = [0, 0, 0, 0, 0, 10, 20, 30, 40, 50];
    public double EarthSpellDamagePercent { get; set; } = 1;
    public double WaterSpellCritPercent { get; set; } = 1;
    public double FireDamageReductionPercent { get; set; } = 0.5;
    public double AirMPPercentRegenPercent { get; set; } = 0.04;
    public double SpiritHPPercentRegenPercent { get; set; } = 0.04;
    public Dictionary<int, EnchantElement> EssenceTemplates { get; set; } = [];
    public int ProtectionTemplateId { get; set; }

    public void Validate(ItemHandler items) { ... }
}
```

`GooseSettings.Enchanting { get; set; } = new();`

- `SettingsOptions` has no enum converter: put
  `[JsonConverter(typeof(JsonStringEnumConverter))]` on `EnchantElement` so the JSON reads
  `"1201": "Earth"`.
- Percent fields follow the `ItemModifierScript` convention (percent in config, `/100` when
  applied).
- `Validate` throws `FatalStartupException` when: `MaxLevel < 1`;
  `FailPercent.Length != MaxLevel`; any `FailPercent` outside `0..100`; any essence template
  id or a non-zero `ProtectionTemplateId` not found via `items.GetTemplate`. Empty
  `EssenceTemplates` is valid (feature dormant — the window says "Enchanting is not
  available."). Call it right after the Item Templates load step in `GameWorld`, since
  templates are not loaded at settings-load time.

**Step 1 — tests (red):** defaults match the design table; `Validate` passes with defaults +
no essences; throws for length mismatch, out-of-range percent, unknown essence template,
unknown protection template; JSON `{"Enchanting":{"EssenceTemplates":{"5":"Fire"}}}`
deserializes via `JsonHelper.SettingsOptions` to `{5: Fire}` (adversarial: fails without the
enum converter attribute).

**Step 2–4:** implement, run
`env -u MSBuildSDKsPath dotnet test Goose.Tests/Goose.Tests.csproj --filter EnchantSettingsTests`.

**Step 5:** `git commit -m "feat(enchanting): settings and startup validation"`

---

### Task 1: Enchant properties and derived stats

**Files:**
- Create: `Goose/Enchanting.cs` (static helper; grows in Tasks 2 and 4)
- Modify: `Goose/Item.cs` (enum + `RefreshStats`), every `RefreshStats` caller listed in
  *APIs verified*
- Test: `Goose.Tests/EnchantingStatsTests.cs`, update `Goose.Tests/PacketExtraStatsTests.cs`

**Mutation impact:**
- Source of truth: new `ItemProperty` members `EnchantEarth, EnchantWater, EnchantFire,
  EnchantAir, EnchantSpirit, EnchantMaxLoss` appended after `BackpackColumns`
  (`Goose/Item.cs:16`) in `Item.ItemProperties`.
- Derived state: `Item.TotalStats` (`Goose/Item.cs:65-66`), recomputed only by
  `RefreshStats`. Readers: `P.InventorySlot` extra stats (`Goose/Packets.cs:8-35`), equip
  path `player.AddStats(item.TotalStats)` (`Goose/Inventory.cs:1304`).
- Propagation: `RefreshStats(GooseSettings settings)` computes
  `(template + BaseStats) * StatMultiplier + Enchanting.Stats(item, settings.Enchanting)`.
  Making the parameter **required** forces every caller (incl. `.csx`) to pass settings;
  an optional parameter would silently drop enchant stats on any missed path.
- Invariants: enchant stats never written to `BaseStats`; `TotalWeaponDamage` unchanged.

**Helper contract (`Enchanting`, pure, no sends):**
- `int Count(Item, EnchantElement)` — `GetProperty<int>` on the mapped key, 0 if absent.
- `int Level(Item)` — sum of counts.
- `int Max(Item, EnchantSettings)` — `MaxLevel - GetProperty<int>(EnchantMaxLoss)`.
- `AttributeSet Stats(Item, EnchantSettings)` — Earth→`SpellDamage`, Water→`SpellCrit`,
  Fire→`DamageReduction`, Air→`MPPercentRegen`, Spirit→`HPPercentRegen`; `count * percent /
  100`.

**Step 1 — tests (red):**
- `RefreshStats_adds_enchant_stats_after_title_multiplier`: item with `StatMultiplier = 2`,
  template SpellDamage 0.05, Earth 3 → TotalStats.SpellDamage == 0.05*2 + 0.03 (adversarial:
  fails if enchant stats are added before the multiply).
- `RefreshStats_reflects_current_per_level_settings`: change `EarthSpellDamagePercent` 1→2,
  refresh, value doubles.
- `Enchant_properties_round_trip_through_database_json`: set counts as `int`, serialize with
  `JsonHelper.Serialize`, deserialize, `Count`/`Level`/`Max` match (values are `JsonElement`
  after the round trip).
- Each element maps to the right stat (theory over five elements).

**Step 3:** implement; update all callers. In `.csx` scripts use `world.Settings`.
`ResetModifiers` gains a `GooseSettings` parameter here only to compile (`ResetModifiers(item,
world.Settings)`; update `Commands.csx:120` and `ResetModifiersTests`) — behaviour changes in
Task 2.

**Step 4:** full fast suite green
(`env -u MSBuildSDKsPath dotnet test Goose.Tests/Goose.Tests.csproj`), plus
`env -u MSBuildSDKsPath dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj`
because the `.csx` scripts changed and integration tests compile shipped scripts.

**Step 5:** `git commit -m "feat(enchanting): enchant properties and derived stats"`

---

### Task 2: ` +N` name suffix through reset, modifiers and customs

**Files:**
- Modify: `Goose/Enchanting.cs`, `Goose/ItemHandler.cs:329-372`, `Goose/CustomItem.cs:74-104`
- Test: `Goose.Tests/EnchantingNameTests.cs`, extend `Goose.Tests/ResetModifiersTests.cs`,
  `Goose.Tests/CustomItemTests.cs`

**Mutation impact:**
- Source of truth: `Item.Name` (persisted). Readers: inventory packet, tooltips, logs.
- Helpers: `string StripSuffix(Item)` (removes a trailing `" +" + Level(item)` only when
  `Level > 0` and the name ends with it) and `void ApplySuffix(Item)` (strip, then append
  `" +N"` when `Level > 0`). Both mutate `Name` only.
- `SetModifier`: call `StripSuffix` **before** the surname strip (surname-by-`EndsWith` at
  `ItemHandler.cs:348` would otherwise fail on `"... of the Bear +7"`), then `ApplySuffix`
  after title/surname are applied.
- `ResetModifiers(item, settings)`: keep current resets (`Name = Template.Name`, `BaseStats`,
  `WeaponDamage`, `StatMultiplier`, Title/Surname keys); **do not** touch enchant keys; call
  `ApplySuffix`, then `RefreshStats(settings)`.
- `BuildCustomItem`: replace the two per-key copies (`:96-100`) with
  `item.ItemProperties = new Dictionary<ItemProperty, object>(statsItem.ItemProperties);`
  then `Enchanting.ApplySuffix(item)` after `Name = name`. `TotalStats` is already cloned
  from the stats item (`:80`), which includes enchant stats — assert that rather than
  adding a refresh.
- Invariant: suffix appears exactly once and last.

**Step 1 — tests (red):**
- `SetModifier_on_enchanted_item_keeps_suffix_last` — "Sword of the Bear +3", set a title →
  "Ancient Sword of the Bear +3"; replace the surname → old surname removed (adversarial:
  without the pre-strip the old surname survives).
- `ResetModifiers_keeps_enchants_and_suffix` — counts unchanged, name "Sword +3", TotalStats
  still include enchant stats.
- `ResetModifiers_twice_does_not_duplicate_suffix`.
- `BuildCustomItem_copies_all_properties_and_appends_suffix` — custom "My Blade" from a +2
  item → "My Blade +2", all keys present, enchant stats in TotalStats.

**Step 5:** `git commit -m "feat(enchanting): +N name suffix survives reset, modifiers and customs"`

---

### Task 3: Two-decimal regen in the stats window

**Files:** Modify `Goose/Window.cs:256,258`; Test: extend the existing stats-window test or
add `Goose.Tests/CharInfoRegenTests.cs`.

Replace `Math.Round(x * 100, 0)` with `Math.Round(x * 100, 2)` formatted `"0.##"`
(`CultureInfo.InvariantCulture`), so 2.04% shows `2.04%` and 2% still shows `2%`.

Test: player with HPPercentRegen 0.0204 → line contains `"HP Regeneration: 2.04%"`;
0.02 → `"2%"` (adversarial against `F2` producing `2.00%`).

`git commit -m "feat(stats): show regen to two decimals"`

---

### Task 4: Attempt resolution core

**Files:** Modify `Goose/Enchanting.cs`; Test: `Goose.Tests/EnchantingAttemptTests.cs`

```csharp
public enum EnchantOutcome { Success, ProtectedFailure, Destroyed }

public static int FailPercent(Item item, EnchantSettings s);          // for Level+1; 0 when full
public static bool CanEnchant(Item item, EnchantSettings s, out string reason);
public static EnchantOutcome Resolve(Item item, EnchantElement element, bool protectionAvailable,
                                     EnchantSettings s, Random random);
```

- `CanEnchant`: false with a player-facing reason for: UseType not Armor/Weapon; Slot is
  `Mount` or `Backpack`; `Level >= Max`.
- `Resolve` precondition: `CanEnchant` true (throw `InvalidOperationException` otherwise).
  Roll: fail when `FailPercent > 0 && random.Next(1, 101) <= FailPercent`.
  - Success → increment that element's key, `ApplySuffix`.
  - Fail + `protectionAvailable` → increment `EnchantMaxLoss`; counts unchanged.
  - Fail without protection → `Destroyed`; item unchanged (caller removes it).
- Owns: enchant keys and `Name` only. Does **not** refresh stats, send packets, consume
  items or log — the window does (Task 7), so this stays world-free.

**Tests (queue-backed `Random` subclass like `Goose.Tests/ItemModifierSelectionTests.cs:5`, but override `Next(int, int)` — that is what `Resolve` calls):**
- Levels 1–5 succeed even when `Next` returns 1 (and assert `Next` is not called for 0%).
- Target +6 with roll 10 → fail; roll 11 → success (boundary).
- Protected failure: max 10→9, counts unchanged, name unchanged.
- Destroyed: counts unchanged, returns `Destroyed`.
- Mixed elements: 3 Earth + 2 Fire, add Water → Level 6, suffix "+6".
- `CanEnchant` refuses mount, backpack, NoUse item, item at max (incl. max lowered to 7).
- Adversarial: after a protected failure at +7 with max 8, next success brings it to +8 and
  then `CanEnchant` refuses.

`git commit -m "feat(enchanting): attempt resolution"`

---

### Task 5: Enchant log type

**Files:** Modify `Goose/Log.cs` (append `Enchant` after `ResetItem` → 25),
`Goose/Logs/LogEventRegistry.cs` (`new(25, "Enchant", LogEventGroup.ItemsEconomy,
LogOtherIdKind.Item)`), `Goose/Logs/LogFormatter.cs` (case 25); Tests:
`Goose.Tests/LogEventDescriptorTests.cs`, `Goose.Tests/LogFormatterTests.cs`.

Text format (written in Task 7):
`Enchant: template <tid> element <Earth> level <7> -> <8> max <10> protected <0|1> outcome <Success|ProtectedFailure|Destroyed>`,
and for `Destroyed` a trailing ` item <JsonHelper.Serialize(item)>` so a GM can restore it.
Formatter summary: `"{Player} enchanted an item (template 12) Earth +7 -> +8"`,
`"... failed (protected), max now 9"`, `"... destroyed an item (template 12) at +7"`.
Malformed text → `Fallback(c)` (mirror the `ResetItem` tests at
`Goose.Tests/LogFormatterTests.cs:436-438`).

`git commit -m "feat(logs): enchant log type"`

---

### Task 6: EnchantWindow, opening and `EWS` preview

**Files:**
- Create: `Goose/EnchantWindow.cs`, `Goose/Events/EnchantWindowSlotEvent.cs`
- Modify: `Goose/Window.cs` (`WindowTypes.Enchant`, `WindowFrames.Enchant = 33`),
  `Goose/Events/PlayerRightClickEvent.cs:64-70`, `Goose/EventHandler.cs:155`,
  `Goose/Packets.cs` (`P.EnchantPreview`)
- Test: `Goose.Tests/EnchantWindowTests.cs`

**Opening:** in `PlayerRightClickEvent`, before the `Banker` check:
`if (npc.Properties.GetProperty("enchantTable", false)) { EnchantWindow.Open(world, Player, npc); return; }`
— works on any NPC type. `Open` closes any existing `EnchantWindow` (`Close`), then
constructs: `ID = ++player.LastWindowID`, `Frame = Enchant`, `Type = Enchant`,
`Title = "Enchanting Table"`, `Buttons = "0,1,0,0,1"`, `NPC = npc`, add to `player.Windows`,
`SendCreate`. `Clicked` Close/Exit removes the window (as `CustomWindow.Clicked`).
`Populate` sends an empty preview.

**Protocol (fixed here; Part 2 mirrors it):**

| Packet | Format |
|---|---|
| `EWS` C→S | `EWS<itemSlot>,<essenceSlot>,<protectionSlot>` (1-based inventory ids, 0 = empty) |
| `EWP` S→C | `EWP<windowId>,<itemSlot>,<essenceSlot>,<protectionSlot>,<canEnchant 0/1>` then `|<line>` per display line |
| `EWC` C→S | `EWC<itemSlot>,<essenceSlot>,<protectionSlot>` |

`EWP` echoes the slots the server **accepted**; a rejected slot comes back as 0 and the
client clears it. Lines are server-rendered, so the client needs no stat tables. Commas are
allowed inside lines; `|` is stripped from item names when rendering.

**`HandleSlot(world, player, item, essence, protection)`** (static, like
`CustomWindow.HandleSlot`):
1. No open window → return. Not `Map.InRange(player, window.NPC)` → notice "You are too far
   from the enchanting table." and return.
2. Resolve each non-zero id with `player.Inventory.GetSlot`. Accept item slot only if
   `Enchanting.CanEnchant`; essence only if its `TemplateID` is in `EssenceTemplates`;
   protection only if `TemplateID == ProtectionTemplateId` and non-zero. Same id used twice →
   reject the later one.
3. Store accepted ids + `Item` references (`ItemSlotId/EssenceSlotId/ProtectionSlotId`,
   `TargetItem/EssenceItem/ProtectionItem`).
4. Send `EWP` with lines:
   - item: `"<Name> (max <Max>)"`, then one line per non-zero element
     `"Earth 4: +4% spell damage"` (percent formatted `"0.##"`); or the rejection reason.
   - essence: `"Next: Earth +1% spell damage"`.
   - `"Failure chance: 30%"` or `"Failure chance: none"`.
   - protection: `"Protected: yes"` / `"Protected: no"` / `"Protected: not needed"` (fail 0%).
   - Empty `EssenceTemplates` → single line `"Enchanting is not available."`, canEnchant 0.
   - canEnchant = item accepted && essence accepted.

**Tests:** right-click NPC with `enchantTable` opens one window and sends `MKW…,33,…`;
second open replaces the first; Close removes; `EWS` with valid item+essence → `EWP` with
canEnchant 1 and the expected lines; mount item → slot echoed as 0 with reason; unknown
essence → 0; protection on a +2 item → "not needed"; out of range → notice, stored slots
unchanged; feature dormant → "not available".

`git commit -m "feat(enchanting): enchanting table window and preview"`

---

### Task 7: `EWC` confirm

**Files:** Create `Goose/Events/EnchantWindowCreateEvent.cs`; Modify `Goose/EnchantWindow.cs`,
`Goose/EventHandler.cs`; Test: `Goose.Tests/EnchantWindowConfirmTests.cs`

**Mutation impact:**
- Sources of truth: inventory slots (`Inventory.inventory`), item enchant keys + `Name`.
- Readers: client inventory (`SIS`/clear via `SendSlot`), item tooltip, player save
  (`Inventory.Save` serializes slots in full, `Goose/Inventory.cs:1182-1201`), log.
- Sequence on `Confirm(world, player, item, essence, protection)`:
  1. Window open, in range, `canEnchant` last sent, and the three ids equal the stored ids
     and each `GetSlot(id).Item` is `ReferenceEquals` the stored reference — else notice
     "Items changed, place them again." and return (nothing consumed).
  2. Re-check `Enchanting.CanEnchant` (item may have changed via another path).
  3. Inside `player.Inventory.SuppressQuestIconRefresh(world, ...)`:
     - `RemoveItem(EssenceItem, 1, world)`; null → log error, notice, return before any
       roll.
     - `bool protect = ProtectionItem is not null && Enchanting.FailPercent(...) > 0`.
     - `outcome = Enchanting.Resolve(TargetItem, element, protect, settings, world.Random)`.
     - Success: `TargetItem.RefreshStats(world.Settings)`; `SendSlot(ItemSlotId)`;
       notice "Your <Name> glows (+N)."
     - ProtectedFailure: `RemoveItem(ProtectionItem, 1)`; `RefreshStats`;
       `SendSlot(ItemSlotId)`; notice "The enchantment fizzled. Your protection held, but the
       item can now only reach +X."
     - Destroyed: log first (needs the full item), then `RemoveItem(TargetItem, 1)`; notice
       "The enchantment failed and your <Name> shattered."
  4. Log `Log.Types.Enchant` (Task 5 format, `otherid = TargetItem.ItemID`).
  5. Re-run `HandleSlot` with the stored ids so the client gets a fresh `EWP` (destroyed
     item / last essence / used protection come back as 0).
- Invariants: refusal consumes nothing; essence consumed exactly once per attempt; protection
  consumed only on `ProtectedFailure`.
- Threading: runs on the game loop via `Event.Ready`, same as `CustomWindowCreateEvent`.

**Tests (inject RNG via `new GameWorld(settings, null, random)` or a fixture overload — add
`TestWorldFixture(Action<GooseSettings>?, Random?)` if none exists, Task-local):**
- Success: essence stack 3→2, counts +1, name suffix, `SIS` sent for the item slot, log row.
- Protected failure: protection consumed, max −1, essence consumed, item kept.
- Unprotected failure: item slot empty, essence consumed, log text contains `item {`.
- Protection held on a safe level is not consumed.
- Adversarial: `EWS` then move the item to another slot, `EWC` with old ids → refused,
  essence stack unchanged, counts unchanged.
- Adversarial: `EWC` ids differ from last `EWS` → refused.
- Adversarial: last essence consumed → follow-up `EWP` has essenceSlot 0, canEnchant 0.

Full suite: `env -u MSBuildSDKsPath dotnet test Goose.Tests/Goose.Tests.csproj`.

`git commit -m "feat(enchanting): confirm, consume and resolve attempts"`

---

## Design alignment check

- Rules, values, failure ladder, protection lowering max, no removal, mount/backpack
  exclusion — Tasks 0, 4.
- Stats after title multiplier, derived from settings — Task 1.
- ` +N` last; reset keeps enchants; customs copy all properties — Task 2.
- Regen decimals — Task 3. Tooltip already two-decimal (verified).
- Window opened by `enchantTable` NPC property, slot references, drift guard — Tasks 6–7.
- Logging with full item on destroy — Tasks 5, 7.
- **Refinements vs design** (design doc updated): `EWP` carries server-rendered lines and
  echoes accepted slot ids, so the client no longer computes breakdown text; out-of-range
  refuses operations (chest precedent) rather than auto-closing; `RefreshStats` takes
  `GooseSettings`.

## Deferred

Per design: essence/protection sources, slot clearing on inventory change, GM removal,
mounts/backpacks, balance tuning. Game-data templates for essences/protection are authored
when the arena lands; until then the feature is dormant (empty `EssenceTemplates`).
