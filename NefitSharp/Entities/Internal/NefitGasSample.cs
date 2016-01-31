using System.Diagnostics.CodeAnalysis;

namespace NefitSharp.Entities.Internal
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    class NefitGasSample
    {
        public string d { get; set; }
        public double T { get; set; }
        public double ch { get; set; }
        public double hw { get; set; }
    }
}