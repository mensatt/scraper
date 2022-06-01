namespace MensattScraper.Tests.Converter;

public class FloatToStringUnitTest
{
    [Theory]
    [InlineData("1.0", 10)]
    [InlineData("0.5", 5)]
    [InlineData("123.456", 123456)]
    [InlineData("0.0", 0)]
    public void NumberWithDot(string number, int expected)
    {
        var result = MensattScraper.Converter.FloatStringToInt(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1,0", 10)]
    [InlineData("0,5", 5)]
    [InlineData("123,456", 123456)]
    [InlineData("0,0", 0)]
    public void NumberWithComma(string number, int expected)
    {
        var result = MensattScraper.Converter.FloatStringToInt(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("10", 10)]
    [InlineData("05", 5)]
    [InlineData("123456", 123456)]
    [InlineData("00", 0)]
    public void BareNumber(string number, int expected)
    {
        var result = MensattScraper.Converter.FloatStringToInt(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NullOrEmptyNumber(string number, int? expected)
    {
        var result = MensattScraper.Converter.FloatStringToInt(number);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("null", null)]
    [InlineData("Hello", null)]
    [InlineData("👍👍👍👍👍", null)]
    public void NotANumber(string number, int? expected)
    {
        var result = MensattScraper.Converter.FloatStringToInt(number);
        Assert.Equal(expected, result);
    }
}