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
    /// Der Audit-Trail darf nur angelegt, nie geändert oder gelöscht werden.
    /// Diese Prüfung greift unabhängig davon, über welchen Code-Pfad ein
    /// Update/Delete versucht wird, und ergänzt die init-only-Eigenschaften
    /// von <see cref="AuditLogEintrag"/>.
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
