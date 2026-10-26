using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
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
    private readonly DispatcherTimer _timer;
    private readonly Dispatcher _dispatcher;
    private IReadOnlyList<ProcessRow> _snapshot = Array.Empty<ProcessRow>();
    private string _sortMember = nameof(ProcessRow.CpuPercent);
    private ListSortDirection _sortDirection = ListSortDirection.Descending;
    private int? _selectedPid;
    private bool _refreshInFlight;
    private bool _disposed;

    public ObservableCollection<ProcessRow> VisibleRows { get; } = new();

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private string _overallCpuText = "—";

    [ObservableProperty]
    private string _memoryText = "—";

    [ObservableProperty]
    private string _statusText = "Loading…";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EndTaskCommand))]
    private ProcessRow? _selectedRow;

    public bool CanEndTask =>
        SelectedRow is not null &&
        SelectedRow.Pid != Environment.ProcessId;

    public MainViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        _ = RefreshAsync();
    }

    partial void OnFilterTextChanged(string value) => RebuildVisibleRows(preserveScrollHint: true);

    partial void OnSelectedRowChanged(ProcessRow? value)
    {
        _selectedPid = value?.Pid;
        OnPropertyChanged(nameof(CanEndTask));
        EndTaskCommand.NotifyCanExecuteChanged();
    }

    public void ApplySort(string sortMemberPath, ListSortDirection direction)
    {
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
        _snapshot = snapshot.Rows;
        OverallCpuText = $"{snapshot.OverallCpuPercent:0.0}%";
        MemoryText = $"{snapshot.MemoryUsedMb:0} / {snapshot.MemoryTotalMb:0} MB";
        RebuildVisibleRows(preserveScrollHint: true);
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

    private static IEnumerable<ProcessRow> FilterRows(IReadOnlyList<ProcessRow> rows, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return rows;
        }

        var text = filter.Trim();
        var digitsOnly = text.All(char.IsDigit);

        return rows.Where(row =>
        {
            if (row.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return digitsOnly && row.Pid.ToString().Contains(text, StringComparison.Ordinal);
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
