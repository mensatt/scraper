namespace MensattScraper.Tests.Converter;

public class GetSideDishesUnitTest
{
    [Fact]
    public void EmptyString()
    {
        var result = MensattScraper.Converter.GetSideDishes(string.Empty);

        Assert.Equal(Array.Empty<string>(), result);
    }

    [Theory]
    [InlineData("Wahlbeilagen:A", new[] {"A"})]
    [InlineData("A", new[] {"A"})]
    [InlineData("Wahlbeilagen: A,B,C,D", new[] {"A", "B", "C", "D"})]
    [InlineData("Wahlbeilagen: 🥺", new[] {"🥺"})]
    public void ValidSideDishes(string? sideDish, string?[] expected)
    {
        var result = MensattScraper.Converter.GetSideDishes(sideDish);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("A,,B,,C,,D", new[] {"A", "B", "C", "D"})]
    [InlineData(",,,,,,,,,,e,,,,,,,,", new[] {"E"})]
    [InlineData(",,,,,,,,,,,,,,,,", new string[] { })]
    public void AdditionalCommasInSideDishes(string? sideDish, string?[] expected)
    {
        var result = MensattScraper.Converter.GetSideDishes(sideDish);

        Assert.Equal(expected, result);
    }
}