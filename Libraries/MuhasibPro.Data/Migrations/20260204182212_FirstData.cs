using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MuhasibPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class FirstData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppVersiyonlar",
                columns: table => new
                {
                    CurrentAppVersion = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentAppVersionLastUpdate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PreviousAppVersiyon = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppVersiyonlar", x => x.CurrentAppVersion);
                });

            migrationBuilder.CreateTable(
                name: "Firmalar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    FirmaKodu = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KisaUnvani = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TamUnvani = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    YetkiliKisi = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Il = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Ilce = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Adres = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    PostaKodu = table.Column<string>(type: "TEXT", maxLength: 25, nullable: true),
                    Telefon1 = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    Telefon2 = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    VergiDairesi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VergiNo = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    TCNo = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    Web = table.Column<string>(type: "TEXT", maxLength: 75, nullable: true),
                    Eposta = table.Column<string>(type: "TEXT", maxLength: 75, nullable: true),
                    Logo = table.Column<byte[]>(type: "BLOB", nullable: true),
                    LogoOnizleme = table.Column<byte[]>(type: "BLOB", nullable: true),
                    PBu1 = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PBu2 = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    KaydedenId = table.Column<long>(type: "INTEGER", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuncelleyenId = table.Column<long>(type: "INTEGER", nullable: true),
                    AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArananTerim = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Firmalar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciRoller",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    RolAdi = table.Column<string>(type: "TEXT", nullable: false),
                    Aciklama = table.Column<string>(type: "TEXT", nullable: false),
                    RolTip = table.Column<int>(type: "INTEGER", nullable: false),
                    KaydedenId = table.Column<long>(type: "INTEGER", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuncelleyenId = table.Column<long>(type: "INTEGER", nullable: true),
                    AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArananTerim = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciRoller", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SistemLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    User = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    KaydedenId = table.Column<long>(type: "INTEGER", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuncelleyenId = table.Column<long>(type: "INTEGER", nullable: true),
                    AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArananTerim = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SistemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppDbVersiyonlar",
                columns: table => new
                {
                    CurrentAppVersion = table.Column<string>(type: "TEXT", nullable: false),
                    DatabaseName = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentDatabaseVersion = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentDatabaseLastUpdate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PreviousDatabaseVersion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDbVersiyonlar", x => x.CurrentAppVersion);
                    table.ForeignKey(
                        name: "FK_AppDbVersiyonlar_AppVersiyonlar_CurrentAppVersion",
                        column: x => x.CurrentAppVersion,
                        principalTable: "AppVersiyonlar",
                        principalColumn: "CurrentAppVersion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaliDonemler",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    FirmaId = table.Column<long>(type: "INTEGER", nullable: false),
                    MaliYil = table.Column<int>(type: "INTEGER", nullable: false),
                    DatabaseName = table.Column<string>(type: "TEXT", nullable: false),
                    DatabaseType = table.Column<int>(type: "INTEGER", nullable: false),
                    KaydedenId = table.Column<long>(type: "INTEGER", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuncelleyenId = table.Column<long>(type: "INTEGER", nullable: true),
                    AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArananTerim = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaliDonemler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaliDonemler_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false),
                    KullaniciAdi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ParolaHash = table.Column<string>(type: "TEXT", maxLength: 400, nullable: false),
                    Adi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Soyadi = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Eposta = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RolId = table.Column<long>(type: "INTEGER", nullable: false),
                    Telefon = table.Column<string>(type: "TEXT", maxLength: 17, nullable: false),
                    Resim = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ResimOnizleme = table.Column<byte[]>(type: "BLOB", nullable: true),
                    KaydedenId = table.Column<long>(type: "INTEGER", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GuncelleyenId = table.Column<long>(type: "INTEGER", nullable: true),
                    AktifMi = table.Column<bool>(type: "INTEGER", nullable: false),
                    ArananTerim = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_KullaniciRoller_RolId",
                        column: x => x.RolId,
                        principalTable: "KullaniciRoller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Hesaplar",
                columns: table => new
                {
                    KullaniciId = table.Column<long>(type: "INTEGER", nullable: false),
                    FirmaId = table.Column<long>(type: "INTEGER", nullable: true),
                    SonGirisTarihi = table.Column<DateTime>(type: "TEXT", nullable: false),
                    KullaniciId1 = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hesaplar", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_Hesaplar_Firmalar_FirmaId",
                        column: x => x.FirmaId,
                        principalTable: "Firmalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Hesaplar_Kullanicilar_KullaniciId1",
                        column: x => x.KullaniciId1,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AppVersiyonlar",
                columns: new[] { "CurrentAppVersion", "CurrentAppVersionLastUpdate", "PreviousAppVersiyon" },
                values: new object[] { "1.0.0", new DateTime(2025, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.InsertData(
                table: "KullaniciRoller",
                columns: new[] { "Id", "Aciklama", "AktifMi", "ArananTerim", "GuncellemeTarihi", "GuncelleyenId", "KaydedenId", "KayitTarihi", "RolAdi", "RolTip" },
                values: new object[,]
                {
                    { 241341L, "Sistemi yönetme yetkisine sahip kullanıcı rolü", true, null, null, null, 5413300800L, new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yönetici", 1 },
                    { 241342L, "Sistemi sınırlı şekilde kullanma yetkisine sahip kullanıcı rolü", true, null, null, null, 5413300800L, new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Kullanıcı", 2 }
                });

            migrationBuilder.InsertData(
                table: "AppDbVersiyonlar",
                columns: new[] { "CurrentAppVersion", "CurrentDatabaseLastUpdate", "CurrentDatabaseVersion", "DatabaseName", "PreviousDatabaseVersion" },
                values: new object[] { "1.0.0", new DateTime(2025, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "1.0.0", "Sistem.db", null });

            migrationBuilder.InsertData(
                table: "Kullanicilar",
                columns: new[] { "Id", "Adi", "AktifMi", "ArananTerim", "Eposta", "GuncellemeTarihi", "GuncelleyenId", "KaydedenId", "KayitTarihi", "KullaniciAdi", "ParolaHash", "Resim", "ResimOnizleme", "RolId", "Soyadi", "Telefon" },
                values: new object[] { 5413300800L, "Ömer", true, "korkutomer, Ömer Korkut, Yönetici", "korkutomer@gmail.com", null, null, 5413300800L, new DateTime(2025, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "korkutomer", "AQAAAAIAAYagAAAAECnYdlrjFiWFJc+FGeGDmvR87uz20oU/Z0K4JE9ddoF2VUnmHw0idEFX8UPOb4cpzQ==", null, null, 241341L, "Korkut", "0 (541) 330 0800" });

            migrationBuilder.CreateIndex(
                name: "IX_Hesaplar_FirmaId",
                table: "Hesaplar",
                column: "FirmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Hesaplar_KullaniciId1",
                table: "Hesaplar",
                column: "KullaniciId1");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_RolId",
                table: "Kullanicilar",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_MaliDonemler_FirmaId",
                table: "MaliDonemler",
                column: "FirmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppDbVersiyonlar");

            migrationBuilder.DropTable(
                name: "Hesaplar");

            migrationBuilder.DropTable(
                name: "MaliDonemler");

            migrationBuilder.DropTable(
                name: "SistemLogs");

            migrationBuilder.DropTable(
                name: "AppVersiyonlar");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "Firmalar");

            migrationBuilder.DropTable(
                name: "KullaniciRoller");
        }
    }
}
