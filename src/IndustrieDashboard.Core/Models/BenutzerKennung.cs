namespace IndustrieDashboard.Core.Models;

/// <summary>
/// Stabile, unveränderliche Kennung eines Benutzers - im Unterschied zum
/// Anzeigenamen niemals für Sicherheitsentscheidungen (z. B. Vier-Augen-Prinzip)
/// durch einen Namensvergleich ersetzbar. Bei Windows die SID als Text
/// (Präfix "proto:" für den Prototyp-Anbieter, später "entra:{tid}:{oid}" für
/// Entra ID). Der Vergleich ist immer ordinal (Standardverhalten von
/// <see cref="string"/>-Gleichheit in einem record struct).
/// </summary>
public readonly record struct BenutzerKennung(string Wert)
{
    public override string ToString() => Wert;
}
