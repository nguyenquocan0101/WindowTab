using System.Windows.Forms;
using WindowTab.Models;
using WindowTab.Platform;
using WindowTab.Views;

namespace WindowTab.Services;

internal sealed class WindowCycler : IDisposable
{
    private readonly ForegroundWindowTracker _tracker;
    private readonly SlideOverlay _overlay;
    private readonly Queue<GestureDirection> _pending = new();
    private List<IntPtr>? _cycleWindows;
    private int _cycleIndex;
    private DateTime _lastAccepted = DateTime.MinValue;
    private int _cooldownMs;
    private int _animationMs;
    private bool _animating;
    private bool _atBoundary;
    private bool _paused;
    private bool _disposed;

    public WindowCycler(ForegroundWindowTracker tracker, SlideOverlay overlay, AppSettings settings)
    {
        _tracker = tracker;
        _overlay = overlay;
        UpdateSettings(settings);
    }

    public bool IsPaused
    {
        get => _paused;
        set
        {
            _paused = value;
            if (!value) return;
            _pending.Clear();
            _overlay.Cancel();
        }
    }

    public void UpdateSettings(AppSettings settings)
    {
        _cooldownMs = Math.Clamp(settings.CooldownMs, 0, 1000);
        _animationMs = Math.Clamp(settings.AnimationMs, 100, 600);
        if (_tracker.IncludeDesktop != settings.IncludeDesktop)
        {
            _tracker.IncludeDesktop = settings.IncludeDesktop;
            _cycleWindows = null;
            _pending.Clear();
        }
    }

    public void HandleGesture(GestureDirection direction)
    {
        if (_disposed || _paused) return;

        var now = DateTime.UtcNow;
        if (_lastAccepted != DateTime.MinValue && now - _lastAccepted < TimeSpan.FromMilliseconds(_cooldownMs)) return;
        _lastAccepted = now;

        if (_animating)
        {
            if (!_atBoundary && _pending.Count < 1) _pending.Enqueue(direction);
            return;
        }

        StartTransition(direction);
    }

    private void StartTransition(GestureDirection direction)
    {
        if (_disposed || _paused) return;
        var current = NativeMethods.GetForegroundWindow();
        if (_tracker.IncludeDesktop && ForegroundWindowTracker.IsDesktopWindow(current))
            current = NativeMethods.GetShellWindow();
        if (!_tracker.IsEligible(current)) return;

        var monitor = Screen.FromHandle(current).DeviceName;
        var available = _tracker.GetOrderedWindows(current, monitor);
        if (_cycleWindows is null)
        {
            _cycleWindows = [.. available];
        }
        else
        {
            _cycleWindows.RemoveAll(hWnd => !available.Contains(hWnd));
            foreach (var window in available)
                if (!_cycleWindows.Contains(window)) _cycleWindows.Add(window);
        }

        var desktopIndex = _cycleWindows.FindIndex(ForegroundWindowTracker.IsDesktopWindow);
        if (desktopIndex >= 0)
        {
            var desktop = _cycleWindows[desktopIndex];
            _cycleWindows.RemoveAt(desktopIndex);
            _cycleWindows.Add(desktop);
        }
        _cycleIndex = _cycleWindows.IndexOf(current);
        if (_cycleIndex < 0) return;

        var nextIndex = FindNextIndex(direction);
        if (nextIndex < 0)
        {
            _animating = true;
            _atBoundary = true;
            _pending.Clear();
            DiagnosticLog.Write($"Boundary bounce: direction={direction}, windows={_cycleWindows.Count}, travel=20%.");
            if (!_overlay.AnimateBoundary(current, direction, _animationMs, CompleteBoundary)) CompleteBoundary();
            return;
        }

        var target = _cycleWindows[nextIndex];
        var from = current;
        DiagnosticLog.Write($"Transition requested: direction={direction}, durationMs={_animationMs}, windows={_cycleWindows.Count}.");
        _cycleIndex = nextIndex;
        _animating = true;
        _atBoundary = false;

        if (!_overlay.Animate(
            from,
            target,
            direction,
            _animationMs,
            () => ActivateTarget(target),
            CompleteTransition))
        {
            DiagnosticLog.Write("Overlay animation unavailable; attempting immediate activation.");
            CompleteTransition(ActivateTarget(target));
        }
    }

    private int FindNextIndex(GestureDirection direction)
    {
        if (_cycleWindows is null) return -1;

        var step = direction == GestureDirection.Left ? 1 : -1;
        for (var candidate = _cycleIndex + step; candidate >= 0 && candidate < _cycleWindows.Count; candidate += step)
        {
            if (_tracker.IsEligible(_cycleWindows[candidate])) return candidate;
        }

        return -1;
    }

    private bool ActivateTarget(IntPtr target)
    {
        if (!NativeMethods.IsWindow(target)) return false;

        var activated = NativeMethods.SetForegroundWindow(target);
        if (!activated)
        {
            NativeMethods.BringWindowToTop(target);
            activated = NativeMethods.SetForegroundWindow(target);
        }

        if (!activated)
        {
            var flashInfo = new NativeMethods.FlashWindowInfo
            {
                Size = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.FlashWindowInfo>(),
                Window = target,
                Flags = NativeMethods.FlashwTray | NativeMethods.FlashwTimerNoForeground,
                Count = 0,
                Timeout = 0
            };
            NativeMethods.FlashWindowEx(ref flashInfo);
            DiagnosticLog.Write("Foreground activation failed after two attempts; taskbar flash requested.");
        }
        else
        {
            DiagnosticLog.Write("Foreground activation succeeded.");
        }

        return activated;
    }

    private void CompleteTransition(bool targetActivated)
    {
        _animating = false;

        if (!targetActivated)
        {
            _pending.Clear();
            var current = NativeMethods.GetForegroundWindow();
            var currentIndex = _cycleWindows?.IndexOf(current) ?? -1;
            if (currentIndex >= 0) _cycleIndex = currentIndex;
            return;
        }

        if (_pending.Count == 0) return;
        var next = _pending.Dequeue();
        StartTransition(next);
    }

    private void CompleteBoundary()
    {
        _animating = false;
        _atBoundary = false;
        _pending.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pending.Clear();
        _overlay.Dispose();
    }
}
