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

    /// <summary>
    /// 體力/精神消耗係數（乘在「每 10 公尺基本消耗」上）。
    /// 1.0 = 標準;&gt;1 較耗（如黑糖）;&lt;1 較省（如花生）。
    /// 鼠鼠的體力差異＝此係數 × HP/MP 上限,長跑時差距被放大。
    /// </summary>
    public double StaminaFactor { get; init; } = 1.0;

    /// <summary>這隻鼠帶的牌組（card id）。Stage 2 每隻 6 張;Stage 6 才做選牌 UI。</summary>
    public IReadOnlyList<string> Deck { get; init; } = new List<string>();

    /// <summary>這隻鼠的主動技能（skill id,每隻 2 個）。Stage 4。</summary>
    public IReadOnlyList<string> Skills { get; init; } = new List<string>();

    /// <summary>親密度（技能成功率,0–1）。Stage 4 先固定,不做養成。</summary>
    public double Intimacy { get; init; } = 0.9;

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
