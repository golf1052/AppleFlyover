using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AppleFlyover.AirQuality.AirNow.Objects;
using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppleFlyover.AirQuality.AirNow
{
    public class AirNowAPI
    {
        private const string BaseUrl = "https://www.airnowapi.org";

        private string apiKey;
        private HttpClient httpClient;

        public AirNowAPI(string apiKey)
        {
            this.apiKey = apiKey;
            httpClient = new HttpClient();
        }

        public async Task<List<Observation>> GetCurrentObservationByLocation(double latitude, double longitude, int? distance = null)
        {
            Url url = new Url(BaseUrl).AppendPathSegments("/aq/observation/latLong/current")
                .SetQueryParam("latitude", latitude)
                .SetQueryParam("longitude", longitude)
                .SetQueryParam("distance", distance)
                .SetQueryParam("format", "application/json")
                .SetQueryParam("api_key", apiKey);

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
            if (!responseMessage.IsSuccessStatusCode)
            {
                return new List<Observation>();
            }
            List<Observation> observations = JsonConvert.DeserializeObject<List<Observation>>(await responseMessage.Content.ReadAsStringAsync());
            return observations;
        }
    }
}
