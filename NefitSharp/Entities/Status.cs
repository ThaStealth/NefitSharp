namespace NefitSharp.Entities
{
    public struct Status
    {
        private string _userMode;
        private string _clockProgram;
        private string _inHouseStatus;
        private int _inHouseTemperature;
        private string _BoilerIndicator;
        private string _control;
        private int _tempOverrideDuration;
        private int _currentSwitchpoint;
        private bool _powerSaveMode;
        private bool _fireplaceMode;
        private bool _tempOverride;
        private bool _holidayMode;
        private bool _boilerBlock;
        private bool _boilerLock;
        private bool _boilerMaintenance;
        private int _temperatureSetpoint;
        private int _temperatureOverrideSetpoint;
        private int _temparatureManualSetpoint;
        private bool _hedEnabled;
        private bool _hedDeviceAtHome;
        private string _outdoorTemperature;
        private string _outdoorTemperatureSource;

        public string UserMode
        {
            get { return _userMode; }
        }

        public string ClockProgram
        {
            get { return _clockProgram; }
        }

        public string InHouseStatus
        {
            get { return _inHouseStatus; }
        }

        public int InHouseTemperature
        {
            get { return _inHouseTemperature; }
        }

        public string BoilerIndicator
        {
            get { return _BoilerIndicator; }
        }

        public string Control
        {
            get { return _control; }
        }

        public int TempOverrideDuration
        {
            get { return _tempOverrideDuration; }
        }

        public int CurrentSwitchpoint
        {
            get { return _currentSwitchpoint; }
        }

        public bool PowerSaveMode
        {
            get { return _powerSaveMode; }
        }

        public bool FireplaceMode
        {
            get { return _fireplaceMode; }
        }

        public bool TempOverride
        {
            get { return _tempOverride; }
        }

        public bool HolidayMode
        {
            get { return _holidayMode; }
        }

        public bool BoilerBlock
        {
            get { return _boilerBlock; }
        }

        public bool BoilerLock
        {
            get { return _boilerLock; }
        }

        public bool BoilerMaintenance
        {
            get { return _boilerMaintenance; }
        }

        public int TemperatureSetpoint
        {
            get { return _temperatureSetpoint; }
        }

        public int TemperatureOverrideSetpoint
        {
            get { return _temperatureOverrideSetpoint; }
        }

        public int TemparatureManualSetpoint
        {
            get { return _temparatureManualSetpoint; }
        }

        public bool HedEnabled
        {
            get { return _hedEnabled; }
        }

        public bool HedDeviceAtHome
        {
            get { return _hedDeviceAtHome; }
        }

        public string OutdoorTemperature
        {
            get { return _outdoorTemperature; }
        }

        public string OutdoorTemperatureSource
        {
            get { return _outdoorTemperatureSource; }
        }

        internal Status(string userMode, string clockProgram, string inHouseStatus, int inHouseTemperature, string boilerIndicator, string control, int tempOverrideDuration, int currentSwitchpoint, bool powerSaveMode, bool fireplaceMode, bool tempOverride, bool holidayMode, bool boilerBlock, bool boilerLock, bool boilerMaintenance, int temperatureSetpoint, int temperatureOverrideSetpoint, int temparatureManualSetpoint, bool hedEnabled, bool hedDeviceAtHome, string outdoorTemperature, string outdoorTemperatureSource)
        {
            _userMode = userMode;
            _clockProgram = clockProgram;
            _inHouseStatus = inHouseStatus;
            _inHouseTemperature = inHouseTemperature;
            _BoilerIndicator = boilerIndicator;
            _control = control;
            _tempOverrideDuration = tempOverrideDuration;
            _currentSwitchpoint = currentSwitchpoint;
            _powerSaveMode = powerSaveMode;
            _fireplaceMode = fireplaceMode;
            _tempOverride = tempOverride;
            _holidayMode = holidayMode;
            _boilerBlock = boilerBlock;
            _boilerLock = boilerLock;
            _boilerMaintenance = boilerMaintenance;
            _temperatureSetpoint = temperatureSetpoint;
            _temperatureOverrideSetpoint = temperatureOverrideSetpoint;
            _temparatureManualSetpoint = temparatureManualSetpoint;
            _hedEnabled = hedEnabled;
            _hedDeviceAtHome = hedDeviceAtHome;
            _outdoorTemperature = outdoorTemperature;
            _outdoorTemperatureSource = outdoorTemperatureSource;
        }
    }
}