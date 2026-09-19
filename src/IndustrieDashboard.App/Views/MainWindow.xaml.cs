using System.Windows;
using IndustrieDashboard.App.ViewModels;

namespace IndustrieDashboard.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
