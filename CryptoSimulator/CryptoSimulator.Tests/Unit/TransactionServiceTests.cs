using AutoMapper;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

public class TransactionServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IMapper _mapper =
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static Transactions Tx(int id, int walletId, int day) =>
        new() { Id = id, WalletId = walletId, Date = new DateTime(2026, 1, day, 0, 0, 0, DateTimeKind.Utc) };

    private TransactionService Setup(params Transactions[] txs)
    {
        var walletRepo = RepositoryFakes.WithData(new Wallet { Id = 1, UserId = 2 });
        var txRepo = RepositoryFakes.WithData(txs);
        _uow.WalletRepository.Returns(walletRepo);
        _uow.TransactionRepository.Returns(txRepo);
        return new TransactionService(_uow, _mapper);
    }

    [Fact]
    public async Task Should_ReturnNull_When_WalletMissing()
    {
        var sut = Setup();

        Assert.Null(await sut.GetUserTransactionsAsync(99));
    }

    [Fact]
    public async Task Should_ReturnEmpty_When_NoTransactions()
    {
        var sut = Setup();

        Assert.Empty((await sut.GetUserTransactionsAsync(2))!);
    }

    [Fact]
    public async Task Should_OrderNewestFirst_When_NewestFirstDefault()
    {
        var sut = Setup(Tx(1, 1, 1), Tx(2, 1, 3), Tx(3, 1, 2));

        var ids = (await sut.GetUserTransactionsAsync(2))!.Select(t => t.Id);

        Assert.Equal(new[] { 2, 3, 1 }, ids);
    }

    [Fact]
    public async Task Should_OrderOldestFirst_When_NewestFirstFalse()
    {
        var sut = Setup(Tx(1, 1, 1), Tx(2, 1, 3), Tx(3, 1, 2));

        var ids = (await sut.GetUserTransactionsAsync(2, false))!.Select(t => t.Id);

        Assert.Equal(new[] { 1, 3, 2 }, ids);
    }

    [Fact]
    public async Task Should_ExcludeOtherWalletsTransactions_When_Filtering()
    {
        var sut = Setup(Tx(1, 1, 1), Tx(2, 7, 2));

        var result = (await sut.GetUserTransactionsAsync(2))!.ToList();

        Assert.Equal(1, Assert.Single(result).Id);
    }
}
