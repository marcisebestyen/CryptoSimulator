using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CryptoSimulator.Tests.Integration;

public class CorsApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    private const string AllowedOrigin = "http://localhost:5173";

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/cryptos");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        return request;
    }

    [Fact]
    public async Task Should_ReturnAllowOriginHeader_When_PreflightFromAllowedOrigin()
    {
        using var client = Factory.CreateClient();

        var response = await client.SendAsync(Preflight(AllowedOrigin));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Should_OmitAllowOriginHeader_When_PreflightFromUnknownOrigin()
    {
        using var client = Factory.CreateClient();

        var response = await client.SendAsync(Preflight("http://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public void Should_FailStartup_When_NoAllowedOriginsConfigured()
    {
        using var factory = Factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", ""));

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }
}
