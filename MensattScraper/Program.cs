using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;

namespace MensattScraper;

public class Program
{
    private const string ApiUrl = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";
    private const string DbConnection = "HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt";

    private static Program? _instance;

    private static Program Instance => _instance ??= new();

    public static void Main(string[] args)
    {
        Instance.StartScraper();
    }

    private void StartScraper()
    {
        IDatabaseWrapper privateDatabaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
        privateDatabaseWrapper.ConnectAndPrepare();
        var databaseMapper = new DatabaseMapping(privateDatabaseWrapper);

        // Creating multiple database wrappers on the same connection should be fine, as they are pooled
        IDatabaseWrapper databaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
        IDataProvider dataProvider = new HttpDataProvider(ApiUrl);
        using var scraper = new Scraper(databaseWrapper, databaseMapper, dataProvider);
        scraper.Initialize();
        scraper.Scrape();
    }
}