using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AppleFlyover.Models;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace AppleFlyover
{
    public class CalendarHelper
    {
        public ObservableCollection<CalendarItem> Events;
        private IPublicClientApplication publicClientApplication;
        private GraphServiceClient graphServiceClient;

        public CalendarHelper()
        {
            Events = new ObservableCollection<CalendarItem>();
            publicClientApplication = PublicClientApplicationBuilder.Create(Secrets.GraphClientId)
                .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient")
                .WithAuthority(AzureCloudInstance.AzurePublic, "consumers")
                .Build();
            HttpClient graphHttpClient = GraphClientFactory.Create(new InteractiveAuthenticationProvider(publicClientApplication));
            graphServiceClient = new GraphServiceClient(graphHttpClient);
        }

        public async Task Run()
        {
            var accounts = await publicClientApplication.GetAccountsAsync();
            var firstAccount = accounts.FirstOrDefault();
            if (firstAccount != null)
            {
                var result = await publicClientApplication.AcquireTokenSilent(new List<string>() { "user.read", "calendars.read" }, firstAccount).ExecuteAsync();
            }
            else
            {
                var result = await publicClientApplication.AcquireTokenInteractive(new List<string>() { "user.read", "calendars.read" }).ExecuteAsync();
            }
            await Update();
        }

        public async Task Update()
        {
            while (true)
            {
                Events.Clear();
                DateTime now = DateTime.Now;
                var userEvents = await graphServiceClient.Me.Events.Request()
                    .Header("Prefer", $"outlook.timezone=\"{TimeZoneInfo.Local.Id}\"")
                    .Select(u => new
                    {
                        u.Subject,
                        u.Start,
                        u.End,
                        u.Location
                    })
                    .Filter($"start/dateTime ge '{now:yyyy-MM-dd}'")
                    .OrderBy("start/dateTime")
                    .GetAsync();

                var todayEvents = userEvents.Where(u =>
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
                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        }
    }
}
