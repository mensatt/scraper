namespace MensattScraper.Tests;

public class ConverterRemoveMultipleWhiteSpaces
{
    [Theory]
    [InlineData("Es gibt Essen", "Es gibt Essen")]
    [InlineData("NoWhitespace", "NoWhitespace")]
    [InlineData("Mmmm 🍕", "Mmmm 🍕")]
    public void TitleWithSingleSpaces(string title, string expected)
    {
        var result = Converter.RemoveMultipleWhiteSpaces(title);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Es  gibt Essen", "Es gibt Essen")]
    [InlineData("Es  gibt  Essen", "Es gibt Essen")]
    [InlineData("Viele   Leerzeichen  gibt    es  hier 🫠", "Viele Leerzeichen gibt es hier 🫠")]
    [InlineData("1 2  3   4    5     ", "1 2 3 4 5")]
    public void TitleWithMultipleSpaces(string title, string expected)
    {
        var result = Converter.RemoveMultipleWhiteSpaces(title);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void EmptyTitle()
    {
        var result = Converter.RemoveMultipleWhiteSpaces(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(" ", "")]
    [InlineData("   ", "")]
    [InlineData("       ", "")]
    public void TitleConsistingOfWhitespaces(string title, string expected)
    {
        var result = Converter.RemoveMultipleWhiteSpaces(title);

        Assert.Equal(expected, result);
    }
}