namespace Goose.Commands
{
    [Command("/grantsurname ", AccessPrivilege.SetSurname, Section = "GM", Help = "Unlock a surname for a player and equip it.")]
    public sealed class GrantSurnameCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string[] surname)
        {
            var world = ctx.World;
            string surnameText = string.Join(" ", surname);

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is null)
            {
                ctx.Send("Couldn't find player.");
                return;
            }

            player.GrantSurname(surnameText, world);
            ctx.Send("Granted surname successfully.");

            if (player.State == Player.States.NotLoggedIn)
                player.SaveToDatabase(world);
        }
    }
}
