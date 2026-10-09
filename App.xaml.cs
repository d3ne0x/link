using System.Windows;
using System.Windows.Threading;

namespace Symbolic11;

public partial class App : Application
{
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show(
            $"An unexpected error occurred:\n{e.Exception.Message}",
            "Symbolic11 Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
