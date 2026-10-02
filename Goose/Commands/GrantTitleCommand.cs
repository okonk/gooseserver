namespace Goose.Commands
{
    [Command("/granttitle ", AccessPrivilege.SetTitle, Section = "GM", Help = "Unlock a title for a player and equip it.")]
    public sealed class GrantTitleCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, string[] title)
        {
            var world = ctx.World;
            string titleText = string.Join(" ", title);

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is null)
            {
                ctx.Send("Couldn't find player.");
                return;
            }

            player.GrantTitle(titleText, world);
            ctx.Send("Granted title successfully.");

            if (player.State == Player.States.NotLoggedIn)
                player.SaveToDatabase(world);
        }
    }
}
