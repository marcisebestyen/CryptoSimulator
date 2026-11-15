using CryptoSimulator.Configuration;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Services;
using NSubstitute;

namespace CryptoSimulator.Tests.Unit;

public class AdminSeederTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<Wallet> _wallets = Substitute.For<IRepository<Wallet>>();
    private IRepository<User> _users = RepositoryFakes.WithData<User>();

    private static readonly AdminSettings Settings = new()
    {
        Username = "admin",
        Email = "Admin@Example.com",
        Password = "long-enough-password",
    };

    private AdminSeeder Sut()
    {
        _uow.UserRepository.Returns(_users);
        _uow.WalletRepository.Returns(_wallets);
        return new AdminSeeder(_uow, Settings);
    }

    [Fact]
    public async Task Should_CreateAdminWithWallet_When_NoAdminExists()
    {
        await Sut().SeedAsync();

        await _users.Received(1).InsertAsync(Arg.Is<User>(u =>
            u.Role == Role.Admin
            && u.Username == "admin"
            && u.Email == "Admin@Example.com"
            && u.FirstName == "Admin"
            && u.LastName == "Admin"
            && u.RecoveryCodeHash == null
            && BCrypt.Net.BCrypt.Verify("long-enough-password", u.Password)));
        await _wallets.Received(1).InsertAsync(Arg.Is<Wallet>(w => w.Balance == 0m && w.User.Role == Role.Admin));
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_DoNothing_When_AdminAlreadyExists()
    {
        _users = RepositoryFakes.WithData(new User { Id = 1, Role = Role.Admin, Username = "other", Email = "other@example.com" });

        await Sut().SeedAsync();

        await _users.DidNotReceiveWithAnyArgs().InsertAsync(default!);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Theory]
    [InlineData("someone", "admin@example.com")]
    [InlineData("ADMIN", "someone@example.com")]
    public async Task Should_Throw_When_NonAdminHasSameUsernameOrEmail(string username, string email)
    {
        _users = RepositoryFakes.WithData(new User { Id = 2, Role = Role.User, Username = username, Email = email });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().SeedAsync());

        Assert.Contains("already in use", ex.Message);
        await _uow.DidNotReceive().SaveAsync();
    }
}
