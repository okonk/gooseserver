using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    public class Combination
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }
        public long ClassRestrictions { get; set; }

        public List<ItemTemplate> ResultItems { get; set; }

        public Dictionary<int, int> RequiredHash { get; set; }
    }
}
