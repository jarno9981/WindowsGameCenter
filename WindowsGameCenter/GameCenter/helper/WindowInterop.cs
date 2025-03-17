using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.helper
{
    public static class WindowInterop
    {
        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        internal interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("45D64A29-A63E-4CB6-B498-5781D298CB4F")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        public static IntPtr GetWindowHandle(Window window)
        {
            var nativeWindow = window as IWindowNative;
            return nativeWindow?.WindowHandle ?? IntPtr.Zero;
        }
    }
}
