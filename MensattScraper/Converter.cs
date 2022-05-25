using System.Globalization;

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

            if (end == -1)
            {
                Console.Error.WriteLine("Mismatched parentheses");
                return string.Empty;
            }

            var content = title.Substring(start, end - start);

            var split = content.Split(',');

            if (split.Any(possibleTag => KnownTags.Contains(possibleTag)))
            {
                if (titleElement == TitleElement.Tag)
                    output += content + ',';
                else
                    output += title.Substring(currentParenthesisIndex, start - currentParenthesisIndex - 2);
            }
            else if (titleElement == TitleElement.Name)
            {
                output += title.Substring(currentParenthesisIndex, end - currentParenthesisIndex + 1);
            }

            currentParenthesisIndex = end + 1;
        }

        return output.Trim(' ', ',');
    }

    public static string[] ExtractSingleTagsFromTitle(string title) =>
        ExtractElementFromTitle(title.Replace("Cfebar", "").Replace("Hf", ""), TitleElement.Tag).Split(',')
            .Where(x => !string.IsNullOrEmpty(x) && KnownTags.Contains(x)).Select(x => x.Trim()).Distinct().ToArray();

    public static DateOnly GetDateFromTimestamp(int timestamp)
    {
        var utcTimestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var x = TimeZoneInfo.GetSystemTimeZones();
        var offsetToCest = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time")
            .GetUtcOffset(utcTimestamp);
        return DateOnly.FromDateTime(utcTimestamp.Add(offsetToCest).Date);
    }

    public static int? FloatToInt(string? cents) => (string.IsNullOrEmpty(cents) || cents == "-")
        ? null
        : int.Parse(cents.Replace(",", string.Empty).Replace(".", string.Empty));

    public static string[] GetSideDishes(string sideDishes) => ExtractElementFromTitle(sideDishes, TitleElement.Name)
        .Replace("Wahlbeilagen: ", string.Empty).Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
        .ToArray();
}