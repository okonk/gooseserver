using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Events
{
    public class ReloadSqlCommandEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static Event Create(Player player, Object data)
        {
            Event e = new ReloadSqlCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && 
                this.Player.HasPrivilege(AccessPrivilege.ReloadSQL))
            {
                Task.Run(() =>
                {
                    try
                    {
                        world.SpellHandler.LoadSpellEffects(world);
                        world.SpellHandler.LoadSpells(world);
                        world.ItemHandler.LoadTemplates(world);
                        world.ItemHandler.RefreshItemStats(world);
                        world.QuestHandler.LoadQuests(world);
                        //world.MapHandler.LoadMaps(world);
                        //world.ClassHandler.LoadClasses(world);
                        world.NPCHandler.LoadNPCTemplates(world);
                        //world.NPCHandler.LoadNPCs(world);
                        //world.CombinationHandler.LoadCombinations(world);

                        // TODO: Not safe to call Send from multiple threads
                        //world.Send(this.Player, "$7Reloaded sql data.");
                        log.Info("Reloaded sql data");
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "Failed reloading sql data");
                        //world.Send(this.Player, "$7" + e.Message);
                    }
                });
            }
        }
    }
}