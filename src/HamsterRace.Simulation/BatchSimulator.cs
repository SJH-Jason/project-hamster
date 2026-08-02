using System.Text;
using HamsterRace.Domain;

namespace HamsterRace.Simulation;

/// <summary>
/// Stage 5:批次模擬 + 統計（規格 §18-12「連續模擬至少 10,000 場統計勝率」）。
/// 用 seed 1..N 跑 N 場（每場天氣/風向/波動各自由 seed 抽,自然取樣預報分布）,
/// 聚合各鼠的勝率、平均名次、平均完賽時間,並依天氣拆分 → 驗證「無全環境最強鼠」。
/// </summary>
public sealed class BatchSimulator
{
    public sealed class Stat
    {
        public string Name = "";
        public int Races, Wins, Top3;
        public long RankSum;
        public double TimeSum;
        public int TimeCount;
        public double WinRate => Races > 0 ? 100.0 * Wins / Races : 0;
        public double Top3Rate => Races > 0 ? 100.0 * Top3 / Races : 0;
        public double AvgRank => Races > 0 ? (double)RankSum / Races : 0;
        public double AvgTime => TimeCount > 0 ? TimeSum / TimeCount : 0;
    }

    public sealed class BatchReport
    {
        public int Races;
        public double ElapsedMs;
        public Dictionary<string, Stat> ByHamster = new();
        public Dictionary<string, int> WeatherCount = new();               // weather → 場數
        public Dictionary<string, Dictionary<string, int>> WinsByWeather = new(); // weather → (hamsterId → wins)
        public Dictionary<string, string> DisplayName = new();             // id → 名稱
    }

    public BatchReport Run(RaceConfig baseConfig, int races, ulong startSeed = 1)
    {
        var sim = new RaceSimulator();
        var report = new BatchReport { Races = races };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (ulong i = 0; i < (ulong)races; i++)
        {
            var config = baseConfig with { Seed = startSeed + i };
            var result = sim.Run(config);

            report.WeatherCount.TryGetValue(result.ActualWeather, out var wc);
            report.WeatherCount[result.ActualWeather] = wc + 1;

            foreach (var r in result.Rankings)
            {
                if (!report.ByHamster.TryGetValue(r.HamsterId, out var s))
                    report.ByHamster[r.HamsterId] = s = new Stat { Name = r.Name };
                report.DisplayName[r.HamsterId] = r.Name;

                s.Races++;
                s.RankSum += r.Rank;
                if (r.Rank == 1) s.Wins++;
                if (r.Rank <= 3) s.Top3++;
                if (r.Finished) { s.TimeSum += r.FinishTimeSeconds; s.TimeCount++; }

                if (r.Rank == 1)
                {
                    if (!report.WinsByWeather.TryGetValue(result.ActualWeather, out var m))
                        report.WinsByWeather[result.ActualWeather] = m = new();
                    m.TryGetValue(r.HamsterId, out var w);
                    m[r.HamsterId] = w + 1;
                }
            }
        }

        sw.Stop();
        report.ElapsedMs = sw.Elapsed.TotalMilliseconds;
        return report;
    }

    public static string Render(BatchReport rep)
    {
        var sb = new StringBuilder();
        sb.AppendLine("════════════════════════════════════════════");
        sb.AppendLine($"  🐹 批次平衡報告 — {rep.Races:N0} 場  ({rep.ElapsedMs:0} ms)");
        sb.AppendLine("════════════════════════════════════════════");

        sb.AppendLine("── 總體勝率（依勝率排序）──");
        foreach (var s in rep.ByHamster.Values.OrderByDescending(x => x.WinRate))
            sb.AppendLine($"  {s.Name,-6} 勝率 {s.WinRate,5:0.0}%  " +
                          $"前三 {s.Top3Rate,5:0.0}%  均名次 {s.AvgRank:0.00}  均完賽 {s.AvgTime:0.0}s");

        sb.AppendLine();
        sb.AppendLine("── 依天氣拆分的勝率（驗證有無「全環境最強」）──");
        foreach (var (weather, count) in rep.WeatherCount.OrderByDescending(x => x.Value))
        {
            string wname = weather switch
            { "normal" => "一般", "sunny" => "大晴天", "rain" => "大雨", _ => weather };
            sb.AppendLine($"  【{wname}】{count} 場:");
            var wins = rep.WinsByWeather.TryGetValue(weather, out var m) ? m : new();
            foreach (var (id, w) in wins.OrderByDescending(x => x.Value))
                sb.AppendLine($"      {rep.DisplayName[id],-6} 拿下 {100.0 * w / count,5:0.0}% 的冠軍 ({w}/{count})");
        }
        sb.AppendLine("════════════════════════════════════════════");
        return sb.ToString();
    }
}
