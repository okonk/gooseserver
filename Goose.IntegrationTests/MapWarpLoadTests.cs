namespace Goose.IntegrationTests;

public class MapWarpLoadTests : PlayerFirstSaveTestBase
{
    private readonly string dataPath =
        Path.Combine(Path.GetTempPath(), "mapwarp-" + Guid.NewGuid().ToString("N") + ".dir");

    public MapWarpLoadTests() : base("maps", "warptiles")
    {
        // Code-built settings leave ServerType null, which routes LoadData to
        // AsperetaMapLoader (Goose/Map.cs); the map files below are Illutia format.
        world.Settings.ServerType = "Illutia";
        world.Settings.DataPath = dataPath;
        Directory.CreateDirectory(Path.Combine(dataPath, "Maps"));
        WriteIllutiaMap(Path.Combine(dataPath, "Maps", "Map1.map"), 5, 5);
        WriteIllutiaMap(Path.Combine(dataPath, "Maps", "Map2.map"), 5, 5);

        world.Database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO warptiles (map_id, map_x, map_y, warp_id, warp_x, warp_y) VALUES (1, 3, 3, 999, 1, 1), (1, 4, 4, 2, 1, 1)";
            cmd.ExecuteNonQuery();
        });
    }

    [Fact]
    public void LoadMaps_UnknownWarpTarget_SkipsTileWithoutThrowing()
    {
        world.MapHandler.LoadMaps(world);

        Assert.Null(world.MapHandler.GetMap(1)!.GetTile(3, 3));
        Assert.IsType<WarpTile>(world.MapHandler.GetMap(1)!.GetTile(4, 4));
        Assert.Equal(2, ((WarpTile)world.MapHandler.GetMap(1)!.GetTile(4, 4)!).WarpMap.ID);

        Directory.Delete(dataPath, recursive: true);
    }

    private static void WriteIllutiaMap(string path, int width, int height)
    {
        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(width);
        w.Write(height);
        for (int y = 1; y <= height; y++)
        {
            for (int x = 1; x <= width; x++)
            {
                w.Write(0);
                for (int k = 0; k < 5; k++)
                {
                    w.Write(0);
                    w.Write((short)0);
                }
            }
        }
    }
}
