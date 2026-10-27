using System.Diagnostics;
using System.Drawing.Drawing2D;
using WindowTab.Models;
using WindowTab.Services;

namespace WindowTab.Views;

internal sealed class MotionPreview : Control
{
    private readonly AnimationFrameClock _clock;
    private readonly Stopwatch _watch = new();
    private double _progress;
    private int _duration;
    private bool _playing;

    public MotionPreview()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        Height = 176;
        BackColor = SettingsTheme.Field;
        AccessibleName = "Xem trước hiệu ứng thu nhỏ, trượt và phóng lớn";
        _clock = new AnimationFrameClock(this, OnFrame, Stop);
    }

    public void Play(int durationMs)
    {
        if (!IsHandleCreated) return;
        _duration = Math.Max(100, durationMs);
        _progress = 0;
        _playing = true;
        _watch.Restart();
        if (!_clock.Start()) Stop();
        Invalidate();
    }

    public void Stop()
    {
        _clock.Stop();
        _playing = false;
        Invalidate();
    }

    private void OnFrame()
    {
        _progress = Math.Clamp(_watch.Elapsed.TotalMilliseconds / _duration, 0, 1);
        if (_progress >= 1) Stop();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);
        var scale = DeviceDpi / 96f;
        var height = Height - (int)(32 * scale);
        var width = Math.Min(Width - (int)(64 * scale), (int)(height * 1.8));
        var home = new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
        if (home.Width < 1 || home.Height < 1) return;
        if (_playing)
        {
            var frame = TransitionMotion.Calculate(home, home, Width, GestureDirection.Left, _progress);
            DrawWindow(e.Graphics, frame.Outgoing, false, scale);
            DrawWindow(e.Graphics, frame.Incoming, true, scale);
        }
        else DrawWindow(e.Graphics, home, _progress >= 1, scale);
    }

    private static void DrawWindow(Graphics graphics, Rectangle rect, bool incoming, float scale)
    {
        using var clip = SettingsTheme.Round(rect, 10 * scale);
        using var fill = new SolidBrush(SettingsTheme.Surface);
        using var border = new Pen(SettingsTheme.Border);
        graphics.FillPath(fill, clip);
        graphics.DrawPath(border, clip);
        var stripe = new Rectangle(rect.Left + (int)(16 * scale), rect.Top + (int)(16 * scale),
            Math.Max(1, rect.Width - (int)(32 * scale)), Math.Max(1, rect.Height - (int)(32 * scale)));
        using var accent = new SolidBrush(incoming ? Color.FromArgb(91, 154, 157) : SettingsTheme.Accent);
        using var muted = new SolidBrush(SettingsTheme.Sidebar);
        graphics.FillEllipse(accent, stripe.Left, stripe.Top, 8 * scale, 8 * scale);
        graphics.FillRectangle(muted, stripe.Left + 16 * scale, stripe.Top, Math.Min(88 * scale, stripe.Width / 2), 8 * scale);
        if (stripe.Height > 40 * scale)
        {
            using var region = SettingsTheme.Round(new RectangleF(stripe.Left, stripe.Top + 24 * scale, stripe.Width * .34f, stripe.Height - 24 * scale), 6 * scale);
            using var tint = new SolidBrush(incoming ? Color.FromArgb(224, 239, 238) : SettingsTheme.Selection);
            graphics.FillPath(tint, region);
            graphics.FillRectangle(muted, stripe.Left + stripe.Width * .4f, stripe.Top + 28 * scale, stripe.Width * .5f, 8 * scale);
            graphics.FillRectangle(muted, stripe.Left + stripe.Width * .4f, stripe.Top + 48 * scale, stripe.Width * .36f, 6 * scale);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _clock.Dispose();
        base.Dispose(disposing);
    }
}
