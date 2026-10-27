using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using WindowTab.Platform;

namespace WindowTab.Services;

// Only runs during an animation. UI frames are coalesced instead of building a backlog.
internal sealed class AnimationFrameClock(Control dispatcher, Action frame, Action failed) : IDisposable
{
    private FrameRun? _current;

    public bool Start()
    {
        Stop();
        var handle = NativeMethods.CreateWaitableTimerEx(IntPtr.Zero, null, 0x2, 0x00100002);
        if (handle == IntPtr.Zero)
        {
            DiagnosticLog.Write($"High-resolution frame timer unavailable: error={Marshal.GetLastWin32Error()}.");
            return false;
        }

        var run = new FrameRun(handle);
        _current = run;
        new Thread(() => RenderLoop(run)) { IsBackground = true, Name = "WindowTab frame clock" }.Start();
        return true;
    }

    public void Stop() => Interlocked.Exchange(ref _current, null)?.Cancel();

    private void RenderLoop(FrameRun run)
    {
        try
        {
            var period = Stopwatch.Frequency / 60d;
            var nextFrame = Stopwatch.GetTimestamp() + period;
            WaitHandle[] waits = [run.Cancelled, run.Timer];

            while (true)
            {
                var remaining = nextFrame - Stopwatch.GetTimestamp();
                var dueTime = -(long)Math.Max(1, Math.Ceiling(remaining * 10_000_000d / Stopwatch.Frequency));
                if (!NativeMethods.SetWaitableTimer(run.Timer.SafeWaitHandle, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, false))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (WaitHandle.WaitAny(waits) == 0) break;
                if (Interlocked.CompareExchange(ref run.FramePending, 1, 0) == 0)
                {
                    Post(run, () =>
                    {
                        Interlocked.Exchange(ref run.FramePending, 0);
                        frame();
                    });
                }

                nextFrame += period;
                var now = Stopwatch.GetTimestamp();
                if (nextFrame <= now) nextFrame += (Math.Floor((now - nextFrame) / period) + 1) * period;
            }
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write($"Frame clock stopped unexpectedly: {ex.GetType().Name}.");
            Post(run, failed);
        }
        finally
        {
            run.Dispose();
        }
    }

    private void Post(FrameRun run, Action action)
    {
        try
        {
            dispatcher.BeginInvoke((Action)(() =>
            {
                if (ReferenceEquals(_current, run)) action();
            }));
        }
        catch (InvalidOperationException)
        {
            // The UI handle can disappear while the app is exiting.
        }
    }

    public void Dispose() => Stop();

    private sealed class TimerWaitHandle : WaitHandle
    {
        public TimerWaitHandle(IntPtr handle) => SafeWaitHandle = new SafeWaitHandle(handle, ownsHandle: true);
    }

    private sealed class FrameRun(IntPtr timerHandle) : IDisposable
    {
        private readonly object _gate = new();
        private bool _disposed;
        public readonly ManualResetEvent Cancelled = new(false);
        public readonly TimerWaitHandle Timer = new(timerHandle);
        public int FramePending;

        public void Cancel()
        {
            lock (_gate)
            {
                if (!_disposed) Cancelled.Set();
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                Timer.Dispose();
                Cancelled.Dispose();
            }
        }
    }
}
