using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrieDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Haertung : Migration
    {
        // Leere Migration: HasMaxLength (Beschreibung/AusnahmeGrund) und
        // IsConcurrencyToken (Status) sind reine EF-Core-Modellmetadaten ohne
        // Auswirkung auf das von SQLite erzeugte Schema (SQLite kennt keine
        // Spaltenlaengenbegrenzung und keinen nativen Rowversion-Typ). Die
        // Migration existiert trotzdem, damit der Modell-Snapshot aktuell
        // bleibt und "dotnet ef migrations add" nicht wegen ausstehender
        // Modelländerungen scheitert. Die eigentliche Durchsetzung passt bei
        // beiden Metadaten im Dienst (Laengenprüfung, DbUpdateConcurrencyException).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
