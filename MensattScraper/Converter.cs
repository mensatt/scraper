using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MensattScraper.DatabaseSupport;
using Microsoft.Extensions.Logging;

namespace MensattScraper;

public static class Converter
{
    public enum TitleElement
    {
        Name,
        Tag
    }

    public static string ExtractElementFromTitle(string? title, TitleElement titleElement)
    {
        if (title is null)
        {
            SharedLogger.LogWarning("Can\'t extract titleElement={TitleElement} out of a null title", titleElement);
            return string.Empty;
        }


        if (string.IsNullOrEmpty(title) || !title.Contains('('))
            return titleElement == TitleElement.Name
                ? title.Trim(' ', ',').RemoveMultipleWhiteSpaces().FirstCharUpper()
                : string.Empty;

        if (title.Count(x => x == '(') != title.Count(x => x == ')'))
        {
            SharedLogger.LogError("Mismatched parentheses, Mode: {TitleElement} Title:` {Title}`",
                titleElement, title);
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
            Trace.Assert(end != -1, "Braces are mismatched, missing )");

            // Trim is necessary, as sometimes the tags are like this "Pizza ( Wz)"
            var content = title.Substring(start, end - start).Trim();

            var split = content.Split(',');

            if (split.Any(DatabaseMapping.IsTagValid))
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

        return output.Trim(' ', ',').RemoveMultipleWhiteSpaces().FirstCharUpper();
    }

    public static string SanitizeString(string input) =>
        Regex.Replace(input.ToLowerInvariant().RemoveDiacritics(),
            "[^a-z0-9 ]", string.Empty).RemoveIrrelevantWords().RemoveMultipleWhiteSpaces();

    private static readonly HashSet<string> IrrelevantWords = new() {"mit", "und"};

    public static string RemoveIrrelevantWords(this string text) =>
        IrrelevantWords.Aggregate(text, (current, word) =>
            current.Replace(word, string.Empty));

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

    public static string FirstCharUpper(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        if (input.Length == 1)
            return char.ToUpper(input[0]).ToString();
        return char.ToUpper(input[0]) + input[1..];
    }

    public static IEnumerable<string> ExtractSingleTagsFromTitle(string? title) =>
        ExtractElementFromTitle(title, TitleElement.Tag).Split(',')
            .Where(x => !string.IsNullOrEmpty(x) && DatabaseMapping.IsTagValid(x)).Select(x => NormalizeTag(x.Trim()))
            .Distinct();

    // (?<=\/)([A-z]*?)(?=\.png)
    private static readonly Regex PictogramRegex = new(@"(?<=\/)([A-z]*?)(?=\.png)");

    public static IEnumerable<string> ExtractTagsFromPictogram(string? pictogram)
        => string.IsNullOrEmpty(pictogram)
            ? Array.Empty<string>()
            : PictogramRegex.Matches(pictogram).Select(x => NormalizeTag(x.Value.Trim().ToUpper())).Distinct();

    public static IEnumerable<string> ExtractCombinedTags(string? title, string? pictogram) =>
        ExtractSingleTagsFromTitle(title).Concat(ExtractTagsFromPictogram(pictogram)).Distinct();

    internal static string NormalizeTag(string tag) =>
        tag switch
        {
            "VEG" => "Veg",
            "GF" => "Gf",
            "Fi" => "F",
            _ => tag
        };

    public static DateOnly GetDateFromTimestamp(int timestamp)
    {
        var utcTimestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp);
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

    public static int? BigFloatStringToInt(string? value)
    {
        return FloatStringToInt(value) / 10;
    }

    public static string?[] GetSideDishes(string? sideDishes) => ExtractElementFromTitle(sideDishes, TitleElement.Name)
        .Replace("Wahlbeilagen:", string.Empty).Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
        .ToArray();
}
