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

    /// <summary>
    /// Je konfigurierter Rolle, ob die Gruppe aufgelöst und die Rolle vergeben
    /// wurde - Grundlage sowohl für den Anmeldung-Audit-Eintrag als auch für
    /// einen Hinweis in der Kopfzeile, falls gar keine Rolle vergeben wurde.
    /// </summary>
    public IReadOnlyList<RollenErmittlungsDetail> Rollenermittlung { get; }

    public event EventHandler? BenutzerGewechselt
    {
        add { }
        remove { }
    }

    public WindowsBenutzerKontext(
        Benutzer aktuellerBenutzer,
        IGruppenPruefer gruppenPruefer,
        RollenGruppenOptions rollenGruppen,
        IAuditLogService auditLogService,
        ILogger<WindowsBenutzerKontext> logger)
    {
        AktuellerBenutzer = aktuellerBenutzer;
        Rollenermittlung = ErmittleRollenDetails(gruppenPruefer, rollenGruppen, logger);
        Rollen = Rollenermittlung.Where(d => d.Vergeben).Select(d => d.Rolle).ToHashSet();

        ProtokolliereAnmeldungAsync(auditLogService).GetAwaiter().GetResult();
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
    public static WindowsBenutzerKontext AusAktuellerAnmeldung(
        RollenGruppenOptions rollenGruppen,
        IAuditLogService auditLogService,
        ILogger<WindowsBenutzerKontext> logger)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var sid = identity.User?.Value
            ?? throw new InvalidOperationException("Die SID der aktuellen Windows-Anmeldung konnte nicht ermittelt werden.");

        var benutzer = new Benutzer(new BenutzerKennung(sid), identity.Name);
        var gruppenPruefer = new WindowsGruppenPruefer(identity);
        return new WindowsBenutzerKontext(benutzer, gruppenPruefer, rollenGruppen, auditLogService, logger);
    }

    private static IReadOnlyList<RollenErmittlungsDetail> ErmittleRollenDetails(
        IGruppenPruefer gruppenPruefer, RollenGruppenOptions rollenGruppen, ILogger logger)
    {
        var details = new List<RollenErmittlungsDetail>();

        foreach (var (rolle, gruppenname) in rollenGruppen.AlleZuordnungen())
        {
            if (string.IsNullOrWhiteSpace(gruppenname))
            {
                details.Add(new RollenErmittlungsDetail(rolle, null, false, "Keine Gruppe konfiguriert"));
                continue;
            }

            if (gruppenPruefer.IstMitglied(gruppenname, out var aufloesungsFehler))
            {
                details.Add(new RollenErmittlungsDetail(rolle, gruppenname, true, null));
            }
            else if (aufloesungsFehler is not null)
            {
                // Fail closed: Eine nicht aufloesbare (oder zu breite) Gruppe
                // vergibt die Rolle nicht, sie wird nur protokolliert -
                // niemals "im Zweifel erlauben".
                logger.LogWarning(
                    "Windows-Gruppe '{Gruppenname}' für Rolle {Rolle} konnte nicht aufgelöst werden ({Grund}); die Rolle wird nicht vergeben.",
                    gruppenname, rolle, aufloesungsFehler);
                details.Add(new RollenErmittlungsDetail(rolle, gruppenname, false, aufloesungsFehler));
            }
            else
            {
                details.Add(new RollenErmittlungsDetail(rolle, gruppenname, false, "Keine Mitgliedschaft"));
            }
        }

        return details;
    }

    /// <summary>
    /// Schreibt einen Audit-Eintrag der Kategorie <see cref="AuditKategorie.Anmeldung"/>
    /// mit Kennung, Anzeigename, vergebenen Rollen und je Rolle dem konfigurierten
    /// Gruppennamen samt Auflösungsergebnis - nicht aufgelöste Gruppen gesondert
    /// vermerkt (Härtung, auf Wunsch von Steven).
    /// </summary>
    private async Task ProtokolliereAnmeldungAsync(IAuditLogService auditLogService)
    {
        var vergebeneRollen = Rollenermittlung.Where(d => d.Vergeben).Select(d => d.Rolle.ToString()).ToList();
        var nichtAufgeloest = Rollenermittlung.Where(d => !d.Vergeben && d.Grund is not null && d.Gruppenname is not null).ToList();

        var zeilen = Rollenermittlung.Select(d => d.Gruppenname is null
            ? $"{d.Rolle}: keine Gruppe konfiguriert"
            : $"{d.Rolle}: Gruppe '{d.Gruppenname}' - {(d.Vergeben ? "aufgelöst, Rolle vergeben" : $"NICHT vergeben ({d.Grund})")}");

        await auditLogService.ProtokolliereAsync(new AuditLogEintrag
        {
            Benutzer = AktuellerBenutzer.Anzeigename,
            BenutzerKennung = AktuellerBenutzer.Kennung.Wert,
            Kategorie = AuditKategorie.Anmeldung,
            Aktion = "Windows-Anmeldung",
            Zielobjekt = "Rollenermittlung",
            NeuerWert = vergebeneRollen.Count > 0 ? string.Join(", ", vergebeneRollen) : "Keine Rolle zugewiesen",
            Begruendung = string.Join("; ", zeilen)
        });
    }
}

/// <summary>Ergebnis der Rollenermittlung für eine einzelne Rolle (siehe <see cref="WindowsBenutzerKontext.Rollenermittlung"/>).</summary>
public sealed record RollenErmittlungsDetail(Rolle Rolle, string? Gruppenname, bool Vergeben, string? Grund);
