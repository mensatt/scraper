namespace MensattScraper.Tests.Converter;

public class ExtractElementFromTitleUnitTest
{
    [Theory]
    [InlineData("This is a sample title", "This is a sample title")]
    [InlineData("", "")]
    [InlineData("Pizza🍕👍", "Pizza🍕👍")]
    public void TitleWithoutParentheses(string? title, string expected)
    {
        var result =
            MensattScraper.Converter.ExtractElementFromTitle(title, MensattScraper.Converter.TitleElement.Name);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("This is a sample title(These, are, no, tags)!", "This is a sample title(These, are, no, tags)!")]
    [InlineData("()", "()")]
    [InlineData("()()", "()()")]
    [InlineData("(Hello)", "(Hello)")]
    [InlineData("(Hello,World)", "(Hello,World)")]
    [InlineData("This (is) a (sentence)", "This (is) a (sentence)")]
    [InlineData("Pizza Mediterrane (Oliven, Peperoni, Paprika, Zwiebeln)",
        "Pizza Mediterrane (Oliven, Peperoni, Paprika, Zwiebeln)")]
    public void TitleWithNonTagParentheses(string? title, string expected)
    {
        var result =
            MensattScraper.Converter.ExtractElementFromTitle(title, MensattScraper.Converter.TitleElement.Name);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("(")]
    [InlineData("(()")]
    [InlineData("())")]
    [InlineData("Mensa((tt) (👍")]
    [InlineData("M)ensa(tt ))👍")]
    [InlineData("((((((((((((((((())()(")]
    [InlineData(")))))))))))))()())))")]
    public void TitleWithNonTagMismatchedParentheses(string? title)
    {
        var result =
            MensattScraper.Converter.ExtractElementFromTitle(title, MensattScraper.Converter.TitleElement.Name);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Pizza Mediterrane (1,4,10,Wz,So,Mi,Ge)",
        "Pizza Mediterrane")]
    [InlineData("Test (1,4)", "Test")]
    [InlineData("(Wz,So,Mi,Ge)", "")]
    [InlineData("My (1) dear (Wz) so (Hf) many (30) tags (V)", "My dear so many tags")]
    [InlineData("Hackbraten (Wz,Ei,So,Sen) mit Jus (1,Wz,Sel,Ge) Kartoffel-Kräuterpüree (7,12,Mi)",
        "Hackbraten mit Jus Kartoffel-Kräuterpüree")]
    [InlineData("Vegane Currywurst mit Soße und Pommes frites(Wz)", "Vegane Currywurst mit Soße und Pommes frites")]
    [InlineData("Putenschnitzel (Wz) und Remouladensoße (4,5,Wz,Ei,So,Mi)und Chips (Wz)",
        "Putenschnitzel und Remouladensoße und Chips")]
    public void TitleWithTagParentheses(string? title, string expected)
    {
        var result =
            MensattScraper.Converter.ExtractElementFromTitle(title, MensattScraper.Converter.TitleElement.Name);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("a", "A")]
    [InlineData("pommes Frites", "Pommes Frites")]
    [InlineData("1Test", "1Test")]
    public void TitleWithLowercaseFirstChar(string? title, string expected)
    {
        var result =
            MensattScraper.Converter.ExtractElementFromTitle(title, MensattScraper.Converter.TitleElement.Name);

        Assert.Equal(expected, result);
    }
}