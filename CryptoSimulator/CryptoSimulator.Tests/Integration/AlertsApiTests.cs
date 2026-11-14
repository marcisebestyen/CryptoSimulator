using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public class AlertsApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    private static PriceAlertCreateDto NewAlert(int userId, int cryptoId, string type = "above", decimal target = 100m) =>
        new() { UserId = userId, CryptoId = cryptoId, TargetPrice = target, AlertType = type };

    [Theory]
    [InlineData("above", PriceAlertType.Above)]
    [InlineData("BELOW", PriceAlertType.Below)]
    public async Task Create_Returns201AndActiveAlert_WhenValid(string type, PriceAlertType expected)
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);

        var response = await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id, type));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var alert = await response.Content.ReadFromJsonAsync<PriceAlertGetDto>();
        Assert.Equal(expected, alert!.AlertType);
        Assert.True(alert.IsActive);
        Assert.Equal(crypto.Name, alert.CryptoName);
        Assert.Null(alert.LastTriggeredAt);
    }

    [Fact]
    public async Task Create_Returns400_WhenAlertTypeInvalid()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);

        var response = await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id, "sideways"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns400_WhenTargetPriceNotPositive()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);

        var response = await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id, target: 0m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns404_WhenUserMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);

        var response = await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(2000000000, crypto.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns404_WhenCryptoMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, 2000000000));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/alerts", NewAlert(1, 1));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns403_WhenCreatingForAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);

        var response = await a.Client.PostAsJsonAsync("/api/alerts", NewAlert(b.Id, crypto.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnActiveAlerts()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);
        (await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id))).EnsureSuccessStatusCode();
        (await other.Client.PostAsJsonAsync("/api/alerts", NewAlert(other.Id, crypto.Id))).EnsureSuccessStatusCode();

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var alerts = await response.Content.ReadFromJsonAsync<List<PriceAlertGetDto>>();
        var alert = Assert.Single(alerts!);
        Assert.Equal(user.Id, alert.UserId);
    }

    [Fact]
    public async Task List_Returns404_WhenUserMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/users/2000000000/alerts");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_Returns403_WhenReadingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.GetAsync($"/api/users/{b.Id}/alerts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204AndRemovesFromList_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);
        var created = await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id));
        var alert = (await created.Content.ReadFromJsonAsync<PriceAlertGetDto>())!;

        var delete = await user.Client.DeleteAsync($"/api/alerts/{alert.Id}");
        var alerts = await user.Client.GetFromJsonAsync<List<PriceAlertGetDto>>($"/api/users/{user.Id}/alerts");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(alerts!);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.DeleteAsync("/api/alerts/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns403_WhenAlertBelongsToAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);
        var created = await b.Client.PostAsJsonAsync("/api/alerts", NewAlert(b.Id, crypto.Id));
        var alert = (await created.Content.ReadFromJsonAsync<PriceAlertGetDto>())!;

        var response = await a.Client.DeleteAsync($"/api/alerts/{alert.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task History_IsEmpty_WhenNothingTriggered()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);
        (await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id))).EnsureSuccessStatusCode();

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/alerts/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<PriceAlertGetDto>>())!);
    }

    [Fact]
    public async Task History_ContainsAlert_AfterPriceCrossesTarget()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);
        (await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id, "above", 100m))).EnsureSuccessStatusCode();

        // The background price job is disabled in tests, so run the alert check directly.
        using (var scope = Factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IPriceAlertService>();
            await service.CheckAlertsAsync(crypto.Id, 120m);
        }
        var history = await user.Client.GetFromJsonAsync<List<PriceAlertGetDto>>($"/api/users/{user.Id}/alerts/history");

        var alert = Assert.Single(history!);
        Assert.NotNull(alert.LastTriggeredAt);
        Assert.Equal(crypto.Id, alert.CryptoId);
    }

    [Fact]
    public async Task History_StaysEmpty_WhenPriceDoesNotCrossTarget()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 50m);
        (await user.Client.PostAsJsonAsync("/api/alerts", NewAlert(user.Id, crypto.Id, "above", 100m))).EnsureSuccessStatusCode();

        using (var scope = Factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IPriceAlertService>();
            await service.CheckAlertsAsync(crypto.Id, 90m);
        }
        var history = await user.Client.GetFromJsonAsync<List<PriceAlertGetDto>>($"/api/users/{user.Id}/alerts/history");

        Assert.Empty(history!);
    }

    [Fact]
    public async Task History_Returns404_WhenUserMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/users/2000000000/alerts/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task History_Returns403_WhenReadingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.GetAsync($"/api/users/{b.Id}/alerts/history");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
