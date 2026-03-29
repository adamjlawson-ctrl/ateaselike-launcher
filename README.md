# AtEase-Win11

AtEase-Win11 is a WinUI 3 launcher prototype inspired by classic large-tile desktop launchers, built for Windows 11 with .NET 8.

## Release Notes

### Latest

- Added removable media tabs with integrated media browsing support.
- Added targeted eject flow that prompts for which removable drive to eject.
- Added app-active indicator menu for switching between launched apps and returning to AtEase.
- Implemented in-panel folder browsing with back navigation.
- Added per-launch display selection for app launch targets.
- Hardened non-primary display window placement, including fallback handling for shell-hosted apps like Calculator.
- Improved launcher click reliability by using explicit click forwarding in key tile templates.
- Refreshed project documentation and build/run guidance.

## Highlights

- Launcher-style home experience with large app and folder tiles.
- Folder browsing inside the launcher panel with back navigation.
- Removable media shown as dedicated tabs with browsable contents.
- Special actions menu, including drive eject selection.
- App-active mode with tracked running apps and return switching.
- Per-launch display picker for apps, with monitor-aware window placement.
- User settings persisted to local app data as JSON.

## Tech Stack

- C#
- .NET 8
- WinUI 3 (Windows App SDK)
- MVVM (CommunityToolkit.Mvvm)
- Microsoft.Extensions.DependencyInjection
- JSON-backed settings

## Repository Layout

- src/AtEase.App: Main WinUI app
- tests/AtEase.App.Tests: Unit tests
- docs/architecture: Architecture notes
- docs/ux-notes: UX references and intent

## Requirements

- Windows 11
- .NET SDK 8.x
- Windows App SDK tooling/workloads for WinUI 3

## Build and Run

Build app project:

```powershell
dotnet build src/AtEase.App/AtEase.App.csproj
```

Run app project:

```powershell
dotnet run --project src/AtEase.App/AtEase.App.csproj
```

Or run compiled executable directly:

```powershell
src/AtEase.App/bin/Debug/net8.0-windows10.0.19041.0/AtEase.App.exe
```

## Settings Storage

Settings are stored per user at:

%LOCALAPPDATA%\\AtEaseWin11\\settings.json

## Current Behavior Notes

- App launch flow supports selecting a target display at launch time.
- Window placement uses monitor detection and retries to handle delayed window creation.
- Shell-hosted apps may behave differently than standard Win32 apps; logic includes fallback targeting for new top-level windows.

## Testing

Run tests with:

```powershell
dotnet test AtEase-Win11.sln
```

If tests fail due local environment or missing test dependencies, validate app changes with a focused app project build command first.
