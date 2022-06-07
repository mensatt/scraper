using MensattScraper.DestinationCompat;

namespace MensattScraper.DatabaseSupport;

public class DatabaseMapping
{
    private List<Location> _locations;
    private List<Tag> _tags;
    private readonly IDatabaseWrapper _databaseWrapper;

    public DatabaseMapping(IDatabaseWrapper databaseWrapper)
    {
        _locations = new();
        _tags = new();
        _databaseWrapper = databaseWrapper;
    }

    public void RefreshDatabaseMappings()
    {
        // Locking on the current instance only works, because this is essentially a singleton.
        // This needs to be enforced, otherwise synchronisation errors will occur.
        lock (this)
        {
            _locations = _databaseWrapper.ExecuteSelectIdNameLocationIdCommand();
            _tags = _databaseWrapper.ExecuteSelectTagAllCommand();
        }
    }

    // I believe those methods don't need to be locked, as they are readonly
    public Guid GetLocationGuidByLocationId(int id) => _locations.Find(location => location.LocationId == id).Id;
    public bool IsTagValid(string tagKey) => _tags.Any(tag => tag.Key == tagKey);
}