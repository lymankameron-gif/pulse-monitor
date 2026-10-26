# Pulse Monitor

A small Windows process monitor built with C# and WPF on .NET 8. One window shows overall CPU and memory, a searchable process list, End task, and a Settings page for theme and list options.

## Requirements

- Windows 10 or newer
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build and run

### Cursor / VS Code (live reload)

1. Open this folder in Cursor (File → Open Folder…).
2. Terminal → Run Task… → **Watch Pulse Monitor** (or press Ctrl+Shift+B — it is the default build task).
3. Edit and save; `dotnet watch` rebuilds and restarts the app.

### Command line

From the repository root:

```bash
dotnet restore PulseMonitor.sln
dotnet build PulseMonitor.sln -c Release
dotnet run --project src/PulseMonitor/PulseMonitor.csproj -c Release
```

Or open `PulseMonitor.sln` in Visual Studio 2022 and press F5.

## Settings

Use the **Settings** tab for theme (Dark / Light / Follow system), accent color, font size, refresh interval, column visibility, memory units, sort defaults, filter options, confirm-before-end-task, always-on-top, opacity, and more. Values persist under `%LocalAppData%\PulseMonitor\settings.json`.

## Layout

```text
src/PulseMonitor/
  App.xaml
  MainWindow.xaml
  Themes/Dark.xaml
  Themes/Light.xaml
  Models/
  Services/SettingsService.cs
  Services/ThemeService.cs
  Services/ProcessSampler.cs
  Services/ProcessEnder.cs
  ViewModels/MainViewModel.cs
  ViewModels/SettingsViewModel.cs
```

## License

MIT — see [LICENSE](LICENSE).
