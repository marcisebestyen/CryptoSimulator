using CryptoSimulator.Services;

namespace CryptoSimulator.Configuration
{
    public class AdminSettings
    {
        // Registration only needs a non-empty password; the admin gets a stricter floor.
        public const int MinPasswordLength = 8;

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // Config (Admin__Username, ...) wins over .env, same as JwtSettings.
        public static AdminSettings Load(IConfiguration configuration, Dictionary<string, string> env)
        {
            var settings = new AdminSettings
            {
                Username = Read(configuration, env, "Admin:Username", "ADMIN_USERNAME"),
                Email = Read(configuration, env, "Admin:Email", "ADMIN_EMAIL"),
                Password = Read(configuration, env, "Admin:Password", "ADMIN_PASSWORD"),
            };

            if (!UserCredentials.IsValidEmail(settings.Email))
            {
                throw new InvalidOperationException("ADMIN_EMAIL is not a valid email address.");
            }

            if (settings.Password.Length < MinPasswordLength)
            {
                throw new InvalidOperationException(
                    $"ADMIN_PASSWORD must be at least {MinPasswordLength} characters.");
            }

            return settings;
        }

        private static string Read(IConfiguration configuration, Dictionary<string, string> env, string configKey, string envKey)
        {
            var value = configuration[configKey];
            if (string.IsNullOrWhiteSpace(value))
            {
                value = env.GetValueOrDefault(envKey, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{envKey} is missing. Set it in .env (see .env.example) or set {configKey.Replace(":", "__")}.");
            }

            return value;
        }
    }
}
