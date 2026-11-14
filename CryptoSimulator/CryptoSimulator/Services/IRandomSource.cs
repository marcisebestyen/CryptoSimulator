namespace CryptoSimulator.Services
{
    public interface IRandomSource
    {
        double NextDouble();
    }

    public class SystemRandomSource : IRandomSource
    {
        public double NextDouble() => Random.Shared.NextDouble();
    }
}
