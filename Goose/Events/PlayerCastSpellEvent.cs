using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * PlayerCastSpellEvent, event for "CAST" packet
     * 
     * Syntax: CASTspellbookid,targetid
     * 
     */
    public class PlayerCastSpellEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PlayerCastSpellEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                string packet = ((string)this.Data).Substring(4);
                string[] t = packet.Split(",".ToCharArray());

                int spellid = 0;
                int target = 0;

                if (t.Length == 2)
                {
                    try
                    {
                        spellid = Convert.ToInt32(t[0]);
                        target = Convert.ToInt32(t[1]);
                    }
                    catch (Exception)
                    {
                        spellid = 0;
                        target = 0;
                    }

                    if (spellid == 0 || target == 0) return;

                    if (spellid >= 1 && spellid <= GameSettings.Default.SpellbookSize)
                    {
                        if (this.Player.LoginID == target)
                        {
                            this.Player.CastSpell(spellid, this.Player, world);
                            return;
                        }
                        else
                        {
                            foreach (Player player in this.Player.Map.GetPlayersInRange(this.Player))
                            {
                                if (player.LoginID == target)
                                {
                                    this.Player.CastSpell(spellid, player, world);
                                    return;
                                }
                            }

                            foreach (NPC npc in this.Player.Map.GetNPCsInRange(this.Player))
                            {
                                if (npc.LoginID == target)
                                {
                                    this.Player.CastSpell(spellid, npc, world);
                                    return;
                                }
                            }
                        }

                        this.Player.SuspectedMacroCount++;
                        if (this.Player.SuspectedMacroCount > 10000)
                        {
                            world.LostConnection(this.Player.Sock);
                        }
                        Console.WriteLine("SUSPECTED MACRO: " + this.Player.Name);
                    }
                }
            }
        }
    }
}
