# Dimensions Part 6 — The Spirit Economy — Design

Date: 2026-08-13

## Summary

Close the dimension loop: give spirit a faucet, sinks and something to buy. Part 5 built
the currency abstraction and registered spirit; nothing yet mints it except selling loot,
and nothing spends it except a vendor that stocks the wrong goods.

This part adds rebirth (experience → spirit), four spirit commands, and dimension vendor
stock repointing. It also closes a Part 1 fidelity gap in the dimension map entry gates.

Source of truth for behaviour is `~/code/abyssserver` (Java). Line references point at it
or at this repository, as marked.

## Scope

**In scope**

- Five server extension points, all generic and additive
- Rebirth as a script-created NPC and repeatable quest
- `/resetitem`, `/buygold`, `/buyexperience`, `/givesp`
- Dimension vendor stock repointing
- Abyss's flat dimension-5/6 minimum experience floors (Part 1 gap)

**Out of scope**

- Cosmetic spirit sinks (`/setcolor`, custom graphic, custom weapon type)
- Crafting restricted to a single dimension
- Scriptable or dynamic pricing
- Any change to how spirit is earned from vendor sales

## Decisions

| Decision | Value | Rationale |
|---|---|---|
| Rebirth carrier | Script-created NPC + quest, in `Dimensions.csx` | User decision. The dimensions feature stays self-contained, and `Enabled = false` remains a genuine off switch — a sheet-authored NPC would linger with the feature disabled |
| Rebirth conversion | One-shot, converts everything | User decision. `floor(total / 100M)` spirit; the sub-threshold remainder is destroyed, faithful to `RebirthEvent.java:47` |
| Rebirth repeatable | Yes | The only renewable spirit faucet. Quest 60 is already `repeatable = 1`, so the engine supports it |
| Class-change experience loss | Disabled for rebirth | User decision. Rebirth is an exchange, not a tax; the only experience destroyed is the sub-100M remainder |
| Rebirth placement | Dimension 0 only | Rebirth requires stripping naked and leaves the player at level 1, and every dimension above 0 has `CanPVP` forced on |
| Exchange rates | Abyss's, as configurable constants | 1 spirit = 1M gold, 1 spirit = 25M experience, `/resetitem` = `3^dim` |
| Command registration | Script, via `EventHandler.RegisterEvent` | User decision. Same as `/dimension` (`Dimensions.csx:121`); nothing dimension-specific enters the server |
| Server/script split | Two generic hooks plus three overloads | User decision. See [Server changes](#server-changes) |
| Vendor stock | Repoint to same-dimension clones where one exists | User decision. Mirrors Part 4's drop-table pass; consumables and money keep the base template |
| Dimension-0 vendors buying dimension loot | Allowed, as today | User decision. Abyss blocked it; goose's item-level currency resolution already permits it and the payout is identical anywhere |
| Dimension 5/6 minimum experience | Flat 100B / 500B floors | Ports `Map.java:251-260`, which shipped Part 1 omitted |
| `/givesp` negative amounts | Dropped | Abyss let GMs confiscate spirit (`GiveSPCommandEvent.java:40`). Not ported |

---

## Server changes

Five changes. All are additive; every existing call site is untouched.

### 1. `Player.ChangeClass` overload

```csharp
public void ChangeClass(int classid, int newLevel, GameWorld world, decimal experienceLossPercent)
```

The existing three-argument method becomes a delegation passing
`GameWorld.Settings.ChangeClassExperienceLossPercent`, so quest 60 ("Class Removal",
`QuestWindow.cs:438`) behaves exactly as today. Rebirth passes `0`.

`ChangeClass` (`Player.cs:1358-1400`) already does everything abyss's `RebirthEvent`
inlines: `RemoveStats`/`AddStats`, the `MaxStats` adjustment against the old class level
row, banking `Experience` into `ExperienceSold`, the level-1 class row, `BaseStats.HP/MP =
0`, `Spellbook.RemoveNonClassSpells`, the bind reset to `StartingMapID`, and the
`StatusInfo`/`ExpBar` packets. Only the loss percent is in the way.

### 2. `Player.AddExperience` overload

```csharp
public virtual void AddExperience(long exp, GameWorld world, ExperienceMessage message, bool applyModifiers)
```

The existing three-argument method delegates with `true`.

**Why this is required.** `AddExperience` re-multiplies the amount by
`world.ExperienceModifier` on a branch selected by whether the player is past
`Settings.ExperienceModifierLimit` (`Player.cs:1662-1671`). Abyss inverted a flat modifier
by dividing (`BuyExperienceCommandEvent.java:52`); goose's two-branch version cannot be
inverted reliably from script, and the players buying experience are precisely the ones
past the limit. Purchased experience passes `false` and the player gets exactly what they
paid for.

### 3. `ItemHandler.ResetModifiers(Item)`

Returns an item to its template state: `Name = Template.Name`, `BaseStats = new
AttributeSet()`, `StatMultiplier = 1`, clears `ItemProperties[TitleId]` and
`[SurnameId]`, then `RefreshStats()`.

It deliberately does **not** reuse `Item.LoadFromTemplate`, which accumulates rather than
assigns (`TotalStats += Template.BaseStats`, `Item.cs:159`) and would double-count on a
second call. Idempotence is a tested property, not an incidental one.

### 4. `IItemScript.OnRerollModifiersEvent(Item, GameWorld) → bool`

With a `BaseItemScript` default of `false`, so the four existing item scripts are
unaffected. Mirrors Part 4's `OnRollModifiersEvent` exactly.

### 5. `ItemHandler.RerollModifiers(Item, GameWorld)`

Calls `ResetModifiers`, then the script hook inside the same try/catch-and-log pattern as
`RollTitleAndSurname` (`ItemHandler.cs:279-289`), then falls through to the native
`RollTitleAndSurname` path when the hook returns false.

The separate hook exists because a paid reroll and a drop roll differ: the drop rolls a
45% suffix chance, a paid reroll always lands one.

---

## Scripts

### `Dimensions.csx` — new configuration

```
# Rebirth. Ids are clear of WardenTemplateId (800000 + Offset*6) and of
# QuestIdBase (900000 + n*10 + k).
RebirthTemplateId = 810000
RebirthQuestId    = 910000
RebirthName, RebirthTitle, RebirthSurname
RebirthClassId, RebirthLevel
RebirthBody*/Face*/Hair*/EquippedItems     # same fields as the warden
RebirthMapId, RebirthX, RebirthY
ExpPerSpirit         = 100_000_000

# Sinks
GoldPerSpirit        = 1_000_000
ExpPerSpiritPurchase = 25_000_000
ResetItemCostBase    = 3                   # cost is ResetItemCostBase^dim

# Part 1 gap: abyss's flat top-end floors
Dim5MinExperience    = 100_000_000_000
Dim6MinExperience    = 500_000_000_000
```

**Everything lands in this one file.** Goose's `ScriptHandler` compiles each `.csx`
separately (`ScriptHandler.cs:19-31`) with no `#load` support, so a second global script
could not reference `Dimensions.Offset` or `Dimensions.SpiritCurrencyId`. The file grows
from 1,176 to roughly 1,700 lines. `Rebirth.csx` is separate only because the quest engine
loads scripts by path from the requirement and reward rows.

### `CreateRebirthQuest(world)`

A new pass alongside `CreateUnlockChain`, running after the clone passes so the template
escapes the pass-1 scaler — scaling a quest giver's HP is meaningless, exactly as for the
warden.

- **NPC template** at `RebirthTemplateId`: `NPCTypes.Quest`, `CanBeKilled = false`,
  `CanMove = false`, configured appearance. Class and level are validated against
  `class_info` at startup the way `ValidateWardenClass` does — `class_info` carries levels
  1–5 for class 1 and 1–50 for classes 2–7, so a bad pair makes `Class.GetLevel` return
  null and `NPC.LoadFromTemplate` throw at spawn time. The same id-collision preflight the
  warden uses applies.
- **Quest** at `RebirthQuestId`, `Repeatable = true`, registered with
  `QuestHandler.AddQuest` and attached to the template's `Quests` list before the spawn is
  created. `NPC.cs:637` aliases rather than copies, so attaching to the template first is
  sufficient.
- **Requirements**: `RebirthQuestId + 1` is stock `NothingEquipped` (type 6), which
  iterates every equip slot (`QuestWindow.cs:280-284`). `RebirthQuestId + 2` is `Script`
  (type 7) pointing at `Rebirth.csx`, with `KeepRequirement = true`.
- **Reward**: `RebirthQuestId + 11`, `Script` (type 21) pointing at `Rebirth.csx`, with
  `ScriptParams` carrying `ExpPerSpirit`.
- One spawn via `NPCHandler.SpawnNPC` at `RebirthMapId, RebirthX, RebirthY` — never
  `NPC.LoadFromTemplate`, which leaves the NPC out of `NPCHandler.npcs` and out of
  `NPCCount`.

Only dimension 0 gets one; there is no per-dimension copy.

### `Rebirth.csx`

| Member | Behaviour |
|---|---|
| `IsMet` | `player.Experience + player.ExperienceSold >= ExpPerSpirit` |
| `GetProgressText` | `"{total:N0} / {threshold:N0} experience"` |
| `CanComplete` | Refuses with a message when `world.CurrencyHandler.Get("spirit")` is null — i.e. `Enabled = false` — rather than resetting the player for nothing |
| `GiveReward` | The whole transaction, below |

```
total  = player.Experience + player.ExperienceSold
spirit = total / ExpPerSpirit                   // integer floor
player.ChangeClass(1, 1, world, 0m)             // loss percent explicitly off
player.Experience = 0; player.ExperienceSold = 0
currency.Add(player, spirit, world)
log the mint
```

**`KeepRequirement = true` is load-bearing.** `QuestWindow` runs `TakeRequirements` before
`GiveRewards` (`QuestWindow.cs:341-342`), so a consuming requirement would zero the
experience the reward needs to read. All state change lives in the reward.

`ExpPerSpirit` is read from `reward.ScriptParams` inside the call and never cached in a
field, per the `IQuestScript` contract. A configured `0` is rejected by the startup
validation pass rather than dividing by zero at turn-in.

### `DimensionItem.csx` — `OnRerollModifiersEvent`

The same abyss table as the drop roll (`Item.java:359-402`) with the suffix **guaranteed**
rather than 45%: six equal bands over the registered surnames, then the independent 2%
Legendary (`StatMultiplier` 1.25) / 2% Stunted (0.5) rarity roll. Returns true.

Because `ResetModifiers` clears the title as well as the surname, a reroll re-rolls rarity
too — matching abyss, whose reset re-ran the whole `loadFromTemplate` roll
(`ResetItemEvent.java:62`).

### `RepointVendorStock(world)`

A new pass next to `RepointDrops`, after both `CloneItemTemplates` and `CloneTemplates`.
For every NPC template with `ID >= Offset` that has `VendorItems`:

- Allocate a **new `NPCVendorSlot[]` holding new `NPCVendorSlot` instances**.
  `NPCTemplate`'s copy constructor does `this.VendorItems = other.VendorItems`
  (`NPCTemplate.cs:254`) — the array is shared with the base template, and the slot objects
  with it. Mutating either in place would rewrite dimension 0's shops. This is the same
  trap Part 4 documented for `Drops`.
- Each slot's `ItemTemplate` becomes `ItemHandler.GetTemplate(baseId + Offset*dim)` where a
  clone exists, else the base template unchanged. `Slot`, `Stack` and `CanSeeStats` copy
  across.

115 vendor templates and 686 slots in the April 2025 snapshot, so roughly 4,116 repointed
slots across six dimensions.

No vendor-side `CurrencyId` change is needed: the clones carry `CurrencyId = "spirit"` on
the item, and `CurrencyHandler.Resolve` puts the item override above the vendor. A
dimension vendor therefore sells cloned gear for spirit and unrepointed consumables for
gold from the same window, which reads correctly client-side because `f03f7ed` sends the
currency name per slot.

### `CloneMaps` — the dimension 5/6 floors

`Dimensions.csx:245-246` currently applies only the `(dim*5)²` scale. Abyss overrides the
result at the top end (`Map.java:251-260`):

```java
minExp = minExp * (dimension * 5)^2;
if (dimension == 5)      minExp = 100_000_000_000 * (dimension - 4);   // 100B
else if (dimension >= 6) minExp = 500_000_000_000 * (dimension - 5);   // 500B
```

That is a **flat floor, not a scale**. Since `0 × anything = 0` and most maps carry
`MinExperience = 0`, the override is the only thing gating the vast majority of maps at
the top end. Without it, dimensions 5 and 6 have no experience gate at all and
`dimension.max` is the sole barrier. `MaxExperience` keeps the plain scale, as in abyss.

---

## Commands

All four registered from `OnLoaded`, below the `if (!Enabled) return;` guard, so a
disabled feature never registers them. All four log through `LogHandler`: rebirth mints
spirit, `/buygold` and `/buyexperience` destroy it while creating gold and experience, and
`/givesp` moves it between players — every one of them is something support will be asked
about.

**Balance checks are load-bearing, not defensive.** Part 5 established that
`Player.RemoveGold` silently no-ops when the amount exceeds the balance, making the vendor
event's check the only thing preventing a free purchase. Every command below checks
`ICurrency.GetBalance` before calling `Remove`, and every refusal path must charge nothing.

### `/resetitem <slot>`

Inventory slots only — equipped gear cannot be reset, matching abyss. Order matters:

1. Parse the slot; refuse out of range, against `Settings.InventorySize` rather than
   abyss's hardcoded 30.
2. Refuse when the slot is empty.
3. `dim = item.TemplateID / Offset`; refuse `dim < 1` with *"Only items from a higher plane
   can be reset."*
4. Refuse anything that is not `UseTypes.Armor` or `UseTypes.Weapon`. Dimension tomes are
   `OneTime` consumables that can stack, and a reroll on a stack would rewrite every item
   in it.
5. `cost = ResetItemCostBase^dim` — 3/9/27/81/243/729. Refuse on insufficient balance,
   quoting the cost as abyss does.
6. `world.ItemHandler.RerollModifiers(item, world)`.
7. Charge, mark the item dirty, resend the inventory slot, and resend the player's HP/MP
   status — a rerolled suffix moves `HPStaticRegen` and `MaxHP`.

### `/buygold <n>`

`n` must parse and be `> 0`. Balance checked, then `spirit.Remove(player, n, world)` and
`world.CurrencyHandler.Gold.Add(player, n * GoldPerSpirit, world)` — through the currency
abstraction rather than `Player.AddGold` directly, so gold's packet handling stays in one
place.

### `/buyexperience <n>`

Refused for `ClassID == 1` (Commoner), as abyss does — a commoner has no level table to
consume it.

The command checks `Settings.ExperienceCap` itself before charging: `AddExperience`
early-returns when the player is over it (`Player.cs:1653`), which would otherwise take the
spirit and grant nothing. It then calls the new overload with `applyModifiers: false`, so
the player receives exactly `n × ExpPerSpiritPurchase`.

### `/givesp <player> <amount>`

Positive amounts only, online targets only (`PlayerHandler.GetPlayer(name)`), self-transfer
refused, balance checked, then `Remove` from the sender and `Add` to the receiver with both
sides messaged.

---

## Testing

**Unit, in `Goose.Tests`**

- `ChangeClass` with an explicit `0` loss percent banks the full experience; the three-arg
  overload still applies `Settings.ChangeClassExperienceLossPercent`.
- `ResetModifiers` returns name, `BaseStats`, `StatMultiplier` and both `ItemProperties` to
  template state, and is idempotent — the `LoadFromTemplate` accumulation trap is exactly
  what this guards.
- `RerollModifiers` prefers the script hook, falls through to `RollTitleAndSurname` when it
  returns false, and swallows-and-logs a throwing hook.
- `AddExperience(..., applyModifiers: false)` grants the exact figure on both sides of
  `ExperienceModifierLimit`.

**Integration, via Part 4's `GlobalScriptFixture`**

- Rebirth template, spawn, quest, requirements and reward exist at the configured ids, and
  none of them exist when `Enabled = false`.
- `IsMet` at, just below and just above the threshold.
- `GiveReward` on 250M experience yields 2 spirit, level 1, class 1, zeroed experience, and
  no 7% shave.
- `CanComplete` refuses when the currency is unregistered.
- Vendor stock: dimension slots repointed, dimension 0 byte-identical, arrays and slot
  objects not shared, consumable slots untouched.
- Dimension 5 and 6 map clones carry the flat floors; dimensions 1–4 keep the scale.
- Each command's refusal paths charge nothing — wrong slot, non-dimension item, stacked
  tome, insufficient balance, commoner buying experience, over the experience cap,
  self-transfer, offline target.

**Manual**

Rebirth end to end against live data, a `/resetitem` on a dimension-6 item, and a walk into
a dimension-5 vendor to confirm gear prices in spirit while potions price in gold.

---

## Known limitations

Accepted for this part, listed so they are not mistaken for bugs during testing.

1. **Rebirth exists only in dimension 0.** No per-dimension copy, by decision.
2. **The sub-threshold experience remainder is destroyed.** A player at 199M experience
   converts 1 spirit and loses 99M, faithful to abyss.
3. **Rebirth wipes the player off the experience leaderboard.** `Ranks.cs:72,84` orders by
   `ExperienceSold` descending, and rebirth zeroes it. Abyss behaved the same way.
4. **Rebirth locks a player out of the dimensions they have unlocked.** `dimension.max`
   survives, but `Map.PlayerCanJoin` gates on `Experience + ExperienceSold`
   (`Map.cs:638,644`), so a level-1 rebirthed player fails the map's own experience gate
   until they re-earn it. This is abyss's loop, and porting the flat 5/6 floors makes it
   stricter, not looser.
5. **Dimension-0 vendors still buy dimension loot for spirit**, by decision.
6. **`/resetitem` cannot touch equipped items or stacked tomes.**
7. **`3^dim` at dimension 6 is 729 spirit** — roughly 72.9 billion experience through the
   rebirth faucet. Abyss's constant against goose's rate; worth retuning once it is live.
8. **Spirit persists but becomes unspendable if `Enabled` is later set to `false`** —
   inherited from Part 5. The commands are never registered, and the vendors never clone.

## Follow-ups

1. **Cosmetic spirit sinks.** Abyss charged 1 SP for `/setcolor`
   (`SetCustomColorCommandEvent.java:119`) and for custom graphics
   (`SetCustomGraphicEvent.java:106`), plus a variable cost for custom weapon types
   (`SetCustomWeaponTypeEvent.java:76`). Goose has `/custom` already; wiring it to spirit is
   a small follow-on.
2. **A spirit leaderboard.** Abyss had `RankTypes.SP` (`RankHandler.java:71`). Considered
   and set aside as a mitigation for limitation 3; it remains the natural answer if
   rebirthed players want something to climb.
3. **A `/balance` command.** Spirit shows in the client's SP bar, so there is no way to read
   an exact figure if the bar rounds.
