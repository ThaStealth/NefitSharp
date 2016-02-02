using System.Collections.Generic;
using NefitSharp.Entities.Internal;

namespace NefitSharp.Entities
{
    public class UserProgram
    {
        public ProgramSwitch[][] Program { get; }
        
        public int ActiveProgram { get; }
        public bool FireplaceFunction { get; }
        public bool PreHeating { get; }
        public string UserSwitchpointName1 { get; }
        public string UserSwitchpointName2 { get; }

        internal UserProgram(NefitProgram[] program0, NefitProgram[] program1, NefitProgram[] program2, int activeProgram, bool fireplaceFunction, bool preHeating, string userSwitchpointName1, string userSwitchpointName2)
        {
            Program = new ProgramSwitch[3][];            
            Program[0] = ParseProgram(program0);
            Program[1] = ParseProgram(program1);
            Program[2] = ParseProgram(program2);
            ActiveProgram = activeProgram;
            FireplaceFunction = fireplaceFunction;
            PreHeating = preHeating;
            UserSwitchpointName1 = userSwitchpointName1;
            UserSwitchpointName2 = userSwitchpointName2;
        }

        private ProgramSwitch[] ParseProgram(NefitProgram[] proag)
        {
            List<ProgramSwitch> programs2 = new List<ProgramSwitch>();
            foreach (NefitProgram prog in proag)
            {
                programs2.Add(new ProgramSwitch(prog));
            }
            return programs2.ToArray();
        }
    }
}