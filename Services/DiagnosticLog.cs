using System.Globalization;

namespace WindowTab.Services;

internal static class DiagnosticLog
{
    private static readonly object Gate = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WindowTab",
        "diagnostics.log");

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(
                    LogPath,
                    $"{DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never interrupt window switching.
        }
    }
}
