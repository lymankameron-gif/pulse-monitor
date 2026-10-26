using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PulseMonitor.Models;
using PulseMonitor.Services;

namespace PulseMonitor.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ProcessSampler _sampler = new();
    private readonly ProcessEnder _ender = new();
    private readonly SettingsService _settings;
    private readonly DispatcherTimer _timer;
    private readonly Dispatcher _dispatcher;
    private IReadOnlyList<ProcessRow> _snapshot = Array.Empty<ProcessRow>();
    private string _sortMember = nameof(ProcessRow.CpuPercent);
    private ListSortDirection _sortDirection = ListSortDirection.Descending;
    private int? _selectedPid;
    private bool _refreshInFlight;
    private bool _disposed;
    private bool _userChangedSort;
    private SortColumn _lastDefaultSortColumn;
    private bool _lastDefaultSortAscending;

    public ObservableCollection<ProcessRow> VisibleRows { get; } = new();

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    private string _selectedNav = "Processes";

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private string _overallCpuText = "—";

    [ObservableProperty]
    private string _memoryText = "—";

    [ObservableProperty]
    private string _statusText = "Loading…";

    [ObservableProperty]
    private DataGridGridLinesVisibility _gridLinesVisibility = DataGridGridLinesVisibility.Horizontal;

    [ObservableProperty]
    private int _alternationCount = 2;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EndTaskCommand))]
    private ProcessRow? _selectedRow;

    public bool CanEndTask =>
        SelectedRow is not null &&
        SelectedRow.Pid != Environment.ProcessId;

    public bool IsProcessesSelected =>
        string.Equals(SelectedNav, "Processes", StringComparison.Ordinal);

    public bool IsSettingsSelected =>
        string.Equals(SelectedNav, "Settings", StringComparison.Ordinal);

    public MainViewModel()
        : this(SettingsService.Instance)
    {
    }

    public MainViewModel(SettingsService settings)
    {
        _settings = settings;
        Settings = new SettingsViewModel(settings);
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        ApplySortFromSettings(force: true);
        _lastDefaultSortColumn = _settings.Current.DefaultSortColumn;
        _lastDefaultSortAscending = _settings.Current.DefaultSortAscending;
        ApplyDisplaySettings(_settings.Current);

        _timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromSeconds(Math.Max(0.5, _settings.Current.RefreshIntervalSeconds)),
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        _settings.Changed += OnSettingsChanged;
        _ = RefreshAsync();
    }

    partial void OnFilterTextChanged(string value) => RebuildVisibleRows(preserveScrollHint: true);

    partial void OnSelectedRowChanged(ProcessRow? value)
    {
        _selectedPid = value?.Pid;
        OnPropertyChanged(nameof(CanEndTask));
        EndTaskCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedNavChanged(string value)
    {
        OnPropertyChanged(nameof(IsProcessesSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    public void SelectProcesses() => SelectedNav = "Processes";

    public void SelectSettings() => SelectedNav = "Settings";

    public void ApplySort(string sortMemberPath, ListSortDirection direction)
    {
        _userChangedSort = true;
        _sortMember = sortMemberPath;
        _sortDirection = direction;
        RebuildVisibleRows(preserveScrollHint: true);
    }

    [RelayCommand(CanExecute = nameof(CanEndTask))]
    private void EndTask()
    {
        if (SelectedRow is null || SelectedRow.Pid == Environment.ProcessId)
        {
            return;
        }

        var row = SelectedRow;
        if (_settings.Current.ConfirmBeforeEndTask)
        {
            var owner = Application.Current?.MainWindow;
            var confirm = new ConfirmEndTaskWindow(row.Name, row.Pid);
            if (owner is not null)
            {
                confirm.Owner = owner;
            }

            if (confirm.ShowDialog() != true)
            {
                return;
            }
        }

        var endResult = _ender.End(row.Pid, row.Name);
        switch (endResult.Outcome)
        {
            case EndOutcome.Ok:
                StatusText = $"Ended {row.Name} (PID {row.Pid}).";
                _ = RefreshAsync();
                break;
            case EndOutcome.Gone:
                MessageBox.Show(
                    endResult.Message ?? "That process is no longer running.",
                    "Pulse Monitor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                _ = RefreshAsync();
                break;
            case EndOutcome.Rejected:
                MessageBox.Show(
                    endResult.Message ?? "Windows refused to end that process.",
                    "Pulse Monitor",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                break;
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _ = _dispatcher.InvokeAsync(() =>
        {
            var s = _settings.Current;
            ThemeService.Apply(s);
            ApplyDisplaySettings(s);

            if (s.DefaultSortColumn != _lastDefaultSortColumn ||
                s.DefaultSortAscending != _lastDefaultSortAscending)
            {
                _userChangedSort = false;
                _lastDefaultSortColumn = s.DefaultSortColumn;
                _lastDefaultSortAscending = s.DefaultSortAscending;
                ApplySortFromSettings(force: true);
            }

            var interval = TimeSpan.FromSeconds(Math.Max(0.5, s.RefreshIntervalSeconds));
            if (_timer.Interval != interval)
            {
                _timer.Interval = interval;
            }

            ReformatSnapshot();
            RebuildVisibleRows(preserveScrollHint: true);
        });
    }

    private void ApplyDisplaySettings(AppSettings s)
    {
        GridLinesVisibility = s.ShowGridLines
            ? DataGridGridLinesVisibility.Horizontal
            : DataGridGridLinesVisibility.None;
        AlternationCount = s.AlternatingRowColors ? 2 : 0;
    }

    private void ApplySortFromSettings(bool force)
    {
        if (!force && _userChangedSort)
        {
            return;
        }

        var s = _settings.Current;
        _sortMember = s.DefaultSortColumn switch
        {
            SortColumn.Name => nameof(ProcessRow.Name),
            SortColumn.Pid => nameof(ProcessRow.Pid),
            SortColumn.Memory => nameof(ProcessRow.MemoryMb),
            _ => nameof(ProcessRow.CpuPercent),
        };
        _sortDirection = s.DefaultSortAscending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;
    }

    private void OnTimerTick(object? sender, EventArgs e) => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_refreshInFlight || _disposed)
        {
            return;
        }

        _refreshInFlight = true;
        try
        {
            var snapshot = await Task.Run(() => _sampler.TakeSnapshot()).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => ApplySnapshot(snapshot));
        }
        catch (Exception ex)
        {
            await _dispatcher.InvokeAsync(() => StatusText = $"Refresh failed: {ex.Message}");
        }
        finally
        {
            _refreshInFlight = false;
        }
    }

    private void ApplySnapshot(ProcessSnapshot snapshot)
    {
        var s = _settings.Current;
        foreach (var row in snapshot.Rows)
        {
            row.ApplyFormat(s.CpuDecimals, s.MemoryUnit);
        }

        _snapshot = snapshot.Rows;
        OverallCpuText = FormatCpu(snapshot.OverallCpuPercent, s.CpuDecimals);
        MemoryText = FormatMemorySummary(snapshot.MemoryUsedMb, snapshot.MemoryTotalMb, s.MemoryUnit);
        RebuildVisibleRows(preserveScrollHint: true);
    }

    private void ReformatSnapshot()
    {
        var s = _settings.Current;
        foreach (var row in _snapshot)
        {
            row.ApplyFormat(s.CpuDecimals, s.MemoryUnit);
        }
    }

    private void RebuildVisibleRows(bool preserveScrollHint)
    {
        _ = preserveScrollHint;

        var filtered = FilterRows(_snapshot, FilterText);
        var sorted = SortRows(filtered, _sortMember, _sortDirection).ToList();

        VisibleRows.Clear();
        foreach (var row in sorted)
        {
            VisibleRows.Add(row);
        }

        if (_selectedPid is int pid)
        {
            SelectedRow = VisibleRows.FirstOrDefault(r => r.Pid == pid);
        }

        StatusText = VisibleRows.Count == 0
            ? (string.IsNullOrWhiteSpace(FilterText)
                ? "No processes."
                : "No processes match the filter.")
            : $"{VisibleRows.Count} process{(VisibleRows.Count == 1 ? string.Empty : "es")}";
    }

    private IEnumerable<ProcessRow> FilterRows(IReadOnlyList<ProcessRow> rows, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return rows;
        }

        var text = filter.Trim();
        var s = _settings.Current;
        var comparison = s.FilterCaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        var digitsOnly = text.All(char.IsDigit);

        return rows.Where(row =>
        {
            if (row.Name.Contains(text, comparison))
            {
                return true;
            }

            return s.FilterMatchPid &&
                   digitsOnly &&
                   row.Pid.ToString().Contains(text, StringComparison.Ordinal);
        });
    }

    private static IEnumerable<ProcessRow> SortRows(
        IEnumerable<ProcessRow> rows,
        string sortMember,
        ListSortDirection direction)
    {
        Func<ProcessRow, object?> key = sortMember switch
        {
            nameof(ProcessRow.Name) => r => r.Name,
            nameof(ProcessRow.Pid) => r => r.Pid,
            nameof(ProcessRow.MemoryMb) => r => r.MemoryMb,
            _ => r => r.CpuPercent ?? -1.0,
        };

        return direction == ListSortDirection.Ascending
            ? rows.OrderBy(key)
            : rows.OrderByDescending(key);
    }

    private static string FormatCpu(double percent, int decimals) =>
        decimals <= 0 ? $"{percent:0}%" : $"{percent:0.0}%";

    private static string FormatMemorySummary(double usedMb, double totalMb, MemoryUnit unit)
    {
        if (unit == MemoryUnit.Gb)
        {
            return $"{usedMb / 1024.0:0.00} / {totalMb / 1024.0:0.00} GB";
        }

        return $"{usedMb:0} / {totalMb:0} MB";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _settings.Changed -= OnSettingsChanged;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
