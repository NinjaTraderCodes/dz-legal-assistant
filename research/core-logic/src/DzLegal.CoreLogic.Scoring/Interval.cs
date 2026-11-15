namespace DzLegal.CoreLogic.Scoring;

public enum Verdict
{
    Met,
    NotMet,
    Inconclusive,
    NotMeasurable,
    NotMetCoverage,
}

/// <summary>The Wilson 95 % interval and the three verdicts of specs/scorer.md ("Three verdicts").</summary>
public static class Interval
{
    public const double Z = 1.96;

    public static (double Low, double High) Wilson(int right, int n)
    {
        if (n <= 0)
        {
            return (0, 1);
        }

        var p = (double)right / n;
        var z2 = Z * Z;
        var centre = (p + z2 / (2 * n)) / (1 + z2 / n);
        var half = Z * Math.Sqrt(p * (1 - p) / n + z2 / (4.0 * n * n)) / (1 + z2 / n);
        return (Math.Max(0, centre - half), Math.Min(1, centre + half));
    }

    /// <summary>met: the lower bound at or above the target; not met: the upper bound below it; else inconclusive.</summary>
    public static Verdict Of(int right, int n, double target)
    {
        if (n <= 0)
        {
            return Verdict.NotMeasurable;
        }

        var (low, high) = Wilson(right, n);
        return low >= target ? Verdict.Met : high < target ? Verdict.NotMet : Verdict.Inconclusive;
    }

    /// <summary>The smallest count that is "met" and the largest that is "not met" at size n (the size table).</summary>
    public static (int? MetFrom, int? NotMetUpTo) Thresholds(int n, double target)
    {
        int? met = null, notMet = null;
        for (var k = 0; k <= n; k++)
        {
            var verdict = Of(k, n, target);
            if (verdict == Verdict.Met && met is null)
            {
                met = k;
            }

            if (verdict == Verdict.NotMet)
            {
                notMet = k;
            }
        }

        return (met, notMet);
    }

    /// <summary>
    /// For an inconclusive line: how many notes in all would decide it if the rate stayed the same; null when no size up to
    /// 100,000 would (the rate sits on the target).
    /// </summary>
    public static int? NeededToDecide(int right, int n, double target)
    {
        if (n <= 0)
        {
            return null;
        }

        var rate = (double)right / n;
        for (var m = n + 1; m <= 100_000; m = m < 1000 ? m + 1 : m + m / 100)
        {
            var k = (int)Math.Round(rate * m, MidpointRounding.ToEven);
            if (Of(k, m, target) is Verdict.Met or Verdict.NotMet)
            {
                return m;
            }
        }

        return null;
    }
}
