namespace CryptoSimulator.Services
{
    public interface IRecoveryCodeService
    {
        string Generate();
        string Hash(string code);
    }
}
