using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GooseServerBrowserService.Contract
{
    public class RegisterRequest
    {
        public string ServerName { get; set; }
        public int Port { get; set; }
        public int PlayerCount { get; set; }
        public string Version { get; set; }

        public string Checksum { get; set; }

        public string ComputeChecksum()
        {
            return CalculateMD5("uXBbUBoCylMjyRWApxZO" + ServerName + Port + PlayerCount + Version + "SzycoiHZmn5aftnN0FWd");
        }

        public string CalculateMD5(string input)
        {
            var md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // string representation (similar to UNIX format)
            string hash = BitConverter.ToString(hashBytes)
                // without dashes
               .Replace("-", string.Empty)
                // make lowercase
               .ToLower();

            return hash;
        }
    }
}
