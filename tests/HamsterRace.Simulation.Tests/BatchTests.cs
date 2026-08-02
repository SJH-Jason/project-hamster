using HamsterRace.Domain;
using HamsterRace.Simulation;
using Xunit;

namespace HamsterRace.Simulation.Tests;

public class BatchTests
{
    private static RaceConfig Base() => new()
    {
        RaceId = "B", Seed = 1, DistanceMeters = 100,
        Environment = new RaceEnvironment
        {
            WeatherForecast = new Dictionary<string, double> { ["normal"] = 0.5, ["rain"] = 0.5 },
            WindForecast = new Dictionary<string, double> { ["none"] = 1.0 },
        },
        Hamsters = new List<Hamster>
        {
            new() { Id = "a", Name = "A", BaseSpeed = 5.0, SpeedJitter = 0.3 },
            new() { Id = "b", Name = "B", BaseSpeed = 4.8, SpeedJitter = 0.5 },
        },
    };

    [Fact]
    public void Batch_TotalsAreConsistent()
    {
        var rep = new BatchSimulator().Run(Base(), 500);
        // 每場都有一位冠軍 → 總冠軍數 = 場數
        int totalWins = rep.ByHamster.Values.Sum(s => s.Wins);
        Assert.Equal(500, totalWins);
        // 每隻鼠都跑滿場數
        Assert.All(rep.ByHamster.Values, s => Assert.Equal(500, s.Races));
    }

    [Fact]
    public void Batch_IsDeterministic()
    {
        var r1 = new BatchSimulator().Run(Base(), 300);
        var r2 = new BatchSimulator().Run(Base(), 300);
        foreach (var id in r1.ByHamster.Keys)
            Assert.Equal(r1.ByHamster[id].Wins, r2.ByHamster[id].Wins);
    }
}
