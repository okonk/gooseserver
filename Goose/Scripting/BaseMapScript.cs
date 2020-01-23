using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class DynamicTile
    {
        public List<int> LayerInfo { get; set; }

        public bool Blocked { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public DynamicTile(int x, int y, int flags)
        {
            this.X = x;
            this.Y = y;
            this.Blocked = ((flags & 2) > 0);
            this.LayerInfo = new List<int>();
        }

        public string GetTUPPacket()
        {
            return string.Format("TUP{0},{1},{2},{3}", X, Y, string.Join(",", LayerInfo), (Blocked ? 2 : 0));
        }
    }

    public class MapScriptData
    {
        public Dictionary<int, DynamicTile> DynamicTiles { get; set; }

        public DynamicTile GetDynamicTile(int x, int y, int width)
        {
            DynamicTile tile = null;
            if (DynamicTiles.TryGetValue(y * width + x, out tile))
                return tile;

            return null;
        }

        public void SetDynamicTile(int x, int y, int width, DynamicTile tile)
        {
            DynamicTiles[y * width + x] = tile;
        }

        public MapScriptData()
        {
            this.DynamicTiles = new Dictionary<int, DynamicTile>();
        }
    }

    public class BaseMapScript : IMapScript
    {
        public BaseMapScript() { }

        public virtual void OnLoad(Map map, GameWorld world)
        {

        }

        public virtual void OnLoadTile(Map map, int x, int y, int layerNumber, int graphic, short sheet, int flags, GameWorld world)
        { 
}

        public virtual void OnFinishedLoad(Map map, GameWorld world)
        {

        }

        public virtual void OnPlayerEntered(Map map, Player player, GameWorld world)
        {

        }

        public virtual void OnPlayerLeft(Map map, Player player, GameWorld world)
        {

        }

        public virtual void OnPlayerMove(Map map, Player player, GameWorld world)
        {

        }

        public virtual void OnPlayerChatEvent(Map map, Player player, string message, GameWorld world)
        {

        }

        public virtual void OnNPCKilledEvent(Map map, NPC npc, ICharacter killer, GameWorld world)
        {

        }
        public virtual void OnNPCSpawnEvent(Map map, NPC npc, GameWorld world)
        {

        }

        public virtual void OnPetMove(Map map, Pet pet, GameWorld world)
        {

        }
    }
}
