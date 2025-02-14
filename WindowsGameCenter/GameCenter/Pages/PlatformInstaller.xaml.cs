using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;

namespace GameCenter.Pages
{
    public sealed partial class PlatformInstaller : Page, INotifyPropertyChanged
    {
        public ObservableCollection<LauncherInfo> Launchers { get; } = new ObservableCollection<LauncherInfo>();
        private InstallerProgressWindow _progressWindow;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PlatformInstaller()
        {
            this.InitializeComponent();
            InitializeLaunchers();
            ScanForInstalledLaunchers();
        }

        private void InitializeLaunchers()
        {
            Launchers.Add(new LauncherInfo { Name = "Steam", IconPath = "ms-appx:///Assets/SteamIcon.png", DownloadUrl = "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe", InstallPath = @"C:\Program Files (x86)\Steam" });
            Launchers.Add(new LauncherInfo { Name = "Epic Games", IconPath = "ms-appx:///Assets/EpicIcon.png", DownloadUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", InstallPath = @"C:\Program Files (x86)\Epic Games" });
            Launchers.Add(new LauncherInfo { Name = "GOG Galaxy *Note Not Working Still manaully*", IconPath = "ms-appx:///Assets/GOGIcon.png", DownloadUrl = "https://content-system.gog.com/open_link/download?path=/open/galaxy/client/setup_galaxy_2.0.exe", InstallPath = @"C:\Program Files (x86)\GOG Galaxy" });
            Launchers.Add(new LauncherInfo { Name = "Ubisoft Connect (Stable)", IconPath = "ms-appx:///Assets/UbisoftIcon.png", DownloadUrl = "https://ubistatic3-a.akamaihd.net/orbit/launcher_installer/UbisoftConnectInstaller.exe", InstallPath = @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher", UbisoftInstallType = "Stable" });
            Launchers.Add(new LauncherInfo { Name = "Battle.net", IconPath = "ms-appx:///Assets/BattleNetIcon.png", DownloadUrl = "https://us.battle.net/download/getInstaller?os=win&installer=Battle.net-Setup.exe", InstallPath = @"C:\Program Files (x86)\Battle.net", InstallerType = "BattleNet" });
            Launchers.Add(new LauncherInfo { Name = "Rockstar Games Launcher *Note Not Working Still manaully*", IconPath = "ms-appx:///Assets/RockstarIcon.png", DownloadUrl = "https://gamedownloads.rockstargames.com/public/installer/Rockstar-Games-Launcher.exe", InstallPath = @"C:\Program Files\Rockstar Games\Launcher", InstallerType = "Rockstar" });
        }

        private void ScanForInstalledLaunchers()
        {
            foreach (var launcher in Launchers)
            {
                launcher.IsInstalled = Directory.Exists(launcher.InstallPath);
            }
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var launcher = button.CommandParameter as LauncherInfo;

            if (launcher == null)
            {
                await ShowErrorDialog("Error: Unable to retrieve launcher information.");
                return;
            }

            if (launcher.IsInstalled)
            {
                await UninstallLauncher(launcher);
            }
            else
            {
                button.IsEnabled = false;
                await DownloadAndInstallLauncher(launcher);
                button.IsEnabled = true;
            }
        }

        private async Task DownloadAndInstallLauncher(LauncherInfo launcher)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                    Debug.WriteLine($"Attempting to download {launcher.Name} from {launcher.DownloadUrl}");

                    var response = await client.GetAsync(launcher.DownloadUrl);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? 0;
                    var readBytes = 0L;

                    var originalFileName = Path.GetFileName(launcher.DownloadUrl);
                    var sanitizedFileName = SanitizeFileName(originalFileName);

                    if (string.IsNullOrWhiteSpace(sanitizedFileName))
                    {
                        sanitizedFileName = $"{launcher.Name.Replace(" ", "_")}_installer{Path.GetExtension(originalFileName)}";
                    }

                    var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(sanitizedFileName, CreationCollisionOption.GenerateUniqueName);

                    Debug.WriteLine($"Downloading {launcher.Name} to {file.Path}");

                    ShowProgressWindow(launcher.Name);
                    _progressWindow.UpdateStatus($"Downloading {launcher.Name}...");

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer.AsBuffer());
                            readBytes += bytesRead;
                            var progress = (int)((double)readBytes / totalBytes * 100);
                            launcher.Progress = progress;
                            _progressWindow.SetProgress(false, progress);
                            _progressWindow.UpdateStatus($"Downloading {launcher.Name}... {progress}%");
                        }
                    }

                    Debug.WriteLine($"Download complete for {launcher.Name}. Starting installation.");

                    switch (launcher.InstallerType)
                    {
                        case "BattleNet":
                            await InstallBattleNet(file, launcher);
                            break;
                        case "Ubisoft":
                            await InstallUbisoftConnect(file, launcher);
                            break;
                        case "Rockstar":
                            await InstallRockstarLauncher(file, launcher);
                            break;
                        default:
                            if (Path.GetExtension(file.Name).ToLower() == ".msi")
                            {
                                await InstallMsiWithUac(file, launcher);
                            }
                            else
                            {
                                await InstallExeSilently(file, launcher);
                            }
                            break;
                    }

                    launcher.Progress = 100;
                    Debug.WriteLine($"Installation complete for {launcher.Name}.");

                    ScanForInstalledLaunchers();
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"HTTP Request Exception for {launcher.Name}: {ex.Message}");
                    launcher.Progress = 0;
                    await ShowErrorDialog($"Failed to download {launcher.Name}. Please check your internet connection and try again.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"General Exception for {launcher.Name}: {ex.Message}");
                    launcher.Progress = 0;
                    await ShowErrorDialog($"An error occurred while processing {launcher.Name}. Please try again later.");
                }
                finally
                {
                    if (_progressWindow != null)
                    {
                        _progressWindow.Close();
                        _progressWindow = null;
                    }
                }
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitizedName = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            sanitizedName = sanitizedName.Trim('.', ' ');

            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                sanitizedName = "installer";
            }
            if (sanitizedName.Length > 255)
            {
                sanitizedName = sanitizedName.Substring(0, 255);
            }

            return sanitizedName;
        }

        private async Task InstallBattleNet(StorageFile installerFile, LauncherInfo launcher)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = installerFile.Path,
                    Arguments = "--lang=enUS --installpath=\"" + launcher.InstallPath + "\"",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                _progressWindow.UpdateStatus($"Installing {launcher.Name}...");

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Installation failed with exit code {process.ExitCode}.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to start the installation process.");
                    }
                }

                _progressWindow.UpdateStatus($"Done installing {launcher.Name}.");
                _progressWindow.ResetAutoCloseTimer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Installation Exception: {ex.Message}");
                await ShowErrorDialog($"Failed to install {installerFile.Name}. Please try installing it manually.");
            }
        }

        private async Task InstallRockstarLauncher(StorageFile installerFile, LauncherInfo launcher)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = installerFile.Path,
                    // Use silent install parameters and force English language
                    Arguments = "/S /LANG=en-US /SILENT /VERYSILENT /SUPPRESSMSGBOXES /SP- /NOCANCEL",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                _progressWindow.UpdateStatus($"Installing {launcher.Name}...");

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        // Wait for the initial process to start
                        await Task.Delay(2000);

                        // Handle any additional windows that might appear
                        await Task.Run(async () =>
                        {
                            // Look for language selection window
                            var languageWindow = await WaitForWindowAsync("Rockstar Games Launcher", "Rockstar Games Launcher Installer", timeoutSeconds: 30);
                            if (languageWindow != IntPtr.Zero)
                            {
                                // Set window to foreground before sending input
                                NativeMethods.SetForegroundWindow(languageWindow);
                                await Task.Delay(500);

                                // Simulate Enter key
                                SimulateKeyPress(VirtualKeyCode.RETURN);
                                await Task.Delay(1000);
                                SimulateKeyPress(VirtualKeyCode.RETURN);
                            }

                            // Look for any additional confirmation windows
                            var confirmWindow = await WaitForWindowAsync("Rockstar Games Launcher", "", timeoutSeconds: 30);
                            if (confirmWindow != IntPtr.Zero)
                            {
                                NativeMethods.SetForegroundWindow(confirmWindow);
                                await Task.Delay(500);
                                SimulateKeyPress(VirtualKeyCode.RETURN);
                            }
                        });

                        await process.WaitForExitAsync();
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Installation failed with exit code {process.ExitCode}.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to start the installation process.");
                    }
                }

                _progressWindow.UpdateStatus($"Done installing {launcher.Name}.");
                _progressWindow.ResetAutoCloseTimer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Installation Exception: {ex.Message}");
                await ShowErrorDialog($"Failed to install {installerFile.Name}. Please try installing it manually.");
            }
        }
        private void SimulateKeyPress(VirtualKeyCode keyCode)
        {
            var inputs = new INPUT[]
            {
                new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = keyCode,
                            wScan = 0,
                            dwFlags = 0,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                },
                new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = keyCode,
                            wScan = 0,
                            dwFlags = KEYEVENTF.KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            };

            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private async Task<IntPtr> WaitForWindowAsync(string className, string windowTitle, int timeoutSeconds = 30)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now < endTime)
            {
                var hwnd = NativeMethods.FindWindow(className, windowTitle);
                if (hwnd != IntPtr.Zero)
                {
                    return hwnd;
                }
                await Task.Delay(500);
            }
            return IntPtr.Zero;
        }


        private async Task InstallUbisoftConnect(StorageFile installerFile, LauncherInfo launcher)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = installerFile.Path,
                    Arguments = "/S",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                _progressWindow.UpdateStatus($"Installing {launcher.Name}...");

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Installation failed with exit code {process.ExitCode}.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to start the installation process.");
                    }
                }

                _progressWindow.UpdateStatus($"Done installing {launcher.Name}.");
                _progressWindow.ResetAutoCloseTimer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Installation Exception: {ex.Message}");
                await ShowErrorDialog($"Failed to install {installerFile.Name}. Please try installing it manually.");
            }
        }

        private async Task InstallMsiWithUac(StorageFile installerFile, LauncherInfo launcher)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = $"/i \"{installerFile.Path}\" /qn /norestart",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                _progressWindow.UpdateStatus($"Installing {launcher.Name}...");

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Installation failed with exit code {process.ExitCode}.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to start the installation process.");
                    }
                }

                _progressWindow.UpdateStatus($"Done installing {launcher.Name}.");
                _progressWindow.ResetAutoCloseTimer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Installation Exception: {ex.Message}");
                await ShowErrorDialog($"Failed to install {installerFile.Name}. Please try installing it manually.");
            }
        }

        private async Task InstallExeSilently(StorageFile installerFile, LauncherInfo launcher)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = installerFile.Path,
                    Arguments = "/S",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                _progressWindow.UpdateStatus($"Installing {launcher.Name}...");

                using (var process = Process.Start(processStartInfo))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Installation failed with exit code {process.ExitCode}.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to start the installation process.");
                    }
                }

                _progressWindow.UpdateStatus($"Done installing {launcher.Name}.");
                _progressWindow.ResetAutoCloseTimer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Installation Exception: {ex.Message}");
                await ShowErrorDialog($"Failed to install {installerFile.Name}. Please try installing it manually.");
            }
        }

        private async Task UninstallLauncher(LauncherInfo launcher)
        {
            try
            {
                ProcessStartInfo uninstallProcess;

                switch (launcher.InstallerType)
                {
                    case "BattleNet":
                        uninstallProcess = new ProcessStartInfo
                        {
                            FileName = Path.Combine(launcher.InstallPath, "Battle.net Uninstaller.exe"),
                            Arguments = "/S",
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        break;
                    case "Rockstar":
                        uninstallProcess = new ProcessStartInfo
                        {
                            FileName = Path.Combine(launcher.InstallPath, "Uninstall.exe"),
                            Arguments = "/S",
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        break;
                    default:
                        uninstallProcess = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c wmic product where name='{launcher.Name}' call uninstall /nointeractive",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        break;
                }

                ShowProgressWindow(launcher.Name, isUninstall: true);
                _progressWindow.UpdateStatus($"Uninstalling {launcher.Name}...");

                using (var process = Process.Start(uninstallProcess))
                {
                    if (process != null)
                    {
                        await process.WaitForExitAsync();
                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Uninstallation failed with exit code {process.ExitCode}.");
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to start the uninstallation process.");
                    }
                }

                _progressWindow.UpdateStatus($"Done uninstalling {launcher.Name}.");
                _progressWindow.ResetAutoCloseTimer();

                launcher.IsInstalled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Uninstallation Exception for {launcher.Name}: {ex.Message}");
                await ShowErrorDialog($"Failed to uninstall {launcher.Name}. Please try uninstalling it manually.");
            }
            finally
            {
                if (_progressWindow != null)
                {
                    _progressWindow.Close();
                    _progressWindow = null;
                }
            }
        }

        private void ShowProgressWindow(string launcherName, bool isUninstall = false)
        {
            _progressWindow = new InstallerProgressWindow();
            _progressWindow.Initialize(launcherName, isUninstall);
            _progressWindow.CancelRequested += ProgressWindow_CancelRequested;
            _progressWindow.SetAlwaysOnTop(true);
            _progressWindow.Activate();
        }

        private void ProgressWindow_CancelRequested(object sender, EventArgs e)
        {
            // Implement cancellation logic here
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK"
            };

            errorDialog.XamlRoot = this.XamlRoot;
            await errorDialog.ShowAsync();
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    }

    internal enum InputType : uint
    {
        INPUT_MOUSE = 0,
        INPUT_KEYBOARD = 1,
        INPUT_HARDWARE = 2
    }

    internal enum KEYEVENTF : uint
    {
        KEYDOWN = 0x0000,
        EXTENDEDKEY = 0x0001,
        KEYUP = 0x0002,
        UNICODE = 0x0004,
        SCANCODE = 0x0008
    }

    internal enum VirtualKeyCode : ushort
    {
        RETURN = 0x0D,
        ESCAPE = 0x1B,
        TAB = 0x09
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public InputType type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public VirtualKeyCode wVk;
        public ushort wScan;
        public KEYEVENTF dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
    public class LauncherInfo : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string IconPath { get; set; }
        public string DownloadUrl { get; set; }
        public string InstallPath { get; set; }
        public string UbisoftInstallType { get; set; }
        public string InstallerType { get; set; }

        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled != value)
                {
                    _isInstalled = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActionButtonText));
                }
            }
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressVisibility));
                }
            }
        }

        public Visibility ProgressVisibility => Progress > 0 && Progress < 100 ? Visibility.Visible : Visibility.Collapsed;

        public string ActionButtonText => IsInstalled ? "Uninstall" : "Install";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}