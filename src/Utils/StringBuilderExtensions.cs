using System.Globalization;
using System.Text;

namespace Maenifold.Utils;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendInvariant(this StringBuilder sb, FormattableString formattable)
    {
        sb.Append(formattable.ToString(CultureInfo.InvariantCulture));
        return sb;
    }

    public static StringBuilder AppendLineInvariant(this StringBuilder sb, FormattableString formattable)
    {
        sb.AppendLine(formattable.ToString(CultureInfo.InvariantCulture));
        return sb;
    }

    public static StringBuilder AppendFormatInvariant(this StringBuilder sb, string format, params object[] args)
            => sb.AppendFormat(CultureInfo.InvariantCulture, format, args);
}