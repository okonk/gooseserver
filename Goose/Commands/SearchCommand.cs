using System.Text.RegularExpressions;

namespace Goose.Commands
{
    [Command("/search ", AccessPrivilege.Search, Section = "GM", Help = "Search item and NPC templates.")]
    public sealed class SearchCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string command, string name, string[] query)
        {
            var world = ctx.World;

            try
            {
                var regex = new Regex(string.Join(" ", [name, .. query]), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

                switch (command.ToLowerInvariant())
                {
                    case "item":
                        {
                            var templates = world.ItemHandler.GetTemplates();
                            var matched = templates.Where(i => regex.IsMatch(i.Name)).OrderBy(i => i.ID).ToArray();

                            foreach (var item in matched)
                            {
                                ctx.Send($"{item.ID} - {item.Name}");
                            }

                            ctx.Send($"[Matched {matched.Length} items]");

                            break;
                        }
                    case "npc":
                        {
                            var templates = world.NPCHandler.GetTemplates();
                            var matched = templates.Where(n => regex.IsMatch(n.Name)).OrderBy(i => i.NPCTemplateID).ToArray();

                            foreach (var npc in matched)
                            {
                                ctx.Send($"{npc.NPCTemplateID} - {npc.Name}");
                            }

                            ctx.Send($"[Matched {matched.Length} npcs]");
                            break;
                        }
                }
            }
            catch
            {
                ctx.Send("Invalid search pattern or arguments");
            }
        }
    }
}
