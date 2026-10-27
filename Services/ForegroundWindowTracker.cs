using System.Text;
using System.Windows.Forms;
using WindowTab.Platform;

namespace WindowTab.Services;

internal sealed class ForegroundWindowTracker : IDisposable
{
    private readonly object _gate = new();
    private readonly List<IntPtr> _mostRecent = [];
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly IntPtr _hook;
    private bool _disposed;

    public ForegroundWindowTracker()
    {
        _callback = OnWinEvent;
        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EventSystemForeground,
            NativeMethods.EventSystemForeground,
            IntPtr.Zero,
            _callback,
            0,
            0,
            NativeMethods.WineventOutOfContext | NativeMethods.WineventSkipOwnProcess);

        SeedFromCurrentDesktop();
    }

    public bool IsTrackingAvailable => _hook != IntPtr.Zero;
    public bool IncludeDesktop { get; set; }

    public static bool IsDesktopWindow(IntPtr window)
    {
        if (window == IntPtr.Zero) return false;
        if (window == NativeMethods.GetShellWindow()) return true;
        var className = new StringBuilder(256);
        NativeMethods.GetClassName(window, className, className.Capacity);
        return className.ToString() is "Progman" or "WorkerW";
    }

    public IReadOnlyList<IntPtr> GetOrderedWindows(IntPtr current, string monitorDeviceName)
    {
        var result = new List<IntPtr>();
        var seen = new HashSet<IntPtr>();

        void AddIfUsable(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !seen.Add(hWnd) || !IsEligible(hWnd)) return;
            if (!string.Equals(Screen.FromHandle(hWnd).DeviceName, monitorDeviceName, StringComparison.OrdinalIgnoreCase)) return;
            result.Add(hWnd);
        }

        AddIfUsable(current);

        List<IntPtr> recent;
        lock (_gate)
        {
            recent = [.. _mostRecent];
        }

        foreach (var hWnd in recent) AddIfUsable(hWnd);

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            AddIfUsable(hWnd);
            return true;
        }, IntPtr.Zero);

        // The desktop is a single optional terminal entry, never a window between apps.
        result.RemoveAll(IsDesktopWindow);
        if (IncludeDesktop)
        {
            var desktop = NativeMethods.GetShellWindow();
            if (IsEligible(desktop) && Screen.FromHandle(desktop).DeviceName == monitorDeviceName) result.Add(desktop);
        }

        return result;
    }

    public bool IsEligible(IntPtr hWnd)
    {
        var desktop = IsDesktopWindow(hWnd);
        if (desktop && (!IncludeDesktop || hWnd != NativeMethods.GetShellWindow())) return false;
        if (hWnd == IntPtr.Zero || !NativeMethods.IsWindow(hWnd) ||
            !NativeMethods.IsWindowVisible(hWnd) || NativeMethods.IsIconic(hWnd) ||
            NativeMethods.GetWindow(hWnd, NativeMethods.GwOwner) != IntPtr.Zero)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
        if (processId == 0 || processId == _ownProcessId || (!desktop && NativeMethods.GetWindowTextLength(hWnd) == 0))
        {
            return false;
        }

        if (NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DwmwaCloaked, out var cloaked, sizeof(int)) == 0 && cloaked != 0)
        {
            return false;
        }

        return NativeMethods.GetWindowRect(hWnd, out var rect) && rect.Width >= 80 && rect.Height >= 60;
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var length = NativeMethods.GetWindowTextLength(hWnd);
        if (length <= 0) return string.Empty;
        var text = new StringBuilder(length + 1);
        NativeMethods.GetWindowText(hWnd, text, text.Capacity);
        return text.ToString();
    }

    private void SeedFromCurrentDesktop()
    {
        var seed = new List<IntPtr>();
        var seen = new HashSet<IntPtr>();
        var foreground = NativeMethods.GetForegroundWindow();
        if (IsEligible(foreground) && seen.Add(foreground)) seed.Add(foreground);

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (IsEligible(hWnd) && seen.Add(hWnd)) seed.Add(hWnd);
            return true;
        }, IntPtr.Zero);

        lock (_gate) _mostRecent.AddRange(seed);
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr hWnd, int objectId, int childId, uint eventThread, uint eventTime)
    {
        if (_disposed || eventType != NativeMethods.EventSystemForeground ||
            objectId != NativeMethods.ObjIdWindow || childId != NativeMethods.ChildIdSelf ||
            !IsEligible(hWnd))
        {
            return;
        }

        Remember(hWnd);
    }

    private void Remember(IntPtr hWnd)
    {
        lock (_gate)
        {
            _mostRecent.Remove(hWnd);
            _mostRecent.Insert(0, hWnd);
            if (_mostRecent.Count > 64) _mostRecent.RemoveRange(64, _mostRecent.Count - 64);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hook != IntPtr.Zero) NativeMethods.UnhookWinEvent(_hook);
    }
}
