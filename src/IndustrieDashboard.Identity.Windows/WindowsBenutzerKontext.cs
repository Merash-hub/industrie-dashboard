using System.Security.Principal;
using IndustrieDashboard.Core.Autorisierung;
using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using Microsoft.Extensions.Logging;

namespace IndustrieDashboard.Identity.Windows;

/// <summary>
/// Windows-Anbieter von <see cref="IBenutzerKontext"/> (Spezifikation A7):
/// Kennung ist die SID der aktuellen Windows-Anmeldung, Anzeigename das
/// Format <c>DOMAENE\benutzer</c>. Rollen werden einmal bei der Erzeugung aus
/// den konfigurierten Gruppen ermittelt (<see cref="IGruppenPruefer"/>) und
/// bleiben für die Lebensdauer der Instanz fest - Änderungen an der
/// Gruppenmitgliedschaft wirken erst nach einem Neustart der Anwendung.
/// Deshalb wechselt der Benutzer hier nie zur Laufzeit: <see cref="BenutzerGewechselt"/>
/// wird nie ausgelöst.
/// </summary>
public sealed class WindowsBenutzerKontext : IBenutzerKontext
{
    public Benutzer AktuellerBenutzer { get; }

    public IReadOnlySet<Rolle> Rollen { get; }

    public event EventHandler? BenutzerGewechselt
    {
        add { }
        remove { }
    }

    public WindowsBenutzerKontext(Benutzer aktuellerBenutzer, IGruppenPruefer gruppenPruefer, RollenGruppenOptions rollenGruppen, ILogger<WindowsBenutzerKontext> logger)
    {
        AktuellerBenutzer = aktuellerBenutzer;
        Rollen = ErmittleRollen(gruppenPruefer, rollenGruppen, logger);
    }

    public bool HatBerechtigung(Berechtigung berechtigung) => RollenBerechtigungen.Hat(Rollen, berechtigung);

    /// <summary>
    /// Baut den Kontext für die aktuelle Windows-Anmeldung
    /// (<see cref="WindowsIdentity.GetCurrent"/>): einziger Berührungspunkt
    /// mit der echten Windows-API in diesem Typ. Rollen werden noch
    /// innerhalb dieser Methode (und damit vor dem Entsorgen der Identität)
    /// vollständig ermittelt - der Rest des Typs arbeitet nur noch mit der
    /// bereits extrahierten <see cref="Benutzer"/>-Kennung und ist dadurch
    /// ohne echte Windows-Anmeldung testbar.
    /// </summary>
    public static WindowsBenutzerKontext AusAktuellerAnmeldung(RollenGruppenOptions rollenGruppen, ILogger<WindowsBenutzerKontext> logger)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User?.Value
            ?? throw new InvalidOperationException("Die SID der aktuellen Windows-Anmeldung konnte nicht ermittelt werden.");

        var benutzer = new Benutzer(new BenutzerKennung(sid), identity.Name);
        var gruppenPruefer = new WindowsGruppenPruefer(identity);
        return new WindowsBenutzerKontext(benutzer, gruppenPruefer, rollenGruppen, logger);
    }

    private static IReadOnlySet<Rolle> ErmittleRollen(IGruppenPruefer gruppenPruefer, RollenGruppenOptions rollenGruppen, ILogger logger)
    {
        var rollen = new HashSet<Rolle>();

        foreach (var (rolle, gruppenname) in rollenGruppen.AlleZuordnungen())
        {
            if (string.IsNullOrWhiteSpace(gruppenname))
            {
                continue;
            }

            if (gruppenPruefer.IstMitglied(gruppenname, out var aufloesungsFehler))
            {
                rollen.Add(rolle);
            }
            else if (aufloesungsFehler is not null)
            {
                // Fail closed: Eine nicht aufloesbare Gruppe vergibt die Rolle
                // nicht, sie wird nur protokolliert - niemals "im Zweifel erlauben".
                logger.LogWarning(
                    "Windows-Gruppe '{Gruppenname}' für Rolle {Rolle} konnte nicht aufgelöst werden ({Grund}); die Rolle wird nicht vergeben.",
                    gruppenname, rolle, aufloesungsFehler);
            }
        }

        return rollen;
    }
}
