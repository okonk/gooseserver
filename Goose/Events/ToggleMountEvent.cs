namespace Goose.Events
{
    public class ToggleMountEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready) return;

            if ((int)Inventory.EquipSlots.Mount > world.Settings.EquippedSize) return;

            ItemSlot? mountSlot = this.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount);
            if (mountSlot is null) return;

            if (!this.Player.Mounted && !this.Player.Map.CanUseItems)
            {
                world.Send(this.Player, P.HashMessage("You can't use items in this map."));
                return;
            }

            this.Player.Mounted = !this.Player.Mounted;
            if (this.Player.Mounted)
            {
                this.Player.Inventory.ApplyMountBuff(mountSlot.Item, world);
            }
            else
            {
                this.Player.Inventory.RemoveMountBuff(mountSlot.Item, world);
            }

            this.Player.SendCHPString(world);
        }
    }
}
