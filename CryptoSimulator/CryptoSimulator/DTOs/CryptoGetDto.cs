namespace CryptoSimulator.DTOs
{
    public class CryptoGetDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CryptoGetWithCurrentValueDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal CurrentExchangeRate { get; set; }   
    }

    public class CryptoPutDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CryptoPostDto
    {
        public string Name { get; set; }
    }

    public class  BuyCryptoDto
    {
        public int UserId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; } 
    }

    public class SellCryptoDto
    {
        public int UserId { get; set; }
        public int CryptoId { get; set; }
        public decimal Amount { get; set; }
    }
}
