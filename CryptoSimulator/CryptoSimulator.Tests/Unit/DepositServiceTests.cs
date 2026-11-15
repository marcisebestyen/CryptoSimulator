using System.Linq.Expressions;
using System.Text.Json;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Results;
using CryptoSimulator.Services;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

public class DepositServiceTests
{
    private const int UserId = 1;
    private const int WalletId = 10;
    private const string ValidNumber = "4532710938428896";
    private const string Cvv = "123";

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Wallet> _wallets = Substitute.For<IRepository<Wallet>>();
    private readonly Wallet _wallet = new() { Id = WalletId, UserId = UserId, Balance = 0m };
    private readonly List<SavedCard> _cardData = [];
    private readonly List<Deposit> _depositData = [];
    private readonly IRepository<SavedCard> _cards;
    private readonly IRepository<Deposit> _deposits;
    private readonly DepositService _sut;

    public DepositServiceTests()
    {
        _cards = Substitute.For<IRepository<SavedCard>>();
        _deposits = Substitute.For<IRepository<Deposit>>();
        WireList(_cards, _cardData);
        WireList(_deposits, _depositData);
        _wallets.Get(Arg.Any<Expression<Func<Wallet, bool>>>(), Arg.Any<string[]?>()).Returns([_wallet]);
        _uow.WalletRepository.Returns(_wallets);
        _uow.SavedCardRepository.Returns(_cards);
        _uow.DepositRepository.Returns(_deposits);
        _sut = new DepositService(_uow);
    }

    private static void WireList<T>(IRepository<T> repo, List<T> data) where T : class
    {
        repo.GetAsync(Arg.Any<Expression<Func<T, bool>>>(), Arg.Any<string[]?>())
            .Returns(call =>
            {
                var predicate = call.Arg<Expression<Func<T, bool>>>().Compile();
                return Task.FromResult<IEnumerable<T>>(data.Where(predicate).ToList());
            });
    }

    private static DepositCreateDto NewCardDto(bool save = false, decimal amount = 500m) => new()
    {
        UserId = UserId,
        Amount = amount,
        HolderName = "Jane Doe",
        CardNumber = ValidNumber,
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 2,
        Cvv = Cvv,
        SaveCard = save
    };

    private static SavedCard Card(int id, int userId = UserId) => new()
    {
        Id = id,
        UserId = userId,
        HolderName = "Jane Doe",
        Last4 = "8896",
        Network = CardNetwork.Visa,
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 2
    };

    private List<T> Inserted<T>(IRepository<T> repo) where T : class =>
        repo.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRepository<T>.InsertAsync))
            .Select(c => (T)c.GetArguments()[0]!)
            .ToList();

    [Fact]
    public async Task Should_AddAmountToBalance_AndInsertDeposit_When_NewCardValid()
    {
        var before = DateTime.UtcNow;

        var outcome = await _sut.DepositAsync(NewCardDto());

        Assert.Equal(DepositStatus.Success, outcome.Status);
        Assert.Equal(500m, _wallet.Balance);
        Assert.Equal(500m, outcome.Deposit!.NewBalance);
        var deposit = Assert.Single(Inserted(_deposits));
        Assert.Equal(WalletId, deposit.WalletId);
        Assert.Equal("8896", deposit.Last4);
        Assert.Equal(CardNetwork.Visa, deposit.Network);
        Assert.Equal(DateTimeKind.Utc, deposit.Date.Kind);
        Assert.InRange(deposit.Date, before, DateTime.UtcNow);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_StripSpaces_When_NumberHasSpaces()
    {
        var dto = NewCardDto();
        dto.CardNumber = "4532 7109 3842 8896";

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.Success, outcome.Status);
    }

    [Fact]
    public async Task Should_SaveCard_When_SaveCardTrue()
    {
        await _sut.DepositAsync(NewCardDto(save: true));

        var card = Assert.Single(Inserted(_cards));
        Assert.Equal(UserId, card.UserId);
        Assert.Equal("8896", card.Last4);
        Assert.Equal("Jane Doe", card.HolderName);
        Assert.Same(card, Assert.Single(Inserted(_deposits)).SavedCard);
    }

    [Fact]
    public async Task Should_NotSaveCard_When_SaveCardFalse()
    {
        await _sut.DepositAsync(NewCardDto(save: false));

        Assert.Empty(Inserted(_cards));
        Assert.Null(Assert.Single(Inserted(_deposits)).SavedCard);
    }

    [Fact]
    public async Task Should_ReuseExistingCard_When_SameUserLast4ExpiryHolder()
    {
        _cardData.Add(Card(7));

        await _sut.DepositAsync(NewCardDto(save: true));

        Assert.Empty(Inserted(_cards));
        Assert.Equal(7, Assert.Single(Inserted(_deposits)).SavedCardId);
    }

    [Fact]
    public async Task Should_UseSavedCard_When_SavedCardIdOwned()
    {
        _cardData.Add(Card(7));
        var dto = new DepositCreateDto { UserId = UserId, Amount = 50m, SavedCardId = 7, Cvv = Cvv };

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.Success, outcome.Status);
        var deposit = Assert.Single(Inserted(_deposits));
        Assert.Equal(7, deposit.SavedCardId);
        Assert.Equal("8896", deposit.Last4);
        Assert.Equal(CardNetwork.Visa, deposit.Network);
        Assert.Equal(50m, _wallet.Balance);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_SavedCardCvvBad()
    {
        _cardData.Add(Card(7));
        var dto = new DepositCreateDto { UserId = UserId, Amount = 50m, SavedCardId = 7, Cvv = "1" };

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.Invalid, outcome.Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_SavedCardHasExpired()
    {
        var expired = Card(7);
        expired.ExpiryYear = DateTime.UtcNow.Year - 1;
        _cardData.Add(expired);
        var dto = new DepositCreateDto { UserId = UserId, Amount = 50m, SavedCardId = 7, Cvv = Cvv };

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.Invalid, outcome.Status);
        Assert.Equal(0m, _wallet.Balance);
        Assert.Empty(Inserted(_deposits));
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnCardNotFound_When_SavedCardBelongsToOtherUser()
    {
        _cardData.Add(Card(7, userId: 99));
        var dto = new DepositCreateDto { UserId = UserId, Amount = 50m, SavedCardId = 7, Cvv = Cvv };

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.CardNotFound, outcome.Status);
        Assert.Equal(0m, _wallet.Balance);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_BothCardSources()
    {
        _cardData.Add(Card(7));
        var dto = NewCardDto();
        dto.SavedCardId = 7;

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.Invalid, outcome.Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_NeitherCardSource()
    {
        var dto = new DepositCreateDto { UserId = UserId, Amount = 50m, Cvv = Cvv };

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.Invalid, outcome.Status);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("9.99")]
    [InlineData("100000.01")]
    [InlineData("10.001")]
    public async Task Should_ReturnInvalid_And_LeaveBalance_When_AmountOutOfRange(string amount)
    {
        var outcome = await _sut.DepositAsync(NewCardDto(amount: decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(DepositStatus.Invalid, outcome.Status);
        Assert.Equal(0m, _wallet.Balance);
        Assert.Empty(Inserted(_deposits));
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_LuhnFails()
    {
        var dto = NewCardDto();
        dto.CardNumber = "4532710938428897";

        Assert.Equal(DepositStatus.Invalid, (await _sut.DepositAsync(dto)).Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_CvvBad()
    {
        var dto = NewCardDto();
        dto.Cvv = "12a";

        Assert.Equal(DepositStatus.Invalid, (await _sut.DepositAsync(dto)).Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_ExpiryPast()
    {
        var dto = NewCardDto();
        dto.ExpiryYear = DateTime.UtcNow.Year - 1;

        Assert.Equal(DepositStatus.Invalid, (await _sut.DepositAsync(dto)).Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_ExpiryMissing()
    {
        var dto = NewCardDto();
        dto.ExpiryMonth = null;

        Assert.Equal(DepositStatus.Invalid, (await _sut.DepositAsync(dto)).Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_HolderMissing()
    {
        var dto = NewCardDto();
        dto.HolderName = "  ";

        Assert.Equal(DepositStatus.Invalid, (await _sut.DepositAsync(dto)).Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_HolderOver100Chars()
    {
        var dto = NewCardDto();
        dto.HolderName = new string('A', 101);

        var outcome = await _sut.DepositAsync(dto);

        Assert.Equal(DepositStatus.Invalid, outcome.Status);
        Assert.Equal(0m, _wallet.Balance);
        Assert.Empty(Inserted(_deposits));
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_Succeed_When_HolderIs100Chars()
    {
        var dto = NewCardDto();
        dto.HolderName = new string('A', 100);

        Assert.Equal(DepositStatus.Success, (await _sut.DepositAsync(dto)).Status);
    }

    [Fact]
    public async Task Should_ReturnInvalid_When_WalletMissing()
    {
        _wallets.Get(Arg.Any<Expression<Func<Wallet, bool>>>(), Arg.Any<string[]?>()).Returns([]);

        Assert.Equal(DepositStatus.Invalid, (await _sut.DepositAsync(NewCardDto())).Status);
    }

    [Fact]
    public async Task Should_NeverPersistFullNumberOrCvv()
    {
        await _sut.DepositAsync(NewCardDto(save: true));

        var json = JsonSerializer.Serialize(new object[]
        {
            Inserted(_cards).Select(c => new { c.UserId, c.HolderName, c.Last4, c.ExpiryMonth, c.ExpiryYear }),
            Inserted(_deposits).Select(d => new { d.WalletId, d.Last4, d.Amount, d.SavedCardId }),
            _wallet.Balance
        });
        Assert.DoesNotContain(ValidNumber, json);
        Assert.DoesNotContain($"\"{Cvv}\"", json);
    }

    [Fact]
    public async Task Should_KeepDeposit_When_CardDeleted()
    {
        _cardData.Add(Card(7));

        var deleted = await _sut.DeleteCardAsync(UserId, 7);

        Assert.True(deleted);
        await _cards.Received(1).DeleteAsync(Arg.Is<object[]>(k => (int)k[0] == 7));
        await _deposits.DidNotReceiveWithAnyArgs().DeleteAsync();
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnFalse_When_DeletingOtherUsersCard()
    {
        _cardData.Add(Card(7, userId: 99));

        Assert.False(await _sut.DeleteCardAsync(UserId, 7));
        await _cards.DidNotReceiveWithAnyArgs().DeleteAsync();
    }

    [Fact]
    public async Task Should_ReturnDepositsNewestFirst_When_Listing()
    {
        var now = DateTime.UtcNow;
        _depositData.Add(new Deposit { Id = 1, WalletId = WalletId, Date = now.AddDays(-2), Last4 = "1111" });
        _depositData.Add(new Deposit { Id = 2, WalletId = WalletId, Date = now, Last4 = "2222" });
        _depositData.Add(new Deposit { Id = 3, WalletId = 99, Date = now, Last4 = "3333" });

        var list = await _sut.GetDepositsAsync(UserId);

        Assert.Equal([2, 1], list.Select(d => d.Id));
        Assert.Equal("Visa", list[0].Network);
    }

    [Fact]
    public async Task Should_ReturnHigherIdFirst_When_DatesTie()
    {
        var now = DateTime.UtcNow;
        _depositData.Add(new Deposit { Id = 1, WalletId = WalletId, Date = now, Last4 = "1111" });
        _depositData.Add(new Deposit { Id = 2, WalletId = WalletId, Date = now, Last4 = "2222" });

        var list = await _sut.GetDepositsAsync(UserId);

        Assert.Equal([2, 1], list.Select(d => d.Id));
    }

    [Fact]
    public async Task Should_ReturnOnlyOwnCards_When_ListingCards()
    {
        _cardData.Add(Card(7));
        _cardData.Add(Card(8, userId: 99));

        var list = await _sut.GetCardsAsync(UserId);

        var card = Assert.Single(list);
        Assert.Equal(7, card.Id);
        Assert.Equal("Visa", card.Network);
    }

    [Fact]
    public async Task Should_BeginLockWalletSaveThenCommit_When_Depositing()
    {
        var outcome = await _sut.DepositAsync(NewCardDto());

        Assert.Equal(DepositStatus.Success, outcome.Status);
        Received.InOrder(() =>
        {
            _uow.BeginTransactionAsync(Arg.Any<CancellationToken>());
            _uow.LockOfferAndWalletsAsync(0, WalletId, WalletId, Arg.Any<CancellationToken>());
            _uow.SaveAsync();
            _uow.CommitAsync(Arg.Any<CancellationToken>());
        });
    }
}
