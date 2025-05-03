namespace CryptoSimulator.DTOs
{
    public class WalletGetDto
    {
        public int Id { get; set; }
        public decimal Balance { get; set; }
        public int UserId { get; set; }
    }

    public class WalletPutDto
    {
        public decimal Balance { get; set; }
    }

    public class WalletPostDto
    {
        public decimal Balance { get; set; }
        public int UserId { get; set; }
    }

    public class UserWalletDetailsDto
    {
        public int UserId { get; set; }
        public decimal FiatBalance { get; set; }
        public List<OwnedCryptoDto> OwnedCryptos { get; set; } = new List<OwnedCryptoDto>();
    }
}
