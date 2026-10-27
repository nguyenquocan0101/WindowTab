using System.Threading;

namespace WindowTab;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, name: @"Local\WindowTab.Singleton", out var createdNew);
        if (!createdNew)
        {
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApplicationContext(args.Contains("--settings", StringComparer.OrdinalIgnoreCase)));
    }
}
