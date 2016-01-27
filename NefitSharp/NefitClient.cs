using System;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using agsXMPP;

namespace NefitSharp
{
    class NefitClient
    {
        private const string cHost = "wa2-mz36-qrmzh6.bosch.de";
        private const string cAccesskeyPrefix = "Ct7ZR03b_";
        private const string cRrcContactPrefix = "rrccontact_";
        private const string cRrcGatewayPrefix = "rrcgateway_";
        private const int cPingInterval = 30 * 1000;
        private const int cAliveInterval = 1000;

        private XmppClientConnection _client;
        private readonly NefitEncryption _encryptionHelper;
        private string _accessKey;
        private string _serial;

        public event NefitResponse NefitServerMessage = delegate { };

        public bool Connected
        {
            get
            {
                if(_client==null)
                {
                    return false;
                }
                return _client.XmppConnectionState == XmppConnectionState.SessionStarted;
            }
        }

        public NefitClient(string serial, string accesskey, string password)
        {
            _serial = serial;
            _accessKey = accesskey;
            _encryptionHelper = new NefitEncryption(serial, accesskey, password);
        }        

        public void Connect()
        {
            if (_client != null)
            {
                Disconnect();
            }
            try
            {
                _client = new XmppClientConnection(cHost);
                _client.Open(cRrcContactPrefix + _serial, cAccesskeyPrefix + _accessKey);
                _client.Server = cHost;
                _client.Resource = "";
                _client.AutoAgents = false;                
                _client.AutoRoster = true;
                _client.AutoPresence = true;        
                _client.OnReadXml += _client_OnReadXml;
                _client.OnWriteXml += _client_OnWriteXml;                  
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + " - " + e.StackTrace);
            }
        }

    

        private void _client_OnReadXml(object sender, string xml)
        {
            Console.WriteLine("<< " + xml);

            if (xml.StartsWith("<message"))
            {
                string[] response = xml.Split('\n');
                string payload = response[response.Length-1];
                payload = payload.Remove(payload.Length - 17, 17);
                NefitServerMessage(_encryptionHelper.Decrypt(payload));                
            }
        }
        private void _client_OnWriteXml(object sender, string xml)
        {
            Console.WriteLine(">> "+xml);
        }

        public void Disconnect()
        {
            try
            {
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + " - " + e.StackTrace);
            }
        }

        public void Put(string uri, string data)
        {
            string encryptedData = _encryptionHelper.Encrypt(data);
            _client.Send("<message from=\"" + _client.MyJID + "\" to=\"" + cRrcGatewayPrefix + _serial + "@" + cHost + "\"><body>PUT " + uri + " HTTP/1.1&amp;#13;Content-Type: application/json&amp;#13;Content-Length:"+encryptedData.Length+ "&amp;#13;User-Agent: NefitEasy&amp;#13;&amp;#13;" + encryptedData+"</body></message>");
            //_client.Send(new GenericTag(_to, $"PUT {uri} HTTP/1.1&#13;\nContent-Type: application/json&#13;\nContent-Length:{encryptedData.Length}&#13;\nUser-Agent:NefitEasy&#13;\n&#13;\n{encryptedData}")); { From = _jid });
        }

        public void Get(string uri)
        {
            _client.Send("<message from=\"" + _client.MyJID + "\" to=\"" + cRrcGatewayPrefix + _serial + "@" + cHost + "\"><body>GET "+uri+" HTTP/1.1&amp;#13;User-Agent: NefitEasy&amp;#13;&amp;#13;</body></message>");                    
        }
    }
}
