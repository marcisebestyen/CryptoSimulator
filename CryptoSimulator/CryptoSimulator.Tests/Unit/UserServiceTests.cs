using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CryptoSimulator.Tests.Unit;

public class UserServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<User> _users;
    private readonly IRepository<Wallet> _wallets = Substitute.For<IRepository<Wallet>>();

    private static User Existing(string password = "hashed_secret") =>
        new() { Id = 1, Username = "Bob", Email = "Bob@Mail.com", Password = password };

    public UserServiceTests()
    {
        _users = RepositoryFakes.WithData(Existing());
        _uow.UserRepository.Returns(_users);
        _uow.WalletRepository.Returns(_wallets);
    }

    private UserService Sut() => new(_uow);

    [Theory]
    [InlineData("", "pw")]
    [InlineData("   ", "pw")]
    [InlineData("bob@mail.com", "")]
    [InlineData("bob@mail.com", " ")]
    public async Task Should_ReturnFalse_When_LoginInputBlank(string email, string password)
    {
        Assert.False(await Sut().LoginAsync(email, password));
    }

    [Fact]
    public async Task Should_ReturnTrue_When_LoginCredentialsCorrectIgnoringEmailCase()
    {
        Assert.True(await Sut().LoginAsync("bob@MAIL.com", "secret"));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_LoginPasswordWrong()
    {
        Assert.False(await Sut().LoginAsync("bob@mail.com", "nope"));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_LoginUserMissing()
    {
        Assert.False(await Sut().LoginAsync("ghost@mail.com", "secret"));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_StoredPasswordEmpty()
    {
        var repo = RepositoryFakes.WithData(Existing(password: ""));
        _uow.UserRepository.Returns(repo);

        Assert.False(await Sut().LoginAsync("bob@mail.com", "secret"));
    }

    [Fact]
    public async Task Should_CreateUserAndWallet_When_RegisterInputValid()
    {
        var result = await Sut().RegisterAsync("alice", "alice@mail.com", "pw");

        Assert.True(result.Succeeded);
        Assert.Equal("alice", result.CreatedUser!.Username);
        Assert.Equal("hashed_pw", result.CreatedUser.Password);
        Assert.Equal(Role.User, result.CreatedUser.Role);
        await _users.Received(1).InsertAsync(result.CreatedUser);
        await _wallets.Received(1).InsertAsync(Arg.Is<Wallet>(w => w.Balance == 1000m && w.User == result.CreatedUser));
    }

    [Fact]
    public async Task Should_ReturnAllErrors_When_RegisterInputBlank()
    {
        var result = await Sut().RegisterAsync("", "", "");

        Assert.False(result.Succeeded);
        Assert.Equal(3, result.Errors.Count());
        await _users.DidNotReceive().InsertAsync(Arg.Any<User>());
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("Name <a@b.com>")]
    public async Task Should_ReturnEmailError_When_EmailInvalid(string email)
    {
        var result = await Sut().RegisterAsync("alice", email, "pw");

        Assert.False(result.Succeeded);
        Assert.Contains("A valid email is required.", result.Errors);
    }

    [Fact]
    public async Task Should_Fail_When_RegisterEmailAlreadyUsed()
    {
        var result = await Sut().RegisterAsync("alice", "BOB@mail.com", "pw");

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        await _users.DidNotReceive().InsertAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task Should_Fail_When_RegisterUsernameAlreadyUsed()
    {
        var result = await Sut().RegisterAsync("BOB", "new@mail.com", "pw");

        Assert.False(result.Succeeded);
        Assert.Contains("Email or username already in use.", result.Errors);
    }

    [Fact]
    public async Task Should_ReportOneError_When_EmailAndUsernameBothUsed()
    {
        var result = await Sut().RegisterAsync("bob", "bob@mail.com", "pw");

        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData("", "new")]
    [InlineData("old", " ")]
    public async Task Should_Fail_When_ChangePasswordInputBlank(string current, string next)
    {
        var result = await Sut().ChangePasswordAsync(1, current, next);

        Assert.False(result.Succeeded);
        Assert.Contains("Current and new passwords are required.", result.Errors);
    }

    [Fact]
    public async Task Should_Fail_When_NewPasswordEqualsCurrent()
    {
        var result = await Sut().ChangePasswordAsync(1, "same", "same");

        Assert.False(result.Succeeded);
        Assert.Contains("New password cannot be the same as the current password.", result.Errors);
    }

    [Fact]
    public async Task Should_Fail_When_ChangePasswordUserMissing()
    {
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(Task.FromResult<User?>(null));

        var result = await Sut().ChangePasswordAsync(99, "secret", "new");

        Assert.Contains("User not found.", result.Errors);
    }

    [Fact]
    public async Task Should_Fail_When_CurrentPasswordIncorrect()
    {
        var user = Existing();
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(Task.FromResult<User?>(user));

        var result = await Sut().ChangePasswordAsync(1, "wrong", "new");

        Assert.Contains("Incorrect current password.", result.Errors);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_UpdateAndSave_When_ChangePasswordValid()
    {
        var user = Existing();
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(Task.FromResult<User?>(user));

        var result = await Sut().ChangePasswordAsync(1, "secret", "new");

        Assert.True(result.Succeeded);
        Assert.Equal("hashed_new", user.Password);
        await _users.Received(1).UpdateAsync(user);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_Fail_When_SaveThrowsOnChangePassword()
    {
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(Task.FromResult<User?>(Existing()));
        _uow.SaveAsync().ThrowsAsync(new InvalidOperationException("db"));

        var result = await Sut().ChangePasswordAsync(1, "secret", "new");

        Assert.False(result.Succeeded);
        Assert.Contains("An error occurred while saving the new password.", result.Errors);
    }
}
