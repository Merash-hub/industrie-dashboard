using System.Windows.Controls;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Schichtplanung.Views;

public partial class SchichtplanungView : UserControl
{
    public SchichtplanungView(PlatzhalterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
