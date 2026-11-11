using System.Globalization;

namespace Hive.Application.Formatting;

/// <summary>
/// How dates appear in text Hive writes for the manager to read: activity descriptions,
/// insight messages and Excel exports. The one date format is dd/MM/yyyy.
/// </summary>
/// <remarks>
/// Always formatted with the invariant culture: in a .NET format string <c>/</c> means
/// "the current culture's date separator", so a machine set to German would otherwise
/// print 06.10.2025. Machine-read dates (file names, prompts) stay ISO 8601.
/// </remarks>
public static class DisplayFormat
{
    public const string Date = "dd/MM/yyyy";
    public const string DateTime = "dd/MM/yyyy HH:mm";

    public static string ToDisplayDate(this System.DateTime value) =>
        value.ToString(Date, CultureInfo.InvariantCulture);

    public static string ToDisplayDate(this DateOnly value) =>
        value.ToString(Date, CultureInfo.InvariantCulture);

    public static string ToDisplayDateTime(this System.DateTime value) =>
        value.ToString(DateTime, CultureInfo.InvariantCulture);
}
