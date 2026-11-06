#load "ArenaCore.csx"
using System.Collections.Generic;
using Goose;
using Goose.Scripting;

public class ArenaController : BaseGlobalScript
{
    public override void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
        => ArenaCore.OnNpcKilled(npc, killer, credited, world);
}

return typeof(ArenaController);
