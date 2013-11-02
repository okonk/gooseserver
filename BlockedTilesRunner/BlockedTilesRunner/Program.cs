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
                sql.ConnectionString = "Server=localhost;Database=IllutiaGoose;User Id=GooseServer;Password=password1;";
                sql.Open();

                SqlCommand cmd = sql.CreateCommand();

                //string[] lines = File.ReadAllLines("blockedtiles.sql");
                //int onePercent = lines.Length / 100;
                int progress = 0;
                //int percentage = 0;

                using (FileStream fs = File.Open("blockedtiles.sql", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BufferedStream bs = new BufferedStream(fs))
                using (StreamReader sr = new StreamReader(bs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        cmd.CommandText = line;
                        cmd.ExecuteNonQuery();

                        if (progress % 10000 == 0) Console.WriteLine("Added {0} blocked tiles", progress);

                        progress++;
                    }
                }

                //foreach (string line in File.ReadAllLines("blockedtiles.sql"))
                //{
                //    cmd.CommandText = line;
                //    cmd.ExecuteNonQuery();

                //    if (progress % 10000 == 0) Console.WriteLine("Added {0} blocked tiles", progress);

                //    progress++;

                //    //if (progress % onePercent == 0)
                //    //{
                //    //    percentage++;
                //    //    Console.WriteLine("{0}% adding blocked tiles", percentage);
                //    //}
                //}
            }

            Console.WriteLine("Added all blocked tiles!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
