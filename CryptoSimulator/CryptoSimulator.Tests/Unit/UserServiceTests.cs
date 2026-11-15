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
    private readonly IRecoveryCodeService _recovery = Substitute.For<IRecoveryCodeService>();

    private static User Existing(string? password = null) =>
        new() { Id = 1, Username = "Bob", Email = "Bob@Mail.com", Password = password ?? BCrypt.Net.BCrypt.HashPassword("secret") };

    public UserServiceTests()
    {
        _users = RepositoryFakes.WithData(Existing());
        _uow.UserRepository.Returns(_users);
        _uow.WalletRepository.Returns(_wallets);
        _recovery.Generate().Returns("Ab1!Ab1!");
        _recovery.Hash(Arg.Any<string>()).Returns(call => "H-" + call.Arg<string>());
    }

    private UserService Sut() => new(_uow, _recovery);

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
        Assert.Equal(Role.User, result.CreatedUser.Role);
        await _users.Received(1).InsertAsync(result.CreatedUser);
        await _wallets.Received(1).InsertAsync(Arg.Is<Wallet>(w => w.Balance == 1000m && w.User == result.CreatedUser));
    }

    [Fact]
    public async Task Should_StoreRecoveryCodeHashAndReturnCode_When_Registering()
    {
        _recovery.Hash("Ab1!Ab1!").Returns("H1");

        var result = await Sut().RegisterAsync("alice", "alice@mail.com", "pw");

        Assert.Equal("H1", result.CreatedUser!.RecoveryCodeHash);
        Assert.Equal("Ab1!Ab1!", result.RecoveryCode);
        await _users.Received(1).InsertAsync(Arg.Is<User>(u => u.RecoveryCodeHash == "H1"));
    }

    [Fact]
    public async Task Should_RegenerateCode_When_HashAlreadyTaken()
    {
        var taken = Existing();
        taken.RecoveryCodeHash = "H1";
        var repo = RepositoryFakes.WithData(taken);
        _uow.UserRepository.Returns(repo);
        _recovery.Generate().Returns("c1", "c2");
        _recovery.Hash("c1").Returns("H1");
        _recovery.Hash("c2").Returns("H2");

        var result = await Sut().RegisterAsync("alice", "alice@mail.com", "pw");

        Assert.Equal("c2", result.RecoveryCode);
        Assert.Equal("H2", result.CreatedUser!.RecoveryCodeHash);
    }

    [Fact]
    public async Task Should_Throw_When_FiveCodesClash()
    {
        var taken = Existing();
        taken.RecoveryCodeHash = "H1";
        var repo = RepositoryFakes.WithData(taken);
        _uow.UserRepository.Returns(repo);
        _recovery.Hash(Arg.Any<string>()).Returns("H1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Sut().RegisterAsync("alice", "alice@mail.com", "pw"));
        _recovery.Received(5).Generate();
    }

    [Fact]
    public async Task Should_StoreBCryptHash_When_Registering()
    {
        var result = await Sut().RegisterAsync("alice", "alice@mail.com", "pw");

        var stored = result.CreatedUser!.Password;
        Assert.NotEqual("hashed_pw", stored);
        Assert.StartsWith("$2", stored);
        Assert.True(BCrypt.Net.BCrypt.Verify("pw", stored));
    }

    [Fact]
    public async Task Should_StoreBCryptHash_When_ChangingPassword()
    {
        var user = Existing();
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>())
            .Returns(Task.FromResult<User?>(user));

        await Sut().ChangePasswordAsync(1, "secret", "new");

        Assert.NotEqual("hashed_new", user.Password);
        Assert.StartsWith("$2", user.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("new", user.Password));
    }

    [Fact]
    public async Task Should_ReturnFalse_When_StoredPasswordIsLegacyPlaceholder()
    {
        var repo = RepositoryFakes.WithData(Existing(password: "hashed_secret"));
        _uow.UserRepository.Returns(repo);

        Assert.False(await Sut().LoginAsync("bob@mail.com", "secret"));
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
        Assert.True(BCrypt.Net.BCrypt.Verify("new", user.Password));
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

    private const string InvalidResetMessage = "Invalid email or recovery code.";

    private static User WithRecoveryCode(string code = "Ab1!Ab1!")
    {
        var user = Existing();
        user.RecoveryCodeHash = "H-" + code;
        return user;
    }

    private UserService SutWithUser(User user)
    {
        var repo = RepositoryFakes.WithData(user);
        _uow.UserRepository.Returns(repo);
        return Sut();
    }

    [Fact]
    public async Task Should_ResetPasswordAndRotateCode_When_EmailAndCodeMatch()
    {
        var user = WithRecoveryCode();
        _recovery.Generate().Returns("Zz9?Zz9?");

        var result = await SutWithUser(user).ResetPasswordAsync("bob@mail.com", "Ab1!Ab1!", "brand-new");

        Assert.True(result.Succeeded);
        Assert.Equal("Zz9?Zz9?", result.NewRecoveryCode);
        Assert.Equal("H-Zz9?Zz9?", user.RecoveryCodeHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("brand-new", user.Password));
        await _uow.UserRepository.Received(1).UpdateAsync(user);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_Fail_When_CodeWrong()
    {
        var user = WithRecoveryCode();
        var oldPassword = user.Password;

        var result = await SutWithUser(user).ResetPasswordAsync("bob@mail.com", "Wrong123", "brand-new");

        Assert.False(result.Succeeded);
        Assert.Equal(InvalidResetMessage, result.Error);
        Assert.Equal(oldPassword, user.Password);
        Assert.Equal("H-Ab1!Ab1!", user.RecoveryCodeHash);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_Fail_When_UserHasNoRecoveryCode()
    {
        var user = Existing();
        var oldPassword = user.Password;

        var result = await SutWithUser(user).ResetPasswordAsync("bob@mail.com", "Ab1!Ab1!", "brand-new");

        Assert.False(result.Succeeded);
        Assert.Equal(InvalidResetMessage, result.Error);
        Assert.Equal(oldPassword, user.Password);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_Fail_When_CodeIsWhitespace()
    {
        var user = WithRecoveryCode();

        var result = await SutWithUser(user).ResetPasswordAsync("bob@mail.com", "   ", "brand-new");

        Assert.False(result.Succeeded);
        Assert.Equal(InvalidResetMessage, result.Error);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_Fail_When_EmailUnknown()
    {
        var result = await SutWithUser(WithRecoveryCode()).ResetPasswordAsync("ghost@mail.com", "Ab1!Ab1!", "brand-new");

        Assert.False(result.Succeeded);
        Assert.Equal(InvalidResetMessage, result.Error);
    }

    [Fact]
    public async Task Should_AcceptEmail_IgnoringCase()
    {
        _recovery.Generate().Returns("Zz9?Zz9?");

        var result = await SutWithUser(WithRecoveryCode()).ResetPasswordAsync("BOB@MAIL.COM", "Ab1!Ab1!", "brand-new");

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Should_Fail_AndKeepCode_When_NewPasswordBlank(string newPassword)
    {
        var user = WithRecoveryCode();

        var result = await SutWithUser(user).ResetPasswordAsync("bob@mail.com", "Ab1!Ab1!", newPassword);

        Assert.False(result.Succeeded);
        Assert.Equal("New password is required.", result.Error);
        Assert.Equal("H-Ab1!Ab1!", user.RecoveryCodeHash);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Theory]
    [InlineData("", "Ab1!Ab1!")]
    [InlineData("bob@mail.com", "")]
    public async Task Should_Fail_When_EmailOrCodeBlank(string email, string code)
    {
        var result = await SutWithUser(WithRecoveryCode()).ResetPasswordAsync(email, code, "brand-new");

        Assert.False(result.Succeeded);
        Assert.Equal(InvalidResetMessage, result.Error);
    }

    [Fact]
    public async Task Should_Fail_When_SaveThrowsOnReset()
    {
        _recovery.Generate().Returns("Zz9?Zz9?");
        _uow.SaveAsync().ThrowsAsync(new InvalidOperationException("db"));

        var result = await SutWithUser(WithRecoveryCode()).ResetPasswordAsync("bob@mail.com", "Ab1!Ab1!", "brand-new");

        Assert.False(result.Succeeded);
        Assert.Equal("An error occurred while saving the new password.", result.Error);
    }
}
