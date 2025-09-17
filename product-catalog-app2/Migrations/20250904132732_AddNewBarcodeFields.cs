using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductCatalogApp.Migrations
{
    public partial class AddNewBarcodeFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XmlProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DescriptionHtml = table.Column<string>(type: "TEXT", nullable: false),
                    DescriptionPlain = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Weight = table.Column<decimal>(type: "TEXT", nullable: false),
                    Desi = table.Column<decimal>(type: "TEXT", nullable: false),
                    Width = table.Column<decimal>(type: "TEXT", nullable: false),
                    Height = table.Column<decimal>(type: "TEXT", nullable: false),
                    Depth = table.Column<decimal>(type: "TEXT", nullable: false),
                    WarrantyMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    EanCode = table.Column<string>(type: "TEXT", nullable: false),
                    Features = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    TrendyolBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    HepsiburadaBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    KoctasBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    KoctasIstanbulBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    HepsiburadaTedarikBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    PttAvmBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    PazaramaBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    HaceyapiBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    AmazonBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    SpareBarcode1 = table.Column<string>(type: "TEXT", nullable: false),
                    SpareBarcode2 = table.Column<string>(type: "TEXT", nullable: false),
                    SpareBarcode3 = table.Column<string>(type: "TEXT", nullable: false),
                    SpareBarcode4 = table.Column<string>(type: "TEXT", nullable: false),
                    LogoBarcodes = table.Column<string>(type: "TEXT", nullable: false),
                    KoctasEanBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    KoctasEanIstanbulBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    PttUrunStokKodu = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImageUrl1 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl2 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl3 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl4 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl5 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl6 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl7 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl8 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl9 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ImageUrl10 = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    MarketplaceImageUrl1 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl2 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl3 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl4 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl5 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl6 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl7 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl8 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl9 = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrl10 = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrl1 = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrl2 = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrl3 = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrl4 = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrl5 = table.Column<string>(type: "TEXT", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XmlProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SKU = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Features = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Desi = table.Column<decimal>(type: "TEXT", nullable: false),
                    Width = table.Column<decimal>(type: "TEXT", nullable: false),
                    Height = table.Column<decimal>(type: "TEXT", nullable: false),
                    Depth = table.Column<decimal>(type: "TEXT", nullable: false),
                    Length = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    WarrantyMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    Material = table.Column<string>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    EanCode = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TrendyolBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    HepsiburadaBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    HepsiburadaSellerStockCode = table.Column<string>(type: "TEXT", nullable: true),
                    KoctasBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    KoctasIstanbulBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    HepsiburadaTedarikBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    PttAvmBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    PazaramaBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    HaceyapiBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    AmazonBarcode = table.Column<string>(type: "TEXT", nullable: true),
                    N11CatalogId = table.Column<string>(type: "TEXT", nullable: true),
                    N11ProductCode = table.Column<string>(type: "TEXT", nullable: true),
                    SpareBarcode1 = table.Column<string>(type: "TEXT", nullable: true),
                    SpareBarcode2 = table.Column<string>(type: "TEXT", nullable: true),
                    SpareBarcode3 = table.Column<string>(type: "TEXT", nullable: true),
                    SpareBarcode4 = table.Column<string>(type: "TEXT", nullable: true),
                    LogoBarcodes = table.Column<string>(type: "TEXT", nullable: true),
                    KoctasEanBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    KoctasEanIstanbulBarcode = table.Column<string>(type: "TEXT", nullable: false),
                    PttUrunStokKodu = table.Column<string>(type: "TEXT", nullable: false),
                    KlozetKanalYapisi = table.Column<string>(type: "TEXT", nullable: true),
                    KlozetTipi = table.Column<string>(type: "TEXT", nullable: true),
                    KlozetKapakCinsi = table.Column<string>(type: "TEXT", nullable: true),
                    KlozetMontajTipi = table.Column<string>(type: "TEXT", nullable: true),
                    LawaboSuTasmaDeligi = table.Column<string>(type: "TEXT", nullable: true),
                    LawaboArmaturDeligi = table.Column<string>(type: "TEXT", nullable: true),
                    LawaboTipi = table.Column<string>(type: "TEXT", nullable: true),
                    LawaboOzelligi = table.Column<string>(type: "TEXT", nullable: true),
                    BataryaCikisUcuUzunlugu = table.Column<string>(type: "TEXT", nullable: true),
                    BataryaYuksekligi = table.Column<string>(type: "TEXT", nullable: true),
                    KabinTipi = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl1 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl2 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl3 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl4 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl5 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl6 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl7 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl8 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl9 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrl10 = table.Column<string>(type: "TEXT", nullable: false),
                    ImageUrls = table.Column<string>(type: "TEXT", nullable: false),
                    MarketplaceImageUrls = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrls = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Brand",
                table: "Products",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category_Brand",
                table: "Products",
                columns: new[] { "Category", "Brand" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedDate",
                table: "Products",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsArchived",
                table: "Products",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsArchived_Category_Brand",
                table: "Products",
                columns: new[] { "IsArchived", "Category", "Brand" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsArchived_Name",
                table: "Products",
                columns: new[] { "IsArchived", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "XmlProducts");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
