using Goose;
using Goose.Testing;
using System.Data.SQLite;

namespace Goose.Tests;

public class CombineBagTests
{
    private const int Needle = 340;
    private const int BlackThread = 351;
    private const int CatHair = 355;
    private const int LeatherPadding = 455;
    private const int Cloth = 449;
    private const int CatEars = 453;
    private const int Elixir = 456;
    private const int ElixirStackSize = 5;
    private const int Shard = 457;
    private const int ShardStackSize = 3;

    private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player) WorldWithClothAndCatEars()
    {
        var fixture = new TestWorldFixture();
        SeedTemplates(fixture);

        fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
        fixture.World.Database.Execute(conn =>
        {
            Run(conn, SchemaDdl.ForAll("combinations", "combination_item_required",
                                       "combination_item_results"));

            InsertRecipe(conn, 1, "Cloth", [Needle, BlackThread], [Cloth]);
            InsertRecipe(conn, 10, "Cat Ears", [Needle, BlackThread, CatHair, LeatherPadding], [CatEars]);
        });
        fixture.World.CombinationHandler.LoadCombinations(fixture.World);

        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        player.Level = 50;

        return (fixture, player);
    }

    private static void SeedTemplates(TestWorldFixture fixture)
    {
        fixture.AddBaseItemTemplate(Needle, "Needle", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(BlackThread, "Spool of Black Thread", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(CatHair, "Cats Hair", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(LeatherPadding, "Leather Padding", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(Cloth, "Cloth", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(CatEars, "Cat Ears", ItemTemplate.UseTypes.Armor);
    }

    private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player) WorldWithStackableResult()
    {
        var fixture = new TestWorldFixture();
        fixture.AddBaseItemTemplate(Needle, "Needle", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(CatHair, "Cats Hair", ItemTemplate.UseTypes.NoUse);
        fixture.AddBaseItemTemplate(Elixir, "Elixir", ItemTemplate.UseTypes.OneTime,
                                    t => t.StackSize = ElixirStackSize);
        fixture.AddBaseItemTemplate(Shard, "Shard", ItemTemplate.UseTypes.NoUse,
                                    t => t.StackSize = ShardStackSize);

        fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
        fixture.World.Database.Execute(conn =>
        {
            Run(conn, SchemaDdl.ForAll("combinations", "combination_item_required",
                                       "combination_item_results"));

            InsertRecipe(conn, 20, "Elixir", [Needle], [Elixir]);
            // Two required items, so it wins GetMatch's most-specific tiebreak over Elixir
            // in the bags that hold a cat hair; five results exercise merge-then-spill.
            InsertRecipe(conn, 21, "Shards", [Needle, CatHair],
                         [Shard, Shard, Shard, Shard, Shard]);
        });
        fixture.World.CombinationHandler.LoadCombinations(fixture.World);

        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        player.Level = 50;

        return (fixture, player);
    }

    private static void PutInBag(TestWorldFixture fixture, Player player, params int[] templateIds)
    {
        var bag = player.Inventory.GetCombineBagContainer();
        for (int i = 0; i < templateIds.Length; i++)
            bag.SetSlot(i + 1, new ItemSlot { Item = LoadItem(fixture, templateIds[i]) });
    }

    private static Item LoadItem(TestWorldFixture fixture, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(templateId)!);
        return item;
    }

    private static ItemSlot StackOf(TestWorldFixture fixture, int templateId, long amount) =>
        new ItemSlot { Item = LoadItem(fixture, templateId), Stack = amount };

    private static void Combine(TestWorldFixture fixture, Player player)
    {
        player.Inventory.Combine(new CombineBagWindow(fixture.World, player), fixture.World);
    }

    private static string? NameInBag(Player player, int slot) =>
        player.Inventory.GetCombineBagContainer().GetSlot(slot)?.Item.Name;

    private static void Run(SQLiteConnection conn, string sql)
    {
        using var command = conn.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void InsertRecipe(SQLiteConnection conn, int id, string name, int[] required, int[] results)
    {
        Run(conn, $"INSERT INTO combinations (combination_id, combination_name) VALUES ({id}, '{name}')");
        foreach (var item in required)
            Run(conn, $"INSERT INTO combination_item_required (combination_id, item_template_id) VALUES ({id}, {item})");
        foreach (var item in results)
            Run(conn, $"INSERT INTO combination_item_results (combination_id, item_template_id) VALUES ({id}, {item})");
    }

    [Fact]
    public void Needle_and_thread_make_cloth()
    {
        var (fixture, player) = WorldWithClothAndCatEars();
        using (fixture)
        {
            PutInBag(fixture, player, Needle, BlackThread);

            Combine(fixture, player);

            Assert.Equal("Cloth", NameInBag(player, 1));
            Assert.Null(NameInBag(player, 2));
        }
    }

    [Fact]
    public void Cat_ear_ingredients_make_cat_ears_rather_than_cloth()
    {
        var (fixture, player) = WorldWithClothAndCatEars();
        using (fixture)
        {
            PutInBag(fixture, player, Needle, BlackThread, CatHair, LeatherPadding);

            Combine(fixture, player);

            Assert.Equal("Cat Ears", NameInBag(player, 1));
            Assert.Null(NameInBag(player, 2));
            Assert.Null(NameInBag(player, 3));
            Assert.Null(NameInBag(player, 4));
        }
    }

    [Fact]
    public void Cat_ear_ingredients_stacked_in_fewer_slots_make_cat_ears()
    {
        var (fixture, player) = WorldWithClothAndCatEars();
        using (fixture)
        {
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, new ItemSlot { Item = LoadItem(fixture, Needle), Stack = 2 });
            bag.SetSlot(2, new ItemSlot { Item = LoadItem(fixture, CatHair) });
            bag.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, BlackThread), Stack = 2 });
            bag.SetSlot(4, new ItemSlot { Item = LoadItem(fixture, LeatherPadding) });

            Combine(fixture, player);

            Assert.Equal("Needle", NameInBag(player, 1));
            Assert.Equal(1, bag.GetSlot(1)!.Stack);
            Assert.Equal("Cat Ears", NameInBag(player, 2));
            Assert.Equal("Spool of Black Thread", NameInBag(player, 3));
            Assert.Equal(1, bag.GetSlot(3)!.Stack);
            Assert.Null(NameInBag(player, 4));
        }
    }

    [Fact]
    public void Returned_surplus_goes_back_to_its_own_slot()
    {
        var (fixture, player) = WorldWithClothAndCatEars();
        using (fixture)
        {
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, new ItemSlot { Item = LoadItem(fixture, BlackThread) });
            bag.SetSlot(2, new ItemSlot { Item = LoadItem(fixture, Needle), Stack = 2 });

            Combine(fixture, player);

            Assert.Equal("Cloth", NameInBag(player, 1));
            Assert.Equal("Needle", NameInBag(player, 2));
            Assert.Equal(1, bag.GetSlot(2)!.Stack);
            Assert.Null(NameInBag(player, 3));
        }
    }

    [Fact]
    public void Short_ingredients_message_when_nothing_matches()
    {
        var (fixture, player) = WorldWithClothAndCatEars();
        using (fixture)
        {
            PutInBag(fixture, player, Needle);

            Combine(fixture, player);

            Assert.Equal("Needle", NameInBag(player, 1));
            Assert.Contains(player.Sent, s => s.Contains("Couldn't combine items."));
        }
    }

    [Fact]
    public void Result_stacks_onto_a_stack_already_in_the_bag()
    {
        var (fixture, player) = WorldWithStackableResult();
        using (fixture)
        {
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, StackOf(fixture, Elixir, 3));
            bag.SetSlot(2, new ItemSlot { Item = LoadItem(fixture, Needle) });

            Combine(fixture, player);

            Assert.Equal(4, bag.GetSlot(1)!.Stack);
            Assert.Null(NameInBag(player, 2));
        }
    }

    [Fact]
    public void Crafts_fill_a_stack_and_spill_into_a_free_slot_once_it_is_full()
    {
        var (fixture, player) = WorldWithStackableResult();
        using (fixture)
        {
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, StackOf(fixture, Elixir, 3));

            bag.SetSlot(2, new ItemSlot { Item = LoadItem(fixture, Needle) });
            Combine(fixture, player);
            bag.SetSlot(2, new ItemSlot { Item = LoadItem(fixture, Needle) });
            Combine(fixture, player);

            Assert.Equal(ElixirStackSize, bag.GetSlot(1)!.Stack);
            Assert.Null(NameInBag(player, 2));

            bag.SetSlot(2, new ItemSlot { Item = LoadItem(fixture, Needle) });
            Combine(fixture, player);

            Assert.Equal(ElixirStackSize, bag.GetSlot(1)!.Stack);
            Assert.Equal("Elixir", NameInBag(player, 2));
            Assert.Equal(1, bag.GetSlot(2)!.Stack);
        }
    }

    [Fact]
    public void Combine_refuses_when_the_result_has_no_room_and_no_free_slot()
    {
        var (fixture, player) = WorldWithStackableResult();
        using (fixture)
        {
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, StackOf(fixture, Needle, 2));
            for (int i = 2; i <= 10; i++)
                bag.SetSlot(i, StackOf(fixture, Elixir, ElixirStackSize));

            Combine(fixture, player);

            Assert.Contains(player.Sent, s => s.Contains("Not enough free slots to create Elixir."));
            Assert.DoesNotContain(player.Sent, s => s.Contains("Successfully created"));
            Assert.Equal("Needle", NameInBag(player, 1));
            Assert.Equal(2, bag.GetSlot(1)!.Stack);
            Assert.Equal(ElixirStackSize, bag.GetSlot(2)!.Stack);
        }
    }

    [Fact]
    public void A_batch_that_stages_some_results_and_then_runs_out_of_room_places_none_of_them()
    {
        var (fixture, player) = WorldWithStackableResult();
        using (fixture)
        {
            var bag = player.Inventory.GetCombineBagContainer();
            bag.SetSlot(1, StackOf(fixture, Shard, ShardStackSize - 1));
            bag.SetSlot(2, StackOf(fixture, Needle, 2));
            bag.SetSlot(3, new ItemSlot { Item = LoadItem(fixture, CatHair) });
            for (int i = 4; i <= 10; i++)
                bag.SetSlot(i, StackOf(fixture, Elixir, ElixirStackSize));

            int registered = fixture.World.ItemHandler.GetItems().Count();

            Combine(fixture, player);

            Assert.Contains(player.Sent, s => s.Contains("Not enough free slots to create Shards."));
            Assert.DoesNotContain(player.Sent, s => s.Contains("Successfully created"));
            Assert.Equal("Shard", NameInBag(player, 1));
            Assert.Equal(ShardStackSize - 1, bag.GetSlot(1)!.Stack);
            Assert.Equal("Needle", NameInBag(player, 2));
            Assert.Equal(2, bag.GetSlot(2)!.Stack);
            Assert.Equal("Cats Hair", NameInBag(player, 3));
            Assert.Equal(ElixirStackSize, bag.GetSlot(4)!.Stack);
            Assert.Equal(registered, fixture.World.ItemHandler.GetItems().Count());
        }
    }
}
