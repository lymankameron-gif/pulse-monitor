using System.Windows;
using Microsoft.Win32;
using PulseMonitor.Services;

namespace PulseMonitor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = SettingsService.Instance;
        ThemeService.Apply(settings.Current);

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        Exit += (_, _) => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color))
        {
            return;
        }

        var settings = SettingsService.Instance;
        if (settings.Current.Theme != Models.ThemeMode.System)
        {
            return;
        }

        Current?.Dispatcher.InvokeAsync(() => ThemeService.Apply(settings.Current));
    }
}
