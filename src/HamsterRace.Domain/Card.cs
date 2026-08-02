namespace HamsterRace.Domain;

/// <summary>比賽階段（依跑完進度切分）。地形/天氣/風向卡屬 Stage 3,先不做。</summary>
public enum RacePhase
{
    Start,   // 起跑 0–15%
    Early,   // 初期 0–40%
    Mid,     // 中期 40–70%
    Sprint,  // 衝刺 70–100%
    Whole,   // 全程
}

/// <summary>
/// 一張卡牌（Stage 2:階段速度卡 + 資源消耗 + 補給）。
/// 卡牌在「進入自己的階段」時觸發一次,付出 HP/MP,給予速度加成或回復。
/// </summary>
public sealed record Card
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>此卡在哪個階段觸發。</summary>
    public required RacePhase Phase { get; init; }

    /// <summary>觸發後的速度加成（m/s，加法）。</summary>
    public double SpeedBonus { get; init; } = 0.0;

    /// <summary>加成持續秒數;WholeRace=true 時忽略,整場有效。</summary>
    public double DurationSeconds { get; init; } = 2.0;

    /// <summary>true = 一觸發就整場持續（配速類）。</summary>
    public bool WholeRace { get; init; } = false;

    public int HpCost { get; init; } = 0;
    public int MpCost { get; init; } = 0;

    /// <summary>觸發時回復的 HP（補給卡）。</summary>
    public int HpRecover { get; init; } = 0;

    /// <summary>觸發時回復的 MP。</summary>
    public int MpRecover { get; init; } = 0;

    /// <summary>
    /// 消耗倍率（保留體力卡:持續時間內降低 HP/MP 基本消耗）。
    /// 1.0=不變;0.4=消耗砍到四成。多張同時生效取相乘。
    /// </summary>
    public double DrainMultiplier { get; init; } = 1.0;

    // --- Stage 3:環境條件卡（押天氣/地形/風向）---
    /// <summary>觸發條件類型:null=無條件;"weather"/"wind"/"terrain"。</summary>
    public string? ConditionType { get; init; }

    /// <summary>條件值（如 "rain"）。實際環境不符 → 卡失效（押錯代價）。</summary>
    public string? ConditionValue { get; init; }
}
