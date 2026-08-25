using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

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
        private Dictionary<string, Player> allNameToPlayer = new();

        private List<Player> players = new();
        private Dictionary<Socket, Player> sockToPlayer = new();
        private Dictionary<string, Player> nameToPlayer = new();
        // Support LoginIDs 1..MaxPlayers inclusive; index 0 unused (LoginID 0 = none / full)
        private Player[] idToPlayer;

        public PlayerHandler(GooseSettings settings)
        {
            this.idToPlayer = new Player[settings.MaxPlayers + 1];
        }

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
         * AddPlayer, adds a player to the handler
         * 
         * Takes a Player, adds the player to Players list
         * Also adds a key to the dictionary to map Socket to Player
         * 
         * Automatically gives a player an ID
         * 
         */
        public void AddPlayer(Player player, GameWorld world)
        {
            player.LoginID = this.GetNewID(world);
            this.players.Add(player);
            this.sockToPlayer[player.Sock] = player;
            this.nameToPlayer[player.Name.ToLower()] = player;
            if (player.LoginID != 0)
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
            if (this.sockToPlayer.TryGetValue(sock, out var player))
            {
                this.RemovePlayer(player);
            }
        }

        /**
         * RemovePlayer, removes a player from the PlayerHandler by Player
         * 
         * Removes the Player from our dictionary and list.
         * 
         */
        public void RemovePlayer(Player player)
        {
            this.sockToPlayer.Remove(player.Sock);
            this.players.Remove(player);
            this.nameToPlayer.Remove(player.Name.ToLower());
            if (player.LoginID != 0 && player.LoginID < this.idToPlayer.Length)
                this.idToPlayer[player.LoginID] = null;
            player.LoginID = 0;
        }

        /// <summary>
        /// Returns the lowest free LoginID in 1..MaxPlayers, or 0 if the server is full.
        /// </summary>
        public int GetNewID(GameWorld world)
        {
            for (int id = 1; id <= world.Settings.MaxPlayers; id++)
            {
                if (this.idToPlayer[id] == null)
                    return id;
            }
            return 0;
        }

        public void AssignNewId(GameWorld world, Player player)
        {
            if (player.LoginID != 0 && player.LoginID < this.idToPlayer.Length)
                this.idToPlayer[player.LoginID] = null;

            player.LoginID = this.GetNewID(world);
            if (player.LoginID != 0)
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
            if (this.sockToPlayer.TryGetValue(sock, out var player))
                return player;

            return null;
        }

        /**
         * GetPlayer, takes a name and returns the associated Player
         * 
         */
        public Player GetPlayer(string name)
        {
            return this.nameToPlayer.TryGetValue(name.ToLower(), out var player) ? player : null;
        }

        /**
         * IsLoggedIn, checks if a player is logged in
         * 
         */
        public bool IsLoggedIn(string name)
        {
            return GetPlayer(name) != null;
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

            if (this.allNameToPlayer.TryGetValue(name, out var player))
                return player;

            return null;
        }

        public void LoadPlayerData(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM players";
                using var reader = command.ExecuteReader();

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
            });

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

        /// <summary>
        /// Updates the online name index after a player's name has already been changed.
        /// Call from /changename when the player is logged in.
        /// </summary>
        public void RenamePlayer(Player player, string newName)
        {
            this.nameToPlayer.Remove(player.Name.ToLower());
            this.nameToPlayer[newName.ToLower()] = player;
        }

        public List<Player> GetAllPlayerData()
        {
            return this.allNameToPlayer.Values.ToList();
        }
    }
}
