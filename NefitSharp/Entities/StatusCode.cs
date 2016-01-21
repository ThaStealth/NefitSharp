namespace NefitSharp.Entities
{
    public struct StatusCode
    {
        int _displayCode;
        int _causeCode;
        

        public int DisplayCode
        {
            get { return _displayCode; }
        }

        public int CauseCode
        {
            get { return _causeCode; }
        }



        internal StatusCode(int displayCode, int causeCode)
        {
            _displayCode = displayCode;
            _causeCode = causeCode;
        }
    }
}