using System.Text;

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
            Gold,
            Class
        }
        public RankTypes Type { get; set; }

        public List<Player>? RanksList { get; set; }

        private List<string> ranksStrings;
        private long lastUpdated;
        private int classId;

        /**
         * Constructor
         */
        public Ranks(RankTypes type, int classId = -1)
        {
            this.RanksList = [];
            this.ranksStrings = [];
            this.lastUpdated = 0;
            this.Type = type;
            this.classId = classId;
        }

        /**
         * GetRanks, returns the list of ranks
         *
         * If the information is out of date then it updates first
         *
         */
        public List<string> GetRanks(GameWorld world)
        {
            if ((world.TimeNow - this.lastUpdated) / (double)world.TimerFrequency >
                world.Settings.RankUpdatePeriod)
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
            this.ranksStrings = [];

            List<Player>? result = null;

            switch (this.Type)
            {
                case RankTypes.All:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.Access == Player.AccessStatus.Normal
                              orderby p.ExperienceSold descending
                              select p).Take(world.Settings.NumberOfRanks).ToList();
                    break;
                case RankTypes.Gold:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.Access == Player.AccessStatus.Normal
                              orderby p.Gold descending
                              select p).Take(world.Settings.NumberOfRanks).ToList();
                    break;
                case RankTypes.Class:
                    result = (from p in world.PlayerHandler.GetAllPlayerData()
                              where p.ClassID == this.classId && p.Access == Player.AccessStatus.Normal
                              orderby p.ExperienceSold descending
                              select p).Take(world.Settings.NumberOfRanks).ToList();
                    break;
            }

            this.RanksList = result;

            string line = "";
            int i = 1;
            foreach (var player in result!)
            {
                line = this.Type switch
                {
                    RankTypes.Class => i + ". " + player.Name + ", " +
                        Utils.FormatNumber(player.ExperienceSold) + " xp",
                    RankTypes.All => i + ". " + player.Name + ", " +
                        player.Class.ClassName +
                        ", " + Utils.FormatNumber(player.ExperienceSold) + " xp",
                    RankTypes.Gold => i + ". " + player.Name + ", " +
                        Utils.FormatNumber(player.Gold) + " gp",
                    _ => line,
                };
                i++;
                this.ranksStrings.Add(line);
            }
            while (i <= world.Settings.NumberOfRanks)
            {
                this.ranksStrings.Add(i + ". ");
                i++;
            }

            this.lastUpdated = world.TimeNow;
        }
    }
}
