using System.Collections.Concurrent;

namespace CryptoSimulator.Services
{
    public class PriceSimulator
    {
        private enum Phase { Calm, Trend, Correction }

        private sealed class CoinState
        {
            public required double Anchor { get; init; }
            public Phase Phase { get; set; } = Phase.Calm;
            public int TicksRemaining { get; set; }
            public double Drift { get; set; }
            public double LastChange { get; set; }
        }

        private const decimal MinPrice = 0.01m;
        private const double NoiseSize = 0.05;
        private const double MomentumFactor = 0.3;
        private const double MeanReversionFactor = 0.02;
        private const double MaxChange = 0.25;
        private const double TrendChance = 0.08;
        private const double SpontaneousShockChance = 0.03;
        private const double ShockAfterTrendChance = 0.35;
        private const int TrendMinTicks = 4;
        private const int TrendMaxTicks = 8;
        private const double TrendMinDrift = 0.015;
        private const double TrendMaxDrift = 0.025;
        private const double ShockMinSize = 0.10;
        private const double ShockMaxSize = 0.20;
        private const int CorrectionMinTicks = 3;
        private const int CorrectionMaxTicks = 6;
        private const double CorrectionMinDrift = 0.01;
        private const double CorrectionMaxDrift = 0.03;

        private readonly IRandomSource _random;
        private readonly ConcurrentDictionary<int, CoinState> _states = new();

        public PriceSimulator(IRandomSource random)
        {
            _random = random;
        }

        public decimal NextPrice(int cryptoId, decimal currentPrice)
        {
            var price = Math.Max(MinPrice, currentPrice);
            var state = _states.GetOrAdd(cryptoId, _ => new CoinState { Anchor = (double)price });

            // One coin is updated by one thread at a time; the lock keeps its state consistent.
            lock (state)
            {
                var change = ComputeChange(state, (double)price);
                return Math.Max(MinPrice, Math.Round(price * (1 + (decimal)change), 2));
            }
        }

        private double ComputeChange(CoinState state, double price)
        {
            var eventChange = AdvancePhase(state);
            var isShock = eventChange is not null;

            var baseMove = eventChange ?? ((2 * _random.NextDouble() - 1) * NoiseSize + state.Drift);
            var change = baseMove
                + MomentumFactor * state.LastChange
                - MeanReversionFactor * Math.Log(price / state.Anchor);
            change = Math.Clamp(change, -MaxChange, MaxChange);

            // Reset after a shock so the bounce is not drowned out by momentum.
            state.LastChange = isShock ? 0 : change;
            return change;
        }

        // Moves the phase machine one tick. Returns a shock jump when this tick is a shock, else null.
        private double? AdvancePhase(CoinState state)
        {
            if (state.Phase != Phase.Calm && state.TicksRemaining == 0)
            {
                var endedTrend = state.Phase == Phase.Trend;
                var trendSign = Math.Sign(state.Drift);
                state.Phase = Phase.Calm;
                state.Drift = 0;

                if (endedTrend && _random.NextDouble() < ShockAfterTrendChance)
                {
                    return StartShock(state, -trendSign);
                }
            }

            if (state.Phase == Phase.Calm)
            {
                if (_random.NextDouble() < SpontaneousShockChance)
                {
                    return StartShock(state, RandomSign());
                }

                if (_random.NextDouble() < TrendChance)
                {
                    state.Phase = Phase.Trend;
                    state.Drift = RandomSign() * Between(TrendMinDrift, TrendMaxDrift);
                    state.TicksRemaining = RandomTicks(TrendMinTicks, TrendMaxTicks);
                }
            }

            if (state.Phase != Phase.Calm)
            {
                state.TicksRemaining--;
            }

            return null;
        }

        private double StartShock(CoinState state, int sign)
        {
            var jump = sign * Between(ShockMinSize, ShockMaxSize);
            state.Phase = Phase.Correction;
            state.TicksRemaining = RandomTicks(CorrectionMinTicks, CorrectionMaxTicks);
            state.Drift = -sign * Between(CorrectionMinDrift, CorrectionMaxDrift);
            return jump;
        }

        private int RandomSign() => _random.NextDouble() >= 0.5 ? 1 : -1;

        private double Between(double min, double max) => min + (max - min) * _random.NextDouble();

        private int RandomTicks(int min, int max) => min + (int)(_random.NextDouble() * (max - min + 1));
    }
}
