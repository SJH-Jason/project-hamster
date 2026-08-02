using HamsterRace.Domain;
using HamsterRace.Simulation;
using Xunit;

namespace HamsterRace.Simulation.Tests;

public class EnvironmentTests
{
    private static readonly IReadOnlyDictionary<string, Card> Catalog = new Dictionary<string, Card>
    {
        ["rain_run"] = new()
        {
            Id = "rain_run", Name = "雨天適應", Phase = RacePhase.Whole, WholeRace = true,
            SpeedBonus = 1.0, ConditionType = "weather", ConditionValue = "rain",
        },
    };

    private static RaceConfig Config(ulong seed, RaceEnvironment env, Hamster h) => new()
    {
        RaceId = "T", Seed = seed, DistanceMeters = 100,
        Cards = Catalog, Environment = env,
        Hamsters = new List<Hamster> { h },
    };

    private static RaceEnvironment Fixed(string weather) => new()
    {
        Terrain = "grass", TimeOfDay = "day",
        WeatherForecast = new Dictionary<string, double> { [weather] = 1.0 },
        WindForecast = new Dictionary<string, double> { ["none"] = 1.0 },
    };

    [Fact]
    public void WeatherAffinity_ChangesSpeed()
    {
        // 同一隻愛雨的鼠,雨天應比晴天快。
        var rainLover = new Hamster
        {
            Id = "m", Name = "M", BaseSpeed = 5.0, SpeedJitter = 0.0,
            WeatherAffinity = new Dictionary<string, double> { ["rain"] = 1.2, ["sunny"] = 0.9 },
        };
        var inRain = new RaceSimulator().Run(Config(1, Fixed("rain"), rainLover));
        var inSun = new RaceSimulator().Run(Config(1, Fixed("sunny"), rainLover));
        Assert.True(inRain.Rankings[0].FinishTimeSeconds < inSun.Rankings[0].FinishTimeSeconds,
            "愛雨的鼠在雨天應更快");
    }

    [Fact]
    public void ConditionCard_FiresOnMatch_FizzlesOnMismatch()
    {
        var h = new Hamster { Id = "x", Name = "X", BaseSpeed = 5.0, Deck = new[] { "rain_run" } };

        var inRain = new RaceSimulator().Run(Config(1, Fixed("rain"), h));
        Assert.Equal(1, inRain.Rankings[0].CardsFired);
        Assert.Equal(0, inRain.Rankings[0].CardsFizzled);

        var inSun = new RaceSimulator().Run(Config(1, Fixed("sunny"), h));
        Assert.Equal(0, inSun.Rankings[0].CardsFired);
        Assert.Equal(1, inSun.Rankings[0].CardsFizzled); // 押錯啞掉
    }

    [Fact]
    public void ActualWeather_IsDeterministic()
    {
        var env = new RaceEnvironment
        {
            WeatherForecast = new Dictionary<string, double> { ["normal"] = 0.5, ["sunny"] = 0.2, ["rain"] = 0.3 },
            WindForecast = new Dictionary<string, double> { ["none"] = 0.4, ["tail"] = 0.35, ["head"] = 0.25 },
        };
        var h = new Hamster { Id = "x", Name = "X", BaseSpeed = 5.0 };
        var r1 = new RaceSimulator().Run(Config(777, env, h));
        var r2 = new RaceSimulator().Run(Config(777, env, h));
        Assert.Equal(r1.ActualWeather, r2.ActualWeather);
        Assert.Equal(r1.ActualWind, r2.ActualWind);
    }
}
