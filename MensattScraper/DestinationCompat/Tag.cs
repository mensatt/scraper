namespace MensattScraper.DestinationCompat;

public struct Tag
{
    public Tag(string key, string name, string description, string? shortName, Priority priority, bool isAllergy)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ShortName = shortName;
        Priority = priority;
        IsAllergy = isAllergy;
    }

    public string Key { get; }

    public string Name { get; }

    public string Description { get; }

    public string? ShortName { get; }

    public Priority Priority { get; }

    public bool IsAllergy { get; }
}