using System.Text.Json;
using Goose.Tools.SpriteBundle;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tools.Tests;

public class BundleWriterTests
{
    [Fact]
    public void Renders_a_self_contained_html_fragment()
    {
        using var img = new Image<Rgba32>(4, 4);
        var rects = new Dictionary<string, SpriteRect> { ["a:1"] = new(0, 0, 2, 2) };

        var html = BundleWriter.Render("icons", img, rects);

        Assert.Contains("GOOSE_SPRITES", html);
        Assert.Contains("\"icons\"", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("\"a:1\": [0,0,2,2]", html);
        Assert.StartsWith("<script>", html);
        Assert.EndsWith("</script>\n", html);
    }

    [Fact]
    public void Base64_payload_decodes_to_a_valid_png()
    {
        using var img = new Image<Rgba32>(8, 8);
        img[3, 3] = new Rgba32(10, 20, 30, 255);

        var html = BundleWriter.Render("icons", img, new Dictionary<string, SpriteRect>());

        var marker = "data:image/png;base64,";
        var start = html.IndexOf(marker) + marker.Length;
        var end = html.IndexOf('"', start);
        var bytes = Convert.FromBase64String(html[start..end]);

        using var decoded = Image.Load<Rgba32>(bytes);
        Assert.Equal(8, decoded.Width);
        Assert.Equal(new Rgba32(10, 20, 30, 255), decoded[3, 3]);
    }

    [Fact]
    public void Does_not_overwrite_other_bundles_on_the_global()
    {
        using var img = new Image<Rgba32>(2, 2);

        var html = BundleWriter.Render("parts", img, new Dictionary<string, SpriteRect>());

        // Must initialise the global defensively so bundles load in any order.
        Assert.Contains("var GOOSE_SPRITES = GOOSE_SPRITES || {};", html);
    }

    [Fact]
    public void Reports_the_atlas_dimensions()
    {
        using var img = new Image<Rgba32>(6, 3);

        var html = BundleWriter.Render("icons", img, new Dictionary<string, SpriteRect>());

        var body = Body(html);
        Assert.Equal(6, body.GetProperty("width").GetInt32());
        Assert.Equal(3, body.GetProperty("height").GetInt32());
    }

    /// <summary>Trailing commas are legal in modern JS but not in JSON, and the rects block is
    /// hand-built rather than serialised, so separator placement and ordering are pinned.</summary>
    [Fact]
    public void Emits_keys_in_ordinal_order_with_no_trailing_comma()
    {
        using var img = new Image<Rgba32>(2, 2);
        var rects = new Dictionary<string, SpriteRect>
        {
            ["b"] = new(1, 1, 1, 1),
            ["a"] = new(0, 0, 1, 1),
        };

        var html = BundleWriter.Render("icons", img, rects);

        Assert.Contains("\"a\": [0,0,1,1],\n    \"b\": [1,1,1,1]\n", html);
    }

    /// <summary>Keys derive from asset directory names, so they are not fully under this tool's
    /// control. Interpolated raw, a quote or backslash would emit broken JS that nothing here
    /// parses — the failure would only surface in the browser. Asserted by parsing the emitted
    /// object rather than by matching an escape sequence, so the test pins the meaning.</summary>
    [Fact]
    public void Escapes_quotes_and_backslashes_in_keys()
    {
        using var img = new Image<Rgba32>(2, 2);
        var rects = new Dictionary<string, SpriteRect> { ["a\"b\\c"] = new(1, 2, 3, 4) };

        var html = BundleWriter.Render("icons", img, rects);

        var emitted = Body(html).GetProperty("rects").GetProperty("a\"b\\c");
        Assert.Equal([1, 2, 3, 4], emitted.EnumerateArray().Select(e => e.GetInt32()));
    }

    /// <summary>The fragment lives inside a &lt;script&gt; element, where a literal
    /// "&lt;/script&gt;" inside a string closes the tag regardless of JS quoting, so escaping
    /// must cover the HTML-unsafe characters too — not just the JSON-unsafe ones.</summary>
    [Fact]
    public void Escapes_a_key_that_would_close_the_script_tag()
    {
        using var img = new Image<Rgba32>(2, 2);
        var rects = new Dictionary<string, SpriteRect> { ["</script>"] = new(0, 0, 1, 1) };

        var html = BundleWriter.Render("icons", img, rects);

        // Exactly one "</script>" in the output: the closing tag this method wrote.
        Assert.Equal(2, html.Split("</script>").Length);
        Assert.True(Body(html).GetProperty("rects").TryGetProperty("</script>", out _));
    }

    [Fact]
    public void Escapes_the_bundle_name_too()
    {
        using var img = new Image<Rgba32>(2, 2);

        var html = BundleWriter.Render("od\"d", img, new Dictionary<string, SpriteRect>());

        Assert.Equal("od\"d", Name(html));
    }

    /// <summary>The assigned object literal is valid JSON, so it can be parsed rather than
    /// string-matched.</summary>
    private static JsonElement Body(string html)
    {
        var start = html.IndexOf("] = ") + 4;
        var end = html.LastIndexOf("};");
        return JsonDocument.Parse(html[start..(end + 1)]).RootElement.Clone();
    }

    /// <summary>The bundle name appears only in the JS index expression; its contents are a
    /// JSON string literal.</summary>
    private static string Name(string html)
    {
        var start = html.IndexOf("GOOSE_SPRITES[") + "GOOSE_SPRITES[".Length;
        var end = html.IndexOf("] = ", start);
        return JsonDocument.Parse(html[start..end]).RootElement.GetString()!;
    }
}
