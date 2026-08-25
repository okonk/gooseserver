namespace Goose.IntegrationTests;

public class GuildSaveTests : PlayerFirstSaveTestBase
{
    public GuildSaveTests() : base("players", "banks", "pets", "quests", "guilds") { }

    [Fact]
    public void A_new_guild_is_persisted_with_the_player_row()
    {
        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;

        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(player.PlayerID, Guild.GuildRanks.Leader, dirty: true, justadded: true);
        guild.OnlineMembers.Add(player);
        player.Guild = guild;

        player.SaveToDatabase(world);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM guilds"));
        int guildId = GuildId();
        Assert.True(guildId > 0);
        Assert.Equal(1, Count("SELECT COUNT(*) FROM guild_members WHERE guild_id=" + guildId + " AND player_id=" + player.PlayerID));
        Assert.Equal(guildId, PlayerGuildId(player.PlayerID));
        Assert.Equal(guildId, player.GuildID);
    }

    [Fact]
    public void Re_saving_an_existing_player_keeps_the_guild_id()
    {
        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;

        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(player.PlayerID, Guild.GuildRanks.Leader, dirty: true, justadded: true);
        guild.OnlineMembers.Add(player);
        player.Guild = guild;

        player.SaveToDatabase(world);
        player.SaveToDatabase(world);

        int guildId = GuildId();
        Assert.Equal(guildId, PlayerGuildId(player.PlayerID));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM guilds"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM guild_members"));
    }

    private int GuildId()
    {
        return world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT guild_id FROM guilds LIMIT 1";
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    private int PlayerGuildId(int playerId)
    {
        return world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT guild_id FROM players WHERE player_id=" + playerId;
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }
}

public class GuildMemberUpsertTests : PlayerFirstSaveTestBase
{
    public GuildMemberUpsertTests() : base("guilds") { }

    [Fact]
    public void Re_running_a_member_upsert_without_clearing_flags_does_not_violate_the_primary_key()
    {
        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(1, Guild.GuildRanks.Member, dirty: true, justadded: true);

        var (save1, commit1) = guild.BuildSave();
        world.Database.EnqueueTransaction(save1, commit1);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM guilds"));
        int guildId = world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT guild_id FROM guilds LIMIT 1";
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
        Assert.Equal(1, Count("SELECT COUNT(*) FROM guild_members WHERE guild_id=" + guildId + " AND player_id=1"));

        // Crash-desync scenario: the member row exists but the dirty flags were never
        // cleared, so the next save re-issues the same member upsert against it.
        var status = guild.Members[1];
        status.Dirty = true;
        status.JustAdded = true;
        guild.Dirty = true;

        var (save2, commit2) = guild.BuildSave();
        world.Database.EnqueueTransaction(save2, commit2);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM guilds"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM guild_members"));
        Assert.False(guild.Dirty);
        Assert.False(status.Dirty);
        Assert.False(status.JustAdded);
    }

    [Fact]
    public void Two_saves_built_at_id_zero_enqueue_exactly_one_guild_row()
    {
        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(1, Guild.GuildRanks.Member, dirty: true, justadded: true);

        var (save1, commit1) = guild.BuildSave();
        var (save2, commit2) = guild.BuildSave();
        world.Database.EnqueueTransaction(save1, commit1);
        world.Database.EnqueueTransaction(save2, commit2);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM guilds"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM guild_members"));
        Assert.False(guild.Dirty);
        Assert.True(guild.ID > 0);
    }

    [Fact]
    public void A_rank_change_landing_after_the_snapshot_survives_until_the_next_save()
    {
        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(1, Guild.GuildRanks.Member, dirty: true, justadded: true);

        var (save1, commit1) = guild.BuildSave();
        guild.ChangeRank(new Player(0) { PlayerID = 1 }, Guild.GuildRanks.Officer, world);

        world.Database.EnqueueTransaction(save1, commit1);

        Assert.Equal((int)Guild.GuildRanks.Member, MemberRank(guild));
        var status = guild.Members[1];
        Assert.True(status.Dirty);
        Assert.True(status.JustAdded);
        Assert.True(guild.Dirty);

        var (save2, commit2) = guild.BuildSave();
        world.Database.EnqueueTransaction(save2, commit2);

        Assert.Equal((int)Guild.GuildRanks.Officer, MemberRank(guild));
        Assert.False(status.Dirty);
        Assert.False(status.JustAdded);
        Assert.False(guild.Dirty);
    }

    private int MemberRank(Guild guild)
    {
        return world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT guild_rank FROM guild_members WHERE guild_id=" + guild.ID + " AND player_id=1";
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    [Fact]
    public void A_kicked_member_is_deleted_and_removed_from_the_guild()
    {
        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(1, Guild.GuildRanks.Member, dirty: true, justadded: true);
        guild.AddMember(2, Guild.GuildRanks.Member, dirty: true, justadded: true);

        var (save1, commit1) = guild.BuildSave();
        world.Database.EnqueueTransaction(save1, commit1);
        Assert.Equal(2, Count("SELECT COUNT(*) FROM guild_members"));

        guild.Members[2].Rank = Guild.GuildRanks.Deleted;
        guild.Members[2].Dirty = true;
        guild.Dirty = true;

        var (save2, commit2) = guild.BuildSave();
        world.Database.EnqueueTransaction(save2, commit2);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM guild_members"));
        Assert.False(guild.Members.ContainsKey(2));
    }
}

public class GuildSaveCadenceTests : PlayerFirstSaveTestBase
{
    public GuildSaveCadenceTests() : base("guilds") { }

    [Fact]
    public void The_save_cadence_persists_a_dirty_existing_guild()
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO guilds (guild_name, guild_motd) VALUES ('TestGuild', 'motd')";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO guild_members (guild_id, player_id, guild_rank) VALUES (1, 7, 1)";
            cmd.ExecuteNonQuery();
        });

        var handler = new GuildHandler();
        handler.LoadGuilds(world);
        var guild = handler.GetGuild(1);
        Assert.NotNull(guild);

        var status = guild.Members[7];
        status.Rank = Guild.GuildRanks.Officer;
        status.Dirty = true;
        guild.Dirty = true;

        handler.Save(world);

        Assert.Equal(1, Count("SELECT COUNT(*) FROM guilds"));
        int rank = world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT guild_rank FROM guild_members WHERE guild_id=1 AND player_id=7";
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
        Assert.Equal((int)Guild.GuildRanks.Officer, rank);
        Assert.False(status.Dirty);
        Assert.False(guild.Dirty);
    }
}

public class GuildSaveRollbackTests : PlayerFirstSaveTestBase
{
    // quest_status deliberately absent: the quest upsert is the last part of a player
    // save, so it fails and rolls back everything queued before it.
    public GuildSaveRollbackTests() : base("players", "banks", "pets", "guilds") { }

    [Fact]
    public void A_failed_player_save_rolls_back_the_new_guild_too()
    {
        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;

        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(player.PlayerID, Guild.GuildRanks.Leader, dirty: true, justadded: true);
        guild.OnlineMembers.Add(player);
        player.Guild = guild;

        player.SaveToDatabase(world);

        Assert.Equal(0, Count("SELECT COUNT(*) FROM guilds"));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM guild_members"));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM players WHERE player_id=" + player.PlayerID));
    }

    [Fact]
    public void A_rolled_back_first_save_leaves_memory_unchanged_and_the_retry_persists_cleanly()
    {
        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;

        var guild = new Guild { Name = "TestGuild", MOTD = "motd" };
        guild.AddMember(player.PlayerID, Guild.GuildRanks.Leader, dirty: true, justadded: true);
        guild.OnlineMembers.Add(player);
        player.Guild = guild;
        var status = guild.Members[player.PlayerID];

        player.SaveToDatabase(world);
        Count("SELECT COUNT(*) FROM guilds");

        Assert.Equal(0, Count("SELECT COUNT(*) FROM guilds"));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM guild_members"));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM players WHERE player_id=" + player.PlayerID));
        Assert.Equal(0, guild.ID);
        Assert.True(status.Dirty);
        Assert.True(status.JustAdded);
        Assert.True(guild.Dirty);
        Assert.Equal(0, player.GuildID);
        Assert.True(player.AutoCreatedNotSaved);

        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "quests.sql"));
            cmd.ExecuteNonQuery();
        });

        player.SaveToDatabase(world);
        Count("SELECT COUNT(*) FROM guilds");

        Assert.Equal(1, Count("SELECT COUNT(*) FROM guilds"));
        int guildId = GuildId();
        Assert.True(guildId > 0);
        Assert.Equal(guildId, guild.ID);
        Assert.Equal(1, Count("SELECT COUNT(*) FROM guild_members WHERE guild_id=" + guildId + " AND player_id=" + player.PlayerID));
        Assert.Equal(guildId, PlayerGuildId(player.PlayerID));
        Assert.Equal(guildId, player.GuildID);
        Assert.False(status.Dirty);
        Assert.False(status.JustAdded);
        Assert.False(guild.Dirty);
    }

    private int GuildId()
    {
        return world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT guild_id FROM guilds LIMIT 1";
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    private int PlayerGuildId(int playerId)
    {
        return world.Database.Execute<int>(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT guild_id FROM players WHERE player_id=" + playerId;
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }
}
