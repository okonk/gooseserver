namespace Goose.Commands
{
    [Command("/checkname ", AccessPrivilege.ChangeName, Section = "GM", Help = "Check whether a name is taken.")]
    public sealed class CheckNameCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] name)
        {
            string nameText = string.Join(" ", name);
            Player? player = ctx.World.PlayerHandler.GetPlayerFromData(nameText);

            if (player is null)
            {
                ctx.Send(nameText + " is currently unused.");
            }
            else
            {
                ctx.Send(nameText + " is used.");
            }
        }
    }
}
