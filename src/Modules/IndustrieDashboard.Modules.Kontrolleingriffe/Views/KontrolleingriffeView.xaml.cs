using System.Windows.Controls;
using IndustrieDashboard.Modules.Kontrolleingriffe.ViewModels;

namespace IndustrieDashboard.Modules.Kontrolleingriffe.Views;

public partial class KontrolleingriffeView : UserControl
{
    public KontrolleingriffeView(KontrolleingriffeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.InitialisierenAsync();
    }
}
