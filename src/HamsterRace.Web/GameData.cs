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
    public IReadOnlyList<(string Id, RaceEnvironment Env)> Races { get; private set; } = new List<(string, RaceEnvironment)>();
    public RaceRules Rules { get; private set; } = new();
    public bool Loaded { get; private set; }

    public async Task EnsureLoadedAsync()
    {
        if (Loaded) return;
        Hamsters = DataLoader.ParseHamsters(await _http.GetStringAsync("data/hamsters.json"));
        Cards = DataLoader.ParseCards(await _http.GetStringAsync("data/cards.json"));
        Skills = DataLoader.ParseSkills(await _http.GetStringAsync("data/skills.json"));
        Races = DataLoader.ParseRaces(await _http.GetStringAsync("data/races.json"));
        Rules = DataLoader.ParseRules(await _http.GetStringAsync("data/rules.json"));
        Loaded = true;
    }

    /// <summary>玩家指導 meIndex 那隻鼠:自組 6 張牌、選 1 個技能並設發動時機、選賽道。</summary>
    public RaceResult Simulate(int meIndex, string trackId, List<string> deck,
                               string skillId, SkillTrigger trigger, double triggerValue, ulong seed)
    {
        var env = Races.FirstOrDefault(r => r.Id == trackId).Env
                  ?? Races.First().Env;

        // 玩家選的技能:複製一份、套上玩家設的發動時機（不動共用型錄）
        var baseSkill = Skills[skillId];
        var playerSkill = baseSkill with { Id = "player_skill", Trigger = trigger, TriggerValue = triggerValue };
        var skills = new Dictionary<string, Skill>(Skills) { ["player_skill"] = playerSkill };

        var roster = Hamsters.Select((h, i) => i == meIndex
            ? h with { Deck = deck, Skills = new[] { "player_skill" } }
            : h).ToList();

        var config = new RaceConfig
        {
            RaceId = "WEB",
            Seed = seed,
            DistanceMeters = 100,
            Hamsters = roster,
            Cards = Cards,
            Skills = skills,
            Environment = env,
            Rules = Rules,
        };
        return new RaceSimulator().Run(config, recordReplay: true);
    }
}
