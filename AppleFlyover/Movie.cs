using System;
using Newtonsoft.Json.Linq;

namespace AppleFlyover
{
    public class Movie
    {
        public enum FileTypes
        {
            Unknown,
            Video
        }

        public enum TimesOfDay
        {
            Unknown,
            Day,
            Night
        }

        public Uri Url { get; private set; }
        public string Label { get; private set; }
        public FileTypes FileType { get; private set; }
        public string Id { get; private set; }
        public TimesOfDay TimeOfDay { get; private set; }

        public Movie(JObject o)
        {
            Url = new Uri((string)o["url"]);
            Label = (string)o["accessibilityLabel"];
            string fileType = (string)o["type"];
            if (fileType == "video")
            {
                FileType = FileTypes.Video;
            }
            else
            {
                FileType = FileTypes.Unknown;
            }
            Id = (string)o["id"];
            string timeOfDay = (string)o["timeOfDay"];
            if (timeOfDay == "day")
            {
                TimeOfDay = TimesOfDay.Day;
            }
            else if (timeOfDay == "night")
            {
                TimeOfDay = TimesOfDay.Night;
            }
            else
            {
                TimeOfDay = TimesOfDay.Unknown;
            }
        }
    }
}
