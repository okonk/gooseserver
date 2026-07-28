namespace Goose.Tools.SchemaGen;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: SchemaGen <output-path/schema.js>");
            return 1;
        }

        var model = SchemaModel.Build();
        var js = SchemaJs.Render(model);

        string path;
        try
        {
            path = Path.GetFullPath(args[0]);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, js);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Could not write '{args[0]}': {e.GetBaseException().Message}");
            return 1;
        }

        Console.WriteLine($"Wrote {path} ({new FileInfo(path).Length:N0} bytes, " +
                          $"{model.Sheets.Count} sheets)");
        return 0;
    }
}
