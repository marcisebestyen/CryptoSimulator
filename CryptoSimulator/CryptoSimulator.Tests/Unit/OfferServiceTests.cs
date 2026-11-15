using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Results;
using CryptoSimulator.Services;
using System.Linq.Expressions;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

public class OfferServiceTests
{
    private const int UserId = 1;
    private const int OtherUserId = 2;
    private const int WalletId = 10;
    private const int CryptoId = 5;
    private const int OfferId = 77;
    private static readonly DateTime Max = new(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private void Arrange(MyCryptos? holding = null, Offer? offer = null, bool withPrice = true)
    {
        var wallet = new Wallet { Id = WalletId, UserId = UserId };
        var crypto = new Crypto { Id = CryptoId, Name = "Bitcoin" };
        var logs = withPrice
            ? new[] { new CryptoLog { CryptoId = CryptoId, CurrentValue = 95000m, To = Max } }
            : Array.Empty<CryptoLog>();
        var wallets = RepositoryFakes.WithData(wallet);
        var cryptos = RepositoryFakes.WithData(crypto);
        var cryptoLogs = RepositoryFakes.WithData(logs);
        var holdings = RepositoryFakes.WithData(holding is null ? Array.Empty<MyCryptos>() : [holding]);
        var offers = RepositoryFakes.WithData(offer is null ? Array.Empty<Offer>() : [offer]);
        _uow.WalletRepository.Returns(wallets);
        _uow.CryptoRepository.Returns(cryptos);
        _uow.CryptoLogRepository.Returns(cryptoLogs);
        _uow.MyCryptosRepository.Returns(holdings);
        _uow.OfferRepository.Returns(offers);
        var disposable = Substitute.For<IAsyncDisposable>();
        _uow.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(disposable));
        _uow.LockOfferAndWalletsAsync(default, default, default, default).ReturnsForAnyArgs(Task.CompletedTask);
    }

    private static Offer NewOffer(OfferStatus status, decimal remaining, int sellerWalletId = WalletId) => new()
    {
        Id = OfferId,
        SellerWalletId = sellerWalletId,
        CryptoId = CryptoId,
        Crypto = new Crypto { Id = CryptoId, Name = "Bitcoin" },
        Amount = 0.5m,
        RemainingAmount = remaining,
        AskingPrice = 90000m,
        Status = status
    };

    private static MyCryptos Holding(decimal amount) => new() { WalletId = WalletId, CryptoId = CryptoId, Amount = amount };

    private OfferService Sut() => new(_uow);

    [Fact]
    public async Task Create_ReducesHoldingAndStoresOpenOffer_WhenValid()
    {
        var holding = Holding(1.0m);
        Arrange(holding);
        Offer? stored = null;
        var offers = RepositoryFakes.WithData<Offer>();
        offers.When(r => r.InsertAsync(Arg.Any<Offer>())).Do(c => stored = c.Arg<Offer>());
        _uow.OfferRepository.Returns(offers);

        var result = await Sut().CreateAsync(UserId, new CreateOfferDto { CryptoId = CryptoId, Amount = 0.25m, AskingPrice = 90000m });

        Assert.Equal(OfferResultStatus.Success, result.Status);
        Assert.Equal(0.75m, holding.Amount);
        Assert.NotNull(stored);
        Assert.Equal(OfferStatus.Open, stored!.Status);
        Assert.Equal(0.25m, stored.RemainingAmount);
        Assert.Equal(0.25m, stored.Amount);
        Assert.Equal(WalletId, stored.SellerWalletId);
        Assert.Equal(95000m, result.Offer!.CurrentRate);
        await _uow.Received(1).LockOfferAndWalletsAsync(Arg.Any<int>(), WalletId, WalletId, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveAsync();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_DeletesHolding_WhenAllCoinsOffered()
    {
        var holding = Holding(0.25m);
        Arrange(holding);

        var result = await Sut().CreateAsync(UserId, new CreateOfferDto { CryptoId = CryptoId, Amount = 0.25m, AskingPrice = 90000m });

        Assert.Equal(OfferResultStatus.Success, result.Status);
        await _uow.MyCryptosRepository.Received(1).DeleteAsync(WalletId, CryptoId);
    }

    [Fact]
    public async Task Create_ReturnsInvalid_WhenAmountExceedsHolding()
    {
        Arrange(Holding(0.1m));

        var result = await Sut().CreateAsync(UserId, new CreateOfferDto { CryptoId = CryptoId, Amount = 0.25m, AskingPrice = 90000m });

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Create_ReturnsInvalid_WhenCryptoHasNoPrice()
    {
        Arrange(Holding(1m), withPrice: false);

        var result = await Sut().CreateAsync(UserId, new CreateOfferDto { CryptoId = CryptoId, Amount = 0.25m, AskingPrice = 90000m });

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Create_ReturnsInvalid_WhenAmountHasMoreThanFourDecimals()
    {
        Arrange(Holding(1m));

        var result = await Sut().CreateAsync(UserId, new CreateOfferDto { CryptoId = CryptoId, Amount = 0.12345m, AskingPrice = 90000m });

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Theory]
    [InlineData(0.123456789)]
    public async Task Create_ReturnsInvalid_WhenPriceHasMoreThanEightDecimals(double price)
    {
        Arrange(Holding(1m));

        var result = await Sut().CreateAsync(UserId, new CreateOfferDto { CryptoId = CryptoId, Amount = 0.25m, AskingPrice = (decimal)price });

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Create_ReturnsInvalid_WhenPriceTooLarge()
    {
        Arrange(Holding(1m));

        var result = await Sut().CreateAsync(UserId, new CreateOfferDto { CryptoId = CryptoId, Amount = 0.25m, AskingPrice = 10_000_000_000m });

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Cancel_ReturnsRemainingCoinsToHolding_AndSetsCancelled()
    {
        var holding = Holding(1.0m);
        var offer = NewOffer(OfferStatus.PartlySold, 0.15m);
        Arrange(holding, offer);

        var result = await Sut().CancelAsync(UserId, OfferId);

        Assert.Equal(OfferResultStatus.Success, result.Status);
        Assert.Equal(1.15m, holding.Amount);
        Assert.Equal(OfferStatus.Cancelled, offer.Status);
        Assert.NotNull(offer.ClosedAt);
        Assert.Equal(0.35m, result.Offer!.SoldAmount);
        await _uow.Received(1).LockOfferAndWalletsAsync(OfferId, WalletId, WalletId, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveAsync();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_RecreatesHolding_WhenHoldingWasDeleted()
    {
        Arrange(null, NewOffer(OfferStatus.Open, 0.5m));

        var result = await Sut().CancelAsync(UserId, OfferId);

        Assert.Equal(OfferResultStatus.Success, result.Status);
        await _uow.MyCryptosRepository.Received(1).InsertAsync(
            Arg.Is<MyCryptos>(m => m.WalletId == WalletId && m.CryptoId == CryptoId && m.Amount == 0.5m));
    }

    [Fact]
    public async Task Cancel_ReturnsNotOwner_WhenCallerIsNotSeller()
    {
        Arrange(Holding(1m), NewOffer(OfferStatus.Open, 0.5m));

        var result = await Sut().CancelAsync(OtherUserId, OfferId);

        Assert.Equal(OfferResultStatus.NotOwner, result.Status);
        Assert.Equal("Only the seller can cancel this offer.", result.Error);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Theory]
    [InlineData(OfferStatus.SoldOut)]
    [InlineData(OfferStatus.Cancelled)]
    public async Task Cancel_ReturnsConflict_WhenOfferSoldOutOrCancelled(OfferStatus status)
    {
        Arrange(Holding(1m), NewOffer(status, 0m));

        var result = await Sut().CancelAsync(UserId, OfferId);

        Assert.Equal(OfferResultStatus.Conflict, result.Status);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Cancel_ReturnsNotFound_WhenMissing()
    {
        Arrange(Holding(1m));

        var result = await Sut().CancelAsync(UserId, OfferId);

        Assert.Equal(OfferResultStatus.NotFound, result.Status);
    }

    private const int SellerWalletId = 20;
    private const int SellerUserId = 3;

    private static Offer SellerOffer(int id, OfferStatus status, decimal remaining, DateTime? createdAt = null) => new()
    {
        Id = id,
        SellerWalletId = SellerWalletId,
        SellerWallet = new Wallet { Id = SellerWalletId, UserId = SellerUserId, User = new User { Id = SellerUserId, Username = "seller" } },
        CryptoId = CryptoId,
        Crypto = new Crypto { Id = CryptoId, Name = "Bitcoin" },
        Amount = 1m,
        RemainingAmount = remaining,
        AskingPrice = 90000m,
        Status = status,
        CreatedAt = createdAt ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static OfferFill Fill(Offer offer, decimal amount = 0.1m) => new()
    {
        Id = offer.Id * 100 + (int)(amount * 10),
        OfferId = offer.Id,
        Offer = offer,
        Amount = amount,
        Price = offer.AskingPrice
    };

    private void ArrangeMarket(Offer[] offers, params OfferFill[] fills)
    {
        var wallets = RepositoryFakes.WithData(new Wallet { Id = WalletId, UserId = UserId });
        var offerRepo = RepositoryFakes.WithData(offers);
        var fillRepo = RepositoryFakes.WithData(fills);
        var logs = RepositoryFakes.WithData(
            new CryptoLog { CryptoId = CryptoId, CurrentValue = 95000m, To = Max },
            new CryptoLog { CryptoId = CryptoId, CurrentValue = 1m, To = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        _uow.WalletRepository.Returns(wallets);
        _uow.OfferRepository.Returns(offerRepo);
        _uow.OfferFillRepository.Returns(fillRepo);
        _uow.CryptoLogRepository.Returns(logs);
    }

    [Fact]
    public async Task GetMarketOffers_ExcludesOwnAndFinishedOffers()
    {
        var visible = SellerOffer(1, OfferStatus.Open, 1m);
        var partly = SellerOffer(2, OfferStatus.PartlySold, 0.5m);
        var own = NewOffer(OfferStatus.Open, 0.5m);
        own.Id = 3;
        ArrangeMarket([visible, partly, own, SellerOffer(4, OfferStatus.SoldOut, 0m), SellerOffer(5, OfferStatus.Cancelled, 1m)]);

        var result = await Sut().GetMarketOffersAsync(UserId);

        Assert.Equal(new[] { 1, 2 }, result.Select(o => o.Id).ToArray());
        Assert.All(result, o => Assert.Equal("seller", o.SellerUsername));
        Assert.All(result, o => Assert.Equal(95000m, o.CurrentRate));
    }

    [Fact]
    public async Task GetMarketOffers_ComputesCompletionRate()
    {
        var open = SellerOffer(1, OfferStatus.Open, 1m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var sold = SellerOffer(2, OfferStatus.SoldOut, 0m);
        var partlyCancelled = SellerOffer(3, OfferStatus.Cancelled, 0.5m);
        var cancelledNoSale = SellerOffer(4, OfferStatus.Cancelled, 1m);
        ArrangeMarket([open, sold, partlyCancelled, cancelledNoSale], Fill(sold), Fill(partlyCancelled));

        var result = await Sut().GetMarketOffersAsync(UserId);

        var dto = Assert.Single(result);
        Assert.Equal(2m / 3m, dto.SellerCompletionRate);
    }

    [Fact]
    public async Task GetMarketOffers_RateIsNull_WhenNoSalesAndNoCancels()
    {
        ArrangeMarket([SellerOffer(1, OfferStatus.Open, 1m)]);

        var result = await Sut().GetMarketOffersAsync(UserId);

        var dto = Assert.Single(result);
        Assert.Null(dto.SellerCompletionRate);
        Assert.Equal(0, dto.SellerTrades);
    }

    [Fact]
    public async Task GetMarketOffers_CountsTradesAsFillsWhereUserWasSeller()
    {
        var open = SellerOffer(1, OfferStatus.PartlySold, 0.4m);
        var sold = SellerOffer(2, OfferStatus.SoldOut, 0m);
        ArrangeMarket([open, sold], Fill(open, 0.2m), Fill(open, 0.4m), Fill(sold));

        var result = await Sut().GetMarketOffersAsync(UserId);

        var dto = Assert.Single(result);
        Assert.Equal(3, dto.SellerTrades);
    }

    [Fact]
    public async Task GetMyOffers_ReportsPartlySoldWithSoldAmount()
    {
        var older = NewOffer(OfferStatus.SoldOut, 0m);
        older.Id = 1;
        older.CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = NewOffer(OfferStatus.PartlySold, 0.2m);
        newer.Id = 2;
        newer.CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        ArrangeMarket([older, newer, SellerOffer(3, OfferStatus.Open, 1m)]);

        var result = await Sut().GetMyOffersAsync(UserId);

        Assert.Equal(new[] { 2, 1 }, result.Select(o => o.Id).ToArray());
        Assert.Equal(0.3m, result[0].SoldAmount);
        Assert.Equal(OfferStatus.PartlySold, result[0].Status);
        Assert.Equal(95000m, result[0].CurrentRate);
    }

    private sealed record BuyArrangement(
        Wallet Buyer, Wallet Seller, Offer Offer, List<Transactions> Transactions, List<OfferFill> Fills, MyCryptos? BuyerHolding);

    private BuyArrangement ArrangeBuy(
        decimal buyerBalance = 1_000_000m, OfferStatus status = OfferStatus.Open, decimal remaining = 1m,
        decimal price = 90000m, MyCryptos? buyerHolding = null, int sellerWalletId = SellerWalletId)
    {
        var buyer = new Wallet { Id = WalletId, UserId = UserId, Balance = buyerBalance };
        var seller = new Wallet { Id = SellerWalletId, UserId = SellerUserId, Balance = 100m };
        var offer = SellerOffer(OfferId, status, remaining);
        offer.AskingPrice = price;
        offer.SellerWalletId = sellerWalletId;
        var transactions = new List<Transactions>();
        var fills = new List<OfferFill>();
        var transactionRepo = RepositoryFakes.WithData<Transactions>();
        transactionRepo.When(r => r.InsertAsync(Arg.Any<Transactions>())).Do(c => transactions.Add(c.Arg<Transactions>()));
        var fillRepo = RepositoryFakes.WithData<OfferFill>();
        fillRepo.When(r => r.InsertAsync(Arg.Any<OfferFill>())).Do(c => fills.Add(c.Arg<OfferFill>()));
        var wallets = RepositoryFakes.WithData(buyer, seller);
        var offers = RepositoryFakes.WithData(offer);
        var holdings = RepositoryFakes.WithData(buyerHolding is null ? Array.Empty<MyCryptos>() : [buyerHolding]);
        _uow.WalletRepository.Returns(wallets);
        _uow.OfferRepository.Returns(offers);
        _uow.MyCryptosRepository.Returns(holdings);
        _uow.TransactionRepository.Returns(transactionRepo);
        _uow.OfferFillRepository.Returns(fillRepo);
        var disposable = Substitute.For<IAsyncDisposable>();
        _uow.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(disposable));
        _uow.LockOfferAndWalletsAsync(default, default, default, default).ReturnsForAnyArgs(Task.CompletedTask);
        return new BuyArrangement(buyer, seller, offer, transactions, fills, buyerHolding);
    }

    private async Task AssertNothingWritten()
    {
        await _uow.DidNotReceive().SaveAsync();
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Buy_MovesCashAndCoins_AndWritesTwoTransactionsAndFill()
    {
        var holding = Holding(2m);
        var a = ArrangeBuy(buyerHolding: holding);

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Success, result.Status);
        Assert.Equal(1_000_000m - 22500m, a.Buyer.Balance);
        Assert.Equal(100m + 22500m, a.Seller.Balance);
        Assert.Equal(2.25m, holding.Amount);
        Assert.Equal(0.75m, a.Offer.RemainingAmount);
        Assert.Equal(2, a.Transactions.Count);
        var buyerRow = Assert.Single(a.Transactions, t => t.IsPurchase);
        var sellerRow = Assert.Single(a.Transactions, t => !t.IsPurchase);
        Assert.Equal(WalletId, buyerRow.WalletId);
        Assert.Equal(SellerWalletId, sellerRow.WalletId);
        Assert.All(a.Transactions, t =>
        {
            Assert.Equal(0.25m, t.Amount);
            Assert.Equal(90000m, t.ExchangeRate);
            Assert.Equal(CryptoId, t.CryptoId);
        });
        var fill = Assert.Single(a.Fills);
        Assert.Equal(OfferId, fill.OfferId);
        Assert.Equal(WalletId, fill.BuyerWalletId);
        Assert.Equal(0.25m, fill.Amount);
        Assert.Equal(90000m, fill.Price);
        await _uow.Received(1).LockOfferAndWalletsAsync(OfferId, SellerWalletId, WalletId, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveAsync();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Buy_SetsPartlySold_WhenPartBought()
    {
        var a = ArrangeBuy(buyerHolding: Holding(0m));

        await Sut().BuyAsync(UserId, OfferId, 0.4m);

        Assert.Equal(OfferStatus.PartlySold, a.Offer.Status);
        Assert.Equal(0.6m, a.Offer.RemainingAmount);
        Assert.Null(a.Offer.ClosedAt);
    }

    [Fact]
    public async Task Buy_SetsSoldOutAndClosedAt_WhenAllBought()
    {
        var a = ArrangeBuy(remaining: 0.4m, status: OfferStatus.PartlySold, buyerHolding: Holding(0m));

        await Sut().BuyAsync(UserId, OfferId, 0.4m);

        Assert.Equal(OfferStatus.SoldOut, a.Offer.Status);
        Assert.Equal(0m, a.Offer.RemainingAmount);
        Assert.NotNull(a.Offer.ClosedAt);
    }

    [Fact]
    public async Task Buy_CreatesHolding_WhenBuyerHasNone()
    {
        ArrangeBuy();

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Success, result.Status);
        await _uow.MyCryptosRepository.Received(1).InsertAsync(
            Arg.Is<MyCryptos>(m => m.WalletId == WalletId && m.CryptoId == CryptoId && m.Amount == 0.25m));
        await _uow.MyCryptosRepository.DidNotReceive().InsertAsync(Arg.Is<MyCryptos>(m => m.WalletId == SellerWalletId));
        await _uow.MyCryptosRepository.DidNotReceive().UpdateAsync(Arg.Is<MyCryptos>(m => m.WalletId == SellerWalletId));
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenAmountExceedsRemaining()
    {
        var a = ArrangeBuy(remaining: 0.2m);

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        Assert.Equal(0.2m, a.Offer.RemainingAmount);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenNotEnoughCash()
    {
        var a = ArrangeBuy(buyerBalance: 22499.99m);

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        Assert.Equal(22499.99m, a.Buyer.Balance);
        Assert.Empty(a.Transactions);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenAmountHasMoreThanFourDecimals()
    {
        ArrangeBuy();

        var result = await Sut().BuyAsync(UserId, OfferId, 0.12345m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await AssertNothingWritten();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Buy_ReturnsInvalid_WhenAmountNotPositive(double amount)
    {
        ArrangeBuy();

        var result = await Sut().BuyAsync(UserId, OfferId, (decimal)amount);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenBuyingOwnOffer()
    {
        ArrangeBuy(sellerWalletId: WalletId);

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await AssertNothingWritten();
    }

    [Theory]
    [InlineData(OfferStatus.SoldOut)]
    [InlineData(OfferStatus.Cancelled)]
    public async Task Buy_ReturnsConflict_WhenOfferNotOpen(OfferStatus status)
    {
        ArrangeBuy(status: status, remaining: 0m);

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Conflict, result.Status);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_ReturnsNotFound_WhenOfferMissing()
    {
        ArrangeBuy();

        var result = await Sut().BuyAsync(UserId, OfferId + 1, 0.25m);

        Assert.Equal(OfferResultStatus.NotFound, result.Status);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_RoundsTotalToTwoDecimals()
    {
        var a = ArrangeBuy(price: 1.01m, buyerBalance: 10m);

        await Sut().BuyAsync(UserId, OfferId, 0.5m);

        Assert.Equal(10m - 0.51m, a.Buyer.Balance);
        Assert.Equal(100m + 0.51m, a.Seller.Balance);
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenTotalRoundsToZero()
    {
        var a = ArrangeBuy(price: 0.01m);

        var result = await Sut().BuyAsync(UserId, OfferId, 0.0001m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        Assert.Equal(1m, a.Offer.RemainingAmount);
        Assert.Equal(1_000_000m, a.Buyer.Balance);
        Assert.Equal(100m, a.Seller.Balance);
        Assert.Empty(a.Transactions);
        await AssertNothingWritten();
    }

    // Pre-lock reads return the stale copies; reads after the lock return the fresh ones.
    private (Offer Fresh, Wallet FreshBuyer) ArrangeStaleReads(BuyArrangement a)
    {
        var freshOffer = SellerOffer(OfferId, a.Offer.Status, a.Offer.RemainingAmount);
        freshOffer.AskingPrice = a.Offer.AskingPrice;
        var freshBuyer = new Wallet { Id = a.Buyer.Id, UserId = UserId, Balance = a.Buyer.Balance };
        var locked = false;
        _uow.LockOfferAndWalletsAsync(default, default, default, default).ReturnsForAnyArgs(_ =>
        {
            locked = true;
            return Task.CompletedTask;
        });
        var offers = Substitute.For<IRepository<Offer>>();
        offers.GetAsync(Arg.Any<Expression<Func<Offer, bool>>>(), Arg.Any<string[]?>()).Returns(c =>
            Task.FromResult<IEnumerable<Offer>>(new[] { locked ? freshOffer : a.Offer }.Where(c.Arg<Expression<Func<Offer, bool>>>().Compile()).ToList()));
        var wallets = Substitute.For<IRepository<Wallet>>();
        wallets.GetAsync(Arg.Any<Expression<Func<Wallet, bool>>>(), Arg.Any<string[]?>()).Returns(c =>
            Task.FromResult<IEnumerable<Wallet>>(new[] { locked ? freshBuyer : a.Buyer, a.Seller }.Where(c.Arg<Expression<Func<Wallet, bool>>>().Compile()).ToList()));
        _uow.OfferRepository.Returns(offers);
        _uow.WalletRepository.Returns(wallets);
        return (freshOffer, freshBuyer);
    }

    [Fact]
    public async Task Buy_ReturnsConflict_WhenOfferSoldOutWhileWaitingForLock()
    {
        var a = ArrangeBuy();
        var (fresh, _) = ArrangeStaleReads(a);
        fresh.Status = OfferStatus.SoldOut;
        fresh.RemainingAmount = 0m;

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Conflict, result.Status);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenCashDrainedWhileWaitingForLock()
    {
        var a = ArrangeBuy();
        var (_, freshBuyer) = ArrangeStaleReads(a);
        freshBuyer.Balance = 1m;

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        Assert.Equal(1m, freshBuyer.Balance);
        Assert.Equal(100m, a.Seller.Balance);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenRemainingShrankWhileWaitingForLock()
    {
        var a = ArrangeBuy();
        var (fresh, _) = ArrangeStaleReads(a);
        fresh.RemainingAmount = 0.1m;
        fresh.Status = OfferStatus.PartlySold;

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task Buy_BeginsLocksSavesThenCommits_InOrder()
    {
        ArrangeBuy();

        await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Received.InOrder(() =>
        {
            _uow.BeginTransactionAsync(Arg.Any<CancellationToken>());
            _uow.LockOfferAndWalletsAsync(OfferId, SellerWalletId, WalletId, Arg.Any<CancellationToken>());
            _uow.SaveAsync();
            _uow.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Buy_ReturnsInvalid_WhenSellerCreditWouldOverflowBalanceColumn()
    {
        var a = ArrangeBuy();
        a.Seller.Balance = 9_999_999_999_999_999.99m;

        var result = await Sut().BuyAsync(UserId, OfferId, 0.25m);

        Assert.Equal(OfferResultStatus.Invalid, result.Status);
        Assert.Equal(1_000_000m, a.Buyer.Balance);
        Assert.Equal(9_999_999_999_999_999.99m, a.Seller.Balance);
        await AssertNothingWritten();
    }

    [Fact]
    public async Task GetMyOffers_AsksTheDatabaseOnlyForCurrentRateLogs()
    {
        Expression<Func<CryptoLog, bool>>? captured = null;
        ArrangeMarket([NewOffer(OfferStatus.Open, 0.5m)]);
        var logs = Substitute.For<IRepository<CryptoLog>>();
        logs.GetAsync(Arg.Do<Expression<Func<CryptoLog, bool>>>(p => captured = p), Arg.Any<string[]?>())
            .Returns(Task.FromResult<IEnumerable<CryptoLog>>([new CryptoLog { CryptoId = CryptoId, CurrentValue = 95000m, To = Max }]));
        _uow.CryptoLogRepository.Returns(logs);

        var result = await Sut().GetMyOffersAsync(UserId);

        Assert.Equal(95000m, Assert.Single(result).CurrentRate);
        var matches = captured!.Compile();
        Assert.True(matches(new CryptoLog { CryptoId = CryptoId, To = Max }));
        Assert.False(matches(new CryptoLog { CryptoId = CryptoId, To = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }));
    }
}
