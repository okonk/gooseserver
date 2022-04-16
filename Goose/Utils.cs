using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public static class Utils
    {
        public static string FormatDuration(long milliseconds)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(milliseconds);

            string cd = "";
            if (t.Hours != 0)
                cd += t.Hours + "h ";

            if (t.Minutes != 0)
                cd += t.Minutes + "m ";

            if (t.Seconds != 0)
            {
                double seconds = t.Seconds;
                if (t.Milliseconds != 0)
                {
                    seconds += t.Milliseconds / 1000.0d;
                    cd += string.Format("{0:N1}s ", seconds);
                }
                else
                {
                    cd += t.Seconds + "s ";
                }
            }
            else if (cd == "" || t.Milliseconds != 0)
            {
                cd += t.Milliseconds + "ms";
            }

            return cd;
        }

        public static string FormatNumber(long num)
        {
            // Ensure number has max 3 significant digits (no rounding up can happen)
            long i = (long)Math.Pow(10, (int)Math.Max(0, Math.Log10(num) - 2));
            num = num / i * i;

            if (num >= 1000000000)
                return (num / 1000000000D).ToString("0.##") + "b";
            if (num >= 1000000)
                return (num / 1000000D).ToString("0.##") + "m";
            if (num >= 1000)
                return (num / 1000D).ToString("0.##") + "k";

            return num.ToString("#,0");
        }
    }
}
