using HamsterRace.Domain;
using HamsterRace.Simulation;
using Xunit;

namespace HamsterRace.Simulation.Tests;

public class SkillTests
{
    private static readonly IReadOnlyDictionary<string, Skill> Catalog = new Dictionary<string, Skill>
    {
        ["burst"] = new()
        {
            Id = "burst", Name = "短衝", Trigger = SkillTrigger.Periodic,
            SpeedBonus = 1.5, DurationSeconds = 1.0, CooldownSeconds = 4.0, HpCost = 2,
        },
        ["spurt"] = new()
        {
            Id = "spurt", Name = "最後衝刺", Trigger = SkillTrigger.RemainingM, TriggerValue = 20,
            SpeedBonus = 3.0, DurationSeconds = 2.0, CooldownSeconds = 99, HpCost = 5,
        },
    };

    private static RaceConfig Config(ulong seed, Hamster h) => new()
    {
        RaceId = "T", Seed = seed, DistanceMeters = 100,
        Skills = Catalog,
        Hamsters = new List<Hamster> { h },
    };

    private static Hamster Hammy(double intimacy, params string[] skills) => new()
    {
        Id = "x", Name = "X", BaseSpeed = 5.0, SpeedJitter = 0.0,
        MaxHp = 500, MaxMp = 500, Intimacy = intimacy, Skills = skills,
    };

    [Fact]
    public void PerfectIntimacy_SkillFiresAndRepeatsOnCooldown()
    {
        // 100m ≈ 20s,短衝冷卻 4s → 約可放 5 次左右;至少 >1 次(可重複)。
        var result = new RaceSimulator().Run(Config(1, Hammy(1.0, "burst")));
        Assert.True(result.Rankings[0].SkillsFired >= 3, $"應重複發動,實際 {result.Rankings[0].SkillsFired}");
        Assert.Equal(0, result.Rankings[0].SkillsFailed);
    }

    [Fact]
    public void ZeroIntimacy_SkillAlwaysFails()
    {
        var result = new RaceSimulator().Run(Config(1, Hammy(0.0, "burst")));
        Assert.Equal(0, result.Rankings[0].SkillsFired);
        Assert.True(result.Rankings[0].SkillsFailed >= 1);
    }

    [Fact]
    public void RemainingMSkill_FiresExactlyOnce_NearFinish()
    {
        var result = new RaceSimulator().Run(Config(1, Hammy(1.0, "spurt")));
        Assert.Equal(1, result.Rankings[0].SkillsFired); // 只在最後 20m 觸發一次
    }

    [Fact]
    public void SkillFiring_IsDeterministic()
    {
        var r1 = new RaceSimulator().Run(Config(42, Hammy(0.7, "burst")));
        var r2 = new RaceSimulator().Run(Config(42, Hammy(0.7, "burst")));
        Assert.Equal(r1.Rankings[0].SkillsFired, r2.Rankings[0].SkillsFired);
        Assert.Equal(r1.Rankings[0].SkillsFailed, r2.Rankings[0].SkillsFailed);
    }
}
