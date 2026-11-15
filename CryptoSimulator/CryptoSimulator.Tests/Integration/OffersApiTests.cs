using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public class OffersApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    private async Task<(int OfferId, int WalletA, int WalletB)> SeedOfferAsync()
    {
        var seller = await TestApi.CreateUserAsync(Factory);
        var buyer = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 10m);
        var sellerWallet = await TestApi.GetWalletIdAsync(Factory, seller.Id);
        var buyerWallet = await TestApi.GetWalletIdAsync(Factory, buyer.Id);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        var offer = new Offer
        {
            SellerWalletId = sellerWallet,
            CryptoId = crypto.Id,
            Amount = 1.2345m,
            RemainingAmount = 1.2345m,
            AskingPrice = 12.12345678m,
            Status = OfferStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        db.Offers.Add(offer);
        await db.SaveChangesAsync();
        return (offer.Id, sellerWallet, buyerWallet);
    }

    [Fact]
    public async Task Offers_CanBePersisted_AndLockRunsInsideTransaction()
    {
        var (offerId, sellerWallet, buyerWallet) = await SeedOfferAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await using var tx = await uow.BeginTransactionAsync();
            await uow.LockOfferAndWalletsAsync(offerId, buyerWallet, sellerWallet);
            await uow.CommitAsync();
        }

        using var readScope = Factory.Services.CreateScope();
        var db = readScope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        var reloaded = await db.Offers.AsNoTracking().SingleAsync(o => o.Id == offerId);
        Assert.Equal(1.2345m, reloaded.Amount);
        Assert.Equal(1.2345m, reloaded.RemainingAmount);
        Assert.Equal(12.12345678m, reloaded.AskingPrice);
        Assert.Equal(OfferStatus.Open, reloaded.Status);
    }

    [Fact]
    public async Task Lock_AcceptsSameWalletTwice_AndUnknownIds()
    {
        var (offerId, sellerWallet, _) = await SeedOfferAsync();

        using var scope = Factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using var tx = await uow.BeginTransactionAsync();
        await uow.LockOfferAndWalletsAsync(offerId, sellerWallet, sellerWallet);
        await uow.LockOfferAndWalletsAsync(int.MaxValue, int.MaxValue - 1, int.MaxValue - 2);
        await uow.CommitAsync();
    }

    [Fact]
    public async Task Lock_BlocksSecondTransaction_UntilFirstCommits()
    {
        var (offerId, sellerWallet, buyerWallet) = await SeedOfferAsync();

        using var scope1 = Factory.Services.CreateScope();
        using var scope2 = Factory.Services.CreateScope();
        var uow1 = scope1.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var uow2 = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await using var tx1 = await uow1.BeginTransactionAsync();
        await uow1.LockOfferAndWalletsAsync(offerId, buyerWallet, sellerWallet);

        await using var tx2 = await uow2.BeginTransactionAsync();
        var second = uow2.LockOfferAndWalletsAsync(offerId, buyerWallet, sellerWallet);

        var finishedEarly = await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.NotSame(second, finishedEarly);

        await uow1.CommitAsync();

        var finishedAfterCommit = await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(second, finishedAfterCommit);
        await second;
        await uow2.CommitAsync();
    }

    private async Task<int> SeedHoldingAsync(int walletId, int cryptoId, decimal amount)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        db.MyCryptos.Add(new MyCryptos { WalletId = walletId, CryptoId = cryptoId, Amount = amount });
        await db.SaveChangesAsync();
        return cryptoId;
    }

    private async Task<int> GetCryptoIdAsync(int offerId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        return await db.Offers.Where(o => o.Id == offerId).Select(o => o.CryptoId).SingleAsync();
    }

    [Fact]
    public async Task Lock_DropsHoldingLoadedBeforeLock_SoReloadSeesFreshValue()
    {
        var (offerId, sellerWallet, buyerWallet) = await SeedOfferAsync();
        var cryptoId = await SeedHoldingAsync(sellerWallet, await GetCryptoIdAsync(offerId), 5m);

        using var scope = Factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var before = await uow.MyCryptosRepository.GetByIdAsync([sellerWallet, cryptoId]);
        Assert.Equal(5m, before!.Amount);

        using (var other = Factory.Services.CreateScope())
        {
            var otherDb = other.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
            await otherDb.MyCryptos.Where(m => m.WalletId == sellerWallet && m.CryptoId == cryptoId)
                .ExecuteUpdateAsync(u => u.SetProperty(m => m.Amount, 2m));
        }

        await using var tx = await uow.BeginTransactionAsync();
        await uow.LockOfferAndWalletsAsync(offerId, sellerWallet, buyerWallet);
        var after = await uow.MyCryptosRepository.GetByIdAsync([sellerWallet, cryptoId]);
        await uow.CommitAsync();

        Assert.Equal(2m, after!.Amount);
    }

    [Fact]
    public async Task Lock_Throws_WhenLockedWalletHoldingHasPendingChanges()
    {
        var (offerId, sellerWallet, buyerWallet) = await SeedOfferAsync();
        var cryptoId = await SeedHoldingAsync(sellerWallet, await GetCryptoIdAsync(offerId), 5m);

        using var scope = Factory.Services.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var holding = await uow.MyCryptosRepository.GetByIdAsync([sellerWallet, cryptoId]);
        holding!.Amount = 1m;

        await using var tx = await uow.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => uow.LockOfferAndWalletsAsync(offerId, sellerWallet, buyerWallet));
    }

    private const decimal StartBalance = 1000m;

    private async Task<(TestUser Seller, int WalletId, CryptoGetDto Crypto)> SeedSellerAsync(decimal holding, decimal price = 10m)
    {
        var seller = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, price);
        var walletId = await TestApi.GetWalletIdAsync(Factory, seller.Id);
        await SeedHoldingAsync(walletId, crypto.Id, holding);
        return (seller, walletId, crypto);
    }

    private static async Task<MyOfferDto> CreateOfferAsync(TestUser seller, int cryptoId, decimal amount, decimal askingPrice)
    {
        var response = await seller.Client.PostAsJsonAsync("/api/trades/offers",
            new CreateOfferDto { CryptoId = cryptoId, Amount = amount, AskingPrice = askingPrice });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MyOfferDto>())!;
    }

    private static Task<HttpResponseMessage> BuyOfferAsync(TestUser buyer, int offerId, decimal amount) =>
        buyer.Client.PostAsJsonAsync($"/api/trades/offers/{offerId}/buy", new BuyOfferDto { Amount = amount });

    private static async Task<List<MyOfferDto>> GetMyOffersAsync(TestUser user) =>
        (await user.Client.GetFromJsonAsync<List<MyOfferDto>>("/api/trades/offers/mine"))!;

    private static async Task<List<MarketOfferDto>> GetMarketAsync(TestUser user) =>
        (await user.Client.GetFromJsonAsync<List<MarketOfferDto>>("/api/trades/offers"))!;

    private static async Task<PortfolioDto> GetPortfolioAsync(TestUser user) =>
        (await user.Client.GetFromJsonAsync<PortfolioDto>($"/api/users/{user.Id}/portfolio"))!;

    private async Task<decimal> GetHoldingAmountAsync(int walletId, int cryptoId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        return await db.MyCryptos.Where(m => m.WalletId == walletId && m.CryptoId == cryptoId)
            .Select(m => m.Amount).SingleOrDefaultAsync();
    }

    private async Task<decimal> GetBalanceAsync(int walletId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        return await db.Wallets.Where(w => w.Id == walletId).Select(w => w.Balance).SingleAsync();
    }

    private async Task SetBalanceAsync(int walletId, decimal balance)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        await db.Wallets.Where(w => w.Id == walletId).ExecuteUpdateAsync(s => s.SetProperty(w => w.Balance, balance));
    }

    private async Task<(decimal Cash, decimal Coins)> TotalsAsync(int cryptoId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        var cash = await db.Wallets.SumAsync(w => w.Balance);
        var held = await db.MyCryptos.Where(m => m.CryptoId == cryptoId).SumAsync(m => (decimal?)m.Amount) ?? 0m;
        var escrow = await db.Offers.Where(o => o.CryptoId == cryptoId && o.Status != OfferStatus.Cancelled)
            .SumAsync(o => (decimal?)o.RemainingAmount) ?? 0m;
        return (cash, held + escrow);
    }

    [Fact]
    public async Task Create_Then_Cancel_RestoresHolding()
    {
        var (seller, walletId, crypto) = await SeedSellerAsync(2m);

        var offer = await CreateOfferAsync(seller, crypto.Id, 1.5m, 12m);
        Assert.Equal(0.5m, await GetHoldingAmountAsync(walletId, crypto.Id));
        Assert.Equal(OfferStatus.Open, offer.Status);

        var cancel = await seller.Client.DeleteAsync($"/api/trades/offers/{offer.Id}");

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(2m, await GetHoldingAmountAsync(walletId, crypto.Id));
        var mine = Assert.Single(await GetMyOffersAsync(seller));
        Assert.Equal(OfferStatus.Cancelled, mine.Status);
    }

    [Fact]
    public async Task Buy_PartialThenRest_MovesCashAndCoinsAndShowsStatuses()
    {
        var (seller, sellerWallet, crypto) = await SeedSellerAsync(1m);
        var buyer = await TestApi.CreateUserAsync(Factory);
        var buyerWallet = await TestApi.GetWalletIdAsync(Factory, buyer.Id);
        var offer = await CreateOfferAsync(seller, crypto.Id, 1m, 20m);

        var first = await BuyOfferAsync(buyer, offer.Id, 0.4m);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(StartBalance - 8m, await GetBalanceAsync(buyerWallet));
        Assert.Equal(StartBalance + 8m, await GetBalanceAsync(sellerWallet));
        Assert.Equal(0.4m, await GetHoldingAmountAsync(buyerWallet, crypto.Id));
        var partly = Assert.Single(await GetMyOffersAsync(seller));
        Assert.Equal(OfferStatus.PartlySold, partly.Status);
        Assert.Equal(0.4m, partly.SoldAmount);
        var onMarket = Assert.Single(await GetMarketAsync(buyer), m => m.Id == offer.Id);
        Assert.Equal(0.6m, onMarket.RemainingAmount);

        var second = await BuyOfferAsync(buyer, offer.Id, 0.6m);

        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(StartBalance - 20m, await GetBalanceAsync(buyerWallet));
        Assert.Equal(StartBalance + 20m, await GetBalanceAsync(sellerWallet));
        Assert.Equal(1m, await GetHoldingAmountAsync(buyerWallet, crypto.Id));
        Assert.Equal(OfferStatus.SoldOut, Assert.Single(await GetMyOffersAsync(seller)).Status);
        Assert.DoesNotContain(await GetMarketAsync(buyer), m => m.Id == offer.Id);
    }

    [Fact]
    public async Task Portfolio_ExcludesEscrowedCoins_AndPnlUsesBuyerPurchasePrice()
    {
        var (seller, _, crypto) = await SeedSellerAsync(2m, 10m);
        var buyer = await TestApi.CreateUserAsync(Factory);
        var offer = await CreateOfferAsync(seller, crypto.Id, 1.5m, 40m);

        var sellerPortfolio = await GetPortfolioAsync(seller);
        Assert.Equal(0.5m, Assert.Single(sellerPortfolio.Holdings).AmountOwned);

        var buy = await BuyOfferAsync(buyer, offer.Id, 1m);
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);

        var pnl = await buyer.Client.GetFromJsonAsync<List<CryptoProfitLossDto>>($"/api/users/{buyer.Id}/portfolio/profit-loss");
        var line = Assert.Single(pnl!);
        Assert.Equal(40m, line.AvgPurchasePrice);
        Assert.Equal(40m, line.TotalCostBasis);
        Assert.Equal(10m, line.CurrentValue);
    }

    [Fact]
    public async Task Buy_Returns409_WhenOfferAlreadySoldOut()
    {
        var (seller, _, crypto) = await SeedSellerAsync(1m);
        var buyer = await TestApi.CreateUserAsync(Factory);
        var latecomer = await TestApi.CreateUserAsync(Factory);
        var offer = await CreateOfferAsync(seller, crypto.Id, 1m, 10m);
        Assert.Equal(HttpStatusCode.NoContent, (await BuyOfferAsync(buyer, offer.Id, 1m)).StatusCode);

        var response = await BuyOfferAsync(latecomer, offer.Id, 0.5m);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_Returns403_ForOtherUsersOffer()
    {
        var (seller, _, crypto) = await SeedSellerAsync(1m);
        var other = await TestApi.CreateUserAsync(Factory);
        var offer = await CreateOfferAsync(seller, crypto.Id, 1m, 10m);

        var response = await other.Client.DeleteAsync($"/api/trades/offers/{offer.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Only the seller can cancel this offer.", await response.Content.ReadAsStringAsync());
        Assert.Equal(OfferStatus.Open, Assert.Single(await GetMyOffersAsync(seller)).Status);
    }

    [Fact]
    public async Task ParallelBuys_NeverOversellOffer_AndConserveMoney()
    {
        var (seller, _, crypto) = await SeedSellerAsync(1m);
        var offer = await CreateOfferAsync(seller, crypto.Id, 1m, 10m);
        var buyers = new List<TestUser>();
        for (var i = 0; i < 10; i++)
        {
            buyers.Add(await TestApi.CreateUserAsync(Factory));
        }

        var before = await TotalsAsync(crypto.Id);
        // Every buyer has 1000 cash, so exactly three 0.3 buys fit into the 1.0 offer.
        var statuses = await Task.WhenAll(buyers.Select(async b =>
        {
            using var response = await BuyOfferAsync(b, offer.Id, 0.3m);
            return response.StatusCode;
        }));
        var after = await TotalsAsync(crypto.Id);

        var successes = statuses.Count(st => st == HttpStatusCode.NoContent);
        Assert.All(statuses, st => Assert.Contains(st, new[] { HttpStatusCode.NoContent, HttpStatusCode.BadRequest, HttpStatusCode.Conflict }));
        Assert.Equal(3, successes);
        var mine = Assert.Single(await GetMyOffersAsync(seller));
        Assert.Equal(0.3m * successes, mine.SoldAmount);
        Assert.Equal(1m - 0.3m * successes, mine.RemainingAmount);
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task ParallelBuysAcrossOffers_SameBuyerWallet_NeverOverspendCash()
    {
        const int rounds = 5;
        const int offerCount = 8;
        var admin = await TestApi.CreateAdminAsync(Factory);

        for (var round = 0; round < rounds; round++)
        {
            // Each offer has its own crypto, so only the buyer wallet lock can keep the buys apart.
            var sellerWallets = new List<int>();
            var offerIds = new List<int>();
            for (var i = 0; i < offerCount; i++)
            {
                var crypto = await TestApi.CreateCryptoAsync(admin.Client, 10m);
                var seller = await TestApi.CreateUserAsync(Factory);
                var walletId = await TestApi.GetWalletIdAsync(Factory, seller.Id);
                await SeedHoldingAsync(walletId, crypto.Id, 1m);
                offerIds.Add((await CreateOfferAsync(seller, crypto.Id, 1m, 50m)).Id);
                sellerWallets.Add(walletId);
            }

            var buyer = await TestApi.CreateUserAsync(Factory);
            var buyerWallet = await TestApi.GetWalletIdAsync(Factory, buyer.Id);
            await SetBalanceAsync(buyerWallet, 60m);
            await GetMarketAsync(buyer);
            var sellerCashBefore = await SumBalancesAsync(sellerWallets);

            var gate = new TaskCompletionSource();
            var buys = offerIds.Select(async id =>
            {
                await gate.Task;
                using var response = await BuyOfferAsync(buyer, id, 1m);
                return response.StatusCode;
            }).ToList();
            gate.SetResult();
            var statuses = await Task.WhenAll(buys);

            Assert.All(statuses, st => Assert.Contains(st, new[] { HttpStatusCode.NoContent, HttpStatusCode.BadRequest }));
            Assert.Equal(1, statuses.Count(st => st == HttpStatusCode.NoContent));
            Assert.Equal(10m, await GetBalanceAsync(buyerWallet));
            Assert.Equal(1m, await SumHoldingsAsync(buyerWallet));
            Assert.Equal(50m, await SumBalancesAsync(sellerWallets) - sellerCashBefore);
        }
    }

    private async Task<decimal> SumBalancesAsync(List<int> walletIds)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        return await db.Wallets.Where(w => walletIds.Contains(w.Id)).SumAsync(w => w.Balance);
    }

    private async Task<decimal> SumHoldingsAsync(int walletId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        return await db.MyCryptos.Where(m => m.WalletId == walletId).SumAsync(m => (decimal?)m.Amount) ?? 0m;
    }

    [Fact]
    public async Task Cancel_Race_ReturnsHoldingExactlyOnce()
    {
        var (seller, walletId, crypto) = await SeedSellerAsync(1m);
        var offer = await CreateOfferAsync(seller, crypto.Id, 1m, 10m);

        var responses = await Task.WhenAll(
            seller.Client.DeleteAsync($"/api/trades/offers/{offer.Id}"),
            seller.Client.DeleteAsync($"/api/trades/offers/{offer.Id}"));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1m, await GetHoldingAmountAsync(walletId, crypto.Id));
    }

    [Fact]
    public async Task MarketList_ReturnsSellerCompletionRateAndTrades_FromRealData()
    {
        var (seller, _, crypto) = await SeedSellerAsync(3m);
        var viewer = await TestApi.CreateUserAsync(Factory);
        var soldOut = await CreateOfferAsync(seller, crypto.Id, 1m, 10m);
        var cancelled = await CreateOfferAsync(seller, crypto.Id, 1m, 10m);
        var open = await CreateOfferAsync(seller, crypto.Id, 1m, 10m);
        Assert.Equal(HttpStatusCode.NoContent, (await BuyOfferAsync(viewer, soldOut.Id, 1m)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await seller.Client.DeleteAsync($"/api/trades/offers/{cancelled.Id}")).StatusCode);

        var market = await GetMarketAsync(viewer);

        var listed = Assert.Single(market, m => m.Id == open.Id);
        Assert.Equal(0.5m, listed.SellerCompletionRate);
        Assert.Equal(1, listed.SellerTrades);
        Assert.Equal(seller.Username, listed.SellerUsername);
        Assert.Equal(10m, listed.CurrentRate);
        Assert.DoesNotContain(market, m => m.Id == soldOut.Id || m.Id == cancelled.Id);
    }

    [Fact]
    public async Task MyOffers_SerializesStatusAsString()
    {
        var (seller, _, crypto) = await SeedSellerAsync(1m);
        var buyer = await TestApi.CreateUserAsync(Factory);
        var offer = await CreateOfferAsync(seller, crypto.Id, 1m, 20m);

        Assert.Equal("Open", await GetRawStatusAsync(seller));

        Assert.Equal(HttpStatusCode.NoContent, (await BuyOfferAsync(buyer, offer.Id, 0.4m)).StatusCode);

        Assert.Equal("PartlySold", await GetRawStatusAsync(seller));
    }

    private static async Task<string?> GetRawStatusAsync(TestUser user)
    {
        var raw = await user.Client.GetStringAsync("/api/trades/offers/mine");
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        var status = doc.RootElement.EnumerateArray().Single().GetProperty("status");
        return status.ValueKind == System.Text.Json.JsonValueKind.String ? status.GetString() : status.GetRawText();
    }

    [Fact]
    public async Task ParallelCreates_ForTheSameHolding_SpendItOnlyOnce()
    {
        const int rounds = 5;
        for (var round = 0; round < rounds; round++)
        {
            var (seller, walletId, crypto) = await SeedSellerAsync(1m);
            await GetMyOffersAsync(seller);

            var gate = new TaskCompletionSource();
            var creates = Enumerable.Range(0, 2).Select(async _ =>
            {
                await gate.Task;
                using var response = await seller.Client.PostAsJsonAsync("/api/trades/offers",
                    new CreateOfferDto { CryptoId = crypto.Id, Amount = 1m, AskingPrice = 12m });
                return response.StatusCode;
            }).ToList();
            gate.SetResult();
            var statuses = await Task.WhenAll(creates);

            Assert.Single(statuses, st => st == HttpStatusCode.Created);
            Assert.Single(statuses, st => st == HttpStatusCode.BadRequest);
            Assert.Equal(0m, await GetHoldingAmountAsync(walletId, crypto.Id));
            Assert.Single(await GetMyOffersAsync(seller));
        }
    }

    [Fact]
    public async Task MarketBuy_RacingAFillOnTheSameWallet_LosesNoCash()
    {
        const int rounds = 15;
        const decimal marketCost = 10m;
        const decimal fillTotal = 25m;
        for (var round = 0; round < rounds; round++)
        {
            var (seller, sellerWallet, crypto) = await SeedSellerAsync(1m, 10m);
            var buyer = await TestApi.CreateUserAsync(Factory);
            var offer = await CreateOfferAsync(seller, crypto.Id, 1m, 50m);
            await GetMyOffersAsync(seller);

            var gate = new TaskCompletionSource();
            var market = Task.Run(async () =>
            {
                await gate.Task;
                using var response = await TestApi.BuyAsync(seller.Client, seller.Id, crypto.Id, 1m);
                return response.StatusCode;
            });
            var fill = Task.Run(async () =>
            {
                await gate.Task;
                using var response = await BuyOfferAsync(buyer, offer.Id, 0.5m);
                return response.StatusCode;
            });
            gate.SetResult();
            var statuses = await Task.WhenAll(market, fill);

            Assert.All(statuses, st => Assert.Contains(st, new[] { HttpStatusCode.OK, HttpStatusCode.NoContent }));
            Assert.Equal(StartBalance - marketCost + fillTotal, await GetBalanceAsync(sellerWallet));
            Assert.Equal(1m, await GetHoldingAmountAsync(sellerWallet, crypto.Id));
        }
    }
}
