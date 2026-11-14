using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CryptoSimulator.Tests.Unit;

public class MappingProfileTests
{
    private static MapperConfiguration Config() =>
        new(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

    private static IMapper Mapper() => Config().CreateMapper();

    [Fact]
    public void Should_BeValid_When_ConfigurationAsserted()
    {
        Config().AssertConfigurationIsValid();
    }

    [Fact]
    public void Should_MapCurrentValueToValue_When_MappingCryptoLog()
    {
        var log = new CryptoLog { CurrentValue = 5.5m, From = DateTime.UnixEpoch, To = DateTime.MaxValue };

        var dto = Mapper().Map<CryptoPriceHistoryDto>(log);

        Assert.Equal(5.5m, dto.Value);
        Assert.Equal(log.From, dto.From);
        Assert.Equal(log.To, dto.To);
    }

    [Fact]
    public void Should_MapCryptoName_When_MappingPriceAlert()
    {
        var alert = new PriceAlert { Id = 1, Crypto = new Crypto { Name = "BTC" } };

        Assert.Equal("BTC", Mapper().Map<PriceAlertGetDto>(alert).CryptoName);
    }

    [Fact]
    public void Should_MapNullCryptoName_When_AlertCryptoMissing()
    {
        var alert = new PriceAlert { Id = 1, Crypto = null! };

        Assert.Null(Mapper().Map<PriceAlertGetDto>(alert).CryptoName);
    }

    [Fact]
    public void Should_NotExposePassword_When_MappingUserToDto()
    {
        var user = new User { Id = 4, Username = "u", Email = "e@x.com", Role = Role.Admin, Password = "p" };

        var dto = Mapper().Map<UserGetDto>(user);

        Assert.Equal(4, dto.Id);
        Assert.Equal(Role.Admin, dto.Role);
        Assert.Equal("e@x.com", dto.Email);
        Assert.DoesNotContain(typeof(UserGetDto).GetProperties(), p => p.Name == "Password");
    }

    [Fact]
    public void Should_KeepExistingValues_When_PutDtoFieldsNullOrEmpty()
    {
        var user = new User { Username = "old", FirstName = "F", LastName = "L", Email = "old@x.com" };
        var put = new UserPutDto { Username = "", FirstName = null!, LastName = "New" };

        Mapper().Map(put, user);

        Assert.Equal("old", user.Username);
        Assert.Equal("F", user.FirstName);
        Assert.Equal("New", user.LastName);
        Assert.Equal("old@x.com", user.Email);
    }

    [Fact]
    public void Should_MapTransactionTypeAndFields_When_MappingTransaction()
    {
        var tx = new Transactions { Id = 2, WalletId = 3, CryptoId = 4, Amount = 1m, IsPurchase = true };

        var dto = Mapper().Map<TransactionsGetDto>(tx);

        Assert.Equal(2, dto.Id);
        Assert.Equal("Purchase", dto.TransactionType);
    }

    [Fact]
    public void Should_MapWalletAndCrypto_When_MappingEntities()
    {
        var m = Mapper();

        Assert.Equal(9m, m.Map<WalletGetDto>(new Wallet { Id = 1, Balance = 9m, UserId = 2 }).Balance);
        Assert.Equal("ETH", m.Map<CryptoGetDto>(new Crypto { Id = 1, Name = "ETH" }).Name);
    }
}
