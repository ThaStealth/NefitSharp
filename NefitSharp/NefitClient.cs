using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using agsXMPP;
using NefitSharp.Entities;
using NefitSharp.Entities.Internal;
using Newtonsoft.Json;

namespace NefitSharp
{
    public class NefitClient
    {
        private const string cHost = "wa2-mz36-qrmzh6.bosch.de";
        private const string cAccesskeyPrefix = "Ct7ZR03b_";
        private const string cRrcContactPrefix = "rrccontact_";
        private const string cRrcGatewayPrefix = "rrcgateway_";

        private XmppClientConnection _client;
        private readonly NefitEncryption _encryptionHelper;
        private readonly string _accessKey;
        private readonly string _serial;

        private NefitHTTPResponse _lastMessage;
        private readonly object _lockObj;
        private bool _authenticationError;
        private bool _readyForCommands;

        private const int cRequestTimeout = 30 * 1000;
        private const int cCheckInterval = 100;
        private const int cKeepAliveInterval = 30*1000;

        public bool Connected
        {
            get
            {
                if (_client == null)
                {
                    return false;
                }
                return _client.XmppConnectionState == XmppConnectionState.SessionStarted && !_authenticationError && _readyForCommands;
            }
        }
        
        public NefitClient(string serial, string accesskey, string password)
        {
            _lockObj = new object();
            _lastMessage = null;
            _serial = serial;
            
            _accessKey = accesskey;
            _encryptionHelper = new NefitEncryption(serial, accesskey, password);
        }

        #region XMPP Communication

        public bool Connect()
        {
            ConnectAsync();
            while (!Connected)
            {
                Thread.Sleep(10);
            }
            return Connected;
        }

        public void ConnectAsync()
        {
            if (_client != null)
            {
                Disconnect();
            }
            try
            {
                _authenticationError = false;
                _readyForCommands = false;
                _client = new XmppClientConnection(cHost);
                _client.Open(cRrcContactPrefix + _serial, cAccesskeyPrefix + _accessKey);
                _client.Server = cHost;
                _client.KeepAliveInterval = cKeepAliveInterval;
                _client.Resource = "";
                _client.AutoAgents = false;
                _client.AutoRoster = true;
                _client.AutoPresence = true;
                _client.OnReadXml += XmppRead;
                _client.OnWriteXml += XmppWrite;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + " - " + e.StackTrace);
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
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + " - " + e.StackTrace);
            }
        }

        private void XmppRead(object sender, string xml)
        {
            if (!xml.StartsWith("<stream"))
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);
                    if (xmlDoc.DocumentElement.Name == "message")
                    {                        
                        NefitHTTPResponse header = new NefitHTTPResponse(xmlDoc.InnerText);                        
                        lock (_lockObj)
                        {
                            _lastMessage = header;
                        }
                    }
                    else if (xmlDoc.DocumentElement.Name == "failure" && xmlDoc.FirstChild.FirstChild.Name == "not-authorized")
                    {
                        _authenticationError = true;
                        Disconnect();
                    }
                }
                catch
                {
                    Debug.Write("XML Parsing error");
                    Disconnect();
                }
            }
        }
        
        private void XmppWrite(object sender, string xml)
        {
            if (!xml.StartsWith("<stream"))
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);
                    if (xmlDoc.DocumentElement.Name == "presence")
                    {
                        _readyForCommands = true;
                    }
                }
                catch
                {
                    Debug.Write("XML Parsing error");
                    Disconnect();
                }
            }
        }


        private bool PutSync(string url, string data)
        {
            NefitHTTPRequest request = new NefitHTTPRequest(url, _client.MyJID, cRrcGatewayPrefix + _serial + "@" + cHost, _encryptionHelper.Encrypt(data));
            _client.Send(request.ToString());
            int timeout = cRequestTimeout;
            while (timeout > 0)
            {
                lock (_lockObj)
                {
                    if (_lastMessage!=null)
                    {
                        return _lastMessage.Code == 204 || _lastMessage.Code == 200;
                    }
                }
                timeout -= cCheckInterval;
                Thread.Sleep(cCheckInterval);
            }
            return false;
        }

        private T GetSync<T>(string url)
        {
            NefitHTTPRequest request = new NefitHTTPRequest(url, _client.MyJID, cRrcGatewayPrefix + _serial + "@" + cHost);
            _client.Send(request.ToString());         
            int timeout = cRequestTimeout;
            while (timeout > 0)
            {
                lock (_lockObj)
                {
                    if (_lastMessage!=null)
                    {                        
                        NefitJson<T> obj = JsonConvert.DeserializeObject<NefitJson<T>>(_encryptionHelper.Decrypt(_lastMessage.Payload));
                        _lastMessage = null;
                        if (obj != null)
                        {
                            return obj.value;
                        }

                        timeout = 0;
                    }
                }
                timeout -= cCheckInterval;
                Thread.Sleep(cCheckInterval);
            }
            return default(T);
        }
        #endregion

        #region Commands

        #region Sync Methods

        public CVProgram GetProgram()
        {
            return AwaitTask(GetProgramAsync());
        }

        public Status? GetStatus()
        {
            return AwaitTask(GetStatusAsync());
        }

        public Location? GetLocation()
        {
            return AwaitTask(GetLocationAsync());
        }

        public StatusCode? GetDisplayCode()
        {
            return AwaitTask(GetDisplayCodeAsync());
        }

        public double? GetCVWaterTemperature()
        {
            return AwaitTask(GetCVWaterTemperatureAsync());
        }

        public double? GetPressure()
        {
            return AwaitTask(GetPressureAsync());
        }

        public string GetCVSerialNumber()
        {
            return AwaitTask(GetCVSerialNumberAsync());
        }

        public GasSample[] GetGasusage()
        {
            return AwaitTask(GetGasusageAsync());
        }

        public Information? GetInformation()
        {
            return AwaitTask(GetInformationAsync());
        }

        public bool SetTemperature(double temperature)
        {
            return AwaitTask(SetTemperatureAsync(temperature));
        }

        private T AwaitTask<T>(Task<T> task)
        {
            try
            {
                task.Wait();
                return task.Result;
            }
            catch
            {
                return default(T);
            }
        }

        #endregion

        #region Async commands

        public async Task<CVProgram> GetProgramAsync()
        {
            CVProgram result = null;
            await Task.Run(() =>
            {
                int activeProgram = GetSync<int>("/ecus/rrc/userprogram/activeprogram");
                NefitProgram[] program0 = GetSync<NefitProgram[]>("/ecus/rrc/userprogram/program0");
                NefitProgram[] program1 = GetSync<NefitProgram[]>("/ecus/rrc/userprogram/program1");
                NefitProgram[] program2 = GetSync<NefitProgram[]>("/ecus/rrc/userprogram/program2");

                result = new CVProgram(program0, program1, program2,  activeProgram);
            });
            return result;
        }


        public async Task<Information?> GetInformationAsync()
        {
            Information? result = null;
            await Task.Run(() =>
            {
                string owner = GetSync<string>("/ecus/rrc/personaldetails");
                string[] ownerInfo = owner.Split(';');
                string installer = GetSync<string>("/ecus/rrc/installerdetails");
                string[] installerInfo = installer.Split(';');
                string easySerial = GetSync<string>("/gateway/serialnumber");
                string easyUpdate = GetSync<string>("/gateway/update/strategy");
                string easyFirmware = GetSync<string>("/gateway/versionFirmware");
                string easyHardware = GetSync<string>("/gateway/versionHardware");
                string easyUuid = GetSync<string>("/gateway/uuid");
                string cvSerial = GetCVSerialNumber();
                string cvVersion = GetSync<string>("/system/appliance/version");
                string cvBurner = GetSync<string>("/system/interfaces/ems/brandbit");
                result = new Information(ownerInfo[0], ownerInfo[1], ownerInfo[2], installerInfo[0], installerInfo[1],installerInfo[2],
                 easySerial,easyUpdate,easyFirmware,easyHardware,easyUuid,cvSerial,cvVersion,cvBurner);
            });
            return result;
        }
 
        
        public async Task<Status?> GetStatusAsync()
        {
            Status? result = null;
            await Task.Run(() =>
            {
                NefitStatus status = GetSync<NefitStatus>("/ecus/rrc/uiStatus");
                double outdoor = GetSync<double>("/system/sensors/temperatures/outdoor_t1");
                string serviceStatus = GetSync<string>("/gateway/remote/servicestate");
                result = new Status(status, serviceStatus, outdoor);
            });
            return result;
        }

        public async Task<Location?> GetLocationAsync()
        {
            Location? result = null;
            await Task.Run(() =>
            {
                string lat = GetSync<string>("/system/location/latitude");
                string lon = GetSync<string>("/system/location/longitude");
                result = new Location(Convert.ToDouble(lat.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator)), Convert.ToDouble(lon.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator)));
            });
            return result;
        }

        public async Task<StatusCode?> GetDisplayCodeAsync()
        {
            StatusCode? result = null;
           await Task.Run(() =>
            {
                string displayCode = GetSync<string>("/system/appliance/displaycode");
                string causeCode = GetSync<string>("/system/appliance/causecode");
                if (!string.IsNullOrEmpty(displayCode) && !string.IsNullOrEmpty(causeCode))
                {
                    result= new StatusCode(displayCode, Convert.ToInt32(causeCode));
                }
            });
            return result;
        }
        
        public async Task<double?> GetPressureAsync()
        {
            return await Task.Run(() => GetSync<double>("/system/appliance/systemPressure"));            
        }

        public async Task<double?> GetCVWaterTemperatureAsync()
        {
            return await Task.Run(() => GetSync<double>("/heatingCircuits/hc1/actualSupplyTemperature"));            
        }

        public async Task<string> GetCVSerialNumberAsync()
        {
            return await Task.Run(() => GetSync<string>("/system/appliance/serialnumber"));
        }

        public async Task<GasSample[]> GetGasusageAsync()
        {
            GasSample[] result = null;
            await Task.Run(() =>
            {
//                int currentPage = GetSync<int>("/ecus/rrc/recordings/gasusagePointer")-1;
                bool hasValidSamples = true;
                int currentPage = 1;
                List<GasSample> gasSamples = new List<GasSample>();
                while(hasValidSamples)                
                {
                    NefitGasSample[] samples = GetSync<NefitGasSample[]>("/ecus/rrc/recordings/gasusage?page=" + currentPage);
                    foreach(NefitGasSample sample in samples)
                    {
                        if (sample.d != "255-256-65535")
                        {
                            gasSamples.Add(new GasSample(Convert.ToDateTime(sample.d), sample.hw/10.0, sample.ch/10.0));
                        }
                        else
                        {
                            hasValidSamples = false;
                            break;
                        }
                    }
                    currentPage++;
                }
                result = gasSamples.ToArray();
            });
            return result;
        }



        public async Task<bool> SetTemperatureAsync(double temperature)
        {
            return await Task.Run(() =>
            {
                bool result = PutSync("/heatingCircuits/hc1/temperatureRoomManual", "{\"value\":" + temperature.ToString().Replace(",",".") + "}");
                if (result)
                {
                    result = PutSync("/heatingCircuits/hc1/manualTempOverride/status", "{\"value\":'on'}");
                }
                if (result)
                {
                    PutSync("/heatingCircuits/hc1/manualTempOverride/temperature", "{\"value\":\"" + temperature.ToString().Replace(",", ".") + "\"}");
                }
                return result;
            });
        }

        #endregion
#endregion
    }
}
