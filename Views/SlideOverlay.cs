using System.Diagnostics;
using System.Drawing.Drawing2D;
using WindowTab.Models;
using WindowTab.Platform;
using WindowTab.Services;

namespace WindowTab.Views;

internal sealed class SlideOverlay : Form
{
    private readonly AnimationFrameClock _frameClock;
    private readonly Stopwatch _stopwatch = new();
    private readonly List<double> _frameIntervals = [];
    private IntPtr _outgoingThumbnail;
    private IntPtr _incomingThumbnail;
    private Rectangle _outgoingHome;
    private Rectangle _incomingHome;
    private GestureDirection _direction;
    private int _durationMs;
    private Func<bool>? _beforeHide;
    private Action<bool>? _afterHide;
    private bool _boundary;
    private bool _rollingBack;
    private bool _reportedThumbnailUpdateFailure;
    private long _lastFrameTime;

    public SlideOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        BackColor = Color.FromArgb(15, 17, 20);
        DoubleBuffered = true;
        _frameClock = new AnimationFrameClock(this, OnFrame, OnClockFailed);
    }

    protected override bool ShowWithoutActivation => true;

    public void Cancel()
    {
        if (_afterHide is not null) CompleteAnimation(false);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate | NativeMethods.WsExTransparent;
            return parameters;
        }
    }

    public bool Animate(IntPtr fromWindow, IntPtr toWindow, GestureDirection direction, int durationMs,
        Func<bool> beforeHide, Action<bool> afterHide)
    {
        CleanupThumbnails();
        if (!PrepareWindow(fromWindow) || !NativeMethods.IsWindow(toWindow) ||
            Screen.FromHandle(toWindow).DeviceName != Screen.FromHandle(fromWindow).DeviceName ||
            !TryGetWindowRect(toWindow, Bounds, out _incomingHome)) return false;

        var outgoingResult = NativeMethods.DwmRegisterThumbnail(Handle, fromWindow, out _outgoingThumbnail);
        var incomingResult = NativeMethods.DwmRegisterThumbnail(Handle, toWindow, out _incomingThumbnail);
        DiagnosticLog.Write($"Thumbnail register: outgoing=0x{outgoingResult:X8}, incoming=0x{incomingResult:X8}.");
        if (outgoingResult != 0 || incomingResult != 0)
        {
            CleanupThumbnails();
            return false;
        }

        _direction = direction;
        _boundary = false;
        _rollingBack = false;
        _durationMs = Math.Clamp(durationMs, 100, 600);
        _beforeHide = beforeHide;
        _afterHide = afterHide;
        return StartFrames();
    }

    public bool AnimateBoundary(IntPtr fromWindow, GestureDirection direction, int durationMs, Action completed)
    {
        CleanupThumbnails();
        if (!PrepareWindow(fromWindow)) return false;
        if (NativeMethods.DwmRegisterThumbnail(Handle, fromWindow, out _outgoingThumbnail) != 0)
        {
            CleanupThumbnails();
            return false;
        }

        _direction = direction;
        _boundary = true;
        _rollingBack = false;
        _durationMs = Math.Clamp(durationMs, 350, 600);
        _beforeHide = null;
        _afterHide = _ => completed();
        return StartFrames();
    }

    private bool PrepareWindow(IntPtr window)
    {
        if (!NativeMethods.IsWindow(window)) return false;
        Bounds = Screen.FromHandle(window).WorkingArea;
        return TryGetWindowRect(window, Bounds, out _outgoingHome);
    }

    private bool StartFrames()
    {
        _reportedThumbnailUpdateFailure = false;
        if (!RenderFrame(0))
        {
            CleanupThumbnails();
            _beforeHide = null;
            _afterHide = null;
            return false;
        }

        _frameIntervals.Clear();
        Show();
        // Present the fully prepared first frame before starting the movement clock.
        NativeMethods.DwmFlush();
        _lastFrameTime = Stopwatch.GetTimestamp();
        _stopwatch.Restart();
        if (_frameClock.Start()) return true;
        Hide();
        CleanupThumbnails();
        _beforeHide = null;
        _afterHide = null;
        return false;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(10, 13, 18), Color.FromArgb(30, 34, 42), 90f);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    private void OnFrame()
    {
        var now = Stopwatch.GetTimestamp();
        _frameIntervals.Add((now - _lastFrameTime) * 1000d / Stopwatch.Frequency);
        _lastFrameTime = now;
        var progress = Math.Clamp(_stopwatch.Elapsed.TotalMilliseconds / _durationMs, 0, 1);
        if (!RenderFrame(progress))
        {
            CompleteAnimation(!_boundary && !_rollingBack && TryActivateTarget());
            return;
        }

        if (progress < 1) return;
        if (_boundary || _rollingBack)
        {
            CompleteAnimation(false);
            return;
        }

        if (TryActivateTarget())
        {
            CompleteAnimation(true);
            return;
        }

        _rollingBack = true;
        _durationMs = 220;
        _stopwatch.Restart();
    }

    private bool RenderFrame(double progress)
    {
        if (_boundary)
            return UpdateThumbnail(_outgoingThumbnail,
                TransitionMotion.Boundary(_outgoingHome, ClientSize.Width, _direction, progress));

        var frame = TransitionMotion.Calculate(_outgoingHome, _incomingHome, ClientSize.Width, _direction,
            _rollingBack ? 1 - progress : progress);
        var outgoingUpdated = UpdateThumbnail(_outgoingThumbnail, frame.Outgoing);
        var incomingUpdated = UpdateThumbnail(_incomingThumbnail, frame.Incoming);
        return outgoingUpdated && incomingUpdated;
    }

    private bool TryActivateTarget()
    {
        try
        {
            // The target is activated and composed beneath the final preview before revealing it.
            var activated = _beforeHide?.Invoke() ?? false;
            if (activated) NativeMethods.DwmFlush();
            return activated;
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write($"Animation completion callback failed: {ex.GetType().Name}.");
            return false;
        }
    }

    private void OnClockFailed() => CompleteAnimation(!_boundary && !_rollingBack && TryActivateTarget());

    private void CompleteAnimation(bool targetActivated)
    {
        _frameClock.Stop();
        var afterHide = _afterHide;
        _beforeHide = null;
        _afterHide = null;
        Hide();
        CleanupThumbnails();

        if (_frameIntervals.Count > 0)
        {
            var sorted = _frameIntervals.Order().ToArray();
            var average = _frameIntervals.Average();
            var p95 = sorted[(int)Math.Floor((sorted.Length - 1) * 0.95)];
            DiagnosticLog.Write(FormattableString.Invariant(
                $"Frame timing: mode={(_boundary ? "edge" : "transition")}, updates={sorted.Length}, averageMs={average:F2}, p95Ms={p95:F2}, maxMs={sorted[^1]:F2}, updateHz={1000 / average:F1}."));
        }

        afterHide?.Invoke(targetActivated);
    }

    private static bool TryGetWindowRect(IntPtr window, Rectangle screen, out Rectangle result)
    {
        if (!NativeMethods.GetWindowRect(window, out var rect))
        {
            result = Rectangle.Empty;
            return false;
        }

        result = new Rectangle(rect.Left - screen.Left, rect.Top - screen.Top, rect.Width, rect.Height);
        return result.Width > 0 && result.Height > 0;
    }

    private bool UpdateThumbnail(IntPtr thumbnail, Rectangle destination)
    {
        if (thumbnail == IntPtr.Zero) return false;
        var properties = new NativeMethods.DwmThumbnailProperties
        {
            Flags = NativeMethods.DwmTnpRectDestination | NativeMethods.DwmTnpOpacity | NativeMethods.DwmTnpVisible,
            Destination = new NativeMethods.NativeRect
            {
                Left = destination.Left, Top = destination.Top, Right = destination.Right, Bottom = destination.Bottom
            },
            Opacity = 255,
            Visible = true,
            SourceClientAreaOnly = false
        };
        var result = NativeMethods.DwmUpdateThumbnailProperties(thumbnail, ref properties);
        if (result != 0 && !_reportedThumbnailUpdateFailure)
        {
            _reportedThumbnailUpdateFailure = true;
            DiagnosticLog.Write($"Thumbnail property update failed: 0x{result:X8}.");
        }
        return result == 0;
    }

    private void CleanupThumbnails()
    {
        _frameClock.Stop();
        if (_outgoingThumbnail != IntPtr.Zero)
        {
            NativeMethods.DwmUnregisterThumbnail(_outgoingThumbnail);
            _outgoingThumbnail = IntPtr.Zero;
        }
        if (_incomingThumbnail != IntPtr.Zero)
        {
            NativeMethods.DwmUnregisterThumbnail(_incomingThumbnail);
            _incomingThumbnail = IntPtr.Zero;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CleanupThumbnails();
            _frameClock.Dispose();
        }
        base.Dispose(disposing);
    }
}
