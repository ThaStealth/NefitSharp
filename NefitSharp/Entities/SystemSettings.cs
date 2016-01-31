using System;

namespace NefitSharp.Entities
{
    public struct SystemSettings
    {
        public bool ThermalDesinfectEnabled { get; }
        public DateTime NextThermalDesinfect { get; }
        public EasySensitivity EasyAutoOnSensitivity { get; }
        public double EasyTemperatureStep { get; }
        public double EasyTemperatureAdjustment { get; }

        public SystemSettings(bool thermalDesinfectEnabled, DateTime nextThermalDesinfect, string easyAutoOnSensitivity, double easyTemperatureStep, double easyTemperatureAdjustment)
        {
            ThermalDesinfectEnabled = thermalDesinfectEnabled;
            NextThermalDesinfect = nextThermalDesinfect;
            switch (easyAutoOnSensitivity.ToLower())
            {
                case "high":
                    EasyAutoOnSensitivity = EasySensitivity.High;
                    break;
                case "low":
                    EasyAutoOnSensitivity = EasySensitivity.Low;
                    break;
                default:
                    EasyAutoOnSensitivity = EasySensitivity.Unknown;
                    break;
            }            
            EasyTemperatureStep = easyTemperatureStep;
            EasyTemperatureAdjustment = easyTemperatureAdjustment;
        }
    }
}