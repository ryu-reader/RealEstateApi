using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RealEstateAPI.Migrations
{
    /// <inheritdoc />
    public partial class Fido2Test1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Properties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Properties",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FidoCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "longblob", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "longblob", nullable: false),
                    SignCount = table.Column<uint>(type: "int unsigned", nullable: false),
                    CredType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FidoCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FidoCredentials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_CreatedById",
                table: "Properties",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FidoCredentials_UserId",
                table: "FidoCredentials",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Users_CreatedById",
                table: "Properties",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Users_CreatedById",
                table: "Properties");

            migrationBuilder.DropTable(
                name: "FidoCredentials");

            migrationBuilder.DropIndex(
                name: "IX_Properties_CreatedById",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Properties");
        }
    }
}
