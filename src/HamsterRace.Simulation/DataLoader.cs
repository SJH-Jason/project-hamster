using System.Text.Json;
using HamsterRace.Domain;

namespace HamsterRace.Simulation;

/// <summary>從 JSON 資料檔載入鼠鼠（規格 §15 data-driven:數值不寫死在程式）。</summary>
public static class DataLoader
{
    private sealed class HamsterFile
    {
        public List<HamsterDto> Hamsters { get; set; } = new();
    }

    private sealed class HamsterDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double BaseSpeed { get; set; }
        public double SpeedJitter { get; set; }
        public int MaxHp { get; set; } = 100;
        public int MaxMp { get; set; } = 100;
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyList<Hamster> LoadHamsters(string path)
    {
        string json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<HamsterFile>(json, Options)
                   ?? throw new InvalidDataException($"無法解析鼠鼠資料檔:{path}");

        return file.Hamsters.Select(d => new Hamster
        {
            Id = d.Id,
            Name = d.Name,
            BaseSpeed = d.BaseSpeed,
            SpeedJitter = d.SpeedJitter,
            MaxHp = d.MaxHp,
            MaxMp = d.MaxMp,
        }).ToList();
    }
}
