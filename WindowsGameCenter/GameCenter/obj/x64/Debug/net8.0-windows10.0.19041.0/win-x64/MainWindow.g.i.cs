// Updated by XamlIntelliSenseFileGenerator 13-2-2025 16:06:33
#pragma checksum "C:\Users\Jarno\Desktop\WindowsGameCenter\WindowsGameCenter\WindowsGameCenter\GameCenter\MainWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "F2D116CAA5B0E59D7996A876B9DF9A6205BDCBD24C63BA05518AFBF73F243FF7"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GameCenter
{
    partial class MainWindow : global::GameCenter.CustomWindow
    {
#pragma warning restore 0649
#pragma warning restore 0169
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2502")]
        private bool _contentLoaded;

        /// <summary>
        /// InitializeComponent()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2502")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent()
        {
            if (_contentLoaded)
                return;

            _contentLoaded = true;

            global::System.Uri resourceLocator = new global::System.Uri("ms-appx:///MainWindow.xaml");
            global::Microsoft.UI.Xaml.Application.LoadComponent(this, resourceLocator, global::Microsoft.UI.Xaml.Controls.Primitives.ComponentResourceLocation.Application);
        }

        partial void UnloadObject(global::Microsoft.UI.Xaml.DependencyObject unloadableObject);

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2502")]
        private interface IMainWindow_Bindings
        {
            void Initialize();
            void Update();
            void StopTracking();
            void DisconnectUnloadedObject(int connectionId);
        }

        private interface IMainWindow_BindingsScopeConnector
        {
            global::System.WeakReference Parent { get; set; }
            bool ContainsElement(int connectionId);
            void RegisterForElementConnection(int connectionId, global::Microsoft.UI.Xaml.Markup.IComponentConnector connector);
        }

        internal global::Microsoft.UI.Xaml.Controls.Grid AppTitleBar;
        internal global::Microsoft.UI.Xaml.Controls.Button SearchButton;
        internal global::Microsoft.UI.Xaml.Controls.Flyout SearchFlyout;
        internal global::Microsoft.UI.Xaml.Controls.TextBox SearchBox;
        internal global::Microsoft.UI.Xaml.Controls.ListView GameListView;
        internal global::Microsoft.UI.Xaml.Controls.Button NetworkButton;
        internal global::Microsoft.UI.Xaml.Controls.FontIcon NetworkIcon;
        internal global::Microsoft.UI.Xaml.Controls.Flyout NetworkFlyout;
        internal global::Microsoft.UI.Xaml.Controls.TextBlock DeviceNameText;
        internal global::Microsoft.UI.Xaml.Controls.TextBlock IpAddressText;
        internal global::Microsoft.UI.Xaml.Controls.Grid SpeedGraph;
        internal global::Microsoft.UI.Xaml.Shapes.Polyline UploadSpeedLine;
        internal global::Microsoft.UI.Xaml.Shapes.Polyline DownloadSpeedLine;
        internal global::Microsoft.UI.Xaml.Controls.TextBlock UploadSpeedText;
        internal global::Microsoft.UI.Xaml.Controls.TextBlock DownloadSpeedText;
        internal global::Microsoft.UI.Xaml.Controls.Button VolumeButton;
        internal global::Microsoft.UI.Xaml.Controls.Flyout VolumeFlyout;
        internal global::Microsoft.UI.Xaml.Controls.Slider VolumeSlider;
        internal global::Microsoft.UI.Xaml.Controls.ComboBox AudioSourceComboBox;
        internal global::Microsoft.UI.Xaml.Controls.TextBlock TimeDisplay;
        internal global::Microsoft.UI.Xaml.Controls.Button CloseButton;
        internal global::Microsoft.UI.Xaml.Controls.NavigationView NavView;
        internal global::Microsoft.UI.Xaml.Controls.Button AccountButton;
        internal global::Microsoft.UI.Xaml.Controls.Frame ContentFrame;
#pragma warning restore 0649
#pragma warning restore 0169
    }
}


