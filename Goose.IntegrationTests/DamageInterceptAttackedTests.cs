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
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim", 5000);
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
        var victim = CreatePlayer(fixture, map, 5, 5, "Victim", 1000);
        AddScriptedBuff(victim, fixture, ZeroBody, "Zero.csx");
        var attacker = CreateNpc(map, 4, 5);

        victim.Attacked(attacker, 100, fixture.World);

        Assert.Equal(1000, victim.CurrentHP);
        Assert.Contains(P.BattleTextMiss(victim) + "\x01", victim.Sent);
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
        Assert.Contains(P.BattleTextMiss(npc) + "\x01", attacker.Sent);
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
