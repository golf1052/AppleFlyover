namespace AppleFlyover.AirQuality
{
    public class AQI : Index
    {
        public AQI()
        {
            categories.Add(Categories.Good, new Category("Good", 0, 50, 0, 12));
            categories.Add(Categories.Moderate, new Category("Moderate", 51, 100, 12.1f, 35.4f));
            categories.Add(Categories.UnhealthyForSensitiveGroups, new Category("Unhealthy for Sensitive Groups", 101, 150, 35.5f, 55.4f));
            categories.Add(Categories.Unhealthy, new Category("Unhealthy", 151, 200, 55.5f, 150.4f));
            categories.Add(Categories.VeryUnhealthy, new Category("Very Unhealthy", 201, 300, 150.5f, 250.4f));
            categories.Add(Categories.Hazardous1, new Category("Hazardous", 301, 400, 250.5f, 350.4f));
            categories.Add(Categories.Hazardous2, new Category("Hazardous", 401, 350.5f));
        }
    }
}
