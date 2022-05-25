using System.Text;

namespace MensattScraper.Tests;

public class ConverterExtractElementFromTitle
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
            var randomString = GenerateRandomString(titleLength);

            Assert.Equal(randomString, Converter.ExtractElementFromTitle(randomString, Converter.TitleElement.Name));
        }
    }

    [Theory]
    [InlineData("This is a sample title", "This is a sample title")]
    [InlineData("", "")]
    [InlineData("Pizza🍕👍", "Pizza🍕👍")]
    public void TitleWithoutParentheses(string title, string expected)
    {
        var result = Converter.ExtractElementFromTitle(title, Converter.TitleElement.Name);

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
    public void TitleWithNonTagParentheses(string title, string expected)
    {
        var result = Converter.ExtractElementFromTitle(title, Converter.TitleElement.Name);

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
    public void TitleWithNonTagMismatchedParentheses(string title)
    {
        var result = Converter.ExtractElementFromTitle(title, Converter.TitleElement.Name);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Pizza Mediterrane (1,4,10,Wz,So,Mi,Ge)",
        "Pizza Mediterrane")]
    [InlineData("Test (1,4)", "Test")]
    [InlineData("(Wz,So,Mi,Ge)", "")]
    [InlineData("My (1) dear (Wz) so (Hafer) many (30) tags (V)", "My dear so many tags")]
    [InlineData("Hackbraten (Wz,Ei,So,Sen) mit Jus (1,Wz,Sel,Ge) Kartoffel-Kräuterpüree (7,12,Mi)", "Hackbraten mit Jus Kartoffel-Kräuterpüree")]
    public void TitleWithTagParentheses(string title, string expected)
    {
        var result = Converter.ExtractElementFromTitle(title, Converter.TitleElement.Name);

        Assert.Equal(expected, result);
    }

    private static readonly char[] Alphabet =
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
        'W',
        'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
        't',
        'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '!', '§', '€'
    };

    private static readonly Random Rng = new Random(0xae75a77);

    private static string GenerateRandomString(uint length)
    {
        var resultBuilder = new StringBuilder((int) length);
        for (uint i = 0; i < length; i++)
            resultBuilder.Append(Alphabet[Rng.Next(0, Alphabet.Length)]);
        return resultBuilder.ToString();
    }
}