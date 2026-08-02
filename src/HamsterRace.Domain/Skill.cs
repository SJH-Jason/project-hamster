namespace HamsterRace.Domain;

/// <summary>技能自動觸發條件（規格 §8.2 的實用子集）。</summary>
public enum SkillTrigger
{
    Periodic,     // 冷卻一好就放（規律加速）
    RemainingM,   // 進入「剩餘 ≤ Value 公尺」時（衝刺類）
    HpBelow,      // HP 低於 Value×上限 時（保命/回復類）
    RankBehind,   // 排名非第一時（逆境突進類）
}

/// <summary>
/// 主動技能（規格 §8）。與卡牌不同:技能有冷卻、可重複發動。
/// Stage 4:親密度(成功率)與熟練度(效果)先固定,不做養成變動。
/// </summary>
public sealed record Skill
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    public required SkillTrigger Trigger { get; init; }

    /// <summary>觸發門檻:RemainingM=公尺;HpBelow=比例(0–1);其餘忽略。</summary>
    public double TriggerValue { get; init; }

    public double SpeedBonus { get; init; }
    public double DurationSeconds { get; init; } = 2.0;
    public double CooldownSeconds { get; init; } = 5.0;

    public int HpCost { get; init; }
    public int MpCost { get; init; }

    /// <summary>回復量（穩定呼吸類）。</summary>
    public int HpRecover { get; init; }
    public int MpRecover { get; init; }
}
