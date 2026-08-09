# Quest Scripts Design

Date: 2026-08-08

## Summary

Extend the quests system to support C# scripts, matching the pattern already used by
NPCs, items, maps, spell effects, item modifiers, and global scripts.

Scope is **script-driven requirement and reward types**: a new `Script` value in both
`RequirementType` and `RewardType` delegates that requirement's evaluation or that
reward's delivery to a `.csx` script, exactly as `SpellEffect.EffectTypes.Script`
delegates a spell effect. Quest-level lifecycle hooks (`OnStarted`, `OnCompleted`,
eligibility overrides) are **out of scope**.

The script path lives on the individual `quest_requirements` / `quest_rewards` row, not
on the quest, so each scripted requirement or reward names its own script. This is the
convention every other system follows, keeps each script single-purpose, and the
`ScriptHandler` path-keyed cache means two rows pointing at the same file share one
instance.

## Script interface and type visibility

### Visibility

Scripts compile against the assembly via
`ScriptOptions.WithReferences(Assembly.GetExecutingAssembly())`, so they can only
reference `public` types. Today `Quest`, `QuestRequirement`, `QuestReward`,
`QuestProgress`, `RequirementType`, and `RewardType` are all internal (no access
modifier). All six become `public`. `QuestStatus` is already public.

`QuestWindow` stays internal — scripts receive what they need through hook parameters
and never touch the window.

`Player.QuestsStarted` / `QuestsCompleted` / `QuestProgress` stay internal, and
`GameWorld.QuestHandler` stays internal. A script can therefore read its own row and
`requirement.Quest`, plus the player's public surface, but cannot enumerate every quest
in the world or inspect the player's completed-quest list. See "Known limits".

### Interface

A single interface covering both roles, in `Goose/Scripting/IQuestScript.cs`, with
`Goose/Scripting/BaseQuestScript.cs` providing no-op defaults:

```csharp
public interface IQuestScript
{
    // Requirement role
    bool IsMet(QuestRequirement requirement, Player player, GameWorld world);
    string GetProgressText(QuestRequirement requirement, Player player, GameWorld world);
    void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world);

    // Reward role
    string CanComplete(QuestReward reward, Player player, GameWorld world);
    void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world);
}
```

One interface rather than separate requirement and reward interfaces: a single script
file can then implement a paired "requirement + reward" behaviour, and there is one
file pair to maintain instead of two. The cost is that every script inherits
irrelevant no-op members, which is accepted.

`BaseQuestScript` defaults:

| Member               | Default |
|----------------------|---------|
| `IsMet`              | `true`  |
| `GetProgressText`    | `""`    |
| `OnTakeRequirement`  | no-op   |
| `CanComplete`        | `null`  |
| `GiveReward`         | no-op   |

`CanComplete` returns `null` or empty to allow completion, or the message to display
when blocking it. Returning the message rather than a bare `bool` lets the script
explain itself ("You need a free inventory slot", "You must be rank 5") without
out-of-band `P.ServerMessage` calls, and the window renders it through the same
`\n`-splitting path as `Description` and `FailText`, so multi-line text works.

Long messages are not truncated or wrapped. The existing hardcoded window strings are
hand-wrapped to roughly 50 characters; keeping a scripted message inside the client's
quest frame is the script author's responsibility. This is the same trust model as NPC
and map scripts, which already send arbitrary text.

## Schema and loading

### Enum values

Appended, so existing numeric values in spreadsheets and databases are untouched:

- `RequirementType.Script` = 7
- `RewardType.Script` = 21

These enums are mirrored in two places that must stay in sync:
`Goose/Quests/QuestRequirement.cs` and `Goose/Quests/QuestReward.cs` for the server,
and `CsvToSql.Core/QuestRequirementsCsvToSql.cs` and
`CsvToSql.Core/QuestRewardsCsvToSql.cs`, which drive the DataEditor dropdowns through
SchemaGen.

### Columns

Following `spell_effects` exactly:

```sql
ALTER TABLE quest_requirements ADD script_path TEXT DEFAULT '' NOT NULL;
ALTER TABLE quest_requirements ADD script_params TEXT DEFAULT '' NOT NULL;
ALTER TABLE quest_rewards ADD script_path TEXT DEFAULT '' NOT NULL;
ALTER TABLE quest_rewards ADD script_params TEXT DEFAULT '' NOT NULL;
```

Landing in:

- `Goose/sql/quests.sql` — fresh installs, inline in the `CREATE TABLE` statements.
- `Goose/sql/onetimeupdates.sql` — existing databases, appended as the `ALTER` statements above.
- `CsvToSql.Core/QuestRequirementsCsvToSql.cs` and `QuestRewardsCsvToSql.cs` — as
  `Col.Text("script_path", def: "''")` and `Col.Text("script_params", def: "''")` at the
  end of each descriptor, so the editor picks them up.
- `tools/DataEditor/schema.js` — regenerated via SchemaGen.

`QuestRequirement` and `QuestReward` each gain `public Script<IQuestScript> Script` and
`public string ScriptParams`.

### Loading, with early failure

`QuestRequirement.FromReader` and `QuestReward.FromReader` gain a `GameWorld world`
parameter. Both are called only from `QuestHandler.LoadQuests`, so the signature change
is contained.

```csharp
requirement.ScriptParams = Convert.ToString(reader["script_params"]);
string scriptPath = Convert.ToString(reader["script_path"]);
if (!string.IsNullOrEmpty(scriptPath))
    requirement.Script = world.ScriptHandler.GetScript<IQuestScript>(scriptPath);

if (requirement.Type == RequirementType.Script && requirement.Script == null)
    throw new Exception($"Quest requirement {requirement.Id} (quest {quest.Id}) has type Script but no script_path");
```

The reward side mirrors this against `RewardType.Script`.

Failing at load time rather than silently leaving `Script` null is deliberate: a
`Script` requirement with no script would otherwise fall through
`PlayerMeetsRequirements`'s `default: return false` arm and make the quest quietly
uncompletable, and a scripted reward with no script would silently do nothing.

All three misconfigurations abort loudly, naming the offending row:

| Misconfiguration | Source of the error |
|---|---|
| Type is `Script`, `script_path` empty | the explicit throw above |
| `script_path` names a missing file | `Script<T>.LoadScript` throws `FileNotFoundException` |
| Script has a compile error | `script.Compile()` throws |

At startup the exception propagates through `LoadStep("Quests", ...)`, which logs
`Fatal` and aborts — the existing behaviour for any load-time failure. On a live server
`/reloadsql` catches it, logs it, and reports `"Failed reloading sql: <message>"` to the
GM, so the row id and quest id appear in the GM's chat.

The inverse case — a `script_path` on a non-`Script` row — is a deliberate silent no-op.
The script is loaded and cached but never invoked. This lets a designer stage a script
before flipping the type, and no other system objects to it.

`/reloadscripts` works with no extra wiring, since `ScriptHandler.ReloadScripts` walks
its own cache.

### Script location

`Data/Illutia/Scripts/Quest/` — a new directory alongside the existing `Scripts/NPC`,
`Scripts/Item`, `Scripts/Spell`, and `Scripts/Global`.

## QuestWindow integration

Five touch points, all in `QuestWindow`:

1. **`PlayerMeetsRequirements`** — a `case RequirementType.Script` arm before the
   `default`, returning false when `requirement.Script.Object.IsMet(...)` is false. The
   method signature gains `GameWorld world`; its single caller (`Clicked`) already has one.

2. **`GetQuestProgressText`** — a `Script` arm calling `GetProgressText`. An empty
   return contributes no line. Non-empty values are appended verbatim followed by `\n`,
   matching the surrounding arms, so the script controls its own wording and may emit
   multiple lines with embedded `\n`. Requirements are sorted `OrderBy(r => r.Type)` and
   `Script` = 7 sorts last, so scripted lines appear after the built-in ones.

3. **`TakeRequirements`** — a `Script` arm calling `OnTakeRequirement`, inside the
   existing `if (!requirement.KeepRequirement)` guard. `keep_requirement` therefore
   applies to scripted requirements exactly as to built-in ones: a script that should
   never consume anything is configured with `keep_requirement = 1` rather than by
   implementing an empty hook.

4. **A new `CanComplete` gate** in `Clicked`, after the inventory and spellbook checks
   and before `CompleteQuest`. A helper walks `quest.Rewards.Where(r => r.Type ==
   RewardType.Script)` and returns the first non-null, non-empty message, else null. The
   message is stored in a field and rendered by a new
   `QuestWindowState.QuestScriptCannotComplete` arm in `Populate`, flowing through the
   existing `\n`-splitting loop. Buttons behave as in the other blocking states.

   `CanComplete` is consulted only for `Script` **rewards**, not scripted requirements.
   A scripted requirement that is not met fails through `IsMet` and shows `FailText`,
   which is the correct existing path; calling `CanComplete` on requirements too would
   be redundant. The asymmetry is intentional.

5. **`GiveRewards`** — a `Script` arm calling `GiveReward(reward, npc, player, world)`,
   leaving `rewardMessage` null so the window prints nothing and the script owns its own
   messaging. This is consistent with `RewardType.LearnSpell`, which is already silent.

Rewards are given in database row order. A script reward that depends on a built-in
reward having already been applied — reading the new class after a `ClassChange`, for
instance — works only if the rows are ordered that way. No sequencing column is added.

Script hooks are called without try/catch, matching every other system (`Map.cs` calls
`Script?.Object.OnPlayerEntered` bare). A throwing script propagates into the event
loop. Note that a throw inside `GiveRewards` leaves a quest half-rewarded and already
marked complete; this is accepted for consistency with the other systems.

## Shared script instances

`ScriptHandler` caches **one instance per file path**, shared by every row pointing at
that file. Scripts must be stateless, or key any state by player and row.

The trap: `ScriptParams` is per-row, but the instance is shared. A script that
deserializes `ScriptParams` into an instance or static field in one hook and reads it
in another will read whichever row touched it last. `HealerNPC.csx` already falls into
this shape with its `static` fields — it happens to work only because one NPC template
uses it.

**Scripted quest hooks must deserialize `ScriptParams` from the row passed into the
call, on every call.** The example script demonstrates this.

## Deliverables

- `Goose/Scripting/IQuestScript.cs`, `Goose/Scripting/BaseQuestScript.cs`
- Visibility changes across `Goose/Quests/`
- Enum values, `Script`/`ScriptParams` properties, and `FromReader` loading with validation
- `QuestWindow` integration and the new window state
- SQL: `quests.sql`, `onetimeupdates.sql`
- Editor: both CsvToSql descriptors, regenerated `schema.js`
- One example script in `Data/Illutia/Scripts/Quest/` exercising both roles and
  deserializing `ScriptParams` per call
- Tests in `Goose.Tests` (which has no quest coverage today); `CsvToSqlSnapshotTests`
  and the `Schema` tests will need their snapshots updated for the new columns

## Known limits (deferred, not oversights)

- **Scripted requirements are stateless predicates.** They evaluate the player's current
  state when the quest window opens. They cannot accumulate counter progress, because
  `QuestWindow.Handle` only creates `QuestProgress` rows for `Kill` and `TalkToNPC`, and
  the hooks needed to update them on arbitrary events (the quest-lifecycle-hooks
  approach) are out of scope. "Kill 10 of X, but only at night" is not expressible.
- **Scripts cannot read the player's quest history.** `Player.QuestsCompleted` /
  `QuestsStarted` / `QuestProgress` and `GameWorld.QuestHandler` remain internal. A
  likely future request.
- **A throw partway through `LoadQuests` leaves quests partially reloaded**, since it
  mutates `this.Quests` in place as it reads. Pre-existing across every handler (a bad
  `class_restrictions` value does the same today) and not fixed here.
