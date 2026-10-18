using System.Reflection;
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
        Assert.Equal(0.08, (double)weeperType.GetProperty("HitPercent")!.GetValue(weeper)!);
        Assert.Equal(2, ((int[])weeperType.GetProperty("Start")!.GetValue(weeper)!).Length);
        kit.Dispose();
    }
}
