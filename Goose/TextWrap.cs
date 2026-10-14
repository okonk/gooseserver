namespace Goose
{
    public static class TextWrap
    {
        // Quest data text uses the two-character sequence backslash-n as a paragraph
        // separator in addition to real newlines.
        public static List<string> Lines(string text, int width = 50)
        {
            var paragraphs = text.Replace("\r\n", "\n").Replace("\\n", "\n").Split('\n');
            var lines = new List<string>();
            foreach (var paragraph in paragraphs)
                lines.AddRange(WrapParagraph(paragraph, width));
            return lines;
        }

        private static List<string> WrapParagraph(string paragraph, int width)
        {
            var lines = new List<string>();
            var current = "";
            foreach (var token in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var word = token;
                while (word.Length > width)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current);
                        current = "";
                    }
                    lines.Add(word[..width]);
                    word = word[width..];
                }
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (candidate.Length <= width)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }
            lines.Add(current);
            return lines;
        }
    }
}
