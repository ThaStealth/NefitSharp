using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
#if !NET20 && !NET35
using System.Threading.Tasks;
#endif
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
        private object _communicationLock;
        private bool _authenticationError;
        private bool _readyForCommands;

        private const int cRequestTimeout = 5*1000;
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
                return _client.XmppConnectionState == XmppConnectionState.SessionStarted && _readyForCommands;
            }
        }

        public NefitClient(string serial, string accesskey, string password)
        {
            _lockObj = new object();
            _communicationLock = new object();
            _lastMessage = null;
            _serial = serial;

            _accessKey = accesskey;
            _encryptionHelper = new NefitEncryption(serial, accesskey, password);
        }

        #region XMPP Communication

        public bool Connect()
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
            while (!Connected && !_authenticationError)
            {
                Thread.Sleep(10);
            }
            return Connected;
        }




        public void Disconnect()
        {
            try
            {
                if (_client != null)
                {
                    _client.OnReadXml -= XmppRead;
                    _client.OnWriteXml -= XmppWrite;
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
            Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "<< " + xml);
            if (!xml.StartsWith("<stream"))
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);
                    if (xmlDoc.DocumentElement != null && xmlDoc.DocumentElement.Name == "message")
                    {
                        NefitHTTPResponse header = new NefitHTTPResponse(xmlDoc.InnerText);
                        lock (_lockObj)
                        {
                            _lastMessage = header;
                        }
                    }
                    else if (xmlDoc.DocumentElement != null && (xmlDoc.DocumentElement.Name == "failure" && xmlDoc.FirstChild.FirstChild.Name == "not-authorized"))
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
            Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ">> " + xml);
            if (!xml.StartsWith("<stream"))
            {
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);
                    if (xmlDoc.DocumentElement != null && xmlDoc.DocumentElement.Name == "presence")
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

        private bool Put(string url, string data)
        {
            lock (_communicationLock)
            {
                NefitHTTPRequest request = new NefitHTTPRequest(url, _client.MyJID, cRrcGatewayPrefix + _serial + "@" + cHost, _encryptionHelper.Encrypt(data));
                _client.Send(request.ToString());
                int timeout = cRequestTimeout;
                while (timeout > 0)
                {
                    lock (_lockObj)
                    {
                        if (_lastMessage != null)
                        {
                            bool result = _lastMessage.Code == 204 || _lastMessage.Code == 200;
                            _lastMessage = null;
                            return result;
                        }
                    }
                    timeout -= cCheckInterval;
                    Thread.Sleep(cCheckInterval);
                }
                return false;
            }
        }

        private T Get<T>(string url)
        {
            lock (_communicationLock)
            {
                NefitHTTPRequest request = new NefitHTTPRequest(url, _client.MyJID, cRrcGatewayPrefix + _serial + "@" + cHost);
                _client.Send(request.ToString());
                int timeout = cRequestTimeout;
                while (timeout > 0)
                {
                    lock (_lockObj)
                    {
                        if (_lastMessage != null)
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
        }

        #endregion

        #region Commands

        #region Sync Methods

        public UserProgram GetProgram()
        {
            int activeProgram = Get<int>("/ecus/rrc/userprogram/activeprogram");
            bool preheating = Get<string>("/ecus/rrc/userprogram/activeprogram") == "off";
            bool fireplaceFunction = Get<string>("/ecus/rrc/userprogram/activeprogram") == "off";
            string switchpointName1 = Get<string>("/ecus/rrc/userprogram/userswitchpointname1");
            string switchpointName2 = Get<string>("/ecus/rrc/userprogram/userswitchpointname2");
            NefitProgram[] program0 = Get<NefitProgram[]>("/ecus/rrc/userprogram/program0");
            NefitProgram[] program1 = Get<NefitProgram[]>("/ecus/rrc/userprogram/program1");
            NefitProgram[] program2 = Get<NefitProgram[]>("/ecus/rrc/userprogram/program2");
            if (program0 != null && program1 != null && program2 != null)
            {
                return new UserProgram(program0, program1, program2, activeProgram, fireplaceFunction, preheating, switchpointName1, switchpointName2);
            }
            return null;
        }

        public FullStatus GetStatus()
        {
            NefitStatus status = Get<NefitStatus>("/ecus/rrc/uiStatus");
            if (status != null)
            {
                double outdoor = Get<double>("/system/sensors/temperatures/outdoor_t1");
                string serviceStatus = Get<string>("/gateway/remote/servicestate");
                bool ignition = Get<string>("/ecus/rrc/pm/ignition/status") == "true";
                bool refillNeeded = Get<string>("/ecus/rrc/pm/refillneeded/status") == "true";
                bool closingValve = Get<string>("/ecus/rrc/pm/closingvalve/status") == "true";
                bool shortTapping = Get<string>("/ecus/rrc/pm/shorttapping/status") == "true";
                bool systemLeaking = Get<string>("/ecus/rrc/pm/systemleaking/status") == "true";
                double systemPressure = Get<double>("/system/appliance/systemPressure");
                double chSupplyTemperature = Get<double>("/heatingCircuits/hc1/actualSupplyTemperature");
                string operationMode = Get<string>("/heatingCircuits/hc1/operationMode");
                string displayCode = Get<string>("/system/appliance/displaycode");
                string causeCode = Get<string>("/system/appliance/causecode");
                if (!string.IsNullOrEmpty(displayCode) && !string.IsNullOrEmpty(causeCode))
                {
                    return new FullStatus(status, serviceStatus, outdoor, operationMode, refillNeeded, ignition, closingValve, shortTapping, systemLeaking, systemPressure, chSupplyTemperature, new StatusCode(displayCode, Convert.ToInt32(causeCode)));
                }
            }
            return null;
        }

        public Location? GetLocation()
        {
            string lat = Get<string>("/system/location/latitude");
            string lon = Get<string>("/system/location/longitude");
            return new Location(Utils.StringToDouble(lat), Utils.StringToDouble(lon));
        }

        public GasSample[] GetGasusage()
        {
            bool hasValidSamples = true;
            int currentPage = 1;
            List<GasSample> gasSamples = new List<GasSample>();
            while (hasValidSamples)
            {
                NefitGasSample[] samples = Get<NefitGasSample[]>("/ecus/rrc/recordings/gasusage?page=" + currentPage);
                if (samples != null && samples.Length > 0)
                {
                    foreach (NefitGasSample sample in samples)
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
                else
                {
                    hasValidSamples = false;
                }
            }
            return gasSamples.ToArray();
        }

        public UIStatus GetUIStatus()
        {
            NefitStatus status = Get<NefitStatus>("/ecus/rrc/uiStatus");
            if (status != null)
            {
                return new UIStatus(status);
            }
            return null;
        }

        public Information? GetInformation()
        {
            string owner = Get<string>("/ecus/rrc/personaldetails");
            string[] ownerInfo = owner.Split(';');
            string installer = Get<string>("/ecus/rrc/installerdetails");
            string[] installerInfo = installer.Split(';');
            string easySerial = Get<string>("/gateway/serialnumber");
            string easyUpdate = Get<string>("/gateway/update/strategy");
            string easyFirmware = Get<string>("/gateway/versionFirmware");
            string easyHardware = Get<string>("/gateway/versionHardware");
            string easyUuid = Get<string>("/gateway/uuid");
            string cvSerial = Get<string>("/system/appliance/serialnumber");
            string cvVersion = Get<string>("/system/appliance/version");
            string cvBurner = Get<string>("/system/interfaces/ems/brandbit");
            return new Information(ownerInfo[0], ownerInfo[1], ownerInfo[2], installerInfo[0], installerInfo[1], installerInfo[2],
                easySerial, easyUpdate, easyFirmware, easyHardware, easyUuid, cvSerial, cvVersion, cvBurner);
        }

        public SystemSettings? GetSystemSettings()
        {
            bool desinfect = Get<string>("/dhwCircuits/dhwA/thermaldesinfect/state") == "on";

            int nextTermalTime = Get<int>("/dhwCircuits/dhwA/thermaldesinfect/time");
            string nextTermalDay = Get<string>("/dhwCircuits/dhwA/thermaldesinfect/weekday");
            string sensitivity = Get<string>("/ecus/rrc/pirSensitivity");
            string tempStep = Get<string>("/ecus/rrc/temperaturestep");
            double adjustment = Get<double>("/heatingCircuits/hc1/temperatureAdjustment");
            return new SystemSettings(desinfect, Utils.GetNextDate(nextTermalDay, nextTermalTime), sensitivity, Utils.StringToDouble(tempStep), adjustment);
        }

        public bool SetUserMode(UserModes newMode)
        {
            return Put("/heatingCircuits/hc1/usermode", "{\"value\":" + newMode.ToString().ToLower() + "}");
        }

        public bool SetTemperature(double temperature)
        {
            bool result = Put("/heatingCircuits/hc1/temperatureRoomManual", "{\"value\":" + Utils.DoubleToString(temperature) + "}");
            if (result)
            {
                result = Put("/heatingCircuits/hc1/manualTempOverride/status", "{\"value\":'on'}");
            }
            if (result)
            {
                result = Put("/heatingCircuits/hc1/manualTempOverride/temperature", "{\"value\":" + Utils.DoubleToString(temperature) + "}");
            }
            return result;
        }

        #endregion
        
#if !NET20 && !NET35

        #region Async commands

        public async Task ConnectAsync()
        {
            await Task.Run(() =>
            {
                Connect();
            });
        }

        public async Task<UserProgram> GetProgramAsync()
        {
            return await Task.Run(() =>
            {
                return GetProgram();
            });
        }

        public async Task<Information?> GetInformationAsync()
        {
            return await Task.Run(() =>
            {
                return GetInformation();
            });
        }

        public async Task<UIStatus> GetUIStatusAsync()
        {
            return await Task.Run(() =>
            {
                return GetUIStatus();
            });
        }

        public async Task<FullStatus> GetFullStatusAsync()
        {
            return await Task.Run(() =>
            {
                return GetStatus();
            });
        }

        public async Task<SystemSettings?> GetSystemSettingsAsync()
        {
            return await Task.Run(() =>
            {
                return GetSystemSettings();
            });
        }

        public async Task<Location?> GetLocationAsync()
        {
            return await Task.Run(() =>
            {
                return GetLocation();
            });
        }

        public async Task<GasSample[]> GetGasusageAsync()
        {
            return await Task.Run(() =>
            {
                return GetGasusage();
            });
        }

        public async Task<bool> SetTemperatureAsync(double temperature)
        {
            return await Task.Run(() =>
            {
                return SetTemperature(temperature);
            });
        }

        public async Task<bool> SetUserModeAsync(UserModes newMode)
        {
            return await Task.Run(() =>
            {
                return SetUserMode(newMode);
            });
        }

        #endregion

#endif

        #endregion
    }
}
