using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using Goose.Scripting;

namespace Goose
{
    /**
     * Map, holds map information/methods
     * 
     */
    public class Map
    {
        // Viewing ranges
        public static int RANGE_X = 16;
        public static int RANGE_Y = 12;

        /**
         * map_id
         * 
         */
        public int ID { get; set; }
        /**
         * map_name
         * 
         */
        public string Name { get; set; }
        public string FileName { get; set; }
        /**
         * map_x
         * 
         */
        public int Width { get; set; }
        /**
         * map_y
         * 
         */
        public int Height { get; set; }

        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }

        public bool CanPVP { get; set; }
        public bool CanChat { get; set; }
        public bool CanAuction { get; set; }
        public bool CanShout { get; set; }
        public bool CanCast { get; set; }
        public bool CanBind { get; set; }
        public bool CanUseItems { get; set; }
        public bool CanSpawnPets { get; set; }
        public bool Muted { get; set; }

        public Script<IMapScript> Script { get; set; }

        public string ScriptParams { get; set; }

        public object ScriptStore { get; set; }

        ICharacter[] characters;
        ITile[] tiles;
        List<Player> players;
        List<int> requiredItems;
        List<NPC> npcs;
        List<ItemTile> items;

        /**
         * Players, returns player list
         * 
         */
        public List<Player> Players
        {
            get { return this.players; }
        }

        /**
         * Constructor
         * 
         */
        public Map(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            this.players = new List<Player>();
            this.requiredItems = new List<int>();
            this.npcs = new List<NPC>();
            this.items = new List<ItemTile>();
        }

        public static bool InRange(ICharacter a, ICharacter b)
        {
            return (a.Map.ID == b.Map.ID &&
                Math.Abs(a.MapX - b.MapX) < RANGE_X &&
                Math.Abs(a.MapY - b.MapY) < RANGE_Y);
        }

        /**
         * GetPlayersInRange, returns all players that the character can see
         * 
         */
        public List<Player> GetPlayersInRange(ICharacter character)
        {
            List<Player> range = (from p in this.players
                                  where Math.Abs(p.MapX - character.MapX) < RANGE_X &&
                                        Math.Abs(p.MapY - character.MapY) < RANGE_Y &&
                                        p != character
                                  select p).ToList<Player>();
            return range;
        }

        /**
         * GetNPCsInRange, returns all npcs that the character can see
         * 
         */
        public List<NPC> GetNPCsInRange(ICharacter character)
        {
            List<NPC> range = (from p in this.npcs
                               where Math.Abs(p.MapX - character.MapX) < RANGE_X &&
                                     Math.Abs(p.MapY - character.MapY) < RANGE_Y &&
                                     p != character &&
                                     p.State == NPC.States.Alive
                               select p).ToList<NPC>();
            return range;
        }

        /**
         * AddPlayer, adds player to players list
         * 
         */
        public void AddPlayer(Player player, GameWorld world)
        {
            this.players.Add(player);

            try
            {
                this.Script?.Object.OnPlayerEntered(this, player, world);
            }
            catch (Exception e)
            {
                // TODO: need a logging system
            }
        }

        /**
         * RemovePlayer, removes player from players list
         * 
         */
        public void RemovePlayer(Player player, GameWorld world)
        {
            this.players.Remove(player);

            try
            {
                this.Script?.Object.OnPlayerLeft(this, player, world);
            }
            catch (Exception e)
            {
                // TODO: need a logging system
            }
        }

        /**
         * AddNPC, adds npc to npcs list
         * 
         */
        public void AddNPC(NPC npc)
        {
            this.npcs.Add(npc);
        }

        /**
         * RemoveNPC, removes npc from npcs list
         * 
         */
        public void RemoveNPC(NPC npc)
        {
            this.npcs.Remove(npc);
        }

        /**
         * AddItem, adds item to items list
         * adds item to tiles array
         * 
         * Updates everyone in the map about the item
         * 
         */
        public void AddItem(ItemTile item, GameWorld world)
        {
            this.items.Add(item);
            this.tiles[item.Y * this.Width + item.X] = item;

            item.DroppedTime = world.TimeNow;

            world.SendToMap(this, item.MOBString());
        }

        /**
         * RemoveItem, removes item from items list
         * removes item from tiles array
         * 
         * Updates everyone in the map about the item
         * 
         */
        public void RemoveItem(ItemTile item, GameWorld world)
        {
            this.items.Remove(item);
            this.tiles[item.Y * this.Width + item.X] = null;

            world.SendToMap(this, item.EOBString());
        }

        /**
         * CanMoveTo, checks if character can move to x, y
         * 
         */
        public bool CanMoveTo(ICharacter character, int x, int y)
        {
            // invalid coordinates
            if (x < 1 || x >= this.Width + 1 || y < 1 || y >= this.Height + 1) return false;

            if ((Math.Abs(character.MapX - x) == 1 && Math.Abs(character.MapY - y) == 0) ||
                (Math.Abs(character.MapX - x) == 0 && Math.Abs(character.MapY - y) == 1))
            {
                ITile tile = this.tiles[y * this.Width + x];
                if (tile != null)
                {
                    if (tile is WarpTile)
                    {
                        if (character is Pet) return false;
                        else if (character is Player) return true;
                        else return false;
                    }
                }

                return !this.IsTileBlocked(character, x, y);
            }

            return false;
        }

        /**
         * PlacePlayer, places a character on the map
         * 
         * This method checks if the players current coordinates are valid and not blocked
         * if they're blocked it moves the player until they can be placed
         * 
         */
        public void PlaceCharacter(ICharacter character)
        {
            // radius at which we're searching
            int r = 0;
            // set origin
            int ox = character.MapX;
            int oy = character.MapY;

            // this loop is for increasing radius until we find a good tile
            while (true)
            {
                // searches the radius around origin
                for (int y = oy - r; y < oy + r + 1; y++)
                {
                    // within map bounds
                    if (y > 0 && y <= this.Height)
                    {
                        // searches the radius around origin
                        for (int x = ox - r; x < ox + r + 1; x++)
                        {
                            // within map bounds
                            if (x > 0 && x <= this.Width)
                            {
                                // if x or y is at the radius we're searching 
                                // so we don't search already searched tiles
                                if ((y == oy - r || y == oy + r) || (x == ox - r || x == ox + r))
                                {
                                    if (!this.IsTileBlocked(character, x, y))
                                    {
                                        character.MapX = x;
                                        character.MapY = y;

                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                r++;
            }
        }

        /**
         * PlaceItem, places an item on the map
         * 
         * This method checks if the items current coordinates are valid and not blocked
         * if they're blocked it moves the item until it can be placed
         * 
         * returns true if could place item
         * 
         */
        public bool PlaceItem(ItemTile item)
        {
            // radius at which we're searching
            int r = 0;
            // set origin
            int ox = item.X;
            int oy = item.Y;

            // this loop is for increasing radius until we find a good tile
            while (true)
            {
                // searches the radius around origin
                for (int y = oy - r; y < oy + r + 1; y++)
                {
                    // within map bounds
                    if (y > 0 && y <= this.Height)
                    {
                        // searches the radius around origin
                        for (int x = ox - r; x < ox + r + 1; x++)
                        {
                            // within map bounds
                            if (x > 0 && x <= this.Width)
                            {
                                // if x or y is at the radius we're searching 
                                // so we don't search already searched tiles
                                if ((y == oy - r || y == oy + r) || (x == ox - r || x == ox + r))
                                {
                                    ITile tile = this.GetTile(x, y);
                                    if (tile == null)
                                    {
                                        item.X = x;
                                        item.Y = y;

                                        return true;
                                    }
                                    else if (tile is ItemTile)
                                    {
                                        if (((ItemTile)tile).ItemSlot.CanStack(item.ItemSlot))
                                        {
                                            item.X = x;
                                            item.Y = y;

                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                r++;
            }
        }

        /**
         * IsTileBlocked, checks if the tile at x,y is blocked
         * 
         * Blocked either by another character or warp/unwalkable tiles
         * 
         */
        public bool IsTileBlocked(ICharacter ignore, int x, int y)
        {
            // invalid coordinates
            if (x < 1 || x >= this.Width + 1 || y < 1 || y >= this.Height + 1) return true;

            Player ignorePlayer = ignore as Player;
            bool isgm = (ignorePlayer != null && ignorePlayer.Access == Player.AccessStatus.GameMaster);

            ITile tile = this.tiles[y * this.Width + x];
            if (tile != null)
            {
                if (tile is WarpTile)
                {
                    return true;
                }
                if (tile is BlockedTile)
                {
                    return !isgm;
                }
            }

            ICharacter character = this.GetCharacterAt(x, y);
            if (character == null || character == ignore || (isgm && ignorePlayer.IsGMInvisible)) return false;

            return true;
        }

        /**
         * LoadData, loads warp/blocked tiles, required items
         * 
         */
        public void LoadData(GameWorld world)
        {
            try
            {
                this.Script?.Object.OnLoad(this, world);
            }
            catch (Exception e) { }

            using (var fileStream = File.Open(@"Maps/" + FileName, FileMode.Open, FileAccess.Read))
            using (var mapReader = new BinaryReader(fileStream))
            {
                var version = mapReader.ReadInt16();
                var editorVersion = mapReader.ReadInt16();
                this.Width = mapReader.ReadInt32();
                this.Height = mapReader.ReadInt32();

                this.characters = new ICharacter[(this.Width + 1) * (this.Height + 1)];
                this.tiles = new ITile[(this.Width + 1) * (this.Height + 1)];

                for (int y = 1; y <= this.Height; y++)
                {
                    for (int x = 1; x <= this.Width; x++)
                    {
                        var flags = mapReader.ReadInt32();

                        for (int k = 0; k < 5; k++)
                        {
                            var graphic = mapReader.ReadInt32();
                            var sheet = mapReader.ReadInt16();

                            try
                            {
                                this.Script?.Object.OnLoadTile(this, x, y, k, graphic, sheet, flags, world);
                            }
                            catch (Exception e) { }
                        }

                        if ((flags & 2) > 0)
                        {
                            BlockedTile blocked = new BlockedTile();
                            this.tiles[y * this.Width + x] = blocked;
                        }
                    }
                }
            }

            SqlCommand command = new SqlCommand("SELECT * FROM warptiles " +
                                                "WHERE map_id=" + this.ID, world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                WarpTile warp = new WarpTile();
                warp.WarpMap = world.MapHandler.GetMap(Convert.ToInt32(reader["warp_id"]));
                warp.WarpX = Convert.ToInt32(reader["warp_x"]);
                warp.WarpY = Convert.ToInt32(reader["warp_y"]);

                int x = Convert.ToInt32(reader["map_x"]);
                int y = Convert.ToInt32(reader["map_y"]);

                this.tiles[y * this.Width + x] = warp;
            }

            reader.Close();

            command = new SqlCommand("SELECT * FROM map_required_items " +
                                                "WHERE map_id=" + this.ID, world.SqlConnection);
            reader = command.ExecuteReader();

            while (reader.Read())
            {
                this.requiredItems.Add(Convert.ToInt32(reader["item_template_id"]));
            }

            try
            {
                this.Script?.Object.OnFinishedLoad(this, world);
            }
            catch (Exception e) { }

            reader.Close();
        }

        /**
         * GetTile, returns the tile at x, y
         * 
         */
        public ITile GetTile(int x, int y)
        {
            // invalid coordinates
            if (x < 1 || x >= this.Width + 1 || y < 1 || y >= this.Height + 1) return null;

            return this.tiles[y * this.Width + x];
        }

        public void SetTile(int x, int y, ITile tile)
        {
            // invalid coordinates
            if (x < 1 || x >= this.Width + 1 || y < 1 || y >= this.Height + 1) return;

            this.tiles[y * this.Width + x] = tile;
        }

        /**
         * PlayerCanJoin, checks if player meets requirements to join map
         * 
         */
        public bool PlayerCanJoin(Player player, GameWorld world)
        {
            if (player.HasPrivilege(AccessPrivilege.IgnoreMapRequirements)) return true;

            if (this.MinLevel != 0 && player.Level < this.MinLevel)
            {
                return false;
            }
            if (this.MaxLevel != 0 && player.Level > this.MaxLevel)
            {
                return false;
            }
            if ((this.MinExperience != 0) &&
                (player.Experience + player.ExperienceSold < this.MinExperience))
            {
                return false;
            }
            if ((this.MaxExperience != 0) &&
                (player.Experience + player.ExperienceSold > this.MaxExperience))
            {
                return false;
            }

            foreach (int id in this.requiredItems)
            {
                if (!player.HasItem(id)) return false;
            }

            return true;
        }

        /**
         * Items, returns items list
         * 
         */
        public List<ItemTile> Items { get { return this.items; } }

        /**
         * GetCharacterAt, gets character at x,y
         * 
         */
        public ICharacter GetCharacterAt(int x, int y)
        {
            if (x < 1 || x >= this.Width + 1 || y < 1 || y >= this.Height + 1) return null;

            return this.characters[y * this.Width + x];
        }

        /**
         * Set Character at x,y to character
         */
        public void SetCharacter(ICharacter character, int x, int y)
        {
            if (x < 1 || x >= this.Width + 1 || y < 1 || y >= this.Height + 1) return;

            this.characters[y * this.Width + x] = character;
        }

        /**
         * NPCs, returns npcs list
         * 
         */
        public List<NPC> NPCs { get { return this.npcs; } }
    }
}
