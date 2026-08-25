using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        // Viewing ranges: the Godot client (1280x720, 32px tiles) sees 20 tiles out
        // left/right and ~11 up/down, so these must exceed that or objects pop in.
        public static int RANGE_X = 24;
        public static int RANGE_Y = 16;

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

        // Field block: when adding a field to Map, mirror it in CloneAs below or clones
        // will silently diverge from the maps they were copied from.
        public ICharacter[] characters;
        public ITile[] tiles;
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
        public Map()
        {
            this.players = new List<Player>();
            this.requiredItems = new List<int>();
            this.npcs = new List<NPC>();
            this.items = new List<ItemTile>();
        }

        /// <summary>Item template ids a player must carry to enter. Read-only - use
        /// AddRequiredItem to populate.</summary>
        public IReadOnlyList<int> RequiredItems { get { return this.requiredItems; } }

        public void AddRequiredItem(int itemTemplateId)
        {
            this.requiredItems.Add(itemTemplateId);
        }

        /// <summary>Copies this map as a new map at another id. Every setting comes across,
        /// including the private requiredItems list, which is why this lives here and not in a
        /// script. The clone gets its own occupancy state - characters, players, npcs, items -
        /// which is what keeps two copies of the same map independent.
        ///
        /// tiles is a shallow copy: the tile objects are shared. BlockedTile is a stateless
        /// marker; WarpTile is expected to be replaced by the caller if it should point somewhere
        /// else; ItemTile only ever appears at runtime, after loading.
        ///
        /// Deliberately not LoadData: that re-parses the .map file and issues two SQL queries
        /// keyed on the new id (Map.cs:466-520), which match no rows for a clone.</summary>
        public Map CloneAs(int id, string name)
        {
            var clone = new Map
            {
                ID = id,
                Name = name,
                FileName = this.FileName,
                Width = this.Width,
                Height = this.Height,
                MinLevel = this.MinLevel,
                MaxLevel = this.MaxLevel,
                MinExperience = this.MinExperience,
                MaxExperience = this.MaxExperience,
                CanPVP = this.CanPVP,
                CanChat = this.CanChat,
                CanAuction = this.CanAuction,
                CanShout = this.CanShout,
                CanCast = this.CanCast,
                CanBind = this.CanBind,
                CanUseItems = this.CanUseItems,
                CanSpawnPets = this.CanSpawnPets,
                Muted = this.Muted,
                Script = this.Script,
                ScriptParams = this.ScriptParams,
                tiles = (ITile[])this.tiles.Clone(),
                characters = new ICharacter[this.characters.Length],
            };

            clone.requiredItems.AddRange(this.requiredItems);
            return clone;
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

            world.SendToMap(this, P.MakeObject(item));
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

            world.SendToMap(this, P.EraseObject(item));
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

        public static Action<BinaryReader, Map, GameWorld> IllutiaMapLoader = (mapReader, map, world) =>
        {
            var version = mapReader.ReadInt16();
            var editorVersion = mapReader.ReadInt16();
            map.Width = mapReader.ReadInt32();
            map.Height = mapReader.ReadInt32();

            map.characters = new ICharacter[(map.Width + 1) * (map.Height + 1)];
            map.tiles = new ITile[(map.Width + 1) * (map.Height + 1)];

            for (int y = 1; y <= map.Height; y++)
            {
                for (int x = 1; x <= map.Width; x++)
                {
                    var flags = mapReader.ReadInt32();

                    for (int k = 0; k < 5; k++)
                    {
                        var graphic = mapReader.ReadInt32();
                        var sheet = mapReader.ReadInt16();

                        try
                        {
                            map.Script?.Object.OnLoadTile(map, x, y, k, graphic, sheet, flags, world);
                        }
                        catch (Exception e) { }
                    }

                    if ((flags & 2) > 0)
                    {
                        BlockedTile blocked = new BlockedTile();
                        map.tiles[y * map.Width + x] = blocked;
                    }
                }
            }
        };

        public static Action<BinaryReader, Map, GameWorld> AsperetaMapLoader = (mapReader, map, world) =>
        {
            var version = mapReader.ReadInt16();
            var editorVersion = mapReader.ReadInt16();
            map.Width = 100;
            map.Height = 100;

            map.characters = new ICharacter[(map.Width + 1) * (map.Height + 1)];
            map.tiles = new ITile[(map.Width + 1) * (map.Height + 1)];

            for (int y = 1; y <= map.Height; y++)
            {
                for (int x = 1; x <= map.Width; x++)
                {
                    byte flags = mapReader.ReadByte();

                    for (int k = 0; k < 4; k++)
                    {
                        var graphic = mapReader.ReadInt32();
                        short sheet = 0;

                        try
                        {
                            map.Script?.Object.OnLoadTile(map, x, y, k, graphic, sheet, flags, world);
                        }
                        catch (Exception e) { }
                    }

                    if ((flags & 1) > 0)
                    {
                        BlockedTile blocked = new BlockedTile();
                        map.tiles[y * map.Width + x] = blocked;
                    }
                }
            }
        };

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

            using (var fileStream = File.Open(world.Settings.DataPathAbsolute + "/Maps/" + FileName, FileMode.Open, FileAccess.Read))
            using (var mapReader = new BinaryReader(fileStream))
            {
                if (world.Settings.ServerType == "Illutia")
                    IllutiaMapLoader(mapReader, this, world);
                else
                    AsperetaMapLoader(mapReader, this, world);
            }

            int mapId = this.ID;
            world.Database.Execute(conn =>
            {
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM warptiles WHERE map_id=" + mapId;
                    using var reader = command.ExecuteReader();

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
                }

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM map_required_items WHERE map_id=" + mapId;
                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        this.AddRequiredItem(Convert.ToInt32(reader["item_template_id"]));
                    }
                }
            });

            try
            {
                this.Script?.Object.OnFinishedLoad(this, world);
            }
            catch (Exception e) { }
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

            string refusal = null;
            try
            {
                refusal = this.Script?.Object.CanPlayerJoin(this, player, world);
            }
            catch (Exception e)
            {
                // Fail CLOSED. This is an access-control gate, not a cosmetic hook: a broken gate
                // script must refuse rather than admit. The player gets a generic message; the
                // detail goes to the log, where someone can act on it.
                log.Error(e, "Map CanPlayerJoin {0} Exception", this.Name);
                refusal = "You cannot enter this map right now.";
            }
            if (refusal != null)
            {
                world.Send(player, "$7" + refusal);
                return false;
            }

            if (this.MinLevel != 0 && player.Level < this.MinLevel)
            {
                world.Send(player, $"$7You must be at least level {this.MinLevel} to enter this map.");
                return false;
            }
            if (this.MaxLevel != 0 && player.Level > this.MaxLevel)
            {
                world.Send(player, $"$7You must be at most level {this.MaxLevel} to enter this map.");
                return false;
            }
            if ((this.MinExperience != 0) &&
                (player.Experience + player.ExperienceSold < this.MinExperience))
            {
                world.Send(player, $"$7You must have at least {Utils.FormatNumber(this.MinExperience)} experience to enter this map.");
                return false;
            }
            if ((this.MaxExperience != 0) &&
                (player.Experience + player.ExperienceSold > this.MaxExperience))
            {
                world.Send(player, $"$7You must have at most {Utils.FormatNumber(this.MaxExperience)} experience to enter this map.");
                return false;
            }

            foreach (int id in this.requiredItems)
            {
                if (!player.HasItem(id))
                {
                    world.Send(player, $"$7You need '{world.ItemHandler.GetTemplate(id)?.Name ?? "<unknown item>"}' to enter this map.");
                    return false;
                }
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
