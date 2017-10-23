using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AppleFlyover
{
    public static class HelperMethods
    {
        public static async Task CallOnUiThreadAsync(CoreDispatcher dispatcher, DispatchedHandler handler) =>
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);

        public static async Task CallOnMainViewUiThreadAsync(DispatchedHandler handler) =>
            await CallOnUiThreadAsync(CoreApplication.MainView.CoreWindow.Dispatcher, handler);
    }
}
