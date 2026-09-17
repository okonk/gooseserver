using Goose;
using Goose.Testing;
using System.Data.SQLite;

namespace Goose.Tests;

public class CombinationHandlerTests
{
    private sealed record Recipe(int Id, string Name, int[] Required, int[] Results);

    private sealed class HandlerFixture : IDisposable
    {
        private readonly TestWorldFixture fixture = new();

        public HandlerFixture(params Recipe[] recipes)
        {
            foreach (var recipe in recipes)
                foreach (var id in recipe.Required.Concat(recipe.Results).Distinct())
                    fixture.AddBaseItemTemplate(id, "Item " + id, ItemTemplate.UseTypes.NoUse);

            fixture.World.Database.Start(Path.Combine(fixture.DataDirectory, "test.db"));
            fixture.World.Database.Execute(conn =>
            {
                Run(conn, SchemaDdl.ForAll("combinations", "combination_item_required",
                                           "combination_item_results"));

                foreach (var recipe in recipes)
                {
                    Run(conn, $"INSERT INTO combinations (combination_id, combination_name) VALUES ({recipe.Id}, '{recipe.Name}')");
                    foreach (var id in recipe.Required)
                        Run(conn, $"INSERT INTO combination_item_required (combination_id, item_template_id) VALUES ({recipe.Id}, {id})");
                    foreach (var id in recipe.Results)
                        Run(conn, $"INSERT INTO combination_item_results (combination_id, item_template_id) VALUES ({recipe.Id}, {id})");
                }
            });

            fixture.World.CombinationHandler.LoadCombinations(fixture.World);
        }

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
}
