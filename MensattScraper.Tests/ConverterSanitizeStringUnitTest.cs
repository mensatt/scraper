namespace MensattScraper.Tests;

public class ConverterSanitizeStringUnitTest
{

    [Theory]
    [InlineData("°!§$%&/()=?`*';:_,.-#+´`^¥∑‚≈€∂®ƒ√†©∫Ωª~µ⁄∆∞ø@…πœ–•æ‘±'¿≠}{|][¢¶“¡", "")]
    [InlineData("🫠🙈😜🥸🤫🫥🤖😻🙀💩👱🏽🧑‍🌾🧑‍🌾【₣▼◇▷❀➠➠➢", "")]
    [InlineData("門薩特是有史以來最好的平台", "")]
    public void FullyInvalidStrings(string input, string expected)
    {
        var result = Converter.SanitizeString(input);
        
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("Pommes Frites", "pommes frites")]
    [InlineData("pomm's frit's", "pomms frits")]
    [InlineData("Döner mit Dän", "doner mit dan")]
    [InlineData("Pizza mit Oliven,Tomaten und einem Kilo Salz", "pizza mit oliventomaten und einem kilo salz")]
    public void StandardDishTitles(string input, string expected)
    {
        var result = Converter.SanitizeString(input);
        
        Assert.Equal(expected, result);
    }

}