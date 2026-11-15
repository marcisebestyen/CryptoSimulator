using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class WalletsApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task Portfolio_ReturnsStartingBalanceAndNoHoldings_ForNewUser()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/portfolio");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var portfolio = await response.Content.ReadFromJsonAsync<PortfolioDto>();
        Assert.Equal(user.Id, portfolio!.UserId);
        Assert.Equal(1000m, portfolio.FiatBalance);
        Assert.Empty(portfolio.Holdings);
    }

    [Fact]
    public async Task Portfolio_ReturnsRateAndValue_ForOwnedCrypto()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 100m);
        (await TestApi.BuyAsync(user.Client, user.Id, crypto.Id, 2m)).EnsureSuccessStatusCode();

        var portfolio = await user.Client.GetFromJsonAsync<PortfolioDto>($"/api/users/{user.Id}/portfolio");

        var owned = Assert.Single(portfolio!.Holdings);
        Assert.Equal(100m, owned.CurrentExchangeRate);
        Assert.Equal(200m, owned.CurrentValue);
    }

    [Fact]
    public async Task UpdateWallet_ReturnsNewBalance_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, user.Id);

        var response = await user.Client.PutAsJsonAsync($"/api/wallets/{walletId}", new WalletPutDto { Balance = 2500m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WalletGetDto>();
        Assert.Equal(2500m, body!.Balance);
        Assert.Equal(user.Id, body.UserId);
    }

    [Fact]
    public async Task UpdateWallet_Returns400_WhenBalanceNegative()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, user.Id);

        var response = await user.Client.PutAsJsonAsync($"/api/wallets/{walletId}", new WalletPutDto { Balance = -1m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWallet_Returns404_WhenMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.PutAsJsonAsync("/api/wallets/2000000000", new WalletPutDto { Balance = 1m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWallet_Returns403_WhenWalletBelongsToAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, b.Id);

        var response = await a.Client.PutAsJsonAsync($"/api/wallets/{walletId}", new WalletPutDto { Balance = 999999m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWallet_Returns204ThenWalletGone_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, user.Id);

        var delete = await user.Client.DeleteAsync($"/api/wallets/{walletId}");
        var get = await user.Client.GetAsync($"/api/users/{user.Id}/portfolio");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteWallet_Returns404_WhenMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.DeleteAsync("/api/wallets/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWallet_Returns403_WhenWalletBelongsToAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, b.Id);

        var response = await a.Client.DeleteAsync($"/api/wallets/{walletId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
