using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NefitSharp
{
    public class NefitAPI
    {
        private NefitClient _client;
        private string _lastMessage;
        private object _lockObj;
        private const int cAliveInterval = 1000;

        private const int cRequestTimeout = 30 * 1000;

        public bool Connected
        {
            get
            {
                return _client.Connected;
            }
        }

        public NefitAPI(string serial, string accesskey, string password)
        {
            _lockObj = new object();
            _lastMessage = null;
            _client = new NefitClient(serial,accesskey,password);
          
        }

        public void Connect()
        {
            _client.Connect();
            if (!Connected)
            {
                throw new UnauthorizedAccessException("Invalid serial/accesskey/password");
            }
            _client.NefitServerMessage += HandleNefitServerMessage;
        }

        private void HandleNefitServerMessage(string message)
        {
            lock (_lockObj)
            {
                _lastMessage = message;
            }
        }

        private string AwaitMessage()
        {
            int timeout = cRequestTimeout;
            while (timeout > 0)
            {
                lock (_lockObj)
                {
                    if (_lastMessage != null)
                    {
                        string result = _lastMessage;
                        _lastMessage = null;
                        return result;
                    }
                }
                timeout -= cAliveInterval;
                Thread.Sleep(cAliveInterval);
            }
            return null;
        }

        public async Task<bool> SetTemperature(int temperature)
        {
            bool result = false;
            await Task.Run(() =>
            {
                _client.Put("/heatingCircuits/hc1/temperatureRoomManual", "{\"value\":\"" + temperature + "\"}");
                AwaitMessage();
                _client.Put("/heatingCircuits/hc1/manualTempOverride/status", "{\"value\":\"on\"}");
                AwaitMessage();
                _client.Put("/heatingCircuits/hc1/manualTempOverride/temperature", "{\"value\":\"" + temperature + "\"}");
                AwaitMessage();
                result = true;
            });
            return result;
        }

    }
}