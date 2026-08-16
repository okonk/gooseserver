using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

public partial class Dimensions
{

    // ---- Spell pass -------------------------------------------------------

    /// <summary>Validates every id the spell pass depends on BEFORE anything is registered.
    ///
    /// Two reasons this is a preflight rather than a check inside each clone loop. First, a bad
    /// id found halfway through leaves the handler half-mutated - thousands of generated effects
    /// registered, spells not, cross-references unwired - which is a worse thing to hand someone
    /// than a clean refusal. Second, "base" means ID &lt; Offset everywhere downstream
    /// (RewireSpellEffects filters on exactly that), so a base id at or above the offset is not
    /// merely a collision risk: it would be cloned here and then skipped during rewiring, and
    /// the result would be a spell that exists but never stacks or resolves correctly.</summary>
    private void PreflightSpellIds(GameWorld world)
    {
        var effects = world.SpellHandler.GetSpellEffects().ToList();
        var spells = world.SpellHandler.GetSpells().ToList();

        foreach (var effect in effects)
            if (effect.ID < 0 || effect.ID >= Offset)
                throw new Exception($"Spell effect id {effect.ID} is outside the base range "
                    + $"0..{Offset - 1}. Dimension cloning keys on id + {Offset} * dimension, so "
                    + "every sheet id must fit below the offset. Raise Offset or fix the data.");

        foreach (var spell in spells)
            if (spell.ID < 0 || spell.ID >= Offset)
                throw new Exception($"Spell id {spell.ID} is outside the base range "
                    + $"0..{Offset - 1}. Dimension cloning keys on id + {Offset} * dimension, so "
                    + "every sheet id must fit below the offset. Raise Offset or fix the data.");

        // Backstop. Unreachable once the range checks above pass - id + Offset*dim is injective
        // over 0..Offset-1 x 1..DimensionCount - but AddSpell/AddSpellEffect overwrite silently,
        // so the failure this would catch is a real spell vanishing without a trace.
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var effect in effects)
                if (world.SpellHandler.GetSpellEffect(effect.ID + Offset * dim) != null)
                    throw new Exception($"Dimension spell effect id {effect.ID + Offset * dim} "
                        + $"(base {effect.ID}, dim {dim}) already exists.");

            foreach (var spell in spells)
                if (world.SpellHandler.GetSpell(spell.ID + Offset * dim) != null)
                    throw new Exception($"Dimension spell id {spell.ID + Offset * dim} "
                        + $"(base {spell.ID}, dim {dim}) already exists.");
        }
    }

    /// <summary>Dimension name prefixes. SpellHandler.java:235.</summary>
    private static readonly string[] DimensionPrefixes =
    {
        "", "Powerful ", "Super Powerful ", "Supreme ", "Omnipotent ", "Almighty ", "Godly ",
    };

    private string PrefixFor(int dim)
    {
        return dim >= 0 && dim < DimensionPrefixes.Length ? DimensionPrefixes[dim] : "";
    }

    /// <summary>SpellHandler.java:226.</summary>
    private string DescriptionPrefixFor(int dim)
    {
        return dim > 0 ? "Abyss (" + dim + ") " : "";
    }

    /// <summary>Step 1 of the spell pass: one scaled copy of every effect per dimension.
    /// Cross-references are left pointing at dimension-0 effects here and rewired by
    /// RewireSpellEffects - clone order is dictionary order, so a referenced effect's clone
    /// may not exist yet at the moment this runs.</summary>
    private void CloneSpellEffects(GameWorld world)
    {
        // Snapshot: AddSpellEffect mutates the dictionary GetSpellEffects() enumerates.
        var baseEffects = world.SpellHandler.GetSpellEffects().ToList();

        // No collision guard here - PreflightSpellIds already proved every id in this loop is
        // free, before the first registration. Do not re-add one: a throw from inside this loop
        // is exactly the half-mutated handler the preflight exists to avoid.
        for (int dim = 1; dim <= DimensionCount; dim++)
            foreach (var basic in baseEffects)
                world.SpellHandler.AddSpellEffect(ScaleSpellEffect(basic, dim));
    }

    /// <summary>Step 2 of the spell pass. Two jobs, both of which need every clone to exist:
    /// repoint each clone's melee-reaction references at its own dimension, and build the
    /// dimension ladder on the buff stacking lists.</summary>
    private void RewireSpellEffects(GameWorld world)
    {
        var baseEffects = world.SpellHandler.GetSpellEffects()
                               .Where(e => e.ID < Offset).ToList();

        // Snapshot the base lists before touching anything: the dim-0 pass below rewrites the
        // base effect's own BuffDoesntStackOver, and later dimensions must still read the
        // original list.
        var baseStacksOver = baseEffects.ToDictionary(e => e.ID, e => e.BuffStacksOver.ToList());
        var baseDoesntStackOver = baseEffects.ToDictionary(e => e.ID, e => e.BuffDoesntStackOver.ToList());

        // Melee reactions: dimension copies only. The base effect keeps what it loaded.
        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseEffects)
            {
                var clone = world.SpellHandler.GetSpellEffect(basic.ID + Offset * dim);
                if (clone == null) continue;

                // A reference with no clone is dropped, not left pointing across dimensions -
                // same rule RewireAllies applies to NPC allies.
                clone.OnMeleeAttackSpell = world.SpellHandler.GetSpellEffect(
                    basic.OnMeleeAttackSpellID + Offset * dim);
                clone.OnMeleeAttackSpellID = clone.OnMeleeAttackSpell == null
                    ? 0 : clone.OnMeleeAttackSpell.ID;

                clone.OnMeleeHitSpell = world.SpellHandler.GetSpellEffect(
                    basic.OnMeleeHitSpellID + Offset * dim);
                clone.OnMeleeHitSpellID = clone.OnMeleeHitSpell == null
                    ? 0 : clone.OnMeleeHitSpell.ID;
            }
        }

        // The stacking ladder covers dimension 0 as well.
        for (int dim = 0; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseEffects)
            {
                var effect = world.SpellHandler.GetSpellEffect(basic.ID + Offset * dim);
                if (effect == null) continue;

                // Everything this effect supersedes, plus itself. Split by dimension: copies at
                // or below this one are stacked over, copies above it refuse the cast. Splitting
                // the SAME set both ways is what guarantees no copy lands in neither list.
                var superseded = baseStacksOver[basic.ID].Concat(new[] { basic }).ToList();

                var stacks = new List<SpellEffect>();
                foreach (var entry in superseded)
                    for (int k = 0; k <= dim; k++)
                        AddEffectIfPresent(world, stacks, entry.ID + Offset * k);

                var doesnt = new List<SpellEffect>();

                // Explicit "never stacks" entries lose at every dimension, both directions.
                foreach (var entry in baseDoesntStackOver[basic.ID])
                    for (int k = 0; k <= DimensionCount; k++)
                        AddEffectIfPresent(world, doesnt, entry.ID + Offset * k);

                // And the upper half of the ladder. This covers the whole superseded set, not
                // just the effect itself: a dim-3 Bless meeting a dim-5 MINOR Bless is in neither
                // list otherwise, and both stat blocks apply at once.
                foreach (var entry in superseded)
                    for (int k = dim + 1; k <= DimensionCount; k++)
                        AddEffectIfPresent(world, doesnt, entry.ID + Offset * k);

                effect.BuffStacksOver = stacks;
                effect.BuffDoesntStackOver = doesnt;

                // Keep the string forms consistent. Nothing re-parses them after load, but a
                // divergent string is a trap for anyone debugging from a dump - same reasoning
                // as AlliesString in RewireAllies.
                effect.BuffStacksOverString = string.Join(" ", stacks.Select(e => e.ID));
                effect.BuffDoesntStackOverString = string.Join(" ", doesnt.Select(e => e.ID));
            }
        }
    }

    private void AddEffectIfPresent(GameWorld world, List<SpellEffect> into, int id)
    {
        var effect = world.SpellHandler.GetSpellEffect(id);
        if (effect != null && !into.Contains(effect)) into.Add(effect);
    }

    /// <summary>Step 3 of the spell pass. Runs after RewireSpellEffects so every effect clone
    /// exists and is fully wired before a spell points at one.</summary>
    private void CloneSpells(GameWorld world)
    {
        var baseSpells = world.SpellHandler.GetSpells().ToList();

        for (int dim = 1; dim <= DimensionCount; dim++)
        {
            foreach (var basic in baseSpells)
            {
                int id = basic.ID + Offset * dim;

                if (world.SpellHandler.GetSpell(id) != null)
                    throw new Exception($"Dimension spell id {id} (base {basic.ID}, dim {dim}) "
                                        + "already exists. Offset is too small for this data set.");

                var effect = world.SpellHandler.GetSpellEffect(basic.SpellEffectID + Offset * dim);

                // LoadSpells drops a spell whose effect is missing (SpellHandler.cs:250); do
                // the same rather than registering a spell that cannot be cast.
                if (effect == null) continue;

                world.SpellHandler.AddSpell(new Spell(basic)
                {
                    ID = id,
                    Name = PrefixFor(dim) + basic.Name,
                    Description = DescriptionPrefixFor(dim) + basic.Description,
                    Aether = (long)(basic.Aether * Math.Pow(0.9, dim)),          // SpellHandler.java:279
                    HPStaticCost = (int)(basic.HPStaticCost * Math.Pow(3, dim)), // :280
                    MPStaticCost = (int)(basic.MPStaticCost * Math.Pow(3, dim)), // :281
                    SpellEffectID = effect.ID,
                    SpellEffect = effect,
                });
            }
        }
    }

    /// <summary>Step 4 of the spell pass, and the last thing the spell work does. Every
    /// teleport effect - dimension 0 included - becomes a script effect so its destination
    /// resolves in the caster's dimension.
    ///
    /// Dimension 0 is deliberate: class level-up spells stay at dimension 0, so that copy is
    /// the teleport every player actually holds. Skipping it would leave a way out of any
    /// dimension.
    ///
    /// Runs after CloneSpellEffects so the clones were still Teleport-typed when they were
    /// copied, and one pass here converts base and clones together.</summary>
    private void RewriteTeleportEffects(GameWorld world)
    {
        var script = world.ScriptHandler.GetScript<ISpellEffectScript>("Scripts/Global/Dimensions/DimensionTeleport.csx");

        foreach (var effect in world.SpellHandler.GetSpellEffects().ToList())
        {
            if (effect.EffectType != SpellEffect.EffectTypes.Teleport) continue;

            effect.EffectType = SpellEffect.EffectTypes.Script;
            effect.Script = script;
            effect.ScriptParams = Offset.ToString();
        }
    }

    /// <summary>SpellHandler.java:288-330, applied in abyss's order - the formula wrap reads
    /// TargetType before the shape morph rewrites it.</summary>
    private SpellEffect ScaleSpellEffect(SpellEffect basic, int dim)
    {
        var clone = new SpellEffect(basic)
        {
            ID = basic.ID + Offset * dim,
            Name = PrefixFor(dim) + basic.Name,
            Duration = (long)(basic.Duration * Math.Pow(1.15, dim)),
            TargetSize = basic.TargetSize + dim,
        };

        // SpellHandler.java:290-294
        clone.MinimumLevelEffected =
            (basic.EffectType == SpellEffect.EffectTypes.Buff ||
             basic.EffectType == SpellEffect.EffectTypes.Permanent) ? 50 : 1;

        // SpellHandler.java:298
        if (basic.TauntAggro > 0)
            clone.TauntAggro = (long)(basic.TauntAggro * Math.Pow(3, dim) + 100000 * Math.Pow(20, dim));

        ScaleBuffStats(clone.Stats, dim);

        // SpellHandler.java:307-308, then :310-328. Order matters: targetScale comes from the
        // ORIGINAL target type, before the morph below rewrites it.
        clone.HPFormula = ScaleFormula(basic.HPFormula, basic.TargetType, dim);
        clone.MPFormula = ScaleFormula(basic.MPFormula, basic.TargetType, dim);

        MorphTargetShape(clone, basic.TargetType, basic.TargetSize, dim);

        return clone;
    }

    /// <summary>AttributeSet.java:347. The set is already a clone (SpellEffect copy
    /// constructor), so fields abyss omits keep their base value instead of being zeroed -
    /// notably MoveSpeed and SP. Deliberate deviation, see the design doc.</summary>
    private void ScaleBuffStats(AttributeSet stats, int dim)
    {
        decimal linear = 1m + 0.5m * dim;

        stats.HP = stats.HP * (dim + 1) * (dim + 1);
        stats.MP = stats.MP * (dim + 1) * (dim + 1);

        stats.HPStaticRegen = (int)(stats.HPStaticRegen * Math.Pow(4, dim));
        stats.MPStaticRegen = (int)(stats.MPStaticRegen * Math.Pow(4, dim));

        stats.AC = (int)(stats.AC * linear);
        stats.DamageReduction *= linear;
        stats.Haste *= linear;
        stats.HPPercentRegen *= linear;
        stats.MPPercentRegen *= linear;
        stats.MeleeCrit *= linear;
        stats.MeleeDamage *= linear;
        stats.SpellCrit *= linear;
        stats.SpellDamage *= linear;

        stats.FireResist *= dim;
        stats.AirResist *= dim;
        stats.EarthResist *= dim;
        stats.WaterResist *= dim;
        stats.SpiritResist *= dim;
        stats.Strength *= dim;
        stats.Stamina *= dim;
        stats.Intelligence *= dim;
        stats.Dexterity *= dim;
    }

    /// <summary>SpellHandler.java:260. Single-target spells get an extra 1.15.
    ///
    /// InvariantCulture is required: ParseFormula reads literals with Convert.ToDecimal and no
    /// format provider (SpellEffect.cs:1311), and shipped sheet data already uses '.' as the
    /// separator ("0.10 * %ccmp"), so '.' is the convention the parser is fed everywhere.</summary>
    private string ScaleFormula(string formula, SpellEffect.TargetTypes targetType, int dim)
    {
        if (string.IsNullOrEmpty(formula)) return formula;

        double targetScale = targetType == SpellEffect.TargetTypes.Target ? 1.15 : 1.0;
        double multiplier = targetScale * Math.Pow(1.25, dim);

        return "(" + formula + ") * "
               + multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>SpellHandler.java:310-328. Small shapes grow into bigger ones.</summary>
    private void MorphTargetShape(SpellEffect clone, SpellEffect.TargetTypes targetType, int baseSize, int dim)
    {
        if (targetType == SpellEffect.TargetTypes.Cross || targetType == SpellEffect.TargetTypes.Plus)
        {
            clone.TargetSize = dim;
            clone.TargetType = SpellEffect.TargetTypes.Area;
        }
        else if (targetType == SpellEffect.TargetTypes.LineFront)
        {
            clone.TargetSize = baseSize == 3 ? dim + 1 : dim;
            clone.TargetType = baseSize <= 1
                ? SpellEffect.TargetTypes.Plus
                : SpellEffect.TargetTypes.TriangleFront;
        }
    }
}
