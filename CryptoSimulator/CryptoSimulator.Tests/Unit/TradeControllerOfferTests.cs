using CryptoSimulator.Controllers;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Results;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit.Controllers;

public class TradeControllerOfferTests
{
    private readonly IOfferService _offers = Substitute.For<IOfferService>();
    private readonly TradeController _sut;

    public TradeControllerOfferTests()
    {
        _sut = new TradeController(Substitute.For<IUnitOfWork>(), Substitute.For<ITradeService>(), _offers).SignedInAs();
    }

    private static CreateOfferDto CreateDto() => new() { CryptoId = 2, Amount = 1m, AskingPrice = 5m };

    [Fact]
    public async Task Should_ReturnMarketOffers_When_GetMarketOffers()
    {
        IReadOnlyList<MarketOfferDto> list = new List<MarketOfferDto> { new() { Id = 7 } };
        _offers.GetMarketOffersAsync(1, Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetMarketOffers(CancellationToken.None);

        Assert.Same(list, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnMyOffers_When_GetMyOffers()
    {
        IReadOnlyList<MyOfferDto> list = new List<MyOfferDto> { new() { Id = 7 } };
        _offers.GetMyOffersAsync(1, Arg.Any<CancellationToken>()).Returns(list);

        var result = await _sut.GetMyOffers(CancellationToken.None);

        Assert.Same(list, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnCreated_When_CreateSucceeds()
    {
        var offer = new MyOfferDto { Id = 9 };
        var dto = CreateDto();
        _offers.CreateAsync(1, dto, Arg.Any<CancellationToken>()).Returns(OfferResult.Success(offer));

        var result = await _sut.CreateOffer(dto, CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("/api/trades/offers/mine", created.Location);
        Assert.Same(offer, created.Value);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreateInvalid()
    {
        _offers.CreateAsync(1, Arg.Any<CreateOfferDto>(), Arg.Any<CancellationToken>()).Returns(OfferResult.Invalid("bad"));

        var result = await _sut.CreateOffer(CreateDto(), CancellationToken.None);

        Assert.Equal("bad", Assert.IsType<BadRequestObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CreateCryptoMissing()
    {
        _offers.CreateAsync(1, Arg.Any<CreateOfferDto>(), Arg.Any<CancellationToken>()).Returns(OfferResult.NotFound("none"));

        var result = await _sut.CreateOffer(CreateDto(), CancellationToken.None);

        Assert.Equal("none", Assert.IsType<NotFoundObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNoContent_When_CancelSucceeds()
    {
        _offers.CancelAsync(1, 5, Arg.Any<CancellationToken>()).Returns(OfferResult.Success());

        var result = await _sut.CancelOffer(5, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_CancelNotOwner()
    {
        _offers.CancelAsync(1, 5, Arg.Any<CancellationToken>()).Returns(OfferResult.NotOwner());

        var result = await _sut.CancelOffer(5, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbidden.StatusCode);
        Assert.Equal("Only the seller can cancel this offer.", forbidden.Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CancelMissing()
    {
        _offers.CancelAsync(1, 5, Arg.Any<CancellationToken>()).Returns(OfferResult.NotFound("none"));

        var result = await _sut.CancelOffer(5, CancellationToken.None);

        Assert.Equal("none", Assert.IsType<NotFoundObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_CancelConflict()
    {
        _offers.CancelAsync(1, 5, Arg.Any<CancellationToken>()).Returns(OfferResult.Conflict("closed"));

        var result = await _sut.CancelOffer(5, CancellationToken.None);

        Assert.Equal("closed", Assert.IsType<ConflictObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnNoContent_When_BuySucceeds()
    {
        _offers.BuyAsync(1, 5, 2m, Arg.Any<CancellationToken>()).Returns(OfferResult.Success());

        var result = await _sut.BuyOffer(5, new BuyOfferDto { Amount = 2m }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_BuyInvalid()
    {
        _offers.BuyAsync(1, 5, 2m, Arg.Any<CancellationToken>()).Returns(OfferResult.Invalid("bad"));

        var result = await _sut.BuyOffer(5, new BuyOfferDto { Amount = 2m }, CancellationToken.None);

        Assert.Equal("bad", Assert.IsType<BadRequestObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_BuyMissing()
    {
        _offers.BuyAsync(1, 5, 2m, Arg.Any<CancellationToken>()).Returns(OfferResult.NotFound("none"));

        var result = await _sut.BuyOffer(5, new BuyOfferDto { Amount = 2m }, CancellationToken.None);

        Assert.Equal("none", Assert.IsType<NotFoundObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnConflict_When_BuyConflict()
    {
        _offers.BuyAsync(1, 5, 2m, Arg.Any<CancellationToken>()).Returns(OfferResult.Conflict("own"));

        var result = await _sut.BuyOffer(5, new BuyOfferDto { Amount = 2m }, CancellationToken.None);

        Assert.Equal("own", Assert.IsType<ConflictObjectResult>(result).Value);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_BuyNotOwner()
    {
        _offers.BuyAsync(1, 5, 2m, Arg.Any<CancellationToken>()).Returns(OfferResult.NotOwner());

        var result = await _sut.BuyOffer(5, new BuyOfferDto { Amount = 2m }, CancellationToken.None);

        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_NoUserClaim()
    {
        var anonymous = new TradeController(Substitute.For<IUnitOfWork>(), Substitute.For<ITradeService>(), _offers);

        var result = await anonymous.GetMyOffers(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}
