using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AppleFlyover.Spotify
{
    public class SpotifyDevice
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

        [JsonProperty("is_restricted")]
        public bool IsRestricted { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("volume_percent")]
        public int? VolumePercent { get; set; }
    }
}
