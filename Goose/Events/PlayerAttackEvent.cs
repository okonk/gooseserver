using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * PlayerAttackEvent, event for "ATT" packet
     * 
     * Packet syntax: ATT
     * 
     */
    class PlayerAttackEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PlayerAttackEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                foreach (Buff b in this.Player.Buffs)
                {
                    // can't attack when stunned
                    if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                    {
                        world.Send(this.Player, "BT" + this.Player.LoginID + ",50");
                        return;
                    }
                }
                foreach (Window window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.Vendor)
                    {
                        world.Send(this.Player, "$7You can't attack while with a vendor.");
                        return;
                    }
                }

                if (this.Player.IsMounted())
                    return;

                long delay = (long)(((decimal)(this.Player.WeaponDelay / 10.0) * (1 - this.Player.MaxStats.Haste)) * 
                    world.TimerFrequency);
                long now = world.TimeNow;

                if (now - this.Player.LastAttack >= delay)
                {
                    var weaponSlot = this.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
                    if (weaponSlot?.Item != null)
                    {
                        try
                        {
                            weaponSlot.Item.Script?.Object.OnMeleeEvent(this.Player, weaponSlot.Item, world);
                        }
                        catch (Exception e) { }
                    }

                    List<Player> range = this.Player.Map.GetPlayersInRange(this.Player);
                    string packet = "ATT" + this.Player.LoginID;
                    foreach (Player player in range)
                    {
                        world.Send(player, packet);
                    }

                    this.Player.LastAttack = now;

                    int x = this.Player.MapX;
                    int y = this.Player.MapY;

                    // check tile 1 square in front of player
                    switch (this.Player.Facing)
                    {
                        case 1: y--; break;
                        case 2: x++; break;
                        case 3: y++; break;
                        case 4: x--; break;
                    }

                    ICharacter character = this.Player.Map.GetCharacterAt(x, y);
                    if (character != null) this.Player.Attack(character, world);
                }
            }
        }
    }
}
