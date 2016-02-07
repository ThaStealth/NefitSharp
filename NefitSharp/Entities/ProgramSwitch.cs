using System;
using NefitSharp.Entities.Internal;

namespace NefitSharp.Entities
{
    public enum ProgramName
    {
        Sleep,Awake,LeaveHome,Home,OtherPeriod1,OtherPeriod2
    }   

    public class ProgramSwitch
    {
        public DateTime Timestamp { get; }

        public ProgramName Name { get; }

        public double Temperature { get; }


        internal ProgramSwitch(NefitSwitch prog)
        {
            DateTime now = Utils.GetNextDate(prog.d, prog.t);
            Timestamp = now;
            //On = prog.dhw == "on";
            Temperature = prog.T;
        }

        internal ProgramSwitch(NefitProgram prog)
        {
            DateTime now = Utils.GetNextDate(prog.d, prog.t);
            Timestamp = now;
            Name = (ProgramName) prog.name;
            Temperature = prog.T;            
        }
    }
}