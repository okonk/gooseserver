using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * RankHandler, handles caching of ranks
     *
     *
     */
    public class RankHandler
    {
        public Ranks All { get; set; }
        public Ranks Gold { get; set; }
        public Dictionary<string, Ranks> ClassRanks { get; set; }

        public RankHandler()
        {
            this.All = new Ranks(Ranks.RankTypes.All);
            this.Gold = new Ranks(Ranks.RankTypes.Gold);
            this.ClassRanks = new Dictionary<string, Ranks>();
        }

        public void UpdateAll(GameWorld world)
        {
            this.All.Update(world);
            this.Gold.Update(world);
            foreach (var rank in this.ClassRanks.Values)
                rank.Update(world);
        }

        public void AddClass(Class @class)
        {
            this.ClassRanks[@class.ClassName.ToLowerInvariant()] = new Ranks(Ranks.RankTypes.Class, @class.ClassID);
        }
    }
}
