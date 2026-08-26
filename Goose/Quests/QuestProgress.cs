using System.Data.SqlClient;
using System.Text;

namespace Goose.Quests
{
    public class QuestProgress
    {
        public QuestRequirement Requirement { get; set; } = null!;
        public long Value { get; set; }
    }
}
