namespace MensattScraper.DestinationCompat;

public class Occurrence
{
    public Occurrence(Guid id, Guid dish, DateTime? notAvailableAfter)
    {
        this.Id = id;
        this.Dish = dish;
        this.NotAvailableAfter = notAvailableAfter;
    }
    public Guid Id { get; private set; }
    public Guid Dish { get; private set; }
    public DateTime? NotAvailableAfter { get; private set; }


}
