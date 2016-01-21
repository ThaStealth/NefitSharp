namespace NefitSharp.Entities
{
    public struct Location
    {
        private double _latitude;
        private double _longitude;

        public double Latitude
        {
            get { return _latitude; }
        }

        public double Longitude
        {
            get { return _longitude; }
        }

        internal Location(double latitude, double longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }
    }
}
