using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PulseMonitor.Models;

namespace PulseMonitor.Services;

public sealed class SettingsService
{
    private static readonly Lazy<SettingsService> LazyInstance = new(() => new SettingsService());
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly object _gate = new();
    private AppSettings _current;

    public static SettingsService Instance => LazyInstance.Value;

    public AppSettings Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public string SettingsPath { get; }

    public event EventHandler? Changed;

    private SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PulseMonitor");
        Directory.CreateDirectory(folder);
        SettingsPath = Path.Combine(folder, AppSettings.FileName);
        _current = LoadFromDisk() ?? AppSettings.CreateDefaults();
    }

    public void Replace(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Normalize(settings);

        lock (_gate)
        {
            _current = settings.Clone();
            SaveUnlocked(_current);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Update(Action<AppSettings> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        AppSettings snapshot;
        lock (_gate)
        {
            mutate(_current);
            Normalize(_current);
            snapshot = _current.Clone();
            SaveUnlocked(snapshot);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ResetToDefaults()
    {
        Replace(AppSettings.CreateDefaults());
    }

    /// <summary>
    /// Persists window geometry without raising <see cref="Changed"/> (avoids UI thrash while resizing).
    /// </summary>
    public void SaveWindowBounds(double left, double top, double width, double height, bool maximized)
    {
        lock (_gate)
        {
            if (!_current.RememberWindowBounds)
            {
                return;
            }

            _current.StartMaximized = maximized;
            if (!maximized)
            {
                _current.WindowLeft = left;
                _current.WindowTop = top;
                _current.WindowWidth = width;
                _current.WindowHeight = height;
            }

            SaveUnlocked(_current.Clone());
        }
    }

    private AppSettings? LoadFromDisk()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (loaded is null)
            {
                return null;
            }

            Normalize(loaded);
            return loaded;
        }
        catch
        {
            return null;
        }
    }

    private void SaveUnlocked(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Persistence failures should not crash the monitor.
        }
    }

    private static void Normalize(AppSettings settings)
    {
        if (!AccentColors.All.Contains(settings.AccentColor, StringComparer.OrdinalIgnoreCase))
        {
            settings.AccentColor = AccentColors.Blue;
        }
        else
        {
            settings.AccentColor = AccentColors.All.First(a =>
                string.Equals(a, settings.AccentColor, StringComparison.OrdinalIgnoreCase));
        }

        if (!RefreshIntervals.All.Contains(settings.RefreshIntervalSeconds))
        {
            settings.RefreshIntervalSeconds = RefreshIntervals.All
                .OrderBy(v => Math.Abs(v - settings.RefreshIntervalSeconds))
                .First();
        }

        settings.CpuDecimals = settings.CpuDecimals <= 0 ? 0 : 1;
        settings.WindowOpacity = Math.Clamp(settings.WindowOpacity, 0.55, 1.0);

        if (settings.WindowWidth is < 640)
        {
            settings.WindowWidth = 820;
        }

        if (settings.WindowHeight is < 400)
        {
            settings.WindowHeight = 560;
        }
    }
}
