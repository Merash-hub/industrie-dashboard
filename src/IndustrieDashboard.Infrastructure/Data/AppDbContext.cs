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
}
