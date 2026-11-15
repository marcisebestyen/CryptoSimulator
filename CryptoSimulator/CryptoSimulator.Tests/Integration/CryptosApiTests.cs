using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public class CryptosApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task Create_Returns201WithLocation_WhenValid()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var name = $"coin-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = name, InitialPrice = 10m });

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
    public async Task Create_ListsCryptoImmediatelyWithInitialPrice()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var crypto = await TestApi.CreateCryptoAsync(client, 42.5m);
        var list = await client.GetFromJsonAsync<List<CryptoGetWithCurrentValueDto>>("/api/cryptos");

        Assert.Equal(42.5m, Assert.Single(list!, c => c.Id == crypto.Id).CurrentExchangeRate);
    }

    [Fact]
    public async Task Create_RecordsSingleOpenEndedPriceHistory()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var crypto = await TestApi.CreateCryptoAsync(client, 42.5m);
        var page = await client.GetFromJsonAsync<PagedResult<CryptoPriceHistoryDto>>($"/api/cryptos/{crypto.Id}/price-history");

        Assert.NotNull(page);
        var record = Assert.Single(page.Items);
        Assert.Equal(42.5m, record.Value);
        Assert.Equal(new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc), record.To);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Create_Returns400_WhenPriceNotPositive(double price)
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.PostAsJsonAsync("/api/cryptos",
            new CryptoPostDto { Name = $"coin-{Guid.NewGuid():N}", InitialPrice = (decimal)price });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns400_WhenPriceMissing()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.PostAsJsonAsync("/api/cryptos", new { name = $"coin-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = "x", InitialPrice = 1m });

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
    public async Task PriceHistory_ReturnsNewestFirst()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);
        (await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price", new ManualPriceUpdateDto { NewPrice = 30m }))
            .EnsureSuccessStatusCode();

        var response = await client.GetAsync($"/api/cryptos/{crypto.Id}/price-history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<CryptoPriceHistoryDto>>();
        Assert.NotNull(page);
        Assert.Equal(new[] { 30m, 10m }, page.Items.Select(h => h.Value).ToArray());
        Assert.True(page.Items[1].To <= page.Items[0].From);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task PriceHistory_Returns25_ByDefault_WithTotalCount()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 1m);
        await SeedHistoryAsync(crypto.Id, 29);

        var page = await client.GetFromJsonAsync<PagedResult<CryptoPriceHistoryDto>>($"/api/cryptos/{crypto.Id}/price-history");

        Assert.NotNull(page);
        Assert.Equal(25, page.Items.Count);
        Assert.Equal(30, page.TotalCount);
        Assert.Equal(1, page.Page);
        Assert.Equal(25, page.PageSize);
    }

    [Fact]
    public async Task PriceHistory_Page2_ReturnsRemaining()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 1m);
        await SeedHistoryAsync(crypto.Id, 29);

        var page = await client.GetFromJsonAsync<PagedResult<CryptoPriceHistoryDto>>(
            $"/api/cryptos/{crypto.Id}/price-history?page=2");

        Assert.NotNull(page);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(30, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(new[] { 6m, 5m, 4m, 3m, 2m }, page.Items.Select(h => h.Value).ToArray());
    }

    [Theory]
    [InlineData(99)]
    [InlineData(int.MaxValue)]
    public async Task PriceHistory_ReturnsEmptyItems_When_PageBeyondRange(int pageNumber)
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 1m);
        await SeedHistoryAsync(crypto.Id, 29);

        var response = await client.GetAsync($"/api/cryptos/{crypto.Id}/price-history?page={pageNumber}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<CryptoPriceHistoryDto>>();
        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(30, page.TotalCount);
        Assert.Equal(pageNumber, page.Page);
    }

    [Fact]
    public async Task PriceHistory_CapsPageSizeAt100()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 1m);
        await SeedHistoryAsync(crypto.Id, 104);

        var page = await client.GetFromJsonAsync<PagedResult<CryptoPriceHistoryDto>>(
            $"/api/cryptos/{crypto.Id}/price-history?pageSize=500");

        Assert.NotNull(page);
        Assert.Equal(100, page.Items.Count);
        Assert.Equal(100, page.PageSize);
        Assert.Equal(105, page.TotalCount);
    }

    // (page - 1) * pageSize = 4294967300, which wraps to 4 in int math and would return real rows.
    [Fact]
    public async Task PriceHistory_ReturnsEmptyItems_When_OffsetOverflowsInt()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 1m);
        await SeedHistoryAsync(crypto.Id, 29);

        var page = await client.GetFromJsonAsync<PagedResult<CryptoPriceHistoryDto>>(
            $"/api/cryptos/{crypto.Id}/price-history?page=42949674&pageSize=100");

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(30, page.TotalCount);
    }

    [Fact]
    public async Task PriceHistory_Returns400_When_PageIsZero()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 1m);

        var response = await client.GetAsync($"/api/cryptos/{crypto.Id}/price-history?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PriceHistory_Returns400_When_PageSizeIsZero()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 1m);

        var response = await client.GetAsync($"/api/cryptos/{crypto.Id}/price-history?pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PriceHistory_Returns404_WhenCryptoMissing()
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;

        var response = await client.GetAsync("/api/cryptos/2000000000/price-history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Adds old closed logs, so the crypto ends up with extraLogs + its current log.
    private async Task SeedHistoryAsync(int cryptoId, int extraLogs)
    {
        var start = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var logs = Enumerable.Range(0, extraLogs).Select(i => new CryptoLog
        {
            CryptoId = cryptoId,
            CurrentValue = i + 2,
            From = start.AddMinutes(i),
            To = start.AddMinutes(i + 1)
        });
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        db.CryptoLogs.AddRange(logs);
        await db.SaveChangesAsync();
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

    [Theory]
    [InlineData(OfferStatus.Open)]
    [InlineData(OfferStatus.Cancelled)]
    public async Task Delete_Returns409_WhenAnOfferReferencesTheCrypto(OfferStatus status)
    {
        var client = (await TestApi.CreateAdminAsync(Factory)).Client;
        var crypto = await TestApi.CreateCryptoAsync(client, 10m);
        var seller = await TestApi.CreateUserAsync(Factory);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
            db.Offers.Add(new Offer
            {
                SellerWalletId = await TestApi.GetWalletIdAsync(Factory, seller.Id),
                CryptoId = crypto.Id,
                Amount = 1m,
                RemainingAmount = 1m,
                AskingPrice = 10m,
                Status = status,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var delete = await client.DeleteAsync($"/api/cryptos/{crypto.Id}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Contains("offers", await delete.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/cryptos/{crypto.Id}")).StatusCode);
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

        var response = await user.Client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = $"coin-{Guid.NewGuid():N}", InitialPrice = 1m });

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

        var response = await client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = taken, InitialPrice = 1m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
