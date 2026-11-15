using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CryptoSimulator.Migrations;

/// <inheritdoc />
public partial class _20261115134612_AddOffers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Offers",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SellerWalletId = table.Column<int>(type: "integer", nullable: false),
                CryptoId = table.Column<int>(type: "integer", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                RemainingAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                AskingPrice = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Offers", x => x.Id);
                table.ForeignKey(
                    name: "FK_Offers_Cryptos_CryptoId",
                    column: x => x.CryptoId,
                    principalTable: "Cryptos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Offers_Wallets_SellerWalletId",
                    column: x => x.SellerWalletId,
                    principalTable: "Wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "OfferFills",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OfferId = table.Column<int>(type: "integer", nullable: false),
                BuyerWalletId = table.Column<int>(type: "integer", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                Price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OfferFills", x => x.Id);
                table.ForeignKey(
                    name: "FK_OfferFills_Offers_OfferId",
                    column: x => x.OfferId,
                    principalTable: "Offers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_OfferFills_Wallets_BuyerWalletId",
                    column: x => x.BuyerWalletId,
                    principalTable: "Wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OfferFills_BuyerWalletId",
            table: "OfferFills",
            column: "BuyerWalletId");

        migrationBuilder.CreateIndex(
            name: "IX_OfferFills_OfferId",
            table: "OfferFills",
            column: "OfferId");

        migrationBuilder.CreateIndex(
            name: "IX_Offers_CryptoId",
            table: "Offers",
            column: "CryptoId");

        migrationBuilder.CreateIndex(
            name: "IX_Offers_SellerWalletId",
            table: "Offers",
            column: "SellerWalletId");

        migrationBuilder.CreateIndex(
            name: "IX_Offers_Status_CryptoId",
            table: "Offers",
            columns: new[] { "Status", "CryptoId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OfferFills");

        migrationBuilder.DropTable(
            name: "Offers");
    }
}
