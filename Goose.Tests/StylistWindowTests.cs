using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class StylistWindowTests
{
    private const int StylistId = 910;

    private static (TestWorldFixture Fixture, Map Map, TestWorldFixture.CapturingPlayer Player, NPC Npc) Setup()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 40, 40);
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        fixture.AddOnlinePlayer(player);
        player.HairID = 33;
        player.FaceID = 2;
        player.Gold = 10000;
        var template = fixture.AddNpcTemplate(StylistId, "Stylist", t => t.NPCType = NPCTemplate.Types.Ambient);
        var npc = fixture.SpawnNpc(map, 5, 5, template, properties: new PropertiesDictionary { ["stylist"] = true });
        return (fixture, map, player, npc);
    }

    private static StylistWindow OpenWindow(TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, NPC npc)
    {
        Assert.True(fixture.RunCommand(player, "RC" + npc.MapX + "," + npc.MapY));
        var window = Assert.Single(player.Windows);
        return Assert.IsType<StylistWindow>(window);
    }

    [Fact]
    public void RightClick_opens_stylist_window()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            var window = OpenWindow(fixture, player, npc);

            Assert.Equal("Stylist", window.Title);
            Assert.Equal(Window.WindowFrames.Stylist, window.Frame);
            Assert.Equal(Window.WindowTypes.Stylist, window.Type);
            Assert.Equal("0,1,0,0,1", window.Buttons);
            Assert.Same(npc, window.NPC);

            Assert.Contains(player.Sent, s => s.StartsWith($"MKW{window.ID},34,Stylist,0,1,0,0,1,{npc.LoginID},"));
            Assert.Contains(player.Sent, s => s.StartsWith("ENW" + window.ID));
        }
    }

    [Fact]
    public void RightClick_second_open_replaces_first()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            var first = OpenWindow(fixture, player, npc);
            var second = OpenWindow(fixture, player, npc);

            Assert.Single(player.Windows);
            Assert.NotSame(first, second);
            Assert.Contains(player.Sent, s => s.StartsWith("CLW" + first.ID));
        }
    }

    [Fact]
    public void Change_hair_only_costs_one_slot()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);

            StylistWindow.Change(fixture.World, player, 12, 2);

            Assert.Equal(12, player.HairID);
            Assert.Equal(2, player.FaceID);
            Assert.Equal(8000, player.Gold);
        }
    }

    [Fact]
    public void Change_face_only_costs_one_slot()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);

            StylistWindow.Change(fixture.World, player, 33, 5);

            Assert.Equal(33, player.HairID);
            Assert.Equal(5, player.FaceID);
            Assert.Equal(8000, player.Gold);
        }
    }

    [Fact]
    public void Change_both_costs_two_slots()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            int r = player.HairR, g = player.HairG, b = player.HairB, a = player.HairA;

            StylistWindow.Change(fixture.World, player, 12, 5);

            Assert.Equal(12, player.HairID);
            Assert.Equal(5, player.FaceID);
            Assert.Equal(6000, player.Gold);
            Assert.Equal((r, g, b, a), (player.HairR, player.HairG, player.HairB, player.HairA));
        }
    }

    [Fact]
    public void Change_nothing_is_free_and_sends_no_update()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            player.Sent.Clear();

            StylistWindow.Change(fixture.World, player, 33, 2);

            Assert.Equal(10000, player.Gold);
            Assert.Contains(player.Sent, s => s.Contains("Nothing changed."));
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHP"));
        }
    }

    [Fact]
    public void Change_broadcasts_update_to_self_and_nearby()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            var nearby = fixture.CommandPlayerOn(map, 6, 5, "Bystander");
            map.Players.Add(nearby);
            player.Sent.Clear();
            nearby.Sent.Clear();

            StylistWindow.Change(fixture.World, player, 12, 5);

            Assert.Contains(player.Sent, s => s.StartsWith("CHP" + player.LoginID + ","));
            Assert.Contains(nearby.Sent, s => s.StartsWith("CHP" + player.LoginID + ","));
        }
    }

    [Fact]
    public void Change_with_one_gold_short_changes_nothing()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            player.Gold = 1999;
            player.Sent.Clear();

            StylistWindow.Change(fixture.World, player, 12, 2);

            Assert.Equal(33, player.HairID);
            Assert.Equal(2, player.FaceID);
            Assert.Equal(1999, player.Gold);
            Assert.Contains(player.Sent, s => s.Contains("You need 2000 gold."));
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHP"));
        }
    }

    [Fact]
    public void Change_both_with_gold_for_one_changes_nothing()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            player.Gold = 3000;
            player.Sent.Clear();

            StylistWindow.Change(fixture.World, player, 12, 5);

            Assert.Equal(33, player.HairID);
            Assert.Equal(2, player.FaceID);
            Assert.Equal(3000, player.Gold);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHP"));
        }
    }

    [Fact]
    public void Change_out_of_range_notices_and_changes_nothing()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            player.MapX = 30;
            player.Sent.Clear();

            StylistWindow.Change(fixture.World, player, 12, 5);

            Assert.Equal(33, player.HairID);
            Assert.Equal(2, player.FaceID);
            Assert.Equal(10000, player.Gold);
            Assert.Contains(player.Sent, s => s.Contains("You are too far from the stylist."));
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHP"));
        }
    }

    [Fact]
    public void Change_without_window_is_ignored()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            player.Sent.Clear();

            StylistWindow.Change(fixture.World, player, 12, 5);

            Assert.Equal(33, player.HairID);
            Assert.Equal(2, player.FaceID);
            Assert.Equal(10000, player.Gold);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHP"));
            Assert.Empty(player.Sent);
        }
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(47, 2)]
    [InlineData(33, 0)]
    [InlineData(33, 19)]
    [InlineData(-1, 2)]
    public void Change_with_out_of_range_ids_is_ignored(int hair, int face)
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            player.Sent.Clear();

            StylistWindow.Change(fixture.World, player, hair, face);

            Assert.Equal(33, player.HairID);
            Assert.Equal(2, player.FaceID);
            Assert.Equal(10000, player.Gold);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHP"));
            Assert.Empty(player.Sent);
        }
    }

    [Fact]
    public void Stc_packet_changes_hair_and_face()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);

            Assert.True(fixture.RunCommand(player, "STC12,5"));

            Assert.Equal(12, player.HairID);
            Assert.Equal(5, player.FaceID);
            Assert.Equal(6000, player.Gold);
        }
    }

    [Theory]
    [InlineData("STC")]
    [InlineData("STC12")]
    [InlineData("STC12,5,1")]
    [InlineData("STCa,5")]
    [InlineData("STC12,")]
    public void Stc_malformed_is_ignored(string command)
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);
            player.Sent.Clear();

            fixture.RunCommand(player, command);

            Assert.Equal(33, player.HairID);
            Assert.Equal(2, player.FaceID);
            Assert.Equal(10000, player.Gold);
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("CHP"));
        }
    }

    [Fact]
    public void Change_logs_old_and_new_ids_and_cost()
    {
        var (fixture, map, player, npc) = Setup();
        using (fixture)
        {
            OpenWindow(fixture, player, npc);

            StylistWindow.Change(fixture.World, player, 12, 5);

            var log = Assert.Single(fixture.World.LogHandler.Pending, l => l.Type == Log.Types.Stylist);
            Assert.Equal("Stylist: hair 33 -> 12 face 2 -> 5 cost 4000", log.Text);
        }
    }
}
