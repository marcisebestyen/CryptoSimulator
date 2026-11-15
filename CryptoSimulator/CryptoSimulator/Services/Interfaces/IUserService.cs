using CryptoSimulator.Results;

namespace CryptoSimulator.Services
{
    public interface IUserService
    {
        Task<bool> LoginAsync(string email, string password);
        Task<RegistrationResult> RegisterAsync(string username, string email, string password);
        Task<PasswordChangeResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<PasswordResetResult> ResetPasswordAsync(string email, string recoveryCode, string newPassword);
    }
}
