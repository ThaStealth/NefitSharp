using System;

namespace NefitSharp
{
    class NefitHTTPResponse
    {
        public int Code { get; }
        public string[] HeaderData { get; }
        public string Payload { get; }
        
        public NefitHTTPResponse(string result)
        {
            string[] res = result.Split('\n');
            if (res.Length > 0)
            {
                if (res[0].StartsWith("HTTP"))
                {
                    string[] resCode = res[0].Split(' ');
                    Code = Convert.ToInt32(resCode[1]);
                }
            }
            HeaderData = new string[res.Length-4];
            for (int i = 1; i < res.Length - 3; i++)
            {
                HeaderData[i - 1] = res[i];
            }
            Payload = res[res.Length - 1];
        }
    }
}