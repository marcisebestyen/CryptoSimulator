namespace CryptoSimulator.DTOs
{
    public class UserRegisterResponseDto
    {
        public UserGetDto User { get; set; } = null!;
        public string RecoveryCode { get; set; } = string.Empty;
    }
}
