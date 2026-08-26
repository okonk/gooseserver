using System.Text;

namespace Goose
{

    public class ChatFilter
    {
        Dictionary<string, string> WordFilter;

        public ChatFilter()
        {
            this.WordFilter = [];
        }

        public void LoadFilter(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT word,filtered FROM wordfilter";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    WordFilter.Add(reader.GetString("word"),
                        reader.GetString("filtered"));
                }
            });
        }

        public string Filter(string input)
        {
            string replaced;
            string output = "";

            foreach (string word in input.Split(' ', StringSplitOptions.None))
            {
                if (this.WordFilter.TryGetValue(word.ToLower(), out replaced))
                {
                    output += replaced + " ";
                }
                else
                {
                    output += word + " ";
                }
            }

            return output;
        }

        public int Count { get => this.WordFilter.Count; }
    }
}