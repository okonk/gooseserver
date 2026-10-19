using System.Text;

namespace Goose.Scripting
{
    public class BaseGlobalScript : IGlobalScript
    {
        public BaseGlobalScript() { }

        public virtual void OnLoaded(GameWorld world)
        {

        }

        public virtual void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
        {

        }
    }
}
