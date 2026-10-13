# Dimension Item Progression Parity Implementation Plan

**Goal:** Match Abyss’s dimension-item progression rules by gating dimension-5/6 equipment, cloning and pricing every `NoUse` item, and keeping map keys, Goose quests, and crafting correct when item IDs encode dimensions.

**Architecture:** Give generated item templates explicit runtime-only family/dimension metadata instead of making core systems decode the dimension ID offset. The dimensions script remains the source of generated variants and scaled prices; core inventory, quest, map, and crafting paths use the metadata only where they need family-aware behavior. NPC combat formulas and the already-fixed vendor currency-resolution order do not change.

**Tech Stack:** C#/.NET, Roslyn `.csx` scripts, xUnit unit and integration tests, Goose runtime registries.

---

## Scope and decisions

- Clone all `NoUse` templates, including zero-value keys and quest materials, in dimensions 1–6. Current sheet data has 99 `NoUse` templates; 53 have positive gold value.
- Preserve base item IDs and behavior. A generated variant gets `BaseTemplateID == base.ID` and an explicit `Dimension`; this metadata is runtime-only and reconstructs at server startup.
- Scale `Value` and `Credits` by `3^dimension` exactly as existing dimension equipment does. Currency choice remains item override → vendor currency → gold; dimension does not imply spirit.
- Apply Abyss’s minimum-experience override to generated equipment only: dimension 5 requires 100,000,000,000 total experience; dimension 6 requires 500,000,000,000. Keep the formula valid for later dimensions (`500,000,000,000 × (dimension - 5)` for dimension 6+). `NoUse` and tome clones do not acquire an unusable equipment gate.
- Donation credits are a Goose extension, not Abyss SP value, so credit-priced equipment receives the same dimension-5/6 equipment gate.
- A dimension-N map key gate accepts the same item family from dimension N or higher, and rejects the base/lower-dimensional item. This matches `Map.java:595-601` in Abyss.
- Goose item quests have no Abyss equivalent. Preserve Goose functionality by allowing any dimension variant in the required item family; when the quest consumes items, take base/lower-dimension variants first so a generic quest does not unnecessarily destroy a higher-tier item. Quest rewards remain authored base items because a Goose quest does not record an acceptance/completion dimension.
- Crafting matches recipes by base template IDs, requires all dimension-capable ingredients used for one craft to come from one common dimension, leaves other dimensions untouched, and creates that dimension’s result variant when one exists. Non-dimensional ingredients remain dimension 0; therefore they cannot be combined with dimension-N ingredients into a dimension-N recipe, matching Abyss’s same-dimension search.
- Do not change NPC HP/damage formulas. The prior audit found the dimension-1 spike comes from Abyss’s fixed floors rather than duplicate scaling.
- Do not add schema columns or migrations. Do not add comments/docstrings unless a non-obvious invariant needs one; remove or rewrite existing comments made false by this change.
- Keep the existing cold-start lifecycle boundary: generated dimension registries are not rebuilt safely by `/reloadscripts` or `/reloadsql`, so dimension data changes require a full server restart. Document this constraint rather than expanding this change into hot-reload redesign; replacing registered template objects would strand already-loaded `Item.Template` references.

## APIs verified before planning

- `ItemTemplate(ItemTemplate)` copies every runtime field explicitly and must be extended for new metadata: `Goose/ItemTemplate.cs:119-162`.
- Generated templates are published through `ItemHandler.AddTemplate`; exact-ID lookup remains `GetTemplate(int)`: `Goose/ItemHandler.cs:185-213`.
- The dimension script creates item clones in `ScaleItemTemplate`, publishes them, and then rewires drops and vendor stock: `Goose/Data/Illutia/Scripts/Global/Dimensions/Items.csx:90-145`, `Goose/Data/Illutia/Scripts/Global/Dimensions.csx:206-219`.
- `Player.CanUse` enforces `ItemTemplate.MinExperience` against banked plus sold experience: `Goose/Player.cs:1648-1681`.
- Exact inventory presence `HasItem(int)` scans ordinary inventory, equipped slots, and the combine bag; counted `HasItem(int,long)`/`GetItemCount` scan ordinary inventory only and exclude `Item.Custom`: `Goose/Inventory.cs:883-933`. Exact removal is `RemoveItem(int,long,GameWorld)`: `Goose/Inventory.cs:553-587`.
- Quest item readiness calls exact `Inventory.HasItem`: `Goose/Quests/QuestStateResolver.cs:48-103`. Progress text independently calls exact `GetItemCount`: `Goose/Quests/QuestWindow.cs:295-313`. `CompleteQuest` currently mutates completion state before `TakeRequirements`, whose item path removes the exact authored template ID while script requirements call `OnTakeRequirement`: `Goose/Quests/QuestWindow.cs:385-410,570-603`.
- Script rewards are preflighted through `GetRequiredInventorySpace` and `CanComplete`, then delivered through `GiveReward`: `Goose/Quests/QuestWindow.cs:355-383,413-550`. The shipped class reward resolves the player’s class to an authored template ID and creates that base item: `Goose/Data/Illutia/Scripts/Quest/ClassReward.csx:29-70`.
- `Map.PlayerCanJoin` runs the map script before level/experience and its exact required-item loop: `Goose/Map.cs:697-752`. `Map.CloneAs` copies the private required-item list: `Goose/Map.cs:92-112`.
- `CloneMaps` fully configures a clone, publishes it by assigning `world.MapHandler.Maps[clone.ID]`, then schedules its sweep event: `Goose/Data/Illutia/Scripts/Global/Dimensions/Maps.csx:52-92`; `MapHandler.Maps` is the canonical dictionary: `Goose/MapHandler.cs:16-27,89-92`.
- `DimensionMap.CanPlayerJoin` already owns dimension-specific entry policy and delegates to the base map script: `Goose/Data/Illutia/Scripts/Global/Dimensions/DimensionMap.csx:20-33`.
- Recipe definitions remain base-ID keyed in `Combination.RequiredHash`, and `CombinationHandler.GetMatch` accepts a hash of template ID to quantity: `Goose/Combination.cs:6-23`, `Goose/CombinationHandler.cs:125-167`.
- `Inventory.Combine` currently hashes, consumes, and creates results with exact IDs; it stages the new container and result items before committing: `Goose/Inventory.cs:1156-1339`.
- `Item` retains its resolved `ItemTemplate` at runtime while persisting `TemplateID`, so family/dimension metadata needs no item-row migration: `Goose/Item.cs:24-32`, `Goose/Item.cs:146-165`.
- Abyss dimension items retain the base template ID and store dimension separately: `../abyssserver/src/Abyss/Item.java:432-435`. Its crafting matches base IDs, searches for one complete dimension, and produces results in that dimension: `../abyssserver/src/Abyss/CombinationHandler.java:115-135`, `../abyssserver/src/Abyss/Inventory.java:844-960`.
- Abyss item minimum experience is 100B at dimension 5 and `500B × (dimension - 5)` at dimension 6+: `../abyssserver/src/Abyss/Item.java:288-295`.
- Abyss’s legacy quests have no item requirements or rewards; Goose family-aware quest behavior is explicitly an extension, not claimed parity: `../abyssserver/src/Abyss/QuestTemplate.java:3-53`, `../abyssserver/src/Abyss/Quests.java:182-200`.
- `/reloadscripts` clears scripts and reruns global loading while generated IDs remain registered; `/reloadsql` reloads authored templates without regenerating live clones: `Goose/Commands/ReloadScriptsCommand.cs:13-21`, `Goose/GameWorld.cs:871-879`, `Goose/Commands/ReloadSqlCommand.cs:19-28`. This plan therefore documents restart-only reconstruction rather than promising unsafe hot reload.

### Task 1: Add runtime item-family metadata and variant lookup

**Files:**
- Modify: `Goose/ItemTemplate.cs:58-162`
- Modify: `Goose/ItemHandler.cs:185-213`
- Test: `Goose.Tests/ItemHandlerRegistrationTests.cs`

**Mutation impact:**
- Source of truth changed: registered `ItemTemplate` objects in `ItemHandler.templates` (`Goose/ItemHandler.cs:17-30`).
- Important readers: inventory items retain the template reference (`Goose/Item.cs:24-32`); later tasks use metadata in inventory queries, map gates, quests, and crafting.
- Derived/cached state affected: none. `GetTemplateVariant` reads the canonical template dictionary; no second index can drift.
- Required propagation sequence: construct/copy template → set generated variant metadata → validate → publish with `AddTemplate` → resolve by `(BaseTemplateID, Dimension)`.
- Invariants to preserve: authored templates are family roots at dimension 0; copies carry metadata; exact-ID lookup is unchanged; failed lookup returns null; no persistence change.
- Observable proof required: real registered templates round-trip through exact and family/dimension lookup, and the copy constructor preserves metadata.

**Step 1: Write failing unit tests**

Add tests covering:

1. A normal template reports its own ID as `BaseTemplateID` and dimension 0.
2. A generated template with `VariantOfTemplateID = 50`, `Dimension = 3`, and `ID = 300050` resolves from `GetTemplateVariant(50, 3)`.
3. Copying that generated template preserves both fields.
4. Missing dimensions return null, and exact `GetTemplate(300050)` still returns the same object.

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter ItemHandlerRegistrationTests`

Expected red result: the metadata and variant lookup API do not exist.

**Step 2: Implement the minimal metadata API**

Add runtime properties with these contracts:

- `int? VariantOfTemplateID`: null only for a family root.
- `int Dimension`: zero for authored/non-dimensional templates.
- `int BaseTemplateID`: computed as `VariantOfTemplateID ?? ID`.
- `ItemTemplate? ItemHandler.GetTemplateVariant(int baseTemplateId, int dimension)`: exact root lookup for dimension 0; for higher dimensions, return the registered template whose base/dimension metadata matches; return null when absent.

Extend the explicit copy constructor. Do not serialize these fields or add database columns.

**Step 3: Run focused tests and commit**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter ItemHandlerRegistrationTests`

Expected: PASS.

Commit: `feat(items): track generated template families`

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Base templates remain dimension 0 roots | new base-template metadata test |
| Generated variants resolve independently of encoded IDs | new family/dimension lookup test |
| Copying cannot erase identity metadata | new copy-constructor test |
| Missing variants fail without fallback | adversarial missing-dimension test |

### Task 2: Clone every NoUse item and add high-dimension equipment gates

**Files:**
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Items.csx:57-145`
- Test: `Goose.IntegrationTests/DimensionItemTemplateTests.cs:16-192`
- Test: `Goose.IntegrationTests/DimensionDropTests.cs:7-75`
- Test: `Goose.IntegrationTests/DimensionVendorStockTests.cs:22-355`

**Mutation impact:**
- Source of truth changed: generated templates published into `ItemHandler`; cloned NPC drop entries and vendor slots point at those templates.
- Important readers: item creation/persistence, vendor currency resolution, vendor packets, NPC drops, modifier rolling, `Player.CanUse`, later map/quest/crafting tasks.
- Derived/cached state affected: cloned `NPCDropInfo` and `NPCVendorSlot` entries are derived from template availability. No persistence migration is needed because saved items retain encoded `TemplateID` and variants regenerate on startup.
- Required propagation sequence: snapshot base templates → compute and collision-check every target ID before mutation → construct and validate every variant in a temporary list → publish the complete list with `AddTemplate` → rewire drops → rewire vendor stock. A preflight/construction failure leaves no generated item template visible; never publish a clone under an existing ID.
- Invariants to preserve: base templates/drop tables/vendor slots remain unchanged; `OneTime`, money, and non-learning scroll behavior remains unchanged; `NoUse` gets no equipment stats or random dimension modifiers; explicit `CurrencyId` remains copied; credits and gold values scale independently.
- Observable proof required: tests inspect final registered templates and final NPC/vendor references, not only `ShouldClone`.

**Step 1: Write failing integration tests**

Update the obsolete “does not clone NoUse” assertion and add coverage for:

1. A positive-value `NoUse` template receives six variants and dimension 3 has `Value == base × 27`.
2. A zero-value key also receives six variants.
3. `NoUse` clone stats equal the base stats and the clone does not receive equipment suffix/rarity changes when its item script roll hook runs.
4. A dimension NPC’s `NoUse` drop points at the same-dimension variant; base and `OneTime` drops remain unchanged.
5. A dimension vendor’s `NoUse` stock points at the same-dimension variant and resolves to gold unless item/vendor currency says otherwise.
6. Equipment keeps its authored minimum through dimension 4, then dimension 5 is exactly 100B and dimension 6 exactly 500B. Exercise `Player.CanUse` immediately below and at each threshold.
7. A credit-priced equipment clone is still gated, proving credits are not treated as Abyss SP exemption.
8. A collision at a later target ID throws before any earlier item clone is published.

Run:

`dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~DimensionItemTemplateTests|FullyQualifiedName~DimensionDropTests|FullyQualifiedName~DimensionVendorStockTests"`

Expected red result: `NoUse` variants are absent and equipment retains base `MinExperience`.

**Step 2: Implement cloning and gates**

- Include every `UseTypes.NoUse` template in `ShouldClone`.
- Split clone generation from publication: preflight every target ID and build/validate all clones before the first `AddTemplate` call.
- Set `VariantOfTemplateID` and `Dimension` in `ScaleItemTemplate`.
- Set `MinExperience` only for Armor/Weapon clones at dimension 5+, using the verified Abyss thresholds/formula.
- Keep existing value/credit multiplication and equipment-only stat scaling.
- Remove or rewrite the comments that say `NoUse` items or quest tokens remain unscaled.

**Step 3: Run focused tests and commit**

Run the focused command from Step 1.

Expected: PASS.

Commit: `feat(dimensions): scale NoUse items and gate endgame gear`

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| All NoUse templates, including zero-value keys, clone | positive-value and zero-value tests |
| NoUse does not become equipment | stat/modifier adversarial test |
| Drops/vendors observe the new variants | final NPC drop and vendor slot tests |
| Dimension 5/6 equipment gates are exact | boundary `CanUse` tests |
| Donation credits do not bypass progression | credit-equipment gate test |

### Task 3: Add separate family-presence and quest-inventory operations

**Files:**
- Modify: `Goose/Inventory.cs:507-587,883-933`
- Modify: `Goose/Quests/QuestStateResolver.cs:48-104`
- Modify: `Goose/Quests/QuestWindow.cs:295-333,385-410,570-603`
- Test: create `Goose.Tests/ItemFamilyInventoryTests.cs`
- Test: `Goose.Tests/QuestIconResolverTests.cs`
- Test: create `Goose.Tests/QuestWindowItemRequirementTests.cs`
- Test: `Goose.Tests/ClassRewardScriptTests.cs`

**Mutation impact:**
- Source of truth changed: ordinary inventory slot stacks in `Inventory.inventory`; quest definitions remain authored against base IDs.
- Important readers: map-key presence checks, quest-state/icon resolution, player-visible quest progress text, quest completion, inventory packets, and quest icon refresh.
- Derived/cached state affected: client inventory slots and NPC quest icons. Quest removal sends each changed slot but suppresses intermediate icon refresh; `CompleteQuest` keeps its existing single final `QuestHandler.RefreshIcons` call.
- Required propagation sequence: resolve/group item rows identically for readiness, progress text, and turn-in → show one summed line per valid family → require the sum of kept and consumptive rows → preflight every family before mutation → remove the summed non-kept quantities in dimension/slot order → continue completion/rewards → perform the existing final icon refresh.
- Invariants to preserve: existing exact `HasItem`/`RemoveItem` semantics do not change; map presence keeps the existing inventory + equipped + combine-bag search and permits custom items; quest quantity checks use ordinary inventory only and exclude `Item.Custom`; variants may contribute to one Goose quest family total; lower dimensions are consumed first; kept quantities remain after consumptive rows of the same family; `RequirementType.Script` hooks and every `RewardType.Script` preflight/delivery hook remain in their existing order.
- Observable proof required: assert final slot stacks/template IDs and final quest readiness with real containers, not mocked helper calls.

**Step 1: Write failing tests for distinct family APIs**

Define and test two deliberately different surfaces:

- `HasItemFamily(int baseTemplateId, int minimumDimension)`: presence in ordinary inventory, equipped slots, or combine bag, preserving the current map-key semantics of `HasItem(int)`.
- `GetInventoryItemFamilyCount(int baseTemplateId)`: quantity in ordinary inventory only, excluding `Item.Custom`, preserving quest semantics of `GetItemCount`/`HasItem(id,count)`.
- `TryRemoveInventoryItemFamily(int baseTemplateId, long numberOf, GameWorld world)`: preflight the whole quantity, return false with no mutation when short, otherwise consume dimension then slot order and send changed inventory slots; it intentionally does not refresh quest icons because the completion workflow owns the single final refresh.

Use real `Item`, `ItemTemplate`, inventory, equipped, and combine containers. Cover mixed base/d1/d2 stacks, minimum-dimension presence in each container, unrelated families, custom-item inclusion for presence but exclusion for quest count/removal, exact requested removal, and base-first removal. Add an adversarial test where an earlier high-dimension slot exists but enough base items exist later; the high item must survive.

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter ItemFamilyInventoryTests`

Expected red result: the family APIs do not exist.

**Step 2: Implement the family operations**

All matching reads use `slot.Item.Template.BaseTemplateID` and `Dimension`. Keep exact-ID methods untouched. The removal method builds its ordered candidate set and verifies the total before changing any slot; after successful preflight it applies the exact remaining amount and sends every changed slot. It neither refreshes quest icons nor mutates quest state.

**Step 3: Aggregate Goose item requirements before readiness or removal**

- In `QuestStateResolver.MeetsRequirements`, resolve each authored requirement ID through `ItemHandler.GetTemplate`, fail closed when it is missing, group rows by the resolved `BaseTemplateID`, and require the sum of all row quantities with `GetInventoryItemFamilyCount`; evaluate non-item rows through the existing per-requirement switch.
- In `QuestWindow.GetQuestProgressText`, resolve valid item rows to their family, sum required quantities, and render one line per family using `GetInventoryItemFamilyCount`. Preserve a stable `Unknown item` line and fail-closed behavior for each unresolved authored template instead of merging it into a valid family.
- In `QuestWindow`, resolve and build the same valid-family groups, preflight every family total before marking the quest complete, and remove only the summed quantities from rows where `KeepRequirement == false`. Requiring total kept + consumed quantity guarantees kept rows still remain afterward. Remove the old per-row item branch from `TakeRequirements`, but leave gold, progress, experience, and especially `RequirementType.Script.OnTakeRequirement` handling unchanged.
- Abort completion without quest/reward/inventory mutation if any grouped preflight fails. The family remover does not refresh icons; the existing final completion refresh remains the single notification.
- Add resolver, progress-renderer, and completion-path tests showing a dimension `NoUse` item satisfies, displays, and is consumed by an authored base-ID requirement.
- Add adversarial duplicate-family tests: the progress window shows one summed line; two rows cannot double-count one short stack; kept plus consumed rows leave the kept quantity; custom items neither satisfy nor get consumed. Retain missing-template progress coverage.
- Add an end-to-end regression using the shipped `ClassReward.csx`: a quest with a dimension `NoUse` family requirement still runs the script reward’s `CanComplete` and inventory-space preflights, consumes the requirement, and gives the item mapped to the player’s class. Keep the reward as its authored base template; quests have no dimension context from which to select a reward variant.

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter "ItemFamilyInventoryTests|QuestIconResolverTests|QuestWindowItemRequirementTests|ClassRewardScriptTests"`

Expected: PASS.

**Step 4: Commit**

Commit: `feat(quests): recognize dimension item families`

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Map-style presence still scans all three containers | inventory/equipped/combine presence tests |
| Quest counts exclude custom items and non-inventory containers | custom/container adversarial tests |
| Duplicate requirement rows cannot double-count or misreport progress | grouped-family readiness and progress-text tests |
| Variant progress agrees with readiness | variant-only progress-text test |
| Failed grouped removal is atomic | short-family completion test |
| Lower tiers are protected from arbitrary slot order | adversarial base-first removal test |
| Kept requirements remain after same-family consumption | kept + consumed grouped test |
| Scripted requirement/reward hooks remain compatible | shipped class-reward end-to-end test |

### Task 4: Enforce same-or-higher dimension map keys in the core gate

**Files:**
- Modify: `Goose/Map.cs:92-112,697-752`
- Modify: `Goose/Data/Illutia/Scripts/Global/Dimensions/Maps.csx:52-93`
- Test: `Goose.IntegrationTests/DimensionsScriptTests.cs:344-361`
- Test: `Goose.IntegrationTests/DimensionMapScriptTests.cs`

**Mutation impact:**
- Source of truth changed: base and generated maps retain their authored `RequiredItems`; a runtime-only map field records the minimum item-family dimension for the core gate.
- Important readers: `Map.PlayerCanJoin`, warp movement, teleport effects, and the existing `DimensionMap` wrapper/base-script delegation.
- Derived/cached state affected: generated map objects only. No database/persistence state changes.
- Required propagation sequence: preflight every base required item as cloneable before publishing any map clone → clone the map with its existing requirements → set the clone’s required-item minimum dimension → publish the fully initialized clone → run the existing script, level, and experience checks → in the existing core required-item loop call all-container `HasItemFamily` for generated maps and exact `HasItem` for base maps.
- Invariants to preserve: map-script-versus-core-check ordering stays unchanged; base-map exact gates are unchanged; GM `IgnoreMapRequirements` bypass remains before all checks in `Map.PlayerCanJoin`; base/lower keys cannot enter a higher map; higher keys can enter lower maps; requirements are not consumed.
- Observable proof required: call real `Map.PlayerCanJoin` on generated clones with real inventory contents so the script and core ordering are exercised.

**Step 1: Write failing integration tests**

For a base map requiring a cloned `NoUse` key, assert on its dimension-3 clone:

- base and dimension-2 keys are rejected;
- dimension-3 and dimension-4 keys are accepted from ordinary inventory;
- qualifying keys in an equipped slot or the combine bag are also accepted, preserving existing `HasItem(int)` behavior;
- no key is rejected with the existing item name in the refusal;
- the base map still accepts only the exact base key;
- a refusal from the delegated base map script still wins before required-item and level/experience failures;
- a malformed requirement whose template cannot be cloned aborts dimension loading before any map clone is published.

Retain the existing assertion that cloned maps carry the authored required-item IDs; add an assertion for the clone’s minimum required dimension.

Run: `dotnet test Goose.IntegrationTests/Goose.IntegrationTests.csproj --filter "FullyQualifiedName~DimensionMapScriptTests|FullyQualifiedName~DimensionsScriptTests"`

Expected red result: the clone’s core exact-ID check rejects valid variants.

**Step 2: Implement the core gate**

- Add `Map.RequiredItemMinimumDimension`, defaulting to 0, and copy it in `CloneAs`; this is runtime metadata, not persisted map data.
- Add an all-base-map requirement preflight before `CloneMaps` mutates or publishes any map clone; resolve every required template and apply the same `ShouldClone` predicate used by item generation so the gate and clone set cannot drift.
- Set each generated clone’s value to its dimension before `world.MapHandler.Maps[clone.ID] = clone` publishes it; keep the existing post-publication `ClearMapItemsEvent` scheduling unchanged.
- In the existing `Map.PlayerCanJoin` required-item loop, use exact `HasItem` when the minimum is 0 and `HasItemFamily(requiredId, minimumDimension)` otherwise. Keep the loop after script/level/experience checks and keep refusal sending in `Map.PlayerCanJoin`.
- Do not move the gate into `DimensionMap.CanPlayerJoin` and do not clear copied requirements; either would change established check ordering or risk a second exact-ID rejection.

**Step 3: Run focused tests and commit**

Run the focused command from Step 1.

Expected: PASS.

Commit: `fix(dimensions): make map keys dimension aware`

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| Same/higher key opens dimension map | d3/d4 acceptance tests |
| Base/lower key cannot bypass progression | adversarial base/d2 rejection tests |
| Base map behavior remains exact | base-map regression test |
| Existing script/core ordering remains stable | delegated-script precedence test |
| Invalid generated gate cannot silently strand a map | load-time preflight/no-publication test |

### Task 5: Craft one complete item dimension and produce that dimension

**Files:**
- Modify: `Goose/Inventory.cs:1152-1339`
- Modify: `Goose/ItemHandler.cs:185-213` only if Task 1 lookup needs a focused adjustment
- Test: `Goose.Tests/CombineBagTests.cs`

**Mutation impact:**
- Source of truth changed: the staged combine container and newly created result template selection.
- Important readers: `CombinationHandler.GetMatch`, combine-bag packet updates, item registration/creation hooks, lore checks, and stack merging.
- Derived/cached state affected: `newcombine`, free-slot accounting, and staged result registrations. Keep the existing atomic publication boundary: do not replace the live combine container or register results until all requirements and result-space checks pass.
- Required propagation sequence: build one aggregate base-ID hash across every combine-bag slot → let `CombinationHandler.GetMatch` choose the most-specific authored recipe → search dimensions in ascending order for a complete exact-dimension set for that recipe → enforce player requirements → consume only that dimension into the staged container → resolve and fully construct every result against the same dimension (fall back to its authored dimension-0 template only when no variant exists) → if any result resolution/load/capacity check fails, discard staged state and return → otherwise stack/stage all results → register own-slot results → publish the staged container/send slots.
- Invariants to preserve: no mixed-dimension recipe; aggregate recipe specificity matches Abyss and existing Goose behavior; incomplete or invalid-result attempts leave the bag byte-for-byte equivalent in contents; surplus and other-dimension items remain; duplicate quantities are honored; results still stack without losing leftovers; dimension-0 crafting is unchanged.
- Observable proof required: inspect final combine-bag slots and result template IDs after real `Inventory.Combine` calls.

**Step 1: Write failing combine-bag tests**

Add tests for:

1. Complete dimension-2 `NoUse` ingredients match a base-ID recipe and produce the dimension-2 Armor/Weapon/NoUse result variant.
2. Mixed dimension-1 and dimension-2 ingredients cannot collectively satisfy one recipe; bag contents remain unchanged.
3. A complete dimension-1 set plus unrelated dimension-2 surplus crafts dimension 1 and preserves dimension-2 slots.
4. A recipe containing a non-dimensional `OneTime` ingredient cannot combine with dimension-1 ingredients; this pins strict common-dimension parity.
5. If a result family has no variant, the authored dimension-0 result is created.
6. When a smaller recipe is complete at dimension 0 but the aggregate bag matches a more-specific recipe whose complete set is at dimension 1, the more-specific recipe/dimension wins. When that selected recipe has no complete single dimension, crafting fails rather than falling back to the smaller recipe.
7. A missing or unloadable result aborts without consuming ingredients or publishing partial results.
8. Existing duplicate-count, most-specific-recipe, full-bag atomicity, leftover, and result-stacking tests remain green.

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter CombineBagTests`

Expected red result: variant IDs do not match any recipe, or mixed variants use the wrong exact-ID behavior.

**Step 2: Refactor matching/consumption around family identity**

Use small private helpers with explicit contracts:

- An aggregate hash builder reads all combine slots, returns base-template quantities, and does not mutate/send/register. Its output goes through the existing `CombinationHandler.GetMatch` once so recipe specificity is chosen before dimension search, as in Abyss.
- A dimension hash builder reads one exact `ItemTemplate.Dimension`, returns base-template quantities, and does not mutate/send/register. Search only for a dimension that completely satisfies the already-selected recipe.
- A result resolver returns the same-dimension registered variant when one exists and the authored template otherwise; it does not publish items.
- Result construction is part of the staged preflight. Replace the current `LoadFromTemplate` failure `continue` path with an abort that leaves the live bag and item registries untouched.

Keep recipe definitions and `/recipes` output base-only. Preserve the existing staged publication boundary and recent result-stack fix.

**Step 3: Run focused tests and commit**

Run: `dotnet test Goose.Tests/Goose.Tests.csproj --filter CombineBagTests`

Expected: PASS.

Commit: `feat(crafting): preserve dimension item families`

**Invariant-to-test matrix:**

| Invariant | Proved by |
|---|---|
| One craft uses one complete dimension | mixed-dimension adversarial test |
| Recipe specificity is chosen before dimension search | competing recipes across dimensions test |
| Output inherits selected dimension | dimension-2 output test |
| Other dimensions and surplus survive | unrelated-surplus test |
| Failure is atomic | mixed/incomplete, invalid-result, and existing full-bag tests |
| Base crafting and recipe UI remain unchanged | existing combine tests; no combination registry mutation |

### Task 6: Update operator documentation and run final verification

**Files:**
- Modify: `docs/dimensions.md:212-235,264-275`

**Mutation impact:**
- Source of truth changed: operational documentation only.
- Important readers: maintainers configuring and debugging dimensions.
- Derived/cached state affected: none.
- Required propagation sequence: update the item clone set, minimum-experience gates, map-key rule, Goose quest-family rule, and same-dimension crafting behavior after tests prove them.
- Invariants to preserve: currency documentation continues to say dimension scales prices but does not choose spirit/gold/credits; archived historical plans remain untouched.
- Observable proof required: review current docs against the tested behavior and run repository searches for now-false current-code comments.

**Step 1: Update current documentation**

Document:

- Armor, Weapon, learning Scroll, and all `NoUse` cloning.
- Equipment-only dimension-5/6 experience thresholds.
- `NoUse` gold/credit price scaling without currency changes.
- map required-item minimum dimensions;
- Goose generic quest family matching/low-first removal;
- strict same-dimension crafting and result propagation;
- full-restart requirement after dimension item/script/SQL data changes; `/reloadscripts` and `/reloadsql` do not safely reconstruct live generated variants.

Do not rewrite archived design/implementation plans.

**Step 2: Verify stale statements**

Search current code and `docs/dimensions.md` for claims that `NoUse`/quest items are not cloned or that dimension forces spirit. Fix only statements made false by this implementation.

**Step 3: Run complete verification**

Run: `dotnet test Goose.sln`

Expected: all currently enabled tests pass; existing skipped tests remain skipped. The rebased baseline is 1,758 unit tests, 471 integration tests, and 146 tools tests passing, with 26 tools tests skipped. Read-only NuGet vulnerability-cache `NU1900` warnings are non-fatal in this environment.

**Step 4: Manual startup smoke**

Start the normal server configuration once and verify dimension script load completes without ID/preflight errors. On a disposable character/world:

- inspect a dimension-1 `NoUse` vendor item and confirm scaled value with the vendor’s resolved currency;
- verify a dimension-3 key cannot open a dimension-4 map but a dimension-4 key can;
- combine a complete dimension material set and inspect the result template/prefix;
- attempt to equip dimension-5 equipment below 100B total experience and confirm refusal.

No separate server or GUI behavior is needed for the implementation plan itself.

**Step 5: Commit**

Commit: `docs(dimensions): document item progression parity`

## Final acceptance checklist

- [ ] All 99 current `NoUse` templates generate dimensions 1–6 without giving non-equipment stat rolls.
- [ ] Gold value and donation credits scale independently; currency resolution remains unchanged.
- [ ] Dimension-5 equipment requires 100B total experience and dimension-6 equipment requires 500B.
- [ ] Map keys require the map dimension or higher across inventory/equipment/combine bag; base maps retain exact authored behavior.
- [ ] Goose item quests aggregate duplicate family rows, exclude custom/non-inventory items, preserve kept quantities, and consume lower dimensions first.
- [ ] Crafting selects the most-specific base recipe, rejects incomplete mixed dimensions atomically, and emits the selected dimension’s result.
- [ ] No database migration or persisted JSON shape change is introduced; full restart remains required to rebuild generated registries.
- [ ] NPC scaling formulas remain unchanged.
- [ ] Full unit/integration/tools suite passes.
