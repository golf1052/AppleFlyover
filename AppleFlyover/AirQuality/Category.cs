namespace AppleFlyover.AirQuality
{
    public class Category
    {
        public string Label { get; private set; }
        public int IndexLow { get; private set; }
        public int? IndexHigh { get; private set; }
        public float ConcentrationLow { get; private set; }
        public float? ConcentrationHigh { get; private set; }

        public Category(string label,
            int indexLow,
            int indexHigh,
            float concentrationLow,
            float concentrationHigh)
        {
            Label = label;
            IndexLow = indexLow;
            IndexHigh = indexHigh;
            ConcentrationLow = concentrationLow;
            ConcentrationHigh = concentrationHigh;
        }

        public Category(string label,
            int indexLow,
            float concentrationLow)
        {
            Label = label;
            IndexLow = indexLow;
            ConcentrationLow = concentrationLow;
        }
    }
}
