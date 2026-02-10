using MuhasibPro.Helpers.WindowHelpers.DisplayMonitor;

namespace MuhasibPro.Helpers.WindowHelpers
{
    public static class WindowPosition
    {
        public static void SwitchToThisWindow(Microsoft.UI.Xaml.Window window)
        {
            if (window != null)
            {
                SwitchToThisWindow(new HWND(WindowNative.GetWindowHandle(window)));
            }
        }

        /// <summary>
        /// Switches the focus to a specified window identified by its handle.
        /// </summary>
        /// <param name="hwnd">Identifies the window to which the focus will be switched.</param>
        public static void SwitchToThisWindow(IntPtr hwnd)
        {
            PInvoke.SwitchToThisWindow(new HWND(hwnd), true);
        }
        public static bool SetForegroundWindow(Microsoft.UI.Xaml.Window window) => SetForegroundWindow(WindowNative.GetWindowHandle(window));

        /// <summary>
        /// Sets the specified window to the foreground, making it the active window. This can bring a window to the front
        /// of other windows.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <returns>Returns true if the operation was successful, otherwise false.</returns>
        public static bool SetForegroundWindow(IntPtr hwnd)
        {
            return PInvoke.SetForegroundWindow(new HWND(hwnd));
        }
        public static void PositionRelativeToParent(Window window, Window parentWindow)
        {
            if (parentWindow == null || window == null) return;

            try
            {
                var hWnd = WindowNative.GetWindowHandle(window);
                var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                var parentHWnd = WindowNative.GetWindowHandle(parentWindow);
                var parentWindowId = Win32Interop.GetWindowIdFromWindow(parentHWnd);
                var parentAppWindow = AppWindow.GetFromWindowId(parentWindowId);

                if (appWindow != null && parentAppWindow != null)
                {
                    // Ekran sınırlarını kontrol et
                    var offsetX = (parentAppWindow.Size.Width - appWindow.Size.Width) / 2;
                    var offsetY = (parentAppWindow.Size.Height - appWindow.Size.Height) / 2;

                    var newX = Math.Max(0, parentAppWindow.Position.X + offsetX);
                    var newY = Math.Max(0, parentAppWindow.Position.Y + offsetY);

                    appWindow.Move(new Windows.Graphics.PointInt32(newX, newY));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error positioning window: {ex.Message}");
            }
        }
        /// <summary>
        /// Centers the specified window on the screen.
        /// </summary>
        /// <param name="window">The window to be centered on the screen.</param>
        /// <returns>Returns true if the operation was successful, otherwise false.</returns>
        public static bool CenterOnScreen(Microsoft.UI.Xaml.Window window) => CenterOnScreen(WindowNative.GetWindowHandle(window));

        /// <summary>
        /// Centers the specified window on the screen.
        /// </summary>
        /// <param name="window">The window to be centered on the screen.</param>
        /// <param name="width">Defines the desired width of the window for centering purposes.</param>
        /// <param name="height">Defines the desired height of the window for centering purposes.</param>
        /// <returns>Returns a boolean indicating whether the window was successfully centered.</returns>
        private static bool CenterOnScreen(Microsoft.UI.Xaml.Window window, double? width, double? height) => CenterOnScreen(WindowNative.GetWindowHandle(window), width, height);

        /// <summary>
        /// Centers the specified window on the screen.
        /// </summary>
        /// <param name="hwnd">The handle of the window to be centered on the screen.</param>
        /// <returns>Returns true if the window was successfully centered.</returns>
        private static bool CenterOnScreen(IntPtr hwnd) => CenterOnScreen(hwnd, null, null);

        /// <summary>
        /// Centers the specified window on the screen.
        /// </summary>
        /// <param name="hwnd">The handle of the window to be centered on the screen.</param>
        /// <param name="width">Specifies the desired width of the window; if not provided, the current width is used.</param>
        /// <param name="height">Specifies the desired height of the window; if not provided, the current height is used.</param>
        /// <returns>Indicates whether the window was successfully repositioned.</returns>
        private static bool CenterOnScreen(IntPtr hwnd, double? width, double? height)
        {
            var monitor = DisplayMonitorHelper.GetMonitorInfo(hwnd);
            if (monitor is null)
                return false;

            var dpi = PInvoke.GetDpiForWindow(new HWND(hwnd));
            if (!PInvoke.GetWindowRect(new HWND(hwnd), out RECT windowRect))
                return false;

            var scalingFactor = dpi / 96.0;
            var w = width.HasValue ? (int)(width.Value * scalingFactor) : (windowRect.right - windowRect.left);
            var h = height.HasValue ? (int)(height.Value * scalingFactor) : (windowRect.bottom - windowRect.top);

            var cx = (monitor.RectMonitor.Left + monitor.RectMonitor.Right) / 2;
            var cy = (monitor.RectMonitor.Bottom + monitor.RectMonitor.Top) / 2;
            var left = (int)cx - (w / 2);
            var top = (int)cy - (h / 2);

            return PInvoke.SetWindowPos(new HWND(hwnd), new HWND(), left, top, w, h,
                Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
                Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        }
        public static void SetWindowOwner(Microsoft.UI.Xaml.Window parentWindow, Microsoft.UI.Xaml.Window childWindow) => SetWindowOwner(WindowNative.GetWindowHandle(parentWindow), WindowNative.GetWindowHandle(childWindow));

        /// <summary>
        /// Sets the owner of a child window to a specified parent window.
        /// </summary>
        /// <param name="parentHwnd">The main window that will own the child window.</param>
        /// <param name="childHwnd">The window that will be owned by the specified parent.</param>
        private static void SetWindowOwner(IntPtr parentHwnd, IntPtr childHwnd)
        {
            NativeMethods.SetWindowLong(childHwnd, -8, parentHwnd);
        }
    }
}
