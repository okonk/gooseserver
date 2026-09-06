namespace Goose.Tools.SchemaGen;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length is not (1 or 2))
        {
            Console.Error.WriteLine("usage: SchemaGen <output-path/schema.js> [output-path/schema.json]");
            return 1;
        }

        var model = SchemaModel.Build();

        if (!Write(args[0], SchemaJs.Render(model)))
            return 1;
        if (args.Length == 2 && !Write(args[1], SchemaJson.Render(model)))
            return 1;

        return 0;
    }

    private static bool Write(string path, string contents)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(full, contents);
            Console.WriteLine($"Wrote {full} ({new FileInfo(full).Length:N0} bytes)");
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Could not write '{path}': {e.GetBaseException().Message}");
            return false;
        }
    }
}
