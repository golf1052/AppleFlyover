﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AppleFlyover.AirQuality;
using golf1052.SeattleCollectionCalendar;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Devices.Geolocation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking.BackgroundTransfer;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.ViewManagement;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AppleFlyover
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string SegoeAssetFont = "Segoe MDL2 Assets";
        private const string TemperatureText = "Temperature";
        private const string ColorText = "Color";
        private const string BrightnessText = "Brightness";

        DateTime sunrise;
        DateTime sunset;
        MediaPlayer mediaPlayer;
        private Queue<TimeSpan> lastPositions;
        public SpotifyHelper SpotifyHelper { get; private set; }
        public HueHelper HueHelper { get; private set; }
        public AirQualityHelper AirQualityHelper { get; private set; }
        public SolidWasteCollectionHelper SolidWasteCollectionHelper { get; private set; }

        private HttpClient httpClient;
        private AppleMovieDownloader appleMovieDownloader;
        private RadialController dial;
        private RadialControllerConfiguration dialConfig;
        private List<RadialControllerMenuItem> menuItems;
        private int selectedItem = 0;
        private bool isWindowFocused;

        private NetworkConnectivityLevel currentNetworkConnectivityLevel;
        private bool ranOnLaunchInternetTasks;

        private double rotationBuffer;

        public enum LightMode
        {
            Brightness,
            Temperature,
            Color
        }

        private LightMode lightMode;

        public enum Device
        {
            Desktop,
            Mobile,
            Other
        }
        private Device device;
        private DispatcherQueue MainPageDispatcher;

        public CalendarHelper CalendarHelper { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();

            ranOnLaunchInternetTasks = false;
            currentNetworkConnectivityLevel = NetworkInformation.GetInternetConnectionProfile().GetNetworkConnectivityLevel();
            App.Window.Activated += Window_Activated;
            MainPageDispatcher = DispatcherQueue.GetForCurrentThread();

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            string deviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;
            if (deviceFamily.Contains("Mobile"))
            {
                device = Device.Mobile;
            }
            else if (deviceFamily.Contains("Desktop"))
            {
                device = Device.Desktop;
            }
            else
            {
                device = Device.Other;
            }

            mediaPlayer = new MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayerElement.SetMediaPlayer(mediaPlayer);
            App.Window.GetAppWindow().SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
            httpClient = new HttpClient();
            appleMovieDownloader = new AppleMovieDownloader(httpClient);
            sunrise = DateTime.MinValue;
            sunset = DateTime.MinValue;
            lastPositions = new Queue<TimeSpan>();
            SpotifyHelper = new SpotifyHelper(MainPageDispatcher);
            HueHelper = new HueHelper();
            AirQualityHelper = new AirQualityHelper();
            SolidWasteCollectionHelper = new SolidWasteCollectionHelper(new CollectionClient(httpClient));
            CalendarHelper = new CalendarHelper();

            rotationBuffer = 0;
            lightMode = LightMode.Brightness;
            
            if (device == Device.Desktop)
            {
                var hwnd = App.WindowHandle;
                dial = RadialControllerInterop.CreateForWindow(hwnd);
                dial.RotationResolutionInDegrees = 5;
                dial.UseAutomaticHapticFeedback = false;
                dialConfig = RadialControllerConfigurationInterop.GetForWindow(hwnd);
                menuItems = new List<RadialControllerMenuItem>();
                isWindowFocused = true;
                dial.ButtonClicked += Dial_ButtonClicked;
                dial.RotationChanged += Dial_RotationChanged;
                dial.ControlAcquired += Dial_ControlAcquired;
                dial.ControlLost += Dial_ControlLost;
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                // Window deactivated
            }
            else
            {
                // Window activated
                UpdateClock();
            }
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            if (connectionProfile == null)
            {
                currentNetworkConnectivityLevel = NetworkConnectivityLevel.None;
            }
            else
            {
                currentNetworkConnectivityLevel = connectionProfile.GetNetworkConnectivityLevel();
            }

            if (currentNetworkConnectivityLevel == NetworkConnectivityLevel.InternetAccess)
            {
                if (!ranOnLaunchInternetTasks)
                {
                    await RunOnLaunchInternetTasks();
                }
            }
        }

        private async void Dial_ControlLost(RadialController sender, object args)
        {
            await FlushRotation();
            isWindowFocused = false;
        }

        private void Dial_ControlAcquired(RadialController sender, RadialControllerControlAcquiredEventArgs args)
        {
            isWindowFocused = true;
        }

        private void Dial_RotationChanged(RadialController sender, RadialControllerRotationChangedEventArgs args)
        {
            rotationBuffer += args.RotationDeltaInDegrees;
        }

        private async Task ProcessRotationBuffer()
        {
            while (true)
            {
                await FlushRotation();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        private async Task FlushRotation()
        {
            if (rotationBuffer != 0)
            {
                short rotation = (short)rotationBuffer;
                if (lightMode == LightMode.Temperature)
                {
                    await HueHelper.IncreaseDecreaseTemperature((short)-rotation);
                }
                else if (lightMode == LightMode.Color)
                {
                    await HueHelper.IncreaseDecreaseColor((short)(rotation * 182));
                }
                else if (lightMode == LightMode.Brightness)
                {
                    //LightBrightness.Value += rotation;
                }
                rotationBuffer = 0;
            }
        }

        private async void Dial_ButtonClicked(RadialController sender, RadialControllerButtonClickedEventArgs args)
        {
            await HueHelper.ToggleLight();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            GeolocationAccessStatus accessStatus = await Geolocator.RequestAccessAsync();
            await RunOnLaunchInternetTasks();
            await HueHelper.Setup();

            if (device == Device.Desktop)
            {
                BuildDialMenu();
            }
            Task refreshLightStatus = HueHelper.RefreshStatus();
            Task updateClockTask = UpdateClockUI();
            Task checkFrozenVideo = CheckFrozenVideo();
            Task processRotationBufferTask = ProcessRotationBuffer();
            _ = AirQualityHelper.Run();
            _ = CalendarHelper.Run();
            _ = SolidWasteCollectionHelper.Run();

            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Runs everything that is needed on app startup if there is internet access
        /// </summary>
        /// <returns></returns>
        private async Task RunOnLaunchInternetTasks()
        {
            if (currentNetworkConnectivityLevel == NetworkConnectivityLevel.InternetAccess && !ranOnLaunchInternetTasks)
            {
                await appleMovieDownloader.LoadMovies();
                await PlayMovies();
                WebView.Visibility = Visibility.Visible;
                string spotifyUrl = SpotifyHelper.GetAuthorizeUrl();
                System.Diagnostics.Debug.WriteLine(spotifyUrl);
                WebView.Source = new Uri(spotifyUrl);
                ranOnLaunchInternetTasks = true;
            }
        }

        private void BuildDialMenu()
        {
            menuItems.Clear();
            dial.Menu.Items.Clear();
            RadialControllerMenuItem briItem = RadialControllerMenuItem.CreateFromFontGlyph(BrightnessText, "", SegoeAssetFont);
            briItem.Invoked += (s, e) =>
            {
                lightMode = LightMode.Brightness;
            };
            RadialControllerMenuItem tempItem = RadialControllerMenuItem.CreateFromFontGlyph(TemperatureText, "", SegoeAssetFont);
            tempItem.Invoked += (s, e) =>
            {
                lightMode = LightMode.Temperature;
            };
            RadialControllerMenuItem colorItem = RadialControllerMenuItem.CreateFromKnownIcon(ColorText, RadialControllerMenuKnownIcon.InkColor);
            colorItem.Invoked += (s, e) =>
            {
                lightMode = LightMode.Color;
            };
            menuItems.Add(briItem);
            menuItems.Add(tempItem);
            menuItems.Add(colorItem);

            for (int i = 0; i < HueHelper.Lights.Count; i++)
            {
                var internalI = i;
                string light = HueHelper.Lights[i];
                RadialControllerMenuItem lightItem = RadialControllerMenuItem.CreateFromFontGlyph(light, "", SegoeAssetFont);
                lightItem.Invoked += (s, e) =>
                {
                    if (LightComboBox.Items.Count >= internalI + 1)
                    {
                        LightComboBox.SelectedIndex = internalI;
                    }
                };
                menuItems.Add(lightItem);
            }
            foreach (var item in menuItems)
            {
                dial.Menu.Items.Add(item);
            }
            dialConfig.SetDefaultMenuItems(new List<RadialControllerSystemMenuItemKind>());
        }

        private async Task UpdateClockUI()
        {
            while (true)
            {
                UpdateClock();
                UpdateSolidWasteAlert();
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            timeBlock.Text = now.ToString("t").ToLower();
            dateBlock.Text = now.ToString("dddd, MMMM d");
        }

        private void UpdateSolidWasteAlert()
        {
            if (SolidWasteCollectionHelper.NextTrigger == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            if (now >= SolidWasteCollectionHelper.NextTrigger)
            {
                SolidWasteCollectionGrid.Visibility = Visibility.Visible;
                foreach (var type in SolidWasteCollectionHelper.ToDisplay)
                {
                    if (type == SolidWasteType.Garbage)
                    {
                        GarbageRow.Visibility = Visibility.Visible;
                    }
                    else if (type == SolidWasteType.Recycle)
                    {
                        RecyclingRow.Visibility = Visibility.Visible;
                    }
                    else if (type == SolidWasteType.FoodYardWaste)
                    {
                        CompostRow.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        /// <summary>
        /// Sometimes for some reason the video player gets stuck. This checks that the position of the stream keeps
        /// advancing. If it stops advancing play a new stream.
        /// </summary>
        /// <returns></returns>
        private async Task CheckFrozenVideo()
        {
            while (true)
            {
                MediaPlaybackSession playbackSession = mediaPlayer.PlaybackSession;
                TimeSpan currentPosition = playbackSession.Position;
                if (lastPositions.Count >= 5)
                {
                    var items = lastPositions.ToList();
                    bool allMatch = true;
                    foreach (var item in items)
                    {
                        if (item != currentPosition)
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    if (allMatch)
                    {
                        lastPositions.Clear();
                        await PlayMovies();
                    }
                    else
                    {
                        lastPositions.Dequeue();
                        lastPositions.Enqueue(currentPosition);
                    }
                }
                else
                {
                    lastPositions.Enqueue(currentPosition);
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        private async Task PlayMovies()
        {
            mediaPlayer.Pause();
            mediaPlayer.Source = null;
            if (currentNetworkConnectivityLevel == NetworkConnectivityLevel.InternetAccess)
            {
                Movie selectedMovie = GetRandomMovie();
                // need to update specifically on the UI thread because this gets called from async methods
                HelperMethods.CallOnUiThreadAsync(MainPageDispatcher, () =>
                {
                    labelBlock.Text = selectedMovie.Label;
                });
                mediaPlayer.Source = MediaSource.CreateFromUri(selectedMovie.Url);
                mediaPlayer.Play();
            }
        }

        private async Task<IMediaPlaybackSource> GetMovieSource(Movie movie)
        {
            // First try to get the movie from local storage
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile movieFile = await cacheFolder.TryGetItemAsync(movie.CacheFileName).AsTask() as StorageFile;
            if (movieFile == null)
            {
                return await GetDownload(movie);
            }
            else
            {
                // If the movie is greater than 30 days old then get a new version
                if (movieFile.DateCreated < DateTimeOffset.Now - TimeSpan.FromDays(30))
                {
                    await movieFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    return await GetDownload(movie);
                }
                else
                {
                    return MediaSource.CreateFromStorageFile(movieFile);
                }
            }
        }

        private async Task<IMediaPlaybackSource> GetDownload(Movie movie)
        {
            StorageFolder cacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile destinationFile = await cacheFolder.CreateFileAsync(movie.CacheFileName, CreationCollisionOption.ReplaceExisting).AsTask();

            BackgroundDownloader downloader = new BackgroundDownloader();
            DownloadOperation download = downloader.CreateDownload(movie.Url, destinationFile);
            // According to this source https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/media-playback-with-mediasource#create-a-mediasource-from-a-downloadoperation
            // we need to set random access and start the download before we can set it as a media source
            download.IsRandomAccessRequired = true;
            _ = download.StartAsync().AsTask();
            return MediaSource.CreateFromDownloadOperation(download);
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

        private Movie GetRandomMovie()
        {
            Random random = new Random();
            return appleMovieDownloader.Movies[random.Next(appleMovieDownloader.Movies.Count)];
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            await PlayMovies();
        }

        private async void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            await PlayMovies();
        }

        public Symbol GetCorrectSymbol(bool isPlaying)
        {
            isPlaying = SpotifyHelper.IsPlaying;
            if (isPlaying)
            {
                return Symbol.Pause;
            }
            else
            {
                return Symbol.Play;
            }
        }

        private async void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            await SpotifyHelper.GoBack();
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (SpotifyHelper.IsPlaying)
            {
                await SpotifyHelper.Pause();
            }
            else
            {
                await SpotifyHelper.Play();
            }
        }

        private async void GoForwardButton_Click(object sender, RoutedEventArgs e)
        {
            await SpotifyHelper.GoFoward();
        }

        private async void LightBrightness_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (HueHelper != null)
            {
                await HueHelper.ChangeBrightness((byte)e.NewValue);
            }
        }

        private async void LightSwitch_Click(object sender, RoutedEventArgs e)
        {
            await HueHelper.ToggleLight();
        }

        private async void LightComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                await HueHelper.SelectLight((string)e.AddedItems[0]);
            }
        }

        public SolidColorBrush GetLightStatus(bool lightOn)
        {
            if (lightOn)
            {
                return new SolidColorBrush(Colors.White);
            }
            else
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        private async void AddRemoveSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (SpotifyHelper.SavedTrack)
            {
                await SpotifyHelper.UnsaveCurrentTrack();
            }
            else
            {
                await SpotifyHelper.SaveCurrentTrack();
            }
        }

        public SolidColorBrush GetAddRemoveSongIconColor(bool savedTrack)
        {
            if (savedTrack)
            {
                return new SolidColorBrush(Color.FromArgb(255, 30, 215, 96));
            }
            else
            {
                return new SolidColorBrush(Colors.White);
            }
        }

        public Symbol GetAddRemoveSongIconSymbol(bool savedTrack)
        {
            if (savedTrack)
            {
                return Symbol.Accept;
            }
            else
            {
                return Symbol.Add;
            }
        }

        private async void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (SpotifyHelper != null)
            {
                await SpotifyHelper.SetVolume((uint)e.NewValue);
            }
        }

        private async void WebView_NavigationCompleted_1(Microsoft.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            try
            {
                if (WebView.Source.Host.Contains("golf1052.com"))
                {
                    WebView.Visibility = Visibility.Collapsed;
                    await SpotifyHelper.ProcessRedirect(WebView.Source);
                    Task spotifyTokenRefresh = SpotifyHelper.CheckIfRefreshNeeded();
                    _ = SpotifyHelper.StartUpdate();
                }
            }
            catch
            {
                WebView.Visibility = Visibility.Collapsed;
            }
        }

        private void AckButton_Click(object sender, RoutedEventArgs e)
        {
            SolidWasteCollectionHelper.Ack();
            SolidWasteCollectionGrid.Visibility = Visibility.Collapsed;
            GarbageRow.Visibility = Visibility.Collapsed;
            RecyclingRow.Visibility = Visibility.Collapsed;
            CompostRow.Visibility = Visibility.Collapsed;
        }
    }
}
