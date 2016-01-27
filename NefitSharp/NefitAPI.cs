using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using agsXMPP.protocol.iq.privacy;
using NefitSharp.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            while (!_client.Connected)
            {
                Thread.Sleep(10);
            }
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



        private T WaitForResponseSync<T>()
        {
            int timeout = cRequestTimeout;
            while (timeout > 0)
            {
                lock (_lockObj)
                {
                    if (_lastMessage != null)
                    {
                        NefitJson<T> obj =  JsonConvert.DeserializeObject< NefitJson<T>>(_lastMessage);
                        _lastMessage = null;
                        if (obj != null)
                        {
                            return obj.value;
                        }
                        timeout = 0;
                    }
                }
                timeout -= cAliveInterval;
                Thread.Sleep(cAliveInterval);
            }
            return default(T);
        }

        public Program? GetProgram()
        {
            Task<Program?> task = GetProgramAsync();
            task.Wait();
            return task.Result;
        }

        public async Task<Program?> GetProgramAsync()
        {
            Program? result = null;
            await Task.Run(() =>
            {
                _client.Get("/ecus/rrc/userprogram/activeprogram");
                int activeProgram = WaitForResponseSync<int>();
                _client.Get("/ecus/rrc/userprogram/program1");
                NefitProgram[] program1 = WaitForResponseSync<NefitProgram[]>();
                List<ProgramSwitch> programs1 = new List<ProgramSwitch>();

                foreach (NefitProgram prog in program1)
                {
                   programs1.Add(new ProgramSwitch(prog.d,prog.t,prog.active=="on",prog.T));
                }


                _client.Get("/ecus/rrc/userprogram/program2");
                NefitProgram[] program2 = WaitForResponseSync<NefitProgram[]>();
                List<ProgramSwitch> programs2 = new List<ProgramSwitch>();

                foreach (NefitProgram prog in program2)
                {
                    programs2.Add(new ProgramSwitch(prog.d, prog.t, prog.active == "on", prog.T));
                }
                result = new Program (programs1.ToArray(),programs2.ToArray(),activeProgram);//TODO: Write parser
            });
            return result;
        }

        public Status? GetStatus()
        {
            Task<Status?> task = GetStatusAsync();
            task.Wait();
            return task.Result;
        }

        public async Task<Status?> GetStatusAsync()
        {
            Status? result = null;
            await Task.Run(() =>
            {
                _client.Get("/ecus/rrc/uiStatus");
                NefitStatus status = WaitForResponseSync<NefitStatus>();
                _client.Get("/system/sensors/temperatures/outdoor_t1");
                double outdoor = WaitForResponseSync<double>();
                result = new Status(status,outdoor);                
            });
            return result;
        }

        public Location? GetLocation()
        {
            Task<Location?> task = GetLocationAsync();
            task.Wait();
            return task.Result;
        }

        public async Task<Location?> GetLocationAsync()
        {
            Location? result = null;
            await Task.Run(() => 
            {
                _client.Get("/system/location/latitude");
                string lat = WaitForResponseSync<string>();                
                _client.Get("/system/location/longitude");
                string lon = WaitForResponseSync<string>();                
                result = new Location(Convert.ToDouble(lat.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator)), Convert.ToDouble(lon.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator)));
            });
            return result;
        }

        public StatusCode? GetDisplayCode()
        {
            Task<StatusCode?> task = GetDisplayCodeAsync();
            task.Wait();
            return task.Result;
        }

        public async Task<StatusCode?> GetDisplayCodeAsync()
        {
            StatusCode? result = null;
            await Task.Run(() =>
            {
                _client.Get("/system/appliance/displaycode");
                string displayCode = WaitForResponseSync<string>();
                _client.Get("/system/appliance/causecode");
                string causeCode = WaitForResponseSync<string>();                
                result = new StatusCode(displayCode,Convert.ToInt32(causeCode));
            });
            return result;
        }

        public double? GetPressure()
        {
            Task<double?> task = GetPressureAsync();
            task.Wait();
            return task.Result;
        }

        public async Task<double?> GetPressureAsync()
        {
            double? result = null;
            await Task.Run(() =>
            {
                _client.Get("/system/appliance/systemPressure");
                result = WaitForResponseSync<double>();
            });
            return result;
        }

        public double? GetTemperature()
        {
            Task<double?> task = GetTemperatureAsync();
            task.Wait();
            return task.Result;
        }

        public async Task<double?> GetTemperatureAsync()
        {
            double? result = null;
            await Task.Run(() =>
            {
                _client.Get("/heatingCircuits/hc1/actualSupplyTemperature");
                result = WaitForResponseSync<double>();
            });
            return result;
        }

        public async Task<bool> SetTemperature(double temperature)
        {
            bool result = false;
            await Task.Run(() =>
            {
                _client.Put("/heatingCircuits/hc1/temperatureRoomManual", "{\"value\":" + temperature + "}");
                WaitForResponseSync<string>();
                _client.Put("/heatingCircuits/hc1/manualTempOverride/status", "{\"value\":'on'}");
                WaitForResponseSync<string>();
                _client.Put("/heatingCircuits/hc1/manualTempOverride/temperature", "{\"value\":\"" + temperature + "\"}");
                WaitForResponseSync<string>();
                result = true;
            });
            return result;
        }
    }
}