namespace GlanceCore.Core;

using Microsoft.Win32;
using System.Reflection;

public static class AutoStartManager
{
    private const string AppName = "GlanceCore";
    private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    // English: Enable or disable Windows startup with a silent flag
    public static void SetAutoStart(bool enable)
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true)!;
        if (enable)
        {
            string exePath = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            key.SetValue(AppName, $"\"{exePath}\" --autostart");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }

    public static bool IsAutoStartEnabled()
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, false)!;
        return key.GetValue(AppName) != null;
    }
}