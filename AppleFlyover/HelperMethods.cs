using System;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;

namespace AppleFlyover
{
    public static class HelperMethods
    {
        public static void CallOnUiThreadAsync(DispatcherQueue dispatcherQueue, DispatcherQueueHandler handler) => dispatcherQueue.TryEnqueue(handler);

        public static AppWindow GetAppWindow(this Microsoft.UI.Xaml.Window window)
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            return GetAppWindowFromWindowHandle(windowHandle);
        }

        private static AppWindow GetAppWindowFromWindowHandle(IntPtr windowHandle)
        {
            var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
}
