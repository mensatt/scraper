namespace MensattScraper.Tests.Converter;

public class FirstCharUpperUnitTest
{
    [Theory]
    [InlineData("a", "A")]
    [InlineData("1", "1")]
    [InlineData("A", "A")]
    public void OneCharStrings(string str, string expected)
    {
        var result = str.FirstCharUpper();

        Assert.Equal(expected, result);
    }


    [Theory]
    [InlineData("aaa", "Aaa")]
    [InlineData("1aa", "1aa")]
    [InlineData("ABC", "ABC")]
    public void MultiCharStrings(string str, string expected)
    {
        var result = str.FirstCharUpper();

        Assert.Equal(expected, result);
    }
}