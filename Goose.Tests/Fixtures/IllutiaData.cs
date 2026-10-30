namespace Goose.Tests.Fixtures;

public static class IllutiaData
{
    public static string DataDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "Data", "Illutia"),
                Path.Combine(dir.FullName, "Goose", "Data", "Illutia"),
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate Data/Illutia from " + AppContext.BaseDirectory);
    }
}
