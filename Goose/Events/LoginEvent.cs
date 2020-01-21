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
     * LoginEvent, event for the first packet, which should be the login packet
     * 
     * Called as soon as a client connects
     * Packet format: LOGINUsername,password,ALPHA33,3.5.2
     * 
     * OR for Illutia's newest client:
     * A 2 byte, followed by another byte the length of the message
     * The message is xor-encrypted with the key "Tamra"
     * The first byte should be 2, then 2 more bytes which are the version number
     * Then the next 32 bytes of md5 data
     * 16 bytes of username in ASCII
     * length of the username
     * 16 bytes of password in ASCII
     * length of the password
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

            string name;
            string password;

            if (packet.StartsWith("LOGIN"))
            {
                // regular old login packet
                if (packet.IndexOf(',') == -1) return;
                if (packet.Length < 6) return;

                name = packet.Substring(5, packet.IndexOf(',') - 5);

                string[] t = packet.Split(",".ToCharArray());

                if (t.Length < 2) return;
                password = t[1];
            }
            else
            {
                // "encrypted" login packet
                // we ignore checking the hash since we don't really care at this point

                byte[] data = Encoding.ASCII.GetBytes(packet).Skip(2).ToArray();
                byte[] keyBytes = Encoding.ASCII.GetBytes("Tamra");
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
                }

                if (data.Length < 69) return;

                name = Encoding.ASCII.GetString(data, 35, data[51]);
                password = Encoding.ASCII.GetString(data, 52, data[68]);
            }

            if (name.Length <= 1 || password.Length <= 1)
            {
                world.Send(new Player() { Sock = sock }, P.LoginDenied("Please use a longer username or password."));
                world.GameServer.Disconnect(sock);
                return;
            }

            if (world.PlayerHandler.IsLoggedIn(name.ToLower()))
            {
                world.Send(new Player() { Sock = sock }, P.LoginDenied("Character is already logged in."));
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
                            world.Send(new Player() { Sock = sock }, P.LoginDenied("You can't create any more characters today."));
                            world.GameServer.Disconnect(sock);
                            return;
                        }
                    }

                    created++;
                    world.CharactersCreatedPerIP[IP] = created;
                }

                if (GameSettings.Default.AutoCharacterCreation)
                {
                    if (!name.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                    {
                        world.Send(new Player() { Sock = sock }, P.LoginDenied("Your name must contain letters only."));
                        world.GameServer.Disconnect(sock);
                        return;
                    }

                    player = new Player(0);
                    player.Sock = sock;
                    player.LoadFromAutoCreate(name, password, world);
                    world.PlayerHandler.AddPlayerToData(player);

                    this.Player = player;
                }
                else
                {
                    world.Send(new Player() { Sock = sock }, P.LoginDenied("Character does not exist."));
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

                    world.Send(this.Player, P.LoginDenied("Wrong password for character."));
                    world.GameServer.Disconnect(this.Player.Sock);
                    return;
                }
                else if (player.Access == Player.AccessStatus.Banned)
                {
                    string banRemaining = "";
                    if (player.UnbanDate.HasValue)
                    {
                        if (player.UnbanDate.Value < DateTime.Now)
                        {
                            player.Access = Player.AccessStatus.Normal;
                            player.UnbanDate = null;
                        }
                        else
                        {
                            var remaining = player.UnbanDate.Value - DateTime.Now;
                            if (remaining.Days > 0)
                                banRemaining += " " + remaining.Days + "d";
                            if (remaining.Hours > 0)
                                banRemaining += " " + remaining.Hours + "h";
                            if (remaining.Minutes > 0)
                                banRemaining += " " + remaining.Minutes + "m";
                        }
                    }

                    if (player.Access == Player.AccessStatus.Banned)
                    {
                        world.Send(this.Player, P.LoginDenied("You have been banned." + banRemaining));
                        world.GameServer.Disconnect(this.Player.Sock);
                        return;
                    }
                }
            }

            if (GameSettings.Default.LockdownModeEnabled && this.Player.Access != Player.AccessStatus.GameMaster)
            {
                world.Send(this.Player, P.LoginDenied("Server is currently under maintenance. Try again soon."));
                world.GameServer.Disconnect(this.Player.Sock);
                return;
            }

            world.PlayerHandler.AddPlayer(this.Player, world);
            if (this.Player.LoginID == 0)
            {
                world.Send(this.Player, P.LoginDenied("The server is full. Try again later."));
                world.GameServer.Disconnect(this.Player.Sock);
                world.PlayerHandler.RemovePlayer(this.Player);
                return;
            }

            world.LogHandler.Log(Log.Types.JoinGame, this.Player.PlayerID, IP);

            if (this.Player.Access != Goose.Player.AccessStatus.GameMaster)
                world.SendToAll(P.ServerMessage(this.Player.Name + " has joined the world."));

            this.Player.State = Player.States.LoadingGame;
            world.Send(this.Player, P.LoginAccepted(GameSettings.Default.ServerName));

            this.Player.Windows = new List<Window>();
            this.Player.LastPing = world.TimeNow;
            this.Player.LastActive = world.TimeNow;
            this.Player.LastPlaytimeUpdate = world.TimeNow;
            this.Player.SuspectedMacroCount = this.Player.SuspectedMacroCount / 2;
            this.Player.AddSaveEvent(world);

            this.Player.CurrentHP = (int)(this.Player.MaxHP * 0.8);
            this.Player.CurrentMP = (int)(this.Player.MaxMP * 0.8);
            this.Player.CurrentSP = this.Player.MaxStats.SP;

            this.Player.UpdateIdleStatus(world);

            Console.WriteLine(this.Player.Name + " logged in.");
        }
    }
}
