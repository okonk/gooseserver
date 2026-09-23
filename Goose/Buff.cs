using System.Text;

namespace Goose
{
    /**
     * Buff holds info about a buff on a player
     *
     */
    public class Buff
    {
        public ICharacter Caster { get; set; } = null!;
        public ICharacter Target { get; set; } = null!;
        public SpellEffect SpellEffect { get; set; } = null!;
        public long TimeCast { get; set; }
        public bool ItemBuff { get; set; }
        public Event? BuffExpireEvent { get; set; }

        public (long RemainingMs, long TotalMs) GetDurations(GameWorld world)
        {
            long totalMs = this.SpellEffect.Duration > 0 ? this.SpellEffect.Duration * 1000 : 0;
            long remainingMs = totalMs > 0
                ? Math.Max(0, Math.Min(totalMs, (this.TimeCast + this.SpellEffect.Duration * world.TimerFrequency - world.TimeNow) * 1000 / world.TimerFrequency))
                : 0;
            return (remainingMs, totalMs);
        }
    }
}
