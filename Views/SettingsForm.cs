using WindowTab.Models;
using WindowTab.Services;

namespace WindowTab.Views;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _leftShortcut = new();
    private readonly TextBox _rightShortcut = new();
    private readonly SettingsNumber _cooldown = new();
    private readonly SettingsNumber _animation = new();
    private readonly SettingsToggle _startWithWindows = new();
    private readonly SettingsToggle _includeDesktop = new();
    private readonly Label _message = new();
    private readonly Label _heading = new();
    private readonly Label _description = new();
    private readonly Panel _pageHost = new();
    private readonly List<Panel> _pages = [];
    private readonly List<SettingsButton> _navigation = [];
    private readonly MotionPreview _preview = new();
    private readonly Func<AppSettings, string?> _save;
    private readonly AppSettings _initial;
    private readonly bool _hotkeysReady;
    private readonly bool _trackingReady;
    private readonly SettingsButton _saveButton = new();
    private readonly ToolTip _toolTip = new() { AutoPopDelay = 10000 };
    private bool _initializing = true;
    private int _selectedTab;

    private static readonly string[] Titles = ["Cử chỉ", "Chuyển động", "Ứng dụng", "Hướng dẫn"];
    private static readonly string[] Descriptions =
    [
        "Kết nối thao tác vuốt với hai phím tắt riêng biệt.",
        "Điều chỉnh nhịp chuyển cửa sổ theo cách bạn thích.",
        "Chọn cách WindowTab hoạt động cùng Windows.",
        "Thiết lập một lần, sau đó chuyển cửa sổ bằng touchpad."
    ];

    public SettingsForm(AppSettings settings, bool hotkeysReady, bool trackingReady, Func<AppSettings, string?> save)
    {
        _initial = settings.Clone();
        _save = save;
        _hotkeysReady = hotkeysReady;
        _trackingReady = trackingReady;
        Text = "WindowTab · Cài đặt";
        Icon = BrandAssets.AppIcon;
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = SettingsTheme.Body;
        ForeColor = SettingsTheme.Ink;
        BackColor = SettingsTheme.Background;
        ClientSize = new Size(960, 704);
        MinimumSize = new Size(880, 640);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        Controls.Add(root);
        var rail = BuildNavigation();
        root.Controls.Add(rail, 0, 0);
        root.SetRowSpan(rail, 2);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Padding = new Padding(32, 24, 24, 0), Margin = Padding.Empty
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(main, 1, 0);
        var header = Stack();
        _heading.Font = SettingsTheme.Title;
        _heading.AutoSize = true;
        _heading.Margin = new Padding(0, 0, 0, 4);
        _description.Font = SettingsTheme.Body;
        _description.ForeColor = SettingsTheme.Muted;
        _description.AutoSize = true;
        _description.Margin = Padding.Empty;
        header.Controls.Add(_heading);
        header.Controls.Add(_description);
        main.Controls.Add(header, 0, 0);
        _pageHost.Dock = DockStyle.Fill;
        _pageHost.Margin = Padding.Empty;
        main.Controls.Add(_pageHost, 0, 1);

        _pages.Add(BuildGesturePage());
        _pages.Add(BuildMotionPage());
        _pages.Add(BuildApplicationPage());
        _pages.Add(BuildHelpPage());
        foreach (var page in _pages)
        {
            if (page.Controls[0] is TableLayoutPanel body && body.Controls.Count > 0)
                body.Controls[body.Controls.Count - 1].Margin = Padding.Empty;
            page.Visible = false;
            _pageHost.Controls.Add(page);
        }
        root.Controls.Add(BuildFooter(), 1, 1);
        ConfigureCapture(_leftShortcut, _initial.SwipeLeft);
        ConfigureCapture(_rightShortcut, _initial.SwipeRight);
        _startWithWindows.Checked = _initial.StartWithWindows;
        _includeDesktop.Checked = _initial.IncludeDesktop;
        _startWithWindows.CheckedChanged += (_, _) => MarkChanged();
        _includeDesktop.CheckedChanged += (_, _) => MarkChanged();
        _animation.ValueChanged += (_, _) => MarkChanged();
        _cooldown.ValueChanged += (_, _) => MarkChanged();
        _initializing = false;
        SelectTab(0);
        ShowMessage("Thay đổi được áp dụng khi bạn lưu.");
        _saveButton.Enabled = false;
    }

    private Control BuildNavigation()
    {
        var rail = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = SettingsTheme.Sidebar, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(20, 24, 20, 24), Margin = Padding.Empty,
            AccessibleName = "Các tab cài đặt", AccessibleRole = AccessibleRole.PageTabList
        };
        rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        rail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        var brand = Stack();
        brand.BackColor = SettingsTheme.Sidebar;
        brand.Controls.Add(new PictureBox { Image = BrandAssets.Logo, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(48, 48), Margin = new Padding(0, 0, 0, 8), TabStop = false });
        brand.Controls.Add(TextLabel("WindowTab", SettingsTheme.Section, SettingsTheme.Ink, 0));
        rail.Controls.Add(brand, 0, 0);
        var nav = Stack();
        nav.BackColor = SettingsTheme.Sidebar;
        var glyphs = new[] { SettingsGlyph.Gesture, SettingsGlyph.Motion, SettingsGlyph.Application, SettingsGlyph.Help };
        for (var i = 0; i < Titles.Length; i++)
        {
            var index = i;
            var button = new SettingsButton
            {
                Text = Titles[i], Glyph = glyphs[i], Navigation = true,
                Dock = DockStyle.Top, Height = 48, Margin = new Padding(0, 0, 0, 8),
                AccessibleRole = AccessibleRole.PageTab, AccessibleName = Titles[i], TabIndex = i
            };
            button.Click += (_, _) => SelectTab(index);
            button.KeyDown += (_, e) =>
            {
                if (e.KeyCode is not (Keys.Up or Keys.Down)) return;
                var next = (_selectedTab + (e.KeyCode == Keys.Down ? 1 : Titles.Length - 1)) % Titles.Length;
                SelectTab(next); _navigation[next].Focus(); e.Handled = true;
            };
            _navigation.Add(button);
            nav.Controls.Add(button);
        }
        rail.Controls.Add(nav, 0, 1);
        rail.Controls.Add(TextLabel("Cài đặt riêng cho\ntài khoản Windows này", SettingsTheme.Small, SettingsTheme.Muted, 0), 0, 2);
        return rail;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            Padding = new Padding(32, 20, 32, 20), Margin = Padding.Empty
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 156));
        _message.Font = SettingsTheme.Small;
        _message.Dock = DockStyle.Fill;
        _message.TextAlign = ContentAlignment.MiddleLeft;
        _message.Margin = new Padding(0, 0, 16, 0);
        _message.AccessibleRole = AccessibleRole.StatusBar;
        _message.AutoEllipsis = true;
        footer.Controls.Add(_message, 0, 0);
        var cancel = new SettingsButton { Text = "Hủy", DialogResult = DialogResult.Cancel, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };
        _saveButton.Text = "Lưu thay đổi";
        _saveButton.Primary = true;
        _saveButton.Dock = DockStyle.Fill;
        _saveButton.Margin = Padding.Empty;
        _saveButton.Click += SaveSettings;
        footer.Controls.Add(cancel, 1, 0);
        footer.Controls.Add(_saveButton, 2, 0);
        AcceptButton = _saveButton;
        CancelButton = cancel;
        return footer;
    }

    private Panel BuildGesturePage()
    {
        var page = Page(out var content);
        var card = Card(content, "Phím tắt vuốt ba ngón", "Bấm vào một ô rồi nhấn tổ hợp phím mới. Hai hướng cần dùng hai tổ hợp khác nhau.");
        var columns = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 8, 0, 16) };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        columns.Controls.Add(ShortcutInput("Vuốt sang trái", _leftShortcut, 12), 0, 0);
        columns.Controls.Add(ShortcutInput("Vuốt sang phải", _rightShortcut, 0), 1, 0);
        card.Controls.Add(columns);
        card.Controls.Add(TextLabel("Phím tắt được tạm dừng khi cửa sổ Cài đặt đang mở để bạn ghi lại tổ hợp dễ dàng.", SettingsTheme.Small, SettingsTheme.Muted));
        var setup = Card(content, "Kết nối với touchpad", "Gán hai tổ hợp trên cho Swipe left và Swipe right trong Advanced gestures của Windows.");
        var open = new SettingsButton { Text = "Mở cài đặt Touchpad", Glyph = SettingsGlyph.External, Width = 244, Margin = new Padding(0, 8, 0, 0) };
        open.Click += (_, _) => OpenTouchpadSettings();
        setup.Controls.Add(open);
        return page;
    }

    private Control ShortcutInput(string title, TextBox field, int gap)
    {
        var stack = Stack();
        stack.Margin = new Padding(0, 0, gap, 0);
        stack.Controls.Add(TextLabel(title, SettingsTheme.Strong, SettingsTheme.Ink));
        var border = new SurfacePanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(14, 14, 14, 12), BackColor = SettingsTheme.Field, Margin = Padding.Empty };
        field.BorderStyle = BorderStyle.None;
        field.Font = SettingsTheme.Strong;
        field.BackColor = SettingsTheme.Field;
        field.ForeColor = SettingsTheme.Ink;
        field.Dock = DockStyle.Top;
        field.AccessibleName = "Phím tắt " + title.ToLowerInvariant();
        field.Enter += (_, _) => { border.FocusAccent = true; border.Invalidate(); };
        field.Leave += (_, _) => { border.FocusAccent = false; border.Invalidate(); };
        border.Controls.Add(field);
        border.Click += (_, _) => field.Focus();
        stack.Controls.Add(border);
        return stack;
    }

    private Panel BuildMotionPage()
    {
        var page = Page(out var content);
        var play = new SettingsButton { Text = "Xem thử", Glyph = SettingsGlyph.Play, Width = 144, Margin = Padding.Empty };
        play.Click += (_, _) => _preview.Play((int)_animation.Value);
        var previewCard = Card(content, "Thu nhỏ · Trượt · Phóng lớn", null, play);
        _preview.Dock = DockStyle.Top;
        _preview.Height = 128;
        _preview.Margin = new Padding(0, 8, 0, 0);
        previewCard.Controls.Add(_preview);
        var timing = Card(content, "Nhịp chuyển cửa sổ", "Tăng thời lượng để chuyển cảnh chậm hơn. Chống lặp giúp lọc các lần vuốt quá sát nhau.");
        timing.Controls.Add(TimingInput("Thời lượng chuyển cảnh", _animation, _initial.AnimationMs, 100, 600, 20));
        timing.Controls.Add(TimingInput("Chống lặp nhanh", _cooldown, _initial.CooldownMs, 0, 1000, 50));
        return page;
    }

    private Control TimingInput(string title, NumericUpDown number, int value, int min, int max, int step)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 2, Margin = new Padding(0, 8, 0, 0) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        var label = TextLabel(title, SettingsTheme.Strong, SettingsTheme.Ink, 0);
        label.Dock = DockStyle.Fill; label.TextAlign = ContentAlignment.MiddleLeft;
        row.Controls.Add(label, 0, 0);
        number.Minimum = min; number.Maximum = max; number.Increment = step; number.Value = Math.Clamp(value, min, max);
        number.Font = SettingsTheme.Strong; number.BorderStyle = BorderStyle.None;
        number.BackColor = SettingsTheme.Field; number.ForeColor = SettingsTheme.Ink;
        number.Dock = DockStyle.None; number.Anchor = AnchorStyles.Left | AnchorStyles.Right; number.Margin = Padding.Empty;
        number.AccessibleName = title + " (mili giây)";
        var field = new SurfacePanel { Height = 40, Dock = DockStyle.Top, BackColor = SettingsTheme.Field, Padding = new Padding(12, 8, 12, 8), Margin = Padding.Empty };
        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = SettingsTheme.Field };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var unit = TextLabel("ms", SettingsTheme.Small, SettingsTheme.Muted, 0);
        unit.Dock = DockStyle.Fill; unit.TextAlign = ContentAlignment.MiddleRight;
        editor.Controls.Add(number, 0, 0); editor.Controls.Add(unit, 1, 0); field.Controls.Add(editor);
        number.Enter += (_, _) => { field.FocusAccent = true; field.Invalidate(); };
        number.Leave += (_, _) => { field.FocusAccent = false; field.Invalidate(); };
        field.Click += (_, _) => number.Focus(); unit.Click += (_, _) => number.Focus();
        row.Controls.Add(field, 1, 0);
        var slider = new SettingsSlider { Minimum = min, Maximum = max, Value = (int)number.Value, SmallChange = step, LargeChange = step * 2,
            Dock = DockStyle.Top, Margin = Padding.Empty, AccessibleName = title };
        slider.ValueChanged += (_, _) => { if (number.Value != slider.Value) number.Value = slider.Value; };
        number.ValueChanged += (_, _) => { if (slider.Value != (int)number.Value) slider.Value = (int)number.Value; };
        row.Controls.Add(slider, 0, 1);
        row.SetColumnSpan(slider, 2);
        return row;
    }

    private Panel BuildApplicationPage()
    {
        var page = Page(out var content);
        var behavior = Card(content, "Hoạt động", "Các tùy chọn được lưu riêng trên máy của bạn.");
        behavior.Controls.Add(ToggleRow("Khởi động cùng Windows", "Tự chạy trong khay hệ thống khi bạn đăng nhập.", _startWithWindows));
        behavior.Controls.Add(Separator());
        behavior.Controls.Add(ToggleRow("Cho phép chuyển tới Desktop", "Thêm Desktop ở cuối danh sách. Khi tắt, bạn dừng ở cửa sổ ứng dụng cuối.", _includeDesktop));
        var edge = Card(content, "Khi chạm đầu hoặc cuối danh sách", "Cửa sổ di chuyển khoảng 20% rồi bật trở lại để báo đã tới biên. Danh sách không quay vòng.");
        edge.Controls.Add(TextLabel("Taskbar luôn nằm ngoài vùng chuyển cảnh.", SettingsTheme.Small, SettingsTheme.Muted, 0));
        var status = Card(content, "Trạng thái kết nối", null);
        status.Controls.Add(TextLabel(_hotkeysReady ? "Phím tắt: đã kết nối khi app chạy" : "Phím tắt: cần chọn lại tổ hợp khả dụng",
            SettingsTheme.Body, _hotkeysReady ? SettingsTheme.Success : SettingsTheme.Error));
        status.Controls.Add(TextLabel(_trackingReady ? "Theo dõi thứ tự cửa sổ: sẵn sàng" : "Theo dõi cửa sổ: đang dùng thứ tự dự phòng",
            SettingsTheme.Body, _trackingReady ? SettingsTheme.Muted : SettingsTheme.Error, 0));
        return page;
    }

    private static Control ToggleRow(string title, string description, SettingsToggle toggle)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 12, 0, 12) };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        var text = Stack(); text.Margin = new Padding(0, 0, 16, 0);
        text.Controls.Add(TextLabel(title, SettingsTheme.Strong, SettingsTheme.Ink));
        text.Controls.Add(TextLabel(description, SettingsTheme.Small, SettingsTheme.Muted, 0));
        toggle.AccessibleName = title;
        toggle.AccessibleDescription = description;
        toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        row.Controls.Add(text, 0, 0); row.Controls.Add(toggle, 1, 0);
        return row;
    }

    private Panel BuildHelpPage()
    {
        var page = Page(out var content);
        var setup = Card(content, "Kết nối cử chỉ với Windows", "Bạn chỉ cần thiết lập lại nếu đổi phím tắt hoặc thay touchpad.");
        setup.Controls.Add(TextLabel("Mở Bluetooth & devices → Touchpad → Advanced gestures.", SettingsTheme.Body, SettingsTheme.Ink, 16));
        setup.Controls.Add(TextLabel("Chọn Custom shortcut cho Swipe left và Swipe right, sau đó ghi đúng hai tổ hợp ở tab Cử chỉ.", SettingsTheme.Body, SettingsTheme.Ink, 16));
        setup.Controls.Add(TextLabel("Lưu cài đặt WindowTab, đóng cửa sổ này rồi thử vuốt ba ngón.", SettingsTheme.Body, SettingsTheme.Ink, 16));
        var open = new SettingsButton { Text = "Mở cài đặt Touchpad", Glyph = SettingsGlyph.External, Width = 244, Margin = new Padding(0, 8, 0, 0) };
        open.Click += (_, _) => OpenTouchpadSettings(); setup.Controls.Add(open);
        var notes = Card(content, "Một vài điều cần biết", null);
        notes.Controls.Add(TextLabel("Windows và driver touchpad quyết định khoảng cách vuốt. Chống lặp chỉ lọc những lần kích hoạt quá gần nhau.", SettingsTheme.Small, SettingsTheme.Muted, 12));
        notes.Controls.Add(TextLabel("Một số cửa sổ được bảo vệ có thể không cho xem trước hoặc chuyển focus. WindowTab sẽ trả bạn về cửa sổ đang dùng nếu không chuyển được.", SettingsTheme.Small, SettingsTheme.Muted, 0));
        return page;
    }

    private static Panel Page(out TableLayoutPanel content)
    {
        var page = new Panel { Dock = DockStyle.Fill, AutoScroll = true, AccessibleRole = AccessibleRole.PropertyPage, Margin = Padding.Empty };
        content = Stack(); content.Padding = new Padding(0, 0, 8, 0);
        page.Controls.Add(content);
        return page;
    }

    private static TableLayoutPanel Card(TableLayoutPanel parent, string title, string? description, SettingsButton? action = null)
    {
        var card = new SurfacePanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, 16) };
        var body = Stack(); body.BackColor = SettingsTheme.Surface;
        if (action is null) body.Controls.Add(TextLabel(title, SettingsTheme.Section, SettingsTheme.Ink, description is null ? 0 : 8));
        else
        {
            var header = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2, Margin = Padding.Empty };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144));
            var caption = TextLabel(title, SettingsTheme.Section, SettingsTheme.Ink, 0);
            caption.Dock = DockStyle.Fill; caption.TextAlign = ContentAlignment.MiddleLeft;
            header.Controls.Add(caption, 0, 0); header.Controls.Add(action, 1, 0);
            body.Controls.Add(header);
        }
        if (description is not null) body.Controls.Add(TextLabel(description, SettingsTheme.Small, SettingsTheme.Muted, 8));
        card.Controls.Add(body); parent.Controls.Add(card);
        return body;
    }

    private static TableLayoutPanel Stack() => new()
    {
        Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 1, Margin = Padding.Empty, Padding = Padding.Empty,
        ColumnStyles = { new ColumnStyle(SizeType.Percent, 100) }
    };

    private static Label TextLabel(string text, Font font, Color color, int bottom = 8)
    {
        var label = new Label { Text = text, AutoSize = true, Dock = DockStyle.Top, Font = font,
            ForeColor = color, Margin = new Padding(0, 0, 0, bottom), UseMnemonic = false };
        label.ParentChanged += (_, _) =>
        {
            if (label.Parent is not { } parent) return;
            void Wrap() { if (!label.IsDisposed) label.MaximumSize = new Size(Math.Max(80, parent.ClientSize.Width - parent.Padding.Horizontal - label.Margin.Horizontal), 0); }
            parent.SizeChanged += (_, _) => Wrap();
            Wrap();
        };
        return label;
    }

    private static Control Separator() => new Panel { Height = 1, Dock = DockStyle.Top, BackColor = SettingsTheme.Border, Margin = new Padding(0, 4, 0, 4) };

    private void SelectTab(int index)
    {
        if (_pages.Count != Titles.Length) return;
        _selectedTab = index;
        for (var i = 0; i < _pages.Count; i++)
        {
            _pages[i].Visible = i == index;
            _navigation[i].Selected = i == index;
            _navigation[i].Invalidate();
        }
        _heading.Text = Titles[index]; _description.Text = Descriptions[index];
        if (index != 1) _preview.Stop();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is (Keys.Control | Keys.Tab) or (Keys.Control | Keys.Shift | Keys.Tab))
        {
            SelectTab((_selectedTab + ((keyData & Keys.Shift) != 0 ? Titles.Length - 1 : 1)) % Titles.Length);
            _navigation[_selectedTab].Focus(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ConfigureCapture(TextBox field, HotkeyBinding initial)
    {
        field.ReadOnly = true; field.ShortcutsEnabled = false;
        field.Tag = initial.Clone(); field.Text = initial.ToDisplayString();
        field.KeyDown += (_, e) =>
        {
            // Tab stays available for keyboard navigation; Escape and Enter keep normal dialog behavior.
            if (e.KeyCode is Keys.Tab or Keys.Escape or Keys.Enter) return;
            e.Handled = true; e.SuppressKeyPress = true;
            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin) return;
            var modifiers = HotkeyModifiers.None;
            if (e.Control) modifiers |= HotkeyModifiers.Control;
            if (e.Alt) modifiers |= HotkeyModifiers.Alt;
            if (e.Shift) modifiers |= HotkeyModifiers.Shift;
            var binding = new HotkeyBinding { Key = e.KeyCode, Modifiers = modifiers };
            if (!binding.IsValid) { ShowMessage("Dùng Ctrl, Alt hoặc Shift cùng một phím chính.", true); return; }
            field.Tag = binding; field.Text = binding.ToDisplayString(); MarkChanged();
        };
    }

    private void MarkChanged()
    {
        if (_initializing) return;
        var left = (HotkeyBinding)_leftShortcut.Tag!;
        var right = (HotkeyBinding)_rightShortcut.Tag!;
        var changed = left.Key != _initial.SwipeLeft.Key || left.Modifiers != _initial.SwipeLeft.Modifiers ||
            right.Key != _initial.SwipeRight.Key || right.Modifiers != _initial.SwipeRight.Modifiers ||
            _animation.Value != _initial.AnimationMs || _cooldown.Value != _initial.CooldownMs ||
            _startWithWindows.Checked != _initial.StartWithWindows || _includeDesktop.Checked != _initial.IncludeDesktop;
        _saveButton.Enabled = changed;
        ShowMessage(changed ? "Có thay đổi chưa lưu." : "Thay đổi được áp dụng khi bạn lưu.");
    }

    private void ShowMessage(string text, bool error = false)
    {
        _message.Text = text;
        _message.ForeColor = error ? SettingsTheme.Error : SettingsTheme.Muted;
        _message.AccessibleDescription = text;
        _toolTip.SetToolTip(_message, error ? text : null);
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        var left = (HotkeyBinding)_leftShortcut.Tag!;
        var right = (HotkeyBinding)_rightShortcut.Tag!;
        if (left.Key == right.Key && left.Modifiers == right.Modifiers)
        {
            SelectTab(0); ShowMessage("Hai hướng vuốt cần hai phím tắt khác nhau.", true); _rightShortcut.Focus(); return;
        }
        var draft = new AppSettings
        {
            SwipeLeft = left.Clone(), SwipeRight = right.Clone(), CooldownMs = (int)_cooldown.Value,
            AnimationMs = (int)_animation.Value, StartWithWindows = _startWithWindows.Checked, IncludeDesktop = _includeDesktop.Checked
        };
        var error = _save(draft);
        if (error is not null) { ShowMessage(error, true); return; }
        DialogResult = DialogResult.OK; Close();
    }

    private void OpenTouchpadSettings()
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:devices-touchpad") { UseShellExecute = true }); }
        catch (Exception) { ShowMessage("Không mở được Touchpad. Bạn có thể mở trực tiếp từ Cài đặt Windows.", true); }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _preview.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _toolTip.Dispose();
        base.Dispose(disposing);
    }
}
