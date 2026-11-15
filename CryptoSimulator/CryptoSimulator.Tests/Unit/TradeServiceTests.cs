using System.Linq.Expressions;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

public class TradeServiceTests
{
    private const int UserId = 1;
    private const int WalletId = 10;
    private const int CryptoId = 5;
    private static readonly DateTime Max = new(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Wallet> _wallets = Substitute.For<IRepository<Wallet>>();
    private readonly IRepository<Crypto> _cryptos = Substitute.For<IRepository<Crypto>>();
    private readonly IRepository<CryptoLog> _logs = Substitute.For<IRepository<CryptoLog>>();
    private readonly IRepository<MyCryptos> _myCryptos = Substitute.For<IRepository<MyCryptos>>();
    private readonly IRepository<Transactions> _transactions = Substitute.For<IRepository<Transactions>>();
    private readonly TradeService _sut;

    public TradeServiceTests()
    {
        _uow.WalletRepository.Returns(_wallets);
        _uow.CryptoRepository.Returns(_cryptos);
        _uow.CryptoLogRepository.Returns(_logs);
        _uow.MyCryptosRepository.Returns(_myCryptos);
        _uow.TransactionRepository.Returns(_transactions);
        _sut = new TradeService(_uow);
    }

    private static Wallet NewWallet(decimal balance) => new() { Id = WalletId, UserId = UserId, Balance = balance };

    private static CryptoLog Log(decimal value, int cryptoId = CryptoId) =>
        new() { CryptoId = cryptoId, CurrentValue = value, To = Max };

    private void SetupTrade(Wallet? wallet, Crypto? crypto, CryptoLog? log, MyCryptos? holding = null)
    {
        _wallets.Get(Arg.Any<Expression<Func<Wallet, bool>>>(), Arg.Any<string[]?>())
            .Returns(wallet is null ? [] : [wallet]);
        _cryptos.Get(Arg.Any<Expression<Func<Crypto, bool>>>(), Arg.Any<string[]?>())
            .Returns(crypto is null ? [] : [crypto]);
        _logs.Get(Arg.Any<Expression<Func<CryptoLog, bool>>>(), Arg.Any<string[]?>())
            .Returns(log is null ? [] : [log]);
        _myCryptos.Get(Arg.Any<Expression<Func<MyCryptos, bool>>>(), Arg.Any<string[]?>())
            .Returns(holding is null ? [] : [holding]);
    }

    private void SetupHappyTrade(decimal balance, decimal price, MyCryptos? holding = null) =>
        SetupTrade(NewWallet(balance), new Crypto { Id = CryptoId, Name = "Bitcoin" }, Log(price), holding);

    private void SetupWallet(Wallet? wallet) =>
        _wallets.GetAsync(Arg.Any<Expression<Func<Wallet, bool>>>(), Arg.Any<string[]?>())
            .Returns(wallet is null ? [] : [wallet]);

    private void SetupHoldings(params MyCryptos[] holdings) =>
        _myCryptos.GetAsync(Arg.Any<Expression<Func<MyCryptos, bool>>>(), Arg.Any<string[]?>())
            .Returns(holdings);

    private void SetupCurrentLogs(params CryptoLog[] logs) =>
        _logs.GetAsync(Arg.Any<Expression<Func<CryptoLog, bool>>>(), Arg.Any<string[]?>())
            .Returns(logs);

    private void SetupPurchases(params Transactions[] purchases) =>
        _transactions.GetAsync(Arg.Any<Expression<Func<Transactions, bool>>>(), Arg.Any<string[]?>())
            .Returns(purchases);

    private static MyCryptos Holding(decimal amount, int cryptoId = CryptoId, string name = "Bitcoin") =>
        new() { WalletId = WalletId, CryptoId = cryptoId, Amount = amount, Crypto = new Crypto { Id = cryptoId, Name = name } };

    private static Transactions Purchase(decimal amount, decimal rate, int cryptoId = CryptoId) =>
        new() { WalletId = WalletId, CryptoId = cryptoId, Amount = amount, ExchangeRate = rate, IsPurchase = true };

    // BuyCrypto

    [Fact]
    public async Task Should_ReturnFalse_When_BuyWalletMissing()
    {
        SetupTrade(null, new Crypto { Id = CryptoId }, Log(100));

        Assert.False(await _sut.BuyCrypto(UserId, CryptoId, 1));
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnFalse_When_BuyCryptoMissing()
    {
        SetupTrade(NewWallet(1000), null, Log(100));

        Assert.False(await _sut.BuyCrypto(UserId, CryptoId, 1));
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnFalse_When_BuyHasNoPriceLog()
    {
        SetupTrade(NewWallet(1000), new Crypto { Id = CryptoId }, null);

        Assert.False(await _sut.BuyCrypto(UserId, CryptoId, 1));
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnFalse_When_BuyBalanceInsufficient()
    {
        SetupHappyTrade(balance: 99m, price: 100m);

        Assert.False(await _sut.BuyCrypto(UserId, CryptoId, 1));
        await _transactions.DidNotReceive().InsertAsync(Arg.Any<Transactions>());
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_Buy_When_BalanceExactlyEqualsPrice()
    {
        var wallet = NewWallet(100m);
        SetupTrade(wallet, new Crypto { Id = CryptoId }, Log(100m));

        Assert.True(await _sut.BuyCrypto(UserId, CryptoId, 1));
        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public async Task Should_DeductBalanceAndCreateHolding_When_BuyingNewCrypto()
    {
        var wallet = NewWallet(1000m);
        SetupTrade(wallet, new Crypto { Id = CryptoId }, Log(100m));

        var result = await _sut.BuyCrypto(UserId, CryptoId, 2.5m);

        Assert.True(result);
        Assert.Equal(750m, wallet.Balance);
        await _wallets.Received(1).UpdateAsync(wallet);
        await _myCryptos.Received(1).InsertAsync(Arg.Is<MyCryptos>(m =>
            m.WalletId == WalletId && m.CryptoId == CryptoId && m.Amount == 2.5m));
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_RecordPurchaseTransaction_When_BuySucceeds()
    {
        SetupHappyTrade(1000m, 100m);

        await _sut.BuyCrypto(UserId, CryptoId, 2m);

        await _transactions.Received(1).InsertAsync(Arg.Is<Transactions>(t =>
            t.IsPurchase && t.Amount == 2m && t.ExchangeRate == 100m &&
            t.WalletId == WalletId && t.CryptoId == CryptoId));
    }

    [Fact]
    public async Task Should_IncreaseExistingHolding_When_BuyingOwnedCrypto()
    {
        var holding = Holding(3m);
        SetupHappyTrade(1000m, 100m, holding);

        Assert.True(await _sut.BuyCrypto(UserId, CryptoId, 2m));

        Assert.Equal(5m, holding.Amount);
        await _myCryptos.Received(1).UpdateAsync(holding);
        await _myCryptos.DidNotReceive().InsertAsync(Arg.Any<MyCryptos>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Should_ReturnFalse_When_BuyAmountNotPositive(int amount)
    {
        var wallet = NewWallet(1000m);
        SetupTrade(wallet, new Crypto { Id = CryptoId }, Log(100m));

        Assert.False(await _sut.BuyCrypto(UserId, CryptoId, amount));
        Assert.Equal(1000m, wallet.Balance);
    }

    // SellCrypto

    [Fact]
    public async Task Should_ReturnFalse_When_SellWalletMissing()
    {
        SetupTrade(null, new Crypto { Id = CryptoId }, Log(100), Holding(5));

        Assert.False(await _sut.SellCrypto(UserId, CryptoId, 1));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_SellCryptoMissing()
    {
        SetupTrade(NewWallet(0), null, Log(100), Holding(5));

        Assert.False(await _sut.SellCrypto(UserId, CryptoId, 1));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_SellHasNoPriceLog()
    {
        SetupTrade(NewWallet(0), new Crypto { Id = CryptoId }, null, Holding(5));

        Assert.False(await _sut.SellCrypto(UserId, CryptoId, 1));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_SellingUnownedCrypto()
    {
        SetupHappyTrade(0m, 100m, holding: null);

        Assert.False(await _sut.SellCrypto(UserId, CryptoId, 1));
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnFalse_When_SellAmountExceedsHoldings()
    {
        var wallet = NewWallet(0m);
        SetupTrade(wallet, new Crypto { Id = CryptoId }, Log(100m), Holding(1m));

        Assert.False(await _sut.SellCrypto(UserId, CryptoId, 1.01m));
        Assert.Equal(0m, wallet.Balance);
    }

    [Fact]
    public async Task Should_CreditBalanceAndReduceHolding_When_SellingPartially()
    {
        var wallet = NewWallet(50m);
        var holding = Holding(5m);
        SetupTrade(wallet, new Crypto { Id = CryptoId }, Log(100m), holding);

        Assert.True(await _sut.SellCrypto(UserId, CryptoId, 2m));

        Assert.Equal(250m, wallet.Balance);
        Assert.Equal(3m, holding.Amount);
        await _myCryptos.Received(1).UpdateAsync(holding);
        await _myCryptos.DidNotReceive().DeleteAsync(Arg.Any<object[]>());
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_RecordSaleTransaction_When_SellSucceeds()
    {
        SetupTrade(NewWallet(0m), new Crypto { Id = CryptoId }, Log(100m), Holding(5m));

        await _sut.SellCrypto(UserId, CryptoId, 2m);

        await _transactions.Received(1).InsertAsync(Arg.Is<Transactions>(t =>
            !t.IsPurchase && t.Amount == 2m && t.ExchangeRate == 100m));
    }

    [Fact]
    public async Task Should_DeleteHoldingByCompositeKey_When_SellingEntireAmount()
    {
        var wallet = NewWallet(0m);
        SetupTrade(wallet, new Crypto { Id = CryptoId }, Log(100m), Holding(5m));

        Assert.True(await _sut.SellCrypto(UserId, CryptoId, 5m));

        Assert.Equal(500m, wallet.Balance);
        // MyCryptos key is (WalletId, CryptoId)
        await _myCryptos.Received(1).DeleteAsync(WalletId, CryptoId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Should_ReturnFalse_When_SellAmountNotPositive(int amount)
    {
        var wallet = NewWallet(100m);
        SetupTrade(wallet, new Crypto { Id = CryptoId }, Log(100m), Holding(5m));

        Assert.False(await _sut.SellCrypto(UserId, CryptoId, amount));
        Assert.Equal(100m, wallet.Balance);
    }

    // GetPortfolio

    [Fact]
    public async Task Should_ReturnNull_When_PortfolioWalletMissing()
    {
        SetupWallet(null);

        Assert.Null(await _sut.GetPortfolio(UserId));
    }

    [Fact]
    public async Task Should_ReturnFiatOnly_When_NoHoldings()
    {
        SetupWallet(NewWallet(321m));
        SetupHoldings();

        var result = await _sut.GetPortfolio(UserId);

        Assert.NotNull(result);
        Assert.Empty(result.Holdings);
        Assert.Equal(0m, result.TotalCryptoValue);
        Assert.Equal(321m, result.TotalPortfolioValue);
    }

    [Fact]
    public async Task Should_SumHoldingValues_When_PortfolioHasHoldings()
    {
        SetupWallet(NewWallet(100m));
        SetupHoldings(Holding(2m, 5, "Bitcoin"), Holding(10m, 6, "Ether"));
        SetupCurrentLogs(Log(1000m, 5), Log(50m, 6));

        var result = await _sut.GetPortfolio(UserId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Holdings.Count);
        Assert.Equal(2500m, result.TotalCryptoValue);
        Assert.Equal(2600m, result.TotalPortfolioValue);
        Assert.Equal(100m, result.FiatBalance);
        var btc = result.Holdings.Single(h => h.CryptoId == 5);
        Assert.Equal("Bitcoin", btc.CryptoName);
        Assert.Equal(2000m, btc.CurrentValue);
        Assert.Equal(1000m, btc.CurrentExchangeRate);
    }

    [Fact]
    public async Task Should_ValueAtZero_When_NoCurrentRateForHolding()
    {
        SetupWallet(NewWallet(10m));
        SetupHoldings(Holding(2m));
        SetupCurrentLogs();

        var result = await _sut.GetPortfolio(UserId);

        Assert.NotNull(result);
        Assert.Equal(0m, result.Holdings.Single().CurrentValue);
        Assert.Equal(10m, result.TotalPortfolioValue);
    }

    [Fact]
    public async Task Should_SkipHolding_When_CryptoNavigationMissing()
    {
        var orphan = Holding(2m);
        orphan.Crypto = null!;
        SetupWallet(NewWallet(10m));
        SetupHoldings(orphan);
        SetupCurrentLogs(Log(100m));

        var result = await _sut.GetPortfolio(UserId);

        Assert.NotNull(result);
        Assert.Empty(result.Holdings);
        Assert.Equal(10m, result.TotalPortfolioValue);
    }

    // GetDetailedProfitLoss

    [Fact]
    public async Task Should_ReturnNull_When_ProfitLossWalletMissing()
    {
        SetupWallet(null);

        Assert.Null(await _sut.GetDetailedProfitLoss(UserId));
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_ProfitLossHasNoHoldings()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings();

        var result = await _sut.GetDetailedProfitLoss(UserId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Should_ComputeProfit_When_PriceAboveAveragePurchase()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(2m));
        SetupCurrentLogs(Log(150m));
        SetupPurchases(Purchase(1m, 100m), Purchase(1m, 200m)); // average 150

        var result = (await _sut.GetDetailedProfitLoss(UserId))!.Single();

        Assert.Equal(150m, result.AvgPurchasePrice);
        Assert.Equal(300m, result.TotalCostBasis);
        Assert.Equal(300m, result.CurrentValue);
        Assert.Equal(0m, result.ProfitLossAmount);
        Assert.Equal(0m, result.ProfitLossPercentage);
    }

    [Fact]
    public async Task Should_ReportGainPercentage_When_PriceRose()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(2m));
        SetupCurrentLogs(Log(150m));
        SetupPurchases(Purchase(2m, 100m));

        var result = (await _sut.GetDetailedProfitLoss(UserId))!.Single();

        Assert.Equal(200m, result.TotalCostBasis);
        Assert.Equal(300m, result.CurrentValue);
        Assert.Equal(100m, result.ProfitLossAmount);
        Assert.Equal(50m, result.ProfitLossPercentage);
    }

    [Fact]
    public async Task Should_ReportLoss_When_PriceFell()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(3m));
        SetupCurrentLogs(Log(80m));
        SetupPurchases(Purchase(3m, 100m));

        var result = (await _sut.GetDetailedProfitLoss(UserId))!.Single();

        Assert.Equal(-60m, result.ProfitLossAmount);
        Assert.Equal(-20m, result.ProfitLossPercentage);
    }

    [Fact]
    public async Task Should_RoundPercentageToTwoDecimals()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(1m));
        SetupCurrentLogs(Log(100m));
        SetupPurchases(Purchase(1m, 300m));

        var result = (await _sut.GetDetailedProfitLoss(UserId))!.Single();

        Assert.Equal(-66.67m, result.ProfitLossPercentage);
    }

    [Fact]
    public async Task Should_ReportZeroPercent_When_NoPurchaseHistory()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(2m));
        SetupCurrentLogs(Log(100m));
        SetupPurchases();

        var result = (await _sut.GetDetailedProfitLoss(UserId))!.Single();

        Assert.Equal(0m, result.AvgPurchasePrice);
        Assert.Equal(0m, result.TotalCostBasis);
        Assert.Equal(200m, result.ProfitLossAmount);
        Assert.Equal(0m, result.ProfitLossPercentage);
    }

    [Fact]
    public async Task Should_ReturnZeroAvgPrice_When_PurchaseAmountsSumToZero()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(2m));
        SetupCurrentLogs(Log(100m));
        SetupPurchases(Purchase(0m, 100m));

        var result = (await _sut.GetDetailedProfitLoss(UserId))!.Single();

        Assert.Equal(0m, result.AvgPurchasePrice);
    }

    [Fact]
    public async Task Should_UseZeroRate_When_ProfitLossHasNoCurrentLog()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(2m));
        SetupCurrentLogs();
        SetupPurchases(Purchase(2m, 100m));

        var result = (await _sut.GetDetailedProfitLoss(UserId))!.Single();

        Assert.Equal(0m, result.CurrentExchangeRate);
        Assert.Equal(-100m, result.ProfitLossPercentage);
    }

    [Fact]
    public async Task Should_SkipHolding_When_ProfitLossCryptoMissing()
    {
        var orphan = Holding(2m);
        orphan.Crypto = null!;
        SetupWallet(NewWallet(0m));
        SetupHoldings(orphan);
        SetupCurrentLogs(Log(100m));

        var result = await _sut.GetDetailedProfitLoss(UserId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // GetPortfolioProfitLoss

    [Fact]
    public async Task Should_ReturnNull_When_PortfolioProfitLossWalletMissing()
    {
        SetupWallet(null);

        Assert.Null(await _sut.GetPortfolioProfitLoss(UserId));
    }

    [Fact]
    public async Task Should_ReturnZeroedSummary_When_PortfolioProfitLossHasNoHoldings()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings();

        var result = await _sut.GetPortfolioProfitLoss(UserId);

        Assert.NotNull(result);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(0m, result.TotalCostBasis);
        Assert.Equal(0m, result.TotalProfitLossPercentage);
    }

    [Fact]
    public async Task Should_AggregateProfitLoss_When_MultipleHoldings()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(1m, 5, "Bitcoin"), Holding(2m, 6, "Ether"));
        SetupCurrentLogs(Log(200m, 5), Log(50m, 6));
        _transactions.GetAsync(Arg.Any<Expression<Func<Transactions, bool>>>(), Arg.Any<string[]?>())
            .Returns(
                new[] { Purchase(1m, 100m, 5) },
                new[] { Purchase(2m, 100m, 6) });

        var result = await _sut.GetPortfolioProfitLoss(UserId);

        Assert.NotNull(result);
        Assert.Equal(300m, result.TotalCostBasis);
        Assert.Equal(300m, result.TotalCurrentCryptoValue);
        Assert.Equal(0m, result.TotalProfitLossAmount);
        Assert.Equal(0m, result.TotalProfitLossPercentage);
    }

    [Fact]
    public async Task Should_ReportZeroPercent_When_TotalCostIsZero()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(1m));
        SetupCurrentLogs(Log(100m));
        SetupPurchases();

        var result = await _sut.GetPortfolioProfitLoss(UserId);

        Assert.NotNull(result);
        Assert.Equal(100m, result.TotalProfitLossAmount);
        Assert.Equal(0m, result.TotalProfitLossPercentage);
    }

    [Fact]
    public async Task Should_RoundTotalPercentage_When_PortfolioProfitLossComputed()
    {
        SetupWallet(NewWallet(0m));
        SetupHoldings(Holding(1m));
        SetupCurrentLogs(Log(100m));
        SetupPurchases(Purchase(1m, 300m));

        var result = await _sut.GetPortfolioProfitLoss(UserId);

        Assert.NotNull(result);
        Assert.Equal(-200m, result.TotalProfitLossAmount);
        Assert.Equal(-66.67m, result.TotalProfitLossPercentage);
    }

    // Wallet locking

    [Fact]
    public async Task Buy_BeginsLocksWalletSavesThenCommits_InOrder()
    {
        SetupHappyTrade(balance: 1000m, price: 100m);

        Assert.True(await _sut.BuyCrypto(UserId, CryptoId, 1));

        Received.InOrder(() =>
        {
            _uow.BeginTransactionAsync(Arg.Any<CancellationToken>());
            _uow.LockOfferAndWalletsAsync(0, WalletId, WalletId, Arg.Any<CancellationToken>());
            _uow.SaveAsync();
            _uow.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Buy_DoesNotCommit_When_BalanceInsufficient()
    {
        SetupHappyTrade(balance: 1m, price: 100m);

        Assert.False(await _sut.BuyCrypto(UserId, CryptoId, 1));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sell_BeginsLocksWalletSavesThenCommits_InOrder()
    {
        SetupHappyTrade(balance: 0m, price: 100m, Holding(2m));

        Assert.True(await _sut.SellCrypto(UserId, CryptoId, 1));

        Received.InOrder(() =>
        {
            _uow.BeginTransactionAsync(Arg.Any<CancellationToken>());
            _uow.LockOfferAndWalletsAsync(0, WalletId, WalletId, Arg.Any<CancellationToken>());
            _uow.SaveAsync();
            _uow.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Sell_DoesNotCommit_When_HoldingTooSmall()
    {
        SetupHappyTrade(balance: 0m, price: 100m, Holding(0.5m));

        Assert.False(await _sut.SellCrypto(UserId, CryptoId, 1));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
