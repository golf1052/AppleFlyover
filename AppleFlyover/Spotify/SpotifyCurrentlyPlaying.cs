using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AppleFlyover.Spotify
{
    public class SpotifyCurrentlyPlaying
    {
        /// <summary>
        /// Progress into the currently playing track, can be null
        /// </summary>
        [JsonProperty("progress_ms")]
        public long? Progress { get; set; }

        [JsonProperty("is_playing")]
        public bool IsPlaying { get; set; }

        [JsonProperty("item")]
        public SpotifyTrack Track { get; set; }
    }
}
