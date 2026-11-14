using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class TransactionsApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task GetUserTransactions_ReturnsEmpty_ForNewUser()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<TransactionsGetDto>>())!);
    }

    [Fact]
    public async Task GetUserTransactions_ReturnsNewestFirstByDefault_AfterTrades()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 10m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 3m)).EnsureSuccessStatusCode();
        (await TestApi.SellAsync(user.Client, user.Id, crypto.Id, 1m)).EnsureSuccessStatusCode();

        var newest = await user.Client.GetFromJsonAsync<List<TransactionsGetDto>>($"/api/users/{user.Id}/transactions");
        var oldest = await user.Client.GetFromJsonAsync<List<TransactionsGetDto>>($"/api/users/{user.Id}/transactions?newestFirst=false");

        Assert.Equal(new[] { "Sale", "Purchase" }, newest!.Select(t => t.TransactionType).ToArray());
        Assert.Equal(new[] { "Purchase", "Sale" }, oldest!.Select(t => t.TransactionType).ToArray());
        Assert.All(newest!, t => Assert.Equal(crypto.Id, t.CryptoId));
        Assert.Equal(10m, newest![0].ExchangeRate);
    }

    [Fact]
    public async Task GetUserTransactions_Returns404_WhenUserMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/users/2000000000/transactions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserTransactions_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/users/1/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserTransactions_Returns403_WhenReadingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.GetAsync($"/api/users/{b.Id}/transactions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTransaction_Returns200_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 10m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 3m)).EnsureSuccessStatusCode();
        var list = await user.Client.GetFromJsonAsync<List<TransactionsGetDto>>($"/api/users/{user.Id}/transactions");

        var response = await user.Client.GetAsync($"/api/transactions/{list![0].Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionsGetDto>();
        Assert.Equal(3m, body!.Amount);
        Assert.True(body.IsPurchase);
    }

    [Fact]
    public async Task GetTransaction_Returns404_WhenMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/transactions/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTransaction_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/transactions/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTransaction_Returns403_WhenTransactionBelongsToAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(a.Client, 10m);
        (await TestApi.BuyAsync(b.Client, b.Id, crypto.Id, 1m)).EnsureSuccessStatusCode();
        var list = await b.Client.GetFromJsonAsync<List<TransactionsGetDto>>($"/api/users/{b.Id}/transactions");

        var response = await a.Client.GetAsync($"/api/transactions/{list![0].Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
