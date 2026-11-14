using CryptoSimulator.Services;

namespace CryptoSimulator.Tests.Unit;

public class PriceSimulatorTests
{
    // Plays the given values first, then returns 0.5 (zero noise, no events) forever.
    private sealed class ScriptedRandom(params double[] values) : IRandomSource
    {
        private readonly Queue<double> _queue = new(values);
        public double NextDouble() => _queue.Count > 0 ? _queue.Dequeue() : 0.5;
    }

    private sealed class SeededRandom(int seed) : IRandomSource
    {
        private readonly Random _random = new(seed);
        public double NextDouble() => _random.NextDouble();
    }

    // Draw order for a Calm tick: shock roll, trend roll, then noise.
    // Shock: sign, size, correction ticks, correction drift. Trend: sign, drift, ticks.
    private static readonly double[] ForcedUpShock = [0.0, 0.9, 0.5, 0.5, 0.5];
    private static readonly double[] ForcedUpTrend = [0.5, 0.0, 0.9, 0.5, 0.5];

    private static double Change(decimal from, decimal to) => (double)(to / from) - 1;

    [Fact]
    public void Should_KeepPriceValidAndChangeWithinQuarter_When_ManyTicks()
    {
        var sim = new PriceSimulator(new SeededRandom(42));
        decimal price = 1000m;

        for (var i = 0; i < 5000; i++)
        {
            var next = sim.NextPrice(1, price);

            Assert.True(next >= 0.01m);
            Assert.InRange(Math.Abs(Change(price, next)), 0, 0.251);
            price = next;
        }
    }

    [Fact]
    public void Should_MoveMuchMoreThanOnePercent_When_ManyTicks()
    {
        var sim = new PriceSimulator(new SeededRandom(7));
        decimal price = 1000m;
        double total = 0, max = 0;

        for (var i = 0; i < 5000; i++)
        {
            var next = sim.NextPrice(1, price);
            var change = Math.Abs(Change(price, next));
            total += change;
            max = Math.Max(max, change);
            price = next;
        }

        Assert.True(total / 5000 > 0.015, $"mean abs change was {total / 5000}");
        Assert.True(max > 0.10, $"max abs change was {max}");
    }

    [Fact]
    public void Should_JumpTenToTwentyPercent_When_ShockForced()
    {
        var sim = new PriceSimulator(new ScriptedRandom(ForcedUpShock));

        var next = sim.NextPrice(1, 100m);

        Assert.Equal(115m, next);
    }

    [Fact]
    public void Should_DriftOppositeToShock_When_InCorrection()
    {
        var sim = new PriceSimulator(new ScriptedRandom(ForcedUpShock));
        var price = sim.NextPrice(1, 100m);
        var afterShock = price;

        for (var i = 0; i < 5; i++)
        {
            var next = sim.NextPrice(1, price);
            Assert.True(next < price, $"tick {i} went {price} -> {next}");
            price = next;
        }

        Assert.True(price < afterShock * 0.9m);
    }

    [Fact]
    public void Should_MovePriceInTrendDirection_When_TrendForced()
    {
        var sim = new PriceSimulator(new ScriptedRandom(ForcedUpTrend));
        decimal price = 100m;

        for (var i = 0; i < 6; i++)
        {
            var next = sim.NextPrice(1, price);
            Assert.True(next > price, $"tick {i} went {price} -> {next}");
            price = next;
        }

        Assert.True(price > 110m);
    }

    [Fact]
    public void Should_KeepStateSeparate_When_DifferentCryptoIds()
    {
        var sim = new PriceSimulator(new ScriptedRandom(ForcedUpTrend));
        var trending = sim.NextPrice(1, 100m);

        var other = sim.NextPrice(2, 50m);
        var trendingAgain = sim.NextPrice(1, trending);

        Assert.Equal(50m, other);
        Assert.True(trendingAgain > trending);
    }

    [Fact]
    public void Should_StayValid_When_PriceIsMinimum()
    {
        var sim = new PriceSimulator(new SeededRandom(3));
        decimal price = 0.01m;

        for (var i = 0; i < 2000; i++)
        {
            price = sim.NextPrice(1, price);
            Assert.True(price >= 0.01m);
        }
    }

    [Fact]
    public void Should_ReturnMinimum_When_CurrentPriceIsZero()
    {
        var sim = new PriceSimulator(new ScriptedRandom());

        Assert.Equal(0.01m, sim.NextPrice(1, 0m));
    }
}
