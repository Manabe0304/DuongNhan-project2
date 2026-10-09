using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuongNhan.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkinTypeAndStepToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkinType",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "All");

            migrationBuilder.AddColumn<int>(
                name: "Step",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_products_Step",
                table: "products",
                column: "Step");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_Step",
                table: "products");

            migrationBuilder.DropColumn(
                name: "SkinType",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Step",
                table: "products");
        }
    }
}
