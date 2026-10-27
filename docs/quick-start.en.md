# WindowTab · English quick start

**A little more flow between the windows you work in.**

WindowTab is a Windows 11 tray utility that turns mapped three-finger touchpad shortcuts into a shrink → slide → zoom transition between application windows. The taskbar stays stationary. Navigation stops at either end with a 20% rebound.

## Download

[Get the Windows x64 ZIP](https://github.com/nguyenquocan0101/WindowTab/releases/latest/download/WindowTab-win-x64.zip)

You need Windows 11 x64 and a touchpad driver that supports recording custom shortcuts for three-finger gestures. The release bundles its .NET runtime. The UI is currently Vietnamese.

## Set up

1. Extract the entire ZIP into a permanent folder. Run `Start-WindowTab.cmd`, or start `WindowTab.exe` and open **Cài đặt…** from its tray icon.
2. The **Cử chỉ** tab shows your shortcuts. A fresh installation defaults to **Ctrl + Alt + Shift + F8** for left and **Ctrl + Alt + Shift + F10** for right. Existing settings are preserved.
3. Keep WindowTab Settings open while recording shortcuts. Click **Mở cài đặt Touchpad** and go to **Bluetooth & devices → Touchpad → Advanced gestures** in Windows.
4. Set **Swipe left/right → Custom shortcut**. Use **Start recording**, press the corresponding chord, then **Stop recording** for each direction.
5. Return to WindowTab and save any changes with **Lưu thay đổi**. **Close Settings before trying a gesture**: global triggers are temporarily released while that dialog is open.

Open two or more non-minimized app windows on the same monitor. Swipe left for the next window, right for the previous one. The first window is a boundary, so the initial right swipe may rebound until you have advanced left.

## Settings labels

| Vietnamese | Meaning |
|---|---|
| Cử chỉ | Gesture shortcuts |
| Chuyển động | Animation preview, duration and cooldown |
| Ứng dụng | Startup and optional Desktop entry |
| Hướng dẫn | Setup help |
| Lưu thay đổi / Hủy | Save changes / Cancel |
| Tạm dừng gesture / Thoát | Pause gestures / Exit |

Animation duration is 100–600 ms; cooldown is 0–1000 ms. Desktop is excluded by default. The app targets 60 Hz updates; presentation depends on your hardware, refresh rate and workload.

WindowTab switches top-level app windows. Browser-internal tabs and Windows virtual desktops have their own behavior. Protected/elevated windows may reject previews or focus; the app does not install a touchpad driver.

Settings and status-only diagnostics stay in `%LOCALAPPDATA%\WindowTab`. Updates preserve saved settings. Before removing the app, disable **Khởi động cùng Windows**, then exit from the tray.

This first release is unsigned. Download from this repository's Releases and compare the ZIP hash with the supplied `SHA256SUMS.txt` if needed.

[Vietnamese step-by-step guide](getting-started.md) · [Report a problem](https://github.com/nguyenquocan0101/WindowTab/issues)
