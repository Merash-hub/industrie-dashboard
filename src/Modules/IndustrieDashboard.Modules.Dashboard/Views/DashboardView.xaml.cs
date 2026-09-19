using System.Windows.Controls;
using IndustrieDashboard.Modules.Dashboard.ViewModels;

namespace IndustrieDashboard.Modules.Dashboard.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitialisierenAsync();
    }
}
