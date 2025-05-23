﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AppleFlyover.Spotify;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading;
using System.ComponentModel;
using Windows.Networking.Connectivity;
using Microsoft.UI.Dispatching;

namespace AppleFlyover
{
    public class SpotifyHelper : INotifyPropertyChanged
    {
        private const string BaseSpotifyPlayerUrl = "https://api.spotify.com/v1/me/player/";
        private const string BaseSpotifyUserTracks = "https://api.spotify.com/v1/me/tracks/";
        public event PropertyChangedEventHandler PropertyChanged;
        private HttpClient httpClient;
        private NetworkConnectivityLevel currentNetworkConnectivityLevel;
        private DateTime? TokenExpireTime { get; set; }
        private string AccessToken { get; set; }
        private string RefreshToken { get; set; }
        private bool AutomaticallyRefreshInfo { get; set; }
        private SpotifyCurrentlyPlaying CurrentlyPlaying { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        private DispatcherQueue mainPageDispatcher;

        private bool available;
        public bool Available
        {
            get { return available; }
            private set { available = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Available))); }
        }

        private string trackName;
        public string TrackName
        {
            get { return trackName; }
            private set { trackName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackName))); }
        }

        private string albumArtist;
        public string AlbumArtist
        {
            get { return albumArtist; }
            private set { albumArtist = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlbumArtist))); }
        }

        private BitmapImage albumCover;
        public BitmapImage AlbumCover
        {
            get { return albumCover; }
            private set { albumCover = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AlbumCover))); }
        }

        private bool isPlaying;
        public bool IsPlaying
        {
            get { return isPlaying; }
            private set { isPlaying = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying))); }
        }

        private uint volume;
        public uint Volume
        {
            get { return volume; }
            set { volume = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Volume))); }
        }

        private bool savedTrack;
        public bool SavedTrack
        {
            get { return savedTrack; }
            private set { savedTrack = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SavedTrack))); }
        }

        public SpotifyHelper(DispatcherQueue dispatcherQueue)
        {
            httpClient = new HttpClient();
            Available = true;
            TokenExpireTime = null;
            AutomaticallyRefreshInfo = false;
            cancellationTokenSource = new CancellationTokenSource();
            mainPageDispatcher = dispatcherQueue;

            currentNetworkConnectivityLevel = NetworkInformation.GetInternetConnectionProfile().GetNetworkConnectivityLevel();
            Available = currentNetworkConnectivityLevel == NetworkConnectivityLevel.InternetAccess;
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            currentNetworkConnectivityLevel = NetworkInformation.GetInternetConnectionProfile().GetNetworkConnectivityLevel();

            // Apparently this method can be called not on the UI thread so make sure we set Available on the UI thread
            HelperMethods.CallOnUiThreadAsync(mainPageDispatcher, () =>
            {
                Available = currentNetworkConnectivityLevel == NetworkConnectivityLevel.InternetAccess;
            });
        }

        public string GetAuthorizeUrl()
        {
            return $"https://accounts.spotify.com/authorize?client_id={Secrets.SpotifyClientId}&response_type=code&redirect_uri={Secrets.SpotifyRedirectUrl}&scope=user-read-playback-state user-modify-playback-state user-read-currently-playing user-library-read user-library-modify";
        }

        public async Task ProcessRedirect(Uri url)
        {
            if (url.Host == "golf1052.com" && !url.Query.Contains("error=access_denied"))
            {
                var decoded = new WwwFormUrlDecoder(url.Query);
                string code = decoded.GetFirstValueByName("code");
                FormUrlEncodedContent tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", Secrets.SpotifyRedirectUrl }
                });
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GetEncodedAuth());
                HttpResponseMessage tokenResponseMessage = await httpClient.PostAsync("https://accounts.spotify.com/api/token", tokenRequestContent);
                try
                {
                    await SetupSpotifyToken(tokenResponseMessage);
                }
                catch
                {
                    HelperMethods.CallOnUiThreadAsync(mainPageDispatcher, () =>
                    {
                        Available = false;
                    });
                    throw;
                }
            }
            else
            {
                HelperMethods.CallOnUiThreadAsync(mainPageDispatcher, () =>
                {
                    Available = false;
                });
            }
        }

        private string GetEncodedAuth()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Secrets.SpotifyClientId}:{Secrets.SpotifyClientSecret}"));
        }

        public async Task CheckIfRefreshNeeded()
        {
            while (true)
            {
                if (TokenExpireTime.HasValue && DateTime.Now >= TokenExpireTime.Value)
                {
                    FormUrlEncodedContent refreshRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", RefreshToken }
                    });
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", GetEncodedAuth());
                    HttpResponseMessage refreshResponseMessage = await httpClient.PostAsync("https://accounts.spotify.com/api/token", refreshRequestContent);
                    try
                    {
                        await SetupSpotifyToken(refreshResponseMessage);
                    }
                    catch
                    {
                        HelperMethods.CallOnUiThreadAsync(mainPageDispatcher, () =>
                        {
                            Available = false;
                        });
                        break;
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        public async Task SetupSpotifyToken(HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                JObject responseObject = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
                long expiresIn = (long)responseObject["expires_in"];
                TokenExpireTime = DateTime.Now.AddSeconds(expiresIn);
                AccessToken = (string)responseObject["access_token"];
                string refreshToken = (string)responseObject["refresh_token"];
                if (refreshToken != null)
                {
                    RefreshToken = refreshToken;
                }
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            }
            else
            {
                throw new Exception("Spotify error");
            }
        }

        public async Task StartUpdate()
        {
            AutomaticallyRefreshInfo = true;
            bool doubleCheck = false;
            while (AutomaticallyRefreshInfo)
            {
                bool attempting = true;
                int tries = 3;
                while (attempting)
                {
                    if (tries <= 0)
                    {
                        AutomaticallyRefreshInfo = false;
                        EmptyPlayer();
                        return;
                    }

                    CurrentlyPlaying = await GetCurrentInfo();
                    if (CurrentlyPlaying == null)
                    {
                        tries--;
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(15), cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            if (cancellationTokenSource.IsCancellationRequested)
                            {
                                AutomaticallyRefreshInfo = false;
                                return;
                            }
                        }
                    }
                    else
                    {
                        attempting = false;
                    }
                }

                if (CurrentlyPlaying.Device != null &&
                    CurrentlyPlaying.Device.VolumePercent != null)
                {
                    Volume = (uint)CurrentlyPlaying.Device.VolumePercent;
                }

                IsPlaying = CurrentlyPlaying.IsPlaying;
                TrackName = CurrentlyPlaying.Track.Name;
                string albumArtist = CurrentlyPlaying.Track.Album.Name;
                if (CurrentlyPlaying.Track.Artists.Count > 0)
                {
                    albumArtist += $" by {CurrentlyPlaying.Track.Artists[0].Name}";
                }

                AlbumArtist = albumArtist;
                if (CurrentlyPlaying.Track.Album.Images.Count > 0)
                {
                    AlbumCover = new BitmapImage(new Uri(CurrentlyPlaying.Track.Album.Images[0].Url));
                }
                else
                {
                    AlbumCover = null;
                }

                if (!IsPlaying)
                {
                    // when an album finishes spotify takes a second or two to queue up more songs
                    // here we double check that playback has actually stopped.
                    if (!doubleCheck)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3));
                        doubleCheck = true;
                        continue;
                    }
                    else
                    {
                        AutomaticallyRefreshInfo = false;
                        return;
                    }
                }
                else
                {
                    doubleCheck = false;
                }

                long timeRemainingMs = (long)TimeSpan.FromSeconds(15).TotalMilliseconds;
                if (CurrentlyPlaying.Progress.HasValue)
                {
                    timeRemainingMs = CurrentlyPlaying.Track.Duration - CurrentlyPlaying.Progress.Value;
                }

                SavedTrack = await IsTrackSaved(CurrentlyPlaying.Track.Id);

                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(timeRemainingMs), cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        AutomaticallyRefreshInfo = false;
                        return;
                    }
                }
            }
        }

        private void EmptyPlayer()
        {
            IsPlaying = false;
            TrackName = string.Empty;
            AlbumArtist = string.Empty;
            AlbumCover = null;
        }

        public async Task<SpotifyCurrentlyPlaying> GetCurrentInfo()
        {
            try
            {
                return await MakeAuthorizedSpotifyRequest<SpotifyCurrentlyPlaying>($"{BaseSpotifyPlayerUrl}", HttpMethod.Get);
            }
            catch
            {
                return null;
            }
        }

        public async Task Play()
        {
            try
            {
                await MakeAuthorizedSpotifyRequest<bool>($"{BaseSpotifyPlayerUrl}play", HttpMethod.Put);
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                await Task.Delay(TimeSpan.FromSeconds(1));
                _ = StartUpdate();
            }
            catch
            {
            }
        }

        public async Task Pause()
        {
            try
            {
                await MakeAuthorizedSpotifyRequest<bool>($"{BaseSpotifyPlayerUrl}pause", HttpMethod.Put);
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                await Task.Delay(TimeSpan.FromSeconds(1));
                _ = StartUpdate();
            }
            catch
            {
            }
        }

        public async Task GoFoward()
        {
            try
            {
                await MakeAuthorizedSpotifyRequest<bool>($"{BaseSpotifyPlayerUrl}next", HttpMethod.Post);
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                await Task.Delay(TimeSpan.FromSeconds(1));
                _ = StartUpdate();
            }
            catch
            {
            }
        }

        public async Task GoBack()
        {
            try
            {
                await MakeAuthorizedSpotifyRequest<bool>($"{BaseSpotifyPlayerUrl}previous", HttpMethod.Post);
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                await Task.Delay(TimeSpan.FromSeconds(1));
                _ = StartUpdate();
            }
            catch
            {
            }
        }

        public async Task SetVolume(uint volume)
        {
            await MakeAuthorizedSpotifyRequest<bool>($"{BaseSpotifyPlayerUrl}volume?volume_percent={volume}", HttpMethod.Put);
        }

        public async Task SaveCurrentTrack()
        {
            SavedTrack = true;
            await MakeAuthorizedSpotifyRequest<bool>($"{BaseSpotifyUserTracks}?ids={CurrentlyPlaying.Track.Id}", HttpMethod.Put);
        }

        public async Task UnsaveCurrentTrack()
        {
            SavedTrack = false;
            await MakeAuthorizedSpotifyRequest<bool>($"{BaseSpotifyUserTracks}?ids={CurrentlyPlaying.Track.Id}", HttpMethod.Delete);
        }

        private async Task<bool> IsTrackSaved(string id)
        {
            List<bool> response = await MakeAuthorizedSpotifyRequest<List<bool>>($"{BaseSpotifyUserTracks}contains?ids={id}", HttpMethod.Get);
            if (response.Count > 0)
            {
                return response[0];
            }
            else
            {
                return false;
            }
        }

        private async Task<T> MakeAuthorizedSpotifyRequest<T>(string url, HttpMethod method)
        {
            if (method.Method == "GET")
            {
                HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
                if (responseMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<T>(await responseMessage.Content.ReadAsStringAsync());
                }
                else if (responseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    return default(T);
                }
                else if (responseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default(T);
                }
                else
                {
                    throw new Exception();
                }
            }
            else if (method.Method == "PUT")
            {
                HttpResponseMessage responseMessage = await httpClient.PutAsync(url, null);
                return default(T);
            }
            else if (method.Method == "POST")
            {
                HttpResponseMessage responseMessage = await httpClient.PostAsync(url, null);
                return default(T);
            }
            else if (method.Method == "DELETE")
            {
                HttpResponseMessage responseMessage = await httpClient.DeleteAsync(url);
                return default(T);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
