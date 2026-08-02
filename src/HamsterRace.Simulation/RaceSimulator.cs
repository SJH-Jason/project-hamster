using HamsterRace.Domain;

namespace HamsterRace.Simulation;

/// <summary>
/// Stage 1 純模擬核心:固定時間步進(tick)引擎。
/// 目前只用「基礎跑速 + 每 tick 微小波動」推進,產出名次、完賽時間與逐段事件。
/// 卡牌(Stage 2)、環境倍率(Stage 3)、技能(Stage 4)之後掛在 ComputeTickSpeed 上。
///
/// 確定性保證:所有隨機都來自 DeterministicRng(seed);同 seed+同設定 → 同結果。
/// </summary>
public sealed class RaceSimulator
{
    private sealed class Runner
    {
        public required Hamster Hamster { get; init; }
        public double Distance { get; set; }
        public double SpeedSum { get; set; }
        public double MaxSpeed { get; set; }
        public int Ticks { get; set; }
        public bool Finished { get; set; }
        public double FinishTime { get; set; }
    }

    public RaceResult Run(RaceConfig config)
    {
        if (config.Hamsters.Count == 0)
            throw new ArgumentException("至少要有一隻鼠鼠參賽");

        var rng = new DeterministicRng(config.Seed);
        var events = new List<RaceEvent>();

        // 每隻鼠一個獨立的 RNG 子流,順序固定 → 確定性且彼此不互相干擾。
        var runners = new List<Runner>();
        var rngByHamster = new Dictionary<string, DeterministicRng>();
        foreach (var h in config.Hamsters)
        {
            runners.Add(new Runner { Hamster = h });
            // 用主 rng 派生每隻的種子,保證固定順序 → 可重現。
            rngByHamster[h.Id] = new DeterministicRng(rng.NextULong());
        }

        events.Add(new RaceEvent(0.0, "-",
            $"比賽開始。距離 {config.DistanceMeters:0} 公尺,{runners.Count} 隻鼠鼠起跑。"));

        double t = 0.0;
        int finishedCount = 0;
        int rankCounter = 0;
        var finishOrder = new Dictionary<string, int>(); // hamsterId -> rank

        while (t < config.MaxRaceSeconds && finishedCount < runners.Count)
        {
            t += config.TickSeconds;

            foreach (var r in runners)
            {
                if (r.Finished) continue;

                double speed = ComputeTickSpeed(r.Hamster, rngByHamster[r.Hamster.Id]);
                if (speed < 0) speed = 0;

                r.Distance += speed * config.TickSeconds;
                r.SpeedSum += speed;
                r.Ticks++;
                if (speed > r.MaxSpeed) r.MaxSpeed = speed;

                if (r.Distance >= config.DistanceMeters)
                {
                    r.Finished = true;
                    r.FinishTime = t;
                    finishedCount++;
                    rankCounter++;
                    finishOrder[r.Hamster.Id] = rankCounter;
                    events.Add(new RaceEvent(t, r.Hamster.Id,
                        $"{r.Hamster.Name} 以第 {rankCounter} 名完賽,用時 {t:0.0} 秒。"));
                }
            }
        }

        // 未完賽的（撞到時間上限）依已跑距離排在完賽者之後。
        var unfinished = runners.Where(r => !r.Finished)
                                .OrderByDescending(r => r.Distance)
                                .ToList();
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

    /// <summary>
    /// 本 tick 的瞬時速度。Stage 1 = 基礎跑速 ± 波動。
    /// 之後階段會在這裡疊加:卡牌加成、地形/天氣/風向/時段倍率、技能、HP/MP 折扣。
    /// </summary>
    private static double ComputeTickSpeed(Hamster h, DeterministicRng rng)
    {
        double jitter = h.SpeedJitter > 0 ? rng.NextRange(-h.SpeedJitter, h.SpeedJitter) : 0.0;
        return h.BaseSpeed + jitter;
    }
}
