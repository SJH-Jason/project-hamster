using HamsterRace.Domain;
using HamsterRace.Simulation;
using Xunit;

namespace HamsterRace.Simulation.Tests;

public class DeterminismTests
{
    private static RaceConfig MakeConfig(ulong seed) => new()
    {
        RaceId = "TEST",
        Seed = seed,
        DistanceMeters = 100,
        Hamsters = new List<Hamster>
        {
            new() { Id = "a", Name = "A", BaseSpeed = 5.0, SpeedJitter = 0.4 },
            new() { Id = "b", Name = "B", BaseSpeed = 4.8, SpeedJitter = 0.6 },
            new() { Id = "c", Name = "C", BaseSpeed = 5.1, SpeedJitter = 0.2 },
            new() { Id = "d", Name = "D", BaseSpeed = 4.9, SpeedJitter = 0.5 },
        },
    };

    [Fact]
    public void SameSeed_ProducesIdenticalResult()
    {
        var r1 = new RaceSimulator().Run(MakeConfig(12345));
        var r2 = new RaceSimulator().Run(MakeConfig(12345));

        for (int i = 0; i < r1.Rankings.Count; i++)
        {
            Assert.Equal(r1.Rankings[i].HamsterId, r2.Rankings[i].HamsterId);
            Assert.Equal(r1.Rankings[i].Rank, r2.Rankings[i].Rank);
            Assert.Equal(r1.Rankings[i].FinishTimeSeconds, r2.Rankings[i].FinishTimeSeconds, 9);
        }
    }

    [Fact]
    public void DifferentSeed_CanChangeOutcome()
    {
        // 不同 seed 至少要能產生不同的完賽時間序列（否則亂數沒作用）。
        var r1 = new RaceSimulator().Run(MakeConfig(1));
        var r2 = new RaceSimulator().Run(MakeConfig(999999));

        var t1 = r1.Rankings.Select(x => x.FinishTimeSeconds).ToArray();
        var t2 = r2.Rankings.Select(x => x.FinishTimeSeconds).ToArray();
        Assert.False(t1.SequenceEqual(t2), "不同 seed 應該能產生不同結果");
    }

    [Fact]
    public void AllHamsters_FinishStandardRace()
    {
        var result = new RaceSimulator().Run(MakeConfig(42));
        Assert.All(result.Rankings, r => Assert.True(r.Finished, $"{r.Name} 應該要完賽"));
        Assert.Equal(4, result.Rankings.Count);
        // 名次 1..4 齊全
        Assert.Equal(new[] { 1, 2, 3, 4 }, result.Rankings.Select(r => r.Rank).OrderBy(x => x));
    }
}
