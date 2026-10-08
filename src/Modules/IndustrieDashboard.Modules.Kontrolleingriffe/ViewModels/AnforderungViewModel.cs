using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Kontrolleingriffe.ViewModels;

/// <summary>
/// Kapselt eine <see cref="KontrolleingriffAnforderung"/> für die Anzeige und
/// berechnet, welche Aktionen der aktuell angemeldete Benutzer ausführen darf
/// (Spezifikation A6/A10). Das ist reine Oberflächen-Information: Der Dienst
/// prüft jede Aktion zusätzlich und unabhängig selbst - diese Berechnung
/// bestimmt nur, welche Schaltflächen aktiv sind und welchen Tooltip eine
/// deaktivierte Schaltfläche zeigt.
/// </summary>
public class AnforderungViewModel : ViewModelBase
{
    private readonly KontrolleingriffAnforderung _anforderung;
    private readonly IBenutzerKontext _benutzerKontext;

    private bool _darfFreigeben;
    private bool _darfAblehnen;
    private bool _darfZurueckziehen;
    private bool _darfFreigabeMitZeugeAnfordern;
    private bool _darfAlsZeugeBestaetigen;
    private string _freigabeHinweis = string.Empty;
    private string _ausnahmeGrundEingabe = string.Empty;

    public AnforderungViewModel(KontrolleingriffAnforderung anforderung, IBenutzerKontext benutzerKontext)
    {
        _anforderung = anforderung;
        _benutzerKontext = benutzerKontext;
        Aktualisiere();
    }

    public int Id => _anforderung.Id;

    public int MaschineId => _anforderung.MaschineId;

    public string MaschineAnzeige => $"Maschine #{_anforderung.MaschineId}";

    public string Beschreibung => _anforderung.Beschreibung;

    public string AngefordertVon => _anforderung.AngefordertVon;

    public DateTime AngefordertAm => _anforderung.AngefordertAm;

    public KontrolleingriffStatus Status => _anforderung.Status;

    public string StatusAnzeige => Status switch
    {
        KontrolleingriffStatus.Angefordert => "Angefordert",
        KontrolleingriffStatus.ZeugeAngefragt => "Wartet auf Zeuge",
        KontrolleingriffStatus.ZeugeBestaetigt => "Zeuge hat bestätigt",
        KontrolleingriffStatus.Freigegeben => "Freigegeben",
        KontrolleingriffStatus.Abgelehnt => "Abgelehnt",
        KontrolleingriffStatus.Zurueckgezogen => "Zurückgezogen",
        KontrolleingriffStatus.Ausgefuehrt => "Ausgeführt",
        _ => Status.ToString()
    };

    public bool DarfFreigeben
    {
        get => _darfFreigeben;
        private set => SetProperty(ref _darfFreigeben, value);
    }

    public bool DarfAblehnen
    {
        get => _darfAblehnen;
        private set => SetProperty(ref _darfAblehnen, value);
    }

    /// <summary>Nur die anfordernde Person selbst, nur solange die Anforderung noch offen ist.</summary>
    public bool DarfZurueckziehen
    {
        get => _darfZurueckziehen;
        private set => SetProperty(ref _darfZurueckziehen, value);
    }

    /// <summary>Zeugenpfad, Schritt 1 (Spezifikation A6): nur die anfordernde Instandhaltung selbst.</summary>
    public bool DarfFreigabeMitZeugeAnfordern
    {
        get => _darfFreigabeMitZeugeAnfordern;
        private set => SetProperty(ref _darfFreigabeMitZeugeAnfordern, value);
    }

    /// <summary>Zeugenpfad, Schritt 2: nur eine dritte Person mit Zeugen-Berechtigung (Schichtleitung).</summary>
    public bool DarfAlsZeugeBestaetigen
    {
        get => _darfAlsZeugeBestaetigen;
        private set => SetProperty(ref _darfAlsZeugeBestaetigen, value);
    }

    /// <summary>Tooltip-Grund für die deaktivierte Freigeben-Schaltfläche (Spezifikation A10).</summary>
    public string FreigabeHinweis
    {
        get => _freigabeHinweis;
        private set => SetProperty(ref _freigabeHinweis, value);
    }

    /// <summary>Pflichtgrund-Eingabe für "Freigabe mit Zeuge" (z. B. "Alleinbesetzung").</summary>
    public string AusnahmeGrundEingabe
    {
        get => _ausnahmeGrundEingabe;
        set => SetProperty(ref _ausnahmeGrundEingabe, value);
    }

    /// <summary>Wird nach jedem Laden und bei jedem Benutzerwechsel neu berechnet.</summary>
    public void Aktualisiere()
    {
        var istEigene = string.Equals(
            _anforderung.AngefordertVonKennung,
            _benutzerKontext.AktuellerBenutzer.Kennung.Wert,
            StringComparison.Ordinal);

        var nochOffen = Status is KontrolleingriffStatus.Angefordert
            or KontrolleingriffStatus.ZeugeAngefragt
            or KontrolleingriffStatus.ZeugeBestaetigt;

        var hatFreigebenRecht = _benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben);
        var hatAblehnenRecht = _benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAblehnen);
        var hatZeugeRecht = _benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAlsZeugeBestaetigen);
        var hatAnfordernRecht = _benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAnfordern);

        DarfFreigeben = hatFreigebenRecht && nochOffen && (!istEigene || Status == KontrolleingriffStatus.ZeugeBestaetigt);
        DarfAblehnen = hatAblehnenRecht && nochOffen && !istEigene;
        DarfZurueckziehen = hatAnfordernRecht && istEigene
            && Status is KontrolleingriffStatus.Angefordert or KontrolleingriffStatus.ZeugeAngefragt;
        DarfFreigabeMitZeugeAnfordern = hatFreigebenRecht && istEigene && Status == KontrolleingriffStatus.Angefordert;
        DarfAlsZeugeBestaetigen = hatZeugeRecht && !istEigene && Status == KontrolleingriffStatus.ZeugeAngefragt;

        FreigabeHinweis = BerechneFreigabeHinweis(istEigene, hatFreigebenRecht, nochOffen);
    }

    private string BerechneFreigabeHinweis(bool istEigene, bool hatFreigebenRecht, bool nochOffen)
    {
        if (!nochOffen)
        {
            return string.Empty;
        }

        if (!hatFreigebenRecht)
        {
            return "Keine Berechtigung zum Freigeben (nur Instandhaltung).";
        }

        if (istEigene && Status != KontrolleingriffStatus.ZeugeBestaetigt)
        {
            return "Freigabe durch andere Person nötig (bei Alleinbesetzung: 'Freigabe mit Zeuge').";
        }

        return string.Empty;
    }
}
