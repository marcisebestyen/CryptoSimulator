using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;
using System.Security.Cryptography;
using System.Text;
using CryptoSimulator.Results;

namespace CryptoSimulator.Services
{
    public class UserService : IUserService
    {
        private const int MaxRecoveryCodeAttempts = 5;
        private const string InvalidResetMessage = "Invalid email or recovery code.";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecoveryCodeService _recoveryCodeService;

        public UserService(IUnitOfWork unitOfWork, IRecoveryCodeService recoveryCodeService)
        {
            _unitOfWork = unitOfWork;
            _recoveryCodeService = recoveryCodeService;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var userFound = await _unitOfWork.UserRepository.GetAsync(u => u.Email.ToLower() == email.ToLower(), null);

            var user = userFound.FirstOrDefault();
            if (user == null)
            {
                return false;
            }

            bool isPasswordValid = VerifyPassword(user.Password, password);

            return isPasswordValid;
        }

        public async Task<RegistrationResult> RegisterAsync(string username, string email, string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add("Username is required.");
            }
            if (string.IsNullOrWhiteSpace(email) || !UserCredentials.IsValidEmail(email))
            {
                errors.Add("A valid email is required.");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required.");
            }

            if (errors.Any())
            {
                return RegistrationResult.Failure(errors);
            }

            var emailExist = (await _unitOfWork.UserRepository.GetAsync(u => u.Email.ToLower() == email.ToLower())).Any();
            if (emailExist)
            {
                errors.Add("Email or username already in use.");
            }

            var usernameExist = (await _unitOfWork.UserRepository.GetAsync(u => u.Username.ToLower() == username.ToLower())).Any();
            if (usernameExist && !errors.Any())
            {
                errors.Add("Email or username already in use.");
            }

            if (errors.Any())
            {
                return RegistrationResult.Failure(errors);
            }

            string passwordHash;
            try
            {
                passwordHash = UserCredentials.HashPassword(password);
                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    return RegistrationResult.Failure("An error occurred while hashing the password.");
                }
            }
            catch (Exception)
            {
                return RegistrationResult.Failure("An error occurred while hashing the password.");
            }

            var (recoveryCode, recoveryCodeHash) = await GenerateUniqueRecoveryCodeAsync();

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = passwordHash,
                Role = Role.User,
                FirstName = string.Empty,
                LastName = string.Empty,
                RecoveryCodeHash = recoveryCodeHash
            };
            await _unitOfWork.UserRepository.InsertAsync(newUser);

            var newWallet = new Wallet
            {
                Balance = 1000.0m,
                User = newUser
            };
            await _unitOfWork.WalletRepository.InsertAsync(newWallet);

            return RegistrationResult.Success(newUser, recoveryCode);
        }

        public async Task<PasswordChangeResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return PasswordChangeResult.Failure("Current and new passwords are required.");
            }

            if (currentPassword == newPassword)
            {
                return PasswordChangeResult.Failure("New password cannot be the same as the current password.");
            }

            var user = await _unitOfWork.UserRepository.GetByIdAsync(new object[] { userId });
            if (user == null)
            {
                return PasswordChangeResult.Failure("User not found.");
            }

            if (!VerifyPassword(user.Password, currentPassword))
            {
                return PasswordChangeResult.Failure("Incorrect current password.");
            }

            var error = await SetPasswordAndSaveAsync(user, newPassword);
            if (error != null)
            {
                return PasswordChangeResult.Failure(error);
            }

            return PasswordChangeResult.Success();
        }

        public async Task<PasswordResetResult> ResetPasswordAsync(string email, string recoveryCode, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return PasswordResetResult.Failure("New password is required.");
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(recoveryCode))
            {
                return PasswordResetResult.Failure(InvalidResetMessage);
            }

            var userFound = await _unitOfWork.UserRepository.GetAsync(u => u.Email.ToLower() == email.ToLower(), null);
            var user = userFound.FirstOrDefault();
            if (user?.RecoveryCodeHash == null || !HashesMatch(user.RecoveryCodeHash, _recoveryCodeService.Hash(recoveryCode)))
            {
                return PasswordResetResult.Failure(InvalidResetMessage);
            }

            var (newCode, newCodeHash) = await GenerateUniqueRecoveryCodeAsync();
            user.RecoveryCodeHash = newCodeHash;

            var error = await SetPasswordAndSaveAsync(user, newPassword);
            if (error != null)
            {
                return PasswordResetResult.Failure(error);
            }

            return PasswordResetResult.Success(newCode);
        }

        private async Task<string?> SetPasswordAndSaveAsync(User user, string newPassword)
        {
            string newPasswordHash;
            try
            {
                newPasswordHash = UserCredentials.HashPassword(newPassword);
                if (string.IsNullOrWhiteSpace(newPasswordHash))
                {
                    return "An error occurred while hashing the new password.";
                }
            }
            catch (Exception)
            {
                return "An error occurred while hashing the new password.";
            }

            user.Password = newPasswordHash;

            await _unitOfWork.UserRepository.UpdateAsync(user);
            try
            {
                await _unitOfWork.SaveAsync();
            }
            catch (Exception)
            {
                return "An error occurred while saving the new password.";
            }

            return null;
        }

        private static bool HashesMatch(string stored, string computed)
        {
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stored), Encoding.UTF8.GetBytes(computed));
        }

        private async Task<(string Code, string Hash)> GenerateUniqueRecoveryCodeAsync()
        {
            for (var attempt = 0; attempt < MaxRecoveryCodeAttempts; attempt++)
            {
                var code = _recoveryCodeService.Generate();
                var hash = _recoveryCodeService.Hash(code);
                var clash = (await _unitOfWork.UserRepository.GetAsync(u => u.RecoveryCodeHash == hash)).Any();
                if (!clash)
                {
                    return (code, hash);
                }
            }

            throw new InvalidOperationException("Could not generate a unique recovery code.");
        }

        private static bool VerifyPassword(string stored, string provided)
        {
            if (string.IsNullOrWhiteSpace(stored) || string.IsNullOrWhiteSpace(provided))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(provided, stored);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }
    }
}
