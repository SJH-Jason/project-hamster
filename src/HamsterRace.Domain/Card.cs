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

    /// <summary>每秒體力節省（保留體力卡:觸發後降低該段 stamina 消耗）。</summary>
    public double StaminaSavePerSec { get; init; } = 0.0;
}
