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
        ManageTab.FontWeight = FontWeights.Normal;
    }
    private void ShowManage()
    {
        Workspace.Navigate(new LinkView());
        CreateTab.FontWeight = FontWeights.Normal;
        ManageTab.FontWeight = FontWeights.SemiBold;
    }
    private void Create_Click(object sender, RoutedEventArgs e) => ShowCreate();
    private void Manage_Click(object sender, RoutedEventArgs e) => ShowManage();
}
