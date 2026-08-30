namespace Goose.Commands
{
    [Command("/broadcast ", AccessPrivilege.Broadcast, Section = "GM", Help = "Send a message to all players.", Usage = "/broadcast <message...>")]
    public sealed class BroadcastCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] message)
        {
            string text = string.Join(" ", message);

            if (text.Length <= 0) return;

            ctx.World.SendToAll(P.ServerMessage($"[{ctx.Player.Access.ToString().Replace("Master", " Master")}]: {text}"));
        }
    }
}
