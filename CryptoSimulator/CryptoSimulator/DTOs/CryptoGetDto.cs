namespace CryptoSimulator.DTOs
{
    public class CryptoGetDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
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
}
