using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MensattScraper;

public static class Converter
{
    public enum TitleElement
    {
        Name,
        Tag
    }

    private static readonly HashSet<string> KnownTags = new()
    {
        "1",
        "2", "4", "5", "7", "8", "9", "10", "11", "12", "13", "30", "S", "R", "G", "L", "W", "F", "V", "Veg", "MSC",
        "Gf", "CO2", "B", "MV", "Wz", "Ro", "Ge", "Hafer", "Kr", "Ei", "Fi", "Er", "So", "Mi", "Man", "Hs", "Wa", "Ka",
        "Pe", "Pa", "Pi", "Mac", "Sel", "Sen", "Ses", "Su", "Lu", "We"
    };

    public static string ExtractElementFromTitle(string title, TitleElement titleElement)
    {
        if (string.IsNullOrEmpty(title) || !title.Contains('('))
            return titleElement == TitleElement.Name ? title : string.Empty;

        if (title.Count(x => x == '(') != title.Count(x => x == ')'))
        {
            Console.Error.WriteLine("Mismatched parentheses, Mode: " + titleElement + ", Title:`" + title + "`");
            return string.Empty;
        }

        var output = string.Empty;
        var currentParenthesisIndex = 0;
        while (currentParenthesisIndex < title.Length)
        {
            var start = title.IndexOf('(', currentParenthesisIndex);
            if (start == -1)
            {
                if (titleElement == TitleElement.Name)
                    output += title.Substring(currentParenthesisIndex, title.Length - currentParenthesisIndex);
                break;
            }

            start++;
            var end = title.IndexOf(')', start);

            // There should be no way for this condition to be true (according to the constraints placed above),
            // but I left it here for safety reasons
            if (end == -1)
            {
                Console.Error.WriteLine("Mismatched parentheses, Mode: " + titleElement + ", Title:`" + title + "`");
                return string.Empty;
            }

            var content = title.Substring(start, end - start);

            var split = content.Split(',');

            if (split.Any(possibleTag => KnownTags.Contains(possibleTag)))
            {
                if (titleElement == TitleElement.Tag)
                    output += content + ',';
                else if ((start - currentParenthesisIndex - 2) > 0)
                    output += title.Substring(currentParenthesisIndex, start - currentParenthesisIndex - 1);
            }
            else if (titleElement == TitleElement.Name)
            {
                output += title.Substring(currentParenthesisIndex, end - currentParenthesisIndex + 1);
            }

            currentParenthesisIndex = end + 1;
        }

        return output.Trim(' ', ',').RemoveMultipleWhiteSpaces();
    }

    public static string SanitizeString(string input) =>
        Regex.Replace(input.ToLowerInvariant().RemoveDiacritics(),
            @"[^a-z0-9 ]", string.Empty).RemoveMultipleWhiteSpaces();

    public static string RemoveDiacritics(this string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString.EnumerateRunes())
        {
            var unicodeCategory = Rune.GetUnicodeCategory(c);
            // Ignore all runes, which are not simple letters
            // This works because é is ('´','e') in Unicode
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string RemoveMultipleWhiteSpaces(this string input) =>
        string.Join(' ', input.Split(' ').Where(x => !string.IsNullOrEmpty(x)));

    public static string[] ExtractSingleTagsFromTitle(string title) =>
        ExtractElementFromTitle(title.Replace("Cfebar", "").Replace("Hf", ""), TitleElement.Tag)
            .Split(',')
            .Where(x => !string.IsNullOrEmpty(x) && KnownTags.Contains(x)).Select(x => x.Trim()).Distinct().ToArray();

    public static DateOnly GetDateFromTimestamp(int timestamp)
    {
        var utcTimestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var x = TimeZoneInfo.GetSystemTimeZones();
        var offsetToCest = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time")
            .GetUtcOffset(utcTimestamp);
        return DateOnly.FromDateTime(utcTimestamp.Add(offsetToCest).Date);
    }

    public static int? FloatStringToInt(string? cents)
    {
        if (string.IsNullOrEmpty(cents) || cents == "-")
            return null;
        if (int.TryParse(cents.Replace(",", string.Empty).Replace(".", string.Empty), out var ret))
            return ret;
        return null;
    }

    public static string[] GetSideDishes(string sideDishes) => ExtractElementFromTitle(sideDishes, TitleElement.Name)
        .Replace("Wahlbeilagen: ", string.Empty).Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
        .ToArray();
}