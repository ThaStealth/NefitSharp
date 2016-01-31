using System;

namespace NefitSharp.Entities
{
    public struct SystemSettings
    {
        public bool ThermalDesinfectEnabled { get; }
        public DateTime NextThermalDesinfect { get; }
        public string EasyAutoOnSensitivity { get; }
        public double EasyTemperatureStep { get; }
        public double EasyTemperatureAdjustment { get; }

        public SystemSettings(bool thermalDesinfectEnabled, DateTime nextThermalDesinfect, string easyAutoOnSensitivity, double easyTemperatureStep, double easyTemperatureAdjustment)
        {
            ThermalDesinfectEnabled = thermalDesinfectEnabled;
            NextThermalDesinfect = nextThermalDesinfect;
            EasyAutoOnSensitivity = easyAutoOnSensitivity;
            EasyTemperatureStep = easyTemperatureStep;
            EasyTemperatureAdjustment = easyTemperatureAdjustment;
        }
    }
}