namespace MensattScraper.Internals;

public class ConfidenceSuggestion
{
    public Guid OccurrenceId;
    public readonly Guid DishId;
    public readonly string CreatedDishAlias;
    public readonly List<Tuple<float, string>> Suggestions;

    public ConfidenceSuggestion(Guid occurrenceId, Guid dishId, string createdDishAlias,
        List<Tuple<float, string>> suggestions)
    {
        OccurrenceId = occurrenceId;
        DishId = dishId;
        CreatedDishAlias = createdDishAlias;
        Suggestions = suggestions;
    }

    public ConfidenceSuggestion(Guid occurrenceId, Guid dishId, string createdDishAlias,
        IEnumerable<string> suggestions)
    {
        OccurrenceId = occurrenceId;
        DishId = dishId;
        CreatedDishAlias = createdDishAlias;
        Suggestions = new();
        foreach (var suggestion in suggestions)
            Suggestions.Add(new(float.NaN, suggestion));
    }
}