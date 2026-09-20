namespace Goose.Commands
{
    [Command("/recipes", Section = "General", Help = "List all combination recipes.")]
    public sealed class RecipesCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var player = ctx.Player;
            var world = ctx.World;

            var combinations = world.CombinationHandler.GetAll()
                .OrderBy(c => c.ID)
                .ToList();

            if (combinations.Count == 0)
            {
                ctx.Send("There are no recipes.");
                return;
            }

            foreach (var w in player.Windows.Where(w => w is RecipeWindow
                || (w.Type == Window.WindowTypes.OptionList && w.NPC is null)).ToList())
                w.Close(player, world);

            var lines = combinations
                .Select(c => string.Join(", ", c.ResultItems.Select(i => i.Name)))
                .ToList();

            var lineGraphics = combinations
                .Select(c =>
                {
                    var result = c.ResultItems.FirstOrDefault();
                    return (
                        Sheet: result?.GraphicFile ?? 0,
                        Graphic: result?.GraphicTile ?? 0,
                        R: result?.GraphicR ?? 0,
                        G: result?.GraphicG ?? 0,
                        B: result?.GraphicB ?? 0,
                        A: result?.GraphicA ?? 0);
                })
                .ToList();

            OptionListWindow? list = null;

            void OpenList(Player p, GameWorld w, int page)
            {
                list = new OptionListWindow(p, w, "Recipes", lines,
                    (line, pp, ww) => new RecipeWindow(combinations[line], pp, ww,
                        (bp, bw) => OpenList(bp, bw, list!.Page)), null, page, lineGraphics);
            }

            OpenList(player, world, 0);
        }
    }
}
