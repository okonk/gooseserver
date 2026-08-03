using Goose;
using Xunit;

namespace Goose.Tests
{
    public class EventHandlerTests
    {
        [Fact]
        public void RegisterEvent_DoesNotReplaceRestrictedCommandWithOpenFactory()
        {
            var handler = new Goose.EventHandler();
            var factoryCalled = false;
            var player = new Player { Access = Player.AccessStatus.Normal };

            handler.RegisterEvent("/shutdown", (eventPlayer, data) =>
            {
                factoryCalled = true;
                return new StubEvent();
            });

            Assert.True(handler.AddEvent(player, "/shutdown"));
            Assert.False(factoryCalled);
        }

        private sealed class StubEvent : Goose.Event
        {
            public override void Ready(GameWorld world)
            {
            }
        }
    }
}
