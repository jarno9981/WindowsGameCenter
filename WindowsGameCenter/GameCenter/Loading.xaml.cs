using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using GameCenter.Helpers;


namespace GameCenter;

public sealed partial class Loading : Window
{
    private DispatcherTimer _timer;
    private int _progress = 0;

    public Loading()
    {
        this.InitializeComponent();

        // Extend into the titlebar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(null);

        // Remove default title bar buttons
        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

        // Set window size
        appWindow.Resize(new Windows.Graphics.SizeInt32(400, 275));

        // Initialize and start the timer
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
        _timer.Interval = TimeSpan.FromMilliseconds(100); // Update every 100ms
        _timer.Start();

    }

    private void Timer_Tick(object sender, object e)
    {
        _progress += 5; // Increase by 2% each tick
        LoadingProgressBar.Value = _progress;

        if (_progress >= 100)
        {
            _timer.Stop();
            LaunchMainWindow();
        }
    }

    private void LaunchMainWindow()
    {
        var mainWindow = new MainWindow();
        mainWindow.Activate();
        this.Close();
    }
}
