using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class CryptosApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task Create_Returns201WithLocation_WhenValid()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var name = $"coin-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = name });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CryptoGetDto>();
        Assert.Equal(name, body!.Name);
        Assert.EndsWith($"/api/cryptos/{body.Id}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Create_Returns400_WhenBodyInvalid()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.PostAsJsonAsync("/api/cryptos", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Returns200_WhenExists()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var response = await client.GetAsync($"/api/cryptos/{crypto.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CryptoGetDto>();
        Assert.Equal(crypto.Name, body!.Name);
    }

    [Fact]
    public async Task Get_Returns404_WhenMissing()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.GetAsync("/api/cryptos/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_IncludesCryptoWithCurrentPrice()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 12.5m);

        var response = await client.GetAsync("/api/cryptos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<CryptoGetWithCurrentValueDto>>();
        var item = Assert.Single(list!, c => c.Id == crypto.Id);
        Assert.Equal(12.5m, item.CurrentExchangeRate);
    }

    [Fact]
    public async Task UpdatePrice_Returns204AndChangesCurrentPrice_WhenValid()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var response = await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price",
            new ManualPriceUpdateDto { NewPrice = 20m });
        var list = await client.GetFromJsonAsync<List<CryptoGetWithCurrentValueDto>>("/api/cryptos");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(20m, list!.Single(c => c.Id == crypto.Id).CurrentExchangeRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task UpdatePrice_Returns400_WhenPriceNotPositive(double price)
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var response = await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price",
            new ManualPriceUpdateDto { NewPrice = (decimal)price });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePrice_Returns404_WhenCryptoMissing()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.PutAsJsonAsync("/api/cryptos/2000000000/price",
            new ManualPriceUpdateDto { NewPrice = 5m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PriceHistory_ReturnsLogsOldestFirst_AfterUpdates()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);
        (await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price", new ManualPriceUpdateDto { NewPrice = 30m }))
            .EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/cryptos/{crypto.Id}/price-history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<List<CryptoPriceHistoryDto>>();
        Assert.NotNull(history);
        Assert.Equal(new[] { 10m, 30m }, history.Select(h => h.Value).ToArray());
        Assert.True(history[0].To <= history[1].From);
    }

    [Fact]
    public async Task PriceHistory_Returns404_WhenCryptoMissing()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.GetAsync("/api/cryptos/2000000000/price-history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204ThenGet404_WhenExists()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var delete = await client.DeleteAsync($"/api/cryptos/{crypto.Id}");
        var get = await client.GetAsync($"/api/cryptos/{crypto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.DeleteAsync("/api/cryptos/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns403_When_User_IsNotAdmin()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = $"coin-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Rename_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/cryptos/1", new CryptoPutDto { Name = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePrice_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/cryptos/1/price", new ManualPriceUpdateDto { NewPrice = 5m });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.DeleteAsync("/api/cryptos/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_Rename_Returns403_When_User_IsNotAdmin()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 10m);

        var response = await user.Client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}", new CryptoPutDto { Name = "renamed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutPrice_Returns403_When_User_IsNotAdmin()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 10m);

        var response = await user.Client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price",
            new ManualPriceUpdateDto { NewPrice = 20m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns403_When_User_IsNotAdmin()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 10m);

        var response = await user.Client.DeleteAsync($"/api/cryptos/{crypto.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_List_Returns200_When_User_IsNotAdmin()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.GetAsync("/api/cryptos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Rename_Returns204_And_PersistsName_When_Admin()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);
        var newName = $"renamed-{Guid.NewGuid():N}";

        var response = await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}", new CryptoPutDto { Name = newName });
        var get = await client.GetFromJsonAsync<CryptoGetDto>($"/api/cryptos/{crypto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(newName, get!.Name);
    }

    [Fact]
    public async Task Rename_Returns204_When_NameUnchanged()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var response = await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}", new CryptoPutDto { Name = crypto.Name });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Rename_Returns400_When_BodyInvalid()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var response = await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rename_Returns404_When_CryptoMissing()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.PutAsJsonAsync("/api/cryptos/2000000000", new CryptoPutDto { Name = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rename_Returns409_When_NameTaken(bool changeCase)
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var first = await TestApi.CreateCryptoAsync(client, 10m);
        var second = await TestApi.CreateCryptoAsync(client, 10m);
        var taken = changeCase ? first.Name.ToUpperInvariant() : first.Name;

        var response = await client.PutAsJsonAsync($"/api/cryptos/{second.Id}", new CryptoPutDto { Name = taken });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Post_Returns409_When_NameTaken(bool changeCase)
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var existing = await TestApi.CreateCryptoAsync(client, 10m);
        var taken = changeCase ? existing.Name.ToUpperInvariant() : existing.Name;

        var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = taken });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
