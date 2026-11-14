using CryptoSimulator.Entities;
using CryptoSimulator.Results;

namespace CryptoSimulator.Tests.Unit;

public class ResultTypesTests
{
    [Fact]
    public void Should_HaveNoErrors_When_PasswordChangeSucceeds()
    {
        var result = PasswordChangeResult.Success();

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_CarryErrors_When_PasswordChangeFails()
    {
        var result = PasswordChangeResult.Failure("a", "b");

        Assert.False(result.Succeeded);
        Assert.Equal(new[] { "a", "b" }, result.Errors);
    }

    [Fact]
    public void Should_ReturnEmptyErrors_When_PasswordChangeFailureGivenNull()
    {
        var result = PasswordChangeResult.Failure(null!);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_CarryUser_When_RegistrationSucceeds()
    {
        var user = new User { Id = 3 };

        var result = RegistrationResult.Success(user);

        Assert.True(result.Succeeded);
        Assert.Same(user, result.CreatedUser);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Should_CarryErrors_When_RegistrationFailsWithParams()
    {
        var result = RegistrationResult.Failure("x");

        Assert.False(result.Succeeded);
        Assert.Null(result.CreatedUser);
        Assert.Equal(new[] { "x" }, result.Errors);
    }

    [Fact]
    public void Should_CarryErrors_When_RegistrationFailsWithEnumerable()
    {
        var result = RegistrationResult.Failure(new List<string> { "x", "y" });

        Assert.Equal(2, result.Errors.Count());
    }

    [Fact]
    public void Should_ReturnEmptyErrors_When_RegistrationFailureGivenNull()
    {
        Assert.Empty(RegistrationResult.Failure((IEnumerable<string>)null!).Errors);
        Assert.Empty(RegistrationResult.Failure((string[])null!).Errors);
    }
}
