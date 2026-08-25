using System.Text;
using System.Data;
using System.Data.SQLite;

namespace Goose
{
    public class Log
    {
        public enum Types
        {
            Chat = 0,
            Shout = 1,
            Auction = 2,
            JoinGame = 3,
            LeaveGame = 4,
            JoinGuild = 5,
            LeaveGuild = 6,
            GuildChat = 7,
            JoinGroup = 8,
            LeaveGroup = 9,
            GroupChat = 10,
            PickupItem = 11,
            PlayerDropItem = 12,
            Tell = 13,
            ReceivedCredits = 14,
            GaveCredits = 15,
            InvalidPassword = 16,
            CreatedCustom,
            BuyFromVendor,
            SellToVendor,
            Rebirth,
            BuyGold,
            BuyExperience,
            GiveSpirit,
            ResetItem,

            // GM-related logs
            GetItem = 10001,
            ClassChange,
            GiveExperience,
            GiveGold,
            RespawnMap,
            SpawnedNPC,
            MacroCheck,
            MacroCheckConfirm,
            MacroCheckFailed,
            Ban,
            Kick,
            SetPassword,
        }

        public Types Type { get; set; }
        public int PlayerID { get; set; }
        public int OtherID { get; set; }
        public int MapID { get; set; }
        public int MapX { get; set; }
        public int MapY { get; set; }
        public string Text { get; set; }
        public DateTime Time { get; set; }

        public Log(Types type, int playerid, string text, int otherid = 0, int mapid = 0, int mapx = 0, int mapy = 0)
        {
            this.Type = type;
            this.PlayerID = playerid;
            this.OtherID = otherid;
            this.Text = text;
            this.MapID = mapid;
            this.MapX = mapx;
            this.MapY = mapy;
            this.Time = DateTime.Now;
        }

        public void SaveToDatabase(GameWorld world)
        {
            string text = this.Text;
            DateTime time = this.Time;
            int type = (int)this.Type;
            int playerId = this.PlayerID;
            int otherId = this.OtherID;
            int mapId = this.MapID;
            int mapX = this.MapX;
            int mapY = this.MapY;

            string query = "INSERT INTO logs (text, log_date, log_type, playerid, otherid, mapid, mapx, mapy) VALUES (@logText, @logDate, ";
            query += type + ", ";
            query += playerId + ", ";
            query += otherId + ", ";
            query += mapId + ", ";
            query += mapX + ", ";
            query += mapY;
            query += ");";

            world.Database.Enqueue(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = query;
                command.Parameters.Add(new SQLiteParameter("@logText", DbType.String) { Value = text });
                command.Parameters.Add(new SQLiteParameter("@logDate", DbType.DateTime2) { Value = time });
                command.ExecuteNonQuery();
            });
        }
    }
}
