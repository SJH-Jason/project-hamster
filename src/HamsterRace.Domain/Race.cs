namespace HamsterRace.Domain;

/// <summary>平衡參數（規格 §7.3 資源折扣、階段界線）。集中可調,不寫死在引擎。</summary>
public sealed record RaceRules
{
    /// <summary>低於「上限×此比例」視為「資源不足」→ 速度打 75 折。</summary>
    public double LowResourceFraction { get; init; } = 0.30;

    // 階段界線（跑完進度比例）
    public double StartPhaseEnd { get; init; } = 0.15;
    public double EarlyPhaseEnd { get; init; } = 0.40;
    public double MidPhaseEnd { get; init; } = 0.70;
    // 之後（>MidPhaseEnd）即衝刺段
}

/// <summary>一場比賽的設定。</summary>
public sealed record RaceConfig
{
    public required string RaceId { get; init; }
    public required ulong Seed { get; init; }

    /// <summary>比賽距離（公尺）。MVP 起手 100m。</summary>
    public double DistanceMeters { get; init; } = 100.0;

    /// <summary>每個 tick 的時間長度（秒）。規格 §10.1 建議 0.1。</summary>
    public double TickSeconds { get; init; } = 0.1;

    /// <summary>安全上限,避免異常設定造成無限迴圈（秒）。</summary>
    public double MaxRaceSeconds { get; init; } = 600.0;

    public required IReadOnlyList<Hamster> Hamsters { get; init; }

    /// <summary>卡牌型錄（id → Card）。Stage 2 起用。</summary>
    public IReadOnlyDictionary<string, Card> Cards { get; init; }
        = new Dictionary<string, Card>();

    public RaceRules Rules { get; init; } = new();
}

/// <summary>比賽過程中的一則事件（給文字戰報用）。</summary>
public sealed record RaceEvent(double TimeSeconds, string HamsterId, string Message);

/// <summary>單一鼠鼠的完賽結果。</summary>
public sealed record HamsterResult
{
    public required string HamsterId { get; init; }
    public required string Name { get; init; }
    public required int Rank { get; init; }
    public required double FinishTimeSeconds { get; init; }
    public required double AverageSpeed { get; init; }
    public required double MaxSpeed { get; init; }
    public required bool Finished { get; init; }

    // Stage 2:賽後資源與卡牌數據（規格 §12）
    public double RemainingHp { get; init; }
    public double RemainingMp { get; init; }
    public int CardsFired { get; init; }
    public int CardsFizzled { get; init; }
}

/// <summary>一整場比賽的結果。</summary>
public sealed record RaceResult
{
    public required string RaceId { get; init; }
    public required ulong Seed { get; init; }
    public required double DistanceMeters { get; init; }
    public required IReadOnlyList<HamsterResult> Rankings { get; init; }
    public required IReadOnlyList<RaceEvent> Events { get; init; }
}
