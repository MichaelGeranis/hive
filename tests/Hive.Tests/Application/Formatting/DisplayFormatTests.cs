using System.Globalization;
using FluentAssertions;
using Hive.Application.Formatting;

namespace Hive.Tests.Application.Formatting;

/// <summary>
/// Tests for DisplayFormat.
/// </summary>
public class DisplayFormatTests
{
    [Fact]
    public void ToDisplayDate_DateTime_FormatsAsDayMonthYear()
    {
        new DateTime(2025, 10, 6, 15, 30, 0).ToDisplayDate().Should().Be("06/10/2025");
    }

    [Fact]
    public void ToDisplayDate_DateOnly_FormatsAsDayMonthYear()
    {
        new DateOnly(2025, 1, 9).ToDisplayDate().Should().Be("09/01/2025");
    }

    [Fact]
    public void ToDisplayDateTime_UsesA24HourClock()
    {
        new DateTime(2025, 10, 6, 15, 5, 59).ToDisplayDateTime().Should().Be("06/10/2025 15:05");
    }

    [Fact]
    public void ToDisplayDate_UnderACultureWithAnotherSeparator_StillUsesSlashes()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            new DateTime(2025, 10, 6).ToDisplayDate().Should().Be("06/10/2025");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
