using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AppleFlyover
{
    public class SettingsManager
    {
        public string LastHueIp
        {
            get
            {
                return GetSetting<string>("LastHueIp");
            }
            set
            {
                ApplicationData.Current.LocalSettings.Values["LastHueIp"] = value;
            }
        }

        public SettingsManager()
        {
        }

        private T GetSetting<T>(string key)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(key))
            {
                return (T)localSettings.Values[key];
            }
            else
            {
                return default;
            }
        }
    }
}
