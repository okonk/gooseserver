using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class ClassChangeCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ClassChangeCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.ClassChange))
            {
                string packet = (string)this.Data;
                string[] tokens = packet.Split(" ".ToCharArray());
                if (tokens.Length < 4) return;

                string name = tokens[1];
                string cl = tokens[2];
                decimal modifier = 1;

                try
                {
                    modifier = Convert.ToDecimal(tokens[3]);
                }
                catch (Exception)
                {
                    modifier = 1;
                }

                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player == null)
                {
                    world.Send(this.Player, P.ServerMessage("Player " + name + " doesn't exist."));
                    return;
                }

                switch (cl.ToLower())
                {
                    case "rogue":
                        player.ClassID = 2;
                        break;
                    case "warrior":
                        player.ClassID = 3;
                        break;
                    case "magus":
                        player.ClassID = 4;
                        break;
                    case "priest":
                        player.ClassID = 5;
                        break;
                    default:
                        world.Send(this.Player, P.ServerMessage("Invalid class name."));
                        return;
                }

                player.RemoveStats(player.BaseStats, world);

                player.MaxStats -= player.Class.GetLevel(player.Level).BaseStats;
                player.Experience += player.ExperienceSold;
                player.Experience = (long)(player.Experience * modifier);
                player.ExperienceSold = 0;
                player.BaseStats.HP = 0;
                player.BaseStats.MP = 0;

                player.AddStats(player.BaseStats, world);

                player.Class = world.ClassHandler.GetClass(player.ClassID);

                player.AddStats(player.Class.GetLevel(player.Level).BaseStats, world);

                world.Send(player, P.StatusInfo(player));
                world.Send(player, P.ExpBar(player));

                player.Spellbook.RemoveNonClassSpells(world);

                for (int level = 1; level <= player.Level; level++)
                {
                    if (level > player.Class.MaxLevel) break;

                    foreach (Spell spell in player.Class.GetLevel(level).Spells)
                    {
                        player.LearnSpell(spell.ID, world);
                    }
                }

                world.Send(this.Player, P.ServerMessage("Changed class successfully."));

                if (player.State != Goose.Player.States.NotLoggedIn)
                {
                    world.Send(player, P.StatusInfo(player));
                }
                else
                {
                    player.SaveToDatabase(world);
                }

                world.LogHandler.Log(Log.Types.ClassChange,
                    this.Player.PlayerID, player.PlayerID + " " + cl + " " + modifier,
                    0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
