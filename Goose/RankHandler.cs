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
        public Ranks Magus { get; set; }
        public Ranks Warrior { get; set; }
        public Ranks Rogue { get; set; }
        public Ranks Priest { get; set; }
        public Ranks All { get; set; }
        public Ranks Gold { get; set; }

        public RankHandler()
        {
            this.Magus = new Ranks(Ranks.RankTypes.Magus);
            this.Warrior = new Ranks(Ranks.RankTypes.Warrior);
            this.Rogue = new Ranks(Ranks.RankTypes.Rogue);
            this.Priest = new Ranks(Ranks.RankTypes.Priest);
            this.All = new Ranks(Ranks.RankTypes.All);
            this.Gold = new Ranks(Ranks.RankTypes.Gold);
        }

        public void UpdateAll(GameWorld world)
        {
            this.Magus.Update(world);
            this.Warrior.Update(world);
            this.Rogue.Update(world);
            this.Priest.Update(world);
            this.All.Update(world);
            this.Gold.Update(world);
        }
    }
}
