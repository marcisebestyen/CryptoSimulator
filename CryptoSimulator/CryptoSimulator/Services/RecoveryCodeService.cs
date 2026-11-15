using System.Security.Cryptography;
using System.Text;
using CryptoSimulator.Configuration;

namespace CryptoSimulator.Services
{
    public class RecoveryCodeService : IRecoveryCodeService
    {
        private const int CodeLength = 8;
        private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string Symbols = "!@#$%^&*?-_+=";
        private const string AllChars = Letters + Digits + Symbols;

        private readonly byte[] _key;

        public RecoveryCodeService(RecoveryCodeSettings settings)
        {
            _key = Encoding.UTF8.GetBytes(settings.Key);
        }

        public string Generate()
        {
            var chars = new char[CodeLength];
            chars[0] = Pick(Letters);
            chars[1] = Pick(Digits);
            chars[2] = Pick(Symbols);
            for (var i = 3; i < CodeLength; i++)
            {
                chars[i] = Pick(AllChars);
            }

            // Shuffle so the guaranteed characters are not always first.
            for (var i = CodeLength - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }

        public string Hash(string code) =>
            Convert.ToBase64String(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(code)));

        private static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
    }
}
