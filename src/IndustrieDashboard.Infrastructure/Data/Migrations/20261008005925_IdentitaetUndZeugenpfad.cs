using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrieDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdentitaetUndZeugenpfad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AngefordertVonKennung",
                table: "KontrolleingriffAnforderungen",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AusnahmeGrund",
                table: "KontrolleingriffAnforderungen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreigegebenVonKennung",
                table: "KontrolleingriffAnforderungen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZeugeAnzeigename",
                table: "KontrolleingriffAnforderungen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZeugeBestaetigtAm",
                table: "KontrolleingriffAnforderungen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZeugeKennung",
                table: "KontrolleingriffAnforderungen",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BenutzerKennung",
                table: "AuditLogEintraege",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Kategorie",
                table: "AuditLogEintraege",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AngefordertVonKennung",
                table: "KontrolleingriffAnforderungen");

            migrationBuilder.DropColumn(
                name: "AusnahmeGrund",
                table: "KontrolleingriffAnforderungen");

            migrationBuilder.DropColumn(
                name: "FreigegebenVonKennung",
                table: "KontrolleingriffAnforderungen");

            migrationBuilder.DropColumn(
                name: "ZeugeAnzeigename",
                table: "KontrolleingriffAnforderungen");

            migrationBuilder.DropColumn(
                name: "ZeugeBestaetigtAm",
                table: "KontrolleingriffAnforderungen");

            migrationBuilder.DropColumn(
                name: "ZeugeKennung",
                table: "KontrolleingriffAnforderungen");

            migrationBuilder.DropColumn(
                name: "BenutzerKennung",
                table: "AuditLogEintraege");

            migrationBuilder.DropColumn(
                name: "Kategorie",
                table: "AuditLogEintraege");
        }
    }
}
