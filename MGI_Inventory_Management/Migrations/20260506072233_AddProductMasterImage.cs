using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MGI_Inventory_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMasterImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ProductMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ProductMasterLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ProductMasters");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ProductMasterLogs");
        }
    }
}
