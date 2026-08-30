namespace Goose.Commands
{
    [Command("/spawnnpc ", AccessPrivilege.SpawnNPC, Section = "GM", Help = "Spawn an NPC at your location.")]
    public sealed class SpawnNpcCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int id)
        {
            var world = ctx.World;

            if (id <= 0) return;

            NPCTemplate? template = world.NPCHandler.GetNPCTemplate(id);
            if (template is null) return;

            world.NPCHandler.SpawnNPC(world, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY, template, shouldRespawn: false);

            world.LogHandler.Log(Log.Types.SpawnedNPC,
                ctx.Player.PlayerID, template.Name,
                template.NPCTemplateID, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
