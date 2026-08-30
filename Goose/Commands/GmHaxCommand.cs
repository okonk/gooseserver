namespace Goose.Commands
{
    [Command("/gmhax ", AccessPrivilege.Debug, Section = "Admin", Help = "Send yourself a modified CHP packet.")]
    public sealed class GmHaxCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;
            var player = ctx.Player;

            if (player.Access != Player.AccessStatus.GameMaster) return;

            var data = ctx.Remainder;

            int pose = player.BodyState;
            ItemSlot? weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon is not null)
            {
                pose = weapon.Item.BodyState;
            }

            var chp = "CHP" +
                player.LoginID + "," +
                player.CurrentBodyID + "," +
                player.BodyR + "," + // Body Color R
                player.BodyG + "," + // Body Color G
                player.BodyB + "," + // Body Color B
                player.BodyA + "," + // Body Color A
                (player.CurrentBodyID >= 100 ? 3 : pose) + "," +
                (player.CurrentBodyID >= 100 ? "" : player.HairID + ",") +
                (player.CurrentBodyID >= 100 ? "" : player.Inventory.EquippedDisplay()) + // Note: EquippedDisplay() adds it's own , on end
                (player.CurrentBodyID >= 100 ? "" : player.HairR + ",") +
                (player.CurrentBodyID >= 100 ? "" : player.HairG + ",") +
                (player.CurrentBodyID >= 100 ? "" : player.HairB + ",") +
                (player.CurrentBodyID >= 100 ? "" : player.HairA + ",") +
                "0" + "," + // Invis thing
                (player.CurrentBodyID >= 100 ? "" : player.FaceID + ",") +
                data + "," + // Move Speed
                (player.CurrentBodyID >= 100 ? "" : player.Inventory.MountDisplay()); // Mount

            world.Send(player, chp);


            // var data = ctx.Remainder;
            // world.Send(player, data);
            // foreach (var other in player.Map.GetPlayersInRange(player))
            // {
            //     world.Send(other, data);
            // }
        }
    }
}
