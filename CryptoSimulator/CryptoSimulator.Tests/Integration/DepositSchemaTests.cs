using CryptoSimulator.Data;
using CryptoSimulator.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public class DepositSchemaTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task Deposit_survives_card_deletion()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, user.Id);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        var card = new SavedCard
        {
            UserId = user.Id,
            HolderName = "Jane Doe",
            Last4 = "4242",
            Network = CardNetwork.Visa,
            ExpiryMonth = 12,
            ExpiryYear = 2030
        };
        db.SavedCards.Add(card);
        await db.SaveChangesAsync();
        var deposit = new Deposit
        {
            WalletId = walletId,
            SavedCardId = card.Id,
            Last4 = "4242",
            Network = CardNetwork.Visa,
            Amount = 50m,
            Date = DateTime.UtcNow
        };
        db.Deposits.Add(deposit);
        await db.SaveChangesAsync();

        db.SavedCards.Remove(card);
        await db.SaveChangesAsync();

        var reloaded = await db.Deposits.AsNoTracking().SingleAsync(d => d.Id == deposit.Id);
        Assert.Null(reloaded.SavedCardId);
        Assert.Equal("4242", reloaded.Last4);
        Assert.Equal(CardNetwork.Visa, reloaded.Network);
    }

    [Fact]
    public async Task Deposit_amount_has_two_decimals()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, user.Id);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        var deposit = new Deposit
        {
            WalletId = walletId,
            Last4 = "0005",
            Network = CardNetwork.Amex,
            Amount = 10.129m,
            Date = DateTime.UtcNow
        };
        db.Deposits.Add(deposit);
        await db.SaveChangesAsync();

        var reloaded = await db.Deposits.AsNoTracking().SingleAsync(d => d.Id == deposit.Id);

        Assert.Equal(10.13m, reloaded.Amount);
    }
}
