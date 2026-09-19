using System.Collections.ObjectModel;
using IndustrieDashboard.Shared.Modules;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.App.ViewModels;

/// <summary>
/// ViewModel der Shell (MainWindow). Kennt nur die Liste der Module und deren
/// Metadaten - welche fachlichen Views dahinterstecken, ist der Shell egal
/// (siehe IAppModule.ErzeugeStartView).
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private object? _aktuelleAnsicht;
    private NavigationEintrag? _ausgewaehlterEintrag;

    public MainWindowViewModel(IServiceProvider serviceProvider, IReadOnlyList<IAppModule> module)
    {
        _serviceProvider = serviceProvider;

        foreach (var modul in module.OrderBy(m => m.Reihenfolge))
        {
            NavigationEintraege.Add(new NavigationEintrag(modul));
        }

        AusgewaehlterEintrag = NavigationEintraege.FirstOrDefault();
    }

    public ObservableCollection<NavigationEintrag> NavigationEintraege { get; } = new();

    public object? AktuelleAnsicht
    {
        get => _aktuelleAnsicht;
        private set => SetProperty(ref _aktuelleAnsicht, value);
    }

    public NavigationEintrag? AusgewaehlterEintrag
    {
        get => _ausgewaehlterEintrag;
        set
        {
            if (SetProperty(ref _ausgewaehlterEintrag, value) && value is not null)
            {
                AktuelleAnsicht = value.Modul.ErzeugeStartView(_serviceProvider);
            }
        }
    }
}
