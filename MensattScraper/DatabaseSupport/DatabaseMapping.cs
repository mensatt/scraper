using MensattScraper.DestinationCompat;
using Npgsql;

namespace MensattScraper.DatabaseSupport;

public static class DatabaseMapping
{
    private static List<Location> _locations;
    private static List<Tag> _tags;
    private static readonly IDatabaseWrapper? DatabaseWrapper;

    // I do not really like that we are using multiple potentially blocking calls in the static constructor at the moment
    // However making this a static class seems to be the best approach currently (to prevent passing an object through
    // to every IDatabaseWrapper), but it could be replaced by a singleton later on (with dependency injection).
    static DatabaseMapping()
    {
        _locations = new();
        _tags = new();

        try
        {
            DatabaseWrapper =
                new NpgsqlDatabaseWrapper(
                    "HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt");
            DatabaseWrapper.ConnectAndPrepare();
            RefreshDatabaseMappings();
        }
        catch (NpgsqlException)
        {
            // This *should* only happen in unit tests
            DatabaseWrapper = null;
            List<string> tagKeys = new()
            {
                "1",
                "2", "4", "5", "7", "8", "9", "10", "11", "12", "13", "30", "S", "R", "G", "L", "W", "F", "V", "Veg",
                "MSC",
                "Gf", "CO2", "B", "MV", "Wz", "Ro", "Ge", "Hf", "Kr", "Ei", "Fi", "Er", "So", "Mi", "Man", "Hs", "Wa",
                "Ka",
                "Pe", "Pa", "Pi", "Mac", "Sel", "Sen", "Ses", "Su", "Lu", "We"
            };
            foreach (var tagKey in tagKeys)
            {
                _tags.Add(new(tagKey, string.Empty, string.Empty, null, Priority.UNSET, false));
            }
        }
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