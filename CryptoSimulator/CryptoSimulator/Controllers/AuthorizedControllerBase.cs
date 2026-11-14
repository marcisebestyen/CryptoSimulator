using System.Security.Claims;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoSimulator.Controllers
{
    // Shared ownership checks. Services stay unaware of auth.
    public abstract class AuthorizedControllerBase : ControllerBase
    {
        private readonly IUnitOfWork _authUnitOfWork;

        protected AuthorizedControllerBase(IUnitOfWork unitOfWork)
        {
            _authUnitOfWork = unitOfWork;
        }

        protected int? CurrentUserId =>
            int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        protected bool IsAdmin => User?.IsInRole(Role.Admin.ToString()) ?? false;

        protected bool IsOwner(int ownerId) => CurrentUserId == ownerId;

        protected bool IsOwnerOrAdmin(int ownerId) => IsOwner(ownerId) || IsAdmin;

        // Returns null when access is allowed. A missing user stays 404, other users' data is 403.
        protected Task<ActionResult?> DenyUnlessOwnerOrAdminAsync(int userId) =>
            DenyUnlessAsync(IsOwnerOrAdmin(userId), userId);

        protected Task<ActionResult?> DenyUnlessOwnerAsync(int userId) =>
            DenyUnlessAsync(IsOwner(userId), userId);

        private async Task<ActionResult?> DenyUnlessAsync(bool allowed, int userId)
        {
            if (allowed)
            {
                return null;
            }

            var user = await _authUnitOfWork.UserRepository.GetByIdAsync(new object[] { userId }, null, null);
            return user == null ? NotFound($"User with ID {userId} not found.") : Forbid();
        }
    }
}
