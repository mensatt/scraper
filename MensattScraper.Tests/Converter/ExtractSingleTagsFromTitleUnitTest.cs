namespace MensattScraper.Tests.Converter;

public class ExtractSingleTagsFromTitleUnitTest
{
    [Theory]
    [InlineData("")]
    [InlineData("Pizza mit Oliven und 👍")]
    [InlineData("(Dies, sind, alles, keine, Tags)")]
    [InlineData("(,,,,)")]
    public void TitleWithoutTags(string? title)
    {
        var result = MensattScraper.Converter.ExtractSingleTagsFromTitle(title);

        Assert.Equal(Array.Empty<string>(), result);
    }

    [Theory]
    [InlineData("(Wz,Wz,Wz,Wz,Wz,Wz,Wz)", new[] {"Wz"})]
    [InlineData("(1,2,4,4,2,1)", new[] {"1", "2", "4"})]
    public void TitleWithDuplicatesTags(string? title, string[] expected)
    {
        var result = MensattScraper.Converter.ExtractSingleTagsFromTitle(title);

        Assert.Equal(expected, result);
    }
}