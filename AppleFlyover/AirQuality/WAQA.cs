namespace AppleFlyover.AirQuality
{
    public class WAQA : Index
    {
        public WAQA()
        {
            categories.Add(Categories.Good, new Category("Good", 0, 50, 0, 12));
            categories.Add(Categories.Moderate, new Category("Moderate", 51, 100, 12.1f, 20.4f));
            categories.Add(Categories.UnhealthyForSensitiveGroups, new Category("Unhealthy for Sensitive Groups", 101, 150, 20.5f, 35.4f));
            categories.Add(Categories.Unhealthy, new Category("Unhealthy", 151, 200, 35.5f, 80.4f));
            categories.Add(Categories.VeryUnhealthy, new Category("Very Unhealthy", 201, 300, 80.5f, 150.4f));
            categories.Add(Categories.Hazardous1, new Category("Hazardous", 301, 150.5f));
        }
    }
}
