namespace Goose.IntegrationTests;

public class DoubleRoundTripTests : PlayerFirstSaveTestBase
{
    public DoubleRoundTripTests() : base("players", "banks", "pets", "quests", "classes") { }

    [Fact]
    public void Former_decimal_columns_round_trip_through_real_storage()
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE class_info SET haste=0.5, spell_damage=1.25 WHERE class_id=1 AND level=1";
            cmd.ExecuteNonQuery();
        });

        world.ClassHandler.LoadClasses(world);
        var level = world.ClassHandler.GetClass(1)!.GetLevel(1)!;
        Assert.Equal(0.5, level.BaseStats.Haste);
        Assert.Equal(1.25, level.BaseStats.SpellDamage);

        var player = MakePlayer();
        player.Access = Player.AccessStatus.Normal;
        player.AutoCreatedNotSaved = true;
        player.AetherThreshold = 0.04;
        player.SaveToDatabase(world);

        world.PlayerHandler.LoadPlayerData(world);
        var loaded = world.PlayerHandler.GetPlayerFromData("Test")!;
        Assert.Equal(0.04, loaded.AetherThreshold);

        loaded.SaveToDatabase(world);

        world.PlayerHandler.LoadPlayerData(world);
        var reloaded = world.PlayerHandler.GetPlayerFromData("Test")!;
        Assert.Equal(0.04, reloaded.AetherThreshold);
    }
}
