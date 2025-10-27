using System.Globalization;

namespace Maenifold.Utils;

public static class CultureInvariantHelpers
{
    public static DateTime ParseDateTime(string s)
    => DateTime.Parse(s, CultureInfo.InvariantCulture);

    public static bool TryParseDateTime(string s, out DateTime result)
            => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    public static long ParseLong(string s)
            => long.Parse(s, CultureInfo.InvariantCulture);

    public static int ParseInt(string s)
            => int.Parse(s, CultureInfo.InvariantCulture);

    public static int ToInt32(object value)
            => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime dateTime, string format)
            => dateTime.ToString(format, CultureInfo.InvariantCulture);

    public static string FormatDateTimeOffset(DateTimeOffset dateTimeOffset, string format)
            => dateTimeOffset.ToString(format, CultureInfo.InvariantCulture);

    public static string ToString(int value)
            => value.ToString(CultureInfo.InvariantCulture);

    public static string ToString(long value)
            => value.ToString(CultureInfo.InvariantCulture);
}