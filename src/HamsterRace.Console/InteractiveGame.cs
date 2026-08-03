using HamsterRace.Domain;
using HamsterRace.Simulation;

namespace HamsterRace.Console;

/// <summary>
/// Stage 6:簡易互動介面（規格 §16）。
/// 玩家當教練:看情報 → 選鼠 → 手動配 6 張牌 → 開賽 → 看戰報 → 重玩。
/// 玩家指導的那隻鼠用自組牌組,其餘三隻用預設牌組。
/// </summary>
public sealed class InteractiveGame
{
    private readonly IReadOnlyList<Hamster> _hamsters;
    private readonly IReadOnlyDictionary<string, Card> _cards;
    private readonly IReadOnlyDictionary<string, Skill> _skills;
    private readonly RaceEnvironment _env;
    private readonly RaceRules _rules;
    private readonly double _distance;

    public InteractiveGame(IReadOnlyList<Hamster> hamsters, IReadOnlyDictionary<string, Card> cards,
                           IReadOnlyDictionary<string, Skill> skills, RaceEnvironment env,
                           RaceRules rules, double distance)
    {
        _hamsters = hamsters; _cards = cards; _skills = skills;
        _env = env; _rules = rules; _distance = distance;
    }

    public void Run()
    {
        var C = System.Console.Out;
        C.WriteLine();
        C.WriteLine("╔══════════════════════════════════════╗");
        C.WriteLine("║        🐹 Project Hamster  MVP        ║");
        C.WriteLine("║        鼠鼠競賽 · 教練模式           ║");
        C.WriteLine("╚══════════════════════════════════════╝");

        int? chosenIdx = null;
        List<string>? chosenDeck = null;

        while (true)
        {
            ShowIntel();

            // 選鼠（沿用上一輪的可直接 Enter）
            if (chosenIdx is null)
            {
                chosenIdx = PickHamster();
                if (chosenIdx is null) { C.WriteLine("掰掰 🎐"); return; }
                chosenDeck = null;
            }
            var me = _hamsters[chosenIdx.Value];

            // 配牌（沒配過或選擇重配時）
            if (chosenDeck is null)
            {
                chosenDeck = BuildDeck(me);
                if (chosenDeck is null) { C.WriteLine("掰掰 🎐"); return; }
            }

            RunRace(chosenIdx.Value, chosenDeck);

            // 重玩選單
            C.WriteLine();
            C.Write("再來一場? [Enter]=同鼠同牌再抽天氣  r=重配牌組  n=換鼠  q=離開 > ");
            var cmd = (System.Console.ReadLine() ?? "q").Trim().ToLowerInvariant();
            if (cmd == "q") { C.WriteLine("辛苦了,掰掰 🎐"); return; }
            if (cmd == "n") { chosenIdx = null; chosenDeck = null; }
            else if (cmd == "r") { chosenDeck = null; }
            // 其餘（含 Enter）= 同設定再跑一場（換 seed → 換天氣）
        }
    }

    private void ShowIntel()
    {
        var C = System.Console.Out;
        C.WriteLine();
        C.WriteLine($"── 賽事情報 ──  {Name(_env.Terrain)}賽道 · {TimeName(_env.TimeOfDay)} · {_distance:0} 公尺");
        C.WriteLine($"  天氣預報: {Forecast(_env.WeatherForecast, WeatherName)}");
        C.WriteLine($"  風向預報: {Forecast(_env.WindForecast, WindName)}");
        C.WriteLine("  （實際天氣/風向鎖牌後才抽 → 這就是「押天氣」）");
    }

    private int? PickHamster()
    {
        var C = System.Console.Out;
        C.WriteLine();
        C.WriteLine("選你要指導的鼠鼠:");
        for (int i = 0; i < _hamsters.Count; i++)
        {
            var h = _hamsters[i];
            C.WriteLine($"  {i + 1}) {h.Name,-4} 速{h.BaseSpeed:0.0} HP{h.MaxHp}/MP{h.MaxMp} " +
                        $"技[{string.Join("、", h.Skills.Select(SkillName))}]  {Affinities(h)}");
        }
        C.Write("> ");
        var line = System.Console.ReadLine();
        if (line is null || line.Trim().ToLowerInvariant() == "q") return null;
        if (int.TryParse(line.Trim(), out var n) && n >= 1 && n <= _hamsters.Count) return n - 1;
        C.WriteLine("（沒聽懂，預設選第 1 隻）");
        return 0;
    }

    private List<string>? BuildDeck(Hamster me)
    {
        var C = System.Console.Out;
        var pool = _cards.Values.ToList();
        C.WriteLine();
        C.WriteLine($"為【{me.Name}】配 6 張牌（牌庫 {pool.Count} 張）：");
        for (int i = 0; i < pool.Count; i++)
            C.WriteLine($"  {i + 1,2}) {DescribeCard(pool[i])}");
        C.WriteLine($"  預設牌組: {string.Join("、", me.Deck.Select(CardName))}");
        C.Write("輸入 6 個編號(空格分隔)，或直接 Enter 用預設 > ");

        var line = System.Console.ReadLine();
        if (line is null) return null;
        line = line.Trim();
        if (line.ToLowerInvariant() == "q") return null;
        if (line.Length == 0) return me.Deck.ToList();

        var picks = line.Split(new[] { ' ', ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s, out var v) ? v : -1)
                        .Where(v => v >= 1 && v <= pool.Count)
                        .Distinct().ToList();
        if (picks.Count == 0) { C.WriteLine("（沒選到有效的牌，用預設）"); return me.Deck.ToList(); }
        if (picks.Count > 6) { C.WriteLine("（超過 6 張，取前 6）"); picks = picks.Take(6).ToList(); }
        var deck = picks.Select(p => pool[p - 1].Id).ToList();
        C.WriteLine($"你的牌組: {string.Join("、", deck.Select(CardName))}"
                    + (deck.Count < 6 ? $"（只有 {deck.Count} 張，也能跑）" : ""));
        return deck;
    }

    private void RunRace(int meIdx, List<string> deck)
    {
        // 換掉玩家那隻鼠的牌組，其餘不動
        var roster = _hamsters.Select((h, i) => i == meIdx ? h with { Deck = deck } : h).ToList();
        ulong seed = unchecked((ulong)(Random.Shared.NextInt64() ^ DateTime.Now.Ticks));

        var config = new RaceConfig
        {
            RaceId = "PLAY",
            Seed = seed,
            DistanceMeters = _distance,
            Hamsters = roster,
            Cards = _cards,
            Skills = _skills,
            Environment = _env,
            Rules = _rules,
        };

        var result = new RaceSimulator().Run(config);
        System.Console.WriteLine();
        System.Console.WriteLine(TextReport.Render(result));

        var mine = result.Rankings.First(r => r.HamsterId == _hamsters[meIdx].Id);
        System.Console.WriteLine(mine.Rank == 1
            ? $"🏆 你指導的 {mine.Name} 拿下第 1 名！配牌成功！"
            : $"你指導的 {mine.Name} 這場第 {mine.Rank} 名——換個配牌或賭賭天氣再來？");
    }

    // ---- 顯示小工具 ----
    private string DescribeCard(Card c)
    {
        string when = c.Phase switch
        {
            RacePhase.Start => "起跑", RacePhase.Early => "初期", RacePhase.Mid => "中期",
            RacePhase.Sprint => "衝刺", RacePhase.Whole => "全程", _ => "?"
        };
        var parts = new List<string> { $"[{when}]", c.Name };
        if (c.SpeedBonus != 0) parts.Add($"+{c.SpeedBonus:0.0}m/s");
        if (c.HpRecover > 0 || c.MpRecover > 0) parts.Add($"回HP{c.HpRecover}/MP{c.MpRecover}");
        if (c.DrainMultiplier != 1.0) parts.Add($"消耗×{c.DrainMultiplier:0.0}");
        if (c.HpCost > 0 || c.MpCost > 0) parts.Add($"耗HP{c.HpCost}/MP{c.MpCost}");
        if (c.ConditionType is not null) parts.Add($"⚠押{CondName(c)}");
        return string.Join(" ", parts);
    }

    private string CondName(Card c) => c.ConditionType switch
    {
        "weather" => WeatherName(c.ConditionValue ?? ""),
        "wind" => WindName(c.ConditionValue ?? ""),
        "terrain" => Name(c.ConditionValue ?? ""),
        _ => c.ConditionValue ?? ""
    };

    private string Affinities(Hamster h)
    {
        var likes = new List<string>();
        foreach (var (k, v) in h.WeatherAffinity) if (v > 1.0) likes.Add(WeatherName(k));
        foreach (var (k, v) in h.WindAffinity) if (v > 1.0) likes.Add(WindName(k));
        foreach (var (k, v) in h.TerrainAffinity) if (v > 1.0) likes.Add(Name(k));
        return likes.Count > 0 ? $"擅長:{string.Join("/", likes)}" : "";
    }

    private string CardName(string id) => _cards.TryGetValue(id, out var c) ? c.Name : id;
    private string SkillName(string id) => _skills.TryGetValue(id, out var s) ? s.Name : id;

    private static string Forecast(IReadOnlyDictionary<string, double> f, Func<string, string> name)
        => string.Join(" / ", f.OrderByDescending(x => x.Value).Select(x => $"{name(x.Key)} {x.Value * 100:0}%"));

    private static string Name(string k) => k switch
    { "track" => "運動場", "grass" => "草地", "asphalt" => "柏油", _ => k };
    private static string WeatherName(string k) => k switch
    { "normal" => "一般", "sunny" => "大晴天", "rain" => "大雨", _ => k };
    private static string WindName(string k) => k switch
    { "none" => "無風", "tail" => "順風", "head" => "逆風", _ => k };
    private static string TimeName(string k) => k switch
    { "morning" => "晨間", "day" => "白天", "evening" => "傍晚", "night" => "夜間", _ => k };
}
