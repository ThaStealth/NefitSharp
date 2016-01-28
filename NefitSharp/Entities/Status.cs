using System;
using System.Threading;
using NefitSharp.Entities.Internal;

namespace NefitSharp.Entities
{
    public struct Settings
    {
        public bool ThermalDesinfectEnabled { get; }
        public DateTime NextThermalDesinfect { get; }
        public string EasyAutoOnSensitivity { get; }
        public double EasyTemperatureStep { get; }
        private double EasyTemperatureAdjustment { get; }

    }

    public struct Status
    {
        public string LastThermalDesinfectResult { get; }

        public string ServiceStatus { get; }
        
        public string UserMode { get; }
               
        public string ClockProgram { get; }
               
        public string InHouseStatus { get; }
               
        public double InHouseTemperature { get; }
               
        public string BoilerIndicator { get; }
               
        public string Control { get; }
               
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
               
        public double OutdoorTemperature { get; }
               
        public string OutdoorTemperatureSource { get; }        
        public string OperationMode { get; }
        public bool RefillNeeded { get; }
        public bool Ignition { get; }
        public bool ClosingValve { get; }
        public bool ShortTapping { get; }
        public bool SystemLeaking { get; }
        public double CentralHeatingSystemPressure { get; }
        public double CentralHeatingSystemSupplyTemperature { get; }

        private StatusCode? CurrentStatus { get; }



        internal Status(NefitStatus stat,string serviceStatus,double outdoorTemp,string operationMode,bool refillNeeded,bool ignition,bool closingValve,bool shortTapping,bool systemLeaking,double centralHeatingSystemPressure, double centralHeatingSystemSupplyTemperature, StatusCode? code)
        {
            UserMode = stat.UMD;
            CurrentStatus = code;
            ServiceStatus = serviceStatus;
            ClockProgram = stat.CPM;
            InHouseStatus = stat.IHS;
            InHouseTemperature = Utils.StringToDouble(stat.IHT);
            BoilerIndicator = stat.BAI;
            Control = stat.CTR;
            TempOverrideDuration = Utils.StringToDouble(stat.TOD);
            CurrentSwitchpoint = Utils.StringToDouble(stat.CSP);
            PowerSaveMode = stat.ESI=="on";
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
            OutdoorTemperature =outdoorTemp;
            OutdoorTemperatureSource = "unknown";
            OperationMode = operationMode;
            RefillNeeded = refillNeeded;
            Ignition = ignition;
            ClosingValve = closingValve;
            ShortTapping = shortTapping;
            SystemLeaking = systemLeaking;
            CentralHeatingSystemPressure = centralHeatingSystemPressure;
            CentralHeatingSystemSupplyTemperature = centralHeatingSystemSupplyTemperature;
        }        
    }
}