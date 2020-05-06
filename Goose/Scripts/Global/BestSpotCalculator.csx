using System;
using System.Linq;
using System.Collections.Generic;
using Goose;
using Goose.Events;
using Goose.Scripting;

public class BestSpotCalculator : BaseGlobalScript
{
    class CalcData
    {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public Map map;
        public long combinedExp;
        public long exp1;
        public long exp2;

    }

    public enum Spell
    {
        OAA,
        RAB,
        AA
    }

    public void CalculateBestSpells(GameWorld world)
    {
        var map = world.MapHandler.Maps.FirstOrDefault(m => m.Name == "Savage Isle");
        int view_x = 16;
        int view_y = 14;

        int x = 40;
        int y = 58;

        var npcsInRange = (from p in map.NPCs
                    where Math.Abs(p.MapX - x) < view_x &&
                        Math.Abs(p.MapY - y) < view_y &&
                        p.Level == 50 && p.RespawnTime <= 60
                    select p).ToList<NPC>();

        Console.WriteLine($"NPCs in range: {npcsInRange.Count}");

        var oaa_pattern = new List<int[]>
        {
            new[]{-2, -2}, new[]{-1, -2}, new[]{0, -2}, new[]{1, -2}, new[]{2, -2},
            new[]{-2, -1}, new[]{-1, -1}, new[]{0, -1}, new[]{1, -1}, new[]{2, -1},
            new[]{-2, 0}, new[]{-1, 0}, new[]{0, 0}, new[]{1, 0}, new[]{2, 0},
            new[]{-2, 1}, new[]{-1, 1}, new[]{0, 1}, new[]{1, 1}, new[]{2, 1},
            new[]{-2, 2}, new[]{-1, 2}, new[]{0, 2}, new[]{1, 2}, new[]{2, 2}
        };

        var aa_pattern = new List<int[]>{ new[]{0, -1}, new[]{-1, 0}, new[]{0, 0}, new[]{1, 0}, new[]{0, 1} };

        var sequence = new List<Spell> { Spell.OAA, Spell.RAB, Spell.AA };
        int seqIndex = 0;

        while (npcsInRange.Count > 0)
        {
            Spell currentSpell = sequence[seqIndex];
            seqIndex = (seqIndex + 1) % sequence.Count;
            var currentPattern = aa_pattern;
            if (currentSpell == Spell.OAA)
                currentPattern = oaa_pattern;

            NPC bestTarget = null;
            List<NPC> bestKills = null;
            NPC leastOAATarget = null;
            List<NPC> leastOAAKills = null;

            foreach (var npc in npcsInRange)
            {
                var currentKills = GetKills(currentPattern, npc, npcsInRange);

                if (currentSpell != Spell.OAA)
                {
                    var oaaKills = GetKills(oaa_pattern, npc, npcsInRange);

                    if (oaaKills.Count > currentKills.Count)
                    {
                        if (leastOAAKills == null || oaaKills.Count < leastOAAKills.Count)
                        {
                            leastOAATarget = npc;
                            leastOAAKills = oaaKills;
                        }

                        continue;
                    }
                }

                if (bestTarget == null || bestKills.Count < currentKills.Count)
                {
                    bestTarget = npc;
                    bestKills = currentKills;
                }
            }

            if (bestTarget == null)
            {
                bestTarget = leastOAATarget;
                bestKills = leastOAAKills;
            }

            foreach (var npc in bestKills)
            {
                npcsInRange.Remove(npc);
            }

            Console.WriteLine($"{currentSpell} at {bestTarget.MapX},{bestTarget.MapY} hits {bestKills.Count}");
        }
    }

    public List<NPC> GetKills(List<int[]> pattern, NPC npc, List<NPC> npcsInRange)
    {
        var currentKills = new List<NPC>();

        foreach (var c in pattern)
        {
            int check_x = npc.MapX + c[0];
            int check_y = npc.MapY + c[1];

            var hitNpc = npcsInRange.FirstOrDefault(n => n.MapX == check_x && n.MapY == check_y);
            if (hitNpc != null)
            {
                currentKills.Add(hitNpc);
            }
        }

        return currentKills;
    }

	public override void OnLoaded(GameWorld world)
	{
        return;
        CalculateBestSpells(world);
        //OneScreen(world);
        return;

        //var map = world.MapHandler.Maps.FirstOrDefault(m => m.Name == "Winter Heights");
        int view_x = 16;
        int view_y = 14;

        var spots = new List<CalcData>();

        foreach (var map in world.MapHandler.Maps)
        {
            foreach (var npc in map.NPCs)
            {
                if (npc.Name == "Simon")
                    npc.RespawnTime = 20;
            }

            for (int y = view_y + 1; y <= map.Height; y++)
            {
                for (int x = view_x + 1; x <= map.Width; x++)
                {
                    if (map.GetTile(x, y) is BlockedTile) continue;

                    var range = (from p in map.NPCs
                                    where Math.Abs(p.MapX - x) < view_x &&
                                        Math.Abs(p.MapY - y) < view_y &&
                                        p.Level == 50 && p.RespawnTime <= 60
                                    select p).ToList<NPC>();

                    for (int y2 = y - view_y; y2 <= Math.Min(map.Height, y + view_y); y2++)
                    {
                        for (int x2 = x - view_x; x2 <= Math.Min(map.Width, x + view_x); x2++)
                        {
                            if (x == x2 && y == y2) continue;
                            if (map.GetTile(x2, y2) is BlockedTile) continue;

                            var range2 = (from p in map.NPCs
                                            where Math.Abs(p.MapX - x2) < view_x &&
                                                Math.Abs(p.MapY - y2) < view_y &&
                                                p.Level == 50 && p.RespawnTime <= 60
                                            select p).ToList<NPC>();

                            var common = range.Intersect(range2).ToList();
                            var only1 = range.Except(common).ToList();
                            var only2 = range2.Except(common).ToList();

                            long commonExp = common.Sum(x => (long)((x.Experience + 5000) * (60d / x.RespawnTime) * 60));
                            long only1Exp = only1.Sum(x => (long)((x.Experience + 5000) * (60d / x.RespawnTime) * 60));
                            long only2Exp = only2.Sum(x => (long)((x.Experience + 5000) * (60d / x.RespawnTime) * 60));

                            long exp = commonExp + only1Exp + only2Exp;
                            long exp1 = commonExp * 2 + only1Exp;
                            long exp2 = commonExp * 2 + only2Exp;

                            var data = new CalcData
                            {
                                x1 = x,
                                y1 = y,
                                x2 = x2,
                                y2 = y2,
                                map = map,
                                combinedExp = exp,
                                exp1 = exp1,
                                exp2 = exp2
                            };

                            spots.Add(data);
                        }
                    }
                }
            }
        }

        foreach (var spot in spots.OrderByDescending(s => s.combinedExp).GroupBy(s => s.map.Name).Select(x => x.First()).Take(10))
        {
            Console.WriteLine($"{spot.map.Name} P1: ({spot.x1},{spot.y1}) {spot.exp1} xp/hr P2: ({spot.x2},{spot.y2}) {spot.exp2} xp/hr");
        }
	}

	public void OneScreen(GameWorld world)
	{
        var map = world.MapHandler.Maps.FirstOrDefault(m => m.Name == "Savage Isle");
        int view_x = 16;
        int view_y = 14;

        var spots = new List<Tuple<int, int, long, int, Map>>();

        //foreach (var map in world.MapHandler.Maps)
        {
            foreach (var npc in map.NPCs)
            {
                if (npc.Name == "Simon")
                    npc.RespawnTime = 20;
            }

            for (int y = view_y + 1; y <= map.Height; y++)
            {
                for (int x = view_x + 1; x <= map.Width; x++)
                {
                    if (map.GetTile(x, y) is BlockedTile) continue;

                    var range = (from p in map.NPCs
                                    where Math.Abs(p.MapX - x) < view_x &&
                                        Math.Abs(p.MapY - y) < view_y &&
                                        p.Level == 50 && p.RespawnTime <= 60
                                    select p).ToList<NPC>();

                    spots.Add(Tuple.Create(x, y, range.Sum(x => (long)((x.Experience + 5000) * (60d / x.RespawnTime) * 60)), range.Count, map));
                }
            }
        }

        foreach (var spot in spots.OrderByDescending(s => s.Item3).GroupBy(s => s.Item5.Name).Select(x => x.Take(10)).FirstOrDefault())
        {
            Console.WriteLine("{4} {0}, {1}: {2} ({3})", spot.Item1, spot.Item2, spot.Item3, spot.Item4, spot.Item5.Name);
        }
	}
}

return typeof(BestSpotCalculator);