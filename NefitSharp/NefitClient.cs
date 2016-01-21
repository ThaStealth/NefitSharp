using System;
using System.Diagnostics;
using System.Threading;
using Sharp.Xmpp.Client;
using Sharp.Xmpp.Im;

namespace NefitSharp
{
    delegate void NefitResponse(string message);

    class NefitClient
    {
        private const string cHost = "wa2-mz36-qrmzh6.bosch.de";

        private const string cAccesskeyPrefix = "Ct7ZR03b_";
        private const string cRrcContactPrefix = "rrccontact_";
        private const string cRrcGatewayPrefix = "rrcgateway_";

    
        private const int cPingInterval = 30 * 1000;
        private const int cAliveInterval = 1000;

        private XmppClient _client;
        private readonly string _to;
        private readonly string _jid;

        private readonly NefitEncryption _encryptionHelper;
        private Thread _keepAliveThread;
        private bool _keepAlive;
        
        private string _accessKey;

        public event NefitResponse NefitServerMessage = delegate { };

        public bool Connected
        {
            get
            {
                if(_client==null)
                {
                    return false;
                }
                return _client.Connected;
            }
        }

        public NefitClient(string serial, string accesskey, string password)
        {
            _accessKey = accesskey;
            _encryptionHelper = new NefitEncryption(serial, accesskey, password);                        
            _jid = cRrcContactPrefix + serial + "@" + cHost;
            _to = cRrcGatewayPrefix + serial + "@" + cHost;           
        }        

        public void Connect()
        {
            if (_client != null || _keepAliveThread!=null)
            {
                Disconnect();
            }
            try
            {
                _client = new XmppClient(cHost, _jid, cAccesskeyPrefix + _accessKey);          
                _keepAliveThread = new Thread(AliveHandler);
                _keepAlive = true;
                _keepAliveThread.IsBackground = true;                
                _client.DefaultTimeOut = -1;
                //  _client.Connect();
                _client.Message += MessageReceived;
                if (_client.Connected)
                {
                    _keepAliveThread.Start();
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message +" - "+e.StackTrace);
            }
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
                if (_keepAliveThread != null)
                {
                    _keepAlive = false;
                    _keepAliveThread.Abort();
                    _keepAliveThread = null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + " - " + e.StackTrace);
            }
        }

        private void AliveHandler()
        {
            int pingTimeout = 0;
            while (_keepAlive)
            {
                if (pingTimeout <= 0)
                {
                    _client.SendMessage(_to, "<presence/>");
                    pingTimeout = cPingInterval;
                }
                Thread.Sleep(cAliveInterval);
                pingTimeout -= cAliveInterval;
            }
        }

        public void Put(string uri, string data)
        {
            string encryptedData = _encryptionHelper.Encrypt(data);            
            _client.SendMessage(new Message(_to, $"PUT {uri} HTTP/1.1&#13;\nContent-Type: application/json&#13;\nContent-Length:{encryptedData.Length}&#13;\nUser-Agent:NefitEasy&#13;\n&#13;\n{encryptedData}") { From = _jid });
        }

        public void Get(string uri)
        {            
            _client.SendMessage(new Message(_to, $"GET {uri} HTTP/1.1&#13;\nUser-Agent:NefitEasy&#13;\n&#13;\n") { From = _jid });            
        }

        private void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Jid == _jid)
            {
                if (e.Message.Type == MessageType.Error)
                {
                    //error
                }
                else if (e.Message.Type == MessageType.Chat)
                {
                    //response
                    NefitServerMessage(_encryptionHelper.Decrypt(e.Message.Body));
                }
            }
        }
    }
}
