using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace Goose
{

    public class ChatFilter
    {
        Dictionary<string, string> WordFilter;

        public ChatFilter()
        {
            this.WordFilter = new Dictionary<string, string>();
        }

        public void LoadFilter(GameWorld world)
        {
            SqlCommand command = new SqlCommand("SELECT word,filtered FROM wordfilter", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                WordFilter.Add(Convert.ToString(reader["word"]),
                    Convert.ToString(reader["filtered"]));
            }

            reader.Close();
        }

        public string Filter(string input)
        {
            string replaced;
            string output = "";

            foreach (string word in input.Split(" ".ToCharArray(), StringSplitOptions.None))
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

        public int Count { get { return this.WordFilter.Count; } }
    }
}