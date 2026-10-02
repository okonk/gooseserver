using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class GrantTitleCommandTests
{
    private sealed class Ctx : IDisposable
    {
        public TestWorldFixture Fixture { get; }
        public Map Map { get; }
        public TestWorldFixture.CapturingPlayer Target { get; }
        public TestWorldFixture.CapturingPlayer Gm { get; }

        public Ctx(bool targetRegistered = true)
        {
            this.Fixture = new TestWorldFixture();
            this.Map = this.Fixture.AddBaseMap(1, "m", 60, 40);
            this.Target = this.Fixture.CommandPlayerOn(this.Map, 5, 5, "Target");
            this.Gm = this.Fixture.CommandPlayerOn(this.Map, 6, 5, "Gm");
            this.Gm.Access = Player.AccessStatus.GameMaster;
            if (targetRegistered)
            {
                this.Fixture.RegisterOnlinePlayer(this.Target);
                this.Fixture.RegisterDatabasePlayer(this.Target);
            }
        }

        public void Dispose() => this.Fixture.Dispose();
    }

    [Fact]
    public void Granttitle_unlocks_and_equips()
    {
        using var ctx = new Ctx();

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/granttitle Target Lord of the Vast"));

        Assert.Equal("Lord of the Vast", ctx.Target.Title);
        Assert.Equal(new List<string> { "Lord of the Vast" }, ctx.Target.UnlockedTitles());
    }

    [Fact]
    public void Settitle_equips_without_unlocking()
    {
        using var ctx = new Ctx();

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/settitle Target Temporary Title"));

        Assert.Equal("Temporary Title", ctx.Target.Title);
        Assert.Empty(ctx.Target.UnlockedTitles());
    }

    [Fact]
    public void Granting_twice_to_the_same_player_stores_one_entry()
    {
        using var ctx = new Ctx();

        ctx.Fixture.RunCommand(ctx.Gm, "/granttitle Target Lord");
        ctx.Fixture.RunCommand(ctx.Gm, "/granttitle Target lord");

        Assert.Equal(new List<string> { "Lord" }, ctx.Target.UnlockedTitles());
    }

    [Fact]
    public void Unknown_target_reports_failure_and_unlocks_nothing()
    {
        using var ctx = new Ctx(targetRegistered: false);

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/granttitle Nobody Lord"));

        Assert.Contains(ctx.Gm.Sent, s => s.Contains("Couldn't find player"));
        Assert.Empty(ctx.Target.UnlockedTitles());
    }

    [Fact]
    public void An_offline_target_is_saved_rather_than_broadcast_to()
    {
        using var ctx = new Ctx(targetRegistered: false);
        var offline = new Player(0)
        {
            Name = "Offline",
            Title = "",
            Surname = "",
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = ctx.Fixture.World.ClassHandler.GetClass(0)!,
        };
        offline.Inventory = new Inventory(offline, ctx.Fixture.Settings);
        offline.Spellbook = new Spellbook(offline, ctx.Fixture.Settings);
        offline.Bank = new PlayerBank();
        ctx.Fixture.RegisterDatabasePlayer(offline);
        ctx.Fixture.World.Database.Start(Path.Combine(ctx.Fixture.DataDirectory, "test.db"));
        ctx.Fixture.World.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "players.sql"));
            cmd.ExecuteNonQuery();
        });

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/granttitle Offline Lord"));

        Assert.Equal("Lord", offline.Title);
        Assert.Equal(new List<string> { "Lord" }, offline.UnlockedTitles());
    }

    [Fact]
    public void An_unprivileged_actor_changes_nothing()
    {
        using var ctx = new Ctx();
        ctx.Gm.Access = Player.AccessStatus.Normal;

        Assert.True(ctx.Fixture.RunCommand(ctx.Gm, "/granttitle Target Lord"));

        Assert.Empty(ctx.Target.UnlockedTitles());
        Assert.NotEqual("Lord", ctx.Target.Title);
    }
}
