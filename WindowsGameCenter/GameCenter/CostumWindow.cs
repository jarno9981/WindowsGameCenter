using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WinRT.Interop;

namespace GameCenter
{
    public class CustomWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int WS_SYSMENU = 0x80000;

        public CustomWindow()
        {
            // Remove window buttons
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            int style = GetWindowLong(hWnd, GWL_STYLE);
            SetWindowLong(hWnd, GWL_STYLE, style & ~(WS_MAXIMIZEBOX | WS_MINIMIZEBOX | WS_SYSMENU));

            // Extend into titlebar
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);

            // Make window fullscreen
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);

            if (displayArea is not null)
            {
                // Set window size to match display
                var windowSize = new Windows.Graphics.SizeInt32(
                    displayArea.WorkArea.Width,
                    displayArea.WorkArea.Height
                );
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(
                    displayArea.WorkArea.X,
                    displayArea.WorkArea.Y,
                    windowSize.Width,
                    windowSize.Height
                ));
            }

            // Set to fullscreen mode
            if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.IsAlwaysOnTop = true;
                overlappedPresenter.IsMaximizable = false;
                overlappedPresenter.IsMinimizable = false;
                overlappedPresenter.IsResizable = true;
                overlappedPresenter.SetBorderAndTitleBar(true, false);
                overlappedPresenter.Maximize();
            }
        }
    }
}
