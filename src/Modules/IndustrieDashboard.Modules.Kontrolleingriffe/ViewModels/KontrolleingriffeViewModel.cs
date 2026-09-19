using System.Collections.ObjectModel;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Shared.Events;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Kontrolleingriffe.ViewModels;

/// <summary>
/// ViewModel des Kontrolleingriffe-Moduls: Freigabe/Ablehnung offener
/// Anforderungen nach dem Vier-Augen-Prinzip sowie Einsicht in den
/// vollständigen Audit-Trail.
/// </summary>
public class KontrolleingriffeViewModel : ViewModelBase, IDisposable
{
    private const int MaxAuditEintraege = 100;

    private readonly IKontrolleingriffService _kontrolleingriffService;
    private readonly IAuditLogService _auditLogService;
    private readonly IBenutzerKontext _benutzerKontext;
    private readonly IBenutzerWechsel _benutzerWechsel;
    private readonly IEventAggregator _eventAggregator;

    private string _aktuellerBenutzer;
    private AnforderungViewModel? _ausgewaehlteAnforderung;
    private string _statusMeldung = string.Empty;
    private bool _initialisiert;

    public KontrolleingriffeViewModel(
        IKontrolleingriffService kontrolleingriffService,
        IAuditLogService auditLogService,
        IBenutzerKontext benutzerKontext,
        IBenutzerWechsel benutzerWechsel,
        IEventAggregator eventAggregator)
    {
        _kontrolleingriffService = kontrolleingriffService;
        _auditLogService = auditLogService;
        _benutzerKontext = benutzerKontext;
        _benutzerWechsel = benutzerWechsel;
        _eventAggregator = eventAggregator;

        _aktuellerBenutzer = _benutzerKontext.AktuellerBenutzer;

        FreigebenCommand = new AsyncRelayCommand(FreigebenAsync, p => p is AnforderungViewModel);
        AblehnenCommand = new AsyncRelayCommand(AblehnenAsync, p => p is AnforderungViewModel);
        AktualisierenCommand = new AsyncRelayCommand(AktualisierenAsync);

        _benutzerKontext.BenutzerGewechselt += OnBenutzerGewechselt;
        _eventAggregator.Subscribe<KontrolleingriffStatusGeaendertEvent>(OnKontrolleingriffStatusGeaendert);
    }

    public ObservableCollection<AnforderungViewModel> OffeneAnforderungen { get; } = new();

    public ObservableCollection<AuditLogEintrag> AuditEintraege { get; } = new();

    public AnforderungViewModel? AusgewaehlteAnforderung
    {
        get => _ausgewaehlteAnforderung;
        set => SetProperty(ref _ausgewaehlteAnforderung, value);
    }

    /// <summary>
    /// Prototyp: Setzen löst einen Benutzerwechsel über <see cref="IBenutzerWechsel"/>
    /// aus. Der tatsächliche Wert wird über <see cref="IBenutzerKontext.BenutzerGewechselt"/>
    /// zurückgemeldet (siehe <see cref="OnBenutzerGewechselt"/>).
    /// </summary>
    public string AktuellerBenutzer
    {
        get => _aktuellerBenutzer;
        set
        {
            if (value is null || value == _aktuellerBenutzer)
            {
                return;
            }

            _benutzerWechsel.Wechsle(value);
        }
    }

    public IReadOnlyList<string> VerfuegbareBenutzer => _benutzerWechsel.VerfuegbareBenutzer;

    public string StatusMeldung
    {
        get => _statusMeldung;
        private set => SetProperty(ref _statusMeldung, value);
    }

    public bool OffeneAnforderungenVorhanden => OffeneAnforderungen.Count > 0;

    public bool KeineOffenenAnforderungen => OffeneAnforderungen.Count == 0;

    public AsyncRelayCommand FreigebenCommand { get; }

    public AsyncRelayCommand AblehnenCommand { get; }

    public AsyncRelayCommand AktualisierenCommand { get; }

    /// <summary>Wird beim ersten Anzeigen der View aufgerufen (aus dem Code-Behind).</summary>
    public async Task InitialisierenAsync()
    {
        if (_initialisiert)
        {
            return;
        }

        _initialisiert = true;
        await LadenAsync();
    }

    private async Task AktualisierenAsync() => await LadenAsync();

    private async Task LadenAsync()
    {
        var offene = await _kontrolleingriffService.GetOffeneAnforderungenAsync();

        OffeneAnforderungen.Clear();
        foreach (var anforderung in offene.OrderByDescending(a => a.AngefordertAm))
        {
            OffeneAnforderungen.Add(new AnforderungViewModel(anforderung, _aktuellerBenutzer));
        }

        OnPropertyChanged(nameof(OffeneAnforderungenVorhanden));
        OnPropertyChanged(nameof(KeineOffenenAnforderungen));

        var audit = await _auditLogService.GetEintraegeAsync(MaxAuditEintraege);

        AuditEintraege.Clear();
        foreach (var eintrag in audit)
        {
            AuditEintraege.Add(eintrag);
        }
    }

    private async Task FreigebenAsync(object? parameter)
    {
        if (parameter is not AnforderungViewModel anforderungVm)
        {
            return;
        }

        AusgewaehlteAnforderung = anforderungVm;

        try
        {
            var anforderung = await _kontrolleingriffService.FreigebenAsync(anforderungVm.Id, AktuellerBenutzer);

            StatusMeldung = $"Kontrolleingriff #{anforderung.Id} freigegeben.";

            _eventAggregator.Publish(new KontrolleingriffStatusGeaendertEvent(
                anforderung.Id, anforderung.MaschineId, anforderung.Status));

            await LadenAsync();
        }
        catch (Exception ex)
        {
            StatusMeldung = $"Fehler beim Freigeben: {ex.Message}";
        }
    }

    private async Task AblehnenAsync(object? parameter)
    {
        if (parameter is not AnforderungViewModel anforderungVm)
        {
            return;
        }

        AusgewaehlteAnforderung = anforderungVm;

        try
        {
            var anforderung = await _kontrolleingriffService.AblehnenAsync(anforderungVm.Id, AktuellerBenutzer, begruendung: null);

            StatusMeldung = $"Kontrolleingriff #{anforderung.Id} abgelehnt.";

            _eventAggregator.Publish(new KontrolleingriffStatusGeaendertEvent(
                anforderung.Id, anforderung.MaschineId, anforderung.Status));

            await LadenAsync();
        }
        catch (Exception ex)
        {
            StatusMeldung = $"Fehler beim Ablehnen: {ex.Message}";
        }
    }

    private async void OnKontrolleingriffStatusGeaendert(KontrolleingriffStatusGeaendertEvent evt)
    {
        try
        {
            await LadenAsync();
        }
        catch (Exception ex)
        {
            StatusMeldung = $"Fehler beim Aktualisieren nach Statusänderung: {ex.Message}";
        }
    }

    private void OnBenutzerGewechselt(object? sender, EventArgs e)
    {
        _aktuellerBenutzer = _benutzerKontext.AktuellerBenutzer;
        OnPropertyChanged(nameof(AktuellerBenutzer));

        foreach (var anforderung in OffeneAnforderungen)
        {
            anforderung.AktualisiereDarfFreigeben(_aktuellerBenutzer);
        }
    }

    /// <summary>
    /// Meldet die Abonnements wieder ab, sonst sammeln sich bei jedem
    /// Moduswechsel tote Abonnenten an den Singleton-Diensten an.
    /// </summary>
    public void Dispose()
    {
        _benutzerKontext.BenutzerGewechselt -= OnBenutzerGewechselt;
        _eventAggregator.Unsubscribe<KontrolleingriffStatusGeaendertEvent>(OnKontrolleingriffStatusGeaendert);
    }
}
