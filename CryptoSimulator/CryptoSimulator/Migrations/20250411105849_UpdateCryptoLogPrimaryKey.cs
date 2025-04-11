using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoSimulator.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCryptoLogPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CryptoLogs",
                table: "CryptoLogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CryptoLogs",
                table: "CryptoLogs",
                columns: new[] { "CryptoId", "From" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CryptoLogs",
                table: "CryptoLogs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CryptoLogs",
                table: "CryptoLogs",
                column: "CryptoId");
        }
    }
}
