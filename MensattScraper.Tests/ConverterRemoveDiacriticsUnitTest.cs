namespace MensattScraper.Tests;

public class ConverterRemoveDiacriticsUnitTest
{
    [Theory]
    [InlineData("ÄÖÜÊÉÈ", "AOUEEE")]
    [InlineData("This is a tést", "This is a test")]
    [InlineData("Pömmés Frîtès", "Pommes Frites")]
    public void StringWithDiacritics(string input, string expected)
    {
        var result = input.RemoveDiacritics();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Please do not replace anything here!", "Please do not replace anything here!")]
    [InlineData("°!§$%&/()=?`*',.-;:_*'«∑€∑®€†®Ω¨iøπ•æœ@∆ºª©ƒ∂", "°!§$%&/()=?`*',.-;:_*'«∑€∑®€†®Ω¨iøπ•æœ@∆ºª©ƒ∂")]
    public void StringWithoutDiacritics(string input, string expected)
    {
        var result = input.RemoveDiacritics();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void EmptyString()
    {
        var result = string.Empty.RemoveDiacritics();

        Assert.Equal(string.Empty, result);
    }
}