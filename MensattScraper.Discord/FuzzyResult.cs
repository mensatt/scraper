namespace MensattScraper.Discord;

public class FuzzyResult : IComparable<FuzzyResult>
{
    public int Score { get; }
    public Guid Dish { get; }
    public string Matched { get; }

    public FuzzyResult(int score, Guid dish, string matched)
    {
        Score = score;
        Dish = dish;
        Matched = matched;
    }

    public override string ToString()
    {
        return $"{nameof(Score)}: {Score}, {nameof(Dish)}: {Dish}, {nameof(Matched)}: {Matched}";
    }

    public int CompareTo(FuzzyResult? other)
    {
        return other!.Score.CompareTo(Score);
    }
}