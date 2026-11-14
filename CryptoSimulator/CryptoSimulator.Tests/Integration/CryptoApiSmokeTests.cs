using System.Net;

namespace CryptoSimulator.Tests.Integration;

public class CryptoApiSmokeTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task GetCryptos_Returns401_WhenUnauthenticated()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/cryptos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCryptos_Returns200_WhenAuthenticated()
    {
        using var client = await Factory.CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/api/cryptos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
