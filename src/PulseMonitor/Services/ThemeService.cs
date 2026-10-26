using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PulseMonitor.Models;

namespace PulseMonitor.Services;

public static class ThemeService
{
    private const string ThemeDictionaryPathPrefix = "Themes/";

    public static void Apply(AppSettings settings)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var mode = ResolveTheme(settings.Theme);
        SwapThemeDictionary(app, mode);
        ApplyAccent(app, settings.AccentColor);
        ApplyFontSize(app, settings.FontSize);
    }

    public static ThemeMode ResolveTheme(ThemeMode requested)
    {
        if (requested != ThemeMode.System)
        {
            return requested;
        }

        return IsSystemLightTheme() ? ThemeMode.Light : ThemeMode.Dark;
    }

    public static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i)
            {
                return i != 0;
            }
        }
        catch
        {
            // Fall through to dark.
        }

        return false;
    }

    private static void SwapThemeDictionary(Application app, ThemeMode mode)
    {
        var source = new Uri(
            $"{ThemeDictionaryPathPrefix}{(mode == ThemeMode.Light ? "Light" : "Dark")}.xaml",
            UriKind.Relative);

        var dictionaries = app.Resources.MergedDictionaries;
        ResourceDictionary? existing = null;
        foreach (var dictionary in dictionaries)
        {
            if (dictionary.Source is not null &&
                dictionary.Source.OriginalString.Contains("Themes/", StringComparison.OrdinalIgnoreCase))
            {
                existing = dictionary;
                break;
            }
        }

        var next = new ResourceDictionary { Source = source };
        if (existing is null)
        {
            dictionaries.Insert(0, next);
        }
        else
        {
            var index = dictionaries.IndexOf(existing);
            dictionaries[index] = next;
        }
    }

    private static void ApplyAccent(Application app, string accentName)
    {
        var (active, inactive) = AccentToColors(accentName);
        app.Resources["SelectionBrush"] = new SolidColorBrush(active);
        app.Resources["SelectionInactiveBrush"] = new SolidColorBrush(inactive);
    }

    private static void ApplyFontSize(Application app, UiFontSize size)
    {
        var value = size switch
        {
            UiFontSize.Small => 12.0,
            UiFontSize.Large => 15.0,
            _ => 13.0,
        };

        if (app.MainWindow is not null)
        {
            app.MainWindow.FontSize = value;
        }
    }

    private static (Color Active, Color Inactive) AccentToColors(string accentName) =>
        accentName switch
        {
            AccentColors.Teal => (Color.FromRgb(0x0F, 0x7B, 0x82), Color.FromRgb(0x1A, 0x4F, 0x54)),
            AccentColors.Green => (Color.FromRgb(0x2E, 0x7D, 0x32), Color.FromRgb(0x1B, 0x4D, 0x20)),
            AccentColors.Orange => (Color.FromRgb(0xC0, 0x5A, 0x18), Color.FromRgb(0x7A, 0x3A, 0x10)),
            AccentColors.Red => (Color.FromRgb(0xC4, 0x2B, 0x2B), Color.FromRgb(0x7A, 0x1F, 0x1F)),
            _ => (Color.FromRgb(0x0E, 0x63, 0x9C), Color.FromRgb(0x26, 0x4F, 0x78)),
        };
}
