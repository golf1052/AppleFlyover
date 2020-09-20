using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppleFlyover.AirQuality.AirNow;
using AppleFlyover.AirQuality.AirNow.Objects;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace AppleFlyover.AirQuality
{
    public class AirQualityHelper : INotifyPropertyChanged
    {
        private AQI aqi;
        private WAQA waqa;
        private AirNowAPI airNowAPI;
        private Geolocator geolocator;

        public event PropertyChangedEventHandler PropertyChanged;

        private int currentWAQA;
        public int CurrentWAQA
        {
            get { return currentWAQA; }
            private set
            {
                currentWAQA = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentWAQA)));
                Brush = new SolidColorBrush(GetCategoryColor());
                Text = GetText();
                System.Diagnostics.Debug.WriteLine(CurrentWAQA);
                System.Diagnostics.Debug.WriteLine(GetCategoryColor());
                System.Diagnostics.Debug.WriteLine(Text);
            }
        }

        private Brush brush;
        public Brush Brush
        {
            get { return brush; }
            private set { brush = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Brush))); }
        }

        private string text;
        public string Text
        {
            get { return text; }
            private set { text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); }
        }

        public AirQualityHelper()
        {
            aqi = new AQI();
            waqa = new WAQA();
            airNowAPI = new AirNowAPI(Secrets.AirNowAPIKey);
            geolocator = new Geolocator()
            {
                DesiredAccuracy = PositionAccuracy.Default,
                ReportInterval = (uint)TimeSpan.FromMinutes(15).TotalMilliseconds
            };
            geolocator.AllowFallbackToConsentlessPositions();
            CurrentWAQA = -1;
        }

        public async Task Run()
        {
            while (true)
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync();
                BasicGeoposition position = geoposition.Coordinate.Point.Position;
                List<Observation> observations = await airNowAPI.GetCurrentObservationByLocation(position.Latitude, position.Longitude);
                Observation pm25Observation = observations.FirstOrDefault(o => o.ParameterName == "PM2.5");
                if (pm25Observation != null)
                {
                    int aqiValue = pm25Observation.AQI;
                    float calculatedPM25 = aqi.ToConcentrationValue(aqiValue);
                    if (calculatedPM25 == -1)
                    {
                        CurrentWAQA = int.MaxValue;
                    }
                    else
                    {
                        int calculatedWAQAValue = waqa.ToIndexValue(calculatedPM25);
                        if (calculatedWAQAValue == -1)
                        {
                            CurrentWAQA = int.MaxValue;
                        }
                        else
                        {
                            CurrentWAQA = calculatedWAQAValue;
                        }
                    }
                    
                }
                else
                {
                    CurrentWAQA = -1;
                }
                await Task.Delay(TimeSpan.FromMinutes(30));
            }
        }

        public Color GetCategoryColor()
        {
            if (CurrentWAQA < 0)
            {
                return Colors.Black;
            }
            else if (CurrentWAQA <= 50)
            {
                // Green
                return Color.FromArgb(255, 96, 169, 23);
            }
            else if (CurrentWAQA <= 100)
            {
                // Yellow
                return Color.FromArgb(255, 227, 200, 0);
            }
            else if (CurrentWAQA <= 150)
            {
                // Orange
                return Color.FromArgb(255, 250, 104, 0);
            }
            else if (CurrentWAQA <= 200)
            {
                // Red
                return Color.FromArgb(255, 229, 20, 0);
            }
            else if (CurrentWAQA <= 300)
            {
                // Purple
                return Color.FromArgb(255, 170, 0, 255);
            }
            else
            {
                // Maroon
                return Color.FromArgb(255, 162, 0, 37);
            }
        }

        public string GetText()
        {
            if (CurrentWAQA < 0)
            {
                return "???";
            }
            else if (CurrentWAQA == int.MaxValue)
            {
                return "!!!";
            }
            else
            {
                return CurrentWAQA.ToString();
            }
        }
    }
}
