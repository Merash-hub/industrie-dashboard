using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Shared.Mvvm;

namespace IndustrieDashboard.Modules.Kontrolleingriffe.ViewModels;

/// <summary>
/// Kapselt eine <see cref="KontrolleingriffAnforderung"/> für die Anzeige und
/// berechnet, ob der aktuell angemeldete Benutzer sie freigeben darf
/// (Vier-Augen-Prinzip: nicht dieselbe Person, die angefordert hat).
/// </summary>
public class AnforderungViewModel : ViewModelBase
{
    private const string HinweisNichtFreigebbar =
        "Vier-Augen-Prinzip: Eine Anforderung darf nicht von der Person freigegeben werden, die sie gestellt hat.";

    private readonly KontrolleingriffAnforderung _anforderung;
    private bool _darfFreigeben;

    public AnforderungViewModel(KontrolleingriffAnforderung anforderung, string aktuellerBenutzer)
    {
        _anforderung = anforderung;
        _darfFreigeben = BerechneDarfFreigeben(aktuellerBenutzer);
    }

    public int Id => _anforderung.Id;

    public int MaschineId => _anforderung.MaschineId;

    public string MaschineAnzeige => $"Maschine #{_anforderung.MaschineId}";

    public string Beschreibung => _anforderung.Beschreibung;

    public string AngefordertVon => _anforderung.AngefordertVon;

    public DateTime AngefordertAm => _anforderung.AngefordertAm;

    public bool DarfFreigeben
    {
        get => _darfFreigeben;
        private set => SetProperty(ref _darfFreigeben, value);
    }

    public string FreigabeHinweis => DarfFreigeben ? string.Empty : HinweisNichtFreigebbar;

    /// <summary>Wird bei jedem Benutzerwechsel aus dem ViewModel heraus aufgerufen.</summary>
    public void AktualisiereDarfFreigeben(string aktuellerBenutzer)
    {
        DarfFreigeben = BerechneDarfFreigeben(aktuellerBenutzer);
        OnPropertyChanged(nameof(FreigabeHinweis));
    }

    private bool BerechneDarfFreigeben(string aktuellerBenutzer) => _anforderung.AngefordertVon != aktuellerBenutzer;
}
