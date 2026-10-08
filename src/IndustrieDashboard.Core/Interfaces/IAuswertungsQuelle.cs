using IndustrieDashboard.Core.Models;

namespace IndustrieDashboard.Core.Interfaces;

/// <summary>
/// Liefert verdichtete Anzeigepunkte (Mittelwert/Minimum/Maximum), niemals
/// Rohwerte (Spezifikation Teil B/D5). Grundlage für <c>MaschinenVerlaufsDienst</c>.
/// Im Prototyp verdichtet eine Umsetzung die simulierten Werte im
/// Arbeitsspeicher; später liefert PostgreSQL sie (Teil D, Stufe 2).
///
/// Bewusst minimal gehalten für Teil B (nur "den nächsten Punkt liefern");
/// die volle Auswertungsschnittstelle aus Teil D5 (<c>HoleAggregateSeit</c>,
/// <c>HoleVerlauf</c> je Maschine/Metrik/Zeitraum) ist erst für Stufe 4 geplant.
/// </summary>
public interface IAuswertungsQuelle
{
    /// <summary>
    /// Verdichtet die seit dem letzten Aufruf gesehenen Rohwerte zu einem
    /// Anzeigepunkt. Liefert <c>null</c>, wenn seitdem keine neuen Rohwerte
    /// vorlagen (z. B. unmittelbar nach dem Start).
    /// </summary>
    Anzeigepunkt? ErfasseAktuellenPunkt();
}
