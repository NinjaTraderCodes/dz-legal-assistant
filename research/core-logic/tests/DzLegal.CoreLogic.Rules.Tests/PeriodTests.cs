using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules.Tests;

/// <summary>
/// LastDay(start, period, counting): each method, with and without the extension to a working day; no counting; end of
/// month; leap year; a period ending on each placeholder rest day. January 2026: the 15th is a Thursday, the 16th a Friday,
/// the 17th a Saturday, the 18th a Sunday.
/// </summary>
public sealed class PeriodTests
{
    [Theory]
    [InlineData("2026-01-05", 10, PeriodUnit.Day, CountingMethod.ExcludeFirstDay, false, "2026-01-15")]
    [InlineData("2026-01-05", 10, PeriodUnit.Day, CountingMethod.ExcludeFirstAndLastDay, false, "2026-01-16")]
    [InlineData("2026-01-05", 10, PeriodUnit.Day, CountingMethod.Inclusive, false, "2026-01-14")]
    [InlineData("2026-01-05", 10, PeriodUnit.Day, CountingMethod.ExcludeFirstDay, true, "2026-01-15")] // Thursday: not extended
    [InlineData("2026-01-06", 10, PeriodUnit.Day, CountingMethod.ExcludeFirstDay, false, "2026-01-16")]
    [InlineData("2026-01-06", 10, PeriodUnit.Day, CountingMethod.ExcludeFirstDay, true, "2026-01-18")] // Friday -> Sunday
    [InlineData("2026-01-06", 10, PeriodUnit.Day, CountingMethod.ExcludeFirstAndLastDay, false, "2026-01-17")]
    [InlineData("2026-01-06", 10, PeriodUnit.Day, CountingMethod.ExcludeFirstAndLastDay, true, "2026-01-18")] // Saturday -> Sunday
    [InlineData("2026-01-08", 10, PeriodUnit.Day, CountingMethod.Inclusive, true, "2026-01-18")] // Saturday -> Sunday
    [InlineData("2026-01-31", 1, PeriodUnit.Month, CountingMethod.ExcludeFirstDay, false, "2026-02-28")] // a shorter month
    [InlineData("2028-01-31", 1, PeriodUnit.Month, CountingMethod.ExcludeFirstDay, false, "2028-02-29")] // a leap year
    [InlineData("2026-08-31", 6, PeriodUnit.Month, CountingMethod.ExcludeFirstAndLastDay, false, "2027-03-01")]
    [InlineData("2025-06-01", 6, PeriodUnit.Month, CountingMethod.ExcludeFirstAndLastDay, true, "2025-12-02")] // the module's ex2
    [InlineData("2024-02-29", 1, PeriodUnit.Year, CountingMethod.ExcludeFirstDay, false, "2025-02-28")]
    [InlineData("2024-02-29", 4, PeriodUnit.Year, CountingMethod.ExcludeFirstDay, false, "2028-02-29")]
    [InlineData("2019-01-31", 5, PeriodUnit.Year, CountingMethod.Inclusive, false, "2024-01-30")]
    public void TheLastDayFollowsTheCountingTheRecordGives(string start, int value, PeriodUnit unit, CountingMethod method, bool extend, string lastDay)
    {
        var counting = new CountingRule { Method = method, ExtendToWorkingDay = extend, Basis = ["s1"] };
        Assert.Equal(DateOnly.Parse(lastDay, System.Globalization.CultureInfo.InvariantCulture),
            Periods.LastDay(DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture), new Period { Value = value, Unit = unit }, counting));
    }

    [Theory]
    [InlineData("2026-01-06", 10, "2026-01-16")] // ends on a Friday, and is not extended
    [InlineData("2019-01-31", 5, "2019-02-05")]
    public void WithoutCountingThePeriodIsCountedFromTheNextDayWithoutExtension(string start, int days, string lastDay) =>
        Assert.Equal(DateOnly.Parse(lastDay, System.Globalization.CultureInfo.InvariantCulture),
            Periods.LastDay(DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture), new Period { Value = days, Unit = PeriodUnit.Day }, counting: null));

    [Fact]
    public void ThePlaceholdersAreTheOnesTheSpecificationNames()
    {
        Assert.Equal([DayOfWeek.Friday, DayOfWeek.Saturday], Periods.UnsourcedRestDays.Order());
        Assert.Equal(CountingMethod.ExcludeFirstDay, Periods.UnsourcedMethodWithoutCounting);
    }
}
