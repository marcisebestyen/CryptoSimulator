using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class AdminAccessApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    private static PriceAlertCreateDto NewAlert(int userId, int cryptoId) =>
        new() { UserId = userId, CryptoId = cryptoId, TargetPrice = 500m, AlertType = "above" };

    [Theory]
    [InlineData("")]
    [InlineData("/wallet")]
    [InlineData("/portfolio")]
    [InlineData("/portfolio/profit-loss")]
    [InlineData("/portfolio/profit-loss/summary")]
    [InlineData("/transactions")]
    [InlineData("/alerts")]
    [InlineData("/alerts/history")]
    public async Task Admin_Returns200_WhenReadingAnotherUsersData(string suffix)
    {
        var admin = await TestApi.CreateAdminAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);

        var response = await admin.Client.GetAsync($"/api/users/{other.Id}{suffix}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanUpdateAndDeleteAnotherUser()
    {
        var admin = await TestApi.CreateAdminAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);

        var update = await admin.Client.PutAsJsonAsync($"/api/users/{other.Id}",
            new UserPutDto { Username = other.Username, FirstName = "A", LastName = "B", Email = other.Email });
        var delete = await admin.Client.DeleteAsync($"/api/users/{other.Id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Admin_CanUpdateAndDeleteAnotherUsersWallet()
    {
        var admin = await TestApi.CreateAdminAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);
        var walletId = await TestApi.GetWalletIdAsync(Factory, other.Id);

        var update = await admin.Client.PutAsJsonAsync($"/api/wallets/{walletId}", new WalletPutDto { Balance = 5m });
        var delete = await admin.Client.DeleteAsync($"/api/wallets/{walletId}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Admin_CanReadAnotherUsersTransaction()
    {
        var admin = await TestApi.CreateAdminAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 10m);
        (await TestApi.BuyAsync(other.Client, other.Id, crypto.Id, 1m)).EnsureSuccessStatusCode();
        var list = await other.Client.GetFromJsonAsync<List<TransactionsGetDto>>($"/api/users/{other.Id}/transactions");

        var response = await admin.Client.GetAsync($"/api/transactions/{list![0].Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Returns403_WhenChangingAnotherUsersPassword()
    {
        var admin = await TestApi.CreateAdminAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);

        var response = await admin.Client.PostAsJsonAsync($"/api/users/{other.Id}/change-password", new UserChangePasswordDto
        {
            CurrentPassword = other.Password,
            NewPassword = "new-pw-1",
            ConfirmedNewPassword = "new-pw-1",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Returns403_WhenTradingForAnotherUser()
    {
        var admin = await TestApi.CreateAdminAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 10m);

        var buy = await TestApi.BuyAsync(admin.Client, other.Id, crypto.Id, 1m);
        var sell = await TestApi.SellAsync(admin.Client, other.Id, crypto.Id, 1m);

        Assert.Equal(HttpStatusCode.Forbidden, buy.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, sell.StatusCode);
    }

    [Fact]
    public async Task Admin_Returns403_WhenCreatingOrDeletingAnotherUsersAlert()
    {
        var admin = await TestApi.CreateAdminAsync(Factory);
        var other = await TestApi.CreateUserAsync(Factory);
        var crypto = await TestApi.CreateCryptoAsync(Factory, 10m);
        var created = await other.Client.PostAsJsonAsync("/api/alerts", NewAlert(other.Id, crypto.Id));
        var alert = (await created.Content.ReadFromJsonAsync<PriceAlertGetDto>())!;

        var create = await admin.Client.PostAsJsonAsync("/api/alerts", NewAlert(other.Id, crypto.Id));
        var delete = await admin.Client.DeleteAsync($"/api/alerts/{alert.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}
