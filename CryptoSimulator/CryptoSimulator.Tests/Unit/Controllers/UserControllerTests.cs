using AutoMapper;
using CryptoSimulator.Controllers;
using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using CryptoSimulator.Results;
using CryptoSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CryptoSimulator.Tests.Unit.Controllers;

public class UserControllerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRepository<User> _users = Substitute.For<IRepository<User>>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly UserController _sut;

    public UserControllerTests()
    {
        _uow.UserRepository.Returns(_users);
        _sut = new UserController(_uow, _mapper, _userService, _tokens).SignedInAs();
    }

    private void ArrangeUser(User? user) =>
        _users.GetByIdAsync(Arg.Any<object[]>(), Arg.Any<string[]?>(), Arg.Any<string[]?>()).Returns(user);

    [Fact]
    public async Task Should_ReturnOkWithDto_When_UserExists()
    {
        var user = new User { Id = 1 };
        var dto = new UserGetDto { Id = 1 };
        ArrangeUser(user);
        _mapper.Map<UserGetDto>(user).Returns(dto);

        var result = await _sut.GetUser(1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GetUserAndUserMissing()
    {
        ArrangeUser(null);

        var result = await _sut.GetUser(5);

        var nf = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User with ID 5 not found.", nf.Value);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_LoginFails()
    {
        _userService.LoginAsync("a@b.c", "pw").Returns(false);

        var result = await _sut.Login(new UserLoginDto { Email = "a@b.c", Password = "pw" });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_LoginOkButUserNotFound()
    {
        _userService.LoginAsync("a@b.c", "pw").Returns(true);
        _users.GetAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<string[]?>())
            .Returns(Array.Empty<User>());

        var result = await _sut.Login(new UserLoginDto { Email = "a@b.c", Password = "pw" });

        var un = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("An error occurred after login", un.Value);
    }

    [Fact]
    public async Task Should_ReturnTokenAndUser_When_LoginSucceeds()
    {
        var user = new User { Id = 2, Email = "a@b.c" };
        var dto = new UserGetDto { Id = 2 };
        _userService.LoginAsync("a@b.c", "pw").Returns(true);
        _users.GetAsync(Arg.Any<System.Linq.Expressions.Expression<Func<User, bool>>>(), Arg.Any<string[]?>())
            .Returns(new[] { user });
        _tokens.CreateToken(user).Returns("jwt");
        _mapper.Map<UserGetDto>(user).Returns(dto);

        var result = await _sut.Login(new UserLoginDto { Email = "a@b.c", Password = "pw" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.Equal("jwt", body.Token);
        Assert.Same(dto, body.User);
    }

    private static UserRegisterDto RegisterDto() =>
        new() { Username = "u", Email = "e@e.e", Password = "p" };

    [Fact]
    public async Task Should_ReturnBadRequest_When_RegistrationFails()
    {
        _userService.RegisterAsync("u", "e@e.e", "p").Returns(RegistrationResult.Failure("bad"));

        var result = await _sut.Register(RegisterDto());

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_Return500_When_RegisterSaveThrowsDbUpdateException()
    {
        _userService.RegisterAsync("u", "e@e.e", "p").Returns(RegistrationResult.Success(new User()));
        _uow.SaveAsync().ThrowsAsync(new DbUpdateException());

        var result = await _sut.Register(RegisterDto());

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, obj.StatusCode);
        Assert.Equal("A database error occurred during the registration.", obj.Value);
    }

    [Fact]
    public async Task Should_Return500_When_RegisterSaveThrowsUnexpectedException()
    {
        _userService.RegisterAsync("u", "e@e.e", "p").Returns(RegistrationResult.Success(new User()));
        _uow.SaveAsync().ThrowsAsync(new InvalidOperationException());

        var result = await _sut.Register(RegisterDto());

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, obj.StatusCode);
        Assert.Equal("An unexpected error occurred during the registration.", obj.Value);
    }

    [Fact]
    public async Task Should_Return500_When_RegisterSucceededWithoutCreatedUser()
    {
        var registration = RegistrationResult.Success(new User());
        registration.CreatedUser = null;
        _userService.RegisterAsync("u", "e@e.e", "p").Returns(registration);

        var result = await _sut.Register(RegisterDto());

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, obj.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnCreatedAtGetUser_When_RegisterSucceeds()
    {
        var user = new User { Id = 9 };
        var dto = new UserGetDto { Id = 9 };
        _userService.RegisterAsync("u", "e@e.e", "p").Returns(RegistrationResult.Success(user));
        _mapper.Map<UserGetDto>(user).Returns(dto);

        var result = await _sut.Register(RegisterDto());

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(UserController.GetUser), created.ActionName);
        Assert.Equal(9, created.RouteValues!["userId"]);
        Assert.Same(dto, created.Value);
    }

    private static UserChangePasswordDto PwDto() =>
        new() { CurrentPassword = "old", NewPassword = "new", ConfirmedNewPassword = "new" };

    [Fact]
    public async Task Should_ReturnNoContent_When_PasswordChanged()
    {
        _userService.ChangePasswordAsync(1, "old", "new").Returns(PasswordChangeResult.Success());

        var result = await _sut.ChangePassword(1, PwDto());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ChangePasswordUserNotFound()
    {
        _userService.ChangePasswordAsync(1, "old", "new").Returns(PasswordChangeResult.Failure("User not found."));

        var result = await _sut.ChangePassword(1, PwDto());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Should_ReturnBadRequestWithModelErrors_When_ChangePasswordFails()
    {
        _userService.ChangePasswordAsync(1, "old", "new").Returns(PasswordChangeResult.Failure("Wrong password."));

        var result = await _sut.ChangePassword(1, PwDto());

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<SerializableError>(bad.Value);
        Assert.Contains("Wrong password.", (string[])errors[string.Empty]);
    }

    [Fact]
    public async Task Should_ReturnOkAndSave_When_UpdateUserSucceeds()
    {
        var user = new User { Id = 1 };
        var dto = new UserGetDto { Id = 1 };
        ArrangeUser(user);
        _mapper.Map<UserGetDto>(user).Returns(dto);

        var result = await _sut.UpdateUser(1, new UserPutDto());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
        await _users.Received(1).UpdateAsync(user);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdateUserMissing()
    {
        ArrangeUser(null);

        var result = await _sut.UpdateUser(1, new UserPutDto());

        Assert.IsType<NotFoundObjectResult>(result.Result);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNoContentAndDelete_When_DeleteUserExists()
    {
        ArrangeUser(new User { Id = 1 });

        var result = await _sut.DeleteUser(1);

        Assert.IsType<NoContentResult>(result);
        await _users.Received(1).DeleteAsync(1);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_DeleteUserMissing()
    {
        ArrangeUser(null);

        var result = await _sut.DeleteUser(1);

        Assert.IsType<NotFoundObjectResult>(result);
        await _users.DidNotReceive().DeleteAsync(Arg.Any<object[]>());
    }

    [Fact]
    public async Task Should_ReturnForbid_When_GetUserOfAnotherUser()
    {
        ArrangeUser(new User { Id = 2 });

        var result = await _sut.GetUser(2);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnForbid_When_NoCallerIdentity()
    {
        ArrangeUser(new User { Id = 1 });
        var anonymous = new UserController(_uow, _mapper, _userService, _tokens);

        var result = await anonymous.GetUser(1);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnOk_When_AdminGetsAnotherUser()
    {
        var user = new User { Id = 2 };
        ArrangeUser(user);
        _mapper.Map<UserGetDto>(user).Returns(new UserGetDto { Id = 2 });

        var result = await _sut.SignedInAs(99, "Admin").GetUser(2);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Should_ReturnForbidAndNotSave_When_UpdateAnotherUser()
    {
        ArrangeUser(new User { Id = 2 });

        var result = await _sut.UpdateUser(2, new UserPutDto());

        Assert.IsType<ForbidResult>(result.Result);
        await _uow.DidNotReceive().SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnOkAndSave_When_AdminUpdatesAnotherUser()
    {
        var user = new User { Id = 2 };
        ArrangeUser(user);
        _mapper.Map<UserGetDto>(user).Returns(new UserGetDto { Id = 2 });

        var result = await _sut.SignedInAs(99, "Admin").UpdateUser(2, new UserPutDto());

        Assert.IsType<OkObjectResult>(result.Result);
        await _uow.Received(1).SaveAsync();
    }

    [Fact]
    public async Task Should_ReturnForbidAndNotDelete_When_DeleteAnotherUser()
    {
        ArrangeUser(new User { Id = 2 });

        var result = await _sut.DeleteUser(2);

        Assert.IsType<ForbidResult>(result);
        await _users.DidNotReceive().DeleteAsync(Arg.Any<object[]>());
    }

    [Fact]
    public async Task Should_ReturnNoContent_When_AdminDeletesAnotherUser()
    {
        ArrangeUser(new User { Id = 2 });

        var result = await _sut.SignedInAs(99, "Admin").DeleteUser(2);

        Assert.IsType<NoContentResult>(result);
    }

    [Theory]
    [InlineData(2, "User")]
    [InlineData(99, "Admin")]
    public async Task Should_ReturnForbidAndNotChange_When_ChangePasswordNotOwner(int callerId, string role)
    {
        ArrangeUser(new User { Id = 2 });

        var result = await _sut.SignedInAs(callerId == 2 ? 1 : callerId, role).ChangePassword(2, PwDto());

        Assert.IsType<ForbidResult>(result);
        await _userService.DidNotReceive().ChangePasswordAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
