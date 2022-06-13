namespace MensattScraper.Tests.Converter;

public class RemoveIrrelevantWordsUnitTest
{
    [Theory]
    [InlineData("Pizza", "Pizza")]
    [InlineData("Nudeln + Yum", "Nudeln + Yum")]
    public void NoIrrelevantWords(string text, string expected)
    {
        var result = text.RemoveIrrelevantWords();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("mit")]
    [InlineData("und")]
    [InlineData("mitundmit")]
    public void OnlyIrrelevantWords(string text)
    {
        var result = text.RemoveIrrelevantWords();

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Pizza mit Oliven", "Pizza  Oliven")]
    [InlineData("Pizza und Nudeln", "Pizza  Nudeln")]
    [InlineData("Schnitzel mit und Nudeln", "Schnitzel   Nudeln")]
    public void MixedWords(string text, string expected)
    {
        var result = text.RemoveIrrelevantWords();

        Assert.Equal(expected, result);
    }
}