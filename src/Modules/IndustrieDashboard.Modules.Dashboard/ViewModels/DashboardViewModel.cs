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
    private const int MaxVerlaufsPunkte = 30;

    private readonly IMaschinenDatenQuelle _maschinenDatenQuelle;
    private readonly IKontrolleingriffService _kontrolleingriffService;
    private readonly IBenutzerKontext _benutzerKontext;
    private readonly IEventAggregator _eventAggregator;
    private readonly ObservableCollection<double> _auslastungsVerlauf = new();

    private MaschineViewModel? _ausgewaehlteMaschine;
    private string _statusMeldung = string.Empty;
    private bool _initialisiert;

    public DashboardViewModel(
        IMaschinenDatenQuelle maschinenDatenQuelle,
        IKontrolleingriffService kontrolleingriffService,
        IBenutzerKontext benutzerKontext,
        IEventAggregator eventAggregator)
    {
        _maschinenDatenQuelle = maschinenDatenQuelle;
        _kontrolleingriffService = kontrolleingriffService;
        _benutzerKontext = benutzerKontext;
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

        // Start/Stopp der Überwachung laufen auf Anwendungsebene (siehe
        // App.xaml.cs) - IMaschinenDatenQuelle ist ein geteilter Singleton-
        // Dienst, den dieses kurzlebige ViewModel nicht steuern darf.
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
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var maschine = Maschinen.FirstOrDefault(m => m.Id == e.MaschineId);
            maschine?.Aktualisieren(e.NeuerWert, e.Status);

            if (Maschinen.Count > 0)
            {
                var durchschnitt = Maschinen.Average(m => m.AktuellerWert);
                _auslastungsVerlauf.Add(Math.Round(durchschnitt, 1));
                while (_auslastungsVerlauf.Count > MaxVerlaufsPunkte)
                {
                    _auslastungsVerlauf.RemoveAt(0);
                }
            }
        });
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
    }
}
