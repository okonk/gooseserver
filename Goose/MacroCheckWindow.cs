using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public class MacroCheckWindow : Window
    {
        public override string Title
        {
            get { return "Macro Check"; }
        }

        public override string Buttons
        {
            get { return "0,0,0,0,0"; }
        }

        private string code;

        public MacroCheckWindow(GameWorld world, Player player, string code)
        {
            this.ID = ++player.LastWindowID;
            this.Frame = WindowFrames.GenericInfo;
            this.Type = WindowTypes.MacroCheck;
            this.code = code;

            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player, string code)
        {
            var macroCheck = player.Windows.FirstOrDefault(w => w.Type == WindowTypes.MacroCheck);
            if (macroCheck != null)
            {
                player.Windows.Remove(macroCheck);
            }

            player.Windows.Add(new MacroCheckWindow(world, player, code));
        }

        public override void Populate(Player player, GameWorld world)
        {
            int lineno = 1;
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "You have been suspected of macroing."));
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "To pass this check type:"));
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "/mc " + code));
            lineno++;
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "If you pass you will gain 1mil xp."));
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "If you don't you will be kicked."));
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "Multiple failures will be a ban."));
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "You have 10 minutes."));
            world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineno++, "You have been warned. :)"));
        }

        public override void Refresh(Player player, GameWorld world)
        {
            this.Populate(player, world);
        }
    }
}
