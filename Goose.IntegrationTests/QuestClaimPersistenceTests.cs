using System.Data;
using System.Data.SQLite;
using Goose.Quests;

namespace Goose.IntegrationTests;

public class QuestClaimPersistenceTests : PlayerFirstSaveTestBase
{
    public QuestClaimPersistenceTests()
        : base(["players", "quest_claims"], ["quests", "quest_requirements", "quest_rewards"]) { }

    [Fact]
    public void Claiming_writes_a_row_and_reload_restores_it()
    {
        InsertQuestRow(9, oneTime: true);
        world.QuestHandler.LoadQuests(world);
        var player = new Player(0) { Name = "Hero", PlayerID = 7 };

        world.QuestHandler.Claim(world.QuestHandler.Get(9)!, player, world);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM quest_claims WHERE quest_id=9 AND player_id=7"));

        var reloaded = new QuestHandler();
        reloaded.LoadQuests(world);
        Assert.True(reloaded.IsClaimed(9));
        Assert.True(reloaded.TryGetClaim(9, out var claim));
        Assert.Equal(7, claim.PlayerId);
    }

    private void InsertQuestRow(int id, bool oneTime)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                @"INSERT INTO quests (id, name, description, pass_text, fail_text, class_restrictions,
                    min_experience, max_experience, min_level, max_level, repeatable, show_progress,
                    only_one_player_can_complete, prerequisite_quests)
                  VALUES (@id, @name, @description, @pass_text, @fail_text, @class_restrictions,
                    @min_experience, @max_experience, @min_level, @max_level, @repeatable, @show_progress,
                    @only_one_player_can_complete, @prerequisite_quests)";
            cmd.Parameters.Add(new SQLiteParameter("@id", DbType.Int32) { Value = id });
            cmd.Parameters.Add(new SQLiteParameter("@name", DbType.String) { Value = "Quest " + id });
            cmd.Parameters.Add(new SQLiteParameter("@description", DbType.String) { Value = "desc" });
            cmd.Parameters.Add(new SQLiteParameter("@pass_text", DbType.String) { Value = "pass" });
            cmd.Parameters.Add(new SQLiteParameter("@fail_text", DbType.String) { Value = "fail" });
            cmd.Parameters.Add(new SQLiteParameter("@class_restrictions", DbType.Int64) { Value = 0 });
            cmd.Parameters.Add(new SQLiteParameter("@min_experience", DbType.Int64) { Value = 0 });
            cmd.Parameters.Add(new SQLiteParameter("@max_experience", DbType.Int64) { Value = 0 });
            cmd.Parameters.Add(new SQLiteParameter("@min_level", DbType.Int32) { Value = 1 });
            cmd.Parameters.Add(new SQLiteParameter("@max_level", DbType.Int32) { Value = 99 });
            cmd.Parameters.Add(new SQLiteParameter("@repeatable", DbType.String) { Value = oneTime ? "0" : "1" });
            cmd.Parameters.Add(new SQLiteParameter("@show_progress", DbType.String) { Value = "0" });
            cmd.Parameters.Add(new SQLiteParameter("@only_one_player_can_complete", DbType.String) { Value = "0" });
            cmd.Parameters.Add(new SQLiteParameter("@prerequisite_quests", DbType.String) { Value = "" });
            cmd.ExecuteNonQuery();
        });
    }
}
