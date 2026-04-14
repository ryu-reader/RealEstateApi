using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateAPI.Migrations
{
    /// <inheritdoc />
    public partial class FeaturesFix1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Features_Properties_PropertyId",
                table: "Features");

            migrationBuilder.DropIndex(
                name: "IX_Features_PropertyId",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Features");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PropertyId",
                table: "Features",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Features_PropertyId",
                table: "Features",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Features_Properties_PropertyId",
                table: "Features",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id");
        }
    }
}
