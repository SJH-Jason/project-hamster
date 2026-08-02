using System.Text;
using HamsterRace.Domain;

namespace HamsterRace.Simulation;

/// <summary>把 RaceResult 轉成可讀的文字戰報（規格 §11 精簡戰報）。</summary>
public static class TextReport
{
    public static string Render(RaceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("════════════════════════════════════");
        sb.AppendLine($"  🐹 鼠鼠競賽戰報  (賽事 {result.RaceId} / seed {result.Seed})");
        sb.AppendLine($"  距離 {result.DistanceMeters:0} 公尺");
        sb.AppendLine("════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("── 逐段實況 ──");
        foreach (var e in result.Events)
            sb.AppendLine($"[{e.TimeSeconds,5:0.0}s] {e.Message}");

        sb.AppendLine();
        sb.AppendLine("── 最終名次 ──");
        foreach (var r in result.Rankings)
        {
            string time = r.Finished ? $"{r.FinishTimeSeconds,5:0.0}s" : "未完賽";
            sb.AppendLine(
                $"  第{r.Rank}名  {r.Name,-6}  完賽 {time}  " +
                $"均速 {r.AverageSpeed:0.00} m/s  最高 {r.MaxSpeed:0.00} m/s");
        }
        sb.AppendLine("════════════════════════════════════");
        return sb.ToString();
    }
}
