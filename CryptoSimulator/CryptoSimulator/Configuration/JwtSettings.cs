namespace CryptoSimulator.Configuration
{
    public class JwtSettings
    {
        public const int MinKeyLength = 32;

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;

        // Config (Jwt__Key etc.) wins over .env, same as the connection string.
        public static JwtSettings Load(IConfiguration configuration, Dictionary<string, string> env)
        {
            var settings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

            settings.Key = Pick(settings.Key, env, "JWT_KEY");
            settings.Issuer = Pick(settings.Issuer, env, "JWT_ISSUER");
            settings.Audience = Pick(settings.Audience, env, "JWT_AUDIENCE");

            if (configuration["Jwt:ExpiryMinutes"] == null && env.TryGetValue("JWT_EXPIRY_MINUTES", out var minutes))
            {
                settings.ExpiryMinutes = int.Parse(minutes);
            }

            if (settings.Key.Length < MinKeyLength)
            {
                throw new InvalidOperationException(
                    $"JWT_KEY is missing or shorter than {MinKeyLength} characters. Set it in .env (see .env.example) or set Jwt__Key.");
            }
            if (settings.Issuer.Length == 0 || settings.Audience.Length == 0)
            {
                throw new InvalidOperationException(
                    "JWT_ISSUER and JWT_AUDIENCE are required. Set them in .env (see .env.example) or set Jwt__Issuer / Jwt__Audience.");
            }
            if (settings.ExpiryMinutes <= 0)
            {
                throw new InvalidOperationException("JWT expiry must be greater than 0 minutes.");
            }

            return settings;
        }

        private static string Pick(string configured, Dictionary<string, string> env, string envKey) =>
            configured.Length > 0 ? configured : env.GetValueOrDefault(envKey, string.Empty);
    }
}
