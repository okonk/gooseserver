using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class NpcInterceptDamageTakenTests : IDisposable
{
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

    private readonly TestWorldFixture fixture;

    public NpcInterceptDamageTakenTests()
    {
        fixture = new TestWorldFixture();
    }

    public void Dispose() => fixture.Dispose();

    private (NPC Npc, Player Player) SpawnPair()
    {
        var map = fixture.AddBaseMap(1, "m");
        var template = fixture.AddNpcTemplate(1, "Npc", t => t.Experience = 100);
        var npc = fixture.SpawnNpc(map, 5, 5, template, false);
        var player = fixture.PlayerOn(map, 5, 6);
        return (npc, player);
    }

    [Fact]
    public void Script_halves_damage_taken()
    {
        var (npc, player) = SpawnPair();
        npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new HalfScript());

        npc.Attacked(player, 100, fixture.World);

        Assert.Equal(950, npc.CurrentHP);
    }

    [Fact]
    public void Script_sees_damage_after_buff_intercepts()
    {
        var (npc, player) = SpawnPair();
        var effect = fixture.AddBaseSpellEffect(1, "Double",
            e => e.Script = fixture.CompileSpellEffectScript(DoubleBody, "Double.csx"));
        npc.AddBuff(new Buff { Caster = player, Target = npc, SpellEffect = effect }, fixture.World);
        var recorder = new RecordingNpcScript();
        npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(recorder);

        npc.Attacked(player, 10, fixture.World);

        Assert.Equal(20, recorder.SeenDamage);
    }

    [Fact]
    public void Script_returning_zero_is_a_miss_and_npc_survives()
    {
        var (npc, player) = SpawnPair();
        npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new ZeroScript());

        npc.Attacked(player, 10000, fixture.World);

        Assert.Equal(1000, npc.CurrentHP);
        Assert.NotEqual(NPC.States.Dead, npc.State);
    }

    [Fact]
    public void Throwing_script_leaves_damage_unchanged()
    {
        var (npc, player) = SpawnPair();
        npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new ThrowingScript());

        npc.Attacked(player, 100, fixture.World);

        Assert.Equal(900, npc.CurrentHP);
    }

    [Fact]
    public void Lethal_intercepted_damage_kills_and_credits_killer()
    {
        var (npc, player) = SpawnPair();
        npc.NPCTemplate.Script = ScriptStub.For<INPCScript>(new LethalScript());
        var recorder = new RecordingGlobalScript();
        var script = ScriptStub.For<IGlobalScript>(recorder);
        script.FilePath = "Scripts/Global/Record.csx";
        fixture.World.ScriptHandler.TryAdd(script);

        npc.Attacked(player, 10, fixture.World);

        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.Single(recorder.Kills);
        Assert.Same(npc, recorder.Kills[0].Npc);
        Assert.Same(player, recorder.Kills[0].Killer);
        Assert.Contains(player, recorder.Kills[0].Credited);
    }

    private sealed class HalfScript : BaseNPCScript
    {
        public override long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world)
            => damage / 2;
    }

    private sealed class ZeroScript : BaseNPCScript
    {
        public override long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world)
            => 0;
    }

    private sealed class ThrowingScript : BaseNPCScript
    {
        public override long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world)
            => throw new InvalidOperationException("boom");
    }

    private sealed class LethalScript : BaseNPCScript
    {
        public override long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world)
            => damage * 100;
    }

    private sealed class RecordingNpcScript : BaseNPCScript
    {
        public long SeenDamage { get; private set; }

        public override long InterceptDamageTaken(NPC npc, ICharacter attacker, long damage, GameWorld world)
        {
            this.SeenDamage = damage;
            return damage;
        }
    }

    private sealed class RecordingGlobalScript : BaseGlobalScript
    {
        public List<(NPC Npc, ICharacter Killer, IReadOnlyList<Player> Credited)> Kills { get; } = [];

        public override void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
            => this.Kills.Add((npc, killer, credited));
    }
}
