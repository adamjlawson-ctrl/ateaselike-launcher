# Phase 1 Architecture

## Layers

- Models: persistent app data and launcher entities
- Services: JSON settings, item projection, launch behavior, validation, navigation
- ViewModels: shell orchestration and screen state for Home/Settings/Browse
- Views: WinUI screens with minimal Windows 11 native look

## Flow

1. App starts and builds a DI container.
2. ShellViewModel initializes Home, Settings, and Browser state.
3. JsonSettingsService loads settings from local app data, creating defaults on first run.
4. HomeViewModel renders visible launcher items.
5. SettingsViewModel toggles visibility and persists to JSON.

## Constraints Enforced

- No database
- No authentication
- No multi-user account model
- Single local settings file for currently logged-in Windows user

## Deferred to Later Phases

- Full guided navigation graph
- Rich icon extraction
- App/folder discovery wizard
- Complete simplified desktop browsing interactions
- Polished accessibility and animation pass
