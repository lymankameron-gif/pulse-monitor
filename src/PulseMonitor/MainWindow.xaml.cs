using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using PulseMonitor.Models;
using PulseMonitor.ViewModels;

namespace PulseMonitor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Dispose();
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
        vm.ApplySort(path, direction);
    }
}
