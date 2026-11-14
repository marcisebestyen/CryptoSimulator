using System.Net.Http.Headers;
using System.Net.Http.Json;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public sealed record TestUser(HttpClient Client, int Id, string Username, string Email, string Password);

// Helpers shared by the integration test classes. Every call uses unique data.
public static class TestApi
{
    public const string DefaultPassword = "P@ssw0rd-test";

    public static async Task<TestUser> CreateUserAsync(ApiFactory factory)
    {
        var client = factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var username = $"user-{suffix}";
        var email = $"{suffix}@example.com";

        var register = await client.PostAsJsonAsync("/api/users/register",
            new UserRegisterDto { Username = username, Email = email, Password = DefaultPassword });
        register.EnsureSuccessStatusCode();
        var created = await register.Content.ReadFromJsonAsync<UserGetDto>()
            ?? throw new InvalidOperationException("Register returned an empty body.");

        var login = await client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = email, Password = DefaultPassword });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponseDto>()
            ?? throw new InvalidOperationException("Login returned an empty body.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);

        return new TestUser(client, created.Id, username, email, DefaultPassword);
    }

    // Role is only set in the DB, so the user logs in again to get a token that carries it.
    public static async Task<TestUser> CreateAdminAsync(ApiFactory factory)
    {
        var user = await CreateUserAsync(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
            await db.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, Role.Admin));
        }

        var login = await user.Client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = user.Email, Password = user.Password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponseDto>()
            ?? throw new InvalidOperationException("Login returned an empty body.");
        user.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return user;
    }

    // Writing cryptos is admin-only, so this signs up a throwaway admin to do it.
    public static async Task<CryptoGetDto> CreateCryptoAsync(ApiFactory factory, decimal price)
    {
        var admin = await CreateAdminAsync(factory);
        return await CreateCryptoAsync(admin.Client, price);
    }

    // New crypto has no price log, so a price is set right away to make it tradable.
    public static async Task<CryptoGetDto> CreateCryptoAsync(HttpClient client, decimal price)
    {
        var create = await client.PostAsJsonAsync("/api/cryptos",
            new CryptoPostDto { Name = $"coin-{Guid.NewGuid():N}" });
        create.EnsureSuccessStatusCode();
        var crypto = await create.Content.ReadFromJsonAsync<CryptoGetDto>()
            ?? throw new InvalidOperationException("Create crypto returned an empty body.");

        var set = await client.PutAsJsonAsync($"/api/cryptos/{crypto.Id}/price",
            new ManualPriceUpdateDto { NewPrice = price });
        set.EnsureSuccessStatusCode();
        return crypto;
    }

    public static async Task<int> GetWalletIdAsync(ApiFactory factory, int userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        return await db.Wallets.Where(w => w.UserId == userId).Select(w => w.Id).SingleAsync();
    }

    public static Task<HttpResponseMessage> BuyAsync(HttpClient client, int userId, int cryptoId, decimal amount) =>
        client.PostAsJsonAsync("/api/trades/buy", new BuyCryptoDto { UserId = userId, CryptoId = cryptoId, Amount = amount });

    public static Task<HttpResponseMessage> SellAsync(HttpClient client, int userId, int cryptoId, decimal amount) =>
        client.PostAsJsonAsync("/api/trades/sell", new SellCryptoDto { UserId = userId, CryptoId = cryptoId, Amount = amount });
}
