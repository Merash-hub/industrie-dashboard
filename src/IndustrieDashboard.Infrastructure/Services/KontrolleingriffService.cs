using IndustrieDashboard.Core.Enums;
using IndustrieDashboard.Core.Exceptions;
using IndustrieDashboard.Core.Interfaces;
using IndustrieDashboard.Core.Models;
using IndustrieDashboard.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndustrieDashboard.Infrastructure.Services;

/// <summary>
/// Referenzimplementierung des Vier-Augen-Prinzips und des Zeugenpfads
/// (Spezifikation A6). Wer handelt, kommt ausschließlich aus dem injizierten
/// <see cref="IBenutzerKontext"/> - der Dienst vertraut dabei nie einem von
/// außen übergebenen Namen, sonst ließe sich die Prüfung über die Oberfläche
/// umgehen ("Zero Trust: explizit prüfen"). Jede Zustandsänderung und jede
/// Verweigerung landet in derselben Transaktion wie der zugehörige
/// Audit-Eintrag: Anforderung und Audit-Eintrag werden über denselben
/// <see cref="AppDbContext"/> verfolgt und mit einem gemeinsamen
/// <c>SaveChangesAsync</c> gespeichert, was EF Core intern bereits atomar in
/// einer Transaktion ausführt.
/// </summary>
public class KontrolleingriffService : IKontrolleingriffService
{
    private static readonly TimeSpan StandardZeugenBestaetigungGueltigkeit = TimeSpan.FromMinutes(15);

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IBenutzerKontext _benutzerKontext;
    private readonly TimeSpan _zeugenBestaetigungGueltigkeit;

    public KontrolleingriffService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IBenutzerKontext benutzerKontext,
        TimeSpan? zeugenBestaetigungGueltigkeit = null)
    {
        _dbContextFactory = dbContextFactory;
        _benutzerKontext = benutzerKontext;
        _zeugenBestaetigungGueltigkeit = zeugenBestaetigungGueltigkeit ?? StandardZeugenBestaetigungGueltigkeit;
    }

    public async Task<KontrolleingriffAnforderung> AnfordernAsync(int maschineId, string beschreibung, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;
        var zielobjekt = $"Maschine #{maschineId}";

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAnfordern))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff angefordert", zielobjekt,
                "Keine Berechtigung, einen Kontrolleingriff anzufordern.",
                grund => new NichtBerechtigtException(grund), ct);
        }

        var anforderung = new KontrolleingriffAnforderung
        {
            MaschineId = maschineId,
            Beschreibung = beschreibung,
            AngefordertVon = aktueller.Anzeigename,
            AngefordertVonKennung = aktueller.Kennung.Wert,
            Status = KontrolleingriffStatus.Angefordert
        };

        db.KontrolleingriffAnforderungen.Add(anforderung);
        db.AuditLogEintraege.Add(new AuditLogEintrag
        {
            Benutzer = aktueller.Anzeigename,
            BenutzerKennung = aktueller.Kennung.Wert,
            Kategorie = AuditKategorie.Kontrolleingriff,
            Aktion = "Kontrolleingriff angefordert",
            Zielobjekt = zielobjekt,
            NeuerWert = beschreibung
        });
        await db.SaveChangesAsync(ct);

        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> FreigebenAsync(int anforderungId, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        var anforderung = await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff freigegeben", zielobjekt,
                "Keine Berechtigung zum Freigeben (nur Instandhaltung).",
                grund => new NichtBerechtigtException(grund), ct);
        }

        if (anforderung.Status is KontrolleingriffStatus.Freigegeben or KontrolleingriffStatus.Abgelehnt)
        {
            throw await VerweigereAsync(db, "Kontrolleingriff freigegeben", zielobjekt,
                $"Anforderung #{anforderungId} ist bereits abgeschlossen (Status {anforderung.Status}); kein doppeltes Freigeben.",
                grund => new InvalidOperationException(grund), ct);
        }

        var istSelbstfreigabe = string.Equals(anforderung.AngefordertVonKennung, aktueller.Kennung.Wert, StringComparison.Ordinal);

        if (istSelbstfreigabe)
        {
            // Zeugenpfad (A6): Selbstfreigabe ist nur erlaubt, wenn zuvor eine
            // dritte Person als Zeuge bestätigt hat und das noch nicht abgelaufen ist.
            if (anforderung.Status != KontrolleingriffStatus.ZeugeBestaetigt)
            {
                throw await VerweigereAsync(db, "Kontrolleingriff freigegeben", zielobjekt,
                    "Vier-Augen-Prinzip verletzt: Selbstfreigabe ist nur nach Zeugenbestätigung möglich (siehe 'Freigabe mit Zeuge').",
                    grund => new VierAugenVerletzungException(grund), ct);
            }

            if (anforderung.ZeugeBestaetigtAm is null
                || DateTime.UtcNow - anforderung.ZeugeBestaetigtAm.Value > _zeugenBestaetigungGueltigkeit)
            {
                throw await VerweigereAsync(db, "Kontrolleingriff freigegeben", zielobjekt,
                    "Die Zeugenbestätigung ist abgelaufen; die Selbstfreigabe muss erneut mit Zeuge angefordert werden.",
                    grund => new VierAugenVerletzungException(grund), ct);
            }
        }

        anforderung.Status = KontrolleingriffStatus.Freigegeben;
        anforderung.FreigegebenVon = aktueller.Anzeigename;
        anforderung.FreigegebenVonKennung = aktueller.Kennung.Wert;
        anforderung.FreigegebenAm = DateTime.UtcNow;

        if (istSelbstfreigabe)
        {
            db.AuditLogEintraege.Add(new AuditLogEintrag
            {
                Benutzer = aktueller.Anzeigename,
                BenutzerKennung = aktueller.Kennung.Wert,
                Kategorie = AuditKategorie.FreigabeMitZeuge,
                Aktion = "Kontrolleingriff freigegeben (Zeugenpfad, Alleinbesetzung)",
                Zielobjekt = zielobjekt,
                AlterWert = "ZeugeBestaetigt",
                NeuerWert =
                    $"Freigegeben. AngefordertVonKennung={anforderung.AngefordertVonKennung}; " +
                    $"FreigegebenVonKennung={anforderung.FreigegebenVonKennung}; ZeugeKennung={anforderung.ZeugeKennung}",
                Begruendung = anforderung.AusnahmeGrund,
                GegengezeichnetVon = $"Zeuge: {anforderung.ZeugeAnzeigename} ({anforderung.ZeugeKennung})"
            });
        }
        else
        {
            db.AuditLogEintraege.Add(new AuditLogEintrag
            {
                Benutzer = aktueller.Anzeigename,
                BenutzerKennung = aktueller.Kennung.Wert,
                Kategorie = AuditKategorie.Kontrolleingriff,
                Aktion = "Kontrolleingriff freigegeben",
                Zielobjekt = zielobjekt,
                AlterWert = "Angefordert",
                NeuerWert = "Freigegeben",
                GegengezeichnetVon = aktueller.Anzeigename
            });
        }

        await db.SaveChangesAsync(ct);
        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> AblehnenAsync(int anforderungId, string? begruendung, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        var anforderung = await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAblehnen))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff abgelehnt", zielobjekt,
                "Keine Berechtigung zum Ablehnen (nur Instandhaltung).",
                grund => new NichtBerechtigtException(grund), ct);
        }

        if (anforderung.Status is KontrolleingriffStatus.Freigegeben or KontrolleingriffStatus.Abgelehnt)
        {
            throw await VerweigereAsync(db, "Kontrolleingriff abgelehnt", zielobjekt,
                $"Anforderung #{anforderungId} ist bereits abgeschlossen (Status {anforderung.Status}).",
                grund => new InvalidOperationException(grund), ct);
        }

        if (string.Equals(anforderung.AngefordertVonKennung, aktueller.Kennung.Wert, StringComparison.Ordinal))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff abgelehnt", zielobjekt,
                "Vier-Augen-Prinzip verletzt: Anfordernde und ablehnende Person müssen unterschiedlich sein.",
                grund => new VierAugenVerletzungException(grund), ct);
        }

        var vorherigerStatus = anforderung.Status;
        anforderung.Status = KontrolleingriffStatus.Abgelehnt;

        db.AuditLogEintraege.Add(new AuditLogEintrag
        {
            Benutzer = aktueller.Anzeigename,
            BenutzerKennung = aktueller.Kennung.Wert,
            Kategorie = AuditKategorie.Kontrolleingriff,
            Aktion = "Kontrolleingriff abgelehnt",
            Zielobjekt = zielobjekt,
            AlterWert = vorherigerStatus.ToString(),
            NeuerWert = "Abgelehnt",
            Begruendung = begruendung
        });

        await db.SaveChangesAsync(ct);
        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> ZurueckziehenAsync(int anforderungId, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        var anforderung = await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAnfordern))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff zurückgezogen", zielobjekt,
                "Keine Berechtigung, einen Kontrolleingriff anzufordern bzw. zurückzuziehen.",
                grund => new NichtBerechtigtException(grund), ct);
        }

        // Bewusst keine Vier-Augen-Prüfung: Zurückziehen ist die Rücknahme der
        // eigenen, noch nicht entschiedenen Anforderung, kein Freigeben/Ablehnen.
        if (!string.Equals(anforderung.AngefordertVonKennung, aktueller.Kennung.Wert, StringComparison.Ordinal))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff zurückgezogen", zielobjekt,
                "Nur die anfordernde Person selbst darf ihre eigene Anforderung zurückziehen.",
                grund => new NichtBerechtigtException(grund), ct);
        }

        if (anforderung.Status is not (KontrolleingriffStatus.Angefordert or KontrolleingriffStatus.ZeugeAngefragt))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff zurückgezogen", zielobjekt,
                $"Anforderung #{anforderungId} ist nicht mehr offen (Status {anforderung.Status}) und kann nicht mehr zurückgezogen werden.",
                grund => new InvalidOperationException(grund), ct);
        }

        var vorherigerStatus = anforderung.Status;
        anforderung.Status = KontrolleingriffStatus.Zurueckgezogen;

        db.AuditLogEintraege.Add(new AuditLogEintrag
        {
            Benutzer = aktueller.Anzeigename,
            BenutzerKennung = aktueller.Kennung.Wert,
            Kategorie = AuditKategorie.Kontrolleingriff,
            Aktion = "Kontrolleingriff zurückgezogen",
            Zielobjekt = zielobjekt,
            AlterWert = vorherigerStatus.ToString(),
            NeuerWert = "Zurückgezogen"
        });

        await db.SaveChangesAsync(ct);
        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> FreigabeMitZeugeAnfordernAsync(int anforderungId, string ausnahmeGrund, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        var anforderung = await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben))
        {
            throw await VerweigereAsync(db, "Freigabe mit Zeuge angefordert", zielobjekt,
                "Keine Berechtigung zum Freigeben; nur Instandhaltung darf den Zeugenpfad nutzen.",
                grund => new NichtBerechtigtException(grund), ct);
        }

        if (!string.Equals(anforderung.AngefordertVonKennung, aktueller.Kennung.Wert, StringComparison.Ordinal))
        {
            throw await VerweigereAsync(db, "Freigabe mit Zeuge angefordert", zielobjekt,
                "Der Zeugenpfad gilt nur für die Selbstfreigabe der eigenen Anforderung.",
                grund => new VierAugenVerletzungException(grund), ct);
        }

        if (anforderung.Status != KontrolleingriffStatus.Angefordert)
        {
            throw await VerweigereAsync(db, "Freigabe mit Zeuge angefordert", zielobjekt,
                $"Anforderung #{anforderungId} ist nicht mehr im Status 'Angefordert' (aktuell: {anforderung.Status}).",
                grund => new InvalidOperationException(grund), ct);
        }

        if (string.IsNullOrWhiteSpace(ausnahmeGrund))
        {
            // Kein eigener Ausnahmefall in der Spezifikation benannt; da der
            // Pflichtgrund Teil der Zeugenpfad-Integritaet ist, ordnen wir ihn
            // wie die anderen Zeugenpfad-Verstoesse als VierAugenVerletzung ein.
            throw await VerweigereAsync(db, "Freigabe mit Zeuge angefordert", zielobjekt,
                "Für die Freigabe mit Zeuge ist ein Grund Pflicht (z. B. 'Alleinbesetzung').",
                grund => new VierAugenVerletzungException(grund), ct);
        }

        anforderung.Status = KontrolleingriffStatus.ZeugeAngefragt;
        anforderung.AusnahmeGrund = ausnahmeGrund;

        db.AuditLogEintraege.Add(new AuditLogEintrag
        {
            Benutzer = aktueller.Anzeigename,
            BenutzerKennung = aktueller.Kennung.Wert,
            Kategorie = AuditKategorie.Kontrolleingriff,
            Aktion = "Freigabe mit Zeuge angefordert",
            Zielobjekt = zielobjekt,
            Begruendung = ausnahmeGrund
        });

        await db.SaveChangesAsync(ct);
        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> AlsZeugeBestaetigenAsync(int anforderungId, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        var anforderung = await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAlsZeugeBestaetigen))
        {
            throw await VerweigereAsync(db, "Als Zeuge bestätigt", zielobjekt,
                "Keine Berechtigung, als Zeuge zu bestätigen (nur Schichtleitung).",
                grund => new NichtBerechtigtException(grund), ct);
        }

        if (anforderung.Status != KontrolleingriffStatus.ZeugeAngefragt)
        {
            throw await VerweigereAsync(db, "Als Zeuge bestätigt", zielobjekt,
                $"Keine offene Zeugenanfrage (aktueller Status: {anforderung.Status}).",
                grund => new InvalidOperationException(grund), ct);
        }

        if (string.Equals(anforderung.AngefordertVonKennung, aktueller.Kennung.Wert, StringComparison.Ordinal))
        {
            throw await VerweigereAsync(db, "Als Zeuge bestätigt", zielobjekt,
                "Der Zeuge muss eine dritte, von der anfordernden Person verschiedene Person sein.",
                grund => new VierAugenVerletzungException(grund), ct);
        }

        anforderung.Status = KontrolleingriffStatus.ZeugeBestaetigt;
        anforderung.ZeugeKennung = aktueller.Kennung.Wert;
        anforderung.ZeugeAnzeigename = aktueller.Anzeigename;
        anforderung.ZeugeBestaetigtAm = DateTime.UtcNow;

        db.AuditLogEintraege.Add(new AuditLogEintrag
        {
            Benutzer = aktueller.Anzeigename,
            BenutzerKennung = aktueller.Kennung.Wert,
            Kategorie = AuditKategorie.ZeugeBestaetigt,
            Aktion = "Als Zeuge bestätigt",
            Zielobjekt = zielobjekt,
            Begruendung = anforderung.AusnahmeGrund
        });

        await db.SaveChangesAsync(ct);
        return anforderung;
    }

    public async Task<IReadOnlyList<KontrolleingriffAnforderung>> GetOffeneAnforderungenAsync(CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await db.KontrolleingriffAnforderungen
            .Where(a => a.Status == KontrolleingriffStatus.Angefordert
                || a.Status == KontrolleingriffStatus.ZeugeAngefragt
                || a.Status == KontrolleingriffStatus.ZeugeBestaetigt)
            .OrderBy(a => a.AngefordertAm)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<KontrolleingriffAnforderung>> GetAlleAnforderungenAsync(int maxAnzahl = 100, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await db.KontrolleingriffAnforderungen
            .OrderByDescending(a => a.AngefordertAm)
            .Take(maxAnzahl)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Schreibt einen <see cref="AuditKategorie.ZugriffVerweigert"/>-Eintrag
    /// über denselben <paramref name="db"/>-Kontext und speichert ihn sofort,
    /// damit die Verweigerung auch dann sicher protokolliert ist, wenn direkt
    /// danach die zurückgegebene Ausnahme geworfen wird.
    /// </summary>
    private async Task<Exception> VerweigereAsync(
        AppDbContext db,
        string aktion,
        string zielobjekt,
        string grund,
        Func<string, Exception> ausnahmeErzeugen,
        CancellationToken ct)
    {
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        db.AuditLogEintraege.Add(new AuditLogEintrag
        {
            Benutzer = aktueller.Anzeigename,
            BenutzerKennung = aktueller.Kennung.Wert,
            Kategorie = AuditKategorie.ZugriffVerweigert,
            Aktion = aktion,
            Zielobjekt = zielobjekt,
            Begruendung = grund
        });
        await db.SaveChangesAsync(ct);

        return ausnahmeErzeugen(grund);
    }
}
