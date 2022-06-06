namespace MensattScraper.DestinationCompat;

public struct Location
{
    public Location(Guid id, string name, uint locationId)
    {
        Id = id;
        Name = name;
        LocationId = locationId;
    }

    public Guid Id { get; }

    public string Name { get; }

    public uint LocationId { get; }
}