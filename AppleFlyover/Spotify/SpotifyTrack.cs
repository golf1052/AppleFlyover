using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AppleFlyover.Spotify
{
    public class SpotifyTrack
    {
        [JsonProperty("album")]
        public SpotifyAlbum Album { get; set; }

        [JsonProperty("artists")]
        public List<SpotifyArtist> Artists { get; set; }

        /// <summary>
        /// Duration of the track in milliseconds
        /// </summary>
        [JsonProperty("duration_ms")]
        public long Duration { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
