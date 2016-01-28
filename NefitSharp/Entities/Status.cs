using System;
using System.Threading;
using NefitSharp.Entities.Internal;

namespace NefitSharp.Entities
{
    public struct Information
    {
        public string OwnerName { get; }
        public string OwnerCellNumber { get; }
        public string OwnerEmail { get; }
        public string InstallerName { get; }
        public string InstallerCellNumber { get; }
        public string InstallerEmail { get; }

        public string EasySerialNumber { get; }
        public string EasyUpdateStrategy { get; }
        public string EasyFirmwareVersion { get; }
        public string EasyHardwareVersion { get; }
        public string EasuUUID { get; }

        public string CVSerial { get; }
        public string CVVersion { get; }
        public string CVBurnerType { get; }

        public Information(string ownerName, string ownerCellNumber, string ownerEmail, string installerName, string installerCellNumber, string installerEmail, string easySerialNumber, string easyUpdateStrategy, string easyFirmwareVersion, string easyHardwareVersion, string easuUuid, string cvSerial, string cvVersion, string cvBurnerType)
        {
            OwnerName = ownerName;
            OwnerCellNumber = ownerCellNumber;
            OwnerEmail = ownerEmail;
            InstallerName = installerName;
            InstallerCellNumber = installerCellNumber;
            InstallerEmail = installerEmail;
            EasySerialNumber = easySerialNumber;
            EasyUpdateStrategy = easyUpdateStrategy;
            EasyFirmwareVersion = easyFirmwareVersion;
            EasyHardwareVersion = easyHardwareVersion;
            EasuUUID = easuUuid;
            CVSerial = cvSerial;
            CVVersion = cvVersion;
            CVBurnerType = cvBurnerType;
        }
    }


    public struct Status
    {
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

        internal Status(NefitStatus stat,string serviceStatus,double outdoorTemp)
        {
            UserMode = stat.UMD;
            ServiceStatus = serviceStatus;
            ClockProgram = stat.CPM;
            InHouseStatus = stat.IHS;
            InHouseTemperature = Convert.ToDouble(stat.IHT.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            BoilerIndicator = stat.BAI;
            Control = stat.CTR;
            TempOverrideDuration = Convert.ToDouble(stat.TOD.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            CurrentSwitchpoint = Convert.ToDouble(stat.CSP.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            PowerSaveMode = stat.ESI=="on";
            FireplaceMode = stat.FPA == "on";
            TempOverride = stat.TOR == "on";
            HolidayMode = stat.HMD == "on";
            BoilerBlock = stat.BBE == "on";
            BoilerLock = stat.BLE == "on";
            BoilerMaintenance = stat.BMR == "on";
            TemperatureSetpoint = Convert.ToDouble(stat.TSP.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            TemperatureOverrideSetpoint = Convert.ToDouble(stat.TOT.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            TemparatureManualSetpoint = Convert.ToDouble(stat.MMT.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            HedEnabled = stat.HED_EN == "on";
            HedDeviceAtHome = stat.HED_DEV == "on";
            OutdoorTemperature =outdoorTemp;
            OutdoorTemperatureSource = "unknown";
        }
    }
}