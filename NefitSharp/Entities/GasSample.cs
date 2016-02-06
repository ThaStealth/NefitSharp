using System;

namespace NefitSharp.Entities
{
    public class GasSample
    {
        public DateTime Timestamp { get; }
        public double HotWater { get; }
        public double CentralHeating { get; }
        private double AverageTemperature { get; }
        public double Total => HotWater + CentralHeating;

        public GasSample(DateTime timestamp, double hotWater, double centralHeating,double temperature)
        {
            Timestamp = timestamp;
            HotWater = hotWater;
            CentralHeating = centralHeating;
            AverageTemperature = temperature;
        }
    }
}