using System.Xml;

namespace NefitSharp
{
    class NefitHTTPRequest
    {
        public string URL { get; }
        public string Payload { get; }
        public string Jid { get; }
        public string To { get; }
        
        public NefitHTTPRequest(string url, string jid, string to, string payload=null)
        {
            URL = url;
            Payload = payload;
            Jid = jid;
            To = to;
        }

        public override string ToString()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement(string.Empty, "message", string.Empty);
            root.SetAttribute("from", Jid);
            root.SetAttribute("to", To);
            XmlElement body = xmlDoc.CreateElement(string.Empty, "body", string.Empty);
            root.AppendChild(body);
            xmlDoc.AppendChild(root);
            string result = URL;
            result += " HTTP/1.1\n";
            if (!string.IsNullOrEmpty(Payload))
            {
                result = "PUT " + result;
                result += "Content-Type: application/json\n";
                result += $"Content-Length:{Payload.Length}\n";
            }
            else
            {
                result = "GET " + result;
            }
            result += "User-Agent: NefitEasy\n\n";
            result += Payload;
            body.InnerText = result;
            return xmlDoc.InnerXml.Replace("\n", "&#13;\n");
        }
    }
}