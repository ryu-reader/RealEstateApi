using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateAPI.Migrations
{
    /// <inheritdoc />
    public partial class StatusProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FidoCredentials_Users_UserId",
                table: "FidoCredentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FidoCredentials",
                table: "FidoCredentials");

            migrationBuilder.RenameTable(
                name: "FidoCredentials",
                newName: "FidoCredential");

            migrationBuilder.RenameIndex(
                name: "IX_FidoCredentials_UserId",
                table: "FidoCredential",
                newName: "IX_FidoCredential_UserId");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Properties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FidoCredential",
                table: "FidoCredential",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FidoCredential_Users_UserId",
                table: "FidoCredential",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FidoCredential_Users_UserId",
                table: "FidoCredential");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FidoCredential",
                table: "FidoCredential");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Properties");

            migrationBuilder.RenameTable(
                name: "FidoCredential",
                newName: "FidoCredentials");

            migrationBuilder.RenameIndex(
                name: "IX_FidoCredential_UserId",
                table: "FidoCredentials",
                newName: "IX_FidoCredentials_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FidoCredentials",
                table: "FidoCredentials",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FidoCredentials_Users_UserId",
                table: "FidoCredentials",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
