using Goose;
using Goose.Testing;
using System.Data.SQLite;

namespace Goose.Tests;

public class CombinationHandlerTests
{
    private sealed record Recipe(int Id, string Name, int[] Required, int[] Results, int[]? Quantities = null);

    private sealed class HandlerFixture : IDisposable
    {
        private readonly TestWorldFixture fixture = new();

        public HandlerFixture(params Recipe[] recipes) : this(0, recipes) { }

        public HandlerFixture(int stackSize, params Recipe[] recipes)
        {
            foreach (var recipe in recipes)
                foreach (var id in recipe.Required.Concat(recipe.Results).Distinct())
                    fixture.AddBaseItemTemplate(id, "Item " + id, ItemTemplate.UseTypes.NoUse, t => t.StackSize = stackSize);

            fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            fixture.World.Database.Execute(conn =>
            {
                Run(conn, SchemaDdl.ForAll("combinations", "combination_item_required",
                                           "combination_item_results"));

                foreach (var recipe in recipes)
                {
                    Run(conn, $"INSERT INTO combinations (combination_id, combination_name) VALUES ({recipe.Id}, '{recipe.Name}')");
                    for (int i = 0; i < recipe.Required.Length; i++)
                    {
                        Run(conn, recipe.Quantities is null
                            ? $"INSERT INTO combination_item_required (combination_id, item_template_id) VALUES ({recipe.Id}, {recipe.Required[i]})"
                            : $"INSERT INTO combination_item_required (combination_id, item_template_id, quantity) VALUES ({recipe.Id}, {recipe.Required[i]}, {recipe.Quantities[i]})");
                    }
                    foreach (var id in recipe.Results)
                        Run(conn, $"INSERT INTO combination_item_results (combination_id, item_template_id) VALUES ({recipe.Id}, {id})");
                }
            });

            try
            {
                fixture.World.CombinationHandler.LoadCombinations(fixture.World);
            }
            catch
            {
                fixture.Dispose();
                throw;
            }
        }

        public Combination Get(int id) => fixture.World.CombinationHandler.GetAll().Single(c => c.ID == id);

        public Combination? Match(params (int TemplateId, long Count)[] bag)
        {
            Dictionary<int, long> combine = [];
            foreach (var (templateId, count) in bag) combine[templateId] = count;

            return fixture.World.CombinationHandler.GetMatch(combine);
        }

        public void Dispose() => fixture.Dispose();

        private static void Run(SQLiteConnection conn, string sql)
        {
            using var command = conn.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }

    private static Recipe Cloth => new(1, "Cloth", [340, 351], [449]);
    private static Recipe Shirt => new(7, "Cloth Shirt", [340, 347, 346, 449], [450]);
    private static Recipe CatEars => new(10, "Cat Ears", [340, 351, 355, 455], [453]);

    [Fact]
    public void A_recipe_is_never_shadowed_by_a_shorter_recipe_it_contains()
    {
        using var handler = new HandlerFixture(Cloth, CatEars);

        var match = handler.Match((340, 1), (351, 1), (355, 1), (455, 1));

        Assert.NotNull(match);
        Assert.Equal("Cat Ears", match.Name);
    }

    [Fact]
    public void The_shorter_recipe_still_matches_without_the_extra_ingredients()
    {
        using var handler = new HandlerFixture(Cloth, CatEars);

        var match = handler.Match((340, 1), (351, 1));

        Assert.NotNull(match);
        Assert.Equal("Cloth", match.Name);
    }

    [Fact]
    public void The_longer_recipe_wins_when_it_loads_first()
    {
        using var handler = new HandlerFixture(new Recipe(10, "Cat Ears", [340, 351, 355, 455], [453]), Cloth);

        var match = handler.Match((340, 1), (351, 1), (355, 1), (455, 1));

        Assert.NotNull(match);
        Assert.Equal("Cat Ears", match.Name);
    }

    [Fact]
    public void A_larger_recipe_wins_even_when_it_also_contains_a_shorter_one()
    {
        using var handler = new HandlerFixture(Cloth, Shirt);

        var match = handler.Match((340, 1), (351, 1), (347, 1), (346, 1), (449, 1));

        Assert.NotNull(match);
        Assert.Equal("Cloth Shirt", match.Name);
    }

    [Fact]
    public void Ingredients_beyond_the_recipe_do_not_stop_it_matching()
    {
        using var handler = new HandlerFixture(Cloth, CatEars);

        var match = handler.Match((340, 1), (351, 1), (999, 5));

        Assert.NotNull(match);
        Assert.Equal("Cloth", match.Name);
    }

    [Fact]
    public void A_recipe_is_never_shadowed_by_a_surplus_quantity_of_a_shorter_recipe()
    {
        using var handler = new HandlerFixture(Cloth, CatEars);

        var match = handler.Match((340, 2), (351, 2), (355, 1), (455, 1));

        Assert.NotNull(match);
        Assert.Equal("Cat Ears", match.Name);
    }

    [Fact]
    public void Fewer_ingredients_than_the_recipe_needs_does_not_match()
    {
        using var handler = new HandlerFixture(new Recipe(37, "Pearl Bracelet", [343, 343, 343, 343, 343, 343, 343], [347]));

        Assert.Null(handler.Match((343, 6)));
    }

    [Fact]
    public void Enough_ingredients_matches_even_when_they_share_a_slot()
    {
        using var handler = new HandlerFixture(new Recipe(37, "Pearl Bracelet", [343, 343, 343, 343, 343, 343, 343], [347]));

        var match = handler.Match((343, 7));

        Assert.NotNull(match);
        Assert.Equal("Pearl Bracelet", match.Name);
    }

    [Fact]
    public void Ingredient_count_totals_across_slots()
    {
        using var handler = new HandlerFixture(new Recipe(37, "Pearl Bracelet", [343, 343, 343], [347]));

        Assert.Null(handler.Match((343, 2)));
        Assert.NotNull(handler.Match((343, 3)));
    }

    [Fact]
    public void An_empty_bag_matches_nothing()
    {
        using var handler = new HandlerFixture(Cloth, CatEars);

        Assert.Null(handler.Match());
    }

    [Fact]
    public void Unknown_ingredients_alone_match_nothing()
    {
        using var handler = new HandlerFixture(Cloth, CatEars);

        Assert.Null(handler.Match((955, 3)));
    }

    [Fact]
    public void Same_size_recipes_matching_one_bag_resolve_to_the_lower_id()
    {
        using var handler = new HandlerFixture(
            new Recipe(7, "Higher", [340, 351], [449]),
            new Recipe(3, "Lower", [340, 351], [450]));

        var match = handler.Match((340, 1), (351, 1));

        Assert.NotNull(match);
        Assert.Equal("Lower", match.Name);
    }

    [Fact]
    public void A_quantity_requires_that_many_of_the_ingredient()
    {
        using var handler = new HandlerFixture(99, new Recipe(75, "Trappers Pack", [8, 455, 58], [772], [10, 10, 1]));

        Assert.Null(handler.Match((8, 9), (455, 10), (58, 1)));
        Assert.NotNull(handler.Match((8, 10), (455, 10), (58, 1)));
        Assert.Equal(21, handler.Get(75).RequiredTotal);
    }

    [Fact]
    public void Repeated_rows_add_their_quantities_together()
    {
        using var handler = new HandlerFixture(99, new Recipe(75, "Trappers Pack", [438, 438], [772], [1, 1]));

        Assert.Equal(2, handler.Get(75).RequiredHash[438]);
    }

    [Fact]
    public void A_recipe_larger_than_the_bag_loads_when_its_ingredients_stack()
    {
        int[] required = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        using var handler = new HandlerFixture(99, new Recipe(75, "Big", required, [772], required.Select(_ => 30).ToArray()));

        Assert.Equal(300, handler.Get(75).RequiredTotal);
    }

    [Fact]
    public void A_recipe_needing_more_slots_than_the_bag_holds_fails_to_load()
    {
        Assert.ThrowsAny<Exception>(() =>
            new HandlerFixture(1, new Recipe(75, "Too Big", [1, 2], [772], [6, 5])));
    }

    [Fact]
    public void A_quantity_of_zero_fails_to_load()
    {
        Assert.ThrowsAny<Exception>(() =>
            new HandlerFixture(99, new Recipe(75, "Broken", [1], [772], [0])));
    }
}
