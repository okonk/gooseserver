using Goose.Testing;

namespace Goose.IntegrationTests;

public class CustomTicketScriptTests
{
    private const int TicketId = 823;

    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, Item Ticket) Setup(
        int ticketId = TicketId)
    {
        var fixture = new TestWorldFixture(s => s.CustomTicketId = TicketId);
        var map = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var player = fixture.CommandPlayerOn(map, 5, 5);

        var template = fixture.AddBaseItemTemplate(ticketId, "Custom Ticket", ItemTemplate.UseTypes.OneTime);
        var shipped = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ItemScripts", "CustomTicket.csx"));
        template.Script = fixture.CompileItemScript(shipped, "CustomTicket.csx");

        var ticket = new Item();
        ticket.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(ticket, fixture.World);
        player.Inventory.AddItem(ticket, 1, fixture.World);

        return (fixture, player, ticket);
    }

    private static void AddEquipment(TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player,
                                     int templateId)
    {
        var template = fixture.AddBaseItemTemplate(templateId, "Sword " + templateId, ItemTemplate.UseTypes.Weapon);
        var item = new Item();
        item.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        player.Inventory.AddItem(item, 1, fixture.World);
    }

    [Fact]
    public void Using_the_ticket_opens_the_custom_window_and_keeps_the_ticket()
    {
        var (fixture, player, ticket) = Setup();
        using var _ = fixture;

        player.Inventory.UseConsumable(ticket, fixture.World);

        Assert.Contains(player.Sent, p => p.StartsWith("MKW") && p.Contains(",28,Custom,"));
        Assert.Same(ticket, player.Inventory.GetSlot(1)!.Item);
    }

    [Fact]
    public void A_second_use_refuses_and_sends_no_second_window()
    {
        var (fixture, player, ticket) = Setup();
        using var _ = fixture;

        player.Inventory.UseConsumable(ticket, fixture.World);
        player.Inventory.UseConsumable(ticket, fixture.World);

        Assert.Equal(1, player.Sent.Count(p => p.StartsWith("MKW")));
        Assert.Contains(player.Sent, p => p.Contains("You are already customising an item."));
        Assert.Same(ticket, player.Inventory.GetSlot(1)!.Item);
    }

    [Fact]
    public void A_cloned_ticket_template_opens_the_window_and_creates_the_custom()
    {
        var (fixture, player, ticket) = Setup(ticketId: 704);
        using var _ = fixture;

        AddEquipment(fixture, player, 900);
        AddEquipment(fixture, player, 901);

        player.Inventory.UseConsumable(ticket, fixture.World);

        Assert.Contains(player.Sent, p => p.StartsWith("MKW") && p.Contains(",28,Custom,"));
        Assert.Same(ticket, CustomWindow.FindOpen(player)!.TicketItem);

        Assert.True(fixture.RunCommand(player, "CWS2,3"));
        Assert.True(fixture.RunCommand(player, "CWC2,3,10,20,30,40,Clone Custom"));

        var result = player.Inventory.GetSlot(1)!;
        Assert.Equal("Clone Custom", result.Item.Name);
        Assert.Equal(901, result.Item.TemplateID);
        Assert.Equal(10, result.Item.GraphicR);
        Assert.Equal(40, result.Item.GraphicA);
        Assert.Null(player.Inventory.GetSlot(2));
        Assert.Null(player.Inventory.GetSlot(3));
        Assert.Contains(player.Sent, p => p.Contains("Created custom: Clone Custom"));
    }
}
