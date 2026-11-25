using Goose.Events;

namespace Goose
{
    public class GroundZones
    {
        public const int MinTickMs = 100;

        private static int nextId;

        private readonly Map map;
        private readonly List<GroundZone> zones = [];
        private readonly Dictionary<(int X, int Y), GroundZone> owners = [];

        public GroundZones(Map map)
        {
            this.map = map;
        }

        public IReadOnlyList<GroundZone> All => this.zones;

        public GroundZone? OwnerAt(int x, int y) => this.owners.GetValueOrDefault((x, y));

        public bool IsActiveAt(GroundZone zone, int x, int y) => zone.Overlaps || this.OwnerAt(x, y) == zone;

        public GroundZone Create(SpellEffect effect, ICharacter caster, int x, int y, GameWorld world)
        {
            long now = world.TimeNow;
            var zone = new GroundZone
            {
                Id = ++nextId,
                Effect = effect,
                Caster = caster,
                Map = this.map,
                Tiles = SpellEffect.ShapeTiles(effect.TargetType, effect.TargetSize, x, y, caster.Facing)
                    .Where(t => t.X >= 1 && t.X <= this.map.Width && t.Y >= 1 && t.Y <= this.map.Height)
                    .ToHashSet(),
                CreatedAt = now,
                ExpiresAt = now + effect.Duration * world.TimerFrequency,
            };

            this.zones.Add(zone);
            if (!zone.Overlaps)
                foreach (var tile in zone.Tiles)
                    this.owners[tile] = zone;

            this.Apply(zone, world);
            this.Schedule(zone, now, world);
            return zone;
        }

        internal void Tick(GroundZone zone, long now, GameWorld world)
        {
            if (!this.zones.Contains(zone)) return;

            if (now >= zone.ExpiresAt)
            {
                this.Remove(zone);
                return;
            }

            this.Apply(zone, world);
            this.Schedule(zone, now, world);
        }

        private void Remove(GroundZone zone)
        {
            this.zones.Remove(zone);
            if (zone.Overlaps) return;

            foreach (var tile in zone.Tiles)
            {
                if (this.owners.GetValueOrDefault(tile) != zone) continue;

                var next = this.zones.LastOrDefault(z => !z.Overlaps && z.Tiles.Contains(tile));
                if (next is null) this.owners.Remove(tile);
                else this.owners[tile] = next;
            }
        }

        private void Apply(GroundZone zone, GameWorld world)
        {
            if (!zone.Effect.HasFormula) return;

            foreach (var (x, y) in zone.Tiles)
            {
                if (!this.IsActiveAt(zone, x, y)) continue;

                var target = this.map.GetCharacterAt(x, y);
                if (target is null || !IsAlive(target)) continue;
                if (!zone.Effect.CanCastSpell(zone.Caster, target, this.map)) continue;

                zone.Effect.ApplyFormula(zone.Caster, target, world, animate: false, out _, out _);
            }
        }

        private static bool IsAlive(ICharacter character) => character switch
        {
            Player player => player.State == Player.States.Ready && !player.IsDead,
            NPC npc => npc.State == NPC.States.Alive,
            _ => false,
        };

        private void Schedule(GroundZone zone, long now, GameWorld world)
        {
            long interval = Math.Max(MinTickMs, zone.Effect.ZoneTickMs) * world.TimerFrequency / 1000;
            var ev = new GroundZoneTickEvent { Data = zone };
            ev.Ticks = Math.Min(now + interval, zone.ExpiresAt);
            world.EventHandler.AddEvent(ev);
        }
    }
}
