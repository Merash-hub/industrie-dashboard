using IndustrieDashboard.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace IndustrieDashboard.Infrastructure.Data;

/// <summary>
/// EF-Core-Datenkontext für den Prototyp. Läuft lokal gegen SQLite;
/// die Zielarchitektur sieht zusätzlich einen zentralen PostgreSQL-Kontext
/// vor (z. B. für konsolidierte Auswertungen über mehrere Standorte) - dafür
/// reicht später ein zweiter DbContext mit demselben Modell und
/// UseNpgsql(...) statt UseSqlite(...).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Maschine> Maschinen => Set<Maschine>();
    public DbSet<Maschinenmesswert> Maschinenmesswerte => Set<Maschinenmesswert>();
    public DbSet<Schicht> Schichten => Set<Schicht>();
    public DbSet<Mitarbeiter> Mitarbeiter => Set<Mitarbeiter>();
    public DbSet<AuditLogEintrag> AuditLogEintraege => Set<AuditLogEintrag>();
    public DbSet<KontrolleingriffAnforderung> KontrolleingriffAnforderungen => Set<KontrolleingriffAnforderung>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Maschine>(b =>
        {
            b.Property(m => m.Name).IsRequired().HasMaxLength(200);
            b.Property(m => m.Standort).HasMaxLength(200);
            b.HasMany(m => m.Messwerte)
                .WithOne(w => w.Maschine)
                .HasForeignKey(w => w.MaschineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Schicht>()
            .HasMany(s => s.Mitarbeiter)
            .WithMany();

        modelBuilder.Entity<AuditLogEintrag>(b =>
        {
            b.Property(a => a.Benutzer).IsRequired().HasMaxLength(200);
            b.Property(a => a.Aktion).IsRequired().HasMaxLength(200);
        });
    }

    /// <summary>
    /// Erstellt das Schema, falls es noch nicht existiert, und legt danach die
    /// SQLite-Trigger an, die UPDATE/DELETE auf der Audit-Log-Tabelle
    /// abweisen. Muss beim Initialisieren der Datenbank statt eines nackten
    /// <c>Database.EnsureCreated()</c> aufgerufen werden.
    /// </summary>
    public void SicherstellenErstelltMitAuditSchutz()
    {
        Database.EnsureCreated();
        ErstelleAuditLogTrigger();
    }

    /// <summary>
    /// <see cref="PruefeAuditLogUnveraenderlichkeit"/> greift nur, wenn über
    /// den ChangeTracker gespeichert wird. <c>ExecuteUpdate</c>,
    /// <c>ExecuteDelete</c> und rohes SQL gehen daran vorbei, weil sie direkt
    /// gegen die Datenbank übersetzt werden. Diese Trigger sind deshalb die
    /// eigentliche, nicht umgehbare Durchsetzung der Unveränderlichkeit: Sie
    /// greifen auf Datenbankebene, unabhängig vom Zugriffsweg (EF Core,
    /// rohes ADO.NET, ein externes Tool, das direkt auf die .db-Datei
    /// zugreift).
    /// </summary>
    private void ErstelleAuditLogTrigger()
    {
        const string tabelle = "AuditLogEintraege";

        Database.ExecuteSqlRaw($"""
            CREATE TRIGGER IF NOT EXISTS trg_{tabelle}_kein_update
            BEFORE UPDATE ON "{tabelle}"
            BEGIN
                SELECT RAISE(ABORT, 'Audit-Log-Eintraege sind unveraenderlich: UPDATE ist nicht erlaubt.');
            END;
            """);

        Database.ExecuteSqlRaw($"""
            CREATE TRIGGER IF NOT EXISTS trg_{tabelle}_kein_delete
            BEFORE DELETE ON "{tabelle}"
            BEGIN
                SELECT RAISE(ABORT, 'Audit-Log-Eintraege sind unveraenderlich: DELETE ist nicht erlaubt.');
            END;
            """);
    }

    /// <summary>
    /// Der Audit-Trail darf nur angelegt, nie geändert oder gelöscht werden.
    /// Diese Prüfung greift unabhängig davon, über welchen Code-Pfad ein
    /// Update/Delete versucht wird, und ergänzt die init-only-Eigenschaften
    /// von <see cref="AuditLogEintrag"/>. Sie fängt Verstöße früh und mit
    /// einer sprechenden Meldung ab, wenn über den ChangeTracker gespeichert
    /// wird; die eigentliche, nicht umgehbare Absicherung sind die Trigger
    /// aus <see cref="ErstelleAuditLogTrigger"/>.
    /// </summary>
    private void PruefeAuditLogUnveraenderlichkeit()
    {
        var verletzungen = ChangeTracker.Entries<AuditLogEintrag>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (verletzungen)
        {
            throw new InvalidOperationException(
                "Audit-Log-Einträge sind unveränderlich: Ändern oder Löschen ist nicht erlaubt.");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PruefeAuditLogUnveraenderlichkeit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PruefeAuditLogUnveraenderlichkeit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
