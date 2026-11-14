using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Tests.Unit.Controllers;

// Gives a directly constructed controller a signed-in caller.
internal static class ControllerUser
{
    public const int OwnerId = 1;

    public static T SignedInAs<T>(this T controller, int userId = OwnerId, string role = "User") where T : ControllerBase
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
        }, "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
        return controller;
    }
}
