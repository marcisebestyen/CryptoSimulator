using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoSimulator.Migrations;

/// <inheritdoc />
public partial class _20261115133913_WidenAmountsToFourDecimals : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Amount",
            table: "Transactions",
            type: "numeric(18,4)",
            precision: 18,
            scale: 4,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2);

        migrationBuilder.AlterColumn<decimal>(
            name: "Amount",
            table: "MyCryptos",
            type: "numeric(18,4)",
            precision: 18,
            scale: 4,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,2)",
            oldPrecision: 18,
            oldScale: 2);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(
            name: "Amount",
            table: "Transactions",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)",
            oldPrecision: 18,
            oldScale: 4);

        migrationBuilder.AlterColumn<decimal>(
            name: "Amount",
            table: "MyCryptos",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "numeric(18,4)",
            oldPrecision: 18,
            oldScale: 4);
    }
}
