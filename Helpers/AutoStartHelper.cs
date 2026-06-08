using Microsoft.Win32;

namespace ProcessLimit.Helpers;

public static class AutoStartHelper
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ProcessLimit";

    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(AppName) != null;
    }

    public static void SetAutoStart(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (key == null) return;

        if (enable)
        {
            var exePath = Environment.ProcessPath ?? AppDomain.CurrentDomain.BaseDirectory + "ProcessLimit.exe";
            key.SetValue(AppName, $"\"{exePath}\" --minimized");
        }
        else
        {
            key.DeleteValue(AppName, false);
        }
    }
}
