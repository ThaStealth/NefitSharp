namespace NefitSharp.Entities
{
    public struct Program
    {
        public ProgramSwitch[] Program1 { get; }

        public ProgramSwitch[] Program2 { get; }

        public int ActiveProgram { get; }

        public Program(ProgramSwitch[] program1, ProgramSwitch[] program2, int activeProgram)
        {
            Program1 = program1;
            Program2 = program2;
            ActiveProgram = activeProgram;
        }
    }
}