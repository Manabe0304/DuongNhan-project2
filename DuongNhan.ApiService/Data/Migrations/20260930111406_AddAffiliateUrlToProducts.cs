using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuongNhan.ApiService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAffiliateUrlToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliateUrl",
                table: "products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffiliateUrl",
                table: "products");
        }
    }
}
