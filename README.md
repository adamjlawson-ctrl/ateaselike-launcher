# At Ease (Windows 11)

Phase 1 scaffold for a simplified launcher-style desktop app.

## Tech Stack

- C#
- .NET 8
- WinUI 3 (Windows App SDK)
- MVVM (CommunityToolkit.Mvvm)
- JSON settings in local app data

## Current Scope

- Home launcher view with large app/folder tiles
- Settings view to toggle item visibility
- Placeholder simplified browsing view
- JSON settings service with defaults and validation
- Single-user model based on current Windows account

## Settings Location

`%LOCALAPPDATA%\\AtEaseWin11\\settings.json`

## Build Notes

This workspace was scaffolded without running `dotnet` commands because the .NET SDK is not currently available in this terminal.

When SDK is installed, run:

1. `dotnet --info`
2. `dotnet restore src/AtEase.App/AtEase.App.csproj`
3. `dotnet build src/AtEase.App/AtEase.App.csproj`

If WinUI templates/workloads are missing, install Windows App SDK tooling for .NET 8 on Windows 11.
