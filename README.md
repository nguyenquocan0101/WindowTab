<p align="center">
  <img src="Assets/windowtab-logo.png" width="128" alt="WindowTab liquid-glass logo" />
</p>

<h1 align="center">WindowTab</h1>

<p align="center"><strong>Make every window switch feel right.</strong></p>

<p align="center">Three fingers. One fluid transition. Move between your windows with a little more flow.</p>

<p align="center">
  <a href="https://github.com/nguyenquocan0101/WindowTab/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/nguyenquocan0101/WindowTab?style=flat-square&amp;color=3063dd" /></a>
  <img alt="Windows 11 x64" src="https://img.shields.io/badge/Windows_11-x64-3063dd?style=flat-square" />
  <a href="LICENSE"><img alt="MIT license" src="https://img.shields.io/badge/License-MIT-207355?style=flat-square" /></a>
  <a href="https://github.com/nguyenquocan0101/WindowTab/actions/workflows/build.yml"><img alt="Windows build" src="https://github.com/nguyenquocan0101/WindowTab/actions/workflows/build.yml/badge.svg" /></a>
</p>

<p align="center">
  <a href="https://github.com/nguyenquocan0101/WindowTab/releases/latest/download/WindowTab-win-x64.zip"><strong>Download for Windows x64 ↓</strong></a>
  &nbsp; · &nbsp; <a href="#quick-start">Get started</a>
  &nbsp; · &nbsp; <a href="README.vi.md">Tiếng Việt</a>
</p>

---

## A little more flow between the windows you work in

From docs to code, from a conversation back to the task at hand: switching windows is part of the work. WindowTab gives those small moments a little more attention.

Inspired by the feel of MacBook transitions, WindowTab brings a **shrink → slide → zoom** rhythm to Windows. It lives in your system tray, receives two shortcuts mapped to your touchpad, and moves you through your open windows in order.

<img src="docs/assets/windowtab-flow.svg" width="100%" alt="Illustration of WindowTab's shrink, slide and zoom transition" />

<p align="center"><sub>A three-stage transition, with a stationary taskbar to keep your workspace grounded.</sub></p>

## Small details. A better feel.

| What you want | How WindowTab delivers |
|---|---|
| **A transition with rhythm** | The preview shrinks to 84%, slides horizontally, then expands to the destination window's original bounds. |
| **A sense of where you are** | Window order stays stable during a session. New windows join the list; closed windows leave it. |
| **An edge you can feel** | At either end, the preview travels about 20% and springs back. The list does not wrap around. |
| **A familiar taskbar** | The overlay stays inside the monitor's work area, leaving the taskbar stationary. |
| **Timing that suits your hands** | Adjust duration and cooldown with synchronized numeric fields, sliders or keyboard input. Try the effect inside Settings. |
| **Desktop on your terms** | Stop at the last application window by default, or enable an optional Desktop entry at the end. |
| **Ready when you sign in** | Optional Windows startup, with settings saved for your own user account. |

**The interface gets the same care:** a liquid-glass logo, four organized settings tabs, fixed Save/Cancel actions, cobalt sliders and matching timing fields.

<a id="quick-start"></a>

## Get started

### What you need

**Windows 11 x64** and a touchpad driver that supports assigning a **Custom shortcut** to three-finger swipes. Support depends on your driver; some gestures require a Precision Touchpad. [See Microsoft's gesture guide.](https://support.microsoft.com/en-us/windows/hardware/input-devices/touch-gestures-for-windows)

The download bundles its .NET runtime, so you can use the app without installing an SDK or building it yourself.

> **The app UI is currently Vietnamese.** The instructions below include the exact labels you will see, with English explanations.

### Download, map, swipe

1. **[Download the ZIP from Releases](https://github.com/nguyenquocan0101/WindowTab/releases/latest/download/WindowTab-win-x64.zip)** and extract the entire archive into a permanent folder.
2. **Run `Start-WindowTab.cmd`** to start the app and open Settings on first launch. You can also run `WindowTab.exe`, then use its tray icon: **Cài đặt…** means Settings, **Tạm dừng gesture** means Pause gestures, and **Thoát** means Exit.
3. **Read the shortcuts in the Cử chỉ (Gestures) tab.** A fresh installation uses:

   | Swipe | Default shortcut |
   |---|---|
   | Left → next window | `Ctrl + Alt + Shift + F8` |
   | Right → previous window | `Ctrl + Alt + Shift + F10` |

4. **Keep WindowTab Settings open** and click **Mở cài đặt Touchpad** (Open Touchpad settings). In Windows, go to **Bluetooth & devices → Touchpad → Advanced gestures**. Set **Swipe left/right → Custom shortcut**, choose **Start recording**, press the corresponding chord, then **Stop recording**.
5. **Return to WindowTab and close Settings.** If you changed anything, click **Lưu thay đổi** (Save changes). Open at least two non-minimized application windows on the same monitor, then swipe left to advance and right to return.

> **Close WindowTab Settings before trying a gesture.** Global shortcuts are temporarily released while that dialog is open so Windows can record the chords.

The current window starts at the beginning of the list. An initial right swipe may therefore rebound at the edge; swipe left to advance first.

Existing users keep their saved bindings. Always match the shortcuts actually displayed in the app.

**[English quick-start reference →](docs/quick-start.en.md)** · [Detailed Vietnamese setup guide](docs/getting-started.md)

## Find your rhythm

| Settings tab | What it controls |
|---|---|
| **Cử chỉ · Gestures** | The two trigger shortcuts and access to Windows Touchpad settings. |
| **Chuyển động · Motion** | Interactive preview, **100–600 ms** animation duration and **0–1000 ms** cooldown. |
| **Ứng dụng · Application** | Windows startup, the optional Desktop entry and connection status. |
| **Hướng dẫn · Help** | Setup instructions inside the app. |

For a quick, compact feel, try **220–300 ms** with a **100–150 ms** cooldown. For a slower transition that gives each stage more room, try **500–600 ms** with a **200–250 ms** cooldown. You can adjust these at any time; **Hủy** (Cancel) discards unsaved edits.

## Know what to expect

- **What moves:** application windows such as your browser, editor, Discord or File Explorer. Browser-internal tabs and Windows virtual desktops have their own behavior.
- **How gestures arrive:** Windows and the touchpad driver recognize your swipe and send the mapped shortcut. WindowTab does not install a touchpad driver, and cooldown does not change the physical swipe-distance threshold.
- **Update cadence:** a high-resolution timer targets **60 UI updates per second**, with at most one UI frame queued. Actual display presentation FPS depends on your monitor, GPU, applications and system load.
- **Which windows qualify:** visible, non-minimized windows on the same monitor. Protected windows or those running at a different privilege level may reject previews or focus changes.
- **How the effect is drawn:** an app-owned 2D overlay using live DWM previews. It does not replace the Windows window manager or install a separate desktop compositor.

If Windows refuses focus, the preview returns to the window you were using. If a preview is unavailable, WindowTab attempts a direct switch.

## Your workspace stays on your machine

The app has no account system or image-processing server. DWM renders the previews locally, and configuration and diagnostics are stored in:

```text
%LOCALAPPDATA%\WindowTab\settings.json
%LOCALAPPDATA%\WindowTab\diagnostics.log
```

Diagnostics record status codes, window counts and update timing. They do not record window titles or contents. Updates preserve your saved settings.

## When a swipe needs a little help

| What happens | Check this first |
|---|---|
| Nothing switches, or you hear a notification sound | Close Settings. Check that Windows Custom shortcuts match the app and that you clicked Stop recording. |
| One direction only rebounds | You may be at the first or last window. Try the other direction. |
| Keyboard shortcuts work, but swipes do not | Check your Windows gesture mapping and touchpad driver. |
| You cannot find the app | Look in the hidden tray icons. WindowTab intentionally runs as a single instance. |
| Custom shortcut is missing | Your driver may not support it. You can still try window switching with the keyboard shortcuts. |

Still not right? [Open an issue](https://github.com/nguyenquocan0101/WindowTab/issues) with your Windows version, touchpad model and reproduction steps. Feedback from more devices helps make the next swipe better.

## Update or remove

**To update:** exit WindowTab from its tray menu, extract the new version into your app folder, then start it again. Saved settings remain in LocalAppData. If you move the app while Windows startup is enabled, launch the new copy to refresh its startup path.

**To remove:** disable **Khởi động cùng Windows** (Start with Windows), exit the app, then remove the extracted app folder. Configuration and diagnostics remain in `%LOCALAPPDATA%\WindowTab` unless you remove them separately.

<a id="build"></a>

## Build it yourself

On Windows with the **.NET 10 SDK**:

```powershell
git clone https://github.com/nguyenquocan0101/WindowTab.git
cd WindowTab
dotnet build .\WindowTab.csproj -c Release
dotnet run --project .\WindowTab.csproj -- --settings
```

Exit an existing tray instance before starting another build. To package the Windows x64 app with its runtime, setup guide and license notices:

```powershell
.\tools\Package-Release.ps1 -Version 0.1.0
```

The output is `dist/releases/WindowTab-win-x64.zip` and `SHA256SUMS.txt`. The GitHub Windows build workflow uses this same script.

See [CONTRIBUTING.md](CONTRIBUTING.md) if you would like to improve gestures, animation, the interface or documentation.

## Download from the official source

The first public release is unsigned. Download from [this repository's Releases](https://github.com/nguyenquocan0101/WindowTab/releases) and compare the supplied `SHA256SUMS.txt` when needed:

```powershell
Get-FileHash .\WindowTab-win-x64.zip -Algorithm SHA256
```

WindowTab is distributed under the **[MIT License](LICENSE)**. Bundled runtime notices are included in the ZIP's `licenses/` directory; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). The liquid-glass logo was created with image generation, with its [prompt available here](Assets/logo-prompt.md).

---

<p align="center"><strong>A small movement. A workspace that feels a little better.</strong><br />If WindowTab feels right in your hands, a ⭐ helps the next person find it.</p>
