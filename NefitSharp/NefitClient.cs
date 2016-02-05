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
    public delegate void ExceptionDelegate(Exception e);
    public delegate void CommunicationLogDelegate(string text);

    public class NefitClient
    {
        private const string cHost = "wa2-mz36-qrmzh6.bosch.de";
        private const string cAccesskeyPrefix = "Ct7ZR03b_";

        private const string cRrcContactPrefix = "rrccontact_";
        private const string cRrcGatewayPrefix = "rrcgateway_";

        private const int cRequestTimeout = 5*1000;
        private const int cCheckInterval = 100;
        private const int cKeepAliveInterval = 30*1000;

        private XmppClientConnection _client;
        private readonly NefitEncryption _encryptionHelper;
        private readonly string _accessKey;

        private readonly string _serial;
        private NefitHTTPResponse _lastMessage;
        private readonly object _lockObj;
        private readonly object _communicationLock;
        private bool _serialAccessKeyValid;
        private bool _passwordValid;
        private bool _readyForCommands;

        /// <summary>      
        /// Subscribe to this event if you want to get notified if an internal exception occurs.        
        /// </summary>        
        public event ExceptionDelegate ExceptionEvent = delegate {};

        public event CommunicationLogDelegate XMLLog = delegate { }; 

        /// <summary>        
        /// Indicates if there was an general authentication error                
        /// </summary>        

        public bool AuthenticationError
        {
            get { return !_serialAccessKeyValid || !_passwordValid; }
        }

        /// <summary>        
        /// Indicates if the used serial and access keys are valid        
        /// </summary> 
        public bool SerialAccessKeyValid
        {
            get { return _serialAccessKeyValid; }
        }

        /// <summary>        
        /// Indicates if the used password is valid        
        /// </summary>     
        public bool PasswordValid
        {
            get { return _passwordValid; }
        }

        /// <summary>        
        /// Indicates if the connection has been successfully established        
        /// Use <see cref="AuthenticationError"/>  to check if the authentication is also valid.        
        /// </summary>   
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

        /// <summary>      
        ///  Constructor of the NefitClient     
        /// </summary>      
        ///  <param name="serial">The serial of your Nefit Easy (see your manual)</param>    
        ///  <param name="accesskey">The serial of your Nefit Easy (see your manual)</param>   
        ///  <param name="password">The serial of your Nefit Easy (which you configured in the App)</param>   
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

        /// <summary>        
        /// Starts the communication to the Bosch server backend with the credentials provided in the constructor        
        /// </summary>     
        ///  <returns>When returns false, check <see cref="PasswordValid"/> and <see cref="SerialAccessKeyValid"/> to see if there was an authentication problem</returns>  
        public bool Connect()
        {
            if (_client != null)
            {
                Disconnect();
            }
            try
            {
                _serialAccessKeyValid = true;
                _passwordValid = true;
                _readyForCommands = false;
                _client = new XmppClientConnection(cHost);                
                _client.Open(cRrcContactPrefix + _serial, cAccesskeyPrefix + _accessKey);              
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
                ExceptionEvent(e);
            }
            while (!Connected && SerialAccessKeyValid)
            {
                Thread.Sleep(10);
            }            
            if (EasyUUID() != _serial)         
            {
                 Disconnect	();
            }
            return Connected;
        }

        /// <summary>        
        /// Disconnects from the Bosch server       
        /// </summary>     
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
                ExceptionEvent(e);
            }
        }

        private void XmppRead(object sender, string xml)
        {
            XMLLog("XML << " + xml);
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
                        _serialAccessKeyValid = false;
                        Disconnect();
                    }
                }
                catch (Exception e)
                {
                    ExceptionEvent(e);
                    Disconnect();
                }
            }
        }

        private void XmppWrite(object sender, string xml)
        {
            XMLLog("XML >> " + xml);
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
                catch (Exception e)
                {
                    ExceptionEvent(e);
                    Disconnect();
                }
            }
        }

        private bool Put(string url, string data)
        {
            lock (_communicationLock)
            {
                
                try
                {
                    NefitHTTPRequest request = new NefitHTTPRequest(url, _client.MyJID, cRrcGatewayPrefix + _serial + "@" + cHost, _encryptionHelper.Encrypt(data));
                    _client.Send(request.ToString());
                    XMLLog(">> " + request.ToString());
                    int timeout = cRequestTimeout;
                    while (timeout > 0)
                    {
                        lock (_lockObj)
                        {
                            if (_lastMessage != null)
                            {
                                XMLLog("<< " + _lastMessage.Code);
                                bool result = _lastMessage.Code == 204 || _lastMessage.Code == 200;
                                _lastMessage = null;
                                return result;
                            }
                        }
                        timeout -= cCheckInterval;
                        Thread.Sleep(cCheckInterval);
                    }
                }
                catch (Exception e)
                {
                    ExceptionEvent(e);
                }
                return false;
            }
        }

        private T Get<T>(string url)
        {
            lock (_communicationLock)
            {
                try
                {
                    NefitHTTPRequest request = new NefitHTTPRequest(url, _client.MyJID, cRrcGatewayPrefix + _serial + "@" + cHost);
                    _client.Send(request.ToString());
                    XMLLog(">> " + request.ToString());
                    int timeout = cRequestTimeout;
                    while (timeout > 0)
                    {
                        lock (_lockObj)
                        {
                            if (_lastMessage != null)
                            {
                                string decrypted = _encryptionHelper.Decrypt(_lastMessage.Payload);
                                XMLLog("<< " + decrypted);
                                if (decrypted.StartsWith("{"))
                                {
                                    NefitJson<T> obj = JsonConvert.DeserializeObject<NefitJson<T>>(decrypted);
                                    _lastMessage = null;
                                    if (obj != null)
                                    {
                                        return obj.value;
                                    }
                                }
                                else
                                {
                                    _passwordValid = false;
                                }
                                timeout = 0;
                            }
                        }
                        timeout -= cCheckInterval;
                        Thread.Sleep(cCheckInterval);
                    }
                }
                catch (Exception e)
                {
                    ExceptionEvent(e);
                }
                return default(T);
            }
        }

        #endregion

        #region Commands 

        #region Sync Methods    

        public int ActiveProgram()
        {
            try
            {
                return Get<int>("/ecus/rrc/userprogram/activeprogram");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return int.MinValue;
        }

        public ProgramSwitch[] Program(int index)
        {
            try
            {
                if (index >= 0 && index < 3)
                {
                    NefitProgram[] program0 = Get<NefitProgram[]>("/ecus/rrc/userprogram/program" + index);
                    if (program0 != null)
                    {
                        return Utils.ParseProgram(program0);
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string SwitchpointName(int nameIndex)
        {
            try
            {
                if (nameIndex >= 1 && nameIndex <= 2)
                {
                    return Get<string>("/ecus/rrc/userprogram/userswitchpointname" + nameIndex);
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public bool FireplaceFunctionActive()
        {
            try
            {
                return Get<string>("/ecus/rrc/userprogram/preheating") == "on";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool PreheatingActive()
        {
            try
            {
                return Get<string>("/ecus/rrc/userprogram/preheating") == "on";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public double OutdoorTemperature()
        {
            try
            {
                return Get<double>("/system/sensors/temperatures/outdoor_t1");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return Double.NaN;
        }

        public string EasyServiceStatus()
        {
            try
            {
                return Get<string>("/gateway/remote/servicestate");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public bool IgnitionStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/ignition/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool RefillNeededStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/refillneeded/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool ClosingValveStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/closingvalve/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool ShortTappingStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/shorttapping/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool SystemLeakingStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/systemleaking/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public double SystemPressure()
        {
            try
            {
                return Get<double>("/system/appliance/systemPressure");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return Double.NaN;
        }

        public double CentralHeatingSupplyTemperature()
        {
            try
            {
                return Get<double>("/heatingCircuits/hc1/actualSupplyTemperature");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return Double.NaN;
        }

        public StatusCode GetStatusCode()
        {
            try
            {
                string displayCode = Get<string>("/system/appliance/displaycode");
                string causeCode = Get<string>("/system/appliance/causecode");
                return new StatusCode(displayCode, Convert.ToInt32(causeCode));
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public ProgramSwitch GetCurrentSwitchPoint()
        {
            try
            {
                NefitSwitch[] sp = Get<NefitSwitch[]>("/dhwCircuits/dhwA/dhwCurrentSwitchpoint");
                if (sp != null)
                {
                    return new ProgramSwitch(sp[0]);
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public ProgramSwitch GetNextSwitchPoint()
        {
            try
            {
                NefitSwitch[] sp = Get<NefitSwitch[]>("/dhwCircuits/dhwA/dhwNextSwitchpoint");
                if (sp != null)
                {
                    return new ProgramSwitch(sp[0]);
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public Location GetLocation()
        {
            try
            {
                string lat = Get<string>("/system/location/latitude");
                string lon = Get<string>("/system/location/longitude");
                return new Location(Utils.StringToDouble(lat), Utils.StringToDouble(lon));
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public GasSample[] GetGasusage()
        {
            bool hasValidSamples = true;
            int currentPage = 1;
            List<GasSample> gasSamples = new List<GasSample>();
            try
            {
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
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return gasSamples.ToArray();
        }

        public UIStatus GetUIStatus()
        {
            try
            {
                NefitStatus status = Get<NefitStatus>("/ecus/rrc/uiStatus");
                if (status != null)
                {
                    return new UIStatus(status);
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string[] OwnerInfo()
        {
            try
            {
                string owner = Get<string>("/ecus/rrc/personaldetails");
                if (owner != null)
                {
                    return owner.Split(';');
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string[] InstallerInfo()
        {
            try
            {
                string owner = Get<string>("/ecus/rrc/installerdetails");
                if (owner != null)
                {
                    return owner.Split(';');
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string EasySerial()
        {
            try
            {
                return Get<string>("/gateway/serialnumber");                
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string EasyFirmware()
        {
            try
            {
                return Get<string>("/gateway/versionFirmware");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string EasyHardware()
        {
            try
            {
                return Get<string>("/gateway/versionHardware");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string EasyUUID()
        {
            try
            {
                return Get<string>("/gateway/uuid");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>       
        /// Needs to be converted into an enum   
        /// </summary>       
        /// <returns></returns>
        public string EasyUpdateStrategy()
        {


            try
            {
                return Get<string>("/gateway/update/strategy");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string CVSerial()
        {
            try
            {
                return Get<string>("/system/appliance/serialnumber");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string CVVersion()
        {
            try
            {
                return Get<string>("/system/appliance/version");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public string CVBurnerMake()
        {
            try
            {
                return Get<string>("/system/interfaces/ems/brandbit");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public bool ThermalDesinfectEnabled()
        {
            try
            {
                return Get<string>("/dhwCircuits/dhwA/thermaldesinfect/state") == "on";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public DateTime NextThermalDesinfect()
        {
            try
            {
                int nextTermalTime = Get<int>("/dhwCircuits/dhwA/thermaldesinfect/time");
                string nextTermalDay = Get<string>("/dhwCircuits/dhwA/thermaldesinfect/weekday");
                if (nextTermalDay != null)
                {
                    return Utils.GetNextDate(nextTermalDay, nextTermalTime);
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return new DateTime();
        }

        public string EasySensitivity()
        {
            try
            {
                return Get<string>("/ecus/rrc/pirSensitivity");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        public double EasyTemperatureStep()
        {
            try
            {
                string sens = Get<string>("/ecus/rrc/temperaturestep");
                if (sens != null)
                {
                    return Utils.StringToDouble(sens);
                }
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return Double.NaN;
        }

        public double EasyTemperatureOffset()
        {
            try
            {
                return Get<double>("/heatingCircuits/hc1/temperatureAdjustment");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return Double.NaN;
        }

        public bool SetHotWaterModeClockProgram(bool onOff)
        {
            try
            {
                string newMode = "off";
                if (onOff)
                {
                    newMode = "on";
                }
                return Put("/dhwCircuits/dhwA/dhwOperationClockMode", "{\"value\":'" + newMode + "'}");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool SetHotWaterModeManualProgram(bool onOff)
        {
            try
            {
                string newMode = "off";
                if (onOff)
                {
                    newMode = "on";
                }
                return Put("/dhwCircuits/dhwA/dhwOperationManualMode", "{\"value\":'" + newMode + "'}");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool SetUserMode(UserModes newMode)
        {
            try
            {
                return Put("/heatingCircuits/hc1/usermode", "{\"value\":" + newMode.ToString().ToLower() + "}");
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        public bool SetTemperature(double temperature)
        {
            try
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
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return false;
        }

        #endregion

#if !NET20 && !NET35

        #region Async commands    

        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() =>{return Connect();});
        }

        public async Task<int> ActiveProgramAsync()
        {
            return await Task.Run(() => { return ActiveProgram(); });
        }

        public async Task<ProgramSwitch[]> ProgramAsync(int index)
        {
            return await Task.Run(() => { return Program(index); });
        }

        public async Task<string> SwitchpointNameAsync(int nameIndex)
        {
            return await Task.Run(() => { return SwitchpointName(nameIndex); });
        }

        public async Task<bool> FireplaceFunctionActiveAsync()
        {
            return await Task.Run(() => { return FireplaceFunctionActive(); });
        }

        public async Task<bool> PreheatingActiveAsync()
        {
            return await Task.Run(() => { return PreheatingActive(); });
        }

        public async Task<double> OutdoorTemperatureAsync()
        {
            return await Task.Run(() => { return OutdoorTemperature(); });
        }

        public async Task<string> EasyServiceStatusAsync()
        {
            return await Task.Run(() => { return EasyServiceStatus(); });
        }

        public async Task<bool> IgnitionStatusAsync()
        {
            return await Task.Run(() => { return IgnitionStatus(); });
        }

        public async Task<bool> RefillNeededStatusAsync()
        {
            return await Task.Run(() => { return RefillNeededStatus(); });
        }

        public async Task<bool> ClosingValveStatusAsync()
        {
            return await Task.Run(() => { return ClosingValveStatus(); });
        }

        public async Task<bool> ShortTappingStatusAsync()
        {
            return await Task.Run(() => { return ShortTappingStatus(); });
        }

        public async Task<bool> SystemLeakingStatusAsync()
        {
            return await Task.Run(() => { return SystemLeakingStatus(); });
        }

        public async Task<double> SystemPressureAsync()
        {
            return await Task.Run(() => { return SystemPressure(); });
        }

        public async Task<double> CentralHeatingSupplyTemperatureAsync()
        {
            return await Task.Run(() => { return CentralHeatingSupplyTemperature(); });
        }

        public async Task<StatusCode> GetStatusCodeAsync()
        {
            return await Task.Run(() => { return GetStatusCode(); });
        }

        public async Task<ProgramSwitch> GetCurrentSwitchPointAsync()
        {
            return await Task.Run(() => { return GetCurrentSwitchPoint(); });
        }

        public async Task<ProgramSwitch> GetNextSwitchPointAsync()
        {
            return await Task.Run(() => { return GetNextSwitchPoint(); });
        }

        public async Task<Location> GetLocationAsync()
        {
            return await Task.Run(() => { return GetLocation(); });
        }

        public async Task<GasSample[]> GetGasusageAsync()
        {
            return await Task.Run(() => { return GetGasusage(); });
        }

        public async Task<UIStatus> GetUIStatusAsync()
        {
            return await Task.Run(() => { return GetUIStatus(); });
        }

        public async Task<string[]> OwnerInfoAsync()
        {
            return await Task.Run(() => { return OwnerInfo(); });
        }

        public async Task<string[]> InstallerInfoAsync()
        {
            return await Task.Run(() => { return InstallerInfo(); });
        }

        public async Task<string> EasySerialAsync()
        {
            return await Task.Run(() => { return EasySerial(); });
        }

        public async Task<string> EasyFirmwareAsync()
        {
            return await Task.Run(() => { return EasyFirmware(); });
        }

        public async Task<string> EasyHardwareAsync()
        {
            return await Task.Run(() => { return EasyHardware(); });
        }

        public async Task<string> EasyUUIDAsync()
        {
            return await Task.Run(() => { return EasyUUID(); });
        }

        public async Task<string> EasyUpdateStrategyAsync()
        {
            return await Task.Run(() => { return EasyUpdateStrategy(); });
        }

        public async Task<string> CVSerialAsync()
        {
            return await Task.Run(() => { return CVSerial(); });
        }

        public async Task<string> CVVersionAsync()
        {
            return await Task.Run(() => { return CVVersion(); });
        }

        public async Task<string> CVBurnerMakeAsync()
        {
            return await Task.Run(() => { return CVBurnerMake(); });
        }

        public async Task<bool> ThermalDesinfectEnabledAsync()
        {
            return await Task.Run(() => { return ThermalDesinfectEnabled(); });
        }

        public async Task<DateTime> NextThermalDesinfectAsync()
        {
            return await Task.Run(() => { return NextThermalDesinfect(); });
        }

        public async Task<string> EasySensitivityAsync()
        {
            return await Task.Run(() => { return EasySensitivity(); });
        }

        public async Task<double> EasyTemperatureStepAsync()
        {
            return await Task.Run(() => { return EasyTemperatureStep(); });
        }

        public async Task<double> EasyTemperatureOffsetAsync()
        {
            return await Task.Run(() => { return EasyTemperatureOffset(); });
        }

        public async Task<bool> SetHotWaterModeClockProgramAsync(bool onOff)
        {
            return await Task.Run(() => { return SetHotWaterModeClockProgram(onOff); });
        }

        public async Task<bool> SetHotWaterModeManualProgramAsync(bool onOff)
        {
            return await Task.Run(() => { return SetHotWaterModeManualProgram(onOff); });
        }

        public async Task<bool> SetUserModeAsync(UserModes newMode)
        {
            return await Task.Run(() => { return SetUserMode(newMode); });
        }

        public async Task<bool> SetTemperatureAsync(double temperature)
        {
            return await Task.Run(() => { return SetTemperature(temperature); });
        }

        # endregion

#endif

        #endregion
    }
}