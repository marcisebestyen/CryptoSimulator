namespace CryptoSimulator.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserGetDto User { get; set; } = null!;
    }
}
