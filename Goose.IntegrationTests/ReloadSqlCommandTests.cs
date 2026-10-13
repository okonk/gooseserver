using Goose;
using Goose.Commands;
using Goose.Testing;

namespace Goose.IntegrationTests;

public class ReloadSqlCommandTests : PlayerFirstSaveTestBase
{
    private readonly TestWorldFixture.CapturingPlayer player = new() { State = Player.States.Ready };

    public ReloadSqlCommandTests()
        : base([], ["spell_effects", "spells", "item_templates", "quests", "quest_requirements", "quest_rewards",
                    "npc_templates", "npc_drops", "npc_vendor_items"]) { }

    private void Sql(string sql) => world.Database.Execute(conn =>
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    });

    private void ReloadAndAwaitCompletion()
    {
        new ReloadSqlCommand().Execute(new CommandContext(player, world, new CommandRegistry(), [], ""));

        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (world.PendingCompletionCount == 0)
        {
            Assert.True(DateTime.UtcNow < deadline, "reload never enqueued its completion");
            Thread.Sleep(20);
        }
    }

    [Fact]
    public void Reload_AppliesSheetDataOnlyOnTheGameThread()
    {
        Sql("INSERT INTO quests (id, name) VALUES (10, 'Database quest')");

        ReloadAndAwaitCompletion();
        Assert.Null(world.QuestHandler.Get(10));

        world.Update();

        Assert.Equal("Database quest", world.QuestHandler.Get(10)!.Name);
        Assert.Contains(player.Sent, s => s.Contains("Reloaded sql data."));
    }

    [Fact]
    public void Reload_MissingScriptAbortsBeforeTouchingAnyData()
    {
        Sql("INSERT INTO quests (id, name) VALUES (10, 'Database quest')");
        Sql("INSERT INTO npc_templates (npc_id, npc_name, script_path) VALUES (1, 'Scripted', 'Scripts/Missing.csx')");

        ReloadAndAwaitCompletion();
        world.Update();

        Assert.Null(world.QuestHandler.Get(10));
        Assert.Contains(player.Sent, s => s.Contains("Failed reloading sql") && s.Contains("Missing.csx"));
    }
}
