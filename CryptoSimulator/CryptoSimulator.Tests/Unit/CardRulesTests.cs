using System.Globalization;
using CryptoSimulator.Entities;
using CryptoSimulator.Services;

namespace CryptoSimulator.Tests.Unit;

public class CardRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("4532710938428896", true)]
    [InlineData("4532710938428897", false)]
    [InlineData("4111111111111111", true)]
    [InlineData("371449635398431", true)]
    [InlineData("45327109384288", false)]
    [InlineData("45327109384288961", false)]
    [InlineData("453271093842889a", false)]
    [InlineData("", false)]
    public void Should_ValidateNumber_When_LengthDigitsAndLuhnChecked(string digits, bool expected)
    {
        Assert.Equal(expected, CardRules.IsValidNumber(digits));
    }

    [Theory]
    [InlineData("371449635398431", CardNetwork.Amex)]
    [InlineData("340000000000009", CardNetwork.Amex)]
    [InlineData("5105105105105100", CardNetwork.Mastercard)]
    [InlineData("5500000000000004", CardNetwork.Mastercard)]
    [InlineData("4111111111111111", CardNetwork.Visa)]
    [InlineData("5600000000000000", CardNetwork.Visa)]
    public void Should_DetectNetwork_When_PrefixMatches(string digits, CardNetwork expected)
    {
        Assert.Equal(expected, CardRules.DetectNetwork(digits));
    }

    [Theory]
    [InlineData(11, 2026, true)]
    [InlineData(12, 2026, true)]
    [InlineData(10, 2026, false)]
    [InlineData(13, 2026, false)]
    [InlineData(0, 2026, false)]
    [InlineData(11, 2046, true)]
    [InlineData(11, 2047, false)]
    [InlineData(12, 2025, false)]
    public void Should_ValidateExpiry_When_CheckedAgainstUtcNow(int month, int year, bool expected)
    {
        Assert.Equal(expected, CardRules.IsExpiryValid(month, year, Now));
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("1234", true)]
    [InlineData("12", false)]
    [InlineData("12345", false)]
    [InlineData("12a", false)]
    public void Should_ValidateCvv_When_LengthAndDigitsChecked(string cvv, bool expected)
    {
        Assert.Equal(expected, CardRules.IsValidCvv(cvv));
    }

    [Theory]
    [InlineData("10", true)]
    [InlineData("9.99", false)]
    [InlineData("100000", true)]
    [InlineData("100000.01", false)]
    [InlineData("10.001", false)]
    [InlineData("50.25", true)]
    public void Should_ValidateAmount_When_RangeAndScaleChecked(string amount, bool expected)
    {
        var value = decimal.Parse(amount, CultureInfo.InvariantCulture);
        Assert.Equal(expected, CardRules.IsValidAmount(value));
    }
}
