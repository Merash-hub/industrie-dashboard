using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Langlebiger, WPF-freier Anzeigeverlauf (Spezifikation Teil B): hält die
/// letzten Anzeigepunkte in einem Ringpuffer fester Kapazität, unabhängig
/// davon, welches Modul gerade angezeigt wird - ein Modulwechsel überdauert
/// den Verlauf. Dient nur der Anzeige, nicht der dauerhaften Speicherung
/// (siehe Teil D).
/// </summary>
public interface IVerlaufsDienst
{
    /// <summary>Schreibgeschützte Kopie der aktuell gespeicherten Anzeigepunkte, älteste zuerst.</summary>
    IReadOnlyList<Anzeigepunkt> Momentaufnahme { get; }

    /// <summary>Wird ausgelöst, nachdem ein neuer Anzeigepunkt aufgenommen wurde.</summary>
    event EventHandler? VerlaufAktualisiert;

    /// <summary>Lebenszyklus wie die Überwachung: einmal beim Anwendungsstart. Wiederholte Aufrufe sind folgenlos.</summary>
    void StarteErfassung();

    void StoppeErfassung();
}
