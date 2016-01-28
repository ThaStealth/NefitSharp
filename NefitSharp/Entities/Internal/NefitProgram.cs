namespace NefitSharp.Entities.Internal
{
    class NefitGasSample
    {
        public string d { get; set; }
        public double T { get; set; }
        public double ch { get; set; }
        public double hw { get; set; }
    }

    class NefitProgram
    {
        public string d { get; set; }
        public int t { get; set; }
        public double T { get; set; }
        public string active { get; set; }
        public int name { get; set; }
    }
}