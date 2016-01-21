using System;
using System.Threading;
using System.Threading.Tasks;
using NefitSharp.Entities;

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

        private string WaitForResponseSync()
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

        public async Task<Program[]> GetProgram()
        {
            Program[] result = null;
            await Task.Run(() =>
            {
                _client.Get("/ecus/rrc/userprogram/activeprogram");
                string activeProgram = WaitForResponseSync();
                _client.Get("/ecus/rrc/userprogram/program1");
                string program1 = WaitForResponseSync();
                _client.Get("/ecus/rrc/userprogram/program2");
                string program2 = WaitForResponseSync();
                result = null;//TODO: Write parser
            });
            return result;
        }


        public async Task<Status?> GetStatus()
        {
            Status? result = null;
            await Task.Run(() =>
            {
                _client.Get("/ecus/rrc/uiStatus");
                string status = WaitForResponseSync();
                _client.Get("/system/sensors/temperatures/outdoor_t1");
                string outdoor = WaitForResponseSync();
                result = null;//TODO: Write parser
            });
            return result;
        }


        public async Task<Location?> GetLocation()
        {
            Location? result = null;
            await Task.Run(() =>
            {
                _client.Get("/system/location/latitude");
                string lat = WaitForResponseSync();
                _client.Get("/system/location/longitude");
                string lon = WaitForResponseSync();
                result = new Location(Convert.ToDouble(lat), Convert.ToDouble(lon));
            });
            return result;
        }


        public async Task<StatusCode?> GetDisplayCode()
        {
            StatusCode? result = null;
            await Task.Run(() =>
            {
                _client.Get("/system/appliance/displaycode");
                string displayCode = WaitForResponseSync();
                _client.Get("/system/appliance/causecode");
                string causeCode = WaitForResponseSync();                
                result = new StatusCode(Convert.ToInt32(displayCode),Convert.ToInt32(causeCode));
            });
            return result;
        }

        public async Task<double?> GetPressure()
        {
            double? result = null;
            await Task.Run(() =>
            {
                _client.Get("/system/appliance/systemPressure");
                string pressure = WaitForResponseSync();
                result = Convert.ToDouble(pressure);
            });
            return result;
        }

        public async Task<double?> GetTemperature()
        {
            double? result = null;
            await Task.Run(() =>
            {
                _client.Get("/heatingCircuits/hc1/actualSupplyTemperature");
                string temperature = WaitForResponseSync();
                result = Convert.ToDouble(temperature);
            });
            return result;
        }

        public async Task<bool> SetTemperature(double temperature)
        {
            bool result = false;
            await Task.Run(() =>
            {
                _client.Put("/heatingCircuits/hc1/temperatureRoomManual", "{\"value\":\"" + temperature + "\"}");
                WaitForResponseSync();
                _client.Put("/heatingCircuits/hc1/manualTempOverride/status", "{\"value\":\"on\"}");
                WaitForResponseSync();
                _client.Put("/heatingCircuits/hc1/manualTempOverride/temperature", "{\"value\":\"" + temperature + "\"}");
                WaitForResponseSync();
                result = true;
            });
            return result;
        }
    }
}