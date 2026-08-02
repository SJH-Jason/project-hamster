namespace HamsterRace.Domain;

/// <summary>一場比賽的設定。Stage 1:距離、參賽鼠、seed、tick 長度。</summary>
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
