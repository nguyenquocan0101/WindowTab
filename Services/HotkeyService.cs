using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowTab.Models;
using WindowTab.Platform;

namespace WindowTab.Services;

internal enum GestureDirection
{
    Left,
    Right
}

internal sealed class HotkeyService : NativeWindow, IDisposable
{
    private const int LeftHotkeyId = 0x4111;
    private const int RightHotkeyId = 0x4112;
    private readonly Action<GestureDirection> _onGesture;
    private HotkeyBinding? _left;
    private HotkeyBinding? _right;
    private bool _leftRegistered;
    private bool _rightRegistered;
    private bool _disposed;
    private int _lastRegisterError;

    public HotkeyService(Action<GestureDirection> onGesture)
    {
        _onGesture = onGesture;
        CreateHandle(new CreateParams { Caption = "WindowTab.GlobalHotkeys" });
    }

    public bool TryConfigure(HotkeyBinding left, HotkeyBinding right, out string error)
    {
        if (!left.IsValid || !right.IsValid)
        {
            error = "Mỗi phím tắt cần có một phím chính và ít nhất một phím bổ trợ.";
            return false;
        }

        if (BindingsEqual(left, right))
        {
            error = "Hai hướng vuốt phải dùng hai phím tắt khác nhau.";
            return false;
        }

        var oldLeft = _left?.Clone();
        var oldRight = _right?.Clone();
        UnregisterCurrent();

        if (RegisterPair(left, right))
        {
            _left = left.Clone();
            _right = right.Clone();
            error = string.Empty;
            return true;
        }

        var failedError = new Win32Exception(_lastRegisterError).Message;
        UnregisterCurrent();
        if (oldLeft is not null && oldRight is not null && RegisterPair(oldLeft, oldRight))
        {
            _left = oldLeft;
            _right = oldRight;
        }
        else
        {
            _left = null;
            _right = null;
            UnregisterCurrent();
        }

        error = $"Không đăng ký được phím tắt toàn hệ thống. Có thể phím đã được ứng dụng khác dùng. Chi tiết: {failedError}";
        return false;
    }

    public bool IsReady => _leftRegistered && _rightRegistered;

    public void Suspend() => UnregisterCurrent();

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey)
        {
            if (m.WParam.ToInt32() == LeftHotkeyId)
            {
                _onGesture(GestureDirection.Left);
                return;
            }

            if (m.WParam.ToInt32() == RightHotkeyId)
            {
                _onGesture(GestureDirection.Right);
                return;
            }
        }

        base.WndProc(ref m);
    }

    private bool RegisterPair(HotkeyBinding left, HotkeyBinding right)
    {
        _leftRegistered = RegisterOne(LeftHotkeyId, left);
        if (!_leftRegistered)
        {
            return false;
        }

        _rightRegistered = RegisterOne(RightHotkeyId, right);
        if (_rightRegistered)
        {
            return true;
        }

        NativeMethods.UnregisterHotKey(Handle, LeftHotkeyId);
        _leftRegistered = false;
        return false;
    }

    private bool RegisterOne(int id, HotkeyBinding binding)
    {
        var modifiers = (uint)binding.Modifiers | NativeMethods.ModNoRepeat;
        var registered = NativeMethods.RegisterHotKey(Handle, id, modifiers, (uint)binding.Key);
        if (!registered) _lastRegisterError = Marshal.GetLastWin32Error();
        return registered;
    }

    private void UnregisterCurrent()
    {
        if (_leftRegistered) NativeMethods.UnregisterHotKey(Handle, LeftHotkeyId);
        if (_rightRegistered) NativeMethods.UnregisterHotKey(Handle, RightHotkeyId);
        _leftRegistered = false;
        _rightRegistered = false;
    }

    private static bool BindingsEqual(HotkeyBinding first, HotkeyBinding second) =>
        first.Key == second.Key && first.Modifiers == second.Modifiers;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterCurrent();
        DestroyHandle();
    }
}
