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
        _locations = _databaseWrapper.ExecuteSelectIdNameLocationIdCommand();
        _tags = _databaseWrapper.ExecuteSelectTagAllCommand();
    }

    public Guid GetLocationGuidByLocationId(int id) => _locations.Find(location => location.LocationId == id).Id;
    public bool IsTagValid(string tagKey) => _tags.Any(tag => tag.Key == tagKey);
}