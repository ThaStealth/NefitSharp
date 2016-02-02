using System.Diagnostics.CodeAnalysis;

namespace NefitSharp.Entities.Internal
{
    class NefitSwitch : NefitProgram
    {
        public string dhw { get; set; }
    }

    class NefitProgram : NefitProgramBase
    {
        public int name { get; set; }
        public string active { get; set; }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    class NefitProgramBase
    {
        public string d { get; set; }
        public int t { get; set; }
        public double T { get; set; }
    
    }
}