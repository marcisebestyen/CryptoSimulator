using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CryptoSimulator.Migrations;

/// <inheritdoc />
public partial class _20261115114908_AddSavedCardsAndDeposits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SavedCards",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                HolderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Last4 = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                Network = table.Column<string>(type: "text", nullable: false),
                ExpiryMonth = table.Column<int>(type: "integer", nullable: false),
                ExpiryYear = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SavedCards", x => x.Id);
                table.ForeignKey(
                    name: "FK_SavedCards_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Deposits",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                WalletId = table.Column<int>(type: "integer", nullable: false),
                SavedCardId = table.Column<int>(type: "integer", nullable: true),
                Last4 = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                Network = table.Column<string>(type: "text", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Deposits", x => x.Id);
                table.ForeignKey(
                    name: "FK_Deposits_SavedCards_SavedCardId",
                    column: x => x.SavedCardId,
                    principalTable: "SavedCards",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Deposits_Wallets_WalletId",
                    column: x => x.WalletId,
                    principalTable: "Wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Deposits_SavedCardId",
            table: "Deposits",
            column: "SavedCardId");

        migrationBuilder.CreateIndex(
            name: "IX_Deposits_WalletId",
            table: "Deposits",
            column: "WalletId");

        migrationBuilder.CreateIndex(
            name: "IX_SavedCards_UserId",
            table: "SavedCards",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Deposits");

        migrationBuilder.DropTable(
            name: "SavedCards");
    }
}
