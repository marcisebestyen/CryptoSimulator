using CryptoSimulator.Configuration;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;

namespace CryptoSimulator.Services
{
    public interface IAdminSeeder
    {
        Task SeedAsync();
    }

    public class AdminSeeder : IAdminSeeder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AdminSettings _settings;

        public AdminSeeder(IUnitOfWork unitOfWork, AdminSettings settings)
        {
            _unitOfWork = unitOfWork;
            _settings = settings;
        }

        public async Task SeedAsync()
        {
            var adminExists = (await _unitOfWork.UserRepository.GetAsync(u => u.Role == Role.Admin)).Any();
            if (adminExists)
            {
                return;
            }

            var email = _settings.Email.ToLower();
            var username = _settings.Username.ToLower();
            var taken = (await _unitOfWork.UserRepository.GetAsync(
                u => u.Email.ToLower() == email || u.Username.ToLower() == username)).Any();
            if (taken)
            {
                throw new InvalidOperationException(
                    "Cannot create the admin: ADMIN_EMAIL or ADMIN_USERNAME is already in use by another user.");
            }

            // No recovery code on purpose: forgot-password never works for the admin.
            var admin = new User
            {
                Role = Role.Admin,
                Username = _settings.Username,
                FirstName = "Admin",
                LastName = "Admin",
                Email = _settings.Email,
                Password = UserCredentials.HashPassword(_settings.Password),
                RecoveryCodeHash = null,
            };
            await _unitOfWork.UserRepository.InsertAsync(admin);
            await _unitOfWork.WalletRepository.InsertAsync(new Wallet { Balance = 0m, User = admin });
            await _unitOfWork.SaveAsync();
        }
    }
}
