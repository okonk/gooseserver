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

        var path = Path.GetFullPath(args[0]);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, js);

        Console.WriteLine($"Wrote {args[0]} ({js.Length:N0} bytes, {model.Sheets.Count} sheets)");
        return 0;
    }
}
