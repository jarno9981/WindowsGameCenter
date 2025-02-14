using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using System;
using WinRT.Interop;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI;
using Windows.UI;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Media;

namespace GameCenter.Pages
{
    public sealed partial class InstallerProgressWindow : Window
    {
        public event EventHandler CancelRequested;
        private AppWindow _appWindow;
        private DispatcherTimer _autoCloseTimer;
        private IntPtr _hwnd;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        public InstallerProgressWindow()
        {
            this.InitializeComponent();

            SetupWindow();
            SetupTitleBar();
            SetupAutoCloseTimer();
        }

        private void SetupWindow()
        {
            _appWindow = GetAppWindowForCurrentWindow();
            if (_appWindow != null)
            {
                _appWindow.Resize(new Windows.Graphics.SizeInt32(400, 250));
            }

            SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };

            _hwnd = WindowNative.GetWindowHandle(this);
            SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        private void SetupTitleBar()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            if (_appWindow != null)
            {
                _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
        }

        private void SetupAutoCloseTimer()
        {
            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
            _autoCloseTimer.Interval = TimeSpan.FromSeconds(30);
            _autoCloseTimer.Start();
        }

        private void AutoCloseTimer_Tick(object sender, object e)
        {
            _autoCloseTimer.Stop();
            this.Close();
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        public void Initialize(string launcherName, bool isUninstall)
        {
            TitleTextBlock.Text = isUninstall ? $"Uninstalling {launcherName}" : $"Installing {launcherName}";
            StatusTextBlock.Text = isUninstall
                ? $"Please wait while {launcherName} is being uninstalled..."
                : $"Please wait while {launcherName} is being installed...";
            AppTitleTextBlock.Text = isUninstall ? $"Uninstalling {launcherName}" : $"Installing {launcherName}";
        }

        public void SetAlwaysOnTop(bool alwaysOnTop)
        {
            if (alwaysOnTop)
            {
                SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
            else
            {
                SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        public void UpdateStatus(string status)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StatusTextBlock.Text = status;
            });
        }

        public void SetProgress(bool isIndeterminate, double value = 0)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (isIndeterminate)
                {
                    ProgressRing.IsIndeterminate = true;
                }
                else
                {
                    ProgressRing.IsIndeterminate = false;
                    ProgressRing.Value = value;
                }
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        public void ResetAutoCloseTimer()
        {
            _autoCloseTimer.Stop();
            _autoCloseTimer.Start();
        }
    }
}