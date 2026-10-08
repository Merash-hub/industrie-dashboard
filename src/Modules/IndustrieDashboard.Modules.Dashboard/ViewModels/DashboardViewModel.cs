using System.Collections.ObjectModel;
using System.Windows;
using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Shared.Events;
using IndustrieDashboard.Shared.Mvvm;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace IndustrieDashboard.Modules.Dashboard.ViewModels;

/// <summary>
/// ViewModel der Dashboard-Startseite. Zeigt den aktuellen Zustand aller
/// Maschinen als Kacheln, einen Live-Verlauf der Durchschnittsauslastung und
/// bietet eine Demo für einen Kontrolleingriff nach dem Vier-Augen-Prinzip.
/// </summary>
public class DashboardViewModel : ViewModelBase, IDisposable
{
    private readonly IMaschinenDatenQuelle _maschinenDatenQuelle;
    private readonly IKontrolleingriffService _kontrolleingriffService;
    private readonly IBenutzerKontext _benutzerKontext;
    private readonly IVerlaufsDienst _verlaufsDienst;
    private readonly IEventAggregator _eventAggregator;
    private readonly ObservableCollection<double> _auslastungsVerlauf = new();

    private MaschineViewModel? _ausgewaehlteMaschine;
    private string _statusMeldung = string.Empty;
    private bool _initialisiert;

    public DashboardViewModel(
        IMaschinenDatenQuelle maschinenDatenQuelle,
        IKontrolleingriffService kontrolleingriffService,
        IBenutzerKontext benutzerKontext,
        IVerlaufsDienst verlaufsDienst,
        IEventAggregator eventAggregator)
    {
        _maschinenDatenQuelle = maschinenDatenQuelle;
        _kontrolleingriffService = kontrolleingriffService;
        _benutzerKontext = benutzerKontext;
        _verlaufsDienst = verlaufsDienst;
        _eventAggregator = eventAggregator;

        AuslastungsSerien = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Values = _auslastungsVerlauf,
                Name = "Ø Auslastung",
                Fill = null,
                GeometrySize = 4
            }
        };

        KontrolleingriffAnfordernCommand = new AsyncRelayCommand(
            KontrolleingriffAnfordernAsync,
            () => AusgewaehlteMaschine is not null && DarfKontrolleingriffAnfordern);

        _maschinenDatenQuelle.WertAktualisiert += OnWertAktualisiert;
        _benutzerKontext.BenutzerGewechselt += OnBenutzerGewechselt;
        _verlaufsDienst.VerlaufAktualisiert += OnVerlaufAktualisiert;
    }

    public ObservableCollection<MaschineViewModel> Maschinen { get; } = new();

    public ObservableCollection<ISeries> AuslastungsSerien { get; }

    public MaschineViewModel? AusgewaehlteMaschine
    {
        get => _ausgewaehlteMaschine;
        set
        {
            if (SetProperty(ref _ausgewaehlteMaschine, value))
            {
                KontrolleingriffAnfordernCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMeldung
    {
        get => _statusMeldung;
        private set => SetProperty(ref _statusMeldung, value);
    }

    public bool DarfKontrolleingriffAnfordern => _benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAnfordern);

    /// <summary>Tooltip-Grund, wenn die Schaltfläche wegen fehlender Berechtigung deaktiviert ist (Spezifikation A10).</summary>
    public string KontrolleingriffAnfordernHinweis => DarfKontrolleingriffAnfordern
        ? string.Empty
        : "Keine Berechtigung, einen Kontrolleingriff anzufordern.";

    public AsyncRelayCommand KontrolleingriffAnfordernCommand { get; }

    /// <summary>Wird beim ersten Anzeigen der View aufgerufen (aus dem Code-Behind).</summary>
    public async Task InitialisierenAsync()
    {
        if (_initialisiert)
        {
            return;
        }

        _initialisiert = true;

        var maschinen = await _maschinenDatenQuelle.GetMaschinenAsync();
        foreach (var maschine in maschinen)
        {
            Maschinen.Add(new MaschineViewModel(maschine));
        }

        // Start/Stopp der Überwachung und der Verlaufserfassung laufen auf
        // Anwendungsebene (siehe App.xaml.cs) - beides sind geteilte Singleton-
        // Dienste, die dieses kurzlebige ViewModel nicht steuern darf. Die
        // Momentaufnahme hier zu lesen genügt, damit ein zweites, nach einem
        // Modulwechsel neu erzeugtes DashboardViewModel sofort den bisherigen
        // Verlauf sieht, statt bei null anzufangen (Spezifikation Teil B).
        AktualisiereVerlaufAusDienst();
    }

    private async Task KontrolleingriffAnfordernAsync()
    {
        if (AusgewaehlteMaschine is null)
        {
            return;
        }

        try
        {
            var anforderung = await _kontrolleingriffService.AnfordernAsync(
                AusgewaehlteMaschine.Id,
                $"Not-Stopp für '{AusgewaehlteMaschine.Name}' angefordert (Prototyp-Demo)");

            StatusMeldung = $"Kontrolleingriff #{anforderung.Id} angefordert – wartet auf Freigabe durch eine zweite Person (Vier-Augen-Prinzip).";

            _eventAggregator.Publish(new KontrolleingriffStatusGeaendertEvent(
                anforderung.Id, AusgewaehlteMaschine.Id, anforderung.Status));
        }
        catch (Exception ex)
        {
            StatusMeldung = $"Fehler beim Anfordern des Kontrolleingriffs: {ex.Message}";
        }
    }

    private void OnWertAktualisiert(object? sender, MaschinenwertAktualisiertEventArgs e)
    {
        // Der Timer der simulierten Datenquelle läuft auf einem Threadpool-Thread;
        // UI-Updates müssen auf den Dispatcher-Thread der WPF-Anwendung.
        // Der historische Verlauf kommt nicht mehr von hier, sondern aus dem
        // geteilten IVerlaufsDienst (siehe OnVerlaufAktualisiert) - dieser Tick
        // aktualisiert nur noch die aktuelle Kachel der jeweiligen Maschine.
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var maschine = Maschinen.FirstOrDefault(m => m.Id == e.MaschineId);
            maschine?.Aktualisieren(e.NeuerWert, e.Status);
        });
    }

    /// <summary>
    /// Der Dienst läuft auf einem Threadpool-Thread (Timer); der Wechsel auf
    /// den UI-Thread geschieht bewusst hier im ViewModel, nicht im Dienst
    /// (Spezifikation Teil B).
    /// </summary>
    private void OnVerlaufAktualisiert(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.BeginInvoke(AktualisiereVerlaufAusDienst);
    }

    private void AktualisiereVerlaufAusDienst()
    {
        _auslastungsVerlauf.Clear();
        foreach (var punkt in _verlaufsDienst.Momentaufnahme)
        {
            _auslastungsVerlauf.Add(Math.Round(punkt.Mittelwert, 1));
        }
    }

    private void OnBenutzerGewechselt(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(DarfKontrolleingriffAnfordern));
        OnPropertyChanged(nameof(KontrolleingriffAnfordernHinweis));
        KontrolleingriffAnfordernCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        _maschinenDatenQuelle.WertAktualisiert -= OnWertAktualisiert;
        _benutzerKontext.BenutzerGewechselt -= OnBenutzerGewechselt;
        _verlaufsDienst.VerlaufAktualisiert -= OnVerlaufAktualisiert;
    }
}
