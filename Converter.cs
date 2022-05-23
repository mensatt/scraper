namespace MensattScraper;

public static class Converter
{

    public enum TitleElement
    {
        Name, Tag
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
        ExtractElementFromTitle(title, TitleElement.Tag).Split(',');

    public static DateOnly GetDateFromTimestamp(int timestamp) => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(timestamp).Date);

    public static int EuroToCents(string cents) => int.Parse(cents.Replace(",", string.Empty));

}