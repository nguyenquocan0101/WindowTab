using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WindowTab.Views;

// Retain native numeric editing, range validation and keyboard stepping inside the styled field.
internal sealed class SettingsNumber : NumericUpDown
{
    public SettingsNumber()
    {
        BorderStyle = BorderStyle.None;
        TextAlign = HorizontalAlignment.Right;
        BackColor = SettingsTheme.Field;
        ForeColor = SettingsTheme.Ink;
        Font = SettingsTheme.Strong;
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        foreach (Control child in Controls)
        {
            if (child is TextBox)
                child.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);
            else if (child.Visible) child.Visible = false;
        }
    }
}

// Keep native range/accessibility semantics; route styled pointer and keyboard input through Value.
internal sealed class SettingsSlider : TrackBar
{
    private bool _dragging;
    private bool _hovered;
    private int _wheelDelta;

    public SettingsSlider()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        TickStyle = TickStyle.None;
        AutoSize = false;
        Height = 36;
        Cursor = Cursors.Hand;
    }

    protected override void OnValueChanged(EventArgs e) { Invalidate(); base.OnValueChanged(e); }
    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    private float Inset => 16 * DeviceDpi / 96f;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        long? next = e.KeyCode switch
        {
            Keys.Left or Keys.Down => (long)Value - SmallChange,
            Keys.Right or Keys.Up => (long)Value + SmallChange,
            Keys.PageDown => (long)Value - LargeChange,
            Keys.PageUp => (long)Value + LargeChange,
            Keys.Home => Minimum,
            Keys.End => Maximum,
            _ => null
        };
        if (next is not null)
        {
            Value = (int)Math.Clamp(next.Value, Minimum, Maximum);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (e.Delta == 0) return;
        if ((e.Delta > 0 && Value >= Maximum) || (e.Delta < 0 && Value <= Minimum))
        {
            _wheelDelta = 0;
            return;
        }
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        _wheelDelta += e.Delta;
        var notches = _wheelDelta / 120;
        _wheelDelta %= 120;
        if (notches != 0)
            Value = (int)Math.Clamp((long)Value + (long)notches * SmallChange, Minimum, Maximum);
    }

    private void MoveThumb(int x)
    {
        var span = Width - 2 * Inset;
        if (span <= 0 || Maximum <= Minimum) return;
        var fraction = Math.Clamp((x - Inset) / span, 0, 1);
        var step = Math.Max(1, SmallChange);
        var value = Minimum + (int)Math.Round(fraction * (Maximum - Minimum) / step, MidpointRounding.AwayFromZero) * step;
        Value = Math.Clamp(value, Minimum, Maximum);
    }

    protected override void WndProc(ref Message m)
    {
        const int LeftDown = 0x0201, LeftUp = 0x0202, MouseMove = 0x0200, CaptureChanged = 0x0215, Wheel = 0x020A;
        if (Enabled && m.Msg == Wheel)
        {
            var delta = (short)((m.WParam.ToInt64() >> 16) & 0xffff);
            var args = new HandledMouseEventArgs(MouseButtons.None, 0, 0, 0, delta);
            OnMouseWheel(args);
            m.Result = !args.Handled && Parent is { IsHandleCreated: true }
                ? SendMessage(Parent.Handle, m.Msg, m.WParam, m.LParam) : IntPtr.Zero;
            return;
        }
        if (Enabled && m.Msg == LeftDown)
        {
            Focus();
            _dragging = true;
            Capture = true;
            MoveThumb((short)(m.LParam.ToInt64() & 0xffff));
            Invalidate();
            return;
        }
        if (_dragging && m.Msg is MouseMove or LeftUp)
        {
            MoveThumb((short)(m.LParam.ToInt64() & 0xffff));
            if (m.Msg == LeftUp)
            {
                _dragging = false;
                Capture = false;
                Invalidate();
                return;
            }
        }
        if (m.Msg == CaptureChanged) { _dragging = false; Invalidate(); }
        base.WndProc(ref m);
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SettingsTheme.Surface);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = DeviceDpi / 96f;
        var start = Inset;
        var end = Math.Max(start, Width - Inset);
        var center = Height / 2f;
        var fraction = Maximum > Minimum ? (Value - Minimum) / (double)(Maximum - Minimum) : 0;
        var thumbX = start + (float)fraction * (end - start);
        using var rail = new Pen(SettingsTheme.Border, 6 * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var progress = new Pen(Enabled ? SettingsTheme.Accent : SettingsTheme.Muted, 6 * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLine(rail, start, center, end, center);
        if (thumbX > start) e.Graphics.DrawLine(progress, start, center, thumbX, center);
        if (Enabled && (_hovered || Focused || _dragging))
        {
            using var halo = new SolidBrush(SettingsTheme.Selection);
            e.Graphics.FillEllipse(halo, thumbX - 16 * scale, center - 16 * scale, 32 * scale, 32 * scale);
        }
        using var fill = new SolidBrush(SettingsTheme.Surface);
        using var border = new Pen(Enabled ? SettingsTheme.Accent : SettingsTheme.Muted, 2 * scale);
        var thumb = new RectangleF(thumbX - 10 * scale, center - 10 * scale, 20 * scale, 20 * scale);
        e.Graphics.FillEllipse(fill, thumb);
        e.Graphics.DrawEllipse(border, thumb);
        if (Focused && ShowFocusCues)
        {
            using var dot = new SolidBrush(SettingsTheme.Accent);
            e.Graphics.FillEllipse(dot, thumbX - 3 * scale, center - 3 * scale, 6 * scale, 6 * scale);
        }
    }
}
