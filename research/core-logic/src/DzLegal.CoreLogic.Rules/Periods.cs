using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>
/// Counting a period (specs/rule-evaluator.md, "Counting a period"). The counting rule itself comes from the record; three
/// things the evaluator needs have no source in the store yet and are kept here as named placeholders.
/// <para>
/// THE PLACEHOLDERS BELOW ARE UNSOURCED. They are engineering choices, not statements of the law: never cite them as a rule,
/// in a record, a message or a report. Replace each one as soon as a final record cites a text for it, and record the
/// replacement in the step 1 report.
/// </para>
/// </summary>
public static class Periods
{
    /// <summary>
    /// UNSOURCED placeholder 2: the weekly rest days used by <c>extendToWorkingDay</c>, until the periods module cites the
    /// text that sets them. Official holidays are not known here at all, which is one reason for the warning band.
    /// </summary>
    public static readonly IReadOnlySet<DayOfWeek> UnsourcedRestDays = new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday };

    /// <summary>
    /// UNSOURCED placeholder 3: how a deadline without <c>counting</c> is counted (the texts did not settle it, so the
    /// deadline is interpretive and warns at most). Only so that a date can be shown; never extended to a working day.
    /// </summary>
    public const CountingMethod UnsourcedMethodWithoutCounting = CountingMethod.ExcludeFirstDay;

    /// <summary>The last day on which the act is in time.</summary>
    public static DateOnly LastDay(DateOnly start, Period period, CountingRule? counting)
    {
        ArgumentNullException.ThrowIfNull(period);
        var end = Add(start, period);
        end = (counting?.Method ?? UnsourcedMethodWithoutCounting) switch
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
    /// <c>start + period</c>. UNSOURCED placeholder 1, for months and years: the same day number in the last month, or that
    /// month's last day when it is shorter (31 January + 1 month = 28 or 29 February). <see cref="DateOnly.AddMonths"/> and
    /// <see cref="DateOnly.AddYears"/> do exactly that.
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
