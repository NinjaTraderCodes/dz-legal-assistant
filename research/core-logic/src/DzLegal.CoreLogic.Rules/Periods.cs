using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>
/// Counting a period (specs/rule-evaluator.md, "Counting a period"). The counting rule itself comes from the record. Since
/// the module <c>periods</c> is final (15 November 2026), its two counting methods are sourced: <c>excludeFirstDay</c> is
/// its statement b11 (civil-code 314 ¶1: a limitation period is counted in days, its first day not counted, complete at
/// the end of its last day), and <c>excludeFirstAndLastDay</c> with <c>extendToWorkingDay</c> is b32 and b35 (cpca 405
/// ¶1 and ¶4: neither the day of service nor the last day counts; a last day that is not a working day runs to the next).
/// <para>
/// TWO PLACEHOLDERS REMAIN UNSOURCED (marked below): the day arithmetic of months and years, and the rest days. They are
/// engineering choices, not statements of the law: never cite them as a rule, in a record, a message or a report. Replace
/// each one as soon as a final record cites a text for it, and record the replacement in the step 1 report.
/// </para>
/// </summary>
public static class Periods
{
    /// <summary>
    /// UNSOURCED placeholder 2: the weekly rest days used by <c>extendToWorkingDay</c>. The final <c>periods</c> record
    /// defines holidays as the official holidays and weekly rest days "under the texts in force" (b34, cpca 405 ¶3), and
    /// those texts are not in the store (its open point o7, grade D). Official holidays are not known here at all, which is
    /// one reason for the warning band.
    /// </summary>
    public static readonly IReadOnlySet<DayOfWeek> UnsourcedRestDays = new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday };

    /// <summary>
    /// How a deadline without <c>counting</c> is counted (the texts did not settle it, so the deadline is interpretive and
    /// warns at most), only so that a date can be shown, never extended to a working day. It is <c>excludeFirstDay</c>, the
    /// final <c>periods</c> record's rule for a limitation period (b11, civil-code 314 ¶1), and the earlier of the two
    /// methods that record established (b11; b32, cpca 405 ¶1): the date shown is never later than either.
    /// </summary>
    public const CountingMethod MethodWithoutCounting = CountingMethod.ExcludeFirstDay;

    /// <summary>The last day on which the act is in time.</summary>
    public static DateOnly LastDay(DateOnly start, Period period, CountingRule? counting)
    {
        ArgumentNullException.ThrowIfNull(period);
        var end = Add(start, period);
        end = (counting?.Method ?? MethodWithoutCounting) switch
        {
            CountingMethod.ExcludeFirstDay => end,
            CountingMethod.ExcludeFirstAndLastDay => end.AddDays(1),
            CountingMethod.Inclusive => end.AddDays(-1),
            var method => throw new ArgumentOutOfRangeException(nameof(counting), method, "Unknown counting method."),
        };

        if (counting?.ExtendToWorkingDay == true)
        {
            while (UnsourcedRestDays.Contains(end.DayOfWeek))
            {
                end = end.AddDays(1);
            }
        }

        return end;
    }

    /// <summary>
    /// <c>start + period</c>. UNSOURCED placeholder 1, in part: the final <c>periods</c> record counts by the Gregorian
    /// calendar (b27, civil-code 3 ¶1, grade D; whether a year is a calendar year or a fixed number of days is its open
    /// point o15, and the record keeps the calendar), but no text says where a month or a year ends when the last month is
    /// shorter: the same day number in the last month, or that month's last day when it is shorter (31 January + 1 month =
    /// 28 or 29 February). <see cref="DateOnly.AddMonths"/> and <see cref="DateOnly.AddYears"/> do exactly that.
    /// </summary>
    public static DateOnly Add(DateOnly start, Period period)
    {
        ArgumentNullException.ThrowIfNull(period);
        return period.Unit switch
        {
            PeriodUnit.Day => start.AddDays(period.Value),
            PeriodUnit.Month => start.AddMonths(period.Value),
            PeriodUnit.Year => start.AddYears(period.Value),
            var unit => throw new ArgumentOutOfRangeException(nameof(period), unit, "Unknown period unit."),
        };
    }
}
