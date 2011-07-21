using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Goose.Events
{
    /**
     * LoginEvent, event for LOGIN packet
     * 
     * Called as soon as a client connects
     * Packet format: LOGINUsername,password,ALPHA33,3.5.2
     * 
     * Server responds: LOKServername or LNOReason for failure
     * Login OK or Login Not OK
     * 
     */
    public class LoginEvent : Event
    {

        public enum LoginMessages
        {
            Success = 0,
            WrongPassword,
            Banned
        }

        public static Event Create(Player player, Object data)
        {
            Event e = new LoginEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player != null) return;

            string packet = (string)(((Object[])this.Data)[1]);
            Socket sock = (Socket)(((Object[])this.Data)[0]);

            string IP = sock.RemoteEndPoint.ToString();
            IP = IP.Substring(0, IP.IndexOf(":"));

            if (packet.IndexOf(',') == -1) return;

            string name = packet.Substring(5, packet.IndexOf(',') - 5);

            string[] t = packet.Split(",".ToCharArray());

            if (t.Length < 2) return;
            string password = t[1];

            if (name.Length <= 1 || password.Length <= 1)
            {
                world.Send(new Player() { Sock = sock }, "LNOPlease use a longer username or password.");
                world.GameServer.Disconnect(sock);
                return;
            }

            if (world.PlayerHandler.IsLoggedIn(name.ToLower()))
            {
                world.Send(new Player() { Sock = sock }, "LNOCharacter is already logged in.");
                world.GameServer.Disconnect(sock);
                return;
            }

            Player player = world.PlayerHandler.GetPlayerFromData(name);

            if (player == null)
            {
                // limit characters created per day
                if (GameSettings.Default.NewCharactersPerDayPerIP > 0)
                {
                    int created;
                    if (world.CharactersCreatedPerIP.TryGetValue(IP, out created))
                    {
                        if (created >= GameSettings.Default.NewCharactersPerDayPerIP)
                        {
                            world.Send(new Player() { Sock = sock }, "LNOYou can't create any more characters today.");
                            world.GameServer.Disconnect(sock);
                            return;
                        }
                    }

                    created++;
                    world.CharactersCreatedPerIP[IP] = created;
                }

                if (GameSettings.Default.AutoCharacterCreation)
                {
                    player = new Player(0);
                    player.Sock = sock;
                    player.LoadFromAutoCreate(name, password, world);
                    world.PlayerHandler.AddPlayerToData(player);

                    this.Player = player;
                }
                else
                {
                    world.Send(new Player() { Sock = sock }, "LNOCharacter does not exist.");
                    world.GameServer.Disconnect(sock);
                    return;
                }
            }
            else
            {
                this.Player = player;
                this.Player.Sock = sock;

                string salt = Encoding.ASCII.GetString(Convert.FromBase64String(this.Player.PasswordSalt));

                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] data = Encoding.ASCII.GetBytes(salt + password + GameSettings.Default.ServerName);
                data = md5.ComputeHash(data);
                string hash = BitConverter.ToString(data).Replace("-", "").ToLower();

                // Incorrect password
                if (!this.Player.PasswordHash.Equals(hash))
                {
                    world.LogHandler.Log(Log.Types.InvalidPassword, this.Player.PlayerID, IP);

                    world.Send(this.Player, "LNOWrong password for character.");
                    world.GameServer.Disconnect(this.Player.Sock);
                    return;
                }
                else if (player.Access == Goose.Player.AccessStatus.Banned)
                {
                    world.Send(this.Player, "LNOYour character has been banned.");
                    world.GameServer.Disconnect(this.Player.Sock);
                    return;
                }
            }

            if (GameSettings.Default.LockdownModeEnabled && this.Player.Access != Player.AccessStatus.GameMaster)
            {
                world.Send(this.Player, "LNOServer is currently under maintenance. Try again soon.");
                world.GameServer.Disconnect(this.Player.Sock);
                return;
            }

            world.PlayerHandler.AddPlayer(this.Player);
            if (this.Player.LoginID == 0)
            {
                world.Send(this.Player, "LNOThe server is full. Try again later.");
                world.GameServer.Disconnect(this.Player.Sock);
                world.PlayerHandler.RemovePlayer(this.Player);
                return;
            }

            world.LogHandler.Log(Log.Types.JoinGame, this.Player.PlayerID, IP);

            if (this.Player.Access != Goose.Player.AccessStatus.GameMaster)
                world.SendToAll("$7" + this.Player.Name + " has joined the world.");

            this.Player.State = Player.States.LoadingGame;
            world.Send(this.Player, "LOK" + GameSettings.Default.ServerName);

            this.Player.Windows = new List<Window>();
            this.Player.LastPing = world.TimeNow;
            this.Player.LastActive = world.TimeNow;
            this.Player.LastPlaytimeUpdate = world.TimeNow;
            this.Player.AddSaveEvent(world);

            this.Player.CurrentHP = (int)(this.Player.MaxStats.HP * 0.8);
            this.Player.CurrentMP = (int)(this.Player.MaxStats.MP * 0.8);
            this.Player.CurrentSP = this.Player.MaxStats.SP;

            this.Player.UpdateIdleStatus(world);

            Console.WriteLine(this.Player.Name + " logged in.");
        }
    }
}
