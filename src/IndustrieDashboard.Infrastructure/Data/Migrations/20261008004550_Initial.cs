using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IndustrieDashboard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogEintraege",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Zeitstempel = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Benutzer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Aktion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Zielobjekt = table.Column<string>(type: "TEXT", nullable: false),
                    AlterWert = table.Column<string>(type: "TEXT", nullable: true),
                    NeuerWert = table.Column<string>(type: "TEXT", nullable: true),
                    Begruendung = table.Column<string>(type: "TEXT", nullable: true),
                    GegengezeichnetVon = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEintraege", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KontrolleingriffAnforderungen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaschineId = table.Column<int>(type: "INTEGER", nullable: false),
                    Beschreibung = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AngefordertVon = table.Column<string>(type: "TEXT", nullable: false),
                    AngefordertAm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FreigegebenVon = table.Column<string>(type: "TEXT", nullable: true),
                    FreigegebenAm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KontrolleingriffAnforderungen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Maschinen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Standort = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FeldbusKennung = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AktuellerWert = table.Column<double>(type: "REAL", nullable: false),
                    LetzteAktualisierung = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maschinen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mitarbeiter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Personalnummer = table.Column<string>(type: "TEXT", nullable: false),
                    Qualifikation = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mitarbeiter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schichten",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Bezeichnung = table.Column<string>(type: "TEXT", nullable: false),
                    Beginn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Ende = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schichten", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Maschinenmesswerte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MaschineId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kennwert = table.Column<string>(type: "TEXT", nullable: false),
                    Wert = table.Column<double>(type: "REAL", nullable: false),
                    Zeitstempel = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maschinenmesswerte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maschinenmesswerte_Maschinen_MaschineId",
                        column: x => x.MaschineId,
                        principalTable: "Maschinen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MitarbeiterSchicht",
                columns: table => new
                {
                    MitarbeiterId = table.Column<int>(type: "INTEGER", nullable: false),
                    SchichtId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MitarbeiterSchicht", x => new { x.MitarbeiterId, x.SchichtId });
                    table.ForeignKey(
                        name: "FK_MitarbeiterSchicht_Mitarbeiter_MitarbeiterId",
                        column: x => x.MitarbeiterId,
                        principalTable: "Mitarbeiter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MitarbeiterSchicht_Schichten_SchichtId",
                        column: x => x.SchichtId,
                        principalTable: "Schichten",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Maschinenmesswerte_MaschineId",
                table: "Maschinenmesswerte",
                column: "MaschineId");

            migrationBuilder.CreateIndex(
                name: "IX_MitarbeiterSchicht_SchichtId",
                table: "MitarbeiterSchicht",
                column: "SchichtId");

            // Audit-Trail unveraenderlich machen (siehe AppDbContext): UPDATE/DELETE
            // auf der Tabelle werden auf Datenbankebene abgewiesen, unabhaengig vom
            // Zugriffsweg (EF Core, ExecuteUpdate/ExecuteDelete, rohes SQL, externes Tool).
            migrationBuilder.Sql("""
                CREATE TRIGGER IF NOT EXISTS trg_AuditLogEintraege_kein_update
                BEFORE UPDATE ON "AuditLogEintraege"
                BEGIN
                    SELECT RAISE(ABORT, 'Audit-Log-Eintraege sind unveraenderlich: UPDATE ist nicht erlaubt.');
                END;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER IF NOT EXISTS trg_AuditLogEintraege_kein_delete
                BEFORE DELETE ON "AuditLogEintraege"
                BEGIN
                    SELECT RAISE(ABORT, 'Audit-Log-Eintraege sind unveraenderlich: DELETE ist nicht erlaubt.');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_AuditLogEintraege_kein_delete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_AuditLogEintraege_kein_update;");

            migrationBuilder.DropTable(
                name: "AuditLogEintraege");

            migrationBuilder.DropTable(
                name: "KontrolleingriffAnforderungen");

            migrationBuilder.DropTable(
                name: "Maschinenmesswerte");

            migrationBuilder.DropTable(
                name: "MitarbeiterSchicht");

            migrationBuilder.DropTable(
                name: "Maschinen");

            migrationBuilder.DropTable(
                name: "Mitarbeiter");

            migrationBuilder.DropTable(
                name: "Schichten");
        }
    }
}
