using System.Text;
using System.Text.RegularExpressions;

namespace Goose.Events
{
    class SearchCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;

            try
            {
                string[] tokens = ((string)this.Data).Split(" ".ToCharArray(), 3);
                string command, name;
                if (tokens.Length < 3)
                {
                    world.Send(this.Player, P.ServerMessage("/search [item|npc] name"));
                    return;
                }
                else
                {
                    command = tokens[1];
                    name = tokens[2];
                }

                var regex = new Regex(name, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

                switch (command.ToLowerInvariant())
                {
                    case "item":
                        {
                            var templates = world.ItemHandler.GetTemplates();
                            var matched = templates.Where(i => regex.IsMatch(i.Name)).OrderBy(i => i.ID).ToArray();

                            foreach (var item in matched)
                            {
                                world.Send(this.Player, P.ServerMessage($"{item.ID} - {item.Name}"));
                            }

                            world.Send(this.Player, P.ServerMessage($"[Matched {matched.Length} items]"));

                            break;
                        }
                    case "npc":
                        {
                            var templates = world.NPCHandler.GetTemplates();
                            var matched = templates.Where(n => regex.IsMatch(n.Name)).OrderBy(i => i.NPCTemplateID).ToArray();

                            foreach (var npc in matched)
                            {
                                world.Send(this.Player, P.ServerMessage($"{npc.NPCTemplateID} - {npc.Name}"));
                            }

                            world.Send(this.Player, P.ServerMessage($"[Matched {matched.Length} npcs]"));
                            break;
                        }
                }
            }
            catch
            {
                world.Send(this.Player, P.ServerMessage("Invalid search pattern or arguments"));
            }
        }
    }
}
