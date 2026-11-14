using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class WalletsApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task GetUserWallet_ReturnsStartingBalanceAndNoHoldings_ForNewUser()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.GetAsync($"/api/users/{user.Id}/wallet");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wallet = await response.Content.ReadFromJsonAsync<UserWalletDetailsDto>();
        Assert.Equal(user.Id, wallet!.UserId);
        Assert.Equal(1000m, wallet.FiatBalance);
        Assert.Empty(wallet.OwnedCryptos);
    }

    [Fact]
    public async Task GetUserWallet_Returns404_WhenUserMissing()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/users/2000000000/wallet");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserWallet_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/users/1/wallet");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserWallet_Returns403_WhenReadingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.GetAsync($"/api/users/{b.Id}/wallet");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWallet_ReturnsNewBalance_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, user.Id);

        var response = await user.Client.PutAsJsonAsync($"/api/wallets/{walletId}", new WalletPutDto { Balance = 2500m });
        var details = await user.Client.GetFromJsonAsync<UserWalletDetailsDto>($"/api/users/{user.Id}/wallet");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<WalletGetDto>();
        Assert.Equal(2500m, body!.Balance);
        Assert.Equal(user.Id, body.UserId);
        Assert.Equal(2500m, details!.FiatBalance);
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
        var get = await user.Client.GetAsync($"/api/users/{user.Id}/wallet");

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
