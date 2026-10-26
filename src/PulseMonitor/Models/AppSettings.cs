namespace PulseMonitor.Models;

public sealed class AppSettings
{
    public const string FileName = "settings.json";

    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    public string AccentColor { get; set; } = AccentColors.Blue;
    public UiFontSize FontSize { get; set; } = UiFontSize.Medium;

    public bool RememberWindowBounds { get; set; } = true;
    public bool StartMaximized { get; set; }
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }

    /// <summary>Refresh interval in seconds. Supported: 0.5, 1, 2, 5.</summary>
    public double RefreshIntervalSeconds { get; set; } = 1.0;

    public bool ShowPidColumn { get; set; } = true;
    public bool ShowCpuColumn { get; set; } = true;
    public bool ShowMemoryColumn { get; set; } = true;

    public MemoryUnit MemoryUnit { get; set; } = MemoryUnit.Mb;
    public int CpuDecimals { get; set; } = 1;

    public SortColumn DefaultSortColumn { get; set; } = SortColumn.Cpu;
    public bool DefaultSortAscending { get; set; }

    public bool ConfirmBeforeEndTask { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public double WindowOpacity { get; set; } = 1.0;

    public bool ShowGridLines { get; set; } = true;
    public bool AlternatingRowColors { get; set; } = true;

    public bool FilterCaseSensitive { get; set; }
    public bool FilterMatchPid { get; set; } = true;

    public static AppSettings CreateDefaults() => new();

    public AppSettings Clone() => new()
    {
        Theme = Theme,
        AccentColor = AccentColor,
        FontSize = FontSize,
        RememberWindowBounds = RememberWindowBounds,
        StartMaximized = StartMaximized,
        WindowLeft = WindowLeft,
        WindowTop = WindowTop,
        WindowWidth = WindowWidth,
        WindowHeight = WindowHeight,
        RefreshIntervalSeconds = RefreshIntervalSeconds,
        ShowPidColumn = ShowPidColumn,
        ShowCpuColumn = ShowCpuColumn,
        ShowMemoryColumn = ShowMemoryColumn,
        MemoryUnit = MemoryUnit,
        CpuDecimals = CpuDecimals,
        DefaultSortColumn = DefaultSortColumn,
        DefaultSortAscending = DefaultSortAscending,
        ConfirmBeforeEndTask = ConfirmBeforeEndTask,
        AlwaysOnTop = AlwaysOnTop,
        WindowOpacity = WindowOpacity,
        ShowGridLines = ShowGridLines,
        AlternatingRowColors = AlternatingRowColors,
        FilterCaseSensitive = FilterCaseSensitive,
        FilterMatchPid = FilterMatchPid,
    };
}

public enum ThemeMode
{
    Dark,
    Light,
    System,
}

public enum UiFontSize
{
    Small,
    Medium,
    Large,
}

public enum MemoryUnit
{
    Mb,
    Gb,
}

public enum SortColumn
{
    Name,
    Pid,
    Cpu,
    Memory,
}

public static class AccentColors
{
    public const string Blue = "Blue";
    public const string Teal = "Teal";
    public const string Green = "Green";
    public const string Orange = "Orange";
    public const string Red = "Red";

    public static readonly IReadOnlyList<string> All =
    [
        Blue,
        Teal,
        Green,
        Orange,
        Red,
    ];
}

public static class RefreshIntervals
{
    public static readonly IReadOnlyList<double> All = [0.5, 1.0, 2.0, 5.0];
}
