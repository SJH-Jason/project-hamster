namespace HamsterRace.Domain;

/// <summary>平衡參數（規格 §7.3 資源折扣、階段界線）。集中可調,不寫死在引擎。</summary>
public sealed record RaceRules
{
    /// <summary>
    /// 單一資源耗盡時的速度地板（每項）。速度倍率 = HP因子 × MP因子,
    /// 每項因子 = ResourceFloor + (1-ResourceFloor)×該資源剩餘比例。
    /// 0.5 時:兩滿=1.0、單項歸零=0.5、雙歸零=0.25(對齊規格 §7.3 端點),
    /// 但中間連續遞減——體力/精神一掉,速度就跟著掉。
    /// </summary>
    public double ResourceFloor { get; init; } = 0.5;

    // --- 基本體力/精神消耗（每跑一段距離扣,與時間無關）---
    /// <summary>每 10 公尺的基本 HP 消耗。</summary>
    public double HpDrainPer10m { get; init; } = 5.0;
    /// <summary>每 10 公尺的基本 MP 消耗。</summary>
    public double MpDrainPer10m { get; init; } = 5.0;
    /// <summary>疲勞曲線:比賽起點的消耗倍率（前期消耗少）。</summary>
    public double FatigueStartMult { get; init; } = 0.5;
    /// <summary>疲勞曲線:比賽終點的消耗倍率（後段消耗多）。平均≈1 → 維持 5/10m 基準。</summary>
    public double FatigueEndMult { get; init; } = 1.5;

    // 階段界線（跑完進度比例）
    public double StartPhaseEnd { get; init; } = 0.15;
    public double EarlyPhaseEnd { get; init; } = 0.40;
    public double MidPhaseEnd { get; init; } = 0.70;
    // 之後（>MidPhaseEnd）即衝刺段

    // --- 賽場隨機事件（卡凡 2026-08-04）---
    /// <summary>每 20m 判定跌倒的機率;跌倒後停住的秒數。</summary>
    public double TripChance { get; init; } = 0.05;
    public double TripStunSeconds { get; init; } = 1.0;
    /// <summary>每 10m 判定偷吃零食的機率、回復量、每場上限。</summary>
    public double SnackChance { get; init; } = 0.10;
    public int SnackHp { get; init; } = 20;
    public int SnackMp { get; init; } = 20;
    public int SnackMaxPerRace { get; init; } = 2;
    /// <summary>開場判定「得心應手」機率;每秒累加的速度。</summary>
    public double FlowChance { get; init; } = 0.10;
    public double FlowSpeedPerSec { get; init; } = 0.2;
    /// <summary>最後 20m 判定「熱血沸騰」機率;加的速度(m/s)。</summary>
    public double FiredUpChance { get; init; } = 0.10;
    public double FiredUpSpeed { get; init; } = 1.5;
}

/// <summary>
/// 賽道環境。賽道與時段賽前固定可見;天氣與風向賽前只給「預報機率」,
/// 鎖牌後才用亂數抽出實際結果（規格 §4.4、§5）——這就是「押天氣」的賭注來源。
/// </summary>
public sealed record RaceEnvironment
{
    /// <summary>賽道地形 key（track/grass/asphalt…）。對應鼠鼠 TerrainAffinity。</summary>
    public string Terrain { get; init; } = "track";

    /// <summary>時段 key（morning/day/evening/night）。對應 TimeAffinity。</summary>
    public string TimeOfDay { get; init; } = "day";

    /// <summary>天氣預報:key→機率（normal/sunny/rain…）。加總約 1。</summary>
    public IReadOnlyDictionary<string, double> WeatherForecast { get; init; }
        = new Dictionary<string, double> { ["normal"] = 1.0 };

    /// <summary>風向預報:key→機率（none/tail/head…）。</summary>
    public IReadOnlyDictionary<string, double> WindForecast { get; init; }
        = new Dictionary<string, double> { ["none"] = 1.0 };
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

    /// <summary>賽道環境（Stage 3）。</summary>
    public RaceEnvironment Environment { get; init; } = new();

    /// <summary>技能型錄（id → Skill）。Stage 4 起用。</summary>
    public IReadOnlyDictionary<string, Skill> Skills { get; init; }
        = new Dictionary<string, Skill>();

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
    public int SkillsFired { get; init; }
    public int SkillsFailed { get; init; }
}

/// <summary>2D 動畫用的一格畫面:某時間點各鼠的已跑距離（依參賽順序）。</summary>
public sealed record ReplayFrame(double T, IReadOnlyList<double> Distances);

/// <summary>整場的位置時間軸,給前端動畫播放。</summary>
public sealed record RaceReplay
{
    public required double DistanceMeters { get; init; }
    public required IReadOnlyList<string> HamsterIds { get; init; }
    public required IReadOnlyList<string> HamsterNames { get; init; }
    public required IReadOnlyList<ReplayFrame> Frames { get; init; }
}

/// <summary>一整場比賽的結果。</summary>
public sealed record RaceResult
{
    public required string RaceId { get; init; }
    public required ulong Seed { get; init; }
    public required double DistanceMeters { get; init; }
    public required IReadOnlyList<HamsterResult> Rankings { get; init; }
    public required IReadOnlyList<RaceEvent> Events { get; init; }

    /// <summary>2D 動畫重播（只有要求時才產生,batch 不需要）。</summary>
    public RaceReplay? Replay { get; init; }

    // Stage 3:實際抽出的環境（供戰報顯示「預報 vs 實際」）
    public string Terrain { get; init; } = "";
    public string TimeOfDay { get; init; } = "";
    public string ActualWeather { get; init; } = "";
    public string ActualWind { get; init; } = "";
}
