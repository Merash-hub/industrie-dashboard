using System.Windows.Controls;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Kontrolleingriffe.Views;

public partial class KontrolleingriffeView : UserControl
{
    public KontrolleingriffeView(PlatzhalterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
