using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace BlockedTilesRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Adding blocked tiles");

            using (SqlConnection sql = new SqlConnection())
            {
                sql.ConnectionString = "Server=localhost;Database=Goose;Trusted_Connection=True;";
                sql.Open();

                SqlCommand cmd = sql.CreateCommand();

                string[] lines = File.ReadAllLines("blockedtiles.sql");
                int onePercent = lines.Length / 100;
                int progress = 0;
                int percentage = 0;
                foreach (string line in lines)
                {
                    cmd.CommandText = line;
                    cmd.ExecuteNonQuery();

                    progress++;

                    if (progress % onePercent == 0)
                    {
                        percentage++;
                        Console.WriteLine("{0}% adding blocked tiles", percentage);
                    }
                }
            }

            Console.WriteLine("Added all blocked tiles!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
