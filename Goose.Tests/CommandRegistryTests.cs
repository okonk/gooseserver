using Goose;
using Goose.Commands;

namespace Goose.Tests;

public class CommandRegistryTests
{
    private static readonly Action<CommandContext> NoArgs = _ => { };

    private static void RestNotLast(CommandContext ctx, string[] rest, string tail) { }
    private static void NoCtx(string x) { }
    private static void UnsupportedType(CommandContext ctx, System.DateTime when) { }

    [Fact]
    public void Seed_empty_list_finds_nothing()
    {
        var registry = new CommandRegistry();
        registry.SeedAttributedTypes([]);

        Assert.False(registry.TryGet("/anything", out _));
        Assert.Empty(registry.Snapshot.Ordered);
    }

    [Fact]
    public void Seed_subcommand_only_class_is_discovered()
    {
        var registry = new CommandRegistry();
        registry.SeedAttributedTypes([typeof(SubOnlyCommand)]);

        Assert.True(registry.TryGet("/subonly", out var def));
        Assert.Equal("/subonly", def!.PrimaryKey);
        Assert.Null(def.ExecuteMethod);
        Assert.IsType<SubOnlyCommand>(def.Instance);
        Assert.Equal(2, def.Subcommands.Count);
        var names = def.Subcommands.Select(s => s.PrimaryName).ToArray();
        Assert.Contains("alpha", names);
        Assert.Contains("beta", names);
        var methods = def.Subcommands.Select(s => s.Method.Name).ToArray();
        Assert.Contains("Alpha", methods);
        Assert.Contains("Beta", methods);
        Assert.Contains(def.Subcommands, s => s.Parameters.Length == 2);
    }

    [Fact]
    public void Seed_two_execute_methods_rejected()
    {
        var registry = new CommandRegistry();
        registry.SeedAttributedTypes([typeof(TwoExecuteCommand)]);

        Assert.False(registry.TryGet("/twexec", out _));
    }

    [Fact]
    public void Seed_no_targets_rejected()
    {
        var registry = new CommandRegistry();
        registry.SeedAttributedTypes([typeof(NoTargetsCommand)]);

        Assert.False(registry.TryGet("/noexec", out _));
    }

    [Fact]
    public void Register_valid_key_is_resolvable()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/invite ", "General", "Invite a player", NoArgs));

        Assert.True(registry.TryGet("/invite ", out var def));
        Assert.Equal("/invite ", def!.PrimaryKey);
        Assert.Null(def.Privilege);
        Assert.Equal("Invite a player", def.Help);
        Assert.True(registry.Snapshot.Trie.ContainsKey("/invite "));
        Assert.True(registry.Snapshot.ByKey.ContainsKey("/invite "));
    }

    [Theory]
    [InlineData("badkey")]
    [InlineData("/bad key")]
    public void Register_invalid_key_refused(string key)
    {
        var registry = new CommandRegistry();
        Assert.False(registry.Register(key, "General", "help", NoArgs));
        Assert.False(registry.TryGet(key, out _));
    }

    [Fact]
    public void Register_restricted_to_open_refused()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/ban ", AccessPrivilege.Ban, "Admin", "Ban", NoArgs));
        Assert.False(registry.Register("/ban ", "Admin", "Ban open", NoArgs));

        Assert.True(registry.TryGet("/ban ", out var def));
        Assert.Equal(AccessPrivilege.Ban, def!.Privilege);
        Assert.Equal("Ban", def.Help);
    }

    [Fact]
    public void Register_restricted_to_other_restricted_replaces()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/warp ", AccessPrivilege.Ban, "Admin", "old", NoArgs));
        Assert.True(registry.Register("/warp ", AccessPrivilege.Warp, "Admin", "new", NoArgs));

        Assert.True(registry.TryGet("/warp ", out var def));
        Assert.Equal(AccessPrivilege.Warp, def!.Privilege);
        Assert.Equal("new", def.Help);
    }

    [Fact]
    public void RegisterKeys_keys_from_two_definitions_refused()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/a ", "S", "a", NoArgs));
        Assert.True(registry.Register("/b ", "S", "b", NoArgs));

        Assert.False(registry.RegisterKeys(["/a ", "/b "], null, "S", "c", NoArgs));
        Assert.True(registry.TryGet("/a ", out var a));
        Assert.True(registry.TryGet("/b ", out var b));
        Assert.Equal("a", a!.Help);
        Assert.Equal("b", b!.Help);
        Assert.Equal(2, registry.Snapshot.Ordered.Count);
    }

    [Fact]
    public void Multikey_replacement_frees_all_old_keys()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.RegisterKeys(["/invite ", "/groupadd "], null, "S", "a", NoArgs));
        Assert.True(registry.RegisterKeys(["/groupadd "], null, "S", "b", NoArgs));

        Assert.True(registry.TryGet("/groupadd ", out var def));
        Assert.Equal("b", def!.Help);
        Assert.False(registry.TryGet("/invite ", out _));
        Assert.False(registry.Snapshot.Trie.ContainsKey("/invite "));
        Assert.False(registry.Snapshot.ByKey.ContainsKey("/invite "));
    }

    [Fact]
    public void Multikey_registration_takes_occupied_and_new_keys()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/invite ", "S", "a", NoArgs));
        Assert.True(registry.RegisterKeys(["/invite ", "/groupadd "], null, "S", "b", NoArgs));

        Assert.True(registry.TryGet("/invite ", out var a));
        Assert.True(registry.TryGet("/groupadd ", out var b));
        Assert.Same(a, b);
        Assert.Equal("b", a!.Help);
        Assert.Single(registry.Snapshot.Ordered);
    }

    [Fact]
    public void Multikey_replacement_downgrade_protection()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.RegisterKeys(["/a "], AccessPrivilege.Ban, "S", "a", NoArgs));

        Assert.False(registry.RegisterKeys(["/a "], null, "S", "open", NoArgs));
        Assert.True(registry.TryGet("/a ", out var def));
        Assert.Equal(AccessPrivilege.Ban, def!.Privilege);

        Assert.True(registry.RegisterKeys(["/a "], AccessPrivilege.Warp, "S", "warp", NoArgs));
        Assert.True(registry.TryGet("/a ", out def));
        Assert.Equal(AccessPrivilege.Warp, def!.Privilege);

        Assert.True(registry.Register("/b ", "S", "open", NoArgs));
        Assert.True(registry.Register("/b ", AccessPrivilege.Kick, "S", "kick", NoArgs));
        Assert.True(registry.TryGet("/b ", out def));
        Assert.Equal(AccessPrivilege.Kick, def!.Privilege);
    }

    [Fact]
    public void Replacement_keeps_position_new_appends()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/x ", "S", "x", NoArgs));
        Assert.True(registry.Register("/y ", "S", "y", NoArgs));
        Assert.True(registry.Register("/w ", "S", "w", NoArgs));

        Assert.True(registry.Register("/y ", "S", "y2", NoArgs));

        Assert.Equal(["/x ", "/y ", "/w "], registry.Snapshot.Ordered.Select(d => d.PrimaryKey).ToArray());
    }

    [Fact]
    public void Register_same_key_replaces_in_place()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/solo ", "S", "one", NoArgs));
        Assert.True(registry.Register("/solo ", "S", "two", NoArgs));

        Assert.Single(registry.Snapshot.Ordered);
        Assert.True(registry.TryGet("/solo ", out var def));
        Assert.Equal("two", def!.Help);
        var section = Assert.Single(registry.Sections);
        Assert.Equal("S", section.Name);
        Assert.Single(section.Commands);
    }

    [Fact]
    public void Register_handler_rest_not_final_refused()
    {
        var registry = new CommandRegistry();
        Assert.False(registry.Register("/rest ", "S", "help", RestNotLast));
        Assert.False(registry.TryGet("/rest ", out _));
    }

    [Fact]
    public void Register_handler_without_context_first_refused()
    {
        var registry = new CommandRegistry();
        Assert.False(registry.Register("/noctx ", "S", "help", NoCtx));
        Assert.False(registry.TryGet("/noctx ", out _));
    }

    [Fact]
    public void Register_null_key_refused()
    {
        var registry = new CommandRegistry();
        Assert.False(registry.Register(null, "S", "help", NoArgs));
        Assert.Empty(registry.Snapshot.ByKey);
    }

    [Fact]
    public void Register_null_handler_refused()
    {
        var registry = new CommandRegistry();
        Assert.False(registry.Register("/x ", "S", "help", null));
        Assert.False(registry.TryGet("/x ", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Register_empty_help_refused(string help)
    {
        var registry = new CommandRegistry();
        Assert.False(registry.Register("/help ", "S", help, NoArgs));
        Assert.False(registry.TryGet("/help ", out _));
    }

    [Fact]
    public void RegisterKeys_empty_keys_refused()
    {
        var registry = new CommandRegistry();
        Assert.False(registry.RegisterKeys([], null, "S", "help", NoArgs));
        Assert.Empty(registry.Snapshot.ByKey);
    }

    [Fact]
    public void RegisterKeys_duplicate_keys_in_request_refused()
    {
        var registry = new CommandRegistry();
        Assert.False(registry.RegisterKeys(["/dup ", "/dup "], null, "S", "help", NoArgs));
        Assert.False(registry.TryGet("/dup ", out _));
        Assert.Empty(registry.Snapshot.ByKey);
    }

    [Fact]
    public void Register_handler_unsupported_type_refused()
    {
        var registry = new CommandRegistry();
        Assert.False(registry.Register("/unsup ", "S", "help", UnsupportedType));
        Assert.False(registry.TryGet("/unsup ", out _));
    }

    [Fact]
    public void Sections_group_and_preserve_order()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/a ", "Alpha", "a", NoArgs));
        Assert.True(registry.Register("/b ", "Beta", "b", NoArgs));
        Assert.True(registry.Register("/c ", "Alpha", "c", NoArgs));

        var sections = registry.Sections;
        Assert.Equal(["Alpha", "Beta"], sections.Select(s => s.Name).ToArray());
        Assert.Equal(["/a ", "/c "], sections[0].Commands.Select(d => d.PrimaryKey).ToArray());
        Assert.Equal(["/b "], sections[1].Commands.Select(d => d.PrimaryKey).ToArray());
    }

    [Fact]
    public void Concurrent_register_against_captured_snapshot()
    {
        var registry = new CommandRegistry();
        for (var i = 0; i < 200; i++)
            Assert.True(registry.Register($"/seed_{i} ", "S", "h", NoArgs));

        var snapshot = registry.Snapshot;
        Assert.Equal(200, snapshot.ByKey.Count);
        var errors = new List<Exception>();
        var gate = new object();

        var reader = new Thread(() =>
        {
            try
            {
                for (var i = 0; i < 20000; i++)
                {
                    foreach (var (key, def) in snapshot.ByKey)
                    {
                        if (!snapshot.Trie.TryGetValue(key, out var trieDef) || !ReferenceEquals(def, trieDef))
                            throw new InvalidOperationException("captured snapshot inconsistent");
                    }
                    foreach (var def in snapshot.Ordered)
                    {
                        if (!ReferenceEquals(snapshot.ByKey[def.PrimaryKey], def))
                            throw new InvalidOperationException("ordered/key mismatch");
                    }
                }
            }
            catch (Exception e)
            {
                lock (gate) errors.Add(e);
            }
        });

        var writers = Enumerable.Range(0, 8).Select(n => new Thread(() =>
        {
            try
            {
                for (var i = 0; i < 200; i++)
                    if (!registry.Register($"/t{n}_{i} ", "S", "h", NoArgs))
                        throw new InvalidOperationException("register failed");
            }
            catch (Exception e)
            {
                lock (gate) errors.Add(e);
            }
        })).ToArray();

        reader.Start();
        foreach (var writer in writers) writer.Start();
        foreach (var writer in writers) writer.Join();
        reader.Join();

        Assert.Empty(errors);

        var final = registry.Snapshot;
        foreach (var n in Enumerable.Range(0, 8))
            for (var i = 0; i < 200; i++)
                Assert.True(final.ByKey.ContainsKey($"/t{n}_{i} "), $"missing /t{n}_{i} ");
    }

    [Fact]
    public void Name_collision_section_then_command()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/alpha ", "General", "a", NoArgs));
        Assert.True(registry.Register("/general ", "Other", "g", NoArgs));

        Assert.Contains("General", registry.FindNameCollisions());
    }

    [Fact]
    public void Name_collision_command_then_section()
    {
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/general ", "Other", "g", NoArgs));
        Assert.Empty(registry.FindNameCollisions());

        Assert.True(registry.Register("/beta ", "General", "b", NoArgs));
        Assert.Contains("General", registry.FindNameCollisions());
    }

    [Fact]
    public void IsUsableBy_privilege_policy()
    {
        var open = new CommandDefinition(["/o "], null, "S", "h", null, null, NoArgs, null, [], null, null);
        var banned = new CommandDefinition(["/b "], AccessPrivilege.Ban, "S", "h", null, null, NoArgs, null, [], null, null);

        var normal = new Player(0) { Access = Player.AccessStatus.Normal };
        var gm = new Player(0) { Access = Player.AccessStatus.GameMaster };

        Assert.True(CommandRegistry.IsUsableBy(normal, open));
        Assert.True(CommandRegistry.IsUsableBy(gm, open));
        Assert.False(CommandRegistry.IsUsableBy(normal, banned));
        Assert.True(CommandRegistry.IsUsableBy(gm, banned));
    }
}

[Collection("NLog")]
public class CommandRegistryLoggingTests
{
    private static readonly Action<CommandContext> NoArgs = _ => { };

    [Fact]
    public void Seed_rejections_are_logged()
    {
        using var log = new CapturingLog();
        var registry = new CommandRegistry();
        registry.SeedAttributedTypes([typeof(TwoExecuteCommand), typeof(NoTargetsCommand)]);

        Assert.Contains(log.Messages, m => m.Contains("TwoExecute"));
        Assert.Contains(log.Messages, m => m.Contains("NoTargets"));
    }

    [Fact]
    public void Register_invalid_key_is_logged()
    {
        using var log = new CapturingLog();
        var registry = new CommandRegistry();
        registry.Register("badkey", "S", "help", NoArgs);
        registry.Register("/bad key", "S", "help", NoArgs);

        Assert.Contains(log.Messages, m => m.Contains("invalid key"));
    }

    [Fact]
    public void Seed_duplicate_key_rejected_with_type_logged()
    {
        using var log = new CapturingLog();
        var registry = new CommandRegistry();
        registry.SeedAttributedTypes([typeof(SubOnlyCommand)]);
        registry.SeedAttributedTypes([typeof(SubOnlyCommand)]);

        Assert.Contains(log.Messages, m => m.Contains("SubOnlyCommand") && m.Contains("/subonly"));
    }

    [Fact]
    public void Each_publish_logs_a_warning_per_collision()
    {
        using var log = new CapturingLog();
        var registry = new CommandRegistry();
        Assert.True(registry.Register("/alpha ", "General", "a", NoArgs));
        Assert.True(registry.Register("/general ", "Other", "g", NoArgs));

        Assert.Contains(log.Messages, m => m.Contains("General"));
    }
}

[Command("/subonly", Help = "Subcommand-only test command.")]
public sealed class SubOnlyCommand : BaseCommand
{
    [Subcommand("alpha")]
    public void Alpha(CommandContext ctx) { }

    [Subcommand("beta")]
    public void Beta(CommandContext ctx, string name) { }
}

[Command("/twexec")]
public sealed class TwoExecuteCommand : BaseCommand
{
    public void Execute(CommandContext ctx) { }

    public void Execute(CommandContext ctx, string name) { }
}

[Command("/noexec")]
public sealed class NoTargetsCommand : BaseCommand
{
}
