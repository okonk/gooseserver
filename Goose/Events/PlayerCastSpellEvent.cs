using System.Text;

namespace Goose.Events
{
    /**
     * PlayerCastSpellEvent, event for "CAST" packet
     *
     * Syntax: CASTspellbookid,targetid or CASTspellbookid,0,x,y
     *
     */
    public class PlayerCastSpellEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            if (this.Player.RefuseWhileDead(world)) return;
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                if (this.Player.IsMounted(world))
                    return;

                string packet = ((string)this.Data).Substring(4);
                string[] t = packet.Split(',');

                if (t.Length == 4)
                {
                    if (int.TryParse(t[0], out int slot) && t[1] == "0" &&
                        int.TryParse(t[2], out int x) && int.TryParse(t[3], out int y) &&
                        slot >= 1 && slot <= world.Settings.SpellbookSize &&
                        this.Player.CastSpellAt(slot, x, y, world))
                        return;

                    this.SuspectMacro(world);
                    return;
                }

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

                    if (spellid >= 1 && spellid <= world.Settings.SpellbookSize)
                    {
                        if (this.Player.LoginID == target)
                        {
                            this.Player.CastSpell(spellid, this.Player, world);
                            return;
                        }
                        else
                        {
                            foreach (var player in this.Player.Map.GetPlayersInRange(this.Player))
                            {
                                if (player.LoginID == target)
                                {
                                    this.Player.CastSpell(spellid, player, world);
                                    return;
                                }
                            }

                            foreach (var npc in this.Player.Map.GetNPCsInRange(this.Player))
                            {
                                if (npc.LoginID == target)
                                {
                                    this.Player.CastSpell(spellid, npc, world);
                                    return;
                                }
                            }
                        }

                        this.SuspectMacro(world);
                    }
                }
            }
        }

        private void SuspectMacro(GameWorld world)
        {
            this.Player.SuspectedMacroCount++;
            if (this.Player.SuspectedMacroCount > 10000)
            {
                world.LostConnection(this.Player.Sock);
            }

            if (this.Player.SuspectedMacroCount > 100)
                log.Info("SUSPECTED MACRO: " + this.Player.Name);
        }
    }
}
