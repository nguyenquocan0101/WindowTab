using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowTab.Services;

internal sealed class TaskbarTransparency(Action<string> report) : IDisposable
{
    private const string ModuleName = "WindowTab.Taskbar.dll";
    private const int EndpointNotFound = unchecked((int)0x80070490);
    private const int ExplorerHasTranslucentTb = unchecked((int)0x800700AA);
    private const int ExplorerHasOldWindowTab = unchecked((int)0x8007047E);
    private static readonly Guid SiteClass = new("d2568989-b623-475e-a390-291b4d6c2490");
    private static readonly int SessionId = GetSessionId();
    private readonly EventWaitHandle _enabled = new(false, EventResetMode.ManualReset,
        @"Local\WindowTab.TaskbarTransparency.Enabled.2");
    private readonly EventWaitHandle _applied = new(false, EventResetMode.ManualReset,
        @"Local\WindowTab.TaskbarTransparency.Applied");
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private Task<int>? _attachment;
    private uint _explorerPid;
    private long _attemptStarted;
    private bool _reported;
    private bool _conflict;
    private int _attempts;

    public static string? CheckAvailability(bool checkRunningConflict = true)
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64 ||
            RuntimeInformation.OSArchitecture != Architecture.X64 ||
            !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
            return "Cần Windows 11 22H2 trở lên trên máy x64 để làm trong suốt taskbar.";

        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, ModuleName)))
            return "Không tìm thấy mô-đun làm trong suốt taskbar của WindowTab.";

        return checkRunningConflict && IsTranslucentTbRunning()
            ? "Hãy thoát TranslucentTB trước khi bật taskbar trong suốt."
            : null;
    }

    public void SetEnabled(bool enabled)
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _enabled.Reset();
        _reported = false;
        _attempts = 0;
        if (!enabled)
            return;

        _applied.Reset();
        _attemptStarted = Environment.TickCount64;
        if (_attachment is { IsCompletedSuccessfully: true } previous && previous.Result < 0)
            _explorerPid = 0;
        _timer.Tick += OnTick;
        _timer.Start();
        OnTick(this, EventArgs.Empty);
    }

    private void OnTick(object? sender, EventArgs args)
    {
        if (IsTranslucentTbRunning())
        {
            _enabled.Reset();
            if (!_conflict)
                report("WindowTab đã tạm dừng hiệu ứng taskbar vì TranslucentTB đang chạy.");
            _conflict = true;
            return;
        }

        if (_conflict)
        {
            _conflict = false;
            _attemptStarted = Environment.TickCount64;
            _reported = false;
        }

        _enabled.Set();
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero || GetWindowThreadProcessId(taskbar, out var pid) == 0)
            return;

        if (pid != _explorerPid && _attachment is not { IsCompleted: false })
        {
            _explorerPid = pid;
            _applied.Reset();
            _reported = false;
            _attempts = 0;
            StartAttachment(pid);
        }
        else if (pid == _explorerPid && _attachment is { IsCompletedSuccessfully: true } previous &&
            previous.Result == EndpointNotFound && _attempts < 6 && Environment.TickCount64 - _attemptStarted >= 5000)
        {
            StartAttachment(pid);
        }

        if (_reported || _applied.WaitOne(0))
            return;

        if (_attachment is { IsCompletedSuccessfully: true } completed && completed.Result < 0)
        {
            if (completed.Result == EndpointNotFound && _attempts < 6)
                return;

            _reported = true;
            report(completed.Result switch
            {
                ExplorerHasTranslucentTb => "Explorer vẫn giữ mô-đun TranslucentTB. Hãy khởi động lại Explorer hoặc đăng xuất rồi đăng nhập lại.",
                ExplorerHasOldWindowTab => "Explorer vẫn giữ mô-đun WindowTab cũ. Hãy khởi động lại Explorer để áp dụng bản mới.",
                _ => $"Không kết nối được với taskbar Windows (0x{completed.Result:X8})."
            });
        }
        else if (Environment.TickCount64 - _attemptStarted >= 10000)
        {
            _reported = true;
            report("Windows chưa áp dụng được hiệu ứng trong suốt cho taskbar.");
        }
    }

    private void StartAttachment(uint pid)
    {
        _attempts++;
        _attemptStarted = Environment.TickCount64;
        // XAML diagnostics can treat later calls on an already-used thread as a no-op.
        _attachment = Task.Factory.StartNew(() => Attach(pid), CancellationToken.None,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private static bool IsTranslucentTbRunning()
    {
        var processes = Process.GetProcessesByName("TranslucentTB");
        var running = false;
        foreach (var process in processes)
        {
            using (process)
            {
                try { running |= process.SessionId == SessionId; }
                catch (InvalidOperationException) { }
            }
        }
        return running;
    }

    private static int GetSessionId()
    {
        using var process = Process.GetCurrentProcess();
        return process.SessionId;
    }

    private static int Attach(uint pid)
    {
        try
        {
            using var explorer = Process.GetProcessById((int)pid);
            foreach (ProcessModule module in explorer.Modules)
            {
                if (string.Equals(module.ModuleName, "ExplorerTAP.dll", StringComparison.OrdinalIgnoreCase))
                    return ExplorerHasTranslucentTb;
            }
            var attachmentName = $@"Local\WindowTab.TaskbarTransparency.Explorer.{pid}";
            if (EventWaitHandle.TryOpenExisting(attachmentName, out var existing))
            {
                existing.Dispose();
                if (!EventWaitHandle.TryOpenExisting(attachmentName + ".2", out var version))
                    return ExplorerHasOldWindowTab;
                version.Dispose();
                return 0;
            }

            var path = Path.Combine(AppContext.BaseDirectory, ModuleName);
            var result = EndpointNotFound;
            for (var endpoint = 1; endpoint <= 60 && result == EndpointNotFound; endpoint++)
                result = InitializeXamlDiagnosticsEx($"VisualDiagConnection{endpoint}", pid, "", path, SiteClass, null);

            return result;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            DiagnosticLog.Write($"Taskbar module load failed: {ex.Message}");
            return ex.HResult;
        }
        catch (Exception ex) when (ex is Win32Exception or ArgumentException or InvalidOperationException)
        {
            return EndpointNotFound;
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        _enabled.Reset();
        _enabled.Dispose();
        _applied.Dispose();
    }

    [DllImport("Windows.UI.Xaml.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int InitializeXamlDiagnosticsEx(string endpoint, uint pid, string diagnosticsPath,
        string tapPath, Guid siteClass, string? initializationData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
