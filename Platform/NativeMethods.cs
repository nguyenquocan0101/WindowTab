using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WindowTab.Platform;

internal static class NativeMethods
{
    internal const int WmHotkey = 0x0312;
    internal const uint ModAlt = 0x0001;
    internal const uint ModControl = 0x0002;
    internal const uint ModShift = 0x0004;
    internal const uint ModNoRepeat = 0x4000;

    internal const uint EventSystemForeground = 0x0003;
    internal const uint WineventOutOfContext = 0x0000;
    internal const uint WineventSkipOwnProcess = 0x0002;
    internal const int ObjIdWindow = 0;
    internal const int ChildIdSelf = 0;
    internal const uint GwOwner = 4;
    internal const uint DwmwaCloaked = 14;

    internal const uint DwmTnpRectDestination = 0x00000001;
    internal const uint DwmTnpOpacity = 0x00000004;
    internal const uint DwmTnpVisible = 0x00000008;

    internal const uint FlashwTray = 0x00000002;
    internal const uint FlashwTimerNoForeground = 0x0000000C;

    internal const int WsExToolWindow = 0x00000080;
    internal const int WsExNoActivate = 0x08000000;
    internal const int WsExTransparent = 0x00000020;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr module,
        WinEventDelegate callback,
        uint processId,
        uint threadId,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

    [DllImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr CreateWaitableTimerEx(IntPtr attributes, string? name, uint flags, uint access);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWaitableTimer(SafeWaitHandle timer, ref long dueTime, int period,
        IntPtr completionRoutine, IntPtr context, [MarshalAs(UnmanagedType.Bool)] bool resume);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmFlush();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FlashWindowEx(ref FlashWindowInfo info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetWindow(IntPtr hWnd, uint command);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsDelegate callback, IntPtr lParam);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmGetWindowAttribute(IntPtr hWnd, uint attribute, out int value, int valueSize);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmRegisterThumbnail(IntPtr destination, IntPtr source, out IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmUnregisterThumbnail(IntPtr thumbnail);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmUpdateThumbnailProperties(IntPtr thumbnail, ref DwmThumbnailProperties properties);

    [DllImport("dwmapi.dll")]
    internal static extern int DwmQueryThumbnailSourceSize(IntPtr thumbnail, out NativeSize size);

    [DllImport("user32.dll")]
    internal static extern uint GetWindowLong(IntPtr hWnd, int index);

    internal delegate void WinEventDelegate(
        IntPtr hook,
        uint eventType,
        IntPtr hWnd,
        int objectId,
        int childId,
        uint eventThread,
        uint eventTime);

    internal delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeSize
    {
        public int Width;
        public int Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DwmThumbnailProperties
    {
        public uint Flags;
        public NativeRect Destination;
        public NativeRect Source;
        public byte Opacity;
        [MarshalAs(UnmanagedType.Bool)] public bool Visible;
        [MarshalAs(UnmanagedType.Bool)] public bool SourceClientAreaOnly;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FlashWindowInfo
    {
        public uint Size;
        public IntPtr Window;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }
}
