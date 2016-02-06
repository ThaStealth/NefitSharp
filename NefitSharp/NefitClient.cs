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

        private const int cRequestTimeout = 35*1000;
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
                Disconnect();
            }
            return Connected;

        }

#if !NET20 && !NET35

        /// <summary>        
        /// Starts the communication to the Bosch server backend with the credentials provided in the constructor        
        /// </summary>     
        ///  <returns>When returns false, check <see cref="PasswordValid"/> and <see cref="SerialAccessKeyValid"/> to see if there was an authentication problem</returns>  
        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() => { return Connect(); });
        }
#endif

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
                    else if (xmlDoc.DocumentElement != null && xmlDoc.DocumentElement.Name == "presence")
                    {
                        _readyForCommands = true;
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

        #region Programs
        /// <summary>
        /// Returns the active user program, can only be 0, 1 or 2
        /// Use this in the <see cref="Program"/> command to get the active program
        /// </summary>
        /// <returns>A value between 0 and 2, or <see cref="int.MinValue"/> if the command fails</returns>
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

        /// <summary>
        /// Returns the requested user program defined in switch points
        /// </summary>
        /// <param name="programNumber">The program number which to request from the Easy</param>
        /// <returns>An array of ProgramSwitches (converted to the next timestamp of the switch) or null if the command fails</returns>
        public ProgramSwitch[] Program(int programNumber)
        {
            try
            {
                if (programNumber >= 0 && programNumber < 3)
                {
                    NefitProgram[] program0 = Get<NefitProgram[]>("/ecus/rrc/userprogram/program" + programNumber);
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

        /// <summary>
        /// Returns the user definable switch point names, there are 2 custom names configurable
        /// </summary>
        /// <param name="nameIndex">The index of the name, can only be 0 or 1</param>
        /// <returns>The name of the switchpoint or null if the command fails</returns>
        public string SwitchpointName(int nameIndex)
        {
            try
            {
                nameIndex--;
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

        #endregion

        /// <summary>
        /// Indicates if the fireplace function is currently activated
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? FireplaceFunctionActive()
        {
            try
            {
                return Get<string>("/ecus/rrc/userprogram/preheating") == "on";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Indicates if the preheating setting is active
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? PreheatingActive()
        {
            try
            {
                return Get<string>("/ecus/rrc/userprogram/preheating") == "on";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Returns the outdoor temperature measured by the Easy or collected over the internet
        /// </summary>
        /// <returns>The outdoor temperature or <see cref="Double.NaN"/> if the command fails</returns>
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

        /// <summary>
        /// Inidicates the Easy service status
        /// </summary>
        /// <returns>Returns a string containing the Easy service status or null if the command fails</returns>
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

        /// <summary>
        /// Returns the status of the ignition (presumably if the CV is heating)
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? IgnitionStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/ignition/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Returns the status of the CV circuit, if a refill is needed
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? RefillNeededStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/refillneeded/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? ClosingValveStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/closingvalve/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Unknown
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? ShortTappingStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/shorttapping/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Indiciates if the Easy detected a leak
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? SystemLeakingStatus()
        {
            try
            {
                return Get<string>("/ecus/rrc/pm/systemleaking/status") == "true";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Indiciates if the thermal desinfect program is enabled
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public bool? ThermalDesinfectEnabled()
        {
            try
            {
                return Get<string>("/dhwCircuits/dhwA/thermaldesinfect/state") == "on";
            }
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Returns the timestamp of the next scheduled thermal desinfect.
        /// </summary>
        /// <returns>The date/time of the next thermal desinfect or a datetime with 0 ticks if the command fails</returns>
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

        /// <summary>
        /// Returns the current pressure of the CV circuit
        /// </summary>
        /// <returns>The presure of the CV circuit or <see cref="Double.NaN"/> if the command fails</returns>
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

        /// <summary>
        /// Returns the current tempreature of the supply temperature of the CV circuit
        /// </summary>
        /// <returns>The presure of the CV circuit or <see cref="Double.NaN"/> if the command fails</returns>
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

        /// <summary>
        /// Returns the current status of the central heating, note; the descriptions are in Dutch
        /// </summary>
        /// <returns>The current status of the central heating or null if the command fails</returns>
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

        /// <summary>
        /// Returns the current switch point (in other words what the CV is currently doing)
        /// </summary>
        /// <returns>The current switch point of the central heating or null if the command fails</returns>
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

        /// <summary>
        /// Returns the next switch point (in other words what the CV will be doing)
        /// </summary>
        /// <returns>The current switch point of the central heating or null if the command fails</returns>
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

        /// <summary>
        /// Returns the location of the Easy device
        /// </summary>
        /// <returns>Location of the easy device, or null if the command fails</returns>
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

        /// <summary>
        /// Returns all daily gas usage samples collected by the Easy device
        /// </summary>
        /// <returns>An array of gas samples,or null if the command fails</returns>
        public GasSample[] GetGasusage()
        {
            bool hasValidSamples = true;
            int currentPage = 1;

            try
            {
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
                                gasSamples.Add(new GasSample(Convert.ToDateTime(sample.d), sample.hw/10.0, sample.ch/10.0,sample.T/10.0));
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
            catch (Exception e)
            {
                ExceptionEvent(e);
            }
            return null;
        }

        /// <summary>
        /// Returns the overall status presented in the UI
        /// </summary>
        /// <returns>The UI status, or null if the command fails</returns>
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

        /// <summary>
        /// Returns the owner information filled in on the Easy app
        /// </summary>
        /// <returns>Returns a array of the following items: Name/Phone number/Email address, or null if the command fails</returns>
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

        /// <summary>
        /// Returns the installer information filled in on the Easy app
        /// </summary>
        /// <returns>Returns a array of the following items: Name/Company/Telephone number/Email address, or null if the command fails</returns>
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

        /// <summary>
        /// Returns the serial number of the Nefit Easy thermostat, this is not the serial number you enter for communication
        /// Use <see cref="EasyUUID"/> for that
        /// </summary>
        /// <returns>The serial number of the Nefit Easy thermostat, or null if the command fails</returns>
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
        
        /// <summary>
        /// Returns the firmware version of the Nefit Easy thermostat
        /// </summary>
        /// <returns>Firmware version or null if the command fails</returns>
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

        /// <summary>
        /// Returns the hardware revision of the Nefit Easy thermostat
        /// </summary>
        /// <returns>Hardware revision or null if the command fails</returns>
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

        /// <summary>
        /// Returns the UUID of the Easy thermostat, this is the number you enter in as serial when connecting
        /// </summary>
        /// <returns>The UUID of the Nefit Easy thermostat, or null if the command fails</returns>
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
        /// Returns the way the Easy is updated
        /// </summary>       
        /// <returns>The way the Easy is updated, or null if the command fails</returns>
        public string EasyUpdateStrategy()
        {
            //TODO: Convert into enum
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

        /// <summary>
        /// Returns the serial of the central heating appliance
        /// </summary>
        /// <returns>The serial of the central heating appliance, or null if the command fails</returns>
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

        /// <summary>
        /// Returns the version of the central heating appliance
        /// </summary>
        /// <returns>The version of the central heating appliance, or null if the command fails</returns>
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

        /// <summary>
        /// Returns the make of the burner in the central heating appliance 
        /// </summary>
        /// <returns>The make of the burner in the central heating appliance, or null if the command fails</returns>
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

        /// <summary>
        /// Returns the proximity setting of the Nefit Easy
        /// </summary>
        /// <returns>The proximity setting of the Easy or null if the command fails</returns>
        public string EasySensitivity()
        {
            //TODO: convert to enum
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

        /// <summary>
        /// Returns the temperature step setting when changing setpoints 
        /// </summary>
        /// <returns>The temperature step setting for setpoints or <see cref="Double.NaN"/> if the command fails</returns>
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

        /// <summary>
        /// Returns the room temperature offset setting used by the Nefit Easy 
        /// </summary>
        /// <returns>The room temperature offset setting or <see cref="Double.NaN"/> if the command fails</returns>
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

        /// <summary>
        /// Changes the hot water mode to on or off when the Easy is in clock program mode
        /// </summary>
        /// <param name="onOff">True if the hotwater needs to be turned on, false if off</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
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

        /// <summary>
        /// Changes the hot water mode to on or off when the Easy is in manual program mode
        /// </summary>
        /// <param name="onOff">True if the hotwater needs to be turned on, false if off</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
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

        /// <summary>
        /// Switches the Easy between manual and program mode
        /// </summary>
        /// <param name="newMode">Use <see cref="UserModes.Manual"/> to switch to manual mode, use <see cref="UserModes.Clock"/> to switch to program mode, <see cref="UserModes.Unknown"/> is not supported</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
        public bool SetUserMode(UserModes newMode)
        {
            if (newMode != UserModes.Unknown)
            {
                try
                {
                    return Put("/heatingCircuits/hc1/usermode", "{\"value\":" + newMode.ToString().ToLower() + "}");
                }
                catch (Exception e)
                {
                    ExceptionEvent(e);
                }
            }
            return false;
        }

        /// <summary>
        /// Changes the setpoint temperature
        /// </summary>
        /// <param name="temperature">The new temperature (in degrees celcius). The new setpoint must be between 5 and 30 degrees celcius</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
        public bool SetTemperature(double temperature)
        {
            if (temperature >= 5 && temperature <= 30)
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
            }
            return false;
        }

        #endregion

#if !NET20 && !NET35

        #region Async commands    

        /// <summary>
        /// Returns the active user program, can only be 0, 1 or 2
        /// Use this in the <see cref="Program"/> command to get the active program
        /// </summary>
        /// <returns>A value between 0 and 2, or <see cref="int.MinValue"/> if the command fails</returns>
        public async Task<int> ActiveProgramAsync()
        {
            return await Task.Run(() => { return ActiveProgram(); });
        }
        /// <summary>
        /// Returns the requested user program defined in switch points
        /// </summary>
        /// <param name="programNumber">The program number which to request from the Easy</param>
        /// <returns>An array of ProgramSwitches (converted to the next timestamp of the switch) or null if the command fails</returns>
        public async Task<ProgramSwitch[]> ProgramAsync(int index)
        {
            return await Task.Run(() => { return Program(index); });
        }
        /// <summary>
        /// Returns the user definable switch point names, there are 2 custom names configurable
        /// </summary>
        /// <param name="nameIndex">The index of the name, can only be 0 or 1</param>
        /// <returns>The name of the switchpoint or null if the command fails</returns>
        public async Task<string> SwitchpointNameAsync(int nameIndex)
        {
            return await Task.Run(() => { return SwitchpointName(nameIndex); });
        }
        /// <summary>
        /// Indicates if the fireplace function is currently activated
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> FireplaceFunctionActiveAsync()
        {
            return await Task.Run(() => { return FireplaceFunctionActive(); });
        }
        /// <summary>
        /// Indicates if the preheating setting is active
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> PreheatingActiveAsync()
        {
            return await Task.Run(() => { return PreheatingActive(); });
        }
        /// <summary>
        /// Returns the outdoor temperature measured by the Easy or collected over the internet
        /// </summary>
        /// <returns>The outdoor temperature or <see cref="Double.NaN"/> if the command fails</returns>
        public async Task<double> OutdoorTemperatureAsync()
        {
            return await Task.Run(() => { return OutdoorTemperature(); });
        }

        /// <summary>
        /// Inidicates the Easy service status
        /// </summary>
        /// <returns>Returns a string containing the Easy service status or null if the command fails</returns>
        public async Task<string> EasyServiceStatusAsync()
        {
            return await Task.Run(() => { return EasyServiceStatus(); });
        }

        /// <summary>
        /// Returns the status of the ignition (presumably if the CV is heating)
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> IgnitionStatusAsync()
        {
            return await Task.Run(() => { return IgnitionStatus(); });
        }
        /// <summary>
        /// Returns the status of the CV circuit, if a refill is needed
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> RefillNeededStatusAsync()
        {
            return await Task.Run(() => { return RefillNeededStatus(); });
        }
        /// <summary>
        /// Unknown
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> ClosingValveStatusAsync()
        {
            return await Task.Run(() => { return ClosingValveStatus(); });
        }
        /// <summary>
        /// Unknown
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> ShortTappingStatusAsync()
        {
            return await Task.Run(() => { return ShortTappingStatus(); });
        }
        /// <summary>
        /// Indiciates if the Easy detected a leak
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> SystemLeakingStatusAsync()
        {
            return await Task.Run(() => { return SystemLeakingStatus(); });
        }
        /// <summary>
        /// Returns the current pressure of the CV circuit
        /// </summary>
        /// <returns>The presure of the CV circuit or <see cref="Double.NaN"/> if the command fails</returns>
        public async Task<double> SystemPressureAsync()
        {
            return await Task.Run(() => { return SystemPressure(); });
        }
        /// <summary>
        /// Returns the current tempreature of the supply temperature of the CV circuit
        /// </summary>
        /// <returns>The presure of the CV circuit or <see cref="Double.NaN"/> if the command fails</returns>
        public async Task<double> CentralHeatingSupplyTemperatureAsync()
        {
            return await Task.Run(() => { return CentralHeatingSupplyTemperature(); });
        }
        /// <summary>
        /// Returns the current status of the central heating, note; the descriptions are in Dutch
        /// </summary>
        /// <returns>The current status of the central heating or null if the command fails</returns>
        public async Task<StatusCode> GetStatusCodeAsync()
        {
            return await Task.Run(() => { return GetStatusCode(); });
        }
        /// <summary>
        /// Returns the current switch point (in other words what the CV is currently doing)
        /// </summary>
        /// <returns>The current switch point of the central heating or null if the command fails</returns>
        public async Task<ProgramSwitch> GetCurrentSwitchPointAsync()
        {
            return await Task.Run(() => { return GetCurrentSwitchPoint(); });
        }
        /// <summary>
        /// Returns the next switch point (in other words what the CV will be doing)
        /// </summary>
        /// <returns>The current switch point of the central heating or null if the command fails</returns>
        public async Task<ProgramSwitch> GetNextSwitchPointAsync()
        {
            return await Task.Run(() => { return GetNextSwitchPoint(); });
        }
        /// <summary>
        /// Returns the location of the Easy device
        /// </summary>
        /// <returns>Location of the easy device, or null if the command fails</returns>
        public async Task<Location> GetLocationAsync()
        {
            return await Task.Run(() => { return GetLocation(); });
        }
        /// <summary>
        /// Returns all daily gas usage samples collected by the Easy device
        /// </summary>
        /// <returns>An array of gas samples,or null if the command fails</returns>
        public async Task<GasSample[]> GetGasusageAsync()
        {
            return await Task.Run(() => { return GetGasusage(); });
        }
        /// <summary>
        /// Returns the overall status presented in the UI
        /// </summary>
        /// <returns>The UI status, or null if the command fails</returns>
        public async Task<UIStatus> GetUIStatusAsync()
        {
            return await Task.Run(() => { return GetUIStatus(); });
        }
        /// <summary>
        /// Returns the owner information filled in on the Easy app
        /// </summary>
        /// <returns>Returns a array of the following items: Name/Phone number/Email address, or null if the command fails</returns>
        public async Task<string[]> OwnerInfoAsync()
        {
            return await Task.Run(() => { return OwnerInfo(); });
        }
        /// <summary>
        /// Returns the installer information filled in on the Easy app
        /// </summary>
        /// <returns>Returns a array of the following items: Name/Company/Telephone number/Email address, or null if the command fails</returns>
        public async Task<string[]> InstallerInfoAsync()
        {
            return await Task.Run(() => { return InstallerInfo(); });
        }

        /// <summary>
        /// Returns the serial number of the Nefit Easy thermostat, this is not the serial number you enter for communication
        /// Use <see cref="EasyUUID"/> for that
        /// </summary>
        /// <returns>The serial number of the Nefit Easy thermostat, or null if the command fails</returns>
        public async Task<string> EasySerialAsync()
        {
            return await Task.Run(() => { return EasySerial(); });
        }
        /// <summary>
        /// Returns the firmware version of the Nefit Easy thermostat
        /// </summary>
        /// <returns>Firmware version or null if the command fails</returns>
        public async Task<string> EasyFirmwareAsync()
        {
            return await Task.Run(() => { return EasyFirmware(); });
        }
        /// <summary>
        /// Returns the hardware revision of the Nefit Easy thermostat
        /// </summary>
        /// <returns>Hardware revision or null if the command fails</returns>
        public async Task<string> EasyHardwareAsync()
        {
            return await Task.Run(() => { return EasyHardware(); });
        }
        /// <summary>
        /// Returns the UUID of the Easy thermostat, this is the number you enter in as serial when connecting
        /// </summary>
        /// <returns>The UUID of the Nefit Easy thermostat, or null if the command fails</returns>
        public async Task<string> EasyUUIDAsync()
        {
            return await Task.Run(() => { return EasyUUID(); });
        }
        /// <summary>       
        /// Returns the way the Easy is updated
        /// </summary>       
        /// <returns>The way the Easy is updated, or null if the command fails</returns>
        public async Task<string> EasyUpdateStrategyAsync()
        {
            return await Task.Run(() => { return EasyUpdateStrategy(); });
        }
        /// <summary>
        /// Returns the serial of the central heating appliance
        /// </summary>
        /// <returns>The serial of the central heating appliance, or null if the command fails</returns>
        public async Task<string> CVSerialAsync()
        {
            return await Task.Run(() => { return CVSerial(); });
        }
        /// <summary>
        /// Returns the version of the central heating appliance
        /// </summary>
        /// <returns>The version of the central heating appliance, or null if the command fails</returns>
        public async Task<string> CVVersionAsync()
        {
            return await Task.Run(() => { return CVVersion(); });
        }
        /// <summary>
        /// Returns the make of the burner in the central heating appliance 
        /// </summary>
        /// <returns>The make of the burner in the central heating appliance, or null if the command fails</returns>
        public async Task<string> CVBurnerMakeAsync()
        {
            return await Task.Run(() => { return CVBurnerMake(); });
        }
        /// <summary>
        /// Indiciates if the thermal desinfect program is enabled
        /// </summary>
        /// <returns>True/false or null if the command fails</returns>
        public async Task<bool?> ThermalDesinfectEnabledAsync()
        {
            return await Task.Run(() => { return ThermalDesinfectEnabled(); });
        }
        /// <summary>
        /// Returns the timestamp of the next scheduled thermal desinfect.
        /// </summary>
        /// <returns>The date/time of the next thermal desinfect or a datetime with 0 ticks if the command fails</returns>
        public async Task<DateTime> NextThermalDesinfectAsync()
        {
            return await Task.Run(() => { return NextThermalDesinfect(); });
        }
        /// <summary>
        /// Returns the proximity setting of the Nefit Easy
        /// </summary>
        /// <returns>The proximity setting of the Easy or null if the command fails</returns>
        public async Task<string> EasySensitivityAsync()
        {
            return await Task.Run(() => { return EasySensitivity(); });
        }
        /// <summary>
        /// Returns the temperature step setting when changing setpoints 
        /// </summary>
        /// <returns>The temperature step setting for setpoints or <see cref="Double.NaN"/> if the command fails</returns>
        public async Task<double> EasyTemperatureStepAsync()
        {
            return await Task.Run(() => { return EasyTemperatureStep(); });
        }
        /// <summary>
        /// Returns the room temperature offset setting used by the Nefit Easy 
        /// </summary>
        /// <returns>The room temperature offset setting or <see cref="Double.NaN"/> if the command fails</returns>
        public async Task<double> EasyTemperatureOffsetAsync()
        {
            return await Task.Run(() => { return EasyTemperatureOffset(); });
        }
        /// <summary>
        /// Changes the hot water mode to on or off when the Easy is in clock program mode
        /// </summary>
        /// <param name="onOff">True if the hotwater needs to be turned on, false if off</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
        public async Task<bool> SetHotWaterModeClockProgramAsync(bool onOff)
        {
            return await Task.Run(() => { return SetHotWaterModeClockProgram(onOff); });
        }
        /// <summary>
        /// Changes the hot water mode to on or off when the Easy is in manual program mode
        /// </summary>
        /// <param name="onOff">True if the hotwater needs to be turned on, false if off</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
        public async Task<bool> SetHotWaterModeManualProgramAsync(bool onOff)
        {
            return await Task.Run(() => { return SetHotWaterModeManualProgram(onOff); });
        }
        /// <summary>
        /// Switches the Easy between manual and program mode
        /// </summary>
        /// <param name="newMode">Use <see cref="UserModes.Manual"/> to switch to manual mode, use <see cref="UserModes.Clock"/> to switch to program mode, <see cref="UserModes.Unknown"/> is not supported</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
        public async Task<bool> SetUserModeAsync(UserModes newMode)
        {
            return await Task.Run(() => { return SetUserMode(newMode); });
        }
        /// <summary>
        /// Changes the setpoint temperature
        /// </summary>
        /// <param name="temperature">The new temperature (in degrees celcius). The new setpoint must be between 5 and 30 degrees celcius</param>
        /// <returns>True if the command succeeds, false if it fails</returns>
        public async Task<bool> SetTemperatureAsync(double temperature)
        {
            return await Task.Run(() => { return SetTemperature(temperature); });
        }

        # endregion

#endif

        #endregion
    }
}