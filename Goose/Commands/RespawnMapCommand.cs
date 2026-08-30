namespace Goose.Commands
{
    [Command("/respawnmap", AccessPrivilege.RespawnMap, Section = "Admin", Help = "Respawn all dead NPCs on your map.")]
    public sealed class RespawnMapCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;

            foreach (var npc in ctx.Player.Map.NPCs)
            {
                if (npc.State == NPC.States.Dead)
                {
                    npc.Spawn(world);
                }
            }

            world.SendToMap(ctx.Player.Map, P.ServerMessage("Respawned all NPCs."));

            world.LogHandler.Log(Log.Types.RespawnMap,
                ctx.Player.PlayerID, "",
                ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
