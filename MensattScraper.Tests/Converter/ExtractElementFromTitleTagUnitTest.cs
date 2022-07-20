namespace MensattScraper.Tests.Converter;

public class ExtractElementFromTitleTagUnitTest
{
    [Theory]
    [InlineData(10, 32)]
    [InlineData(128, 256)]
    [InlineData(256, 128)]
    [InlineData(3, 32)]
    public void RandomTitlesWithoutParentheses(uint titleLength, uint repeats)
    {
        for (uint i = 0; i < repeats; i++)
        {
            var randomString = RandomUtil.GenerateRandomString(titleLength);

            Assert.Equal(string.Empty,
                MensattScraper.Converter.ExtractElementFromTitle(randomString,
                    MensattScraper.Converter.TitleElement.Tag));
        }
    }

    [Theory]
    [InlineData("Pizza Mediterrane (1,4,10,Wz,So,Mi,Ge)",
        "1,4,10,Wz,So,Mi,Ge")]
    [InlineData("Test (1,4)", "1,4")]
    [InlineData("(Wz,So,Mi,Ge)", "Wz,So,Mi,Ge")]
    [InlineData("My (1) dear (Wz) so (Hf) many (30) tags (V)", "1,Wz,Hf,30,V")]
    [InlineData("Hackbraten (Wz,Ei,So,Sen) mit Jus (1,Wz,Sel,Ge) Kartoffel-Kräuterpüree (7,12,Mi)",
        "Wz,Ei,So,Sen,1,Wz,Sel,Ge,7,12,Mi")]
    [InlineData("Vegane Currywurst mit Soße und Pommes frites(Wz)", "Wz")]
    [InlineData("Putenschnitzel (Wz) und Remouladensoße (4,5,Wz,Ei,So,Mi)und Chips (Wz)",
        "Wz,4,5,Wz,Ei,So,Mi,Wz")]
    [InlineData("Hähnchenbrust ( Wz) mit Pfeffer-Rahmsoße (1,Wz,Mi,Sel,Ge)", "Wz,1,Wz,Mi,Sel,Ge")]
    public void TitleWithTagParentheses(string? title, string expected)
    {
        var result = MensattScraper.Converter.ExtractElementFromTitle(title, MensattScraper.Converter.TitleElement.Tag);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("This is a sample title")]
    [InlineData("")]
    [InlineData("Pizza🍕👍")]
    public void TitleWithoutParentheses(string? title)
    {
        var result = MensattScraper.Converter.ExtractElementFromTitle(title, MensattScraper.Converter.TitleElement.Tag);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TitleWithEmptyParentheses()
    {
        var result = MensattScraper.Converter.ExtractElementFromTitle("()", MensattScraper.Converter.TitleElement.Tag);

        Assert.Equal(string.Empty, result);
    }
}