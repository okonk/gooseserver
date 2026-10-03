using System.Data;
using System.Data.SQLite;
using System.Text.Json;
using Goose;
using Goose.Quests;

namespace Goose.IntegrationTests;

public class QuestClaimPersistenceTests : PlayerFirstSaveTestBase
{
    public QuestClaimPersistenceTests()
        : base(["players", "quest_claims", "logs"], ["quests", "quest_requirements", "quest_rewards"]) { }

    [Fact]
    public void Claiming_writes_a_row_and_reload_restores_it()
    {
        InsertQuestRow(9, oneTime: true);
        world.QuestHandler.LoadQuests(world);
        var player = new Player(0) { Name = "Hero", PlayerID = 7 };

        world.QuestHandler.Claim(world.QuestHandler.Get(9)!, player, world);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM quest_claims WHERE quest_id=9 AND player_id=7"));
        var firstCompletedAt = world.QuestHandler.Claims[9].CompletedAt;

        Thread.Sleep(20);
        world.QuestHandler.Claim(world.QuestHandler.Get(9)!, player, world);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM quest_claims WHERE quest_id=9"));
        var secondCompletedAt = world.QuestHandler.Claims[9].CompletedAt;

        var persisted = world.Database.Execute<string?>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT completed_at FROM quest_claims WHERE quest_id=9";
            return (string?)cmd.ExecuteScalar();
        });
        Assert.Equal(secondCompletedAt.ToString("o"), persisted);

        var reloaded = new QuestHandler();
        reloaded.LoadClaims(world);
        Assert.True(reloaded.IsClaimed(9));
        Assert.True(reloaded.TryGetClaim(9, out var claim));
        Assert.Equal(7, claim.PlayerId);
        Assert.Equal(DateTimeKind.Utc, claim.CompletedAt.Kind);
    }

    [Fact]
    public void Claiming_writes_the_roster_into_player_ids()
    {
        InsertQuestRow(9, oneTime: true);
        world.QuestHandler.LoadQuests(world);
        var quest = world.QuestHandler.Get(9)!;
        quest.Requirements.Add(new QuestRequirement { Id = 98, Quest = quest, Type = RequirementType.Gold, Value = 50 });

        var rostered = new Player(0) { Name = "Helper", PlayerID = 7, Gold = 100 };
        rostered.QuestsStarted.Add(quest);
        world.PlayerHandler.AddPlayerToData(rostered);

        var completer = new Player(0) { Name = "Hero", PlayerID = 8 };
        world.QuestHandler.Claim(quest, completer, world);
        world.Database.Execute(conn => { });

        var raw = world.Database.Execute<string?>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT player_ids FROM quest_claims WHERE quest_id=9";
            return (string?)cmd.ExecuteScalar();
        });
        Assert.NotNull(raw);
        var roster = JsonSerializer.Deserialize<HashSet<int>>(raw, JsonHelper.DatabaseOptions);
        Assert.Equal([7], roster);
        Assert.Equal(8, world.QuestHandler.Claims[9].PlayerId);
    }

    [Fact]
    public void Roster_survives_restart_and_unblocks_the_rostered_player()
    {
        InsertQuestRow(9, oneTime: true);
        world.QuestHandler.LoadQuests(world);
        var quest = world.QuestHandler.Get(9)!;
        quest.Requirements.Add(new QuestRequirement { Id = 98, Quest = quest, Type = RequirementType.Gold, Value = 50 });

        var rostered = new Player(0) { Name = "Helper", PlayerID = 7, Gold = 100 };
        rostered.QuestsStarted.Add(quest);
        world.PlayerHandler.AddPlayerToData(rostered);

        var completer = new Player(0) { Name = "Hero", PlayerID = 8 };
        world.QuestHandler.Claim(quest, completer, world);
        world.Database.Execute(conn => { });

        var reloaded = new QuestHandler();
        reloaded.LoadClaims(world);
        Assert.True(reloaded.TryGetClaim(9, out var claim));
        Assert.Equal([7], claim.Roster);

        Assert.False(reloaded.IsClaimedFor(quest, rostered));
        Assert.True(reloaded.IsClaimedFor(quest, new Player(0) { Name = "Stranger", PlayerID = 81 }));
    }

    [Fact]
    public void Migration_adds_player_ids_to_an_old_schema_table()
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DROP TABLE quest_claims";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "CREATE TABLE quest_claims (quest_id INT PRIMARY KEY, player_id INT NOT NULL, completed_at TEXT NOT NULL)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO quest_claims VALUES (9, 7, '2026-10-01T00:00:00.0000000Z')";
            cmd.ExecuteNonQuery();
        });

        world.MigrateDatabaseSchema(); // idempotent — every helper is guard-first

        var hasColumn = world.Database.Execute<bool>(conn =>
            GameWorld.ColumnExists(conn, "quest_claims", "player_ids"));
        Assert.True(hasColumn);

        var reloaded = new QuestHandler();
        reloaded.LoadClaims(world);
        Assert.True(reloaded.IsClaimed(9));
        Assert.True(reloaded.TryGetClaim(9, out var claim));
        Assert.Empty(claim.Roster);
    }

    [Fact]
    public void Loading_quests_does_not_touch_existing_claims()
    {
        InsertQuestRow(9, oneTime: true);
        world.QuestHandler.LoadQuests(world);
        world.QuestHandler.Claims[9] = new QuestClaim
        {
            QuestId = 9,
            PlayerId = 7,
            CompletedAt = DateTime.UtcNow,
            Roster = [7, 8],
        };

        world.QuestHandler.LoadQuests(world);

        Assert.True(world.QuestHandler.IsClaimed(9));
        Assert.True(world.QuestHandler.TryGetClaim(9, out var claim));
        Assert.Equal(7, claim.PlayerId);
        Assert.Equal([7, 8], claim.Roster);
    }

    [Fact]
    public void A_claim_written_by_the_window_flow_survives_a_reload_and_hides_the_quest_from_a_fresh_player()
    {
        InsertQuestRow(9, oneTime: true);
        world.QuestHandler.LoadQuests(world);
        var quest = world.QuestHandler.Get(9)!;
        quest.Requirements.Add(new QuestRequirement { Id = 98, Quest = quest, Type = RequirementType.Gold, Value = 50 });

        var npc = new NPC
        {
            NPCTemplate = new NPCTemplate { NPCTemplateID = 5, Name = "Quest NPC" },
            Quests = [quest],
        };

        var player = new Player(0)
        {
            Name = "Hero",
            PlayerID = 7,
            Level = 1,
            Class = new Class { ClassID = 0, ClassName = "Test" },
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Gold = 100,
        };
        player.Inventory = new Inventory(player, world.Settings);
        player.Spellbook = new Spellbook(player, world.Settings);

        player.QuestsStarted.Add(quest);
        var window = new QuestWindow(npc, player, quest, world);
        window.Clicked(Window.ButtonTypes.Next, npc.NPCTemplate.NPCTemplateID, 0, 0, player, world);

        Assert.Contains(player.QuestsCompleted, q => q.Id == quest.Id);
        Assert.Equal(50, player.Gold);
        world.Database.Execute(conn => { });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM quest_claims WHERE quest_id=9 AND player_id=7"));

        var reloaded = new QuestHandler();
        reloaded.LoadClaims(world);
        Assert.True(reloaded.IsClaimed(9));
        world.QuestHandler = reloaded;

        var fresh = new Player(0)
        {
            Name = "Newcomer",
            PlayerID = 8,
            Level = 1,
            Class = new Class { ClassID = 0, ClassName = "Test" },
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Gold = 100,
        };
        fresh.Inventory = new Inventory(fresh, world.Settings);
        fresh.Spellbook = new Spellbook(fresh, world.Settings);

        Assert.DoesNotContain(QuestWindow.GetAvailableQuests(npc, fresh, world), q => q.Id == quest.Id);
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
            cmd.Parameters.Add(new SQLiteParameter("@only_one_player_can_complete", DbType.String) { Value = oneTime ? "1" : "0" });
            cmd.Parameters.Add(new SQLiteParameter("@prerequisite_quests", DbType.String) { Value = "" });
            cmd.ExecuteNonQuery();
        });
    }
}
