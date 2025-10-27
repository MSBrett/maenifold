namespace Maenifold.Utils;

public static class StringExtensions
{
    public static bool StartsWithOrdinal(this string str, string value)
    => str.StartsWith(value, StringComparison.Ordinal);

    public static bool EndsWithOrdinal(this string str, string value)
            => str.EndsWith(value, StringComparison.Ordinal);

    public static bool ContainsOrdinal(this string str, string value)
            => str.Contains(value, StringComparison.Ordinal);

    public static bool StartsWithOrdinalIgnoreCase(this string str, string value)
            => str.StartsWith(value, StringComparison.OrdinalIgnoreCase);

    public static bool EndsWithOrdinalIgnoreCase(this string str, string value)
            => str.EndsWith(value, StringComparison.OrdinalIgnoreCase);

    public static bool ContainsOrdinalIgnoreCase(this string str, string value)
            => str.Contains(value, StringComparison.OrdinalIgnoreCase);
}