using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class TradesApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    // New users start with 1000 fiat.
    private const decimal StartBalance = 1000m;

    [Fact]
    public async Task Buy_Returns200AndMovesFundsToHoldings_WhenAffordable()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);

        var response = await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 2m);
        var wallet = await user.Client.GetFromJsonAsync<UserWalletDetailsDto>($"/api/users/{user.Id}/wallet");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StartBalance - 200m, wallet!.FiatBalance);
        var owned = Assert.Single(wallet.OwnedCryptos);
        Assert.Equal(crypto.Id, owned.CryptoId);
        Assert.Equal(2m, owned.Amount);
    }

    [Fact]
    public async Task Buy_Returns400_WhenInsufficientFunds()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);

        var response = await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 11m);
        var wallet = await user.Client.GetFromJsonAsync<UserWalletDetailsDto>($"/api/users/{user.Id}/wallet");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(StartBalance, wallet!.FiatBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Buy_Returns400_WhenAmountNotPositive(double amount)
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);

        var response = await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, (decimal)amount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Buy_Returns400_WhenCryptoMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await TestApi.BuyAsync(user.Client, user.Id, 2000000000, 1m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Buy_Returns400_WhenCryptoHasNoPrice()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var created = await user.Client.PostAsJsonAsync("/api/cryptos", new CryptoPostDto { Name = $"coin-{Guid.NewGuid():N}" });
        var crypto = (await created.Content.ReadFromJsonAsync<CryptoGetDto>())!;

        var response = await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 1m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Buy_Returns404_WhenUserMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);

        var response = await TestApi.BuyAsync(user.Client, 2000000000, crypto.Id, 1m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Buy_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await TestApi.BuyAsync(client, 1, 1, 1m);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Buy_Returns403_WhenTradingForAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(a.Client, 100m);

        var response = await TestApi.BuyAsync(a.Client, b.Id, crypto.Id, 1m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Sell_Returns200AndCreditsFiat_WhenHoldingsSufficient()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 4m)).EnsureSuccessStatusCode();
        (await user.Client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price", new ManualPriceUpdateDto { NewPrice = 150m }))
            .EnsureSuccessStatusCode();

        var response = await TestApi.SellAsync(user.Client, user.Id, crypto.Id, 1m);
        var wallet = await user.Client.GetFromJsonAsync<UserWalletDetailsDto>($"/api/users/{user.Id}/wallet");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StartBalance - 400m + 150m, wallet!.FiatBalance);
        Assert.Equal(3m, Assert.Single(wallet.OwnedCryptos).Amount);
    }

    [Fact]
    public async Task Sell_Returns200AndRemovesHolding_WhenSellingEntireAmount()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 2m)).EnsureSuccessStatusCode();

        var response = await TestApi.SellAsync(user.Client, user.Id, crypto.Id, 2m);
        var wallet = await user.Client.GetFromJsonAsync<UserWalletDetailsDto>($"/api/users/{user.Id}/wallet");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StartBalance, wallet!.FiatBalance);
        Assert.Empty(wallet.OwnedCryptos);
    }

    [Fact]
    public async Task Sell_Returns400_WhenInsufficientHoldings()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 1m)).EnsureSuccessStatusCode();

        var response = await TestApi.SellAsync(user.Client, user.Id, crypto.Id, 2m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sell_Returns400_WhenNothingOwned()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);

        var response = await TestApi.SellAsync(user.Client, user.Id, crypto.Id, 1m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Sell_Returns400_WhenAmountNotPositive(double amount)
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 1m)).EnsureSuccessStatusCode();

        var response = await TestApi.SellAsync(user.Client, user.Id, crypto.Id, (decimal)amount);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sell_Returns404_WhenUserMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await TestApi.SellAsync(user.Client, 2000000000, 1, 1m);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Portfolio_ReturnsHoldingsAndTotals_AfterBuy()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 50m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 4m)).EnsureSuccessStatusCode();

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/portfolio");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDto>();
        Assert.Equal(StartBalance - 200m, portfolio!.FiatBalance);
        Assert.Equal(200m, portfolio.TotalCryptoValue);
        Assert.Equal(StartBalance, portfolio.TotalPortfolioValue);
        var item = Assert.Single(portfolio.Holdings);
        Assert.Equal(4m, item.AmountOwned);
    }

    [Fact]
    public async Task Portfolio_Returns404_WhenUserMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/users/2000000000/portfolio");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Portfolio_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/users/1/portfolio");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Portfolio_Returns403_WhenReadingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.GetAsync($"/api/users/{b.Id}/portfolio");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProfitLoss_ReturnsGainPerCrypto_WhenPriceRose()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 2m)).EnsureSuccessStatusCode();
        (await user.Client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price", new ManualPriceUpdateDto { NewPrice = 150m }))
            .EnsureSuccessStatusCode();

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/portfolio/profit-loss");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<List<CryptoProfitLossDto>>();
        var row = Assert.Single(rows!);
        Assert.Equal(200m, row.TotalCostBasis);
        Assert.Equal(300m, row.CurrentValue);
        Assert.Equal(100m, row.ProfitLossAmount);
        Assert.Equal(50m, row.ProfitLossPercentage);
    }

    [Fact]
    public async Task ProfitLoss_Returns404_WhenUserMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/users/2000000000/portfolio/profit-loss");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProfitLossSummary_ReturnsTotals_WhenPriceRose()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(user.Client, 100m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 2m)).EnsureSuccessStatusCode();
        (await user.Client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price", new ManualPriceUpdateDto { NewPrice = 150m }))
            .EnsureSuccessStatusCode();

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/portfolio/profit-loss/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PortfolioProfitLossDto>();
        Assert.Equal(200m, summary!.TotalCostBasis);
        Assert.Equal(300m, summary.TotalCurrentCryptoValue);
        Assert.Equal(100m, summary.TotalProfitLossAmount);
        Assert.Equal(50m, summary.TotalProfitLossPercentage);
    }

    [Fact]
    public async Task ProfitLossSummary_Returns404_WhenUserMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/users/2000000000/portfolio/profit-loss/summary");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
