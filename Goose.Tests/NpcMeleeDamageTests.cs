using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests;

public class NpcMeleeDamageTests
{
    private sealed record Setup(TestWorldFixture Fixture, Map Map, NPC Npc, Player Target);

    private static Setup BuildWorld(int? seed, Action<NPCTemplate>? configureNpc = null,
                                    Action<Player>? configureTarget = null, bool capturingTarget = false)
    {
        var fixture = seed is null
            ? new TestWorldFixture(s => { s.MaxAC = 3500; })
            : new TestWorldFixture(s => { s.MaxAC = 3500; }, new Random(seed.Value));
        var map = fixture.AddBaseMap(1, "m", width: 20, height: 20);
        var template = fixture.AddNpcTemplate(1, "MeleeNpc", t =>
        {
            t.BaseStats = new AttributeSet { HP = 1000, Strength = 100 };
            t.WeaponDamage = 5000;
            configureNpc?.Invoke(t);
        });
        var npc = fixture.SpawnNpc(map, 10, 10, template);
        Player target = capturingTarget
            ? new TestWorldFixture.CapturingPlayer
            {
                Map = map, MapID = map.ID, MapX = 11, MapY = 10,
                State = Player.States.Ready,
                Level = 1,
                BaseStats = new AttributeSet(),
                MaxStats = new AttributeSet(),
                Class = fixture.World.ClassHandler.GetClass(0)!,
            }
            : fixture.PlayerOn(map, 11, 10);
        target.State = Player.States.Ready;
        target.Level = 1;
        target.MaxStats = new AttributeSet { HP = 1_000_000, AC = 500, DamageReduction = 0.1 };
        target.CurrentHP = 1_000_000;
        configureTarget?.Invoke(target);
        return new Setup(fixture, map, npc, target);
    }

    // Hand-computed from the NPC.Attack formula with the BuildWorld values:
    // base = Str 100 + WeaponDamage 5000 + Level 50 + (50 - 1) = 5199; MeleeCrit/MeleeDamage 0;
    // target AC 500, DR 0.1 (soft cap 0.5), class ACMultiplier 1, ArmorPierce 0, MaxAC 3500.
    // 5199 × 0.9 × (1 − 500/3500) − 20 = 3990.657…, truncated by the (long) cast in Attack.

    [Fact]
    public void Attack_damage_matches_formula_without_crit()
    {
        var setup = BuildWorld(seed: null);

        setup.Npc.Attack(setup.Target, setup.Fixture.World);

        Assert.Equal(1_000_000 - 3990, setup.Target.CurrentHP);
    }

    [Fact]
    public void Attack_with_seeded_crit_matches_reference()
    {
        var setup = BuildWorld(seed: 1234,
            configureNpc: t => t.BaseStats = new AttributeSet { HP = 1000, Strength = 100, MeleeCrit = 0.5 });

        long[] losses = new long[20];
        for (int i = 0; i < losses.Length; i++)
        {
            long before = setup.Target.CurrentHP;
            setup.Npc.Attack(setup.Target, setup.Fixture.World);
            losses[i] = before - setup.Target.CurrentHP;
        }

        Assert.Equal(new long[]
        {
            3990, 8001, 3990, 8001, 3990, 3990, 3990, 3990, 8001, 8001,
            3990, 3990, 8001, 3990, 3990, 3990, 8001, 8001, 8001, 3990,
        }, losses);
    }

    [Fact]
    public void MitigateMelee_matches_attack_without_crit_and_consumes_no_random()
    {
        var a = BuildWorld(seed: 4321);
        var b = BuildWorld(seed: 4321);

        for (int i = 0; i < 5; i++)
            Assert.Equal(3990, (long)a.Npc.MitigateMelee(a.Target, 5199, a.Fixture.World));

        Assert.Equal(b.Fixture.World.Random.Next(), a.Fixture.World.Random.Next());
    }

    [Fact]
    public void Script_base_damage_replaces_template_base()
    {
        var setup = BuildWorld(seed: null);
        setup.Npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new FixedBaseScript());

        setup.Npc.Attack(setup.Target, setup.Fixture.World);

        Assert.Equal(1_000_000 - (long)setup.Npc.MitigateMelee(setup.Target, 65000, setup.Fixture.World),
            setup.Target.CurrentHP);
    }

    [Fact]
    public void Script_sees_the_actual_target_including_secondary_targets()
    {
        var setup = BuildWorld(seed: null);
        var recording = new RecordingScript();
        setup.Npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(recording);

        var outOfRange = setup.Fixture.PlayerOn(setup.Map, 18, 18);
        outOfRange.State = Player.States.Ready;
        outOfRange.Level = 1;
        outOfRange.MaxStats = new AttributeSet { HP = 1_000_000 };
        outOfRange.CurrentHP = 1_000_000;

        setup.Npc.AddAggro(outOfRange, 100, false, setup.Fixture.World);
        setup.Npc.AddAggro(setup.Target, 50, false, setup.Fixture.World);

        setup.Npc.HandleAttackEvent(setup.Fixture.World);

        Assert.Single(recording.Targets);
        Assert.Same(setup.Target, recording.Targets[0]);
        Assert.Equal(1_000_000, outOfRange.CurrentHP);
        Assert.True(setup.Target.CurrentHP < 1_000_000);
    }

    [Fact]
    public void Throwing_script_uses_template_base()
    {
        var setup = BuildWorld(seed: null);
        setup.Npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new ThrowingBaseScript());

        setup.Npc.Attack(setup.Target, setup.Fixture.World);

        Assert.Equal(1_000_000 - 3990, setup.Target.CurrentHP);
    }

    [Fact]
    public void Script_base_mitigated_below_zero_is_a_miss()
    {
        var setup = BuildWorld(seed: null, capturingTarget: true,
            configureTarget: p =>
            {
                p.MaxStats = new AttributeSet { HP = 1_000_000, AC = 3000, DamageReduction = 0.1 };
                p.CurrentHP = 1_000_000;
            });
        var capturing = (TestWorldFixture.CapturingPlayer)setup.Target;
        setup.Map.AddPlayer(capturing, setup.Fixture.World);
        setup.Npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new FixedBaseScript(baseDamage: 1));

        setup.Npc.Attack(capturing, setup.Fixture.World);

        Assert.Equal(1_000_000, capturing.CurrentHP);
        Assert.Contains(capturing.Sent, s => s.Contains(",21"));
    }

    private sealed class FixedBaseScript : BaseNPCScript
    {
        private readonly double baseDamage;

        public FixedBaseScript(double baseDamage = 65000)
        {
            this.baseDamage = baseDamage;
        }

        public override double MeleeBaseDamage(NPC npc, ICharacter target, double baseDamage, GameWorld world)
            => this.baseDamage;
    }

    private sealed class ThrowingBaseScript : BaseNPCScript
    {
        public override double MeleeBaseDamage(NPC npc, ICharacter target, double baseDamage, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class RecordingScript : BaseNPCScript
    {
        public List<ICharacter> Targets { get; } = new();

        public override double MeleeBaseDamage(NPC npc, ICharacter target, double baseDamage, GameWorld world)
        {
            this.Targets.Add(target);
            return baseDamage;
        }
    }
}
