using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace WindowTab.Views;

internal static class SettingsTheme
{
    public static readonly Color Background = Color.FromArgb(245, 247, 252);
    public static readonly Color Sidebar = Color.FromArgb(235, 240, 248);
    public static readonly Color Surface = Color.FromArgb(253, 254, 255);
    public static readonly Color Field = Color.FromArgb(244, 247, 252);
    public static readonly Color Border = Color.FromArgb(217, 225, 238);
    public static readonly Color Ink = Color.FromArgb(28, 39, 59);
    public static readonly Color Muted = Color.FromArgb(91, 107, 130);
    public static readonly Color Accent = Color.FromArgb(48, 99, 221);
    public static readonly Color AccentHover = Color.FromArgb(36, 81, 193);
    public static readonly Color Selection = Color.FromArgb(218, 230, 253);
    public static readonly Color Success = Color.FromArgb(32, 115, 85);
    public static readonly Color Error = Color.FromArgb(174, 52, 66);
    public static readonly Font Body = new("Segoe UI", 10f);
    public static readonly Font Small = new("Segoe UI", 9f);
    public static readonly Font Strong = new("Segoe UI Semibold", 10f);
    public static readonly Font Section = new("Segoe UI Semibold", 12f);
    public static readonly Font Title = new("Segoe UI Semibold", 20f);

    public static GraphicsPath Round(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        if (diameter <= 0) return path;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class SurfacePanel : Panel
{
    [DefaultValue(false)] public bool FocusAccent { get; set; }
    public SurfacePanel()
    {
        DoubleBuffered = true;
        BackColor = SettingsTheme.Surface;
        Padding = new Padding(20);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SettingsTheme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = DeviceDpi / 96f;
        using var path = SettingsTheme.Round(new RectangleF(.5f, .5f, Width - 1, Height - 1), 12 * scale);
        using var fill = new SolidBrush(BackColor);
        using var border = new Pen(FocusAccent ? SettingsTheme.Accent : SettingsTheme.Border, FocusAccent ? 2 : 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }
}

internal enum SettingsGlyph { None, Gesture, Motion, Application, Help, External, Play }

internal sealed class SettingsButton : Button
{
    private bool _hovered;
    private bool _pressed;
    [DefaultValue(false)] public bool Primary { get; set; }
    [DefaultValue(false)] public bool Navigation { get; set; }
    [DefaultValue(false)] public bool Selected { get; set; }
    [DefaultValue(SettingsGlyph.None)] public SettingsGlyph Glyph { get; set; }

    public SettingsButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = SettingsTheme.Strong;
        Cursor = Cursors.Hand;
        Height = 44;
        MinimumSize = new Size(44, 44);
        UseVisualStyleBackColor = false;
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override AccessibleObject CreateAccessibilityInstance() => Navigation
        ? new NavigationAccessibleObject(this) : base.CreateAccessibilityInstance();

    private sealed class NavigationAccessibleObject(SettingsButton owner) : ControlAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.PageTab;
        public override AccessibleStates State => base.State | (owner.Selected ? AccessibleStates.Selected : AccessibleStates.None);
        public override string DefaultAction => "Chọn tab";
        public override void DoDefaultAction() => owner.PerformClick();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var scale = DeviceDpi / 96f;
        var background = Primary ? (_hovered || _pressed ? SettingsTheme.AccentHover : SettingsTheme.Accent)
            : Selected ? SettingsTheme.Selection : _hovered ? SettingsTheme.Field : Navigation ? SettingsTheme.Sidebar : SettingsTheme.Surface;
        var foreground = !Enabled ? SettingsTheme.Muted : Primary ? SettingsTheme.Surface : Selected ? SettingsTheme.Accent : SettingsTheme.Ink;
        e.Graphics.Clear(Parent?.BackColor ?? SettingsTheme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var offset = _pressed ? scale : 0;
        using var path = SettingsTheme.Round(new RectangleF(.5f, .5f + offset, Width - 1, Height - 1 - offset), 10 * scale);
        using var brush = new SolidBrush(Enabled ? background : SettingsTheme.Sidebar);
        e.Graphics.FillPath(brush, path);
        if (!Navigation && !Primary)
        {
            using var border = new Pen(SettingsTheme.Border);
            e.Graphics.DrawPath(border, path);
        }
        var text = Rectangle.Inflate(ClientRectangle, -(int)(16 * scale), 0);
        if (Glyph != SettingsGlyph.None)
        {
            DrawGlyph(e.Graphics, new PointF(16 * scale, (Height - 20 * scale) / 2 + offset), scale, foreground, Glyph);
            text.X += (int)(32 * scale);
            text.Width -= (int)(32 * scale);
        }
        text.Y += (int)offset;
        TextRenderer.DrawText(e.Graphics, Text, Font, text, foreground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
            (Navigation || Glyph != SettingsGlyph.None ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter));
        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(ClientRectangle, -(int)(4 * scale), -(int)(4 * scale));
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, foreground, background);
        }
    }

    private static void DrawGlyph(Graphics graphics, PointF origin, float scale, Color color, SettingsGlyph glyph)
    {
        var saved = graphics.Save();
        graphics.TranslateTransform(origin.X, origin.Y);
        graphics.ScaleTransform(scale, scale);
        using var pen = new Pen(color, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        switch (glyph)
        {
            case SettingsGlyph.Gesture:
                graphics.DrawLine(pen, 3, 6, 17, 6); graphics.DrawLines(pen, [new PointF(6, 3), new(3, 6), new(6, 9)]);
                graphics.DrawLine(pen, 3, 14, 17, 14); graphics.DrawLines(pen, [new PointF(14, 11), new(17, 14), new(14, 17)]); break;
            case SettingsGlyph.Motion:
                using (var round = SettingsTheme.Round(new RectangleF(5, 4, 12, 12), 3)) graphics.DrawPath(pen, round);
                graphics.DrawLine(pen, 1, 7, 3, 7); graphics.DrawLine(pen, 0, 11, 3, 11); graphics.DrawLine(pen, 2, 15, 3, 15); break;
            case SettingsGlyph.Application:
                using (var round = SettingsTheme.Round(new RectangleF(2, 3, 16, 14), 3)) graphics.DrawPath(pen, round);
                graphics.DrawLine(pen, 2, 7, 18, 7); graphics.DrawLine(pen, 6, 3, 6, 7); break;
            case SettingsGlyph.Help:
                graphics.DrawEllipse(pen, 2, 2, 16, 16); graphics.DrawArc(pen, 7, 5, 6, 6, 195, 230);
                graphics.DrawLine(pen, 10, 10, 10, 12); graphics.DrawEllipse(pen, 9.6f, 14.5f, .8f, .8f); break;
            case SettingsGlyph.External:
                graphics.DrawLines(pen, [new PointF(8, 3), new(17, 3), new(17, 12)]); graphics.DrawLine(pen, 17, 3, 7, 13);
                graphics.DrawLines(pen, [new PointF(4, 6), new(2, 6), new(2, 18), new(14, 18), new(14, 16)]); break;
            case SettingsGlyph.Play:
                graphics.DrawPolygon(pen, [new PointF(5, 3), new(16, 10), new(5, 17)]); break;
        }
        graphics.Restore(saved);
    }
}

internal sealed class SettingsToggle : CheckBox
{
    public SettingsToggle()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(48, 32);
        AccessibleRole = AccessibleRole.CheckButton;
        Cursor = Cursors.Hand;
    }
    protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }
    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? SettingsTheme.Surface);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var scale = DeviceDpi / 96f;
        var track = new RectangleF(2 * scale, (Height - 24 * scale) / 2, 44 * scale, 24 * scale);
        using var path = SettingsTheme.Round(track, 12 * scale);
        using var fill = new SolidBrush(Checked ? SettingsTheme.Accent : SettingsTheme.Muted);
        e.Graphics.FillPath(fill, path);
        using var knob = new SolidBrush(SettingsTheme.Surface);
        e.Graphics.FillEllipse(knob, track.X + (Checked ? 23 : 3) * scale, track.Y + 3 * scale, 18 * scale, 18 * scale);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -1, -1));
    }
}
