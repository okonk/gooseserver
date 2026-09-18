using Goose.Testing;

namespace Goose.IntegrationTests;

public class CustomTicketScriptTests
{
    private const int TicketId = 823;

    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, Item Ticket) Setup()
    {
        var fixture = new TestWorldFixture(s => s.CustomTicketId = TicketId);
        var map = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var player = fixture.CommandPlayerOn(map, 5, 5);

        var template = fixture.AddBaseItemTemplate(TicketId, "Custom Ticket", ItemTemplate.UseTypes.OneTime);
        var shipped = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ItemScripts", "CustomTicket.csx"));
        template.Script = fixture.CompileItemScript(shipped, "CustomTicket.csx");

        var ticket = new Item();
        ticket.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(ticket, fixture.World);
        player.Inventory.AddItem(ticket, 1, fixture.World);

        return (fixture, player, ticket);
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
}
