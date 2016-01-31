using NefitSharp.Entities.Internal;

namespace NefitSharp.Entities
{
    public class UIStatus
    {
        public ClockProgram ClockProgram { get; }
        public InHouseStatus InHouseStatus { get; }
        public double InHouseTemperature { get; }
        public BoilerIndicator BoilerIndicator { get; }
        public ControlMode Control { get; }
        public double TempOverrideDuration { get; }
        public double CurrentSwitchpoint { get; }
        public bool PowerSaveMode { get; }
        public bool FireplaceMode { get; }
        public bool TempOverride { get; }
        public bool HolidayMode { get; }
        public bool BoilerBlock { get; }
        public bool BoilerLock { get; }
        public bool BoilerMaintenance { get; }
        public double TemperatureSetpoint { get; }
        public double TemperatureOverrideSetpoint { get; }
        public double TemparatureManualSetpoint { get; }
        public bool HedEnabled { get; }
        public bool HedDeviceAtHome { get; }
        public UserModes UserMode { get; }
        internal UIStatus(NefitStatus stat)
        {
            switch (stat.UMD.ToLower())
            {
                case "clock":
                    UserMode = UserModes.Clock;
                    break;

                case "manual":
                    UserMode = UserModes.Manual;
                    break;
                default:
                    UserMode = UserModes.Unknown;
                    break;
            }
            switch (stat.CPM.ToLower())
            {
                case "selflearning":
                    ClockProgram = ClockProgram.SelfLearning;
                    break;
                case "auto":
                    ClockProgram = ClockProgram.Auto;
                    break;
                default:
                    ClockProgram = ClockProgram.Unknown;
                    break;
            }
            switch (stat.IHS.ToLower())
            {
                case "ok":
                    InHouseStatus = InHouseStatus.Ok;
                    break;

                default:
                    InHouseStatus = InHouseStatus.Unknown;
                    break;
            }
            switch (stat.CTR.ToLower())
            {
                case "room":
                    Control = ControlMode.Room;
                    break;

                default:
                    Control = ControlMode.Unknown;
                    break;
            }
            switch (stat.BAI.ToLower())
            {
                case "no":
                    BoilerIndicator = BoilerIndicator.Off;
                    break;

                case "ch":
                    BoilerIndicator = BoilerIndicator.CentralHeating;
                    break;
                case "hw":
                    BoilerIndicator = BoilerIndicator.HotWater;
                    break;


                default:
                    BoilerIndicator = BoilerIndicator.Unknown;
                    break;
            }
            InHouseTemperature = Utils.StringToDouble(stat.IHT);
            TempOverrideDuration = Utils.StringToDouble(stat.TOD);
            CurrentSwitchpoint = Utils.StringToDouble(stat.CSP);
            PowerSaveMode = stat.ESI == "on";
            FireplaceMode = stat.FPA == "on";
            TempOverride = stat.TOR == "on";
            HolidayMode = stat.HMD == "on";
            BoilerBlock = stat.BBE == "on";
            BoilerLock = stat.BLE == "on";
            BoilerMaintenance = stat.BMR == "on";
            TemperatureSetpoint = Utils.StringToDouble(stat.TSP);
            TemperatureOverrideSetpoint = Utils.StringToDouble(stat.TOT);
            TemparatureManualSetpoint = Utils.StringToDouble(stat.MMT);
            HedEnabled = stat.HED_EN == "on";
            HedDeviceAtHome = stat.HED_DEV == "on";         
        }
    }
}