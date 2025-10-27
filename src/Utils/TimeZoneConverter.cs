using System.Globalization;

namespace Maenifold.Utils;

public static class TimeZoneConverter

{

    public static string ToLocalDisplay(string? isoTimestamp)
    {
        if (string.IsNullOrWhiteSpace(isoTimestamp))
            return "N/A";

        try
        {

            if (DateTimeOffset.TryParse(isoTimestamp, out var utcTime))
            {

                var localTime = TimeZoneInfo.ConvertTime(utcTime, TimeZoneInfo.Local);


                return localTime.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
            }

            return "Invalid date";
        }
        catch
        {
            return "Invalid date";
        }
    }

    public static string GetUtcNowIso()
    {
        return DateTimeOffset.UtcNow.ToString("O");
    }

    public static string FileTimeToIso(DateTime fileTime)
    {

        return new DateTimeOffset(fileTime, TimeSpan.Zero).ToString("O");
    }
}