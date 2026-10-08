using System.Collections.ObjectModel;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Shared.Events;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Kontrolleingriffe.ViewModels;

/// <summary>
/// ViewModel des Kontrolleingriffe-Moduls: Anfordern/Zurückziehen,
/// Freigabe/Ablehnung nach dem Vier-Augen-Prinzip, der Zeugenpfad bei
/// Alleinbesetzung der Instandhaltung (Spezifikation A6) sowie Einsicht in
/// den vollständigen Audit-Trail.
/// </summary>
public class KontrolleingriffeViewModel : ViewModelBase, IDisposable
{
    private const int MaxAuditEintraege = 100;

    private readonly IKontrolleingriffService _kontrolleingriffService;
    private readonly IAuditLogService _auditLogService;
    private readonly IBenutzerKontext _benutzerKontext;
    private readonly IBenutzerWechsel? _benutzerWechsel;
    private readonly IEventAggregator _eventAggregator;

    private string _aktuellerBenutzer;
    private AnforderungViewModel? _ausgewaehlteAnforderung;
    private string _statusMeldung = string.Empty;
    private bool _initialisiert;

    public KontrolleingriffeViewModel(
        IKontrolleingriffService kontrolleingriffService,
        IAuditLogService auditLogService,
        IBenutzerKontext benutzerKontext,
        IEventAggregator eventAggregator,
        IBenutzerWechsel? benutzerWechsel = null)
    {
        _kontrolleingriffService = kontrolleingriffService;
        _auditLogService = auditLogService;
        _benutzerKontext = benutzerKontext;
        _benutzerWechsel = benutzerWechsel;
        _eventAggregator = eventAggregator;

        _aktuellerBenutzer = _benutzerKontext.AktuellerBenutzer.Anzeigename;

        FreigebenCommand = new AsyncRelayCommand(FreigebenAsync, p => p is AnforderungViewModel);
        AblehnenCommand = new AsyncRelayCommand(AblehnenAsync, p => p is AnforderungViewModel);
        ZurueckziehenCommand = new AsyncRelayCommand(ZurueckziehenAsync, p => p is AnforderungViewModel);
        FreigabeMitZeugeCommand = new AsyncRelayCommand(FreigabeMitZeugeAsync, p => p is AnforderungViewModel);
        AlsZeugeBestaetigenCommand = new AsyncRelayCommand(AlsZeugeBestaetigenAsync, p => p is AnforderungViewModel);
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

            _benutzerWechsel?.Wechsle(value);
        }
    }

    /// <summary>
    /// Leer, wenn kein Prototyp-Anbieter aktiv ist - die Benutzerwechsel-
    /// Oberfläche erscheint dann gar nicht erst (Spezifikation A8).
    /// </summary>
    public IReadOnlyList<string> VerfuegbareBenutzer => _benutzerWechsel?.VerfuegbareBenutzer ?? Array.Empty<string>();

    public bool BenutzerwechselVerfuegbar => _benutzerWechsel is not null;

    public string StatusMeldung
    {
        get => _statusMeldung;
        private set => SetProperty(ref _statusMeldung, value);
    }

    public bool OffeneAnforderungenVorhanden => OffeneAnforderungen.Count > 0;

    public bool KeineOffenenAnforderungen => OffeneAnforderungen.Count == 0;

    public AsyncRelayCommand FreigebenCommand { get; }

    public AsyncRelayCommand AblehnenCommand { get; }

    public AsyncRelayCommand ZurueckziehenCommand { get; }

    public AsyncRelayCommand FreigabeMitZeugeCommand { get; }

    public AsyncRelayCommand AlsZeugeBestaetigenCommand { get; }

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
            OffeneAnforderungen.Add(new AnforderungViewModel(anforderung, _benutzerKontext));
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

    private async Task FreigebenAsync(object? parameter) =>
        await AktionAusfuehrenAsync(parameter, "freigegeben", "Freigeben",
            anforderungVm => _kontrolleingriffService.FreigebenAsync(anforderungVm.Id));

    private async Task AblehnenAsync(object? parameter) =>
        await AktionAusfuehrenAsync(parameter, "abgelehnt", "Ablehnen",
            anforderungVm => _kontrolleingriffService.AblehnenAsync(anforderungVm.Id, begruendung: null));

    private async Task ZurueckziehenAsync(object? parameter) =>
        await AktionAusfuehrenAsync(parameter, "zurückgezogen", "Zurückziehen",
            anforderungVm => _kontrolleingriffService.ZurueckziehenAsync(anforderungVm.Id));

    private async Task FreigabeMitZeugeAsync(object? parameter) =>
        await AktionAusfuehrenAsync(parameter, "mit Zeuge angefordert", "Freigabe mit Zeuge anfordern",
            anforderungVm => _kontrolleingriffService.FreigabeMitZeugeAnfordernAsync(anforderungVm.Id, anforderungVm.AusnahmeGrundEingabe));

    private async Task AlsZeugeBestaetigenAsync(object? parameter) =>
        await AktionAusfuehrenAsync(parameter, "als Zeuge bestätigt", "Zeugenbestätigung",
            anforderungVm => _kontrolleingriffService.AlsZeugeBestaetigenAsync(anforderungVm.Id));

    private async Task AktionAusfuehrenAsync(
        object? parameter,
        string vergangenheitsform,
        string fehlerBezeichnung,
        Func<AnforderungViewModel, Task<KontrolleingriffAnforderung>> aktion)
    {
        if (parameter is not AnforderungViewModel anforderungVm)
        {
            return;
        }

        AusgewaehlteAnforderung = anforderungVm;

        try
        {
            var anforderung = await aktion(anforderungVm);

            StatusMeldung = $"Kontrolleingriff #{anforderung.Id} {vergangenheitsform}.";

            _eventAggregator.Publish(new KontrolleingriffStatusGeaendertEvent(
                anforderung.Id, anforderung.MaschineId, anforderung.Status));

            await LadenAsync();
        }
        catch (Exception ex)
        {
            StatusMeldung = $"Fehler bei '{fehlerBezeichnung}': {ex.Message}";
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
        _aktuellerBenutzer = _benutzerKontext.AktuellerBenutzer.Anzeigename;
        OnPropertyChanged(nameof(AktuellerBenutzer));

        foreach (var anforderung in OffeneAnforderungen)
        {
            anforderung.Aktualisiere();
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
