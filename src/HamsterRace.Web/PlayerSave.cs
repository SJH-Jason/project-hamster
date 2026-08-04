namespace HamsterRace.Web;

/// <summary>
/// 玩家存檔（Stage-2 養成層）。存進 localStorage。
/// 資源：種子幣(經濟/外觀/設施)、訓練點(技能/成長)。訓練等級=帳號進度(解鎖設施+提高體力上限)。
/// 體力(行動力)＝gate 訓練與比賽,實時回復,跟賽中 HP 無關。
/// 數值都是「先 demo」的,之後再平衡。
/// </summary>
public class PlayerSave
{
    public int Version { get; set; } = 1;

    public int SeedCoins { get; set; } = 0;      // 🪙 種子幣
    public int TrainingPoints { get; set; } = 0; // ⭐ 訓練點
    public int TrainingLevel { get; set; } = 1;  // 📈 訓練等級
    public int TrainingExp { get; set; } = 0;

    public int Energy { get; set; } = 8;         // ⚡ 目前體力
    public long LastEnergyTicks { get; set; } = 0; // 上次體力更新的 UTC ticks(算回復用)

    /// <summary>擁有的鼠(2A 先給預設 4 隻;2D 招募擴充)。</summary>
    public List<string> OwnedHamsterIds { get; set; } = new();

    // ---- demo 常數(先 demo 再調) ----
    public const int RaceEnergyCost = 1;
    public const int TrainEnergyCost = 1;
    public const int RefillMinutes = 60;         // 全滿要 60 分鐘

    /// <summary>體力上限：基礎 8(≈訓練5+比賽3),每升 1 級 +2。</summary>
    public int EnergyMax => 8 + (TrainingLevel - 1) * 2;
    /// <summary>升下一級所需經驗。</summary>
    public int ExpToNext => TrainingLevel * 100;

    /// <summary>依實時經過補回體力（全滿需 RefillMinutes 分鐘）。</summary>
    public void RefreshEnergy(DateTime nowUtc)
    {
        if (LastEnergyTicks == 0) { LastEnergyTicks = nowUtc.Ticks; return; }
        if (Energy >= EnergyMax) { LastEnergyTicks = nowUtc.Ticks; return; }

        var last = new DateTime(LastEnergyTicks, DateTimeKind.Utc);
        double minutes = (nowUtc - last).TotalMinutes;
        if (minutes <= 0) return;

        double perPoint = (double)RefillMinutes / EnergyMax; // 幾分鐘回 1 點
        int add = (int)(minutes / perPoint);
        if (add <= 0) return;

        Energy = Math.Min(EnergyMax, Energy + add);
        LastEnergyTicks = Energy >= EnergyMax ? nowUtc.Ticks : last.AddMinutes(add * perPoint).Ticks;
    }

    /// <summary>下一點體力還要幾分鐘(給 UI 顯示)。滿了回 0。</summary>
    public double MinutesToNextPoint(DateTime nowUtc)
    {
        if (Energy >= EnergyMax) return 0;
        var last = new DateTime(Math.Max(LastEnergyTicks, 1), DateTimeKind.Utc);
        double perPoint = (double)RefillMinutes / EnergyMax;
        double since = (nowUtc - last).TotalMinutes;
        return Math.Max(0, perPoint - (since % perPoint));
    }

    /// <summary>花體力(不足回 false)。從滿的狀態開始扣時,啟動回復計時。</summary>
    public bool TrySpendEnergy(int cost, DateTime nowUtc)
    {
        RefreshEnergy(nowUtc);
        if (Energy < cost) return false;
        if (Energy >= EnergyMax) LastEnergyTicks = nowUtc.Ticks; // 從滿開始扣→啟動計時
        Energy -= cost;
        return true;
    }

    /// <summary>比賽名次獎勵：種子幣 / 訓練點 / 訓練經驗。回傳實得,供 UI 顯示。</summary>
    public (int coins, int tp, int exp, int levelUps) AddRaceReward(int rank)
    {
        var (c, t, e) = rank switch
        {
            1 => (100, 20, 30),
            2 => (70, 12, 20),
            3 => (50, 8, 15),
            _ => (30, 5, 10),
        };
        SeedCoins += c;
        TrainingPoints += t;
        int lv = AddExp(e);
        return (c, t, e, lv);
    }

    /// <summary>加經驗,回傳升了幾級。</summary>
    public int AddExp(int e)
    {
        TrainingExp += e;
        int ups = 0;
        while (TrainingExp >= ExpToNext) { TrainingExp -= ExpToNext; TrainingLevel++; ups++; }
        return ups;
    }

    /// <summary>新檔:擁有預設 4 隻鼠、體力滿。</summary>
    public static PlayerSave NewGame(IEnumerable<string> defaultHamsterIds, DateTime nowUtc)
    {
        var s = new PlayerSave { OwnedHamsterIds = defaultHamsterIds.ToList(), LastEnergyTicks = nowUtc.Ticks };
        s.Energy = s.EnergyMax;
        return s;
    }
}
