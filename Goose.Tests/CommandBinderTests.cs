using System.Reflection;
using Goose;
using Goose.Commands;
using Goose.Testing;

namespace Goose.Tests;

public class CommandBinderTests
{
    private static TestWorldFixture NewFixture()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town");
        var player = fixture.CommandPlayerOn(map, 5, 5, "Alice");
        fixture.RegisterOnlinePlayer(player);
        return fixture;
    }

    private static Player Alice(TestWorldFixture fixture) => fixture.World.PlayerHandler.GetPlayer("Alice")!;

    private static ParameterInfo[] ParamsOf(string name)
        => typeof(CommandBinderTests).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters();

    private static void MPositional(CommandContext ctx, int a, long b, float c, double d, decimal e, string s) { }
    private static void MDefaultedInt(CommandContext ctx, int n = 7) { }
    private static void MRequiredInt(CommandContext ctx, int n) { }
    private static void MIntBad(CommandContext ctx, int n) { }
    private static void MDouble(CommandContext ctx, double d) { }
    private static void MDecimal(CommandContext ctx, decimal e) { }
    private static void MBool(CommandContext ctx, bool b) { }
    private static void MIntOnly(CommandContext ctx, int n) { }
    private static void MNoArgs(CommandContext ctx) { }
    private static void MSearch(CommandContext ctx, string command, string name, string[] query) { }
    private static void MRequiredTail(CommandContext ctx, string required, string[] message) { }
    private static void MOptionalString(CommandContext ctx, string a, string? b = null) { }
    private static void MWarpA(CommandContext ctx, int? mapId = null) { }
    private static void MWarpB(CommandContext ctx, int? mapx = null) { }
    private static void MWarpC(CommandContext ctx, int? mapy = null) { }
    private static void MStringTail(CommandContext ctx, string a, string[] rest) { }
    private static void MPlayer(CommandContext ctx, Player target) { }
    private static void MNullablePlayer(CommandContext ctx, Player? target = null) { }
    private static void MNullableInt(CommandContext ctx, int? mapId = null) { }
    private static void MNullableDecimal(CommandContext ctx, decimal? price = null) { }
    private static void MKick(CommandContext ctx, Player target) { }

    [Fact]
    public void Positional_scalars_bind()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MPositional)), ["1", "2", "3.5", "4.5", "5.5", "hi"], "Usage: /m");

        Assert.Null(error);
        Assert.Equal(1, args![1]);
        Assert.Equal(2L, args![2]);
        Assert.Equal(3.5f, args![3]);
        Assert.Equal(4.5, args![4]);
        Assert.Equal(5.5m, args![5]);
        Assert.Equal("hi", args![6]);
    }

    [Fact]
    public void Defaulted_int_missing_token_uses_default()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MDefaultedInt)), Array.Empty<string>(), "Usage: /m [n]");

        Assert.Null(error);
        Assert.Equal(7, args![1]);
    }

    [Fact]
    public void Required_int_missing_token_is_usage_error()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MRequiredInt)), Array.Empty<string>(), "Usage: /cmd <name>");

        Assert.Null(args);
        Assert.Equal("Usage: /cmd <name>", error);
    }

    [Fact]
    public void Int_token_that_does_not_parse_is_usage_error()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MIntBad)), ["abc"], "Usage: /cmd <n>");

        Assert.Null(args);
        Assert.Equal("Usage: /cmd <n>", error);
    }

    [Fact]
    public void Numeric_parsing_uses_invariant_culture()
    {
        using var fixture = NewFixture();
        var player = Alice(fixture);

        var (dArgs, dError) = CommandBinder.Bind(fixture.World, player, ParamsOf(nameof(MDouble)), ["1.5"], "Usage: /d <d>");
        Assert.Null(dError);
        Assert.Equal(1.5, dArgs![1]);

        var (dBadArgs, dBadError) = CommandBinder.Bind(fixture.World, player, ParamsOf(nameof(MDouble)), ["1,5"], "Usage: /d <d>");
        Assert.Null(dBadArgs);
        Assert.Equal("Usage: /d <d>", dBadError);

        var (eArgs, eError) = CommandBinder.Bind(fixture.World, player, ParamsOf(nameof(MDecimal)), ["1.5"], "Usage: /e <e>");
        Assert.Null(eError);
        Assert.Equal(1.5m, eArgs![1]);

        var (eBadArgs, eBadError) = CommandBinder.Bind(fixture.World, player, ParamsOf(nameof(MDecimal)), ["1,5"], "Usage: /e <e>");
        Assert.Null(eBadArgs);
        Assert.Equal("Usage: /e <e>", eBadError);
    }

    [Theory]
    [InlineData("on", true)]
    [InlineData("off", false)]
    [InlineData("true", true)]
    [InlineData("False", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void Bool_binds_from_accepted_tokens(string token, bool expected)
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MBool)), [token], "Usage: /m <b>");

        Assert.Null(error);
        Assert.Equal(expected, args![1]);
    }

    [Fact]
    public void Bool_rejects_unrecognized_token_with_usage_error()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MBool)), ["maybe"], "Usage: /m <b>");

        Assert.Null(args);
        Assert.Equal("Usage: /m <b>", error);
    }

    [Fact]
    public void Usage_strings_are_exact()
    {
        Assert.Equal("Usage: /testcmd <n>", CommandBinder.Usage("/testcmd", ParamsOf(nameof(MIntOnly))));
        Assert.Equal("Usage: /custom help", CommandBinder.Usage("/custom help", ParamsOf(nameof(MNoArgs))));
        Assert.Equal("Usage: /custom kill", CommandBinder.Usage("/custom kill", ParamsOf(nameof(MNoArgs))));
        Assert.Equal("Usage: /custom make", CommandBinder.Usage("/custom make", ParamsOf(nameof(MNoArgs))));
        Assert.Equal("Usage: /search <command> <name> [query...]", CommandBinder.Usage("/search", ParamsOf(nameof(MSearch))));
        Assert.Equal("Usage: /cmd <required> [message...]", CommandBinder.Usage("/cmd", ParamsOf(nameof(MRequiredTail))));
        Assert.Equal("Usage: /cmd <a> [b]", CommandBinder.Usage("/cmd", ParamsOf(nameof(MOptionalString))));
        Assert.Equal("Usage: /warp [mapId] [mapx] [mapy]", CommandBinder.Usage("/warp",
            new[]
            {
                ParamsOf(nameof(MWarpA))[1],
                ParamsOf(nameof(MWarpB))[1],
                ParamsOf(nameof(MWarpC))[1],
            }));
        Assert.Equal("Usage: /custom make <r> <g> <b> <a> <name...>",
            CommandBinder.Usage("/custom make", ParamsOf(nameof(MIntOnly)), "/custom make <r> <g> <b> <a> <name...>"));
    }

    [Fact]
    public void String_tail_captures_remaining_tokens()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MStringTail)), ["x", "y", "z"], "Usage: /m <a> [rest...]");

        Assert.Null(error);
        Assert.Equal("x", args![1]);
        Assert.Equal(new[] { "y", "z" }, args![2]);
    }

    [Fact]
    public void String_tail_with_no_tokens_binds_empty_array()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MStringTail)), ["x"], "Usage: /m <a> [rest...]");

        Assert.Null(error);
        Assert.NotNull(args![2]);
        Assert.Empty((string[])args[2]!);
    }

    [Fact]
    public void Player_parameter_resolves_online_player()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MPlayer)), ["Alice"], "Usage: /m <target>");

        Assert.Null(error);
        Assert.Same(Alice(fixture), args![1]);
    }

    [Fact]
    public void Player_parameter_with_unknown_name_is_specific_error()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MPlayer)), ["Nobody"], "Usage: /m <target>");

        Assert.Null(args);
        Assert.Equal("Couldn't find player Nobody.", error);
    }

    [Fact]
    public void Nullable_player_missing_token_binds_null()
    {
        using var fixture = NewFixture();

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MNullablePlayer)), Array.Empty<string>(), "Usage: /m [target]");

        Assert.Null(error);
        Assert.Null(args![1]);
    }

    [Fact]
    public void Nullable_value_types_bind()
    {
        using var fixture = NewFixture();
        var player = Alice(fixture);

        var (iMissing, iMissingError) = CommandBinder.Bind(fixture.World, player,
            ParamsOf(nameof(MNullableInt)), Array.Empty<string>(), "Usage: /i [mapId]");
        Assert.Null(iMissingError);
        Assert.Null(iMissing![1]);

        var (iArgs, iError) = CommandBinder.Bind(fixture.World, player,
            ParamsOf(nameof(MNullableInt)), ["5"], "Usage: /i [mapId]");
        Assert.Null(iError);
        Assert.Equal(5, iArgs![1]);

        var (iBadArgs, iBadError) = CommandBinder.Bind(fixture.World, player,
            ParamsOf(nameof(MNullableInt)), ["abc"], "Usage: /i [mapId]");
        Assert.Null(iBadArgs);
        Assert.Equal("Usage: /i [mapId]", iBadError);

        var (dMissing, dMissingError) = CommandBinder.Bind(fixture.World, player,
            ParamsOf(nameof(MNullableDecimal)), Array.Empty<string>(), "Usage: /d [price]");
        Assert.Null(dMissingError);
        Assert.Null(dMissing![1]);

        var (dArgs, dError) = CommandBinder.Bind(fixture.World, player,
            ParamsOf(nameof(MNullableDecimal)), ["1.5"], "Usage: /d [price]");
        Assert.Null(dError);
        Assert.Equal(1.5m, dArgs![1]);

        var (dBadArgs, dBadError) = CommandBinder.Bind(fixture.World, player,
            ParamsOf(nameof(MNullableDecimal)), ["abc"], "Usage: /d [price]");
        Assert.Null(dBadArgs);
        Assert.Equal("Usage: /d [price]", dBadError);
    }

    [Fact]
    public void Extra_tokens_beyond_parameters_are_ignored()
    {
        using var fixture = NewFixture();
        var bob = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(1)!, 6, 5, "Bob");
        fixture.RegisterOnlinePlayer(bob);

        var (args, error) = CommandBinder.Bind(fixture.World, Alice(fixture),
            ParamsOf(nameof(MKick)), ["Bob", "extra"], "Usage: /kick <target>");

        Assert.Null(error);
        Assert.Equal("Bob", ((Player)args![1]!).Name);
    }
}
