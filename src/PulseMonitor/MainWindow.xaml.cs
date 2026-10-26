using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PulseMonitor.Models;
using PulseMonitor.Services;
using PulseMonitor.ViewModels;

namespace PulseMonitor;

public partial class MainWindow : Window
{
    private readonly SettingsService _settings = SettingsService.Instance;
    private bool _suppressBoundsPersist;
    private bool _loaded;

    public MainWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
        _settings.Changed += OnSettingsChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _suppressBoundsPersist = true;
        try
        {
            _lastDefaultSortColumn = _settings.Current.DefaultSortColumn;
            _lastDefaultSortAscending = _settings.Current.DefaultSortAscending;
            ApplyWindowSettings(_settings.Current, applyBounds: true);
            ApplyGridSettings(_settings.Current);
            SyncSortIndicators();
        }
        finally
        {
            _suppressBoundsPersist = false;
            _loaded = true;
        }
    }

    private SortColumn _lastDefaultSortColumn;
    private bool _lastDefaultSortAscending;
    private bool _userHasSorted;

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var s = _settings.Current;
            ApplyWindowSettings(s, applyBounds: false);
            ApplyGridSettings(s);

            if (s.DefaultSortColumn != _lastDefaultSortColumn ||
                s.DefaultSortAscending != _lastDefaultSortAscending)
            {
                _lastDefaultSortColumn = s.DefaultSortColumn;
                _lastDefaultSortAscending = s.DefaultSortAscending;
                _userHasSorted = false;
                SyncSortIndicators();
            }
        });
    }

    private void ApplyWindowSettings(AppSettings s, bool applyBounds)
    {
        Topmost = s.AlwaysOnTop;
        Opacity = Math.Clamp(s.WindowOpacity, 0.55, 1.0);
        FontSize = s.FontSize switch
        {
            UiFontSize.Small => 12,
            UiFontSize.Large => 15,
            _ => 13,
        };

        if (!applyBounds)
        {
            return;
        }

        if (s.StartMaximized)
        {
            WindowState = WindowState.Maximized;
            return;
        }

        if (!s.RememberWindowBounds)
        {
            return;
        }

        if (s.WindowWidth is double w && s.WindowHeight is double h)
        {
            Width = w;
            Height = h;
        }

        if (s.WindowLeft is double left && s.WindowTop is double top)
        {
            Left = left;
            Top = top;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
    }

    private void ApplyGridSettings(AppSettings s)
    {
        PidColumn.Visibility = s.ShowPidColumn ? Visibility.Visible : Visibility.Collapsed;
        CpuColumn.Visibility = s.ShowCpuColumn ? Visibility.Visible : Visibility.Collapsed;
        MemoryColumn.Visibility = s.ShowMemoryColumn ? Visibility.Visible : Visibility.Collapsed;
        MemoryColumn.Header = s.MemoryUnit == MemoryUnit.Gb ? "Memory (GB)" : "Memory (MB)";

        if (s.AlternatingRowColors)
        {
            ProcessGrid.AlternationCount = 2;
            ProcessGrid.ClearValue(DataGrid.AlternatingRowBackgroundProperty);
        }
        else
        {
            ProcessGrid.AlternationCount = 0;
            ProcessGrid.AlternatingRowBackground = ProcessGrid.RowBackground ?? Brushes.Transparent;
        }
    }

    private void OnBoundsChanged(object? sender, EventArgs e) => PersistBoundsIfNeeded();

    private void OnStateChanged(object? sender, EventArgs e) => PersistBoundsIfNeeded();

    private void PersistBoundsIfNeeded()
    {
        if (!_loaded || _suppressBoundsPersist || !_settings.Current.RememberWindowBounds)
        {
            return;
        }

        if (WindowState == WindowState.Minimized)
        {
            return;
        }

        var maximized = WindowState == WindowState.Maximized;
        var bounds = RestoreBounds;
        _settings.SaveWindowBounds(bounds.Left, bounds.Top, bounds.Width, bounds.Height, maximized);
    }

    private void OnClosing(object? sender, CancelEventArgs e) => PersistBoundsIfNeeded();

    private void OnClosed(object? sender, EventArgs e)
    {
        _settings.Changed -= OnSettingsChanged;
        if (DataContext is MainViewModel vm)
        {
            vm.Dispose();
        }
    }

    private void OnProcessesNavChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectProcesses();
        }
    }

    private void OnSettingsNavChecked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.SelectSettings();
        }
    }

    private void OnSorting(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not MainViewModel vm || e.Column.SortMemberPath is not { Length: > 0 } path)
        {
            return;
        }

        var direction = e.Column.SortDirection != ListSortDirection.Ascending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;

        foreach (var column in ((DataGrid)sender).Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = direction;
        _userHasSorted = true;
        vm.ApplySort(path, direction);
    }

    private void SyncSortIndicators()
    {
        var s = _settings.Current;
        var path = s.DefaultSortColumn switch
        {
            SortColumn.Name => nameof(ProcessRow.Name),
            SortColumn.Pid => nameof(ProcessRow.Pid),
            SortColumn.Memory => nameof(ProcessRow.MemoryMb),
            _ => nameof(ProcessRow.CpuPercent),
        };
        var direction = s.DefaultSortAscending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;

        foreach (var column in ProcessGrid.Columns)
        {
            column.SortDirection = column.SortMemberPath == path ? direction : null;
        }
    }
}
