using System;
using System.Windows;
using System.Windows.Controls;

namespace Symbolic11.Pages;

public partial class HomePage : Page
{
    private readonly Action create;
    private readonly Action manage;
    private readonly Action settings;
    public HomePage(Action create, Action manage, Action settings)
    {
        InitializeComponent();
        this.create = create;
        this.manage = manage;
        this.settings = settings;
    }
    private void Create_Click(object sender, RoutedEventArgs e) => create();
    private void Manage_Click(object sender, RoutedEventArgs e) => manage();
    private void Settings_Click(object sender, RoutedEventArgs e) => settings();
}