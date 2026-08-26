using System.Collections;
using System.Text;

namespace Goose
{
    public class Combination
    {
        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }
        public long ClassRestrictions { get; set; }

        public List<ItemTemplate> ResultItems { get; set; } = null!;

        public Dictionary<int, int> RequiredHash { get; set; } = null!;
    }
}
