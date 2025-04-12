using CryptoSimulator.DTOs;
using CryptoSimulator.Entities;
using CryptoSimulator.Repositories;

namespace CryptoSimulator.Services
{
    public interface IUserService
    {
        Task<bool> LoginAsync(string email, string password);
        Task<RegistrationResult> RegisterAsync(string username, string email, string password);
    }

    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            bool isPasswordValid = VerifyPasswordPlaceholder(user.Password, password);

            return isPasswordValid;
        }

        public async Task<RegistrationResult> RegisterAsync(string username, string email, string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add("Username is required.");
            }
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
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
                passwordHash = HashedPasswordPlaceholder(password);
                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    return RegistrationResult.Failure("An error occurred while hashing the password.");
                }
            }
            catch (Exception ex)
            {
                return RegistrationResult.Failure("An error occurred while hashing the password.");
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                Password = passwordHash,
                Role = Role.User,
                FirstName = string.Empty,
                LastName = string.Empty
            };
            await _unitOfWork.UserRepository.InsertAsync(newUser);

            var newWallet = new Wallet
            {
                Balance = 1000.0m,
                User = newUser
            };
            await _unitOfWork.WalletRepository.InsertAsync(newWallet);

            return RegistrationResult.Success(newUser);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(email);
                return address.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string HashedPasswordPlaceholder(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            return "hashed_" + password;
        }

        private bool VerifyPasswordPlaceholder(string storedPlaceholder, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(storedPlaceholder) || string.IsNullOrWhiteSpace(providedPassword))
            {
                return false;
            }

            string expectedPlaceholderHash = "hashed_" + providedPassword;

            return storedPlaceholder == expectedPlaceholderHash;
        }
    }
}
