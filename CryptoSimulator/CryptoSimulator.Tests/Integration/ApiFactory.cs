using System.Net.Http.Headers;
using System.Net.Http.Json;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program reads these while building the host, so they must be set before it runs.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789");
        builder.UseSetting("Jwt:Issuer", "crypto-simulator-tests");
        builder.UseSetting("Jwt:Audience", "crypto-simulator-tests");

        builder.ConfigureServices(services =>
        {
            var hosted = services.Where(d => d.ImplementationType == typeof(PriceUpdateService)).ToList();
            foreach (var descriptor in hosted)
            {
                services.Remove(descriptor);
            }
        });
    }

    public async Task MigrateAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var email = $"{suffix}@example.com";
        const string password = "P@ssw0rd-test";

        var register = await client.PostAsJsonAsync("/api/users/register",
            new UserRegisterDto { Username = $"user-{suffix}", Email = email, Password = password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = email, Password = password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginResponseDto>()
            ?? throw new InvalidOperationException("Login returned an empty body.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return client;
    }
}
