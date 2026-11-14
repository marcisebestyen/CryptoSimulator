using AutoMapper;
using CryptoSimulator.Controllers;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit.Controllers;

public class TransactionControllerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Transactions> _repo = Substitute.For<IRepository<Transactions>>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ITransactionService _service = Substitute.For<ITransactionService>();
    private readonly TransactionController _sut;

    public TransactionControllerTests()
    {
        _uow.TransactionRepository.Returns(_repo);
        _sut = new TransactionController(_uow, _mapper, _service);
    }

    private void ArrangeTransactions(params Transactions[] items) =>
        _repo.GetAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Transactions, bool>>>(), Arg.Any<string[]?>())
            .Returns(items);

    [Fact]
    public async Task Should_ReturnOkWithDto_When_TransactionExists()
    {
        var tx = new Transactions { Id = 1 };
        var dto = new TransactionsGetDto();
        ArrangeTransactions(tx);
        _mapper.Map<TransactionsGetDto>(tx).Returns(dto);

        var result = await _sut.GetTransaction(1);

        Assert.Same(dto, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_TransactionMissing()
    {
        ArrangeTransactions();

        var result = await _sut.GetTransaction(8);

        var nf = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Transaction with ID 8 not found.", nf.Value);
    }

    [Fact]
    public async Task Should_ReturnTransactions_When_UserHasWallet()
    {
        var list = new List<TransactionsGetDto>();
        _service.GetUserTransactionsAsync(1, false).Returns(list);

        var result = await _sut.GetUserTransactions(1, false);

        Assert.Same(list, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_DefaultToNewestFirst_When_FlagOmitted()
    {
        _service.GetUserTransactionsAsync(1, true).Returns(new List<TransactionsGetDto>());

        await _sut.GetUserTransactions(1);

        await _service.Received(1).GetUserTransactionsAsync(1, true);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ServiceReturnsNull()
    {
        _service.GetUserTransactionsAsync(1, true).Returns((IEnumerable<TransactionsGetDto>)null!);

        var result = await _sut.GetUserTransactions(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
