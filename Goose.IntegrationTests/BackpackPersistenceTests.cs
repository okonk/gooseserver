using System.Data.SQLite;
using Xunit;

namespace Goose.IntegrationTests;

public class BackpackPersistenceTests : PlayerFirstSaveTestBase
{
    public BackpackPersistenceTests() : base(["players", "banks", "logs"]) { }

    [Fact]
    public void Backpack_contents_round_trip_through_save_and_load()
    {
        var backpackTemplate = new ItemTemplate
        {
            ID = 700,
            Name = "Backpack",
            Slot = ItemTemplate.ItemSlots.Backpack,
            BaseStats = new AttributeSet(),
        };
        world.ItemHandler.AddTemplate(backpackTemplate);
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 701,
            Name = "Apple",
            BaseStats = new AttributeSet(),
        });
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 702,
            Name = "Sword",
            BaseStats = new AttributeSet(),
        });

        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;
        player.MaxStats = new AttributeSet();
        player.Class = new Class { ClassID = 1 };

        var backpack = new Item { TemplateID = 700, BaseStats = new AttributeSet() };
        backpack.Template = backpackTemplate;
        backpack.SetBackpackSize(10, 5);
        player.Inventory.GetEquippedSlots()[(int)Inventory.EquipSlots.Backpack] =
            new ItemSlot { Item = backpack, Stack = 1 };

        var container = player.Inventory.GetBackpackContainer();
        container.Resize(11);
        container.SetSlot(1, new ItemSlot
        {
            Item = new Item { TemplateID = 701, BaseStats = new AttributeSet() },
            Stack = 3,
        });
        container.SetSlot(10, new ItemSlot
        {
            Item = new Item { TemplateID = 702, BaseStats = new AttributeSet() },
            Stack = 1,
        });

        player.SaveToDatabase(world);

        var loaded = new Inventory(player, world.Settings);
        loaded.Load(world);

        var equippedBackpack = loaded.EquippedBackpack;
        Assert.NotNull(equippedBackpack);
        Assert.Equal(700, equippedBackpack!.TemplateID);
        Assert.Equal(10, equippedBackpack.BackpackSlots);

        var loadedContainer = loaded.GetBackpackContainer();
        Assert.Equal(11, loadedContainer.MaxSlots);
        var slot1 = loadedContainer.GetSlot(1);
        Assert.NotNull(slot1);
        Assert.Equal(701, slot1!.Item.TemplateID);
        Assert.Equal(3, slot1.Stack);
        var slot10 = loadedContainer.GetSlot(10);
        Assert.NotNull(slot10);
        Assert.Equal(702, slot10!.Item.TemplateID);
        Assert.Equal(1, slot10.Stack);
        Assert.Null(loadedContainer.GetSlot(2));
    }

    [Fact]
    public void First_save_of_a_new_player_persists_backpack()
    {
        var player = MakePlayer();
        player.AutoCreatedNotSaved = true;

        player.SaveToDatabase(world);

        Assert.Equal(JsonHelper.Serialize(new ItemContainer(1)), BackpackColumn());
    }

    [Fact]
    public void Contents_without_an_equipped_backpack_are_logged_and_cleared()
    {
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 701,
            Name = "Apple",
            BaseStats = new AttributeSet(),
        });

        InsertPlayerRow(BackpackJson((1, SlotJson(701)), (5, SlotJson(701))));
        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray()));

        var player = MakePlayer();
        player.Inventory.Load(world);

        Assert.False(player.Inventory.BackpackHasItems);
        Assert.Equal(1, player.Inventory.GetBackpackContainer().MaxSlots);

        player.SaveToDatabase(world);

        Assert.Equal(JsonHelper.Serialize(new ItemContainer(1)), BackpackColumn());
    }

    [Fact]
    public void Contents_are_preserved_when_the_backpack_template_is_missing()
    {
        string raw = BackpackJson((1, SlotJson(701)), (10, SlotJson(702)));
        InsertPlayerRow(raw);
        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((15, SlotJson(999)))));

        var player = MakePlayer();
        player.Inventory.Load(world);

        Assert.True(player.Inventory.BackpackContentsPreserved);
        Assert.Null(player.Inventory.EquippedBackpack);
        Assert.False(player.Inventory.BackpackHasItems);

        player.SaveToDatabase(world);

        Assert.Equal(raw, BackpackColumn());
    }

    [Fact]
    public void Contents_beyond_backpack_size_are_discarded()
    {
        var backpackTemplate = new ItemTemplate
        {
            ID = 700,
            Name = "Backpack",
            Slot = ItemTemplate.ItemSlots.Backpack,
            BaseStats = new AttributeSet(),
        };
        world.ItemHandler.AddTemplate(backpackTemplate);
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 701,
            Name = "Apple",
            BaseStats = new AttributeSet(),
        });

        InsertPlayerRow(BackpackJson((15, SlotJson(701))));
        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((15, SlotJson(700, backpackSlots: 10)))));

        var player = MakePlayer();
        player.MaxStats = new AttributeSet();
        player.Class = new Class { ClassID = 1 };
        player.Inventory.Load(world);

        var container = player.Inventory.GetBackpackContainer();
        Assert.Equal(11, container.MaxSlots);
        Assert.False(player.Inventory.BackpackHasItems);
    }

    [Fact]
    public void Contents_are_ignored_when_there_is_no_backpack_slot()
    {
        settings.EquippedSize = 14;
        InsertPlayerRow(BackpackJson((1, SlotJson(701))));
        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", NullArray(14)));

        var player = MakePlayer();
        player.Inventory.Load(world);

        Assert.False(player.Inventory.HasBackpackSlot);
        Assert.Null(player.Inventory.EquippedBackpack);
        Assert.False(player.Inventory.BackpackHasItems);
        Assert.Equal(1, player.Inventory.GetBackpackContainer().MaxSlots);

        player.SaveToDatabase(world);

        Assert.Equal(JsonHelper.Serialize(new ItemContainer(1)), BackpackColumn());
    }

    [Fact]
    public void A_corrupt_backpack_blob_loads_empty()
    {
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 700,
            Name = "Backpack",
            Slot = ItemTemplate.ItemSlots.Backpack,
            BaseStats = new AttributeSet(),
        });

        InsertPlayerRow("not json");
        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((15, SlotJson(700, backpackSlots: 10)))));

        var player = MakePlayer();
        player.MaxStats = new AttributeSet();
        player.Class = new Class { ClassID = 1 };
        player.Inventory.Load(world);

        Assert.NotNull(player.Inventory.EquippedBackpack);
        Assert.Equal(11, player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.False(player.Inventory.BackpackHasItems);
    }

    [Fact]
    public void A_non_backpack_item_in_the_backpack_slot_loads_as_an_empty_backpack()
    {
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 703,
            Name = "Tank",
            Slot = ItemTemplate.ItemSlots.Mount,
            BaseStats = new AttributeSet(),
        });

        InsertPlayerRow(BackpackJson((1, SlotJson(701))));
        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((15, SlotJson(703)))));

        var player = MakePlayer();
        player.MaxStats = new AttributeSet();
        player.Class = new Class { ClassID = 1 };
        player.Inventory.Load(world);

        Assert.NotNull(player.Inventory.EquippedBackpack);
        Assert.Equal(703, player.Inventory.EquippedBackpack!.TemplateID);
        Assert.Equal(1, player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.False(player.Inventory.BackpackHasItems);
    }

    [Fact]
    public void MigrateDatabaseSchema_adds_backpack_to_an_old_players_table()
    {
        world.Database.Execute(conn =>
        {
            string sql = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "players.sql"));
            using var cmd = conn.CreateCommand();
            foreach (var table in new[] { "players", "inventory", "equipped", "combinebag", "spellbook", "quest_status" })
            {
                cmd.CommandText = "DROP TABLE " + table + ";";
                cmd.ExecuteNonQuery();
            }
            cmd.CommandText = sql.Replace(",\n  backpack TEXT DEFAULT '' NOT NULL", "");
            cmd.ExecuteNonQuery();
        });

        Assert.False(BackpackColumnExists());

        world.MigrateDatabaseSchema();

        Assert.True(BackpackColumnExists());

        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO players (player_id, player_name, password_hash, password_salt) VALUES (1, 'Test', 'hash', 'salt')";
            cmd.ExecuteNonQuery();
        });

        Assert.Equal("", BackpackColumn());
    }

    [Fact]
    public void Old_fifteen_entry_equipped_blob_loads_with_a_null_backpack_slot()
    {
        settings.EquippedSize = 15;
        Insert("INSERT INTO equipped (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FifteenEntryArray()));

        var player = MakePlayer();
        player.Inventory.Load(world);

        Assert.True(player.Inventory.HasBackpackSlot);
        Assert.Null(player.Inventory.GetEquippedSlot(15));
        Assert.False(player.Inventory.BackpackHasItems);
    }

    private bool BackpackColumnExists()
    {
        return world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info(players)";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                if (reader.GetString("name") == "backpack") return true;
            return false;
        });
    }

    private string? BackpackColumn()
    {
        return world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT backpack FROM players WHERE player_id=1";
            return Convert.ToString(cmd.ExecuteScalar());
        });
    }

    private void InsertPlayerRow(string? backpack = null)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO players (player_id, player_name, password_hash, password_salt) VALUES (1, 'Test', 'hash', 'salt')";
            cmd.ExecuteNonQuery();
            if (backpack is not null)
            {
                using var update = conn.CreateCommand();
                update.CommandText = "UPDATE players SET backpack=@b WHERE player_id=1";
                update.Parameters.Add(new SQLiteParameter("@b", backpack));
                update.ExecuteNonQuery();
            }
        });
    }

    private void Insert(string sql, (string name, object value)? arg = null)
    {
        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            if (arg is not null)
                cmd.Parameters.Add(new SQLiteParameter(arg.Value.name, arg.Value.value));
            cmd.ExecuteNonQuery();
        });
    }

    private static string SlotJson(int templateId, int? backpackSlots = null)
    {
        var item = new Item { TemplateID = templateId, BaseStats = new AttributeSet() };
        if (backpackSlots is not null)
            item.ItemProperties[ItemProperty.BackpackSlots] = backpackSlots.Value;
        return JsonHelper.Serialize(new ItemSlot { Item = item, Stack = 1 });
    }

    private static string BackpackJson(params (int index, string json)[] slots)
    {
        var entries = new string[16];
        foreach (var (index, json) in slots)
            entries[index] = json;
        return "[" + string.Join(",", entries.Select(e => e ?? "null")) + "]";
    }

    private static string FullArray(params (int index, string json)[] slots)
    {
        // EquippedSize is 20 in PlayerFirstSaveTestBase; the equipped array is size+1.
        var entries = new string[21];
        foreach (var (index, json) in slots)
            entries[index] = json;
        return "[" + string.Join(",", entries.Select(e => e ?? "null")) + "]";
    }

    private static string FifteenEntryArray()
    {
        return NullArray(15);
    }

    private static string NullArray(int length)
    {
        return "[" + string.Join(",", Enumerable.Repeat("null", length)) + "]";
    }
}
