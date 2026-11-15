using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.Data;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoSimulator.Tests.Integration;

public class AdminSeedingApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    private async Task<List<User>> SeededAdminsAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CryptoSimulationDbContext>();
        return await db.Users.Where(u => u.Role == Role.Admin && u.Email == ApiFactory.AdminEmail).ToListAsync();
    }

    [Fact]
    public async Task Startup_CreatesExactlyOneAdmin_WithConfiguredEmail()
    {
        using var _ = Factory.CreateClient();

        var admins = await SeededAdminsAsync();

        var admin = Assert.Single(admins);
        Assert.Equal(ApiFactory.AdminEmail, admin.Email);
        Assert.Null(admin.RecoveryCodeHash);
    }

    [Fact]
    public async Task Seeder_KeepsOneAdmin_WhenRunAgain()
    {
        using var _ = Factory.CreateClient();

        using (var scope = Factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAdminSeeder>().SeedAsync();
        }

        Assert.Single(await SeededAdminsAsync());
    }

    [Fact]
    public async Task Admin_CanLogIn_WithConfiguredPassword()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = ApiFactory.AdminEmail, Password = ApiFactory.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
