using Microsoft.Win32;
using WindowTab.Models;
using WindowTab.Services;
using WindowTab.Views;

namespace WindowTab;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore = new();
    private readonly ForegroundWindowTracker _tracker;
    private readonly SlideOverlay _overlay = new();
    private readonly WindowCycler _cycler;
    private readonly HotkeyService _hotkeys;
    private readonly NotifyIcon _notifyIcon;
    private AppSettings _settings;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext(bool showSettings = false)
    {
        _settings = _settingsStore.Load();
        _tracker = new ForegroundWindowTracker();
        _cycler = new WindowCycler(_tracker, _overlay, _settings);
        _hotkeys = new HotkeyService(_cycler.HandleGesture);

        var registered = _hotkeys.TryConfigure(_settings.SwipeLeft, _settings.SwipeRight, out var hotkeyError);
        DiagnosticLog.Write($"Application started: hotkeysRegistered={registered}, animationMs={_settings.AnimationMs}, cooldownMs={_settings.CooldownMs}, includeDesktop={_settings.IncludeDesktop}, animation=shrink-slide-zoom, clock=high-resolution-60Hz.");
        if (!registered)
        {
            _settings.SwipeLeft = HotkeyBinding.CreateDefaultLeft();
            _settings.SwipeRight = HotkeyBinding.CreateDefaultRight();
            if (_hotkeys.TryConfigure(_settings.SwipeLeft, _settings.SwipeRight, out _))
            {
                try { _settingsStore.Save(_settings); } catch { }
            }
        }

        var menu = new ContextMenuStrip();
        var openSettings = new ToolStripMenuItem("Cài đặt…");
        openSettings.Click += (_, _) => OpenSettings();
        var pause = new ToolStripMenuItem("Tạm dừng gesture") { CheckOnClick = true };
        pause.CheckedChanged += (_, _) =>
        {
            _cycler.IsPaused = pause.Checked;
            UpdateTrayText();
        };
        var exit = new ToolStripMenuItem("Thoát");
        exit.Click += (_, _) => ExitThread();
        menu.Items.Add(openSettings);
        menu.Items.Add(pause);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        _notifyIcon = new NotifyIcon
        {
            Icon = BrandAssets.AppIcon,
            Text = "WindowTab",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => OpenSettings();
        UpdateTrayText();

        if (!registered)
        {
            DiagnosticLog.Write($"Hotkey registration failed: {hotkeyError}");
            _notifyIcon.ShowBalloonTip(5000, "WindowTab", hotkeyError, ToolTipIcon.Warning);
        }
        else if (!_tracker.IsTrackingAvailable)
        {
            _notifyIcon.ShowBalloonTip(5000, "WindowTab", "Theo dõi thứ tự cửa sổ không khả dụng; dùng thứ tự z-order khi chuyển.", ToolTipIcon.Info);
        }

        if (_settings.StartWithWindows)
        {
            try { StartupManager.Apply(true); } catch { }
        }

        if (showSettings)
        {
            _ = _overlay.Handle;
            _overlay.BeginInvoke((Action)OpenSettings);
        }
    }

    private void OpenSettings()
    {
        if (_settingsForm is { IsDisposed: false }) { _settingsForm.Activate(); return; }
        var wasPaused = _cycler.IsPaused;
        var hotkeysReady = _hotkeys.IsReady;
        _cycler.IsPaused = true;
        _hotkeys.Suspend();
        DiagnosticLog.Write("Settings opened: gesture hotkeys suspended.");
        try
        {
            using var form = new SettingsForm(_settings, hotkeysReady, _tracker.IsTrackingAvailable, updated =>
            {
                var error = SaveSettings(updated);
                if (error is not null) _hotkeys.Suspend();
                return error;
            });
            _settingsForm = form;
            form.ShowDialog();
        }
        finally
        {
            _settingsForm = null;
            var registered = _hotkeys.TryConfigure(_settings.SwipeLeft, _settings.SwipeRight, out var error);
            if (!registered)
                DiagnosticLog.Write($"Hotkey registration after settings failed: {error}");
            _cycler.IsPaused = wasPaused;
            DiagnosticLog.Write($"Settings closed: hotkeysRegistered={registered}, paused={wasPaused}.");
            UpdateTrayText();
        }
    }

    private string? SaveSettings(AppSettings updated)
    {
        var previous = _settings.Clone();
        if (!_hotkeys.TryConfigure(updated.SwipeLeft, updated.SwipeRight, out var hotkeyError))
        {
            return hotkeyError;
        }

        try
        {
            StartupManager.Apply(updated.StartWithWindows);
            _settingsStore.Save(updated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            try { StartupManager.Apply(previous.StartWithWindows); } catch { }
            _hotkeys.TryConfigure(previous.SwipeLeft, previous.SwipeRight, out _);
            return $"Không ghi được cài đặt: {ex.Message}";
        }

        _settings = updated.Clone();
        _cycler.UpdateSettings(_settings);
        UpdateTrayText();
        return null;
    }

    private void UpdateTrayText()
    {
        var state = _cycler.IsPaused ? "tạm dừng" : _hotkeys.IsReady ? "đang chạy" : "thiếu phím tắt";
        _notifyIcon.Text = $"WindowTab — {state}";
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _hotkeys.Dispose();
        _tracker.Dispose();
        _cycler.Dispose();
        base.ExitThreadCore();
    }
}
