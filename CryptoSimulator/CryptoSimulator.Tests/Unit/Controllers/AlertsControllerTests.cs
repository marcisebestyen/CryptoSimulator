using CryptoSimulator.Controllers;
using CryptoSimulator.DTOs;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit.Controllers;

public class AlertsControllerTests
{
    private readonly IPriceAlertService _service = Substitute.For<IPriceAlertService>();
    private readonly AlertsController _sut;

    public AlertsControllerTests()
    {
        _sut = new AlertsController(_service, Substitute.For<ILogger<AlertsController>>());
    }

    private static PriceAlertCreateDto Create() =>
        new() { UserId = 1, CryptoId = 2, TargetPrice = 5m, AlertType = "above" };

    private static int StatusOf(object? r) => Assert.IsType<ObjectResult>(r).StatusCode!.Value;

    [Fact]
    public void Should_ThrowArgumentNull_When_ServiceNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AlertsController(null!, Substitute.For<ILogger<AlertsController>>()));
    }

    [Fact]
    public void Should_ThrowArgumentNull_When_LoggerNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AlertsController(_service, null!));
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreateModelStateInvalid()
    {
        _sut.ModelState.AddModelError("AlertType", "required");

        var result = await _sut.CreateAlert(Create());

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _service.DidNotReceive().CreateAlertAsync(Arg.Any<PriceAlertCreateDto>());
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CreateErrorSaysNotFound()
    {
        _service.CreateAlertAsync(Arg.Any<PriceAlertCreateDto>()).Returns((null, "User not found."));

        var result = await _sut.CreateAlert(Create());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreateOtherError()
    {
        _service.CreateAlertAsync(Arg.Any<PriceAlertCreateDto>()).Returns((null, "Invalid type."));

        var result = await _sut.CreateAlert(Create());

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_Return500_When_CreateReturnsNoAlertAndNoError()
    {
        _service.CreateAlertAsync(Arg.Any<PriceAlertCreateDto>()).Returns((null, null));

        var result = await _sut.CreateAlert(Create());

        Assert.Equal(500, StatusOf(result.Result));
    }

    [Fact]
    public async Task Should_ReturnCreatedWithLocation_When_CreateSucceeds()
    {
        var alert = new PriceAlertGetDto { Id = 7, UserId = 1 };
        _service.CreateAlertAsync(Arg.Any<PriceAlertCreateDto>()).Returns((alert, null));

        var result = await _sut.CreateAlert(Create());

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("/api/alerts/7", created.Location);
        Assert.Same(alert, created.Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ActiveAlertsErrorSaysNotFound()
    {
        _service.GetActiveAlertsForUserAsync(1).Returns((null, "User not found"));

        var result = await _sut.GetActiveAlertsForUser(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_ActiveAlertsOtherError()
    {
        _service.GetActiveAlertsForUserAsync(1).Returns((null, "boom"));

        var result = await _sut.GetActiveAlertsForUser(1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_Return500_When_ActiveAlertsNullWithoutError()
    {
        _service.GetActiveAlertsForUserAsync(1).Returns((null, null));

        var result = await _sut.GetActiveAlertsForUser(1);

        Assert.Equal(500, StatusOf(result.Result));
    }

    [Fact]
    public async Task Should_ReturnOk_When_ActiveAlertsFound()
    {
        var alerts = new List<PriceAlertGetDto> { new() };
        _service.GetActiveAlertsForUserAsync(1).Returns((alerts, null));

        var result = await _sut.GetActiveAlertsForUser(1);

        Assert.Same(alerts, Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    [Fact]
    public async Task Should_ReturnNoContent_When_DeleteSucceeds()
    {
        _service.DeleteAlertAsync(1).Returns((true, null));

        var result = await _sut.DeleteAlert(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_DeleteErrorSaysNotFound()
    {
        _service.DeleteAlertAsync(1).Returns((false, "Alert not found."));

        var result = await _sut.DeleteAlert(1);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_Return500WithServiceError_When_DeleteOtherError()
    {
        _service.DeleteAlertAsync(1).Returns((false, "db down"));

        var result = await _sut.DeleteAlert(1);

        Assert.Equal(500, StatusOf(result));
    }

    [Fact]
    public async Task Should_Return500_When_DeleteFailsWithoutError()
    {
        _service.DeleteAlertAsync(1).Returns((false, null));

        var result = await _sut.DeleteAlert(1);

        Assert.Equal(500, StatusOf(result));
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_HistoryErrorSaysNotFound()
    {
        _service.GetTriggeredAlertsHistoryForUserAsync(1).Returns((null, "User not found"));

        var result = await _sut.GetAlertHistoryForUser(1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_HistoryOtherError()
    {
        _service.GetTriggeredAlertsHistoryForUserAsync(1).Returns((null, "boom"));

        var result = await _sut.GetAlertHistoryForUser(1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_Return500_When_HistoryNullWithoutError()
    {
        _service.GetTriggeredAlertsHistoryForUserAsync(1).Returns((null, null));

        var result = await _sut.GetAlertHistoryForUser(1);

        Assert.Equal(500, StatusOf(result.Result));
    }

    [Fact]
    public async Task Should_ReturnOk_When_HistoryFound()
    {
        var alerts = new List<PriceAlertGetDto>();
        _service.GetTriggeredAlertsHistoryForUserAsync(1).Returns((alerts, null));

        var result = await _sut.GetAlertHistoryForUser(1);

        Assert.Same(alerts, Assert.IsType<OkObjectResult>(result.Result).Value);
    }
}
