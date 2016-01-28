using System;

namespace NefitSharp.Entities
{
    public struct ProgramSwitch
    {
        public DateTime Timestamp { get; }

        public bool On { get; }

        public double Temperature { get; }

        internal ProgramSwitch(string day,int time, bool on, double temperature)
        {
            DayOfWeek dwi;
            switch (day)
            {
                default:
                case "Su":
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
            Timestamp = now;
            On = on;
            Temperature = temperature;
        }
    }
}