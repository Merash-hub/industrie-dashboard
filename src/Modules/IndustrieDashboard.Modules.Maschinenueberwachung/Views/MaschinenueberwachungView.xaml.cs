using System.Windows.Controls;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Maschinenueberwachung.Views;

public partial class MaschinenueberwachungView : UserControl
{
    public MaschinenueberwachungView(PlatzhalterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
