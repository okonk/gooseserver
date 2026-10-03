using Goose;
using Goose.Testing;

namespace Goose.Tests;

public class CommunityChestWiringTests
{
    private const int ChestBankerId = 77;
    private const int PlainBankerId = 78;
    private const int EmptyPropsBankerId = 79;

    private static NPC SpawnBanker(TestWorldFixture fixture, int templateId, PropertiesDictionary? properties = null)
    {
        var template = new NPCTemplate
        {
            NPCTemplateID = templateId, Name = "Banker " + templateId, Level = 50, ClassID = 0,
            NPCType = NPCTemplate.Types.Banker,
            BaseStats = new AttributeSet(),
        };
        fixture.World.NPCHandler.AddTemplate(template);
        return fixture.World.NPCHandler.SpawnNPC(
            fixture.World, 1, 5, 5, template, shouldRespawn: true, properties)!;
    }

    private static (TestWorldFixture Fixture, Map Map, TestWorldFixture.CapturingPlayer Player) Setup()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town");
        var player = fixture.CommandPlayerOn(map, 5, 5, "A");
        player.Bank = new PlayerBank();
        player.NumberOfBankPages = 1;
        player.Windows = new List<Window>();
        fixture.AddOnlinePlayer(player);
        return (fixture, map, player);
    }

    [Fact]
    public void PropertyTrue_OpensChestWindow()
    {
        var (fixture, map, player) = Setup();
        using (fixture)
        {
            var npc = SpawnBanker(fixture, ChestBankerId, new PropertiesDictionary { ["communityChest"] = true });

            Assert.True(fixture.RunCommand(player, "RC" + npc.MapX + "," + npc.MapY));

            var window = Assert.Single(player.Windows);
            Assert.IsType<CommunityChestWindow>(window);
        }
    }

    [Fact]
    public void NoProperty_OpensBankWindow()
    {
        var (fixture, map, player) = Setup();
        using (fixture)
        {
            var npc = SpawnBanker(fixture, PlainBankerId);

            Assert.True(fixture.RunCommand(player, "RC" + npc.MapX + "," + npc.MapY));

            var window = Assert.Single(player.Windows);
            Assert.IsType<BankWindow>(window);
        }
    }

    [Fact]
    public void RuntimeSpawnEmptyProperties_OpensBankWindow()
    {
        var (fixture, map, player) = Setup();
        using (fixture)
        {
            var npc = SpawnBanker(fixture, EmptyPropsBankerId, new PropertiesDictionary());

            Assert.True(fixture.RunCommand(player, "RC" + npc.MapX + "," + npc.MapY));

            var window = Assert.Single(player.Windows);
            Assert.IsType<BankWindow>(window);
        }
    }
}
