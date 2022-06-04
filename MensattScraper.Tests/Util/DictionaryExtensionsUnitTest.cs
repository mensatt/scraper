using MensattScraper.Util;

namespace MensattScraper.Tests.Util;

public class DictionaryExtensionsUnitTest
{
    public static TheoryData<Dictionary<string, int>> SampleDictionariesData => new()
    {
        new()
        {
            {"one", 1},
            {"two", 2},
            {"three", 3},
        },
        new()
    };

    [Theory]
    [MemberData(nameof(SampleDictionariesData))]
    public void FalsePredicateLeavesDictionaryUnchanged(Dictionary<string, int> input)
    {
        var original = new Dictionary<string, int>(input);

        input.RemoveAllByKey(_ => false);

        Assert.Equal(original, input);
    }

    [Fact]
    public void RemoveOneKeepRest()
    {
        var testDict = new Dictionary<string, int>
        {
            {"one", 1},
            {"two", 2},
            {"three", 3},
        };

        var expectedDict = new Dictionary<string, int>
        {
            {"two", 2},
            {"three", 3},
        };

        testDict.RemoveAllByKey(key => key == "one");

        Assert.Equal(expectedDict, testDict);
    }

    [Fact]
    public void RemoveAllKeepNone()
    {
        var testDict = new Dictionary<string, int>
        {
            {"one", 1},
            {"two", 2},
            {"three", 3},
        };

        testDict.RemoveAllByKey(_ => true);

        Assert.Empty(testDict);
    }
}