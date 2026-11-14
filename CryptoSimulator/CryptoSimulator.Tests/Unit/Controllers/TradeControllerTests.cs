using CryptoSimulator.Controllers;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit.Controllers;

public class TradeControllerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<User> _users = Substitute.For<IRepository<User>>();
    private readonly ITradeService _service = Substitute.For<ITradeService>();
    private readonly TradeController _sut;

    public TradeControllerTests()
    {
        _uow.UserRepository.Returns(_users);
        _sut = new TradeController(_uow, _service);
    }

    private void ArrangeUser(bool exists) =>
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(exists ? new User { Id = 1 } : null);

    [Fact]
    public async Task Should_ReturnNotFound_When_BuyUserMissing()
    {
        ArrangeUser(false);

        var result = await _sut.BuyCrypto(new BuyCryptoDto { UserId = 1, CryptoId = 2, Amount = 1 });

        Assert.IsType<NotFoundObjectResult>(result);
        await _service.DidNotReceive().BuyCrypto(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>());
    }

    [Fact]
    public async Task Should_ReturnOk_When_BuySucceeds()
    {
        ArrangeUser(true);
        _service.BuyCrypto(1, 2, 3m).Returns(true);

        var result = await _sut.BuyCrypto(new BuyCryptoDto { UserId = 1, CryptoId = 2, Amount = 3m });

        Assert.Equal("Crypto purchased successfully", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_BuyFails()
    {
        ArrangeUser(true);
        _service.BuyCrypto(1, 2, 3m).Returns(false);

        var result = await _sut.BuyCrypto(new BuyCryptoDto { UserId = 1, CryptoId = 2, Amount = 3m });

        Assert.Equal("Transaction failed", Assert.IsType<BadRequestObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_SellUserMissing()
    {
        ArrangeUser(false);

        var result = await _sut.SellCrypto(new SellCryptoDto { UserId = 1, CryptoId = 2, Amount = 1 });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnOk_When_SellSucceeds()
    {
        ArrangeUser(true);
        _service.SellCrypto(1, 2, 3m).Returns(true);

        var result = await _sut.SellCrypto(new SellCryptoDto { UserId = 1, CryptoId = 2, Amount = 3m });

        Assert.Equal("Crypto sold successfully", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_SellFails()
    {
        ArrangeUser(true);
        _service.SellCrypto(1, 2, 3m).Returns(false);

        var result = await _sut.SellCrypto(new SellCryptoDto { UserId = 1, CryptoId = 2, Amount = 3m });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_PortfolioUserMissing()
    {
        ArrangeUser(false);

        var result = await _sut.GetPortfolio(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_PortfolioNull()
    {
        ArrangeUser(true);
        _service.GetPortfolio(1).Returns((PortfolioDto?)null);

        var result = await _sut.GetPortfolio(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnPortfolio_When_Exists()
    {
        var portfolio = new PortfolioDto();
        ArrangeUser(true);
        _service.GetPortfolio(1).Returns(portfolio);

        var result = await _sut.GetPortfolio(1);

        Assert.Same(portfolio, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ProfitLossUserMissing()
    {
        ArrangeUser(false);

        var result = await _sut.GetDetailedProfitLoss(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ProfitLossNull()
    {
        ArrangeUser(true);
        _service.GetDetailedProfitLoss(1).Returns((IEnumerable<CryptoProfitLossDto>?)null);

        var result = await _sut.GetDetailedProfitLoss(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnProfitLoss_When_Exists()
    {
        var list = new List<CryptoProfitLossDto>();
        ArrangeUser(true);
        _service.GetDetailedProfitLoss(1).Returns(list);

        var result = await _sut.GetDetailedProfitLoss(1);

        Assert.Same(list, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_SummaryUserMissing()
    {
        ArrangeUser(false);

        var result = await _sut.GetPortfolioProfitLossSummary(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_SummaryNull()
    {
        ArrangeUser(true);
        _service.GetPortfolioProfitLoss(1).Returns((PortfolioProfitLossDto?)null);

        var result = await _sut.GetPortfolioProfitLossSummary(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnSummary_When_Exists()
    {
        var summary = new PortfolioProfitLossDto();
        ArrangeUser(true);
        _service.GetPortfolioProfitLoss(1).Returns(summary);

        var result = await _sut.GetPortfolioProfitLossSummary(1);

        Assert.Same(summary, Assert.IsType<OkObjectResult>(result.Result).Value);
    }
}
