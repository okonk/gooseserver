namespace Goose.Commands
{
    [Command("/mutemap", AccessPrivilege.MuteMap, Section = "GM", Help = "Mute or unmute chat on your map.")]
    public sealed class MuteMapCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            ctx.Player.Map.Muted = !ctx.Player.Map.Muted;

            ctx.World.SendToMap(ctx.Player.Map, P.ServerMessage($"Chat is now {(ctx.Player.Map.Muted ? "muted" : "unmuted")}."));
        }
    }
}
