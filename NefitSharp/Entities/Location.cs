namespace NefitSharp.Entities
{
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
