# Nullable Reference Types — Classified Warning Inventory

Captured at HEAD `8e72576` with `<Nullable>enable</Nullable>` enabled in `Goose/Goose.csproj`.
This is the load-bearing artifact for the Nullable Reference Types Adoption Plan: Tasks 2–4
fix warnings by area and reduce the per-area counts below to zero; Task 5 proves the build is
warning-free. If any task stalls, this inventory is sufficient to hand the work to a fresh
plan: every warning has file:line, code, message, and area.

## Gate decision (Task 1 Step 3)

The prescribed capture (`grep -E "warning CS8[5-9][0-9]{2}" | sort | wc -l`) returned a raw
count of **978**, above the 500 threshold. However, the MSBuild log emits **every warning
line exactly twice** (the compiler output block is repeated), verified line-for-line.
Deduplicated (`sort -u`) counts:

| Scope | Raw (wc -l) | Unique |
|---|---|---|
| `dotnet build Goose.sln` (Goose + Goose.Tests + tools) | 978 | **489** (Goose 442 + Goose.Tests 47) |
| + `Goose.IntegrationTests` (separate build, not in the sln) | — | **502** total (+13) |

The plan owner accepted the deduplicated count as the metric on 2026-08-25: 489 ≤ 500, so
the Step 3 gate does **not** trip and the plan proceeds. All counts in this document are
deduplicated unique warnings.

`Goose.IntegrationTests` is deliberately omitted from `Goose.sln` (fast-test-boundary
plan), so its 13 warnings are only visible when building the project directly:
`dotnet build Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-incremental`.
They are listed under area 6, clearly marked.

## Capture procedure

```bash
dotnet build Goose.sln --no-incremental > /tmp/nullable-inventory-build.txt 2>&1   # exit 0
grep -E "warning CS8[5-9][0-9]{2}" /tmp/nullable-inventory-build.txt | sort -u
dotnet build Goose.IntegrationTests/Goose.IntegrationTests.csproj --no-incremental
grep -E "warning CS8[5-9][0-9]{2}" <log> | sort -u
```

The `CS8[5-9]xx` range is a deliberate superset of the nullable diagnostics. In practice
**only CS86xx codes appeared** — no CS85xx/CS87xx/CS89xx codes (e.g. CS8701/CS8702) were
emitted, so no non-nullable codes needed excluding.

### Per-code breakdown (unique, all projects)

| Code | Meaning | Count |
|---|---|---|
| CS8618 | Non-nullable field/property must contain a non-null value when exiting constructor | 189 |
| CS8625 | Cannot convert null literal to non-nullable reference type | 106 |
| CS8600 | Converting null literal or possible null value to non-nullable type | 101 |
| CS8603 | Possible null reference return | 51 |
| CS8602 | Dereference of a possibly null reference | 32 |
| CS8604 | Possible null reference argument | 11 |
| CS8601 | Possible null reference assignment | 9 |
| CS8605 | Cannot use null-forgiving operator on expression of non-nullable type | 2 |
| CS8620 | Argument cannot be used for parameter specified as [AllowNull] | 1 |
| | **Total** | **502** |

## Areas and targets

1. **Model construction** — entity/model constructors and properties that must be set
   before use (`Player`, `NPC`, `Item`, quest models, …). Fix: annotate non-nullable +
   `!` at the construction site where non-null is provable, or `T?` where absence is real.
2. **Database row mapping** — `FromReader`/`FromRow` paths, `ExecuteScalar`/
   `Convert.ToString` sites, `JsonHelper.Deserialize` of DB-serialized columns. Includes
   the one named `!`: `DataReaderExtensions.GetString`'s body — see note below.
3. **Collections containing nullable slots** — `List<T?>`/`Dictionary<K, V?>` where
   absence is represented by null elements or a failed lookup (inventory/equipped slots,
   map tiles, spell slots, registry lookups in handlers).
4. **Packet/event inputs** — `Event.Data`/event payload properties, command-argument
   strings, packet payload parsing and construction in `Goose/Events/*`,
   `Goose/Packets.cs`, the console command surface, and connection/socket handling.
5. **Script-facing APIs** — the Roslyn scripting boundary (`ScriptHandler`, `Script`,
   `Base*Script`, `GooseSettings` script surface) where nullability crosses into compiled
   scripts.
6. **Tests and fakes** — `Goose.Tests`, `Goose.IntegrationTests`, linked
   `TestSupport/*`.

| Area | Unique warnings (target for Tasks 2–4) |
|---|---|
| 1. Model construction | 218 |
| 2. Database row mapping | 37 |
| 3. Collections containing nullable slots | 80 |
| 4. Packet/event inputs | 74 |
| 5. Script-facing APIs | 33 |
| 6. Tests and fakes (47 Goose.Tests + 13 Goose.IntegrationTests) | 60 |
| **Total** | **502** |

Classification convention: a warning is assigned to the area that owns the nullability
contract being fixed. E.g. `this.NPC.MoveEvent = null` inside `NPCMoveEvent` is area 1
(the `NPC.MoveEvent` property is what gets annotated), while `Event`'s own payload
properties (`Player`, `Data`, `NPC`) are area 4.

### Area 2 note: `DataReaderExtensions.GetString`

`Goose/DataReaderExtensions.cs(14)` — `GetString` returns non-nullable `string` (null and
`DBNull` cells both yield `""`); its body `Convert.ToString(reader[column])!` emits
CS8603. That is the one named `!` in this plan. Proof: for every SQLite-supported cell
value (null, `DBNull`, string, numeric, `byte[]`) `Convert.ToString` returns a non-null
string, and the class is `internal`, so no external `DbDataReader` implementation can
reach it.

## Pre-plan test baseline

- Location: `/tmp/goose-pre-nullable-trx/` (TRX, `LogFilePrefix=pre`); log
  `/tmp/goose-pre-nullable-test.log`.
- Captured at `8e72576` before the csproj change. Exit 0: Goose.Tests 341 passed /
  0 failed; Tools.Tests 124 passed / 26 skipped / 0 failed.
- (The older `/tmp/goose-baseline-trx` predates the fast-test-boundary split and the
  DbDataReader migration commits; do not use it.)

## Latent bugs (deferred)

(Empty — Tasks 2–4 record behavior-affecting findings here as they annotate.)

## Classified inventory

Format: `file(line,col): warning CSxxxx: message`. Paths relative to the repository root.

### Area 1 — Model construction (218)

Goose/Buff.cs(11,27): warning CS8618: Non-nullable property 'Caster' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Buff.cs(12,27): warning CS8618: Non-nullable property 'Target' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Buff.cs(13,28): warning CS8618: Non-nullable property 'SpellEffect' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Buff.cs(16,22): warning CS8618: Non-nullable property 'BuffExpireEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Class.cs(14,23): warning CS8618: Non-nullable property 'ClassName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ClassLevel.cs(26,29): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ClassLevel.cs(28,28): warning CS8618: Non-nullable property 'Spells' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Combination.cs(9,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Combination.cs(16,35): warning CS8618: Non-nullable property 'ResultItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Combination.cs(18,37): warning CS8618: Non-nullable property 'RequiredHash' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Currency/CurrencyHandler.cs(41,25): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Currency/CurrencyHandler.cs(42,48): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/BuffExpireEvent.cs(18,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/LogoutEvent.cs(56,49): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/MacroCheckEvent.cs(15,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/MacroConfirmCommandEvent.cs(28,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/NPCAttackEvent.cs(9,36): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/NPCMoveEvent.cs(13,34): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetAttackEvent.cs(10,31): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetAttackEvent.cs(21,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetAttackEvent.cs(29,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetMoveEvent.cs(10,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/PetMoveEvent.cs(34,34): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/ToggleCommandEvent.cs(79,58): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/GameServer.cs(61,27): warning CS8618: Non-nullable field 'IP' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/GameServer.cs(67,16): warning CS8618: Non-nullable field 'gameworld' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/GameServer.cs(67,16): warning CS8618: Non-nullable field 'listen' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/GameWorld.cs(97,16): warning CS8618: Non-nullable property 'CharactersCreatedPerIP' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GameWorld.cs(97,16): warning CS8618: Non-nullable property 'LoginThrottle' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GameWorld.cs(97,70): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/GameWorld.cs(326,79): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Group.cs(59,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Group.cs(80,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Guild.cs(54,16): warning CS8618: Non-nullable property 'MOTD' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Guild.cs(54,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Guild.cs(149,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/ItemContainerWindow.cs(7,30): warning CS8618: Non-nullable property 'ItemContainer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemContainerWindow.cs(65,47): warning CS8602: Dereference of a possibly null reference.
Goose/ItemContainerWindow.cs(66,45): warning CS8602: Dereference of a possibly null reference.
Goose/Item.cs(137,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Item.cs(137,16): warning CS8618: Non-nullable property 'Template' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemModifier.cs(11,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemModifier.cs(19,44): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemModifier.cs(20,23): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'CurrencyId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTemplate.cs(119,16): warning CS8618: Non-nullable property 'SpellEffect' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable field 'characters' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable field 'tiles' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'FileName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(84,16): warning CS8618: Non-nullable property 'ScriptStore' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Map.cs(419,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Map.cs(436,70): warning CS8602: Dereference of a possibly null reference.
Goose/NPC.cs(69,20): warning CS8618: Non-nullable property 'Map' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(73,29): warning CS8618: Non-nullable property 'MaxStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(149,28): warning CS8618: Non-nullable property 'NPCTemplate' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(153,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(157,23): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(161,23): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(169,29): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(237,22): warning CS8618: Non-nullable property 'Class' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(286,23): warning CS8618: Non-nullable property 'EquippedItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(299,23): warning CS8618: Non-nullable property 'AggroTarget' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(303,22): warning CS8618: Non-nullable property 'AggroValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(307,42): warning CS8618: Non-nullable property 'AggroTargetToValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(314,22): warning CS8618: Non-nullable property 'MoveEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(319,22): warning CS8618: Non-nullable property 'AttackEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(321,27): warning CS8618: Non-nullable property 'Buffs' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(363,30): warning CS8618: Non-nullable property 'Quests' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(369,23): warning CS8618: Non-nullable property 'ScriptStore' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPC.cs(450,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(454,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(531,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(703,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(705,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(928,69): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(978,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(990,36): warning CS8601: Possible null reference assignment.
Goose/NPC.cs(1116,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(1118,38): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(1151,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(1171,47): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(1171,47): warning CS8602: Dereference of a possibly null reference.
Goose/NPC.cs(1300,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPC.cs(1466,40): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPC.cs(1548,17): warning CS8602: Dereference of a possibly null reference.
Goose/NPC.cs(1648,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/NPCDropInfo.cs(9,29): warning CS8618: Non-nullable property 'ItemTemplate' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCHandler.cs(108,42): warning CS8601: Possible null reference assignment.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Allies' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'AlliesString' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'CurrencyId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Drops' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'EquippedItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(208,16): warning CS8618: Non-nullable property 'VendorItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(215,16): warning CS8618: Non-nullable property 'Allies' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(215,16): warning CS8618: Non-nullable property 'Drops' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/NPCTemplate.cs(257,27): warning CS8601: Possible null reference assignment.
Goose/NPCTemplate.cs(258,26): warning CS8601: Possible null reference assignment.
Goose/NPCVendorSlot.cs(8,29): warning CS8618: Non-nullable property 'ItemTemplate' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Paths.cs(34,56): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Paths.cs(39,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'AttackEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'EquippedItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'MoveEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'Owner' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(286,16): warning CS8618: Non-nullable property 'Target' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Pet.cs(535,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Pet.cs(536,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Pet.cs(655,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(492,16): warning CS8618: Non-nullable field 'sock' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Bank' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'BoundMap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Class' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Group' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Guild' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Inventory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'MacroCheckEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Map' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'MaxStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'PasswordHash' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'PasswordSalt' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'SendBuffer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Spellbook' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(492,16): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable field 'sock' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Bank' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'BaseStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'BoundMap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Buffer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Buffs' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Class' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Group' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Guild' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Inventory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'MacroCheckEvent' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Map' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'MaxStats' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'moveSpeed' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'PasswordHash' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'PasswordSalt' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Pets' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'QuestProgress' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'QuestsCompleted' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'QuestsStarted' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'SendBuffer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Spellbook' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Surname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(517,16): warning CS8618: Non-nullable property 'Windows' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Player.cs(1199,39): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(1318,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(1370,43): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(1374,28): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(2218,17): warning CS8602: Dereference of a possibly null reference.
Goose/Player.cs(2396,40): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(2410,17): warning CS8602: Dereference of a possibly null reference.
Goose/Program.cs(60,20): warning CS8603: Possible null reference return.
Goose/Program.cs(71,13): warning CS8602: Dereference of a possibly null reference.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'FailText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/Quest.cs(32,16): warning CS8618: Non-nullable property 'PassText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestProgress.cs(8,33): warning CS8618: Non-nullable property 'Requirement' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestRequirement.cs(24,22): warning CS8618: Non-nullable property 'Quest' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestRequirement.cs(33,37): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestRequirement.cs(34,23): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestReward.cs(40,23): warning CS8618: Non-nullable property 'StringValue' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestReward.cs(41,37): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestReward.cs(42,23): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestStatus.cs(30,22): warning CS8618: Non-nullable property 'Started' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestStatus.cs(31,22): warning CS8618: Non-nullable property 'Completed' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestStatus.cs(32,32): warning CS8618: Non-nullable property 'Progress' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Quests/QuestWindow.cs(23,16): warning CS8618: Non-nullable field 'scriptCannotCompleteMessage' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Quests/QuestWindow.cs(347,40): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Ranks.cs(62,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Ranks.cs(86,30): warning CS8601: Possible null reference assignment.
Goose/Ranks.cs(90,36): warning CS8602: Dereference of a possibly null reference.
Goose/Spell.cs(40,16): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Spell.cs(40,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Spell.cs(40,16): warning CS8618: Non-nullable property 'SpellEffect' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'HPFormula' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'MPFormula' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OffEffectText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OnEffectText' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OnMeleeAttackSpell' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'OnMeleeHitSpell' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'Script' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'ScriptParams' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(226,16): warning CS8618: Non-nullable property 'SPFormula' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/SpellEffect.cs(332,20): warning CS8603: Possible null reference return.
Goose/SpellEffect.cs(824,92): warning CS8602: Dereference of a possibly null reference.
Goose/SpellEffect.cs(921,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/SpellEffect.cs(956,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/SpellEffect.cs(974,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/WarpTile.cs(7,20): warning CS8618: Non-nullable property 'WarpMap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(70,31): warning CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(81,31): warning CS8618: Non-nullable property 'Buttons' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(83,20): warning CS8618: Non-nullable property 'NPC' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Window.cs(87,23): warning CS8618: Non-nullable property 'Data' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.

### Area 2 — Database row mapping (37)

Goose/Database.cs(17,34): warning CS8618: Non-nullable field '_connection' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(18,22): warning CS8618: Non-nullable field '_loopTask' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(27,45): warning CS8618: Non-nullable field 'Action' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(28,51): warning CS8618: Non-nullable field 'Func' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(29,27): warning CS8618: Non-nullable field 'Result' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(30,30): warning CS8618: Non-nullable field 'Error' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(36,45): warning CS8618: Non-nullable field 'Action' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(37,38): warning CS8618: Non-nullable field 'OnComplete' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Database.cs(58,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Database.cs(89,39): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(108,29): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(148,58): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(167,31): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(204,54): warning CS8603: Possible null reference return.
Goose/Database.cs(218,93): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(238,91): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Database.cs(313,25): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/DataReaderExtensions.cs(14,16): warning CS8603: Possible null reference return.
Goose/Events/CreditsUpdateEvent.cs(23,72): warning CS8604: Possible null reference argument for parameter 'name' in 'Player PlayerHandler.GetPlayerFromData(string name)'.
Goose/Events/CreditsUpdateEvent.cs(36,50): warning CS8620: Argument of type '(Player player, int credits, string?)' cannot be used for parameter 'item' of type '(Player Player, int Credits, string TxnId)' in 'void List<(Player Player, int Credits, string TxnId)>.Add((Player Player, int Credits, string TxnId) item)' due to differences in the nullability of reference types.
Goose/Events/CreditsUpdateEvent.cs(39,42): warning CS8604: Possible null reference argument for parameter 'item' in 'void List<string>.Add(string item)'.
Goose/Guild.cs(274,99): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(912,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(913,73): warning CS8604: Possible null reference argument for parameter 'json' in 'ItemSlot[] JsonHelper.Deserialize<ItemSlot[]>(string json)'.
Goose/Inventory.cs(931,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(932,72): warning CS8604: Possible null reference argument for parameter 'json' in 'ItemSlot[] JsonHelper.Deserialize<ItemSlot[]>(string json)'.
Goose/Inventory.cs(962,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(963,75): warning CS8604: Possible null reference argument for parameter 'json' in 'ItemSlot[] JsonHelper.Deserialize<ItemSlot[]>(string json)'.
Goose/JsonHelper.cs(49,13): warning CS8603: Possible null reference return.
Goose/Pet.cs(296,65): warning CS8604: Possible null reference argument for parameter 'onCommit' in 'void Database.EnqueueTransaction(Action<SQLiteConnection> action, Action onCommit = null)'.
Goose/Player.cs(821,42): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Player.cs(822,71): warning CS8604: Possible null reference argument for parameter 'json' in 'QuestStatus JsonHelper.Deserialize<QuestStatus>(string json)'.
Goose/Player.cs(904,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Player.cs(933,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Player.cs(952,16): warning CS8604: Possible null reference argument for parameter 'onCommit' in 'void Database.EnqueueTransaction(Action<SQLiteConnection> action, Action onCommit = null)'.
Goose/Spellbook.cs(42,42): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Spellbook.cs(43,62): warning CS8604: Possible null reference argument for parameter 'json' in 'int[] JsonHelper.Deserialize<int[]>(string json)'.

### Area 3 — Collections containing nullable slots (80)

Goose/ChatFilter.cs(38,69): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Class.cs(21,20): warning CS8603: Possible null reference return.
Goose/ClassHandler.cs(27,50): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ClassHandler.cs(32,20): warning CS8603: Possible null reference return.
Goose/CombinationHandler.cs(151,20): warning CS8603: Possible null reference return.
Goose/Currency/CurrencyHandler.cs(29,50): warning CS8603: Possible null reference return.
Goose/Currency/CurrencyHandler.cs(30,20): warning CS8603: Possible null reference return.
Goose/Events/CustomCommandEvent.cs(84,97): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/CustomCommandEvent.cs(87,99): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/CustomCommandEvent.cs(98,51): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/CustomCommandEvent.cs(107,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Events/RankCommandEvent.cs(46,90): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GuildHandler.cs(70,20): warning CS8603: Possible null reference return.
Goose/Inventory.cs(180,20): warning CS8603: Possible null reference return.
Goose/Inventory.cs(249,39): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(459,49): warning CS8603: Possible null reference return.
Goose/Inventory.cs(463,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(481,20): warning CS8603: Possible null reference return.
Goose/Inventory.cs(504,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(516,41): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(537,45): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Inventory.cs(542,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Inventory.cs(680,20): warning CS8603: Possible null reference return.
Goose/ItemContainer.cs(35,24): warning CS8603: Possible null reference return.
Goose/Item.cs(226,60): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Item.cs(227,24): warning CS8603: Possible null reference return.
Goose/Item.cs(234,24): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(190,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemHandler.cs(193,20): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(219,20): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(219,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemHandler.cs(224,20): warning CS8603: Possible null reference return.
Goose/ItemHandler.cs(224,54): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemHandler.cs(361,20): warning CS8603: Possible null reference return.
Goose/ItemModifier.cs(26,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemModifier.cs(27,48): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemSlot.cs(14,21): warning CS8618: Non-nullable property 'Item' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemSlot.cs(49,33): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/ItemSlot.cs(50,24): warning CS8601: Possible null reference assignment.
Goose/ItemSlot.cs(51,22): warning CS8601: Possible null reference assignment.
Goose/ItemSlot.cs(57,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/ItemTile.cs(16,25): warning CS8618: Non-nullable property 'ItemSlot' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/ItemTile.cs(22,23): warning CS8618: Non-nullable property 'Owner' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/LoginThrottle.cs(74,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/LoginThrottle.cs(94,52): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Map.cs(261,56): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Map.cs(584,87): warning CS8603: Possible null reference return.
Goose/Map.cs(671,87): warning CS8603: Possible null reference return.
Goose/MapHandler.cs(91,20): warning CS8603: Possible null reference return.
Goose/NPCHandler.cs(38,43): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(39,60): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(225,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(226,51): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/NPCHandler.cs(229,20): warning CS8603: Possible null reference return.
Goose/NPCHandler.cs(308,98): warning CS8603: Possible null reference return.
Goose/PlayerBank.cs(94,39): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/PlayerBank.cs(95,62): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/PlayerHandler.cs(86,51): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/PlayerHandler.cs(106,51): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/PlayerHandler.cs(124,20): warning CS8603: Possible null reference return.
Goose/PlayerHandler.cs(133,20): warning CS8603: Possible null reference return.
Goose/PlayerHandler.cs(183,20): warning CS8603: Possible null reference return.
Goose/Quests/Quest.cs(43,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/Quest.cs(44,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/QuestHandler.cs(75,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/QuestHandler.cs(77,54): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/QuestHandler.cs(82,20): warning CS8603: Possible null reference return.
Goose/Spellbook.cs(255,37): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Spellbook.cs(305,38): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/SpellHandler.cs(38,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(39,55): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(204,34): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(205,46): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(206,20): warning CS8603: Possible null reference return.
Goose/SpellHandler.cs(238,31): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(239,54): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(282,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(283,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellHandler.cs(284,20): warning CS8603: Possible null reference return.
Goose/SpellHandler.cs(300,20): warning CS8603: Possible null reference return.

### Area 4 — Packet/event inputs (74)

Goose/Console/Commands/SetAccessCommand.cs(13,23): warning CS8618: Non-nullable field 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/Commands/SetAccessCommand.cs(43,23): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Console/Commands/SetAccessCommand.cs(44,21): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Console/ConsoleCommandHandler.cs(11,44): warning CS8618: Non-nullable field 'Run' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandHandler.cs(12,23): warning CS8618: Non-nullable field 'Usage' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandHandler.cs(13,23): warning CS8618: Non-nullable field 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandHandler.cs(53,60): warning CS8602: Dereference of a possibly null reference.
Goose/Console/ConsoleCommandHandler.cs(99,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Console/ConsoleCommandHandler.cs(121,48): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Console/ConsoleCommandHandler.cs(127,65): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Console/ConsoleCommandParser.cs(10,23): warning CS8618: Non-nullable field 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandParser.cs(11,25): warning CS8618: Non-nullable field 'Args' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/Console/ConsoleCommandParser.cs(34,57): warning CS8603: Possible null reference return.
Goose/Console/ConsoleCommandParser.cs(37,17): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Event.cs(19,16): warning CS8618: Non-nullable property 'Data' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Event.cs(19,16): warning CS8618: Non-nullable property 'NPC' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Event.cs(19,16): warning CS8618: Non-nullable property 'Player' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/EventHandler.cs(29,38): warning CS8602: Dereference of a possibly null reference.
Goose/EventHandler.cs(64,32): warning CS8618: Non-nullable field 'EventTypeId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/EventHandler.cs(65,32): warning CS8618: Non-nullable field 'EventFactory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose/EventHandler.cs(250,55): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/EventHandler.cs(285,67): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/EventHandler.cs(365,44): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/CustomCommandEvent.cs(366,20): warning CS8603: Possible null reference return.
Goose/Events/HairdyeCommandEvent.cs(182,20): warning CS8603: Possible null reference return.
Goose/Events/LoginEvent.cs(47,25): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/LoginEvent.cs(47,25): warning CS8602: Dereference of a possibly null reference.
Goose/Events/LoginEvent.cs(48,18): warning CS8602: Dereference of a possibly null reference.
Goose/Events/MacroCheckEvent.cs(9,23): warning CS8618: Non-nullable property 'Code' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Events/PetDamageCommandEvent.cs(47,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetDeleteCommandEvent.cs(29,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetInfoCommandEvent.cs(29,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetSpawnCommandEvent.cs(40,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PetVitaCommandEvent.cs(47,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs(19,41): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs(19,41): warning CS8602: Dereference of a possibly null reference.
Goose/Events/PlayerCountExperienceModifierUpdateEvent.cs(20,34): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(20,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(28,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(29,37): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(31,21): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(33,21): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(41,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/SetConfigCommandEvent.cs(42,25): warning CS8602: Dereference of a possibly null reference.
Goose/Events/SetConfigCommandEvent.cs(43,44): warning CS8601: Possible null reference assignment.
Goose/Events/SetConfigCommandEvent.cs(43,44): warning CS8602: Dereference of a possibly null reference.
Goose/Events/VendorPurchaseInventoryEvent.cs(38,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/VendorSellInventoryEvent.cs(42,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WhoEvent.cs(24,32): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(41,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(42,36): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(50,20): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/WindowToWindowEvent.cs(55,18): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(162,29): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(232,44): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(248,45): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(400,23): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(400,23): warning CS8602: Dereference of a possibly null reference.
Goose/GameServer.cs(428,57): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameServer.cs(455,34): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameWorld.cs(71,20): warning CS8603: Possible null reference return.
Goose/GameWorld.cs(71,58): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GameWorld.cs(402,47): warning CS8602: Dereference of a possibly null reference.
Goose/GameWorld.cs(426,48): warning CS8602: Dereference of a possibly null reference.
Goose/GameWorld.cs(478,60): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Group.cs(109,50): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Packets.cs(81,39): warning CS8602: Dereference of a possibly null reference.
Goose/Packets.cs(118,39): warning CS8602: Dereference of a possibly null reference.
Goose/Packets.cs(292,44): warning CS8603: Possible null reference return.
Goose/Packets.cs(295,96): warning CS8603: Possible null reference return.
Goose/Packets.cs(298,97): warning CS8603: Possible null reference return.
Goose/Packets.cs(480,66): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Player.cs(2470,44): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose/Spellbook.cs(152,53): warning CS8625: Cannot convert null literal to non-nullable reference type.

### Area 5 — Script-facing APIs (33)

Goose/Events/PickupItemEvent.cs(91,38): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Events/PickupItemEvent.cs(94,35): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/GooseSettings.cs(6,23): warning CS8618: Non-nullable property 'ServerVersion' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(7,23): warning CS8618: Non-nullable property 'ServerType' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(8,23): warning CS8618: Non-nullable property 'DatabaseName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(9,23): warning CS8618: Non-nullable property 'DataLinkId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(10,23): warning CS8618: Non-nullable property 'DataPath' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(20,23): warning CS8618: Non-nullable property 'ServerName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(24,23): warning CS8618: Non-nullable property 'StartingItems' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(33,23): warning CS8618: Non-nullable property 'GameServerIP' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(75,23): warning CS8618: Non-nullable property 'MOTD' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(76,23): warning CS8618: Non-nullable property 'StartingTitle' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(77,23): warning CS8618: Non-nullable property 'StartingSurname' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettings.cs(95,23): warning CS8618: Non-nullable property 'DefaultGuildMOTD' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/GooseSettingsLoader.cs(52,20): warning CS8603: Possible null reference return.
Goose/Map.cs(605,30): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Map.cs(608,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Quests/QuestWindow.cs(323,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseItemScript.cs(26,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseMapScript.cs(35,32): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/BaseMapScript.cs(36,61): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/BaseMapScript.cs(39,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseMapScript.cs(78,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseQuestScript.cs(27,20): warning CS8603: Possible null reference return.
Goose/Scripting/BaseSpellEffectScript.cs(31,20): warning CS8603: Possible null reference return.
Goose/Scripting/Script.cs(14,16): warning CS8618: Non-nullable property 'Object' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Goose/Scripting/Script.cs(44,27): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/Script.cs(44,27): warning CS8601: Possible null reference assignment.
Goose/Scripting/ScriptHandler.cs(20,30): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/Scripting/ScriptHandler.cs(21,57): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose/SpellEffect.cs(474,45): warning CS8603: Possible null reference return.
Goose/SpellEffect.cs(479,24): warning CS8603: Possible null reference return.
Goose/SpellEffect.cs(484,24): warning CS8603: Possible null reference return.

### Area 6 — Tests and fakes (60)

Goose.Tests (47, built via Goose.sln):

Goose.Tests/BuiltInCurrencyTests.cs(86,77): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/CurrencyHandlerTests.cs(91,71): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/CurrencyHandlerTests.cs(123,92): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/CurrencyHandlerTests.cs(134,82): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/GameServerStartupTests.cs(30,25): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/GameServerStartupTests.cs(30,25): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/GameServerStartupTests.cs(47,33): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/GameServerStartupTests.cs(47,33): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/GameServerStartupTests.cs(62,71): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/GameWorldSettingsIsolationTests.cs(61,16): warning CS8605: Unboxing a possibly null value.
Goose.Tests/GameWorldSettingsIsolationTests.cs(61,21): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/InvisibilityTransitionTests.cs(374,17): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/LoginEventNameLengthTests.cs(37,64): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/LoginEventNameLengthTests.cs(37,64): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/MapCanPlayerJoinTests.cs(15,55): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(15,61): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(15,67): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(21,70): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(21,76): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapCanPlayerJoinTests.cs(21,82): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapPlayerCanJoinHookTests.cs(55,37): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/MapPlayerCanJoinHookTests.cs(92,70): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/PacketCurrencyTests.cs(24,62): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/PacketCurrencyTests.cs(95,35): warning CS8604: Possible null reference argument for parameter 'arg1' in 'string Func<Window, ItemTemplate, GameWorld, int, long, string>.Invoke(Window arg1, ItemTemplate arg2, GameWorld arg3, int arg4, long arg5)'.
Goose.Tests/PlayerSendTests.cs(24,64): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/PlayerSendTests.cs(24,64): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/PreLoginReassemblyTests.cs(118,55): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.Tests/PreLoginReassemblyTests.cs(118,55): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/QuestScriptTests.cs(40,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(41,62): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(42,56): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(43,47): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(44,35): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/QuestScriptTests.cs(44,49): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/ScriptLoadDirectiveTests.cs(50,16): warning CS8605: Unboxing a possibly null value.
Goose.Tests/ScriptLoadDirectiveTests.cs(50,21): warning CS8602: Dereference of a possibly null reference.
Goose.Tests/VendorPurchaseCurrencyTests.cs(36,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(49,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(65,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(83,30): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(100,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/VendorPurchaseCurrencyTests.cs(117,32): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.Tests/WhoEventTests.cs(30,87): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(12,63): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(56,96): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(113,92): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(127,78): warning CS8625: Cannot convert null literal to non-nullable reference type.

Goose.IntegrationTests (13, separate build — project not in Goose.sln):

Goose.IntegrationTests/DimensionItemScriptTests.cs(11,82): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionItemScriptTests.cs(93,21): warning CS8600: Converting null literal or possible null value to non-nullable type.
Goose.IntegrationTests/DimensionItemScriptTests.cs(152,50): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionMapScriptTests.cs(244,23): warning CS8618: Non-nullable field 'Refusal' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
Goose.IntegrationTests/DimensionRebirthTests.cs(212,49): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionsScriptTests.cs(603,54): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionsScriptTests.cs(618,54): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/DimensionsScriptTests.cs(632,54): warning CS8625: Cannot convert null literal to non-nullable reference type.
Goose.IntegrationTests/PlayerPropertiesPersistenceTests.cs(56,41): warning CS8604: Possible null reference argument for parameter 'json' in 'void Player.LoadPropertiesFromColumn(string json)'.
TestSupport/TestWorldFixture.cs(12,63): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(56,96): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(113,92): warning CS8625: Cannot convert null literal to non-nullable reference type.
TestSupport/TestWorldFixture.cs(127,78): warning CS8625: Cannot convert null literal to non-nullable reference type.
