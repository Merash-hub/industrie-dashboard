using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IndustrieDashboard.Infrastructure.Data;

/// <summary>
/// Design-Time-Factory, damit "dotnet ef migrations add ..." funktioniert,
/// ohne dass die gesamte WPF-App gestartet werden muss.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var dbPfad = Path.Combine(AppContext.BaseDirectory, "industriedashboard.db");
        optionsBuilder.UseSqlite($"Data Source={dbPfad}");
        return new AppDbContext(optionsBuilder.Options);
    }
}
