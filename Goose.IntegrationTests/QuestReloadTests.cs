using Goose;
using Goose.Quests;

namespace Goose.IntegrationTests;

public class QuestReloadTests : PlayerFirstSaveTestBase
{
    public QuestReloadTests()
        : base([], ["quests", "quest_requirements", "quest_rewards", "npc_templates", "npc_drops", "npc_vendor_items"]) { }

    [Fact]
    public void Reload_removes_deleted_database_quests_and_keeps_script_quests()
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO quests (id, name, description, pass_text, fail_text, class_restrictions,
                    min_experience, max_experience, min_level, max_level, repeatable, show_progress,
                    only_one_player_can_complete, prerequisite_quests)
                VALUES (10, 'Database quest', 'desc', 'pass', 'fail', 0,
                    0, 0, 1, 99, '0', '0', '0', '')
                """;
            cmd.ExecuteNonQuery();
        });

        world.QuestHandler.LoadQuests(world);
        var databaseQuest = world.QuestHandler.Get(10)!;
        var template = new NPCTemplate
        {
            NPCTemplateID = 1,
            Name = "Quest giver",
            BaseStats = new AttributeSet(),
            Quests = [databaseQuest],
        };
        world.NPCHandler.AddTemplate(template);
        var npc = new NPC { Quests = template.Quests };

        var scriptQuest = new Quest { Id = 900000, Name = "Script quest" };
        world.QuestHandler.AddQuest(scriptQuest);
        template.Quests.Add(scriptQuest);
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM quests WHERE id=10";
            cmd.ExecuteNonQuery();
        });

        world.QuestHandler.LoadQuests(world);

        Assert.Null(world.QuestHandler.Get(10));
        Assert.Same(scriptQuest, Assert.Single(template.Quests));
        Assert.Same(scriptQuest, Assert.Single(npc.Quests));
        Assert.Same(scriptQuest, world.QuestHandler.Get(900000));
    }

    [Fact]
    public void Reloading_npc_templates_updates_live_npc_quest_lists_in_place()
    {
        var scriptQuest = new Quest { Id = 900000, Name = "Script quest" };
        world.QuestHandler.AddQuest(scriptQuest);
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO npc_templates (npc_id, npc_name, quest_ids, script_path) VALUES (1, 'Quest giver', '900000', '')";
            cmd.ExecuteNonQuery();
        });
        world.NPCHandler.LoadNPCTemplates(world);

        var template = world.NPCHandler.GetNPCTemplate(1)!;
        var questList = template.Quests;
        var npc = new NPC { Quests = questList };
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE npc_templates SET quest_ids='' WHERE npc_id=1";
            cmd.ExecuteNonQuery();
        });

        world.NPCHandler.LoadNPCTemplates(world);

        Assert.Same(questList, template.Quests);
        Assert.Empty(npc.Quests);
    }
}
