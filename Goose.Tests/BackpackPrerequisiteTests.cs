using System.Data.SQLite;
using Goose;
using Xunit;

namespace Goose.Tests;

public class BackpackPrerequisiteTests
{
    [Fact]
    public void MakeWindow_UsesFrameParams()
    {
        var window = new FrameParamWindow { ID = 1, Title = "T", Buttons = "0,0,0,0,0" };
        Assert.EndsWith(",20,5", P.MakeWindow(window));

        var plain = new Window { ID = 1, Title = "T", Buttons = "0,0,0,0,0" };
        Assert.EndsWith(",0,0", P.MakeWindow(plain));
    }

    private class FrameParamWindow : Window
    {
        public override int FrameParam1 => 20;
        public override int FrameParam2 => 5;
    }

    [Fact]
    public void ItemSlotToEquipSlot_Backpack()
    {
        var inventory = new Inventory(new Player(), new GooseSettings { EquippedSize = 15 });
        Assert.Equal(Inventory.EquipSlots.Backpack,
            inventory.ItemSlotToEquipSlot(ItemTemplate.ItemSlots.Backpack));
    }

    [Fact]
    public void Adds_a_missing_backpack_column_and_is_idempotent()
    {
        using var conn = new SQLiteConnection("Data Source=:memory:");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE players (player_id INT PRIMARY KEY);";
            cmd.ExecuteNonQuery();
        }

        Assert.False(GameWorld.ColumnExists(conn, "players", "backpack"));

        GameWorld.AddColumnIfMissing(conn, "players", "backpack", "TEXT DEFAULT '' NOT NULL");
        Assert.True(GameWorld.ColumnExists(conn, "players", "backpack"));

        // Running again must not throw - migrations run on every startup.
        GameWorld.AddColumnIfMissing(conn, "players", "backpack", "TEXT DEFAULT '' NOT NULL");
        Assert.True(GameWorld.ColumnExists(conn, "players", "backpack"));
    }
}
