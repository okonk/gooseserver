using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.IntegrationTests;

public class PetNameTagScriptTests : IDisposable
{
    private const int ItemId = 902;

    private readonly TestWorldFixture fixture = new();
    private readonly string dbPath =
        Path.Combine(Path.GetTempPath(), "pet-name-tag-" + Guid.NewGuid().ToString("N") + ".db");
    private readonly Map map;
    private readonly TestWorldFixture.CapturingPlayer owner;
    private readonly Item tag;
    private readonly Pet pet;

    public PetNameTagScriptTests()
    {
        map = fixture.AddBaseMap(1, "Test");
        owner = fixture.CommandPlayerOn(map, 1, 1);
        owner.PlayerID = 42;
        map.AddPlayer(owner, fixture.World);

        fixture.World.Database.Start(dbPath);
        fixture.World.Database.Execute(conn =>
        {
            using var command = conn.CreateCommand();
            command.CommandText = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "sql", "pets.sql"));
            command.ExecuteNonQuery();
        });

        pet = new Pet
        {
            PetID = 1,
            Name = "Rex",
            Title = "",
            Surname = "",
            Level = 1,
            ClassID = 0,
            Class = fixture.World.ClassHandler.GetClass(0)!,
            BaseStats = new AttributeSet { HP = 1, Dexterity = -1 },
            MaxStats = new AttributeSet { HP = 1, Dexterity = -1 },
            EquippedItems = "",
            AutoCreatedNotSaved = true,
        };
        owner.AddPet(pet);
        pet.SaveToDatabase(fixture.World);
        fixture.World.Database.Execute(_ => { });

        var template = fixture.AddBaseItemTemplate(ItemId, "Name Tag", ItemTemplate.UseTypes.OneTime,
            t => t.StackSize = 1);
        var shipped = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ItemScripts", "PetNameTag.csx"));
        template.Script = fixture.CompileItemScript(shipped, "PetNameTag.csx");

        tag = new Item();
        tag.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(tag, fixture.World);
        owner.Inventory.AddItem(tag, 1, fixture.World);
    }

    private static OptionListWindow OptionListOf(TestWorldFixture.CapturingPlayer player)
    {
        return (OptionListWindow)Assert.Single(player.Windows, x => x.Type == Window.WindowTypes.OptionList);
    }

    private static TextInputWindow TextInputOf(TestWorldFixture.CapturingPlayer player)
    {
        return (TextInputWindow)Assert.Single(player.Windows, x => x.Type == Window.WindowTypes.TextInput);
    }

    [Fact]
    public void Using_the_tag_keeps_it_and_opens_the_pet_chooser()
    {
        using var _ = fixture;

        owner.Inventory.UseConsumable(tag, fixture.World);

        Assert.Equal(1, owner.Inventory.GetSlot(1)!.Stack);
        var window = OptionListOf(owner);
        Assert.Equal("Name Tag - choose a pet", window.Title);
    }

    [Fact]
    public void Choosing_a_pet_opens_the_text_input_with_the_current_name()
    {
        using var _ = fixture;

        owner.Inventory.UseConsumable(tag, fixture.World);
        OptionListOf(owner).LineClicked(0, 0, owner, fixture.World);

        var input = TextInputOf(owner);
        Assert.Equal("Name Tag", input.Title);
        var txi = owner.Sent.Single(s => s.StartsWith($"TXI{input.ID}")).TrimEnd('\x1');
        var fields = txi.Split('|');
        Assert.Equal(pet.Name, fields[3]);
    }

    [Fact]
    public void A_valid_name_renames_the_pet_consumes_the_tag_and_closes_the_window()
    {
        using var _ = fixture;

        owner.Inventory.UseConsumable(tag, fixture.World);
        OptionListOf(owner).LineClicked(0, 0, owner, fixture.World);
        var input = TextInputOf(owner);

        input.Submit("Barkley", owner, fixture.World);

        Assert.Equal("Barkley", pet.Name);
        Assert.Null(owner.Inventory.GetSlot(1));
        Assert.DoesNotContain(owner.Windows, x => x is TextInputWindow);
        Assert.Contains(owner.Sent, s => s.Contains("Rex is now known as Barkley."));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("bad name!!")]
    public void An_invalid_name_keeps_the_pet_the_tag_and_the_window(string name)
    {
        using var _ = fixture;

        owner.Inventory.UseConsumable(tag, fixture.World);
        OptionListOf(owner).LineClicked(0, 0, owner, fixture.World);
        var input = TextInputOf(owner);

        input.Submit(name, owner, fixture.World);

        Assert.Equal("Rex", pet.Name);
        Assert.Equal(1, owner.Inventory.GetSlot(1)!.Stack);
        Assert.Same(input, TextInputOf(owner));
        Assert.Contains(owner.Sent, s => s.Contains("Pet names must be 3-16 letters, with single spaces between words."));
    }

    [Fact]
    public void A_player_without_pets_is_told_there_is_nothing_to_name()
    {
        using var _ = fixture;

        var petless = fixture.CommandPlayerOn(map, 2, 2, "Petless");
        map.AddPlayer(petless, fixture.World);
        var theirTag = new Item();
        theirTag.LoadFromTemplate(tag.Template);
        fixture.World.ItemHandler.AddAndAssignId(theirTag, fixture.World);
        petless.Inventory.AddItem(theirTag, 1, fixture.World);

        petless.Inventory.UseConsumable(theirTag, fixture.World);

        Assert.Equal(1, petless.Inventory.GetSlot(1)!.Stack);
        Assert.Empty(petless.Windows);
        Assert.Contains(petless.Sent, s => s.Contains("You have no pets to name."));
    }

    [Fact]
    public void A_removed_tag_before_confirm_renames_nothing()
    {
        using var _ = fixture;

        owner.Inventory.UseConsumable(tag, fixture.World);
        OptionListOf(owner).LineClicked(0, 0, owner, fixture.World);
        var input = TextInputOf(owner);

        owner.Inventory.RemoveItem(tag, 1, fixture.World);
        input.Submit("Barkley", owner, fixture.World);

        Assert.Equal("Rex", pet.Name);
        Assert.DoesNotContain(owner.Windows, x => x is TextInputWindow);
        Assert.Contains(owner.Sent, s => s.Contains("Your name tag is gone."));
    }

    [Fact]
    public void Another_tag_stack_is_used_when_the_original_is_gone()
    {
        using var _ = fixture;

        var second = new Item();
        second.LoadFromTemplate(tag.Template);
        fixture.World.ItemHandler.AddAndAssignId(second, fixture.World);
        owner.Inventory.AddItem(second, 1, fixture.World);

        owner.Inventory.UseConsumable(tag, fixture.World);
        OptionListOf(owner).LineClicked(0, 0, owner, fixture.World);
        var input = TextInputOf(owner);

        owner.Inventory.RemoveItem(tag, 1, fixture.World);
        input.Submit("Barkley", owner, fixture.World);

        Assert.Equal("Barkley", pet.Name);
        Assert.Null(owner.Inventory.GetSlot(1));
        Assert.Null(owner.Inventory.GetSlot(2));
        Assert.DoesNotContain(owner.Windows, x => x is TextInputWindow);
        Assert.Contains(owner.Sent, s => s.Contains("Rex is now known as Barkley."));
    }

    public void Dispose()
    {
        fixture.World.Database.Stop();
        fixture.Dispose();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            string path = dbPath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
