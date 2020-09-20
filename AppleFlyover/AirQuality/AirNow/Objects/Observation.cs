namespace AppleFlyover.AirQuality.AirNow.Objects
{
    public class Observation
    {
        public string DateObserved { get; set; }
        public int HourObserved { get; set; }
        public string LocalTimeZone { get; set; }
        public string ReportingArea { get; set; }
        public string StateCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ParameterName { get; set; }
        public int AQI { get; set; }
        public Category Category { get; set; }
    }
}
