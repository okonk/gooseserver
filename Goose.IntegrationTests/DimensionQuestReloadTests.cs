using Goose;
using Goose.Quests;
using Goose.IntegrationTests.Fixtures;
using Goose.Testing;

namespace Goose.IntegrationTests;

/// <summary>The dimension unlock chain and the rebirth quest are created by scripts, not
/// sheet rows. /reloadsql re-runs QuestHandler.LoadQuests, which must not re-attach
/// requirements/rewards from the database to quests it did not just read from the
/// quests table - an empty DB result used to strip the scripted quests bare, making them
/// completable for nothing and locking the player out of the reward.</summary>
public class DimensionQuestReloadTests : IDisposable
{
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "quest-reload-" + Guid.NewGuid().ToString("N") + ".db");

    private GlobalScriptFixture LoadedWorld()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var boss = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40, CanBeKilled = true };
        boss.BaseStats = new AttributeSet { HP = 3704 };
        fixture.World.NPCHandler.AddTemplate(boss);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    private void ReloadSql(GlobalScriptFixture fixture)
    {
        fixture.World.Database.Start(dbPath);
        fixture.World.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SchemaDdl.ForAll("quests", "quest_requirements", "quest_rewards");
            cmd.ExecuteNonQuery();
        });
        fixture.World.QuestHandler.LoadQuests(fixture.World);
    }

    [Fact]
    public void Reload_keeps_script_quest_requirements_and_rewards()
    {
        using var fixture = LoadedWorld();

        ReloadSql(fixture);

        for (int dim = 0; dim < 6; dim++)
        {
            var quest = fixture.World.QuestHandler.Get(900000 + dim)!;
            var requirement = Assert.Single(quest.Requirements);
            Assert.Equal(RequirementType.Kill, requirement.Type);
            Assert.Equal(162 + 100000 * dim, requirement.Value);
            var reward = Assert.Single(quest.Rewards);
            Assert.Equal(RewardType.Script, reward.Type);
            Assert.Equal((dim + 1).ToString(), reward.ScriptParams);
        }

        var rebirth = fixture.World.QuestHandler.Get(910000)!;
        Assert.Equal(2, rebirth.Requirements.Count);
        Assert.Single(rebirth.Rewards);
    }

    [Fact]
    public void Reload_does_not_let_the_unlock_quest_complete_without_the_kill()
    {
        using var fixture = LoadedWorld();
        ReloadSql(fixture);

        var town = fixture.World.MapHandler.GetMap(GlobalScriptFixture.ShopsMapId)!;
        var warden = town.NPCs.First(n => n.NPCTemplate.NPCTemplateID == 800000);
        var player = fixture.CommandPlayerOn(town, 50, 50);
        player.Class = fixture.World.ClassHandler.GetClass(0)!;
        player.Spellbook = new Spellbook(player, fixture.Settings);

        QuestWindow.Handle(warden, player, fixture.World);
        var window = player.Windows.OfType<QuestWindow>().Single();
        window.Clicked(Window.ButtonTypes.Next, warden.NPCTemplate.NPCTemplateID, 0, 0, player, fixture.World);

        Assert.DoesNotContain(fixture.World.QuestHandler.Get(900000)!, player.QuestsCompleted);
        Assert.Equal(0, player.Properties.GetProperty<int>("dimension.max", 0));
    }

    [Fact]
    public void Completing_the_unlock_quest_after_a_reload_still_grants_the_dimension()
    {
        using var fixture = LoadedWorld();
        ReloadSql(fixture);

        var town = fixture.World.MapHandler.GetMap(GlobalScriptFixture.ShopsMapId)!;
        var warden = town.NPCs.First(n => n.NPCTemplate.NPCTemplateID == 800000);
        var bossNpc = new NPC { NPCTemplate = fixture.World.NPCHandler.GetNPCTemplate(162)! };

        var player = fixture.CommandPlayerOn(town, 50, 50);
        player.Class = fixture.World.ClassHandler.GetClass(0)!;
        player.Spellbook = new Spellbook(player, fixture.Settings);

        QuestWindow.Handle(warden, player, fixture.World);

        player.Killed(bossNpc, fixture.World);

        QuestWindow.Handle(warden, player, fixture.World);
        var window = player.Windows.OfType<QuestWindow>().Single();
        window.Clicked(Window.ButtonTypes.Next, warden.NPCTemplate.NPCTemplateID, 0, 0, player, fixture.World);

        Assert.Contains(fixture.World.QuestHandler.Get(900000)!, player.QuestsCompleted);
        Assert.Equal(1, player.Properties.GetProperty<int>("dimension.max", 0));
    }

    public void Dispose()
    {
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }
}
