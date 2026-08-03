using HamsterRace.Domain;
using HamsterRace.Simulation;

namespace HamsterRace.Web;

/// <summary>載入資料（前端經 HttpClient 抓 wwwroot/data/*.json）並用 C# 引擎跑比賽。</summary>
public class GameData
{
    private readonly HttpClient _http;
    public GameData(HttpClient http) => _http = http;

    public IReadOnlyList<Hamster> Hamsters { get; private set; } = new List<Hamster>();
    public IReadOnlyDictionary<string, Card> Cards { get; private set; } = new Dictionary<string, Card>();
    public IReadOnlyDictionary<string, Skill> Skills { get; private set; } = new Dictionary<string, Skill>();
    public RaceEnvironment Env { get; private set; } = new();
    public RaceRules Rules { get; private set; } = new();
    public bool Loaded { get; private set; }

    public async Task EnsureLoadedAsync()
    {
        if (Loaded) return;
        Hamsters = DataLoader.ParseHamsters(await _http.GetStringAsync("data/hamsters.json"));
        Cards = DataLoader.ParseCards(await _http.GetStringAsync("data/cards.json"));
        Skills = DataLoader.ParseSkills(await _http.GetStringAsync("data/skills.json"));
        Env = DataLoader.ParseEnvironment(await _http.GetStringAsync("data/races.json"));
        Rules = DataLoader.ParseRules(await _http.GetStringAsync("data/rules.json"));
        Loaded = true;
    }

    /// <summary>玩家指導 meIndex 那隻鼠用 deck，其餘用預設；回傳含 2D 重播的結果。</summary>
    public RaceResult Simulate(int meIndex, List<string> deck, ulong seed)
    {
        var roster = Hamsters.Select((h, i) => i == meIndex ? h with { Deck = deck } : h).ToList();
        var config = new RaceConfig
        {
            RaceId = "WEB",
            Seed = seed,
            DistanceMeters = 100,
            Hamsters = roster,
            Cards = Cards,
            Skills = Skills,
            Environment = Env,
            Rules = Rules,
        };
        return new RaceSimulator().Run(config, recordReplay: true);
    }
}
