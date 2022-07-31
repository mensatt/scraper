namespace MensattScraper.Discord;

public class TransferData
{
    public TransferData(Guid createdDishId, string dishAlias, List<FuzzyResult> results)
    {
        CreatedDishId = createdDishId;
        DishAlias = dishAlias;
        Results = results;
        FullDishTitle = null;
        SanitizedDishTitle = null;
        Occurrence = default;
    }

    public List<FuzzyResult> Results { get; }

    public string FullDishTitle { set; get; }
    public string SanitizedDishTitle { set; get; }
    public Guid Occurrence { set; get; }
    public Guid CreatedDishId { get; }
    public string DishAlias { get; }
}