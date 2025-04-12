using CryptoSimulator.Entities;

namespace CryptoSimulator.DTOs
{
    public class RegistrationResult
    {
        public bool Succeeded { get; set; }
        public User? CreatedUser { get; set; }
        public IEnumerable<string> Errors { get; private set; } = Enumerable.Empty<string>();

        private RegistrationResult() { }

        public static RegistrationResult Success(User user)
        {
            return new RegistrationResult
            {
                Succeeded = true,
                CreatedUser = user
            };
        }

        public static RegistrationResult Failure(params string[] errors)
        {
            return new RegistrationResult
            {
                Succeeded = false,
                Errors = errors ?? Enumerable.Empty<string>()
            };
        }

        public static RegistrationResult Failure(IEnumerable<string> errors)
        {
            return new RegistrationResult
            {
                Succeeded = false,
                Errors = errors ?? Enumerable.Empty<string>()
            };
        }
    }
}
