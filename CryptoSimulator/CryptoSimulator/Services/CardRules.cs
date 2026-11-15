using CryptoSimulator.Entities;

namespace CryptoSimulator.Services
{
    public static class CardRules
    {
        private const decimal MinAmount = 10.00m;
        private const decimal MaxAmount = 100000.00m;
        private const int MaxExpiryYearsAhead = 20;

        public static bool IsValidNumber(string digits)
        {
            if (digits is null || digits.Length is < 15 or > 16 || !digits.All(char.IsAsciiDigit))
            {
                return false;
            }

            // Luhn: from the right, double every second digit; subtract 9 if the result is above 9.
            var sum = 0;
            for (var i = 0; i < digits.Length; i++)
            {
                var digit = digits[digits.Length - 1 - i] - '0';
                if (i % 2 == 1)
                {
                    digit *= 2;
                    if (digit > 9)
                    {
                        digit -= 9;
                    }
                }

                sum += digit;
            }

            return sum % 10 == 0;
        }

        public static CardNetwork DetectNetwork(string digits)
        {
            if (digits.Length >= 2)
            {
                if (digits[0] == '3' && digits[1] is '4' or '7')
                {
                    return CardNetwork.Amex;
                }

                if (digits[0] == '5' && digits[1] is >= '1' and <= '5')
                {
                    return CardNetwork.Mastercard;
                }
            }

            return CardNetwork.Visa;
        }

        public static bool IsExpiryValid(int month, int year, DateTime utcNow)
        {
            if (month is < 1 or > 12)
            {
                return false;
            }

            var current = utcNow.Year * 12 + utcNow.Month;
            var expiry = year * 12 + month;
            return expiry >= current && year <= utcNow.Year + MaxExpiryYearsAhead;
        }

        public static bool IsValidCvv(string cvv)
        {
            return cvv is not null && cvv.Length is 3 or 4 && cvv.All(char.IsAsciiDigit);
        }

        public static bool IsValidAmount(decimal amount)
        {
            return amount is >= MinAmount and <= MaxAmount && decimal.Round(amount, 2) == amount;
        }
    }
}
