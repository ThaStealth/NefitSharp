using System;
using NefitSharp.Entities.Internal;

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

        internal ProgramSwitch(NefitSwitch prog)
        {
            DateTime now = Utils.GetNextDate(prog.d, prog.t);
            Timestamp = now;
            On = prog.dhw == "on";
            Temperature = prog.T;
        }

        internal ProgramSwitch(NefitProgram prog)
        {
            DateTime now = Utils.GetNextDate(prog.d, prog.t);
            Timestamp = now;
            On = prog.active == "on";
            Temperature = prog.T;            
        }
    }
}