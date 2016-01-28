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
            DateTime now = Utils.GetNextDate(day, time);
            Timestamp = now;
            On = on;
            Temperature = temperature;
        }


    }
}