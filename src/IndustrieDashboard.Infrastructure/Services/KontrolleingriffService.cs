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
///
/// Reihenfolge in jeder Aktion: Zuerst die generische Berechtigung prüfen,
/// erst danach die Anforderung per Id laden. So erfährt eine Person ohne
/// jede Berechtigung nie, ob eine bestimmte Anforderungsnummer überhaupt
/// existiert (Härtung).
/// </summary>
public class KontrolleingriffService : IKontrolleingriffService
{
    private static readonly TimeSpan StandardZeugenBestaetigungGueltigkeit = TimeSpan.FromMinutes(15);

    /// <summary>Härtung: Freitext-Felder werden abgelehnt, nicht abgeschnitten, wenn sie das überschreiten.</summary>
    private const int MaxLaengeText = 500;

    /// <summary>Härtung: Der Pflichtgrund für die Freigabe mit Zeuge muss inhaltlich sein, nicht nur nicht-leer.</summary>
    private const int MindestlaengeAusnahmeGrund = 5;

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
        PruefeLaenge(beschreibung, nameof(beschreibung), "Die Beschreibung");

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
        // Anlegen ist ein reines INSERT: kein bestehender Concurrency-Token,
        // an dem ein Konflikt auftreten könnte.
        await db.SaveChangesAsync(ct);

        return anforderung;
    }

    public async Task<KontrolleingriffAnforderung> FreigebenAsync(int anforderungId, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff freigegeben", $"Anforderung #{anforderungId}",
                "Keine Berechtigung zum Freigeben (nur Instandhaltung).",
                grund => new NichtBerechtigtException(grund), ct);
        }

        var anforderung = await LadeAsync(db, anforderungId, ct);
        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

        if (IstAbgeschlossen(anforderung.Status))
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

        return await SpeichereMitGleichzeitigkeitsschutzAsync(db, anforderung, "Kontrolleingriff freigegeben", zielobjekt, ct);
    }

    public async Task<KontrolleingriffAnforderung> AblehnenAsync(int anforderungId, string? begruendung, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAblehnen))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff abgelehnt", $"Anforderung #{anforderungId}",
                "Keine Berechtigung zum Ablehnen (nur Instandhaltung).",
                grund => new NichtBerechtigtException(grund), ct);
        }

        var anforderung = await LadeAsync(db, anforderungId, ct);
        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

        if (IstAbgeschlossen(anforderung.Status))
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

        return await SpeichereMitGleichzeitigkeitsschutzAsync(db, anforderung, "Kontrolleingriff abgelehnt", zielobjekt, ct);
    }

    public async Task<KontrolleingriffAnforderung> ZurueckziehenAsync(int anforderungId, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAnfordern))
        {
            throw await VerweigereAsync(db, "Kontrolleingriff zurückgezogen", $"Anforderung #{anforderungId}",
                "Keine Berechtigung, einen Kontrolleingriff anzufordern bzw. zurückzuziehen.",
                grund => new NichtBerechtigtException(grund), ct);
        }

        var anforderung = await LadeAsync(db, anforderungId, ct);
        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

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

        return await SpeichereMitGleichzeitigkeitsschutzAsync(db, anforderung, "Kontrolleingriff zurückgezogen", zielobjekt, ct);
    }

    public async Task<KontrolleingriffAnforderung> FreigabeMitZeugeAnfordernAsync(int anforderungId, string ausnahmeGrund, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffFreigeben))
        {
            throw await VerweigereAsync(db, "Freigabe mit Zeuge angefordert", $"Anforderung #{anforderungId}",
                "Keine Berechtigung zum Freigeben; nur Instandhaltung darf den Zeugenpfad nutzen.",
                grund => new NichtBerechtigtException(grund), ct);
        }

        var anforderung = await LadeAsync(db, anforderungId, ct);
        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

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

        // Pflichtgrund-Prüfungen bewusst alle als VierAugenVerletzung: Sie
        // gehören zusammen zur Integrität des Zeugenpfads (ein Grund muss da
        // UND inhaltlich UND nicht überlang sein), nicht zu allgemeiner
        // Eingabevalidierung.
        var getrimmterGrund = ausnahmeGrund?.Trim() ?? string.Empty;

        if (getrimmterGrund.Length < MindestlaengeAusnahmeGrund)
        {
            throw await VerweigereAsync(db, "Freigabe mit Zeuge angefordert", zielobjekt,
                $"Für die Freigabe mit Zeuge ist ein Grund mit mindestens {MindestlaengeAusnahmeGrund} Zeichen Pflicht (z. B. 'Alleinbesetzung').",
                grund => new VierAugenVerletzungException(grund), ct);
        }

        if (getrimmterGrund.Length > MaxLaengeText)
        {
            throw await VerweigereAsync(db, "Freigabe mit Zeuge angefordert", zielobjekt,
                $"Der Grund für die Freigabe mit Zeuge darf höchstens {MaxLaengeText} Zeichen lang sein.",
                grund => new VierAugenVerletzungException(grund), ct);
        }

        anforderung.Status = KontrolleingriffStatus.ZeugeAngefragt;
        anforderung.AusnahmeGrund = getrimmterGrund;

        db.AuditLogEintraege.Add(new AuditLogEintrag
        {
            Benutzer = aktueller.Anzeigename,
            BenutzerKennung = aktueller.Kennung.Wert,
            Kategorie = AuditKategorie.Kontrolleingriff,
            Aktion = "Freigabe mit Zeuge angefordert",
            Zielobjekt = zielobjekt,
            Begruendung = getrimmterGrund
        });

        return await SpeichereMitGleichzeitigkeitsschutzAsync(db, anforderung, "Freigabe mit Zeuge angefordert", zielobjekt, ct);
    }

    public async Task<KontrolleingriffAnforderung> AlsZeugeBestaetigenAsync(int anforderungId, CancellationToken ct = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        var aktueller = _benutzerKontext.AktuellerBenutzer;

        if (!_benutzerKontext.HatBerechtigung(Berechtigung.KontrolleingriffAlsZeugeBestaetigen))
        {
            throw await VerweigereAsync(db, "Als Zeuge bestätigt", $"Anforderung #{anforderungId}",
                "Keine Berechtigung, als Zeuge zu bestätigen (nur Schichtleitung).",
                grund => new NichtBerechtigtException(grund), ct);
        }

        var anforderung = await LadeAsync(db, anforderungId, ct);
        var zielobjekt = $"Maschine #{anforderung.MaschineId}";

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

        return await SpeichereMitGleichzeitigkeitsschutzAsync(db, anforderung, "Als Zeuge bestätigt", zielobjekt, ct);
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

    private static bool IstAbgeschlossen(KontrolleingriffStatus status) =>
        status is KontrolleingriffStatus.Freigegeben or KontrolleingriffStatus.Abgelehnt or KontrolleingriffStatus.Zurueckgezogen;

    /// <summary>
    /// Lädt die Anforderung erst, NACHDEM die aufrufende Methode die
    /// generische Berechtigung geprüft hat (Härtung): Wer die Berechtigung
    /// nicht hat, bekommt dieselbe Verweigerung unabhängig davon, ob die Id
    /// überhaupt existiert, und kann so nicht über die Fehlermeldung
    /// vorhandene Anforderungsnummern erraten.
    /// </summary>
    private static async Task<KontrolleingriffAnforderung> LadeAsync(AppDbContext db, int anforderungId, CancellationToken ct) =>
        await db.KontrolleingriffAnforderungen.FirstOrDefaultAsync(a => a.Id == anforderungId, ct)
            ?? throw new InvalidOperationException($"Kontrolleingriff-Anforderung #{anforderungId} wurde nicht gefunden.");

    private static void PruefeLaenge(string? wert, string parameterName, string feldbezeichnung)
    {
        if (wert is not null && wert.Length > MaxLaengeText)
        {
            throw new ArgumentException($"{feldbezeichnung} darf höchstens {MaxLaengeText} Zeichen lang sein.", parameterName);
        }
    }

    /// <summary>
    /// Speichert die Änderung und fängt dabei einen Gleichzeitigkeitskonflikt
    /// ab: Hat eine andere Aktion den Status derselben Anforderung zwischen
    /// Laden und Speichern bereits geändert (Status ist Concurrency-Token,
    /// siehe AppDbContext), betrifft das UPDATE null Zeilen und EF Core wirft
    /// <see cref="DbUpdateConcurrencyException"/>. Die Verweigerung wird über
    /// einen frischen Kontext protokolliert, weil <paramref name="db"/> nach
    /// dem fehlgeschlagenen Speichern eine inkonsistente, nicht erneut
    /// speicherbare Änderung verfolgt.
    /// </summary>
    private async Task<KontrolleingriffAnforderung> SpeichereMitGleichzeitigkeitsschutzAsync(
        AppDbContext db,
        KontrolleingriffAnforderung anforderung,
        string aktion,
        string zielobjekt,
        CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return anforderung;
        }
        catch (DbUpdateConcurrencyException)
        {
            var grund =
                $"Anforderung #{anforderung.Id} wurde zwischenzeitlich von einer anderen Aktion geändert " +
                "(gleichzeitiger Zugriff); diese Aktion wurde nicht ausgeführt. Bitte neu laden und erneut versuchen.";

            var aktueller = _benutzerKontext.AktuellerBenutzer;
            await using var auditDb = await _dbContextFactory.CreateDbContextAsync(ct);
            auditDb.AuditLogEintraege.Add(new AuditLogEintrag
            {
                Benutzer = aktueller.Anzeigename,
                BenutzerKennung = aktueller.Kennung.Wert,
                Kategorie = AuditKategorie.ZugriffVerweigert,
                Aktion = aktion,
                Zielobjekt = zielobjekt,
                Begruendung = grund
            });
            await auditDb.SaveChangesAsync(ct);

            throw new GleichzeitigkeitskonfliktException(grund);
        }
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
