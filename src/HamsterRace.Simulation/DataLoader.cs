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
        public double StaminaDrainPerSec { get; set; }
        public List<string> Deck { get; set; } = new();
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
            StaminaDrainPerSec = d.StaminaDrainPerSec,
            Deck = d.Deck,
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
        public double StaminaSavePerSec { get; set; }
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
            StaminaSavePerSec = d.StaminaSavePerSec,
        });
    }
}
