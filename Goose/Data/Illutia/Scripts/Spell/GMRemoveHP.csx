using Goose;
using Goose.Scripting;

public class GMRemoveHP : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
    {
        if (caster is not Player player) return false;
        if (target is not NPC npc)
        {
            world.Send(player, P.ServerMessage("GMRemoveHP only works on NPCs."));
            return false;
        }

        long amount = thisEffect.Stats.HP;
        if (amount <= 0) return false;

        long baseHP = npc.BaseStats.HP + npc.Class.GetLevel(npc.Level)!.BaseStats.HP;
        long trackedHP = npc.Buffs.Sum(buff => buff.SpellEffect.Stats.HP);
        long untrackedHP = Math.Max(0, npc.MaxStats.HP - baseHP - trackedHP);
        if (untrackedHP < amount)
        {
            world.Send(player, P.ServerMessage("This NPC does not have enough untracked HP to remove."));
            return false;
        }

        npc.MaxStats.HP -= amount;
        npc.CurrentHP = npc.CurrentHP;
        return true;
    }

    public override IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        yield return "Remove " + thisEffect.Stats.HP.ToString("N0") + " untracked HP from an NPC";
    }
}

return typeof(GMRemoveHP);
