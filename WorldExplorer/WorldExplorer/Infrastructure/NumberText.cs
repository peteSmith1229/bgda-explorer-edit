using System;
using System.Globalization;

namespace WorldExplorer.Infrastructure;

/// <summary>Reads and writes the numbers typed into editor boxes (always with a "." decimal point).</summary>
public static class NumberText
{
    /// <summary>A whole number, such as 250 or -1, or hexadecimal such as 0xFA.</summary>
    public static bool TryParseInteger(string text, out int value)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("-0x", StringComparison.OrdinalIgnoreCase))
        {
            var negative = text[0] == '-';
            if (uint.TryParse(text[(negative ? 3 : 2)..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                    out var hex) && (!negative || hex <= 0x80000000u))
            {
                value = negative ? (int)-(long)hex : unchecked((int)hex);
                return true;
            }

            value = 0;
            return false;
        }

        return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>A number such as 12.5 or -3.</summary>
    public static bool TryParseDecimal(string text, out double value) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
        double.IsFinite(value);

    /// <summary>A number with at most <paramref name="decimals"/> decimal places and no trailing zeros.</summary>
    public static string Format(double value, int decimals = 3)
    {
        var rounded = Math.Round(value, decimals);
        if (rounded == 0) rounded = 0; // no "-0"
        return rounded.ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);
    }
}
