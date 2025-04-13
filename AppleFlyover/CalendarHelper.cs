using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppleFlyover.Models;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Kiota.Abstractions.Authentication;
using WinRT.Interop;

namespace AppleFlyover
{
    public class CalendarHelper
    {
        public ObservableCollection<CalendarItem> Events;
        private IPublicClientApplication publicClientApplication;
        private InteractiveBrowserCredential interactiveBrowserCredential;
        private GraphServiceClient graphServiceClient;

        public CalendarHelper()
        {
            Events = new ObservableCollection<CalendarItem>();
            // Setup cache instructions https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization?tabs=desktop
            publicClientApplication = PublicClientApplicationBuilder.Create(Secrets.GraphClientId)
                .WithParentActivityOrWindow(() =>
                {
                    return App.WindowHandle;
                })
                .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
                .Build();
            graphServiceClient = new GraphServiceClient(
                new BaseBearerTokenAuthenticationProvider(
                    new TokenProvider(publicClientApplication)));
        }

        public async Task Run()
        {
            var storageProperties = new StorageCreationPropertiesBuilder("msal_cache.txt", MsalCacheHelper.UserRootDirectory)
                .Build();
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(publicClientApplication.UserTokenCache);
            await Update();
        }

        public async Task Update()
        {
            while (true)
            {
                Events.Clear();
                DateTime now = DateTime.Now;
                var calendars = await graphServiceClient.Me.Calendars.GetAsync();
                foreach (var calendar in calendars.Value)
                {
                    var userEvents = await graphServiceClient.Me.Calendars[calendar.Id].Events.GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.Headers.Add("Prefer", $"outlook.timezone=\"{TimeZoneInfo.Local.Id}\"");
                        requestConfiguration.QueryParameters.Select = new string[] { "subject", "start", "end", "location" };
                        requestConfiguration.QueryParameters.Filter = $"start/dateTime ge '{now:yyyy-MM-dd}'";
                        requestConfiguration.QueryParameters.Orderby = new string[] { "start/dateTime" };
                    });

                    var todayEvents = userEvents.Value.Where(u =>
                    {
                        DateTime startTime = DateTime.Parse(u.Start.DateTime);
                        return startTime.Year == now.Year && startTime.Month == now.Month && startTime.Day == now.Day;
                    })
                    .Select(e =>
                    {
                        CalendarItem item = new CalendarItem()
                        {
                            Subject = e.Subject,
                            Start = DateTime.Parse(e.Start.DateTime).ToString("t"),
                            End = DateTime.Parse(e.End.DateTime).ToString("t"),
                            Location = e.Location.DisplayName
                        };
                        return item;
                    })
                    .ToList();

                    foreach (var item in todayEvents)
                    {
                        Events.Add(item);
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        }

        class TokenProvider : IAccessTokenProvider
        {
            private IPublicClientApplication publicClientApplication;

            public TokenProvider(IPublicClientApplication publicClientApplication)
            {
                this.publicClientApplication = publicClientApplication;
            }

            public AllowedHostsValidator AllowedHostsValidator { get; set; }

            public async Task<string> GetAuthorizationTokenAsync(Uri uri,
                Dictionary<string, object> additionalAuthenticationContext = default,
                CancellationToken cancellationToken = default)
            {
                var accounts = await publicClientApplication.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();
                AuthenticationResult authenticationResult;
                if (firstAccount != null)
                {
                    try
                    {
                        authenticationResult = await publicClientApplication.AcquireTokenSilent(new List<string>() { "user.read", "calendars.read" }, firstAccount).ExecuteAsync();
                    }
                    catch (MsalUiRequiredException)
                    {
                        // this exception means re-authentication is required
                        authenticationResult = await publicClientApplication.AcquireTokenInteractive(new List<string>() { "user.read", "calendars.read" }).ExecuteAsync();
                    }
                }
                else
                {
                    authenticationResult = await publicClientApplication.AcquireTokenInteractive(new List<string>() { "user.read", "calendars.read" })
                        .ExecuteAsync();
                }

                return authenticationResult.AccessToken;
            }
        }
    }
}
