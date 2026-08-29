using System.Data.SQLite;

namespace Goose.IntegrationTests;

public class PlayerLoadMissingRowTests : PlayerFirstSaveTestBase
{
    public PlayerLoadMissingRowTests() : base("players", "banks", "quests") { }

    [Fact]
    public void Loaders_NoRows_LeaveStateEmpty()
    {
        var player = MakePlayer();
        player.Inventory.Load(world);
        player.Spellbook.Load(world);
        player.LoadQuests(world);
        player.Bank.Load(world, player);

        for (int i = 1; i <= 30; i++) Assert.Null(player.Inventory.GetSlot(i));
        for (int i = 1; i <= 30; i++) Assert.Null(player.Spellbook.GetSlot(i));
        Assert.Empty(player.Bank.Containers);
    }

    [Fact]
    public void Inventory_Load_UnknownTemplateId_DiscardsSlot()
    {
        RegisterTemplate(5);
        var player = MakePlayer();
        Insert("INSERT INTO inventory (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((3, SlotJson(5)), (10, SlotJson(999)))));

        player.Inventory.Load(world);

        Assert.NotNull(player.Inventory.GetSlot(3));
        Assert.Equal(5, player.Inventory.GetSlot(3)!.Item.TemplateID);
        Assert.Null(player.Inventory.GetSlot(10));
    }

    [Fact]
    public void Inventory_Load_NullItemInSlot_DiscardsSlot()
    {
        var player = MakePlayer();
        Insert("INSERT INTO inventory (player_id, serialized_data) VALUES (1, @d)",
            ("@d", FullArray((5, "{\"Item\":null,\"Stack\":1}"))));

        player.Inventory.Load(world);

        for (int i = 1; i <= 30; i++) Assert.Null(player.Inventory.GetSlot(i));
    }

    [Fact]
    public void Inventory_Load_JsonNullBlob_StartsEmpty()
    {
        var player = MakePlayer();
        Insert("INSERT INTO inventory (player_id, serialized_data) VALUES (1, @d)",
            ("@d", "null"));

        player.Inventory.Load(world);

        for (int i = 1; i <= 30; i++) Assert.Null(player.Inventory.GetSlot(i));
    }

    [Fact]
    public void Inventory_Load_MalformedJson_StartsEmpty()
    {
        var player = MakePlayer();
        Insert("INSERT INTO inventory (player_id, serialized_data) VALUES (1, @d)",
            ("@d", "{not json"));

        player.Inventory.Load(world);

        for (int i = 1; i <= 30; i++) Assert.Null(player.Inventory.GetSlot(i));
    }

    [Fact]
    public void Inventory_Load_ShortArray_DoesNotThrowOnGetSlot()
    {
        RegisterTemplate(5);
        var player = MakePlayer();
        Insert("INSERT INTO inventory (player_id, serialized_data) VALUES (1, @d)",
            ("@d", "[null," + SlotJson(5) + ",null]"));

        player.Inventory.Load(world);

        Assert.NotNull(player.Inventory.GetSlot(1));
        Assert.Equal(5, player.Inventory.GetSlot(1)!.Item.TemplateID);
        Assert.Null(player.Inventory.GetSlot(30));
    }

    [Fact]
    public void Spellbook_Load_ShortArray_LeavesRemainingSlotsNull()
    {
        var player = MakePlayer();
        Insert("INSERT INTO spellbook (player_id, serialized_data) VALUES (1, @d)",
            ("@d", "[0,0]"));

        player.Spellbook.Load(world);

        for (int i = 1; i <= 30; i++) Assert.Null(player.Spellbook.GetSlot(i));
    }

    [Fact]
    public void Bank_Load_EmptyStringCell_SkipsRow()
    {
        var player = MakePlayer();
        Insert("INSERT INTO bank_items (npc_id, player_id, serialized_data) VALUES (7, 1, '')");

        player.Bank.Load(world, player);

        Assert.DoesNotContain(player.Bank.Containers[7], s => s is not null);
    }

    private void RegisterTemplate(int id)
    {
        world.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = id,
            Name = "Sword",
            BaseStats = new AttributeSet(),
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

    private static string SlotJson(int templateId) =>
        JsonHelper.Serialize(new ItemSlot
        {
            Item = new Item { TemplateID = templateId, BaseStats = new AttributeSet() },
            Stack = 1,
        });

    private static string FullArray(params (int index, string json)[] slots)
    {
        var entries = new string[31];
        foreach (var (index, json) in slots)
            entries[index] = json;
        return "[" + string.Join(",", entries.Select(e => e ?? "null")) + "]";
    }
}
