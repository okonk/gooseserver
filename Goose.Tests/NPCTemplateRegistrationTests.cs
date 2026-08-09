using Goose.Quests;

namespace Goose.Tests;

public class NPCTemplateRegistrationTests
{
    [Fact]
    public void Registered_templates_are_retrievable()
    {
        var handler = new NPCHandler();
        var template = new NPCTemplate { NPCTemplateID = 100162, Name = "King Terror (1)" };

        handler.AddTemplate(template);

        Assert.Same(template, handler.GetNPCTemplate(100162));
        Assert.Contains(template, handler.GetTemplates());
    }

    [Fact]
    public void Copy_constructor_copies_scalars_and_detaches_the_quest_list()
    {
        var quest = new Quest { Id = 1 };
        var original = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", WeaponDamage = 365, Level = 40 };
        original.Quests.Add(quest);

        var copy = new NPCTemplate(original) { NPCTemplateID = 100162 };

        Assert.Equal(100162, copy.NPCTemplateID);
        Assert.Equal("Shadow Dog", copy.Name);
        Assert.Equal(365, copy.WeaponDamage);
        Assert.Equal(40, copy.Level);

        // Detached: attaching a dimension quest must not touch the base template.
        copy.Quests.Add(new Quest { Id = 900001 });
        Assert.Single(original.Quests);
        Assert.Equal(2, copy.Quests.Count);
    }

    /// <summary>Allies are copied into a new list, but the entries still point at the
    /// templates the original allied with. Part 2 rewires them per dimension; this test
    /// pins the contract so that pass is written against something stated, not assumed.</summary>
    [Fact]
    public void Copy_constructor_detaches_the_ally_list_but_keeps_its_entries()
    {
        var ally = new NPCTemplate { NPCTemplateID = 5 };
        var original = new NPCTemplate { NPCTemplateID = 162, Allies = new List<NPCTemplate> { ally } };

        var copy = new NPCTemplate(original) { NPCTemplateID = 100162 };
        copy.Allies.Clear();

        Assert.Single(original.Allies);
        Assert.Same(ally, original.Allies[0]);
    }
}
