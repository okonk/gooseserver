using Goose;
using Goose.Scripting;

public class GMAddHP : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
    {
        if (caster is not Player player) return false;
        if (target is not NPC npc)
        {
            world.Send(player, P.ServerMessage("GMAddHP only works on NPCs."));
            return false;
        }

        long amount = thisEffect.Stats.HP;
        if (amount == 0) return false;

        npc.MaxStats.HP += amount;
        npc.CurrentHP += amount;

        return true;
    }

    public override IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        yield return "Increase Maximum and current HP by " + thisEffect.Stats.HP.ToString("N0");
    }
}

return typeof(GMAddHP);
