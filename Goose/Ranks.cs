using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Goose
{
    /**
     * Ranks, holds a list of strings which contain rank info
     * 
     */
    public class Ranks
    {
        public enum RankTypes
        {
            All = 0,
            Rogue,
            Warrior,
            Priest,
            Magus,
            Gold
        }
        public RankTypes Type { get; set; }

        List<String> ranks;
        long lastUpdated;

        /**
         * Constructor
         */
        public Ranks(RankTypes type)
        {
            this.ranks = new List<string>();
            this.lastUpdated = 0;
            this.Type = type;
        }

        /**
         * GetRanks, returns the list of ranks
         * 
         * If the information is out of date then it updates first
         * 
         */
        public List<string> GetRanks(GameWorld world)
        {
            if ((world.TimeNow - this.lastUpdated) * world.TimerFrequency >
                GameSettings.Default.RankUpdatePeriod)
            {
                this.Update(world);
            }

            return this.ranks;
        }

        /**
         * Update, updates ranking information
         * 
         */
        public void Update(GameWorld world)
        {
            this.ranks = new List<string>();

            List<Player> result = null;

            switch (this.Type)
            {
                case RankTypes.All:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              orderby p.Experience + p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Gold:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              orderby p.Gold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Magus:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 4
                              orderby p.Experience + p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Priest:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 5
                              orderby p.Experience + p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Rogue:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 2
                              orderby p.Experience + p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Warrior:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 3
                              orderby p.Experience + p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
            }

            string line = "";
            int i = 1;
            foreach (Player player in result)
            {
                switch (this.Type)
                {
                    case RankTypes.Magus:
                    case RankTypes.Priest:
                    case RankTypes.Rogue:
                    case RankTypes.Warrior:
                        line = i + ". " + player.Name + ", " +
                            (player.Experience + player.ExperienceSold) + " xp";
                        break;
                    case RankTypes.All:
                        line = i + ". " + player.Name + ", " + 
                            player.Class.ClassName +
                            ", " + (player.Experience + player.ExperienceSold) + " xp";
                        break;
                    case RankTypes.Gold:
                        line = i + ". " + player.Name + ", " +
                            player.Gold + " gp";
                        break;
                }
                i++;
                this.ranks.Add(line);
            }
            while (i <= GameSettings.Default.NumberOfRanks)
            {
                this.ranks.Add(i + ". ");
                i++;
            }

            this.lastUpdated = world.TimeNow;
        }
    }
}
