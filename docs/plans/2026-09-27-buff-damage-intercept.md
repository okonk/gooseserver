# Buff Damage Interception Implementation Plan

**Goal:** Give buff spell effects a script hook that rewrites incoming damage, and use it to restore the Shard of Invincibility's 100% damage reduction.

**Architecture:** A new `InterceptDamage` hook on `ISpellEffectScript` (base class passes through). A small `DamageIntercept.Apply` helper walks the target's buffs in order, feeding each scripted buff the original damage and the accumulated damage, and adopts the final value. Each `Attacked` implementation (Player/Pet/NPC) is rearranged so PvP scaling runs first, then the pipeline, then the miss/dodge/damage logic. The shard's spell effect (id 192) drops `damage_reduce` and gains a csx script that zeroes positive damage.

**Tech Stack:** C# / .NET 10, Roslyn csx scripting (`Microsoft.CodeAnalysis.CSharp.Scripting`), xUnit, Google Sheets game data via the gws CLI (@ `.agents/skills/goose-game-data/SKILL.md`).

Design doc: `docs/plans/2026-09-27-buff-damage-intercept-design.md`

Work in the worktree `.worktrees/buff-damage-intercept` (branch `feat/buff-damage-intercept`). Baseline at plan time: 1542 unit + 417 integration tests passing.

---

## APIs verified

| API | Location |
| --- | --- |
| `ICharacter.Attacked(ICharacter, long, GameWorld)` | `Goose/ICharacter.cs:81` |
| `Player.Attacked` (PvP 1/3 line at :2000, GM/miss check at :1972, dodge roll at :1985) | `Goose/Player.cs:1964` |
| `Pet.Attacked` (PvP 1/2 line at :797) | `Goose/Pet.cs:763` |
| `NPC.Attacked` (`OnAttackedEvent` :1079, `CanBeKilled` :1093, `damage <= 0` :1104) | `Goose/NPC.cs:1073` |
| `Player.Buffs` / `NPC.Buffs` (`List<Buff>`) | `Goose/Player.cs:350`, `Goose/NPC.cs:323` |
| `Pet.Buffs` initialized in `Pet()` ctor | `Goose/Pet.cs:294-297` |
| `ICharacter` implementors: only `NPC` and `Player` | `Goose/NPC.cs:14`, `Goose/Player.cs:21` |
| `SpellEffect.Script` (`Script<ISpellEffectScript>?`) | `Goose/SpellEffect.cs:222` |
| `SpellEffect.EffectTypes.Buff` (= 1) | `Goose/SpellEffect.cs:72` |
| `Buff.SpellEffect` | `Goose/Buff.cs:16` |
| `ISpellEffectScript` (Cast, OnBuffAdded, OnBuffRemoved, OnBuffTick, GetItemDescription) | `Goose/Scripting/ISpellEffectScript.cs` |
| `BaseSpellEffectScript` (default no-op impls) | `Goose/Scripting/BaseSpellEffectScript.cs` |
| `Script<T>.Object`; `LoadScript` compiles csx via Roslyn at load | `Goose/Scripting/Script.cs:11,27` |
| `ScriptHandler.GetScript<T>` (path-keyed cache) | `Goose/Scripting/ScriptHandler.cs:19` |
| `P.BattleTextMiss` = `"BT{id},21"`; `BattleTextDamage` = `"BT{id},1,-{d}"`; `BattleTextHeal` = `"BT{id},7,+{-heal}"` | `Goose/Packets.cs:364,369,374` |
| `Pet.IsAlive` = `Map is not null` | `Goose/Pet.cs:86` |
| `Map.GetPlayersInRange` (uses `map.players` + tile distance) | `Goose/Map.cs:155` |
| `GooseSettings.MaxAC` (int, **no default — must be set in fixtures** for `Attack` absorb math) / `DamageModifier` | `Goose/GooseSettings.cs:98,82` |
| `InternalsVisibleTo` Goose.Tests + Goose.IntegrationTests | `Goose/Goose.csproj:18-26` |
| `TestWorldFixture`: `CompileSpellEffectScript` (:37), `AddBaseMap` (:61), `AddBaseSpellEffect` (:74), `CommandPlayerOn` (:100, sets `State=Ready`, returns `CapturingPlayer` with `Sent`), `World`, `Settings` | `TestSupport/TestWorldFixture.cs` |
| csx link pattern into test output | `Goose.IntegrationTests/Goose.IntegrationTests.csproj:53-54` |
| Real-csx test pattern (`File.ReadAllText(AppContext.BaseDirectory/...)`) | `Goose.IntegrationTests/BackstabScriptTests.cs:131` |
| Sheet: Spell Effects = 76 cols; `damage_reduce` col 42, `oneffect_text` col 45, `script_path` col 75 (letters to verify via `schema`) | goose-game-data skill |

**Threading:** `Attacked` runs on the game thread; the pipeline is a synchronous in-thread call. Scripts compile at load time (existing `Script.LoadScript` pattern) — no new concurrency.

**Persistence:** no DB schema change. `spell_effects` is regenerated from the sheet on import (`/updatesql` or restart); no migration needed.

---

### Task 1: Core mechanism + pipeline tests

**Files:**
- Modify: `Goose/ICharacter.cs` (add `Buffs` member near the existing `AddBuff` declaration, :137)
- Modify: `Goose/Scripting/ISpellEffectScript.cs`
- Modify: `Goose/Scripting/BaseSpellEffectScript.cs`
- Create: `Goose/DamageIntercept.cs`
- Test: `Goose.Tests/DamageInterceptTests.cs`

**Step 1: Write the failing tests**

`Goose.Tests/DamageInterceptTests.cs`:

```csharp
using Goose.Testing;

namespace Goose.Tests;

public class DamageInterceptTests
{
    private const string BaseBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
        }

        return typeof(T);
        """;

    private const string DoubleBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return currentDamage * 2;
            }
        }

        return typeof(T);
        """;

    private const string PlusTenBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return currentDamage + 10;
            }
        }

        return typeof(T);
        """;

    private const string RawTimes10PlusCurrentBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return rawDamage * 10 + currentDamage;
            }
        }

        return typeof(T);
        """;

    private const string ThrowBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                throw new Exception("boom");
            }
        }

        return typeof(T);
        """;

    [Fact]
    public void Apply_NoBuffs_ReturnsDamageUnchanged()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);

        Assert.Equal(50, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_BuffWithoutScript_ReturnsDamageUnchanged()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "noscript", e => { });

        Assert.Equal(50, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_BaseScript_PassesDamageThrough()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "base", e => e.Script = fixture.CompileSpellEffectScript(BaseBody, "Base.csx"));

        Assert.Equal(50, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_ModifyingScript_ReturnsScriptValue()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "double", e => e.Script = fixture.CompileSpellEffectScript(DoubleBody, "Double.csx"));

        Assert.Equal(100, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_TwoScripts_SecondSeesFirstOutputAndOriginalRaw()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "plus10", e => e.Script = fixture.CompileSpellEffectScript(PlusTenBody, "PlusTen.csx"));
        AddBuff(target, fixture, "raw10pluscurrent", e => e.Script = fixture.CompileSpellEffectScript(RawTimes10PlusCurrentBody, "Raw10.csx"));

        Assert.Equal(1110, DamageIntercept.Apply(target, target, 100, fixture.World));
    }

    [Fact]
    public void Apply_ThrowingScript_SkippedAndLaterScriptsStillRun()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "boom", e => e.Script = fixture.CompileSpellEffectScript(ThrowBody, "Throw.csx"));
        AddBuff(target, fixture, "double", e => e.Script = fixture.CompileSpellEffectScript(DoubleBody, "Double2.csx"));

        Assert.Equal(100, DamageIntercept.Apply(target, target, 50, fixture.World));
    }

    [Fact]
    public void Apply_Heal_PassesThroughUnmodifiedByDefault()
    {
        using var fixture = new TestWorldFixture();
        var target = CreateTarget(fixture);
        AddBuff(target, fixture, "base", e => e.Script = fixture.CompileSpellEffectScript(BaseBody, "Base2.csx"));

        Assert.Equal(-50, DamageIntercept.Apply(target, target, -50, fixture.World));
    }

    private static Player CreateTarget(TestWorldFixture fixture)
    {
        var map = fixture.AddBaseMap(1, "m");
        return fixture.CommandPlayerOn(map, 1, 1);
    }

    private static void AddBuff(Player target, TestWorldFixture fixture, string fileName, Action<SpellEffect> configure)
    {
        var effect = fixture.AddBaseSpellEffect(1, fileName, e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            configure(e);
        });
        target.Buffs.Add(new Buff { Caster = target, Target = target, SpellEffect = effect });
    }
}
```

Notes:
- Distinct `fileName` per compiled script: `ScriptHandler` caches by path (`Goose/Scripting/ScriptHandler.cs:19`).
- `AddBuff` adds to the list directly (like `Goose.Tests/BuffNullGuardTests.cs:53`) — no `AddBuff` side effects (packets, regen events) needed for pipeline semantics.
- `Apply_TwoScripts`: 100 → +10 = 110 → `100*10 + 110` = 1110. Proves `rawDamage` stays original while `currentDamage` accumulates.

**Step 2: Run tests to verify they fail (red)**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~DamageInterceptTests" --nologo`
Expected: build failure — `DamageIntercept` does not exist, `InterceptDamage` not on `BaseSpellEffectScript`.

**Step 3: Implement**

`Goose/ICharacter.cs` — add next to the `AddBuff` declaration (:137):

```csharp
List<Buff> Buffs { get; }
```

(`Player.cs:350` and `NPC.cs:323` already declare `List<Buff> Buffs { get; set; }`, which satisfies a get-only interface member; `Pet : Player` inherits. No implementor changes needed.)

`Goose/Scripting/ISpellEffectScript.cs` — add to the interface:

```csharp
long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world);
```

`Goose/Scripting/BaseSpellEffectScript.cs` — add:

```csharp
public virtual long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
{
    return currentDamage;
}
```

`Goose/DamageIntercept.cs`:

```csharp
using Goose.Scripting;

namespace Goose
{
    internal static class DamageIntercept
    {
        public static long Apply(ICharacter target, ICharacter attacker, long damage, GameWorld world)
        {
            long raw = damage;

            foreach (Buff buff in target.Buffs)
            {
                Script<ISpellEffectScript>? script = buff.SpellEffect?.Script;
                if (script is null) continue;

                try
                {
                    damage = script.Object.InterceptDamage(buff, attacker, raw, damage, world);
                }
                catch (Exception e)
                {
                    log.Error(e, "SpellEffect InterceptDamage {0} ({1}) target {2} ({3}) Exception",
                        buff.SpellEffect.Name, buff.SpellEffect.ID, target.Name, target.LoginID);
                }
            }

            return damage;
        }
    }
}
```

No comments in new code (AGENTS.md). The `world` parameter is passed through for script use even though the helper itself doesn't need it.

**Step 4: Run tests to verify they pass (green)**

Run: `dotnet test Goose.Tests --filter "FullyQualifiedName~DamageInterceptTests" --nologo`
Expected: 7 passed.

**Step 5: Commit**

```bash
git add Goose/ICharacter.cs Goose/Scripting/ISpellEffectScript.cs Goose/Scripting/BaseSpellEffectScript.cs Goose/DamageIntercept.cs Goose.Tests/DamageInterceptTests.cs
git commit -m "feat(combat): add buff damage interception pipeline"
```

**Invariant-to-test matrix:**

| Invariant | Proved by |
| --- | --- |
| No buffs / no script / base script → damage unchanged | `Apply_NoBuffs_*`, `Apply_BuffWithoutScript_*`, `Apply_BaseScript_*` |
| Script output adopted | `Apply_ModifyingScript_*` |
| `rawDamage` stable, `currentDamage` accumulates across the chain | `Apply_TwoScripts_*` |
| Throwing script: logged, skipped, later scripts still run | `Apply_ThrowingScript_*` |
| Heals (negative) pass through by default | `Apply_Heal_*` |

---

### Task 2: Rearrange the three `Attacked` call sites + ordering tests

**Files:**
- Modify: `Goose/Player.cs:1964-2004` (`Attacked`)
- Modify: `Goose/Pet.cs:763-804` (`Attacked`)
- Modify: `Goose/NPC.cs:1093-1106` (`Attacked`)
- Test: `Goose.IntegrationTests/DamageInterceptAttackedTests.cs`

**Mutation impact:**
- Source of truth changed: the `damage` parameter of the three `Attacked` implementations (value that reaches `CurrentHP`).
- Important readers: `CurrentHP` (HP, death handling), battle-text packets (`P.BattleTextMiss/Damage/Heal`), NPC aggro/attack-event registration (`Goose/NPC.cs:1111-1112` zero path, :1119-1120 positive path).
- Derived/cached state affected: none — no derived state found; `MaxStats.DamageReduction` and the attacker-side formula are untouched.
- Required propagation sequence: none beyond the reassignment — `CurrentHP -= damage` and the packet builders read the local `damage` directly.
- Behaviour deltas (all intentional): (1) the pipeline now runs; (2) PvP scaling happens before the dodge roll instead of after — dodge probability is damage-independent, so outcomes are identical; (3) the GM check now sits after the pipeline — a GM with scripted buffs runs the scripts but still takes no damage; (4) `damage == 0` now sees the post-PvP value — 0 input is unchanged by the `damage > 0` guard, so 0 still misses.
- Invariants to preserve: with no buffs, behaviour is byte-identical to today (full existing suite must stay green); the `// pvp 1/3 damage` / `// pvp 1/2 damage` comments move with their lines (they state the game rule, not the position).
- Observable proof required: the ordering tests below assert final HP, not that the pipeline was called.

**Step 1: Write the failing tests**

`Goose.IntegrationTests/DamageInterceptAttackedTests.cs`:

```csharp
using Goose.Testing;

namespace Goose.IntegrationTests;

public class DamageInterceptAttackedTests
{
    private const string OffsetBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return currentDamage + 1000;
            }
        }

        return typeof(T);
        """;

    private const string ZeroBody = """
        using Goose;
        using Goose.Scripting;

        public class T : BaseSpellEffectScript
        {
            public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
            {
                return 0;
            }
        }

        return typeof(T);
        """;

    [Fact]
    public void Player_PvpDamage_ScaledBeforeScriptSeesIt()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, 5000);
        AddScriptedBuff(victim, fixture, OffsetBody, "Offset.csx");
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");

        victim.Attacked(attacker, 101, fixture.World);

        // PvP 1/3 first: 101 / 3 = 33, then script: 33 + 1000 = 1033.
        // Script-before-scaling would give (101 + 1000) / 3 = 367; no script at all gives 33.
        Assert.Equal(5000 - 1033, victim.CurrentHP);
    }

    [Fact]
    public void Pet_PvpDamage_ScaledBeforeScriptSeesIt()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var pet = CreatePet(fixture, map, 5, 5);
        AddScriptedBuff(pet, fixture, OffsetBody, "OffsetPet.csx");
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");

        pet.Attacked(attacker, 101, fixture.World);

        // Pet PvP 1/2 first: 101 / 2 = 50, then script: 50 + 1000 = 1050.
        Assert.Equal(5000 - 1050, pet.CurrentHP);
    }

    [Fact]
    public void Player_ZeroedDamage_SendsMissAndSkipsHP()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, 1000);
        AddScriptedBuff(victim, fixture, ZeroBody, "Zero.csx");
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, 100, fixture.World);

        Assert.Equal(1000, victim.CurrentHP);
        Assert.Contains(P.BattleTextMiss(victim), victim.Sent);
    }

    [Fact]
    public void Npc_ZeroedDamage_SendsMissToAttacker()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var npc = CreateNpc(map, 5, 5);
        map.SetCharacter(npc, 5, 5);
        AddScriptedBuff(npc, fixture, ZeroBody, "ZeroNpc.csx");
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");
        map.SetCharacter(attacker, 4, 5);
        map.AddPlayer(attacker, fixture.World);

        npc.Attacked(attacker, 100, fixture.World);

        Assert.Equal(1000, npc.CurrentHP);
        Assert.Contains(P.BattleTextMiss(npc), attacker.Sent);
    }

    private static TestWorldFixture.CapturingPlayer CreatePlayer(
        TestWorldFixture fixture, Map map, int x, int y, string name, long hp = 1000)
    {
        var player = fixture.CommandPlayerOn(map, x, y, name);
        player.LoginID = 202;
        player.Level = 10;
        player.MaxStats.HP = hp;
        player.MaxStats.Dexterity = -1;
        player.CurrentHP = hp;
        return player;
    }

    private static Pet CreatePet(TestWorldFixture fixture, Map map, int x, int y)
    {
        var pet = new Pet
        {
            Name = "Pet",
            Map = map,
            MapID = map.ID,
            MapX = x,
            MapY = y,
            State = Player.States.Ready,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet { HP = 5000, Dexterity = -1 },
            Class = fixture.World.ClassHandler.GetClass(0)!,
        };
        pet.CurrentHP = 5000;
        return pet;
    }

    private static NPC CreateNpc(Map map, int x, int y)
    {
        var npc = new NPC
        {
            LoginID = 404,
            Name = "NPC",
            Map = map,
            MapID = map.ID,
            MapX = x,
            MapY = y,
            Facing = Direction.Down,
            Level = 10,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet { HP = 1000 },
            NPCTemplate = new NPCTemplate(),
            CanBeKilled = true,
            Buffs = [],
            AggroTargetToValue = []
        };
        npc.CurrentHP = 1000;
        return npc;
    }

    private static void AddScriptedBuff(ICharacter target, TestWorldFixture fixture, string body, string fileName)
    {
        var effect = fixture.AddBaseSpellEffect(1, fileName, e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Script = fixture.CompileSpellEffectScript(body, fileName);
        });
        target.Buffs.Add(new Buff { Caster = target, Target = target, SpellEffect = effect });
    }
}
```

Notes:
- `Dexterity = -1` makes the dodge roll impossible (`Goose/Player.cs:1985`, `Goose/Pet.cs:781`), same trick as `BackstabScriptTests.cs:163`.
- High HP (5000) in the ordering tests keeps the victim alive: a 1033/1050 hit on 1000 HP would enter the death path (`WarpTo`, buff purge) and null-ref on unbound fixture state.
- The PvP check inside `Attacked` is `character is Player` (`Goose/Player.cs:2000`) — `map.CanPVP` is not consulted there, so no map flag is needed for these tests.
- `CreateNpc` mirrors `BackstabScriptTests.cs:169`.
- `Npc_ZeroedDamage` exercises the zero path's side effects (`AddAggro`, `AddAttackEvent` at `Goose/NPC.cs:1111-1112`) — safe: `AttackSpeed` is 0 so `AddAttackEvent` returns early (`Goose/NPC.cs:1390`).

**Step 2: Run tests to verify they fail (red)**

Run: `dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~DamageInterceptAttackedTests" --nologo`
Expected: all 4 FAIL — the pipeline is not wired into `Attacked` yet (HP deltas are 33/50/100/100 instead of 1033/1050/0/0).

**Step 3: Implement the rearrangement**

`Goose/Player.cs` `Attacked` (:1964): insert after the state check, and delete the old PvP line:

```csharp
        public virtual void Attacked(ICharacter character, long damage, GameWorld world)
        {
            if (this.State != States.Ready) return;

            // pvp 1/3 damage
            if (damage > 0 && character is Player) damage /= 3;

            damage = DamageIntercept.Apply(this, character, damage, world);

            List<Player> range = this.Map.GetPlayersInRange(this);
```

and in the `damage > 0` branch delete:

```csharp
                // pvp 1/3 damage
                if (character is Player) damage /= 3;
```

leaving `packet = P.BattleTextDamage(this, damage) + "\x1";` as the first line of that branch.

`Goose/Pet.cs` `Attacked` (:763): same shape — insert after `if (!this.IsAlive) return;`:

```csharp
            // pvp 1/2 damage
            if (damage > 0 && character is Player) damage /= 2;

            damage = DamageIntercept.Apply(this, character, damage, world);
```

and delete the old `// pvp 1/2 damage` + `if (character is Player) damage /= 2;` pair from the `damage > 0` branch (:796-797).

`Goose/NPC.cs` `Attacked` (:1093): insert between the `CanBeKilled` block and `packet = "";`:

```csharp
                damage = DamageIntercept.Apply(this, character, damage, world);

                packet = "";
                if (damage <= 0)
```

`OnAttackedEvent` (:1079) stays first and keeps seeing the pre-buff value (deliberate — do not change what existing NPC scripts observe).

**Step 4: Run tests to verify they pass (green)**

Run: `dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~DamageInterceptAttackedTests" --nologo`
Expected: 4 passed.

**Step 5: Commit**

```bash
git add Goose/Player.cs Goose/Pet.cs Goose/NPC.cs Goose.IntegrationTests/DamageInterceptAttackedTests.cs
git commit -m "feat(combat): run buff damage interception in Attacked after pvp scaling"
```

**Invariant-to-test matrix:**

| Invariant | Proved by |
| --- | --- |
| PvP 1/3 runs before the pipeline (player) | `Player_PvpDamage_ScaledBeforeScriptSeesIt` (1033 vs 367 vs 33 — three distinguishable outcomes) |
| PvP 1/2 runs before the pipeline (pet) | `Pet_PvpDamage_ScaledBeforeScriptSeesIt` |
| Script returning 0 → miss text, no HP change | `Player_ZeroedDamage_SendsMissAndSkipsHP` |
| NPC path: zero → miss to attacker, no HP change, aggro path intact | `Npc_ZeroedDamage_SendsMissToAttacker` |
| No-buff behaviour unchanged | full existing suite (Task 5) |

---

### Task 3: Shard csx script + integration tests

**Files:**
- Create: `Goose/Data/Illutia/Scripts/Spell/ShardOfInvincibility.csx`
- Modify: `Goose.IntegrationTests/Goose.IntegrationTests.csproj` (add csx link next to :53-54)
- Test: `Goose.IntegrationTests/ShardOfInvincibilityScriptTests.cs`

**Step 1: Write the failing tests + csproj link**

`Goose.IntegrationTests/Goose.IntegrationTests.csproj` — add after the Backstab link (:54):

```xml
    <None Include="../Goose/Data/Illutia/Scripts/Spell/ShardOfInvincibility.csx"
          Link="SpellScripts/ShardOfInvincibility.csx" CopyToOutputDirectory="PreserveNewest" />
```

`Goose.IntegrationTests/ShardOfInvincibilityScriptTests.cs`:

```csharp
using Goose.Testing;

namespace Goose.IntegrationTests;

public class ShardOfInvincibilityScriptTests
{
    [Fact]
    public void ShardBuff_MeleeHit_DealsNoDamageAndSendsMiss()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        AddShardBuff(victim, fixture);
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, 100, fixture.World);

        Assert.Equal(1000, victim.CurrentHP);
        Assert.Contains(P.BattleTextMiss(victim), victim.Sent);
    }

    [Fact]
    public void NoShardBuff_MeleeHit_DealsFullDamage()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, 100, fixture.World);

        Assert.Equal(900, victim.CurrentHP);
        Assert.Contains(P.BattleTextDamage(victim, 100), victim.Sent);
    }

    [Fact]
    public void ShardBuff_Heal_PassesThrough()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        AddShardBuff(victim, fixture);
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, -50, fixture.World);

        Assert.Equal(1050, victim.CurrentHP);
        Assert.Contains(P.BattleTextHeal(victim, -50), victim.Sent);
    }

    [Fact]
    public void ShardBuff_NpcTarget_DealsNoDamageAndSendsMiss()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var npc = CreateNpc(map, 5, 5);
        map.SetCharacter(npc, 5, 5);
        AddShardBuff(npc, fixture);
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");
        map.SetCharacter(attacker, 4, 5);
        map.AddPlayer(attacker, fixture.World);

        npc.Attacked(attacker, 100, fixture.World);

        Assert.Equal(1000, npc.CurrentHP);
        Assert.Contains(P.BattleTextMiss(npc), attacker.Sent);
    }

    [Fact]
    public void ShardBuff_GetItemDescription_ReturnsCustomLine()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        var effect = AddShardBuff(victim, fixture);

        Assert.Contains("Reduces all damage taken to 0.", effect.GetItemDescription(fixture.World));
    }

    [Fact]
    public void ShardBuff_EndToEnd_PlayerAttack_DealsNoDamage()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        map.CanPVP = true;
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        AddShardBuff(victim, fixture);
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");
        attacker.MaxStats.Strength = 10;

        attacker.Attack(victim, fixture.World);

        Assert.Equal(1000, victim.CurrentHP);
        Assert.Contains(P.BattleTextMiss(victim), victim.Sent);
    }

    [Fact]
    public void EndToEnd_PlayerAttack_NoShard_DealsPvpScaledDamage()
    {
        using var fixture = CreateFixture();
        var map = fixture.AddBaseMap(1, "Test");
        map.CanPVP = true;
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim");
        var attacker = CreatePlayer(fixture, map, 4, 5, "Attacker");
        attacker.MaxStats.Strength = 10;

        attacker.Attack(victim, fixture.World);

        Assert.Equal(997, victim.CurrentHP);
    }

    private static TestWorldFixture CreateFixture()
    {
        return new TestWorldFixture(settings =>
        {
            settings.DamageModifier = 1.0;
            settings.MaxAC = 100;
        });
    }

    private static TestWorldFixture.CapturingPlayer CreatePlayer(
        TestWorldFixture fixture, Map map, int x, int y, string name)
    {
        var player = fixture.CommandPlayerOn(map, x, y, name);
        player.LoginID = 202;
        player.Level = 10;
        player.MaxStats.HP = 1000;
        player.MaxStats.Dexterity = -1;
        player.CurrentHP = 1000;
        return player;
    }

    private static NPC CreateNpc(Map map, int x, int y)
    {
        var npc = new NPC
        {
            LoginID = 404,
            Name = "NPC",
            Map = map,
            MapID = map.ID,
            MapX = x,
            MapY = y,
            Facing = Direction.Down,
            Level = 10,
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet { HP = 1000 },
            NPCTemplate = new NPCTemplate(),
            CanBeKilled = true,
            Buffs = [],
            AggroTargetToValue = []
        };
        npc.CurrentHP = 1000;
        return npc;
    }

    private static SpellEffect AddShardBuff(ICharacter target, TestWorldFixture fixture)
    {
        var scriptBody = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SpellScripts", "ShardOfInvincibility.csx"));
        var effect = fixture.AddBaseSpellEffect(192, "Shard of Invincibility", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Buff;
            e.Script = fixture.CompileSpellEffectScript(scriptBody, "ShardOfInvincibility.csx");
        });
        target.Buffs.Add(new Buff { Caster = target, Target = target, SpellEffect = effect });
        return effect;
    }
}
```

Notes:
- End-to-end math: unarmed attacker (`WeaponDamage` getter returns 1 for an empty inventory) → `damage = Strength + 1 + (Level - Level)` = 11 (`Goose/Player.cs:1773`); crit 0 → ×1; `DamageModifier` 1.0; victim `DamageReduction` 0; `MaxAC = 100` keeps the absorb division finite (`Goose/Player.cs:1780`). PvP 1/3 → 3 → shard zeroes → miss. Control: 11 → 3 → HP 997.
- `map.CanPVP = true` is required for the end-to-end tests only: `Player.Attack` early-returns for player-vs-player on non-PvP maps (`Goose/Player.cs:1764-1767`).
- The csx is compiled from the real repo file (Backstab pattern, `BackstabScriptTests.cs:131`), so the shipped script is what's tested.

**Step 2: Run tests to verify they fail (red)**

Run: `dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~ShardOfInvincibilityScriptTests" --nologo`
Expected: build failure — the csproj link points at a csx that doesn't exist yet.

**Step 3: Write the shard script**

`Goose/Data/Illutia/Scripts/Spell/ShardOfInvincibility.csx`:

```csharp
using Goose;
using Goose.Scripting;

public class ShardOfInvincibility : BaseSpellEffectScript
{
    public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
    {
        return currentDamage > 0 ? 0 : currentDamage;
    }

    public override IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        yield return "Reduces all damage taken to 0.";
    }
}

return typeof(ShardOfInvincibility);
```

Only positive damage is zeroed; heals (negative) pass through — invincibility means "takes no damage", not "cannot be healed".

**Step 4: Run tests to verify they pass (green)**

Run: `dotnet test Goose.IntegrationTests --filter "FullyQualifiedName~ShardOfInvincibilityScriptTests" --nologo`
Expected: 7 passed.

**Step 5: Commit**

```bash
git add Goose/Data/Illutia/Scripts/Spell/ShardOfInvincibility.csx Goose.IntegrationTests/Goose.IntegrationTests.csproj Goose.IntegrationTests/ShardOfInvincibilityScriptTests.cs
git commit -m "feat(combat): shard of invincibility intercepts damage to zero"
```

**Invariant-to-test matrix:**

| Invariant | Proved by |
| --- | --- |
| Shard zeroes melee damage, miss text shown | `ShardBuff_MeleeHit_DealsNoDamageAndSendsMiss` |
| Control: without the shard the full damage lands | `NoShardBuff_MeleeHit_DealsFullDamage` |
| Heals pass through the shard | `ShardBuff_Heal_PassesThrough` |
| Mechanism works on NPC targets | `ShardBuff_NpcTarget_DealsNoDamageAndSendsMiss` |
| Item preview shows the custom line | `ShardBuff_GetItemDescription_ReturnsCustomLine` |
| Full melee path (attacker formula → Attacked → pipeline) | `ShardBuff_EndToEnd_PlayerAttack_DealsNoDamage` + `EndToEnd_PlayerAttack_NoShard_DealsPvpScaledDamage` |

---

### Task 4: Sheet edit — spell effect 192

**Files:** none in the repo. Spreadsheet `1O2mbze7WGIt2JLeqDctR1zFSL6CdaNhf7iZlaqE4ieU`, "Spell Effects" worksheet, row for `spell_effect_id` 192 (row 193 = id + 1 header row; confirm from the read below).

**Mutation impact:**
- Source of truth: the Google spreadsheet (not in the repo — the running server imports it).
- Important readers: `CsvToSqlConverter` at startup and `/updatesql` → `spell_effects` table → `SpellHandler` (`Goose/SpellHandler.cs:105` reads `damage_reduce`, :141 reads `script_path`).
- Derived state affected: buff/item description for effect 192 and item 471 (`SpellEffect.GetItemDescription` → now the script's line), and the "Damage Reduction: X%" line in `PlayerInfoWindow`/`Window` for a sharded player (`Goose/PlayerInfoWindow.cs:58`, `Goose/Window.cs:260`) — the buff no longer contributes +1 to `MaxStats.DamageReduction`.
- Required propagation sequence: sheet edit → `/updatesql` (or restart) → import replaces the table (DROP + CREATE) → `SpellHandler` reloads effect 192 and compiles the csx via `ScriptHandler.GetScript` (`Goose/SpellHandler.cs:141`).
- Invariants to preserve: import succeeds (no enum/required-column breakage — only three cells change), the script compiles, the buff still applies (60s duration, `works_not_in_pvp=1` untouched).
- Observable proof required: read-back of row 192 via the helper shows all three cells; server log after `/updatesql` shows a clean import. In-game verification (use the shard, take a hit) is manual and out of scope for this plan.

**Step 1: Bootstrap gws** (per @ `.agents/skills/goose-game-data/SKILL.md`)

```bash
python3 .agents/skills/goose-game-data/scripts/gsheets.py doctor
```

If the `.gws` config dir is missing, run the printed bootstrap command first.

**Step 2: Read the current row**

```bash
S=.agents/skills/goose-game-data/scripts/gsheets.py
python3 $S read "Spell Effects" --id 192 --json
python3 $S schema "Spell Effects"
```

Confirm: `damage_reduce=1`, `script_path` empty, `oneffect_text` empty; note the sheet row number and the column letters for `damage_reduce` (expected AP), `oneffect_text` (expected AS), `script_path` (expected BW).

**Step 3: Dry-run the update**

```bash
python3 $S update "Spell Effects" --id 192 \
  --set "script path=Scripts/Spell/ShardOfInvincibility.csx" \
  --set "oneffect text=You are invincible." \
  --set "damage_reduce=" --dry-run
```

Check whether the helper accepts clearing `damage_reduce` with an empty value.

**Step 4: Apply**

- If the helper accepts the empty value: re-run the same command without `--dry-run`.
- If it refuses: run `update` with only the two non-empty columns, then clear the cell with raw `gws` (check `gws values update --help` for exact flags):

```bash
gws values update ... --range "<col><row>" --values '[[""]]' --value-input-option RAW
```

**Step 5: Verify by reading back**

```bash
python3 $S read "Spell Effects" --id 192 --json
```

Expected: `damage_reduce` empty, `script_path=Scripts/Spell/ShardOfInvincibility.csx`, `oneffect_text=You are invincible.`, everything else unchanged (`effect_duration=60`, `works_not_in_pvp=1`, `buff_graphic=810035`).

**Step 6: Note the live path (no commit — the sheet is not in git)**

Deploy a build containing `ShardOfInvincibility.csx` → `/updatesql` in the GM console. State in the session summary: sheet, worksheet, row, id 192, and each cell written.

---

### Task 5: Full-suite verification

**Step 1: Run the entire suite**

Run: `dotnet test --nologo`
Expected: all green — 1542 + 7 new unit tests, 417 + 4 + 7 new integration tests, 0 failed.

**Step 2: Check for new compiler warnings in touched files**

Run: `dotnet build --nologo 2>&1 | grep -E "DamageIntercept|ShardOfInvincibility|ICharacter|SpellEffect|Player\.cs|Pet\.cs|NPC\.cs"`
Expected: no new warnings from the changed files (the codebase has pre-existing warnings in unrelated log test files — ignore those).

**Step 3: Review the branch**

Run: `git log --oneline master..HEAD` and `git diff --stat master..HEAD`
Expected: 4 commits (Tasks 1-3 + this task if anything changed) and the design doc commit; no stray files.

---

## Red-team notes (decisions already made)

- **Threading:** pipeline runs inside `Attacked` on the game thread; no new async, no new locks. Scripts compile at load only.
- **Persistence:** no schema change; the sheet is the source of truth and the import regenerates `spell_effects`. No migration.
- **Failure behavior:** a throwing intercept script is logged and skipped (damage passes through, later scripts still run) — proven by `Apply_ThrowingScript_*`. A missing csx fails the server's script load loudly (existing `Script.LoadScript` behaviour), which is why the shard drops the `damage_reduce` stat rather than keeping it as a fallback (design decision A).
- **Fixture reality:** `CommandPlayerOn` sets `State=Ready` and returns a capturing player (`TestWorldFixture.cs:100-114`); `Pet.IsAlive` only needs `Map` (`Goose/Pet.cs:86`); `MaxAC` must be set in fixtures or the `Attack` absorb math divides by zero (`Goose/GooseSettings.cs:98`); high HP in ordering tests avoids the death path.
- **Registry/publication:** no new registries. `ScriptHandler`'s existing path-keyed cache is used; distinct file names per compiled script in tests.
- **Not covered here (deferred):** in-game manual verification of the live shard (Task 4 step 6), and any client-side changes (none needed — miss text is the existing `BT{id},21` packet).
