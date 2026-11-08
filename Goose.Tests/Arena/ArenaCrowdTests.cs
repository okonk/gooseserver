using System.Text.RegularExpressions;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaCrowdTests : IDisposable
{
    private readonly TestWorldFixture fixture = new();
    private readonly Map map;
    private readonly Script<INPCScript> script;

    public ArenaCrowdTests()
    {
        map = fixture.AddBaseMap(1, "Arena Lobby", 40, 40);
        var folder = Path.Combine(fixture.DataDirectory, "Scripts", "Arena");
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(ShippedDataDirectory, "Scripts", "Arena", "ArenaCrowd.csx"),
            Path.Combine(folder, "ArenaCrowd.csx"), overwrite: true);
        script = fixture.World.ScriptHandler.GetScript<INPCScript>("Scripts/Arena/ArenaCrowd.csx");
    }

    public void Dispose() => fixture.Dispose();

    private NPC SpawnCrowd(int id, string scriptParams = "")
    {
        var template = fixture.AddNpcTemplate(id, "Spectator " + id, t =>
        {
            t.NPCType = NPCTemplate.Types.Ambient;
            t.Script = script;
            t.ScriptParams = scriptParams;
        });
        return fixture.SpawnNpc(map, 10, 10, template);
    }

    private TestWorldFixture.CapturingPlayer Watcher(int x, int y)
    {
        var player = fixture.CommandPlayerOn(map, x, y);
        fixture.AddOnlinePlayer(player);
        map.Players.Add(player);
        player.Sent.Clear();
        return player;
    }

    private TimeSpan Call(string method, NPC npc)
        => (TimeSpan)script.Object.GetType().GetMethod(method)!.Invoke(script.Object, new object[] { npc, fixture.World })!;

    [Fact]
    public void Emote_sends_a_valid_animation_and_sheet_pair_to_nearby_players()
    {
        var npc = SpawnCrowd(291);
        var watcher = Watcher(12, 12);

        for (int i = 0; i < 50; i++) Call("Emote", npc);

        var emotes = watcher.Sent.Where(p => p.StartsWith("EMOT")).ToList();
        Assert.Equal(50, emotes.Count);
        foreach (var packet in emotes)
        {
            var match = Regex.Match(packet, @"^EMOT(\d+),(\d+),(\d+)");
            Assert.True(match.Success, packet);
            Assert.Equal(npc.LoginID, int.Parse(match.Groups[1].Value));
            int animation = int.Parse(match.Groups[2].Value);
            int sheet = int.Parse(match.Groups[3].Value);
            Assert.InRange(animation, 1080, 1091);
            Assert.Equal(8 + (animation - 1080) / 4, sheet);
        }
    }

    [Fact]
    public void Attack_sends_the_attack_animation()
    {
        var npc = SpawnCrowd(300);
        var watcher = Watcher(12, 12);

        Call("Attack", npc);

        Assert.Contains(watcher.Sent, p => p.StartsWith("ATT" + npc.LoginID));
    }

    [Fact]
    public void Players_out_of_range_receive_nothing()
    {
        var npc = SpawnCrowd(301);
        var far = Watcher(39, 39);

        Call("Emote", npc);
        Call("Attack", npc);

        Assert.DoesNotContain(far.Sent, p => p.StartsWith("EMOT") || p.StartsWith("ATT"));
    }

    [Fact]
    public void Sweat_token_only_uses_the_sweat_emote()
    {
        var npc = SpawnCrowd(443, "sweat");
        var watcher = Watcher(12, 12);

        for (int i = 0; i < 20; i++) Call("Emote", npc);

        var emotes = watcher.Sent.Where(p => p.StartsWith("EMOT")).ToList();
        Assert.Equal(20, emotes.Count);
        Assert.All(emotes, p => Assert.StartsWith($"EMOT{npc.LoginID},1088,10", p));
    }

    [Fact]
    public void Sweaty_npc_punches_in_bursts_and_sweats_once_per_burst()
    {
        var npc = SpawnCrowd(445, "sweat");
        var watcher = Watcher(12, 12);

        for (int burst = 0; burst < 10; burst++)
        {
            watcher.Sent.Clear();
            int hits = 0;
            TimeSpan wait;
            do
            {
                wait = Call("Attack", npc);
                hits++;
            } while (wait == TimeSpan.FromSeconds(0.6));

            Assert.InRange(hits, 3, 5);
            Assert.InRange(wait.TotalSeconds, 20, 60);
            Assert.Equal(hits, watcher.Sent.Count(p => p.StartsWith("ATT" + npc.LoginID)));
            Assert.Single(watcher.Sent, p => p.StartsWith($"EMOT{npc.LoginID},1088,10"));
        }
    }

    [Fact]
    public void Fast_spectators_attack_far_more_often()
    {
        var normal = SpawnCrowd(392);
        var fast = SpawnCrowd(394, "fast");

        for (int i = 0; i < 50; i++)
        {
            Assert.InRange(Call("Attack", normal).TotalSeconds, 20, 60);
            Assert.InRange(Call("Attack", fast).TotalSeconds, 3, 8);
        }
    }

    [Fact]
    public void Respawn_starts_a_new_generation_so_old_timers_retire()
    {
        var npc = SpawnCrowd(310);
        long first = npc.Properties.GetProperty<long>("crowd.gen", 0);

        npc.Spawn(fixture.World);

        Assert.Equal(first + 1, npc.Properties.GetProperty<long>("crowd.gen", 0));
    }

    private static string ShippedDataDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "Goose", "Data", "Illutia");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Goose/Data/Illutia not found");
        }
    }
}
