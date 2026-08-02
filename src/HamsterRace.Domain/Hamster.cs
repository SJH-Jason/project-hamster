namespace HamsterRace.Domain;

/// <summary>
/// 一隻鼠鼠的定義。Stage 1 只用到基礎跑速與 HP/MP 上限；
/// 適性（地形/天氣/風向/時段）與技能欄位先預留，Stage 3、4 才啟用，
/// 避免之後擴充要改動核心（Roadmap §19 預留接口原則）。
/// </summary>
public sealed record Hamster
{
    /// <summary>識別用 id（資料檔 key）。</summary>
    public required string Id { get; init; }

    /// <summary>顯示名稱。</summary>
    public required string Name { get; init; }

    /// <summary>基礎跑速（m/s）。實際速度會再乘上各種倍率。</summary>
    public required double BaseSpeed { get; init; }

    /// <summary>每 tick 的自然速度波動幅度（±，m/s）；0 = 完全穩定。</summary>
    public double SpeedJitter { get; init; } = 0.0;

    public int MaxHp { get; init; } = 100;
    public int MaxMp { get; init; } = 100;

    // --- 以下 Stage 3+ 才生效，先給預設，維持可攜 ---

    /// <summary>地形適性倍率，例如 { "grass": 1.1 }；查無 key 視為 1.0。</summary>
    public IReadOnlyDictionary<string, double> TerrainAffinity { get; init; }
        = new Dictionary<string, double>();

    /// <summary>天氣適性倍率。</summary>
    public IReadOnlyDictionary<string, double> WeatherAffinity { get; init; }
        = new Dictionary<string, double>();

    /// <summary>風向適性倍率。</summary>
    public IReadOnlyDictionary<string, double> WindAffinity { get; init; }
        = new Dictionary<string, double>();

    /// <summary>時段適性倍率。</summary>
    public IReadOnlyDictionary<string, double> TimeAffinity { get; init; }
        = new Dictionary<string, double>();
}
