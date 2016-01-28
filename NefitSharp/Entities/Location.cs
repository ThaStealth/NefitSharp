using System;

namespace NefitSharp.Entities
{
    public class GasSample
    {
        public DateTime Timestamp { get; }
        public double HotWater { get; }
        public double CentralHeating { get; }
        public double Total { get { return HotWater + CentralHeating; } }

        public GasSample(DateTime timestamp, double hotWater, double centralHeating)
        {
            Timestamp = timestamp;
            HotWater = hotWater;
            CentralHeating = centralHeating;
        }
    }

    public struct Location
    {
        public double Latitude { get; }

        public double Longitude { get; }

        internal Location(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
