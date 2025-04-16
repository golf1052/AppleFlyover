using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppleFlyover.AirQuality.AirNow;
using AppleFlyover.AirQuality.AirNow.Objects;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.Devices.Geolocation;
using Windows.UI;

namespace AppleFlyover.AirQuality
{
    public class AirQualityHelper : INotifyPropertyChanged
    {
        private AirNowAPI airNowAPI;
        private Geolocator geolocator;

        public event PropertyChangedEventHandler PropertyChanged;

        private int currentAQI;
        public int CurrentAQI
        {
            get { return currentAQI; }
            private set
            {
                currentAQI = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentAQI)));
                Brush = new SolidColorBrush(GetCategoryColor());
                Text = GetText();
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
            airNowAPI = new AirNowAPI(Secrets.AirNowAPIKey);
            geolocator = new Geolocator()
            {
                DesiredAccuracy = PositionAccuracy.Default,
                ReportInterval = (uint)TimeSpan.FromMinutes(15).TotalMilliseconds
            };
            geolocator.AllowFallbackToConsentlessPositions();
            CurrentAQI = -1;
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
                    CurrentAQI = pm25Observation.AQI;
                }
                else
                {
                    CurrentAQI = -1;
                }
                await Task.Delay(TimeSpan.FromMinutes(30));
            }
        }

        public Color GetCategoryColor()
        {
            if (CurrentAQI < 0)
            {
                return Colors.Black;
            }
            else if (CurrentAQI <= 50)
            {
                // Green
                return Color.FromArgb(255, 96, 169, 23);
            }
            else if (CurrentAQI <= 100)
            {
                // Yellow
                return Color.FromArgb(255, 227, 200, 0);
            }
            else if (CurrentAQI <= 150)
            {
                // Orange
                return Color.FromArgb(255, 250, 104, 0);
            }
            else if (CurrentAQI <= 200)
            {
                // Red
                return Color.FromArgb(255, 229, 20, 0);
            }
            else if (CurrentAQI <= 300)
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
            if (CurrentAQI < 0)
            {
                return "???";
            }
            else if (CurrentAQI == int.MaxValue)
            {
                return "!!!";
            }
            else
            {
                return CurrentAQI.ToString();
            }
        }
    }
}
