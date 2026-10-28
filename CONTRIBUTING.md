# Contributing to WindowTab

WindowTab started with a simple wish: make everyday window switching feel more deliberate. Improvements that preserve that feeling are welcome.

## Before opening a change

- Search existing issues first. For a bug, include the Windows version, touchpad model, app version and the shortest reproduction you can provide.
- Describe the expected behavior and the actual result. Gesture delivery, preview rendering and focus activation are different layers, so mention whether the keyboard shortcut works directly.
- Keep screenshots/logs limited to what is necessary and review them for private information.

## Build locally

Use Windows 11 and the .NET 10 SDK:

```powershell
git clone https://github.com/nguyenquocan0101/WindowTab.git
cd WindowTab
dotnet build .\WindowTab.csproj -c Release
dotnet run --project .\WindowTab.csproj -- --settings
```

Exit any existing tray instance before running another build, because the app intentionally runs as a single instance.

## What to check

- Gesture bindings restore after Settings is saved, cancelled or closed.
- Numeric fields, keyboard input and slider positions stay synchronized.
- Navigation reaches both ends without wrapping, and Desktop remains opt-in.
- The taskbar stays outside the overlay; activation failure does not leave the overlay stuck.
- New resources are embedded so the standalone executable works after extraction.

The repository currently has no committed automated test suite. The Windows build workflow verifies that the distributable can be built and packaged. Include relevant manual/runtime evidence with UI or gesture changes; avoid claiming a frame-update rate proves display presentation FPS.

## Prepare a release package

```powershell
.\tools\Package-Release.ps1
```

This produces a self-contained ZIP and checksum under `dist/releases`, including the exact bundled runtimes' license files. Application source is licensed under MIT. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for bundled components and branding provenance.
