using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CryptoSimulator.Migrations;

/// <inheritdoc />
public partial class _20261114113827_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Cryptos",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cryptos", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Role = table.Column<int>(type: "integer", nullable: false),
                Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                FirstName = table.Column<string>(type: "text", nullable: false),
                LastName = table.Column<string>(type: "text", nullable: false),
                Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Password = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CryptoLogs",
            columns: table => new
            {
                CryptoId = table.Column<int>(type: "integer", nullable: false),
                From = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CurrentValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                To = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CryptoLogs", x => new { x.CryptoId, x.From });
                table.ForeignKey(
                    name: "FK_CryptoLogs_Cryptos_CryptoId",
                    column: x => x.CryptoId,
                    principalTable: "Cryptos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "PriceAlerts",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                CryptoId = table.Column<int>(type: "integer", nullable: false),
                TargetPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                AlertType = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                HasBeenNotifiedForCurrentState = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                table.ForeignKey(
                    name: "FK_PriceAlerts_Cryptos_CryptoId",
                    column: x => x.CryptoId,
                    principalTable: "Cryptos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PriceAlerts_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Wallets",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                UserId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Wallets", x => x.Id);
                table.ForeignKey(
                    name: "FK_Wallets_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MyCryptos",
            columns: table => new
            {
                WalletId = table.Column<int>(type: "integer", nullable: false),
                CryptoId = table.Column<int>(type: "integer", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MyCryptos", x => new { x.WalletId, x.CryptoId });
                table.ForeignKey(
                    name: "FK_MyCryptos_Cryptos_CryptoId",
                    column: x => x.CryptoId,
                    principalTable: "Cryptos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MyCryptos_Wallets_WalletId",
                    column: x => x.WalletId,
                    principalTable: "Wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Transactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                WalletId = table.Column<int>(type: "integer", nullable: false),
                CryptoId = table.Column<int>(type: "integer", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                ExchangeRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsPurchase = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Transactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Transactions_Cryptos_CryptoId",
                    column: x => x.CryptoId,
                    principalTable: "Cryptos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Transactions_Wallets_WalletId",
                    column: x => x.WalletId,
                    principalTable: "Wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MyCryptos_CryptoId",
            table: "MyCryptos",
            column: "CryptoId");

        migrationBuilder.CreateIndex(
            name: "IX_PriceAlerts_CryptoId",
            table: "PriceAlerts",
            column: "CryptoId");

        migrationBuilder.CreateIndex(
            name: "IX_PriceAlerts_UserId",
            table: "PriceAlerts",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_CryptoId",
            table: "Transactions",
            column: "CryptoId");

        migrationBuilder.CreateIndex(
            name: "IX_Transactions_WalletId",
            table: "Transactions",
            column: "WalletId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Wallets_UserId",
            table: "Wallets",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CryptoLogs");

        migrationBuilder.DropTable(
            name: "MyCryptos");

        migrationBuilder.DropTable(
            name: "PriceAlerts");

        migrationBuilder.DropTable(
            name: "Transactions");

        migrationBuilder.DropTable(
            name: "Cryptos");

        migrationBuilder.DropTable(
            name: "Wallets");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
