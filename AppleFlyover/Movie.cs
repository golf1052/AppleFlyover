using System;
using Newtonsoft.Json.Linq;

namespace AppleFlyover
{
    public class Movie
    {
        public Uri Url { get; private set; }
        public string Label { get; private set; }
        public string CacheFileName { get; private set; }

        public Movie(Uri url, string label, string id)
        {
            Url = url;
            Label = label;
            CacheFileName = $"{label}-{id}.mov";
        }
    }
}
