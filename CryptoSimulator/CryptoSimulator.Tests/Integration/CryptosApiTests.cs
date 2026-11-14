using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class CryptosApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task Create_Returns201WithLocation_WhenValid()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();
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
        using var client = await Factory.CreateAuthorizedClientAsync();

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
        using var client = await Factory.CreateAuthorizedClientAsync();
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var response = await client.GetAsync($"/api/cryptos/{crypto.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CryptoGetDto>();
        Assert.Equal(crypto.Name, body!.Name);
    }

    [Fact]
    public async Task Get_Returns404_WhenMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/cryptos/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_IncludesCryptoWithCurrentPrice()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();
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
        using var client = await Factory.CreateAuthorizedClientAsync();
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
        using var client = await Factory.CreateAuthorizedClientAsync();
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var response = await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price",
            new ManualPriceUpdateDto { NewPrice = (decimal)price });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePrice_Returns404_WhenCryptoMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.PutAsJsonAsync("/api/cryptos/2000000000/price",
            new ManualPriceUpdateDto { NewPrice = 5m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PriceHistory_ReturnsLogsOldestFirst_AfterUpdates()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();
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
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/cryptos/2000000000/price-history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns204ThenGet404_WhenExists()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);

        var delete = await client.DeleteAsync($"/api/cryptos/{crypto.Id}");
        var get = await client.GetAsync($"/api/cryptos/{crypto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.DeleteAsync("/api/cryptos/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
