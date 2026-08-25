using System.Data.SqlClient;
using System.Text;

namespace Goose.Quests
{
    public class QuestProgress
    {
        public QuestRequirement Requirement { get; set; }
        public long Value { get; set; }
    }
}
