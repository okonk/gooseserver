using System.Reflection;
using System.Text.Json;
using Goose;
using Goose.Scripting;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class WeeperShippedConfigTests
{
    [Fact]
    public void Shipped_halloween_json_passes_validation()
    {
        var kit = new HalloweenTestKit();
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Goose", "Data", "Illutia",
                "Scripts", "Global", "Halloween", "halloween.json");
            if (File.Exists(candidate))
            {
                File.Copy(candidate, kit.ConfigPath, true);
                File.SetLastWriteTimeUtc(kit.ConfigPath, File.GetLastWriteTimeUtc(kit.ConfigPath).AddSeconds(1));
                break;
            }
            directory = directory.Parent;
        }
        var cfg = kit.Host.GetType().GetMethod("Config")!.Invoke(kit.Host, [kit.World])!;
        var weeper = cfg.GetType().GetProperty("Weeper")!.GetValue(cfg)!;
        var weeperType = weeper.GetType();
        var loops = (System.Collections.IDictionary)weeperType.GetProperty("Loops")!.GetValue(weeper)!;
        Assert.Equal(3, loops.Count);
        Assert.Equal(0.12, (double)weeperType.GetProperty("HitPercent")!.GetValue(weeper)!);
        Assert.Equal(2, ((int[])weeperType.GetProperty("Start")!.GetValue(weeper)!).Length);
        kit.Dispose();
    }

    [Theory]
    [InlineData(43, 53, "Foyer")]
    [InlineData(32, 64, "Foyer")]
    [InlineData(80, 12, "Master Bedroom")]
    [InlineData(50, 12, "Kids' Bedroom")]
    [InlineData(80, 45, "Ballroom")]
    [InlineData(70, 80, "Garden")]
    [InlineData(40, 35, "Statue Gallery")]
    [InlineData(12, 55, "Kitchen")]
    public void Shipped_rooms_are_x_y_width_height(int x, int y, string room)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ShippedConfigPath()));
        var matches = doc.RootElement.GetProperty("rooms").EnumerateArray()
            .Where(r =>
            {
                var rect = r.GetProperty("rect").EnumerateArray().Select(e => e.GetInt32()).ToArray();
                return x >= rect[0] && x < rect[0] + rect[2] && y >= rect[1] && y < rect[1] + rect[3];
            })
            .Select(r => r.GetProperty("name").GetString())
            .ToList();

        Assert.Equal([room], matches);
    }

    private static string ShippedConfigPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Goose", "Data", "Illutia",
                "Scripts", "Global", "Halloween", "halloween.json");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("halloween.json");
    }
}
