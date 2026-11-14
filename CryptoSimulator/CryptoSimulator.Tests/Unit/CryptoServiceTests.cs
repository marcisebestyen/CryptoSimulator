using AutoMapper;
using CryptoSimulator.Data;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

// GetAllWithCurrentValueAsync and GetCryptoPriceHistoryAsync need a real DbContext query; it belongs to integration tests.
public class CryptoServiceTests
{
    private static readonly DateTime Max = new(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<CryptoLog> _logs;
    private readonly IMapper _mapper =
        new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private CryptoLog Current = new() { CryptoId = 1, CurrentValue = 10m, From = T0, To = Max };

    public CryptoServiceTests()
    {
        _logs = RepositoryFakes.WithData(Current, new CryptoLog { CryptoId = 2, From = T0, To = Max });
        _uow.CryptoLogRepository.Returns(_logs);
    }

    private CryptoService Sut()
    {
        var options = new DbContextOptionsBuilder<CryptoSimulationDbContext>().Options;
        return new CryptoService(new CryptoSimulationDbContext(options), _uow, _mapper);
    }

    [Fact]
    public async Task Should_CloseCurrentLogAndInsertNew_When_PriceUpdatedLater()
    {
        var at = T0.AddHours(1);

        var ok = await Sut().UpdateCryptoPriceAsync(1, 20m, at);

        Assert.True(ok);
        Assert.Equal(at, Current.To);
        await _logs.Received(1).UpdateAsync(Current);
        await _logs.Received(1).InsertAsync(Arg.Is<CryptoLog>(l =>
            l.CryptoId == 1 && l.CurrentValue == 20m && l.From == at && l.To == Max));
    }

    [Fact]
    public async Task Should_ReturnFalseAndWriteNothing_When_TimestampBeforeCurrentLog()
    {
        var ok = await Sut().UpdateCryptoPriceAsync(1, 20m, T0.AddHours(-1));

        Assert.False(ok);
        await _logs.DidNotReceive().UpdateAsync(Arg.Any<CryptoLog>());
        await _logs.DidNotReceive().InsertAsync(Arg.Any<CryptoLog>());
    }

    [Fact]
    public async Task Should_OnlyInsert_When_NoPreviousLog()
    {
        var ok = await Sut().UpdateCryptoPriceAsync(5, 7m, T0);

        Assert.True(ok);
        await _logs.DidNotReceive().UpdateAsync(Arg.Any<CryptoLog>());
        await _logs.Received(1).InsertAsync(Arg.Is<CryptoLog>(l => l.CryptoId == 5 && l.To == Max));
    }

    [Fact]
    public async Task Should_ConvertLocalTimestampToUtc_When_KindNotUtc()
    {
        var local = DateTime.SpecifyKind(T0.AddDays(1), DateTimeKind.Local);

        await Sut().UpdateCryptoPriceAsync(1, 20m, local);

        await _logs.Received(1).InsertAsync(Arg.Is<CryptoLog>(l => l.From.Kind == DateTimeKind.Utc && l.From == local.ToUniversalTime()));
    }
}
