using Goose;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

public class ReloadMapNpcsTests
{
    [Fact]
    public void Reapplies_template_stats_to_live_npcs_on_the_map()
    {
        using var fixture = new GlobalScriptFixture();
        var map = fixture.AddBaseMap(1, "Cavern", width: 20, height: 20);
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "ReloadMapNpcs.csx"));
        fixture.CompileSource(source, "ReloadMapNpcs.csx").Object.OnLoaded(fixture.World);

        var template = new NPCTemplate
        {
            NPCTemplateID = 1, Name = "Bear", Level = 50, ClassID = 3,
            BaseStats = new AttributeSet { HP = 2310 }, WeaponDamage = 100, Experience = 10,
            HairA = 255, FaceID = 70,
        };
        var npc = fixture.World.NPCHandler.SpawnNPC(fixture.World, map.ID, 5, 5, template, shouldRespawn: false)!;
        var classHp = npc.MaxHP - 2310;

        template.BaseStats = new AttributeSet { HP = 103176 };
        template.WeaponDamage = 4550;
        template.Experience = 4563;

        var player = fixture.CommandPlayerOn(map, 2, 2);
        player.Access = Player.AccessStatus.GameMaster;
        Assert.True(fixture.RunCommand(player, "/reloadmapnpcs"));

        Assert.Equal(103176 + classHp, npc.MaxHP);
        Assert.Equal(npc.MaxHP, npc.CurrentHP);
        Assert.Equal(4550, npc.WeaponDamage);
        Assert.Equal(4563, npc.Experience);
        Assert.Contains(player.Sent, m => m.Contains("Reloaded 1 NPCs"));
    }
}
