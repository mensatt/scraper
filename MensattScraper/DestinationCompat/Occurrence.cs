namespace MensattScraper.DestinationCompat;

public class Occurrence
{
    public Occurrence(Guid id, Guid dish, DateTime? notAvailableAfter = null)
    {
        Id = id;
        Dish = dish;
        NotAvailableAfter = notAvailableAfter;
    }

    public Guid Id { get; private set; }
    public Guid Dish { get; private set; }
    public DateTime? NotAvailableAfter { get; private set; }
}
