using System.Text.Json;
using HamsterRace.Domain;

namespace HamsterRace.Simulation;

/// <summary>從 JSON 載入鼠鼠與卡牌（規格 §15 data-driven:數值不寫死在程式）。</summary>
public static class DataLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    // ---- 鼠鼠 ----
    private sealed class HamsterFile { public List<HamsterDto> Hamsters { get; set; } = new(); }

    private sealed class HamsterDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double BaseSpeed { get; set; }
        public double SpeedJitter { get; set; }
        public int MaxHp { get; set; } = 100;
        public int MaxMp { get; set; } = 100;
        public double StaminaFactor { get; set; } = 1.0;
        public List<string> Deck { get; set; } = new();
        public Dictionary<string, double> TerrainAffinity { get; set; } = new();
        public Dictionary<string, double> WeatherAffinity { get; set; } = new();
        public Dictionary<string, double> WindAffinity { get; set; } = new();
        public Dictionary<string, double> TimeAffinity { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public double Intimacy { get; set; } = 0.9;
    }

    public static IReadOnlyList<Hamster> LoadHamsters(string path)
    {
        var file = JsonSerializer.Deserialize<HamsterFile>(File.ReadAllText(path), Options)
                   ?? throw new InvalidDataException($"無法解析鼠鼠資料檔:{path}");

        return file.Hamsters.Select(d => new Hamster
        {
            Id = d.Id,
            Name = d.Name,
            BaseSpeed = d.BaseSpeed,
            SpeedJitter = d.SpeedJitter,
            MaxHp = d.MaxHp,
            MaxMp = d.MaxMp,
            StaminaFactor = d.StaminaFactor,
            Deck = d.Deck,
            TerrainAffinity = d.TerrainAffinity,
            WeatherAffinity = d.WeatherAffinity,
            WindAffinity = d.WindAffinity,
            TimeAffinity = d.TimeAffinity,
            Skills = d.Skills,
            Intimacy = d.Intimacy,
        }).ToList();
    }

    // ---- 卡牌 ----
    private sealed class CardFile { public List<CardDto> Cards { get; set; } = new(); }

    private sealed class CardDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public RacePhase Phase { get; set; }
        public double SpeedBonus { get; set; }
        public double DurationSeconds { get; set; } = 2.0;
        public bool WholeRace { get; set; }
        public int HpCost { get; set; }
        public int MpCost { get; set; }
        public int HpRecover { get; set; }
        public int MpRecover { get; set; }
        public double DrainMultiplier { get; set; } = 1.0;
        public string? ConditionType { get; set; }
        public string? ConditionValue { get; set; }
    }

    public static IReadOnlyDictionary<string, Card> LoadCards(string path)
    {
        var file = JsonSerializer.Deserialize<CardFile>(File.ReadAllText(path), Options)
                   ?? throw new InvalidDataException($"無法解析卡牌資料檔:{path}");

        return file.Cards.ToDictionary(d => d.Id, d => new Card
        {
            Id = d.Id,
            Name = d.Name,
            Phase = d.Phase,
            SpeedBonus = d.SpeedBonus,
            DurationSeconds = d.DurationSeconds,
            WholeRace = d.WholeRace,
            HpCost = d.HpCost,
            MpCost = d.MpCost,
            HpRecover = d.HpRecover,
            MpRecover = d.MpRecover,
            DrainMultiplier = d.DrainMultiplier,
            ConditionType = d.ConditionType,
            ConditionValue = d.ConditionValue,
        });
    }

    // ---- 平衡參數 ----
    private sealed class RulesDto
    {
        public double ResourceFloor { get; set; } = 0.5;
        public double HpDrainPer10m { get; set; } = 5.0;
        public double MpDrainPer10m { get; set; } = 5.0;
        public double FatigueStartMult { get; set; } = 0.5;
        public double FatigueEndMult { get; set; } = 1.5;
        public double StartPhaseEnd { get; set; } = 0.15;
        public double EarlyPhaseEnd { get; set; } = 0.40;
        public double MidPhaseEnd { get; set; } = 0.70;
    }

    public static RaceRules LoadRules(string path)
    {
        var d = JsonSerializer.Deserialize<RulesDto>(File.ReadAllText(path), Options)
                ?? new RulesDto();
        return new RaceRules
        {
            ResourceFloor = d.ResourceFloor,
            HpDrainPer10m = d.HpDrainPer10m,
            MpDrainPer10m = d.MpDrainPer10m,
            FatigueStartMult = d.FatigueStartMult,
            FatigueEndMult = d.FatigueEndMult,
            StartPhaseEnd = d.StartPhaseEnd,
            EarlyPhaseEnd = d.EarlyPhaseEnd,
            MidPhaseEnd = d.MidPhaseEnd,
        };
    }

    // ---- 賽道環境 ----
    private sealed class RaceFile { public List<EnvDto> Races { get; set; } = new(); }

    private sealed class EnvDto
    {
        public string Id { get; set; } = "";
        public string Terrain { get; set; } = "track";
        public string TimeOfDay { get; set; } = "day";
        public Dictionary<string, double> WeatherForecast { get; set; } = new() { ["normal"] = 1.0 };
        public Dictionary<string, double> WindForecast { get; set; } = new() { ["none"] = 1.0 };
    }

    // ---- 技能 ----
    private sealed class SkillFile { public List<SkillDto> Skills { get; set; } = new(); }

    private sealed class SkillDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public SkillTrigger Trigger { get; set; }
        public double TriggerValue { get; set; }
        public double SpeedBonus { get; set; }
        public double DurationSeconds { get; set; } = 2.0;
        public double CooldownSeconds { get; set; } = 5.0;
        public int HpCost { get; set; }
        public int MpCost { get; set; }
        public int HpRecover { get; set; }
        public int MpRecover { get; set; }
    }

    public static IReadOnlyDictionary<string, Skill> LoadSkills(string path)
    {
        var file = JsonSerializer.Deserialize<SkillFile>(File.ReadAllText(path), Options)
                   ?? throw new InvalidDataException($"無法解析技能資料檔:{path}");
        return file.Skills.ToDictionary(d => d.Id, d => new Skill
        {
            Id = d.Id,
            Name = d.Name,
            Trigger = d.Trigger,
            TriggerValue = d.TriggerValue,
            SpeedBonus = d.SpeedBonus,
            DurationSeconds = d.DurationSeconds,
            CooldownSeconds = d.CooldownSeconds,
            HpCost = d.HpCost,
            MpCost = d.MpCost,
            HpRecover = d.HpRecover,
            MpRecover = d.MpRecover,
        });
    }

    /// <summary>載入賽事環境;raceId 為 null 時取第一筆。</summary>
    public static RaceEnvironment LoadEnvironment(string path, string? raceId = null)
    {
        var file = JsonSerializer.Deserialize<RaceFile>(File.ReadAllText(path), Options)
                   ?? throw new InvalidDataException($"無法解析賽事資料檔:{path}");
        var dto = (raceId is null ? file.Races.FirstOrDefault()
                                  : file.Races.FirstOrDefault(r => r.Id == raceId))
                  ?? throw new InvalidDataException($"賽事找不到:{raceId}");

        return new RaceEnvironment
        {
            Terrain = dto.Terrain,
            TimeOfDay = dto.TimeOfDay,
            WeatherForecast = dto.WeatherForecast,
            WindForecast = dto.WindForecast,
        };
    }
}
