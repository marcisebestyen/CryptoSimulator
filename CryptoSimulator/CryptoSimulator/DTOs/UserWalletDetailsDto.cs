namespace CryptoSimulator.DTOs
{
    public class UserWalletDetailsDto
    {
        public int UserId { get; set; }
        public decimal FiatBalance { get; set; }
        public List<OwnedCryptoDto> OwnedCryptos { get; set; } = new List<OwnedCryptoDto>();
    }
}
