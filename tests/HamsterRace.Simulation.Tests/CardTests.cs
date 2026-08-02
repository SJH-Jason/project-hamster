using HamsterRace.Domain;
using HamsterRace.Simulation;
using Xunit;

namespace HamsterRace.Simulation.Tests;

public class CardTests
{
    private static readonly IReadOnlyDictionary<string, Card> Catalog = new Dictionary<string, Card>
    {
        ["sprint"] = new() { Id = "sprint", Name = "衝刺", Phase = RacePhase.Sprint, SpeedBonus = 3.0, DurationSeconds = 3.0, HpCost = 15 },
    };

    private static RaceConfig Config(ulong seed, IReadOnlyList<string> deck) => new()
    {
        RaceId = "T",
        Seed = seed,
        DistanceMeters = 100,
        Cards = Catalog,
        Hamsters = new List<Hamster>
        {
            new() { Id = "x", Name = "X", BaseSpeed = 5.0, SpeedJitter = 0.0, MaxHp = 100, MaxMp = 100, Deck = deck },
        },
    };

    [Fact]
    public void DeckWithSprintCard_IsFasterThanEmptyDeck()
    {
        var withCard = new RaceSimulator().Run(Config(1, new[] { "sprint" }));
        var noCard = new RaceSimulator().Run(Config(1, Array.Empty<string>()));

        double tCard = withCard.Rankings[0].FinishTimeSeconds;
        double tNone = noCard.Rankings[0].FinishTimeSeconds;

        Assert.True(tCard < tNone, $"帶衝刺卡應更快:帶卡 {tCard:0.0}s vs 空牌 {tNone:0.0}s");
        Assert.Equal(1, withCard.Rankings[0].CardsFired);
    }

    [Fact]
    public void UnaffordableCard_Fizzles()
    {
        // HP 只有 5,付不起 15 HP 的衝刺卡 → 啞掉。
        var cfg = Config(1, new[] { "sprint" }) with
        {
            Hamsters = new List<Hamster>
            {
                new() { Id = "x", Name = "X", BaseSpeed = 5.0, MaxHp = 5, MaxMp = 100, Deck = new[] { "sprint" } },
            },
        };
        var result = new RaceSimulator().Run(cfg);
        Assert.Equal(0, result.Rankings[0].CardsFired);
        Assert.Equal(1, result.Rankings[0].CardsFizzled);
    }

    [Fact]
    public void StaminaDrain_ReducesRemainingHp()
    {
        var cfg = Config(1, Array.Empty<string>()) with
        {
            Hamsters = new List<Hamster>
            {
                new() { Id = "x", Name = "X", BaseSpeed = 5.0, MaxHp = 100, StaminaDrainPerSec = 2.0, Deck = Array.Empty<string>() },
            },
        };
        var result = new RaceSimulator().Run(cfg);
        Assert.True(result.Rankings[0].RemainingHp < 100, "跑完體力應下降");
    }
}
