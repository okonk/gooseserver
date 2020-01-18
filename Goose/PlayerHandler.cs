using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Data.SqlClient;

namespace Goose
{
    /**
     * PlayerHandler, handles Player objects
     * 
     * Has a list of Players and a mapping of Sockets to Players
     * 
     */
    public class PlayerHandler
    {
        /// <summary>
        /// A mapping of all player names to their corresponding Player object. Loaded on startup
        /// </summary>
        Hashtable allNameToPlayer;

        List<Player> players;
        Hashtable sockToPlayer;
        Player[] idToPlayer;

        int currentdbid = 1;
        /// <summary>
        /// Gets/sets the next available player database id
        /// </summary>
        public int CurrentID
        {
            get { return this.currentdbid; }
            set { this.currentdbid = value; }
        }
        
        /**
         * Constructor
         * 
         * Constructs list/mapping
         * 
         */
        public PlayerHandler()
        {
            this.players = new List<Player>();
            this.sockToPlayer = new Hashtable();
            this.idToPlayer = new Player[GameSettings.Default.MaxPlayers];
            this.allNameToPlayer = new Hashtable();
        }

        /**
         * AddPlayer, adds a player to the handler
         * 
         * Takes a Player, adds the player to Players list
         * Also adds a key to the hashtable to map Socket to Player
         * 
         * Automatically gives a player an ID
         * 
         */
        public void AddPlayer(Player player, GameWorld world)
        {
            player.LoginID = this.GetNewID(world);
            this.players.Add(player);
            this.sockToPlayer[player.Sock] = player;
            this.idToPlayer[player.LoginID] = player;
        }

        /**
         * RemovePlayer, removes a player from the PlayerHandler by Socket
         * 
         * First finds the associated Player from the Socket
         * Then calls RemovePlayer(Player)
         * 
         */
        public void RemovePlayer(Socket sock)
        {
            Player player = (Player)this.sockToPlayer[sock];
            if (player != null)
            {
                this.RemovePlayer(player);
            }
        }

        /**
         * RemovePlayer, removes a player from the PlayerHandler by Player
         * 
         * Removes the Player from our hashtable and list.
         * 
         */
        public void RemovePlayer(Player player)
        {
            this.sockToPlayer.Remove(player.Sock);
            this.players.Remove(player);
            this.idToPlayer[player.LoginID] = null;
        }


        public int GetNewID(GameWorld world)
        {
            int id;
            do
            {
                id = world.Random.Next(1, GameSettings.Default.MaxPlayers);
            } while (this.idToPlayer[id] != null);

            return id;
        }

        public void AssignNewId(GameWorld world, Player player)
        {
            if (player.LoginID != 0 && this.idToPlayer[player.LoginID] != null)
            {
                this.idToPlayer[player.LoginID] = null;
            }

            player.LoginID = this.GetNewID(world);
            this.idToPlayer[player.LoginID] = player;
        }

        /**
         * GetPlayer, takes a socket and returns the associated Player
         * 
         * Just uses our dictionary mapping to get the Player object
         * 
         */
        public Player GetPlayer(Socket sock)
        {
            return (Player)this.sockToPlayer[sock];
        }

        /**
         * GetPlayer, takes a name and returns the associated Player
         * 
         */
        public Player GetPlayer(string name)
        {
            name = name.ToLower();

            foreach (Player player in this.players)
            {
                if (name.Equals(player.Name.ToLower())) return player;
            }

            return null;
        }

        /**
         * IsLoggedIn, checks if a player is logged in
         * 
         * 
         */
        public bool IsLoggedIn(string name)
        {
            foreach (Player player in this.players)
            {
                if (name.Equals(player.Name.ToLower())) return true;
            }

            return false;
        }

        /**
         * PlayerCount, readonly, returns the player count
         * 
         */
        public int PlayerCount 
        {
            get { return this.players.Count; }
        }

        /**
         * Players, readonly, returns the current players
         * 
         */
        public List<Player> Players 
        {
            get { return this.players; }
        }

        /**
         * PlayerDataCount, readonly, returns the player database count
         * 
         */
        public int PlayerDataCount
        {
            get { return this.allNameToPlayer.Count; }
        }

        /**
         * GetPlayerFromData, takes a name and returns the associated Player from the in-memory database
         * 
         */
        public Player GetPlayerFromData(string name)
        {
            name = name.ToLower();

            return (Player)this.allNameToPlayer[name];
        }

        public void LoadPlayerData(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM players";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                Player player = new Player(0);
                player.LoadFromReader(world, reader);

                if (player.PlayerID >= this.CurrentID)
                {
                    this.CurrentID = player.PlayerID + 1;
                }

                if (player.Access == Player.AccessStatus.Deleted) continue;

                this.allNameToPlayer[player.Name.ToLower()] = player;
            }

            foreach (Player player in this.allNameToPlayer.Values)
            {
                player.LoadAdditional(world);
            }
        }

        public void AddPlayerToData(Player player)
        {
            this.allNameToPlayer[player.Name.ToLower()] = player;
        }

        public void RemovePlayerFromData(Player player)
        {
            this.allNameToPlayer.Remove(player.Name.ToLower());
        }

        public List<Player> GetAllPlayerData()
        {
            return this.allNameToPlayer.Values.Cast<Player>().ToList();
        }
    }
}
