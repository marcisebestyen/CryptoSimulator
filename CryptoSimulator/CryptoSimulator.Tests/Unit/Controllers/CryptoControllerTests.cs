using AutoMapper;
using CryptoSimulator.Controllers;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit.Controllers;

public class CryptoControllerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Crypto> _cryptos = Substitute.For<IRepository<Crypto>>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ICryptoService _service = Substitute.For<ICryptoService>();
    private readonly CryptoController _sut;

    public CryptoControllerTests()
    {
        _uow.CryptoRepository.Returns(_cryptos);
        _sut = new CryptoController(_uow, _mapper, _service);
    }

    private void ArrangeCrypto(Crypto? crypto) =>
        _cryptos.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>()).Returns(crypto);

    [Fact]
    public async Task Should_ReturnOkWithDto_When_CryptoExists()
    {
        var crypto = new Crypto { Id = 1 };
        var dto = new CryptoGetDto();
        ArrangeCrypto(crypto);
        _mapper.Map<CryptoGetDto>(crypto).Returns(dto);

        var result = await _sut.GetCrypto(1);

        Assert.Same(dto, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GetCryptoMissing()
    {
        ArrangeCrypto(null);

        var result = await _sut.GetCrypto(3);

        var nf = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Crypto with ID 3 not found.", nf.Value);
    }

    [Fact]
    public async Task Should_ReturnAllCryptos_When_GetAll()
    {
        var list = new List<CryptoGetWithCurrentValueDto>();
        _service.GetAllWithCurrentValueAsync().Returns(list);

        var result = await _sut.GetAllCryptos();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNoContentAndSave_When_PriceUpdateSucceeds()
    {
        ArrangeCrypto(new Crypto { Id = 1 });
        _service.UpdateCryptoPriceAsync(1, 10m, Arg.Any<DateTime>()).Returns(true);

        var result = await _sut.UpdateCryptoPrice(1, new ManualPriceUpdateDto { NewPrice = 10m });

        Assert.IsType<NoContentResult>(result);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_PriceUpdateFailsAndCryptoMissing()
    {
        _service.UpdateCryptoPriceAsync(1, 10m, Arg.Any<DateTime>()).Returns(false);
        ArrangeCrypto(null);

        var result = await _sut.UpdateCryptoPrice(1, new ManualPriceUpdateDto { NewPrice = 10m });

        Assert.IsType<NotFoundObjectResult>(result);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_NotCallServiceOrSave_When_UpdatePriceCryptoMissing()
    {
        ArrangeCrypto(null);

        var result = await _sut.UpdateCryptoPrice(1, new ManualPriceUpdateDto { NewPrice = 10m });

        Assert.IsType<NotFoundObjectResult>(result);
        await _service.DidNotReceive().UpdateCryptoPriceAsync(Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<DateTime>());
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_PriceUpdateFailsAndCryptoExists()
    {
        _service.UpdateCryptoPriceAsync(1, 10m, Arg.Any<DateTime>()).Returns(false);
        ArrangeCrypto(new Crypto { Id = 1 });

        var result = await _sut.UpdateCryptoPrice(1, new ManualPriceUpdateDto { NewPrice = 10m });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Should_PassUtcTimestamp_When_UpdatingPrice()
    {
        ArrangeCrypto(new Crypto { Id = 1 });
        _service.UpdateCryptoPriceAsync(1, 10m, Arg.Any<DateTime>()).Returns(true);

        await _sut.UpdateCryptoPrice(1, new ManualPriceUpdateDto { NewPrice = 10m });

        await _service.Received(1).UpdateCryptoPriceAsync(1, 10m,
            Arg.Is<DateTime>(d => d.Kind == DateTimeKind.Utc));
    }

    [Fact]
    public async Task Should_ReturnCreatedAtGetCrypto_When_CreateCrypto()
    {
        var crypto = new Crypto { Id = 4 };
        var dto = new CryptoGetDto();
        _mapper.Map<Crypto>(Arg.Any<CryptoPostDto>()).Returns(crypto);
        _mapper.Map<CryptoGetDto>(crypto).Returns(dto);

        var result = await _sut.CreateCrypto(new CryptoPostDto { Name = "BTC" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CryptoController.GetCrypto), created.ActionName);
        Assert.Equal(4, created.RouteValues!["cryptoId"]);
        Assert.Same(dto, created.Value);
        await _cryptos.Received(1).InsertAsync(crypto);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNoContentAndDelete_When_DeleteCryptoExists()
    {
        ArrangeCrypto(new Crypto { Id = 1 });

        var result = await _sut.DeleteCrypto(1);

        Assert.IsType<NoContentResult>(result);
        await _cryptos.Received(1).DeleteAsync(1);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_DeleteCryptoMissing()
    {
        ArrangeCrypto(null);

        var result = await _sut.DeleteCrypto(1);

        Assert.IsType<NotFoundObjectResult>(result);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnPagedHistory_When_CryptoExists()
    {
        var history = new PagedResult<CryptoPriceHistoryDto>(new List<CryptoPriceHistoryDto>(), 0, 2, 10);
        ArrangeCrypto(new Crypto { Id = 1 });
        _service.GetCryptoPriceHistoryAsync(1, 2, 10).Returns(history);

        var result = await _sut.GetCryptoPriceHistory(1, 2, 10);

        Assert.Same(history, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_UseDefaultPaging_When_HistoryDefaultsUsed()
    {
        ArrangeCrypto(new Crypto { Id = 1 });

        await _sut.GetCryptoPriceHistory(1);

        await _service.Received(1).GetCryptoPriceHistoryAsync(1, 1, 25);
    }

    [Fact]
    public async Task Should_CapPageSizeAt100_When_HistoryPageSizeTooLarge()
    {
        ArrangeCrypto(new Crypto { Id = 1 });

        await _sut.GetCryptoPriceHistory(1, 1, 500);

        await _service.Received(1).GetCryptoPriceHistoryAsync(1, 1, 100);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-1, 25)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public async Task Should_ReturnBadRequest_When_HistoryPagingInvalid(int page, int pageSize)
    {
        ArrangeCrypto(new Crypto { Id = 1 });

        var result = await _sut.GetCryptoPriceHistory(1, page, pageSize);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _service.DidNotReceive().GetCryptoPriceHistoryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Should_ReturnBadRequest_NotNotFound_When_HistoryPagingInvalidAndCryptoMissing()
    {
        ArrangeCrypto(null);

        var result = await _sut.GetCryptoPriceHistory(1, 0, 25);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_HistoryCryptoMissing()
    {
        ArrangeCrypto(null);

        var result = await _sut.GetCryptoPriceHistory(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await _service.DidNotReceive().GetCryptoPriceHistoryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task Should_ReturnConflictAndNotInsert_When_CreateNameTaken()
    {
        _service.IsNameTakenAsync("BTC").Returns(true);

        var result = await _sut.CreateCrypto(new CryptoPostDto { Name = "BTC" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        await _cryptos.DidNotReceive().InsertAsync(Arg.Any<Crypto>());
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_RenameAndSave_When_RenameCryptoValid()
    {
        var crypto = new Crypto { Id = 1, Name = "Old" };
        ArrangeCrypto(crypto);

        var result = await _sut.RenameCrypto(1, new CryptoPutDto { Name = "New" });

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("New", crypto.Name);
        await _cryptos.Received(1).UpdateAsync(crypto);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_RenameCryptoMissing()
    {
        ArrangeCrypto(null);

        var result = await _sut.RenameCrypto(1, new CryptoPutDto { Name = "New" });

        Assert.IsType<NotFoundObjectResult>(result);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnConflictAndKeepName_When_RenameNameTakenByOther()
    {
        var crypto = new Crypto { Id = 1, Name = "Old" };
        ArrangeCrypto(crypto);
        _service.IsNameTakenAsync("New", 1).Returns(true);

        var result = await _sut.RenameCrypto(1, new CryptoPutDto { Name = "New" });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Old", crypto.Name);
        await _uow.DidNotReceive().SaveAsync();
    }
}
