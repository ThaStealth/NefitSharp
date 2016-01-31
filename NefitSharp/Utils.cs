using System;
using System.Threading;

namespace NefitSharp
{
    internal static class Utils
    {
        internal static string DoubleToString(double d)
        {
            return d.ToString().Replace(",", ".");
        }

        internal static double StringToDouble(string str)
        {
            return Convert.ToDouble(str.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
        }
    
        internal static DateTime GetNextDate(string day, int time)
        {
            DayOfWeek dwi;
            switch (day)
            {
                default:
                    dwi = DayOfWeek.Sunday;
                    break;
                case "Mo":
                    dwi = DayOfWeek.Monday;
                    break;
                case "Tu":
                    dwi = DayOfWeek.Tuesday;
                    break;
                case "We":
                    dwi = DayOfWeek.Wednesday;
                    break;
                case "Th":
                    dwi = DayOfWeek.Thursday;
                    break;
                case "Fr":
                    dwi = DayOfWeek.Friday;
                    break;
                case "Sa":
                    dwi = DayOfWeek.Saturday;
                    break;
            }
            DateTime now = DateTime.Today;
            while (now.DayOfWeek != dwi)
            {
                now = now.AddDays(1);
            }
            now = now.AddMinutes(time);
            return now;
        }
    }
}