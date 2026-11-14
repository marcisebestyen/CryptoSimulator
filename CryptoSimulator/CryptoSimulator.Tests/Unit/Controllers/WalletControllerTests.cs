using AutoMapper;
using CryptoSimulator.Controllers;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit.Controllers;

public class WalletControllerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Wallet> _wallets = Substitute.For<IRepository<Wallet>>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IWalletService _service = Substitute.For<IWalletService>();
    private readonly WalletController _sut;

    public WalletControllerTests()
    {
        _uow.WalletRepository.Returns(_wallets);
        _sut = new WalletController(_uow, _mapper, _service);
    }

    private void ArrangeWallet(Wallet? wallet) =>
        _wallets.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>()).Returns(wallet);

    [Fact]
    public async Task Should_ReturnOkWithDetails_When_WalletFound()
    {
        var details = new UserWalletDetailsDto();
        _service.GetUserWalletDetailsAsync(1).Returns(details);

        var result = await _sut.GetUserWallet(1);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_WalletDetailsMissing()
    {
        _service.GetUserWalletDetailsAsync(1).Returns((UserWalletDetailsDto?)null);

        var result = await _sut.GetUserWallet(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnOkAndSave_When_UpdateWalletSucceeds()
    {
        var wallet = new Wallet { Id = 1 };
        var dto = new WalletGetDto();
        ArrangeWallet(wallet);
        _mapper.Map<WalletGetDto>(wallet).Returns(dto);

        var result = await _sut.UpdateWallet(1, new WalletPutDto { Balance = 5m });

        Assert.Same(dto, Assert.IsType<OkObjectResult>(result.Result).Value);
        await _wallets.Received(1).UpdateAsync(wallet);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdateWalletMissing()
    {
        ArrangeWallet(null);

        var result = await _sut.UpdateWallet(1, new WalletPutDto());

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNoContentAndDelete_When_DeleteWalletExists()
    {
        ArrangeWallet(new Wallet { Id = 1 });

        var result = await _sut.DeleteWallet(1);

        Assert.IsType<NoContentResult>(result);
        await _wallets.Received(1).DeleteAsync(1);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_DeleteWalletMissing()
    {
        ArrangeWallet(null);

        var result = await _sut.DeleteWallet(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
