using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Goose.Events
{
    class GMSearchCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMSearchCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;
            if (this.Player.Access != Player.AccessStatus.GameMaster) return;

            string[] tokens = ((string)this.Data).Split(" ".ToCharArray(), 3);
            string command, name;
            if (tokens.Length < 3)
            {
                world.Send(this.Player, "$7/search [item|npc] name");
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
                            world.Send(this.Player, string.Format("$7{0} - {1}", item.ID, item.Name));
                        }

                        world.Send(this.Player, string.Format("$7[Matched {0} items]", matched.Length));

                        break;
                    }
                case "npc":
                    {
                        var templates = world.NPCHandler.GetTemplates();
                        var matched = templates.Where(n => regex.IsMatch(n.Name)).OrderBy(i => i.NPCTemplateID).ToArray();

                        foreach (var npc in matched)
                        {
                            world.Send(this.Player, string.Format("$7{0} - {1}", npc.NPCTemplateID, npc.Name));
                        }

                        world.Send(this.Player, string.Format("$7[Matched {0} npcs]", matched.Length));
                        break;
                    }
            }
        }
    }
}
