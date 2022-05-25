namespace MensattScraper.Tests;

public class ConverterGetDateFromTimestampUnitTest
{
    public static TheoryData<int, DateOnly> SummerTimeBeforeMidnightData => new()
    {
        {1653602400, new DateOnly(2022, 5, 27)},
        {1652047200, new DateOnly(2022, 5, 9)},
        {1652133600, new DateOnly(2022, 5, 10)},
        {1652220000, new DateOnly(2022, 5, 11)}
    };

    [Theory]
    [MemberData(nameof(SummerTimeBeforeMidnightData))]
    public void SummertimeBeforeMidnight(int timestamp, DateOnly expected)
    {
        var result = Converter.GetDateFromTimestamp(timestamp);
        Assert.Equal(result, expected);
    }

    public static TheoryData<int, DateOnly> WinterTimeBeforeMidnightData => new()
    {
        {1641769200, new DateOnly(2022, 1, 10)},
        {1641855600, new DateOnly(2022, 1, 11)},
        {1641942000, new DateOnly(2022, 1, 12)},
        {1642028400, new DateOnly(2022, 1, 13)}
    };

    [Theory]
    [MemberData(nameof(WinterTimeBeforeMidnightData))]
    public void WinterTimeBeforeMidnight(int timestamp, DateOnly expected)
    {
        var result = Converter.GetDateFromTimestamp(timestamp);
        Assert.Equal(result, expected);
    }
}