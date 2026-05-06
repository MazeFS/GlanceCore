namespace GlanceCore.Core;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

public static class WindowHelper
{
    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void EnableMica(Window window)
    {
        var helper = new WindowInteropHelper(window);

        if (helper.Handle == IntPtr.Zero)
        {
            window.SourceInitialized += (s, e) => ApplyMica(new WindowInteropHelper(window).Handle);
        }
        else
        {
            ApplyMica(helper.Handle);
        }
    }

    private static void ApplyMica(IntPtr handle)
    {
        try
        {
            int backdropType = 2; // 2 = Mica, 3 = Acrylic, 4 = Tabbed
            int useImmersiveDarkMode = 1; 

            DwmSetWindowAttribute(handle, 20, ref useImmersiveDarkMode, Marshal.SizeOf(typeof(int)));

            DwmSetWindowAttribute(handle, 33, ref backdropType, Marshal.SizeOf(typeof(int)));
        }
        catch
        {
        }
    }
}