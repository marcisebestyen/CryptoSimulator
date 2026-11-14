using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

public class WalletServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private WalletService Setup(Wallet[] wallets, params MyCryptos[] holdings)
    {
        var walletRepo = RepositoryFakes.WithData(wallets);
        var holdingRepo = RepositoryFakes.WithData(holdings);
        _uow.WalletRepository.Returns(walletRepo);
        _uow.MyCryptosRepository.Returns(holdingRepo);
        return new WalletService(_uow);
    }

    [Fact]
    public async Task Should_ReturnNull_When_WalletMissing()
    {
        var sut = Setup(new[] { new Wallet { Id = 1, UserId = 2 } });

        Assert.Null(await sut.GetUserWalletDetailsAsync(5));
    }

    [Fact]
    public async Task Should_ReturnBalanceAndEmptyList_When_NoHoldings()
    {
        var sut = Setup(new[] { new Wallet { Id = 1, UserId = 2, Balance = 50m } });

        var dto = await sut.GetUserWalletDetailsAsync(2);

        Assert.Equal(2, dto!.UserId);
        Assert.Equal(50m, dto.FiatBalance);
        Assert.Empty(dto.OwnedCryptos);
    }

    [Fact]
    public async Task Should_ListOnlyPositiveHoldingsOfWallet_When_HoldingsExist()
    {
        var sut = Setup(
            new[] { new Wallet { Id = 1, UserId = 2 } },
            new MyCryptos { WalletId = 1, CryptoId = 10, Amount = 3m, Crypto = new Crypto { Id = 10, Name = "BTC" } },
            new MyCryptos { WalletId = 1, CryptoId = 11, Amount = 0m, Crypto = new Crypto { Id = 11, Name = "ETH" } },
            new MyCryptos { WalletId = 9, CryptoId = 12, Amount = 4m, Crypto = new Crypto { Id = 12, Name = "XRP" } });

        var dto = await sut.GetUserWalletDetailsAsync(2);

        var owned = Assert.Single(dto!.OwnedCryptos);
        Assert.Equal(10, owned.CryptoId);
        Assert.Equal("BTC", owned.CryptoName);
        Assert.Equal(3m, owned.Amount);
    }

    [Fact]
    public async Task Should_SkipHolding_When_CryptoNotLoaded()
    {
        var sut = Setup(
            new[] { new Wallet { Id = 1, UserId = 2 } },
            new MyCryptos { WalletId = 1, CryptoId = 10, Amount = 3m, Crypto = null! });

        var dto = await sut.GetUserWalletDetailsAsync(2);

        Assert.Empty(dto!.OwnedCryptos);
    }
}
