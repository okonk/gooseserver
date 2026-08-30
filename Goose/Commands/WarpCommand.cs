namespace Goose.Commands
{
    [Command("/warp ", AccessPrivilege.Warp, Section = "GM", Help = "Warp to a location.")]
    public sealed class WarpCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int mapId = 1, int mapx = 50, int mapy = 50)
        {
            // The key's trailing space means bare /warp never matches, so partial
            // args must stay a silent no-op to preserve the legacy all-or-nothing warp.
            if (ctx.Args.Length != 3) return;

            var world = ctx.World;
            Map? map = world.MapHandler.GetMap(mapId);
            if (map is not null)
            {
                // invalid coordinates
                if (mapx < 1 || mapx >= map.Width + 1 || mapy < 1 || mapy >= map.Height + 1) return;

                ctx.Player.WarpTo(world, map, mapx, mapy);
            }
        }
    }
}
