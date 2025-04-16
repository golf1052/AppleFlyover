using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using golf1052.SeattleCollectionCalendar;
using golf1052.SeattleCollectionCalendar.Models.Response;

namespace AppleFlyover
{
    public class SolidWasteCollectionHelper
    {
        public DateTime? NextTrigger { get; private set; }
        public List<SolidWasteType> ToDisplay { get; private set; }

        private readonly CollectionClient collectionClient;

        public SolidWasteCollectionHelper(CollectionClient collectionClient)
        {
            this.collectionClient = collectionClient;
            NextTrigger = null;
            ToDisplay = new List<SolidWasteType>();
        }

        public async Task Run()
        {
            while (true)
            {
                await GetCalendar();
                await Task.Delay(TimeSpan.FromHours(8));
            }
        }

        public void Ack()
        {
            NextTrigger = null;
            ToDisplay.Clear();
        }

        private async Task GetCalendar()
        {
            var addressSearchResponse = await collectionClient.FindAddress(Secrets.CollectionAddress);
            var address = addressSearchResponse.Address.FirstOrDefault(a => a.AddressLine1.StartsWith(Secrets.CollectionAddress, StringComparison.CurrentCultureIgnoreCase));
            if (address == null)
            {
                return;
            }

            var accountSearchResponse = await collectionClient.FindAccount(address);
            var account = accountSearchResponse.Account;
            if (account.AccountNumber == null)
            {
                return;
            }

            var auth = await collectionClient.GetGuestAuth();
            var swSummary = await collectionClient.SolidWasteSummary(account, auth);

            DateTime now = DateTime.Now;
            ServiceItem garbageService = swSummary.GetServiceItem(SolidWasteType.Garbage).FirstOrDefault();
            if (garbageService == null)
            {
                return;
            }
            ServiceItem recycleService = swSummary.GetServiceItem(SolidWasteType.Recycle).FirstOrDefault();
            ServiceItem foodYardWasteService = swSummary.GetServiceItem(SolidWasteType.FoodYardWaste).FirstOrDefault();

            if (garbageService.Schedule == null)
            {
                return;
            }

            if (now.DayOfWeek == DayOfWeek.Monday && garbageService.Schedule.Tue != null)
            {
                NextTrigger = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
            }
            else if (now.DayOfWeek == DayOfWeek.Tuesday && garbageService.Schedule.Wed != null)
            {
                NextTrigger = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
            }
            else if (now.DayOfWeek == DayOfWeek.Wednesday && garbageService.Schedule.Thu != null)
            {
                NextTrigger = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
            }
            else if (now.DayOfWeek == DayOfWeek.Thursday && garbageService.Schedule.Fri != null)
            {
                NextTrigger = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
            }
            else if (now.DayOfWeek == DayOfWeek.Friday && garbageService.Schedule.Sat != null)
            {
                NextTrigger = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
            }
            else if (now.DayOfWeek == DayOfWeek.Saturday && garbageService.Schedule.Sun != null)
            {
                NextTrigger = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
            }
            else if (now.DayOfWeek == DayOfWeek.Sunday && garbageService.Schedule.Mon != null)
            {
                NextTrigger = new DateTime(now.Year, now.Month, now.Day, 22, 0, 0);
            }

            var calendar = await collectionClient.SolidWasteCalendar(swSummary.AccountSummaryType, auth);
            var nextGarbagePickup = calendar.GetNextPickup(garbageService.ServicePointId);
            if (nextGarbagePickup.HasValue && DateOnly.FromDateTime(nextGarbagePickup.Value) == DateOnly.FromDateTime(now.AddDays(1)))
            {
                ToDisplay.Add(SolidWasteType.Garbage);
            }

            if (recycleService != null)
            {
                var nextRecyclePickup = calendar.GetNextPickup(recycleService.ServicePointId);
                if (nextRecyclePickup.HasValue && DateOnly.FromDateTime(nextRecyclePickup.Value) == DateOnly.FromDateTime(now.AddDays(1)))
                {
                    ToDisplay.Add(SolidWasteType.Recycle);
                }
            }

            if (foodYardWasteService != null)
            {
                var nextFoodYardWastePickup = calendar.GetNextPickup(foodYardWasteService.ServicePointId);
                if (nextFoodYardWastePickup.HasValue && DateOnly.FromDateTime(nextFoodYardWastePickup.Value) == DateOnly.FromDateTime(now.AddDays(1)))
                {
                    ToDisplay.Add(SolidWasteType.FoodYardWaste);
                }
            }
        }
    }
}
