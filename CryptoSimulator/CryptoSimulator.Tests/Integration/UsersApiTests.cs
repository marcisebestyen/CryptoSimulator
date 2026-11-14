using System.Net;
using System.Net.Http.Json;
using CryptoSimulator.DTOs;

namespace CryptoSimulator.Tests.Integration;

public class UsersApiTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task Register_Returns201AndUser_WhenValid()
    {
        using var client = Factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..12];

        var response = await client.PostAsJsonAsync("/api/users/register",
            new UserRegisterDto { Username = $"u-{suffix}", Email = $"{suffix}@example.com", Password = "pw1" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserGetDto>();
        Assert.NotNull(user);
        Assert.Equal($"u-{suffix}", user.Username);
        Assert.True(user.Id > 0);
    }

    [Fact]
    public async Task Register_Returns400_WhenEmailDuplicate()
    {
        var existing = await TestApi.CreateUserAsync(Factory);
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register",
            new UserRegisterDto { Username = $"other-{Guid.NewGuid():N}", Email = existing.Email.ToUpperInvariant(), Password = "pw1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_Returns400_WhenUsernameDuplicate()
    {
        var existing = await TestApi.CreateUserAsync(Factory);
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register",
            new UserRegisterDto { Username = existing.Username, Email = $"{Guid.NewGuid():N}@example.com", Password = "pw1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "a@example.com", "pw")]
    [InlineData("name", "not-an-email", "pw")]
    [InlineData("name", "a@example.com", "")]
    public async Task Register_Returns400_WhenInputInvalid(string username, string email, string password)
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/register",
            new UserRegisterDto { Username = username, Email = email, Password = password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsTokenAndUser_WhenCredentialsCorrect()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = user.Email, Password = user.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.Equal(user.Id, body.User.Id);
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordWrong()
    {
        var user = await TestApi.CreateUserAsync(Factory);
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = user.Email, Password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns401_WhenUserUnknown()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = $"{Guid.NewGuid():N}@example.com", Password = "pw" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns400_WhenEmailMalformed()
    {
        using var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = "nope", Password = "pw" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_Returns200_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.GetAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserGetDto>();
        Assert.Equal(user.Email, body!.Email);
    }

    [Fact]
    public async Task GetUser_Returns404_WhenMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.GetAsync("/api/users/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_Returns401_WhenNoToken()
    {
        using var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_Returns401_WhenTokenInvalid()
    {
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not.a.token");

        var response = await client.GetAsync("/api/users/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_Returns403_WhenReadingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.GetAsync($"/api/users/{b.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ReturnsUpdatedUser_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PutAsJsonAsync($"/api/users/{user.Id}",
            new { username = user.Username, firstName = "Ada", lastName = "Lovelace", email = $"{Guid.NewGuid():N}@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserGetDto>();
        Assert.Equal("Ada", body!.FirstName);
        Assert.Equal("Lovelace", body.LastName);
        Assert.Equal(user.Email, body.Email);
        var fetched = await user.Client.GetFromJsonAsync<UserGetDto>($"/api/users/{user.Id}");
        Assert.Equal(user.Email, fetched!.Email);
    }

    [Fact]
    public async Task UpdateUser_IgnoresEmail_WhenEmailInBody()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PutAsJsonAsync($"/api/users/{user.Id}",
            new { username = user.Username, firstName = "Grace", lastName = "Hopper", email = "changed@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await user.Client.GetFromJsonAsync<UserGetDto>($"/api/users/{user.Id}");
        Assert.Equal(user.Email, fetched!.Email);
        Assert.Equal("Grace", fetched.FirstName);
    }

    [Fact]
    public async Task UpdateUser_Returns404_WhenMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PutAsJsonAsync("/api/users/2000000000",
            new UserPutDto { Username = "x", FirstName = "x", LastName = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_Returns403_WhenUpdatingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.PutAsJsonAsync($"/api/users/{b.Id}",
            new UserPutDto { Username = "hijack", FirstName = "x", LastName = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns204ThenGet404_WhenExists()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var delete = await user.Client.DeleteAsync($"/api/users/{user.Id}");
        var get = await user.Client.GetAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns404_WhenMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.DeleteAsync("/api/users/2000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_Returns403_WhenDeletingAnotherUser()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.DeleteAsync($"/api/users/{b.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns204AndNewPasswordWorks_WhenValid()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PostAsJsonAsync($"/api/users/{user.Id}/change-password", new UserChangePasswordDto
        {
            CurrentPassword = user.Password,
            NewPassword = "N3w-password",
            ConfirmedNewPassword = "N3w-password"
        });
        var oldLogin = await user.Client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = user.Email, Password = user.Password });
        var newLogin = await user.Client.PostAsJsonAsync("/api/users/login",
            new UserLoginDto { Email = user.Email, Password = "N3w-password" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenCurrentPasswordWrong()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PostAsJsonAsync($"/api/users/{user.Id}/change-password", new UserChangePasswordDto
        {
            CurrentPassword = "wrong",
            NewPassword = "N3w-password",
            ConfirmedNewPassword = "N3w-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenConfirmationMismatch()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PostAsJsonAsync($"/api/users/{user.Id}/change-password", new UserChangePasswordDto
        {
            CurrentPassword = user.Password,
            NewPassword = "N3w-password",
            ConfirmedNewPassword = "different"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenNewEqualsCurrent()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PostAsJsonAsync($"/api/users/{user.Id}/change-password", new UserChangePasswordDto
        {
            CurrentPassword = user.Password,
            NewPassword = user.Password,
            ConfirmedNewPassword = user.Password
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns404_WhenUserMissing()
    {
        var user = await TestApi.CreateUserAsync(Factory);

        var response = await user.Client.PostAsJsonAsync("/api/users/2000000000/change-password", new UserChangePasswordDto
        {
            CurrentPassword = "a",
            NewPassword = "b",
            ConfirmedNewPassword = "b"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Returns403_WhenChangingAnotherUsersPassword()
    {
        var a = await TestApi.CreateUserAsync(Factory);
        var b = await TestApi.CreateUserAsync(Factory);

        var response = await a.Client.PostAsJsonAsync($"/api/users/{b.Id}/change-password", new UserChangePasswordDto
        {
            CurrentPassword = b.Password,
            NewPassword = "N3w-password",
            ConfirmedNewPassword = "N3w-password"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
