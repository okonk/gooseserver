using System;
using System.Collections.Generic;
using System.Linq;
using Goose;

public static class ArenaFloor
{
    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    private static List<(int X, int Y)>? floorTiles;
    private static Map? floorMap;
    private static string floorRect = "";
    private static HashSet<(int X, int Y)> floorSet = new();

    public static bool OnFloor(Map map, int[] rect, int x, int y)
    {
        Tiles(map, rect);
        return floorSet.Contains((x, y));
    }

    // The rect is the arena's bounding box; only tiles connected to its centre count, so the corners
    // of a non-rectangular floor that fall outside its walls are never used.
    public static List<(int X, int Y)> Tiles(Map map, int[] rect)
    {
        string key = string.Join(",", rect);
        if (floorTiles is not null && ReferenceEquals(floorMap, map) && floorRect == key) return floorTiles;
        int minX = rect[0], minY = rect[1], maxX = rect[0] + rect[2] - 1, maxY = rect[1] + rect[3] - 1;
        var start = (X: rect[0] + rect[2] / 2, Y: rect[1] + rect[3] / 2);
        var seen = new HashSet<(int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();
        if (!map.IsTileStaticBlocked(start.X, start.Y))
        {
            seen.Add(start);
            queue.Enqueue(start);
        }
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var next in new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) })
            {
                if (next.Item1 < minX || next.Item1 > maxX || next.Item2 < minY || next.Item2 > maxY) continue;
                if (map.IsTileStaticBlocked(next.Item1, next.Item2) || !seen.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        if (seen.Count == 0)
        {
            log.Error("Arena rect {0} centre is blocked; using the whole rect", string.Join(",", rect));
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    seen.Add((x, y));
        }
        floorSet = seen;
        floorTiles = seen.OrderBy(t => t.Y).ThenBy(t => t.X).ToList();
        floorMap = map;
        floorRect = key;
        return floorTiles;
    }
}
