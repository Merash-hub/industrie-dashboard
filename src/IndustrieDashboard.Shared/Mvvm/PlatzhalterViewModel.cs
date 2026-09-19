namespace IndustrieDashboard.Shared.Mvvm;

/// <summary>
/// Einfaches ViewModel für Module, die im Prototyp nur als Platzhalter
/// existieren (Grundgerüst inkl. Navigation, aber noch ohne Fachlogik).
/// Wird von den Skeleton-Modulen Schichtplanung, Maschinenüberwachung und
/// Kontrolleingriffe verwendet.
/// </summary>
public class PlatzhalterViewModel : ViewModelBase
{
    public PlatzhalterViewModel(string titel, string beschreibung, IReadOnlyList<string> geplanteFunktionen)
    {
        Titel = titel;
        Beschreibung = beschreibung;
        GeplanteFunktionen = geplanteFunktionen;
    }

    public string Titel { get; }

    public string Beschreibung { get; }

    public IReadOnlyList<string> GeplanteFunktionen { get; }
}
