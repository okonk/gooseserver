using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class GlobalNpcKilledHookTests : IDisposable
{
    private readonly TestWorldFixture fixture;

    public GlobalNpcKilledHookTests()
    {
        fixture = new TestWorldFixture();
    }

    public void Dispose() => fixture.Dispose();

    private RecordingGlobalScript RegisterRecorder(string path)
    {
        var recorder = new RecordingGlobalScript();
        var script = ScriptStub.For<IGlobalScript>(recorder);
        script.FilePath = path;
        this.fixture.World.ScriptHandler.TryAdd(script);
        return recorder;
    }

    private (NPC Npc, Player Killer) SpawnKiller(int mapWidth = 10, int mapHeight = 10)
    {
        var map = this.fixture.AddBaseMap(1, "m", mapWidth, mapHeight);
        var template = this.fixture.AddNpcTemplate(1, "CandyNpc", t => t.Experience = 100);
        var npc = this.fixture.SpawnNpc(map, 5, 5, template, false);
        var killer = this.fixture.PlayerOn(map, 5, 6);
        return (npc, killer);
    }

    [Fact]
    public void Solo_kill_dispatches_with_killer_credited()
    {
        var (npc, killer) = SpawnKiller();
        var recorder = RegisterRecorder("Scripts/Global/Record.csx");

        npc.Attacked(killer, 1000, this.fixture.World);

        Assert.Single(recorder.Kills);
        var (n, k, credited) = recorder.Kills[0];
        Assert.Same(npc, n);
        Assert.Same(killer, k);
        Assert.Equal([killer], credited);
    }

    [Fact]
    public void Group_kill_dispatches_once_with_credited_members_in_range()
    {
        var map = this.fixture.AddBaseMap(1, "m", 40, 40);
        var template = this.fixture.AddNpcTemplate(1, "CandyNpc", t => t.Experience = 100);
        var npc = this.fixture.SpawnNpc(map, 25, 25, template, false);
        var eligible1 = this.fixture.PlayerOn(map, 25, 26);
        var eligible2 = this.fixture.PlayerOn(map, 26, 25);
        var far = this.fixture.PlayerOn(map, 0, 0);
        var group = new Group();
        foreach (var p in new[] { eligible1, eligible2, far })
        {
            p.Group = group;
            group.Players.Add(p);
        }
        var recorder = RegisterRecorder("Scripts/Global/Record.csx");

        npc.Attacked(eligible1, 1000, this.fixture.World);

        Assert.Single(recorder.Kills);
        var (n, k, credited) = recorder.Kills[0];
        Assert.Same(npc, n);
        Assert.Same(eligible1, k);
        Assert.Equal([eligible1, eligible2], credited);
        Assert.DoesNotContain(far, credited);
    }

    [Fact]
    public void Throwing_global_script_does_not_stop_others_or_the_kill()
    {
        var (npc, killer) = SpawnKiller();

        var throwing = ScriptStub.For<IGlobalScript>(new ThrowingGlobalScript());
        throwing.FilePath = "Scripts/Global/Throw.csx";
        this.fixture.World.ScriptHandler.TryAdd(throwing);
        var recorder = RegisterRecorder("Scripts/Global/Record.csx");

        npc.Attacked(killer, 1000, this.fixture.World);

        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.Single(recorder.Kills);
        Assert.Same(killer, recorder.Kills[0].Killer);
        Assert.Equal(100, killer.Experience);
    }

    [Fact]
    public void Non_respawning_npc_still_has_properties_at_dispatch()
    {
        var map = this.fixture.AddBaseMap(1, "m");
        var template = this.fixture.AddNpcTemplate(1, "CandyNpc", t => t.Experience = 100);
        var npc = this.fixture.SpawnNpc(map, 5, 5, template, false,
            new PropertiesDictionary { ["candy"] = "ambush" });
        var killer = this.fixture.PlayerOn(map, 5, 6);
        var recorder = RegisterRecorder("Scripts/Global/Record.csx");

        npc.Attacked(killer, 1000, this.fixture.World);

        Assert.Single(recorder.Properties);
        Assert.Equal("ambush", recorder.Properties[0].GetProperty<string>("candy", null));
    }

    private sealed class RecordingGlobalScript : BaseGlobalScript
    {
        public List<(NPC Npc, ICharacter Killer, IReadOnlyList<Player> Credited)> Kills { get; } = [];
        public List<PropertiesDictionary> Properties { get; } = [];

        public override void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
        {
            this.Kills.Add((npc, killer, credited));
            this.Properties.Add(npc.Properties.Clone());
        }
    }

    private sealed class ThrowingGlobalScript : BaseGlobalScript
    {
        public override void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
            => throw new InvalidOperationException("boom");
    }
}
