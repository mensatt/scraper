using MensattScraper.DestinationCompat;

namespace MensattScraper.DatabaseSupport;

public static class DatabaseMapping
{
    private static List<Location> _locations;
    private static List<Tag> _tags;
    private static readonly IDatabaseWrapper DatabaseWrapper;

    // I do not really like that we are using multiple potentially blocking calls in the static constructor at the moment
    // However making this a static class seems to be the best approach currently (to prevent passing an object through
    // to every IDatabaseWrapper), but it could be replaced by a singleton later on (with dependency injection).
    static DatabaseMapping()
    {
        _locations = new();
        _tags = new();
        DatabaseWrapper =
            new NpgsqlDatabaseWrapper("HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt");
        DatabaseWrapper.ConnectAndPrepare();
        RefreshDatabaseMappings();
    }


    private static void RefreshDatabaseMappings()
    {
        _locations = DatabaseWrapper.ExecuteSelectIdNameLocationIdCommand();
        _tags = DatabaseWrapper.ExecuteSelectTagAllCommand();
    }

    // I believe those methods don't need to be locked, as they are readonly
    public static Guid GetLocationGuidByLocationId(int id) => _locations.Find(location => location.LocationId == id).Id;
    public static bool IsTagValid(string tagKey) => _tags.Any(tag => tag.Key == tagKey);
}