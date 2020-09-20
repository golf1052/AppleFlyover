using System;
using System.Collections.Generic;

namespace AppleFlyover.AirQuality
{
    public abstract class Index
    {
        public enum Categories
        {
            Good,
            Moderate,
            UnhealthyForSensitiveGroups,
            Unhealthy,
            VeryUnhealthy,
            Hazardous1,
            Hazardous2
        }

        protected Dictionary<Categories, Category> categories;

        public Index()
        {
            categories = new Dictionary<Categories, Category>();
        }

        public int ToIndexValue(float pm25)
        {
            Category containingCategory = null;
            foreach (var category in categories)
            {
                if (category.Value.ConcentrationHigh.HasValue)
                {
                    if (pm25 >= category.Value.ConcentrationLow && pm25 <= category.Value.ConcentrationHigh)
                    {
                        containingCategory = category.Value;
                        break;
                    }
                }
                else
                {
                    containingCategory = category.Value;
                    break;
                }
            }

            if (containingCategory.ConcentrationHigh.HasValue)
            {
                return (int)Math.Round((containingCategory.IndexHigh.Value - containingCategory.IndexLow) / (containingCategory.ConcentrationHigh.Value - containingCategory.ConcentrationLow) * (pm25 - containingCategory.ConcentrationLow) + containingCategory.IndexLow);
            }
            else
            {
                return -1;
            }
        }

        public float ToConcentrationValue(int indexValue)
        {
            Category containingCategory = null;
            foreach (var category in categories)
            {
                if (category.Value.IndexHigh.HasValue)
                {
                    if (indexValue >= category.Value.IndexLow && indexValue <= category.Value.IndexHigh)
                    {
                        containingCategory = category.Value;
                        break;
                    }
                }
                else
                {
                    containingCategory = category.Value;
                    break;
                }
            }

            if (containingCategory.IndexHigh.HasValue)
            {
                return (float)Math.Round((containingCategory.ConcentrationHigh.Value - containingCategory.ConcentrationLow) / (containingCategory.IndexHigh.Value - containingCategory.IndexLow) * (indexValue - containingCategory.IndexLow) + containingCategory.ConcentrationLow, 1);
            }
            else
            {
                return -1;
            }
        }
    }
}
