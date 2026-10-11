using System.Windows;
using Symbolic11.Pages;

namespace Symbolic11;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Workspace.Navigate(new HomePage(NavigateCreate, NavigateManage, NavigateSettings));
    }

    private void NavigateCreate() => Workspace.Navigate(new CreateLink());
    private void NavigateManage() => Workspace.Navigate(new LinkView());
    private void NavigateSettings() => Workspace.Navigate(new SettingsPage());
    private void Home_Click(object sender, RoutedEventArgs e) => Workspace.Navigate(new HomePage(NavigateCreate, NavigateManage, NavigateSettings));
    private void Create_Click(object sender, RoutedEventArgs e) => NavigateCreate();
    private void Manage_Click(object sender, RoutedEventArgs e) => NavigateManage();
    private void Settings_Click(object sender, RoutedEventArgs e) => NavigateSettings();
    private void About_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show("LinkCraft — Create • Manage • Simplify Links\nPhase 1 preview.\nBuilt on Symbolic11.", "About LinkCraft");
}
