using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PulseMonitor.Models;
using PulseMonitor.Services;

namespace PulseMonitor.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private bool _suppressPersist;

    public IReadOnlyList<ThemeMode> ThemeOptions { get; } =
        [ThemeMode.Dark, ThemeMode.Light, ThemeMode.System];

    public IReadOnlyList<string> AccentOptions => AccentColors.All;

    public IReadOnlyList<UiFontSize> FontSizeOptions { get; } =
        [UiFontSize.Small, UiFontSize.Medium, UiFontSize.Large];

    public IReadOnlyList<double> RefreshIntervalOptions => RefreshIntervals.All;

    public IReadOnlyList<MemoryUnit> MemoryUnitOptions { get; } =
        [MemoryUnit.Mb, MemoryUnit.Gb];

    public IReadOnlyList<int> CpuDecimalOptions { get; } = [0, 1];

    public IReadOnlyList<SortColumn> SortColumnOptions { get; } =
        [SortColumn.Name, SortColumn.Pid, SortColumn.Cpu, SortColumn.Memory];

    [ObservableProperty]
    private ThemeMode _theme;

    [ObservableProperty]
    private string _accentColor = AccentColors.Blue;

    [ObservableProperty]
    private UiFontSize _fontSize;

    [ObservableProperty]
    private bool _rememberWindowBounds;

    [ObservableProperty]
    private bool _startMaximized;

    [ObservableProperty]
    private double _refreshIntervalSeconds = 1.0;

    [ObservableProperty]
    private bool _showPidColumn = true;

    [ObservableProperty]
    private bool _showCpuColumn = true;

    [ObservableProperty]
    private bool _showMemoryColumn = true;

    [ObservableProperty]
    private MemoryUnit _memoryUnit;

    [ObservableProperty]
    private int _cpuDecimals = 1;

    [ObservableProperty]
    private SortColumn _defaultSortColumn;

    [ObservableProperty]
    private bool _defaultSortAscending;

    [ObservableProperty]
    private bool _confirmBeforeEndTask = true;

    [ObservableProperty]
    private bool _alwaysOnTop;

    [ObservableProperty]
    private double _windowOpacity = 1.0;

    [ObservableProperty]
    private bool _showGridLines = true;

    [ObservableProperty]
    private bool _alternatingRowColors = true;

    [ObservableProperty]
    private bool _filterCaseSensitive;

    [ObservableProperty]
    private bool _filterMatchPid = true;

    [ObservableProperty]
    private string _settingsPath = string.Empty;

    public SettingsViewModel()
        : this(SettingsService.Instance)
    {
    }

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        SettingsPath = settings.SettingsPath;
        LoadFrom(_settings.Current);
        _settings.Changed += OnSettingsChangedExternally;
    }

    private void OnSettingsChangedExternally(object? sender, EventArgs e)
    {
        if (_suppressPersist)
        {
            return;
        }

        LoadFrom(_settings.Current);
    }

    private void LoadFrom(AppSettings s)
    {
        _suppressPersist = true;
        try
        {
            Theme = s.Theme;
            AccentColor = s.AccentColor;
            FontSize = s.FontSize;
            RememberWindowBounds = s.RememberWindowBounds;
            StartMaximized = s.StartMaximized;
            RefreshIntervalSeconds = s.RefreshIntervalSeconds;
            ShowPidColumn = s.ShowPidColumn;
            ShowCpuColumn = s.ShowCpuColumn;
            ShowMemoryColumn = s.ShowMemoryColumn;
            MemoryUnit = s.MemoryUnit;
            CpuDecimals = s.CpuDecimals;
            DefaultSortColumn = s.DefaultSortColumn;
            DefaultSortAscending = s.DefaultSortAscending;
            ConfirmBeforeEndTask = s.ConfirmBeforeEndTask;
            AlwaysOnTop = s.AlwaysOnTop;
            WindowOpacity = s.WindowOpacity;
            ShowGridLines = s.ShowGridLines;
            AlternatingRowColors = s.AlternatingRowColors;
            FilterCaseSensitive = s.FilterCaseSensitive;
            FilterMatchPid = s.FilterMatchPid;
        }
        finally
        {
            _suppressPersist = false;
        }
    }

    partial void OnThemeChanged(ThemeMode value) => Persist();
    partial void OnAccentColorChanged(string value) => Persist();
    partial void OnFontSizeChanged(UiFontSize value) => Persist();
    partial void OnRememberWindowBoundsChanged(bool value) => Persist();
    partial void OnStartMaximizedChanged(bool value) => Persist();
    partial void OnRefreshIntervalSecondsChanged(double value) => Persist();
    partial void OnShowPidColumnChanged(bool value) => Persist();
    partial void OnShowCpuColumnChanged(bool value) => Persist();
    partial void OnShowMemoryColumnChanged(bool value) => Persist();
    partial void OnMemoryUnitChanged(MemoryUnit value) => Persist();
    partial void OnCpuDecimalsChanged(int value) => Persist();
    partial void OnDefaultSortColumnChanged(SortColumn value) => Persist();
    partial void OnDefaultSortAscendingChanged(bool value) => Persist();
    partial void OnConfirmBeforeEndTaskChanged(bool value) => Persist();
    partial void OnAlwaysOnTopChanged(bool value) => Persist();
    partial void OnWindowOpacityChanged(double value) => Persist();
    partial void OnShowGridLinesChanged(bool value) => Persist();
    partial void OnAlternatingRowColorsChanged(bool value) => Persist();
    partial void OnFilterCaseSensitiveChanged(bool value) => Persist();
    partial void OnFilterMatchPidChanged(bool value) => Persist();

    private void Persist()
    {
        if (_suppressPersist)
        {
            return;
        }

        _suppressPersist = true;
        try
        {
            _settings.Update(s =>
            {
                s.Theme = Theme;
                s.AccentColor = AccentColor;
                s.FontSize = FontSize;
                s.RememberWindowBounds = RememberWindowBounds;
                s.StartMaximized = StartMaximized;
                s.RefreshIntervalSeconds = RefreshIntervalSeconds;
                s.ShowPidColumn = ShowPidColumn;
                s.ShowCpuColumn = ShowCpuColumn;
                s.ShowMemoryColumn = ShowMemoryColumn;
                s.MemoryUnit = MemoryUnit;
                s.CpuDecimals = CpuDecimals;
                s.DefaultSortColumn = DefaultSortColumn;
                s.DefaultSortAscending = DefaultSortAscending;
                s.ConfirmBeforeEndTask = ConfirmBeforeEndTask;
                s.AlwaysOnTop = AlwaysOnTop;
                s.WindowOpacity = WindowOpacity;
                s.ShowGridLines = ShowGridLines;
                s.AlternatingRowColors = AlternatingRowColors;
                s.FilterCaseSensitive = FilterCaseSensitive;
                s.FilterMatchPid = FilterMatchPid;
            });
        }
        finally
        {
            _suppressPersist = false;
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _settings.ResetToDefaults();
        LoadFrom(_settings.Current);
    }
}
