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

        public List<Player> RanksList { get; set; }
        List<String> ranksStrings;
        long lastUpdated;

        /**
         * Constructor
         */
        public Ranks(RankTypes type)
        {
            this.RanksList = new List<Player>();
            this.ranksStrings = new List<string>();
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

            return this.ranksStrings;
        }

        /**
         * Update, updates ranking information
         * 
         */
        public void Update(GameWorld world)
        {
            this.ranksStrings = new List<string>();

            List<Player> result = null;

            switch (this.Type)
            {
                case RankTypes.All:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.Access == Player.AccessStatus.Normal
                              orderby p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Gold:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.Access == Player.AccessStatus.Normal
                              orderby p.Gold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Magus:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 4 && p.Access == Player.AccessStatus.Normal
                              orderby p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Priest:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 5 && p.Access == Player.AccessStatus.Normal
                              orderby p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Rogue:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 2 && p.Access == Player.AccessStatus.Normal
                              orderby p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
                case RankTypes.Warrior:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == 3 && p.Access == Player.AccessStatus.Normal
                              orderby p.ExperienceSold descending
                              select p).Take(GameSettings.Default.NumberOfRanks).ToList();
                    break;
            }

            this.RanksList = result;

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
                            FormatNumber(player.ExperienceSold) + " xp";
                        break;
                    case RankTypes.All:
                        line = i + ". " + player.Name + ", " + 
                            player.Class.ClassName +
                            ", " + FormatNumber(player.ExperienceSold) + " xp";
                        break;
                    case RankTypes.Gold:
                        line = i + ". " + player.Name + ", " +
                            FormatNumber(player.Gold) + " gp";
                        break;
                }
                i++;
                this.ranksStrings.Add(line);
            }
            while (i <= GameSettings.Default.NumberOfRanks)
            {
                this.ranksStrings.Add(i + ". ");
                i++;
            }

            this.lastUpdated = world.TimeNow;
        }

        private static string FormatNumber(long num)
        {
            // Ensure number has max 3 significant digits (no rounding up can happen)
            long i = (long)Math.Pow(10, (int)Math.Max(0, Math.Log10(num) - 2));
            num = num / i * i;

            if (num >= 1000000000)
                return (num / 1000000000D).ToString("0.##") + "b";
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.##") + "m";
            if (num >= 1000)
                return (num / 1000D).ToString("0.##") + "k";

            return num.ToString("#,0");
        }
    }
}
