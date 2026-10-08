using System.Collections.ObjectModel;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Shared.Modules;
using IndustrieDashboard.Shared.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrieDashboard.App.ViewModels;

/// <summary>
/// ViewModel der Shell (MainWindow). Kennt nur die Liste der Module und deren
/// Metadaten - welche fachlichen Views dahinterstecken, ist der Shell egal
/// (siehe IAppModule.ErzeugeStartView). Zeigt zusätzlich die Kopfzeile mit
/// Anzeigename und Rollen (Spezifikation A10) - reine Information, keine
/// Schutzfunktion; die Dienste prüfen jede Aktion unabhängig selbst.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBenutzerKontext _benutzerKontext;
    private object? _aktuelleAnsicht;
    private NavigationEintrag? _ausgewaehlterEintrag;
    private IServiceScope? _aktuellerModulScope;

    public MainWindowViewModel(IServiceProvider serviceProvider, IReadOnlyList<IAppModule> module, IBenutzerKontext benutzerKontext)
    {
        _serviceProvider = serviceProvider;
        _benutzerKontext = benutzerKontext;

        foreach (var modul in module.OrderBy(m => m.Reihenfolge))
        {
            NavigationEintraege.Add(new NavigationEintrag(modul));
        }

        AusgewaehlterEintrag = NavigationEintraege.FirstOrDefault();

        _benutzerKontext.BenutzerGewechselt += OnBenutzerGewechselt;
    }

    public ObservableCollection<NavigationEintrag> NavigationEintraege { get; } = new();

    public string AngemeldeterBenutzer => _benutzerKontext.AktuellerBenutzer.Anzeigename;

    public string RollenAnzeige => _benutzerKontext.Rollen.Count == 0
        ? "Keine Rolle zugewiesen"
        : string.Join(", ", _benutzerKontext.Rollen.Select(r => r.ToString()).OrderBy(r => r));

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
                WechsleModul(value);
            }
        }
    }

    /// <summary>
    /// Jeder Modulwechsel bekommt einen eigenen DI-Scope. Die View (und damit
    /// transitiv ihr ViewModel) wird aus diesem Scope aufgelöst; wird der
    /// vorherige Scope danach verworfen, disposed der Container automatisch
    /// alle IDisposable-Instanzen, die er erzeugt hat. Ohne das sammeln sich
    /// bei jedem Moduswechsel tote Abonnements an Singleton-Diensten wie dem
    /// EventAggregator oder dem Benutzerkontext an, weil das alte ViewModel
    /// sonst nie Dispose() bekommt.
    /// </summary>
    private void WechsleModul(NavigationEintrag eintrag)
    {
        var vorherigerScope = _aktuellerModulScope;

        _aktuellerModulScope = _serviceProvider.CreateScope();
        AktuelleAnsicht = eintrag.Modul.ErzeugeStartView(_aktuellerModulScope.ServiceProvider);

        vorherigerScope?.Dispose();
    }

    private void OnBenutzerGewechselt(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(AngemeldeterBenutzer));
        OnPropertyChanged(nameof(RollenAnzeige));
    }

    /// <summary>Wird beim Beenden der Anwendung über den DI-Container aufgerufen (siehe App.xaml.cs).</summary>
    public void Dispose()
    {
        _benutzerKontext.BenutzerGewechselt -= OnBenutzerGewechselt;
        _aktuellerModulScope?.Dispose();
    }
}
