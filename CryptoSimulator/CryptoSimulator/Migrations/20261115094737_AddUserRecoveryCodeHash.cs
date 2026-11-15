using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoSimulator.Migrations;

/// <inheritdoc />
public partial class _20261115094737_AddUserRecoveryCodeHash : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RecoveryCodeHash",
            table: "Users",
            type: "text",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_RecoveryCodeHash",
            table: "Users",
            column: "RecoveryCodeHash",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_RecoveryCodeHash",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "RecoveryCodeHash",
            table: "Users");
    }
}
