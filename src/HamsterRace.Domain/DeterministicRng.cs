namespace HamsterRace.Domain;

/// <summary>
/// 確定性亂數產生器（SplitMix64）。
/// 刻意不用 System.Random —— 它的內部演算法不保證跨 .NET 版本/平台一致，
/// 會破壞「相同 seed 必產生相同比賽結果」這條 MVP 驗收鐵則（規格 §10.2）。
/// SplitMix64 演算法固定、可攜、可重現。
/// </summary>
public sealed class DeterministicRng
{
    private ulong _state;

    public ulong Seed { get; }

    public DeterministicRng(ulong seed)
    {
        Seed = seed;
        _state = seed;
    }

    /// <summary>下一個 64-bit 亂數。</summary>
    public ulong NextULong()
    {
        // SplitMix64
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>[0,1) 的 double。</summary>
    public double NextDouble()
    {
        // 取高 53 bits 給 double 的尾數，得到 [0,1)
        return (NextULong() >> 11) * (1.0 / 9007199254740992.0);
    }

    /// <summary>[minInclusive, maxExclusive) 的 int。</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentException("maxExclusive 必須大於 minInclusive");
        ulong range = (ulong)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextULong() % range);
    }

    /// <summary>[min, max] 的 double，均勻分布。</summary>
    public double NextRange(double min, double max) => min + NextDouble() * (max - min);
}
