using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
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


            // GM-related logs
            GetItem = 10001,
            ClassChange,
            GiveExperience,
            RespawnMap,
            SpawnedNPC,
            MacroCheck,
            MacroCheckConfirm,
            MacroCheckFailed,
            Ban,
            Kick,
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
            var logTextParam = new SQLiteParameter("@logText", DbType.String) { Value = this.Text };
            var logDateParam = new SQLiteParameter("@logDate", DbType.DateTime2) { Value = this.Time };

            string query = "INSERT INTO logs (text, log_date, log_type, playerid, otherid, mapid, mapx, mapy) VALUES (@logText, @logDate, ";
            query += (int)this.Type + ", ";
            query += this.PlayerID + ", ";
            query += this.OtherID + ", ";
            query += this.MapID + ", ";
            query += this.MapX + ", ";
            query += this.MapY;
            query += ");";

            var command = world.SqlConnection.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(logTextParam);
            command.Parameters.Add(logDateParam);
            world.DatabaseWriter.Add(command);
        }
    }
}
