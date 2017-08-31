﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Core;
using Windows.Media.Playback;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AppleFlyover
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string AppleUrl = "http://a1.phobos.apple.com/us/r1000/000/Features/atv/AutumnResources/videos/entries.json";
        private static DateTime lastDownloaded;
        private static bool haveNotPulled;
        private static DateTime lastTimeCheck;
        private static Movie.TimesOfDay cachedTimeOfDay;
        List<Movie> movies;
        DateTime sunrise;
        DateTime sunset;
        MediaPlayer mediaPlayer;

        public MainPage()
        {
            this.InitializeComponent();
            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayerElement.SetMediaPlayer(mediaPlayer);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            movies = new List<Movie>();
            lastDownloaded = DateTime.MinValue;
            haveNotPulled = true;
            lastTimeCheck = DateTime.MinValue;
            cachedTimeOfDay = Movie.TimesOfDay.Unknown;
            sunrise = DateTime.MinValue;
            sunset = DateTime.MinValue;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await DownloadJson(AppleUrl);
            await PlayMovies();
            UpdateClock();

            base.OnNavigatedTo(e);
        }

        private async Task UpdateClock()
        {
            while (true)
            {
                DateTime now = DateTime.Now;
                timeBlock.Text = now.ToString("h:mm tt").ToLower();
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        private async Task DownloadJson(string url)
        {
            movies.Clear();
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            JArray a = JArray.Parse(await response.Content.ReadAsStringAsync());
            foreach (JObject o in a)
            {
                JArray assets = (JArray)o["assets"];
                foreach (JObject movieO in assets)
                {
                    movies.Add(new Movie(movieO));
                }
            }
            lastDownloaded = DateTime.UtcNow;
        }

        private async Task PlayMovies()
        {
            if (DateTime.UtcNow > lastDownloaded + TimeSpan.FromDays(1))
            {
                await DownloadJson(AppleUrl);
            }
            if (lastTimeCheck < Midnight())
            {
                haveNotPulled = true;
            }
            lastTimeCheck = DateTime.Now;
            Movie.TimesOfDay timeOfDay = await GetTimeOfDay();
            Movie selectedMovie = GetRandomMovie(timeOfDay);
            mediaPlayer.Source = MediaSource.CreateFromUri(selectedMovie.Url);
            mediaPlayer.Play();
        }

        public async Task<Movie.TimesOfDay> GetTimeOfDay()
        {
            if (haveNotPulled)
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(string.Format("http://api.wunderground.com/api/{0}/astronomy/q/autoip.json", Secrets.ApiKey));
                haveNotPulled = false;
                JObject o = null;
                try
                {
                    o = JObject.Parse(await response.Content.ReadAsStringAsync());
                    sunrise = DateTimeHelper(int.Parse((string)o["moon_phase"]["sunrise"]["hour"]), int.Parse((string)o["moon_phase"]["sunrise"]["minute"]));
                    sunset = DateTimeHelper(int.Parse((string)o["moon_phase"]["sunset"]["hour"]), int.Parse((string)o["moon_phase"]["sunset"]["minute"]));
                    DateTime now = DateTime.Now;
                    if (sunrise < now && now < sunset)
                    {
                        cachedTimeOfDay = Movie.TimesOfDay.Day;
                    }
                    else
                    {
                        cachedTimeOfDay = Movie.TimesOfDay.Night;
                    }
                    return cachedTimeOfDay;
                }
                catch
                {
                    cachedTimeOfDay = Movie.TimesOfDay.Day;
                    return cachedTimeOfDay;
                }
            }
            else
            {
                DateTime now = DateTime.Now;
                if (sunrise < now && now < sunset)
                {
                    cachedTimeOfDay = Movie.TimesOfDay.Day;
                }
                else
                {
                    cachedTimeOfDay = Movie.TimesOfDay.Night;
                }
                return cachedTimeOfDay;
            }
        }

        private DateTime DateTimeHelper(int hour, int minute)
        {
            DateTime now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        }

        private DateTime Midnight()
        {
            DateTime now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day);
        }

        private Movie GetRandomMovie(Movie.TimesOfDay timeOfDay)
        {
            List<Movie> validList = new List<Movie>();
            foreach (Movie movie in movies)
            {
                if (movie.TimeOfDay == timeOfDay)
                {
                    validList.Add(movie);
                }
            }

            Random random = new Random();
            return validList[random.Next(validList.Count)];
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await PlayMovies();
        }

        private async void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            await PlayMovies();
        }
    }
}
