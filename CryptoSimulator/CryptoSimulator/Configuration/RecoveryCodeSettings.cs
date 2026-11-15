namespace CryptoSimulator.Configuration
{
    public class RecoveryCodeSettings
    {
        public const int MinKeyLength = 32;

        public string Key { get; set; } = string.Empty;

        // Config (RecoveryCode__Key) wins over .env, same as JwtSettings.
        public static RecoveryCodeSettings Load(IConfiguration configuration, Dictionary<string, string> env)
        {
            var key = configuration["RecoveryCode:Key"];
            if (string.IsNullOrEmpty(key))
            {
                key = env.GetValueOrDefault("RECOVERY_CODE_KEY", string.Empty);
            }

            if (key.Length < MinKeyLength)
            {
                throw new InvalidOperationException(
                    $"RECOVERY_CODE_KEY is missing or shorter than {MinKeyLength} characters. Set it in .env (see .env.example) or set RecoveryCode__Key.");
            }

            return new RecoveryCodeSettings { Key = key };
        }
    }
}
