using System.Text;
using Goose.Quests;

namespace Goose.Scripting
{
    public class BaseQuestScript : IQuestScript
    {
        public BaseQuestScript() { }

        public virtual bool IsMet(QuestRequirement requirement, Player player, GameWorld world)
        {
            return true;
        }

        public virtual string GetProgressText(QuestRequirement requirement, Player player, GameWorld world)
        {
            return "";
        }

        public virtual void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world)
        {

        }

        public virtual string? CanComplete(QuestReward reward, Player player, GameWorld world)
        {
            return null;
        }

        public virtual void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world)
        {

        }
    }
}
