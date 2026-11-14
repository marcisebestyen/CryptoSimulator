using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CryptoSimulator.Tests.Unit;

// Each run ends by cancelling the token from inside a mocked call, so the 45s delay never starts.
public class PriceUpdateServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Crypto> _cryptos = Substitute.For<IRepository<Crypto>>();
    private readonly IRepository<CryptoLog> _logs = Substitute.For<IRepository<CryptoLog>>();
    private readonly ICryptoService _cryptoService = Substitute.For<ICryptoService>();
    private readonly IPriceAlertService _alerts = Substitute.For<IPriceAlertService>();
    private readonly CancellationTokenSource _cts = new();
    private readonly PriceUpdateService _sut;

    public PriceUpdateServiceTests()
    {
        _uow.CryptoRepository.Returns(_cryptos);
        _uow.CryptoLogRepository.Returns(_logs);
        _cryptos.GetAllAsync(Arg.Any<string[]?>()).Returns(new[] { new Crypto { Id = 1 } });
        ArrangeLog(100m);

        var provider = new ServiceCollection()
            .AddSingleton(_uow)
            .AddSingleton(_cryptoService)
            .AddSingleton(_alerts)
            .BuildServiceProvider();
        _sut = new PriceUpdateService(provider, NullLogger<PriceUpdateService>.Instance);
    }

    private void ArrangeLog(decimal? value) =>
        _logs.GetAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CryptoLog, bool>>>(), Arg.Any<string[]?>())
            .Returns(value is null ? Array.Empty<CryptoLog>() : new[] { new CryptoLog { CurrentValue = value.Value } });

    private void ArrangeUpdate(bool processed) =>
        _cryptoService.UpdateCryptoPriceAsync(1, Arg.Any<decimal>(), Arg.Any<DateTime>())
            .Returns(_ => { _cts.Cancel(); return processed; });

    private async Task RunAsync()
    {
        await _sut.StartAsync(_cts.Token);
        await _sut.ExecuteTask!;
    }

    [Fact]
    public async Task Should_SaveAndCheckAlerts_When_PriceUpdateProcessed()
    {
        ArrangeUpdate(true);

        await RunAsync();

        await _uow.Received(1).SaveAsync();
        await _alerts.Received(1).CheckAlertsAsync(1, Arg.Any<decimal>());
    }

    [Fact]
    public async Task Should_KeepNewPriceWithinOnePercent_When_CurrentPriceKnown()
    {
        ArrangeUpdate(true);

        await RunAsync();

        await _cryptoService.Received(1).UpdateCryptoPriceAsync(1,
            Arg.Is<decimal>(p => p >= 99m && p <= 101m), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Should_UseMinimumPrice_When_NoCurrentLogExists()
    {
        ArrangeLog(null);
        ArrangeUpdate(true);

        await RunAsync();

        await _cryptoService.Received(1).UpdateCryptoPriceAsync(1, 0.01m, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Should_SkipSaveAndAlerts_When_PriceUpdateNotProcessed()
    {
        ArrangeUpdate(false);

        await RunAsync();

        await _uow.DidNotReceive().SaveAsync();
        await _alerts.DidNotReceive().CheckAlertsAsync(Arg.Any<int>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task Should_NotThrow_When_PriceUpdateThrows()
    {
        _cryptoService.UpdateCryptoPriceAsync(1, Arg.Any<decimal>(), Arg.Any<DateTime>())
            .Returns<bool>(_ => { _cts.Cancel(); throw new InvalidOperationException("boom"); });

        await RunAsync();

        await _alerts.DidNotReceive().CheckAlertsAsync(Arg.Any<int>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task Should_StopCleanly_When_LoadingCryptosThrows()
    {
        _cryptos.GetAllAsync(Arg.Any<string[]?>())
            .Returns<IEnumerable<Crypto>>(_ => { _cts.Cancel(); throw new InvalidOperationException("db down"); });

        await RunAsync();

        Assert.True(_sut.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Should_StopCleanly_When_LoadingCryptosIsCancelled()
    {
        _cryptos.GetAllAsync(Arg.Any<string[]?>())
            .Returns<IEnumerable<Crypto>>(_ => throw new OperationCanceledException());

        await RunAsync();

        Assert.True(_sut.ExecuteTask!.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Should_NotUpdateAnything_When_TokenAlreadyCancelled()
    {
        _cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(RunAsync);

        await _cryptos.DidNotReceive().GetAllAsync(Arg.Any<string[]?>());
    }
}
