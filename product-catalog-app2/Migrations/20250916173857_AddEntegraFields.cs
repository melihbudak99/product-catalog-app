using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductCatalogApp.Migrations
{
    public partial class AddEntegraFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HepsiburadaSellerStockCode",
                table: "XmlProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "N11CatalogId",
                table: "XmlProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "N11ProductCode",
                table: "XmlProducts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntegraBarkod",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntegraUrunId",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntegraUrunKodu",
                table: "Products",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HepsiburadaSellerStockCode",
                table: "XmlProducts");

            migrationBuilder.DropColumn(
                name: "N11CatalogId",
                table: "XmlProducts");

            migrationBuilder.DropColumn(
                name: "N11ProductCode",
                table: "XmlProducts");

            migrationBuilder.DropColumn(
                name: "EntegraBarkod",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EntegraUrunId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EntegraUrunKodu",
                table: "Products");
        }
    }
}
