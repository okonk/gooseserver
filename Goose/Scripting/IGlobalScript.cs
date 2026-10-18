using System.Text;

namespace Goose.Scripting
{
    public interface IGlobalScript
    {
        void OnLoaded(GameWorld world);
        void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world);
    }
}
