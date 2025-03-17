using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameCenter.Helpers.Models
{
    public static class IconExtractor
    {
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // Additional API for alternative icon extraction
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x1;

        public static async Task<BitmapImage> ExtractIconFromFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                // First try to extract icon using Shell32
                IntPtr large;
                IntPtr small;
                int iconCount = ExtractIconEx(filePath, 0, out large, out small, 1);

                if (iconCount > 0 && large != IntPtr.Zero)
                {
                    try
                    {
                        // Convert the icon to a bitmap
                        using (Icon icon = Icon.FromHandle(large))
                        {
                            return await ConvertIconToBitmapImageAsync(icon);
                        }
                    }
                    finally
                    {
                        // Clean up
                        DestroyIcon(large);
                        if (small != IntPtr.Zero)
                            DestroyIcon(small);
                    }
                }
                else if (small != IntPtr.Zero)
                {
                    try
                    {
                        // Try with small icon if large failed
                        using (Icon icon = Icon.FromHandle(small))
                        {
                            return await ConvertIconToBitmapImageAsync(icon);
                        }
                    }
                    finally
                    {
                        DestroyIcon(small);
                    }
                }

                // If ExtractIconEx failed, try alternative method using SHGetFileInfo
                return await ExtractIconUsingSHGetFileInfoAsync(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting icon from {filePath}: {ex.Message}");

                // Try alternative method if primary method fails
                try
                {
                    return await ExtractIconUsingSHGetFileInfoAsync(filePath);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Alternative icon extraction also failed: {ex2.Message}");
                    return null;
                }
            }
        }

        private static async Task<BitmapImage> ExtractIconUsingSHGetFileInfoAsync(string filePath)
        {
            SHFILEINFO shfi = new SHFILEINFO();
            IntPtr hSuccess = SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_LARGEICON);

            if (hSuccess != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                try
                {
                    using (Icon icon = Icon.FromHandle(shfi.hIcon))
                    {
                        return await ConvertIconToBitmapImageAsync(icon);
                    }
                }
                finally
                {
                    DestroyIcon(shfi.hIcon);
                }
            }

            return null;
        }

        public static async Task<BitmapImage> ExtractIconForFileTypeAsync(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return null;

            // Make sure extension starts with a dot
            if (!extension.StartsWith("."))
                extension = "." + extension;

            try
            {
                // Create a temporary file with the specified extension
                string tempFilePath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, $"temp_{Guid.NewGuid()}{extension}");
                File.WriteAllText(tempFilePath, "");

                try
                {
                    // Get the icon for this file type
                    SHFILEINFO shfi = new SHFILEINFO();
                    IntPtr hSuccess = SHGetFileInfo(tempFilePath, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_LARGEICON);

                    if (hSuccess != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                    {
                        try
                        {
                            using (Icon icon = Icon.FromHandle(shfi.hIcon))
                            {
                                return await ConvertIconToBitmapImageAsync(icon);
                            }
                        }
                        finally
                        {
                            DestroyIcon(shfi.hIcon);
                        }
                    }

                    return null;
                }
                finally
                {
                    // Clean up the temporary file
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting icon for file type {extension}: {ex.Message}");
                return null;
            }
        }

        private static async Task<BitmapImage> ConvertIconToBitmapImageAsync(Icon icon)
        {
            try
            {
                // Save the icon to a temporary file
                string tempIconPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, $"icon_{Guid.NewGuid()}.png");

                using (Bitmap bitmap = icon.ToBitmap())
                {
                    bitmap.Save(tempIconPath);
                }

                // Load the icon as a BitmapImage
                var bitmapImage = new BitmapImage();
                var storageFile = await StorageFile.GetFileFromPathAsync(tempIconPath);

                using (IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read))
                {
                    await bitmapImage.SetSourceAsync(stream);
                }

                // Delete the temporary file
                await storageFile.DeleteAsync();

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting icon to bitmap: {ex.Message}");
                return null;
            }
        }
    }
}

