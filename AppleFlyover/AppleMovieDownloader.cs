using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AppleFlyover
{
    public class AppleMovieDownloader
    {
        // urls from https://vscode.dev/github.com/JohnCoates/Aerial/blob/97068951d2c67a56d42f705d405bd0578e45a672/Aerial/Source/Models/Sources/SourceList.swift#L17
        private const string Apple10Url = "http://a1.phobos.apple.com/us/r1000/000/Features/atv/AutumnResources/videos/entries.json";
        // Can't use 11 because it's only HEVC movies
        private const string Apple11Url = "http://sylvan.apple.com/Aerials/2x/entries.json";
        private const string Apple12Url = "http://sylvan.apple.com/Aerials/resources.tar";
        private const string Apple13Url = "http://sylvan.apple.com/Aerials/resources-13.tar";
        private const string Apple16Url = "http://sylvan.apple.com/Aerials/resources-16.tar";

        private readonly HttpClient httpClient;
        public List<Movie> Movies { get; private set; }

        public AppleMovieDownloader(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            Movies = new List<Movie>();
        }

        public async Task LoadMovies()
        {
            // Get tvOS10 movies

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(Apple10Url);
                JArray a = JArray.Parse(await response.Content.ReadAsStringAsync());
                foreach (JObject o in a)
                {
                    JArray assets = (JArray)o["assets"];
                    foreach (JObject movieO in assets)
                    {
                        Movie movie = new Movie(new Uri((string)movieO["url"]), (string)movieO["accessibilityLabel"], (string)movieO["id"]);
                        Movies.Add(movie);
                    }
                }

                // Get tvOS12 movies
                response = await httpClient.GetAsync(Apple12Url);
                JObject tv12MoviesO = ExtractTar(await response.Content.ReadAsStreamAsync());
                foreach (JObject o in (JArray)tv12MoviesO["assets"])
                {
                    string url = (string)o["url-1080-H264"];
                    url = url.Replace("\\", "");
                    // Domain has misconfigured cert so downgrade to http
                    url = url.Replace("https", "http");
                    Movie movie = new Movie(new Uri(url), (string)o["accessibilityLabel"], (string)o["id"]);
                    Movies.Add(movie);
                }

                // Get tvOS13 movies
                response = await httpClient.GetAsync(Apple13Url);
                JObject tv13MoviesO = ExtractTar(await response.Content.ReadAsStreamAsync());
                foreach (JObject o in (JArray)tv13MoviesO["assets"])
                {
                    string url = (string)o["url-1080-H264"];
                    url = url.Replace("\\", "");
                    // Domain has misconfigured cert so downgrade to http
                    url = url.Replace("https", "http");
                    Movie movie = new Movie(new Uri(url), (string)o["accessibilityLabel"], (string)o["id"]);
                    Movies.Add(movie);
                }

                // Get tvOS16 movies
                response = await httpClient.GetAsync(Apple16Url);
                JObject tv16MoviesO = ExtractTar(await response.Content.ReadAsStreamAsync());
                foreach (JObject o in (JArray)tv16MoviesO["assets"])
                {
                    string url = (string)o["url-1080-H264"];
                    url = url.Replace("\\", "");
                    // Domain has misconfigured cert so downgrade to http
                    url = url.Replace("https", "http");
                    Movie movie = new Movie(new Uri(url), (string)o["accessibilityLabel"], (string)o["id"]);
                    Movies.Add(movie);
                }
            }
            catch (HttpRequestException)
            {
            }
        }

        // from https://gist.github.com/ForeverZer0/a2cd292bd2f3b5e114956c00bb6e872b
        private JObject ExtractTar(Stream stream)
        {
            byte[] buffer = new byte[100];
            while (true)
            {
                stream.Read(buffer, 0, 100);
                string name = Encoding.ASCII.GetString(buffer).Trim('\0');
                if (string.IsNullOrWhiteSpace(name))
                {
                    break;
                }
                stream.Seek(24, SeekOrigin.Current);
                stream.Read(buffer, 0, 12);
                long size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);
                stream.Seek(376, SeekOrigin.Current);

                if (!name.Equals("./"))
                {
                    byte[] fileBuffer = new byte[size];
                    stream.Read(fileBuffer, 0, fileBuffer.Length);
                    if (name.Contains(".json"))
                    {
                        Stream fileStream = new MemoryStream(fileBuffer);
                        return JObject.Load(new JsonTextReader(new StreamReader(fileStream)));
                    }
                }

                long position = stream.Position;
                long offset = 512 - (position % 512);
                if (offset == 512)
                {
                    offset = 0;
                }
                stream.Seek(offset, SeekOrigin.Current);
            }

            return null;
        }
    }
}
