using System.Windows;

namespace PulseMonitor;

public partial class ConfirmEndTaskWindow : Window
{
    public ConfirmEndTaskWindow(string processName, int pid)
    {
        InitializeComponent();
        MessageText.Text = $"End task \"{processName}\" (PID {pid})?";
    }

    private void OnEndTask(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
