namespace MensattScraper.DestinationCompat;

public class Occurrence
{
    public Occurrence(Guid id, Guid dish, DateTime? notAvailableAfter = null)
    {
        Id = id;
        Dish = dish;
        NotAvailableAfter = notAvailableAfter;
    }

    public Occurrence(Guid id, Guid dish, DateTime? notAvailableAfter, int? priceStudent, int? priceStaff,
        int? priceGuest, List<string>? tags, int? kj, int? kcal, int? fat, int? saturatedFat, int? carbohydrates,
        int? sugar, int? fiber, int? protein, int? salt)
    {
        Id = id;
        Dish = dish;
        NotAvailableAfter = notAvailableAfter;
        PriceStudent = priceStudent;
        PriceStaff = priceStaff;
        PriceGuest = priceGuest;
        Tags = tags;
        Kj = kj;
        Kcal = kcal;
        Fat = fat;
        SaturatedFat = saturatedFat;
        Carbohydrates = carbohydrates;
        Sugar = sugar;
        Fiber = fiber;
        Protein = protein;
        Salt = salt;
    }

    public Guid Id { get; private set; }
    public Guid Dish { get; private set; }
    public DateTime? NotAvailableAfter { get; private set; }

    // TODO: Make side dishes updateable

    public int? PriceStudent { get; set; }

    public int? PriceStaff { get; set; }

    public int? PriceGuest { get; set; }

    public List<string>? Tags { get; set; }

    public int? Kj { get; set; }

    public int? Kcal { get; set; }

    public int? Fat { get; set; }

    public int? SaturatedFat { get; set; }

    public int? Carbohydrates { get; set; }

    public int? Sugar { get; set; }

    public int? Fiber { get; set; }

    public int? Protein { get; set; }

    public int? Salt { get; set; }
}
