using HamsterRace.Domain;

namespace HamsterRace.Simulation;

/// <summary>
/// 模擬核心:固定時間步進(tick)引擎。
///   Stage 1:基礎跑速＋波動。
///   Stage 2:卡牌（依階段觸發、消耗 HP/MP、補給）、體力自然衰減、資源不足折扣(§7.3)。
/// 環境倍率(Stage 3)、技能(Stage 4)之後同樣掛在 ComputeTickSpeed。
///
/// 確定性:所有隨機來自 DeterministicRng(seed);同 seed+同設定 → 同結果。
/// </summary>
public sealed class RaceSimulator
{
    private sealed class ActiveEffect
    {
        public double SpeedBonus { get; init; }
        public double DrainMultiplier { get; init; } = 1.0;
        public double EndTime { get; init; }      // 到此時間（秒）失效
        public bool WholeRace { get; init; }
    }

    private sealed class Runner
    {
        public required Hamster Hamster { get; init; }
        public required DeterministicRng Rng { get; init; }
        public double Distance { get; set; }
        public double Hp { get; set; }
        public double Mp { get; set; }
        public double SpeedSum { get; set; }
        public double MaxSpeed { get; set; }
        public int Ticks { get; set; }
        public bool Finished { get; set; }
        public double FinishTime { get; set; }
        public HashSet<string> FiredCards { get; } = new();
        public List<ActiveEffect> Active { get; } = new();
        public int CardsFired { get; set; }
        public int CardsFizzled { get; set; }
    }

    public RaceResult Run(RaceConfig config)
    {
        if (config.Hamsters.Count == 0)
            throw new ArgumentException("至少要有一隻鼠鼠參賽");

        var rng = new DeterministicRng(config.Seed);
        var events = new List<RaceEvent>();
        var runners = new List<Runner>();
        foreach (var h in config.Hamsters)
        {
            runners.Add(new Runner
            {
                Hamster = h,
                Rng = new DeterministicRng(rng.NextULong()), // 每隻獨立子流,固定順序
                Hp = h.MaxHp,
                Mp = h.MaxMp,
            });
        }

        events.Add(new RaceEvent(0.0, "-",
            $"比賽開始。距離 {config.DistanceMeters:0} 公尺,{runners.Count} 隻鼠鼠起跑。"));

        double t = 0.0;
        int finishedCount = 0;
        int rankCounter = 0;
        var finishOrder = new Dictionary<string, int>();

        while (t < config.MaxRaceSeconds && finishedCount < runners.Count)
        {
            t += config.TickSeconds;

            foreach (var r in runners)
            {
                if (r.Finished) continue;

                double progress = r.Distance / config.DistanceMeters;
                RacePhase phase = PhaseOf(progress, config.Rules);

                // 1) 嘗試觸發本階段還沒打過的牌
                TryFireCards(r, phase, t, config, events);

                // 2) 移除過期效果
                r.Active.RemoveAll(e => !e.WholeRace && t >= e.EndTime);

                // 3) 算速度:( 基礎 + 卡加成 + 波動 ) × 資源折扣
                //    用「本 tick 起始的 HP/MP 狀態」決定速度,再依跑出的距離扣消耗。
                double cardBonus = r.Active.Sum(e => e.SpeedBonus);
                double jitter = r.Hamster.SpeedJitter > 0
                    ? r.Rng.NextRange(-r.Hamster.SpeedJitter, r.Hamster.SpeedJitter) : 0.0;
                double resourceMult = ResourceMultiplier(r, config.Rules);
                double speed = (r.Hamster.BaseSpeed + cardBonus + jitter) * resourceMult;
                if (speed < 0) speed = 0;

                // 4) 前進
                double distThisTick = speed * config.TickSeconds;
                r.Distance += distThisTick;
                r.SpeedSum += speed;
                r.Ticks++;
                if (speed > r.MaxSpeed) r.MaxSpeed = speed;

                // 5) 基本體力/精神消耗:依「跑出的距離」× 疲勞曲線 × 鼠鼠係數 × 卡牌節省
                //    每 10m 基本扣 HpDrainPer10m/MpDrainPer10m;疲勞隨進度 0.5→1.5 遞增（後段更耗）。
                double fatigue = config.Rules.FatigueStartMult +
                    (config.Rules.FatigueEndMult - config.Rules.FatigueStartMult) * Math.Min(1.0, progress);
                double drainMult = 1.0;
                foreach (var e in r.Active) drainMult *= e.DrainMultiplier;
                double units = (distThisTick / 10.0) * fatigue * r.Hamster.StaminaFactor * drainMult;
                r.Hp = Math.Max(0, r.Hp - units * config.Rules.HpDrainPer10m);
                r.Mp = Math.Max(0, r.Mp - units * config.Rules.MpDrainPer10m);

                if (r.Distance >= config.DistanceMeters)
                {
                    r.Finished = true;
                    r.FinishTime = t;
                    finishedCount++;
                    rankCounter++;
                    finishOrder[r.Hamster.Id] = rankCounter;
                    events.Add(new RaceEvent(t, r.Hamster.Id,
                        $"{r.Hamster.Name} 以第 {rankCounter} 名完賽,用時 {t:0.0} 秒" +
                        $"（剩餘 HP {r.Hp:0}／MP {r.Mp:0}）。"));
                }
            }
        }

        var unfinished = runners.Where(r => !r.Finished)
                                .OrderByDescending(r => r.Distance).ToList();
        foreach (var r in unfinished)
        {
            rankCounter++;
            finishOrder[r.Hamster.Id] = rankCounter;
        }

        var rankings = runners
            .Select(r => new HamsterResult
            {
                HamsterId = r.Hamster.Id,
                Name = r.Hamster.Name,
                Rank = finishOrder[r.Hamster.Id],
                FinishTimeSeconds = r.Finished ? r.FinishTime : double.PositiveInfinity,
                AverageSpeed = r.Ticks > 0 ? r.SpeedSum / r.Ticks : 0,
                MaxSpeed = r.MaxSpeed,
                Finished = r.Finished,
                RemainingHp = r.Hp,
                RemainingMp = r.Mp,
                CardsFired = r.CardsFired,
                CardsFizzled = r.CardsFizzled,
            })
            .OrderBy(x => x.Rank)
            .ToList();

        return new RaceResult
        {
            RaceId = config.RaceId,
            Seed = config.Seed,
            DistanceMeters = config.DistanceMeters,
            Rankings = rankings,
            Events = events,
        };
    }

    private static RacePhase PhaseOf(double progress, RaceRules rules)
    {
        if (progress < rules.StartPhaseEnd) return RacePhase.Start;
        if (progress < rules.EarlyPhaseEnd) return RacePhase.Early;
        if (progress < rules.MidPhaseEnd) return RacePhase.Mid;
        return RacePhase.Sprint;
    }

    /// <summary>卡片的觸發時機是否涵蓋當前階段。</summary>
    private static bool PhaseMatches(RacePhase cardPhase, RacePhase current)
        => cardPhase == RacePhase.Whole || cardPhase == current;

    private static void TryFireCards(Runner r, RacePhase phase, double t,
                                     RaceConfig config, List<RaceEvent> events)
    {
        foreach (var cardId in r.Hamster.Deck)
        {
            if (r.FiredCards.Contains(cardId)) continue;
            if (!config.Cards.TryGetValue(cardId, out var card)) continue;
            if (!PhaseMatches(card.Phase, phase)) continue;

            // 付得起才發（HP/MP 不夠 → 卡失效,這就是「亂花體力後段發不出衝刺」的策略張力）
            if (r.Hp < card.HpCost || r.Mp < card.MpCost)
            {
                r.FiredCards.Add(cardId); // 標記已嘗試,不重複試
                r.CardsFizzled++;
                events.Add(new RaceEvent(t, r.Hamster.Id,
                    $"{r.Hamster.Name} 想用「{card.Name}」但資源不足,發不出來。"));
                continue;
            }

            r.FiredCards.Add(cardId);
            r.Hp -= card.HpCost;
            r.Mp -= card.MpCost;
            if (card.HpRecover > 0)
                r.Hp = Math.Min(r.Hamster.MaxHp, r.Hp + card.HpRecover);
            if (card.MpRecover > 0)
                r.Mp = Math.Min(r.Hamster.MaxMp, r.Mp + card.MpRecover);
            r.CardsFired++;

            if (card.SpeedBonus != 0 || card.DrainMultiplier != 1.0)
            {
                r.Active.Add(new ActiveEffect
                {
                    SpeedBonus = card.SpeedBonus,
                    DrainMultiplier = card.DrainMultiplier,
                    EndTime = t + card.DurationSeconds,
                    WholeRace = card.WholeRace,
                });
            }

            string detail = card.HpRecover > 0 || card.MpRecover > 0
                ? $"回復 HP {card.HpRecover}／MP {card.MpRecover}"
                : card.DrainMultiplier != 1.0
                    ? $"消耗×{card.DrainMultiplier:0.0}"
                    : $"+{card.SpeedBonus:0.0} m/s";
            events.Add(new RaceEvent(t, r.Hamster.Id,
                $"{r.Hamster.Name} 打出「{card.Name}」（{detail}）。"));
        }
    }

    /// <summary>
    /// 資源不足的速度折扣（連續遞減,規格 §7.3 為端點）。
    /// 速度倍率 = HP因子 × MP因子;每項因子 = floor + (1-floor)×剩餘比例。
    /// 體力或精神一掉,基本速度就按比例往下掉;耗越兇跑越慢。
    /// </summary>
    private static double ResourceMultiplier(Runner r, RaceRules rules)
    {
        double floor = rules.ResourceFloor;
        double hpFrac = r.Hamster.MaxHp > 0 ? Math.Clamp(r.Hp / r.Hamster.MaxHp, 0, 1) : 1.0;
        double mpFrac = r.Hamster.MaxMp > 0 ? Math.Clamp(r.Mp / r.Hamster.MaxMp, 0, 1) : 1.0;
        double hpFactor = floor + (1 - floor) * hpFrac;
        double mpFactor = floor + (1 - floor) * mpFrac;
        return hpFactor * mpFactor;
    }
}
