namespace MensattScraper;

public static class Converter
{
    public enum TitleElement
    {
        Name,
        Tag
    }

    public static string ExtractElementFromTitle(string title, TitleElement titleElement)
    {
        var ret = string.Empty;
        var isTag = false;
        foreach (var c in title)
        {
            switch (c)
            {
                case '(':
                    isTag = true;
                    break;
                case ')':
                    isTag = false;
                    if (titleElement == TitleElement.Tag)
                        ret += ',';
                    break;
                default:
                {
                    switch (isTag)
                    {
                        case true when titleElement == TitleElement.Tag:
                        case false when titleElement == TitleElement.Name:
                            ret += c;
                            break;
                    }

                    break;
                }
            }
        }

        return ret.Trim();
    }

    public static string[] ExtractSingleTagsFromTitle(string title) =>
        ExtractElementFromTitle(title, TitleElement.Tag).Split(',').Where(x => !string.IsNullOrEmpty(x)).ToArray();

    public static DateOnly GetDateFromTimestamp(int timestamp) =>
        DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(timestamp).Date);

    public static int FloatToInt(string cents) =>
        int.Parse(cents.Replace(",", string.Empty).Replace(".", String.Empty));

    public static string[] GetSideDishes(string sideDishes) => ExtractElementFromTitle(sideDishes, TitleElement.Name)
        .Replace("Wahlbeilagen: ", string.Empty).Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x))
        .ToArray();
}