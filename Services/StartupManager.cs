using Microsoft.Win32;
using System.Windows.Forms;

namespace WindowTab.Services;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WindowTab";

    public static void Apply(bool enabled)
    {
        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            runKey.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            runKey.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
