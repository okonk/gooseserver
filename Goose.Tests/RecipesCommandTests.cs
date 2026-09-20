using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class RecipesCommandTests
{
    private sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new();
        public override bool Send(string data) { Sent.Add(data); return true; }
    }

    private static Combination MakeCombination(TestWorldFixture world, int id, string name,
        (int Id, string Name, int Count)[] required, (int Id, string Name)[] results)
    {
        var combination = new Combination
        {
            ID = id,
            Name = name,
            RequiredHash = [],
            ResultItems = [],
        };
        foreach (var (itemId, itemName, count) in required)
        {
            combination.RequiredHash[itemId] = count;
            combination.RequiredTotal += count;
            world.AddBaseItemTemplate(itemId, itemName, ItemTemplate.UseTypes.NoUse);
        }
        foreach (var (itemId, itemName) in results)
        {
            combination.ResultItems.Add(world.AddBaseItemTemplate(itemId, itemName, ItemTemplate.UseTypes.NoUse));
        }
        world.World.CombinationHandler.Add(combination);
        return combination;
    }

    private static (TestWorldFixture World, CapturingPlayer Player, CommandContext Ctx) Setup()
    {
        var world = new TestWorldFixture();
        var player = new CapturingPlayer
        {
            Class = world.World.ClassHandler.GetClass(0)!,
        };
        var ctx = new CommandContext(player, world.World, new CommandRegistry(), [], "");
        return (world, player, ctx);
    }

    [Fact]
    public void Execute_NoCombinations_SendsMessage()
    {
        var (_, player, ctx) = Setup();

        new RecipesCommand().Execute(ctx);

        Assert.Empty(player.Windows);
        Assert.Contains(player.Sent, s => s.Contains("There are no recipes."));
    }

    [Fact]
    public void Execute_ListsCombinationOutputs()
    {
        var (world, player, ctx) = Setup();
        MakeCombination(world, 1, "Cloth",
            [(1, "Thread", 1), (2, "Needle", 1)], [(10, "Cloth")]);

        new RecipesCommand().Execute(ctx);

        Assert.Single(player.Windows);
        var list = Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Null(list.NPC);
        Assert.Equal(["WNF1001,1,Cloth|0|0|0|0|*"],
            player.Sent.Where(s => s.StartsWith("WNF")).ToArray());
    }

    [Fact]
    public void Execute_SendsOutputItemIconOnEachLine()
    {
        var (world, player, ctx) = Setup();
        var result = world.AddBaseItemTemplate(10, "Cloth", ItemTemplate.UseTypes.NoUse,
            t => { t.GraphicFile = 5; t.GraphicTile = 7; });
        world.World.CombinationHandler.Add(new Combination
        {
            ID = 1,
            Name = "Cloth",
            RequiredHash = [],
            ResultItems = [result],
        });

        new RecipesCommand().Execute(ctx);

        Assert.Equal(["WNF1001,1,Cloth|0|0|5|7|*"],
            player.Sent.Where(s => s.StartsWith("WNF")).ToArray());
    }

    [Fact]
    public void Execute_MultipleResultItems_AreJoinedOnOneLine()
    {
        var (world, player, ctx) = Setup();
        MakeCombination(world, 1, "Bundle",
            [(1, "Thread", 1)], [(10, "Cloth"), (11, "Rope")]);

        new RecipesCommand().Execute(ctx);

        Assert.Equal(["WNF1001,1,Cloth, Rope|0|0|0|0|*"],
            player.Sent.Where(s => s.StartsWith("WNF")).ToArray());
    }

    [Fact]
    public void LineClick_OpensQuestInfoWindowWithRequiredItemsAndCounts()
    {
        var (world, player, ctx) = Setup();
        MakeCombination(world, 1, "Cat Ears",
            [(1, "Thread", 2), (2, "Needle", 1), (3, "Cat Hair", 3)], [(10, "Cat Ears")]);

        new RecipesCommand().Execute(ctx);
        player.Windows[0].LineClicked(0, 0, player, ctx.World);

        Assert.Single(player.Windows);
        var info = Assert.IsType<RecipeWindow>(player.Windows[0]);

        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Contains(lines, s => s.Contains("Place the following items in a combine bag:"));
        Assert.Contains(lines, s => s.Contains("Thread (2)"));
        Assert.Contains(lines, s => s.Contains("Needle (1)"));
        Assert.Contains(lines, s => s.Contains("Cat Hair (3)"));
        Assert.Equal("0,1,1,0,0", info.Buttons);
    }

    [Fact]
    public void BackButton_ReopensListOnSamePage()
    {
        var (world, player, ctx) = Setup();
        for (var i = 1; i <= 12; i++)
            MakeCombination(world, i, $"Recipe {i}",
                [(1, "Thread", 1)], [(10 + i, $"Recipe {i}")]);

        new RecipesCommand().Execute(ctx);
        player.Windows[0].Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, ctx.World);
        player.Windows[0].LineClicked(0, 0, player, ctx.World);

        var recipe = Assert.IsType<RecipeWindow>(player.Windows[0]);
        Assert.Equal("0,1,1,0,0", recipe.Buttons);

        player.Sent.Clear();
        recipe.Clicked(Window.ButtonTypes.Back, 0, 0, 0, player, ctx.World);

        Assert.Contains(player.Sent, s => s.StartsWith($"CLW{recipe.ID}"));
        Assert.Single(player.Windows);
        var list = Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Equal(1, list.Page);
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Contains(lines, s => s.Contains("Recipe 11"));
        Assert.Contains(lines, s => s.Contains("Recipe 12"));
    }

    [Fact]
    public void Execute_MoreThanTenCombinations_PagesAndOpensCorrectRecipe()
    {
        var (world, player, ctx) = Setup();
        for (var i = 1; i <= 12; i++)
            MakeCombination(world, i, $"Recipe {i}",
                [(1, "Thread", 1)], [(10 + i, $"Recipe {i}")]);

        new RecipesCommand().Execute(ctx);
        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(10, lines.Length);

        player.Sent.Clear();
        player.Windows[0].Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, ctx.World);
        lines = player.Sent.Where(s => s.StartsWith("WNF")).ToArray();
        Assert.Equal(2, lines.Length);

        player.Windows[0].LineClicked(1, 0, player, ctx.World);

        Assert.Single(player.Windows);
        var info = Assert.IsType<RecipeWindow>(player.Windows[0]);
        Assert.Equal("Recipe 12", info.Title);
    }

    [Fact]
    public void CloseButton_ClosesRecipeWindow()
    {
        var (world, player, ctx) = Setup();
        MakeCombination(world, 1, "Cloth",
            [(1, "Thread", 1)], [(10, "Cloth")]);

        new RecipesCommand().Execute(ctx);
        player.Windows[0].LineClicked(0, 0, player, ctx.World);

        player.Windows[0].Clicked(Window.ButtonTypes.Close, 0, 0, 0, player, ctx.World);
        Assert.Empty(player.Windows);
    }

    [Fact]
    public void Execute_RepeatedRun_ClosesPreviousListAndRecipeWindows()
    {
        var (world, player, ctx) = Setup();
        MakeCombination(world, 1, "Cloth",
            [(1, "Thread", 1)], [(10, "Cloth")]);

        new RecipesCommand().Execute(ctx);
        player.Windows[0].LineClicked(0, 0, player, ctx.World);
        new RecipesCommand().Execute(ctx);

        Assert.Single(player.Windows);
        Assert.IsType<OptionListWindow>(player.Windows[0]);
        Assert.Contains(player.Sent, s => s.StartsWith("CLW1001"));
    }
}
