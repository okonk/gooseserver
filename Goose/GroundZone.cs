namespace Goose
{
    public class GroundZone
    {
        public int Id { get; init; }
        public SpellEffect Effect { get; init; } = null!;
        public ICharacter Caster { get; init; } = null!;
        public Map Map { get; init; } = null!;
        public HashSet<(int X, int Y)> Tiles { get; init; } = [];
        public long CreatedAt { get; init; }
        public long ExpiresAt { get; init; }

        public bool Overlaps { get; init; }
    }
}
