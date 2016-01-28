using System.Diagnostics.CodeAnalysis;

namespace NefitSharp.Entities.Internal
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    class NefitProgram
    {
        public string d { get; set; }
        public int t { get; set; }
        public double T { get; set; }
        public string active { get; set; }
        public int name { get; set; }
    }
}