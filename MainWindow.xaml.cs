using System.Windows;
using Symbolic11.Pages;

namespace Symbolic11;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ShowCreate();
    }
    private void ShowCreate()
    {
        Workspace.Navigate(new CreateLink());
        CreateTab.FontWeight = FontWeights.SemiBold;
        CreateTab.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(8,105,239));
        ManageTab.Foreground = System.Windows.Media.Brushes.Black;
        ManageTab.FontWeight = FontWeights.Normal;
    }
    private void ShowManage()
    {
        Workspace.Navigate(new LinkView());
        CreateTab.FontWeight = FontWeights.Normal;
        ManageTab.FontWeight = FontWeights.SemiBold;
        ManageTab.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(8,105,239));
        CreateTab.Foreground = System.Windows.Media.Brushes.Black;
    }
    private void Create_Click(object sender, RoutedEventArgs e) => ShowCreate();
    private void Manage_Click(object sender, RoutedEventArgs e) => ShowManage();
}
