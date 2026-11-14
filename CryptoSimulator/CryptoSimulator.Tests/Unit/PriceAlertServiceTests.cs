using System.Linq.Expressions;
using AutoMapper;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CryptoSimulator.Tests.Unit;

public class PriceAlertServiceTests
{
    private const int UserId = 1;
    private const int CryptoId = 5;

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<PriceAlert> _alerts = Substitute.For<IRepository<PriceAlert>>();
    private readonly IRepository<User> _users = Substitute.For<IRepository<User>>();
    private readonly IRepository<Crypto> _cryptos = Substitute.For<IRepository<Crypto>>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly PriceAlertService _sut;

    public PriceAlertServiceTests()
    {
        _uow.PriceAlertRepository.Returns(_alerts);
        _uow.UserRepository.Returns(_users);
        _uow.CryptoRepository.Returns(_cryptos);
        _sut = new PriceAlertService(_uow, _mapper, Substitute.For<ILogger<PriceAlertService>>());
    }

    private static PriceAlertCreateDto CreateDto(string type = "above", decimal price = 100m) =>
        new() { UserId = UserId, CryptoId = CryptoId, TargetPrice = price, AlertType = type };

    private void SetupUserAndCrypto(bool userExists = true, bool cryptoExists = true)
    {
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(userExists ? new User { Id = UserId } : null);
        _cryptos.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(cryptoExists ? new Crypto { Id = CryptoId, Name = "Bitcoin" } : null);
    }

    private void SetupActiveAlerts(params PriceAlert[] alerts) =>
        _alerts.GetAsync(Arg.Any<Expression<Func<PriceAlert, bool>>>(), Arg.Any<string[]?>())
            .Returns(alerts);

    private static PriceAlert Alert(PriceAlertType type, decimal target, bool notified = false) =>
        new() { Id = 1, UserId = UserId, CryptoId = CryptoId, TargetPrice = target, AlertType = type, IsActive = true, HasBeenNotifiedForCurrentState = notified };

    // CreateAlertAsync

    [Fact]
    public async Task Should_ReturnError_When_CreatingAlertForMissingUser()
    {
        SetupUserAndCrypto(userExists: false);

        var (alert, error) = await _sut.CreateAlertAsync(CreateDto());

        Assert.Null(alert);
        Assert.Contains("User", error);
        await _alerts.DidNotReceive().InsertAsync(Arg.Any<PriceAlert>());
    }

    [Fact]
    public async Task Should_ReturnError_When_CreatingAlertForMissingCrypto()
    {
        SetupUserAndCrypto(cryptoExists: false);

        var (alert, error) = await _sut.CreateAlertAsync(CreateDto());

        Assert.Null(alert);
        Assert.Contains("Crypto", error);
        await _alerts.DidNotReceive().InsertAsync(Arg.Any<PriceAlert>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("sideways")]
    [InlineData("abovee")]
    public async Task Should_ReturnError_When_AlertTypeInvalid(string type)
    {
        SetupUserAndCrypto();

        var (alert, error) = await _sut.CreateAlertAsync(CreateDto(type));

        Assert.Null(alert);
        Assert.Contains("Invalid AlertType", error);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Theory]
    [InlineData("above", PriceAlertType.Above)]
    [InlineData("ABOVE", PriceAlertType.Above)]
    [InlineData("below", PriceAlertType.Below)]
    [InlineData("Below", PriceAlertType.Below)]
    public async Task Should_InsertActiveAlert_When_TypeIsValid(string type, PriceAlertType expected)
    {
        SetupUserAndCrypto();
        _mapper.Map<PriceAlertGetDto>(Arg.Any<PriceAlert>()).Returns(new PriceAlertGetDto());

        var (alert, error) = await _sut.CreateAlertAsync(CreateDto(type, 42.5m));

        Assert.Null(error);
        Assert.NotNull(alert);
        await _alerts.Received(1).InsertAsync(Arg.Is<PriceAlert>(a =>
            a.AlertType == expected && a.TargetPrice == 42.5m && a.UserId == UserId &&
            a.CryptoId == CryptoId && a.IsActive && !a.HasBeenNotifiedForCurrentState));
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_SetCryptoNameOnDto_When_AlertCreated()
    {
        SetupUserAndCrypto();
        _mapper.Map<PriceAlertGetDto>(Arg.Any<PriceAlert>()).Returns(new PriceAlertGetDto { Id = 9 });

        var (alert, _) = await _sut.CreateAlertAsync(CreateDto());

        Assert.Equal("Bitcoin", alert!.CryptoName);
        Assert.Equal(9, alert.Id);
    }

    // CheckAlertsAsync

    [Fact]
    public async Task Should_NotSave_When_NoActiveAlerts()
    {
        SetupActiveAlerts();

        await _sut.CheckAlertsAsync(CryptoId, 100m);

        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_TriggerBelowAlert_When_PriceDropsBelowTarget()
    {
        var alert = Alert(PriceAlertType.Below, 100m);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 99.99m);

        Assert.True(alert.HasBeenNotifiedForCurrentState);
        Assert.NotNull(alert.LastTriggeredAt);
        _alerts.Received(1).Update(alert);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_NotTriggerBelowAlert_When_PriceEqualsTarget()
    {
        var alert = Alert(PriceAlertType.Below, 100m);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 100m);

        Assert.False(alert.HasBeenNotifiedForCurrentState);
        Assert.Null(alert.LastTriggeredAt);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_NotRetriggerBelowAlert_When_AlreadyNotified()
    {
        var alert = Alert(PriceAlertType.Below, 100m, notified: true);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 50m);

        Assert.Null(alert.LastTriggeredAt);
        _alerts.DidNotReceive().Update(Arg.Any<PriceAlert>());
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ResetBelowAlert_When_PriceRecoversToTarget()
    {
        var alert = Alert(PriceAlertType.Below, 100m, notified: true);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 100m);

        Assert.False(alert.HasBeenNotifiedForCurrentState);
        Assert.Null(alert.LastTriggeredAt);
        _alerts.Received(1).Update(alert);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_TriggerAboveAlert_When_PriceRisesAboveTarget()
    {
        var alert = Alert(PriceAlertType.Above, 100m);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 100.01m);

        Assert.True(alert.HasBeenNotifiedForCurrentState);
        Assert.NotNull(alert.LastTriggeredAt);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_NotTriggerAboveAlert_When_PriceEqualsTarget()
    {
        var alert = Alert(PriceAlertType.Above, 100m);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 100m);

        Assert.False(alert.HasBeenNotifiedForCurrentState);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_NotRetriggerAboveAlert_When_AlreadyNotified()
    {
        var alert = Alert(PriceAlertType.Above, 100m, notified: true);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 500m);

        Assert.Null(alert.LastTriggeredAt);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ResetAboveAlert_When_PriceFallsBackToTarget()
    {
        var alert = Alert(PriceAlertType.Above, 100m, notified: true);
        SetupActiveAlerts(alert);

        await _sut.CheckAlertsAsync(CryptoId, 100m);

        Assert.False(alert.HasBeenNotifiedForCurrentState);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_SaveOnce_When_MultipleAlertsChange()
    {
        var below = Alert(PriceAlertType.Below, 100m);
        var above = Alert(PriceAlertType.Above, 10m);
        SetupActiveAlerts(below, above);

        await _sut.CheckAlertsAsync(CryptoId, 50m);

        Assert.True(below.HasBeenNotifiedForCurrentState);
        Assert.True(above.HasBeenNotifiedForCurrentState);
        await _uow.Received(1).SaveAsync();
    }

    // GetActiveAlertsForUserAsync

    [Fact]
    public async Task Should_ReturnError_When_ActiveAlertsUserMissing()
    {
        SetupUserAndCrypto(userExists: false);

        var (alerts, error) = await _sut.GetActiveAlertsForUserAsync(UserId);

        Assert.Null(alerts);
        Assert.Contains("not found", error);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoActiveAlerts()
    {
        SetupUserAndCrypto();
        SetupActiveAlerts();

        var (alerts, error) = await _sut.GetActiveAlertsForUserAsync(UserId);

        Assert.Null(error);
        Assert.Empty(alerts!);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_ActiveAlertsRepositoryReturnsNull()
    {
        SetupUserAndCrypto();
        _alerts.GetAsync(Arg.Any<Expression<Func<PriceAlert, bool>>>(), Arg.Any<string[]?>())
            .Returns((IEnumerable<PriceAlert>)null!);

        var (alerts, error) = await _sut.GetActiveAlertsForUserAsync(UserId);

        Assert.Null(error);
        Assert.Empty(alerts!);
    }

    [Fact]
    public async Task Should_ReturnMappedDtos_When_ActiveAlertsExist()
    {
        SetupUserAndCrypto();
        SetupActiveAlerts(Alert(PriceAlertType.Above, 1m));
        var dtos = new[] { new PriceAlertGetDto { Id = 1 } };
        _mapper.Map<IEnumerable<PriceAlertGetDto>>(Arg.Any<object>()).Returns(dtos);

        var (alerts, error) = await _sut.GetActiveAlertsForUserAsync(UserId);

        Assert.Null(error);
        Assert.Same(dtos, alerts);
    }

    // DeleteAlertAsync

    [Fact]
    public async Task Should_ReturnError_When_DeletingMissingAlert()
    {
        _alerts.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns((PriceAlert?)null);

        var (success, error) = await _sut.DeleteAlertAsync(7);

        Assert.False(success);
        Assert.Contains("7", error);
        await _alerts.DidNotReceive().DeleteAsync(Arg.Any<object[]>());
    }

    [Fact]
    public async Task Should_DeleteAndSave_When_AlertExists()
    {
        _alerts.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(Alert(PriceAlertType.Above, 1m));

        var (success, error) = await _sut.DeleteAlertAsync(7);

        Assert.True(success);
        Assert.Null(error);
        await _alerts.Received(1).DeleteAsync(7);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnFailure_When_DeleteThrows()
    {
        _alerts.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(Alert(PriceAlertType.Above, 1m));
        _uow.SaveAsync().Throws(new InvalidOperationException("db down"));

        var (success, error) = await _sut.DeleteAlertAsync(7);

        Assert.False(success);
        Assert.Contains("db down", error);
    }

    // GetTriggeredAlertsHistoryForUserAsync

    [Fact]
    public async Task Should_ReturnError_When_HistoryUserMissing()
    {
        SetupUserAndCrypto(userExists: false);

        var (alerts, error) = await _sut.GetTriggeredAlertsHistoryForUserAsync(UserId);

        Assert.Null(alerts);
        Assert.Contains("not found", error);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoTriggeredHistory()
    {
        SetupUserAndCrypto();
        SetupActiveAlerts();

        var (alerts, error) = await _sut.GetTriggeredAlertsHistoryForUserAsync(UserId);

        Assert.Null(error);
        Assert.Empty(alerts!);
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_HistoryRepositoryReturnsNull()
    {
        SetupUserAndCrypto();
        _alerts.GetAsync(Arg.Any<Expression<Func<PriceAlert, bool>>>(), Arg.Any<string[]?>())
            .Returns((IEnumerable<PriceAlert>)null!);

        var (alerts, error) = await _sut.GetTriggeredAlertsHistoryForUserAsync(UserId);

        Assert.Null(error);
        Assert.Empty(alerts!);
    }

    [Fact]
    public async Task Should_MapHistoryNewestFirst_When_TriggeredAlertsExist()
    {
        SetupUserAndCrypto();
        var older = Alert(PriceAlertType.Above, 1m);
        older.Id = 1;
        older.LastTriggeredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = Alert(PriceAlertType.Below, 1m);
        newer.Id = 2;
        newer.LastTriggeredAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        SetupActiveAlerts(older, newer);
        IEnumerable<PriceAlert>? mapped = null;
        _mapper.Map<IEnumerable<PriceAlertGetDto>>(Arg.Do<object>(o => mapped = ((IEnumerable<PriceAlert>)o).ToList()))
            .Returns(new List<PriceAlertGetDto>());

        var (alerts, error) = await _sut.GetTriggeredAlertsHistoryForUserAsync(UserId);

        Assert.Null(error);
        Assert.NotNull(alerts);
        Assert.Equal([2, 1], mapped!.Select(a => a.Id));
    }
}
