using System.Globalization;

public static class StringParseUtils
{
    public static bool TryParseNoLocale(this string text, out ushort result) =>
        ushort.TryParse(text, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out result);

    public static bool TryParseNoLocale(this string text, out float result) =>
        float.TryParse(text, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out result);

    public static bool TryParseNoLocale(this string text, out double result) =>
        double.TryParse(text, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out result);

    public static bool TryParseNoLocale(this string text, out int result) =>
        int.TryParse(text, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out result);

    public static bool TryParseNoLocale(this string text, out uint result) =>
        uint.TryParse(text, NumberStyles.Number, CultureInfo.CreateSpecificCulture("en-US"), out result);
}